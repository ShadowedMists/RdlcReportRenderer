# Chart Engine: GDI+ Type Abstraction — Progress

**Parent plan:** [`chart-cross-platform-implementation.md`](chart-cross-platform-implementation.md)
**Design, namespaces, and recurring patterns:** [`docs/rendering-abstractions.md`](../docs/rendering-abstractions.md)
**Decisions:** [`docs/decisions.md`](../docs/decisions.md)
**Known gaps:** [`docs/platform-support.md`](../docs/platform-support.md)

**Goal:** Replace concrete GDI+ resource types (`Pen`, `Brush`, `Font`, `GraphicsPath`, …) with backend-agnostic interfaces + an abstract factory, so the chart engine can run on GDI+ (Windows) and SkiaSharp (Linux/macOS).

## Milestone status

| Milestone | Status | Summary |
|---|---|---|
| A1 — port interfaces | Done | Drafted in `Rendering/`; pure interfaces later relocated to shared `Microsoft.Reporting.Rendering` for Gauge reuse. `IClipRegion` stayed Chart-specific (Chart-specific dependency). |
| A2 — Gdi adapter set | Done | One adapter class per resource interface, behavior-identical to existing GDI+ calls. |
| A3 — interface-typed engine overloads | Done | ~30 dual overloads added to `IChartRenderingEngine`, coexisting with the original concrete overloads. |
| B1a — inject resource factory into `ChartGraphics` | Done | Constructor field, defaults to `GdiResourceFactory`. |
| B1b — retype shared `pen`/`solidBrush` fields | **Superseded, not done** | See Open items. |
| B2 — real call-site conversion | **Mostly done** | Nearly every `ChartTypes/*.cs`, annotation, axis, label, legend, title, and strip-line file converted via the dual-overload/bridge-at-sink pattern. A short, precise list of holdouts remains — see Open items. |
| C1 `Region`→`IClipRegion` | Done | Port + adapters + real callers converted. |
| C2 `Matrix`→`Matrix3x2` | Done | Port + real callers converted; a few sites still feed concrete `GraphicsPath.Transform`, tracked under C7. |
| C3 `Pen`→`IPen` | Done | One permanent holdout: `AdjustableArrowCap` (see `docs/platform-support.md`). |
| C4 `Brush` family→`IBrush` | Done | Includes `IPathGradientBrush`. |
| C5 `StringFormat`→`ITextFormat` | Done | |
| C6 `Font`→`IChartFont` | Done | Public model properties (`Series.Font`, etc.) intentionally stay concrete by design. |
| C7 `GraphicsPath`→`IGraphicsPath` | Done | `GraphicsPathIterator` has no equivalent — replaced by a hand-rolled `SplitAtMarkers` substitute in `CalloutAnnotation.cs`. |
| C8 `ImageAttributes`→`IImageDrawOptions` | Done | |
| D1 `IRenderSurfaceFactory` | Done (narrowed) | Metafile/EMF path deliberately untouched (Windows-only). Top-level `Paint(Graphics)` entry point still concrete — that's D2/E1. |
| D2 — backend selection by platform | **In progress** | `Paint(Graphics,...)`'s chain now has an `IRenderSurface`-typed sibling (`Paint(IRenderSurface,...)`, dual-overload with the Graphics-typed one kept only for the EMF holdout); all 4 real production callers converted. ~45 of ~49 real-blocker call sites now converted. See Open items for the small remainder. |
| D3 — remove temporary concrete overloads | **Not done, larger than estimated** | See Open items. |
| E0 — visual regression harness | Done | MSTest project, pixel-baseline compare (`ImageComparer`), ~52 tests. |
| E1 — Skia adapter set | **Spike only** | Validates the design (a hand-built scene renders identically on Windows and Linux) but isn't wired to a real `Chart` object. Blocked on B1b/B2/C1-C8 substantially completing and on D2's finding below. |
| E2 — cross-platform visual regression | **Not done** | Depends on E1. |

## Open items (detail needed to resume)

