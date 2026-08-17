// File: src/positioning-ai/PositioningAIConstants.cs
// Created:  2026-05-29
// Modified: 2026-07-28
// Author:   —
// Spec: #12 Positioning AI §6.1, Appendix B, new §3.5/§7.13 rest-defense coverage (cheap-item addition)
// Purpose: Single constant catalogue for Spec #12. All scalars, formation tables, and lookup arrays.
//          Per FR-PA-011 / KD-17 / #20 §4.2 FR-CS-025: NO second constant file may exist for this spec.

using UnityEngine;

namespace TacticalDirector.PositioningAI
{
    /// <summary>
    /// Complete constant catalogue for Positioning AI (Spec #12).
    /// Region order: Fixed → Derived → Cross → GT → Formation Tables → Lookup Arrays.
    /// Tags: [FIXED] [DERIVED] [CROSS] [GT]. No [EST] — all promoted to [GT] per Appendix A.
    /// </summary>
    public static class PositioningAIConstants
    {
        #region Fixed — physical / logical invariants

        /// <summary>[FIXED] Pitch length (goal-line to goal-line). #1 §1.2.</summary>
        public const float PITCH_LENGTH_M = 105.0f;

        /// <summary>[FIXED] Pitch width (touchline to touchline). #1 §1.2.</summary>
        public const float PITCH_WIDTH_M = 68.0f;

        /// <summary>[FIXED] Minimum allowable distance between any two agent formation slots. #3 collision radius.</summary>
        public const float MIN_AGENT_SEPARATION_M = 1.5f;

        /// <summary>[FIXED] Sentinel slot value for substituted / red-carded agents. Both components −∞. AR-S1-07, FR-PA-036.</summary>
        public static readonly Vector2 SENTINEL_NO_SLOT = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

        /// <summary>[FIXED] Squared distance epsilon for floating-point boundary comparisons. KD-16. Value = (0.01 m)².</summary>
        public const float SPACING_EPSILON_M2 = 1e-4f;

        /// <summary>
        /// [FIXED] Degenerate-distance guard for the dismark offset direction (F3): a marker closer
        /// than this to the agent skips the offset this tick (deterministic no-op). #23 §3.3/§3.5.
        /// </summary>
        public const float DISMARK_MIN_MARKER_DIST_EPS = 1e-3f;

        /// <summary>
        /// [FIXED] Hard cap on rotation adjacency-table rows per formation family — bounds the #25
        /// per-heartbeat evaluation cost. A cap, not a tunable. #25 §3.5 / FR-RO-016.
        /// </summary>
        public const int ROTATION_MAX_PAIRS_PER_FAMILY = 8;

        #endregion

        #region Derived — computed from other constants; never set independently

        /// <summary>[DERIVED] Precomputed MIN_AGENT_SEPARATION_M². Formula: MIN_AGENT_SEPARATION_M². §3.6.1. = 1.5² = 2.25 m².</summary>
        public const float MIN_AGENT_SEPARATION_M_SQ = MIN_AGENT_SEPARATION_M * MIN_AGENT_SEPARATION_M;

        /// <summary>[DERIVED] sqrt(SPACING_EPSILON_M2) = 0.01 m; used in displacement magnitude formula §3.6.3.</summary>
        public const float SPACING_EPSILON_M = 0.01f;

        /// <summary>[DERIVED] Pitch half-length used in basisX piecewise interpolation. = PITCH_LENGTH_M / 2.</summary>
        public const float PITCH_HALF_LENGTH_M = PITCH_LENGTH_M * 0.5f;

        /// <summary>[DERIVED] Pitch half-width used in basisY piecewise interpolation. = PITCH_WIDTH_M / 2.</summary>
        public const float PITCH_HALF_WIDTH_M = PITCH_WIDTH_M * 0.5f;

        /// <summary>[DERIVED] Lower clamp bound for slot X. = PITCH_TOUCHLINE_MARGIN_M.</summary>
        public const float SLOT_X_MIN = PITCH_TOUCHLINE_MARGIN_M;

        /// <summary>[DERIVED] Upper clamp bound for slot X. = PITCH_LENGTH_M − PITCH_TOUCHLINE_MARGIN_M.</summary>
        public const float SLOT_X_MAX = PITCH_LENGTH_M - PITCH_TOUCHLINE_MARGIN_M;

