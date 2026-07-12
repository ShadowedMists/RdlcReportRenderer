## PDF Rendering on Linux

### Problem Statement
Current PDF rendering relies on System.Drawing and Windows-specific APIs incompatible with Linux.

### Goals
1. Replace System.Drawing with cross-platform PDF library (PDFium/SkiaSharp)
2. Create abstraction layer for rendering pipeline
3. Ensure PDF output matches Windows implementation

### Technical Requirements
- Implement `IPdfRenderer` interface
- Create `LinuxPdfRenderer` class implementing `IPdfRenderer`
- Replace all calls to `System.Drawing` with PDFium/SkiaSharp equivalents
- Maintain identical output formatting as Windows version

### Acceptance Criteria
- Pass all existing unit tests
- Generate valid PDF files on Linux
- No dependency on Chromium/WebView2

### Implementation Notes
- Use PDFium via NuGet for native PDF rendering
- Implement document layout algorithms independently of rendering engine
- Maintain identical page size/positioning as Windows implementation

### Unit Tests
- `TestLinuxPdfRendering()` - Verify PDF output matches Windows version
- `TestPdfFormatConsistency()` - Validate page dimensions and content positioning
- `TestPdfGeneration()` - Ensure PDF files are properly formed and readable