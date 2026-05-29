// File:     src/perception-system/FilteredView.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Perception System #7 §3.7, §2.3, Code Standards #20
// Purpose:  Authoritative struct definitions for FilteredView (Decision Tree consumer output),
//           PerceptionDiagnostics (filter metadata, never delivered to DT),
//           PerceivedAgent (per-entity sub-struct), ShoulderCheckAnimData (Stage 1+ stub),
//           OcclusionDebugRecord (editor/debug only), PerceivedAgentDebug (editor/debug only).
//           OWNED BY THIS SPECIFICATION. See §2.3.5 amendment authority.

using UnityEngine;

namespace TacticalDirector.PerceptionSystem
{
    /// <summary>
    /// FilteredView — the agent's filtered knowledge of the world at one heartbeat.
    ///
    /// Produced once per 10Hz heartbeat per agent. Passed by value to the Decision Tree.
    /// Contains ONLY confirmed, filtered, latency-resolved world-state data.
    /// Does NOT contain filter metadata (FoV angles, pressure scalars, animation stubs) —
    /// those are in PerceptionDiagnostics (§3.7.2), which is never delivered to the DT.
    ///
    /// OWNERSHIP: Perception System Specification #7 owns this struct definition.
    /// CONSUMERS: Decision Tree Specification #8 (sole consumer at Stage 0).
    /// MODIFICATION: Any field change requires a version bump of this specification.
    ///
    /// Stage 0 note: VisibleTeammates, VisibleOpponents, and BlindSidePerceivedAgents
    /// reference pre-allocated fixed-size arrays. The *Count fields track valid entries
    /// within those arrays. This is a Stage 0 implementation detail to satisfy INV-10
    /// (zero per-heartbeat heap allocation). The Decision Tree reads entries [0..Count-1].
    /// Perception System #7 §3.7.1.
    /// </summary>
    public struct FilteredView
    {
        // ── Identity and Timing ──────────────────────────────────────────────────────

        /// <summary>AgentId (array index 0–21) of the agent this view describes. §3.7.1.</summary>
        public int ObserverId;

        /// <summary>Heartbeat tick (10Hz counter) at which this view was built. DT uses this to detect stale views. §3.7.1.</summary>
        public int FrameNumber;

        /// <summary>True if this view was produced by a mid-heartbeat forced refresh (§3.8). False for standard 10Hz views. §3.7.1.</summary>
        public bool ForcedRefreshThisTick;

        // ── Ball State ───────────────────────────────────────────────────────────────

        /// <summary>True if ball passed all three visibility tests (range, FoV, occlusion) this tick. Ball is never subject to L_rec (OQ-2). §3.7.1.</summary>
        public bool BallVisible;

        /// <summary>
        /// Last confirmed ball position (2D XY). If BallVisible=true: equals ground truth this tick.
        /// If BallVisible=false: retains last confirmed position (stale). Never (0,0) after first sighting.
        /// INVARIANT: BallStalenessFrames == 0 iff BallVisible == true (INV-8). §3.7.1.
        /// </summary>
        public Vector2 BallPerceivedPosition;

        /// <summary>
        /// Consecutive heartbeats during which BallVisible was false. 0 = currently visible.
        /// DT should weight ball confidence inversely with this value. §3.7.1.
        /// </summary>
        public int BallStalenessFrames;

        // ── Visible Agents (Forward Arc) ─────────────────────────────────────────────

        /// <summary>
        /// Pre-allocated array of confirmed visible teammates (within FoV, not occluded, L_rec elapsed).
        /// Valid entries: [0..VisibleTeammatesCount-1]. Capacity: MaxVisibleTeammates. §3.7.1.
        /// </summary>
        public PerceivedAgent[] VisibleTeammates;

        /// <summary>Number of valid entries in VisibleTeammates. Stage 0 implementation detail for INV-10. §3.7.1.</summary>
        public int VisibleTeammatesCount;

        /// <summary>
        /// Pre-allocated array of confirmed visible opponents (within FoV, not occluded, L_rec elapsed).
        /// Valid entries: [0..VisibleOpponentsCount-1]. Capacity: MaxVisibleOpponents. §3.7.1.
        /// </summary>
        public PerceivedAgent[] VisibleOpponents;

        /// <summary>Number of valid entries in VisibleOpponents. Stage 0 implementation detail for INV-10. §3.7.1.</summary>
        public int VisibleOpponentsCount;

        // ── Blind-Side Agents (Shoulder Check) ───────────────────────────────────────

        /// <summary>
        /// Agents confirmed during an active shoulder check window (§3.4.3).
        /// Empty when no check window is active or when no agents have been confirmed.
        /// Valid entries: [0..BlindSidePerceivedAgentsCount-1]. Capacity: MaxBlindSideAgents. §3.7.1.
        /// </summary>
        public PerceivedAgent[] BlindSidePerceivedAgents;

        /// <summary>Number of valid entries in BlindSidePerceivedAgents. Stage 0 implementation detail for INV-10. §3.7.1.</summary>
        public int BlindSidePerceivedAgentsCount;
    }

    /// <summary>
    /// PerceptionDiagnostics — filter metadata for one agent at one heartbeat.
    ///
    /// Contains information about HOW the perception filter operated, NOT what the agent
    /// knows about the world. This struct is NEVER delivered to the Decision Tree.
    ///
    /// CONSUMERS: Debug tooling, telemetry, balance analysis, Animation System (Stage 1+).
    /// OWNERSHIP: Perception System Specification #7. Same amendment authority as FilteredView.
    /// Perception System #7 §3.7.2.
    /// </summary>
    public struct PerceptionDiagnostics
    {
        // ── Correlation Keys ─────────────────────────────────────────────────────────

