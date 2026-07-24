using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Numerics;
using Microsoft.Reporting.Chart.WebForms.Rendering;
using Microsoft.Reporting.Rendering;
using Microsoft.Reporting.Chart.WebForms.Rendering.Skia;
using SkiaSharp;

namespace Microsoft.Reporting.Chart.WebForms
{
	/// <summary>
	/// Spike prototype (tasks/chart-cross-platform-implementation.md Phase 0) —
	/// SkiaSharp-backed sibling of <see cref="GdiGraphics"/>. Only implements the
	/// Milestone A3 interface-typed (<c>Rendering.*</c>) subset of
	/// <see cref="IChartRenderingEngine"/> for real; the GDI+-typed members (needed to
	/// satisfy the interface, since <see cref="ChartGraphics"/> still calls them directly —
	/// see Milestone B1b blocker notes) are intentionally unreachable stubs. A production
	/// backend can't ship until B1b/B2/C1-C8 stop routing through the GDI+-typed overloads
	/// at all (see spike report).
	/// </summary>
	internal sealed class SkiaChartGraphics : IChartRenderingEngine
	{
		private SKCanvas canvas;

		internal SKCanvas Canvas
		{
			get => canvas;
			set => canvas = value;
		}

		private static SKPaint Native(IPen pen) => ((SkiaPen)pen).NativePaint;

		private static SKPaint Native(IBrush brush) => brush switch
		{
			SkiaSolidBrush b => b.NativePaint,
			SkiaTextureBrush b => b.NativePaint,
			SkiaLinearGradientBrush b => b.NativePaint,
			SkiaPathGradientBrush b => b.NativePaint,
			_ => throw new NotSupportedException($"Spike scope: unrecognized IBrush implementation: {brush.GetType()}"),
		};

		private static SKFont Native(IChartFont font) => ((SkiaChartFont)font).NativeFont;

		private static SKPath Native(IGraphicsPath path) => ((SkiaGraphicsPath)path).NativePath;

		// --- Milestone A3 interface-typed members: the actual spike surface ---

		public void DrawLine(IPen pen, PointF pt1, PointF pt2) =>
			canvas.DrawLine(SkiaConvert.ToSKPoint(pt1), SkiaConvert.ToSKPoint(pt2), Native(pen));

		public void DrawLine(IPen pen, float x1, float y1, float x2, float y2) => canvas.DrawLine(x1, y1, x2, y2, Native(pen));

		public void DrawRectangle(IPen pen, int x, int y, int width, int height) => canvas.DrawRect(new SKRect(x, y, x + width, y + height), Native(pen));

		public void DrawRectangle(IPen pen, float x, float y, float width, float height) => canvas.DrawRect(new SKRect(x, y, x + width, y + height), Native(pen));

		public void DrawEllipse(IPen pen, float x, float y, float width, float height) => canvas.DrawOval(new SKRect(x, y, x + width, y + height), Native(pen));

		public void DrawEllipse(IPen pen, RectangleF rect) => canvas.DrawOval(SkiaConvert.ToSKRect(rect), Native(pen));

		public void DrawPolygon(IPen pen, PointF[] points)
		{
			using var path = ToClosedSKPath(points);
			canvas.DrawPath(path, Native(pen));
		}

		public void DrawPath(IPen pen, IGraphicsPath path) => canvas.DrawPath(Native(path), Native(pen));

		public void FillEllipse(IBrush brush, RectangleF rect) => canvas.DrawOval(SkiaConvert.ToSKRect(rect), Native(brush));

		public void FillPath(IBrush brush, IGraphicsPath path) => canvas.DrawPath(Native(path), Native(brush));

		public void FillRectangle(IBrush brush, RectangleF rect) => canvas.DrawRect(SkiaConvert.ToSKRect(rect), Native(brush));

		public void FillRectangle(IBrush brush, float x, float y, float width, float height) => canvas.DrawRect(new SKRect(x, y, x + width, y + height), Native(brush));

		public void FillPolygon(IBrush brush, PointF[] points)
		{
			using var path = ToClosedSKPath(points);
			canvas.DrawPath(path, Native(brush));
		}

		public void DrawString(string s, IChartFont font, IBrush brush, RectangleF layoutRectangle, ITextFormat format) =>
			DrawAlignedString(s, Native(font), Native(brush), layoutRectangle, format);

		public void DrawString(string s, IChartFont font, IBrush brush, PointF point, ITextFormat format) =>
			DrawAlignedString(s, Native(font), Native(brush), new RectangleF(point.X, point.Y, 0, 0), format);

		public SizeF MeasureString(string text, IChartFont font, SizeF layoutArea, ITextFormat stringFormat) => MeasureString(text, font);