        /// <summary>[DERIVED] Lower clamp bound for slot Y. = PITCH_TOUCHLINE_MARGIN_M.</summary>
        public const float SLOT_Y_MIN = PITCH_TOUCHLINE_MARGIN_M;

        /// <summary>[DERIVED] Upper clamp bound for slot Y. = PITCH_WIDTH_M − PITCH_TOUCHLINE_MARGIN_M.</summary>
        public const float SLOT_Y_MAX = PITCH_WIDTH_M - PITCH_TOUCHLINE_MARGIN_M;

        /// <summary>
        /// [DERIVED] Rest-defense coverage line (m), own-goal-relative, in the team's canonical
        /// attack-toward-+X frame. Formula: PITCH_LENGTH_M * REST_DEFENSE_DEPTH_FRACTION. Cheap-item
        /// addition (§3.5, new §7.13 rest-defense coverage check).
        /// </summary>
        public const float REST_DEFENSE_DEPTH_M = PITCH_LENGTH_M * REST_DEFENSE_DEPTH_FRACTION;

        /// <summary>
        /// [DERIVED] Build-up zone boundary 1 (own third → middle third), team-relative X.
        /// Formula: PITCH_LENGTH_M / 3 = 35.0 m. #24 §3.1 (FM-BU-01).
        /// </summary>
        public const float BUILDUP_ZONE_THRESHOLD_1_M = PITCH_LENGTH_M / 3f;

        /// <summary>
        /// [DERIVED] Build-up zone boundary 2 (middle third → final third), team-relative X.
        /// Formula: 2 × PITCH_LENGTH_M / 3 = 70.0 m. #24 §3.1 (FM-BU-01).
        /// </summary>
        public const float BUILDUP_ZONE_THRESHOLD_2_M = 2f * PITCH_LENGTH_M / 3f;

        #endregion

        #region Cross — values defined in other approved specs; consumed read-only here

        /// <summary>[CROSS: #16 §3.4] RNG domain tag for Positioning AI. ERR-012-001 resolved 2026-05-18.</summary>
        public const byte DOMAIN_TAG_POSITIONING_AI = 0x17;

        #endregion

        #region GT — game-tuned; all [EST] promoted to [GT] per Appendix A, 2026-05-18

        // ── Hysteresis ──────────────────────────────────────────────────────────

        /// <summary>[GT] Anchor dwell window: 5 ticks = 500 ms. Appendix A.1.</summary>
        public const int ANCHOR_DWELL_TICKS = 5;

        /// <summary>[GT] Line boundary dead-zone half-width. Appendix A.2.</summary>
        public const float LINE_HYSTERESIS_M = 3.0f;

        /// <summary>[GT] Ticks the candidate line must be observed before committing. Appendix A.3.</summary>
        public const int LINE_DWELL_TICKS = 5;

        /// <summary>[GT] Lane boundary dead-zone half-width. Appendix A.4.</summary>
        public const float LANE_HYSTERESIS_M = 2.0f;

        /// <summary>[GT] Ticks the candidate phase must be observed before committing. Appendix A.5.</summary>
        public const int PHASE_HYSTERESIS_TICKS = 3;

        /// <summary>[GT] Ball longitudinal velocity threshold to distinguish loose-ball transitions. Appendix A.6.</summary>
        public const float PHASE_LOOSE_VELOCITY_THRESHOLD = 4.0f;

        // ── Ball-relative offset ────────────────────────────────────────────────

        /// <summary>[GT] Maximum longitudinal slot displacement from ball-relative offset. Appendix A.7.</summary>
        public const float OFFSET_RANGE_X_M = 12.0f;

        /// <summary>[GT] Maximum lateral slot displacement from ball-relative offset. Appendix A.8.</summary>
        public const float OFFSET_RANGE_Y_M = 8.0f;

        // ── Context modifiers ───────────────────────────────────────────────────

        /// <summary>[GT] Per-goal-lead increase in lateral compactness. §3.5.1.</summary>
        public const float SCORE_ATK_GAIN = 0.05f;

