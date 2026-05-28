// File:     src/goalkeeper-mechanics/GkContactState.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §2.2.3, Code Standards #20
// Purpose:  Per-attempt mutable contact state tracking for the 60 Hz dive/airborne pipeline.
//           Holds predicted and actual contact-frame indices, quality scalar, contact-point
//           error, and clutch firmness for the current save attempt.

using UnityEngine;

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// Per-attempt contact state tracked through the 60 Hz dive and airborne phases.
    /// Mutated each frame by GoalkeeperDiveKinematics and GoalkeeperHandlingQuality.
    /// Zero-initialised when a new intent is committed. §2.2.3.
    /// Goalkeeper Mechanics #11 §2.2.3.
    /// </summary>
    public struct GkContactState
    {
        /// <summary>60 Hz frame at which the save pipeline predicts contact. Set by §3.3 / §3.2 reaction pipeline. §2.2.3.</summary>
        public int PredictedContactFrame;

        /// <summary>60 Hz frame at which contact actually occurred. Set at the moment of confirmed hand-ball collision. §2.2.3.</summary>
        public int ActualContactFrame;

        /// <summary>
        /// Normalised reaction window achievement [0, 1]. Computed by §3.2.3 each tick during Anticipate/Diving.
        /// 1.0 = GK reacted at exactly the right time; lower = early or late commit.
        /// </summary>
        public float ReactionWindowAchieved;

        /// <summary>Handling quality scalar [0, 1] from §3.5. 0 = complete miss; 1 = perfect catch. §2.2.3.</summary>
        public float HandlingQualityScalar;

        /// <summary>
        /// Contact point error in hand-local metres (2-D: forward / lateral offset from target).
        /// Populated at the contact frame by GoalkeeperHandlingQuality. §2.2.3 / §3.5.
        /// </summary>
        public Vector2 ContactPointError;

        /// <summary>Which hand was committed for this save attempt. §2.2.3 / SaveIntent.TargetHand.</summary>
        public HandEnum HandChoice;

        /// <summary>Clutch firmness [0, 1] from the SaveIntent. Carried through to §3.5 parry helpers. §2.2.3.</summary>
        public float ClutchFirmness;

        /// <summary>
        /// Factory: creates a zero-initialised GkContactState with sentinel values for frame indices.
        /// PredictedContactFrame and ActualContactFrame are set to -1 (unresolved).
        /// </summary>
        public static GkContactState CreateNew()
        {
            return new GkContactState
            {
                PredictedContactFrame  = -1,
                ActualContactFrame     = -1,
                ReactionWindowAchieved = 0.0f,
                HandlingQualityScalar  = 0.0f,
                ContactPointError      = Vector2.zero,
                HandChoice             = HandEnum.Either,
                ClutchFirmness         = 0.0f
            };
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
