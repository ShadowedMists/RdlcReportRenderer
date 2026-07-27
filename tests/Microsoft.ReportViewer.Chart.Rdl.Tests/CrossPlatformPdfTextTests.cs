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
    ///
    /// Also covers PDFWriter.DrawTextRunCrossPlatform/ProcessDrawStringFontCrossPlatform -
    /// the real TextBox.RenderParagraph pipeline's PDF glyph-drawing hook, as opposed to
    /// the DrawWrappedText/DrawWrappedRichText MVP bypass above. DrawTextRunCrossPlatform
    /// bypasses the OperatingSystem.IsWindows() check inside DrawTextRun the same way
    /// TextRun.ShapeAndPlaceCrossPlatform/Paragraph.ScriptItemizeCrossPlatform already do,
    /// since DrawTextRun's real dispatch (via ITextBoxProps.DrawTextRun -&gt;
    /// WriterBase.DrawTextRun) always resolves to the Windows branch when actually running
    /// on this Windows dev box/CI runner, regardless of how the TextRun was shaped.
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

        [TestMethod]
        public void DrawTextRunCrossPlatform_Base14_EmitsTjOperatorWithGivenText()
        {
            string pdf = RenderContentStream(writer =>
            {
                FontCache fontCache = new FontCache(96f);
                TextRun run = new TextRun("Hello", new TestTextRunProps());
                run.ShapeAndPlaceCrossPlatform(fontCache);

                writer.DrawTextRunCrossPlatform(Win32DCSafeHandle.Zero, fontCache, textBox: null, run, System.TypeCode.String,
                    RPLFormat.TextAlignments.Left, RPLFormat.VerticalAlignments.Top, RPLFormat.WritingModes.Horizontal,
                    RPLFormat.Directions.LTR, new Point(0, 20), new System.Drawing.Rectangle(0, 0, 200, 50), lHeight: 24, baselineY: 20);
            });

            StringAssert.Contains(pdf, "BT ");
            StringAssert.Contains(pdf, "(Hello) Tj");
        }

        [TestMethod]
        public void DrawTextRunCrossPlatform_EmbeddedSubsetFont_EmitsCompositeGlyphHexTjOperator()
        {
            string pdf = RenderContentStream(writer =>
            {
                writer.EmbedFonts = FontEmbedding.Subset;
                FontCache fontCache = new FontCache(96f);
                TextRun run = new TextRun("Hello", new TestTextRunProps());
                run.ShapeAndPlaceCrossPlatform(fontCache);

                writer.DrawTextRunCrossPlatform(Win32DCSafeHandle.Zero, fontCache, textBox: null, run, System.TypeCode.String,
                    RPLFormat.TextAlignments.Left, RPLFormat.VerticalAlignments.Top, RPLFormat.WritingModes.Horizontal,
                    RPLFormat.Directions.LTR, new Point(0, 20), new System.Drawing.Rectangle(0, 0, 200, 50), lHeight: 24, baselineY: 20);
            });

            StringAssert.Contains(pdf, "BT ");
            // Composite (Identity-H) fonts write glyph ids as a hex string, not the literal
            // text, so no "(Hello) Tj" - just a "<...> Tj" with real HarfBuzz/Skia glyph ids.
            Assert.IsFalse(pdf.Contains("(Hello) Tj"), "A composite/embedded font should not draw via the literal-text Tj branch");
            StringAssert.Matches(pdf, new System.Text.RegularExpressions.Regex("<[0-9A-Fa-f]+> Tj"));
        }

        [TestMethod]
        public void DrawTextRunCrossPlatform_ReusesSameFontIdAcrossRunsWithSameStyle()
        {
            string pdf = RenderContentStream(writer =>
            {
                FontCache fontCache = new FontCache(96f);
                TextRun run1 = new TextRun("First", new TestTextRunProps());
                TextRun run2 = new TextRun("Second", new TestTextRunProps());
                run1.ShapeAndPlaceCrossPlatform(fontCache);
                run2.ShapeAndPlaceCrossPlatform(fontCache);

                writer.DrawTextRunCrossPlatform(Win32DCSafeHandle.Zero, fontCache, textBox: null, run1, System.TypeCode.String,
                    RPLFormat.TextAlignments.Left, RPLFormat.VerticalAlignments.Top, RPLFormat.WritingModes.Horizontal,
                    RPLFormat.Directions.LTR, new Point(0, 20), new System.Drawing.Rectangle(0, 0, 200, 50), lHeight: 24, baselineY: 20);
                writer.DrawTextRunCrossPlatform(Win32DCSafeHandle.Zero, fontCache, textBox: null, run2, System.TypeCode.String,
                    RPLFormat.TextAlignments.Left, RPLFormat.VerticalAlignments.Top, RPLFormat.WritingModes.Horizontal,
                    RPLFormat.Directions.LTR, new Point(0, 40), new System.Drawing.Rectangle(0, 0, 200, 50), lHeight: 24, baselineY: 40);
            });

            StringAssert.Contains(pdf, "(First) Tj");
            StringAssert.Contains(pdf, "(Second) Tj");
            // Both runs share the same (family, bold, italic) style, so PDFWriter's font
            // cache (GetOrCreateBase14Font, keyed by style) should hand back the same
            // PDFFont/FontId for both - not allocate a second embedded font object.
            System.Text.RegularExpressions.MatchCollection fontIds = System.Text.RegularExpressions.Regex.Matches(pdf, @"/F(\d+) \d");
            Assert.IsTrue(fontIds.Count >= 2, "Expected at least two /F<id> <size> Tf operators, one per run");
            Assert.AreEqual(fontIds[0].Groups[1].Value, fontIds[1].Groups[1].Value, "Both runs use identical style and should reuse the same cached font id");
        }

        [TestMethod]
        public void FillHighlightRectangle_EmitsFilledRectangleOperatorInDeviceRectPosition()
        {
            string pdf = RenderContentStream(writer =>
            {
                FontCache fontCache = new FontCache(96f);
                ReportTextBox reportTextBox = new ReportTextBox(null, writer);

                // ReportTextBox.FillHighlightRectangle (TextBox.RenderHighlightedTextRun's
                // cross-platform counterpart to g.FillRectangle - see TextBox.cs) is called
                // directly here the same way DrawTextRunCrossPlatform is above: its real
                // caller only reaches it when no System.Drawing.Graphics exists, which on
                // this Windows dev box never happens, so there is no separate OS check to
                // bypass - just a normal internal method to call directly.
                reportTextBox.FillHighlightRectangle(fontCache, new Rectangle(10, 20, 30, 5), Color.Yellow);
            });

            StringAssert.Contains(pdf, " re f");
        }
    }
}
