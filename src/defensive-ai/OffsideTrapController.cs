// File:     src/defensive-ai/OffsideTrapController.cs
// Created:  2026-05-29
// Modified: 2026-11-10
// Author:   —
// Spec:     Defensive AI #14 §3.7, §4.3, ERR-014-008, Code Standards #20; Tactical Instructions #21 §3.4 (FR-TI-022 / KD-9)
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
            // Update CurrentLineDepth mirror from snapshot.
            offsideState.CurrentLineDepth = snapshot.DefensiveLineDepth;

            // Decrement post-trap cooldown.
            if (offsideState.CooldownTicksRemaining > 0)
                offsideState.CooldownTicksRemaining--;

            // Evaluate the four trigger conditions (§3.7.2).
            bool cond1 = snapshot.BallVelocity.magnitude < DefensiveAIConstants.OffsideBallSpeedThresholdMS;
            bool cond2 = LastManDetector.DistToOwnGoal(snapshot.BallPosition.x)
                       > DefensiveAIConstants.HALF_LINE_X;
            bool cond3 = ComputeDefenseLineSpread(snapshot, poolBuffer, poolCount)
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

            // #21 FR-TI-022 / KD-9 additive request: a manager-requested trap arms after fewer
            // qualifying ticks. The request only lowers the threshold (never raises it) and the
            // four §3.7.2 conditions above still adjudicate — the toggle is a request, not a
            // guarantee. Default (Balanced ⇒ OffsideTrapRequested false) keeps the autonomous
            // baseline dwell, so behaviour is byte-identical to pre-#21.
            int dwellRequired = snapshot.OffsideTrapRequested
                ? DefensiveAIConstants.OffsideTrapRequestedDwellTicks
                : DefensiveAIConstants.OffsideTrapDwellTicks;

            // Fire the trap when dwell count reached and cooldown expired.
            if (offsideState.StepUpDwellCounter >= dwellRequired
             && offsideState.CooldownTicksRemaining == 0)
            {
                ExecuteStepUp(snapshot, poolBuffer, poolCount, currentTick,
                              ref offsideState, ref directive, assignments);
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
            MarkAssignment[]     assignments)
        {
            // Compute step target depth (distToOwnGoal).
            float stepDepth = offsideState.CurrentLineDepth + DefensiveAIConstants.OffsideStepSizeM;
            stepDepth = Mathf.Max(stepDepth, snapshot.DefensiveLineDepth); // Never below #12 target.
            stepDepth = Mathf.Min(stepDepth, DefensiveAIConstants.OffsideMaxDepthM); // Safety ceiling.

            // Canonical frame (own goal at x = 0): distToOwnGoal IS the x coordinate.
            float targetX = stepDepth;

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
            int               poolCount)
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

                float d = LastManDetector.DistToOwnGoal(a.Position.x);
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
// | 1.2     | 2026-06-29 | —      | #21 FR-TI-022 / KD-9: active consumption of OffsideTrapRequested — a requested trap    |
// |         |            |        |   arms after OffsideTrapRequestedDwellTicks (≤ baseline); the §3.7.2 conditions still   |
// |         |            |        |   adjudicate. Default false ⇒ baseline dwell ⇒ behaviour-neutral.                       |
// | 1.3     | 2026-11-10 | —      | ERR-014-008: defendsX0 from DefensiveSnapshot.IsOwnGoalAtX0 (canonical frame), not       |
// |         |            |        |   teamId == 0. The away team's ball-in-opponent-half test, defence-line spread and          |
// |         |            |        |   step-up x were computed against x = 105 although its snapshot has its goal at x = 0.      |
// | 1.4     | 2026-11-10 | —      | W13.0 review M1: canonical-only; defendsX0 plumbing and the 105 - depth step-up branch removed. |
#endregion
