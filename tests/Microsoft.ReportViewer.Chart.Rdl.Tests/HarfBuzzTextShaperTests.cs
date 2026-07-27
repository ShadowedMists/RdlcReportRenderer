using Microsoft.ReportingServices.Rendering.RichText;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ReportViewer.Chart.Rdl.Tests
{
    /// <summary>
    /// Exercises HarfBuzzTextShaper - the prototype P4 "shaping layer" translation
    /// (tasks/pdf-text-shaping-abstraction.md) that repackages HarfBuzzSharp's shaped
    /// glyph output into the same GlyphData/GlyphShapeData/ABC/GOFFSET shapes Win32's
    /// ScriptShape/ScriptPlace already produce for TextRun. Not yet wired into
    /// TextRun/FontCache - see that class's doc comment for scope and known gaps
    /// (RTL/ligature clustering, SCRIPT_VISATTR, fallback fonts are all out of scope for
    /// this prototype).
    /// </summary>
    [TestClass]
    public class HarfBuzzTextShaperTests
    {
        [TestMethod]
        public void PlainLatinText_ShapesOneGlyphPerCharacter()
        {
            using var font = new SkiaCachedFont("Arial", 16f, bold: false, italic: false);

            GlyphData glyphData = HarfBuzzTextShaper.Shape("Hello", font);

            Assert.AreEqual(5, glyphData.GlyphScriptShapeData.GlyphCount, "Plain non-ligated Latin text should shape 1 glyph per character");
        }

        [TestMethod]
        public void PlainLatinText_ProducesPositiveAdvancesAndTotalWidth()
        {
            using var font = new SkiaCachedFont("Arial", 16f, bold: false, italic: false);

            GlyphData glyphData = HarfBuzzTextShaper.Shape("Hello", font);

            int[] advances = glyphData.RawAdvances;
            Assert.AreEqual(5, advances.Length);
            foreach (int advance in advances)
            {
                Assert.IsTrue(advance > 0, "Every glyph in plain Latin text should have a positive advance");
            }

            Assert.IsTrue(glyphData.ABC.Width > 0, "Total run width should be positive");

            int sumOfAdvances = 0;
            foreach (int advance in advances)
            {
                sumOfAdvances += advance;
            }
            Assert.AreEqual(sumOfAdvances, glyphData.ABC.Width, "ABC.Width should equal the sum of individual glyph advances for this prototype's all-in-abcB convention");
        }

        [TestMethod]
        public void ClusterMapping_CoversEveryCharacterWithAValidGlyphIndex()
        {
            using var font = new SkiaCachedFont("Arial", 16f, bold: false, italic: false);
            const string text = "Report";

            GlyphData glyphData = HarfBuzzTextShaper.Shape(text, font);
            short[] clusters = glyphData.GlyphScriptShapeData.Clusters;

            Assert.AreEqual(text.Length, clusters.Length);
            foreach (short glyphIndex in clusters)
            {
                Assert.IsTrue(glyphIndex >= 0 && glyphIndex < glyphData.GlyphScriptShapeData.GlyphCount,
                    "Every character's cluster mapping should point at a real glyph index");
            }
        }

        [TestMethod]
        public void LongerText_ProducesMonotonicWidthGrowth()
        {
            using var font = new SkiaCachedFont("Arial", 16f, bold: false, italic: false);

            GlyphData shortText = HarfBuzzTextShaper.Shape("Hi", font);
            GlyphData longerText = HarfBuzzTextShaper.Shape("Hi there", font);

            Assert.IsTrue(longerText.ABC.Width > shortText.ABC.Width, "More text should shape to a wider total run");
        }

        [TestMethod]
        public void EmptyText_ProducesZeroGlyphsWithoutThrowing()
        {
            using var font = new SkiaCachedFont("Arial", 16f, bold: false, italic: false);

            GlyphData glyphData = HarfBuzzTextShaper.Shape(string.Empty, font);

            Assert.AreEqual(0, glyphData.GlyphScriptShapeData.GlyphCount);
        }

        [TestMethod]
        public void GOffsets_AreProducedForEveryGlyph()
        {
            using var font = new SkiaCachedFont("Arial", 16f, bold: false, italic: false);

            GlyphData glyphData = HarfBuzzTextShaper.Shape("Test", font);

            Assert.AreEqual(glyphData.GlyphScriptShapeData.GlyphCount, glyphData.RawGOffsets.Length);
        }

        [TestMethod]
        public void PlainLatinText_ReportsNoMissingGlyph()
        {
            using var font = new SkiaCachedFont("Arial", 16f, bold: false, italic: false);

            HarfBuzzTextShaper.Shape("Hello", font, out int missingGlyphCodepoint);

            Assert.AreEqual(-1, missingGlyphCodepoint, "Arial covers plain Latin text - nothing should be reported missing");
        }

        [TestMethod]
        public void CjkCharacterAgainstLatinOnlyFont_ReportsItsCodepointAsMissing()
        {
            // Arial has no CJK coverage, so shaping a Han character against it should shape
            // to .notdef (glyph id 0) and report that character's codepoint back - the
            // signal TextRun.ShapeAndPlaceCrossPlatform uses to trigger a fallback-font retry.
            using var font = new SkiaCachedFont("Arial", 16f, bold: false, italic: false);
            const string text = "A中B";

            HarfBuzzTextShaper.Shape(text, font, out int missingGlyphCodepoint);

            Assert.AreEqual((int)'中', missingGlyphCodepoint);
        }

        [TestMethod]
        public void WhitespaceShapingToNotdef_IsNotReportedAsMissing()
        {
            using var font = new SkiaCachedFont("Arial", 16f, bold: false, italic: false);

            HarfBuzzTextShaper.Shape("Hello World", font, out int missingGlyphCodepoint);

            Assert.AreEqual(-1, missingGlyphCodepoint, "A missing glyph is only meaningful for non-whitespace/control characters");
        }
    }
}
