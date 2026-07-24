using System.IO;
using Microsoft.Reporting.Rendering;
using SkiaSharp;

namespace Microsoft.Reporting.Chart.WebForms.Rendering.Skia
{
	/// <summary>
	/// Milestone E2 (2026-07-23) — <see cref="IRenderSurface"/> sibling of <see cref="Gdi.GdiRenderSurface"/>,
	/// wrapping a raster <see cref="SKSurface"/> instead of a GDI+ <see cref="System.Drawing.Bitmap"/>. Not
	/// wired into any production entry point yet (<c>ChartImage.GetImage</c>/<c>SaveImage</c> still hard-select
	/// <c>GdiRenderSurface</c> — see chart-gdi-type-abstraction.md's E2 notes); constructed directly by test
	/// code together with <see cref="RenderingType.Skia"/> to drive <c>ChartPicture.Paint(IRenderSurface,...)</c>.
	/// </summary>
	internal sealed class SkiaRenderSurface : IRenderSurface
	{
		internal SKSurface NativeSurface { get; }

		public int Width { get; }

		public int Height { get; }

		public float Dpi { get; }

		internal SkiaRenderSurface(int width, int height, float dpi)
		{
			Width = width;
			Height = height;
			Dpi = dpi;
			SKImageInfo info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
			NativeSurface = SKSurface.Create(info);
			NativeSurface.Canvas.Clear(SKColors.White);
		}

		public void Encode(Stream stream, ChartImageFormat format)
		{
			using SKImage image = NativeSurface.Snapshot();
			using SKData data = image.Encode(ToSkiaFormat(format), 100);
			byte[] bytes = data.ToArray();
			stream.Write(bytes, 0, bytes.Length);
		}

		private static SKEncodedImageFormat ToSkiaFormat(ChartImageFormat format) => format switch
		{
			ChartImageFormat.Jpeg => SKEncodedImageFormat.Jpeg,
			ChartImageFormat.Bmp => SKEncodedImageFormat.Bmp,
			_ => SKEncodedImageFormat.Png,
		};

		public void Dispose() => NativeSurface.Dispose();
	}

	/// <summary>Milestone E2 adapter — creates <see cref="SkiaRenderSurface"/> instances. No EMF/EMF+ equivalent exists.</summary>
	internal sealed class SkiaRenderSurfaceFactory : IRenderSurfaceFactory
	{
		public IRenderSurface CreateRasterSurface(int width, int height, float dpi) => new SkiaRenderSurface(width, height, dpi);

		public bool SupportsMetafile => false;
	}
}
