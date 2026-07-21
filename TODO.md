# TODO

## Project Status Summary

**Overall Progress:** Infrastructure complete, Excel Phases 4-5 complete, chart/gauge + PDF cross-platform pending

### Current Priorities

| Priority | Phase | Status | Timeline | Risk |
|----------|-------|--------|----------|------|
| 🔴 **HIGH** | Excel Phase 4: ImageFormatType Enum | ✅ COMPLETE | — | LOW |
| 🔴 **HIGH** | Excel Phase 5: IImageProvider Abstraction | ✅ COMPLETE | — | MEDIUM |
| 🟡 **HIGH** | Chart & Gauge: re-target GDI+ engines to SkiaSharp | ✅ SPIKE COMPLETE — Option A confirmed, scope revised | Phases 1-6 (see below) | HIGH |
| 🔵 **LOW** | PDF Phase 1: SkiaSharp Migration | 📋 PENDING | 2-3 weeks | VERY HIGH |

> **Note:** The prior "Chart Library Migration (OxyPlot)" line was retracted — see `tasks/chart-library-decision.md`. Charts are rendered by a vendored GDI+ engine we own, not an external library; the corrected plan re-targets its existing rendering seam to SkiaSharp. A time-boxed spike must size the effort before scheduling.

---

## Next Tasks

### Excel Phase 4: ImageFormatType Enum (✅ COMPLETE)

**Status:** Implementation complete, all tests passing  
**Effort:** 2-3 days  
**Risk:** LOW (internal API only, no public breaking changes)  
**Files Created:** `ImageFormatType.cs`  
**Files Modified:** IExcelGenerator.cs, ImageInformation.cs, OpenXmlGenerator.cs, BIFF8Generator.cs, LayoutEngine.cs, Escher.cs

- [x] Create ImageFormatType enum with cross-platform values
- [x] Implement ImageFormatTypeHelper static class with conversion methods
- [x] Add FromSystemDrawingImageFormat conversion method
- [x] Update IExcelGenerator interface signature
- [x] Modify ImageInformation.cs to use new enum
- [x] Update OpenXmlGenerator.cs format detection logic
- [x] Update BIFF8Generator.cs AddImage signature
- [x] Update Escher.cs DrawingGroupContainer.AddImage method
- [x] Update LayoutEngine.cs to use new enum
- [x] Verify Excel XLSX output remains unchanged
- [x] All tests passing (5/5)

**Reference:** `tasks/imagetype-enum-implementation.md` (complete 11-item checklist provided)

### Excel Phase 5: IImageProvider Abstraction (✅ COMPLETE)

**Status:** Implementation complete, all tests passing  
**Effort:** 3-4 days  
**Risk:** MEDIUM (architectural change, affects chart handling)  
**Files Created:** IImageProvider.cs, ImageMetadata.cs, WindowsImageProvider.cs, CrossPlatformImageProvider.cs, ImageProviderFactory.cs  
**Files Modified:** ChartMapper.cs, GaugeMapper.cs

- [x] Create IImageProvider interface with LoadImage and GetImageForChart methods
- [x] Create ImageMetadata class for image dimensions and format
- [x] Implement WindowsImageProvider (System.Drawing wrapper)
- [x] Implement CrossPlatformImageProvider (SixLabors.ImageSharp wrapper)
- [x] Create ImageProviderFactory for platform-specific selection
- [x] Inject IImageProvider into ChartMapper and GaugeMapper
- [x] Updated GetImageFromStream() in both mappers to use abstraction
- [x] Verified chart rendering with updated code
- [x] All tests passing (5/5)

**Reference:** `tasks/chart-image-abstraction-analysis.md` (complete design patterns and roadmap provided)

### Chart & Gauge Cross-Platform Rendering (✅ Phase 0 spike complete)

**Status:** Prior OxyPlot decision RETRACTED. Phase 0 spike (2026-07-18) confirms Option A (re-target to SkiaSharp) — see spike report in `tasks/chart-cross-platform-implementation.md` for full detail.
**Blocker:** Two vendored GDI+ engines (Chart.WebForms + GaugeContainer) rasterize to PNG/EMF via `System.Drawing`, which — per the spike — cannot even construct a `Pen`/`Font`/`Bitmap`/etc. on Linux with .NET 10, independent of `libgdiplus` presence.

**Corrected finding:** Charts are NOT rendered by a missing library — they are rendered by a **vendored GDI+ chart engine we own**, which already has an `IChartRenderingEngine` seam. The fix is to re-target that seam to SkiaSharp (dual-backend: Windows keeps GDI+, Linux uses Skia), NOT to replace the engine with OxyPlot.

