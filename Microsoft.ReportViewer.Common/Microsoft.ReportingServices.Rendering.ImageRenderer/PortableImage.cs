using Microsoft.ReportingServices.Rendering.ExcelRenderer;
using System;
using System.Drawing;
using System.IO;

namespace Microsoft.ReportingServices.Rendering.ImageRenderer
{
	/// <summary>
	/// Replaces System.Drawing.Image as ImageWriter's in-memory decoded-image
	/// representation, since Image.FromStream cannot run at all on non-Windows
	/// platforms (GDI+ cannot construct any System.Drawing object on Linux - see
	/// docs/platform-support.md's Phase 0 spike finding). On Windows this wraps a
	/// real GDI+ Image (unchanged behavior). On non-Windows it decodes via the
	/// IImageProvider abstraction already built for Excel's cross-platform image
	/// handling (docs/rendering-abstractions.md), giving Width/Height/resolution
	/// without needing GDI+ at all. Actually drawing this onto a page surface still
	/// requires a non-Windows raster surface, which does not exist yet - see
	/// tasks/image-renderer-cross-platform.md's Phase 2. This type only covers
	/// decode/metadata (Phase 1); IsGdiBacked is false on non-Windows and callers
	/// must skip the actual draw call until Phase 2 lands.
	/// </summary>
	internal sealed class PortableImage : IDisposable
	{
		private readonly Image m_gdiImage;

		private int m_width;

		private int m_height;

		private float m_horizontalResolution;

		private float m_verticalResolution;

		internal Image GdiImage => m_gdiImage;

		internal bool IsGdiBacked => m_gdiImage != null;

		internal int Width => m_width;

		internal int Height => m_height;

		internal float HorizontalResolution => m_horizontalResolution;

		internal float VerticalResolution => m_verticalResolution;

		private PortableImage(Image gdiImage)
		{
			m_gdiImage = gdiImage;
			m_width = gdiImage.Width;
			m_height = gdiImage.Height;
			m_horizontalResolution = gdiImage.HorizontalResolution;
			m_verticalResolution = gdiImage.VerticalResolution;
		}

		private PortableImage(int width, int height, float horizontalResolution, float verticalResolution)
		{
			m_width = width;
			m_height = height;
			m_horizontalResolution = horizontalResolution;
			m_verticalResolution = verticalResolution;
		}

		internal static PortableImage FromStream(Stream stream)
		{
			if (OperatingSystem.IsWindows())
			{
				return new PortableImage(Image.FromStream(stream));
			}
			ImageMetadata metadata = ImageProviderFactory.CreateProvider().LoadImage(stream)
				?? throw new InvalidDataException("Unable to decode image stream.");
			return new PortableImage(metadata.Width, metadata.Height, metadata.HorizontalResolution, metadata.VerticalResolution);
		}

		internal static PortableImage FromGdiImage(Image gdiImage)
		{
			return new PortableImage(gdiImage);
		}

		/// <summary>
		/// Mirrors the GDI+ Bitmap.SetResolution call ImageWriter.SetResolution used to
		/// make directly - only meaningfully affects drawing on the (Windows-only, for
		/// now) GDI-backed path; on non-Windows this just updates the metadata this
		/// object reports, since there is no raster surface yet to draw onto.
		/// </summary>
		internal void SetResolution(float horizontalResolution, float verticalResolution)
		{
			if (m_gdiImage is Bitmap bitmap)
			{
				bitmap.SetResolution(horizontalResolution, verticalResolution);
			}
			m_horizontalResolution = horizontalResolution;
			m_verticalResolution = verticalResolution;
		}

		public void Dispose()
		{
			m_gdiImage?.Dispose();
		}
	}
}
