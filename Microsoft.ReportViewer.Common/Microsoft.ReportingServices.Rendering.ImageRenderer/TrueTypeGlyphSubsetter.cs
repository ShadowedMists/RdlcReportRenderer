using System;
using System.Collections.Generic;

namespace Microsoft.ReportingServices.Rendering.ImageRenderer
{
	/// <summary>
	/// Reduces an embedded TrueType-outline (glyf/loca) font's file size to only the glyphs
	/// actually used in the rendered document, without renumbering glyph ids - so nothing
	/// else already emitted (glyph-indexed Tj hex strings, CIDToGIDMap /Identity, hmtx-
	/// derived /W widths in the PDF font dictionary) needs to change. Unlike a full font
	/// subsetter (which renumbers glyph ids to a dense, compact range), this only zeroes the
	/// outline data of glyphs never referenced - every other table, and every glyph id
	/// already baked into earlier PDF output, is untouched. This still captures most of a
	/// real subset's size win for a typical report (a handful of glyphs used out of a large
	/// multi-script font's thousands), at a much smaller implementation/risk cost than full
	/// glyph-id renumbering.
	///
	/// Honest scope: only TrueType-outline (glyf/loca) fonts are supported - CFF-flavored
	/// OpenType ('OTTO'/'CFF ' table) and TrueType Collection ('ttcf') fonts are left
	/// whole-file-embedded (<see cref="TrySubset"/> returns false), since neither uses the
	/// glyf/loca format this class rewrites. Any parse surprise falls back the same way -
	/// this never risks shipping a corrupt embedded font over a slightly larger correct one.
	/// </summary>
	internal static class TrueTypeGlyphSubsetter
	{
		private const uint TagGlyf = 0x676C7966u;

		private const uint TagLoca = 0x6C6F6361u;

		private const uint TagHead = 0x68656164u;

		private const uint TagMaxp = 0x6D617870u;

		private const uint TagOtto = 0x4F54544Fu;

		private const uint TagTtcf = 0x74746366u;

		private const int CompositeArgsAreWords = 0x0001;

		private const int CompositeWeHaveAScale = 0x0008;

		private const int CompositeMoreComponents = 0x0020;

		private const int CompositeWeHaveAnXAndYScale = 0x0040;

		private const int CompositeWeHaveATwoByTwo = 0x0080;

		private readonly struct DirectoryEntry
		{
			internal readonly int Offset;

			internal readonly int Length;

			internal DirectoryEntry(int offset, int length)
			{
				Offset = offset;
				Length = length;
			}
		}

		internal static bool TrySubset(byte[] fontData, IEnumerable<ushort> usedGlyphIds, out byte[] subsetted)
		{
			try
			{
				return TrySubsetCore(fontData, usedGlyphIds, out subsetted);
			}
			catch (Exception)
			{
				subsetted = null;
				return false;
			}
		}

		private static bool TrySubsetCore(byte[] fontData, IEnumerable<ushort> usedGlyphIds, out byte[] subsetted)
		{
			subsetted = null;
			if (fontData == null || fontData.Length < 12)
			{
				return false;
			}

			uint version = ReadUInt32BE(fontData, 0);
			if (version == TagOtto || version == TagTtcf)
			{
				return false;
			}

			ushort numTables = ReadUInt16BE(fontData, 4);
			var directory = new Dictionary<uint, DirectoryEntry>();
			for (int i = 0; i < numTables; i++)
			{
				int entryOffset = 12 + i * 16;
				uint tag = ReadUInt32BE(fontData, entryOffset);
				int tableOffset = (int)ReadUInt32BE(fontData, entryOffset + 8);
				int tableLength = (int)ReadUInt32BE(fontData, entryOffset + 12);
				directory[tag] = new DirectoryEntry(tableOffset, tableLength);
			}

			if (!directory.TryGetValue(TagGlyf, out DirectoryEntry glyfEntry) ||
				!directory.TryGetValue(TagLoca, out DirectoryEntry locaEntry) ||
				!directory.TryGetValue(TagHead, out DirectoryEntry headEntry) ||
				!directory.TryGetValue(TagMaxp, out DirectoryEntry maxpEntry))
			{
				return false;
			}

			short indexToLocFormat = (short)ReadUInt16BE(fontData, headEntry.Offset + 50);
			if (indexToLocFormat != 0 && indexToLocFormat != 1)
			{
				return false;
			}
			ushort numGlyphs = ReadUInt16BE(fontData, maxpEntry.Offset + 4);

			int[] loca = ReadLoca(fontData, locaEntry.Offset, numGlyphs, indexToLocFormat);
			if (loca == null)
			{
				return false;
			}

			HashSet<ushort> keep = ResolveKeptGlyphs(fontData, glyfEntry, loca, numGlyphs, usedGlyphIds);

			BuildGlyfAndLoca(fontData, glyfEntry, loca, numGlyphs, keep, out byte[] newGlyfTable, out int[] newLoca);
			byte[] newLocaTable = WriteLoca(newLoca, indexToLocFormat);

			subsetted = RebuildFont(fontData, numTables, new Dictionary<uint, byte[]>
			{
				[TagGlyf] = newGlyfTable,
				[TagLoca] = newLocaTable
			});
			return true;
		}

