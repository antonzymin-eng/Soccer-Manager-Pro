// File:     src/heading-mechanics/HeaderIntent.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Heading Mechanics #10 §2.2, KD-17, FR-HE-018, Code Standards #20
// Purpose:  Intent produced by Decision Tree #8 at the 10 Hz tactical tick; locked at commit.

using UnityEngine;

namespace TacticalDirector.HeadingMechanics
{
    /// <summary>
    /// Intent from Decision Tree #8 at the 10 Hz tactical tick. Locked at commit per KD-17 / FR-HE-018:
    /// TargetIntent, PowerIntent, and ContactPointIntent are NOT re-issued by DT mid-attempt.
    /// Only PredictedContactFrame (tracked in HeaderContactState) is re-evaluated each 60 Hz tick.
    /// Heading Mechanics #10 §2.2.
    /// Stage 0+1 activation: DT #8 §1.7.2 stub promotes to live HEADER action-type at Stage 0+1 (§4.6.1).
    /// </summary>
    public struct HeaderIntent
    {
        /// <summary>Intended power scale [0, 1]. Locked at commit. §2.2.</summary>
        public float PowerIntent;

        /// <summary>
        /// Intended 2-D contact point in head-local coordinates (m).
        /// Origin at head centre. +x = agent.facing forward; +y = agent-left lateral.
        /// Clamped to head-local envelope per FR-HE-030. Locked at commit. §2.2.
        /// </summary>
        public Vector2 ContactPointIntent;

        /// <summary>
        /// Intended target position in corner-origin world-space coordinates (m).
        /// Clamped to pitch bounding box per FR-HE-029 if outside bounds.
        /// Locked at commit. §2.2.
        /// </summary>
        public Vector3 TargetIntent;

        /// <summary>
        /// 10 Hz tactical tick index at which the header was committed.
        /// Used by §3.3 to derive jumpStartFrame: first 60 Hz frame ≥ AttemptCommittedTick × 6
        /// where agent is not in GROUNDED / STUMBLING. §2.2 / v0.2 L-1.
        /// </summary>
        public int AttemptCommittedTick;

        /// <summary>
        /// Delivery type for telemetry only (KD-13 / FR-HE-016). Physics is uniform across types.
        /// Set by the caller alongside the intent; never consumed by §3.5–§3.7 formulas.
        /// </summary>
        public SetPieceContext SetPieceContext;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
