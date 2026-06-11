// File:     src/first-touch/FirstTouchSystem.cs
// Created:  2026-05-25
// Modified: 2026-06-10
// Author:   —
// Spec:     First Touch Mechanics #4 §4.4, §4.5, Code Standards #20
// Purpose:  Orchestrates the first-touch evaluation and application pipeline.

using System;

using UnityEngine;
using UnityEngine.Profiling;

namespace TacticalDirector.FirstTouch
{
    /// <summary>
    /// Sealed system class implementing the first-touch evaluation and application pipeline.
    /// EvaluateFirstTouch is pure (no side effects); ApplyTouchResult mutates simulation state
    /// via injected subsystems. First Touch Mechanics #4 §4.4.
    /// </summary>
    public sealed class FirstTouchSystem : IFirstTouchSystem
    {
        private static readonly ProfilerMarker s_evaluateMarker =
            new ProfilerMarker("FirstTouch.Evaluate");

        private static readonly ProfilerMarker s_applyMarker =
            new ProfilerMarker("FirstTouch.Apply");

        private readonly IBallPhysicsSystem _ballPhysics;
        private readonly IAgentMovementSystem _agentMovement;

        /// <summary>
        /// Constructs the system with injected subsystem dependencies.
        /// First Touch Mechanics #4 §4.4.
        /// </summary>
        /// <param name="ballPhysics">Ball physics system for writing ball state after touch.</param>
        /// <param name="agentMovement">Agent movement system for writing dribbling state after touch.</param>
        public FirstTouchSystem(IBallPhysicsSystem ballPhysics, IAgentMovementSystem agentMovement)
        {
            _ballPhysics = ballPhysics ?? throw new ArgumentNullException(nameof(ballPhysics));
            _agentMovement = agentMovement ?? throw new ArgumentNullException(nameof(agentMovement));
        }

