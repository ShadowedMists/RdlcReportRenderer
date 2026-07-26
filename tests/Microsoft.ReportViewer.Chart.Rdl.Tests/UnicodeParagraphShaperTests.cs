using System.Collections.Generic;
using Microsoft.ReportingServices.Rendering.RichText;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ReportViewer.Chart.Rdl.Tests
{
    /// <summary>
    /// Exercises UnicodeParagraphShaper - the composed itemize -> line-break -> shape
    /// pipeline (tasks/pdf-text-shaping-abstraction.md, P4 step 3) that ties
    /// UnicodeTextItemizer, UnicodeLineBreakAnalyzer, and HarfBuzzTextShaper together.
    /// Confirms the three prototypes' outputs actually compose (no gaps/overlaps
    /// between items, each item's shaped text matches its slice of the paragraph, each
    /// item's line-break attributes are sliced correctly) - not yet wired into
    /// LineBreaker/Paragraph/TextRun, see that class's doc comment for what's still
    /// missing before production wiring.
    /// </summary>
    [TestClass]
    public class UnicodeParagraphShaperTests
    {
        [TestMethod]
        public void PlainLatinText_ProducesOneShapedRunCoveringTheWholeString()
        {
            using var font = new SkiaCachedFont("Arial", 16f, bold: false, italic: false);
            const string text = "Hello, world!";

            List<ShapedRunItem> runs = UnicodeParagraphShaper.Shape(text, font);

            Assert.AreEqual(1, runs.Count);
            Assert.AreEqual(0, runs[0].CharPos);
            Assert.AreEqual(text.Length, runs[0].Length);
            Assert.AreEqual(text.Length, runs[0].GlyphData.GlyphScriptShapeData.GlyphCount);
        }

        [TestMethod]
        public void MixedScriptText_ProducesRunsThatAreContiguousAndGapFree()
        {
            using var font = new SkiaCachedFont("Arial", 16f, bold: false, italic: false);
            const string text = "Hello Привет again";

            List<ShapedRunItem> runs = UnicodeParagraphShaper.Shape(text, font);

            Assert.IsTrue(runs.Count >= 2, "Mixed-script text should produce more than one run");
            int expectedNextStart = 0;
            foreach (ShapedRunItem run in runs)
            {
                Assert.AreEqual(expectedNextStart, run.CharPos, "Runs should be contiguous with no gaps or overlaps");
                expectedNextStart += run.Length;
            }
            Assert.AreEqual(text.Length, expectedNextStart, "Runs should cover the entire paragraph");
        }

        [TestMethod]
        public void EachRun_HasLineBreakAttributesMatchingItsOwnLength()
        {
            using var font = new SkiaCachedFont("Arial", 16f, bold: false, italic: false);
            const string text = "Hello Привет again";

            List<ShapedRunItem> runs = UnicodeParagraphShaper.Shape(text, font);

            foreach (ShapedRunItem run in runs)
            {
                Assert.AreEqual(run.Length, run.ScriptLogAttr.Length, "Each run's ScriptLogAttr slice should have exactly one entry per character in that run");
            }
        }

        [TestMethod]
        public void FirstRun_LineBreakAttributesMatchTheParagraphs()
        {
            using var font = new SkiaCachedFont("Arial", 16f, bold: false, italic: false);
            const string text = "one two";

            List<ShapedRunItem> runs = UnicodeParagraphShaper.Shape(text, font);
            SCRIPT_LOGATTR[] wholeParagraph = UnicodeLineBreakAnalyzer.Analyze(text);

            Assert.AreEqual(1, runs.Count);
            for (int i = 0; i < runs[0].Length; i++)
            {
                Assert.AreEqual(wholeParagraph[i].IsWhiteSpace, runs[0].ScriptLogAttr[i].IsWhiteSpace);
                Assert.AreEqual(wholeParagraph[i].IsSoftBreak, runs[0].ScriptLogAttr[i].IsSoftBreak);
            }
        }

        [TestMethod]
        public void EachRun_ShapesToAtLeastOneGlyph()
        {
            using var font = new SkiaCachedFont("Arial", 16f, bold: false, italic: false);
            const string text = "Mix Смесь more";

            List<ShapedRunItem> runs = UnicodeParagraphShaper.Shape(text, font);

            foreach (ShapedRunItem run in runs)
            {
                Assert.IsTrue(run.GlyphData.GlyphScriptShapeData.GlyphCount > 0, "Every non-empty run should shape to at least one glyph");
            }
        }

        [TestMethod]
        public void EmptyText_ProducesNoRuns()
        {
            using var font = new SkiaCachedFont("Arial", 16f, bold: false, italic: false);

            List<ShapedRunItem> runs = UnicodeParagraphShaper.Shape(string.Empty, font);

            Assert.AreEqual(0, runs.Count);
        }

        [TestMethod]
        public void PureHebrewText_ProducesOneRunMarkedRtl()
        {
            using var font = new SkiaCachedFont("Arial", 16f, bold: false, italic: false);
            const string text = "שלום עולם";

            List<ShapedRunItem> runs = UnicodeParagraphShaper.Shape(text, font);

            Assert.AreEqual(1, runs.Count);
            Assert.IsTrue((runs[0].Analysis.word1 & (1 << 10)) != 0, "Hebrew run should be marked RTL");
        }

        [TestMethod]
        public void LtrBase_SingleEmbeddedRtlWord_KeepsItsLogicalPosition()
        {
            // A lone RTL island inside an LTR paragraph doesn't need its run-order changed -
            // only its own glyphs (already visual-order via HarfBuzz) are affected.
            using var font = new SkiaCachedFont("Arial", 16f, bold: false, italic: false);
            const string text = "Hello שלום world";

            List<ShapedRunItem> runs = UnicodeParagraphShaper.Shape(text, font);

            Assert.IsTrue(runs.Count >= 3, "Expected at least [Latin][Hebrew][Latin] runs");
            Assert.AreEqual(0, runs[0].CharPos, "First visual run should still be the leading Latin text");
            int hebrewCharPos = text.IndexOf('ש');
            bool foundHebrewInMiddle = false;
            for (int i = 1; i < runs.Count - 1; i++)
            {
                if (runs[i].CharPos == hebrewCharPos)
                {
                    foundHebrewInMiddle = true;
                }
            }
            Assert.IsTrue(foundHebrewInMiddle, "Hebrew run should remain in the middle visual position");
        }

        [TestMethod]
        public void RtlBase_TrailingLatinWord_DrawsBeforeTheRtlRunVisually()
        {
            // A paragraph whose first strong character is RTL (Hebrew) establishes an RTL
            // base direction; a trailing embedded LTR word visually sits to its left, so it
            // must appear first in the returned (visual-order) run list.
            using var font = new SkiaCachedFont("Arial", 16f, bold: false, italic: false);
            const string text = "שלום world";

            List<ShapedRunItem> runs = UnicodeParagraphShaper.Shape(text, font);

            Assert.AreEqual(2, runs.Count);
            Assert.IsTrue(runs[0].CharPos > runs[1].CharPos, "The logically-later Latin run should be drawn first");
            Assert.IsFalse((runs[0].Analysis.word1 & (1 << 10)) != 0, "First visual run should be the Latin (non-RTL) run");
            Assert.IsTrue((runs[1].Analysis.word1 & (1 << 10)) != 0, "Second visual run should be the Hebrew (RTL) run");
        }
    }
}
