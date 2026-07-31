using System;
using System.IO;
using Microsoft.Reporting.NETCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ReportViewer.Chart.Rdl.Tests
{
    /// <summary>
    /// Basic end-to-end smoke test for the Map engine through the real LocalReport.Render path
    /// (MapMapper populating a Map/MapViewport from the report definition), confirming the
    /// existing Windows-only behavior actually works - Map had zero automated coverage, on any
    /// platform, before this (see tasks/test-coverage-gaps.md). No visual baseline exists for Map
    /// yet, so this asserts on well-formed output rather than pixel content, matching
    /// GaugeRdlTests/HtmlCsvXmlRdlTests's pattern. Map's own GDI+-to-interface migration is a
    /// separate, deferred, HIGH-risk item (see docs/decisions.md) - this test doesn't touch that.
    /// </summary>
    [TestClass]
    public class MapRdlTests
    {
        [TestMethod]
        public void SimpleMap_RendersToImage()
        {
            var report = new LocalReport();
            using (var fs = new FileStream(Path.Combine(AppContext.BaseDirectory, "Reports", "SimpleMapReport.rdlc"), FileMode.Open))
            {
                report.LoadReportDefinition(fs);
            }

            const string deviceInfo = "<DeviceInfo><OutputFormat>PNG</OutputFormat></DeviceInfo>";
            var actual = report.Render("IMAGE", deviceInfo);

            Assert.IsTrue(actual.Length > 8, "IMAGE output should not be empty");
            byte[] pngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            for (int i = 0; i < pngSignature.Length; i++)
            {
                Assert.AreEqual(pngSignature[i], actual[i], "Output should be a well-formed PNG image");
            }
        }

        /// <summary>
        /// Exercises the static-MapPoints-plus-MapColorRangeRule path fixed in
        /// EmbeddedSpatialDataMapper.AddSpatialElement (see tasks/map-spatial-data-population-gap.md).
        /// This is a smoke test only, matching GaugeRdlTests/SimpleMap_RendersToImage's own pattern (no
        /// pixel-content assertion, no visual baseline) -- it confirms the report no longer throws and
        /// still produces a well-formed PNG. It does NOT confirm the color-scale legend actually renders
        /// visibly: manual inspection during development found a marker appears at the first static point
        /// on Windows, the same fixture renders fully blank on WSL (a real, distinct, not-yet-root-caused
        /// cross-platform gap), and the ColorSwatchPanel itself was not confirmed to draw anything on
        /// either platform. See the task file's "What's still not proven" section before treating this
        /// test as evidence the legend works.
        /// </summary>
        [TestMethod]
        public void MapWithStaticPointsAndColorScale_RendersToImage()
        {
            var report = new LocalReport();
            using (var fs = new FileStream(Path.Combine(AppContext.BaseDirectory, "Reports", "MapColorScaleReport.rdlc"), FileMode.Open))
            {
                report.LoadReportDefinition(fs);
            }

            const string deviceInfo = "<DeviceInfo><OutputFormat>PNG</OutputFormat></DeviceInfo>";
            var actual = report.Render("IMAGE", deviceInfo);

            Assert.IsTrue(actual.Length > 8, "IMAGE output should not be empty");
            byte[] pngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            for (int i = 0; i < pngSignature.Length; i++)
            {
                Assert.AreEqual(pngSignature[i], actual[i], "Output should be a well-formed PNG image");
            }

            // Saved for manual visual inspection -- no automated pixel assertion exists yet (no baseline).
            var resultsDir = Path.Combine(AppContext.BaseDirectory, "Results");
            Directory.CreateDirectory(resultsDir);
            File.WriteAllBytes(Path.Combine(resultsDir, "MapColorScaleReport.png"), actual);
        }
    }
}
