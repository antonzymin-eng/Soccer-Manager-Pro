// File:     src/client-app/CareerSession.cs
// Created:  2026-10-18
// Modified: 2026-10-30
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P7 (D-P7-1..D-P7-5), roadmap C3/C4,
//           Season & Competition Loop #30 §3.3–§3.5 / §4, league-bootstrap-design.md KD-9,
//           Player Progression #28 KD-4, Code Standards #20
// Purpose:  The client's owner of one running career: the canonical new-game composition, resume from
//           a save slot, the day-granular progress command, autosave after every round and season roll,
//           and manual saves. It composes #30's public seams and decides nothing #30 already decides.

using System;
using System.IO;

using TacticalDirector.Discipline;
using TacticalDirector.LivingWorld;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.PlayerProgression;
using TacticalDirector.SeasonSave;

namespace TacticalDirector.ClientApp
{
    /// <summary>
    /// One running career (§5-P7). Created by <see cref="StartNew"/> (C4: league bootstrap → season
    /// start → autosave slot) or <see cref="Resume"/> / <see cref="ContinueLatest"/>; progressed only
    /// through <see cref="Execute"/>, which accepts exactly the action <see cref="CareerProgress.NextAction"/>
    /// reports, so the client cannot offer what #30 would refuse.
    /// <para>
    /// <b>Composition.</b> New game is the canonical full career: <c>LeagueBootstrap</c> → #28
    /// <c>ProgressionEngine.SeedFrom</c> (the roster authority, KD-4) → #29/#41 career state read
    /// through the progression projection with the production injury dial ARMED (FR-MD-027) → a fresh
    /// #44 tally → <c>League.CreateLoop</c>, which bootstraps #40 once (FR-FN-025). Resume threads every
    /// block <c>SeasonSaveManager.Load</c> returns back in verbatim; the league is regenerated from the
    /// saved world seed only for club identity (names), never for rosters.
    /// </para>
    /// <para>
    /// <b>Saves.</b> The autosave slot is written when the career is created and after every played
    /// round and season roll (D-P7-2). An autosave failure does not undo the step — the sim has already
    /// advanced — and is held in <see cref="LastAutosaveError"/> until the next successful write, so a
    /// failed save is never a transient-only message (ux-experience-architecture §7 rule 5).
    /// </para>
    /// Not thread-safe. <see cref="IsBusy"/> refuses re-entry while an operation runs, which is the
    /// duplicate-invocation suppression §7 rule 2 asks for.
    /// </summary>
    public sealed class CareerSession
    {
        private readonly CareerSaveSlots _slots;
        private readonly League _league;
        private readonly SeasonLoop _loop;
        private readonly ulong _worldSeed;

        private CareerSession(CareerSaveSlots slots, League league, SeasonLoop loop, ulong worldSeed, CareerChange first)
        {
            _slots = slots;
            _league = league;
            _loop = loop;
            _worldSeed = worldSeed;
            LastChange = first;
        }

        /// <summary>The career's season loop — the read surface screens project from.</summary>
        public SeasonLoop Loop => _loop;

        /// <summary>The career's league identity (club ids and names). Rosters come from <see cref="SeasonLoop.Progression"/>.</summary>
        public League League => _league;

        /// <summary>The career's world seed.</summary>
        public ulong WorldSeed => _worldSeed;

        /// <summary>The slot store this career saves into.</summary>
        public CareerSaveSlots Slots => _slots;

        /// <summary>The managed club's id.</summary>
        public int ManagedClubId => _loop.State.ManagedClubId;

        /// <summary>The managed club's display name.</summary>
        public string ManagedClubName => ClubName(ManagedClubId);

        /// <summary>The most recent operation and its #30 outputs.</summary>
        public CareerChange LastChange { get; private set; }

        /// <summary>
        /// Incremented after every operation that can change what a screen shows (progress, save,
        /// autosave). Sources cache their projection on it, so polling <c>Project()</c> every frame
        /// allocates nothing between operations.
        /// </summary>
        public int Revision { get; private set; }

        /// <summary>True while an operation is running; a second invocation throws.</summary>
        public bool IsBusy { get; private set; }

        /// <summary>The last autosave's message if it failed, else null. Cleared by the next successful autosave.</summary>
        public string LastAutosaveError { get; private set; }

        /// <summary>The one progress action legal now.</summary>
        public CareerProgressAction NextAction => CareerProgress.NextAction(_loop);

