using System.Collections.Generic;
using Microsoft.ReportingServices.Rendering.RichText;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ReportViewer.Chart.Rdl.Tests
{
    /// <summary>
    /// Exercises UnicodeTextItemizer - the prototype P4 "itemization" step
    /// (tasks/pdf-text-shaping-abstraction.md) that splits text into script/direction
    /// runs the way Paragraph.ScriptItemize does via Win32.ScriptItemize, using coarse
    /// Unicode-range checks instead of Uniscribe's script table. Not wired into
    /// production - see that class's doc comment for scope and gaps.
    /// </summary>
    [TestClass]
    public class UnicodeTextItemizerTests
    {
        [TestMethod]
        public void PlainLatinText_ProducesOneItem()
        {
            List<TextItem> items = UnicodeTextItemizer.Itemize("Hello, world!");

            Assert.AreEqual(1, items.Count);
            Assert.AreEqual(0, items[0].CharPos);
            Assert.AreEqual(13, items[0].Length);
            Assert.AreEqual(UnicodeScriptKind.Latin, items[0].Script);
        }

        [TestMethod]
        public void MixedLatinAndCyrillicText_ProducesTwoItems()
        {
            const string text = "Hello Привет";

            List<TextItem> items = UnicodeTextItemizer.Itemize(text);

            Assert.AreEqual(2, items.Count);
            Assert.AreEqual(UnicodeScriptKind.Latin, items[0].Script);
            Assert.AreEqual(UnicodeScriptKind.Cyrillic, items[1].Script);
            Assert.AreEqual(text.Length, items[0].Length + items[1].Length, "Items should cover the whole string with no gaps");
        }

        [TestMethod]
        public void CommonCharacters_MergeIntoTheSurroundingScriptRunWithoutForcingANewItem()
        {
            List<TextItem> items = UnicodeTextItemizer.Itemize("Report #123, please.");

            Assert.AreEqual(1, items.Count, "Digits/punctuation/whitespace should not force new items on their own");
            Assert.AreEqual(UnicodeScriptKind.Latin, items[0].Script);
        }

        [TestMethod]
        public void RtlScript_SetsFRtlOnTheConstructedScriptAnalysis()
        {
            const string hebrewText = "שלום";

            List<TextItem> items = UnicodeTextItemizer.Itemize(hebrewText);

            Assert.AreEqual(1, items.Count);
            Assert.AreEqual(UnicodeScriptKind.Hebrew, items[0].Script);
            var analysis = new ScriptAnalysis(items[0].Analysis.word1);
            Assert.AreEqual(1, analysis.fRTL, "Hebrew text should be marked RTL in the constructed SCRIPT_ANALYSIS");
        }

        [TestMethod]
        public void LatinScript_DoesNotSetFRtl()
        {
            List<TextItem> items = UnicodeTextItemizer.Itemize("Hello");

            var analysis = new ScriptAnalysis(items[0].Analysis.word1);
            Assert.AreEqual(0, analysis.fRTL);
        }

        [TestMethod]
        public void EmptyText_ProducesNoItems()
        {
            List<TextItem> items = UnicodeTextItemizer.Itemize(string.Empty);

            Assert.AreEqual(0, items.Count);
        }

        [TestMethod]
        public void ItemsCoverTheEntireStringContiguously()
        {
            const string text = "Mix Смесь текст more Latin";

            List<TextItem> items = UnicodeTextItemizer.Itemize(text);

            int coveredLength = 0;
            int expectedNextStart = 0;
            foreach (TextItem item in items)
            {
                Assert.AreEqual(expectedNextStart, item.CharPos, "Items should be contiguous with no gaps or overlaps");
                expectedNextStart += item.Length;
                coveredLength += item.Length;
            }
            Assert.AreEqual(text.Length, coveredLength);
        }
    }
}
