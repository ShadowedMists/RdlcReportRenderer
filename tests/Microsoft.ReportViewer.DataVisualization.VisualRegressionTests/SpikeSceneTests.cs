using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ReportViewer.DataVisualization.VisualRegressionTests
{
    /// <summary>
    /// Phase 0 spike (tasks/chart-cross-platform-implementation.md) — not a regression gate.
    /// Writes both backends' output under Results/ for manual/visual comparison; there is no
    /// committed baseline here because backend-to-backend pixel parity (GDI+ vs Skia) is not
    /// this spike's goal (see spike report) — Milestone E2 handles that once a production
    /// Skia adapter exists.
    /// </summary>
    [TestClass]
    public class SpikeSceneTests
    {
        private static string ResultsDir => Path.Combine(AppContext.BaseDirectory, "Results");

        [TestMethod]
        public void GdiBackend_RendersSpikeScene()
        {
            if (!OperatingSystem.IsWindows())
            {
                Assert.Inconclusive("GDI+ cannot initialize on this platform (see spike report) — Windows-only check.");
                return;
            }

            Directory.CreateDirectory(ResultsDir);
            var png = SpikeRunners.RenderViaGdi();
            File.WriteAllBytes(Path.Combine(ResultsDir, "SpikeScene.Gdi.png"), png);

            Assert.IsTrue(png.Length > 0, "Expected non-empty PNG output from the GDI+ backend.");
        }

        [TestMethod]
        public void SkiaBackend_RendersSpikeScene()
        {
            Directory.CreateDirectory(ResultsDir);
            var png = SpikeRunners.RenderViaSkia();
            File.WriteAllBytes(Path.Combine(ResultsDir, "SpikeScene.Skia.png"), png);

            Assert.IsTrue(png.Length > 0, "Expected non-empty PNG output from the Skia backend.");
        }
    }
}