        /// <summary>
        /// Starts a new career (C4): generates the league from <paramref name="worldSeed"/>, composes
        /// the full career for <paramref name="managedClubId"/>, and writes the autosave slot.
        /// </summary>
        /// <param name="worldSeed">The world seed. Supplied by the caller: the host binding is where
        /// entropy may enter, never this assembly (no wall clock or System.Random in game logic).</param>
        /// <param name="managedClubId">The player's club, one of the generated league's ids.</param>
        /// <param name="slots">The slot store to autosave into.</param>
        /// <param name="mode">How rounds resolve; the FR-SN-013b default runs the managed fixture
        /// through the engine.</param>
        /// <exception cref="ArgumentNullException"><paramref name="slots"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="managedClubId"/> is not in the generated league.</exception>
        public static CareerSession StartNew(
            ulong worldSeed,
            int managedClubId,
            CareerSaveSlots slots,
            RoundResolutionMode mode = RoundResolutionMode.ManagedThroughEngine)
        {
            if (slots == null)
            {
                throw new ArgumentNullException(nameof(slots));
            }

            League league = LeagueBootstrap.Generate(worldSeed);
            RequireClub(league, managedClubId);

            var world = new WorldStore(ClientAppConstants.CAREER_MANAGER_ID, worldSeed);
            int[] clubIds = league.ClubIds();
            var day0Squads = new Squad[clubIds.Length];
            for (int i = 0; i < clubIds.Length; i++)
            {
                day0Squads[i] = league.ResolveByClubId(clubIds[i]);
            }

            ProgressionEngine progression = ProgressionEngine.SeedFrom(day0Squads, world.CurrentWorldTick);
            PlayerCareerStates career = PlayerCareerStates.ForLeague(
                new ProgressionSquads(progression), clubIds, injuryOccurrenceEnabled: true);
            SeasonLoop loop = league.CreateLoop(
                world, managedClubId, mode, null, career, progression, new DisciplineState());

            var session = new CareerSession(
                slots, league, loop, worldSeed, CareerChange.Plain(CareerChangeKind.Started, loop.CurrentWorldDay));
            session.Autosave();
            return session;
        }

        /// <summary>
        /// Resumes the career saved in <paramref name="slot"/>. A <see cref="CareerSlotState.Ready"/>
        /// slot whose save then fails #30's load (decode or coherence gates), holds a mid-match save,
        /// or fails to rebuild into a career is invalidated (<see cref="CareerSaveSlots.MarkDamaged"/>,
        /// reversible) before the failure propagates, so it cannot keep winning Continue. A read that
        /// fails on IO is left alone, as it may succeed on retry. Caller arguments are validated first,
        /// so a caller error never invalidates a healthy slot.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="slots"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="slot"/> or
        /// <paramref name="mode"/> is not defined.</exception>
        /// <exception cref="InvalidOperationException">The slot is not loadable, or it holds a mid-match
        /// save (the client never writes one; a supported mid-match resume needs the interactive
        /// career-fixture seam, D-P7-6).</exception>
        public static CareerSession Resume(
            CareerSaveSlots slots,
            CareerSaveSlot slot,
            RoundResolutionMode mode = RoundResolutionMode.ManagedThroughEngine)
        {
            if (slots == null)
            {
                throw new ArgumentNullException(nameof(slots));
            }

            if (!Enum.IsDefined(typeof(RoundResolutionMode), mode))
            {
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Undefined RoundResolutionMode.");
            }

            // Describe also rejects an undefined slot. Past this point every input to the rebuild comes
            // from the save, so a failure inside the try is the save's.
            CareerSlotSummary summary = slots.Describe(slot);
            if (!summary.IsLoadable)
            {
                throw new InvalidOperationException(
                    "Slot " + slot + " is " + summary.State + " and cannot be loaded.");
            }

            try
            {
                SeasonSaveContents contents = slots.Load(slot);
                if (contents.Match != null)
                {
                    throw new InvalidOperationException(
                        "Slot " + slot + " holds a mid-match save; the career client cannot resume a match (§5-P7 D-P7-6).");
                }

                return Rebuild(slots, contents, mode);
            }
            catch (Exception e) when (e is InvalidDataException || e is InvalidOperationException
                || e is ArgumentException || e is FormatException)
            {
                slots.MarkDamaged(slot);
                throw;
            }
        }

        // A failure here may be this build's code rather than the file, but left Ready the slot would
        // keep winning Continue and failing the same way; invalidation is reversible, so it is cheap.
        private static CareerSession Rebuild(CareerSaveSlots slots, SeasonSaveContents contents, RoundResolutionMode mode)
        {
            ulong worldSeed = contents.World.WorldSeed;
            League league = LeagueBootstrap.Generate(worldSeed, contents.Season.ClubCount);
            PlayerCareerStates career = PlayerCareerStates.FromBlocks(
                contents.TrainingClubs, contents.MedicalClubs, contents.AppearanceClubs, injuryOccurrenceEnabled: true);
            var loop = new SeasonLoop(
                contents.World,
                contents.Season,
                mode,
                career,
                null,
                contents.Progression,
                contents.Discipline,
                contents.Finances,
                contents.Transfers);

            return new CareerSession(
                slots, league, loop, worldSeed, CareerChange.Plain(CareerChangeKind.Resumed, loop.CurrentWorldDay));
        }

        /// <summary>Resumes the most recently written loadable slot (Continue, D-P7-2).</summary>
        /// <exception cref="InvalidOperationException">No slot is loadable.</exception>
        public static CareerSession ContinueLatest(
            CareerSaveSlots slots,
            RoundResolutionMode mode = RoundResolutionMode.ManagedThroughEngine)
        {
            if (slots == null)
            {
                throw new ArgumentNullException(nameof(slots));
            }

            if (!slots.TryGetLatest(out CareerSlotSummary latest))
            {
                throw new InvalidOperationException("There is no loadable career save to continue.");
            }

            return Resume(slots, latest.Slot, mode);
        }

