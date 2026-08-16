// File:     src/discipline/DisciplineEntry.cs
// Created:  2026-08-13
// Modified: 2026-08-16, latest (finding B, doc only — v1.4: the constructor's negative-PlayerId XML doc
//           and runtime message justified the guard by "integer division would otherwise silently
//           derive it to club 0's OnClubFixturePlayed loop" — machinery ERR-044-014 deleted the same
//           day (DisciplineRules.cs v1.7). Restated on grounds that survive: a negative id is an
//           unresolvable identity, MigratePlayerId refuses one as a migration target, and the save
//           codec's/FromEntries' ordering invariant depends on non-negative ids. Also corrected the L2
//           accumulator-arithmetic comment's stale "wiring is outside this file's ownership for this
//           pass" — DisciplineRules.cs v1.10 wired it the same day, and the comment now states the
//           surviving residual honestly: the guards make an overflow throw loud and correctly named,
//           they do not make CardLedgerFold.Commit atomic against it. No behaviour change.)
// Modified: 2026-08-16 (reviewed findings pass, L2 — v1.3: added YellowsPlusOne/BanMatchesPlus, guarded
//           accumulator arithmetic that refuses BEFORE an addition would overflow int.MaxValue and wrap
//           to a negative value the constructor's existing guard would then misreport as "a counting
//           bug". DisciplineRules.AddYellow/AddBan are the intended callers; wiring them is outside this
//           file's ownership for this pass — see CardLedgerFold.cs v1.8's L2 doc for the scoped claim
//           this closes the gap under.)
// Modified: 2026-08-15 (reviewed findings pass, L4 — v1.2: the CompetitionId doc's
//           <see cref="DisciplineConstants.LEAGUE_COMPETITION_KEY"/> reference renamed for that
//           constant's ALL_CAPS -> LeagueCompetitionKey rename (DisciplineConstants.cs v1.5). No
//           behaviour change.)
// Author:   —
// Spec:     Discipline & Suspensions #44 §2.2 (data structures) / §2.3 F2 / FR-DC-012 / FR-DC-017 /
//           FR-DC-020; Code Standards #20
// Purpose:  One player's discipline tally within one competition — the (PlayerId, CompetitionId) →
//           (Yellows, BanMatchesRemaining) row DisciplineState stores and DisciplineSaveCodec writes,
//           plus the guarded arithmetic that grows those two fields without a silent overflow.

using System;

