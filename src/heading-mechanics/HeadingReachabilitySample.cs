// File:     src/heading-mechanics/HeadingReachabilitySample.cs
// Created:  2026-10-10
// Modified: 2026-10-10
// Author:   —
// Spec:     Heading Mechanics #10 §4.6; issue #441 counters (docs/tracking/header-reachability-441-counters.md)
// Purpose:  Observation-only carrier for the #441 header-reachability instrument. Built only when a
//           test attaches HeadingMechanics.TestOnly_ReachabilityObserver; never serialized, digested
//           or read by simulation code.

using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;

namespace TacticalDirector.HeadingMechanics
{
    /// <summary>Lifecycle point at which a #441 reachability sample was taken.</summary>
    internal enum HeadingReachabilityKind
    {
        /// <summary>CommitIntent accepted an intent. <see cref="HeadingReachabilitySample.WasActive"/> marks an overwrite.</summary>
        Commit,

        /// <summary>CancelIntent called. <see cref="HeadingReachabilitySample.WasActive"/> marks a live intent.</summary>
        Cancel,

        /// <summary>The first aerial frame set JumpStartFrame.</summary>
        JumpStart,

        /// <summary>Pass 1 evaluated eligibility and the intent stays live this frame.</summary>
        Pending,

        /// <summary>Pass 1 reached the predicted contact frame and prepared real Head geometry.</summary>
        Prepared,

        /// <summary>A failed attempt was emitted; see <see cref="HeadingReachabilitySample.Cause"/>.</summary>
        Failed,

        /// <summary>The jump landed with no terminal event.</summary>
        LandingDrop,

        /// <summary>Pass 2 applied the header to the ball.</summary>
        Executed,
    }

    /// <summary>
    /// One #441 observation. Fields that a lifecycle point does not have are left at their defaults
    /// (for example, Commit carries no agent state). Observation only; Heading Mechanics #10 §4.6.
    /// </summary>
    internal readonly struct HeadingReachabilitySample
    {
        internal readonly HeadingReachabilityKind Kind;
        internal readonly int AgentId;
        internal readonly int Frame;
        internal readonly bool WasActive;
        internal readonly AgentState Agent;
        internal readonly BallState Ball;
        internal readonly float HeadZ;
        internal readonly int JumpStartFrame;
        internal readonly float JumpReachM;
        internal readonly EligibilityResult Eligibility;
        internal readonly FailureCause Cause;

        internal HeadingReachabilitySample(
            HeadingReachabilityKind kind,
            int agentId,
            int frame,
            bool wasActive,
            in AgentState agent,
            in BallState ball,
            float headZ,
            int jumpStartFrame,
            float jumpReachM,
            in EligibilityResult eligibility,
            FailureCause cause)
        {
            Kind = kind;
            AgentId = agentId;
            Frame = frame;
            WasActive = wasActive;
            Agent = agent;
            Ball = ball;
            HeadZ = headZ;
            JumpStartFrame = jumpStartFrame;
            JumpReachM = jumpReachM;
            Eligibility = eligibility;
            Cause = cause;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-10-10 | —      | #441: observation-only reachability carrier; never serialized.     |
#endregion