**Spike result:** Built a spike-scoped `Rendering/Skia/` adapter set (`SkiaResourceFactory`, `SkiaPen`/`SkiaSolidBrush`/`SkiaChartFont`/`SkiaTextFormat`/`SkiaGraphicsPath`) + `SkiaChartGraphics : IChartRenderingEngine`, and a hand-built bar-chart scene written only against the backend-agnostic `Rendering/` port — it renders correctly on **both Windows and WSL Linux**, unmodified. This confirms the port design (already drafted in `chart-gdi-type-abstraction.md` Milestone A) is the right direction. **But** the spike also found GDI+ itself can't construct on Linux at all today (not just at the rendering seam) — so completing the per-type migration (Milestones B1b/B2/C1-C8) is now a **hard prerequisite** for any Linux chart rendering, GDI+ or Skia, not optional/parallelizable polish. Effort-wise, the adapter-pair pattern (one Gdi/Skia pair per resource type) proved mechanical once built once — the XL sizing for C6/C7 still holds, but urgency changes.

**Post-spike progress (2026-07-18, same-day follow-through):** Completed Milestones C1 (`Region`), C2 (`Matrix`→`Matrix3x2`, with verified GDI+ rotation-parity + a new visual-regression test), and C3 (`Pen`) for all independently-convertible call sites, each with build+test verification and zero baseline regressions. Confirmed C4-C8's ports/adapters (`IBrush`+`IPathGradientBrush` — a real gap found and fixed —, `ITextFormat`, `IChartFont`, `IGraphicsPath`, `IImageDrawOptions`) are all already complete from the earlier A1/A2 drafting. **Key strategic finding:** per-type migration (C1-C8) only cleanly works for `Region`/`Matrix`; real painter code bundles `Pen`+`GraphicsPath`, or `Font`+`Brush`+`StringFormat`, in the same calls — so the actual remaining bottleneck is **B1b/B2** (converting whole methods/files at once), not the remaining per-type checkboxes. **B2's blast radius is bigger than scoped:** `ChartGraphics`'s brush-helper methods are called from 20+ `ChartTypes/*.cs` painter files, and there are **three** parallel GDI+-coupled engines — chart, gauge (`GaugeGraphics`), **and map** (`MapGraphics`, not previously in scope) — see `chart-gdi-type-abstraction.md` B2 note.

**B2 real-caller migration started (2026-07-19):** Completed the full interface-typed brush/circle/path/rect-fill method surface on `ChartGraphics` (`Get*BrushResource`, `DrawCircleAbs(IPen,IBrush,...)`, `DrawPathAbs(IGraphicsPath,...)`, `FillRectangleRelResource`/`FillRectangleAbsResource`), then began switching real painter call sites onto it: renamed `.FillRectangleRel(`/`.FillRectangleAbs(` → their `*Resource` siblings across all 17 real Chart-engine callers (pure drop-in rename, since these methods take only value-type parameters) — build 0 errors, all 5 visual regression tests pass, zero baseline changes. Investigated extending the same rename to `GetHatchBrush`/`GetGradientBrush`/`GetTextureBrush`/`DrawCircleAbs` callers and found it does **not** generalize: 3 of the next 5 candidate files (`AreaChart.cs`, `RangeChart.cs`, `StackedAreaChart.cs`) are blocked on the same GDI+-only gaps already flagged for `CalloutAnnotation.cs`/`FunnelChart.cs` (`new Region(graphicsPath)` and `ChartGraphics.Widen(path, pen)`, neither of which has a cross-platform equivalent yet). **This raises the priority of resolving `Widen`/`Region`-from-path** — it's now the single highest-leverage remaining blocker, since fixing it unblocks 5+ files at once. See `chart-gdi-type-abstraction.md`'s Milestone C progress notes and §7 risk table for full detail.

**AxisScaleSegment.cs and PieChart.cs's full 3D pie/doughnut cluster converted (2026-07-19):** `AxisScaleSegment.cs`'s break-line fill (`PaintBreakLine`/`GetBreakLinePath`/`GetChartFillBrush`) is now fully interface-typed (found and fixed a missing `IGraphicsPath.AddCurve(points, offset, count, tension)` overload along the way). `PieChart.cs`'s entire 3D pie/doughnut rendering chain (`Draw3DPie` → `DrawPieCurves`/`DrawDoughnutCurves`/their BigSlice helpers/`Draw3DOutsideLabels` → `ChartGraphics3D.FillPieSlice`/`FillDoughnutSlice`/`FillPieSides`/`FillPieCurve`) is also fully converted — the first B2 conversion this session verified with genuine pixel-identical proof: added two new sample charts (3D pie, 3D doughnut) and baselines generated from the pre-conversion code via a temporary `git stash`, and the post-conversion render matches byte-for-byte. Found and fixed another real gap: `IPen` had no `Clone()` (needed to derive a border pen without mutating the shared fill pen); added it to the interface and both `Gdi`/`Skia` adapters. Full detail in `chart-gdi-type-abstraction.md`.

