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
    /// (no UAX #14 compliance, no CJK per-character breaks).
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
    }
}
