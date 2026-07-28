# Chart/Gauge default-Font cross-platform gap

**Status: object construction fixed (2026-07-28); rendering-time measurement still blocked.** See `docs/decisions.md`'s "Chart/Gauge model classes could not even be constructed on Linux" entry for the full discovery narrative — this file tracks the remaining open scope.

## What this covers

`Microsoft.Reporting.Chart.WebForms`/`Microsoft.Reporting.Gauge.WebForms`'s model classes (`Title`, `Legend`, `Axis`, `Series`/`DataPointAttributes`, `Annotation`, `FinancialMarker`, `LegendCellColumn`, `StripLine`, `ChartPicture`; Gauge's `CustomLabel`, `GaugeLabel`, `LinearLabelStyle`, `LinearPinLabel`, `NumericIndicator`, `State`, `StateIndicator`) each expose a public `Font` property with a sensible default value (e.g. "Microsoft Sans Serif, 8pt") for when the report/user code never explicitly sets one.

## Current state

**Fixed (2026-07-28):** every one of these classes used to construct its default `Font` **eagerly**, in a field initializer (`private Font font = new Font(...)`) — meaning simply constructing a `Chart`/`Legend`/`Axis`/etc. object crashed immediately on Linux, since `System.Drawing.Font` construction requires GDI+, which cannot construct anything at all on Linux under .NET 10 (even with `libgdiplus` installed — the Phase 0 spike finding, `docs/platform-support.md`). All of the above were converted to lazy backing fields (`private Font font;` + `return font ??= new Font(...)` in the getter), so building the object graph itself no longer touches GDI+ unless something actually reads `.Font`.

**Still open:** GDI+ cannot construct `Font`/`FontFamily` **at all** on Linux, lazily or not — so the FIRST time any code path actually needs a live default `Font` instance (not a user-supplied one), it still crashes. Confirmed via `Legend.GetOptimalSize` → `Legend.Font`'s getter, reached from `ChartPicture.Paint`→`Resize`→`CalcLegendPosition`→`LegendCollection.CalcLegendPosition` — i.e., **any chart with a Legend still fails to render on Linux**, not at construction time anymore, but at layout/measurement time. `VisualRegressionTests` improved from 29/137 to only 30/137 passing after the construction-time fix — most remaining failures now hit this same wall.

## Why this is a different, harder problem than the construction-time bug

The construction-time bug was fixable with laziness alone, because nothing actually *needed* the Font value merely to exist as an object graph. The measurement-time problem is different: `Legend.GetOptimalSize` calls `chartGraph.MeasureStringAbs("W", Font)`, which bridges via `resourceFactory.WrapFont(font)` (`ChartGraphics.cs`) to the already-correct, Skia-safe `MeasureStringAbs(string, IChartFont)` overload — so the *measurement infrastructure itself* is fully migrated and correct. The problem is one step earlier: `WrapFont` needs an actual `System.Drawing.Font` instance to wrap, and constructing that instance (even just to read its `.FontFamily.Name`/`.Size`/`.Style` so a portable `IChartFont` could be built instead) is exactly the operation that's permanently impossible on Linux.

## Proposed fix — revised and deepened after reading `Legend.GetOptimalSize` in full (2026-07-28)

**The original scoping above (this section, prior to this update) underestimated the problem.** It assumed the only issue was a one-time measurement call (`MeasureStringAbs("W", Font)`) that could be routed around by detecting "was Font ever explicitly set." Reading `GetOptimalSize` in full shows the real blocker is deeper and *unconditional*: immediately after the first measurement calls, line `autofitFont = new Font(Font, Font.Style);` runs **whenever `legendItems.Count > 0`**, regardless of whether `AutoFitText` is even enabled, regardless of whether the user explicitly set a `Font`. Then, inside the auto-fit loop (only when `AutoFitText` is on), it constructs *another* new `Font` at a smaller size on every iteration: `autofitFont = new Font(Font.FontFamily, num2, Font.Style, Font.Unit);` — repeatedly, to find a size that fits.

This means the real, pervasive pattern isn't "a default Font was never set" — it's that **Legend's whole auto-fit-sizing mechanism is built around constructing new `System.Drawing.Font` instances at computed sizes**, as its core operating technique, for *any* legend with content, default or user-supplied font alike. The "was it explicitly set" distinction from the original scoping doesn't help here: even a user-supplied `Font` gets re-constructed at a new size for auto-fit, and that re-construction is exactly what's impossible on Linux.

