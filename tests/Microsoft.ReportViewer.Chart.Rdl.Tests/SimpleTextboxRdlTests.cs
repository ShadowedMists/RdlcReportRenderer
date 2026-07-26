using System;
using System.IO;
using System.Text;
using Microsoft.Reporting.NETCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ReportViewer.Chart.Rdl.Tests
{
    /// <summary>
    /// Exercises a plain single-style textbox through the real PDF rendering path
    /// (PDFRenderer -> Renderer -> PDFWriter). On Windows (this test's environment) this
    /// runs through the original RichText/LineBreaker/TextBox/Uniscribe pipeline.
    ///
    /// This exact report definition was also used to manually verify (2026-07-26) the new
    /// cross-platform DrawWrappedText path added for tasks/pdf-text-shaping-abstraction.md
    /// - see that doc for how the cross-platform branch was forced locally (a temporary,
    /// reverted one-line edit to Renderer.ProcessSimpleTextBox) and the resulting PDF's
    /// rendered appearance. There is currently no automated way to force that branch from
    /// a test: Renderer/PDFWriter are internal with no InternalsVisibleTo grant to any test
    /// project (the assembly is strong-name signed), and the branch condition is a direct
    /// OperatingSystem.IsWindows() check with no injectable seam - unlike Chart's
    /// SkiaChartRenderingTests, which can construct a Skia-backed object graph directly.
    /// Adding a test-only seam would mean either weakening the platform check in
    /// production code or touching strong-name signing infrastructure, neither of which
    /// should happen without explicit sign-off - so this gap is documented rather than
    /// worked around.
    /// </summary>
    [TestClass]
    public class SimpleTextboxRdlTests
    {
        [TestMethod]
        public void SimpleTextbox_RendersToPdf_ContainsExpectedText()
        {
            var report = new LocalReport();
            using (var fs = new FileStream(Path.Combine(AppContext.BaseDirectory, "Reports", "SimpleTextboxReport.rdlc"), FileMode.Open))
            {
                report.LoadReportDefinition(fs);
            }

            var actual = report.Render("PDF", "<DeviceInfo><HumanReadablePdf>true</HumanReadablePdf></DeviceInfo>");

            Assert.IsTrue(actual.Length > 0, "PDF output should not be empty");
            string header = Encoding.ASCII.GetString(actual, 0, Math.Min(5, actual.Length));
            Assert.AreEqual("%PDF-", header, "Output should be a well-formed PDF document");

            string content = Encoding.Latin1.GetString(actual);
            StringAssert.Contains(content, "The quick brown");
            StringAssert.Contains(content, "Tj");
        }
    }
}
