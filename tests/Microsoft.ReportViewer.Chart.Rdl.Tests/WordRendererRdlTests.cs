using System;
using System.IO;
using System.Text;
using Microsoft.Reporting.NETCore;
using Microsoft.ReportingServices.Rendering.WordRenderer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenMcdf;

namespace Microsoft.ReportViewer.Chart.Rdl.Tests
{
    /// <summary>
    /// Exercises the WORD (binary Word 97) and WORDOPENXML renderers through the real
    /// RDL rendering path (LocalReport.Render), including a report with an embedded
    /// picture - the path that used to throw on non-Windows via a bare
    /// System.Drawing.Image.FromStream call in PictureDescriptor.cs/WordOpenXmlWriter.cs
    /// before both were routed through the same IImageProvider abstraction Excel/PDF
    /// already use (see tasks/word-renderer-cross-platform.md).
    /// </summary>
    [TestClass]
    public class WordRendererRdlTests
    {
        private static LocalReport LoadReport(string reportFileName)
        {
            var report = new LocalReport();
            using (var fs = new FileStream(Path.Combine(AppContext.BaseDirectory, "Reports", reportFileName), FileMode.Open))
            {
                report.LoadReportDefinition(fs);
            }
            return report;
        }

        [TestMethod]
        public void SimpleTextbox_RendersToWord()
        {
            var report = LoadReport("SimpleTextboxReport.rdlc");
            byte[] actual = report.Render("WORD", null);
            Assert.IsTrue(actual.Length > 0, "WORD output should not be empty");

            // The portable CFBF writer (StructuredStorage.cs, via OpenMcdf) must still produce a
            // well-formed OLE Compound File - the standard magic-number header every real .doc
            // reader (including Word itself) checks for.
            byte[] cfbfSignature = { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };
            for (int i = 0; i < cfbfSignature.Length; i++)
            {
                Assert.AreEqual(cfbfSignature[i], actual[i], $"CFBF signature byte {i} mismatch");
            }

            using var stream = new MemoryStream(actual);
            using var root = RootStorage.Open(stream, StorageModeFlags.LeaveOpen);
            Assert.IsTrue(root.ContainsEntry("WordDocument"), "WordDocument stream should exist");
            Assert.IsTrue(root.ContainsEntry("1Table"), "1Table stream should exist");
            Assert.IsTrue(root.ContainsEntry("Data"), "Data stream should exist");
            using (CfbStream wordDocument = root.OpenStream("WordDocument"))
            {
                Assert.IsTrue(wordDocument.Length > 0, "WordDocument stream should not be empty");
            }
        }

        [TestMethod]
        public void CreateMultiStreamFile_SummaryInformation_RoundTrips()
        {
            byte[] mainBytes = Encoding.ASCII.GetBytes("main-stream-content");
            byte[] tableBytes = Encoding.ASCII.GetBytes("table-stream-content");
            byte[] dataBytes = Encoding.ASCII.GetBytes("data-stream-content");
            var sources = new Stream[]
            {
                new MemoryStream(mainBytes),
                new MemoryStream(tableBytes),
                new MemoryStream(dataBytes),
            };
            var streamNames = new[] { "WordDocument", "1Table", "Data" };

            using var output = new MemoryStream();
            bool result = StructuredStorage.CreateMultiStreamFile(
                sources, streamNames, "00020906-0000-0000-c000-000000000046",
                "Test Author", "Test Title", "Test Comments", output, forceInMemory: false);

            Assert.IsTrue(result, "CreateMultiStreamFile should report success");

            output.Position = 0;
            using var root = RootStorage.Open(output, StorageModeFlags.LeaveOpen);
            AssertStreamContents(root, "WordDocument", mainBytes);
            AssertStreamContents(root, "1Table", tableBytes);
            AssertStreamContents(root, "Data", dataBytes);

            Assert.IsTrue(root.ContainsEntry("SummaryInformation"), "SummaryInformation property-set stream should exist");
            using CfbStream summaryInfo = root.OpenStream("SummaryInformation");
            byte[] summaryBytes = new byte[summaryInfo.Length];
            summaryInfo.ReadExactly(summaryBytes, 0, summaryBytes.Length);

            var properties = ParseSummaryInformation(summaryBytes);
            Assert.AreEqual("Test Title", properties[2], "Title property should round-trip");
            Assert.AreEqual("Test Author", properties[4], "Author property should round-trip");
            Assert.AreEqual("Test Comments", properties[6], "Comments property should round-trip");
            Assert.AreEqual((short)1200, properties[1] as short?, "Codepage property should be CP_WINUNICODE");
        }

