# Word (WORD/WORDOPENXML) renderer: cross-platform support

**Status: image-decode gap FIXED and verified (2026-07-27); WORDOPENXML is fully cross-platform now. WORD (binary Word 97) has a separate, deeper, newly-discovered blocker — see "New gap found" below.**

## Original gap (now fixed) — image-decode coupling

`Microsoft.ReportViewer.Common/Microsoft.ReportingServices.Rendering.WordRenderer/` (Word 97 binary) and `...WordRenderer.WordOpenXmlRenderer/` (WordOpenXml) don't share code with `ImageWriter`'s Metafile/EMF stack or the `RichText`/`LineBreaker`/`FontCache` pipeline PDF's own fix touched — both write their own document markup directly rather than routing through GDI+ drawing primitives. The only `System.Drawing` coupling was two `Image.FromStream(...)` calls used purely to read an embedded picture's dimensions/format:

- `PictureDescriptor.cs`'s `ParseImageData()` — also re-encoded non-JPEG/PNG images to PNG via `image.Save(memoryStream, ImageFormat.Png)`, not just read metadata.
- `WordOpenXmlWriter.cs`'s `AddImage(...)` — metadata only (height/width/format), no re-encoding.

**Fix:** added `IImageProvider.EncodeToPng(Stream)` (implemented in both `WindowsImageProvider` via GDI+ and `CrossPlatformImageProvider` via SixLabors.ImageSharp) alongside the existing `LoadImage(Stream)`/`DecodeToBgra32(...)` members, then routed both call sites through `ImageProviderFactory.CreateProvider()` — the same abstraction Excel and PDF already use (`docs/rendering-abstractions.md`). No other changes were needed to either renderer.

New tests: `tests/Microsoft.ReportViewer.Chart.Rdl.Tests/WordRendererRdlTests.cs` (4 tests: plain-textbox and embedded-picture reports, each rendered to both WORD and WORDOPENXML) plus a new fixture `Reports/WordImageReport.rdlc` (a tiny embedded 1x1 PNG) to actually exercise the image-decode path — there was previously zero test coverage of any kind for either Word renderer.

Verified on Windows: all 4 pass, `dotnet build --no-incremental` 0 errors. Verified under WSL — see "New gap found" below for the result.

## New gap found (2026-07-27, via the WSL run) — WORD (binary) needs a portable OLE compound-file writer

Both `SimpleTextbox_RendersToWord` and `ImageReport_RendersToWord` **fail under WSL** with:

```
System.Runtime.InteropServices.MarshalDirectiveException: Cannot marshal 'parameter #4': Invalid managed/unmanaged type combination (Marshaling to and from COM interface pointers isn't supported).
  at Microsoft.ReportingServices.Rendering.WordRenderer.StructuredStorage.OLEStructuredStorage.StgCreateDocfile(...)
  at Microsoft.ReportingServices.Rendering.WordRenderer.StructuredStorage.CreateMultiStreamFile(...)
```

This is **unrelated to the image-decode fix above and pre-existing** — `StructuredStorage.cs` builds the actual `.doc` file container via real Windows COM interop (`ole32.dll`'s `StgCreateDocfile`/`IStorage`/`IStream`, OLE Compound File Binary Format), not a P/Invoke that could be swapped for a portable equivalent call-by-call. `WORDOPENXML`'s own container is an ordinary zip/OPC package (no COM involved), which is exactly why `SimpleTextbox_RendersToWordOpenXml`/`ImageReport_RendersToWordOpenXml` both pass under WSL with no changes — this confirms the image-decode fix itself has no platform gap; the WORD-binary failure is a completely separate, deeper architectural wall specific to that one format's container.

**This is now the real remaining scope for Word cross-platform support** — not image handling. Fixing it means replacing `StructuredStorage.cs`'s COM-based CFBF writer with a portable managed one (either a library like `OpenMcdf` or a hand-rolled writer against the documented CFBF spec), which is a meaningfully sized, separate effort — not a drive-by fix alongside the image-decode work. Until then, WORD (binary) rendering remains Windows-only; WORDOPENXML should be recommended as the cross-platform-safe alternative for anyone needing Word output on Linux/macOS.

`docs/build-and-test.md`'s known-WSL-failures list has been updated with these two test names.

## Related upstream signal

Upstream `lkosson/reportviewercore` PR #146 ("fix drawing image in rendering excel/word report in linux") targeted the same image-decode class of problem fixed above, for both Excel and Word.

## Remaining proposed tasks

1. Scope and implement a portable OLE Structured Storage / CFBF writer to replace `StructuredStorage.cs`'s COM-based one, so WORD (binary) can render on non-Windows.
2. Until then, update `docs/platform-support.md`'s support matrix to show WORDOPENXML as cross-platform and WORD (binary) as Windows-only with this specific blocker named, rather than leaving Word unlisted entirely.
