# PDF Rendering Cross-Platform Analysis

**Status (2026-07-24): re-scoped — the 2026-07-13 "full GDI+/Metafile replacement" premise was wrong.** A fresh read of the current source shows the PDF path never uses `Metafile`, `System.Drawing.Graphics` drawing calls, `Pen`/`Brush`/`GraphicsPath` — those all belong to a *different* rendering extension (`ImageWriter`, the BMP/GIF/JPEG/PNG/TIFF/EMF output format), which happens to share `Graphics.cs`/`GraphicsBase.cs`/`MetafileGraphics.cs` but is not on `PDFRenderer`'s call chain at all. The real remaining PDF-path dependencies are much narrower. See `docs/decisions.md`'s "PDF path vs. Image-renderer path" clarification for why this distinction matters and how it was found.

**Detailed per-milestone tracking, once work starts:** none yet — this doc is the scoping reference; a `pdf-gdi-type-abstraction.md` in the style of the Chart/Gauge docs should be created when implementation begins, rather than continuing to grow this analysis doc.

## Actual call chain (verified 2026-07-24)

```
LocalReport.Render("PDF")
  └─ PDFRenderer.Render()
     ├─ Renderer.ProcessPage() — draws via Writer.DrawLine/DrawRectangle/FillRectangle/FillPolygon/DrawImage/...
     │   (all calls use portable Color/RectangleF/PointF — no GDI+ drawing types)
     └─ PDFWriter : WriterBase
         ├─ DrawLine/DrawRectangle/etc. append literal PDF content-stream operators
         │   to a StringBuilder (e.g. DrawLine at PDFWriter.cs:279 emits "...m ... l S")
         │   — there is no System.Drawing.Graphics call anywhere in this path.
         ├─ Bitmap decode (image embedding): PDFWriter.cs:1189, 1526, 1529
         ├─ PDFFont.GDIFontStyle (System.Drawing.FontStyle enum, no live Graphics use): PDFFont.cs:58
         └─ Win32 HDC-based font metrics/embedding, via GraphicsBase.GetHdc()/ReleaseHdc():
             ├─ PDFWriter.cs:1343-1356 (ProcessDrawStringFont) — TEXTMETRIC read for simulated bold/italic
             ├─ PDFWriter.cs:1972-2008 (WriteEmbeddedFont) — GetFontData, raw font-table bytes for subsetting
             ├─ PDFWriter.cs:2170-2296 (font descriptor writer) — GetCharABCWidthsFloat, GetOutlineTextMetrics
             └─ PDFWriter.cs:2338-2352 (ProcessFontForFontEmbedding) — embedding-rights bits from font table
         └─ Complex-script text shaping/line-breaking, upstream of PDFWriter's DrawTextRun:
             Microsoft.ReportingServices.Rendering.RichText.LineBreaker/TextBox
             → usp10.dll (Uniscribe): ScriptItemize/ScriptShape/ScriptPlace/ScriptBreak/ScriptGetLogicalWidths/...
```

`ImageWriter` (separate extension, NOT on this call chain) is the actual consumer of `Metafile`/`Graphics`/`GraphicsBase`'s GDI+ drawing surface. If that renderer is ever targeted for cross-platform support, it needs the Chart/Gauge-style Ports & Adapters treatment on its own — track it separately, don't fold it into PDF scoping.

## What's actually cross-platform-blocking, by size

