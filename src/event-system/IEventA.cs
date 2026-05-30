// File:     src/event-system/IEventA.cs
// Created:  2026-05-30
// Modified: 2026-05-30
// Author:   —
// Spec:     Event System #17 §3.1.3, §4.2.1, Code Standards #20
// Purpose:  Tier A marker interface. Constrains EventBus.Publish<T> and Subscribe<T>
//           overloads that route through the authoritative ring buffer and digest ledger.

namespace TacticalDirector.EventSystem
{
    /// <summary>
    /// Tier A event marker — authoritative, state-changing, included in the per-tick digest.
    /// Implement exactly one tier marker per event struct (FR-EVT-009a). §4.2.1.
    /// Event System #17 §3.1.3 / §4.2.1.
    /// </summary>
    public interface IEventA { }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-30 | —      | Initial implementation. |
#endregion
