// File:     src/player-progression/ClubCareerStates.cs
// Created:  2026-08-08
// Modified: 2026-08-08
// Author:   —
// Spec:     Player Progression & Lifecycle #28 §2.2 / §3.5 (the persisted career-state block), KD-4,
//           FR-PG-016/019; Code Standards #20
// Purpose:  One club's per-player career state — the evolving PlayerRecord set plus its PlayerLifecycle
//           overlay. The unit ProgressionSaveCodec encodes and decodes, and the unit ProgressionEngine
//           holds per club. Binds the club id and the two parallel arrays once, so no call site can
//           pair one club's records with another club's lifecycles.

using System;

using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.PlayerProgression
{
    /// <summary>
    /// One club's persisted career-state block (§2.2 / KD-4): the club id plus the parallel per-player
    /// <see cref="PlayerRecord"/> / <see cref="PlayerLifecycle"/> arrays.
    /// <para>
    /// <b>Why this holds the records and not just the overlay.</b> KD-4 makes #28's block the
    /// serialized career-state roster: the <c>[1,20]</c> attributes EVOLVE under
    /// <see cref="GrowthProjection"/>, and nothing else in the stack persists them —
    /// <c>Squad</c> is immutable and rebuilt from the world seed. An overlay-only block would carry the
    /// growth cursor across a save and lose the attribute the cursor was spent on, which is the
    /// silent-divergence class this project keeps filing.
    /// </para>
    /// <para>
    /// <b>No parallel id array.</b> The sibling <c>ClubTrainingStates</c> carries one because
    /// <c>TrainingState</c> has no identity of its own; <see cref="PlayerRecord.PlayerId"/> is the key
    /// here, so a separate id array would be a second source of truth for the same fact and could
    /// disagree with the record it indexes. Index <c>i</c> of <see cref="Lifecycles"/> is the overlay of
    /// <see cref="Records"/>[i], keyed by that record's own <c>PlayerId</c>.
    /// </para>
    /// <para>
    /// The arrays are <b>borrowed, not copied</b> (the <c>ClubTrainingStates</c> posture): the value
    /// reads whatever they hold at call time, so the owner must not resize them while a value over them
    /// is alive. Encoding takes a snapshot of the bytes, not of the arrays.
    /// </para>
    /// <para>
    /// <b>Player order is not state — but a DECODED block is strictly ascending by
    /// <c>PlayerId</c>, and a consumer depends on it.</b> The block is a map keyed by player id
    /// (FR-PG-019), so this constructor imposes no order. What is guaranteed is narrower and
    /// load-bearing: <see cref="ProgressionSaveCodec.Encode"/> canonicalizes to ascending player id and
    /// <see cref="ProgressionSaveCodec.Decode"/> gates it, so every block out of a save file ascends —
    /// which is what lets <see cref="ProgressionEngine"/> binary-search it. Do not relax the decode gate
    /// on the grounds that "order is not state": it is not state, it is a decoded-form invariant with a
    /// consumer (the <c>ClubTrainingStates</c> note, for the identical reason).
    /// </para>
    /// </summary>
    public readonly struct ClubCareerStates
    {
        /// <summary>The club these career states belong to.</summary>
        public readonly int ClubId;

        /// <summary>The club's evolving career-state records. Never null.</summary>
        public readonly PlayerRecord[] Records;

        /// <summary>The matching lifecycle overlays. Index <c>i</c> is the overlay of <see cref="Records"/>[i]. Never null.</summary>
        public readonly PlayerLifecycle[] Lifecycles;

        /// <summary>Number of players in this club's block.</summary>
        public int Count => Records.Length;

        /// <summary>Binds a club id and its parallel record / lifecycle arrays.</summary>
        /// <param name="clubId">The owning club.</param>
        /// <param name="records">The club's career-state records.</param>
        /// <param name="lifecycles">The matching lifecycle overlays.</param>
        /// <exception cref="ArgumentNullException">Either array is null.</exception>
        /// <exception cref="ArgumentException">The arrays have different lengths — a mismatch is a
        /// roster-lifecycle bug (FR-PG-019), never something to iterate to the shorter length.</exception>
        public ClubCareerStates(int clubId, PlayerRecord[] records, PlayerLifecycle[] lifecycles)
        {
            if (records == null)
            {
                throw new ArgumentNullException(nameof(records));
            }

            if (lifecycles == null)
            {
                throw new ArgumentNullException(nameof(lifecycles));
            }

            if (records.Length != lifecycles.Length)
            {
                throw new ArgumentException(
                    $"Club {clubId} has {records.Length} record(s) but {lifecycles.Length} lifecycle "
                    + "overlay(s) — a career block pairs one overlay with every record (FR-PG-019).",
                    nameof(lifecycles));
            }

            ClubId = clubId;
            Records = records;
            Lifecycles = lifecycles;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-08 | —      | #28 T1: the per-club career-state unit the codec encodes and   |
// |         |            |        | ProgressionEngine holds. Keyed by PlayerRecord.PlayerId — no   |
// |         |            |        | parallel id array, unlike ClubTrainingStates, because the      |
// |         |            |        | record carries its own identity.                                |
#endregion
