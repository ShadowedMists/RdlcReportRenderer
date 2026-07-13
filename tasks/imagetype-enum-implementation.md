# ImageFormatType Enum Implementation Guide

## Objective
Replace System.Drawing.Imaging.ImageFormat with a cross-platform custom enum to decouple from System.Drawing dependencies in Excel rendering.

## Current State

### System.Drawing Usage in Excel Rendering
- **Location:** `Microsoft.ReportingServices.Rendering.ExcelOpenXmlRenderer/OpenXmlGenerator.cs` (Line ~660)
- **Usage Pattern:** Image format GUID comparison
- **Reason:** Determine file extension for embedded images in XLSX

```csharp
// Current implementation
string extension = (format.Guid == ImageFormat.Bmp.Guid) ? "bmp" :
                   ((format.Guid == ImageFormat.Gif.Guid) ? "gif" :
                    ((!(format.Guid == ImageFormat.Jpeg.Guid)) ? "png" : "jpg"));
```

### IExcelGenerator Interface

**File:** `Microsoft.ReportingServices.Rendering.ExcelRenderer.Excel/IExcelGenerator.cs` (Line 62)

```csharp
void AddImage(string imageName, Stream imageData, ImageFormat format, 
              int rowStart, double rowStartPercentage, int columnStart, double columnStartPercentage,
              int rowEnd, double rowEndPercentage, int columnEnd, double colEndPercentage, 
              string hyperlinkURL, bool isBookmarkLink);
```

### ImageInformation Class

**File:** `Microsoft.ReportingServices.Rendering.ExcelRenderer.Layout/ImageInformation.cs`

- **Current ImageFormat usage:** Lines 82-96 (ImageFormat property)
- **SetMimeType method:** Lines 245-272 (Maps MIME types to ImageFormat)
- **DetermineImageFormat method:** Custom method added during SixLabors.ImageSharp migration
- **RawFormat detection:** Line 280 (now using SixLabors instead)

## Implementation Plan

### Step 1: Create ImageFormatType Enum

**File:** Create new file `Microsoft.ReportingServices.Rendering.ExcelRenderer.Excel/ImageFormatType.cs`

```csharp
namespace Microsoft.ReportingServices.Rendering.ExcelRenderer.Excel
{
    /// <summary>
    /// Cross-platform image format enumeration for Excel rendering.
    /// Replaces System.Drawing.Imaging.ImageFormat to maintain cross-platform compatibility.
    /// </summary>
    internal enum ImageFormatType
    {
        /// <summary>BMP (Bitmap) format</summary>
        Bmp,

        /// <summary>GIF (Graphics Interchange Format)</summary>
        Gif,

        /// <summary>JPEG (Joint Photographic Experts Group)</summary>
        Jpeg,

        /// <summary>PNG (Portable Network Graphics)</summary>
        Png,

        /// <summary>Unknown or unsupported format (defaults to PNG)</summary>
        Unknown
    }

    /// <summary>
    /// Helper class for ImageFormatType operations.
    /// </summary>
    internal static class ImageFormatTypeHelper
    {
        /// <summary>
        /// Convert ImageFormatType to file extension.
        /// </summary>
        public static string ToFileExtension(ImageFormatType format)
        {
            return format switch
            {
                ImageFormatType.Bmp => "bmp",
                ImageFormatType.Gif => "gif",
                ImageFormatType.Jpeg => "jpg",
                ImageFormatType.Png => "png",
                _ => "png" // Default to PNG
            };
        }

        /// <summary>
        /// Convert ImageFormatType to MIME type.
        /// </summary>
        public static string ToMimeType(ImageFormatType format)
        {
            return format switch
            {
                ImageFormatType.Bmp => "image/bmp",
                ImageFormatType.Gif => "image/gif",
                ImageFormatType.Jpeg => "image/jpeg",
                ImageFormatType.Png => "image/png",
                _ => "image/png" // Default to PNG
            };
        }

        /// <summary>
        /// Detect image format from MIME type string.
        /// </summary>
        public static ImageFormatType FromMimeType(string mimeType)
        {
            if (string.IsNullOrEmpty(mimeType))
                return ImageFormatType.Unknown;

            return mimeType.ToLowerInvariant() switch
            {
                "image/bmp" or "image/x-windows-bmp" => ImageFormatType.Bmp,
                "image/gif" => ImageFormatType.Gif,
                "image/jpeg" or "image/jpg" => ImageFormatType.Jpeg,
                "image/png" or "image/x-png" => ImageFormatType.Png,
                _ => ImageFormatType.Unknown
            };
        }

        /// <summary>
        /// Detect image format from image data stream using ImageSharp.
        /// </summary>
        public static ImageFormatType DetectFromStream(System.IO.Stream imageStream)
        {
            if (imageStream == null || imageStream.Length == 0)
                return ImageFormatType.Unknown;

            try
            {
                imageStream.Position = 0;
                var imageInfo = SixLabors.ImageSharp.Image.Identify(imageStream);
                if (imageInfo == null)
                    return ImageFormatType.Unknown;

                imageStream.Position = 0;

                string format = imageInfo.Metadata.DecodedImageFormat?.Name.ToLowerInvariant() ?? "png";
                return format switch
                {
                    "bmp" => ImageFormatType.Bmp,
                    "gif" => ImageFormatType.Gif,
                    "jpeg" => ImageFormatType.Jpeg,
                    "png" => ImageFormatType.Png,
                    _ => ImageFormatType.Unknown
                };
            }
            catch
            {
                return ImageFormatType.Unknown;
            }
        }
    }
}
```

