// File:     src/living-world/LivingWorldConstants.cs
// Created:  2026-06-21
// Modified: 2026-07-24 (arc-triggers Slice 1/E1: Fixed region gains the world.arcs stream ids, the
//           world-scoped entity-id sentinel block, the ARC_BOARD_SCOPE_KEY latch sentinel, and the
//           arc-trigger signal keys; GT region gains the per-trigger thresholds + per-kind lifetimes)
// Modified: 2026-08-08
//           Cross region gains the DomainTagLivingWorld mirror; region order now Fixed→Cross→GT)
// Author:   —
// Spec:     Living World System #22 Appendix A, §3.3, §4.4, §4.6, Code Standards #20
// Purpose:  Constant catalogue for the living-world layer. No literals in formula code.
//
// NOTE: every [GT] magnitude below is ILLUSTRATIVE pending the §7 (G2) balance pass — the reviewed
// contract is the shape/direction, not the value (precedent #21 G2, #8 draft-level). The values are
// placeholders for the [GT] config-loader (KD-10); runtime activation is gated on that loader. The
// [CROSS] values are consumed read-only from vol-2 (not set here) and are mirrored as comments until
// the human-systems assembly exists.

namespace TacticalDirector.LivingWorld
{
    /// <summary>
    /// Living-world constant catalogue. #22 Appendix A. All values [GT] unless tagged otherwise.
    /// </summary>
    public static class LivingWorldConstants
    {
        #region Fixed

        /// <summary>
        /// [FIXED] Stable siteId of the aperiodic world.text RNG sub-stream (#22 §3.3 / §4.4,
        /// FR-LW-020). Registered by <see cref="InteractionTextGenerator"/> — separate from the
        /// tick-driven world.arcs stream (still the ArcEngine's KD-10 seam) so player-triggered text
        /// generation never perturbs the arc cursor. #16 §3.2.5.1: the siteId must never change.
        /// </summary>
        public const string WORLD_TEXT_STREAM_SITE_ID = "world.text";

        /// <summary>
        /// [FIXED] world.text stream version (#16 §3.2.5.1 — bumped only if the draw-site ordering
        /// contract is ever re-authored; a bump invalidates replay parity by design).
        /// </summary>
        public const ushort WORLD_TEXT_STREAM_VERSION = 1;

        /// <summary>
        /// [FIXED] Stable siteId of the periodic tick-driven world.arcs RNG sub-stream (#22 §3.4 /
        /// FR-LW-020). Registered by <see cref="ArcTriggerEvaluator"/> — separate from the aperiodic
        /// <c>world.text</c> stream so player-triggered text never perturbs the arc cursor. #16
        /// §3.2.5.1: the siteId must never change.
        /// </summary>
        public const string WORLD_ARCS_STREAM_SITE_ID = "world.arcs";

        /// <summary>
        /// [FIXED] world.arcs stream version (#16 §3.2.5.1 — bump only on a draw-site reordering; a
        /// bump invalidates arc replay parity by design).
        /// </summary>
        public const ushort WORLD_ARCS_STREAM_VERSION = 1;

        // World-scoped LivingWorld RNG sub-stream entity-id sentinels. DeterministicRngService.
        // ComputeStreamKey hashes (subsystemOrdinal ‖ entityId ‖ streamVersion) and EXCLUDES siteId,
        // so two world-scoped streams in SubsystemOrdinals.LivingWorld MUST differ here to get distinct
        // keys (a key collision would draw an identical value sequence). Real entity ids are >= 0, so a
        // negative sentinel never collides with a real contact.

        /// <summary>[FIXED] Entity-id sentinel of the world.text sub-stream (the aperiodic
        /// player-triggered text stream). #22 §3.3.</summary>
        public const int WORLD_STREAM_ENTITY_TEXT = -1;

        /// <summary>[FIXED] Entity-id sentinel of the world.arcs sub-stream (the tick-driven arc-trigger
        /// stream). Distinct from <see cref="WORLD_STREAM_ENTITY_TEXT"/> so ComputeStreamKey yields a
        /// distinct key within the LivingWorld subsystem ordinal. #22 §3.4.</summary>
        public const int WORLD_STREAM_ENTITY_ARCS = -2;

        /// <summary>[FIXED] Entity-id sentinel reserved for the world.background sub-stream (the future
        /// BackgroundTierSim phase-5 stream — not yet registered; its cursor is a future
        /// WORLD_STORE_FORMAT_VERSION bump when built). #22 §3.5.</summary>
        public const int WORLD_STREAM_ENTITY_BACKGROUND = -3;