		public SizeF MeasureString(string text, IChartFont font)
		{
			var nativeFont = Native(font);
			var width = nativeFont.MeasureText(text);
			nativeFont.GetFontMetrics(out var metrics);
			return new SizeF(width, metrics.Descent - metrics.Ascent);
		}

		public SizeF MeasureString(string text, IChartFont font, SizeF layoutArea, ITextFormat stringFormat, out int charactersFitted, out int linesFilled)
		{
			charactersFitted = text.Length;
			linesFilled = 1;
			return MeasureString(text, font);
		}

		public IClipRegion GetClipRegion() => new SkiaClipRegion();

		public void SetClipRegion(IClipRegion region)
		{
			// Spike scope: the sample scene never clips (see SkiaClipRegion); no-op.
		}

		public Matrix3x2 GetTransform() => Matrix3x2.Identity;

		public void SetTransform(Matrix3x2 matrix)
		{
			// Spike scope: the sample scene draws in device space directly; no-op.
		}

		private void DrawAlignedString(string s, SKFont font, SKPaint paint, RectangleF layoutRectangle, ITextFormat format)
		{
			var width = font.MeasureText(s);
			font.GetFontMetrics(out var metrics);
			var textHeight = metrics.Descent - metrics.Ascent;

			var alignment = format?.Alignment ?? StringAlignment.Near;
			var lineAlignment = format?.LineAlignment ?? StringAlignment.Near;

			float x = alignment switch
			{
				StringAlignment.Center => layoutRectangle.X + (layoutRectangle.Width - width) / 2f,
				StringAlignment.Far => layoutRectangle.X + layoutRectangle.Width - width,
				_ => layoutRectangle.X,
			};

			float top = lineAlignment switch
			{
				StringAlignment.Center => layoutRectangle.Y + (layoutRectangle.Height - textHeight) / 2f,
				StringAlignment.Far => layoutRectangle.Y + layoutRectangle.Height - textHeight,
				_ => layoutRectangle.Y,
			};

			var baselineY = top - metrics.Ascent;
			canvas.DrawText(s, x, baselineY, SKTextAlign.Left, font, paint);
		}

		private static SKPath ToClosedSKPath(PointF[] points)
		{
			var path = new SKPath();
			path.AddPoly(Array.ConvertAll(points, SkiaConvert.ToSKPoint), close: true);
			return path;
		}

		// --- The GDI+-typed members immediately below are intentionally unreachable in the
		// spike: ChartGraphics still allocates concrete GDI+ objects (new Pen/SolidBrush/Font/
		// GraphicsPath, see ChartGraphics.cs fields + the B1b blocker notes in
		// chart-gdi-type-abstraction.md) and calls the GDI+-typed IChartRenderingEngine
		// overloads, not the interface-typed ones above. A real backend can only retire these
		// once B1b/B2/C1-C8 land. The interface-typed (Rendering.*) overloads further down this
		// block (DrawImage(IChartImage,...)/DrawPie/DrawArc/DrawLines/FillPie/FillRegion/
		// DrawCurve(IPen,...)) are genuinely reachable from real E1-converted call paths and are
		// implemented for real (E1, 2026-07-22) rather than left as NotReachable() stubs.

		private static NotSupportedException NotReachable([System.Runtime.CompilerServices.CallerMemberName] string member = "") =>
			new($"{member}: unreachable in the spike — ChartGraphics still calls the GDI+-typed IChartRenderingEngine surface " +
				"directly (see B1b blocker in chart-gdi-type-abstraction.md); only the Rendering.*-typed overloads are implemented here.");

		public Matrix Transform { get => throw NotReachable(); set => throw NotReachable(); }
		public SmoothingMode SmoothingMode { get => throw NotReachable(); set => throw NotReachable(); }
		public TextRenderingHint TextRenderingHint { get => throw NotReachable(); set => throw NotReachable(); }
		public Region Clip { get => throw NotReachable(); set => throw NotReachable(); }
		public Graphics Graphics { get => throw NotReachable(); set => throw NotReachable(); }
		public bool IsClipEmpty => throw NotReachable();
		public CompositingQuality CompositingQuality { get => throw NotReachable(); set => throw NotReachable(); }
		public InterpolationMode InterpolationMode { get => throw NotReachable(); set => throw NotReachable(); }
		public float GetDpiX() => throw NotReachable();
		public void BindSurface(IRenderSurface surface) => throw NotReachable();

