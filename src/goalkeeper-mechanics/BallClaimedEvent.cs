// File:     src/goalkeeper-mechanics/BallClaimedEvent.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §2.2.4, §4.3, Event System #17 §3.2.1, Code Standards #20
// Purpose:  Struct event published when the GK successfully catches the ball
//           (handlingQualityScalar ≥ CATCH_THRESHOLD) or wins a cross-claim duel.

using UnityEngine;

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// Published when a GK successfully gains possession (catches ball or wins cross-claim duel).
    /// Triggers Ball.SetPossessor and begins the distribution intent window.
    /// Goalkeeper Mechanics #11 §2.2.4 / §4.3.
    /// </summary>
    public struct BallClaimedEvent
    {
        /// <summary>Unique GK agent ID. §2.2.4.</summary>
        public int AgentId;

        /// <summary>Match time (ms) at the claim frame. §2.2.4.</summary>
        public float MatchTimeMs;

        /// <summary>Handling quality scalar [0, 1] that triggered the catch band. §3.5.</summary>
        public float HandlingQualityScalar;

        /// <summary>Telemetry classification of how the claim was made. Not consumed by physics (KD-2). §2.2.4.</summary>
        public ClaimType ClaimType;

        /// <summary>World-space position of the hand at the claim frame. §2.2.4.</summary>
        public Vector3 ClaimPosition;

        /// <summary>Body part used for the claim (normally Hand; Head routes through Heading #10). §3.6.1.</summary>
        public BodyPartEnum ContactBodyPart;

        /// <summary>Duel ID if this claim resolved a contested duel; -1 for uncontested. §3.6.</summary>
        public int ContestedDuelId;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
