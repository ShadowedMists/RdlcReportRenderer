# Chart and Gauge Image Handling: Cross-Platform Abstraction Analysis

## Problem Statement

The Excel rendering pipeline encounters Windows-specific dependencies when handling chart and gauge background images due to the Microsoft.Reporting.Chart.WebForms library, which fundamentally depends on System.Drawing.Image.

### Key Files with System.Drawing Dependency

1. **ChartMapper.cs** (Lines 5251-5276)
   - `CreateImage(BackgroundImage backgroundImage)` - Line 5251
   - `GetImageFromStream(BackgroundImage backgroundImage)` - Line 5270
   - Returns System.Drawing.Image to add to m_coreChart.Images collection

2. **GaugeMapper.cs** (Similar pattern)
   - Image loading for gauge background images
   - Also uses System.Drawing.Image

3. **Root Cause:**
   - Microsoft.Reporting.Chart.WebForms is Windows-specific
   - Expects System.Drawing.Image objects in its Images collection
   - Cannot be directly replaced without chart library migration

## Current Architecture

```
LocalReport.Render("EXCELOPENXML")
  ↓
ExcelRenderer.Render()
  ↓
MainEngine.RenderRPLPage()
  ├─ Image rendering for embedded images (MIGRATED to ImageSharp ✓)
  ├─ Chart rendering (uses Chart.WebForms)
  │   ├─ ChartMapper.CreateImage()
  │   │   └─ GetImageFromStream() ← System.Drawing.Image ❌
  │   └─ m_coreChart.Images.Add(name, System.Drawing.Image)
  └─ Gauge rendering (uses Gauge.WebForms)
      └─ Similar pattern to charts
```

## Analysis: Why Charts Require System.Drawing

### Microsoft.Reporting.Chart.WebForms Architecture

**Location:** `Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms/`

**Key Classes:**
- `Chart` class - Main chart control
- `Chart.Images` collection - Type: `ImageCollection`
- `Image` class - Chart image representation

**Dependency Chain:**
```
ChartMapper.CreateImage()
  ↓ (creates System.Drawing.Image)
Chart.Images.Add(name, image)  ← Expects System.Drawing.Image
  ↓
Chart rendering pipeline uses GDI+ (System.Drawing) for:
  • Image composition
  • Bitmap manipulation
  • Graphics rendering
  • Font rendering
```

### Why Direct Replacement Doesn't Work

1. **Type Compatibility:** Chart.Images collection is typed for System.Drawing.Image
   - Cannot pass wrapper objects
   - Would require modifying Chart.WebForms source (not viable)

2. **Drawing Operations:** Chart controls perform GDI+ operations
   - Drawing lines, shapes, text
   - Bitmap compositing
   - These fundamentally require System.Drawing or equivalent graphics library

3. **Scope:** Chart library refactoring is outside Excel rendering scope
   - Affects PDF rendering too
   - Affects HTML rendering
   - Cross-cutting change requiring architectural decision

## Recommended Solution: IImageProvider Abstraction

### Strategy: Isolate Chart-Specific Image Handling

Create an abstraction layer that encapsulates platform-specific image handling while keeping the Excel rendering core cross-platform.

### Step 1: Create IImageProvider Interface

**File:** `Microsoft.ReportingServices.Rendering.ExcelRenderer/IImageProvider.cs`

```csharp
namespace Microsoft.ReportingServices.Rendering.ExcelRenderer
{
    /// <summary>
    /// Platform-agnostic interface for image operations.
    /// Abstracts away platform-specific image libraries (System.Drawing vs alternatives).
    /// </summary>
    internal interface IImageProvider
    {
        /// <summary>
        /// Load an image from a stream and get its dimensions.
        /// </summary>
        ImageMetadata LoadImage(System.IO.Stream imageStream);

        /// <summary>
        /// Get an image object suitable for the rendering backend.
        /// For chart rendering, this returns System.Drawing.Image (Windows).
        /// For future cross-platform charts, would return platform-appropriate type.
        /// </summary>
        object GetImageForChart(System.IO.Stream imageStream);
    }

    /// <summary>
    /// Image metadata extracted from image data.
    /// </summary>
    internal class ImageMetadata
    {
        public int Width { get; set; }
        public float HorizontalResolution { get; set; }
        public int Height { get; set; }
        public float VerticalResolution { get; set; }
        public ImageFormatType Format { get; set; }
    }
}
```

### Step 2: Create Platform-Specific Implementations

