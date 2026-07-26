# PDF Rendering Cross-Platform Analysis (historical)

**Status (2026-07-26): P0-P3 done and superseded by `docs/`; P4 re-scoped into its own doc.** This document's job — tracing the real PDF call chain and finding P1-P3 were bounded, unlike the original 2026-07-13 "full Metafile/GDI+ replacement" estimate — is complete. Durable facts now live in `docs/platform-support.md` (current state) and `docs/decisions.md` (the two correction entries: the 2026-07-24 "PDF path vs. Image-renderer path" finding, and the 2026-07-26 "P4 is bigger than scoped" finding). The remaining real work (text shaping) has its own doc: **[`pdf-text-shaping-abstraction.md`](pdf-text-shaping-abstraction.md)**.

## What was found, in one line each

- PDF's own path (`PDFRenderer`→`Renderer`→`PDFWriter`) writes PDF content-stream operators directly and never uses `Metafile`/`Graphics`/`Pen`/`Brush`/`GraphicsPath` — those belong to the separate `ImageWriter` (EMF/TIFF) extension, not PDF's call chain.
- P1 (image decode), P2 (font-style enum), and two extra eager-GDI+-construction bugs found along the way (`Renderer.ImageResources`, `GraphicsBase`'s constructor) are fixed — see `docs/platform-support.md`'s "PDF (RDL engine)" section for what changed.
- P4 ("Uniscribe complex-script shaping") was under-scoped: `RichText`'s Uniscribe calls and GDI+ `Font` construction are unconditional for *every* text run in *every* script, not an RTL/CJK-only concern, and the same pipeline backs `ImageWriter`, the WinForms viewer, and the shared pagination engines. Full breakdown, file/line citations, and a phased plan: `pdf-text-shaping-abstraction.md`.

## Navigation

- Current PDF platform-support state → `docs/platform-support.md`
- Why P1-P4 were re-scoped twice (2026-07-24, 2026-07-26) → `docs/decisions.md`
- What's left and how to start it → `tasks/pdf-text-shaping-abstraction.md`
- Progress tracking → `TODO.md`
