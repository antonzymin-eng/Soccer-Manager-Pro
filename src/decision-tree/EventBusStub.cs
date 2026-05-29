// File:     src/decision-tree/EventBusStub.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Decision Tree #8 §4.5, Event System #17 §7.2, Code Standards #20
// Purpose:  Stage 0 no-op event bus. Replace at Stage 1 with Event System #17 wiring.
//           Provides the same API surface so callers need no changes at Stage 1.

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// Stage 0 no-op event bus. Accepts DecisionMadeEvent and discards it.
    /// Stage 1+: replace with Event System #17 wiring (channel dt.decision_made).
    /// Decision Tree #8 §4.5.
    /// </summary>
    internal static class EventBusStub
    {
        /// <summary>Stage 0: no-op. Stage 1+: publish to Event System #17 channel dt.decision_made.</summary>
        internal static void Publish(in DecisionMadeEvent evt)
        {
            // no-op at Stage 0
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
