// File:     src/agent-movement/Tests/AgentMovementScenarios.cs
// Created:  2026-06-10
// Modified: 2026-06-10
// Author:   —
// Spec:     Agent Movement #2 test-plan.md §3.12 (T-AM-110..115),
//           Testing Strategy & Framework #19 §3.3.1 / §3.3.2 / Appendix A.1,
//           Code Standards #20
// Purpose:  Per-spec closed-loop scenario corpus for Spec #2 (owned by #2 per #19
//           §3.3.1 / KD-8). Migrates the AR-12/AR-13 closed-loop locomotion fixture
//           (formerly AgentMovementSystemClosedLoopTests in AgentMovementTests.cs
//           v2.1/v2.2) onto the #19 ScenarioRunner as its first fixture corpus.
//           Requirement IDs T-AM-110..115 are unchanged; the assertions are expressed
//           as expected-outcome-envelope predicates (FR-TS-030). These are the tests
//           the AR-12 H-1/H-2/H-3 family would have failed — every prior test either
//           exercised a pure function or injected mid-flight state.

using System;

using TacticalDirector.TestingStrategy;

using UnityEngine;

namespace TacticalDirector.AgentMovement.Tests
{
    /// <summary>
    /// Builds the Spec #2 scenario index. Manifest paths follow the #19 §3.3.5 layout;
    /// fixture_refs are empty at Stage 0 because initial state is constructed in code
    /// (canonical fixture files land at Stage 0+1 per #19 KD-10). Scenario bodies draw
    /// no randomness — the 12-step pipeline is deterministic for a fixed command — so
    /// the recorded seeds are canonical identifiers per FR-TS-025, not behaviour inputs.
    /// Tier classification is B (bounded behavioural envelopes per #16 §1.1.1).
    /// </summary>
    internal static class AgentMovementScenarios
    {
        public const string LaunchFromRestPath        = "tests/scenarios/agent-movement/launch-from-rest";
        public const string JogBandRespectPath        = "tests/scenarios/agent-movement/jog-band-respect";
        public const string BoundedStopPath           = "tests/scenarios/agent-movement/bounded-stop";
        public const string WalkBandStabilityPath     = "tests/scenarios/agent-movement/walk-band-stability";
        public const string ExhaustedDegradationPath  = "tests/scenarios/agent-movement/exhausted-command-degradation";
        public const string HoldAtOwnPositionPath     = "tests/scenarios/agent-movement/hold-at-own-position";

        public const ulong LaunchFromRestSeed       = 1UL;
        public const ulong JogBandRespectSeed       = 2UL;
        public const ulong BoundedStopSeed          = 3UL;
        public const ulong WalkBandStabilitySeed    = 4UL;
        public const ulong ExhaustedDegradationSeed = 5UL;
        public const ulong HoldAtOwnPositionSeed    = 6UL;

        private const float PhysicsHz = 60.0f;
        private const float Dt = 1.0f / 60.0f;
        private static readonly Vector2 StartPos = new Vector2(20.0f, 34.0f);

        public static ScenarioIndex BuildIndex()
        {
            return new ScenarioIndex(new[]
            {
                Entry(LaunchFromRestPath,       "launch-from-rest",               LaunchFromRestSeed,       LaunchFromRest),
                Entry(JogBandRespectPath,       "jog-band-respect",               JogBandRespectSeed,       JogBandRespect),
                Entry(BoundedStopPath,          "bounded-stop",                   BoundedStopSeed,          BoundedStop),
                Entry(WalkBandStabilityPath,    "walk-band-stability",            WalkBandStabilitySeed,    WalkBandStability),
                Entry(ExhaustedDegradationPath, "exhausted-command-degradation",  ExhaustedDegradationSeed, ExhaustedCommandDegradation),
                Entry(HoldAtOwnPositionPath,    "hold-at-own-position",           HoldAtOwnPositionSeed,    HoldAtOwnPosition),
            });
        }

        private static ScenarioIndexEntry Entry(string manifestPath, string name, ulong seed, ScenarioBody body)
        {
            var manifest = new ScenarioManifest(
                name,
                owningSpecIds: new[] { 2 },
                seed: seed,
                tierClassification: TestTier.TierB,
                fixtureRefs: Array.Empty<string>(),
                formatVersion: TestingStrategyConstants.SCENARIO_MANIFEST_FORMAT_VERSION);
            return new ScenarioIndexEntry(manifestPath, manifest, new ClosedLoopScenario(manifest, body));
        }

