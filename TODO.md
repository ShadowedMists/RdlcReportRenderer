# TODO

## Project Status Summary

**Overall Progress:** Infrastructure complete, Excel Phases 4-5 complete, chart/gauge + PDF cross-platform pending

### Current Priorities

| Priority | Phase | Status | Timeline | Risk |
|----------|-------|--------|----------|------|
| 🔴 **HIGH** | Excel Phase 4: ImageFormatType Enum | ✅ COMPLETE | — | LOW |
| 🔴 **HIGH** | Excel Phase 5: IImageProvider Abstraction | ✅ COMPLETE | — | MEDIUM |
| 🟡 **HIGH** | Chart & Gauge: re-target GDI+ engines to SkiaSharp | ✅ SPIKE COMPLETE — Option A confirmed, scope revised | Phases 1-6 (see below) | HIGH |
| 🔵 **LOW** | PDF Phase 1: SkiaSharp Migration | 📋 PENDING | 2-3 weeks | VERY HIGH |

> **Note:** The prior "Chart Library Migration (OxyPlot)" line was retracted — see `tasks/chart-library-decision.md`. Charts are rendered by a vendored GDI+ engine we own, not an external library; the corrected plan re-targets its existing rendering seam to SkiaSharp. A time-boxed spike must size the effort before scheduling.

---

## Next Tasks

### Excel Phase 4: ImageFormatType Enum (✅ COMPLETE)

**Status:** Implementation complete, all tests passing  
**Effort:** 2-3 days  
**Risk:** LOW (internal API only, no public breaking changes)  
**Files Created:** `ImageFormatType.cs`  
**Files Modified:** IExcelGenerator.cs, ImageInformation.cs, OpenXmlGenerator.cs, BIFF8Generator.cs, LayoutEngine.cs, Escher.cs

- [x] Create ImageFormatType enum with cross-platform values
- [x] Implement ImageFormatTypeHelper static class with conversion methods
- [x] Add FromSystemDrawingImageFormat conversion method
- [x] Update IExcelGenerator interface signature
- [x] Modify ImageInformation.cs to use new enum
- [x] Update OpenXmlGenerator.cs format detection logic
- [x] Update BIFF8Generator.cs AddImage signature
- [x] Update Escher.cs DrawingGroupContainer.AddImage method
- [x] Update LayoutEngine.cs to use new enum
- [x] Verify Excel XLSX output remains unchanged
- [x] All tests passing (5/5)

**Reference:** `tasks/imagetype-enum-implementation.md` (complete 11-item checklist provided)

### Excel Phase 5: IImageProvider Abstraction (✅ COMPLETE)

**Status:** Implementation complete, all tests passing  
**Effort:** 3-4 days  
**Risk:** MEDIUM (architectural change, affects chart handling)  
**Files Created:** IImageProvider.cs, ImageMetadata.cs, WindowsImageProvider.cs, CrossPlatformImageProvider.cs, ImageProviderFactory.cs  
**Files Modified:** ChartMapper.cs, GaugeMapper.cs

- [x] Create IImageProvider interface with LoadImage and GetImageForChart methods
- [x] Create ImageMetadata class for image dimensions and format
- [x] Implement WindowsImageProvider (System.Drawing wrapper)
- [x] Implement CrossPlatformImageProvider (SixLabors.ImageSharp wrapper)
- [x] Create ImageProviderFactory for platform-specific selection
- [x] Inject IImageProvider into ChartMapper and GaugeMapper
- [x] Updated GetImageFromStream() in both mappers to use abstraction
- [x] Verified chart rendering with updated code
- [x] All tests passing (5/5)

**Reference:** `tasks/chart-image-abstraction-analysis.md` (complete design patterns and roadmap provided)

### Chart & Gauge Cross-Platform Rendering (✅ Phase 0 spike complete)

**Status:** Prior OxyPlot decision RETRACTED. Phase 0 spike (2026-07-18) confirms Option A (re-target to SkiaSharp) — see spike report in `tasks/chart-cross-platform-implementation.md` for full detail.
**Blocker:** Two vendored GDI+ engines (Chart.WebForms + GaugeContainer) rasterize to PNG/EMF via `System.Drawing`, which — per the spike — cannot even construct a `Pen`/`Font`/`Bitmap`/etc. on Linux with .NET 10, independent of `libgdiplus` presence.

**Corrected finding:** Charts are NOT rendered by a missing library — they are rendered by a **vendored GDI+ chart engine we own**, which already has an `IChartRenderingEngine` seam. The fix is to re-target that seam to SkiaSharp (dual-backend: Windows keeps GDI+, Linux uses Skia), NOT to replace the engine with OxyPlot.

**Spike result:** Built a spike-scoped `Rendering/Skia/` adapter set (`SkiaResourceFactory`, `SkiaPen`/`SkiaSolidBrush`/`SkiaChartFont`/`SkiaTextFormat`/`SkiaGraphicsPath`) + `SkiaChartGraphics : IChartRenderingEngine`, and a hand-built bar-chart scene written only against the backend-agnostic `Rendering/` port — it renders correctly on **both Windows and WSL Linux**, unmodified. This confirms the port design (already drafted in `chart-gdi-type-abstraction.md` Milestone A) is the right direction. **But** the spike also found GDI+ itself can't construct on Linux at all today (not just at the rendering seam) — so completing the per-type migration (Milestones B1b/B2/C1-C8) is now a **hard prerequisite** for any Linux chart rendering, GDI+ or Skia, not optional/parallelizable polish. Effort-wise, the adapter-pair pattern (one Gdi/Skia pair per resource type) proved mechanical once built once — the XL sizing for C6/C7 still holds, but urgency changes.

**Implementation Plan:**
- [x] Phase 0: Time-boxed spike to measure real effort — **done**, see `tasks/chart-cross-platform-implementation.md` Spike Report
- [ ] Phase 1: Platform selection + graceful degradation (no crash on Linux)
- [ ] Phase 2: Abstract the pixel/output boundary (Bitmap/Graphics → SKSurface)
- [ ] Phase 3: Finish Milestones B1b/B2/C1-C8 (`chart-gdi-type-abstraction.md`) — now understood as blocking, not just cleanup — then wire the spike's `SkiaChartGraphics`/`SkiaResourceFactory` into the real `Chart` render path (`RenderingType.Skia`)
- [ ] Phase 4: Backend factory + integration
- [ ] Phase 5: Apply same pattern to gauge engine
- [ ] Phase 6: Visual regression testing (extend the E0 harness — `tests/Microsoft.ReportViewer.DataVisualization.VisualRegressionTests/` — with a real Skia render path and a broader report corpus)

**References:**
- `tasks/chart-cross-platform-implementation.md` (corrected technical instructions with line refs)
- `tasks/chart-gdi-type-abstraction.md` (per-type task scope for replacing GDI+ types with interfaces + factory)
- `tasks/chart-library-decision.md` (retraction notice explaining why OxyPlot was dropped)

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
