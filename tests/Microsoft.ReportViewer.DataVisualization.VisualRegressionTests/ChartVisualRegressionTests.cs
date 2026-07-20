using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ReportViewer.DataVisualization.VisualRegressionTests
{
    /// <summary>
    /// Pixel baseline coverage for the chart engine. These tests exist so that the GDI+ ->
    /// backend-agnostic rendering migration (see tasks/chart-gdi-type-abstraction.md) can be
    /// checked for byte-for-byte-equivalent output at every milestone, on top of the engine
    /// still compiling and running.
    /// </summary>
    [TestClass]
    public class ChartVisualRegressionTests
    {
        [TestMethod]
        public void SimpleBarChart_MatchesBaseline()
        {
            var actual = SampleCharts.RenderSimpleBarChart();
            var result = ImageComparer.CompareToBaseline(actual, "SimpleBarChart.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void SimpleLineChart_MatchesBaseline()
        {
            var actual = SampleCharts.RenderSimpleLineChart();
            var result = ImageComparer.CompareToBaseline(actual, "SimpleLineChart.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void RotatedLabelsChart_MatchesBaseline()
        {
            var actual = SampleCharts.RenderRotatedLabelsChart();
            var result = ImageComparer.CompareToBaseline(actual, "RotatedLabelsChart.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void Pie3DChart_MatchesBaseline()
        {
            var actual = SampleCharts.RenderPie3DChart();
            var result = ImageComparer.CompareToBaseline(actual, "Pie3DChart.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void Doughnut3DChart_MatchesBaseline()
        {
            var actual = SampleCharts.RenderDoughnut3DChart();
            var result = ImageComparer.CompareToBaseline(actual, "Doughnut3DChart.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void EmbossBorderChart_MatchesBaseline()
        {
            var actual = SampleCharts.RenderEmbossBorderChart();
            var result = ImageComparer.CompareToBaseline(actual, "EmbossBorderChart.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void SunkenBorderChart_MatchesBaseline()
        {
            var actual = SampleCharts.RenderSunkenBorderChart();
            var result = ImageComparer.CompareToBaseline(actual, "SunkenBorderChart.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void AreaChartWithShadow_MatchesBaseline()
        {
            var actual = SampleCharts.RenderAreaChartWithShadow();
            var result = ImageComparer.CompareToBaseline(actual, "AreaChartWithShadow.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void StockChartWithTriangleMarks_MatchesBaseline()
        {
            var actual = SampleCharts.RenderStockChartWithTriangleMarks();
            var result = ImageComparer.CompareToBaseline(actual, "StockChartWithTriangleMarks.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void FastPointChartWithMarkers_MatchesBaseline()
        {
            var actual = SampleCharts.RenderFastPointChartWithMarkers();
            var result = ImageComparer.CompareToBaseline(actual, "FastPointChartWithMarkers.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void FastLineChart_MatchesBaseline()
        {
            var actual = SampleCharts.RenderFastLineChart();
            var result = ImageComparer.CompareToBaseline(actual, "FastLineChart.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void LineChartWithShadow_MatchesBaseline()
        {
            var actual = SampleCharts.RenderLineChartWithShadow();
            var result = ImageComparer.CompareToBaseline(actual, "LineChartWithShadow.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void PointChartWithLabels_MatchesBaseline()
        {
            var actual = SampleCharts.RenderPointChartWithLabels();
            var result = ImageComparer.CompareToBaseline(actual, "PointChartWithLabels.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void BarChartWithLabels_MatchesBaseline()
        {
            var actual = SampleCharts.RenderBarChartWithLabels();
            var result = ImageComparer.CompareToBaseline(actual, "BarChartWithLabels.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void BarChart3DWithLabels_MatchesBaseline()
        {
            var actual = SampleCharts.RenderBarChart3DWithLabels();
            var result = ImageComparer.CompareToBaseline(actual, "BarChart3DWithLabels.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void ErrorBarChartWithLabels_MatchesBaseline()
        {
            var actual = SampleCharts.RenderErrorBarChartWithLabels();
            var result = ImageComparer.CompareToBaseline(actual, "ErrorBarChartWithLabels.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void BoxPlotChartWithLabels_MatchesBaseline()
        {
            var actual = SampleCharts.RenderBoxPlotChartWithLabels();
            var result = ImageComparer.CompareToBaseline(actual, "BoxPlotChartWithLabels.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void StockChartWithLabels_MatchesBaseline()
        {
            var actual = SampleCharts.RenderStockChartWithLabels();
            var result = ImageComparer.CompareToBaseline(actual, "StockChartWithLabels.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void TreeMapChartWithLabels_MatchesBaseline()
        {
            var actual = SampleCharts.RenderTreeMapChartWithLabels();
            var result = ImageComparer.CompareToBaseline(actual, "TreeMapChartWithLabels.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        // SunburstChart.Name has been fixed (was hardcoded "TreeMap", now correctly "Sunburst" -
        // see SunburstChart.cs:16). SunburstChart reads its hierarchy from
        // ChartArea.CategoryNodes, which is only ever populated by the RDL rendering bridge
        // (ChartMapper.RenderCategoryGrouping), not by this project's direct Chart/Series API -
        // so it has no regression test here. See
        // tests/Microsoft.ReportViewer.Chart.Rdl.Tests/SunburstChartRdlTests.cs for its real,
        // RDL-driven regression coverage.

        [TestMethod]
        public void RangeChartWithShadow_MatchesBaseline()
        {
            var actual = SampleCharts.RenderRangeChartWithShadow();
            var result = ImageComparer.CompareToBaseline(actual, "RangeChartWithShadow.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void RangeChartWithHatch_MatchesBaseline()
        {
            var actual = SampleCharts.RenderRangeChartWithHatch();
            var result = ImageComparer.CompareToBaseline(actual, "RangeChartWithHatch.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void RangeChartWithGradient_MatchesBaseline()
        {
            var actual = SampleCharts.RenderRangeChartWithGradient();
            var result = ImageComparer.CompareToBaseline(actual, "RangeChartWithGradient.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void CalloutRoundedRectangle_MatchesBaseline()
        {
            var actual = SampleCharts.RenderCalloutRoundedRectangle();
            var result = ImageComparer.CompareToBaseline(actual, "CalloutRoundedRectangle.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void CalloutEllipse_MatchesBaseline()
        {
            var actual = SampleCharts.RenderCalloutEllipse();
            var result = ImageComparer.CompareToBaseline(actual, "CalloutEllipse.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void CalloutRectangle_MatchesBaseline()
        {
            var actual = SampleCharts.RenderCalloutRectangle();
            var result = ImageComparer.CompareToBaseline(actual, "CalloutRectangle.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void CalloutCloud_MatchesBaseline()
        {
            var actual = SampleCharts.RenderCalloutCloud();
            var result = ImageComparer.CompareToBaseline(actual, "CalloutCloud.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void CalloutPerspective_MatchesBaseline()
        {
            var actual = SampleCharts.RenderCalloutPerspective();
            var result = ImageComparer.CompareToBaseline(actual, "CalloutPerspective.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void CalloutBorderline_MatchesBaseline()
        {
            var actual = SampleCharts.RenderCalloutBorderline();
            var result = ImageComparer.CompareToBaseline(actual, "CalloutBorderline.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void CalloutSimpleLine_MatchesBaseline()
        {
            var actual = SampleCharts.RenderCalloutSimpleLine();
            var result = ImageComparer.CompareToBaseline(actual, "CalloutSimpleLine.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void LineChart3D_MatchesBaseline()
        {
            var actual = SampleCharts.RenderLineChart3D();
            var result = ImageComparer.CompareToBaseline(actual, "LineChart3D.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void ChartWithAxisTitles_MatchesBaseline()
        {
            var actual = SampleCharts.RenderChartWithAxisTitles();
            var result = ImageComparer.CompareToBaseline(actual, "ChartWithAxisTitles.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void Chart3DWithAxisTitles_MatchesBaseline()
        {
            var actual = SampleCharts.RenderChart3DWithAxisTitles();
            var result = ImageComparer.CompareToBaseline(actual, "Chart3DWithAxisTitles.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void TextAnnotationDefault_MatchesBaseline()
        {
            var actual = SampleCharts.RenderTextAnnotationDefault();
            var result = ImageComparer.CompareToBaseline(actual, "TextAnnotationDefault.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void TextAnnotationFrame_MatchesBaseline()
        {
            var actual = SampleCharts.RenderTextAnnotationFrame();
            var result = ImageComparer.CompareToBaseline(actual, "TextAnnotationFrame.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void TextAnnotationEmbed_MatchesBaseline()
        {
            var actual = SampleCharts.RenderTextAnnotationEmbed();
            var result = ImageComparer.CompareToBaseline(actual, "TextAnnotationEmbed.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void TextAnnotationEmboss_MatchesBaseline()
        {
            var actual = SampleCharts.RenderTextAnnotationEmboss();
            var result = ImageComparer.CompareToBaseline(actual, "TextAnnotationEmboss.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void TextAnnotationShadow_MatchesBaseline()
        {
            var actual = SampleCharts.RenderTextAnnotationShadow();
            var result = ImageComparer.CompareToBaseline(actual, "TextAnnotationShadow.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void TextAnnotationEllipse_MatchesBaseline()
        {
            var actual = SampleCharts.RenderTextAnnotationEllipse();
            var result = ImageComparer.CompareToBaseline(actual, "TextAnnotationEllipse.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void RadarChartWithAxisLabels_MatchesBaseline()
        {
            // maxDiffPixels: 10 — Label.PaintCircular's rotated-text anti-aliasing is not
            // perfectly deterministic across process runs (confirmed against fully unmodified
            // code too; see ImageComparer.CompareToBaseline's remarks). A handful of edge pixels
            // on the rotated glyphs, not a real regression.
            var actual = SampleCharts.RenderRadarChartWithAxisLabels();
            var result = ImageComparer.CompareToBaseline(actual, "RadarChartWithAxisLabels.png", maxDiffPixels: 10);
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void StripLineWithTitle_MatchesBaseline()
        {
            var actual = SampleCharts.RenderStripLineWithTitle();
            var result = ImageComparer.CompareToBaseline(actual, "StripLineWithTitle.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void TitleFrame_MatchesBaseline()
        {
            var actual = SampleCharts.RenderTitleFrame();
            var result = ImageComparer.CompareToBaseline(actual, "TitleFrame.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void TitleEmbed_MatchesBaseline()
        {
            var actual = SampleCharts.RenderTitleEmbed();
            var result = ImageComparer.CompareToBaseline(actual, "TitleEmbed.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void TitleEmboss_MatchesBaseline()
        {
            var actual = SampleCharts.RenderTitleEmboss();
            var result = ImageComparer.CompareToBaseline(actual, "TitleEmboss.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void TitleShadow_MatchesBaseline()
        {
            var actual = SampleCharts.RenderTitleShadow();
            var result = ImageComparer.CompareToBaseline(actual, "TitleShadow.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void LegendWithTitleAndHeader_MatchesBaseline()
        {
            var actual = SampleCharts.RenderLegendWithTitleAndHeader();
            var result = ImageComparer.CompareToBaseline(actual, "LegendWithTitleAndHeader.png");
            Assert.IsTrue(result.Matches, result.Message);
        }
    }
}