**`Widen` half of the `Widen`/`Region` blocker resolved (2026-07-19):** implemented `SkiaGraphicsPath.Widen(IPen)` for real via `SKPaint.GetFillPath` (Skia's own stroke-to-fill primitive, not hand-rolled), added the missing `ChartGraphics.Widen(IGraphicsPath, IPen)` overload and 2 more missing `HotRegionsList.AddHotRegion` interface-typed overloads, then converted 9 of the 13 real `ChartGraphics.Widen` call sites across `LineChart.cs`, `StepLineChart.cs`, `KagiChart.cs`, `RadarChart.cs`, `LineAnnotation.cs`, `PolylineAnnotation.cs`, `Axis.cs`, and the hit-region-only halves of `AreaChart.cs`/`StackedAreaChart.cs`. Full detail, including the 4 sites left blocked on unrelated deeper issues, in `chart-gdi-type-abstraction.md`.

**`Region`/`Clip` half also resolved (2026-07-19, same day):** turned out the interface-typed alternative (`GetClipRegion()`/`SetClipRegion(IClipRegion)`) already existed — no property retyping needed, just real-caller migration, same pattern as `Pen`/`Brush`. Extended `IClipRegion` with 4 previously-missing members (`Translate`, `Clone`, `IsInfinite`, `Complement`) and converted 11 real call sites across `StackedBarChart.cs`, `StackedColumnChart.cs`, `RangeColumnChart.cs`, `PieChart.cs`, `StackedAreaChart.cs`, `AreaChart.cs`, `Axis.cs`, and `Borders3D/EmbossBorder.cs`/`SunkenBorder.cs` (the latter two were the exact sites flagged back in Milestone C1 as blocked on C7 — C7 finished long ago; the real blocker was `IClipRegion` missing `Complement`). Verified with 3 new pixel-exact regression tests (Emboss border, Sunken border, shadowed area chart), generated from pre-conversion code via the same `git stash` technique — all match byte-for-byte. `RangeChart.cs`'s equivalent block stays concrete (shares its path/brush with the main fill, plus touches `graph.Transform` too) — real, separate future work. Also found a genuinely new gap while investigating it: its fallback stroke does `new Pen(brush, 1f)` where `brush` can be a `HatchBrush`/`TextureBrush`, not just solid — GDI+ strokes with the brush's actual pattern in that case, which `IPen` (deliberately solid-color-only) can't represent. Not the same class of problem as `Region`/`Transform`; flagged as a separate open architectural question, not force-converted. Full detail in `chart-gdi-type-abstraction.md`.

**Milestone D1 done (2026-07-19):** routed all 4 real `new Bitmap`+`Graphics.FromImage` construction sites (`ChartImage.GetImage` — the real `Chart.Save` render path — `ChartImage.GetSvgImage`, `ChartPicture.Select`'s hit-test bitmap, `ChartPicture.Paint`'s SVG-border bitmap) through the already-drafted `IRenderSurfaceFactory`/`GdiRenderSurfaceFactory`. `ChartImage.SaveIntoMetafile`'s Bitmap+Graphics (used only to obtain a Windows HDC for `Metafile`) deliberately left untouched — Windows-only by nature. This does **not** yet make the top-level paint entry point backend-agnostic: `ChartPicture.Paint(Graphics graph, ...)` still takes a concrete `Graphics` parameter, and `ChartGraphics.Graphics` is still concrete-typed — that's D2/E1's job, still correctly gated on B1b/B2/C1-C8 finishing. Verified: build 0 errors, all 10 regression tests pass byte-for-byte (harness renders via the exact `chart.Save(...)` path converted) — zero behavior change on Windows. Full detail in `chart-gdi-type-abstraction.md`.

**`StockChart.cs` converted (2026-07-19):** its Triangle open/close mark drawing (2D + 3D) built a fully self-contained `GraphicsPath`+`SolidBrush` with no `Region`/`Widen` entanglement, so it converted cleanly onto `resourceFactory.CreatePath()`/`CreateSolidBrush()`. No existing baseline exercised `SeriesChartType.Stock`, so added a new sample chart + regression test, generated its pre-conversion baseline via the same `git stash` technique — byte-for-byte match confirmed. Build 0 errors, all 11 tests pass.

**`FastPointChart.cs` converted too (2026-07-19):** `DrawMarker`'s `Brush`/`Pen` parameters (plus its caller's `SolidBrush`/`Pen` locals) converted in place — confirmed zero subclasses exist, so no dual-overload needed; the marker-drawing calls (`FillPolygon`/`DrawPolygon`/`FillEllipse`/etc.) already had matching interface overloads from Milestone A3. Added a new sample chart + regression test (`SeriesChartType.FastPoint` was previously unexercised), verified byte-for-byte against a pre-conversion baseline. Build 0 errors, all 12 tests pass.

**`FastLineChart.cs` converted too (2026-07-19):** `DrawLine`'s `Pen` parameter (plus caller's `pen`/`pen2` locals and its own hit-region `GraphicsPath`) converted in place — same "confirmed zero subclasses" pattern, and a matching `IGraphicsPath` `AddHotRegion` overload already existed. Added a new sample chart + regression test (`SeriesChartType.FastLine` was previously unexercised), verified byte-for-byte. Build 0 errors, all 13 tests pass.

**`LineChart.cs`'s shadow-line block converted too (2026-07-19):** its local shadow `Pen` (independent of the shared `linePen` field, left concrete) converted to `IPen`, adding a private dual-overload of `DrawTruncatedLine` since both the new local pen and the shared `linePen` field call it. `graph.Transform`/`Save()`/`Restore(GraphicsState)` stay concrete — `GraphicsState` has no interface equivalent anywhere in the port, a separate gap. New sample chart + baseline added, verified byte-for-byte. Build 0 errors, all 14 tests pass.

