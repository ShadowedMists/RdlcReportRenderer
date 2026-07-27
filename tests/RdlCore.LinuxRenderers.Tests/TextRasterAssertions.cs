using System;
using System.Collections.Generic;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace RdlCore.LinuxRenderers.Tests
{
    internal sealed class TextRasterCheckResult
    {
        public bool Passed { get; init; }
        public string Message { get; init; } = "";
    }

    /// <summary>
    /// Behavioral (not pixel-diff) verification for shaped/rasterized text: checks glyph
    /// ink actually landed where shaping said it would. Chosen deliberately over a
    /// checked-in golden-image PNG comparison (as Chart's ImageComparer uses) for two
    /// reasons documented in tasks/pdf-text-shaping-abstraction.md: (1) AGENTS.md's
    /// Testing Philosophy prefers behavioral tests and explicitly says to avoid
    /// pixel-perfect comparisons where possible; (2) SKTypeface.Default resolves to
    /// whatever font is installed on the host, which is not guaranteed identical between
    /// this Windows dev environment and a Linux CI/production image with no fontconfig
    /// fonts installed - a pixel-exact baseline generated here would not be reproducible
    /// there. This still catches the class of bug this repo has hit before (Chart's
    /// SkiaGraphicsPath.AddLine/AddArc bugs: a scene "rendered" without throwing but was
    /// visibly wrong) because a dropped/misplaced/blank glyph shows up as a missing-ink
    /// column, independent of which font was actually used.
    /// </summary>
    internal static class TextRasterAssertions
    {
        private const byte InkLuminanceThreshold = 250;

        /// <summary>
        /// Verifies every non-whitespace character produced visible ink within its own
        /// shaped glyph column (with a small horizontal tolerance for antialiasing/hinting
        /// overhang), and that all ink stays within the font's vertical ascent/descent
        /// bounds (with margin). Does not require ink to be absent from whitespace columns:
        /// only that non-whitespace glyphs are not silently blank or drawn off-column.
        /// </summary>
        internal static TextRasterCheckResult VerifyGlyphInkPresence(string text, TextRasterResult result, float columnTolerance = 1.5f)
        {
            SKBitmap bitmap = result.Bitmap;
            SKShaper.Result shaped = result.Shaped;
            SKPoint[] points = shaped.Points;

            if (points.Length == 0)
            {
                return new TextRasterCheckResult { Passed = string.IsNullOrEmpty(text), Message = "No glyphs were shaped for non-empty text." };
            }

            float top = result.BaselineY + result.Metrics.Ascent - 2f;
            float bottom = result.BaselineY + result.Metrics.Descent + 2f;

            var missing = new List<string>();
            for (int i = 0; i < points.Length; i++)
            {
                uint clusterStart = shaped.Clusters[i];
                if (clusterStart < (uint)text.Length && char.IsWhiteSpace(text[(int)clusterStart]))
                {
                    continue;
                }

                float columnStart = result.OriginX + points[i].X - columnTolerance;
                float columnEnd = result.OriginX + (i + 1 < points.Length ? points[i + 1].X : shaped.Width) + columnTolerance;

                if (!HasInk(bitmap, columnStart, columnEnd, top, bottom))
                {
                    missing.Add($"glyph[{i}] (cluster char '{Describe(text, clusterStart)}') expected in x=[{columnStart:F1},{columnEnd:F1}] y=[{top:F1},{bottom:F1}]");
                }
            }

            if (missing.Count > 0)
            {
                return new TextRasterCheckResult
                {
                    Passed = false,
                    Message = $"{missing.Count} of {points.Length} glyphs had no ink in their expected column: {string.Join("; ", missing)}"
                };
            }

            return new TextRasterCheckResult { Passed = true };
        }

        /// <summary>
        /// Verifies glyph pen positions are monotonically non-decreasing in X - i.e. this
        /// is plain LTR text with no reordering. RTL/bidi text is a documented gap
        /// (tasks/pdf-text-shaping-abstraction.md) this harness does not attempt to verify.
        /// </summary>
        internal static TextRasterCheckResult VerifyMonotonicLtrAdvance(TextRasterResult result)
        {
            SKPoint[] points = result.Shaped.Points;
            for (int i = 1; i < points.Length; i++)
            {
                if (points[i].X < points[i - 1].X)
                {
                    return new TextRasterCheckResult
                    {
                        Passed = false,
                        Message = $"Glyph {i} at x={points[i].X:F1} is left of glyph {i - 1} at x={points[i - 1].X:F1}; expected non-decreasing X for LTR text."
                    };
                }
            }
            return new TextRasterCheckResult { Passed = true };
        }

        private static string Describe(string text, uint clusterStart)
        {
            return clusterStart < (uint)text.Length ? text[(int)clusterStart].ToString() : "?";
        }

        private static bool HasInk(SKBitmap bitmap, float xStart, float xEnd, float yStart, float yEnd)
        {
            int x0 = Math.Max(0, (int)Math.Floor(xStart));
            int x1 = Math.Min(bitmap.Width - 1, (int)Math.Ceiling(xEnd));
            int y0 = Math.Max(0, (int)Math.Floor(yStart));
            int y1 = Math.Min(bitmap.Height - 1, (int)Math.Ceiling(yEnd));

            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    SKColor pixel = bitmap.GetPixel(x, y);
                    if (pixel.Red < InkLuminanceThreshold || pixel.Green < InkLuminanceThreshold || pixel.Blue < InkLuminanceThreshold)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
