// File:     src/first-touch/TouchResult.cs
// Created:  2026-05-25
// Modified: 2026-05-25
// Author:   —
// Spec:     First Touch Mechanics #4 §4.2.1, Code Standards #20
// Purpose:  Enum representing the four possible outcomes of a first-touch evaluation.

namespace TacticalDirector.FirstTouch
{
    /// <summary>
    /// Classification of a first-touch attempt outcome. First Touch Mechanics #4 §4.2.1.
    /// </summary>
    public enum TouchResult
    {
        /// <summary>Ball stays within ControlledRadius of the agent; agent retains possession. §3.4.</summary>
        CONTROLLED = 0,

        /// <summary>Ball displaced beyond ControlledRadius but no interception or deflection; ball is contested. §3.4.2.</summary>
        LOOSE_BALL = 1,

        /// <summary>Ball displaced sharply with momentum retained in original direction; ball exits play zone. §3.4.2.</summary>
        DEFLECTION = 2,

        /// <summary>Nearby opponent gains possession due to a poor touch within their reach. §3.4.2.</summary>
        INTERCEPTION = 3
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes          |
// | 1.0     | 2026-05-25 | —      | Initial draft. |
#endregion
