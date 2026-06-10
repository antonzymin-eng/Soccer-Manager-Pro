// File:     src/testing-strategy/Tests/CrossSpecScenarios.cs
// Created:  2026-06-10
// Modified: 2026-06-10
// Author:   —
// Spec:     Testing Strategy & Framework #19 §3.3.1 / §3.3.5 / Appendix A.1 / KD-8,
//           Pass Mechanics #5 §3.8 / §4.2, Ball Physics #1 §3.1.8 / §3.1.10,
//           Event System #17 §3.2.1, Code Standards #20
// Purpose:  Cross-spec closed-loop scenario corpus (owned by Spec #19 per KD-8;
//           manifest paths live under SCENARIO_PATH_CROSS_SPEC_PREFIX and declare
//           ≥ 2 owning specs per A.1). First chain: a Lofted pass executed by the
//           real PassExecutor (#5) kicks a real BallState through the real
//           BallPhysicsCore loop (#1) — windup → contact → kick → flight → bounce →
//           roll → rest. This is the composition surface where the AR-7 (#1) and
//           AR-2..AR-8 (#5) defect families met: each spec's own suite verified its
//           half of the boundary; only the chained run verifies the handoff
//           (ApplyKick velocity construction → state-machine entry → ground-out).
//           Envelope windows derive from a numerical mirror of the composed model
//           (25 m lofted pass, attributes 14, rested: kick 12.79 m/s at 43.8°,
//           first touch-down 1.6 s after contact, rest at x ≈ 46.5 m after ~6 s).

using System;
using System.Globalization;

using TacticalDirector.BallPhysics;
using TacticalDirector.DeterministicSim;
using TacticalDirector.EventSystem;
using TacticalDirector.PassMechanics;

using UnityEngine;

// Type alias per the EventBusWiringSmokeTests precedent: each spec exports an
// EventBusRegistrar from its own namespace; the alias keeps the boot call site
// unambiguous.
using PassRegistrar = TacticalDirector.PassMechanics.EventBusRegistrar;

namespace TacticalDirector.TestingStrategy.Tests
{
    /// <summary>
    /// Builds the cross-spec scenario index (#19 §3.3.5 cross-spec/ layout; KD-8
    /// ownership). Manifest paths under <see cref="TestingStrategyConstants.SCENARIO_PATH_CROSS_SPEC_PREFIX"/>
    /// declare every spec whose behaviour the envelope asserts — here #1 (Ball
    /// Physics) and #5 (Pass Mechanics). The #17 EventBus is exercised (boot wiring +
    /// CONTACT-time PassAttemptEvent publish + drain) but not asserted: subscribing
    /// here would trip the FR-EVT-020 boot-phase guard whenever an earlier test in
    /// the same process has already drained a tick, so ledger-content assertions stay
    /// with the #17 smoke test. Bodies draw no randomness (the #5 error chain is
    /// deterministic by construction — FM hash, no System.Random), so the recorded
    /// seeds are canonical identifiers per FR-TS-025. Tier classification is B.
    /// </summary>
    internal static class CrossSpecScenarios
    {
        public const string LoftedPassKickBounceRollPath =
            TestingStrategyConstants.SCENARIO_PATH_CROSS_SPEC_PREFIX + "lofted-pass-kick-bounce-roll";

        public const ulong LoftedPassKickBounceRollSeed = 1UL;

        private const float Dt = 1.0f / 60.0f;
        private const int   MaxFrames = 600; // 10 s; mirror settles ~6.2 s incl. windup

        private const int PasserAgentId   = 7;
        private const int ReceiverAgentId = 11;
        private static readonly Vector2 PasserPos   = new Vector2(20.0f, 34.0f);
        private static readonly Vector2 ReceiverPos = new Vector2(45.0f, 34.0f);
        private const float IntendedDistance = 25.0f;

        public static ScenarioIndex BuildIndex()
        {
            var manifest = new ScenarioManifest(
                "lofted-pass-kick-bounce-roll",
                owningSpecIds: new[] { 1, 5 },
                seed: LoftedPassKickBounceRollSeed,
                tierClassification: TestTier.TierB,
                fixtureRefs: Array.Empty<string>(),
                formatVersion: TestingStrategyConstants.SCENARIO_MANIFEST_FORMAT_VERSION);
            return new ScenarioIndex(new[]
            {
                new ScenarioIndexEntry(
                    LoftedPassKickBounceRollPath,
                    manifest,
                    new ClosedLoopScenario(manifest, LoftedPassKickBounceRoll)),
            });
        }

