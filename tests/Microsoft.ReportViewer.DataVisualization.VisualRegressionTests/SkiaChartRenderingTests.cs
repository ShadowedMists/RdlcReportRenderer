using System.IO;
using Microsoft.Reporting.Chart.WebForms;
using Microsoft.Reporting.Chart.WebForms.Rendering.Skia;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ReportViewer.DataVisualization.VisualRegressionTests
{
    /// <summary>
    /// Milestone E2 (2026-07-23) — renders real <see cref="Chart"/> object-model scenes (the same
    /// ones <see cref="SampleCharts"/>'s <c>Build*</c> methods feed the GDI+ regression suite)
    /// end to end through the Skia backend, against committed Skia-rendered baselines. Bypasses
    /// <see cref="ChartImage.GetImage"/>/<see cref="ChartImage.SaveImage"/> entirely (both hard-downcast
    /// to <c>GdiRenderSurface</c> — see chart-gdi-type-abstraction.md's E2 notes) by driving
    /// <c>ChartPicture.Paint(IRenderSurface, bool)</c> directly with a <see cref="SkiaRenderSurface"/>
    /// after selecting <see cref="RenderingType.Skia"/> on the chart's <c>chartGraph</c>.
    ///
    /// Deliberately scoped to the 2D scenes only — every 3D scene (Enable3D or a 3D-only chart type)
    /// is skipped here since Chart's 3D subsystem (Milestone D3) is permanently blocked by design; see
    /// chart-gdi-type-abstraction.md. Gradient/hatch-heavy scenes render (E1 covers every brush kind
    /// for real) but are Skia-vs-its-own-baseline only, not GDI+-vs-Skia parity — text/gradient
    /// rasterization differs between backends regardless of how faithful the port is.
    /// </summary>
    [TestClass]
    public class SkiaChartRenderingTests
    {
        private static byte[] RenderViaSkia(Chart chart)
        {
            // ChartGraphics.resourceFactory is fixed at construction (readonly), so selecting the
            // Skia backend means swapping the whole ChartGraphics instance, not just
            // ActiveRenderingType — otherwise brush/font/path resources built via the (still-Gdi)
            // resourceFactory would be fed into Skia's IChartRenderingEngine members and throw on
            // the first cross-backend cast.
            chart.chartPicture.chartGraph = new ChartGraphics(chart.chartPicture.common, new SkiaResourceFactory())
            {
                ActiveRenderingType = RenderingType.Skia,
            };
            using var renderSurface = new SkiaRenderSurface(chart.Width, chart.Height, 96f);
            chart.chartPicture.Paint(renderSurface, paintTopLevelElementOnly: false);

            using var stream = new MemoryStream();
            renderSurface.Encode(stream, ChartImageFormat.Png);
            return stream.ToArray();
        }

        private static void AssertMatchesBaseline(Chart chart, string baselineFileName)
        {
            using (chart)
            {
                var actual = RenderViaSkia(chart);
                var result = ImageComparer.CompareToBaseline(actual, baselineFileName);
                Assert.IsTrue(result.Matches, result.Message);
            }
        }

        [TestMethod]
        public void SimpleBarChart_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildSimpleBarChart(), "SimpleBarChart.Skia.png");

        [TestMethod]
        public void SimpleLineChart_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildSimpleLineChart(), "SimpleLineChart.Skia.png");

        [TestMethod]
        public void RotatedLabelsChart_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildRotatedLabelsChart(), "RotatedLabelsChart.Skia.png");

        [TestMethod]
        public void EmbossBorderChart_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildEmbossBorderChart(), "EmbossBorderChart.Skia.png");

        [TestMethod]
        public void SunkenBorderChart_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildSunkenBorderChart(), "SunkenBorderChart.Skia.png");

        [TestMethod]
        public void AreaChartWithShadow_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildAreaChartWithShadow(), "AreaChartWithShadow.Skia.png");

        [TestMethod]
        public void StockChartWithTriangleMarks_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildStockChartWithTriangleMarks(), "StockChartWithTriangleMarks.Skia.png");

        [TestMethod]
        public void FastPointChartWithMarkers_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildFastPointChartWithMarkers(), "FastPointChartWithMarkers.Skia.png");

        [TestMethod]
        public void FastLineChart_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildFastLineChart(), "FastLineChart.Skia.png");

        [TestMethod]
        public void LineChartWithShadow_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildLineChartWithShadow(), "LineChartWithShadow.Skia.png");

        [TestMethod]
        public void PointChartWithLabels_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildPointChartWithLabels(), "PointChartWithLabels.Skia.png");

        [TestMethod]
        public void BarChartWithLabels_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildBarChartWithLabels(), "BarChartWithLabels.Skia.png");

        [TestMethod]
        public void ErrorBarChartWithLabels_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildErrorBarChartWithLabels(), "ErrorBarChartWithLabels.Skia.png");

        [TestMethod]
        public void BoxPlotChartWithLabels_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildBoxPlotChartWithLabels(), "BoxPlotChartWithLabels.Skia.png");

        [TestMethod]
        public void StockChartWithLabels_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildStockChartWithLabels(), "StockChartWithLabels.Skia.png");

        [TestMethod]
        public void TreeMapChartWithLabels_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildTreeMapChartWithLabels(), "TreeMapChartWithLabels.Skia.png");

        [TestMethod]
        public void RangeChartWithShadow_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildRangeChartWithShadow(), "RangeChartWithShadow.Skia.png");

        [TestMethod]
        public void RangeChartWithHatch_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildRangeChartWithHatch(), "RangeChartWithHatch.Skia.png");

        [TestMethod]
        public void RangeChartWithGradient_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildRangeChartWithGradient(), "RangeChartWithGradient.Skia.png");

        [TestMethod]
        public void CalloutRoundedRectangle_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildCalloutRoundedRectangle(), "CalloutRoundedRectangle.Skia.png");

        [TestMethod]
        public void CalloutEllipse_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildCalloutEllipse(), "CalloutEllipse.Skia.png");

        [TestMethod]
        public void CalloutRectangle_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildCalloutRectangle(), "CalloutRectangle.Skia.png");

        [TestMethod]
        public void CalloutCloud_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildCalloutCloud(), "CalloutCloud.Skia.png");

        [TestMethod]
        public void CalloutPerspective_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildCalloutPerspective(), "CalloutPerspective.Skia.png");

        [TestMethod]
        public void CalloutBorderline_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildCalloutBorderline(), "CalloutBorderline.Skia.png");

        [TestMethod]
        public void CalloutSimpleLine_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildCalloutSimpleLine(), "CalloutSimpleLine.Skia.png");

        [TestMethod]
        public void ChartWithAxisTitles_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildChartWithAxisTitles(), "ChartWithAxisTitles.Skia.png");

        [TestMethod]
        public void TextAnnotationDefault_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildTextAnnotationDefault(), "TextAnnotationDefault.Skia.png");

        [TestMethod]
        public void TextAnnotationFrame_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildTextAnnotationFrame(), "TextAnnotationFrame.Skia.png");

        [TestMethod]
        public void TextAnnotationEmbed_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildTextAnnotationEmbed(), "TextAnnotationEmbed.Skia.png");

        [TestMethod]
        public void TextAnnotationEmboss_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildTextAnnotationEmboss(), "TextAnnotationEmboss.Skia.png");

        [TestMethod]
        public void TextAnnotationShadow_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildTextAnnotationShadow(), "TextAnnotationShadow.Skia.png");

        [TestMethod]
        public void TextAnnotationEllipse_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildTextAnnotationEllipse(), "TextAnnotationEllipse.Skia.png");

        [TestMethod]
        public void RadarChartWithAxisLabels_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildRadarChartWithAxisLabels(), "RadarChartWithAxisLabels.Skia.png");

        [TestMethod]
        public void StripLineWithTitle_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildStripLineWithTitle(), "StripLineWithTitle.Skia.png");

        [TestMethod]
        public void TitleFrame_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildTitleFrame(), "TitleFrame.Skia.png");

        [TestMethod]
        public void TitleEmbed_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildTitleEmbed(), "TitleEmbed.Skia.png");

        [TestMethod]
        public void TitleEmboss_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildTitleEmboss(), "TitleEmboss.Skia.png");

        [TestMethod]
        public void TitleShadow_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildTitleShadow(), "TitleShadow.Skia.png");

        [TestMethod]
        public void LegendWithTitleAndHeader_RendersViaSkia_MatchesBaseline() =>
            AssertMatchesBaseline(SampleCharts.BuildLegendWithTitleAndHeader(), "LegendWithTitleAndHeader.Skia.png");
    }
}
