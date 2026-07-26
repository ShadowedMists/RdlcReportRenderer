# Platform support

## Current status

The new rendering abstractions are intended to support a gradual migration away from Windows-only assumptions. At the moment the work is best thought of as a compatibility layer rather than a complete cross-platform rewrite.

## Supported areas

| Area | Windows | Linux | macOS | Notes |
| --- | --- | --- | --- | --- |
| Excel rendering abstraction | Yes | Yes | Planned | Linux path uses ClosedXML |
| PDF rendering abstraction (thin `LinuxPdfRenderer`/PdfSharpCore path) | Yes | Yes | Planned | A separate, simple string-to-PDF renderer (`Renderers/LinuxPdfRenderer.cs`) — not the real RDL PDF engine below |
| PDF rendering (real RDL engine: `PDFRenderer`→`Renderer`→`PDFWriter`) | Yes | No | No | See "PDF (RDL engine)" section below — blocked on RichText/Uniscribe text shaping, not on Metafile/GDI+ drawing |
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
| Chart rendering (2D) | Yes | Yes | `ChartRenderingBackendFactory` selects Skia by platform for the real `ChartMapper.GetImage`→`Chart.Save`→`ChartImage.SaveImage` path (`tasks/chart-gdi-type-abstraction.md` Milestone F, done 2026-07-26); every non-3D `SampleCharts.cs` scene renders correctly (Milestone E2) |
| Chart rendering (3D) | Yes | Yes | `Draw3DSurface`/`Draw3DPolygon` virtual-dispatch chain converted to `IGraphicsPath` (Milestone D3-real); reaches the same production path as 2D now that Milestone F wires backend selection. `Pie3D`/`Doughnut3D`'s label-drawing gap (`PieChart.Draw3DInsideLabels`) closed the same day (Milestone B1b) — every 3D scene renders correctly on both backends |
| Gauge rendering | Yes | No | Same migration shape as Chart, not started for Gauge's Skia backend; see `tasks/gauge-gdi-type-abstraction.md` (GDI+→interface abstraction itself is complete) |
| Map rendering | Yes | No | Deferred, LOW priority (after PDF Phase 1) — see `docs/decisions.md`; Bing Maps tile integration is end-of-lifed regardless, a Google Maps/OpenStreetMap adapter would be a prerequisite decision |
| Chart Skia backend | N/A | Wired into production | `ChartRenderingBackendFactory` (Milestone F) selects it by platform for both `ChartPicture.renderSurfaceFactory` and `chartGraph`; `IRenderSurface.DrawBackgroundBorder` removed the last hard `GdiRenderSurface` downcast from `ChartImage.SaveImage`. See `tasks/chart-gdi-type-abstraction.md` Milestone F. |

**Fundamental blocker (confirmed by a Phase 0 spike, 2026-07-18):** GDI+ cannot construct *any* `System.Drawing` object at all on Linux under .NET 10 — not even a bare `Font`/`Pen`/`Bitmap` — even with `libgdiplus` installed. This no longer blocks Chart rendering itself (Milestone F routes the real production path through Skia on non-Windows instead), but still applies to anything that hasn't been ported behind the Skia backend (Gauge, Map, `ChartImage.GetImage`'s `Bitmap` contract below).

**`ChartImage.GetImage(float) : Bitmap`'s declared return type is itself a hard, external, GDI+-typed public API contract, permanently unfixable** — even a complete Skia backend must still produce a `System.Drawing.Bitmap` to satisfy it, which GDI+'s Linux construction gap (above) makes impossible today. This is a distinct, permanent wall, separate from `Chart.Save`'s path: `ChartMapper.GetImage` calls `Chart.Save(Stream, ChartImageFormat)`, which never returns a `Bitmap` and already routes through `IRenderSurface.Encode`/`DrawBackgroundBorder` — the *reachable* production path has no `Bitmap`-contract wall at all, and Milestone F wired its platform selection. Don't conflate the two: `GetImage`'s wall is permanent; `Chart.Save`'s path is fully cross-platform now.

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
- **Gauge's `ScaleBase.DrawTickMark` wholesale `LinearGradientBrush.Transform = matrix` assignment** — `ILinearGradientBrush` deliberately has no generalized transform setter, but both real call sites always build `matrix` as a pure single rotation about one point, so it decomposes exactly (not an approximation) back to `SetRotationTransform(angle, center)` via `ScaleBase.DecomposeRotation`.
- **A "conclusion" from an earlier scoping pass is not evidence — always re-derive it from the current code before trusting it.** Gauge's `XamlRenderer.cs`/`XamlLayer.cs` were labeled "architecturally blocked" by one pass, then found on re-investigation to need only the same static-utility-threading pattern already solved elsewhere (`DigitalSegment.cs`) plus one new generalized brush-transform method — not a real blocker at all. Same category of mistake as Chart's D3/D3-real distinction above: a scoping label can outlive the reasoning that produced it, so re-check the reasoning, not just the label, before extending or accepting it.

## PDF (RDL engine): `PDFRenderer` → `Renderer` → `PDFWriter`

This is the real RDL PDF rendering path (`LocalReport.Render("PDF")`), distinct from the small standalone `LinuxPdfRenderer`/PdfSharpCore renderer in the row above. Full analysis: `tasks/pdf-render-callstack-analysis.md`; text-shaping scope: `tasks/pdf-text-shaping-abstraction.md`.

