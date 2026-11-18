// File:     src/defensive-ai/DefensiveSnapshot.cs
// Created:  2026-05-29
// Modified: 2026-11-10 (W13.1 / ERR-014-010 — + MarkMovementActive per-tick input)
// Author:   —
// Spec:     Defensive AI #14 §2.3, §4.4, ERR-014-008, ERR-014-010, Code Standards #20; Tactical Instructions #21 §3.4 (FR-TI-022 / FR-TI-033)
// Purpose:  Full tick input container for DefensiveAITick. Populated once per tick by
//           the match orchestrator from #7, #12, #13 state before Tick() is called.

using UnityEngine;

using TacticalDirector.PositioningAI;
using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.DefensiveAI
{
    /// <summary>
    /// Full tick input for one Defensive AI tick, populated by the match orchestrator.
    /// All fields are read-only within the tick; no mid-tick re-reads occur (FR-DA-029 F1).
    /// Defensive AI #14 §2.3 / §4.4.
    ///
    /// <see cref="Agents"/> contains all up-to-22 agents (both teams). Callers filter by
    /// <see cref="DefensiveTeamId"/> to obtain own-team or opponent agents.
    ///
    /// <para>
    /// <b>Frame contract (ERR-014-008, #14 §2.3).</b> Defensive AI #14 is <b>canonical-only</b>. Every
    /// position and velocity in this snapshot (<see cref="BallPosition"/>, <see cref="BallVelocity"/>,
    /// <see cref="GkPosition"/>, and each <see cref="DefensiveAgentSnapshot"/> <c>Position</c> /
    /// <c>Velocity</c> / <c>BaselineSlot</c>, opponents included) is expressed in the <b>defending team's
    /// canonical attack-toward-+X frame</b>: the defended goal-line is x = 0 and the goal being attacked
    /// is x = 105, for BOTH teams. That is a fixed invariant of the input, not a switch: #14 has no
    /// "defends x = 105" mode, so a producer holding world-space data must rotate it (the away team's
    /// 180-degree pitch mirror) BEFORE filling the snapshot, as the composition root does. This is NOT
    /// world space. <see cref="DefensiveTeamId"/> identifies who is defending and which agents are
    /// own-team; it says nothing about which goal is defended and must never be used to pick one.
    /// </para>
    /// </summary>
    public sealed class DefensiveSnapshot
    {
        /// <summary>
        /// Tick index when this snapshot was captured. Used for F1 stale-perception detection
        /// (FR-DA-029): if snapshot.TickIndex &lt; lastProcessedTick, reuse previous tick output.
        /// </summary>
        public int TickIndex;

        /// <summary>
        /// TeamId of the team running the defensive algorithm (0 = home, 1 = away). Identifies the own
        /// team only; it does NOT select the defended goal (see the frame contract on the class).
        /// </summary>
        public int DefensiveTeamId;

        /// <summary>Ball position (X, Y) in the defending team's canonical frame. Z available but unused at Stage 0.</summary>
        public Vector2 BallPosition;

        /// <summary>Ball velocity vector (m/s). Magnitude used for offside-trap trigger (§3.7.2 condition 1).</summary>
        public Vector2 BallVelocity;

        /// <summary>EntityId of current ball possessor. -1 for loose ball.</summary>
        public int PossessionOwnerEntityId;

        /// <summary>
        /// Team phase for the defensive team, from Positioning AI #12 §3.0.
        /// Phase gate: if InPoss, #14 emits all-ZONAL and returns (FR-DA-013 / KD-19).
        /// </summary>
        public Phase TeamPhase;

        /// <summary>
        /// Defensive line depth (distToOwnGoal, m) from Positioning AI #12.
        /// Read-only mirror written to MarkDirective.OffensiveLineDepth (FR-DA-012).
        /// </summary>
        public float DefensiveLineDepth;

        /// <summary>
        /// EntityId of the defensive team's goalkeeper (-1 if none detected).
        /// Used by HoldShapePoolFilter to exclude GK (FR-DA-009 / KD-7).
        /// </summary>
        public int GkEntityId;

        /// <summary>
        /// Perceived position of the defensive team's goalkeeper (X, Y), in the canonical frame.
        /// Used by LastManDetector COVER_GK_ZONE trigger (§3.9). Set to Vector2.zero if GK absent.
        /// </summary>
        public Vector2 GkPosition;

        /// <summary>
        /// All agents for both teams. Length is at most DefensiveAIConstants.SQUAD_SIZE (22).
        /// </summary>
        public DefensiveAgentSnapshot[] Agents;

        /// <summary>Actual number of valid entries in <see cref="Agents"/>.</summary>
        public int AgentCount;

        /// <summary>
        /// True when the defensive team has an active PRIMARY_PRESS role this tick.
        /// Used by offside-trap trigger condition 4 (§3.7.2).
        /// </summary>
        public bool HasActivePrimaryPress;

        /// <summary>
        /// #21 T2 routing field (FR-TI-022): the team's manager OffsideTrap toggle, routed through
        /// <see cref="TacticTranslation.OffsideTrapRequested"/>. The class-field default is
        /// <c>false</c> = the <c>TeamTactic.Balanced</c> identity (FR-TI-031). Per KD-9 this is a
        /// <b>request, never a guarantee</b>: the §3.7.2 autonomous cascade in
        /// <see cref="OffsideTrapController"/> still adjudicates the four trigger conditions.
        /// <see cref="OffsideTrapController.Update"/> consumes it as an additive request (v1.2) — when
        /// <c>true</c> the trap arms after the reduced
        /// <see cref="DefensiveAIConstants.OffsideTrapRequestedDwellTicks"/>; <c>false</c> keeps the
        /// baseline dwell (behaviour-neutral).
        /// </summary>
        public bool OffsideTrapRequested;

        /// <summary>
        /// #21 T2 routing field (cheap-item addition, FR-TI-033): the team's manager
        /// <see cref="MarkingOrientation"/> dial, resolved through
        /// <see cref="TacticTranslation.MarkRadiusScalar"/> by <see cref="MarkAssigner"/> to scale the
        /// MAN_MARK candidate radius. <b>Constructor-seeded</b> to
        /// <see cref="TacticalInstructions.MarkingOrientation.Balanced"/> — the zero-value enum default
        /// is <c>BallOriented</c> (×0.80), NOT identity, so an un-set snapshot must not rely on the
        /// field's default value (same zero-value-trap class as #13's <c>PressingSnapshot.LineOfEngagement</c>).
        /// </summary>
        public MarkingOrientation MarkingOrientation;

        /// <summary>
        /// W13.1 (ERR-014-010) per-tick input: true when the composition root will CONSUME this tick's
        /// assignments as per-agent mark targets (§3.13.1) — i.e. the engine's mark-movement switch is on
        /// for this tick. It activates anti-chaos Invariant 4 (one marker per opponent, §3.10.2) in
        /// <see cref="InvariantEnforcer"/>; while <c>false</c> (the class-field default, and the only value
        /// the shipped engine writes) Invariant 4 does not run, so assignments and tackle intents are exactly
        /// the pre-W13.1 ones. A per-tick input like every other field here: set by the producer before
        /// <see cref="DefensiveAITick.Tick"/>, never read across ticks, NOT serialized.
        /// </summary>
        public bool MarkMovementActive;

        /// <summary>
        /// Constructs a snapshot with a pre-allocated agents array of the given capacity.
        /// </summary>
        public DefensiveSnapshot(int agentCapacity = DefensiveAIConstants.SQUAD_SIZE)
        {
            Agents     = new DefensiveAgentSnapshot[agentCapacity];
            AgentCount = 0;
            MarkingOrientation = TacticalInstructions.MarkingOrientation.Balanced;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-05-29 | —      | Initial implementation.                                          |
// | 1.1     | 2026-06-29 | —      | #21 T2: + OffsideTrapRequested routing field (FR-TI-022); false   |
// |         |            |        |   identity, arming-gate consumption deferred (KD-9, not neutral). |
// | 1.2     | 2026-06-29 | —      | Doc: Phase-D writer landed (MatchEngine v1.19); arming-gate       |
// |         |            |        |   consumption deferred to §3.7.2 additive-request (doc-only).     |
// | 1.3     | 2026-06-29 | —      | Doc: OffsideTrapController now consumes the request additively    |
// |         |            |        |   (reduced dwell when true; false = baseline = neutral).          |
// | 1.4     | 2026-07-07 | —      | Cheap-item addition: + MarkingOrientation routing field (FR-TI-033),|
// |         |            |        |   ctor-seeded Balanced (zero-value default is BallOriented).      |
// | 1.5     | 2026-11-10 | —      | ERR-014-008: documents the canonical-frame contract (own goal at x = 0 for |
// |         |            |        |   both teams; positions were mis-documented as world space) and adds the   |
// |         |            |        |   IsOwnGoalAtX0 frame accessor that replaces every team-id goal decision.  |
// | 1.6     | 2026-11-10 | —      | W13.0 review M1: IsOwnGoalAtX0 removed. #14 is canonical-only — own goal at x = 0 is a    |
// |         |            |        |   fixed frame invariant, not a per-producer switch; the unreachable x = 105 branches are  |
// |         |            |        |   gone from production code.                                                              |
// | 1.7     | 2026-11-10 | —      | W13.1 / ERR-014-010: + MarkMovementActive (per-tick input, default false, not serialized) — |
// |         |            |        |   activates Invariant 4 (one marker per opponent, §3.10.2).                                  |
#endregion
