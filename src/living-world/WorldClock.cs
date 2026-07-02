// File:     src/living-world/WorldClock.cs
// Created:  2026-07-02
// Modified: 2026-07-02
// Author:   —
// Spec:     Living World System #22 §4.2 (KD-4), FR-LW-019, Code Standards #20
// Purpose:  Deterministic season-calendar clock. One worldTick = one calendar day — the unit vol-2
//           §2.2 latencies use. Distinct from the 60 Hz MatchClock; never advanced by the match loops.

namespace TacticalDirector.LivingWorld
{
    /// <summary>
    /// The deterministic season-calendar clock that owns the world loop (#22 §4.2, KD-4).
    /// One <c>worldTick</c> = one calendar day (FR-LW-019). This clock is advanced ONLY by
    /// <see cref="WorldLoop.RunWorldTick"/> — never inside the 10 Hz tactical or 60 Hz physics loops
    /// (loop-conflation hazard, CLAUDE.md; T-LW-DET-006). No wall-clock, no <c>System.DateTime</c>
    /// (FR-CS-042 parallel); the tick counter is plain serialisable state restored on load.
    /// </summary>
    public sealed class WorldClock
    {
        private uint _currentWorldTick;

        /// <summary>Current calendar day, 0-based from career/world start.</summary>
        public uint CurrentWorldTick => _currentWorldTick;

        /// <summary>
        /// Constructs a WorldClock at the given calendar day (0 at world start; restored from the
        /// world-store snapshot on load).
        /// </summary>
        public WorldClock(uint initialWorldTick = 0u)
        {
            _currentWorldTick = initialWorldTick;
        }

        /// <summary>Advances the calendar by one day. Called exactly once per world tick.</summary>
        public void Advance()
        {
            _currentWorldTick++;
        }

        /// <summary>Restores the calendar day from a world-store snapshot (FR-LW-022).</summary>
        public void RestoreFromSnapshot(uint worldTick)
        {
            _currentWorldTick = worldTick;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-02 | —      | Initial implementation (season/world loop slice 1).           |
#endregion
