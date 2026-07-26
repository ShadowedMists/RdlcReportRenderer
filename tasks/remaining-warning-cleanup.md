# Remaining build-warning cleanup (accomplishable categories)

**Status: NOT STARTED.** Documented 2026-07-26, immediately after the obsolete-API/CA2200/CS0169/CS0649 cleanup pass (see git history same day). This doc covers what's left in a full `dotnet build ReportViewerCore.sln -t:Rebuild` that is realistically fixable without a large architectural project — as opposed to the two already-tracked deep migrations (`tasks/webrequest-httpclient-migration.md`, `tasks/xmlvalidatingreader-migration.md`) and the CA1416 platform-support warnings, which are the existing Chart/Gauge/PDF/Map cross-platform migration itself (see `TODO.md`, `docs/rendering-abstractions.md`) — not a warning-cleanup task, just this project's normal work restated as warnings.

**Explicitly out of scope for this doc:**
- **CA1416** (11,340 warnings) — "this call site is reachable on all platforms but only supported on windows" for GDI+/System.Drawing APIs. This is the entire Chart/Gauge/PDF/Map cross-platform effort already tracked in `TODO.md` and `tasks/chart-gdi-type-abstraction.md`/`tasks/gauge-gdi-type-abstraction.md`. Do not attempt to "fix" these piecemeal here.
- **SYSLIB0014** (14 remaining) — see `tasks/webrequest-httpclient-migration.md`.
- **CS0618** (2, `XmlValidatingReader`) — see `tasks/xmlvalidatingreader-migration.md`.

## Priority order suggested below: cheapest/safest first.

---

## 1. NU1510 — unnecessary PackageReference (4 warnings, 1 package)

`Microsoft.ReportViewer.WinForms.csproj`: `System.Security.Cryptography.Xml` "will not be pruned... consider removing this package from your dependencies, as it is likely unnecessary."

**Effort: trivial.** Check whether any code in `Microsoft.ReportViewer.WinForms` actually references `System.Security.Cryptography.Xml` types (`grep -r "Cryptography.Xml"`); if nothing does, remove the `<PackageReference>`. Note: this package was pinned to 10.0.10 in a recent commit (`af09b43`) to resolve a vulnerability — if it turns out to be genuinely unused, removing it entirely is a better fix than keeping a pinned-but-unused dependency around. If something transitively needs it, leave it and this warning may need a `<NoWarn>` instead.

---

## 2. CS1717 — self-assignment (1 warning)

`Microsoft.ReportViewer.Common\Microsoft.ReportingServices.Rendering.RichText\RichTextRenderer.cs:474` — "Assignment made to same variable; did you mean to assign something else?"

**Effort: trivial to diagnose, but read carefully.** This is very likely a real bug (a typo where the RHS should reference a different variable/field than the LHS). Read the surrounding method to determine what the assignment was probably supposed to do before touching it — don't just delete the dead self-assignment, since the intended assignment (to the right variable) may still need to happen.

---

## 3. CS0472 — expression always same value (1 warning)

`Microsoft.ReportViewer.Common\Microsoft.ReportingServices.Rendering.ExcelRenderer.Layout\ImageInformation.cs:270` — "The result of the expression is always 'false' since a value of type 'ImageFormatType' is never equal to 'null' of type 'ImageFormatType?'."

**Effort: small, but investigate first.** Likely `someImageFormatTypeValue == null` where the LHS is a non-nullable `ImageFormatType` compared against a nullable-typed `null` literal (possibly from a signature change during the Excel `ImageFormatType` enum migration noted in `TODO.md`). Check whether the intended comparison was against a differently-typed nullable field/parameter, or whether the condition is genuinely always-false dead code (in which case simplify per the same "delete provably-dead branch" approach used in `tasks/`-adjacent cleanup, i.e. remove the branch only if trivially safe).

---

## 4. CS0809 — obsolete override of non-obsolete member (1 warning)

`Microsoft.ReportViewer.Common\Microsoft.ReportingServices.OnDemandReportRendering\ActionInfoWithDynamicImageMap.cs:86` — `ActionInfoWithDynamicImageMap.SetNewContext()` is marked `[Obsolete]` but overrides a non-obsolete `ActionInfo.SetNewContext()`.