        private static void AssertStreamContents(RootStorage root, string streamName, byte[] expected)
        {
            using CfbStream stream = root.OpenStream(streamName);
            byte[] actual = new byte[stream.Length];
            stream.ReadExactly(actual, 0, actual.Length);
            CollectionAssert.AreEqual(expected, actual, $"{streamName} stream contents should match what was written");
        }

        // Hand-parses the MS-OLEPS PropertySetStream format written by
        // StructuredStorage.WriteSummaryInformation, independent of that method's own code, so a
        // bug in the writer's byte offsets would show up as a real parse/assertion failure here
        // rather than being validated by re-using the same (possibly-wrong) logic.
        private static System.Collections.Generic.Dictionary<int, object> ParseSummaryInformation(byte[] bytes)
        {
            using var reader = new BinaryReader(new MemoryStream(bytes), Encoding.Unicode);
            ushort byteOrder = reader.ReadUInt16();
            Assert.AreEqual((ushort)0xFFFE, byteOrder, "PropertySetStream byte-order mark");
            reader.ReadUInt16(); // version
            reader.ReadInt32(); // OS identifier
            reader.ReadBytes(16); // CLSID
            int numPropertySets = reader.ReadInt32();
            Assert.AreEqual(1, numPropertySets, "Should write exactly one property set");
            Guid fmtid = new Guid(reader.ReadBytes(16));
            Assert.AreEqual(new Guid("F29F85E0-4FF9-1068-AB91-08002B27B3D9"), fmtid, "FMTID should be SummaryInformation");
            int sectionOffset = reader.ReadInt32();

            reader.BaseStream.Position = sectionOffset;
            reader.ReadInt32(); // cbSection
            int numProperties = reader.ReadInt32();
            var idOffsets = new (int id, int offset)[numProperties];
            for (int i = 0; i < numProperties; i++)
            {
                idOffsets[i] = (reader.ReadInt32(), reader.ReadInt32());
            }

            var result = new System.Collections.Generic.Dictionary<int, object>();
            foreach (var (id, offset) in idOffsets)
            {
                reader.BaseStream.Position = sectionOffset + offset;
                int type = reader.ReadInt32();
                if (type == 2) // VT_I2
                {
                    result[id] = reader.ReadInt16();
                }
                else if (type == 31) // VT_LPWSTR
                {
                    int charCount = reader.ReadInt32(); // includes null terminator
                    string value = new string(reader.ReadChars(charCount - 1));
                    result[id] = value;
                }
                else
                {
                    Assert.Fail($"Unexpected property type {type} for property {id}");
                }
            }
            return result;
        }

        [TestMethod]
        public void SimpleTextbox_RendersToWordOpenXml()
        {
            var report = LoadReport("SimpleTextboxReport.rdlc");
            byte[] actual = report.Render("WORDOPENXML", null);
            Assert.IsTrue(actual.Length > 0, "WORDOPENXML output should not be empty");

            // WORDOPENXML (.docx) is a zip package - starts with the standard local-file-header signature.
            string header = Encoding.ASCII.GetString(actual, 0, Math.Min(2, actual.Length));
            Assert.AreEqual("PK", header, "Output should be a well-formed zip/OPC package");
        }

        [TestMethod]
        public void ImageReport_RendersToWord()
        {
            var report = LoadReport("WordImageReport.rdlc");
            byte[] actual = report.Render("WORD", null);
            Assert.IsTrue(actual.Length > 0, "WORD output with an embedded picture should not be empty");
        }

        [TestMethod]
        public void ImageReport_RendersToWordOpenXml()
        {
            var report = LoadReport("WordImageReport.rdlc");
            byte[] actual = report.Render("WORDOPENXML", null);
            Assert.IsTrue(actual.Length > 0, "WORDOPENXML output with an embedded picture should not be empty");

            string header = Encoding.ASCII.GetString(actual, 0, Math.Min(2, actual.Length));
            Assert.AreEqual("PK", header, "Output should be a well-formed zip/OPC package");
        }
    }
}
