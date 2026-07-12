// File:     src/positioning-ai/RotationController.cs
// Created:  2026-07-11
// Modified: 2026-07-11
// Author:   —
// Spec:     Positional Rotations #25 §3.1–§3.4 (FM-RO-01/02), §4.2, FR-RO-001..018, Code Standards #20
// Purpose:  Per-team rotation controller: evaluates the organic-exchange trigger against the
//           previous heartbeat's composed targets, commits/reverts atomic pairwise SlotIndex swaps
//           under dwell/hold hysteresis, and owns the serialized LastComposedTarget cache.

using System;

using UnityEngine;

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.PositioningAI
{
    /// <summary>
    /// One team's positional-rotation controller (#25). Runs each heartbeat after phase
    /// classification and BEFORE <see cref="SlotComposer"/> (FR-RO-008 / §4.2), so consumers always
    /// see a consistent post-swap binding within a tick. It is the SOLE post-seed writer of
    /// <see cref="AgentPositioningData.SlotIndex"/> (the #12 §3.7.1 single-writer contract,
    /// ERR-012-009). The §3.1 predicate consumes the PREVIOUS heartbeat's composed targets held in
    /// the controller-owned <c>LastComposedTarget</c> cache (§4.2 — serialized state, FR-RO-013:
    /// restore loads it verbatim; a re-seed would break byte-identity). Pure-deterministic: no RNG
    /// draw site, no domain tag (FR-RO-015); zero allocation after construction.
    /// </summary>
    public sealed class RotationController
    {
        // Adjacency table for this team's formation family (FR-RO-002 — the only eligible pairs).
        private readonly RotationPair[] _pairs;

        // Per-pair persistent state, table-row order (the FR-RO-013 serialization order).
        private readonly RotationPairState[] _pairStates;

        // The slot-binding permutation: _slotOfAgent[rosterIndex] = bound slot index, with
        // _agentAtSlot as its inverse. Identity at seed; mutated ONLY by the §3.3 atomic swap.
        private readonly int[] _slotOfAgent;
        private readonly int[] _agentAtSlot;

        // §2.2.4 / §4.2: per-agent (roster order) composed slot target from the previous heartbeat.
        private readonly Vector2[] _lastComposedTargets;

        private readonly int _squadSize;

        /// <summary>Number of adjacency-table rows (serialization bound, FR-RO-013).</summary>
        public int PairCount => _pairs.Length;

        /// <summary>Squad size this controller was built for.</summary>
        public int SquadSize => _squadSize;

        public RotationController(FormationFamily family, int squadSize)
        {
            if (squadSize <= 1)
            {
                throw new ArgumentOutOfRangeException(nameof(squadSize), squadSize,
                    "Rotation controller needs a GK + at least one outfield slot.");
            }

            var pairs = RotationAdjacencyCatalogue.GetPairs(family);
            _pairs = new RotationPair[pairs.Count];
            for (int r = 0; r < pairs.Count; r++) _pairs[r] = pairs[r];

            _pairStates          = new RotationPairState[_pairs.Length];
            _slotOfAgent         = new int[squadSize];
            _agentAtSlot         = new int[squadSize];
            _lastComposedTargets = new Vector2[squadSize];
            _squadSize           = squadSize;

            ResetToIdentity();
        }

        /// <summary>
        /// Resets to the seeded state: identity binding, zero pair state, zero cache. Called at
        /// construction and by <see cref="PositioningAITick.SeedFromFormation"/> (the cache is then
        /// seeded from the initial compose via <see cref="WriteBackComposedTargets"/>).
        /// </summary>
        public void ResetToIdentity()
        {
            for (int i = 0; i < _squadSize; i++)
            {
                _slotOfAgent[i] = i;
                _agentAtSlot[i] = i;
                _lastComposedTargets[i] = Vector2.zero;
            }
            for (int r = 0; r < _pairStates.Length; r++)
            {
                _pairStates[r] = default;
            }
        }

        /// <summary>
        /// One heartbeat of the controller (§3.2, FM-RO-02): evaluates trigger/revert predicates in
        /// ascending table-row order, commits at most <c>ROTATION_MAX_PER_TICK</c> swap per call,
        /// then applies the (possibly updated) binding onto <paramref name="snapshot"/>'s agents so
        /// the downstream stages compose against the post-swap binding (FR-RO-008). At
        /// <see cref="RotationFreedom.Off"/> no evaluation runs (FR-RO-011) and, while the binding
        /// is the identity, the snapshot is left untouched — byte-identical to pre-#25.
        /// </summary>
        /// <param name="snapshot">This heartbeat's filled snapshot (agents at array position = roster index).</param>
        /// <param name="phase">The committed team phase this heartbeat.</param>
        /// <param name="formation">The family's formation table (re-derives the slot's Role on rebind).</param>
        public void Run(PositioningPerceptionSnapshot snapshot, Phase phase, FormationSlotRecord[] formation)
        {
            RotationFreedom freedom = snapshot.RotationFreedom;
            if (freedom != RotationFreedom.Off)
            {
                // F5: refuse an undefined dial byte at the consuming seam (fail loud, never clamp).
                if (freedom != RotationFreedom.Conservative && freedom != RotationFreedom.Free)
                {
                    throw new ArgumentOutOfRangeException(nameof(snapshot), freedom,
                        "Undefined RotationFreedom at the #25 consuming seam (F5).");
                }
                Evaluate(snapshot, phase, freedom);
            }

            ApplyBinding(snapshot, formation);
        }

        /// <summary>
        /// Post-compose hook (§4.2): caches each agent's composed slot target for the NEXT
        /// heartbeat's §3.1 predicate. A sentinel (inactive-agent) target is skipped — the cache
        /// keeps its last finite value, and the F4 finite gate makes the predicate false for that
        /// agent meanwhile.
        /// </summary>
        /// <param name="outSlots">The composed slots this heartbeat, indexed by slot index.</param>
        public void WriteBackComposedTargets(Vector2[] outSlots)
        {
            for (int i = 0; i < _squadSize; i++)
            {
                Vector2 target = outSlots[_slotOfAgent[i]];
                if (float.IsNegativeInfinity(target.x)) continue; // sentinel — keep last finite value
                _lastComposedTargets[i] = target;
            }
        }

        // ── §3.2 evaluation ───────────────────────────────────────────────────────────

        private void Evaluate(PositioningPerceptionSnapshot snapshot, Phase phase, RotationFreedom freedom)
        {
            bool inPhase = phase == Phase.InPoss || phase == Phase.TransToAtk;

            // §3.4 / FR-RO-010: out of phase, dwell freezes (not reset) and hold countdowns continue;
            // committed rotations persist. No predicate is evaluated.
            if (!inPhase)
            {
                for (int r = 0; r < _pairStates.Length; r++)
                {
                    if (_pairStates[r].Rotated && _pairStates[r].HoldTicksRemaining > 0)
                    {
                        _pairStates[r].HoldTicksRemaining--;
                    }
                }
                return;
            }

            float advantage = PositioningAIConstants.ROTATION_ADVANTAGE_M
                            * (freedom == RotationFreedom.Conservative
                                ? PositioningAIConstants.ROTATION_ADVANTAGE_SCALAR_CONSERVATIVE
                                : PositioningAIConstants.ROTATION_ADVANTAGE_SCALAR_FREE);

            int teamCommitsThisTick = 0;

            for (int r = 0; r < _pairs.Length; r++)
            {
                ref RotationPairState state = ref _pairStates[r];

                // §3.3 partner lock (FR-RO-009): a row sharing a slot with ANOTHER committed
                // rotation is skipped and its dwell resets. (Slot-sharing ≡ agent-sharing here —
                // a swap exchanges occupants between exactly its own two slots.)
                if (!state.Rotated && SharesSlotWithRotatedRow(r))
                {
                    state.TriggerDwellTicks = 0;
                    continue;
                }

                if (!state.Rotated)
                {
                    bool predicate = SwapPredicate(snapshot, in _pairs[r], advantage);
                    state.TriggerDwellTicks = predicate ? state.TriggerDwellTicks + 1 : 0;
                    if (state.TriggerDwellTicks >= PositioningAIConstants.ROTATION_TRIGGER_DWELL_TICKS
                        && teamCommitsThisTick < PositioningAIConstants.ROTATION_MAX_PER_TICK)
                    {
                        SwapBinding(in _pairs[r]);                     // §3.3 atomic pairwise swap
                        state.Rotated            = true;
                        state.HoldTicksRemaining = PositioningAIConstants.ROTATION_HOLD_TICKS;
                        state.TriggerDwellTicks  = 0;
                        teamCommitsThisTick++;
                    }
                }
                else
                {
                    if (state.HoldTicksRemaining > 0)
                    {
                        state.HoldTicksRemaining--;
                        // §3.2: revert becomes evaluable on the SAME heartbeat the hold reaches 0
                        // (worked example: commit at 204, hold 30 ⇒ revert evaluable from 234).
                        if (state.HoldTicksRemaining > 0) continue;
                    }
                    // Revert: the same §3.1 form against the swapped targets — which IS the same
                    // predicate against the CURRENT occupants (reverting is swapping A↔B again).
                    // Commit direction is defined by table order + Rotated state (F3).
                    bool revert = SwapPredicate(snapshot, in _pairs[r], advantage);
                    state.TriggerDwellTicks = revert ? state.TriggerDwellTicks + 1 : 0;
                    if (state.TriggerDwellTicks >= PositioningAIConstants.ROTATION_TRIGGER_DWELL_TICKS
                        && teamCommitsThisTick < PositioningAIConstants.ROTATION_MAX_PER_TICK)
                    {
                        SwapBinding(in _pairs[r]);
                        state.Rotated           = false;
                        state.TriggerDwellTicks = 0;
                        teamCommitsThisTick++;
                    }
                }
            }
        }

        /// <summary>
        /// FM-RO-01 (§3.1): both current occupants closer to each other's PREVIOUS-heartbeat
        /// composed target than to their own by ≥ the dial-scaled advantage. Pure geometry;
        /// F4: any non-finite position/target makes the predicate false (never propagate NaN).
        /// </summary>
        private bool SwapPredicate(PositioningPerceptionSnapshot snapshot, in RotationPair pair, float advantage)
        {
            int agentA = _agentAtSlot[pair.SlotA];
            int agentB = _agentAtSlot[pair.SlotB];

            ref readonly AgentPositioningData a = ref snapshot.Agents[agentA];
            ref readonly AgentPositioningData b = ref snapshot.Agents[agentB];
            if (!a.IsActive || !b.IsActive) return false;

            Vector2 pA = a.Position;
            Vector2 pB = b.Position;
            Vector2 tA = _lastComposedTargets[agentA];   // slot A's previous target (its occupant's cache)
            Vector2 tB = _lastComposedTargets[agentB];

            if (!IsFinite(pA) || !IsFinite(pB) || !IsFinite(tA) || !IsFinite(tB)) return false;

            float gainA = Vector2.Distance(pA, tA) - Vector2.Distance(pA, tB);
            float gainB = Vector2.Distance(pB, tB) - Vector2.Distance(pB, tA);

            // NaN-gate form: a NaN gain fails the ≥ compare (compares false).
            return gainA >= advantage && gainB >= advantage;
        }

        private bool SharesSlotWithRotatedRow(int row)
        {
            for (int r = 0; r < _pairs.Length; r++)
            {
                if (r == row || !_pairStates[r].Rotated) continue;
                if (_pairs[r].Contains(_pairs[row].SlotA) || _pairs[r].Contains(_pairs[row].SlotB))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>§3.3: writes both agents' bindings in one step — no observable intermediate (FR-RO-008).</summary>
        private void SwapBinding(in RotationPair pair)
        {
            int agentA = _agentAtSlot[pair.SlotA];
            int agentB = _agentAtSlot[pair.SlotB];
            _slotOfAgent[agentA] = pair.SlotB;
            _slotOfAgent[agentB] = pair.SlotA;
            _agentAtSlot[pair.SlotA] = agentB;
            _agentAtSlot[pair.SlotB] = agentA;
        }

        /// <summary>
        /// Applies the binding onto the snapshot's agent rows (the sole post-seed SlotIndex writer,
        /// ERR-012-009). An agent whose binding matches its filled SlotIndex is left untouched, so
        /// an identity binding never rewrites a row (byte-identical for every pre-#25 fill,
        /// including fills that carry non-formation Role values). On a rebind the Role is re-derived
        /// from the newly bound slot's formation record — the pull-factor row is a property of the
        /// SLOT's positional duty, so the exchanged agents fully exchange positional targets
        /// (FR-RO-001: the persistent role/duty/PlayerTactic surfaces are untouched; this is the
        /// per-fill snapshot derivation, which the orchestrator also derives from the slot).
        /// </summary>
        private void ApplyBinding(PositioningPerceptionSnapshot snapshot, FormationSlotRecord[] formation)
        {
            int n = Math.Min(_squadSize, snapshot.Agents.Length);
            for (int i = 0; i < n; i++)
            {
                int boundSlot = _slotOfAgent[i];
                ref readonly AgentPositioningData a = ref snapshot.Agents[i];
                if (a.SlotIndex == boundSlot) continue;

                snapshot.Agents[i] = new AgentPositioningData(
                    entityId:     a.EntityId,
                    slotIndex:    boundSlot,
                    position:     a.Position,
                    isActive:     a.IsActive,
                    role:         formation[boundSlot].Role,
                    isGoalkeeper: a.IsGoalkeeper);
            }
        }

        // ── Serialization accessors (FR-RO-013, #25 Appendix B order) ────────────────

        /// <summary>The bound slot index for the given roster index (Appendix B block 1).</summary>
        public int GetSlotOfAgent(int rosterIndex) => _slotOfAgent[rosterIndex];

        /// <summary>The cached previous-heartbeat composed target for the given roster index (Appendix B block 2).</summary>
        public Vector2 GetLastComposedTarget(int rosterIndex) => _lastComposedTargets[rosterIndex];

        /// <summary>The per-pair state for the given table row (Appendix B block 3).</summary>
        public RotationPairState GetPairState(int row) => _pairStates[row];

        // ── Validating restore seams (FR-RO-013 F2/F6 gates) ─────────────────────────

        /// <summary>
        /// Restores the full slot-binding permutation. Fails loud (F2) when the set is not a
        /// permutation of [0, squadSize), or when the goalkeeper binding (roster 0 ↔ slot 0) is
        /// disturbed — the GK appears in no adjacency row (FR-RO-003), so any non-identity GK
        /// binding is corrupt state.
        /// </summary>
        public void RestoreBinding(ReadOnlySpan<int> slotOfAgent)
        {
            if (slotOfAgent.Length != _squadSize)
            {
                throw new ArgumentException(
                    $"Binding length {slotOfAgent.Length} != squad size {_squadSize} (F2).");
            }
            if (slotOfAgent[0] != 0)
            {
                throw new ArgumentException("Goalkeeper binding must be identity (FR-RO-003 / F2).");
            }
            // Permutation check without allocation: mark seen slots in _agentAtSlot AFTER validation,
            // using a stack-bounded seen mask.
            Span<bool> seen = stackalloc bool[_squadSize];
            for (int i = 0; i < _squadSize; i++)
            {
                int s = slotOfAgent[i];
                if (s < 0 || s >= _squadSize || seen[s])
                {
                    throw new ArgumentException(
                        $"Deserialized SlotIndex set is not a permutation (agent {i} → slot {s}) (F2).");
                }
                seen[s] = true;
            }
            for (int i = 0; i < _squadSize; i++)
            {
                _slotOfAgent[i] = slotOfAgent[i];
                _agentAtSlot[slotOfAgent[i]] = i;
            }
        }

        /// <summary>
        /// Restores one pair row's state. Fails loud (F6) on incoherent state: negative dwell,
        /// HoldTicksRemaining above <c>ROTATION_HOLD_TICKS</c>, or a positive hold while not
        /// rotated. (No dwell upper bound — the per-tick commit cap can legitimately defer a
        /// commit past the threshold.)
        /// </summary>
        public void RestorePairState(int row, in RotationPairState state)
        {
            if (row < 0 || row >= _pairStates.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(row), row, "Pair row out of table range (F6).");
            }
            if (state.TriggerDwellTicks < 0)
            {
                throw new ArgumentException("TriggerDwellTicks negative (F6).");
            }
            if (state.HoldTicksRemaining < 0
                || state.HoldTicksRemaining > PositioningAIConstants.ROTATION_HOLD_TICKS)
            {
                throw new ArgumentException(
                    $"HoldTicksRemaining {state.HoldTicksRemaining} outside [0, {PositioningAIConstants.ROTATION_HOLD_TICKS}] (F6).");
            }
            if (!state.Rotated && state.HoldTicksRemaining > 0)
            {
                throw new ArgumentException("HoldTicksRemaining > 0 while not Rotated (F6).");
            }
            _pairStates[row] = state;
        }

        /// <summary>
        /// Restores one agent's cached previous-heartbeat composed target verbatim (FR-RO-013 —
        /// PASS-1 H-1: a re-seed would break byte-identity). Fails loud (F6) on a non-finite value.
        /// </summary>
        public void RestoreLastComposedTarget(int rosterIndex, Vector2 target)
        {
            if (rosterIndex < 0 || rosterIndex >= _squadSize)
            {
                throw new ArgumentOutOfRangeException(nameof(rosterIndex), rosterIndex, "Roster index out of range (F6).");
            }
            if (!IsFinite(target))
            {
                throw new ArgumentException("Non-finite LastComposedTarget (F6).");
            }
            _lastComposedTargets[rosterIndex] = target;
        }

        private static bool IsFinite(Vector2 v)
        {
            return !float.IsNaN(v.x) && !float.IsInfinity(v.x)
                && !float.IsNaN(v.y) && !float.IsInfinity(v.y);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                             |
// | 1.0     | 2026-07-11 | —      | Initial implementation (#25 wiring): FM-RO-01 predicate on the    |
// |         |            |        |   serialized LastComposedTarget cache, FM-RO-02 dwell/commit/     |
// |         |            |        |   revert/hold, §3.3 atomic swap + partner lock, §3.4 phase-exit   |
// |         |            |        |   freeze, F2/F5/F6 validating seams. Identity binding leaves the  |
// |         |            |        |   snapshot untouched (byte-identical default).                    |
#endregion
