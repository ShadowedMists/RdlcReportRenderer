# Word (WORD/WORDOPENXML) renderer: cross-platform support

**Status: DONE (2026-07-30).** Both the image-decode gap (2026-07-27) and the WORD-binary CFBF-writer gap (2026-07-30) are fixed and WSL-verified. WORD (binary Word 97) and WORDOPENXML are both fully cross-platform now.

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

**This was the real remaining scope for Word cross-platform support** — not image handling. Fixed below.

`docs/build-and-test.md`'s known-WSL-failures list has been updated to remove these two test names (no longer failing).

## CFBF-writer gap: fixed (2026-07-30)

Scoped first via a research agent before any code changes: found only **one caller** in the entire codebase (`Word97Writer.cs:945`, calling `StructuredStorage.CreateMultiStreamFile`), writing exactly 3 flat, sequential streams (`WordDocument`/`1Table`/`Data`) plus a SummaryInformation property set (title/author/comments) — no nested storages, no random access, no reads-back. Of `StructuredStorage.cs`'s 6 declared `ole32.dll` P/Invokes, only 2 (`StgCreateDocfile`, `StgCreatePropSetStg`) were ever actually called; the rest (`StgOpenStorage`, `StgCreateDocfileOnILockBytes`, `CreateILockBytesOnHGlobal`, `StgCreateStorageEx`) were dead declarations. This meant the fix was genuinely single-session-sized, not a multi-milestone effort like the Chart/Gauge/Map Ports & Adapters migrations.

**Fix:** rewrote `StructuredStorage.CreateMultiStreamFile` against the `OpenMcdf` NuGet package (3.2.0) — a pure-managed CFBF implementation with no native/COM dependency, added as a `PackageReference` to `Microsoft.ReportViewer.Common.csproj` (the least disruptive dependency shape in the project — no native asset variants needed, unlike SkiaSharp/HarfBuzzSharp; matches `ClosedXML`/`System.IO.Packaging`'s existing role as container-format libraries). The method's public signature was preserved exactly, so `Word97Writer.cs` needed zero changes.

OpenMcdf itself only handles the CFBF container (storages/streams) — it has no property-set (SummaryInformation) support, so the title/author/comments metadata stream is hand-written against the MS-OLEPS `PropertySetStream` format (header, one section for `FMTID_SummaryInformation`, the mandatory codepage property, then whichever of title/author/comments were supplied as `VT_LPWSTR` values). Wrapped in `try`/`catch` so a bug in this hand-written format can never break the main document — matches the original code's own "only write if a value was actually supplied" tolerance.

**Real bug caught during verification, then fixed:** `RootStorage.Commit()` throws `NotSupportedException: Cannot commit non-transacted storage` unless `StorageModeFlags.Transacted` was passed at creation — switched to `RootStorage.Flush(consolidate: true)` instead, the correct non-transacted equivalent. Caught immediately by the new tests below, not discovered later.

**Verification, matching this repo's "verify for real" convention** (`tests/Microsoft.ReportViewer.Chart.Rdl.Tests/WordRendererRdlTests.cs`):
- `SimpleTextbox_RendersToWord` strengthened to assert the CFBF magic-byte signature (`D0 CF 11 E0 A1 B1 1A E1`) and read back the `WordDocument`/`1Table`/`Data` streams via a real `OpenMcdf.RootStorage.Open` (confirming the output isn't just non-empty bytes, but an actually-openable compound file with the right structure).
- New `CreateMultiStreamFile_SummaryInformation_RoundTrips` test calls the method directly with known title/author/comments, then **hand-parses the raw property-set bytes independently of the writer's own logic** (a from-scratch `BinaryReader` walk of the PropertySetStream header/section/property table) and asserts they round-trip correctly — this is real, rigorous verification of the hand-written binary format, not a re-use of the same code path that could hide a shared bug.
- All 187 pre-existing Windows tests + 137 `VisualRegressionTests` still pass (188 total Chart.Rdl.Tests including the 1 new test) - zero regressions elsewhere.
- **WSL-confirmed**: all 5 Word-related tests now pass on Linux, where WORD-binary rendering previously crashed entirely with `MarshalDirectiveException` at `StgCreateDocfile`. This is a genuine cross-platform milestone, not just a Windows-side refactor.

## Related upstream signal

Upstream `lkosson/reportviewercore` PR #146 ("fix drawing image in rendering excel/word report in linux") targeted the same image-decode class of problem fixed above, for both Excel and Word.

## Remaining proposed tasks

1. ~~Scope and implement a portable OLE Structured Storage / CFBF writer...~~ — done 2026-07-30, see above.
2. Update `docs/platform-support.md`'s support matrix to show both WORD (binary) and WORDOPENXML as fully cross-platform now (was previously scoped to show WORD as Windows-only, now outdated).