**Effort: small.** Determine whether the override itself is dead (nothing calls `SetNewContext()` on an `ActionInfoWithDynamicImageMap`-typed reference, only ever on the base `ActionInfo` type, in which case the obsolete tag may be stale/no-op) or whether the `[Obsolete]` attribute was added by mistake and should be removed. Read why it was marked obsolete (check for a comment or message string on the attribute) before deciding.

---

## 5. CS0108 — member hides inherited member without `new` (4 warnings, 2 sites)

`Reference.cs` (both `Microsoft.ReportViewer.WinForms` and `Microsoft.ReportViewer.NETCore` copies, identical generated SOAP client code) — `ReportExecutionServiceSoapClient.CloseAsync()` hides `ClientBase<ReportExecutionServiceSoap>.CloseAsync()`.

**Effort: trivial, but this is generated code.** `Reference.cs` is a WCF/SOAP-generated proxy file (the `.asmx`/WSDL-generated client). Normally you'd add the `new` keyword, but check whether this file is regenerated from a `.wsdl`/service reference at build/update time — if so, a hand-edit will be silently lost next regeneration, and the better fix is a `<NoWarn>` scoped to this generated file (e.g. a `.editorconfig` exclusion for `Reference.cs`, or a `#pragma` if the file is fully static/vendored and never regenerated in practice). Determine regeneration status first.

---

## 6. CS0109 — member marked `new` unnecessarily (2 warnings)

`Microsoft.ReportViewer.Common\Microsoft.ReportingServices.Rendering.ExcelOpenXmlRenderer.Model\IPictureShapesModel.cs:14` (`ToString()`) and `IStyleModel.cs:85` (`Equals(object)`) — both declared with `new` but don't actually hide anything accessible.

**Effort: trivial.** These are almost certainly interface members redeclaring `object.ToString()`/`object.Equals(object)` with `new` out of habit (copy-paste from a class pattern) where it's not needed on an interface. Safe to just remove the `new` keyword from each declaration — verify the file still builds and nothing depended on the redundant `new` semantics (it doesn't add any, so this is a no-op removal).

---

## 7. CS0162 — unreachable code (1 warning)

`Microsoft.ReportViewer.DataVisualization\Microsoft.Reporting.Chart.WebForms\ChartArea.cs:1147` — "Unreachable code detected."

