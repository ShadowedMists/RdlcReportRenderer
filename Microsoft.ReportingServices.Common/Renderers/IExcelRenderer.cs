using System.IO;

namespace Microsoft.ReportingServices.Common.Renderers
{
    public interface IExcelRenderer
    {
        /// <summary>
        /// Renders the provided data to an Excel file written to the output stream.
        /// Implementations should be headless and cross-platform.
        /// </summary>
        /// <param name="data">An object representing the report data. Implementations may cast to expected types.</param>
        /// <param name="output">Stream to write the generated .xlsx file to.</param>
        void RenderToExcel(object data, Stream output);
    }
}
