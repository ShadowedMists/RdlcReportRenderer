using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Numerics;
using SkiaSharp;
using Microsoft.Reporting.Rendering;

namespace Microsoft.Reporting.Chart.WebForms.Rendering.Skia
{
	/// <summary>
	/// Spike adapter — wraps an <see cref="SKPath"/> behind <see cref="IGraphicsPath"/>.
	/// Covers the operations the sample scene exercises (lines/rectangles/ellipses/polygons,
	/// figure control, transform, bounds/hit-test). The remaining Appendix A.1 members
	/// (<c>AddCurve</c>/<c>AddClosedCurve</c>/<c>AddPath</c>/<c>AddString</c>/<c>Widen</c>/
	/// <c>Reverse</c>/<c>SetMarkers</c>/<c>Flatten</c>) have direct-enough Skia equivalents
	/// (<c>SKPath.Op</c>, <c>SKPaint.GetFillPath</c>, ...) but translating them precisely is
	/// real Milestone E1 work — stubbed here since the spike scene never calls them.
	/// </summary>
	internal sealed class SkiaGraphicsPath : IGraphicsPath
	{
		internal SKPath NativePath { get; } = new SKPath();

		public FillMode FillMode
		{
			get => NativePath.FillType == SKPathFillType.EvenOdd ? FillMode.Alternate : FillMode.Winding;
			set => NativePath.FillType = value == FillMode.Alternate ? SKPathFillType.EvenOdd : SKPathFillType.Winding;
		}

		public PointF[] PathPoints
		{
			get
			{
				var points = NativePath.Points;
				var result = new PointF[points.Length];
				for (var i = 0; i < points.Length; i++)
				{
					result[i] = new PointF(points[i].X, points[i].Y);
				}
				return result;
			}
		}

		/// <summary>
		/// Real (Milestone E2, 2026-07-23) — genuinely reachable via <c>HotRegionsList.AddHotRegion(IGraphicsPath,...)</c>,
		/// which bridges back to a concrete <see cref="GraphicsPath"/> for hit-testing storage (deliberately
		/// concrete-only regardless of backend, same as the Gauge engine's <c>HotRegionList</c>) by feeding
		/// <see cref="PathPoints"/>/<see cref="PathTypes"/> straight into <c>new GraphicsPath(points, types)</c> —
		/// which throws if the two arrays don't line up 1:1. Walks <see cref="SKPath"/>'s verb iterator and maps
		/// each verb to GDI+'s <see cref="PathPointType"/> byte encoding (Move → Start, Line → Line, Cubic → 3×
		/// Bezier, Close → OR the CloseSubpath flag onto the previous point, no new point). Quad/Conic segments
		/// (produced internally by <c>AddEllipse</c>/<c>AddArc</c>) have no direct GDI+ equivalent and aren't hit
		/// by any current caller of this method — left as a documented gap (throws) rather than silently wrong,
		/// same precedent as this file's other Appendix A.1 stubs.
		/// </summary>
		public byte[] PathTypes
		{
			get
			{
				var types = new System.Collections.Generic.List<byte>(NativePath.PointCount);
				var verbPoints = new SKPoint[4];
				using var iterator = NativePath.CreateRawIterator();
				SKPathVerb verb;
				while ((verb = iterator.Next(verbPoints)) != SKPathVerb.Done)
				{
					switch (verb)
					{
						case SKPathVerb.Move:
							types.Add((byte)PathPointType.Start);
							break;
						case SKPathVerb.Line:
							types.Add((byte)PathPointType.Line);
							break;
						case SKPathVerb.Cubic:
							types.Add((byte)PathPointType.Bezier);
							types.Add((byte)PathPointType.Bezier);
							types.Add((byte)PathPointType.Bezier);
							break;
						case SKPathVerb.Close:
							if (types.Count > 0)
							{
								types[types.Count - 1] = (byte)(types[types.Count - 1] | (byte)PathPointType.CloseSubpath);
							}
							break;
						default:
							throw new NotImplementedException($"SkiaGraphicsPath.PathTypes: {verb} segments (from AddEllipse/AddArc) have no GDI+ PathPointType equivalent implemented yet.");
					}
				}
				return types.ToArray();
			}
		}

		public int PointCount => NativePath.PointCount;

		public void AddLine(PointF pt1, PointF pt2)
		{
			NativePath.MoveTo(SkiaConvert.ToSKPoint(pt1));
			NativePath.LineTo(SkiaConvert.ToSKPoint(pt2));
		}

		public void AddLine(float x1, float y1, float x2, float y2) => AddLine(new PointF(x1, y1), new PointF(x2, y2));

