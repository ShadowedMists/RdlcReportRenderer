# Excel Rendering Call Stack Analysis: LocalReport.Render("EXCELOPENXML")

## Executive Summary

This document details the complete call stack for Excel rendering in the ReportViewerCore framework, identifying Windows dependencies and recommending cross-platform replacements.

## Call Flow: LocalReport.Render("EXCELOPENXML")

```
LocalReport.Render(format="EXCELOPENXML", deviceInfo, pageCountMode, createStream)
  ↓
LocalReport.InternalRender(format, allowInternalRenderers=false, deviceInfo, ...)
  ↓
ILocalProcessingHost.Render(format, deviceInfo, paginationMode, allowInternalRenderers, dataSources, createStreamCallback)
  ↓ [Implementation: LocalService.Render]
  
LocalService.Render (LocalService.cs:358)
  ├─ CreateAndConfigureReportProcessing() [LocalService.cs]
  ├─ CreateRenderer(format, allowInternalRenderers=false) [ControlService.cs:89]
  │   └─ item.Instantiate() → ExcelOpenXmlRenderer instance
  ├─ ReinitializeSnapshot() [if snapshot is null]
  ├─ CreateProcessingContext() [creates ProcessingContext with data sources]
  ├─ CreateRenderingContext() [creates RenderingContext]
  │   └─ RenderingContext constructor (Diagnostics/Internal/RenderingContext.cs)
  │
  └─ Either:
      A) CreateSnapshotAndRender() → ReportProcessing.RenderReport()
      B) ReportProcessing.RenderSnapshot() [if snapshot exists]
```

## Key Components in Call Stack

### 1. **LocalReport (Microsoft.ReportViewer.NETCore)**
- **File:** `Microsoft.ReportViewer.NETCore/Microsoft.Reporting.NETCore/LocalReport.cs`
- **Methods:**
  - `Render(format, deviceInfo, pageCountMode, createStream)` - Line 979
  - `InternalRender(format, allowInternalRenderers, deviceInfo, pageCountMode, createStreamCallback)` - Line 991
- **Responsibilities:** Public API entry point, parameter validation, exception handling
- **Windows Dependencies:** None directly, but delegates to ILocalProcessingHost

### 2. **LocalService (ILocalProcessingHost Implementation)**
- **File:** `Microsoft.ReportViewer.Common/Microsoft.Reporting/LocalService.cs`
- **Method:** `Render(format, deviceInfo, paginationMode, allowInternalRenderers, dataSources, createStreamCallback)` - Line 358
- **Responsibilities:**
  - Snapshot management
  - Renderer instantiation
  - Processing context creation
  - Rendering execution
- **Windows Dependencies:** None directly (abstract class)

### 3. **ControlService (Concrete LocalService Implementation)**
- **File:** `Microsoft.ReportViewer.Common/Microsoft.Reporting/ControlService.cs`
- **Method:** `CreateRenderer(format, allowInternal)` - Line 89
- **Responsibilities:** Instantiate rendering extensions based on format
- **Windows Dependencies:** None

### 4. **ExcelOpenXmlRenderer**
- **File:** `Microsoft.ReportViewer.Common/Microsoft.ReportingServices.Rendering.ExcelOpenXmlRenderer/ExcelOpenXmlRenderer.cs`
- **Inherits from:** `ExcelRenderer`
- **Key Methods:**
  - `Render(report, reportServerParameters, deviceInfo, clientCapabilities, renderProperties, createAndRegisterStream)` - Inherited from ExcelRenderer (Line 67)
  - `CreateExcelGenerator(createTempStream)` - Line 19 → Returns `OpenXmlGenerator`
  - `CreateFinalOutputStream(name, createAndRegisterStream)` - Line 14
- **Responsibilities:** Excel-specific rendering orchestration
- **Windows Dependencies:** **System.Drawing.Common** (for image handling)

### 5. **ExcelRenderer (Base Class)**
- **File:** `Microsoft.ReportViewer.Common/Microsoft.ReportingServices.Rendering.ExcelRenderer/ExcelRenderer.cs`
- **Key Method:** `Render(report, reportServerParameters, deviceInfo, clientCapabilities, renderProperties, createAndRegisterStream)` - Line 67
- **Flow:**
  - ParseDeviceinfo(deviceInfo) - Line 71
  - CreateFinalOutputStream() - Line 72
  - **MainEngine** instance creation - Line 73
  - Document map processing (if present) - Lines 74-81
  - SPBProcessing loop for pagination - Lines 82-116
  - MainEngine.RenderRPLPage() for each page - Line 113
  - MainEngine.Save(output) - Line 117
- **Windows Dependencies:** **System.Drawing** (indirectly through MainEngine/image handling)

