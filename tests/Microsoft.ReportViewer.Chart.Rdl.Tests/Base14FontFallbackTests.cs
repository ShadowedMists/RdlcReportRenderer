using Microsoft.ReportingServices.Rendering.ImageRenderer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ReportViewer.Chart.Rdl.Tests
{
    /// <summary>
    /// Exercises PDFWriter.ClassifyFallbackBase14Family/BuildBase14Name - the base-14
    /// font-family fallback used by DrawWrappedText/DrawWrappedRichText's
    /// GetOrCreateBase14Font when a requested family isn't one of the three families
    /// already in m_internalFonts (Arial/Times New Roman/Courier New). Previously every
    /// unknown family fell back to Helvetica regardless of whether it was actually a
    /// serif or monospace family; this closes that gap with a name-only heuristic (see
    /// tasks/pdf-text-shaping-abstraction.md).
    /// </summary>
    [TestClass]
    public class Base14FontFallbackTests
    {
        [TestMethod]
        public void SerifFamilyNames_ClassifyAsTimes()
        {
            Assert.AreEqual("Times", PDFWriter.ClassifyFallbackBase14Family("Georgia"));
            Assert.AreEqual("Times", PDFWriter.ClassifyFallbackBase14Family("Cambria"));
            Assert.AreEqual("Times", PDFWriter.ClassifyFallbackBase14Family("Garamond"));
            Assert.AreEqual("Times", PDFWriter.ClassifyFallbackBase14Family("Book Antiqua"));
        }

        [TestMethod]
        public void MonospaceFamilyNames_ClassifyAsCourier()
        {
            Assert.AreEqual("Courier", PDFWriter.ClassifyFallbackBase14Family("Consolas"));
            Assert.AreEqual("Courier", PDFWriter.ClassifyFallbackBase14Family("Menlo"));
            Assert.AreEqual("Courier", PDFWriter.ClassifyFallbackBase14Family("Lucida Console"));
        }

        [TestMethod]
        public void UnrecognizedFamilyNames_ClassifyAsHelvetica()
        {
            Assert.AreEqual("Helvetica", PDFWriter.ClassifyFallbackBase14Family("Calibri"));
            Assert.AreEqual("Helvetica", PDFWriter.ClassifyFallbackBase14Family("Segoe UI"));
            Assert.AreEqual("Helvetica", PDFWriter.ClassifyFallbackBase14Family(null));
        }

        [TestMethod]
        public void ClassificationIsCaseInsensitive()
        {
            Assert.AreEqual("Times", PDFWriter.ClassifyFallbackBase14Family("GEORGIA"));
            Assert.AreEqual("Courier", PDFWriter.ClassifyFallbackBase14Family("consolas"));
        }

        [TestMethod]
        public void BuildBase14Name_TimesUsesItsOwnStyleSuffixSpelling()
        {
            Assert.AreEqual("Times-Roman", PDFWriter.BuildBase14Name("Times", bold: false, italic: false));
            Assert.AreEqual("Times-Bold", PDFWriter.BuildBase14Name("Times", bold: true, italic: false));
            Assert.AreEqual("Times-Italic", PDFWriter.BuildBase14Name("Times", bold: false, italic: true));
            Assert.AreEqual("Times-BoldItalic", PDFWriter.BuildBase14Name("Times", bold: true, italic: true));
        }

        [TestMethod]
        public void BuildBase14Name_HelveticaAndCourierUseObliqueSpelling()
        {
            Assert.AreEqual("Helvetica", PDFWriter.BuildBase14Name("Helvetica", bold: false, italic: false));
            Assert.AreEqual("Helvetica-BoldOblique", PDFWriter.BuildBase14Name("Helvetica", bold: true, italic: true));
            Assert.AreEqual("Courier-Oblique", PDFWriter.BuildBase14Name("Courier", bold: false, italic: true));
        }
    }
}
