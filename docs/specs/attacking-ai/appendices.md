# Attacking AI Specification #15 — Appendices

**Created:** May 17, 2026
**Last Updated:** May 17, 2026 (v0.1 — initial draft from `outline-detailed.md` v1.1)
**Version:** 0.1
**Status:** DRAFT
**Source:** `outline-detailed.md` v1.1 (May 17, 2026)

---

## Appendix A — Derivations

### A.1 ATTACK_DWELL_TICKS `[EST]` → `[GT]` Promotion

**Promoted value:** `ATTACK_DWELL_TICKS = 3 [GT]`

**Derivation:**

The dwell-time hysteresis pattern is inherited from Agent Movement #2 §3.1
(`XC-015-003`). The principle: a role transition fires only after the new
candidate has been consistently preferred for N consecutive ticks.

Selection rationale for N = 3:

- At 10 Hz, 3 ticks = 300 ms. This is long enough to suppress oscillation
  at the common tactical boundary conditions (e.g., an agent oscillating
  at exactly `SUPPORT_RADIUS_M` distance every tick due to ball movement),
  which occur on a per-tick basis (100 ms cycle).
- 300 ms is short enough that #15 responds within 1 second to a genuine
  role-change event (e.g., a ball carrier drive opening a new run lane).
  Human reaction time to tactical events is 400–800 ms at elite level;
  300 ms hysteresis keeps AI response within perceptually acceptable range.
- The Defensive AI #14 `MARK_DWELL_TICKS [GT]` uses the same value (3
  ticks), establishing a cross-spec consistency anchor for the tactical-AI
  chain's hysteresis latency.
- Values below 2 ticks would allow oscillation on the first "miss" tick;
  values above 5 ticks would introduce noticeable lag for genuine role
  changes visible to a watching player.

**Conclusion:** N = 3 balances oscillation suppression and tactical
responsiveness. Promoted from `[EST]` to `[GT]` (gameplay-tunable) because
the exact value may require adjustment after first-runtime play-testing.

---

### A.2 FINAL_THIRD_X_M `[DERIVED]`

**Formula:** `FINAL_THIRD_X_M = PITCH_LENGTH_M [CROSS: #1 §1.2] × 2/3`

**Computation:** `105.0 × 2/3 = 70.0 m`

**Interpretation:** For the team attacking the x=105 goal, the final third
begins at x = 70.0 m (measuring from the corner-origin). For the team
attacking the x=0 goal, the final third begins at x = 35.0 m (= 105 − 70.0).

This spec uses a normalised "distance-to-opponent-goal" scalar internally
(per KD-16) to avoid per-team branching, so the literal value 70.0m is not
hard-coded in algorithm code — it appears only in the constant catalogue for
documentation and in §5.7 as part of the surrogate metric definition.

