// File:     src/decision-tree/DecisionContextAssembler.cs
// Created:  2026-05-29
// Modified: 2026-06-14 (audit AR-3 fix pass)
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
                // Agent IDs [0, HomeSquadAgentCount) = home, the rest = away (Stage 0
                // convention; single source of truth in DecisionTreeConstants — AR-3 L,
                // was a bare literal 11 decoupled from the convention). Stage 1+:
                // replace ID-range inference with AgentState.TeamId lookup.
                bool possessorIsHome =
                    matchContext.PossessingAgentId < DecisionTreeConstants.HomeSquadAgentCount;
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

            // ── Position / facing sanitise ───────────────────────────────────────
            // AR-3 L: non-finite AgentPosition would propagate NaN through every
            // generated TargetPosition (dribble look-ahead, intercept projection,
            // pass-lane geometry); SnapshotValidator does not range-check position, so
            // sanitise non-finite components to 0 here (project NaN-gate pattern). The
            // generators and dispatcher read this field, not AgentState.Position.
            Vector2 agentPosition = SanitiseXy(agentState.Position);

            // AR-3 L: a degenerate facing vector defaults toward the opponent goal
            // (team-relative) rather than the home-perspective Vector2.right — the
            // latter pointed downfield for the home team but BACKWARD for the away
            // team, the home-default class the AR-2 audit targeted. Falls back to the
            // team forward axis only if the agent is exactly on the goal centre.
            Vector2 agentFacing;
            if (agentState.FacingDirection.sqrMagnitude
                > DecisionTreeConstants.FacingDegenerateSqrThreshold)
            {
                agentFacing = agentState.FacingDirection;
            }
            else
            {
                Vector2 toGoal = opponentGoalCentre - agentPosition;
                agentFacing = toGoal.sqrMagnitude
                    > DecisionTreeConstants.FacingDegenerateSqrThreshold
                    ? toGoal.normalized
                    : (teamId == 0 ? Vector2.right : Vector2.left);
            }

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
                AgentPosition     = agentPosition,
                AgentFacingDirection = agentFacing,

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

        // AR-3 L: non-finite (NaN / ±Inf) XY components → 0, so corrupt upstream
        // position state cannot seed NaN into generated TargetPositions.
        private static Vector2 SanitiseXy(Vector2 p)
        {
            float x = (float.IsNaN(p.x) || float.IsInfinity(p.x)) ? 0.0f : p.x;
            float y = (float.IsNaN(p.y) || float.IsInfinity(p.y)) ? 0.0f : p.y;
            return new Vector2(x, y);
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
// | 1.3     | 2026-06-14 | —      | Audit AR-3 L: degenerate-facing default now team-relative (toward opponent      |
// |         |            |        |   goal, then team forward axis) instead of the home-perspective Vector2.right;  |
// |         |            |        |   AgentPosition sanitised against non-finite components (SanitiseXy) so corrupt |
// |         |            |        |   position cannot seed NaN into generated TargetPositions; possessor-team split |
// |         |            |        |   reads DecisionTreeConstants.HomeSquadAgentCount (was a bare literal 11).      |
#endregion
