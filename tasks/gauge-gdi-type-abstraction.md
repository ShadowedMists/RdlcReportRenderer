# Gauge engine: GDI+ type abstraction — COMPLETE

Sibling effort to [`chart-gdi-type-abstraction.md`](chart-gdi-type-abstraction.md), applying the same Ports & Adapters pattern to `Microsoft.Reporting.Gauge.WebForms`.

**Status (2026-07-23): all milestones and open items closed.** Every `Pen`/`Brush`/`Font`/`GraphicsPath`/`StringFormat` call site that reaches `IGaugeRenderingEngine` is interface-typed; the Gdi adapter set (`Rendering/Gdi/`) is complete and pixel-verified against 5 sample-gauge baselines (80/80 `VisualRegressionTests`, zero diffs). No Skia (or any second) backend exists for Gauge yet — none was built, since Gauge's own migration only needed to reach parity with Chart's Milestones A-C; a Gauge Skia backend was never in scope for this doc and would be new work, not a continuation of it.

**Design, namespaces, and recurring patterns:** [`docs/rendering-abstractions.md`](../docs/rendering-abstractions.md)
**Decisions and durable gaps carried forward:** [`docs/decisions.md`](../docs/decisions.md), [`docs/platform-support.md`](../docs/platform-support.md) — includes the `XamlRenderer`/`ScaleBase.DecomposeRotation` facts from this effort.

## What's left, if a Gauge Skia backend is ever wanted

Not scoped by this doc (out of its original goal). Whoever picks this up should expect a shape similar to Chart's own E1/E2/F: a `Rendering/Skia/` adapter set mirroring `Rendering/Gdi/`, a `GaugeCore.Paint`/`SaveTo` split analogous to Chart's `IRenderSurface`, and platform-selection wiring into `GaugeMapper.GetImage`. `GaugeCore`'s `BufferBitmap`-based raster pipeline (no `IRenderSurface`-equivalent today) was deliberately left alone — see `docs/platform-support.md`'s known-gaps list — precisely because introducing that split before a second backend exists would be pure ceremony with zero call sites benefiting from it.