**Update rule:** If `PITCH_LENGTH_M` changes (per #1 §1.2 update), this
constant MUST be re-derived. This is enforced by the `[DERIVED]` tag.

---

### A.3 DANGER_ZONE_CORRIDOR_HW_M `[GT]`

**Promoted value:** `DANGER_ZONE_CORRIDOR_HW_M = 10.16 m [GT]`

**Design intent:** The "dangerous zone" is the area in front of goal from
which shots have the highest conversion probability at Stage 0 (before a
proper xG model is available at Stage 1+). The corridor half-width defines
how far from the goal centerline a SHOOT action must occur to count as a
"dangerous-zone shot" in the §5.7 surrogate metric.

**Derivation:**

FIFA-standard pitch geometry (per #1 §1.2):
- Penalty area width: 40.32 m total (16.5 m from each post, posts 7.32 m apart
  → 16.5 + 3.66 = 20.16 m from centre to each penalty-area post).
- Penalty area half-width (from goal centre to penalty area edge): 20.16 m.
- 6-yard box half-width (from goal centre): 9.16 m (= 3.66 + 5.5 m).

Derivation of 10.16 m:
- Start with the 6-yard-box half-width: 9.16 m (closest high-danger zone
  to goal mouth).
- Add a goalkeeper diving-reach margin: ≈ 1.0 m (goalkeeper can dive
  approximately arm's length beyond the 6-yard box boundary without
  leaving the zone).
- Result: 9.16 + 1.0 = 10.16 m.

This value places the dangerous-zone corridor slightly wider than the 6-yard
box but well within the penalty area (penalty area half-width = 20.16 m).
Shots from within this corridor are broadly consistent with "central penalty
area" shots in published xG frameworks.

**Tunable:** Tagged `[GT]` because the exact boundary is a gameplay design
decision. The derivation establishes the design intent; the lead developer
may adjust after first-runtime play-testing. If pitch dimensions change per
#1 §1.2, this constant should be re-reviewed (though it is tagged `[GT]`,
not `[DERIVED]`, because the relationship to pitch dimensions is
design-intent rather than mathematically necessary).

---

### A.4 Style-Profile Multiplier Rationale

The three team-style profiles (`POSSESSION`, `DIRECT`, `COUNTER_ATTACK`)
are implemented as constant-multiplier clusters. The algorithm code is
identical across all profiles (KD-8 / KD-12). The following table records
the design intent for each multiplier value:

| Multiplier | POSSESSION | DIRECT | COUNTER_ATTACK | Rationale |
|---|---|---|---|---|
| `DEPTH_MULT` | 0.8 | 1.4 | 1.6 | POSSESSION: shallower runs preserve possession shape (closer support triangle); DIRECT: deeper runs open channels; COUNTER_ATTACK: deepest runs to exploit disorganised defence immediately |
| `TIMING_MULT` | 1.2 | 0.7 | 0.5 | POSSESSION: delayed runs (patient build-up, wait for openings); DIRECT: faster runs (vertical intent); COUNTER_ATTACK: instant runs (exploit momentum before defence recovers) |
| `SUPPORT_MULT` | 1.3 | 0.8 | 0.5 | POSSESSION: wider support radius (more agents close to carrier, maximising passing options); DIRECT: tighter (fewer close support agents; space for runs); COUNTER_ATTACK: minimal (all agents making distance runs) |
| `MAX_RUNNERS_OVERRIDE` | 1 | 3 | 4 | POSSESSION: one disciplined run at a time (high-press vulnerability if caught with many runners); DIRECT: three runners (enough to stretch backline); COUNTER_ATTACK: four runners (maximum overload of recovering defence) |
| `TRANSITION_HOLD_TICKS` | 5 (500ms) | 5 (500ms) | 0 (instant) | POSSESSION/DIRECT: brief hold after possession loss allows continuation of a partially-completed run before defensive shape sets; COUNTER: immediate recovery (agents never committed to deep runs — they are always ready to press) |

**Note on COUNTER_ATTACK `TRANSITION_HOLD_TICKS = 0`:** This means the
COUNTER_ATTACK profile produces an immediate empty `AttackDirective` on
possession loss. Agents revert instantly to their #12 baseline defensive
shape. This creates the tactical signature of a counter-attacking team:
rapid defensive organisation after possession loss, followed by explosive
re-launch when the ball is recovered.

---

## Appendix B — RunParameters Verification Table

Four canonical scenarios for §3.4 RunParameters generation. All use
corner-origin coordinates. teamAttackAngle = 0.0 rad (team attacking x=105)
unless stated otherwise.

### Scenario B.1 — POSSESSION Profile, Central Lane

**Setup:**
- Ball carrier position: (60, 34) — midfield, central
- `teamAttackAngle` = 0.0 rad (attacking toward x=105)
- `formationSlot.lateralPct` = 0.5 (central lane)
- Profile: POSSESSION (`DEPTH_MULT = 0.8`, `TIMING_MULT = 1.2`)
- `BASE_RUN_DEPTH_M = 15.0`, `LATERAL_SCALE = 0.8`, `BASE_RUN_TRIGGER_DELAY_TICKS = 3`

**Computation:**
```
depthOffset_m   = Clamp(15.0 × 0.8, 5.0, 40.0) = Clamp(12.0, ...) = 12.0 m
centeredPct     = 0.5 − 0.5 = 0.0
lateralOffset_m = Clamp(0.0 × 68 × 0.8, −34.0, 34.0) = 0.0 m
runTriggerTick  = currentTick + max(1, round(3 × 1.2))
                = currentTick + max(1, round(3.6))
                = currentTick + max(1, 4) = currentTick + 4

depthVec    = Vector2(cos(0), sin(0)) × 12.0 = Vector2(1, 0) × 12.0 = (12.0, 0)
lateralVec  = Vector2(−sin(0), cos(0)) × 0.0 = (0, 0)
runTarget   = (60, 34) + (12, 0) + (0, 0) = (72.0, 34.0)
```

**Result:** `depthOffset_m = 12.0m`, `lateralOffset_m = 0.0m`,
`runTriggerTick = currentTick + 4`, `runTargetPosition = (72.0, 34.0)`.

Geometry: central penetrating run, 12m ahead, delayed 400ms — typical
possession-style through-ball target.

---

### Scenario B.2 — DIRECT Profile, Right Lane (from §3.4 main worked example)

**Setup:**
- Ball carrier position: (70, 34)
- `teamAttackAngle` = 0.0 rad
- `formationSlot.lateralPct` = 0.75 (right-side channel)
- Profile: DIRECT (`DEPTH_MULT = 1.4`, `TIMING_MULT = 0.7`)

**Computation:**
```
depthOffset_m   = Clamp(15.0 × 1.4, 5.0, 40.0) = 21.0 m
centeredPct     = 0.75 − 0.5 = 0.25
lateralOffset_m = Clamp(0.25 × 68 × 0.8, −34.0, 34.0)
                = Clamp(13.6, ...) = 13.6 m
runTriggerTick  = currentTick + max(1, round(3 × 0.7))
                = currentTick + max(1, round(2.1))
                = currentTick + max(1, 2) = currentTick + 2

depthVec    = Vector2(1, 0) × 21.0 = (21.0, 0)
lateralVec  = Vector2(0, 1) × 13.6 = (0, 13.6)
runTarget   = (70, 34) + (21, 0) + (0, 13.6) = (91.0, 47.6)
Clamp: x=91.0 ∈ [0,105] ✓; y=47.6 ∈ [0,68] ✓
```

**Result:** `depthOffset_m = 21.0m`, `lateralOffset_m = 13.6m`,
`runTriggerTick = currentTick + 2`, `runTargetPosition = (91.0, 47.6)`.

Geometry: right-channel overlap run into the final third — inside final
third (91 > FINAL_THIRD_X_M = 70), right-side angle. Classic "overlap"
in gameplay vocabulary.

---

### Scenario B.3 — COUNTER_ATTACK Profile, Left Lane

**Setup:**
- Ball carrier position: (55, 34)
- `teamAttackAngle` = 0.0 rad
- `formationSlot.lateralPct` = 0.25 (left-side channel)
- Profile: COUNTER_ATTACK (`DEPTH_MULT = 1.6`, `TIMING_MULT = 0.5`)

**Computation:**
```
depthOffset_m   = Clamp(15.0 × 1.6, 5.0, 40.0) = Clamp(24.0, ...) = 24.0 m
centeredPct     = 0.25 − 0.5 = −0.25
lateralOffset_m = Clamp(−0.25 × 68 × 0.8, −34.0, 34.0)
                = Clamp(−13.6, ...) = −13.6 m
runTriggerTick  = currentTick + max(1, round(3 × 0.5))
                = currentTick + max(1, round(1.5))
                = currentTick + max(1, 2) = currentTick + 2

depthVec    = Vector2(1, 0) × 24.0 = (24.0, 0)
lateralVec  = Vector2(0, 1) × (−13.6) = (0, −13.6)
runTarget   = (55, 34) + (24, 0) + (0, −13.6) = (79.0, 20.4)
Clamp: x=79.0 ∈ [0,105] ✓; y=20.4 ∈ [0,68] ✓
```

**Result:** `depthOffset_m = 24.0m`, `lateralOffset_m = −13.6m`,
`runTriggerTick = currentTick + 2`, `runTargetPosition = (79.0, 20.4)`.

Geometry: left-channel inward-angled run (toward y=0 touchline) into the
final third. "Underlap" geometry in gameplay vocabulary (inward angle toward
centre, not outside the carrier).

---

### Scenario B.4 — depthOffset_m Upper-Clamp Edge Case

**Setup:**
- Profile: COUNTER_ATTACK with hypothetical `BASE_RUN_DEPTH_M = 30.0` (for
  edge-case demonstration)
- `formationSlot.lateralPct` = 1.0 (wide right extreme)
- Ball carrier at (50, 34), `teamAttackAngle` = 0.0 rad

**Computation:**
```
depthOffset_m   = Clamp(30.0 × 1.6, 5.0, 40.0) = Clamp(48.0, ...) = 40.0 m  ← upper clamp fires
centeredPct     = 1.0 − 0.5 = 0.5
lateralOffset_m = Clamp(0.5 × 68 × 0.8, −34.0, 34.0) = Clamp(27.2, ...) = 27.2 m
runTriggerTick  = currentTick + 2 (same as B.3)

depthVec    = Vector2(1, 0) × 40.0 = (40.0, 0)
lateralVec  = Vector2(0, 1) × 27.2 = (0, 27.2)
runTarget   = (50, 34) + (40, 0) + (0, 27.2) = (90.0, 61.2)
Clamp: x=90.0 ∈ [0,105] ✓; y=61.2 ∈ [0,68] ✓
```

**Result:** `depthOffset_m = 40.0m` (clamped from 48.0m),
`lateralOffset_m = 27.2m`, `runTargetPosition = (90.0, 61.2)`.

Demonstrates that the upper clamp prevents runs beyond 40m depth, which
would otherwise produce implausibly deep targets (a carrier at midfield with
48m depth would produce a target at x=98m from x=50, crossing the
opposition penalty area).

---

## Appendix C — Width-Holding and Weak-Side Reference Card

Three canonical scenarios illustrating §3.6 (width-holding) and
§3.7 (weak-side) computation.

### Scenario C.1 — 9 Agents, Ball at Center, Correct Width Allocation

**Setup:** Pool of 9 agents after role assignment. Ball at (60, 34).
`MIN_WIDTH_HOLDERS = 2`. `MIN_WEAK_SIDE_AGENT_THRESHOLD = 4`.
1 RUNNER, 2 SUPPORT_BALL, 3 HOLD_WIDTH, 2 WEAK_SIDE (hypothetical pre-check state).

**Width-holding check:** ball.y = 34 < 34 (not ≥ PITCH_WIDTH_M/2 = 34).
Using `<` strictly: `nearTouchlineY = TOUCHLINE_HOLD_DIST_M = 4.0m`.
HOLD_WIDTH agents assigned to (ballCarrier.x, 4.0) = (60.0, 4.0).

Actually ball.y = 34 = PITCH_WIDTH_M/2 exactly. Per §3.6: "if ball.y ≥ PITCH_WIDTH_M / 2" → condition is true (34 ≥ 34). `nearTouchlineY = 68 − 4.0 = 64.0m`. Width-holders go to (60.0, 64.0). 

Count check: 3 HOLD_WIDTH + 1 WEAK_SIDE (if on near side) ≥ `MIN_WIDTH_HOLDERS = 2` → no promotion needed.

**Result:** Width-holding satisfied. HOLD_WIDTH target = (60.0, 64.0).
WEAK_SIDE agent targets (60 + 5, y_weakside) = (65.0, 8.0) per §3.7
(ball.y ≥ 34 → `weakSideTarget.y = WEAK_SIDE_FAR_Y_M = 8.0m`).

---

### Scenario C.2 — Pool of 3 Agents, WEAK_SIDE Not Assigned

**Setup:** Pool size = 3 (< `MIN_WEAK_SIDE_AGENT_THRESHOLD = 4`).

Per FR-AT-015 and §3.7: "At least one agent holds the WEAK_SIDE position
unless agent count < MIN_WEAK_SIDE_AGENT_THRESHOLD". With pool = 3 < 4:
**no WEAK_SIDE assignment**. All 3 agents assigned SUPPORT_BALL or HOLD_WIDTH
based on role priority.

**Rationale:** Dedicating an agent to the weak side with only 3 pool agents
would leave the ball side dangerously undermanned. The threshold prevents
this misallocation.

---

### Scenario C.3 — Agent at Exactly SUPPORT_RADIUS_M Boundary (Hysteresis)

**Setup:** Agent at distance exactly `SUPPORT_RADIUS_M = 12.0m` from ball
carrier. Previous role = SUPPORT_BALL (held for `ATTACK_DWELL_TICKS = 3`
ticks). Ball carrier moves 0.5m away, making agent distance = 12.5m.

**Tick-by-tick trace (5 ticks):**
- Tick T: agent at 12.0m. `SUPPORT_RADIUS_M = 12.0`. Agent eligible (= boundary). Role: SUPPORT_BALL. Dwell counter = 3 (at threshold).
- Tick T+1: ball carrier moves; agent now at 12.5m > SUPPORT_RADIUS_M. New candidate = HOLD_WIDTH. `ATTACK_DWELL_TICKS` dwell: new candidate preferred for 1 tick (counter = 1).
- Tick T+2: agent at 12.5m. New candidate = HOLD_WIDTH. Dwell = 2.
- Tick T+3: agent at 12.5m. New candidate = HOLD_WIDTH. Dwell = 3 ≥ `ATTACK_DWELL_TICKS`. **Transition fires.** Role = HOLD_WIDTH.
- Tick T+4: Role = HOLD_WIDTH (stable).

**Conclusion:** The 3-tick dwell suppresses the oscillation on ticks T+1
and T+2, producing a stable role change on tick T+3. If the ball carrier
had moved back within 12.0m on tick T+2, the dwell counter would reset
and no transition would fire.

---

## Appendix D — Acceptance Criteria and Style-Profile Validation

### D.1 Dangerous-Zone Shot Surrogate

**Stage-0 declaration of measurement method** (per KD-10 / §5.7):
- Dangerous zone = distance ≤ `DANGER_ZONE_MAX_DIST_M = 20.0m` to goal
  centre AND |y − goalCentre.y| ≤ `DANGER_ZONE_CORRIDOR_HW_M = 10.16m`.
- Measurement: count of #8 SHOOT actions satisfying both conditions.
- Baseline comparison: Stage-0 behaviour (no #15 coordination) vs.
  Stage-1 behaviour (with #15 RunParameters guiding off-ball positions).
- Stage-of-availability: Stage 1 first-runtime. No Stage-0 measurement.

**Stage-1+ provisional thresholds** (`[GT]`, tunable):
- POSSESSION profile: ≥ 3 dangerous-zone shots per 90 min per team.
- DIRECT profile: ≥ 4 dangerous-zone shots per 90 min per team.
- COUNTER_ATTACK profile: ≥ 3 dangerous-zone shots per 90 min per team.

These thresholds assume that #15's off-ball positioning creates better
shot opportunities than Stage-0 uncoordinated MOVE_TO_POSITION. If #15
reduces dangerous-zone shots below Stage-0 baselines, the constant tuning
is incorrect (not a spec defect).

### D.2 DIRECT vs. POSSESSION Tactical Identity Test

**Threshold:** `DIRECT_RUN_COUNT_DELTA [GT] = 15`

**Justification:** In a 90-minute match at 10 Hz, approximately 54,000
tactical ticks fire per team. With 9 off-ball agents and DIRECT
`MAX_RUNNERS_DIRECT = 3` vs. POSSESSION `MAX_RUNNERS_POSSESSION = 1`,
the DIRECT profile fires up to 2 more RUNNER assignments per tick on
average, subject to eligibility and invariant enforcement. Over a full
match with realistic possession phases (≈ 30% of ticks IN_POSSESSION),
the expected delta is ≈ 2 × 0.30 × 54,000 × (occupancy fraction) >> 15.
A threshold of 15 additional RUNNER assignments is a weak minimum that
will be easily met if the profiles are correctly differentiated; it
exists as a regression gate, not an aspirational target.

### D.3 COUNTER_ATTACK Transition-Speed Test

**Threshold:** `COUNTER_MAX_HOLD_TICKS [GT] = 0`

**Justification:** COUNTER_ATTACK profile is defined as having
`TRANSITION_HOLD_TICKS = 0` per §3.9 / §3.10. The test directly verifies
that the constant is applied correctly (no hold on possession loss). A
value > 0 would indicate the profile multiplier system is broken.

---

## Appendix E — Anti-Chaos Sensitivity Analysis

Qualitative sensitivity analysis for the three anti-chaos invariants
in §3.11 / KD-13.

### E.1 MAX_RUNNERS Sensitivity

| Setting | Effect on overload frequency | Effect on defensive exposure |
|---|---|---|
| MAX_RUNNERS = 1 (tighter) | Reduces overload frequency; at most one run per tick | Reduces counter-attack vulnerability; lowest tactical variety |
| MAX_RUNNERS = 2 (default POSSESSION) | Balanced; allows two simultaneous runs | Moderate defensive exposure on turnovers |
| MAX_RUNNERS = 3 (DIRECT) | Higher overload frequency | Increased counter-attack exposure |
| MAX_RUNNERS = 4 (COUNTER_ATTACK) | Maximum overload probability | Highest exposure (offset by zero TRANSITION_HOLD_TICKS — agents recover immediately) |

### E.2 MIN_SUPPORT_AGENTS Sensitivity

| Setting | Effect |
|---|---|
| MIN_SUPPORT_AGENTS = 0 | All agents could be RUNNERs; ball carrier has no short pass option; not recommended |
| MIN_SUPPORT_AGENTS = 1 (default) | At least one close support option; minimal constraint |
| MIN_SUPPORT_AGENTS = 2 | Ensures two support options; reduces RUNNER count by 1 in tight invariant scenarios |
| MIN_SUPPORT_AGENTS = 3 | Strongly constrains RUNNER count; possession-oriented behaviour regardless of profile |

### E.3 OWN_HALF_RUN_BLOCK_M Sensitivity

| Setting | Effect |
|---|---|
| OWN_HALF_RUN_BLOCK_M = 0 | No own-half block; agents may run entirely backward (degenerate) |
| OWN_HALF_RUN_BLOCK_M = 5.0 (default) | Allows runs up to 5m behind the half-line (handles deep-lying forward patterns) |
| OWN_HALF_RUN_BLOCK_M = 0 (tight) | No own-half runs permitted; all RUNNER targets must be in opponent half |
| OWN_HALF_RUN_BLOCK_M = 15.0 (loose) | Allows runs deep into own half; useful for very defensive buildup patterns |

---

## Appendix F — Glossary

| Term | Definition |
|---|---|
| **Attacking role** | One of four discrete labels assigned to each off-ball agent per tick: `RUNNER`, `SUPPORT_BALL`, `HOLD_WIDTH`, `WEAK_SIDE`. Not an enum in the physics layer — stored only in the `AttackIntent` AI-layer struct. |
| **Attack directive** | The per-team per-tick output of Attacking AI #15: one `AttackDirective` struct carrying `overloadActive bool`, `overloadFlank` (LEFT/RIGHT), and `transitionHoldTick int`. |
| **Attack intent** | The per-agent per-tick output of Attacking AI #15: one `AttackIntent` struct per off-ball agent carrying `role`, `runParameters RunParameters?` (null unless RUNNER), and `validThroughTick int`. |
| **RunParameters** | Sub-struct of `AttackIntent` with exactly three fields: `depthOffset_m float`, `lateralOffset_m float`, `runTriggerTick int`. The angle of a run is a derived quantity (`atan2(lateralOffset_m, depthOffset_m)`) computed at use-site only — never stored. |
| **Off-ball movement pool** | The set of all outfield agents for a team, excluding the goalkeeper and the current ball carrier. Pool size is at most 10 (11 outfield − 1 ball carrier). |
| **Support radius** | The radius within which an agent is considered a `SUPPORT_BALL` candidate: `SUPPORT_RADIUS_M × styleProfile.supportMult`. |
| **Overload zone** | The Y-corridor around the ball's current Y-position with half-width `OVERLOAD_ZONE_WIDTH_M`. When ≥ `OVERLOAD_COUNT` non-WEAK_SIDE agents are in this corridor, an overload is declared. |
| **Weak-side puller** | An agent assigned the `WEAK_SIDE` role whose function is to occupy the opposite half of the pitch from the ball, creating a width threat and preventing the defence from compacting on the ball side. |
| **Width holder** | An agent assigned the `HOLD_WIDTH` role whose function is to maintain a presence near the near-touchline at the ball's current depth, preventing the defence from condensing centrally. |
| **Team-style profile** | A named cluster of `[GT]` constant multipliers (`POSSESSION`, `DIRECT`, or `COUNTER_ATTACK`) that modify run depth, timing, support radius, and maximum runner count without changing the algorithm code. |
| **Transition-hold** | The period (in ticks) after possession loss during which #15 continues to emit the last attacking directive (frozen) before switching to an empty directive. Duration = `TRANSITION_HOLD_TICKS` (0 for COUNTER_ATTACK profile). |
| **Anti-chaos invariant** | One of three measurable constraints enforced post-assignment, pre-publication in §3.11: (1) `MAX_RUNNERS`, (2) `MIN_SUPPORT_AGENTS`, (3) `OWN_HALF_RUN_BLOCK_M`. |
| **Assignment hysteresis** | A per-agent dwell counter that suppresses role transitions until the new candidate role has been consistently preferred for `ATTACK_DWELL_TICKS = 3` consecutive ticks. Prevents oscillation at boundary conditions. |
| **Dangerous zone** | A Stage-0 surrogate for shot quality: the area within `DANGER_ZONE_MAX_DIST_M = 20m` of the opponent goal centre and within `DANGER_ZONE_CORRIDOR_HW_M = 10.16m` of the goal centreline. Replaces an xG model at Stage 0. |
| **Final third** | For the team attacking x=105: the region where x ≥ `FINAL_THIRD_X_M [DERIVED] = 70.0m`. For the team attacking x=0: x ≤ 35.0m. #15 uses a normalised "distance-to-opponent-goal" scalar internally to avoid per-team branching. |
| **Weak side (formal)** | The half of the Y-axis opposite to the ball's current Y-position. Formal definition (KD-16): if `ball.y > PITCH_WIDTH_M / 2`, weak side is toward y=0 (`weakSideTarget.y = WEAK_SIDE_FAR_Y_M`); else weak side is toward y=68 (`weakSideTarget.y = PITCH_WIDTH_M − WEAK_SIDE_FAR_Y_M`). Asymmetric thresholds in KD-16 prevent Y=34 flicker. |
| **Overlap** | Gameplay vocabulary for a `RunParameters` geometry where `depthOffset_m ≈ 15–25m` and `|lateralOffset_m|` is large (agent runs outside the ball carrier's lane). Not an enum — described by the three `RunParameters` fields only. |
| **Underlap** | Gameplay vocabulary for `depthOffset_m ≈ 8–15m` and `lateralOffset_m` toward the pitch centre (inward of the ball carrier's lane). Not an enum. |
| **Third-man run** | Gameplay vocabulary for `depthOffset_m ≈ 20–30m`, `lateralOffset_m ≈ 0`, `runTriggerTick` set N ticks after the first pass (enabling a third player to receive a through-ball). Not an enum. |
| **Cutback** | Gameplay vocabulary for a run toward the goal line with a short lateral offset, generating a low cross. Owned by #15 at Stage 2+ via set-piece / open-play cross infrastructure (§7.5). Not implemented at Stage 0 or Stage 1. |

---

## Appendix G — Telemetry & Troubleshooting Playbook

**Stage 0 placeholder.** This appendix will be populated at Stage 1
implementation. The following debug overlays are planned:

| Overlay | Description |
|---|---|
| Per-agent role label | Text label (`RUNNER` / `SUPPORT_BALL` / `HOLD_WIDTH` / `WEAK_SIDE`) displayed above each agent in the pitch debug view on each 10 Hz tick |
| Overload zone highlight | Y-corridor shading around ball position when `overloadActive = true`; flank label (LEFT/RIGHT) |
| Run-target position trail | Line from each RUNNER agent's current position to its `runTargetPosition`; highlighted at `runTriggerTick` |
| Weak-side corridor shading | Y-corridor shading on the weak side to visualise the WEAK_SIDE agent's target zone |
| Transition-hold countdown | HUD counter showing `transitionHoldTick` for each team during TRANSITION phase |
| Hysteresis dwell counter | Per-agent dwell counter displayed in the advanced debug panel (role + current dwell count vs. `ATTACK_DWELL_TICKS`) |
| Constant-catalogue inspector | Runtime display of active `[GT]` values per style profile (which profile is active, all multiplier values) |

All overlays are non-authoritative display aids; they do not feed back into
the simulation. They are enabled via the Unity Editor debug flags and are
excluded from production builds.

---

## Appendix H — Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 17, 2026 | AI agent (claude-sonnet-4-6 / draft-attacking-ai-spec) | Initial draft from `outline-detailed.md` v1.1. Appendices A–G authored. Derivations A.1–A.4 complete. RunParameters verification table B.1–B.4 with full calculations. Width/weak-side reference C.1–C.3. Acceptance criteria D.1–D.3. Sensitivity analysis E.1–E.3. Full glossary F. Telemetry placeholder G. |
