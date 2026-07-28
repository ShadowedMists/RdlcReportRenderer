# Chart/Gauge default-Font cross-platform gap

**Status: object construction fixed (2026-07-28); rendering-time measurement still blocked.** See `docs/decisions.md`'s "Chart/Gauge model classes could not even be constructed on Linux" entry for the full discovery narrative — this file tracks the remaining open scope.

## What this covers

`Microsoft.Reporting.Chart.WebForms`/`Microsoft.Reporting.Gauge.WebForms`'s model classes (`Title`, `Legend`, `Axis`, `Series`/`DataPointAttributes`, `Annotation`, `FinancialMarker`, `LegendCellColumn`, `StripLine`, `ChartPicture`; Gauge's `CustomLabel`, `GaugeLabel`, `LinearLabelStyle`, `LinearPinLabel`, `NumericIndicator`, `State`, `StateIndicator`) each expose a public `Font` property with a sensible default value (e.g. "Microsoft Sans Serif, 8pt") for when the report/user code never explicitly sets one.

## Current state

**Fixed (2026-07-28):** every one of these classes used to construct its default `Font` **eagerly**, in a field initializer (`private Font font = new Font(...)`) — meaning simply constructing a `Chart`/`Legend`/`Axis`/etc. object crashed immediately on Linux, since `System.Drawing.Font` construction requires GDI+, which cannot construct anything at all on Linux under .NET 10 (even with `libgdiplus` installed — the Phase 0 spike finding, `docs/platform-support.md`). All of the above were converted to lazy backing fields (`private Font font;` + `return font ??= new Font(...)` in the getter), so building the object graph itself no longer touches GDI+ unless something actually reads `.Font`.

**Still open:** GDI+ cannot construct `Font`/`FontFamily` **at all** on Linux, lazily or not — so the FIRST time any code path actually needs a live default `Font` instance (not a user-supplied one), it still crashes. Confirmed via `Legend.GetOptimalSize` → `Legend.Font`'s getter, reached from `ChartPicture.Paint`→`Resize`→`CalcLegendPosition`→`LegendCollection.CalcLegendPosition` — i.e., **any chart with a Legend still fails to render on Linux**, not at construction time anymore, but at layout/measurement time. `VisualRegressionTests` improved from 29/137 to only 30/137 passing after the construction-time fix — most remaining failures now hit this same wall.

## Why this is a different, harder problem than the construction-time bug

The construction-time bug was fixable with laziness alone, because nothing actually *needed* the Font value merely to exist as an object graph. The measurement-time problem is different: `Legend.GetOptimalSize` calls `chartGraph.MeasureStringAbs("W", Font)`, which bridges via `resourceFactory.WrapFont(font)` (`ChartGraphics.cs`) to the already-correct, Skia-safe `MeasureStringAbs(string, IChartFont)` overload — so the *measurement infrastructure itself* is fully migrated and correct. The problem is one step earlier: `WrapFont` needs an actual `System.Drawing.Font` instance to wrap, and constructing that instance (even just to read its `.FontFamily.Name`/`.Size`/`.Style` so a portable `IChartFont` could be built instead) is exactly the operation that's permanently impossible on Linux.

## Proposed fix (not attempted — scope only)

`IDrawingResourceFactory` already has `IChartFont CreateFont(string familyName, float sizeInPoints)` (and overloads with style/unit) — this constructs an `IChartFont` directly (via `SKTypeface`/`SKFont` on the Skia backend) **without ever touching `System.Drawing.Font`**. The fix is for call sites like `Legend.GetOptimalSize` to detect "this Font was never explicitly set by the caller" (the lazy backing field is still `null`) and, in that case, call `chartGraph.ResourceFactory.CreateFont(ChartPicture.GetDefaultFontFamilyName(), 8f)` directly instead of reading the `.Font` property (which would materialize a real, crash-inducing `System.Drawing.Font`). This means:
1. Each affected class needs an internal way to distinguish "default, never set" from "explicitly set by caller" — the raw nullable backing field already does this; it's a matter of exposing that distinction to callers (an internal `bool IsFontExplicitlySet` or similar, or a parallel `IChartFont` accessor that only falls back to `CreateFont` on the same lazy check).
2. Every rendering-time consumer of these `.Font` properties needs updating to prefer the interface-typed accessor over the concrete one, matching this repo's established "dual-overload strategy" (`docs/rendering-abstractions.md`) — add the new accessor alongside the existing property, migrate real callers incrementally.
3. `Legend.GetOptimalSize` is the first confirmed call site; a full fix needs a sweep for every other place that reads one of these default-Font properties for measurement/drawing (Axis labels, Series data-point labels, Annotations, StripLine titles, etc.) — not yet inventoried.

This is a real, moderately-sized migration (in the same spirit as the original Chart/Gauge GDI+→interface work), not a quick fix — scoped here for whoever picks it up next.

## Related, deliberately not fixed this pass

`DynamicImageInstance.GetImage`'s exception handler falls back to `CreateExceptionImage`, which is itself 100% GDI+ (`new Bitmap`, `Graphics.FromImage`, `Pen`/`Brush`/`Font`) and silently fails the same way on Linux (own inner catch swallows the failure, returns `null`). This is why a chart/gauge that fails to render on Linux today produces a *silently blank* space rather than any visible error or exception — worth knowing when debugging, not fixed here since it's a defensive fallback path working as designed everywhere except this platform.

## Proposed tasks

1. Inventory every rendering/measurement call site (not just `Legend.GetOptimalSize`) that reads one of the affected `.Font` properties expecting a real, usable `System.Drawing.Font` — not yet done.
2. Design and add the "prefer `IChartFont` over concrete `Font` when using the *default* value" accessor pattern described above, for at least the highest-traffic classes (`Legend`, `Axis`, `Title`).
3. Re-run `VisualRegressionTests` under WSL after each increment to track how many of the 106 remaining failures clear.
4. Once Chart's Legend/measurement path is fixed, re-test `SunburstChartWithCategoryHierarchy_MatchesBaseline` (IMAGE format) — it has a Legend and was the original motivating test for this whole investigation.
5. Consider whether `CreateExceptionImage`'s own GDI+ dependency is worth a portable fallback (a solid-color/text-free placeholder built via SkiaSharp) so failures at least become *visible* on non-Windows instead of silently blank — separate, smaller task.
