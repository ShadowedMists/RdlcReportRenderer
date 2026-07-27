using Microsoft.ReportingServices.Rendering.ImageRenderer;
using Microsoft.ReportingServices.Rendering.RichText;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Microsoft.ReportViewer.Chart.Rdl.Tests
{
    /// <summary>
    /// Exercises SkiaFontEmbeddingRights - the cross-platform counterpart to
    /// FontPackage.CheckEmbeddingRights, reading OS/2 fsType embedding-permission bits
    /// directly from an SKTypeface instead of via a Win32 HFONT/HDC.
    /// </summary>
    [TestClass]
    public class SkiaFontEmbeddingRightsTests
    {
        [TestMethod]
        public void CanEmbedFsType_Zero_ReturnsTrue()
        {
            // 0x0000 = Installable Embedding: fully allowed, no restriction bits set.
            Assert.IsTrue(SkiaFontEmbeddingRights.CanEmbedFsType(0x0000));
        }

        [TestMethod]
        public void CanEmbedFsType_RestrictedLicenseBit_ReturnsFalse()
        {
            // 0x0002 = Restricted License embedding: the only fsType bit that forbids
            // embedding outright.
            Assert.IsFalse(SkiaFontEmbeddingRights.CanEmbedFsType(0x0002));
        }

        [TestMethod]
        public void CanEmbedFsType_PreviewAndPrintBit_ReturnsTrue()
        {
            // 0x0004 = Preview & Print embedding: a usage restriction for the consuming
            // viewer/print client, not a prohibition on embedding the font at all.
            Assert.IsTrue(SkiaFontEmbeddingRights.CanEmbedFsType(0x0004));
        }

        [TestMethod]
        public void CanEmbedFsType_EditableBit_ReturnsTrue()
        {
            Assert.IsTrue(SkiaFontEmbeddingRights.CanEmbedFsType(0x0008));
        }

        [TestMethod]
        public void CanEmbedFsType_RestrictedCombinedWithOtherBits_ReturnsFalse()
        {
            Assert.IsFalse(SkiaFontEmbeddingRights.CanEmbedFsType(0x0002 | 0x0100));
        }

        [TestMethod]
        public void CanEmbed_NullTypeface_ReturnsFalse()
        {
            Assert.IsFalse(SkiaFontEmbeddingRights.CanEmbed(null));
        }

        [TestMethod]
        public void CanEmbed_RealTypeface_MatchesItsOwnOS2FsType()
        {
            // Whatever font SKTypeface.FromFamilyName("Arial") actually resolves to on this
            // platform (a real Arial, or a fallback default) should agree with the
            // fsType-derived expectation computed the same way CanEmbed itself reads it -
            // no hardcoded expected value, so this holds regardless of which font resolves.
            using var font = new SkiaCachedFont("Arial", 16f, bold: false, italic: false);
            SKTypeface typeface = font.Typeface;

            byte[] os2Table = typeface.GetTableData(0x4F532F32u);
            bool expected = true;
            if (os2Table != null && os2Table.Length >= 10)
            {
                ushort fsType = (ushort)((os2Table[8] << 8) | os2Table[9]);
                expected = SkiaFontEmbeddingRights.CanEmbedFsType(fsType);
            }

            Assert.AreEqual(expected, SkiaFontEmbeddingRights.CanEmbed(typeface));
        }
    }
}
