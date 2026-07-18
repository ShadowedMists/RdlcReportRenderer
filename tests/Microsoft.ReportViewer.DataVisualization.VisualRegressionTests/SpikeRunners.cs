using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Microsoft.Reporting.Chart.WebForms;
using Microsoft.Reporting.Chart.WebForms.Rendering.Gdi;
using Microsoft.Reporting.Chart.WebForms.Rendering.Skia;
using SkiaSharp;

namespace Microsoft.ReportViewer.DataVisualization.VisualRegressionTests
{
    /// <summary>
    /// Phase 0 spike — drives <see cref="SpikeScene"/> through each backend. Proves the same
    /// backend-agnostic paint routine runs against both the existing GDI+ adapters (Windows
    /// only — GDI+ itself cannot initialize on this Linux environment, see spike report) and
    /// the new Skia adapters (Windows and Linux).
    /// </summary>
    internal static class SpikeRunners
    {
        internal static byte[] RenderViaGdi()
        {
            using var bitmap = new Bitmap(SpikeScene.Width, SpikeScene.Height);
            using var graphics = Graphics.FromImage(bitmap);
            var engine = new GdiGraphics { Graphics = graphics };
            var factory = new GdiResourceFactory();

            SpikeScene.Paint(engine, factory);

            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            return stream.ToArray();
        }

        internal static byte[] RenderViaSkia()
        {
            var info = new SKImageInfo(SpikeScene.Width, SpikeScene.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var surface = SKSurface.Create(info);
            surface.Canvas.Clear(SKColors.White);

            var engine = new SkiaChartGraphics { Canvas = surface.Canvas };
            var factory = new SkiaResourceFactory();

            SpikeScene.Paint(engine, factory);

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }
    }
}