        /// <summary>[GT] Maximum lateral compactness relaxation when team is fully fatigued. §3.5.1, FR-PA-030.</summary>
        public const float FATIGUE_LATERAL_RELAX = 0.15f;

        /// <summary>[GT] Maximum vertical compactness increase at full tactical intensity. §3.5.1, FR-PA-031.</summary>
        public const float INTENSITY_VERTICAL_GAIN = 0.20f;

        // ── Spacing ─────────────────────────────────────────────────────────────

        /// <summary>[GT] Cost penalty applied when two agents share the same line+lane. §3.6.2, FR-PA-013. TODO: consumed at Stage 1 by soft-spacing penalty system.</summary>
        public const float SOFT_LANE_OVERLOAD_COST = 0.5f;

        /// <summary>[GT] Maximum spacing iteration passes per tick. §3.6.1.</summary>
        public const int SPACING_MAX_PASSES = 4;

        // ── Boundary ────────────────────────────────────────────────────────────

        /// <summary>[GT] Touchline margin for pitch-bound slot clamp. §3.7, FR-PA-033.</summary>
        public const float PITCH_TOUCHLINE_MARGIN_M = 0.5f;

        // ── Goalkeeper dedicated slot formula ───────────────────────────────────

        /// <summary>[GT] GK base depth from own goal-line at centre ball. Appendix A.9. #11 APPROVED 2026-05-18.</summary>
        public const float GK_DEPTH_M = 5.5f;

        /// <summary>[GT] GK longitudinal advance factor per unit basisX. Appendix A.10. #11 APPROVED 2026-05-18.</summary>
        public const float GK_ADVANCE_FACTOR = 8.0f;

        /// <summary>
        /// [GT] Lateral bound (m) of the GK slot off goal centre — the clamp on the ERR-012-010
        /// ball-line lateral term. 3.0 m sits inside the 3.66 m half-mouth so the slot never leads
        /// the keeper past a post. Replaces GK_LATERAL_FACTOR (retired, not retuned: no value of a
        /// pitch-width-anchored gain expresses ball-line tracking — gk-contact-rate-design.md
        /// KD-CR4). Appendix A.11. §3.3.3.
        /// </summary>
        public const float GK_LATERAL_CLAMP_M = 3.0f;

        // ── Stage 0 squad size ──────────────────────────────────────────────────

        /// <summary>[GT] Stage-0 squad size per team (1 GK + 10 outfield).</summary>
        public const int SQUAD_SIZE = 11;

        /// <summary>[GT] Ticks the candidate lane must be observed outside dead zone before committing. Separate from LINE_DWELL_TICKS for independent tuning.</summary>
        public const int LANE_DWELL_TICKS = 5;

        // ── Rest defense (cheap-item addition, new §3.5 / §7.13) ──────────────────

        /// <summary>
        /// [GT] Fraction of PITCH_LENGTH_M (own-goal-relative) counted as "sufficient defensive cover"
        /// while the team is in possession — an outfield agent at or behind this line contributes to
        /// rest-defense coverage. See <see cref="REST_DEFENSE_DEPTH_M"/>.
        /// </summary>
        public const float REST_DEFENSE_DEPTH_FRACTION = 0.40f;

        /// <summary>
        /// [GT] Minimum count of outfield agents at/behind REST_DEFENSE_DEPTH_M for the team's rest-
        /// defense coverage to be judged sufficient (RestDefenseEvaluator). Mirrors the "2+3"/"3+2"
        /// coverage structures observed in modern positional play.
        /// </summary>
        public const int REST_DEFENSE_MIN_COUNT = 3;

        // ── Phase-indexed compactness baselines (4 rows: InPoss/OutOfPoss/TransToAtk/TransToDef) ─

        /// <summary>
        /// [GT] Phase-indexed lateral compactness baseline.
        /// Higher value = wider shape relative to centroid.
        /// Index by (int)Phase. §3.5.1.
        /// </summary>
        public static readonly float[] BaseLateral = new float[4]
        {
            1.00f,  // InPoss     — standard width in possession
            0.88f,  // OutOfPoss  — narrower compact block when defending
            1.05f,  // TransToAtk — exploit width in transition attack
            0.82f,  // TransToDef — rapid lateral compression when transitioning to defend
        };

