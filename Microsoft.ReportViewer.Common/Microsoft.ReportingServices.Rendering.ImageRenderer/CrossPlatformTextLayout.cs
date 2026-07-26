using System;
using System.Collections.Generic;
using Microsoft.ReportingServices.Rendering.RichText;

namespace Microsoft.ReportingServices.Rendering.ImageRenderer
{
	/// <summary>
	/// Approximate, font-metric-free character-width estimation used only to decide
	/// where to word-wrap text on the cross-platform PDF text path (see
	/// tasks/pdf-text-shaping-abstraction.md). These are rough per-character-class
	/// fractions of the em size, not real font metrics - the actual glyph positions
	/// within a wrapped line are laid out by the PDF reader itself using the real
	/// base-14 font metrics (PDFWriter never emits explicit per-glyph positioning for
	/// these fonts), so imprecision here only risks a slightly early/late line wrap,
	/// never incorrect glyph spacing.
	/// </summary>
	internal static class ApproximateTextMetrics
	{
		private const float NarrowEm = 0.28f;
		private const float WideEm = 0.83f;
		private const float DigitEm = 0.56f;
		private const float UpperEm = 0.67f;
		private const float LowerEm = 0.5f;
		private const float DefaultEm = 0.55f;

		internal static float EstimateCharWidthEm(char c)
		{
			switch (c)
			{
				case 'i': case 'l': case 'I': case 'j': case '.': case ',':
				case ';': case ':': case '\'': case '!': case '|': case ' ':
					return NarrowEm;
				case 'm': case 'w': case 'M': case 'W':
					return WideEm;
			}
			if (char.IsDigit(c))
			{
				return DigitEm;
			}
			if (char.IsUpper(c))
			{
				return UpperEm;
			}
			if (char.IsLower(c))
			{
				return LowerEm;
			}
			return DefaultEm;
		}

		internal static float EstimateStringWidthPoints(string text, float fontSizePoints)
		{
			float widthEm = 0f;
			foreach (char c in text)
			{
				widthEm += EstimateCharWidthEm(c);
			}
			return widthEm * fontSizePoints;
		}
	}

	/// <summary>
	/// Greedy word-wrap for the cross-platform PDF text path: splits on explicit line
	/// breaks first, then packs words onto each line up to maxWidthPoints using
	/// ApproximateTextMetrics for wrap decisions. Deliberately does not attempt
	/// hyphenation, justification, bidi reordering, or complex-script line-breaking
	/// rules (SCRIPT_LOGATTR-equivalent) - those are the documented gaps in
	/// tasks/pdf-text-shaping-abstraction.md; this covers plain LTR/Latin word-wrap only.
	/// </summary>
	internal static class SimpleTextWrapper
	{
		internal static List<string> Wrap(string text, float fontSizePoints, float maxWidthPoints)
		{
			var lines = new List<string>();
			if (string.IsNullOrEmpty(text))
			{
				lines.Add(string.Empty);
				return lines;
			}

			foreach (string paragraphLine in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
			{
				WrapParagraphLine(paragraphLine, fontSizePoints, maxWidthPoints, lines);
			}
			return lines;
		}

		private static void WrapParagraphLine(string paragraphLine, float fontSizePoints, float maxWidthPoints, List<string> lines)
		{
			if (maxWidthPoints <= 0f || paragraphLine.Length == 0)
			{
				lines.Add(paragraphLine);
				return;
			}

			string[] words = paragraphLine.Split(' ');
			var currentLine = new System.Text.StringBuilder();
			float currentWidth = 0f;

			foreach (string word in words)
			{
				float wordWidth = ApproximateTextMetrics.EstimateStringWidthPoints(word, fontSizePoints);
				float spaceWidth = currentLine.Length > 0 ? ApproximateTextMetrics.EstimateStringWidthPoints(" ", fontSizePoints) : 0f;

				if (currentLine.Length > 0 && currentWidth + spaceWidth + wordWidth > maxWidthPoints)
				{
					lines.Add(currentLine.ToString());
					currentLine.Clear();
					currentWidth = 0f;
					spaceWidth = 0f;
				}

				if (currentLine.Length > 0)
				{
					currentLine.Append(' ');
					currentWidth += spaceWidth;
				}
				currentLine.Append(word);
				currentWidth += wordWidth;
			}

			lines.Add(currentLine.ToString());
		}
	}