**Survey of remaining `ChartTypes/*.cs` files complete:** `TreeMapChart.cs`, `SunburstChart.cs`, `BarChart.cs`, `PointChart.cs`, `ErrorBarChart.cs`, `BoxPlotChart.cs` all reduce to the same `DrawPointLabelStringRel(..., new SolidBrush(...), ...)` pattern — blocked on the same C5/C6 (`IChartFont`/`ITextFormat`) real-caller migration already flagged as XL-sized in the tracking doc, not a per-file gap. The self-contained-block conversion vein (`StockChart`/`FastPointChart`/`FastLineChart`/`LineChart`) is now exhausted.

**C5/C6 (Font/TextFormat) B2 call-site conversion started (2026-07-19):** key finding — `Series.Font`/`DataPoint.Font` are public model properties (set directly by chart consumers), so they must stay `System.Drawing.Font`-typed forever; only the rendering call itself moves to `IChartFont`. Added interface-typed dual overloads for `ChartGraphics`'s `DrawStringRel`/`DrawStringAbs`/`DrawPointLabelStringRel`/`MeasureStringRel` (the `IChartRenderingEngine.DrawString`/`MeasureString` overloads they call already existed, unused, from Milestone A3). Established a bridging pattern — concrete `Font`/`StringFormat` locals stay concrete through the whole method (position-calculation helpers are themselves concrete), bridged to `IChartFont`/`ITextFormat`/`IBrush` only at the final draw call — proven end-to-end on `PointChart.cs`'s `DrawLabels`, verified byte-for-byte against a new baseline. Build 0 errors, all 15 tests pass. Every other `DrawPointLabelStringRel` site (found in the earlier survey) follows the same now-proven pattern — each still needs its own per-file pass, not attempted further yet.

**C5/C6 migration completed for all surveyed sites (2026-07-19), same day:** converted `BarChart.cs` (both 2D and 3D label calls), `ErrorBarChart.cs`, `BoxPlotChart.cs`, `StockChart.cs`, and `TreeMapChart.cs` (2 call sites) using the same proven bridge pattern — each confirmed to have no overriding subclass first. `SunburstChart.cs` also converted and builds clean, but **could not be visually verified**: found a pre-existing, unrelated bug — `SunburstChart.Name` returns `"TreeMap"` instead of `"Sunburst"`, so any chart using `SeriesChartType.Sunburst` throws `InvalidOperationException` today, independent of this migration. Not fixed (out of scope); flagged for a separate decision. Added 6 new sample charts + regression tests for the 6 verifiable conversions, all verified byte-for-byte via the git-stash technique. Build 0 errors, all 21 tests pass. **The C5/C6 real-caller migration is now complete for every site found in the B2 file survey.**

**`SunburstChart.Name` bug fixed (2026-07-19):** changed `SunburstChart.cs:16` to correctly return `"Sunburst"` instead of `"TreeMap"`, stopping the combine-check exception that fired for every `SeriesChartType.Sunburst` chart. A direct `Chart`/`Series` construction still rendered blank after the fix, which led to finding the real mechanism: `Microsoft.ReportViewer.Common/.../ChartMapper.cs`'s `RenderCategoryGrouping` builds `ChartArea.CategoryNodes` from the RDL report's `ChartCategoryHierarchy`, and that's the only place in the whole solution that ever populates it — Sunburst is driven entirely by RDL category groupings, not by the direct `Chart`/`Series` API (which is why `CategoryNode`/`CategoryNodeCollection` are `internal sealed`, with no public constructor — they're only meant to be built by `ChartMapper`). Added a new test project, `tests/Microsoft.ReportViewer.Chart.Rdl.Tests`, with a hand-authored RDL report exercising a real 2-level Sunburst category hierarchy end-to-end through `LocalReport.Render("IMAGE", ...)`; visually confirmed two correctly nested, labeled rings, then promoted to a baseline. Build 0 errors, all 22 tests across the solution pass (21 pre-existing + 1 new). Sunburst is now confirmed working for its real RDL use case, with regression coverage.

**`RangeChart.cs`'s brush-stroke gap resolved (2026-07-19):** this was one of the three remaining flagged architectural gaps — GDI+'s `new Pen(Brush, float)` (stroke with an arbitrary hatch/texture/gradient brush, not just solid) had no interface equivalent. Added `IDrawingResourceFactory.CreatePen(IBrush, float)` (implemented for real in `GdiResourceFactory`, stubbed `NotImplementedException` in the still-spike-scoped `SkiaResourceFactory`, matching its other unimplemented brush kinds), then converted `RangeChart.cs`'s `DrawLine` in full — `Brush`→`IBrush`, `Pen`→`IPen`, `GraphicsPath`→`IGraphicsPath`, and its shadow-offset `Region`/`Matrix` handling → `IClipRegion` + `Save()`/`TranslateTransform()`/`Restore()` (the same pattern already proven on `AreaChart.cs`). Nearly all the needed interface infrastructure already existed from earlier milestones; only the brush-backed pen constructor was missing. Added 3 new regression tests (`RangeChartWithShadow`/`WithHatch`/`WithGradient` — no `RangeChart` coverage existed at all before this), verified byte-for-byte against pre-conversion output via `git stash`. Build 0 errors, all 25 tests across the solution pass.

