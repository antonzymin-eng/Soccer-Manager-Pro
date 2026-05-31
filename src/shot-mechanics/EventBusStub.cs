// File:     src/shot-mechanics/EventBusStub.cs
// Created:  2026-05-27
// Modified: 2026-05-30
// Author:   —
// Spec:     Shot Mechanics #6 §4.7.3, Event System #17 §3.2.1, Code Standards #20
// Purpose:  Thin forwarding wrapper over EventBus. Replaces the Stage 0 no-op stub.
//           Callers need no changes — same API surface, now produces real event dispatch.

using TacticalDirector.EventSystem;

namespace TacticalDirector.ShotMechanics
{
    /// <summary>
    /// Forwards publish calls to the real Event System #17 EventBus.
    /// Replaces the Stage 0 no-op. Shot Mechanics #6 §4.7.3.
    /// </summary>
    public static class EventBusStub
    {
        /// <summary>Publishes a Tier A event. Enqueued for Events-phase delivery. §3.2.1.</summary>
        public static void Publish<T>(in T evt) where T : struct, IEventA
            => EventBus.Publish(in evt);

        /// <summary>Publishes a Tier B event. Enqueued for Events-phase delivery. §3.2.1.</summary>
        public static void Publish<T>(in T evt) where T : struct, IEventB
            => EventBus.Publish(in evt);

        /// <summary>Publishes a Tier C cosmetic event. Immediate dispatch via CosmeticChannel. §3.2.1.</summary>
        public static void Publish<T>(in T evt) where T : struct, IEventC
            => EventBus.Publish(in evt);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                    |
// | 1.0     | 2026-05-27 | —      | Initial implementation. Pattern reused from Pass Mechanics EventBusStub. |
// | 1.1     | 2026-05-30 | —      | Stage 1: replaced no-op with three-tier forwarding to EventBus.          |
#endregion
