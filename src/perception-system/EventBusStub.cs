// File:     src/perception-system/EventBusStub.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Perception System #7 §4.6.5, Code Standards #20
// Purpose:  Stage 0 no-op event bus stub. Accepts all events; performs no dispatch.
//           Pattern consistent with Pass Mechanics §4.6.3 and Shot Mechanics §4.7.3.
//           Replace — do not remove — at Stage 1 with Event System #17 integration.

using UnityEngine;

namespace TacticalDirector.PerceptionSystem
{
    /// <summary>
    /// Stage 0 event bus stub — no-op receiver for all Perception System events.
    /// In DEVELOPMENT builds: logs event type name to console.
    /// In RELEASE builds: compiles to no-op.
    /// Perception System #7 §4.6.5.
    /// </summary>
    public static class EventBusStub
    {
        /// <summary>
        /// Accepts any struct event. Performs no dispatch. Validates struct is non-default.
        /// Stage 0: no consumer; Stage 1: replace with Event System #17 publish call.
        /// </summary>
        public static void Publish<T>(in T evt) where T : struct
        {
#if DEVELOPMENT_BUILD
            Debug.Log($"[EventBus STUB] {typeof(T).Name} published");
#endif
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