#### Windows Implementation (Using System.Drawing)

**File:** `Microsoft.ReportingServices.Rendering.ExcelRenderer/Windows/WindowsImageProvider.cs`

```csharp
namespace Microsoft.ReportingServices.Rendering.ExcelRenderer.Windows
{
    /// <summary>
    /// Windows-specific image provider using System.Drawing.
    /// Used for chart rendering which depends on System.Drawing.Image.
    /// </summary>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    internal class WindowsImageProvider : IImageProvider
    {
        public ImageMetadata LoadImage(System.IO.Stream imageStream)
        {
            if (imageStream == null || imageStream.Length == 0)
                return null;

            imageStream.Position = 0;
            System.Drawing.Image gdiImage = System.Drawing.Image.FromStream(imageStream);
            
            var metadata = new ImageMetadata
            {
                Width = gdiImage.Width,
                Height = gdiImage.Height,
                HorizontalResolution = gdiImage.HorizontalResolution,
                VerticalResolution = gdiImage.VerticalResolution,
                Format = DetermineFormat(gdiImage.RawFormat)
            };

            gdiImage.Dispose();
            return metadata;
        }

        public object GetImageForChart(System.IO.Stream imageStream)
        {
            if (imageStream == null || imageStream.Length == 0)
                return null;

            imageStream.Position = 0;
            return System.Drawing.Image.FromStream(imageStream);
        }

        private static ImageFormatType DetermineFormat(System.Drawing.Imaging.ImageFormat rawFormat)
        {
            if (rawFormat.Equals(System.Drawing.Imaging.ImageFormat.Bmp))
                return ImageFormatType.Bmp;
            if (rawFormat.Equals(System.Drawing.Imaging.ImageFormat.Gif))
                return ImageFormatType.Gif;
            if (rawFormat.Equals(System.Drawing.Imaging.ImageFormat.Jpeg))
                return ImageFormatType.Jpeg;
            if (rawFormat.Equals(System.Drawing.Imaging.ImageFormat.Png))
                return ImageFormatType.Png;
            return ImageFormatType.Unknown;
        }
    }
}
```

#### Cross-Platform Implementation (Using SixLabors.ImageSharp)

**File:** `Microsoft.ReportingServices.Rendering.ExcelRenderer/CrossPlatformImageProvider.cs`

```csharp
namespace Microsoft.ReportingServices.Rendering.ExcelRenderer
{
    /// <summary>
    /// Cross-platform image provider using SixLabors.ImageSharp.
    /// Used for non-chart image operations (Excel embedded images, web rendering).
    /// Returns null for chart operations (not supported on non-Windows platforms).
    /// </summary>
    internal class CrossPlatformImageProvider : IImageProvider
    {
        public ImageMetadata LoadImage(System.IO.Stream imageStream)
        {
            if (imageStream == null || imageStream.Length == 0)
                return null;

            try
            {
                imageStream.Position = 0;
                var imageInfo = SixLabors.ImageSharp.Image.Identify(imageStream);
                
                if (imageInfo == null)
                    return null;

                imageStream.Position = 0;

                var metadata = new ImageMetadata
                {
                    Width = imageInfo.Width,
                    Height = imageInfo.Height,
                    HorizontalResolution = (float)imageInfo.Metadata.HorizontalResolution,
                    VerticalResolution = (float)imageInfo.Metadata.VerticalResolution,
                    Format = DetermineFormat(imageInfo.Metadata.DecodedImageFormat)
                };

                return metadata;
            }
            catch
            {
                return null;
            }
        }

        public object GetImageForChart(System.IO.Stream imageStream)
        {
            // Chart rendering is Windows-only in current architecture
            // Return null on non-Windows platforms
            // Future: Alternative chart libraries may provide cross-platform support
            return null;
        }

        private static ImageFormatType DetermineFormat(SixLabors.ImageSharp.Formats.IImageFormat format)
        {
            if (format == null)
                return ImageFormatType.Unknown;

            string formatName = format.Name.ToLowerInvariant();
            return formatName switch
            {
                "bmp" => ImageFormatType.Bmp,
                "gif" => ImageFormatType.Gif,
                "jpeg" => ImageFormatType.Jpeg,
                "png" => ImageFormatType.Png,
                _ => ImageFormatType.Unknown
            };
        }
    }
}
```

### Step 3: Update ChartMapper and GaugeMapper

