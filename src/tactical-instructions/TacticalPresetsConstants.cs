// File:     src/tactical-instructions/TacticalPresetsConstants.cs
// Created:  2026-07-11
// Modified: 2026-07-11 (later same day — MATCH_TICKS_TOTAL note updated: allocated engine-side, [CROSS])
// Author:   —
// Spec:     Tactical Presets #26 §3.5, Appendix A.2/A.3 (v0.3), §4.1 placement; Code Standards #20
// Purpose:  Single constant catalogue for the #26 manager-AI layer: the §3.5 decision-cadence /
//           adaptation scalars plus the Appendix A.2 ManagerProfile archetype rows and the A.3
//           kickoff-selection affinity tables (indexed by TacticPresetLibrary preset ordinal).

using static TacticalDirector.ProjectConstants.GameplayConfigHolder;

namespace TacticalDirector.TacticalInstructions
{
    /// <summary>
    /// Constant catalogue for Tactical Presets &amp; AI-Manager Selection #26 (§3.5 + Appendix A.2/A.3).
    /// Placed in this assembly per #26 §4.1 — preset DATA sits with the #21 types it composes; the
    /// manager LOGIC that consumes these rows lives in the match-engine composition root
    /// (<c>ManagerDecisionGate</c> / <c>ManagerAdaptation</c>), which references this assembly.
    ///
    /// The A.3 rows are indexed by <see cref="TacticPresetLibrary"/> preset ordinal (the pinned
    /// A.1 ladder order, ParkTheBus 0 … Gegenpress 4) and the A.2 rows by manager-archetype
    /// ordinal (Aggressive 0 / Pragmatic 1 / Balanced 2) — both APPEND-only, matching the
    /// catalogue's own FR-TP-013 contract (a removed row would dangle serialized ordinals).
    ///
    /// NOTE — <c>MATCH_TICKS_TOTAL</c> is deliberately ABSENT: it is engine-owned, allocated
    /// 2026-07-11 as <c>MatchEngineConstants.MATCH_TICKS_TOTAL</c> (the #26 §3.5 row, promoted
    /// <c>[CROSS-PENDING]</c> → <c>[CROSS]</c>). It cannot be mirrored here — this bottom-of-graph
    /// assembly sits BELOW the match engine in the reference graph — so the §3.4 ladder keeps
    /// taking the total as an explicit parameter, supplied live by the engine's decision-point seam.
    /// </summary>
    public static class TacticalPresetsConstants
    {
        #region Fixed

        /// <summary>
        /// [FIXED] Cardinality of the Appendix A.2 manager-archetype table (Aggressive / Pragmatic /
        /// Balanced). The F2 restore-seam bound for serialized <c>ProfileOrdinal</c> values. #26 A.2.
        /// </summary>
        public const int MANAGER_ARCHETYPE_COUNT = 3;

        /// <summary>[FIXED] Archetype ordinal 0 — Aggressive (A.2 row order; APPEND-only).</summary>
        public const byte ARCHETYPE_AGGRESSIVE = 0;

        /// <summary>[FIXED] Archetype ordinal 1 — Pragmatic.</summary>
        public const byte ARCHETYPE_PRAGMATIC = 1;

        /// <summary>[FIXED] Archetype ordinal 2 — Balanced.</summary>
        public const byte ARCHETYPE_BALANCED = 2;

        #endregion

        #region GT

        /// <summary>
        /// [GT] Fixed manager-decision interval in 60 Hz ticks (5 match-minutes). A decision point
        /// recurs every this-many ticks after the last fired decision (#26 §3.2, FM-TP-02).
        /// Config key [tactical-presets] ManagerDecisionIntervalTicks.
        /// </summary>
        public static readonly int ManagerDecisionIntervalTicks =
            Config.GetInt("tactical-presets", "ManagerDecisionIntervalTicks", 18000);

        /// <summary>
        /// [GT] Base hold after a preset switch, in decision intervals; multiplied by the profile's
        /// <c>PatienceIntervals</c> (#26 §3.4, FR-TP-011).
        /// Config key [tactical-presets] ManagerSwitchHoldIntervals.
        /// </summary>
        public static readonly int ManagerSwitchHoldIntervals =
            Config.GetInt("tactical-presets", "ManagerSwitchHoldIntervals", 2);

