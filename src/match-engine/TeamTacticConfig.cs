// File:     src/match-engine/TeamTacticConfig.cs
// Created:  2026-06-29
// Modified: 2026-06-29
// Author:   —
// Spec:     Tactical Instructions #21 §3.1/§3.2 (T2 runtime activation), FR-TI-031; Match Engine design note §5; Code Standards #20
// Purpose:  In-memory manager-tactic config — one TeamTactic per team — that the boot applier feeds into
//           MatchEngine.SetTeamTactic before kickoff. The Stage 0+1 on-disk loader is a parser swap that
//           produces this same type (the #19 ScenarioIndex D1 precedent); no disk format is invented here.

using System;

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Immutable per-match manager-tactic configuration: the <see cref="TeamTactic"/> chosen for each
    /// team (index = teamId, 0 = home / 1 = away). This is the in-code source the boot
    /// <see cref="TeamTacticConfigApplier"/> reads to populate <see cref="MatchEngine.SetTeamTactic"/>
    /// before the first tick — the in-code half of the #21 §3.1/§3.2 T2 runtime-activation gate.
    /// </summary>
    /// <remarks>
    /// SOURCES — Stage 0 authors this config either in code (the <see cref="Default"/> factory or the
    /// full <see cref="TeamTactic"/> constructor) or from the on-disk Stage-0 text format via
    /// <see cref="TeamTacticFileLoader.Parse"/> (the parser swap this type was authored to receive, per
    /// the #19 <c>ScenarioIndex</c> precedent — it produces a <see cref="TeamTacticConfig"/> and leaves
    /// <see cref="TeamTacticConfigApplier"/> unchanged). The Stage-1 <c>[GT]</c> config-loader
    /// (FR-CS-019) may replace that text grammar with the pinned <c>[GT]</c> encoding; this type and the
    /// applier are unaffected.
    ///
    /// <see cref="Default"/> sets every team to <see cref="TeamTactic.Balanced"/>, which reproduces the
    /// pre-#21 baseline exactly (FR-TI-031) — applying the default config is behaviour-neutral.
    /// Construct with real factories (<see cref="TeamTactic.Balanced"/> or the full constructor):
    /// <c>default(TeamTactic)</c> is NOT the identity (see <see cref="TeamTactic"/>).
    /// </remarks>
    public sealed class TeamTacticConfig
    {
        private readonly TeamTactic[] _byTeam;

        /// <summary>
        /// Constructs a config from the home (teamId 0) and away (teamId 1) manager tactics.
        /// </summary>
        /// <param name="home">The home team's tactic (teamId 0).</param>
        /// <param name="away">The away team's tactic (teamId 1).</param>
        public TeamTacticConfig(in TeamTactic home, in TeamTactic away)
        {
            _byTeam = new TeamTactic[MatchEngineConstants.TEAM_COUNT];
            _byTeam[0] = home;
            _byTeam[1] = away;
        }

        /// <summary>
        /// The behaviour-neutral default: <see cref="TeamTactic.Balanced"/> for every team (FR-TI-031).
        /// Applying this config is byte-identical to never calling <see cref="MatchEngine.SetTeamTactic"/>.
        /// </summary>
        public static TeamTacticConfig Default =>
            new TeamTacticConfig(TeamTactic.Balanced, TeamTactic.Balanced);

        /// <summary>Returns the configured tactic for <paramref name="teamId"/> (0 = home, 1 = away).</summary>
        /// <param name="teamId">Team index in <c>[0, TEAM_COUNT)</c>.</param>
        public TeamTactic ForTeam(int teamId)
        {
            if (teamId < 0 || teamId >= MatchEngineConstants.TEAM_COUNT)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(teamId), teamId, "teamId must be 0 (home) or 1 (away).");
            }
            return _byTeam[teamId];
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-06-29 | —      | Initial implementation — in-code TeamTactic config source (#21 T2). |
// | 1.1     | 2026-06-29 | —      | Remark updated: on-disk source now exists (TeamTacticFileLoader.Parse). |
#endregion