**Current implementation (ChartMapper.cs:5270-5276):**
```csharp
private System.Drawing.Image GetImageFromStream(BackgroundImage backgroundImage)
{
    if (backgroundImage.Instance.ImageData == null)
        return null;
    return System.Drawing.Image.FromStream(new MemoryStream(backgroundImage.Instance.ImageData, writable: false));
}
```

**Recommended refactoring:**
```csharp
private System.Drawing.Image GetImageFromStream(BackgroundImage backgroundImage)
{
    if (backgroundImage.Instance.ImageData == null)
        return null;

    using (var stream = new MemoryStream(backgroundImage.Instance.ImageData, writable: false))
    {
        return (System.Drawing.Image)m_imageProvider.GetImageForChart(stream);
    }
}

// Chart mapper should accept IImageProvider in constructor
private readonly IImageProvider m_imageProvider;

public ChartMapper(/* other params */, IImageProvider imageProvider)
{
    m_imageProvider = imageProvider;
    // ...
}
```

### Step 4: Update ImageInformation to Use IImageProvider

**Current:** Uses SixLabors.ImageSharp directly

**Recommended:** Inject IImageProvider

```csharp
internal void CalculateMetrics()
{
    if (m_imageData == null || m_imageData.Length == 0)
        return;

    var metadata = m_imageProvider.LoadImage(m_imageData);
    if (metadata != null)
    {
        Width = metadata.Width;
        Height = metadata.Height;
        HorizontalResolution = metadata.HorizontalResolution;
        VerticalResolution = metadata.VerticalResolution;
        m_imageFormat = metadata.Format;
    }
}
```

## Implementation Roadmap

### Phase 1: Create Abstraction (Minimal Impact)
**Priority:** Medium
**Effort:** 2-3 days
**Risk:** Low

- [ ] Create IImageProvider interface
- [ ] Create WindowsImageProvider (wrapper around System.Drawing)
- [ ] Create CrossPlatformImageProvider (using SixLabors.ImageSharp)
- [ ] Unit test both implementations
- [ ] No changes to existing code required

### Phase 2: Integrate with Chart/Gauge Mappers (Medium Impact)
**Priority:** Medium
**Effort:** 1-2 days
**Risk:** Medium (affects chart rendering)

- [ ] Inject IImageProvider into ChartMapper
- [ ] Inject IImageProvider into GaugeMapper
- [ ] Update GetImageFromStream methods
- [ ] Test chart rendering with images
- [ ] Verify BIFF8 and OpenXML generators still work

### Phase 3: Integrate with ImageInformation (Low Impact)
**Priority:** Low
**Effort:** 1 day
**Risk:** Low

- [ ] Inject IImageProvider into ImageInformation
- [ ] Update CalculateMetrics() to use abstraction
- [ ] Remove direct SixLabors.ImageSharp usage from ImageInformation
- [ ] Verify image dimension calculations still work

### Phase 4: Platform-Specific Initialization
**Priority:** Medium
**Effort:** 1 day
**Risk:** Medium

- [ ] Create factory: `ImageProviderFactory.CreateProvider()`
- [ ] Windows: Use WindowsImageProvider
- [ ] Linux/Mac: Use CrossPlatformImageProvider
- [ ] Handle graceful degradation on unsupported platforms

## Factory Pattern Example

**File:** `Microsoft.ReportingServices.Rendering.ExcelRenderer/ImageProviderFactory.cs`

```csharp
namespace Microsoft.ReportingServices.Rendering.ExcelRenderer
{
    internal static class ImageProviderFactory
    {
        /// <summary>
        /// Create appropriate image provider for current platform.
        /// </summary>
        public static IImageProvider CreateProvider()
        {
            // Check if running on Windows
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Windows))
            {
                return new Windows.WindowsImageProvider();
            }
            
            // Use cross-platform provider on non-Windows
            return new CrossPlatformImageProvider();
        }
    }
}
```

## Benefits of IImageProvider Abstraction

### Immediate Benefits
- ✅ Isolates System.Drawing to Windows implementation only
- ✅ Enables cross-platform image analysis (via CrossPlatformImageProvider)
- ✅ Future-proofs architecture for chart library replacement
- ✅ Separates concerns: image analysis vs. chart integration
- ✅ Enables unit testing with mock implementations

### Strategic Benefits
- ✅ Foundation for future chart library migration
- ✅ Can support multiple image providers simultaneously
- ✅ Enables A/B testing of implementations
- ✅ Clear contract for image operations

## Known Limitations (Without Chart Library Replacement)

