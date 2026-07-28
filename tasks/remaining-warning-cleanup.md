# Remaining build-warning cleanup: CS0649 fields needing individual judgment calls

**Status: DONE (2026-07-28).** NU1510, CS1717, CS0472, CS0809, CS0108, CS0109, CS0162, CS0414, CS0219 (166 warnings/59 files), and CA2022 (74 warnings/18 files) are all fixed — see git history. All 6 originally-flagged CS0649 fields below have been resolved: `Paragraph.m_compiledParagraphsCollection`/`ReportRuntime.m_exprHostAssembly` were deleted 2026-07-27; `ReportWalker.m_atomHeaderInstanceWalk`/`m_atomRendererWalk`, `PageTableLayout.m_firstVisibleRow`/`m_firstVisibleColumn`, and `MapControl.isCallback` were investigated in depth (via parallel sub-agent investigation) and deleted 2026-07-28, confirmed genuinely dead (present in the original decompiled Microsoft assembly with no writer, not something stripped during porting); `Chart.m_imageMapAreaCollection` was investigated the same way and deliberately left in place with a documenting comment — it's a doubly-unreachable stub (legacy `IsOldSnapshot` chart image-map path that this fork doesn't otherwise support), not a live bug.

**New, out-of-scope discovery while verifying (2026-07-28):** a full `dotnet build --no-incremental` still shows ~16 *other* CS0649 fields this doc never tracked — `Escher.BlipStoreEntry` (5 fields), `ListLevelOnFile` (2 fields), `PictureDescriptor` (7 fields), `ReportColor.m_empty` — all in `Microsoft.ReportViewer.Common`. These look like classic binary-format struct layouts (Excel/Word OLE structures) populated via raw byte-level reads the C# compiler can't trace as an "assignment" — a different, likely-benign category from the "unfinished feature" fields this doc was scoped around, not confirmed dead in the same sense. Worth its own investigation pass if picked up, but out of this doc's original scope (which is now fully closed) — not tracked further here.

**Explicitly out of scope for this doc:**
- **CA1416** (~11,000 warnings) — "reachable on all platforms but only supported on windows" for GDI+/System.Drawing APIs. This is the Chart/Gauge/PDF/Map cross-platform migration itself (see `TODO.md`, `docs/platform-support.md`) — not a warning-cleanup task.
- **SYSLIB0014** — see `tasks/webrequest-httpclient-migration.md`.
- **CS0618** (`XmlValidatingReader`) — see `tasks/xmlvalidatingreader-migration.md`.

## CS0649 — remaining dead fields (52 warnings after the fields below were fixed)

Investigated in depth via parallel sub-agent investigation, cross-checked, and deliberately left untouched because each needs a judgment call. Two of the original six were low-risk enough to fix directly (2026-07-27):

- ~~`Paragraph.m_compiledParagraphsCollection`~~ — confirmed zero external callers of both the field and its `CompiledParagraphsCollection` property; deleted both.
- ~~`ReportRuntime.m_exprHostAssembly`~~ — confirmed no assignment anywhere in the repo (declaration + one read, inside the compound assert only); removed the field and its clause from `Global.Tracer.Assert(...)` in `LoadCompiledCode`, leaving the other two (non-trivial) conditions intact.

**Complex/unfinished-feature candidates (needs a deeper read before any fix, not a quick patch) — still open:**
- `ReportWalker.m_atomHeaderInstanceWalk`/`m_atomRendererWalk` (`Microsoft.ReportingServices.Rendering.DataRenderer`) — 10+ read sites each, driving branching throughout pagination/atomization logic; always false, no write site found anywhere. High-value but needs someone who understands the atom/instance-walk state machine to confirm whether this is dead-by-design or an incomplete feature.
- `PageTableLayout.m_firstVisibleRow`/`m_firstVisibleColumn` (`Microsoft.ReportingServices.Rendering.HtmlRenderer`) — `[NonSerialized]` fields, always 0, folded into arithmetic/loop-initializer expressions across two methods (`NeedExtraRow()`, `EmptyRow()`); looks like an unfinished "resume table rendering at a row/column across page breaks" feature.
- `Chart.m_imageMapAreaCollection` (`Microsoft.ReportingServices.ReportRendering`) — `DataPointMapAreas`/`RenderChartImageMap()`/`GetImage(...)` all suggest chart image-map rendering may be an intentionally-unimplemented stub in this port; a real consumer (`ChartDataPoint.cs`) asserts non-null before indexing it, so hitting this path could assert-fail.
- `MapControl.isCallback` (`Microsoft.Reporting.Map.WebForms`) — backs a `public bool IsCallback` property with zero consumers found in-repo; flagged rather than deleted since it's `public` (potential external API surface for ASPX markup/reflection in classic ASP.NET, which a repo-wide grep can't see into consumer projects).

**Effort:** small-to-medium per field once triaged, but each needs the same repo-wide-grep-and-read-context approach — don't attempt a batch mechanical pass on these.