		public void DrawLine(Pen pen, PointF pt1, PointF pt2) => throw NotReachable();
		public void DrawLine(Pen pen, float x1, float y1, float x2, float y2) => throw NotReachable();
		public void DrawImage(Image image, Rectangle destRect, int srcX, int srcY, int srcWidth, int srcHeight, GraphicsUnit srcUnit, ImageAttributes imageAttr) => throw NotReachable();
		public void DrawEllipse(Pen pen, float x, float y, float width, float height) => throw NotReachable();
		public void DrawCurve(Pen pen, PointF[] points, int offset, int numberOfSegments, float tension) => throw NotReachable();
		public void DrawRectangle(Pen pen, int x, int y, int width, int height) => throw NotReachable();
		public void DrawPolygon(Pen pen, PointF[] points) => throw NotReachable();
		public void DrawString(string s, Font font, Brush brush, RectangleF layoutRectangle, StringFormat format) => throw NotReachable();
		public void DrawString(string s, Font font, Brush brush, PointF point, StringFormat format) => throw NotReachable();
		public void DrawImage(Image image, Rectangle destRect, float srcX, float srcY, float srcWidth, float srcHeight, GraphicsUnit srcUnit, ImageAttributes imageAttrs) => throw NotReachable();
		public void DrawRectangle(Pen pen, float x, float y, float width, float height) => throw NotReachable();
		public void DrawPath(Pen pen, GraphicsPath path) => throw NotReachable();
		public void DrawPie(Pen pen, float x, float y, float width, float height, float startAngle, float sweepAngle) => throw NotReachable();
		public void DrawArc(Pen pen, float x, float y, float width, float height, float startAngle, float sweepAngle) => throw NotReachable();
		public void DrawImage(Image image, RectangleF rect) => throw NotReachable();
		public void DrawEllipse(Pen pen, RectangleF rect) => throw NotReachable();
		public void DrawLines(Pen pen, PointF[] points) => throw NotReachable();
		public void FillEllipse(Brush brush, RectangleF rect) => throw NotReachable();
		public void FillPath(Brush brush, GraphicsPath path) => throw NotReachable();
		public void FillRegion(Brush brush, Region region) => throw NotReachable();
		public void FillRectangle(Brush brush, RectangleF rect) => throw NotReachable();
		public void FillRectangle(Brush brush, float x, float y, float width, float height) => throw NotReachable();
		public void FillPolygon(Brush brush, PointF[] points) => throw NotReachable();
		public void FillPie(Brush brush, float x, float y, float width, float height, float startAngle, float sweepAngle) => throw NotReachable();
		public SizeF MeasureString(string text, Font font, SizeF layoutArea, StringFormat stringFormat) => throw NotReachable();
		public SizeF MeasureString(string text, Font font) => throw NotReachable();
		public SizeF MeasureString(string text, Font font, SizeF layoutArea, StringFormat stringFormat, out int charactersFitted, out int linesFilled) => throw NotReachable();
		public GraphicsState Save() => throw NotReachable();
		public void Restore(GraphicsState gstate) => throw NotReachable();
		public void ResetClip() => throw NotReachable();
		public void SetClip(RectangleF rect) => throw NotReachable();
		public void SetClip(GraphicsPath path, CombineMode combineMode) => throw NotReachable();
		public void TranslateTransform(float dx, float dy) => throw NotReachable();
		public void BeginSelection(string hRef, string title) { }
		public void EndSelection() { }
		public void DrawImage(IChartImage image, Rectangle destRect, int srcX, int srcY, int srcWidth, int srcHeight, GraphicsUnit srcUnit, IImageDrawOptions imageAttr) =>
			DrawImageCore(image, destRect, srcX, srcY, srcWidth, srcHeight, imageAttr);
		public void DrawCurve(IPen pen, PointF[] points, int offset, int numberOfSegments, float tension)
		{
			using SKPath path = BuildCurvePath(points, offset, numberOfSegments, tension);
			canvas.DrawPath(path, Native(pen));
		}
		public void DrawImage(IChartImage image, Rectangle destRect, float srcX, float srcY, float srcWidth, float srcHeight, GraphicsUnit srcUnit, IImageDrawOptions imageAttrs) =>
			DrawImageCore(image, destRect, srcX, srcY, srcWidth, srcHeight, imageAttrs);
		public void DrawPie(IPen pen, float x, float y, float width, float height, float startAngle, float sweepAngle) =>
			canvas.DrawArc(new SKRect(x, y, x + width, y + height), startAngle, sweepAngle, useCenter: true, Native(pen));
		public void DrawArc(IPen pen, float x, float y, float width, float height, float startAngle, float sweepAngle) =>
			canvas.DrawArc(new SKRect(x, y, x + width, y + height), startAngle, sweepAngle, useCenter: false, Native(pen));
		public void DrawImage(IChartImage image, RectangleF rect) => canvas.DrawBitmap(((SkiaChartImage)image).NativeBitmap, SkiaConvert.ToSKRect(rect));
		public void DrawLines(IPen pen, PointF[] points)
		{
			using SKPath path = new SKPath();
			path.AddPoly(Array.ConvertAll(points, SkiaConvert.ToSKPoint), close: false);
			canvas.DrawPath(path, Native(pen));
		}
		public void FillRegion(IBrush brush, IClipRegion region)
		{
			using SKPath path = ((SkiaClipRegion)region).ToDrawablePath();
			canvas.DrawPath(path, Native(brush));
		}
		public void FillPie(IBrush brush, float x, float y, float width, float height, float startAngle, float sweepAngle) =>
			canvas.DrawArc(new SKRect(x, y, x + width, y + height), startAngle, sweepAngle, useCenter: true, Native(brush));

