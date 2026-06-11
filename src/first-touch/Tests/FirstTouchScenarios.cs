// File:     src/first-touch/Tests/FirstTouchScenarios.cs
// Created:  2026-06-10
// Modified: 2026-06-10
// Author:   —
// Spec:     First Touch Mechanics #4 §3.3 / §3.4 / §3.5,
//           Testing Strategy & Framework #19 §3.3.1 / §3.3.2 / Appendix A.1,
//           Code Standards #20
// Purpose:  Per-spec closed-loop scenario corpus for Spec #4 (owned by #4 per #19
//           §3.3.1 / KD-8). Locks the AR-7 closed-loop defect class on the
//           ScenarioRunner: H-1 (ERR-004-003) — the §3.3.2 direction blend negated
//           ball velocity, so a heavy touch displaced the ball back toward the passer
//           AGAINST its own §3.3.5 retained momentum; M-1 (ERR-004-004) — the
//           interception gate was agent-anchored and PressureRadius-truncated, so the
//           §3.4.2 ball-anchored geometry could never fire for opponents beyond 3 m
//           of the agent; M-3 — the §3.4.5 interception velocity redirect was never
//           coded, breaking the Frame N+1 contact chain. All three were invisible to
//           (or actively mis-encoded by) the pure-function unit suite; only an
//           Evaluate → Apply → next-touch run composed through the public pipeline
//           and the real PressureEvaluator producer exposes them. Envelope windows
//           are derived from a numerical mirror of the fixed model (heavy toucher
//           T5/F5 vs 25 m/s ball: q = 0.150, r = 1.933, displaced x = 50.57,
//           |v| = 10.625; Frame N+1 interceptor T14/F12: q = 0.946, r = 0.172).

using System;
using System.Globalization;

using UnityEngine;

using TacticalDirector.TestingStrategy;

namespace TacticalDirector.FirstTouch.Tests
{
    /// <summary>
    /// Builds the Spec #4 scenario index. Manifest paths follow the #19 §3.3.5 layout;
    /// fixture_refs are empty at Stage 0 (initial state constructed in code; canonical
    /// fixture files land at Stage 0+1 per #19 KD-10). Scenario bodies draw no
    /// randomness — EvaluateFirstTouch is deterministic for a fixed context — so the
    /// recorded seeds are canonical identifiers per FR-TS-025, not behaviour inputs.
    /// Tier classification is B (bounded behavioural envelopes per #16 §1.1.1).
    /// </summary>
    internal static class FirstTouchScenarios
    {
        public const string HeavyTouchRunsOnPath = "tests/scenarios/first-touch/heavy-touch-runs-on";
        public const string InterceptionChainPath = "tests/scenarios/first-touch/interception-chain-anchors-at-displaced-ball";

        public const ulong HeavyTouchRunsOnSeed  = 1UL;
        public const ulong InterceptionChainSeed = 2UL;

        private static readonly Vector3 AgentPos = new Vector3(52.5f, 34.0f, 0.0f);

        public static ScenarioIndex BuildIndex()
        {
            return new ScenarioIndex(new[]
            {
                Entry(HeavyTouchRunsOnPath, "heavy-touch-runs-on",
                    HeavyTouchRunsOnSeed, HeavyTouchRunsOn),
                Entry(InterceptionChainPath, "interception-chain-anchors-at-displaced-ball",
                    InterceptionChainSeed, InterceptionChainAnchorsAtDisplacedBall),
            });
        }

        private static ScenarioIndexEntry Entry(string manifestPath, string name, ulong seed, ScenarioBody body)
        {
            var manifest = new ScenarioManifest(
                name,
                owningSpecIds: new[] { 4 },
                seed: seed,
                tierClassification: TestTier.TierB,
                fixtureRefs: Array.Empty<string>(),
                formatVersion: TestingStrategyConstants.SCENARIO_MANIFEST_FORMAT_VERSION);
            return new ScenarioIndexEntry(manifestPath, manifest, new ClosedLoopScenario(manifest, body));
        }

