using System.Drawing;

namespace Microsoft.Reporting.Chart.WebForms.Rendering.Gdi
{
	/// <summary>Milestone A2 adapter — wraps <see cref="System.Drawing.Region"/> behind <see cref="IClipRegion"/>.</summary>
	internal sealed class GdiClipRegion : IClipRegion
	{
		internal Region NativeRegion { get; }

		internal GdiClipRegion()
		{
			NativeRegion = new Region();
		}

		internal GdiClipRegion(RectangleF rect)
		{
			NativeRegion = new Region(rect);
		}

		internal GdiClipRegion(IGraphicsPath path)
		{
			NativeRegion = new Region(((GdiGraphicsPath)path).NativePath);
		}

		/// <summary>Wraps an existing native <see cref="Region"/> (e.g. the current <c>Graphics.Clip</c>) rather than creating a new one.</summary>
		internal GdiClipRegion(Region existingRegion)
		{
			NativeRegion = existingRegion;
		}

		public void Intersect(RectangleF rect) => NativeRegion.Intersect(rect);

		public void Intersect(IGraphicsPath path) => NativeRegion.Intersect(((GdiGraphicsPath)path).NativePath);

		public void Union(RectangleF rect) => NativeRegion.Union(rect);

		public void Exclude(RectangleF rect) => NativeRegion.Exclude(rect);

		public void MakeEmpty() => NativeRegion.MakeEmpty();

		public void MakeInfinite() => NativeRegion.MakeInfinite();

		public bool IsVisible(PointF point) => NativeRegion.IsVisible(point);

		public RectangleF GetBounds(IRenderSurface surface) => NativeRegion.GetBounds(((GdiRenderSurface)surface).NativeGraphics);

		public void Dispose() => NativeRegion.Dispose();
	}
}
