using System;
using System.IO;
using HarfBuzzSharp;
using NUnit.Framework;
using SkiaSharp;

namespace ReportViewerCore.LinuxRenderers.Tests
{
    /// <summary>
    /// Phase 1 spike for tasks/pdf-text-shaping-abstraction.md: validates that a plain-Latin
    /// paragraph can be shaped (text -> glyph ids + advances) with SkiaSharp (font/glyph
    /// metrics) + HarfBuzzSharp (shaping) alone, with no Win32/Uniscribe/System.Drawing
    /// dependency anywhere in the call path. This is deliberately not wired into any
    /// production class yet - see that doc's phased plan for why (font-layer port,
    /// shaping-layer port, and new visual-verification tooling all come before production
    /// wiring).
    /// </summary>
    public class TextShapingSpikeTests
    {
        [Test]
        public void HarfBuzzSharp_ShapesPlainLatinText_ProducesOneGlyphPerCharacterWithPositiveAdvances()
        {
            const string text = "Hello, PDF!";

            using var skTypeface = SKTypeface.FromFamilyName(SKTypeface.Default.FamilyName, SKFontStyle.Normal);
            using var blob = OpenTypefaceBlob(skTypeface);
            using var hbFace = new Face(blob, 0);
            using var hbFont = new HarfBuzzSharp.Font(hbFace);
            hbFont.SetScale(1000, 1000);

            using var buffer = new HarfBuzzSharp.Buffer();
            buffer.AddUtf16(text);
            buffer.GuessSegmentProperties();

            hbFont.Shape(buffer);

            int glyphCount = buffer.Length;
            Assert.That(glyphCount, Is.GreaterThan(0), "HarfBuzzSharp should produce at least one glyph for plain Latin text");
            // A simple, non-ligated Latin string should shape 1:1 with its characters.
            Assert.That(glyphCount, Is.EqualTo(text.Length),
                "Plain Latin text with no ligatures should shape to one glyph per character");

            var infos = buffer.GlyphInfos;
            var positions = buffer.GlyphPositions;
            Assert.That(infos.Length, Is.EqualTo(positions.Length));

            for (int i = 0; i < infos.Length; i++)
            {
                if (char.IsWhiteSpace(text[i]))
                {
                    continue;
                }
                Assert.That(infos[i].Codepoint, Is.Not.EqualTo(0u), $"Glyph {i} should resolve to a real glyph id, not .notdef");
                Assert.That(positions[i].XAdvance, Is.GreaterThan(0), $"Glyph {i} should have a positive horizontal advance");
            }
        }

        [Test]
        public void SkiaSharp_MeasuresGlyphMetrics_WithoutSystemDrawingOrWin32()
        {
            using var typeface = SKTypeface.FromFamilyName(SKTypeface.Default.FamilyName, SKFontStyle.Normal);
            using var font = new SKFont(typeface, size: 12);

            ushort[] glyphs = font.GetGlyphs("PDF");
            Assert.That(glyphs.Length, Is.EqualTo(3));
            Assert.That(Array.TrueForAll(glyphs, g => g != 0), "All three characters should resolve to real glyphs");

            float[] widths = font.GetGlyphWidths(glyphs);
            Assert.That(widths.Length, Is.EqualTo(3));
            Assert.That(Array.TrueForAll(widths, w => w > 0), "All glyph advance widths should be positive");

            SKFontMetrics metrics = font.Metrics;
            Assert.That(metrics.Descent - metrics.Ascent, Is.GreaterThan(0), "Font should report a positive line height");
        }

        private static Blob OpenTypefaceBlob(SKTypeface typeface)
        {
            using SKStreamAsset stream = typeface.OpenStream(out int ttcIndex);
            using SKData data = SKData.Create(stream);
            return Blob.FromStream(new MemoryStream(data.ToArray()));
        }
    }
}
