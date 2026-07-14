using System.Drawing;
using System.Drawing.Drawing2D;

namespace Microsoft.Reporting.Chart.WebForms.Rendering
{
	/// <summary>
	/// Stroke resource. Abstracts <see cref="System.Drawing.Pen"/>.
	/// Surface measured in the engine (Appendix A.4): created as <c>(color, width)</c>;
	/// dash/cap/join set post-construction. Members kept mutable to mirror GDI+ usage
	/// and minimise caller migration.
	/// </summary>
	internal interface IPen : IRenderingResource
	{
		Color Color { get; set; }

		float Width { get; set; }

		DashStyle DashStyle { get; set; }

		LineCap StartCap { get; set; }

		LineCap EndCap { get; set; }

		LineJoin LineJoin { get; set; }
	}
}
