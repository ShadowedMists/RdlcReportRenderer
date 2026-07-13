# Architecture map

## Overview

This note describes the current rendering flow that is relevant to the cross-platform work in ReportViewerCore. It is intentionally focused on the seam that was introduced for Excel, PDF, and resource handling rather than the entire legacy reporting pipeline.

## High-level flow

1. Report content is prepared by the existing reporting pipeline.
2. The rendering layer decides which output strategy to use for the requested export format.
3. A renderer implementation is selected through the renderer factory.
4. The selected renderer serializes the content into the requested output format.
5. The resulting stream is returned to the caller for further handling or file output.

## Current abstraction boundary

The new abstraction boundary is centered on:

- [Microsoft.ReportViewer.Common/Renderers/IExcelRenderer.cs](../Microsoft.ReportViewer.Common/Renderers/IExcelRenderer.cs)
- [Microsoft.ReportViewer.Common/Renderers/IPdfRenderer.cs](../Microsoft.ReportViewer.Common/Renderers/IPdfRenderer.cs)
- [Microsoft.ReportViewer.Common/Renderers/ReportRendererFactory.cs](../Microsoft.ReportViewer.Common/Renderers/ReportRendererFactory.cs)

These types keep platform selection and rendering responsibilities loosely connected. The intent is to avoid scattering platform checks throughout the rendering pipeline.

## Rendering responsibilities

- The factory owns the decision about which renderer implementation to instantiate.
- The Excel renderer owns the conversion of simple data payloads into an XLSX document.
- The PDF renderer owns the conversion of document content into a PDF stream.
- The resource adapter owns the normalization of embedded-resource data before the renderer or HTML path consumes it.

## Integration points

The initial integration point is the HTML rendering path in [Microsoft.ReportViewer.Common/Microsoft.ReportingServices.Rendering.HtmlRenderer/RenderingExtensionBase.cs](../Microsoft.ReportViewer.Common/Microsoft.ReportingServices.Rendering.HtmlRenderer/RenderingExtensionBase.cs). This is where embedded-resource handling now goes through the adapter instead of relying on the older direct path.

## Future direction

As more rendering seams are isolated, the same pattern can be applied to additional export types or resource-related features. The aim is to keep platform-specific behavior behind interfaces and make the rendering pipeline easier to extend.
