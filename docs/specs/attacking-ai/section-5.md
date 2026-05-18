# Attacking AI Specification #15 — Section 5: Testing & Validation

**Created:** May 17, 2026
**Last Updated:** May 17, 2026 (v0.1 — initial draft from `outline-detailed.md` v1.1)
**Version:** 0.1
**Status:** DRAFT
**Source:** `outline-detailed.md` v1.1 (May 17, 2026)

---

## 5.1 Test Strategy

The test plan follows Testing Strategy & Framework #19 §3 and §4 for
taxonomy and FR-traceability conventions. All test-requirement IDs use
the `T-AT-` prefix for Attacking AI #15 unit/integration tests.

**Stage availability note:** All test execution is Stage 1+ (no simulation
runtime at Stage 0). The test catalogue is authored now to make measurement
methods unambiguous before implementation. The Stage-0 deliverable is the
test specification only.

### 5.1.1 Test Count Targets

| Category | Target | Binding |
|---|---|---|
| Unit (pool construction, role assignment, RunParameters, support heuristic, width-holding, weak-side, overload, transition, hysteresis, invariant enforcement) | ≥ 52 | §3.2–§3.12 |
| Integration (full-team possession phase under each profile, possession→transition→defense sequence) | ≥ 12 | §3.13 |
| Determinism regression | ≥ 6 | #16 §5 |
| Performance | ≥ 3 | §6 |
| Anti-chaos invariant tests | ≥ 6 | KD-13 |
| Style-profile tactical-identity tests | ≥ 6 | KD-10 / KD-12 |
| **Total** | **≥ 85** | — |

### 5.1.2 FR-to-Test Traceability

Every FR-AT-NNN in §2.1 is covered by at least one test in §5.2–§5.8.
The FR column in §5.2 identifies which FR each unit test exercises.
Integration tests exercise multiple FRs per scenario (listed in §5.3).

---

## 5.2 Unit Test Catalogue

Representative unit tests. At Stage 1, a full test-ID table is generated.
Each test exercises one narrow behaviour; parametric variants expand the
count to ≥ 52.