**`CalloutAnnotation.cs`'s `GraphicsPathIterator` gap resolved (2026-07-19):** the second of the three remaining architectural gaps, and bigger than `RangeChart.cs` — 5 shape-drawing methods (~600 lines) plus a from-scratch replacement for `GraphicsPathIterator.NextMarker` (a GDI+-only type with no interface equivalent). Converted all 5 `Draw*Callout` methods to `IGraphicsPath`/`IBrush`/`IPen`, reusing infrastructure that mostly already existed (`DrawPathAbs(IGraphicsPath, ...)`, `AddHotRegion(..., IGraphicsPath, ...)`, `ChartGraphics.Widen(IGraphicsPath, IPen)` — resolving 2 of the 4 previously-blocked `Widen` callers). Left the static, cached `GetCloudPath`/`GetCloudOutlinePath` geometry helpers concrete internally (their `Matrix.Translate`/`Scale` chaining wasn't worth risking a hand-derived `Matrix3x2` translation with no way to verify the math independently) and bridged their output at the call site instead. Replaced `GraphicsPathIterator.NextMarker` with a new `SplitAtMarkers` helper that reads the `PathMarker` flag bit directly off `IGraphicsPath.PathTypes` — the port already exposes the same raw path data GDI+ uses internally, so this needed no GDI+-specific API at all. Verified in two ways matching what changed: 7 new PNG regression tests (one per `CalloutStyle`) for the 5 converted rendering methods, byte-for-byte via `git stash`; and 4 new hit-test tests (`CalloutAnnotationHitTestTests.cs`, using the public `Chart.HitTest` API) for `SplitAtMarkers` itself, since hot regions produce no pixels a PNG diff could ever catch — confirmed a `Perspective` callout's two marker-merged wedges each hit-test correctly at their independently-computed centroids. Build 0 errors, all 36 tests across the solution pass.

**`ChartGraphics3D.cs`'s `frontLinePen` entanglement resolved (2026-07-19) — the third and last of the three flagged architectural gaps.** `Draw3DPolygon`/`Draw3DSurface` (a combined ~430 lines) share a persistent `Pen frontLinePen` field across calls (each 3D line/step-line/Kagi segment's front edge is deferred and drawn on the *next* segment's call), and `Draw3DSurface` also entangles `Widen` with an incrementally-built `graphicsPath` returned to ~7 external callers — too risky/large to convert wholesale. Reframed to the minimal real fix: `frontLinePen`'s only consumer is `DrawLine`, which already has an `IPen` overload, so just retyped the field and added a `BridgeFrontLinePen` helper that captures the concrete pen's *current* property values (color/width/caps/etc., important since `Draw3DSurface` mutates `EndCap` after construction but before the field assignment) at the point of assignment — same bridge-at-the-sink pattern used throughout this migration. Everything else in both methods stays concrete, untouched. Added `RenderLineChart3D` (3D line chart, default `Perspective == 0`, verifies the carry-over path executes across multiple segments), verified byte-for-byte via `git stash`. Build 0 errors, all 37 tests across the solution pass. **All three flagged architectural gaps from the B2 survey are now resolved.**

**`Axis.cs` converted (2026-07-19), reviewed at the user's request:** the largest engine file (3877 lines) — found and converted its 4 remaining concrete call sites: `DrawAxisTitle`/`DrawAxis3DTitle`'s axis-title `DrawStringRel` calls (added 2 missing `IChartFont`/`IBrush`/`ITextFormat`-typed overloads to `ChartGraphics.cs` to support them) and `DrawAxisLine`'s 2 hit-region-only `GraphicsPath` blocks. `DrawRadialLine`/`DrawCircularLine` and their `Widen` calls were already converted from the earlier `Widen` pass — confirmed by reading, not assumed. No `Axis.Title` baseline existed before this, so added `RenderChartWithAxisTitles`/`RenderChart3DWithAxisTitles` and verified byte-for-byte via `git stash`. Build 0 errors, all 38 `VisualRegressionTests` pass (39 total across the solution).

**`Grid.cs`/`TickMark.cs` converted (2026-07-19):** continuing the B2 sweep beyond `ChartTypes/*.cs` — found their only remaining `GraphicsPath` usage is hit-region-only (never drawn), so both converted cleanly to `IGraphicsPath` via `graph.ResourceFactory.CreatePath()`, with `TickMark.cs`'s one `.Transform(graph.Transform)` call bridging to the existing `Matrix3x2`/`GetTransform()` pair. No new gaps found. Build 0 errors, all 36 tests pass (unaffected, as expected, since these paths produce no pixels).