        // Kick → bounce → roll chain across the #5 → #1 boundary. A 25 m Lofted pass
        // from a rested, neutral-attribute passer to a stationary receiver: the
        // executor runs its real WINDUP → CONTACT lifecycle, ApplyKick hands the
        // constructed velocity/spin to Ball Physics, and the ball must fly, bounce,
        // roll, and come to rest near the receiver. Pre-AR-7 H-1/H-2 this chain dies
        // at the first touch-down (no rebound / hover deadlock) even though every
        // per-spec unit suite passes.
        private static void LoftedPassKickBounceRoll(ScenarioContext context)
        {
            // #17 boot wiring: PassAttemptEvent (0x0C) / PassCancelledEvent (0x0D)
            // must be registered before the CONTACT-time publish. Initialize() is
            // idempotent (s_registered guard), so repeated runs in one process are safe.
            PassRegistrar.Initialize();

            // Hermeticity (FR-TS-023): the EventBus is a spec-mandated static
            // singleton; reset per-tick state in case an earlier test in this process
            // left the bus mid-tick, then open the canonical tick lifecycle. The
            // whole scenario runs inside one tick's Resolve phase — the registered
            // producer phase for the #5 Tier A events (#17 §3.10).
            EventBus.OnTickBoundary();
            EventBus.BeginTick(1u);
            EventBus.BeginPhase(PhaseId.Resolve);

            BallState ball = BallState.CreateAtPosition(
                new Vector3(PasserPos.x, PasserPos.y, BallPhysicsConstants.Ball.RADIUS));
            BallCollision.SetBallControlled(ref ball);

            var ballSystem = new ScenarioBallSystem(PasserAgentId);
            var executor   = new PassExecutor(
                ballSystem, new ScenarioAgentQuery(), new ScenarioCollisionQuery());

            var request = new PassRequest
            {
                AgentId          = PasserAgentId,
                PassType         = PassType.Lofted,
                CrossSubType     = CrossSubType.Flat,
                TargetAgentId    = ReceiverAgentId, // Lofted is player-targeted (FM-11)
                TargetPosition   = Vector3.zero,    // unused when TargetAgentId >= 0
                IntendedDistance = IntendedDistance,
                Urgency          = 0.0f,
                IsWeakFoot       = false,
                TeamId           = 0,
                FrameNumber      = 0
            };

            PassResult initiated = executor.Execute(in request);
            context.Envelope.CheckEquals(
                "pass-initiated", (int)initiated.Outcome, (int)PassOutcome.Initiated);

            bool sawBouncing       = false;
            bool airborneAfterKick = false;
            int  contactFrame      = -1;
            int  settleFrame       = -1;

            for (int frame = 1; frame <= MaxFrames; frame++)
            {
                float matchTime = frame * Dt;

                if (!executor.IsIdle)
                {
                    executor.Update(matchTime, frame, ref ball);
                    if (contactFrame < 0 && executor.LastResult.Outcome == PassOutcome.Completed)
                    {
                        contactFrame      = frame;
                        airborneAfterKick = ball.State == BallStateType.Airborne;
                    }
                }

                BallPhysicsCore.UpdateBallPhysics(
                    ref ball, Dt, SurfaceType.GrassDry, Vector3.zero, null, matchTime);

                sawBouncing |= ball.State == BallStateType.Bouncing;
                if (settleFrame < 0 && ball.State == BallStateType.Stationary)
                    settleFrame = frame;
                if (settleFrame > 0 && executor.IsIdle)
                    break;
            }

            // Close the tick: drain the CONTACT-time publish and reset per-tick state
            // so the next scenario (or test) sees a clean bus.
            EventBus.BeginPhase(PhaseId.Events);
            EventBus.DrainTick();
            EventBus.OnTickBoundary();

            context.Envelope.CheckEquals(
                "pass-completed", (int)executor.LastResult.Outcome, (int)PassOutcome.Completed);
            // WINDUP_FRAMES(Lofted) = 15 at Urgency 0; CONTACT executes on the frame
            // after windup expiry (frame 16). Windowed, not pinned, so a [GT] windup
            // retune does not break a cross-spec scenario.
            context.Envelope.CheckInRange(
                "contact-frame", contactFrame, 10.0f, 25.0f);
            // #5 §4.2.1: exactly one ApplyKick per pass execution. The invocation
            // count (not LastKickResult alone) is asserted because KickResult.Applied
            // is ordinal 0 — a never-invoked stub would vacuously equal it.
            context.Envelope.CheckEquals(
                "kick-invoked-exactly-once", ballSystem.KickInvocations, 1);
            context.Envelope.CheckEquals(
                "kick-result-applied", (long)ballSystem.LastKickResult, (long)KickResult.Applied);
            context.Envelope.CheckTrue(
                "airborne-after-kick", airborneAfterKick,
                "a Lofted kick (positive launch angle) must leave the ball Airborne at CONTACT");
            context.Envelope.CheckTrue(
                "entered-bouncing", sawBouncing,
                "the flight must come down through Bouncing (AR-7 H-1/H-2 composed surface)");
            context.Envelope.CheckTrue(
                "settled", settleFrame > 0,
                "ball must reach Stationary within 10 s; final state=" + ball.State);
            // Mirror: contact at 0.27 s, touch-down 1.6 s later, rest at ~6.2 s.
            context.Envelope.CheckInRange(
                "settle-time-s",
                settleFrame > 0 ? settleFrame * Dt : float.PositiveInfinity,
                2.0f, 9.0f);
            // Mirror: rest at x = 46.5 m — the 25 m pass delivers the ball within a
            // couple of metres of the receiver at (45, 34) after bounce + roll-out.
            context.Envelope.CheckInRange(
                "resting-x-m", ball.Position.x, 43.0f, 50.0f);
            // Lateral window bounds the deterministic #5 error chain: error angle for
            // these inputs is ≤ 3.6°, ≤ ~1.7 m of lateral drift over the carry.
            context.Envelope.CheckInRange(
                "resting-y-m", ball.Position.y, 30.0f, 38.0f);
            context.Envelope.CheckTrue(
                "possession-released", !ballSystem.IsBallPossessedBy(PasserAgentId),
                "ApplyKick must release possession (Option B / ERR-008); detail: passer="
                    + PasserAgentId.ToString(CultureInfo.InvariantCulture));
        }

