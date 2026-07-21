using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Microsoft.Reporting.Rendering;

namespace Microsoft.Reporting.Gauge.WebForms.Rendering
{
	/// <summary>
	/// Gauge-engine counterpart of <c>Microsoft.Reporting.Chart.WebForms.Rendering.IDrawingResourceFactory</c>
	/// (see tasks/gauge-gdi-type-abstraction.md Milestone A1). Constructs the shared, engine-agnostic
	/// resource interfaces from <see cref="Microsoft.Reporting.Rendering"/>. Deliberately narrower than
	/// the Chart engine's factory for now: no clip-region members yet — gauge clipping stays on
	/// <see cref="Region"/> until a gauge-specific clip-region abstraction is scoped (mirrors the Chart
	/// engine's own phased rollout). Image abstraction (<see cref="IChartImage"/>/<see cref="IImageDrawOptions"/>)
	/// was added during the GetTextureBrush prerequisite (Milestone B) via <see cref="WrapImage"/> — see
	/// <see cref="GaugeGraphics.GetTextureBrushResource"/>.
	/// </summary>
	internal interface IGaugeDrawingResourceFactory
	{
		IPen CreatePen(Color color, float width);

		IPen CreatePen(IBrush brush, float width);

		ISolidBrush CreateSolidBrush(Color color);

		ILinearGradientBrush CreateLinearGradientBrush(RectangleF rect, Color startColor, Color endColor, float angle);

		ITextureBrush CreateTextureBrush(IChartImage image, WrapMode wrapMode);

		/// <summary>
		/// Construct a texture brush drawn into <paramref name="rect"/> with colour-key/wrap-mode
		/// <paramref name="options"/> applied — GDI+'s <c>TextureBrush(Image, RectangleF, ImageAttributes)</c>
		/// constructor, needed by <see cref="GaugeGraphics.GetTextureBrushResource"/>.
		/// </summary>
		ITextureBrush CreateTextureBrush(IChartImage image, RectangleF rect, IImageDrawOptions options);

		IHatchBrush CreateHatchBrush(HatchStyle style, Color foreColor, Color backColor);

		IPathGradientBrush CreatePathGradientBrush(IGraphicsPath path);

		IChartFont CreateFont(string familyName, float sizeInPoints);

		IChartFont CreateFont(string familyName, float size, FontStyle style);

		IChartFont CreateFont(string familyName, float size, FontStyle style, GraphicsUnit unit);

		/// <summary>Wrap an already-constructed native <see cref="Font"/>, without reconstructing it from decomposed properties (see the Chart engine's identically-named method for why this matters).</summary>
		IChartFont WrapFont(Font font);

		ITextFormat CreateTextFormat();

		ITextFormat CreateTypographicTextFormat();

		IGraphicsPath CreatePath();

		IGraphicsPath CreatePath(PointF[] points, byte[] types);

		/// <summary>
		/// Wrap an already-loaded native image as an <see cref="IChartImage"/> — a bridge for
		/// <c>common.ImageLoader</c>'s legacy loading pipeline, which remains GDI+-only/concrete
		/// (found during the GetTextureBrush prerequisite; see tasks/gauge-gdi-type-abstraction.md
		/// Milestone B). Not available on backends that can't construct <see cref="Image"/> at all.
		/// </summary>
		IChartImage WrapImage(Image image);

		IImageDrawOptions CreateImageDrawOptions();
	}
}
