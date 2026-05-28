// File:     src/shot-mechanics/EventBusStub.cs
// Created:  2026-05-27
// Modified: 2026-05-27
// Author:   —
// Spec:     Shot Mechanics #6 §4.7.3, Code Standards #20
// Purpose:  Stage 0 no-op event bus stub. Identical in structure to Pass Mechanics §4.6.3.
//           Replace — do not remove — at Stage 1 with real Event System #17. §4.7.3.

using UnityEngine;

namespace TacticalDirector.ShotMechanics
{
    /// <summary>
    /// Stage 0 no-op event bus. Accepts struct events and discards them.
    /// Replace — do not remove — at Stage 1 with real Event System #17 (§4.7.3).
    /// Shot Mechanics #6 §4.7.3.
    /// </summary>
    public static class EventBusStub
    {
        /// <summary>
        /// Accepts any struct event. In DEVELOPMENT_BUILD or UNITY_EDITOR logs the type name.
        /// In all other builds compiles to a no-op.
        /// </summary>
        public static void Publish<T>(T evt) where T : struct
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[EventBus STUB] {typeof(T).Name}");
#endif
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                    |
// | 1.0     | 2026-05-27 | —      | Initial implementation. Pattern reused from Pass Mechanics EventBusStub. |
#endregion