        /// <summary>Heavy receiver context: T5/F5 at rest, 25 m/s ball travelling -X, +Y intent.</summary>
        private static FirstTouchContext CreateHeavyTouchContext(int agentId)
        {
            return new FirstTouchContext
            {
                AgentID                 = agentId,
                TeamID                  = 0,
                Technique               = 5,
                FirstTouchAttribute     = 5,
                AgentPosition           = AgentPos,
                AgentVelocity           = Vector3.zero,
                AgentFacing             = new Vector3(0.0f, 1.0f, 0.0f),
                IntendedTouchDirection  = new Vector3(0.0f, 1.0f, 0.0f),
                HasMovementTarget       = true,
                BallPosition            = new Vector3(AgentPos.x, AgentPos.y, 0.11f),
                BallVelocity            = new Vector3(-25.0f, 0.0f, 0.0f),
                BallHeight              = 0.0f,
                BallIsAirborne          = false,
                PressureScalar          = 0.0f,
                HasNearbyOpponent       = false,
                NearestOpponentDistance = float.PositiveInfinity,
                NearestOpponentPositionXY = Vector2.zero,
                IsHalfTurnOriented      = false,
                IsGoalkeeper            = false
            };
        }

        // PRIMARY AR-7 H-1 lock (ERR-004-003): a heavy touch must run ON PAST the
        // receiver along the ball's travel path, with displacement and velocity
        // pointing the same way. Pre-fix the §3.3.2 blend negated ball velocity:
        // the ball was positioned up to 2.0 m BACK toward the passer (dx > 0 here)
        // while §3.3.5 momentum retention kept the velocity forward — the
        // displacement-velocity dot product was negative and the ball travelled back
        // through the receiving agent on the next frame. The coherence predicate is
        // the load-bearing one; the dx sign localises the defect.
        private static void HeavyTouchRunsOn(ScenarioContext context)
        {
            var ball = new RecordingBallPhysicsSystem();
            var movement = new RecordingAgentMovementSystem();
            var system = new FirstTouchSystem(ball, movement);

            FirstTouchContext ctx = CreateHeavyTouchContext(agentId: 7);

            FirstTouchResult result = system.EvaluateFirstTouch(ctx);
            system.ApplyTouchResult(result, ctx);

            // Mirror: q = 0.150 (heavy band), r = 1.933 (velocity modifier 1.1667).
            context.Envelope.CheckInRange("control-quality", result.ControlQuality, 0.10f, 0.20f);
            context.Envelope.CheckInRange("touch-radius-m", result.TouchRadius, 1.8f, 2.0f);

            float dx = result.NewBallPosition.x - ctx.AgentPosition.x;
            context.Envelope.CheckTrue(
                "displacement-downstream", dx < 0.0f,
                "heavy touch must continue along travel (-X); dx="
                    + dx.ToString(CultureInfo.InvariantCulture));

            Vector2 dispXY = new Vector2(
                result.NewBallPosition.x - ctx.AgentPosition.x,
                result.NewBallPosition.y - ctx.AgentPosition.y);
            Vector2 velXY = new Vector2(result.NewBallVelocity.x, result.NewBallVelocity.y);
            float coherence = Vector2.Dot(dispXY, velXY);
            context.Envelope.CheckTrue(
                "displacement-velocity-coherent", coherence > 0.0f,
                "displacement and velocity must point the same way (pre-ERR-004-003: opposed); dot="
                    + coherence.ToString(CultureInfo.InvariantCulture));

            // r = 1.933 ≥ 1.50 with retention-dominated alignment → DEFLECTION (ERR-004-005).
            context.Envelope.CheckEquals(
                "outcome-ordinal", (long)result.PossessionOutcome, (long)TouchResult.Deflection);

            // Apply surface: the exact evaluated state must reach Ball Physics, and a
            // non-CONTROLLED outcome must clear dribbling for the touching agent.
            // Exact component equality, not Vector3.operator== (approximate, ~1e-5):
            // the contract is verbatim forwarding (#19 AR-1 L-3 / T-AM-115 precedent).
            bool forwardedVerbatim =
                ball.LastPosition.x == result.NewBallPosition.x
                && ball.LastPosition.y == result.NewBallPosition.y
                && ball.LastPosition.z == result.NewBallPosition.z
                && ball.LastVelocity.x == result.NewBallVelocity.x
                && ball.LastVelocity.y == result.NewBallVelocity.y
                && ball.LastVelocity.z == result.NewBallVelocity.z;
            context.Envelope.CheckTrue(
                "applied-ball-state-matches-result", forwardedVerbatim,
                "ApplyTouchResult must forward the evaluated ball state verbatim");
            context.Envelope.CheckTrue(
                "dribbling-cleared",
                movement.LastAgentID == ctx.AgentID && !movement.LastIsDribbling,
                "non-CONTROLLED outcome must clear dribbling for the toucher; agent="
                    + movement.LastAgentID.ToString(CultureInfo.InvariantCulture));
        }

