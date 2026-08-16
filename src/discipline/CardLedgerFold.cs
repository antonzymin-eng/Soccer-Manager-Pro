// File:     src/discipline/CardLedgerFold.cs
// Created:  2026-08-13
// Modified: 2026-08-16, latest of all (L-1 and M-B, adversarial review, doc only — v1.11: L-1 —
//           NO_PLAYER's doc said a spec declaration "is owed" against #44; ERR-044-013 discharged it
//           2026-08-15 (section-2.md v0.9 + appendices v0.8) — added a dated discharge note. M-B —
//           the constructor's onPitchAgentIdCount ArgumentOutOfRangeException doc now cites
//           CardLedgerFoldTests.Constructor_OnPitchAgentIdCountOutOfRange_Throws (T-DC-FOLD-003) as the
//           lock, giving the spec citation an anchor. No behaviour change either way.)
// Modified: 2026-08-16, latest again (findings A and B, reviewed findings pass — v1.10: A —
//           ERR-044-022. The constructor now takes a required onPitchAgentIdCount, validated
//           0 < onPitchAgentIdCount <= the seed's length, and ApplySubstitution refuses an Incoming
//           that names an on-pitch id ("an on-pitch agent id cannot come on") and an Outgoing that
//           names a bench id ("only an on-pitch agent id can go off") — both before the write/clear
//           pair runs. Closes the hole the v1.8 M1 fix could not see: the seed's one-to-one check
//           runs once at construction and cannot, by itself, tell a bench id from a pitch id at
//           ApplySubstitution time, so Sub(Outgoing=5, Incoming=6) with 6 an occupied ON-PITCH slot
//           previously wrote silently — player 100 (slot 5's prior occupant) lost his mapping
//           entirely and a card at slot 5 landed on player 101 (slot 6's occupant), the Appendix C
//           "slot 19" family (ERR-044-001) the v1.8 M1 comment falsely claimed the vacated-slot
//           clear alone made "impossible rather than merely unlikely" — that claim covered only the
//           narrow bench-double-mapping shape M1 actually closed; it is annotated in place in the
//           version-history row below rather than rewritten, and the inline comment at
//           ApplySubstitution is corrected to state what is true now that this fix lands. SeasonLoop's
//           construction site passes MatchEngineConstants.SQUAD_SIZE. B — ERR-044-023, doc only, no
//           behaviour change: the constructor's XML doc now states the boot-time precondition on the
//           seed explicitly (MatchEngine.PlayerIdsByAgentId is one-to-one over non-sentinel entries
//           only AT BOOT — SubstitutePlayer never clears the incoming player's bench-origin entry, so
//           a seed taken after any substitution maps him to two agent ids and this constructor's own
//           M1 check would refuse it), matching the corrected MatchEngine.cs XML doc.
// Modified: 2026-08-16, latest (findings A and C, doc only — v1.9: A — CommitWithExplicitConfig's L2
//           doc still said wiring DisciplineRules.AddYellow/AddBan through the guarded YellowsPlusOne/
//           BanMatchesPlus helpers was "outside this file's ownership for this pass" — stale the same
//           day it was written, since DisciplineRules.cs v1.9/v1.10 landed that wiring. Corrected to
//           say the helpers ARE wired and to state the surviving residual honestly: Commit still
//           applies card-by-card, so a mid-list OverflowException still leaves 0..k-1 applied with
//           _committed false — the guards make the failure loud and correctly named, they do not make
//           Commit atomic against this class. C — RequireCommittableConfig's M2 remark now says the
//           completeness lock covers every public static readonly field of ANY type, not only int,
//           matching DisciplineConfigCompletenessTests.cs v1.1. No code change — this file's ownership
//           here is DOC comments only.)
// Modified: 2026-08-16, later (reviewed findings pass, M1/M3/L2 — v1.8: M1 — ApplySubstitution now
//           clears the vacated incoming slot after the swap (a later record naming it throws F1 rather
//           than silently double-booking) and refuses outgoing == incoming; the constructor's seed loop
//           now refuses a seed that maps one player id to two agent ids. M3 — ObserveTick refuses a
//           non-consecutive IDisciplineTickLedgerTap.CurrentTick and latches shut on a part-way
//           failure, mirroring #37 MatchAnalyticsAggregator's F6; IDisciplineTickLedgerTap gained
//           CurrentTick. L2 — CommitWithExplicitConfig's atomicity doc now scopes the "all-or-nothing"
//           claim to the four RequireCommittableConfig guards, naming the uncovered accumulator-overflow
//           throw from DisciplineEntry's range guards as a real, currently-open gap; the guarded
//           DisciplineEntry.YellowsPlusOne/BanMatchesPlus helpers exist for DisciplineRules to route
//           through, which is outside this file's ownership for this pass.)
// Modified: 2026-08-16 (adversarial-review M2, doc only — RequireCommittableConfig records that adding
//           a guarded [GT] is a five-site change and names DisciplineConfigCompletenessTests, the check
//           that detects the drift; the DisciplineConfig restructure stays recorded and gated — v1.7)
// Modified: 2026-08-15, later (reviewed findings pass, L3/L5 — v1.6: L3 — the L22 comment inside
//           ObserveTick was spliced into the middle of a sentence ("…read at the same index only in the
//           sense" broken across the whole L22 block from "that both are dispatched…"); the sentence now
//           closes before the L22 paragraph instead of resuming after it. L5 — NO_PLAYER's XML doc had
//           no tag (FR-CS-060/061); tagged [FIXED]. A #44 Appendix A row + spec declaration are owed and
//           are reported, not filed here.)
// Modified: 2026-08-15 (#44 AR round 5, L19/L22 — the type remark's claim that SeasonSaveManager
//           refuses a live ActiveMatch was FALSE and is replaced by the real reason the hazard is
//           unreachable; ObserveTick now states its dependency on MatchEngine.RunResolvePhase
//           flushing queued SubstitutionEvents before card issuance — v1.5. Prior:
//           2026-08-13, round 5 L17 — RequireKnownCardKind's three
//           comparisons updated for DisciplineConstants' CARD_KIND_* -> CardKind* rename ([CROSS],
//           PascalCase per src/CLAUDE.md §3.2.3); no behaviour change — v1.4)
// Author:   —
// Spec:     Discipline & Suspensions #44 §3.1 (the occupancy fold) / §4.3 (the tap read);
//           FR-DC-002/003/004/005/006/010; F1/F4; ERR-044-001 (Appendix C's bench-id defect);
//           ERR-044-022 (the onPitchAgentIdCount boundary); ERR-044-023 (the boot-time seed
//           precondition); Code Standards #20
// Purpose:  The per-tick fold over an engine-resolved fixture's card and substitution records —
//           attributes each card to the PlayerId occupying the recipient agent slot AT THAT TICK, and
//           commits the fixture's whole card list once, at resolution.

