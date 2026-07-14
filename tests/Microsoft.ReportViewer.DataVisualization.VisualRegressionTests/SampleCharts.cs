using System.IO;
using Microsoft.Reporting.Chart.WebForms;

namespace Microsoft.ReportViewer.DataVisualization.VisualRegressionTests
{
    /// <summary>
    /// Builds small, self-contained <see cref="Chart"/> instances directly against the internal
    /// chart engine API (no .rdlc report or host required) and rasterizes them to PNG, so a
    /// refactor of the GDI+ call sites underneath can be checked against a pixel baseline.
    /// </summary>
    internal static class SampleCharts
    {
        internal static byte[] RenderSimpleBarChart()
        {
            using var chart = new Chart();
            chart.Width = 400;
            chart.Height = 300;

            chart.ChartAreas.Add("Default");

            var series = chart.Series.Add("Sales");
            series.ChartType = SeriesChartType.Bar;
            series.Points.AddXY("Q1", 12);
            series.Points.AddXY("Q2", 18);
            series.Points.AddXY("Q3", 9);
            series.Points.AddXY("Q4", 24);

            using var stream = new MemoryStream();
            chart.Save(stream, ChartImageFormat.Png);
            return stream.ToArray();
        }

        internal static byte[] RenderSimpleLineChart()
        {
            using var chart = new Chart();
            chart.Width = 400;
            chart.Height = 300;

            chart.ChartAreas.Add("Default");

            var series = chart.Series.Add("Temperature");
            series.ChartType = SeriesChartType.Line;
            series.Points.AddXY(1, 10);
            series.Points.AddXY(2, 14);
            series.Points.AddXY(3, 11);
            series.Points.AddXY(4, 19);
            series.Points.AddXY(5, 15);

            using var stream = new MemoryStream();
            chart.Save(stream, ChartImageFormat.Png);
            return stream.ToArray();
        }
    }
}
