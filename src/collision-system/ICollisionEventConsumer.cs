// File:     src/collision-system/ICollisionEventConsumer.cs
// Created:  2026-05-25
// Modified: 2026-05-25
// Author:   —
// Spec:     Collision System #3 §3.4.2, FR-07, Code Standards #20
// Purpose:  Consumer interface for collision events; implemented by Event System (Spec #17),
//           statistics tracker, and replay system.

namespace TacticalDirector.CollisionSystem
{
    /// <summary>
    /// Receives one CollisionEvent per detected collision per frame.
    /// Implemented by downstream systems that consume collision data. Collision System #3 §3.4.2.
    /// Both consumer and producer are specified (FR-CS-048 compliant).
    /// </summary>
    public interface ICollisionEventConsumer
    {
        /// <summary>Called once per confirmed collision per frame, in detection order.</summary>
        void OnCollisionEvent(CollisionEvent evt);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes          |
// | 1.0     | 2026-05-25 | —      | Initial draft. |
#endregion
