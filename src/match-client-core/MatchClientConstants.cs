// File:     src/match-client-core/MatchClientConstants.cs
// Created:  2026-07-24
// Modified: 2026-07-24
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P0),
//           Code Standards #20 (constant catalogue; no magic numbers)
// Purpose:  Constant catalogue for the host-free interactive-client core. P0 pins the master-plan
//           playback-speed set the UI presents (Pause is a streamer state, not a multiplier); camera
//           and render-cue [GT] tuning land alongside their consumers at P3/P4.

using static TacticalDirector.ProjectConstants.GameplayConfigHolder;

namespace TacticalDirector.MatchClientCore
{
    /// <summary>
    /// Constant catalogue for the interactive Unity client's host-free core. Presentation/pacing
    /// only — nothing here feeds the simulation or the snapshot digest. The four playback-speed
    /// steps are the master plan's set {Pause, 1, 3, 5, 10} (§3.4); Pause is delivered by the
    /// streamer's <c>Pause()</c>, the four multipliers by <c>SetSpeedMultiplier</c> (whose
    /// <c>MatchViewerConstants.MaxLiveSpeedMultiplier</c> cap is raised to ≥ 10 so 10× is not
    /// silently clamped — §5-P0). The P5 UI presents these; the core does not consume them.
    /// </summary>
    public static class MatchClientConstants
    {
        #region GT — playback-speed set (master plan §3.4: Pause / 1× / 3× / 5× / 10×)

        /// <summary>[GT] Real-time playback multiplier (1×). Config key [match-client] Speed1x.</summary>
        public static readonly float Speed1x = Config.GetFloat("match-client", "Speed1x", 1f);

        /// <summary>[GT] Fast playback multiplier (3×). Config key [match-client] Speed3x.</summary>
        public static readonly float Speed3x = Config.GetFloat("match-client", "Speed3x", 3f);

        /// <summary>[GT] Faster playback multiplier (5×). Config key [match-client] Speed5x.</summary>
        public static readonly float Speed5x = Config.GetFloat("match-client", "Speed5x", 5f);

        /// <summary>[GT] Fastest playback multiplier (10×). Config key [match-client] Speed10x. Requires <c>MatchViewerConstants.MaxLiveSpeedMultiplier</c> ≥ 10 (§5-P0).</summary>
        public static readonly float Speed10x = Config.GetFloat("match-client", "Speed10x", 10f);

        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-24 | —      | Initial creation (P0): master-plan playback-speed set          |
// |         |            |        | {1,3,5,10} as [GT] scalars via GameplayConfig. Camera /        |
// |         |            |        | render-cue tuning deferred to P3/P4 with their consumers.      |
#endregion
