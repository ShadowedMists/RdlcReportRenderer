using System;
using System.IO;
using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;

namespace Microsoft.ReportingServices.Common.Renderers
{
    public class LinuxPdfRenderer : IPdfRenderer
    {
        public void RenderToPdf(object document, Stream output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));

            // For now, accept a simple string document or DataTable; complex layouts should map to a document model
            using (var pdf = new PdfDocument())
            {
                var page = pdf.AddPage();
                page.Width = XUnit.FromMillimeter(210);
                page.Height = XUnit.FromMillimeter(297);

                using (var gfx = XGraphics.FromPdfPage(page))
                {
                    var font = new XFont("Arial", 12);
                    string text = document?.ToString() ?? string.Empty;
                    gfx.DrawString(text, font, XBrushes.Black, new XRect(40, 40, page.Width - 80, page.Height - 80));
                }

                pdf.Save(output);
            }
        }
    }
}
