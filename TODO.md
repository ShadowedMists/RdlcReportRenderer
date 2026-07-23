# TODO

## Project Status Summary

**Overall Progress:** Infrastructure complete, Excel Phases 4-5 complete, Chart/Gauge GDI+ abstraction substantially complete (Chart) / actively in progress (Gauge), PDF cross-platform not started.

### Current Priorities

| Priority | Phase | Status | Risk |
|----------|-------|--------|------|
| 🔴 **HIGH** | Excel Phase 4: ImageFormatType Enum | ✅ COMPLETE | LOW |
| 🔴 **HIGH** | Excel Phase 5: IImageProvider Abstraction | ✅ COMPLETE | MEDIUM |
| 🟡 **HIGH** | Chart engine: GDI+ → interface abstraction | 🔄 Substantially complete (Milestones A-C, D2 done; D3/E1/E2 open) | HIGH |
| 🟡 **HIGH** | Gauge engine: GDI+ → interface abstraction | 🔄 In progress (Milestones A-B3 done; several named gaps open) | HIGH |
| 🔵 **LOW** | PDF Phase 1: SkiaSharp Migration | 📋 NOT STARTED | VERY HIGH |

> The prior "Chart Library Migration (OxyPlot)" decision was retracted — see `docs/decisions.md`. Charts/gauges are rendered by vendored GDI+ engines this repo owns, not external libraries; the plan re-targets their existing rendering seams to SkiaSharp.

---

## Excel Phase 4: ImageFormatType Enum (✅ COMPLETE)

Replaced `System.Drawing.Imaging.ImageFormat` with a cross-platform `ImageFormatType` enum across the Excel renderer (internal API only, no public breaking changes). Reference: `docs/rendering-abstractions.md`.

## Excel Phase 5: IImageProvider Abstraction (✅ COMPLETE)

Added `IImageProvider`/`WindowsImageProvider`/`CrossPlatformImageProvider`/`ImageProviderFactory`, injected into `ChartMapper`/`GaugeMapper`. Reference: `docs/rendering-abstractions.md`.

## Chart & Gauge Cross-Platform Rendering

**Approach:** re-target the existing vendored GDI+ Chart/Gauge engines to SkiaSharp behind their existing rendering seams (Ports & Adapters — `IDrawingResourceFactory`/`IGaugeDrawingResourceFactory` port, `Gdi`/`Skia` adapters). Full design: `docs/rendering-abstractions.md`. Key decisions and the Phase 0 spike findings (GDI+ cannot construct at all on Linux, independent of backend): `docs/decisions.md`, `docs/platform-support.md`.

**Chart engine** (`tasks/chart-gdi-type-abstraction.md`): Milestones A (foundation), B (chokepoint migration), C1-C8 (per-type ports + call-site conversion) are done. D2 (backend selection) is **essentially done** (2026-07-22): scoped its real blocker to ~49 call sites reading `.Graphics` as raw `System.Drawing.Graphics` and converted ~47 of them across three sub-increments — (1) `ImageLoader.GetAdjustedImageSize`'s dead `Graphics` param removed, DPI/`CompositingQuality`/`InterpolationMode` abstracted onto `IChartRenderingEngine`; (2) `ChartPicture.Paint`'s `Graphics`-typed entry point given an `IRenderSurface`-typed dual-overload sibling (via a new `IChartRenderingEngine.BindSurface(IRenderSurface)` that moves the Gdi-surface downcast into the backend adapters), all 4 real production callers converted, `Graphics`-typed overload kept permanently for `ChartImage.SaveIntoMetafile`'s EMF-only path; (3) `TextAnnotation.cs`'s standalone `Bitmap`/`Graphics` construction routed through `IRenderSurfaceFactory`, and a provably-redundant `Graphics` reassignment removed from `SelectionManager.cs`. Only 2 already-documented permanent holdouts remain (`FunnelChart` 3D fill, `SaveIntoMetafile` EMF), both by design. D3 (remove temporary concrete overloads — blocked on `ChartGraphics3D.cs`, `MapAreasCollection.cs`, and the entire Gauge engine). E1 (Skia backend) is **started** (2026-07-22): scoped its real blocker — `ChartGraphics.cs`/painter files still allocate concrete GDI+ resources feeding GDI+-typed engine calls, ~100-150 sites for a non-trivial 2D chart, independent of and much smaller than D3's everywhere-scope. Converted the two highest-leverage chokepoint helpers (`ChartGraphics.FillRectangleShadowAbs`, `DrawMarkerAbs`'s shape-marker branch) plus the Pie/Doughnut chokepoint (`DrawPieRel` + its `DrawPieGradientEffects`/`DrawPieSoftShadow` helpers — all retyped in place, no signature change) since ~70 painter files draw through the first two and `PieChart`/`SunburstChart` through the third. Along the way, added `Blend`/`InterpolationColors` to the shared `IPathGradientBrush` interface (a gap from the original brush count) and implemented them on both the Chart and Gauge engines' `GdiPathGradientBrush`. `PieChart.cs`'s 3 label call sites converted too (bridging `Font`/`StringFormat` to `IChartFont`/`ITextFormat`, same pattern as `BarChart.cs`) — `PieChart.cs` is now fully converted apart from its hit-testing-only `CreateMapAreaPath`. `AreaChart.cs`'s 2D fill/stroke path converted next, along with `StackedAreaChart.cs` (forced into the same increment since it shares `AreaChart`'s protected `areaPath` field) — both charts' 3D-only branches stay concrete by design (same D3-blocked 3D subsystem as `RangeChart.Draw3DSplinePolygon`). Remaining: ~55 more `ChartGraphics.cs` sites, 9 more `ChartTypes/*.cs` files, `SunburstChart`'s small label-fitting residual, then the Skia adapters' own remaining stubs (`SkiaClipRegion` extensions, brush-based pens, `SetTransparentColor`/`SetWrapMode`, `WrapImage`/`CreateTextureBrush`) — see task doc for exact detail.

