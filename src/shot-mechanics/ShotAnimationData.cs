// File:     src/shot-mechanics/ShotAnimationData.cs
// Created:  2026-05-27
// Modified: 2026-05-27
// Author:   —
// Spec:     Shot Mechanics #6 §2.4.4, Code Standards #20
// Purpose:  Stage 0 stub struct populated by ShotExecutor at CONTACT state.
//           Not consumed by any Stage 0 system. Reserved for Animation System (Stage 1+).
//           Must not be removed during Stage 0 implementation. §2.4.4.

namespace TacticalDirector.ShotMechanics
{
    /// <summary>
    /// Animation data stub populated at CONTACT state. Unconsumed at Stage 0.
    /// Animation System (Stage 1+) subscribes to event bus for this struct.
    /// Shot Mechanics populates and publishes it; Animation System owns consumption.
    /// Shot Mechanics #6 §2.4.4.
    /// </summary>
    public struct ShotAnimationData
    {
        /// <summary>Agent who took the shot.</summary>
        public int AgentId;

        /// <summary>Contact zone — Animation System selects clip based on zone.</summary>
        public ContactZone ContactZone;

        /// <summary>Power intent [0.0, 1.0] — drives animation blend (slow → explosive windup).</summary>
        public float PowerIntent;

        /// <summary>Body mechanics score [0.0, 1.0] — poor mechanics → unbalanced animation variant.</summary>
        public float BodyMechanicsScore;

        /// <summary>True if weak foot — selects foot-side animation variant.</summary>
        public bool IsWeakFoot;

        /// <summary>Windup duration in frames — allows Animation System to sync clip timing.</summary>
        public int WindupFrames;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-27 | —      | Initial implementation. |
#endregion
