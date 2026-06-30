// File:     src/match-engine/PlayerTacticConfigApplier.cs
// Created:  2026-06-30
// Modified: 2026-06-30
// Author:   —
// Spec:     Tactical Instructions #21 §3.3 (per-agent tactic config surface), FR-TI-027/031; Match Engine design note §5; Code Standards #20
// Purpose:  Boot-time applier that stages a PlayerTacticConfig into a MatchEngine via SetPlayerTactic, once
//           per agent, before kickoff. This is the per-agent seam the Stage 0+1 on-disk tactic loader feeds.

using System;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Applies a <see cref="PlayerTacticConfig"/> to a <see cref="MatchEngine"/> by calling
    /// <see cref="MatchEngine.SetPlayerTactic"/> once per roster slot (#21 §3.3 T2 runtime activation).
    /// The per-agent analogue of <see cref="TeamTacticConfigApplier"/>.
    /// </summary>
    /// <remarks>
    /// Call before the first <see cref="MatchEngine.RunTick"/>: each tactic is staged as <em>pending</em>
    /// and committed at the first AI-stride boundary (FR-TI-027). Applying
    /// <see cref="PlayerTacticConfig.Identity"/> is behaviour-neutral (every agent identity ⇒ byte-identical
    /// to an unconfigured match, FR-TI-031).
    /// </remarks>
    public static class PlayerTacticConfigApplier
    {
        /// <summary>
        /// Stages every agent's tactic from <paramref name="config"/> into <paramref name="engine"/>.
        /// </summary>
        /// <param name="engine">The match engine to configure (before its first tick).</param>
        /// <param name="config">The per-agent tactics to apply.</param>
        public static void Apply(MatchEngine engine, PlayerTacticConfig config)
        {
            if (engine == null)
            {
                throw new ArgumentNullException(nameof(engine));
            }
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            for (int agentId = 0; agentId < MatchEngineConstants.SQUAD_SIZE; agentId++)
            {
                engine.SetPlayerTactic(agentId, config.ForAgent(agentId));
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-30 | —      | Initial implementation — boot applier for PlayerTacticConfig (#21 §3.3). |
#endregion
