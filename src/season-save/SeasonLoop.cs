// File:     src/season-save/SeasonLoop.cs
// Created:  2026-07-26
// Modified: 2026-08-08 (#28 T1/T2a + AR pass 1 — v1.16)
//           v1.15; the pass 1-3 recording chain and the pass-5 doc fix are the rows below)
// Author:   —
// Spec:     Season & Competition Loop #30 §3.3 (day advance / KD-2 tick order), §3.4 (playing a round /
//           KD-9), §3.5 (season-boundary roll / KD-6), §4.3 (the composition root), §4.6 (the #22
//           producer boundary / KD-3), §4.7 (CS0104);
//           FR-SN-010/011/012/013/013a/013b/016/017/018/025/026/029/030/031/032/033/034;
//           Training System #29 §3.3/§3.5, FR-TR-004/016/025; Injuries & Medical #41 §3.5,
//           FR-MD-003/022/023/025; ERR-030-002 / ERR-030-009;
//           path-to-playable A4 + A5 + D2/D3 (T2); Code Standards #20
// Purpose:  The season composition root — the only writer of SeasonState (KD-7 / FR-SN-032). Advances the
//           world one calendar day at a time in the KD-2 fixed order, resolves a whole round of fixtures,
//           rolls the season boundary into the next season, and hands #37/#38 a read-only view. Runs on
//           the WORLD tick (FR-SN-025); the 10 Hz / 60 Hz match loops live entirely inside a managed
//           fixture's MatchEngine and never drive this.
//
// Not on the 60 Hz hot path (§4.3), so allocation / new / exceptions are permitted — the
// SeasonSaveManager / WorldStore precedent.
//
// CS0104 / §4.7: `MatchEngine` is both a namespace and a class in this scope, so the class is written
// fully qualified as TacticalDirector.MatchEngine.MatchEngine from the first line that needs it — the
// #27 v1.73 lesson, applied by construction rather than discovered by a failed build.

using System.Collections.Generic;
using System.Collections.ObjectModel;

