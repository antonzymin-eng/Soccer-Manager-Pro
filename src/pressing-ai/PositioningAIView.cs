// File:     src/pressing-ai/PositioningAIView.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Pressing AI #13 §4.2, Code Standards #20
// Purpose:  Read-only facade wrapping a PositioningAITick reference. Exposes only
//           the accessors needed by PressingAITick without leaking the full API.

using UnityEngine;

using TacticalDirector.PositioningAI;

namespace TacticalDirector.PressingAI
{
    /// <summary>
    /// Lightweight read-only wrapper around <see cref="PositioningAITick"/>.
    /// Pressing AI uses this facade to query formation slots and phase without
    /// accessing the full Positioning AI API surface. Pressing AI #13 §4.2.
    /// </summary>
    public readonly struct PositioningAIView
    {
        private readonly PositioningAITick _tick;

        /// <summary>Wraps an existing <see cref="PositioningAITick"/> instance.</summary>
        /// <param name="tick">The Positioning AI tick object to wrap. Must not be null.</param>
        public PositioningAIView(PositioningAITick tick)
        {
            _tick = tick;
        }

        /// <summary>Returns the committed formation slot (X, Y) for the given EntityId.</summary>
        /// <param name="entityId">Agent entity identifier.</param>
        public Vector2 GetFormationSlot(int entityId) => _tick.GetFormationSlot(entityId);

        /// <summary>Returns the committed line assignment for the given EntityId.</summary>
        /// <param name="entityId">Agent entity identifier.</param>
        public LineId GetLine(int entityId) => _tick.GetLine(entityId);

        /// <summary>Returns the committed team phase from the last Positioning AI tick.</summary>
        public Phase GetPhase() => _tick.GetPhase();

        /// <summary>Returns true when the slot is the inactive-agent sentinel (−∞, −∞).</summary>
        /// <param name="slot">Slot value returned by <see cref="GetFormationSlot"/>.</param>
        public static bool IsSentinelSlot(Vector2 slot) => PositioningAITick.IsSentinelSlot(slot);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
