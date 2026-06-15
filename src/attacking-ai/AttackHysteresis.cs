// File:     src/attacking-ai/AttackHysteresis.cs
// Created:  2026-05-29
// Modified: 2026-06-15
// Author:   —
// Spec:     Attacking AI #15 §3.12, FR-AT-022, FR-AT-023, Code Standards #20
// Purpose:  Increment-based role dwell-time hysteresis. A role transition fires only after
//           the candidate role has been continuously preferred for ATTACK_DWELL_TICKS ticks.
//           Authoritative simulation state per #16 §3.2 (FR-AT-004).

namespace TacticalDirector.AttackingAI
{
    /// <summary>
    /// Dwell-time hysteresis for attacking role transitions. Pure static.
    /// INCREMENT-based: DwellCounter increments while the same role is re-preferred; IsStable
    /// (a diagnostic predicate, not an evaluation gate) fires when DwellCounter &gt;=
    /// ATTACK_DWELL_TICKS. A role transition commits only after a DIFFERENT candidate has been
    /// preferred for ATTACK_DWELL_TICKS consecutive ticks (CandidateDwell). Distinct from the
    /// DECREMENT pattern in #14. Attacking AI #15 §3.12 (FR-AT-022 / FR-AT-023).
    /// </summary>
    internal static class AttackHysteresis
    {
        /// <summary>
        /// Returns true when the agent's current role has been held for at least
        /// <see cref="AttackingAIConstants.AttackDwellTicks"/> ticks. DIAGNOSTIC / telemetry
        /// predicate only — it does NOT gate role re-evaluation. The role-assignment loop
        /// (<see cref="RoleAssigner"/>) evaluates every agent every tick; the anti-thrash
        /// hysteresis is supplied entirely by <see cref="Update"/> (a transition commits only
        /// after a new candidate persists <see cref="AttackingAIConstants.AttackDwellTicks"/>
        /// ticks). Using this as a "skip evaluation while stable" gate permanently locks an
        /// agent's role (ERR-015-009).
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
// | 1.2     | 2026-06-15 | —      | AR-4 H-1 (ERR-015-009): IsStable / class docs clarified — IsStable is a diagnostic predicate, NOT a role-evaluation gate; using it to skip re-evaluation permanently locks a role. Hysteresis is enforced solely by Update()'s CandidateDwell. No behavioural change to Update/IsStable themselves; the fix is in RoleAssigner. |
#endregion
