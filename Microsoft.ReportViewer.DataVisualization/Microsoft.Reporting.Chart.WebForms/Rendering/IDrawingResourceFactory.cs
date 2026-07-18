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

		// --- Brushes (A.5) ---
		ISolidBrush CreateSolidBrush(Color color);

		ILinearGradientBrush CreateLinearGradientBrush(RectangleF rect, Color startColor, Color endColor, float angle);

		ITextureBrush CreateTextureBrush(IChartImage image, WrapMode wrapMode);

		IHatchBrush CreateHatchBrush(HatchStyle style, Color foreColor, Color backColor);

		IPathGradientBrush CreatePathGradientBrush(IGraphicsPath path);

		// --- Images (image-loading prerequisite for C4's GetTextureBrush, found during B2) ---
		/// <summary>Decode an image from an already-open stream (file/URL/embedded-resource bytes). Caller owns disposing the stream.</summary>
		IChartImage LoadImage(Stream stream);

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
