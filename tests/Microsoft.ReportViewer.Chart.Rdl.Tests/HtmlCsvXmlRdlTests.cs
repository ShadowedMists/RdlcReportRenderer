using System;
using System.IO;
using System.Text;
using Microsoft.Reporting.NETCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ReportViewer.Chart.Rdl.Tests
{
    /// <summary>
    /// Basic end-to-end smoke tests for the HTML4.0/HTML5/CSV/XML renderers through the real
    /// LocalReport.Render path. These formats had zero automated test coverage before this
    /// (see tasks/test-coverage-gaps.md) - none are known to have cross-platform blockers, so
    /// these are gap-filling smoke tests rather than a specific bug investigation.
    /// </summary>
    [TestClass]
    public class HtmlCsvXmlRdlTests
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
        public void SimpleTextbox_RendersToHtml5()
        {
            var report = LoadReport("SimpleTextboxReport.rdlc");
            byte[] actual = report.Render("HTML5", null);
            Assert.IsTrue(actual.Length > 0, "HTML5 output should not be empty");

            string content = Encoding.UTF8.GetString(actual);
            StringAssert.Contains(content, "The quick brown");
        }

        [TestMethod]
        public void SimpleTextbox_RendersToHtml40()
        {
            var report = LoadReport("SimpleTextboxReport.rdlc");
            byte[] actual = report.Render("HTML4.0", null);
            Assert.IsTrue(actual.Length > 0, "HTML4.0 output should not be empty");

            string content = Encoding.UTF8.GetString(actual);
            StringAssert.Contains(content, "The quick brown");
        }

        [TestMethod]
        public void SimpleTextbox_RendersToCsv()
        {
            // CSV only emits rows from tablix/list/table data regions, not free-standing
            // textboxes - SimpleTextboxReport.rdlc has neither, so an empty (BOM-only) result
            // here is correct output, not a bug. This smoke test only confirms the real
            // LocalReport.Render("CSV") path completes without throwing.
            var report = LoadReport("SimpleTextboxReport.rdlc");
            byte[] actual = report.Render("CSV", null);
            Assert.IsNotNull(actual);
        }

        [TestMethod]
        public void SimpleTextbox_RendersToXml()
        {
            var report = LoadReport("SimpleTextboxReport.rdlc");
            byte[] actual = report.Render("XML", null);
            Assert.IsTrue(actual.Length > 0, "XML output should not be empty");

            string content = Encoding.UTF8.GetString(actual);
            StringAssert.Contains(content, "<?xml");
        }
    }
}
