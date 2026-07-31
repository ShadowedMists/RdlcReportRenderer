# Rendering format test-coverage gaps

**Status: Done for this doc's original scope.** HTML/HTML4.0/CSV/XML smoke tests added 2026-07-27; Gauge RDL-render-level smoke tests added 2026-07-27; IMAGE TIFF/BMP smoke tests added 2026-07-27 (see `tasks/image-renderer-cross-platform.md`); Map RDL-render-level smoke test added 2026-07-27 (confirms existing Windows-only behavior actually works — item 3 below). Compiled 2026-07-27 while auditing README's "Supported rendering formats" against actual test coverage, since the Chart/Excel/PDF work has been verified in real depth (including WSL cross-platform runs) while other formats had no attention at all.

## Current state (updated 2026-07-27)

Only three test projects exist: `Microsoft.ReportViewer.Chart.Rdl.Tests`, `Microsoft.ReportViewer.DataVisualization.VisualRegressionTests`, and `ReportViewerCore.LinuxRenderers.Tests`.

| Format | Automated test coverage |
| --- | --- |
| Chart (2D/3D) | Yes — extensive, including Skia-backend visual regression |
| PDF | Yes — real RDL-engine end-to-end tests, WSL-verified cross-platform (2026-07-27) |
| EXCEL/EXCELOPENXML | Basic — `TestExcelGeneration` in `ReportViewerCore.LinuxRenderers.Tests` |
| HTML5/HTML4.0 | Yes (2026-07-27) — `HtmlCsvXmlRdlTests.cs`, WSL-verified |
| WORD/WORDOPENXML | Yes (2026-07-27) — `WordRendererRdlTests.cs`, see `tasks/word-renderer-cross-platform.md` |
| IMAGE (TIFF/EMF) | Partial (2026-07-27) — `ImageWriterRdlTests.cs` adds TIFF/BMP smoke tests (well-formed magic bytes only, Windows-run); EMF and cross-platform behavior remain untested — see `tasks/image-renderer-cross-platform.md` |
| CSV | Yes (2026-07-27) — `HtmlCsvXmlRdlTests.cs`; smoke-test only (asserts non-null, not content — `SimpleTextboxReport.rdlc` has no tablix/list/table data region, so CSV's real per-row output isn't exercised. A future pass should add a data-region-bearing fixture) |
| XML | Yes (2026-07-27) — `HtmlCsvXmlRdlTests.cs`, WSL-verified |
| Gauge | Yes (2026-07-27) — `GaugeRdlTests.cs`, a new `SimpleGaugeReport.rdlc` fixture (no pre-existing Gauge `.rdlc` existed; authored from `ReportDefinition.xsd`'s `GaugePanelType`/`RadialGaugeType` schema) with a data-bound radial-gauge needle pointer, rendered via `IMAGE`/`PDF` |
| Map | Yes (2026-07-27) — `MapRdlTests.cs`, a new `SimpleMapReport.rdlc` fixture (minimal `<Map>`/`<MapViewport>`, no layers — `MapViewport` is the only required child per schema) confirms the existing Windows-only behavior works, rendered via `IMAGE`. Migration itself remains deferred (see `docs/decisions.md`) |

## Why this matters now

The PDF fix committed 2026-07-27 (itemized-text-shape cache crashing on non-Windows) was found only by actually running `LocalReport.Render("PDF")` end to end under WSL — the existing unit-level shaping tests all passed despite the real bug, because they never exercised the specific reuse path that broke. That same risk — "a plausible-looking test suite that doesn't actually exercise the real render path" — applies with more force to formats that have *no* tests at all, not just incomplete ones.

## Proposed tasks

1. ~~Add at least one end-to-end RDL render smoke test per currently-untested format (HTML, CSV, XML at minimum...)~~ — done 2026-07-27 (`HtmlCsvXmlRdlTests.cs`, WSL-verified for HTML5/HTML4.0/XML; CSV is a non-throwing smoke test only, see the table note above).
2. ~~For Gauge: add RDL-render-level tests...~~ — done 2026-07-27 (`GaugeRdlTests.cs`; asserts well-formed PNG/PDF output rather than pixel content, since no Gauge visual baseline exists yet — a future pass could add one, mirroring Chart's `ImageComparer.CompareToBaseline`).
3. ~~For Map: at minimum, confirm the existing Windows-only behavior actually works via one baseline test...~~ — done 2026-07-27 (`MapRdlTests.cs`; a smoke test only, no visual baseline, matching the same rationale as Gauge's).
4. Treat WORD/WORDOPENXML and IMAGE/TIFF/EMF test coverage as part of those renderers' own dedicated tasks (already tracked — see the linked files above) rather than duplicating scope here.
5. As each gap closes, update the table above and remove the row rather than leaving a stale "none found" note next to a now-tested format.
6. A future pass should add a tablix/list/table-bearing RDL fixture so CSV's real per-row output gets exercised, not just "doesn't throw" — the current fixture (`SimpleTextboxReport.rdlc`) has no data region for CSV to act on.
7. **New (2026-07-31): `SimpleMapReport.rdlc`'s no-layers fixture doesn't exercise a color-scale/gradient legend**, so `ColorSwatchPanel.cs` (just converted to `IGraphicsPath`/`IChartFont`/etc. as part of `tasks/map-engine-cross-platform.md`'s Milestone B) has zero test coverage of its own — confirmed via grep, no test file anywhere references `ColorSwatchPanel`/`SwatchColor`/`ColorScale`/`LegendCell`. A Map report with a color-rule/gradient legend item (`MapColorScale`, per the RDL schema) would both close this gap and give the `ColorSwatchPanel.cs` conversion the same real, rendered-and-inspected verification the Chart-engine work already got.
   - **Attempted 2026-07-31, found bigger than a quick fixture add — documenting rather than rushing.** `<MapColorScale>` itself is a simple, direct `<Map>` child (no data binding needed for the legend UI element itself). The blocker is upstream: `MapColorRangeRule`/`MapColorPaletteRule`/`MapCustomColorRule`'s `DataValue` expression (the thing that actually populates `ColorSwatchPanel.Colors` via `GroupRule.cs`/`PathRule.cs`/`ShapeRule.cs`/`SymbolRule.cs`) can only reference fields from a real dataset scope — confirmed empirically: a `MapPointLayer` built from static `<MapPoints>`/`<MapPoint><MapFields>` (no `MapDataRegion`) throws `ReportPublishingException: "...refers to the field 'Value'. Report item expressions can only refer to fields within the current dataset scope..."` at compile time, even though the static `MapFields` mechanism exists for other purposes (labels, tooltips). A working fixture needs a real `MapDataRegion` (bound to a `<DataSet>`, per the `GaugeRdlTests.cs`/`SimpleGaugeReport.rdlc` pattern of supplying rows via `ReportDataSource` at runtime) with a `MapMember`/`Group` spatial-field binding and a `MapPolygonLayer` or `MapPointLayer` referencing that region via `MapDataRegionName` — a multi-part RDL construct (dataset + data region + group + layer + rule), not a same-shape one-file addition like `SimpleGaugeReport.rdlc`'s single `RadialGauge`/`GaugeInputValue`. Left for a dedicated future pass rather than continuing to iterate against the schema blind.