**Grep confirms the same pattern recurs elsewhere** — `Axis.cs` has multiple `autoLabelFont = new Font(base.LabelStyle.Font.FontFamily, ..., base.LabelStyle.Font.Style, GraphicsUnit.Point)` sites (axis label auto-fit, the same shape as Legend's), and likely other auto-fit-adjacent code not yet inventoried (`ChartArea.cs:1626` does the same for `AxisX.autoLabelFont`).

### What a real fix looks like

This is not a call-site patch — it's a genuine port of the "compute an auto-fit font at a candidate size" operation from constructing `System.Drawing.Font` to constructing `IChartFont` (via `IDrawingResourceFactory.CreateFont(familyName, size, style, unit)`, which already exists and already builds a Skia-native font with no `System.Drawing.Font` involved at all). Concretely, for `Legend` (and the same shape for `Axis`):
1. Change `autofitFont`'s type (or add a parallel field) from `Font` to `IChartFont`, and change every construction site (`new Font(Font, Font.Style)`, `new Font(Font.FontFamily, num2, Font.Style, Font.Unit)`) to `chartGraph.ResourceFactory.CreateFont(...)`, reading `FontFamily.Name`/`Style`/`Unit` as plain data once (this one read of the *base* `Font` — not a resize — is the only place a real `System.Drawing.Font` construction is still needed, and only when a non-default value was actually set; the default-family-name case can skip the concrete `Font` entirely by using `ChartPicture.GetDefaultFontFamilyName()` directly).
2. `MeasureStringAbs` already has an `IChartFont`-taking overload (`ChartGraphics.cs`) — switch the measurement calls to use it with the new `IChartFont`-typed `autofitFont` instead of the concrete `Font`.
3. Whatever eventually *draws* using `autofitFont` (not yet traced — likely `LegendItem`/`LegendCell` drawing code) needs to accept `IChartFont` too, or the dual-overload bridge pattern already established for the rest of Chart (`docs/rendering-abstractions.md`'s "dual-overload strategy") needs to apply here as well.
4. This is real, non-trivial work in the same spirit as the original Chart/Gauge GDI+→interface migration (which took multiple sessions/milestones) — **not a quick fix**, and shouldn't be rushed as a single autonomous-loop increment. Whoever picks this up next should budget for tracing the full `autofitFont`/drawing call chain before touching code, the same way the original Milestone A/B/C Chart migration did.

## Related, deliberately not fixed this pass

`DynamicImageInstance.GetImage`'s exception handler falls back to `CreateExceptionImage`, which is itself 100% GDI+ (`new Bitmap`, `Graphics.FromImage`, `Pen`/`Brush`/`Font`) and silently fails the same way on Linux (own inner catch swallows the failure, returns `null`). This is why a chart/gauge that fails to render on Linux today produces a *silently blank* space rather than any visible error or exception — worth knowing when debugging, not fixed here since it's a defensive fallback path working as designed everywhere except this platform.

## Proposed tasks

1. Trace `Legend`'s full `autofitFont` lifecycle end to end — every construction site *and* every place it's later read/drawn with — before changing anything, the same discipline the original Chart Ports & Adapters migration used per-method/per-file (`docs/rendering-abstractions.md`).
2. Port `Legend`'s auto-fit-sizing construction sites (`GetOptimalSize`) from `new Font(...)` to `IDrawingResourceFactory.CreateFont(...)`, changing `autofitFont`'s type to `IChartFont` and updating its measurement (`MeasureStringAbs`) and drawing consumers to match — a real, careful port, not a call-site patch (see the deepened scope above).
3. Repeat for `Axis`'s equivalent `autoLabelFont` auto-fit pattern (`Axis.cs`, `ChartArea.cs:1626`) — same shape, separate class, not yet inventoried in depth.
4. Re-run `VisualRegressionTests` under WSL after each increment to track how many of the 106 remaining failures clear.
5. Once Chart's Legend/measurement path is fixed, re-test `SunburstChartWithCategoryHierarchy_MatchesBaseline` (IMAGE format) — it has a Legend and was the original motivating test for this whole investigation.
6. Consider whether `CreateExceptionImage`'s own GDI+ dependency is worth a portable fallback (a solid-color/text-free placeholder built via SkiaSharp) so failures at least become *visible* on non-Windows instead of silently blank — separate, smaller task.
