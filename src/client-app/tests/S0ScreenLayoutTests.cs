// File:     src/client-app/tests/S0ScreenLayoutTests.cs
// Created:  2026-10-08
// Modified: 2026-10-08
// Author:   —
// Spec:     S0 binding contracts §3, I-Q16, Code Standards #20
// Purpose:  Check measured reflow and dense pitch-label layout independently of Unity font/render binding.

using System;
using NUnit.Framework;
using UnityEngine;

namespace TacticalDirector.ClientApp.Tests
{
    [TestFixture]
    public sealed class S0ScreenLayoutTests
    {
        [Test]
        public void MeasuredWidthsChooseColumnsOrStackAtActualBoundary()
        {
            Assert.IsTrue(S0ScreenLayout.CanUseColumns(1016, 600, 400, 16));
            Assert.IsFalse(S0ScreenLayout.CanUseColumns(1015, 600, 400, 16));
        }

        [Test]
        public void TwentyTwoCoincidentMarkersKeepTheirFullLabelsInsideViewportWithoutOverlap()
        {
            var markers = new Vector2[22];
            var sizes = new Vector2[22];
            var visible = new bool[22];
            var output = new Vector2[22];
            for (int i = 0; i < markers.Length; i++)
            {
                markers[i] = new Vector2(300, 200);
                sizes[i] = new Vector2(80, 40);
                visible[i] = true;
            }

            S0ScreenLayout.PlacePitchLabels(new Vector2(600, 400), markers, sizes, visible, output, 8);
            for (int i = 0; i < output.Length; i++)
            {
                Assert.That(output[i].x, Is.InRange(40f, 560f));
                Assert.That(output[i].y, Is.InRange(20f, 380f));
                for (int j = 0; j < i; j++)
                    Assert.IsTrue(Math.Abs(output[i].x - output[j].x) >= 88 || Math.Abs(output[i].y - output[j].y) >= 48);
            }

            Assert.AreEqual(new Vector2(300, 200), markers[0], "world/marker data stays unchanged");
        }

        [Test]
        public void ImpossibleMeasuredViewportFailsRatherThanShrinkingOrDroppingLabels()
        {
            Assert.Throws<InvalidOperationException>(() => S0ScreenLayout.PlacePitchLabels(new Vector2(40, 40), new[] { new Vector2(20, 20), new Vector2(20, 20) }, new[] { new Vector2(30, 30), new Vector2(30, 30) }, new[] { true, true }, new Vector2[2], 8));
        }

        [Test]
        public void AFullLabelLargerThanTheViewportCannotBeReportedAsPlaced()
        {
            Assert.Throws<InvalidOperationException>(() => S0ScreenLayout.PlacePitchLabels(new Vector2(40, 40), new[] { new Vector2(20, 20) }, new[] { new Vector2(41, 20) }, new[] { true }, new Vector2[1], 8));
        }
    }
}
#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-08 | —      | Measured reflow, dense labels, identity-preserving bounds and refusal to truncate. |
#endregion