        private static float RunFrames(
            AgentMovementSystem sys, ref AgentState state, in MovementCommand cmd,
            int frames, float startTime, System.Action<AgentState> perFrame = null)
        {
            var (attrs, perf) = (PlayerAttributes.CreateDefault(), PerformanceContext.CreateNeutral());
            float t = startTime;
            for (int i = 0; i < frames; i++)
            {
                sys.Update(ref state, attrs, perf, cmd, Dt, t,
                    isCollisionKnockdown: false, collisionForce: 0.0f);
                t += Dt;
                perFrame?.Invoke(state);
            }
            return t;
        }

        // T-AM-110 (PRIMARY AR-12 H-1 lock): an agent created at rest and given MoveTo must
        // launch — pre-fix the IDLE branch only decayed speed while EvaluateFromIdle required
        // speed > IdleExit, deadlocking every agent at speed 0 forever.
        private static void LaunchFromRest(ScenarioContext context)
        {
            var sys = new AgentMovementSystem(PhysicsHz);
            AgentState state = AgentState.CreateAtPosition(StartPos, Vector2.right);
            MovementCommand cmd = MovementCommand.MoveTo(new Vector2(67.5f, 34.0f)); // 47.5 m, +X

            RunFrames(sys, ref state, cmd, frames: 180, startTime: 0.0f); // 3 s

            context.Envelope.CheckEquals(
                "final-state-jogging", (int)state.CurrentState, (int)AgentMovementState.JOGGING);
            context.Envelope.CheckTrue(
                "speed-above-jog-enter", state.Speed > MovementThresholds.JogEnter,
                "speed=" + state.Speed + " jogEnter=" + MovementThresholds.JogEnter);
            context.Envelope.CheckTrue(
                "travelled-toward-target", state.Position.x > StartPos.x + 5.0f,
                "x=" + state.Position.x + " startX=" + StartPos.x);
        }

        // T-AM-111 (PRIMARY AR-12 H-2 lock): a jog command must never promote to SPRINTING —
        // pre-fix topSpeed ignored commandSpeed, the agent accelerated through SprintEnter,
        // drained the sprint reservoir, and flap-cycled via DECELERATING.
        private static void JogBandRespect(ScenarioContext context)
        {
            var sys = new AgentMovementSystem(PhysicsHz);
            AgentState state = AgentState.CreateAtPosition(StartPos, Vector2.right);
            MovementCommand cmd = MovementCommand.MoveTo(new Vector2(67.5f, 34.0f));

            bool everSprinted = false;
            bool everDecelerated = false;
            RunFrames(sys, ref state, cmd, frames: 480, startTime: 0.0f, s => // 8 s
            {
                everSprinted |= s.CurrentState == AgentMovementState.SPRINTING;
                everDecelerated |= s.CurrentState == AgentMovementState.DECELERATING;
            });

            context.Envelope.CheckTrue("never-sprinting", !everSprinted,
                "a jog command must never enter SPRINTING (pre-AR-12 H-2 auto-promotion)");
            context.Envelope.CheckTrue("never-decelerating", !everDecelerated,
                "a jog command at steady state must not flap through DECELERATING");
            context.Envelope.CheckInRange(
                "speed-within-band-ceiling", state.Speed,
                0.0f, MovementThresholds.SprintEnter + 0.001f);
        }

        // T-AM-112 (PRIMARY AR-12 H-3 lock): a Stop command from jogging speed must reach
        // IDLE within bounded time and distance — pre-fix the Zeno deceleration tail took
        // ~78 s and ~32 m before crossing IdleEnter.
        private static void BoundedStop(ScenarioContext context)
        {
            var sys = new AgentMovementSystem(PhysicsHz);
            AgentState state = AgentState.CreateAtPosition(StartPos, Vector2.right);
            state.CurrentState = AgentMovementState.JOGGING;
            state.Velocity = new Vector2(5.5f, 0.0f);
            state.Speed = 5.5f;
            state.LastValidVelocity = state.Velocity;
            MovementCommand cmd = MovementCommand.Stop(state.Position);

            RunFrames(sys, ref state, cmd, frames: 180, startTime: 0.0f); // 3 s

            context.Envelope.CheckEquals(
                "final-state-idle", (int)state.CurrentState, (int)AgentMovementState.IDLE);
            context.Envelope.CheckTrue(
                "stopping-distance-bounded", Vector2.Distance(state.Position, StartPos) < 8.0f,
                "distance=" + Vector2.Distance(state.Position, StartPos) + " bound=8.0 (pre-AR-12 H-3: ~32 m)");
        }