        /// <summary>
        /// [GT] Phase-indexed vertical compactness baseline.
        /// Higher value = deeper longitudinal spread relative to centroid.
        /// Index by (int)Phase. §3.5.1.
        /// </summary>
        public static readonly float[] BaseVertical = new float[4]
        {
            1.00f,  // InPoss     — standard depth in possession
            0.85f,  // OutOfPoss  — compact block depth when defending
            1.10f,  // TransToAtk — push lines up in attack transition
            0.75f,  // TransToDef — drop lines quickly when transitioning to defend
        };

        // ----- Dismarking AI (#23) — magnitudes illustrative pending the balance pass (#21 G2). -----

        /// <summary>[GT] Marking-radius (m): a perceived opponent inside this range is a marker candidate. #23 §3.1/§3.5.</summary>
        public const float MARKING_RADIUS_M = 3.0f;

        /// <summary>[GT] Heartbeats of sustained marking at which dwell01 saturates to 1. #23 §3.1/§3.5.</summary>
        public const int MARKING_DWELL_FULL_TICKS = 10;

        /// <summary>[GT] Dwell decay per unmarked (or out-of-phase) heartbeat. #23 §3.2/§3.5.</summary>
        public const int MARKING_DWELL_DECAY_PER_TICK = 2;

        /// <summary>[GT] MarkingPressure below/at this floor produces no dismark offset. #23 §3.3/§3.5 (FR-DM-006/009).</summary>
        public const float DISMARK_PRESSURE_FLOOR = 0.15f;

        /// <summary>[GT] Maximum dismark offset magnitude (m) at full pressure and full intensity. #23 §3.3/§3.5.</summary>
        public const float DISMARK_OFFSET_MAX_M = 2.5f;

        /// <summary>
        /// [GT] Dismark intensity scalar, indexed by (int)DismarkIntensity — Off/Conservative/Aggressive.
        /// Off row MUST stay exactly 0.0 (the FR-DM-012 identity). #23 §3.3/§3.5.
        /// </summary>
        public static readonly float[] DISMARK_INTENSITY_SCALAR = new float[3]
        {
            0.0f,   // Off          — identity (FR-DM-012)
            0.6f,   // Conservative
            1.0f,   // Aggressive
        };

        // ----- Build-Up Structures (#24) — magnitudes illustrative pending the balance pass. -----

        /// <summary>[GT] Zone-boundary hysteresis (m): the committed zone's interval expands by this at each boundary. #24 §3.1 (FM-BU-01, PASS-1 M-2).</summary>
        public const float BUILDUP_ZONE_HYSTERESIS_M = 2.0f;

        /// <summary>[GT] Componentwise bound on every overlay-catalogue offset (m); enforced by a catalogue-invariant test (FR-BU-008). #24 §3.2/§3.4.</summary>
        public const float BUILDUP_OFFSET_MAX_M = 8.0f;

        /// <summary>[GT] Post-regain suppression window (heartbeats) armed on a team-level regain when TransitionWon ∈ {CounterAttack, CounterPress}. #24 §3.3 (FM-BU-03).</summary>
        public const int REGAIN_SUPPRESS_TICKS = 30;

        // ----- Positional Rotations (#25) — magnitudes illustrative pending the balance pass. -----

        /// <summary>[GT] Base rotation-trigger advantage margin (m). #25 §3.1 (FM-RO-01).</summary>
        public const float ROTATION_ADVANTAGE_M = 4.0f;

        /// <summary>[GT] Advantage scalar at RotationFreedom.Conservative. Off is the dial gate, never arithmetic. #25 §3.1/§3.5.</summary>
        public const float ROTATION_ADVANTAGE_SCALAR_CONSERVATIVE = 1.5f;

        /// <summary>[GT] Advantage scalar at RotationFreedom.Free. #25 §3.1/§3.5.</summary>
        public const float ROTATION_ADVANTAGE_SCALAR_FREE = 1.0f;

        /// <summary>[GT] Consecutive heartbeats the (commit or revert) predicate must hold. #25 §3.2 (FM-RO-02).</summary>
        public const int ROTATION_TRIGGER_DWELL_TICKS = 5;

