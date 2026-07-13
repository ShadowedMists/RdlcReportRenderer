using System;
using System.Data;
using System.IO;
using System.Resources;
using System.Text;
using Microsoft.ReportViewer.Common.Renderers;
using NUnit.Framework;

namespace ReportViewerCore.LinuxRenderers.Tests
{
    public class LinuxRenderersTests
    {
        [Test]
        public void TestExcelGeneration()
        {
            var renderer = new LinuxExcelRenderer();
            var dt = new DataTable("Test");
            dt.Columns.Add("A");
            dt.Columns.Add("B");
            dt.Rows.Add("1", "2");

            using (var ms = new MemoryStream())
            {
                renderer.RenderToExcel(dt, ms);
                Assert.IsTrue(ms.Length > 0, "Excel output should not be empty");
            }
        }

        [Test]
        public void TestPdfGeneration()
        {
            var renderer = new LinuxPdfRenderer();
            string doc = "Hello PDF";

            using (var ms = new MemoryStream())
            {
                renderer.RenderToPdf(doc, ms);
                Assert.IsTrue(ms.Length > 0, "PDF output should not be empty");
            }
        }

        [Test]
        public void TestRendererFactoryUsesLinuxRenderers()
        {
            var excelRenderer = ReportRendererFactory.CreateExcelRenderer(RenderingPlatform.Linux);
            var pdfRenderer = ReportRendererFactory.CreatePdfRenderer(RenderingPlatform.Linux);

            Assert.That(excelRenderer, Is.TypeOf<LinuxExcelRenderer>());
            Assert.That(pdfRenderer, Is.TypeOf<LinuxPdfRenderer>());
        }

        [Test]
        public void TestImageResourceAdapterCanWriteEmbeddedData()
        {
            using var stream = new MemoryStream();
            var adapter = new ImageResourceAdapter();
            var resourceManager = new ResourceManager("ReportViewerCore.LinuxRenderers.Tests.Resources.TestResources", typeof(LinuxRenderersTests).Assembly);

            adapter.WriteEmbeddedImage(resourceManager, "TestResource", stream);

            Assert.That(stream.Length, Is.GreaterThan(0));
            Assert.That(Encoding.UTF8.GetString(stream.ToArray()), Does.Contain("resource"));
        }

        [Test]
        public void TestResourceAdapterCanWriteGenericPayloads()
        {
            using var stream = new MemoryStream();
            IResourceAdapter adapter = new ImageResourceAdapter();

            adapter.WriteResource("cross-platform payload", stream);

            Assert.That(Encoding.UTF8.GetString(stream.ToArray()), Does.Contain("cross-platform payload"));
        }
    }
}