using TacticalDirector.InjuriesMedical;
using TacticalDirector.LivingWorld;
using TacticalDirector.MatchEngine;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.PlayerProgression;
using TacticalDirector.TrainingSystem;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// The season/competition loop (#30 §4.3): owns a <see cref="SeasonState"/>, references a
    /// <see cref="WorldStore"/> it does not own, and holds the managed club's in-progress
    /// <c>MatchEngine</c> or null between fixtures.
    /// <para>
    /// <b>Sole writer (KD-7 / FR-SN-032).</b> Every <see cref="SeasonState"/> mutator is
    /// <c>internal</c> to this assembly, and this class is the only production caller. Season state is
    /// therefore mutable only through the command API below — <see cref="AdvanceToNextFixtureDay"/>,
    /// <see cref="AdvanceAndPlayNextRound"/> and (at #30 T3) the boundary roll — never by field access.
    /// </para>
    /// <para>
    /// <b>Producer, not consumer (KD-3 / FR-SN-016..018).</b> Each played fixture emits exactly one
    /// <see cref="MatchResult"/>, recorded in <see cref="MatchOutcomes"/>. Nothing here calls a #22
    /// ingest method — none exists (<c>WorldLoop</c> phase 1 has no interface, FR-LW-031) and #30 must
    /// not add one; ingest activates with #33 (FR-LW-032). The only #22 surface touched is
    /// <see cref="WorldStore"/>'s public API (FR-SN-018 / FR-LW-003).
    /// </para>
    /// <para>
    /// THREAD SAFETY: none, matching <see cref="SeasonState"/>'s single-threaded contract. #38's client
    /// runs a sim thread beside a UI thread, so this command API is the marshaling point: the UI thread
    /// reads a <see cref="SeasonViewModel"/> snapshot (which copies) and never touches season state.
    /// </para>
    /// </summary>
    public sealed class SeasonLoop
    {
        private readonly WorldStore _world;
        private readonly SeasonState _state;
        private readonly List<MatchResult> _outcomes = new List<MatchResult>();

        // The #29/#41 T2 wiring, or (null, null). Bound as a PAIR because neither half means anything
        // alone: the career state is keyed by (ClubId, PlayerId) and every operation on it reads
        // attributes out of a roster, so a career with no provider could not advance a day and a
        // provider with no career would have nothing to advance.
        private readonly PlayerCareerStates _career;
        private readonly ISquadProvider _careerSquads;
        private readonly ProgressionEngine _progression;

        private TacticalDirector.MatchEngine.MatchEngine _activeMatch;

        /// <summary>
        /// Composes a loop over an existing world and season.
        /// </summary>
        /// <param name="world">The day-advance substrate. Referenced, not owned — #22 owns its
        /// lifecycle. The caller is responsible for having constructed it from the SAME world seed the
        /// league was generated from (league-bootstrap KD-9); nothing here can verify that.
        /// <para>
        /// While a season is in progress this loop is the sanctioned DRIVER of that clock:
        /// <see cref="AdvanceToNextFixtureDay"/> / <see cref="AdvanceDays"/> run the KD-2 fixed order and
        /// enforce the KD-4 bounds. Calling <c>WorldStore.AdvanceDay()</c> directly bypasses both — it
        /// skips the per-day seams Wave-2+ specs slot into, and can carry the clock past the day the next
        /// season would open, after which <see cref="RollToNextSeason"/> can no longer install a calendar
        /// (the season is then playable by neither route). Drive the clock through this loop.
        /// </para></param>
        /// <param name="season">The season this loop drives. Taken by reference and mutated in place —
        /// this loop becomes its sole writer, so the caller must not retain a second writer.</param>
        /// <param name="mode">How a round's fixtures resolve (§3.4.1). Defaults to the FR-SN-013b
        /// arrangement: the managed club's fixture through the real engine, the rest quick-simmed.</param>
        /// <param name="careerOrNull">
        /// The #29 training / #41 medical state this loop drives (the T2 wiring), or <c>null</c> for a
        /// loop that runs neither — which is what every caller before T2 got, and stays byte-identical.
        /// Must be supplied together with <paramref name="careerSquadsOrNull"/>.
        /// </param>
        /// <param name="careerSquadsOrNull">
        /// The rosters <paramref name="careerOrNull"/> reads attributes from, and the SAME provider
        /// <see cref="AdvanceAndPlayNextRound"/> must be called with — that is enforced rather than
        /// documented, because two providers would silently advance training against one league's
        /// attributes and resolve fixtures against another's.
        /// </param>
        /// <exception cref="System.ArgumentNullException">A required reference is null.</exception>
        /// <exception cref="System.ArgumentException">
        /// The KD-4 cursor invariant is already violated: the world clock has passed the season's pending
        /// round (F4). Checked here as well as at <c>SeasonSaveManager.Load</c> because a loop can be
        /// composed from a freshly built world and an advanced season without any file involved. Or the
        /// career pair is half-supplied.
        /// </exception>
        public SeasonLoop(
            WorldStore world,
            SeasonState season,
            RoundResolutionMode mode = RoundResolutionMode.ManagedThroughEngine,
            PlayerCareerStates careerOrNull = null,
            ISquadProvider careerSquadsOrNull = null,
            ProgressionEngine progressionOrNull = null)
        {
            if (world == null)
            {
                throw new System.ArgumentNullException(nameof(world));
            }

            if (season == null)
            {
                throw new System.ArgumentNullException(nameof(season));
            }

            if (!System.Enum.IsDefined(typeof(RoundResolutionMode), mode))
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(mode), mode, "Undefined RoundResolutionMode.");
            }

            if (!season.Calendar.SatisfiesCursorInvariant(world.CurrentWorldTick))
            {
                throw new System.ArgumentException(
                    $"KD-4 cursor invariant violated: the world is on day {world.CurrentWorldTick} but the "
                    + $"season's next fixture is day {season.Calendar.NextFixtureDay()} (round "
                    + $"{season.Calendar.NextRoundIndex}).",
                    nameof(season));
            }

            // #28 T2a — ONE roster authority, enforced by the signature rather than by convention.
            // When a progression store is supplied it IS the roster (KD-4), so the loop derives the
            // provider from it and refuses a separately-supplied one. Accepting both would let a caller
            // hand in the bootstrap product beside the store that has been evolving away from it since
            // day 0 — two surfaces that agree only at the moment anything would compare them, which is
            // the silent-divergence shape #29/#41 T2's H2 filed one layer down.
            //
            // An EMPTY store is NOT that authority (ERR-028-013). It carries no rosters, so it cannot
            // be the thing every consumer reads through, and treating it as wired made the one
            // composition the save root documents impossible to build: SeasonSaveContents.Progression
            // is never null, a pre-#28 save carries a well-formed ZERO-club block, and
            // SeasonSaveManager.Save's own refusal message tells the caller to thread that store back
            // in. Beside a provider it tripped the two-authorities refusal below; alone it failed the
            // season-coverage check. The only way through was to pass null INSTEAD of the loaded
            // store — an undocumented special case, and the opposite of what the guard advised. An
            // empty store is the absence of #28, stated rather than arrived at.
            bool progressionIsRoster = progressionOrNull != null && progressionOrNull.ClubCount > 0;

            if (progressionIsRoster && careerSquadsOrNull != null)
            {
                throw new System.ArgumentException(
                    "Supply a progression store OR a squad provider, never both: when #28 is wired the "
                    + "roster IS its career block (KD-4) and the loop projects the provider from it. A "
                    + "separately-supplied provider would be the day-0 bootstrap, stale by exactly the "
                    + "growth the store has banked.",
                    nameof(careerSquadsOrNull));
            }

            ISquadProvider resolvedSquads = progressionIsRoster
                ? new ProgressionSquads(progressionOrNull)
                : careerSquadsOrNull;

            // The progression store must cover the season, for the same reason the career must (below)
            // and checked in the same place — this is the only layer holding both. A store built over a
            // subset would construct and advance days without complaint, then hand back a null squad
            // part-way through a round, after earlier fixtures had already been applied to the table.
            // Refuse the pairing rather than half-resolving a round.
            if (progressionIsRoster)
            {
                ReadOnlyCollection<int> progressionClubs = season.ClubIds;
                for (int i = 0; i < progressionClubs.Count; i++)
                {
                    if (!progressionOrNull.CarriesClub(progressionClubs[i]))
                    {
                        throw new System.ArgumentException(
                            $"The progression store carries no career state for club "
                            + $"{progressionClubs[i]}, which plays in this season. Seed it over the "
                            + "season's whole club set (ProgressionEngine.SeedFrom).",
                            nameof(progressionOrNull));
                    }
                }
            }

            // A career still cannot be advanced without the rosters its attributes come from.
            if (careerOrNull != null && resolvedSquads == null)
            {
                throw new System.ArgumentException(
                    "The career state needs the squad provider its attributes are read from: supply "
                    + "careerSquadsOrNull, or a populated progression store for the loop to project "
                    + "one from.",
                    nameof(careerSquadsOrNull));
            }

            // …but a provider "on its own" is no longer inert (ERR-028-013). That rule predates #28:
            // a bare ISquadProvider drives nothing, so requiring a career beside it was right. A
            // PROGRESSION store drives slot 1 and is its own provider, so demanding #29/#41 career
            // state beside it makes #28 unusable without two subsystems it does not depend on — and
            // it made slot 1's own `_career == null` branch, the FR-PG-009 "no training anywhere"
            // path, provably unreachable: a guard on a branch nothing can execute, which is how a
            // dead path ships green forever.
            if (careerOrNull == null && resolvedSquads != null && !progressionIsRoster)
            {
                throw new System.ArgumentException(
                    "A squad provider on its own drives nothing: supply the career state it feeds, or "
                    + "a populated progression store, which drives the slot-1 daily step by itself.",
                    nameof(careerSquadsOrNull));
            }

            // The career must cover the season, and this is the only layer holding both — the same
            // argument that puts the KD-4 cursor invariant on the two lines above. A career built over
            // a subset would construct, advance days and roll seasons without complaint (the day steps
            // iterate the CAREER's clubs, not the season's) and then throw from the availability filter
            // part-way through a round, after earlier fixtures in that round had already been applied
            // to the table and marked played. Refuse the pairing instead of half-resolving a round.
            if (careerOrNull != null)
            {
                ReadOnlyCollection<int> seasonClubs = season.ClubIds;
                for (int i = 0; i < seasonClubs.Count; i++)
                {
                    if (!careerOrNull.CarriesClub(seasonClubs[i]))
                    {
                        throw new System.ArgumentException(
                            $"The career carries no state for club {seasonClubs[i]}, which plays in this "
                            + "season. Build it over the season's whole club set "
                            + "(PlayerCareerStates.ForLeague(squads, season.ClubIds)).",
                            nameof(careerOrNull));
                    }
                }

                // The cursor-vs-clock rule at the COMPOSITION boundary (AR pass 6 M3), for the same
                // reason the KD-4 check above runs here: a loop can be composed from a career and a
                // world that never met a save file, and a desynchronised pair either silently skips
                // every day step (cursor ahead — F6 reads the day as done) or wedges permanently
                // (cursor lagging by >= 2 — F7 refuses the gap, and the steps run before the clock
                // increment). The save root enforces the same rules at the file boundary.
                careerOrNull.RequireCursorsWithinClock(world.CurrentWorldTick);
            }

            // The same rule for #28's cursor, at the same boundary and through the same owner
            // (ERR-028-007). A loop can be composed from a store and a world that never met a save
            // file: ahead of the clock silently freezes growth, and lagging by two or more makes the
            // next advance REPLAY the gap, banking days of growth from one day's inputs.
            if (progressionIsRoster)
            {
                ClubCareerStates[] careerBlocks = progressionOrNull.ToBlocks();
                for (int c = 0; c < careerBlocks.Length; c++)
                {
                    for (int p = 0; p < careerBlocks[c].Count; p++)
                    {
                        PlayerCareerStates.RequireProgressionCursorWithinClock(
                            world.CurrentWorldTick,
                            careerBlocks[c].ClubId,
                            careerBlocks[c].Records[p].PlayerId,
                            careerBlocks[c].Lifecycles[p].LastAdvancedWorldDay,
                            "SeasonLoop composition");
                    }
                }
            }

            _world = world;
            _state = season;
            Mode = mode;
            _career = careerOrNull;
            _careerSquads = resolvedSquads;

            // Only a store that IS the roster is retained (ERR-028-013): an empty one has nothing for
            // slot 1 to advance, and holding it would make `_progression != null` mean two different
            // things at the two places that read it. Save writes `loop.Progression ?? Empty`, so a
            // loop composed from an empty store still round-trips to the same well-formed zero-club
            // block it was built from.
            _progression = progressionIsRoster ? progressionOrNull : null;
        }

        /// <summary>How this loop resolves a round's fixtures (§3.4.1).</summary>
        public RoundResolutionMode Mode { get; }

        /// <summary>The world's current calendar day (<c>WorldStore.CurrentWorldTick</c>).</summary>
        public uint CurrentWorldDay => _world.CurrentWorldTick;

        /// <summary>
        /// The day-advance substrate this loop drives — referenced, not owned (#22 owns its lifecycle).
        /// <para>
        /// <b><c>internal</c>, deliberately.</b> It exists for
        /// <see cref="SeasonSaveManager.Save(SeasonLoop,MatchEngine.MatchEngine,string)"/>, which is in
        /// this assembly and needs the store to snapshot it. Making it public would hand every caller
        /// <c>WorldStore.AdvanceDay()</c>, which bypasses the KD-4 bounds AND the per-day career seams —
        /// and the career then fails loud on the resulting day gap, correctly but a long way from the
        /// call that caused it. The public read of the clock is <see cref="CurrentWorldDay"/>.
        /// </para>
        /// </summary>
        internal WorldStore World => _world;

        /// <summary>True once every round has been played — the caller must run the boundary roll (#30 T3).</summary>
        public bool IsSeasonComplete => _state.Calendar.IsSeasonComplete;

        /// <summary>The index of the round that <see cref="AdvanceAndPlayNextRound"/> would resolve.</summary>
        public int NextRoundIndex => _state.Calendar.NextRoundIndex;

        /// <summary>
        /// The managed club's in-progress match, or <c>null</c> between fixtures (§4.3 / the KD-1
        /// <c>matchPresent</c> flag). Non-null only while a <see cref="RoundResolutionMode.ManagedThroughEngine"/>
        /// or <see cref="RoundResolutionMode.FullEngine"/> fixture is being played, which within a single
        /// synchronous <see cref="AdvanceAndPlayNextRound"/> call is visible only to a #38 observer on
        /// another thread — the seam a later interactive "watch my match" flow drives.
        /// </summary>
        public TacticalDirector.MatchEngine.MatchEngine ActiveMatch => _activeMatch;

        /// <summary>
        /// How many fixtures this loop has resolved through a real <c>MatchEngine</c> rather than the
        /// round-resolution model, across its whole lifetime.
        /// <para>
        /// Session-scoped like <see cref="MatchOutcomes"/> and not serialized. It is the cheapest honest
        /// answer to "did a real match actually run?" without either having to re-run a ~2-minute match
        /// to find out or pin an engine-produced scoreline — which is what the capstone asserts on.
        /// </para>
        /// <para>
        /// <b>Career-scoped, not season-scoped.</b> <see cref="RollToNextSeason"/> deliberately does not
        /// reset it, so from season 2 onward this is a career total. A client wanting "matches watched
        /// this season" must snapshot it at the boundary — <see cref="SeasonRollOutcome"/> is the signal
        /// that the boundary happened — rather than reading this directly.
        /// </para>
        /// </summary>
        public int EnginePlayedFixtures { get; private set; }

        /// <summary>
        /// Every <see cref="MatchResult"/> this loop has emitted, oldest first — the FR-SN-016
        /// match-outcome producer record.
        /// <para>
        /// <b>Session-scoped, deliberately not persisted (ERR-030-013).</b> §4.6 describes
        /// <c>EmitMatchOutcome</c> as recording the result "in <c>SeasonState</c>", but §2.2 and
        /// Appendix B give <see cref="SeasonState"/> no outcome collection, and adding one would be a
        /// <c>SEASON_STATE_FORMAT_VERSION</c> bump carrying a payload with no consumer — #22 ingest does
        /// not exist and FR-SN-017 forbids #30 from creating it. The durable record of what happened is
        /// the league table, which IS serialized; this list is the producer surface #33 will subscribe to
        /// when it lands.
        /// </para>
        /// <para>
        /// It grows for the lifetime of the loop and is never trimmed — 380 entries per 20-club season, so
        /// a long career held in one loop instance accumulates them. Bounded enough not to matter at Stage 2
        /// and deliberately not capped, because silently dropping the oldest outcomes would make the
        /// producer surface lossy right where #33 will want the history.
        /// </para>
        /// <para>
        /// It also spans season boundaries: <see cref="RollToNextSeason"/> does not clear it, and
        /// <see cref="MatchResult"/> carries no season number. <c>WorldDay</c> is non-decreasing but NOT
        /// strictly increasing — every fixture in a round is stamped with that round's single day — so a
        /// consumer separates seasons by bucketing on the boundary days
        /// (<see cref="SeasonRollOutcome.NextFirstFixtureDay"/>) rather than by any field on the result,
        /// and must not treat <c>WorldDay</c> as a unique key.
        /// </para>
        /// </summary>
        public ReadOnlyCollection<MatchResult> MatchOutcomes => _outcomes.AsReadOnly();

        /// <summary>The read-only observation surface for #37 / #38 (FR-SN-033). Reading never mutates.</summary>
        public SeasonViewModel View() => _state.View();

        /// <summary>
        /// The season this loop drives — the <c>season</c> argument
        /// <see cref="SeasonSaveManager.Save"/> needs, and the one #38 binds richer screens to than
        /// <see cref="SeasonViewModel"/> carries.
        /// <para>
        /// Exposing the object does <b>not</b> weaken FR-SN-032: every <see cref="SeasonState"/> mutator is
        /// <c>internal</c> to this assembly, so an outside caller holding this reference can only read.
        /// Inside the assembly the single-writer contract is upheld by this class being the only
        /// production code that touches those mutators.
        /// </para>
        /// </summary>
        public SeasonState State => _state;

        /// <summary>
        /// The #29 training / #41 medical state this loop drives, or <c>null</c> when none is wired.
        /// <para>
        /// The surface a client reads a player's conditioning, focus and fitness through, the surface
        /// the FR-TR-023 focus command is issued on (<c>TrySetFocus</c>), and the surface
        /// <see cref="SeasonSaveManager.Save"/>'s two block arguments come from
        /// (<c>TrainingBlocks()</c> / <c>MedicalBlocks()</c>). Exposed rather than proxied because
        /// every one of those is a read or a command the career state already validates for itself, and
        /// re-declaring them here would put a second copy of each contract on this class.
        /// </para>
        /// </summary>
        public PlayerCareerStates Career => _career;

        /// <summary>
        /// The #28 career store this loop drives at slot 1, or <c>null</c> when none is wired.
        /// <para>
        /// When non-null this is also <b>the roster authority</b> (KD-4): the provider slots 2 and 4
        /// read through is projected from it, so a client wanting a club's current squad should ask
        /// this rather than the bootstrap it was seeded from. It is the surface
        /// <see cref="SeasonSaveManager.Save(SeasonLoop,MatchEngine.MatchEngine,string)"/> takes the
        /// progression block from, and the surface #31/#38 read
        /// <c>LifecycleView</c> through (FR-PG-023).
        /// </para>
        /// </summary>
        public ProgressionEngine Progression => _progression;

        /// <summary>
        /// Advances the world one calendar day at a time, in the KD-2 fixed order, until the clock sits ON
        /// the next unplayed round's fixture day (§3.3 / FR-SN-010). Returns the number of days advanced —
        /// zero if the clock is already there.
        /// <para>
        /// <b>The fixture day's OWN day steps have not run when this returns</b> — the loop stops as soon
        /// as the clock reaches the target, so the last tick executed is the one for <c>targetDay − 1</c>.
        /// They run at the top of <see cref="AdvanceAndPlayNextRound"/>, pre-round (ERR-030-027), so a
        /// round resolves against matchday-morning career state; the next day-advance's re-run of the
        /// same day is a cursor no-op. See <see cref="RunWorldTickInFixedOrder"/>.
        /// </para>
        /// </summary>
        /// <exception cref="System.InvalidOperationException">The season is complete, so there is no next
        /// fixture day; run the boundary roll first (F5).</exception>
        public int AdvanceToNextFixtureDay()
        {
            uint targetDay = _state.Calendar.NextFixtureDay();   // throws when complete (F5)

            int advanced = 0;
            while (_world.CurrentWorldTick < targetDay)
            {
                RunWorldTickInFixedOrder();
                advanced++;
            }

            return advanced;
        }

        /// <summary>
        /// Advances the world exactly <paramref name="days"/> calendar days in the KD-2 fixed order,
        /// regardless of where the next fixture falls — the "skip ahead a bit" surface a client needs
        /// between fixtures, and what makes a mid-sequence save (FR-SN-024) expressible in a test.
        /// </summary>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="days"/> is negative.</exception>
        /// <exception cref="System.InvalidOperationException">The advance would carry the clock past the
        /// day the season's next round is playable on — the pending round's fixture day mid-season, or
        /// the day the NEXT season would open once this one is complete. Either would violate the KD-4
        /// invariant (FR-SN-011).</exception>
        public void AdvanceDays(int days)
        {
            if (days < 0)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(days), days, "days must be non-negative.");
            }

            ulong endDay = (ulong)_world.CurrentWorldTick + (ulong)days;

            if (!_state.Calendar.IsSeasonComplete)
            {
                uint targetDay = _state.Calendar.NextFixtureDay();
                if (endDay > targetDay)
                {
                    throw new System.InvalidOperationException(
                        $"Advancing {days} days from world-day {_world.CurrentWorldTick} would reach "
                        + $"{endDay}, past the pending round's fixture day {targetDay} — the KD-4 cursor "
                        + "invariant (FR-SN-011). Play the round first.");
                }
            }
            else
            {
                // The same invariant, on the far side of the boundary. RollToNextSeason derives the new
                // calendar purely from the old one (KD-6), so the day the next season opens is already
                // determined the moment this one ends — and the roll refuses to install a calendar that
                // opens in the past. Without this bound a client walking the close season one day at a
                // time can step past that day and reach a state with NO way forward: the season cannot be
                // played (it is complete) and cannot be rolled (the derived calendar is now behind the
                // clock), the world clock only moves forward, and the stuck state saves and reloads
                // cleanly. Refusing the step that would cause it fails loud at the mistake instead.
                uint openingDay = NextSeasonCalendar().NextFixtureDay();
                if (endDay > openingDay)
                {
                    throw new System.InvalidOperationException(
                        $"Advancing {days} days from world-day {_world.CurrentWorldTick} would reach "
                        + $"{endDay}, past the day the next season opens ({openingDay}) — and a roll can "
                        + "no longer install that calendar, leaving the career unable to progress "
                        + "(FR-SN-011). Call RollToNextSeason() first, then advance.");
                }
            }

            for (int i = 0; i < days; i++)
            {
                RunWorldTickInFixedOrder();
            }
        }

        /// <summary>
        /// Resolves <b>every</b> fixture of the round at the cursor, applies all their results to the
        /// table, emits one match-outcome per fixture, and advances the cursor by one round
        /// (§3.4 / FR-SN-012). Returns the results in resolution order.
        /// <para>
        /// Resolving a strict subset is forbidden and structurally impossible here: the whole round is
        /// resolved in one call, because leaving a club without a result for a round it had a fixture in
        /// makes the table undefined for that club (the KD-9 finding).
        /// </para>
        /// <para>
        /// <b>Order-independent.</b> The managed fixture consumes its own engine's RNG streams and every
        /// other fixture draws by KEY rather than cursor position (<see cref="RoundResolutionModel"/>), so
        /// permuting the resolution order yields the byte-identical final table (§3.4.1 / T-SN-CAL-003c).
        /// </para>
        /// </summary>
        /// <param name="squads">Resolves a <c>ClubId</c> to its roster. A <see cref="League"/> is one
        /// (league-bootstrap KD-9). Needed for EVERY club in the round, not just the managed one: the
        /// quick-sim's rating is the starting-XI mean of the squad each club would field.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="squads"/> is null.</exception>
        /// <exception cref="System.InvalidOperationException">The cursor is past the last round, or the
        /// round at the cursor has no unplayed fixtures (F5) — the caller must run the boundary roll.</exception>
        /// <exception cref="System.ArgumentException">A club in the round cannot be resolved to a squad
        /// (F6) — the #27 <see cref="ISquadProvider"/> fail-loud contract; or a career is wired and
        /// <paramref name="squads"/> is not the provider it was bound to.</exception>
        /// <remarks>
        /// With a career wired, matchday's own career day-steps (slots 2 and 4) run HERE, before the
        /// round is resolved — so a player whose recovery expires on matchday is available for it, and
        /// the availability filter reads matchday-morning state, not yesterday's (ERR-030-027, closing
        /// the half of ERR-030-026 deferred to the balance pass). The step is idempotent per day, so
        /// the re-run inside the next day-advance's <see cref="RunWorldTickInFixedOrder"/> is a no-op
        /// over the cursors.
        /// </remarks>
        public MatchResult[] AdvanceAndPlayNextRound()
        {
            // The reachable entry point for a loop that owns its provider (ERR-028-010). When #28 is
            // wired the constructor PROJECTS the provider from the progression store and keeps it
            // private, so the ISquadProvider overload below — which demands reference-equality with
            // that instance — could not be satisfied by any caller: the store's projection was not
            // exposed anywhere, and constructing an equivalent one is a different object. The headline
            // configuration of the #28 landing could therefore advance days and save, and never play a
            // round. Resolving through the loop's own provider also removes the older hazard the
            // overload guards against by hand: there is no second provider to disagree with.
            if (_careerSquads == null)
            {
                throw new System.InvalidOperationException(
                    "This loop owns no squad provider, so it cannot resolve a round on its own. Use "
                    + "AdvanceAndPlayNextRound(ISquadProvider) — the careerless path, where the caller "
                    + "supplies the rosters.");
            }

            return PlayNextRound(_careerSquads);
        }

        /// <summary>
        /// The careerless / caller-supplied-provider path. When the loop owns a provider (a career, or
        /// #28's projection) prefer the no-argument overload: this one exists for a loop that has no
        /// provider of its own, and it still refuses a provider that disagrees with a wired career.
        /// </summary>
        /// <param name="squads">The rosters this round resolves against.</param>
        public MatchResult[] AdvanceAndPlayNextRound(ISquadProvider squads)
        {
            if (squads == null)
            {
                throw new System.ArgumentNullException(nameof(squads));
            }

            // A wired career reads attributes and availability out of the provider it was constructed
            // with, while this method resolves squads out of the one it is handed. Two different
            // providers would train one league and play another, and every symptom of that would be a
            // plausible-looking result rather than a crash — so require the same object, which for the
            // one caller that matters (a League, which IS the provider) is free.
            if (_career != null && !ReferenceEquals(squads, _careerSquads))
            {
                throw new System.ArgumentException(
                    "This loop drives a career whose state is keyed to a different ISquadProvider. "
                    + "Pass the same provider the loop was constructed with, or the round would be "
                    + "resolved against rosters the training and medical state does not describe.",
                    nameof(squads));
            }

            return PlayNextRound(squads);
        }

        // The one body both overloads run. Extracted rather than duplicated: two copies of a
        // twelve-guard round resolution is the parallel-surface defect this repo keeps filing.
        private MatchResult[] PlayNextRound(ISquadProvider squads)
        {
            if (_state.Calendar.IsSeasonComplete)
            {
                throw new System.InvalidOperationException(
                    "The season is complete; there is no round to play. Run the boundary roll first (F5).");
            }

            int round = _state.Calendar.NextRoundIndex;
            int[] indices = _state.UnplayedFixtureIndicesInRound(round);
            if (indices.Length == 0)
            {
                throw new System.InvalidOperationException(
                    $"Round {round} has no unplayed fixtures (F5).");
            }

            // §3.3's contract is "the cursor is now AT the fixture day; the caller runs
            // AdvanceAndPlayNextRound" — so playing a round the clock has not reached yet is a caller
            // error, and a silent one: every result would be stamped with a WorldDay that is not the
            // fixture's day, and a client could skip the day-advance for a whole career and still get a
            // plausible-looking table. The KD-4 invariant only bounds this from one side (targetDay >=
            // today), which is why it needs its own gate rather than being implied.
            uint fixtureDay = _state.Calendar.DayOfRound(round);
            if (_world.CurrentWorldTick != fixtureDay)
            {
                throw new System.InvalidOperationException(
                    $"Round {round} is played on world-day {fixtureDay} but the world is on day "
                    + $"{_world.CurrentWorldTick}. Call AdvanceToNextFixtureDay() first (§3.3).");
            }

            uint worldDay = _world.CurrentWorldTick;

            // ERR-030-027: the fixture day's own career day-steps run BEFORE the round, in the same
            // KD-2 slot order, over the WHOLE career — not just the round's clubs, or clubs without a
            // fixture today would desynchronise from those with one. This is what makes the recovery
            // countdown land on the correct side of the round (a player who served his time plays
            // today), and it puts the occurrence draw on matchday MORNING: a player drawn injured now
            // is filtered by SelectAvailable below, which is a training-ground knock before kickoff,
            // not a match injury — match load reaches the draw through the FR-MD-010 appearance
            // window, which never contains today. Runs after every guard above so a refused call
            // cannot advance a cursor.
            RunCareerDaySteps(worldDay);

            var results = new MatchResult[indices.Length];

            for (int i = 0; i < indices.Length; i++)
            {
                Fixture fixture = _state.FixtureAt(indices[i]);
                MatchResult result = ResolveFixture(
                    in fixture, squads, worldDay, out int[] homeXi, out int[] awayXi);

                // ERR-041-010(b): the appearance record, written from the XIs the RESOLUTION ITSELF
                // fielded — ResolveFixture hands back the ids it derived at its own filter+configure
                // site (AR pass 2: a second SelectAvailable walk here was an unenforced agreement with
                // the engine's configuration, of exactly the documented-not-structural class this file
                // refuses elsewhere). Null XIs = no career wired = nothing to record. Placed BEFORE
                // the pinned apply/emit/mark sequence (AR pass 1): it is the only fallible call in
                // this block, and a throw after MarkFixturePlayed would strand the round — the fixture
                // skipped by the unplayed-index filter on retry, the cursor never advancing, the
                // season unrecoverable. The pair form (AR pass 3) validates BOTH clubs before writing
                // EITHER, so a refused away side no longer leaves the home XI carrying an appearance
                // for a fixture that was never applied.
                if (_career != null)
                {
                    _career.RecordFixtureAppearances(
                        fixture.HomeClubId, homeXi, fixture.AwayClubId, awayXi, worldDay);
                }

                // FR-SN-013's pinned order, for every fixture: (1) table, (2) event, then mark played.
                _state.ApplyResult(in result);
                EmitMatchOutcome(in result);
                _state.MarkFixturePlayed(indices[i]);

                results[i] = result;
            }

            _state.AdvanceCursorOneRound();
            return results;
        }

        /// <summary>
        /// The season-boundary roll (§3.5 / FR-SN-029): finalize the table, evaluate the board, derive
        /// the next season's seed, regenerate its schedule and calendar, and reset. Returns what the
        /// board decided and what the new season starts from.
        /// <para>
        /// <b>KD-6 — one restartable transform.</b> The result is a pure function of the prior
        /// <see cref="SeasonState"/> alone: the next seed derives from the current seed and season
        /// number, the schedule from that seed, and the calendar by shifting THIS season's round spacing
        /// forward by one season-length plus the close-season break. Nothing is drawn from a cursor and
        /// nothing reads the wall clock, so a save taken either side of this call restores to the same
        /// continuation, and rolling twice from the same prior state yields the same next state.
        /// </para>
        /// <para>
        /// <b>Ordering is load-bearing.</b> Everything is computed and validated before anything is
        /// written, and the one write that can fail (<c>BeginNextSeason</c>, which re-applies the
        /// constructor's club-set and calendar-coverage gates) runs BEFORE the one that cannot
        /// (<c>SetBoard</c>, whose value is pre-clamped into range). A refused roll therefore leaves the
        /// season completely untouched, rather than half-rolled with a new board verdict against an old
        /// schedule — the `ConfigureSquads` validate-both-before-write discipline.
        /// </para>
        /// <para>
        /// <b>Insertion points (FR-SN-031), declared and empty.</b> (a') #43's promotion/relegation
        /// transform and (b') #40's finance settlement sit between the board evaluation and the
        /// regeneration, in that order, so budgets reflect the post-promotion division. They are
        /// positions in this method, not interfaces — neither spec has code (FR-SN-034 / FR-LW-031).
        /// (d) #28's age advance is the same: a documented position, empty until #28 T2.
        /// </para>
        /// <para>
        /// <b>(d′) the FR-TR-025 / FR-MD-025 roster reconciliation is LIVE, and split in two.</b> It
        /// reads the provider at (d′) — after #28's churn would have happened — but installs only after
        /// (e)'s commits, because <c>BeginNextSeason</c> is the one write here that can fail and a
        /// career reconciled against a season that never began is the same half-rolled state the
        /// ordering above exists to prevent. Staging throws where the sync throws; installing cannot.
        /// </para>
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        /// The season is not over (F5 — rounds remain, or a fixture in a resolved round was never
        /// played), or the world clock has already passed the day the new season would open on, which
        /// would install a state violating the KD-4 cursor invariant (FR-SN-011).
        /// </exception>
        public SeasonRollOutcome RollToNextSeason()
        {
            if (!_state.Calendar.IsSeasonComplete)
            {
                throw new System.InvalidOperationException(
                    $"The season is not complete — round {_state.Calendar.NextRoundIndex} of "
                    + $"{_state.Calendar.RoundCount} is still pending. Play every round before rolling (F5).");
            }

            RequireEveryFixturePlayed();

            // ── (a) finalize ────────────────────────────────────────────────────────────────────
            // The table is already final; what the roll needs from it is the managed club's position.
            int finalPosition = _state.PositionOf(_state.ManagedClubId);

            // ── (b) board pass/fail + job security ──────────────────────────────────────────────
            // The board owns the rule; this step asks it for a verdict rather than re-deriving one, so
            // the reported ObjectiveMet and the job-security consequence cannot drift apart.
            BoardState board = _state.Board;
            int target = board.Objective.TargetPositionOrBetter;
            bool objectiveMet = board.Objective.IsMetBy(finalPosition);
            int securityBefore = board.JobSecurityPerMille;
            BoardState evaluated = board.EvaluateAtSeasonEnd(finalPosition);
            int securityAfter = evaluated.JobSecurityPerMille;

            // ── (a') #43 promotion/relegation inserts HERE (FR-SN-031) — empty at Stage 2. ──────
            // ── (b') #40 finance settlement inserts HERE (ERR-030-003) — empty at Stage 2. ──────

            // ── (c) regenerate ──────────────────────────────────────────────────────────────────
            ulong nextSeed = DeriveNextSeasonSeed(_state.Seed, _state.SeasonNumber);
            Fixture[] nextFixtures = FixtureScheduler.Generate(ClubIdsAscending(), nextSeed);
            SeasonCalendar nextCalendar = NextSeasonCalendar();

            // The KD-4 invariant is the one thing the roll cannot establish on its own: the calendar is
            // derived purely from the old one, so a client that advanced the world deep into the close
            // season before rolling would get a schedule that opens in the past. Refusing here beats
            // installing it and having SeasonLoop's own constructor reject the season on the next load.
            if (!nextCalendar.SatisfiesCursorInvariant(_world.CurrentWorldTick))
            {
                throw new System.InvalidOperationException(
                    $"The next season would open on world-day {nextCalendar.NextFixtureDay()} but the "
                    + $"world is already on day {_world.CurrentWorldTick} — that violates the KD-4 cursor "
                    + "invariant (FR-SN-011). Roll at the end of the season, then advance the world.");
            }

            // ── (d) #28 season boundary — RESERVED, still empty. ────────────────────────────────
            // The DAILY step is live at slot 1 since T2a, and age is derived there rather than
            // advanced here, so there is no "age advance" left for this position. What the slot
            // still holds open is RunSeasonBoundary: retiree removal + 1:1 regen, which needs the
            // player-progression.regen stream (#28 §3.5 does not pin how that survives a save).
            // #30 §3.5 carries the same wording.
            // (d′) the FR-TR-025 / FR-MD-025 roster-membership handoff, STAGED here and installed
            // below. #28's regens and retirements are the roster change, so the reconciliation reads
            // the provider at this point — after (d) — but it must not WRITE here: BeginNextSeason is
            // the one commit in this method that can fail, and a career reconciled against a season
            // that never began is exactly the half-rolled state this method's ordering exists to
            // prevent. Staging is pure and throws where the sync throws; installing cannot fail.
            PlayerCareerStates.RosterSyncPlan rosterSync = default;
            if (_career != null)
            {
                rosterSync = _career.PrepareRosterSync(_careerSquads);
            }

            // ── (e) commit: schedule + table + season number + seed, then the board verdict ─────
            int completedSeasonNumber = _state.SeasonNumber;
            _state.BeginNextSeason(nextSeed, nextFixtures, nextCalendar);

            // Cannot throw: EvaluateAtSeasonEnd returns an already-validated BoardState.
            _state.SetBoard(evaluated);

            // The staged reconciliation installs last, after the final write that could have refused
            // the roll. The returned churn count is deliberately dropped: what a caller wants to know
            // about a boundary is which players the career now carries, which is readable off
            // Career itself — a count would be a second, weaker answer to the same question.
            if (_career != null)
            {
                _career.CommitRosterSync(in rosterSync);
            }

            return new SeasonRollOutcome(
                completedSeasonNumber,
                finalPosition,
                target,
                objectiveMet,
                securityBefore,
                securityAfter,
                _state.SeasonNumber,
                nextSeed,
                nextCalendar.NextFixtureDay());
        }

        /// <summary>
        /// The calendar the next season would open on: this season's round spacing shifted forward by
        /// one season length plus the <c>[GT]</c> close season (§3.5 step (c′)).
        /// <para>
        /// One derivation with two readers — <see cref="RollToNextSeason"/> installs it, and
        /// <see cref="AdvanceDays"/> bounds the post-season clock by it. The arithmetic belongs to
        /// <see cref="SeasonCalendar"/>; what lives here is only the choice of
        /// <see cref="SeasonLoopConstants.SeasonBreakDays"/>, so that policy is bound in exactly one place.
        /// </para>
        /// </summary>
        private SeasonCalendar NextSeasonCalendar() =>
            _state.Calendar.ShiftedToNextSeason(SeasonLoopConstants.SeasonBreakDays);

        /// <summary>
        /// The §3.5 <c>DeriveNextSeasonSeed</c>: the successor season's seed, from this season's seed and
        /// number through <see cref="SeasonLoopConstants.SEASON_ROLL_SEED_DOMAIN"/>.
        /// <para>
        /// Folding the season NUMBER in as well as the seed is what stops a career from cycling: seed
        /// alone would make season N+1 a function of season N's seed only, so any repeat of a seed value
        /// would replay a schedule the manager has already seen. The domain constant separates this
        /// derivation from every draw made <i>inside</i> a season (<see cref="RoundResolutionModel"/>),
        /// so a fixture's result cannot correlate with the next season's shape.
        /// </para>
        /// </summary>
        public static ulong DeriveNextSeasonSeed(ulong seasonSeed, int seasonNumber)
        {
            unchecked  // Spec #16 §3.4.4: deliberate 64-bit wrap-around; not an overflow bug
            {
                // Cast through uint so a (structurally impossible) negative season number still mixes to
                // a defined value rather than sign-extending into the high half.
                ulong key = seasonSeed ^ ((ulong)(uint)seasonNumber * 0x9E3779B97F4A7C15UL);
                return RoundResolutionModel.Mix(key ^ SeasonLoopConstants.SEASON_ROLL_SEED_DOMAIN);
            }
        }

        /// <summary>
        /// The club ids as the ascending array <see cref="FixtureScheduler.Generate"/> expects.
        /// </summary>
        private int[] ClubIdsAscending()
        {
            ReadOnlyCollection<int> ids = _state.ClubIds;
            var array = new int[ids.Count];
            for (int i = 0; i < ids.Count; i++)
            {
                array[i] = ids[i];
            }

            return array;
        }

        /// <summary>
        /// F5's second half: a complete cursor is not on its own proof that every fixture was resolved.
        /// No path through <see cref="AdvanceAndPlayNextRound"/> can leave one unplayed, but a restored
        /// blob is only structurally validated, and rolling over an unplayed fixture would erase it
        /// silently — the club would carry a table that never counted a match it was scheduled to play.
        /// </summary>
        private void RequireEveryFixturePlayed()
        {
            ReadOnlyCollection<Fixture> fixtures = _state.Fixtures;
            for (int i = 0; i < fixtures.Count; i++)
            {
                if (!fixtures[i].Played)
                {
                    throw new System.InvalidOperationException(
                        $"Fixture {i} (round {fixtures[i].RoundIndex}, {fixtures[i].HomeClubId} v "
                        + $"{fixtures[i].AwayClubId}) was never played, so the season is not complete "
                        + "even though the calendar cursor is at the end (F5).");
                }
            }
        }

        /// <summary>
        /// Encodes this loop's season state as the season sub-blob (§3.6). The world is snapshotted
        /// separately by <see cref="WorldStore.Snapshot"/>; <c>SeasonSaveManager.Save</c> is what frames
        /// the two (plus an optional in-progress match) into one file.
        /// </summary>
        public byte[] Snapshot() => SeasonStateCodec.Encode(_state);

        /// <summary>
        /// Rebuilds a loop from a world and a season sub-blob — the counterpart of
        /// <see cref="Snapshot"/>. The KD-4 cursor invariant is re-validated by the constructor (F4).
        /// </summary>
        /// <param name="world">The restored world this season runs on.</param>
        /// <param name="seasonBlob">The season sub-blob from <see cref="Snapshot"/>.</param>
        /// <param name="mode">How a round's fixtures resolve (§3.4.1).</param>
        /// <param name="careerOrNull">The restored #29/#41 career state (from
        /// <c>PlayerCareerStates.FromBlocks</c> over <see cref="SeasonSaveContents"/>'s two block
        /// arrays), or null for a loop that drives neither.</param>
        /// <param name="careerSquadsOrNull">The rosters that career reads, on the constructor's terms —
        /// which means it must be null when <paramref name="progressionOrNull"/> is supplied.</param>
        /// <param name="progressionOrNull">The restored #28 career store (from
        /// <see cref="SeasonSaveContents.Progression"/>), or null for a loop that drives no progression.</param>
        /// <exception cref="System.ArgumentException">The blob is malformed (F3) or the restored pair
        /// violates the cursor invariant (F4).</exception>
        public static SeasonLoop Restore(
            WorldStore world,
            byte[] seasonBlob,
            RoundResolutionMode mode = RoundResolutionMode.ManagedThroughEngine,
            PlayerCareerStates careerOrNull = null,
            ISquadProvider careerSquadsOrNull = null,
            ProgressionEngine progressionOrNull = null)
        {
            return new SeasonLoop(
                world, SeasonStateCodec.Decode(seasonBlob), mode, careerOrNull, careerSquadsOrNull,
                progressionOrNull);
        }

        /// <summary>
        /// One calendar day, in the KD-2 pinned order (§3.3). Slots 2 and 4 and step 12 are live; every other
        /// step is a <b>documented position</b>, not an interface (FR-SN-034 / FR-LW-031) — each
        /// Wave-2+ spec slots into its pre-declared slot when it lands, so fixing the order now avoids a
        /// re-pin across all of them.
        /// <para>
        /// <b>The slot ORDER is the contract, not just the slot.</b> #41's risk assembly reads the
        /// conditioning and training-fatigue #29 wrote on this same day (ERR-030-002 / #41 KD-6), so
        /// running step 4 before step 2 would price every injury off a one-day-stale training state
        /// without throwing anything.
        /// </para>
        /// <para>
        /// With no career wired, only step 12 runs and a no-fixture day's advance is byte-identical to a
        /// bare <see cref="WorldStore.AdvanceDay"/> (FR-SN-026 / KD-8) — which is exactly what the
        /// behaviour-neutral floor test asserts. With one wired it stays byte-identical <i>to the
        /// world</i>: neither day step touches <see cref="WorldStore"/>, they mutate only the career
        /// state, which is serialized in its own sub-blobs.
        /// </para>
        /// <para>
        /// Both steps take the world day BEFORE step 12's increment — the day being lived, not the day
        /// being entered. That is what makes the first advance of a fresh world day 0 and keeps
        /// <c>LastAdvancedWorldDay</c> exactly one behind the clock between ticks, so a save taken here
        /// restores without a phantom gap.
        /// </para>
        /// <para>
        /// <b>Where the ROUND sits in this order was ERR-030-026's finding, and is now pinned rather
        /// than emergent</b> — the slot list has no slot for "play the round", because a round is
        /// resolved by a separate command (<see cref="AdvanceAndPlayNextRound"/>) rather than by the
        /// day advance. <see cref="AdvanceToNextFixtureDay"/> still stops the moment the clock REACHES
        /// the fixture day, but the fixture day's own slots no longer wait for the next
        /// advance: <see cref="AdvanceAndPlayNextRound"/> runs them itself, pre-round, through the same
        /// idempotent <see cref="RunCareerDaySteps"/> this method calls — so the re-run here is a
        /// cursor no-op. <b>Matchday is processed, then the round is played</b> (ERR-030-027): the
        /// recovery countdown lands before selection, so a player who served his time plays today, and
        /// the occurrence draw moves to matchday morning, where match load reaches it through the
        /// FR-MD-010 appearance window (which never contains today) instead of through a draw-after-
        /// the-round convention. Locked by <c>DayAdvance_StopsBeforeTheFixtureDaysOwnSteps</c>.
        /// </para>
        /// </summary>
        private void RunWorldTickInFixedOrder()
        {
            RunCareerDaySteps(_world.CurrentWorldTick);

            // 12. world day    — LIVE (the only step outside RunCareerDaySteps).
            _world.AdvanceDay();
        }

        /// <summary>
        /// Slots 0–11 of the KD-2 day order for one world day (#30 §3.3 as reconciled by
        /// ERR-030-022/-020) — everything except step 12's clock increment. Idempotent per day: both
        /// live steps carry their own per-player cursor, so the second caller of the same day is a
        /// no-op. Called from two places, deliberately:
        /// <see cref="RunWorldTickInFixedOrder"/> (every advanced day) and
        /// <see cref="AdvanceAndPlayNextRound"/> (the fixture day, pre-round — ERR-030-027).
        /// </summary>
        /// <param name="day">The world day being lived — the clock BEFORE step 12's increment.</param>
        private void RunCareerDaySteps(uint day)
        {
            // 0. facilities    (#53) — NULL SEAM (ERR-030-020: the upgrade-completion latch; numbered
            //                          zero so it precedes its same-day consumers without renumbering
            //                          the slots six APPROVED specs cite by number)
            // 1. progression   (#28) — LIVE (T2a, August 8, 2026: ERR-029-006 closed). #29 §3.5 step 1:
            //                          #30 gathers each player's ComputeTrainingInput into the batch and
            //                          hands it to #28's FR-PG-021 AdvanceDay. The gather is a pure read
            //                          of fields slot 2 does not mutate (FR-TR-006), so slots 1 and 2
            //                          are order-independent — but the batch is taken BEFORE slot 2 runs
            //                          so that independence is a property of the code and not only of
            //                          the argument for it.
            if (_progression != null)
            {
                TrainingInputBatch growth = _career != null
                    ? new TrainingInputBatch(
                        _career.GatherTrainingInputs(_careerSquads, CoachingModifier.Identity))
                    : TrainingInputBatch.Neutral;
                _progression.AdvanceDay(day, in growth);
            }

            // 2. training      (#29) — LIVE (T2).
            if (_career != null)
            {
                _career.AdvanceTrainingDay(day, _careerSquads, CoachingModifier.Identity);
            }

            // 3. human-systems (#33) — NULL SEAM
            // 4. injuries      (#41) — LIVE (T2). ERR-030-002: after #28/#29 so it reads the day's
            //                          updated fatigue/condition; before the world-day tick.
            if (_career != null)
            {
                _career.AdvanceMedicalDay(
                    day, _world.WorldSeed, _careerSquads, MedicalModifier.Identity);
            }

            // 5. transfers     (#31) — NULL SEAM (ERR-030-004: a deep-tier position reservation;
            //                          minimal transfers are command-driven)
            // 6. staff         (#34) — NULL SEAM (ERR-030-006: deep-tier position reservation;
            //                          #34's scaffold projections are pull-based)
            // 7. academy       (#42) — NULL SEAM (ERR-030-007: the youth-intake one-shot, latched on
            //                          LastIntakeWorldDay; live at #42's own T-phase)
            // 8. board         (#45) — NULL SEAM (ERR-030-008: one bounded integer drift per modelled
            //                          club; live at #45's own T-phase)
            // 9. scouting      (#32) — NULL SEAM (ERR-030-022: deep-tier assignment progress; the
            //                          minimal tier is the fog-off identity, so the seam is empty)
            // 10. media expiry (#35) — NULL SEAM (ERR-030-022: conference-window / pending-question
            //                          expiry; after scouting, before the world-day tick)
            // 11. tenure       (#54) — NULL SEAM (ERR-030-021: EvaluateTenure — after board, whose
            //                          confidence it reads; the terminating decision is #54's)
        }

        /// <summary>
        /// Routes one fixture to its producer (§3.4 / FR-SN-013 / FR-SN-013b): the managed club's fixture
        /// through the full engine under <see cref="RoundResolutionMode.ManagedThroughEngine"/>, every
        /// fixture through the engine under <see cref="RoundResolutionMode.FullEngine"/>, everything else
        /// through <see cref="RoundResolutionModel"/>.
        /// <para>
        /// <b>The fielded XIs come out of THIS method</b> (ERR-041-010(b), AR pass 2): each branch
        /// derives them at its own filter/configure site — the engine branch inside
        /// <see cref="BootFixtureEngine"/>, one statement from the <c>ConfigureSquads</c> that consumes
        /// the same squad instances; the quick-sim branch from the very squads its rating reads — so
        /// the appearance record cannot drift from what the resolution actually fielded. Null with no
        /// career wired. When a manager-chosen XI lands (#38 Wave-7), the id derivation moves WITH the
        /// configuration it sits beside, not in a distant caller.
        /// </para>
        /// </summary>
        private MatchResult ResolveFixture(
            in Fixture fixture, ISquadProvider squads, uint worldDay,
            out int[] homeXi, out int[] awayXi)
        {
            bool managed = fixture.Involves(_state.ManagedClubId);

            if (ShouldPlayThroughEngine(Mode, managed))
            {
                return PlayThroughEngine(in fixture, squads, worldDay, out homeXi, out awayXi);
            }

            Squad home = SelectAvailable(ResolveSquad(squads, fixture.HomeClubId));
            Squad away = SelectAvailable(ResolveSquad(squads, fixture.AwayClubId));
            homeXi = _career != null ? SquadRating.StartingElevenPlayerIds(home) : null;
            awayXi = _career != null ? SquadRating.StartingElevenPlayerIds(away) : null;

            // #29's match-entry fatigue is deliberately NOT an input here, unlike the availability
            // filter one line above. §3.4.1 keys this model on the rating differential alone, and the
            // rating is a property of the squad, not of how tired it is — feeding fatigue in would be a
            // change to the model, not a wiring gap. It is called out because the two are the same class
            // of thing and the filter's own comment argues the opposite way for itself: a club missing
            // four first-choice players is rated as such whether or not a human is watching, while a
            // club that trained flat out all week is not. Inert today (Balanced projects exactly 0), and
            // due with the balance pass that decides whether the quick-sim should model condition at all.
            //
            // Each club's rating is recomputed per fixture rather than cached, so a club is re-rated once
            // per matchday (38 times a season). That is deliberate: a cache would be state this loop would
            // then have to invalidate on every squad change — transfers (#31), injuries (#41), progression
            // (#28) all move ratings — and getting that wrong would silently resolve a season against stale
            // strengths. Selection is pure and the cost is microseconds against a season measured in
            // milliseconds; revisit only if profiling says otherwise.
            return RoundResolutionModel.Resolve(
                in fixture,
                _state.Seed,
                _state.SeasonNumber,
                SquadRating.StartingElevenMean(home),
                SquadRating.StartingElevenMean(away),
                worldDay);
        }

        /// <summary>
        /// The §3.4 / FR-SN-013b routing decision, extracted as a pure predicate so all six
        /// (mode × managed) combinations are unit-testable. Inline, the
        /// <see cref="RoundResolutionMode.FullEngine"/> branch could only be covered by running two real
        /// 90-minute matches, so a typo in it would have shipped as "FullEngine quietly behaves like
        /// ManagedThroughEngine".
        /// </summary>
        internal static bool ShouldPlayThroughEngine(RoundResolutionMode mode, bool managed)
        {
            switch (mode)
            {
                case RoundResolutionMode.FullEngine:
                    return true;
                case RoundResolutionMode.ManagedThroughEngine:
                    return managed;
                case RoundResolutionMode.QuickSimAll:
                    return false;
                default:
                    // Unreachable: the constructor gates the mode with Enum.IsDefined. Fail loud rather
                    // than silently quick-simming a fixture a future mode meant to route elsewhere.
                    throw new System.ArgumentOutOfRangeException(
                        nameof(mode), mode, "Undefined RoundResolutionMode.");
            }
        }

        /// <summary>
        /// Plays a fixture through a real <c>MatchEngine</c> (§3.4 <c>PlayThroughEngine</c>): boot from the
        /// fixture-keyed seed, configure both squads, tick the 10 Hz / 60 Hz loops to full time, and read
        /// the score off the engine.
        /// <para>
        /// The match runs on its own clocks while this method is invoked from the world-tick loop, so the
        /// two remain disjoint (FR-SN-025) — the world day does not advance during a match.
        /// </para>
        /// <para>
        /// The boot — the ERR-030-009 availability filter, #29's match-entry fatigue, and the
        /// <c>ConfigureSquads</c> call that consumes both — is <see cref="BootFixtureEngine"/>; what is
        /// left here is the run loop and the result. See that method for why the split exists.
        /// </para>
        /// <para>
        /// With a career wired this resolves against MATCHDAY-MORNING state: the fixture day's own
        /// career day-steps have already run pre-round at the top of
        /// <see cref="AdvanceAndPlayNextRound"/> (ERR-030-027), so the availability filter and the
        /// entry-fatigue projection read the day being played, not yesterday.
        /// </para>
        /// </summary>
        private MatchResult PlayThroughEngine(
            in Fixture fixture, ISquadProvider squads, uint worldDay,
            out int[] homeXi, out int[] awayXi)
        {
            TacticalDirector.MatchEngine.MatchEngine engine =
                BootFixtureEngine(in fixture, squads, out homeXi, out awayXi);

            _activeMatch = engine;
            try
            {
                while (!engine.MatchEnded)
                {
                    engine.RunTick();
                }

                EnginePlayedFixtures++;

                return new MatchResult(
                    fixture.HomeClubId,
                    fixture.AwayClubId,
                    engine.HomeScore,
                    engine.AwayScore,
                    fixture.RoundIndex,
                    worldDay);
            }
            finally
            {
                // Cleared even if the engine throws mid-match, so a failed fixture cannot leave a dead
                // engine reachable through ActiveMatch for the rest of the season.
                _activeMatch = null;
            }
        }

        /// <summary>
        /// The boot half of <see cref="PlayThroughEngine"/>, extracted: resolve → filter → project →
        /// configure, returning a kicked-off-ready engine without running a tick of it.
        /// <para>
        /// <b>Extracted so it can be tested, for the reason <see cref="ShouldPlayThroughEngine"/> was.</b>
        /// Inline, every decision here — the ERR-030-009 filter on BOTH clubs, #29's match-entry fatigue
        /// projected AFTER that filter because filtering renumbers the local indices it is keyed by, and
        /// the four-argument <c>ConfigureSquads</c> that consumes it — could only be reached by playing a
        /// full 90-minute match, which no suite in this assembly does (the capstone owns that cost). So
        /// the whole career-wired match-boot path shipped at T2 with no execution anywhere: the sole
        /// production call site of the entry-fatigue seam, unrun. That is the ERR-030-014 shape, and this
        /// seam is what lets a millisecond-scale test assert on the configured engine instead.
        /// </para>
        /// <para>
        /// Draws no RNG of its own beyond seeding the engine, and mutates no season state, so calling it
        /// and discarding the engine is free of side effects on this loop.
        /// </para>
        /// </summary>
        /// <param name="fixture">The fixture to boot an engine for.</param>
        /// <param name="squads">The provider the rosters resolve from.</param>
        internal TacticalDirector.MatchEngine.MatchEngine BootFixtureEngine(
            in Fixture fixture, ISquadProvider squads)
        {
            return BootFixtureEngine(in fixture, squads, out _, out _);
        }

        /// <summary>
        /// The id-producing form (ERR-041-010(b), AR pass 2): the fielded XIs are derived HERE, from
        /// the same filtered squad instances the <c>ConfigureSquads</c> one statement below consumes —
        /// so the appearance record's source is the configuration site itself, and a future
        /// manager-chosen XI changes both or neither. This overload IS the production path
        /// (<see cref="PlayThroughEngine"/> calls it); the discarding form above exists for boot-only
        /// assertions.
        /// </summary>
        /// <param name="fixture">The fixture to boot an engine for.</param>
        /// <param name="squads">The provider the rosters resolve from.</param>
        /// <param name="homeXi">The home club's fielded starting eleven, or null with no career wired.</param>
        /// <param name="awayXi">The away club's fielded starting eleven, or null with no career wired.</param>
        internal TacticalDirector.MatchEngine.MatchEngine BootFixtureEngine(
            in Fixture fixture, ISquadProvider squads, out int[] homeXi, out int[] awayXi)
        {
            // ── resolve → filter → configure (ERR-030-009); #44's suspension view joins at its T2. ──
            Squad home = SelectAvailable(ResolveSquad(squads, fixture.HomeClubId));
            Squad away = SelectAvailable(ResolveSquad(squads, fixture.AwayClubId));

            // The XI derivation sits against the squads ConfigureSquads consumes. Today both go
            // through the one LineupSelector walk (SquadRating is its public read); the colocation is
            // what makes a future explicit-XI seam edit this method, not a distant record site.
            homeXi = _career != null ? SquadRating.StartingElevenPlayerIds(home) : null;
            awayXi = _career != null ? SquadRating.StartingElevenPlayerIds(away) : null;

            ulong matchSeed = RoundResolutionModel.MatchSeedFor(in fixture, _state.Seed, _state.SeasonNumber);
            var engine = new TacticalDirector.MatchEngine.MatchEngine(matchSeed);
            engine.ConfigureSquads(home, away, MatchEntryFatigue(home), MatchEntryFatigue(away));
            return engine;
        }

        /// <summary>
        /// Resolves one club's roster, failing loud on an unresolvable id (F6). <see cref="ISquadProvider"/>
        /// returns null by contract so the consumer's gate — this one — decides, rather than any provider
        /// silently substituting a default roster.
        /// </summary>
        private static Squad ResolveSquad(ISquadProvider squads, int clubId)
        {
            Squad squad = squads.ResolveByClubId(clubId);
            if (squad == null)
            {
                throw new System.ArgumentException(
                    $"ISquadProvider cannot resolve club {clubId}; a round cannot be resolved without "
                    + "every participating club's roster (F6).",
                    nameof(squads));
            }

            return squad;
        }

        /// <summary>
        /// The FR-MD-023 availability filter, applied between resolving a roster and using it. With no
        /// career wired — and with one whose players are all fit, the common case though no longer
        /// every career (the dial has been ARMED since the balance pass) — this returns the same
        /// instance, so the resolved-and-configured path is reference-identical to the pre-T2 one.
        /// <para>
        /// Applied to quick-simmed fixtures as well as engine ones: the quick-sim rates a club by the XI
        /// it would field, and a club missing four first-choice players should be rated as such whether
        /// or not a human is watching.
        /// </para>
        /// </summary>
        private Squad SelectAvailable(Squad squad) =>
            _career == null ? squad : _career.SelectAvailable(squad);

        /// <summary>
        /// #29's per-local-index match-entry fatigue for a squad about to be configured, or <c>null</c>
        /// when no career is wired — which <c>ConfigureSquads</c> reads as "rested", its boot value.
        /// </summary>
        private float[] MatchEntryFatigue(Squad squad) =>
            _career == null ? null : _career.MatchEntryFatigue(squad);

        /// <summary>
        /// The FR-SN-016 match-outcome producer step. Records the result and nothing else — see
        /// <see cref="MatchOutcomes"/> for why it is session-scoped rather than serialized
        /// (ERR-030-013), and §4.6 / FR-SN-017 for why it calls no #22 ingest.
        /// </summary>
        private void EmitMatchOutcome(in MatchResult result) => _outcomes.Add(result);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-26 | —      | Initial implementation (#30 T2 / roadmap A4): the composition root  |
// |         |            |        | with the KD-2 fixed day-advance order (only the world tick live),   |
// |         |            |        | whole-round resolution routed by RoundResolutionMode, the FR-SN-016 |
// |         |            |        | producer record, the #44 availability-filter null seam, season      |
// |         |            |        | sub-blob Snapshot/Restore, and the read-only SeasonViewModel.       |
// | 1.1     | 2026-07-27 | —      | #30 T3 / roadmap A5: RollToNextSeason — the KD-6 restartable        |
// |         |            |        | boundary transform (finalize → board evaluate → regenerate →        |
// |         |            |        | reset), pure in the prior SeasonState so a save either side of it   |
// |         |            |        | restores to the same continuation. The (a') #43 and (b') #40        |
// |         |            |        | insertion points and (d) #28's age advance are declared positions,  |
// |         |            |        | not interfaces. Everything is computed and validated before any     |
// |         |            |        | write, and the throwing commit runs before the non-throwing one, so |
// |         |            |        | a refused roll leaves the season untouched. Plus the pure           |
// |         |            |        | ShiftCalendarToNextSeason / DeriveNextSeasonSeed helpers,           |
// |         |            |        | extracted so their branches are testable without driving a          |
// |         |            |        | 380-fixture season to its boundary first. (ShiftCalendarToNextSeason|
// |         |            |        | moved to SeasonCalendar.ShiftedToNextSeason at v1.3 below.)         |
// | 1.2     | 2026-07-27 | —      | #30 T3 AR: AdvanceDays now bounds the POST-season advance by the    |
// |         |            |        | day the next season would open. Its KD-4 guard covered only the     |
// |         |            |        | in-season case, so walking the close season past that day reached a |
// |         |            |        | career that could neither be played (season complete) nor rolled    |
// |         |            |        | (the derived calendar now opens in the past) — unrecoverable, and   |
// |         |            |        | it saved and reloaded cleanly. Plus: the step (b) job-security      |
// |         |            |        | arithmetic moved to BoardState.EvaluateAtSeasonEnd (it had          |
// |         |            |        | re-derived IsMetBy); EnginePlayedFixtures / MatchOutcomes docs      |
// |         |            |        | corrected — both span the season boundary, which T3 made true.      |
// | 1.3     | 2026-07-27 | —      | #30 T3 AR (L): the §3.5 step (c′) calendar shift moved to           |
// |         |            |        | SeasonCalendar.ShiftedToNextSeason — it was pure calendar           |
// |         |            |        | arithmetic on the composition root, next to nothing else that       |
// |         |            |        | understood round days, and it copied the day array twice and        |
// |         |            |        | re-validated an ordering it provably preserves. What stays here is  |
// |         |            |        | NextSeasonCalendar(): the choice of the [GT] close season, bound in |
// |         |            |        | one place and read by both AdvanceDays and RollToNextSeason.        |
// | 1.4     | 2026-07-27 | —      | #30 T3 AR pass 4 (doc): MatchOutcomes claimed results are ordered  |
// |         |            |        | by STRICTLY increasing WorldDay. They are non-decreasing — a round |
// |         |            |        | captures one worldDay and stamps every fixture in it with that —   |
// |         |            |        | so a consumer must not use WorldDay as a unique key.                |
// | 1.5     | 2026-08-06 | —      | Doc-only: the State summary quoted SeasonSaveManager.Save's four- |
// |         |            |        | argument form, which #29/#41 T1 replaced. Points at the method     |
// |         |            |        | instead, so the citation cannot go stale on the next signature     |
// |         |            |        | change.                                                            |
// | 1.6     | 2026-08-06 | —      | #29/#41 T2: the loop optionally drives a PlayerCareerStates. Slot |
// |         |            |        | 2 (#29 AdvanceTrainingDay) and slot 4 (#41 AdvanceMedicalDay) go  |
// |         |            |        | live in the KD-2 order — #41 after #29 so its risk reads the same-|
// |         |            |        | day conditioning (ERR-030-002) — both taking the world day BEFORE |
// |         |            |        | step 9's increment. ResolveFixture and PlayThroughEngine gain the |
// |         |            |        | FR-MD-023 availability filter at the pre-declared ERR-030-009     |
// |         |            |        | resolve→filter→configure position (#44 joins the same call), and  |
// |         |            |        | the engine path passes #29's match-entry fatigue, projected after |
// |         |            |        | the filter because filtering renumbers the local indices it is    |
// |         |            |        | keyed by. RollToNextSeason gains (d') the FR-TR-025 / FR-MD-025   |
// |         |            |        | roster reconciliation, before the commits so a refused roll       |
// |         |            |        | leaves the career untouched too. The career and its provider bind |
// |         |            |        | as a pair, and AdvanceAndPlayNextRound refuses a DIFFERENT        |
// |         |            |        | provider — two would train one league and play another, with      |
// |         |            |        | every symptom a plausible result rather than a crash. Unwired     |
// |         |            |        | (careerOrNull == null) is byte-identical to v1.5 throughout.      |
// | 1.7     | 2026-08-06 | —      | T2 AR pass 1 (2M). The constructor now requires the career to     |
// |         |            |        | carry every club in the season — this is the only layer holding   |
// |         |            |        | both, the same argument that puts the KD-4 cursor invariant here, |
// |         |            |        | and a career over a subset otherwise constructed happily and then |
// |         |            |        | threw from the availability filter part-way through a round,      |
// |         |            |        | after earlier fixtures had been applied to the table. And (d′)    |
// |         |            |        | splits: the roster reconciliation is STAGED at (d′) and INSTALLED |
// |         |            |        | after (e)'s commits. v1.6 wrote it before BeginNextSeason — the   |
// |         |            |        | one commit this method's own docs call fallible — so a refused    |
// |         |            |        | roll left a career reconciled against a season that never began,  |
// |         |            |        | flatly contradicting the comment that claimed otherwise. Plus an  |
// |         |            |        | INTERNAL World accessor, so the new SeasonSaveManager.Save        |
// |         |            |        | overload can capture a whole season from the loop alone without   |
// |         |            |        | handing every caller AdvanceDay() past the KD-4 bounds.           |
// | 1.8     | 2026-08-06 | —      | T2 AR passes 3-5 (1M + 2L). **M (pass 5):** where the ROUND sits |
// |         |            |        | in the KD-2 day order was settled by AdvanceToNextFixtureDay's   |
// |         |            |        | loop condition and declared nowhere. It stops on REACHING the    |
// |         |            |        | fixture day, so matchday's own steps 2 and 4 run after the round: |
// |         |            |        | right for #41's occurrence draw, wrong for the recovery countdown |
// |         |            |        | that shares its atomic step, which makes every injury one         |
// |         |            |        | matchday longer than its tier. Splitting the halves would change |
// |         |            |        | #41's contract, so the convention is stated on three methods,    |
// |         |            |        | filed as ERR-030-026 and locked by a test — the balance pass now  |
// |         |            |        | fits recovery tiers against a written rule, not an emergent one.  |
// |         |            |        | **L:** the quick-sim's silence about match-entry fatigue is now   |
// |         |            |        | an argued omission rather than an unremarked one (the filter one  |
// |         |            |        | line above argues the opposite way for itself); the v1.7 row's    |
// |         |            |        | "public World accessor" corrected to internal, which is what      |
// |         |            |        | landed; Career's summary points at TrySetFocus, since ScheduleFor |
// |         |            |        | is no longer the public focus surface.                            |
// | 1.9     | 2026-08-07 | —      | Balance pass D1 (ERR-030-027): AdvanceAndPlayNextRound runs the   |
// |         |            |        | fixture day's own career day-steps pre-round, through the new     |
// |         |            |        | RunCareerDaySteps helper both callers share — slots 1-8 extracted |
// |         |            |        | from RunWorldTickInFixedOrder, idempotent per day via the two     |
// |         |            |        | live steps' own cursors, so the post-round re-run is a no-op.     |
// |         |            |        | Closes the recovery half of ERR-030-026: a player whose recovery  |
// |         |            |        | expires on matchday now plays it, tiers mean what they say, and   |
// |         |            |        | the occurrence draw sits on matchday morning where the FR-MD-010  |
// |         |            |        | appearance window (which never contains today) feeds it. Placed   |
// |         |            |        | after every guard so a refused call advances no cursor. #41's     |
// |         |            |        | step contract (FR-MD-022) is untouched — this is a #30 wiring     |
// |         |            |        | decision, the cheaper shape the balance-pass council converged    |
// |         |            |        | on over splitting the step and bumping the medical format.        |
// | 1.10    | 2026-08-07 | —      | Balance pass D2 (ERR-041-010(b)): after each fixture resolves,    |
// |         |            |        | RecordFieldedAppearances writes both clubs' fielded XIs into the  |
// |         |            |        | career's appearance record via SquadRating.StartingElevenPlayer-  |
// |         |            |        | Ids — the same single TrySelect walk, so the recorded eleven IS   |
// |         |            |        | the fielded eleven on both resolution paths.                      |
// | 1.11    | 2026-08-07 | —      | Balance-pass AR pass 1 (3L): RecordFieldedAppearances moves ABOVE |
// |         |            |        | the apply/emit/mark sequence — the only fallible call in the      |
// |         |            |        | block, and a throw after MarkFixturePlayed strands the round      |
// |         |            |        | unrecoverably; RunCareerDaySteps' seam list renumbered to the     |
// |         |            |        | spec's 0-12 order (slot 0 was missing, the tick was "9");         |
// |         |            |        | PlayThroughEngine's summary no longer states the retired          |
// |         |            |        | ERR-030-026 convention.                                           |
// | 1.12    | 2026-08-07 | —      | Balance-pass AR pass 2 (M): the fielded XIs come OUT of           |
// |         |            |        | ResolveFixture — the engine branch derives them inside            |
// |         |            |        | BootFixtureEngine one statement from the ConfigureSquads that     |
// |         |            |        | consumes the same squad instances, the quick-sim branch from the  |
// |         |            |        | very squads its rating reads — replacing the loop's second        |
// |         |            |        | SelectAvailable walk, which was an unenforced agreement with the  |
// |         |            |        | engine's configuration (the documented-not-structural class).     |
// |         |            |        | Slots 9-11 join the seam list. (Rows 1.11/1.12 were appended out  |
// |         |            |        | of order and swapped at AR pass 3 — L2.)                          |
// | 1.13    | 2026-08-08 | —      | Balance-pass AR pass 3 (L): the recording goes through the new    |
// |         |            |        | RecordFixtureAppearances pair form — BOTH clubs validated before  |
// |         |            |        | EITHER is written, so a refused away side no longer leaves the    |
// |         |            |        | home XI carrying an appearance for a fixture never applied.       |
// | 1.14    | 2026-08-08 | —      | Balance-pass AR pass 5 (M5, doc only): a stale "every career   |
// |         |            |        | until the dial is armed" sentence updated — the dial has been  |
// |         |            |        | armed since the balance pass.                                  |
// | 1.15    | 2026-08-08 | —      | Balance-pass AR pass 6 (M3 + L1): the constructor pairs the    |
// |         |            |        | career against the world clock (RequireCursorsWithinClock)    |
// |         |            |        | beside its KD-4 and coverage gates; three step-9 prose sites  |
// |         |            |        | corrected to step 12 (step 9 is the #32 scouting null seam    |
// |         |            |        | under pass 1's own renumbering).                              |
// | 1.16    | 2026-08-08 | —      | #28 T2a: optional ProgressionEngine; slot 1 LIVE (gathers the  |
// |         |            |        | #29 batch via PlayerCareerStates.GatherTrainingInputs and hands|
// |         |            |        | it to the FR-PG-021 batch AdvanceDay); the constructor REFUSES a|
// |         |            |        | separately-supplied ISquadProvider when a store is present and |
// |         |            |        | projects one from it instead (KD-4 single roster authority), and|
// |         |            |        | gates the store's coverage + its cursor vs the world clock     |
// |         |            |        | (ERR-028-007).                                                 |
// | 1.17    | 2026-08-09 | —      | AR pass 2 (H1 + M1), ERR-028-013: an EMPTY ProgressionEngine is |
// |         |            |        | no longer treated as a wired roster authority — it carries no   |
// |         |            |        | rosters, and treating it as wired made the pre-#28 save the save |
// |         |            |        | root deliberately WRITES impossible to resume, since            |
// |         |            |        | SeasonSaveContents.Progression is never null. The pairing rule   |
// |         |            |        | also splits: a career still needs a provider, but a populated    |
// |         |            |        | store now composes WITHOUT #29/#41 career state, which is what   |
// |         |            |        | makes slot 1's `_career == null` Neutral branch reachable — it   |
// |         |            |        | had been provably dead since it was written.                    |
#endregion
