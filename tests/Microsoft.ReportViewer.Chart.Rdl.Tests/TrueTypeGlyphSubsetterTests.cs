using System.IO;
using Microsoft.ReportingServices.Rendering.ImageRenderer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Microsoft.ReportViewer.Chart.Rdl.Tests
{
    /// <summary>
    /// Exercises TrueTypeGlyphSubsetter - the size-reduction pass for the cross-platform
    /// composite font path's embedded font files, which zeroes unused glyf entries (keeping
    /// glyph ids stable) rather than performing a full glyph-renumbering subset.
    /// </summary>
    [TestClass]
    public class TrueTypeGlyphSubsetterTests
    {
        private static byte[] GetRealFontBytes(out SKTypeface typeface)
        {
            typeface = SKTypeface.FromFamilyName("Arial");
            using SKStreamAsset stream = typeface.OpenStream(out _);
            using SKData data = SKData.Create(stream);
            return data.ToArray();
        }

        [TestMethod]
        public void TrySubset_RealFont_KeepingFewGlyphs_ProducesSmallerFile()
        {
            byte[] original = GetRealFontBytes(out SKTypeface typeface);
            using (typeface)
            {
                using var font = new SKFont(typeface, 16f);
                ushort[] usedGlyphs = font.GetGlyphs("Hello");

                bool result = TrueTypeGlyphSubsetter.TrySubset(original, usedGlyphs, out byte[] subsetted);

                Assert.IsTrue(result, "A real TrueType-outline font should be subsettable");
                Assert.IsNotNull(subsetted);
                Assert.IsTrue(subsetted.Length < original.Length, "Keeping a handful of glyphs out of a large font should shrink the file");
            }
        }

        [TestMethod]
        public void TrySubset_Result_ReloadsAsValidTypefaceWithSameGlyphCount()
        {
            byte[] original = GetRealFontBytes(out SKTypeface typeface);
            using (typeface)
            {
                using var font = new SKFont(typeface, 16f);
                ushort[] usedGlyphs = font.GetGlyphs("Hello world");

                Assert.IsTrue(TrueTypeGlyphSubsetter.TrySubset(original, usedGlyphs, out byte[] subsetted));

                using var ms = new MemoryStream(subsetted);
                using SKTypeface reloaded = SKTypeface.FromStream(ms);

                Assert.IsNotNull(reloaded, "The subsetted font bytes should still parse as a valid font");
                Assert.AreEqual(typeface.GlyphCount, reloaded.GlyphCount, "Glyph count/numbering must stay identical - only outline data is stripped, not glyph ids");
            }
        }

        [TestMethod]
        public void TrySubset_Result_PreservesAdvanceWidthsForAllGlyphs()
        {
            byte[] original = GetRealFontBytes(out SKTypeface typeface);
            using (typeface)
            {
                using var originalFont = new SKFont(typeface, 16f);
                ushort[] usedGlyphs = originalFont.GetGlyphs("Hi");

                Assert.IsTrue(TrueTypeGlyphSubsetter.TrySubset(original, usedGlyphs, out byte[] subsetted));

                using var ms = new MemoryStream(subsetted);
                using SKTypeface reloaded = SKTypeface.FromStream(ms);
                using var reloadedFont = new SKFont(reloaded, 16f);

                // hmtx is never touched by the subsetter, so widths for ANY glyph id -
                // used or not - must be unchanged, not just the kept ones.
                ushort[] sampleGlyphIds = { 0, 1, 2, 3, 40, 101 };
                float[] originalWidths = originalFont.GetGlyphWidths(sampleGlyphIds);
                float[] reloadedWidths = reloadedFont.GetGlyphWidths(sampleGlyphIds);

                CollectionAssert.AreEqual(originalWidths, reloadedWidths);
            }
        }

        [TestMethod]
        public void TrySubset_Result_PreservesCompositeGlyphOutline()
        {
            // 'É' is commonly a composite glyph (base 'E' + combining acute accent) in
            // Arial-family fonts - only its own glyph id is passed as "used", so this
            // proves component glyph dependencies are discovered and kept automatically.
            byte[] original = GetRealFontBytes(out SKTypeface typeface);
            using (typeface)
            {
                using var originalFont = new SKFont(typeface, 16f);
                ushort[] usedGlyphs = originalFont.GetGlyphs("É"); // 'É'
                using SKPath originalPath = originalFont.GetGlyphPath(usedGlyphs[0]);

                Assert.IsTrue(TrueTypeGlyphSubsetter.TrySubset(original, usedGlyphs, out byte[] subsetted));

                using var ms = new MemoryStream(subsetted);
                using SKTypeface reloaded = SKTypeface.FromStream(ms);
                using var reloadedFont = new SKFont(reloaded, 16f);
                using SKPath subsettedPath = reloadedFont.GetGlyphPath(usedGlyphs[0]);

                Assert.AreEqual(originalPath.PointCount, subsettedPath.PointCount, "Composite glyph's component outlines must survive subsetting");
                Assert.AreEqual(originalPath.Bounds, subsettedPath.Bounds);
            }
        }

        [TestMethod]
        public void TrySubset_OttoFlavoredFont_ReturnsFalse()
        {
            byte[] fakeCffFont = new byte[16];
            fakeCffFont[0] = (byte)'O';
            fakeCffFont[1] = (byte)'T';
            fakeCffFont[2] = (byte)'T';
            fakeCffFont[3] = (byte)'O';

            bool result = TrueTypeGlyphSubsetter.TrySubset(fakeCffFont, new ushort[] { 1 }, out byte[] subsetted);

            Assert.IsFalse(result);
            Assert.IsNull(subsetted);
        }

        [TestMethod]
        public void TrySubset_TrueTypeCollection_ReturnsFalse()
        {
            byte[] fakeTtc = new byte[16];
            fakeTtc[0] = (byte)'t';
            fakeTtc[1] = (byte)'t';
            fakeTtc[2] = (byte)'c';
            fakeTtc[3] = (byte)'f';

            bool result = TrueTypeGlyphSubsetter.TrySubset(fakeTtc, new ushort[] { 1 }, out byte[] subsetted);

            Assert.IsFalse(result);
            Assert.IsNull(subsetted);
        }

        [TestMethod]
        public void TrySubset_TooShortData_ReturnsFalseWithoutThrowing()
        {
            bool result = TrueTypeGlyphSubsetter.TrySubset(new byte[4], new ushort[] { 0 }, out byte[] subsetted);

            Assert.IsFalse(result);
            Assert.IsNull(subsetted);
        }

        [TestMethod]
        public void TrySubset_MissingRequiredTables_ReturnsFalse()
        {
            // A well-formed offset table claiming TrueType (0x00010000) but zero tables -
            // no glyf/loca/head/maxp to be found, so this must decline rather than throw.
            byte[] data = new byte[12];
            data[0] = 0;
            data[1] = 1;
            data[2] = 0;
            data[3] = 0;
            data[4] = 0;
            data[5] = 0; // numTables = 0

            bool result = TrueTypeGlyphSubsetter.TrySubset(data, new ushort[] { 0 }, out byte[] subsetted);

            Assert.IsFalse(result);
            Assert.IsNull(subsetted);
        }
    }
}
