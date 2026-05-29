// File: src/positioning-ai/PositioningPerceptionSnapshot.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec: #12 Positioning AI §4.3, §3.0, §3.2
// Purpose: Pre-allocated perception input consumed by PositioningAITick; filled each 10 Hz tick by the orchestrator.

using UnityEngine;

namespace TacticalDirector.PositioningAI
{
    /// <summary>
    /// Perception data required by the positioning system for one tactical tick.
    /// Allocated once per match by the orchestrator; refilled in-place each tick to avoid heap churn.
    /// Stale detection: if TickIndex &lt; currentTick, PositioningAITick returns previous slots (F1 recovery).
    /// </summary>
    public sealed class PositioningPerceptionSnapshot
    {
        /// <summary>Monotonically increasing tick counter from the 10 Hz loop. Used for F1 stale detection.</summary>
        public int TickIndex;

        /// <summary>Ball world-space position (Z = height per #1 §1.2). Pitch corner-origin.</summary>
        public Vector3 BallPosition;

        /// <summary>
        /// Longitudinal (X-axis) ball velocity filtered over the last few frames (m/s).
        /// Used by PhaseClassifier to distinguish TransToAtk from TransToDef when ball is loose.
        /// Positive = moving toward opponent goal (increasing X).
        /// </summary>
        public float BallVxFiltered;

        /// <summary>
        /// EntityId of the agent currently in possession, or -1 if ball is loose.
        /// FR-PA-022: phase classified locally from possession state.
        /// </summary>
        public int PossessionOwnerEntityId;

        /// <summary>True when PossessionOwnerEntityId ≥ 0 and that agent belongs to the team being processed.</summary>
        public bool PossessionOwnerIsOwnTeam;

        /// <summary>
        /// Per-agent data for all squad members, sorted by EntityId ascending at fill time.
        /// Length = SquadSize (set at construction). Slot index matches HysteresisState.Agents indexing.
        /// </summary>
        public readonly AgentPositioningData[] Agents;

        /// <summary>Number of valid active outfield agents (excludes GK). Optimisation hint; updated each fill.</summary>
        public int ActiveOutfieldCount;

        /// <param name="squadSize">Exactly 11 for Stage 0 (1 GK + 10 outfield).</param>
        public PositioningPerceptionSnapshot(int squadSize)
        {
            Agents = new AgentPositioningData[squadSize];
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
