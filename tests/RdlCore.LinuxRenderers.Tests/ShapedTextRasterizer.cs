using System;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace RdlCore.LinuxRenderers.Tests
{
    /// <summary>
    /// Renders a HarfBuzzSharp-shaped string to an off-screen SkiaSharp bitmap.
    /// This is the harness under test for tasks/pdf-text-shaping-abstraction.md's
    /// verification-tooling step: it exercises the exact shape-then-position-glyphs
    /// pipeline a future production PDF/Skia text shaper would use (SKShaper wraps
    /// HarfBuzzSharp shaping + converts font-unit advances to pixel positions), so bugs
    /// in that pipeline (wrong glyph, wrong position, dropped glyph) show up here before
    /// any production wiring exists.
    /// </summary>
    internal sealed class TextRasterResult
    {
        public required SKBitmap Bitmap { get; init; }
        public required SKShaper.Result Shaped { get; init; }
        public required float OriginX { get; init; }
        public required float BaselineY { get; init; }
        public required SKFontMetrics Metrics { get; init; }
    }

    internal static class ShapedTextRasterizer
    {
        private const float Margin = 8f;

        internal static TextRasterResult Render(string text, SKTypeface typeface, float fontSize)
        {
            using var font = new SKFont(typeface, fontSize);
            using var shaper = new SKShaper(typeface);

            SKShaper.Result shaped = shaper.Shape(text, font);
            SKFontMetrics metrics = font.Metrics;

            float originX = Margin;
            float baselineY = Margin - metrics.Ascent; // Ascent is negative in SkiaSharp.
            int width = (int)Math.Ceiling(shaped.Width + Margin * 2);
            int height = (int)Math.Ceiling(metrics.Descent - metrics.Ascent + Margin * 2);

            var bitmap = new SKBitmap(Math.Max(width, 1), Math.Max(height, 1));
            using (var canvas = new SKCanvas(bitmap))
            using (var paint = new SKPaint { Color = SKColors.Black, IsAntialias = true })
            {
                canvas.Clear(SKColors.White);
                canvas.DrawShapedText(shaper, text, originX, baselineY, font, paint);
            }

            return new TextRasterResult
            {
                Bitmap = bitmap,
                Shaped = shaped,
                OriginX = originX,
                BaselineY = baselineY,
                Metrics = metrics
            };
        }
    }
}
