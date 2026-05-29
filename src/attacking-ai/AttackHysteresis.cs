// File:     src/attacking-ai/AttackHysteresis.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Attacking AI #15 §3.12, FR-AT-022, FR-AT-023, Code Standards #20
// Purpose:  Increment-based role dwell-time hysteresis. A role transition fires only after
//           the candidate role has been continuously preferred for ATTACK_DWELL_TICKS ticks.
//           Authoritative simulation state per #16 §3.2 (FR-AT-004).

namespace TacticalDirector.AttackingAI
{
    /// <summary>
    /// Dwell-time hysteresis for attacking role transitions. Pure static.
    /// INCREMENT-based: DwellCounter increments while the role is stable; IsStable fires when
    /// DwellCounter &gt;= ATTACK_DWELL_TICKS. Distinct from the DECREMENT pattern in #14.
    /// Attacking AI #15 §3.12 (FR-AT-022 / FR-AT-023).
    /// </summary>
    internal static class AttackHysteresis
    {
        /// <summary>
        /// Returns true when the agent's current role has been stably held for
        /// at least <see cref="AttackingAIConstants.AttackDwellTicks"/> ticks.
        /// While stable, the role is retained without re-evaluation (§3.3 step 1 / §3.13).
        /// </summary>
        public static bool IsStable(ref AttackHysteresisState hyst)
        {
            return hyst.DwellCounter >= AttackingAIConstants.AttackDwellTicks;
        }

        /// <summary>
        /// Updates <paramref name="hyst"/> for <paramref name="candidateRole"/> this tick.
        /// If the candidate matches the current role, DwellCounter increments (capped at
        /// AttackDwellTicks + 1) and CandidateDwell is reset to 0 to clear any interrupted
        /// evaluation window. If a different candidate is preferred, CandidateDwell accumulates;
        /// when it reaches AttackDwellTicks the transition commits.
        /// </summary>
        public static void Update(ref AttackHysteresisState hyst, AttackRole candidateRole)
        {
            if (candidateRole == hyst.CurrentRole)
            {
                // Same role preferred; accumulate stability and clear any pending candidate.
                if (hyst.DwellCounter < AttackingAIConstants.AttackDwellTicks + 1)
                    hyst.DwellCounter++;
                hyst.CandidateDwell = 0;
            }
            else
            {
                // New candidate preferred.
                if (hyst.CandidateRole != candidateRole)
                {
                    hyst.CandidateRole  = candidateRole;
                    hyst.CandidateDwell = 1;
                }
                else
                {
                    hyst.CandidateDwell++;
                }

                // Commit transition when candidate has been preferred long enough.
                if (hyst.CandidateDwell >= AttackingAIConstants.AttackDwellTicks)
                {
                    hyst.CurrentRole    = candidateRole;
                    hyst.DwellCounter   = 0;
                    hyst.CandidateDwell = 0;
                }
            }
        }

        /// <summary>
        /// Resets the hysteresis state for an agent, reverting it to the default HOLD_WIDTH role.
        /// Used when an agent re-enters the pool after absence (substitution, GK hand-off, etc.).
        /// </summary>
        public static void Reset(ref AttackHysteresisState hyst)
        {
            hyst.CurrentRole    = AttackRole.HoldWidth;
            hyst.DwellCounter   = 0;
            hyst.CandidateRole  = AttackRole.HoldWidth;
            hyst.CandidateDwell = 0;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
// | 1.1     | 2026-05-29 | —      | AR-1 M-1: reset CandidateDwell=0 when current role is re-preferred, preventing stale candidate accumulation. AR-2 L-2: Update() doc updated to describe CandidateDwell reset behaviour. |
#endregion
