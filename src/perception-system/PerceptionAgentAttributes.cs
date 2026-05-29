// File:     src/perception-system/PerceptionAgentAttributes.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Perception System #7 §4.2.2, Agent Movement #2 §3.5.6, Code Standards #20
// Purpose:  Snapshot of agent attributes and flags consumed by the perception pipeline.
//           Read once per heartbeat at pipeline Step 1; held constant for the duration.

namespace TacticalDirector.PerceptionSystem
{
    /// <summary>
    /// Per-agent attribute snapshot consumed by FovCalculator, RecognitionLatencyTracker,
    /// and ShoulderCheckScheduler. Cached at heartbeat start (Step 1 — §3.0).
    /// All integer attributes are in [1, 20]. Perception System #7 §4.2.2 / Agent Movement #2 §3.5.6.
    /// </summary>
    public struct PerceptionAgentAttributes
    {
        /// <summary>Decisions attribute [1–20]. Governs FoV modifier (§3.1.3) and L_rec (§3.3.2). Agent Movement #2 §3.5.6.</summary>
        public int Decisions;

        /// <summary>Anticipation attribute [1–20]. Governs shoulder check interval (§3.4.2). Agent Movement #2 §3.5.6.</summary>
        public int Anticipation;

        /// <summary>Team identifier (0 or 1). Used to discriminate teammate vs opponent shadow cones (§3.2.2). Ball Physics #1 §1.2.</summary>
        public int TeamId;

        /// <summary>
        /// True when agent is in a half-turned body stance.
        /// When true, L_rec for entities in the peripheral arc (40°–80° from facing) is reduced
        /// by HalfTurnLRecReduction (15%). First Touch Mechanics #4 §3.3.2 — cross-spec constant.
        /// </summary>
        public bool IsHalfTurned;

        /// <summary>Creates a default-attribute snapshot (mid-range Decisions/Anticipation, Team 0).</summary>
        public static PerceptionAgentAttributes CreateDefault()
        {
            return new PerceptionAgentAttributes
            {
                Decisions    = 10,
                Anticipation = 10,
                TeamId       = 0,
                IsHalfTurned = false
            };
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
