// File: src/positioning-ai/PositioningAITick.cs
// Created:  2026-05-29
// Modified: 2026-06-27 (Match Engine Phase D D4: CaptureState snapshot seam)
// Author:   —
// Spec: #12 Positioning AI §3.7, §3.11, §4.3, FR-PA-001..006
// Purpose: 10 Hz entry point for Positioning AI. Classifies phase, delegates to SlotComposer,
//          and exposes per-tick accessors consumed by the match orchestrator.

using UnityEngine;
using Unity.Profiling;

namespace TacticalDirector.PositioningAI
{
    /// <summary>
    /// 10 Hz Positioning AI orchestrator for one team.
    /// Lifecycle: construct once per team per match → call SeedFromFormation() at match start →
    /// call Tick() every 10 Hz → read results via GetFormationSlot() / IsSentinelSlot().
    ///
    /// The orchestrator (external to this class) is responsible for:
    ///   (a) Filling the PositioningPerceptionSnapshot before each Tick().
    ///   (b) Writing GetFormationSlot(entityId) into TacticalContext.FormationSlot per agent (§4.4.3).
    ///   (c) Calling TacticalContext.Stage0Default(initialSlot) once at match init.
    ///
    /// All computation is deterministic per EntityId-ascending iteration order (#16 §3.2.5).
    /// FR-PA-006: hot path is zero-heap-allocation after construction.
    /// </summary>
    public sealed class PositioningAITick
    {
        // ── Profiler markers ──────────────────────────────────────────────────
        private static readonly ProfilerMarker s_TickMarker =
            new ProfilerMarker("PositioningAI.Tick");
        private static readonly ProfilerMarker s_GetSlotMarker =
            new ProfilerMarker("PositioningAI.GetFormationSlot");

        // ── Persistent state ──────────────────────────────────────────────────
        private readonly HysteresisState _hyst;
        private readonly FormationFamily _archetype;

        // ── Per-tick output buffer (pre-allocated, reused each tick) ──────────
        private readonly Vector2[] _slots;        // length = SQUAD_SIZE; index = SlotIndex
        private readonly Vector2[] _anchorBuf;    // scratch; length = SQUAD_SIZE
        private readonly int[]     _entityIdArr;  // scratch EntityId array; passed to SpacingResolver each tick (FR-PA-006)

        // ── EntityId → SlotIndex lookup (populated at SeedFromFormation) ─────
        // Cap at 64 entities (well above Stage-0 squad of 11).
        private readonly int[] _entityToSlotIndex; // entityId → slotIndex; -1 = not in squad
        private readonly int   _entityIdCapacity;

        // ── F1 stale detection ────────────────────────────────────────────────
        private int _lastProcessedTick = -1;

        public PositioningAITick(FormationFamily archetype, int maxEntityId = 64)
        {
            _archetype        = archetype;
            _hyst             = new HysteresisState(PositioningAIConstants.SQUAD_SIZE);
            _slots            = new Vector2[PositioningAIConstants.SQUAD_SIZE];
            _anchorBuf        = new Vector2[PositioningAIConstants.SQUAD_SIZE];
            _entityIdArr      = new int[PositioningAIConstants.SQUAD_SIZE];
            _entityIdCapacity = maxEntityId + 1;
            _entityToSlotIndex = new int[_entityIdCapacity];

            for (int i = 0; i < _entityIdCapacity; i++) _entityToSlotIndex[i] = -1;

            // Initialise slots to pitch centre as a safe default.
            var centre = new Vector2(
                PositioningAIConstants.PITCH_HALF_LENGTH_M,
                PositioningAIConstants.PITCH_HALF_WIDTH_M);
            for (int i = 0; i < _slots.Length; i++) _slots[i] = centre;
        }

        // ── Match initialisation ───────────────────────────────────────────────

        /// <summary>
        /// Seeds hysteresis state and entity→slot mapping from the perception snapshot.
        /// Must be called once at match start before the first Tick().
        /// Assumes snapshot.Agents is sorted by EntityId ascending (FR-PA-003).
        /// </summary>
        public void SeedFromFormation(PositioningPerceptionSnapshot snapshot)
        {
            if (snapshot.Agents.Length != PositioningAIConstants.SQUAD_SIZE)
                throw new System.ArgumentException(
                    $"Squad size mismatch: expected {PositioningAIConstants.SQUAD_SIZE}, got {snapshot.Agents.Length}.");

            FormationSlotRecord[] formation = PositioningAIConstants.GetFormationSlots(_archetype);
            _hyst.SeedFromFormation(formation);

            for (int i = 0; i < snapshot.Agents.Length; i++)
            {
                ref readonly AgentPositioningData a = ref snapshot.Agents[i];

                if (a.EntityId < _entityIdCapacity)
                    _entityToSlotIndex[a.EntityId] = a.SlotIndex;

                // Seed initial slot from formation anchor.
                if (!a.IsGoalkeeper)
                    _slots[a.SlotIndex] = AnchorCalculator.ComputeAnchor(formation[a.SlotIndex]);
                else
                    _slots[a.SlotIndex] = AnchorCalculator.ComputeGkSlot(snapshot.BallPosition);
            }
        }