        // ── Boundary stubs ───────────────────────────────────────────────────────────
        // Real Ball Physics behind the #5 IPassBallSystem seam; fixed-value agent and
        // collision queries (no Agent Movement / Collision System behaviour is asserted
        // here, so #2 / #3 are not owning specs).

        private sealed class ScenarioBallSystem : IPassBallSystem
        {
            private readonly int _possessorAgentId;
            private bool _hasPossession = true;

            public KickResult LastKickResult { get; private set; }

            public int KickInvocations { get; private set; }

            public ScenarioBallSystem(int possessorAgentId)
            {
                _possessorAgentId = possessorAgentId;
            }

            public bool IsBallPossessedBy(int agentId)
            {
                return _hasPossession && agentId == _possessorAgentId;
            }

            public void ApplyKick(ref BallState ball, Vector3 velocity, Vector3 spin, int agentId, float matchTime)
            {
                KickInvocations++;
                LastKickResult = BallCollision.ApplyKick(ref ball, velocity, spin, agentId, matchTime);
                if (LastKickResult == KickResult.Applied)
                    _hasPossession = false; // Option B (ERR-008): kick releases possession
            }
        }

        private sealed class ScenarioAgentQuery : IPassAgentQuery
        {
            public PassAgentAttributes GetAttributes(int agentId)
            {
                return new PassAgentAttributes
                {
                    Passing        = 14.0f,
                    Technique      = 14.0f,
                    KickPower      = 14.0f,
                    WeakFootRating = 3,
                    Crossing       = 14.0f,
                    Fatigue        = 0.0f // 0 = fully rested
                };
            }

            public PassAgentState GetState(int agentId)
            {
                if (agentId == ReceiverAgentId)
                {
                    return new PassAgentState
                    {
                        Position        = ReceiverPos,
                        Velocity        = Vector2.zero,
                        FacingDirection = new Vector2(-1.0f, 0.0f)
                    };
                }
                return new PassAgentState
                {
                    Position        = PasserPos,
                    Velocity        = Vector2.zero,
                    // Facing the receiver: body angle 0 keeps the §3.5 orientation
                    // modifier neutral so the lateral window stays tight.
                    FacingDirection = new Vector2(1.0f, 0.0f)
                };
            }
        }

        private sealed class ScenarioCollisionQuery : IPassCollisionQuery
        {
            public bool GetAndClearTackleFlag(int agentId)
            {
                return false; // unopposed pass — no WINDUP interrupt
            }

            public float ComputePressureScalar(Vector2 passerPosition, int passerTeamId)
            {
                return 0.0f; // no nearby defenders
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-10 | —      | Initial implementation. First cross-spec scenario corpus (KD-8): |
// |         |            |        | lofted-pass-kick-bounce-roll chains PassExecutor (#5) into        |
// |         |            |        | BallPhysicsCore (#1) through the IPassBallSystem seam, with #17   |
// |         |            |        | boot wiring + tick lifecycle around the CONTACT publish. Owning   |
// |         |            |        | specs {1, 5}; path under SCENARIO_PATH_CROSS_SPEC_PREFIX.         |
#endregion
