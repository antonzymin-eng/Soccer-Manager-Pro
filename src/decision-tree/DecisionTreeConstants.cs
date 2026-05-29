// File:     src/decision-tree/DecisionTreeConstants.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Decision Tree #8 §4.2, §3.7, Code Standards #20
// Purpose:  Pipeline-level constants not owned by UtilityWeights, ComposureWeights,
//           or TacticalWeights. Covers capacity limits, timing, and pipeline invariants.

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// Pipeline constants for the Decision Tree: capacity limits, timing budgets,
    /// and state machine invariants. Decision Tree #8 §4.2.
    /// </summary>
    public static class DecisionTreeConstants
    {
        // ── Agent Count ───────────────────────────────────────────────────────

        /// <summary>[FIXED] Standard football squad size on pitch.</summary>
        public const int AgentCount = 22;

        // ── Option Array Capacity ─────────────────────────────────────────────
        // §3.1.0: up to 17 slots (7 action types + 10 PASS candidates)

        /// <summary>[DERIVED] Maximum PASS candidates per evaluation (Decisions=20 cap).</summary>
        public const int MaxPassCandidates = 10;

        /// <summary>[DERIVED] Total option array capacity: MaxPassCandidates + 7 non-PASS types.</summary>
        public const int MaxOptions = 17;

        // ── Heartbeat ─────────────────────────────────────────────────────────

        /// <summary>[CROSS — Perception System #7] Tactical loop frequency (Hz).</summary>
        public const int HeartbeatHz = 10;

        /// <summary>[DERIVED] Milliseconds per heartbeat tick.</summary>
        public const float HeartbeatMs = 1000.0f / HeartbeatHz;

        // ── Performance Budget ────────────────────────────────────────────────

        /// <summary>[GT] Total batch budget (ms) for 22 agents per heartbeat. Decision Tree #8 §6.</summary>
        public const float BatchBudgetMs = 4.0f;

        /// <summary>[DERIVED] Per-agent budget (ms) = BatchBudgetMs / AgentCount.</summary>
        public const float PerAgentBudgetMs = BatchBudgetMs / AgentCount;

        // ── Attribute Normalisation ───────────────────────────────────────────

        /// <summary>[FIXED] Raw attribute range: [1, 20]. Normalises to A = (raw − 1) / 19.</summary>
        public const float AttributeNormRange = 19.0f;

        /// <summary>[FIXED] Minimum raw attribute value (raw − AttributeNormMin) / AttributeNormRange = A ∈ [0,1].</summary>
        public const float AttributeNormMin   = 1.0f;

        // ── Intercept Look-ahead Steps ────────────────────────────────────────
        // Decision Tree #8 §3.1.9.2

        /// <summary>[DERIVED] Intercept look-ahead time step (s). Step × Count = MAX_INTERCEPT_TIME (1.5s).</summary>
        public const float InterceptStepSeconds = 0.1f;

        /// <summary>[DERIVED] Number of intercept time steps. MAX_INTERCEPT_TIME / InterceptStepSeconds = 15.</summary>
        public const int InterceptStepCount = 15;

        // ── Warning Codes ─────────────────────────────────────────────────────

        /// <summary>AgentHasBall=true but BallVisible=false — unexpected state (§3.1.1.3).</summary>
        public const string WarnFmDt09 = "FM-DT-09";

        /// <summary>Dispatch invoked on a snapshot with ForcedRefreshThisTick=true. Informational.</summary>
        public const string WarnFmDt10 = "FM-DT-10";
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                           |
// | 1.0     | 2026-05-29 | —      | Initial implementation.                                                         |
// | 1.1     | 2026-05-29 | —      | AR-1 L-1/L-2: Add InterceptStepSeconds/Count constants; XML doc on              |
// |         |            |        |   AttributeNormMin.                                                             |
#endregion
