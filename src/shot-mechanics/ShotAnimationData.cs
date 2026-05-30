// File:     src/shot-mechanics/ShotAnimationData.cs
// Created:  2026-05-27
// Modified: 2026-05-30
// Author:   —
// Spec:     Shot Mechanics #6 §2.4.4, Event System #17 §3.2.1, Code Standards #20
// Purpose:  Tier C cosmetic event struct populated by ShotExecutor at CONTACT state.
//           Ordinal 0x0F; immediate dispatch via CosmeticChannel. Animation System
//           (Stage 1+) subscribes to event bus for this struct. §2.4.4.

using System.Runtime.InteropServices;

using TacticalDirector.EventSystem;

namespace TacticalDirector.ShotMechanics
{
    /// <summary>
    /// Animation data published at CONTACT state via CosmeticChannel (Tier C; no digest entry).
    /// Ordinal 0x0F. Animation System (Stage 1+) subscribes to event bus for this struct.
    /// Shot Mechanics #6 §2.4.4 / Event System #17 Appendix A.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ShotAnimationData : IEventC
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
// | Version | Date       | Author | Notes                                                         |
// | 1.0     | 2026-05-27 | —      | Initial implementation.                                       |
// | 1.1     | 2026-05-30 | —      | Stage 1: added IEventC, [StructLayout(Sequential)].           |
// |         |            |        | Ordinal 0x0F. Event System #17 §3.2.1 wiring.                 |
#endregion
