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
    }
}