        /// <summary>
        /// [GT] Minimum heartbeats a committed rotation holds before revert becomes evaluable.
        /// Subject to the FR-RO-007 [DERIVED] lower bound ROTATION_HOLD_TICKS ≥ LINE_DWELL_TICKS
        /// (#25 Appendix D — hysteresis non-interference; locked by T-RO-U-013). #25 §3.2/§3.5.
        /// </summary>
        public const int ROTATION_HOLD_TICKS = 30;

        /// <summary>[GT] Maximum rotation commits per team per heartbeat. #25 §3.2/§3.5 (FR-RO-009).</summary>
        public const int ROTATION_MAX_PER_TICK = 1;

        #endregion

        #region Formation Tables — GT; per KD-17 / FR-PA-011 these live in this single file

        /// <summary>
        /// [GT] 4-4-2 Flat archetype. 11 slots: GK (index 0) + 10 outfield.
        /// lineCutIndices = (4, 8): Defense=4 / Midfield=4 / Attack=2.
        /// Appendix B.1.
        /// </summary>
        public static readonly FormationSlotRecord[] Family442 = new FormationSlotRecord[SQUAD_SIZE]
        {
            // slot 0 — GK
            new FormationSlotRecord(0.05f, 0.500f, RoleId.GK, LineId.Defense, LaneId.C,  isGoalkeeper: true),
            // slot 1 — LB
            new FormationSlotRecord(0.20f, 0.150f, RoleId.LB, LineId.Defense, LaneId.LH),
            // slot 2 — CB1
            new FormationSlotRecord(0.20f, 0.380f, RoleId.CB, LineId.Defense, LaneId.C),
            // slot 3 — CB2
            new FormationSlotRecord(0.20f, 0.620f, RoleId.CB, LineId.Defense, LaneId.C),
            // slot 4 — RB
            new FormationSlotRecord(0.20f, 0.850f, RoleId.RB, LineId.Defense, LaneId.RH),
            // slot 5 — LM
            new FormationSlotRecord(0.50f, 0.100f, RoleId.LM, LineId.Midfield, LaneId.LW),
            // slot 6 — CM1
            new FormationSlotRecord(0.50f, 0.380f, RoleId.CM, LineId.Midfield, LaneId.C),
            // slot 7 — CM2
            new FormationSlotRecord(0.50f, 0.620f, RoleId.CM, LineId.Midfield, LaneId.C),
            // slot 8 — RM
            new FormationSlotRecord(0.50f, 0.900f, RoleId.RM, LineId.Midfield, LaneId.RW),
            // slot 9 — ST1
            new FormationSlotRecord(0.78f, 0.420f, RoleId.ST, LineId.Attack, LaneId.C),
            // slot 10 — ST2
            new FormationSlotRecord(0.78f, 0.580f, RoleId.ST, LineId.Attack, LaneId.C),
        };

        /// <summary>lineCutIndices for 4-4-2: (firstMid=4, firstAtk=8). §3.3.1.</summary>
        public static readonly (int FirstMid, int FirstAtk) LineCuts442 = (4, 8);

        /// <summary>
        /// [GT] 4-3-3 archetype. 11 slots: GK (index 0) + 10 outfield.
        /// lineCutIndices = (4, 7): Defense=4 / Midfield=3 / Attack=3.
        /// Appendix B.2.
        /// </summary>
        public static readonly FormationSlotRecord[] Family433 = new FormationSlotRecord[SQUAD_SIZE]
        {
            // slot 0 — GK
            new FormationSlotRecord(0.05f, 0.500f, RoleId.GK, LineId.Defense, LaneId.C,  isGoalkeeper: true),
            // slot 1 — LB
            new FormationSlotRecord(0.22f, 0.150f, RoleId.LB, LineId.Defense, LaneId.LH),
            // slot 2 — CB1
            new FormationSlotRecord(0.22f, 0.380f, RoleId.CB, LineId.Defense, LaneId.C),
            // slot 3 — CB2
            new FormationSlotRecord(0.22f, 0.620f, RoleId.CB, LineId.Defense, LaneId.C),
            // slot 4 — RB
            new FormationSlotRecord(0.22f, 0.850f, RoleId.RB, LineId.Defense, LaneId.RH),
            // slot 5 — DM
            new FormationSlotRecord(0.42f, 0.500f, RoleId.DM, LineId.Midfield, LaneId.C),
            // slot 6 — CM1
            new FormationSlotRecord(0.55f, 0.350f, RoleId.CM, LineId.Midfield, LaneId.C),
            // slot 7 — CM2  (note: lineCuts firstAtk=7 so slot 7 is first Attack row)
            new FormationSlotRecord(0.55f, 0.650f, RoleId.CM, LineId.Midfield, LaneId.C),
            // slot 8 — LW
            new FormationSlotRecord(0.74f, 0.100f, RoleId.LW, LineId.Attack, LaneId.LW),
            // slot 9 — ST
            new FormationSlotRecord(0.80f, 0.500f, RoleId.ST, LineId.Attack, LaneId.C),
            // slot 10 — RW
            new FormationSlotRecord(0.74f, 0.900f, RoleId.RW, LineId.Attack, LaneId.RW),
        };

