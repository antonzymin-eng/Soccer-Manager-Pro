// File:     src/season-save/SeasonLoop.cs
// Created:  2026-07-26
// Modified: 2026-07-27
// Author:   —
// Spec:     Season & Competition Loop #30 §3.3 (day advance / KD-2 tick order), §3.4 (playing a round /
//           KD-9), §3.5 (season-boundary roll / KD-6), §4.3 (the composition root), §4.6 (the #22
//           producer boundary / KD-3), §4.7 (CS0104);
//           FR-SN-010/011/012/013/013a/013b/016/017/018/025/026/029/030/031/032/033/034;
//           path-to-playable A4 + A5; Code Standards #20
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

using TacticalDirector.LivingWorld;
using TacticalDirector.MatchEngine;
using TacticalDirector.PlayerDatabase;

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
        /// <exception cref="System.ArgumentNullException">A required reference is null.</exception>
        /// <exception cref="System.ArgumentException">
        /// The KD-4 cursor invariant is already violated: the world clock has passed the season's pending
        /// round (F4). Checked here as well as at <c>SeasonSaveManager.Load</c> because a loop can be
        /// composed from a freshly built world and an advanced season without any file involved.
        /// </exception>
        public SeasonLoop(
            WorldStore world,
            SeasonState season,
            RoundResolutionMode mode = RoundResolutionMode.ManagedThroughEngine)
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

            _world = world;
            _state = season;
            Mode = mode;
        }

        /// <summary>How this loop resolves a round's fixtures (§3.4.1).</summary>
        public RoundResolutionMode Mode { get; }

        /// <summary>The world's current calendar day (<c>WorldStore.CurrentWorldTick</c>).</summary>
        public uint CurrentWorldDay => _world.CurrentWorldTick;

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
        /// The season this loop drives — the object <c>SeasonSaveManager.Save(world, season, match, path)</c>
        /// needs, and the one #38 binds richer screens to than <see cref="SeasonViewModel"/> carries.
        /// <para>
        /// Exposing the object does <b>not</b> weaken FR-SN-032: every <see cref="SeasonState"/> mutator is
        /// <c>internal</c> to this assembly, so an outside caller holding this reference can only read.
        /// Inside the assembly the single-writer contract is upheld by this class being the only
        /// production code that touches those mutators.
        /// </para>
        /// </summary>
        public SeasonState State => _state;

        /// <summary>
        /// Advances the world one calendar day at a time, in the KD-2 fixed order, until the clock sits ON
        /// the next unplayed round's fixture day (§3.3 / FR-SN-010). Returns the number of days advanced —
        /// zero if the clock is already there.
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
        /// (F6) — the #27 <see cref="ISquadProvider"/> fail-loud contract.</exception>
        public MatchResult[] AdvanceAndPlayNextRound(ISquadProvider squads)
        {
            if (squads == null)
            {
                throw new System.ArgumentNullException(nameof(squads));
            }

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
            var results = new MatchResult[indices.Length];

            for (int i = 0; i < indices.Length; i++)
            {
                Fixture fixture = _state.FixtureAt(indices[i]);
                MatchResult result = ResolveFixture(in fixture, squads, worldDay);

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

            // ── (d) #28 age advance inserts HERE — empty until #28 T2. ──────────────────────────

            // ── (e) commit: schedule + table + season number + seed, then the board verdict ─────
            int completedSeasonNumber = _state.SeasonNumber;
            _state.BeginNextSeason(nextSeed, nextFixtures, nextCalendar);

            // Cannot throw: EvaluateAtSeasonEnd returns an already-validated BoardState.
            _state.SetBoard(evaluated);

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
        /// <exception cref="System.ArgumentException">The blob is malformed (F3) or the restored pair
        /// violates the cursor invariant (F4).</exception>
        public static SeasonLoop Restore(
            WorldStore world,
            byte[] seasonBlob,
            RoundResolutionMode mode = RoundResolutionMode.ManagedThroughEngine)
        {
            return new SeasonLoop(world, SeasonStateCodec.Decode(seasonBlob), mode);
        }

        /// <summary>
        /// One calendar day, in the KD-2 pinned order (§3.3). Only the world-day tick is live; every
        /// other step is a <b>documented position</b>, not an interface (FR-SN-034 / FR-LW-031) — each
        /// Wave-2+ spec slots into its pre-declared slot when it lands, so fixing the order now avoids a
        /// re-pin across all of them.
        /// <para>
        /// With only step 9 live, a no-fixture day's advance is byte-identical to a bare
        /// <see cref="WorldStore.AdvanceDay"/> (FR-SN-026 / KD-8) — which is exactly what the
        /// behaviour-neutral floor test asserts.
        /// </para>
        /// </summary>
        private void RunWorldTickInFixedOrder()
        {
            // 1. progression   (#28) — NULL SEAM (its T0 core is built but unwired; #28 T2 wires it here)
            // 2. training      (#29) — NULL SEAM
            // 3. human-systems (#33) — NULL SEAM
            // 4. injuries      (#41) — NULL SEAM (ERR-030-002: after #28/#29 so it reads the day's
            //                          updated fatigue/condition; before the world-day tick)
            // 5. transfers     (#31) — NULL SEAM (ERR-030-004: a deep-tier position reservation;
            //                          minimal transfers are command-driven)
            // 6. staff         (#34) — NULL SEAM (ERR-030-006: deep-tier position reservation;
            //                          #34's scaffold projections are pull-based)
            // 7. academy       (#42) — NULL SEAM (ERR-030-007: the youth-intake one-shot, latched on
            //                          LastIntakeWorldDay; live at #42's own T-phase)
            // 8. board         (#45) — NULL SEAM (ERR-030-008: one bounded integer drift per modelled
            //                          club; live at #45's own T-phase)
            // 9. world day     — the only LIVE tick.
            _world.AdvanceDay();
        }

        /// <summary>
        /// Routes one fixture to its producer (§3.4 / FR-SN-013 / FR-SN-013b): the managed club's fixture
        /// through the full engine under <see cref="RoundResolutionMode.ManagedThroughEngine"/>, every
        /// fixture through the engine under <see cref="RoundResolutionMode.FullEngine"/>, everything else
        /// through <see cref="RoundResolutionModel"/>.
        /// </summary>
        private MatchResult ResolveFixture(in Fixture fixture, ISquadProvider squads, uint worldDay)
        {
            bool managed = fixture.Involves(_state.ManagedClubId);

            if (ShouldPlayThroughEngine(Mode, managed))
            {
                return PlayThroughEngine(in fixture, squads, worldDay);
            }

            Squad home = ResolveSquad(squads, fixture.HomeClubId);
            Squad away = ResolveSquad(squads, fixture.AwayClubId);

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
        /// <b>#44 availability-filter null seam (ERR-030-009).</b> The flow is resolve → <i>filter</i> →
        /// configure: a suspension-availability view may reduce the resolved squad by value copy between
        /// the two lines below. Empty until #44 T2 wires it; the position is declared here so wiring it
        /// changes one call, not this method's shape.
        /// </para>
        /// </summary>
        private MatchResult PlayThroughEngine(in Fixture fixture, ISquadProvider squads, uint worldDay)
        {
            Squad home = ResolveSquad(squads, fixture.HomeClubId);
            Squad away = ResolveSquad(squads, fixture.AwayClubId);

            // ── #44 availability filter inserts HERE (ERR-030-009) — empty at Stage 2. ──

            ulong matchSeed = RoundResolutionModel.MatchSeedFor(in fixture, _state.Seed, _state.SeasonNumber);
            var engine = new TacticalDirector.MatchEngine.MatchEngine(matchSeed);
            engine.ConfigureSquads(home, away);

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
#endregion
