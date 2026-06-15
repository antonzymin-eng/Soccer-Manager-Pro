// File:     src/defensive-ai/OffsideTrapController.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Defensive AI #14 §3.7, §4.3, Code Standards #20
// Purpose:  Pure static module: manages the offside-trap dwell counter (§3.7.3) and
//           executes the DEFENSE-line step-up when all trigger conditions are satisfied.

using UnityEngine;

namespace TacticalDirector.DefensiveAI
{
    /// <summary>
    /// Implements the offside-trap algorithm (§3.7 / KD-9). Pure static; zero allocation.
    /// #14 places defenders at the target depth; a future referee spec adjudicates offside
    /// (FR-DA-020 / KD-9). Defensive AI #14 §3.7 / §4.3.
    /// </summary>
    public static class OffsideTrapController
    {
        /// <summary>
        /// Updates the dwell counter (§3.7.3) and fires the step-up when eligible (§3.7.4).
        /// </summary>
        /// <param name="snapshot">Current tick snapshot.</param>
        /// <param name="poolBuffer">EntityId-ascending HOLD_SHAPE pool.</param>
        /// <param name="poolCount">Number of valid pool entries.</param>
        /// <param name="currentTick">Tick index for assignment stamping.</param>
        /// <param name="offsideState">Per-team offside state (modified in place).</param>
        /// <param name="directive">Per-team directive (modified in place: OffsideTrapActive / StepUpTargetDepth).</param>
        /// <param name="assignments">Assignment buffer (modified in place for DEFENSE-line agents when trap fires).</param>
        public static void Update(
            DefensiveSnapshot snapshot,
            int[]             poolBuffer,
            int               poolCount,
            int               currentTick,
            ref OffsideLineState offsideState,
            ref MarkDirective    directive,
            MarkAssignment[]     assignments)
        {
            bool defendsX0 = LastManDetector.DefendsX0(snapshot.DefensiveTeamId);

            // Update CurrentLineDepth mirror from snapshot.
            offsideState.CurrentLineDepth = snapshot.DefensiveLineDepth;

            // Decrement post-trap cooldown.
            if (offsideState.CooldownTicksRemaining > 0)
                offsideState.CooldownTicksRemaining--;

            // Evaluate the four trigger conditions (§3.7.2).
            bool cond1 = snapshot.BallVelocity.magnitude < DefensiveAIConstants.OffsideBallSpeedThresholdMS;
            bool cond2 = LastManDetector.DistToOwnGoal(snapshot.BallPosition.x, defendsX0)
                       > DefensiveAIConstants.HALF_LINE_X;
            bool cond3 = ComputeDefenseLineSpread(snapshot, poolBuffer, poolCount, defendsX0)
                       < DefensiveAIConstants.LineCoherenceThresholdM;
            bool cond4 = !snapshot.HasActivePrimaryPress;

            if (cond1 && cond2 && cond3 && cond4)
            {
                offsideState.StepUpDwellCounter++;
            }
            else
            {
                offsideState.StepUpDwellCounter = 0;
            }

            // Fire the trap when dwell count reached and cooldown expired.
            if (offsideState.StepUpDwellCounter >= DefensiveAIConstants.OffsideTrapDwellTicks
             && offsideState.CooldownTicksRemaining == 0)
            {
                ExecuteStepUp(snapshot, poolBuffer, poolCount, currentTick,
                              ref offsideState, ref directive, assignments, defendsX0);
            }
        }

        // ── Step-up execution (§3.7.4) ────────────────────────────────────────

        private static void ExecuteStepUp(
            DefensiveSnapshot  snapshot,
            int[]              poolBuffer,
            int                poolCount,
            int                currentTick,
            ref OffsideLineState offsideState,
            ref MarkDirective    directive,
            MarkAssignment[]     assignments,
            bool               defendsX0)
        {
            // Compute step target depth (distToOwnGoal).
            float stepDepth = offsideState.CurrentLineDepth + DefensiveAIConstants.OffsideStepSizeM;
            stepDepth = Mathf.Max(stepDepth, snapshot.DefensiveLineDepth); // Never below #12 target.
            stepDepth = Mathf.Min(stepDepth, DefensiveAIConstants.OffsideMaxDepthM); // Safety ceiling.

            // Convert distToOwnGoal to world-space x.
            float targetX = defendsX0
                ? stepDepth
                : DefensiveAIConstants.PITCH_LENGTH_M - stepDepth;

            // Issue ZONAL assignments to all DEFENSE-line agents (FR-DA-019).
            for (int p = 0; p < poolCount; p++)
            {
                // Skip agents already set by emergency overrides.
                if (assignments[p].OverriddenThisTick)
                    continue;

                int aIdx = HoldShapePoolFilter.SnapshotIndexOf(snapshot, poolBuffer[p]);
                if (aIdx < 0)
                    continue;

                ref readonly DefensiveAgentSnapshot a = ref snapshot.Agents[aIdx];
                if (a.Line != TacticalDirector.PositioningAI.LineId.Defense)
                    continue;

                assignments[p] = new MarkAssignment
                {
                    AgentEntityId      = poolBuffer[p],
                    Mode               = MarkMode.Zonal,
                    TargetEntityId     = -1,
                    TargetPosition     = new Vector2(targetX, a.BaselineSlot.y),
                    ValidThroughTick   = currentTick,
                    OverriddenThisTick = false,
                    IsManuallyAssigned = false,
                };
            }

            directive.OffsideTrapActive  = true;
            directive.StepUpTargetDepth  = stepDepth;

            offsideState.CooldownTicksRemaining = DefensiveAIConstants.OffsideResetCooldownTicks;
            offsideState.StepUpDwellCounter     = 0;
        }

        // ── Defense-line spread (§3.7.2 condition 3) ─────────────────────────

        private static float ComputeDefenseLineSpread(
            DefensiveSnapshot snapshot,
            int[]             poolBuffer,
            int               poolCount,
            bool              defendsX0)
        {
            float minX = float.MaxValue;
            float maxX = float.MinValue;

            for (int p = 0; p < poolCount; p++)
            {
                int aIdx = HoldShapePoolFilter.SnapshotIndexOf(snapshot, poolBuffer[p]);
                if (aIdx < 0)
                    continue;

                ref readonly DefensiveAgentSnapshot a = ref snapshot.Agents[aIdx];
                if (a.Line != TacticalDirector.PositioningAI.LineId.Defense)
                    continue;

                float d = LastManDetector.DistToOwnGoal(a.Position.x, defendsX0);
                if (d < minX) minX = d;
                if (d > maxX) maxX = d;
            }

            // No DEFENSE-line agents found: there is no coherent line to step up. Return a
            // sentinel that FAILS the §3.7.2 condition-3 coherence gate (< threshold) rather
            // than 0f, which would spuriously read as a perfectly coherent (zero-spread) line.
            return (minX > maxX) ? float.MaxValue : (maxX - minX);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation.                                                              |
// | 1.1     | 2026-06-15 | —      | AR-2 M-2: ComputeDefenseLineSpread returned 0f when no DEFENSE-line agents exist,     |
// |         |            |        |   which passes condition-3 (< coherence threshold) and could arm the trap with no      |
// |         |            |        |   back line to step up; now returns float.MaxValue (fails the gate) for the empty case. |
#endregion
