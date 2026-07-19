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

        /// <summary>
        /// Exercises the Emboss border skin (Borders3D/EmbossBorder.cs) — its
        /// Region.Complement-based clip-shadow trick (tasks/chart-gdi-type-abstraction.md,
        /// Milestone B2's Clip/Region conversion) isn't reached by any other sample chart.
        /// </summary>
        internal static byte[] RenderEmbossBorderChart()
        {
            using var chart = new Chart();
            chart.Width = 400;
            chart.Height = 300;
            chart.BorderSkin.SkinStyle = BorderSkinStyle.Emboss;

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

        /// <summary>Same as <see cref="RenderEmbossBorderChart"/> but for the Sunken border skin (Borders3D/SunkenBorder.cs), which has a more elaborate Complement/Intersect combination.</summary>
        internal static byte[] RenderSunkenBorderChart()
        {
            using var chart = new Chart();
            chart.Width = 400;
            chart.Height = 300;
            chart.BorderSkin.SkinStyle = BorderSkinStyle.Sunken;

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

        /// <summary>
        /// Exercises AreaChart's shadow-drawing block (Clip save/Translate/restore around a
        /// per-segment shadow fill) — tasks/chart-gdi-type-abstraction.md's Milestone B2 Clip/Region
        /// conversion. No other sample chart uses an Area series with a shadow.
        /// </summary>
        internal static byte[] RenderAreaChartWithShadow()
        {
            using var chart = new Chart();
            chart.Width = 400;
            chart.Height = 300;

            chart.ChartAreas.Add("Default");

            var series = chart.Series.Add("Sales");
            series.ChartType = SeriesChartType.Area;
            series.ShadowOffset = 3;
            series.Points.AddXY("Q1", 12);
            series.Points.AddXY("Q2", 18);
            series.Points.AddXY("Q3", 9);
            series.Points.AddXY("Q4", 24);

            using var stream = new MemoryStream();
            chart.Save(stream, ChartImageFormat.Png);
            return stream.ToArray();
        }

        /// <summary>
        /// Exercises StockChart's Triangle open/close mark style (StockChart.cs), which builds a
        /// GraphicsPath + SolidBrush that no other sample chart reaches (the default Candlestick
        /// style takes a different, already-interface-typed FillRectangleRel path).
        /// </summary>
        internal static byte[] RenderStockChartWithTriangleMarks()
        {
            using var chart = new Chart();
            chart.Width = 400;
            chart.Height = 300;

            chart.ChartAreas.Add("Default");

            var series = chart.Series.Add("Prices");
            series.ChartType = SeriesChartType.Stock;
            series["OpenCloseStyle"] = "Triangle";
            series.Points.AddY(12, 8, 9, 11);
            series.Points.AddY(15, 10, 11, 13);
            series.Points.AddY(14, 9, 13, 10);
            series.Points.AddY(18, 12, 10, 16);

            using var stream = new MemoryStream();
            chart.Save(stream, ChartImageFormat.Png);
            return stream.ToArray();
        }

        /// <summary>
        /// Exercises FastPointChart.DrawMarker (FastPointChart.cs), which builds its brush/pen
        /// locally per marker (bordered Diamond/Cross styles) rather than through any of
        /// ChartGraphics's shared brush-getter helpers.
        /// </summary>
        internal static byte[] RenderFastPointChartWithMarkers()
        {
            using var chart = new Chart();
            chart.Width = 400;
            chart.Height = 300;

            chart.ChartAreas.Add("Default");

            var series = chart.Series.Add("Points");
            series.ChartType = SeriesChartType.FastPoint;
            series.MarkerStyle = MarkerStyle.Diamond;
            series.MarkerSize = 10;
            series.MarkerBorderColor = System.Drawing.Color.Black;
            series.MarkerBorderWidth = 2;
            series.Points.AddXY(1, 10);
            series.Points.AddXY(2, 14);
            series.Points.AddXY(3, 11);

            var series2 = chart.Series.Add("Points2");
            series2.ChartType = SeriesChartType.FastPoint;
            series2.MarkerStyle = MarkerStyle.Cross;
            series2.MarkerSize = 10;
            series2.MarkerColor = System.Drawing.Color.Red;
            series2.MarkerBorderColor = System.Drawing.Color.DarkRed;
            series2.MarkerBorderWidth = 1;
            series2.Points.AddXY(1, 16);
            series2.Points.AddXY(2, 18);
            series2.Points.AddXY(3, 15);

            using var stream = new MemoryStream();
            chart.Save(stream, ChartImageFormat.Png);
            return stream.ToArray();
        }

        /// <summary>
        /// Exercises FastLineChart.DrawLine (FastLineChart.cs), which builds its own Pen locally
        /// (not through any of ChartGraphics's shared brush/pen-getter helpers) and also builds a
        /// hit-region GraphicsPath in the same method.
        /// </summary>
        internal static byte[] RenderFastLineChart()
        {
            using var chart = new Chart();
            chart.Width = 400;
            chart.Height = 300;

            chart.ChartAreas.Add("Default");

            var series = chart.Series.Add("Trend");
            series.ChartType = SeriesChartType.FastLine;
            series.BorderWidth = 3;
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
        /// Exercises LineChart.DrawLine's shadow-line block (LineChart.cs), which builds its own
        /// local Pen (independent of the shared linePen field) only when ShadowOffset is set —
        /// not reached by RenderSimpleLineChart, which has no shadow.
        /// </summary>
        internal static byte[] RenderLineChartWithShadow()
        {
            using var chart = new Chart();
            chart.Width = 400;
            chart.Height = 300;

            chart.ChartAreas.Add("Default");

            var series = chart.Series.Add("Temperature");
            series.ChartType = SeriesChartType.Line;
            series.ShadowOffset = 3;
            series.BorderWidth = 2;
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
