using System.Collections.Generic;

namespace Microsoft.ReportingServices.Rendering.RichText
{
	/// <summary>
	/// Coarse Unicode-range script classification used only by <see cref="UnicodeTextItemizer"/>
	/// to decide item boundaries - not a replacement for Uniscribe's full script table
	/// (<see cref="ScriptProperties"/>), which this prototype does not attempt to model.
	/// </summary>
	internal enum UnicodeScriptKind
	{
		Common,
		Latin,
		Cyrillic,
		Greek,
		Hebrew,
		Arabic,
		Han,
		Other
	}

	/// <summary>One itemized run: a contiguous character range sharing one script/direction, with a constructed SCRIPT_ANALYSIS matching what Win32.ScriptItemize would have produced for it.</summary>
	internal readonly struct TextItem
	{
		internal readonly int CharPos;
		internal readonly int Length;
		internal readonly UnicodeScriptKind Script;
		internal readonly SCRIPT_ANALYSIS Analysis;

		internal TextItem(int charPos, int length, UnicodeScriptKind script, SCRIPT_ANALYSIS analysis)
		{
			CharPos = charPos;
			Length = length;
			Script = script;
			Analysis = analysis;
		}
	}

	/// <summary>
	/// Prototype "itemization" step for tasks/pdf-text-shaping-abstraction.md's P4 step 3
	/// (following HarfBuzzTextShaper's shaping-translation prototype): splits a string
	/// into script/direction runs the way <see cref="Paragraph.ScriptItemize"/> does via
	/// Win32's ScriptItemize, but using coarse Unicode code-point range checks instead of
	/// Uniscribe's script table. Produces a constructed <see cref="SCRIPT_ANALYSIS"/> per
	/// item via the existing <see cref="ScriptAnalysis"/>/<see cref="ScriptState"/>
	/// managed wrappers (no P/Invoke, no reflection - those wrappers already expose an
	/// internal encode path from plain fields back to the packed struct).
	///
	/// Honest scope: this is deliberately not a script-table port. It only distinguishes
	/// enough script buckets to (a) get direction (LTR/RTL) right for the common business-
	/// report case and (b) demonstrate that item boundaries can be produced without
	/// Uniscribe at all. <see cref="UnicodeScriptKind.Common"/> characters (whitespace,
	/// digits, ASCII punctuation) merge into whichever neighboring script run they're
	/// adjacent to (mirroring Uniscribe's own "common" script merge behavior) rather than
	/// forcing a new item per punctuation mark. <see cref="UnicodeScriptKind.Other"/> is
	/// everything not explicitly bucketed (Thai/Devanagari/Hangul/etc.) - it gets its own
	/// item, treated as LTR, which is wrong for scripts with real shaping/reordering rules;
	/// this is the same "RTL/complex-script is a known, explicit gap" boundary the rest of
	/// this doc already draws.
	/// </summary>
	internal static class UnicodeTextItemizer
	{
		internal static List<TextItem> Itemize(string text)
		{
			var items = new List<TextItem>();
			if (string.IsNullOrEmpty(text))
			{
				return items;
			}

			int runStart = 0;
			UnicodeScriptKind runScript = ClassifyChar(text[0]);
			UnicodeScriptKind lastNonCommonScript = (runScript == UnicodeScriptKind.Common) ? UnicodeScriptKind.Latin : runScript;

			for (int i = 1; i < text.Length; i++)
			{
				UnicodeScriptKind charScript = ClassifyChar(text[i]);
				if (charScript == UnicodeScriptKind.Common)
				{
					// Common characters (spaces, digits, punctuation) never force a new
					// item - they merge into the run currently in progress, same as
					// Uniscribe treating "Common" script as compatible with any
					// neighboring script.
					continue;
				}
				if (charScript == lastNonCommonScript)
				{
					continue;
				}

				items.Add(BuildItem(runStart, i - runStart, lastNonCommonScript));
				runStart = i;
				lastNonCommonScript = charScript;
			}

			items.Add(BuildItem(runStart, text.Length - runStart, lastNonCommonScript));
			return items;
		}

		private static TextItem BuildItem(int charPos, int length, UnicodeScriptKind script)
		{
			bool isRtl = IsRtlScript(script);
			var analysis = new ScriptAnalysis(0)
			{
				fRTL = isRtl ? 1 : 0,
				fLayoutRTL = isRtl ? 1 : 0,
				s = new ScriptState()
			};
			return new TextItem(charPos, length, script, analysis.GetAs_SCRIPT_ANALYSIS());
		}

		private static bool IsRtlScript(UnicodeScriptKind script)
		{
			return script == UnicodeScriptKind.Hebrew || script == UnicodeScriptKind.Arabic;
		}

		private static UnicodeScriptKind ClassifyChar(char c)
		{
			if (char.IsWhiteSpace(c) || char.IsDigit(c) || char.IsPunctuation(c) || char.IsSymbol(c))
			{
				return UnicodeScriptKind.Common;
			}
			if (c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= 'À' and <= 'ɏ'))
			{
				return UnicodeScriptKind.Latin;
			}
			if (c is >= 'Ͱ' and <= 'Ͽ')
			{
				return UnicodeScriptKind.Greek;
			}
			if (c is >= 'Ѐ' and <= 'ӿ')
			{
				return UnicodeScriptKind.Cyrillic;
			}
			if (c is >= '֐' and <= '׿')
			{
				return UnicodeScriptKind.Hebrew;
			}
			if (c is >= '؀' and <= 'ۿ')
			{
				return UnicodeScriptKind.Arabic;
			}
			if (c is (>= '一' and <= '鿿') or (>= '぀' and <= 'ヿ'))
			{
				return UnicodeScriptKind.Han;
			}
			return UnicodeScriptKind.Other;
		}
	}
}
