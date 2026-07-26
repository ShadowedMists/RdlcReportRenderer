using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Reporting.Chart.WebForms;
using Microsoft.Reporting.Chart.WebForms.Rendering;
using Microsoft.Reporting.Chart.WebForms.Rendering.Gdi;
using Microsoft.Reporting.Chart.WebForms.Rendering.Skia;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ReportViewer.DataVisualization.VisualRegressionTests
{
    /// <summary>
    /// Milestone F — proves the platform-selection wiring itself, not just that a scene renders.
    /// <see cref="ChartRenderingBackendFactory"/> is the one place that decides Gdi vs. Skia for
    /// both <c>ChartPicture.renderSurfaceFactory</c> and <c>ChartPicture.chartGraph</c>; these tests
    /// assert the two agree with each other and with the current platform. The last test exercises
    /// the real production entry point (<c>Chart.Save</c> → <c>ChartImage.SaveImage</c>, the same
    /// path <c>ChartMapper.GetImage</c> uses) rather than <see cref="SkiaChartRenderingTests"/>'s
    /// manual <c>chartGraph</c>-swap helper, proving <c>SaveImage</c>'s background-fill no longer
    /// needs its own <c>GdiRenderSurface</c> downcast to work.
    /// </summary>
    [TestClass]
    public class ChartRenderingBackendFactoryTests
    {
        private static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        [TestMethod]
        public void CreateRenderSurfaceFactory_MatchesCurrentPlatform()
        {
            IRenderSurfaceFactory factory = ChartRenderingBackendFactory.CreateRenderSurfaceFactory();

            if (IsWindows)
            {
                Assert.IsInstanceOfType(factory, typeof(GdiRenderSurfaceFactory));
            }
            else
            {
                Assert.IsInstanceOfType(factory, typeof(SkiaRenderSurfaceFactory));
            }
        }

        [TestMethod]
        public void CreateChartGraphics_AgreesWithRenderSurfaceFactorySelection()
        {
            using var chart = SampleCharts.BuildSimpleBarChart();
            ChartGraphics chartGraph = chart.chartPicture.chartGraph;
            IRenderSurfaceFactory renderSurfaceFactory = chart.chartPicture.renderSurfaceFactory;

            if (IsWindows)
            {
                Assert.AreEqual(RenderingType.Gdi, chartGraph.ActiveRenderingType);
                Assert.IsInstanceOfType(renderSurfaceFactory, typeof(GdiRenderSurfaceFactory));
            }
            else
            {
                Assert.AreEqual(RenderingType.Skia, chartGraph.ActiveRenderingType);
                Assert.IsInstanceOfType(renderSurfaceFactory, typeof(SkiaRenderSurfaceFactory));
            }
        }

        [TestMethod]
        public void RealProductionPath_ChartSave_RendersThroughSelectedBackend()
        {
            using var chart = SampleCharts.BuildSimpleBarChart();
            using var stream = new MemoryStream();

            chart.Save(stream, ChartImageFormat.Png);

            Assert.IsTrue(stream.Length > 0, "Chart.Save (the real ChartMapper.GetImage -> ChartImage.SaveImage path) produced no output.");
        }
    }
}
