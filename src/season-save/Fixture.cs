// File:     src/season-save/Fixture.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     Season & Competition Loop #30 §2.2 (data structures); §3.1 (generation); §3.4 (play);
//           Appendix B row 7 (byte layout); Code Standards #20
// Purpose:  One scheduled fixture in the season's serialized schedule (#30 KD-5). The fixture list is
//           the IMMUTABLE schedule; the only thing that changes as a season runs is the Played flag,
//           and the resolved score is recorded on the LeagueTable, never here (§2.2).

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// One fixture in the season schedule (#30 §2.2): a round index, the home and away clubs, and
    /// whether it has been resolved yet.
    /// <para>
    /// <b>Immutability contract (§2.2).</b> The spec calls <c>Fixture</c> a readonly struct whose
    /// <c>Played</c> is "the only mutable-on-play field" — which a readonly struct cannot express
    /// directly. It is resolved here the idiomatic way: the struct stays fully readonly and
    /// <see cref="MarkPlayed"/> returns a NEW value, so the season's fixture array is updated by
    /// slot replacement (<c>fixtures[i] = fixtures[i].MarkPlayed()</c>) rather than in-place field
    /// mutation. That keeps the §3.4 pseudocode's <c>f.Played := true</c> semantics while preserving
    /// the readonly-struct guarantee that a copy handed to a caller can never be aliased into the
    /// stored schedule (the #30 FR-SN-033 observer-neutrality posture).
    /// </para>
    /// <para>
    /// The resolved SCORE is deliberately not carried here: §2.2 records it on the
    /// <see cref="LeagueTable"/>, so the schedule stays a pure plan and the table stays the single
    /// source of truth for results.
    /// </para>
    /// </summary>
    public readonly struct Fixture
    {
        /// <summary>Zero-based round index within the season schedule (first leg rounds
        /// <c>0 .. M-2</c>, second leg offset by <c>M-1</c> — #30 §3.1).</summary>
        public readonly int RoundIndex;

        /// <summary>The home club's <c>ClubId</c>.</summary>
        public readonly int HomeClubId;

        /// <summary>The away club's <c>ClubId</c>.</summary>
        public readonly int AwayClubId;

        /// <summary>True once this fixture has been resolved and applied to the table (#30 §3.4).</summary>
        public readonly bool Played;

        /// <summary>
        /// Constructs a fixture. Fail-loud on a negative round index or a self-fixture
        /// (<c>home == away</c>) — the F2 class, gated here at construction so a malformed fixture can
        /// never enter a schedule (the project's validate-at-the-consuming-seam discipline).
        /// </summary>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="roundIndex"/> is negative.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="homeClubId"/> equals <paramref name="awayClubId"/>.</exception>
        public Fixture(int roundIndex, int homeClubId, int awayClubId, bool played = false)
        {
            if (roundIndex < 0)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(roundIndex), roundIndex, "roundIndex must be non-negative.");
            }

            if (homeClubId == awayClubId)
            {
                throw new System.ArgumentException(
                    $"A fixture cannot be a self-fixture; got homeClubId == awayClubId == {homeClubId}.",
                    nameof(awayClubId));
            }

            RoundIndex = roundIndex;
            HomeClubId = homeClubId;
            AwayClubId = awayClubId;
            Played = played;
        }

        /// <summary>
        /// Returns a copy of this fixture with <see cref="Played"/> set to <c>true</c> — the
        /// slot-replacement form of §3.4's <c>f.Played := true</c>. Idempotent: marking an
        /// already-played fixture returns an equal value.
        /// </summary>
        public Fixture MarkPlayed() => new Fixture(RoundIndex, HomeClubId, AwayClubId, true);

        /// <summary>True when <paramref name="clubId"/> is one of the two clubs in this fixture.</summary>
        public bool Involves(int clubId) => HomeClubId == clubId || AwayClubId == clubId;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-25 | —      | Initial implementation (#30 T0). Readonly struct + MarkPlayed()    |
// |         |            |        | slot-replacement resolving the §2.2 readonly/mutable-Played        |
// |         |            |        | tension; ctor fail-loud on negative round / self-fixture.          |
#endregion