		private static HashSet<ushort> ResolveKeptGlyphs(byte[] fontData, DirectoryEntry glyfEntry, int[] loca, ushort numGlyphs, IEnumerable<ushort> usedGlyphIds)
		{
			var keep = new HashSet<ushort> { 0 }; // .notdef is always required
			var toVisit = new Queue<ushort>();
			toVisit.Enqueue(0);
			foreach (ushort glyphId in usedGlyphIds)
			{
				if (glyphId < numGlyphs && keep.Add(glyphId))
				{
					toVisit.Enqueue(glyphId);
				}
			}

			while (toVisit.Count > 0)
			{
				ushort glyphId = toVisit.Dequeue();
				int start = loca[glyphId];
				int end = loca[glyphId + 1];
				if (end <= start)
				{
					continue; // empty glyph (e.g. space) - no outline, no component references
				}
				foreach (ushort component in GetCompositeComponents(fontData, glyfEntry.Offset + start, end - start))
				{
					if (component < numGlyphs && keep.Add(component))
					{
						toVisit.Enqueue(component);
					}
				}
			}
			return keep;
		}

		private static void BuildGlyfAndLoca(byte[] fontData, DirectoryEntry glyfEntry, int[] loca, ushort numGlyphs, HashSet<ushort> keep, out byte[] newGlyfTable, out int[] newLoca)
		{
			var newGlyf = new List<byte>(glyfEntry.Length);
			newLoca = new int[numGlyphs + 1];
			for (int glyphId = 0; glyphId < numGlyphs; glyphId++)
			{
				newLoca[glyphId] = newGlyf.Count;
				if (!keep.Contains((ushort)glyphId))
				{
					continue;
				}
				int start = loca[glyphId];
				int end = loca[glyphId + 1];
				for (int b = glyfEntry.Offset + start; b < glyfEntry.Offset + end; b++)
				{
					newGlyf.Add(fontData[b]);
				}
			}
			newLoca[numGlyphs] = newGlyf.Count;
			newGlyfTable = newGlyf.ToArray();
		}

		/// <summary>Simple glyphs (numberOfContours &gt;= 0) reference no other glyph. Composite glyphs (numberOfContours &lt; 0) are a chain of component records, each naming a component glyph id.</summary>
		private static IEnumerable<ushort> GetCompositeComponents(byte[] fontData, int glyphOffset, int glyphLength)
		{
			if (glyphLength < 10)
			{
				yield break;
			}
			short numberOfContours = (short)ReadUInt16BE(fontData, glyphOffset);
			if (numberOfContours >= 0)
			{
				yield break;
			}

			int pos = glyphOffset + 10; // numberOfContours(2) + xMin/yMin/xMax/yMax(2 each)
			int end = glyphOffset + glyphLength;
			while (pos + 4 <= end)
			{
				ushort flags = ReadUInt16BE(fontData, pos);
				ushort glyphIndex = ReadUInt16BE(fontData, pos + 2);
				pos += 4;
				yield return glyphIndex;

				pos += (flags & CompositeArgsAreWords) != 0 ? 4 : 2;
				if ((flags & CompositeWeHaveATwoByTwo) != 0)
				{
					pos += 8;
				}
				else if ((flags & CompositeWeHaveAnXAndYScale) != 0)
				{
					pos += 4;
				}
				else if ((flags & CompositeWeHaveAScale) != 0)
				{
					pos += 2;
				}

				if ((flags & CompositeMoreComponents) == 0)
				{
					break;
				}
			}
		}

		private static int[] ReadLoca(byte[] fontData, int offset, ushort numGlyphs, short format)
		{
			int count = numGlyphs + 1;
			int[] result = new int[count];
			if (format == 0)
			{
				if (offset + count * 2 > fontData.Length)
				{
					return null;
				}
				for (int i = 0; i < count; i++)
				{
					result[i] = ReadUInt16BE(fontData, offset + i * 2) * 2;
				}
			}
			else
			{
				if (offset + count * 4 > fontData.Length)
				{
					return null;
				}
				for (int i = 0; i < count; i++)
				{
					result[i] = (int)ReadUInt32BE(fontData, offset + i * 4);
				}
			}
			return result;
		}

		private static byte[] WriteLoca(int[] loca, short format)
		{
			if (format == 0)
			{
				byte[] buffer = new byte[loca.Length * 2];
				for (int i = 0; i < loca.Length; i++)
				{
					WriteUInt16BE(buffer, i * 2, (ushort)(loca[i] / 2));
				}
				return buffer;
			}
			byte[] buffer2 = new byte[loca.Length * 4];
			for (int i = 0; i < loca.Length; i++)
			{
				WriteUInt32BE(buffer2, i * 4, (uint)loca[i]);
			}
			return buffer2;
		}

