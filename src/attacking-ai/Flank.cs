// File:     src/attacking-ai/Flank.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Attacking AI #15 §3.8, §2.2.1, Code Standards #20
// Purpose:  Two-value lateral flank discriminator used in AttackDirective.overloadFlank.
//           Identifies which half of the pitch an overload is occurring on (§3.8).
//           Not a movement-pattern enum; does not enter the physics pipeline (KD-8).

namespace TacticalDirector.AttackingAI
{
    /// <summary>
    /// Lateral flank side. Spatial discriminator — identifies the near-touchline half of
    /// the pitch where an overload is detected. Attacking AI #15 §3.8 / §2.2.1.
    /// Meaningful only when <see cref="AttackDirective.OverloadActive"/> is true.
    /// </summary>
    public enum Flank : byte
    {
        /// <summary>Ball-side is on the y=0 touchline half (ball.y &lt; PITCH_WIDTH_M/2).</summary>
        Left  = 0,

        /// <summary>Ball-side is on the y=68 touchline half (ball.y >= PITCH_WIDTH_M/2).</summary>
        Right = 1,
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