        /// <summary>Matches FilteredView.ObserverId for the same heartbeat. §3.7.2.</summary>
        public int ObserverId;

        /// <summary>Matches FilteredView.FrameNumber for the same heartbeat. §3.7.2.</summary>
        public int FrameNumber;

        // ── Filter State ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Actual FoV full angle (degrees) after Decisions modifier and pressure narrowing.
        /// Range: [MIN_FOV_ANGLE, BASE_FOV_ANGLE + MAX_FOV_BONUS_ANGLE] = [120°, 170°]. §3.7.2.
        /// </summary>
        public float EffectiveFoVAngle;

        /// <summary>Pressure scalar [0–1] at the time of filter execution. Computed from opponent proximity (§3.6). §3.7.2.</summary>
        public float PressureScalar;

        // ── Shoulder Check State ─────────────────────────────────────────────────────

        /// <summary>True during an active shoulder check window (§3.4.3). §3.7.2.</summary>
        public bool BlindSideWindowActive;

        /// <summary>Heartbeat tick at which the current shoulder check window expires. Meaningless if BlindSideWindowActive = false. §3.7.2.</summary>
        public float BlindSideWindowExpiry;

        // ── Animation Stub ───────────────────────────────────────────────────────────

        /// <summary>Stage 1+ stub. Populated at Stage 0; no consumer until Animation System is written. §3.4.4. §3.7.2.</summary>
        public ShoulderCheckAnimData ShoulderCheckAnim;
    }

    /// <summary>
    /// PerceivedAgent — data for one visible agent in a FilteredView.
    ///
    /// All fields reflect agent state at the time of view construction.
    /// At Stage 0: position and velocity are ground truth for confirmed entities.
    /// At Stage 1+: error modelling will introduce uncertainty into these values.
    /// Perception System #7 §3.7.3.
    /// </summary>
    public struct PerceivedAgent
    {
        /// <summary>AgentState array index (0–21) of the perceived agent. Never equals the ObserverId. §3.7.3.</summary>
        public int AgentId;

        /// <summary>Ground-truth 2D position at time of view construction (Stage 0). Stage 1+: includes perception error. §3.7.3.</summary>
        public Vector2 PerceivedPosition;

        /// <summary>Ground-truth 2D velocity at time of view construction (Stage 0). Stage 1+: includes direction estimation error. §3.7.3.</summary>
        public Vector2 PerceivedVelocity;

        /// <summary>[0.0, 1.0]. At Stage 0: 1.0 for all confirmed entries. Stage 1+: degrades with distance and staleness. §3.7.3.</summary>
        public float ConfidenceScore;
    }

    /// <summary>
    /// ShoulderCheckAnimData — stub for Stage 1+ Animation System.
    /// Populated at Stage 0 and carried in PerceptionDiagnostics; no consumer at Stage 0.
    /// Perception System #7 §3.4.4.
    /// </summary>
    public struct ShoulderCheckAnimData
    {
        /// <summary>AgentId for which this check was triggered. §3.4.4.</summary>
        public int AgentId;

        /// <summary>Heartbeat tick on which the check was triggered. §3.4.4.</summary>
        public int FireFrame;

        /// <summary>Degrees: which side the check was directed (left/right of the rear). §3.4.4.</summary>
        public float CheckDirection;

        /// <summary>True if at least one blind-side entity was confirmed during this window. §3.4.4.</summary>
        public bool AnyEntityConfirmed;
    }

    /// <summary>
    /// OcclusionDebugRecord — populated only when perception debug mode is active.
    /// Not part of the production FilteredView. Used by Stage 1+ visual debug tooling.
    /// Perception System #7 §3.7.4.
    /// </summary>
    public struct OcclusionDebugRecord
    {
        /// <summary>Observer agent ID. §3.7.4.</summary>
        public int ObserverId;

        /// <summary>Target entity ID. §3.7.4.</summary>
        public int TargetId;

        /// <summary>True if target was occluded by any shadow cone this tick. §3.7.4.</summary>
        public bool IsOccluded;

        /// <summary>Agent ID of the occluder. -1 if not occluded. §3.7.4.</summary>
        public int OccludingAgentId;

        /// <summary>Half-angle (degrees) of the occluding agent's shadow cone. §3.7.4.</summary>
        public float ShadowHalfAngle;

        /// <summary>Heartbeat tick when record was captured. §3.7.4.</summary>
        public int FrameNumber;
    }

#if UNITY_EDITOR
    /// <summary>
    /// PerceivedAgentDebug — editor-only diagnostic fields for confirmed perception entries.
    /// Never compiled into release builds. Used alongside PerceivedAgent for debug rendering.
    /// Perception System #7 §2.3.3.
    /// </summary>
    public struct PerceivedAgentDebug
    {
        /// <summary>AgentId matching the corresponding PerceivedAgent.AgentId. §2.3.3.</summary>
        public int AgentId;

        /// <summary>True if this entity was discarded by shadow cone test at Step 4 this tick. §2.3.3.</summary>
        public bool WasOccludedThisTick;

        /// <summary>Raw latency counter value when entity was confirmed. §2.3.3.</summary>
        public int LatencyCounterAtConfirm;

        /// <summary>Half-angle (degrees) of the occluding cone if WasOccludedThisTick = true. §2.3.3.</summary>
        public float ShadowConeAngle;
    }
#endif
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
