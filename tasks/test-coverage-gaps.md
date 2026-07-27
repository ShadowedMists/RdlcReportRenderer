# Rendering format test-coverage gaps

**Status: Partially done.** HTML/HTML4.0/CSV/XML smoke tests added 2026-07-27; Gauge/Map RDL-render-level coverage still open. Compiled 2026-07-27 while auditing README's "Supported rendering formats" against actual test coverage, since the Chart/Excel/PDF work has been verified in real depth (including WSL cross-platform runs) while other formats had no attention at all.

## Current state (updated 2026-07-27)

Only three test projects exist: `Microsoft.ReportViewer.Chart.Rdl.Tests`, `Microsoft.ReportViewer.DataVisualization.VisualRegressionTests`, and `ReportViewerCore.LinuxRenderers.Tests`.

| Format | Automated test coverage |
| --- | --- |
| Chart (2D/3D) | Yes — extensive, including Skia-backend visual regression |
| PDF | Yes — real RDL-engine end-to-end tests, WSL-verified cross-platform (2026-07-27) |
| EXCEL/EXCELOPENXML | Basic — `TestExcelGeneration` in `ReportViewerCore.LinuxRenderers.Tests` |
| HTML5/HTML4.0 | Yes (2026-07-27) — `HtmlCsvXmlRdlTests.cs`, WSL-verified |
| WORD/WORDOPENXML | Yes (2026-07-27) — `WordRendererRdlTests.cs`, see `tasks/word-renderer-cross-platform.md` |
| IMAGE (TIFF/EMF) | None — see `tasks/image-renderer-cross-platform.md` |
| CSV | Yes (2026-07-27) — `HtmlCsvXmlRdlTests.cs`; smoke-test only (asserts non-null, not content — `SimpleTextboxReport.rdlc` has no tablix/list/table data region, so CSV's real per-row output isn't exercised. A future pass should add a data-region-bearing fixture) |
| XML | Yes (2026-07-27) — `HtmlCsvXmlRdlTests.cs`, WSL-verified |
| Gauge | None (has visual/interface-conversion coverage under the Chart-adjacent test project structure, but not RDL-render-level tests) — still open |
| Map | None — consistent with its migration being deferred, but the *existing* Windows-only rendering behavior is also unverified by any test — still open |

## Why this matters now

The PDF fix committed 2026-07-27 (itemized-text-shape cache crashing on non-Windows) was found only by actually running `LocalReport.Render("PDF")` end to end under WSL — the existing unit-level shaping tests all passed despite the real bug, because they never exercised the specific reuse path that broke. That same risk — "a plausible-looking test suite that doesn't actually exercise the real render path" — applies with more force to formats that have *no* tests at all, not just incomplete ones.

## Proposed tasks

1. ~~Add at least one end-to-end RDL render smoke test per currently-untested format (HTML, CSV, XML at minimum...)~~ — done 2026-07-27 (`HtmlCsvXmlRdlTests.cs`, WSL-verified for HTML5/HTML4.0/XML; CSV is a non-throwing smoke test only, see the table note above).
2. For Gauge: add RDL-render-level tests (not just the existing lower-level interface-conversion/visual tests) to confirm the real `LocalReport.Render` path produces correct output, mirroring how Chart's RDL tests complement its own lower-level tests. **Still open.**
3. For Map: at minimum, confirm the existing Windows-only behavior actually works via one baseline test, before any further migration decision is made — right now there is no automated evidence either way. **Still open.**
4. Treat WORD/WORDOPENXML and IMAGE/TIFF/EMF test coverage as part of those renderers' own dedicated tasks (already tracked — see the linked files above) rather than duplicating scope here.
5. As each gap closes, update the table above and remove the row rather than leaving a stale "none found" note next to a now-tested format.
6. A future pass should add a tablix/list/table-bearing RDL fixture so CSV's real per-row output gets exercised, not just "doesn't throw" — the current fixture (`SimpleTextboxReport.rdlc`) has no data region for CSV to act on.
