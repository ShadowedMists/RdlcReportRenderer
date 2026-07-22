# Chart and Gauge Image Handling: Cross-Platform Abstraction — Superseded

The design proposed here (`IImageProvider`/`ImageMetadata`/`WindowsImageProvider`/`CrossPlatformImageProvider`/`ImageProviderFactory`) is fully implemented — see [`docs/rendering-abstractions.md`](../docs/rendering-abstractions.md) for the current class/interface reference.

**Known limitation that remains:** chart/gauge background images still cannot render on non-Windows — that depends on the much larger Chart/Gauge engine migration tracked in [`chart-gdi-type-abstraction.md`](chart-gdi-type-abstraction.md) and [`gauge-gdi-type-abstraction.md`](gauge-gdi-type-abstraction.md).
