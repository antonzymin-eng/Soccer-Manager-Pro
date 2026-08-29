// File:     src/event-system/EventSystemConstants.cs
// Created:  2026-05-30
// Modified: 2026-08-15, later still (reviewed findings pass, L1 — corrected the v1.6 entry directly
//           below: MatchEngine.cs's foulOrdinal: 0xFFFF literal call site WAS repointed, by the SAME
//           DAY's later "#44 AR round 5, L3" pass (MatchEngine.cs v1.71, MatchEngine.cs:5286) — so
//           "NOT repointed / still needs repointing / open call site" below is SUPERSEDED, not true of
//           the tree today. Left in place per this repo's annotate-in-place convention — v1.7)
// Modified: 2026-08-15, later (reviewed-findings pass, L3 — added FOUL_ORDINAL_NONE = 0xFFFF to
//           #region Fixed beside the CARD_KIND_* rows: CardIssuedEvent.FoulOrdinal's "no associated
//           foul" sentinel, previously prose-only in CardIssuedEvent.cs — ERR-017-004's exact defect
//           shape recurring on the sibling payload field. Mirrored [CROSS] in MatchEngineConstants;
//           the one production call site (MatchEngine.cs's foulOrdinal: 0xFFFF literal) is outside
//           this pass's ownership and still needs repointing — v1.6) — SUPERSEDED, see the entry above.
// Modified: 2026-08-15 (#44 C1/C2 adversarial review round 4, M24/ERR-017-004 — added a new #region
//           Fixed (first region in the file) with CARD_KIND_YELLOW/CARD_KIND_RED/
//           CARD_KIND_SECOND_YELLOW, ALL_CAPS [FIXED]: the CardIssuedEvent.CardKind domain-ordinal
//           encoding, the single authoritative declaration MatchEngineConstants and
//           DisciplineConstants now mirror [CROSS] (PascalCase) from — v1.5)
// Author:   —
// Spec:     Event System #17 §3.10, Code Standards #20
// Purpose:  All constants for the event system. Region order: Fixed → GT → Cross (Fixed added
//           2026-08-15; GT → Cross was, and remains, this file's pre-existing order — src/CLAUDE.md's
//           canonical Fixed → Derived → Cross → GT is not fully adopted here and is not in this
//           pass's scope).
//           No magic literals permitted in any other event-system file.

using TacticalDirector.DeterministicSim;
using static TacticalDirector.ProjectConstants.GameplayConfigHolder;

namespace TacticalDirector.EventSystem
{
    /// <summary>
    /// All constants for Event System #17. §3.10.
    /// GT sub-classes per §3.10 note: runtime-tunable (queue sizes, depth) vs
    /// design-fixed (ordinal width, error codes) — the latter are locked at approval.
    /// </summary>
    public static class EventSystemConstants
    {
        #region Fixed

        /// <summary>[FIXED] <c>CardIssuedEvent.CardKind</c> domain ordinal for a first (or non-promoting)
        /// caution. Appendix A row 0x06 / §3.10: "Card kind: 0=Yellow, 1=Red, 2=SecondYellow (domain
        /// ordinal)". A wire-format decision, not a physics law, but `[GT]` is the wrong tag too: this
        /// is not designer-tunable at all — it is the byte a producer (match-engine) writes and a
        /// consumer (discipline) reads, and changing it after publication breaks the `CardIssuedEvent`
        /// payload contract both already agree on. `[FIXED]` per the root `CLAUDE.md` tag table.
        /// <para>
        /// <b>ERR-017-004.</b> Before this constant existed, the three-value encoding was declared
        /// independently in <c>MatchEngineConstants</c> (tagged <c>[FIXED]</c>) and
        /// <c>DisciplineConstants</c> (tagged <c>[CROSS]</c>, but citing this row's prose rather than
        /// a bound symbol) — two catalogues, two tags, neither actually pointing at #17, the spec that
        /// owns the encoding (Appendix A: "#17 (default owner)"). All three consumers — those two plus
        /// <c>MatchAnalyticsConstants</c> — now mirror this constant directly (the owning-catalogue
        /// carve-out, ERR-020-004, src/CLAUDE.md §"[CROSS] mirrors" — corrected here, reviewed findings
        /// pass, L2, 2026-08-15: this paragraph previously said "Both … single-consumer routing", which
        /// was wrong on the count (three consumers, not two) and wrong on the clause — the carve-out
        /// applies regardless of consumer count).
        /// </para>
        /// </summary>
        public const byte CARD_KIND_YELLOW = 0;

