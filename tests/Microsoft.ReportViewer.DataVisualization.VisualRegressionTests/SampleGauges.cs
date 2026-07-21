using System.Drawing;
using System.IO;
using Microsoft.Reporting.Gauge.WebForms;

namespace Microsoft.ReportViewer.DataVisualization.VisualRegressionTests
{
    /// <summary>
    /// Builds small, self-contained <see cref="GaugeContainer"/> instances directly against the
    /// internal gauge engine API (no .rdlc report or host required) and rasterizes them to PNG,
    /// so a refactor of the GDI+ call sites underneath can be checked against a pixel baseline.
    /// Mirrors <see cref="SampleCharts"/> for the Chart engine
    /// (see tasks/gauge-gdi-type-abstraction.md).
    /// </summary>
    internal static class SampleGauges
    {
        internal static byte[] RenderSimpleCircularGauge()
        {
            using var container = new GaugeContainer();
            container.Width = 300;
            container.Height = 300;

            var circularGauge = new CircularGauge();
            container.CircularGauges.Add(circularGauge);

            var scale = new CircularScale();
            circularGauge.Scales.Add(scale);

            var pointer = new CircularPointer();
            pointer.Value = 65;
            circularGauge.Pointers.Add(pointer);

            using var stream = new MemoryStream();
            container.SaveAsImage(stream);
            return stream.ToArray();
        }

        internal static byte[] RenderSimpleLinearGauge()
        {
            using var container = new GaugeContainer();
            container.Width = 300;
            container.Height = 100;

            var linearGauge = new LinearGauge();
            linearGauge.Orientation = GaugeOrientation.Horizontal;
            container.LinearGauges.Add(linearGauge);

            var scale = new LinearScale();
            linearGauge.Scales.Add(scale);

            var pointer = new LinearPointer();
            pointer.Value = 65;
            linearGauge.Pointers.Add(pointer);

            using var stream = new MemoryStream();
            container.SaveAsImage(stream);
            return stream.ToArray();
        }

        /// <summary>
        /// Exercises <c>BackFrame.DrawFrameImage</c> end to end (see tasks/gauge-gdi-type-abstraction.md
        /// Milestone A4) — a frame image with clip-to-path enabled (the <see cref="IGaugeClipRegion"/>
        /// clip-swap), a transparent colour key (<c>IImageDrawOptions.SetTransparentColor</c>), and a hue
        /// recolor (<c>IImageDrawOptions.SetChannelScale</c>), all in one render. Not previously exercised
        /// by any sample gauge — neither <see cref="RenderSimpleCircularGauge"/> nor
        /// <see cref="RenderSimpleLinearGauge"/> sets a frame image.
        /// </summary>
        internal static byte[] RenderCircularGaugeWithFrameImage()
        {
            using var container = new GaugeContainer();
            container.Width = 300;
            container.Height = 300;

            using var frameImage = new Bitmap(64, 64);
            using (Graphics imageGraphics = Graphics.FromImage(frameImage))
            {
                imageGraphics.Clear(Color.CornflowerBlue);
                imageGraphics.FillEllipse(Brushes.White, 8, 8, 48, 48);
            }
            container.NamedImages.Add(new NamedImage("frameImage", frameImage));

            var circularGauge = new CircularGauge();
            container.CircularGauges.Add(circularGauge);
            circularGauge.BackFrame.Image = "frameImage";
            circularGauge.BackFrame.ImageTransColor = Color.CornflowerBlue;
            circularGauge.BackFrame.ImageHueColor = Color.Firebrick;
            circularGauge.BackFrame.ClipImage = true;

            var scale = new CircularScale();
            circularGauge.Scales.Add(scale);

            var pointer = new CircularPointer();
            pointer.Value = 65;
            circularGauge.Pointers.Add(pointer);

            using var stream = new MemoryStream();
            container.SaveAsImage(stream);
            return stream.ToArray();
        }
    }
}
