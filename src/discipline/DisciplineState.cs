// File:     src/discipline/DisciplineState.cs
// Created:  2026-08-13
// Modified: 2026-08-16 (reviewed-findings pass, L1 — v1.1)
// Author:   —
// Spec:     Discipline & Suspensions #44 §2.2 (data structures) / FR-DC-001 / FR-DC-012 /
//           FR-DC-016 / FR-DC-017 / FR-DC-021; Code Standards #20
// Purpose:  #44's only owned mutable state — the sparse (PlayerId, CompetitionId) tally map, held in
//           canonical ascending key order so two equivalent runs always serialize identical bytes.

using System;
using System.Collections.Generic;

namespace TacticalDirector.Discipline
{
    /// <summary>
    /// The per-player discipline tally (#44 §2.2, KD-1). Sparse: a player with nothing to record has
    /// no row at all, which is the ONE representation of "clean" (FR-DC-017).
    /// <para>
    /// <b>This is #44's only owned mutable state</b> (FR-DC-001). Nothing here touches engine state,
    /// #27 <c>Squad</c>/<c>PlayerRecord</c>, or #30 season state, and there is <b>no RNG state field
    /// of any kind</b> (FR-DC-016) — #44 has no draw site anywhere (FR-DC-019).
    /// </para>
    /// <para>
    /// <b>Order is not state.</b> Rows are held sorted by <c>(PlayerId, CompetitionId)</c> and every
    /// read enumerates in that order, so <see cref="DisciplineSaveCodec"/> needs no canonicalisation
    /// pass and FR-DC-021's two-run determinism is structural rather than asserted. Lookup is a binary
    /// search over that order — the shape that burned <c>PlayerCareerStates</c> at its T2 AR (an
    /// unordered block made every lookup miss a player who WAS carried), so the order is enforced on
    /// the way in rather than assumed: see <see cref="FromEntries"/>.
    /// </para>
    /// <para>
    /// <b>Mutation is <c>internal</c> on purpose.</b> <see cref="DisciplineRules"/> is the sole public
    /// mutating entry point (the <c>AdvanceMedicalDay</c> precedent, #41 FR-MD-003) — the thresholds,
    /// the serving decrement and the hygiene rules are the contract, and a caller that could add a
    /// yellow without them could write a state the spec forbids.
    /// </para>
    /// <para>
    /// Season-cadence, never per-tick (the fold reads the tap every tick but writes only on a card),
    /// so the zero-allocation game-loop rules do not apply — the same carve-out <c>Squad</c> takes.
    /// </para>
    /// </summary>
    public sealed class DisciplineState
    {
        // Sorted ascending by (PlayerId, CompetitionId). The invariant every read depends on; the two
        // write paths (Upsert, FromEntries) are the only places it can be established.
        private readonly List<DisciplineEntry> _entries;

        /// <summary>Builds the genesis state — empty, which is what a new career starts from (FR-DC-017).</summary>
        public DisciplineState()
        {
            _entries = new List<DisciplineEntry>();
        }

        private DisciplineState(List<DisciplineEntry> entries)
        {
            _entries = entries;
        }

        /// <summary>Number of rows carried. Zero at genesis, and zero again once every ban is served and every tally cleared.</summary>
        public int Count => _entries.Count;

        /// <summary>
        /// The row at <paramref name="index"/> in canonical ascending <c>(PlayerId, CompetitionId)</c>
        /// order — the enumeration <see cref="DisciplineSaveCodec"/> writes and any view model reads.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside <c>[0, Count)</c>.</exception>
        public DisciplineEntry EntryAt(int index)
        {
            if ((uint)index >= (uint)_entries.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index,
                    "DisciplineState.EntryAt: index must be in [0, Count).");
            }
            return _entries[index];
        }

        /// <summary>
        /// Rebuilds a state from decoded rows, requiring them strictly ascending by
        /// <c>(PlayerId, CompetitionId)</c> and none of them empty.
        /// <para>
        /// Both refusals exist because this is the restore door and every read below is a binary
        /// search: an unordered block would make a lookup miss a player who IS carried, and the caller
        /// would read the miss as "clean" and quietly wipe a season of bans — the exact silent-overwrite
        /// class <c>PlayerCareerStates.FromBlocks</c> filed at its T2 AR pass 1. An empty row would
        /// reopen FR-DC-017's two-representations hazard behind the codec's back.
        /// </para>
        /// <para>The array is copied, not borrowed — a caller keeping its reference must not become a second writer.</para>
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="entries"/> is null.</exception>
        /// <exception cref="ArgumentException">Rows are not strictly ascending, or a row is empty.</exception>
        public static DisciplineState FromEntries(DisciplineEntry[] entries)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            var copy = new List<DisciplineEntry>(entries.Length);
            for (int i = 0; i < entries.Length; i++)
            {
                DisciplineEntry entry = entries[i];
                if (entry.IsEmpty)
                {
                    throw new ArgumentException(
                        "DisciplineState.FromEntries: entry " + i + " (player " + entry.PlayerId +
                        ", competition " + entry.CompetitionId + ") is all-zero. FR-DC-017 makes an " +
                        "absent row the ONE representation of a clean player — storing (0, 0) would " +
                        "give two equivalent runs different bytes.",
                        nameof(entries));
                }
                if (i > 0 && Compare(entries[i - 1], entry) >= 0)
                {
                    throw new ArgumentException(
                        "DisciplineState.FromEntries: entries must be STRICTLY ascending by " +
                        "(PlayerId, CompetitionId); entry " + i + " (" + entry.PlayerId + ", " +
                        entry.CompetitionId + ") does not follow entry " + (i - 1) + " (" +
                        entries[i - 1].PlayerId + ", " + entries[i - 1].CompetitionId + "). Every " +
                        "lookup here is a binary search over that order.",
                        nameof(entries));
                }
                copy.Add(entry);
            }

