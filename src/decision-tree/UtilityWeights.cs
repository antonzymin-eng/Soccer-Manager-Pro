// File:     src/decision-tree/UtilityWeights.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Modified: 2026-07-28 (ERR-008-016 — + POWER_INTENT_FLOOR [GT] (shot-speed design KD-1))
// Modified: 2026-07-28 (ERR-008-017 — + SHOOT_SWEET_RANGE_M / SHOOT_DIST_FALLOFF_M [GT] (shot-volume design KD-V2))
// Modified: 2026-08-04 (ERR-008-018 — + DRIBBLE_GOAL_DIR_MIN_MODIFIER [GT] (close-chance-creation design KD-CC2))
// Modified: 2026-08-04 (ERR-008-020 — pass-lane threat model: PASS_LANE_WIDTH_HALF → CORE_HALF_WIDTH/FALLOFF_END + INTERCEPTOR_ABILITY_MIN/MAX + LANE_VISION_FIDELITY_FLOOR)
// Modified: 2026-08-05 (ERR-008-019 — + LONG_SHOT_RAMP_HALF_WIDTH [GT]; LONG_SHOT_THRESHOLD redocumented as the ramp centre)
// Modified: 2026-08-05 (ERR-008-019 owner revision — LONG_SHOT_RAMP_HALF_WIDTH 0.05 → 0.25: full-range ramp, no plateaus)
// Modified: 2026-08-05 (ERR-008-019 AR — LONG_SHOT_RAMP_HALF_WIDTH XML doc: the (0, 0.25] range is the formula's validity domain; the suite pins 0.25)
// Modified: 2026-08-06 (ERR-008-021 AR-1 M-1 — doc only: INTERCEPTOR_ABILITY_MIN/MAX + LANE_VISION_FIDELITY_FLOOR now name their second consumer, the §3.2.3.2 step-3a shot-lane occlusion)
// Author:   —
// Spec:     Decision Tree #8 §3.2.11, Code Standards #20
// Purpose:  Authoritative constant catalogue for the utility scoring model.
//           All 58 utility constants (§3.2.11) reside here and nowhere else.
//           No other file may define utility scoring constants (§3.2.1.6).

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// All gameplay-tunable and estimated constants for the Decision Tree utility model.
    /// No constant in this file may be referenced inline in code — access via this class.
    /// Decision Tree #8 §3.2.11.
    /// </summary>
    public static class UtilityWeights
    {
        // ── Universal ──────────────────────────────────────────────────────────────

        /// <summary>[DERIVED] Minimum scored utility after clamping. Guarantees every option has positive appeal. Decision Tree #8 §3.2.1.5.</summary>
        public const float UTILITY_FLOOR = 0.01f;

        /// <summary>[DERIVED] Maximum scored utility after clamping. Perfect conditions cap. Decision Tree #8 §3.2.1.5.</summary>
        public const float UTILITY_CEILING = 1.00f;

        // ── Zone Modifiers ─────────────────────────────────────────────────────────

        public const float PASS_ZONE_DEF = 1.05f;  // [GT] pass urgency in own third
        public const float PASS_ZONE_MID = 1.00f;  // [GT] neutral baseline
        public const float PASS_ZONE_ATT = 0.90f;  // [GT] passing less dominant in attack

        public const float SHOOT_ZONE_ATT = 1.00f;  // [GT] full baseline in attacking third
        public const float SHOOT_ZONE_MID_LONG = 0.55f;  // [GT] long shot midfield modifier
        public const float SHOOT_ZONE_MID_SHORT = 0.05f; // [GT] near-suppression for no long shot
        public const float SHOOT_ZONE_DEF = 0.10f;  // [GT] strong discouragement from own half

        public const float DRIBBLE_ZONE_DEF = 0.70f; // [GT] dangerous in own third
        public const float DRIBBLE_ZONE_MID = 1.00f; // [GT] neutral
        public const float DRIBBLE_ZONE_ATT = 1.10f; // [GT] mildly encouraged in attack

        public const float HOLD_ZONE_DEF = 1.25f;  // [GT] safe in own third
        public const float HOLD_ZONE_MID = 1.00f;  // [GT] neutral
        public const float HOLD_ZONE_ATT = 0.80f;  // [GT] waste of attacking opportunity

        public const float MOVE_ZONE_DEF = 1.00f;  // [GT] positional duty unchanged by zone (§3.2.1.3)
        public const float MOVE_ZONE_MID = 1.00f;  // [GT]
        public const float MOVE_ZONE_ATT = 1.00f;  // [GT]

        public const float PRESS_ZONE_DEF = 0.80f;  // [GT] pressing from deep exposes space
        public const float PRESS_ZONE_MID = 1.00f;  // [GT] neutral
        public const float PRESS_ZONE_ATT = 1.20f;  // [GT] high press encouraged in attack

        public const float INTERCEPT_ZONE_DEF = 1.10f; // [GT] most valuable defensively
        public const float INTERCEPT_ZONE_MID = 1.00f; // [GT] neutral
        public const float INTERCEPT_ZONE_ATT = 0.90f; // [GT] in attack, seek creation not chase

        // ── Base Utility Nominals ───────────────────────────────────────────────────

        public const float U_BASE_PASS = 0.60f;  // [GT] primary positive action
        public const float U_BASE_SHOOT = 0.85f;  // [GT] highest ceiling; zone-gated
        public const float U_BASE_DRIBBLE = 0.45f;  // [GT] creative outlet; deliberately secondary
        public const float U_BASE_HOLD = 0.28f;  // [GT] fallback; must be lowest baseline
        public const float U_BASE_MOVE = 0.40f;  // [GT] positional duty; moderate urgency
        public const float U_BASE_PRESS = 0.50f;  // [GT] active defence; moderate baseline
        public const float U_BASE_INTERCEPT = 0.55f;  // [GT] best active defensive action
        // [GT] SAVE base utility (ERR-008-013). NOT load-bearing for selection — SAVE is the SOLE
        // off-ball option when TacticalContext.SaveAvailable (OptionGenerator), so it is always
        // selected regardless of this value; it only feeds AgentAction.UtilityScore / DecisionMadeEvent.
        public const float U_BASE_SAVE = 1.00f;

        // ── Attribute Exponents ─────────────────────────────────────────────────────

        public const float PASS_VISION_EXP = 0.30f;  // [GT] lane quality reading
        public const float PASS_TECHNIQUE_EXP = 0.40f;  // [GT] passing execution accuracy
        public const float SHOOT_FINISHING_EXP = 0.50f;  // [GT] shot execution; steeper curve
        public const float SHOOT_COMPOSURE_EXP = 0.30f;  // [EST] discrete-event composure; Beilock & Carr (2001)
        public const float DRIBBLE_DRIBBLING_EXP = 0.40f; // [GT] core dribbling skill
        public const float DRIBBLE_AGILITY_EXP = 0.30f;  // [GT] directional change speed
        public const float HOLD_COMPOSURE_EXP = 0.50f;  // [GT] sustained-state composure gate
        public const float MOVE_POSITIONING_EXP = 0.40f;  // [GT] positional commitment
        public const float MOVE_WORKRATE_EXP = 0.30f;  // [GT] running effort
        public const float PRESS_AGGRESSION_EXP = 0.30f;  // [GT] pressing intent
        public const float PRESS_WORKRATE_EXP = 0.30f;  // [GT] pressing engine
        public const float PRESS_STAMINA_EXP = 0.20f;  // [GT] capacity to press
        public const float INTERCEPT_ANTICIPATION_EXP = 0.50f; // [EST] interceptive timing; Müller & Abernethy (2006)
        public const float INTERCEPT_PACE_EXP = 0.30f;  // [GT] speed to intercept point

        // ── Risk Penalty Coefficients ───────────────────────────────────────────────

        public const float PASS_RISK_COEFF = 0.30f;  // [GT] passing risk under pressure
        public const float SHOOT_RISK_COEFF = 0.40f;  // [GT] shot blocked = possession lost
        public const float DRIBBLE_RISK_COEFF = 0.35f;  // [GT] dribble tackle = possession lost
        public const float INTERCEPT_PRESSURE_COEFF = 0.20f; // [GT] pressure reduces intercept read
        public const float HOLD_PRESSURE_COEFF = 0.50f;  // [GT] pressure reduces HOLD appeal

        // ── Context Score Thresholds and Distances ──────────────────────────────────

        /// <summary>
        /// [GT] Midfield long-shot ramp CENTRE, in the SHIFTED attribute form
        /// (0.5 + A_LongShots × 0.5) per §3.2.3.1 (the AR-2 M-4 correction — the raw
        /// form required raw ≥ 16). ERR-008-019: no longer a hard gate. The midfield
        /// zone modifier ramps linearly from SHOOT_ZONE_MID_SHORT at
        /// (THRESHOLD − LONG_SHOT_RAMP_HALF_WIDTH) to SHOOT_ZONE_MID_LONG at
        /// (THRESHOLD + LONG_SHOT_RAMP_HALF_WIDTH); at exactly THRESHOLD the modifier
        /// is the exact SHORT/LONG midpoint (§3.2.3.4 derives the raw-attribute bands).
        /// </summary>
        public const float LONG_SHOT_THRESHOLD = 0.75f;

        /// <summary>
        /// [GT] Midfield long-shot ramp half-width, in shifted-attribute units.
        /// 0.25 is the FULL-RANGE setting (owner-directed, August 5, 2026 — supersedes
        /// the initial 0.05 landing value): the ramp spans the entire shifted domain
        /// [0.5, 1.0], so every raw LongShots point from 1 to 20 moves the zone
        /// modifier by ≈ 0.026 — no plateau at either end; raw 1 is exactly
        /// SHOOT_ZONE_MID_SHORT and raw 20 exactly SHOOT_ZONE_MID_LONG. Must be > 0
        /// and ≤ 0.25 (the ramp must stay inside the shifted form's [0.5, 1.0] range).
        /// That range is the FORMULA's validity domain, not a free dial: the test suite
        /// pins the full-range value through
        /// UtilityScorerTests.ShootMidfield_FullRangeRamp_EndpointsExact_AndStrictlyMonotone,
        /// which fails at any half-width below 0.25 because the end plateaus return —
        /// the lock deliberately encodes the owner's no-plateau instruction. A retune
        /// below 0.25 is therefore an owner decision that must revisit that lock in the
        /// same change.
        /// Centred on LONG_SHOT_THRESHOLD = the attribute midpoint, so the
        /// population-mean modifier over a uniform attribute is 0.30 at ANY symmetric
        /// half-width — the doctrine P5 pivot holds at this value too. §3.2.3.1,
        /// ERR-008-019.
        /// </summary>
        public const float LONG_SHOT_RAMP_HALF_WIDTH = 0.25f;
        public const float GOAL_OPENING_MIN = 0.05f;  // [GT] minimum goal opening score floor
        public const float BLOCKER_RADIUS_M = 0.50f;  // [GT] outfield player body width in shot lane
        public const float GK_BLOCKER_RADIUS_M = 1.50f;  // [GT] goalkeeper effective blocking radius
        public const float GK_PROXIMITY_TO_GOAL = 6.00f;  // [GT] distance from goal line to classify as GK
        public const float GOAL_MIN_SHOT_DIST = 1.00f;  // [GT] minimum dist to count as blocker

        public const float MOVE_URGENCY_DIST_M = 15.0f;  // [GT] full urgency distance for MOVE
        public const float MOVE_DIST_MIN = 0.10f;  // [GT] minimum distance modifier floor
        public const float MOVE_PRESS_SUPPRESSION_DIST = 6.0f;  // [GT] proximity threshold for MOVE suppression
        public const float MOVE_PRESS_SUPPRESSION_FACTOR = 0.60f; // [GT] multiplier applied to MOVE when opponent is close

        // ── Phase Modifiers ─────────────────────────────────────────────────────────

        public const float MOVE_PHASE_OWN_TEAM = 0.70f;  // [GT] delay repositioning in possession
        public const float MOVE_PHASE_OPPONENT = 1.25f;  // [GT] urgent to recover shape without ball
        public const float MOVE_PHASE_CONTESTED = 1.00f;  // [DERIVED] neutral baseline

        // ── Tactical Pressing Modifiers ─────────────────────────────────────────────
        // AR-2 L: the tactical pressing multipliers live EXCLUSIVELY in
        // TacticalWeights.cs (§3.4.7 "All tactical constants reside exclusively in
        // TacticalWeights.cs"): PressingHighPressMod / PressingLowPressMod et al.
        // The PRESS_TACTICAL_HIGH/MEDIUM/LOW duplicates previously declared here were
        // never consumed and were a parallel-surface drift hazard (a designer tuning
        // one copy would silently miss the live one) — removed.

        // ── Option Generation Constants ─────────────────────────────────────────────
        // Constants referenced in §3.1; catalogued here per §3.2.1.6.

        // ── Pass-lane threat model (§3.1.3.3, ERR-008-020 / judgment-proxy doctrine P1+P2) ──
        // The former single PASS_LANE_WIDTH_HALF = 0.8 m corridor counted every opponent
        // inside it as exactly 1 interceptor and every opponent outside it as 0 — a 2 cm
        // positional cliff, blind to who the defender is. Replaced by a continuous per-
        // opponent threat weight: full weight inside the core corridor, linear falloff to
        // zero at the outer edge, scaled by the defender's perceived interception ability.
        // The ramp is centred on the old 0.8 m cliff (0.4 core + 0.8 m fade ⇒ the same
        // integrated threat over a uniform defender position), so an average defender at
        // an average position costs the lane what it cost before (doctrine P5 pivot).

        /// <summary>[GT] Core corridor half-width (m): an opponent within this perpendicular distance of the pass line carries full positional threat. §3.1.3.3, ERR-008-020.</summary>
        public const float PASS_LANE_CORE_HALF_WIDTH = 0.4f;

        /// <summary>[GT] Outer threat edge (m): positional threat fades linearly from 1.0 at PASS_LANE_CORE_HALF_WIDTH to 0.0 here. Must exceed PASS_LANE_CORE_HALF_WIDTH. §3.1.3.3, ERR-008-020.</summary>
        public const float PASS_LANE_FALLOFF_END = 1.2f;

        /// <summary>[GT] Interception/blocking-ability scalar at Anticipation+Pace mean = 0 (raw 1/1). Consumed by BOTH the pass lane (§3.1.3.3, ERR-008-020) and the shot-lane occlusion (§3.2.3.2 step 3a, ERR-008-021) — one calibration lever moves both lanes (KD-W1).</summary>
        public const float INTERCEPTOR_ABILITY_MIN = 0.6f;

        /// <summary>[GT] Interception/blocking-ability scalar at Anticipation+Pace mean = 1 (raw 20/20). Midpoint of MIN..MAX is exactly 1.0 so the ability-midpoint defender is weight-neutral. Consumed by BOTH the pass lane (§3.1.3.3, ERR-008-020) and the shot-lane occlusion (§3.2.3.2 step 3a, ERR-008-021).</summary>
        public const float INTERCEPTOR_ABILITY_MAX = 1.4f;

        /// <summary>[GT] Vision-fidelity floor: at Vision raw 1 the reading agent (the passer in §3.1.3.3, the shooter in §3.1.4.3) resolves this fraction of an opponent's true ability deviation from average (doctrine P2 — low Vision degrades to the attribute-blind read, it never invents information). ERR-008-020; also the shot lane, ERR-008-021.</summary>
        public const float LANE_VISION_FIDELITY_FLOOR = 0.2f;

        public const float PASS_LANE_DIVISOR = 3.0f;  // [GT] summed lane threat → score=0
        public const float MIN_PASS_LANE_SCORE = 0.05f; // [GT] adjusted lane score floor
        public const float GOAL_DIR_MIN_MODIFIER = 0.5f;  // [GT] backward-pass direction penalty floor

        public const float SHORT_PASS_MAX_DISTANCE = 15.0f; // [GT] m; §3.1.3.4
        public const float MEDIUM_PASS_MAX_DISTANCE = 30.0f; // [GT] m; §3.1.3.4
        public const float CROSS_ANGLE_THRESHOLD = 60.0f; // [GT] degrees; §3.1.3.4
        public const float THROUGH_BALL_VEL_THRESHOLD = 1.0f; // [GT] m/s; §3.1.3.4

        // [GT] Minimum goal visibility for SHOOT. Sits ABOVE GOAL_OPENING_MIN (the §3.2.3.2 step-5
        // floor) by design: at the former 0.05 the two were equal, so the §3.1.4.1 gate could only
        // fire on the degenerate zero-arc early return and a fully walled-off shot was generated,
        // scored and taken (§5.Z.17 §7.4). At 0.12 a shooter whose goal arc is ≥ ~88% occluded
        // holds / passes / dribbles instead — shot-outcome design KD-7.
        public const float MIN_GOAL_VISIBILITY = 0.12f;
        public const float BASE_SHOOT_RANGE = 20.0f; // [GT] m; §3.1.4.2
        public const float LONGSHOT_RANGE_BONUS = 15.0f; // [GT] m; §3.1.4.2

        // [GT] PowerIntent floor for SHOOT (§3.5.3, ERR-008-016 / shot-speed design KD-1). The
        // former clamp(goalOpening × A_Finishing, 0.1, 1.0) is a product of two [0,1] factors —
        // A_Finishing ≈ 0.47 for a neutral 10 and goalOpening typically 0.2–0.6 — so nearly every
        // shot pinned at the 0.1 clamp floor and left the boot at 10–30% power (measured shot-tick
        // means 7–10 m/s vs football's ~25). A deliberate shot is always struck hard; opening ×
        // finishing modulates the TOP band above this floor, preserving the old formula's
        // direction (a better opening and a better finisher still strike harder, up to 1.0).
        public const float POWER_INTENT_FLOOR = 0.65f;

        // [GT] SHOOT distance-quality knee + falloff (§3.2.3.1 DistanceQuality_SHOOT,
        // ERR-008-017 / shot-volume design KD-V2). U_SHOOT previously had NO distance term while
        // the range gate is a cliff at 20 + A_LongShots × 15 m — within range a 34 m shot scored
        // identically to a 10 m one, and measured shots clustered AT the range boundary (means
        // 30–34 m over full matches vs football's ~17; ~60% of shots beyond 22 m). distQ(d) = 1
        // for d ≤ SWEET (every close-range utility byte-identical to pre-fix), then
        // FALLOFF / (FALLOFF + (d − SWEET)) — continuous at the knee, bounded (0,1], monotone.
        // Football's P(goal|shot) falls ~10× from 11 m to 30 m; this is the utility-side shape
        // of that fact, calibrated against the measured shots/match + mean-distance bands
        // (shot-volume design §6).
        public const float SHOOT_SWEET_RANGE_M = 12.0f;
        public const float SHOOT_DIST_FALLOFF_M = 8.0f;

        public const float MIN_DRIBBLE_SPACE = 0.10f; // [GT] minimum space score to generate DRIBBLE
        public const float DRIBBLE_THREAT_RADIUS = 2.0f;  // [GT] m; opponent proximity for space scoring
        public const float DRIBBLE_LOOKAHEAD_M = 5.0f;  // [GT] m; look-ahead target distance

        // [GT] DRIBBLE directional-to-goal modifier floor (§3.2.4.1 DirectionQuality_DRIBBLE,
        // ERR-008-018 / close-chance-creation design KD-CC2). §3.1.5.2 chooses best_direction by
        // FREE SPACE alone and closes with "No backward-sector penalty is applied to SpaceScore at
        // generation time; the scoring stage applies directional-to-goal modifiers to the DRIBBLE
        // utility" — but §3.2.4.1's formula has no such factor, and its cross-reference points at
        // §3.2.2 (the PASS section), which is why the promised term never had a home. Measured in
        // the final third over six full matches, DRIBBLE was the modal carrier action at 40% of
        // decisions with a mean cosine to the goal of −0.30: the average dribble in the attacking
        // third pointed AWAY from the goal, and the utility was identical either way. Same shape as
        // the PASS GOAL_DIR_MIN_MODIFIER above — a directly-away dribble keeps this fraction of its
        // utility, a directly-goalward one keeps all of it, linear in the cosine between.
        //
        // 0.80 is DELIBERATELY WEAKER than the 0.50 PASS floor, and the asymmetry is measured, not
        // an oversight. Suppressing the dribble pushes the carrier onto HOLD (share 20% → 23% here,
        // → 31% at floor 0.50), and HOLD has no timeout: a carrier with no pass, no shot and no
        // dribble can hold indefinitely. At floors 0.50 and 0.65 one seed in six developed exactly
        // that stall — mean final-third episode length 5.1 s → 28.6 s and 17.5 s respectively —
        // while every seed at 0.80 stayed in the 4.5–5.6 s band. The floor cannot go lower until
        // the HOLD stall is fixed; see close-chance-creation-design.md §7 item 2 and §8.
        public const float DRIBBLE_GOAL_DIR_MIN_MODIFIER = 0.8f;

        public const float PRESS_TRIGGER_DISTANCE = 8.0f;  // [GT] m; maximum distance for PRESS generation
        public const float PRESS_STAMINA_MINIMUM = 0.20f; // [GT] AerobicPool threshold for PRESS gate

        public const float INTERCEPT_MIN_BALL_SPEED = 1.0f; // [GT] m/s; ball must be moving for INTERCEPT
        public const float MAX_INTERCEPT_TIME = 1.5f;  // [GT] s; maximum look-ahead for intercept geometry
        /// <summary>
        /// [EST] First-order drag decay coefficient (s⁻¹) for the §3.1.9.2 intercept
        /// trajectory approximation: v(t) = v₀·e^(−kt). Retagged from [CROSS] (AR-2 L /
        /// ERR-008-009): Ball Physics #1 models QUADRATIC drag (½ρC_dAv²) and declares
        /// no 0.3 s⁻¹ constant — this value is a DT-side approximation calibrated
        /// against #1's worked examples (~26% speed loss over 1 s), not a verbatim
        /// copy, so [CROSS] violated the tag rules. Error bounds: §3.1.9.2 (≤6.9% at
        /// 30 m/s over 1.5 s). Stage 1: replace with BallPhysics.ProjectPosition(t).
        /// </summary>
        public const float DRAG_APPROX = 0.3f;

        // URGENCY_PRESSURE_SCALE: lives in TacticalWeights.UrgencyPressureScale (the
        // consumed surface; §3.4.7). The unconsumed duplicate previously declared
        // here is removed (AR-2 L parallel-surface drift hazard).

        // ── Option Generation: Derived Geometry ─────────────────────────────────
        // Constants used in OptionGenerator formula code per §3.2.1.6.

        /// <summary>[DERIVED] Half-angle of each 8-sector DRIBBLE space scan sector. 360°/8/2 = 22.5°.</summary>
        public const float DRIBBLE_SECTOR_HALF_ANGLE = 22.5f;

        /// <summary>[GT] Fraction of lane endpoint excluded from interceptor projection (prevents endpoint artifacts).</summary>
        public const float PASS_LANE_ENDPOINT_MARGIN = 0.05f;

        // ── Agent Speed Cross-Reference ──────────────────────────────────────────
        // [CROSS — Agent Movement #2 §3.2.4]

        /// <summary>[CROSS — Agent Movement #2 §3.2.4] Minimum agent speed at Pace=1 (m/s).</summary>
        public const float AGENT_SPEED_MIN_MPS = 7.5f;

        /// <summary>[CROSS — Agent Movement #2 §3.2.4] Maximum agent speed at Pace=20 (m/s).</summary>
        public const float AGENT_SPEED_MAX_MPS = 10.2f;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                           |
// | 1.0     | 2026-05-29 | —      | Initial implementation.                                                         |
// | 1.1     | 2026-05-29 | —      | AR-1 L-1/L-3: Add DRIBBLE_SECTOR_HALF_ANGLE, PASS_LANE_ENDPOINT_MARGIN,        |
// |         |            |        |   AGENT_SPEED_MIN/MAX_MPS; XML docs on UTILITY_FLOOR/CEILING.                  |
// | 1.2     | 2026-06-11 | —      | Audit AR-2: L removed unconsumed duplicates of TacticalWeights constants       |
// |         |            |        |   (PRESS_TACTICAL_HIGH/MEDIUM/LOW, URGENCY_PRESSURE_SCALE — parallel-surface   |
// |         |            |        |   drift hazard); DRAG_APPROX retagged [CROSS]→[EST] with derivation note       |
// |         |            |        |   (ERR-008-009); LONG_SHOT_THRESHOLD doc states the shifted-form comparison    |
// |         |            |        |   (M-4 companion); MOVE_ZONE_* gain [GT] tags (now consumed via               |
// |         |            |        |   GetZoneModifier).                                                            |
// | 1.3     | 2026-07-23 | —      | ERR-008-013: + U_BASE_SAVE (= UTILITY_CEILING). Not load-bearing for          |
// |         |            |        |   selection (SAVE is the sole off-ball option when available); feeds only      |
// |         |            |        |   AgentAction.UtilityScore / DecisionMadeEvent.                                |
// | 1.4     | 2026-07-28 | —      | ERR-008-016 (shot-speed design KD-1): + POWER_INTENT_FLOOR [GT] = 0.65 —    |
// |         |            |        | the §3.5.3 floor a deliberate shot is always struck at; opening ×           |
// |         |            |        | finishing modulates the band above it.                                      |
// | 1.5     | 2026-07-28 | —      | ERR-008-017 (shot-volume design KD-V2): + SHOOT_SWEET_RANGE_M [GT] = 12 +   |
// |         |            |        | SHOOT_DIST_FALLOFF_M [GT] = 10 — the DistanceQuality_SHOOT knee + falloff.  |
// | 1.6     | 2026-08-04 | —      | ERR-008-018 (close-chance-creation design KD-CC2): + DRIBBLE_GOAL_DIR_MIN_ |
// |         |            |        | MODIFIER [GT] = 0.80 — the §3.2.4.1 DirectionQuality_DRIBBLE floor. The    |
// |         |            |        | value is deliberately WEAKER than the 0.50 PASS floor: suppressing the     |
// |         |            |        | dribble pushes the carrier onto the timeout-free HOLD, and at floors 0.65  |
// |         |            |        | and 0.50 one seed in six stalled (final-third episode 5.1 s -> 17.5/28.6). |
// | 1.7     | 2026-08-04 | —      | ERR-008-020 (judgment-proxy doctrine §6.4): pass-lane threat model.        |
// |         |            |        | PASS_LANE_WIDTH_HALF (0.8 m binary corridor) removed; + PASS_LANE_CORE_    |
// |         |            |        | HALF_WIDTH [GT] = 0.4 + PASS_LANE_FALLOFF_END [GT] = 1.2 (ramp centred on  |
// |         |            |        | the old cliff — integrated threat preserved) + INTERCEPTOR_ABILITY_MIN/    |
// |         |            |        | MAX [GT] = 0.6/1.4 (Anticipation+Pace) + LANE_VISION_FIDELITY_FLOOR [GT]   |
// |         |            |        | = 0.2 (doctrine P2 — Vision resolves ability deviation from average).      |
// | 1.8     | 2026-08-05 | —      | ERR-008-019 (judgment-proxy doctrine P1/P5): + LONG_SHOT_RAMP_HALF_WIDTH   |
// |         |            |        | [GT] = 0.05 (shifted units; ramp spans raw ≈ 8.6–12.4, centred on the old  |
// |         |            |        | cliff so the integrated modifier is preserved — P5 pivot).                 |
// |         |            |        | LONG_SHOT_THRESHOLD redocumented as the ramp centre; value unchanged.      |
// | 1.9     | 2026-08-05 | —      | ERR-008-019 owner revision: LONG_SHOT_RAMP_HALF_WIDTH 0.05 → 0.25 — the    |
// |         |            |        | full-range setting. The ramp spans the whole shifted domain [0.5, 1.0]:    |
// |         |            |        | every raw point 1–20 moves the modifier ≈ 0.026, no plateau at either      |
// |         |            |        | end. Still centred on the attribute midpoint, so the uniform-population    |
// |         |            |        | mean stays 0.30 (P5 holds at any symmetric half-width).                    |
// | 1.10    | 2026-08-05 | —      | ERR-008-019 adversarial review (doc only; no value changes):               |
// |         |            |        | LONG_SHOT_RAMP_HALF_WIDTH's XML doc stated a valid range of (0, 0.25]     |
// |         |            |        | that the suite forbids below 0.25 — ShootMidfield_FullRangeRamp_          |
// |         |            |        | EndpointsExact_AndStrictlyMonotone fails at any smaller half-width (the   |
// |         |            |        | end plateaus return, which is what the owner's no-plateau instruction     |
// |         |            |        | ruled out). Doc now records that (0, 0.25] is the FORMULA's validity      |
// |         |            |        | domain, not a free dial, and that a retune below 0.25 must revisit that   |
// |         |            |        | lock in the same change.                                                  |
// | 1.11    | 2026-08-06 | —      | ERR-008-021 AR-1 M-1 (doc only; no value changes): INTERCEPTOR_ABILITY_    |
// |         |            |        | MIN/MAX and LANE_VISION_FIDELITY_FLOOR now document their second          |
// |         |            |        | consumer — the §3.2.3.2 step-3a shot-lane occlusion (ERR-008-021) — and   |
// |         |            |        | the fidelity doc names "the reading agent" (passer OR shooter). These     |
// |         |            |        | three [GT]s are deliberately shared: one calibration lever moves both     |
// |         |            |        | lanes at the eventual KD-W1 balance pass.                                 |
#endregion