**Effort: small, investigate first.** Read the method to determine whether this is dead code that should be deleted (e.g. code after an unconditional `return`/`throw`/`break` — safe to delete) or a sign of an actual logic bug (e.g. a condition that should be reachable but an earlier branch's own unconditional exit makes it not). Don't blindly delete without reading the surrounding control flow.

---

## 8. CS0414 — field assigned but never read (12 warnings, 6 fields)

Unlike CS0649 (never assigned, always default — already triaged this session), these fields ARE assigned somewhere but the assigned value is never read. Likely genuinely dead state, but check each:

- `DataProtectionLocal.cs:42` — `m_dwProtectionFlags`
- `RIFAppendOnlyStorage.cs:19` — `m_writerSetup`
- `ThreadSet.cs:16` — `m_waitCalled` (note: this file was touched this session for the `Thread.VolatileRead`→`Volatile.Read` obsolete fix — check whether `m_waitCalled` was meant to guard something in `WaitForCompletion()`)
- `MapControl.cs:42` — `doNotDispose`
- `MapCore.cs:319` — `CurrentLatitudeLimit`
- `MapCore.cs:321` — `CurrentSrid`

**Effort: small per field, but same caution as the CS0649 triage applies** — grep the whole repo for each field name (not just the declaring file) to rule out reflection/partial-class/designer-generated reads before deleting. If genuinely dead, delete the field and its assignment sites. `MapControl.doNotDispose` and `MapCore.CurrentLatitudeLimit`/`CurrentSrid` in particular sound like they could be intended for external/designer consumption (similar to the CS0649 `MapControl.isCallback` flagged this session as a public-API-surface case) — check accessibility modifiers before assuming they're safe to delete.

---

## 9. CS0649 — remaining dead fields flagged during this session's triage, not yet resolved (54 warnings after this session's fixes)

These were investigated in depth on 2026-07-26 (two parallel sub-agent investigations, cross-checked) and deliberately left untouched because each needs a judgment call, not a mechanical fix. Full detail is in this conversation's history; summarized here for follow-up:

**Likely real, pre-existing bugs (a missing assignment, not dead-by-design):**
- `RecordSetInfo.m_validCompareOptions` (`Microsoft.ReportingServices.ReportIntermediateFormat`) — its sibling class `Microsoft.ReportingServices.ReportProcessing.RecordSetInfo` (different namespace, same name) has an analogous reader that explicitly sets `recordSetInfo.ValidCompareOptions = true;` after reading `CompareOptions`; this one's `Deserialize()` reads `CompareOptions` but never sets the flag. Consumer: `ChunkManager.cs` → `ProcessingDataReader.OverrideDataCacheCompareOptions` — the guarded code never executes for RIF-format snapshots. Likely fix: add the missing `m_validCompareOptions = true;` line in `Deserialize()`.
- `ReportPublishing.m_targetRDLNamespace` (outer class, not the nested `RmlValidatingReader` field of the same name which IS correctly assigned) — always null, gets passed into an error-message call (`m_errorContext.Register(..., "Namespace", m_targetRDLNamespace)`) instead of the intended namespace string. Likely fix: assign it (e.g. from the same literal namespace constant `RmlValidatingReader`'s constructor receives) during `ReportPublishing`'s own setup.
- `ScalableHybridList<T>.m_version` — sibling classes (`ScalableList`, `ScalableDictionary`, `SegmentedDictionary`) all increment their own `m_version` field on every `Add`/`Remove`/`Clear`; `ScalableHybridList.Add`/`Remove`/`Clear` never touch it, silently disabling the enumerator's concurrent-modification-detection `Assert`. Likely fix: increment `m_version` in `Add`/`Remove`/`Clear`, matching the sibling classes' pattern.

**Complex/unfinished-feature candidates (needs deeper read before any fix, not a quick patch):**
- `ReportWalker.m_atomHeaderInstanceWalk`/`m_atomRendererWalk` (`Microsoft.ReportingServices.Rendering.DataRenderer`) — 10+ read sites each, driving branching throughout pagination/atomization logic; always false, no write site found anywhere. High-value but needs someone who understands the atom/instance-walk state machine to confirm whether this is dead-by-design or an incomplete feature.
- `PageTableLayout.m_firstVisibleRow`/`m_firstVisibleColumn` (`Microsoft.ReportingServices.Rendering.HtmlRenderer`) — `[NonSerialized]` fields, always 0, folded into arithmetic/loop-initializer expressions across two methods (`NeedExtraRow()`, `EmptyRow()`); looks like an unfinished "resume table rendering at a row/column across page breaks" feature.
- `Chart.m_imageMapAreaCollection` (`Microsoft.ReportingServices.ReportRendering`) — `DataPointMapAreas`/`RenderChartImageMap()`/`GetImage(...)` all suggest chart image-map rendering may be an intentionally-unimplemented stub in this port; a real consumer (`ChartDataPoint.cs`) asserts non-null before indexing it, so hitting this path could assert-fail.
- `ReportRuntime.m_exprHostAssembly` — only use is inside a multi-clause `Global.Tracer.Assert(...)`; the field's own clause is trivially true (it's always null) but bundled with two other non-trivial conditions in one compound assert, so isolating it isn't a clean one-line fix.
- `Paragraph.m_compiledParagraphsCollection` (`Microsoft.ReportingServices.Rendering.SPBProcessing`) — backs a property (`CompiledParagraphsCollection`) that itself has zero callers anywhere in the repo; likely both the field and property are safe to delete together, but confirm the property really has no external consumers (e.g. via a public API surface) first.
- `MapControl.isCallback` (`Microsoft.Reporting.Map.WebForms`) — backs a `public bool IsCallback` property with zero consumers found in-repo; flagged rather than deleted since it's `public` (potential external API surface for ASPX markup/reflection in classic ASP.NET, which this repo-wide grep can't see into consumer projects).

**Effort:** small-to-medium per field once triaged, but each needs the same repo-wide-grep-and-read-context approach used this session — don't attempt a batch mechanical pass on these.

---

## 10. CS0219 — assigned-but-unused local variables (166 warnings, 59 files)

The single largest "accomplishable" category by count. Classic dead-local-variable warning — a variable is assigned a value that's never subsequently read. Concentrated in a handful of files: `ChartMapper.cs` (12), `CommonElements.cs` (8), `ZoomPanel.cs`/`ScaleBase.cs`/`SPBInteractivityProcessing.cs`/`LinearScale.cs`/`ChunkManager.cs` (6 each), plus 50+ other files with 2-4 each.

