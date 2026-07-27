using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.ReportingServices.Interfaces;
using Microsoft.ReportingServices.Rendering.ImageRenderer;
using Microsoft.ReportingServices.Rendering.RichText;
using Microsoft.ReportingServices.Rendering.RPLProcessing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Microsoft.ReportViewer.Chart.Rdl.Tests
{
    /// <summary>
    /// End-to-end counterpart to EmbeddedFontPdfTextTests/CffGlyphSubsetterTests, but for a
    /// font family backed by a real TrueType Collection ('ttcf') container - "SimSun"
    /// (simsun.ttc, ~18MB, installed on this dev box) rather than a plain single-face .ttf.
    /// Before this increment, PDFWriter.WriteSkiaCompositeFont/WriteSkiaEmbeddedFont never
    /// extracted a single face out of a TTC first: SfntBinaryUtils.DetectOutlineFormat
    /// returned Unsupported for the raw multi-face blob, which fell through to the
    /// CIDFontType2/FontFile2 branch anyway (isCffOutline defaults false) - silently
    /// embedding the *entire* multi-face container as if it were a bare TrueType font
    /// program, which is not what FontFile2 is allowed to hold per PDF spec 9.9. These tests
    /// confirm the real fix: the specific face SkiaSharp resolved is extracted into a
    /// standalone sfnt first (SfntTtcExtractionTests covers that step in isolation), then
    /// goes through the exact same subsetting/embedding path as any other TrueType font.
    /// </summary>
    [TestClass]
    public class TrueTypeCollectionPdfTextTests
    {
        private sealed class TestTextRunProps : ITextRunProps
        {
            public string FontFamily { get; set; } = "SimSun";
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
                HumanReadablePDF = true,
                EmbedFonts = FontEmbedding.Subset
            };

            writer.BeginReport(96, 96);
            writer.BeginPage(210f, 297f);
            writer.BeginPageSection(new RectangleF(0f, 0f, 200f, 280f));

            draw(writer);

            writer.EndPageSection();
            writer.EndPage();

            // Font objects (the only thing these tests assert on) are written directly to the
            // stream before EndReport()'s document-map/catalog metadata, which depends on a
            // fully-processed RplReport this minimal harness doesn't have - same convention as
            // EmbeddedFontPdfTextTests.
            try
            {
                writer.EndReport();
            }
            catch (System.Exception)
            {
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
        public void DrawWrappedText_WithTtcBackedFont_DeclaresCIDFontType2WithFontFile2()
        {
            string pdf = RenderContentStream(writer =>
            {
                var style = new TestTextRunProps();
                writer.DrawWrappedText(new RectangleF(0f, 0f, 100f, 20f), PointF.Empty, "Hello", style, RPLFormat.TextAlignments.Left);
            });

            // SimSun's own outlines are TrueType (glyf), so once correctly extracted from the
            // .ttc container it must get the same declaration a plain .ttf font would.
            StringAssert.Contains(pdf, "/Subtype /Type0");
            StringAssert.Contains(pdf, "/Subtype /CIDFontType2");
            StringAssert.Contains(pdf, "/CIDToGIDMap /Identity");
            StringAssert.Contains(pdf, "/FontFile2");
        }

        [TestMethod]
        public void DrawWrappedText_WithTtcBackedFont_EmbedsExtractedSingleFaceNotWholeContainer()
        {
            string pdf = RenderContentStream(writer =>
            {
                var style = new TestTextRunProps();
                writer.DrawWrappedText(new RectangleF(0f, 0f, 100f, 20f), PointF.Empty, "Hello", style, RPLFormat.TextAlignments.Left);
            });

            Match match = Regex.Match(pdf, @"/Length1 (\d+)");
            Assert.IsTrue(match.Success, "Expected the embedded font stream's /Length1 (uncompressed font-program length) to be present");
            long embeddedLength = long.Parse(match.Groups[1].Value);

            using SKTypeface typeface = SKTypeface.FromFamilyName("SimSun");
            using SKStreamAsset stream = typeface.OpenStream(out int ttcIndex);
            using SKData data = SKData.Create(stream);
            byte[] rawContainer = data.ToArray();
            Assert.IsTrue(SfntBinaryUtils.IsTtc(rawContainer), "SimSun should still be backed by simsun.ttc on this dev box");
            Assert.IsTrue(SfntBinaryUtils.TryExtractTtcFace(rawContainer, ttcIndex, out byte[] extractedUnsubsetted));

            // Neither the whole multi-face container nor the extracted-but-unsubsetted single
            // face should have leaked into FontFile2 - only "Hello"'s handful of glyphs' worth
            // of outline data should remain, out of a face covering thousands of CJK glyphs.
            Assert.IsTrue(embeddedLength < rawContainer.Length, $"Embedded font ({embeddedLength} bytes) should be smaller than the whole .ttc container ({rawContainer.Length} bytes)");
            Assert.IsTrue(embeddedLength < extractedUnsubsetted.Length / 2, $"Embedded font ({embeddedLength} bytes) should be markedly smaller than the extracted-but-unsubsetted face ({extractedUnsubsetted.Length} bytes)");
        }

        [TestMethod]
        public void DrawWrappedText_WithTtcBackedFont_EmitsHexGlyphTj()
        {
            string pdf = RenderContentStream(writer =>
            {
                var style = new TestTextRunProps();
                writer.DrawWrappedText(new RectangleF(0f, 0f, 100f, 20f), PointF.Empty, "Hello", style, RPLFormat.TextAlignments.Left);
            });

            Assert.IsFalse(pdf.Contains("(Hello) Tj"), "A TTC-backed embedded font should draw via glyph-indexed hex Tj, same as any other embedded composite font");
            StringAssert.Contains(pdf, "> Tj");
        }
    }
}