        /// <summary>lineCutIndices for 4-3-3: (firstMid=4, firstAtk=7). §3.3.1.</summary>
        public static readonly (int FirstMid, int FirstAtk) LineCuts433 = (4, 7);

        /// <summary>
        /// [GT] 4-2-3-1 archetype. 11 slots: GK (index 0) + 10 outfield.
        /// lineCutIndices = (4, 9): Defense=4 / Midfield=5 / Attack=1.
        /// AM slots 7/8/9 carry defaultLine=Midfield override per §3.3.1 note.
        /// Appendix B.3.
        /// </summary>
        public static readonly FormationSlotRecord[] Family4231 = new FormationSlotRecord[SQUAD_SIZE]
        {
            // slot 0 — GK
            new FormationSlotRecord(0.05f, 0.500f, RoleId.GK, LineId.Defense, LaneId.C,  isGoalkeeper: true),
            // slot 1 — LB
            new FormationSlotRecord(0.22f, 0.150f, RoleId.LB, LineId.Defense, LaneId.LH),
            // slot 2 — CB1
            new FormationSlotRecord(0.22f, 0.380f, RoleId.CB, LineId.Defense, LaneId.C),
            // slot 3 — CB2
            new FormationSlotRecord(0.22f, 0.620f, RoleId.CB, LineId.Defense, LaneId.C),
            // slot 4 — RB
            new FormationSlotRecord(0.22f, 0.850f, RoleId.RB, LineId.Defense, LaneId.RH),
            // slot 5 — DM1
            new FormationSlotRecord(0.40f, 0.380f, RoleId.DM, LineId.Midfield, LaneId.C),
            // slot 6 — DM2
            new FormationSlotRecord(0.40f, 0.620f, RoleId.DM, LineId.Midfield, LaneId.C),
            // slot 7 — LAM  (defaultLine=Midfield override per §3.3.1)
            new FormationSlotRecord(0.62f, 0.150f, RoleId.AM, LineId.Midfield, LaneId.LW),
            // slot 8 — CAM  (defaultLine=Midfield override per §3.3.1)
            new FormationSlotRecord(0.65f, 0.500f, RoleId.AM, LineId.Midfield, LaneId.C),
            // slot 9 — RAM  (defaultLine=Midfield override per §3.3.1)
            new FormationSlotRecord(0.62f, 0.850f, RoleId.AM, LineId.Midfield, LaneId.RW),
            // slot 10 — ST
            new FormationSlotRecord(0.82f, 0.500f, RoleId.ST, LineId.Attack, LaneId.C),
        };

        /// <summary>lineCutIndices for 4-2-3-1: (firstMid=4, firstAtk=9). §3.3.1.</summary>
        public static readonly (int FirstMid, int FirstAtk) LineCuts4231 = (4, 9);

        /// <summary>
        /// [DERIVED] Lane edge positions in metres along pitch width.
        /// 6 edges defining 5 equal bins: each bin = PITCH_WIDTH_M / 5 = 13.6 m.
        /// LW=[0,13.6), LH=[13.6,27.2), C=[27.2,40.8), RH=[40.8,54.4), RW=[54.4,68].
        /// §3.4.1, AR-S1-12.
        /// </summary>
        public static readonly float[] LaneEdgesM = new float[6]
        {
            0f,                          // LW lower bound
            PITCH_WIDTH_M * 1f / 5f,     // LH lower bound  = 13.6
            PITCH_WIDTH_M * 2f / 5f,     // C  lower bound  = 27.2
            PITCH_WIDTH_M * 3f / 5f,     // RH lower bound  = 40.8
            PITCH_WIDTH_M * 4f / 5f,     // RW lower bound  = 54.4
            PITCH_WIDTH_M,               // RW upper bound  = 68.0
        };

