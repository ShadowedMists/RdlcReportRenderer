using Microsoft.ReportingServices.Rendering.ImageRenderer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Microsoft.ReportViewer.Chart.Rdl.Tests
{
    /// <summary>
    /// Exercises SfntBinaryUtils.IsTtc/TryExtractTtcFace - the TrueType Collection ('ttcf')
    /// face-extraction step PDFWriter now runs before TrueTypeGlyphSubsetter/CffGlyphSubsetter
    /// (which both assume a single-face sfnt whose table directory starts at offset 0) and
    /// before declaring a FontFile2/FontFile3 stream (which can only hold one font program,
    /// not a multi-face container). Unlike CffGlyphSubsetterTests' hand-built synthetic font
    /// (no real CFF/OTF fixture existed on that dev box), real .ttc files are already
    /// installed on this one (cambria.ttc, simsun.ttc, msyh.ttc, etc. under
    /// C:\Windows\Fonts) - a real fixture is used here instead.
    /// </summary>
    [TestClass]
    public class SfntTtcExtractionTests
    {
        private static byte[] GetRawFontBytes(string family, out int ttcIndex, out SKTypeface typeface)
        {
            typeface = SKTypeface.FromFamilyName(family);
            using SKStreamAsset stream = typeface.OpenStream(out ttcIndex);
            using SKData data = SKData.Create(stream);
            return data.ToArray();
        }

        [TestMethod]
        public void IsTtc_RealTtcFont_ReturnsTrue()
        {
            byte[] cambria = GetRawFontBytes("Cambria", out _, out SKTypeface typeface);
            using (typeface)
            {
                Assert.IsTrue(SfntBinaryUtils.IsTtc(cambria), "cambria.ttc is a real TrueType Collection on this dev box");
            }
        }

        [TestMethod]
        public void IsTtc_PlainTrueTypeFont_ReturnsFalse()
        {
            byte[] arial = GetRawFontBytes("Arial", out _, out SKTypeface typeface);
            using (typeface)
            {
                Assert.IsFalse(SfntBinaryUtils.IsTtc(arial), "Arial is a plain single-face .ttf, not a collection");
            }
        }

        [TestMethod]
        public void TryExtractTtcFace_MatchingFaceIndex_ProducesValidSingleFaceSfnt()
        {
            byte[] cambria = GetRawFontBytes("Cambria", out int ttcIndex, out SKTypeface typeface);
            using (typeface)
            {
                bool result = SfntBinaryUtils.TryExtractTtcFace(cambria, ttcIndex, out byte[] extracted);

                Assert.IsTrue(result, "Extracting the face SkiaSharp actually resolved should succeed");
                Assert.IsFalse(SfntBinaryUtils.IsTtc(extracted), "The extracted face is a standalone sfnt, not itself a collection");
                Assert.AreEqual(SfntOutlineFormat.TrueType, SfntBinaryUtils.DetectOutlineFormat(extracted));

                using var ms = new System.IO.MemoryStream(extracted);
                using SKTypeface reloaded = SKTypeface.FromStream(ms);
                Assert.IsNotNull(reloaded, "The extracted face's bytes should reload as a valid standalone typeface");
                Assert.AreEqual("Cambria", reloaded.FamilyName);
            }
        }

        [TestMethod]
        public void TryExtractTtcFace_DifferentFaceIndicesOfSameContainer_ProduceDistinctFaces()
        {
            // cambria.ttc bundles two faces at known indices: 0 = Cambria (the text face),
            // 1 = Cambria Math (the math/symbol face) - extracting each by its own resolved
            // ttcIndex must produce genuinely different fonts, not the same face twice.
            byte[] cambria = GetRawFontBytes("Cambria", out int cambriaIndex, out SKTypeface cambriaTypeface);
            byte[] cambriaMath = GetRawFontBytes("Cambria Math", out int cambriaMathIndex, out SKTypeface cambriaMathTypeface);
            using (cambriaTypeface)
            using (cambriaMathTypeface)
            {
                Assert.AreNotEqual(cambriaIndex, cambriaMathIndex, "The two families must resolve to different faces within the same .ttc");

                Assert.IsTrue(SfntBinaryUtils.TryExtractTtcFace(cambria, cambriaIndex, out byte[] extractedCambria));
                Assert.IsTrue(SfntBinaryUtils.TryExtractTtcFace(cambriaMath, cambriaMathIndex, out byte[] extractedCambriaMath));

                using var msText = new System.IO.MemoryStream(extractedCambria);
                using var msMath = new System.IO.MemoryStream(extractedCambriaMath);
                using SKTypeface reloadedText = SKTypeface.FromStream(msText);
                using SKTypeface reloadedMath = SKTypeface.FromStream(msMath);

                Assert.AreEqual("Cambria", reloadedText.FamilyName);
                Assert.AreEqual("Cambria Math", reloadedMath.FamilyName);
                Assert.AreNotEqual(extractedCambria.Length, extractedCambriaMath.Length, "The two faces' own private tables (e.g. glyf) differ in size");
            }
        }

        [TestMethod]
        public void TryExtractTtcFace_Result_IsSubsettableByTrueTypeGlyphSubsetter()
        {
            // Proves the two increments compose: a face pulled out of a TTC container is a
            // normal enough standalone sfnt that the existing (offset-0-assuming) subsetter
            // works on it unchanged.
            byte[] cambria = GetRawFontBytes("Cambria", out int ttcIndex, out SKTypeface typeface);
            using (typeface)
            {
                Assert.IsTrue(SfntBinaryUtils.TryExtractTtcFace(cambria, ttcIndex, out byte[] extracted));

                using var font = new SKFont(typeface, 16f);
                ushort[] usedGlyphs = font.GetGlyphs("Hello");

                bool subsetResult = TrueTypeGlyphSubsetter.TrySubset(extracted, usedGlyphs, out byte[] subsetted);

                Assert.IsTrue(subsetResult, "An extracted TTC face should subset the same as any other TrueType-outline sfnt");
                Assert.IsTrue(subsetted.Length < extracted.Length, "Keeping only a handful of glyphs out of a large multi-script face should shrink the file");
            }
        }

        [TestMethod]
        public void TryExtractTtcFace_FaceIndexOutOfRange_ReturnsFalse()
        {
            byte[] cambria = GetRawFontBytes("Cambria", out _, out SKTypeface typeface);
            using (typeface)
            {
                bool result = SfntBinaryUtils.TryExtractTtcFace(cambria, faceIndex: 99, out byte[] extracted);

                Assert.IsFalse(result);
                Assert.IsNull(extracted);
            }
        }

        [TestMethod]
        public void TryExtractTtcFace_NegativeFaceIndex_ReturnsFalse()
        {
            byte[] cambria = GetRawFontBytes("Cambria", out _, out SKTypeface typeface);
            using (typeface)
            {
                bool result = SfntBinaryUtils.TryExtractTtcFace(cambria, faceIndex: -1, out byte[] extracted);

                Assert.IsFalse(result);
                Assert.IsNull(extracted);
            }
        }

        [TestMethod]
        public void TryExtractTtcFace_NonTtcData_ReturnsFalse()
        {
            byte[] arial = GetRawFontBytes("Arial", out _, out SKTypeface typeface);
            using (typeface)
            {
                bool result = SfntBinaryUtils.TryExtractTtcFace(arial, faceIndex: 0, out byte[] extracted);

                Assert.IsFalse(result);
                Assert.IsNull(extracted);
            }
        }

        [TestMethod]
        public void TryExtractTtcFace_TooShortData_ReturnsFalseWithoutThrowing()
        {
            byte[] fakeTtc = { (byte)'t', (byte)'t', (byte)'c', (byte)'f' };

            bool result = SfntBinaryUtils.TryExtractTtcFace(fakeTtc, faceIndex: 0, out byte[] extracted);

            Assert.IsFalse(result);
            Assert.IsNull(extracted);
        }

        [TestMethod]
        public void TryExtractTtcFace_TruncatedOffsetTable_ReturnsFalseWithoutThrowing()
        {
            // Claims 5 fonts but the file is nowhere near long enough to hold that many
            // offset-table entries, let alone 5 real table directories.
            byte[] truncated = { (byte)'t', (byte)'t', (byte)'c', (byte)'f', 0, 1, 0, 0, 0, 0, 0, 5 };

            bool result = SfntBinaryUtils.TryExtractTtcFace(truncated, faceIndex: 0, out byte[] extracted);

            Assert.IsFalse(result);
            Assert.IsNull(extracted);
        }
    }
}
