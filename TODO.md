# TODO

## Project Status Summary

**Overall Progress:** 60% - Infrastructure complete, analysis complete, implementation pending

### Current Priorities

| Priority | Phase | Status | Timeline | Risk |
|----------|-------|--------|----------|------|
| 🔴 **HIGH** | Excel Phase 4: ImageFormatType Enum | 🔄 PENDING | 2-3 days | LOW |
| 🔴 **HIGH** | Excel Phase 5: IImageProvider Abstraction | 🔄 PENDING | 3-4 days | MEDIUM |
| 🟡 **MEDIUM** | Chart Library Evaluation | 📋 BLOCKED | 2-3 weeks | HIGH |
| 🔵 **LOW** | PDF Phase 1: SkiaSharp Migration | 📋 PENDING | 2-3 weeks | VERY HIGH |

---

## Next Tasks

### Excel Phase 4: ImageFormatType Enum (READY TO START)

**Status:** Design complete, ready for implementation  
**Effort:** 2-3 days  
**Risk:** LOW (internal API only, no public breaking changes)  
**Files to Create:** `ImageFormatType.cs`  
**Files to Modify:** IExcelGenerator.cs, ImageInformation.cs, OpenXmlGenerator.cs

- [ ] Create ImageFormatType enum with cross-platform values
- [ ] Implement ImageFormatTypeHelper static class with conversion methods
- [ ] Update IExcelGenerator interface signature
- [ ] Modify ImageInformation.cs to use new enum
- [ ] Update OpenXmlGenerator.cs format detection logic
- [ ] Write unit tests for format detection and conversion
- [ ] Verify Excel XLSX output remains unchanged

**Reference:** `tasks/imagetype-enum-implementation.md` (complete 11-item checklist provided)

### Excel Phase 5: IImageProvider Abstraction (READY TO START)

**Status:** Design complete, ready for implementation  
**Effort:** 3-4 days  
**Risk:** MEDIUM (architectural change, affects chart handling)  
**Files to Create:** IImageProvider.cs, ImageMetadata.cs, WindowsImageProvider.cs, CrossPlatformImageProvider.cs, ImageProviderFactory.cs  
**Files to Modify:** ChartMapper.cs, GaugeMapper.cs, ImageInformation.cs, MainEngine.cs

- [ ] Create IImageProvider interface with LoadImage and GetImageForChart methods
- [ ] Create ImageMetadata class for image dimensions and format
- [ ] Implement WindowsImageProvider (System.Drawing wrapper)
- [ ] Implement CrossPlatformImageProvider (SixLabors.ImageSharp wrapper)
- [ ] Create ImageProviderFactory for platform-specific selection
- [ ] Inject IImageProvider into ChartMapper and GaugeMapper
- [ ] Refactor image handling to use abstraction
- [ ] Test chart rendering on Windows
- [ ] Test embedded images on cross-platform

**Reference:** `tasks/chart-image-abstraction-analysis.md` (complete design patterns and roadmap provided)

### Chart Library Evaluation (BLOCKING BOTH EXCEL & PDF)

**Status:** Analysis complete, decision needed  
**Effort:** 2-3 weeks research + decision  
**Impact:** Affects both Excel chart rendering and PDF chart rendering  
**Blocker:** Microsoft.Reporting.Chart.WebForms requires System.Drawing, has no cross-platform alternative

**Actions needed:**
- [ ] Evaluate alternative chart libraries (LiveCharts2, OxyPlot, XyChart)
- [ ] Assess licensing, API stability, feature parity
- [ ] Determine migration effort and timeline
- [ ] Create RFP or decision document
- [ ] Executive decision on chart library replacement

**Reference:** `tasks/chart-image-abstraction-analysis.md` (includes strategic considerations and alternatives)

### PDF Phase 1: SkiaSharp Graphics Migration (LOWER PRIORITY)

**Status:** Analysis complete, strategic decision pending  
**Effort:** 2-3 weeks  
**Risk:** VERY HIGH (complete graphics stack replacement)  
**Timeline:** Start after Excel phases 4-5 and chart decision  
**Blocker:** Metafile/EMF generation has NO cross-platform equivalent

**Recommendation:** Complete Excel work first, then decide on PDF strategy

**Reference:** `tasks/pdf-render-callstack-analysis.md` (complete 5-phase roadmap with implementation details)

---

## Completed Analysis & Documentation

| Category | Status | Details |
|----------|--------|---------|
| ✅ **.NET 10 Upgrade** | COMPLETE | All projects migrated, tests passing |
| ✅ **ImageSharp Integration** | COMPLETE | Image metrics cross-platform |
| ✅ **Excel Call Stack Analysis** | COMPLETE | 800+ lines, 3 major dependencies |
| ✅ **PDF Call Stack Analysis** | COMPLETE | 700+ lines, 7 major dependencies |
| ✅ **Dependency Inventory** | COMPLETE | All Windows deps documented |
| ✅ **Implementation Guides** | COMPLETE | 5 comprehensive guides created |
| ✅ **AGENTS.md Updated** | COMPLETE | Mission, guidelines, documentation standards |
| ✅ **Documentation Policy** | COMPLETE | No summary files, use TODO.md + docs/ |
| ✅ **Investigation Cleanup** | COMPLETE | Old templates removed |

---

## Documentation Available

**For Decision Makers:**
- `AGENTS.md` - Project mission and immediate priorities
- `tasks/rendering-comparison-analysis.md` - Excel vs PDF scope comparison

**For Architects:**
- `tasks/excel-render-callstack-analysis.md` - Complete Excel analysis
- `tasks/pdf-render-callstack-analysis.md` - Complete PDF analysis
- `tasks/chart-image-abstraction-analysis.md` - Architecture design

**For Developers (Phase 4):**
- `tasks/imagetype-enum-implementation.md` - Step-by-step guide with code examples
- `tasks/excel-quick-reference.md` - Quick lookup reference

**For Developers (Phase 5):**
- `tasks/chart-image-abstraction-analysis.md` - Design patterns and implementation roadmap

---

## Notes

- 🔄 All analysis complete - implementation-ready documentation provided
- 📋 Chart library replacement is blocking both Excel and PDF work
- ✅ No commits to repository (working copy only)
- 📝 Update TODO.md continuously as work progresses
- 📚 Maintain `docs/` folder synchronized with implementation changes
