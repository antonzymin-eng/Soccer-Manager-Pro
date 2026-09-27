// File:     src/pass-mechanics/IPassAgentQuery.cs
// Created:  2026-05-26
// Modified: 2026-09-26 (W8 B — CONTACT receiver eligibility / own-goal-line / fallback queries)
// Modified: 2026-05-26
// Author:   —
// Spec:     Pass Mechanics #5 §4.3, Code Standards #20
// Purpose:  IPassAgentQuery interface: Pass Mechanics' read-only view of Agent
//           Movement attributes and state. Data structs in own files (H5 fix).

using UnityEngine;

namespace TacticalDirector.PassMechanics
{
    /// <summary>
    /// Pass Mechanics' read-only interface to Agent Movement data.
    /// Consumed by PassExecutor at INITIATING state. Pass Mechanics #5 §4.3.
    /// </summary>
    public interface IPassAgentQuery
    {
        /// <summary>
        /// Returns the pass-relevant attributes for the given agent.
        /// Pass Mechanics #5 §4.3.1.
        /// </summary>
        PassAgentAttributes GetAttributes(int agentId);

        /// <summary>
        /// Returns the current position, velocity, and facing direction for the given agent.
        /// Pass Mechanics #5 §4.3.2.
        /// </summary>
        PassAgentState GetState(int agentId);

        /// <summary>CONTACT-time eligibility for a goalkeeper-distribution receiver.</summary>
        bool IsEligibleGoalkeeperDistributionReceiver(int agentId, int teamId);

        /// <summary>True when the point lies on this team's current own goal line.</summary>
        bool IsGoalkeeperDistributionOwnGoalLine(int teamId, Vector3 targetPosition);

        /// <summary>Returns the host-owned deterministic receiverless fallback zone for this team.</summary>
        Vector3 GetGoalkeeperDistributionFallbackPosition(int teamId);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                     |
// | 1.0     | 2026-05-26 | —      | Initial implementation (combined with PassAgentAttributes, PassAgentState). |
// | 1.1     | 2026-05-26 | —      | H5: PassAgentAttributes and PassAgentState moved to own files.            |
// | 1.2     | 2026-09-26 | —      | W8 B: + CONTACT-time goalkeeper-distribution receiver eligibility, own-goal-line safety predicate and host-owned deterministic receiverless fallback position. |
#endregion
