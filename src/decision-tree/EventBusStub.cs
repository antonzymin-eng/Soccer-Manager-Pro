// File:     src/decision-tree/EventBusStub.cs
// Created:  2026-05-29
// Modified: 2026-05-30
// Author:   —
// Spec:     Decision Tree #8 §4.5, Event System #17 §3.2.1, Code Standards #20
// Purpose:  Thin forwarding wrapper over EventBus. Replaces the Stage 0 no-op stub.
//           Kept internal (single event type, internal callers only).

using TacticalDirector.EventSystem;

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// Forwards DecisionMadeEvent to the real Event System #17 EventBus (Tier C).
    /// Decision Tree #8 §4.5.
    /// </summary>
    internal static class EventBusStub
    {
        /// <summary>Publishes DecisionMadeEvent to CosmeticChannel (Tier C, channel dt.decision_made).</summary>
        internal static void Publish(in DecisionMadeEvent evt)
            => EventBus.Publish(in evt);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                         |
// | 1.0     | 2026-05-29 | —      | Initial implementation.                                       |
// | 1.1     | 2026-05-30 | —      | Stage 1: replaced no-op with EventBus.Publish forwarding.     |
#endregion
