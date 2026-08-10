// File: src/positioning-ai/PhaseClassifier.cs
// Created:  2026-05-29
// Modified: 2026-08-08
// Author:   —
// Spec: #12 Positioning AI §3.0
// Purpose: Classifies and commits the tactical phase for one team per 10 Hz tick with hysteresis.

namespace TacticalDirector.PositioningAI
{
    /// <summary>
    /// Computes the committed tactical phase from possession state and ball velocity.
    /// Phase transitions require PHASE_HYSTERESIS_TICKS consecutive ticks of the same candidate
    /// before committing, preventing rapid oscillation on contested possession.
    /// Pure static utility; no instance state — all mutable state lives in HysteresisState.
    /// </summary>
    public static class PhaseClassifier
    {
        /// <summary>
        /// Classifies the candidate phase from the current perception snapshot,
        /// advances the dwell counter, commits if threshold reached, and returns the committed phase.
        /// Called before any slot computation each tick.
        /// </summary>
        /// <param name="snapshot">Current perception; HasTeamPossession == false means no team holds the ball.</param>
        /// <param name="hyst">Team hysteresis state; mutated in-place.</param>
        /// <returns>Committed phase; may be unchanged if candidate has not dwelled long enough.</returns>
        public static Phase ClassifyAndCommit(PositioningPerceptionSnapshot snapshot, HysteresisState hyst)
        {
            Phase candidate = ComputeCandidate(snapshot, hyst.CurrentPhase);

            if (candidate == hyst.CandidatePhase)
            {
                hyst.PhaseDwellCount++;
                if (hyst.PhaseDwellCount >= PositioningAIConstants.PHASE_HYSTERESIS_TICKS)
                {
                    hyst.CurrentPhase   = candidate;
                    hyst.PhaseDwellCount = PositioningAIConstants.PHASE_HYSTERESIS_TICKS; // clamp; don't overflow
                }
            }
            else
            {
                hyst.CandidatePhase  = candidate;
                hyst.PhaseDwellCount = 1;
                // Do NOT flip CurrentPhase — previous committed phase stands until dwell completes.
            }

            return hyst.CurrentPhase;
        }

        // ── Internal helpers ──────────────────────────────────────────────────

        // Returns the new candidate phase.  When no team is in possession and speed is below
        // threshold, returns the existing committed phase so the dwell counter is not disrupted.
        private static Phase ComputeCandidate(PositioningPerceptionSnapshot snap, Phase lastCommitted)
        {
            // §3.0.2 keys on TEAM possession, not on the on-ball carrier (ERR-012-011). A team keeps
            // the ball while a pass it played travels to a team-mate; reading snap.PossessionOwner-
            // EntityId here classified every pass flight as a transition, and the engine holds no
            // carrier for the whole flight, so InPoss was committed on 7.5% of final-third samples.
            // The orchestrator owns the distinction; this classifier must not reconstruct it from
            // the carrier field, or there are two possession concepts in one function.
            if (snap.HasTeamPossession)
                return snap.TeamPossessionIsOwnTeam ? Phase.InPoss : Phase.OutOfPoss;

            // No team in possession: direction of ball travel decides transition phase.
            if (snap.BallVxFiltered > PositioningAIConstants.PHASE_LOOSE_VELOCITY_THRESHOLD)
                return Phase.TransToAtk;
            if (snap.BallVxFiltered < -PositioningAIConstants.PHASE_LOOSE_VELOCITY_THRESHOLD)
                return Phase.TransToDef;

            // Velocity insufficient — §3.0.2 "else → lastPhase": no candidate change.
            return lastCommitted;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
// | 1.1     | 2026-08-08 | —      | ERR-012-011: ComputeCandidate keys on the snapshot's TEAM-possession |
// |         |            |        |   pair instead of the on-ball carrier id. The carrier is absent for |
// |         |            |        |   the whole flight of every pass, so a passing team read as being in |
// |         |            |        |   transition. Hysteresis and the V0 velocity branch are unchanged.   |
#endregion
