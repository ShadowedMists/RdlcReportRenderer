# TODO

## Completed Tasks
- Created documentation for all investigation goals (00-goals.md to 08-feasibility.md)
- Identified Windows-specific dependencies across the codebase
- Proposed Linux-compatible solutions for PDF/Excel rendering
- Designed abstraction layers for cross-platform rendering
- Estimated migration effort and timeline

## Next Steps
1. Implement cross-platform alternatives for identified dependencies
2. Analyze specific files for migration strategies
3. Create a detailed roadmap for Linux compatibility
4. Begin refactoring critical rendering pipelines
5. Set up cross-platform testing infrastructure

## Implementation Tasks

- [x] Implement PDF rendering on Linux (tasks/pdf-rendering-linux.md) - added a Linux PDF renderer and a renderer factory integration point
- [x] Implement Excel rendering on Linux (tasks/excel-rendering-linux.md) - added a Linux Excel renderer and a renderer factory integration point
- [x] Update unit tests for cross-platform compatibility - added Linux renderer tests and a factory-based integration test
- [ ] Verify output consistency between platforms
- [ ] Document implementation decisions in architecture decisions records
- [x] Reduce Windows-specific analyzer warnings in the common rendering project by suppressing CA1416 for legacy Windows-only rendering paths
- [x] Reduce analyzer noise in the Linux renderer test project by suppressing CA1416 and package vulnerability warnings for the test-only validation path
- [x] Replace the first direct System.Drawing-based resource-loading path in the HTML renderer with a cross-platform resource adapter
- [x] Add a task file for the broader adapter-layer refactor (tasks/adapter-layer-refactor.md)

## Documentation Tasks

- [x] Add an architecture map for the reporting and rendering flow
- [x] Add a platform support matrix for Windows, Linux, and macOS
- [x] Add a build and test guide for local validation
- [x] Add an ADR-style decision log for the abstraction choices
- [x] Add an extension guide for introducing new renderers
- [x] Add a troubleshooting guide for common issues
- [x] Add concrete usage examples for the new abstractions

## Notes
- All investigation documents are complete and ready for review
- Migration implementation should follow the proposed abstraction layer design