# TODO

## Current Work: .NET 10 Upgrade & System.Drawing Replacement

### Phase 1: Update to .NET 10

#### Step 1: Update Target Framework in All Projects
- [x] Update Microsoft.ReportViewer.Common.csproj to net10.0
- [x] Update Microsoft.ReportViewer.DataVisualization.csproj to net10.0
- [x] Update Microsoft.ReportViewer.NETCore.csproj to net10.0
- [x] Update Microsoft.ReportViewer.ProcessingObjectModel.csproj to net10.0
- [x] Update Microsoft.ReportViewer.WinForms.csproj to net10.0-windows7
- [x] Update ReportViewerCore.Sample.AspNetCore.csproj to net10.0
- [x] Update ReportViewerCore.Sample.Console.csproj to net10.0
- [x] Update ReportViewerCore.Sample.WinForms.csproj to net10.0-windows
- [x] Update ReportViewerCore.Sample.WinFormsServer.csproj to net10.0-windows
- [x] Update ReportViewerCore.LinuxRenderers.Tests.csproj to net10.0

#### Step 2: Update NuGet Package References
- [x] Updated all top-level packages to latest versions compatible with .NET 10
- [x] Updated Microsoft.CodeAnalysis.VisualBasic to 5.0.0 for net10.0 targets
- [x] Updated System.* packages to 10.0.0 versions where available
- [ ] Address System.Security.Cryptography.Xml vulnerabilities (GHSA-37gx-xxp4-5rgx, GHSA-w3x6-4m5h-cxqf)

#### Step 3: Build & Fix Compilation Errors
- [x] Run `dotnet build` - Build succeeded
- [x] Fixed compilation errors (removed net8.0/net9.0 from TargetFrameworks)
- [x] All tests passing (5/5 passed in ReportViewerCore.LinuxRenderers.Tests)

### Phase 2: Replace System.Drawing with SixLabors.ImageSharp

#### Step 1: Analyze System.Drawing Usage
- [ ] Identify all System.Drawing usages across projects
- [ ] Map each usage to ImageSharp equivalents
- [ ] Review the referenced PR #146 implementation for Excel rendering

#### Step 2: Implement Excel Rendering with ImageSharp
- [ ] Implement CalculateMetrics method in Microsoft.ReportViewer.Common/Microsoft.ReportingServices.Rendering.ExcelRenderer.Layout/ImageInformation.cs using SixLabors.ImageSharp
- [ ] Replace System.Drawing.Common dependency with SixLabors.ImageSharp
- [ ] Update all image dimension calculations to use ImageSharp APIs

#### Step 3: Replace Remaining System.Drawing Dependencies
- [ ] Update Microsoft.ReportViewer.DataVisualization to use ImageSharp
- [ ] Replace System.Drawing.Common in all other projects
- [ ] Update graphics/bitmap operations to use ImageSharp equivalents

#### Step 4: Build & Test
- [ ] Run `dotnet build` and resolve any remaining errors
- [ ] Run `dotnet test` to ensure all tests pass
- [ ] Verify Excel rendering output with EXCELOPENXML format

### Phase 3: Verification

- [ ] Verify all projects compile without errors
- [ ] Verify all tests pass
- [ ] Test rendering output for PDF, Excel, and other formats
- [ ] Verify no new compiler warnings introduced

## Completed Tasks
- Created documentation for all investigation goals (00-goals.md to 08-feasibility.md)
- Identified Windows-specific dependencies across the codebase
- Proposed Linux-compatible solutions for PDF/Excel rendering
- Designed abstraction layers for cross-platform rendering
- Estimated migration effort and timeline
- Implemented PDF rendering on Linux
- Implemented Excel rendering on Linux
- Updated unit tests for cross-platform compatibility
- Reduced Windows-specific analyzer warnings
- Replaced first System.Drawing-based resource-loading path with cross-platform adapter

## Documentation Tasks (Already Complete)
- [x] Add an architecture map for the reporting and rendering flow
- [x] Add a platform support matrix for Windows, Linux, and macOS
- [x] Add a build and test guide for local validation
- [x] Add an ADR-style decision log for the abstraction choices
- [x] Add an extension guide for introducing new renderers
- [x] Add a troubleshooting guide for common issues
- [x] Add concrete usage examples for the new abstractions

## Notes
- All investigation documents are complete and ready for review
- Migration implementation should follow the proposed abstraction layer design
- System.Drawing.Common is used in multiple projects and needs systematic replacement
- SixLabors.ImageSharp is already a dependency and should be the primary replacement
