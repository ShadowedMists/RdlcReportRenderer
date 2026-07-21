# Handoff: Gauge engine's `GetTextureBrush`/`ImageLoader` prerequisite

Paste this whole file as the prompt for the next session. It's self-contained — you don't need
the conversation that produced it, only this doc plus the referenced source files.

## Context (read this first)

This is the next increment of the Gauge engine's GDI+-type-abstraction migration, tracked in
[`tasks/gauge-gdi-type-abstraction.md`](gauge-gdi-type-abstraction.md) (read Milestone B in full
before touching any code — it has the precise history of what's already done and why). That doc
mirrors [`tasks/chart-gdi-type-abstraction.md`](chart-gdi-type-abstraction.md), which did the same
migration for the Chart engine first; **read the Chart doc's Milestone B2 section too** (search for
"image-loading prerequisite" and "DPI") — this task is the Gauge-engine analogue of a prerequisite
Chart already solved, and the goal here is to reuse Chart's proven shape, not re-derive it from
scratch, while checking at each step whether Gauge's version is actually simpler (it looks like it is
— see "Good news" below).

**Current state, verified 2026-07-21:** `GaugeGraphics.cs` has three brush-getters already converted
to interface-typed siblings (`GetHatchBrushResource`, `GetGradientBrushResource`,
`GetPieGradientBrushResource` — all in `GaugeGraphics.cs`, search for them to see the established
pattern: mirror the original method's body exactly, source every resource from
`RenderingEngine.ResourceFactory` instead of `new Xxx(...)`, keep the original concrete method
untouched alongside it). `GetTextureBrush` is the fourth, deliberately left undone, because it depends
on `common.ImageLoader.LoadImage(string) : Image` — a concrete `System.Drawing.Image`-returning call —
and Gauge's `IGaugeDrawingResourceFactory` has no image-abstraction members. This is what blocks real
callers (`BackFrame.GetBrush`, `DrawPathAbs`, `CreateBrush`, `GetCircularRangeBrush`,
`GetLinearRangeBrush`) from migrating at all: each interchanges results from all four brush-getters
through one shared `Brush brush` local, so a shared local can only become type `IBrush` once *every*
branch that can assign into it returns an `IBrush`-compatible type — including the texture-brush
branch.

## The exact code to look at

- **`GaugeGraphics.GetTextureBrush`** (`Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Gauge.WebForms/GaugeGraphics.cs`, currently around line 81-91):
  ```csharp
  internal Brush GetTextureBrush(string name, Color backImageTranspColor, GaugeImageWrapMode mode)
  {
      Image image = common.ImageLoader.LoadImage(name);
      ImageAttributes imageAttributes = new ImageAttributes();
      imageAttributes.SetWrapMode((WrapMode)((mode == GaugeImageWrapMode.Unscaled) ? GaugeImageWrapMode.Scaled : mode));
      if (backImageTranspColor != Color.Empty)
      {
          imageAttributes.SetColorKey(backImageTranspColor, backImageTranspColor, ColorAdjustType.Default);
      }
      return new TextureBrush(image, new RectangleF(0f, 0f, image.Width, image.Height), imageAttributes);
  }
  ```
