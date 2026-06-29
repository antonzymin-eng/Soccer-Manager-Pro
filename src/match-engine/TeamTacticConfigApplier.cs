// File:     src/match-engine/TeamTacticConfigApplier.cs
// Created:  2026-06-29
// Modified: 2026-06-29
// Author:   —
// Spec:     Tactical Instructions #21 §3.1/§3.2 (T2 runtime activation), FR-TI-027/031; Match Engine design note §5; Code Standards #20
// Purpose:  Boot-time applier that stages a TeamTacticConfig into a MatchEngine via SetTeamTactic, once
//           per team, before kickoff. This is the seam the Stage 0+1 on-disk tactic loader feeds.

using System;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Applies a <see cref="TeamTacticConfig"/> to a <see cref="MatchEngine"/> by calling
    /// <see cref="MatchEngine.SetTeamTactic"/> once per team (#21 §3.1/§3.2 T2 runtime activation).
    /// </summary>
    /// <remarks>
    /// Call before the first <see cref="MatchEngine.RunTick"/>: each tactic is staged as <em>pending</em>
    /// and committed at the first AI-stride boundary (FR-TI-027), so a pre-kickoff apply is
    /// restore-deterministic. Applying <see cref="TeamTacticConfig.Default"/> is behaviour-neutral
    /// (every team Balanced ⇒ byte-identical to an unconfigured match, FR-TI-031).
    ///
    /// This is the boot-time entry point the Stage 0+1 on-disk tactic loader will feed: that loader parses
    /// a file into a <see cref="TeamTacticConfig"/> and calls <see cref="Apply"/> unchanged.
    /// </remarks>
    public static class TeamTacticConfigApplier
    {
        /// <summary>
        /// Stages every team's tactic from <paramref name="config"/> into <paramref name="engine"/>.
        /// </summary>
        /// <param name="engine">The match engine to configure (before its first tick).</param>
        /// <param name="config">The per-team manager tactics to apply.</param>
        public static void Apply(MatchEngine engine, TeamTacticConfig config)
        {
            if (engine == null)
            {
                throw new ArgumentNullException(nameof(engine));
            }
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            for (int teamId = 0; teamId < MatchEngineConstants.TEAM_COUNT; teamId++)
            {
                engine.SetTeamTactic(teamId, config.ForTeam(teamId));
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-06-29 | —      | Initial implementation — boot applier for TeamTacticConfig (#21 T2). |
#endregion
