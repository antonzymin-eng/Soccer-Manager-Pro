// File:     src/discipline/CardLedgerFold.cs
// Created:  2026-08-13
// Modified: 2026-08-13
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
    /// slot state. That save path is refused today (<c>SeasonSaveManager</c> declines a live
    /// <c>ActiveMatch</c>), but <c>MatchSession.TickOnce/CaptureSave/RestoreFrom</c> already exists one
    /// assembly over, so the buffer is what keeps this correct when the seam opens rather than a
    /// second thing to remember then.
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
        /// <summary>Occupancy sentinel: this agent id maps to no player (an unused slot in the seed).</summary>
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
        /// commit would double every card in the fixture.</exception>
        public int Commit(DisciplineRules rules)
        {
            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }
            RequireNotCommitted();

            for (int i = 0; i < _pending.Count; i++)
            {
                CardAttribution attribution = _pending[i];
                rules.ApplyCard(attribution.PlayerId, _competitionId, attribution.CardKind);
            }

            _committed = true;
            return _pending.Count;
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
            if (cardKind != DisciplineConstants.CARD_KIND_YELLOW
                && cardKind != DisciplineConstants.CARD_KIND_RED
                && cardKind != DisciplineConstants.CARD_KIND_SECOND_YELLOW)
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
#endregion
