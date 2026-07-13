# Developer documentation

This folder contains internal documentation for the cross-platform rendering work in ReportViewerCore. The documents are intended for maintainers and contributors who need to understand the current abstraction layer and the rationale behind the new renderer implementations.

## Documents

- [rendering-abstractions.md](rendering-abstractions.md) — explains the renderer interfaces, platform-specific implementations, design patterns, and integration points
- [architecture-map.md](architecture-map.md) — outlines the current end-to-end render flow from report processing to output generation
- [platform-support.md](platform-support.md) — summarizes current Windows, Linux, and macOS support and the remaining gaps
- [build-and-test.md](build-and-test.md) — lists the local build and test commands used to validate the cross-platform renderer work
- [decisions.md](decisions.md) — captures the main architecture decisions behind the new abstraction layer
- [renderer-extension-guide.md](renderer-extension-guide.md) — explains how to add another renderer implementation behind the existing interfaces
- [troubleshooting.md](troubleshooting.md) — collects common issues and their likely fixes
- [examples.md](examples.md) — shows small examples for the Excel, PDF, and resource adapter entry points
