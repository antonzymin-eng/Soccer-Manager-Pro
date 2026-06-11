// File:     src/decision-tree/DtAgentAttributes.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Decision Tree #8 §3.1 (attribute dependency flags), §3.2 (utility formulas),
//           Code Standards #20
// Purpose:  Decision Tree attribute snapshot. Separates DT-specific attributes from
//           locomotion attributes in AgentMovement.PlayerAttributes (§3.5.6 of AM #2).
//           Populated by the simulation orchestrator each heartbeat from the player
//           data store. Mirrors the pattern established by PerceptionAgentAttributes
//           in Perception System #7 §4.2.2.
//
//           All attributes are int [1–20] raw values matching the spec attribute table.
//           Normalised A_ values are computed on demand by DecisionContextAssembler.

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// Tactical and technical player attributes consumed by the Decision Tree.
    /// All values in [1, 20] matching the master attribute table.
    /// Orchestrator populates this per-heartbeat from the player data store.
    /// Decision Tree #8 §3.1 attribute dependency flags; §3.2 utility formulas.
    /// </summary>
    public struct DtAgentAttributes
    {
        // ── Tactical ──────────────────────────────────────────────────────────

        /// <summary>Cognitive breadth under pressure. Governs pass candidate cap (§3.1.3.6).</summary>
        public int Decisions;

        /// <summary>Passing range awareness. PASS utility Vision component (§3.2.2).</summary>
        public int Vision;

        // ── Technical: Possession ─────────────────────────────────────────────

        /// <summary>Passing execution accuracy. PASS utility technique component (§3.2.2).</summary>
        public int Passing;

        /// <summary>Shooting accuracy. SHOOT utility Finishing component (§3.2.3).</summary>
        public int Finishing;

        /// <summary>Ball manipulation skill. DRIBBLE utility core component (§3.2.4).</summary>
        public int Dribbling;

        /// <summary>Long-range shooting intent. Extends shooting range and midfield shoot modifier (§3.1.4.2).</summary>
        public int LongShots;

        /// <summary>
        /// Crossing quality. DECLARED-BUT-UNCONSUMED at Stage 0 (AR-2 L; CS AR-10 /
        /// PM AR-9 L-3 precedent): §3.1.3.4's CROSS gate also requires the WIDE_ZONE
        /// channel test, which has no declared constant — until that gate lands, pass
        /// type derivation classifies CROSS from range + facing angle alone and does
        /// not read this attribute (see OptionGenerator.DerivePassType note).
        /// Normalised into DecisionContext.A_Crossing for forward compatibility.
        /// </summary>
        public int Crossing;

        // ── Mental: Composure & Anticipation ─────────────────────────────────

        /// <summary>Maintained calm under pressure. HOLD and SHOOT composure component; noise bound (§3.3.4).</summary>
        public int Composure;

        /// <summary>Proactive read of play. INTERCEPT utility and PRESS gate (§3.2.8, §3.1.9).</summary>
        public int Anticipation;

        // ── Physical: Movement ────────────────────────────────────────────────

        /// <summary>Top running speed. INTERCEPT agent_max_speed (§3.1.9.2).</summary>
        public int Pace;

        /// <summary>Directional change speed. DRIBBLE utility agility component (§3.2.4).</summary>
        public int Agility;

        // ── Physical: Work Capacity ───────────────────────────────────────────

        /// <summary>Pressing intensity and workrate. PRESS utility (§3.2.7), MOVE utility (§3.2.6).</summary>
        public int WorkRate;

        /// <summary>Stamina attribute [1–20]. PRESS utility stamina component (§3.2.7). PRESS gate uses AgentState.AerobicPool (§3.1.8.1).</summary>
        public int Stamina;

        /// <summary>Pressing aggression. PRESS utility aggression component (§3.2.7).</summary>
        public int Aggression;

        // ── Positional ────────────────────────────────────────────────────────

        /// <summary>Positional discipline. MOVE utility (§3.2.6).</summary>
        public int Positioning;

        // ── Identity ─────────────────────────────────────────────────────────

        /// <summary>Team identifier: 0 = home, 1 = away.</summary>
        public int TeamId;

        /// <summary>Creates mid-range (10) defaults for testing purposes.</summary>
        public static DtAgentAttributes CreateDefault(int teamId = 0)
        {
            return new DtAgentAttributes
            {
                Decisions   = 10,
                Vision      = 10,
                Passing     = 10,
                Finishing   = 10,
                Dribbling   = 10,
                LongShots   = 10,
                Crossing    = 10,
                Composure   = 10,
                Anticipation = 10,
                Pace        = 10,
                Agility     = 10,
                WorkRate    = 10,
                Stamina     = 10,
                Aggression  = 10,
                Positioning = 10,
                TeamId      = teamId
            };
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                    |
// | 1.0     | 2026-05-29 | —      | Initial implementation.                                                  |
// | 1.1     | 2026-05-29 | —      | AR-1 M-3: Fix Stamina doc — it is int [1-20], not [0,1]; PRESS gate     |
// |         |            |        |   uses AgentState.AerobicPool, not this attribute.                       |
// | 1.2     | 2026-06-11 | —      | Audit AR-2 L: Crossing doc-noted declared-but-unconsumed (§3.1.3.4       |
// |         |            |        |   WIDE_ZONE cross gate not implementable at Stage 0; ERR-008-006).       |
#endregion
