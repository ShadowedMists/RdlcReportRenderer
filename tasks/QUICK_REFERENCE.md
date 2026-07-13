# Quick Reference: Excel Rendering Call Stack & Implementation

## What You Need to Know

### The Big Picture
**Goal:** Make Excel rendering cross-platform by removing Windows-specific dependencies

**Status:** 60% complete
- ✅ Done: .NET 10 upgrade, analysis, ImageSharp integration
- 🔄 Pending: ImageFormatType enum, IImageProvider abstraction
- ⚠️ Known limit: Charts remain Windows-only (needs chart library replacement)

---

## Call Stack at a Glance

```
LocalReport.Render("EXCELOPENXML")
  └─ LocalService.Render() [LocalService.cs:358]
     └─ ExcelRenderer.Render() [ExcelRenderer.cs:67]
        └─ MainEngine.RenderRPLPage()
           ├─ Image handling ✅ (Cross-platform via ImageSharp)
           ├─ Chart rendering ❌ (Windows-only, System.Drawing)
           └─ OpenXmlGenerator.AddImage() ✅ (ClosedXML)
```

---

## Key Files

| File | Purpose | Windows Dep | Status |
|------|---------|-----------|--------|
| ImageInformation.cs:274 | Image metrics | ✅ MIGRATED | Uses SixLabors.ImageSharp |
| OpenXmlGenerator.cs:660 | Format detection | 🔄 PENDING | Needs ImageFormatType enum |
| ChartMapper.cs:5251 | Chart images | ⚠️ BLOCKER | Needs chart lib replacement |
| IExcelGenerator.cs:62 | Image interface | 🔄 PENDING | Signature needs enum change |

---

## What You Need to Implement

### 1. ImageFormatType Enum (2-3 days)
**Why:** Replace System.Drawing.Imaging.ImageFormat with cross-platform alternative

**Where:** Create `ImageFormatType.cs` in Excel renderer namespace

**What:**
```csharp
enum ImageFormatType { Bmp, Gif, Jpeg, Png, Unknown }
// + ImageFormatTypeHelper with conversion methods
```

**Files to update:** IExcelGenerator, ImageInformation, OpenXmlGenerator (3 files)

**See:** `tasks/imagetype-enum-implementation.md`

---

### 2. IImageProvider Abstraction (3-4 days)
**Why:** Isolate chart image handling to prepare for chart library migration

**Where:** Create `IImageProvider.cs` and platform-specific implementations

**What:**
```csharp
interface IImageProvider {
    ImageMetadata LoadImage(Stream) // For all platforms
    object GetImageForChart(Stream) // null on non-Windows
}
```

**Classes to create:**
- `IImageProvider.cs` - Interface
- `Windows/WindowsImageProvider.cs` - System.Drawing wrapper
- `CrossPlatformImageProvider.cs` - SixLabors.ImageSharp

**Files to modify:** ChartMapper, GaugeMapper, ImageInformation (3 files)

**See:** `tasks/chart-image-abstraction-analysis.md`

---

## Windows Dependencies Summary

### Migrated ✅
- **System.Drawing (image metrics)** → SixLabors.ImageSharp
  - ImageInformation.cs uses `Image.Identify()` instead of `Image.FromStream()`
  - Status: Complete and tested

### Pending 🔄
- **System.Drawing.Imaging.ImageFormat** → ImageFormatType enum
  - Replace GUID comparisons with switch expressions
  - Status: Design complete, ready to implement

### Technical Debt ⚠️
- **System.Drawing.Image (chart images)** → Requires chart library replacement
  - ChartMapper and GaugeMapper need chart library that doesn't use System.Drawing
  - Status: Strategic decision (separate effort)

---

## Third-Party Dependencies

### Keep ✅
- **ClosedXML** - Excel file generation (specified requirement)
- **DocumentFormat.OpenXml** - OOXML low-level API
- **SixLabors.ImageSharp** - Cross-platform image analysis

