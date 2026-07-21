# Gauge engine: GDI+ type abstraction

Sibling effort to `tasks/chart-gdi-type-abstraction.md`, applying the same pattern to
`Microsoft.Reporting.Gauge.WebForms` (the Gauge rendering engine — ~30,000 lines / 253 files,
completely separate from the Chart engine, never previously touched by any of this migration
work). Kicked off 2026-07-21, user-directed ("Let's begin the Gauge engine").

## 1. Scoping findings (2026-07-21)

Investigated before writing any code (see conversation for the full research pass). Key findings:

- **Production entry point**: `GaugeMapper.GetImage()` (Common project) → `GaugeContainer.SaveAsImage()`
  → `GaugeCore.SaveTo()` (the real chokepoint, parallel to Chart's `ChartImage.GetImage`) → `BufferBitmap`
  (own `new Bitmap`+`Graphics.FromImage` wrapper, unrelated to Chart's `ChartPicture`/`ChartImage`) →
  `GaugeCore.Paint()` → `GaugeGraphics`. Entirely separate infrastructure from the Chart engine — no
  sharing at all, as expected.
- **Already has a pre-A1-equivalent seam**: `GaugeGraphics : RenderingEngine`, `RenderingEngine : IGaugeRenderingEngine`
  dispatching to a `GdiGraphics : IGaugeRenderingEngine` field (currently the only real branch — an
  `Svg`/other `RenderingType` returns `null` from `RenderingObject`). Every member is concrete
  GDI+-typed (`Pen`, `Brush`, `Font`, `StringFormat`, `GraphicsPath`, `Region`, `Matrix`, `GraphicsState`)
  — structurally identical to what `IChartRenderingEngine` looked like before the Chart engine's own
  Milestone A. This means Gauge starts at Chart's Milestone-A line, not before it.
- **GDI+ construction-site inventory** (~249 sites across ~30 files — smaller and more concentrated
  than Chart's ~1000 across ~24 files): `new Matrix(` 79/17 files, `new GraphicsPath(` 67/16 files,
  `new SolidBrush(` 29/12 files, `new Pen(` 24/13 files, `new PathGradientBrush(` 11/7 files,
  `new Font(` 14/11 files, `new LinearGradientBrush(` 7/4 files, `new StringFormat(` 8/7 files,
  `new Bitmap(` 4/4 files, `new HatchBrush(`/`new TextureBrush(` 1 each, `Graphics.FromImage(` 3/2 files,
  `new Region(` 3/3 files. **Zero** `GraphicsPath.Widen`/`GraphicsPathIterator`/`AdjustableArrowCap`/
  custom line-caps anywhere — the trickiest Chart-engine gaps don't recur here. `GaugeGraphics.cs`
  alone carries ~20% of all sites (mirrors `ChartGraphics.cs`'s chokepoint role).
- **Interface reusability**: the Chart engine's pure resource interfaces (`IRenderingResource`, `IPen`,
  `IBrush`+family, `IChartFont`, `ITextFormat`, `IGraphicsPath`) had zero Chart-specific dependencies
  (only `System.Drawing`/`System.Drawing.Drawing2D`/`System.Numerics`) — confirmed by attempting the
  move and watching the build, not just by reading. **`IClipRegion` did NOT qualify** — its
  `GetBounds`/`IsEmpty`/`IsInfinite` take an `IChartRenderingEngine` parameter, a genuine Chart-specific
  dependency only surfaced by the attempted move (see chart-gdi-type-abstraction.md's A1 entry for the
  relocation details). `IDrawingResourceFactory`, `IChartRenderingEngine`, `IRenderSurface`/`Factory`,
  `IImageDrawOptions`, and the `Gdi`/`Skia` concrete adapter folders stayed in `Chart.WebForms.Rendering`
  — either genuinely Chart-shaped or backend-concrete, not pure contracts.
- **No visual-regression coverage exists for gauges** — the Chart engine's harness
  (`tests/Microsoft.ReportViewer.DataVisualization.VisualRegressionTests/`) has zero gauge baselines or
  sample gauge definitions. Building this from scratch is real, separate work, not yet started.

## 2. Design decision: interface sharing (user-directed, 2026-07-21)

Chose **"share the pure ones, relocate to a neutral namespace"** over duplicating a parallel interface
set or referencing Chart's namespace directly from Gauge code. Implemented as part of this same session
— see chart-gdi-type-abstraction.md's Milestone A1 entry for the mechanical details (which files moved,
which stayed, why `IClipRegion` stayed put). Net result:

- **Shared** (`Microsoft.Reporting.Rendering/`, new top-level folder, sibling to both engines):
  `IRenderingResource`, `IPen`, `IBrush` (+`ISolidBrush`/`ILinearGradientBrush`/`ITextureBrush`/
  `IHatchBrush`/`IPathGradientBrush`), `IChartFont`, `ITextFormat`, `IGraphicsPath`.
- **Gauge-owned** (new, this session): `IGaugeDrawingResourceFactory`, `Rendering/Gdi/{GdiPen,
  GdiBrushes, GdiChartFont, GdiTextFormat, GdiGraphicsPath, GdiResourceFactory}.cs` — near-identical
  in shape to the Chart engine's own `Rendering/Gdi/` adapters (they wrap the same `System.Drawing`
  types), but a separate, decoupled implementation per the chosen design — not a shared/reused class,
  matching the recommended option's explicit preview ("Gauge.WebForms gets its own... Gdi/, mirroring
  Chart's adapter shape").
- **Deliberately narrower than Chart's factory for now, and left as an open gap**: no image abstraction
  (`GdiTextureBrush`'s two constructors take a concrete `System.Drawing.Image` directly — there's no
  Gauge equivalent of `IChartImage` yet) and no clip-region abstraction (`Region`-typed clipping stays
  fully concrete — an `IGaugeClipRegion` mirroring Chart's `IClipRegion`-minus-`IChartRenderingEngine`
  shape hasn't been scoped). Both mirror gaps the Chart engine also only closed in a later pass (C4/C8),
  not part of its first A1/A2 either — not a shortcut unique to Gauge.
- **Known naming wart, not fixed**: the shared interfaces keep their Chart-flavored names (`IChartFont`
  in particular) since the user's direction was to relocate the namespace, not rename the types —
  renaming would ripple through every already-converted Chart-engine call site for a cosmetic gain.
  Worth a dedicated rename pass later if it reads awkwardly from Gauge code long-term.

## 3. Progress

### Milestone A — Foundation (no behavior change)

- [x] **A1 equivalent — interfaces**: satisfied by reuse via the relocation above; no new interfaces
  needed beyond `IGaugeDrawingResourceFactory` (Gauge-specific factory contract, narrower than Chart's
  — see §2).
- [x] **A2 equivalent — GDI+ adapter set**: [`Rendering/Gdi/`](../Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Gauge.WebForms/Rendering/Gdi/)
  (`GdiPen`, `GdiSolidBrush`/`GdiLinearGradientBrush`/`GdiTextureBrush`/`GdiHatchBrush`/
  `GdiPathGradientBrush`, `GdiChartFont`, `GdiTextFormat`, `GdiGraphicsPath`) plus
  `GdiResourceFactory : IGaugeDrawingResourceFactory`. Each adapter constructs the exact same concrete
  GDI+ object the engine builds today, mechanically mirroring the Chart engine's own A2 adapters (same
  wrapped types, same `Native*` property pattern). Builds clean, 0 errors.
- [x] **A3 equivalent — interface-typed `IGaugeRenderingEngine` overloads**: added ~21 interface-typed
  overloads to [`IGaugeRenderingEngine.cs`](../Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Gauge.WebForms/IGaugeRenderingEngine.cs)
  (one per GDI+-typed resource-parameterized member, including the Gauge-specific
  `FillPath(Brush, GraphicsPath, float angle, bool useBrushOffset, bool circularFill)` overload Chart
  doesn't have). Both implementers updated: `GdiGraphics` unwraps each interface via private `Native(...)`
  helpers and delegates to the existing concrete-typed call (same call, same output); `RenderingEngine`
  (the Gdi/Svg dispatcher) adds pure passthroughs to `RenderingObject`. Exposed `RenderingEngine.ResourceFactory`
  (settable, defaults to `new GdiResourceFactory()`) so gauge painter classes can reach it via
  `GaugeGraphics : RenderingEngine` inheritance, mirroring `ChartGraphics.ResourceFactory`. No caller
  passes the new overloads yet — that's Milestone B. Verified: build 0 errors, all 52 existing tests
  pass unchanged (pure additive surface, nothing new is called yet).
- [ ] **A4 (new, not in the Chart doc's original numbering) — clip-region abstraction**: scope an
  `IGaugeClipRegion`-equivalent for the 3 `new Region(` sites / `Region Clip` property, once real
  call-site conversion reaches them. Not started.

### Milestone B — Migrate the chokepoint

- [ ] **B1. Inject `IGaugeDrawingResourceFactory` into `GaugeGraphics`** as a constructor/settable field
  (mirrors Chart's B1a) — infrastructure only, not consumed yet.
- [ ] **B2. Real call-site conversion**, file by file, same bridge-at-the-sink pattern as the Chart
  engine: public model properties (e.g. any `Font`/`GraphicsPath` a consumer can set directly) stay
  concrete forever; only the rendering call itself moves to interface types. `GaugeGraphics.cs` first
  (chokepoint, ~20% of the surface), then the top files by construction-site count: `DigitalSegment.cs`,
  `BackFrame.cs`, `Knob.cs`, `GaugeCore.cs`, `StateIndicator.cs`, `CircularPointer.cs`, `CircularScale.cs`,
  `GaugeLabel.cs`, `XamlRenderer.cs`, `GaugeImage.cs`, `NumericIndicator.cs`, `LinearPointer.cs`,
  `ScaleBase.cs`, `CircularGauge.cs`/`LinearGauge.cs`.

### Milestone E0 equivalent — Visual regression harness

- [ ] **Build gauge test coverage from scratch**: no sample gauge definitions or baseline PNGs exist
  anywhere in `tests/`. Needs its own `SampleGauges.cs`-equivalent (building `GaugeContainer`/gauge
  objects directly against the internal engine API, mirroring `SampleCharts.cs`) plus baselines,
  before any real B2 call-site conversion can be verified byte-for-byte the way the Chart engine's
  conversions were. This blocks meaningful B2 progress — should be tackled early, likely right after
  B1, not deferred to the end.

## 4. Notes for future sessions

- Everything in §3 Milestone A is infrastructure only — **zero behavior change, unverified by any
  gauge-rendering test** (none exist yet). Treat A2/A3 as "builds clean" verified, not "renders
  correctly" verified, until B1/B2 + the E0-equivalent harness land.
- `GaugeCore.GetGraphics(renderingType, g, stream)` (`GaugeCore.cs:1447`) is the Gauge-engine analogue
  of Chart's `ChartRenderingEngine.RenderingObject` — worth checking whether it has the same kind of
  hard downcast/GDI+-typed-return issue D2's investigation found in `ChartImage.GetImage`, before
  assuming Gauge's own eventual D2 equivalent will be any easier.
- `BufferBitmap.cs` is Gauge's equivalent of `GdiRenderSurface`/`IRenderSurface` — not yet abstracted
  behind an interface at all (Chart's own `IRenderSurface` work was Milestone D1, much later than
  where Gauge currently stands). Do not assume it's already portable.