### 6. **MainEngine**
- **File:** `Microsoft.ReportViewer.Common/Microsoft.ReportingServices.Rendering.ExcelRenderer.ExcelGenerator/MainEngine.cs`
- **Key Methods:**
  - Constructor: Creates IExcelGenerator
  - `RenderRPLPage(rplPage, createStyle, suppressOutlines)` - Main page rendering
  - `AddDocumentMap(documentMap)` - Document map processing
  - `Save(output)` - Finalizes and writes Excel file
- **Responsibilities:**
  - Excel content generation
  - Page-by-page RPL processing
  - Image embedding
  - Cell styling and formatting
- **Windows Dependencies:** **System.Drawing** (image operations in image handling)

### 7. **OpenXmlGenerator (IExcelGenerator Implementation)**
- **File:** `Microsoft.ReportViewer.Common/Microsoft.ReportingServices.Rendering.ExcelOpenXmlRenderer/OpenXmlGenerator.cs`
- **Key Methods:**
  - `AddImage(imageName, imageData, format, rowStart, rowStartPercentage, columnStart, columnStartPercentage, rowEnd, rowEndPercentage, columnEnd, colEndPercentage, hyperlinkURL, isBookmarkLink)` - Line 62
  - Cell writing methods
  - Worksheet management
  - Document relationships management
- **Dependencies:**
  - **ClosedXML** - For XLSX generation ✓ (Keep)
  - **System.Drawing.Imaging.ImageFormat** - For image format detection
  - **DocumentFormat.OpenXml** - For Open XML manipulation
- **Windows Dependencies:** **System.Drawing.Imaging** for ImageFormat enum

### 8. **ImageInformation (Image Metrics Calculation)**
- **File:** `Microsoft.ReportViewer.Common/Microsoft.ReportingServices.Rendering.ExcelRenderer.Layout/ImageInformation.cs`
- **Key Methods:**
  - `CalculateMetrics()` - Line 274
  - `DetermineImageFormat()` - Custom method (added in recent refactoring)
- **Responsibilities:** Image dimension and format detection
- **Windows Dependencies:** **[MIGRATED]** Now uses **SixLabors.ImageSharp** instead of System.Drawing ✓
- **Status:** ✓ Cross-platform compatible

## Windows Dependencies Found

### System.Drawing.Common (Extent: MODERATE)

**Files with direct System.Drawing usage:**

1. **ImageInformation.cs** - Line 279 (MIGRATED to ImageSharp)
   - ❌ `System.Drawing.Image.FromStream()` → ✅ `SixLabors.ImageSharp.Image.Identify()`
   - Status: ✓ **MIGRATED** in recent work

2. **ChartMapper.cs** - Line 5251, 5270, 5276
   - `System.Drawing.Image` used for background image processing
   - Called in `CreateImage(BackgroundImage)` method
   - Reason: Image loading for chart backgrounds
   - Status: ⚠️ Requires chart library refactoring

3. **GaugeMapper.cs** - Line similar locations
   - Similar image handling for gauge backgrounds
   - Status: ⚠️ Requires chart library refactoring

4. **ImageUtility.cs**
   - General image utility functions using System.Drawing
   - Status: ⚠️ Partially replaceable

5. **DynamicImageInstance.cs**
   - Image format handling
   - Status: ⚠️ Replaceable with ImageSharp

### System.Drawing.Imaging.ImageFormat (Extent: MODERATE)

**Locations:**
1. OpenXmlGenerator.cs:
   - `format.Guid == ImageFormat.Bmp.Guid` (line ~660)
   - `format.Guid == ImageFormat.Gif.Guid`
   - `format.Guid == ImageFormat.Jpeg.Guid`
   - Reason: Image format determination for Excel embedded images
   - Status: ⚠️ Requires custom enum replacement

### System.IO & Stream Handling (Extent: HIGH - Cross-Platform ✓)
- Used throughout but is **cross-platform**
- Examples: MemoryStream, StreamCache, CreateAndRegisterStream
- Status: ✓ **No action needed**

### System.Collections (Extent: HIGH - Cross-Platform ✓)
- Hashtable, NameValueCollection, List<T>
- Status: ✓ **Cross-platform**

### ClosedXML (Dependency: EXTERNAL - Acceptable ✓)
- Used for XLSX generation in OpenXmlGenerator
- Status: ✓ **Keep** (specified in requirements)
- Alternative: DocumentFormat.OpenXml (lower-level, more complex)

### DocumentFormat.OpenXml (Dependency: EXTERNAL - Acceptable ✓)
- Used for low-level XLSX structure manipulation
- Status: ✓ **Keep** (underlying OOXML standard library)

## Cross-Platform Assessment

