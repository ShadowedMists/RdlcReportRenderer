# IMAGE (TIFF/EMF) renderer: cross-platform support

**Status: NOT STARTED, not scheduled.** Identified as a genuine gap in `docs/decisions.md`'s "PDF path vs. Image-renderer path" clarification (2026-07-24) but never given its own tracked task until now (2026-07-27).

## What this covers

`Microsoft.ReportingServices.Rendering.ImageRenderer/ImageWriter.cs` — the renderer behind the `IMAGE` format (TIFF/EMF export), used by `LocalReport.Render("IMAGE", ...)`. This is a **separate rendering extension from PDF**, despite living in the same `ImageRenderer` project folder and sharing the `Renderer`/`RichText` pagination pipeline PDF uses. `ImageWriter` draws through real `System.Drawing.Graphics`/`Pen`/`Brush`/`GraphicsPath` calls and `MetafileGraphics.cs`'s raw HDC/`Metafile` handling — this is exactly the GDI+/Metafile stack that PDF's own analysis (`docs/decisions.md`) confirmed PDF itself never touches. IMAGE does touch it, directly and unavoidably for EMF specifically (`Graphics.GetHdc()` has no cross-platform equivalent — same wall already called out as permanent for Chart's `SaveIntoMetafile` in `docs/platform-support.md`).

## Current gap

- Entirely GDI+-coupled: `System.Drawing.Image`/`Bitmap`, `MetafileGraphics`, `GDIPen`/`GDIBrush`, `RectangleF`, `Metafile`. No cross-platform work has started.
- TIFF output (a real bitmap format, not EMF's vector/metafile format) is in principle portable to a Skia-backed path the same way Chart's `ChartImage.SaveImage` now is — but this hasn't been scoped or attempted.
- EMF output has no portable equivalent at all — same permanent architectural wall as Chart's Metafile export (`docs/platform-support.md`'s "Known permanent/architectural gaps" list). Any future work here should split TIFF (portable, worth doing) from EMF (permanent Windows-only, guard rather than port) rather than treating IMAGE as one indivisible format.
- **No test coverage anywhere in `tests/`** — confirmed 2026-07-27. Whatever exists today only works on Windows and isn't verified even there by an automated test.

**Empirically confirmed under WSL (2026-07-27, via a throwaway scratch test against `LocalReport.Render("IMAGE", "<DeviceInfo><OutputFormat>TIFF</OutputFormat></DeviceInfo>")`, not committed):** the failure is even more immediate than "GDI+ can't construct a `Bitmap`" — it happens at the very first page, before any drawing or text: `ImageWriter.BeginPage` → `Graphics.NewPage` calls `Win32.GetDC(IntPtr hWnd)`, a raw Win32 P/Invoke (`user32.dll`) used to query device DPI, and throws `DllNotFoundException` immediately on Linux. This means any future TIFF-via-Skia port needs to replace `Graphics.NewPage`'s DPI-query mechanism specifically (not just the drawing/encode calls further down the pipeline) as its very first step — a Skia-backed `IRenderSurface` wouldn't need a real HDC for this at all, but whatever replaces `Graphics.cs` here has to stop calling `GetDC` unconditionally on every new page.

## Related upstream signal

Upstream `lkosson/reportviewercore` issue #42 ("PDF & Image generation fails on Windows Nano Server Docker container", open) reports the *same underlying dependency* — GDI+ and Uniscribe being unavailable — failing even on a **Windows** host (Nano Server is headless Windows with no GDI+/Uniscribe). This confirms the IMAGE/EMF renderer's GDI+ dependency is a real production concern beyond just "Linux support" — worth linking here since a future TIFF-via-Skia path would fix both the Linux gap and the headless-Windows-container gap in one move, unlike PDF's fix (which only had to address non-Windows, since PDF was already routed off Uniscribe's Windows-only shape/place split).

## Proposed tasks

1. Scope TIFF and EMF separately — do not assume one fix covers both.
2. For TIFF: investigate whether `IRenderSurface`/Skia (already proven for Chart's `ChartImage.SaveImage`) can back `ImageWriter`'s bitmap output, or whether this needs its own abstraction given `ImageWriter`'s different call shape.
3. For EMF: document as a permanent Windows-only limitation (mirroring Chart's `SaveIntoMetafile` wording in `docs/platform-support.md`) rather than scheduling porting work with no viable target.
4. Add end-to-end RDL render tests once any part of this is fixed — none exist today even for the Windows-only baseline.
5. Update `docs/platform-support.md`'s support matrix and `docs/decisions.md` once scoped/resolved.
