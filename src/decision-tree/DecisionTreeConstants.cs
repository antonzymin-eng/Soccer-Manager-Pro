// File:     src/decision-tree/DecisionTreeConstants.cs
// Created:  2026-05-29
// Modified: 2026-06-14 (audit AR-3 fix pass)
// Modified: 2026-07-26 (+ NoPossessorAgentId — names the loose-ball sentinel at the DecisionContextAssembler seam)
// Modified: 2026-08-06 (ERR-008-022 AR-1 — + BisectorDegenerateSqrThreshold: was a named local in OptionGenerator)
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

        /// <summary>
        /// [FIXED] AgentId convention split point: IDs [0, HomeSquadAgentCount) are the
        /// home team, [HomeSquadAgentCount, AgentCount) the away team (Stage 0 convention).
        /// Single source of truth for the possessor-team classification in
        /// DecisionContextAssembler (AR-3 L: the bare literal 11 was previously inlined
        /// there, decoupled from this convention). Stage 1+: replace ID-range inference
        /// with an explicit per-agent team lookup.
        /// </summary>
        public const int HomeSquadAgentCount = 11;

        /// <summary>
        /// [FIXED] <c>MatchContext.PossessingAgentId</c> sentinel meaning "the ball is loose — no agent
        /// holds it". Consumed by <c>DecisionContextAssembler</c> for the §2.2.4 possessor-team
        /// classification (loose ⇒ <see cref="PossessionState.CONTESTED"/>), replacing a bare <c>-1</c>
        /// literal at that one site. Mirrors the host's <c>MatchEngineConstants.NO_POSSESSION</c>; the two
        /// are cross-assembly siblings, so neither may change without the other.
        /// </summary>
        public const int NoPossessorAgentId = -1;

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

        // ── Facing ────────────────────────────────────────────────────────────

        /// <summary>
        /// [FIXED] Squared-magnitude floor below which a facing vector is treated as
        /// degenerate (zero-length) and a team-relative default is substituted
        /// (DecisionContextAssembler). AR-3 L: the bare literal 0.0001f was previously
        /// inlined; promoting it satisfies FR-CS-016 and documents the shared threshold.
        /// </summary>
        public const float FacingDegenerateSqrThreshold = 0.0001f;

        /// <summary>
        /// [FIXED] Squared-magnitude floor below which the sum of the two unit post
        /// directions is treated as a degenerate bisector (a 180° goal arc, i.e. the
        /// shooter standing on the goal line between the posts) and the goal is scored
        /// as fully closed. Sits above Unity's own 1e-5 normalisation epsilon so the
        /// guard fires before <c>normalized</c> silently returns a zero vector.
        /// §3.2.3.2 (OptionGenerator). ERR-008-022 AR-1: was an inlined named local.
        /// </summary>
        public const float BisectorDegenerateSqrThreshold = 1e-8f;

        // ── Intercept Look-ahead Steps ────────────────────────────────────────
        // Decision Tree #8 §3.1.9.2

        /// <summary>[DERIVED] Intercept look-ahead time step (s). Step × Count = MAX_INTERCEPT_TIME (1.5s).</summary>
        public const float InterceptStepSeconds = 0.1f;

        /// <summary>[DERIVED] Number of intercept time steps. MAX_INTERCEPT_TIME / InterceptStepSeconds = 15.</summary>
        public const int InterceptStepCount = 15;

        // ── Failure-Mode Codes ────────────────────────────────────────────────
        // NOTE: the spec double-allocated "FM-DT-09" to two unrelated failure modes
        // (§3.1.1.3 possession-uncertainty warning AND §3.5.9 unknown-ActionType).
        // ERR-008-007 keeps FM-DT-09 for §3.1.1.3 and renumbers the §3.5.9 dispatch
        // failure to FM-DT-14 (next free ID after FM-DT-13); §3.5.9 patched in the
        // same commit.

        /// <summary>FM-DT-09 (§3.1.1.3): AgentHasBall=true but BallVisible=false — implausible but non-fatal.</summary>
        public const string WarnFmDt09 = "FM-DT-09";

        /// <summary>FM-DT-10 (§3.5.9): PASS dispatch with IntendedDistance ≤ 0 / non-finite; recomputed fallback.</summary>
        public const string WarnFmDt10 = "FM-DT-10";

        /// <summary>FM-DT-11 (§3.5.9): SHOOT dispatch with DistanceToGoal ≤ 0 / non-finite; recomputed fallback.</summary>
        public const string WarnFmDt11 = "FM-DT-11";

        /// <summary>FM-DT-12 (§3.5.9): SHOOT PlacementTarget component out of [0,1]; clamped (non-finite → 0.5).</summary>
        public const string WarnFmDt12 = "FM-DT-12";

        /// <summary>FM-DT-14 (§3.5.9, renumbered per ERR-008-007): unknown ActionType at dispatch; HOLD-safe command issued.</summary>
        public const string WarnFmDt14 = "FM-DT-14";
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                           |
// | 1.0     | 2026-05-29 | —      | Initial implementation.                                                         |
// | 1.1     | 2026-05-29 | —      | AR-1 L-1/L-2: Add InterceptStepSeconds/Count constants; XML doc on              |
// |         |            |        |   AttributeNormMin.                                                             |
// | 1.2     | 2026-06-11 | —      | Audit AR-2 M-10: failure-mode codes aligned to §3.5.9 — WarnFmDt10 doc was a    |
// |         |            |        |   nonexistent "forced-refresh dispatch" notion; FM-DT-11/12 added for the new   |
// |         |            |        |   pre-dispatch assertions; FM-DT-14 allocated for unknown-ActionType            |
// |         |            |        |   (FM-DT-09 double-allocation resolved per ERR-008-007).                        |
// | 1.3     | 2026-06-14 | —      | Audit AR-3 L: HomeSquadAgentCount (single source for the possessor-team ID     |
// |         |            |        |   split, was a bare literal 11 in the assembler) + FacingDegenerateSqrThreshold |
// |         |            |        |   (was an inlined 0.0001f) added per FR-CS-016.                                 |
// | 1.4     | 2026-07-26 | —      | + [FIXED] NoPossessorAgentId = -1: names the                    |
// |         |            |        |   MatchContext.PossessingAgentId loose-ball sentinel that       |
// |         |            |        |   DecisionContextAssembler's possessor-team classification      |
// |         |            |        |   keys on (was a bare -1 literal). Cross-assembly sibling of    |
// |         |            |        |   MatchEngineConstants.NO_POSSESSION.                           |
// | 1.5     | 2026-08-06 | —      | ERR-008-022 AR-1: + [FIXED] BisectorDegenerateSqrThreshold = 1e-8. Was a    |
// |         |            |        | named local inside OptionGenerator.ComputeGoalOpeningScore — a constant     |
// |         |            |        | in formula code (FR-CS-016). Sits above Unity's 1e-5 normalisation          |
// |         |            |        | epsilon so the degenerate-arc guard fires before `normalized` silently      |
// |         |            |        | returns a zero vector.                                                      |
#endregion
