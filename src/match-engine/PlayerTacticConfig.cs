// File:     src/match-engine/PlayerTacticConfig.cs
// Created:  2026-06-30
// Modified: 2026-06-30
// Author:   —
// Spec:     Tactical Instructions #21 §3.3 (per-agent tactic config surface), FR-TI-031; Match Engine design note §5; Code Standards #20
// Purpose:  In-memory per-agent manager-tactic config — one PlayerTactic per roster slot — that the boot
//           applier feeds into MatchEngine.SetPlayerTactic before kickoff. The Stage 0+1 on-disk loader is a
//           parser swap that produces this same type (the #19 ScenarioIndex / TeamTacticConfig precedent).

using System;

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Immutable per-match per-agent tactic configuration: the <see cref="PlayerTactic"/> chosen for each
    /// roster slot (index = agentId, 0..SQUAD_SIZE−1). This is the in-code source the boot
    /// <see cref="PlayerTacticConfigApplier"/> reads to populate <see cref="MatchEngine.SetPlayerTactic"/>
    /// before the first tick — the per-agent half of the #21 §3.3 T2 runtime-activation gate (the
    /// per-team analogue is <see cref="TeamTacticConfig"/>).
    /// </summary>
    /// <remarks>
    /// SOURCES — Stage 0 authors this config in code (the <see cref="Identity"/> factory or the explicit
    /// per-agent constructor). The Stage 0+1 on-disk loader is a parser swap that produces a
    /// <see cref="PlayerTacticConfig"/> and leaves <see cref="PlayerTacticConfigApplier"/> unchanged (the
    /// #19 <c>ScenarioIndex</c> / <see cref="TeamTacticFileLoader"/> precedent); the Stage-1 <c>[GT]</c>
    /// config-loader (FR-CS-019) may then replace the grammar — this type and the applier are unaffected.
    ///
    /// <see cref="Identity"/> sets every agent to <see cref="PlayerTactic.Default(PlayerRole)"/> with
    /// <see cref="PlayerRole.Default"/>, which reproduces the pre-#21 baseline exactly (every §3.3 product
    /// factor ×1.0; FR-TI-031) — applying the identity config is behaviour-neutral. Construct with real
    /// factories: <c>default(PlayerTactic)</c> is NOT the identity (its embedded instructions man-mark
    /// agent 0; see <see cref="PlayerTactic"/>).
    /// </remarks>
    public sealed class PlayerTacticConfig
    {
        private readonly PlayerTactic[] _byAgent;

        /// <summary>
        /// Constructs a config from a per-agent array of tactics (length must equal
        /// <see cref="MatchEngineConstants.SQUAD_SIZE"/>; index = agentId). The array is copied so the
        /// caller cannot mutate the config after construction.
        /// </summary>
        /// <param name="byAgent">Per-agent tactics, indexed by roster slot.</param>
        public PlayerTacticConfig(PlayerTactic[] byAgent)
        {
            if (byAgent == null)
            {
                throw new ArgumentNullException(nameof(byAgent));
            }
            if (byAgent.Length != MatchEngineConstants.SQUAD_SIZE)
            {
                throw new ArgumentException(
                    $"byAgent must have exactly {MatchEngineConstants.SQUAD_SIZE} entries (one per roster slot); " +
                    $"got {byAgent.Length}.",
                    nameof(byAgent));
            }
            _byAgent = new PlayerTactic[MatchEngineConstants.SQUAD_SIZE];
            Array.Copy(byAgent, _byAgent, MatchEngineConstants.SQUAD_SIZE);
        }

        /// <summary>
        /// The behaviour-neutral identity: <see cref="PlayerTactic.Default(PlayerRole)"/> with
        /// <see cref="PlayerRole.Default"/> for every agent (FR-TI-031). Applying this config is
        /// byte-identical to never calling <see cref="MatchEngine.SetPlayerTactic"/>.
        /// </summary>
        public static PlayerTacticConfig Identity
        {
            get
            {
                PlayerTactic[] all = new PlayerTactic[MatchEngineConstants.SQUAD_SIZE];
                PlayerTactic identity = PlayerTactic.Default(PlayerRole.Default);
                for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
                {
                    all[i] = identity;
                }
                return new PlayerTacticConfig(all);
            }
        }

        /// <summary>Returns the configured tactic for <paramref name="agentId"/> (roster index 0..SQUAD_SIZE−1).</summary>
        /// <param name="agentId">Roster index in <c>[0, SQUAD_SIZE)</c>.</param>
        public PlayerTactic ForAgent(int agentId)
        {
            if (agentId < 0 || agentId >= MatchEngineConstants.SQUAD_SIZE)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(agentId), agentId, "agentId must be a roster index in [0, SQUAD_SIZE).");
            }
            return _byAgent[agentId];
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-30 | —      | Initial implementation — in-code per-agent PlayerTactic config (#21 §3.3). |
#endregion