        /// <summary>[FIXED] <c>CardIssuedEvent.CardKind</c> domain ordinal for a straight red. #17
        /// Appendix A row 0x06 / §3.10, as <see cref="CARD_KIND_YELLOW"/>. ERR-017-004.</summary>
        public const byte CARD_KIND_RED = 1;

        /// <summary>[FIXED] <c>CardIssuedEvent.CardKind</c> domain ordinal for a second caution promoted
        /// to a dismissal — the producer (<c>MatchEngine.ApplyCardAndCheckSentOff</c>) emits this as ONE
        /// event, never a yellow-then-red pair. #17 Appendix A row 0x06 / §3.10, as
        /// <see cref="CARD_KIND_YELLOW"/>. ERR-017-004.</summary>
        public const byte CARD_KIND_SECOND_YELLOW = 2;

        /// <summary>[FIXED] <c>CardIssuedEvent.FoulOrdinal</c> sentinel for "procedural card, no
        /// associated <c>FoulCommittedEvent</c>". Appendix A row 0x06 / §3.10. Widened from <c>0xFF</c>
        /// alongside the field's <c>byte</c>→<c>ushort</c> widening (AR-5 L-1, <c>CardIssuedEvent.cs</c>
        /// v1.2, 2026-06-02) — same wire-format reasoning as <see cref="CARD_KIND_YELLOW"/>: a value a
        /// producer (match-engine) writes and a consumer (discipline) reads, not designer-tunable.
        /// <para>
        /// <b>L3 (reviewed-findings pass, 2026-08-15).</b> Had no catalogue home anywhere in this spec —
        /// it existed only as prose in <c>CardIssuedEvent.cs</c> — the exact ERR-017-004 defect shape
        /// recurring on the sibling payload field of the same event, in the same statement that fix
        /// rewrote to remove the bare card-kind literals (FR-CS-016).
        /// </para>
        /// </summary>
        public const ushort FOUL_ORDINAL_NONE = 0xFFFF;

        #endregion

        #region GT

        // ── Runtime-tunable [GT] ────────────────────────────────────────────────────────

        /// <summary>[GT] Ring-buffer slot count per tick. §3.5.1 / §6.3.
        /// Derivation: 64 first-order ceiling × MAX_EVENT_DISPATCH_DEPTH (8) × 2 headroom = 1024
        /// (additive BFS under FR-EVT-046a out-degree cap = 1).</summary>
        public static readonly int EventQueueCapacity = Config.GetInt("event-system", "EventQueueCapacity", 1024);

        /// <summary>[GT] Aggregate per-tick Tier C publication sanity ceiling. §3.5.3 / §6.3.
        /// NOT a delivery queue capacity — Tier C is immediate-dispatch per §3.2.3.
        /// AR-12 L-1: declared-but-unconsumed at Stage 0 — the per-ordinal drop predicate
        /// (FR-EVT-043) is the only Tier C cap enforced at Stage 0. The cross-ordinal aggregate
        /// ceiling activates at Stage 0+1 alongside the FR-EVT-045 dropped-publish trace channel.</summary>
        public static readonly int CosmeticPerTickPublicationBudget = Config.GetInt("event-system", "CosmeticPerTickPublicationBudget", 4096);

        /// <summary>[GT] Maximum second-order Tier A/B BFS dispatch depth per DrainTick. §3.2.5.</summary>
        public static readonly int MaxEventDispatchDepth = Config.GetInt("event-system", "MaxEventDispatchDepth", 8);

