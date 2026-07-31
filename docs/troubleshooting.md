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

### `Version conflict detected for "Microsoft.CodeAnalysis.Common"`

Add `Microsoft.CodeAnalysis.CSharp.Workspaces`/`Microsoft.CodeAnalysis.Common` yourself first, pinned to a version matching your target framework (3.6.0 for .NET Core 3.1, 3.8.0 for .NET 5, 4.0.1 for .NET 6+).

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

### HarfBuzzSharp shaping crashes the process with `0xC0000005` (access violation in `hb_shape_full`), only after many shape calls

Not reproducible within a single isolated test — only appears once enough shaping calls have accumulated across a test run (or process). Root cause: hand-building a `HarfBuzzSharp.Blob`/`Face`/`Font` from a raw font-stream blob has a native lifetime/memory bug, regardless of whether the `Face`/`Font` is rebuilt per call or cached once per font instance. Fix: use `SkiaSharp.HarfBuzz.SKShaper` (the official HarfBuzzSharp+SkiaSharp integration package) instead of hand-rolling the native objects — see `docs/decisions.md`'s 2026-07-26 `SKShaper` entry. If you hit this crash signature anywhere else in this codebase, suspect the same hand-rolled-native-object pattern first.

### A chart baseline shows visibly clipped or unusual text

`TextStyle.Frame`'s title-text baseline shows visible clipping in some cases — confirmed via `git stash` to be pre-existing GDI+ behavior, unrelated to the rendering-abstraction migration. Not a regression; don't "fix" it as part of unrelated conversion work.

### A TrueType Collection (`.ttc`) font embeds as a suspiciously huge, spec-invalid PDF font stream

If a report resolves to a `.ttc`-backed family (common for CJK: `simsun.ttc`, `msyh.ttc`, `mingliub.ttc`; also some Latin families like `cambria.ttc`), naively embedding the raw font bytes stuffs the *entire multi-face container* (often 10-20MB) into PDF's `/FontFile2`, which per spec 9.9 must hold a single TrueType font program, not a collection. Root cause: a raw `'ttcf'` blob correctly fails outline-format detection (it isn't a single-format font program), but a naive "not CFF → must be TrueType" check treats "unsupported" the same as "TrueType" and lets the whole container through unchanged. Fix: extract the one face SkiaSharp actually resolved (`SKTypeface.OpenStream(out int ttcIndex)`) into a standalone single-face sfnt before embedding — see `docs/decisions.md`'s matching 2026-07-27 entry. If an embedded TrueType font looks unexpectedly large or a PDF viewer rejects it, check whether the source family is TTC-backed first.

### Some Unicode code points can't be written as raw character literals in C# — the lexer treats them as line terminators

The C# spec's "new-line" character set — not just CR/LF, but also NEL (U+0085), Line Separator (U+2028), and Paragraph Separator (U+2029) — can't appear unescaped inside a non-verbatim `char`/`string` literal; pasting one in raw (e.g. building `UnicodeLineBreakAnalyzer`'s break-class `switch`, which needs a `case` for exactly these code points) produces `CS1010`/`CS1011`/`CS1012`-style errors, or silently splits the literal across lines if the surrounding code happens to still parse. Always write these three as `\uXXXX` escapes. (Other invisible/control code points relevant to the same table — vertical tab U+000B, form feed U+000C, zero-width space U+200B — are *not* lexically restricted this way and compile fine raw, but escaping them too is still good practice since they're invisible in an editor.)
