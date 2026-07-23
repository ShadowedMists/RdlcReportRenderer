# Architecture decisions

## Decision: introduce thin render abstractions

The rendering layer now uses small interfaces for Excel and PDF output rather than embedding platform-specific logic directly in the call sites. This keeps the implementation modular and makes it easier to swap in additional implementations later.

## Decision: use a factory for renderer selection

Renderer selection is centralized in the factory rather than being repeated across the codebase. The factory provides a single place to decide which implementation should handle a given platform and format.

## Decision: use adapters for resource handling

The new resource adapter normalizes embedded-resource payloads before the rendering layer uses them. This reduces coupling to a specific resource representation such as a stream, string, or byte array.

## Why these choices

- They preserve backwards compatibility with the existing codebase.
- They minimize the amount of new infrastructure needed for the first cross-platform seam.
- They make it easier to extend the design as additional renderers are introduced.

---

## Decision: OxyPlot chart-library replacement — retracted (2026-07-13)

An earlier decision to replace the vendored `Microsoft.Reporting.Chart.WebForms` engine with OxyPlot 2.1.x was reviewed against the actual code and retracted. Reasons:
- The chart engine is not an external dependency — it's ~400 files of vendored source the repo owns, with an existing `IChartRenderingEngine` seam the original evaluation never considered.
- Cited performance/coverage numbers and supporting analysis documents backing the OxyPlot decision could not be found or reproduced.
- Replacing the engine would mean re-implementing the RDL chart model (40+ chart types, 3D, financial, radar, annotations, statistical formulas, palettes) onto a different library's model — a compatibility break, not a port.
- Gauges use a separate engine (`GaugeContainer`/`Microsoft.Reporting.Gauge.WebForms`) that a chart-library swap wouldn't have touched anyway.

**Replaced by:** re-targeting the existing `IChartRenderingEngine` seam to SkiaSharp via the Ports & Adapters design in `docs/rendering-abstractions.md`. OxyPlot (or any other library) remains an option only if a visual/pixel-parity break is acceptable — not as a validated drop-in.

## Decision: re-target the vendored Chart/Gauge engines to SkiaSharp, not replace them

A Phase 0 spike (2026-07-18) confirmed the plan: keep the vendored GDI+ engines, but make their resource types (`Pen`, `Brush`, `Font`, `GraphicsPath`, …) backend-agnostic so a SkiaSharp backend can be swapped in behind the existing rendering seam. The spike found that on .NET 10 under Linux, **every** bare `System.Drawing` object construction (`Font`, `Pen`, `SolidBrush`, `GraphicsPath`, `Bitmap`) throws `DllNotFoundException` even with `libgdiplus` installed — this rules out any "wrap and translate GDI+ descriptors" approach and confirms full type abstraction (not just a rendering-seam swap) is required before Linux/macOS chart rendering is possible at all. A hand-built Skia scene, written only against the abstracted port, rendered correctly and identically on both Windows and Linux, validating the design. See `docs/platform-support.md` for the current state of this migration.

## Decision: interfaces (not descriptor records) for every resource type, including pure-data ones

`Pen`, `Brush`, `Font`, and `StringFormat` are pure parameter bags and could have been modeled as `readonly record struct`s. Interfaces were chosen uniformly instead, for extensibility and one consistent factory pattern — while allowing `record`-shaped concrete implementations behind those interfaces where a resource really is pure data.

## Decision: per-method/per-caller conversion instead of per-type

The original migration plan sequenced work by GDI+ *type* (convert all `Pen` usage, then all `Brush` usage, etc.). In practice, real painter code bundles types together (a single `DrawStringRel` call touches `Font`+`Brush`+`StringFormat`; `DrawPath` touches `Pen`+`GraphicsPath`), so "finish type X" checkboxes were redefined to mean "the interface + adapter for X is complete," with actual call-site conversion done per-method/per-file (tracked as Milestone B2) using the dual-overload pattern described in `docs/rendering-abstractions.md`.

