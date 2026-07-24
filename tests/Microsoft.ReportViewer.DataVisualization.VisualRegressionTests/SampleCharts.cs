using System.Drawing;
using System.IO;
using Microsoft.Reporting.Chart.WebForms;

namespace Microsoft.ReportViewer.DataVisualization.VisualRegressionTests
{
    /// <summary>
    /// Builds small, self-contained <see cref="Chart"/> instances directly against the internal
    /// chart engine API (no .rdlc report or host required) and rasterizes them to PNG, so a
    /// refactor of the GDI+ call sites underneath can be checked against a pixel baseline.
    /// Each scene is exposed as a <c>Build*</c> method (returns the configured <see cref="Chart"/>,
    /// no rendering) plus a <c>Render*</c> wrapper (Build + <see cref="Chart.Save"/> to PNG via the
    /// GDI+ path) — Skia tests (Milestone E2) reuse the <c>Build*</c> methods directly instead of
    /// duplicating scene construction.
    /// </summary>
    internal static class SampleCharts
    {
        internal static Chart BuildSimpleBarChart()
        {
            var chart = new Chart();
            chart.Width = 400;
            chart.Height = 300;

            chart.ChartAreas.Add("Default");

            var series = chart.Series.Add("Sales");
            series.ChartType = SeriesChartType.Bar;
            series.Points.AddXY("Q1", 12);
            series.Points.AddXY("Q2", 18);
            series.Points.AddXY("Q3", 9);
            series.Points.AddXY("Q4", 24);

            return chart;
        }

        internal static byte[] RenderSimpleBarChart()
        {
            using var chart = BuildSimpleBarChart();
            using var stream = new MemoryStream();
            chart.Save(stream, ChartImageFormat.Png);
            return stream.ToArray();
        }

        internal static Chart BuildSimpleLineChart()
        {
            var chart = new Chart();
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

            return chart;
        }