        #endregion

        #region Pull-Factor Lookup Array — GT; 13 roles × 4 phases = 52 Vector2 entries

        // Index formula: PullFactor[(int)role * 4 + (int)phase]
        // pullFactor.x = longitudinal pull strength [0,1]; direction encoded in basisX sign.
        // pullFactor.y = lateral pull strength [0,1]; direction encoded in basisY sign.
        //
        // Calibration anchor (verified against §3.2.2 worked example):
        //   PullFactor[AM * 4 + OutOfPoss] = (0.60, 0.10)  ← spec §3.2.2
        //
        // Row order: GK=0, LB=1, CB=2, RB=3, LM=4, CM=5, RM=6, DM=7,
        //            LW=8, CF=9, RW=10, AM=11, ST=12
        // Column order: InPoss=0, OutOfPoss=1, TransToAtk=2, TransToDef=3

        /// <summary>[GT] 13×4 pull-factor table. Access via GetPullFactor(role, phase). §3.2.1.</summary>
        public static readonly Vector2[] PullFactor = new Vector2[13 * 4]
        {
            // ── GK (0) ──────────────────────────────────────────────────────────
            new Vector2(0.00f, 0.00f),  // InPoss
            new Vector2(0.00f, 0.00f),  // OutOfPoss
            new Vector2(0.00f, 0.00f),  // TransToAtk
            new Vector2(0.00f, 0.00f),  // TransToDef

            // ── LB (1) ──────────────────────────────────────────────────────────
            new Vector2(0.30f, 0.35f),  // InPoss     — slight overlap
            new Vector2(0.40f, 0.25f),  // OutOfPoss  — track wide threat
            new Vector2(0.35f, 0.40f),  // TransToAtk — push wide
            new Vector2(0.45f, 0.25f),  // TransToDef — drop back, hold width

            // ── CB (2) ──────────────────────────────────────────────────────────
            new Vector2(0.10f, 0.10f),  // InPoss     — stays deep
            new Vector2(0.30f, 0.10f),  // OutOfPoss  — narrow tracking
            new Vector2(0.15f, 0.10f),  // TransToAtk — slight advance
            new Vector2(0.35f, 0.10f),  // TransToDef — drop to cover

            // ── RB (3) ──────────────────────────────────────────────────────────
            new Vector2(0.30f, 0.35f),  // InPoss
            new Vector2(0.40f, 0.25f),  // OutOfPoss
            new Vector2(0.35f, 0.40f),  // TransToAtk
            new Vector2(0.45f, 0.25f),  // TransToDef

            // ── LM (4) ──────────────────────────────────────────────────────────
            new Vector2(0.50f, 0.50f),  // InPoss
            new Vector2(0.55f, 0.40f),  // OutOfPoss
            new Vector2(0.55f, 0.55f),  // TransToAtk
            new Vector2(0.50f, 0.40f),  // TransToDef

            // ── CM (5) ──────────────────────────────────────────────────────────
            new Vector2(0.40f, 0.30f),  // InPoss
            new Vector2(0.45f, 0.30f),  // OutOfPoss
            new Vector2(0.50f, 0.35f),  // TransToAtk
            new Vector2(0.40f, 0.25f),  // TransToDef

            // ── RM (6) ──────────────────────────────────────────────────────────
            new Vector2(0.50f, 0.50f),  // InPoss
            new Vector2(0.55f, 0.40f),  // OutOfPoss
            new Vector2(0.55f, 0.55f),  // TransToAtk
            new Vector2(0.50f, 0.40f),  // TransToDef

            // ── DM (7) ──────────────────────────────────────────────────────────
            new Vector2(0.20f, 0.20f),  // InPoss     — screening role; stays
            new Vector2(0.45f, 0.30f),  // OutOfPoss  — presses ball side
            new Vector2(0.30f, 0.25f),  // TransToAtk — controlled advance
            new Vector2(0.55f, 0.35f),  // TransToDef — deepest retreat

            // ── LW (8) ──────────────────────────────────────────────────────────
            new Vector2(0.65f, 0.60f),  // InPoss
            new Vector2(0.60f, 0.45f),  // OutOfPoss
            new Vector2(0.70f, 0.65f),  // TransToAtk
            new Vector2(0.50f, 0.45f),  // TransToDef

            // ── CF (9) ──────────────────────────────────────────────────────────
            new Vector2(0.55f, 0.40f),  // InPoss
            new Vector2(0.50f, 0.30f),  // OutOfPoss  — high press
            new Vector2(0.65f, 0.45f),  // TransToAtk
            new Vector2(0.45f, 0.30f),  // TransToDef

            // ── RW (10) ─────────────────────────────────────────────────────────
            new Vector2(0.65f, 0.60f),  // InPoss
            new Vector2(0.60f, 0.45f),  // OutOfPoss
            new Vector2(0.70f, 0.65f),  // TransToAtk
            new Vector2(0.50f, 0.45f),  // TransToDef

            // ── AM (11) ─────────────────────────────────────────────────────────
            new Vector2(0.50f, 0.40f),  // InPoss
            new Vector2(0.60f, 0.10f),  // OutOfPoss  ← anchored to §3.2.2 worked example
            new Vector2(0.60f, 0.45f),  // TransToAtk
            new Vector2(0.50f, 0.35f),  // TransToDef

            // ── ST (12) ─────────────────────────────────────────────────────────
            new Vector2(0.60f, 0.45f),  // InPoss
            new Vector2(0.65f, 0.35f),  // OutOfPoss  — hold high / press
            new Vector2(0.75f, 0.50f),  // TransToAtk — spearhead run
            new Vector2(0.50f, 0.35f),  // TransToDef
        };