		/// <summary>
		/// Reassembles a full sfnt binary from the original file's offset table (unchanged -
		/// table count never changes) plus each table's bytes, in the original directory's
		/// table order, substituting <paramref name="replacements"/> for the tables named in
		/// it and recomputing every table checksum plus head's whole-file checkSumAdjustment
		/// per the OpenType spec.
		/// </summary>
		private static byte[] RebuildFont(byte[] original, ushort numTables, Dictionary<uint, byte[]> replacements)
		{
			var tags = new uint[numTables];
			var tableBytes = new byte[numTables][];
			for (int i = 0; i < numTables; i++)
			{
				int entryOffset = 12 + i * 16;
				uint tag = ReadUInt32BE(original, entryOffset);
				int tableOffset = (int)ReadUInt32BE(original, entryOffset + 8);
				int tableLength = (int)ReadUInt32BE(original, entryOffset + 12);
				tags[i] = tag;
				tableBytes[i] = replacements.TryGetValue(tag, out byte[] replacement) ? replacement : CopyRange(original, tableOffset, tableLength);
			}

			int headIndex = Array.IndexOf(tags, TagHead);
			if (headIndex >= 0)
			{
				byte[] headCopy = (byte[])tableBytes[headIndex].Clone();
				WriteUInt32BE(headCopy, 8, 0u); // zero checkSumAdjustment before computing checksums
				tableBytes[headIndex] = headCopy;
			}

			int directorySize = 12 + numTables * 16;
			int runningOffset = directorySize;
			var tableOffsets = new int[numTables];
			var tableLengths = new int[numTables];
			var tableChecksums = new uint[numTables];
			var paddedTableBytes = new byte[numTables][];
			for (int i = 0; i < numTables; i++)
			{
				byte[] data = tableBytes[i];
				tableOffsets[i] = runningOffset;
				tableLengths[i] = data.Length;
				tableChecksums[i] = CalculateChecksum(data, 0, data.Length);
				int padding = (4 - data.Length % 4) % 4;
				paddedTableBytes[i] = padding == 0 ? data : Pad(data, padding);
				runningOffset += paddedTableBytes[i].Length;
			}

			byte[] output = new byte[runningOffset];
			Array.Copy(original, 0, output, 0, 12); // version/numTables/searchRange/entrySelector/rangeShift unchanged
			for (int i = 0; i < numTables; i++)
			{
				int entryOffset = 12 + i * 16;
				WriteUInt32BE(output, entryOffset, tags[i]);
				WriteUInt32BE(output, entryOffset + 4, tableChecksums[i]);
				WriteUInt32BE(output, entryOffset + 8, (uint)tableOffsets[i]);
				WriteUInt32BE(output, entryOffset + 12, (uint)tableLengths[i]);
				Array.Copy(paddedTableBytes[i], 0, output, tableOffsets[i], paddedTableBytes[i].Length);
			}

			uint fileChecksum = CalculateChecksum(output, 0, output.Length);
			uint checkSumAdjustment = unchecked(0xB1B0AFBAu - fileChecksum);
			if (headIndex >= 0)
			{
				WriteUInt32BE(output, tableOffsets[headIndex] + 8, checkSumAdjustment);
			}

			return output;
		}

		private static uint CalculateChecksum(byte[] data, int offset, int length)
		{
			uint sum = 0;
			int end = offset + length;
			for (int i = offset; i < end; i += 4)
			{
				uint word = 0;
				for (int b = 0; b < 4; b++)
				{
					word <<= 8;
					if (i + b < end)
					{
						word |= data[i + b];
					}
				}
				sum = unchecked(sum + word);
			}
			return sum;
		}

		private static byte[] CopyRange(byte[] source, int offset, int length)
		{
			byte[] result = new byte[length];
			Array.Copy(source, offset, result, 0, length);
			return result;
		}

		private static byte[] Pad(byte[] data, int padding)
		{
			byte[] result = new byte[data.Length + padding];
			Array.Copy(data, result, data.Length);
			return result;
		}

		private static ushort ReadUInt16BE(byte[] buffer, int offset)
		{
			return (ushort)((buffer[offset] << 8) | buffer[offset + 1]);
		}

		private static uint ReadUInt32BE(byte[] buffer, int offset)
		{
			return ((uint)buffer[offset] << 24) | ((uint)buffer[offset + 1] << 16) | ((uint)buffer[offset + 2] << 8) | buffer[offset + 3];
		}

		private static void WriteUInt16BE(byte[] buffer, int offset, ushort value)
		{
			buffer[offset] = (byte)(value >> 8);
			buffer[offset + 1] = (byte)value;
		}

		private static void WriteUInt32BE(byte[] buffer, int offset, uint value)
		{
			buffer[offset] = (byte)(value >> 24);
			buffer[offset + 1] = (byte)(value >> 16);
			buffer[offset + 2] = (byte)(value >> 8);
			buffer[offset + 3] = (byte)value;
		}
	}
}
