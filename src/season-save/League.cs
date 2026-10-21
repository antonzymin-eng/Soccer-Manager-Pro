// File:     src/season-save/League.cs
// Created:  2026-07-25
// Modified: 2026-09-11 (#40 T2b review — CreateLoop now preserves already-composed career,
//           progression and discipline state instead of bootstrapping finances by dropping them.)
// Modified: 2026-09-11 (#40 T2b — League.CreateLoop is the #30-owned new-game composition seam that
//           bootstraps exactly one finance entry per canonical squad and passes it into SeasonLoop.)
// Modified: 2026-10-20 (AR round 4 S4-L2 — v1.6: the KD-4 refusal is SeasonCalendar.RequireCursorInvariant.)
// Modified: 2026-10-20 (AR round 3 S-L5 — v1.5: CreateLoop checks the KD-4 cursor invariant before deriving the #31 window.)
// Modified: 2026-10-20 (ERR-031-011 — v1.4: genesis window anchored to season 1's opening fixture day.)
// Modified: 2026-10-20 (#31 T2 genesis contract seeding in CreateLoop — v1.3)
// Author:   —
// Spec:     League Bootstrap design supplement (docs/tracking/league-bootstrap-design.md) KD-9;
//           Season & Competition Loop #30 §2.2 (SeasonState.CreateNew), §4.3;
//           Club Finances & Economy #40 FR-FN-002/025/027, §4.1-§4.3, §7.1 T2b;
//           Squad/Player Data #27; Code Standards #20
// Purpose:  The immutable product of LeagueBootstrap.Generate — the clubs, their rosters, and the
//           derived season seed — and the ISquadProvider the match engine and #30 T2's
//           AdvanceAndPlayNextRound both consume.

using System.Collections.ObjectModel;

