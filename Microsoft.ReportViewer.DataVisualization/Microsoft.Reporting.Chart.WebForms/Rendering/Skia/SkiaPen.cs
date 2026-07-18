using System.Drawing;
using System.Drawing.Drawing2D;
using SkiaSharp;

namespace Microsoft.Reporting.Chart.WebForms.Rendering.Skia
{
	/// <summary>
	/// Spike adapter (tasks/chart-cross-platform-implementation.md Phase 0) — wraps an
	/// <see cref="SKPaint"/> configured for stroking behind <see cref="IPen"/>. Unlike
	/// <c>GdiPen</c>, this never touches <see cref="System.Drawing"/>, so it can be
	/// constructed on Linux where GDI+ initialization itself throws (see spike report).
	/// </summary>
	internal sealed class SkiaPen : IPen
	{
		internal SKPaint NativePaint { get; }

		internal SkiaPen(Color color, float width)
		{
			NativePaint = new SKPaint
			{
				Style = SKPaintStyle.Stroke,
				Color = SkiaConvert.ToSKColor(color),
				StrokeWidth = width,
				IsAntialias = true,
			};
		}

		public Color Color
		{
			get => SkiaConvert.ToColor(NativePaint.Color);
			set => NativePaint.Color = SkiaConvert.ToSKColor(value);
		}

		public float Width
		{
			get => NativePaint.StrokeWidth;
			set => NativePaint.StrokeWidth = value;
		}

		// Spike scope: the sample scene only needs solid, round-joined strokes.
		// Dash/cap/join translation is straightforward (SKPathEffect.CreateDash,
		// SKStrokeCap, SKStrokeJoin) but left for the real Milestone E1 adapter.
		public DashStyle DashStyle { get; set; }

		public LineCap StartCap { get; set; }

		public LineCap EndCap { get; set; }

		public LineJoin LineJoin { get; set; }

		public PenAlignment Alignment { get; set; }

		public void Dispose() => NativePaint.Dispose();
	}
}