            return new DisciplineState(copy);
        }

        /// <summary>
        /// The row for <paramref name="playerId"/> in <paramref name="competitionId"/>, or a clean
        /// zero row when the player has none. Pure — the absent case is not an error (FR-DC-008),
        /// including a negative <paramref name="playerId"/> (L1): no row can ever exist for one (the
        /// validating constructor refuses it on every write path), so this returns
        /// <c>default(DisciplineEntry)</c> directly rather than routing through
        /// <see cref="DisciplineEntry"/>'s constructor — which runs the §2.3 F2 guard and throws.
        /// <see cref="HasEntry"/> already reads false for the same key; a doc that promises "the absent
        /// case is not an error" must not let one sibling method throw where the other returns false.
        /// </summary>
        public DisciplineEntry EntryFor(int playerId, int competitionId)
        {
            if (playerId < 0)
            {
                return default;
            }

            int index = IndexOf(playerId, competitionId);
            return index >= 0
                ? _entries[index]
                : new DisciplineEntry(playerId, competitionId, 0, 0);
        }

        /// <summary>True when a row exists for this key. Distinguishes "carried, and clean" — which FR-DC-017 makes unrepresentable — from "absent".</summary>
        public bool HasEntry(int playerId, int competitionId) => IndexOf(playerId, competitionId) >= 0;

        /// <summary>
        /// Writes <paramref name="entry"/> at its canonical position, or removes the row when the
        /// entry is empty (FR-DC-017's immediate drop, applied at the one place every mutation lands
        /// so no rule has to remember it).
        /// </summary>
        internal void Upsert(in DisciplineEntry entry)
        {
            int index = IndexOf(entry.PlayerId, entry.CompetitionId);
            if (entry.IsEmpty)
            {
                if (index >= 0)
                {
                    _entries.RemoveAt(index);
                }
                return;
            }

            if (index >= 0)
            {
                _entries[index] = entry;
            }
            else
            {
                _entries.Insert(~index, entry);
            }
        }

        /// <summary>Removes the row for this key if one exists; returns whether anything was removed.</summary>
        internal bool Remove(int playerId, int competitionId)
        {
            int index = IndexOf(playerId, competitionId);
            if (index < 0)
            {
                return false;
            }
            _entries.RemoveAt(index);
            return true;
        }

        /// <summary>
        /// Binary search over the canonical order. Returns the index when found, otherwise the
        /// bitwise complement of the insertion point (the <see cref="List{T}.BinarySearch(T)"/>
        /// convention <see cref="Upsert"/> relies on).
        /// </summary>
        private int IndexOf(int playerId, int competitionId)
        {
            int lo = 0;
            int hi = _entries.Count - 1;
            while (lo <= hi)
            {
                int mid = lo + ((hi - lo) >> 1);
                DisciplineEntry probe = _entries[mid];
                int cmp = Compare(probe.PlayerId, probe.CompetitionId, playerId, competitionId);
                if (cmp == 0)
                {
                    return mid;
                }
                if (cmp < 0)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }
            return ~lo;
        }

        private static int Compare(in DisciplineEntry a, in DisciplineEntry b) =>
            Compare(a.PlayerId, a.CompetitionId, b.PlayerId, b.CompetitionId);

        private static int Compare(int playerA, int competitionA, int playerB, int competitionB)
        {
            if (playerA != playerB)
            {
                return playerA < playerB ? -1 : 1;
            }
            if (competitionA != competitionB)
            {
                return competitionA < competitionB ? -1 : 1;
            }
            return 0;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-08-13 | —      | Initial implementation (#44 T0, roadmap C1): the sparse tally    |
// |         |            |        | map in canonical key order, with the FR-DC-017 drop applied at   |
// |         |            |        | the single Upsert site and the ascending-order refusal enforced  |
// |         |            |        | at the restore door (the PlayerCareerStates.FromBlocks H1 class). |
// | 1.1     | 2026-08-16 | —      | Reviewed-findings fix (L1): EntryFor no longer routes a negative  |
// |         |            |        | playerId through DisciplineEntry's validating constructor, which  |
// |         |            |        | ran the §2.3 F2 guard and threw ArgumentOutOfRangeException —     |
// |         |            |        | disagreeing with HasEntry(-1, 0), which already reads false for   |
// |         |            |        | the same key, and propagating uncaught out of Availability.       |
// |         |            |        | IsAvailable / MarkSuspended, neither of which declares it. Now    |
// |         |            |        | returns default(DisciplineEntry) directly for a negative key —    |
// |         |            |        | the clean zero row, with no constructor call at all.              |
#endregion
