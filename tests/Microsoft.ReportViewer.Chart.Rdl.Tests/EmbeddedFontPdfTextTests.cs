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
    /// Exercises PDFWriter's real font-embedding path for the cross-platform (base-14
    /// replacement) text writers - the "full-fidelity rich text wiring" increment that
    /// reversed the earlier decision to defer font embedding (docs/decisions.md,
    /// 2026-07-26). When EmbedFonts is FontEmbedding.Subset, DrawWrappedText/
    /// DrawWrappedRichText now draw through a real SkiaSharp-backed Type0/CIDFontType2
    /// composite font (glyph-indexed Tj) instead of a base-14 Tj string - see
    /// GetOrCreateEmbeddedFont/WriteCompositeText/WriteSkiaCompositeFont/
    /// WriteSkiaEmbeddedFont in PDFWriter.cs.
    /// </summary>
    [TestClass]
    public class EmbeddedFontPdfTextTests
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

        private static string RenderContentStream(System.Action<PDFWriter> draw, bool writeFonts)
        {
            using var renderer = new Renderer(physicalPagination: true);
            using var stream = new MemoryStream();
            using var writer = new PDFWriter(renderer, stream, disposeRenderer: false, CreateAndRegisterStream, 96, 96)
            {
                HumanReadablePDF = true,
                EmbedFonts = FontEmbedding.Subset
            };

            writer.BeginReport(96, 96);
            writer.BeginPage(210f, 297f);
            writer.BeginPageSection(new RectangleF(0f, 0f, 200f, 280f));

            draw(writer);

            writer.EndPageSection();
            writer.EndPage();

            if (writeFonts)
            {
                // EndReport() also writes document-map/catalog metadata that depends on a
                // fully-processed RplReport, which this minimal harness doesn't have - but
                // font objects (the only thing these tests assert on) are written first,
                // directly to the stream, before any of that. Swallow whatever happens
                // afterward so the already-written font bytes are still there to read.
                try
                {
                    writer.EndReport();
                }
                catch (System.Exception)
                {
                }
            }

            stream.Position = 0;
            using var reader = new StreamReader(stream, Encoding.Latin1);
            return reader.ReadToEnd();

            static Stream CreateAndRegisterStream(string name, string extension, System.Text.Encoding encoding, string mimeType, bool willSeek, StreamOper operation)
            {
                return new MemoryStream();
            }
        }

        [TestMethod]
        public void DrawWrappedText_WithEmbedding_EmitsHexGlyphTjNotLiteralString()
        {
            string pdf = RenderContentStream(writer =>
            {
                var style = new TestTextRunProps();
                writer.DrawWrappedText(new RectangleF(0f, 0f, 100f, 20f), PointF.Empty, "Hello", style, RPLFormat.TextAlignments.Left);
            }, writeFonts: false);

            StringAssert.Contains(pdf, "BT ");
            Assert.IsFalse(pdf.Contains("(Hello) Tj"), "Embedded-font drawing should use glyph-indexed hex Tj, not a literal WinAnsi string");
            StringAssert.Contains(pdf, "> Tj", "Expected a composite hex-string Tj operator");
        }

        [TestMethod]
        public void DrawWrappedText_WithEmbedding_WritesType0FontWithIdentityHEncoding()
        {
            string pdf = RenderContentStream(writer =>
            {
                var style = new TestTextRunProps();
                writer.DrawWrappedText(new RectangleF(0f, 0f, 100f, 20f), PointF.Empty, "Hello", style, RPLFormat.TextAlignments.Left);
            }, writeFonts: true);

            StringAssert.Contains(pdf, "/Subtype /Type0");
            StringAssert.Contains(pdf, "/Encoding /Identity-H");
            StringAssert.Contains(pdf, "/Subtype /CIDFontType2");
            StringAssert.Contains(pdf, "/CIDToGIDMap /Identity");
        }

        [TestMethod]
        public void DrawWrappedText_WithEmbedding_EmbedsRealFontFile()
        {
            string pdf = RenderContentStream(writer =>
            {
                var style = new TestTextRunProps();
                writer.DrawWrappedText(new RectangleF(0f, 0f, 100f, 20f), PointF.Empty, "Hello", style, RPLFormat.TextAlignments.Left);
            }, writeFonts: true);

            StringAssert.Contains(pdf, "/FontFile2");
        }

        [TestMethod]
        public void DrawWrappedRichText_WithEmbedding_EmitsHexGlyphTjForEachFragment()
        {
            string pdf = RenderContentStream(writer =>
            {
                var normal = new TestTextRunProps();
                var bold = new TestTextRunProps { Bold = true };

                var paragraphs = new System.Collections.Generic.List<(RPLFormat.TextAlignments Alignment, System.Collections.Generic.List<(string Text, ITextRunProps Style)> Runs)>
                {
                    (RPLFormat.TextAlignments.Left, new System.Collections.Generic.List<(string, ITextRunProps)>
                    {
                        ("Plain ", normal),
                        ("Bold", bold)
                    })
                };

                writer.DrawWrappedRichText(new RectangleF(0f, 0f, 150f, 20f), PointF.Empty, paragraphs);
            }, writeFonts: false);

            Assert.IsFalse(pdf.Contains("(Plain ) Tj"), "Embedded-font drawing should not fall back to literal-string Tj");
            Assert.IsFalse(pdf.Contains("(Bold) Tj"), "Embedded-font drawing should not fall back to literal-string Tj");
            StringAssert.Contains(pdf, "> Tj");
        }

        [TestMethod]
        public void DrawWrappedText_WithoutEmbedding_StillUsesBase14LiteralTj()
        {
            using var renderer = new Renderer(physicalPagination: true);
            using var stream = new MemoryStream();
            using var writer = new PDFWriter(renderer, stream, disposeRenderer: false, (name, extension, encoding, mimeType, willSeek, operation) => new MemoryStream(), 96, 96)
            {
                HumanReadablePDF = true
                // EmbedFonts left at its default (FontEmbedding.None) - the pre-existing
                // base-14 behavior (CrossPlatformPdfTextTests.cs) must be unaffected.
            };

            writer.BeginReport(96, 96);
            writer.BeginPage(210f, 297f);
            writer.BeginPageSection(new RectangleF(0f, 0f, 200f, 280f));

            var style = new TestTextRunProps();
            writer.DrawWrappedText(new RectangleF(0f, 0f, 100f, 20f), PointF.Empty, "Hello", style, RPLFormat.TextAlignments.Left);

            writer.EndPageSection();
            writer.EndPage();

            stream.Position = 0;
            using var reader = new StreamReader(stream, Encoding.Latin1);
            string pdf = reader.ReadToEnd();

            StringAssert.Contains(pdf, "(Hello) Tj");
        }
    }
}
