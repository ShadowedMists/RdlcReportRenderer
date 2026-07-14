using System.Drawing;

namespace Microsoft.Reporting.Chart.WebForms.Rendering
{
	/// <summary>
	/// Clip region. Abstracts <see cref="System.Drawing.Region"/> (17 occ / 6 files).
	/// Combine operations mirror GDI+ so the adapter is a passthrough; the engine
	/// uses regions for clipping and <c>FillRegion</c>.
	/// </summary>
	internal interface IClipRegion : IRenderingResource
	{
		void Intersect(RectangleF rect);

		void Intersect(IGraphicsPath path);

		void Union(RectangleF rect);

		void Exclude(RectangleF rect);

		void MakeEmpty();

		void MakeInfinite();

		bool IsVisible(PointF point);

		RectangleF GetBounds(IRenderSurface surface);
	}
}
