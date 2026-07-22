# Gauge engine: GDI+ type abstraction — Progress

Sibling effort to [`chart-gdi-type-abstraction.md`](chart-gdi-type-abstraction.md), applying the same Ports & Adapters pattern to `Microsoft.Reporting.Gauge.WebForms` (~30,000 lines / 253 files, never previously touched by the Chart migration).

**Design, namespaces, and recurring patterns:** [`docs/rendering-abstractions.md`](../docs/rendering-abstractions.md)
**Decisions:** [`docs/decisions.md`](../docs/decisions.md)
**Known gaps:** [`docs/platform-support.md`](../docs/platform-support.md)

Scoping found the Gauge engine already has a pre-Milestone-A-equivalent seam (`IGaugeRenderingEngine`/`RenderingEngine`/`GdiGraphics`) and a smaller, more concentrated GDI+ surface than Chart's (~249 construction sites/~30 files vs. Chart's ~1000/~24) — critically, zero `GraphicsPath.Widen`/`GraphicsPathIterator`/custom-arrow-cap sites, so several of Chart's trickiest gaps don't recur here.

## Milestone status

| Milestone | Status | Summary |
|---|---|---|
| Scoping + design decision | Done | Inventoried GDI+ sites; chose to relocate Chart's pure resource interfaces into the shared `Microsoft.Reporting.Rendering` namespace rather than duplicate them (see `docs/decisions.md`); Gauge gets its own `IGaugeDrawingResourceFactory` + decoupled `Rendering/Gdi/` adapters. |
| A1 — shared interfaces | Done | Satisfied by reuse/relocation; no new Gauge-specific resource interfaces needed. |
| A2 — Gdi adapter set | Done | `Rendering/Gdi/` adapters + `GdiResourceFactory`, mirrors Chart's A2 shape as a separate implementation. |
| A3 — interface-typed engine overloads | Done | ~21 dual overloads added to `IGaugeRenderingEngine`/`GdiGraphics`/`RenderingEngine`; purely additive. |
| A4 — clip-region abstraction | Done | `IGaugeClipRegion`/`GdiClipRegion` built (Gauge-owned, not shared with Chart's `IClipRegion`); real call sites (`DrawPathAbs`, `BackFrame.DrawFrameImage`) converted. `GaugeCore.Paint`/`SaveTo` deliberately left — blocked on `BufferBitmap` (see Open items). |
| B1 — inject resource factory | Done | Free via `GaugeGraphics : RenderingEngine` inheritance — no code change needed. |
| B2 — real call-site conversion | **Substantially done, a few named gaps remain** | File-by-file conversion of every `DrawImage`-shaped method and every self-contained brush/pen/path getter across `GaugeGraphics.cs`, `BackFrame.cs`, `Knob.cs`, `GaugeCore.cs`, `StateIndicator.cs`, `CircularGauge.cs`/`LinearGauge.cs`, `NumericIndicator.cs`, `GaugeLabel.cs`, `GaugeImage.cs`, `ScaleBase.cs`. Closed several genuine capability gaps along the way: `IImageDrawOptions.SetChannelScale`, brush rotation/translation transforms, `IPen.DashPattern`, the `GetTextureBrush`/`ImageLoader` prerequisite (see `docs/decisions.md`). |
| B3 — atomic rewrite of shared attrib classes | Done (2026-07-22) | `KnobStyleAttrib`/`NeedleStyleAttrib`/`MarkerStyleAttrib`/`BarStyleAttrib` fully retyped to `IGraphicsPath`/`IBrush` across `Knob.cs`/`CircularPointer.cs`/`LinearPointer.cs`, producers and consumers converted end-to-end. Added `IGraphicsPath.GetBounds(Matrix3x2)`/`Flatten(float)` and the `UnwrapPath` bridge to unblock it. Verified: build 0 errors, 55/55 tests, zero baseline diffs — genuinely pixel-verified since `CircularPointer`'s default type (`Needle`) is exercised by both existing sample gauges. |
| E0 — visual regression harness | Done | `SampleGauges.cs`/`GaugeVisualRegressionTests.cs` built from scratch (no prior Gauge coverage existed); 3 sample gauges (simple circular, simple linear, circular with frame image). |

## Open items (detail needed to resume)

1. **`HotRegionList.SetHotRegion`/`AddHotRegion` + `GaugeGraphics.DrawRadialSelection`** — systemic, concrete-only GDI+ hit-testing infrastructure used by every gauge element; no `IGraphicsPath`-typed overload exists anywhere in the solution (confirmed by solution-wide grep). Blocks `CircularScale.GetBarPath`/`SetScaleHitTestPath`/`GetSelectionMarkers`/`ISelectable.DrawSelection` and `NumericIndicator.RenderStaticElements` even though their own path-building is otherwise self-contained. B3 bridged around this with `IGaugeDrawingResourceFactory.UnwrapPath(IGraphicsPath):GraphicsPath` rather than converting it. Treat as its own future single-fix-many-sites milestone.

2. **`XamlRenderer.cs`/`XamlLayer.cs`** — architecturally blocked, not just undone. Needs three new primitives before any conversion is possible: a `ColorBlend`-equivalent gradient factory (arbitrary multi-stop gradients, no interface equivalent today), scale/shear-capable transforms (only rotate/translate are covered), and a way to build `IGraphicsPath`/`IBrush`/`IPen` without a live `GaugeGraphics` instance (its geometry-parsing methods run before any engine instance exists).

3. **`CircularScale.DrawLabel`** — needs `Font`→`IChartFont`/`StringFormat`→`ITextFormat` conversion to reach the existing `DrawString(string, IChartFont, IBrush, RectangleF, ITextFormat)` overload; no mixed `DrawString(Font, IBrush, ..., StringFormat)` overload exists.

4. **`ScaleBase.GetLightBrush`'s `Circle` branch** — needs a `Blend` property on `IPathGradientBrush` (GDI+'s `PathGradientBrush.Blend`); `ILinearGradientBrush` already has `Blend` but `IPathGradientBrush` doesn't. Deliberately not given a partial sibling covering only the linear-gradient branch — would silently misrender the `Circle` case if reached.

