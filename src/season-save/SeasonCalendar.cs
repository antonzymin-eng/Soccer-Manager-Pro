// File:     src/season-save/SeasonCalendar.cs
// Created:  2026-07-25
// Modified: 2026-07-27
// Author:   —
// Spec:     Season & Competition Loop #30 §2.2, §3.3 (cursor + day advance), Appendix B row 8,
//           FR-SN-009/011, KD-4; Code Standards #20
// Purpose:  The season's fixture-round cursor and the round -> world-day mapping. KD-4: this cursor is
//           SEASON-scoped state, not world time — WorldStore owns WorldClock (the calendar day); this
//           is a discrete pointer into the schedule and lives in the season sub-blob.

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// The fixture calendar (#30 §2.2 / KD-4): which round is next, and which world-day each round is
    /// played on.
    /// <para>
    /// Day values are <c>uint</c> to match <c>WorldStore.CurrentWorldTick</c> (Appendix B row 8 — the
    /// PASS-1 v0.2 correction; §2.2's looser <c>int[]</c> predates it).
    /// </para>
    /// <para>
    /// <b>KD-4 invariant (FR-SN-011).</b> The next fixture day is always <c>&gt;=</c> the current world
    /// day. It is checked with <see cref="SatisfiesCursorInvariant"/> — a pure predicate here, since this
    /// type never sees the world clock. Its production caller is <c>SeasonSaveManager.Load</c> (#30 T1),
    /// the only layer holding both the restored world and the decoded season; it fails loud on violation
    /// (F4).
    /// </para>
    /// </summary>
    public readonly struct SeasonCalendar
    {
        private readonly uint[] _roundToDay;

        /// <summary>The index of the next round to be played. Equals <see cref="RoundCount"/> once the
        /// season is complete (the F5 "cursor past the last round" condition).</summary>
        public readonly int NextRoundIndex;

        private SeasonCalendar(int nextRoundIndex, uint[] roundToDay)
        {
            NextRoundIndex = nextRoundIndex;
            _roundToDay = roundToDay;
        }

        /// <summary>The number of rounds in the season schedule.</summary>
        public int RoundCount => _roundToDay?.Length ?? 0;

        /// <summary>True once every round has been played — the cursor sits past the last round.</summary>
        public bool IsSeasonComplete => NextRoundIndex >= RoundCount;

        /// <summary>
        /// Creates a calendar over an explicit round → world-day mapping.
        /// </summary>
        /// <param name="nextRoundIndex">The cursor. May equal <paramref name="roundToDay"/>.Length
        /// (season complete), but not exceed it and not be negative.</param>
        /// <param name="roundToDay">World-day per round. Must be non-empty and strictly ascending — two
        /// rounds cannot share a day (the cursor would be ambiguous), and a later round cannot fall
        /// earlier than an earlier one.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="roundToDay"/> is null.</exception>
        /// <exception cref="System.ArgumentException">Empty or non-ascending day mapping.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="nextRoundIndex"/> is out of range.</exception>
        public static SeasonCalendar Create(int nextRoundIndex, uint[] roundToDay)
        {
            if (roundToDay == null)
            {
                throw new System.ArgumentNullException(nameof(roundToDay));
            }

            if (roundToDay.Length == 0)
            {
                throw new System.ArgumentException(
                    "A season calendar needs at least one round.", nameof(roundToDay));
            }

            for (int i = 1; i < roundToDay.Length; i++)
            {
                if (roundToDay[i] <= roundToDay[i - 1])
                {
                    throw new System.ArgumentException(
                        $"Round days must be strictly ascending; round {i} is day {roundToDay[i]} but " +
                        $"round {i - 1} is day {roundToDay[i - 1]}.",
                        nameof(roundToDay));
                }
            }

            if (nextRoundIndex < 0 || nextRoundIndex > roundToDay.Length)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(nextRoundIndex), nextRoundIndex,
                    $"nextRoundIndex must be in [0, {roundToDay.Length}] (length = season complete).");
            }

            // Snapshot-copy: the calendar must not alias the caller's array.
            var days = new uint[roundToDay.Length];
            System.Array.Copy(roundToDay, days, roundToDay.Length);
            return new SeasonCalendar(nextRoundIndex, days);
        }

        /// <summary>
        /// Creates a calendar that spaces <paramref name="roundCount"/> rounds evenly, starting at
        /// <paramref name="firstRoundDay"/> and repeating every <paramref name="daysBetweenRounds"/>
        /// days — the Stage-2 "linear calendar" of §1.6.
        /// </summary>
        /// <exception cref="System.ArgumentOutOfRangeException">Non-positive round count or spacing.</exception>
        public static SeasonCalendar Linear(int roundCount, uint firstRoundDay, uint daysBetweenRounds)
        {
            if (roundCount <= 0)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(roundCount), roundCount, "roundCount must be positive.");
            }

            if (daysBetweenRounds == 0)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(daysBetweenRounds), daysBetweenRounds,
                    "daysBetweenRounds must be positive (rounds cannot share a day).");
            }

            // Overflow guard: the last round's day must stay inside uint.
            ulong lastDay = (ulong)firstRoundDay + (ulong)(roundCount - 1) * daysBetweenRounds;
            if (lastDay > uint.MaxValue)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(daysBetweenRounds), daysBetweenRounds,
                    $"The schedule would end on world-day {lastDay}, beyond uint.MaxValue.");
            }

            var days = new uint[roundCount];
            for (int i = 0; i < roundCount; i++)
            {
                days[i] = firstRoundDay + (uint)i * daysBetweenRounds;
            }

            return new SeasonCalendar(0, days);
        }

        /// <summary>The world-day round <paramref name="roundIndex"/> is played on.</summary>
        /// <exception cref="System.ArgumentOutOfRangeException">The round index is out of range.</exception>
        public uint DayOfRound(int roundIndex)
        {
            if (_roundToDay == null || roundIndex < 0 || roundIndex >= _roundToDay.Length)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(roundIndex), roundIndex,
                    $"roundIndex must be in [0, {RoundCount - 1}].");
            }

            return _roundToDay[roundIndex];
        }

        /// <summary>
        /// The world-day of the next unplayed round (§3.3's <c>targetDay</c>).
        /// </summary>
        /// <exception cref="System.InvalidOperationException">The season is complete — there is no next
        /// fixture day; the caller must run the boundary roll first (F5).</exception>
        public uint NextFixtureDay()
        {
            if (IsSeasonComplete)
            {
                throw new System.InvalidOperationException(
                    "The season is complete; there is no next fixture day. Run the boundary roll first.");
            }

            return _roundToDay[NextRoundIndex];
        }

        /// <summary>
        /// The KD-4 / FR-SN-011 invariant: the next fixture day is at or after
        /// <paramref name="currentWorldDay"/>. Vacuously true for a completed season (no next fixture).
        /// Checked on restore, which fails loud on violation (F4).
        /// </summary>
        public bool SatisfiesCursorInvariant(uint currentWorldDay) =>
            IsSeasonComplete || _roundToDay[NextRoundIndex] >= currentWorldDay;

        /// <summary>
        /// This calendar's round spacing shifted forward into the next season (#30 §3.5 step (c′)): every
        /// round day moves by one season length plus <paramref name="breakDays"/>, and the cursor returns
        /// to round 0. The first round of the new season therefore lands exactly
        /// <paramref name="breakDays"/> after the last round of this one.
        /// <para>
        /// <b>Shifting the shape, not rebuilding a linear calendar</b> (ERR-030-015). It keeps the
        /// boundary roll a pure function of the season it is rolling (KD-6) AND preserves a non-uniform
        /// schedule — a calendar with a mid-season break keeps that break next year instead of being
        /// silently flattened. Adding one constant to a strictly-ascending sequence preserves strict
        /// ascent, so the result needs no re-validation and is built through the private constructor
        /// without a second array copy.
        /// </para>
        /// <para>
        /// The caller supplies the close-season length rather than this type reading it, so the calendar
        /// owns the arithmetic and the season loop owns the <c>[GT]</c> policy
        /// (<c>SeasonLoopConstants.SeasonBreakDays</c>).
        /// </para>
        /// </summary>
        /// <param name="breakDays">Calendar days between this season's last round and the next season's
        /// first. Must be positive: at zero a single-round calendar would shift by nothing and reproduce
        /// itself, giving a "next" season identical to this one.</param>
        /// <exception cref="System.InvalidOperationException">This calendar maps no rounds.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="breakDays"/> is zero, or
        /// the shift would carry the final round past <c>uint.MaxValue</c>.</exception>
        public SeasonCalendar ShiftedToNextSeason(uint breakDays)
        {
            if (_roundToDay == null || _roundToDay.Length == 0)
            {
                throw new System.InvalidOperationException(
                    "Cannot roll a season whose calendar maps no rounds.");
            }

            if (breakDays == 0)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(breakDays), breakDays,
                    "The close season must be at least one day, or the next season's calendar could "
                    + "coincide with this one's.");
            }

            uint firstDay = _roundToDay[0];
            uint lastDay = _roundToDay[_roundToDay.Length - 1];

            // Season length measured end-to-end, plus the close season. Days are strictly ascending, so
            // the subtraction cannot wrap and lastDay is the largest value in the mapping.
            ulong period = (ulong)(lastDay - firstDay) + breakDays;

            if ((ulong)lastDay + period > uint.MaxValue)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(breakDays), breakDays,
                    $"Shifting the calendar forward by {period} days would carry the final round past "
                    + "world-day uint.MaxValue.");
            }

            var days = new uint[_roundToDay.Length];
            for (int i = 0; i < days.Length; i++)
            {
                days[i] = (uint)(_roundToDay[i] + period);
            }

            return new SeasonCalendar(0, days);
        }

        /// <summary>Returns this calendar with the cursor advanced one round (the §3.4 tail step).</summary>
        /// <exception cref="System.InvalidOperationException">The season is already complete (F5).</exception>
        public SeasonCalendar AdvancedOneRound()
        {
            if (IsSeasonComplete)
            {
                throw new System.InvalidOperationException(
                    "Cannot advance the cursor past the end of the season; run the boundary roll first.");
            }

            return new SeasonCalendar(NextRoundIndex + 1, _roundToDay);
        }

        /// <summary>The round → world-day mapping, as a copy (the T1 encode path / observation).</summary>
        public uint[] RoundDaysCopy()
        {
            if (_roundToDay == null)
            {
                return System.Array.Empty<uint>();
            }

            var copy = new uint[_roundToDay.Length];
            System.Array.Copy(_roundToDay, copy, _roundToDay.Length);
            return copy;
        }

        /// <summary>Field-identity comparison — the T1 round-trip assertion.</summary>
        public bool FieldsEqual(in SeasonCalendar other)
        {
            if (NextRoundIndex != other.NextRoundIndex || RoundCount != other.RoundCount)
            {
                return false;
            }

            for (int i = 0; i < RoundCount; i++)
            {
                if (_roundToDay[i] != other._roundToDay[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-25 | —      | Initial implementation (#30 T0): readonly cursor struct, strictly  |
// |         |            |        | ascending day validation, Linear factory with overflow guard,      |
// |         |            |        | SatisfiesCursorInvariant (KD-4), AdvancedOneRound, FieldsEqual.    |
// | 1.1     | 2026-07-25 | —      | AR pass 2 (doc): SatisfiesCursorInvariant's caller re-anchored to  |
// |         |            |        | SeasonSaveManager.Load — SeasonLoop is a T2 type, and the          |
// |         |            |        | predicate had no production caller until the FR-SN-011 gate landed.|
// | 1.2     | 2026-07-27 | —      | #30 T3 AR: ShiftedToNextSeason — the §3.5 step (c′) calendar       |
// |         |            |        | shift, moved here from SeasonLoop, where it was pure calendar      |
// |         |            |        | arithmetic sitting on the composition root next to nothing else    |
// |         |            |        | that understood round days. Owning it here also drops two array    |
// |         |            |        | copies (RoundDaysCopy + Create's snapshot) and the redundant       |
// |         |            |        | ascending re-validation: adding one constant to a strictly-        |
// |         |            |        | ascending sequence provably preserves strict ascent, so the        |
// |         |            |        | private constructor is reached directly. Plus a positive-breakDays |
// |         |            |        | gate — at zero a single-round calendar reproduces itself.          |
#endregion
