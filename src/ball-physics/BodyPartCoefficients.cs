// File:     src/ball-physics/BodyPartCoefficients.cs
// Created:  2026-06-03
// Author:   —
// Spec:     Ball Physics #1, Code Standards #20
// Purpose:  Speed and spin retention coefficients per body part for deflection
//           calculations. One-time-allocated lookup table; Get(BodyPart) throws on
//           unknown enum values to surface cast-from-int programming errors.

using System;
using System.Collections.Generic;

namespace TacticalDirector.BallPhysics
{
    /// <summary>
    /// Speed and spin retention coefficients per body part for deflection calculations.
    /// </summary>
    public static class BodyPartCoefficients
    {
        private static readonly Dictionary<BodyPart, (float speedRetention, float spinRetention)> s_coefficients
            = new Dictionary<BodyPart, (float, float)>
        {
            { BodyPart.Foot,  (BallPhysicsConstants.BodyPartRetention.FootSpeed,  BallPhysicsConstants.BodyPartRetention.FootSpin)  },
            { BodyPart.Shin,  (BallPhysicsConstants.BodyPartRetention.ShinSpeed,  BallPhysicsConstants.BodyPartRetention.ShinSpin)  },
            { BodyPart.Thigh, (BallPhysicsConstants.BodyPartRetention.ThighSpeed, BallPhysicsConstants.BodyPartRetention.ThighSpin) },
            { BodyPart.Torso, (BallPhysicsConstants.BodyPartRetention.TorsoSpeed, BallPhysicsConstants.BodyPartRetention.TorsoSpin) },
            { BodyPart.Head,  (BallPhysicsConstants.BodyPartRetention.HeadSpeed,  BallPhysicsConstants.BodyPartRetention.HeadSpin)  },
            { BodyPart.Arm,   (BallPhysicsConstants.BodyPartRetention.ArmSpeed,   BallPhysicsConstants.BodyPartRetention.ArmSpin)   }
        };

        /// <summary>
        /// Returns (speedRetention, spinRetention) for the given body part.
        /// Throws <see cref="ArgumentOutOfRangeException"/> for unknown enum values
        /// (e.g. <c>default(BodyPart)</c> from a cast-from-int caller) so programming
        /// errors surface immediately instead of being masked by a silent default.
        /// </summary>
        public static (float speedRetention, float spinRetention) Get(BodyPart part)
        {
            if (s_coefficients.TryGetValue(part, out var coef))
                return coef;

            throw new ArgumentOutOfRangeException(
                nameof(part), part, "Unknown BodyPart — extend BodyPartCoefficients when adding a new body part.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-03 | —      | Extracted from BallCollision.cs as part of AR-2 L-2 file split    |
// |         |            |        | (one public type per file, src/CLAUDE.md FILE NAMING). Retains the |
// |         |            |        | AR-1 M-2 (s_coefficients naming) and AR-1 L-4 (throw on unknown)   |
// |         |            |        | fixes verbatim.                                                    |
// | 1.1     | 2026-06-03 | —      | AR-3 M-1: 12 inline (speedRetention, spinRetention) literals       |
// |         |            |        | replaced with references to the new                                |
// |         |            |        | BallPhysicsConstants.BodyPartRetention catalogue block             |
// |         |            |        | (FR-CS-016 — no magic numbers in ball-physics assembly).           |
#endregion
