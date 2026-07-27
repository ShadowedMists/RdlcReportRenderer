using System;
using System.IO;
using System.Text;
using Microsoft.Reporting.NETCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ReportViewer.Chart.Rdl.Tests
{
    /// <summary>
    /// Exercises the WORD (binary Word 97) and WORDOPENXML renderers through the real
    /// RDL rendering path (LocalReport.Render), including a report with an embedded
    /// picture - the path that used to throw on non-Windows via a bare
    /// System.Drawing.Image.FromStream call in PictureDescriptor.cs/WordOpenXmlWriter.cs
    /// before both were routed through the same IImageProvider abstraction Excel/PDF
    /// already use (see tasks/word-renderer-cross-platform.md).
    /// </summary>
    [TestClass]
    public class WordRendererRdlTests
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
        public void SimpleTextbox_RendersToWord()
        {
            var report = LoadReport("SimpleTextboxReport.rdlc");
            byte[] actual = report.Render("WORD", null);
            Assert.IsTrue(actual.Length > 0, "WORD output should not be empty");
        }

        [TestMethod]
        public void SimpleTextbox_RendersToWordOpenXml()
        {
            var report = LoadReport("SimpleTextboxReport.rdlc");
            byte[] actual = report.Render("WORDOPENXML", null);
            Assert.IsTrue(actual.Length > 0, "WORDOPENXML output should not be empty");

            // WORDOPENXML (.docx) is a zip package - starts with the standard local-file-header signature.
            string header = Encoding.ASCII.GetString(actual, 0, Math.Min(2, actual.Length));
            Assert.AreEqual("PK", header, "Output should be a well-formed zip/OPC package");
        }

        [TestMethod]
        public void ImageReport_RendersToWord()
        {
            var report = LoadReport("WordImageReport.rdlc");
            byte[] actual = report.Render("WORD", null);
            Assert.IsTrue(actual.Length > 0, "WORD output with an embedded picture should not be empty");
        }

        [TestMethod]
        public void ImageReport_RendersToWordOpenXml()
        {
            var report = LoadReport("WordImageReport.rdlc");
            byte[] actual = report.Render("WORDOPENXML", null);
            Assert.IsTrue(actual.Length > 0, "WORDOPENXML output with an embedded picture should not be empty");

            string header = Encoding.ASCII.GetString(actual, 0, Math.Min(2, actual.Length));
            Assert.AreEqual("PK", header, "Output should be a well-formed zip/OPC package");
        }
    }
}
