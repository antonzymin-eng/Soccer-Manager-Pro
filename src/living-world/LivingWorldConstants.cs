// File:     src/living-world/LivingWorldConstants.cs
// Created:  2026-06-21
// Modified: 2026-06-21
// Author:   —
// Spec:     Living World System #22 Appendix A, Code Standards #20
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
        #region GT (illustrative, pending the §7 balance pass)

        /// <summary>[GT] Episodes retained per significant edge (target 8–16). §3.2.</summary>
        public const int MEMORY_BUFFER_DEPTH = 12;

        /// <summary>[GT] Salience of a fresh episode. §3.2.</summary>
        public const float SALIENCE_INITIAL = 1.0f;

        /// <summary>[GT] Per-calendar-day salience decay rate. §3.2.</summary>
        public const float SALIENCE_DECAY_RATE = 0.02f;

        /// <summary>[GT] Minimum salience for an episode to be citable in text. §3.3.</summary>
        public const float SALIENCE_REF_THRESHOLD = 0.30f;

        /// <summary>[GT] Default edge-update responsiveness (volatility v). §3.1.</summary>
        public const float LAYER_VOLATILITY_DEFAULT = 0.30f;

        /// <summary>[GT] Per-tick relaxation rate toward baseline (r). §3.1.</summary>
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

        #region Cross (vol-2 — consumed read-only; mirrored as comments until the human-systems assembly exists)

        /// <summary>[CROSS: vol-2 §2.1] Clique-formation threshold (mutual edge weight). Consumed, not set here.</summary>
        public const float CLIQUE_THRESHOLD = 0.6f;

        #endregion
    }
}