### Step 2: Update IExcelGenerator Interface

**File:** `Microsoft.ReportingServices.Rendering.ExcelRenderer.Excel/IExcelGenerator.cs` (Line 62)

**Before:**
```csharp
void AddImage(string imageName, Stream imageData, ImageFormat format, int rowStart, double rowStartPercentage, ...);
```

**After:**
```csharp
void AddImage(string imageName, Stream imageData, ImageFormatType format, int rowStart, double rowStartPercentage, ...);
```

**Also remove:** `using System.Drawing.Imaging;` directive

### Step 3: Update ImageInformation Class

**File:** `Microsoft.ReportingServices.Rendering.ExcelRenderer.Layout/ImageInformation.cs`

**Changes Required:**

1. **Update property type** (Lines 82-96):
```csharp
private ImageFormatType m_imageFormat;

internal ImageFormatType ImageFormat
{
    get
    {
        if (m_imageFormat == ImageFormatType.Unknown)
        {
            CalculateMetrics();
        }
        return m_imageFormat;
    }
    set
    {
        m_imageFormat = value;
    }
}
```

2. **Update SetMimeType method** (Lines 245-272):
```csharp
internal void SetMimeType(string mimeType)
{
    if (mimeType == null)
    {
        return;
    }
    m_imageFormat = ImageFormatTypeHelper.FromMimeType(mimeType);
    if (m_imageFormat == ImageFormatType.Unknown)
    {
        throw new ReportRenderingException(ExcelRenderRes.UnknownImageFormat(mimeType));
    }
}
```

3. **Update DetermineImageFormat method** (currently added):
```csharp
private ImageFormatType DetermineImageFormat()
{
    if (m_imageData == null || m_imageData.Length == 0)
    {
        return ImageFormatType.Png;
    }
    return ImageFormatTypeHelper.DetectFromStream(m_imageData);
}
```

4. **Update usings** - Remove System.Drawing.Imaging:
```csharp
// REMOVE: using System.Drawing.Imaging;
// Already have: using Microsoft.ReportingServices.Rendering.ExcelRenderer.Excel;
```

### Step 4: Update OpenXmlGenerator

**File:** `Microsoft.ReportingServices.Rendering.ExcelOpenXmlRenderer/OpenXmlGenerator.cs`

**Locate the code around line 660:**
```csharp
string extension = (format.Guid == ImageFormat.Bmp.Guid) ? "bmp" :
                   ((format.Guid == ImageFormat.Gif.Guid) ? "gif" :
                    ((!(format.Guid == ImageFormat.Jpeg.Guid)) ? "png" : "jpg"));
```

**Replace with:**
```csharp
string extension = ImageFormatTypeHelper.ToFileExtension(format);
```

**Also remove:** `using System.Drawing.Imaging;` directive if present

### Step 5: Update BIFF8Generator (if applicable)

**File:** `Microsoft.ReportingServices.Rendering.ExcelRenderer.Excel.BIFF8/BIFF8Generator.cs`

- Check if uses ImageFormat
- Update any BIFF8-specific image format handling
- Apply same changes as OpenXmlGenerator

### Step 6: Update LayoutEngine (if applicable)

**File:** `Microsoft.ReportingServices.Rendering.ExcelRenderer.Layout/LayoutEngine.cs`

- Search for ImageFormat usage
- Update to ImageFormatType
- Example search: `ImageFormat.Png` → `ImageFormatType.Png`

## Implementation Checklist

- [ ] Create `ImageFormatType.cs` enum and helper class
- [ ] Update `IExcelGenerator.cs` interface
- [ ] Update `ImageInformation.cs` - property type
- [ ] Update `ImageInformation.cs` - SetMimeType method
- [ ] Update `ImageInformation.cs` - DetermineImageFormat method
- [ ] Update `ImageInformation.cs` - remove System.Drawing.Imaging using
- [ ] Update `OpenXmlGenerator.cs` - extension detection logic
- [ ] Update `OpenXmlGenerator.cs` - remove System.Drawing.Imaging using
- [ ] Update `BIFF8Generator.cs` (if needed)
- [ ] Update `LayoutEngine.cs` (if needed)
- [ ] Search entire solution for remaining System.Drawing.Imaging.ImageFormat usages
- [ ] Update unit tests if any reference ImageFormat directly
- [ ] Build solution - verify no errors
- [ ] Run tests - verify 5/5 pass
- [ ] Test Excel rendering with various image formats (BMP, GIF, JPEG, PNG)