		// Mirrors the no-op GDI+-typed SetClip(GraphicsPath, CombineMode) above and SvgChartGraphics's
		// identical no-op — real callers of this overload are the Gauge/Map engines' own separate
		// GDI+-coupled pipelines (out of scope here); wiring SkiaClipRegion into the canvas's own clip
		// stack (Save/Restore parity included) is the still-open follow-up noted on SkiaClipRegion/
		// SetClipRegion above, not something this pass attempts.
		public void SetClip(IGraphicsPath path, CombineMode combineMode) { }

		/// <summary>Shared by both int/float <c>DrawImage(IChartImage,...)</c> overloads — GDI+'s <c>Graphics.DrawImage(Image, Rectangle, float×4, GraphicsUnit, ImageAttributes)</c> equivalent. <paramref name="srcUnit"/> is ignored (every real caller passes <see cref="GraphicsUnit.Pixel"/>).</summary>
		private void DrawImageCore(IChartImage image, RectangleF destRect, float srcX, float srcY, float srcWidth, float srcHeight, IImageDrawOptions imageAttr)
		{
			SKBitmap bitmap = ((SkiaChartImage)image).NativeBitmap;
			SkiaImageDrawOptions options = imageAttr as SkiaImageDrawOptions;
			SKBitmap ownedBitmap = null;
			if (options?.TransparentColor is Color transparentColor)
			{
				bitmap = ownedBitmap = ApplyColorKey(bitmap, SkiaConvert.ToSKColor(transparentColor));
			}
			try
			{
				SKRect srcRect = new SKRect(srcX, srcY, srcX + srcWidth, srcY + srcHeight);
				canvas.DrawBitmap(bitmap, srcRect, SkiaConvert.ToSKRect(destRect));
			}
			finally
			{
				ownedBitmap?.Dispose();
			}
		}

		private static SKBitmap ApplyColorKey(SKBitmap source, SKColor keyColor)
		{
			SKBitmap result = source.Copy();
			for (int y = 0; y < result.Height; y++)
			{
				for (int x = 0; x < result.Width; x++)
				{
					SKColor pixel = result.GetPixel(x, y);
					if (pixel.Red == keyColor.Red && pixel.Green == keyColor.Green && pixel.Blue == keyColor.Blue)
					{
						result.SetPixel(x, y, SKColors.Transparent);
					}
				}
			}
			return result;
		}

		/// <summary>Cardinal-spline-to-Bezier conversion matching GDI+'s <c>Graphics.DrawCurve</c> tension convention (each segment's control points scaled by <paramref name="tension"/>/3) — SkiaSharp has no native cardinal-spline primitive.</summary>
		private static SKPath BuildCurvePath(PointF[] points, int offset, int numberOfSegments, float tension)
		{
			SKPath path = new SKPath();
			path.MoveTo(SkiaConvert.ToSKPoint(points[offset]));
			float t = tension / 3f;
			for (int i = 0; i < numberOfSegments; i++)
			{
				int i0 = offset + i;
				PointF p0 = i0 > 0 ? points[i0 - 1] : points[i0];
				PointF p1 = points[i0];
				PointF p2 = points[i0 + 1];
				PointF p3 = i0 + 2 < points.Length ? points[i0 + 2] : points[i0 + 1];
				SKPoint c1 = new SKPoint(p1.X + (p2.X - p0.X) * t, p1.Y + (p2.Y - p0.Y) * t);
				SKPoint c2 = new SKPoint(p2.X - (p3.X - p1.X) * t, p2.Y - (p3.Y - p1.Y) * t);
				path.CubicTo(c1, c2, SkiaConvert.ToSKPoint(p2));
			}
			return path;
		}
	}
}