        /// <summary>
        /// [FIXED] KD-7 latch scope key for board/squad-level arc triggers (§3.4). Real entity ids are
        /// >= 0, so this negative sentinel never collides; board triggers share it and are disambiguated
        /// by <c>TriggerId</c>. <c>int.MinValue</c> sorts first deterministically in the canonical latch
        /// serialization order.
        /// </summary>
        public const int ARC_BOARD_SCOPE_KEY = int.MinValue;

        // ── Arc-trigger signal keys (SpawnCause.Input.Key identifiers) ────────────────────────────
        // Stable short identifiers for the canon scalars each Stage-0 trigger thresholds on, embedded
        // in the recorded SpawnCause.Input at spawn. APPEND-only (never re-number — an id is a durable
        // provenance handle). The real vol-2/vol-3 producer maps its own signals onto these keys.

        /// <summary>[FIXED] Canon signal: vol-2 §2.4 ego clash on a high-reputation pair (feeds the
        /// WonderkidVsVeteran entity trigger).</summary>
        public const short ARC_SIGNAL_KEY_EGO_CLASH = 1;

        /// <summary>[FIXED] Canon signal: accumulated media hostility toward an entity (feeds the
        /// MediaVendetta entity trigger).</summary>
        public const short ARC_SIGNAL_KEY_MEDIA_HOSTILITY = 2;

        /// <summary>[FIXED] Canon signal: vol-2 §2.2 pulse divergence across cliques (feeds the
        /// DressingRoomSplit board trigger).</summary>
        public const short ARC_SIGNAL_KEY_PULSE_DIVERGENCE = 3;

        /// <summary>[FIXED] Canon signal: board impatience vs. the vol-3 §4.1 patience profile (feeds
        /// the BoardPatienceCollapse board trigger; higher = closer to a collapse, keeping the trigger
        /// test monotone-increasing like every other signal).</summary>
        public const short ARC_SIGNAL_KEY_BOARD_IMPATIENCE = 4;

        /// <summary>
        /// [FIXED] Format version of the §4.6 living-world snapshot block (Appendix B pinned field
        /// order; ERR-022-002). WorldStateSerializer refuses any other value — bump only with an
        /// Appendix B order change.
        /// </summary>
        public const ushort WORLD_SNAPSHOT_FORMAT_VERSION = 1;

        /// <summary>
        /// [FIXED] Format version of the composite <see cref="WorldStore"/> save produced at the KD-10
        /// season composition root — the §4.6 four-store block PLUS the manager id, the world.text RNG
        /// block (world seed + <see cref="InteractionTextGenerator"/> stream cursor + action ordinal),
        /// and the FR-LW-022 active-set membership roster (none of which the §4.6 block carries).
        /// WorldStore refuses any other value; bump only when the composite field order changes.
        /// v2: added the world.text RNG block (generator wired into the store).
        /// v3 (arc-triggers E2): added the world.arcs RNG block (cursor + action ordinal) and the KD-7
        /// armed-off latch, between the world.text block and the membership roster. v2 payloads are
        /// rejected fail-loud (no in-place migration at Stage 0).
        /// </summary>
        public const ushort WORLD_STORE_FORMAT_VERSION = 3;

        #endregion

        #region Cross

        /// <summary>
        /// [CROSS] Living-world hash-domain tag.
        /// Authoritative source: DeterministicSimConstants.DOMAIN_TAG_LIVING_WORLD.
        /// Deterministic Simulation #16 §3.4 (allocated per ERR-022-001). Value: 0x1E.
        /// Single-consumer mirror per Spec #20 §4.2; leads the §4.6 snapshot block.
        /// </summary>
        public static readonly byte DomainTagLivingWorld =
            TacticalDirector.DeterministicSim.DeterministicSimConstants.DOMAIN_TAG_LIVING_WORLD;

        /// <summary>[CROSS: vol-2 §2.1] Clique-formation threshold (mutual edge weight). Consumed, not set here.
        /// DECLARED-BUT-UNCONSUMED in production (slice-2 AR-2 L-1): its consumer is the
        /// DressingRoomSplit trigger evaluator reading vol-2 clique state (§3.4), deferred with the
        /// KD-10 human-systems upstream; the value is a comment-mirror until that assembly exists.</summary>
        public const float CLIQUE_THRESHOLD = 0.6f;

        #endregion

        #region GT (illustrative, pending the §7 balance pass)

        /// <summary>[GT] Episodes retained per significant edge (target 8–16). §3.2.</summary>
        public const int MEMORY_BUFFER_DEPTH = 12;

        /// <summary>[GT] Salience of a fresh episode. §3.2.</summary>
        public const float SALIENCE_INITIAL = 1.0f;

        /// <summary>[GT] Per-calendar-day salience decay rate. §3.2.</summary>
        public const float SALIENCE_DECAY_RATE = 0.02f;

