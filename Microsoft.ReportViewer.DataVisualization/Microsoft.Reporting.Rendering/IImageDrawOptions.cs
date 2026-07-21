using System.Drawing;
using System.Drawing.Drawing2D;

namespace Microsoft.Reporting.Rendering
{
	/// <summary>
	/// Options for drawing an image (color remap / transparency). Abstracts
	/// <see cref="System.Drawing.Imaging.ImageAttributes"/>. Relocated from
	/// <c>Microsoft.Reporting.Chart.WebForms.Rendering</c> to this shared namespace (verified
	/// portable — no Chart-specific dependencies — during the Gauge engine's GetTextureBrush
	/// prerequisite, see tasks/gauge-gdi-type-abstraction.md Milestone B).
	/// </summary>
	internal interface IImageDrawOptions : IRenderingResource
	{
		/// <summary>Remap <paramref name="from"/> pixels to <paramref name="to"/> (used for transparent-colour keying).</summary>
		void SetColorRemap(Color from, Color to);

		/// <summary>
		/// Mark <paramref name="color"/> transparent when the image is drawn — GDI+'s
		/// <c>ImageAttributes.SetColorKey(color, color, ColorAdjustType.Default)</c> (a colour-range
		/// key), which is distinct from <see cref="SetColorRemap"/>'s straight 1:1 substitution
		/// (<c>SetRemapTable</c>) and is what the chart engine's background/marker/texture image
		/// code actually relies on for transparent-colour images.
		/// </summary>
		void SetTransparentColor(Color color);

		/// <summary>How out-of-bounds sampling wraps when this image backs a tiled texture brush.</summary>
		void SetWrapMode(WrapMode mode);

		/// <summary>Uniform alpha applied to the drawn image (0–1).</summary>
		void SetOpacity(float opacity);
	}
}
