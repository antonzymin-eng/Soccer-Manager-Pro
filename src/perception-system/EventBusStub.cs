// File:     src/perception-system/EventBusStub.cs
// Created:  2026-05-28
// Modified: 2026-05-30
// Author:   —
// Spec:     Perception System #7 §4.6.5, Event System #17 §3.2.1, Code Standards #20
// Purpose:  Thin forwarding wrapper over EventBus. Replaces the Stage 0 no-op stub.

using TacticalDirector.EventSystem;

namespace TacticalDirector.PerceptionSystem
{
    /// <summary>
    /// Forwards publish calls to the real Event System #17 EventBus.
    /// Replaces the Stage 0 no-op. Perception System #7 §4.6.5.
    /// </summary>
    public static class EventBusStub
    {
        /// <summary>
        /// Publishes an event. Tier routing (A/B Events-phase enqueue vs C immediate
        /// CosmeticChannel dispatch) is resolved by EventBus from the event's tier marker
        /// (ERR-017-002 single-method dispatch — C# forbids constraint-only overloads,
        /// CS0111). §3.2.1.
        /// </summary>
        public static void Publish<T>(in T evt) where T : struct
            => EventBus.Publish(in evt);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
// | 1.1     | 2026-05-30 | —      | Stage 1: replaced no-op with three-tier forwarding to EventBus. |
// | 1.2     | 2026-06-12 | —      | ERR-017-002: three constraint-only Publish overloads (CS0111 -  |
// |         |            |        | constraints are not part of a method signature; never compiled) |
// |         |            |        | merged into one where T : struct forwarder; tier routing now    |
// |         |            |        | resolved inside EventBus via cached marker dispatch. Call sites |
// |         |            |        | unchanged.                                                      |
#endregion
