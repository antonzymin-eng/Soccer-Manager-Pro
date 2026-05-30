// File:     src/event-system/EventSystemConstants.cs
// Created:  2026-05-30
// Modified: 2026-05-30
// Author:   —
// Spec:     Event System #17 §3.10, Code Standards #20
// Purpose:  All constants for the event system. Region order: GT → Cross.
//           No magic literals permitted in any other event-system file.

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.EventSystem
{
    /// <summary>
    /// All constants for Event System #17. §3.10.
    /// GT sub-classes per §3.10 note: runtime-tunable (queue sizes, depth) vs
    /// design-fixed (ordinal width, error codes) — the latter are locked at approval.
    /// </summary>
    public static class EventSystemConstants
    {
        #region GT

        // ── Runtime-tunable [GT] ────────────────────────────────────────────────────────

        /// <summary>[GT] Ring-buffer slot count per tick. §3.5.1 / §6.3.
        /// Derivation: 64 first-order ceiling × MAX_EVENT_DISPATCH_DEPTH (8) × 2 headroom = 1024
        /// (additive BFS under FR-EVT-046a out-degree cap = 1).</summary>
        public static readonly int EventQueueCapacity = 1024; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Aggregate per-tick Tier C publication sanity ceiling. §3.5.3 / §6.3.
        /// NOT a delivery queue capacity — Tier C is immediate-dispatch per §3.2.3.</summary>
        public static readonly int CosmeticPerTickPublicationBudget = 4096; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum second-order Tier A/B BFS dispatch depth per DrainTick. §3.2.5.</summary>
        public static readonly int MaxEventDispatchDepth = 8; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum Tier A/B subscriber handlers per event type. Revisited at Stage 0+1 measurements.</summary>
        public static readonly int MaxHandlersPerEventType = 32; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum Tier C subscriber handlers per event type. §4.3.2.</summary>
        public static readonly int MaxTierCHandlersPerType = 64; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum bytes per ring-buffer slot (12-byte header + up to MaxEventSlotBytes-12 bytes payload).
        /// Sized to accommodate the largest registered event struct: HeaderExecutedEvent and DecisionMadeEvent (136 bytes each).
        /// AR-2 H-1/H-3: was 128 — caused ring-buffer overrun and stackalloc slice crash. §3.5.1.</summary>
        public static readonly int MaxEventSlotBytes = 160; // TODO: replace with config loader (Stage 1)

        // ── Design-fixed [GT] — locked at approval; NOT runtime-tunable per §3.10 sub-class note ────

        /// <summary>[GT] Event type ordinal namespace width in bytes. §3.1.2. Stage 5+ expansion to 2 bytes per D5 §7.5.</summary>
        public const int EventTypeOrdinalWidth = 1;

        /// <summary>[GT] Payload version field width in bytes. §3.1 / §3.7.</summary>
        public const int PayloadVersionWidth = 1;

        /// <summary>[GT] Fixed 12-byte event header size (per §2.4.1 struct skeleton).
        /// 1+1+2+4+2+2 = eventTypeOrdinal+payloadVersion+_reserved+tick+subsystemOrdinal+intraPhaseDrawIndex.</summary>
        public const int EventHeaderBytes = 12;

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
#endregion
