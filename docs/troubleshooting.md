# Troubleshooting

## Common issues

### Renderer tests fail to build

Check that the required NuGet packages are restored and that the test project references the common rendering project correctly.

### PDF output is empty or visually sparse

The initial Linux PDF implementation is intentionally lightweight. If the output needs richer layout, the renderer contract should be expanded and another implementation should be introduced.

### Excel output is missing expected content

Verify the input payload type and confirm the renderer receives the expected data shape. The current implementation supports simple DataTable, DataSet, and scalar-value paths.

### Embedded resources are not written correctly

Confirm that the resource payload is exposed as a stream, string, byte array, or another supported object-backed format that the adapter can normalize.

### Analyzer warnings remain noisy

Some legacy Windows-specific paths still produce warnings. The current mitigation is to suppress the known warning categories for the legacy paths while new abstractions are introduced.

### Chart/Gauge visual regression test fails by a handful of pixels with no code change

GDI+'s anti-aliased rendering of **rotated** text is not perfectly deterministic across separate process runs on some machines — confirmed by testing fully unmodified, pre-existing code against its own freshly-generated baseline and seeing the identical few-pixel drift (found via `Label.PaintCircular`). This is not a real regression. `ImageComparer.CompareToBaseline` has an optional `maxDiffPixels` parameter (default `0`) for exactly this situation — add a narrow, explicitly-documented tolerance only to the specific affected test rather than weakening the harness generally.

### A chart/gauge conversion "passes" but isn't actually pixel-verified

Purely additive interface-typed surface (a new `*Resource` sibling method with no real caller yet) is only build-verified, not pixel-verified, until some real caller or a dedicated sample chart/gauge exercises it. Don't treat "build 0 errors + tests pass" as proof of correctness for code nothing renders yet — check whether the new path is actually exercised by an existing baseline before trusting it.

### A brush/pen getter looks safe to convert to an interface type but breaks callers

Watch for shared concrete-field arrays on helper/attrib classes (e.g. Gauge's `KnobStyleAttrib`/`NeedleStyleAttrib`/`MarkerStyleAttrib`/`BarStyleAttrib`) — individual getters look convertible in isolation, but all their results are consumed together by the same `FillPath(Brush, GraphicsPath)`/`DrawPath(Pen, GraphicsPath)` call. Converting one getter without converting the whole class plus its producers and consumer in one pass just adds unreachable dead code. Trace the actual real callers (not just the method signature) before concluding a method is blocked or safe to convert in place — a documented case (`BackFrame.GetBrush`) was initially assumed blocked and later found not to be, only by re-reading its actual callers.

### `SeriesChartType.Sunburst` throws `InvalidOperationException` on combine

Fixed: `SunburstChart.cs`'s `Name` property incorrectly returned `"TreeMap"` instead of `"Sunburst"`, causing a spurious "cannot be combined" exception for every Sunburst chart. Also note: Sunburst is driven entirely by RDL category groupings (`ChartMapper.RenderCategoryGrouping` → `ChartArea.CategoryNodes`) — `CategoryNode`/`CategoryNodeCollection` are `internal sealed` with no public constructor, so it cannot be exercised via direct `Chart`/`Series` construction. Test coverage for it lives in a separate project, `tests/Microsoft.ReportViewer.Chart.Rdl.Tests`, for this reason.

### Some converted GDI+ code paths can never be exercised by a test

`AxisScrollBar.Paint()`'s scroll-button drawing and `ImageAnnotation`'s design-mode "(no image)" text are permanently unreachable in this vendored/stripped build — `AxisScrollBar.IsVisible()` and `Chart.IsDesignMode()` are hardcoded to return `false`. Their conversions are complete and behavior-preserving but have no possible regression test; this is expected, not a gap to fix.

### A chart baseline shows visibly clipped or unusual text

`TextStyle.Frame`'s title-text baseline shows visible clipping in some cases — confirmed via `git stash` to be pre-existing GDI+ behavior, unrelated to the rendering-abstraction migration. Not a regression; don't "fix" it as part of unrelated conversion work.
