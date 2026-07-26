using System;
using System.IO;
using System.Text;
using Microsoft.Reporting.NETCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ReportViewer.Chart.Rdl.Tests
{
    /// <summary>
    /// Exercises underlined/strikethrough text boxes (single-run and mixed-decoration
    /// rich text) through the real PDF rendering path. On Windows (this test's
    /// environment) this runs through the original RichText/LineBreaker/TextBox/
    /// Uniscribe pipeline.
    ///
    /// This exact report definition was also used to manually verify (2026-07-26) the
    /// underline/strikethrough support added to the cross-platform DrawWrappedText/
    /// DrawWrappedRichText paths (tasks/pdf-text-shaping-abstraction.md) - see that doc
    /// for how the cross-platform branch was forced locally and the resulting PDF's
    /// rendered appearance. See SimpleTextboxRdlTests for why there is no automated test
    /// of the cross-platform branch itself.
    /// </summary>
    [TestClass]
    public class DecorationRdlTests
    {
        [TestMethod]
        public void DecoratedTextboxes_RenderToPdf_ContainExpectedText()
        {
            var report = new LocalReport();
            using (var fs = new FileStream(Path.Combine(AppContext.BaseDirectory, "Reports", "DecorationReport.rdlc"), FileMode.Open))
            {
                report.LoadReportDefinition(fs);
            }

            var actual = report.Render("PDF", "<DeviceInfo><HumanReadablePdf>true</HumanReadablePdf></DeviceInfo>");

            Assert.IsTrue(actual.Length > 0, "PDF output should not be empty");
            string header = Encoding.ASCII.GetString(actual, 0, Math.Min(5, actual.Length));
            Assert.AreEqual("%PDF-", header, "Output should be a well-formed PDF document");

            string content = Encoding.Latin1.GetString(actual);
            StringAssert.Contains(content, "Underlined text sample");
            StringAssert.Contains(content, "Strikethrough text sample");
            StringAssert.Contains(content, "underlined");
            StringAssert.Contains(content, "struck");
        }
    }
}
