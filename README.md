# RdlCore

**A cross-platform .NET rendering engine for RDL/RDLC reports** — the format historically produced by SQL Server Reporting Services and Report Designer. RdlCore lets you load, process, and render `.rdlc`/RDL report definitions to PDF, Excel, Word, HTML, CSV, XML, and image formats on Windows, Linux, and macOS, from ASP.NET Core, console apps, services, or WinForms desktop applications — no SQL Server Reporting Services installation required.

> **Licensing notice:** Large parts of this codebase originate from decompiling a proprietary Microsoft product. That code is **not** covered by an open-source license, and no license granted by this project can extend one to it. See [License](#license) below before using this project in anything you redistribute. This is not legal advice — if that matters to your use case, consult your own counsel.

For version history and recent fixes, see the [changelog](CHANGELOG.md).

## Acknowledgements

RdlCore builds directly on the extraordinary work of **[Łukasz Kosson](https://github.com/lkosson)**, whose [reportviewercore](https://github.com/lkosson/reportviewercore) project first decompiled and ported Microsoft's Report Viewer for WinForms to .NET Core, and kept it alive and usable long after Microsoft made clear there would be no official successor. Every renderer in this repository — Excel, PDF, Word, Chart, Gauge, and the RDL processing engine itself — exists because of that original effort. This project is a fork and continuation of that work, focused specifically on removing the remaining Windows-only dependencies so the engine can run natively wherever .NET runs.

## Mission

Reporting Services' report definition format (RDL/RDLC) is mature, well-tooled (Visual Studio's Report Designer), and used in a huge number of existing business applications — but the only engine that could render it was tied to Windows, GDI+, and a Microsoft product line with no cross-platform future. RdlCore's mission is to turn that engine into a real cross-platform reporting **platform**: one that runs in Linux containers, in cloud-native deployments, and on macOS development machines, with the same fidelity it always had on Windows — so that applications built around RDL reports don't have to choose between keeping their reports and modernizing their infrastructure.

This is an incremental effort. Each rendering engine is migrated from direct GDI+/Windows dependencies to a small set of platform-neutral interfaces (an `IImageProvider`, an `IRenderSurface`, and similar seams), with a platform-specific implementation registered behind each — Windows keeps its original GDI+ path unchanged, while Linux and macOS get a SkiaSharp-, ImageSharp-, or ClosedXML-backed equivalent. Where a real architectural wall exists (a handful of Windows-only primitives with no cross-platform equivalent, documented below), we say so plainly rather than pretend it's solved.

## What works today

* RDLC file loading, parsing, and compiling
* Local and remote (Report Server / SOAP) data sources
* Parameters, expressions, and the full RDL expression language (VB-based, compiled via Roslyn)
* WinForms report preview control
* All rendering formats listed below, on Windows; the majority on Linux and macOS as well — see the support matrix
* MSChart (2D and 3D) and Gauge report items

## Supported rendering formats

| Format | Windows | Linux | macOS |
| --- | --- | --- | --- |
| PDF | Yes | Yes | Not yet tested |
| HTML5 / HTML4.0 / MHTML | Yes | Yes | Not yet tested |
| EXCELOPENXML (Excel Open XML) | Yes | Yes | Not yet tested |
| EXCEL (Excel 97/2003) | Yes | Yes | Not yet tested |
| WORDOPENXML (Word Open XML) | Yes | Yes | Not yet tested |
| WORD (Word 97/2003) | Yes | No — Windows-only OLE Structured Storage dependency | Not yet tested |
| CSV | Yes | Yes | Not yet tested |
| XML | Yes | Yes | Not yet tested |
| IMAGE (TIFF/EMF) | Yes | No — not yet started | Not yet tested |

Chart and Gauge report items render through the same cross-platform path as the rest of the engine (Skia-backed on Linux/macOS) and are usable inside any of the formats above. Map report items are Windows-only today; that migration is deliberately deferred (see `docs/decisions.md`).

For the detailed, continuously-updated breakdown — including exactly which code paths route through which backend, and precisely what's blocked and why — see [docs/platform-support.md](docs/platform-support.md).

## Known permanent limitations

A small number of gaps are architectural, not "not ported yet":

* **EMF/Metafile export** (Chart's `SaveIntoMetafile`, IMAGE format's EMF output) needs a raw Windows HDC (`Graphics.GetHdc()`) with no cross-platform equivalent.
* **WORD (binary Word 97/2003) container writing** uses real Windows COM interop (OLE Structured Storage) with no cross-platform equivalent. Use WORDOPENXML on Linux/macOS instead.
* **Expression sandboxing.** There is no isolation between report expression code and the host process — this was true of the original Reporting Services CodeDom design and remains true under Roslyn. Do not load and render reports from untrusted sources. See `tasks/expression-compiler-modernization.md` for the full reasoning.
* **Single-file (`PublishSingleFile`) deployment** is not currently supported — the Roslyn expression compiler needs on-disk assembly references at runtime. Partially addressed; see `tasks/expression-compiler-modernization.md` for current status.
* **Spatial SQL types** (`Microsoft.SqlServer.Types`/`SqlGeography`) are .NET Framework-only and unavailable in .NET Core; reports depending on them won't load.
* **Interactive web report preview** (the WebForms-era browser preview UI) was never ported — it's tightly coupled to WebForms/ASP.NET architecture that has no ASP.NET Core equivalent. `HTML5`/`HTML4.0` rendering formats (including a no-JavaScript-required HTML5 mode) are available as a substitute.
* **WinForms control designer support** is not available. Add the `ReportViewer` control programmatically instead — see [docs/usage-guide.md](docs/usage-guide.md#6-interactive-preview-in-winforms-reportviewer-control).

## Getting started

**For step-by-step instructions and sample code** covering local reports, report-server reports, and rendering to HTML/Excel/PDF, see [docs/usage-guide.md](docs/usage-guide.md).

Reference either package depending on your application type:

| Scenario | Package | Namespace |
| --- | --- | --- |
| ASP.NET Core, console apps, services, headless rendering | `RdlCore.NETCore` | `Microsoft.Reporting.NETCore` |
| WinForms desktop app with interactive preview | `RdlCore.WinForms` | `Microsoft.Reporting.WinForms` |

Assembly and namespace names are unchanged from the upstream `ReportViewerCore` project on purpose, so existing applications can move to RdlCore as a drop-in replacement without code changes — only the NuGet package IDs and repository branding move to `RdlCore`.

### Designing reports

Visual Studio doesn't include Report Designer by default. Install Microsoft's **[RDLC Report Designer](https://marketplace.visualstudio.com/items?itemName=ProBITools.MicrosoftRdlcReportDesignerforVisualStudio-18001)** extension (VS2019) or **[RDLC Report Designer 2022](https://marketplace.visualstudio.com/items?itemName=ProBITools.MicrosoftRdlcReportDesignerforVisualStudio2022)** (VS2022).

The dataset wizard won't discover classes from a .NET Core/.NET project (and `.datasource` files aren't supported), so add a hand-built or generated `.xsd` describing the types you want to bind to your reports:

```csharp
var types = new[] { typeof(ReportItemClass1), typeof(ReportItemClass2), typeof(ReportItemClass3) };
var xri = new System.Xml.Serialization.XmlReflectionImporter();
var xss = new System.Xml.Serialization.XmlSchemas();
var xse = new System.Xml.Serialization.XmlSchemaExporter(xss);
foreach (var type in types)
{
    var xtm = xri.ImportTypeMapping(type);
    xse.ExportTypeMapping(xtm);
}
using var sw = new System.IO.StreamWriter("ReportItemSchemas.xsd", false, Encoding.UTF8);
for (int i = 0; i < xss.Count; i++)
{
    var xs = xss[i];
    xs.Id = "ReportItemSchemas";
    xs.Write(sw);
}
```

After adding `ReportItemSchemas.xsd` to your project, Report Designer will offer a new datasource called `ReportItemSchemas` you can use when building datasets.

### Running on Linux/macOS

Cross-platform rendering (PDF, HTML, Excel, Word Open XML, CSV, XML, Chart, Gauge) works natively — no Wine, no Windows compatibility shims. Just reference `Microsoft.ReportViewer.NETCore` and run.

If you also need the small subset of formats still gated to Windows (binary WORD, IMAGE/TIFF/EMF), you'll need to run on an actual Windows host or container until those are ported — see the limitations above.

## Architecture

RdlCore's cross-platform work follows a Ports & Adapters pattern: a small interface for each Windows-coupled contract (image decoding, 2D drawing surfaces, font metrics), one adapter backed by the original GDI+/Windows implementation, and one backed by a portable library (SkiaSharp, ImageSharp, ClosedXML, PdfSharpCore, HarfBuzz). A factory selects the right adapter at runtime based on the current OS.

* [docs/rendering-abstractions.md](docs/rendering-abstractions.md) — renderer interfaces and the Chart/Gauge Ports & Adapters design
* [docs/architecture-map.md](docs/architecture-map.md) — end-to-end render flow
* [docs/platform-support.md](docs/platform-support.md) — current Windows/Linux/macOS support matrix and known gaps
* [docs/decisions.md](docs/decisions.md) — architecture decisions and why
* [docs/coding-standards.md](docs/coding-standards.md) — engineering conventions and migration lessons learned
* [docs/renderer-extension-guide.md](docs/renderer-extension-guide.md) — how to add another renderer implementation
* [docs/troubleshooting.md](docs/troubleshooting.md) / [docs/build-and-test.md](docs/build-and-test.md) / [docs/examples.md](docs/examples.md) — supporting reference docs

`TODO.md` tracks current priorities and links every active task; `tasks/*.md` files hold the working detail for anything still in progress.

## Reporting bugs

Before filing an issue, please confirm the problem is specific to this package — i.e. it doesn't reproduce against the original Microsoft ReportViewer control, if you have a way to check. Include the full exception stack trace and, where possible, a minimal `.rdlc` or sample project that reproduces it.

If you hit `Version conflict detected for "Microsoft.CodeAnalysis.Common"` when adding this package: add `Microsoft.CodeAnalysis.CSharp.Workspaces`/`Microsoft.CodeAnalysis.Common` yourself first, pinned to a version matching your target framework (3.6.0 for .NET Core 3.1, 3.8.0 for .NET 5, 4.0.1 for .NET 6+).

## Provenance

Source code originates from decompiling Microsoft Report Viewer for WinForms (version 15.0.1404.0, via ILSpy) — Reporting Services' original client-side rendering engine. The original CodeDom/System.CodeDom Visual Basic compilation (unavailable on .NET Core) has been replaced with the Roslyn Visual Basic compiler; references to .NET Framework-only assemblies unavailable on .NET Core (e.g. `Microsoft.SqlServer.Types`) have been removed along with the functionality that depended on them. Source formatting is intentionally left as ILSpy produced it, rather than reformatted, to keep diffs against the original decompilation meaningful.

`Microsoft.ReportViewer.WinForms` is close to a one-to-one recompilation of the original WinForms ReportViewer. `Microsoft.ReportViewer.NETCore` is a heavily stripped-down variant suitable for web applications, web services, and batch processing, with no WinForms UI dependency.

## License

**No open-source license is granted over the Microsoft-derived portions of this codebase, because this project does not hold copyright over them.** The core rendering engine — the RDL processing pipeline, the WinForms/NETCore report objects, and the Excel/PDF/Word/Chart/Gauge renderers inherited from the original decompilation — is a derivative work of Microsoft's proprietary Report Viewer for WinForms. Reporting Services is a free-to-use Microsoft product, but "free to use" is not the same as "licensed for redistribution of modified derivative works," and Microsoft has not published terms that clearly permit it. Decompiling it for local, personal compatibility purposes may be legal depending on your jurisdiction; redistributing a modified version — which is what using this repository necessarily involves — is a separate question this project cannot answer on your behalf. Applying an MIT/Apache/GPL-style license header to this code would not change that; it would only misrepresent that such rights exist.

The parts of this repository written directly by its contributors and not derived from Microsoft's decompiled source — the cross-platform rendering adapters (Skia/ImageSharp/ClosedXML/PdfSharpCore backends), the associated interfaces and factories, tests, and documentation — are original work, but are layered on top of and depend on the Microsoft-derived core described above, so they cannot be extracted and used independently under a separate license in any way that matters in practice.

**Use this repository at your own risk.** If you plan to redistribute binaries built from it, embed it in a commercial product, or otherwise need legal certainty about your rights to do so, consult your own legal counsel — this document is not a substitute for that.
