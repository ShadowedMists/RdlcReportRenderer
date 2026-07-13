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