		public void AddLines(PointF[] points)
		{
			if (points.Length == 0)
			{
				return;
			}
			NativePath.MoveTo(SkiaConvert.ToSKPoint(points[0]));
			for (var i = 1; i < points.Length; i++)
			{
				NativePath.LineTo(SkiaConvert.ToSKPoint(points[i]));
			}
		}

		public void AddArc(float x, float y, float width, float height, float startAngle, float sweepAngle) =>
			NativePath.AddArc(new SKRect(x, y, x + width, y + height), startAngle, sweepAngle);

		public void AddBezier(PointF pt1, PointF pt2, PointF pt3, PointF pt4)
		{
			NativePath.MoveTo(SkiaConvert.ToSKPoint(pt1));
			NativePath.CubicTo(SkiaConvert.ToSKPoint(pt2), SkiaConvert.ToSKPoint(pt3), SkiaConvert.ToSKPoint(pt4));
		}

		public void AddBeziers(PointF[] points) => throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public void AddCurve(PointF[] points, float tension) => throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public void AddCurve(PointF[] points, int offset, int numberOfSegments, float tension) => throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public void AddClosedCurve(PointF[] points) => throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public void AddClosedCurve(PointF[] points, float tension) => throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public void AddEllipse(float x, float y, float width, float height) => NativePath.AddOval(new SKRect(x, y, x + width, y + height));

		public void AddEllipse(RectangleF rect) => NativePath.AddOval(SkiaConvert.ToSKRect(rect));

		public void AddRectangle(RectangleF rect) => NativePath.AddRect(SkiaConvert.ToSKRect(rect));

		public void AddPolygon(PointF[] points)
		{
			var skPoints = new SKPoint[points.Length];
			for (var i = 0; i < points.Length; i++)
			{
				skPoints[i] = SkiaConvert.ToSKPoint(points[i]);
			}
			NativePath.AddPoly(skPoints, close: true);
		}

		public void AddPie(float x, float y, float width, float height, float startAngle, float sweepAngle)
		{
			var rect = new SKRect(x, y, x + width, y + height);
			NativePath.MoveTo(rect.MidX, rect.MidY);
			NativePath.ArcTo(rect, startAngle, sweepAngle, forceMoveTo: false);
			NativePath.Close();
		}

		public void AddPath(IGraphicsPath addingPath, bool connect) => NativePath.AddPath(((SkiaGraphicsPath)addingPath).NativePath, connect ? SKPathAddMode.Extend : SKPathAddMode.Append);

		public void AddString(string text, IChartFont font, PointF origin, ITextFormat format) => throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public void AddString(string text, IChartFont font, RectangleF layoutRect, ITextFormat format) => throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public void StartFigure()
		{
			// SKPath has no explicit figure marker to "start" ahead of the next Add* call
			// (each Add* implicitly starts a new contour with its own MoveTo); no-op.
		}

		public void CloseFigure() => NativePath.Close();

		public void CloseAllFigures() => NativePath.Close();

		public void Flatten() => throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public void Flatten(float flatness) => throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		/// <summary>
		/// Real implementation (not spike-scoped) — GDI+'s <c>GraphicsPath.Widen(Pen)</c> converts
		/// a stroked path into the filled outline geometry of that stroke. <see cref="SKPaint.GetFillPath"/>
		/// is Skia's direct equivalent (used by Skia internally to rasterize strokes), so this needs
		/// no hand-rolled geometry algorithm.
		/// </summary>
		public void Widen(IPen pen)
		{
			using SKPaint strokePaint = new SKPaint
			{
				Style = SKPaintStyle.Stroke,
				StrokeWidth = pen.Width,
				StrokeCap = SkiaConvert.ToSKStrokeCap(pen.StartCap),
				StrokeJoin = SkiaConvert.ToSKStrokeJoin(pen.LineJoin),
			};
			using SKPath widened = new SKPath();
			strokePaint.GetFillPath(NativePath, widened);
			NativePath.Reset();
			NativePath.AddPath(widened, SKPathAddMode.Append);
		}

		public void Reverse() => throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public void SetMarkers()
		{
			// GDI+-specific path-marker concept with no Skia equivalent; no-op (not read back by the spike scene).
		}

		public void Transform(Matrix3x2 matrix) =>
			NativePath.Transform(new SKMatrix(matrix.M11, matrix.M21, matrix.M31, matrix.M12, matrix.M22, matrix.M32, 0, 0, 1));

		public RectangleF GetBounds()
		{
			var b = NativePath.Bounds;
			return new RectangleF(b.Left, b.Top, b.Width, b.Height);
		}

		public RectangleF GetBounds(Matrix3x2 matrix) => throw new NotImplementedException("Spike scope: not exercised by the sample scene.");

		public bool IsVisible(PointF point) => NativePath.Contains(point.X, point.Y);

		public void Reset() => NativePath.Reset();

		public void Dispose() => NativePath.Dispose();
	}
}
