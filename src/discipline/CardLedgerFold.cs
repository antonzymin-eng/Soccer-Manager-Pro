// File:     src/discipline/CardLedgerFold.cs
// Created:  2026-08-13
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
//           Code Standards #20
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
    /// <b>Recorded — the substitution branch has no production driver.</b> <c>SeasonLoop</c> never calls
    /// <c>SubstitutePlayer</c> (Stage 0 fields a fixed eleven), so occupancy never actually changes on
    /// the season path. That is precisely the shape that shipped <c>BootFixtureEngine</c> unrun for
    /// months, so the branch is driven from authored record lists in the suite rather than from a
    /// contrived match — a test that cannot reach a branch does not cover it.
    /// </para>
    /// </summary>
    public sealed class CardLedgerFold
    {
        /// <summary>[FIXED] Occupancy sentinel: this agent id maps to no player (an unused slot in the
        /// seed). Reviewed findings pass, L5, 2026-08-15: this constant carried no tag, violating
        /// FR-CS-060/061 — a spec declaration and Appendix A row are owed against #44 and are reported,
        /// not filed here (spec-error-log.md is out of this pass's ownership).</summary>
        public const int NO_PLAYER = -1;

        // Indexed by agent id: on-pitch slots first, then the engine's synthetic bench ids. Mutable —
        // a substitution moves the occupant of the OUTGOING slot to the incoming player's identity.
        private readonly int[] _occupancy;
        private readonly int _competitionId;

        // The fixture's cards in observation order, buffered until Commit. Order is the bus's canonical
        // publish order (tick, then intra-phase), which is what makes FR-DC-021 hold: the same fixture
        // always yields the same list, so it always yields the same tally.
        private readonly List<CardAttribution> _pending = new List<CardAttribution>();

        private bool _committed;

        /// <summary>
        /// Seeds the fold with the fixture's full agent-id → <c>PlayerId</c> map — <b>starters and
        /// bench</b>, because a card can be shown to a player who came on.
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
        /// <param name="competitionId">The competition partition these cards accrue in (FR-DC-012).</param>
        /// <exception cref="ArgumentNullException"><paramref name="occupancyByAgentId"/> is null.</exception>
        /// <exception cref="ArgumentException">The seed is empty, or carries a negative player id that
        /// is not <see cref="NO_PLAYER"/>.</exception>
        public CardLedgerFold(int[] occupancyByAgentId, int competitionId)
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

            _occupancy = new int[occupancyByAgentId.Length];
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
        /// <exception cref="InvalidOperationException">The fixture has already been committed, or a
        /// record names an agent slot with no occupancy mapping (<b>F1</b> — the lineup seed is
        /// incomplete, a root-contract bug whose silent form is misattribution).</exception>
        /// <exception cref="ArgumentOutOfRangeException">A <c>CardKind</c> outside <c>{0, 1, 2}</c>
        /// (<b>F4</b>).</exception>
        public void ObserveTick(IDisciplineTickLedgerTap tap)
        {
            if (tap == null)
            {
                throw new ArgumentNullException(nameof(tap));
            }
            RequireNotCommitted();

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
        /// synthetic bench id.
        /// </summary>
        private void ApplySubstitution(int outgoingAgentId, int incomingAgentId)
        {
            int incomingPlayerId = OccupantOf(incomingAgentId, "SubstitutionEvent.Incoming");

            // Read the outgoing slot too, purely so an unmapped OUTGOING id fails loud here rather than
            // silently writing a player into a slot the seed never described.
            OccupantOf(outgoingAgentId, "SubstitutionEvent.Outgoing");

            _occupancy[outgoingAgentId] = incomingPlayerId;
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
#endregion
