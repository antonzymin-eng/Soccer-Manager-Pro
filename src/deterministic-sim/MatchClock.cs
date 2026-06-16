// File:     src/deterministic-sim/MatchClock.cs
// Created:  2026-05-29
// Modified: 2026-06-16
// Author:   —
// Spec:     Deterministic Simulation #16 §3.4, CLAUDE.md §Determinism Rules (FR-CS-042), Code Standards #20
// Purpose:  Deterministic match clock. Provides tick index and match time (ms) derived exclusively from
//           the physics tick counter. No System.DateTime, no wall-clock, no async state.

namespace TacticalDirector.DeterministicSim
{
    /// <summary>
    /// Deterministic match clock injected into systems requiring simulation time.
    /// All time values are derived from the 60 Hz physics tick counter — never from wall-clock.
    /// CLAUDE.md FR-CS-042; Deterministic Simulation #16 §3.4.
    /// </summary>
    public sealed class MatchClock
    {
        private ulong _currentTick;

        /// <summary>Current 60 Hz physics tick index (0-based from match start).</summary>
        public ulong CurrentTick => _currentTick;

        /// <summary>Current 10 Hz tactical tick index (CurrentTick / AI_PHASE_STRIDE).</summary>
        public ulong CurrentTacticalTick => _currentTick / (ulong)DeterministicSimConstants.AI_PHASE_STRIDE;

        /// <summary>
        /// Current match time in milliseconds from kickoff.
        /// Formula: CurrentTick × FrameMs. Always ≥ 0.
        /// </summary>
        public float CurrentMatchTimeMs =>
            (float)_currentTick * DeterministicSimConstants.FrameMs;

        /// <summary>
        /// Current match time in seconds from kickoff.
        /// Formula: CurrentTick × FrameSeconds. Always ≥ 0.
        /// This is the seconds-domain clock systems must consume — e.g. the per-agent
        /// <c>currentTime</c> AgentMovement passes to <c>OscillationGuard.RecordAndCheck</c>,
        /// which compares elapsed time against <c>WindowSeconds</c>. Passing CurrentMatchTimeMs
        /// there is a silent 1000× unit error (the finite/≥0 guards accept ms just as readily).
        /// </summary>
        public float CurrentMatchTimeSeconds =>
            (float)_currentTick * DeterministicSimConstants.FrameSeconds;

        /// <summary>
        /// Constructs a MatchClock with the given initial tick.
        /// Typically 0 at match start; restored from snapshot on replay.
        /// </summary>
        public MatchClock(ulong initialTick = 0UL)
        {
            _currentTick = initialTick;
        }

        /// <summary>
        /// Advances the clock by one physics tick.
        /// Called exactly once per TickOrchestrator.RunTick invocation before phase execution begins.
        /// </summary>
        public void Advance()
        {
            _currentTick++;
        }

        /// <summary>
        /// Restores the clock to the given tick (used during replay rehydration step 5).
        /// §4.2.2 step 5.
        /// </summary>
        public void RestoreFromSnapshot(ulong tick)
        {
            _currentTick = tick;
        }

        /// <summary>
        /// Returns true if the current tick is an AI-stride tick (AI phase runs this tick).
        /// §3.1.2: AI phase runs when tick % AI_PHASE_STRIDE == 0.
        /// </summary>
        public bool IsAiStrideTick =>
            (_currentTick % (ulong)DeterministicSimConstants.AI_PHASE_STRIDE) == 0UL;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-05-29 | —      | Initial implementation.                                        |
// | 1.1     | 2026-06-16 | —      | Match Engine Phase B step B1 (time-unit plumbing): added       |
// |         |            |        | CurrentMatchTimeSeconds (CurrentTick × FrameSeconds) so seconds |
// |         |            |        | consumers (AgentMovement OscillationGuard WindowSeconds) no     |
// |         |            |        | longer risk the silent 1000× ms↔s unit error.                  |
#endregion