**B1b / shared `pen`/`solidBrush` fields on `ChartGraphics`.** Deliberately not retyped — superseded by giving new conversions their own local `IPen`/`IBrush` via `resourceFactory` instead (the pattern used throughout B2). `pen` feeds `DrawPath(pen, path)` (`ChartGraphics.cs:2295`, no mixed `DrawPath(IPen, GraphicsPath)` overload by design); `solidBrush` is interchanged with `GetGradientBrush`/`GetHatchBrush`/`GetTextureBrush` results in one `Brush brush` local (`ChartGraphics.cs:2052-2063`, `2202-2224`). If ever revisited: `DrawPathAbs` (`ChartGraphics.cs:2175`) has 6 real callers (`PolylineAnnotation.cs`, `CalloutAnnotation.cs`, `ArrowAnnotation.cs`, `FunnelChart.cs`, `Borders3D/SunkenBorder.cs`, `Borders3D/EmbossBorder.cs`) that would ripple.

**B2 holdouts.** `FunnelChart.cs`'s `DrawFunnelCircularSegment` (3D branch) builds a `graphicsPath` via a concrete-only `AddEllipseSegment` helper consumed directly by real `System.Drawing.Graphics.FillPath/DrawPath`, not `IChartRenderingEngine` — stays concrete. `ChartGraphics3D.cs`'s `Draw3DSurface`/`Draw3DPolygon` had their `frontLinePen` field entanglement resolved via a narrow `BridgeFrontLinePen` helper, but the rest of both method bodies remain concrete by design (converting further wasn't judged worth it once the one real consumer was bridged). Map engine (`Microsoft.Reporting.Map.WebForms`) and Gauge engine each have their own separate, parallel GDI+ pipeline — out of scope here; Gauge tracked in `gauge-gdi-type-abstraction.md`, Map's scope is an open decision.

**D2 — backend selection.** The naive fix (swap `ChartRenderingEngine.RenderingObject`'s backend by `RuntimeInformation.IsOSPlatform`) would be reachable but pointless: the real production entry point, `ChartImage.GetImage(float) : Bitmap`, unconditionally downcasts to `GdiRenderSurface` and returns a hard GDI+-typed `Bitmap` *before* `RenderingObject` is ever consulted — throws on Linux regardless of backend selection, and `GetImage`'s own return type is a public API contract a Skia backend still couldn't satisfy (`Bitmap` construction itself fails on Linux per the Phase 0 spike). One independent, safe fix landed alongside this investigation: `Chart.Save(Stream, ChartImageFormat)` now goes through `ChartImage.SaveImage(Stream, ChartImageFormat, float)`, which calls the previously-unused `IRenderSurface.Encode(Stream, ChartImageFormat)` instead of a raw `Bitmap.Save`. **Real blocker for D2/E1:** `Paint(Graphics)`/`ChartGraphics.Graphics` must stop being GDI+-typed before backend selection has anything real to select between.

*Scoping pass (2026-07-22) found the blocker's real call-site count much smaller than the "~70 painter files" framing implied — B2 already did the heavy lifting. Confirmed ~49 total sites across 14 files reading `.Graphics` off `ChartGraphics`/`IChartRenderingEngine` and calling raw `System.Drawing.Graphics` members directly. Converted so far (~41 sites):*
- *`ImageLoader.GetAdjustedImageSize(Image, Graphics, ref SizeF)`'s `Graphics` parameter was already dead (unused since the 96-DPI-baseline decision) — dropped from both overloads' signatures; fixed all 28 call sites across `Axis.cs`, `ChartGraphics.cs`, `ImageAnnotation.cs`, `LegendCell.cs`, and `ChartTypes/{BarChart,BoxPlotChart,ErrorBarChart,PointChart,RadarChart,StockChart}.cs`.*
- *Added `float GetDpiX()` and `CompositingQuality`/`InterpolationMode` properties to `IChartRenderingEngine` (same pattern as the existing `SmoothingMode` property — GDI+-enum-typed by design, backed by the live `Graphics` on Gdi/Svg, `NotReachable` stub on the Skia spike). Converted the 6 remaining `.Graphics.DpiX` reads (`ChartGraphics.cs`, `ChartPicture.cs`, `LegendCell.cs`×3, `LegendItem.cs`) and `LegendCell.cs`'s 6-site `CompositingQuality`/`InterpolationMode` save-restore block.*

*`Paint` chain conversion (2026-07-22, same session): added `IChartRenderingEngine.BindSurface(IRenderSurface)` — each backend downcasts internally to its own concrete surface type (`GdiGraphics`/`SvgChartGraphics` both do `((GdiRenderSurface)surface).NativeGraphics`; `SkiaChartGraphics` stubs `NotReachable`, consistent with its other unimplemented members), so the downcast moved from 4 call sites into the adapters that are allowed to know their own concrete types. `ChartPicture.Paint`'s 2-arg and 8-arg overloads got `IRenderSurface`-typed siblings (dual-overload, per this doc's own convention) that call `chartGraph.BindSurface(...)` instead of `chartGraph.Graphics = ...`; the original `Graphics`-typed overloads are kept permanently (not deprecated) since `ChartImage.SaveIntoMetafile` genuinely needs to hand in a Metafile-backed `Graphics` with no `IRenderSurface` equivalent. Both overload pairs delegate into a shared private `PaintCore(..., Action bindGraph)` to avoid duplicating the ~300-line body — `bindGraph()` must run at the exact point it did before (after the SVG `Open(...)` call, which swaps in a fresh `SvgChartGraphics`, and before anything else touches the surface). Converted all 4 real callers: `ChartImage.GetImage`, `ChartImage.SaveImage`, `ChartImage.GetSvgImage`, `ChartPicture.Select` now pass their `IRenderSurface` straight through instead of downcasting to pull out `.NativeGraphics`. Also fixed one bonus site found in `Paint`'s own body (`chartGraph.Graphics.SmoothingMode` → `chartGraph.SmoothingMode`).*

*Remaining ~4-site tail (deliberately not touched — genuinely separate from the `Paint` chain):*
- **`SelectionManager.Annotation.DrawSelection(Graphics g)`** (`Utilities/SelectionManager.cs`) — reassigns `graph.Graphics = g` redundantly (it's already set from the active `Paint` call, which now uses `BindSurface`/the Graphics-typed overload depending on caller); harmless since `Graphics` is still a valid property on the interface, but could be simplified away in a future pass since the reassignment is a no-op.
- **`TextAnnotation.GetContentPosition`**'s standalone `new Bitmap(...)` + `Graphics.FromImage(...)` construction (for measuring annotation content size before any chart-attached `ChartGraphics` exists) — a genuine second GDI+-only construction site independent of the `Paint` chain. Needs its own fix (route through `IRenderSurfaceFactory` like `GetSvgImage` does, then `BindSurface`) before it stops being Windows-only.
- **`FunnelChart.DrawFunnelCircularSegment`** (3D branch) and **`ChartImage.SaveIntoMetafile`** (EMF/HDC construction, now the sole caller of the retained `Graphics`-typed `Paint` overloads) remain the two already-documented permanent holdouts (B2 holdouts / D1 notes) — deliberately out of scope.
- `ChartImage.GetImage`/`SaveImage`'s background-rectangle fill still does its own `GdiRenderSurface` downcast for a raw pre-paint `graphics.DrawRectangle` call — not part of the `Paint` chain (it draws before `Paint` runs), could become an `IRenderSurface.Clear`/`FillBackground` method in a future increment but wasn't judged worth it for this pass.

**D3 — remove temporary concrete overloads.** Blocked on: the whole `ChartGraphics3D.cs` 3D subsystem, `MapAreasCollection.cs` (scope undecided), `RangeChart.cs`'s shadow-region block, `CalloutAnnotation.cs`'s cached geometry helpers (`GetCloudPath`/`GetCloudOutlinePath`), and the entire untouched Gauge engine.

**E1/E2 — Skia backend.** Several `Skia*` adapter members are explicit stubs (`NotImplementedException`/`NotSupportedException`) today: `SkiaClipRegion`'s extended members, `SkiaResourceFactory.CreatePen(IBrush, float)`, brush-based pen kinds, `SetTransparentColor`/`SetWrapMode`, `WrapImage`/`CreateTextureBrush` (since `ImageLoader`'s pipeline is GDI+-only and unreachable on Skia anyway).
