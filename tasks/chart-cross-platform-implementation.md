# Chart & Gauge Cross-Platform Rendering — Implementation Instructions

**Status:** 🔬 Spike required before committing to an approach
**Supersedes:** The OxyPlot selection in `chart-library-decision.md` (see §2 — that decision was based on unsourced metrics and a misframing of the problem)

This document explains **how charts and gauges are rendered today** (with line references), **why the prior "replace with OxyPlot" decision is being corrected**, and gives **concrete, phased implementation steps** for making them cross-platform.

---

## 1. How Charts Are Rendered Today

There is **no missing "charting library."** There are **two full visualization engines vendored as source** inside this repo — they are first-party code we compile, not external NuGet packages:

| Engine | Location | Consumed by |
|--------|----------|-------------|
| **Chart** (`Microsoft.Reporting.Chart.WebForms`, legacy Dundas/MS Chart Controls) | [Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms/](../Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms/) | `ChartMapper` |
| **Gauge** (`GaugeContainer`, legacy Dundas Gauge) | Microsoft.ReportViewer.DataVisualization | `GaugeMapper` |

### 1.1 The render flow (chart)

```
ChartMapper.GetImage(imageType)                     ChartMapper.cs:580-607
  ├─ format = EMF+ or PNG                            ChartMapper.cs:588-600
  └─ m_coreChart.Save(stream, format)                ChartMapper.cs:605
       └─ Chart.Save(Stream, ChartImageFormat)       Chart.cs:1275-1321
            ├─ EMF path → chartPicture.SaveIntoMetafile(stream, emfType)   Chart.cs:1293
            └─ raster path:
                 image = chartPicture.GetImage(dpi)   Chart.cs:1296
                   └─ new Bitmap(Width, Height)        ChartPicture.cs:733
                      Graphics.FromImage(bitmap)       ChartPicture.cs:734
                      Paint(...) → draws all elements  ChartPicture.cs:752-...
                 image.Save(stream, ImageFormat.Png)   Chart.cs:1313
```

**Key fact:** the chart is **rasterized to a PNG (or recorded to an EMF+) and then embedded as an image** into the PDF / Excel / IMAGE output. It is never drawn directly into the page. The supported output formats are `Jpeg, Png, Bmp, Emf, EmfPlus, EmfDual` — there is **no SVG** on the report path ([ChartImageFormat.cs](../Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms/ChartImageFormat.cs)).

### 1.2 The render flow (gauge)

```
GaugeMapper.GetImage(imageType)                      GaugeMapper.cs:236-263
  ├─ format = EMF or PNG                             GaugeMapper.cs:244-255
  └─ m_coreGaugeContainer.SaveAsImage(stream, format) GaugeMapper.cs:261
```

Same pattern: build model → `SaveAsImage` → PNG/EMF → embed.

### 1.3 The rendering seam (this is the important part)

The Chart engine already has a rendering abstraction. Inheritance chain:

```
IChartRenderingEngine            (interface)   IChartRenderingEngine.cs:8
  ← ChartRenderingEngine         (dispatcher)  ChartRenderingEngine.cs:10
      ← ChartGraphics3D                        ChartGraphics3D.cs:8
          ← ChartGraphics        (chokepoint)  ChartGraphics.cs:11
```

