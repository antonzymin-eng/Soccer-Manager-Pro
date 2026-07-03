// File:     src/living-world/InteractionSlots.cs
// Created:  2026-07-02
// Modified: 2026-07-02
// Author:   —
// Spec:     Living World System #22 §3.3, FR-LW-013, Code Standards #20
// Purpose:  Slot facts for §3.3 surface-text expansion: match-engine-emitted facts (opponent,
//           scoreline) plus an optional §3.2 cited episode. No derived stats (FR-LW-013).

using System;

namespace TacticalDirector.LivingWorld
{
    /// <summary>
    /// The slot facts an interaction's surface text is expanded from (#22 §3.3). FR-LW-013 bounds the
    /// roster: only facts the match engine actually emits (the opponent, the scoreline) plus one
    /// optionally cited §3.2 <see cref="MemoryEpisode"/> — no assumed derived stats (no xG, no form
    /// tables). Names are caller-supplied display strings; they never enter any digest (the surface
    /// text is not a determinism-pinned wire format — only the RNG draw is, via the world.text
    /// cursor).
    ///
    /// C# default-value semantics bypass the constructors, so
    /// <see cref="InteractionTextGenerator.Generate"/> re-validates before use (a default-valued
    /// instance is malformed — null names).
    /// </summary>
    public readonly struct InteractionSlots
    {
        /// <summary>Display name of the entity the interaction addresses (manager/player/journalist subject).</summary>
        public readonly string SubjectName;

        /// <summary>Display name of the last match's opponent (a match-engine-emitted fact).</summary>
        public readonly string OpponentName;

        /// <summary>Goals for the manager's side in the referenced match (match-engine-emitted).</summary>
        public readonly int HomeGoals;

        /// <summary>Goals for the opponent in the referenced match (match-engine-emitted).</summary>
        public readonly int AwayGoals;

        /// <summary>True if <see cref="CitedEpisode"/> carries a §3.2 episode to reference.</summary>
        public readonly bool HasCitedEpisode;

        /// <summary>
        /// The cited episode (§3.2 "Referencing"). Must satisfy the SALIENCE_REF_THRESHOLD gate at
        /// generation time; meaningful only when <see cref="HasCitedEpisode"/> is true.
        /// </summary>
        public readonly MemoryEpisode CitedEpisode;

        /// <summary>Slots without an episode citation.</summary>
        public InteractionSlots(string subjectName, string opponentName, int homeGoals, int awayGoals)
        {
            ValidateFacts(subjectName, opponentName, homeGoals, awayGoals);
            SubjectName = subjectName;
            OpponentName = opponentName;
            HomeGoals = homeGoals;
            AwayGoals = awayGoals;
            HasCitedEpisode = false;
            CitedEpisode = default;
        }

        /// <summary>Slots citing a §3.2 episode (eligibility is gated at generation, not here).</summary>
        public InteractionSlots(string subjectName, string opponentName, int homeGoals, int awayGoals, in MemoryEpisode citedEpisode)
        {
            ValidateFacts(subjectName, opponentName, homeGoals, awayGoals);
            SubjectName = subjectName;
            OpponentName = opponentName;
            HomeGoals = homeGoals;
            AwayGoals = awayGoals;
            HasCitedEpisode = true;
            CitedEpisode = citedEpisode;
        }

        private static void ValidateFacts(string subjectName, string opponentName, int homeGoals, int awayGoals)
        {
            if (string.IsNullOrEmpty(subjectName))
            {
                throw new ArgumentException("InteractionSlots: SubjectName must be non-empty.", nameof(subjectName));
            }
            if (string.IsNullOrEmpty(opponentName))
            {
                throw new ArgumentException("InteractionSlots: OpponentName must be non-empty.", nameof(opponentName));
            }
            if (homeGoals < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(homeGoals), homeGoals, "InteractionSlots: goals must be non-negative.");
            }
            if (awayGoals < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(awayGoals), awayGoals, "InteractionSlots: goals must be non-negative.");
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-02 | —      | Initial implementation (slice 3): §3.3 slot-fact carrier —    |
// |         |            |        | match-engine facts + optional cited episode (FR-LW-013).      |
#endregion
