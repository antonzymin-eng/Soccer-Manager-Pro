// File:     src/goalkeeper-mechanics/SaveIntent.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §2.2.1, KD-17, Code Standards #20
// Purpose:  Locked intent for a GK save attempt. Committed at the 10 Hz tactical tick
//           and held immutable for the 60 Hz physics resolution phase (KD-17).

using UnityEngine;

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// Locked save intent committed by the Decision Tree at the 10 Hz tactical tick.
    /// Fields are immutable once committed; the 60 Hz physics loop reads but never writes them (KD-17).
    /// Goalkeeper Mechanics #11 §2.2.1.
    /// </summary>
    public struct SaveIntent
    {
        /// <summary>Which hand the GK commits to lead the save. Anatomy-lookup only; no formula gating (KD-1). §2.2.1.</summary>
        public HandEnum TargetHand;

        /// <summary>
        /// Clutch firmness [0, 1]. Higher values reduce rebound scatter in parry/deflect paths.
        /// 0 = minimal grip attempt; 1 = maximum committed grip. §3.5 / CLUTCH_FIRMNESS_K_RETAIN.
        /// </summary>
        public float ClutchFirmness;

        /// <summary>
        /// Optional world-space target for a directed deflection. Null when the GK intends to catch or parry
        /// without a specific deflection direction (Decision Tree sets this for parry-to-corner intent). §3.5.
        /// </summary>
        public Vector3? DeflectionTarget;

        /// <summary>
        /// 10 Hz tactical tick index at which the intent was locked.
        /// Used by §3.2 reaction-pipeline to compute reactionOffsetMs (KD-17).
        /// </summary>
        public int AttemptCommittedTick;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