- `ChartRenderingEngine` holds two backends and switches between them via `RenderingObject` ([ChartRenderingEngine.cs:18-32](../Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms/ChartRenderingEngine.cs#L18-L32)):
  - `GdiGraphics` — `RenderingType.Gdi`
  - `SvgChartGraphics` — `RenderingType.Svg`
- `GdiGraphics` is a **thin pass-through** over `System.Drawing.Graphics` — every method just forwards ([GdiGraphics.cs](../Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms/GdiGraphics.cs)).
- `ChartGraphics` is the **single high-level chokepoint** every chart-element painter uses (`DrawLineRel`, `FillRectangleAbs`, etc.). ~39 files reference it; they do **not** call `GdiGraphics` directly.

**Implication:** there is a natural seam (`IChartRenderingEngine`, ~40 methods) where a third backend could be inserted. This is the foundation of the recommended approach in §4.

---

## 2. Why the Prior OxyPlot Decision Is Corrected

A prior decision (formerly in `chart-library-decision.md`, and a now-deleted root `CHART_LIBRARY_EVALUATION_COMPLETE.md`) selected **OxyPlot 2.1.x** to replace the chart engine. That conclusion is **retracted** for the following concrete reasons:

| Problem | Evidence |
|---------|----------|
| ❌ **Fabricated metrics** | "45ms vs 50ms" perf tables, "85%+ coverage", "95%+ of reports", "7.8/10 score", "CVE scanning" — there is **no benchmark harness and no report corpus** in this repo to produce any of these numbers. They are unsourced. |
| ❌ **Dangling citations** | The decision cites `chart-libraries-research.md`, `CHART_LIBRARY_DEPENDENCY_ANALYSIS.md`, `INTEGRATION_TECHNICAL_DETAILS.md` — **none of these files exist.** |
| ❌ **Wrong problem framing** | It treats the chart engine as an external dependency to swap. It is **vendored source we own**, with an existing `IChartRenderingEngine` seam (§1.3) that the evaluation never mentions. |
| ❌ **Understates compatibility risk** | Replacement means re-implementing the RDL chart model → OxyPlot for **40+ chart types** plus 3D, financial/OHLC, radar, strip lines, annotations, smart data-label layout, statistical formulas, and palettes — and the doc itself admits several will be visually different or need custom work. For a component whose value is faithful RDLC reproduction, that is a **behavior/compat break**, not a port. |
| ❌ **Ignores the gauge engine** | Gauges use a **separate** engine (`GaugeContainer`, §1.2). Replacing the chart library does nothing for gauges. |

OxyPlot remains a legitimate option **only** under the trade-offs in §4 Option B (visual redesign acceptable). It should not be presented as a validated, low-risk, drop-in decision.

---

## 3. Scope of the System.Drawing Coupling

This is the real work, and it must be measured, not guessed.

**Measured fact:** `using System.Drawing*` appears in **73 files** across the Chart engine (`grep -c "using System.Drawing"` → 118 occurrences / 73 files). The gauge engine is additional.

The coupling splits into two categories that behave very differently on Linux:

| Category | Types | Assembly | Linux behavior |
|----------|-------|----------|----------------|
| ✅ **Portable value types** | `Color`, `Point`, `PointF`, `Rectangle`, `RectangleF`, `Size`, `SizeF` | `System.Drawing.Primitives` | Work cross-platform (no GDI+) |
| ❌ **GDI+-backed classes** | `Graphics`, `Bitmap`, `Pen`, `Brush`/`SolidBrush`, `Font`, `GraphicsPath`, `Matrix`, `Region`, `StringFormat`, `ImageAttributes`, `Metafile` | `System.Drawing.Common` | **Throw `PlatformNotSupportedException`** on non-Windows (.NET 7+) |

Two hard boundaries that any solution must cross:

1. **The pixel boundary** — `new Bitmap(...)` + `Graphics.FromImage(...)` ([ChartPicture.cs:733-734](../Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms/ChartPicture.cs#L733-L734)) and the EMF `Metafile` path ([Chart.cs:1281-1293](../Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms/Chart.cs#L1281-L1293)). These allocate GDI+ objects and throw on Linux.
2. **Text measurement** — `graphics.MeasureString(...)` ([GdiGraphics.cs:194-207](../Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms/GdiGraphics.cs#L194-L207)) needs a real `Graphics`; layout math depends on it.

The painters also **construct** GDI+ objects directly (e.g. `ChartGraphics` holds `Pen pen`, `SolidBrush solidBrush`, `Matrix myMatrix` — [ChartGraphics.cs:15-19](../Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms/ChartGraphics.cs#L15-L19)). So a Skia backend that only implements `IChartRenderingEngine` is **not sufficient by itself** — the objects handed to it (`Pen`, `Brush`, `Font`, `GraphicsPath`) must themselves be constructible on the target platform. This is the crux of the effort and the reason a spike (§5) comes first.

---

## 4. Options (Honest Trade-offs)

### Option A — Re-target the vendored engine to SkiaSharp (RECOMMENDED)

Add a SkiaSharp-backed `IChartRenderingEngine` and cross the two boundaries in §3, keeping the entire chart model and visual output.

| | |
|---|---|
| ✅ Preserves 100% of the chart model and ~100% visual fidelity | ❌ Large, deep change: the seam's contract is in GDI+ types; ~73 files construct GDI+ objects |
| ✅ Reuses the existing `IChartRenderingEngine` seam | ❌ Text metrics must move from `Graphics.MeasureString` to `SKFont` metrics — risk of layout drift |
| ✅ Can be **dual-backend**: Windows keeps `GdiGraphics` unchanged (zero risk), Linux uses Skia | ❌ Effort is genuinely uncertain until the spike (§5) |
| ✅ Same pattern extends to the gauge engine | |

Best fit when the goal is **faithful, drop-in cross-platform RDLC rendering**.

### Option B — Replace the chart engine with OxyPlot (the prior decision)

| | |
|---|---|
| ✅ Modern, maintained, MIT-licensed library | ❌ Re-implement RDL→OxyPlot mapping for 40+ chart types |
| ✅ Native cross-platform via SkiaSharp | ❌ Visual drift; several chart types need custom work or fallbacks |
| | ❌ Does nothing for gauges (separate engine) |
| | ❌ High report-compatibility risk |

Best fit **only** if a visual redesign is acceptable and pixel-parity with existing reports is an explicit non-goal.

### Option C — Keep charts/gauges Windows-only for now (graceful degradation)

Detect platform; on Linux, skip chart/gauge rasterization and emit a placeholder instead of throwing. This is a **stopgap**, not a solution, but it unblocks cross-platform Excel/PDF for report *bodies* while charts are addressed. Partially in place already via the `IImageProvider` seam (see `chart-image-abstraction-analysis.md`, which covers chart/gauge **background images** — a related but narrower concern).

---

## 5. Recommended Implementation Plan

Do **not** start coding a full backend or an OxyPlot adapter until Phase 0 answers the open questions.

### Phase 0 — Spike (time-boxed, ~1 week) 🔬

**Goal:** replace the uncertainty in §3/§4 with measured facts before committing.

- [ ] Build the solution on Linux and confirm the **exact** exception + call site when a chart renders (validates the §3 boundary claims).
- [ ] Prototype a minimal `SkiaChartGraphics : IChartRenderingEngine` that implements ~5 primitives (`DrawLine`, `FillRectangle`, `DrawString`, `MeasureString`, `FillPath`).
- [ ] Render one simple bar chart through it on Linux; compare pixels to the Windows GDI+ output.
- [ ] Decide the `Pen`/`Brush`/`Font`/`GraphicsPath` strategy: (a) keep GDI+ value construction where types are portable, (b) translate GDI+ objects to Skia inside the backend, or (c) introduce neutral paint/geometry types.
- [ ] **Output:** a spike report with measured effort → pick Option A or B with evidence.

### Phase 1 — Platform selection + graceful degradation (Option C safety net)

- [ ] Add a platform check at the render entry points: `ChartMapper.GetImage` ([ChartMapper.cs:580](../Microsoft.ReportViewer.Common/Microsoft.ReportingServices.OnDemandReportRendering/ChartMapper.cs#L580)) and `GaugeMapper.GetImage` ([GaugeMapper.cs:236](../Microsoft.ReportViewer.Common/Microsoft.ReportingServices.OnDemandReportRendering/GaugeMapper.cs#L236)).
- [ ] On unsupported platforms, return a placeholder image or `null` instead of letting GDI+ throw. Log once.
- [ ] Result: reports with charts no longer crash on Linux; charts render on Windows exactly as before.

### Phase 2 — Abstract the pixel/output boundary

- [ ] Introduce an output-surface abstraction so the raster/EMF boundary is not hard-coded to GDI+:
  - Raster: `new Bitmap` + `Graphics.FromImage` ([ChartPicture.cs:733-734](../Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms/ChartPicture.cs#L733-L734)) → `SKSurface`/`SKCanvas` on Skia.
  - Encode: `image.Save(stream, ImageFormat.Png)` ([Chart.cs:1313](../Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms/Chart.cs#L1313)) → `SKImage.Encode`.
  - EMF: keep `SaveIntoMetafile` **Windows-only** (EMF is inherently a Windows format; no cross-platform equivalent — document as a known limitation).

### Phase 3 — Abstract the GDI+ resource types + implement the Skia backend

This is the largest phase and has its own detailed task breakdown:

➡️ **[`chart-gdi-type-abstraction.md`](chart-gdi-type-abstraction.md)** — per-type task scope with measured usage counts, the `IDrawingResourceFactory` port design, and ordered milestones A–E.

In brief:
- [ ] Abstract the ~10 GDI+ resource types (`Pen`/`Brush`/`Font`/`GraphicsPath`/`Matrix`/`Region`/`StringFormat`/`ImageAttributes`) behind interfaces + a factory, keeping the portable value types (`Color`/`PointF`/`RectangleF`/`SizeF`) concrete.
- [ ] Create `SkiaChartGraphics` next to `GdiGraphics` ([GdiGraphics.cs](../Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms/GdiGraphics.cs)) and add `RenderingType.Skia` to the `RenderingObject` switch ([ChartRenderingEngine.cs:22-32](../Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms/ChartRenderingEngine.cs#L22-L32)).
- [ ] Route text measurement through the font abstraction (`SKFont` metrics), not `Graphics.MeasureString`.

### Phase 4 — Backend factory + integration

- [ ] Select the backend by platform (Windows → `GdiGraphics`, Linux/macOS → `SkiaChartGraphics`), mirroring the `ImageProviderFactory` pattern from the Excel work.
- [ ] Validate through the full report path: `LocalReport.Render("PDF")` and `Render("EXCELOPENXML")` with charts.

### Phase 5 — Apply the same pattern to the gauge engine

- [ ] Repeat Phases 2–4 for `GaugeContainer` ([GaugeMapper.cs:261](../Microsoft.ReportViewer.Common/Microsoft.ReportingServices.OnDemandReportRendering/GaugeMapper.cs#L261)).

### Phase 6 — Visual regression testing

- [ ] Build a corpus of representative RDLC reports (bar, line, pie, area, scatter, 3D, stock, radar, gauges).
- [ ] Render on Windows (GDI+) and Linux (Skia); diff images with a tolerance threshold.
- [ ] Document any type-specific deviations as known limitations. **Do not publish coverage percentages without this corpus.**

---

## 6. Known Constraints (Factual)

| Constraint | Detail |
|-----------|--------|
| **EMF is Windows-only** | `Metafile`/EMF has no cross-platform equivalent. On Linux, chart output must be raster (PNG). PDF/Excel embed both, so this is acceptable. |
| **Two engines, not one** | Chart and Gauge are independent GDI+ engines; both need the treatment. |
| **`System.Drawing.Common` currently referenced** | [DataVisualization.csproj:21](../Microsoft.ReportViewer.DataVisualization/Microsoft.ReportViewer.DataVisualization.csproj#L21). It cannot be removed until the GDI+ boundaries in §3 are crossed. |
| **Text metrics differ** | GDI+ and Skia measure text differently; expect sub-pixel layout differences requiring tolerance in regression tests. |

---

## 7. References (verified to exist)

- [ChartMapper.cs](../Microsoft.ReportViewer.Common/Microsoft.ReportingServices.OnDemandReportRendering/ChartMapper.cs) — `GetImage` (580), background image seam (5250-5284)
- [GaugeMapper.cs](../Microsoft.ReportViewer.Common/Microsoft.ReportingServices.OnDemandReportRendering/GaugeMapper.cs) — `GetImage` (236)
- [Chart.cs](../Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms/Chart.cs) — `Save` (1275)
- [ChartPicture.cs](../Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms/ChartPicture.cs) — `GetImage`/`Paint` (733, 752)
- [ChartRenderingEngine.cs](../Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms/ChartRenderingEngine.cs) — backend dispatcher (10)
- [IChartRenderingEngine.cs](../Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms/IChartRenderingEngine.cs) — the seam (8)
- [GdiGraphics.cs](../Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms/GdiGraphics.cs) — pass-through GDI+ backend (8)
- [ChartGraphics.cs](../Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms/ChartGraphics.cs) — high-level chokepoint (11)
- `chart-image-abstraction-analysis.md` — related `IImageProvider` work for chart/gauge **background** images