using System;
using System.Collections.Generic;

using TacticalDirector.EventSystem;

namespace TacticalDirector.Discipline
{
    /// <summary>
    /// Folds one engine-resolved fixture's cards onto players (#44 §3.1, KD-2).
    /// <para>
    /// <b>Occupancy, not slots.</b> The engine's own per-slot yellow count is reset by a substitution
    /// (<c>MatchEngine.SubstitutePlayer</c>, v1.33 — correctly, since the slot now holds a different
    /// player), so reading post-match slot state would lose a substituted player's bookings entirely.
    /// This fold instead tracks WHO occupies each agent slot at each tick, seeded from the fixture's
    /// configured lineup and updated on every <see cref="SubstitutionEvent"/>, and attributes each
    /// <see cref="CardIssuedEvent"/> to the occupant at the card's own tick (FR-DC-005).
    /// </para>
    /// <para>
    /// <b>Buffer, then commit once at resolution</b> (FR-DC-010: "the fold MUST complete at fixture
    /// resolution"). Writing straight through to <see cref="DisciplineRules"/> would put half a
    /// fixture's cards into persisted state at any moment a save could be taken mid-fixture — and a
    /// restore has no way to know which half, because KD-2 rules out re-deriving the tally from engine
    /// slot state. <b>Nothing refuses that save path</b> (L19) — <c>SeasonSaveManager</c> does NOT
    /// decline a live <c>ActiveMatch</c>; it documents that <c>SeasonLoop.ActiveMatch</c> is not a safe
    /// source and asks the caller to pass an engine it owns. The hazard is unreachable today only
    /// because no seam exposes a mid-fixture engine at all: <c>ActiveMatch</c> is non-null solely
    /// inside the synchronous tick loop that owns it. That is unreachability, not a gate — and
    /// <c>MatchSession.TickOnce/CaptureSave/RestoreFrom</c> already exists one assembly over. <b>Buffering alone does not make a mid-fixture save CORRECT</b> (L2) — this fold
    /// is not itself serialized, so a restore rebuilds an empty fold and every card issued before the
    /// save point is lost outright, trading a half-persisted tally for a silently-truncated one.
    /// Buffering keeps a half-fixture tally OUT of persisted state; making a mid-fixture save correct
    /// additionally requires serializing the fold's pending list, which is deferred with the seam.
    /// </para>
    /// <para>
    /// <b>Observer-neutral</b> (FR-DC-003): every tap member is a read and this type holds no reference
    /// to the engine. An observed fixture is digest-identical to an unobserved one.
    /// </para>
    /// <para>
    /// <b>Lossless consumption is enforced, not merely documented</b> (M3, reviewed findings pass).
    /// <see cref="ObserveTick"/> refuses a non-consecutive
    /// <see cref="IDisciplineTickLedgerTap.CurrentTick"/> and latches shut if a tick throws part-way
    /// through — the same F6 shape #37's <c>MatchAnalyticsAggregator</c> enforces, adopted here because
    /// nothing previously distinguished a caller that skipped a tick from one that pumped every tick;
    /// both looked identical to this fold before this pass.
    /// </para>
    /// <para>
    /// <b>Recorded — the substitution branch has no production driver.</b> <c>SeasonLoop</c> never calls
    /// <c>SubstitutePlayer</c> (Stage 0 fields a fixed eleven), so occupancy never actually changes on
    /// the season path. That is precisely the shape that shipped <c>BootFixtureEngine</c> unrun for
    /// months, so the branch is driven from authored record lists in the suite rather than from a
    /// contrived match — a test that cannot reach a branch does not cover it.
    /// </para>
    /// <para>
    /// <b>The seed must be taken at boot (ERR-044-023).</b> <c>MatchEngine.PlayerIdsByAgentId</c> is
    /// one-to-one over its non-sentinel entries only at that moment — <c>SubstitutePlayer</c> never
    /// clears the incoming player's own bench-origin entry, so a seed taken after even one
    /// substitution maps him to two agent ids, and this class's own constructor (M1) would then
    /// correctly refuse it. See the constructor's own doc for the precondition and where it comes
    /// from; <c>SeasonLoop</c> satisfies it by seeding immediately after <c>BootFixtureEngine</c>,
    /// before the tick loop that could ever call <c>SubstitutePlayer</c> runs.
    /// </para>
    /// </summary>
    public sealed class CardLedgerFold
    {
        /// <summary>[FIXED] Occupancy sentinel: this agent id maps to no player (an unused slot in the
        /// seed). Reviewed findings pass, L5, 2026-08-15: this constant carried no tag, violating
        /// FR-CS-060/061 — a spec declaration and Appendix A row were owed against #44.
        /// <para>
        /// <b>Discharged 2026-08-15 — ERR-044-013.</b> The owed spec declaration landed the same day
        /// (section-2.md v0.9 + appendices v0.8); this L5 debt is closed.
        /// </para>
        /// </summary>
        public const int NO_PLAYER = -1;

        // Indexed by agent id: on-pitch slots first, then the engine's synthetic bench ids. Mutable —
        // a substitution moves the occupant of the OUTGOING slot to the incoming player's identity.
        private readonly int[] _occupancy;
        private readonly int _competitionId;

        // ERR-044-022: the boundary between on-pitch agent ids [0, _onPitchAgentIdCount) and the
        // engine's synthetic bench ids [_onPitchAgentIdCount, _occupancy.Length). ApplySubstitution
        // uses this to refuse an Incoming that names an on-pitch id or an Outgoing that names a bench
        // id — a distinction the seed's own one-to-one check (M1) cannot make, since it only knows
        // player ids and array length, never which agent ids are on-pitch versus bench.
        private readonly int _onPitchAgentIdCount;

        // The fixture's cards in observation order, buffered until Commit. Order is the bus's canonical
        // publish order (tick, then intra-phase), which is what makes FR-DC-021 hold: the same fixture
        // always yields the same list, so it always yields the same tally.
        private readonly List<CardAttribution> _pending = new List<CardAttribution>();

        private bool _committed;

        // ── Lossless-consumption enforcement (M3, reviewed findings pass) ──────────────────────────
        // Mirrors #37 MatchAnalyticsAggregator's F6: ObserveTick is a per-tick obligation (see the type
        // remarks and IDisciplineTickLedgerTap's own doc), and neither this class nor the tap interface
        // could previously tell a skipped tick from a normal one. _hasObservedTick/_lastObservedTick
        // anchor on the first call and refuse a non-consecutive one thereafter; _faulted latches once a
        // tick throws part-way through, so a partial application never silently continues into the next
        // tick's count.
        private bool _hasObservedTick;
        private ulong _lastObservedTick;
        private bool _faulted;