### Remove/Replace ❌
- **System.Drawing.Common** - Partially migrated, remaining usage in charts
- **System.Drawing.Imaging** - Replace with custom enum

---

## Testing Checklist

**Before/After Each Phase:**
- [ ] `dotnet build` succeeds
- [ ] `dotnet test` passes (5/5)
- [ ] No breaking changes to public API
- [ ] Excel XLSX output is valid

**For ImageFormatType:**
- [ ] Format detection works for BMP, GIF, JPEG, PNG
- [ ] Conversion methods work both directions
- [ ] MIME type detection accurate

**For IImageProvider:**
- [ ] Windows implementation uses System.Drawing
- [ ] Cross-platform implementation uses SixLabors.ImageSharp
- [ ] Chart images return null on non-Windows
- [ ] Embedded images work on all platforms

---

## Document Navigation

**Want to understand the full call stack?**
→ Read: `tasks/excel-render-callstack-analysis.md`

**Ready to implement ImageFormatType enum?**
→ Read: `tasks/imagetype-enum-implementation.md`

**Want architecture context for chart images?**
→ Read: `tasks/chart-image-abstraction-analysis.md`

**Need executive summary?**
→ Read: `tasks/IMPLEMENTATION_SUMMARY.md`

**Checking progress?**
→ Check: `TODO.md` (this file has status updates)

---

## Architecture Decisions

| Decision | Chosen | Rationale | Status |
|----------|--------|-----------|--------|
| Image analysis library | SixLabors.ImageSharp | Modern, cross-platform | ✅ DONE |
| Image format replacement | Custom enum | Decouple from System.Drawing | 🔄 PENDING |
| Chart image handling | IImageProvider interface | Future-proof for lib migration | 🔄 PENDING |
| Chart rendering | Keep System.Drawing | Requires strategic chart lib decision | ⚠️ KNOWN LIMIT |

---

## Success Criteria

### Phase 1 (ImageFormatType)
- ✅ All tests pass
- ✅ No breaking public API changes
- ✅ All ImageFormat references replaced

### Phase 2 (IImageProvider)
- ✅ Chart rendering works on Windows
- ✅ Embedded images work on all platforms
- ✅ Chart images gracefully return null on non-Windows
- ✅ All tests pass

### Final State
- ✅ Excel rendering fully cross-platform (except charts)
- ✅ Clear path for chart library migration
- ✅ No System.Drawing.Common in Excel render path (except charts)
- ✅ Tested on Windows + Linux (if available)

---

## Common Questions

**Q: Can I run Excel rendering on Linux now?**
A: Mostly yes! Embedded images work. Charts/gauges don't (need chart library replacement).

**Q: Why can't we just replace the chart library now?**
A: Chart library replacement affects rendering broadly (PDF, HTML, etc.). It's a strategic decision outside the Excel scope.

**Q: How long will ImageFormatType take?**
A: 2-3 days for experienced developer. Low risk (internal API only).

**Q: What about backward compatibility?**
A: IExcelGenerator is internal-only. No public API breaking changes.

**Q: Will tests pass after my changes?**
A: Yes, if you follow the step-by-step guides. All changes are straightforward.

---

## Getting Started

1. **Read** `IMPLEMENTATION_SUMMARY.md` (this gives you context)
2. **Pick** Phase 1 (ImageFormatType) or Phase 2 (IImageProvider)
3. **Follow** the detailed implementation guide in the relevant task file
4. **Check** the checklist as you complete each step
5. **Run** tests frequently (`dotnet test`)
6. **Ask** questions if anything is unclear

---

## Key Metrics

| Metric | Value |
|--------|-------|
| Call stack depth | 8 major components |
| Windows dependencies identified | 3 (1 migrated, 1 pending, 1 blocked) |
| Documentation pages created | 4 comprehensive |
| Implementation effort estimated | 5-7 days total |
| Tests currently passing | 5/5 ✅ |
| Public API breaking changes | 0 (internal only) |

---

**Last Updated:** 2026-07-13
**Project Status:** 60% complete, pending implementation