        // ── Per-tick entry point ───────────────────────────────────────────────

        /// <summary>
        /// Executes one 10 Hz positioning tick.
        /// Updates internal slot buffer; results are immediately available via GetFormationSlot().
        /// </summary>
        /// <param name="snapshot">Current perception from Spec #7. Must have TickIndex ≥ 0.</param>
        /// <param name="modifiers">Match-context inputs from the orchestrator.</param>
        public void Tick(PositioningPerceptionSnapshot snapshot, ContextModifierInputs modifiers)
        {
            using var _ = s_TickMarker.Auto();

            // F1 — stale perception: reuse previous tick's slots (FR-PA-042).
            if (snapshot.TickIndex < _lastProcessedTick)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                UnityEngine.Debug.LogWarning(
                    $"[PositioningAI] F1 stale perception: tick {snapshot.TickIndex} < last {_lastProcessedTick}; reusing previous slots.");
#endif
                return;
            }

            // F2 — invalid archetype: already resolved at construction (constructor defaults to F442).

            // F6 — phase corruption guard: committed inside PhaseClassifier.
            Phase phase = PhaseClassifier.ClassifyAndCommit(snapshot, _hyst);

            SlotComposer.Compose(snapshot, _archetype, modifiers, phase, _hyst, _slots, _anchorBuf, _entityIdArr);

            _lastProcessedTick = snapshot.TickIndex;
        }

        // ── Result accessors ─────────────────────────────────────────────────

        /// <summary>Returns the computed formation slot for the given EntityId.</summary>
        /// <remarks>Returns SENTINEL_NO_SLOT if the EntityId is unknown or out of range.</remarks>
        public Vector2 GetFormationSlot(int entityId)
        {
            using var _ = s_GetSlotMarker.Auto();
            int slotIdx = GetSlotIndex(entityId);
            return slotIdx >= 0 ? _slots[slotIdx] : PositioningAIConstants.SENTINEL_NO_SLOT;
        }

        /// <summary>Returns true when the slot is the inactive-agent sentinel (−∞, −∞).</summary>
        public static bool IsSentinelSlot(Vector2 slot) => float.IsNegativeInfinity(slot.x);

        /// <summary>Returns the committed line membership for the given EntityId. Stage-1+ API (§4.5).</summary>
        public LineId GetLine(int entityId)
        {
            int idx = GetSlotIndex(entityId);
            return idx >= 0 ? _hyst.Agents[idx].CurrentLine : LineId.Defense;
        }

        /// <summary>Returns the committed lane membership for the given EntityId. Stage-1+ API (§4.5).</summary>
        public LaneId GetLane(int entityId)
        {
            int idx = GetSlotIndex(entityId);
            return idx >= 0 ? _hyst.Agents[idx].CurrentLane : LaneId.C;
        }

        /// <summary>Returns the committed team phase. Stage-1+ API (§4.5).</summary>
        public Phase GetPhase() => _hyst.CurrentPhase;

        /// <summary>
        /// Snapshot seam: returns the live team-level <see cref="HysteresisState"/> (phase dwell +
        /// per-agent line/lane membership) so a host snapshot layer can serialize this cross-tick state
        /// canonically for deterministic save/restore (parallel to the DecisionTree D0 / executor C0 /
        /// OscillationGuard B0 seams). The instance is returned by reference for read-only serialization;
        /// callers MUST NOT mutate it outside a sanctioned restore path.
        /// </summary>
        public HysteresisState CaptureState() => _hyst;

        // ── Private helpers ───────────────────────────────────────────────────

        private int GetSlotIndex(int entityId)
        {
            if (entityId < 0 || entityId >= _entityIdCapacity) return -1;
            return _entityToSlotIndex[entityId];
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
// | 1.1     | 2026-06-27 | —      | Match Engine Phase D D4 follow-up: CaptureState() snapshot seam |
// |         |            |        | exposes the live HysteresisState so the host snapshot layer can |
// |         |            |        | serialize the per-team cross-tick positioning hysteresis        |
// |         |            |        | (parallel to DecisionTree D0 / executor C0 / OscillationGuard   |
// |         |            |        | B0). Read-only serialization use; no behaviour change.          |
#endregion
