using System.IO;
using Microsoft.Reporting.Chart.WebForms;
using Microsoft.Reporting.Chart.WebForms.Rendering.Skia;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ReportViewer.DataVisualization.VisualRegressionTests
{
    /// <summary>
    /// Milestone E2 (2026-07-23) — the first test to render a real <see cref="Chart"/> object-model
    /// scene (not a hand-authored <see cref="SpikeScene"/>) end to end through the Skia backend, against
    /// a committed Skia-rendered baseline. Bypasses <see cref="ChartImage.GetImage"/>/<see cref="ChartImage.SaveImage"/>
    /// entirely (both hard-downcast to <c>GdiRenderSurface</c> — see chart-gdi-type-abstraction.md's E2
    /// notes) by driving <c>ChartPicture.Paint(IRenderSurface, bool)</c> directly with a
    /// <see cref="SkiaRenderSurface"/> after selecting <see cref="RenderingType.Skia"/> on the chart's
    /// <c>chartGraph</c>. Getting this one scene rendering surfaced several genuinely-reachable-but-still-
    /// throwing <c>SkiaChartGraphics</c>/<c>SkiaGraphicsPath</c> members (<c>SmoothingMode</c>,
    /// <c>TextRenderingHint</c>, <c>Graphics</c>, <c>GetDpiX</c>, <c>MeasureString(string, Font, ...)</c>,
    /// <c>PathTypes</c>) plus one un-bridged concrete call site (<c>Label.cs</c>'s second
    /// <c>DrawLabelStringRel</c> caller) — all fixed alongside this test, see chart-gdi-type-abstraction.md's
    /// E2 notes for detail on each. Deliberately scoped to exactly one scene chosen as already fully
    /// E1-converted (no 3D, no gradients/hatches) — not yet a sweep over every <see cref="SampleCharts"/>
    /// scene (several of those are expected to hit the still-unconverted 3D subsystem or other residual
    /// gaps and need separate triage).
    /// </summary>
    [TestClass]
    public class SkiaChartRenderingTests
    {
        [TestMethod]
        public void SimpleBarChart_RendersViaSkia_MatchesBaseline()
        {
            using var chart = new Chart();
            chart.Width = 400;
            chart.Height = 300;

            chart.ChartAreas.Add("Default");

            var series = chart.Series.Add("Sales");
            series.ChartType = SeriesChartType.Bar;
            series.Points.AddXY("Q1", 12);
            series.Points.AddXY("Q2", 18);
            series.Points.AddXY("Q3", 9);
            series.Points.AddXY("Q4", 24);

            // ChartGraphics.resourceFactory is fixed at construction (readonly), so selecting the Skia
            // backend means swapping the whole ChartGraphics instance, not just ActiveRenderingType —
            // otherwise brush/font/path resources built via the (still-Gdi) resourceFactory would be
            // fed into Skia's IChartRenderingEngine members and throw on the first cross-backend cast.
            chart.chartPicture.chartGraph = new ChartGraphics(chart.chartPicture.common, new SkiaResourceFactory())
            {
                ActiveRenderingType = RenderingType.Skia,
            };
            using var renderSurface = new SkiaRenderSurface(chart.Width, chart.Height, 96f);
            chart.chartPicture.Paint(renderSurface, paintTopLevelElementOnly: false);

            using var stream = new MemoryStream();
            renderSurface.Encode(stream, ChartImageFormat.Png);
            var actual = stream.ToArray();

            var result = ImageComparer.CompareToBaseline(actual, "SimpleBarChart.Skia.png");
            Assert.IsTrue(result.Matches, result.Message);
        }
    }
}
