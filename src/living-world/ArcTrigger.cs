// File:     src/living-world/ArcTrigger.cs
// Created:  2026-07-24
// Modified: 2026-08-08
// Author:   —
// Spec:     Living World System #22 §3.4, §6.2, FR-LW-016/017/028, arc-triggers-design KD-3/KD-7, Code Standards #20
// Purpose:  One arc-trigger catalogue row: the target arc kind, the canon signal it thresholds on, its
//           rising-edge threshold, and the spawned arc's per-instance liveness bound.

namespace TacticalDirector.LivingWorld
{
    /// <summary>
    /// One catalogue row in <see cref="ArcTriggerCatalogue"/> (#22 §3.4, arc-triggers-design KD-3): the
    /// stable trigger id (recorded as <see cref="SpawnCause.TriggerId"/> at spawn, FR-LW-016), the
    /// target <see cref="ArcKind"/>, whether it is entity-scoped or board/squad-level (FR-LW-017), the
    /// canon signal key it thresholds on, the rising-edge <c>[GT]</c> threshold (KD-7 — a trigger fires
    /// the tick its signal first crosses to <c>&gt;= Threshold</c>), and the spawned arc's per-instance
    /// liveness bound in calendar days (§6.2, in [1, ARC_MAX_LIFETIME_DAYS]).
    /// </summary>
    public readonly struct ArcTrigger
    {
        /// <summary>Stable trigger id → <see cref="SpawnCause.TriggerId"/> at spawn.</summary>
        public readonly ushort TriggerId;

        /// <summary>The arc kind this trigger spawns.</summary>
        public readonly ArcKind Kind;

        /// <summary>True for entity-scoped triggers (walked per entity in ascending id order); false
        /// for board/squad-level triggers (walked once per tick, by ArcKind ordinal). FR-LW-017.</summary>
        public readonly bool IsEntityScoped;

        /// <summary>Which canon scalar this trigger thresholds on (the recorded input key).</summary>
        public readonly short SignalKey;

        /// <summary>[GT] Rising-edge threshold (KD-7): the signal must cross to <c>&gt;= Threshold</c>.</summary>
        public readonly float Threshold;

        /// <summary>[GT] Spawned-arc liveness bound in calendar days (§6.2).</summary>
        public readonly uint MaxLifetimeDays;

        public ArcTrigger(ushort triggerId, ArcKind kind, bool isEntityScoped, short signalKey,
            float threshold, uint maxLifetimeDays)
        {
            TriggerId = triggerId;
            Kind = kind;
            IsEntityScoped = isEntityScoped;
            SignalKey = signalKey;
            Threshold = threshold;
            MaxLifetimeDays = maxLifetimeDays;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author       | Notes                                                     |
// | 1.0     | 2026-07-24 | —            | Initial file. |
// | 1.1     | 2026-08-08 | Claude Code  | Added the required #region VersionHistory block (FR-CS-058; tools/recurring-defect-lint.py hygiene pass). |
#endregion
