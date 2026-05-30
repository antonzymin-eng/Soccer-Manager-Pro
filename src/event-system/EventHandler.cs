// File:     src/event-system/EventHandler.cs
// Created:  2026-05-30
// Modified: 2026-05-30
// Author:   —
// Spec:     Event System #17 §3.2.2, §4.2.2, Code Standards #20
// Purpose:  Custom delegate type for all event subscribers. Takes evt by in-reference to avoid
//           boxing; where T : struct constraint satisfies FR-EVT-052 Action<T>/Func<T> exemption.

namespace TacticalDirector.EventSystem
{
    /// <summary>
    /// Subscriber handler delegate. Takes <paramref name="evt"/> by <c>in</c> reference
    /// (no copy, no boxing). All Tier A/B/C subscriber methods MUST match this signature.
    /// The <c>where T : struct</c> constraint satisfies FR-EVT-052's Action/Func exemption:
    /// struct-ref delegates with <c>in T</c> avoid boxing at the call site.
    /// FR-EVT-019 / FR-EVT-078. Event System #17 §3.2.2 / §4.2.2.
    /// </summary>
    public delegate void EventHandler<T>(in T evt) where T : struct;
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-30 | —      | Initial implementation. |
#endregion
