// File:     src/event-system/UiNotificationCue.cs
// Created:  2026-05-30
// Modified: 2026-05-30
// Author:   —
// Spec:     Event System #17 §2.4.2, Appendix A row 0x0B, Code Standards #20
// Purpose:  Tier C cosmetic cue published when the UI layer should display a notification.
//           Ordinal 0x0B; no ring-buffer entry; immediate dispatch via CosmeticChannel.
//           MaxPerTick=32 (deterministic drop predicate; §3.6.2).

using System.Runtime.InteropServices;

namespace TacticalDirector.EventSystem
{
    /// <summary>
    /// Published by game systems to request that the UI display a transient notification.
    /// Tier C: immediate dispatch via CosmeticChannel; not included in digest (FR-EVT-011).
    /// Ordinal <c>0x0B</c>. MaxPerTick=32.
    /// Event System #17 Appendix A / §2.4.2.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct UiNotificationCue : IEventC
    {
        // ── Payload fields (Appendix A row 0x0B) ─────────────────────────────────────
        /// <summary>Notification kind ordinal (domain-specific enum; e.g., GoalScored/CardIssued/Substitution).</summary>
        public readonly byte NotificationKind;
        /// <summary>EntityId of the primary subject; -1 if no specific entity (e.g., match-level notification).</summary>
        public readonly int SubjectEntity;

        /// <summary>
        /// Constructs a <see cref="UiNotificationCue"/>.
        /// </summary>
        public UiNotificationCue(byte notificationKind, int subjectEntity)
        {
            NotificationKind = notificationKind;
            SubjectEntity    = subjectEntity;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-30 | —      | Initial implementation. |
#endregion