        /// <summary>
        /// Seeds the fold with the fixture's full agent-id → <c>PlayerId</c> map — <b>starters and
        /// bench</b>, because a card can be shown to a player who came on.
        /// <para>
        /// <b>Boot-time precondition (ERR-044-023).</b> <paramref name="occupancyByAgentId"/> must be
        /// a snapshot taken AT BOOT, before any substitution. <c>MatchEngine.PlayerIdsByAgentId</c> is
        /// one-to-one over its non-sentinel entries only at that moment: <c>SubstitutePlayer</c>
        /// copies the incoming player's identity onto the outgoing on-pitch slot but never clears his
        /// OWN bench-origin entry, so a seed taken after even one substitution maps that player to two
        /// agent ids at once and the constructor's own one-to-one check (M1, below) would then
        /// correctly refuse it — for a caller with no fresh boot-time array left to re-supply.
        /// <c>SeasonLoop</c> satisfies this by calling <c>PlayerIdsByAgentId()</c> immediately after
        /// <c>BootFixtureEngine</c>, before the tick loop that could ever call
        /// <c>SubstitutePlayer</c> runs.
        /// </para>
        /// </summary>
        /// <param name="occupancyByAgentId">
        /// Indexed by the engine's agent id. On-pitch slots occupy the low indices; the engine's
        /// substitution ids are <b>synthetic</b> — <c>SQUAD_SIZE + teamId * SUBSTITUTES_PER_TEAM +
        /// benchIndex</c> (verified at <c>MatchEngine.SubstitutePlayer</c>) — and occupy the high ones,
        /// so this array is longer than the pitch. Entries with no player carry
        /// <see cref="NO_PLAYER"/>. The array is copied; the caller's may be reused.
        /// <para>
        /// #44 §7.1 T2 lists "the <c>Incoming</c>-id semantics verified against the live engine" as a
        /// T-phase obligation. It is, and the verification FAILED against Appendix C, whose worked
        /// example puts a bench player at "slot 19" — an ON-PITCH slot under <c>SQUAD_SIZE = 22</c>.
        /// Filed as <b>ERR-044-001</b>; the code follows the engine.
        /// </para>
        /// </param>
        /// <param name="onPitchAgentIdCount">
        /// The number of leading entries of <paramref name="occupancyByAgentId"/> that are ON-PITCH
        /// agent ids — <c>MatchEngineConstants.SQUAD_SIZE</c> in production. Every entry at or past
        /// this index is one of the engine's synthetic bench ids. <see cref="ApplySubstitution"/> uses
        /// this boundary to refuse a <c>SubstitutionEvent</c> whose <c>Incoming</c> names an on-pitch
        /// id (an on-pitch agent id cannot come ON) or whose <c>Outgoing</c> names a bench id (only an
        /// on-pitch agent id can go OFF) — <b>ERR-044-022</b>, the gap the M1 seed-injectivity check
        /// alone could not close: that check runs once, over the whole seed, and knows only player ids
        /// and array length, never which agent ids are on-pitch versus bench.
        /// </param>
        /// <param name="competitionId">The competition partition these cards accrue in (FR-DC-012).</param>
        /// <exception cref="ArgumentNullException"><paramref name="occupancyByAgentId"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="onPitchAgentIdCount"/> is not
        /// strictly greater than zero and at most <paramref name="occupancyByAgentId"/>'s length
        /// (ERR-044-022). Locked by <c>CardLedgerFoldTests.Constructor_OnPitchAgentIdCountOutOfRange_
        /// Throws</c> (T-DC-FOLD-003, M-B reviewed findings pass) — both edges plus a negative value,
        /// mutation-verified.</exception>
        /// <exception cref="ArgumentException">The seed is empty, carries a negative player id that is
        /// not <see cref="NO_PLAYER"/>, or maps the same non-<see cref="NO_PLAYER"/> player id to two
        /// agent ids (M1, reviewed findings pass — the seed must be one-to-one, or a card at either
        /// agent id would attribute to the same player while whoever else the seed intended for one of
        /// those ids loses his mapping entirely).</exception>
        public CardLedgerFold(int[] occupancyByAgentId, int onPitchAgentIdCount, int competitionId)
        {
            if (occupancyByAgentId == null)
            {
                throw new ArgumentNullException(nameof(occupancyByAgentId));
            }
            if (occupancyByAgentId.Length == 0)
            {
                throw new ArgumentException(
                    "CardLedgerFold: the occupancy seed is empty — a fixture always fields somebody, so " +
                    "an empty seed means the lineup mapping was never threaded in (F1 at construction, " +
                    "where the message can still name the cause).",
                    nameof(occupancyByAgentId));
            }
            if (onPitchAgentIdCount <= 0 || onPitchAgentIdCount > occupancyByAgentId.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(onPitchAgentIdCount), onPitchAgentIdCount,
                    "CardLedgerFold: onPitchAgentIdCount must be > 0 and <= the occupancy seed's " +
                    "length (" + occupancyByAgentId.Length + ") — it marks the boundary between " +
                    "on-pitch agent ids and the engine's synthetic bench ids (ERR-044-022); every " +
                    "entry from it onward is a bench id.");
            }
            _onPitchAgentIdCount = onPitchAgentIdCount;

            _occupancy = new int[occupancyByAgentId.Length];

            // M1: the seed must be one-to-one — every non-NO_PLAYER player id may occupy exactly one
            // agent id. A duplicate is not a harmless redundancy: ObserveTick would attribute a card at
            // EITHER agent id to the same player, and whichever id the seed actually meant for a
            // DIFFERENT player is now indistinguishable from a legitimate mapping. Checked at
            // construction, once, rather than left as a runtime property nothing enforces.
            var firstAgentIdForPlayer = new Dictionary<int, int>();
            for (int i = 0; i < occupancyByAgentId.Length; i++)
            {
                int playerId = occupancyByAgentId[i];
                if (playerId < 0 && playerId != NO_PLAYER)
                {
                    throw new ArgumentException(
                        "CardLedgerFold: occupancy seed at agent id " + i + " is " + playerId +
                        "; a player id is >= 0 and an unused slot is NO_PLAYER (" + NO_PLAYER + ").",
                        nameof(occupancyByAgentId));
                }
                if (playerId != NO_PLAYER)
                {
                    if (firstAgentIdForPlayer.TryGetValue(playerId, out int firstAgentId))
                    {
                        throw new ArgumentException(
                            "CardLedgerFold: the occupancy seed maps player " + playerId +
                            " to two agent ids (" + firstAgentId + " and " + i + "). The mapping must " +
                            "be one-to-one (M1) — a card at either id would attribute to player " +
                            playerId + ", and whoever the seed actually intended for one of these ids " +
                            "would never be attributed a card at all.",
                            nameof(occupancyByAgentId));
                    }
                    firstAgentIdForPlayer[playerId] = i;
                }
                _occupancy[i] = playerId;
            }

            _competitionId = competitionId;
        }

