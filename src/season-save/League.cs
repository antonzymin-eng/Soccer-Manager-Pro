// File:     src/season-save/League.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     League Bootstrap design supplement (docs/tracking/league-bootstrap-design.md) KD-9;
//           Season & Competition Loop #30 §2.2 (SeasonState.CreateNew); Squad/Player Data #27;
//           Code Standards #20
// Purpose:  The immutable product of LeagueBootstrap.Generate — the clubs, their rosters, and the
//           derived season seed — and the ISquadProvider the match engine and #30 T2's
//           AdvanceAndPlayNextRound both consume.

using System.Collections.ObjectModel;

using TacticalDirector.MatchEngine;
using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// A bootstrapped league: <c>N</c> clubs with contiguous ids <c>0 .. N-1</c>, one 25-player
    /// <see cref="Squad"/> each, and the derived season seed the fixture schedule is generated from
    /// (design KD-9).
    /// <para>
    /// Immutable after construction: the club and squad arrays are snapshot-copied in, every accessor
    /// hands back a value copy or a <see cref="ReadOnlyCollection{T}"/>, and <see cref="Squad"/> itself
    /// snapshot-copies its players — so the recurring live-array aliasing defect class (living-world
    /// slice-2 AR-1 M-1, match-viewer AR-3 M-1, #26 T0 AR-1, #30 T0 H-1) cannot recur here.
    /// </para>
    /// <para>
    /// Implements <see cref="ISquadProvider"/>, so one league instance serves
    /// <c>MatchEngine.RestoreFromSnapshot</c>, <c>SeasonSaveManager.Load</c> and (at roadmap A4)
    /// <c>AdvanceAndPlayNextRound</c> without an adapter.
    /// </para>
    /// </summary>
    public sealed class League : ISquadProvider
    {
        private readonly Club[] _clubs;
        private readonly Squad[] _squads;   // index == ClubId (KD-2 contiguous ids)

        /// <summary>The seed the whole league was generated from (design KD-4).</summary>
        public ulong WorldSeed { get; }

        /// <summary>
        /// The domain-separated seed derived from <see cref="WorldSeed"/> for the fixture schedule
        /// (design KD-4). <see cref="CreateSeason"/> passes it to <see cref="SeasonState.CreateNew"/>;
        /// it is also the key input the roadmap-A4 round-resolution model draws from (KD-7).
        /// </summary>
        public ulong SeasonSeed { get; }

        /// <summary>The number of clubs.</summary>
        public int ClubCount => _clubs.Length;

        /// <summary>
        /// Constructs a league. Internal: <see cref="LeagueBootstrap.Generate"/> is the only production
        /// producer, which keeps the "ids are contiguous <c>0 .. N-1</c>" invariant (KD-2) — relied on by
        /// <see cref="ResolveByClubId"/>'s O(1) indexing — impossible to violate from outside.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">A required array is null.</exception>
        /// <exception cref="System.ArgumentException">The arrays disagree in length, are empty, or a
        /// club/squad pair is not at its own <c>ClubId</c> index.</exception>
        internal League(ulong worldSeed, ulong seasonSeed, Club[] clubs, Squad[] squads)
        {
            if (clubs == null)
            {
                throw new System.ArgumentNullException(nameof(clubs));
            }

            if (squads == null)
            {
                throw new System.ArgumentNullException(nameof(squads));
            }

            if (clubs.Length != squads.Length)
            {
                throw new System.ArgumentException(
                    $"{clubs.Length} clubs but {squads.Length} squads.", nameof(squads));
            }

            if (clubs.Length < 2)
            {
                throw new System.ArgumentException(
                    $"A league needs at least 2 clubs; got {clubs.Length}.", nameof(clubs));
            }

            // Copy first, then validate the copy — the caller cannot invalidate a check we passed.
            var clubCopy = new Club[clubs.Length];
            System.Array.Copy(clubs, clubCopy, clubs.Length);
            var squadCopy = new Squad[squads.Length];
            System.Array.Copy(squads, squadCopy, squads.Length);

            for (int i = 0; i < clubCopy.Length; i++)
            {
                if (clubCopy[i].ClubId != i)
                {
                    throw new System.ArgumentException(
                        $"clubs[{i}] carries ClubId {clubCopy[i].ClubId}; a league's ids must be the " +
                        "contiguous range 0..N-1 in index order (KD-2).",
                        nameof(clubs));
                }

                if (squadCopy[i] == null)
                {
                    throw new System.ArgumentException($"squads[{i}] is null.", nameof(squads));
                }

                if (squadCopy[i].ClubId != i)
                {
                    throw new System.ArgumentException(
                        $"squads[{i}] carries ClubId {squadCopy[i].ClubId}, not {i}.", nameof(squads));
                }
            }

            WorldSeed = worldSeed;
            SeasonSeed = seasonSeed;
            _clubs = clubCopy;
            _squads = squadCopy;
        }

        /// <summary>The clubs in ascending <c>ClubId</c> order, as read-only value copies.</summary>
        public ReadOnlyCollection<Club> Clubs
        {
            get
            {
                var copy = new Club[_clubs.Length];
                System.Array.Copy(_clubs, copy, _clubs.Length);
                return new ReadOnlyCollection<Club>(copy);
            }
        }

        /// <summary>The club ids, ascending — the set <see cref="SeasonState"/> is built over.</summary>
        public int[] ClubIds()
        {
            var ids = new int[_clubs.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                ids[i] = _clubs[i].ClubId;
            }

            return ids;
        }

        /// <summary>A copy of one club's identity.</summary>
        /// <exception cref="System.ArgumentOutOfRangeException">No such club.</exception>
        public Club ClubAt(int clubId)
        {
            if ((uint)clubId >= (uint)_clubs.Length)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(clubId), clubId, $"clubId must be in [0, {_clubs.Length - 1}].");
            }

            return _clubs[clubId];
        }

        /// <summary>
        /// <see cref="ISquadProvider"/>: the roster for <paramref name="clubId"/>, or <c>null</c> if
        /// this league has no such club (F4 — the interface's contract is to return null so the
        /// caller's own gate fails loud rather than substituting a default roster).
        /// </summary>
        public Squad ResolveByClubId(int clubId)
        {
            if ((uint)clubId >= (uint)_squads.Length)
            {
                return null;
            }

            return _squads[clubId];
        }

        /// <summary>
        /// Builds a startable season over this league (design KD-9) — the "hands #30 a starting world"
        /// step. Delegates to <see cref="SeasonState.CreateNew"/>; the bootstrap owns the club set and
        /// the seed derivation, not a second season constructor.
        /// </summary>
        /// <param name="managedClubId">The human manager's club (FR-SN-013b).</param>
        /// <param name="objective">The board's objective; defaults to "finish in the top half".</param>
        /// <exception cref="System.ArgumentException"><paramref name="managedClubId"/> is not in this
        /// league (F5 — delegated to <see cref="SeasonState"/>'s own club-set gate).</exception>
        public SeasonState CreateSeason(int managedClubId, BoardObjective? objective = null)
        {
            BoardObjective boardObjective = objective ?? DefaultObjective();

            return SeasonState.CreateNew(
                ClubIds(),
                managedClubId,
                SeasonSeed,
                boardObjective,
                LeagueBootstrapConstants.FirstRoundDay,
                LeagueBootstrapConstants.DaysBetweenRounds);
        }

        /// <summary>
        /// The default board objective: finish in the top half, i.e. at or above position
        /// <c>ceil(N/2)</c> (design KD-9).
        /// </summary>
        public BoardObjective DefaultObjective() => new BoardObjective((_clubs.Length + 1) / 2);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-25 | —      | Initial implementation (roadmap A3): immutable league product,     |
// |         |            |        | ISquadProvider, CreateSeason over SeasonState.CreateNew.           |
#endregion
