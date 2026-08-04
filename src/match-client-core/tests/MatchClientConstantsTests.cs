// File:     src/match-client-core/tests/MatchClientConstantsTests.cs
// Created:  2026-08-04
// Modified: 2026-08-04
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P4a),
//           Code Standards #20 §3.2.3 ([GT] loading), Testing Strategy #19
// Purpose:  Covers the catalogue's boot-time [GT] validators — the branch a config file reaches and
//           no test process otherwise can, since these constants are static readonly.

using System;

using NUnit.Framework;

using TacticalDirector.MatchClientCore;

namespace TacticalDirector.MatchClientCore.Tests
{
    [TestFixture]
    public sealed class MatchClientConstantsTests
    {
        // The validators are internal and driven from field initialisers, so a [GT] override can
        // reach them but no test can bind a config without locking GameplayConfigHolder for the
        // whole run. Calling them directly is how the refusal path gets executed at all — without
        // this fixture it is exactly the never-compiled-surface shape this project has paid for.

        [Test]
        public void RequireAtLeast_PassesAValueOnTheBoundaryAndAbove()
        {
            Assert.AreEqual(1f, MatchClientConstants.RequireAtLeast(1f, 1f, "k"));
            Assert.AreEqual(2.5f, MatchClientConstants.RequireAtLeast(2.5f, 1f, "k"));
        }

        [Test]
        public void RequireAtLeast_RefusesBelowTheMinimum_AndAnythingNonFinite()
        {
            // NaN fails every comparison, so the !(value >= minimum) form catches it; a naive
            // (value < minimum) would have let NaN through as "not less than".
            foreach (float bad in new[] { 0.5f, 0f, -1f, float.NaN })
            {
                Assert.Throws<InvalidOperationException>(
                    () => MatchClientConstants.RequireAtLeast(bad, 1f, "CameraHeightM"), "value " + bad);
            }
        }

        [Test]
        public void RequireInRange_RefusesEitherSide_AndNaN()
        {
            Assert.AreEqual(22f, MatchClientConstants.RequireInRange(22f, 0f, 89f, "CameraTiltDegrees"));
            Assert.AreEqual(0f, MatchClientConstants.RequireInRange(0f, 0f, 89f, "k"), "bounds are inclusive");
            Assert.AreEqual(89f, MatchClientConstants.RequireInRange(89f, 0f, 89f, "k"));

            foreach (float bad in new[] { -1f, 90f, float.NaN, float.PositiveInfinity })
            {
                Assert.Throws<InvalidOperationException>(
                    () => MatchClientConstants.RequireInRange(bad, 0f, 89f, "CameraTiltDegrees"), "value " + bad);
            }
        }

        [Test]
        public void RequireGreaterThan_RefusesEquality_NotJustBelow()
        {
            // A ring exactly the size of the marker it annotates is as invisible as a smaller one,
            // so the invariant is strict.
            Assert.AreEqual(1.2f, MatchClientConstants.RequireGreaterThan(1.2f, 0.7f, "ring", "marker"));

            Assert.Throws<InvalidOperationException>(
                () => MatchClientConstants.RequireGreaterThan(0.7f, 0.7f, "ring", "marker"));
            Assert.Throws<InvalidOperationException>(
                () => MatchClientConstants.RequireGreaterThan(0.5f, 0.7f, "ring", "marker"));
            Assert.Throws<InvalidOperationException>(
                () => MatchClientConstants.RequireGreaterThan(float.NaN, 0.7f, "ring", "marker"));
        }

        [Test]
        public void RequireFinite_TakesAnySign_ButRefusesNaNAndInfinity()
        {
            // The lateral offset picks a side, so a range check would be wrong — but "any sign" is
            // not "any value", and this is the distinction.
            Assert.AreEqual(5f, MatchClientConstants.RequireFinite(5f, "k"));
            Assert.AreEqual(-5f, MatchClientConstants.RequireFinite(-5f, "k"), "either side is legal");
            Assert.AreEqual(0f, MatchClientConstants.RequireFinite(0f, "k"), "dead centre is legal");

            foreach (float bad in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                Assert.Throws<InvalidOperationException>(
                    () => MatchClientConstants.RequireFinite(bad, "CameraLateralOffsetM"), "value " + bad);
            }
        }