        /// <summary>
        /// Evaluates a first-touch attempt. Pure computation; produces no side effects.
        /// First Touch Mechanics #4 §4.5.1.
        /// </summary>
        /// <param name="context">Snapshot of all per-touch input data.</param>
        /// <returns>Computed touch result including outcome, ball state, and possession IDs.</returns>
        public FirstTouchResult EvaluateFirstTouch(FirstTouchContext context)
        {
            using var _ = s_evaluateMarker.Auto();

            // Step 0 — Sanitise gate (AR-8 M-1 / EC-001): non-finite velocity and direction
            // components map to zero before any model math. Mathf.Clamp01 does NOT filter NaN,
            // so a NaN BallVelocity would otherwise flow through velDifficulty into
            // ControlQuality and the result struct (parallels Agent Movement AR-10
            // SanitiseCollisionForce and Collision System AR-7 snapshot-gate sanitise).
            // Zeroed vectors route through the existing degenerate-input fallbacks.
            // Recovery choice: zero + the §3.1 VelocityMin guard, NOT the EC-001 row's
            // suggested VELOCITY_REFERENCE substitution — one consistent recovery must
            // serve BOTH the q path and the §3.3.5 momentum-retention path, and
            // manufacturing 15 m/s of phantom incoming momentum from corrupt data is
            // worse than treating it as at-rest. §2.6.1 FM-01 (clamp-and-continue) is
            // the normative failure-mode rule; the EC-001 sentence is advisory.
            // `context` is a by-value copy; the caller's struct is untouched.
            context.BallVelocity = SanitiseVector(context.BallVelocity);
            context.AgentVelocity = SanitiseVector(context.AgentVelocity);
            context.AgentFacing = SanitiseVector(context.AgentFacing);
            context.IntendedTouchDirection = SanitiseVector(context.IntendedTouchDirection);

            // Step 1 — Orientation bonus.
            float orientationBonus = context.IsHalfTurnOriented
                ? FirstTouchConstants.HalfTurnBonus
                : 0.0f;

            // Step 2 — Derive scalar speeds.
            float ballSpeed = context.BallVelocity.magnitude;
            float agentSpeed = context.AgentVelocity.magnitude;

            // Step 3 — Control quality. weightedAttr surfaced for Step 8 diagnostic.
            float q = ControlQualityCalculator.Calculate(
                context.Technique,
                context.FirstTouchAttribute,
                ballSpeed,
                agentSpeed,
                context.PressureScalar,
                orientationBonus,
                out float weightedAttr);

            // Step 4 — Thunderbolt cap: separate post-Step-8 operation per §3.3.7.
            // Spec uses strict greater-than: ballSpeed > THUNDERBOLT_SPEED.
            if (ballSpeed > FirstTouchConstants.ThunderboltSpeed)
            {
                q = Mathf.Min(q, FirstTouchConstants.ThunderboltQualityCap);
            }

            // Step 5 — Touch radius.
            float r = TouchRadiusCalculator.Calculate(q, ballSpeed);

            // Step 6 — New ball position and velocity.
            (Vector3 newBallPos, Vector3 newBallVel) = BallDisplacementProcessor.Compute(context, q, r);

            // Step 7 — Possession outcome (classified on r and the displaced ball position).
            (TouchResult outcome, int possessingId, int interceptingId) = PossessionStateMachine.Determine(
                r, newBallPos, newBallVel, context);

            // Step 7.5 — §3.4.5: on INTERCEPTION the ball travels toward the intercepting
            // opponent (not zero) so the Frame N+1 contact chain can occur. Speed preserved
            // from the §3.3.5 model; direction re-anchored at the displaced ball position.
            // Opponent data is valid here: the INTERCEPTION branch gates on it. If the
            // opponent is coincident with the displaced ball (degenerate direction), the
            // §3.3.5 displacement velocity is kept as-is — the interceptor is already at
            // the ball, so the Frame N+1 contact fires regardless of direction (AR-9 L-2).
            if (outcome == TouchResult.Interception)
            {
                Vector2 toOpponent = context.NearestOpponentPositionXY
                                   - new Vector2(newBallPos.x, newBallPos.y);
                if (toOpponent.sqrMagnitude > FirstTouchConstants.BLEND_MIN_MAGNITUDE_SQ)
                {
                    float interceptSpeed = newBallVel.magnitude;
                    Vector2 toOpponentDir = toOpponent.normalized;
                    newBallVel = new Vector3(
                        toOpponentDir.x * interceptSpeed,
                        toOpponentDir.y * interceptSpeed,
                        0.0f);
                }
            }

            // Step 8 — Diagnostic: EffectiveAttribute = WeightedAttr × OrientationMult × PressureMult.
            float effectiveAttribute = weightedAttr
                * (1.0f + orientationBonus)
                * (1.0f - context.PressureScalar * FirstTouchConstants.PressureWeight);

            // Step 9 — Assemble result.
            return new FirstTouchResult
            {
                PossessionOutcome     = outcome,
                ControlQuality        = q,
                TouchRadius           = r,
                NewBallPosition       = newBallPos,
                NewBallVelocity       = newBallVel,
                PossessingAgentID     = possessingId,
                InterceptingAgentID   = interceptingId,
                TriggeredDribblingState = outcome == TouchResult.Controlled,
                IncomingBallSpeed     = ballSpeed,
                EffectiveAttribute    = effectiveAttribute
            };
        }

        /// <summary>
        /// Maps non-finite (NaN / ±Inf) vector components to zero. AR-8 M-1 / EC-001 input gate.
        /// </summary>
        /// <param name="v">Vector to sanitise.</param>
        private static Vector3 SanitiseVector(Vector3 v)
        {
            return new Vector3(
                float.IsFinite(v.x) ? v.x : 0.0f,
                float.IsFinite(v.y) ? v.y : 0.0f,
                float.IsFinite(v.z) ? v.z : 0.0f);
        }

