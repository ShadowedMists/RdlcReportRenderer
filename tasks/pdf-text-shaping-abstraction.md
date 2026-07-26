# RichText text-shaping cross-platform abstraction (PDF's real blocker)

**Status (2026-07-26): scoped; spike and verification tooling done, font/shaping layers not started.** This supersedes the "P4" line item in `pdf-render-callstack-analysis.md`/`pdf-quick-reference.md` — those docs' P1-P3 items are done; this doc is the detailed scope for what remains.

## Why this exists as its own doc

The 2026-07-24 PDF re-scoping correctly found that `PDFWriter` itself has no Metafile/GDI+-drawing dependency, but under-scoped its one remaining hard item ("Uniscribe complex-script shaping, ~1,375 lines") as a bounded, RTL/CJK-only concern. Implementing P1-P3 required tracing it for real, and two facts change the picture:

1. **There is no simple/Latin fast path.** `TextRun.ShapeAndPlace` (`Microsoft.ReportViewer.Common/Microsoft.ReportingServices.Rendering.RichText/TextRun.cs:356-451`) calls `Win32.ScriptShape`/`ScriptPlace` unconditionally for every run. `TextRun.GetCharset()` (`TextRun.cs:502-515`) only affects font-fallback/charset selection, never skips shaping. Uniscribe is this engine's only text-shaping backend, for all scripts, not a complex-script add-on.
2. **The pipeline constructs GDI+ `Font` objects and real Win32 `HFONT`s upstream of Uniscribe, for every run.** `FontCache.CreateFont` (`RichText/FontCache.cs:174-195`) calls `CreateGdiPlusFont`→`new Font(...)` (`FontCache.cs:309-409`) and `Win32.CreateFont` (`FontCache.cs:221`, a real `CreateFontW` P/Invoke) per distinct font/run key. `new Font(...)` is independently blocked on Linux (Phase 0 spike, `docs/platform-support.md`) — a Uniscribe replacement alone would not be sufficient; `FontCache`/`CachedFont`'s GDI+ Font construction is an equally hard, separate wall in the same call path.

## Full P/Invoke call-site inventory (verified 2026-07-26)

| Function | Call site | Purpose |
|---|---|---|
| `ScriptItemize` | `Paragraph.cs:217` (via `LineBreaker.cs:635`) | Splits mixed-direction/mixed-script text into runs (bidi/script segmentation) |
| `ScriptBreak` | `Paragraph.cs:179` (used throughout `LineBreaker.cs`, e.g. `:440,539,572`) | Per-character `SCRIPT_LOGATTR` soft-break/word-break flags — the actual data `LineBreaker` folds lines on |
| `ScriptShape` | `TextRun.cs:375,380,387,398,422,438` | Text → glyph indices + cluster map + visual attributes |
| `ScriptPlace` | `TextRun.cs:459,463,467` | Glyph indices → advance widths (`GOFFSET`/`ABC`) |
| `ScriptGetFontProperties` | `TextRun.cs:409` | Fallback/default-glyph detection for CJK/font fallback |
| `ScriptGetLogicalWidths` | `TextRun.cs:273` | Per-character logical widths for caret/selection |
| `ScriptTextOut` | `TextBox.cs:541,599` | Actual glyph drawing |
| `ScriptXtoCP`/`ScriptCPtoX` | `RichTextRenderer.cs:460,900` | Hit-testing/caret (WinForms viewer only, not PDF) |
| `ScriptFreeCache` | `ScriptCacheSafeHandle.cs:16` | Cache cleanup |
| `ScriptGetProperties` | `ScriptProperties.cs:29` | Script property table (`TextRun.IsComplex`, `TextRun.cs:35,506`) |
| `ScriptLayout` | `TextLine.cs:256` | Visual↔logical reordering for bidi/RTL |
| `CreateFont`/`GetTextMetrics` | `FontCache.cs:221`, `CachedFont.cs:105` | Real Win32 `HFONT` + `TEXTMETRIC`, not GDI+ |
| `new Font(...)` | `FontCache.cs:333,352,372,387,391` | GDI+ font object, independently blocked on Linux (Phase 0 spike) |

