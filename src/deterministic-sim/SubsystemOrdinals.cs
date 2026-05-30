// File:     src/deterministic-sim/SubsystemOrdinals.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Deterministic Simulation #16 §3.1.1, §3.4, Code Standards #20
// Purpose:  Compile-time subsystem ordinals used as the secondary sort key in canonical intra-phase ordering.
//           Ordinals MUST remain stable across all builds and platforms. Reordering or renumbering is a
//           breaking change requiring SNAPSHOT_SCHEMA_VERSION bump. Gaps are intentional to allow future
//           insertion without global renumbering.

namespace TacticalDirector.DeterministicSim
{
    /// <summary>
    /// Compile-time subsystem ordinals for canonical intra-phase entity ordering.
    /// Secondary sort key after EntityId ascending. §3.1.1.
    /// All values are contiguous within each layer group; gaps between groups reserve space for Stage 1+.
    /// Deterministic Simulation #16 §3.1.1 / §3.4.
    /// </summary>
    public static class SubsystemOrdinals
    {
        // ── Physics layer (0–19) ──────────────────────────────────────────────────────

        /// <summary>[FIXED] Ball Physics #1 subsystem ordinal. §3.1.1.</summary>
        public const int BallPhysics = 0;

        /// <summary>[FIXED] Agent Movement #2 subsystem ordinal. §3.1.1.</summary>
        public const int AgentMovement = 1;

        /// <summary>[FIXED] Collision System #3 subsystem ordinal. §3.1.1.</summary>
        public const int CollisionSystem = 2;

        /// <summary>[FIXED] First Touch #4 subsystem ordinal. §3.1.1.</summary>
        public const int FirstTouch = 3;

        /// <summary>[FIXED] Pass Mechanics #5 subsystem ordinal. §3.1.1.</summary>
        public const int PassMechanics = 4;

        /// <summary>[FIXED] Shot Mechanics #6 subsystem ordinal. §3.1.1.</summary>
        public const int ShotMechanics = 5;

        /// <summary>[FIXED] Heading Mechanics #10 subsystem ordinal. §3.1.1.</summary>
        public const int HeadingMechanics = 6;

        /// <summary>[FIXED] Goalkeeper Mechanics #11 subsystem ordinal. §3.1.1.</summary>
        public const int GoalkeeperMechanics = 7;

        // ── Mechanics layer (20–39) ───────────────────────────────────────────────────

        /// <summary>[FIXED] Positioning AI #12 subsystem ordinal. §3.1.1.</summary>
        public const int PositioningAI = 20;

        /// <summary>[FIXED] Pressing AI #13 subsystem ordinal. §3.1.1.</summary>
        public const int PressingAI = 21;

        /// <summary>[FIXED] Defensive AI #14 subsystem ordinal. §3.1.1.</summary>
        public const int DefensiveAI = 22;

        /// <summary>[FIXED] Attacking AI #15 subsystem ordinal. §3.1.1.</summary>
        public const int AttackingAI = 23;

        // ── AI / Decision layer (40–59) ───────────────────────────────────────────────

        /// <summary>[FIXED] Perception System #7 subsystem ordinal. §3.1.1.</summary>
        public const int PerceptionSystem = 40;

        /// <summary>[FIXED] Decision Tree #8 subsystem ordinal. §3.1.1.</summary>
        public const int DecisionTree = 41;

        // ── Infrastructure (60+) ──────────────────────────────────────────────────────

        /// <summary>[FIXED] Event System #17 subsystem ordinal. §3.1.1.</summary>
        public const int EventSystem = 60;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
