using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using Microsoft.Reporting.Chart.WebForms;
using Microsoft.Reporting.Chart.WebForms.Rendering.Skia;
using Microsoft.Reporting.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Microsoft.ReportViewer.DataVisualization.VisualRegressionTests
{
    /// <summary>
    /// Functional smoke tests (E1) for the Skia adapters' previously-stubbed members —
    /// SkiaClipRegion's region algebra, SkiaResourceFactory's brush-based pens/texture
    /// brushes/WrapImage, and SkiaPen's dash translation. These exercise real runtime
    /// behavior (SKPath.Op/SKShader construction/pixel edits), not just compilation —
    /// there is no visual baseline for the Skia backend yet (see SpikeSceneTests), so
    /// this is the only automated signal these code paths actually work. Not a pixel-
    /// regression gate.
    /// </summary>
    [TestClass]
    public class SkiaResourceFactoryTests
    {
        private static SkiaResourceFactory Factory => new SkiaResourceFactory();

        private static IChartImage CreateTestImage(int width, int height, SKColor fill)
        {
            using var bitmap = new SKBitmap(width, height);
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(fill);
            }
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = new MemoryStream(data.ToArray());
            return Factory.LoadImage(stream);
        }

        [TestMethod]
        public void ClipRegion_IntersectRect_NarrowsBounds()
        {
            using var region = Factory.CreateRegion(new RectangleF(0, 0, 100, 100));
            region.Intersect(new RectangleF(50, 50, 100, 100));

            var bounds = region.GetBounds(engine: null);
            Assert.AreEqual(50f, bounds.X, 0.01f);
            Assert.AreEqual(50f, bounds.Y, 0.01f);
            Assert.AreEqual(50f, bounds.Width, 0.01f);
            Assert.AreEqual(50f, bounds.Height, 0.01f);
        }

        [TestMethod]
        public void ClipRegion_UnionRect_GrowsBounds()
        {
            using var region = Factory.CreateRegion(new RectangleF(0, 0, 10, 10));
            region.Union(new RectangleF(20, 20, 10, 10));

            var bounds = region.GetBounds(engine: null);
            Assert.AreEqual(0f, bounds.X, 0.01f);
            Assert.AreEqual(0f, bounds.Y, 0.01f);
            Assert.AreEqual(30f, bounds.Width, 0.01f);
            Assert.AreEqual(30f, bounds.Height, 0.01f);
        }

        [TestMethod]
        public void ClipRegion_ExcludeRect_RemovesVisibility()
        {
            using var region = Factory.CreateRegion(new RectangleF(0, 0, 100, 100));
            region.Exclude(new RectangleF(0, 0, 50, 100));

            Assert.IsFalse(region.IsVisible(new PointF(25, 50)), "Excluded half should no longer be visible.");
            Assert.IsTrue(region.IsVisible(new PointF(75, 50)), "Non-excluded half should remain visible.");
        }

        [TestMethod]
        public void ClipRegion_MakeEmpty_ReportsEmpty()
        {
            using var region = Factory.CreateRegion(new RectangleF(0, 0, 100, 100));
            Assert.IsFalse(region.IsEmpty(engine: null));

            region.MakeEmpty();
            Assert.IsTrue(region.IsEmpty(engine: null));
        }

        [TestMethod]
        public void ClipRegion_MakeInfinite_ReportsInfinite()
        {
            using var region = Factory.CreateRegion(new RectangleF(0, 0, 100, 100));
            Assert.IsFalse(region.IsInfinite(engine: null));

            region.MakeInfinite();
            Assert.IsTrue(region.IsInfinite(engine: null));
            Assert.IsTrue(region.IsVisible(new PointF(1_000_000, 1_000_000)));
        }

        [TestMethod]
        public void ClipRegion_Clone_IsIndependent()
        {
            using var region = Factory.CreateRegion(new RectangleF(0, 0, 100, 100));
            using var clone = region.Clone();
            region.Union(new RectangleF(200, 200, 10, 10));

            var cloneBounds = clone.GetBounds(engine: null);
            Assert.AreEqual(100f, cloneBounds.Width, 0.01f, "Clone must not see mutations made to the original after cloning.");
        }

        [TestMethod]
        public void CreatePen_FromSolidBrush_CopiesColor()
        {
            var factory = Factory;
            using var brush = factory.CreateSolidBrush(Color.FromArgb(255, 10, 20, 30));
            using var pen = factory.CreatePen(brush, 2f) as SkiaPen;

            Assert.IsNotNull(pen);
            Assert.AreEqual(Color.FromArgb(255, 10, 20, 30), pen.Color);
            Assert.AreEqual(2f, pen.Width);
        }

        [TestMethod]
        public void CreatePen_DashStyle_SetsPathEffect()
        {
            var factory = Factory;
            using var pen = factory.CreatePen(Color.Black, 1f) as SkiaPen;
            Assert.IsNotNull(pen);
            Assert.IsNull(pen.NativePaint.PathEffect);

            pen.DashStyle = DashStyle.Dash;
            Assert.IsNotNull(pen.NativePaint.PathEffect);

            pen.DashStyle = DashStyle.Solid;
            Assert.IsNull(pen.NativePaint.PathEffect);
        }

        [TestMethod]
        public void CreateTextureBrush_WholeImage_BuildsBitmapShader()
        {
            var factory = Factory;
            using var image = CreateTestImage(4, 4, SKColors.Red);
            using var brush = factory.CreateTextureBrush(image, WrapMode.Tile) as SkiaTextureBrush;

            Assert.IsNotNull(brush);
            Assert.IsNotNull(brush.NativePaint.Shader);
        }

        [TestMethod]
        public void CreateTextureBrush_IntoRect_BuildsBitmapShader()
        {
            var factory = Factory;
            using var image = CreateTestImage(4, 4, SKColors.Blue);
            using var options = factory.CreateImageDrawOptions();
            options.SetTransparentColor(Color.Blue);
            options.SetWrapMode(WrapMode.Clamp);

            using var brush = factory.CreateTextureBrush(image, new RectangleF(0, 0, 40, 40), options) as SkiaTextureBrush;

            Assert.IsNotNull(brush);
            Assert.IsNotNull(brush.NativePaint.Shader);
        }

        [TestMethod]
        public void WrapImage_BridgesGdiBitmap_PreservesSize()
        {
            using var gdiBitmap = new Bitmap(6, 8);
            using var image = Factory.WrapImage(gdiBitmap);

            Assert.AreEqual(6, image.Width);
            Assert.AreEqual(8, image.Height);
        }

        [TestMethod]
        public void CreatePen_FromUnsupportedBrushKind_Throws()
        {
            var factory = Factory;
            using var gradientBrush = new SkiaLinearGradientBrush();

            Assert.ThrowsException<System.NotSupportedException>(() => factory.CreatePen(gradientBrush, 1f));
        }

        [TestMethod]
        public void DrawPie_DoesNotThrow_AndPaintsSomething()
        {
            using var surfaceBitmap = new SKBitmap(50, 50);
            using var canvas = new SKCanvas(surfaceBitmap);
            var graphics = new SkiaChartGraphics { Canvas = canvas };
            using var brush = Factory.CreateSolidBrush(Color.Red);

            graphics.FillPie(brush, 5, 5, 30, 30, 0, 270);
            canvas.Flush();

            Assert.AreNotEqual(default(SKColor), surfaceBitmap.GetPixel(20, 20));
        }

        [TestMethod]
        public void DrawArc_DoesNotThrow()
        {
            using var surfaceBitmap = new SKBitmap(50, 50);
            using var canvas = new SKCanvas(surfaceBitmap);
            var graphics = new SkiaChartGraphics { Canvas = canvas };
            using var pen = Factory.CreatePen(Color.Black, 1f);

            graphics.DrawArc(pen, 5, 5, 30, 30, 0, 180);
        }

        [TestMethod]
        public void DrawCurve_ProducesNonEmptyPath()
        {
            using var surfaceBitmap = new SKBitmap(50, 50);
            using var canvas = new SKCanvas(surfaceBitmap);
            var graphics = new SkiaChartGraphics { Canvas = canvas };
            using var pen = Factory.CreatePen(Color.Black, 1f);
            var points = new[] { new PointF(0, 25), new PointF(15, 5), new PointF(30, 25), new PointF(45, 5) };

            graphics.DrawCurve(pen, points, 0, points.Length - 1, 0.5f);
            canvas.Flush();

            Assert.AreNotEqual(default(SKColor), surfaceBitmap.GetPixel(15, 5));
        }

        [TestMethod]
        public void DrawLines_PaintsOpenPolyline()
        {
            using var surfaceBitmap = new SKBitmap(50, 50);
            using var canvas = new SKCanvas(surfaceBitmap);
            var graphics = new SkiaChartGraphics { Canvas = canvas };
            using var pen = Factory.CreatePen(Color.Black, 1f);

            graphics.DrawLines(pen, new[] { new PointF(0, 0), new PointF(40, 0), new PointF(40, 40) });
            canvas.Flush();

            Assert.AreNotEqual(default(SKColor), surfaceBitmap.GetPixel(40, 20));
        }

        [TestMethod]
        public void FillRegion_PaintsRegionBounds()
        {
            using var surfaceBitmap = new SKBitmap(50, 50);
            using var canvas = new SKCanvas(surfaceBitmap);
            var graphics = new SkiaChartGraphics { Canvas = canvas };
            using var brush = Factory.CreateSolidBrush(Color.Blue);
            using var region = Factory.CreateRegion(new RectangleF(5, 5, 20, 20));

            graphics.FillRegion(brush, region);
            canvas.Flush();

            Assert.AreEqual(new SKColor(0, 0, 255), surfaceBitmap.GetPixel(10, 10));
        }

        [TestMethod]
        public void DrawImage_IChartImage_BlitsBitmapIntoDestRect()
        {
            using var surfaceBitmap = new SKBitmap(50, 50);
            using var canvas = new SKCanvas(surfaceBitmap);
            var graphics = new SkiaChartGraphics { Canvas = canvas };
            using var image = CreateTestImage(4, 4, SKColors.Green);

            graphics.DrawImage(image, new Rectangle(5, 5, 20, 20), 0, 0, 4, 4, GraphicsUnit.Pixel, Factory.CreateImageDrawOptions());
            canvas.Flush();

            Assert.AreEqual(new SKColor(0, 128, 0), surfaceBitmap.GetPixel(15, 15));
        }

        [TestMethod]
        public void DrawImage_IChartImage_WithTransparentColor_SkipsColorKeyedPixels()
        {
            using var surfaceBitmap = new SKBitmap(50, 50);
            using var canvas = new SKCanvas(surfaceBitmap);
            canvas.Clear(SKColors.White);
            var graphics = new SkiaChartGraphics { Canvas = canvas };
            using var image = CreateTestImage(4, 4, SKColors.Green);
            var options = Factory.CreateImageDrawOptions();
            options.SetTransparentColor(Color.FromArgb(0, 128, 0));

            graphics.DrawImage(image, new Rectangle(5, 5, 20, 20), 0, 0, 4, 4, GraphicsUnit.Pixel, options);
            canvas.Flush();

            Assert.AreEqual(SKColors.White, surfaceBitmap.GetPixel(15, 15));
        }

        [TestMethod]
        public void ClipRegion_ToDrawablePath_ReturnsIndependentCopy()
        {
            using var region = Factory.CreateRegion(new RectangleF(0, 0, 10, 10)) as SkiaClipRegion;
            using var path = region.ToDrawablePath();
            region.Union(new RectangleF(50, 50, 5, 5));

            Assert.AreEqual(10f, path.Bounds.Width, 0.01f, "Path returned by ToDrawablePath must not reflect later mutations to the region.");
        }
    }
}