        /// <summary>Cards folded so far this fixture. Zero for the overwhelming majority of fixtures.</summary>
        public int PendingCardCount => _pending.Count;

        /// <summary>
        /// Consumes the records the engine captured for the tick just completed (FR-DC-002). Must be
        /// called every tick — the tap is scoped to one tick and a skipped call loses those records
        /// rather than deferring them.
        /// <para>
        /// Only <see cref="CardIssuedEvent"/> and <see cref="SubstitutionEvent"/> are folded; every
        /// other ordinal is <b>ignored</b>, which is the FR-DC-004 forward-compatibility posture — a
        /// new Tier A event must not stop a season.
        /// </para>
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="tap"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The fixture has already been committed; a
        /// previous call to this method failed part-way through, poisoning the fold (<b>M3</b> — see
        /// the type remarks' lossless-consumption note); <paramref name="tap"/>'s
        /// <see cref="IDisciplineTickLedgerTap.CurrentTick"/> is not consecutive with the last observed
        /// tick (<b>M3</b>, mirrors #37 <c>MatchAnalyticsAggregator</c>'s F6); or a record names an
        /// agent slot with no occupancy mapping (<b>F1</b> — the lineup seed is incomplete, a
        /// root-contract bug whose silent form is misattribution).</exception>
        /// <exception cref="ArgumentOutOfRangeException">A <c>CardKind</c> outside <c>{0, 1, 2}</c>
        /// (<b>F4</b>).</exception>
        public void ObserveTick(IDisciplineTickLedgerTap tap)
        {
            if (tap == null)
            {
                throw new ArgumentNullException(nameof(tap));
            }
            RequireNotCommitted();
            RequireNotFaulted();
            RequireConsecutive(tap.CurrentTick);

            // M3: latched BEFORE the loop runs, cleared only once the WHOLE tick applied. A record
            // partway through the loop can throw (F1/F4) — RequireConsecutive above has already
            // advanced by the time that happens, so without this latch the NEXT call would be accepted
            // and the fold would carry on buffering cards on top of a tick it only half-applied. That is
            // the exact silent-wrong-tally shape #37's F6 exists to prevent, one layer up.
            _faulted = true;

            byte cardOrdinal = EventRegistry.GetOrdinal<CardIssuedEvent>();
            byte substitutionOrdinal = EventRegistry.GetOrdinal<SubstitutionEvent>();

            int count = tap.RecordCount;
            for (int i = 0; i < count; i++)
            {
                byte ordinal = tap.OrdinalAt(i);

                // Substitutions are handled BEFORE cards are read at the same index only in the sense
                // that both are dispatched in the tap's own canonical order — a substitution and a card
                // in one tick apply in publish order, which is what FR-DC-021 pins.
                //
                // L22 — DEPENDENCY ON THE ENGINE'S PHASE ORDER, stated because nothing else states it.
                // MatchEngine.SubstitutePlayer swaps the slot IMMEDIATELY, between ticks, and QUEUES the
                // SubstitutionEvent for flush at the top of the next RunResolvePhase — the same phase that
                // issues cards. This fold is correct only while that flush PRECEDES card issuance in
                // RunResolvePhase. If it ever moves after, every card in the substitution tick is
                // attributed to the OUTGOING player, silently and permanently; Stage 0 fields a fixed
                // eleven, so SubstitutePlayer has no production caller and nothing would catch it.
                if (ordinal == substitutionOrdinal)
                {
                    SubstitutionEvent sub = tap.RecordAt<SubstitutionEvent>(i);
                    ApplySubstitution(sub.Outgoing, sub.Incoming);
                }
                else if (ordinal == cardOrdinal)
                {
                    CardIssuedEvent card = tap.RecordAt<CardIssuedEvent>(i);
                    RequireKnownCardKind(card.CardKind);
                    _pending.Add(new CardAttribution(OccupantOf(card.Recipient, "CardIssuedEvent.Recipient"), card.CardKind));
                }

                // else: an ordinal #44 does not fold. Ignored (FR-DC-004).
            }

            // Reached only when the WHOLE tick applied; any escape above leaves _faulted set. No
            // try/finally — a finally would clear it on the exception path too, which is the one path
            // it exists for (mirrors #37 MatchAnalyticsAggregator.ObserveTick verbatim).
            _faulted = false;
        }

        // ── M3 (reviewed findings pass): lossless-consumption guard ────────────────────────────────

        private void RequireNotFaulted()
        {
            if (_faulted)
            {
                throw new InvalidOperationException(
                    "CardLedgerFold.ObserveTick: a previous tick failed part-way through, so this " +
                    "fixture's buffered cards are incomplete and every further tick would compound a " +
                    "tally that is silently wrong (M3, mirrors #37 MatchAnalyticsAggregator's F6). " +
                    "Commit still applies whatever was buffered before the failure; a new fixture needs " +
                    "a new fold.");
            }
        }

        private void RequireConsecutive(ulong currentTick)
        {
            if (_hasObservedTick && currentTick != _lastObservedTick + 1UL)
            {
                throw new InvalidOperationException(
                    "CardLedgerFold.ObserveTick: tick " + currentTick + " is not consecutive with the " +
                    "last observed tick " + _lastObservedTick + ". ObserveTick is a per-tick obligation " +
                    "of the composition root (#44 §4.3) — a skipped tick does not defer its records, it " +
                    "loses them permanently (M3, mirrors #37 MatchAnalyticsAggregator's F6).");
            }

            _hasObservedTick = true;
            _lastObservedTick = currentTick;
        }

        /// <summary>
        /// Applies the fixture's whole card list to <paramref name="rules"/>, in publish order, and
        /// closes the fold. Called once, at fixture resolution (FR-DC-010) — so a card in fixture N is
        /// already banning by the time fixture N+1 selects, with no off-by-one.
        /// </summary>
        /// <returns>The number of cards applied.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="rules"/> is null.</exception>
        /// <exception cref="InvalidOperationException">This fold has already been committed — a second
        /// commit would double every card in the fixture; or a bound <c>[GT]</c> this fixture's cards
        /// could touch is out of range (M13 — see <see cref="CommitWithExplicitConfig"/>).</exception>
        public int Commit(DisciplineRules rules)
        {
            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            return CommitWithExplicitConfig(
                rules,
                DisciplineConstants.YellowAccumulationThreshold,
                DisciplineConstants.AccumBanMatches,
                DisciplineConstants.SecondYellowBanMatches,
                DisciplineConstants.StraightRedBanMatches);
        }

