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
- [x] **A4 (new, not in the Chart doc's original numbering) — clip-region abstraction**: **scoped and
  implemented (2026-07-21)**. Investigated by reading Chart's `IClipRegion`/`GdiClipRegion`
  in full (including every real call site) and every `System.Drawing.Region` touch point in the Gauge
  engine, not just grepping for `new Region(`.

  **Why `IClipRegion` can't just be relocated/reused (confirmed, not assumed):** its `GetBounds`/
  `IsEmpty`/`IsInfinite` members take an `IChartRenderingEngine` parameter purely to reach
  `engine.Graphics` (GDI+'s `Region.GetBounds(Graphics)`/`IsEmpty(Graphics)`/`IsInfinite(Graphics)` all
  require a live `Graphics`, confirmed by reading `GdiClipRegion`'s bodies — each is a one-line
  passthrough: `NativeRegion.GetBounds(engine.Graphics)` etc.). This is an engine-shaped dependency, not
  a Chart-domain one, but there's no existing shared "thing with a live `Graphics`" interface — so a
  Gauge equivalent needs its **own** `IGaugeClipRegion`, parameterized on `IGaugeRenderingEngine`
  instead of `IChartRenderingEngine`, not a relocation. Good news: `IGaugeRenderingEngine` already
  exposes `Graphics Graphics { get; set; }` (added in A3, mirrors `IChartRenderingEngine.Graphics`
  exactly), so no *new* interface needs inventing just to satisfy this — `IGaugeRenderingEngine` already
  plays the role `IChartRenderingEngine` plays for Chart, and `GaugeGraphics : RenderingEngine`
  transitively implements it, so real call sites can pass `this` exactly the way `ChartGraphics` does at
  its two real `GetBounds`/`IsEmpty` sites (both found via `this`, never any other object — see below).

  **Shape to build, mirroring `IClipRegion`/`GdiClipRegion` member-for-member:**
  - `IGaugeClipRegion : IRenderingResource` (Gauge-owned, `Rendering/IGaugeClipRegion.cs`) —
    `Intersect(RectangleF)`, `Intersect(IGraphicsPath)`, `Union(RectangleF)`, `Exclude(RectangleF)`,
    `Complement(IGraphicsPath)`, `Xor(RectangleF)`, `MakeEmpty()`, `MakeInfinite()`,
    `IsVisible(PointF)`, `Transform(Matrix3x2)`, `Translate(float, float)`, `Clone() : IGaugeClipRegion`,
    `GetBounds(IGaugeRenderingEngine)`, `IsEmpty(IGaugeRenderingEngine)`, `IsInfinite(IGaugeRenderingEngine)`.
  - `GdiClipRegion` (Gauge-owned, `Rendering/Gdi/GdiClipRegion.cs`) — separate class from Chart's
    identically-shaped one, same decoupled-adapter precedent as every other Gauge Gdi adapter; wraps
    `System.Drawing.Region`, plus a `GdiClipRegion(Region existingRegion)` constructor (needed to wrap
    the live `base.Clip`/`g.Clip` at the swap sites below, mirroring Chart's identical constructor).
  - `IGaugeDrawingResourceFactory` additions: `CreateRegion()`, `CreateRegion(RectangleF)`,
    `CreateRegion(IGraphicsPath)` — mirrors Chart's factory; `GdiResourceFactory` implements each via
    `new GdiClipRegion(...)`.
  - `IGaugeRenderingEngine`/`RenderingEngine`/`GdiGraphics` additions: `IGaugeClipRegion GetClipRegion()`
    and `void SetClipRegion(IGaugeClipRegion region)`, added **alongside** the existing concrete
    `Region Clip { get; set; }` property (kept, not replaced — same dual-surface approach Chart used:
    `ChartRenderingEngine` keeps both `Clip` and `GetClipRegion`/`SetClipRegion` side by side).

  **Every real `Region`-touching site in the Gauge engine, and which the new abstraction would/wouldn't
  reach cleanly (found by reading each site's full method, not just the matching grep line):**
  1. `GaugeGraphics.DrawPathAbs` (`GaugeGraphics.cs`) — `Region clip = base.Clip; base.Clip = new
     Region(path); DrawImage(image, ...); base.Clip = clip;`. A pure clip-swap with **no**
     `GetBounds`/`IsEmpty` query, so it wouldn't even need the `IGaugeRenderingEngine` parameter —
     just `GetClipRegion()`/`SetClipRegion(resourceFactory.CreateRegion(path))`. **But** the
     `DrawImage(image, ...)` call in the same block is still fully concrete (`Image`/`ImageAttributes`
     parameters) — `IGaugeRenderingEngine` has no `DrawImage(IChartImage, ...)` overload yet (Chart's
     does: confirmed Chart's `IChartRenderingEngine.DrawImage(IChartImage, Rectangle, ...)` exists but
     Gauge's `IGaugeRenderingEngine.DrawImage` overloads are still `Image`-only). Converting this site's
     clip lines alone is easy; converting the whole block (matching the "no half-migrated call site"
     spirit already followed elsewhere) also needs a `DrawImage(IChartImage, ...)` sibling added first —
     a small, separate, not-yet-scoped gap, same shape as the `GetTextureBrush`/`ImageLoader` prerequisite
     was for brushes. `DrawPathAbs` is already blocked on the brush side too (§Milestone B2 findings), so
     this method doesn't fully convert regardless until both gaps close.
  2. `BackFrame.DrawFrameImage` (`BackFrame.cs`) — same clip-swap shape (`Region clip = null; ... region =
     new Region(graphicsPath); clip = g.Clip; g.Clip = region; ... g.Clip = clip;`), engine passed as an
     ordinary parameter (`GaugeGraphics g`) rather than `this`/`base.` — still trivially usable, since `g`
     is an `IGaugeRenderingEngine` either way. Same `DrawImage`-still-concrete caveat as site 1 applies
     (this method also calls `g.DrawImage(image, destRect, ...)` with concrete `Image`/`ImageAttributes`).
  3. `GaugeCore.GetClipRegion()` (private helper, `GaugeCore.cs`) — builds `new Region(graphicsPath)` from
     the union of all hot-region paths. Feeds sites 4/5 below. Could return `IGaugeClipRegion` instead,
     but its only callers are sites 4/5, which don't go through `IGaugeRenderingEngine` at all (next point)
     — so converting it in isolation buys nothing until 4/5 do too.
  4. `GaugeCore.Paint(Graphics g)` and 5. `GaugeCore.SaveTo(...)` (`GaugeCore.cs`) — both do
     `Region clip = bufferBitmap.Graphics.Clip; bufferBitmap.Graphics.Clip = GetClipRegion(); ...
     bufferBitmap.Graphics.Clip.Dispose(); bufferBitmap.Graphics.Clip = clip;` against
     **`BufferBitmap.Graphics`, a raw `System.Drawing.Graphics`** — not a `GaugeGraphics`/
     `IGaugeRenderingEngine` at all. `BufferBitmap` isn't abstracted behind any interface yet (already
     flagged in §4 Notes as the Gauge analogue of Chart's `IRenderSurface`/Milestone D1, i.e. separate,
     later-scoped work). `IGaugeClipRegion` can't reach these two sites without either `BufferBitmap`
     gaining an `IGaugeRenderingEngine`-shaped wrapper first, or a lower-level `IGaugeClipRegion`
     constructor overload that accepts a raw `Graphics` — out of scope for A4 itself.

  **Net scope call:** A4's `IGaugeClipRegion`/`GdiClipRegion`/factory-methods/`GetClipRegion`/
  `SetClipRegion` infrastructure is small and mechanical to add (closely mirrors A2/A3's existing
  adapters) and would be purely additive like every other Milestone A step. But **real call-site
  conversion is more entangled than the "3 `new Region(` sites" framing suggested**: 2 of the 5 sites
  (`GaugeCore.Paint`/`SaveTo`) sit behind `BufferBitmap`'s still-fully-concrete `Graphics`, not reachable
  without that separate, larger D1-equivalent milestone; the other 2 real engine-level sites
  (`DrawPathAbs`/`DrawFrameImage`) are clip-swaps that convert easily on their own but sit in methods
  already blocked elsewhere (brush cluster / concrete `DrawImage`), so converting just the clip lines
  would leave a half-migrated method rather than fully closing either site. Recommend building the A4
  infrastructure (mechanical, low-risk, unlocks nothing prematurely) but treating full real-site
  migration as bundled with whichever future pass finally unblocks `DrawPathAbs`/`DrawFrameImage`
  wholesale (needs the `DrawImage(IChartImage, ...)` sibling too) — not a standalone quick win.

  **Full migration done (2026-07-21), user-directed ("proceed with full migration").** Built the A4
  infrastructure exactly as scoped: `IGaugeClipRegion` (`Rendering/IGaugeClipRegion.cs`, Gauge-owned,
  14 members mirroring Chart's `IClipRegion`), `GdiClipRegion` (`Rendering/Gdi/GdiClipRegion.cs`),
  `IGaugeDrawingResourceFactory.CreateRegion()`/`CreateRegion(RectangleF)`/`CreateRegion(IGraphicsPath)`,
  and `GetClipRegion()`/`SetClipRegion(IGaugeClipRegion)` added alongside the existing concrete `Clip`
  property on `IGaugeRenderingEngine`/`RenderingEngine`/`GdiGraphics`. Also added
  `DrawImage(IChartImage, Rectangle, int, int, int, int, GraphicsUnit, IImageDrawOptions)` (only the one
  overload actually needed by both real call sites — Chart's 3-overload mirror wasn't fully ported since
  the other two have no caller here).

  Then closed the real sites, using Chart's exact resolution for the harder one: **`GaugeGraphics.cs`'s
  real `DrawPathAbs` calls `resourceFactory.CreateRegion(path)` where Chart's already-interface-typed
  `DrawPathAbs(IGraphicsPath path, ...)` sibling exists — checking that method's signature (not just the
  snippet) showed Chart resolved its identical "shared `pen`/`solidBrush` fields + concrete `GraphicsPath`"
  blocker not by converting the concrete overload in place, but by adding a fully-interfaced
  `DrawPathAbs(IGraphicsPath, ...)` sibling that builds its own local `IPen`/`IBrush` via
  `resourceFactory` instead of touching the shared fields — and confirmed (via grep across all of
  `Chart.WebForms/`) that overload has zero real callers even in Chart; it's purely additive, the same
  "build the port before any caller migrates" C4 pattern used throughout this migration.** Mirrored it
  exactly for Gauge: added `GaugeGraphics.DrawPathAbs(IGraphicsPath path, ...)` (same signature shape,
  no shadow-parameter overload needed since Gauge's concrete `DrawPathAbs` never had one either), reusing
  the already-existing `GetGradientBrushResource`/`GetHatchBrushResource`/`GetTextureBrushResource` and
  the new `GetClipRegion`/`SetClipRegion`/`DrawImage(IChartImage, ...)`. Purely additive/unreachable for
  now (no real Gauge caller builds its path as `IGraphicsPath` yet — all of `CircularScale.GetBarPath`
  and friends stay concrete), same as Chart's.

  One new bridge needed that Chart's version didn't (Chart's `CreateRegion(path)` call already had an
  `IGraphicsPath` in scope, since its whole method took one): Gauge's real, still-concrete call sites
  (`BackFrame.DrawFrameImage`) only ever have a concrete `GraphicsPath` on hand (from `GetFramePath`,
  which has 9 other callers and wasn't touched). Added `IGaugeDrawingResourceFactory.WrapPath(GraphicsPath) : IGraphicsPath`
  (`GdiResourceFactory.WrapPath` → `new GdiGraphicsPath(path)`, using a new wrapping constructor on
  `GdiGraphicsPath` that stores the passed-in native path instead of building one) — the direct analogue
  of `WrapImage`'s role for the image-loading prerequisite. This let `BackFrame.DrawFrameImage`'s
  clip-swap (`Region clip = g.Clip; g.Clip = new Region(graphicsPath); ...; g.Clip = clip;`) convert to
  `IGaugeClipRegion`/`GetClipRegion`/`SetClipRegion` **without** needing `GetFramePath` itself to change.

  **`DrawFrameImage`'s image-draw call deliberately left on the old concrete `DrawImage`/`ImageAttributes`
  overload — a new, small gap found, not fixed:** its hue-recolor branch needs a raw `ColorMatrix` with
  `Matrix00`/`Matrix11`/`Matrix22` channel scaling, which has no equivalent on `IImageDrawOptions` (only
  `SetColorRemap`/`SetTransparentColor`/`SetWrapMode`/`SetOpacity` exist — none model an arbitrary
  per-channel scale). Converting just the clip lines while leaving the image-draw concrete is a valid
  partial conversion (the two concerns don't share any local state), not a half-finished method — matches
  this migration's precedent of independently converting whatever sub-parts of a method are genuinely
  self-contained. `GaugeCore.Paint`/`SaveTo` remain untouched, exactly as scoped (blocked behind
  `BufferBitmap`'s un-abstracted `Graphics`, a separate D1-equivalent milestone).

  Verified: build 0 errors, full suite 54/54 passing, zero baseline diffs (all new surface is either
  unreachable — `DrawPathAbs(IGraphicsPath, ...)` — or a clip-swap around already-passing gauge frame
  rendering, confirmed byte-for-byte unchanged).

  **Hue-recolor gap resolved (2026-07-21), user-directed ("Begin resolution of hue-recolor with
  IImageDrawOptions").** Added `IImageDrawOptions.SetChannelScale(float red, float green, float blue,
  float alpha)` to the **shared** interface (`Microsoft.Reporting.Rendering/IImageDrawOptions.cs`) —
  abstracts GDI+'s `ColorMatrix` diagonal (`Matrix00`/`Matrix11`/`Matrix22`/`Matrix33`). Deliberately one
  combined method rather than extending `SetOpacity` separately: GDI+'s `ImageAttributes.SetColorMatrix`
  replaces the whole matrix on each call rather than merging with a prior one, so a caller needing both
  colour scale and opacity together (found below) must set every diagonal entry in one call. Implemented
  in all 3 existing implementers — Gauge's `GdiImageDrawOptions` (`Rendering/Gdi/GdiImage.cs`), Chart's
  `GdiImageDrawOptions`, and Chart's `SkiaImageDrawOptions` spike stub (throws `NotImplementedException`,
  matching its existing pattern for every other member). Confirmed the exact same shape recurs in Chart's
  own `ChartGraphics.cs` (~line 418, a marker-image shadow using `Matrix00`/`Matrix11`/`Matrix22` = 0.25
  plus `Matrix33` = 0.5 for a dimmed alpha) — not converted here since it's Chart-engine scope, but noted
  as evidence this is a genuinely shared gap, not Gauge-specific, and `SetChannelScale`'s 4-parameter
  shape (covering combined colour+alpha scaling) was chosen so it can close that site too later without
  a second interface change.

  Converted `BackFrame.DrawFrameImage` fully: `ImageAttributes imageAttributes` → `IImageDrawOptions`
  via `ResourceFactory.CreateImageDrawOptions()`; `SetColorKey` → `SetTransparentColor`; the hue branch's
  raw `ColorMatrix` → `imageAttributes.SetChannelScale(r, g, b, 1f)` (alpha `1f` reproduces the original's
  untouched-alpha behavior, since `new ColorMatrix()`'s default diagonal is already identity); the
  `DrawImage(Image, ...)` call → `DrawImage(ResourceFactory.WrapImage(image), ...)`. This closes out
  `DrawFrameImage` completely — no remaining concrete GDI+ resource types in the method other than the
  still-intentionally-concrete `graphicsPath`/`pen` (bridged via `WrapPath`/kept local respectively).

  Verified: build 0 errors, full suite 54/54, zero baseline diffs — `DrawFrameImage` isn't exercised by
  either current sample gauge (neither sets a frame `Image`), so this is "builds clean, behavior-identical
  by construction" verified, not pixel-verified; a future sample gauge with a frame image (plain and
  hue-recolored) would be worth adding to actually exercise this path, same open item already noted for
  the hatch/gradient/texture brush cluster.

  **Now pixel-verified (2026-07-21), user-directed ("proceed").** Added
  [`SampleGauges.RenderCircularGaugeWithFrameImage`](../tests/Microsoft.ReportViewer.DataVisualization.VisualRegressionTests/SampleGauges.cs) —
  a 64x64 in-memory bitmap (CornflowerBlue background, white filled circle) registered via
  `container.NamedImages`, assigned as `BackFrame.Image` with `ClipImage = true` (exercises the
  `IGaugeClipRegion` clip-swap), `ImageTransColor = CornflowerBlue` (exercises `SetTransparentColor`),
  and `ImageHueColor = Firebrick` (exercises `SetChannelScale`) — all three of `DrawFrameImage`'s
  converted code paths in one render. Rendered, visually inspected the PNG (via `Read`): a circular
  gauge with a Firebrick-tinted disc (white circle × Firebrick's R/G/B factors ≈ Firebrick — confirms
  `SetChannelScale` is scaling correctly) clipped to the circular frame ring, CornflowerBlue keyed
  transparent, default blue/white scale ticks and needle drawn normally on top — no garbage or
  corruption. Promoted to `Baselines/CircularGaugeWithFrameImage.png`. Added
  `CircularGaugeWithFrameImage_MatchesBaseline` to `GaugeVisualRegressionTests.cs`. Verified: build 0
  errors, full suite now 55/55 (54 `VisualRegressionTests` — up from 53, +1 — + 1 `Chart.Rdl.Tests`),
  zero baseline diffs on the pre-existing tests, new test passing byte-for-byte against its own
  freshly-promoted baseline.

### Milestone B — Migrate the chokepoint

- [x] **B1. Inject `IGaugeDrawingResourceFactory` into `GaugeGraphics`** (2026-07-21) — turned out to
  already be satisfied by inheritance, not a new field: `GaugeGraphics : RenderingEngine`, and
  `RenderingEngine.ResourceFactory` (settable, defaults to `new GdiResourceFactory()`) was already
  added during A3 (see above) specifically so gauge painter classes could reach it. Unlike Chart, where
  `ChartGraphics` doesn't inherit from a class carrying the factory (hence needing its own field for
  B1a), `GaugeGraphics` gets it for free via the base class — adding a second, redundant field on
  `GaugeGraphics` itself would just be duplication. No code change; documenting the check as done since
  the infrastructure genuinely exists and is reachable (`this.ResourceFactory` from any `GaugeGraphics`
  method).
- [~] **B2. Real call-site conversion** — started (2026-07-21), first slice landed; large remaining
  scope, same shape as the Chart engine's own multi-session B2 saga (see
  `chart-gdi-type-abstraction.md`'s Milestone B2 entry for the pattern this is following: shared
  brush/pen locals mixing multiple concrete-returning helpers block naive conversion; only genuinely
  self-contained call clusters convert cleanly without a much larger atomic pass).
  - **Landed**: `GaugeGraphics.cs`'s design-time/interactive **selection-drawing cluster**
    (`GetDesignTimeSelectionFillBrush`, `GetDesignTimeSelectionBorderPen`, `DrawSelection` (both
    overloads), and the marker-drawing half of `DrawRadialSelection`) — chosen because tracing actual
    callers showed this cluster is genuinely self-contained: none of `DrawSelection`'s parameters are
    concrete GDI+ resource types (only `RectangleF`/`bool`/`Color`), and its internal `Pen`/`Brush`
    locals only ever flow into `DrawRectangle`/`FillEllipse`/`DrawEllipse` — all of which already have
    interface-typed overloads on `RenderingEngine` from A3. Converted `GetDesignTimeSelectionFillBrush`
    or `GetDesignTimeSelectionBorderPen`'s return types directly to `IBrush`/`IPen` (both had exactly 2
    internal-only callers, safe to retype outright, no dual-overload needed). Added a new sibling
    `GetSelectionPenResource(bool, Color) : IPen` alongside the original concrete `GetSelectionPen`
    (dual-overload strategy, same as Chart's) — the original stays because `DrawRadialSelection`'s
    `DrawPath(pen, selectionPath)` call needs a concrete `Pen` (its `GraphicsPath` parameter is built by
    `CircularScale.GetBarPath` and stays concrete; no mixed `DrawPath(IPen, GraphicsPath)` overload
    exists, matching Chart's own `DrawPathAbs` finding). `DrawRadialSelection`'s marker-drawing loop
    (independent of the path) converted fully to `IBrush`/`IPen`.
  - **New, small, genuine gap found and fixed**: `IPen` (shared `Microsoft.Reporting.Rendering`
    interface) had no `DashPattern` member — `GetSelectionPen`'s dotted-selection-outline behavior sets
    a custom `{2f, 2f}` dash cadence that GDI+'s built-in `DashStyle.Dot` alone doesn't reproduce. Added
    `float[] DashPattern { get; set; }` to `IPen` and implemented it in all 3 existing implementers:
    Gauge's `GdiPen` (forwards to `NativePen.DashPattern`), Chart's `GdiPen` (same), and Chart's spike
    `SkiaPen` (plain auto-property, spike-scope, matching its existing dash/cap/join stubs). Small,
    additive, harmless regardless of what converts next — same category as the Chart engine's own
    mid-investigation `ILinearGradientBrush.LinearColors` addition.
  - **Not exercised by the current visual-regression harness** — `DrawSelection`/`DrawRadialSelection`
    only run for design-time/interactive selection rendering (report designer, image-map selection),
    not a plain `SaveAsImage` render of an unselected gauge, so `SimpleCircularGauge`/`SimpleLinearGauge`
    don't touch this code. Safety instead comes from the adapters being trivially behavior-identical
    (`GdiPen`/`GdiSolidBrush` construct the exact same concrete GDI+ object the code built directly
    before) — same reasoning already relied on for A2. Verified: build 0 errors, full suite still 54/54
    (zero baseline diffs, as expected — this code path isn't in either sample).
  - **Deliberately not attempted this pass** — the much larger, harder cluster: `GetHatchBrush`/
    `GetGradientBrush`/`GetPieGradientBrush`/`GetTextureBrush`/`CreateBrush`/`GetCircularRangeBrush`/
    `GetLinearRangeBrush`/`GetMarkerBrush`/`DrawPathAbs` all interchange results through shared
    `Brush brush`/`Brush brush2` locals (confirmed by reading `BackFrame.GetBrush`'s call to
    `GaugeGraphics.GetHatchBrush`, which feeds the same kind of multi-branch shared local Chart hit) —
    this is the actual chokepoint code (used by every real gauge render, unlike the selection cluster),
    and per the Chart engine's precise findings, converting it requires either a large atomic pass
    across all of `GetHatchBrush`/`GetGradientBrush`/`GetPieGradientBrush`/`GetTextureBrush` plus their
    shared-local consumers, or solving `common.ImageLoader`'s concrete-`Image`-loading prerequisite
    first (`GetTextureBrush` calls `common.ImageLoader.LoadImage(name)` directly). Left for a future
    session — do not attempt a partial slice of this cluster without re-reading Chart's B2 findings in
    full first, since the exact same trap (reverted attempts #1/#2) is very likely to recur here.
  - Also shared `pen`/`solidBrush` instance fields on `GaugeGraphics` (used by `DrawPathAbs`) remain
    untouched — same reasoning as Chart's B1b: `pen` is drawn via `DrawPath(pen, path)` with a
    caller-supplied concrete `GraphicsPath`, and `solidBrush` is interchanged with the same shared-local
    brush-family results above.
  - **Groundwork continued (2026-07-21), following the Chart engine's own C4 precedent: build the interface-typed brush-getter siblings first, even before any real caller can migrate.** Added `GetHatchBrushResource`, `GetGradientBrushResource`, `GetPieGradientBrushResource` — interface-typed (`IHatchBrush`/`IBrush`/`IPathGradientBrush`) siblings of `GetHatchBrush`/`GetGradientBrush`/`GetPieGradientBrush`, each mirroring its original's body exactly but sourcing every resource from `ResourceFactory` instead of `new Xxx(...)`. Chose these three specifically because they're genuinely self-contained: all their geometry (`GetGradientBrush`'s/`GetPieGradientBrush`'s path-gradient branches) is built locally from method parameters via `ResourceFactory.CreatePath()`, with no shared instance field and — critically — no `common.ImageLoader` dependency, unlike `GetTextureBrush`. `GetHatchBrush`'s sibling had to become an instance method (the original is `static`) purely to reach `ResourceFactory`.
  - **Still deliberately not attempted**: the actual real callers (`BackFrame.GetBrush`, `DrawPathAbs`, `CreateBrush`, `GetCircularRangeBrush`, `GetLinearRangeBrush`) all interchange these three getters' results with `GetTextureBrush`'s in the same shared `Brush brush` local — migrating any of them still requires `GetTextureBrush` to have an interface-typed sibling too, which needs `common.ImageLoader`'s image-loading path bridged to an interface type first (Gauge's `IGaugeDrawingResourceFactory` has no image-abstraction members yet, per the A2 scope note). This is the identical shape of Chart's own `GetTextureBrush`/`ImageLoader`/DPI saga (`chart-gdi-type-abstraction.md` Milestone B2, the multi-session "Attempted the image-loading prerequisite" thread) — not attempted here to avoid repeating that multi-session investigation speculatively. `GetMarkerBrush` was also considered and found blocked for a different, new reason: its gradient-brush branches set `((LinearGradientBrush)brush).Transform`/`((PathGradientBrush)brush).RotateTransform`/`.TranslateTransform` — GDI+ brush-transform members with **no equivalent on `ILinearGradientBrush`/`IPathGradientBrush` today** (a real, small future gap, analogous to Chart's missing `IPen.Alignment`/`Clone()` discoveries, just not filled yet since nothing in this pass needed it filled).
  - Verified: build 0 errors, full suite still 54/54, zero baseline diffs (purely additive, unreachable surface — same verification shape as Chart's own C4 "port + adapter complete" sub-steps).
  - Real call-site conversion continues file by file after the brush cluster is solved: `GaugeGraphics.cs`
    (chokepoint, ~20% of the surface, this pass's partial start), then the top files by
    construction-site count: `DigitalSegment.cs`, `BackFrame.cs`, `Knob.cs`, `GaugeCore.cs`,
    `StateIndicator.cs`, `CircularPointer.cs`, `CircularScale.cs`, `GaugeLabel.cs`, `XamlRenderer.cs`,
    `GaugeImage.cs`, `NumericIndicator.cs`, `LinearPointer.cs`, `ScaleBase.cs`,
    `CircularGauge.cs`/`LinearGauge.cs`.
  - **GetTextureBrush/ImageLoader prerequisite — solved (2026-07-21)**, per the handoff doc
    (`tasks/gauge-texturebrush-imageloader-handoff.md`). User chose **Option B** (full abstraction,
    not the quick concrete-`Image`/`ImageAttributes`-parameter shortcut): confirmed `ImageLoader.LoadImage`
    has no DPI logic (unlike Chart's), so the DPI rabbit hole did not recur. Relocated Chart's
    `IChartImage`/`IImageDrawOptions` (previously Chart-only, `Microsoft.Reporting.Chart.WebForms.Rendering`)
    to the shared `Microsoft.Reporting.Rendering` namespace — verified portable by attempting the move and
    building (0 errors): both interfaces only depend on `IRenderingResource` (already shared) and
    `System.Drawing`/`System.Drawing.Drawing2D`, no Chart-engine coupling. All Chart consumer files already
    had `using Microsoft.Reporting.Rendering;`, so no using-directive churn was needed there. Added
    Gauge-owned `GdiChartImage`/`GdiImageDrawOptions` adapters (`Rendering/Gdi/GdiImage.cs`) — separate
    classes from Chart's identically-shaped ones, per the established decoupled-adapter design (not shared
    instances). Extended `IGaugeDrawingResourceFactory`/`GdiResourceFactory` with `WrapImage(Image):IChartImage`,
    `CreateImageDrawOptions():IImageDrawOptions`, and replaced the two concrete-`Image`/`ImageAttributes`
    `CreateTextureBrush` overloads with `CreateTextureBrush(IChartImage, WrapMode)` /
    `CreateTextureBrush(IChartImage, RectangleF, IImageDrawOptions)` (safe to replace outright, not
    dual-overload, since neither concrete overload had any caller yet). Added
    `GaugeGraphics.GetTextureBrushResource` mirroring Chart's `GetTextureBrushResource` calling pattern
    exactly (`common.ImageLoader.LoadImage` stays concrete; `ResourceFactory.WrapImage(image)` bridges it
    into `IChartImage` at the call site) — simpler than Chart's version since Gauge's `GetTextureBrush` has
    no Metafile/backColor-compositing branch to mirror. Verified: build 0 errors, full suite 54/54, zero
    baseline diffs.
  - **Pure-rename attempt on `CreateBrush`/`GetCircularRangeBrush`/`GetLinearRangeBrush` (2026-07-21)** —
    only `CreateBrush` actually qualified. It has exactly one real caller
    (`NumericIndicator.DrawBackground`, via `g.CreateBrush(...)` feeding straight into
    `FillRectangle(Brush, RectangleF)`), a value-type-only consumption path with no concrete-`GraphicsPath`
    coupling. Added `CreateBrushResource : IBrush` (dual-overload, built from
    `GetTextureBrushResource`/`GetHatchBrushResource`/`GetGradientBrushResource`/`ResourceFactory.CreateSolidBrush`)
    and migrated the one caller to it (`NumericIndicator.cs`, now `using Microsoft.Reporting.Rendering;`).
    **`GetCircularRangeBrush`/`GetLinearRangeBrush` do NOT qualify** — reading their actual callers (not
    just the signature) shows every one of them assigns into a `Brush`-typed field on `BarStyleAttrib`
    (`primaryBrush`/`secondaryBrushes[]`/`totalBrush`, all concrete `Brush`) that is later consumed by
    `GaugeGraphics.FillPath(Brush, GraphicsPath)` alongside a concrete `GraphicsPath` field
    (`BarStyleAttrib.primaryPath`, etc. — confirmed in `CircularPointer.cs`/`LinearPointer.cs`). No mixed
    `FillPath(IBrush, GraphicsPath)` overload exists, the identical blocker already documented for
    `DrawPathAbs`. Converting either getter would require retyping `BarStyleAttrib`'s brush fields too,
    which is a materially larger, not-this-pass change. Left concrete, undocumented-no-longer: this finding
    replaces the handoff doc's hopeful "good pure-rename candidates" assumption for these two specifically.
  - **`DrawPathAbs`/`BackFrame.GetBrush` investigated, confirmed still blocked (2026-07-21)** — both read
    in full. `DrawPathAbs` mixes the shared `pen`/`solidBrush` instance fields with `brush`/`brush2` locals
    that flow into `FillPath(Brush, GraphicsPath)` and `DrawPath(Pen, GraphicsPath)` against a
    caller-supplied concrete `GraphicsPath` (same shape as previously documented). `BackFrame.GetBrush` has
    a second, independent blocker beyond the concrete-`GraphicsPath` coupling: its circular-gradient
    branches do `((LinearGradientBrush)brush).Transform = matrix` — a GDI+ brush-transform assignment with
    no equivalent on `ILinearGradientBrush` today (the same gap already noted for `GetMarkerBrush`'s
    `RotateTransform`/`TranslateTransform` calls). Neither converts this pass.

  - **Brush-transform gap closed (2026-07-21), user-directed ("Let's resume building out the remaining
    Gauge work").** Added `SetRotationTransform(float angle, PointF center)`, `RotateTransform(float angle,
    MatrixOrder order)`, and `TranslateTransform(float dx, float dy, MatrixOrder order)` to the **shared**
    `ILinearGradientBrush`/`IPathGradientBrush` interfaces (`Microsoft.Reporting.Rendering/IBrush.cs`).
    `SetRotationTransform` is a literal 1:1 port of the exact GDI+ call sequence both real call sites use
    (`new Matrix(); matrix.RotateAt(angle, center); brush.Transform = matrix;`) — deliberately not
    generalized to a `System.Numerics.Matrix3x2`-typed settable `Transform` property, to avoid
    reintroducing matrix-composition-order risk into behavior that's currently zero-risk-by-construction
    (same GDI+ method, same arguments, just behind an interface call). Implemented in all real
    implementers: Gauge's and Chart's `GdiLinearGradientBrush`/`GdiPathGradientBrush` (identical
    passthrough bodies); Chart's `SkiaLinearGradientBrush` spike stub throws `NotImplementedException`
    (matching its file's existing pattern for methods, as opposed to the auto-properties it already has);
    `IPathGradientBrush` has no Skia implementer to update (`SkiaResourceFactory.CreatePathGradientBrush`
    already throws directly, no concrete class exists).

  - **`BackFrame.GetBrushResource` added (2026-07-21)** — re-reading `GetBrush`'s actual signature (not
    just the earlier note) found it takes `RectangleF rect` directly, **not** a caller-supplied
    `GraphicsPath` — the "concrete-`GraphicsPath` coupling" in the earlier B2 entry was a misattribution;
    `GetBrush`'s only geometry is a `GraphicsPath` built and consumed entirely locally (the path-gradient
    branch), exactly the same self-contained shape already established for
    `GetGradientBrushResource`/`GetPieGradientBrushResource`. With the transform gap now closed, this
    converts cleanly. Added as a dual-overload sibling (`IBrush GetBrushResource(...)`), built from
    `GetHatchBrushResource`/`GetGradientBrushResource`/`ResourceFactory.CreateSolidBrush`/
    `ResourceFactory.CreatePathGradientBrush`. Still purely additive: `GetBrush`'s only real callers
    (inside `BackFrame.RenderFrame`) feed the result into `FillPath(Brush, GraphicsPath, ...)` alongside
    paths from `GetFramePath` (9 other callers, concrete, not touched this pass) — unreachable until that
    converts too, same "build the port before any caller migrates" shape as `DrawPathAbs(IGraphicsPath, ...)`.

  - **`GetMarkerBrush` investigated again with the transform gap closed — still blocked, for a genuinely
    new reason found this pass.** Unlike `GetBrush`, `GetMarkerBrush(GraphicsPath path, ...)` takes a
    caller-supplied concrete `GraphicsPath` and calls `path.GetBounds(matrix)` — GDI+'s
    transformed-bounds overload (bounds of the path after applying a rotation matrix, used by its
    Circle/DiagonalLeft/DiagonalRight branches) — and `IGraphicsPath` has no `GetBounds(Matrix3x2)`
    equivalent, only the parameterless `GetBounds()`. Deliberately did not add one this pass: unlike
    `SetRotationTransform` (a literal passthrough of an existing call), a `GetBounds(Matrix3x2)` addition
    would need matrix-type conversion (`System.Numerics.Matrix3x2` ↔ GDI+ `Matrix`) verified correct
    end-to-end, and `GetMarkerBrush` is *also* still blocked by the same shared-concrete-field pattern as
    `GetCircularRangeBrush`/`GetLinearRangeBrush` (`markerStyleAttrib.brush`/`knobStyleAttrib.brushes[]`,
    consumed by `FillPath(Brush, GraphicsPath)` against concrete path fields) — so closing the
    `GetBounds` gap alone wouldn't make this method reachable either. Left concrete; the `GetBounds(Matrix)`
    gap is now documented for whoever eventually tackles this method.

  - **`DigitalSegment.cs` investigated (next file in the planned conversion order) — found to be pure
    geometry with no `Brush`/`Pen` usage at all** (only `GraphicsPath`/`Matrix` construction, building
    7-/14-segment LED digit shapes), but still not convertible standalone: it's a `static` utility class
    (no `GaugeGraphics` instance, hence no `ResourceFactory` to reach), and its only consumer
    (`NumericIndicator.DrawSymbol`'s `Digital7Segment`/`Digital14Segment` branches) feeds every returned
    `GraphicsPath` straight into `g.FillPath(brush, path)` with a concrete `Brush` — no mixed
    `FillPath(Brush, IGraphicsPath)` overload exists. Converting `DigitalSegment`/`SegmentsCache` to
    `IGraphicsPath` would mean threading a factory parameter through ~15 static methods for zero reachable
    benefit until `DrawSymbol`'s own brush chain also converts — the same "large atomic pass, don't attempt
    a partial slice" trap already documented for the hatch/gradient/texture cluster. **Did find one genuine,
    self-contained win along the way**: `NumericIndicator.GetFontBrush` (feeds the `Digital7Segment`/
    `Digital14Segment` "dim/off LED" branches) has no shared field or path dependency at all — added
    `GetFontBrushResource : IBrush` as a private dual-overload sibling. Still additive/unreachable for the
    same `DigitalSegment`-output reason above. `DigitalSegment.cs`/`SegmentsCache.cs` left fully concrete —
    correctly identified as blocked, not attempted speculatively.

  Verified: build 0 errors, full suite 55/55 (54 `VisualRegressionTests` + 1 `Chart.Rdl.Tests`), zero
  baseline diffs — all new surface this round is either unreachable additive infrastructure
  (`GetBrushResource`/`GetFontBrushResource`) or implementer-only changes to already-adapter-tested
  interfaces (the transform methods), no real call site changed behavior.

### Milestone E0 equivalent — Visual regression harness

- [x] **Build gauge test coverage from scratch** (2026-07-21): added
  [`SampleGauges.cs`](../tests/Microsoft.ReportViewer.DataVisualization.VisualRegressionTests/SampleGauges.cs)
  (mirrors `SampleCharts.cs` — builds a bare `GaugeContainer` directly against the internal engine
  API, no .rdlc report or host required) with two samples: `RenderSimpleCircularGauge` (default
  `CircularScale` + one `CircularPointer` at 65) and `RenderSimpleLinearGauge` (`LinearGauge` forced
  to `GaugeOrientation.Horizontal` in a 300x100 container — the default `Auto` orientation on a
  300x300 square container produced overlapping/illegible scale labels; this is pre-existing engine
  behavior with a degenerate aspect ratio, not a bug, but a bad first baseline, so the sample uses a
  saner container shape instead). Added
  [`GaugeVisualRegressionTests.cs`](../tests/Microsoft.ReportViewer.DataVisualization.VisualRegressionTests/GaugeVisualRegressionTests.cs)
  (mirrors `ChartVisualRegressionTests.cs`) with `SimpleCircularGauge_MatchesBaseline` /
  `SimpleLinearGauge_MatchesBaseline`, reusing the existing `ImageComparer`/`Baselines/` machinery
  as-is (no changes needed — it's already generic over any PNG). Both render through
  `GaugeContainer.SaveAsImage(Stream)` → `GaugeCore.SaveTo` → `GdiGraphics`, exercising the
  concrete/untouched call paths (Milestone A added no new callers, so this is the first-ever test
  coverage of the Gauge engine's actual rendering, not a re-verification of anything). Rendered once,
  visually inspected both PNGs (recognizable circular dial with needle at ~65, recognizable
  horizontal linear scale with triangular pointer at ~65 — no garbage/corruption), then promoted
  directly to `Baselines/SimpleCircularGauge.png` / `Baselines/SimpleLinearGauge.png` — no
  `git stash` baseline dance needed here since nothing pre-existing was being changed, this is a
  first baseline for previously-uncovered code. Verified: build 0 errors, full suite 54/54 passing
  (53 `VisualRegressionTests` — up from 51, +2 new gauge tests — + 1 `Chart.Rdl.Tests`). This
  unblocks B1/B2 real call-site conversion, which can now be checked byte-for-byte the same way the
  Chart engine's conversions were.

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