**D2/D3 status:** explicitly asked the user whether to proceed to D2/D3 (backend selection) despite the gate not being met — user chose to keep converting B2/C1-C8 call sites first, consistent with the doc's own sequencing (`D2/D3` depend on `B1b/B2/C1-C8` substantially completing). Remaining known concrete-GDI+ surface: `MapAreasCollection.cs` (scope not yet decided — Map.WebForms may be out of scope for this engine's conversion entirely) and `ChartGraphics3D.cs` (the separate 3D-rendering subsystem, several call sites already bridged individually but not converted wholesale). Every other file outside `ChartTypes/*.cs` is now fully converted or reviewed-and-confirmed-correctly-concrete: `Annotation.cs`, `AnnotationPathPointCollection.cs`, `AxisScaleSegment.cs`, `AxisScrollBar.cs`, `Axis.cs`, `ImageAnnotation.cs`, `Label.cs`, `Legend.cs`, `LegendCell.cs`, `PolylineAnnotation.cs`, `SmartLabels.cs`, `StripLine.cs`, `TextAnnotation.cs`, `Title.cs`.

**Annotation/axis/label sweep complete (2026-07-20), user-directed ("proceed with all annotation axis and label implementations"):** converted every remaining file outside `ChartTypes/*.cs` with concrete GDI+ construction — `AxisScrollBar.cs`, `ImageAnnotation.cs`, `TextAnnotation.cs`, `SmartLabels.cs` (partial, see below), `Label.cs`'s `PaintCircular`. `AnnotationPathPointCollection.cs`/`PolylineAnnotation.cs`/`Annotation.cs`/`AxisScaleSegment.cs` reviewed and confirmed already correctly handled or by-design concrete (public `Font`/`GraphicsPath` model properties, or already-documented self-contained blocks). Two real gaps found and fixed: `IGraphicsPath.AddString(RectangleF)` (GDI+'s rectangle-layout outline-text overload, needed by `TextAnnotation`'s Frame style) and `IDrawingResourceFactory.WrapFont(Font)` (wraps an existing `Font` losslessly, vs. reconstructing via `CreateFont(familyName, size, style, unit)` — added after finding the reconstruction approach isn't always byte-for-byte lossless).

**`SmartLabels.cs`'s `format` parameter deliberately NOT converted:** attempted, then reverted, converting `StringFormat` → `ITextFormat` across its whole call chain after discovering (via a solution-wide grep) it's called from 9 more `ChartTypes/*.cs` files, each following the already-established "position-calc stays concrete, bridge only at the final draw sink" pattern from C5/C6. Converting `SmartLabels` itself would have silently reopened that already-closed scope decision. Kept two independent, safe fixes: `DrawCallout`'s brush (`IBrush`), and a code comment on why its pen stays concrete (`AdjustableArrowCap`, no `IPen` equivalent — a previously-documented gap).

