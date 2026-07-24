# Cross-platform rendering abstractions

## Overview

This document summarizes the internal implementation that introduces cross-platform rendering abstractions for the ReportViewerCore common rendering layer. The goal is to provide a thin compatibility layer that allows the library to generate Excel and PDF content without depending directly on Windows-only rendering behavior.

## Key classes

### ReportRendererFactory

Location: [Microsoft.ReportViewer.Common/Renderers/ReportRendererFactory.cs](../Microsoft.ReportViewer.Common/Renderers/ReportRendererFactory.cs)

The factory encapsulates renderer selection behind a small strategy-style abstraction. It exposes a single entry point for creating Excel and PDF renderer implementations based on the requested platform.

Design patterns used:
- Factory pattern for renderer selection
- Strategy pattern for platform-specific handling

Responsibilities:
- Select an implementation of [Microsoft.ReportViewer.Common/Renderers/IExcelRenderer.cs](../Microsoft.ReportViewer.Common/Renderers/IExcelRenderer.cs) for the requested platform
- Select an implementation of [Microsoft.ReportViewer.Common/Renderers/IPdfRenderer.cs](../Microsoft.ReportViewer.Common/Renderers/IPdfRenderer.cs) for the requested platform
- Keep platform selection logic centralized instead of scattering it through the call sites

### LinuxExcelRenderer

Location: [Microsoft.ReportViewer.Common/Renderers/LinuxExcelRenderer.cs](../Microsoft.ReportViewer.Common/Renderers/LinuxExcelRenderer.cs)

This class is the Linux-oriented implementation of the Excel abstraction. It uses ClosedXML to build workbook content directly from DataTable, DataSet, or string payloads and writes the result to the supplied stream.

Design patterns used:
- Adapter pattern to expose a cross-platform rendering interface over ClosedXML

Responsibilities:
- Convert simple data payloads into an XLSX document
- Support DataTable and DataSet input by creating a worksheet per table
- Provide a fallback path for scalar values by emitting a single-sheet workbook with the value written to the first cell

### LinuxPdfRenderer

Location: [Microsoft.ReportViewer.Common/Renderers/LinuxPdfRenderer.cs](../Microsoft.ReportViewer.Common/Renderers/LinuxPdfRenderer.cs)

This class provides a Linux-oriented implementation of the PDF abstraction. It uses PdfSharpCore to create a simple document and draw the incoming text content onto a page.

Design patterns used:
- Adapter pattern to expose a cross-platform rendering interface over PdfSharpCore

Responsibilities:
- Create a simple PDF document in memory
- Render a string representation of the supplied document object to the output stream
- Keep the implementation deterministic and lightweight for the initial cross-platform path

### ImageResourceAdapter

Location: [Microsoft.ReportViewer.Common/Renderers/ImageResourceAdapter.cs](../Microsoft.ReportViewer.Common/Renderers/ImageResourceAdapter.cs)

This class provides a compatibility adapter for embedded resources that may be returned as streams, strings, byte arrays, or other object-backed values. It is used by the HTML renderer path as a first concrete replacement for logic that previously relied on direct image handling behavior.

Design patterns used:
- Adapter pattern
- Strategy-like branching around resource payload types

Responsibilities:
- Normalize resource access through a single API
- Write embedded resource content to an output stream without coupling the caller to a specific resource representation
- Preserve compatibility with a variety of resource types that may appear in the existing codebase

## Interfaces

### IExcelRenderer

Location: [Microsoft.ReportViewer.Common/Renderers/IExcelRenderer.cs](../Microsoft.ReportViewer.Common/Renderers/IExcelRenderer.cs)

The Excel renderer contract defines the expected behavior for implementations that can serialize data to Excel output.

Expected input:
- An object representing the content to render, typically a DataTable, DataSet, or scalar value.

Expected output:
- The renderer writes an Excel document stream to the supplied output stream.

### IPdfRenderer

Location: [Microsoft.ReportViewer.Common/Renderers/IPdfRenderer.cs](../Microsoft.ReportViewer.Common/Renderers/IPdfRenderer.cs)

The PDF renderer contract defines the expected behavior for implementations that can serialize content to PDF output.

Expected input:
- An object representing the document content to render.

Expected output:
- The renderer writes a PDF document stream to the supplied output stream.

## Integration points

