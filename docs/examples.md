# Examples

## Excel example

The Excel abstraction can be used through the renderer factory to write a simple workbook to a stream:

```csharp
var renderer = ReportRendererFactory.CreateExcelRenderer();
using var stream = new MemoryStream();
renderer.RenderToExcel(data, stream);
```

## PDF example

The PDF abstraction can be used to write a simple document stream:

```csharp
var renderer = ReportRendererFactory.CreatePdfRenderer();
using var stream = new MemoryStream();
renderer.RenderToPdf("Hello from ReportViewerCore", stream);
```

## Resource adapter example

The resource adapter can normalize a resource payload before it is written:

```csharp
var adapter = new ImageResourceAdapter();
using var output = new MemoryStream();
adapter.WriteResource(resourceValue, output);
```
