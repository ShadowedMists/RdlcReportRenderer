# Chart Library Decision — RETRACTED / SUPERSEDED

**Status:** ⛔ RETRACTED (2026-07-13)

An earlier decision to replace `Microsoft.Reporting.Chart.WebForms` with OxyPlot 2.1.x was retracted after review found its supporting metrics and citations unsourced, and that it misframed the chart engine as an external dependency rather than vendored source with an existing rendering seam. Full retraction reasoning: [`docs/decisions.md`](../docs/decisions.md).

**Replaced by:** [`chart-cross-platform-implementation.md`](chart-cross-platform-implementation.md) — re-target the existing engines to SkiaSharp behind their current rendering seams.
