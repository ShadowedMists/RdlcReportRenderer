using System;
using System.IO;
using SkiaSharp;

namespace Microsoft.ReportViewer.Chart.Rdl.Tests
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
        private const int ChannelTolerance = 2;

        internal static ImageDiffResult CompareToBaseline(byte[] actualPngBytes, string baselineName)
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

            using var baseline = SKBitmap.Decode(baselinePath);
            using var actual = SKBitmap.Decode(actualPngBytes);

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
            using var diff = new SKBitmap(baseline.Width, baseline.Height);

            for (var y = 0; y < baseline.Height; y++)
            {
                for (var x = 0; x < baseline.Width; x++)
                {
                    var b = baseline.GetPixel(x, y);
                    var a = actual.GetPixel(x, y);
                    var different =
                        Math.Abs(b.Red - a.Red) > ChannelTolerance ||
                        Math.Abs(b.Green - a.Green) > ChannelTolerance ||
                        Math.Abs(b.Blue - a.Blue) > ChannelTolerance ||
                        Math.Abs(b.Alpha - a.Alpha) > ChannelTolerance;

                    diff.SetPixel(x, y, different ? new SKColor(255, 0, 0, 255) : SKColors.Transparent);
                    if (different)
                    {
                        diffPixels++;
                    }
                }
            }

            if (diffPixels == 0)
            {
                return new ImageDiffResult { Matches = true };
            }

            var diffPath = Path.Combine(resultsDir, Path.GetFileNameWithoutExtension(baselineName) + ".diff.png");
            using (var diffData = diff.Encode(SKEncodedImageFormat.Png, 100))
            using (var diffStream = File.Create(diffPath))
            {
                diffData.SaveTo(diffStream);
            }

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
