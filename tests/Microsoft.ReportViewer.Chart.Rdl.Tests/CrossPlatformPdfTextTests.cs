using System.Drawing;
using System.IO;
using System.Text;
using Microsoft.ReportingServices.Interfaces;
using Microsoft.ReportingServices.Rendering.ImageRenderer;
using Microsoft.ReportingServices.Rendering.RichText;
using Microsoft.ReportingServices.Rendering.RPLProcessing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ReportViewer.Chart.Rdl.Tests
{
    /// <summary>
    /// Directly exercises PDFWriter.DrawWrappedText/DrawWrappedRichText - the
    /// cross-platform PDF text path used on non-Windows via Renderer's
    /// !OperatingSystem.IsWindows() branch (see tasks/pdf-text-shaping-abstraction.md).
    /// Unlike SimpleTextboxRdlTests/RichTextboxRdlTests/AlignedTextboxRdlTests/
    /// DecorationRdlTests (which render through LocalReport.Render and so only cover
    /// this branch on Linux), these tests call the internal methods directly and so run
    /// this exact code on every platform/CI run - possible because this test assembly is
    /// signed with the same key as Microsoft.ReportViewer.Common (see AssemblyInfo.cs's
    /// InternalsVisibleTo grant), not a new trust boundary.
    /// </summary>
    [TestClass]
    public class CrossPlatformPdfTextTests
    {
        private sealed class TestTextRunProps : ITextRunProps
        {
            public string FontFamily { get; set; } = "Arial";
            public float FontSize { get; set; } = 12f;
            public Color Color { get; set; } = Color.Black;
            public bool Bold { get; set; }
            public bool Italic { get; set; }
            public RPLFormat.TextDecorations TextDecoration { get; set; } = RPLFormat.TextDecorations.None;
            public int IndexInParagraph { get; set; }
            public string FontKey { get; set; }

            public void AddSplitIndex(int index)
            {
            }
        }

        private static string RenderContentStream(System.Action<PDFWriter> draw)
        {
            using var renderer = new Renderer(physicalPagination: true);
            using var stream = new MemoryStream();
            using var writer = new PDFWriter(renderer, stream, disposeRenderer: false, CreateAndRegisterStream, 96, 96)
            {
                HumanReadablePDF = true
            };

            writer.BeginReport(96, 96);
            writer.BeginPage(210f, 297f);
            writer.BeginPageSection(new RectangleF(0f, 0f, 200f, 280f));

            draw(writer);

            writer.EndPageSection();
            writer.EndPage();
            // Deliberately not calling EndReport(): it writes document metadata
            // (title/author from Renderer.RplReport) that only exists after a real
            // report has been processed via Renderer.ProcessPage, which these tests
            // don't do - they only need the page content stream EndPage() already
            // wrote, which is enough to assert on the operators DrawWrappedText/
            // DrawWrappedRichText emitted.

            stream.Position = 0;
            using var reader = new StreamReader(stream, Encoding.Latin1);
            return reader.ReadToEnd();

            static Stream CreateAndRegisterStream(string name, string extension, System.Text.Encoding encoding, string mimeType, bool willSeek, StreamOper operation)
            {
                return new MemoryStream();
            }
        }

        [TestMethod]
        public void DrawWrappedText_EmitsTjOperatorWithGivenText()
        {
            string pdf = RenderContentStream(writer =>
            {
                var style = new TestTextRunProps();
                writer.DrawWrappedText(new RectangleF(0f, 0f, 100f, 20f), PointF.Empty, "Hello cross-platform", style, RPLFormat.TextAlignments.Left);
            });

            StringAssert.Contains(pdf, "BT ");
            StringAssert.Contains(pdf, "(Hello cross-platform) Tj");
        }

        [TestMethod]
        public void DrawWrappedText_WithUnderline_EmitsFillRectangleAfterTextBlock()
        {
            string pdf = RenderContentStream(writer =>
            {
                var style = new TestTextRunProps { TextDecoration = RPLFormat.TextDecorations.Underline };
                writer.DrawWrappedText(new RectangleF(0f, 0f, 100f, 20f), PointF.Empty, "Underlined", style, RPLFormat.TextAlignments.Left);
            });

            int etIndex = pdf.IndexOf("ET");
            int reFIndex = pdf.IndexOf("re f", etIndex);
            Assert.IsTrue(etIndex >= 0, "Expected an ET (end text object) operator");
            Assert.IsTrue(reFIndex > etIndex, "Expected a filled rectangle (re f) after the text block for the underline");
        }

        [TestMethod]
        public void DrawWrappedText_WithoutDecoration_EmitsNoFillRectangle()
        {
            string pdf = RenderContentStream(writer =>
            {
                var style = new TestTextRunProps();
                writer.DrawWrappedText(new RectangleF(0f, 0f, 100f, 20f), PointF.Empty, "Plain text", style, RPLFormat.TextAlignments.Left);
            });

            Assert.IsFalse(pdf.Contains("re f"), "No decoration was requested, so no fill rectangle should be emitted");
        }

        [TestMethod]
        public void DrawWrappedRichText_WithMixedDecorations_EmitsOneFillRectanglePerDecoratedFragment()
        {
            string pdf = RenderContentStream(writer =>
            {
                var normal = new TestTextRunProps();
                var underlined = new TestTextRunProps { TextDecoration = RPLFormat.TextDecorations.Underline };
                var struckThrough = new TestTextRunProps { TextDecoration = RPLFormat.TextDecorations.LineThrough };

                var paragraphs = new System.Collections.Generic.List<(RPLFormat.TextAlignments Alignment, System.Collections.Generic.List<(string Text, ITextRunProps Style)> Runs)>
                {
                    (RPLFormat.TextAlignments.Left, new System.Collections.Generic.List<(string, ITextRunProps)>
                    {
                        ("Normal ", normal),
                        ("underlined ", underlined),
                        ("struck", struckThrough)
                    })
                };

                writer.DrawWrappedRichText(new RectangleF(0f, 0f, 150f, 20f), PointF.Empty, paragraphs);
            });

            StringAssert.Contains(pdf, "(Normal ) Tj");
            StringAssert.Contains(pdf, "(underlined ) Tj");
            StringAssert.Contains(pdf, "(struck) Tj");

            int etIndex = pdf.IndexOf("ET");
            string afterText = pdf.Substring(etIndex);
            int fillRectangleCount = 0;
            int searchStart = 0;
            while (true)
            {
                int found = afterText.IndexOf("re f", searchStart);
                if (found < 0)
                {
                    break;
                }
                fillRectangleCount++;
                searchStart = found + 1;
            }
            Assert.AreEqual(2, fillRectangleCount, "Expected one fill rectangle each for the underlined and struck-through fragments, none for the normal fragment");
        }
    }
}