        /// <summary>[GT] Maximum Tier A/B subscriber handlers per event type. Revisited at Stage 0+1 measurements.</summary>
        public static readonly int MaxHandlersPerEventType = Config.GetInt("event-system", "MaxHandlersPerEventType", 32);

        /// <summary>[GT] Maximum Tier C subscriber handlers per event type. §4.3.2.</summary>
        public static readonly int MaxTierCHandlersPerType = Config.GetInt("event-system", "MaxTierCHandlersPerType", 64);

        /// <summary>[GT] Maximum bytes per ring-buffer slot (12-byte header + up to MaxEventSlotBytes-12 bytes payload).
        /// Sized to accommodate the largest registered event struct: HeaderExecutedEvent and DecisionMadeEvent (136 bytes each).
        /// AR-2 H-1/H-3: was 128 — caused ring-buffer overrun and stackalloc slice crash. §3.5.1.</summary>
        public static readonly int MaxEventSlotBytes = Config.GetInt("event-system", "MaxEventSlotBytes", 160);

        // ── Design-fixed [GT] — locked at approval; NOT runtime-tunable per §3.10 sub-class note ────

        /// <summary>[GT] Event type ordinal namespace width in bytes. §3.1.2. Stage 5+ expansion to 2 bytes per D5 §7.5.</summary>
        public const int EventTypeOrdinalWidth = 1;

        /// <summary>[GT] Payload version field width in bytes. §3.1 / §3.7.
        /// AR-12 L-1: declared-but-unconsumed at Stage 0 — the version byte itself is written
        /// from <see cref="EventRegistry.GetVersion"/> at header offset 1. This width constant
        /// feeds the Stage 5+ canonical-layout / §7.5 D5 ordinal-expansion documentation.</summary>
        public const int PayloadVersionWidth = 1;

        /// <summary>[GT] Fixed 12-byte event header size (per §2.4.1 struct skeleton).
        /// 1+1+2+4+2+2 = eventTypeOrdinal+payloadVersion+_reserved+tick+subsystemOrdinal+intraPhaseDrawIndex.</summary>
        public const int EventHeaderBytes = 12;

        /// <summary>[FIXED] Compile-time upper bound on the per-tick sort-index stackalloc used
        /// by EventLedger.DrainTick and EventLedger.SerializeLedger. AR-9 M-1: caps the worst-case
        /// stack footprint at 8 KB (2048 ints × 4 bytes) so a future Stage 1 config-loader bump
        /// of EventQueueCapacity cannot quietly grow the stackalloc into StackOverflowException
        /// territory inside the tick pipeline. EventLedger's static constructor enforces
        /// EventQueueCapacity &lt;= MAX_QUEUE_SORT_INTS at boot. §3.5.1 / FR-EVT-049/050.</summary>
        public const int MAX_QUEUE_SORT_INTS = 2048;

        // ── Error codes — design-fixed [GT]; 0x17NN reserved block; must NOT collide with #16's 0x16NN ──

        /// <summary>[GT] Tier A/B ring-buffer overflow, OR BFS dispatch depth exceeded, OR per-handler out-degree exceeded.
        /// §2.5 / §3.6.1 / §3.2.5. EC-017-002 / EC-017-006.</summary>
        public const ushort ErrEvtQueueOverflow = 0x1701;

        // 0x1702 reserved; tier-marker mismatch is compile-time only (FR-EVT-016, FR-EVT-076).

        /// <summary>[GT] Fixture load: eventTypeOrdinal not in Appendix A registry. §2.5 / §3.7.2. EC-017-003.</summary>
        public const ushort ErrEvtOrdinalUnknown = 0x1703;

        /// <summary>[GT] Fixture load: payloadVersion newer than current registry row. §2.5 / §3.7.2. EC-017-004.</summary>
        public const ushort ErrEvtVersionIncompatible = 0x1704;

        /// <summary>[GT] Runtime register/unregister of Tier A/B subscriber after boot phase ended. §2.5 / §3.2.2. EC-017-005b.</summary>
        public const ushort ErrEvtRegistrationPhase = 0x1705;

