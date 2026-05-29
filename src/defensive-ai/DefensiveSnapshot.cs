// File:     src/defensive-ai/DefensiveSnapshot.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Defensive AI #14 §2.3, §4.4, Code Standards #20
// Purpose:  Full tick input container for DefensiveAITick. Populated once per tick by
//           the match orchestrator from #7, #12, #13 state before Tick() is called.

using UnityEngine;

using TacticalDirector.PositioningAI;

namespace TacticalDirector.DefensiveAI
{
    /// <summary>
    /// Full tick input for one Defensive AI tick, populated by the match orchestrator.
    /// All fields are read-only within the tick; no mid-tick re-reads occur (FR-DA-029 F1).
    /// Defensive AI #14 §2.3 / §4.4.
    ///
    /// <see cref="Agents"/> contains all up-to-22 agents (both teams). Callers filter by
    /// <see cref="DefensiveTeamId"/> to obtain own-team or opponent agents.
    /// </summary>
    public sealed class DefensiveSnapshot
    {
        /// <summary>
        /// Tick index when this snapshot was captured. Used for F1 stale-perception detection
        /// (FR-DA-029): if snapshot.TickIndex &lt; lastProcessedTick, reuse previous tick output.
        /// </summary>
        public int TickIndex;

        /// <summary>TeamId of the team running the defensive algorithm (0 = home, 1 = away).</summary>
        public int DefensiveTeamId;

        /// <summary>Ball world-space position (X, Y). Z available but unused at Stage 0.</summary>
        public Vector2 BallPosition;

        /// <summary>Ball velocity vector (m/s). Magnitude used for offside-trap trigger (§3.7.2 condition 1).</summary>
        public Vector2 BallVelocity;

        /// <summary>EntityId of current ball possessor. -1 for loose ball.</summary>
        public int PossessionOwnerEntityId;

        /// <summary>
        /// Team phase for the defensive team, from Positioning AI #12 §3.0.
        /// Phase gate: if InPoss, #14 emits all-ZONAL and returns (FR-DA-013 / KD-19).
        /// </summary>
        public Phase TeamPhase;

        /// <summary>
        /// Defensive line depth (distToOwnGoal, m) from Positioning AI #12.
        /// Read-only mirror written to MarkDirective.OffensiveLineDepth (FR-DA-012).
        /// </summary>
        public float DefensiveLineDepth;

        /// <summary>
        /// EntityId of the defensive team's goalkeeper (-1 if none detected).
        /// Used by HoldShapePoolFilter to exclude GK (FR-DA-009 / KD-7).
        /// </summary>
        public int GkEntityId;

        /// <summary>
        /// Perceived world-space position of the defensive team's goalkeeper (X, Y).
        /// Used by LastManDetector COVER_GK_ZONE trigger (§3.9). Set to Vector2.zero if GK absent.
        /// </summary>
        public Vector2 GkPosition;

        /// <summary>
        /// All agents for both teams. Length is at most DefensiveAIConstants.SQUAD_SIZE (22).
        /// </summary>
        public DefensiveAgentSnapshot[] Agents;

        /// <summary>Actual number of valid entries in <see cref="Agents"/>.</summary>
        public int AgentCount;

        /// <summary>
        /// True when the defensive team has an active PRIMARY_PRESS role this tick.
        /// Used by offside-trap trigger condition 4 (§3.7.2).
        /// </summary>
        public bool HasActivePrimaryPress;

        /// <summary>
        /// Constructs a snapshot with a pre-allocated agents array of the given capacity.
        /// </summary>
        public DefensiveSnapshot(int agentCapacity = DefensiveAIConstants.SQUAD_SIZE)
        {
            Agents     = new DefensiveAgentSnapshot[agentCapacity];
            AgentCount = 0;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
