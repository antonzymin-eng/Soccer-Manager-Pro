// File: src/positioning-ai/HysteresisState.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec: #12 Positioning AI §3.0, §3.8, FR-PA-038
// Purpose: Full team-level hysteresis state including phase dwell counters and per-agent membership state.

namespace TacticalDirector.PositioningAI
{
    /// <summary>
    /// All persistent positioning state for one team across 10 Hz ticks.
    /// Allocated once per match by the orchestrator; mutated in-place by PositioningAITick.
    /// Included verbatim in the per-tick determinism digest (FR-PA-038, #16 §6.2).
    /// </summary>
    public sealed class HysteresisState
    {
        // ── Team phase ───────────────────────────────────────────────────────────

        /// <summary>Committed phase; drives compactness and pull-factor row selection.</summary>
        public Phase CurrentPhase;

        /// <summary>Candidate phase pending PHASE_HYSTERESIS_TICKS consecutive observations.</summary>
        public Phase CandidatePhase;

        /// <summary>Consecutive ticks the candidate phase has been observed. Commits at PHASE_HYSTERESIS_TICKS.</summary>
        public int PhaseDwellCount;

        // ── Per-agent membership ─────────────────────────────────────────────────

        /// <summary>
        /// Per-agent hysteresis state; indexed by SlotIndex (0 = lowest EntityId in squad).
        /// Length = squad size (11 for Stage 0).
        /// </summary>
        public readonly AgentHysteresisState[] Agents;

        /// <summary>Number of agents in the squad; matches Agents.Length.</summary>
        public int SquadSize => Agents.Length;

        /// <param name="squadSize">11 for Stage 0 (1 GK + 10 outfield).</param>
        public HysteresisState(int squadSize)
        {
            Agents = new AgentHysteresisState[squadSize];
        }

        /// <summary>
        /// Seeds all per-agent line/lane membership from the formation's default values.
        /// Called once at match init (Stage0Default path per §4.4.3).
        /// </summary>
        public void SeedFromFormation(FormationSlotRecord[] slots)
        {
            for (int i = 0; i < Agents.Length && i < slots.Length; i++)
            {
                Agents[i].CurrentLine    = slots[i].DefaultLine;
                Agents[i].CandidateLine  = slots[i].DefaultLine;
                Agents[i].LineDwellCount = 0;
                Agents[i].CurrentLane    = slots[i].DefaultLane;
                Agents[i].CandidateLane  = slots[i].DefaultLane;
                Agents[i].LaneDwellCount = 0;
            }

            CurrentPhase    = Phase.InPoss;
            CandidatePhase  = Phase.InPoss;
            PhaseDwellCount = 0;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
