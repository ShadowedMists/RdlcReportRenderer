# Map engine: GDI+ → interface abstraction

**Status: scoped (2026-07-28), not started.** Migration itself deferred per `docs/decisions.md`'s Map engine entry (Bing Maps EOL, tile-service choice needed first — defaulted to OpenStreetMap 2026-07-28, see that same doc). This file is the concrete migration plan, mirroring how `docs/rendering-abstractions.md` documents the already-completed Chart/Gauge Ports & Adapters work — write it here as work lands, fold into that doc once substantially complete (matching this repo's `tasks/` vs `docs/` convention).

## Why "347 files" overstates the real painter-conversion surface

The original Map deferral decision cited "347 files, ~22,400 lines" as the migration size, in the same category as Chart's. A 2026-07-28 survey found this is misleading for scoping purposes: of the 347 files, only **24 actually contain direct `Draw*`/`Fill*` GDI+ calls** (the real "painter" surface that needs call-site conversion) — the rest are data-model/attribute/converter/collection classes with no drawing at all (largest categories: `*Converter.cs`, frame/border style classes, group/rule/binding classes). This is smaller than Chart's own painter surface (73 files) despite Map's larger total file count. The real migration is closer in scale to Gauge's (~30 painter files) than to "347 files" as a raw estimate would suggest.

## Entry-point call chain (RDL → pixels)

`MapMapper.GetImage(ImageType)` (`Microsoft.ReportViewer.Common/Microsoft.ReportingServices.OnDemandReportRendering/MapMapper.cs:175`) branches on PNG vs EMF:
- **PNG**: `GetPngImage` → `new Bitmap(width, height)` → `Graphics.FromImage(bitmap)` → `GetImage(graphics)` → `m_coreMap.mapCore.Paint(graphics)` → `bitmap.Save(stream, ImageFormat.Png)`.
- **EMF**: `GetEmfImage` → `new Bitmap` + `Graphics.FromImage` → `graphics.GetHdc()` → `new Metafile(stream, hdc, ...)` → `Graphics.FromImage(metafile)` → same `Paint(graphics2)` → `ReleaseHdc`. Permanently Windows-only, same class of wall as Chart's `SaveIntoMetafile`/IMAGE renderer's `MetafileGraphics` — guard rather than port, matching those two precedents.

`MapCore.Paint(Graphics gdiGraph, RenderingType, Stream, buffered)` (`Microsoft.Reporting.Map.WebForms/MapCore.cs:3424`) is the Map equivalent of `ChartImage.SaveImage`/`GaugeImage`: calls `GetGraphics(renderingType, gdiGraph, stream)` to build a `MapGraphics`, then dispatches to `RenderOneGridSection`/`RenderOnePanel`/`RenderFrame`/`RenderElements(Buffered)`.

**Important structural difference from Chart:** Chart's real production path (`ChartMapper.GetImage → Chart.Save → ChartImage.SaveImage`) is already routed through `IRenderSurface.Encode` at the outermost layer — the raster surface itself is already abstracted. Map's raw `Bitmap`/`Graphics.FromImage`/`Metafile`/`GetHdc` calls happen **directly inside `MapMapper.cs`**, one layer above `MapCore`/`MapGraphics`. Map's abstraction boundary needs to reach one layer higher than Chart's did — `MapMapper.GetPngImage`/`GetEmfImage` are themselves GDI+ call sites requiring a render-surface abstraction (mirroring `IRenderSurface`/`GdiRenderSurface`/`SkiaRenderSurface`), not just internal engine plumbing.

## The existing chokepoint: `IMapRenderingEngine`/`MapGraphics`

`MapGraphics` (`MapGraphics.cs`, ~2,094 lines) extends `RenderingEngine : IMapRenderingEngine` (`IMapRenderingEngine.cs`) — this is Map's own chokepoint, structurally analogous to `ChartGraphics`/`GaugeGraphics` **before** their migrations started. Good news: the chokepoint shape already exists, so there's no need to invent one from scratch. Bad news: `IMapRenderingEngine` is 100% GDI+-typed in every signature today (`DrawLine(Pen, ...)`, `FillPath(Brush, GraphicsPath)`, `DrawString(..., Font, Brush, ...)`, a `Graphics` property, `Region Clip`, etc.) — it provides zero type abstraction yet, exactly the starting state Chart/Gauge's `IChartRenderingEngine`/`IGaugeRenderingEngine` were in before their own migrations.

