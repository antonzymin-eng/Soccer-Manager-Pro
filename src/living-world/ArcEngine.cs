// File:     src/living-world/ArcEngine.cs
// Created:  2026-07-02
// Modified: 2026-07-02 (slice-2 AR-1: M-1 pin-array snapshot, L-1 overflow gate)
// Author:   —
// Spec:     Living World System #22 §3.4, §3.6, §6.2, FR-LW-014/016/017/018/020/021/028/031, Code Standards #20
// Purpose:  Emergent-arc lifecycle engine: §3.4 spawn (SpawnCause capture + FR-LW-018 episode pinning),
//           state advancement, resolve/unpin, and the per-tick §6.2 max-lifetime expiry sweep.
//           Trigger evaluation (reading canon) is a documented seam — see the class doc.

using System;
using System.Collections.Generic;

namespace TacticalDirector.LivingWorld
{
    /// <summary>
    /// Owns the live narrative arcs (#22 §3.4). The WorldLoop phase-4 entry point is
    /// <see cref="Update"/>, which runs the deterministic lifecycle duties that exist at this slice:
    /// the §6.2 expiry sweep (no arc instance stays live past its <c>MaxLifetimeTick</c> — expired
    /// arcs are force-resolved and their episodes unpinned, FR-LW-014).
    ///
    /// TRIGGER-EVALUATION SEAM (FR-LW-031): the §3.4 spawn *triggers* read canonical vol-2/vol-3
    /// human-systems state (pulse divergence, ego clash, board patience) that does not exist yet, and
    /// their stochastic components draw from the dedicated periodic <c>world.arcs</c>
    /// `DeterministicRngService` sub-stream (FR-LW-020 — separate from the aperiodic
    /// <c>world.text</c> stream so player-triggered text never perturbs the arc cursor). This slice
    /// therefore registers NO stream and holds NO rng reference — a registered stream with zero draw
    /// sites would be exactly the phantom surface FR-LW-031 forbids. The trigger evaluator (canonical
    /// entity-ID order for entity-scoped triggers, ArcKind ordinal order for squad/board-level ones,
    /// FR-LW-017/021) lands with the KD-10 canon producers and calls <see cref="SpawnArc"/>, which is
    /// the complete §3.4 step 1–3 spawn path and is fully live today.
    ///
    /// Live arcs are held in spawn order — deterministic because trigger evaluation order is
    /// (FR-LW-017/021) — and every sweep iterates that order. All state is plain serialisable value
    /// state (FR-LW-022); the §4.6 snapshot-schema entry remains a follow-up. Day-cadence: not subject
    /// to the 60 Hz zero-alloc / ProfilerMarker hot-path rules.
    /// </summary>
    public sealed class ArcEngine
    {
        /// <summary>
        /// Highest currently-defined <see cref="ArcKind"/> ordinal. The roster is APPEND-only
        /// (FR-LW-028); extend this alongside any roster append so the spawn gate keeps refusing
        /// undefined ordinals (the AR-2 M-1 junk-cast class) while admitting new members.
        /// </summary>
        private const ArcKind MaxDefinedArcKind = ArcKind.WonderkidVsVeteran;

        private readonly MemoryStore _memory;
        private readonly List<Arc> _arcs = new List<Arc>();

        /// <summary>Number of live arcs.</summary>
        public int ArcCount => _arcs.Count;

        /// <summary>
        /// Returns the live arc at the given spawn-order index. NOTE: the returned struct's
        /// <c>PinnedEpisodes</c> array is the engine's LIVE internal buffer (the spawn-time snapshot,
        /// AR-1 M-1), not a copy — callers must treat it as read-only (exposed-array hand-over
        /// convention, MemoryStore.GetEdgeAt parallel). Indices are stable only until the next
        /// <see cref="ResolveArc"/> / <see cref="Update"/> (removals compact the list).
        /// </summary>
        public Arc GetArcAt(int index) => _arcs[index];

