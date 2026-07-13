# Renderer extension guide

## Goal

Use this guide when adding a new renderer implementation behind the existing abstraction layer.

## Steps

1. Define a contract for the renderer.
   - Keep the interface focused on the input and output that the caller needs.
2. Implement the contract for a specific platform or engine.
   - A Linux implementation can use a cross-platform library such as ClosedXML or PdfSharpCore.
3. Register or select the implementation through the factory.
   - Keep selection logic in one place rather than scattering it through the pipeline.
4. Add tests that verify behavior.
   - Prefer tests that validate stream output, object conversion, and factory selection.
5. Update the docs and support matrix.
   - Record the new renderer and any limitations.

## Example shape

A new renderer should usually follow this pattern:

- an interface such as `IExampleRenderer`,
- a concrete implementation such as `LinuxExampleRenderer`,
- a factory method for selection,
- and a unit test for the new behavior.

## Guidance

Avoid introducing broad abstractions before the first concrete seam has proven useful. The current work favors small, focused interfaces that can be extended over time.
