using System.Drawing;
using System.Numerics;

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

		void Xor(RectangleF rect);

		void MakeEmpty();

		void MakeInfinite();

		bool IsVisible(PointF point);

		void Transform(Matrix3x2 matrix);

		/// <summary>
		/// Bounds/emptiness queries need the live drawing context (GDI+'s <c>Region.GetBounds</c>/
		/// <c>IsEmpty</c> require a <see cref="System.Drawing.Graphics"/>). <see cref="IRenderSurface"/>
		/// models an owned, encodable output surface (see Milestone D) — not the mid-paint context
		/// ChartGraphics actually holds — so these take the engine itself instead.
		/// </summary>
		RectangleF GetBounds(IChartRenderingEngine engine);

		bool IsEmpty(IChartRenderingEngine engine);
	}
}
