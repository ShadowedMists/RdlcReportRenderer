using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Microsoft.ReportViewer.DataVisualization.VisualRegressionTests
{
    internal sealed class ImageDiffResult
    {
        public bool Matches { get; init; }
        public string Message { get; init; } = "";
    }

    /// <summary>
    /// Pixel-exact comparison against a committed baseline PNG. On mismatch (or on a missing
    /// baseline) the actual output and a visual diff are written next to the test binaries under
    /// "Results/" so a human can inspect what changed.
    /// </summary>
    internal static class ImageComparer
    {
        // Per-channel tolerance. The chart engine's rendering is deterministic for identical
        // inputs, so this only needs to absorb encoder rounding, not anti-aliasing drift.
        private const int ChannelTolerance = 2;

        /// <summary>
        /// Allows a handful of pixels to exceed <see cref="ChannelTolerance"/>. Found (2026-07-20)
        /// that GDI+'s anti-aliasing for rotated text is not perfectly deterministic across
        /// separate process runs on this machine — confirmed by rendering fully unmodified,
        /// pre-existing code twice in separate `dotnet test` invocations and seeing the same 3
        /// pixels (out of 160000, all on a single rotated glyph edge) drift by a few tolerance
        /// units each time. Not caused by any GDI+-abstraction conversion; a pre-existing property
        /// of rotated-text rendering the harness's "fully deterministic" assumption didn't cover.
        /// Default 0 preserves strict comparison for every other (axis-aligned or unrotated) test.
        /// </summary>
        internal static ImageDiffResult CompareToBaseline(byte[] actualPngBytes, string baselineName, int maxDiffPixels = 0)
        {
            var baselinePath = Path.Combine(AppContext.BaseDirectory, "Baselines", baselineName);
            var resultsDir = Path.Combine(AppContext.BaseDirectory, "Results");
            Directory.CreateDirectory(resultsDir);

            var actualPath = Path.Combine(resultsDir, baselineName);
            File.WriteAllBytes(actualPath, actualPngBytes);

            if (!File.Exists(baselinePath))
            {
                return new ImageDiffResult
                {
                    Matches = false,
                    Message = $"No baseline found at '{baselinePath}'. Actual output was written to " +
                              $"'{actualPath}' — review it and copy it into the Baselines/ folder " +
                              "(with Copy to Output Directory) to establish the baseline."
                };
            }

            using var baseline = Image.Load<Rgba32>(baselinePath);
            using var actual = Image.Load<Rgba32>(actualPngBytes);

            if (baseline.Width != actual.Width || baseline.Height != actual.Height)
            {
                return new ImageDiffResult
                {
                    Matches = false,
                    Message = $"Size mismatch: baseline is {baseline.Width}x{baseline.Height}, " +
                              $"actual is {actual.Width}x{actual.Height}. Actual written to '{actualPath}'."
                };
            }

            long diffPixels = 0;
            using var diff = new Image<Rgba32>(baseline.Width, baseline.Height);

            for (var y = 0; y < baseline.Height; y++)
            {
                for (var x = 0; x < baseline.Width; x++)
                {
                    var b = baseline[x, y];
                    var a = actual[x, y];
                    var different =
                        Math.Abs(b.R - a.R) > ChannelTolerance ||
                        Math.Abs(b.G - a.G) > ChannelTolerance ||
                        Math.Abs(b.B - a.B) > ChannelTolerance ||
                        Math.Abs(b.A - a.A) > ChannelTolerance;

                    diff[x, y] = different ? new Rgba32(255, 0, 0, 255) : new Rgba32(0, 0, 0, 0);
                    if (different)
                    {
                        diffPixels++;
                    }
                }
            }

            if (diffPixels <= maxDiffPixels)
            {
                return new ImageDiffResult { Matches = true };
            }

            var diffPath = Path.Combine(resultsDir, Path.GetFileNameWithoutExtension(baselineName) + ".diff.png");
            diff.Mutate(ctx => ctx.BackgroundColor(Color.Transparent));
            diff.SaveAsPng(diffPath);

            var totalPixels = (long)baseline.Width * baseline.Height;
            var percent = 100.0 * diffPixels / totalPixels;
            return new ImageDiffResult
            {
                Matches = false,
                Message = $"{diffPixels} of {totalPixels} pixels ({percent:F3}%) differ beyond tolerance " +
                          $"{ChannelTolerance}. Actual: '{actualPath}'. Diff (red = changed): '{diffPath}'."
            };
        }
    }
}
