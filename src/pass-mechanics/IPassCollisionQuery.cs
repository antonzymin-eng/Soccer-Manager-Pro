// File:     src/pass-mechanics/IPassCollisionQuery.cs
// Created:  2026-05-26
// Modified: 2026-05-26
// Author:   —
// Spec:     Pass Mechanics #5 §4.4, Code Standards #20
// Purpose:  Pass Mechanics' interface to Collision System for tackle interrupt
//           polling (§4.4.2) and pressure scalar computation (§4.4.1).

using UnityEngine;

namespace TacticalDirector.PassMechanics
{
    /// <summary>
    /// Pass Mechanics' interface to Collision System #3.
    /// Provides tackle-interrupt polling and pressure-scalar computation.
    /// Pass Mechanics #5 §4.4.
    /// </summary>
    public interface IPassCollisionQuery
    {
        /// <summary>
        /// Polls and atomically clears the per-agent tackle-interrupt flag.
        /// Returns true if a tackle was registered since the last poll.
        /// Called at the start of every WINDUP frame (§4.4.2, §3.8.5).
        /// Pass Mechanics #5 §4.4.2.
        /// </summary>
        bool GetAndClearTackleFlag(int agentId);

        /// <summary>
        /// Computes the pressure scalar [0, 1] for the passer from nearby opponents.
        /// Uses the Collision System spatial hash (§4.4.1).
        /// Called once at CONTACT state — captures pressure at the moment of kick.
        /// Pass Mechanics #5 §3.5.6, §4.4.1.
        /// </summary>
        float ComputePressureScalar(Vector2 passerPosition, int passerTeamId);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                  |
// | 1.0     | 2026-05-26 | —      | Initial implementation. |
#endregion
