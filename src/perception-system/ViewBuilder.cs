// File:     src/perception-system/ViewBuilder.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Perception System #7 §3.7, §4.1, Code Standards #20
// Purpose:  Assembles FilteredView and PerceptionDiagnostics from pipeline step outputs (§3.7).
//           Populates all scalar and count fields. PerceivedAgent array references are
//           pre-allocated and pre-filled by PerceptionSystem pipeline Steps 4–6; this class
//           does NOT overwrite them.
//           Static class. Pure assembly — no computation.

using UnityEngine;

namespace TacticalDirector.PerceptionSystem
{
    /// <summary>
    /// Assembles FilteredView and PerceptionDiagnostics from the outputs of all prior
    /// pipeline steps. Finalises all scalar and count fields.
    ///
    /// IMPORTANT: VisibleTeammates, VisibleOpponents, and BlindSidePerceivedAgents array
    /// references inside FilteredView are pre-wired at PerceptionSystem construction time
    /// and pre-filled during pipeline Steps 4–6. Build() does not touch them — only the
    /// corresponding Count fields are set here.
    ///
    /// Static class. No computation — pure field assignment. No side effects.
    /// Perception System #7 §3.7, §4.1.
    /// </summary>
    public static class ViewBuilder
    {
        /// <summary>
        /// Finalises scalar and count fields on a pre-allocated FilteredView and PerceptionDiagnostics.
        /// Must be called after pipeline Steps 4–6 have filled the PerceivedAgent arrays.
        /// Perception System #7 §3.7.1–§3.7.2.
        /// </summary>
        /// <param name="agentId">Observer agent ID (0–21).</param>
        /// <param name="frameNumber">Current 10Hz heartbeat tick.</param>
        /// <param name="forcedRefreshThisTick">True if this view was produced by a forced mid-heartbeat refresh (§3.8).</param>
        /// <param name="ballVisible">True if ball passed range + FoV + occlusion tests this tick.</param>
        /// <param name="ballPerceivedPosition">Last confirmed 2D ball position.</param>
        /// <param name="ballStalenessFrames">Consecutive ticks ball has been invisible (0 = currently visible).</param>
        /// <param name="visibleTeammatesCount">Number of valid entries in outView.VisibleTeammates [0..MaxVisibleTeammates).</param>
        /// <param name="visibleOpponentsCount">Number of valid entries in outView.VisibleOpponents [0..MaxVisibleOpponents).</param>
        /// <param name="blindSidePerceivedAgentsCount">Number of valid entries in outView.BlindSidePerceivedAgents [0..MaxBlindSideAgents).</param>
        /// <param name="effectiveFoVAngle">Effective full FoV angle (degrees) after Decisions modifier and pressure narrowing.</param>
        /// <param name="pressureScalar">Pressure scalar [0, 1] at filter execution time.</param>
        /// <param name="blindSideWindowActive">True if a shoulder check window is currently active.</param>
        /// <param name="blindSideWindowExpiry">Heartbeat tick (as float) at which the current window expires.</param>
        /// <param name="animData">Shoulder check animation stub for Stage 1+ (§3.4.4).</param>
        /// <param name="outView">Pre-allocated FilteredView to finalise. PerceivedAgent arrays must already be wired and filled.</param>
        /// <param name="outDiagnostics">Pre-allocated PerceptionDiagnostics to finalise.</param>
        public static void Build(
            int   agentId,
            int   frameNumber,
            bool  forcedRefreshThisTick,
            bool  ballVisible,
            Vector2 ballPerceivedPosition,
            int   ballStalenessFrames,
            int   visibleTeammatesCount,
            int   visibleOpponentsCount,
            int   blindSidePerceivedAgentsCount,
            float effectiveFoVAngle,
            float pressureScalar,
            bool  blindSideWindowActive,
            float blindSideWindowExpiry,
            ShoulderCheckAnimData animData,
            ref FilteredView outView,
            ref PerceptionDiagnostics outDiagnostics)
        {
            // ── FilteredView — consumer output (§3.7.1) ──────────────────────────────

            outView.ObserverId                    = agentId;
            outView.FrameNumber                   = frameNumber;
            outView.ForcedRefreshThisTick         = forcedRefreshThisTick;
            outView.BallVisible                   = ballVisible;
            outView.BallPerceivedPosition         = ballPerceivedPosition;
            outView.BallStalenessFrames           = ballStalenessFrames;
            outView.VisibleTeammatesCount         = visibleTeammatesCount;
            outView.VisibleOpponentsCount         = visibleOpponentsCount;
            outView.BlindSidePerceivedAgentsCount = blindSidePerceivedAgentsCount;
            // Note: VisibleTeammates, VisibleOpponents, BlindSidePerceivedAgents array
            // references are pre-wired at PerceptionSystem construction. Entries
            // [0..Count-1] were filled during pipeline Steps 4–6.

            // ── PerceptionDiagnostics — filter metadata (§3.7.2) ─────────────────────

            outDiagnostics.ObserverId            = agentId;
            outDiagnostics.FrameNumber           = frameNumber;
            outDiagnostics.EffectiveFoVAngle     = effectiveFoVAngle;
            outDiagnostics.PressureScalar        = pressureScalar;
            outDiagnostics.BlindSideWindowActive = blindSideWindowActive;
            outDiagnostics.BlindSideWindowExpiry = blindSideWindowExpiry;
            outDiagnostics.ShoulderCheckAnim     = animData;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
