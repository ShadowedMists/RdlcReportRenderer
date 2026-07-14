using System.Drawing;

namespace Microsoft.Reporting.Chart.WebForms.Rendering
{
	/// <summary>
	/// Options for drawing an image (color remap / transparency). Abstracts
	/// <see cref="System.Drawing.Imaging.ImageAttributes"/> (25 occ / 6 files).
	/// Coordinate with the existing <c>IImageProvider</c> background-image work
	/// (chart-image-abstraction-analysis.md) during implementation (task C8).
	/// </summary>
	internal interface IImageDrawOptions : IRenderingResource
	{
		/// <summary>Remap <paramref name="from"/> pixels to <paramref name="to"/> (used for transparent-colour keying).</summary>
		void SetColorRemap(Color from, Color to);

		/// <summary>Uniform alpha applied to the drawn image (0–1).</summary>
		void SetOpacity(float opacity);
	}
}
