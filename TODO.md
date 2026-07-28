# TODO

## Project Status Summary

Infrastructure complete. Excel Phases 4-5 complete. Chart engine GDI+→interface abstraction is substantially complete (permanently blocked on one item by design) — **but a 2026-07-28 discovery found Chart/Gauge model objects couldn't even be constructed on Linux at all until a same-day fix; three successive GDI+ construction walls (Font, GraphicsPath/HotRegionsList, StringFormat/Label.cs) have now all been fixed for their initially-confirmed sites, and WSL-verified a real milestone: `SimpleBarChart` no longer crashes on Linux at all — it actually renders a bitmap, differing only on pixel tolerance. Each fix's remaining broader caller-migration sweep (many more files with the same pattern) is scoped into its own task file, not yet started — see `tasks/chart-default-font-cross-platform.md`, `tasks/chart-hotregion-graphicspath-cross-platform.md`, `tasks/chart-stringformat-cross-platform.md`.** Gauge engine GDI+→interface abstraction is complete (no Skia backend yet — not started, low priority). PDF cross-platform rendering is complete. Map engine migration is deferred. Full detail on all of these lives in `docs/`, not here — see the Documentation index below.

### Current Priorities

| Priority | Phase | Status | Risk |
|----------|-------|--------|------|
| 🔴 **HIGH** | Excel Phase 4: ImageFormatType Enum | ✅ COMPLETE | LOW |
| 🔴 **HIGH** | Excel Phase 5: IImageProvider Abstraction | ✅ COMPLETE | MEDIUM |
| 🟡 **HIGH** | Chart engine: GDI+ → interface abstraction | ✅ Substantially complete — see `docs/rendering-abstractions.md`, `docs/platform-support.md` | HIGH |
| 🟡 **HIGH** | Gauge engine: GDI+ → interface abstraction | ✅ COMPLETE (no Skia backend — not started, low priority) | HIGH |
| 🔴 **HIGH** | Chart/Gauge: model classes' default Font crashed Linux at *construction* time (not just rendering) | ✅ Font gap fully fixed for Chart (2026-07-28) — construction-time crash, Legend's and Axis's auto-fit measurement/drawing paths all ported to IChartFont and WSL-verified (crash now moves completely past all Font construction); see `tasks/chart-default-font-cross-platform.md` | HIGH |
| 🔴 **HIGH** | Chart: `HotRegionsList`/`HotRegion` constructed raw `System.Drawing.Drawing2D.GraphicsPath` for tooltip hit-testing | ✅ HotRegionsList's own internals fixed (2026-07-28) — `HotRegion.Path` is now `IGraphicsPath`-typed, `GraphicsPathIterator` usage replaced with a `SplitAtMarkers`-style helper, a real disposal bug caught by tests along the way was fixed; WSL-verified the crash moves completely past this gap too, into a new `Label.Paint`/`StringFormat` wall. Remaining ~30-file caller migration (chart-type files still building raw `GraphicsPath` locally) scoped but not started, and not currently blocking; see `tasks/chart-hotregion-graphicspath-cross-platform.md` | MEDIUM |
| 🟢 **MEDIUM** | Chart: `Label.Paint`/`Paint3D` constructed raw `System.Drawing.StringFormat` directly | ✅ `Label.cs` fixed (2026-07-28) — added `ITextFormat.Clone()`, ported `Paint`/`Paint3D`/`GetAllLabelsRect` to `ITextFormat`; WSL-verified a genuine milestone: `SimpleBarChart` no longer crashes on Linux at all, now renders an actual bitmap (fails only on pixel tolerance, ~5%). Remaining ~64-site/~19-file sweep across other Chart-engine files scoped but not started, not currently confirmed-blocking; see `tasks/chart-stringformat-cross-platform.md` | MEDIUM |
| 🔵 **LOW** | PDF: cross-platform rendering | ✅ COMPLETE — see `docs/platform-support.md`'s "PDF (RDL engine)" section | LOW |
| 🔵 **LOW** | WebRequest → HttpClient migration (SYSLIB0014) | ✅ DONE except 2 Map-engine sites, deliberately deferred with the rest of the Map engine; see `tasks/webrequest-httpclient-migration.md` | LOW |
| 🔵 **LOW** | XmlValidatingReader → XmlReader migration (CS0618) | ✅ DONE (2026-07-27); see `docs/decisions.md`'s `RDLValidatingReader` entry | LOW |
| 🟡 **HIGH** | Map engine: GDI+ → interface abstraction | 🔄 Milestone A done (2026-07-28) — IMapRenderingEngine dual-overloads + Map-owned Gdi adapters (Pen/4 brush kinds/Font/TextFormat/GraphicsPath) added across all 3 implementers (GdiGraphics/RenderingEngine/SvgMapGraphics), zero-behavior-change verified (324 tests, byte-identical baselines); Milestone B (converting ~24 real painter files' call sites) not started; see `tasks/map-engine-cross-platform.md` | HIGH |
| 🔵 **LOW** | Remaining CS0649 dead-field warnings (~52) | ✅ DONE (2026-07-28) — all 6 originally-flagged fields resolved (5 deleted, 1 documented as an intentional stub); ~16 other, never-tracked CS0649 fields found (likely benign binary-struct-layout artifacts) — see `tasks/remaining-warning-cleanup.md` | LOW |
| 🔵 **LOW** | Word (WORD/WORDOPENXML) renderer: cross-platform support | 🔄 Image-decode gap fixed (2026-07-27) — WORDOPENXML fully cross-platform; WORD (binary) blocked on a separate COM/OLE Structured Storage gap; see `tasks/word-renderer-cross-platform.md` | LOW |
| 🔵 **LOW** | IMAGE (TIFF/EMF) renderer: cross-platform support | 🔄 JPEG/PNG raster path incl. text and rich text verified working on Linux via Skia (2026-07-28); Chart/Gauge/Map embedding investigated and root-caused (2026-07-28) — depends on `tasks/chart-default-font-cross-platform.md`, not an IMAGE-renderer-specific gap; BMP/GIF/TIFF/EMF stay Windows-only; see `tasks/image-renderer-cross-platform.md` | MEDIUM |
| 🔵 **LOW** | RDL expression compiler: sandboxing + single-file deployment | ✅ Sandboxing documented as permanent limitation; single-file deployment fixed and verified end-to-end (2026-07-27, published-single-file harness); see `tasks/expression-compiler-modernization.md` | LOW |
| 🔵 **LOW** | Test coverage: HTML/CSV/XML/WORD/IMAGE/Gauge/Map have none | ✅ DONE (2026-07-27) — HTML/CSV/XML/WORD/Gauge/IMAGE(TIFF/BMP)/Map all have smoke-test coverage now; see `tasks/test-coverage-gaps.md` | LOW |

### Long-Term Vision

The architecture should support Windows, Linux, and macOS rendering, third-party rendering engines, and future rendering technologies — the rendering system should become a platform rather than a collection of built-in renderers. See `AGENTS.md`'s Mission statement for the one-line version; this file (not AGENTS.md) tracks how close each renderer actually is to that goal.

---

## Documentation index

- `docs/rendering-abstractions.md` — Excel/PDF renderer factory design + Chart/Gauge Ports & Adapters architecture (interfaces, namespaces, recurring patterns)
- `docs/platform-support.md` — current Windows/Linux/macOS support matrix and known gaps (Chart/Gauge/Map, PDF)
- `docs/decisions.md` — architecture decisions and why (OxyPlot retraction, SkiaSharp re-target, per-method vs. per-type conversion, PDF font-embedding/shaping choices, etc.)
- `docs/troubleshooting.md` — common issues and known quirks found during the migration
- `docs/coding-standards.md` — engineering conventions, including ones established during the Chart/Gauge migration
- `docs/architecture-map.md` / `docs/build-and-test.md` / `docs/renderer-extension-guide.md` / `docs/examples.md` — supporting reference docs
- `tasks/adapter-layer-refactor.md` — broader adapter-layer scope and README compatibility-gap follow-ups (not started)
- `tasks/webrequest-httpclient-migration.md` — SYSLIB0014 migration scope (partially done, 5 sites remain)
- `tasks/remaining-warning-cleanup.md` — remaining CS0649 dead-field warnings, each needing its own judgment call
- `tasks/word-renderer-cross-platform.md` — Word 97/WordOpenXml renderer cross-platform gap (narrow — image-decode only; not started)
- `tasks/image-renderer-cross-platform.md` — IMAGE (TIFF/EMF) renderer cross-platform gap (raster+text done for JPEG/PNG; TIFF/EMF and Chart/Gauge/Map embedding remain)
- `tasks/chart-default-font-cross-platform.md` — Chart/Gauge model classes' default-Font gap (fully fixed 2026-07-28); also tracks the newly-found `Label.Paint`/`StringFormat` gap
- `tasks/chart-hotregion-graphicspath-cross-platform.md` — HotRegionsList/HotRegion GraphicsPath gap (own internals fixed 2026-07-28; ~30-file caller migration scoped, not started, not currently blocking)
- `tasks/chart-stringformat-cross-platform.md` — Label.cs's StringFormat gap (fixed 2026-07-28, confirmed SimpleBarChart now renders on Linux; ~19-file/~64-site sweep scoped, not started)
- `tasks/map-engine-cross-platform.md` — Map engine GDI+→interface migration plan (Milestone A done 2026-07-28: interface + Gdi adapters, zero behavior change; Milestone B — real call-site conversion — not started)
- `tasks/expression-compiler-modernization.md` — RDL expression compiler sandboxing (permanent limitation) + single-file-deployment gaps (fixed 2026-07-27)
- `tasks/test-coverage-gaps.md` — rendering formats with zero automated test coverage (HTML/CSV/XML/WORD/IMAGE/Gauge/Map)
- `tasks/upstream-issue-triage.md` — review of upstream `lkosson/reportviewercore` open issues against this fork's own tracking

## Notes

- Update this file's status table as milestones complete; keep any still-open scope in the linked `tasks/*.md` files, not here.
- Durable architecture facts, decisions, and known gaps belong in `docs/`, not in task-tracking narrative.
- Once a `tasks/*.md` file's work is complete, migrate anything durable into `docs/` and delete the file — don't leave a completed migration's narrative log behind as a duplicate record.
