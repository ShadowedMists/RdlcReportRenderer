# Usage guide: rendering RDLC reports

This guide is for developers integrating ReportViewerCore into their own application to render `.rdlc` report definitions to HTML, Excel, or PDF. It replaces the former `ReportViewerCore.Sample.*` sample projects, which have been removed from the solution — the code below is the same pattern those samples demonstrated, consolidated into one reference.

For internal architecture and contributor documentation, see [README.md](README.md).

## 1. Choose a package

| Scenario | Package | Namespace |
|---|---|---|
| ASP.NET Core, console apps, services, any headless rendering | `Microsoft.ReportViewer.NETCore` | `Microsoft.Reporting.NETCore` |
| WinForms desktop app with an interactive report preview control | `Microsoft.ReportViewer.WinForms` | `Microsoft.Reporting.WinForms` |

Both expose the same `LocalReport` / `ServerReport` API shape (`LoadReportDefinition`, `DataSources`, `SetParameters`, `Render`). The WinForms package additionally provides the `ReportViewer` UI control. Pick one namespace per project — do not reference both.

## 2. Load a report and supply data

This step is identical regardless of output format. `LocalReport` processes an `.rdlc` file you loaded yourself; `ServerReport` (see [§5](#5-rendering-from-a-report-server-serverreport)) instead points at a report already published to Reporting Services.

```csharp
using Microsoft.Reporting.NETCore; // or Microsoft.Reporting.WinForms

var items = new[]
{
    new ReportItem { Description = "Widget 6000", Price = 104.99m, Qty = 1 },
    new ReportItem { Description = "Gizmo MAX", Price = 1.41m, Qty = 25 },
};

using var report = new LocalReport();
using var reportDefinition = File.OpenRead("Report.rdlc"); // or an embedded resource stream
report.LoadReportDefinition(reportDefinition);
report.DataSources.Add(new ReportDataSource("Items", items));
report.SetParameters(new[] { new ReportParameter("Title", "Invoice 4/2020") });
```

`ReportDataSource` names must match the dataset names defined in the report; `ReportParameter` names must match report parameters. Any `IEnumerable<T>` works as a data source — there's no requirement to use `DataTable`.

To load the report definition from an embedded resource instead of a file (common in ASP.NET Core projects that ship the `.rdlc` as content):

```csharp
using var reportDefinition = Assembly.GetExecutingAssembly()
    .GetManifestResourceStream("YourNamespace.Reports.Report.rdlc");
report.LoadReportDefinition(reportDefinition);
```

## 3. Render to a format

Once the report is loaded, call `Render` with the desired format string. All three formats below share the same `byte[] Render(string format)` signature:

```csharp
byte[] pdf  = report.Render("PDF");
byte[] html = report.Render("HTML5");
byte[] xls  = report.Render("EXCEL");         // Excel 97-2003 (.xls)
byte[] xlsx = report.Render("EXCELOPENXML");  // Excel Open XML (.xlsx)
```

| Format string | Output | Notes |
|---|---|---|
| `PDF` | PDF document | |
| `HTML4.0` / `HTML5` | Self-contained HTML | `HTML5` also renders correctly with JavaScript disabled. There is no interactive, stateful web viewer — each call produces a static snapshot. |
| `EXCEL` | Excel 97-2003 (`.xls`) | |
| `EXCELOPENXML` | Excel Open XML (`.xlsx`) | |

Other supported format strings (`IMAGE`, `WORD`, `WORDOPENXML`, `CSV`, `XML`) follow the same `Render(string)` call — see [README.md](../README.md#supported-rendering-formats) for the full list and Linux caveats.

If you need the MIME type and file extension for the format at runtime (e.g. to set response headers), use the overload that returns them as `out` parameters instead of hardcoding them:

```csharp
byte[] output = report.Render(
    format, deviceInfo: null,
    mimeType: out var mimeType,
    encoding: out _,
    fileNameExtension: out var extension,
    streams: out _,
    warnings: out _);
```

## 4. Serve the output from ASP.NET Core

A single parameterized handler covers PDF, HTML, and both Excel formats — there's no need for one method per format:

```csharp
public class IndexModel : PageModel
{
    private static readonly Dictionary<string, (string Extension, string MimeType)> Formats = new()
    {
        ["PDF"] = ("pdf", "application/pdf"),
        ["HTML5"] = ("html", "text/html"),
        ["EXCEL"] = ("xls", "application/vnd.ms-excel"),
        ["EXCELOPENXML"] = ("xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
    };

    [FromForm] public decimal WidgetPrice { get; set; }
    [FromForm] public decimal GizmoPrice { get; set; }

    public IActionResult OnPostRender(string format)
    {
        var (extension, mimeType) = Formats[format];
        using var report = new LocalReport();
        Report.Load(report, WidgetPrice, GizmoPrice);
        var bytes = report.Render(format);
        return File(bytes, mimeType, $"report.{extension}");
    }
}
```

```html
<button asp-page-handler="Render" asp-route-format="PDF">Get PDF</button>
<button asp-page-handler="Render" asp-route-format="HTML5">Get HTML</button>
<button asp-page-handler="Render" asp-route-format="EXCELOPENXML">Get XLSX</button>
```

`Report.Load` here is your own helper following the pattern in [§2](#2-load-a-report-and-supply-data) — keep report-loading logic in one place and call it from every handler rather than duplicating `LoadReportDefinition`/`DataSources`/`SetParameters` per format.

## 5. Rendering from a report server (`ServerReport`)

If the report is published to Reporting Services rather than shipped as a local `.rdlc`, use `ServerReport` in place of `LocalReport`. It exposes the same `Render(string format)` method:

```csharp
using var report = new ServerReport();
report.ReportServerCredentials.NetworkCredentials =
    new NetworkCredential("login", "password", "DOMAIN");
report.ReportServerUrl = new Uri("http://localhost/ReportServer");
report.ReportPath = "/Invoice";
report.SetParameters(new[] { new ReportParameter("Title", "Invoice 4/2020") });

byte[] pdf = report.Render("PDF");
```

## 6. Interactive preview in WinForms (`ReportViewer` control)

For a desktop app that needs an on-screen, paginated preview (rather than just producing a file), host the `ReportViewer` control and drive either `LocalReport` or `ServerReport` through it — the control switches via `ProcessingMode`:

```csharp
using Microsoft.Reporting.WinForms;

class ReportViewerForm : Form
{
    private readonly ReportViewer reportViewer;

    public ReportViewerForm(bool useServerReport)
    {
        Text = "Report viewer";
        WindowState = FormWindowState.Maximized;
        reportViewer = new ReportViewer { Dock = DockStyle.Fill };
        Controls.Add(reportViewer);

        if (useServerReport)
        {
            reportViewer.ProcessingMode = ProcessingMode.Remote;
            reportViewer.ServerReport.ReportServerCredentials.NetworkCredentials =
                new NetworkCredential("login", "password", "DOMAIN");
            reportViewer.ServerReport.ReportServerUrl = new Uri("http://localhost/ReportServer");
            reportViewer.ServerReport.ReportPath = "/Invoice";
            reportViewer.ServerReport.SetParameters(new[] { new ReportParameter("Title", "Invoice 4/2020") });
        }
        else
        {
            Report.Load(reportViewer.LocalReport); // your loading helper, see §2
        }

        reportViewer.RefreshReport();
    }
}
```

`ReportViewer` has no WinForms designer support in this port — add it programmatically as shown above rather than dragging it from the toolbox (see [README.md](../README.md#what-doesnt-work)).

## Further reading

- [README.md](../README.md) — supported formats, Linux/macOS rendering workaround, known limitations
- [rendering-abstractions.md](rendering-abstractions.md) — internal renderer architecture (for contributors, not required for consuming the library)
- [troubleshooting.md](troubleshooting.md) — common issues encountered during rendering
