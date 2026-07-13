# Build and test guide

## Prerequisites

- .NET SDK available on the machine
- The repository checked out locally

## Restore and build

From the repository root, run:

```powershell
dotnet restore
```

```powershell
dotnet build ReportViewerCore.sln
```

## Run the renderer-focused tests

The targeted tests for the new abstraction work can be run with:

```powershell
dotnet test tests/ReportViewerCore.LinuxRenderers.Tests/ReportViewerCore.LinuxRenderers.Tests.csproj --filter "TestImageResourceAdapterCanWriteEmbeddedData|TestExcelGeneration|TestPdfGeneration|TestRendererFactoryUsesLinuxRenderers" -v minimal
```

## Recommended validation workflow

1. Restore dependencies.
2. Build the solution.
3. Run the Linux renderer tests.
4. If a change touches the HTML rendering path, verify that resource adaptation still behaves as expected.
5. Record any new limitations in the troubleshooting guide.
