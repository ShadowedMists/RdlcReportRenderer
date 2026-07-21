using System;
using System.Drawing;
using System.Numerics;
using Microsoft.Reporting.Rendering;

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

		public void Complement(IGraphicsPath path) => throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public void Xor(RectangleF rect) => throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public void MakeEmpty() => throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public void MakeInfinite()
		{
			// Default/no-op state used by SkiaResourceFactory.CreateRegion(); an infinite
			// clip is equivalent to "not clipping", which is all the spike scene needs.
		}

		public bool IsVisible(PointF point) => true;

		public void Transform(Matrix3x2 matrix) => throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public void Translate(float dx, float dy) => throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public IClipRegion Clone() => throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public RectangleF GetBounds(IChartRenderingEngine engine) => throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public bool IsEmpty(IChartRenderingEngine engine) => throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public bool IsInfinite(IChartRenderingEngine engine) => throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public void Dispose()
		{
		}
	}
}
