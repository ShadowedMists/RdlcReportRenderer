using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Microsoft.Reporting.Gauge.WebForms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ReportViewer.DataVisualization.VisualRegressionTests
{
    /// <summary>
    /// Focused math tests for <see cref="ScaleBase.DecomposeRotation"/>, the fixed-point decomposition
    /// used to convert <c>ScaleBase.DrawTickMark</c>'s composed GDI+ <see cref="Matrix"/> back into an
    /// (angle, center) pair for <c>ILinearGradientBrush.SetRotationTransform</c>. Not exercised by the
    /// existing gauge visual-regression baselines (none of the 3 sample gauges enable tick-mark
    /// gradients), so this is the only automated signal this math is correct.
    /// </summary>
    [TestClass]
    public class ScaleBaseRotationTests
    {
        [TestMethod]
        public void DecomposeRotation_SingleRotateAt_RecoversAngleAndCenter()
        {
            var center = new PointF(10f, 20f);
            using var matrix = new Matrix();
            matrix.RotateAt(37f, center);

            ScaleBase.DecomposeRotation(matrix, out float angle, out PointF recoveredCenter);

            Assert.AreEqual(37f, angle, 0.001f);
            Assert.AreEqual(center.X, recoveredCenter.X, 0.01f);
            Assert.AreEqual(center.Y, recoveredCenter.Y, 0.01f);
        }

        [TestMethod]
        public void DecomposeRotation_ComposedRotateAtSameCenter_RecoversSummedAngle()
        {
            // Mirrors CircularScale/LinearScale's DrawTickMark overrides, which call RotateAt twice
            // (e.g. the tick-mark angle, then a conditional 180-degree placement flip) around the
            // same absolutePoint — mathematically a single rotation by the summed angle.
            var center = new PointF(-5f, 8f);
            using var matrix = new Matrix();
            matrix.RotateAt(25f, center);
            matrix.RotateAt(180f, center);

            ScaleBase.DecomposeRotation(matrix, out float angle, out PointF recoveredCenter);

            float normalizedExpected = ((25f + 180f) % 360f + 360f) % 360f;
            float normalizedActual = (angle % 360f + 360f) % 360f;
            Assert.AreEqual(normalizedExpected, normalizedActual, 0.001f);
            Assert.AreEqual(center.X, recoveredCenter.X, 0.01f);
            Assert.AreEqual(center.Y, recoveredCenter.Y, 0.01f);
        }

        [TestMethod]
        public void DecomposeRotation_MatchesSetRotationTransformReconstruction()
        {
            // Confirms round-tripping through DecomposeRotation + a fresh RotateAt(angle, center)
            // (exactly what ILinearGradientBrush.SetRotationTransform does internally) reproduces
            // the identical matrix elements as the original — the actual property this decomposition
            // depends on for DrawTickMark's brush-rotation conversion to be behavior-preserving.
            var center = new PointF(3f, -7f);
            using var original = new Matrix();
            original.RotateAt(50f, center);
            original.RotateAt(15f, center);

            ScaleBase.DecomposeRotation(original, out float angle, out PointF recoveredCenter);

            using var reconstructed = new Matrix();
            reconstructed.RotateAt(angle, recoveredCenter);

            float[] expected = original.Elements;
            float[] actual = reconstructed.Elements;
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], actual[i], 0.001f, $"Element {i} mismatch");
            }
        }

        [TestMethod]
        public void DecomposeRotation_IdentityMatrix_DoesNotThrow()
        {
            using var matrix = new Matrix();

            ScaleBase.DecomposeRotation(matrix, out float angle, out PointF center);

            Assert.AreEqual(0f, angle, 0.001f);
        }
    }
}
