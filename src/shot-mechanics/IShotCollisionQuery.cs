// File:     src/shot-mechanics/IShotCollisionQuery.cs
// Created:  2026-05-27
// Modified: 2026-05-27
// Author:   —
// Spec:     Shot Mechanics #6 §4.4, Code Standards #20
// Purpose:  Shot Mechanics' interface to Collision System: tackle interrupt polling (§4.4.2)
//           and pressure scalar computation (§4.4.1). Patterns reused from Pass Mechanics §4.4.

using UnityEngine;

namespace TacticalDirector.ShotMechanics
{
    /// <summary>
    /// Shot Mechanics' interface to Collision System #3.
    /// Provides tackle-interrupt polling and pressure-scalar computation.
    /// Shot Mechanics #6 §4.4.
    /// </summary>
    public interface IShotCollisionQuery
    {
        /// <summary>
        /// Polls and atomically clears the per-agent tackle-interrupt flag.
        /// Returns true if a tackle was registered since the last poll.
        /// Called at the start of every WINDUP frame (§4.4.2).
        /// Shot Mechanics #6 §4.4.2.
        /// </summary>
        bool GetAndClearTackleFlag(int agentId);

        /// <summary>
        /// Computes the pressure scalar [0, 1] from nearby opponents within PressureRadiusMax.
        /// Uses the Collision System spatial hash (§4.4.1) with a caller-side distance filter
        /// workaround (ERR-011: spatial hash ignores radius, returns fixed ±1.5m neighbourhood).
        /// Called once at CONTACT state — captures pressure at the moment of ball contact.
        /// Shot Mechanics #6 §4.4.1.
        /// </summary>
        float ComputePressureScalar(Vector3 shooterPosition, int shooterTeamId);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-27 | —      | Initial implementation. |
#endregion