**Gauge engine** (`tasks/gauge-gdi-type-abstraction.md`): Milestones A-A4 (foundation + clip-region), B1 (factory injection), B3 (the "atomic rewrite" of `KnobStyleAttrib`/`NeedleStyleAttrib`/`MarkerStyleAttrib`/`BarStyleAttrib`, 2026-07-22) are done, verified pixel-exact (55/55 tests, zero baseline diffs). B2 (real call-site conversion) is substantially done with 9 named open items remaining — most notably `HotRegionList`/`DrawRadialSelection` (systemic concrete-only hit-testing, its own future milestone) and `XamlRenderer.cs`/`XamlLayer.cs` (architecturally blocked, needs new gradient/transform/no-live-engine primitives).

**Map engine** (`Microsoft.Reporting.Map.WebForms`) is a third, separate GDI+-coupled rendering engine discovered during this work; its scope (in scope for this initiative, or permanently Windows-only?) is still an open decision.

**Verification convention** (both engines, every increment): `dotnet build` 0 errors + full test suite (`VisualRegressionTests` + `Chart.Rdl.Tests`) passing + zero baseline PNG diffs. See `docs/rendering-abstractions.md` for the git-stash baseline-generation technique used for previously-uncovered render paths.

## PDF Phase 1: SkiaSharp Graphics Migration (LOWER PRIORITY)

**Status:** Analysis complete, not started. **Blocker:** Metafile/EMF generation has no cross-platform equivalent — full graphics-stack replacement required, not incremental fixes.

**Reference:** `tasks/pdf-render-callstack-analysis.md` (5-phase roadmap), `tasks/pdf-quick-reference.md` (cheat sheet).

**Recommendation:** unchanged — lower priority than Chart/Gauge, since Excel/PDF *bodies* already render cross-platform and PDF's blocker is architecturally the deepest of the three.

---

## Completed Analysis & Documentation

| Category | Status | Details |
|----------|--------|---------|
| ✅ **.NET 10 Upgrade** | COMPLETE | All projects migrated, tests passing |
| ✅ **ImageSharp Integration** | COMPLETE | Image metrics cross-platform |
| ✅ **Excel image/format abstraction** | COMPLETE | See `docs/rendering-abstractions.md` |
| ✅ **Chart engine GDI+ abstraction** | Substantially COMPLETE | See `tasks/chart-gdi-type-abstraction.md` |
| 🔄 **Gauge engine GDI+ abstraction** | IN PROGRESS | See `tasks/gauge-gdi-type-abstraction.md` |
| 📋 **PDF cross-platform migration** | NOT STARTED | See `tasks/pdf-render-callstack-analysis.md` |

---

## Documentation index

- `docs/rendering-abstractions.md` — Excel/PDF renderer factory design + Chart/Gauge Ports & Adapters architecture (interfaces, namespaces, recurring patterns)
- `docs/platform-support.md` — current Windows/Linux/macOS support matrix and known gaps
- `docs/decisions.md` — architecture decisions and why (OxyPlot retraction, SkiaSharp re-target, per-method vs. per-type conversion, etc.)
- `docs/troubleshooting.md` — common issues and known quirks found during the migration
- `docs/architecture-map.md` / `docs/build-and-test.md` / `docs/renderer-extension-guide.md` / `docs/examples.md` — supporting reference docs
- `tasks/chart-gdi-type-abstraction.md` / `tasks/gauge-gdi-type-abstraction.md` — active migration progress + open items
- `tasks/chart-cross-platform-implementation.md` — overall Chart/Gauge phase plan
- `tasks/pdf-render-callstack-analysis.md` / `tasks/pdf-quick-reference.md` — PDF migration roadmap (not started)
- `tasks/adapter-layer-refactor.md` — broader adapter-layer scope and README compatibility-gap follow-ups
- `tasks/chart-library-decision.md` / `tasks/chart-image-abstraction-analysis.md` — retraction/superseded pointers

## Notes

- Update this file's status table as milestones complete; keep detailed progress in the linked `tasks/*.md` files, not here.
- Durable architecture facts, decisions, and known gaps belong in `docs/`, not in task-tracking narrative.
