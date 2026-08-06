// File:     src/season-save/PlayerCareerStates.cs
// Created:  2026-08-06
// Author:   —
// Spec:     Training System #29 §3.1/§3.3/§3.5, §4.3 (seam contracts), FR-TR-004/016/022/023/025;
//           Injuries & Medical #41 §3.1/§3.5, §4.3, FR-MD-003/009/010/022/023/025/027;
//           Season & Competition Loop #30 §3.3 (KD-2 slot order), §3.5 (the boundary), FR-SN-034;
//           path-to-playable D2/D3 (T2); Code Standards #20
// Purpose:  The #30-side owner of the per-club #29 training and #41 medical state — the thing that was
//           missing at T1, when both codecs existed and nothing constructed a state set for them to
//           encode. Holds both sets keyed by (ClubId, PlayerId), drives the two day steps at #30's
//           slot-2 / slot-4, answers the availability question squad selection reads, projects
//           match-entry fatigue, and keeps roster membership in lockstep (FR-TR-025 / FR-MD-025).

using System;
using System.Collections.Generic;

using TacticalDirector.InjuriesMedical;
using TacticalDirector.MatchEngine;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.TrainingSystem;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// The per-club #29 training / #41 medical state a career carries between matches, and the single
    /// place #30 invokes either subsystem from.
    /// <para>
    /// <b>Why both sets live in one type.</b> They are advanced on the same world day, over the same
    /// roster, in a pinned order — #29's slot-2 step must run before #41's slot-4 step, because #41's
    /// risk assembly reads the training fatigue and conditioning that step just wrote (#41 KD-6 /
    /// §3.5). Holding them in two independent objects would make that ordering a convention the call
    /// site has to remember; holding them here makes it a property of two methods on one object whose
    /// key sets are checked to agree.
    /// </para>
    /// <para>
    /// <b>#41's occurrence draw is OFF by default</b> (<see cref="InjuryOccurrenceEnabled"/>,
    /// FR-MD-027). The wiring is what T2 delivers; arming it is the balance pass's, and the reason is
    /// measured rather than cautious — see the property's own documentation.
    /// </para>
    /// <para>
    /// <b>One-way composition.</b> #29 and #41 reference neither #30 nor each other's step; this type
    /// is above both and calls into them (FR-TR-024 / FR-MD-026). Nothing here parses either save
    /// block — it hands whole state sets to the codecs and takes whole state sets back
    /// (<see cref="TrainingBlocks"/> / <see cref="FromBlocks"/>).
    /// </para>
    /// <para>
    /// Off the 60 Hz hot path (it runs on the world tick), so allocation is permitted — the
    /// <see cref="SeasonLoop"/> / <see cref="SeasonSaveManager"/> precedent.
    /// </para>
    /// <para>
    /// THREAD SAFETY: none, matching <see cref="SeasonState"/>'s single-threaded contract.
    /// </para>
    /// </summary>
    public sealed class PlayerCareerStates
    {
        // Per club, parallel and ascending by ClubId. Each club's PlayerIds are ascending too, so a
        // lookup is a binary search and the codecs' canonical-order requirement is satisfied by
        // construction rather than by a sort at encode time.
        private readonly List<int> _clubIds = new List<int>();
        private readonly List<int[]> _playerIds = new List<int[]>();
        private readonly List<TrainingState[]> _training = new List<TrainingState[]>();
        private readonly List<InjuryState[]> _injury = new List<InjuryState[]>();

        private PlayerCareerStates(bool injuryOccurrenceEnabled)
        {
            InjuryOccurrenceEnabled = injuryOccurrenceEnabled;
        }

        /// <summary>
        /// Increments every time <see cref="SyncToRoster"/> replaces a club's arrays — the generation
        /// stamp a cached <see cref="TrainingSchedule"/> is only valid for.
        /// <para>
        /// <b>Why this exists.</b> <see cref="ScheduleFor"/> hands out a handle that binds a club's
        /// live id and state arrays by reference, and <see cref="SyncToRoster"/> replaces both arrays
        /// wholesale. A schedule cached across a season boundary therefore writes into a detached array:
        /// <c>TrySetFocus</c> returns <c>true</c>, nothing throws, and the manager's training
        /// instruction is silently gone. A screen caching the handle is the obvious thing to write, so
        /// the staleness needs to be detectable rather than merely documented — compare this against the
        /// value read when the handle was acquired.
        /// </para>
        /// </summary>
        public int RosterGeneration { get; private set; }

        /// <summary>
        /// The #41 KD-8 dial (FR-MD-027). <b>Off by default, deliberately.</b>
        /// <para>
        /// The fifth adversarial-review pass over #41's T0 landing measured the daily occurrence
        /// probability through the real producer chain rather than through a forced risk, and it is two
        /// to three orders of magnitude out at career inputs in both directions: a freshly inserted
        /// player is ~23% likely to be injured on his first day (his conditioning starts
        /// <c>ConditionMax − ConditionStart</c> below the ceiling and that shortfall carries weight 1 on
        /// the very scale the draw denominator derives from), a half-fatigued player ~43% per day, and
        /// the default <see cref="TrainingFocus.Balanced"/> focus converges on exactly 0 forever. Those
        /// three numbers are locked by a characterization test in #41's own suite so the balance pass
        /// leaves a visible diff, and KD-W1 forbids re-tuning a <c>[GT]</c> ahead of that pass.
        /// </para>
        /// <para>
        /// So T2 wires the call path and leaves it disarmed: with the dial off, <c>AdvanceMedicalDay</c>
        /// reduces to the recovery countdown with no draw at all, which for a career that has never
        /// been injured is a no-op over the cursor. Everything downstream of an injury — the
        /// availability filter, the depleted-squad back-fill, the view models — is live and tested
        /// against directly-constructed injured states, so flipping this dial at the balance pass is a
        /// one-argument change rather than a second wiring pass.
        /// </para>
        /// </summary>
        public bool InjuryOccurrenceEnabled { get; }

        /// <summary>The number of clubs carried.</summary>
        public int ClubCount => _clubIds.Count;

        /// <summary>
        /// Whether this career carries state for <paramref name="clubId"/> — the composition-time check
        /// <see cref="SeasonLoop"/>'s constructor uses to refuse a career that does not cover the season
        /// it is bound to.
        /// <para>
        /// Without it, a career built over a subset of the league constructs and advances days happily
        /// (the day steps iterate the career's clubs, not the season's) and then throws from
        /// <see cref="SelectAvailable"/> on fixture 3 of 10 — after two results have already been
        /// applied to the table and marked played. Better to refuse the pairing than to half-resolve a
        /// round.
        /// </para>
        /// </summary>
        /// <param name="clubId">The club to look for.</param>
        public bool CarriesClub(int clubId)
        {
            for (int c = 0; c < _clubIds.Count; c++)
            {
                if (_clubIds[c] == clubId)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// A fresh career's state: <see cref="TrainingState.Create"/> on
        /// <see cref="TrainingFocus.Balanced"/> and <see cref="InjuryState.Create"/> for every player of
        /// every club, built from the rosters themselves.
        /// <para>
        /// Both factories are used rather than <c>default</c> for the reason FR-TR-025 / FR-MD-025 give:
        /// a defaulted state carries <c>LastAdvancedWorldDay == 0</c>, which is a legitimate world day,
        /// so the first advance of day 0 would read as "already advanced" and that player would never
        /// accrue or be evaluated again — silently, forever.
        /// </para>
        /// </summary>
        /// <param name="squads">Resolves each club id to its roster. A <see cref="League"/> is one.</param>
        /// <param name="clubIds">The clubs to build state for. Order is irrelevant; duplicates are refused.</param>
        /// <param name="injuryOccurrenceEnabled">The #41 KD-8 dial; see <see cref="InjuryOccurrenceEnabled"/>.</param>
        /// <exception cref="ArgumentNullException">A required argument is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="clubIds"/> repeats a club, or a club cannot be resolved to a roster.</exception>
        public static PlayerCareerStates ForLeague(
            ISquadProvider squads, int[] clubIds, bool injuryOccurrenceEnabled = false)
        {
            if (squads == null)
            {
                throw new ArgumentNullException(nameof(squads));
            }

            if (clubIds == null)
            {
                throw new ArgumentNullException(nameof(clubIds));
            }

            var career = new PlayerCareerStates(injuryOccurrenceEnabled);

            var ascending = new int[clubIds.Length];
            Array.Copy(clubIds, ascending, clubIds.Length);
            Array.Sort(ascending);

            for (int c = 0; c < ascending.Length; c++)
            {
                if (c > 0 && ascending[c] == ascending[c - 1])
                {
                    throw new ArgumentException(
                        $"clubIds repeats club {ascending[c]}; a club has exactly one state block.",
                        nameof(clubIds));
                }

                int clubId = ascending[c];
                Squad squad = ResolveSquad(squads, clubId);

                int[] ids = AscendingPlayerIds(squad);
                var training = new TrainingState[ids.Length];
                var injury = new InjuryState[ids.Length];
                for (int i = 0; i < ids.Length; i++)
                {
                    training[i] = TrainingState.Create(TrainingFocus.Balanced);
                    injury[i] = InjuryState.Create();
                }

                career._clubIds.Add(clubId);
                career._playerIds.Add(ids);
                career._training.Add(training);
                career._injury.Add(injury);
            }

            return career;
        }

        /// <summary>
        /// Rebuilds a career's state from the two decoded save blocks — the counterpart of
        /// <see cref="TrainingBlocks"/> / <see cref="MedicalBlocks"/>, and what
        /// <see cref="SeasonSaveContents"/> is fed into after a load.
        /// <para>
        /// <b>This is the one place the two blocks are checked against each other</b>, and the only
        /// layer that can be: each codec sees one block and is forbidden to read the other (#29 §4.4 /
        /// #41 §4.4 blob independence), so "the training block and the medical block describe the same
        /// squads" is a coherence rule with no other owner — the same argument that puts the KD-4 cursor
        /// invariant on <see cref="SeasonSaveManager.Load"/>. A mismatch means one block was hand-edited,
        /// truncated, or paired with another save's sibling, and every later step — the day advance, the
        /// availability read, the roster sync — would then be operating on two different squads.
        /// </para>
        /// </summary>
        /// <param name="training">The decoded #29 blocks.</param>
        /// <param name="medical">The decoded #41 blocks.</param>
        /// <param name="injuryOccurrenceEnabled">The #41 KD-8 dial; see <see cref="InjuryOccurrenceEnabled"/>.</param>
        /// <exception cref="ArgumentNullException">Either array is null.</exception>
        /// <exception cref="ArgumentException">The two blocks disagree on the club set or on any club's player set.</exception>
        public static PlayerCareerStates FromBlocks(
            ClubTrainingStates[] training,
            ClubInjuryStates[] medical,
            bool injuryOccurrenceEnabled = false)
        {
            if (training == null)
            {
                throw new ArgumentNullException(nameof(training));
            }

            if (medical == null)
            {
                throw new ArgumentNullException(nameof(medical));
            }

            if (training.Length != medical.Length)
            {
                throw new ArgumentException(
                    $"The training block carries {training.Length} clubs and the medical block "
                    + $"{medical.Length}; the two describe the same career and must agree.",
                    nameof(medical));
            }

            var career = new PlayerCareerStates(injuryOccurrenceEnabled);

            for (int c = 0; c < training.Length; c++)
            {
                ClubTrainingStates t = training[c];
                ClubInjuryStates m = medical[c];

                // A default-valued block: its constructor refuses nulls, so the only way to hold one is
                // to have never constructed it (`new ClubTrainingStates[n]`). Caught here rather than
                // three methods later as a null-reference on an array nobody can trace back.
                if (t.PlayerIds == null || t.States == null || m.PlayerIds == null || m.States == null)
                {
                    throw new ArgumentException(
                        $"Block {c} is a default value, not a constructed one — a career cannot be "
                        + "rebuilt from blocks that were never populated.",
                        t.PlayerIds == null || t.States == null ? nameof(training) : nameof(medical));
                }

                if (t.ClubId != m.ClubId)
                {
                    throw new ArgumentException(
                        $"Block {c}: the training block is club {t.ClubId} and the medical block is "
                        + $"club {m.ClubId}. Both codecs canonicalize to ascending ClubId, so this is a "
                        + "mismatched pair, not a re-ordering.",
                        nameof(medical));
                }

                if (t.Count != m.Count)
                {
                    throw new ArgumentException(
                        $"Club {t.ClubId}: the training block carries {t.Count} players and the medical "
                        + $"block {m.Count}.",
                        nameof(medical));
                }

                for (int i = 0; i < t.Count; i++)
                {
                    if (t.PlayerIds[i] != m.PlayerIds[i])
                    {
                        throw new ArgumentException(
                            $"Club {t.ClubId}, entry {i}: the training block holds player "
                            + $"{t.PlayerIds[i]} and the medical block player {m.PlayerIds[i]}.",
                            nameof(medical));
                    }

                    // EVERY lookup in this class is a binary search over these ids (IndexOfPlayer), so
                    // ascending order is not a formatting preference — it is the precondition the whole
                    // type runs on. Both codecs canonicalize and gate it, but ClubTrainingStates'
                    // constructor does not, and this method is public: an out-of-order block would make
                    // IndexOfPlayer miss a player who IS carried, and SyncToRoster would then read that
                    // miss as "new" and overwrite his season of state with Create(). Silent, total, and
                    // indistinguishable from a fresh career. ForLeague sorts; this checks.
                    if (i > 0 && t.PlayerIds[i] <= t.PlayerIds[i - 1])
                    {
                        throw new ArgumentException(
                            $"Club {t.ClubId}: player ids must be strictly ascending (entry {i} is "
                            + $"{t.PlayerIds[i]} after {t.PlayerIds[i - 1]}). Every lookup here is a "
                            + "binary search over them.",
                            nameof(training));
                    }
                }

                if (c > 0 && t.ClubId <= career._clubIds[c - 1])
                {
                    throw new ArgumentException(
                        $"Club ids must be strictly ascending; club {t.ClubId} follows "
                        + $"{career._clubIds[c - 1]}.",
                        nameof(training));
                }

                // The player-id arrays are shared, not copied: the two blocks agree on them by the
                // check above, and both codecs already canonicalized them ascending. The state arrays
                // are the ones this type mutates, and each block owns its own.
                career._clubIds.Add(t.ClubId);
                career._playerIds.Add(t.PlayerIds);
                career._training.Add(t.States);
                career._injury.Add(m.States);
            }

            return career;
        }

        /// <summary>
        /// The #29 state as the per-club blocks <c>TrainingSaveCodec.Encode</c> takes — what
        /// <see cref="SeasonSaveManager.Save"/> is handed.
        /// <para>
        /// <b><c>internal</c>, deliberately.</b> The blocks <b>borrow</b> the live arrays rather than
        /// copying them (the <see cref="ClubTrainingStates"/> posture the codec needs), and
        /// <c>ClubTrainingStates.States</c> is a public field — so a public accessor here would hand
        /// every holder of <see cref="SeasonLoop.Career"/> a direct write into a player's
        /// <c>Condition</c>, <c>TrainingFatigue</c> and idempotency cursor, bypassing
        /// <c>TrainingStep.AdvanceTrainingDay</c> and <c>TrainingSchedule.TrySetFocus</c> — the only two
        /// declared writers (FR-TR-004 / FR-TR-023). That is the single-writer property
        /// <see cref="SeasonState"/> enforces by keeping its mutators <c>internal</c>, and it is
        /// enforced here the same way. External callers save through
        /// <see cref="SeasonSaveManager.Save(SeasonLoop,MatchEngine.MatchEngine,string)"/> and read
        /// through <see cref="TrainingView"/>.
        /// </para>
        /// </summary>
        internal ClubTrainingStates[] TrainingBlocks()
        {
            var blocks = new ClubTrainingStates[_clubIds.Count];
            for (int c = 0; c < blocks.Length; c++)
            {
                blocks[c] = new ClubTrainingStates(_clubIds[c], _playerIds[c], _training[c]);
            }

            return blocks;
        }

        /// <summary>The #41 state as the per-club blocks <c>MedicalSaveCodec.Encode</c> takes, <c>internal</c> on the same borrowing / single-writer terms as <see cref="TrainingBlocks"/>.</summary>
        internal ClubInjuryStates[] MedicalBlocks()
        {
            var blocks = new ClubInjuryStates[_clubIds.Count];
            for (int c = 0; c < blocks.Length; c++)
            {
                blocks[c] = new ClubInjuryStates(_clubIds[c], _playerIds[c], _injury[c]);
            }

            return blocks;
        }

        /// <summary>
        /// #30's <b>slot-2</b> training seam (#29 §3.5 / FR-TR-004): one
        /// <c>TrainingStep.AdvanceTrainingDay</c> per player of every club, in ascending
        /// <c>(ClubId, PlayerId)</c> order.
        /// <para>
        /// Idempotent per world day and fail-loud on a day gap — both are #29's own contracts (F6/F7),
        /// which this method deliberately does not soften: a gap means the world clock was advanced
        /// around this loop, and silently accruing one day for a week that passed would be the quieter
        /// of two wrong answers.
        /// </para>
        /// <para>
        /// <b>Not validate-all-then-write, unlike <see cref="SyncToRoster"/> — and that asymmetry is
        /// deliberate.</b> A throw part-way through leaves the day half-advanced across clubs, which is
        /// harmless here <i>because</i> of the F6 idempotency above: re-running the same day after
        /// fixing the roster is a no-op for whoever already advanced and completes the rest. The sync
        /// has no such property — it rebuilds arrays rather than stepping a cursor — so it must stage
        /// everything before installing anything.
        /// </para>
        /// <para>
        /// <b>Behaviour-neutral on the defaults.</b> Every player starts on
        /// <see cref="TrainingFocus.Balanced"/>, whose daily load equals
        /// <c>TrainingSystemConstants.FatigueDailyRecovery</c> exactly, so the training-fatigue
        /// accumulator never leaves 0 and <see cref="MatchEntryFatigue"/> projects 0 — a match booted
        /// through this wiring is byte-identical to one booted without it until a focus is set.
        /// </para>
        /// </summary>
        /// <param name="worldDay">The world day being advanced — #30's clock BEFORE its day-9 increment.</param>
        /// <param name="squads">The rosters the attributes are read from; must match the state's roster (see <see cref="SyncToRoster"/>).</param>
        /// <param name="coach">The #34 staff seam; <see cref="CoachingModifier.Identity"/> until #34 lands.</param>
        /// <exception cref="ArgumentNullException"><paramref name="squads"/> is null.</exception>
        /// <exception cref="ArgumentException">A club or a held player id cannot be resolved against <paramref name="squads"/>, or #29 refuses the day.</exception>
        public void AdvanceTrainingDay(uint worldDay, ISquadProvider squads, CoachingModifier coach)
        {
            if (squads == null)
            {
                throw new ArgumentNullException(nameof(squads));
            }

            for (int c = 0; c < _clubIds.Count; c++)
            {
                Squad squad = ResolveSquad(squads, _clubIds[c]);
                int[] ids = _playerIds[c];
                TrainingState[] states = _training[c];

                for (int i = 0; i < ids.Length; i++)
                {
                    int local = RequireLocalIndex(squad, _clubIds[c], ids[i]);
                    PlayerRecord record = squad.GetPlayer(local);
                    TrainingStep.AdvanceTrainingDay(
                        ref states[i], in record.Attributes, in coach, worldDay);
                }
            }
        }

        /// <summary>
        /// #30's <b>slot-4</b> injuries seam (#41 §3.5 / FR-MD-022): the recovery countdown, then — when
        /// <see cref="InjuryOccurrenceEnabled"/> — the keyed occurrence draw, per player of every club.
        /// <para>
        /// <b>This must run after <see cref="AdvanceTrainingDay"/> for the same day</b>, which is the
        /// whole reason #41's slot sits after #28/#29/#33 and before the world-day tick (KD-6, the
        /// ERR-030-002 back-prop): the risk score reads #29's <i>same-day</i> conditioning and fatigue
        /// through <c>TrainingStep.ComputeInjuryRisk</c>, not yesterday's. Reversing the two would not
        /// throw — it would quietly price every injury off a one-day-stale training state.
        /// </para>
        /// <para>
        /// <b><c>MatchLoad.None</c> is passed, and that is a recorded remainder rather than an
        /// oversight.</b> FR-MD-010 makes the match-load term the caller's to supply, and an exact
        /// per-player appearance record is #30-side state that neither #29's nor #41's save block may
        /// carry (each is forbidden to describe the other's domain), so it needs a persisted home and a
        /// format decision this landing does not make. Recomputing it from the fixture list instead is
        /// not equivalent once the availability filter starts changing who actually played. The term is
        /// inert while <see cref="InjuryOccurrenceEnabled"/> is off — <c>AssembleRiskScore</c> is not
        /// even reached — so nothing today depends on it; it is due with the balance pass that arms the
        /// dial.
        /// </para>
        /// </summary>
        /// <param name="worldDay">The world day being advanced — #30's clock BEFORE its day-9 increment.</param>
        /// <param name="worldSeed">The career's world seed (<c>WorldStore.WorldSeed</c>), the draw key's root — never a per-match seed.</param>
        /// <param name="squads">The rosters the attributes are read from; must match the state's roster.</param>
        /// <param name="medical">The #34 staff seam; <see cref="MedicalModifier.Identity"/> until #34 lands.</param>
        /// <exception cref="ArgumentNullException"><paramref name="squads"/> is null.</exception>
        /// <exception cref="ArgumentException">A club or a held player id cannot be resolved against <paramref name="squads"/>, or #41 refuses the day or the state.</exception>
        public void AdvanceMedicalDay(
            uint worldDay, ulong worldSeed, ISquadProvider squads, MedicalModifier medical)
        {
            if (squads == null)
            {
                throw new ArgumentNullException(nameof(squads));
            }

            for (int c = 0; c < _clubIds.Count; c++)
            {
                Squad squad = ResolveSquad(squads, _clubIds[c]);
                int[] ids = _playerIds[c];
                TrainingState[] training = _training[c];
                InjuryState[] injury = _injury[c];

                for (int i = 0; i < ids.Length; i++)
                {
                    int local = RequireLocalIndex(squad, _clubIds[c], ids[i]);
                    PlayerRecord record = squad.GetPlayer(local);

                    // The risk is an occurrence-draw input and nothing else, so with the dial off it is
                    // read by nobody: #41's step only reaches AssembleRiskScore inside the
                    // `wasAvailableAtEntry && occurrenceEnabled` branch. Computing it anyway would be a
                    // per-player-per-day cost on every career today for a value that is discarded — and,
                    // worse to read, would suggest the recovery countdown depends on it.
                    InjuryRiskContribution risk = InjuryOccurrenceEnabled
                        ? TrainingStep.ComputeInjuryRisk(in training[i], in record.Attributes)
                        : InjuryRiskContribution.None;

                    MedicalStep.AdvanceMedicalDay(
                        ref injury[i],
                        ids[i],
                        in record.Attributes,
                        in risk,
                        MatchLoad.None,
                        in medical,
                        worldDay,
                        worldSeed,
                        InjuryOccurrenceEnabled);
                }
            }
        }

        /// <summary>
        /// The FR-TR-025 / FR-MD-025 roster-membership handoff, applied at #30's season boundary: a
        /// player on the roster with no state gains one (<see cref="TrainingState.Create"/> on
        /// <see cref="TrainingFocus.Balanced"/> + <see cref="InjuryState.Create"/>), and a state whose
        /// player has left the roster is removed. Returns the number of entries inserted plus removed,
        /// so a caller can assert a boundary was a no-op.
        /// <para>
        /// <b>Keyed on the roster, not on a #28 event.</b> Both FRs describe the handoff as reacting to
        /// #28's <c>RegenResult</c> / <c>RetirementResult</c> at the boundary — but #28's roster churn
        /// is unwired (roadmap D1) and those result types do not exist, so subscribing to them would be
        /// a phantom seam. Reconciling against the roster #30 already holds is the same contract stated
        /// over the state that exists: it inserts exactly the regens and removes exactly the retirees the
        /// moment #28 T2 starts producing them, and it is exercisable today by a provider whose roster
        /// changes. Keyed by <c>PlayerId</c> either way, as both FRs require.
        /// </para>
        /// <para>
        /// <b>Validate-all, then write.</b> Every club's roster is resolved and its new arrays built
        /// before any of them is installed, so a provider that fails on the fifth club leaves the career
        /// wholly unsynced rather than half-synced — <see cref="SeasonLoop.RollToNextSeason"/>'s own
        /// discipline, applied here because this runs inside that boundary.
        /// </para>
        /// <para>
        /// A club present in the state but unresolvable by <paramref name="squads"/> fails loud rather
        /// than being dropped: a club whose roster has genuinely gone is a promotion/relegation or
        /// league-restructuring event (#43), and silently deleting a season of its players' conditioning
        /// and injury history on a provider hiccup is not a recoverable mistake.
        /// </para>
        /// </summary>
        /// <param name="squads">The rosters to reconcile against.</param>
        /// <exception cref="ArgumentNullException"><paramref name="squads"/> is null.</exception>
        /// <exception cref="ArgumentException">A held club cannot be resolved to a roster.</exception>
        public int SyncToRoster(ISquadProvider squads)
        {
            RosterSyncPlan plan = PrepareRosterSync(squads);
            return CommitRosterSync(in plan);
        }

        /// <summary>
        /// The computing half of <see cref="SyncToRoster"/>: resolves every club's roster and builds
        /// its replacement arrays, writing nothing. Throws exactly where <see cref="SyncToRoster"/>
        /// throws.
        /// <para>
        /// It is split out for <see cref="SeasonLoop.RollToNextSeason"/>, whose whole shape is
        /// "compute and validate everything, then write". The roll has one write that can fail
        /// (<c>BeginNextSeason</c>), so a sync that both computes and installs cannot be placed
        /// anywhere in that method without some failure leaving a half-rolled career: before the
        /// commits, a refused <c>BeginNextSeason</c> leaves a career reconciled against a season that
        /// never began; after them, an unresolvable club leaves a rolled season with a stale career.
        /// Staging here and installing after the last throwing write removes both.
        /// </para>
        /// </summary>
        /// <param name="squads">The rosters to reconcile against.</param>
        /// <exception cref="ArgumentNullException"><paramref name="squads"/> is null.</exception>
        /// <exception cref="ArgumentException">A held club cannot be resolved to a roster.</exception>
        internal RosterSyncPlan PrepareRosterSync(ISquadProvider squads)
        {
            if (squads == null)
            {
                throw new ArgumentNullException(nameof(squads));
            }

            int clubs = _clubIds.Count;
            var nextIds = new int[clubs][];
            var nextTraining = new TrainingState[clubs][];
            var nextInjury = new InjuryState[clubs][];
            int churn = 0;

            for (int c = 0; c < clubs; c++)
            {
                Squad squad = ResolveSquad(squads, _clubIds[c]);
                int[] rosterIds = AscendingPlayerIds(squad);

                int[] heldIds = _playerIds[c];
                TrainingState[] heldTraining = _training[c];
                InjuryState[] heldInjury = _injury[c];

                var training = new TrainingState[rosterIds.Length];
                var injury = new InjuryState[rosterIds.Length];
                int carried = 0;

                for (int i = 0; i < rosterIds.Length; i++)
                {
                    int held = IndexOfPlayer(heldIds, rosterIds[i]);
                    if (held >= 0)
                    {
                        training[i] = heldTraining[held];
                        injury[i] = heldInjury[held];
                        carried++;
                    }
                    else
                    {
                        // A fresh id: Create, never default — the day-0 trap both specs name.
                        training[i] = TrainingState.Create(TrainingFocus.Balanced);
                        injury[i] = InjuryState.Create();
                        churn++;
                    }
                }

                // Whatever was held and not carried forward is a departure. Counted rather than
                // enumerated: the states are simply not copied into the new arrays.
                churn += heldIds.Length - carried;

                nextIds[c] = rosterIds;
                nextTraining[c] = training;
                nextInjury[c] = injury;
            }

            return new RosterSyncPlan(RosterGeneration, nextIds, nextTraining, nextInjury, churn);
        }

        /// <summary>
        /// The installing half of <see cref="SyncToRoster"/>: swaps in the arrays
        /// <see cref="PrepareRosterSync"/> built and returns the churn count. <b>Cannot fail</b> on a
        /// plan this career prepared and has not since re-synced, which is what makes it safe to run
        /// after <see cref="SeasonLoop.RollToNextSeason"/>'s last throwing write.
        /// </summary>
        /// <param name="plan">A plan from <see cref="PrepareRosterSync"/> on this same career.</param>
        /// <exception cref="ArgumentException">
        /// The plan was prepared against a different career, or against this one before a later sync —
        /// installing it would resurrect a stale roster over a newer one. A <c>default</c> plan is
        /// refused the same way.
        /// </exception>
        internal int CommitRosterSync(in RosterSyncPlan plan)
        {
            if (plan.PlayerIds == null
                || plan.PlayerIds.Length != _clubIds.Count
                || plan.Generation != RosterGeneration)
            {
                throw new ArgumentException(
                    "This roster-sync plan was not prepared from this career's current state "
                    + $"(plan generation {plan.Generation}, career generation {RosterGeneration}); "
                    + "installing it would overwrite the current roster with a stale one.",
                    nameof(plan));
            }

            for (int c = 0; c < _clubIds.Count; c++)
            {
                _playerIds[c] = plan.PlayerIds[c];
                _training[c] = plan.Training[c];
                _injury[c] = plan.Injury[c];
            }

            // Bumped unconditionally, not only when churn > 0: the arrays are replaced either way, so
            // every previously handed-out TrainingSchedule is now detached whether or not the roster
            // actually moved. A generation that only moved on churn would mark the no-churn case valid
            // while the handle it validates writes into a discarded array.
            RosterGeneration++;

            return plan.Churn;
        }

        /// <summary>
        /// A staged roster reconciliation: the replacement arrays <see cref="PrepareRosterSync"/> built,
        /// plus the <see cref="RosterGeneration"/> they were built from so
        /// <see cref="CommitRosterSync"/> can refuse a stale one.
        /// </summary>
        internal readonly struct RosterSyncPlan
        {
            /// <summary>The <see cref="RosterGeneration"/> this plan was prepared at.</summary>
            internal readonly int Generation;

            /// <summary>Per club, the reconciled ascending player ids. Null for a <c>default</c> plan.</summary>
            internal readonly int[][] PlayerIds;

            /// <summary>Per club, the reconciled training states.</summary>
            internal readonly TrainingState[][] Training;

            /// <summary>Per club, the reconciled medical states.</summary>
            internal readonly InjuryState[][] Injury;

            /// <summary>Entries inserted plus removed by this plan.</summary>
            internal readonly int Churn;

            /// <summary>Stages one reconciliation.</summary>
            /// <param name="generation">The career's roster generation at preparation time.</param>
            /// <param name="playerIds">Per-club reconciled player ids.</param>
            /// <param name="training">Per-club reconciled training states.</param>
            /// <param name="injury">Per-club reconciled medical states.</param>
            /// <param name="churn">Entries inserted plus removed.</param>
            internal RosterSyncPlan(
                int generation,
                int[][] playerIds,
                TrainingState[][] training,
                InjuryState[][] injury,
                int churn)
            {
                Generation = generation;
                PlayerIds = playerIds;
                Training = training;
                Injury = injury;
                Churn = churn;
            }
        }

        /// <summary>
        /// The FR-MD-023 availability read, resolved through this career's state: <c>true</c> iff the
        /// player carries no active injury. #41 owns the predicate; #30 owns what to do about it, which
        /// is <see cref="SelectAvailable"/>.
        /// </summary>
        /// <param name="clubId">The club.</param>
        /// <param name="playerId">The player.</param>
        /// <exception cref="ArgumentException">The club or the player is not carried (F2 — a missing state is a roster-lifecycle bug, never a default).</exception>
        public bool IsAvailable(int clubId, int playerId)
        {
            RequireEntry(clubId, playerId, out int club, out int index);
            return MedicalStep.IsAvailable(in _injury[club][index]);
        }

        /// <summary>
        /// The squad-selection filter #30 applies between resolving a roster and configuring a match
        /// (the ERR-030-009 resolve → filter → configure shape, which #44's suspension filter will
        /// share): returns the squad #30 will actually field. That is <paramref name="squad"/> with the
        /// injured removed — <b>except</b> for whoever the depleted-squad rule below has to press back
        /// in, which is nobody unless the injury list would otherwise stop the club playing.
        /// <para>
        /// <b>Returns the same instance when nothing is filtered</b>, so a career with no injuries — every
        /// career today, with the occurrence dial off — resolves through a reference-identical squad and
        /// the match is byte-identical to the unfiltered path.
        /// </para>
        /// <para>
        /// <b>The depleted-squad rule.</b> A club is never stopped from playing by its injury list. If
        /// what remains cannot field the Stage-0 formation — too few players, or every goalkeeper out,
        /// and selection refuses a position-incomplete squad outright (KD-L3) — the least-injured are
        /// pressed back into service one at a time (ascending <c>RecoveryRemaining</c>, ties on the
        /// earliest roster position) until it can. In the limit that is the whole squad, which is
        /// exactly the unfiltered behaviour, so the filter can never leave a club worse off than having
        /// no filter at all.
        /// </para>
        /// <para>
        /// The viability question is asked of the engine's own selector
        /// (<see cref="SquadRating.CanFieldStartingEleven"/>) rather than answered by a player-count
        /// rule here — a count cannot see that a squad has eighteen fit outfielders and no goalkeeper,
        /// and a second selection rule in this assembly is the parallel-surface trap
        /// <see cref="SquadRating"/> exists to avoid.
        /// </para>
        /// <para>
        /// It is stated as a policy here, in #30, because FR-MD-023 puts selection on this side of the
        /// seam — #41 answers only "is he fit". Playing a half-fit player carries no penalty yet; that
        /// consequence belongs with the balance pass that arms the dial.
        /// </para>
        /// </summary>
        /// <param name="squad">The resolved roster.</param>
        /// <exception cref="ArgumentNullException"><paramref name="squad"/> is null.</exception>
        /// <exception cref="ArgumentException">The squad's club or one of its players is not carried by this career.</exception>
        /// <exception cref="InvalidOperationException">
        /// Even the whole squad cannot field the formation. That is a roster problem — too few players,
        /// or none of a required position — and no filter can repair it; the same roster would be
        /// refused identically with no injuries at all.
        /// </exception>
        public Squad SelectAvailable(Squad squad)
        {
            if (squad == null)
            {
                throw new ArgumentNullException(nameof(squad));
            }

            int club = RequireClub(squad.ClubId);
            int[] ids = _playerIds[club];
            InjuryState[] injury = _injury[club];

            int total = squad.Count;
            var stateIndex = new int[total];
            var available = new bool[total];
            int availableCount = 0;
            for (int i = 0; i < total; i++)
            {
                stateIndex[i] = RequireIndexOfPlayer(ids, squad.ClubId, squad.GetPlayer(i).PlayerId);
                available[i] = MedicalStep.IsAvailable(in injury[stateIndex[i]]);
                if (available[i])
                {
                    availableCount++;
                }
            }

            if (availableCount == total)
            {
                // Nothing to filter — hand back the same instance so the fit-squad path stays
                // reference-identical, which is every club until the occurrence dial is armed.
                return squad;
            }

            Squad filtered = Compose(squad, available, availableCount);

            // Press the least-injured back in until the club can actually play. Bounded by the roster:
            // each pass marks one more player selectable, and the loop ends at the latest when everyone
            // is — at which point the verdict is the roster's own, not the injury list's.
            while (filtered == null || !SquadRating.CanFieldStartingEleven(filtered))
            {
                if (availableCount == total)
                {
                    throw new InvalidOperationException(
                        $"Club {squad.ClubId} cannot field the Stage-0 formation even with all "
                        + $"{total} of its players selected. That is a roster problem — too few "
                        + "players, or none of a position the formation requires — and the "
                        + "availability filter cannot repair it.");
                }

                MarkLeastInjured(injury, stateIndex, available);
                availableCount++;
                filtered = Compose(squad, available, availableCount);
            }

            return availableCount == total ? squad : filtered;
        }

        /// <summary>
        /// The squad of the currently-selectable players, or <c>null</c> when none are — which
        /// <see cref="Squad"/> itself refuses to represent, and which the back-fill loop then resolves
        /// by selecting someone.
        /// </summary>
        private static Squad Compose(Squad squad, bool[] available, int availableCount)
        {
            if (availableCount == 0)
            {
                return null;
            }

            var selected = new PlayerRecord[availableCount];
            int w = 0;
            for (int i = 0; i < available.Length; i++)
            {
                if (available[i])
                {
                    selected[w++] = squad.GetPlayer(i);
                }
            }

            return new Squad(squad.ClubId, selected);
        }

        /// <summary>
        /// #29's match-boot fatigue projection (§3.3 / KD-1) for one squad, as the per-local-index array
        /// <c>MatchEngine.ConfigureSquads</c> takes: index <c>i</c> is
        /// <c>TrainingStep.ProjectMatchEntryFatigue</c> for <c>squad.GetPlayer(i)</c>, on the project's
        /// standing convention that <b>0 = fully rested, 1 = fully fatigued</b>.
        /// <para>
        /// Recomputed from the serialized accumulator every time and never stored, so it is identical
        /// either side of a save → restore; match-tick fatigue never writes back into it (FR-TR-012).
        /// </para>
        /// <para>
        /// Indexed by the <b>local roster index of the squad passed in</b> — which after
        /// <see cref="SelectAvailable"/> is a different squad with different indices, so this must be
        /// called on the squad that is actually configured, not the one it was filtered from.
        /// </para>
        /// </summary>
        /// <param name="squad">The squad about to be configured.</param>
        /// <exception cref="ArgumentNullException"><paramref name="squad"/> is null.</exception>
        /// <exception cref="ArgumentException">The squad's club or one of its players is not carried by this career.</exception>
        public float[] MatchEntryFatigue(Squad squad)
        {
            if (squad == null)
            {
                throw new ArgumentNullException(nameof(squad));
            }

            int club = RequireClub(squad.ClubId);
            int[] ids = _playerIds[club];
            TrainingState[] training = _training[club];

            var fatigue = new float[squad.Count];
            for (int i = 0; i < fatigue.Length; i++)
            {
                int index = RequireIndexOfPlayer(ids, squad.ClubId, squad.GetPlayer(i).PlayerId);
                fatigue[i] = TrainingStep.ProjectMatchEntryFatigue(in training[index]);
            }

            return fatigue;
        }

        /// <summary>
        /// The club-scoped FR-TR-023 focus command surface: a <see cref="TrainingSchedule"/> bound to
        /// this club's own id and state arrays, so a caller cannot pair one club's ids with another's
        /// states (the bind-once discipline the schedule exists for).
        /// <para>
        /// <b>The handle is invalidated by <see cref="SyncToRoster"/>, i.e. by every season boundary.</b>
        /// It binds the live arrays by reference and the sync replaces them, so a schedule cached across
        /// a roll writes into a discarded array — <c>TrySetFocus</c> returns <c>true</c> and the change
        /// is lost with nothing to notice it. Acquire it per use, or capture
        /// <see cref="RosterGeneration"/> alongside it and re-acquire when that value moves.
        /// </para>
        /// </summary>
        /// <param name="clubId">The club.</param>
        /// <exception cref="ArgumentException">The club is not carried.</exception>
        public TrainingSchedule ScheduleFor(int clubId)
        {
            int club = RequireClub(clubId);
            return new TrainingSchedule(_playerIds[club], _training[club]);
        }

        /// <summary>The FR-TR-022 observer read for one player — a value copy, never a handle.</summary>
        /// <param name="clubId">The club.</param>
        /// <param name="playerId">The player.</param>
        /// <exception cref="ArgumentException">The club or the player is not carried.</exception>
        public TrainingViewModel TrainingView(int clubId, int playerId)
        {
            RequireEntry(clubId, playerId, out int club, out int index);
            return TrainingViewModel.Create(in _training[club][index]);
        }

        /// <summary>The FR-MD-024 observer read for one player — a value copy, never a handle.</summary>
        /// <param name="clubId">The club.</param>
        /// <param name="playerId">The player.</param>
        /// <exception cref="ArgumentException">The club or the player is not carried.</exception>
        public MedicalViewModel MedicalView(int clubId, int playerId)
        {
            RequireEntry(clubId, playerId, out int club, out int index);
            return MedicalViewModel.Create(in _injury[club][index]);
        }

        /// <summary>
        /// Test-and-tooling seam: installs a medical state directly, bypassing the day step.
        /// <para>
        /// Production has exactly one writer of <see cref="InjuryState"/> —
        /// <c>MedicalStep.AdvanceMedicalDay</c> (FR-MD-003) — and this is not it. It exists because
        /// everything downstream of an injury (the availability filter, the back-fill, the view) has to
        /// be provable while <see cref="InjuryOccurrenceEnabled"/> is off, and the alternative is
        /// asserting on a subsystem the balance pass has not yet armed. <c>internal</c> so no production
        /// call site outside this assembly can become a second writer.
        /// </para>
        /// </summary>
        /// <param name="clubId">The club.</param>
        /// <param name="playerId">The player.</param>
        /// <param name="state">The state to install.</param>
        /// <exception cref="ArgumentException">The club or the player is not carried.</exception>
        internal void SetMedicalState(int clubId, int playerId, in InjuryState state)
        {
            RequireEntry(clubId, playerId, out int club, out int index);
            _injury[club][index] = state;
        }

        /// <summary>
        /// The back-fill step of <see cref="SelectAvailable"/>: marks the single least-injured
        /// not-yet-selectable player selectable. Ties on <c>RecoveryRemaining</c> break on the earliest
        /// roster position, and the roster is walked in the squad's own order — so the choice is
        /// deterministic and independent of the order the injuries were drawn in.
        /// <para>
        /// The caller has already established that someone is left to press in, which is what its
        /// <c>availableCount == total</c> guard is for.
        /// </para>
        /// </summary>
        private static void MarkLeastInjured(InjuryState[] injury, int[] stateIndex, bool[] available)
        {
            int best = -1;
            int bestRecovery = int.MaxValue;

            for (int i = 0; i < available.Length; i++)
            {
                if (available[i])
                {
                    continue;
                }

                int recovery = injury[stateIndex[i]].RecoveryRemaining;
                if (recovery < bestRecovery)
                {
                    bestRecovery = recovery;
                    best = i;
                }
            }

            available[best] = true;
        }

        /// <summary>The club's player ids, ascending — the canonical key order both codecs require.</summary>
        private static int[] AscendingPlayerIds(Squad squad)
        {
            var ids = new int[squad.Count];
            for (int i = 0; i < ids.Length; i++)
            {
                ids[i] = squad.GetPlayer(i).PlayerId;
            }

            Array.Sort(ids);

            for (int i = 1; i < ids.Length; i++)
            {
                if (ids[i] == ids[i - 1])
                {
                    throw new ArgumentException(
                        $"Club {squad.ClubId} lists player {ids[i]} twice; a per-player state set is "
                        + "keyed by PlayerId and has no defined winner for a duplicate.",
                        nameof(squad));
                }
            }

            return ids;
        }

        /// <summary>Resolves a club to its roster, failing loud on an unresolvable id (the #30 F6 contract).</summary>
        private static Squad ResolveSquad(ISquadProvider squads, int clubId)
        {
            Squad squad = squads.ResolveByClubId(clubId);
            if (squad == null)
            {
                throw new ArgumentException(
                    $"ISquadProvider cannot resolve club {clubId}; per-player career state cannot be "
                    + "maintained without its roster.",
                    nameof(squads));
            }

            return squad;
        }

        /// <summary>The squad-local index of a held player id, failing loud when the roster no longer carries him (F7 / F2 — call <see cref="SyncToRoster"/> at the boundary).</summary>
        private static int RequireLocalIndex(Squad squad, int clubId, int playerId)
        {
            for (int i = 0; i < squad.Count; i++)
            {
                if (squad.GetPlayer(i).PlayerId == playerId)
                {
                    return i;
                }
            }

            throw new ArgumentException(
                $"Club {clubId} carries career state for player {playerId}, who is no longer on its "
                + "roster. Roster membership is reconciled at the season boundary (FR-TR-025 / "
                + "FR-MD-025) — call SyncToRoster after a roster change.",
                nameof(squad));
        }

        /// <summary>Binary search over a club's ascending player ids; -1 when absent.</summary>
        private static int IndexOfPlayer(int[] ascendingIds, int playerId)
        {
            int lo = 0;
            int hi = ascendingIds.Length - 1;
            while (lo <= hi)
            {
                int mid = lo + ((hi - lo) >> 1);
                int value = ascendingIds[mid];
                if (value == playerId)
                {
                    return mid;
                }

                if (value < playerId)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            return -1;
        }

        /// <summary><see cref="IndexOfPlayer"/>, failing loud on absence (F2/F7 — a missing state is a lifecycle bug, never a default).</summary>
        private static int RequireIndexOfPlayer(int[] ascendingIds, int clubId, int playerId)
        {
            int index = IndexOfPlayer(ascendingIds, playerId);
            if (index < 0)
            {
                throw new ArgumentException(
                    $"Club {clubId} carries no career state for player {playerId}; a missing state is a "
                    + "roster-lifecycle bug (FR-TR-025 / FR-MD-025), not something to default.");
            }

            return index;
        }

        /// <summary>The block index of a club, failing loud on absence.</summary>
        private int RequireClub(int clubId)
        {
            for (int c = 0; c < _clubIds.Count; c++)
            {
                if (_clubIds[c] == clubId)
                {
                    return c;
                }
            }


            throw new ArgumentException(
                $"This career carries no state for club {clubId}.", nameof(clubId));
        }

        /// <summary>Resolves a (club, player) pair to its block and entry index, failing loud on either absence.</summary>
        private void RequireEntry(int clubId, int playerId, out int club, out int index)
        {
            club = RequireClub(clubId);
            index = RequireIndexOfPlayer(_playerIds[club], clubId, playerId);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-08-06 | —      | Initial implementation (#29/#41 T2): the #30-side owner of both    |
// |         |            |        | per-club state sets — the slot-2 / slot-4 day steps in their KD-6  |
// |         |            |        | order, the FR-TR-025 / FR-MD-025 roster reconciliation, the        |
// |         |            |        | FR-MD-023 availability filter with its depleted-squad back-fill,   |
// |         |            |        | the §3.3 match-entry fatigue projection, and the cross-block       |
// |         |            |        | coherence gate no codec can own.                                   |
// | 1.1     | 2026-08-06 | —      | AR pass 1 (1H + 3M + 2L). **H:** FromBlocks now requires strictly  |
// |         |            |        | ascending player ids. Every lookup here is a binary search, and    |
// |         |            |        | the entry point is public over blocks whose constructor imposes    |
// |         |            |        | no order — an unordered block made IndexOfPlayer miss a carried    |
// |         |            |        | player, and SyncToRoster then read the miss as "new" and           |
// |         |            |        | overwrote his season of state with Create(). Silent and total.     |
// |         |            |        | **M:** TrainingBlocks/MedicalBlocks internal — they hand out the   |
// |         |            |        | live arrays, so a public accessor made every holder of             |
// |         |            |        | SeasonLoop.Career a second writer of #29/#41 state (FR-TR-004 /    |
// |         |            |        | FR-TR-023); external callers save through the new                  |
// |         |            |        | SeasonSaveManager.Save(SeasonLoop, …). **M:** SyncToRoster splits  |
// |         |            |        | into PrepareRosterSync (pure, throws) + CommitRosterSync (cannot   |
// |         |            |        | fail), so RollToNextSeason can stage before its one throwing       |
// |         |            |        | commit and install after — otherwise SOME failure always left a    |
// |         |            |        | half-rolled career. **M:** + RosterGeneration, bumped on every     |
// |         |            |        | sync, because ScheduleFor's handle binds arrays the sync replaces  |
// |         |            |        | and a cached schedule silently discards focus changes. **L:** the  |
// |         |            |        | risk read is skipped with the dial off (nothing reads it); the     |
// |         |            |        | SelectAvailable summary no longer contradicts its own back-fill;   |
// |         |            |        | + CarriesClub for SeasonLoop's coverage gate.                      |
#endregion
