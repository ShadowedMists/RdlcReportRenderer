using System.Drawing;
using System.Drawing.Drawing2D;

namespace Microsoft.Reporting.Chart.WebForms.Rendering
{
	/// <summary>
	/// Fill resource. Abstracts the <see cref="System.Drawing.Brush"/> hierarchy.
	/// Appendix A.5: <see cref="ISolidBrush"/> is ~90% of usage; the others are low-frequency.
	/// </summary>
	internal interface IBrush : IRenderingResource
	{
	}

	/// <summary>Solid single-colour fill. Abstracts <c>SolidBrush</c>.</summary>
	internal interface ISolidBrush : IBrush
	{
		Color Color { get; set; }
	}

	/// <summary>Two-colour / multi-stop gradient fill. Abstracts <c>LinearGradientBrush</c>.</summary>
	internal interface ILinearGradientBrush : IBrush
	{
		Blend Blend { get; set; }

		ColorBlend InterpolationColors { get; set; }

		WrapMode WrapMode { get; set; }

		/// <summary>The 2-element [start, end] colour pair (GDI+'s <c>LinearColors</c>) — read back by <c>GetSector3DBrush</c>.</summary>
		Color[] LinearColors { get; }
	}

	/// <summary>Image-tiled fill. Abstracts <c>TextureBrush</c>.</summary>
	internal interface ITextureBrush : IBrush
	{
		WrapMode WrapMode { get; set; }
	}

	/// <summary>Hatch-pattern fill. Abstracts <c>HatchBrush</c> (rare — 1 construction site).</summary>
	internal interface IHatchBrush : IBrush
	{
		HatchStyle HatchStyle { get; }

		Color ForegroundColor { get; }

		Color BackgroundColor { get; }
	}

	/// <summary>
	/// Path-bounded gradient fill (soft shadows/glows). Abstracts <c>PathGradientBrush</c> —
	/// missed by the original Appendix A.5 count (16 occ / 2 files, all in the shadow-rendering
	/// code paths of ChartGraphics/ChartGraphics3D), discovered during C4.
	/// </summary>
	internal interface IPathGradientBrush : IBrush
	{
		Color CenterColor { get; set; }

		Color[] SurroundColors { get; set; }

		PointF CenterPoint { get; set; }

		PointF FocusScales { get; set; }
	}
}