| T-AT-U-# | Test name | Input | Expected output | FR |
|---|---|---|---|---|
| 001 | GK excluded from pool | 11-agent team, 1 with `PlayerRole.Goalkeeper` | Pool size = 9 (1 GK + 1 ball carrier excluded) | FR-AT-006 |
| 002 | Ball carrier excluded from pool | Standard 11-agent team, carrier at EntityId=5 | EntityId=5 absent from pool | FR-AT-007 |
| 003 | OUT_OF_POSSESSION → empty directive | Phase = `OUT_OF_POSSESSION` | `AttackDirective` with all fields zeroed/false; no `AttackIntent` entries | FR-AT-008 |
| 004 | TRANSITION → frozen directive for N ticks | Phase transitions IN_POSSESSION → TRANSITION; `TRANSITION_HOLD_TICKS = 5` | For ticks 0–4: frozen directive returned; tick 5: empty directive | FR-AT-009 |
| 005 | TRANSITION counter SET before DECREMENT | Phase change on tick T | On tick T, `transitionHoldTick` = 5 (SET), then immediately decremented to 4 before return | FR-AT-009 |
| 006 | RUNNER assigned only to ATTACK/MIDFIELD agents | Pool of 3 agents: `lineMembership` = DEFENSE/MIDFIELD/ATTACK | Only MIDFIELD and ATTACK agents eligible for RUNNER; DEFENSE agent gets HOLD_WIDTH | FR-AT-012 |
| 007 | RUNNER not assigned when MAX_RUNNERS reached | 3 agents eligible, `MAX_RUNNERS = 2` | At most 2 RUNNER assignments; third gets SUPPORT_BALL or HOLD_WIDTH | FR-AT-018 |
| 008 | SUPPORT_BALL assigned when within radius | Agent at distance = `SUPPORT_RADIUS_M − 0.1` from ball carrier | Agent assigned SUPPORT_BALL | FR-AT-013 |
| 009 | SUPPORT_BALL NOT assigned when outside radius | Agent at distance = `SUPPORT_RADIUS_M + 0.1` from ball carrier | Agent NOT assigned SUPPORT_BALL; gets HOLD_WIDTH | FR-AT-013 |
| 010 | RunParameters: depthOffset_m clamped to [5.0, 40.0] | `BASE_RUN_DEPTH_M × depthMult` computes to 3.0m | `depthOffset_m = 5.0` (lower clamp) | FR-AT-011 |
| 011 | RunParameters: depthOffset_m clamped at upper bound | `BASE_RUN_DEPTH_M × depthMult` computes to 50.0m | `depthOffset_m = 40.0` (upper clamp) | FR-AT-011 |
| 012 | RunParameters: runTriggerTick always ≥ currentTick + 1 | `BASE_RUN_TRIGGER_DELAY_TICKS × timingMult` rounds to 0 | `runTriggerTick = currentTick + 1` (min 1 enforced by `max(1, ...)`) | FR-AT-011 |
| 013 | RunParameters: relativeAngle not stored in struct | Inspect `RunParameters` struct fields at runtime | Struct has exactly 3 fields: `depthOffset_m`, `lateralOffset_m`, `runTriggerTick`; angle absent | FR-AT-011 |
| 014 | RunParameters: run target computed correctly | Ball carrier at (70, 34), `teamAttackAngle = 0`, `lateralPct = 0.75`, DIRECT profile | `runTargetPosition = (91.0, 47.6)` per §3.4 worked example | FR-AT-011 |
| 015 | RunParameters: pitch-boundary clamp applied | Run target computes to (110, 70) | Clamped to (105, 68) | FR-AT-011 |
| 016 | teamAttackAngle = π (team attacking x=0) | Ball carrier at (35, 34), `teamAttackAngle = π`, `lateralPct = 0.5`, DIRECT | `depthVec = (−21, 0)`; `lateralVec = (0, 0)` → `runTargetPosition = (14.0, 34.0)` | FR-AT-011 |
| 017 | Width-holding: MIN_WIDTH_HOLDERS enforced | Only 1 HOLD_WIDTH agent on near-touchline side, `MIN_WIDTH_HOLDERS = 2` | Second agent promoted to HOLD_WIDTH | FR-AT-014 |
| 018 | Width-holding: nearTouchlineY correct (ball on y=68 side) | `ball.y = 55`, `TOUCHLINE_HOLD_DIST_M = 4.0` | `nearTouchlineY = 64.0m` | FR-AT-014 |
| 019 | Width-holding: nearTouchlineY correct (ball on y=0 side) | `ball.y = 10`, `TOUCHLINE_HOLD_DIST_M = 4.0` | `nearTouchlineY = 4.0m` | FR-AT-014 |
| 020 | Weak-side: correct agent selected | Pool of 5 agents, ball at y=50; agent at y=5 has greatest Y-deviation | Agent at y=5 assigned WEAK_SIDE | FR-AT-015 |
| 021 | Weak-side: EntityId tie-break | Two agents at equal Y-deviation | Lower EntityId assigned WEAK_SIDE | FR-AT-015 |
| 022 | Weak-side: not assigned when pool < MIN_WEAK_SIDE_AGENT_THRESHOLD | Pool size = 3, `MIN_WEAK_SIDE_AGENT_THRESHOLD = 4` | No WEAK_SIDE assignment | FR-AT-015 |
| 023 | Overload: fires when ≥ OVERLOAD_COUNT in corridor | 3 agents within `OVERLOAD_ZONE_WIDTH_M = 20m` of ball.y, none WEAK_SIDE | `overloadActive = true` | FR-AT-016 |
| 024 | Overload: does NOT fire when < OVERLOAD_COUNT | 2 agents in corridor, `OVERLOAD_COUNT = 3` | `overloadActive = false` | FR-AT-016 |
| 025 | Overload: WEAK_SIDE agent excluded from count | 4 agents near ball, 1 is WEAK_SIDE; `OVERLOAD_COUNT = 4` | Count = 3; `overloadActive = false` | FR-AT-016 |
| 026 | Overload: correct flank (ball on right) | `ball.y = 55 > 34` | `overloadFlank = RIGHT` | FR-AT-036 |
| 027 | Overload: correct flank (ball on left) | `ball.y = 15 < 34` | `overloadFlank = LEFT` | FR-AT-036 |
| 028 | Anti-chaos MAX_RUNNERS: excess demoted | 4 RUNNER assignments, `MAX_RUNNERS = 2` | 2 demoted; lowest `depthOffset_m` first | FR-AT-018 |
| 029 | Anti-chaos MAX_RUNNERS: EntityId tie-break on equal depth | Two runners at identical `depthOffset_m` | Lower EntityId demoted first | FR-AT-018 |
| 030 | Anti-chaos MIN_SUPPORT_AGENTS: shallowest RUNNER demoted | 0 SUPPORT_BALL/HOLD_WIDTH, `MIN_SUPPORT_AGENTS = 1` | Shallowest RUNNER demoted to SUPPORT_BALL | FR-AT-019 |
| 031 | Anti-chaos own-half block | RUNNER `runTargetPosition.x` = 46m (>5m past half-line into own half for team attacking x=105) | RUNNER demoted to HOLD_WIDTH | FR-AT-020 |
| 032 | Anti-chaos: all-default fallback | Unresolvable after `MAX_INVARIANT_PASSES` iterations | All agents HOLD_WIDTH or SUPPORT_BALL; zero RUNNERs | FR-AT-026 |
| 033 | Hysteresis: role retained when dwell valid | Agent held SUPPORT_BALL for `ATTACK_DWELL_TICKS = 3` ticks | Role retained even if new candidate is HOLD_WIDTH | FR-AT-022 |
| 034 | Hysteresis: role transitions after dwell expires | New candidate preferred for `ATTACK_DWELL_TICKS + 1` ticks | Role transitions to new candidate | FR-AT-023 |
| 035 | Hysteresis: oscillation suppressed at boundary | Agent alternates eligibility every tick | Role held for ≥ `ATTACK_DWELL_TICKS` ticks; no oscillation | FR-AT-023 |
| 036 | Failure F1: stale perception → frozen directive | Stale perception snapshot | Previous-tick directive frozen; no new `AttackIntent` | FR-AT-024 |
| 037 | Failure F2: #12 slot unavailable → empty directive | `formationSlot` unavailable | Empty directive emitted; no exception | FR-AT-025 |
| 038 | Failure F4: phase unavailable → OUT_OF_POSSESSION fallback | `GetPhase()` returns error | Treated as OUT_OF_POSSESSION; empty directive | FR-AT-027 |
| 039 | No PatternType/RunType enum (grep) | Static analysis of `src/AttackingAI/` | Zero matches for `PatternType\|RunType\|OverlapType` | FR-AT-010 |
| 040 | No PASS/SHOOT/DRIBBLE calls (grep) | Static analysis of `src/AttackingAI/` | Zero matches for `ActionType.PASS\|ActionType.SHOOT\|ActionType.DRIBBLE` | KD-3 |
| 041 | EntityId iteration ascending | Shuffled EntityIds in pool | Assignments in EntityId-ascending order | FR-AT-003 |
| 042 | COUNTER_ATTACK: TRANSITION_HOLD_TICKS = 0 | COUNTER_ATTACK profile; possession loss | Immediate empty directive | FR-AT-009 |
| 043 | DIRECT profile: MAX_RUNNERS = 3 | DIRECT profile active | Up to 3 RUNNER assignments | FR-AT-017 |
| 044 | POSSESSION profile: support radius scaled up | `SUPPORT_MULT = 1.3` | Effective radius = `SUPPORT_RADIUS_M × 1.3` | FR-AT-013 |
| 045 | DERIVED constant: FINAL_THIRD_X_M | Read `FINAL_THIRD_X_M` | 70.0m = `105 × 2/3` | KD-16 |
| 046 | Anti-chaos order: post-assignment, pre-publication | Inject violation; observe timing | Enforcer runs after role assignment, before publish | FR-AT-021 |
| 047 | WEAK_SIDE counts toward near-side width total | WEAK_SIDE agent on near-touchline side | Counts toward MIN_WIDTH_HOLDERS total | FR-AT-014 |
| 048 | Support heuristic minimum radius floor | COUNTER_ATTACK `supportMult = 0.5`; `effectiveSupportRadius = max(5.0, 6.0) = 6.0m`; agent at distance 4.0m | IS a SUPPORT_BALL candidate (4.0 ≤ 6.0); floor prevents effective radius collapse to zero | FR-AT-013 |
| 049 | Single constant catalogue (grep) | Static analysis of `src/AttackingAI/` | Constants only in `AttackingAIConstants.cs` | FR-AT-030 |
| 050 | Digest includes AttackHysteresisState | Read per-tick digest | `AttackHysteresisState[]` present | FR-AT-004 |
| 051 | Digest includes TransitionHoldState | Read digest after transition tick | `TransitionHoldState.transitionHoldTick` present | FR-AT-004 |
| 052 | 10 Hz tick rate enforced | Measure interval between `AttackingAITick` calls | Interval ≈ 100ms; no 60 Hz calls | FR-AT-001 |

