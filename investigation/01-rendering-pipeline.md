# Rendering Pipeline Analysis

## Objective
Identify code pathways for rendering Excel and PDF documents in ReportViewerCore.

## Key Areas to Investigate
1. PDF rendering pipeline (focus on `Microsoft.ReportingServices.HtmlRendering` and `Microsoft.ReportingServices.RdlObjectModel`)
2. Excel rendering pipeline (focus on `Microsoft.ReportingServices.DataProcessing` and `Microsoft.ReportingServices.RdlObjectModel`)
3. Shared rendering infrastructure in `Microsoft.ReportingServices.Common`

## Expected Outcomes
- Diagram of document processing flow
- Identification of platform-specific code blocks
- Mapping of data transformation stages

## Tools Required
- Code navigation (VS Code + .NET Reflector)
- Dependency graph visualization
- Cross-platform compatibility analysis