## Decision: keep old GDI+-typed overloads alongside new interface-typed ones (never a big-bang signature change)

`IChartRenderingEngine`/`IGaugeRenderingEngine` and `ChartGraphics`/`GaugeGraphics` keep their original GDI+-typed methods indefinitely while new interface-typed siblings are added next to them. This is what allows the migration to proceed in small, independently buildable/testable increments without ever breaking the build; removing the temporary concrete overloads is its own later milestone (Chart's "D3"), not assumed to happen automatically once callers migrate.

## Decision: share pure resource interfaces between the Chart and Gauge engines, but not the adapters or clip-region interface

When the Gauge engine's migration began, Chart's already-drafted resource interfaces (`IPen`, `IBrush` family, `IChartFont`, `ITextFormat`, `IGraphicsPath`, `IChartImage`, `IImageDrawOptions`) were relocated from a Chart-only namespace into the neutral `Microsoft.Reporting.Rendering` namespace and reused as-is, rather than duplicated or cross-referenced. `IClipRegion` could not make the same move — it depends on `IChartRenderingEngine` to reach a live `Graphics`, a genuine per-engine coupling — so Gauge got its own, separately-defined `IGaugeClipRegion`. Concrete adapters (`GdiPen`, `GdiBrush`, etc.) are likewise implemented separately per engine even when identical in shape, rather than shared instances.

## Decision: `ImageLoader`'s DPI-mismatch rescaling was dropped, not ported (Chart engine)

GDI+'s image-loading path auto-rescaled images when their embedded resolution (`Image.HorizontalResolution`/`VerticalResolution`) differed from the target `Graphics.DpiX`/`DpiY` — a print/high-DPI nuance. `SKBitmap` carries no resolution metadata, so porting this faithfully would require adding meaningless-on-Skia resolution properties plus a resize capability, just for an edge case ordinary web/screen chart images essentially never hit. Decision: assume a fixed 96 DPI baseline and drop the rescale path entirely. This is a real, accepted behavior change on Windows too — an image asset explicitly authored at a non-96 DPI now renders at native pixel size instead of being auto-rescaled — judged acceptable since it's narrow, cosmetic, and not exercised by this engine's real usage.

## Decision: Map engine migration deferred, LOW priority, scheduled after PDF (2026-07-22)

The Map engine (`Microsoft.Reporting.Map.WebForms`) was found during Chart's D3 scoping to be a third, separate, parallel GDI+-coupled rendering engine (347 files, ~22,400 lines) — not shared with Chart or Gauge — that a full cross-platform migration would need to repeat the entire Ports & Adapters treatment for a third time. It's a real, wired-in RDL feature (`MapMapper.cs` + ~20 sibling mapper classes translate report-definition `<Map>` XML into it, the same pattern `ChartMapper`/`GaugeMapper` use), not dead code, but its built-in tile-service integration (`Microsoft.Reporting.Map.WebForms.BingMaps`) depends on Bing Maps, which Microsoft has end-of-lifed for RDL/RDLC consumers — preliminary research indicates neither the Bing Maps Free nor Enterprise tier is supported for this use case any longer.

Decision: deprioritize the Map engine to **LOW**, ordered after PDF Phase 1 in `TODO.md`'s priority table — not because the migration mechanics differ from Chart/Gauge, but because (a) its main built-in value-add (Bing tile layers) is already broken independent of any cross-platform work, and (b) PDF's own blocker is architecturally deeper (full Metafile/EMF replacement) and was already judged higher-priority than a third from-scratch engine migration. If Map is revisited, the tile-service question needs its own decision first — options include a Google Maps adapter (commercial API, usage-based billing, ToS review needed) or an OpenStreetMap adapter (tile-server-based, no API key, but usage-policy/self-hosting considerations for production volume) — before any GDI+→interface migration work on Map itself would be worth starting.