        // T-AM-113 (AR-12 H-2 walking band): a WalkTo command settles in WALKING at the
        // JogEnter ceiling and never escalates — pre-fix it flapped WALKING→JOGGING→DECELERATING.
        private static void WalkBandStability(ScenarioContext context)
        {
            var sys = new AgentMovementSystem(PhysicsHz);
            AgentState state = AgentState.CreateAtPosition(StartPos, Vector2.right);
            MovementCommand cmd = MovementCommand.WalkTo(new Vector2(67.5f, 34.0f));

            bool everAboveWalking = false;
            RunFrames(sys, ref state, cmd, frames: 300, startTime: 0.0f, s => // 5 s
            {
                everAboveWalking |= s.CurrentState == AgentMovementState.JOGGING
                                 || s.CurrentState == AgentMovementState.SPRINTING
                                 || s.CurrentState == AgentMovementState.DECELERATING;
            });

            context.Envelope.CheckTrue("never-above-walking-band", !everAboveWalking,
                "a walk command must never escalate above the walking band (pre-AR-12 H-2 flap)");
            context.Envelope.CheckEquals(
                "final-state-walking", (int)state.CurrentState, (int)AgentMovementState.WALKING);
            context.Envelope.CheckInRange(
                "speed-within-walk-band", state.Speed,
                0.0f, MovementThresholds.JogEnter + 0.001f);
            context.Envelope.CheckTrue(
                "speed-above-idle-exit", state.Speed > MovementThresholds.IdleExit,
                "the agent must actually be walking, not crawling near the IDLE boundary; speed=" + state.Speed);
        }

        // T-AM-114 (AR-13 M-1): an aerobically exhausted agent given a jog command settles
        // in the walking band — pre-fix the state machine refused JOGGING on the aerobic
        // gate while the un-degraded commandSpeed kept pushing speed through JogEnter,
        // flapping WALKING→JOGGING→DECELERATING at ~3 Hz until the OscillationGuard locked.
        private static void ExhaustedCommandDegradation(ScenarioContext context)
        {
            var sys = new AgentMovementSystem(PhysicsHz);
            AgentState state = AgentState.CreateAtPosition(StartPos, Vector2.right);
            state.AerobicPool = 0.05f; // below AerobicJogFloor (0.15)
            MovementCommand cmd = MovementCommand.MoveTo(new Vector2(67.5f, 34.0f));

            bool everAboveWalking = false;
            RunFrames(sys, ref state, cmd, frames: 300, startTime: 0.0f, s => // 5 s
            {
                everAboveWalking |= s.CurrentState == AgentMovementState.JOGGING
                                 || s.CurrentState == AgentMovementState.SPRINTING
                                 || s.CurrentState == AgentMovementState.DECELERATING;
            });

            context.Envelope.CheckTrue("never-above-walking-band", !everAboveWalking,
                "an exhausted agent's jog command must degrade to the walking band, not flap (AR-13 M-1)");
            context.Envelope.CheckEquals(
                "final-state-walking", (int)state.CurrentState, (int)AgentMovementState.WALKING);
            context.Envelope.CheckInRange(
                "speed-within-walk-band", state.Speed,
                0.0f, MovementThresholds.JogEnter + 0.001f);
        }

        // T-AM-115 (AR-13 M-2): the Decision Tree HOLD action (StrafeWhileWatching with
        // target == current position, desired JOGGING) must keep a resting agent at rest —
        // without the movement-intent offset gate, the AR-12 H-1 launch path fed newSpeed > 0
        // into Step 8 with a degenerate target, tripping RotateVelocityToward's
        // both-degenerate Debug.Assert every frame.
        private static void HoldAtOwnPosition(ScenarioContext context)
        {
            var sys = new AgentMovementSystem(PhysicsHz);
            AgentState state = AgentState.CreateAtPosition(StartPos, Vector2.right);
            Vector2 ballPos = new Vector2(40.0f, 34.0f);
            MovementCommand cmd = MovementCommand.StrafeWhileWatching(StartPos, ballPos);

            RunFrames(sys, ref state, cmd, frames: 120, startTime: 0.0f); // 2 s

            context.Envelope.CheckEquals(
                "final-state-idle", (int)state.CurrentState, (int)AgentMovementState.IDLE);
            context.Envelope.CheckInRange("speed-at-rest", state.Speed, -0.0001f, 0.0001f);
            context.Envelope.CheckTrue("position-unchanged", state.Position == StartPos,
                "position=" + state.Position + " start=" + StartPos);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-10 | —      | T-AM-110..115 migrated from AgentMovementSystemClosedLoopTests     |
// |         |            |        | (AgentMovementTests.cs v2.1/v2.2) onto the #19 ScenarioRunner as   |
// |         |            |        | its first per-spec closed-loop fixture corpus. Assertions are      |
// |         |            |        | unchanged in substance, re-expressed as envelope predicates.       |
#endregion
