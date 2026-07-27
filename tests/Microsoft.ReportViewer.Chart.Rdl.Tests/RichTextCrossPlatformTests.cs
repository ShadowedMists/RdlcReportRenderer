using System.Drawing;
using Microsoft.ReportingServices.Rendering.RichText;
using Microsoft.ReportingServices.Rendering.RPLProcessing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ReportViewer.Chart.Rdl.Tests
{
    /// <summary>
    /// Exercises the cross-platform (!OperatingSystem.IsWindows()) branches added to the
    /// real RichText production pipeline - TextRun.ShapeAndPlaceCrossPlatform,
    /// Paragraph.ScriptItemizeCrossPlatform, TextLine.ScriptLayoutCrossPlatform - per
    /// tasks/pdf-text-shaping-abstraction.md's "production wiring" item (a). These are
    /// the same FontCache/TextRun/Paragraph/TextLine classes PDFWriter's Win32-only real
    /// pipeline, the WinForms viewer, and ImageWriter all share (not the separate
    /// base-14/embedded-font MVP bypass in PDFWriter.DrawWrappedText/DrawWrappedRichText).
    ///
    /// This dev/test box is Windows, so the internal *CrossPlatform methods are called
    /// directly - the same convention CrossPlatformPdfTextTests.cs already established for
    /// PDFWriter's own platform-gated methods - rather than through the public
    /// ShapeAndPlace/ScriptItemize/ScriptLayout entry points, which would take the real
    /// Win32 branch on this host regardless of what's being tested.
    /// </summary>
    [TestClass]
    public class RichTextCrossPlatformTests
    {
        private sealed class FakeTextRunProps : ITextRunProps
        {
            public string FontFamily { get; set; } = "Arial";
            public float FontSize { get; set; } = 16f;
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
        public void ShapeAndPlaceCrossPlatform_ProducesRealGlyphDataAndSkiaFont()
        {
            FontCache fontCache = new FontCache(96f);
            TextRun run = new TextRun("Hello", new FakeTextRunProps());

            run.ShapeAndPlaceCrossPlatform(fontCache);

            Assert.IsNotNull(run.GlyphData);
            Assert.AreEqual(5, run.GlyphData.GlyphScriptShapeData.GlyphCount);
            Assert.IsNotNull(run.CachedFont);
            Assert.IsNotNull(run.CachedFont.SkiaFont, "FontCache.CreateFont should resolve a SkiaCachedFont, not a GDI+ Font/HFONT, on this path");
            Assert.IsNull(run.CachedFont.Font, "No live GDI+ Font should be constructed on the cross-platform path");
        }

        [TestMethod]
        public void ShapeAndPlaceCrossPlatform_WidthAndHeightAreQueryableWithoutAnHdc()
        {
            FontCache fontCache = new FontCache(96f);
            TextRun run = new TextRun("Hello world", new FakeTextRunProps());

            run.ShapeAndPlaceCrossPlatform(fontCache);

            int width = run.GetWidth(Win32DCSafeHandle.Zero, fontCache);
            int height = run.GetHeight(Win32DCSafeHandle.Zero, fontCache);
            int ascent = run.GetAscent(Win32DCSafeHandle.Zero, fontCache);
            int descent = run.GetDescent(Win32DCSafeHandle.Zero, fontCache);

            Assert.IsTrue(width > 0, "Shaped text should have positive width");
            Assert.IsTrue(height > 0, "Font metrics should report positive height");
            Assert.IsTrue(ascent > 0, "Font metrics should report positive ascent");
            Assert.IsTrue(descent >= 0, "Font metrics should report non-negative descent");
        }

        [TestMethod]
        public void ShapeAndPlaceCrossPlatform_ReusesCachedFontAcrossRunsWithSameStyle()
        {
            FontCache fontCache = new FontCache(96f);
            TextRun run1 = new TextRun("First", new FakeTextRunProps());
            TextRun run2 = new TextRun("Second", new FakeTextRunProps());

            run1.ShapeAndPlaceCrossPlatform(fontCache);
            run2.ShapeAndPlaceCrossPlatform(fontCache);

            Assert.AreSame(run1.CachedFont, run2.CachedFont, "FontCache should cache-and-reuse the same CachedFont for identically-styled runs");
        }

        [TestMethod]
        public void GetLogicalWidthsCrossPlatform_PlainLatinText_SumsToTotalRunWidth()
        {
            FontCache fontCache = new FontCache(96f);
            TextRun run = new TextRun("Hello world", new FakeTextRunProps());
            run.ShapeAndPlaceCrossPlatform(fontCache);

            int[] widths = run.GetLogicalWidthsCrossPlatform(run.GlyphData);

            Assert.AreEqual("Hello world".Length, widths.Length);
            int sum = 0;
            foreach (int w in widths)
            {
                Assert.IsTrue(w >= 0, "No character should get a negative logical width");
                sum += w;
            }
            Assert.AreEqual(run.GetWidth(Win32DCSafeHandle.Zero, fontCache), sum, "Per-character widths should sum to the run's total shaped width");
        }

        [TestMethod]
        public void GetLogicalWidthsCrossPlatform_EmptyText_ReturnsEmptyArray()
        {
            FontCache fontCache = new FontCache(96f);
            TextRun run = new TextRun(string.Empty, new FakeTextRunProps());
            run.ShapeAndPlaceCrossPlatform(fontCache);

            int[] widths = run.GetLogicalWidthsCrossPlatform(run.GlyphData);

            Assert.AreEqual(0, widths.Length);
        }

        [TestMethod]
        public void ScriptItemizeCrossPlatform_SingleScriptParagraph_AssignsBreakPositionsToEveryRun()
        {
            Paragraph paragraph = new Paragraph();
            paragraph.Runs.Add(new TextRun("Hello world", new FakeTextRunProps()));

            paragraph.ScriptItemizeCrossPlatform();

            Assert.AreEqual(1, paragraph.Runs.Count, "Plain Latin text is one script item - itemization should not split the run");
            TextRun run = paragraph.Runs[0];
            Assert.IsNotNull(run.ScriptLogAttr);
            Assert.AreEqual("Hello world".Length, run.ScriptLogAttr.Length);
            Assert.IsTrue(run.ScriptLogAttr[6].IsSoftBreak, "A soft-break opportunity should be recorded right after the space (index 5)");
            Assert.IsTrue(run.ScriptLogAttr[5].IsWhiteSpace, "The space character itself should be flagged whitespace");
        }

        [TestMethod]
        public void ScriptItemizeCrossPlatform_MixedScriptParagraph_SplitsIntoRtlAndLtrItems()
        {
            Paragraph paragraph = new Paragraph();
            paragraph.Runs.Add(new TextRun("Hello שלום", new FakeTextRunProps()));

            paragraph.ScriptItemizeCrossPlatform();

            Assert.IsTrue(paragraph.Runs.Count >= 2, "A Latin run followed by a Hebrew run should itemize into at least 2 items");
            ScriptAnalysis firstAnalysis = new ScriptAnalysis(paragraph.Runs[0].SCRIPT_ANALYSIS.word1);
            ScriptAnalysis lastAnalysis = new ScriptAnalysis(paragraph.Runs[paragraph.Runs.Count - 1].SCRIPT_ANALYSIS.word1);
            Assert.AreEqual(0, firstAnalysis.fRTL, "The Latin item should not be flagged RTL");
            Assert.AreEqual(1, lastAnalysis.fRTL, "The Hebrew item should be flagged RTL");

            int totalChars = 0;
            foreach (TextRun run in paragraph.Runs)
            {
                Assert.IsNotNull(run.ScriptLogAttr);
                Assert.AreEqual(run.Text.Length, run.ScriptLogAttr.Length);
                totalChars += run.Text.Length;
            }
            Assert.AreEqual("Hello שלום".Length, totalChars, "Itemization must preserve every character exactly once");
        }

        [TestMethod]
        public void ScriptLayoutCrossPlatform_AllLtrRuns_PreservesOrder()
        {
            FontCache fontCache = new FontCache(96f);
            TextRun run1 = new TextRun("Hello ", new FakeTextRunProps());
            TextRun run2 = new TextRun("world", new FakeTextRunProps());
            run1.ShapeAndPlaceCrossPlatform(fontCache);
            run2.ShapeAndPlaceCrossPlatform(fontCache);

            TextLine line = new TextLine();
            line.LogicalRuns.Add(run1);
            line.LogicalRuns.Add(run2);

            line.ScriptLayoutCrossPlatform();

            Assert.AreEqual(2, line.VisualRuns.Count);
            Assert.AreSame(run1, line.VisualRuns[0]);
            Assert.AreSame(run2, line.VisualRuns[1]);
        }

        [TestMethod]
        public void ScriptLayoutCrossPlatform_RtlBaseWithTrailingLtrRun_ReversesRunOrder()
        {
            FontCache fontCache = new FontCache(96f);
            TextRun rtlRun = new TextRun("שלום", new FakeTextRunProps());
            TextRun ltrRun = new TextRun("world", new FakeTextRunProps());
            ScriptAnalysis rtlAnalysis = new ScriptAnalysis(0) { fRTL = 1, fLayoutRTL = 1, s = new ScriptState() };
            rtlRun.SCRIPT_ANALYSIS = rtlAnalysis.GetAs_SCRIPT_ANALYSIS();
            ScriptAnalysis ltrAnalysis = new ScriptAnalysis(0) { fRTL = 0, fLayoutRTL = 0, s = new ScriptState() };
            ltrRun.SCRIPT_ANALYSIS = ltrAnalysis.GetAs_SCRIPT_ANALYSIS();
            rtlRun.ShapeAndPlaceCrossPlatform(fontCache);
            ltrRun.ShapeAndPlaceCrossPlatform(fontCache);

            TextLine line = new TextLine();
            line.LogicalRuns.Add(rtlRun);
            line.LogicalRuns.Add(ltrRun);

            line.ScriptLayoutCrossPlatform();

            Assert.AreEqual(2, line.VisualRuns.Count);
            Assert.AreSame(ltrRun, line.VisualRuns[0], "An RTL-base paragraph with a trailing LTR run should draw the LTR run first (matches BidiRunReordererTests' equivalent case)");
            Assert.AreSame(rtlRun, line.VisualRuns[1]);
            Assert.AreSame(rtlRun, line.LogicalRuns[0], "Logical order must stay untouched - only the visual copy is reordered");
        }

        [TestMethod]
        public void ShapeAndPlaceCrossPlatform_MissingGlyph_RetriesWithFallbackFontAndFlagsIt()
        {
            FontCache fontCache = new FontCache(96f);
            // Arial has no CJK coverage - this should trigger GetFallbackFontCrossPlatform's
            // SKFontManager.MatchCharacter retry rather than silently drawing .notdef boxes.
            TextRun run = new TextRun("中文", new FakeTextRunProps());

            run.ShapeAndPlaceCrossPlatform(fontCache);

            Assert.IsTrue(run.FallbackFont, "A font missing glyphs for the run's text should trigger the fallback-font retry");
            Assert.IsNotNull(run.CachedFont.SkiaFont);
            Assert.IsTrue(run.GetWidth(Win32DCSafeHandle.Zero, fontCache) > 0, "The re-shaped run against the fallback font should still produce real advances");
        }

        [TestMethod]
        public void ShapeAndPlaceCrossPlatform_PlainLatinText_DoesNotTriggerFallback()
        {
            FontCache fontCache = new FontCache(96f);
            TextRun run = new TextRun("Hello", new FakeTextRunProps());

            run.ShapeAndPlaceCrossPlatform(fontCache);

            Assert.IsFalse(run.FallbackFont, "Arial covers plain Latin text - no fallback retry should happen");
        }

        [TestMethod]
        public void GetFallbackFontCrossPlatform_MissingCjkGlyph_ResolvesAFontThatCoversIt()
        {
            FontCache fontCache = new FontCache(96f);

            CachedFont fallback = fontCache.GetFallbackFontCrossPlatform(new FakeTextRunProps(), (int)'中');

            Assert.IsNotNull(fallback, "This dev box has CJK system fonts installed (e.g. simsun.ttc/msyh.ttc) - SKFontManager should resolve one");
            Assert.IsNotNull(fallback.SkiaFont);
        }

        [TestMethod]
        public void GetFallbackFontCrossPlatform_ReusesCachedResultForSameCodepoint()
        {
            FontCache fontCache = new FontCache(96f);

            CachedFont first = fontCache.GetFallbackFontCrossPlatform(new FakeTextRunProps(), (int)'中');
            CachedFont second = fontCache.GetFallbackFontCrossPlatform(new FakeTextRunProps(), (int)'中');

            Assert.AreSame(first, second, "Repeated fallback lookups for the same missing codepoint/style should reuse the cached CachedFont, not re-resolve via SKFontManager each time");
        }

        [TestMethod]
        public void ScriptLayoutCrossPlatform_ComputesUnderlineHeightsWithoutAnHdc()
        {
            FontCache fontCache = new FontCache(96f);
            FakeTextRunProps underlinedProps = new FakeTextRunProps { TextDecoration = RPLFormat.TextDecorations.Underline };
            TextRun run = new TextRun("Hello", underlinedProps);
            run.ShapeAndPlaceCrossPlatform(fontCache);

            TextLine line = new TextLine();
            line.LogicalRuns.Add(run);

            line.ScriptLayoutCrossPlatform();
            line.ApplyUnderlineHeights(Win32DCSafeHandle.Zero, fontCache, line.VisualRuns.Count);
            int height = line.GetHeight(Win32DCSafeHandle.Zero, fontCache);

            Assert.IsTrue(height > 0);
            Assert.IsTrue(line.VisualRuns[0].UnderlineHeight > 0, "An underlined run's UnderlineHeight should be computed from real font metrics");
        }
    }
}
