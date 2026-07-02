// File:     src/living-world/WorldLoop.cs
// Created:  2026-07-02
// Modified: 2026-07-02
// Author:   —
// Spec:     Living World System #22 §4.2 (KD-4), FR-LW-019/027/032/034, Code Standards #20
// Purpose:  The season-calendar world loop orchestrator: advances the WorldClock one calendar day and
//           runs the §4.2 per-tick phases over the subsystems that exist. Event- and day-driven; runs
//           NEVER inside the 10 Hz tactical or 60 Hz physics loops.

using System;

namespace TacticalDirector.LivingWorld
{
    /// <summary>
    /// Orchestrates one world tick (= one calendar day) in the fixed §4.2 phase order. Constructor-
    /// injected dependencies (FR-CS-051–054); day-cadence, so not subject to the 60 Hz zero-alloc /
    /// ProfilerMarker hot-path rules (the assembly is engine-free, noEngineReferences).
    ///
    /// Phase status at this slice: phases 3 (salience decay) runs; phases 1 (match-outcome ingest),
    /// 2 (committed human-systems read), 4 (arc evaluation), 5 (background tier), and 6 (budget
    /// eviction) are documented seams — their producers (structured match-outcome events, vol-2/vol-3,
    /// ArcEngine, BackgroundTierSim, the platform-tuned SAVE_SIZE_BUDGET) do not exist yet, and per
    /// the phantom-interface rule (CLAUDE.md / FR-LW-031) no interface is authored against them.
    /// </summary>
    public sealed class WorldLoop
    {
        private readonly WorldClock _clock;
        private readonly MemoryStore _memory;

        /// <summary>Current calendar day (passthrough to the injected clock).</summary>
        public uint CurrentWorldTick => _clock.CurrentWorldTick;

        public WorldLoop(WorldClock clock, MemoryStore memory)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _memory = memory ?? throw new ArgumentNullException(nameof(memory));
        }

        /// <summary>
        /// Runs one world tick in the §4.2 order. The vol-2/vol-3 daily human-systems update (H-Gate,
        /// §2.2 propagation) is a PRIOR season-loop phase owned outside this layer — callers run it
        /// before this method; the world loop only reads its committed output (FR-LW-027 / KD-9).
        /// </summary>
        public void RunWorldTick()
        {
            _clock.Advance();

            // Phase 1 — ingest match-outcome events: NOT WIRED. Structured match-outcome events are a
            //           KD-10 prerequisite owned by the match engine (§7.1); the consumer lands with
            //           that producer (FR-LW-031 — no phantom interface).
            // Phase 2 — read committed canonical human-systems state: NOT WIRED (vol-2/vol-3 not
            //           implemented; read-and-route only when it lands, FR-LW-027).

            // Phase 3 — memory salience decay (§3.2 step 3). §3.1 layer relaxation toward the
            //           per-entity baseline b is deferred with phase 2: b is vol-2-owned state, and
            //           the rule is a no-op identity until an event/baseline exists (FR-LW-034).
            _memory.DecayEpisodeSalience(LivingWorldConstants.SALIENCE_DECAY_RATE);

            // Phase 4 — arc evaluation (§3.4): ArcEngine is a follow-up slice (draws from the
            //           world.arcs RNG sub-stream, FR-LW-020).
            // Phase 5 — background-tier update (§3.5): BackgroundTierSim is a follow-up slice.
            // Phase 6 — budget/eviction (§4.5): SAVE_SIZE_BUDGET is platform-tuned [GT] via the
            //           config loader (not declarable as a literal — see LivingWorldConstants);
            //           per-edge depth eviction is enforced inline at MemoryStore.RecordEpisode.
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-02 | —      | Initial implementation (season/world loop slice 1): clock     |
// |         |            |        | advance + phase-3 decay; phases 1/2/4/5/6 documented seams.   |
#endregion
