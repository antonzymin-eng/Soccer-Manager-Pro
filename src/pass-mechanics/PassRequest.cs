// File:     src/pass-mechanics/PassRequest.cs
// Created:  2026-05-26
// Modified: 2026-05-26
// Author:   —
// Spec:     Pass Mechanics #5 §2.4.1, §4.1, Code Standards #20
// Purpose:  Input struct from Decision Tree to PassExecutor. Authoritative definition
//           lives here; Decision Tree #8 instantiates but does not define it (§4.1).

using UnityEngine;

namespace TacticalDirector.PassMechanics
{
    /// <summary>
    /// All data required to execute a pass. Created by Decision Tree #8, consumed by PassExecutor.
    /// Lifetime: single pass execution cycle (created → consumed → discarded).
    /// Pass Mechanics #5 §2.4.1.
    /// </summary>
    public struct PassRequest
    {
        /// <summary>Unique ID of the passing agent.</summary>
        public int AgentId;

        /// <summary>
        /// Requested pass type. Set by Decision Tree #8.
        /// Pass Mechanics does not select it (KD-2, §1.3).
        /// </summary>
        public PassType PassType;

        /// <summary>
        /// Cross delivery sub-type. Ignored for non-Cross pass types (logged as warning).
        /// Defaults to Flat when not specified (KD-6, §3.1.2).
        /// </summary>
        public CrossSubType CrossSubType;

        /// <summary>
        /// ID of the target agent. Set to -1 for space-targeted passes.
        /// When >= 0, TargetPosition is ignored.
        /// </summary>
        public int TargetAgentId;

        /// <summary>
        /// World-space target position. Used only when TargetAgentId == -1 (space-targeted).
        /// Z component is typically 0 for ground-level targets. Ball Physics #1 §1.2.
        /// </summary>
        public Vector3 TargetPosition;

        /// <summary>
        /// Intended pass distance in metres. Must be > 0. FM-07 fires if zero or negative.
        /// Used for velocity and launch angle calculations (§3.2, §3.3).
        /// </summary>
        public float IntendedDistance;

        /// <summary>
        /// Pass urgency [0.0, 1.0]. Affects windup duration (§3.8.8) and error angle (§3.5.4).
        /// 0.0 = unhurried (full windup). 1.0 = rushed (minimum windup, maximum urgency penalty).
        /// </summary>
        public float Urgency;

        /// <summary>
        /// True if pass requires the non-preferred foot. Set by Decision Tree #8 (KD-5).
        /// Pass Mechanics applies accuracy and power penalties; does not determine foot preference.
        /// </summary>
        public bool IsWeakFoot;

        /// <summary>
        /// Team ID of the passing agent. Required for pressure scalar computation (§3.5.6).
        /// </summary>
        public int TeamId;

        /// <summary>Frame timestamp. Required for determinism and replay synchronisation.</summary>
        public int FrameNumber;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                  |
// | 1.0     | 2026-05-26 | —      | Initial implementation. |
#endregion
