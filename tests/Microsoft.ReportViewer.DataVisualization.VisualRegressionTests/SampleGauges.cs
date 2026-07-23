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

        /// <summary>
        /// Exercises <c>DigitalSegment.cs</c>/<c>SegmentsCache.cs</c> end to end (see
        /// tasks/gauge-gdi-type-abstraction.md Milestone B2 open item #7) — a
        /// <see cref="NumericIndicatorStyle.Digital7Segment"/> indicator with a non-empty value (real
        /// per-digit segment geometry via <c>DigitalSegment.GetSymbol7</c>) rendered twice in the same
        /// frame so the same segment shape is drawn from a cache hit as well as a cache miss
        /// (<c>SegmentsCache.GetSegment</c>/<c>SetSegment</c>), plus the LED-dim "blank" segment path
        /// (<c>DigitalSegment.GetOrientedSegments</c>) via its <see cref="NumericIndicator.LedDimColor"/>
        /// branch. Not previously exercised by any sample gauge — none of the other three set
        /// <see cref="NumericIndicator.IndicatorStyle"/> away from its <c>Mechanical</c> default.
        /// </summary>
        internal static byte[] RenderDigital7SegmentNumericIndicator()
        {
            using var container = new GaugeContainer();
            container.Width = 300;
            container.Height = 150;

            var circularGauge = new CircularGauge();
            container.CircularGauges.Add(circularGauge);

            var scale = new CircularScale();
            circularGauge.Scales.Add(scale);

            var pointer = new CircularPointer();
            pointer.Value = 65;
            circularGauge.Pointers.Add(pointer);

            var indicator = container.NumericIndicators.Add("digital7");
            indicator.IndicatorStyle = NumericIndicatorStyle.Digital7Segment;
            indicator.LedDimColor = Color.FromArgb(40, 40, 40);
            indicator.Digits = 3;
            indicator.Decimals = 1;
            indicator.Value = 42.5;
            indicator.Location.X = 10f;
            indicator.Location.Y = 40f;
            indicator.Size.Width = 80f;
            indicator.Size.Height = 20f;

            var indicator2 = container.NumericIndicators.Add("digital7b");
            indicator2.IndicatorStyle = NumericIndicatorStyle.Digital7Segment;
            indicator2.LedDimColor = Color.FromArgb(40, 40, 40);
            indicator2.Digits = 3;
            indicator2.Decimals = 1;
            indicator2.Value = 42.5;
            indicator2.Location.X = 10f;
            indicator2.Location.Y = 65f;
            indicator2.Size.Width = 80f;
            indicator2.Size.Height = 20f;

            using var stream = new MemoryStream();
            container.SaveAsImage(stream);
            return stream.ToArray();
        }
    }
}
