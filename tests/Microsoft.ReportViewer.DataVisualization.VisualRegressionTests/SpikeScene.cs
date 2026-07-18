using System.Drawing;
using Microsoft.Reporting.Chart.WebForms;
using Microsoft.Reporting.Chart.WebForms.Rendering;

namespace Microsoft.ReportViewer.DataVisualization.VisualRegressionTests
{
    /// <summary>
    /// Phase 0 spike (tasks/chart-cross-platform-implementation.md) — a hand-built bar-chart
    /// scene written ONLY against the backend-agnostic <see cref="IChartRenderingEngine"/>
    /// interface-typed overloads and <see cref="IDrawingResourceFactory"/>. It never touches
    /// System.Drawing directly, so unlike <see cref="SampleCharts"/> (which goes through the
    /// real <see cref="Chart"/> object model and therefore requires GDI+ to even construct a
    /// Chart on Linux — see spike report), this scene can be driven by either the existing
    /// GDI+ adapters or the new Skia adapters, on Windows or Linux.
    /// </summary>
    internal static class SpikeScene
    {
        internal const int Width = 400;
        internal const int Height = 300;

        internal static void Paint(IChartRenderingEngine engine, IDrawingResourceFactory factory)
        {
            using var background = factory.CreateSolidBrush(Color.White);
            engine.FillRectangle(background, new RectangleF(0, 0, Width, Height));

            using var titleFont = factory.CreateFont("Arial", 14f, FontStyle.Bold);
            using var labelFont = factory.CreateFont("Arial", 10f);
            using var textBrush = factory.CreateSolidBrush(Color.Black);
            using var centerFormat = factory.CreateTextFormat();
            centerFormat.Alignment = StringAlignment.Center;

            engine.DrawString("Sales (Spike Scene)", titleFont, textBrush, new RectangleF(0, 4, Width, 20), centerFormat);

            const float chartLeft = 40f;
            const float chartRight = Width - 20f;
            const float chartTop = 40f;
            const float chartBottom = Height - 40f;

            using (var axisPen = factory.CreatePen(Color.Black, 1f))
            {
                engine.DrawLine(axisPen, new PointF(chartLeft, chartBottom), new PointF(chartRight, chartBottom));
                engine.DrawLine(axisPen, new PointF(chartLeft, chartTop), new PointF(chartLeft, chartBottom));
            }

            var categories = new[] { "Q1", "Q2", "Q3", "Q4" };
            var values = new[] { 12f, 18f, 9f, 24f };
            const float maxValue = 24f;

            using var barBrush = factory.CreateSolidBrush(Color.SteelBlue);

            var chartWidth = chartRight - chartLeft;
            var chartHeight = chartBottom - chartTop;
            var slotWidth = chartWidth / categories.Length;
            var barWidth = slotWidth * 0.6f;

            for (var i = 0; i < categories.Length; i++)
            {
                var barHeight = values[i] / maxValue * chartHeight;
                var x = chartLeft + i * slotWidth + (slotWidth - barWidth) / 2f;
                var y = chartBottom - barHeight;

                engine.FillRectangle(barBrush, new RectangleF(x, y, barWidth, barHeight));
                engine.DrawString(categories[i], labelFont, textBrush, new RectangleF(x, chartBottom + 4, barWidth, 16), centerFormat);
            }
        }
    }
}
