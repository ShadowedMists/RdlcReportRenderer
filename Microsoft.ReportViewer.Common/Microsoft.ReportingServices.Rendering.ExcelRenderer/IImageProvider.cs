using System.IO;

namespace Microsoft.ReportingServices.Rendering.ExcelRenderer
{
	/// <summary>
	/// Platform-agnostic interface for image operations.
	/// Abstracts away platform-specific image libraries (System.Drawing vs alternatives).
	/// </summary>
	internal interface IImageProvider
	{
		/// <summary>
		/// Load an image from a stream and get its dimensions.
		/// </summary>
		ImageMetadata LoadImage(Stream imageStream);

		/// <summary>
		/// Get an image object suitable for the rendering backend.
		/// For chart rendering, this returns System.Drawing.Image (Windows).
		/// For future cross-platform charts, would return platform-appropriate type.
		/// </summary>
		object GetImageForChart(Stream imageStream);
	}
}
