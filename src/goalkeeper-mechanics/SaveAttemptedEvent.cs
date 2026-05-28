// File:     src/goalkeeper-mechanics/SaveAttemptedEvent.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §2.2.4, §4.3, Event System #17 §3.2.1, Code Standards #20
// Purpose:  Struct event published on every save attempt (successful or failed).
//           Published via EventBusStub.Publish<SaveAttemptedEvent> at §3.5 resolution.

using UnityEngine;

using TacticalDirector.BallPhysics;

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// Published when a GK save attempt is resolved (any outcome, including Missed).
    /// Consumers: telemetry, Positioning AI #12, Decision Tree #8 (for next intent).
    /// Goalkeeper Mechanics #11 §2.2.4 / §4.3.
    /// </summary>
    public struct SaveAttemptedEvent
    {
        /// <summary>Unique GK agent ID. Matches the AM #2 agentId. §2.2.4.</summary>
        public int AgentId;

        /// <summary>Match time (ms) at the contact frame. §2.2.4.</summary>
        public float MatchTimeMs;

        /// <summary>Handling quality scalar [0, 1] computed by §3.5. §2.2.4.</summary>
        public float HandlingQualityScalar;

        /// <summary>Telemetry label for the outcome band (KD-2; not consumed by physics). §2.2.4.</summary>
        public HandlingQualityLabel HandlingLabel;

        /// <summary>Reaction quality scalar [0, 1] from §3.2.3. §2.2.4.</summary>
        public float ReactionWindowAchieved;

        /// <summary>Telemetry label for reaction quality (KD-2; not consumed by physics). §2.2.4.</summary>
        public ReactionLabel ReactionLabel;

        /// <summary>Ball state snapshot at the contact frame. §2.2.4.</summary>
        public BallState IncomingBallState;

        /// <summary>Contact point error in hand-local metres. §2.2.4 / GkContactState.ContactPointError.</summary>
        public Vector2 ContactPointError;

        /// <summary>Failure cause when HandlingLabel == Missed; default otherwise. §2.2.4.</summary>
        public FailureCause FailureCause;

        /// <summary>World-space position of the hand at contact frame. §2.2.4.</summary>
        public Vector3 HandContactPosition;

        /// <summary>Which hand was used (anatomy lookup; not a physics input per KD-1). §2.2.4.</summary>
        public HandEnum HandUsed;

        /// <summary>Body part that made contact (Hand for normal saves; Head routes through Heading #10). §3.6.1.</summary>
        public BodyPartEnum ContactBodyPart;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