namespace TacticalDirector.Discipline
{
    /// <summary>
    /// One <c>(PlayerId, CompetitionId)</c> discipline row: the yellows accumulated toward the next
    /// accumulation ban, and the club fixtures still to be served (#44 §2.2).
    /// <para>
    /// <b>All integer</b> (FR-DC-020) and immutable — every mutation produces a new row, so no holder
    /// of an entry can become a second writer of <see cref="DisciplineState"/>.
    /// </para>
    /// <para>
    /// <b>An all-zero row is never stored</b> (FR-DC-017's canonical-minimality rule): a
    /// <c>(0, 0)</c> entry and an absent entry would otherwise both be encodable states for the same
    /// situation, and two equivalent runs would serialize different bytes. <see cref="IsEmpty"/> is
    /// the predicate <see cref="DisciplineState"/> drops on, immediately and wherever it arises — at
    /// the season-boundary sweep or mid-season, the tick a ban is served out with no residual yellows.
    /// </para>
    /// </summary>
    public readonly struct DisciplineEntry : IEquatable<DisciplineEntry>
    {
        /// <summary>The player this row belongs to. Globally unique (#27 FR-SQ-010, ERR-027-004).</summary>
        public readonly int PlayerId;

        /// <summary>
        /// The competition partition (FR-DC-012). <see cref="DisciplineConstants.LeagueCompetitionKey"/>
        /// at minimal; a plain <c>int</c> so #44 needs no #43 reference.
        /// </summary>
        public readonly int CompetitionId;

        /// <summary>
        /// Yellows accumulated toward the next accumulation ban. Never negative. <b>NOT necessarily
        /// below the threshold</b> — <c>AddYellow</c> subtracts the threshold exactly once per
        /// crossing (§3.2's residual rule), so a row seeded or decoded above twice the threshold
        /// stays above it after one crossing: 12 + 1 at threshold 5 → 8, still above threshold.
        /// </summary>
        public readonly int Yellows;

        /// <summary>Club fixtures still to be served. Never negative. Carries across <c>RollToNextSeason</c> (FR-DC-017).</summary>
        public readonly int BanMatchesRemaining;

        /// <summary>Builds a row.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="playerId"/> is negative — §2.3
        /// <b>F2</b>, a player outside the resolvable universe: a negative id is an unresolvable
        /// identity that names no real player, <see cref="DisciplineRules.MigratePlayerId"/> itself
        /// refuses one as a migration target for the identical reason (its own F2 gate), and
        /// <see cref="DisciplineSaveCodec"/>'s strictly-ascending <c>(PlayerId, CompetitionId)</c>
        /// ordering invariant — <see cref="DisciplineState.FromEntries"/>'s own copy of the same rule —
        /// depends on every key being non-negative. <paramref name="yellows"/> or
        /// <paramref name="banMatchesRemaining"/> is negative — a negative tally is a counting bug, and
        /// the codec refuses one on the way back in (F3), so it must never be constructible either.</exception>
        public DisciplineEntry(int playerId, int competitionId, int yellows, int banMatchesRemaining)
        {
            if (playerId < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(playerId), playerId,
                    "DisciplineEntry: PlayerId must be >= 0 (§2.3 F2) — a negative id is an " +
                    "unresolvable identity: it names no real player, DisciplineRules.MigratePlayerId " +
                    "refuses one as a migration target for the same reason, and the save codec's " +
                    "strictly-ascending (PlayerId, CompetitionId) ordering invariant depends on every " +
                    "key being non-negative.");
            }
            if (yellows < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(yellows), yellows,
                    "DisciplineEntry: Yellows must be >= 0 (FR-DC-020 / F3) — a negative tally is a counting bug.");
            }
            if (banMatchesRemaining < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(banMatchesRemaining), banMatchesRemaining,
                    "DisciplineEntry: BanMatchesRemaining must be >= 0 (FR-DC-020 / F3) — serving past zero is a counting bug.");
            }

            PlayerId = playerId;
            CompetitionId = competitionId;
            Yellows = yellows;
            BanMatchesRemaining = banMatchesRemaining;
        }

        /// <summary>
        /// True when this row carries nothing — the state FR-DC-017 requires be dropped rather than
        /// stored, so that an absent entry is the ONE representation of "this player is clean".
        /// </summary>
        public bool IsEmpty => Yellows == 0 && BanMatchesRemaining == 0;

        /// <summary>True while the player is suspended (FR-DC-008): a pure read, no threshold arithmetic.</summary>
        public bool IsSuspended => BanMatchesRemaining > 0;

        /// <summary>Value equality across all four fields.</summary>
        public bool Equals(DisciplineEntry other) =>
            PlayerId == other.PlayerId
            && CompetitionId == other.CompetitionId
            && Yellows == other.Yellows
            && BanMatchesRemaining == other.BanMatchesRemaining;

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is DisciplineEntry other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = PlayerId;
                hash = (hash * 397) ^ CompetitionId;
                hash = (hash * 397) ^ Yellows;
                hash = (hash * 397) ^ BanMatchesRemaining;
                return hash;
            }
        }

        // ── L2 (reviewed findings pass, closed in full at DisciplineRules.cs v1.10): guarded
        // accumulator arithmetic ──────────────────────────────────────────────────────────────────
        //
        // DisciplineRules.AddYellow/AddBan read a field off an existing entry, add to it, and construct
        // a new DisciplineEntry from the result. At int.MaxValue that addition wraps silently to a
        // negative number, and THIS type's own constructor guard then refuses it as "a negative tally
        // is a counting bug" — true of the symptom, false of the cause: the tally was never negative
        // until the wrap put it there. These two helpers are the guarded alternative — they check
        // headroom BEFORE the addition, so the entry (and whichever caller reads it) never wraps in the
        // first place. DisciplineRules.AddYellow (both its `+ 1` and its accumulation-ban `+=`) and
        // AddBan now route through them (DisciplineRules.cs v1.10) — every accumulator addition in that
        // file is guarded, not merely most of them. That does NOT make CardLedgerFold.Commit atomic
        // against this class of throw: Commit still applies cards one at a time through
        // DisciplineRules.ApplyCard, so a mid-list OverflowException — now loud and correctly named
        // instead of a misleading "counting bug" — still leaves cards 0..k-1 already applied and
        // _committed still false. The guards close the misattribution, not the partial-application
        // residual; see CardLedgerFold.cs's own L2 doc for that scope.

        /// <summary>
        /// <paramref name="yellows"/> plus one, refusing BEFORE the addition if
        /// <paramref name="yellows"/> is already <see cref="int.MaxValue"/> — the addition would
        /// otherwise wrap silently to a negative value.
        /// </summary>
        /// <exception cref="OverflowException"><paramref name="yellows"/> is already
        /// <see cref="int.MaxValue"/>.</exception>
        internal static int YellowsPlusOne(int yellows)
        {
            if (yellows == int.MaxValue)
            {
                throw new OverflowException(
                    "DisciplineEntry: Yellows is already int.MaxValue (" + int.MaxValue + "); adding " +
                    "one more would overflow silently, wrapping to a negative value the constructor " +
                    "would then misreport as a counting bug rather than what it actually is — an " +
                    "accumulator overflow.");
            }

            return yellows + 1;
        }

        /// <summary>
        /// <paramref name="banMatchesRemaining"/> plus <paramref name="matches"/>, refusing BEFORE the
        /// addition if the sum would exceed <see cref="int.MaxValue"/>. Checked via subtraction
        /// (<c>banMatchesRemaining &gt; int.MaxValue - matches</c>) rather than performing the addition
        /// and inspecting the result, which would itself be the overflow this guard exists to catch.
        /// </summary>
        /// <exception cref="OverflowException">The sum would exceed <see cref="int.MaxValue"/>.</exception>
        internal static int BanMatchesPlus(int banMatchesRemaining, int matches)
        {
            if (matches > 0 && banMatchesRemaining > int.MaxValue - matches)
            {
                throw new OverflowException(
                    "DisciplineEntry: BanMatchesRemaining (" + banMatchesRemaining + ") + matches (" +
                    matches + ") would overflow int.MaxValue (" + int.MaxValue + "), wrapping to a " +
                    "negative value the constructor would then misreport as a counting bug rather " +
                    "than what it actually is — an accumulator overflow.");
            }

            return banMatchesRemaining + matches;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-08-13 | —      | Initial implementation (#44 T0, roadmap C1): the immutable       |
// |         |            |        | (PlayerId, CompetitionId) tally row, with the FR-DC-017          |
// |         |            |        | canonical-minimality predicate and the F3 non-negative gates     |
// |         |            |        | enforced at construction, not only at decode.                    |
// | 1.1     | 2026-08-13 | —      | AR fix (M1): the constructor now refuses a negative PlayerId     |
// |         |            |        | (§2.3 F2) — C# integer division truncates toward zero, so every  |
// |         |            |        | id in [-24, -1] previously derived to club 0 in                  |
// |         |            |        | OnClubFixturePlayed with nothing refusing it. Corrected the      |
// |         |            |        | Yellows XML doc's false "always below the threshold after a      |
// |         |            |        | crossing" claim (L3) — AddYellow subtracts the threshold exactly |
// |         |            |        | once, so a row seeded above twice the threshold stays above it.  |
// | 1.2     | 2026-08-15 | —      | Reviewed findings pass, L4. The CompetitionId doc's <see        |
// |         |            |        | cref="DisciplineConstants.LEAGUE_COMPETITION_KEY"/> reference     |
// |         |            |        | renamed to LeagueCompetitionKey. No behaviour change.             |
// | 1.3     | 2026-08-16 | —      | Reviewed findings pass, L2. Added internal static                 |
// |         |            |        | YellowsPlusOne(int)/BanMatchesPlus(int,int): guarded arithmetic   |
// |         |            |        | that throws OverflowException BEFORE an addition would wrap       |
// |         |            |        | int.MaxValue to a negative value — the constructor's existing     |
// |         |            |        | guard would otherwise refuse the wrapped result as "a negative    |
// |         |            |        | tally is a counting bug", misnaming an overflow as a counting     |
// |         |            |        | bug. DisciplineRules.AddYellow/AddBan are the intended callers;   |
// |         |            |        | that wiring is outside this file's ownership for this pass.       |
// | 1.4     | 2026-08-16, latest | — | Finding B, doc only. The constructor's negative-PlayerId       |
// |         |            |        | XML doc and runtime message cited a packed-id integer-division    |
// |         |            |        | derivation ERR-044-014 deleted from OnClubFixturePlayed the same  |
// |         |            |        | day (DisciplineRules.cs v1.7) — a guard justified by retired      |
// |         |            |        | machinery a future maintainer could delete believing it unneeded. |
// |         |            |        | Restated on surviving grounds: unresolvable identity,             |
// |         |            |        | MigratePlayerId's own F2 refusal of a negative migration target,  |
// |         |            |        | and the save codec's/FromEntries' ordering invariant depending on |
// |         |            |        | non-negative keys. The L2 accumulator-arithmetic comment's stale  |
// |         |            |        | "wiring is outside this file's ownership" note is corrected —     |
// |         |            |        | DisciplineRules.cs v1.10 wired both AddYellow additions and       |
// |         |            |        | AddBan through these helpers the same day — with the surviving    |
// |         |            |        | residual stated honestly: the guards make an overflow throw loud  |
// |         |            |        | and correctly named, not CardLedgerFold.Commit atomic against it. |
// |         |            |        | No behaviour change.                                               |
#endregion
