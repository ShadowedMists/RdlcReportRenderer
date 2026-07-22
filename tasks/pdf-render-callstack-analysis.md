# PDF Rendering Cross-Platform Analysis

**Status (2026-07-22):** Not started — this remains the active roadmap/reference for future PDF work (see `TODO.md`'s "PDF Phase 1" entry). Excel Phases 4-5 referenced below as "pending" are now complete (`docs/rendering-abstractions.md`).

## Executive Summary

The PDF rendering pipeline in ReportViewerCore has **significant Windows-specific dependencies** through System.Drawing and GDI+ operations. Unlike Excel rendering (which uses ClosedXML for cross-platform XLSX generation), PDF rendering is fundamentally tied to GDI+ graphics operations that don't have direct cross-platform equivalents.

**Key Finding:** PDF rendering requires **substantial architectural refactoring** to achieve cross-platform support, including replacement of Metafile-based rendering with alternative graphics libraries.

---

## Call Stack Analysis

### Entry Point and Flow

```
LocalReport.Render("PDF")
  ↓
LocalService.Render() [LocalService.cs:358]
  ↓
ControlService.CreateRenderer("PDF") [ControlService.cs:89-107]
  ├─ ListRenderingExtensions() creates PDFRenderer instance [ControlService.cs:53]
  └─ Returns PDFRenderer for "PDF" format [ControlService.cs:54]
     ↓
     PDFRenderer.Render() [PDFRenderer.cs:30]
       ├─ CreatePaginationSettings() - Configure DPI and text measurement
       │   └─ PaginationSettings with DPI/font settings [PDFRenderer.cs:32-43]
       │
       ├─ HPBProcessing() - Page layout and pagination [PDFRenderer.cs:45]
       │   └─ GetNextPage(out RPLReport) - Iterate pages [PDFRenderer.cs:89]
       │
       ├─ Renderer (page rendering) [PDFRenderer.cs:48]
       │   └─ ProcessPage(RPLReport, pageNum, ...) [PDFRenderer.cs:94]
       │       ├─ Creates Graphics object (GDI+ based) [PDFRenderer.cs:48]
       │       ├─ System.Drawing.Bitmap for rendering [Renderer.cs:42-54]
       │       └─ Draws report items to bitmap
       │
       └─ PDFWriter (PDF generation) [PDFRenderer.cs:51]
           ├─ BeginReport() - Initialize PDF structure [PDFRenderer.cs:86]
           ├─ ProcessPage() - Render each page to PDF [PDFRenderer.cs:94]
           │   ├─ WritePageContent() - Write page graphics
           │   ├─ WritePageResources() - Font/image resources
           │   └─ WritePageStructure() - PDF page objects
           └─ EndReport() - Finalize PDF [PDFRenderer.cs:99]
```

**Call Depth:** 6+ major components  
**Windows Dependencies:** 7 major (Metafile, System.Drawing graphics, GDI+, fonts, etc.)

---

## Windows Dependency Inventory

### HIGH SEVERITY - Core Blocker

#### 1. **Metafile Graphics (EMF/EMF+)**
- **Location:** `MetafileGraphics.cs` (Lines 13-200+)
- **Severity:** HIGH (architectural blocker)
- **Impact:** Complete replacement required
- **Usage Pattern:**
  - Line 26: `private Metafile m_metafile;` - GDI+ Metafile storage
  - Line 86-99: `CreateMetafileGraphics()` - Creates GDI+ Metafile objects
  - Line 98: `new Metafile(stream, hdc, rect, MetafileFrameUnit.GdiCompatible, emfType)` - Direct Metafile creation
  - Line 178-182: `CalculateMetafileRectangle()` - Win32 device cap queries

**Why Metafile is Problematic:**
- Metafile is a GDI+ format that records drawing commands as a binary stream
- Used to preserve vector graphics in PDF rendering
- No cross-platform equivalent (Linux/Mac don't have GDI+)
- Direct Win32 HDC (device context) dependency

**Cross-Platform Alternative:**
- Use vector graphics library like SkiaSharp instead of Metafile
- SkiaSharp can record drawing operations similarly
- Would require refactoring Renderer to use SkiaSharp graphics instead of System.Drawing.Graphics

#### 2. **System.Drawing Graphics Objects**
- **Location:** `Graphics.cs`, `GraphicsBase.cs` (Lines 1-200+)
- **Severity:** HIGH (core rendering)
- **Impact:** Complete replacement required
- **Usage Pattern:**
  - Line 13: `internal class Graphics : GraphicsBase` - Inherits from custom graphics
  - Line 5-10: Uses `System.Drawing`, `System.Drawing.Drawing2D`, `System.Drawing.Text`
  - Line 67: `m_graphicsBase = System.Drawing.Graphics.FromImage(m_imageBase)` - GDI+ graphics context
  - Line 65: `System.Drawing.Image.FromHbitmap(m_hBitmap.Handle)` - Windows HBITMAP conversion

**Why System.Drawing Graphics is Problematic:**
- System.Drawing.Graphics is a managed wrapper around GDI+
- Used for all drawing operations (lines, shapes, text, images)
- Requires Windows GDI+ subsystem
- Cannot be replaced with simple image libraries (needs vector drawing)

**Cross-Platform Alternative:**
- SkiaSharp (Skia is the graphics engine behind Chromium/Android)
- Uses `SKCanvas` for drawing operations
- Platform-agnostic graphics API

#### 3. **GDI+ Bitmap Rendering**
- **Location:** `Graphics.cs` (Lines 20-65), `GraphicsBase.cs` (Line 65)
- **Severity:** HIGH (rendering target)
- **Impact:** Requires SkiaSharp migration
- **Usage Pattern:**
  - Line 20: `protected Win32ObjectSafeHandle m_hBitmap;` - Windows bitmap handle
  - Line 22: `protected Win32DCSafeHandle m_hdcBitmap;` - Windows device context
  - Line 65: `Bitmap bitmap = System.Drawing.Image.FromHbitmap(m_hBitmap.Handle);` - HBITMAP to managed image
  - Line 69-79: `bitmap.Save(outputStream, ImageFormat.Bmp/Gif/Jpeg/Png);`

**Why GDI+ Bitmaps are Problematic:**
- HBITMAP is Windows-only handle
- Device context (HDC) is Windows API
- SafeHandle wrappers around Win32 objects

**Cross-Platform Alternative:**
- SkiaSharp surfaces instead of HBITMAP
- `SKBitmap` or `SKSurface` for drawing target

---

### MEDIUM SEVERITY - Significant Dependencies

#### 4. **Font Handling with System.Drawing**
- **Location:** `PDFFont.cs` (Lines 1-100+)
- **Severity:** MEDIUM (complex but isolated)
- **Impact:** Font abstraction layer needed
- **Usage Pattern:**
  - Line 4: `using System.Drawing;` - GDI+ font styles
  - Line 58: `internal readonly FontStyle GDIFontStyle;` - GDI+ FontStyle enum
  - Line 76: PDFFont constructor uses `FontStyle` parameter

**Why Font Handling is Complex:**
- FontStyle enum (Bold, Italic, Underline) comes from System.Drawing
- Font enumeration uses GDI+ functions (indirectly through cached fonts)
- Embedded font handling is abstracted in `EmbeddedFont` class

**Cross-Platform Alternative:**
- Custom enum to replace System.Drawing.FontStyle
- Use system font APIs (FontKit on Mac, FontConfig on Linux, Win32 on Windows)
- Abstraction layer: `IFontProvider` interface

#### 5. **Win32 Device Capability Queries**
- **Location:** `MetafileGraphics.cs` (Lines 178-182)
- **Severity:** MEDIUM (platform-specific calculations)
- **Impact:** Fallback values needed
- **Usage Pattern:**
  ```csharp
  int deviceCaps = Microsoft.ReportingServices.Rendering.RichText.Win32.GetDeviceCaps(hdc, 4);
  int deviceCaps2 = Microsoft.ReportingServices.Rendering.RichText.Win32.GetDeviceCaps(hdc, 6);
  int deviceCaps3 = Microsoft.ReportingServices.Rendering.RichText.Win32.GetDeviceCaps(hdc, 118);
  int deviceCaps4 = Microsoft.ReportingServices.Rendering.RichText.Win32.GetDeviceCaps(hdc, 117);
  ```

**Why Win32 Calls are Needed:**
- Get display DPI, color depth, resolution
- Metafile aspect ratio adjustments
- Windows-specific display capabilities

**Cross-Platform Alternative:**
- Use DPI values from PaginationSettings (already available)
- Hardcode reasonable defaults for non-Windows platforms
- Use platform-detection to skip on non-Windows

#### 6. **ImageCodecInfo and Image Encoding**
- **Location:** `Graphics.cs` (Lines 24, 59-90)
- **Severity:** MEDIUM (image output)
- **Impact:** SkiaSharp encoding needed
- **Usage Pattern:**
  - Line 24: `private static ImageCodecInfo[] m_encoders = GetGdiImageEncoders();`
  - Line 69-79: `bitmap.Save(outputStream, ImageFormat.Bmp/Gif/Jpeg/Png);`

**Why ImageCodecInfo is Needed:**
- System.Drawing uses Windows codec infrastructure
- GDI+ handles JPEG quality, PNG optimization, etc.
- Complex encoding options

**Cross-Platform Alternative:**
- SkiaSharp `SKImage.Encode()` with format parameter
- `SKEncodedImageFormat` enum for format specification
- Encoding options through `SKWebpEncoderOptions`, etc.

---

### LOW SEVERITY - Isolated or Manageable

#### 7. **Win32 Safe Handle Wrappers**
- **Location:** `Graphics.cs` (Lines 20-22), `GraphicsBase.cs` (Line 21)
- **Severity:** LOW (wrappers, easily replaced)
- **Impact:** Remove when GDI+ is replaced
- **Usage Pattern:**
  ```csharp
  protected Win32ObjectSafeHandle m_hBitmap;
  protected Win32DCSafeHandle m_hdcBitmap;
  private Win32DCSafeHandle m_hdc = Win32DCSafeHandle.Zero;
  ```

**Why Win32 Handles are Needed:**
- Store native Windows handles safely
- Ensure proper cleanup with GC finalizers

**Cross-Platform Alternative:**
- Remove when migrating to SkiaSharp (which doesn't use Win32 handles)

#### 8. **Image Format Enums**
- **Location:** `PDFWriter.cs` (Line 11)
- **Severity:** LOW (already addressed in Excel work)
- **Impact:** Reuse ImageFormatType enum
- **Usage Pattern:**
  - Line 1526: `using (Bitmap bitmap = new Bitmap(System.Drawing.Image.FromStream(...)))`

**Cross-Platform Alternative:**
- Reuse `ImageFormatType` enum from Excel rendering (Phase 4)
- Not blocking for PDF since image output is secondary to PDF generation

---

## Cross-Platform Assessment Matrix

| Component | Complexity | Impact | Replacement | Timeline |
|-----------|-----------|--------|-------------|----------|
| Metafile graphics | **VERY HIGH** | Core rendering blocked | SkiaSharp graphics recording | 2-3 weeks |
| System.Drawing.Graphics | **VERY HIGH** | All drawing operations | SkiaSharp SKCanvas | 2-3 weeks |
| GDI+ Bitmap rendering | **HIGH** | Rendering target | SkiaSharp surfaces | 1 week |
| Font handling | **MEDIUM** | Font enumeration/embedding | Font abstraction layer | 3-5 days |
| Win32 device queries | **LOW** | DPI/capability detection | Fallback values + detection | 1-2 days |
| Image encoding | **MEDIUM** | Output image formats | SkiaSharp encoding | 2-3 days |
| Win32 handles | **LOW** | Memory management | Remove with GDI+ migration | Automatic |

---

## Implementation Roadmap

### Phase 1: SkiaSharp Graphics Foundation (2-3 weeks)
**Priority:** CRITICAL  
**Risk:** HIGH (architectural change)

**Objective:** Replace GDI+ with SkiaSharp for all drawing operations

**Files to Create:**
- `Microsoft.ReportingServices.Rendering.ImageRenderer/SkiaGraphicsAdapter.cs` - Wrapper for SkiaSharp
- `Microsoft.ReportingServices.Rendering.ImageRenderer/SkiaMetafileRecorder.cs` - Record drawing commands

**Files to Modify:**
- `GraphicsBase.cs` - Use SKSurface instead of System.Drawing.Graphics
- `Graphics.cs` - Use SkiaSharp for bitmap operations
- `MetafileGraphics.cs` - Use SkiaSharp instead of Metafile
- `Renderer.cs` - Pass SkiaSharp graphics to drawing operations

**Implementation Notes:**
```csharp
// Current (GDI+)
protected System.Drawing.Graphics m_graphicsBase;
protected Bitmap m_imageBase;

// New (SkiaSharp)
protected SKCanvas m_skiaCanvas;
protected SKSurface m_skiaSurface;
```

**Success Criteria:**
- ✅ PDF rendering works on Windows with SkiaSharp
- ✅ Output PDF is visually identical to System.Drawing version
- ✅ All drawing operations (lines, text, shapes, images) work
- ✅ All tests pass

**Blocking Issues:** None - can be developed in parallel with Excel work

---

### Phase 2: Font Handling Abstraction (3-5 days)
**Priority:** MEDIUM  
**Risk:** MEDIUM (isolated change)

**Objective:** Decouple font handling from System.Drawing

**Files to Create:**
- `Microsoft.ReportingServices.Rendering.ImageRenderer/IFontProvider.cs` - Font abstraction
- `Microsoft.ReportingServices.Rendering.ImageRenderer/FontStyleType.cs` - Custom font style enum
- `Microsoft.ReportingServices.Rendering.ImageRenderer/SkiaFontProvider.cs` - SkiaSharp implementation

**Files to Modify:**
- `PDFFont.cs` - Use FontStyleType instead of System.Drawing.FontStyle
- `PDFWriter.cs` - Inject IFontProvider
- `Renderer.cs` - Use IFontProvider for font operations

**Implementation Notes:**
```csharp
// Replace System.Drawing.FontStyle
public enum FontStyleType
{
    Regular = 0,
    Bold = 1,
    Italic = 2,
    Underline = 4
}

// Font provider interface
public interface IFontProvider
{
    IFontMetrics GetFontMetrics(string familyName, FontStyleType style, int emHeight);
    bool TryGetSystemFont(string familyName, out FontData fontData);
}
```

**Success Criteria:**
- ✅ All fonts render correctly in PDF
- ✅ Bold/italic styles work
- ✅ Font embedding works
- ✅ No System.Drawing.FontStyle references

---

### Phase 3: Win32 Platform Abstraction (1-2 days)
**Priority:** LOW  
**Risk:** LOW (isolated)

**Objective:** Isolate platform-specific queries

**Files to Create:**
- `Microsoft.ReportingServices.Rendering.ImageRenderer/IPlatformCapabilities.cs` - Platform API abstraction
- `Microsoft.ReportingServices.Rendering.ImageRenderer/PlatformCapabilitiesFactory.cs` - Factory

**Implementation Notes:**
```csharp
public interface IPlatformCapabilities
{
    int GetDisplayDpiX();
    int GetDisplayDpiY();
    int GetColorDepth();
}

// Windows implementation
[SupportedOSPlatform("windows")]
internal class WindowsPlatformCapabilities : IPlatformCapabilities
{
    // Uses Win32 GetDeviceCaps
}

// Cross-platform implementation
internal class CrossPlatformCapabilities : IPlatformCapabilities
{
    // Returns hardcoded defaults (96 DPI, 32-bit color)
}
```

**Success Criteria:**
- ✅ Metafile rendering works on Windows
- ✅ Non-Windows platforms use fallback values
- ✅ No direct Win32 calls in graphics code

---

### Phase 4: Image Codec Migration (2-3 days)
**Priority:** MEDIUM  
**Risk:** MEDIUM (image output)

**Objective:** Replace System.Drawing image encoding with SkiaSharp

**Files to Modify:**
- `Graphics.cs` - Replace ImageFormat with SkiaSharp encoding
- `PDFWriter.cs` - Image embedding code

**Implementation Notes:**
```csharp
// Current (GDI+)
bitmap.Save(outputStream, ImageFormat.Jpeg);

// New (SkiaSharp)
using (var image = SKImage.FromBitmap(bitmap))
{
    image.Encode(SKEncodedImageFormat.Jpeg, quality: 90).SaveTo(outputStream);
}
```

**Success Criteria:**
- ✅ All image formats (BMP, GIF, JPEG, PNG) encode correctly
- ✅ JPEG quality preserved
- ✅ PNG transparency preserved

---

### Phase 5: Windows Device Context Migration (1-2 days)
**Priority:** LOW  
**Risk:** LOW

**Objective:** Remove Win32 device context dependencies

**Files to Modify:**
- `MetafileGraphics.cs` - Remove HDC-based code
- `Graphics.cs` - Remove HBITMAP handling
- `GraphicsBase.cs` - Remove Win32 handles

**Success Criteria:**
- ✅ No Win32 device context code
- ✅ All graphics isolated in SkiaSharp

---

## Strategic Considerations

### Why PDF Rendering is Harder than Excel

**Excel Rendering:**
- Uses ClosedXML library (pure C#, cross-platform)
- Embeds images as binary blobs
- Minimal graphics operations needed

**PDF Rendering:**
- Renders entire page as graphics
- All text, shapes, lines, images drawn to graphics context
- Requires full graphics library equivalent
- Font embedding must preserve vector data

### Chart Rendering Impact

Chart/Gauge rendering affects both Excel and PDF:
- **Excel:** Charts rendered to images → embedded in XLSX
- **PDF:** Charts rendered to graphics → drawn directly on page

Replacing charts affects BOTH renderers - see `chart-image-abstraction-analysis.md`

---

## Success Criteria

### Phase 1 (SkiaSharp) Success Metrics
- ✅ All existing tests pass
- ✅ PDF output renders on Windows with SkiaSharp
- ✅ Visual comparison: SkiaSharp output ≈ GDI+ output
- ✅ No performance regression
- ✅ All DPI settings work correctly

### Phase 2-5 Success Metrics
- ✅ Fonts render consistently across platforms
- ✅ Platform abstraction works without conditional compilation
- ✅ Non-Windows platforms use fallbacks gracefully
- ✅ Image encoding works for all formats

### Final State
- ✅ PDF rendering works on Windows (primary target)
- ✅ Groundwork laid for Linux/Mac support (Phase 6+)
- ✅ All graphics operations isolated from System.Drawing
- ✅ Clear path for future chart library replacement

---

## Known Limitations & Technical Debt

| Item | Severity | Status | Notes |
|------|----------|--------|-------|
| Metafile to PDF conversion | HIGH | PENDING | Requires SkiaSharp or alternative |
| GDI+ fonts | MEDIUM | PENDING | Requires font abstraction |
| Win32 device queries | LOW | PENDING | Can use fallbacks |
| Chart rendering in PDF | HIGH | EXTERNAL | Requires chart lib replacement |
| Gauge rendering in PDF | HIGH | EXTERNAL | Requires chart lib replacement |

---

## References

### Source Files
- `Microsoft.ReportViewer.Common/Microsoft.ReportingServices.Rendering.ImageRenderer/PDFRenderer.cs:30-104` - Main PDF renderer entry point
- `Microsoft.ReportViewer.Common/Microsoft.ReportingServices.Rendering.ImageRenderer/PDFWriter.cs:1-2523` - Core PDF writing (2500+ lines)
- `Microsoft.ReportViewer.Common/Microsoft.ReportingServices.Rendering.ImageRenderer/MetafileGraphics.cs:1-200+` - GDI+ Metafile rendering
- `Microsoft.ReportViewer.Common/Microsoft.ReportingServices.Rendering.ImageRenderer/Graphics.cs:1-300+` - GDI+ graphics wrapper
- `Microsoft.ReportViewer.Common/Microsoft.ReportingServices.Rendering.ImageRenderer/GraphicsBase.cs:1-200+` - Graphics base class
- `Microsoft.ReportViewer.Common/Microsoft.ReportingServices.Rendering.ImageRenderer/PDFFont.cs:1-100+` - Font handling
- `Microsoft.ReportViewer.Common/Microsoft.Reporting/ControlService.cs:40-70` - Renderer registration

### Related Documentation
- `docs/rendering-abstractions.md` - Excel rendering (simpler, now complete) and the Chart/Gauge abstraction design
- `tasks/chart-image-abstraction-analysis.md` - Chart rendering (blocks both Excel and PDF)

### Recommended Reading Order
1. This file (pdf-render-callstack-analysis.md) - Overview and impact
2. `docs/rendering-abstractions.md` - Simpler, completed example for context
3. `tasks/chart-image-abstraction-analysis.md` - Shared challenge
4. `TODO.md` - Project progress

---

## Summary

PDF rendering in ReportViewerCore is **fundamentally dependent on Windows GDI+ graphics operations** through:

1. **Metafile graphics** - Records drawing commands in GDI+ format
2. **System.Drawing.Graphics** - All drawing operations
3. **GDI+ Bitmap rendering** - Output rendering target
4. **Font handling** - GDI+ font styles
5. **Image codecs** - Windows codec infrastructure
6. **Win32 platform calls** - Device capability queries

**To achieve cross-platform PDF rendering requires:**
- **Major refactoring:** Replace entire graphics stack with SkiaSharp
- **Effort:** 3-4 weeks of development
- **Risk:** HIGH (architectural changes)
- **Value:** Foundation for Linux/Mac support

**Alternative approach:**
- Use existing cross-platform PDF library (e.g., PdfSharpCore)
- Adapt LinuxPdfRenderer pattern to all platforms
- Lower effort, but requires evaluating PDF library capabilities

---

**Document Status:** Complete analysis of PDF rendering call stack and Windows dependencies  
**Last Updated:** 2026-07-13  
**Readiness:** Analysis complete, implementation planning ready

