using Microsoft.ReportingServices.Rendering.RichText;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ReportViewer.Chart.Rdl.Tests
{
    /// <summary>
    /// Exercises UnicodeLineBreakAnalyzer - the prototype P4 line-break-attribute
    /// producer (tasks/pdf-text-shaping-abstraction.md) that fills in the same
    /// SCRIPT_LOGATTR shape Paragraph.AnalyzeForBreakPositions gets from
    /// Win32.ScriptBreak, using a hand-rolled heuristic instead of Uniscribe. Not wired
    /// into production LineBreaker - see that class's doc comment for scope and gaps
    /// (still not full UAX #14 compliance - no break-class tailoring, no locale rules -
    /// but CJK/Hangul per-character breaks and non-breaking-space handling are covered).
    /// </summary>
    [TestClass]
    public class UnicodeLineBreakAnalyzerTests
    {
        [TestMethod]
        public void WhitespaceCharacters_AreFlaggedAsWhiteSpace()
        {
            const string text = "one two";
            SCRIPT_LOGATTR[] attrs = UnicodeLineBreakAnalyzer.Analyze(text);

            Assert.AreEqual(text.Length, attrs.Length);
            Assert.IsTrue(attrs[3].IsWhiteSpace, "The space between 'one' and 'two' should be flagged as whitespace");
            Assert.IsFalse(attrs[0].IsWhiteSpace);
            Assert.IsFalse(attrs[4].IsWhiteSpace);
        }

        [TestMethod]
        public void CharacterAfterWhitespace_IsASoftBreakOpportunity()
        {
            const string text = "one two";
            SCRIPT_LOGATTR[] attrs = UnicodeLineBreakAnalyzer.Analyze(text);

            Assert.IsTrue(attrs[4].IsSoftBreak, "'t' in 'two', immediately after the space, should be a break opportunity");
        }

        [TestMethod]
        public void CharacterMidWord_IsNotASoftBreakOpportunity()
        {
            const string text = "one two";
            SCRIPT_LOGATTR[] attrs = UnicodeLineBreakAnalyzer.Analyze(text);

            Assert.IsFalse(attrs[1].IsSoftBreak, "'n' in the middle of 'one' should not be a break opportunity");
            Assert.IsFalse(attrs[6].IsSoftBreak, "'o' in the middle of 'two' should not be a break opportunity");
        }

        [TestMethod]
        public void CharacterAfterHyphen_IsASoftBreakOpportunity()
        {
            const string text = "well-known";
            SCRIPT_LOGATTR[] attrs = UnicodeLineBreakAnalyzer.Analyze(text);

            Assert.IsTrue(attrs[5].IsSoftBreak, "'k' immediately after the hyphen in 'well-known' should be a break opportunity");
        }

        [TestMethod]
        public void FirstCharacter_IsAlwaysASoftBreakOpportunity()
        {
            SCRIPT_LOGATTR[] attrs = UnicodeLineBreakAnalyzer.Analyze("text");

            Assert.IsTrue(attrs[0].IsSoftBreak, "The start of the run is always a valid break/start position");
        }

        [TestMethod]
        public void EmptyText_ProducesEmptyArrayWithoutThrowing()
        {
            SCRIPT_LOGATTR[] attrs = UnicodeLineBreakAnalyzer.Analyze(string.Empty);

            Assert.AreEqual(0, attrs.Length);
        }

        [TestMethod]
        public void AdjacentCjkCharacters_AreEachASoftBreakOpportunity()
        {
            // CJK text conventionally has no whitespace between words - real UAX #14
            // permits a break between almost every adjacent ideograph pair, unlike
            // Latin-style scripts which only break at whitespace/hyphen boundaries.
            string text = "中文测试";
            SCRIPT_LOGATTR[] attrs = UnicodeLineBreakAnalyzer.Analyze(text);

            for (int i = 1; i < text.Length; i++)
            {
                Assert.IsTrue(attrs[i].IsSoftBreak, $"Character at index {i} follows another CJK ideograph and should be a break opportunity");
            }
        }

        [TestMethod]
        public void HangulSyllables_AreEachASoftBreakOpportunity()
        {
            string text = "안녕하세요";
            SCRIPT_LOGATTR[] attrs = UnicodeLineBreakAnalyzer.Analyze(text);

            for (int i = 1; i < text.Length; i++)
            {
                Assert.IsTrue(attrs[i].IsSoftBreak, $"Character at index {i} follows another Hangul syllable and should be a break opportunity");
            }
        }

        [TestMethod]
        public void CjkFollowedByLatin_IsNotForcedIntoASoftBreak()
        {
            // Only CJK-following-CJK gets the ideograph break rule - a CJK character
            // followed by a Latin character falls back to the ordinary whitespace/hyphen
            // rule, same as any other non-ideograph boundary.
            string text = "中" + "A";
            SCRIPT_LOGATTR[] attrs = UnicodeLineBreakAnalyzer.Analyze(text);

            Assert.IsFalse(attrs[1].IsSoftBreak, "'A' immediately after a CJK character with no whitespace/hyphen between them should not be a break opportunity");
        }

        [TestMethod]
        public void NonBreakingSpace_IsNotFlaggedAsWhiteSpaceAndDoesNotEnableABreakAfterIt()
        {
            string text = "one" + ' ' + "two";
            SCRIPT_LOGATTR[] attrs = UnicodeLineBreakAnalyzer.Analyze(text);

            Assert.IsFalse(attrs[3].IsWhiteSpace, "A non-breaking space should not be flagged as whitespace - it's a glue character, not a break point");
            Assert.IsFalse(attrs[4].IsSoftBreak, "The character after a non-breaking space should not be a break opportunity");
        }

        [TestMethod]
        public void EnDashAndEmDash_AreSoftBreakOpportunitiesLikeAHyphen()
        {
            string text = "one" + '–' + "two three" + '—' + "four";
            SCRIPT_LOGATTR[] attrs = UnicodeLineBreakAnalyzer.Analyze(text);

            Assert.IsTrue(attrs[4].IsSoftBreak, "The character immediately after an en dash should be a break opportunity");
            Assert.IsTrue(attrs[14].IsSoftBreak, "The character immediately after an em dash should be a break opportunity");
        }

        [TestMethod]
        public void ClosingParenthesis_IsNeverABreakOpportunityEvenAfterASpace()
        {
            // A plain whitespace-based heuristic would allow a break right after the
            // space; UAX #14's LB13 forbids breaking before closing punctuation even
            // then, so the paren must stay glued to whatever comes before the space.
            const string text = "wait )";
            SCRIPT_LOGATTR[] attrs = UnicodeLineBreakAnalyzer.Analyze(text);

            Assert.IsFalse(attrs[5].IsSoftBreak, "')' should never be a break opportunity, even immediately after a space");
        }

        [TestMethod]
        public void ExclamationMark_IsNeverABreakOpportunityEvenAfterASpace()
        {
            const string text = "stop !";
            SCRIPT_LOGATTR[] attrs = UnicodeLineBreakAnalyzer.Analyze(text);

            Assert.IsFalse(attrs[5].IsSoftBreak, "'!' should never be a break opportunity, even immediately after a space");
        }

        [TestMethod]
        public void Comma_IsNeverABreakOpportunityEvenAfterAHyphen()
        {
            // A plain hyphen-based heuristic would allow a break right after the
            // hyphen; the comma that follows should still refuse the break.
            const string text = "end-,more";
            SCRIPT_LOGATTR[] attrs = UnicodeLineBreakAnalyzer.Analyze(text);

            Assert.IsFalse(attrs[4].IsSoftBreak, "',' should never be a break opportunity, even immediately after a hyphen");
        }

        [TestMethod]
        public void OpeningQuote_IsNeverABreakOpportunityEvenAfterASpace()
        {
            string text = "say " + '“' + "hi" + '”';
            SCRIPT_LOGATTR[] attrs = UnicodeLineBreakAnalyzer.Analyze(text);

            Assert.IsFalse(attrs[4].IsSoftBreak, "An opening quote should never be a break opportunity, even immediately after a space");
        }

        [TestMethod]
        public void CarriageReturnLineFeed_StaysTogetherAsOneMandatoryBreakUnit()
        {
            // A plain whitespace-based heuristic would allow (incorrectly) splitting a
            // CRLF pair, since both characters are whitespace on their own; UAX #14's
            // LB5 treats CRLF as a single, indivisible mandatory-break unit.
            string text = "a\r\nb";
            SCRIPT_LOGATTR[] attrs = UnicodeLineBreakAnalyzer.Analyze(text);

            Assert.IsFalse(attrs[2].IsSoftBreak, "The line feed immediately after a carriage return must not be its own break point");
            Assert.IsTrue(attrs[3].IsSoftBreak, "The character after a CRLF mandatory break must be a break opportunity");
        }

        [TestMethod]
        public void QuestionMark_IsNeverABreakOpportunityEvenAfterASpace()
        {
            // '?' was previously not classified at all (fell through to AL/"ordinary
            // alphabetic"), unlike '!' which already got this same EX treatment - an
            // oversight independent of the fullwidth-punctuation gap below.
            const string text = "stop ?";
            SCRIPT_LOGATTR[] attrs = UnicodeLineBreakAnalyzer.Analyze(text);

            Assert.IsFalse(attrs[5].IsSoftBreak, "'?' should never be a break opportunity, even immediately after a space");
        }

        [TestMethod]
        public void FullwidthExclamationAndQuestionMarks_BehaveLikeTheirHalfwidthCounterparts()
        {
            // Fullwidth '！'/'？' (U+FF01/FF1F) are as common in Chinese/Japanese text as
            // their halfwidth ASCII counterparts, but previously fell through to
            // AL/"ordinary alphabetic" instead of EX.
            string text = "stop " + '！';
            SCRIPT_LOGATTR[] attrs = UnicodeLineBreakAnalyzer.Analyze(text);

            Assert.IsFalse(attrs[5].IsSoftBreak, "'！' should never be a break opportunity, even immediately after a space");
        }

        [TestMethod]
        public void FullwidthComma_SticksToWhatPrecedesItEvenAfterAHyphen()
        {
            // Fullwidth '，' (U+FF0C) - previously misclassified as AL, same gap as the
            // fullwidth exclamation/question marks above.
            string text = "end-" + '，' + "more";
            SCRIPT_LOGATTR[] attrs = UnicodeLineBreakAnalyzer.Analyze(text);

            Assert.IsFalse(attrs[4].IsSoftBreak, "'，' should never be a break opportunity, even immediately after a hyphen");
        }

        [TestMethod]
        public void CurrencyPrefixAndPercentSuffix_StickToTheAdjacentNumber()
        {
            const string text = "$100 and 50%";
            SCRIPT_LOGATTR[] attrs = UnicodeLineBreakAnalyzer.Analyze(text);

            Assert.IsFalse(attrs[1].IsSoftBreak, "The digit right after a currency symbol should not be a break opportunity");
            Assert.IsFalse(attrs[11].IsSoftBreak, "'%' right after a digit should not be a break opportunity");
        }
    }
}
