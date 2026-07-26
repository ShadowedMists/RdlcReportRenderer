using System.Collections.Generic;

namespace Microsoft.ReportingServices.Rendering.RichText
{
	/// <summary>
	/// One itemized-and-shaped run within a paragraph: the composed output of
	/// <see cref="UnicodeTextItemizer"/> (item boundaries + script/direction),
	/// <see cref="UnicodeLineBreakAnalyzer"/> (this item's slice of the paragraph's
	/// per-character break attributes), and <see cref="HarfBuzzTextShaper"/> (this
	/// item's shaped glyph data) - the same three ingredients <see cref="Paragraph.ScriptItemize"/>/
	/// <see cref="Paragraph.AnalyzeForBreakPositions"/>/<see cref="TextRun.ShapeAndPlace"/>
	/// assemble today via Uniscribe, for one run.
	/// </summary>
	internal readonly struct ShapedRunItem
	{
		internal readonly int CharPos;
		internal readonly int Length;
		internal readonly UnicodeScriptKind Script;
		internal readonly SCRIPT_ANALYSIS Analysis;
		internal readonly SCRIPT_LOGATTR[] ScriptLogAttr;
		internal readonly GlyphData GlyphData;

		internal ShapedRunItem(int charPos, int length, UnicodeScriptKind script, SCRIPT_ANALYSIS analysis, SCRIPT_LOGATTR[] scriptLogAttr, GlyphData glyphData)
		{
			CharPos = charPos;
			Length = length;
			Script = script;
			Analysis = analysis;
			ScriptLogAttr = scriptLogAttr;
			GlyphData = glyphData;
		}
	}

	/// <summary>
	/// Composes <see cref="UnicodeTextItemizer"/>, <see cref="UnicodeLineBreakAnalyzer"/>,
	/// and <see cref="HarfBuzzTextShaper"/> into a single per-paragraph pipeline - the
	/// integration step beyond each prototype's own isolated tests (tasks/pdf-text-
	/// shaping-abstraction.md, P4 step 3). Confirms the three pieces actually fit
	/// together (item boundaries slice the line-break array and the shaped text
	/// consistently, with no gaps or overlaps) before any of this touches production
	/// code.
	///
	/// Still NOT wired into <see cref="LineBreaker"/>/<see cref="Paragraph"/>/<see cref="TextRun"/>
	/// - this is one level short of that: it proves the *pieces* compose correctly, not
	/// that they're safe to splice into the real, Win32-HDC-threaded call graph those
	/// classes use (61 call sites, see this doc's step-3 scoping note). It also
	/// deliberately stops at producing shaped run data, not at drawing it - connecting
	/// this to PDFWriter would need real font embedding (glyph-indexed Tj/TJ output),
	/// which is separately, explicitly deferred per user direction until PDF rendering
	/// is otherwise end-to-end (see docs/decisions.md).
	/// </summary>
	internal static class UnicodeParagraphShaper
	{
		internal static List<ShapedRunItem> Shape(string text, SkiaCachedFont font)
		{
			var result = new List<ShapedRunItem>();
			if (string.IsNullOrEmpty(text))
			{
				return result;
			}

			SCRIPT_LOGATTR[] lineBreakAttrs = UnicodeLineBreakAnalyzer.Analyze(text);
			List<TextItem> items = UnicodeTextItemizer.Itemize(text);

			foreach (TextItem item in items)
			{
				string itemText = text.Substring(item.CharPos, item.Length);
				SCRIPT_LOGATTR[] itemLogAttr = new SCRIPT_LOGATTR[item.Length];
				System.Array.Copy(lineBreakAttrs, item.CharPos, itemLogAttr, 0, item.Length);

				GlyphData glyphData = HarfBuzzTextShaper.Shape(itemText, font);

				result.Add(new ShapedRunItem(item.CharPos, item.Length, item.Script, item.Analysis, itemLogAttr, glyphData));
			}

			return result;
		}
	}
}