        public ArcEngine(MemoryStore memory)
        {
            _memory = memory ?? throw new ArgumentNullException(nameof(memory));
        }

        /// <summary>
        /// §3.4 steps 1–3: spawns an arc with its provenance (<paramref name="cause"/>, captured
        /// inline by the caller at trigger time per FR-LW-016/KD-8 — never reconstructed) and pins
        /// every source episode non-evictable until the arc resolves (FR-LW-018). Pinning is atomic:
        /// if any episode is dangling (F1 — MemoryStore fails loud), every hold already taken by this
        /// call is released before the exception propagates, so a failed spawn leaks no pins.
        /// State starts at 0 (state0); byte values per kind are APPEND-only (FR-LW-028).
        /// </summary>
        /// <param name="maxLifetimeDays">
        /// Per-kind [GT] liveness bound in calendar days, in [1, ARC_MAX_LIFETIME_DAYS] (§6.2 — the
        /// catalogue value is the global cap; per-kind values land with the trigger catalogue).
        /// </param>
        /// <returns>The spawn-order index of the new arc (see the <see cref="GetArcAt"/> stability caveat).</returns>
        public int SpawnArc(ArcKind kind, in SpawnCause cause, Arc.PinnedEpisode[] pinnedEpisodes, uint spawnTick, uint maxLifetimeDays)
        {
            if (kind > MaxDefinedArcKind)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind,
                    "ArcEngine.SpawnArc: not a defined ArcKind member (roster is APPEND-only, FR-LW-028).");
            }
            if (pinnedEpisodes == null)
            {
                throw new ArgumentException("ArcEngine.SpawnArc: pinnedEpisodes must be non-null (use Array.Empty).");
            }
            if (maxLifetimeDays < 1u || maxLifetimeDays > LivingWorldConstants.ARC_MAX_LIFETIME_DAYS)
            {
                throw new ArgumentOutOfRangeException(nameof(maxLifetimeDays), maxLifetimeDays,
                    "ArcEngine.SpawnArc: lifetime must be in [1, ARC_MAX_LIFETIME_DAYS] calendar days (§6.2).");
            }
            if (spawnTick > uint.MaxValue - maxLifetimeDays)
            {
                // AR-1 L-1: unchecked uint wrap would make MaxLifetimeTick tiny — an instantly
                // expired arc instead of a fail-loud refusal.
                throw new ArgumentOutOfRangeException(nameof(spawnTick), spawnTick,
                    "ArcEngine.SpawnArc: spawnTick + maxLifetimeDays overflows the calendar (uint).");
            }

            // AR-1 M-1: snapshot the caller's array BEFORE taking any hold — the arc must resolve
            // exactly the pins it took, and a caller mutating its array post-spawn would otherwise
            // desynchronise resolve/expiry unpinning from the pin table (a stale hold leaks — the F1
            // class — or UnpinEpisode fails loud on a key that was never pinned).
            Arc.PinnedEpisode[] pins = new Arc.PinnedEpisode[pinnedEpisodes.Length];
            Array.Copy(pinnedEpisodes, pins, pinnedEpisodes.Length);

            // §3.4 step 2 — pin the source episodes, atomically (roll back on an F1 dangling id).
            int pinned = 0;
            try
            {
                for (; pinned < pins.Length; pinned++)
                {
                    _memory.PinEpisode(pins[pinned].EdgeFromId, pins[pinned].EdgeToId, pins[pinned].EpisodeId);
                }
            }
            catch
            {
                for (int i = 0; i < pinned; i++)
                {
                    _memory.UnpinEpisode(pins[i].EdgeFromId, pins[i].EdgeToId, pins[i].EpisodeId);
                }
                throw;
            }

