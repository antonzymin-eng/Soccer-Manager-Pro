// File:     src/match-engine/TacticPresetProjection.cs
// Created:  2026-07-11
// Modified: 2026-07-11
// Author:   —
// Spec:     Tactical Presets #26 §3.1 (FM-TP-01, FR-TP-004/014), §4.2 boot call path; Code Standards #20
// Purpose:  Pure preset → (TeamTacticConfig, PlayerTacticConfig) projection for one managed team.
//           No engine call; the boot appliers consume the outputs (FR-TP-004).

using System;

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// FM-TP-01 (#26 §3.1): projects one <see cref="TacticPreset"/> into the config types the
    /// existing boot appliers consume. The team tactic fills the MANAGED team's entry only — the
    /// other team's entry stays the caller-supplied value (its own configured/default tactic).
    /// <c>Players</c>, when present, fills the managed team's roster entries (block
    /// <c>managedTeamId × PLAYERS_PER_TEAM … +PLAYERS_PER_TEAM−1</c>, the engine's roster
    /// layout); all other entries stay the <see cref="PlayerTactic"/> identity. Pure construction —
    /// no engine call (that is the applier's job, §4.2).
    ///
    /// This file is not named in the #26 §4.1 placement table (which lists the manager LOGIC
    /// files); it hosts the §4.2 "Project" step and must live in this assembly because the config
    /// types are match-engine-owned and <c>tactical-instructions</c> cannot reference upward.
    /// </summary>
    public static class TacticPresetProjection
    {
        /// <summary>
        /// Projects <paramref name="preset"/> for <paramref name="managedTeamId"/>.
        /// The FR-TP-014 roster gate runs here (this is the consuming applier seam
        /// <see cref="TacticPreset.ValidatePlayers"/> was anchored to): a present <c>Players</c>
        /// array must be exactly <see cref="MatchEngineConstants.PLAYERS_PER_TEAM"/> long (F1).
        /// </summary>
        /// <param name="preset">The preset to apply.</param>
        /// <param name="managedTeamId">The AI-managed team (0 home / 1 away).</param>
        /// <param name="otherTeamTactic">The OTHER team's own tactic (kept as-is per FM-TP-01).</param>
        /// <param name="teamConfig">The composed per-team config for the boot applier.</param>
        /// <param name="playerConfig">The composed per-agent config for the boot applier.</param>
        public static void Project(
            in TacticPreset preset,
            int managedTeamId,
            in TeamTactic otherTeamTactic,
            out TeamTacticConfig teamConfig,
            out PlayerTacticConfig playerConfig)
        {
            if (managedTeamId < 0 || managedTeamId >= MatchEngineConstants.TEAM_COUNT)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(managedTeamId), managedTeamId, "managedTeamId must be 0 (home) or 1 (away).");
            }
            preset.ValidatePlayers(MatchEngineConstants.PLAYERS_PER_TEAM);

            teamConfig = managedTeamId == 0
                ? new TeamTacticConfig(preset.Team, otherTeamTactic)
                : new TeamTacticConfig(otherTeamTactic, preset.Team);

            PlayerTactic[] byAgent = new PlayerTactic[MatchEngineConstants.SQUAD_SIZE];
            PlayerTactic identity = PlayerTactic.Default(PlayerRole.Default);
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                byAgent[i] = identity;
            }
            if (preset.Players != null)
            {
                int baseIndex = managedTeamId * MatchEngineConstants.PLAYERS_PER_TEAM;
                for (int k = 0; k < MatchEngineConstants.PLAYERS_PER_TEAM; k++)
                {
                    byAgent[baseIndex + k] = preset.Players[k];
                }
            }
            playerConfig = new PlayerTacticConfig(byAgent);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                        |
// | 1.0     | 2026-07-11 | —      | Initial implementation (#26 T1 — FM-TP-01).   |
#endregion
