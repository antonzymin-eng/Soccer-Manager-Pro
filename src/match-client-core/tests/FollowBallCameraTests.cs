// File:     src/match-client-core/tests/FollowBallCameraTests.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Interactive Unity client §5-P3 / §7 "Rendering, camera, HUD", Code Standards #20
// Purpose:  Locks the three camera behaviours that are easy to get subtly wrong — the dead zone,
//           frame-rate-independent smoothing, and a pitch clamp that stays well-defined when the
//           view is wider than the pitch.

using NUnit.Framework;

using UnityEngine;

using TacticalDirector.MatchEngine;

namespace TacticalDirector.MatchClientCore.Tests
{
    /// <summary>P3 follow-ball camera tests (see file header).</summary>
    [TestFixture]
    public sealed class FollowBallCameraTests
    {
        private static readonly Vector2 Centre =
            new Vector2(MatchEngineConstants.PITCH_LENGTH_M * 0.5f, MatchEngineConstants.PITCH_WIDTH_M * 0.5f);

        // ── Dead zone ───────────────────────────────────────────────────────────────────────────

        [Test]
        public void DeadZone_BallJostlingNearTheTargetDoesNotMoveTheCamera()
        {
            float inside = MatchClientConstants.CameraDeadZoneM * 0.5f;
            Vector2 desired = FollowBallCamera.DesiredTarget(Centre, Centre + new Vector2(inside, 0f));

            Assert.AreEqual(Centre.x, desired.x, 1e-4f);
            Assert.AreEqual(Centre.y, desired.y, 1e-4f);
        }

        [Test]
        public void DeadZone_OnceBreached_TheTargetTrailsTheBallByExactlyTheDeadZone()
        {
            float dead = MatchClientConstants.CameraDeadZoneM;
            Vector2 ball = Centre + new Vector2(dead + 6f, 0f);

            Vector2 desired = FollowBallCamera.DesiredTarget(Centre, ball);

            Assert.AreEqual(
                dead, (ball - desired).magnitude, 1e-3f,
                "The camera settles with the ball on the dead-zone edge — trailing it, not centring it.");
        }

        // ── Smoothing ───────────────────────────────────────────────────────────────────────────

        [Test]
        public void Smoothing_IsFrameRateIndependent()
        {
            // The property that matters: covering one 1/30 s step must land in the same place as two
            // 1/60 s steps. A naive per-frame lerp fails this by a wide margin.
            Vector2 ball = Centre + new Vector2(20f, 0f);

            Vector2 oneBigStep = FollowBallCamera.ComputeTarget(Centre, ball, 1f / 30f);

            Vector2 twoSmall = FollowBallCamera.ComputeTarget(Centre, ball, 1f / 60f);
            twoSmall = FollowBallCamera.ComputeTarget(twoSmall, ball, 1f / 60f);

            Assert.AreEqual(oneBigStep.x, twoSmall.x, 0.05f,
                "e^(-k*2dt) == (e^(-k*dt))^2 — the same ground covered per second at any frame rate.");
        }

        [Test]
        public void Smoothing_MovesTowardTheBall_ButNotAllTheWayInOneFrame()
        {
            Vector2 ball = Centre + new Vector2(20f, 0f);
            Vector2 next = FollowBallCamera.ComputeTarget(Centre, ball, 1f / 60f);

            Assert.Greater(next.x, Centre.x, "It follows.");
            Assert.Less(next.x, FollowBallCamera.DesiredTarget(Centre, ball).x, "It does not snap.");
        }

        [Test]
        public void Smoothing_ConvergesOnTheDesiredTarget()
        {
            Vector2 ball = Centre + new Vector2(20f, 0f);
            Vector2 target = Centre;
            for (int i = 0; i < 600; i++)
            {
                target = FollowBallCamera.ComputeTarget(target, ball, 1f / 60f);
            }

            Assert.AreEqual(
                MatchClientConstants.CameraDeadZoneM, (ball - target).magnitude, 0.1f,
                "Ten seconds in, the camera has settled on the dead-zone edge.");
        }

        // ── Clamp ───────────────────────────────────────────────────────────────────────────────

        [Test]
        public void Clamp_KeepsTheViewOnThePitchAtACorner()
        {
            Vector2 clamped = FollowBallCamera.Clamp(new Vector2(-50f, -50f));

            Assert.AreEqual(
                MatchClientConstants.CameraViewHalfWidthM - MatchClientConstants.CameraOverscanM,
                clamped.x, 1e-3f);
            Assert.AreEqual(
                MatchClientConstants.CameraViewHalfHeightM - MatchClientConstants.CameraOverscanM,
                clamped.y, 1e-3f);
        }