---

## 5.3 Integration Test Catalogue

| T-AT-I-# | Scenario | Setup | Expected result |
|---|---|---|---|
| 001 | 9-agent IN_POSSESSION, POSSESSION profile | 9 off-ball agents; POSSESSION (`MAX_RUNNERS=1`, `SUPPORT_MULT=1.3`) | ≤ 1 RUNNER, ≥ 1 SUPPORT_BALL, ≥ 2 HOLD_WIDTH, ≥ 1 WEAK_SIDE (pool ≥ 4); all invariants satisfied |
| 002 | 9-agent IN_POSSESSION, DIRECT profile | DIRECT (`MAX_RUNNERS=3`, `DEPTH_MULT=1.4`) | ≤ 3 RUNNERS; `depthOffset_m ≈ 21m`; all invariants satisfied |
| 003 | 9-agent IN_POSSESSION, COUNTER_ATTACK profile | COUNTER_ATTACK (`MAX_RUNNERS=4`, `TIMING_MULT=0.5`) | ≤ 4 RUNNERS; fastest timing; `TRANSITION_HOLD_TICKS = 0` confirmed |
| 004 | Possession → TRANSITION → OUT_OF_POSSESSION (15 ticks) | POSSESSION; phase change tick 5 | Ticks 1–4: normal output; tick 5: SET+decrement; ticks 5–9: frozen; tick 10+: empty |
| 005 | Overload declared on correct flank | Ball at y=52; 3 non-WEAK_SIDE agents within 20m of y=52 | `overloadActive = true`, `overloadFlank = RIGHT` |
| 006 | Overload not declared when WEAK_SIDE excluded | 3 agents in corridor, 1 is WEAK_SIDE | Count = 2; `overloadActive = false` |
| 007 | Anti-chaos combination resolved | 4 RUNNER candidates; after demotion, MIN_SUPPORT still violated | Cascade resolves in ≤ 3 iterations; or all-default if unresolvable |
| 008 | All agents DEFENSE lineMembership | All pool agents `lineMembership = DEFENSE` | Zero RUNNER assignments; MIN_SUPPORT_AGENTS satisfied |
| 009 | Hysteresis over 10-tick run | Agent alternates SUPPORT_BALL-eligible every other tick | Role stable for ATTACK_DWELL_TICKS; oscillation suppressed |
| 010 | OUT_OF_POSSESSION skips all computation | Phase = OUT_OF_POSSESSION for 5 ticks | 5 empty directives; no role assignment performed |
| 011 | Profile depthOffset verified across all 3 profiles | Same agent, same ball position, 3 profiles | POSSESSION: 12m; DIRECT: 21m; COUNTER: 24m |
| 012 | Width-holding both sides | Ball at y=60, then y=8 | y=60: `nearTouchlineY = 64m`; y=8: `nearTouchlineY = 4m` |