| Platform | Chart Images | Gauge Images | Embedded Images |
|----------|-------------|--------------|-----------------|
| Windows | ✅ Works | ✅ Works | ✅ Works (ImageSharp) |
| Linux | ❌ No chart rendering | ❌ No gauge rendering | ✅ Works (ImageSharp) |
| macOS | ❌ No chart rendering | ❌ No gauge rendering | ✅ Works (ImageSharp) |

**Note:** Chart rendering requires System.Drawing or alternative graphics library. Cross-platform solution requires strategic chart library replacement.

## Files to Create/Modify

### Create
- [ ] `Microsoft.ReportingServices.Rendering.ExcelRenderer/IImageProvider.cs`
- [ ] `Microsoft.ReportingServices.Rendering.ExcelRenderer/ImageMetadata.cs`
- [ ] `Microsoft.ReportingServices.Rendering.ExcelRenderer/ImageProviderFactory.cs`
- [ ] `Microsoft.ReportingServices.Rendering.ExcelRenderer/Windows/WindowsImageProvider.cs`
- [ ] `Microsoft.ReportingServices.Rendering.ExcelRenderer/CrossPlatformImageProvider.cs`

### Modify
- [ ] `ChartMapper.cs` - Inject IImageProvider, update GetImageFromStream
- [ ] `GaugeMapper.cs` - Similar changes to ChartMapper
- [ ] `ImageInformation.cs` - Use IImageProvider for image analysis
- [ ] `MainEngine.cs` - Pass IImageProvider to ChartMapper/GaugeMapper
- [ ] Unit tests - Add IImageProvider mock tests

## Testing Strategy

### Unit Tests for Image Providers
```csharp
[TestFixture]
public class ImageProviderTests
{
    [Test]
    public void WindowsImageProvider_LoadImage_ReturnsDimensions()
    {
        var provider = new WindowsImageProvider();
        var metadata = provider.LoadImage(pngStream);
        Assert.IsNotNull(metadata);
        Assert.Greater(metadata.Width, 0);
    }

    [Test]
    public void CrossPlatformImageProvider_LoadImage_Works()
    {
        var provider = new CrossPlatformImageProvider();
        var metadata = provider.LoadImage(pngStream);
        Assert.IsNotNull(metadata);
        Assert.AreEqual(ImageFormatType.Png, metadata.Format);
    }

    [Test]
    [Platform(Exclude = "Linux,MacOsX")]
    public void WindowsImageProvider_GetImageForChart_ReturnsSystemDrawingImage()
    {
        var provider = new WindowsImageProvider();
        var image = provider.GetImageForChart(jpgStream);
        Assert.IsInstanceOf<System.Drawing.Image>(image);
    }

    [Test]
    [Platform(Include = "Linux,MacOsX")]
    public void CrossPlatformImageProvider_GetImageForChart_ReturnsNull()
    {
        var provider = new CrossPlatformImageProvider();
        var image = provider.GetImageForChart(jpgStream);
        Assert.IsNull(image);
    }
}
```

### Integration Tests
- Chart rendering with background images (Windows)
- Embedded images in Excel (all platforms)
- Gauge rendering with images (Windows)

## References

### Source Files
- `Microsoft.ReportViewer.Common/Microsoft.ReportingServices.OnDemandReportRendering/ChartMapper.cs:5251-5276`
- `Microsoft.ReportViewer.Common/Microsoft.ReportingServices.OnDemandReportRendering/GaugeMapper.cs` (similar)
- `Microsoft.ReportViewer.DataVisualization/Microsoft.Reporting.Chart.WebForms/Chart.cs`

### Related Documentation
- `tasks/excel-render-callstack-analysis.md` - Full call stack analysis
- `tasks/imagetype-enum-implementation.md` - ImageFormatType enum details
- `investigation/03-windows-dependencies.md` - Windows dependency inventory

### Future Work
- Chart library evaluation (LiveCharts2, OxyPlot, XyChart)
- Cross-platform charting implementation
- Performance optimization of image handling

## Summary

The IImageProvider abstraction provides a clean way to:
1. ✅ Isolate Windows-specific code (System.Drawing)
2. ✅ Enable cross-platform image analysis
3. ✅ Gracefully degrade on non-Windows platforms
4. ✅ Prepare for future chart library migration

While chart rendering remains Windows-only without a charting library replacement, this abstraction enables embedding images in Excel on all platforms, which is the primary requirement for cross-platform Excel rendering.