        /// <summary>[GT] Minimum salience for an episode to be citable in text. §3.3.
        /// Consumed by InteractionTextGenerator.Generate (the §3.2 referencing gate) since slice 3 —
        /// the slice-2 AR-2 L-1 unconsumed note is retired.</summary>
        public const float SALIENCE_REF_THRESHOLD = 0.30f;

        /// <summary>[GT] Default edge-update responsiveness (volatility v). §3.1.
        /// DECLARED-BUT-UNCONSUMED in production (AR-3 L-1): callers of MemoryStore.ApplyEvent supply
        /// volatility per event; this default is the fallback the event-ingest phase (WorldLoop phase 1)
        /// adopts when it lands with the KD-10 match-outcome-event producer.</summary>
        public const float LAYER_VOLATILITY_DEFAULT = 0.30f;

        /// <summary>[GT] Per-tick relaxation rate toward baseline (r). §3.1.
        /// DECLARED-BUT-UNCONSUMED in production (AR-3 L-1): feeds LivingWorldMath.ApplyDecay, whose
        /// WorldLoop phase-3 caller is deferred with the phase-2 vol-2 baseline wiring (KD-10).</summary>
        public const float LAYER_DECAY_RATE = 0.01f;

        /// <summary>[GT] Per-arc-instance liveness bound, in calendar days. §3.4 / §6.2.</summary>
        public const uint ARC_MAX_LIFETIME_DAYS = 120u;

        /// <summary>[GT] Bound on per-manager external contacts in the active set. §3.5.</summary>
        public const int ACTIVE_SET_EXTERNAL_CONTACTS_MAX = 64;

        /// <summary>[GT] Top-N salient episodes retained on demotion to cold-store. §3.5.</summary>
        public const int COLD_SUMMARY_RETAINED_EPISODES = 4;

        // ── Arc-trigger thresholds + per-kind lifetimes (§3.4, ArcTriggerCatalogue) ───────────────
        // KD-7: each is the rising-edge threshold a canon signal must cross to fire its arc once. All
        // signals are framed "pressure toward the arc" (higher = fire), so the test is a uniform
        // monotone-increasing `signal >= threshold`. Lifetimes are per-instance liveness bounds in
        // calendar days, each in [1, ARC_MAX_LIFETIME_DAYS] (§6.2).

        /// <summary>[GT] Ego-clash threshold for the WonderkidVsVeteran entity trigger. §3.4.</summary>
        public const float ARC_TRIGGER_EGO_CLASH_THRESHOLD = 0.75f;

        /// <summary>[GT] Media-hostility threshold for the MediaVendetta entity trigger. §3.4.</summary>
        public const float ARC_TRIGGER_MEDIA_HOSTILITY_THRESHOLD = 0.70f;

        /// <summary>[GT] Pulse-divergence threshold for the DressingRoomSplit board trigger. §3.4.</summary>
        public const float ARC_TRIGGER_PULSE_DIVERGENCE_THRESHOLD = 0.65f;

        /// <summary>[GT] Board-impatience threshold for the BoardPatienceCollapse board trigger. §3.4.</summary>
        public const float ARC_TRIGGER_BOARD_IMPATIENCE_THRESHOLD = 0.80f;

        /// <summary>[GT] WonderkidVsVeteran arc liveness bound (calendar days). §6.2.</summary>
        public const uint ARC_TRIGGER_WONDERKID_LIFETIME_DAYS = 45u;

        /// <summary>[GT] MediaVendetta arc liveness bound (calendar days). §6.2.</summary>
        public const uint ARC_TRIGGER_MEDIA_LIFETIME_DAYS = 60u;

        /// <summary>[GT] DressingRoomSplit arc liveness bound (calendar days). §6.2.</summary>
        public const uint ARC_TRIGGER_DRESSING_ROOM_LIFETIME_DAYS = 30u;

        /// <summary>[GT] BoardPatienceCollapse arc liveness bound (calendar days). §6.2.</summary>
        public const uint ARC_TRIGGER_BOARD_LIFETIME_DAYS = 90u;

        // SAVE_SIZE_BUDGET is platform-tuned ([GT], set per-platform by the config-loader; caps
        // live edges + live episodes + cold summaries, §4.5) — not declared as a single literal here.

        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author       | Notes                                                     |
// | 1.0     | 2026-06-21 | —            | Initial file. |
// | 1.1     | 2026-07-02 | —            | Substantive edit; no version-history row was recorded for it at the time (FR-CS-058 gap, predates this hygiene pass). |
// | 1.2     | 2026-08-08 | Claude Code  | Added the required #region VersionHistory block (FR-CS-058; tools/recurring-defect-lint.py hygiene pass). |
#endregion
