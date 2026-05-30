// File:     src/event-system/IEventC.cs
// Created:  2026-05-30
// Modified: 2026-05-30
// Author:   —
// Spec:     Event System #17 §3.1.3, §4.2.1, Code Standards #20
// Purpose:  Tier C marker interface. Constrains CosmeticChannel.Publish<T> and Subscribe<T>
//           to the cosmetic immediate-dispatch path. Excluded from digest and SnapshotPayload.

namespace TacticalDirector.EventSystem
{
    /// <summary>
    /// Tier C event marker — cosmetic / observability; immediate synchronous dispatch; excluded from
    /// the determinism digest and SnapshotPayload (FR-EVT-014, FR-EVT-015).
    /// Implement exactly one tier marker per event struct (FR-EVT-009a). §4.2.1.
    /// Event System #17 §3.1.3 / §4.2.1.
    /// </summary>
    public interface IEventC { }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-30 | —      | Initial implementation. |
#endregion