        /// <summary>
        /// <see cref="Commit"/>'s real body, with the three <c>[GT]</c>s <paramref name="rules"/>' card
        /// application can throw on taken as PARAMETERS rather than read directly off
        /// <see cref="DisciplineConstants"/> (M13). This is the L5/L11 seam — the same reason
        /// <c>DisciplineRules.RequireYellowThreshold</c>/<c>RequireBanLength</c> are <c>internal</c> —
        /// extended here because <see cref="DisciplineConstants"/>' fields are resolved once, at their
        /// non-negative defaults, before any test in this process can bind a bad config value, so
        /// nothing exercised through the public <see cref="Commit"/> can observe the property this
        /// method exists to guarantee: a throw on card <c>k</c> must leave cards <c>0..k-1</c>
        /// UNAPPLIED and <see cref="_committed"/> still <c>false</c>, never applied-then-discarded.
        /// <para>
        /// <b>M13.</b> Applying cards one at a time through <see cref="DisciplineRules.ApplyCard"/> and
        /// letting a bad <c>[GT]</c> throw mid-list left cards <c>0..k-1</c> already written to
        /// persisted state while the fold — carrying card <c>k</c> onward, uncommitted — was discarded
        /// by the caller: exactly the half-fixture tally buffering exists to prevent (see the type
        /// remarks). M6 made the consequence PERMANENT: the fixture is marked played by the time
        /// <c>SeasonLoop.PlayNextRound</c> calls this, so a retry's unplayed-index filter skips it and
        /// the partial tally can never be repaired. Fixed by validating every <c>[GT]</c> this
        /// fixture's cards could touch ONCE, before the loop, so a refusal can never follow a partial
        /// write.
        /// </para>
        /// <para>
        /// <b>M17 — atomicity is not recoverability.</b> This makes the commit all-or-nothing, which
        /// leaves the fixture's whole card list lost rather than half-applied, and M6's placement makes
        /// THAT permanent too. The round-level answer is <see cref="RequireCommittableConfig()"/>, run
        /// once before the first fixture of the round is touched; the guards below stay because they
        /// are this type's own contract and a caller that skipped the pre-check still must not write
        /// half a fixture.
        /// </para>
        /// <para>
        /// <b>L2 — the atomicity claim's SCOPE (closed at DisciplineRules.cs v1.10; residual restated,
        /// not removed).</b> "All-or-nothing" above is a claim about the FOUR pre-validated
        /// <c>[GT]</c>s <see cref="RequireCommittableConfig()"/> checks — the only throws this loop is
        /// proven to rule out in advance. It is NOT a claim that <c>rules.ApplyCard</c> cannot throw for
        /// any other reason. <c>DisciplineRules.AddYellow</c>/<c>AddBan</c> now route every accumulator
        /// addition they perform through the guarded <see cref="DisciplineEntry.YellowsPlusOne"/> /
        /// <see cref="DisciplineEntry.BanMatchesPlus"/> helpers (DisciplineRules.cs v1.10 — the second,
        /// AddYellow's own <c>AccumBanMatches</c> addition, was still unrouted as of v1.9), so an
        /// accumulator at <c>int.MaxValue</c> now throws a loud, correctly-named
        /// <see cref="OverflowException"/> BEFORE any write, never <see cref="DisciplineEntry"/>'s
        /// constructor misreporting it as "a negative tally is a counting bug". <b>That does not make
        /// this loop atomic against it.</b> Commit still applies cards one at a time through
        /// <c>rules.ApplyCard</c>, so a card <c>k</c> whose accumulator overflow now throws (loudly,
        /// correctly named) still leaves cards <c>0..k-1</c> already applied and <see cref="_committed"/>
        /// still <c>false</c> — exactly the half-applied fixture this method's own M13/M17 fixes exist to
        /// prevent for the four pre-validated <c>[GT]</c>s, and do not extend to this cause. The guards
        /// make the failure loud and correctly named; they do not make <c>Commit</c> atomic against this
        /// class of throw. Reaching this in practice needs a decoded row already near <c>int.MaxValue</c>
        /// — outside what any card-by-card play sequence can accumulate — so it is a save-boundary
        /// exposure, not a live one, but the scoping above must not be read as a guarantee this loop does
        /// not actually provide.
        /// </para>
        /// </summary>
        /// <exception cref="InvalidOperationException">This fold has already been committed, or one of
        /// the four arguments is out of range for the guard it names.</exception>
        internal int CommitWithExplicitConfig(
            DisciplineRules rules, int yellowThreshold, int accumBan, int secondYellowBan, int straightRedBan)
        {
            RequireNotCommitted();

            RequireCommittableConfig(yellowThreshold, accumBan, secondYellowBan, straightRedBan);

            for (int i = 0; i < _pending.Count; i++)
            {
                CardAttribution attribution = _pending[i];
                rules.ApplyCard(attribution.PlayerId, _competitionId, attribution.CardKind);
            }

            _committed = true;
            return _pending.Count;
        }

        /// <summary>
        /// Validates every <c>[GT]</c> a <see cref="Commit"/> could throw on — <b>without a fold</b>, so
        /// a caller can run the check BEFORE it starts mutating state a throw would strand (M17).
        /// <para>
        /// <b>Why a round-level caller needs this.</b> <see cref="CommitWithExplicitConfig"/> already
        /// makes one fixture's commit atomic (M13), but atomicity within the fixture is not
        /// recoverability of the round: <c>SeasonLoop.PlayNextRound</c> commits AFTER
        /// <c>MarkFixturePlayed</c> (M6, ERR-030-037), deliberately, so a throw here leaves the fixture
        /// marked played with its whole card list gone and the retry's unplayed-index filter skipping
        /// it — permanently. A bad <c>[GT]</c> is a property of the CONFIG, identical for every fixture
        /// in the round, so the round can ask this question once, before the first fixture is touched,
        /// and refuse with nothing written at all.
        /// </para>
        /// <para>
        /// Reads the live <see cref="DisciplineConstants"/> values, which is exactly what
        /// <see cref="Commit"/> passes — one source, two entry points, so the round-level pre-check
        /// cannot drift from the guard it front-runs.
        /// </para>
        /// </summary>
        /// <exception cref="InvalidOperationException">A bound <c>[GT]</c> is out of range for the
        /// guard it names — the same exception, from the same guards, that <see cref="Commit"/> would
        /// have thrown.</exception>
        public static void RequireCommittableConfig()
        {
            RequireCommittableConfig(
                DisciplineConstants.YellowAccumulationThreshold,
                DisciplineConstants.AccumBanMatches,
                DisciplineConstants.SecondYellowBanMatches,
                DisciplineConstants.StraightRedBanMatches);
        }

