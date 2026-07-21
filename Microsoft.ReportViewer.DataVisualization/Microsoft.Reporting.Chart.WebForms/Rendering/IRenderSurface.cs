using System;
using System.IO;
using Microsoft.Reporting.Rendering;

namespace Microsoft.Reporting.Chart.WebForms.Rendering
{
	/// <summary>
	/// A decoded image usable as a texture/draw source. Abstracts <c>System.Drawing.Image</c>
	/// within the chart engine. Overlaps the existing <c>IImageProvider</c> work; the two
	/// are reconciled in task C8.
	/// </summary>
	internal interface IChartImage : IRenderingResource
	{
		int Width { get; }

		int Height { get; }
	}

	/// <summary>
	/// The raster draw target that replaces <c>new Bitmap(w, h)</c> + <c>Graphics.FromImage(...)</c>
	/// (ChartPicture.cs:733-734) and the final <c>image.Save(stream, format)</c> (Chart.cs:1313).
	/// On Skia this is an <c>SKSurface</c>; on GDI+ a <c>Bitmap</c>. Engine wiring lands in
	/// Milestone D1 — kept minimal here (A1 is interfaces only).
	/// </summary>
	internal interface IRenderSurface : IDisposable
	{
		int Width { get; }

		int Height { get; }

		float Dpi { get; }

		/// <summary>Encode the rendered surface to <paramref name="stream"/>.</summary>
		void Encode(Stream stream, ChartImageFormat format);
	}

	/// <summary>
	/// Creates the platform draw surface. Selected per-platform by a factory
	/// (Milestone D2), mirroring the Excel <c>ImageProviderFactory</c> pattern.
	/// </summary>
	internal interface IRenderSurfaceFactory
	{
		IRenderSurface CreateRasterSurface(int width, int height, float dpi);

		/// <summary>
		/// EMF/EMF+ is a Windows-only format (no cross-platform equivalent). Callers must
		/// check this before requesting a metafile; non-Windows backends return <c>false</c>.
		/// </summary>
		bool SupportsMetafile { get; }
	}
}