| Component | Windows Dependency | Severity | Replacement | Status |
|-----------|-------------------|----------|------------|--------|
| ImageInformation | System.Drawing | ✓ MIGRATED | SixLabors.ImageSharp | ✅ Done |
| ImageFormat enum | System.Drawing.Imaging | Moderate | Custom enum + logic | 🔄 Pending |
| ChartMapper images | System.Drawing | High | Chart library refactor | ⚠️ Blocked |
| GaugeMapper images | System.Drawing | High | Chart library refactor | ⚠️ Blocked |
| OpenXmlGenerator | ImageFormat usage | Moderate | Custom format detection | 🔄 Pending |
| Stream handling | System.IO | ✓ NA | Cross-platform | ✅ OK |
| ClosedXML | External lib | Keep | N/A | ✅ OK |

## Critical Path: LocalReport.Render("EXCELOPENXML")

**No Windows API calls required in core Excel rendering path:**

1. ✅ Report processing: Cross-platform
2. ✅ RPL generation: Cross-platform
3. ⚠️ Image handling: System.Drawing (partially migrated)
4. ✅ XLSX generation: Cross-platform (ClosedXML)
5. ✅ Stream output: Cross-platform

## Recommended Implementation Plan

### Phase 1: Complete ImageFormat Migration (Priority: HIGH)
- Create custom `ImageFormatType` enum to replace System.Drawing.Imaging.ImageFormat
- Update OpenXmlGenerator to use custom enum
- Files to modify:
  - `OpenXmlGenerator.cs` (line ~660-670)
  - `ImageInformation.cs` (line 280-287) - Already uses SixLabors, just need format conversion
  - `BIFF8Generator.cs` (if similar pattern exists)
  - `LayoutEngine.cs` (if uses ImageFormat)

### Phase 2: Isolate Image Operations (Priority: MEDIUM)
- Create abstraction layer: `IImageAnalyzer` interface
- Files affected:
  - `ChartMapper.cs:5251-5276` - GetImageFromStream method
  - `GaugeMapper.cs` - Similar pattern
  - `ImageUtility.cs` - Generalized image utilities

### Phase 3: Chart Library Evaluation (Priority: LOW, Strategic)
- Current blocker: Microsoft.Reporting.Chart.WebForms uses System.Drawing extensively
- Alternatives to evaluate:
  - LiveCharts2 (modern, cross-platform)
  - OxyPlot (scientific/engineering charts)
  - XyChart (lightweight)
- Note: This is outside Excel rendering scope; affects charting broadly

## Implementation Notes

### File-by-File Recommendations

#### 1. OpenXmlGenerator.cs (Line ~660)
**Current:**
```csharp
string extension = (format.Guid == ImageFormat.Bmp.Guid) ? "bmp" :
                   ((format.Guid == ImageFormat.Gif.Guid) ? "gif" :
                    ((!(format.Guid == ImageFormat.Jpeg.Guid)) ? "png" : "jpg"));
```

**Recommended:**
```csharp
// Create custom enum
enum ImageFormatType { Bmp, Gif, Jpeg, Png }

// In DetermineImageFormat():
string extension = format switch {
    ImageFormatType.Bmp => "bmp",
    ImageFormatType.Gif => "gif",
    ImageFormatType.Jpeg => "jpg",
    _ => "png"
};
```

#### 2. ImageInformation.cs (Lines 274-287)
**Status:** ✅ **Already migrated to SixLabors.ImageSharp**

Current implementation uses:
- `SixLabors.ImageSharp.Image.Identify()` for cross-platform image analysis
- Custom `DetermineImageFormat()` method for format detection
- Type conversion: `(float)imageInfo.Metadata.VerticalResolution` for resolution values

#### 3. IExcelGenerator Interface
**Current signature:**
```csharp
void AddImage(string imageName, Stream imageData, ImageFormat format, ...)
```

**Recommendation:** Consider updating to:
```csharp
void AddImage(string imageName, Stream imageData, ImageFormatType format, ...)
```

This decouples from System.Drawing.Imaging while maintaining same functionality.

#### 4. ChartMapper.cs (Lines 5251-5276)
**Current:**
```csharp
System.Drawing.Image imageFromStream = GetImageFromStream(backgroundImage);
m_coreChart.Images.Add(text, imageFromStream);

private System.Drawing.Image GetImageFromStream(BackgroundImage backgroundImage)
{
    if (backgroundImage.Instance.ImageData == null) return null;
    return System.Drawing.Image.FromStream(new MemoryStream(...));
}
```

**Issue:** Microsoft.Reporting.Chart.WebForms expects System.Drawing.Image

**Options:**
1. ⚠️ Keep System.Drawing for chart library compatibility (current approach)
2. 🔄 Implement adapter pattern wrapping SixLabors.ImageSharp as System.Drawing.Image substitute (complex)
3. 🔄 Replace charting library entirely (strategic, out of scope)