5. **`CircularScale.GetCompoundPath`** — confirmed dead code (zero callers solution-wide); flagged for a future dead-code pass, not yet removed.

6. **`GaugeCore.Paint`/`PrintPaint`/`SaveTo`/`GetGraphics`** — operate directly on a raw `System.Drawing.Graphics` (caller-supplied or `BufferBitmap.Graphics`) with no `GaugeGraphics`/`IGaugeRenderingEngine` wrapper in scope. Blocked on `BufferBitmap` having no `IRenderSurface`-equivalent (the Gauge analogue of Chart's own earlier `IRenderSurface`/`GdiRenderSurface` milestone). Also unresolved: whether `GaugeCore.GetGraphics(renderingType, g, stream)` (`GaugeCore.cs:1447`, the Gauge analogue of Chart's `ChartRenderingEngine.RenderingObject`) has the same hard GDI+-typed-return blocker Chart's D2 investigation found — not yet checked.

7. **`DigitalSegment.cs`/`SegmentsCache.cs`** — pure geometry, but a `static` utility class with no `GaugeGraphics` instance and hence no `ResourceFactory` to reach. Its only consumer (`NumericIndicator.DrawSymbol`) feeds every returned `GraphicsPath` into `FillPath(Brush, GraphicsPath)` with a concrete `Brush` — same "large atomic pass" shape as the attrib classes were before B3; not attempted piecemeal.

8. **`GetCircularRangeBrush`/`GetLinearRangeBrush`** — real, still-concrete callers remain in `CircularRange.cs`/`LinearRange.cs` (distinct from the `CircularPointer`/`LinearPointer` attrib classes B3 converted). Untouched, explicitly out of scope for B3.

9. **`CircularScale.GetSelectionMarkers`** — needs the tolerance-based `Flatten` call, which now exists (`IGraphicsPath.Flatten(float)`, added during B3). Still unconverted only because it ultimately feeds `HotRegionList` (item 1) — re-check once that closes.
