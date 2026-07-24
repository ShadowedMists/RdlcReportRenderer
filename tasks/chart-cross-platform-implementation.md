# Chart & Gauge Cross-Platform Rendering — Plan

**Supersedes:** `chart-library-decision.md` (OxyPlot retraction — see `docs/decisions.md`)
**Detailed per-type progress:** [`chart-gdi-type-abstraction.md`](chart-gdi-type-abstraction.md), [`gauge-gdi-type-abstraction.md`](gauge-gdi-type-abstraction.md)
**Architecture, spike findings, and decisions:** [`docs/rendering-abstractions.md`](../docs/rendering-abstractions.md), [`docs/platform-support.md`](../docs/platform-support.md), [`docs/decisions.md`](../docs/decisions.md)

## What's rendering today

Chart (`Microsoft.Reporting.Chart.WebForms`) and Gauge (`GaugeContainer`) are two vendored, first-party rendering engines — not external libraries. Both rasterize to PNG (or record to EMF/EMF+) and embed the result as an image; neither draws directly to the page, and there is no SVG output path. Chart already has a rendering seam, `IChartRenderingEngine` → `ChartRenderingEngine` → `ChartGraphics3D` → `ChartGraphics` (the chokepoint ~39 files use); Gauge has the analogous `IGaugeRenderingEngine`/`GaugeGraphics`.

## Approach

Re-target both engines' existing rendering seams to SkiaSharp (dual-backend: Windows keeps GDI+, Linux/macOS use Skia) via the Ports & Adapters design in `docs/rendering-abstractions.md`, rather than replacing either engine with an external library. See `docs/decisions.md` for why an earlier OxyPlot-replacement decision was retracted.

## Phase plan

| Phase | Status | Notes |
|---|---|---|
| 0 — Spike | Done | Confirmed GDI+ can't construct at all on Linux (see `docs/platform-support.md`); validated the neutral-port-type approach with a hand-built scene rendering identically on Windows and Linux via Skia. |
| 1 — Platform selection + graceful degradation | Not started | Add a platform check at `ChartMapper.GetImage`/`GaugeMapper.GetImage`; return a placeholder instead of letting GDI+ throw on unsupported platforms. |
| 2 — Abstract the pixel/output boundary | Done for Chart (`IRenderSurfaceFactory`/D1) | Gauge's equivalent (`BufferBitmap` → `IRenderSurface`) not started — see `gauge-gdi-type-abstraction.md` open item 6. |
| 3 — Abstract GDI+ resource types + implement Skia backend | In progress | Full detail in `chart-gdi-type-abstraction.md` / `gauge-gdi-type-abstraction.md`. |
| 4 — Backend factory + integration | Scoped (2026-07-24), not started | Phase 3 substantially complete; the real reachable production path (`ChartMapper.GetImage`→`Chart.Save`→`ChartImage.SaveImage`) has no `Bitmap`-contract wall (that's `ChartImage.GetImage`, a separate, non-production entry point, which stays permanently Windows-only) — just missing platform-selection wiring. See Chart doc's Milestone F. |
| 5 — Apply pattern to Gauge engine | In progress | Tracked in `gauge-gdi-type-abstraction.md`. Map engine (`Microsoft.Reporting.Map.WebForms`) is a third GDI+-coupled engine whose scope is still undecided. |
| 6 — Visual regression testing | Ongoing | Chart harness has ~52 tests; Gauge harness has 3 sample gauges (built from scratch, 2026-07-21). Needs a broader corpus and, eventually, a real cross-platform (Skia) render path to diff against. |