        /// <summary>
        /// The four guards, with the values taken as PARAMETERS — the L5/L11 seam
        /// <see cref="CommitWithExplicitConfig"/> exists for, shared with it so one rule has one owner.
        /// <para>
        /// Every card a fixture could have booked is one of {yellow, second-yellow, straight red} (F4
        /// already refused anything else at <see cref="ObserveTick"/>), so validating all four values
        /// up front — rather than only the one(s) a given fixture's cards happen to touch — is what
        /// makes the guard unconditional: the answer does not depend on which fold is asking, which is
        /// what lets the round ask before any fold exists.
        /// </para>
        /// <para>
        /// <b>Adding a guarded <c>[GT]</c> touches five places, not one</b> (M2): this method, the
        /// no-argument form above, <see cref="CommitWithExplicitConfig"/>'s parameter list,
        /// <see cref="DisciplineRules"/>' own site guard, and #44 §2.3 F6's guard list. Nothing makes
        /// that mechanical, so <c>DisciplineConfigCompletenessTests</c> asserts the SET of
        /// config-settable constants in <see cref="DisciplineConstants"/> — every <c>public static
        /// readonly</c> field, of any type, not only <c>int</c> (finding C) — equals the set covered
        /// here — extend the pre-flight and that test together. The eventual owner-shape is a single
        /// validated <c>DisciplineConfig</c> struct, which makes the drift impossible rather than
        /// detected; it is recorded and deliberately deferred, gated on the
        /// <c>GameplayConfigHolder.Bind</c> composition-root pass that no production caller runs yet.
        /// </para>
        /// </summary>
        internal static void RequireCommittableConfig(
            int yellowThreshold, int accumBan, int secondYellowBan, int straightRedBan)
        {
            DisciplineRules.RequireYellowThreshold(yellowThreshold);
            DisciplineRules.RequireBanLength(accumBan, nameof(DisciplineConstants.AccumBanMatches));
            DisciplineRules.RequireBanLength(
                secondYellowBan, nameof(DisciplineConstants.SecondYellowBanMatches));
            DisciplineRules.RequireBanLength(
                straightRedBan, nameof(DisciplineConstants.StraightRedBanMatches));
        }

        /// <summary>
        /// Moves the outgoing slot's occupancy to the incoming player's identity (FR-DC-005). Both ids
        /// must be mapped: the outgoing one is an on-pitch agent slot, the incoming one the engine's
        /// synthetic bench id — refused directly if either is the wrong KIND of id
        /// (<see cref="_onPitchAgentIdCount"/>, ERR-044-022), before either is even looked up. The
        /// vacated incoming slot is cleared (M1) so no player id maps to two agent ids once this
        /// returns.
        /// </summary>
        private void ApplySubstitution(int outgoingAgentId, int incomingAgentId)
        {
            // M1: an id cannot be both outgoing and incoming in the same substitution. Without this
            // guard the write below (outgoing <- incomingPlayerId) and the clear below it (incoming <-
            // NO_PLAYER) would target the SAME index, and the clear would erase the write an instant
            // after it landed — silently losing a real player's occupancy rather than moving it.
            if (outgoingAgentId == incomingAgentId)
            {
                throw new InvalidOperationException(
                    "CardLedgerFold: SubstitutionEvent.Outgoing and .Incoming are both agent id " +
                    outgoingAgentId + " — a substitution moves one player OFF an agent id and a " +
                    "DIFFERENT player ON to it; the same id cannot be both (M1).");
            }

            // ERR-044-022 (reviewed findings pass): Incoming must be a BENCH id and Outgoing must be an
            // ON-PITCH id. Before this pair of guards existed, a malformed record naming an ON-PITCH
            // Incoming (Appendix C's own "slot 19" shape, ERR-044-001) reached the write below
            // unchecked — e.g. Sub(Outgoing=5, Incoming=6) with 6 an occupied on-pitch slot: the write
            // overwrote slot 5's mapping with slot 6's occupant, and the vacated-slot clear then erased
            // slot 6's own mapping, so slot 5's PRIOR occupant lost his mapping entirely and a card at
            // slot 5 landed on the wrong player. M1's seed-injectivity check cannot catch this — it
            // runs once, at construction, over player ids and never learns which agent ids are on-pitch
            // versus bench; only this boundary can.
            if (incomingAgentId < _onPitchAgentIdCount)
            {
                throw new InvalidOperationException(
                    "CardLedgerFold: SubstitutionEvent.Incoming = " + incomingAgentId + " is an " +
                    "ON-PITCH agent id (< " + _onPitchAgentIdCount + ") — an on-pitch agent id cannot " +
                    "come ON (ERR-044-022). Only the engine's synthetic bench ids may be Incoming.");
            }
            if (outgoingAgentId >= _onPitchAgentIdCount)
            {
                throw new InvalidOperationException(
                    "CardLedgerFold: SubstitutionEvent.Outgoing = " + outgoingAgentId + " is not an " +
                    "on-pitch agent id (>= " + _onPitchAgentIdCount + ") — only an on-pitch agent id " +
                    "can go OFF (ERR-044-022).");
            }

            int incomingPlayerId = OccupantOf(incomingAgentId, "SubstitutionEvent.Incoming");

            // Read the outgoing slot too, purely so an unmapped OUTGOING id fails loud here rather than
            // silently writing a player into a slot the seed never described.
            OccupantOf(outgoingAgentId, "SubstitutionEvent.Outgoing");

            _occupancy[outgoingAgentId] = incomingPlayerId;

            // M1 (reviewed findings pass): clear the vacated incoming slot. Left mapped, incomingPlayerId
            // would occupy TWO agent ids after this swap — a card naming either attributes to him, which
            // is silent double-booking when a malformed record later names the stale incoming id
            // (Appendix C's own "slot 19" shape, ERR-044-001).
            //
            // CORRECTED (ERR-044-022, reviewed findings pass): this comment originally claimed the
            // clear alone made that double-booking "impossible rather than merely unlikely". That was
            // FALSE as written — it only closed the narrow shape where Incoming was genuinely a bench
            // id; it did nothing to stop Incoming naming an ON-PITCH id with a live occupant, which the
            // two guards above now refuse outright before this write or this clear ever runs. With
            // those guards in place the claim is finally true: OccupantOf refuses NO_PLAYER, so any
            // later record naming this now-vacated agent id throws F1 instead of silently attributing
            // to whoever was just subbed on.
            _occupancy[incomingAgentId] = NO_PLAYER;
        }

        private int OccupantOf(int agentId, string what)
        {
            if ((uint)agentId >= (uint)_occupancy.Length || _occupancy[agentId] == NO_PLAYER)
            {
                throw new InvalidOperationException(
                    "CardLedgerFold: " + what + " = " + agentId + " has no occupancy mapping (F1). The " +
                    "lineup seed covers agent ids [0, " + _occupancy.Length + ") and must include the " +
                    "engine's synthetic bench ids as well as its on-pitch slots. Attributing this card " +
                    "to a guess would put a booking on the wrong player's record, permanently.");
            }
            return _occupancy[agentId];
        }