## Blast radius (this is not PDF-scoped work)

`LineBreaker.Flow`/`TextBox.Render`/`TextBox.MeasureFullHeight` are called from:
- `Microsoft.ReportingServices.Rendering.ImageRenderer/Renderer.cs:1145,1147,1246,1250` — shared by `PDFWriter` **and** the Windows-only `ImageWriter` (EMF/TIFF) renderer.
- `RichText/RichTextRenderer.cs:1118` and `Microsoft.Reporting.WinForms/RenderingTextBox.cs:385` — the WinForms on-screen viewer's own rendering.
- `Rendering.HPBProcessing/PageContext.cs:434,444` and `Rendering.SPBProcessing/PageContext.cs:548` — the shared pagination/page-break engines used by *every* renderer, not just PDF.

`HtmlRenderer` and `ExcelRenderer` do **not** depend on this pipeline (confirmed via grep — no references to `LineBreaker`/`RichText.TextBox`). Fixing this benefits PDF, Image/EMF, the WinForms viewer, and pagination correctness together; there is no PDF-only shortcut to route around it.

## What a replacement needs to produce

`LineBreaker.Flow(TextBox, Win32DCSafeHandle hdc, float dpiX, FontCache, FlowContext, bool keepLines, out float height) : List<Paragraph>` (line-break offsets/`TextLine`s) and `TextBox.Render(...)`/`MeasureFullHeight(...)` (glyph draw + height) are the two entry points `PDFWriter`/`Renderer.cs` call. Internally they need, per run: glyph indices (`GlyphShapeData.Glyphs`), clusters, `SCRIPT_VISATTR`, advance widths (`GlyphData.Advances`/`ABC`), `GOFFSET` glyph offsets, and `SCRIPT_LOGATTR` break flags — i.e. a shaper producing glyph ids + advances + break opportunities + a bidi-reordered run list, matching the shapes already defined in `TexRunShapeData.cs`/`GlyphShapeData.cs`/`GOFFSET.cs`.

## Recommended phased plan