            // §3.4 step 3 — instantiate.
            Arc arc = default;
            arc.Kind = kind;
            arc.State = 0;
            arc.Cause = cause;
            arc.PinnedEpisodes = pins;
            arc.SpawnTick = spawnTick;
            arc.MaxLifetimeTick = spawnTick + maxLifetimeDays;
            _arcs.Add(arc);
            return _arcs.Count - 1;
        }

        /// <summary>
        /// Advances the arc's kind-specific lifecycle state (§3.4 "resolves or escalates on subsequent
        /// results/manager choices"). The engine does not validate kind-specific state values — the
        /// per-kind state machines land with the trigger catalogue; byte values are APPEND-only per
        /// kind (FR-LW-028).
        /// </summary>
        public void AdvanceState(int index, byte state)
        {
            if ((uint)index >= (uint)_arcs.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "ArcEngine.AdvanceState: no live arc at this index.");
            }
            Arc arc = _arcs[index];
            arc.State = state;
            _arcs[index] = arc;
        }

        /// <summary>
        /// Resolves the arc: releases every pin hold it takes (FR-LW-018 — episodes become evictable
        /// only when the LAST holding arc releases; buffers shrink back toward depth on that release)
        /// and removes it from the live list. Routing the resolution's effects into canon (squad
        /// happiness, board action) is canon-owned (FR-LW-027/KD-9) and lands with the KD-10 wiring.
        /// </summary>
        public void ResolveArc(int index)
        {
            if ((uint)index >= (uint)_arcs.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "ArcEngine.ResolveArc: no live arc at this index.");
            }
            UnpinAll(_arcs[index]);
            _arcs.RemoveAt(index);
        }

        /// <summary>
        /// WorldLoop phase-4 per-tick lifecycle: force-resolves every arc past its liveness bound
        /// (<c>worldTick &gt; MaxLifetimeTick</c>, §6.2 — an arc cannot remain unresolved
        /// indefinitely), unpinning its episodes. Sweeps in spawn order — the fixed, deterministic
        /// order matters because unpin order can select different §3.2 shrink evictions when several
        /// expiring arcs pin episodes on the same edge. Trigger evaluation (spawning) remains the
        /// documented seam above.
        /// </summary>
        /// <returns>The number of arcs expired this tick.</returns>
        public int Update(uint worldTick)
        {
            int w = 0;
            for (int i = 0; i < _arcs.Count; i++)
            {
                if (_arcs[i].IsExpired(worldTick))
                {
                    UnpinAll(_arcs[i]);
                }
                else
                {
                    _arcs[w++] = _arcs[i];
                }
            }
            int expired = _arcs.Count - w;
            _arcs.RemoveRange(w, expired);
            return expired;
        }

        private void UnpinAll(in Arc arc)
        {
            Arc.PinnedEpisode[] pins = arc.PinnedEpisodes;
            for (int i = 0; i < pins.Length; i++)
            {
                _memory.UnpinEpisode(pins[i].EdgeFromId, pins[i].EdgeToId, pins[i].EpisodeId);
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-02 | —      | Initial implementation (season/world loop slice 2): §3.4      |
// |         |            |        | spawn with atomic FR-LW-018 pinning, state advancement,       |
// |         |            |        | resolve/unpin, §6.2 expiry sweep. Trigger evaluation + the    |
// |         |            |        | world.arcs RNG sub-stream documented as the KD-10 seam        |
// |         |            |        | (FR-LW-020/031 — no draw site exists, so no stream is         |
// |         |            |        | registered).                                                  |
// | 1.1     | 2026-07-02 | —      | AR-1 fix pass. M-1: SpawnArc snapshots the caller's pin array |
// |         |            |        | before taking holds — post-spawn mutation no longer           |
// |         |            |        | desynchronises resolve/expiry unpinning from the pin table.   |
// |         |            |        | L-1: spawnTick + maxLifetimeDays uint-overflow gate (wrap     |
// |         |            |        | made the arc instantly expired instead of failing loud).      |
#endregion