        private static void RequireKnownCardKind(byte cardKind)
        {
            if (cardKind != DisciplineConstants.CardKindYellow
                && cardKind != DisciplineConstants.CardKindRed
                && cardKind != DisciplineConstants.CardKindSecondYellow)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cardKind), cardKind,
                    "CardLedgerFold: CardIssuedEvent.CardKind must be 0 (yellow), 1 (red) or 2 (second " +
                    "yellow) — F4. Refused at the tap rather than at Commit so the failure names the " +
                    "tick's record, not a buffered copy of it.");
            }
        }

        private void RequireNotCommitted()
        {
            if (_committed)
            {
                throw new InvalidOperationException(
                    "CardLedgerFold: this fixture's fold has already been committed. A fold is per " +
                    "fixture and commits exactly once (FR-DC-010); reusing one would double its cards.");
            }
        }

        /// <summary>One card, already attributed to a player — the buffered form held until <see cref="Commit"/>.</summary>
        private readonly struct CardAttribution
        {
            internal readonly int PlayerId;
            internal readonly byte CardKind;

            internal CardAttribution(int playerId, byte cardKind)
            {
                PlayerId = playerId;
                CardKind = cardKind;
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-08-13 | —      | Initial implementation (#44 T2, roadmap C2): the §3.1 occupancy  |
// |         |            |        | fold, buffered per fixture and committed once at resolution so   |
// |         |            |        | no half-fixture tally can reach a save (the mid-fixture restore  |
// |         |            |        | hazard the council flagged).                                     |
// | 1.1     | 2026-08-13 | —      | AR fix (L2): corrected the overclaim that buffering "keeps this  |
// |         |            |        | correct when the seam opens" — the fold itself is not serialized,|
// |         |            |        | so a mid-fixture restore loses every pre-save card outright.      |
// |         |            |        | Buffering keeps a half-fixture tally OUT of persisted state;      |
// |         |            |        | correctness additionally needs the pending list serialized.      |
// | 1.2     | 2026-08-13 | —      | AR round 3 fix (M13): Commit is now atomic. It previously applied |
// |         |            |        | cards one at a time through DisciplineRules.ApplyCard, which is   |
// |         |            |        | fallible under a bound config — a throw on card k left cards      |
// |         |            |        | 0..k-1 already written while the fold, carrying card k onward,    |
// |         |            |        | was discarded uncommitted by the caller, exactly the half-fixture |
// |         |            |        | tally buffering exists to prevent, and M6 made the consequence    |
// |         |            |        | PERMANENT (the fixture is already marked played). Fixed by        |
// |         |            |        | extracting the real body to internal CommitWithExplicitConfig,    |
// |         |            |        | which validates RequireYellowThreshold + the three RequireBanLength|
// |         |            |        | guards ONCE before the loop; Commit(rules) passes the real         |
// |         |            |        | DisciplineConstants values through it. Locked by                  |
// |         |            |        | CardLedgerFoldTests' two new atomicity tests, which drive the     |
// |         |            |        | guard through an explicit bad value since DisciplineConstants'    |
// |         |            |        | fields cannot be rebound in this process (the same L5/L11 seam).  |
// | 1.3     | 2026-08-13 | —      | AR round 4 fix (M17): the four [GT] guards move into a shared      |
// |         |            |        | RequireCommittableConfig — a public no-argument form reading the   |
// |         |            |        | live DisciplineConstants, and the internal four-argument form      |
// |         |            |        | CommitWithExplicitConfig now delegates to. Commit's atomicity      |
// |         |            |        | (v1.2) stops a HALF-applied fixture but not a LOST one: M6 puts    |
// |         |            |        | the commit after MarkFixturePlayed, so a config refusal there      |
// |         |            |        | strands the round permanently and loses the fixture's whole card   |
// |         |            |        | list. A bad [GT] is a property of the config, identical for every  |
// |         |            |        | fixture, so SeasonLoop.PlayNextRound now asks once before the      |
// |         |            |        | fixture loop. No behaviour change to Commit itself: same guards,   |
// |         |            |        | same order, same values, one owner.                               |
// | 1.4     | 2026-08-13 | —      | AR round 5 fix (L17): DisciplineConstants.CARD_KIND_YELLOW/RED/    |
// |         |            |        | SECOND_YELLOW renamed CardKindYellow/Red/SecondYellow ([FIXED] ->  |
// |         |            |        | [CROSS] — they are #17 CardIssuedEvent.CardKind domain ordinals    |
// |         |            |        | #44 consumes read-only). RequireKnownCardKind's three comparisons  |
// |         |            |        | updated to match; no behaviour change.                             |
// | 1.5     | 2026-08-15 | —      | AR round 5 fixes (L19, L22), comments only — no behaviour change.  |
// |         |            |        | L19: the type remark asserted "that save path is refused today     |
// |         |            |        | (SeasonSaveManager declines a live ActiveMatch)". It declines      |
// |         |            |        | nothing of the sort — it documents that SeasonLoop.ActiveMatch is  |
// |         |            |        | not a safe source and asks the caller to pass its own engine.      |
// |         |            |        | The L2 buffering argument rested on that false premise. Replaced   |
// |         |            |        | with the true reason: no seam exposes a mid-fixture engine at all, |
// |         |            |        | so this is unreachability, not a gate. L22: ObserveTick's          |
// |         |            |        | correctness depends on MatchEngine.RunResolvePhase flushing        |
// |         |            |        | queued SubstitutionEvents BEFORE issuing cards in the same phase   |
// |         |            |        | (SubstitutePlayer swaps the slot between ticks and queues the      |
// |         |            |        | event). If that flush ever moves after card issuance, every card   |
// |         |            |        | in the substitution tick is attributed to the OUTGOING player,     |
// |         |            |        | silently — and Stage 0 fields a fixed eleven, so nothing would     |
// |         |            |        | catch it. Dependency now stated at the site plus a fold test       |
// |         |            |        | (CardLedgerFoldTests) authoring Sub-then-Card in one tick.         |
// | 1.6     | 2026-08-15, later | — | Reviewed findings pass, L3/L5. L3: the L22 comment inside       |
// |         |            |        | ObserveTick was spliced into the middle of a sentence — closed     |
// |         |            |        | before the L22 paragraph instead of resuming after it, comment     |
// |         |            |        | text unchanged otherwise. L5: NO_PLAYER's XML doc tagged [FIXED]   |
// |         |            |        | (FR-CS-060/061); a #44 Appendix A row + spec declaration owed,     |
// |         |            |        | not filed here. No behaviour change either way.                    |
// | 1.7     | 2026-08-16 | —      | Adversarial review, M2 (minimal fix), doc only.                   |
// |         |            |        | RequireCommittableConfig(int,int,int,int) records that a new      |
// |         |            |        | guarded [GT] must reach five places — both forms here,            |
// |         |            |        | CommitWithExplicitConfig's parameters, DisciplineRules' site      |
// |         |            |        | guard and #44 §2.3 F6's list — and names the new                  |
// |         |            |        | DisciplineConfigCompletenessTests, which asserts the guarded set  |
// |         |            |        | still equals DisciplineConstants' config-settable set. M2's       |
// |         |            |        | fuller DisciplineConfig restructure (one validated struct, drift  |
// |         |            |        | impossible rather than detected) is recorded as the eventual      |
// |         |            |        | owner-shape, gated on the GameplayConfigHolder.Bind composition-  |
// |         |            |        | root pass no production caller runs yet. No behaviour change.     |
// | 1.8     | 2026-08-16, later | — | Reviewed findings pass (M1/M3/L2). M1: ApplySubstitution now  |
// |         |            |        | clears _occupancy[incomingAgentId] after the swap and refuses     |
// |         |            |        | outgoing == incoming; the constructor loop refuses a seed that    |
// |         |            |        | maps one player id to two agent ids. Both close the injectivity   |
// |         |            |        | hole the finding names — a later record naming the vacated slot   |
// |         |            |        | now throws F1 instead of silently double-booking. M3: ObserveTick |
// |         |            |        | now refuses a non-consecutive tap.CurrentTick and latches _faulted|
// |         |            |        | shut on a part-way failure, mirroring #37 MatchAnalyticsAggregator|
// |         |            |        | verbatim; IDisciplineTickLedgerTap gained CurrentTick, forwarded   |
// |         |            |        | by MatchEngineDisciplineTap from MatchEngine's already-public      |
// |         |            |        | clock (no MatchEngine.cs change). L2: CommitWithExplicitConfig's  |
// |         |            |        | doc now scopes its "all-or-nothing" claim to the four             |
// |         |            |        | RequireCommittableConfig guards and names the still-open          |
// |         |            |        | accumulator-overflow gap (DisciplineEntry.YellowsPlusOne/         |
// |         |            |        | BanMatchesPlus exist to close it; wiring DisciplineRules to call  |
// |         |            |        | them is outside this file's ownership for this pass — see         |
// |         |            |        | DisciplineEntry.cs v1.3). Behaviour change: a substitution whose  |
// |         |            |        | Incoming names an already-occupied on-pitch slot, a self-         |
// |         |            |        | colliding substitution, and a doubly-mapped construction seed now |
// |         |            |        | throw instead of silently corrupting occupancy; a skipped or      |
// |         |            |        | out-of-order ObserveTick call now throws instead of silently      |
// |         |            |        | losing cards.                                                      |
// |         |            |        | **CORRECTION 2026-08-16, latest again (ERR-044-022, reviewed      |
// |         |            |        | findings pass):** this row's claim that "a substitution whose     |
// |         |            |        | Incoming names an already-occupied on-pitch slot ... now throw"   |
// |         |            |        | was FALSE when written. The M1 fix above closed only the seed's   |
// |         |            |        | one-to-one CHECK (at construction) and the vacated-BENCH-slot     |
// |         |            |        | clear (at ApplySubstitution) — neither one asks whether Incoming  |
// |         |            |        | is actually a bench id. Sub(Outgoing=5, Incoming=6) with 6 an     |
// |         |            |        | occupied ON-PITCH slot did NOT throw at v1.8: it silently         |
// |         |            |        | destroyed slot 5's prior occupant's mapping and misattributed his |
// |         |            |        | cards to slot 6's occupant. TRUE as of v1.10 below, which adds    |
// |         |            |        | the onPitchAgentIdCount boundary this row's claim was missing.    |
// | 1.9     | 2026-08-16, latest | — | Findings A and C, doc only. A: CommitWithExplicitConfig's  |
// |         |            |        | L2 doc corrected: the helpers ARE wired now (DisciplineRules.cs   |
// |         |            |        | v1.9/v1.10), so the "outside this file's ownership for this pass" |
// |         |            |        | line is stale. The residual is restated, not removed — Commit      |
// |         |            |        | still applies cards one at a time, so a mid-list OverflowException |
// |         |            |        | (now loud and correctly named) still leaves cards 0..k-1 applied  |
// |         |            |        | and _committed false. C: RequireCommittableConfig's M2 remark now |
// |         |            |        | says the completeness lock covers every public static readonly    |
// |         |            |        | field of ANY type, not only int, matching                         |
// |         |            |        | DisciplineConfigCompletenessTests.cs v1.1. No code change either   |
// |         |            |        | way.                                                                |
// | 1.10    | 2026-08-16, latest again | — | Reviewed findings pass, findings A and B. A        |
// |         |            |        | (ERR-044-022): the constructor gains a required onPitchAgentIdCount|
// |         |            |        | parameter (0 < onPitchAgentIdCount <= the seed's length);          |
// |         |            |        | ApplySubstitution refuses an Incoming below that boundary (an      |
// |         |            |        | on-pitch id cannot come on) and an Outgoing at or past it (only an |
// |         |            |        | on-pitch id can go off). Closes the hole M1 (v1.8) could not see:  |
// |         |            |        | the seed's one-to-one check knows only player ids, never which     |
// |         |            |        | agent ids are on-pitch versus bench. The v1.8 row above is         |
// |         |            |        | annotated in place (not rewritten) with the dated correction; the  |
// |         |            |        | inline "impossible rather than merely unlikely" comment at         |
// |         |            |        | ApplySubstitution is corrected the same way — false when written,  |
// |         |            |        | true now that these two guards exist. B (ERR-044-023, doc only):   |
// |         |            |        | the constructor's XML doc and the type remarks now state the       |
// |         |            |        | boot-time precondition on the seed explicitly — MatchEngine.       |
// |         |            |        | PlayerIdsByAgentId is one-to-one over non-sentinel entries only AT |
// |         |            |        | BOOT, since SubstitutePlayer never clears the incoming player's    |
// |         |            |        | bench-origin entry — matching the corrected MatchEngine.cs XML doc |
// |         |            |        | and the new SeasonLoopDisciplineTests cross-assembly lock. New     |
// |         |            |        | CardLedgerFoldTests locks: Sub with an occupied on-pitch Incoming  |
// |         |            |        | throws; Outgoing >= onPitchAgentIdCount throws.                   |
// | 1.11    | 2026-08-16, latest of all | — | L-1 and M-B (adversarial review), doc only. L-1:  |
// |         |            |        | NO_PLAYER's doc said a spec declaration "is owed" against #44 —    |
// |         |            |        | ERR-044-013 discharged it 2026-08-15 (section-2.md v0.9 +          |
// |         |            |        | appendices v0.8); added a dated discharge note in place of the     |
// |         |            |        | stale "owed" claim. M-B: the constructor's onPitchAgentIdCount     |
// |         |            |        | ArgumentOutOfRangeException doc now cites the new                  |
// |         |            |        | CardLedgerFoldTests.Constructor_OnPitchAgentIdCountOutOfRange_     |
// |         |            |        | Throws (T-DC-FOLD-003) as its lock, giving the spec's own          |
// |         |            |        | section-5 citation (owned by another fixer) an anchor to point at. |
// |         |            |        | No behaviour change.                                               |
#endregion
