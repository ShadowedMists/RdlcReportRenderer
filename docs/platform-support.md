# Platform support

## Current status

The new rendering abstractions are intended to support a gradual migration away from Windows-only assumptions. At the moment the work is best thought of as a compatibility layer rather than a complete cross-platform rewrite.

## Supported areas

| Area | Windows | Linux | macOS | Notes |
| --- | --- | --- | --- | --- |
| Excel rendering abstraction | Yes | Yes | Planned | Linux path uses ClosedXML |
| PDF rendering abstraction | Yes | Yes | Planned | Linux path uses PdfSharpCore |
| Embedded resource adaptation | Yes | Yes | Planned | First seam implemented in the HTML path |
| Factory-based renderer selection | Yes | Yes | Planned | Centralizes platform selection |

## Known gaps

- The broader reporting pipeline still contains legacy Windows-specific assumptions in other paths.
- Full fidelity for complex Excel or PDF layouts is not yet guaranteed.
- Additional renderers and output formats should be introduced behind the same abstraction pattern.

## Guidance

When introducing a new renderer implementation, prefer:

- a small interface for the contract,
- a platform-specific implementation behind that contract,
- a factory or registration point for selection,
- and tests that verify the behavior rather than the visual output.
