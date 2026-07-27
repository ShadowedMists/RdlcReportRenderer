# Build and test guide

## Prerequisites

- .NET SDK available on the machine
- The repository checked out locally

## Restore and build

From the repository root, run:

```powershell
dotnet restore
```

```powershell
dotnet build ReportViewerCore.sln
```

## Run the renderer-focused tests

The targeted tests for the new abstraction work can be run with:

```powershell
dotnet test tests/ReportViewerCore.LinuxRenderers.Tests/ReportViewerCore.LinuxRenderers.Tests.csproj --filter "TestImageResourceAdapterCanWriteEmbeddedData|TestExcelGeneration|TestPdfGeneration|TestRendererFactoryUsesLinuxRenderers" -v minimal
```

## Verifying cross-platform paths under WSL

Building/testing under WSL (Ubuntu) is a real, low-cost way to exercise the actual non-Windows code paths (`OperatingSystem.IsWindows()` reads `false`) without a separate Linux box, and has caught real bugs that unit tests alone missed (see `docs/platform-support.md`'s PDF section, 2026-07-27). From the repo root, in a WSL shell:

```bash
cd /mnt/c/Development/RdlcReportRenderer
dotnet build tests/Microsoft.ReportViewer.Chart.Rdl.Tests/Microsoft.ReportViewer.Chart.Rdl.Tests.csproj -c Debug
dotnet test tests/Microsoft.ReportViewer.Chart.Rdl.Tests/Microsoft.ReportViewer.Chart.Rdl.Tests.csproj -c Debug
dotnet test tests/ReportViewerCore.LinuxRenderers.Tests/ReportViewerCore.LinuxRenderers.Tests.csproj -c Debug
```

Building the whole solution (`ReportViewerCore.sln`) fails under WSL with `NETSDK1100` — expected, since `Microsoft.ReportViewer.WinForms` targets `net10.0-windows7` and is intentionally Windows-only; build the individual cross-platform projects/test projects above instead.

A handful of test failures under WSL are environment artifacts, not product bugs — they hardcode paths to Windows-only fonts (`simsun.ttc`, `cambria.ttc`, `msyh.ttc`) that don't exist on a Linux box: `IsTtc_RealTtcFont_ReturnsTrue`, `TryExtractTtcFace_*`, `DrawWrappedText_WithTtcBackedFont_EmbedsExtractedSingleFaceNotWholeContainer`, `GetFallbackFontCrossPlatform_MissingCjkGlyph_ResolvesAFontThatCoversIt`. `SunburstChartWithCategoryHierarchy_MatchesBaseline` also fails — it exercises the legacy GDI+/EMF `ImageRenderer` baseline path, which was never in scope for the cross-platform work.

## Recommended validation workflow

1. Restore dependencies.
2. Build the solution.
3. Run the Linux renderer tests.
4. If a change touches the HTML rendering path, verify that resource adaptation still behaves as expected.
5. Record any new limitations in the troubleshooting guide.
6. For changes touching PDF/RichText's cross-platform branches specifically, actually run the affected tests under WSL (see above) — unit tests that construct a fresh `TextRun`/shaping call directly can pass while a real end-to-end render still crashes, if the bug is in a reuse/caching path that only fresh construction skips.
