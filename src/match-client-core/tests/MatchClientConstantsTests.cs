// File:     src/match-client-core/tests/MatchClientConstantsTests.cs
// Created:  2026-08-04
// Modified: 2026-08-16 (P4b AR round 3, M16: RequireMarkingBandFitsBelowShadowLayer coverage, +
//           the marking-band-vs-shadow-layer invariant in the shipped-values test)
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P4a),
//           Code Standards #20 §3.2.3 ([GT] loading), Testing Strategy #19
// Purpose:  Covers the catalogue's boot-time [GT] validators — the branch a config file reaches and
//           no test process otherwise can, since these constants are static readonly.

using System;

using NUnit.Framework;

using TacticalDirector.MatchClientCore;
using TacticalDirector.MatchViewer;

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
        public void RequireMarkingBandFitsBelowShadowLayer_RefusesABandThatDoesNotClearTheShadowLayerComfortably_M16()
        {
            // Mirrors RequireFarRayMeetsGround's shape: the band's TOP must stay comfortably below
            // the shadow layer (under the midpoint of the clearance), not merely under it, or the
            // highest drawable in the marking band would z-fight with the ball's shadow — reopening
            // the exact M12 hazard one layer up.
            Assert.AreEqual(0.00001f, MatchClientConstants.RequireMarkingBandFitsBelowShadowLayer(
                0.00001f, 27, 0f, 0.001f, "MarkingLayerStepM"), "27 * 0.00001 = 0.00027, well under the 0.0005 midpoint");

            Assert.Throws<InvalidOperationException>(
                () => MatchClientConstants.RequireMarkingBandFitsBelowShadowLayer(0.001f, 27, 0f, 0.001f, "MarkingLayerStepM"),
                "the band reaches the shadow layer outright");
            Assert.Throws<InvalidOperationException>(
                () => MatchClientConstants.RequireMarkingBandFitsBelowShadowLayer(0.00002f, 27, 0f, 0.001f, "MarkingLayerStepM"),
                "27 * 0.00002 = 0.00054 clears the shadow layer but not comfortably — past the 0.0005 midpoint");
        }

        [Test]
        public void RequireTiltOrOffsetNonzero_RefusesOnlyBothZero_L8()
        {
            // Each dial is individually legal at zero — straight-down tilt, or dead-centre framing —
            // and it is the PAIR that is degenerate, in RequireFarRayMeetsGround's own shape.
            Assert.AreEqual(5f, MatchClientConstants.RequireTiltOrOffsetNonzero(5f, 0f), "tilt alone");
            Assert.AreEqual(0f, MatchClientConstants.RequireTiltOrOffsetNonzero(0f, 22f), "offset alone");
            Assert.AreEqual(-5f, MatchClientConstants.RequireTiltOrOffsetNonzero(-5f, 0f), "either sign");

            Assert.Throws<InvalidOperationException>(
                () => MatchClientConstants.RequireTiltOrOffsetNonzero(0f, 0f), "both zero is refused");
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

            Assert.Greater(MatchClientConstants.CameraVerticalFovDegrees, 0f);

            // This line, not the one above it, is the static-init guard — and only this shape of it
            // works. CameraVerticalFovDegrees' initialiser reads CameraTiltDegrees, so a reorder that
            // put the fov first would read the tilt as zero and pass the boot check vacuously (the
            // PerceptionConstants.BASE_FOV_HALF_ANGLE defect). Asserting the tilt is non-zero would
            // NOT catch that: by the time a test runs, static init has finished and the field reads
            // its real value either way. Re-evaluating the invariant on the finished values does
            // catch it, because a pair that is actually invalid fails here whatever happened at boot.
            Assert.Less(
                MatchClientConstants.CameraTiltDegrees + MatchClientConstants.CameraVerticalFovDegrees * 0.5f,
                90f, "the camera's lowest ray must still meet the ground");
            Assert.IsTrue(float.IsFinite(MatchClientConstants.CameraLateralOffsetM));

            // M-2: both tint factors are blend fractions, so [0, 1] is the whole legal range, not
            // just today's shipped values.
            Assert.GreaterOrEqual(MatchClientConstants.GoalkeeperTintFactor, 0f);
            Assert.LessOrEqual(MatchClientConstants.GoalkeeperTintFactor, 1f);
            Assert.GreaterOrEqual(MatchClientConstants.SentOffTintFactor, 0f);
            Assert.LessOrEqual(MatchClientConstants.SentOffTintFactor, 1f);

            // L7: MarkingLineWidthM and GoalMouthWidthM are both drawn widths — the same [0.01, 1] m
            // span, re-evaluated on the finished values rather than assumed from the validator call.
            Assert.GreaterOrEqual(MatchClientConstants.MarkingLineWidthM, 0.01f);
            Assert.LessOrEqual(MatchClientConstants.MarkingLineWidthM, 1f);
            Assert.GreaterOrEqual(MatchClientConstants.GoalMouthWidthM, 0.01f);
            Assert.LessOrEqual(MatchClientConstants.GoalMouthWidthM, 1f);

            // M12: the four ground-layer heights must be STRICTLY ascending — markings lowest, then
            // shadow, then ring, then marker — or the layer they were added to separate collapses
            // back onto its neighbour.
            Assert.Less(MatchClientConstants.MarkingLayerHeightM, MatchClientConstants.BallShadowLayerHeightM);
            Assert.Less(MatchClientConstants.BallShadowLayerHeightM, MatchClientConstants.PossessionRingLayerHeightM);
            Assert.Less(MatchClientConstants.PossessionRingLayerHeightM, MatchClientConstants.AgentMarkerLayerHeightM);

            // M16: the marking BAND (DRAWABLE_COUNT steps of MarkingLayerStepM, starting at
            // MarkingLayerHeightM) must stay comfortably under BallShadowLayerHeightM, re-evaluated on
            // the finished values for the same static-init-order reason the tilt/fov pairing above is.
            float markingBandTopM = MatchClientConstants.MarkingLayerHeightM +
                PitchMarkings.DRAWABLE_COUNT * MatchClientConstants.MarkingLayerStepM;
            float shadowHalfClearanceM = MatchClientConstants.MarkingLayerHeightM +
                (MatchClientConstants.BallShadowLayerHeightM - MatchClientConstants.MarkingLayerHeightM) * 0.5f;
            Assert.Less(markingBandTopM, shadowHalfClearanceM,
                "the highest drawable in the marking band must not approach the ball's shadow layer");

            // L8: at least one of the pair must be non-zero, re-evaluated on the finished values for
            // the same static-init-order reason the tilt/fov pairing above is.
            Assert.IsFalse(
                MatchClientConstants.CameraTiltDegrees == 0f && MatchClientConstants.CameraLateralOffsetM == 0f,
                "tilt and lateral offset cannot both be zero — the camera would sit directly above its target");
        }

        [Test]
        public void RequireStreamerAcceptsSpeed_AdmitsTheCapsOwnBoundaries()
        {
            float minimum = MatchViewerConstants.MinLiveSpeedMultiplier;
            float maximum = MatchViewerConstants.MaxLiveSpeedMultiplier;

            Assert.AreEqual(minimum, MatchClientConstants.RequireStreamerAcceptsSpeed(minimum, "k"));
            Assert.AreEqual(maximum, MatchClientConstants.RequireStreamerAcceptsSpeed(maximum, "k"));
        }

        [Test]
        public void RequireStreamerAcceptsSpeed_RefusesWhatTheStreamerWouldRefuse()
        {
            // Mirrors LiveMatchStreamer.SetSpeedMultiplier's own range test, so the two cannot drift:
            // anything this admits, that method must accept. NaN is included because the !(x >= min)
            // form is what catches it — a naive (x < min) would read NaN as in-range.
            // Bounds are expressed relative to the cap rather than as literals, so the test states the
            // invariant instead of restating today's [GT] values — a retuned cap keeps it meaningful.
            foreach (float bad in new[]
                     {
                         MatchViewerConstants.MinLiveSpeedMultiplier - 1f,
                         MatchViewerConstants.MaxLiveSpeedMultiplier + 1f,
                         float.NaN,
                     })
            {
                Assert.Throws<InvalidOperationException>(
                    () => MatchClientConstants.RequireStreamerAcceptsSpeed(bad, "k"));
            }
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
// | 1.3     | 2026-08-04 | —      | AR pass 3 (L): the v1.2 comment credited the wrong assertion.   |
// |         |            |        | A non-zero tilt assertion does NOT catch a reorder — static     |
// |         |            |        | init has finished by the time a test reads the field, so it     |
// |         |            |        | reads its real value either way. Re-evaluating the invariant    |
// |         |            |        | on the finished values is what catches it. Comment only; the    |
// |         |            |        | assertion that does the work was already there.                 |
// | 1.4     | 2026-08-07 | —      | §5-P5 host-free half: + RequireStreamerAcceptsSpeed, the       |
// |         |            |        | cross-catalogue pairing check that a playback speed is one the  |
// |         |            |        | streamer will accept. Bounds are written relative to the cap    |
// |         |            |        | rather than as literals, so a retune keeps the test meaningful. |
// | 1.5     | 2026-08-15 | —      | P4b AR pass M-2: the shipped-values check gains the two new    |
// |         |            |        | tint factors' [0, 1] bound.                                     |
// | 1.6     | 2026-08-15 | —      | P4b AR round 2, M/L pass: + RequireTiltOrOffsetNonzero_         |
// |         |            |        | RefusesOnlyBothZero_L8 (both-legal-alone, illegal-together, in  |
// |         |            |        | RequireFarRayMeetsGround's own test shape). The shipped-values  |
// |         |            |        | check gains MarkingLineWidthM/GoalMouthWidthM's [0.01, 1] bound,|
// |         |            |        | the four ground-layer heights' strict ascending order, and the  |
// |         |            |        | tilt/lateral-offset not-both-zero re-evaluation.                |
// | 1.7     | 2026-08-16 | —      | P4b AR round 3, M16: +                                          |
// |         |            |        | RequireMarkingBandFitsBelowShadowLayer_                          |
// |         |            |        | RefusesABandThatDoesNotClearTheShadowLayerComfortably_M16, in    |
// |         |            |        | RequireFarRayMeetsGround's own test shape. The shipped-values    |
// |         |            |        | check gains the cross-catalogue invariant that DRAWABLE_COUNT    |
// |         |            |        | steps of MarkingLayerStepM keep the marking band comfortably     |
// |         |            |        | under BallShadowLayerHeightM.                                    |
#endregion
