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

        /// <summary>
        /// Exercises the Matrix/Matrix3x2 rotation call sites in ChartGraphics (C2,
        /// tasks/chart-gdi-type-abstraction.md) — rotated axis labels and a rotated title both
        /// go through ChartGraphics.DrawStringAbs/DrawLabelBackground's RotateAt path, which the
        /// other two sample charts (angle == 0 everywhere) never reach.
        /// </summary>
        internal static byte[] RenderRotatedLabelsChart()
        {
            using var chart = new Chart();
            chart.Width = 400;
            chart.Height = 300;

            var title = chart.Titles.Add("Rotated Title");
            title.TextOrientation = TextOrientation.Rotated90;

            chart.ChartAreas.Add("Default");
            chart.ChartAreas[0].AxisX.LabelStyle.FontAngle = 45;

            var series = chart.Series.Add("Sales");
            series.ChartType = SeriesChartType.Bar;
            series.Points.AddXY("January", 12);
            series.Points.AddXY("February", 18);
            series.Points.AddXY("March", 9);
            series.Points.AddXY("April", 24);

            using var stream = new MemoryStream();
            chart.Save(stream, ChartImageFormat.Png);
            return stream.ToArray();
        }

        /// <summary>
        /// Exercises PieChart's 3D rendering path (Draw3DPie/DrawPieCurves/DrawDoughnutCurves/
        /// FillPieSlice/FillPieSides in ChartGraphics3D.cs) — the interface-typed conversion of
        /// that cluster (tasks/chart-gdi-type-abstraction.md, Milestone B2) isn't reached by any
        /// of the other sample charts, which are all 2D.
        /// </summary>
        internal static byte[] RenderPie3DChart()
        {
            using var chart = new Chart();
            chart.Width = 400;
            chart.Height = 300;

            chart.ChartAreas.Add("Default");
            chart.ChartAreas[0].Area3DStyle.Enable3D = true;

            var series = chart.Series.Add("Sales");
            series.ChartType = SeriesChartType.Pie;
            series.Points.AddXY("Q1", 12);
            series.Points.AddXY("Q2", 18);
            series.Points.AddXY("Q3", 9);
            series.Points.AddXY("Q4", 24);

            using var stream = new MemoryStream();
            chart.Save(stream, ChartImageFormat.Png);
            return stream.ToArray();
        }

        /// <summary>Same as <see cref="RenderPie3DChart"/> but doughnut-style, to exercise DrawDoughnutCurves/FillDoughnutSlice specifically.</summary>
        internal static byte[] RenderDoughnut3DChart()
        {
            using var chart = new Chart();
            chart.Width = 400;
            chart.Height = 300;

            chart.ChartAreas.Add("Default");
            chart.ChartAreas[0].Area3DStyle.Enable3D = true;

            var series = chart.Series.Add("Sales");
            series.ChartType = SeriesChartType.Doughnut;
            series.Points.AddXY("Q1", 12);
            series.Points.AddXY("Q2", 18);
            series.Points.AddXY("Q3", 9);
            series.Points.AddXY("Q4", 24);

            using var stream = new MemoryStream();
            chart.Save(stream, ChartImageFormat.Png);
            return stream.ToArray();
        }
    }
}