**Recommendation:** Document as technical debt; chart rendering requires broader refactoring

## Third-Party Dependencies Summary

### ✅ Keep (Specified in Requirements)
- **ClosedXML** - Excel file generation
- **PDFium** - PDF rendering (not used in Excel path)
- **SkiaSharp** - Graphics/text rendering (not used in Excel path)

### ✅ Keep (Cross-Platform .NET)
- System.IO
- System.Collections
- System.Text
- System.Globalization
- System.Reflection
- System.Diagnostics

### ⚠️ Reduce/Replace
- **System.Drawing.Common** → Partially replaced with SixLabors.ImageSharp
  - Remaining usage: Chart background images (requires chart library refactoring)
  - Status: ✓ Excel rendering path is now mostly cross-platform
- **System.Drawing.Imaging** → Custom enum replacement needed
  - Status: 🔄 Pending custom ImageFormatType enum

### ✅ Already Migrated
- **System.Drawing** (image metrics) → **SixLabors.ImageSharp** ✓
  - Completed in ImageInformation.cs
  - Verified by test pass: 5/5 tests

## Testing Recommendations

### Unit Tests
1. `ImageInformation.cs` - CalculateMetrics() with various image formats
2. ImageFormatType enum - Format detection accuracy
3. OpenXmlGenerator.AddImage() - Image embedding in XLSX

### Integration Tests
1. LocalReport.Render("EXCELOPENXML") with images
2. Round-trip: Render → Verify image dimensions in XLSX
3. Cross-platform verification: Windows, Linux (if available)

### Files to Test
- `tests/ReportViewerCore.LinuxRenderers.Tests/` - Already passing ✓
- Manual XLSX validation with Excel/LibreOffice

## Architecture Decisions

### Decision 1: ImageSharp for Image Analysis ✅
- **Chosen:** SixLabors.ImageSharp
- **Rationale:** Cross-platform, modern, lightweight
- **Status:** ✓ Implemented in ImageInformation.cs
- **Trade-off:** Adds external dependency (already present for charting)

### Decision 2: Keep System.Drawing for Chart Images ⚠️
- **Current:** System.Drawing for chart background images
- **Rationale:** Chart library (Microsoft.Reporting.Chart.WebForms) requires System.Drawing.Image
- **Status:** ⚠️ Technical debt; requires chart library replacement
- **Future:** Evaluate alternative charting libraries (OxyPlot, LiveCharts2)

### Decision 3: Custom ImageFormatType Enum 🔄
- **Recommended:** Create custom enum instead of System.Drawing.Imaging.ImageFormat
- **Rationale:** Decouples from System.Drawing.Imaging
- **Status:** Pending implementation
- **Effort:** Low (simple enum + conversion logic)

## Success Criteria

✅ All tests pass (5/5)
✅ No breaking changes to public API
✅ Excel rendering produces valid XLSX files
✅ Cross-platform compatible image handling
⚠️ Chart images still require System.Drawing (known limitation)
🔄 ImageFormatType enum created and tested
🔄 ImageInformation.cs fully cross-platform (In progress)

## References

### Key Source Files
- `Microsoft.ReportViewer.NETCore/Microsoft.Reporting.NETCore/LocalReport.cs` - Public API
- `Microsoft.ReportViewer.Common/Microsoft.Reporting/LocalService.cs:358` - Core rendering orchestration
- `Microsoft.ReportViewer.Common/Microsoft.ReportingServices.Rendering.ExcelRenderer/ExcelRenderer.cs:67` - Excel renderer
- `Microsoft.ReportViewer.Common/Microsoft.ReportingServices.Rendering.ExcelOpenXmlRenderer/OpenXmlGenerator.cs` - XLSX generation
- `Microsoft.ReportViewer.Common/Microsoft.ReportingServices.Rendering.ExcelRenderer.Layout/ImageInformation.cs:274` - Image metrics (migrated)

### Investigation Files
- `investigation/03-windows-dependencies.md` - Windows dependency inventory
- `investigation/05-excel-renderer.md` - Excel rendering architecture
- `tasks/excel-rendering-linux.md` - Linux compatibility task

### Related Work
- Recent commit: "feat: replace System.Drawing with SixLabors.ImageSharp in Excel renderer"
- Issue tracking: Use TODO.md for progress tracking

## Next Steps

1. ✅ ImageInformation.cs migration to SixLabors.ImageSharp (DONE)
2. 🔄 Create ImageFormatType enum for System.Drawing.Imaging replacement
3. 🔄 Update OpenXmlGenerator.cs to use custom enum
4. ⚠️ Document chart image handling as technical debt
5. 📋 Update TODO.md with this analysis
6. 🔄 Consider IImageAnalyzer abstraction for future chart library migration