| Item | Location | Size | Nature |
|---|---|---|---|
| Bitmap decode for image embedding | `PDFWriter.cs:1189,1526,1529` | ~small | Same shape as Excel's already-solved `IImageProvider`/ImageSharp pattern — direct reuse candidate |
| `PDFFont.GDIFontStyle` | `PDFFont.cs:58` | trivial | Swap `System.Drawing.FontStyle` for a local enum; no live `Graphics`/`Font` object escapes anywhere |
| HDC-based font metrics (4 call sites) | `PDFWriter.cs:1343-2352`, listed above | ~small-medium | GDI-only calls (`GetCharABCWidthsFloat`, `GetOutlineTextMetrics`, glyph indices) map directly to `SKFont.GetGlyphWidths`/`SKFont.Metrics`. The `GetFontData`/embedding-rights read needs a font-table-parsing dependency (`SKTypeface.GetTableData` or similar) to reimplement the OS/2 `fsType` check without GDI — bounded, but a new small port, not a straight swap |
| Complex-script shaping/line-breaking | `Microsoft.ReportingServices.Rendering.RichText.LineBreaker.cs` (709 lines) + `TextBox.cs` (666 lines), backed by `Win32.cs` (916 lines of P/Invoke) — namespace totals 51 files/~7,421 lines | **large, open-ended** | Real bidi + Arabic/Thai/Indic/Hebrew shaping via Uniscribe (`ScriptItemize/Shape/Place/Break`). No `SKPaint.MeasureText`-level API covers this — a real replacement (HarfBuzzSharp for shaping + an ICU-based or custom bidi/line-break algorithm) means redesigning ~1,375 lines of shaping/line-break logic against a different API, not porting call-by-call. This is PDF's actual hard blocker — see below. |

No hard external-contract wall (analogous to Chart's `ChartImage.GetImage:Bitmap`) was found on the PDF path — every GDI+-typed value found is internal and replaceable; nothing with a `Bitmap`/`Font`/`Metafile` return type is part of a fixed public API contract.

## Phase plan

| Phase | Status | Notes |
|---|---|---|
| P0 — Corrected call-chain analysis | Done (2026-07-24) | Superseded the 2026-07-13 analysis; see above. |
| P1 — Image decode/encode | Not started | Reuse Excel's `IImageProvider`/ImageSharp pattern for `PDFWriter.cs:1189,1526,1529`. Low risk, direct precedent exists. |
| P2 — Font style enum | Not started | Replace `PDFFont.GDIFontStyle`/`FontStyle` with a local enum. Trivial, no engine coupling. |
| P3 — HDC font-metrics abstraction | Not started | New small port (e.g. `IFontMetricsProvider`) covering the 4 call sites above; GDI-metric calls map to SkiaSharp directly, font-table/embedding-rights read needs a table-parsing dependency. |
| P4 — Complex-script shaping/line-breaking | Not started, **open research question, not a scoped task yet** | Requires evaluating HarfBuzzSharp (shaping) + an ICU or custom bidi/line-break implementation, then redesigning `LineBreaker`/`TextBox` against that API. Should get its own investigation pass (prototype a single script run through HarfBuzzSharp before committing to a full redesign estimate) rather than being estimated from this doc alone. |

P1-P3 do not depend on P4 and can proceed independently; P4 is the phase that actually determines whether full cross-platform PDF text rendering is achievable, and at what cost — treat it as the next thing to spike, not to estimate blind.

## Why the old estimate was wrong

The 2026-07-13 analysis conflated `ImageWriter`'s GDI+/Metafile dependency (real, but for a different renderer) with `PDFWriter`'s own dependencies (much narrower: two Bitmap decode calls, one trivial enum, four bounded HDC-metric calls, and one genuinely hard shaping/line-breaking subsystem). The "3-4 weeks, full SkiaSharp graphics-stack replacement" estimate doesn't apply to the PDF path — P1-P3 above are comparable in size to Chart's Milestone F, not to a from-scratch engine port. P4 (Uniscribe shaping) is the one piece that could still be large, but it's a self-contained subsystem, not a whole-renderer replacement.

## References

- `Microsoft.ReportViewer.Common/Microsoft.ReportingServices.Rendering.ImageRenderer/PDFRenderer.cs`, `PDFWriter.cs`, `PDFFont.cs`, `Renderer.cs`, `GraphicsBase.cs` — PDF path
- `Microsoft.ReportViewer.Common/Microsoft.ReportingServices.Rendering.ImageRenderer/Graphics.cs`, `MetafileGraphics.cs` — Image-renderer path only, not PDF's concern
- `Microsoft.ReportingServices.Rendering.RichText/LineBreaker.cs`, `TextBox.cs`, `Win32.cs` — Uniscribe-based shaping, PDF's real remaining blocker
- `docs/rendering-abstractions.md` — Excel `IImageProvider` pattern (reusable for P1), Chart/Gauge Ports & Adapters design (pattern reference for P3)
- `docs/decisions.md` — "PDF path vs. Image-renderer path" clarification