        /// <summary>Returns the pull factor for the given role and phase. Inlined for hot-path use.</summary>
        public static Vector2 GetPullFactor(RoleId role, Phase phase)
            => PullFactor[(int)role * 4 + (int)phase];

        /// <summary>Returns the formation slot records for the given family (null-safe fallback to F442).</summary>
        public static FormationSlotRecord[] GetFormationSlots(FormationFamily family)
        {
            return family switch
            {
                FormationFamily.F433  => Family433,
                FormationFamily.F4231 => Family4231,
                _                     => Family442,
            };
        }

        /// <summary>Returns lineCutIndices (firstMid, firstAtk) for the given family.</summary>
        public static (int FirstMid, int FirstAtk) GetLineCuts(FormationFamily family)
        {
            return family switch
            {
                FormationFamily.F433  => LineCuts433,
                FormationFamily.F4231 => LineCuts4231,
                _                     => LineCuts442,
            };
        }

        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
// | 1.1     | 2026-07-07 | —      | Cheap-item addition: + REST_DEFENSE_DEPTH_FRACTION / _MIN_COUNT   |
// |         |            |        |   [GT] + REST_DEFENSE_DEPTH_M [DERIVED] (§3.5/§7.13).              |
// | 1.2     | 2026-07-10 | —      | #23/#24/#25 T0 constants (FR-DM-016/FR-BU-014/FR-RO-016):         |
// |         |            |        |   Fixed: DISMARK_MIN_MARKER_DIST_EPS, ROTATION_MAX_PAIRS_PER_FAMILY|
// |         |            |        |   Derived: BUILDUP_ZONE_THRESHOLD_1_M/_2_M;                       |
// |         |            |        |   GT: dismark radius/dwell/floor/offset/scalar table, build-up    |
// |         |            |        |   hysteresis/offset-bound/suppress window, rotation advantage/    |
// |         |            |        |   scalars/dwell/hold/per-tick cap. Magnitudes illustrative        |
// |         |            |        |   pending each spec's balance pass (#21 G2 precedent).            |
// | 1.3     | 2026-07-28 | —      | ERR-012-010: GK_LATERAL_FACTOR retired -> GK_LATERAL_CLAMP_M,     |
// |         |            |        |   the ball-line lateral clamp.                                     |
#endregion
