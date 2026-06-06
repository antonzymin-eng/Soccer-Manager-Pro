// File:     src/first-touch/TouchResult.cs
// Created:  2026-05-25
// Modified: 2026-06-06
// Author:   —
// Spec:     First Touch Mechanics #4 §4.2.1, Code Standards #20
// Purpose:  Enum representing the four possible outcomes of a first-touch evaluation.

namespace TacticalDirector.FirstTouch
{
    /// <summary>
    /// Classification of a first-touch attempt outcome. First Touch Mechanics #4 §4.2.1.
    ///
    /// ORDINAL STABILITY: members are APPEND-only. Inserting a new value in the middle
    /// shifts ordinals 1/2/3 and breaks any persisted analytics / replay logs that embed
    /// the int representation. Add new outcomes at the end with the next int value.
    /// </summary>
    public enum TouchResult
    {
        /// <summary>Ball stays within ControlledRadius of the agent; agent retains possession. §3.4.</summary>
        Controlled = 0,

        /// <summary>Ball displaced beyond ControlledRadius but no interception or deflection; ball is contested. §3.4.2.</summary>
        LooseBall = 1,

        /// <summary>Ball displaced sharply with momentum retained in original direction; ball exits play zone. §3.4.2.</summary>
        Deflection = 2,

        /// <summary>Nearby opponent gains possession due to a poor touch within their reach. §3.4.2.</summary>
        Interception = 3
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes          |
// | 1.0     | 2026-05-25 | —      | Initial draft. |
// | 1.1     | 2026-06-06 | —      | AR-5 M-1: enum members renamed ALL_CAPS_SNAKE → PascalCase per FR-CS-001 / Spec #20 §3.2.3 (CONTROLLED→Controlled, LOOSE_BALL→LooseBall, DEFLECTION→Deflection, INTERCEPTION→Interception); ordinal-stability paragraph added parallel to Ball Physics AR-3 L-2 / AR-4 L-1 sweep. |
#endregion
