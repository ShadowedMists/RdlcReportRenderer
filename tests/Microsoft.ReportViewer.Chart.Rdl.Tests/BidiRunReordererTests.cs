using System.Collections.Generic;
using Microsoft.ReportingServices.Rendering.RichText;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ReportViewer.Chart.Rdl.Tests
{
    /// <summary>
    /// Exercises BidiRunReorderer's run-level visual reordering directly, independent of
    /// shaping/itemization, using plain (label, isRtl) tuples so each scenario's expected
    /// visual order is easy to reason about.
    /// </summary>
    [TestClass]
    public class BidiRunReordererTests
    {
        private static List<(string Label, bool IsRtl)> Reorder(params (string Label, bool IsRtl)[] items)
        {
            var list = new List<(string Label, bool IsRtl)>(items);
            BidiRunReorderer.ReorderToVisualOrder(list, item => item.IsRtl);
            return list;
        }

        private static void AssertOrder(List<(string Label, bool IsRtl)> actual, params string[] expectedLabels)
        {
            Assert.AreEqual(expectedLabels.Length, actual.Count);
            for (int i = 0; i < expectedLabels.Length; i++)
            {
                Assert.AreEqual(expectedLabels[i], actual[i].Label, $"Mismatch at position {i}");
            }
        }

        [TestMethod]
        public void SingleLtrRun_IsUnchanged()
        {
            List<(string, bool)> result = Reorder(("A", false));
            AssertOrder(result, "A");
        }

        [TestMethod]
        public void SingleRtlRun_IsUnchanged()
        {
            List<(string, bool)> result = Reorder(("A", true));
            AssertOrder(result, "A");
        }

        [TestMethod]
        public void AllLtrRuns_AreUnchanged()
        {
            List<(string, bool)> result = Reorder(("A", false), ("B", false), ("C", false));
            AssertOrder(result, "A", "B", "C");
        }

        [TestMethod]
        public void LtrBase_SingleEmbeddedRtlIsland_PositionUnchanged()
        {
            // "Hello <שלום> world" - a lone RTL word inside an LTR paragraph keeps its slot;
            // only its own glyph order (handled by HarfBuzz, not this class) is affected.
            List<(string, bool)> result = Reorder(("Hello ", false), ("שלום", true), (" world", false));
            AssertOrder(result, "Hello ", "שלום", " world");
        }

        [TestMethod]
        public void LtrBase_TwoAdjacentRtlRuns_AreReversedRelativeToEachOther()
        {
            // Two distinct RTL scripts (e.g. Hebrew then Arabic) embedded back-to-back in an
            // LTR paragraph form one same-level bidi run and get reversed as a group.
            List<(string, bool)> result = Reorder(("Hello ", false), ("Hebrew", true), ("Arabic", true), (" world", false));
            AssertOrder(result, "Hello ", "Arabic", "Hebrew", " world");
        }

        [TestMethod]
        public void RtlBase_EmbeddedLtrWord_WholeSequenceReverses()
        {
            // A paragraph whose first strong character is RTL (Hebrew) establishes an RTL
            // base direction; the trailing embedded LTR word visually sits to its LEFT.
            List<(string, bool)> result = Reorder(("שלום ", true), ("world", false));
            AssertOrder(result, "world", "שלום ");
        }

        [TestMethod]
        public void EmptyList_DoesNotThrow()
        {
            var list = new List<(string Label, bool IsRtl)>();
            BidiRunReorderer.ReorderToVisualOrder(list, item => item.IsRtl);
            Assert.AreEqual(0, list.Count);
        }
    }
}
