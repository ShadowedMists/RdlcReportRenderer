using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Microsoft.ReportingServices.Rendering.ImageRenderer;
using Microsoft.ReportingServices.Rendering.RichText;
using Microsoft.ReportingServices.Rendering.RPLProcessing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ReportViewer.Chart.Rdl.Tests
{
    /// <summary>
    /// Exercises ShapedTextMetrics/ShapedTextWrapper/ShapedStyledTextWrapper - the
    /// real-shaped-width replacement for the base-14 PDF text MVP's approximate
    /// character-class word-wrap, wired into PDFWriter.DrawWrappedText/
    /// DrawWrappedRichText (see tasks/pdf-text-shaping-abstraction.md, "production
    /// wiring" entry). Behavioral, not pixel-exact, per this repo's testing philosophy -
    /// asserts on character-preservation invariants and break-opportunity placement
    /// rather than exact float widths, since those depend on whatever font
    /// SKTypeface.FromFamilyName resolves to on the host.
    /// </summary>
    [TestClass]
    public class ShapedTextLayoutTests
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

        [TestMethod]
        public void Measure_EmptyText_ReturnsEmptyArrays()
        {
            using var fontCache = new ShapedFontCache();
            ShapedTextMetrics.Measure(string.Empty, "Arial", 12f, false, false, fontCache, out float[] widths, out bool[] canBreak);

            Assert.AreEqual(0, widths.Length);
            Assert.AreEqual(0, canBreak.Length);
        }

        [TestMethod]
        public void Measure_ProducesOneWidthEntryPerCharacter()
        {
            using var fontCache = new ShapedFontCache();
            string text = "Hello world";
            ShapedTextMetrics.Measure(text, "Arial", 12f, false, false, fontCache, out float[] widths, out bool[] canBreak);

            Assert.AreEqual(text.Length, widths.Length);
            Assert.AreEqual(text.Length, canBreak.Length);
        }

        [TestMethod]
        public void Measure_MarksSoftBreakOpportunityRightAfterWhitespace()
        {
            using var fontCache = new ShapedFontCache();
            string text = "Hello world";
            ShapedTextMetrics.Measure(text, "Arial", 12f, false, false, fontCache, out _, out bool[] canBreak);

            int spaceIndex = text.IndexOf(' ');
            Assert.IsTrue(canBreak[spaceIndex + 1], "Expected a break opportunity right after the space, matching UnicodeLineBreakAnalyzer's soft-break-after-whitespace rule");
        }

        [TestMethod]
        public void MeasureTotalWidthPoints_IsPositive_ForNonEmptyText()
        {
            using var fontCache = new ShapedFontCache();
            float width = ShapedTextMetrics.MeasureTotalWidthPoints("Hello", "Arial", 12f, false, false, fontCache);

            Assert.IsTrue(width > 0f);
        }

        [TestMethod]
        public void MeasureTotalWidthPoints_GrowsWithLongerText()
        {
            using var fontCache = new ShapedFontCache();
            float shortWidth = ShapedTextMetrics.MeasureTotalWidthPoints("Hi", "Arial", 12f, false, false, fontCache);
            float longWidth = ShapedTextMetrics.MeasureTotalWidthPoints("Hi there, this is longer", "Arial", 12f, false, false, fontCache);

            Assert.IsTrue(longWidth > shortWidth);
        }

        [TestMethod]
        public void Wrap_ShortText_FitsOnOneLine()
        {
            using var fontCache = new ShapedFontCache();
            List<string> lines = ShapedTextWrapper.Wrap("Hi", "Arial", 12f, false, false, 1000f, fontCache);

            Assert.AreEqual(1, lines.Count);
            Assert.AreEqual("Hi", lines[0]);
        }

        [TestMethod]
        public void Wrap_LongText_WrapsIntoMultipleLines_PreservingAllCharacters()
        {
            using var fontCache = new ShapedFontCache();
            string text = "The quick brown fox jumps over the lazy dog";
            float narrowWidth = ShapedTextMetrics.MeasureTotalWidthPoints("The quick", "Arial", 12f, false, false, fontCache);

            List<string> lines = ShapedTextWrapper.Wrap(text, "Arial", 12f, false, false, narrowWidth, fontCache);

            Assert.IsTrue(lines.Count > 1, "Expected the long sentence to wrap into more than one line given a narrow box");
            Assert.AreEqual(text, string.Join("\n", lines).Replace("\n", ""), "No characters should be lost or duplicated by wrapping");
        }

        [TestMethod]
        public void Wrap_BreaksOnlyAtSoftBreakPositions_NotMidWord()
        {
            using var fontCache = new ShapedFontCache();
            string text = "abcdefgh ijklmnop";
            // A width that forces a break somewhere in the first word if breaking were
            // allowed anywhere - but soft breaks only occur after whitespace/hyphen, so
            // the wrapper must let the first "word" overflow rather than split it.
            float narrowWidth = ShapedTextMetrics.MeasureTotalWidthPoints("abcd", "Arial", 12f, false, false, fontCache);

            List<string> lines = ShapedTextWrapper.Wrap(text, "Arial", 12f, false, false, narrowWidth, fontCache);

            Assert.IsTrue(lines[0].StartsWith("abcdefgh"), "The first unbreakable word should not be split mid-word even though it exceeds the box width");
        }

        [TestMethod]
        public void Wrap_RespectsExplicitNewlines()
        {
            using var fontCache = new ShapedFontCache();
            List<string> lines = ShapedTextWrapper.Wrap("First\nSecond", "Arial", 12f, false, false, 1000f, fontCache);

            Assert.AreEqual(2, lines.Count);
            Assert.AreEqual("First", lines[0]);
            Assert.AreEqual("Second", lines[1]);
        }

        [TestMethod]
        public void WrapParagraph_ShortSingleRun_ProducesOneLineOneFragment()
        {
            using var fontCache = new ShapedFontCache();
            var style = new TestTextRunProps();
            var runs = new List<(string Text, ITextRunProps Style)> { ("Hello", style) };

            List<List<StyledLineFragment>> lines = ShapedStyledTextWrapper.WrapParagraph(runs, 1000f, fontCache);

            Assert.AreEqual(1, lines.Count);
            Assert.AreEqual(1, lines[0].Count);
            Assert.AreEqual("Hello", lines[0][0].Text);
        }

        [TestMethod]
        public void WrapParagraph_BreaksAcrossRunBoundary_PreservingAllCharacters()
        {
            using var fontCache = new ShapedFontCache();
            var bold = new TestTextRunProps { Bold = true };
            var normal = new TestTextRunProps();
            var runs = new List<(string Text, ITextRunProps Style)>
            {
                ("Bold prefix ", bold),
                ("normal suffix that is long enough to wrap", normal)
            };

            float narrowWidth = ShapedTextMetrics.MeasureTotalWidthPoints("Bold prefix normal", "Arial", 12f, false, false, fontCache);
            List<List<StyledLineFragment>> lines = ShapedStyledTextWrapper.WrapParagraph(runs, narrowWidth, fontCache);

            Assert.IsTrue(lines.Count > 1, "Expected wrapping across the run boundary given a narrow box");

            var reconstructed = new StringBuilder();
            for (int i = 0; i < lines.Count; i++)
            {
                foreach (StyledLineFragment fragment in lines[i])
                {
                    reconstructed.Append(fragment.Text);
                }
                if (i < lines.Count - 1)
                {
                    reconstructed.Append('\n');
                }
            }

            string expected = ("Bold prefix " + "normal suffix that is long enough to wrap").Replace("\n", "");
            Assert.AreEqual(expected, reconstructed.ToString().Replace("\n", ""), "No characters should be lost or duplicated when wrapping across a run boundary");
        }

        [TestMethod]
        public void WrapParagraph_MergesAdjacentSameStyleFragments()
        {
            using var fontCache = new ShapedFontCache();
            var style = new TestTextRunProps();
            var runs = new List<(string Text, ITextRunProps Style)>
            {
                ("Hello ", style),
                ("world", style)
            };

            List<List<StyledLineFragment>> lines = ShapedStyledTextWrapper.WrapParagraph(runs, 1000f, fontCache);

            Assert.AreEqual(1, lines.Count);
            Assert.AreEqual(1, lines[0].Count, "Adjacent fragments sharing the same style instance should merge into one");
            Assert.AreEqual("Hello world", lines[0][0].Text);
        }

        [TestMethod]
        public void WrapParagraph_ForcedBreakOnExplicitNewlineWithinRun()
        {
            using var fontCache = new ShapedFontCache();
            var style = new TestTextRunProps();
            var runs = new List<(string Text, ITextRunProps Style)> { ("First\nSecond", style) };

            List<List<StyledLineFragment>> lines = ShapedStyledTextWrapper.WrapParagraph(runs, 1000f, fontCache);

            Assert.AreEqual(2, lines.Count);
            Assert.AreEqual("First", lines[0][0].Text);
            Assert.AreEqual("Second", lines[1][0].Text);
        }
    }
}
