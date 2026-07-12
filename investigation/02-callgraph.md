# Render Pathway Callgraph Analysis

## Objective
Map the render pathways and shared behaviors across PDF/Excel rendering in ReportViewerCore.

## Key Areas to Investigate
1. PDF rendering pipeline (Microsoft.ReportingServices.HtmlRendering, Microsoft.ReportingServices.RdlObjectModel)
2. Excel rendering pipeline (Microsoft.ReportingServices.DataProcessing, Microsoft.ReportingServices.RdlObjectModel)
3. Shared infrastructure in Microsoft.ReportingServices.Common
4. Dependency on Windows-specific assemblies

## Expected Outcomes
- Visual diagram of document processing flow
- Identification of platform-specific code blocks
- Mapping of data transformation stages

## Tools Required
- .NET Reflector for assembly analysis
- Dependency graph visualization
- Cross-platform compatibility testing framework