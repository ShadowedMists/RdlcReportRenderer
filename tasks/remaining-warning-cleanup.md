# Remaining build-warning cleanup: CS0649 fields needing individual judgment calls

**Status: everything else in this doc's original scope is done.** NU1510, CS1717, CS0472, CS0809, CS0108, CS0109, CS0162, CS0414, CS0219 (166 warnings/59 files), and CA2022 (74 warnings/18 files) are all fixed — see git history. What's left is exactly the CS0649 fields below; each needs its own judgment call, not a mechanical pass, so it's tracked here rather than closed out.

**Explicitly out of scope for this doc:**
- **CA1416** (~11,000 warnings) — "reachable on all platforms but only supported on windows" for GDI+/System.Drawing APIs. This is the Chart/Gauge/PDF/Map cross-platform migration itself (see `TODO.md`, `docs/platform-support.md`) — not a warning-cleanup task.
- **SYSLIB0014** — see `tasks/webrequest-httpclient-migration.md`.
- **CS0618** (`XmlValidatingReader`) — see `tasks/xmlvalidatingreader-migration.md`.

## CS0649 — remaining dead fields (54 warnings after the fields below were fixed)

Investigated in depth via parallel sub-agent investigation, cross-checked, and deliberately left untouched because each needs a judgment call:

**Complex/unfinished-feature candidates (needs a deeper read before any fix, not a quick patch):**
- `ReportWalker.m_atomHeaderInstanceWalk`/`m_atomRendererWalk` (`Microsoft.ReportingServices.Rendering.DataRenderer`) — 10+ read sites each, driving branching throughout pagination/atomization logic; always false, no write site found anywhere. High-value but needs someone who understands the atom/instance-walk state machine to confirm whether this is dead-by-design or an incomplete feature.
- `PageTableLayout.m_firstVisibleRow`/`m_firstVisibleColumn` (`Microsoft.ReportingServices.Rendering.HtmlRenderer`) — `[NonSerialized]` fields, always 0, folded into arithmetic/loop-initializer expressions across two methods (`NeedExtraRow()`, `EmptyRow()`); looks like an unfinished "resume table rendering at a row/column across page breaks" feature.
- `Chart.m_imageMapAreaCollection` (`Microsoft.ReportingServices.ReportRendering`) — `DataPointMapAreas`/`RenderChartImageMap()`/`GetImage(...)` all suggest chart image-map rendering may be an intentionally-unimplemented stub in this port; a real consumer (`ChartDataPoint.cs`) asserts non-null before indexing it, so hitting this path could assert-fail.
- `ReportRuntime.m_exprHostAssembly` — only use is inside a multi-clause `Global.Tracer.Assert(...)`; the field's own clause is trivially true (it's always null) but bundled with two other non-trivial conditions in one compound assert, so isolating it isn't a clean one-line fix.
- `Paragraph.m_compiledParagraphsCollection` (`Microsoft.ReportingServices.Rendering.SPBProcessing`) — backs a property (`CompiledParagraphsCollection`) that itself has zero callers anywhere in the repo; likely both the field and property are safe to delete together, but confirm the property really has no external consumers first.
- `MapControl.isCallback` (`Microsoft.Reporting.Map.WebForms`) — backs a `public bool IsCallback` property with zero consumers found in-repo; flagged rather than deleted since it's `public` (potential external API surface for ASPX markup/reflection in classic ASP.NET, which a repo-wide grep can't see into consumer projects).

**Effort:** small-to-medium per field once triaged, but each needs the same repo-wide-grep-and-read-context approach — don't attempt a batch mechanical pass on these.