        [Test]
        public void Clamp_IsSymmetricAtTheFarCorner()
        {
            Vector2 clamped = FollowBallCamera.Clamp(new Vector2(9_999f, 9_999f));

            Assert.AreEqual(
                MatchEngineConstants.PITCH_LENGTH_M - MatchClientConstants.CameraViewHalfWidthM
                    + MatchClientConstants.CameraOverscanM,
                clamped.x, 1e-3f);
            Assert.AreEqual(
                MatchEngineConstants.PITCH_WIDTH_M - MatchClientConstants.CameraViewHalfHeightM
                    + MatchClientConstants.CameraOverscanM,
                clamped.y, 1e-3f);
        }

        [Test]
        public void Clamp_LeavesAnInteriorTargetAlone()
        {
            Vector2 clamped = FollowBallCamera.Clamp(Centre);

            Assert.AreEqual(Centre.x, clamped.x, 1e-4f);
            Assert.AreEqual(Centre.y, clamped.y, 1e-4f);
        }

        [Test]
        public void Clamp_ViewWiderThanThePitch_CentresRatherThanOscillating()
        {
            // Zoomed out past the pitch, the min bound exceeds the max: there is no satisfying
            // position. Mathf.Clamp with a crossed range returns whichever bound it compares last,
            // which flips as the target drifts — the camera would visibly twitch.
            Vector2 clamped = FollowBallCamera.Clamp(new Vector2(10f, 5f));

            // Only assertable when the shipped view is actually wider than the pitch; otherwise this
            // asserts the ordinary interior behaviour, so state which case ran.
            bool widerThanPitch =
                MatchClientConstants.CameraViewHalfWidthM * 2f > MatchEngineConstants.PITCH_LENGTH_M;

            if (widerThanPitch)
            {
                Assert.AreEqual(MatchEngineConstants.PITCH_LENGTH_M * 0.5f, clamped.x, 1e-3f);
            }
            else
            {
                Assert.GreaterOrEqual(
                    clamped.x,
                    MatchClientConstants.CameraViewHalfWidthM - MatchClientConstants.CameraOverscanM - 1e-3f);
            }
        }

        // ── Degenerate inputs ───────────────────────────────────────────────────────────────────

        [Test]
        public void NonFiniteBall_HoldsTheCameraStill()
        {
            Vector2 next = FollowBallCamera.ComputeTarget(Centre, new Vector2(float.NaN, 34f), 1f / 60f);

            Assert.AreEqual(Centre.x, next.x, 1e-4f, "A NaN ball must not become a NaN camera.");
            Assert.AreEqual(Centre.y, next.y, 1e-4f);
        }

        [Test]
        public void NonPositiveDelta_HoldsTheCameraStill()
        {
            Vector2 ball = Centre + new Vector2(30f, 0f);

            Assert.AreEqual(Centre.x, FollowBallCamera.ComputeTarget(Centre, ball, 0f).x, 1e-4f);
            Assert.AreEqual(Centre.x, FollowBallCamera.ComputeTarget(Centre, ball, -1f).x, 1e-4f);
            Assert.AreEqual(Centre.x, FollowBallCamera.ComputeTarget(Centre, ball, float.NaN).x, 1e-4f);
        }

        [Test]
        public void NonFiniteCurrentTarget_RecoversToTheCentreCircle()
        {
            Vector2 next = FollowBallCamera.ComputeTarget(
                new Vector2(float.NaN, float.NaN), Centre, 1f / 60f);

            Assert.IsTrue(float.IsFinite(next.x) && float.IsFinite(next.y),
                "A corrupted target must recover, not propagate.");
        }

        [Test]
        public void EveryOutput_IsWithinTheClampedRange()
        {
            // Sweep the ball across the whole pitch and beyond it; the camera must never leave bounds.
            Vector2 target = Centre;
            for (float x = -20f; x < MatchEngineConstants.PITCH_LENGTH_M + 20f; x += 3f)
            {
                target = FollowBallCamera.ComputeTarget(target, new Vector2(x, 34f), 1f / 60f);

                Assert.AreEqual(FollowBallCamera.Clamp(target).x, target.x, 1e-4f,
                    "ComputeTarget always returns an already-clamped point.");
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-27 | —      | Initial creation (P3): dead-zone trailing, frame-rate          |
// |         |            |        | independence proven by step-subdivision, clamp corners and the |
// |         |            |        | wider-than-pitch case, and every degenerate input.             |
#endregion