        // PRIMARY AR-7 M-1 lock (ERR-004-004) + M-3 (§3.4.5 redirect) + §3.4.5
        // Frame N+1 chain. The opponent stands 3.3 m from the receiving agent —
        // beyond BOTH the old agent-anchored 2.5 m gate and the 3.0 m PressureRadius
        // truncation (pre-fix: invisible, NearestOpponentDistance read +inf) — but
        // only 1.37 m from the DISPLACED ball, so the §3.4.2 ball-anchored gate must
        // fire. The context's opponent fields are produced by the REAL
        // PressureEvaluator (the producer half of the ERR-004-004 fix). The redirect
        // then points the ball at the interceptor, and the Frame N+1 touch by the
        // interceptor (T14/F12 vs the 10.6 m/s redirected ball) achieves CONTROLLED
        // per §3.4.5 ("Intercepting agent may achieve CONTROLLED").
        private static void InterceptionChainAnchorsAtDisplacedBall(ScenarioContext context)
        {
            var ball = new RecordingBallPhysicsSystem();
            var movement = new RecordingAgentMovementSystem();
            var system = new FirstTouchSystem(ball, movement);

            Vector2 opponentXY = new Vector2(49.2f, 34.0f);

            // Frame N — heavy touch under the real pressure producer.
            FirstTouchContext ctx = CreateHeavyTouchContext(agentId: 7);
            PressureResult pressure = PressureEvaluator.Evaluate(
                new Vector2(ctx.AgentPosition.x, ctx.AgentPosition.y),
                new[] { opponentXY });
            ctx.PressureScalar            = pressure.PressureScalar;
            ctx.HasNearbyOpponent         = pressure.HasNearbyOpponent;
            ctx.NearestOpponentDistance   = pressure.NearestOpponentDistance;
            ctx.NearestOpponentPositionXY = pressure.NearestOpponentPositionXY;

            // The opponent contributes no pressure (outside 3.0 m) yet is still the
            // tracked nearest — the post-fix producer contract.
            context.Envelope.CheckInRange("pressure-scalar", pressure.PressureScalar, 0.0f, 0.0001f);
            context.Envelope.CheckInRange("nearest-opponent-distance-m", pressure.NearestOpponentDistance, 3.25f, 3.35f);

            FirstTouchResult result = system.EvaluateFirstTouch(ctx);
            system.ApplyTouchResult(result, ctx);

            // Pre-fix impossibility proof: the agent-anchored distance exceeds the
            // interception radius, so the old gate could never classify this touch.
            context.Envelope.CheckTrue(
                "agent-anchored-gate-would-miss",
                ctx.NearestOpponentDistance > FirstTouchConstants.InterceptionRadius,
                "scenario geometry requires opponent beyond the agent-anchored radius; dist="
                    + ctx.NearestOpponentDistance.ToString(CultureInfo.InvariantCulture));
            context.Envelope.CheckEquals(
                "outcome-ordinal", (long)result.PossessionOutcome, (long)TouchResult.Interception);

            // §3.4.5 redirect: velocity points at the interceptor (alignment ≈ 1).
            Vector2 toOpponent = opponentXY
                - new Vector2(result.NewBallPosition.x, result.NewBallPosition.y);
            Vector2 velXY = new Vector2(result.NewBallVelocity.x, result.NewBallVelocity.y);
            float alignment = Vector2.Dot(velXY.normalized, toOpponent.normalized);
            context.Envelope.CheckInRange("redirect-alignment", alignment, 0.99f, 1.01f);

            // Frame N+1 — interceptor's own first touch on the redirected ball.
            FirstTouchContext interceptorCtx = new FirstTouchContext
            {
                AgentID                 = 15,
                TeamID                  = 1,
                Technique               = 14,
                FirstTouchAttribute     = 12,
                AgentPosition           = new Vector3(opponentXY.x, opponentXY.y, 0.0f),
                AgentVelocity           = Vector3.zero,
                AgentFacing             = new Vector3(1.0f, 0.0f, 0.0f),
                IntendedTouchDirection  = new Vector3(1.0f, 0.0f, 0.0f),
                HasMovementTarget       = true,
                BallPosition            = result.NewBallPosition,
                BallVelocity            = result.NewBallVelocity,
                BallHeight              = 0.0f,
                BallIsAirborne          = false,
                PressureScalar          = 0.0f,
                HasNearbyOpponent       = false,
                NearestOpponentDistance = float.PositiveInfinity,
                NearestOpponentPositionXY = Vector2.zero,
                IsHalfTurnOriented      = false,
                IsGoalkeeper            = false
            };

            FirstTouchResult chain = system.EvaluateFirstTouch(interceptorCtx);
            system.ApplyTouchResult(chain, interceptorCtx);

            // Mirror: interceptor q = 0.946 (Perfect band), r = 0.172 → CONTROLLED.
            context.Envelope.CheckInRange("interceptor-quality", chain.ControlQuality, 0.90f, 0.98f);
            context.Envelope.CheckEquals(
                "interceptor-outcome-ordinal", (long)chain.PossessionOutcome, (long)TouchResult.Controlled);
            context.Envelope.CheckTrue(
                "interceptor-gains-possession",
                chain.PossessingAgentID == interceptorCtx.AgentID
                    && movement.LastAgentID == interceptorCtx.AgentID
                    && movement.LastIsDribbling,
                "Frame N+1 CONTROLLED must assign possession + dribbling to the interceptor");
        }

