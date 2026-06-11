// File:     src/decision-tree/DecisionContextAssembler.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Decision Tree #8 §2.2.4, §3.1.1, Code Standards #20
// Purpose:  Step 2 of the 6-step pipeline. Assembles DecisionContext from the validated
//           FilteredView, MatchContext, TacticalContext, DtAgentAttributes, and AgentState.
//           Pure function: no side effects, deterministic, zero heap allocation.

using UnityEngine;
using TacticalDirector.AgentMovement;
using TacticalDirector.PerceptionSystem;

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// Step 2: assembles DecisionContext from all pipeline inputs.
    /// Pure function with no side effects. Decision Tree #8 §2.2.4, §3.1.1.
    /// </summary>
    internal static class DecisionContextAssembler
    {
        /// <summary>
        /// Assembles a complete DecisionContext for one agent at one heartbeat.
        /// PressureScalar is taken from PerceptionDiagnostics if available; callers
        /// pass 0.0f if diagnostics are not routed to the DT at Stage 0.
        /// §2.2.4, §3.1.1.
        /// </summary>
        internal static DecisionContext Assemble(
            FilteredView snapshot,
            MatchContext matchContext,
            TacticalContext tacticalContext,
            DtAgentAttributes attributes,
            AgentState agentState,
            float pressureScalar,
            ulong matchSeed)
        {
            int agentId   = snapshot.ObserverId;
            int teamId    = attributes.TeamId;

            // ── Possession classification (§3.1.1.2) ─────────────────────────────
            bool agentHasBall = matchContext.PossessingAgentId == agentId;

            PossessionState possessedByTeam;
            if (matchContext.PossessingAgentId == -1)
            {
                possessedByTeam = PossessionState.CONTESTED;
            }
            else
            {
                // Determine which team possesses from possessing agent ID.
                // Agent IDs 0–10 = home team, 11–21 = away team (Stage 0 convention).
                // Stage 1+: replace with AgentState.TeamId lookup.
                bool possessorIsHome = matchContext.PossessingAgentId < 11;
                possessedByTeam = possessorIsHome
                    ? PossessionState.HOME_TEAM
                    : PossessionState.AWAY_TEAM;
            }

            // §3.1.1.2 / §3.4.6: perspective form of possession. True only when the
            // OPPOSING team possesses (CONTESTED is neither). AR-2 M-1: the §3.4.6
            // press-urgency gate previously compared PossessedByTeam == AWAY_TEAM,
            // which is "opponent" only for home agents.
            bool opponentHasBall =
                (possessedByTeam == PossessionState.HOME_TEAM && teamId == 1) ||
                (possessedByTeam == PossessionState.AWAY_TEAM && teamId == 0);

            // ── Team-relative ball zone (§3.2.1.3) ───────────────────────────────
            // Zones are defined "from own goal line", so the away team's zone must be
            // recomputed from BallPosition.x — NOT taken from the shared
            // home-perspective MatchContext.BallZone (AR-2 H-2: away zone modifiers
            // were inverted; mirroring the enum is also wrong because the home cut
            // points {35, 65} mirror to {40, 70}).
            FieldZone ballZone = PitchGeometry.ComputeFieldZone(
                matchContext.BallPosition.x, teamId);

            // ── Stamina gate (§3.1.8.1) ───────────────────────────────────────────
            bool staminaAvailable = agentState.AerobicPool > UtilityWeights.PRESS_STAMINA_MINIMUM;

            // ── Attribute normalisation (§3.2.1.2): A = (raw − 1) / 19 ──────────
            float norm = DecisionTreeConstants.AttributeNormRange;
            float minA = DecisionTreeConstants.AttributeNormMin;

            // ── Geometry ──────────────────────────────────────────────────────────
            Vector2 opponentGoalCentre = PitchGeometry.GetOpponentGoalCentre(teamId);
            Vector2 opponentGoalPostL  = PitchGeometry.GetOpponentGoalPostL(teamId);
            Vector2 opponentGoalPostR  = PitchGeometry.GetOpponentGoalPostR(teamId);

            return new DecisionContext
            {
                Snapshot          = snapshot,
                AgentId           = agentId,
                CurrentFrame      = snapshot.FrameNumber,
                AgentTeamId       = teamId,
                AgentHasBall      = agentHasBall,
                PossessedByTeam   = possessedByTeam,
                OpponentHasBall   = opponentHasBall,
                BallZone          = ballZone,
                StaminaAvailable  = staminaAvailable,

                AgentState        = agentState,
                AgentPosition     = agentState.Position,
                AgentFacingDirection = agentState.FacingDirection.sqrMagnitude > 0.0001f
                    ? agentState.FacingDirection
                    : Vector2.right,

                A_Vision      = (attributes.Vision      - minA) / norm,
                A_Passing     = (attributes.Passing     - minA) / norm,
                A_Finishing   = (attributes.Finishing   - minA) / norm,
                A_Dribbling   = (attributes.Dribbling   - minA) / norm,
                A_LongShots   = (attributes.LongShots   - minA) / norm,
                A_Composure   = (attributes.Composure   - minA) / norm,
                A_Decisions   = (attributes.Decisions   - minA) / norm,
                A_Anticipation = (attributes.Anticipation - minA) / norm,
                A_Pace        = (attributes.Pace        - minA) / norm,
                A_Agility     = (attributes.Agility     - minA) / norm,
                A_WorkRate    = (attributes.WorkRate     - minA) / norm,
                A_Stamina     = (attributes.Stamina     - minA) / norm,
                A_Aggression  = (attributes.Aggression  - minA) / norm,
                A_Positioning = (attributes.Positioning - minA) / norm,
                A_Crossing    = (attributes.Crossing    - minA) / norm,

                MatchContext      = matchContext,
                TacticalContext   = tacticalContext,
                PressureScalar    = pressureScalar,
                MatchSeed         = matchSeed,

                OpponentGoalCentre = opponentGoalCentre,
                OpponentGoalPostL  = opponentGoalPostL,
                OpponentGoalPostR  = opponentGoalPostR
            };
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                           |
// | 1.0     | 2026-05-29 | —      | Initial implementation.                                                         |
// | 1.1     | 2026-05-29 | —      | AR-1 H-3: Fix possession bug — was using agent/possessor same-team check;       |
// |         |            |        |   now correctly sets HOME_TEAM/AWAY_TEAM based on possessor's team ID.          |
// | 1.2     | 2026-06-11 | —      | Audit AR-2: H-2 team-relative BallZone computed from BallPosition.x per         |
// |         |            |        |   §3.2.1.3 ("from own goal line"); M-1 OpponentHasBall derived flag for the     |
// |         |            |        |   §3.4.6 press-urgency gate.                                                    |
#endregion
