using System;
using System.Data;
using System.IO;
using ClosedXML.Excel;

namespace Microsoft.ReportingServices.Common.Renderers
{
    public class LinuxExcelRenderer : IExcelRenderer
    {
        public void RenderToExcel(object data, Stream output)
        {
            // Expect a DataSet or DataTable for simple report output
            if (data == null) throw new ArgumentNullException(nameof(data));

            using (var workbook = new XLWorkbook())
            {
                if (data is DataSet ds)
                {
                    foreach (DataTable table in ds.Tables)
                    {
                        var ws = workbook.Worksheets.Add(table.TableName ?? "Sheet1");
                        ws.Cell(1, 1).InsertTable(table);
                    }
                }
                else if (data is DataTable dt)
                {
                    var ws = workbook.Worksheets.Add(dt.TableName ?? "Sheet1");
                    ws.Cell(1, 1).InsertTable(dt);
                }
                else
                {
                    // Fallback: write string representation
                    var ws = workbook.Worksheets.Add("Sheet1");
                    ws.Cell(1, 1).Value = data.ToString();
                }

                workbook.SaveAs(output);
            }
        }
    }
}
