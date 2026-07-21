using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ReportViewer.DataVisualization.VisualRegressionTests
{
    /// <summary>
    /// Pixel baseline coverage for the gauge engine. These tests exist so that the GDI+ ->
    /// backend-agnostic rendering migration (see tasks/gauge-gdi-type-abstraction.md) can be
    /// checked for byte-for-byte-equivalent output at every milestone, on top of the engine
    /// still compiling and running. Mirrors <see cref="ChartVisualRegressionTests"/>.
    /// </summary>
    [TestClass]
    public class GaugeVisualRegressionTests
    {
        [TestMethod]
        public void SimpleCircularGauge_MatchesBaseline()
        {
            var actual = SampleGauges.RenderSimpleCircularGauge();
            var result = ImageComparer.CompareToBaseline(actual, "SimpleCircularGauge.png");
            Assert.IsTrue(result.Matches, result.Message);
        }

        [TestMethod]
        public void SimpleLinearGauge_MatchesBaseline()
        {
            var actual = SampleGauges.RenderSimpleLinearGauge();
            var result = ImageComparer.CompareToBaseline(actual, "SimpleLinearGauge.png");
            Assert.IsTrue(result.Matches, result.Message);
        }
    }
}
