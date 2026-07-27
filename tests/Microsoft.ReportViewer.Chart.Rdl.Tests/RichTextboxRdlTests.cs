using System;
using System.IO;
using System.Text;
using Microsoft.Reporting.NETCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ReportViewer.Chart.Rdl.Tests
{
    /// <summary>
    /// Exercises a multi-paragraph, multi-run ("rich text") textbox through the real PDF
    /// rendering path. On Windows (this test's environment) this runs through the
    /// original RichText/LineBreaker/TextBox/Uniscribe pipeline via
    /// Renderer.ProcessRichTextBox.
    ///
    /// This exact report definition was also used to manually verify (2026-07-26) the new
    /// cross-platform DrawWrappedRichText path added for
    /// tasks/pdf-text-shaping-abstraction.md - see that doc for how the cross-platform
    /// branch was forced locally (a temporary, reverted one-line edit to
    /// Renderer.ProcessRichTextBox) and the resulting PDF's rendered appearance (correct
    /// bold-prefix + normal-run wrapping within one paragraph, plus a separate italic
    /// Times New Roman paragraph). See SimpleTextboxRdlTests for why there is no automated
    /// test of the cross-platform branch itself.
    /// </summary>
    [TestClass]
    public class RichTextboxRdlTests
    {
        [TestMethod]
        public void RichTextbox_RendersToPdf_ContainsExpectedText()
        {
            var report = new LocalReport();
            using (var fs = new FileStream(Path.Combine(AppContext.BaseDirectory, "Reports", "RichTextboxReport.rdlc"), FileMode.Open))
            {
                report.LoadReportDefinition(fs);
            }

            // EmbedFonts=None forces the base-14 literal-WinAnsi-Tj path (see PDFWriter's
            // DrawWrappedText) rather than the default Subset/composite-CID-font embedding,
            // where drawn text is written as opaque hex glyph ids and can't be recovered by
            // a plain substring check regardless of platform.
            var actual = report.Render("PDF", "<DeviceInfo><HumanReadablePdf>true</HumanReadablePdf><EmbedFonts>None</EmbedFonts></DeviceInfo>");

            Assert.IsTrue(actual.Length > 0, "PDF output should not be empty");
            string header = Encoding.ASCII.GetString(actual, 0, Math.Min(5, actual.Length));
            Assert.AreEqual("%PDF-", header, "Output should be a well-formed PDF document");

            string content = Encoding.Latin1.GetString(actual);
            StringAssert.Contains(content, "IMPORTANT");
            StringAssert.Contains(content, "Second paragraph");
            StringAssert.Contains(content, "Tj");
        }
    }
}
