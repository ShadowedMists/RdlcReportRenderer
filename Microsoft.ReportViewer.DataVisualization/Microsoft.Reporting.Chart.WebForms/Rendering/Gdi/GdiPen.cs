using System.Drawing;
using System.Drawing.Drawing2D;

namespace Microsoft.Reporting.Chart.WebForms.Rendering.Gdi
{
	/// <summary>Milestone A2 adapter — wraps <see cref="System.Drawing.Pen"/> behind <see cref="IPen"/>.</summary>
	internal sealed class GdiPen : IPen
	{
		internal Pen NativePen { get; }

		internal GdiPen(Color color, float width)
		{
			NativePen = new Pen(color, width);
		}

		public Color Color
		{
			get => NativePen.Color;
			set => NativePen.Color = value;
		}

		public float Width
		{
			get => NativePen.Width;
			set => NativePen.Width = value;
		}

		public DashStyle DashStyle
		{
			get => NativePen.DashStyle;
			set => NativePen.DashStyle = value;
		}

		public LineCap StartCap
		{
			get => NativePen.StartCap;
			set => NativePen.StartCap = value;
		}

		public LineCap EndCap
		{
			get => NativePen.EndCap;
			set => NativePen.EndCap = value;
		}

		public LineJoin LineJoin
		{
			get => NativePen.LineJoin;
			set => NativePen.LineJoin = value;
		}

		public void Dispose() => NativePen.Dispose();
	}
}
