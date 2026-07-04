// File:     src/living-world/LivingWorldConstants.cs
// Created:  2026-06-21
// Modified: 2026-07-02 (slice 3: Fixed region — world.text stream ids + snapshot format version;
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
        /// </summary>
        public const ushort WORLD_STORE_FORMAT_VERSION = 2;

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

        // SAVE_SIZE_BUDGET is platform-tuned ([GT], set per-platform by the config-loader; caps
        // live edges + live episodes + cold summaries, §4.5) — not declared as a single literal here.

        #endregion
    }
}
