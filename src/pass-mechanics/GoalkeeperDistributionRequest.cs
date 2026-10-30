// File:     src/pass-mechanics/GoalkeeperDistributionRequest.cs
// Created:  2026-09-26
// Author:   —
// Spec:     Pass Mechanics #5 §2.4.4, §3.8.13; Code Standards #20
// Purpose:  Dedicated #11 -> #5 goalkeeper-distribution request.

using UnityEngine;

namespace TacticalDirector.PassMechanics
{
    /// <summary>Dedicated #11 → #5 goalkeeper-distribution request captured at B execution start.</summary>
    public struct GoalkeeperDistributionRequest
    {
        /// <summary>Distributing goalkeeper agent id.</summary>
        public int AgentId;
        /// <summary>Goalkeeper team id.</summary>
        public int TeamId;
        /// <summary>Dedicated Roll/Throw/Kick delivery discriminator.</summary>
        public GoalkeeperDeliveryVariant Delivery;
        /// <summary>Committed receiver id, or <see cref="PassMechanicsConstants.AGENT_ID_NONE"/> for a zone target.</summary>
        public int TargetAgentId;
        /// <summary>Committed fallback target; a live eligible receiver is re-aimed at CONTACT.</summary>
        public Vector3 TargetPosition;
        /// <summary>Already-composed power intent in [0,1].</summary>
        public float EmittedPower01;
        /// <summary>Requested spin passed through to the ball kick.</summary>
        public Vector3 SpinIntent;
        /// <summary>Release height above the pitch plane in metres.</summary>
        public float ReleaseHeightM;
        /// <summary>Single #5-owned windup duration in 60 Hz frames.</summary>
        public int WindupFrames;
        /// <summary>Commit frame used by the deterministic error-direction hash.</summary>
        public int FrameNumber;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-09-26 | —      | W8 B dedicated goalkeeper-distribution request. Distinct from ordinary PassRequest; serialized in snapshot schema v24. |
#endregion