The initial integration point for the abstraction layer is the HTML rendering path. The method in [Microsoft.ReportViewer.Common/Microsoft.ReportingServices.Rendering.HtmlRenderer/RenderingExtensionBase.cs](../Microsoft.ReportViewer.Common/Microsoft.ReportingServices.Rendering.HtmlRenderer/RenderingExtensionBase.cs) now routes embedded-resource handling through the adapter rather than keeping the logic tightly coupled to the old image handling approach.

## Testing approach

The implementation is covered by the Linux renderer test project in [tests/ReportViewerCore.LinuxRenderers.Tests/LinuxRenderersTests.cs](../tests/ReportViewerCore.LinuxRenderers.Tests/LinuxRenderersTests.cs). The tests validate the following behaviors:
- Excel output generation from a DataTable
- PDF output generation from a simple string input
- Factory selection for Linux renderers
- Resource adaptation for embedded-resource payloads

## Notes for future work

- The current implementation is intentionally small and focused on the first cross-platform seam.
- The abstraction layer should be extended as more Windows-specific rendering paths are isolated.
- Future work should consider adding richer document models and more platform-specific implementations behind the same interfaces.

---

## Excel image handling: `IImageProvider`

Location: `Microsoft.ReportViewer.Common/.../IImageProvider.cs`, `WindowsImageProvider.cs`, `CrossPlatformImageProvider.cs`, `ImageProviderFactory.cs`

Isolates the `System.Drawing`-dependent parts of Excel image handling (used by `ChartMapper.cs` and `GaugeMapper.cs` as well as plain embedded images) behind a small port:

- `IImageProvider.LoadImage(Stream) : ImageMetadata` and `GetImageForChart(Stream) : object`
- `ImageMetadata`: `Width`, `Height`, `HorizontalResolution`, `VerticalResolution`, `Format`
- `WindowsImageProvider` (`System.Drawing`-based, `[SupportedOSPlatform("windows")]`) vs. `CrossPlatformImageProvider` (SixLabors.ImageSharp-based; `GetImageForChart` returns `null` since chart/gauge rendering itself is still Windows-only)
- `ImageProviderFactory.CreateProvider()` selects an implementation via `RuntimeInformation.IsOSPlatform`

Alongside it, `Microsoft.ReportingServices.Rendering.ExcelRenderer.Excel.ImageFormatType` (`Bmp`/`Gif`/`Jpeg`/`Png`/`Unknown`) plus `ImageFormatTypeHelper` (`ToFileExtension`, `ToMimeType`, `FromMimeType`, `DetectFromStream` via `SixLabors.ImageSharp.Image.Identify`) replaced `System.Drawing.Imaging.ImageFormat` in `IExcelGenerator`, `ImageInformation`, `OpenXmlGenerator`, and `BIFF8Generator`. Both changes are internal-only (no public API break).

**Known limitation:** chart/gauge background images still cannot render on non-Windows — that depends on the much larger Chart/Gauge engine migration below.

---

## Chart and Gauge rendering: Ports & Adapters over GDI+

The Chart engine (`Microsoft.Reporting.Chart.WebForms`) and Gauge engine (`Microsoft.Reporting.Gauge.WebForms`) are vendored, first-party rendering engines (not external libraries) that historically drew directly with GDI+ (`System.Drawing`). Both are being migrated to a Ports & Adapters design so a non-GDI+ backend (SkiaSharp) can eventually be plugged in for Linux/macOS. Full progress/open items: `tasks/chart-gdi-type-abstraction.md` and `tasks/gauge-gdi-type-abstraction.md`. See also `docs/platform-support.md` for cross-platform gaps and `docs/decisions.md` for why this design was chosen.

### Shape

```
Painters (73 Chart files / ~30 Gauge files) ──► ChartGraphics / GaugeGraphics (chokepoint) ──► IChartRenderingEngine / IGaugeRenderingEngine
        │                                              │                                                    │
        └── construct resources via ──────── IDrawingResourceFactory / IGaugeDrawingResourceFactory ────────┘   (the PORT)
                                                   ▲                    ▲
                                            GdiResourceFactory   SkiaResourceFactory (Chart: real 2D scenes, not yet production-wired)          (ADAPTERS)
```

Instead of `new Pen(color, width)`, code calls `factory.CreatePen(color, width)` and receives an `IPen`.

### Namespaces

