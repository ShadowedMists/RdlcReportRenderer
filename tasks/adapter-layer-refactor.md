# Cross-platform adapter layer refactor

## Goal

Replace the current ad-hoc rendering and resource adaptation seams with a broader cross-platform abstraction layer that can support Linux and future platforms without embedding Windows-specific assumptions in the core rendering flow.

## Scope

- Introduce a shared abstraction for resource access and image handling.
- Replace additional direct rendering or resource-loading paths with abstractions.
- Keep the existing public behavior compatible where practical.
- Reduce the need for project-wide warning suppressions as each seam is migrated.

## Proposed tasks

1. Inventory remaining direct rendering/resource-loading entry points and group by responsibility.
2. Define narrow cross-platform abstraction contracts (image/resource access, drawing, output writing) where still implicit.
3. Implement platform-specific adapters, preserving existing Windows-backed behavior.
4. Migrate the first set of direct usages (HTML rendering path and other low-risk seams first).
5. Add behavioral tests for the abstraction layer (resource normalization, renderer selection, fallback) — avoid relying on visual output.
6. Narrow warning suppressions to only clearly identified legacy paths as each seam migrates.
7. Keep `docs/` (architecture, decisions, extension guide) updated as patterns land — see `docs/rendering-abstractions.md` for the Chart/Gauge Ports & Adapters work already following this same shape.

## Related README compatibility gaps

The current rendering work only covers part of the compatibility story described in [README.md](../README.md). Re-audited 2026-07-27 against actual current progress (Chart/Excel/PDF cross-platform work, plus a review of upstream `lkosson/reportviewercore`'s open issues — see `tasks/upstream-issue-triage.md`); several of the original bullets below are now resolved or have their own dedicated task file, so this list only carries the ones still open here:

- Investigate support options for spatial SQL types such as SqlGeography and document the current limitation. *(still open, not started)*
- Review expression sandboxing and code-security expectations for untrusted report definitions. *(still open — see `tasks/expression-compiler-modernization.md` for concrete detail, including that no sandboxing exists today)*
- Assess whether interactive web preview can be supported on ASP.NET Core without a major rewrite, or document it as out of scope. *(still open, not started)*
- Investigate single-file deployment compatibility for Roslyn and runtime assembly loading. *(still open — see `tasks/expression-compiler-modernization.md`; confirmed still real 2026-07-27, `Assembly.Location`-based reference loading breaks under single-file publish)*
- Validate the current status of the map control and capture any known limitations. *(still open — Map migration itself is deferred per `docs/decisions.md`, but even its existing Windows-only behavior is unverified by any test; see `tasks/test-coverage-gaps.md`)*
- Continue replacing System.Drawing-dependent image handling on non-Windows platforms with cross-platform abstractions. *(narrowed 2026-07-27 — Chart, Excel, and PDF's real RDL engine are now done; remaining scope is specifically the Word renderer (`tasks/word-renderer-cross-platform.md`), the IMAGE/TIFF/EMF renderer (`tasks/image-renderer-cross-platform.md`), and Gauge's Skia backend (`docs/platform-support.md`) — track further work in those files, not as a vague bullet here)*

Resolved, removed from this list: "Document the recommended WinForms designer workflow for programmatic control" — done in `docs/usage-guide.md#6`, already linked from README's "What doesn't work" section.

## Acceptance criteria

- The adapter layer is used by more than one rendering seam.
- Direct platform-specific resource-loading logic is reduced in the common rendering project.
- The new abstraction has unit coverage.
- The warning suppression is narrower and easier to reason about.
- The major compatibility gaps called out in [README.md](../README.md) are either addressed, documented, or explicitly tracked as future work.
