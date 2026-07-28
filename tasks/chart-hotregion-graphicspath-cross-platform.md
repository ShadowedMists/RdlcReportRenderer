# Chart HotRegion/GraphicsPath cross-platform gap

**Status: `HotRegionsList`/`HotRegion`'s own internals are fully ported to `IGraphicsPath` (2026-07-28). The remaining ~30-file caller migration (chart-type files still building a raw `System.Drawing.Drawing2D.GraphicsPath` before calling `AddHotRegion`) is scoped below but not started, and turned out NOT to be the next blocker for Linux rendering — see "Why this isn't currently blocking" below.**

## Background

Discovered while chasing the Chart default-Font gap (`tasks/chart-default-font-cross-platform.md`): once Legend's and Axis's Font-construction gaps were fixed, the WSL crash moved to `HotRegionsList.AddHotRegion`'s `new GraphicsPath(pts, types)` — `GraphicsPath` construction is exactly as impossible on Linux as `Font` construction (same Phase 0 finding: GDI+ can't construct *any* `System.Drawing` object on Linux, not even a bare primitive).

## Fix done (2026-07-28): HotRegionsList's own internals

- `HotRegion.Path`'s type changed from `GraphicsPath` to `IGraphicsPath` (`HotRegion.cs`); dropped `using System.Drawing.Drawing2D;` (no longer needed there).
- `HotRegionsList.cs`'s `AddHotRegion` overload pairs were flipped so the **interface-typed overload is now the primary implementation** (stores/builds via `IGraphicsPath`, no `GraphicsPathIterator`/concrete construction), and the **concrete `GraphicsPath`-typed overload bridges into it** via `graph.ResourceFactory.CreatePath(path.PathPoints, path.PathTypes)` — for the ~30 callers not yet migrated (see below), not because HotRegionsList itself needs the concrete type.
- The one ellipse-building overload (`AddHotRegion(int insertIndex, ChartGraphics graph, float x, float y, float radius, ...)`) that used to build `new GraphicsPath()` + `AddEllipse(...)` directly now builds via `graph.ResourceFactory.CreatePath()` + the same `AddEllipse` call (already on the `IGraphicsPath` interface).
- `CheckHotRegions`'s multi-subpath hit-testing, which used the GDI+-only `GraphicsPathIterator`/`NextMarker`, was replaced with a `HasPathMarkers`/`SplitAtMarkers`/`CopySegment` helper trio — same pattern already established in `CalloutAnnotation.cs`'s private `SplitAtMarkers` (reads the `PathMarker` bit, `0x20`, directly off `PathTypes` instead of depending on `GraphicsPathIterator`). Falls back to testing the whole path directly (preserving its exact `FillMode`) when there are no markers at all, which is the common case — only pays the split cost for genuinely multi-subpath paths.
- **Real bug caught during verification, then fixed**: the first pass made the interface-typed overloads store the caller's `IGraphicsPath` reference directly (`hotRegion.Path = path;`). This broke immediately — some callers (e.g. `CalloutAnnotation`'s `foreach (... in SplitAtMarkers(...)) { using (graphicsPath2) { AddHotRegion(..., graphicsPath2, ...); } }`) dispose their path right after the `AddHotRegion` call returns, since they assumed (correctly, under the *old* code) that `AddHotRegion` copied the data out via `PathPoints`/`PathTypes` rather than keeping the reference. `VisualRegressionTests` caught this immediately: 3 failures (`CalloutAnnotationHitTestTests.PerspectiveCallout_*`), each throwing `System.ArgumentException` from `GdiGraphicsPath.get_PathTypes()` on what turned out to be a disposed native path handle, surfaced only later at `HitTest`/`CheckHotRegions` time (long after the disposing `using` block had already run). **Fixed** by having every interface-typed `AddHotRegion` overload build its own owned copy — `hotRegion.Path = graph.ResourceFactory.CreatePath(path.PathPoints, path.PathTypes);` — instead of assigning the caller's reference directly. This is the portable equivalent of exactly what the *old* concrete-only code did (`new GraphicsPath(path.PathPoints, path.PathTypes)`), just via the interface factory instead of the concrete type — so `HotRegion` still always owns an independent copy, safe regardless of what the caller does with its own path afterward.
- **Verified**: `dotnet build --no-incremental` 0 errors. All 137 `VisualRegressionTests` + 187 `Chart.Rdl.Tests` pass on Windows (including the 3 that caught the disposal bug, confirmed fixed on rerun). WSL rebuild + `SimpleBarChart_RendersViaSkia_MatchesBaseline`: the crash moved **completely past** `HotRegionsList`/`GraphicsPath` — to a new, unrelated GDI+ wall, `Label.Paint`'s raw `new StringFormat()` (see `tasks/chart-default-font-cross-platform.md`'s "New gap found: Label.Paint's raw StringFormat construction" section). Direct proof this specific gap is closed, not just moved.

## Why this isn't currently blocking (aggregate pass count didn't move)

`VisualRegressionTests` stayed at 30/137 passing after this fix — expected, not a sign it didn't work. Every real chart-rendering test still hits `Label.Paint`'s `StringFormat` wall *earlier* in the paint order (`ChartArea.Paint` → `Axis.Paint` → `Label.Paint`, which runs well before hit-region registration for most elements). The remaining caller migration below is real, necessary follow-on work for full Linux hit-testing/tooltip support, but it is not what's currently stopping a chart from rendering at all — `StringFormat` is. Prioritize that gap first; come back to this one once `StringFormat` (and whatever comes after it) is cleared and hit-testing is reachable in a WSL test run again.