using TacticalDirector.ClubFinances;
using TacticalDirector.Discipline;
using TacticalDirector.LivingWorld;
using TacticalDirector.MatchEngine;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.PlayerProgression;
using TacticalDirector.Transfers;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// A bootstrapped league: <c>N</c> clubs with contiguous ids <c>0 .. N-1</c>, one 23-player
    /// <see cref="Squad"/> each (the 3/8/7/5 bootstrap template, below the 25-player cap so transfers
    /// have room — ERR-031-004), and the derived season seed the fixture schedule is generated from
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
        /// Creates the canonical new-game <see cref="SeasonLoop"/> for this league and bootstraps #40
        /// exactly once from this league's canonical #27 squad identities (FR-FN-025). Restored games
        /// do not use this method; they thread their persisted finance block through
        /// <see cref="SeasonLoop.Restore"/>, so bootstrap can never overwrite resumed state.
        /// <para>
        /// The finance bootstrap is additive to the already-composed runtime, not a replacement for it:
        /// optional career, progression and discipline state are forwarded to <see cref="SeasonLoop"/>.
        /// When a populated progression store is supplied it remains the roster authority; otherwise a
        /// supplied career reads this league's canonical squads, matching the pre-#40 composition.
        /// </para>
        /// </summary>
        /// <param name="world">The world created from the same world seed as this league.</param>
        /// <param name="managedClubId">The human manager's club.</param>
        /// <param name="mode">How fixtures are resolved.</param>
        /// <param name="objective">Optional board objective; defaults through <see cref="CreateSeason"/>.</param>
        /// <param name="careerOrNull">Existing #29/#41 career state, if already composed.</param>
        /// <param name="progressionOrNull">Existing #28 progression/roster authority, if already composed.</param>
        /// <param name="disciplineOrNull">Existing #44 discipline state, if already composed.</param>
        /// <returns>A new loop with one initialized finance entry per club, all supplied subsystems preserved,
        /// and — when a populated progression store is supplied — the managed club's #31 contracts seeded
        /// at the genesis terms (FR-TX-028).</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="world"/> is null.</exception>
        /// <exception cref="System.ArgumentException">The world and league seeds disagree, the world clock
        /// is already past season 1's opening fixture day (the KD-4 cursor invariant — thrown before any
        /// #31 window is derived), or a supplied subsystem violates <see cref="SeasonLoop"/>'s existing
        /// composition invariants.</exception>
        public SeasonLoop CreateLoop(
            WorldStore world,
            int managedClubId,
            RoundResolutionMode mode = RoundResolutionMode.ManagedThroughEngine,
            BoardObjective? objective = null,
            PlayerCareerStates careerOrNull = null,
            ProgressionEngine progressionOrNull = null,
            DisciplineState disciplineOrNull = null)
        {
            if (world == null)
            {
                throw new System.ArgumentNullException(nameof(world));
            }

            if (world.WorldSeed != WorldSeed)
            {
                throw new System.ArgumentException(
                    $"World seed {world.WorldSeed} does not match league world seed {WorldSeed}; the "
                    + "new-game composition must use one deterministic bootstrap source.",
                    nameof(world));
            }

            ClubFinanceEntry[] finances = ClubFinanceEntry.CreateInitialForSquads(_squads);
            bool progressionIsRoster = progressionOrNull != null && progressionOrNull.ClubCount > 0;
            ISquadProvider careerSquads = careerOrNull != null && !progressionIsRoster ? this : null;

            // #31 T2 genesis (FR-TX-028 / §3.8): a career with a #28 roster store gets the managed club's
            // contracts seeded HERE and only here — a load decodes the persisted block and never seeds.
            // Transfers re-home records in that store (KD-4), so without one #31 stays unwired. The first
            // summer window opens on the new-game day and closes SummerWindowInSeasonDays into season 1,
            // counted from its opening fixture day (ERR-031-011) — the same anchor the roll's (d″) uses.
            SeasonState season = CreateSeason(managedClubId, objective);

            // The KD-4 cursor invariant is checked BEFORE the window is derived (AR round 3 S-L5): with the
            // clock already past season 1's opening fixture day, SummerWindowFor would throw its own
            // ArgumentOutOfRangeException (opening < begin) and mask the real cause — and SeasonLoop's
            // constructor would throw this same ArgumentException anyway when #31 is not wired.
            season.Calendar.RequireCursorInvariant(world.CurrentWorldTick, nameof(world));

            TransfersState transfers = null;
            if (progressionIsRoster)
            {
                Squad managedSquad = progressionOrNull.SquadFor(managedClubId);
                if (managedSquad != null)
                {
                    var ids = new int[managedSquad.Count];
                    for (int i = 0; i < ids.Length; i++)
                    {
                        ids[i] = managedSquad.GetPlayer(i).PlayerId;
                    }

                    transfers = TransfersState.CreateAtGenesis(
                        managedClubId,
                        TransferWindow.SummerWindowFor(world.CurrentWorldTick, season.Calendar.DayOfRound(0)),
                        ids);
                }
            }

            return new SeasonLoop(
                world,
                season,
                mode,
                careerOrNull,
                careerSquads,
                progressionOrNull,
                disciplineOrNull,
                finances,
                transfers);
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
// | 1.1     | 2026-09-11 | —      | #40 T2b: CreateLoop is the new-game lifecycle owner and invokes   |
// |         |            |        | CreateInitialForSquads exactly once before composing SeasonLoop.   |
// | 1.2     | 2026-09-11 | —      | Review: CreateLoop forwards career/progression/discipline state,   |
// |         |            |        | so enabling #40 cannot disable already-live subsystems.            |
// | 1.3     | 2026-10-20 | —      | #31 T2 genesis (FR-TX-028 / §3.8): CreateLoop seeds the managed club's contracts at the genesis terms whenever a populated #28 store is supplied; first window opens on the new-game day (ERR-031-008). |
// | 1.4     | 2026-10-20 | —      | ERR-031-011 (AR round 2 M6): genesis window = SummerWindowFor(new-game day, season 1 opening fixture day), so its close is anchored like the roll's; class summary says 23-player squads (ERR-031-004). |
// | 1.5     | 2026-10-20 | —      | AR round 3 S-L5: CreateLoop throws the KD-4 cursor-invariant ArgumentException (ParamName "world") before deriving the #31 window when the clock is past season 1's opening fixture day (previously the window's ArgumentOutOfRangeException, or KD-4's with a different ParamName when unwired). |
// | 1.6     | 2026-10-20 | —      | AR round 4 S4-L2: the KD-4 cursor-invariant refusal delegates to SeasonCalendar.RequireCursorInvariant (one predicate and message shared with SeasonLoop's constructor); ParamName stays "world". |
#endregion
