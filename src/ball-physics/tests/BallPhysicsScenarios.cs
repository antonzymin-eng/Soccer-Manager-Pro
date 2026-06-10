// File:     src/ball-physics/tests/BallPhysicsScenarios.cs
// Created:  2026-06-10
// Modified: 2026-06-10
// Author:   —
// Spec:     Ball Physics #1 §3.1.8 / §3.1.10 / §3.1.14,
//           Testing Strategy & Framework #19 §3.3.1 / §3.3.2 / Appendix A.1,
//           Code Standards #20
// Purpose:  Per-spec closed-loop scenario corpus for Spec #1 (owned by #1 per #19
//           §3.3.1 / KD-8). Locks the AR-7 H-1/H-2 closed-loop defect class on the
//           ScenarioRunner: H-1 (ERR-001-001) — the bounce ground normal was Unity
//           Y-up in the Z-up coordinate system, so a falling ball NEVER rebounded;
//           H-2 — the ValidatePhysicsState ground clamp zeroed Velocity.z before the
//           state machine could see vz < 0, trapping fast descents in a permanent
//           ground-level Airborne hover. Both defects were invisible to the
//           pure-function unit suite; only a drop → bounce → settle run composed
//           through UpdateBallPhysics exposes them. Envelope windows below are
//           derived from a numerical mirror of the fixed model (drop from 3 m:
//           first contact 0.75 s, first rebound peak 1.22 m, settle 3.0 s;
//           fast descent 1 m / −6 m/s: first contact 0.12 s, grounded ~2.3 s,
//           settle 5.9 s, resting x ≈ 58.1 m).

using System;
using System.Globalization;

using TacticalDirector.TestingStrategy;

using UnityEngine;

namespace TacticalDirector.BallPhysics.Tests
{
    /// <summary>
    /// Builds the Spec #1 scenario index. Manifest paths follow the #19 §3.3.5 layout;
    /// fixture_refs are empty at Stage 0 because initial state is constructed in code
    /// (canonical fixture files land at Stage 0+1 per #19 KD-10). Scenario bodies draw
    /// no randomness — UpdateBallPhysics is deterministic for a fixed initial state —
    /// so the recorded seeds are canonical identifiers per FR-TS-025, not behaviour
    /// inputs. Tier classification is B (bounded behavioural envelopes per #16 §1.1.1).
    /// </summary>
    internal static class BallPhysicsScenarios
    {
        public const string DropAndReboundPath        = "tests/scenarios/ball-physics/drop-and-rebound";
        public const string FastDescentGroundsOutPath = "tests/scenarios/ball-physics/fast-descent-grounds-out";

        public const ulong DropAndReboundSeed        = 1UL;
        public const ulong FastDescentGroundsOutSeed = 2UL;

        private const float Dt = 1.0f / 60.0f;
        private const float StartX = 50.0f;
        private const float StartY = 34.0f;

        public static ScenarioIndex BuildIndex()
        {
            return new ScenarioIndex(new[]
            {
                Entry(DropAndReboundPath,        "drop-and-rebound",         DropAndReboundSeed,        DropAndRebound),
                Entry(FastDescentGroundsOutPath, "fast-descent-grounds-out", FastDescentGroundsOutSeed, FastDescentGroundsOut),
            });
        }

        private static ScenarioIndexEntry Entry(string manifestPath, string name, ulong seed, ScenarioBody body)
        {
            var manifest = new ScenarioManifest(
                name,
                owningSpecIds: new[] { 1 },
                seed: seed,
                tierClassification: TestTier.TierB,
                fixtureRefs: Array.Empty<string>(),
                formatVersion: TestingStrategyConstants.SCENARIO_MANIFEST_FORMAT_VERSION);
            return new ScenarioIndexEntry(manifestPath, manifest, new ClosedLoopScenario(manifest, body));
        }

        private static BallState CreateAirborneBall(Vector3 position, Vector3 velocity)
        {
            return new BallState
            {
                State             = BallStateType.Airborne,
                Position          = position,
                Velocity          = velocity,
                AngularVelocity   = Vector3.zero,
                LastValidPosition = position,
                LastValidVelocity = velocity
            };
        }