        internal static byte[] RenderSimpleLineChart()
        {
            using var chart = BuildSimpleLineChart();
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
        internal static Chart BuildRotatedLabelsChart()
        {
            var chart = new Chart();
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

            return chart;
        }

        internal static byte[] RenderRotatedLabelsChart()
        {
            using var chart = BuildRotatedLabelsChart();
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
        internal static Chart BuildPie3DChart()
        {
            var chart = new Chart();
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

            return chart;
        }

        internal static byte[] RenderPie3DChart()
        {
            using var chart = BuildPie3DChart();
            using var stream = new MemoryStream();
            chart.Save(stream, ChartImageFormat.Png);
            return stream.ToArray();
        }

        /// <summary>Same as <see cref="RenderPie3DChart"/> but doughnut-style, to exercise DrawDoughnutCurves/FillDoughnutSlice specifically.</summary>
        internal static Chart BuildDoughnut3DChart()
        {
            var chart = new Chart();
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

            return chart;
        }

        internal static byte[] RenderDoughnut3DChart()
        {
            using var chart = BuildDoughnut3DChart();
            using var stream = new MemoryStream();
            chart.Save(stream, ChartImageFormat.Png);
            return stream.ToArray();
        }

        /// <summary>
        /// Exercises the Emboss border skin (Borders3D/EmbossBorder.cs) — its
        /// Region.Complement-based clip-shadow trick (tasks/chart-gdi-type-abstraction.md,
        /// Milestone B2's Clip/Region conversion) isn't reached by any other sample chart.
        /// </summary>
        internal static Chart BuildEmbossBorderChart()
        {
            var chart = new Chart();
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

            return chart;
        }

        internal static byte[] RenderEmbossBorderChart()
        {
            using var chart = BuildEmbossBorderChart();
            using var stream = new MemoryStream();
            chart.Save(stream, ChartImageFormat.Png);
            return stream.ToArray();
        }

        /// <summary>Same as <see cref="RenderEmbossBorderChart"/> but for the Sunken border skin (Borders3D/SunkenBorder.cs), which has a more elaborate Complement/Intersect combination.</summary>
        internal static Chart BuildSunkenBorderChart()
        {
            var chart = new Chart();
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

            return chart;
        }

        internal static byte[] RenderSunkenBorderChart()
        {
            using var chart = BuildSunkenBorderChart();
            using var stream = new MemoryStream();
            chart.Save(stream, ChartImageFormat.Png);
            return stream.ToArray();
        }

        /// <summary>
        /// Exercises AreaChart's shadow-drawing block (Clip save/Translate/restore around a
        /// per-segment shadow fill) — tasks/chart-gdi-type-abstraction.md's Milestone B2 Clip/Region
        /// conversion. No other sample chart uses an Area series with a shadow.
        /// </summary>
        internal static Chart BuildAreaChartWithShadow()
        {
            var chart = new Chart();
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

            return chart;
        }

        internal static byte[] RenderAreaChartWithShadow()
        {
            using var chart = BuildAreaChartWithShadow();
            using var stream = new MemoryStream();
            chart.Save(stream, ChartImageFormat.Png);
            return stream.ToArray();
        }

        /// <summary>
        /// Exercises StockChart's Triangle open/close mark style (StockChart.cs), which builds a
        /// GraphicsPath + SolidBrush that no other sample chart reaches (the default Candlestick
        /// style takes a different, already-interface-typed FillRectangleRel path).
        /// </summary>
        internal static Chart BuildStockChartWithTriangleMarks()
        {
            var chart = new Chart();
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

            return chart;
        }

        internal static byte[] RenderStockChartWithTriangleMarks()
        {
            using var chart = BuildStockChartWithTriangleMarks();
            using var stream = new MemoryStream();
            chart.Save(stream, ChartImageFormat.Png);
            return stream.ToArray();
        }

        /// <summary>
        /// Exercises FastPointChart.DrawMarker (FastPointChart.cs), which builds its brush/pen
        /// locally per marker (bordered Diamond/Cross styles) rather than through any of
        /// ChartGraphics's shared brush-getter helpers.
        /// </summary>
        internal static Chart BuildFastPointChartWithMarkers()
        {
            var chart = new Chart();
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

            return chart;
        }

        internal static byte[] RenderFastPointChartWithMarkers()
        {
            using var chart = BuildFastPointChartWithMarkers();
            using var stream = new MemoryStream();
            chart.Save(stream, ChartImageFormat.Png);
            return stream.ToArray();
        }

        /// <summary>
        /// Exercises FastLineChart.DrawLine (FastLineChart.cs), which builds its own Pen locally
        /// (not through any of ChartGraphics's shared brush/pen-getter helpers) and also builds a
        /// hit-region GraphicsPath in the same method.
        /// </summary>
        internal static Chart BuildFastLineChart()
        {
            var chart = new Chart();
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

            return chart;
        }

        internal static byte[] RenderFastLineChart()
        {
            using var chart = BuildFastLineChart();
            using var stream = new MemoryStream();
            chart.Save(stream, ChartImageFormat.Png);
            return stream.ToArray();
        }

        /// <summary>
        /// Exercises LineChart.DrawLine's shadow-line block (LineChart.cs), which builds its own
        /// local Pen (independent of the shared linePen field) only when ShadowOffset is set —
        /// not reached by RenderSimpleLineChart, which has no shadow.
        /// </summary>
        internal static Chart BuildLineChartWithShadow()
        {
            var chart = new Chart();
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

            return chart;
        }

        internal static byte[] RenderLineChartWithShadow()
        {
            using var chart = BuildLineChartWithShadow();
            using var stream = new MemoryStream();
            chart.Save(stream, ChartImageFormat.Png);
            return stream.ToArray();
        }

        /// <summary>
        /// Exercises PointChart.DrawLabels (PointChart.cs), which now bridges the concrete
        /// point.Font/StringFormat into IChartFont/ITextFormat right at the
        /// DrawPointLabelStringRel call site (C5/C6 real-caller migration) — not reached by any
        /// other sample chart, none of which enable point labels.
        /// </summary>
        internal static Chart BuildPointChartWithLabels()
        {
            var chart = new Chart();
            chart.Width = 400;
            chart.Height = 300;

            chart.ChartAreas.Add("Default");

            var series = chart.Series.Add("Readings");
            series.ChartType = SeriesChartType.Point;
            series.ShowLabelAsValue = true;
            series.MarkerStyle = MarkerStyle.Circle;
            series.MarkerSize = 8;
            series.Points.AddXY(1, 10);
            series.Points.AddXY(2, 14);
            series.Points.AddXY(3, 11);

            return chart;
        }

        internal static byte[] RenderPointChartWithLabels()
        {
            using var chart = BuildPointChartWithLabels();
            using var stream = new MemoryStream();
            chart.Save(stream, ChartImageFormat.Png);
            return stream.ToArray();
        }

        /// <summary>
        /// Exercises BarChart.DrawLabelsAndMarkers (BarChart.cs) with the same
        /// Font/StringFormat -> IChartFont/ITextFormat bridge at its DrawPointLabelStringRel call.
        /// </summary>
        internal static Chart BuildBarChartWithLabels()
        {
            var chart = new Chart();
            chart.Width = 400;
            chart.Height = 300;

            chart.ChartAreas.Add("Default");

            var series = chart.Series.Add("Sales");
            series.ChartType = SeriesChartType.Bar;
            series.ShowLabelAsValue = true;
            series.Points.AddXY("Q1", 12);
            series.Points.AddXY("Q2", 18);
            series.Points.AddXY("Q3", 9);

            return chart;
        }

        internal static byte[] RenderBarChartWithLabels()
        {
            using var chart = BuildBarChartWithLabels();
            using var stream = new MemoryStream();
            chart.Save(stream, ChartImageFormat.Png);
            return stream.ToArray();
        }

        /// <summary>
        /// Exercises BarChart.DrawLabels3D (BarChart.cs), the 3D counterpart of the label call
        /// converted above, using a PointF (not RectangleF) DrawPointLabelStringRel overload.
        /// </summary>
        internal static Chart BuildBarChart3DWithLabels()
        {
            var chart = new Chart();
            chart.Width = 400;
            chart.Height = 300;

            chart.ChartAreas.Add("Default");
            chart.ChartAreas[0].Area3DStyle.Enable3D = true;

            var series = chart.Series.Add("Sales");
            series.ChartType = SeriesChartType.Bar;
            series.ShowLabelAsValue = true;
            series.Points.AddXY("Q1", 12);
            series.Points.AddXY("Q2", 18);
            series.Points.AddXY("Q3", 9);

            return chart;
        }

        internal static byte[] RenderBarChart3DWithLabels()
        {
            using var chart = BuildBarChart3DWithLabels();
            using var stream = new MemoryStream();
            chart.Save(stream, ChartImageFormat.Png);
            return stream.ToArray();
        }

        /// <summary>
        /// Exercises ErrorBarChart.DrawLabel (ErrorBarChart.cs) with the same bridge pattern.
        /// </summary>
        internal static Chart BuildErrorBarChartWithLabels()
        {
            var chart = new Chart();
            chart.Width = 400;
            chart.Height = 300;

            chart.ChartAreas.Add("Default");

            var series = chart.Series.Add("Measurements");
            series.ChartType = SeriesChartType.ErrorBar;
            series.ShowLabelAsValue = true;
            series.Points.AddXY(1, 10, 8, 12);
            series.Points.AddXY(2, 14, 11, 17);
            series.Points.AddXY(3, 11, 9, 13);

            return chart;
        }

        internal static byte[] RenderErrorBarChartWithLabels()
        {
            using var chart = BuildErrorBarChartWithLabels();
            using var stream = new MemoryStream();
            chart.Save(stream, ChartImageFormat.Png);
            return stream.ToArray();
        }

        /// <summary>
        /// Exercises BoxPlotChart.DrawLabel (BoxPlotChart.cs) with the same bridge pattern.
        /// </summary>
        internal static Chart BuildBoxPlotChartWithLabels()
        {
            var chart = new Chart();
            chart.Width = 400;
            chart.Height = 300;

            chart.ChartAreas.Add("Default");

            var series = chart.Series.Add("Distribution");
            series.ChartType = SeriesChartType.BoxPlot;
            series.ShowLabelAsValue = true;
            series["BoxPlotSeries"] = "Values";
            var valuesSeries = chart.Series.Add("Values");
            valuesSeries.ChartType = SeriesChartType.Point;
            valuesSeries.ShowInLegend = false;
            valuesSeries.Points.AddY(5);
            valuesSeries.Points.AddY(6);
            valuesSeries.Points.AddY(7);
            valuesSeries.Points.AddY(8);
            valuesSeries.Points.AddY(9);
            valuesSeries.Points.AddY(10);
            series.Points.AddY(0, 0, 0, 0, 0, 0);

            return chart;
        }

        internal static byte[] RenderBoxPlotChartWithLabels()
        {
            using var chart = BuildBoxPlotChartWithLabels();
            using var stream = new MemoryStream();
            chart.Save(stream, ChartImageFormat.Png);
            return stream.ToArray();
        }

        /// <summary>
        /// Exercises StockChart.DrawLabel (StockChart.cs) with the same bridge pattern — the
        /// existing StockChartWithTriangleMarks baseline never enables ShowLabelAsValue, so its
        /// DrawPointLabelStringRel call site was never actually exercised before now.
        /// </summary>
        internal static Chart BuildStockChartWithLabels()
        {
            var chart = new Chart();
            chart.Width = 400;
            chart.Height = 300;

            chart.ChartAreas.Add("Default");

            var series = chart.Series.Add("Prices");
            series.ChartType = SeriesChartType.Stock;
            series.ShowLabelAsValue = true;
            series.Points.AddY(12, 8, 9, 11);
            series.Points.AddY(15, 10, 11, 13);
            series.Points.AddY(14, 9, 13, 10);

            return chart;
        }

        internal static byte[] RenderStockChartWithLabels()
        {
            using var chart = BuildStockChartWithLabels();
            using var stream = new MemoryStream();
            chart.Save(stream, ChartImageFormat.Png);
            return stream.ToArray();
        }

        /// <summary>
        /// Exercises TreeMapChart's RenderSeriesLabel/RenderDataPointLabel (TreeMapChart.cs),
        /// which use DrawStringRel/DrawPointLabelStringRel with the same bridge pattern.
        /// </summary>
        internal static Chart BuildTreeMapChartWithLabels()
        {
            var chart = new Chart();
            chart.Width = 400;
            chart.Height = 300;

            chart.ChartAreas.Add("Default");

            var series = chart.Series.Add("Sizes");
            series.ChartType = SeriesChartType.TreeMap;
            series.Points.AddY(10);
            series.Points.AddY(20);
            series.Points.AddY(30);
            series.Points[0].Label = "A";
            series.Points[1].Label = "B";
            series.Points[2].Label = "C";

            return chart;
        }

        internal static byte[] RenderTreeMapChartWithLabels()
        {
            using var chart = BuildTreeMapChartWithLabels();
            using var stream = new MemoryStream();
            chart.Save(stream, ChartImageFormat.Png);
            return stream.ToArray();
        }

        /// <summary>
        /// Exercises RangeChart's shadow block (RangeChart.cs's DrawLine) with a plain solid
        /// fill: Region-from-path, Save/TranslateTransform/Restore, FillRegion, and the
        /// solid-brush hairline pen at the fill-region edges, plus the `brush is ISolidBrush`
        /// forced-border redraw right after.
        /// </summary>
        internal static Chart BuildRangeChartWithShadow()
        {
            var chart = new Chart();
            chart.Width = 400;
            chart.Height = 300;

            chart.ChartAreas.Add("Default");

            var series = chart.Series.Add("Range");
            series.ChartType = SeriesChartType.Range;
            series.ShadowColor = System.Drawing.Color.Black;
            series.ShadowOffset = 3;
            series.Points.AddXY(1, 5, 15);
            series.Points.AddXY(2, 8, 20);
            series.Points.AddXY(3, 3, 18);
            series.Points.AddXY(4, 10, 22);

            return chart;
        }

        internal static byte[] RenderRangeChartWithShadow()
        {
            using var chart = BuildRangeChartWithShadow();
            using var stream = new MemoryStream();
            chart.Save(stream, ChartImageFormat.Png);
            return stream.ToArray();
        }

        /// <summary>
        /// Exercises RangeChart's hatch-brush fill path (RangeChart.cs's DrawLine):
        /// GetHatchBrushResource, FillPath(IBrush, IGraphicsPath), and the hairline pen built
        /// from the hatch brush's ForegroundColor via CreatePen(IBrush, float).
        /// </summary>
        internal static Chart BuildRangeChartWithHatch()
        {
            var chart = new Chart();
            chart.Width = 400;
            chart.Height = 300;

            chart.ChartAreas.Add("Default");

            var series = chart.Series.Add("Range");
            series.ChartType = SeriesChartType.Range;
            series.BackHatchStyle = ChartHatchStyle.Cross;
            series.Points.AddXY(1, 5, 15);
            series.Points.AddXY(2, 8, 20);
            series.Points.AddXY(3, 3, 18);
            series.Points.AddXY(4, 10, 22);

            return chart;
        }

        internal static byte[] RenderRangeChartWithHatch()
        {
            using var chart = BuildRangeChartWithHatch();
            using var stream = new MemoryStream();
            chart.Save(stream, ChartImageFormat.Png);
            return stream.ToArray();
        }

        /// <summary>
        /// Exercises RangeChart's gradient-fill path (RangeChart.cs's DrawLine/
        /// FillLastSeriesGradient): the `gradientFill = true` branch that skips the
        /// per-segment fill/shadow blocks entirely, deferring to the series-level gradient
        /// fill built once the whole series has been walked.
        /// </summary>
        internal static Chart BuildRangeChartWithGradient()
        {
            var chart = new Chart();
            chart.Width = 400;
            chart.Height = 300;

            chart.ChartAreas.Add("Default");

            var series = chart.Series.Add("Range");
            series.ChartType = SeriesChartType.Range;
            series.BackGradientType = GradientType.TopBottom;
            series.BackGradientEndColor = System.Drawing.Color.White;
            series.Points.AddXY(1, 5, 15);
            series.Points.AddXY(2, 8, 20);
            series.Points.AddXY(3, 3, 18);
            series.Points.AddXY(4, 10, 22);

            return chart;
        }

        internal static byte[] RenderRangeChartWithGradient()
        {
            using var chart = BuildRangeChartWithGradient();
            using var stream = new MemoryStream();
            chart.Save(stream, ChartImageFormat.Png);
            return stream.ToArray();
        }

        private static Chart BuildCallout(CalloutStyle style, double anchorX, double anchorY)
        {
            var chart = new Chart();
            chart.Width = 400;
            chart.Height = 300;

            chart.ChartAreas.Add("Default");

            var series = chart.Series.Add("Sales");
            series.ChartType = SeriesChartType.Column;
            series.Points.AddXY("Q1", 12);
            series.Points.AddXY("Q2", 18);
            series.Points.AddXY("Q3", 9);
            series.Points.AddXY("Q4", 24);

            var annotation = new CalloutAnnotation
            {
                CalloutStyle = style,
                Text = "Peak",
                X = 40,
                Y = 40,
                Width = 20,
                Height = 15,
                AnchorX = anchorX,
                AnchorY = anchorY,
            };
            chart.Annotations.Add(annotation);

            return chart;
        }

        private static byte[] RenderCallout(CalloutStyle style, double anchorX, double anchorY)
        {
            using var chart = BuildCallout(style, anchorX, anchorY);
            using var stream = new MemoryStream();
            chart.Save(stream, ChartImageFormat.Png);
            return stream.ToArray();
        }

        /// <summary>
        /// Exercises CalloutAnnotation's DrawRoundedRectCallout (non-ellipse branch:
        /// CreateRoundedRectPath, then re-pointing the nearest path vertex at the anchor).
        /// </summary>
        internal static Chart BuildCalloutRoundedRectangle() => BuildCallout(CalloutStyle.RoundedRectangle, 10, 10);

        internal static byte[] RenderCalloutRoundedRectangle() =>
            RenderCallout(CalloutStyle.RoundedRectangle, 10, 10);

        /// <summary>
        /// Exercises CalloutAnnotation's DrawRoundedRectCallout (ellipse branch).
        /// </summary>
        internal static Chart BuildCalloutEllipse() => BuildCallout(CalloutStyle.Ellipse, 10, 10);

        internal static byte[] RenderCalloutEllipse() =>
            RenderCallout(CalloutStyle.Ellipse, 10, 10);

        /// <summary>
        /// Exercises CalloutAnnotation's DrawRectangleCallout with the anchor point outside the
        /// callout rectangle (builds the 7-point custom polygon, one of 8 direction cases).
        /// </summary>
        internal static Chart BuildCalloutRectangle() => BuildCallout(CalloutStyle.Rectangle, 10, 10);

        internal static byte[] RenderCalloutRectangle() =>
            RenderCallout(CalloutStyle.Rectangle, 10, 10);

        /// <summary>
        /// Exercises CalloutAnnotation's DrawCloudCallout: GetCloudPath/GetCloudOutlinePath
        /// (bridged from their still-concrete cached GDI+ geometry) plus the decorative
        /// anchor-line ellipse chain.
        /// </summary>
        internal static Chart BuildCalloutCloud() => BuildCallout(CalloutStyle.Cloud, 10, 10);

        internal static byte[] RenderCalloutCloud() =>
            RenderCallout(CalloutStyle.Cloud, 10, 10);

        /// <summary>
        /// Exercises CalloutAnnotation's DrawPerspectiveCallout: the triangular wedge(s) merged
        /// into the main rectangle via SetMarkers/AddPath, which SplitAtMarkers later has to
        /// carve back apart for hot-region generation.
        /// </summary>
        internal static Chart BuildCalloutPerspective() => BuildCallout(CalloutStyle.Perspective, 10, 10);

        internal static byte[] RenderCalloutPerspective() =>
            RenderCallout(CalloutStyle.Perspective, 10, 10);

        /// <summary>
        /// Exercises CalloutAnnotation's DrawRectangleLineCallout with drawRectangle: true
        /// (Borderline style) — the anchor-line stroke widened via
        /// ChartGraphics.Widen(IGraphicsPath, IPen) and merged via SetMarkers/AddPath.
        /// </summary>
        internal static Chart BuildCalloutBorderline() => BuildCallout(CalloutStyle.Borderline, 10, 10);

        internal static byte[] RenderCalloutBorderline() =>
            RenderCallout(CalloutStyle.Borderline, 10, 10);

        /// <summary>
        /// Exercises CalloutAnnotation's DrawRectangleLineCallout with drawRectangle: false
        /// (SimpleLine style) — no filled rectangle, just the widened anchor-line stroke(s).
        /// </summary>
        internal static Chart BuildCalloutSimpleLine() => BuildCallout(CalloutStyle.SimpleLine, 10, 10);

        internal static byte[] RenderCalloutSimpleLine() =>
            RenderCallout(CalloutStyle.SimpleLine, 10, 10);

        /// <summary>
        /// Exercises ChartGraphics3D's frontLinePen field (Draw3DPolygon/Draw3DSurface): a 3D
        /// line chart with the default Area3DStyle.Perspective == 0 defers drawing each
        /// segment's front-facing edge until the next segment's call (or the series' last call),
        /// carrying the pen across calls via the frontLinePen/frontLinePoint1/frontLinePoint2
        /// fields. Multiple points ensure the carry-over path actually executes more than once.
        /// </summary>
        internal static Chart BuildLineChart3D()
        {
            var chart = new Chart();
            chart.Width = 400;
            chart.Height = 300;

            chart.ChartAreas.Add("Default");
            chart.ChartAreas[0].Area3DStyle.Enable3D = true;

            var series = chart.Series.Add("Sales");
            series.ChartType = SeriesChartType.Line;
            series.BorderWidth = 3;
            series.Points.AddXY("Q1", 12);
            series.Points.AddXY("Q2", 18);
            series.Points.AddXY("Q3", 9);
            series.Points.AddXY("Q4", 24);

            return chart;
        }

        internal static byte[] RenderLineChart3D()
        {
            using var chart = BuildLineChart3D();
            using var stream = new MemoryStream();
            chart.Save(stream, ChartImageFormat.Png);
            return stream.ToArray();
        }

        /// <summary>
        /// Exercises Axis.DrawAxisTitle (2D path) — no other sample chart sets Axis.Title, so the
        /// TitleFont/TitleColor Font/Brush-to-IChartFont/IBrush bridge added during the B2 sweep
        /// (tasks/chart-gdi-type-abstraction.md) was previously unverified against real pixels.
        /// </summary>
        internal static Chart BuildChartWithAxisTitles()
        {
            var chart = new Chart();
            chart.Width = 400;
            chart.Height = 300;

            chart.ChartAreas.Add("Default");
            chart.ChartAreas[0].AxisX.Title = "Quarter";
            chart.ChartAreas[0].AxisY.Title = "Revenue";

            var series = chart.Series.Add("Sales");
            series.ChartType = SeriesChartType.Bar;
            series.Points.AddXY("Q1", 12);
            series.Points.AddXY("Q2", 18);
            series.Points.AddXY("Q3", 9);
            series.Points.AddXY("Q4", 24);

            return chart;
        }

        internal static byte[] RenderChartWithAxisTitles()
        {
            using var chart = BuildChartWithAxisTitles();
            using var stream = new MemoryStream();
            chart.Save(stream, ChartImageFormat.Png);
            return stream.ToArray();
        }

        /// <summary>
        /// Exercises Axis.DrawAxis3DTitle (the 3D counterpart of DrawAxisTitle, taken by a
        /// non-circular 3D chart area) — same TitleFont/TitleColor bridge, different call site,
        /// previously unverified for the same reason as RenderChartWithAxisTitles.
        /// </summary>
        internal static Chart BuildChart3DWithAxisTitles()
        {
            var chart = new Chart();
            chart.Width = 400;
            chart.Height = 300;

            chart.ChartAreas.Add("Default");
            chart.ChartAreas[0].Area3DStyle.Enable3D = true;
            chart.ChartAreas[0].AxisX.Title = "Quarter";
            chart.ChartAreas[0].AxisY.Title = "Revenue";

            var series = chart.Series.Add("Sales");
            series.ChartType = SeriesChartType.Column;
            series.Points.AddXY("Q1", 12);
            series.Points.AddXY("Q2", 18);
            series.Points.AddXY("Q3", 9);
            series.Points.AddXY("Q4", 24);

            return chart;
        }

        internal static byte[] RenderChart3DWithAxisTitles()
        {
            using var chart = BuildChart3DWithAxisTitles();
            using var stream = new MemoryStream();
            chart.Save(stream, ChartImageFormat.Png);
            return stream.ToArray();
        }

        /// <summary>
        /// Exercises TextAnnotation.DrawText for a given TextStyle — no prior baseline used
        /// TextAnnotation at all, so its Font/Brush/StringFormat-to-IChartFont/IBrush/ITextFormat
        /// bridge (added in the annotations B2 sweep) was previously unverified. TextStyle.Frame
        /// in particular exercises the new IGraphicsPath.AddString(RectangleF) overload.
        /// </summary>
        private static Chart BuildTextAnnotation(TextStyle style, bool ellipse = false)
        {
            var chart = new Chart();
            chart.Width = 400;
            chart.Height = 300;

            chart.ChartAreas.Add("Default");

            var series = chart.Series.Add("Sales");
            series.ChartType = SeriesChartType.Column;
            series.Points.AddXY("Q1", 12);
            series.Points.AddXY("Q2", 18);
            series.Points.AddXY("Q3", 9);
            series.Points.AddXY("Q4", 24);

            TextAnnotation annotation = ellipse ? new EllipseAnnotation() : new TextAnnotation();
            annotation.Text = "Peak Quarter";
            annotation.TextStyle = style;
            annotation.X = 20;
            annotation.Y = 20;
            annotation.Width = 40;
            annotation.Height = 15;
            chart.Annotations.Add(annotation);

            return chart;
        }

        private static byte[] RenderTextAnnotation(TextStyle style, bool ellipse = false)
        {
            using var chart = BuildTextAnnotation(style, ellipse);
            using var stream = new MemoryStream();
            chart.Save(stream, ChartImageFormat.Png);
            return stream.ToArray();
        }

        internal static Chart BuildTextAnnotationDefault() => BuildTextAnnotation(TextStyle.Default);
        internal static byte[] RenderTextAnnotationDefault() => RenderTextAnnotation(TextStyle.Default);

        internal static Chart BuildTextAnnotationFrame() => BuildTextAnnotation(TextStyle.Frame);
        internal static byte[] RenderTextAnnotationFrame() => RenderTextAnnotation(TextStyle.Frame);

        internal static Chart BuildTextAnnotationEmbed() => BuildTextAnnotation(TextStyle.Embed);
        internal static byte[] RenderTextAnnotationEmbed() => RenderTextAnnotation(TextStyle.Embed);

        internal static Chart BuildTextAnnotationEmboss() => BuildTextAnnotation(TextStyle.Emboss);
        internal static byte[] RenderTextAnnotationEmboss() => RenderTextAnnotation(TextStyle.Emboss);

        internal static Chart BuildTextAnnotationShadow() => BuildTextAnnotation(TextStyle.Shadow);
        internal static byte[] RenderTextAnnotationShadow() => RenderTextAnnotation(TextStyle.Shadow);

        internal static Chart BuildTextAnnotationEllipse() => BuildTextAnnotation(TextStyle.Default, ellipse: true);
        internal static byte[] RenderTextAnnotationEllipse() => RenderTextAnnotation(TextStyle.Default, ellipse: true);

        /// <summary>
        /// Exercises Label.PaintCircular (the circular-chart-area axis title path) — a Radar
        /// series makes the chart area circular (RadarChart.CircularChartArea), and each point's
        /// AxisLabel becomes a circular axis title (ChartArea.GetCircularAxisList).
        /// </summary>
        internal static Chart BuildRadarChartWithAxisLabels()
        {
            var chart = new Chart();
            chart.Width = 400;
            chart.Height = 400;

            chart.ChartAreas.Add("Default");

            var series = chart.Series.Add("Sales");
            series.ChartType = SeriesChartType.Radar;
            series.Points.AddXY(1, 12);
            series.Points[0].AxisLabel = "North";
            series.Points.AddXY(2, 18);
            series.Points[1].AxisLabel = "East";
            series.Points.AddXY(3, 9);
            series.Points[2].AxisLabel = "South";
            series.Points.AddXY(4, 24);
            series.Points[3].AxisLabel = "West";

            return chart;
        }

        internal static byte[] RenderRadarChartWithAxisLabels()
        {
            using var chart = BuildRadarChartWithAxisLabels();
            using var stream = new MemoryStream();
            chart.Save(stream, ChartImageFormat.Png);
            return stream.ToArray();
        }

        /// <summary>
        /// Exercises StripLine.PaintTitle — no prior baseline used a StripLine with a Title set,
        /// so its StringFormat/Font/Brush-to-ITextFormat/IChartFont/IBrush bridge (annotations/
        /// axis/label sweep) was previously unverified.
        /// </summary>
        internal static Chart BuildStripLineWithTitle()
        {
            var chart = new Chart();
            chart.Width = 400;
            chart.Height = 300;

            chart.ChartAreas.Add("Default");
            var stripLine = new StripLine();
            chart.ChartAreas[0].AxisY.StripLines.Add(stripLine);
            stripLine.IntervalOffset = 15;
            stripLine.StripWidth = 5;
            stripLine.BackColor = Color.LightGray;
            stripLine.Title = "Target";

            var series = chart.Series.Add("Sales");
            series.ChartType = SeriesChartType.Bar;
            series.Points.AddXY("Q1", 12);
            series.Points.AddXY("Q2", 18);
            series.Points.AddXY("Q3", 9);
            series.Points.AddXY("Q4", 24);

            return chart;
        }

        internal static byte[] RenderStripLineWithTitle()
        {
            using var chart = BuildStripLineWithTitle();
            using var stream = new MemoryStream();
            chart.Save(stream, ChartImageFormat.Png);
            return stream.ToArray();
        }

        /// <summary>
        /// Exercises Title.Paint for a given TextStyle — no prior baseline set Title.Style away
        /// from Default, so Title's own Font/Brush/StringFormat bridge (distinct from
        /// TextAnnotation's, same switch shape) was previously unverified. TextStyle.Frame in
        /// particular exercises the new IGraphicsPath.AddString(RectangleF) overload from a
        /// second, independent call site.
        /// </summary>
        private static Chart BuildTitleWithStyle(TextStyle style)
        {
            var chart = new Chart();
            chart.Width = 400;
            chart.Height = 300;

            var title = chart.Titles.Add("Chart Title");
            title.Style = style;

            chart.ChartAreas.Add("Default");

            var series = chart.Series.Add("Sales");
            series.ChartType = SeriesChartType.Column;
            series.Points.AddXY("Q1", 12);
            series.Points.AddXY("Q2", 18);
            series.Points.AddXY("Q3", 9);
            series.Points.AddXY("Q4", 24);

            return chart;
        }

        private static byte[] RenderTitleWithStyle(TextStyle style)
        {
            using var chart = BuildTitleWithStyle(style);
            using var stream = new MemoryStream();
            chart.Save(stream, ChartImageFormat.Png);
            return stream.ToArray();
        }

        internal static Chart BuildTitleFrame() => BuildTitleWithStyle(TextStyle.Frame);
        internal static byte[] RenderTitleFrame() => RenderTitleWithStyle(TextStyle.Frame);

        internal static Chart BuildTitleEmbed() => BuildTitleWithStyle(TextStyle.Embed);
        internal static byte[] RenderTitleEmbed() => RenderTitleWithStyle(TextStyle.Embed);

        internal static Chart BuildTitleEmboss() => BuildTitleWithStyle(TextStyle.Emboss);
        internal static byte[] RenderTitleEmboss() => RenderTitleWithStyle(TextStyle.Emboss);

        internal static Chart BuildTitleShadow() => BuildTitleWithStyle(TextStyle.Shadow);
        internal static byte[] RenderTitleShadow() => RenderTitleWithStyle(TextStyle.Shadow);

        /// <summary>
        /// Exercises Legend.DrawLegendTitle and Legend.DrawLegendHeader — no prior baseline set
        /// Legend.Title or a LegendCellColumn.HeaderText, so their Font/Brush/StringFormat
        /// bridges (annotations/axis/label sweep) were previously unverified. (LegendCell.
        /// PaintCellText itself is already exercised by every other baseline, since a default
        /// "Default" legend with a text cell is always present.)
        /// </summary>
        internal static Chart BuildLegendWithTitleAndHeader()
        {
            var chart = new Chart();
            chart.Width = 400;
            chart.Height = 300;

            chart.ChartAreas.Add("Default");

            var series = chart.Series.Add("Sales");
            series.ChartType = SeriesChartType.Bar;
            series.Points.AddXY("Q1", 12);
            series.Points.AddXY("Q2", 18);
            series.Points.AddXY("Q3", 9);
            series.Points.AddXY("Q4", 24);

            var legend = chart.Legends["Default"];
            legend.Title = "Series";
            legend.TitleAlignment = StringAlignment.Center;
            legend.CellColumns.Add("Name", LegendCellColumnType.Text, "#LEGENDTEXT", ContentAlignment.MiddleCenter);

            return chart;
        }

        internal static byte[] RenderLegendWithTitleAndHeader()
        {
            using var chart = BuildLegendWithTitleAndHeader();
            using var stream = new MemoryStream();
            chart.Save(stream, ChartImageFormat.Png);
            return stream.ToArray();
        }
    }
}