        /// <summary>Executes the legal progress action (shorthand for <c>Execute(NextAction)</c>).</summary>
        public CareerChange Progress() => Execute(NextAction);

        /// <summary>
        /// Executes <paramref name="action"/>, which must equal <see cref="NextAction"/>, and autosaves
        /// after a played round or a season roll.
        /// </summary>
        /// <exception cref="InvalidOperationException">An operation is already running, or
        /// <paramref name="action"/> is not the legal action now.</exception>
        public CareerChange Execute(CareerProgressAction action)
        {
            BeginOperation(nameof(Execute));
            try
            {
                CareerProgressAction legal = CareerProgress.NextAction(_loop);
                if (action != legal)
                {
                    throw new InvalidOperationException(
                        "Progress action " + action + " is not legal now; the legal action is " + legal + ".");
                }

                CareerChange change;
                switch (action)
                {
                    case CareerProgressAction.AdvanceDay:
                        _loop.AdvanceDays(1);
                        change = CareerChange.Plain(CareerChangeKind.DayAdvanced, _loop.CurrentWorldDay);
                        break;
                    case CareerProgressAction.PlayMatchday:
                        MatchResult[] results = _loop.AdvanceAndPlayNextRound();
                        change = CareerChange.RoundPlayed(_loop.CurrentWorldDay, results, ManagedClubId);
                        break;
                    case CareerProgressAction.StartNextSeason:
                        SeasonRollOutcome roll = _loop.RollToNextSeason();
                        change = CareerChange.SeasonRolled(_loop.CurrentWorldDay, in roll);
                        break;
                    default:
                        throw new InvalidOperationException("Unreachable: " + action + " passed the legality check.");
                }

                LastChange = change;
                if (action != CareerProgressAction.AdvanceDay)
                {
                    Autosave();
                }

                return change;
            }
            finally
            {
                IsBusy = false;
                Revision++;
            }
        }

        /// <summary>Saves the career to a manual slot.</summary>
        /// <exception cref="ArgumentException"><paramref name="slot"/> is the autosave slot (client-owned).</exception>
        /// <exception cref="InvalidOperationException">An operation is already running.</exception>
        /// <remarks>IO failures propagate: a manual save is a direct request whose failure the caller reports.</remarks>
        public CareerSlotSummary SaveTo(CareerSaveSlot slot)
        {
            if (slot == CareerSaveSlot.Autosave)
            {
                throw new ArgumentException("The autosave slot is written by the client, not on request.", nameof(slot));
            }

            BeginOperation(nameof(SaveTo));
            try
            {
                return _slots.Write(slot, _loop, _worldSeed, ManagedClubName);
            }
            finally
            {
                IsBusy = false;
                Revision++;
            }
        }

        /// <summary>The display name of <paramref name="clubId"/>.</summary>
        public string ClubName(int clubId) => _league.ClubAt(clubId).Name;

        private void Autosave()
        {
            try
            {
                _slots.Write(CareerSaveSlot.Autosave, _loop, _worldSeed, ManagedClubName);
                LastAutosaveError = null;
            }
            catch (IOException e)
            {
                LastAutosaveError = e.Message;
            }
            catch (UnauthorizedAccessException e)
            {
                LastAutosaveError = e.Message;
            }
        }

        private void BeginOperation(string name)
        {
            if (IsBusy)
            {
                throw new InvalidOperationException(name + " was invoked while another career operation is running.");
            }

            IsBusy = true;
        }

        private static void RequireClub(League league, int clubId)
        {
            int[] ids = league.ClubIds();
            for (int i = 0; i < ids.Length; i++)
            {
                if (ids[i] == clubId)
                {
                    return;
                }
            }

            throw new ArgumentException("Club " + clubId + " is not in the generated league.", nameof(clubId));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-10-18 | —      | Initial creation (§5-P7 / C3–C4): canonical new-game           |
// |         |            |        | composition, resume, day-granular Execute with legality check, |
// |         |            |        | autosave after rounds/rolls, manual slots, busy guard.         |
// | 1.1     | 2026-10-27 | —      | PR 519 review: Resume invalidates a Ready slot whose save fails |
// |         |            |        | to decode or rebuild, so Continue stops choosing it.           |
// | 1.2     | 2026-10-27 | —      | PR 519 review 2: Resume validates the mode before the guarded  |
// |         |            |        | rebuild, so a caller error never invalidates a healthy slot.   |
// | 1.3     | 2026-10-27 | —      | PR 519 review 4: only #30's load (and a mid-match save)        |
// |         |            |        | invalidates the slot; a failed rebuild from loaded contents    |
// |         |            |        | propagates and leaves it alone.                                |
// | 1.4     | 2026-10-29 | —      | PR 522 review: a failed rebuild invalidates the slot too, so   |
// |         |            |        | Continue cannot get stuck on it (MarkDamaged is reversible).   |
// | 1.5     | 2026-10-30 | —      | PR 521 merge: pass the saved #31 state into the resumed loop. |
#endregion
