using System.Drawing;
using System.Drawing.Drawing2D;

namespace Microsoft.Reporting.Chart.WebForms.Rendering.Gdi
{
	/// <summary>Milestone A2 adapter — wraps <see cref="System.Drawing.SolidBrush"/> behind <see cref="ISolidBrush"/>.</summary>
	internal sealed class GdiSolidBrush : ISolidBrush
	{
		internal SolidBrush NativeBrush { get; }

		internal GdiSolidBrush(Color color)
		{
			NativeBrush = new SolidBrush(color);
		}

		public Color Color
		{
			get => NativeBrush.Color;
			set => NativeBrush.Color = value;
		}

		public void Dispose() => NativeBrush.Dispose();
	}

	/// <summary>Milestone A2 adapter — wraps <see cref="System.Drawing.Drawing2D.LinearGradientBrush"/> behind <see cref="ILinearGradientBrush"/>.</summary>
	internal sealed class GdiLinearGradientBrush : ILinearGradientBrush
	{
		internal LinearGradientBrush NativeBrush { get; }

		internal GdiLinearGradientBrush(RectangleF rect, Color startColor, Color endColor, float angle)
		{
			NativeBrush = new LinearGradientBrush(rect, startColor, endColor, angle);
		}

		public Blend Blend
		{
			get => NativeBrush.Blend;
			set => NativeBrush.Blend = value;
		}

		public ColorBlend InterpolationColors
		{
			get => NativeBrush.InterpolationColors;
			set => NativeBrush.InterpolationColors = value;
		}

		public WrapMode WrapMode
		{
			get => NativeBrush.WrapMode;
			set => NativeBrush.WrapMode = value;
		}

		public void Dispose() => NativeBrush.Dispose();
	}

	/// <summary>Milestone A2 adapter — wraps <see cref="System.Drawing.TextureBrush"/> behind <see cref="ITextureBrush"/>.</summary>
	internal sealed class GdiTextureBrush : ITextureBrush
	{
		internal TextureBrush NativeBrush { get; }

		internal GdiTextureBrush(Image image, WrapMode wrapMode)
		{
			NativeBrush = new TextureBrush(image, wrapMode);
		}

		public WrapMode WrapMode
		{
			get => NativeBrush.WrapMode;
			set => NativeBrush.WrapMode = value;
		}

		public void Dispose() => NativeBrush.Dispose();
	}

	/// <summary>Milestone A2 adapter — wraps <see cref="System.Drawing.Drawing2D.HatchBrush"/> behind <see cref="IHatchBrush"/>.</summary>
	internal sealed class GdiHatchBrush : IHatchBrush
	{
		internal HatchBrush NativeBrush { get; }

		internal GdiHatchBrush(HatchStyle style, Color foreColor, Color backColor)
		{
			NativeBrush = new HatchBrush(style, foreColor, backColor);
		}

		public HatchStyle HatchStyle => NativeBrush.HatchStyle;

		public Color ForegroundColor => NativeBrush.ForegroundColor;

		public Color BackgroundColor => NativeBrush.BackgroundColor;

		public void Dispose() => NativeBrush.Dispose();
	}

	/// <summary>Milestone C4 adapter — wraps <see cref="System.Drawing.Drawing2D.PathGradientBrush"/> behind <see cref="IPathGradientBrush"/>.</summary>
	internal sealed class GdiPathGradientBrush : IPathGradientBrush
	{
		internal PathGradientBrush NativeBrush { get; }

		internal GdiPathGradientBrush(IGraphicsPath path)
		{
			NativeBrush = new PathGradientBrush(((GdiGraphicsPath)path).NativePath);
		}

		public Color CenterColor
		{
			get => NativeBrush.CenterColor;
			set => NativeBrush.CenterColor = value;
		}

		public Color[] SurroundColors
		{
			get => NativeBrush.SurroundColors;
			set => NativeBrush.SurroundColors = value;
		}

		public PointF CenterPoint
		{
			get => NativeBrush.CenterPoint;
			set => NativeBrush.CenterPoint = value;
		}

		public PointF FocusScales
		{
			get => new PointF(NativeBrush.FocusScales.X, NativeBrush.FocusScales.Y);
			set => NativeBrush.FocusScales = value;
		}

		public void Dispose() => NativeBrush.Dispose();
	}
}