	/// <summary>
	/// One piece of a wrapped line: a run of text drawn with a single style. Adjacent
	/// same-style fragments are merged so PDFWriter emits one Tj per style change, not
	/// per word.
	/// </summary>
	internal readonly struct StyledLineFragment
	{
		internal readonly string Text;
		internal readonly ITextRunProps Style;

		internal StyledLineFragment(string text, ITextRunProps style)
		{
			Text = text;
			Style = style;
		}
	}

	/// <summary>
	/// Word-wrap across multiple styled runs within a paragraph (the cross-platform
	/// counterpart to ProcessRichTextBox - see tasks/pdf-text-shaping-abstraction.md).
	/// Each paragraph is wrapped independently; explicit newlines within a run's own text
	/// force a line break the same way SimpleTextWrapper does. All runs in a paragraph
	/// share one line-height/baseline (the paragraph's largest font size) - mixing wildly
	/// different font sizes within one line is a documented approximation, not exact
	/// per-run baseline alignment.
	/// </summary>
	internal static class StyledTextWrapper
	{
		internal static List<List<StyledLineFragment>> WrapParagraph(IReadOnlyList<(string Text, ITextRunProps Style)> runs, float maxWidthPoints)
		{
			var lines = new List<List<StyledLineFragment>>();
			var currentLine = new List<StyledLineFragment>();
			float currentWidth = 0f;
			bool lineHasContent = false;

			void FlushLine()
			{
				lines.Add(currentLine);
				currentLine = new List<StyledLineFragment>();
				currentWidth = 0f;
				lineHasContent = false;
			}

			void AppendFragment(string text, ITextRunProps style)
			{
				if (currentLine.Count > 0 && ReferenceEquals(currentLine[currentLine.Count - 1].Style, style))
				{
					StyledLineFragment last = currentLine[currentLine.Count - 1];
					currentLine[currentLine.Count - 1] = new StyledLineFragment(last.Text + text, style);
				}
				else
				{
					currentLine.Add(new StyledLineFragment(text, style));
				}
			}

			foreach ((string runText, ITextRunProps style) in runs)
			{
				string[] paragraphPieces = runText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
				for (int pieceIndex = 0; pieceIndex < paragraphPieces.Length; pieceIndex++)
				{
					string[] words = paragraphPieces[pieceIndex].Split(' ');
					for (int w = 0; w < words.Length; w++)
					{
						string word = words[w];
						bool wordHasLeadingSpace = w > 0;
						float spaceWidth = wordHasLeadingSpace ? ApproximateTextMetrics.EstimateStringWidthPoints(" ", style.FontSize) : 0f;
						float wordWidth = ApproximateTextMetrics.EstimateStringWidthPoints(word, style.FontSize);

						if (lineHasContent && currentWidth + spaceWidth + wordWidth > maxWidthPoints)
						{
							FlushLine();
							wordHasLeadingSpace = false;
							spaceWidth = 0f;
						}

						AppendFragment((wordHasLeadingSpace ? " " : "") + word, style);
						currentWidth += spaceWidth + wordWidth;
						lineHasContent = true;
					}

					bool forcedBreakAfterPiece = pieceIndex < paragraphPieces.Length - 1;
					if (forcedBreakAfterPiece)
					{
						FlushLine();
					}
				}
			}

			if (lineHasContent || lines.Count == 0)
			{
				lines.Add(currentLine);
			}
			return lines;
		}
	}
}
