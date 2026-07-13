using System.IO;

namespace Microsoft.ReportingServices.Common.Renderers
{
    public interface IPdfRenderer
    {
        /// <summary>
        /// Renders the provided document model to a PDF written to the output stream.
        /// Implementations must be cross-platform and headless.
        /// </summary>
        /// <param name="document">Document model to render. Implementations may cast to expected types.</param>
        /// <param name="output">Stream to write the generated PDF to.</param>
        void RenderToPdf(object document, Stream output);
    }
}
