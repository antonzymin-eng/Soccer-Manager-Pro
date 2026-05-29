// File:     src/pressing-ai/CoverShadow.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Pressing AI #13 §3.4–§3.5, Code Standards #20
// Purpose:  Value struct describing one cover-shadow assignment: the shadowing
//           defender and the shadow-lane target position.

using UnityEngine;

namespace TacticalDirector.PressingAI
{
    /// <summary>
    /// One cover-shadow assignment.
    /// Pairs a defending agent (EntityId) with the shadow-lane world-space target
    /// computed from §3.5 geometry. Pressing AI #13 §3.4–§3.5.
    /// </summary>
    public struct CoverShadow
    {
        /// <summary>EntityId of the defending agent assigned to this shadow lane.</summary>
        public int DefenderId;

        /// <summary>EntityId of the opposing receiver being shadowed.</summary>
        public int ReceiverId;

        /// <summary>World-space (X, Y) target position for the shadowing defender. §3.5.</summary>
        public Vector2 TargetPosition;

        /// <summary>
        /// Initialises a cover-shadow assignment.
        /// </summary>
        /// <param name="defenderId">Defending agent EntityId.</param>
        /// <param name="receiverId">Receiver agent EntityId being shadowed.</param>
        /// <param name="targetPosition">Shadow-lane target in world space (X, Y).</param>
        public CoverShadow(int defenderId, int receiverId, Vector2 targetPosition)
        {
            DefenderId = defenderId;
            ReceiverId = receiverId;
            TargetPosition = targetPosition;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
