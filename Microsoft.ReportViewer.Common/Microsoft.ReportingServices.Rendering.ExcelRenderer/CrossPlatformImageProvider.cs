using Microsoft.ReportingServices.Rendering.ExcelRenderer.Excel;
using SixLabors.ImageSharp;
using System.IO;

namespace Microsoft.ReportingServices.Rendering.ExcelRenderer
{
	/// <summary>
	/// Cross-platform image provider using SixLabors.ImageSharp.
	/// Used for non-chart image operations (Excel embedded images, web rendering).
	/// Returns null for chart operations (not supported on non-Windows platforms).
	/// </summary>
	internal class CrossPlatformImageProvider : IImageProvider
	{
		/// <summary>
		/// Load an image from a stream and get its dimensions.
		/// </summary>
		public ImageMetadata LoadImage(Stream imageStream)
		{
			if (imageStream == null || imageStream.Length == 0)
				return null;

			try
			{
				imageStream.Position = 0;
				var imageInfo = Image.Identify(imageStream);

				if (imageInfo == null)
					return null;

				imageStream.Position = 0;

				var metadata = new ImageMetadata
				{
					Width = imageInfo.Width,
					Height = imageInfo.Height,
					HorizontalResolution = (float)imageInfo.Metadata.HorizontalResolution,
					VerticalResolution = (float)imageInfo.Metadata.VerticalResolution,
					Format = DetermineFormat(imageInfo.Metadata.DecodedImageFormat)
				};

				return metadata;
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// Chart rendering is Windows-only in current architecture.
		/// Return null on non-Windows platforms.
		/// Future: Alternative chart libraries may provide cross-platform support.
		/// </summary>
		public object GetImageForChart(Stream imageStream)
		{
			return null;
		}

		private static ImageFormatType DetermineFormat(SixLabors.ImageSharp.Formats.IImageFormat format)
		{
			if (format == null)
				return ImageFormatType.Unknown;

			string formatName = format.Name.ToLowerInvariant();
			return formatName switch
			{
				"bmp" => ImageFormatType.Bmp,
				"gif" => ImageFormatType.Gif,
				"jpeg" => ImageFormatType.Jpeg,
				"png" => ImageFormatType.Png,
				_ => ImageFormatType.Unknown
			};
		}
	}
}
