// File:     src/season-save/ClubAppearanceStates.cs
// Created:  2026-08-07
// Modified: 2026-08-07
// Author:   —
// Spec:     Season & Competition Loop #30 Appendix B (the appearance block, ERR-041-010(b));
//           Code Standards #20
// Purpose:  One club's per-player AppearanceState set — the unit AppearanceSaveCodec encodes and
//           decodes. Binds the club id and the parallel playerId / state arrays once, the
//           ClubTrainingStates / ClubInjuryStates discipline.

using System;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// One club's persisted appearance block (#30 Appendix B): the club id plus the parallel per-player
    /// id / state arrays. This is the unit <see cref="AppearanceSaveCodec"/> works in — the third
    /// sibling of <c>ClubTrainingStates</c> / <c>ClubInjuryStates</c>, with the same bind-once
    /// discipline for the same reason: every club in a generated league has the same squad size, so an
    /// API taking ids and states separately would pair one club's ids with another's states without
    /// tripping any length check.
    /// <para>
    /// The arrays are <b>borrowed, not copied</b> (the <c>ClubTrainingStates</c> posture): encoding
    /// takes a snapshot of the bytes, not of the arrays. And as there, a DECODED block is strictly
    /// ascending by player id — <see cref="AppearanceSaveCodec.Decode"/> gates it — because
    /// <c>PlayerCareerStates.FromBlocks</c> binary-searches these ids; this constructor itself imposes
    /// no order.
    /// </para>
    /// </summary>
    public readonly struct ClubAppearanceStates
    {
        /// <summary>The club these states belong to.</summary>
        public readonly int ClubId;

        /// <summary>The club's player ids. Index <c>i</c> pairs with <see cref="States"/>[i]. Never null.</summary>
        public readonly int[] PlayerIds;

        /// <summary>The matching appearance states. Index <c>i</c> is the state of <see cref="PlayerIds"/>[i]. Never null.</summary>
        public readonly AppearanceState[] States;

        /// <summary>Binds a club id and its parallel player-id / appearance-state arrays.</summary>
        /// <param name="clubId">The owning club.</param>
        /// <param name="playerIds">The club's player ids.</param>
        /// <param name="states">The matching appearance states.</param>
        /// <exception cref="ArgumentNullException">Either array is null.</exception>
        /// <exception cref="ArgumentException">The arrays have different lengths — a mismatch is a roster-lifecycle bug (FR-MD-025's class), not something to iterate to the shorter length.</exception>
        public ClubAppearanceStates(int clubId, int[] playerIds, AppearanceState[] states)
        {
            if (playerIds == null)
            {
                throw new ArgumentNullException(nameof(playerIds));
            }

            if (states == null)
            {
                throw new ArgumentNullException(nameof(states));
            }

            if (playerIds.Length != states.Length)
            {
                throw new ArgumentException(
                    "playerIds and states must be parallel; a length mismatch is a roster-lifecycle bug.",
                    nameof(states));
            }

            ClubId = clubId;
            PlayerIds = playerIds;
            States = states;
        }

        /// <summary>The number of players in the block. Zero for a <c>default</c> value.</summary>
        public int Count => PlayerIds == null ? 0 : PlayerIds.Length;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-08-07 | —      | Initial implementation (balance pass D2): the per-club unit       |
// |         |            |        | AppearanceSaveCodec encodes, with the bind-once discipline.       |
#endregion
