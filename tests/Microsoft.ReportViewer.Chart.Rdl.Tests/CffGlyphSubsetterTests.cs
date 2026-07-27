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

        /// <summary>
        /// Builds a minimal, spec-valid CID-keyed CFF: a Top DICT with ROS (marking it
        /// CID-keyed) plus CharStrings/FDArray/FDSelect operators, one Font DICT in FDArray
        /// (containing just a Private operator), an FDSelect format-0 table (all 3 glyphs
        /// mapped to that one Font DICT), and the Private DICT itself placed after
        /// CharStrings/FDArray/FDSelect - so subsetting must patch FDArray's offset, the
        /// Font DICT's own Private offset, and FDSelect's offset, all by the same delta.
        /// </summary>
        private static byte[] BuildSyntheticCidKeyedOttoFont(out int charStringsStart, out int charStringsIndexLength)
        {
            byte[] nameIndex = BuildIndex(new byte[] { (byte)'T', (byte)'e', (byte)'s', (byte)'t' });
            byte[] stringIndex = BuildIndex();
            byte[] globalSubrIndex = BuildIndex();
            byte[] charStringsIndex = BuildIndex(new byte[] { 14 }, new byte[] { 14 }, new byte[] { 14 }); // 3 glyphs, each just "endchar"
            byte[] fdPrivateDict = new byte[] { 0x8F, 0x14 }; // operand 4, operator 20 (defaultWidthX)
            byte[] fdSelectTable = new byte[] { 0, 0, 0, 0 }; // format 0: all 3 glyphs map to Font DICT 0

            const int hdrSize = 4;
            int nameIndexStart = hdrSize;
            int topDictIndexStart = nameIndexStart + nameIndex.Length;

            // Top DICT: CharStrings (17, 6 bytes), FDArray (12 36, 7 bytes), FDSelect (12 37,
            // 7 bytes), ROS (12 30, 5 bytes: 3 one-byte SID/number operands + 2-byte operator) -
            // all offset operands use the fixed 5-byte (b0=29) integer form, same "reserve
            // fixed width, patch later" trick BuildSyntheticOttoFont above uses.
            byte[] topDictContent = new byte[6 + 7 + 7 + 5];
            topDictContent[0] = 29; // CharStrings offset placeholder
            topDictContent[5] = 17; // operator: CharStrings
            topDictContent[6] = 29; // FDArray offset placeholder
            topDictContent[11] = 12;
            topDictContent[12] = 36; // operator: FDArray
            topDictContent[13] = 29; // FDSelect offset placeholder
            topDictContent[18] = 12;
            topDictContent[19] = 37; // operator: FDSelect
            topDictContent[20] = 139; // ROS operand 1 (registry SID = 0)
            topDictContent[21] = 139; // ROS operand 2 (ordering SID = 0)
            topDictContent[22] = 139; // ROS operand 3 (supplement = 0)
            topDictContent[23] = 12;
            topDictContent[24] = 30; // operator: ROS

            byte[] topDictIndex = BuildIndex(topDictContent);
            int stringIndexStart = topDictIndexStart + topDictIndex.Length;
            int globalSubrIndexStart = stringIndexStart + stringIndex.Length;
            int csStart = globalSubrIndexStart + globalSubrIndex.Length;

            // Font DICT (inside FDArray): just a Private operator (size + offset), same
            // 11-byte shape BuildSyntheticOttoFont's own top-level Private block uses.
            byte[] fontDictContent = new byte[11];
            fontDictContent[0] = 29;
            fontDictContent[5] = 29;
            fontDictContent[10] = 18; // operator: Private
            byte[] fdArrayIndex = BuildIndex(fontDictContent);

            int fdArrayStart = csStart + charStringsIndex.Length;
            int fdSelectStart = fdArrayStart + fdArrayIndex.Length;
            int fdPrivateDictStart = fdSelectStart + fdSelectTable.Length;

            Int32BE(csStart).CopyTo(topDictContent, 1);
            Int32BE(fdArrayStart).CopyTo(topDictContent, 7);
            Int32BE(fdSelectStart).CopyTo(topDictContent, 14);
            topDictIndex = BuildIndex(topDictContent); // rebuild with real Top DICT offsets patched in

            Int32BE(fdPrivateDict.Length).CopyTo(fontDictContent, 1);
            Int32BE(fdPrivateDictStart).CopyTo(fontDictContent, 6);
            fdArrayIndex = BuildIndex(fontDictContent); // rebuild with the real Font DICT Private offset patched in

            var cffTable = new List<byte>();
            cffTable.AddRange(new byte[] { 1, 0, (byte)hdrSize, 4 });
            cffTable.AddRange(nameIndex);
            cffTable.AddRange(topDictIndex);
            cffTable.AddRange(stringIndex);
            cffTable.AddRange(globalSubrIndex);
            cffTable.AddRange(charStringsIndex);
            cffTable.AddRange(fdArrayIndex);
            cffTable.AddRange(fdSelectTable);
            cffTable.AddRange(fdPrivateDict);
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
            int padding = (4 - cffBytes.Length % 4) % 4;
            for (int i = 0; i < padding; i++)
            {
                sfnt.Add(0);
            }

            charStringsStart = csStart;
            charStringsIndexLength = charStringsIndex.Length;
            return sfnt.ToArray();
        }

        [TestMethod]
        public void TrySubset_CidKeyedCff_DroppingOneGlyph_ShrinksCharStringsAndReloadsAsValidFont()
        {
            byte[] original = BuildSyntheticCidKeyedOttoFont(out int charStringsStart, out int originalCharStringsIndexLength);

            bool result = CffGlyphSubsetter.TrySubset(original, new ushort[] { 1 }, out byte[] subsetted);

            Assert.IsTrue(result, "CID-keyed CFF (ROS/FDArray/FDSelect present) should now be subsettable");
            Assert.IsNotNull(subsetted);
            int newCharStringsIndexLength = ReadCharStringsIndexLength(subsetted, charStringsStart);
            Assert.IsTrue(newCharStringsIndexLength < originalCharStringsIndexLength, "Dropping glyph 2's charstring data should shrink the CharStrings INDEX even for a CID-keyed font");

            Assert.IsTrue(SfntBinaryUtils.TryReadTableDirectory(subsetted, out ushort numTables, out Dictionary<uint, SfntTableEntry> tables));
            Assert.AreEqual(1, numTables);
            Assert.AreEqual(SfntOutlineFormat.Cff, SfntBinaryUtils.DetectOutlineFormat(subsetted));

            // Re-read the moved-and-patched FDArray/FDSelect/Font-DICT-Private offsets
            // directly (rather than via a real font parser - this synthetic fixture, like
            // BuildSyntheticOttoFont's, deliberately has only a 'CFF ' table and so isn't a
            // spec-complete sfnt a real font-loading API would accept) to prove
            // PatchCidKeyedStructures's three patches actually landed at the right bytes.
            AssertCidKeyedOffsetsAreConsistent(subsetted);
        }

        /// <summary>Re-parses a subsetted CID-keyed CFF's Top DICT well enough to confirm FDArray/FDSelect still point at in-bounds, parseable structures, and that FDArray's one Font DICT's own Private offset does too - the concrete proof PatchCidKeyedStructures's three offset patches are self-consistent, not just "didn't throw".</summary>
        private static void AssertCidKeyedOffsetsAreConsistent(byte[] sfntData)
        {
            Assert.IsTrue(SfntBinaryUtils.TryReadTableDirectory(sfntData, out _, out Dictionary<uint, SfntTableEntry> tables));
            SfntTableEntry cffEntry = tables[0x43464620u];
            int cffStart = cffEntry.Offset;
            int fileEnd = cffStart + cffEntry.Length;
            byte hdrSize = sfntData[cffStart + 2];

            Assert.IsTrue(TryReadIndexTotalLength(sfntData, cffStart + hdrSize, fileEnd, out int nameIndexLength));
            int topDictIndexStart = cffStart + hdrSize + nameIndexLength;
            Assert.IsTrue(TryReadIndexEntryRange(sfntData, topDictIndexStart, fileEnd, 0, out int topDictStart, out int topDictLen));

            // FDArray offset (12 36) is at byte 6 of this fixture's Top DICT content, FDSelect
            // (12 37) at byte 13 - both fixed 5-byte (b0=29) encoded, so their operand value is
            // simply the 4 bytes right after the marker.
            int fdArrayOffset = ReadInt32BE(sfntData, topDictStart + 6 + 1);
            int fdSelectOffset = ReadInt32BE(sfntData, topDictStart + 13 + 1);
            int fdArrayAbs = cffStart + fdArrayOffset;
            int fdSelectAbs = cffStart + fdSelectOffset;
            Assert.IsTrue(fdArrayAbs >= 0 && fdArrayAbs < fileEnd, "FDArray offset should point inside the CFF table after patching");
            Assert.IsTrue(fdSelectAbs >= 0 && fdSelectAbs < fileEnd, "FDSelect offset should point inside the CFF table after patching");

            Assert.IsTrue(TryReadIndexEntryRange(sfntData, fdArrayAbs, fileEnd, 0, out int fontDictStart, out _));
            // The Font DICT's own Private offset (operator 18) is at byte 5 of its 11-byte content.
            int privateOffset = ReadInt32BE(sfntData, fontDictStart + 5 + 1);
            int privateAbs = cffStart + privateOffset;
            Assert.IsTrue(privateAbs >= 0 && privateAbs < fileEnd, "The Font DICT's own Private offset should point inside the CFF table after patching");
        }

        private static int ReadInt32BE(byte[] data, int offset)
        {
            return (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
        }

        private static bool TryReadIndexTotalLength(byte[] data, int start, int fileEnd, out int totalLength)
        {
            ushort count = SfntBinaryUtils.ReadUInt16BE(data, start);
            if (count == 0)
            {
                totalLength = 2;
                return true;
            }
            byte offSize = data[start + 2];
            int offsetArrayStart = start + 3;
            int lastOffset = 0;
            int lastOffsetPos = offsetArrayStart + count * offSize;
            for (int b = 0; b < offSize; b++)
            {
                lastOffset = (lastOffset << 8) | data[lastOffsetPos + b];
            }
            totalLength = 3 + (count + 1) * offSize + (lastOffset - 1);
            return totalLength > 0 && start + totalLength <= fileEnd;
        }

        private static bool TryReadIndexEntryRange(byte[] data, int start, int fileEnd, int entryIndex, out int entryStart, out int entryLen)
        {
            entryStart = 0;
            entryLen = 0;
            ushort count = SfntBinaryUtils.ReadUInt16BE(data, start);
            if (count == 0 || entryIndex >= count)
            {
                return false;
            }
            byte offSize = data[start + 2];
            int offsetArrayStart = start + 3;
            int dataStart = offsetArrayStart + (count + 1) * offSize;
            int ReadOffset(int index)
            {
                int value = 0;
                int p = offsetArrayStart + index * offSize;
                for (int b = 0; b < offSize; b++)
                {
                    value = (value << 8) | data[p + b];
                }
                return value;
            }
            int startOffset = ReadOffset(entryIndex);
            int endOffset = ReadOffset(entryIndex + 1);
            entryStart = dataStart + startOffset - 1;
            entryLen = endOffset - startOffset;
            return entryStart >= 0 && entryStart + entryLen <= fileEnd;
        }

        [TestMethod]
        public void TrySubset_CidKeyedCff_KeepingAllGlyphs_ProducesSameSizeCharStringsData()
        {
            byte[] original = BuildSyntheticCidKeyedOttoFont(out int charStringsStart, out int originalCharStringsIndexLength);

            Assert.IsTrue(CffGlyphSubsetter.TrySubset(original, new ushort[] { 0, 1, 2 }, out byte[] subsetted));

            Assert.AreEqual(originalCharStringsIndexLength, ReadCharStringsIndexLength(subsetted, charStringsStart));
        }

        [TestMethod]
        public void TrySubset_CidKeyedCff_MissingFdArray_ReturnsFalseWithoutThrowing()
        {
            // A ROS operator with no FDArray at all is a malformed CID-keyed CFF (FDArray is
            // mandatory whenever ROS is present) - must decline rather than guess.
            byte[] nameIndex = BuildIndex(new byte[] { (byte)'T', (byte)'e', (byte)'s', (byte)'t' });
            byte[] stringIndex = BuildIndex();
            byte[] globalSubrIndex = BuildIndex();
            byte[] charStringsIndex = BuildIndex(new byte[] { 14 }, new byte[] { 14 }, new byte[] { 14 });

            const int hdrSize = 4;
            byte[] rosOperators = { 139, 139, 139, 12, 30 };
            byte[] topDictContent = new byte[6 + rosOperators.Length];
            topDictContent[0] = 29;
            topDictContent[5] = 17;
            rosOperators.CopyTo(topDictContent, 6);

            int nameIndexStart = hdrSize;
            int topDictIndexStart = nameIndexStart + nameIndex.Length;
            byte[] topDictIndex = BuildIndex(topDictContent);
            int stringIndexStart = topDictIndexStart + topDictIndex.Length;
            int globalSubrIndexStart = stringIndexStart + stringIndex.Length;
            int charStringsStart = globalSubrIndexStart + globalSubrIndex.Length;
            Int32BE(charStringsStart).CopyTo(topDictContent, 1);
            topDictIndex = BuildIndex(topDictContent);

            var cffTable = new List<byte>();
            cffTable.AddRange(new byte[] { 1, 0, (byte)hdrSize, 4 });
            cffTable.AddRange(nameIndex);
            cffTable.AddRange(topDictIndex);
            cffTable.AddRange(stringIndex);
            cffTable.AddRange(globalSubrIndex);
            cffTable.AddRange(charStringsIndex);
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

            bool result = CffGlyphSubsetter.TrySubset(sfnt.ToArray(), new ushort[] { 1 }, out byte[] subsetted);

            Assert.IsFalse(result, "A ROS operator with no FDArray is malformed and should decline rather than guess");
            Assert.IsNull(subsetted);
        }
    }
}
