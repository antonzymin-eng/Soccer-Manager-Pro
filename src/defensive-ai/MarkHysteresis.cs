// File:     src/defensive-ai/MarkHysteresis.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Defensive AI #14 §3.11, §4.3, Code Standards #20
// Purpose:  Pure static module: implements the dwell-time hysteresis pattern for
//           mark-assignment transitions, binding to Agent Movement #2 §3.1 (KD-11).

namespace TacticalDirector.DefensiveAI
{
    /// <summary>
    /// Applies the assignment hysteresis pattern (§3.11) to mark-mode transitions.
    /// Prevents thrash when two candidates alternate in relative threat-score or cost
    /// ranking on adjacent ticks. Binding to Agent Movement #2 §3.1 dwell-time pattern (KD-11).
    /// Pure static; no side effects except via ref parameters. Defensive AI #14 §3.11 / §4.3.
    /// </summary>
    public static class MarkHysteresis
    {
        /// <summary>
        /// Applies the hysteresis pre-check. If the agent is in a non-ZONAL assignment
        /// with an active dwell lock, decrements the counter and retains the current
        /// assignment without re-evaluation.
        /// </summary>
        /// <param name="currentAssignment">Current published assignment for this agent.</param>
        /// <param name="hysteresis">Hysteresis state (modified in place).</param>
        /// <returns>True when the caller should skip candidate evaluation and retain current assignment.</returns>
        public static bool PreCheck(
            ref MarkAssignment    currentAssignment,
            ref MarkHysteresisState hysteresis)
        {
            // Guard: only suppress re-evaluation for non-ZONAL assignments (T-DA-054).
            // ZONAL assignments are always re-evaluated; they have no dwell semantics.
            if (currentAssignment.Mode == MarkMode.Zonal)
                return false;

            if (hysteresis.DwellCounter > 0)
            {
                hysteresis.DwellCounter--;
                return true; // Retain current assignment.
            }

            return false;
        }

        /// <summary>
        /// Applies the post-evaluation hysteresis gate. Commits the transition to
        /// <paramref name="candidate"/> only when the candidate has been consistently
        /// preferred for <see cref="DefensiveAIConstants.MarkDwellTicks"/> consecutive ticks.
        /// </summary>
        /// <param name="candidate">Candidate assignment selected this tick.</param>
        /// <param name="currentAssignment">Current published assignment (modified in place on commit).</param>
        /// <param name="hysteresis">Hysteresis state (modified in place).</param>
        public static void ApplyGate(
            MarkAssignment          candidate,
            ref MarkAssignment      currentAssignment,
            ref MarkHysteresisState hysteresis)
        {
            // Same assignment — no transition needed; reset candidate tracker.
            if (candidate.Mode         == currentAssignment.Mode
             && candidate.TargetEntityId == currentAssignment.TargetEntityId)
            {
                hysteresis.HoldTicks              = 0;
                hysteresis.CandidateMode          = candidate.Mode;
                hysteresis.CandidateTargetEntityId = candidate.TargetEntityId;
                return;
            }

            // Candidate differs from current assignment.
            if (hysteresis.CandidateMode          == candidate.Mode
             && hysteresis.CandidateTargetEntityId == candidate.TargetEntityId)
            {
                // Candidate stable this tick — increment accumulator.
                hysteresis.HoldTicks++;

                if (hysteresis.HoldTicks >= DefensiveAIConstants.MarkDwellTicks)
                {
                    // Consistently preferred: commit the transition.
                    currentAssignment = candidate;
                    hysteresis.DwellCounter           = DefensiveAIConstants.MarkDwellTicks;
                    hysteresis.HoldTicks              = 0;
                    hysteresis.CandidateMode          = MarkMode.Zonal;
                    hysteresis.CandidateTargetEntityId = -1;
                }
            }
            else
            {
                // New candidate: reset accumulator and begin tracking.
                hysteresis.CandidateMode          = candidate.Mode;
                hysteresis.CandidateTargetEntityId = candidate.TargetEntityId;
                hysteresis.HoldTicks              = 1;
            }
        }

        /// <summary>
        /// Immediately resets hysteresis state for emergency overrides (§3.8 / §3.9).
        /// Emergency assignments bypass the dwell requirement and take effect this tick.
        /// </summary>
        public static void Reset(ref MarkHysteresisState hysteresis)
        {
            hysteresis.DwellCounter           = 0;
            hysteresis.HoldTicks              = 0;
            hysteresis.CandidateMode          = MarkMode.Zonal;
            hysteresis.CandidateTargetEntityId = -1;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
