// File:     src/living-world/WorldLoop.cs
// Created:  2026-07-02
// Modified: 2026-07-24 (arc-triggers Slice 1/E1: 5-arg ctor carries the nullable ArcTriggerEvaluator;
//           RunWorldTick takes canon as a per-tick argument; phase 4 evaluates triggers before the
//           expiry sweep when both the evaluator and canon are non-null — KD-5)
// Author:   —
// Spec:     Living World System #22 §4.2 (KD-4), FR-LW-014/019/023/027/032/034, Code Standards #20
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
    /// Phase status at this slice: phase 3 (salience decay) runs; phase 4 runs the ArcEngine
    /// lifecycle duties that exist (§6.2 expiry sweep — trigger evaluation stays the ArcEngine's
    /// documented seam); phase 6 runs the §3.5 external-cap LRU enforcement. Phases 1 (match-outcome
    /// ingest), 2 (committed human-systems read), and 5 (background-tier update) remain documented
    /// seams — their producers (structured match-outcome events, vol-2/vol-3, BackgroundTierSim) do
    /// not exist yet, and per the phantom-interface rule (CLAUDE.md / FR-LW-031) no interface is
    /// authored against them. The arcs/membership constructor arguments accept null as the sanctioned
    /// not-yet-wired seam (decision-tree executor precedent) so slice-1 hosts keep booting unchanged.
    /// </summary>
    public sealed class WorldLoop
    {
        private readonly WorldClock _clock;
        private readonly MemoryStore _memory;
        private readonly ArcEngine _arcs;
        private readonly ActiveSetMembership _membership;
        private readonly ArcTriggerEvaluator _arcTriggers;

        /// <summary>Current calendar day (passthrough to the injected clock).</summary>
        public uint CurrentWorldTick => _clock.CurrentWorldTick;

        /// <summary>Boots without the phase-4/phase-6 subsystems (slice-1 shape; both seams skip).</summary>
        public WorldLoop(WorldClock clock, MemoryStore memory)
            : this(clock, memory, null, null, null)
        {
        }

        /// <summary>
        /// Full wiring without the arc-trigger evaluator. <paramref name="arcs"/> /
        /// <paramref name="membership"/> may be null — the sanctioned not-wired seam; the corresponding
        /// phase is skipped.
        /// </summary>
        public WorldLoop(WorldClock clock, MemoryStore memory, ArcEngine arcs, ActiveSetMembership membership)
            : this(clock, memory, arcs, membership, null)
        {
        }

        /// <summary>
        /// Full wiring incl. the arc-trigger evaluator (arc-triggers-design KD-5). Every subsystem
        /// argument (<paramref name="arcs"/> / <paramref name="membership"/> / <paramref name="arcTriggers"/>)
        /// may be null — the sanctioned not-wired seam; the corresponding phase (or, for the evaluator,
        /// trigger evaluation within phase 4) is skipped.
        /// </summary>
        public WorldLoop(WorldClock clock, MemoryStore memory, ArcEngine arcs,
            ActiveSetMembership membership, ArcTriggerEvaluator arcTriggers)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _memory = memory ?? throw new ArgumentNullException(nameof(memory));
            _arcs = arcs;
            _membership = membership;
            _arcTriggers = arcTriggers;
        }

        /// <summary>
        /// Runs one world tick in the §4.2 order. The vol-2/vol-3 daily human-systems update (H-Gate,
        /// §2.2 propagation) is a PRIOR season-loop phase owned outside this layer — callers run it
        /// before this method; the world loop only reads its committed output (FR-LW-027 / KD-9).
        /// </summary>
        /// <param name="canon">
        /// The arc-trigger canon source (arc-triggers-design KD-1) — <c>null</c> (the default) skips
        /// phase-4 trigger evaluation entirely (byte-identical to a run with no arcs). Passed as a
        /// per-tick argument, NOT held as a field, so the owning <see cref="WorldStore"/> is the single
        /// source of truth for the live canon and no stale-canon seam can form.
        /// </param>
        public void RunWorldTick(ArcCanonSource canon = null)
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

            // Phase 4 — arc evaluation (§3.4): trigger evaluation (rising-edge spawn, FR-LW-020 draws)
            //           runs BEFORE the §6.2 expiry sweep, so a same-tick-spawned arc is subject to this
            //           tick's expiry only if already expired (it cannot be — lifetime >= 1). Trigger
            //           evaluation is skipped when either the evaluator or the canon source is null (the
            //           sanctioned not-wired seam / opt-out, arc-triggers-design KD-5).
            if (_arcs != null)
            {
                if (_arcTriggers != null && canon != null)
                {
                    _arcTriggers.Evaluate(canon, _memory, _arcs, _clock.CurrentWorldTick);
                }
                _arcs.Update(_clock.CurrentWorldTick);
            }

            // Phase 5 — background-tier update (§3.5): BackgroundTierSim is a follow-up slice (its
            //           producers — the abstracted club-AI and canonical systems — do not exist).

            // Phase 6 — budget/eviction (§4.5): the §3.5 external-cap LRU enforcement runs here
            //           (arc-pinned edges skipped; a mid-cycle transient over-cap shrinks back once
            //           arcs resolve). SAVE_SIZE_BUDGET is platform-tuned [GT] via the config loader
            //           (not declarable as a literal — see LivingWorldConstants); per-edge depth
            //           eviction is enforced inline at MemoryStore.RecordEpisode.
            if (_membership != null)
            {
                _membership.EnforceExternalCap();
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-02 | —      | Initial implementation (season/world loop slice 1): clock     |
// |         |            |        | advance + phase-3 decay; phases 1/2/4/5/6 documented seams.   |
// | 1.1     | 2026-07-02 | —      | Slice 2: phase 4 wired to ArcEngine.Update (expiry sweep) and |
// |         |            |        | phase 6 to ActiveSetMembership.EnforceExternalCap; both       |
// |         |            |        | injectable as null = not-wired seam (2-arg ctor kept).        |
// | 1.2     | 2026-07-02 | —      | Slice-2 AR-3 L-2 (doc-only): Spec header gains FR-LW-014/023  |
// |         |            |        | — the citations had drifted from the wired phases 4/6.       |
// | 1.3     | 2026-07-24 | —      | Arc-triggers Slice 1/E1 (KD-5): 5-arg ctor carries the        |
// |         |            |        | nullable ArcTriggerEvaluator; RunWorldTick(canon) evaluates   |
// |         |            |        | triggers in phase 4 before the expiry sweep (skipped when     |
// |         |            |        | evaluator or canon is null). 2-arg/4-arg ctors + arg-less     |
// |         |            |        | RunWorldTick preserved (existing hosts unchanged).           |
#endregion
