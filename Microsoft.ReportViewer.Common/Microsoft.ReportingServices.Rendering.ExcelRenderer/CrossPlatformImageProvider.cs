using Microsoft.ReportingServices.Rendering.ExcelRenderer.Excel;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using System.Runtime.InteropServices;

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

		/// <summary>
		/// Decode into a tightly-packed top-down BGRA32 buffer via ImageSharp.
		/// SixLabors.ImageSharp.PixelFormats.Bgra32 has the same in-memory byte
		/// order (B,G,R,A) as System.Drawing's Format32bppArgb, so the pixel
		/// buffer can be copied out directly with no channel reordering.
		/// </summary>
		public byte[] DecodeToBgra32(Stream imageStream, int width, int height)
		{
			imageStream.Position = 0;
			using (Image<Bgra32> image = Image.Load<Bgra32>(imageStream))
			{
				byte[] buffer = new byte[width * height * 4];
				int rowBytes = width * 4;
				image.ProcessPixelRows(accessor =>
				{
					for (int row = 0; row < height && row < accessor.Height; row++)
					{
						Span<Bgra32> pixelRow = accessor.GetRowSpan(row);
						Span<byte> rowBytesSpan = MemoryMarshal.AsBytes(pixelRow);
						int copyBytes = Math.Min(rowBytes, rowBytesSpan.Length);
						rowBytesSpan.Slice(0, copyBytes).CopyTo(buffer.AsSpan(row * rowBytes, copyBytes));
					}
				});
				return buffer;
			}
		}

		/// <summary>
		/// Decode an arbitrary image and re-encode it as PNG via SixLabors.ImageSharp.
		/// </summary>
		public byte[] EncodeToPng(Stream imageStream)
		{
			imageStream.Position = 0;
			using (Image image = Image.Load(imageStream))
			using (MemoryStream memoryStream = new MemoryStream())
			{
				image.SaveAsPng(memoryStream);
				return memoryStream.ToArray();
			}
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