---

## 5.4 Determinism and Numerical Verification (binding to #16 §5)

All determinism tests run on the reference host per §6.3.

| T-AT-D-# | Test | Method |
|---|---|---|
| 001 | 90-minute replay: bit-identical digest | Replay tick-for-tick; compare `AttackDirective` + `AttackIntent[]` + `AttackHysteresisState[]` + `TransitionHoldState` | Exact match required |
| 002 | EntityId iteration order stability | Fixed pool; observe assignment order across runs | Identical order every run |
| 003 | RNG domain tag isolation | Two matches; `DOMAIN_TAG_ATTACKING_AI` tagged correctly | No cross-domain bleed |
| 004 | Hysteresis state digest round-trip | Serialize/deserialize `AttackHysteresisState[]`; resume tick | Identical output after restore |
| 005 | TransitionHoldState round-trip | Serialize/deserialize; resume transition | Countdown continues correctly |
| 006 | Invariant demotion determinism | Same violation; run 1000 times | Identical demotion order every run |

---

## 5.5 Performance Validation (binding to §6.3)

| T-AT-P-# | Test | Budget |
|---|---|---|
| 001 | Per-tick time, 9 off-ball agents, POSSESSION | ≤ 0.10 ms on reference host |
| 002 | Per-tick time, 10 agents, COUNTER_ATTACK (max pool + max RUNNER candidates) | ≤ 0.10 ms |
| 003 | Per-tick time, anti-chaos worst case (3 invariant-pass iterations, all-default path) | ≤ 0.10 ms |

---

## 5.6 Anti-Chaos and Profile Tests

### 5.6.1 Anti-Chaos (binding to KD-13)

