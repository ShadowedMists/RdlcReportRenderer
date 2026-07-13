# Cross-platform adapter layer refactor

## Goal

Replace the current ad-hoc rendering and resource adaptation seams with a broader cross-platform abstraction layer that can support Linux and future platforms without embedding Windows-specific assumptions in the core rendering flow.

## Scope

- Introduce a shared abstraction for resource access and image handling.
- Replace additional direct rendering or resource-loading paths with abstractions.
- Keep the existing public behavior compatible where practical.
- Reduce the need for project-wide warning suppressions as each seam is migrated.

## Proposed tasks

1. Inventory the remaining direct rendering and resource-loading entry points.
   - Identify places that still depend on platform-specific drawing or image logic.
   - Group them by responsibility so they can be migrated incrementally.

2. Define the cross-platform abstraction contracts.
   - Create interfaces for image/resource access, drawing operations, and output writing where they are currently implicit.
   - Keep interfaces narrow and focused on the behavior each caller needs.

3. Implement platform-specific adapters.
   - Add a neutral implementation for the current cross-platform path.
   - Preserve the existing Windows-backed behavior behind adapter implementations where necessary.

4. Replace the first set of direct usages.
   - Migrate the HTML rendering path and other low-risk seams first.
   - Ensure the adapter is used consistently rather than only in isolated call sites.

5. Add behavioral tests for the abstraction layer.
   - Cover resource normalization, renderer selection, and platform-specific fallback behavior.
   - Avoid relying on visual output for verification.

6. Narrow warning suppressions.
   - Remove project-wide suppression once the migrated seams are warning-free.
   - Keep suppressions limited to clearly identified legacy paths.

7. Document the abstraction model.
   - Update the architecture, decisions, and extension guide documents with the new patterns.
   - Record migration progress and remaining gaps.

## Related README compatibility gaps

The current rendering work only covers part of the compatibility story described in [README.md](../README.md). The refactor should also include follow-up work for the broader gaps listed there:

- Investigate support options for spatial SQL types such as SqlGeography and document the current limitation.
- Review expression sandboxing and code-security expectations for untrusted report definitions.
- Assess whether interactive web preview can be supported on ASP.NET Core without a major rewrite, or document it as out of scope.
- Document the recommended WinForms designer workflow for programmatic control.
- Investigate single-file deployment compatibility for Roslyn and runtime assembly loading.
- Validate the current status of the map control and capture any known limitations.
- Continue replacing System.Drawing-dependent image handling on non-Windows platforms with cross-platform abstractions.

## Acceptance criteria

- The adapter layer is used by more than one rendering seam.
- Direct platform-specific resource-loading logic is reduced in the common rendering project.
- The new abstraction has unit coverage.
- The warning suppression is narrower and easier to reason about.
- The major compatibility gaps called out in [README.md](../README.md) are either addressed, documented, or explicitly tracked as future work.
