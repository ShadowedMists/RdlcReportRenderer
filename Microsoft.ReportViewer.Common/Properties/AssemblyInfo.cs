using System;
using System.Diagnostics;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: AssemblyTitle("Microsoft.ReportViewer.Common.dll")]
[assembly: AssemblyDescription("Microsoft.ReportViewer.Common.dll")]
[assembly: AssemblyDefaultAlias("Microsoft.ReportViewer.Common.dll")]
[assembly: NeutralResourcesLanguage("en-US")]
[assembly: AssemblyConfiguration("")]
[assembly: InternalsVisibleTo("Microsoft.ReportViewer.NETCore, PublicKey=002400000480000094000000060200000024000052534131000400000100010069609cc6d0356bd6c27f95dfe5b65067c6536cfdfdd9a9d598cb0d599d0a9cbe6ab27977d7f7d1da1b9ce2ead9cc430e978697b83ff41efbcfd0c02550844eae30c48dd243164c62faf4435d324ce033b1995648db13f97d6dac558637bcd9c0cf9c2d54b04bb14b53e8d4ab4e3ca31cd98449a6013c5022dc9b6e44e5ad67ca")]
[assembly: InternalsVisibleTo("Microsoft.ReportViewer.WinForms, PublicKey=002400000480000094000000060200000024000052534131000400000100010069609cc6d0356bd6c27f95dfe5b65067c6536cfdfdd9a9d598cb0d599d0a9cbe6ab27977d7f7d1da1b9ce2ead9cc430e978697b83ff41efbcfd0c02550844eae30c48dd243164c62faf4435d324ce033b1995648db13f97d6dac558637bcd9c0cf9c2d54b04bb14b53e8d4ab4e3ca31cd98449a6013c5022dc9b6e44e5ad67ca")]
// Test-only grant: Microsoft.ReportViewer.Chart.Rdl.Tests is signed with the same
// RdlCore.snk key as this assembly (see its .csproj), so this introduces no
// new trust boundary - it lets tests exercise internal, platform-gated rendering paths
// (e.g. PDFWriter/Renderer's cross-platform text code) that have no public surface.
[assembly: InternalsVisibleTo("Microsoft.ReportViewer.Chart.Rdl.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010069609cc6d0356bd6c27f95dfe5b65067c6536cfdfdd9a9d598cb0d599d0a9cbe6ab27977d7f7d1da1b9ce2ead9cc430e978697b83ff41efbcfd0c02550844eae30c48dd243164c62faf4435d324ce033b1995648db13f97d6dac558637bcd9c0cf9c2d54b04bb14b53e8d4ab4e3ca31cd98449a6013c5022dc9b6e44e5ad67ca")]
[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]
[assembly: AssemblyProduct("Microsoft SQL Server")]
[assembly: AssemblyCopyright("Microsoft. All rights reserved.")]
[assembly: AssemblyTrademark("Microsoft SQL Server is a registered trademark of Microsoft Corporation.")]
[assembly: AssemblyCompany("Microsoft Corporation")]
[assembly: AssemblyFileVersion("15.0.1404.0")]
[assembly: AssemblyInformationalVersion("15.0.1404.0")]
[assembly: AssemblyVersion("15.0.0.0")]
[module: UnverifiableCode]
