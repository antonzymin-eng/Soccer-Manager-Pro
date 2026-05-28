// File:     src/shot-mechanics/ShotRequest.cs
// Created:  2026-05-27
// Modified: 2026-05-27
// Author:   —
// Spec:     Shot Mechanics #6 §2.4.1, §4.6.1, Code Standards #20
// Purpose:  Input struct from Decision Tree to ShotExecutor. Owned by Shot Mechanics;
//           Decision Tree instantiates but does not define it (§4.6.1).

using UnityEngine;

namespace TacticalDirector.ShotMechanics
{
    /// <summary>
    /// All data required to execute a shot. Created by Decision Tree (#8), consumed by ShotExecutor.
    /// Owned by Shot Mechanics #6. Authoritative definition here; DT must not define its own copy.
    /// Lifetime: single shot execution cycle (created → consumed → discarded). §2.4.1, §4.6.1.
    /// </summary>
    public struct ShotRequest
    {
        /// <summary>Unique ID of the shooting agent. Must have ball possession (validated in §3.1, VR-01).</summary>
        public int AgentId;

        /// <summary>
        /// Physical intent — how hard the agent attempts to strike the ball.
        /// [0.0 = minimal force, 1.0 = maximum effort]. Drives velocity (§3.2) and windup duration (§3.9).
        /// </summary>
        public float PowerIntent;

        /// <summary>
        /// Where on the ball the foot contacts. Determines BaseAngle (§3.3), SpinBase (§3.4),
        /// ContactZone modifier (§3.2). NOT a Vector2 — three discrete values only (KD-7, §1.4).
        /// </summary>
        public ContactZone ContactZone;

        /// <summary>
        /// Degree of deliberate spin application [0.0 = pure power, 1.0 = maximum curl/chip spin].
        /// Combined with ContactZone drives spin and launch angle (§3.3, §3.4).
        /// </summary>
        public float SpinIntent;

        /// <summary>
        /// Intended target on goal mouth — goal-relative normalised coordinates.
        /// u ∈ [0.0, 1.0] (left post → right post from attacker's perspective).
        /// v ∈ [0.0, 1.0] (ground → crossbar). KD-2: error applied here before world-space conversion.
        /// </summary>
        public Vector2 PlacementTarget;

        /// <summary>
        /// True if agent is shooting with non-preferred foot. Set by Decision Tree (#8).
        /// Shot Mechanics applies accuracy and power penalties (§3.8); does not determine preference.
        /// </summary>
        public bool IsWeakFoot;

        /// <summary>
        /// Distance to goal (metres) at request submission. Must be &gt; 0 (VR-07).
        /// Drives Finishing/LongShots sigmoid blend (§3.2.3, KD-1 resolution).
        /// </summary>
        public float DistanceToGoal;

        /// <summary>
        /// Team ID of the shooting agent. Required for pressure scalar computation (§4.4.1).
        /// </summary>
        public int TeamId;

        /// <summary>
        /// Simulation frame number. Required for deterministic hash seed: matchSeed + agentId + frameNumber → error direction.
        /// Must be &gt; 0 (VR-08). §3.6, KD-4.
        /// </summary>
        public int FrameNumber;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-27 | —      | Initial implementation. |
#endregion
