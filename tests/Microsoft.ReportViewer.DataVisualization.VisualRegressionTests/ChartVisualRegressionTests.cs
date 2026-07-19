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
    }
}
