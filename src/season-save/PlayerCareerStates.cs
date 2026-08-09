// File:     src/season-save/PlayerCareerStates.cs
// Created:  2026-08-06
// Modified: 2026-08-08 (#28 T1/T2a + AR pass 1 — v1.15)
// Author:   —
// Spec:     Training System #29 §3.1/§3.3/§3.5, §4.3 (seam contracts), FR-TR-004/016/022/023/025;
//           Injuries & Medical #41 §3.1/§3.5, §4.3, FR-MD-003/009/010/022/023/025/027;
//           Season & Competition Loop #30 §3.3 (KD-2 slot order), §3.5 (the boundary), FR-SN-034;
//           path-to-playable D2/D3 (T2); Code Standards #20
// Purpose:  The #30-side owner of the per-club #29 training, #41 medical and #30 appearance state.
//           Holds the three sets keyed by (ClubId, PlayerId), drives the two day steps at #30's
//           slot-2 / slot-4, supplies the FR-MD-010 MatchLoad from the appearance record, answers the
//           availability question squad selection reads, projects match-entry fatigue, and keeps
//           roster membership in lockstep (FR-TR-025 / FR-MD-025).

using System;
using System.Collections.Generic;

using TacticalDirector.InjuriesMedical;
using TacticalDirector.MatchEngine;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.PlayerProgression;
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
    /// <b>#41's occurrence draw is ARMED</b> (<see cref="InjuryOccurrenceEnabled"/>, FR-MD-027 as
    /// revised at the balance pass): construction declares the dial's position explicitly, production
    /// passes <c>true</c>, and the OFF position remains supported for isolation — see the property's
    /// own documentation.
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
        //
        // Three parallel state sets is the ceiling for this shape (AR pass 2). Every writer must
        // already touch all of them in lockstep (SyncToRoster stages five arrays; FromBlocks
        // coherence-checks three block sets pairwise), and each addition multiplies the
        // wrong-pairing surface the T0 AR's SetFocus finding came from. A FOURTH per-player set
        // (#44 suspensions is the likely candidate) collapses this into one per-player career
        // struct per club — one array, one pairing, the codecs reading fields off it — rather than
        // extending the parallel walk again.
        private readonly List<int> _clubIds = new List<int>();
        private readonly List<int[]> _playerIds = new List<int[]>();
        private readonly List<TrainingState[]> _training = new List<TrainingState[]>();
        private readonly List<InjuryState[]> _injury = new List<InjuryState[]>();
        private readonly List<AppearanceState[]> _appearance = new List<AppearanceState[]>();

        private PlayerCareerStates(bool injuryOccurrenceEnabled)
        {
            InjuryOccurrenceEnabled = injuryOccurrenceEnabled;
        }

        /// <summary>
        /// Increments every time <see cref="CommitRosterSync"/> replaces the per-club arrays — i.e. once
        /// per season boundary, whether or not the roster actually moved.
        /// <para>
        /// <b>It is load-bearing for <see cref="CommitRosterSync"/></b>, which refuses a plan prepared at
        /// a different generation: installing one would overwrite the current roster with a stale one.
        /// That is the reason it must exist, and the reason it moves on a no-churn sync too — the arrays
        /// are replaced either way.
        /// </para>
        /// <para>
        /// It is <b>not</b> a staleness check any caller is asked to perform. The handle that could go
        /// stale (<see cref="ScheduleFor"/>) is <c>internal</c> and the public focus command
        /// (<see cref="TrySetFocus"/>) resolves fresh every call, so there is nothing outside this class
        /// to invalidate. Read it, if you like, as a cheap "did the squad list change" signal for a
        /// screen — but nothing depends on a caller remembering to.
        /// </para>
        /// </summary>
        public int RosterGeneration { get; private set; }

        /// <summary>
        /// The #41 KD-8 dial (FR-MD-027). <b>ARMED at the balance pass — a production career is
        /// constructed with this ON</b>; the measured rates at the retuned constants sit in the
        /// football band (league ~780 injuries/season over the 20-club bootstrap, starters ~2.1,
        /// reserves ~1.1, squad unavailability ~9%, locked league-wide by the season-scale
        /// instrument). The pre-pass absurdities the dial was shipped OFF for — 23%/day for a fresh
        /// regen, 43%/day half-fatigued, exactly-0-forever on the default focus — are gone
        /// (ERR-041-011; the characterization test carries the AFTER numbers).
        /// <para>
        /// <b>The argument is REQUIRED, not defaulted, deliberately:</b> a default — in either
        /// position — changes behaviour at every omitting call site with no diff at those sites the
        /// day it flips. Construction declares the position: production passes <c>true</c>; a test
        /// isolating the recovery-only path passes <c>false</c>, which stays supported and locked
        /// both ways (a dial-off season injures nobody; a dial-on season injures at the band).
        /// </para>
        /// <para>
        /// With the dial off, <c>AdvanceMedicalDay</c> reduces to the recovery countdown with no draw
        /// at all — the FR-MD-027 identity.
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
            ISquadProvider squads, int[] clubIds, bool injuryOccurrenceEnabled)
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
                // default(AppearanceState) is the valid fresh state here — no day-0 trap, see the type.
                career._appearance.Add(new AppearanceState[ids.Length]);
            }

            RequireGloballyUniquePlayerIds(career._clubIds, career._playerIds, nameof(squads));

            return career;
        }

        /// <summary>
        /// The #41 occurrence-draw key precondition, enforced at the one layer that spans clubs
        /// (ERR-041-019, AR pass 3): the draw key is <c>(worldSeed, playerId, actionOrdinal = worldDay × RADIX + purpose)</c>
        /// with NO club term (#41 §3.1.1), but #27 promises <c>PlayerId</c> uniqueness only WITHIN a
        /// club — this very class is keyed <c>(ClubId, PlayerId)</c> on that premise. Two clubs
        /// carrying the same id would draw bit-identical injury luck on every world day forever,
        /// with matching severities whenever their risks are close: silent, total, and
        /// indistinguishable from chance. Safe today only because <c>RosterGenerator</c> happens to
        /// allocate <c>clubId × CLUB_SQUAD_SIZE + local</c>; the moment another allocator exists
        /// (#42 youth intake, #31 transfers) that accident ends, so the precondition fails loud here
        /// rather than surfacing as two players who are always injured together.
        /// </summary>
        internal static void RequireGloballyUniquePlayerIds(
            IReadOnlyList<int> clubIds, IReadOnlyList<int[]> playerIds, string paramName)
        {
            var owner = new Dictionary<int, int>();
            for (int c = 0; c < clubIds.Count; c++)
            {
                int[] ids = playerIds[c];
                for (int i = 0; i < ids.Length; i++)
                {
                    if (owner.TryGetValue(ids[i], out int otherClub))
                    {
                        throw new ArgumentException(
                            $"Player id {ids[i]} is carried by BOTH club {otherClub} and club "
                            + $"{clubIds[c]}. #41's occurrence draw is keyed on (worldSeed, playerId, "
                            + "actionOrdinal) with no club term (#41 §3.1.1), so two players sharing an id "
                            + "would draw identical injury luck forever. Player ids must be globally "
                            + "unique across the career (ERR-041-019).",
                            paramName);
                    }

                    owner.Add(ids[i], clubIds[c]);
                }
            }
        }

        /// <summary>
        /// Rebuilds a career's state from the three decoded save blocks — the counterpart of
        /// <see cref="TrainingBlocks"/> / <see cref="MedicalBlocks"/>, and what
        /// <see cref="SeasonSaveContents"/> is fed into after a load.
        /// <para>
        /// <b>This is the one place the three blocks are checked against each other</b>, and the only
        /// layer that can be: each codec sees one block and is forbidden to read the other (#29 §4.4 /
        /// #41 §4.4 blob independence), so "the training block and the medical block describe the same
        /// squads" is a coherence rule with no other owner — the same argument that puts the KD-4 cursor
        /// invariant on <see cref="SeasonSaveManager.Load"/>. A mismatch means one block was hand-edited,
        /// truncated, or paired with another save's sibling, and every later step — the day advance, the
        /// availability read, the roster sync — would then be operating on two different squads.
        /// </para>
        /// <para>
        /// <b>The state arrays are COPIED, so the blocks passed in are not a back door into the career.</b>
        /// <c>ClubTrainingStates.States</c> is a public field over a borrowed array, and the usual caller
        /// hands in the very arrays <see cref="SeasonSaveManager.Load"/> returns inside
        /// <see cref="SeasonSaveContents"/> — sharing them would make every holder of those contents a
        /// second writer of #29/#41 state, which is precisely what <see cref="TrainingBlocks"/> being
        /// <c>internal</c> prevents on the save side. The player-id arrays are copied for the same
        /// reason (AR pass 3): they are the binary-search keys every lookup runs on, so an aliased
        /// write to <c>contents.TrainingClubs[c].PlayerIds</c> would break the strictly-ascending
        /// precondition this method just enforced.
        /// </para>
        /// </summary>
        /// <param name="training">The decoded #29 blocks.</param>
        /// <param name="medical">The decoded #41 blocks.</param>
        /// <param name="appearance">The decoded #30 appearance blocks — required, never null-meaning-empty (the T1 AR's silent-empty-career rule).</param>
        /// <param name="injuryOccurrenceEnabled">The #41 KD-8 dial; see <see cref="InjuryOccurrenceEnabled"/>.</param>
        /// <exception cref="ArgumentNullException">Any array is null.</exception>
        /// <exception cref="ArgumentException">The blocks disagree on the club set or on any club's player set.</exception>
        public static PlayerCareerStates FromBlocks(
            ClubTrainingStates[] training,
            ClubInjuryStates[] medical,
            ClubAppearanceStates[] appearance,
            bool injuryOccurrenceEnabled)
        {
            if (training == null)
            {
                throw new ArgumentNullException(nameof(training));
            }

            if (medical == null)
            {
                throw new ArgumentNullException(nameof(medical));
            }

            if (appearance == null)
            {
                throw new ArgumentNullException(nameof(appearance));
            }

            if (training.Length != medical.Length || training.Length != appearance.Length)
            {
                throw new ArgumentException(
                    $"The training block carries {training.Length} clubs, the medical block "
                    + $"{medical.Length} and the appearance block {appearance.Length}; the three "
                    + "describe the same career and must agree.",
                    nameof(medical));
            }

            var career = new PlayerCareerStates(injuryOccurrenceEnabled);

            for (int c = 0; c < training.Length; c++)
            {
                ClubTrainingStates t = training[c];
                ClubInjuryStates m = medical[c];
                ClubAppearanceStates a = appearance[c];

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

                if (a.PlayerIds == null || a.States == null)
                {
                    throw new ArgumentException(
                        $"Block {c} of the appearance set is a default value, not a constructed one.",
                        nameof(appearance));
                }

                if (t.ClubId != m.ClubId || t.ClubId != a.ClubId)
                {
                    throw new ArgumentException(
                        $"Block {c}: the training block is club {t.ClubId}, the medical block club "
                        + $"{m.ClubId}, the appearance block club {a.ClubId}. All three codecs "
                        + "canonicalize to ascending ClubId, so this is a mismatched set, not a "
                        + "re-ordering.",
                        nameof(medical));
                }

                if (t.Count != m.Count || t.Count != a.Count)
                {
                    throw new ArgumentException(
                        $"Club {t.ClubId}: the training block carries {t.Count} players, the medical "
                        + $"block {m.Count}, the appearance block {a.Count}.",
                        t.Count != m.Count ? nameof(medical) : nameof(appearance));
                }

                for (int i = 0; i < t.Count; i++)
                {
                    if (t.PlayerIds[i] != m.PlayerIds[i] || t.PlayerIds[i] != a.PlayerIds[i])
                    {
                        throw new ArgumentException(
                            $"Club {t.ClubId}, entry {i}: the training block holds player "
                            + $"{t.PlayerIds[i]}, the medical block player {m.PlayerIds[i]}, the "
                            + $"appearance block player {a.PlayerIds[i]}.",
                            t.PlayerIds[i] != m.PlayerIds[i] ? nameof(medical) : nameof(appearance));
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

                // The id array is copied too (AR pass 3): "never mutated HERE" was true and beside
                // the point — ClubTrainingStates.PlayerIds is a public field over a mutable array,
                // and an aliased caller writing contents.TrainingClubs[c].PlayerIds[i] would break
                // the strictly-ascending precondition enforced twenty lines above, reopening the
                // pass-1 silent-overwrite High through the keys instead of the states.
                //
                // The STATE arrays are copied, and that is not a defensive habit — it is the single-
                // writer contract (FR-TR-004 / FR-TR-023 / FR-MD-003). ClubTrainingStates.States is a
                // public field holding a borrowed array, and the blocks this method is called with are
                // the ones SeasonSaveManager.Load hands straight back to its caller inside
                // SeasonSaveContents. Sharing them would mean the documented restore path —
                // FromBlocks(contents.TrainingClubs, contents.MedicalClubs) — leaves every holder of
                // `contents` writing directly into the running career's Condition, Severity and
                // idempotency cursors, bypassing both day steps. That is exactly the hole closed on the
                // save side by keeping TrainingBlocks()/MedicalBlocks() internal; copying here closes
                // the same hole on the load side, which is the only other route in.
                // Named for the STATE they hold, not the block they came from: `training` and
                // `medical` are this method's own parameters, so a local of either name is a
                // compile error (CS0136/CS0841), not a shadow.
                var trainingStates = new TrainingState[t.Count];
                Array.Copy(t.States, trainingStates, t.Count);
                var injuryStates = new InjuryState[m.Count];
                Array.Copy(m.States, injuryStates, m.Count);
                var appearanceStates = new AppearanceState[a.Count];
                Array.Copy(a.States, appearanceStates, a.Count);
                var playerIds = new int[t.Count];
                Array.Copy(t.PlayerIds, playerIds, t.Count);

                career._clubIds.Add(t.ClubId);
                career._playerIds.Add(playerIds);
                career._training.Add(trainingStates);
                career._injury.Add(injuryStates);
                career._appearance.Add(appearanceStates);
            }

            RequireGloballyUniquePlayerIds(career._clubIds, career._playerIds, nameof(training));

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

        /// <summary>The #30 appearance state as the per-club blocks <see cref="AppearanceSaveCodec.Encode"/> takes, <c>internal</c> on the same borrowing / single-writer terms as <see cref="TrainingBlocks"/>.</summary>
        internal ClubAppearanceStates[] AppearanceBlocks()
        {
            var blocks = new ClubAppearanceStates[_clubIds.Count];
            for (int c = 0; c < blocks.Length; c++)
            {
                blocks[c] = new ClubAppearanceStates(_clubIds[c], _playerIds[c], _appearance[c]);
            }

            return blocks;
        }

        /// <summary>
        /// Records that <paramref name="fieldedPlayerIds"/> were fielded by <paramref name="clubId"/>
        /// on <paramref name="worldDay"/> — the ERR-041-010(b) appearance record, written by
        /// <see cref="SeasonLoop.AdvanceAndPlayNextRound"/> after each fixture resolves, on BOTH
        /// resolution paths. <c>internal</c>: the season loop is the single production writer, the
        /// same discipline as <see cref="SetMedicalState"/>.
        /// <para>
        /// Validate-all-then-write per club: every id is resolved before any bit is set, so an id the
        /// career does not carry leaves the whole club's matchday unrecorded rather than half-recorded.
        /// Re-recording the same day is idempotent (<see cref="AppearanceWindow.Record"/> is a bit-OR).
        /// </para>
        /// </summary>
        /// <param name="clubId">The fielding club.</param>
        /// <param name="fieldedPlayerIds">The fielded players — the starting XI the selector chose.</param>
        /// <param name="worldDay">The fixture day.</param>
        /// <exception cref="ArgumentNullException"><paramref name="fieldedPlayerIds"/> is null.</exception>
        /// <exception cref="ArgumentException">The club or a fielded player is not carried by this career, or the day precedes an already-recorded appearance.</exception>
        internal void RecordAppearances(int clubId, int[] fieldedPlayerIds, uint worldDay)
        {
            int club;
            int[] indices = ValidateAppearanceWrite(clubId, fieldedPlayerIds, worldDay, out club);
            WriteAppearances(club, indices, worldDay);
        }

        /// <summary>
        /// The cursor-vs-clock rule over the LIVE state (AR pass 6 M3) — the composition-boundary
        /// twin of <c>SeasonSaveManager</c>'s block-level walk: a cursor AHEAD of the clock is
        /// silently skipped by the F6 idempotency until the clock catches up; a cursor LAGGING by
        /// two or more wedges the pairing permanently (F7 refuses the gap, and the day steps run
        /// before the clock increment, so it can never close). The appearance anchor has no gap
        /// contract and is ahead-checked only. Sentinels are exempt — a fresh state is coherent at
        /// any clock.
        /// </summary>
        /// <param name="worldTick">The world clock the career is being paired with.</param>
        /// <exception cref="InvalidOperationException">Any player's cursor is outside the coherent band.</exception>
        internal void RequireCursorsWithinClock(uint worldTick)
        {
            for (int c = 0; c < _clubIds.Count; c++)
            {
                int[] ids = _playerIds[c];
                TrainingState[] training = _training[c];
                InjuryState[] injury = _injury[c];
                AppearanceState[] appearance = _appearance[c];

                for (int i = 0; i < ids.Length; i++)
                {
                    RequireTrainingCursorWithinClock(
                        worldTick, _clubIds[c], ids[i],
                        training[i].LastAdvancedWorldDay, "Career/world pairing");
                    RequireMedicalCursorWithinClock(
                        worldTick, _clubIds[c], ids[i],
                        injury[i].LastAdvancedWorldDay, "Career/world pairing");
                    RequireAppearanceAnchorWithinClock(
                        worldTick, _clubIds[c], ids[i],
                        appearance[i].BitsAsOfWorldDay, "Career/world pairing");
                }
            }
        }

        /// <summary>
        /// The training-cursor predicate BOTH boundaries share (AR pass 9 M1) — the composition
        /// walk above and <c>SeasonSaveManager</c>'s block-level walk both delegate here, the
        /// <see cref="RequireGloballyUniquePlayerIds"/> shape: one rule, one owner, so the two
        /// gates cannot drift apart. Bidirectional: AHEAD is silently skipped by the F6
        /// idempotency until the clock catches up; a lag of two or more wedges the pairing
        /// permanently (F7 refuses the gap, and the day steps run before the clock increment,
        /// so it can never close). The sentinel is exempt — a fresh state is coherent at any clock.
        /// </summary>
        internal static void RequireTrainingCursorWithinClock(
            uint worldTick, int clubId, int playerId, uint trainingDay, string boundary)
        {
            if (trainingDay != TrainingSystemConstants.TRAINING_NOT_ADVANCED_SENTINEL
                && (trainingDay > worldTick || worldTick > trainingDay + 1))
            {
                throw new InvalidOperationException(
                    $"{boundary} is incoherent: club {clubId} player {playerId}'s "
                    + $"training cursor ({trainingDay}) is "
                    + (trainingDay > worldTick ? "ahead of" : "more than one day behind")
                    + $" the world clock ({worldTick}).");
            }
        }

        /// <summary>The medical-cursor twin of <see cref="RequireTrainingCursorWithinClock"/> — same band, same sentinel exemption, one owner for both boundaries (AR pass 9 M1).</summary>
        internal static void RequireMedicalCursorWithinClock(
            uint worldTick, int clubId, int playerId, uint medicalDay, string boundary)
        {
            if (medicalDay != InjuriesMedicalConstants.MEDICAL_NOT_ADVANCED_SENTINEL
                && (medicalDay > worldTick || worldTick > medicalDay + 1))
            {
                throw new InvalidOperationException(
                    $"{boundary} is incoherent: club {clubId} player {playerId}'s "
                    + $"medical cursor ({medicalDay}) is "
                    + (medicalDay > worldTick ? "ahead of" : "more than one day behind")
                    + $" the world clock ({worldTick}).");
            }
        }

        /// <summary>
        /// The #28 progression-cursor predicate — the FOURTH persisted per-player cursor, and checked on
        /// the same terms as its #29/#41 siblings (ERR-028-007). Ahead of the clock silently freezes
        /// growth until the clock catches up; lagging by two or more is WORSE here than for the
        /// siblings, because <c>ProgressionEngine.AdvanceDay</c> REPLAYS a gap — a mispaired file would
        /// bank N days of growth in one call from a single day's inputs, invisibly. The sentinel is
        /// exempt: a never-advanced career is coherent at any clock. One owner, all boundaries
        /// delegating (the AR pass-9 M1 shape).
        /// </summary>
        internal static void RequireProgressionCursorWithinClock(
            uint worldTick, int clubId, int playerId, uint progressionDay, string boundary)
        {
            if (progressionDay != PlayerProgressionConstants.PROGRESSION_NOT_ADVANCED_SENTINEL
                && (progressionDay > worldTick || worldTick > progressionDay + 1))
            {
                throw new InvalidOperationException(
                    $"{boundary} is incoherent: club {clubId} player {playerId}'s "
                    + $"progression cursor ({progressionDay}) is "
                    + (progressionDay > worldTick ? "ahead of" : "more than one day behind")
                    + $" the world clock ({worldTick}).");
            }
        }

        /// <summary>
        /// The appearance-anchor predicate, ahead-checked only — the anchor has no gap contract
        /// (a lazily-shifted bitmask is coherent at any lag; shifting is the read's job). One
        /// owner for both boundaries (AR pass 9 M1).
        /// </summary>
        internal static void RequireAppearanceAnchorWithinClock(
            uint worldTick, int clubId, int playerId, uint appearanceAnchor, string boundary)
        {
            if (appearanceAnchor > worldTick)
            {
                throw new InvalidOperationException(
                    $"{boundary} is incoherent: club {clubId} player {playerId}'s "
                    + $"appearance anchor ({appearanceAnchor}) is ahead of the world clock "
                    + $"({worldTick}) — with the occurrence dial armed, the slot-4 window read "
                    + "throws on every later day and the world clock can never advance again "
                    + "(the career wedges while saving cleanly).");
            }
        }

        /// <summary>
        /// The fixture-pair form (AR pass 3): BOTH clubs validated before EITHER is written — the
        /// validate-all-then-write discipline <see cref="RecordAppearances"/> keeps within a club,
        /// applied one level up. Without it, the away side's refusal landed after the home side's
        /// write, leaving a home XI carrying an appearance for a fixture that was never applied,
        /// emitted or marked played; the retry converges (the bit-OR is idempotent), but an
        /// abandoned round kept the phantom.
        /// </summary>
        /// <param name="homeClubId">The home club.</param>
        /// <param name="homeXi">The home fielded eleven.</param>
        /// <param name="awayClubId">The away club.</param>
        /// <param name="awayXi">The away fielded eleven.</param>
        /// <param name="worldDay">The fixture day.</param>
        /// <exception cref="ArgumentNullException">Either XI is null.</exception>
        /// <exception cref="ArgumentException">Either club or any fielded player is not carried, or the day precedes an already-recorded appearance — in every case NOTHING has been written for EITHER club.</exception>
        internal void RecordFixtureAppearances(
            int homeClubId, int[] homeXi, int awayClubId, int[] awayXi, uint worldDay)
        {
            int home;
            int[] homeIndices = ValidateAppearanceWrite(homeClubId, homeXi, worldDay, out home);
            int away;
            int[] awayIndices = ValidateAppearanceWrite(awayClubId, awayXi, worldDay, out away);

            WriteAppearances(home, homeIndices, worldDay);
            WriteAppearances(away, awayIndices, worldDay);
        }

        /// <summary>The validating half: resolves the club and every id, and pre-checks the day-regression refusal (AR pass 1 — <see cref="AppearanceWindow.Record"/> must not fire mid-write and leave a club half-recorded). Writes nothing.</summary>
        private int[] ValidateAppearanceWrite(
            int clubId, int[] fieldedPlayerIds, uint worldDay, out int club)
        {
            if (fieldedPlayerIds == null)
            {
                throw new ArgumentNullException(nameof(fieldedPlayerIds));
            }

            club = RequireClub(clubId);
            int[] ids = _playerIds[club];
            AppearanceState[] states = _appearance[club];

            var indices = new int[fieldedPlayerIds.Length];
            for (int i = 0; i < fieldedPlayerIds.Length; i++)
            {
                indices[i] = RequireIndexOfPlayer(ids, clubId, fieldedPlayerIds[i]);

                if (worldDay < states[indices[i]].BitsAsOfWorldDay)
                {
                    throw new ArgumentException(
                        $"Appearance for player {fieldedPlayerIds[i]} on day {worldDay} precedes the "
                        + $"state's anchor day {states[indices[i]].BitsAsOfWorldDay} — a wrong-career "
                        + "pairing or clock fault; nothing has been recorded.",
                        nameof(worldDay));
                }
            }

            return indices;
        }

        /// <summary>The writing half of the appearance record: cannot fail on validated indices.</summary>
        private void WriteAppearances(int club, int[] indices, uint worldDay)
        {
            AppearanceState[] states = _appearance[club];
            for (int i = 0; i < indices.Length; i++)
            {
                AppearanceWindow.Record(ref states[indices[i]], worldDay);
            }
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
        /// <param name="worldDay">The world day being advanced — #30's clock BEFORE its step-12 increment.</param>
        /// <param name="squads">The rosters the attributes are read from; must match the state's roster (see <see cref="SyncToRoster"/>).</param>
        /// <param name="coach">The #34 staff seam; <see cref="CoachingModifier.Identity"/> until #34 lands.</param>
        /// <exception cref="ArgumentNullException"><paramref name="squads"/> is null.</exception>
        /// <exception cref="ArgumentException">A club or a held player id cannot be resolved against <paramref name="squads"/>, or #29 refuses the day.</exception>
        internal void AdvanceTrainingDay(uint worldDay, ISquadProvider squads, CoachingModifier coach)
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
        /// Gathers #30's <b>slot-1</b> growth-input batch (#29 §3.5 step 1 / #28 FR-PG-021): each
        /// player's <c>ComputeTrainingInput</c>, keyed by player id, ready for
        /// <c>ProgressionEngine.AdvanceDay</c>. This is the composition ERR-029-006 was filed against —
        /// #29 specifies that #30 gathers the batch and hands it to #28, and until #28 exposed a batch
        /// entry point there was nothing to hand it to.
        /// <para>
        /// <b>A read, never a mutation.</b> <c>ComputeTrainingInput</c> reads only <c>Focus</c>, the
        /// player's attributes and the coach seam — fields <see cref="AdvanceTrainingDay"/> does not
        /// write — which is the FR-TR-006 invariant that makes slot 1 and slot 2 order-independent. Call
        /// this before or after slot 2 and the batch is identical.
        /// </para>
        /// <para>
        /// <b>The ids travel with the inputs</b> so #28 can refuse a batch that does not describe the
        /// players it is about to advance. That turns "#29's roster view and #28's have drifted apart"
        /// from a silent mis-attribution of growth into a fail-loud at the seam.
        /// </para>
        /// </summary>
        /// <param name="squads">The rosters the attributes are read from; must match the state's roster.</param>
        /// <param name="coach">The #34 staff seam; <see cref="CoachingModifier.Identity"/> until #34 lands.</param>
        /// <exception cref="ArgumentNullException"><paramref name="squads"/> is null.</exception>
        /// <exception cref="ArgumentException">A club or a held player id cannot be resolved against <paramref name="squads"/>.</exception>
        internal ClubTrainingInputs[] GatherTrainingInputs(ISquadProvider squads, CoachingModifier coach)
        {
            if (squads == null)
            {
                throw new ArgumentNullException(nameof(squads));
            }

            var batch = new ClubTrainingInputs[_clubIds.Count];
            for (int c = 0; c < _clubIds.Count; c++)
            {
                Squad squad = ResolveSquad(squads, _clubIds[c]);
                int[] ids = _playerIds[c];
                TrainingState[] states = _training[c];

                var playerIds = new int[ids.Length];
                var inputs = new TrainingInput[ids.Length];
                for (int i = 0; i < ids.Length; i++)
                {
                    int local = RequireLocalIndex(squad, _clubIds[c], ids[i]);
                    PlayerRecord record = squad.GetPlayer(local);
                    playerIds[i] = ids[i];
                    // deepTrainingEnabled is #29's OWN Stage-2/3 dial (FR-TR-007), independent of #28's
                    // curveEnabled, and off at Stage 2 — so every input is Neutral today and the batch
                    // is behaviour-neutral by construction, not by accident. The seam is what lands
                    // here; the values arrive when #29 T3 turns its dial on.
                    inputs[i] = TrainingStep.ComputeTrainingInput(
                        in states[i], in record.Attributes, in coach, deepTrainingEnabled: false);
                }

                batch[c] = new ClubTrainingInputs(_clubIds[c], playerIds, inputs);
            }

            return batch;
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
        /// <b>The FR-MD-010 match-load term is supplied from the #30 appearance record</b>
        /// (ERR-041-010(b), closed at the balance pass): <see cref="RecordAppearances"/> writes each
        /// fielded XI post-round, and this step reads the windowed count through
        /// <see cref="AppearanceWindow.AppearanceDaysOn"/> — which never includes the current day, so
        /// the pre-round draw (ERR-030-027) can never see a match that has not been played.
        /// <c>HardContacts</c> stays 0 (deep-tier, KD-3).
        /// </para>
        /// </summary>
        /// <param name="worldDay">The world day being advanced — #30's clock BEFORE its step-12 increment.</param>
        /// <param name="worldSeed">The career's world seed (<c>WorldStore.WorldSeed</c>), the draw key's root — never a per-match seed.</param>
        /// <param name="squads">The rosters the attributes are read from; must match the state's roster.</param>
        /// <param name="medical">The #34 staff seam; <see cref="MedicalModifier.Identity"/> until #34 lands.</param>
        /// <exception cref="ArgumentNullException"><paramref name="squads"/> is null.</exception>
        /// <exception cref="ArgumentException">A club or a held player id cannot be resolved against <paramref name="squads"/>, or #41 refuses the day or the state.</exception>
        internal void AdvanceMedicalDay(
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
                AppearanceState[] appearance = _appearance[c];

                for (int i = 0; i < ids.Length; i++)
                {
                    int local = RequireLocalIndex(squad, _clubIds[c], ids[i]);
                    PlayerRecord record = squad.GetPlayer(local);

                    // The risk and the match load are occurrence-draw inputs and nothing else, so with
                    // the dial off they are read by nobody: #41's step only reaches AssembleRiskScore
                    // inside the `wasAvailableAtEntry && occurrenceEnabled` branch. Computing them
                    // anyway would be a per-player-per-day cost on every career today for values that
                    // are discarded — and, worse to read, would suggest the recovery countdown depends
                    // on them.
                    // The second gate mirrors MedicalStep's own idempotency cursor (F6): a re-entered
                    // day — the ERR-030-027 pre-round/next-advance pair — no-ops inside the step, so
                    // its inputs must not be computed on the re-entry either. Not just thrift: the
                    // re-entry runs AFTER the round recorded today's appearances, and reading the
                    // window then would be the one call site where the anchor can equal the day with
                    // today's match already in the bits — correct today (the window excludes bit 0 at
                    // shift 0), but only this guard keeps that correctness out of the draw path's
                    // dependencies. unchecked: the never-advanced sentinel is uint.MaxValue, so +1
                    // wraps to 0 and a fresh state passes for every day (Spec #16 §3.4.4-style
                    // deliberate wrap, though 32-bit here).
                    //
                    // INERT BY CONSTRUCTION today, so no test can fail if it is deleted (AR pass 4):
                    // on the re-entered day the shift-0 exclusion makes the window read identical and
                    // MedicalStep discards the inputs anyway. The guard's value is forward-looking —
                    // it keeps that coincidence from ever becoming a load-bearing dependency — and
                    // the pass-6 which-test-fails-if-reverted question has no answer here by design.
                    bool willAdvance =
                        worldDay >= unchecked(injury[i].LastAdvancedWorldDay + 1u);

                    InjuryRiskContribution risk;
                    MatchLoad load;
                    if (InjuryOccurrenceEnabled && willAdvance)
                    {
                        risk = TrainingStep.ComputeInjuryRisk(in training[i], in record.Attributes);
                        load = new MatchLoad(
                            AppearanceWindow.AppearanceDaysOn(in appearance[i], worldDay),
                            hardContacts: 0);
                    }
                    else
                    {
                        risk = InjuryRiskContribution.None;
                        load = MatchLoad.None;
                    }

                    MedicalStep.AdvanceMedicalDay(
                        ref injury[i],
                        ids[i],
                        in record.Attributes,
                        in risk,
                        in load,
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
        internal int SyncToRoster(ISquadProvider squads)
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
            var nextAppearance = new AppearanceState[clubs][];
            int churn = 0;

            for (int c = 0; c < clubs; c++)
            {
                Squad squad = ResolveSquad(squads, _clubIds[c]);
                int[] rosterIds = AscendingPlayerIds(squad);

                int[] heldIds = _playerIds[c];
                TrainingState[] heldTraining = _training[c];
                InjuryState[] heldInjury = _injury[c];
                AppearanceState[] heldAppearance = _appearance[c];

                var training = new TrainingState[rosterIds.Length];
                var injury = new InjuryState[rosterIds.Length];
                var appearance = new AppearanceState[rosterIds.Length];
                int carried = 0;

                // RECORDED, NOT FIXED (AR pass 4): this reconciliation is PER CLUB, so a player who
                // MOVES between two carried clubs is a departure here and a Create() there — his
                // conditioning, injury history and any ACTIVE injury silently reset; he arrives fit.
                // That is worse than the club-term key change #41 §3.1.1 refuses on the grounds that
                // "the player carries his medical identity" (FR-MD-006). Inert today — no allocator
                // moves players between clubs — but ERR-041-019's global-id guarantee is exactly what
                // makes cross-club carry implementable (one pre-pass building id → held state before
                // this walk), and that carry is #31 Transfers' arrival obligation, not a change to
                // sneak in here without its spec.
                for (int i = 0; i < rosterIds.Length; i++)
                {
                    int held = IndexOfPlayer(heldIds, rosterIds[i]);
                    if (held >= 0)
                    {
                        training[i] = heldTraining[held];
                        injury[i] = heldInjury[held];
                        appearance[i] = heldAppearance[held];
                        carried++;
                    }
                    else
                    {
                        // A fresh id: Create, never default — the day-0 trap both specs name.
                        // (AppearanceState's default IS its fresh state; the array element stays.)
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
                nextAppearance[c] = appearance;
            }

            // The ERR-041-019 precondition is re-checked here because the sync is the third id
            // entry point (after ForLeague and FromBlocks) — and the one #42's youth intake and
            // #31's transfers will actually come through. In the validating half, deliberately:
            // CommitRosterSync's contract is "cannot fail on a plan this career prepared".
            RequireGloballyUniquePlayerIds(_clubIds, nextIds, nameof(squads));

            return new RosterSyncPlan(
                RosterGeneration, nextIds, nextTraining, nextInjury, nextAppearance, churn);
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
                _appearance[c] = plan.Appearance[c];
            }

            // Bumped unconditionally, not only when churn > 0: the arrays are replaced either way, so a
            // plan prepared before this call is stale either way. A generation that only moved on churn
            // would accept a no-churn plan built against the PREVIOUS arrays and install it over the
            // current ones — the exact overwrite the check exists to refuse.
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

            /// <summary>Per club, the reconciled appearance states.</summary>
            internal readonly AppearanceState[][] Appearance;

            /// <summary>Entries inserted plus removed by this plan.</summary>
            internal readonly int Churn;

            /// <summary>Stages one reconciliation.</summary>
            /// <param name="generation">The career's roster generation at preparation time.</param>
            /// <param name="playerIds">Per-club reconciled player ids.</param>
            /// <param name="training">Per-club reconciled training states.</param>
            /// <param name="injury">Per-club reconciled medical states.</param>
            /// <param name="appearance">Per-club reconciled appearance states.</param>
            /// <param name="churn">Entries inserted plus removed.</param>
            internal RosterSyncPlan(
                int generation,
                int[][] playerIds,
                TrainingState[][] training,
                InjuryState[][] injury,
                AppearanceState[][] appearance,
                int churn)
            {
                Generation = generation;
                PlayerIds = playerIds;
                Training = training;
                Injury = injury;
                Appearance = appearance;
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
        /// seam — #41 answers only "is he fit".
        /// </para>
        /// <para>
        /// <b>RECORDED, NOT FIXED (AR pass 5): fielding the injured is strictly FREE.</b> A player
        /// pressed back in by this rule plays with unmodified attributes, CANNOT be re-injured (the
        /// KD-6 entry gate — he was not available at entry, so the occurrence draw never evaluates
        /// him), and his recovery is not extended. The balance pass armed the dial and deliberately
        /// did not add the consequence: pricing diminished performance is #27/#28 attribute
        /// territory and re-injury/extension is a #41 deep-tier rule, so the obligation lands with
        /// whichever arrives first — not with #30's selection seam.
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
                // reference-identical, which is every fully-fit club (the common case; the dial has been ARMED since the balance pass).
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
        /// The FR-TR-023 weekly focus command, resolved fresh against this career on every call.
        /// Returns <c>false</c> for a player the club's roster does not carry (#29's F2 refusal — a
        /// stale id from a screen is not a crash), <c>true</c> when the focus was written.
        /// <para>
        /// <b>A command rather than a handle, deliberately.</b> The obvious shape here is "hand out a
        /// <see cref="TrainingSchedule"/> and let the caller keep it", and it is wrong: the schedule
        /// binds a club's id and state arrays BY REFERENCE, and <see cref="SyncToRoster"/> replaces both
        /// wholesale at every season boundary. A screen that cached the handle across a roll — the
        /// obvious thing to write — would then call <c>TrySetFocus</c>, get <c>true</c> back, and write
        /// the manager's instruction into a discarded array. Nothing throws and nothing is logged; the
        /// player simply trains Balanced all season.
        /// </para>
        /// <para>
        /// A generation counter the caller is asked to compare would make that DETECTABLE, not
        /// prevented — and this file argues elsewhere that binding must be structural rather than
        /// documented, which is the whole reason <see cref="TrainingSchedule"/> exists at all. So the
        /// handle is not handed out: this method builds a transient one over the CURRENT arrays and
        /// discards it, which cannot be stale because it does not outlive the call. #29 keeps its
        /// single declared writer (<c>TrainingSchedule.TrySetFocus</c>) — this is a lookup in front of
        /// it, not a second one beside it.
        /// </para>
        /// </summary>
        /// <param name="clubId">The club.</param>
        /// <param name="playerId">The player whose focus is being set.</param>
        /// <param name="focus">The new focus.</param>
        /// <returns>True when written; false when the club's roster does not carry the player.</returns>
        /// <exception cref="ArgumentException">The club is not carried by this career.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="focus"/> is not a defined ordinal (#29 F4 / FR-TR-021).</exception>
        public bool TrySetFocus(int clubId, int playerId, TrainingFocus focus)
        {
            int club = RequireClub(clubId);
            return new TrainingSchedule(_playerIds[club], _training[club]).TrySetFocus(playerId, focus);
        }

        /// <summary>
        /// A <see cref="TrainingSchedule"/> over one club's live id and state arrays.
        /// <para>
        /// <b><c>internal</c>: the handle must not outlive the call that acquired it.</b> It binds both
        /// arrays by reference and <see cref="SyncToRoster"/> replaces them, so a cached handle silently
        /// discards writes — see <see cref="TrySetFocus"/>, which is the public surface and is a command
        /// precisely so no caller can hold one. This exists for in-assembly use where the lifetime is
        /// visibly bounded.
        /// </para>
        /// </summary>
        /// <param name="clubId">The club.</param>
        /// <exception cref="ArgumentException">The club is not carried.</exception>
        internal TrainingSchedule ScheduleFor(int clubId)
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
        /// be provable while <see cref="InjuryOccurrenceEnabled"/> is off — the isolation posture tests
        /// still use, and used exclusively before the balance pass armed production. <c>internal</c> so no production
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
// | 1.2     | 2026-08-06 | —      | AR pass 3 (2M). **M:** FromBlocks now COPIES the two state arrays  |
// |         |            |        | instead of borrowing them. ClubTrainingStates.States is a public   |
// |         |            |        | field over a borrowed array and the documented restore path feeds  |
// |         |            |        | this the very arrays SeasonSaveManager.Load hands back inside      |
// |         |            |        | SeasonSaveContents — so every holder of those contents was a       |
// |         |            |        | second writer of #29/#41 state, straight past both day steps.      |
// |         |            |        | v1.1 closed that hole on the SAVE route (internal accessors) and   |
// |         |            |        | left it open on the only other route in. **M:** ScheduleFor goes   |
// |         |            |        | internal and the public focus surface becomes TrySetFocus, a       |
// |         |            |        | command that resolves the club fresh on every call. The handle     |
// |         |            |        | binds arrays SyncToRoster replaces, so a screen caching it across  |
// |         |            |        | a boundary got true back and wrote into a discarded array; v1.1's  |
// |         |            |        | RosterGeneration made that DETECTABLE by a caller who remembered   |
// |         |            |        | to check, which is the documented-not-enforced standard this file  |
// |         |            |        | rejects everywhere else. RosterGeneration stays — CommitRosterSync |
// |         |            |        | refuses a stale plan on it — but nothing outside now depends on a  |
// |         |            |        | caller reading it.                                                 |
// | 1.3     | 2026-08-07 | —      | **Gate fix (CI run 405, the branch's first compile).** v1.2's      |
// |         |            |        | copy-not-borrow change declared its locals `training` / `injury`   |
// |         |            |        | inside FromBlocks, whose own PARAMETERS are `training` and         |
// |         |            |        | `medical` — so C# read every earlier use of the parameter in that  |
// |         |            |        | block as a use-before-declaration: 4x CS0841 plus CS0136 on the    |
// |         |            |        | declaration itself. Renamed to `trainingStates` / `injuryStates`.  |
// |         |            |        | Behaviour is exactly what v1.2 described; it had simply never been |
// |         |            |        | compiled, which is what every "NO GATE RUN" note on this landing   |
// |         |            |        | was warning about.                                                 |
// | 1.4     | 2026-08-07 | —      | Balance pass D2 (ERR-041-010(b)): the third state set. Owns the    |
// |         |            |        | per-player AppearanceState (lazily-shifted bitmask, no KD-2 day    |
// |         |            |        | slot), written by RecordAppearances (internal — SeasonLoop is the  |
// |         |            |        | single production writer) and read into the FR-MD-010 MatchLoad at |
// |         |            |        | slot 4, window-excluded from the current day per ERR-030-027.      |
// |         |            |        | FromBlocks takes the third block set (required, coherence-checked, |
// |         |            |        | states COPIED like its siblings); the roster sync carries          |
// |         |            |        | appearance state with the same plan/commit staging.                |
// | 1.5     | 2026-08-07 | —      | Balance pass D4: the dial is ARMED (FR-MD-027 as revised). The     |
// |         |            |        | injuryOccurrenceEnabled argument becomes REQUIRED on ForLeague and |
// |         |            |        | FromBlocks — a default in either position flips every omitting     |
// |         |            |        | call site with no diff the day it changes; construction declares   |
// |         |            |        | the position (production true; isolation tests false, both locked  |
// |         |            |        | at season scale by SeasonInjuryRealismTests).                      |
// | 1.6     | 2026-08-07 | —      | Balance-pass AR pass 1 (L): RecordAppearances pre-checks the day-  |
// |         |            |        | regression refusal in its validate loop, so the write loop can no  |
// |         |            |        | longer throw half-way and leave a club half-recorded — the         |
// |         |            |        | validate-all-then-write promise its doc already made.              |
// | 1.7     | 2026-08-07 | —      | Balance-pass AR pass 2 (2L): AdvanceMedicalDay computes the        |
// |         |            |        | occurrence inputs only when the step will actually advance         |
// |         |            |        | (mirrors MedicalStep's F6 cursor) — the ERR-030-027 re-entered day |
// |         |            |        | no longer reads the appearance window with today's match already   |
// |         |            |        | in the bits; + the three-parallel-state-sets ceiling note (a       |
// |         |            |        | fourth set collapses this into a per-player career struct).        |
// | 1.8     | 2026-08-08 | —      | Balance-pass AR pass 3 (H+M+L). H (ERR-041-019): cross-club        |
// |         |            |        | PlayerId uniqueness enforced at all three id entry points          |
// |         |            |        | (ForLeague, FromBlocks, PrepareRosterSync) — #41's occurrence      |
// |         |            |        | draw has no club term, so a shared id means shared injury luck     |
// |         |            |        | forever, and #27 only promises club-scoped uniqueness. M2:         |
// |         |            |        | FromBlocks copies the ID arrays too (a public-field alias could    |
// |         |            |        | break the ascending precondition just enforced). L6:               |
// |         |            |        | RecordFixtureAppearances — both clubs validated before either is   |
// |         |            |        | written, the per-club discipline one level up.                     |
// | 1.9     | 2026-08-08 | —      | Balance-pass AR pass 4: the uniqueness guard goes internal so      |
// |         |            |        | SeasonSaveManager's coherence gate shares the one walk (M1); the   |
// |         |            |        | per-club roster reconciliation carries the RECORDED-NOT-FIXED      |
// |         |            |        | transfer residual (M4 — a moved player's career state resets;      |
// |         |            |        | #31's arrival obligation); the F6-mirror guard's comment states    |
// |         |            |        | it is inert by construction and cannot be locked (L10).            |
// | 1.10    | 2026-08-08 | —      | Balance-pass AR pass 5: FromBlocks' mismatch paramNames name   |
// |         |            |        | the offending set (L10); the SelectAvailable doc carries the   |
// |         |            |        | RECORDED-NOT-FIXED fielding-the-injured-is-free residual (M5 — |
// |         |            |        | the dial is armed and the consequence was deliberately not     |
// |         |            |        | added; #27/#28 attributes or #41 deep-tier own it); four       |
// |         |            |        | stale dial-off sentences updated to the armed posture.         |
// | 1.11    | 2026-08-08 | —      | Balance-pass AR pass 6 (M3): + RequireCursorsWithinClock — the |
// |         |            |        | composition-boundary twin of the save root's walk, called by  |
// |         |            |        | SeasonLoop's constructor; AdvanceTrainingDay/AdvanceMedicalDay |
// |         |            |        | /SyncToRoster go internal (public bought nothing — the only   |
// |         |            |        | callers are SeasonLoop and this assembly's tests — and handed |
// |         |            |        | any Career holder the ability to drive the career off the     |
// |         |            |        | world clock). Step-12 prose fixed (L1).                       |
// | 1.12    | 2026-08-08 | —      | Balance-pass AR pass 9 (M1): the cursor-vs-clock rule collapses  |
// |         |            |        | to ONE owner — RequireTraining/Medical/AppearanceAnchor-         |
// |         |            |        | WithinClock internal statics here; the instance walk and         |
// |         |            |        | SeasonSaveManager's block walk both delegate (the                |
// |         |            |        | RequireGloballyUniquePlayerIds shape). The file-boundary copy    |
// |         |            |        | had an UNLOCKED predicate (medical lag >= 2) — the parallel-     |
// |         |            |        | surface class the T2 AR's H3 collapsed, recurring one gate over. |
// | 1.13    | 2026-08-08 | —      | Balance-pass AR pass 11 (L4, doc): one draw key had three          |
// |         |            |        | spellings across the uniqueness guard's doc, its throw message     |
// |         |            |        | and #41 SS3.1.1; both local sites now name (worldSeed, playerId,   |
// |         |            |        | actionOrdinal = worldDay x RADIX + purpose), matching the spec.    |
// | 1.14    | 2026-08-08 | —      | Balance-pass AR pass 13 (L2, doc): FromBlocks' doc still counted   |
// |         |            |        | "two" blocks — three since D2 (the pass-12-L1 stale-count class,  |
// |         |            |        | one file over).                                                    |
// | 1.15    | 2026-08-08 | —      | #28 T2a: + GatherTrainingInputs (#29 §3.5 step 1 — the slot-1  |
// |         |            |        | batch #30 hands to #28, keyed by player id so a drift between  |
// |         |            |        | the two roster views fails loud); + RequireProgressionCursor-  |
// |         |            |        | WithinClock, the fourth per-player cursor's single owner, with |
// |         |            |        | all three boundaries delegating to it (ERR-028-007).           |
#endregion
