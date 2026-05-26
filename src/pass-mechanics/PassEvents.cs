// File:     src/pass-mechanics/PassEvents.cs
// Created:  2026-05-26
// Modified: 2026-05-26
// Author:   —
// Spec:     Pass Mechanics #5 §3.9, §4.6, Code Standards #20
// Purpose:  PassAttemptEvent and PassCancelledEvent struct definitions and Stage 0
//           no-op EventBusStub. Both events are value types (zero allocation).

using UnityEngine;

namespace TacticalDirector.PassMechanics
{
    /// <summary>
    /// Reason a pass was cancelled before Ball.ApplyKick() was called.
    /// Pass Mechanics #5 §3.9.3, §4.6.1.
    /// </summary>
    public enum CancelReason
    {
        /// <summary>Collision System set the tackle flag during WINDUP state.</summary>
        TackleInterrupt,
        // Additional reasons reserved for Stage 2+ (e.g. StumbleInterrupt).
    }

    /// <summary>
    /// Published at CONTACT state immediately after Ball.ApplyKick() succeeds.
    /// ~100 bytes, all value types — no heap allocation. Pass Mechanics #5 §3.9.2.
    /// Consumed by Statistics Engine (Stage 1) and Replay System (Stage 1).
    /// </summary>
    public struct PassAttemptEvent
    {
        /// <summary>ID of the passing agent.</summary>
        public int AgentId;

        /// <summary>Team of the passing agent.</summary>
        public int TeamId;

        /// <summary>Pass type executed.</summary>
        public PassType PassType;

        /// <summary>Cross sub-type (Flat if non-cross).</summary>
        public CrossSubType CrossSubType;

        /// <summary>Pre-error aim point (world space).</summary>
        public Vector3 TargetPosition;

        /// <summary>Final velocity vector passed to ApplyKick().</summary>
        public Vector3 FinalVelocity;

        /// <summary>Final spin vector passed to ApplyKick().</summary>
        public Vector3 FinalSpin;

        /// <summary>Error magnitude applied in degrees.</summary>
        public float ErrorAngleDeg;

        /// <summary>Scalar launch speed in m/s.</summary>
        public float KickSpeed;

        /// <summary>Through-ball lead distance in metres (0 for player-targeted).</summary>
        public float LeadDistance;

        /// <summary>True if the non-preferred foot was used.</summary>
        public bool IsWeakFoot;

        /// <summary>Target agent ID; -1 for space-targeted passes.</summary>
        public int TargetAgentId;

        /// <summary>Simulation frame at CONTACT.</summary>
        public int Frame;

        /// <summary>Match time in seconds at CONTACT.</summary>
        public float MatchTime;
    }

    /// <summary>
    /// Published when a tackle interrupt cancels the pass during WINDUP state.
    /// Invalid request rejection does NOT produce this event (KD-8, §3.9.3).
    /// Pass Mechanics #5 §3.9.3.
    /// </summary>
    public struct PassCancelledEvent
    {
        /// <summary>ID of the agent whose pass was cancelled.</summary>
        public int AgentId;

        /// <summary>Team of the executing agent.</summary>
        public int TeamId;

        /// <summary>Reason for cancellation (TACKLE_INTERRUPT in Stage 0).</summary>
        public CancelReason CancelReason;

        /// <summary>Pass type that was in progress.</summary>
        public PassType PassType;

        /// <summary>Simulation frame at cancellation.</summary>
        public int Frame;

        /// <summary>Match time in seconds at cancellation.</summary>
        public float MatchTime;
    }

    /// <summary>
    /// Stage 0 no-op event bus. Accepts struct events and discards them.
    /// Replace — do not remove — at Stage 1 with real Event System #17 (§4.6.3).
    /// </summary>
    public static class EventBusStub
    {
        /// <summary>
        /// Accepts any struct event. In DEVELOPMENT_BUILD logs type name and frame.
        /// In all other builds compiles to a no-op.
        /// </summary>
        public static void Publish<T>(T evt) where T : struct
        {
#if DEVELOPMENT_BUILD
            UnityEngine.Debug.Log($"[EventBus STUB] {typeof(T).Name}");
#endif
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                  |
// | 1.0     | 2026-05-26 | —      | Initial implementation. |
#endregion
