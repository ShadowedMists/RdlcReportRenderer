using System;
using System.IO;
using Microsoft.Reporting.NETCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ReportViewer.Chart.Rdl.Tests
{
    /// <summary>
    /// End-to-end smoke tests for the IMAGE renderer's TIFF/BMP output through the real
    /// LocalReport.Render path (ImageWriter/Graphics.cs), as opposed to the pre-existing
    /// IMAGE/PNG coverage in GaugeRdlTests/SunburstChartRdlTests. TIFF/BMP/EMF had zero test
    /// coverage before this - see tasks/image-renderer-cross-platform.md. Also guards
    /// Graphics.NewPage's raster-surface construction (rewritten 2026-07-27 to a portable
    /// Bitmap+Graphics.FromImage instead of raw Win32 GetDC/CreateCompatibleDC/CreateDIBSection,
    /// which crashed immediately on non-Windows even for TIFF - see the same task file).
    /// </summary>
    [TestClass]
    public class ImageWriterRdlTests
    {
        private static LocalReport LoadReport(string reportFileName)
        {
            var report = new LocalReport();
            using (var fs = new FileStream(Path.Combine(AppContext.BaseDirectory, "Reports", reportFileName), FileMode.Open))
            {
                report.LoadReportDefinition(fs);
            }
            return report;
        }

        [TestMethod]
        public void SimpleTextbox_RendersToTiff()
        {
            var report = LoadReport("SimpleTextboxReport.rdlc");

            const string deviceInfo = "<DeviceInfo><OutputFormat>TIFF</OutputFormat></DeviceInfo>";
            var actual = report.Render("IMAGE", deviceInfo);

            Assert.IsTrue(actual.Length > 4, "TIFF output should not be empty");
            bool littleEndian = actual[0] == 'I' && actual[1] == 'I' && actual[2] == 42 && actual[3] == 0;
            bool bigEndian = actual[0] == 'M' && actual[1] == 'M' && actual[2] == 0 && actual[3] == 42;
            Assert.IsTrue(littleEndian || bigEndian, "Output should be a well-formed TIFF image");
        }

        [TestMethod]
        public void SimpleTextbox_RendersToBmp()
        {
            var report = LoadReport("SimpleTextboxReport.rdlc");

            const string deviceInfo = "<DeviceInfo><OutputFormat>BMP</OutputFormat></DeviceInfo>";
            var actual = report.Render("IMAGE", deviceInfo);

            Assert.IsTrue(actual.Length > 2, "BMP output should not be empty");
            Assert.AreEqual((byte)'B', actual[0], "Output should be a well-formed BMP image");
            Assert.AreEqual((byte)'M', actual[1], "Output should be a well-formed BMP image");
        }
    }
}