## Testing Strategy

### Unit Tests

**File:** `tests/ReportViewerCore.LinuxRenderers.Tests/`

Add tests for:
```csharp
[TestFixture]
public class ImageFormatTypeTests
{
    [Test]
    public void ToFileExtension_Bmp_ReturnsBmp()
    {
        Assert.AreEqual("bmp", ImageFormatTypeHelper.ToFileExtension(ImageFormatType.Bmp));
    }

    [Test]
    public void ToMimeType_Jpeg_ReturnsImageJpeg()
    {
        Assert.AreEqual("image/jpeg", ImageFormatTypeHelper.ToMimeType(ImageFormatType.Jpeg));
    }

    [Test]
    public void FromMimeType_ImagePng_ReturnsPng()
    {
        Assert.AreEqual(ImageFormatType.Png, ImageFormatTypeHelper.FromMimeType("image/png"));
    }

    [Test]
    public void DetectFromStream_WithPngStream_ReturnsPng()
    {
        var pngBytes = /* PNG test data */;
        using (var stream = new MemoryStream(pngBytes))
        {
            var format = ImageFormatTypeHelper.DetectFromStream(stream);
            Assert.AreEqual(ImageFormatType.Png, format);
        }
    }
}
```

### Integration Tests

- Render report with embedded images in each format
- Verify XLSX contains correct image count
- Verify image dimensions are preserved
- Verify cross-platform (Windows + Linux if available)

## Backward Compatibility

**Breaking Changes:** Yes (internal API change)
- IExcelGenerator.AddImage signature changed
- Only affects internal implementations (OpenXmlGenerator, BIFF8Generator)
- No public API impact

**Mitigation:**
- Update all implementing classes in single commit
- No deprecation period needed (internal interface)
- Comprehensive test coverage before merge

## Performance Impact

- Minimal impact expected
- ImageFormatTypeHelper uses switch expressions (efficient)
- DetectFromStream uses SixLabors.ImageSharp.Image.Identify (same as current)

## Files Affected Summary

| File | Changes | Priority |
|------|---------|----------|
| ImageFormatType.cs | Create new | HIGH |
| IExcelGenerator.cs | Method signature | HIGH |
| ImageInformation.cs | Property + methods | HIGH |
| OpenXmlGenerator.cs | Format detection | HIGH |
| BIFF8Generator.cs | Format detection (if used) | MEDIUM |
| LayoutEngine.cs | Format references | MEDIUM |
| Unit tests | Add format tests | MEDIUM |

## Risk Assessment

### Low Risk
- ✅ Enum replacement straightforward
- ✅ ImageSharp detection already in place
- ✅ Interface is internal-only
- ✅ No public API changes

### Medium Risk
- ⚠️ Multiple files to update (4-6)
- ⚠️ Must verify all ImageFormat usages found
- ⚠️ Format detection logic must match current behavior

### Mitigation
- Comprehensive grep search for "ImageFormat" before starting
- Parallel execution of current approach during transition
- Thorough testing with multiple image formats

## References

### Source Files
- `Microsoft.ReportingServices.Rendering.ExcelRenderer.Excel/IExcelGenerator.cs` - Line 62
- `Microsoft.ReportingServices.Rendering.ExcelRenderer.Layout/ImageInformation.cs` - Lines 82-287
- `Microsoft.ReportingServices.Rendering.ExcelOpenXmlRenderer/OpenXmlGenerator.cs` - Line ~660
- `Microsoft.ReportingServices.Rendering.ExcelRenderer.Excel.BIFF8/BIFF8Generator.cs` - Search for ImageFormat

### Related Documentation
- `tasks/excel-render-callstack-analysis.md` - Call stack analysis with ImageFormat recommendations

### Testing
- `tests/ReportViewerCore.LinuxRenderers.Tests/` - Existing test suite (5/5 currently passing)

## Next Phase: Chart Image Handling

After ImageFormatType is complete, chart image handling still requires:
- **ChartMapper.cs:5251** - Uses System.Drawing.Image directly
- **GaugeMapper.cs** - Similar pattern
- **Reason:** Microsoft.Reporting.Chart.WebForms expects System.Drawing.Image
- **Solution:** Requires chart library refactoring (strategic decision needed)

See `tasks/excel-render-callstack-analysis.md` for detailed analysis.