        /// <summary>[GT] Publish or subscribe before the owning spec's EventBusRegistrar.Initialize() was called —
        /// EventOrdinalCache&lt;T&gt;.Ordinal is still 0 (CLR default). §2.5 / FR-EVT-020. AR-2 M-1.</summary>
        public const ushort ErrEvtUnregisteredOrdinal = 0x1706;

        /// <summary>[GT] Ordinal collision: two distinct IEventC types mapped to the same ordinal via
        /// erroneous RegisterExternalRow calls. Thrown by CosmeticChannel.Subscribe when the existing
        /// dispatcher at s_dispatchers[ordinal] is typed for a different event type. AR-4 L.</summary>
        public const ushort ErrEvtOrdinalCollision = 0x1707;

        // ── Diagnostic message prefixes (derived from the error codes above) ───────────────
        // AR-12 M-1: throw sites previously hardcoded the hex (e.g. "(0x1701)") inside their
        // message literals, duplicating the values declared above — a code retune would leave
        // the diagnostic strings stale. These prefixes derive the rendered "0xNNNN" text from
        // the ushort constants so the codes are the single source of truth. Rendered text is
        // byte-identical to the prior literals (X4 of 0x1701 = "1701"), so existing message
        // substring assertions (e.g. Contains("0x1701")) are unaffected.

        /// <summary>[GT] Rendered prefix for <see cref="ErrEvtQueueOverflow"/> diagnostics.</summary>
        internal static readonly string ErrPrefixQueueOverflow =
            "ERR_EVT_QUEUE_OVERFLOW (0x" + ErrEvtQueueOverflow.ToString("X4") + ")";

        /// <summary>[GT] Rendered prefix for <see cref="ErrEvtRegistrationPhase"/> diagnostics.</summary>
        internal static readonly string ErrPrefixRegistrationPhase =
            "ERR_EVT_REGISTRATION_PHASE (0x" + ErrEvtRegistrationPhase.ToString("X4") + ")";

        /// <summary>[GT] Rendered prefix for <see cref="ErrEvtUnregisteredOrdinal"/> diagnostics.</summary>
        internal static readonly string ErrPrefixUnregisteredOrdinal =
            "ERR_EVT_UNREGISTERED_ORDINAL (0x" + ErrEvtUnregisteredOrdinal.ToString("X4") + ")";

        /// <summary>[GT] Rendered prefix for <see cref="ErrEvtOrdinalCollision"/> diagnostics.</summary>
        internal static readonly string ErrPrefixOrdinalCollision =
            "ERR_EVT_ORDINAL_COLLISION (0x" + ErrEvtOrdinalCollision.ToString("X4") + ")";

        #endregion

        #region Cross

