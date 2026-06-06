// File:     src/pass-mechanics/EventBusStub.cs
// Created:  2026-05-26
// Modified: 2026-05-30
// Author:   —
// Spec:     Pass Mechanics #5 §4.6.3, Event System #17 §3.2.1, Code Standards #20
// Purpose:  Thin forwarding wrapper over EventBus. Replaces the Stage 0 no-op stub.
//           Callers need no changes — same API surface, now produces real event dispatch.

using TacticalDirector.EventSystem;

namespace TacticalDirector.PassMechanics
{
    /// <summary>
    /// Forwards publish calls to the real Event System #17 EventBus.
    /// Replaces the Stage 0 no-op. Pass Mechanics #5 §4.6.3.
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
// | Version | Date       | Author | Notes                                                           |
// | 1.0     | 2026-05-26 | —      | Extracted from PassEvents.cs per one-type-per-file rule (H4).         |
// | 1.1     | 2026-05-27 | —      | AR-1 round-5 L-B: DEVELOPMENT_BUILD || UNITY_EDITOR guard added on    |
// |         |            |        |     the no-op stub's log emit; reverted in v1.2 when the no-op was    |
// |         |            |        |     replaced by the real EventBus forwarding (no log emit remains).   |
// | 1.2     | 2026-05-30 | —      | Stage 1: replaced no-op with three-tier forwarding to EventBus.       |
// | 1.3     | 2026-06-06 | —      | AR-2 L-5: clarified history note above (v1.1 guard does not exist     |
// |         |            |        |     after v1.2 forwarding landed). No code change.                    |
#endregion
