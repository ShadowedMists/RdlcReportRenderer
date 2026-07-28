# TODO

## Project Status Summary

Infrastructure complete. Excel Phases 4-5 complete. Chart engine GDI+→interface abstraction is substantially complete (permanently blocked on one item by design) — **but a 2026-07-28 discovery found Chart/Gauge model objects couldn't even be constructed on Linux at all until a same-day fix; rendering that needs a real default Font (e.g. any chart with a Legend) still can't, a separate, deeper, not-yet-fixed gap — see `tasks/chart-default-font-cross-platform.md`.** Gauge engine GDI+→interface abstraction is complete (no Skia backend yet — not started, low priority). PDF cross-platform rendering is complete. Map engine migration is deferred. Full detail on all of these lives in `docs/`, not here — see the Documentation index below.

### Current Priorities

| Priority | Phase | Status | Risk |
|----------|-------|--------|------|
| 🔴 **HIGH** | Excel Phase 4: ImageFormatType Enum | ✅ COMPLETE | LOW |
| 🔴 **HIGH** | Excel Phase 5: IImageProvider Abstraction | ✅ COMPLETE | MEDIUM |
| 🟡 **HIGH** | Chart engine: GDI+ → interface abstraction | ✅ Substantially complete — see `docs/rendering-abstractions.md`, `docs/platform-support.md` | HIGH |
| 🟡 **HIGH** | Gauge engine: GDI+ → interface abstraction | ✅ COMPLETE (no Skia backend — not started, low priority) | HIGH |
| 🔴 **HIGH** | Chart/Gauge: model classes' default Font crashed Linux at *construction* time (not just rendering) | 🔄 Construction-time crash fixed (2026-07-28, lazy Font fields); rendering that needs a real default Font (e.g. any chart with a Legend) still crashes — permanent-class GDI+ wall, not a quick fix; see `tasks/chart-default-font-cross-platform.md` | HIGH |
| 🔵 **LOW** | PDF: cross-platform rendering | ✅ COMPLETE — see `docs/platform-support.md`'s "PDF (RDL engine)" section | LOW |
| 🔵 **LOW** | WebRequest → HttpClient migration (SYSLIB0014) | ✅ DONE except 2 Map-engine sites, deliberately deferred with the rest of the Map engine; see `tasks/webrequest-httpclient-migration.md` | LOW |
| 🔵 **LOW** | XmlValidatingReader → XmlReader migration (CS0618) | ✅ DONE (2026-07-27); see `docs/decisions.md`'s `RDLValidatingReader` entry | LOW |
| 🔵 **LOW** | Map engine: GDI+ → interface abstraction | 📋 NOT STARTED — tile-service default picked (OpenStreetMap, 2026-07-28, overridable) to unblock scoping; migration itself (347 files, ~22,400 lines) not started; see `docs/decisions.md`'s Map engine entry | HIGH |
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
- `tasks/chart-default-font-cross-platform.md` — Chart/Gauge model classes' default-Font gap (construction-time fixed 2026-07-28; rendering-time measurement still blocked)
- `tasks/expression-compiler-modernization.md` — RDL expression compiler sandboxing (permanent limitation) + single-file-deployment gaps (fixed 2026-07-27)
- `tasks/test-coverage-gaps.md` — rendering formats with zero automated test coverage (HTML/CSV/XML/WORD/IMAGE/Gauge/Map)
- `tasks/upstream-issue-triage.md` — review of upstream `lkosson/reportviewercore` open issues against this fork's own tracking

## Notes

- Update this file's status table as milestones complete; keep any still-open scope in the linked `tasks/*.md` files, not here.
- Durable architecture facts, decisions, and known gaps belong in `docs/`, not in task-tracking narrative.
- Once a `tasks/*.md` file's work is complete, migrate anything durable into `docs/` and delete the file — don't leave a completed migration's narrative log behind as a duplicate record.
