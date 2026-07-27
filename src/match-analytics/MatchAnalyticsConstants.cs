// File:     src/match-analytics/MatchAnalyticsConstants.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Match Analytics & Statistics #37 §6 / Appendix A, Code Standards #20
// Purpose:  The #37 constant catalogue — the xG model coefficients, the positional-bin grid, the
//           territorial sample cadence, and the pitch geometry the xG angle term needs.

using TacticalDirector.BallPhysics;
using TacticalDirector.MatchEngine;

namespace TacticalDirector.MatchAnalytics
{
    /// <summary>
    /// Appendix A catalogue for Match Analytics &amp; Statistics #37.
    ///
    /// <para>The three xG coefficients are <c>[GT]</c> and, per §3.3, <b>illustrative pending a Stage-2
    /// balance pass</b>: what this spec contracts is the model's SHAPE (closer and wider ⇒ higher xG),
    /// not the numbers. They are stated here rather than inlined so the balance pass has one place to
    /// edit, following the #21 G2 precedent.</para>
    /// </summary>
    public static class MatchAnalyticsConstants
    {
        // ── xG location model (§3.3) ─────────────────────────────────────────────────────────────

        /// <summary>[GT] Logistic intercept of the xG model (§3.3, Appendix A).</summary>
        public const float XG_INTERCEPT = 0.86f;

        /// <summary>[GT] Per-metre distance penalty in the xG logit (§3.3).</summary>
        public const float XG_DIST_COEF = 0.11f;

        /// <summary>[GT] Per-degree subtended-goal-angle bonus in the xG logit (§3.3).</summary>
        public const float XG_ANGLE_COEF = 0.017f;

        // ── Pitch geometry (mirrors, never set here) ─────────────────────────────────────────────

        /// <summary>[CROSS] Goal-mouth width (m). Ball Physics #1 §3.1.2 is the authority; mirrored
        /// read-only for the xG subtended-angle geometry (Appendix A).</summary>
        public const float GOAL_WIDTH_M = BallPhysicsConstants.Pitch.GOAL_WIDTH;

        /// <summary>[CROSS] Pitch length, goal-to-goal (m). Ball Physics #1 §3.1.2 (§1.2 corner-origin
        /// frame: team 0 attacks +x).</summary>
        public const float PITCH_LENGTH_M = BallPhysicsConstants.Pitch.LENGTH;

        /// <summary>[CROSS] Pitch width, touchline-to-touchline (m). Ball Physics #1 §3.1.2.</summary>
        public const float PITCH_WIDTH_M = BallPhysicsConstants.Pitch.WIDTH;

        // ── Positional binning / sampling (§3.4) ─────────────────────────────────────────────────

        /// <summary>[GT] Heatmap grid columns over <c>[0, PITCH_LENGTH_M]</c> (§3.4).</summary>
        public const int HEATMAP_COLS = 12;

        /// <summary>[GT] Heatmap grid rows over <c>[0, PITCH_WIDTH_M]</c> (§3.4).</summary>
        public const int HEATMAP_ROWS = 8;

        /// <summary>[DERIVED] Bins per team = <see cref="HEATMAP_COLS"/> × <see cref="HEATMAP_ROWS"/>.
        /// Never set independently.</summary>
        public const int HEATMAP_BINS_PER_TEAM = HEATMAP_COLS * HEATMAP_ROWS;

        /// <summary>[GT] Observed-tick stride for the territorial / heatmap positional sample (§3.4).
        /// A fixed cadence, never wall-clock — that is what makes the sample deterministic. Distinct
        /// from the lossless every-tick ledger pump (§3.5).</summary>
        public const int TERRITORIAL_SAMPLE_STRIDE = 1;

        /// <summary>[CROSS] Team count — two. Authoritative source:
        /// <c>MatchEngineConstants.TEAM_COUNT</c>. Mirrored, not re-declared: this indexes the same
        /// per-team arrays the engine fills, so a local copy is the parallel-surface trap — the two
        /// would agree silently right up until the day they did not.</summary>
        public const int TEAM_COUNT = MatchEngineConstants.TEAM_COUNT;

        // ── Record encodings the §3.2 routing table branches on ──────────────────────────────────
        // Mirrored rather than re-declared: the producer's own catalogue is the authority, so a
        // future re-encoding fails to compile here instead of silently re-labelling every card and
        // restart in the statline.

        /// <summary>[CROSS] <c>CardIssuedEvent.CardKind</c> value for a dismissal. Authoritative
        /// source: <c>MatchEngineConstants.CARD_KIND_RED</c> (§3.2 card routing).</summary>
        public const byte CARD_KIND_RED = MatchEngineConstants.CARD_KIND_RED;

        /// <summary>[CROSS] <c>RestartAwardedEvent.RestartKind</c> value for a throw-in. Authoritative
        /// source: Ball Physics #1 <c>RestartType.ThrowIn</c>.</summary>
        public const byte RESTART_KIND_THROW_IN = (byte)RestartType.ThrowIn;

        /// <summary>[CROSS] <c>RestartAwardedEvent.RestartKind</c> value for a goal kick.
        /// Authoritative source: Ball Physics #1 <c>RestartType.GoalKick</c>.</summary>
        public const byte RESTART_KIND_GOAL_KICK = (byte)RestartType.GoalKick;

        /// <summary>[CROSS] <c>RestartAwardedEvent.RestartKind</c> value for a corner. Authoritative
        /// source: Ball Physics #1 <c>RestartType.Corner</c>.</summary>
        public const byte RESTART_KIND_CORNER = (byte)RestartType.Corner;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-27 | —      | Initial creation (#37 T0): Appendix A catalogue — xG [GT]      |
// |         |            |        | coefficients, pitch [CROSS] mirrors from Ball Physics #1, the  |
// |         |            |        | heatmap grid and the territorial sample stride.                |
// | 1.1     | 2026-07-27 | —      | AR-1 M-4: TEAM_COUNT is a [CROSS] mirror of                    |
// |         |            |        | MatchEngineConstants.TEAM_COUNT rather than a local literal    |
// |         |            |        | 2. It indexes the same per-team arrays the engine fills, so a  |
// |         |            |        | local copy is the parallel-surface trap: the two would agree   |
// |         |            |        | silently until the day they did not.                           |
// | 1.2     | 2026-07-27 | —      | + the B3 card / restart record-encoding [CROSS] mirrors, and   |
// |         |            |        | the v1.1 TEAM_COUNT mirror RESTORED — a merge from main took   |
// |         |            |        | the pre-AR literal back while leaving the v1.1 row claiming    |
// |         |            |        | the mirror, which is the one shape nothing would have caught:  |
// |         |            |        | both values are 2, so no test could fail on the difference.    |
#endregion