**Notable diagnostic finding:** chasing what looked like a real 3-pixel regression in a new Radar-chart test led to discovering that **GDI+'s anti-aliased rendering of rotated text is not perfectly deterministic across separate process runs** on this machine — confirmed by testing fully unmodified, pre-existing code against its own freshly-generated baseline and seeing the identical drift. Not caused by this session's work; added a small, explicitly-documented pixel-count tolerance (`ImageComparer.CompareToBaseline`'s new `maxDiffPixels` parameter, default 0) for just that one test rather than either accepting a false regression or weakening the harness's strict-by-default design.

**Also found (not yet fixed) during this sweep:** `AxisScrollBar.Paint()`'s scroll-button drawing and `ImageAnnotation`'s design-mode "(no image)" text are both **permanently unreachable** in this vendored/stripped build — `AxisScrollBar.IsVisible()` and `Chart.IsDesignMode()` are hardcoded to return `false`. Their GDI+ conversions are complete and safe but have no possible regression test; noted, not treated as gaps.

**`Legend`/`LegendCell`/`StripLine`/`Title` sweep complete (2026-07-20), user-directed continuation:** converted the last four files outside `ChartTypes/*.cs` with concrete GDI+ construction in their own rendering paths. `StripLine.PaintTitle`, `Title.Paint` (all 5 `TextStyle` branches, reusing the `IGraphicsPath.AddString(RectangleF)` gap from the annotation sweep at a second call site), `LegendCell.PaintCellText`, and `Legend`'s `DrawLegendTitle`/`DrawLegendHeader` all converted in full. Three more small gaps found and fixed, same shape as before: `ChartGraphics.MeasureStringRel`/`MeasureString`/`DrawString` orientation-aware overloads and `MeasureStringAbs` were each missing their `IChartFont`/`IBrush`/`ITextFormat`-typed sibling. Pure layout-only `MeasureStringAbs` calls (`Legend.GetTitleSize`/`GetHeaderSize`, `LegendCell.MeasureCell`) correctly left concrete, matching the established "position-calc-only stays concrete" precedent. Noted, not fixed: `TextStyle.Frame`'s title-text baseline shows visible clipping — confirmed via `git stash` to be pre-existing GDI+ behavior, unrelated to this work. Added 6 new regression tests (`StripLineWithTitle`, `TitleFrame`/`Embed`/`Emboss`/`Shadow`, `LegendWithTitleAndHeader`), all verified byte-for-byte. **This closes out the last of the concrete-GDI+ surface outside `ChartTypes/*.cs` and `ChartGraphics3D.cs`.**

Verified: build 0 errors, all 52 tests across the solution pass (51 `VisualRegressionTests`, up from 45 — 6 new — + 1 `Chart.Rdl.Tests`).

**D2/D3 investigated at the user's direction (2026-07-21) — gate confirmed still holding, one real independent fix made along the way.** Traced D2's actual seam (`ChartRenderingEngine.RenderingObject`, which picks the `gdiGraphics` field by `activeRenderingType`) and found swapping it by platform would be mechanically trivial but **unreachable dead code**: the real production entry point, `ChartImage.GetImage(float) : Bitmap`, unconditionally downcasts to `GdiRenderSurface` and returns a hard GDI+-typed `Bitmap` *before* `RenderingObject` is ever consulted — so on Linux this throws before backend selection would even matter. This is the same `Paint(Graphics)`/`ChartGraphics.Graphics` gap D1 already flagged, now traced one level further. D3 re-checked too: still blocked, and the blocker list grew — beyond `ChartGraphics3D.cs`/`MapAreasCollection.cs`/`RangeChart.cs`/`CalloutAnnotation.cs`, the **entire Gauge engine** (`GaugeContainer`/`GaugeGraphics`) also still calls the concrete overloads and was never previously counted here. **Real fix made despite the gate holding**: found `IRenderSurface.Encode(Stream, ChartImageFormat)` — a properly backend-agnostic encode method from D1 — had zero callers; `GetImage`/`Chart.Save` bypassed it via a raw `Bitmap.Save` + duplicated format-mapping switch instead. Added `ChartImage.SaveImage(Stream, ChartImageFormat, float)` (paints via the still-necessary `GdiRenderSurface` downcast, then encodes via the interface method instead of a raw `Bitmap.Save`) and rewired `Chart.Save(Stream, ChartImageFormat)` — the harness's own render path — to use it. Verified build 0 errors, all 52 tests pass byte-for-byte, zero behavior change on Windows. **D2/D3 themselves remain correctly not-done** — this was a narrow, independent cleanup found during the investigation, not progress on the milestones themselves.

**Gauge engine migration started (2026-07-21), user-directed ("Let's begin the Gauge engine") — full scoping + Milestone A equivalent done, tracked in new `tasks/gauge-gdi-type-abstraction.md`.** Scoping found the Gauge engine (~30,000 lines/253 files, never touched by this migration) already has a pre-A1-equivalent seam (`IGaugeRenderingEngine`/`RenderingEngine`/`GdiGraphics`, structurally identical to what Chart's `IChartRenderingEngine` looked like before its own Milestone A) and a smaller, more concentrated GDI+ surface (~249 construction sites/~30 files vs. Chart's ~1000/~24) — critically, **zero** `GraphicsPath.Widen`/`GraphicsPathIterator`/custom-arrow-cap sites, the trickiest Chart-engine gaps don't recur. User chose to relocate the Chart engine's pure resource interfaces (`IPen`/`IBrush`/`IChartFont`/`ITextFormat`/`IGraphicsPath`/`IRenderingResource`) to a new neutral namespace/folder, `Microsoft.Reporting.Rendering/`, rather than duplicate or cross-reference them — done as a mechanical, behavior-preserving move (`IClipRegion` turned out to have a real Chart-specific dependency, found only by attempting the move, and stayed put). Built Gauge's own `IGaugeDrawingResourceFactory` + `Rendering/Gdi/` adapter set (mirrors Chart's A2 adapters in shape, but a separate implementation per the chosen design) and added ~21 interface-typed overloads to `IGaugeRenderingEngine`/`GdiGraphics`/`RenderingEngine` (A3 equivalent) — infrastructure only, nothing calls them yet. Verified: build 0 errors, all 52 existing tests pass unchanged (purely additive surface).

**Gauge visual-regression harness built from scratch (2026-07-21), user-directed ("Proceed with the gauge visual regression harness").** No sample gauge definitions or baseline PNGs existed anywhere in `tests/` before this — the Gauge engine had zero render-path test coverage of any kind. Added `SampleGauges.cs` (mirrors `SampleCharts.cs`: builds a bare `GaugeContainer` directly against the internal engine API, no .rdlc/host needed) with `RenderSimpleCircularGauge` (default `CircularScale` + one `CircularPointer` at 65) and `RenderSimpleLinearGauge` (`LinearGauge` forced to `GaugeOrientation.Horizontal` in a 300x100 container — the default `Auto` orientation in a 300x300 square container produced overlapping/illegible labels, pre-existing engine behavior with a bad aspect ratio, not a bug, but not a usable first baseline). Added `GaugeVisualRegressionTests.cs` (mirrors `ChartVisualRegressionTests.cs`), reusing the existing `ImageComparer`/`Baselines/` machinery completely as-is. Rendered, visually inspected both PNGs (clean circular dial with needle at ~65, clean horizontal scale with triangular pointer at ~65), promoted directly to `Baselines/` — no `git stash` dance needed since this is first-ever coverage, not a re-verification. Verified: build 0 errors, full suite 54/54 passing (53 `VisualRegressionTests` — up from 51, +2 new — + 1 `Chart.Rdl.Tests`). **This unblocks Gauge B1/B2 real call-site conversion**, which can now be checked byte-for-byte the way Chart's conversions were — that and the clip-region abstraction (A4) remain not started.

