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
	/// simple rules: a break is allowed before any character that follows whitespace, or
	/// follows a hyphen (mid-word hyphenation break, e.g. "well-known" can wrap after
	/// "well-"). It does NOT implement the Unicode Line Breaking Algorithm (UAX #14) -
	/// no East Asian line-breaking classes, no non-breaking-space/glue characters, no
	/// locale-specific rules, no CJK character-by-character wrapping (every CJK character
	/// would need its own break opportunity under real UAX #14 rules; this heuristic
	/// only breaks at whitespace/hyphen boundaries, same limitation as the existing
	/// approximate word-wrap in CrossPlatformTextLayout.cs's SimpleTextWrapper).
	/// </summary>
	internal static class UnicodeLineBreakAnalyzer
	{
		internal static SCRIPT_LOGATTR[] Analyze(string text)
		{
			SCRIPT_LOGATTR[] result = new SCRIPT_LOGATTR[text.Length];
			for (int i = 0; i < text.Length; i++)
			{
				bool isWhiteSpace = char.IsWhiteSpace(text[i]);
				bool isSoftBreak = i == 0 || char.IsWhiteSpace(text[i - 1]) || text[i - 1] == '-';
				result[i] = SCRIPT_LOGATTR.FromFlags(isSoftBreak, isWhiteSpace);
			}
			return result;
		}
	}
}