- **The shared-local consumers that need this to unblock them** (all in `Microsoft.Reporting.Gauge.WebForms/`):
  `GaugeGraphics.DrawPathAbs` (mixes `GetHatchBrush`/`GetGradientBrush`/`GetTextureBrush`/`solidBrush` field into one `Brush brush`/`Brush brush2` pair), `GaugeGraphics.CreateBrush`, `GaugeGraphics.GetCircularRangeBrush`, `GaugeGraphics.GetLinearRangeBrush` (all value-type-only signatures — good pure-rename candidates once unblocked, same shape as Chart's `FillRectangleRel`/`FillRectangleAbs`), and `BackFrame.GetBrush` (`Microsoft.Reporting.Gauge.WebForms/BackFrame.cs`, around line 946).
- **`common.ImageLoader`** — `Microsoft.Reporting.Gauge.WebForms/ImageLoader.cs`. **Good news, checked already**: unlike Chart's `ImageLoader.cs`, Gauge's has **no DPI-matching logic at all** (no `GetAdjustedImageSize`/`DoDpisMatch`/`GetScaledImage` equivalents) — `LoadImage` just resolves a name/URL/file to an `Image` and caches it in a `Hashtable`. This means the DPI-decision rabbit hole that consumed a large chunk of Chart's session (`chart-gdi-type-abstraction.md`, search "DPI") **should not recur here** — confirm this by reading the file yourself before assuming it, but it looks like the actual prerequisite is smaller for Gauge than it was for Chart.
- **`IGaugeDrawingResourceFactory`** (`Microsoft.Reporting.Gauge.WebForms/Rendering/IGaugeDrawingResourceFactory.cs`) and **`GdiResourceFactory`** (`Rendering/Gdi/GdiResourceFactory.cs`) — already have:
  ```csharp
  ITextureBrush CreateTextureBrush(Image image, WrapMode wrapMode);
  ITextureBrush CreateTextureBrush(Image image, RectangleF rect, ImageAttributes attributes);
  ```
  **Read this carefully**: both overloads already take the *concrete* `Image`/`ImageAttributes` types directly, not an abstraction. This is a documented, accepted gap from Milestone A2 ("no image abstraction yet"). It means a `GetTextureBrushResource` sibling could be written **today**, trivially, just by calling the existing `ResourceFactory.CreateTextureBrush(image, rect, imageAttributes)` overload — see "Option A" below.

## The decision this session needs to make (don't pick unilaterally — this mirrors a real design fork Chart hit)

**Option A — quick, but leaves a known leak.** Add `GetTextureBrushResource` using the *existing*
`CreateTextureBrush(Image, RectangleF, ImageAttributes)` overload — a near copy-paste of
`GetTextureBrush`'s body, just swapping `new TextureBrush(...)` for `ResourceFactory.CreateTextureBrush(...)`.
This compiles today, unblocks the shared-local consumers immediately, and is consistent with the
already-accepted A2 scope note. **But** it means `IGaugeDrawingResourceFactory` still has two
GDI+-typed parameters (`Image`, `ImageAttributes`) in its public contract — a real Skia/other backend
couldn't implement this cleanly, so the "backend-agnostic interface" goal isn't actually met for this
one method, just deferred again.

**Option B — the real fix, mirrors what Chart eventually did.** Chart hit the identical problem
(`chart-gdi-type-abstraction.md`, Milestone B2, "Implemented (2026-07-18) via a dual-overload
strategy") and solved it by adding:
- `IDrawingResourceFactory.WrapImage(Image) : IChartImage` (wraps an already-loaded concrete `Image`
  without reconstructing it — Gdi implementation: `new GdiChartImage(image)`).
- `IImageDrawOptions.SetTransparentColor(Color)` and `SetWrapMode(WrapMode)` (Gdi: `ImageAttributes.SetColorKey`/`SetWrapMode`).
- `IDrawingResourceFactory.CreateTextureBrush(IChartImage, RectangleF, IImageDrawOptions)` — the fully
  abstracted overload.
For Gauge, this means: design a Gauge-owned `IGaugeImage` (or check first whether it's worth sharing
Chart's `IChartImage` via the `Microsoft.Reporting.Rendering` neutral namespace — it wasn't moved
there originally because it was judged Chart-shaped/not-yet-verified-portable at the time; verify by
attempting the move rather than assuming either way, same discipline used for the other shared
interfaces), a Gauge-owned `IImageDrawOptions`-equivalent (same question — check whether Chart's
`IImageDrawOptions` is actually portable as-is before duplicating it), and add `WrapImage`/the fully
abstracted `CreateTextureBrush` overload to `IGaugeDrawingResourceFactory`/`GdiResourceFactory`.

**Recommendation, not a mandate:** do Option B. The whole point of this migration is a genuinely
backend-agnostic interface; Option A just relocates the same leak one call deeper and would need
revisiting later anyway (exactly what happened on the Chart side — it also initially considered the
equivalent of Option A and rejected it). But this is a real trade-off (more work now vs. a known,
already-precedented shortcut) — surface it to the user explicitly before committing to one, the same
way the original task doc flagged the interface-sharing decision and the DPI decision for explicit
sign-off rather than deciding alone.

## What "done" looks like for this increment

1. Whichever option is chosen, add `GetTextureBrushResource` (interface-typed sibling, original left
   untouched — same dual-overload pattern as `GetHatchBrushResource`/`GetGradientBrushResource`/`GetPieGradientBrushResource`).
2. With all four brush-getters now interface-typed, attempt converting the **pure-rename candidates**
   first (per the Chart playbook: value-types-only signatures convert with zero caller-side type
   changes) — `CreateBrush`, `GetCircularRangeBrush`, `GetLinearRangeBrush` look like good candidates;
   confirm each by reading its full body first, the same way every previous step in this migration has
   (some may have a `GraphicsPath`/`Region`/other concrete coupling not yet noticed — don't assume from
   the signature alone).
3. `DrawPathAbs` and `BackFrame.GetBrush` are likely **not** simple renames (they mix in the `pen`/`solidBrush`
   shared instance fields and/or a concrete-`GraphicsPath`-consuming `DrawPath`/`FillPath` call further
   in the same method) — read them in full before attempting; document precisely why each is or isn't
   convertible this pass, per this migration's established style (see how `DrawRadialSelection`'s path
   coupling was documented in the B2 entry already in `gauge-gdi-type-abstraction.md`).
4. Verify at every step: `dotnet build Microsoft.ReportViewer.DataVisualization/Microsoft.ReportViewer.DataVisualization.csproj`
   (0 errors), then `dotnet test --no-restore` from the repo root (expect 54/54 passing, 0 baseline
   diffs, unless you add new sample gauges/baselines to exercise newly-converted code — none of the
   current 2 samples in `tests/Microsoft.ReportViewer.DataVisualization.VisualRegressionTests/SampleGauges.cs`
   set a hatch style, gradient, or texture image, so none of this cluster is pixel-exercised today;
   consider whether a new sample gauge with a textured/gradient background is worth adding for real
   verification, the same way Chart added targeted samples whenever a conversion touched previously
   unexercised code).
5. Update **both** tracking docs when done: `tasks/gauge-gdi-type-abstraction.md` (Milestone B section)
   and `TODO.md` (the Gauge paragraph trail near the "Chart & Gauge" row) — follow the existing entries'
   style exactly: what was found, what was converted, what's still blocked and why, verification results.

## Constraints carried over from the whole migration (don't relitigate these)

- Public model properties that consumers can set directly (if any exist on Gauge's public surface for
  images/brushes — check before assuming there are none) stay concrete forever; only the rendering
  call itself moves to interface types.
- Never delete or rename the original concrete methods — dual-overload only, until every real caller
  has migrated.
- Read the full body of anything before converting it — this migration's entire history is a series of
  "looked self-contained, wasn't" discoveries caught by reading first, not by assuming from a method
  signature or a grep hit count.
- Don't invent test coverage or baselines you haven't visually inspected — if you add a new sample
  gauge, render it, look at the PNG yourself (via the `Read` tool) before promoting it to `Baselines/`.
