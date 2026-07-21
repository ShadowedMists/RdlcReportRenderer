using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Microsoft.Reporting.Rendering;

namespace Microsoft.Reporting.Chart.WebForms.Rendering.Skia
{
	/// <summary>
	/// Spike stub behind <see cref="IImageDrawOptions"/> — the sample scene draws no images.
	/// Real support (color-key via <c>SKColorFilter</c>, opacity via <c>SKPaint.Color.Alpha</c>)
	/// is Milestone E1/C8 scope.
	/// </summary>
	internal sealed class SkiaImageDrawOptions : IImageDrawOptions
	{
		public void SetColorRemap(Color from, Color to) => throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public void SetTransparentColor(Color color) => throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public void SetWrapMode(WrapMode mode) => throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public void SetOpacity(float opacity) => throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public void SetChannelScale(float red, float green, float blue, float alpha) => throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public void Dispose()
		{
		}
	}
}
