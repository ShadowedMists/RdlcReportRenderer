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
        // see SunburstChart.cs:16), so the combine-check InvalidOperationException no longer
        // fires. However, no automated regression test exists yet: SunburstChart reads its
        // hierarchy from ChartArea.CategoryNodes, which nothing in the engine ever populates
        // automatically (unlike TreeMapChart, which builds its own tree from Series/DataPoints
        // directly), and CategoryNode/CategoryNodeCollection are internal sealed classes with no
        // public constructor - so there is currently no way for any external consumer to
        // populate CategoryNodes at all. This is a separate, pre-existing design gap, flagged
        // for a future decision (auto-build from flat Series/Points like TreeMapChart, or expose
        // a public hierarchy-building API).
    }
}
