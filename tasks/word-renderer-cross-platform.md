# Word (WORD/WORDOPENXML) renderer: cross-platform support

**Status: NOT STARTED.** Not previously tracked anywhere — found 2026-07-27 while auditing README's "What works"/rendering-format claims against actual code state, after the Chart/Excel/PDF cross-platform work. No existing task file or docs/decisions.md entry covered this renderer at all before now.

## Why this is a smaller lift than Chart/Gauge/PDF were

`Microsoft.ReportViewer.Common/Microsoft.ReportingServices.Rendering.WordRenderer/` (Word 97 binary) and `...WordRenderer.WordOpenXmlRenderer/` (WordOpenXml) do **not** share code with `ImageWriter`'s Metafile/EMF stack or the `RichText`/`LineBreaker`/`FontCache` pipeline PDF just got fixed (see `tasks/image-renderer-cross-platform.md` and `docs/platform-support.md`'s PDF section for how deep those two are). Both Word renderers write their own document markup directly, much like `PDFWriter` writes PDF content-stream operators directly — they don't route through GDI+ drawing primitives.

The only `System.Drawing` coupling found (2026-07-27 audit) is narrow:
- `PictureDescriptor.cs` and `WordOpenXmlWriter.cs` call `System.Drawing.Image.FromStream(...)` purely to read an embedded picture's dimensions/format before writing it into the document.
- `WordColor.cs`/`BorderCode.cs` use basic `System.Drawing.Color` structs, not drawing operations.

This means the fix shape likely mirrors Excel's already-completed `IImageProvider` abstraction (see `docs/rendering-abstractions.md`) rather than a from-scratch engine port: replace the `Image.FromStream` dimension/format read with the same cross-platform image-decode seam Excel and PDF already use, and the rest of both renderers should need no changes.

## Current gap

- No cross-platform image-decode path wired in for either Word renderer — will throw on Linux/macOS wherever `System.Drawing.Image.FromStream` is hit (i.e., any report with a picture item rendered to WORD/WORDOPENXML).
- **Zero automated test coverage of any kind** — confirmed 2026-07-27, no Word-renderer test files/classes exist in `tests/` at all (not even Windows-only baseline tests). Any fix here should add both an end-to-end RDL render test (mirroring `tests/Microsoft.ReportViewer.Chart.Rdl.Tests`'s PDF textbox tests) and, once the image-decode seam lands, a WSL-verified cross-platform test — see `docs/build-and-test.md`'s WSL section for why unit tests alone aren't sufficient evidence for this kind of fix.

## Related upstream signal

Upstream `lkosson/reportviewercore` PR #146 ("fix drawing image in rendering excel/word report in linux", opened 2023-07-09, still unmerged as of 2025-03-06) targets the same class of problem for both Excel and Word. Excel's own image-drawing gap is already resolved in this fork via `IImageProvider` (see `docs/rendering-abstractions.md`); worth skimming that PR's diff for the Word half specifically before starting from scratch, since it may already identify every call site.

## Proposed tasks

1. Confirm exact call sites needing conversion (`PictureDescriptor.cs`, `WordOpenXmlWriter.cs`, and any others turned up by a fresh grep at implementation time).
2. Route them through the existing `IImageProvider`/`DecodeToBgra32`-style abstraction already used by Excel and PDF, rather than inventing a new one.
3. Add end-to-end RDL render tests for both WORD and WORDOPENXML (plain textbox + a picture-containing report, matching the shape of the PDF textbox tests).
4. Verify under WSL per `docs/build-and-test.md`'s recommended workflow before declaring this done — this is exactly the kind of fix where a fresh-construction unit test can pass while real end-to-end rendering doesn't.
5. Update `docs/platform-support.md`'s support matrix once verified.
