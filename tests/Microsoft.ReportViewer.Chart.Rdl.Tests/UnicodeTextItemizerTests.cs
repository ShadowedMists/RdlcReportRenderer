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
        public void ThaiText_IsItsOwnLtrScriptBucket()
        {
            const string thaiText = "สวัสดี";

            List<TextItem> items = UnicodeTextItemizer.Itemize(thaiText);

            Assert.AreEqual(1, items.Count);
            Assert.AreEqual(UnicodeScriptKind.Thai, items[0].Script);
            var analysis = new ScriptAnalysis(items[0].Analysis.word1);
            Assert.AreEqual(0, analysis.fRTL, "Thai is a left-to-right script");
        }

        [TestMethod]
        public void DevanagariText_IsItsOwnLtrScriptBucket()
        {
            const string devanagariText = "नमस्ते";

            List<TextItem> items = UnicodeTextItemizer.Itemize(devanagariText);

            Assert.AreEqual(1, items.Count);
            Assert.AreEqual(UnicodeScriptKind.Devanagari, items[0].Script);
            var analysis = new ScriptAnalysis(items[0].Analysis.word1);
            Assert.AreEqual(0, analysis.fRTL, "Devanagari is a left-to-right script");
        }

        [TestMethod]
        public void HangulText_IsItsOwnLtrScriptBucket()
        {
            const string hangulText = "안녕하세요";

            List<TextItem> items = UnicodeTextItemizer.Itemize(hangulText);

            Assert.AreEqual(1, items.Count);
            Assert.AreEqual(UnicodeScriptKind.Hangul, items[0].Script);
            var analysis = new ScriptAnalysis(items[0].Analysis.word1);
            Assert.AreEqual(0, analysis.fRTL, "Hangul is a left-to-right script");
        }

        [TestMethod]
        public void SyriacText_IsItemizedAsRtl()
        {
            const string syriacText = "ܫܠܡܐ";

            List<TextItem> items = UnicodeTextItemizer.Itemize(syriacText);

            Assert.AreEqual(1, items.Count);
            Assert.AreEqual(UnicodeScriptKind.Syriac, items[0].Script);
            var analysis = new ScriptAnalysis(items[0].Analysis.word1);
            Assert.AreEqual(1, analysis.fRTL, "Syriac is a right-to-left script - previously mis-itemized as Other/LTR");
        }

        [TestMethod]
        public void ThaanaText_IsItemizedAsRtl()
        {
            const string thaanaText = "ދިވެހި";

            List<TextItem> items = UnicodeTextItemizer.Itemize(thaanaText);

            Assert.AreEqual(1, items.Count);
            Assert.AreEqual(UnicodeScriptKind.Thaana, items[0].Script);
            var analysis = new ScriptAnalysis(items[0].Analysis.word1);
            Assert.AreEqual(1, analysis.fRTL, "Thaana is a right-to-left script - previously mis-itemized as Other/LTR");
        }

        [TestMethod]
        public void NKoText_IsItemizedAsRtl()
        {
            // U+07C0-U+07C9 are NKo digits (Unicode category Nd), which classify as
            // Common, not NKo - use letters (U+07CA onward) so the run actually gets its
            // own NKo-bucketed item instead of falling back to the Common/Latin default.
            string nkoText = "ߊߋߌߍ";

            List<TextItem> items = UnicodeTextItemizer.Itemize(nkoText);

            Assert.AreEqual(1, items.Count);
            Assert.AreEqual(UnicodeScriptKind.NKo, items[0].Script);
            var analysis = new ScriptAnalysis(items[0].Analysis.word1);
            Assert.AreEqual(1, analysis.fRTL, "N'Ko is a right-to-left script - previously mis-itemized as Other/LTR");
        }

        [TestMethod]
        public void SamaritanText_IsItemizedAsRtl()
        {
            string samaritanText = "ࠀࠁࠂࠃ";

            List<TextItem> items = UnicodeTextItemizer.Itemize(samaritanText);

            Assert.AreEqual(1, items.Count);
            Assert.AreEqual(UnicodeScriptKind.Samaritan, items[0].Script);
            var analysis = new ScriptAnalysis(items[0].Analysis.word1);
            Assert.AreEqual(1, analysis.fRTL, "Samaritan is a right-to-left script - previously mis-itemized as Other/LTR");
        }

        [TestMethod]
        public void MandaicText_IsItemizedAsRtl()
        {
            string mandaicText = "ࡀࡁࡂࡃ";

            List<TextItem> items = UnicodeTextItemizer.Itemize(mandaicText);

            Assert.AreEqual(1, items.Count);
            Assert.AreEqual(UnicodeScriptKind.Mandaic, items[0].Script);
            var analysis = new ScriptAnalysis(items[0].Analysis.word1);
            Assert.AreEqual(1, analysis.fRTL, "Mandaic is a right-to-left script - previously mis-itemized as Other/LTR");
        }

        [TestMethod]
        public void AdlamText_IsItemizedAsRtl()
        {
            // Adlam (U+1E900-U+1E95F) lives outside the Basic Multilingual Plane, so each
            // letter here is a UTF-16 surrogate pair, not a single char - four Adlam
            // letters is 8 chars long.
            string adlamText = "\U0001E900\U0001E901\U0001E902\U0001E903";

            List<TextItem> items = UnicodeTextItemizer.Itemize(adlamText);

            Assert.AreEqual(1, items.Count);
            Assert.AreEqual(0, items[0].CharPos);
            Assert.AreEqual(8, items[0].Length, "Four Adlam codepoints should span 8 UTF-16 chars (surrogate pairs)");
            Assert.AreEqual(UnicodeScriptKind.Adlam, items[0].Script);
            var analysis = new ScriptAnalysis(items[0].Analysis.word1);
            Assert.AreEqual(1, analysis.fRTL, "Adlam is a right-to-left script - previously mis-itemized as Other/LTR");
        }

        [TestMethod]
        public void MixedLatinAndAdlamText_ProducesTwoItemsAtTheCorrectSurrogatePairBoundary()
        {
            const string latinPrefix = "Hi ";
            string text = latinPrefix + "\U0001E900\U0001E901";

            List<TextItem> items = UnicodeTextItemizer.Itemize(text);

            Assert.AreEqual(2, items.Count);
            Assert.AreEqual(UnicodeScriptKind.Latin, items[0].Script);
            Assert.AreEqual(latinPrefix.Length, items[0].Length);
            Assert.AreEqual(UnicodeScriptKind.Adlam, items[1].Script);
            Assert.AreEqual(4, items[1].Length, "Two Adlam codepoints should span 4 UTF-16 chars (surrogate pairs)");
            Assert.AreEqual(text.Length, items[0].Length + items[1].Length, "Items should cover the whole string with no gaps, even across a surrogate-pair boundary");
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
