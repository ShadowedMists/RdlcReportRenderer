# Architecture decisions

## Decision: introduce thin render abstractions

The rendering layer now uses small interfaces for Excel and PDF output rather than embedding platform-specific logic directly in the call sites. This keeps the implementation modular and makes it easier to swap in additional implementations later.

## Decision: use a factory for renderer selection

Renderer selection is centralized in the factory rather than being repeated across the codebase. The factory provides a single place to decide which implementation should handle a given platform and format.

## Decision: use adapters for resource handling

The new resource adapter normalizes embedded-resource payloads before the rendering layer uses them. This reduces coupling to a specific resource representation such as a stream, string, or byte array.

## Why these choices

- They preserve backwards compatibility with the existing codebase.
- They minimize the amount of new infrastructure needed for the first cross-platform seam.
- They make it easier to extend the design as additional renderers are introduced.