        /// <summary>Recording stub for the IBallPhysicsSystem write seam.</summary>
        private sealed class RecordingBallPhysicsSystem : IBallPhysicsSystem
        {
            public Vector3 LastPosition;
            public Vector3 LastVelocity;

            public void SetBallState(Vector3 newPosition, Vector3 newVelocity)
            {
                LastPosition = newPosition;
                LastVelocity = newVelocity;
            }
        }

        /// <summary>Recording stub for the IAgentMovementSystem write seam.</summary>
        private sealed class RecordingAgentMovementSystem : IAgentMovementSystem
        {
            public int LastAgentID = FirstTouchConstants.AGENT_ID_NONE;
            public bool LastIsDribbling;

            public void SetDribblingState(int agentID, bool isDribbling)
            {
                LastAgentID = agentID;
                LastIsDribbling = isDribbling;
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                |
// | 1.0     | 2026-06-10 | —      | Initial implementation. Spec #4 closed-loop scenario corpus on the  |
// |         |            |        | #19 ScenarioRunner: heavy-touch-runs-on (AR-7 H-1 / ERR-004-003     |
// |         |            |        | displacement-velocity coherence lock) + interception-chain-anchors- |
// |         |            |        | at-displaced-ball (AR-7 M-1 / ERR-004-004 ball-anchored gate via    |
// |         |            |        | the real PressureEvaluator producer, §3.4.5 redirect + Frame N+1    |
// |         |            |        | CONTROLLED chain). Envelope windows from a numerical mirror.        |
#endregion
