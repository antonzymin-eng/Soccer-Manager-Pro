// File:     src/living-world/ArcTriggerEvaluator.cs
// Created:  2026-07-24
// Modified: 2026-07-24 (arc-triggers Low-1: delegate to MemoryEpisode.MoreSalientThan)
// Author:   —
// Spec:     Living World System #22 §3.4, §6.2, FR-LW-016/017/018/020/021/031, arc-triggers-design
//           KD-3/KD-4/KD-5/KD-7, Code Standards #20
// Purpose:  Arc trigger evaluator: registers the world.arcs RNG sub-stream, walks the ArcTriggerCatalogue
//           over an ArcCanonSource in FR-LW-017/021 order, applies the KD-7 rising-edge latch, and
//           spawns arcs (via ArcEngine.SpawnArc) on a threshold crossing.

using System;
using System.Collections.Generic;

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.LivingWorld
{
    /// <summary>
    /// Evaluates the §3.4 arc triggers against an <see cref="ArcCanonSource"/> and spawns arcs. It owns
    /// the periodic tick-driven <c>world.arcs</c> RNG sub-stream (FR-LW-020 — distinct key from
    /// <c>world.text</c>, KD-4/KD-5) and the KD-7 armed-off latch (the set of currently-latched
    /// <c>(scopeKey, TriggerId)</c> pairs), which is this evaluator's only cross-tick state.
    ///
    /// FIRING (KD-7, edge-triggered with a latch): a trigger fires on the tick its signal first crosses
    /// from below its threshold to <c>&gt;= Threshold</c>, then stays armed-off (latched) until the
    /// signal drops back below (re-arm). Only the rising edge draws + spawns; a sustained-high signal
    /// spawns exactly one arc, and a non-edge tick leaves the <c>world.arcs</c> cursor untouched (the
    /// <see cref="InteractionTextGenerator"/> "a refusal consumes no cursor" discipline). NaN fails
    /// closed (treated as below threshold). Scope key = entity id (entity-scoped) or
    /// <see cref="LivingWorldConstants.ARC_BOARD_SCOPE_KEY"/> (board/squad-level).
    ///
    /// EVALUATION ORDER (FR-LW-017/021, the load-bearing determinism property): entity-scoped triggers
    /// outer over <see cref="ArcCanonSource.EntityIdAt"/> ascending, inner over the entity catalogue
    /// rows in order; then board triggers by ArcKind ordinal.
    ///
    /// SERIALIZATION (arc-triggers-design KD-6): at Slice-2/E2 the <c>world.arcs</c> cursor + the KD-7
    /// latch are serialized into the composite world save (<c>WORLD_STORE_FORMAT_VERSION</c> 3), so a
    /// flag-on <c>save@N → restore → advance</c> round-trips deterministically and does not re-fire a
    /// still-latched trigger (the completeness lock). The evaluator exposes the latch via
    /// <see cref="EnumerateLatchedCanonical"/> / <see cref="LatchedCount"/> / <see cref="RestoreLatched"/>
    /// (it owns the state; <see cref="WorldStore"/> owns the byte layout).
    ///
    /// Day-cadence — not subject to the 60 Hz zero-alloc / ProfilerMarker hot-path rules.
    /// </summary>
    public sealed class ArcTriggerEvaluator
    {
        private readonly DeterministicRngService _rng;
        private readonly int _streamIndex;
        private readonly int _managerId;
        private readonly HashSet<LatchEntry> _latched = new HashSet<LatchEntry>();

        /// <summary>
        /// Registers the <c>world.arcs</c> sub-stream (FR-LW-020) on the injected service, with the
        /// DISTINCT world-scoped entity sentinel <see cref="LivingWorldConstants.WORLD_STREAM_ENTITY_ARCS"/>
        /// (−2, NOT the world.text −1 — else ComputeStreamKey collides, KD-4). Registration is
        /// unconditional at boot in a fixed position (after world.text) so the stream index is
        /// positionally stable across save/restore. <paramref name="managerId"/> keys the manager→entity
        /// edges the evaluator resolves pins from (§3.4).
        /// </summary>
        public ArcTriggerEvaluator(DeterministicRngService rng, int managerId)
        {
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            _managerId = managerId;
            _streamIndex = rng.RegisterStream(
                LivingWorldConstants.WORLD_ARCS_STREAM_SITE_ID,
                SubsystemOrdinals.LivingWorld,
                LivingWorldConstants.WORLD_STREAM_ENTITY_ARCS,
                LivingWorldConstants.WORLD_ARCS_STREAM_VERSION);
        }

        /// <summary>The registered <c>world.arcs</c> stream index on the injected service (for the
        /// composition root to serialize/restore the cursor and for the E1 fail-loud guard).</summary>
        public int StreamIndex => _streamIndex;

        /// <summary>Number of currently armed-off <c>(scopeKey, TriggerId)</c> latch pairs (KD-7). The
        /// E1 fail-loud guard checks this; test 10 asserts it drops on re-arm.</summary>
        public int LatchedCount => _latched.Count;

        /// <summary>Test seam: the live <c>world.arcs</c> stream cursor (0 for a run that never fired).</summary>
        internal ulong RngCursor => _rng.GetStreamState(_streamIndex).RngCursor;

        /// <summary>
        /// Enumerates the KD-7 armed-off latch in canonical order (ScopeKey ascending, then TriggerId
        /// ascending) so the serialized byte stream is deterministic (E2, §8.6). The evaluator owns the
        /// state; the composition root owns the byte layout (the GK/Heading CaptureState/RestoreState
        /// split).
        /// </summary>
        internal LatchEntry[] EnumerateLatchedCanonical()
        {
            LatchEntry[] arr = new LatchEntry[_latched.Count];
            _latched.CopyTo(arr);
            Array.Sort(arr, static (a, b) =>
            {
                int c = a.ScopeKey.CompareTo(b.ScopeKey);
                return c != 0 ? c : a.TriggerId.CompareTo(b.TriggerId);
            });
            return arr;
        }

        /// <summary>
        /// Replaces the latch with a restored set (E2, §8.6). Fails loud if the entries are not in
        /// strict canonical (ScopeKey, TriggerId) order or contain a duplicate — a tampered/foreign
        /// block, so the serialized set's canonical-order invariant is enforced on read, not trusted.
        /// </summary>
        internal void RestoreLatched(LatchEntry[] entries)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }
            _latched.Clear();
            for (int i = 0; i < entries.Length; i++)
            {
                if (i > 0)
                {
                    LatchEntry prev = entries[i - 1];
                    LatchEntry cur = entries[i];
                    bool strictlyAfter = cur.ScopeKey > prev.ScopeKey
                        || (cur.ScopeKey == prev.ScopeKey && cur.TriggerId > prev.TriggerId);
                    if (!strictlyAfter)
                    {
                        throw new InvalidOperationException(
                            "ArcTriggerEvaluator.RestoreLatched: latch block is not in strict canonical (scopeKey, triggerId) order — tampered or foreign block.");
                    }
                }
                _latched.Add(entries[i]);
            }
        }

        /// <summary>
        /// Evaluates every catalogue trigger against <paramref name="canon"/> for the given calendar
        /// day, firing arcs on the KD-7 rising edge (FR-LW-017/021 order). Fires zero or more arcs;
        /// draws exactly one <c>world.arcs</c> value per rising edge (and none otherwise).
        /// </summary>
        public void Evaluate(ArcCanonSource canon, MemoryStore memory, ArcEngine arcs, uint worldTick)
        {
            if (canon == null) throw new ArgumentNullException(nameof(canon));
            if (memory == null) throw new ArgumentNullException(nameof(memory));
            if (arcs == null) throw new ArgumentNullException(nameof(arcs));

            // 1. Entity-scoped triggers: outer over entities ascending, inner over catalogue rows.
            IReadOnlyList<ArcTrigger> entityTriggers = ArcTriggerCatalogue.EntityTriggers;
            int entityCount = canon.EntitySignalCount;
            for (int ei = 0; ei < entityCount; ei++)
            {
                int entityId = canon.EntityIdAt(ei);
                for (int ti = 0; ti < entityTriggers.Count; ti++)
                {
                    ArcTrigger trigger = entityTriggers[ti];
                    float signal = canon.EntitySignal(ei, trigger.SignalKey);
                    EvaluateTrigger(entityId, in trigger, signal, memory, arcs, worldTick);
                }
            }

            // 2. Board/squad-level triggers: by ArcKind ordinal (the catalogue is authored in that order).
            IReadOnlyList<ArcTrigger> boardTriggers = ArcTriggerCatalogue.BoardTriggers;
            for (int ti = 0; ti < boardTriggers.Count; ti++)
            {
                ArcTrigger trigger = boardTriggers[ti];
                float signal = canon.BoardSignal(trigger.Kind, trigger.SignalKey);
                EvaluateTrigger(LivingWorldConstants.ARC_BOARD_SCOPE_KEY, in trigger, signal, memory, arcs, worldTick);
            }
        }

        // The four-state armed/latched table (KD-7, §2.8): the rising edge is the sole draw+spawn tick.
        private void EvaluateTrigger(int scopeKey, in ArcTrigger trigger, float signal,
            MemoryStore memory, ArcEngine arcs, uint worldTick)
        {
            LatchEntry key = new LatchEntry(scopeKey, trigger.TriggerId);
            bool armedOff = _latched.Contains(key);
            // NaN fails closed: NaN >= Threshold is false, so a non-finite signal is treated as below.
            bool above = signal >= trigger.Threshold;

            if (!armedOff)
            {
                if (above)
                {
                    FireArc(scopeKey, in trigger, signal, memory, arcs, worldTick);
                }
                // else: armed + below ⇒ no-op, no draw.
            }
            else if (!above)
            {
                _latched.Remove(key); // re-arm: latched + below ⇒ remove, no draw.
            }
            // else: latched + above ⇒ still latched, no-op, no draw.
        }

        private void FireArc(int scopeKey, in ArcTrigger trigger, float signal,
            MemoryStore memory, ArcEngine arcs, uint worldTick)
        {
            // Rising edge, in order (arc-triggers-design §8.4):
            // 1. Draw one world.arcs value — the Stage-0 stochastic component. It is recorded as the
            //    SpawnCause.SnapshotRef (a real, deterministic consumer of the draw; the full
            //    accept/shape use is Stage-1+), so the draw is never a dead read.
            ulong draw = DrawArc();

            // 2. Resolve pins (entity-scoped: the manager→entity edge's most-salient episode; board or
            //    edge-less: pin-less — a valid pin-less spawn, NOT a skip). The threshold crossing above
            //    is the SOLE pre-draw refusal; an empty pin set never suppresses a spawn.
            Arc.PinnedEpisode[] pins = ResolvePins(scopeKey, trigger.IsEntityScoped, memory);

            // 3. Build provenance inline (it cannot be reconstructed later, per the SpawnCause contract)
            //    and spawn. SpawnArc does the atomic FR-LW-018 pinning + rollback and the ArcKind /
            //    lifetime / overflow gates — pure functions of the row + spawnTick, so a correct
            //    catalogue always passes; a mis-authored row is a fail-loud corruption abort (the whole
            //    AdvanceDay throws), not a graceful skip.
            SpawnCause.Input[] inputs = { new SpawnCause.Input(trigger.SignalKey, signal) };
            SpawnCause cause = new SpawnCause(trigger.TriggerId, inputs, draw, worldTick);
            arcs.SpawnArc(trigger.Kind, in cause, pins, worldTick, trigger.MaxLifetimeDays);

            // 4. Latch AFTER a successful SpawnArc return, so a corruption-abort never leaves a phantom
            //    armed-off entry.
            _latched.Add(new LatchEntry(scopeKey, trigger.TriggerId));
        }

        // One draw per rising edge (the InteractionTextGenerator.Generate discipline). A non-edge tick
        // never reaches here, so a flag-off (null-canon) run never advances the cursor (E1 byte-identity).
        private ulong DrawArc()
        {
            if (_rng.Reserve(_streamIndex, 1) != 0)
            {
                throw new InvalidOperationException(
                    "ArcTriggerEvaluator: a world.arcs reservation is already open (draw-site misuse).");
            }
            if (_rng.DrawReserved(_streamIndex, 0, out ulong draw) != 0)
            {
                // Unreachable under the API contract (Reserve(1) set DeclaredBudget = 1); a failure means
                // corrupt reservation state. Close the now-open reservation so the stream stays usable,
                // then abort.
                _rng.CloseReservation(_streamIndex);
                throw new InvalidOperationException(
                    "ArcTriggerEvaluator: world.arcs draw failed — corrupt reservation state (internal invariant).");
            }
            _rng.CloseReservation(_streamIndex); // advances RngCursor by the declared budget (1).
            return draw;
        }

        // Entity-scoped: pin the single most-salient episode on the manager→entity edge (a minimal,
        // deterministic FR-LW-018 source pin; the full source set is Stage-1+). Board/squad-level or an
        // edge with no episodes: pin-less (Array.Empty) — a valid pin-less spawn.
        private Arc.PinnedEpisode[] ResolvePins(int scopeKey, bool isEntityScoped, MemoryStore memory)
        {
            if (!isEntityScoped)
            {
                return Array.Empty<Arc.PinnedEpisode>();
            }
            if (!memory.TryGetEdge(_managerId, scopeKey, out RelationshipEdge edge)
                || edge.Memory == null || edge.Memory.Length == 0)
            {
                return Array.Empty<Arc.PinnedEpisode>();
            }

            MemoryEpisode[] mem = edge.Memory;
            MemoryEpisode best = mem[0];
            for (int i = 1; i < mem.Length; i++)
            {
                // Shared canonical (salience → worldTick → episodeId) order — the same total order
                // WorldStore's §3.2 auto-cite selection uses, so both pick the same source episode.
                if (MemoryEpisode.MoreSalientThan(in mem[i], in best))
                {
                    best = mem[i];
                }
            }
            return new[] { new Arc.PinnedEpisode(edge.FromId, edge.ToId, best.EpisodeId) };
        }

        /// <summary>
        /// One armed-off latch entry (KD-7): a <c>(scopeKey, TriggerId)</c> pair. Scope key is an entity
        /// id (entity-scoped) or <see cref="LivingWorldConstants.ARC_BOARD_SCOPE_KEY"/> (board-level).
        /// </summary>
        internal readonly struct LatchEntry : IEquatable<LatchEntry>
        {
            public readonly int ScopeKey;
            public readonly ushort TriggerId;

            public LatchEntry(int scopeKey, ushort triggerId)
            {
                ScopeKey = scopeKey;
                TriggerId = triggerId;
            }

            public bool Equals(LatchEntry other) => ScopeKey == other.ScopeKey && TriggerId == other.TriggerId;

            public override bool Equals(object obj) => obj is LatchEntry e && Equals(e);

            public override int GetHashCode() => unchecked((ScopeKey * 397) ^ TriggerId);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-24 | —      | Initial implementation (arc-triggers Slice 1/E1): world.arcs  |
// |         |            |        | stream registration (distinct KD-4 key), FR-LW-017/021        |
// |         |            |        | evaluation order, KD-7 rising-edge armed-off latch, per-edge  |
// |         |            |        | draw + pin resolution + SpawnArc. Latch not yet serialized    |
// |         |            |        | (flag-on save fails loud in WorldStore.Snapshot — E1).        |
// | 1.1     | 2026-07-24 | —      | Slice 2/E2: EnumerateLatchedCanonical / RestoreLatched latch  |
// |         |            |        | serialization seams (canonical-order enumerate; strict-order  |
// |         |            |        | fail-loud restore). WorldStore now serializes the cursor +    |
// |         |            |        | latch (WORLD_STORE_FORMAT_VERSION 3); no forward-behaviour    |
// |         |            |        | change.                                                       |
// | 1.2     | 2026-07-24 | —      | arc-triggers Low-1 (maintainability): the private IsMoreSalient|
// |         |            |        | comparator was a copy of WorldStore's IsMoreCitable; both now  |
// |         |            |        | delegate to the shared static MemoryEpisode.MoreSalientThan.   |
#endregion
