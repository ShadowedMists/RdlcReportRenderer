using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Reporting.NETCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ReportViewer.Chart.Rdl.Tests
{
    /// <summary>
    /// Basic end-to-end smoke tests for the Gauge engine through the real LocalReport.Render
    /// path (GaugeMapper populating a GaugePanel from the report's RadialGauges/GaugeScales/
    /// GaugePointers), as opposed to
    /// Microsoft.ReportViewer.DataVisualization.VisualRegressionTests, which builds
    /// GaugeContainer/CircularGauge objects directly in C# and never exercises RDL/GaugePanel
    /// parsing at all. Gauge previously had zero RDL-render-level coverage - see
    /// tasks/test-coverage-gaps.md. No visual baseline exists for Gauge yet, so these assert
    /// on well-formed output rather than pixel content, matching HtmlCsvXmlRdlTests's pattern.
    /// </summary>
    [TestClass]
    public class GaugeRdlTests
    {
        private static LocalReport LoadReportWithData(double value)
        {
            var report = new LocalReport();
            using (var fs = new FileStream(Path.Combine(AppContext.BaseDirectory, "Reports", "SimpleGaugeReport.rdlc"), FileMode.Open))
            {
                report.LoadReportDefinition(fs);
            }

            var rows = new List<GaugeDataRow> { new() { Value = value } };
            report.DataSources.Add(new ReportDataSource("Data", rows));
            return report;
        }

        [TestMethod]
        public void SimpleRadialGauge_RendersToImage()
        {
            var report = LoadReportWithData(42);

            const string deviceInfo = "<DeviceInfo><OutputFormat>PNG</OutputFormat></DeviceInfo>";
            var actual = report.Render("IMAGE", deviceInfo);

            Assert.IsTrue(actual.Length > 8, "IMAGE output should not be empty");
            byte[] pngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            for (int i = 0; i < pngSignature.Length; i++)
            {
                Assert.AreEqual(pngSignature[i], actual[i], "Output should be a well-formed PNG image");
            }
        }

        [TestMethod]
        public void SimpleRadialGauge_RendersToPdf()
        {
            var report = LoadReportWithData(42);

            var actual = report.Render("PDF");

            Assert.IsTrue(actual.Length > 0, "PDF output should not be empty");
            string header = System.Text.Encoding.ASCII.GetString(actual, 0, Math.Min(5, actual.Length));
            Assert.AreEqual("%PDF-", header, "Output should be a well-formed PDF document");
        }
    }

    internal sealed class GaugeDataRow
    {
        public double Value { get; set; }
    }
}
