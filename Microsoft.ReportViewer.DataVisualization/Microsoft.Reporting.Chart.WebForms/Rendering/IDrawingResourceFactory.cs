using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace Microsoft.Reporting.Chart.WebForms.Rendering
{
	/// <summary>
	/// The central PORT. Every backend-specific drawing resource is created here
	/// instead of via <c>new Pen(...)</c> / <c>new SolidBrush(...)</c> etc., so the
	/// chart engine never constructs a concrete GDI+ object directly.
	/// Adapters: <c>GdiResourceFactory</c> (wraps System.Drawing), <c>SkiaResourceFactory</c>.
	/// Factory method surface derived from Appendix A.
	/// </summary>
	internal interface IDrawingResourceFactory
	{
		// --- Pens (A.4) ---
		IPen CreatePen(Color color, float width);

		/// <summary>
		/// Construct a pen that strokes with an arbitrary brush (solid/hatch/texture/gradient) —
		/// GDI+'s <c>Pen(Brush, float)</c> constructor, needed by <c>RangeChart.DrawLine</c>'s
		/// brush-backed hairline strokes (found during B2).
		/// </summary>
		IPen CreatePen(IBrush brush, float width);

		// --- Brushes (A.5) ---
		ISolidBrush CreateSolidBrush(Color color);

		ILinearGradientBrush CreateLinearGradientBrush(RectangleF rect, Color startColor, Color endColor, float angle);

		ITextureBrush CreateTextureBrush(IChartImage image, WrapMode wrapMode);

		/// <summary>
		/// Construct a texture brush drawn into <paramref name="rect"/> with colour-key/wrap-mode
		/// <paramref name="options"/> applied — GDI+'s <c>TextureBrush(Image, RectangleF, ImageAttributes)</c>
		/// constructor, needed by <c>ChartGraphics.GetTextureBrush</c> (found during B2).
		/// </summary>
		ITextureBrush CreateTextureBrush(IChartImage image, RectangleF rect, IImageDrawOptions options);

		IHatchBrush CreateHatchBrush(HatchStyle style, Color foreColor, Color backColor);

		IPathGradientBrush CreatePathGradientBrush(IGraphicsPath path);

		// --- Images (image-loading prerequisite for C4's GetTextureBrush, found during B2) ---
		/// <summary>Decode an image from an already-open stream (file/URL/embedded-resource bytes). Caller owns disposing the stream.</summary>
		IChartImage LoadImage(Stream stream);

		/// <summary>
		/// Wrap an already-loaded native image as an <see cref="IChartImage"/> — a bridge for
		/// <c>ImageLoader</c>'s legacy loading pipeline (<c>chart.Images</c>/<c>ResourceManager</c>/
		/// <c>WebRequest</c>/<c>File</c>), which remains GDI+-only/concrete for now (found during B2;
		/// see chart-gdi-type-abstraction.md). Not available on backends that can't construct
		/// <see cref="Image"/> at all (e.g. Skia on Linux).
		/// </summary>
		IChartImage WrapImage(Image image);

		// --- Fonts (A.2) ---
		IChartFont CreateFont(string familyName, float sizeInPoints);

		IChartFont CreateFont(string familyName, float size, FontStyle style);

		IChartFont CreateFont(string familyName, float size, FontStyle style, GraphicsUnit unit);

		IChartFont DeriveFont(IChartFont prototype, FontStyle style);

		IChartFont DeriveFont(IChartFont prototype, float newSizeInPoints);

		// --- Text formats (A.3) ---
		ITextFormat CreateTextFormat();

		ITextFormat CreateTypographicTextFormat();

		ITextFormat CreateDefaultTextFormat();

		// --- Paths (A.1) ---
		IGraphicsPath CreatePath();

		IGraphicsPath CreatePath(PointF[] points, byte[] types);

		// --- Regions ---
		IClipRegion CreateRegion();

		IClipRegion CreateRegion(RectangleF rect);

		IClipRegion CreateRegion(IGraphicsPath path);

		// --- Image draw options (C8) ---
		IImageDrawOptions CreateImageDrawOptions();
	}
}