        /// <summary>
        /// [CROSS] Domain tag prefixed to EventLedgerRecord before SerializeCanonical (FM-017-001). §3.4.2.
        /// Authoritative source: DeterministicSimConstants.DOMAIN_TAG_EVENT_LEDGER.
        /// Deterministic Simulation #16 §3.4 v1.0.1. Value: 0x15. ERR-017-001 RESOLVED May 14, 2026.
        /// </summary>
        public static readonly byte DomainTagEventLedger = DeterministicSimConstants.DOMAIN_TAG_EVENT_LEDGER;

        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                      |
// | 1.0     | 2026-05-30 | —      | Initial implementation.                                                    |
// | 1.1     | 2026-05-30 | —      | AR-2 fix: MaxEventSlotBytes 128→160 (AR-2 H-1/H-3: HeaderExecutedEvent     |
// |         |            |        | 136 bytes + DecisionMadeEvent 136 bytes exceeded 128-byte slot, causing     |
// |         |            |        | ring-buffer overrun and CosmeticChannel stackalloc slice crash).            |
// |         |            |        | Added ErrEvtUnregisteredOrdinal (0x1706) for AR-2 M-1 unregistered-ordinal |
// |         |            |        | throws in EventBus and CosmeticChannel.                                     |
// | 1.2     | 2026-05-31 | —      | AR-4 L: added ErrEvtOrdinalCollision (0x1707) for CosmeticChannel.         |
// |         |            |        | Subscribe diagnostic when two IEventC types share an ordinal (erroneous    |
// |         |            |        | duplicate RegisterExternalRow calls) — replaces hard InvalidCastException.  |
// | 1.3     | 2026-06-07 | —      | AR-9 M-1: added MAX_QUEUE_SORT_INTS = 2048 compile-time const. Caps the   |
// |         |            |        | stackalloc footprint of EventLedger.DrainTick / SerializeLedger so a       |
// |         |            |        | Stage 1 config-loader bump of EventQueueCapacity cannot grow the           |
// |         |            |        | stackalloc into StackOverflowException range inside the tick pipeline.    |
// | 1.4     | 2026-06-15 | —      | AR-12 M-1: added ErrPrefix* internal static readonly strings that derive  |
// |         |            |        | the rendered "0xNNNN" diagnostic text from the ushort error codes, so the |
// |         |            |        | codes are the single source of truth (throw sites in EventBus/EventLedger/|
// |         |            |        | CosmeticChannel/EventRegistry previously hardcoded the hex in message     |
// |         |            |        | literals). Rendered text byte-identical — substring assertions unaffected.|
// |         |            |        | AR-12 L-1: doc-noted CosmeticPerTickPublicationBudget + PayloadVersionWidth|
// |         |            |        | as declared-but-unconsumed at Stage 0 (Stage 0+1 / Stage 5+ activation).  |
// | 1.5     | 2026-08-15 | —      | #44 C1/C2 adversarial review round 4, M24/ERR-017-004: added a new       |
// |         |            |        | #region Fixed with CARD_KIND_YELLOW/CARD_KIND_RED/CARD_KIND_SECOND_YELLOW|
// |         |            |        | — the [FIXED] ALL_CAPS declaration of CardIssuedEvent.CardKind's         |
// |         |            |        | domain-ordinal encoding (Appendix A row 0x06), which had no home in this |
// |         |            |        | catalogue despite #17 owning the encoding. Two consuming catalogues had  |
// |         |            |        | independently declared the same three values under two different tags   |
// |         |            |        | (MatchEngineConstants [FIXED], DisciplineConstants [CROSS] citing prose, |
// |         |            |        | not a symbol); both now mirror these rows [CROSS]/PascalCase, per        |
// |         |            |        | src/CLAUDE.md's [CROSS]-mirror worked example. §3.10 patched in the same |
// |         |            |        | commit (docs/specs/event-system/section-3.md).                          |
// | 1.6     | 2026-08-15, later | — | Reviewed-findings pass (L3). Added FOUL_ORDINAL_NONE = 0xFFFF to  |
// |         |            |        | #region Fixed, beside CARD_KIND_*: CardIssuedEvent.FoulOrdinal's "no     |
// |         |            |        | associated foul" sentinel, previously prose-only in CardIssuedEvent.cs — |
// |         |            |        | ERR-017-004's exact defect shape recurring on the sibling payload field  |
// |         |            |        | of the same event. Mirrored [CROSS]/PascalCase in                        |
// |         |            |        | MatchEngineConstants.FoulOrdinalNone. MatchEngine.cs's own               |
// |         |            |        | foulOrdinal: 0xFFFF literal call site was NOT repointed (out of this     |
// |         |            |        | pass's ownership) — reported as an open call site. **SUPERSEDED same     |
// |         |            |        | day** — see v1.7 below: the later "#44 AR round 5, L3" pass repointed    |
// |         |            |        | the call site after this row was written.                                |
// | 1.7     | 2026-08-15, later still | — | Reviewed findings pass (L1). Corrected v1.6 above, which  |
// |         |            |        | still claimed the call site was open — MatchEngine.cs:5286 reads         |
// |         |            |        | MatchEngineConstants.FoulOrdinalNone. No code change in this file.       |
#endregion
