using System;
using System.Drawing;

namespace Microsoft.Reporting.Chart.WebForms.Rendering.Skia
{
	/// <summary>
	/// Spike stub behind <see cref="IClipRegion"/>. The sample scene never clips, so this
	/// only needs to exist for <see cref="SkiaResourceFactory"/> to compile against
	/// <see cref="IDrawingResourceFactory"/> — real clip-region support (SkiaSharp's
	/// <c>SKRegion</c> or canvas clip stack) is Milestone E1 scope.
	/// </summary>
	internal sealed class SkiaClipRegion : IClipRegion
	{
		public void Intersect(RectangleF rect) => throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public void Intersect(IGraphicsPath path) => throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public void Union(RectangleF rect) => throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public void Exclude(RectangleF rect) => throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public void MakeEmpty() => throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public void MakeInfinite()
		{
			// Default/no-op state used by SkiaResourceFactory.CreateRegion(); an infinite
			// clip is equivalent to "not clipping", which is all the spike scene needs.
		}

		public bool IsVisible(PointF point) => true;

		public RectangleF GetBounds(IRenderSurface surface) => throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public void Dispose()
		{
		}
	}
}