        /// <summary>
        /// Applies a previously evaluated touch result to the simulation via injected subsystems.
        /// First Touch Mechanics #4 §4.5.1.
        /// </summary>
        /// <param name="result">Result produced by EvaluateFirstTouch.</param>
        /// <param name="context">The same context passed to EvaluateFirstTouch.</param>
        public void ApplyTouchResult(FirstTouchResult result, FirstTouchContext context)
        {
            using var _ = s_applyMarker.Auto();

            // Write new ball position and velocity.
            _ballPhysics.SetBallState(result.NewBallPosition, result.NewBallVelocity);

            // TODO: BallState.State write pending Ball Physics API extension (§4.5.4 design gap).
            // The logical ball state (e.g. IN_PLAY, FREE_BALL) cannot be updated here until
            // BallPhysicsSystem exposes a SetBallLogicalState method or equivalent.

            // Update dribbling state based on outcome.
            if (result.TriggeredDribblingState)
            {
                _agentMovement.SetDribblingState(result.PossessingAgentID, true);
            }
            else
            {
                _agentMovement.SetDribblingState(context.AgentID, false);
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // §4.5.5 — Debug logging for touch events (editor and development builds only).
            // String interpolation is acceptable here: this code path is behind a conditional compile
            // and is not on the hot path.
            UnityEngine.Debug.Log(
                $"[FirstTouch] Agent={context.AgentID} Outcome={result.PossessionOutcome} "
                + $"q={result.ControlQuality:F3} r={result.TouchRadius:F3}m "
                + $"PossessingID={result.PossessingAgentID} InterceptingID={result.InterceptingAgentID}");
#endif
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                                                     |
// | 1.0     | 2026-05-25 | —      | Initial draft.                                                                                            |
// | 1.1     | 2026-05-26 | —      | Updated field names (FirstTouchAttribute, TouchRadius, PossessionOutcome); populate new diagnostic fields. |
// | 1.2     | 2026-05-26 | —      | Adversarial review pass 2: Step 4 thunderbolt check changed >= to > per spec §3.3.7 appendix B.5; removed misleading "pre-built q" comment. |
// | 1.3     | 2026-06-06 | —      | AR-5 M-1: TouchResult.CONTROLLED → TouchResult.Controlled (enum PascalCase rename). M-3: constructor null-guards on _ballPhysics / _agentMovement with explicit ArgumentNullException + nameof; using System added. |
// | 1.4     | 2026-06-06 | —      | AR-6 L-3: Step 8 EffectiveAttribute now reads weightedAttr from ControlQualityCalculator.Calculate's new out parameter instead of recomputing TechniqueWeight × Max(tech) + FirstTouchWeight × Max(first). |
// | 1.5     | 2026-06-10 | —      | AR-7 M-1 follow-on: Determine call site updated (q/originalBallVel dropped, newBallPos added — interception is ball-anchored per §3.4.2 / ERR-004-004). AR-7 M-3: new Step 7.5 implements the §3.4.5 INTERCEPTION velocity redirect toward the intercepting opponent (speed preserved, Z = 0) — previously the redirect was specified but never implemented. |
// | 1.6     | 2026-06-10 | —      | AR-8 M-1: Step 0 sanitise gate — non-finite BallVelocity / AgentVelocity / AgentFacing / IntendedTouchDirection components map to 0 before model math. Mathf.Clamp01 does not filter NaN, so NaN ball velocity previously flowed magnitude → velDifficulty → ControlQuality → result (EC-001 encoded the no-NaN contract but could not pass). Parallels AM AR-10 / CS AR-7 sanitise gates. |
// | 1.7     | 2026-06-10 | —      | AR-9 L-2 (doc-only): Step 7.5 comment documents the coincident-opponent degenerate branch — displacement velocity kept as-is; interceptor already at the ball so the §3.4.5 Frame N+1 chain fires regardless. |
#endregion
