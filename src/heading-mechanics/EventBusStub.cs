// File:     src/heading-mechanics/EventBusStub.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Heading Mechanics #10 §4.3, Event System #17 §3.2.1, Code Standards #20
// Purpose:  Stage 0 no-op event bus stub. Replace — do not remove — at Stage 1 with Event System #17.

using UnityEngine;

namespace TacticalDirector.HeadingMechanics
{
    /// <summary>
    /// Stage 0 no-op event bus. Accepts struct events and discards them.
    /// Replace — do not remove — at Stage 1 with real Event System #17 (§4.3).
    /// Heading Mechanics #10 §4.3.
    /// </summary>
    public static class EventBusStub
    {
        /// <summary>
        /// Accepts any struct event. In DEVELOPMENT_BUILD or UNITY_EDITOR logs the type name.
        /// In all other builds compiles to a no-op.
        /// </summary>
        public static void Publish<T>(in T evt) where T : struct
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[EventBus STUB] {typeof(T).Name}");
#endif
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                    |
// | 1.0     | 2026-05-28 | —      | Initial implementation. Pattern reused from Shot Mechanics EventBusStub. |
#endregion
