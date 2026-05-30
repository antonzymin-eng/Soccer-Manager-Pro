// File:     src/event-system/SubscriptionToken.cs
// Created:  2026-05-30
// Modified: 2026-05-30
// Author:   —
// Spec:     Event System #17 §3.2.2, §4.2.2, Code Standards #20
// Purpose:  Opaque handle returned by Subscribe<T>. Struct (no class allocation; FR-EVT-073).
//           Used by CosmeticChannel.Unsubscribe to remove a Tier C handler.

namespace TacticalDirector.EventSystem
{
    /// <summary>
    /// Opaque subscription handle returned by <c>EventBus.Subscribe</c> and
    /// <c>CosmeticChannel.Subscribe</c>. Struct — zero class allocation (FR-EVT-073).
    /// Pass to <c>CosmeticChannel.Unsubscribe</c> to remove a Tier C handler.
    /// Tier A/B tokens cannot be unsubscribed after the boot phase (FR-EVT-020).
    /// Event System #17 §3.2.2 / §4.2.2.
    /// </summary>
    public readonly struct SubscriptionToken
    {
        /// <summary>Ordinal of the event type this token was issued for.</summary>
        internal readonly byte EventTypeOrdinal;

        /// <summary>Index of the handler slot within the dispatcher's handler array.</summary>
        internal readonly ushort SubscriberIndex;

        internal SubscriptionToken(byte eventTypeOrdinal, ushort subscriberIndex)
        {
            EventTypeOrdinal = eventTypeOrdinal;
            SubscriberIndex  = subscriberIndex;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-30 | —      | Initial implementation. |
#endregion
