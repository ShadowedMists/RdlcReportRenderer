using Microsoft.ReportingServices.Rendering.ExcelRenderer.Excel;
using System;
using System.Drawing;
using System.IO;

namespace Microsoft.ReportingServices.Rendering.ExcelRenderer.Windows
{
	/// <summary>
	/// Windows-specific image provider using System.Drawing.
	/// Used for chart rendering which depends on System.Drawing.Image.
	/// </summary>
	[System.Runtime.Versioning.SupportedOSPlatform("windows")]
	internal class WindowsImageProvider : IImageProvider
	{
		/// <summary>
		/// Load an image from a stream and get its dimensions.
		/// </summary>
		public ImageMetadata LoadImage(Stream imageStream)
		{
			if (imageStream == null || imageStream.Length == 0)
				return null;

			imageStream.Position = 0;
			Image gdiImage = null;
			try
			{
				gdiImage = Image.FromStream(imageStream);

				var metadata = new ImageMetadata
				{
					Width = gdiImage.Width,
					Height = gdiImage.Height,
					HorizontalResolution = gdiImage.HorizontalResolution,
					VerticalResolution = gdiImage.VerticalResolution,
					Format = DetermineFormat(gdiImage.RawFormat)
				};

				return metadata;
			}
			finally
			{
				gdiImage?.Dispose();
			}
		}

		/// <summary>
		/// Get an image object suitable for the rendering backend.
		/// For chart rendering, this returns System.Drawing.Image.
		/// </summary>
		public object GetImageForChart(Stream imageStream)
		{
			if (imageStream == null || imageStream.Length == 0)
				return null;

			imageStream.Position = 0;
			return Image.FromStream(imageStream);
		}

		private static ImageFormatType DetermineFormat(System.Drawing.Imaging.ImageFormat rawFormat)
		{
			if (rawFormat == null)
				return ImageFormatType.Unknown;

			if (rawFormat.Guid == System.Drawing.Imaging.ImageFormat.Bmp.Guid)
				return ImageFormatType.Bmp;
			if (rawFormat.Guid == System.Drawing.Imaging.ImageFormat.Gif.Guid)
				return ImageFormatType.Gif;
			if (rawFormat.Guid == System.Drawing.Imaging.ImageFormat.Jpeg.Guid)
				return ImageFormatType.Jpeg;
			if (rawFormat.Guid == System.Drawing.Imaging.ImageFormat.Png.Guid)
				return ImageFormatType.Png;

			return ImageFormatType.Unknown;
		}
	}
}
