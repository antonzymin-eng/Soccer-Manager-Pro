// File:     src/pressing-ai/TriggerFlags.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Pressing AI #13 §3.1, Code Standards #20
// Purpose:  Bitmask enum for the four canonical press triggers.

using System;

namespace TacticalDirector.PressingAI
{
    /// <summary>
    /// Bitmask of active press triggers. Each flag corresponds to one of the
    /// four canonical trigger conditions defined in §3.1.
    /// Pressing AI #13 §3.1 / §3.2.
    /// </summary>
    [Flags]
    public enum TriggerFlags : byte
    {
        /// <summary>No trigger active.</summary>
        None = 0,

        /// <summary>Ball-carrier produced a bad first touch (loose ball, high escape velocity). §3.1.1.</summary>
        BadTouch = 1 << 0,

        /// <summary>Opposing team played a backward pass. §3.1.2.</summary>
        BackwardPass = 1 << 1,

        /// <summary>Ball-carrier is facing toward a nearby touchline. §3.1.3.</summary>
        SidelineTrap = 1 << 2,

        /// <summary>An opposing receiver has low first-touch skill and is under low pressure. §3.1.4.</summary>
        WeakReceiver = 1 << 3,
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
