# Platform support

## Current status

The new rendering abstractions are intended to support a gradual migration away from Windows-only assumptions. At the moment the work is best thought of as a compatibility layer rather than a complete cross-platform rewrite.

## Supported areas

| Area | Windows | Linux | macOS | Notes |
| --- | --- | --- | --- | --- |
| Excel rendering abstraction | Yes | Yes | Planned | Linux path uses ClosedXML |
| PDF rendering abstraction | Yes | Yes | Planned | Linux path uses PdfSharpCore |
| Embedded resource adaptation | Yes | Yes | Planned | First seam implemented in the HTML path |
| Factory-based renderer selection | Yes | Yes | Planned | Centralizes platform selection |

## Known gaps

- The broader reporting pipeline still contains legacy Windows-specific assumptions in other paths.
- Full fidelity for complex Excel or PDF layouts is not yet guaranteed.
- Additional renderers and output formats should be introduced behind the same abstraction pattern.

---

## Chart, Gauge, and Map rendering (GDI+)

There are **three separate, parallel GDI+-coupled rendering engines** in the solution, not one: Chart (`Microsoft.Reporting.Chart.WebForms`), Gauge (`Microsoft.Reporting.Gauge.WebForms`), and Map (`Microsoft.Reporting.Map.WebForms`). Chart and Gauge are actively being migrated to the Ports & Adapters design in `docs/rendering-abstractions.md`; Map's migration is deferred (LOW priority, scheduled after PDF Phase 1 — see `docs/decisions.md`), since its built-in Bing Maps tile-layer integration is independently end-of-lifed for RDL/RDLC consumers.

| Area | Windows | Linux/macOS | Notes |
| --- | --- | --- | --- |
| Chart rendering | Yes | No | Full GDI+→interface migration in progress; see `tasks/chart-gdi-type-abstraction.md` |
| Gauge rendering | Yes | No | Same migration, in progress; see `tasks/gauge-gdi-type-abstraction.md` |
| Map rendering | Yes | No | Deferred, LOW priority (after PDF Phase 1) — see `docs/decisions.md`; Bing Maps tile integration is end-of-lifed regardless, a Google Maps/OpenStreetMap adapter would be a prerequisite decision |
| Chart/Gauge Skia backend | N/A | Spike only | A hand-built scene renders correctly on both platforms through Skia, validating the design, but no real `Chart`/`Gauge` object can use it yet |

**Fundamental blocker (confirmed by a Phase 0 spike, 2026-07-18):** GDI+ cannot construct *any* `System.Drawing` object at all on Linux under .NET 10 — not even a bare `Font`/`Pen`/`Bitmap` — even with `libgdiplus` installed. This is deeper than a rendering-seam limitation: it blocks the whole `Chart.Save`/`ChartImage.GetImage` path today on non-Windows, independent of backend selection.

**`ChartImage.GetImage(float) : Bitmap`'s declared return type is itself a hard, external, GDI+-typed public API contract** — even a complete Skia backend must still produce a `System.Drawing.Bitmap` to satisfy it, which the point above makes impossible on Linux today. This is the actual current blocker to any cross-platform Chart rendering, more fundamental than "which backend is selected."

### Known permanent/architectural gaps (no cross-platform equivalent attempted)

- **Metafile/EMF export** (`ChartImage.SaveIntoMetafile`) — needs a raw Windows HDC (`Graphics.GetHdc()`); intrinsically Windows-only, guarded rather than ported.
- **`Pen.CustomStartCap`/`AdjustableArrowCap`** (custom arrow-shaped line caps) — one Chart site (`SmartLabels.cs`'s `DrawCallout`), no Skia equivalent; left concrete, low priority/cosmetic.
- **`GraphicsPathIterator`** (subpath/compound-path iteration) — no interface or Skia equivalent. Chart's `CalloutAnnotation.cs` worked around this with a hand-rolled `SplitAtMarkers` helper reading `IGraphicsPath.PathTypes`'s marker bits directly, rather than depending on the GDI+-only iterator type.
- **`GraphicsState`** (opaque snapshot from `Graphics.Save()`/`Restore()`) — no interface-typed equivalent exists anywhere in the port; found in `LineChart.cs`'s shadow-line block, not yet investigated further.
- **Gauge's `XamlRenderer.cs`/`XamlLayer.cs`** — architecturally blocked: arbitrary multi-stop `ColorBlend` gradients (no interface equivalent), arbitrary affine transforms including scale/shear (only rotate/translate are covered), and its geometry-parsing methods run with no live `GaugeGraphics`/`ResourceFactory` in scope at all.
- **Gauge's `HotRegionList.SetHotRegion`/`AddHotRegion` and `GaugeGraphics.DrawRadialSelection`** — systemic, concrete-only GDI+ hit-testing infrastructure used by every gauge element; no interface-typed overload exists anywhere. Bridged via a `UnwrapPath(IGraphicsPath):GraphicsPath` helper rather than converted; a distinct future milestone.
- **Gauge's `BufferBitmap`** — no `IRenderSurface`-equivalent abstraction yet (the Gauge analogue of Chart's own earlier `IRenderSurface`/`GdiRenderSurface` work), blocking `GaugeCore.Paint`/`PrintPaint`/`SaveTo`/`GetGraphics` and any second Gauge backend.

### Resolved gaps worth remembering (so they aren't rediscovered as "unsolved")

- **`GraphicsPath.Widen(Pen)`** (stroke-to-fill geometry) — has a real Skia equivalent via `SKPaint.GetFillPath` (Skia's own stroke-to-fill primitive), not a hand-rolled algorithm.
- **`ImageAttributes`/`ColorMatrix` hue-recolor, shadow-alpha, and plain-transparency scaling** — all three recurring shapes are covered by a single `IImageDrawOptions.SetChannelScale(r, g, b, a)` method (a diagonal-only `ColorMatrix`, not a full matrix). A structurally identical, still-unconverted site exists in Chart's `ChartGraphics.cs` (~line 418), confirming the gap and its fix are shared across engines.
- **Brush rotation/translation transforms** (`LinearGradientBrush.Transform`, `PathGradientBrush.RotateTransform`/`TranslateTransform`) — covered by `SetRotationTransform`/`RotateTransform`/`TranslateTransform` on `ILinearGradientBrush`/`IPathGradientBrush`, deliberately as literal 1:1 ports of specific GDI+ call sequences rather than a generalized settable transform (to avoid unverified matrix-composition-order risk).

## Guidance

When introducing a new renderer implementation, prefer:

- a small interface for the contract,
- a platform-specific implementation behind that contract,
- a factory or registration point for selection,
- and tests that verify the behavior rather than the visual output.
