# Windows Dependencies in ReportViewerCore

## Key Dependencies Found
- **UI Frameworks:** `System.Drawing` (25+ files), `System.Windows.Forms` (ReportViewer.cs, ResizableToolStripPanel.cs), `System.Runtime.InteropServices.ComTypes`
- **Windows-Specific APIs:** `System.Security.Principal`, `Environment.GetEnvironmentVariable("USERPROFILE"), `Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE"), "...")`

## Impact on Linux
- `System.Drawing` and `System.Windows.Forms` are not supported on Linux
- `USERPROFILE` handling is Windows-specific

## Additional Findings
- `System.Drawing` and `System.Windows.Forms` are critical for rendering but lack Linux support
- `Environment.GetEnvironmentVariable("USERPROFILE")` is Windows-specific and needs platform-agnostic replacement
- COM interop (`ComTypes`) requires alternative for Linux compatibility

## Next Steps
1. Document these findings in `investigation/02-windows-dependencies.md`
2. Plan replacements for:
   - `System.Drawing` → Cross-platform library (e.g., SkiaSharp)
   - `System.Windows.Forms` → Linux-compatible UI framework
   - `USERPROFILE` handling → Platform-agnostic user configuration system