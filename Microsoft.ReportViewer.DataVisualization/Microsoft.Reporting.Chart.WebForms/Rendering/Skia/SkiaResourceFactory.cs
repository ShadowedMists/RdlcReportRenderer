using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using SkiaSharp;

namespace Microsoft.Reporting.Chart.WebForms.Rendering.Skia
{
	/// <summary>
	/// Spike (tasks/chart-cross-platform-implementation.md Phase 0) — the SkiaSharp
	/// implementation of <see cref="IDrawingResourceFactory"/>, sibling to
	/// <c>GdiResourceFactory</c>. Every resource it creates wraps a SkiaSharp object
	/// instead of a <see cref="System.Drawing"/> one, so a caller that only depends on
	/// this factory + the <c>Rendering.*</c> interfaces never triggers GDI+
	/// initialization — proving that path is constructible on Linux (see spike report;
	/// GDI+ itself cannot be constructed there even with libgdiplus installed).
	/// </summary>
	internal sealed class SkiaResourceFactory : IDrawingResourceFactory
	{
		public IPen CreatePen(Color color, float width) => new SkiaPen(color, width);

		public IPen CreatePen(IBrush brush, float width) =>
			throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public ISolidBrush CreateSolidBrush(Color color) => new SkiaSolidBrush(color);

		public ILinearGradientBrush CreateLinearGradientBrush(RectangleF rect, Color startColor, Color endColor, float angle) =>
			throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public ITextureBrush CreateTextureBrush(IChartImage image, WrapMode wrapMode) =>
			throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public ITextureBrush CreateTextureBrush(IChartImage image, RectangleF rect, IImageDrawOptions options) =>
			throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public IHatchBrush CreateHatchBrush(HatchStyle style, Color foreColor, Color backColor) =>
			throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public IPathGradientBrush CreatePathGradientBrush(IGraphicsPath path) =>
			throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public IChartImage LoadImage(Stream stream) => new SkiaChartImage(SKBitmap.Decode(stream));

		public IChartImage WrapImage(Image image) =>
			throw new NotSupportedException("ImageLoader's chart.Images/ResourceManager/WebRequest/File pipeline is GDI+-only; not available on the Skia backend yet.");

		public IChartFont CreateFont(string familyName, float sizeInPoints) => new SkiaChartFont(familyName, sizeInPoints);

		public IChartFont CreateFont(string familyName, float size, FontStyle style) => new SkiaChartFont(familyName, size, style);

		public IChartFont CreateFont(string familyName, float size, FontStyle style, GraphicsUnit unit) => new SkiaChartFont(familyName, size, style);

		public IChartFont DeriveFont(IChartFont prototype, FontStyle style) => new SkiaChartFont(prototype.FontFamilyName, prototype.SizeInPoints, style);

		public IChartFont DeriveFont(IChartFont prototype, float newSizeInPoints) => new SkiaChartFont(prototype.FontFamilyName, newSizeInPoints, prototype.Style);

		public IChartFont WrapFont(Font font) => new SkiaChartFont(font.FontFamily.Name, font.Size, font.Style);

		public ITextFormat CreateTextFormat() => new SkiaTextFormat();

		public ITextFormat CreateTypographicTextFormat() => new SkiaTextFormat();

		public ITextFormat CreateDefaultTextFormat() => new SkiaTextFormat();

		public IGraphicsPath CreatePath() => new SkiaGraphicsPath();

		public IGraphicsPath CreatePath(PointF[] points, byte[] types) => throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public IClipRegion CreateRegion() => new SkiaClipRegion();

		public IClipRegion CreateRegion(RectangleF rect) => throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public IClipRegion CreateRegion(IGraphicsPath path) => throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public IImageDrawOptions CreateImageDrawOptions() => new SkiaImageDrawOptions();
	}
}
