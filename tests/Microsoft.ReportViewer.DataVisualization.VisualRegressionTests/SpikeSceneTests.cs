using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ReportViewer.DataVisualization.VisualRegressionTests
{
    /// <summary>
    /// Phase 0 spike (tasks/chart-cross-platform-implementation.md). <see cref="GdiBackend_RendersSpikeScene"/>
    /// is still a Windows-only sanity check (GDI+ cannot initialize elsewhere, see spike report). Milestone
    /// E2 (2026-07-23) turned <see cref="SkiaBackend_MatchesBaseline"/> into a real pixel-baseline regression
    /// test — deliberately NOT a GDI+-vs-Skia parity check: this spike scene draws no gradients/hatches/paths
    /// where E1's documented approximations would apply, but text glyph rasterization still differs between
    /// GDI+ and Skia (different rasterizers), so byte-for-byte cross-backend parity was never the goal. Instead
    /// this asserts the Skia backend's *own* output is deterministic against a committed Skia-rendered
    /// baseline — the first regression test in this project that can actually run (and catch regressions) on
    /// non-Windows CI, since it never touches GDI+/System.Drawing.Bitmap at all.
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
        public void SkiaBackend_MatchesBaseline()
        {
            var actual = SpikeRunners.RenderViaSkia();
            var result = ImageComparer.CompareToBaseline(actual, "SpikeScene.Skia.png");
            Assert.IsTrue(result.Matches, result.Message);
        }
    }
}