**Implementation Plan:**
- [x] Phase 0: Time-boxed spike to measure real effort — **done**, see `tasks/chart-cross-platform-implementation.md` Spike Report
- [ ] Phase 1: Platform selection + graceful degradation (no crash on Linux)
- [ ] Phase 2: Abstract the pixel/output boundary (Bitmap/Graphics → SKSurface)
- [~] Phase 3: Finish Milestones B1b/B2/C1-C8 (`chart-gdi-type-abstraction.md`) — **C1-C8 substantially done** (see above); **B1b/B2 in progress**, scope re-measured larger than original L/XL sizing — then wire the spike's `SkiaChartGraphics`/`SkiaResourceFactory` into the real `Chart` render path (`RenderingType.Skia`)
- [ ] Phase 4: Backend factory + integration
- [~] Phase 5: Apply same pattern to gauge engine — **started** (`tasks/gauge-gdi-type-abstraction.md`, Milestone A + visual-regression harness done, see above) — **and decide Map.WebForms's scope** (a third GDI+-coupled engine, not yet started)
- [ ] Phase 6: Visual regression testing (extend the E0 harness — `tests/Microsoft.ReportViewer.DataVisualization.VisualRegressionTests/` — with a real Skia render path and a broader report corpus; the Gauge engine now has its own baseline coverage (`SampleGauges.cs`/`GaugeVisualRegressionTests.cs`) to build on, but it's still 2 samples deep vs. Chart's much broader corpus)

**References:**
- `tasks/chart-cross-platform-implementation.md` (corrected technical instructions with line refs)
- `tasks/chart-gdi-type-abstraction.md` (per-type task scope for replacing GDI+ types with interfaces + factory)
- `tasks/gauge-gdi-type-abstraction.md` (Gauge engine's sibling migration — scope, progress, findings)
- `tasks/chart-library-decision.md` (retraction notice explaining why OxyPlot was dropped)

### PDF Phase 1: SkiaSharp Graphics Migration (LOWER PRIORITY)

**Status:** Analysis complete, strategic decision pending  
**Effort:** 2-3 weeks  
**Risk:** VERY HIGH (complete graphics stack replacement)  
**Timeline:** Start after Excel phases 4-5 and chart decision  
**Blocker:** Metafile/EMF generation has NO cross-platform equivalent

**Recommendation:** Complete Excel work first, then decide on PDF strategy

**Reference:** `tasks/pdf-render-callstack-analysis.md` (complete 5-phase roadmap with implementation details)

---

## Completed Analysis & Documentation

| Category | Status | Details |
|----------|--------|---------|
| ✅ **.NET 10 Upgrade** | COMPLETE | All projects migrated, tests passing |
| ✅ **ImageSharp Integration** | COMPLETE | Image metrics cross-platform |
| ✅ **Excel Call Stack Analysis** | COMPLETE | 800+ lines, 3 major dependencies |
| ✅ **PDF Call Stack Analysis** | COMPLETE | 700+ lines, 7 major dependencies |
| ✅ **Dependency Inventory** | COMPLETE | All Windows deps documented |
| ✅ **Implementation Guides** | COMPLETE | 5 comprehensive guides created |
| ✅ **AGENTS.md Updated** | COMPLETE | Mission, guidelines, documentation standards |
| ✅ **Documentation Policy** | COMPLETE | No summary files, use TODO.md + docs/ |
| ✅ **Investigation Cleanup** | COMPLETE | Old templates removed |

---

## Documentation Available

**For Decision Makers:**
- `AGENTS.md` - Project mission and immediate priorities
- `tasks/rendering-comparison-analysis.md` - Excel vs PDF scope comparison

**For Architects:**
- `tasks/excel-render-callstack-analysis.md` - Complete Excel analysis
- `tasks/pdf-render-callstack-analysis.md` - Complete PDF analysis
- `tasks/chart-image-abstraction-analysis.md` - Architecture design

**For Developers (Phase 4):**
- `tasks/imagetype-enum-implementation.md` - Step-by-step guide with code examples
- `tasks/excel-quick-reference.md` - Quick lookup reference

**For Developers (Phase 5):**
- `tasks/chart-image-abstraction-analysis.md` - Design patterns and implementation roadmap

---

## Notes

- 🔄 All analysis complete - implementation-ready documentation provided
- 📋 Chart library replacement is blocking both Excel and PDF work
- ✅ No commits to repository (working copy only)
- 📝 Update TODO.md continuously as work progresses
- 📚 Maintain `docs/` folder synchronized with implementation changes
