// File:     src/pass-mechanics/GoalkeeperDeliveryVariant.cs
// Created:  2026-09-26
// Author:   —
// Spec:     Pass Mechanics #5 §2.4.4, §3.8.13; Code Standards #20
// Purpose:  Dedicated goalkeeper-distribution delivery discriminator. This is not PassType.

namespace TacticalDirector.PassMechanics
{
    /// <summary>Dedicated goalkeeper-distribution physical-profile selector. Serialized ordinals are APPEND-only.</summary>
    public enum GoalkeeperDeliveryVariant : byte
    {
        /// <summary>Ground-profile hand roll.</summary>
        Roll = 0,
        /// <summary>Driven-profile hand throw.</summary>
        Throw = 1,
        /// <summary>Lofted-profile punt/volley kick.</summary>
        Kick = 2
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-09-26 | —      | W8 B dedicated delivery discriminator; maps Roll/Throw/Kick to Ground/Driven/Lofted #5 profiles. |
#endregion
