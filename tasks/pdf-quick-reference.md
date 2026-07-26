# Quick Reference: PDF Rendering Cross-Platform Scope

**Status (2026-07-26):** P1-P3 done. P4 (text shaping) not started — full scope in [`pdf-text-shaping-abstraction.md`](pdf-text-shaping-abstraction.md).

## Done

- **P1 (image decode):** `PDFWriter`'s embedded-image decode now goes through `IImageProvider.DecodeToBgra32` (Excel's existing `ImageProviderFactory` pattern), not `System.Drawing.Bitmap`/`LockBits`.
- **P2 (font style):** `PDFFont.Style` is `PdfFontStyle`, a local `[Flags]` enum, not `System.Drawing.FontStyle`.
- **Two additional eager-GDI+-construction bugs found and fixed while implementing P1/P3:** `Renderer.ImageResources` (static, built at type-load) and `GraphicsBase`'s `Bitmap`/`Graphics` pair (built in its constructor, called by every `WriterBase.BeginReport`) are now both lazy. A text-free PDF (lines/rectangles/images only) no longer crashes at construction on a platform where GDI+ can't construct `System.Drawing` objects.
- Verified: `dotnet build` 0 errors, full test suite passing, a new end-to-end `SunburstChartWithCategoryHierarchy_RendersToPdf` test (`tests/Microsoft.ReportViewer.Chart.Rdl.Tests`) exercising the real `PDFRenderer`→`Renderer`→`PDFWriter` path.

## Not done — the real blocker (P4)

`Microsoft.ReportingServices.Rendering.RichText`'s text-shaping pipeline (`FontCache`/`CachedFont`/`TextRun`/`LineBreaker`/`TextBox`, backed by `Win32.cs`'s Uniscribe P/Invokes) constructs live `System.Drawing.Font` objects and calls Win32 Uniscribe (`ScriptShape`/`ScriptPlace`/`ScriptBreak`/`ScriptItemize`) **unconditionally for every text run, in every script** — this was the original (2026-07-13/07-24) analysis's biggest scoping miss: it isn't a complex-script/RTL edge case, it's the sole text-shaping mechanism for all text this engine renders. See `docs/decisions.md`'s 2026-07-26 correction and `tasks/pdf-text-shaping-abstraction.md` for the full breakdown and phased plan.

This also isn't PDF-scoped: the same pipeline backs the Windows-only `ImageWriter` (EMF/TIFF) renderer, the WinForms on-screen viewer, and the shared `HPBProcessing`/`SPBProcessing` pagination engine used by every renderer.

## Document navigation

- Full call-chain trace (historical) → [`pdf-render-callstack-analysis.md`](pdf-render-callstack-analysis.md)
- P4 scope, phased plan, file/line references → [`pdf-text-shaping-abstraction.md`](pdf-text-shaping-abstraction.md)
- Platform support matrix → [`docs/platform-support.md`](../docs/platform-support.md)
- Progress tracking → `TODO.md`
