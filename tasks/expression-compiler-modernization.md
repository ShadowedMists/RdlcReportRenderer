# RDL expression compiler: sandboxing and single-file-deployment gaps

**Status: NOT STARTED.** Both items below were flagged generically (one line each, no detail) in `tasks/adapter-layer-refactor.md`'s "Related README compatibility gaps" list; confirmed still real and given concrete detail 2026-07-27.

Both issues live in the same file: `Microsoft.ReportingServices.RdlExpressions/VBExpressionCodeProvider.cs`, which compiles RDL `=Expression` code via `Microsoft.CodeAnalysis.VisualBasic` (Roslyn).

## 1. No expression sandboxing exists (security)

Confirmed 2026-07-27: there is genuinely no sandboxing today. `ExprHostCompiler.Compile` still carries a `refusePermissions`/`AppDomain` parameter left over from the original CodeDom-era design, but it's a vestige with no effect under Roslyn — no permission-restriction or execution-isolation code is active anywhere in the compile/execute path. README's "What doesn't work" already states this plainly ("Do not load and run reports from untrusted sources") — this task exists to make sure that limitation stays a deliberate, documented decision rather than something someone tries to quietly patch over later without understanding the scope.

**Proposed tasks:**
1. Confirm whether Roslyn's Visual Basic compiler exposes any usable sandboxing primitive on modern .NET (AppDomain-based isolation is gone since .NET Core; realistic options are closer to "run untrusted reports in a separate process/container" than in-process sandboxing).
2. If no practical in-process option exists, formalize the current behavior as a permanent, documented limitation (mirroring how `docs/platform-support.md` documents other permanent walls) rather than leaving it as a bare README bullet.

## 2. Single-file deployment breaks expression compilation

Confirmed still real, not a stale note: `VBExpressionCodeProvider.cs` builds Roslyn `MetadataReference`s via `Assembly.Load("...").Location` (roughly lines 21-40). `Assembly.Location` returns an empty string for assemblies bundled into a single-file publish, so `MetadataReference.CreateFromFile(file)` fails whenever any report actually contains an expression — this is exactly the mechanism README's "What doesn't work" section describes ("Roslyn needs to be able to reference .NET and ReportViewer assemblies at runtime... those are unavailable [in a single file] and any non-trivial report won't compile").

**Related upstream signal:** upstream `lkosson/reportviewercore` issue #183 ("replace GetExecutingAssembly with more proper assembly") reports the same root cause — `Assembly.GetExecutingAssembly()`/`.Location`-based resource/assembly lookup breaking under single-file publish — in a different call site (embedded-resource loading via `Assembly.GetExecutingAssembly()`, e.g. `RVSplitContainer`'s bitmap resources), with a concrete proposed fix pattern: use `typeof(SomeKnownType).Assembly` instead of `GetExecutingAssembly()`/relying on `.Location`. That specific pattern doesn't fix `VBExpressionCodeProvider.cs` (it needs a reference's *file bytes*, not just its identity), but confirms this is a recurring class of bug across the codebase, not a one-off — worth a broader grep for `GetExecutingAssembly()`/`.Location` while this is being scoped, not just the expression compiler.

Separately, upstream PR #222 ("Upgrade to .NET 10 and optimize for single-file deployment") was closed unmerged/not-applicable (2025-11-22) with no recorded rationale — this fork is already on .NET 10 independently, so that PR's first goal is moot here, but its second goal (single-file optimization) is the same unresolved problem as this task; worth checking that PR's diff for any reusable approach before starting from scratch.

**Proposed tasks:**
1. Grep the codebase for `GetExecutingAssembly()`/`Assembly.Location`/`Assembly.CodeBase` usage beyond `VBExpressionCodeProvider.cs`, informed by upstream #183's finding that this is a repeating pattern.
2. For the expression compiler specifically: investigate `MetadataReference.CreateFromImage`/embedding reference assemblies as resources (or trimming/embedding the exact minimal reference set at publish time) as alternatives to reading `.Location` off a loaded `Assembly`, since single-file publish removes the on-disk DLL entirely rather than just changing its path.
3. Add a test that actually publishes as a single file and renders a report with a non-trivial expression, since this class of bug is invisible to any test that runs from a normal multi-file build.
4. Update README's "What doesn't work" and `docs/platform-support.md` once resolved or once the investigation concludes it's not practically fixable.
