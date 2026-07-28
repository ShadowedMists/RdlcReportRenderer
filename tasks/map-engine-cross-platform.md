# Map engine: GDI+ → interface abstraction

**Status: Milestone A done (2026-07-28).** Migration itself deferred per `docs/decisions.md`'s Map engine entry (Bing Maps EOL, tile-service choice needed first — defaulted to OpenStreetMap 2026-07-28, see that same doc). This file is the concrete migration plan, mirroring how `docs/rendering-abstractions.md` documents the already-completed Chart/Gauge Ports & Adapters work — write it here as work lands, fold into that doc once substantially complete (matching this repo's `tasks/` vs `docs/` convention).

## Milestone A: done (2026-07-28)

Built from scratch this pass, mirroring Chart's exact Ports & Adapters shape (`Chart.WebForms/GdiGraphics.cs`'s `Native(IPen)`/`Native(IBrush)`/etc. unwrap-and-delegate pattern was copied near-verbatim):

- **New Map-owned resource port**: `Microsoft.Reporting.Map.WebForms/Rendering/IMapDrawingResourceFactory.cs` — mirrors Chart's `IDrawingResourceFactory` but deliberately scoped down to what this milestone's dual-overload methods actually need: `IPen` (2 ctors), `ISolidBrush`/`ILinearGradientBrush`/`IHatchBrush`/`IPathGradientBrush` (4 of the 5 brush kinds — `ITextureBrush` skipped, no `TextureBrush` construction found in Map's own GDI+ touchpoint survey), `IChartFont` (3 `CreateFont` overloads + `WrapFont`), `ITextFormat`, `IGraphicsPath` (`CreatePath`/`CreatePath(points,types)`/`WrapPath`). All resource *interfaces* are the same shared `Microsoft.Reporting.Rendering` ones Chart/Gauge already use — only the factory interface and adapter classes are Map-owned, per the established "Gauge's adapters are separate implementations from Chart's identically-shaped ones by design" convention (`docs/rendering-abstractions.md`).
- **New Map-owned Gdi adapters**: `Microsoft.Reporting.Map.WebForms/Rendering/Gdi/{GdiPen,GdiBrushes,GdiChartFont,GdiTextFormat,GdiGraphicsPath,GdiMapResourceFactory}.cs` — each is a near-verbatim copy of Chart's identically-named/shaped adapter, just in Map's own namespace (per the same duplication convention).
- **`IMapRenderingEngine.cs`**: added ~24 new interface-typed dual-overload method signatures (`DrawLine(IPen,...)`, `DrawString(string,IChartFont,IBrush,...,ITextFormat)`, `FillPath(IBrush,IGraphicsPath[,float,bool,bool])`, `MeasureString(string,IChartFont[,SizeF,ITextFormat])`, `SetClip(IGraphicsPath,CombineMode)`, etc.) alongside the existing 100%-concrete ones — same "dual-overload, never big-bang" strategy used throughout Chart/Gauge's migration.
- **Three implementers found and updated** (only two were expected going in — `GdiGraphics`/`RenderingEngine` — a third, `SvgMapGraphics`, was discovered via a build error and needed the identical treatment):
  - `GdiGraphics.cs` — real bridge: `Native(IPen pen) => ((GdiPen)pen).NativePen` style unwrap helpers, each new method unwraps and delegates to the existing concrete overload.
  - `RenderingEngine.cs` — simple forward to `RenderingObject` (the active concrete engine), matching the existing concrete-typed methods' own forwarding shape.
  - `SvgMapGraphics.cs` (SVG rendering path, extends `SvgRendering`) — same `Native()`-unwrap-and-delegate pattern as `GdiGraphics`, delegating to its own already-existing concrete-typed methods (which write SVG rather than calling `Graphics` directly).
- **`MapGraphics.cs`**: added `internal IMapDrawingResourceFactory ResourceFactory { get; } = new GdiMapResourceFactory();` (Gdi-only default, no Skia backend yet — mirrors `ChartGraphics.ResourceFactory`, but as a simple property rather than constructor-injected, since there's only one backend to select between so far).
- **Deliberately deferred, not part of this milestone**: `ITextureBrush`/`IChartImage` (Map's `DrawImage` overloads stay concrete `Image`-typed — no portable image-loading pipeline built yet), and clip-region abstraction (`Region Clip`/`GetClipRegion`/`SetClipRegion` stay concrete — Chart's `IClipRegion` needed `IChartRenderingEngine`-specific coupling per `docs/rendering-abstractions.md`, and building Map's equivalent felt like more scope than this pass warranted; `SetClip(IGraphicsPath, CombineMode)` was included since it only needs `IGraphicsPath`, not a clip-region type).
- **Verified**: `dotnet build --no-incremental` 0 errors. Full Windows suite (137 `VisualRegressionTests` + 187 `Chart.Rdl.Tests`, the latter including `MapRdlTests.SimpleMap_RendersToImage`) all pass with byte-identical baselines — confirms this milestone is genuinely zero-behavior-change: no real call site anywhere in the codebase uses the new interface-typed overloads yet, they exist purely as unused-so-far surface for Milestone B to convert onto.

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

1. ~~**Milestone A**: define `IMapRenderingEngine`'s interface-typed signatures (dual-overload) and a Gdi adapter pair that's behaviorally identical to today's code.~~ — done 2026-07-28, see above.
2. **Milestone B** (next): convert the 24 real painter files' call sites from concrete to interface-typed, one file/method at a time, verified by whatever test coverage exists (`MapRdlTests.SimpleMap_RendersToImage` today — thin; more fixtures would help before this milestone, since that's the only current regression guard for Map at all). Texture-brush/Image/ClipRegion call sites (deferred from Milestone A) will need their factory methods/adapters added when first encountered, following the same pattern.
3. **Milestone C**: the render-surface abstraction reaching into `MapMapper.GetPngImage`/`GetEmfImage` (the "new for Map" item above).
4. **Milestone D**: a `SkiaMapRenderingEngine`/`SkiaMapDrawingResourceFactory` pair, plus platform-selection wiring (mirroring `ChartRenderingBackendFactory`).
5. **Milestone E**: EMF stays permanently Windows-only (guard, not port) — same as Chart's `SaveIntoMetafile` and the IMAGE renderer's `MetafileGraphics`.

Each milestone should land as its own small, verified commit — this is real, multi-session work, not something to compress into a single pass.