**Done (2026-07-26):** `PDFWriter`'s own dependencies are now cross-platform —
- Embedded-image decode (`Process32bppArgbImage`/`GetDefaultImage`) routes through `IImageProvider.DecodeToBgra32` (Excel's existing `ImageProviderFactory` pattern), not `System.Drawing.Bitmap`/`LockBits`.
- `PDFFont`'s font-style field is `PdfFontStyle`, a local `[Flags]` enum (`PdfFontStyle.cs`), not `System.Drawing.FontStyle`.
- `Renderer.ImageResources` (static, previously built at type-load time) and `GraphicsBase`'s `Bitmap`/`Graphics` pair (previously built in its constructor, called unconditionally by `WriterBase.BeginReport` — i.e. every PDF render) are both now lazily constructed on first real use. Loading the `Renderer`/`PDFWriter` types, and rendering a PDF with no text (lines/rectangles/images only), no longer requires GDI+ at all.

**Also done (2026-07-26): both simple (single-style) and rich (multi-run/multi-paragraph) PDF text boxes now render real text on non-Windows**, without needing any part of the RichText/Uniscribe pipeline below. `Renderer.ProcessSimpleTextBox`/`ProcessRichTextBox` branch on `!OperatingSystem.IsWindows()` to call new `WriterBase.DrawWrappedText`/`DrawWrappedRichText`/`PDFWriter` implementations that resolve the requested font family to one of PDF's standard 14 fonts (Helvetica/Times/Courier, via the existing `m_internalFonts` map), word-wrap using an approximate character-width table (across run boundaries for rich text), and emit `BT/Tf/Td/Tj/ET` content-stream operators directly — no `System.Drawing`, Win32, SkiaSharp, or HarfBuzzSharp dependency. Center/right alignment and underline/strikethrough are also supported on both paths (decorations drawn as filled rectangles appended after each `BT`/`ET` block, since PDF text objects can't contain path-painting operators). Verified end-to-end (temporarily forcing each branch on this Windows dev box, rendering real PDFs with correct word-wrap/position/font/alignment/decoration, including a bold-prefix + normal-suffix run pair, a separately-styled italic paragraph, center/right-aligned text, and a mixed-decoration rich-text paragraph). Known gaps: font family beyond the base-14 map falls back to Helvetica (deliberately deferred per user direction until PDF rendering is otherwise end-to-end); no true Unicode beyond WinAnsi; no overline/vertical text on this path. Full detail: `tasks/pdf-text-shaping-abstraction.md`'s "MVP" section, `docs/decisions.md`'s 2026-07-26 base-14 decision entry.

**Also done (2026-07-26): this path now has an automated regression test, not just manual verification.** `Microsoft.ReportViewer.Common`'s `AssemblyInfo.cs` grants `InternalsVisibleTo` to `Microsoft.ReportViewer.Chart.Rdl.Tests` (already signed with the same `ReportViewerCore.snk` key, so this is not a new trust boundary — see `docs/decisions.md`'s 2026-07-26 entry). `CrossPlatformPdfTextTests.cs` calls `PDFWriter.DrawWrappedText`/`DrawWrappedRichText` directly and asserts on the emitted content-stream operators, so this code now runs — and is checked — on every test run, on every platform, not only when a developer manually forces the OS-gated branch.

**Not done — the actual blocker for everything else (rich text, custom/embedded fonts, RTL/complex scripts, and the Image/WinForms-viewer/pagination engines that share this pipeline):** `Microsoft.ReportingServices.Rendering.RichText`'s text-shaping pipeline (`FontCache`/`CachedFont`/`TextRun`/`LineBreaker`/`TextBox`, backed by `Win32.cs`'s Uniscribe P/Invokes) constructs live `System.Drawing.Font` objects and calls Win32 `ScriptShape`/`ScriptPlace`/`ScriptBreak`/`ScriptItemize` **unconditionally for every text run, in every script** — there is no Latin/simple-script fast path in this codebase (verified by reading `TextRun.ShapeAndPlace`, `RichText/TextRun.cs:356-451`). This means:
- Any PDF containing text hits both the "GDI+ can't construct `Font` on Linux" wall (Phase 0 spike, same as Chart/Gauge) and the "no Uniscribe on Linux" wall, for every run, regardless of language.
- The same pipeline is shared by the Windows-only `ImageWriter` (EMF/TIFF) renderer, the WinForms on-screen viewer (`RichTextRenderer.cs`/`RenderingTextBox.cs`), and the shared `HPBProcessing`/`SPBProcessing` pagination engine that computes page breaks for *all* renderers — fixing it is not PDF-scoped work, and PDF cannot route around it with a renderer-specific shortcut.
- This is real, new-logic work (a HarfBuzzSharp-based shaper + a bidi/line-break implementation), not a resource-type port like the Chart/Gauge migrations — expect it to need its own visual-verification infrastructure (there is currently none for text shaping) before any increment can be trusted, per this repo's own "compiles and runs" ≠ "renders correctly" lesson from the Chart Skia migration.

## Guidance

When introducing a new renderer implementation, prefer:

- a small interface for the contract,
- a platform-specific implementation behind that contract,
- a factory or registration point for selection,
- and tests that verify the behavior rather than the visual output.