## GDI+ touchpoint counts (construction-site counts, `Microsoft.Reporting.Map.WebForms` only)

| Resource | Count |
|---|---|
| `new Pen(` | 32 |
| `new SolidBrush(` | 48 |
| `new LinearGradientBrush(` | 6 |
| `new HatchBrush(` | 1 |
| `new Font(` | 32 (field-initializer subset of these already fixed for construction-time crashes — see `tasks/chart-default-font-cross-platform.md`'s sibling fields in `Microsoft.Reporting.Map.WebForms/*.cs`, left un-lazified since Map rendering itself is out of scope until this migration starts) |
| `new GraphicsPath(` | 58 |
| `new Bitmap(` | 15 |
| `Graphics.From*` (FromImage/FromHdc) | 10 |
| Files with `using System.Drawing` | 104 of 347 |

## What's reusable as-is vs. what needs to be Map-specific

Following the precedent explicitly documented in `docs/rendering-abstractions.md` ("share pure resource interfaces between the Chart and Gauge engines, but not the adapters or clip-region interface"):

- **Directly reusable, no changes needed:** the neutral `Microsoft.Reporting.Rendering` namespace's `IPen`, `IBrush` family, `IChartFont`, `ITextFormat`, `IGraphicsPath`, `IChartImage`, `IImageDrawOptions` — Map's resource vocabulary (Pen/Brush/Font/GraphicsPath/Image) is a subset of Chart's, with no Map-specific resource kind found in the survey.
- **Needs its own, Map-scoped versions** (same reasoning `IClipRegion`/`IGaugeClipRegion` split on): `IMapRenderingEngine` itself (already exists, needs full retyping), a `IMapDrawingResourceFactory` (mirroring `IDrawingResourceFactory`/`IGaugeDrawingResourceFactory`), and Map's own clip-region interface if `Region Clip` construction ties back to a live `Graphics` the same way Chart's `IClipRegion` did.
- **New for Map, no Chart/Gauge precedent:** a render-surface abstraction reaching into `MapMapper.cs` itself (see "structural difference" above) — `IRenderSurface`/`GdiRenderSurface`/`SkiaRenderSurface` from Chart could potentially be reused directly if Map's raster-surface needs (page size, encode-to-stream) are the same shape, worth checking before building a parallel `IMapRenderSurface`.

## Tile-service default (already decided)

See `docs/decisions.md`'s "Tile-service default (2026-07-28)" note under the Map engine deferral entry: OpenStreetMap picked as the default adapter target (no API key/billing relationship required), a placeholder/overridable decision, not a blocker for starting this migration's GDI+→interface work (which is orthogonal to which tile service eventually gets wired in).

## Proposed milestones (mirroring Chart/Gauge's staged approach)

Not started — listed in the order Chart/Gauge's own migration used (interfaces → adapters → per-file conversion → backend selection):

1. **Milestone A**: define `IMapRenderingEngine`'s interface-typed signatures (dual-overload alongside the existing concrete ones, per the established "never a big-bang signature change" decision) and a `GdiMapRenderingEngine`/`GdiMapDrawingResourceFactory` adapter pair that's behaviorally identical to today's code — zero behavior change, build-verified only.
2. **Milestone B**: convert the 24 real painter files' call sites from concrete to interface-typed, one file/method at a time, verified by whatever test coverage exists (`MapRdlTests.SimpleMap_RendersToImage` today — thin; more fixtures would help before this milestone, since that's the only current regression guard for Map at all).
3. **Milestone C**: the render-surface abstraction reaching into `MapMapper.GetPngImage`/`GetEmfImage` (the "new for Map" item above).
4. **Milestone D**: a `SkiaMapRenderingEngine`/`SkiaMapDrawingResourceFactory` pair, plus platform-selection wiring (mirroring `ChartRenderingBackendFactory`).
5. **Milestone E**: EMF stays permanently Windows-only (guard, not port) — same as Chart's `SaveIntoMetafile` and the IMAGE renderer's `MetafileGraphics`.

Each milestone should land as its own small, verified commit — this is real, multi-session work, not something to compress into a single pass.
