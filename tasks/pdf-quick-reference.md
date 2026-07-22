# Quick Reference: PDF Rendering Call Stack & Implementation

**Status (2026-07-22):** Still not started (LOWER PRIORITY per `TODO.md`). Note: the "Comparison with Excel" section below is stale — Excel Phases 4-5 it describes as pending are now complete.

## The Challenge

**Goal:** Make PDF rendering cross-platform by replacing GDI+ graphics operations

**Complexity:** HIGH - PDF rendering renders entire pages as graphics (unlike Excel which embeds images)

**Status:** Analysis complete - implementation pending

---

## Call Stack at a Glance

```
LocalReport.Render("PDF")
  └─ PDFRenderer.Render()
     ├─ Renderer + Graphics (GDI+) ❌ WINDOWS-ONLY
     │   └─ System.Drawing.Graphics.FromImage()
     │   └─ Metafile operations
     │
     └─ PDFWriter (PDF output) ✅ MOSTLY CROSS-PLATFORM
         ├─ Font handling (System.Drawing.FontStyle) 🔄 PENDING
         ├─ Image encoding (GDI+ codecs) 🔄 PENDING
         └─ Win32 device queries ⚠️ FALLBACKS NEEDED
```

---

## Windows Dependencies Found

### Blockers (MUST FIX) 🔴
| Dependency | File | Lines | Severity | Fix |
|-----------|------|-------|----------|-----|
| **Metafile graphics** | MetafileGraphics.cs | 26-99 | CRITICAL | SkiaSharp |
| **System.Drawing.Graphics** | Graphics.cs / GraphicsBase.cs | 5-100 | CRITICAL | SkiaSharp |
| **GDI+ Bitmap** | Graphics.cs | 20-65 | HIGH | SkiaSharp surfaces |

### Manageable 🟡
| Dependency | File | Lines | Severity | Fix |
|-----------|------|-------|----------|-----|
| **Font handling** | PDFFont.cs | 4,58 | MEDIUM | Custom FontStyleType enum |
| **Image codecs** | Graphics.cs | 24,69-79 | MEDIUM | SkiaSharp encoding |
| **Win32 device queries** | MetafileGraphics.cs | 178-182 | LOW | Fallback values |
| **Win32 handles** | Graphics/GraphicsBase | 20-22,47 | LOW | Remove with SkiaSharp |

---

## Key Files

| File | Purpose | Status | Notes |
|------|---------|--------|-------|
| PDFRenderer.cs | Entry point | ✅ WORKS | Orchestrates rendering |
| PDFWriter.cs | PDF generation | ✅ WORKS | Core output, 2500+ lines |
| Renderer.cs | Page rendering | ❌ GDI+ | Uses System.Drawing.Graphics |
| Graphics.cs | GDI+ wrapper | ❌ GDI+ | Manages bitmaps & drawing |
| GraphicsBase.cs | Graphics base | ❌ GDI+ | System.Drawing.Graphics |
| MetafileGraphics.cs | Metafile ops | ❌ GDI+ | Windows EMF/EMF+ only |
| PDFFont.cs | Font handling | 🔄 PENDING | Uses FontStyle enum |

---

## What's Different from Excel

