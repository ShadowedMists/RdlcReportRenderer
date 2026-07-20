using Microsoft.Reporting.Chart.WebForms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ReportViewer.DataVisualization.VisualRegressionTests
{
    /// <summary>
    /// CalloutAnnotation's hot-region splitting (CalloutAnnotation.cs's private SplitAtMarkers,
    /// which replaced GraphicsPathIterator.NextMarker) produces no visible pixels, so it can't be
    /// checked by the PNG baseline tests in ChartVisualRegressionTests. These tests instead call
    /// the public Chart.HitTest API, which exercises the real ProcessModeRegions/AddHotRegion path,
    /// to confirm the carved-apart sub-shapes (e.g. a Perspective callout's triangular wedges,
    /// merged into the main path via SetMarkers/AddPath) are geometrically correct hot regions.
    /// </summary>
    [TestClass]
    public class CalloutAnnotationHitTestTests
    {
        private static (Chart chart, CalloutAnnotation annotation) BuildChartWithPerspectiveCallout()
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

            // Rect (in pixels, 400x300 canvas): X=40% Y=40% W=20% H=15% -> (160,120)-(240,165).
            // Anchor at (10%,10%) -> pixel (40,30), above-and-left of the rect, so
            // DrawPerspectiveCallout adds both the top wedge and the left wedge (merged via
            // SetMarkers/AddPath), giving 3 marker segments total: rect + 2 wedges.
            var annotation = new CalloutAnnotation
            {
                CalloutStyle = CalloutStyle.Perspective,
                Text = "Peak",
                X = 40,
                Y = 40,
                Width = 20,
                Height = 15,
                AnchorX = 10,
                AnchorY = 10,
            };
            chart.Annotations.Add(annotation);
            return (chart, annotation);
        }

        [TestMethod]
        public void PerspectiveCallout_MainRectangle_HitsAnnotation()
        {
            var (chart, annotation) = BuildChartWithPerspectiveCallout();
            using (chart)
            {
                var result = chart.HitTest(200, 142); // center of the (160,120)-(240,165) rect
                Assert.AreEqual(ChartElementType.Annotation, result.ChartElementType);
                Assert.AreSame(annotation, result.Object);
            }
        }

        [TestMethod]
        public void PerspectiveCallout_TopWedge_HitsAnnotation()
        {
            var (chart, annotation) = BuildChartWithPerspectiveCallout();
            using (chart)
            {
                // Centroid of the top wedge triangle (160,120)-(240,120)-(40,30).
                var result = chart.HitTest(147, 90);
                Assert.AreEqual(ChartElementType.Annotation, result.ChartElementType);
                Assert.AreSame(annotation, result.Object);
            }
        }

        [TestMethod]
        public void PerspectiveCallout_LeftWedge_HitsAnnotation()
        {
            var (chart, annotation) = BuildChartWithPerspectiveCallout();
            using (chart)
            {
                // Centroid of the left wedge triangle (160,165)-(160,120)-(40,30).
                var result = chart.HitTest(120, 105);
                Assert.AreEqual(ChartElementType.Annotation, result.ChartElementType);
                Assert.AreSame(annotation, result.Object);
            }
        }

        [TestMethod]
        public void PerspectiveCallout_FarAwayPoint_DoesNotHitAnnotation()
        {
            var (chart, annotation) = BuildChartWithPerspectiveCallout();
            using (chart)
            {
                var result = chart.HitTest(10, 280);
                Assert.AreNotEqual(ChartElementType.Annotation, result.ChartElementType);
            }
        }
    }
}