        [Test]
        public void RequireFarRayMeetsGround_RefusesAPairingThatPutsTheHorizonInShot()
        {
            // Each dial is individually legal here — 80° tilt and 60° fov both pass their own range
            // checks — and together they aim the camera's lowest ray above the horizontal, where it
            // never meets the ground. That is why this is a pairing check.
            Assert.AreEqual(60f, MatchClientConstants.RequireFarRayMeetsGround(60f, 22f));

            Assert.Throws<InvalidOperationException>(
                () => MatchClientConstants.RequireFarRayMeetsGround(60f, 80f));
            Assert.Throws<InvalidOperationException>(
                () => MatchClientConstants.RequireFarRayMeetsGround(120f, 30f), "exactly 90 is refused");
            Assert.Throws<InvalidOperationException>(
                () => MatchClientConstants.RequireFarRayMeetsGround(float.NaN, 22f));
        }

        [Test]
        public void TheMessageNamesTheConfigKey_SoABootFailureIsActionable()
        {
            // It surfaces wrapped in a TypeInitializationException, where the inner message is all
            // the operator gets to work from.
            var ex = Assert.Throws<InvalidOperationException>(
                () => MatchClientConstants.RequireAtLeast(0.5f, 1f, "CameraHeightM"));

            StringAssert.Contains("CameraHeightM", ex.Message);
            StringAssert.Contains("match-client", ex.Message);
        }

        [Test]
        public void TheShippedValuesSatisfyTheInvariantsTheyAreValidatedAgainst()
        {
            Assert.Greater(MatchClientConstants.PossessionRingRadiusM, MatchClientConstants.AgentMarkerRadiusM);
            Assert.GreaterOrEqual(MatchClientConstants.CameraHeightM, 1f);
            Assert.Greater(MatchClientConstants.CameraTiltDegrees, 0f, "a zero tilt is the flat view this replaced");
            Assert.Less(MatchClientConstants.CameraTiltDegrees, 90f, "at 90 the camera is level with the turf");

            // Non-zero matters as much as in-range: the tilt is read during CameraVerticalFovDegrees'
            // own initialiser, and a static readonly field read before its source yields zero — the
            // PerceptionConstants.BASE_FOV_HALF_ANGLE defect, which would make the pairing check
            // below pass vacuously rather than fail.
            Assert.Greater(MatchClientConstants.CameraVerticalFovDegrees, 0f);
            Assert.Less(
                MatchClientConstants.CameraTiltDegrees + MatchClientConstants.CameraVerticalFovDegrees * 0.5f,
                90f, "the camera's lowest ray must still meet the ground");
            Assert.IsTrue(float.IsFinite(MatchClientConstants.CameraLateralOffsetM));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-04 | —      | Initial creation (P4a AR pass, M-4): covers the boot-time [GT]  |
// |         |            |        | validators that replaced HeightScale's silent cap repair. The   |
// |         |            |        | repair branch was untestable, which was half the finding —      |
// |         |            |        | replacing it with an equally untestable guard would have moved  |
// |         |            |        | the problem rather than fixed it.                               |
// | 1.1     | 2026-08-04 | —      | Tilted-view revision: + RequireInRange (the camera tilt's      |
// |         |            |        | bound), and the shipped-values check follows BallMaxHeightScale |
// |         |            |        | out of the catalogue onto the camera rig's dials.              |
// | 1.2     | 2026-08-04 | —      | AR pass 2, H-1/M-3: + RequireFinite (any sign, but not NaN or  |
// |         |            |        | infinity — the lateral offset's check) and                      |
// |         |            |        | RequireFarRayMeetsGround (two individually-legal dials that    |
// |         |            |        | pair into a camera whose lowest ray never meets the ground).   |
// |         |            |        | The shipped-values check also pins the tilt as NON-ZERO: it is |
// |         |            |        | read inside the fov's own initialiser, and a static readonly   |
// |         |            |        | field read before its source yields zero, which would make the |
// |         |            |        | pairing check pass vacuously instead of failing.               |
#endregion