| T-AT-C-# | Invariant | Scenario | Expected |
|---|---|---|---|
| 001 | MAX_RUNNERS cap | Pool of 10 ATTACK agents, `MAX_RUNNERS = 2` | Exactly 2 RUNNER; 8 demoted |
| 002 | MAX_RUNNERS demotion order | 4 runners, depths 30/25/20/15m | Runners at 15m and 20m demoted first |
| 003 | MIN_SUPPORT after RUNNER cap | After capping, 0 SUPPORT_BALL/HOLD_WIDTH remain | Shallowest runner demoted; MIN_SUPPORT satisfied |
| 004 | OWN_HALF_RUN_BLOCK | RUNNER target x=47m (own half >5m past half-line) for team attacking x=105 | RUNNER demoted to HOLD_WIDTH |
| 005 | MAX_INVARIANT_PASSES fallback | Constructed unresolvable scenario | All-default directive; no exception |
| 006 | Multi-invariant cascade | MAX_RUNNERS + MIN_SUPPORT + OWN_HALF all violated | Applied in order 1→2→3; resolved or all-default |

### 5.6.2 Style-Profile Tactical Identity (binding to KD-10 / KD-12)

| T-AT-S-# | Test | Measurement | Acceptance threshold |
|---|---|---|---|
| 001 | DIRECT vs. POSSESSION runner count | Total RUNNER assignments in 90-min simulated match | DIRECT ≥ POSSESSION + 15 (`DIRECT_RUN_COUNT_DELTA`) |
| 002 | DIRECT run timing | Mean `runTriggerTick − currentTick` | DIRECT ≈ 2 ticks; POSSESSION ≈ 4 ticks |
| 003 | POSSESSION support radius larger | SUPPORT_BALL assignments per match | POSSESSION > DIRECT (higher `SUPPORT_MULT`) |
| 004 | COUNTER_ATTACK transition hold = 0 | Mean `transitionHoldTick` per possession loss | ≤ `COUNTER_MAX_HOLD_TICKS` = 0 |
| 005 | COUNTER_ATTACK runner count vs. others | RUNNER assignments per 90-min match | COUNTER_ATTACK ≥ DIRECT ≥ POSSESSION |
| 006 | Profile: algorithm code path identical | Trace code branches for all 3 profiles | Zero `if (profile == X)` branches; only constant values differ |

---

## 5.7 Dangerous-Zone Surrogate Validation (resolves outline.md H-5 / KD-10)

**Stage-0 measurement declaration.** Actual threshold validation is Stage 1+.

**Metric 1 — Shots in dangerous zone:**
Count of `SHOOT` actions (per #8 action log) where:
- `distanceToGoalCenter ≤ DANGER_ZONE_MAX_DIST_M [GT]` = 20.0 m, AND
- `|ball.y − goalCenter.y| ≤ DANGER_ZONE_CORRIDOR_HW_M [GT]` = 10.16 m

`goalCenter` for team attacking x=105: `(105, 34)`. For team attacking x=0: `(0, 34)`.

**Metric 2 — Average shot distance:** Mean of `distanceToGoalCenter` over all
`SHOOT` actions per team per match.

**Stage-1+ acceptance thresholds** (`[GT]`, tunable; declared here for measurement
clarity):
- POSSESSION: ≥ 3 dangerous-zone shots per 90 min per team.
- DIRECT: ≥ 4 dangerous-zone shots per 90 min per team.
- COUNTER_ATTACK: ≥ 3 dangerous-zone shots per 90 min per team.

---

## 5.8 Tactical Identity Tests (resolves outline.md L-13 / KD-10)

**DIRECT vs. POSSESSION:**
- DIRECT MUST produce ≥ `DIRECT_RUN_COUNT_DELTA [GT]` = 15 more RUNNER
  assignments per 90-min simulated match than POSSESSION.
- Measured from `AttackIntent.role` histograms in `AttackIntentSnapshot` log.

**COUNTER_ATTACK transition speed:**
- COUNTER_ATTACK MUST produce ≤ `COUNTER_MAX_HOLD_TICKS [GT]` = 0 average
  `transitionHoldTick` per possession-loss event.
- Measured from `TransitionHoldState` countdown log.

Both criteria are falsifiable. Failure indicates mistuned `[GT]` constants;
the spec defines the measurement method and minimum threshold only.

---

## 5.9 Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 17, 2026 | AI agent (claude-sonnet-4-6 / draft-attacking-ai-spec) | Initial draft from `outline-detailed.md` v1.1. §5.1–§5.9 authored. 52 unit + 12 integration + 6 determinism + 3 performance + 6 anti-chaos + 6 profile = 85 total (meets ≥85 target). Dangerous-zone and tactical-identity criteria declared per KD-10. |
| 0.2 | May 18, 2026 | AI agent (claude-sonnet-4-6) | T-AT-U-048 corrected: expected output was "Not a SUPPORT_BALL candidate" (wrong — agent at distance < 5m is within the `max(5.0, effectiveSupportRadius)` floor and IS a SUPPORT_BALL candidate); updated to reflect correct behavior with a concrete COUNTER_ATTACK example. |
