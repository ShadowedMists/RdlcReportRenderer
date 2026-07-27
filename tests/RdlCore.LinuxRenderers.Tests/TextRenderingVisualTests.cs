using NUnit.Framework;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace RdlCore.LinuxRenderers.Tests
{
    /// <summary>
    /// Visual-verification tooling for tasks/pdf-text-shaping-abstraction.md's Phase 1
    /// spike: confirms the HarfBuzzSharp+SkiaSharp shape-then-rasterize pipeline actually
    /// draws every glyph where shaping says it should be, not just that it runs without
    /// throwing. See TextRasterAssertions for why this is a behavioral check rather than a
    /// checked-in golden-image diff.
    /// </summary>
    public class TextRenderingVisualTests
    {
        private static SKTypeface DefaultTypeface => SKTypeface.FromFamilyName(SKTypeface.Default.FamilyName, SKFontStyle.Normal);

        [TestCase("Hello, World!")]
        [TestCase("The quick brown fox jumps over 12 lazy dogs.")]
        [TestCase("A B C")]
        public void PlainLatinText_EveryGlyphRendersInItsShapedColumn(string text)
        {
            using SKTypeface typeface = DefaultTypeface;
            var result = ShapedTextRasterizer.Render(text, typeface, fontSize: 24);
            try
            {
                var ink = TextRasterAssertions.VerifyGlyphInkPresence(text, result);
                Assert.That(ink.Passed, Is.True, ink.Message);

                var monotonic = TextRasterAssertions.VerifyMonotonicLtrAdvance(result);
                Assert.That(monotonic.Passed, Is.True, monotonic.Message);
            }
            finally
            {
                result.Bitmap.Dispose();
            }
        }

        [TestCase("café naïve résumé")]
        [TestCase("Zürich straße")]
        public void AccentedLatinText_EveryGlyphRendersInItsShapedColumn(string text)
        {
            using SKTypeface typeface = DefaultTypeface;
            var result = ShapedTextRasterizer.Render(text, typeface, fontSize: 24);
            try
            {
                var ink = TextRasterAssertions.VerifyGlyphInkPresence(text, result);
                Assert.That(ink.Passed, Is.True, ink.Message);
            }
            finally
            {
                result.Bitmap.Dispose();
            }
        }

        [Test]
        public void EmptyString_ProducesNoGlyphsAndDoesNotThrow()
        {
            using SKTypeface typeface = DefaultTypeface;
            var result = ShapedTextRasterizer.Render(string.Empty, typeface, fontSize: 24);
            try
            {
                Assert.That(result.Shaped.Points, Is.Empty);
            }
            finally
            {
                result.Bitmap.Dispose();
            }
        }

        /// <summary>
        /// Meta-test: proves VerifyGlyphInkPresence has real discriminating power rather
        /// than trivially passing regardless of correctness. Renders "Hello" normally,
        /// then re-checks the same bitmap against a corrupted shaping result where one
        /// glyph's expected column has been shifted well past the rendered text into
        /// blank canvas - this must fail, or the checker isn't actually checking anything.
        /// </summary>
        [Test]
        public void VerifyGlyphInkPresence_FailsWhenAGlyphColumnIsShiftedIntoBlankCanvas()
        {
            const string text = "Hello";
            using SKTypeface typeface = DefaultTypeface;
            var result = ShapedTextRasterizer.Render(text, typeface, fontSize: 24);
            try
            {
                var sane = TextRasterAssertions.VerifyGlyphInkPresence(text, result);
                Assert.That(sane.Passed, Is.True, "Precondition: normal shaping should pass, " + sane.Message);

                SKShaper.Result shaped = result.Shaped;
                SKPoint[] corruptedPoints = (SKPoint[])shaped.Points.Clone();
                corruptedPoints[0] = new SKPoint(shaped.Width + 200, corruptedPoints[0].Y);
                var corruptedShaped = new SKShaper.Result(shaped.Codepoints, shaped.Clusters, corruptedPoints, shaped.Width);
                var corrupted = new TextRasterResult
                {
                    Bitmap = result.Bitmap,
                    Shaped = corruptedShaped,
                    OriginX = result.OriginX,
                    BaselineY = result.BaselineY,
                    Metrics = result.Metrics
                };

                var check = TextRasterAssertions.VerifyGlyphInkPresence(text, corrupted);
                Assert.That(check.Passed, Is.False, "Shifting a glyph's expected column into blank canvas should be detected");
            }
            finally
            {
                result.Bitmap.Dispose();
            }
        }

        /// <summary>
        /// Documents the current gap (tasks/pdf-text-shaping-abstraction.md): this harness
        /// only asserts non-decreasing (LTR) glyph advance. Arabic/Hebrew text shapes and
        /// reorders visually via HarfBuzz's own bidi handling (SKShaper does not reorder
        /// multi-run bidi paragraphs itself), so this is intentionally not asserted here -
        /// it's a placeholder marking where RTL verification would need to be added once
        /// the production shaping layer handles bidi reordering.
        /// </summary>
        [Test]
        public void RtlText_ShapesWithoutThrowing_ButIsNotYetVerifiedForCorrectReordering()
        {
            const string arabicText = "مرحبا";
            using SKTypeface typeface = DefaultTypeface;
            var result = ShapedTextRasterizer.Render(arabicText, typeface, fontSize: 24);
            try
            {
                Assert.That(result.Shaped.Points.Length, Is.GreaterThan(0),
                    "Shaping should produce at least one glyph even without a documented RTL-correctness check yet");
            }
            finally
            {
                result.Bitmap.Dispose();
            }
        }
    }
}