## Remaining scope: ~30-file caller migration (not started)

Every one of these call sites builds its own local `GraphicsPath graphicsPath = new GraphicsPath(); ...` (or similar) before passing it into one of `HotRegionsList`'s concrete-typed `AddHotRegion` overloads. Now that `HotRegionsList` bridges the concrete type safely (via `CreatePath(PathPoints, PathTypes)`), these calls **do not crash Windows** and **do not currently block Linux rendering either** (they're all unreachable until `StringFormat` clears) — but each one still constructs a real `System.Drawing.Drawing2D.GraphicsPath` locally, which itself throws on Linux the moment that code path is actually reached. Full migration means each of these files building its path via `graph.ResourceFactory.CreatePath()`/`IGraphicsPath`'s builder API instead, and passing the interface-typed overload directly (skipping the bridge entirely).

Files with `AddHotRegion(...)` calls passing a local `GraphicsPath`-typed variable (found via `grep -rn "AddHotRegion(" Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms*`, 2026-07-28 — re-grep before starting, this list may drift):

- `Microsoft.Reporting.Chart.WebForms.ChartTypes\SunburstChart.cs`
- `Microsoft.Reporting.Chart.WebForms.ChartTypes\StepLineChart.cs` (3 sites)
- `Microsoft.Reporting.Chart.WebForms.ChartTypes\StackedColumnChart.cs`
- `Microsoft.Reporting.Chart.WebForms.ChartTypes\StackedBarChart.cs`
- `Microsoft.Reporting.Chart.WebForms.ChartTypes\StackedAreaChart.cs` (2 sites)
- `Microsoft.Reporting.Chart.WebForms.ChartTypes\RangeChart.cs`
- `Microsoft.Reporting.Chart.WebForms.ChartTypes\RadarChart.cs` (2 sites)
- `Microsoft.Reporting.Chart.WebForms.ChartTypes\PointChart.cs`
- `Microsoft.Reporting.Chart.WebForms.ChartTypes\PieChart.cs` (2 sites)
- `Microsoft.Reporting.Chart.WebForms.ChartTypes\LineChart.cs` (2 sites)
- `Microsoft.Reporting.Chart.WebForms.ChartTypes\KagiChart.cs`
- `Microsoft.Reporting.Chart.WebForms.ChartTypes\FunnelChart.cs` (6 sites)
- `Microsoft.Reporting.Chart.WebForms.ChartTypes\FastLineChart.cs`
- `Microsoft.Reporting.Chart.WebForms.ChartTypes\ColumnChart.cs`
- `Microsoft.Reporting.Chart.WebForms.ChartTypes\BarChart.cs`
- `Microsoft.Reporting.Chart.WebForms.ChartTypes\AreaChart.cs` (2 sites)
- `Microsoft.Reporting.Chart.WebForms\ArrowAnnotation.cs`
- `Microsoft.Reporting.Chart.WebForms\TextAnnotation.cs`
- `Microsoft.Reporting.Chart.WebForms\Axis.cs` (4 sites — `GridLines`/`Axis` hot regions, distinct from the already-fixed `autoLabelFont` sites)
- `Microsoft.Reporting.Chart.WebForms\TickMark.cs`
- `Microsoft.Reporting.Chart.WebForms\StripLine.cs` (2 sites)
- `Microsoft.Reporting.Chart.WebForms\ChartGraphics3D.cs` (many sites, 3D painters)
- `Microsoft.Reporting.Chart.WebForms\CalloutAnnotation.cs` — **already fixed**, builds via `IGraphicsPath`/`SplitAtMarkers` already (this is the file whose established pattern the `HotRegionsList` fix copied)
- `Microsoft.Reporting.Chart.WebForms\ChartGraphics.cs` (multiple sites)
- `Microsoft.Reporting.Chart.WebForms\Grid.cs` (2 sites)
- `Microsoft.Reporting.Chart.WebForms\Label.cs`
- `Microsoft.Reporting.Chart.WebForms\LineAnnotation.cs`
- `Microsoft.Reporting.Chart.WebForms\PolylineAnnotation.cs` (2 sites)

Not all of these necessarily build the path themselves right at the call site — some receive an already-local `graphicsPath`/`path` variable built earlier in the same method or a helper; each needs individual tracing (don't assume the fix is purely mechanical — some of these paths may also feed *drawing* calls, not just hit-testing, in which case they may already be `IGraphicsPath`-typed elsewhere and this is just a matter of not re-widening to concrete for the `AddHotRegion` call).

Gauge's `Microsoft.Reporting.Gauge.WebForms\HotRegionList.cs` has the identical shape of gap (already noted in `docs/platform-support.md`) — same fix pattern applies there once Chart's version is done, lower priority since Gauge has no Skia backend yet at all.

## Proposed tasks

1. Re-grep the caller list above (may have drifted) and trace each file's local `GraphicsPath` variable back to its construction site.
2. Port each file's local path construction to `IGraphicsPath` (via `graph.ResourceFactory.CreatePath()` + the builder API, or `CreatePath(points, types)` if built from existing point arrays) and switch the `AddHotRegion` call to the interface-typed overload directly.
3. Verify per-file with the full Windows test suite (137+187) after each batch — byte-identical baselines expected (hit-testing doesn't affect rendered pixels, but any local path is very likely *also* used for drawing in the same method, so pixel regressions are a real risk to watch for, not just a formality).
4. Once `Label.Paint`'s `StringFormat` gap (and whatever's found after it) clears enough that a WSL test run reaches actual hit-testing code, re-verify via WSL that these call sites are the actual next blocker before investing further — don't assume based on this file's static analysis alone.
