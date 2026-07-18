# Chart Engine: GDI+ Type Abstraction — Task Scope

**Parent plan:** [`chart-cross-platform-implementation.md`](chart-cross-platform-implementation.md) (this doc details Phase 3's "translation strategy")
**Status:** 📋 Scoping — depends on Phase 0 spike to confirm the chosen strategy
**Goal:** Replace concrete GDI+ resource types with backend-agnostic **interfaces + an abstract factory**, so the chart engine can run on GDI+ (Windows), SkiaSharp (Linux/macOS), and any future backend.

---

## 1. Design principle

Today the drawing seam `IChartRenderingEngine` is abstract, but the **types passed through it are concrete GDI+ classes** (`Pen`, `Brush`, `Font`, `GraphicsPath`, …). A backend can only be swapped if those resource types are also abstract. The target is **Ports & Adapters**:

```
Painters (73 files)  ──►  ChartGraphics (chokepoint)  ──►  IChartRenderingEngine
        │                        │                                │
        └── construct resources via ─── IDrawingResourceFactory ──┘   (the PORT)
                                          ▲            ▲
                                   GdiResourceFactory  SkiaResourceFactory   (ADAPTERS)
```

Instead of `new Pen(color, width)`, code calls `factory.CreatePen(color, width)` and receives an `IPen`. Each backend implements the factory and the resource interfaces.

---

## 2. Type inventory (measured usage)

Counts are occurrences / files across `Microsoft.Reporting.Chart.WebForms`. Use them as **complexity indicators**, not time estimates.

### 2.1 KEEP as-is — portable value types (no work)

These live in `System.Drawing.Primitives`, which is **fully cross-platform** (no GDI+). Abstracting them would mean thousands of edits for zero portability gain.

| Type | Occurrences | Disposition |
|------|-------------|-------------|
| `Color` | 1207 | ✅ Keep |
| `PointF` / `Point` | 841 | ✅ Keep |
| `RectangleF` / `Rectangle` | 517 | ✅ Keep |
| `SizeF` / `Size` | 313 | ✅ Keep |

**Total ~2,878 occurrences require no change.** This is the single most important scoping fact: the bulk of "System.Drawing" usage is already portable.

### 2.2 ABSTRACT — GDI+-backed resource types

| Type | Occ | Files | Nature | Abstraction | Size |
|------|-----|-------|--------|-------------|------|
| `GraphicsPath` | 290 | 24 | Mutable builder (AddLine/Arc/Bezier/CloseFigure) | **`IGraphicsPath`** (behavior interface) | 🔴 XL |
| `Font` | 232 | 31 | Descriptor + metrics source | **`IChartFont`** | 🔴 XL (most spread) |
| `StringFormat` | 145 | 19 | Descriptor (alignment/trimming/flags) | **`ITextFormat`** | 🟠 L |
| `Pen` | 127 (43 `new`) | 16 | Descriptor (color/width/dash/cap/align) | **`IPen`** | 🟠 L |
| `SolidBrush` + `Brush` | 164 | ~14 | Descriptor (fill) | **`IBrush`/`ISolidBrush`** | 🟠 L |
| `Matrix` | 58 | 11 | Transform w/ ops | **`ITransform`** or `System.Numerics.Matrix3x2` | 🟡 M |
| `ImageAttributes` | 25 | 6 | Image draw options | **`IImageDrawOptions`** | 🟡 M |
| `Region` | 17 | 6 | Clip region w/ combine ops | **`IClipRegion`** | 🟢 S |
| `HatchBrush` | 10 | 2 | Patterned fill | `IHatchBrush : IBrush` | 🟢 S |
| `LinearGradientBrush` | 15 | 2 | Gradient fill | `IGradientBrush : IBrush` | 🟢 S |
| `TextureBrush` | 12 | 2 | Image fill | `ITextureBrush : IBrush` | 🟢 S |

### 2.3 ABSTRACT — surface & output boundary

| Type | Occ | Files | Disposition |
|------|-----|-------|-------------|
| `new Bitmap` + `Graphics.FromImage` | 8 + 8 | 4 | **`IRenderSurfaceFactory`** → GDI+ `Bitmap` / Skia `SKSurface` |
| `Metafile` | 3 | 2 | ⚠️ **Windows-only** — leave behind a platform guard (no cross-platform EMF) |

### 2.4 Enums (GDI+-namespaced but portable)

`SmoothingMode`, `TextRenderingHint`, `GraphicsUnit`, `LineCap`, `DashStyle`, `FillMode`, `HatchStyle`, `StringAlignment`, `StringFormatFlags`, `CombineMode`, `MatrixOrder`, `PenAlignment`. These are value enums and compile cross-platform. **Decision:** keep initially; mirror into neutral enums only if we want to drop the `System.Drawing.Common` reference entirely (a later cleanup, not a blocker).

---

## 3. Design decision to make in the spike: interfaces vs. descriptor records

The user goal is **interfaces for extensibility**. Two of the resource categories split on how well that fits — call this out explicitly and decide in Phase 0:

| Category | Types | Recommended shape | Why |
|----------|-------|-------------------|-----|
| **Behavior-rich** | `GraphicsPath`, `Region`, `Matrix` | **Interface** (`IGraphicsPath`, `IClipRegion`, `ITransform`) | They accumulate state and have real operations; backends need their own implementations. |
| **Immutable descriptors** | `Pen`, `Brush`, `Font`, `StringFormat` | **Interface OR `readonly record struct`** | They are parameter bags. Interfaces give maximum extensibility (the stated goal); records are simpler, allocation-cheap, and let each backend translate+cache at draw time. |

**Recommendation:** use **interfaces for all** to satisfy the extensibility goal and keep one consistent factory pattern, but allow descriptor `record` implementations *behind* those interfaces where a resource is pure data. This keeps the public shape uniform (everything is an interface + factory) without forcing heavyweight objects for what is really data. Confirm during the spike against one real backend.

---

## 4. Target namespace & core artifacts

Proposed internal namespace: `Microsoft.Reporting.Chart.WebForms.Rendering`.

**Ports (interfaces) to create:**
- `IDrawingResourceFactory` — creates every resource below (the central port)
- `IRenderSurfaceFactory` — creates the draw surface + encodes output
- `IPen`, `IBrush` (+ `ISolidBrush`, `IHatchBrush`, `IGradientBrush`, `ITextureBrush`)
- `IChartFont`, `ITextFormat`
- `IGraphicsPath`, `IClipRegion`, `ITransform`
- `IImageDrawOptions`

**Adapters (implementations):**
- GDI+ set: `GdiResourceFactory`, `GdiPen`, `GdiBrush`, … (wrap existing `System.Drawing` objects — behavior-preserving on Windows)
- Skia set: `SkiaResourceFactory`, `SkiaPen`, … (map to `SKPaint`/`SKPath`/`SKFont`)

**Modified seam:** `IChartRenderingEngine` method signatures change from `System.Drawing` types to the interfaces (e.g. `DrawLine(IPen, PointF, PointF)`). `PointF/RectangleF/Color` stay concrete.

---

## 5. Task breakdown (ordered)

Each task is independently reviewable. Sizes are relative (S/M/L/XL) from §2 counts.

### Milestone A — Foundation (no behavior change)

- [x] **A1. Create the `Rendering` namespace + port interfaces** (`IDrawingResourceFactory`, resource interfaces, `IRenderSurfaceFactory`). Interfaces only, no implementations. 🟢 S
  → **Drafted** in [`Microsoft.Reporting.Chart.WebForms/Rendering/`](../Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms/Rendering/): `IRenderingResource`, `IPen`, `IBrush` (+Solid/Gradient/Texture/Hatch), `IChartFont`, `ITextFormat`, `IGraphicsPath`, `IClipRegion`, `IImageDrawOptions`, `IDrawingResourceFactory`, `IRenderSurface`/`IRenderSurfaceFactory`. Member surfaces taken directly from Appendix A. Builds clean (0 errors). Design decisions baked in: value types/enums kept concrete; resources extend `IDisposable`; `Transform` uses `System.Numerics.Matrix3x2`.
- [x] **A2. Implement the GDI+ adapter set** wrapping existing `System.Drawing` objects (`GdiPen : IPen`, etc.) + `GdiResourceFactory`. Behavior-identical to today. 🟠 L
  **Drafted** in [`Microsoft.Reporting.Chart.WebForms/Rendering/Gdi/`](../Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms/Rendering/Gdi/) — one adapter class per resource interface (`GdiPen`, `GdiSolidBrush`/`GdiLinearGradientBrush`/`GdiTextureBrush`/`GdiHatchBrush`, `GdiChartFont`, `GdiTextFormat`, `GdiGraphicsPath`, `GdiClipRegion`, `GdiImageDrawOptions`, `GdiChartImage`, `GdiRenderSurface`/`GdiRenderSurfaceFactory`) plus `GdiResourceFactory : IDrawingResourceFactory` wiring them together. Each adapter constructs the exact same concrete GDI+ object the engine builds today and exposes it via an internal `Native*` property so sibling adapters (and, later, `ChartGraphics` itself) can reach the underlying object without a second abstraction layer. Builds clean (0 errors, 0 new warnings — confirmed no `Rendering\Gdi` hits in the build's CA1416 output). Not yet wired into `ChartGraphics`/`IChartRenderingEngine` — that's A3/B1. `IRenderSurface.Encode` only handles raster formats (Png/Jpeg/Bmp); Emf/EmfPlus/EmfDual remain out of scope for this surface until the Milestone D metafile path exists.
- [x] **A3. Add `IChartRenderingEngine` overloads that accept the interfaces**, delegating to the existing GDI+ methods (keep old signatures temporarily to avoid a big-bang change). 🟡 M
  **Drafted.** Added ~30 interface-typed overloads to [`IChartRenderingEngine.cs`](../Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms/IChartRenderingEngine.cs) — one per existing GDI+-typed member, plus `GetClipRegion`/`SetClipRegion` and `GetTransform`/`SetTransform` as method-pair equivalents of the `Clip`/`Transform` properties (properties can't be overloaded by type). All three implementers updated: `GdiGraphics` unwraps each `Rendering.*` interface to its wrapped GDI+ object (via private `Native(...)` helpers) and delegates to the pre-existing concrete-typed method — same call, same output. `ChartRenderingEngine` (the Gdi/Svg dispatcher) adds pure passthroughs to `RenderingObject`. `SvgChartGraphics` (legacy, not on the report render path) adds its own `Native(...)` unwrap helpers, since it doesn't share `GdiGraphics`'s. Added `GdiClipRegion(Region existingRegion)` constructor to support wrapping the live `Graphics.Clip` for `GetClipRegion()`. Builds clean (0 errors, 0 new warnings in any of the four changed files). No caller passes the new overloads yet — that's Milestone B.

### Milestone B — Migrate the chokepoint

- [x] **B1a. Inject `IDrawingResourceFactory` into `ChartGraphics`** ([ChartGraphics.cs:11](../Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms/ChartGraphics.cs#L11)) as a constructor-provided field, defaulting to `GdiResourceFactory` when not supplied (both existing call sites — `ChartPicture.cs`, `TextAnnotation.cs` — are unaffected). Infrastructure only; the field isn't consumed yet. 🟢 S
- [ ] **B1b. Retype the `pen`/`solidBrush`/`myMatrix` fields** to `IPen`/`ISolidBrush`/`Matrix3x2`, sourced from `resourceFactory`. **Descoped from B1a — discovered blocked, not merely deferred:**
  - `pen` is drawn together with a still-concrete `GraphicsPath` in `DrawPath(pen, path)` ([ChartGraphics.cs:2295](../Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms/ChartGraphics.cs#L2295)); no `DrawPath(IPen, GraphicsPath)` overload exists (by design — A3 only added matched-shape overloads). Needs **C7** (`GraphicsPath` → `IGraphicsPath`) first.
  - `solidBrush` is interchanged with `GetGradientBrush`/`GetHatchBrush`/`GetTextureBrush` results into one `Brush brush` local ([ChartGraphics.cs:2052-2063](../Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms/ChartGraphics.cs#L2052-L2063), [2202-2224](../Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms/ChartGraphics.cs#L2202-L2224)); those helpers still return concrete `Brush`. Needs **C4** (Brush family → `IBrush`) first, so all of them return `IBrush`-compatible values together.
  - ~~`myMatrix` feeds raw `GraphicsPath.Transform`/`Region.Transform` throughout the file~~ — **resolved by C1+C2**: `myMatrix` is now `Matrix3x2` (see C2 below); the handful of remaining `GraphicsPath.Transform(matrix)` call sites bridge via `Matrix3x2Extensions.ToGdiMatrix()` until C7 lands. `pen`/`solidBrush` remain blocked as described above.
  - **Do not** paper over this with mixed transitional overloads (`DrawPath(IPen, GraphicsPath)` etc.) or backend-specific casts inside `ChartGraphics` — both were considered and rejected as throwaway indirection. Revisit B1b once C4/C7 land, retyping the remaining two fields together in one coherent pass.
- [ ] **B2. Convert `ChartGraphics` high-level methods** (`DrawLineRel`, `FillRectangleAbs`, …) to construct resources via the factory. Depends on B1b. 🟠 L
  **Scope re-measured, materially larger than L.** Correction to an earlier note: the "20+ files" calling `GetGradientBrush`/`GetHatchBrush`/`GetTextureBrush` is the combined count across **three separate GDI+-coupled engines** — the real Chart-engine blast radius is just 6 files (`ChartGraphics.cs` + `AxisScaleSegment.cs` + 4 files under `ChartTypes\`: `AreaChart.cs`, `PieChart.cs`, `RangeChart.cs`, `StackedAreaChart.cs`). The other ~14 files are `GaugeGraphics`/`MapGraphics` call sites — **`GaugeGraphics` (`Microsoft.Reporting.Gauge.WebForms`) and `MapGraphics` (`Microsoft.Reporting.Map.WebForms`) each have their own separate, parallel `GetHatchBrush`/`GetGradientBrush` methods**, i.e. there are three GDI+-coupled rendering engines, not two — the parent plan only tracked gauge. Map's scope needs its own decision (in scope for this initiative, or stays Windows-only?).

  **Attempted and reverted (twice) — precise findings, not just "needs C7":**
  1. Converting `ChartGraphics.GetHatchBrush`/`GetGradientBrush`/`GetPieGradientBrush` to return `IBrush`/`IHatchBrush`/`IPathGradientBrush` compiles fine in isolation, but their sole real caller, `FillRectangleRel` (~180 lines), assigns the result to a `Brush brush` local that also flows from `GetTextureBrush` (still concrete) and gets passed to `FillRectangle`/`DrawCircleAbs` — reverted (confirmed zero net diff via `git diff --stat`).
  2. Converting `DrawCircleAbs(Pen, Brush, ...)` → `DrawCircleAbs(IPen, IBrush, ...)` (plus its own local `GraphicsPath` and `GetSector3DBrush`) **worked** for its `Pen`/`GraphicsPath` half — 3 independent external callers (`Axis.cs`, `PointAndFigureChart.cs` ×2) converted cleanly, no `Widen` coupling this time. But `DrawCircleAbs`'s `Brush brush` parameter is called from `FillRectangleRel`/`FillRectangleAbs` with a value that may have originated from `GetTextureBrush` — which loads images via `Microsoft.Reporting.Chart.WebForms.Utilities.ImageLoader.LoadImage(string) → System.Drawing.Image` (concrete, GDI+-only, and **entirely separate from the existing `IImageProvider` abstraction** in `chart-image-abstraction-analysis.md` — confirmed by reading `ImageLoader.cs`; the two never call each other). Reverted (confirmed zero net diff) rather than leave `ChartGraphics.cs` broken.
  3. Kept one genuinely independent, low-risk addition from this investigation: `ILinearGradientBrush.LinearColors` (GDI+'s `LinearGradientBrush.LinearColors`, needed by `GetSector3DBrush`'s color-extraction logic) — a real gap, harmless to add regardless of when the call sites convert.

  **Conclusion: converting `ChartGraphics`'s brush/circle/path methods requires solving image loading first** (a 4th, previously-unrecognized prerequisite alongside C4/C7) **or converting a very large, coupled block of `ChartGraphics.cs` (`FillRectangleRel`+`FillRectangleAbs`+`DrawCircleAbs`+`GetHatchBrush`+`GetGradientBrush`+`GetTextureBrush`+`GetSector3DBrush`, likely 500+ lines) plus `ImageLoader` all in one atomic pass.** This is not a small-increment task like C1-C3.

  **Attempted the image-loading prerequisite next (per explicit direction to do the full atomic conversion) — scope grew again, work paused here.** Added the additive, harmless groundwork: `IDrawingResourceFactory.LoadImage(Stream) : IChartImage`, implemented in `GdiResourceFactory` (`Image.FromStream`) and a new `SkiaChartImage`/`SkiaResourceFactory` (`SKBitmap.Decode`) — both verified building clean, nothing existing depends on them yet so this is safe to keep regardless of what happens next. But wiring it into `ImageLoader.LoadImage` (`Microsoft.Reporting.Chart.WebForms.Utilities/ImageLoader.cs`) turned out bigger than scoped: its DPI-matching helpers (`GetAdjustedImageSize`/`DoDpisMatch`/`GetScaledImage`, all typed on concrete `Image`+`Graphics`) are called from **16+ sites across 4 files** (`Axis.cs`, `ChartGraphics.cs` ×6, `ImageAnnotation.cs`, `LegendCell.cs` ×4) — not just from `GetTextureBrush` as assumed. Converting `LoadImage`'s return type ripples into all of them, and DPI itself isn't cleanly exposed on `IChartRenderingEngine` yet (same `Graphics`-property gap the spike's finding #4 already flagged for Milestone D). **Decision: paused here rather than keep expanding scope.** Three options were measured: (a) simplify by dropping GDI+'s DPI-mismatch image-rescaling special case (a print/high-DPI-only nuance) and convert all 16+ sites on that simplified basis, (b) leave the DPI-matching helpers Windows-only/concrete permanently and convert only `GetTextureBrush`'s non-DPI-rescaling common path, or (c) tackle Milestone D's `Graphics`-property gap first so DPI has a proper interface-typed home, then this converts more cleanly.

  **Decision (2026-07-18): Option (a) — assume a fixed 96 DPI baseline, drop the mismatch-rescale path entirely.** `ImageLoader`'s DPI-matching exists to compensate when a loaded image's embedded resolution (`Image.HorizontalResolution`/`VerticalResolution`) differs from the target `Graphics.DpiX`/`DpiY` — a print/high-DPI nuance, essentially never true for ordinary screen/web chart images (the vast majority are untagged or already 96 DPI, which is also the only case `IChartImage`/SkiaSharp can cheaply support: `SKBitmap` carries no resolution metadata, so faithfully porting this would require adding resolution properties to `IChartImage` — defaultable but meaningless on the Skia side — plus a resize capability, just to serve an edge case that doesn't apply to this engine's real usage). Rejected (b) because the "DPI-matching only guards `GetTextureBrush`" premise turned out false — `Axis.cs`/`ImageAnnotation.cs`/`LegendCell.cs` call `GetAdjustedImageSize` directly for unrelated image-sizing purposes, so there's no single narrow path to keep GDI+-only. Rejected (c) as solving a much larger problem (a proper interface-typed DPI/Graphics home) than this narrow simplification needs. **Accepted trade-off:** this changes shared code, so it affects Windows too, not just Linux — an image asset explicitly authored at a non-96 DPI (e.g. an old print-oriented logo) will now render at native pixel size instead of being auto-rescaled to compensate. Judged acceptable: narrow, cosmetic, and not a scenario this chart engine's real usage exercises.

### Milestone C — Migrate resource types (per type, parallelizable)

Ordered smallest-blast-radius first so patterns settle before the big ones:

> **Strategic finding after C1-C3 (revisit before sinking more time into "C4 alone"):** per-type migration cleanly works for `Region`/`Matrix` (mostly self-contained) but degrades fast for the rest, because real painter code almost never uses one resource type in isolation — `DrawStringRel(text, Font, Brush, ..., StringFormat, ...)` bundles C4+C5+C6 in a single call; `DrawPath`/`FillPath` bundle C3/C4 with C7. Treating C4-C8 as independently-closeable checkboxes undersells this — most remaining value requires converting a **whole method's** resource surface at once (Font+Brush+StringFormat+Path together), which is really **B2's job**, not six separate per-type passes. Revised plan: finish the interface/adapter groundwork for each type first (cheap — mostly already drafted in A1/A2), then do the real conversion **per ChartGraphics method** (B2) and **per painter file** rather than per-type. C4-C8's checkboxes below now track "the port + adapter exists and is complete," not "all call sites converted" — call-site conversion is B2/B1b's job once every type it needs is ready.

- [x] **C1. `Region` → `IClipRegion`** (17 occ / 6 files) 🟢 S
  **Done for the independent usage; two sites intentionally left, matching B1b's precedent.** Added `Xor`, `Transform(Matrix3x2)`, `IsEmpty`/`GetBounds` to `IClipRegion` (the latter two retargeted from a nonexistent-in-context `IRenderSurface` parameter to `IChartRenderingEngine`, since `ChartGraphics` only ever holds the live drawing context, never an owned `IRenderSurface` — that type models Milestone D's encodable output surface, a different thing). Implemented in `GdiClipRegion`/stubbed in `SkiaClipRegion`. Migrated [ChartGraphics.cs:1793-1892](../Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms/ChartGraphics.cs#L1793-L1892) (`FillRectangleShadowAbs`'s save/restore-clip block) to `IClipRegion` via `GetClipRegion()`/`SetClipRegion()`/`resourceFactory.CreateRegion()`. **Update after C2 landed:** the axis-label rotation clip (originally `ChartGraphics.cs:985-1013`) needed both C1's `IClipRegion.Transform(Matrix3x2)` and C2's `myMatrix` retyping — both now landed, so it migrated too (`resourceFactory.CreateRegion(rect)`, `region.Transform(myMatrix)`, `region.IsEmpty(this)`/`GetBounds(this)`). Two remaining `Region` sites are still genuinely blocked, not skipped, now solely on C7: [ChartGraphics.cs:2289-2290](../Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms/ChartGraphics.cs#L2289-L2290) constructs `new Region(path)` from a still-concrete `GraphicsPath` (needs **C7**); [Axis.cs:1172-1181](../Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms/Axis.cs#L1172-L1181) constructs a region from `GetPolygonCirclePath(...)` (concrete `GraphicsPath`, needs **C7**) — its Transform usage was migrated separately as part of C2. `ChartImage.cs:101`'s `Region` use is in the Windows-only EMF/metafile export path (separate from `IChartRenderingEngine` entirely) — out of scope by design, not a gap. Verified: build 0 errors, all 5 visual regression + spike tests pass, baselines unchanged (zero behavior change on Windows).
- [x] **C2. `Matrix` → `ITransform`** (58 / 11) — use `System.Numerics.Matrix3x2` (Appendix A.6: identity-only ctor, 5 ops); verify rotation direction + transform-order parity with GDI+. 🟡 M
  **Done for all `Matrix` usage independent of `GraphicsPath.Transform`; the rest bridges via a helper pending C7.** Before touching any call site, verified GDI+'s default `MatrixOrder.Prepend` composition (`RotateAt`/`Translate`) point-for-point against `Matrix3x2` (`rot * matrix`, `Vector2.Transform`) with a standalone scratch console app across several angles/centers/offsets — exact match. Added [`Rendering/Matrix3x2Extensions.cs`](../Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms/Rendering/Matrix3x2Extensions.cs): `RotateAt`/`Translate`/`Scale`/`TransformPoints` (mirroring GDI+'s `Matrix` API shape) plus a `ToGdiMatrix()` bridge for call sites still feeding a concrete `GraphicsPath`/`Region`'s own `.Transform(Matrix)` overload. Migrated `ChartGraphics.cs` (`myMatrix` field retyped to `Matrix3x2`, driven by `GetTransform()`/`SetTransform()` instead of the legacy `Transform` property), `Axis.cs`, `Title.cs`, `Label.cs`, `ChartArea.cs` — all `base.Transform`/local-`Matrix` bookkeeping now uses the interface-typed pair; the few `GraphicsPath.Transform(matrix)` sites use `.ToGdiMatrix()` as a documented, temporary bridge. **Remaining concrete `Matrix` usage (26 occ / genuinely blocked, not skipped):** `ArrowAnnotation.cs`, `AxisScaleSegment.cs`, `CalloutAnnotation.cs` (4 sites) all rotate/translate a matrix solely to feed a concrete `GraphicsPath.Transform(Matrix)` — needs **C7** first, since bridging every remaining site individually would just be C7's work done piecemeal. Added a new visual-regression test (`RotatedLabelsChart_MatchesBaseline`, baseline generated from the pre-migration code via a temporary `git stash`) that exercises rotated axis labels and a rotated title — the two paths this migration touches most — and it passes byte-for-byte against the pre-migration render, on top of the existing 4 tests. Build: 0 errors on both passes.
- [x] **C3. `Pen` → `IPen`** (127 / 16) — ~5 properties, `(color, width)` ctor (Appendix A.4). 🟠 L
  **Done for the independent usage only — this milestone turned out far more entangled with C7 than its L sizing assumed.** Added the missing `IPen.Alignment` property (Appendix A.4 undercounted it — the doc's own ⚠ caveat on ambiguous member counts held here) and exposed `ChartGraphics.ResourceFactory` (internal, read-only) so the ~70 painter classes outside `ChartGraphics` can construct `IPen` too. Migrated the fully-independent sites (`AxisScrollBar.cs` — now 0 `Pen` occurrences; `SmartLabels.cs`'s `DrawRectangle`/`DrawLine` path; three local `Pen`s in `ChartGraphics.cs` that only ever call `DrawRectangle`/`DrawLine`/`DrawLines`). **Two new blocking discoveries, not just the expected `DrawPath`/`GraphicsPath` coupling:** (1) `ChartGraphics.Widen(GraphicsPath, Pen)` — a thin wrapper over GDI+'s `GraphicsPath.Widen(Pen)` — is called from `CalloutAnnotation.cs`, `LineAnnotation.cs`, `PolylineAnnotation.cs`, `TextAnnotation.cs`, `Title.cs`, and `Axis.cs`, and is inherently GDI+-only (stroke-to-fill geometry) with no direct `IGraphicsPath` equivalent — this is a **new risk**, not merely a C7 dependency (see §7 risk table). (2) `ChartGraphics3D.cs`'s `frontLinePen` field and its two big polygon-drawing methods looked independent (`DrawLine`/`DrawPolygon` only) until a `ChartGraphics.Widen(graphicsPath, pen3)` call turned up 40 lines further into the second method — reverted that attempt (confirmed via `git diff --stat` showing zero net change) rather than leave a half-converted, silently-wrong field. **Remaining 116 occurrences across 12 files are genuinely blocked**, either on C7 (`DrawPath`/`DrawCircleAbs`/`Widen`) or on the `AdjustableArrowCap`/`CustomStartCap` gap (`SmartLabels.cs`, one site — reverted, no cross-platform equivalent exists for custom arrow caps). Verified: build 0 errors, all 5 tests pass, baselines unchanged.
- [x] **C4. Brushes → `IBrush` family** (~200 / ~16) — `ISolidBrush` first (~90% of usage); Hatch/Gradient/Texture follow (Appendix A.5). 🟠 L
  **Port + adapter complete** (per the strategic finding above — call-site conversion moved to B2). Found and fixed a real gap: `PathGradientBrush` (16 occ / 2 files, all in shadow-rendering) was **missing entirely** from the original Appendix A.5 inventory. Added `IPathGradientBrush` (`CenterColor`/`SurroundColors`/`CenterPoint`/`FocusScales`) + `GdiPathGradientBrush` + `IDrawingResourceFactory.CreatePathGradientBrush(IGraphicsPath)` (Skia side stubbed, spike scope). `ISolidBrush`/`ILinearGradientBrush`/`ITextureBrush`/`IHatchBrush` were already complete from A1/A2. Confirmed via grep that most real call sites (`DrawStringRel`, `FillPath`) bundle `Brush` with `Font`/`StringFormat`/`GraphicsPath` in the same call — not independently convertible, hence the strategy pivot.
- [x] **C5. `StringFormat` → `ITextFormat`** (145 / 19) — contract is tiny (4 members + 2 presets, Appendix A.3); L is purely file spread. 🟠 L
  **Port + adapter complete** (`ITextFormat`/`GdiTextFormat`, drafted in A1/A2, verified complete — no gaps found). Call-site conversion deferred to B2.
- [x] **C6. `Font` → `IChartFont`** (232 / 31) — most spread; small API (Appendix A.2). Route text **measurement** through `IChartRenderingEngine.MeasureString`, not the font. 🔴 XL
  **Port + adapter complete** (`IChartFont`/`GdiChartFont`, drafted in A1/A2, verified complete). Call-site conversion deferred to B2.
- [x] **C7. `GraphicsPath` → `IGraphicsPath`** (290 / 24) — largest; full method set enumerated in Appendix A.1 (~24 methods, no hidden surface). 🔴 XL
  **Port + adapter complete** (`IGraphicsPath`/`GdiGraphicsPath`, drafted in A1/A2 — all ~24 methods implemented, not just the spike-scoped subset `SkiaGraphicsPath` has). `Widen(IPen)` is on the interface already; the Skia side of that (stroke-to-fill geometry) is real future work, not an interface gap (see §7 risk table). Call-site conversion deferred to B2 — this was going to be the true bottleneck for per-type conversion (`DrawPath`/`FillPath` touch `GraphicsPath` together with `Pen`/`Brush` at nearly every real site).
- [x] **C8. `ImageAttributes` → `IImageDrawOptions`** (25 / 6); coordinate with the existing `IImageProvider` background-image work in `chart-image-abstraction-analysis.md`. 🟡 M
  **Port + adapter complete** (`IImageDrawOptions`/`GdiImageDrawOptions`, drafted in A1/A2, verified complete). Call-site conversion deferred to B2.

### Milestone D — Surface boundary & backend selection

- [ ] **D1. `IRenderSurfaceFactory`** replacing `new Bitmap` + `Graphics.FromImage` ([ChartPicture.cs:733-734](../Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms/ChartPicture.cs#L733-L734)) and encode at [Chart.cs:1313](../Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms/Chart.cs#L1313). Guard the EMF/`Metafile` path ([Chart.cs:1281-1293](../Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms/Chart.cs#L1281-L1293)) as Windows-only. 🟠 L
- [ ] **D2. Backend factory selection by platform** (Windows → GDI+ adapters, else → Skia), mirroring the Excel `ImageProviderFactory` pattern. 🟢 S
- [ ] **D3. Remove the temporary `System.Drawing`-typed `IChartRenderingEngine` overloads** from A3 once all callers use interfaces. 🟡 M

### Milestone E — Skia adapter & verification

- [x] **E0. Visual regression harness** (pixel-baseline scaffolding, ahead of the Skia work it will eventually verify). 🟢 S
  **Done** in [`tests/Microsoft.ReportViewer.DataVisualization.VisualRegressionTests/`](../tests/Microsoft.ReportViewer.DataVisualization.VisualRegressionTests/) — MSTest project with `InternalsVisibleTo` access into `Microsoft.ReportViewer.DataVisualization` (new entry added to [AssemblyInfo.cs](../Microsoft.ReportViewer.DataVisualization/Properties/AssemblyInfo.cs#L17), signed with the shared `ReportViewerCore.snk`). `SampleCharts.cs` builds a `Chart` directly against the internal engine API (no `.rdlc`/host needed) for a simple bar chart and line chart; `ImageComparer.cs` does a per-pixel compare (tolerance 2/channel, for encoder rounding only — rendering is otherwise deterministic, confirmed via repeated runs) against a committed PNG in `Baselines/`, writing the actual output and a red/transparent diff PNG to `Results/` (gitignored, under `bin/`) on any mismatch or missing baseline. Two baselines committed (`SimpleBarChart.png`, `SimpleLineChart.png`); both currently pass. This is the mechanism Phase 0's "render one simple bar chart, compare pixels" step and Milestone E2 will both run through — E2 still needs the Skia adapter (E1) and a broader report corpus; this only proves the harness/baseline mechanics work today, against GDI+ output only.
- [ ] **E1. Implement the Skia adapter set** (`SkiaResourceFactory`, `SkiaPen/Brush/Font/Path/…` over `SKPaint`/`SKPath`/`SKFont`). 🔴 XL
  **Phase 0 spike done** (see `chart-cross-platform-implementation.md` Spike Report, 2026-07-18) — a partial, spike-scoped `Rendering/Skia/` adapter set + [`SkiaChartGraphics`](../Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms/SkiaChartGraphics.cs) already exist and render a hand-built scene correctly on both Windows and Linux, proving the port design works. **This milestone is not shortened by that, though** — the spike also found GDI+ can't even construct on Linux today (any `System.Drawing` object, not just via the rendering seam), which means **B1b/B2/C1-C8 must be substantially complete before E1's adapters are reachable from a real `Chart`** (today `ChartGraphics` still calls the GDI+-typed `IChartRenderingEngine` overloads exclusively, which the spike's `SkiaChartGraphics` deliberately leaves as unreachable stubs). Treat B1b/B2/C1-C8 as blocking prerequisites for E1, not parallel/independent work.
- [ ] **E2. Visual regression** across a report corpus (Windows GDI+ vs Linux Skia), per Phase 6 of the parent plan. Extend the E0 harness with more chart types/gauges and a second (Skia) render path once E1 lands. 🟠 L

---

## 6. Sequencing & dependencies

```
A1 → A2 → A3 → B1 → B2 → (C1…C8 in parallel) → D1 → D2 → D3 → E1 → E2
                                   │
        C6 (Font) also unblocks cross-platform text measurement
        C7 (GraphicsPath) is the critical-path long pole
```

- **Milestones A–D deliver a fully GDI+-backed engine that still behaves identically on Windows** but is now expressed in interfaces. That's a safe, mergeable state with zero cross-platform code yet.
- **Milestone E** adds the first non-GDI+ backend. The design is validated only when E2 passes.

---

## 7. Risks

| Risk | Impact | Mitigation |
|------|--------|-----------|
| **Text metrics differ** (GDI+ `MeasureString` vs Skia `SKFont`) — C6 | Layout drift | Route measurement through `IChartRenderingEngine.MeasureString`; tolerance-based visual diffs |
| **`GraphicsPath` surface is large** — C7 | Long pole | ✅ Surface now fully inventoried (Appendix A.1): ~24 methods, no hidden API. Risk downgraded to "large but bounded." |
| **`Matrix` semantics** (order, transform direction) — C2 | Subtle geometry bugs | Unit-test transform parity before/after |
| **Big-bang signature change** on `IChartRenderingEngine` | Compilation churn | A3 keeps old overloads; remove only in D3 |
| **Gauge engine is separate** | Charts fixed, gauges still GDI+ | Repeat pattern for `GaugeContainer` (parent plan Phase 5) |
| **Effort genuinely large** (C6+C7 ≈ 520 occ / 40+ files) | Schedule | Phase 0 spike sizes C7/C6 against one real Skia primitive before committing |
| **GDI+ cannot construct on Linux at all** (any `System.Drawing` object, not just via rendering) — confirmed in the Phase 0 spike, holds even with `libgdiplus` installed | E1 can't be exercised by a real `Chart` until B1b/B2/C1-C8 are done, not just started | Treat B1b/B2/C1-C8 as a hard blocking prerequisite for E1 in scheduling, not parallelizable "nice architecture" work |
| **`GraphicsPath.Widen(Pen)` has no cross-platform equivalent** — discovered during C3; used by `ChartGraphics.Widen` (called from 6 files for hot-region hit-testing) | Stroke-to-fill geometry is GDI+-only; C7's `IGraphicsPath` can't just add a passthrough | Needs its own decision during C7 — likely a hand-rolled stroke-outline algorithm behind `IGraphicsPath`, or accept hit-testing precision loss on the Skia backend |
| **`Pen.CustomStartCap`/`AdjustableArrowCap` has no cross-platform equivalent** — discovered during C3, one site (`SmartLabels.cs`) | That one site (custom arrow-shaped smart-label callout line caps) can't move off concrete `Pen` | Low priority — single site, cosmetic; leave GDI+-only (Windows-only feature) or approximate with a fixed `LineCap` on the Skia backend later |

---

## 8. Open questions for the spike (Phase 0)

1. Interfaces vs descriptor records for Pen/Brush/Font/StringFormat (§3) — decide against one real Skia implementation.
2. ~~`System.Numerics.Matrix3x2` sufficient for `ITransform`?~~ — **Yes** (Appendix A.6). Remaining check: rotation direction / transform-order parity with GDI+.
3. ~~Exact `GraphicsPath` method surface actually used~~ — **inventoried** (Appendix A.1).
4. Can the GDI+ adapters be introduced with **zero** rendered-output change on Windows? (Must be yes before merging Milestones A–D.)
5. **Text-metric parity** (GDI+ `MeasureString` vs `SKFont`) — the one unknown only a running prototype can settle.

> **Port surface is now fully specified.** Appendix A enumerates every member of all resource interfaces (`IGraphicsPath`, `IChartFont`, `ITextFormat`, `IPen`, `IBrush` family, `ITransform`). The `IDrawingResourceFactory` can be authored directly from it. The remaining spike work is behavioral parity (#4, #5), not API discovery.

---

## Appendix A — Measured member surface (C5/C6/C7)

Counts from `grep -rohE` across the Chart engine. **Caveat:** counts for generic member names (`.Name`, `.Size`, `.Height`, `.Clone`, `.Dispose`, `.Style`) are **inflated by ambiguity** — they match many types, not just the one in question. Those are marked ⚠️ and should be treated as "used, count unreliable." The distinctive members and constructor patterns are reliable and are what drive the interface shape.

### A.1 `IGraphicsPath` (C7) — the surface is bounded and fully enumerable

Constructors: `new GraphicsPath()` ×92, `new GraphicsPath(points, types)` ×4.

| Member | Hits | Member | Hits | Member | Hits |
|--------|------|--------|------|--------|------|
| `AddLine` | 114 | `AddArc` | 29 | `CloseAllFigures` | 31 |
| `AddBezier` | 36 | `AddEllipse` | 15 | `IsVisible` ⚠️ | 26 |
| `AddLines` | 13 | `AddPolygon` | 12 | `Transform(matrix)` ⚠️ | 16 |
| `AddRectangle` | 11 | `AddPath` | 10 | `GetBounds` ⚠️ | 16 |
| `AddCurve` | 2 | `AddClosedCurve` | 2 | `Reset` ⚠️ | 16 |
| `AddPie` | 2 | `AddString` | 2 | `Reverse` | 10 |
| `StartFigure` | 9 | `CloseFigure` | 4 | `SetMarkers` | 6 |
| `Flatten` | 8 | `Widen` | 8 | | |
| **props:** `PathPoints` | 13 | `PointCount` | 13 | `PathTypes` | 3 |

→ **~24 methods + 3 properties + 2 constructors.** Large but finite. This retires the "hidden method surface" risk from §7: `IGraphicsPath` can be specified completely up front.

### A.2 `IChartFont` (C6) — small distinct surface, wide spread (31 files)

Reliable constructor patterns to support on the factory:
- `Create(familyName, sizeInPoints)` — most common (~19 sites)
- `Create(familyName, size, FontStyle)`
- `Create(familyName, size, FontStyle, GraphicsUnit)`
- `Derive(existingFont, newStyle)` and `Derive(existingFont.FontFamily, newSize, style, unit)` — re-style/re-size an existing font

Reliable members: `FontFamily` (16), `SizeInPoints` (11), `Unit` (3), `Style` ⚠️(42), `Name` ⚠️(218), `Size` ⚠️(107).
**Text measurement is NOT on the font** — it flows through `IChartRenderingEngine.MeasureString` (route to `SKFont` in the Skia adapter). The XL sizing is from the 31-file spread, not API complexity.

### A.3 `ITextFormat` (C5) — cleanest of the three

Constructors: `new StringFormat()` ×19, `StringFormat.GenericTypographic` preset ×3, `StringFormat.GenericDefault` preset ×1.
Members: `Alignment` (95), `LineAlignment` (60), `FormatFlags` (38), `Trimming` (11).
→ **4 members + 2 presets.** L sizing is purely the 19-file spread; the contract is tiny and can be a `readonly record struct` behind the interface.

### A.4 `IPen` (C3) — small clean surface, driven by spread

Constructor: overwhelmingly `new Pen(color, width)` (a few via `Color.FromArgb`). No brush-based pens.
Reliable distinctive members: `DashStyle` (18), `StartCap` (33), `EndCap` (23), `LineJoin` (2). Ambiguous ⚠: `Width` (1155 — mostly Rectangle/element widths, **not** `Pen.Width`), `Color` (47), `Alignment` (95 — mostly `StringFormat`).
Not used (0 hits): `DashPattern`, `DashCap`, `MiterLimit`, `DashOffset`, `Pen.Brush`.
→ **~5 properties + 1 constructor.** L sizing is the 16-file spread, not API size.

### A.5 `IBrush` family (C4) — `SolidBrush` dominates

Construction counts: `SolidBrush` ×68, `LinearGradientBrush` ×7, `TextureBrush` ×3, `HatchBrush` ×1.
- `ISolidBrush` — `new SolidBrush(color)`; **~90% of all brush usage.**
- `IGradientBrush` — plus `Blend` (2), `InterpolationColors` (3), `WrapMode` (2).
- `ITextureBrush` — image fill + `WrapMode`.
- `IHatchBrush` — style + fore/back color (rare; 1 construction site).
→ Prioritize `ISolidBrush`; the other three are low-frequency and can follow.

### A.6 `ITransform` (C2) — confirms `System.Numerics.Matrix3x2` is viable

Construction: **only** `new Matrix()` (identity) ×14 — the 6-float affine constructor is **not used**.
Members: `TransformPoints` (46, the workhorse), `RotateAt` (22), `Translate` (14), `Reset` ⚠(16, shared with `GraphicsPath.Reset`), `Scale` (2). Not used: `Multiply`, `Invert`, `TransformVectors`, `Elements`, `Shear`.
→ Tiny op set. `System.Numerics.Matrix3x2` covers all of it: `CreateTranslation`/`CreateScale`/`CreateRotation` + `Vector2.Transform` for points; `RotateAt` composes as translate→rotate→translate. **Spike question #2 is effectively answered: yes.** Only caveat to verify — rotation direction / append-vs-prepend order parity with GDI+.
