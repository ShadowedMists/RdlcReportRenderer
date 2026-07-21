using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using SkiaSharp;
using Microsoft.Reporting.Rendering;

namespace Microsoft.Reporting.Chart.WebForms.Rendering.Skia
{
	/// <summary>Spike adapter — wraps a fill-styled <see cref="SKPaint"/> behind <see cref="ISolidBrush"/>.</summary>
	internal sealed class SkiaSolidBrush : ISolidBrush
	{
		internal SKPaint NativePaint { get; }

		internal SkiaSolidBrush(Color color)
		{
			NativePaint = new SKPaint
			{
				Style = SKPaintStyle.Fill,
				Color = SkiaConvert.ToSKColor(color),
				IsAntialias = true,
			};
		}

		public Color Color
		{
			get => SkiaConvert.ToColor(NativePaint.Color);
			set => NativePaint.Color = SkiaConvert.ToSKColor(value);
		}

		public void Dispose() => NativePaint.Dispose();
	}

	// Spike scope: gradient/texture/hatch fills are low-frequency (chart-gdi-type-abstraction.md
	// §A.5 — ~10% of brush usage combined) and not exercised by the sample scene. SkiaSharp has
	// direct equivalents (SKShader.CreateLinearGradient, CreateBitmap-backed shaders, no native
	// hatch primitive) but translating them is real Milestone E1 work, not spike scope.

	internal sealed class SkiaLinearGradientBrush : ILinearGradientBrush
	{
		public Blend Blend { get; set; }
		public ColorBlend InterpolationColors { get; set; }
		public WrapMode WrapMode { get; set; }
		public Color[] LinearColors { get; set; } = Array.Empty<Color>();
		public void Dispose() { }
	}

	internal sealed class SkiaTextureBrush : ITextureBrush
	{
		public WrapMode WrapMode { get; set; }
		public void Dispose() { }
	}

	internal sealed class SkiaHatchBrush : IHatchBrush
	{
		public HatchStyle HatchStyle { get; }
		public Color ForegroundColor { get; }
		public Color BackgroundColor { get; }
		public void Dispose() { }
	}
}
