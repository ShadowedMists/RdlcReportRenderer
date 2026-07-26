using System;
using SkiaSharp;

namespace Microsoft.ReportingServices.Rendering.RichText
{
	/// <summary>
	/// Cross-platform, GDI+/Win32-free counterpart to <see cref="CachedFont"/>'s
	/// font-metrics surface (tasks/pdf-text-shaping-abstraction.md, P4 "font layer"
	/// step). Backed by SkiaSharp's <see cref="SKTypeface"/>/<see cref="SKFont"/>
	/// instead of a real Win32 HFONT + GDI+ <see cref="System.Drawing.Font"/>, so it can
	/// be constructed on any platform without a device context.
	///
	/// Not yet wired into <see cref="FontCache"/>/<see cref="TextRun"/>/<see cref="TextBox"/>
	/// - those still exclusively use <see cref="CachedFont.Hfont"/> to select a font into
	/// an HDC before calling Uniscribe (<see cref="FontCache.CreateFont"/>,
	/// <see cref="TextRun"/>'s ShapeAndPlace/drawing methods). Wiring this in is the
	/// larger, not-yet-started "shaping layer" step (step 3 in the task doc): it needs a
	/// HarfBuzzSharp-based replacement for ScriptShape/ScriptPlace/ScriptBreak whose
	/// output gets translated into the same <see cref="GlyphShapeData"/>/
	/// <see cref="SCRIPT_LOGATTR"/>-shaped data <see cref="LineBreaker"/>/<see cref="TextBox"/>
	/// already consume, not merely a font-metrics substitute.
	/// </summary>
	internal sealed class SkiaCachedFont : IDisposable
	{
		private readonly SKTypeface m_typeface;

		private readonly SKFont m_font;

		private readonly SKFontMetrics m_metrics;

		private float m_scaleFactor = 1f;

		internal SKTypeface Typeface => m_typeface;

		internal SKFont Font => m_font;

		internal float ScaleFactor
		{
			get => m_scaleFactor;
			set => m_scaleFactor = value;
		}

		/// <param name="fontFamily">Requested family name; falls back to <see cref="SKTypeface.Default"/> if unresolvable, mirroring <see cref="FontCache.CreateGdiPlusFont"/>'s own family-fallback behavior.</param>
		/// <param name="fontSizePixels">Font size in the same units <see cref="FontCache"/> already computes for its GDI+/Win32 fonts (device pixels at the target DPI, not points) - see <see cref="FontCache.GetKey(ITextRunProps, out string, out float)"/>'s `fontSize = textRunProps.FontSize * m_dpi / 72f`.</param>
		internal SkiaCachedFont(string fontFamily, float fontSizePixels, bool bold, bool italic)
		{
			SKFontStyle style = new SKFontStyle(
				bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
				SKFontStyleWidth.Normal,
				italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);

			m_typeface = SKTypeface.FromFamilyName(fontFamily, style) ?? SKTypeface.Default;
			m_font = new SKFont(m_typeface, fontSizePixels);
			m_metrics = m_font.Metrics;
		}

		/// <summary>Mirrors <see cref="CachedFont.GetHeight"/> (GDI TEXTMETRIC's tmHeight): ascent + descent + internal leading.</summary>
		internal int GetHeight()
		{
			return Scale((int)MathF.Round(-m_metrics.Ascent + m_metrics.Descent + m_metrics.Leading));
		}

		/// <summary>Mirrors <see cref="CachedFont.GetAscent"/> (GDI TEXTMETRIC's tmAscent). SkiaSharp's Ascent is negative (above the baseline); GDI's is a positive magnitude.</summary>
		internal int GetAscent()
		{
			return Scale((int)MathF.Round(-m_metrics.Ascent));
		}

		/// <summary>Mirrors <see cref="CachedFont.GetDescent"/> (GDI TEXTMETRIC's tmDescent).</summary>
		internal int GetDescent()
		{
			return Scale((int)MathF.Round(m_metrics.Descent));
		}

		/// <summary>Mirrors <see cref="CachedFont.GetLeading"/> (GDI TEXTMETRIC's tminternalLeading).</summary>
		internal int GetLeading()
		{
			return Scale((int)MathF.Round(m_metrics.Leading));
		}

		private int Scale(int value)
		{
			if (m_scaleFactor == 1f)
			{
				return value;
			}
			return (int)((float)value / m_scaleFactor + 0.5f);
		}

		public void Dispose()
		{
			m_font?.Dispose();
			m_typeface?.Dispose();
		}
	}
}
