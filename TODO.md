# TODO

## Project Status Summary

Infrastructure complete. Excel Phases 4-5 complete. Chart engine GDI+→interface abstraction is substantially complete (permanently blocked on one item by design). Gauge engine GDI+→interface abstraction is complete (no Skia backend yet — not started, low priority). PDF cross-platform rendering is complete. Map engine migration is deferred. Full detail on all of these lives in `docs/`, not here — see the Documentation index below.

### Current Priorities

| Priority | Phase | Status | Risk |
|----------|-------|--------|------|
| 🔴 **HIGH** | Excel Phase 4: ImageFormatType Enum | ✅ COMPLETE | LOW |
| 🔴 **HIGH** | Excel Phase 5: IImageProvider Abstraction | ✅ COMPLETE | MEDIUM |
| 🟡 **HIGH** | Chart engine: GDI+ → interface abstraction | ✅ Substantially complete — see `docs/rendering-abstractions.md`, `docs/platform-support.md` | HIGH |
| 🟡 **HIGH** | Gauge engine: GDI+ → interface abstraction | ✅ COMPLETE (no Skia backend — not started, low priority) | HIGH |
| 🔵 **LOW** | PDF: cross-platform rendering | ✅ COMPLETE — see `docs/platform-support.md`'s "PDF (RDL engine)" section | LOW |
| 🔵 **LOW** | WebRequest → HttpClient migration (SYSLIB0014) | 🔄 Partially done — 5 sites remain (`WebRequestHelper.cs` x2, 3 Map-engine sites); see `tasks/webrequest-httpclient-migration.md` | MEDIUM |
| 🔵 **LOW** | XmlValidatingReader → XmlReader migration (CS0618) | 📋 Not started; see `tasks/xmlvalidatingreader-migration.md` | MEDIUM |
| 🔵 **LOW** | Map engine: GDI+ → interface abstraction | 📋 NOT STARTED — deferred; see `docs/decisions.md`'s Map engine entry | HIGH |
| 🔵 **LOW** | Remaining CS0649 dead-field warnings (~54) | 📋 Each needs its own judgment call; see `tasks/remaining-warning-cleanup.md` | LOW |

> The prior "Chart Library Migration (OxyPlot)" decision was retracted — see `docs/decisions.md`. Charts/gauges are rendered by vendored GDI+ engines this repo owns, not external libraries; the plan re-targets their existing rendering seams to SkiaSharp.

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
- `tasks/xmlvalidatingreader-migration.md` — CS0618 migration scope for the RDL/RML schema validator (not started)
- `tasks/remaining-warning-cleanup.md` — remaining CS0649 dead-field warnings, each needing its own judgment call

## Notes

- Update this file's status table as milestones complete; keep any still-open scope in the linked `tasks/*.md` files, not here.
- Durable architecture facts, decisions, and known gaps belong in `docs/`, not in task-tracking narrative.
- Once a `tasks/*.md` file's work is complete, migrate anything durable into `docs/` and delete the file — don't leave a completed migration's narrative log behind as a duplicate record.
