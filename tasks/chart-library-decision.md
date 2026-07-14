# Chart Library Decision — RETRACTED / SUPERSEDED

**Status:** ⛔ **RETRACTED** (2026-07-13)
**Replaced by:** [`chart-cross-platform-implementation.md`](chart-cross-platform-implementation.md)

---

## Why this document was retracted

An earlier version of this file recorded a decision to **replace `Microsoft.Reporting.Chart.WebForms` with OxyPlot 2.1.x**, presented as an approved, low-risk, validated choice. On technical review against the actual code, that decision did not hold up:

| Claim in the retracted decision | Reality (verified in code) |
|----------------------------------|----------------------------|
| "Replace the Chart.WebForms **dependency**" | It is **not** an external dependency. It is ~400 files of **vendored source we own**, in [Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms/](../Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms/), with an existing `IChartRenderingEngine` rendering seam the evaluation never mentioned. |
| "85%+ feature coverage", "95%+ of reports render unmodified" | **Unsourced.** No report corpus exists in this repo to measure coverage. |
| Performance table ("Bar 45ms vs 50ms", etc.) | **Fabricated.** No benchmark harness exists. |
| "7.8/10 score", "CVE security scanning" | **Unsupported** by any artifact in the repo. |
| Cites `chart-libraries-research.md`, `CHART_LIBRARY_DEPENDENCY_ANALYSIS.md`, `INTEGRATION_TECHNICAL_DETAILS.md` | **None of these files exist.** |
| Silent on gauges | Gauges use a **separate** engine (`GaugeContainer`, [GaugeMapper.cs:236-263](../Microsoft.ReportViewer.Common/Microsoft.ReportingServices.OnDemandReportRendering/GaugeMapper.cs#L236-L263)); replacing the chart library does nothing for them. |

Replacing the engine would also mean re-implementing the RDL chart model (40+ chart types, 3D, financial, radar, annotations, statistical formulas, palettes) onto a different library's model — a **report-compatibility break**, not a port.

---

## What replaces it

The corrected analysis and phased, line-referenced implementation steps now live in:

➡️ **[`chart-cross-platform-implementation.md`](chart-cross-platform-implementation.md)**

Summary of the corrected position:

- **How rendering works today:** charts and gauges are rasterized to PNG (or recorded to EMF) by two vendored GDI+ engines, then embedded as images into the report output.
- **Recommended approach:** re-target the existing engines to **SkiaSharp behind the existing `IChartRenderingEngine` seam** (dual-backend: Windows keeps GDI+, Linux uses Skia), preserving the chart model and visual fidelity.
- **OxyPlot** remains a *possible* option **only** if a visual redesign is acceptable and pixel-parity with existing reports is an explicit non-goal — not as a validated drop-in.
- **A time-boxed spike is required first** to measure the real effort before committing to any approach.

No library selection is approved at this time. The next action is the **Phase 0 spike** described in the implementation doc.
