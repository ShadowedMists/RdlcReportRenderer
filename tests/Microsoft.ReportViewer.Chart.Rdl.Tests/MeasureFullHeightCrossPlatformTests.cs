using System.Collections.Generic;
using System.Drawing;
using Microsoft.ReportingServices.Rendering.RichText;
using Microsoft.ReportingServices.Rendering.RPLProcessing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ReportViewer.Chart.Rdl.Tests
{
    /// <summary>
    /// Exercises TextBox.MeasureFullHeightCrossPlatform/LineBreaker.FlowVerticalCrossPlatform -
    /// the HDC/GDI+-free counterparts to the pagination-phase height measurement
    /// (SPBProcessing/HPBProcessing PageContext.MeasureFullTextBoxHeight) that
    /// tasks/pdf-text-shaping-abstraction.md's "Vertical-text measurement gap" section
    /// traced to needing a real GDI+ Graphics/Bitmap, which cannot be constructed at all
    /// on non-Windows. These tests call the cross-platform methods directly (same
    /// "bypass the OperatingSystem.IsWindows() check" convention as
    /// TextRun.ShapeAndPlaceCrossPlatform/PDFWriter.DrawTextRunCrossPlatform), since on
    /// this Windows dev box/CI runner the real OS check would always resolve to the
    /// Windows/GDI+ branch regardless of how the TextBox was built.
    /// </summary>
    [TestClass]
    public class MeasureFullHeightCrossPlatformTests
    {
        private sealed class TestTextBoxProps : ITextBoxProps
        {
            public RPLFormat.WritingModes WritingMode { get; set; } = RPLFormat.WritingModes.Horizontal;
            public RPLFormat.TextAlignments DefaultAlignment => RPLFormat.TextAlignments.Left;
            public RPLFormat.Directions Direction => RPLFormat.Directions.LTR;
            public Color BackgroundColor => Color.Transparent;
            public bool CanGrow => true;

            public void DrawTextRun(TextRun run, Paragraph paragraph, Win32DCSafeHandle hdc, float dpiX, FontCache fontCache, int x, int y, int baselineY, int lineHeight, Rectangle layoutRectangle)
            {
            }

            public void DrawClippedTextRun(TextRun run, Paragraph paragraph, Win32DCSafeHandle hdc, float dpiX, FontCache fontCache, int x, int y, int baselineY, int lineHeight, Rectangle layoutRectangle, uint fontColorOverride, Rectangle clipRect)
            {
            }

            public void FillHighlightRectangle(FontCache fontCache, Rectangle rect, Color color)
            {
            }
        }

        private sealed class TestParagraphProps : IParagraphProps
        {
            public float SpaceBefore => 0f;
            public float SpaceAfter => 0f;
            public float LeftIndent => 0f;
            public float RightIndent => 0f;
            public float HangingIndent => 0f;
            public int ListLevel => 0;
            public RPLFormat.ListStyles ListStyle => RPLFormat.ListStyles.None;
            public RPLFormat.TextAlignments Alignment => RPLFormat.TextAlignments.Left;
            public int ParagraphNumber { get; set; }
        }

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

        private static TextBox BuildTextBox(string text, RPLFormat.WritingModes writingMode, FontCache fontCache)
        {
            var textBoxProps = new TestTextBoxProps { WritingMode = writingMode };
            var textBox = new TextBox(textBoxProps);
            var paragraph = new Paragraph(new TestParagraphProps(), 1);
            paragraph.Runs.Add(new TextRun(text, new TestTextRunProps()));
            textBox.Paragraphs = new List<Paragraph> { paragraph };

            // ScriptItemize's real (Windows) branch runs unconditionally here too - same as
            // ShapeAndPlace below - and replaces paragraph.Runs with freshly-extracted
            // TextRun sub-runs (Paragraph.ExtractRuns/TextRun.GetSubRun) even for a single
            // script/direction run, so any pre-shaping done before this call would be
            // discarded. Must itemize first, then shape whatever run objects actually
            // survive itemization.
            textBox.ScriptItemize();

            // LineBreaker.GetLine calls TextRun.GetWidth/GetAscent(hdc, fontCache), which
            // only calls the real (Win32-branching) TextRun.ShapeAndPlace when the run
            // hasn't been shaped yet (m_cachedGlyphData == null) - otherwise it takes the
            // safe LoadGlyphData no-op path. Pre-shaping here via ShapeAndPlaceCrossPlatform
            // (same bypass convention as CrossPlatformPdfTextTests) is what lets these tests
            // exercise MeasureFullHeightCrossPlatform/FlowVerticalCrossPlatform with
            // Win32DCSafeHandle.Zero on this Windows dev box: ShapeAndPlace's own dispatch
            // checks the real OperatingSystem.IsWindows(), not whether the hdc is zero, so
            // without this pre-shaping GetWidth/GetAscent would fall into the real Uniscribe
            // branch and crash on the null device context - the same reason
            // tasks/pdf-text-shaping-abstraction.md documents LineBreaker.Flow/
            // TextBox.Render's bracket-skip changes as not independently unit-testable.
            foreach (Paragraph p in textBox.Paragraphs)
            {
                foreach (TextRun run in p.Runs)
                {
                    run.ShapeAndPlaceCrossPlatform(fontCache);
                }
            }
            return textBox;
        }

        // Deliberately short text and a generously wide/tall flowContext in every test
        // below, so the text always fits on a single unwrapped line/run: LineBreaker's
        // line-wrap split path (Paragraph.ExtractRuns/TextRun.GetSubRun/Split) always
        // builds brand-new, unshaped TextRun objects for a split fragment - only a
        // wholly-unsplit run (GetSubRun's "length == m_text.Length" fast path) returns the
        // original, already-shaped instance. Exercising an actual multi-line wrap here
        // would hit those fresh TextRuns' real (Win32-branching) ShapeAndPlace on this
        // Windows dev box regardless of the Win32DCSafeHandle.Zero passed through
        // FlowVerticalCrossPlatform/MeasureFullHeightCrossPlatform, since ShapeAndPlace
        // dispatches on the real OperatingSystem.IsWindows(), not on hdc-zero-ness - the
        // same non-independently-testable limitation tasks/pdf-text-shaping-abstraction.md
        // already documents for LineBreaker.Flow/TextBox.Render's bracket-skip changes.
        [TestMethod]
        public void HorizontalText_MeasuresAPositiveHeight()
        {
            var fontCache = new FontCache(96f);
            TextBox textBox = BuildTextBox("Hello", RPLFormat.WritingModes.Horizontal, fontCache);
            var flowContext = new FlowContext(500f, float.MaxValue, wordTrim: true, lineLimit: false);

            float height = TextBox.MeasureFullHeightCrossPlatform(textBox, 96f, fontCache, flowContext, out float contentHeight);

            Assert.IsTrue(height > 0f, "A single unwrapped line of text should measure a positive height");
            Assert.AreEqual(height, contentHeight);
        }

        [TestMethod]
        public void VerticalText_MeasuresAPositiveHeightAndWidth_WithoutThrowing()
        {
            // Vertical writing mode is exactly the gap this increment closes -
            // TextBox.MeasureFullHeight's VerticalText branch calls LineBreaker.FlowVertical
            // in a convergence loop; the cross-platform counterpart must do the same without
            // ever touching a System.Drawing.Graphics.
            var fontCache = new FontCache(96f);
            TextBox textBox = BuildTextBox("Hi", RPLFormat.WritingModes.Vertical, fontCache);
            var flowContext = new FlowContext(500f, 500f, wordTrim: true, lineLimit: false);

            float height = TextBox.MeasureFullHeightCrossPlatform(textBox, 96f, fontCache, flowContext, out float contentHeight);

            // Unlike the horizontal branch, "height" (the converged column width) and
            // "contentHeight" (the height reported by the last FlowVerticalCrossPlatform
            // call in the convergence loop) are two different quantities here by design -
            // see MeasureFullHeightCrossPlatform's mirrored convergence loop - so only
            // positivity is asserted, not equality.
            Assert.IsTrue(height > 0f, "Vertical text should converge on a positive width (returned as 'height' from MeasureFullHeightCrossPlatform's perspective)");
            Assert.IsTrue(contentHeight > 0f);
        }

        [TestMethod]
        public void EmptyWidth_ReturnsZeroWithoutFlowing()
        {
            var fontCache = new FontCache(96f);
            TextBox textBox = BuildTextBox("Text", RPLFormat.WritingModes.Horizontal, fontCache);
            var flowContext = new FlowContext(0f, float.MaxValue, wordTrim: true, lineLimit: false);

            float height = TextBox.MeasureFullHeightCrossPlatform(textBox, 96f, fontCache, flowContext, out float contentHeight);

            Assert.AreEqual(0f, height);
            Assert.AreEqual(0f, contentHeight);
        }

        [TestMethod]
        public void HorizontalAndVerticalWritingModes_MeasureDifferentDimensionsForTheSameText()
        {
            // Sanity-checks that MeasureFullHeightCrossPlatform's VerticalText branch is
            // actually taken (not silently falling through to the horizontal Flow call) -
            // horizontal "height" for a single short line is one line's height (small);
            // vertical "height" (really the converged column width) for the same text
            // stacked top-to-bottom is a different quantity, driven by glyph heights summed
            // vertically rather than widths summed horizontally, so they should not
            // coincidentally match.
            var fontCache = new FontCache(96f);
            TextBox horizontalTextBox = BuildTextBox("Hi", RPLFormat.WritingModes.Horizontal, fontCache);
            TextBox verticalTextBox = BuildTextBox("Hi", RPLFormat.WritingModes.Vertical, fontCache);

            var horizontalFlowContext = new FlowContext(500f, float.MaxValue, wordTrim: true, lineLimit: false);
            float horizontalHeight = TextBox.MeasureFullHeightCrossPlatform(horizontalTextBox, 96f, fontCache, horizontalFlowContext, out _);

            var verticalFlowContext = new FlowContext(500f, 500f, wordTrim: true, lineLimit: false);
            float verticalHeight = TextBox.MeasureFullHeightCrossPlatform(verticalTextBox, 96f, fontCache, verticalFlowContext, out _);

            Assert.IsTrue(horizontalHeight > 0f && verticalHeight > 0f);
            Assert.AreNotEqual(horizontalHeight, verticalHeight, "Horizontal and vertical writing modes measure different quantities for the same text and should not produce identical results");
        }
    }
}
