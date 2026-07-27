# Upstream issue triage (lkosson/reportviewercore)

**Status: reference/triage list, not a single task.** Compiled 2026-07-27 by reviewing the upstream fork's open GitHub issues (https://github.com/lkosson/reportviewercore/issues) for anything not already covered by this fork's own docs/tasks. Most substantive cross-platform issues upstream are already tracked here in more detail; this file exists so those issues aren't rediscovered from scratch, and to record the smaller items that don't warrant their own task file yet.

## Already covered by existing tracking (no new action needed here)

- **#243 "Using charts under Linux will cause an error"** — reports `System.Drawing.Common` failing on Linux for chart rendering. This is exactly the Phase 0 GDI+ finding already documented in `docs/platform-support.md` ("GDI+ cannot construct any System.Drawing object at all on Linux"), and is already resolved in this fork via the Chart Skia backend (`ChartRenderingBackendFactory`). Worth a quick sanity check that the fork's fix genuinely covers the reporter's exact scenario, but no new work is implied.
- **#146 "fix drawing image in rendering excel/word report in linux"** — folded into `tasks/word-renderer-cross-platform.md` (Excel's half is already resolved in this fork; Word's half is not).
- **#42 "PDF & Image generation fails on Windows Nano Server Docker container"** — folded into `tasks/image-renderer-cross-platform.md` (confirms the GDI+/Uniscribe dependency is a real concern on headless Windows too, not just Linux).
- **#183 "replace GetExecutingAssembly with more proper assembly"** and **#222 "Upgrade to .NET 10 and optimize for single-file deployment"** — folded into `tasks/expression-compiler-modernization.md`.

## Smaller items, not yet worth a dedicated task file

- **#241 "System.Security.Cryptography.Xml high security vulnerabilities"** (open) — issue body is an image attachment with no text, so the specific CVE isn't confirmed from the issue alone. This fork already references `System.Security.Cryptography.Xml` version `10.0.10` (both `Microsoft.ReportViewer.NETCore.csproj` and `Microsoft.ReportViewer.WinForms.csproj`), which is current as of this writing — likely already resolved by being on a recent version, but worth a `dotnet list package --vulnerable` check next time dependencies are touched, rather than assuming.
- **#239 "Change stream creation to use temporary file to fix 'out of memory' exception"** (PR, unmerged) — suggests spooling large report output to a temp file instead of holding it fully in memory. Worth a look next time large-report memory usage comes up, but no confirmed reproduction against this fork yet.
- **#223 "can add Unicode support?"** (closed, not planned upstream) — notably, **this fork already implements full Unicode support for PDF** (composite CID fonts, bidi run reordering — see `docs/platform-support.md`'s PDF section) as part of the cross-platform PDF work, which upstream declined to build. Worth keeping in mind as a real, intentional divergence from upstream rather than an oversight if this ever comes up in a diff/compatibility discussion.
- **#56 "Blazor WebAssembly Support"** (open, feature request) — out of scope for this fork's current cross-platform focus (server-side rendering paths); noted here only so it isn't silently forgotten if priorities change.
- **#10 "roadmap"** (open, general production-readiness question) — no specific action item, just useful context that upstream users are asking the same "is this production-ready cross-platform" question this fork's own work has been actively answering.

## Not applicable / no action

- Various closed issues (#236, #235, #234, #233, #232, #230, #229, #228, #227, #226, #225, #224) are either upstream-specific packaging/resource-locale fixes already resolved there, or Kerberos/HTTP-auth features unrelated to rendering — no overlap with this fork's current work identified.
