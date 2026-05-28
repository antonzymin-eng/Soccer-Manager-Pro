// File:     src/goalkeeper-mechanics/EventBusStub.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §4.3, Event System #17 §3.2.1, Code Standards #20
// Purpose:  Stage 0 no-op event bus stub. Replace — do not remove — at Stage 1 with Event System #17.
//           Handles SaveAttemptedEvent, BallClaimedEvent, DistributionExecutedEvent, GoalkeeperRushEvent.

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// Stage 0 no-op event bus. Accepts struct events and discards them.
    /// Replace — do not remove — at Stage 1 with real Event System #17 (§4.3).
    /// Goalkeeper Mechanics #11 §4.3.
    /// </summary>
    public static class EventBusStub
    {
        /// <summary>
        /// Accepts any struct event and discards it.
        /// Stage 0 no-op: compiles to nothing in all build configurations.
        /// Replace with Event System #17 at Stage 1 (§4.3).
        /// </summary>
        public static void Publish<T>(in T evt) where T : struct
        {
            // Stage 0 no-op. Logging removed: string interpolation allocates on the hot path (FR-CS-031).
            // Replace with Event System #17 at Stage 1 (§4.3).
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
