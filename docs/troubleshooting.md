# Troubleshooting

## Common issues

### Renderer tests fail to build

Check that the required NuGet packages are restored and that the test project references the common rendering project correctly.

### PDF output is empty or visually sparse

The initial Linux PDF implementation is intentionally lightweight. If the output needs richer layout, the renderer contract should be expanded and another implementation should be introduced.

### Excel output is missing expected content

Verify the input payload type and confirm the renderer receives the expected data shape. The current implementation supports simple DataTable, DataSet, and scalar-value paths.

### Embedded resources are not written correctly

Confirm that the resource payload is exposed as a stream, string, byte array, or another supported object-backed format that the adapter can normalize.

### Analyzer warnings remain noisy

Some legacy Windows-specific paths still produce warnings. The current mitigation is to suppress the known warning categories for the legacy paths while new abstractions are introduced.