        /// <summary>
        /// [GT] Urgency/protect threshold at which the adaptation ladder steps one rung (#26 §3.4).
        /// Config key [tactical-presets] AdaptStepThreshold.
        /// </summary>
        public static readonly float AdaptStepThreshold =
            Config.GetFloat("tactical-presets", "AdaptStepThreshold", 0.35f);

        /// <summary>
        /// [GT] Goal-differential cap in the urgency/protect terms — a 3-goal deficit is not more
        /// urgent than 2 (#26 §3.4; T-TP-U-009).
        /// Config key [tactical-presets] UrgencyDiffCap.
        /// </summary>
        public static readonly int UrgencyDiffCap =
            Config.GetInt("tactical-presets", "UrgencyDiffCap", 2);

        /// <summary>
        /// [GT] A.3 per-preset kickoff-selection baseline row, indexed by preset ordinal
        /// (ParkTheBus 0 … Gegenpress 4). Bounded [−1, +1] by shape test (FR-TP-020).
        /// </summary>
        public static readonly float[] BaseFit = new float[]
            { -0.30f, 0.10f, 0.50f, 0.20f, 0.10f }; // TODO: replace with config loader (Stage 1)

        /// <summary>
        /// [GT] A.3 per-preset Aggression affinity row, indexed by preset ordinal.
        /// Bounded [−1, +1] by shape test (FR-TP-020).
        /// </summary>
        public static readonly float[] AggrAffinity = new float[]
            { -0.50f, 0.10f, 0.00f, 0.30f, 0.80f }; // TODO: replace with config loader (Stage 1)

        /// <summary>
        /// [GT] A.3 per-preset Caution affinity row, indexed by preset ordinal.
        /// Bounded [−1, +1] by shape test (FR-TP-020).
        /// </summary>
        public static readonly float[] CautAffinity = new float[]
            { 0.60f, 0.30f, 0.00f, -0.10f, -0.40f }; // TODO: replace with config loader (Stage 1)

        /// <summary>
        /// [GT] A.2 archetype Aggression column, indexed by archetype ordinal
        /// (Aggressive / Pragmatic / Balanced). Range [0, 1] (FR-TP-020).
        /// </summary>
        public static readonly float[] ArchetypeAggression = new float[]
            { 0.8f, 0.3f, 0.5f }; // TODO: replace with config loader (Stage 1)

        /// <summary>
        /// [GT] A.2 archetype Caution column, indexed by archetype ordinal. Range [0, 1].
        /// </summary>
        public static readonly float[] ArchetypeCaution = new float[]
            { 0.2f, 0.7f, 0.5f }; // TODO: replace with config loader (Stage 1)

        /// <summary>
        /// [GT] A.2 archetype PatienceIntervals column, indexed by archetype ordinal (≥ 1; multiplies
        /// <see cref="ManagerSwitchHoldIntervals"/> per FR-TP-011).
        /// </summary>
        public static readonly int[] ArchetypePatienceIntervals = new int[]
            { 1, 2, 2 }; // TODO: replace with config loader (Stage 1)

        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-07-11 | —      | Initial implementation (#26 T1/T3 — §3.5 scalars + A.2/A.3 [GT] |
// |         |            |        |   tables; arrays literal per the table carve-out precedent;      |
// |         |            |        |   MATCH_TICKS_TOTAL deliberately absent, [CROSS-PENDING]).       |
// | 1.1     | 2026-07-11 | —      | Doc-only: the engine substrate allocated MATCH_TICKS_TOTAL as    |
// |         |            |        |   MatchEngineConstants.MATCH_TICKS_TOTAL (#26 §3.5 row promoted  |
// |         |            |        |   [CROSS-PENDING] → [CROSS]); it stays absent here because this  |
// |         |            |        |   assembly cannot reference upward — the ladder takes it as an   |
// |         |            |        |   explicit parameter, supplied live by the engine.               |
#endregion
