// File: src/positioning-ai/PositioningPerceptionSnapshot.cs
// Created:  2026-05-29
// Modified: 2026-07-11
// Author:   —
// Spec: #12 Positioning AI §4.3, §3.0, §3.2; Dismarking #23 §2.2.3/§3.3; Build-Up #24 §3.1/§3.2; Rotations #25 §4.3
// Purpose: Pre-allocated perception input consumed by PositioningAITick; filled each 10 Hz tick by the orchestrator.

using UnityEngine;

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.PositioningAI
{
    /// <summary>
    /// Perception data required by the positioning system for one tactical tick.
    /// Allocated once per match by the orchestrator; refilled in-place each tick to avoid heap churn.
    /// Stale detection: if TickIndex &lt; currentTick, PositioningAITick returns previous slots (F1 recovery).
    /// </summary>
    public sealed class PositioningPerceptionSnapshot
    {
        /// <summary>Monotonically increasing tick counter from the 10 Hz loop. Used for F1 stale detection.</summary>
        public int TickIndex;

        /// <summary>Ball world-space position (Z = height per #1 §1.2). Pitch corner-origin.</summary>
        public Vector3 BallPosition;

        /// <summary>
        /// Longitudinal (X-axis) ball velocity filtered over the last few frames (m/s).
        /// Used by PhaseClassifier to distinguish TransToAtk from TransToDef when ball is loose.
        /// Positive = moving toward opponent goal (increasing X).
        /// </summary>
        public float BallVxFiltered;

        /// <summary>
        /// EntityId of the agent currently in possession, or -1 if ball is loose.
        /// FR-PA-022: phase classified locally from possession state.
        /// </summary>
        public int PossessionOwnerEntityId;

        /// <summary>True when PossessionOwnerEntityId ≥ 0 and that agent belongs to the team being processed.</summary>
        public bool PossessionOwnerIsOwnTeam;

        /// <summary>
        /// Per-agent data for all squad members, sorted by EntityId ascending at fill time.
        /// Length = SquadSize (set at construction). Slot index matches HysteresisState.Agents indexing.
        /// </summary>
        public readonly AgentPositioningData[] Agents;

        /// <summary>Number of valid active outfield agents (excludes GK). Optimisation hint; updated each fill.</summary>
        public int ActiveOutfieldCount;

        // ── #23 Dismarking routing + per-agent carriers (FR-DM-015 / §3.2 one-stride contract) ──

        /// <summary>
        /// #23 routing field (FR-DM-015): the team's DismarkIntensity dial, populated solely by the
        /// match-engine Phase-D writer from the active TeamTactic. Zero value = Off = the exact
        /// identity (#23 §2.2.3 — safe unseeded), so an unfilled snapshot never dismarks.
        /// </summary>
        public DismarkIntensity DismarkIntensity;

        /// <summary>
        /// #23 per-agent MarkingPressure (FM-DM-01, ∈ [0,1]), parallel to <see cref="Agents"/> by array
        /// position. Computed by the orchestrator from the PREVIOUS stride's FilteredView + the current
        /// dwell state (the §3.2 PASS-1 M-1 one-stride-stale contract: positioning runs before the
        /// per-agent perception pass, so the views it derives from are one stride old). The FR-DM-006
        /// phase gate is applied by the SlotComposer stage with this tick's committed phase, so this
        /// value is the UNGATED proximity × dwell product.
        /// </summary>
        public readonly float[] MarkingPressure;

        /// <summary>#23 per-agent flag: a qualifying perceived marker exists (parallel to <see cref="Agents"/>).</summary>
        public readonly bool[] HasMarker;

        /// <summary>
        /// #23 per-agent nearest qualifying marker's PERCEIVED position (parallel to <see cref="Agents"/>),
        /// in the same team-canonical attack-toward-+X frame as agent positions. Undefined when
        /// <see cref="HasMarker"/> is false. Never ground truth (FR-DM-001/004 — sourced from the agent's
        /// own FilteredView at the match-engine call seam, #23 §4.4).
        /// </summary>
        public readonly Vector2[] MarkerPosition;

        // ── #24 Build-Up routing + per-team zone carriers (FR-BU-012 / §3.1) ─────────────────────

        /// <summary>
        /// #24 routing field (FR-BU-012): the team's BuildUpStructure dial, populated solely by the
        /// match-engine Phase-D writer. Zero value = None = the exact identity (FR-BU-005).
        /// </summary>
        public BuildUpStructure BuildUpStructure;

        /// <summary>
        /// #24: the committed hysteresis zone this heartbeat (FM-BU-01), classified once per team by the
        /// orchestrator BEFORE the positioning tick (#24 §3.1 ordering rule). Zero value = OwnThird, a
        /// valid kickoff-adjacent default.
        /// </summary>
        public BuildUpZone BuildUpCommittedZone;

        /// <summary>
        /// #24: whether the post-regain suppression window (FM-BU-03) is open this heartbeat — read from
        /// the per-team state's pre-decrement value by the orchestrator. True suppresses the overlay
        /// stage (FR-BU-004). Zero value (false) = window closed = identity.
        /// </summary>
        public bool BuildUpSuppressed;

        // ── #25 Rotations routing (FR-RO-014) ────────────────────────────────────────────────────

        /// <summary>
        /// #25 routing field (FR-RO-014): the team's RotationFreedom dial, populated solely by the
        /// match-engine Phase-D writer. Zero value = Off = the exact identity (FR-RO-011 — the
        /// RotationController performs no evaluation at Off).
        /// </summary>
        public RotationFreedom RotationFreedom;

        /// <param name="squadSize">Exactly 11 for Stage 0 (1 GK + 10 outfield).</param>
        public PositioningPerceptionSnapshot(int squadSize)
        {
            Agents          = new AgentPositioningData[squadSize];
            MarkingPressure = new float[squadSize];
            HasMarker       = new bool[squadSize];
            MarkerPosition  = new Vector2[squadSize];
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
// | 1.1     | 2026-07-11 | —      | #23/#24/#25 wiring: DismarkIntensity + per-agent pressure/marker |
// |         |            |        |   carriers (one-stride-stale per §3.2 M-1); BuildUpStructure +   |
// |         |            |        |   committed zone + suppression flag (classified by the           |
// |         |            |        |   orchestrator pre-tick); RotationFreedom. All zero defaults are |
// |         |            |        |   the exact identities, so existing fills stay byte-identical.   |
#endregion