- **`Microsoft.Reporting.Rendering`** (neutral, shared by both engines) — pure engine-agnostic resource contracts: `IRenderingResource`, `IPen`, `IBrush` (+ `ISolidBrush`/`ILinearGradientBrush`/`ITextureBrush`/`IHatchBrush`/`IPathGradientBrush`), `IChartFont`, `ITextFormat`, `IGraphicsPath`, `IChartImage`, `IImageDrawOptions`.
- **`Microsoft.Reporting.Chart.WebForms.Rendering`** — Chart-specific pieces that couldn't move to the neutral namespace: `IClipRegion` (its `GetBounds`/`IsEmpty`/`IsInfinite` need an `IChartRenderingEngine` to reach a live `Graphics` — a genuine Chart-specific dependency, found by attempting the move), `IDrawingResourceFactory`, `IChartRenderingEngine`, `IRenderSurface`/`IRenderSurfaceFactory`, and the concrete `Gdi`/`Skia` adapter implementations.
- **Gauge-owned** (`Microsoft.Reporting.Gauge.WebForms/Rendering/`) — `IGaugeDrawingResourceFactory`, `IGaugeRenderingEngine`, `IGaugeClipRegion` (Gauge's own clip-region interface — deliberately not shared with Chart's `IClipRegion` for the same per-engine-`Graphics` reason), and Gauge's own `Rendering/Gdi/` adapters. Gauge's adapters are separate implementations from Chart's identically-shaped ones by design, not shared instances.
- Portable value types (`Color`, `PointF`/`Point`, `RectangleF`/`Rectangle`, `SizeF`/`Size` — the bulk of `System.Drawing` usage by occurrence count) and GDI+-namespaced enums (`SmoothingMode`, `LineCap`, `DashStyle`, `FillMode`, etc.) are kept concrete — `System.Drawing.Primitives` is fully cross-platform, so abstracting them would add churn for no portability gain. `Matrix` is represented as `System.Numerics.Matrix3x2` rather than a custom interface.

### Recurring conversion patterns

- **Dual-overload strategy:** rather than retype an existing method/field in place, add a new, separately-named interface-typed sibling (e.g. `GetHatchBrushResource` alongside `GetHatchBrush`, `DrawPathAbs(IGraphicsPath, ...)` alongside `DrawPathAbs(GraphicsPath, ...)`) that coexists with the concrete original, then migrate real callers one at a time. This is what makes the migration incremental and revert-safe — new callers get the interface-typed surface immediately; old callers keep working until they're converted.
- **Bridge-at-the-sink:** when a concrete resource (a `Font`, a `GraphicsPath` built by a self-contained geometry helper, an already-loaded `Image`) can't reasonably be retyped at its source, wrap/reconstruct it into the interface type only at the point it's actually consumed (`WrapFont(Font)`, `WrapPath(GraphicsPath)`/`UnwrapPath(IGraphicsPath)`, `WrapImage(Image)`).
- **Public model properties stay concrete forever:** `Series.Font`, `DataPoint.Font`, `PolylineAnnotation.Path`, `Annotation.TextFont`, etc. are consumer-facing API surface, not internal rendering plumbing — only the internal *rendering call* converts to the interface type.
- **Position/layout-only helpers stay concrete:** methods that only compute layout (no adjacent draw call in the same method) are deliberately left on concrete types (e.g. `Legend.GetTitleSize`, `SmartLabels.cs`'s position math) — a consistent, intentional scope boundary.
- **The "large atomic pass" trap:** shared concrete-field arrays on helper/attrib classes (e.g. Gauge's `KnobStyleAttrib`/`NeedleStyleAttrib`/`MarkerStyleAttrib`/`BarStyleAttrib`) look individually convertible per-getter but are all consumed together by the same `FillPath(Brush, GraphicsPath)`/`DrawPath(Pen, GraphicsPath)` call — converting one getter without converting the whole class plus its producers and consumer in one pass just adds unreachable dead code. These are identified and deliberately deferred until a dedicated single pass rather than sliced.

### Verification convention

Every increment is checked with: `dotnet build` (0 errors) + the full test suite (`VisualRegressionTests` + `Chart.Rdl.Tests`) passing + zero baseline PNG diffs. Two techniques back this:
1. For previously-uncovered render paths, generate a "before" baseline by `git stash push --keep-index` on just the engine files being converted (keeping new test files), render through the pre-conversion code, pop the stash, and confirm byte-for-byte match against the post-conversion render.
2. For pure hit-testing/metadata changes with no visible pixels, add dedicated `Chart.HitTest(x, y)`-based tests instead of relying on PNG diffs.

Note: purely additive/unreachable interface surface is only "build-verified," not "pixel-verified," until some real caller or dedicated sample exercises it — this distinction is called out explicitly in both task docs' progress notes.
