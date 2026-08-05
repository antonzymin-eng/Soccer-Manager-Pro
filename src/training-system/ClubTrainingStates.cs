// File:     src/training-system/ClubTrainingStates.cs
// Created:  2026-08-06
// Modified: 2026-08-06
// Author:   —
// Spec:     Training System #29 §2.2 (the persisted training block), §4.4, FR-TR-018/019/025;
//           Code Standards #20
// Purpose:  One club's per-player TrainingState set — the unit TrainingSaveCodec encodes and decodes.
//           Binds the club id and the parallel playerId / state arrays once, so no call site can pair
//           one club's ids with another club's states (the TrainingSchedule precedent).

using System;

namespace TacticalDirector.TrainingSystem
{
    /// <summary>
    /// One club's persisted training block (§2.2): the club id plus the parallel per-player id / state
    /// arrays. This is the unit <see cref="TrainingSaveCodec"/> works in, and the shape #30 will hand it
    /// at T2.
    /// <para>
    /// <b>The three parts are bound once, at construction</b> — the same reason
    /// <see cref="TrainingSchedule"/> binds its pair. Every club in a generated league has the same squad
    /// size, so a codec API taking ids and states as separate arguments would accept one club's ids
    /// alongside another club's states without tripping any length check, and then persist the wrong
    /// club's players with no error at all.
    /// </para>
    /// <para>
    /// The arrays are <b>borrowed, not copied</b> (the <see cref="TrainingSchedule"/> posture): the value
    /// reads whatever they hold at call time, so the owner must not resize them while a value over them
    /// is alive. Encoding takes a snapshot of the bytes, not of the arrays.
    /// </para>
    /// <para>
    /// <b>Player order is not state.</b> The block is a map keyed by player id (FR-TR-019), so the codec
    /// canonicalizes to ascending player id on encode; this type imposes no order of its own and a
    /// decoded value may present its players in a different order than the one that was saved. What
    /// round-trips field-identically is each player's <see cref="TrainingState"/>, keyed by his id.
    /// </para>
    /// </summary>
    public readonly struct ClubTrainingStates
    {
        /// <summary>The club these states belong to.</summary>
        public readonly int ClubId;

        /// <summary>The club's player ids. Index <c>i</c> pairs with <see cref="States"/>[i]. Never null.</summary>
        public readonly int[] PlayerIds;

        /// <summary>The matching training states. Index <c>i</c> is the state of <see cref="PlayerIds"/>[i]. Never null.</summary>
        public readonly TrainingState[] States;

        /// <summary>Binds a club id and its parallel player-id / training-state arrays.</summary>
        /// <param name="clubId">The owning club.</param>
        /// <param name="playerIds">The club's player ids.</param>
        /// <param name="states">The matching training states.</param>
        /// <exception cref="ArgumentNullException">Either array is null.</exception>
        /// <exception cref="ArgumentException">The arrays have different lengths — a mismatch is a roster-lifecycle bug (FR-TR-025), not something to iterate to the shorter length.</exception>
        public ClubTrainingStates(int clubId, int[] playerIds, TrainingState[] states)
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
                    "playerIds and states must be parallel; a length mismatch is a roster-lifecycle bug (FR-TR-025).",
                    nameof(states));
            }

            ClubId = clubId;
            PlayerIds = playerIds;
            States = states;
        }

        /// <summary>The number of players in the block. Zero for a <c>default</c> value.</summary>
        public int Count => PlayerIds == null ? 0 : PlayerIds.Length;

        // No ToSchedule() convenience here. A TrainingSchedule over these same arrays is one line at the
        // call site, and this assembly does not add a public member with no consumer — T2, which will
        // actually hold both, is where that seam earns its place (the FR-LW-031 posture).
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-08-06 | —      | Initial implementation (#29 T1): the per-club unit TrainingSave-   |
// |         |            |        | Codec encodes, with the TrainingSchedule bind-once discipline.     |
#endregion
