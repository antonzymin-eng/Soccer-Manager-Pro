// File:     src/injuries-medical/ClubInjuryStates.cs
// Created:  2026-08-06
// Modified: 2026-08-06
// Author:   —
// Spec:     Injuries & Medical #41 §2.2 (the persisted medical block), §4.4, FR-MD-017/018/025;
//           Code Standards #20
// Purpose:  One club's per-player InjuryState set — the unit MedicalSaveCodec encodes and decodes.
//           Binds the club id and the parallel playerId / state arrays once, so no call site can pair
//           one club's ids with another club's states (#29's ClubTrainingStates precedent).

using System;

namespace TacticalDirector.InjuriesMedical
{
    /// <summary>
    /// One club's persisted medical block (§2.2 / §4.4): the club id plus the parallel per-player id /
    /// state arrays. This is the unit <see cref="MedicalSaveCodec"/> works in, and the shape #30 will
    /// hand it at T2.
    /// <para>
    /// <b>The three parts are bound once, at construction</b> — the discipline #29's
    /// <c>ClubTrainingStates</c> establishes for the same reason: every club in a generated league has
    /// the same squad size, so an API taking ids and states as separate arguments would accept one
    /// club's ids alongside another club's states without tripping any length check, and then persist
    /// the wrong club's players with no error at all.
    /// </para>
    /// <para>
    /// The arrays are <b>borrowed, not copied</b>: the value reads whatever they hold at call time, so
    /// the owner must not resize them while a value over them is alive.
    /// </para>
    /// <para>
    /// <b>Player order is not state — but a DECODED block is nevertheless strictly ascending, and a
    /// consumer depends on it.</b> The block is a map keyed by player id (FR-MD-018), and this type's
    /// own constructor imposes no order. What is guaranteed is narrower and load-bearing:
    /// <see cref="MedicalSaveCodec.Encode"/> canonicalizes to ascending player id and
    /// <see cref="MedicalSaveCodec.Decode"/> <i>gates</i> it
    /// (<c>SaveBlobFramingHelpers.RequireAscending</c>), so every block out of a save file ascends.
    /// <para>
    /// <c>PlayerCareerStates.FromBlocks</c> in <c>season-save</c> relies on that — every lookup there is
    /// a binary search over these ids — and re-checks it, because this constructor is public. <b>Do not
    /// relax the decode gate on the grounds that "order is not state".</b> It is not state; it is a
    /// decoded-form invariant with a consumer.
    /// </para>
    /// <para>
    /// What round-trips field-identically is each player's <see cref="InjuryState"/>, keyed by his id.
    /// </para>
    /// </summary>
    public readonly struct ClubInjuryStates
    {
        /// <summary>The club these states belong to.</summary>
        public readonly int ClubId;

        /// <summary>The club's player ids. Index <c>i</c> pairs with <see cref="States"/>[i]. Never null.</summary>
        public readonly int[] PlayerIds;

        /// <summary>The matching medical states. Index <c>i</c> is the state of <see cref="PlayerIds"/>[i]. Never null.</summary>
        public readonly InjuryState[] States;

        /// <summary>Binds a club id and its parallel player-id / injury-state arrays.</summary>
        /// <param name="clubId">The owning club.</param>
        /// <param name="playerIds">The club's player ids.</param>
        /// <param name="states">The matching medical states.</param>
        /// <exception cref="ArgumentNullException">Either array is null.</exception>
        /// <exception cref="ArgumentException">The arrays have different lengths — a mismatch is a roster-lifecycle bug (FR-MD-025), not something to iterate to the shorter length.</exception>
        public ClubInjuryStates(int clubId, int[] playerIds, InjuryState[] states)
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
                    "playerIds and states must be parallel; a length mismatch is a roster-lifecycle bug (FR-MD-025).",
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
// | 1.0     | 2026-08-06 | —      | Initial implementation (#41 T1): the per-club unit MedicalSave-    |
// |         |            |        | Codec encodes, mirroring #29's ClubTrainingStates.                 |
// | 1.1     | 2026-08-06 | —      | #29/#41 T2 AR pass 3 (L, doc only). Same correction as the sibling |
// |         |            |        | ClubTrainingStates v1.1: "player order is not state" is true of    |
// |         |            |        | the MAP and false of the DECODED form, which Decode gates          |
// |         |            |        | ascending and season-save's PlayerCareerStates binary-searches.    |
#endregion