Given the size (this is new shaping/line-breaking logic, not a resource-type port like Chart/Gauge's migration — expect it to need its own visual-verification tooling, since none exists for text today), do not attempt this as one pass:

1. **Spike — done (2026-07-26):** `tests/ReportViewerCore.LinuxRenderers.Tests/TextShapingSpikeTests.cs` shapes a plain-Latin string through `HarfBuzzSharp` (`Face`/`Font`/`Buffer`, using `SKTypeface.OpenStream` to get the font blob — no `System.Drawing`/Win32 anywhere in the call path) and gets real glyph ids + positive advances, one glyph per character as expected for non-ligated Latin text; a companion test confirms `SkiaSharp.SKFont.GetGlyphs`/`GetGlyphWidths`/`Metrics` give usable glyph/metric data the same way. Packages: `HarfBuzzSharp`/`HarfBuzzSharp.NativeAssets.Linux` 14.2.1.1, `SkiaSharp`/`SkiaSharp.NativeAssets.Linux` 3.119.1 (matches Chart's existing SkiaSharp version). This validates the API shape end to end; it is deliberately not wired into any production class yet — that's steps 2-4 below.
2. **Font layer:** introduce a `Microsoft.Reporting.Rendering`-style port (mirroring Chart/Gauge's `IDrawingResourceFactory` pattern) for `CachedFont`'s two GDI+-typed members (`Font`, `Hfont`) so a Skia-backed `CachedFont` variant can exist alongside the Windows one — dual-overload style, not a retype-in-place, per this repo's established migration convention (`docs/rendering-abstractions.md`).
3. **Shaping layer:** implement a `SkiaTextShapingEngine`/similar behind an interface `TextRun.ShapeAndPlace` can call, covering LTR non-complex scripts first (the realistic majority case for business reports: Latin, Cyrillic, Greek, numbers, punctuation) via HarfBuzzSharp. Document RTL/complex-script (Arabic/Hebrew/Thai/Indic/CJK-vertical) as a known, explicit gap until a bidi/line-break algorithm is selected (ICU4N or a hand-rolled Unicode line-break implementation) and validated the same way.
4. **Verification infrastructure — done (2026-07-26), but not a golden-image diff.** Chart's `VisualRegressionTests`/`ImageComparer` pattern (checked-in baseline PNG, pixel-tolerance diff) was considered and deliberately **not** replicated for text, for two reasons: (a) `AGENTS.md`'s Testing Philosophy explicitly prefers behavioral tests and says to avoid pixel-perfect comparisons where possible; (b) `SKTypeface.Default` resolves to whatever font is installed on the host — not guaranteed identical between this Windows dev box and a Linux CI/production image with no fontconfig fonts installed, so a pixel-exact baseline generated here would not be reproducible there, and no font file is currently vendored in-repo to pin against (checked — none exists).

   Instead, `tests/ReportViewerCore.LinuxRenderers.Tests/` got a **behavioral** harness: `ShapedTextRasterizer.cs` shapes text via `SkiaSharp.HarfBuzz.SKShaper` (the official HarfBuzzSharp+SkiaSharp integration package — handles the font-units-to-pixels scaling correctly, unlike the raw `HarfBuzzSharp.Font.SetScale` calls in the Phase 1 spike) and rasterizes it to an off-screen `SKBitmap`; `TextRasterAssertions.cs` then verifies, per glyph, that ink actually landed in the glyph's shaped column (`VerifyGlyphInkPresence`) and that LTR advance is non-decreasing (`VerifyMonotonicLtrAdvance`) — no golden image needed, since it's checking the raster against the shaping data that produced it, not against a stored reference. `TextRenderingVisualTests.cs` exercises plain Latin, accented Latin, empty string, and documents the RTL gap (`RtlText_...ButIsNotYetVerifiedForCorrectReordering` — `SKShaper` shapes RTL text but this harness has no bidi-reordering check yet). A meta-test (`VerifyGlyphInkPresence_FailsWhenAGlyphColumnIsShiftedIntoBlankCanvas`) proves the checker actually has discriminating power (deliberately corrupts one glyph's shaped position and confirms the check fails) rather than trivially passing regardless of correctness — this is the same "compiles and runs ≠ renders correctly" lesson from Chart's `SkiaGraphicsPath.AddLine`/`AddArc` bugs, applied to the checker itself, not just the thing it checks.

   This harness is reusable once step 3 (shaping layer) exists in production code: feed the production shaper's output into `TextRasterAssertions` the same way. If a future increment needs true visual (not just structural) confidence — e.g. confirming glyph *shapes* look right, not just glyph *positions* — a golden-image approach can still be added then, but should vendor a specific font file into the repo first so the baseline is actually reproducible cross-platform (none of the fonts found in the local NuGet cache during this investigation — Font Awesome, Bootstrap Glyphicons, Semantic UI icon fonts — are suitable body-text fonts; a real one would need to be sourced deliberately, e.g. an open-license face like DejaVu Sans or Noto).
5. Once a Skia/HarfBuzzSharp path renders a plain-Latin PDF page correctly on both Windows and Linux (byte-identical is not the bar — GDI+ vs. Skia text rasterization will differ regardless, same precedent as Chart's E2 Skia-vs-its-own-baseline approach), revisit whether P1-P3's `GraphicsBase`/HDC-metrics call sites (`PDFWriter.cs:1343,1972,2170,2338`) need their own `SKFont`-based port or can be bypassed entirely for the Skia-backed `CachedFont` path.

## References

- `Microsoft.ReportViewer.Common/Microsoft.ReportingServices.Rendering.RichText/` — the whole subsystem (51 files, ~7,421 lines)
- `docs/decisions.md` — 2026-07-26 correction entry with full reasoning
- `docs/platform-support.md` — PDF (RDL engine) section, current state
- `docs/rendering-abstractions.md` — Chart/Gauge Ports & Adapters pattern this should follow
