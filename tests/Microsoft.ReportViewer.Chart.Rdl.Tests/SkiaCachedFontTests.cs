using Microsoft.ReportingServices.Rendering.RichText;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Microsoft.ReportViewer.Chart.Rdl.Tests
{
    /// <summary>
    /// Exercises SkiaCachedFont - the P4 "font layer" port (tasks/pdf-text-shaping-abstraction.md)
    /// that gives CachedFont's GDI+/Win32-backed font-metrics surface (Font/Hfont/
    /// GetHeight/GetAscent/GetDescent/GetLeading) a Skia-backed equivalent that needs no
    /// device context. Not yet wired into FontCache/TextRun/TextBox - see that class's
    /// doc comment for what step 3 (the shaping layer) still needs to do before this is
    /// reachable from production rendering.
    /// </summary>
    [TestClass]
    public class SkiaCachedFontTests
    {
        [TestMethod]
        public void Metrics_AreConsistentAndPositive()
        {
            using var font = new SkiaCachedFont("Arial", 16f, bold: false, italic: false);

            int height = font.GetHeight();
            int ascent = font.GetAscent();
            int descent = font.GetDescent();
            int leading = font.GetLeading();

            Assert.IsTrue(ascent > 0, "Ascent should be a positive magnitude, matching GDI TEXTMETRIC's convention");
            Assert.IsTrue(descent >= 0, "Descent should be a non-negative magnitude");
            Assert.IsTrue(height > 0, "Height should be positive");
            Assert.AreEqual(ascent + descent + leading, height, "Height should equal ascent + descent + leading, mirroring TEXTMETRIC's tmHeight composition");
        }

        [TestMethod]
        public void LargerFontSize_ProducesLargerMetrics()
        {
            using var small = new SkiaCachedFont("Arial", 10f, bold: false, italic: false);
            using var large = new SkiaCachedFont("Arial", 40f, bold: false, italic: false);

            Assert.IsTrue(large.GetHeight() > small.GetHeight());
        }

        [TestMethod]
        public void BoldAndItalic_DoNotThrowAndProduceUsableMetrics()
        {
            using var bold = new SkiaCachedFont("Arial", 16f, bold: true, italic: false);
            using var italic = new SkiaCachedFont("Arial", 16f, bold: false, italic: true);
            using var boldItalic = new SkiaCachedFont("Arial", 16f, bold: true, italic: true);

            Assert.IsTrue(bold.GetHeight() > 0);
            Assert.IsTrue(italic.GetHeight() > 0);
            Assert.IsTrue(boldItalic.GetHeight() > 0);
        }

        [TestMethod]
        public void UnknownFontFamily_FallsBackWithoutThrowing()
        {
            using var font = new SkiaCachedFont("This Font Does Not Exist On Any System", 16f, bold: false, italic: false);

            Assert.IsTrue(font.GetHeight() > 0);
        }

        [TestMethod]
        public void ScaleFactor_ScalesMetricsDown()
        {
            using var font = new SkiaCachedFont("Arial", 16f, bold: false, italic: false);
            int unscaledHeight = font.GetHeight();

            font.ScaleFactor = 2f;
            int scaledHeight = font.GetHeight();

            Assert.IsTrue(scaledHeight < unscaledHeight);
        }

        [TestMethod]
        public void FromResolvedTypeface_ProducesUsableMetrics()
        {
            // FontCache.GetFallbackFontCrossPlatform wraps an already-resolved SKTypeface
            // (from SKFontManager.MatchCharacter) via this constructor, rather than
            // re-resolving one by family name.
            using SKTypeface typeface = SKTypeface.FromFamilyName("Arial");
            using var font = new SkiaCachedFont(typeface, 16f);

            Assert.IsTrue(font.GetHeight() > 0);
            Assert.AreSame(typeface, font.Typeface);
        }

        [TestMethod]
        public void FromResolvedTypeface_NullTypeface_FallsBackWithoutThrowing()
        {
            using var font = new SkiaCachedFont((SKTypeface)null, 16f);

            Assert.IsTrue(font.GetHeight() > 0);
        }
    }
}
