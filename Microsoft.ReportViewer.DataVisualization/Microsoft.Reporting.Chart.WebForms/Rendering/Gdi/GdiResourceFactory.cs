using System.Drawing;
using System.Drawing.Drawing2D;

namespace Microsoft.Reporting.Chart.WebForms.Rendering.Gdi
{
	/// <summary>
	/// Milestone A2 — the GDI+ implementation of <see cref="IDrawingResourceFactory"/>.
	/// Behavior-identical to today: every method constructs the same concrete
	/// <c>System.Drawing</c> object the engine currently creates directly, just
	/// wrapped behind its port interface.
	/// </summary>
	internal sealed class GdiResourceFactory : IDrawingResourceFactory
	{
		public IPen CreatePen(Color color, float width) => new GdiPen(color, width);

		public ISolidBrush CreateSolidBrush(Color color) => new GdiSolidBrush(color);

		public ILinearGradientBrush CreateLinearGradientBrush(RectangleF rect, Color startColor, Color endColor, float angle) =>
			new GdiLinearGradientBrush(rect, startColor, endColor, angle);

		public ITextureBrush CreateTextureBrush(IChartImage image, WrapMode wrapMode) =>
			new GdiTextureBrush(((GdiChartImage)image).NativeImage, wrapMode);

		public IHatchBrush CreateHatchBrush(HatchStyle style, Color foreColor, Color backColor) =>
			new GdiHatchBrush(style, foreColor, backColor);

		public IChartFont CreateFont(string familyName, float sizeInPoints) =>
			new GdiChartFont(new Font(familyName, sizeInPoints));

		public IChartFont CreateFont(string familyName, float size, FontStyle style) =>
			new GdiChartFont(new Font(familyName, size, style));

		public IChartFont CreateFont(string familyName, float size, FontStyle style, GraphicsUnit unit) =>
			new GdiChartFont(new Font(familyName, size, style, unit));

		public IChartFont DeriveFont(IChartFont prototype, FontStyle style)
		{
			var nativePrototype = ((GdiChartFont)prototype).NativeFont;
			return new GdiChartFont(new Font(nativePrototype.FontFamily, nativePrototype.Size, style, nativePrototype.Unit));
		}

		public IChartFont DeriveFont(IChartFont prototype, float newSizeInPoints)
		{
			var nativePrototype = ((GdiChartFont)prototype).NativeFont;
			return new GdiChartFont(new Font(nativePrototype.FontFamily, newSizeInPoints, nativePrototype.Style, GraphicsUnit.Point));
		}

		public ITextFormat CreateTextFormat() => new GdiTextFormat(new StringFormat());

		public ITextFormat CreateTypographicTextFormat() => new GdiTextFormat(new StringFormat(StringFormat.GenericTypographic));

		public ITextFormat CreateDefaultTextFormat() => new GdiTextFormat(new StringFormat(StringFormat.GenericDefault));

		public IGraphicsPath CreatePath() => new GdiGraphicsPath();

		public IGraphicsPath CreatePath(PointF[] points, byte[] types) => new GdiGraphicsPath(points, types);

		public IClipRegion CreateRegion() => new GdiClipRegion();

		public IClipRegion CreateRegion(RectangleF rect) => new GdiClipRegion(rect);

		public IClipRegion CreateRegion(IGraphicsPath path) => new GdiClipRegion(path);

		public IImageDrawOptions CreateImageDrawOptions() => new GdiImageDrawOptions();
	}
}
