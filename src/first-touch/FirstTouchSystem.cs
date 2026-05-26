// File:     src/first-touch/FirstTouchSystem.cs
// Created:  2026-05-25
// Modified: 2026-05-26
// Author:   —
// Spec:     First Touch Mechanics #4 §4.4, §4.5, Code Standards #20
// Purpose:  Orchestrates the first-touch evaluation and application pipeline.

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
            _ballPhysics = ballPhysics;
            _agentMovement = agentMovement;
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

            // Step 1 — Orientation bonus.
            float orientationBonus = context.IsHalfTurnOriented
                ? FirstTouchConstants.HalfTurnBonus
                : 0.0f;

            // Step 2 — Derive scalar speeds.
            float ballSpeed = context.BallVelocity.magnitude;
            float agentSpeed = context.AgentVelocity.magnitude;

            // Step 3 — Control quality.
            float q = ControlQualityCalculator.Calculate(
                context.Technique,
                context.FirstTouchAttribute,
                ballSpeed,
                agentSpeed,
                context.PressureScalar,
                orientationBonus);

            // Step 4 — Thunderbolt cap (also applied inside ControlQualityCalculator;
            // confirmed here to protect callers that pass a pre-built q).
            if (ballSpeed >= FirstTouchConstants.ThunderboltSpeed)
            {
                q = Mathf.Min(q, FirstTouchConstants.ThunderboltQualityCap);
            }

            // Step 5 — Touch radius.
            float r = TouchRadiusCalculator.Calculate(q, ballSpeed);

            // Step 6 — New ball position and velocity.
            (Vector3 newBallPos, Vector3 newBallVel) = BallDisplacementProcessor.Compute(context, q, r);

            // Step 7 — Possession outcome.
            (TouchResult outcome, int possessingId, int interceptingId) = PossessionStateMachine.Determine(
                q, r, newBallVel, context.BallVelocity, context);

            // Step 8 — Diagnostic: EffectiveAttribute = WeightedAttr × OrientationMult × PressureMult.
            float weightedAttr = FirstTouchConstants.TechniqueWeight * Mathf.Max(context.Technique, FirstTouchConstants.AttrMinGuard)
                               + FirstTouchConstants.FirstTouchWeight * Mathf.Max(context.FirstTouchAttribute, FirstTouchConstants.AttrMinGuard);
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
                TriggeredDribblingState = outcome == TouchResult.CONTROLLED,
                IncomingBallSpeed     = ballSpeed,
                EffectiveAttribute    = effectiveAttribute
            };
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
#endregion
