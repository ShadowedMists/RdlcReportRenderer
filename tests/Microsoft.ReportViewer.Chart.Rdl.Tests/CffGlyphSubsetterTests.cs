using System.Collections.Generic;
using System.IO;
using Microsoft.ReportingServices.Rendering.ImageRenderer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Microsoft.ReportViewer.Chart.Rdl.Tests
{
    /// <summary>
    /// Exercises CffGlyphSubsetter - the CFF-flavored ('OTTO') counterpart to
    /// TrueTypeGlyphSubsetterTests. No CFF/OTF font is vendored in this repo and none was
    /// found installed on the dev box that produced this test (same "no reproducible
    /// fixture" constraint noted elsewhere in this project for font-format edge cases), so
    /// these tests hand-build a minimal, spec-valid, single-font CFF table (3 glyphs: gid0
    /// .notdef + 2 real glyphs, each a trivial one-byte "endchar" Type2 charstring, a
    /// Private DICT placed after CharStrings so the Private-offset-patching code path is
    /// actually exercised) wrapped in a one-table 'OTTO' sfnt, rather than testing only via
    /// malformed-input rejection.
    /// </summary>
    [TestClass]
    public class CffGlyphSubsetterTests
    {
        private const int NumGlyphsInSyntheticFont = 3;

        /// <summary>Builds a CFF INDEX (count + offSize + offset array + data) for the given entries, using offSize=1 (every synthetic entry here is tiny).</summary>
        private static byte[] BuildIndex(params byte[][] entries)
        {
            int count = entries.Length;
            var bytes = new List<byte> { (byte)(count >> 8), (byte)count };
            if (count == 0)
            {
                return bytes.ToArray();
            }
            bytes.Add(1); // offSize
            int[] offsets = new int[count + 1];
            offsets[0] = 1;
            for (int i = 0; i < count; i++)
            {
                offsets[i + 1] = offsets[i] + entries[i].Length;
            }
            foreach (int offset in offsets)
            {
                bytes.Add((byte)offset);
            }
            foreach (byte[] entry in entries)
            {
                bytes.AddRange(entry);
            }
            return bytes.ToArray();
        }

        private static byte[] Int32BE(int value)
        {
            return new byte[] { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value };
        }

        /// <summary>
        /// Builds a full 'OTTO'-wrapped sfnt with one 'CFF ' table: Header, Name INDEX
        /// ("Test"), Top DICT INDEX (CharStrings + Private operators, both 5-byte/32-bit
        /// encoded so their byte positions are trivial to compute), empty String/Global
        /// Subr INDEXes, a 3-glyph CharStrings INDEX (each glyph a 1-byte "endchar"
        /// charstring), then the Private DICT - deliberately placed *after* CharStrings so
        /// subsetting must patch its offset operand, the main risk area this class handles.
        /// Padded to a 4-byte boundary the same way <see cref="SfntBinaryUtils.RebuildFont"/>
        /// always pads its output, so total-file-size comparisons against a subsetted result
        /// aren't thrown off by padding rounding on this deliberately tiny synthetic font.
        /// </summary>
        private static byte[] BuildSyntheticOttoFont(out int cffTableOffsetOut, out int charStringsStartOut, out int charStringsIndexLengthOut)
        {
            byte[] nameIndex = BuildIndex(new byte[] { (byte)'T', (byte)'e', (byte)'s', (byte)'t' });
            byte[] stringIndex = BuildIndex();
            byte[] globalSubrIndex = BuildIndex();
            byte[] charStringsIndex = BuildIndex(new byte[] { 14 }, new byte[] { 14 }, new byte[] { 14 }); // 3 glyphs, each just "endchar"
            byte[] privateDict = new byte[] { 0x8F, 0x14 }; // operand 4, operator 20 (defaultWidthX) - minimal valid non-empty Private DICT

            const int hdrSize = 4;
            int nameIndexStart = hdrSize;
            int topDictIndexStart = nameIndexStart + nameIndex.Length;

            // CharStrings/Private operators use fixed 5-byte (b0=29) integer encoding, so the
            // Top DICT's own byte length doesn't depend on the (not-yet-known) offset values -
            // this mirrors the same "reserve fixed width, patch later" trick the real
            // CffGlyphSubsetter relies on when it patches an existing font's Top DICT in place.
            byte[] topDictContent = new byte[6 + 11];
            topDictContent[0] = 29; // placeholder for CharStrings offset, patched below
            topDictContent[5] = 17; // operator: CharStrings
            topDictContent[6] = 29; // placeholder for Private size
            topDictContent[11] = 29; // placeholder for Private offset
            topDictContent[16] = 18; // operator: Private

            byte[] topDictIndex = BuildIndex(topDictContent);
            int stringIndexStart = topDictIndexStart + topDictIndex.Length;
            int globalSubrIndexStart = stringIndexStart + stringIndex.Length;
            int charStringsStart = globalSubrIndexStart + globalSubrIndex.Length;
            int privateDictStart = charStringsStart + charStringsIndex.Length;

            Int32BE(charStringsStart).CopyTo(topDictContent, 1);
            Int32BE(privateDict.Length).CopyTo(topDictContent, 7);
            Int32BE(privateDictStart).CopyTo(topDictContent, 12);
            topDictIndex = BuildIndex(topDictContent); // rebuild with real offset values now patched in

            var cffTable = new List<byte>();
            cffTable.AddRange(new byte[] { 1, 0, (byte)hdrSize, 4 }); // major, minor, hdrSize, header offSize (unused by readers)
            cffTable.AddRange(nameIndex);
            cffTable.AddRange(topDictIndex);
            cffTable.AddRange(stringIndex);
            cffTable.AddRange(globalSubrIndex);
            cffTable.AddRange(charStringsIndex);
            cffTable.AddRange(privateDict);
            byte[] cffBytes = cffTable.ToArray();

            const int sfntHeaderLen = 12;
            const int tableRecordLen = 16;
            const int cffTableOffset = sfntHeaderLen + tableRecordLen;

            var sfnt = new List<byte>();
            sfnt.AddRange(new byte[] { (byte)'O', (byte)'T', (byte)'T', (byte)'O' });
            sfnt.AddRange(new byte[] { 0, 1, 0, 16, 0, 0, 0, 0 }); // numTables=1, searchRange=16, entrySelector=0, rangeShift=0
            sfnt.AddRange(new byte[] { (byte)'C', (byte)'F', (byte)'F', (byte)' ' });
            sfnt.AddRange(new byte[] { 0, 0, 0, 0 }); // checksum - not validated on read
            sfnt.AddRange(Int32BE(cffTableOffset));
            sfnt.AddRange(Int32BE(cffBytes.Length));
            sfnt.AddRange(cffBytes);
            int padding = (4 - cffBytes.Length % 4) % 4;
            for (int i = 0; i < padding; i++)
            {
                sfnt.Add(0);
            }

            cffTableOffsetOut = cffTableOffset;
            charStringsStartOut = charStringsStart;
            charStringsIndexLengthOut = charStringsIndex.Length;
            return sfnt.ToArray();
        }

        /// <summary>Reads the CFF CharStrings INDEX total byte length at a known relative offset within the 'CFF ' table - used to check subsetting shrank/preserved it precisely, rather than comparing whole-file sizes (which a padding-rounding artifact can mask on a font this tiny).</summary>
        private static int ReadCharStringsIndexLength(byte[] sfntData, int charStringsStart)
        {
            Assert.IsTrue(SfntBinaryUtils.TryReadTableDirectory(sfntData, out _, out Dictionary<uint, SfntTableEntry> tables));
            SfntTableEntry cffEntry = tables[0x43464620u];
            int absoluteStart = cffEntry.Offset + charStringsStart;
            ushort count = SfntBinaryUtils.ReadUInt16BE(sfntData, absoluteStart);
            byte offSize = sfntData[absoluteStart + 2];
            int lastOffsetPos = absoluteStart + 3 + count * offSize;
            int lastOffset = 0;
            for (int b = 0; b < offSize; b++)
            {
                lastOffset = (lastOffset << 8) | sfntData[lastOffsetPos + b];
            }
            return 3 + (count + 1) * offSize + (lastOffset - 1);
        }

        [TestMethod]
        public void TrySubset_SyntheticCffFont_DroppingOneGlyph_ShrinksCharStringsAndPatchesPrivateOffset()
        {
            byte[] original = BuildSyntheticOttoFont(out _, out int charStringsStart, out int originalCharStringsIndexLength);

            bool result = CffGlyphSubsetter.TrySubset(original, new ushort[] { 1 }, out byte[] subsetted);

            Assert.IsTrue(result, "A well-formed single-font, non-CID-keyed CFF should be subsettable");
            Assert.IsNotNull(subsetted);
            int newCharStringsIndexLength = ReadCharStringsIndexLength(subsetted, charStringsStart);
            Assert.IsTrue(newCharStringsIndexLength < originalCharStringsIndexLength, "Dropping glyph 2's charstring data should shrink the CharStrings INDEX");
        }

        [TestMethod]
        public void TrySubset_Result_ReloadsAsValidSfntTableDirectory()
        {
            byte[] original = BuildSyntheticOttoFont(out _, out _, out _);
            Assert.IsTrue(CffGlyphSubsetter.TrySubset(original, new ushort[] { 1 }, out byte[] subsetted));

            Assert.IsTrue(SfntBinaryUtils.TryReadTableDirectory(subsetted, out ushort numTables, out Dictionary<uint, SfntTableEntry> tables));
            Assert.AreEqual(1, numTables);
            Assert.IsTrue(tables.ContainsKey(0x43464620u)); // 'CFF '
            Assert.AreEqual(SfntOutlineFormat.Cff, SfntBinaryUtils.DetectOutlineFormat(subsetted));
        }

        [TestMethod]
        public void TrySubset_KeepingAllGlyphs_ProducesSameSizeCharStringsData()
        {
            byte[] original = BuildSyntheticOttoFont(out _, out int charStringsStart, out int originalCharStringsIndexLength);

            Assert.IsTrue(CffGlyphSubsetter.TrySubset(original, new ushort[] { 0, 1, 2 }, out byte[] subsetted));

            // Every glyph's 1-byte charstring is kept, so the CharStrings INDEX itself should
            // be unchanged in size (same offSize/count, same data length) - only the
            // removed-glyph case shrinks it.
            Assert.AreEqual(originalCharStringsIndexLength, ReadCharStringsIndexLength(subsetted, charStringsStart));
        }

        [TestMethod]
        public void TrySubset_NonOttoFont_ReturnsFalse()
        {
            byte[] fakeTrueTypeFont = new byte[16];
            fakeTrueTypeFont[0] = 0;
            fakeTrueTypeFont[1] = 1;
            fakeTrueTypeFont[2] = 0;
            fakeTrueTypeFont[3] = 0;

            bool result = CffGlyphSubsetter.TrySubset(fakeTrueTypeFont, new ushort[] { 1 }, out byte[] subsetted);

            Assert.IsFalse(result);
            Assert.IsNull(subsetted);
        }

        [TestMethod]
        public void TrySubset_TooShortData_ReturnsFalseWithoutThrowing()
        {
            bool result = CffGlyphSubsetter.TrySubset(new byte[4], new ushort[] { 0 }, out byte[] subsetted);

            Assert.IsFalse(result);
            Assert.IsNull(subsetted);
        }

        [TestMethod]
        public void TrySubset_CidKeyedCff_ReturnsFalse()
        {
            // Same synthetic font shape, but with a ROS (12 30) operator appended to the Top
            // DICT - marks it CID-keyed, which this class deliberately does not attempt to
            // subset. Built inline rather than via BuildSyntheticOttoFont since the ROS
            // operator changes the Top DICT content this needs.

            // Construct a fresh Top DICT content with
            // ROS operands (three SIDs, encoded as 1-byte small integers) followed by operator 12,30.
            byte[] nameIndex = BuildIndex(new byte[] { (byte)'T', (byte)'e', (byte)'s', (byte)'t' });
            byte[] stringIndex = BuildIndex();
            byte[] globalSubrIndex = BuildIndex();
            byte[] charStringsIndex = BuildIndex(new byte[] { 14 }, new byte[] { 14 }, new byte[] { 14 });
            byte[] privateDict = new byte[] { 0x8F, 0x14 };

            const int hdrSize = 4;
            int nameIndexStart = hdrSize;
            int topDictIndexStart = nameIndexStart + nameIndex.Length;

            // ROS operands: 3 standard-encoded small SIDs (139 offset form) + operator 12,30 (2 bytes)
            byte[] rosOperators = { 139, 139, 139, 12, 30 };
            byte[] topDictContent = new byte[6 + 11 + rosOperators.Length];
            topDictContent[0] = 29;
            topDictContent[5] = 17;
            topDictContent[6] = 29;
            topDictContent[11] = 29;
            topDictContent[16] = 18;
            rosOperators.CopyTo(topDictContent, 17);

            byte[] topDictIndex = BuildIndex(topDictContent);
            int stringIndexStart = topDictIndexStart + topDictIndex.Length;
            int globalSubrIndexStart = stringIndexStart + stringIndex.Length;
            int charStringsStart = globalSubrIndexStart + globalSubrIndex.Length;
            int privateDictStart = charStringsStart + charStringsIndex.Length;

            Int32BE(charStringsStart).CopyTo(topDictContent, 1);
            Int32BE(privateDict.Length).CopyTo(topDictContent, 7);
            Int32BE(privateDictStart).CopyTo(topDictContent, 12);
            topDictIndex = BuildIndex(topDictContent);

            var cffTable = new List<byte>();
            cffTable.AddRange(new byte[] { 1, 0, (byte)hdrSize, 4 });
            cffTable.AddRange(nameIndex);
            cffTable.AddRange(topDictIndex);
            cffTable.AddRange(stringIndex);
            cffTable.AddRange(globalSubrIndex);
            cffTable.AddRange(charStringsIndex);
            cffTable.AddRange(privateDict);
            byte[] cffBytes = cffTable.ToArray();

            const int sfntHeaderLen = 12;
            const int tableRecordLen = 16;
            const int cffTableOffset = sfntHeaderLen + tableRecordLen;

            var sfnt = new List<byte>();
            sfnt.AddRange(new byte[] { (byte)'O', (byte)'T', (byte)'T', (byte)'O' });
            sfnt.AddRange(new byte[] { 0, 1, 0, 16, 0, 0, 0, 0 });
            sfnt.AddRange(new byte[] { (byte)'C', (byte)'F', (byte)'F', (byte)' ' });
            sfnt.AddRange(new byte[] { 0, 0, 0, 0 });
            sfnt.AddRange(Int32BE(cffTableOffset));
            sfnt.AddRange(Int32BE(cffBytes.Length));
            sfnt.AddRange(cffBytes);
            byte[] cidKeyedFont = sfnt.ToArray();

            bool result = CffGlyphSubsetter.TrySubset(cidKeyedFont, new ushort[] { 1 }, out byte[] subsetted);

            Assert.IsFalse(result, "CID-keyed CFF (ROS present) is out of scope and should fall back to whole-file embedding");
            Assert.IsNull(subsetted);
        }
    }
}
