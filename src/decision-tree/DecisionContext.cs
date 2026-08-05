// File:     src/decision-tree/DecisionContext.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Modified: 2026-08-04 (ERR-008-020 — + AllAgentAttributes squad attribute view)
// Author:   —
// Spec:     Decision Tree #8 §2.2.4, §3.1.1, Code Standards #20
// Purpose:  Internal struct aggregating all inputs for one agent's pipeline execution.
//           Assembled by DecisionContextAssembler from FilteredView + MatchContext +
//           TacticalContext + DtAgentAttributes + AgentState. Internal to the assembly.

using UnityEngine;
using TacticalDirector.AgentMovement;
using TacticalDirector.PerceptionSystem;

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// All inputs needed to run the full 6-step pipeline for one agent.
    /// Assembled once per heartbeat per agent by DecisionContextAssembler (Step 2).
    /// Internal: not exposed outside the decision-tree assembly.
    /// Decision Tree #8 §2.2.4.
    /// </summary>
    internal struct DecisionContext
    {
        // ── Snapshot ──────────────────────────────────────────────────────────

        /// <summary>Perception output for this agent at this heartbeat. §3.6.</summary>
        public FilteredView Snapshot;

        // ── Identity ──────────────────────────────────────────────────────────

        /// <summary>AgentId of the agent being evaluated [0–21].</summary>
        public int AgentId;

        /// <summary>10Hz heartbeat tick at which this context was assembled.</summary>
        public int CurrentFrame;

        /// <summary>Team identifier: 0 = home, 1 = away.</summary>
        public int AgentTeamId;

        // ── Possession Flags ──────────────────────────────────────────────────

        /// <summary>True when MatchContext.PossessingAgentId == AgentId. §3.1.1.2.</summary>
        public bool AgentHasBall;

        /// <summary>
        /// Absolute possession state: HOME_TEAM | AWAY_TEAM | CONTESTED (§2.2.5 enum).
        /// NOTE: §3.1.1.2 describes possession in perspective terms (OWN_TEAM /
        /// OPPONENT); the published §2.2.5 enum is absolute. Consumers needing the
        /// perspective form use <see cref="OpponentHasBall"/> — comparing this field
        /// against a literal team value is the AR-2 M-1 defect class (the §3.4.6
        /// press-urgency gate was keyed to AWAY_TEAM, inverting it for away agents).
        /// </summary>
        public PossessionState PossessedByTeam;

        /// <summary>
        /// True when the OPPOSING team (relative to AgentTeamId) possesses the ball.
        /// False for own-team possession and for CONTESTED. Derived by
        /// DecisionContextAssembler from PossessedByTeam + AgentTeamId; this is the
        /// §3.1.1.2 "OPPONENT" perspective value and the §3.4.6 press-urgency input.
        /// </summary>
        public bool OpponentHasBall;

        // ── Stamina Gate ──────────────────────────────────────────────────────

        /// <summary>
        /// True when AerobicStaminaPool > PRESS_STAMINA_MINIMUM.
        /// Stage 0 binary gate for PRESS eligibility (§3.1.8.1).
        /// </summary>
        public bool StaminaAvailable;

        // ── Agent Physical State ──────────────────────────────────────────────

        /// <summary>Full kinematic and energy state. §3.5.6 coordinate convention applies.</summary>
        public AgentState AgentState;

        /// <summary>XY position extracted from AgentState.Position for convenience.</summary>
        public Vector2 AgentPosition;

        /// <summary>Unit facing direction extracted from AgentState.FacingDirection.</summary>
        public Vector2 AgentFacingDirection;

        // ── Attributes (normalised [0,1]) ─────────────────────────────────────
        // A_X = (X_raw − 1) / 19. Pre-normalised by DecisionContextAssembler.

        public float A_Vision;
        public float A_Passing;
        public float A_Finishing;
        public float A_Dribbling;
        public float A_LongShots;
        public float A_Composure;
        public float A_Decisions;
        public float A_Anticipation;
        public float A_Pace;
        public float A_Agility;
        public float A_WorkRate;
        public float A_Stamina;
        public float A_Aggression;
        public float A_Positioning;
        public float A_Crossing;

        // ── Squad attribute view (ERR-008-020) ────────────────────────────────

        /// <summary>
        /// Read-only view of ALL agents' DT attributes, indexed by AgentId [0–21] —
        /// the orchestrator's own live array (substitutions are visible through it).
        /// Consumed by the §3.1.3.3 pass-lane threat model to read an opponent's
        /// Anticipation/Pace. May be null (unwired host / legacy test context): every
        /// opponent then reads as ability-neutral 1.0, which is exactly the
        /// pre-ERR-008-020 attribute-blind weighting.
        /// </summary>
        public DtAgentAttributes[] AllAgentAttributes;

        // ── Match Context ─────────────────────────────────────────────────────

        public MatchContext MatchContext;

        /// <summary>
        /// Team-relative ball zone (§3.2.1.3: "from own goal line"). Computed by
        /// DecisionContextAssembler from MatchContext.BallPosition.x via
        /// PitchGeometry.ComputeFieldZone(posX, teamId). UtilityScorer MUST read this
        /// field, NOT MatchContext.BallZone (which is the orchestrator's
        /// home-perspective value and is inverted for away agents — AR-2 H-2).
        /// </summary>
        public FieldZone BallZone;

        // ── Tactical Context ──────────────────────────────────────────────────

        public TacticalContext TacticalContext;

        // ── Pressure ─────────────────────────────────────────────────────────

        /// <summary>Pressure scalar [0,1] from PerceptionDiagnostics (Perception #7 §3.6).</summary>
        public float PressureScalar;

        // ── Determinism Seed ──────────────────────────────────────────────────

        /// <summary>Per-match seed for SplitMix64 composure noise (§3.3.3). Set once per match.</summary>
        public ulong MatchSeed;

        // ── Geometry ─────────────────────────────────────────────────────────

        /// <summary>Opponent goal centre for this agent's team. Cached from PitchGeometry (§3.2.1.4).</summary>
        public Vector2 OpponentGoalCentre;

        /// <summary>Left goal post of opponent goal (lower Y).</summary>
        public Vector2 OpponentGoalPostL;

        /// <summary>Right goal post of opponent goal (higher Y).</summary>
        public Vector2 OpponentGoalPostR;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
// | 1.1     | 2026-06-11 | —      | Audit AR-2: H-2 team-relative BallZone field (scorer input; replaces direct |
// |         |            |        |   MatchContext.BallZone reads); M-1 OpponentHasBall derived flag (§3.4.6     |
// |         |            |        |   press urgency); PossessedByTeam doc corrected to absolute §2.2.5 enum      |
// |         |            |        |   semantics (was claiming OWN_TEAM/OPPONENT perspective values).             |
// | 1.2     | 2026-08-04 | —      | ERR-008-020: + AllAgentAttributes (nullable all-agents attribute view for   |
// |         |            |        |   the §3.1.3.3 pass-lane threat model; null ⇒ ability-neutral).             |
#endregion
