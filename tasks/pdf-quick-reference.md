# Quick Reference: PDF Rendering Cross-Platform Scope

**Status (2026-07-24):** Re-scoped, not started. Full detail: [`pdf-render-callstack-analysis.md`](pdf-render-callstack-analysis.md).

## The corrected picture

The PDF path (`PDFRenderer` → `Renderer` → `PDFWriter`) writes PDF content-stream operators directly — it does **not** use `Metafile`, `System.Drawing.Graphics` drawing calls, or `Pen`/`Brush`/`GraphicsPath`. Those GDI+ types are used by a separate rendering extension (`ImageWriter`, the BMP/GIF/JPEG/PNG/TIFF/EMF output format) that happens to live in the same project but isn't reachable from `LocalReport.Render("PDF")`. The 2026-07-13 analysis conflated the two and estimated a 3-4 week full-graphics-stack replacement that doesn't apply here.

## What PDF rendering actually depends on

| Item | Size | Fix |
|---|---|---|
| Bitmap decode for embedded images | small | Reuse Excel's `IImageProvider`/ImageSharp pattern (`PDFWriter.cs:1189,1526,1529`) |
| `PDFFont.GDIFontStyle` (`System.Drawing.FontStyle`) | trivial | Swap for a local enum (`PDFFont.cs:58`) |
| Win32 HDC font-metrics calls (4 sites) | small-medium | `GetCharABCWidthsFloat`/`GetOutlineTextMetrics`/glyph-index calls → SkiaSharp `SKFont` equivalents; font-table/embedding-rights read needs a table-parsing dependency (`PDFWriter.cs:1343-2352`) |
| Complex-script shaping/line-breaking (Uniscribe) | **large, open** | `RichText.LineBreaker`/`TextBox` (~1,375 lines) call `usp10.dll` for bidi + Arabic/Thai/Indic/Hebrew shaping — needs a HarfBuzzSharp/ICU-based redesign, not a call-by-call port. This is PDF's real remaining blocker. |

No hard external-contract wall exists on this path (nothing like Chart's `ChartImage.GetImage:Bitmap`) — every GDI+ type found here is internal and replaceable.

## Recommendation

- P1 (image decode), P2 (font-style enum), P3 (HDC metrics) are bounded, Chart/Gauge-scale work and can proceed independently of P4.
- P4 (Uniscribe shaping/line-breaking) should get its own spike — prototype one script run through HarfBuzzSharp — before estimating it further. Don't schedule a fixed timeline against it yet.
- The GDI+/Metafile machinery the old docs worried about belongs to `ImageWriter`, a distinct rendering extension; if that ever needs cross-platform support, scope it separately using the Chart/Gauge Ports & Adapters pattern rather than folding it into PDF's plan.

## Document navigation

- Full analysis, call-chain trace, phase plan → [`pdf-render-callstack-analysis.md`](pdf-render-callstack-analysis.md)
- Chart/Gauge Ports & Adapters pattern (reference design) → [`docs/rendering-abstractions.md`](../docs/rendering-abstractions.md)
- Excel's `IImageProvider` pattern (direct precedent for P1) → [`docs/rendering-abstractions.md`](../docs/rendering-abstractions.md)
- Progress tracking → `TODO.md`
