namespace Microsoft.ReportingServices.Rendering.RichText
{
	/// <summary>
	/// Prototype line-break-attribute producer for tasks/pdf-text-shaping-abstraction.md's
	/// P4 step 3: produces a <see cref="SCRIPT_LOGATTR"/> per character the way
	/// <see cref="Paragraph.AnalyzeForBreakPositions"/> does via Win32.ScriptBreak, but
	/// using a hand-rolled heuristic instead of Uniscribe (this doc's phased plan
	/// explicitly names "ICU4N or a hand-rolled Unicode line-break implementation" as the
	/// two options; this is the latter, chosen to avoid a new external dependency for a
	/// first cut).
	///
	/// Honest scope: this implements the same two flags <see cref="LineBreaker"/> already
	/// reads from SCRIPT_LOGATTR-shaped data - a break opportunity before this character
	/// (fSoftBreak) and whether the character is itself whitespace (fWhiteSpace) - using
	/// rules a step closer to UAX #14 than a plain "break after any whitespace" heuristic:
	/// a break is allowed before any character that follows whitespace (excluding
	/// no-break/glue spaces - <see cref="IsNoBreakSpace"/>), follows a hyphen or dash
	/// (mid-word hyphenation break, e.g. "well-known" can wrap after "well-"), or is
	/// itself part of a run of CJK ideographs/kana/Hangul syllables following another one
	/// (<see cref="IsWrappableIdeograph"/>) - those scripts permit a break between almost
	/// every adjacent character pair under UAX #14's ID/H2/H3 classes, unlike Latin-style
	/// scripts which only break at whitespace/hyphen boundaries. This is still NOT a full
	/// UAX #14 implementation: no full break-class table (AI/CJ/NS/etc. tailoring), no
	/// locale-specific rules, no context-sensitive numeric/quote handling. Good enough to
	/// let CJK text actually wrap without whitespace and to stop breaking after
	/// non-breaking spaces, which the prior heuristic got wrong.
	/// </summary>
	internal static class UnicodeLineBreakAnalyzer
	{
		internal static SCRIPT_LOGATTR[] Analyze(string text)
		{
			SCRIPT_LOGATTR[] result = new SCRIPT_LOGATTR[text.Length];
			for (int i = 0; i < text.Length; i++)
			{
				char c = text[i];
				bool isWhiteSpace = char.IsWhiteSpace(c) && !IsNoBreakSpace(c);
				bool isSoftBreak = i == 0
					|| IsBreakingCharacter(text[i - 1])
					|| (IsWrappableIdeograph(c) && IsWrappableIdeograph(text[i - 1]));
				result[i] = SCRIPT_LOGATTR.FromFlags(isSoftBreak, isWhiteSpace);
			}
			return result;
		}

		private static bool IsBreakingCharacter(char c)
		{
			// U+002D hyphen-minus, U+2010 hyphen, U+2013 en dash, U+2014 em dash.
			return (char.IsWhiteSpace(c) && !IsNoBreakSpace(c)) || c == '-' || c == '‐' || c == '–' || c == '—';
		}

		/// <summary>
		/// No-break/glue spaces (UAX #14's GL class) - Unicode whitespace that must NOT
		/// allow a line break on either side, unlike ordinary spaces: U+00A0 no-break
		/// space, U+202F narrow no-break space, U+2007 figure space.
		/// </summary>
		private static bool IsNoBreakSpace(char c)
		{
			return c == ' ' || c == ' ' || c == ' ';
		}

		/// <summary>
		/// CJK Unified Ideographs (U+4E00-U+9FFF), Hiragana/Katakana (U+3040-U+30FF), and
		/// Hangul syllables (U+AC00-U+D7A3) - scripts whose UAX #14 break classes
		/// (ID/H2/H3) permit a break between almost every adjacent character pair, since
		/// (unlike Latin-style scripts) they aren't conventionally space-delimited.
		/// </summary>
		private static bool IsWrappableIdeograph(char c)
		{
			return (c >= '一' && c <= '鿿') || (c >= '぀' && c <= 'ヿ') || (c >= '가' && c <= '힣');
		}
	}
}
