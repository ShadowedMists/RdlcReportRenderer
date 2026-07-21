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
	/// the Chart engine's factory for now: no image-abstraction (<c>IChartImage</c>) or clip-region
	/// members yet — <see cref="GaugeGraphics.GetTextureBrush"/> still loads a concrete
	/// <see cref="System.Drawing.Image"/> directly, and gauge clipping stays on <see cref="Region"/>
	/// until a gauge-specific clip-region abstraction is scoped (mirrors the Chart engine's own phased
	/// rollout — those were later additions there too, not part of its first A1/A2 pass).
	/// </summary>
	internal interface IGaugeDrawingResourceFactory
	{
		IPen CreatePen(Color color, float width);

		IPen CreatePen(IBrush brush, float width);

		ISolidBrush CreateSolidBrush(Color color);

		ILinearGradientBrush CreateLinearGradientBrush(RectangleF rect, Color startColor, Color endColor, float angle);

		ITextureBrush CreateTextureBrush(Image image, WrapMode wrapMode);

		ITextureBrush CreateTextureBrush(Image image, RectangleF rect, ImageAttributes attributes);

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
	}
}
