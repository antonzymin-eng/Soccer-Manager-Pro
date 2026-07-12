// File: src/positioning-ai/SlotComposer.cs
// Created: 2026-05-29
// Modified: 2026-07-11
// Author: —
// Spec: #12 Positioning AI §3.7 (§3.7.1 as amended by ERR-012-007/008); Build-Up #24 §3.2 (FM-BU-02);
//       Dismarking #23 §3.3 (FM-DM-02)
// Purpose: Orchestrates the per-agent slot composition pipeline, including the #24 build-up
//          overlay and #23 dismark offset stages.

using UnityEngine;
using Unity.Profiling;

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.PositioningAI
{
    /// <summary>
    /// Composes per-agent formation slots through the pipeline (§3.7 / §3.7.1 as amended by
    /// ERR-012-007/008 — the combined #23/#24 stage order pinned in #24 §4.2):
    ///   1. Compute anchor (§3.1)
    ///   2. Add ball-relative offset (§3.2)
    ///   3. Apply context modifiers (§3.5) — operates on (baseSlot − centroid)
    ///   3b. Build-up overlay (#24 §3.2, FM-BU-02) — additive per-slot displacement, BEFORE spacing
    ///       (structure proposes a shape; spacing resolves conflicts inside it, FR-BU-007)
    ///   4. Enforce hard spacing — up to SPACING_MAX_PASSES (§3.6)
    ///   4b. Dismark offset (#23 §3.3, FM-DM-02) — AFTER spacing, BEFORE the pitch clamp
    ///       (FR-DM-008: the clamp keeps the evasion nudge on-pitch)
    ///   5. Clamp to pitch bounds [0.5, 104.5] × [0.5, 67.5] (FR-PA-033)
    ///   6. Resolve line/lane membership with hysteresis (§3.3, §3.4) — AFTER steps 4+5 per AR-S1-03
    ///   7. Write results into outSlots (caller reads from there)
    ///
    /// Both inserted stages are exact no-ops at their zero-value dials (None / Off) and outside
    /// their gating phase, so a default match is byte-identical to the 7-step pipeline.
    /// GK handled via dedicated formula (§3.3.3), written separately before step 4; the GK slot is
    /// excluded from both inserted stages (FR-BU-009 / FR-DM-007).
    /// </summary>
    public static class SlotComposer
    {
        private static readonly ProfilerMarker s_ComposeMarker =
            new ProfilerMarker("PositioningAI.Compose");

        /// <summary>
        /// Executes the full 7-step pipeline. Writes computed slots into outSlots in-place.
        /// outSlots[0] = GK slot; outSlots[1..10] = outfield slots.
        /// Inactive agents receive SENTINEL_NO_SLOT; line/lane resolution is skipped for them.
        /// </summary>
        public static void Compose(
            PositioningPerceptionSnapshot snapshot,
            FormationFamily               archetype,
            ContextModifierInputs         modifiers,
            Phase                         phase,
            HysteresisState               hyst,
            Vector2[]                     outSlots,
            Vector2[]                     anchorBuffer,  // pre-allocated caller buffer, length = SQUAD_SIZE
            int[]                         entityIdArr)   // pre-allocated EntityId scratch; length = SQUAD_SIZE (FR-PA-006)
        {
            using var _ = s_ComposeMarker.Auto();

            FormationSlotRecord[] formation = PositioningAIConstants.GetFormationSlots(archetype);

            // ── Steps 1+2: Compute base slots (anchor + ball-relative offset) ──────
            // GK: dedicated formula. FR-PA-044 / F3: the GK slot is a composed slot, so a
            // NaN intermediate (e.g. a NaN ball position — Mathf.Clamp passes NaN through per
            // the project NaN-gate hazard) must fall back to the raw GK formation anchor, not
            // be emitted as NaN. The pre-fix code guarded only outfield agents (ERR-012-004).
            Vector2 gkSlot = AnchorCalculator.ComputeGkSlot(snapshot.BallPosition);
            if (float.IsNaN(gkSlot.x) || float.IsNaN(gkSlot.y))
                gkSlot = AnchorCalculator.ComputeAnchor(formation[0]);
            outSlots[0] = gkSlot;

            // Outfield agents in SlotIndex order (0=GK excluded here, 1-10=outfield).
            for (int i = 0; i < snapshot.Agents.Length; i++)
            {
                ref readonly AgentPositioningData agent = ref snapshot.Agents[i];
                int idx = agent.SlotIndex;

                if (agent.IsGoalkeeper) continue;  // GK handled above.

                // FR-PA-036: inactive agents → SENTINEL; skip all further computation.
                if (!agent.IsActive)
                {
                    outSlots[idx] = PositioningAIConstants.SENTINEL_NO_SLOT;
                    anchorBuffer[idx] = PositioningAIConstants.SENTINEL_NO_SLOT;
                    continue;
                }

                Vector2 anchor   = AnchorCalculator.ComputeAnchor(formation[idx]);
                Vector2 offset   = AnchorCalculator.ComputeBallRelativeOffset(
                                       snapshot.BallPosition, agent.Role, phase);
                Vector2 baseSlot = anchor + offset;

                // FR-PA-044 / F3: NaN guard — replace with raw anchor.
                if (float.IsNaN(baseSlot.x) || float.IsNaN(baseSlot.y))
                    baseSlot = anchor;

                outSlots[idx]    = baseSlot;
                anchorBuffer[idx] = anchor;
            }
            // GK anchor for spacing resolver.
            anchorBuffer[0] = outSlots[0];

            // ── Step 3: Context modifiers ─────────────────────────────────────────
            ContextModifier.ApplyToAll(outSlots, snapshot, modifiers, phase);

            // NaN guard pass after context modifiers.
            for (int i = 0; i < snapshot.Agents.Length; i++)
            {
                ref readonly AgentPositioningData a = ref snapshot.Agents[i];
                if (a.IsGoalkeeper || !a.IsActive) continue;
                int idx = a.SlotIndex;
                if (float.IsNaN(outSlots[idx].x) || float.IsNaN(outSlots[idx].y))
                    outSlots[idx] = anchorBuffer[idx];
            }

            // ── Step 3b: Build-up overlay (#24 §3.2, FM-BU-02) ────────────────────
            // FR-BU-004 gate: dial ≠ None AND phase = InPoss AND committed zone ∈ {Own, Middle}
            // AND the post-regain suppression window is closed. The zone/suppression carriers are
            // classified once per team by the orchestrator BEFORE this tick (#24 §3.1). Rows are
            // keyed by the slot's RECORDED DefaultLine/DefaultLane (FR-BU-007 / ERR-024-001); the
            // lateral sign resolves toward pitch centre from the current composed target's y (§3.2).
            // GK excluded (FR-BU-009); FinalThird resolves to zero inside the catalogue.
            if (snapshot.BuildUpStructure != BuildUpStructure.None
                && phase == Phase.InPoss
                && !snapshot.BuildUpSuppressed
                && snapshot.BuildUpCommittedZone != BuildUpZone.FinalThird)
            {
                for (int i = 0; i < snapshot.Agents.Length; i++)
                {
                    ref readonly AgentPositioningData a = ref snapshot.Agents[i];
                    if (a.IsGoalkeeper || !a.IsActive) continue;
                    int idx = a.SlotIndex;
                    Vector2 delta = BuildUpOverlayCatalogue.GetOffset(
                        snapshot.BuildUpStructure,
                        snapshot.BuildUpCommittedZone,
                        formation[idx].DefaultLine,
                        formation[idx].DefaultLane,
                        outSlots[idx].y);
                    outSlots[idx] += delta;
                }
            }

            // ── Step 4: Hard spacing ──────────────────────────────────────────────
            // Populate pre-allocated entityIdArr (passed in from PositioningAITick; zero alloc FR-PA-006).
            for (int i = 0; i < snapshot.Agents.Length; i++)
                entityIdArr[snapshot.Agents[i].SlotIndex] = snapshot.Agents[i].EntityId;

            SpacingResolver.EnforceHardSpacing(outSlots, anchorBuffer, entityIdArr);

            // ── Step 4b: Dismark offset (#23 §3.3, FM-DM-02) ──────────────────────
            // AFTER spacing, BEFORE the pitch clamp (FR-DM-008 — the clamp keeps the nudge on-pitch).
            // Gates: dial ≠ Off (FR-DM-012, checked first) AND phase = InPoss (FR-DM-006, applied
            // HERE with this tick's committed phase — the snapshot pressure is the ungated
            // proximity × dwell product); the ball carrier and the GK are excluded (FR-DM-007).
            // The pressure/marker carriers are one-stride-stale by the §3.2 PASS-1 M-1 contract
            // (positioning runs before the per-agent perception pass in the stride order).
            // ComputeOffset internally applies the pressure floor (FR-DM-009), the F3 coincident-
            // marker guard, and the F1 NaN gates.
            if (snapshot.DismarkIntensity != DismarkIntensity.Off && phase == Phase.InPoss)
            {
                for (int i = 0; i < snapshot.Agents.Length; i++)
                {
                    ref readonly AgentPositioningData a = ref snapshot.Agents[i];
                    if (a.IsGoalkeeper || !a.IsActive) continue;
                    if (!snapshot.HasMarker[i]) continue;
                    // FR-DM-007: the ball carrier keeps its composed target (it is playing the ball,
                    // not making an off-ball dismark run).
                    if (snapshot.PossessionOwnerIsOwnTeam
                        && a.EntityId == snapshot.PossessionOwnerEntityId) continue;

                    int idx = a.SlotIndex;
                    outSlots[idx] += MarkingPressureEvaluator.ComputeOffset(
                        a.Position,
                        snapshot.MarkerPosition[i],
                        snapshot.MarkingPressure[i],
                        snapshot.DismarkIntensity);
                }
            }

            // ── Step 5: Pitch-bound clamp ─────────────────────────────────────────
            for (int i = 0; i < PositioningAIConstants.SQUAD_SIZE; i++)
            {
                if (IsSentinel(outSlots[i])) continue;
                outSlots[i] = ClampToPitch(outSlots[i]);
            }

            // ── Step 6: Line/lane resolution AFTER spacing+clamp (AR-S1-03) ───────
            ShapeAnalyzer.ResolveAllLines(outSlots, archetype, hyst);
            ShapeAnalyzer.ResolveAllLanes(outSlots, hyst);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static Vector2 ClampToPitch(Vector2 slot)
        {
            return new Vector2(
                Mathf.Clamp(slot.x, PositioningAIConstants.SLOT_X_MIN, PositioningAIConstants.SLOT_X_MAX),
                Mathf.Clamp(slot.y, PositioningAIConstants.SLOT_Y_MIN, PositioningAIConstants.SLOT_Y_MAX));
        }

        private static bool IsSentinel(Vector2 v) => float.IsNegativeInfinity(v.x);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                                              |
// | 1.0     | 2026-05-29 | —      | Initial implementation.                                                                            |
// | 1.1     | 2026-06-13 | —      | ERR-012-004 / FR-PA-044: GK composed slot now NaN-guarded to the raw GK formation anchor. The F3   |
// |         |            |        | guard previously covered only outfield agents, so a NaN ball position emitted a NaN GK slot.       |
// | 1.2     | 2026-07-11 | —      | #23/#24 stage insertions (ERR-012-007/008; the #24 §4.2 combined order): Step 3b build-up overlay  |
// |         |            |        | (FM-BU-02, after ContextModifier / before spacing) + Step 4b dismark offset (FM-DM-02, after       |
// |         |            |        | spacing / before the pitch clamp). Both exact no-ops at the zero-value dials — byte-identical      |
// |         |            |        | default pipeline.                                                                                  |
#endregion