        // PRIMARY AR-7 H-1 lock (ERR-001-001): a ball dropped from rest at 3 m must
        // rebound off the ground and settle. Pre-fix the bounce normal was Unity
        // Vector3.up (+Y, the touchline axis in this Z-up project) — restitution acted
        // on vy = 0, the normal impulse was zero, and a vertically falling ball never
        // rebounded. The rebound-peak window is the load-bearing predicate: pre-fix
        // the peak stays at ground level (~0.11 m) while entered-bouncing still fires
        // (the Airborne → Bouncing transition is independent of the bounce response).
        private static void DropAndRebound(ScenarioContext context)
        {
            BallState ball = CreateAirborneBall(
                new Vector3(StartX, StartY, 3.0f), Vector3.zero);

            int   firstBounceFrame = -1;
            float reboundPeak      = 0.0f;
            int   settleFrame      = -1;

            for (int frame = 1; frame <= 420; frame++) // 7 s cap; mirror settles at 3.0 s
            {
                BallPhysicsCore.UpdateBallPhysics(
                    ref ball, Dt, SurfaceType.GrassDry, Vector3.zero, null, frame * Dt);

                if (firstBounceFrame < 0 && ball.State == BallStateType.Bouncing)
                    firstBounceFrame = frame;
                if (firstBounceFrame > 0 && frame > firstBounceFrame && ball.Position.z > reboundPeak)
                    reboundPeak = ball.Position.z;
                if (ball.State == BallStateType.Stationary)
                {
                    settleFrame = frame;
                    break;
                }
            }

            context.Envelope.CheckTrue(
                "entered-bouncing", firstBounceFrame > 0,
                "a 3 m drop must reach Bouncing; firstBounceFrame="
                    + firstBounceFrame.ToString(CultureInfo.InvariantCulture));
            // Free fall from z = 3.0 m to the 0.13 m exit threshold: 0.75 s in the
            // mirrored model (vacuum 0.77 s; drag delays, the threshold crossing
            // advances).
            context.Envelope.CheckInRange(
                "first-contact-time-s", firstBounceFrame * Dt, 0.6f, 0.9f);
            // e = 0.65 on ~7.4 m/s impact → rebound apex 1.22 m in the mirrored model.
            // Pre-AR-7 H-1 this stays at ~0.11 m (no rebound at all).
            context.Envelope.CheckInRange(
                "first-rebound-peak-m", reboundPeak, 1.0f, 1.45f);
            context.Envelope.CheckTrue(
                "settled", settleFrame > 0,
                "ball must reach Stationary within 7 s; final state="
                    + ball.State + " z=" + ball.Position.z.ToString(CultureInfo.InvariantCulture));
            context.Envelope.CheckInRange(
                "settle-time-s",
                settleFrame > 0 ? settleFrame * Dt : float.PositiveInfinity,
                1.0f, 6.0f);
            // Axis-purity lock for the H-1 defect class: every force in a spinless
            // vertical drop is Z-only, so X/Y must be EXACTLY unchanged — a bounce
            // normal on the wrong axis injects lateral velocity.
            bool lateralUnchanged = ball.Position.x == StartX && ball.Position.y == StartY;
            context.Envelope.CheckTrue(
                "trajectory-pure-vertical", lateralUnchanged,
                "position=(" + ball.Position.x.ToString(CultureInfo.InvariantCulture)
                    + ", " + ball.Position.y.ToString(CultureInfo.InvariantCulture)
                    + ") start=(" + StartX.ToString(CultureInfo.InvariantCulture)
                    + ", " + StartY.ToString(CultureInfo.InvariantCulture) + ")");
        }

        // PRIMARY AR-7 H-2 lock: a descent fast enough to step from above
        // AirborneExitThreshold (0.13 m) to below ground in one 60 Hz frame
        // (|vz| > ~1.2 m/s) must bounce, ground out, and come to rest. Pre-fix the
        // ValidatePhysicsState ground clamp zeroed Velocity.z before the state machine
        // could see vz < 0, so Airborne → Bouncing never fired and the ball glided at
        // z = RADIUS under drag alone — no bounce, no roll, no rest. Extends the
        // BallIntegrationTests.Airborne_FastDescent_Bounces_NoHoverDeadlock unit
        // regression to the full composed settle.
        private static void FastDescentGroundsOut(ScenarioContext context)
        {
            BallState ball = CreateAirborneBall(
                new Vector3(StartX, StartY, 1.0f), new Vector3(5.0f, 0.0f, -6.0f));

            int firstBounceFrame   = -1;
            int firstGroundedFrame = -1;
            int settleFrame        = -1;

            for (int frame = 1; frame <= 480; frame++) // 8 s cap; mirror settles at 5.9 s
            {
                BallPhysicsCore.UpdateBallPhysics(
                    ref ball, Dt, SurfaceType.GrassDry, Vector3.zero, null, frame * Dt);

                if (firstBounceFrame < 0 && ball.State == BallStateType.Bouncing)
                    firstBounceFrame = frame;
                if (firstGroundedFrame < 0
                    && (ball.State == BallStateType.Rolling || ball.State == BallStateType.Stationary))
                    firstGroundedFrame = frame;
                if (ball.State == BallStateType.Stationary)
                {
                    settleFrame = frame;
                    break;
                }
            }

            context.Envelope.CheckTrue(
                "entered-bouncing", firstBounceFrame > 0,
                "fast descent must enter Bouncing (pre-AR-7 H-2: permanent Airborne hover); "
                    + "firstBounceFrame=" + firstBounceFrame.ToString(CultureInfo.InvariantCulture));
            // 0.87 m to the exit threshold at ~6 m/s: 0.12 s in the mirrored model.
            context.Envelope.CheckInRange(
                "first-contact-time-s", firstBounceFrame * Dt, 0.05f, 0.3f);
            // Bounce cascade decays |vz| below BounceVelocityThreshold after ~2.3 s in
            // the mirrored model; 4 s mirrors the unit regression's ground-out bound.
            context.Envelope.CheckInRange(
                "grounded-out-time-s",
                firstGroundedFrame > 0 ? firstGroundedFrame * Dt : float.PositiveInfinity,
                0.5f, 4.0f);
            context.Envelope.CheckTrue(
                "settled", settleFrame > 0,
                "ball must reach Stationary within 8 s; final state="
                    + ball.State + " z=" + ball.Position.z.ToString(CultureInfo.InvariantCulture));
            context.Envelope.CheckInRange(
                "settle-time-s",
                settleFrame > 0 ? settleFrame * Dt : float.PositiveInfinity,
                2.0f, 7.5f);
            // Forward carry: bounce friction + rolling resistance bring the 5 m/s
            // horizontal launch to rest ~8.1 m downfield (mirror: x = 58.1 m).
            context.Envelope.CheckInRange(
                "resting-x-m", ball.Position.x, 55.0f, 61.0f);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-10 | —      | Initial implementation. Spec #1 closed-loop scenario corpus on    |
// |         |            |        | the #19 ScenarioRunner: drop-and-rebound (AR-7 H-1 / ERR-001-001  |
// |         |            |        | lock) + fast-descent-grounds-out (AR-7 H-2 hover-deadlock lock).  |
// |         |            |        | Envelope windows derived from a numerical mirror of the fixed     |
// |         |            |        | model.                                                            |
#endregion