### Excel Rendering ✅ (Mostly cross-platform)
- Uses **ClosedXML** library (pure C#)
- Embeds images as blobs
- Minimal graphics needed
- **Status:** Phase 1-3 complete, Phase 4-5 pending

### PDF Rendering ❌ (Windows-only graphics)
- Must render **entire page as graphics**
- All text, shapes, lines drawn to graphics context
- Requires **full graphics library replacement**
- **Status:** Analysis complete, no implementation started

---

## What Needs to be Done

### Phase 1: Graphics Library Migration (2-3 weeks)
**Effort:** VERY HIGH | **Risk:** HIGH | **Impact:** CRITICAL

Migrate from System.Drawing.Graphics + Metafile to SkiaSharp:

```csharp
// Replace This:
private System.Drawing.Graphics m_graphicsBase;
private Metafile m_metafile;

// With This:
private SKCanvas m_skiaCanvas;
private SKSurface m_skiaSurface;
```

**Files to modify:**
- `GraphicsBase.cs` - Change to SkiaSharp surfaces
- `Graphics.cs` - Use SkiaSharp bitmaps
- `MetafileGraphics.cs` - Implement drawing recording with SkiaSharp
- `Renderer.cs` - Pass SkiaSharp canvas to drawing operations

**Why it's hard:**
- Metafile is GDI+ specific (records drawing commands)
- Must convert all drawing operations
- SkiaSharp has different API than System.Drawing

---

### Phase 2: Font Handling Abstraction (3-5 days)
**Effort:** MEDIUM | **Risk:** MEDIUM

Replace System.Drawing.FontStyle with custom enum:

```csharp
// Replace This:
internal readonly FontStyle GDIFontStyle;

// With This:
public enum FontStyleType { Regular, Bold, Italic, Underline }
internal readonly FontStyleType m_fontStyle;
```

**Files to modify:**
- `PDFFont.cs` - Use FontStyleType enum
- `PDFWriter.cs` - Font rendering with custom enum

---

### Phase 3: Image Encoding (2-3 days)
**Effort:** MEDIUM | **Risk:** LOW

Replace System.Drawing image encoding with SkiaSharp:

```csharp
// Replace This:
bitmap.Save(outputStream, ImageFormat.Jpeg);

// With This:
using (var image = SKImage.FromBitmap(bitmap))
    image.Encode(SKEncodedImageFormat.Jpeg, 90).SaveTo(outputStream);
```

---

### Phase 4: Platform Abstraction (1-2 days)
**Effort:** LOW | **Risk:** LOW

Isolate Win32 device queries:

```csharp
interface IPlatformCapabilities
{
    int GetDisplayDpiX();
    int GetDisplayDpiY();
}

// Windows: uses GetDeviceCaps()
// Linux/Mac: returns hardcoded defaults
```

---

## Critical Challenges

### 1. Metafile Rendering
**Problem:** Metafile records GDI+ drawing commands in binary format

**Why it's used:** Preserves vector graphics in PDF (not just raster)

**Solution:** SkiaSharp can record drawing commands via `SKPictureRecorder`

**Complexity:** Need to map System.Drawing.Graphics calls to SkiaSharp equivalents

### 2. Graphics API Differences

| Operation | System.Drawing | SkiaSharp |
|-----------|---|---|
| Create graphics | Graphics.FromImage() | SKCanvas from SKSurface |
| Draw line | graphics.DrawLine() | canvas.DrawLine() |
| Draw text | graphics.DrawString() | canvas.DrawText() |
| Get text size | sf.MeasureString() | paint.MeasureText() |
| Set font | new Font() | SKFont |

### 3. Font Handling
**Problem:** GDI+ font enumeration and embedding

**Solution:** Use platform-specific font APIs:
- Windows: Win32 font APIs
- Linux: FontConfig
- Mac: CoreText

**Complexity:** Fonts are complex - metrics, kerning, embedding all platform-specific

### 4. Performance
**Risk:** SkiaSharp might be slower than GDI+ for some operations

**Mitigation:** Use native SkiaSharp (C++ backend) not pure C# version

---

## Testing Strategy

### Unit Tests Needed
```csharp
[TestFixture]
public class SkiaGraphicsTests
{
    [Test]
    public void LineDrawing_Horizontal_Works() { }
    
    [Test]
    public void TextRendering_MultipleFormats_Works() { }
    
    [Test]
    public void ImageEmbedding_AllFormats_Works() { }
    
    [Test]
    [Platform("Linux,MacOsX")]
    public void PlatformCapabilities_NonWindows_UsesFallbacks() { }
}
```

### Integration Tests
- PDF rendering with charts (if chart lib supports it)
- Embedded images in PDF
- Font embedding and subsetting
- Complex layouts with multiple elements

### Manual Testing
- Visual comparison: SkiaSharp output vs GDI+ output
- File size comparison
- Print output validation
- Performance benchmarking

---

## Timeline Estimate

| Phase | Duration | Risk | Blocking |
|-------|----------|------|----------|
| Phase 1: SkiaSharp | 2-3 weeks | HIGH | YES - everything depends |
| Phase 2: Fonts | 3-5 days | MEDIUM | NO - can parallelize |
| Phase 3: Images | 2-3 days | LOW | NO - can parallelize |
| Phase 4: Platform | 1-2 days | LOW | NO - final polish |
| **Total** | **3-4 weeks** | **HIGH** | |

---

## Dependencies & Tools

### Required Libraries
- **SkiaSharp** - Cross-platform graphics (replacing System.Drawing)
- **PDFSharp or iText** - PDF low-level operations (already used via PDFWriter)

### Development Tools
- Code profiler for performance comparison
- Visual diff tool for output comparison
- Linux test environment for validation

---

## Comparison with Excel

### Excel Rendering
- ✅ Phase 1: .NET 10 upgrade - DONE
- ✅ Phase 2: ImageSharp for image metrics - DONE
- ✅ Phase 3: Architecture analysis - DONE
- 🔄 Phase 4: ImageFormatType enum - PENDING
- 🔄 Phase 5: IImageProvider abstraction - PENDING

### PDF Rendering
- ✅ Phase 0: Architecture analysis - DONE
- 🔄 Phase 1: SkiaSharp graphics - PENDING
- 🔄 Phase 2: Font handling - PENDING
- 🔄 Phase 3: Image encoding - PENDING
- 🔄 Phase 4: Platform abstraction - PENDING

**Key Difference:** PDF requires graphics library replacement (SkiaSharp), while Excel only needed image library (ImageSharp)

---

## Known Limitations

| Limitation | Impact | Workaround |
|-----------|--------|-----------|
| Metafile format | GDI+ specific vector data | SkiaSharp recording API |
| Font enumeration | Platform-specific | Use system font APIs or fallback list |
| Chart rendering | Requires separate chart lib | See chart-image-abstraction-analysis.md |
| Performance | SkiaSharp might differ from GDI+ | Benchmarking needed post-migration |

---

## Success Criteria

### Minimum (Working PDF)
- ✅ PDF renders without crashes on Windows
- ✅ All text visible and readable
- ✅ Images embedded correctly
- ✅ Layout matches Excel rendering

### Ideal (Production Ready)
- ✅ Visual output identical to GDI+ version
- ✅ Performance within 10% of original
- ✅ Works on Linux/Mac (with platform abstraction)
- ✅ All tests pass
- ✅ Font embedding works

---

## Document Navigation

**Want full architectural details?**
→ Read: `tasks/pdf-render-callstack-analysis.md`

**Want to see Excel rendering (simpler, now complete)?**
→ Read: `docs/rendering-abstractions.md`

**Interested in chart rendering (shared challenge)?**
→ Read: `tasks/chart-image-abstraction-analysis.md`

**Checking progress?**
→ Check: `TODO.md`

---

## FAQ

**Q: Why is PDF rendering harder than Excel rendering?**  
A: Excel embeds images as binary blobs using ClosedXML library. PDF must render the entire page as graphics (text, shapes, lines, images all drawn to a graphics context). This requires a full graphics library equivalent.

**Q: Why not use PdfSharpCore (like LinuxPdfRenderer)?**  
A: LinuxPdfRenderer is a stub that doesn't actually render reports properly. The real rendering happens in PDFRenderer/PDFWriter which use GDI+ graphics.

**Q: Can we skip PDF for now?**  
A: Yes - PDF is lower priority than Excel. Recommend focusing on Excel phases 4-5 first, then PDF phases 1-4.

**Q: What about charts in PDF?**  
A: Charts are rendered as images by ChartMapper, then embedded in PDF. See `chart-image-abstraction-analysis.md` - affects both Excel and PDF.

**Q: How much code needs to change?**  
A: Core graphics classes: ~500-800 lines total. Font handling: ~200 lines. Image encoding: ~100 lines. Platform abstraction: ~100 lines.

**Q: Can this be done gradually?**  
A: Partially - font and image encoding can be done after graphics migration. Win32 abstraction can be done last. But graphics migration is the blocker.

---

**Current Status:** Analysis complete  
**Next Step:** Decide whether to tackle PDF or continue with Excel phases 4-5  
**Recommendation:** Complete Excel phases 4-5 first (lower risk, faster ROI), then tackle PDF as Phase 6+