**Effort: mechanical per-site, but do NOT batch-delete blindly.** Two sub-cases:
1. **Truly dead assignment** (e.g. `int x = SomeCall(); // x never used again`) — if `SomeCall()` has no side effects, the whole statement can be deleted; if it does have side effects (e.g. `bool flag = TryDoSomething();` where the return value is ignored but the call itself matters), keep the call and just drop the unused variable/assignment (e.g. `_ = SomeCall();` or just `SomeCall();` if the return type isn't used at all).
2. **Might indicate a bug** — an assigned-but-unused variable sometimes means a follow-up check was intended but got lost (similar in spirit to this session's CS0649 findings). Given the volume, a reasonable approach is a general sweep for the "obviously dead, no side effects" cases (probably the bulk of the 166), with a slower, careful pass reserved for any that look suspicious (e.g. a `bool` result of a validation/comparison call that's assigned and dropped).

**Suggested approach when picked up:** this is a good candidate for delegating to an agent given the volume, but instruct it the same way this session's CS0649 investigation was scoped — triage first, only mechanically fix the "no side effects, provably dead" cases, and report back anything that looks like a dropped check rather than silently deleting it.

---

## 11. CA2022 — inexact `Stream.Read` (74 warnings, 18 files)

`Stream.Read(byte[], int, int)` is not guaranteed to fill the buffer in one call (it can return fewer bytes than requested, especially over network/compressed streams) — the analyzer flags call sites that don't loop or check the return value, which is a **real correctness risk**, not just style. Concentrated in binary-format readers: `ShapeData.cs`/`PathData.cs` (12 each), `MsoDrawingGroup.cs` (8), `SymbolData.cs`/`RecordFactory.cs`/`MapControl.cs` (6 each), plus 12 more files with 2 each (`WordRenderer.cs`, `PDFWriter.cs`, `SerializerBase.cs`, `BinaryFormatSerializer.cs`, `AsyncMainStreamRenderingOperation.cs`, etc.).

**Effort: mechanical fix available, but worth doing carefully — this is the one category here with genuine behavioral risk if skipped.** The modern fix is `Stream.ReadExactly(byte[], int, int)` (available .NET 7+), which throws `EndOfStreamException` if the stream ends early instead of silently returning a short read — a straight drop-in replacement everywhere the code currently assumes `Read` fills the buffer (which is the overwhelmingly common assumption in binary-format parsing code like Escher/Shape/Path/Symbol readers). For sites reading from an in-memory `MemoryStream` (many of the binary-serialization round-trip sites touched this session), a short read is realistically impossible, so this is a very low-risk, high-value mechanical fix — but for the handful of sites reading from a genuinely streamed/network source (`PDFWriter.cs`, `AsyncMainStreamRenderingOperation.cs` — check these two specifically, given their names), a short read is more plausible, so verify behavior (e.g. under partial reads, does `ReadExactly` throwing instead of the old silent-truncation match how callers already handle errors?) rather than assuming zero risk everywhere.

**Suggested approach:** another good agent-delegation candidate — same caution as CS0219: triage in-memory-stream sites (safe, mechanical `Read`→`ReadExactly` swap) separately from genuinely-streamed sites (need a closer look at caller error handling before changing behavior).

---

## Summary table

| Code | Count | Files | Effort | Risk |
|------|-------|-------|--------|------|
| NU1510 | 4 | 1 | trivial | low |
| CS1717 | 1 | 1 | trivial-to-spot, needs read | low (likely a real bug) |
| CS0472 | 1 | 1 | small | low-medium (investigate intent) |
| CS0809 | 1 | 1 | small | low |
| CS0108 | 4 | 2 (generated code) | trivial but check regen | low |
| CS0109 | 2 | 2 | trivial | none |
| CS0162 | 1 | 1 | small | low-medium (investigate intent) |
| CS0414 | 12 | 6 fields | small per-field | low, verify no reflection/designer reads |
| CS0649 (remaining) | 54 | ~9 fields triaged above + others | small-medium per-field | medium (3 are likely real bugs) |
| CS0219 | 166 | 59 | mechanical, but triage first | low for "no side effects" cases |
| CA2022 | 74 | 18 | mechanical (`ReadExactly`) | low for in-memory streams, verify for real streaming sites |
