using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Numerics;
using Microsoft.Reporting.Chart.WebForms.Rendering;
using Microsoft.Reporting.Rendering;

namespace Microsoft.Reporting.Chart.WebForms
{
	internal interface IChartRenderingEngine
	{
		Matrix Transform
		{
			get;
			set;
		}

		SmoothingMode SmoothingMode
		{
			get;
			set;
		}

		TextRenderingHint TextRenderingHint
		{
			get;
			set;
		}

		Region Clip
		{
			get;
			set;
		}

		Graphics Graphics
		{
			get;
			set;
		}

		bool IsClipEmpty
		{
			get;
		}

		void DrawLine(Pen pen, PointF pt1, PointF pt2);

		void DrawLine(Pen pen, float x1, float y1, float x2, float y2);

		void DrawImage(Image image, Rectangle destRect, int srcX, int srcY, int srcWidth, int srcHeight, GraphicsUnit srcUnit, ImageAttributes imageAttr);

		void DrawEllipse(Pen pen, float x, float y, float width, float height);

		void DrawCurve(Pen pen, PointF[] points, int offset, int numberOfSegments, float tension);

		void DrawRectangle(Pen pen, int x, int y, int width, int height);

		void DrawPolygon(Pen pen, PointF[] points);

		void DrawString(string s, Font font, Brush brush, RectangleF layoutRectangle, StringFormat format);

		void DrawString(string s, Font font, Brush brush, PointF point, StringFormat format);

		void DrawImage(Image image, Rectangle destRect, float srcX, float srcY, float srcWidth, float srcHeight, GraphicsUnit srcUnit, ImageAttributes imageAttrs);

		void DrawRectangle(Pen pen, float x, float y, float width, float height);

		void DrawPath(Pen pen, GraphicsPath path);

		void DrawPie(Pen pen, float x, float y, float width, float height, float startAngle, float sweepAngle);

		void DrawArc(Pen pen, float x, float y, float width, float height, float startAngle, float sweepAngle);

		void DrawImage(Image image, RectangleF rect);

		void DrawEllipse(Pen pen, RectangleF rect);

		void DrawLines(Pen pen, PointF[] points);

		void FillEllipse(Brush brush, RectangleF rect);

		void FillPath(Brush brush, GraphicsPath path);

		void FillRegion(Brush brush, Region region);

		void FillRectangle(Brush brush, RectangleF rect);

		void FillRectangle(Brush brush, float x, float y, float width, float height);

		void FillPolygon(Brush brush, PointF[] points);

		void FillPie(Brush brush, float x, float y, float width, float height, float startAngle, float sweepAngle);

		SizeF MeasureString(string text, Font font, SizeF layoutArea, StringFormat stringFormat);

		SizeF MeasureString(string text, Font font);

		SizeF MeasureString(string text, Font font, SizeF layoutArea, StringFormat stringFormat, out int charactersFitted, out int linesFilled);

		GraphicsState Save();

		void Restore(GraphicsState gstate);

		void ResetClip();

		void SetClip(RectangleF rect);

		void SetClip(GraphicsPath path, CombineMode combineMode);

		void TranslateTransform(float dx, float dy);

		void BeginSelection(string hRef, string title);

		void EndSelection();

		// --- Milestone A3: backend-agnostic overloads (Rendering.* interfaces) ---
		// Mirror the GDI+-typed members above 1:1, accepting the port interfaces
		// instead of concrete System.Drawing types. Existing signatures are kept
		// unchanged so callers migrate incrementally (Milestone B/C).

		void DrawLine(IPen pen, PointF pt1, PointF pt2);

		void DrawLine(IPen pen, float x1, float y1, float x2, float y2);

		void DrawImage(IChartImage image, Rectangle destRect, int srcX, int srcY, int srcWidth, int srcHeight, GraphicsUnit srcUnit, IImageDrawOptions imageAttr);

		void DrawEllipse(IPen pen, float x, float y, float width, float height);

		void DrawCurve(IPen pen, PointF[] points, int offset, int numberOfSegments, float tension);

		void DrawRectangle(IPen pen, int x, int y, int width, int height);

		void DrawPolygon(IPen pen, PointF[] points);

		void DrawString(string s, IChartFont font, IBrush brush, RectangleF layoutRectangle, ITextFormat format);

		void DrawString(string s, IChartFont font, IBrush brush, PointF point, ITextFormat format);

		void DrawImage(IChartImage image, Rectangle destRect, float srcX, float srcY, float srcWidth, float srcHeight, GraphicsUnit srcUnit, IImageDrawOptions imageAttrs);

		void DrawRectangle(IPen pen, float x, float y, float width, float height);

		void DrawPath(IPen pen, IGraphicsPath path);

		void DrawPie(IPen pen, float x, float y, float width, float height, float startAngle, float sweepAngle);

		void DrawArc(IPen pen, float x, float y, float width, float height, float startAngle, float sweepAngle);

		void DrawImage(IChartImage image, RectangleF rect);

		void DrawEllipse(IPen pen, RectangleF rect);

		void DrawLines(IPen pen, PointF[] points);

		void FillEllipse(IBrush brush, RectangleF rect);

		void FillPath(IBrush brush, IGraphicsPath path);

		void FillRegion(IBrush brush, IClipRegion region);

		void FillRectangle(IBrush brush, RectangleF rect);

		void FillRectangle(IBrush brush, float x, float y, float width, float height);

		void FillPolygon(IBrush brush, PointF[] points);

		void FillPie(IBrush brush, float x, float y, float width, float height, float startAngle, float sweepAngle);

		SizeF MeasureString(string text, IChartFont font, SizeF layoutArea, ITextFormat stringFormat);

		SizeF MeasureString(string text, IChartFont font);

		SizeF MeasureString(string text, IChartFont font, SizeF layoutArea, ITextFormat stringFormat, out int charactersFitted, out int linesFilled);

		void SetClip(IGraphicsPath path, CombineMode combineMode);

		/// <summary>Interface-typed equivalent of the <see cref="Clip"/> property getter.</summary>
		IClipRegion GetClipRegion();

		/// <summary>Interface-typed equivalent of the <see cref="Clip"/> property setter.</summary>
		void SetClipRegion(IClipRegion region);

		/// <summary>Interface-typed equivalent of the <see cref="Transform"/> property getter (Appendix A.6: identity/translate/rotate/scale only).</summary>
		Matrix3x2 GetTransform();

		/// <summary>Interface-typed equivalent of the <see cref="Transform"/> property setter.</summary>
		void SetTransform(Matrix3x2 matrix);
	}
}
