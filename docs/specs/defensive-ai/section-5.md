# Defensive AI Specification #14 — Section 5: Test Plan

**Created:** May 17, 2026
**Last Updated:** May 17, 2026 (v0.2 — PASS-1 adversarial review fix pass; L1 resolved)
**Version:** 0.2
**Status:** DRAFT
**Source:** `outline-detailed.md` v1.0 (May 17, 2026)

---

## 5.1 Test Counts (Verifiable Target)

All test IDs use the `T-DA-NNN` prefix per Testing Strategy #19 §3.1.4.
Targets listed below are minimums; the full test suite at Stage 1 may exceed
these counts. Every FR from §2.1 must trace to at least one test (FR-to-test
mapping in §5.2).

| Category | Target count | Source sections |
|---|---|---|
| Unit — pool filter (FR-DA-009, FR-DA-010) | ≥ 6 | §3.2 |
| Unit — assignment algorithm (FR-DA-014–FR-DA-017) | ≥ 12 | §3.3–§3.5 |
| Unit — tackle intent (FR-DA-023) | ≥ 6 | §3.6 |
| Unit — offside trap (FR-DA-018, FR-DA-019) | ≥ 8 | §3.7 |
| Unit — last-man predicate (FR-DA-021, FR-DA-022) | ≥ 6 | §3.8 |
| Unit — COVER_GK_ZONE (KD-7, §3.9) | ≥ 4 | §3.9 |
| Unit — anti-chaos invariants (FR-DA-024–FR-DA-028) | ≥ 6 | §3.10 |
| Unit — hysteresis (FR-DA-015) | ≥ 4 | §3.11 |
| Unit — failure modes F1–F5 (FR-DA-029–FR-DA-033) | ≥ 5 | §2.4 |
| Integration — full-team scenarios | ≥ 12 | §3.13 |
| Determinism regression (binding to #16 §5) | ≥ 6 | §5.4 |
| Performance (binding to §6.3) | ≥ 3 | §5.5 |
| Anti-chaos invariant violation cascade | ≥ 3 | KD-17 / §3.10 |
| Exploit-resistance (KD-18 corpus) | ≥ 4 | KD-18 / Appendix E |
| **Total** | **≥ 85** | — |

---

## 5.2 Unit Test List

### Pool Filter Tests (FR-DA-009, FR-DA-010)

**T-DA-001 — GK excluded from pool unconditionally**
Input: 11-agent team; GK EntityId = 1. All agents HOLD_SHAPE in #13.
Expected: `holdShapePool` contains exactly 10 agents; EntityId 1 absent.
Validates: FR-DA-009.

**T-DA-002 — PRIMARY_PRESS excluded from pool**
Input: 11-agent team; 1 GK, 2 agents with `PressRole.PRIMARY_PRESS` in #13 output.
Expected: pool size = 8 (11 − 1 GK − 2 PRIMARY_PRESS).
Validates: FR-DA-010.

**T-DA-003 — COVER_SHADOW excluded from pool**
Input: 11-agent team; 1 GK, 3 agents with `PressRole.COVER_SHADOW`.
Expected: pool size = 7 (11 − 1 GK − 3 COVER_SHADOW).
Validates: FR-DA-010.

**T-DA-004 — All-ZONAL team: all non-GK outfield agents in pool**
Input: 11-agent team; 1 GK; #13 assigns HOLD_SHAPE to all 10 outfield agents.
Expected: pool size = 10; all 10 EntityIds present.
Validates: FR-DA-009, FR-DA-010.

**T-DA-005 — Empty pool handled gracefully**
Input: 11-agent team; 1 GK; #13 assigns PRIMARY_PRESS to all 10 outfield agents.
Expected: pool size = 0; no `MarkAssignment` records emitted; no panic or allocation.
Validates: FR-DA-009, FR-DA-010, F4 fallback path (§2.4).

**T-DA-006 — Pool iteration is EntityId-ascending**
Input: 11-agent team; pool agents have EntityIds {113, 105, 117, 109, 102}.
Expected: internal iteration order observed by `EvaluateCandidates` calls is
`[102, 105, 109, 113, 117]` (ascending).
Validates: FR-DA-003.

### Phase Gate Tests (FR-DA-013, KD-19)

**T-DA-007 — IN_POSSESSION produces all-ZONAL and returns early**
Input: #12 phase = `IN_POSSESSION` for this team; 7 HOLD_SHAPE agents.
Expected: all 7 `MarkAssignment.mode == ZONAL`; `directive.offsideTrapActive == false`;
`directive.emergencyFlag == false`; no candidate evaluation calls executed.
Validates: FR-DA-013.

**T-DA-008 — OUT_OF_POSSESSION proceeds to full algorithm**
Input: #12 phase = `OUT_OF_POSSESSION`; 7 HOLD_SHAPE agents; no special threats.
Expected: phase gate does NOT short-circuit; `EvaluateCandidates` called for pool agents.
Validates: FR-DA-013.

### Mark Assignment Algorithm Tests (FR-DA-014–FR-DA-017)

**T-DA-009 — Highest-threat candidate selected for MAN_MARK**
Input: agent at (30.0, 34.0) m; two opponents within `MAN_MARK_CANDIDATE_RADIUS_M = 15.0 m`:
opponent A at (25.0, 34.0), FirstTouch = 18;
opponent B at (28.0, 34.0), FirstTouch = 10.
Expected: `MarkAssignment.targetEntityId == opponent A` (higher threat score).
Validates: FR-DA-016.

**T-DA-010 — INTERCEPT_RUNNER preferred over MAN_MARK**
Input: opponent A within radius, speed = 1.0 m/s (below RUNNER_VELOCITY_THRESHOLD_M_S = 3.0);
opponent B within radius, speed = 4.5 m/s, direction toward own half.
Expected: `MarkAssignment.mode == INTERCEPT_RUNNER` targeting B.
Validates: FR-DA-016, FR-DA-017; §3.3.2 priority rule.

**T-DA-011 — No candidates within radius → ZONAL fallback**
Input: agent at (30.0, 34.0) m; all opponents at distance > 15.0 m; no INTERCEPT_RUNNER
qualifiers.
Expected: `MarkAssignment.mode == ZONAL`; `targetEntityId == null`;
`targetPosition == agent.formationSlot` from #12.
Validates: FR-DA-014.

**T-DA-012 — Displacement cost tie-break uses EntityId ascending**
Input: two opponents A (EntityId 201) and B (EntityId 202); same threat score (float-equal);
same displacement cost from the evaluating agent.
Expected: `MarkAssignment.targetEntityId == 201` (lower EntityId).
Validates: FR-DA-014.

**T-DA-013 — Threat score perceivedGoalProximity component computed correctly**
Input: team defending x = 0; opponent at (22.0, 34.0) m; FirstTouch = 16.
Expected: `perceivedGoalProximity = 1.0 − (22.0 / 105.0) = 0.7905`;
`opponentReceivingAttribute = (16 − 1) / 19.0 = 0.7895`;
`threat ≈ 0.624` (to 3 d.p.).
Validates: FR-DA-017; §3.5.4 worked example reproduces exactly.

**T-DA-014 — Threat score clamped to [0, 1]**
Input: opponent at x = 0.0 (goalmouth, defending x = 0); FirstTouch = 20.
Expected: `perceivedGoalProximity = 1.0`; `opponentReceivingAttribute = 1.0`;
`threat = 1.0` (no overflow).
Validates: FR-DA-017; §3.5.3 edge cases.

**T-DA-015 — Attribute minimum (FirstTouch = 1) produces zero contribution**
Input: opponent at (22.0, 34.0); FirstTouch = 1.
Expected: `opponentReceivingAttribute = 0.0`; `threat = 0.0`.
Validates: FR-DA-017; §3.5.3 edge cases.

**T-DA-016 — INTERCEPT_RUNNER: direction check toward own goal**
Input: team defending x = 0; opponent velocity = (−3.5, 0.0) m/s (running
toward x = 0, i.e., own half); speed 3.5 m/s > threshold 3.0.
Expected: opponent qualifies as INTERCEPT_RUNNER candidate.
Counter-input: same opponent with velocity = (+3.5, 0.0) m/s (running away from own goal).
Expected: does NOT qualify.
Validates: §3.3.3 dot-product direction check.

**T-DA-017 — INTERCEPT_RUNNER: velocity below threshold → no qualification**
Input: opponent speed = 2.9 m/s (< RUNNER_VELOCITY_THRESHOLD_M_S = 3.0);
direction toward own half.
Expected: opponent NOT an INTERCEPT_RUNNER candidate; MAN_MARK checked instead.
Validates: FR-DA-016; §3.3.3.

**T-DA-018 — Multiple INTERCEPT_RUNNER candidates: highest threat selected**
Input: two opponents both with speed > 3.0 m/s, both running toward own half.
Opponent A: higher threat score. Opponent B: lower threat score.
Expected: `MarkAssignment.targetEntityId == opponent A`.
Validates: §3.3.3 Step 4.

**T-DA-019 — Assignment skips overridden agents**
Input: agent already marked `overriddenThisTick = true` (set by §3.8 last-man logic).
Expected: `EvaluateCandidates` not called for that agent; assignment unchanged.
Validates: §3.3.3 Step 0 skip guard; §3.13 Step 5.

**T-DA-020 — Displacement cost formula numerically correct**
Input: agent at (30.0, 25.0) m; target at (40.0, 30.0) m.
Expected: `cost = 100.0 + 25.0 = 125.0 m²` (§3.4.3 worked example).
Validates: §3.4.

### Tackle Intent Tests (FR-DA-023)

**T-DA-021 — COMMIT when coverageDepth ≥ floor**
Input: agent at (18.0, 34.0) m; assigned opponent at (20.5, 34.0) m
(dist = 2.5 m < 3.0 m); coverageDepth = 2 ≥ 1 (floor).
Expected: `TackleIntentRequest.mode == COMMIT`.
Validates: FR-DA-023; §3.6.4 worked example.

**T-DA-022 — JOCKEY when coverageDepth < floor but approach angle below threshold**
Input: agent at (18.0, 34.0) m; opponent at (20.5, 34.0) m (dist = 2.5 m);
coverageDepth = 0; approach angle = 0.0 rad (< TACKLE_JOCKEY_ANGLE_RAD = 0.35 rad).
Expected: `TackleIntentRequest.mode == JOCKEY`.
Validates: §3.6.2.

**T-DA-023 — HOLD when coverageDepth < floor AND approach angle ≥ threshold**
Input: dist = 2.5 m; coverageDepth = 0; approach angle = 1.2 rad (>> 0.35 rad).
Expected: `TackleIntentRequest.mode == HOLD`.
Validates: §3.6.2.

**T-DA-024 — Agent beyond TACKLE_ELIGIBLE_RADIUS_M: no request emitted**
Input: assigned opponent at distance = 4.0 m > TACKLE_ELIGIBLE_RADIUS_M = 3.0 m.
Expected: no `TackleIntentRequest` emitted for this agent.
Validates: FR-DA-023; §3.6.2 eligibility gate.

**T-DA-025 — ZONAL agents produce no tackle request**
Input: agent assigned `MarkAssignment.mode == ZONAL` (no targetEntityId).
Expected: no `TackleIntentRequest` emitted regardless of proximity.
Validates: §3.6.2 null-target guard.

**T-DA-026 — Coverage depth counts only y-corridor teammates behind agent**
Input: agent A at (18.0, 34.0); team defending x = 0.
Teammate T1 at (12.0, 32.0): `|32.0 − 34.0| = 2.0 < 5.0` (corridor ✓),
`distToOwnGoal(T1) = 12.0 < 18.0` (✓) → counted.
Teammate T2 at (12.0, 44.0): `|44.0 − 34.0| = 10.0 > 5.0` (corridor ✗) → not counted.
Teammate T3 at (25.0, 34.0): `distToOwnGoal(T3) = 25.0 > 18.0` (T3 is not behind A) → not counted.
Expected: `coverageDepth = 1` (only T1 qualifies).
Validates: §3.6.2 coverage depth computation.

### Offside Trap Tests (FR-DA-018, FR-DA-019)

**T-DA-027 — Trap fires after OFFSIDE_TRAP_DWELL_TICKS consecutive qualifying ticks**
Input: three consecutive ticks with ball speed = 1.5 m/s < 4.0 (cond 1 ✓),
ball.x = 60.0 > 52.5 (cond 2 ✓), line spread = 2.1 m < 8.0 (cond 3 ✓),
no PRIMARY_PRESS (cond 4 ✓).
Expected: `dwellCounter` = 1, 2, 3 on ticks 1–3. After tick 3:
`directive.offsideTrapActive == true`; all DEFENSE-line agents receive ZONAL
assignment at `targetX = currentLineDepth + 3.0 m`.
Validates: FR-DA-018, FR-DA-019.

**T-DA-028 — Trap blocked during cooldown**
Input: trap fired on tick T (cooldown = 10 set). Ticks T+1..T+9: all four
trigger conditions hold.
Expected: `offsideState.cooldownTicksRemaining` decrements each tick; trap does
NOT fire again until `cooldownTicksRemaining == 0`.
Validates: §3.7.3 cooldown gate.

**T-DA-029 — Ball too fast: dwell counter resets**
Input: tick T: all conditions except ball speed (6.5 m/s > 4.0 m/s threshold).
Expected: `dwellCounter` resets to 0. Even if ticks T−2 and T−1 were qualifying,
the sequence breaks.
Validates: FR-DA-018; §3.7.3 reset-on-failure rule.

**T-DA-030 — Line not coherent: trap blocked**
Input: DEFENSE-line x-positions: {30.0, 32.0, 38.0, 41.0}; spread = 11.0 m
> LINE_COHERENCE_THRESHOLD_M = 8.0 m.
Expected: condition 3 fails; `dwellCounter = 0`.
Validates: §3.7.2 condition 3.

**T-DA-031 — Active PRIMARY_PRESS blocks trap**
Input: #13 reports 1 agent with PRIMARY_PRESS this tick.
Expected: condition 4 fails (`pressDir.primaryPressAgent != null`); trap blocked.
Validates: §3.7.2 condition 4.

**T-DA-032 — All DEFENSE-line agents advance simultaneously on step-up**
Input: 4 DEFENSE-line agents; trap fires (§3.7.4).
Expected: all 4 `MarkAssignment.targetPosition.x` set to the same `targetX`
in the same tick. Y-coordinates retain individual formationSlot values from #12.
Validates: FR-DA-019; §3.7.4.

**T-DA-033 — OFFSIDE_MAX_DEPTH_M safety ceiling enforced**
Input: `currentLineDepth = 44.0 m`; `OFFSIDE_STEP_SIZE_M = 3.0 m`.
`offsideStepDepth = 44.0 + 3.0 = 47.0 > OFFSIDE_MAX_DEPTH_M = 45.0`.
Expected: `directive.stepUpTargetDepth = 45.0 m` (ceiling applied).
Validates: §3.7.4.

**T-DA-034 — Step depth is max of (currentLineDepth + step, shape.DefensiveLineDepth)**
Input: `currentLineDepth = 35.0 m`; `OFFSIDE_STEP_SIZE_M = 3.0 m`;
`shape.DefensiveLineDepth = 40.0 m`.
`35.0 + 3.0 = 38.0 < 40.0` → `max(38.0, 40.0) = 40.0`.
Expected: `directive.stepUpTargetDepth = 40.0 m`.
Validates: §3.7.4.

### Last-Man Predicate Tests (FR-DA-021, FR-DA-022)

**T-DA-035 — Correct last-man agent identified (minimum distToOwnGoal)**
Input: team defending x = 0; pool distToOwnGoal values:
Agent 102 = 18.0 m, Agent 105 = 22.0 m, Agent 107 = 25.0 m.
Expected: `lastMan = Agent 102`.
Validates: FR-DA-021; §3.8.1.

**T-DA-036 — EntityId tie-break for equal distToOwnGoal**
Input: two agents both at distToOwnGoal = 18.0 m; EntityIds 5 and 12.
Expected: `lastMan = EntityId 5` (lower EntityId wins, FR-DA-033).
Validates: FR-DA-021, FR-DA-033.

**T-DA-037 — GK not selected as last-man even if forward of outfield defenders**
Input: GK at distToOwnGoal = 5.0 m; nearest outfield defender at distToOwnGoal = 20.0 m.
Expected: `lastMan = outfield defender at 20.0 m`, NOT the GK.
GK EntityId entirely absent from pool (FR-DA-009).
Validates: FR-DA-021; §3.8.1 GK exclusion note.

**T-DA-038 — Emergency flag set and INTERCEPT_RUNNER issued when predicate fires**
Input: lastMan at distToOwnGoal = 18.0 m; ball at distToOwnGoal = 12.0 m
(`12.0 < 18.0 + 5.0 = 23.0` ✓); `12.0 > LAST_MAN_OWN_HALF_MIN_X (5.0)` ✓.
Expected: `directive.emergencyFlag == true`; lastMan receives
`MarkAssignment.mode == INTERCEPT_RUNNER`.
Validates: FR-DA-022; §3.8.2.

**T-DA-039 — Predicate does NOT fire when ball not ahead of last man**
Input: lastMan at distToOwnGoal = 18.0 m; ball at distToOwnGoal = 30.0 m
(`30.0 < 18.0 + 5.0 = 23.0` → false).
Expected: `directive.emergencyFlag == false`; normal assignment loop runs.
Validates: §3.8.1 non-triggering case.

**T-DA-040 — Emergency override bypasses hysteresis**
Input: lastMan currently holding a MAN_MARK assignment with `dwellCounter = 3`
(dwell in progress). Emergency fires this tick.
Expected: `MarkAssignment.mode == INTERCEPT_RUNNER` immediately published;
`hysteresis[lastMan].dwellCounter == 0` (ResetHysteresis called).
Validates: §3.11.3 emergency bypass note.

### COVER_GK_ZONE Tests (KD-7, §3.9)

**T-DA-041 — COVER_GK_ZONE issued when GK out-of-zone AND emergency active**
Input: `directive.emergencyFlag == true`; GK at distToOwnGoal = 40.0 m
> `GK_EXPECTED_ZONE_MAX_X = 15.0 m`.
Expected: `COVER_GK_ZONE` assignment issued to the nearest HOLD_SHAPE agent
(excluding lastManAgent) to `abandonedZoneCenter`.
Validates: §3.9.1.

**T-DA-042 — COVER_GK_ZONE NOT issued when GK is in zone**
Input: `directive.emergencyFlag == true`; GK at distToOwnGoal = 8.0 m
≤ `GK_EXPECTED_ZONE_MAX_X = 15.0 m`.
Expected: no COVER_GK_ZONE assignment emitted.
Validates: §3.9.1 condition 2.

**T-DA-043 — COVER_GK_ZONE expires after COVER_GK_ZONE_MAX_TICKS**
Input: COVER_GK_ZONE override active for 20 consecutive ticks
(`COVER_GK_ZONE_MAX_TICKS = 20`); GK still out of zone on tick 21.
Expected: on tick 21, the coverAgent reverts to `ZONAL` (formationSlot from #12);
`coverGkZoneActiveTicks` resets to 0.
Validates: §3.9.2 safety release; §3.9.3.

**T-DA-044 — Minimum-displacement-cost cover agent selected**
Input: emergency active; GK out of zone (team defending x = 0; `abandonedZoneCenter = (7.5, 34.0)`).
Agent 105 at (22.0, 34.0): `cost = 210.25 m²`.
Agent 107 at (25.0, 38.0): `cost = 322.25 m²`.
(Matches §3.9.6 worked example.)
Expected: `coverAgent = Agent 105`.
Validates: §3.9.2.

### Anti-Chaos Invariant Tests (FR-DA-024–FR-DA-028)

**T-DA-045 — Invariant 2 violation: lowest-threat MAN_MARK demoted**
Input: 5 MAN_MARK assignments; `MAX_MAN_MARK_ASSIGNMENTS = 4`.
Threat scores: 0.62, 0.55, 0.71, 0.48 (lowest), 0.59.
Expected: agent with threat 0.48 demoted to ZONAL; final count = 4.
Validates: FR-DA-026, FR-DA-028; §3.10.5 worked example.

**T-DA-046 — Invariant 1 violation: DEFENSE-line backline restored**
Input: 2 DEFENSE-line agents in ZONAL (< MIN_BACKLINE_AGENTS = 3); 1 DEFENSE-line
agent in MAN_MARK with threat 0.40 (lowest among non-ZONAL DEFENSE agents).
Expected: that MAN_MARK agent demoted to ZONAL; backline in ZONAL = 3.
Validates: FR-DA-025, FR-DA-028; §3.10.2.

**T-DA-047 — Invariant 3 violation: over-displaced assignment demoted**
Input: agent's `MarkAssignment.targetPosition` at distance = 25.0 m from
`formationSlot` > `MAX_MARK_DISPLACEMENT_M = 20.0 m`.
Expected: that assignment demoted to ZONAL.
Validates: FR-DA-027, FR-DA-028; §3.10.2.

**T-DA-048 — All invariants satisfied → no demotions applied**
Input: 4 MAN_MARK assignments, 3 DEFENSE-line in ZONAL, all displacements ≤ 20.0 m.
Expected: loop exits at `break` after first pass; assignments unchanged.
Validates: FR-DA-024; §3.10.3.

**T-DA-049 — F4 hard fallback when invariants cannot be resolved in 3 passes**
Input: engineered input where 3-pass demotion loop exhausts without resolving.
(e.g., pool size = 2, MIN_BACKLINE_AGENTS = 3 — structurally impossible to satisfy.)
Expected: `EmitAllZonal` fires; `DEFENSIVE_AI_INVARIANT_FALLBACK` dev-log warning emitted.
Validates: FR-DA-032; §3.10.3 post-loop check.

**T-DA-050 — Interaction: MAX_MAN_MARK_ASSIGNMENTS should not exceed (poolSize − MIN_BACKLINE_AGENTS)**
Input: pool size = 10; MIN_BACKLINE_AGENTS = 3; MAX_MAN_MARK_ASSIGNMENTS = 4.
All 7 non-backline agents initially MAN_MARK; 4 demotions needed to reach 4.
Expected: after enforcement pass, exactly 4 MAN_MARK + ≥ 3 DEFENSE-ZONAL.
Validates: Appendix D sensitivity interaction rule.

### Hysteresis Tests (FR-DA-015)

**T-DA-051 — Assignment held for MARK_DWELL_TICKS before transition commits**
Input: agent currently MAN_MARK → opponent 201; new top candidate = opponent 202
for ticks T, T+1, T+2, T+3 (4 consecutive ticks = MARK_DWELL_TICKS).
Expected: assignment stays on 201 for ticks T..T+2; commits to 202 on tick T+3.
Validates: FR-DA-015; §3.11.3.

**T-DA-052 — Oscillating candidate does not trigger transition**
Input: agent MAN_MARK → 201; candidates alternate 201/202 each tick.
Expected: `holdTicks` never reaches 4; assignment stays on 201 indefinitely.
Validates: FR-DA-015; §3.11.6 thrash-prevention worked example.

**T-DA-053 — New candidate resets holdTicks accumulator**
Input: agent accumulating toward candidate 202 (holdTicks = 2); candidate
changes to 203 this tick.
Expected: `holdTicks` resets to 1 (start of accumulation for 203); no commit to 202.
Validates: §3.11.3 "New candidate: reset dwell accumulator" branch.

**T-DA-054 — ZONAL agents not subject to dwellCounter pre-check**
Input: agent in ZONAL mode with `dwellCounter = 3`.
Expected: assignment IS re-evaluated (§3.3 pre-check only retains non-ZONAL
assignments with `dwellCounter > 0`).
Validates: §3.11.3 pre-check condition.

### Failure Mode Tests (FR-DA-029–FR-DA-033)

**T-DA-055 — F1: Stale perception snapshot → previous tick assignment reused**
Input: `snapshot.tickIndex = T − 2 < currentTick = T`.
Expected: previous tick's full `MarkAssignment[]` reused verbatim; algorithm
not invoked; dev-log warning emitted with delta = 2.
Validates: FR-DA-029; §2.4 F1.

**T-DA-056 — F2: SENTINEL_NO_SLOT → all-ZONAL fallback**
Input: `BaselineDefensiveShapeView.GetSlot(agentId) == SENTINEL_NO_SLOT` for one agent.
Expected: all agents emit ZONAL immediately; no partial assignment; no panic.
Validates: FR-DA-030; §2.4 F2.

**T-DA-057 — F3: #13 directive absent → all-outfield-non-GK treated as HOLD_SHAPE**
Input: `PressAssignment` array for this team has `tickIndex = T − 2`.
Expected: all non-GK outfield agents (10 maximum) treated as HOLD_SHAPE;
dev-log warning `PRESS_DIRECTIVE_ABSENT` emitted.
Validates: FR-DA-031; §2.4 F3.

**T-DA-058 — F4: invariant violation after max passes → all-ZONAL (see T-DA-049)**
(Cross-reference; see T-DA-049 above for the full F4 test case.)
Validates: FR-DA-032; §2.4 F4.

**T-DA-059 — F5: last-man tie resolved by EntityId ascending (see T-DA-036)**
(Cross-reference; see T-DA-036 above for the EntityId tie-break test.)
Validates: FR-DA-033; §2.4 F5.

---

## 5.3 Integration Test List

Integration tests use the full §3.13 pseudocode pipeline across multiple agents
and multiple ticks. State (MarkHysteresisState[], OffsideLineState) persists
across ticks within each test scenario.

**T-DA-060 — Full-team press + defend scenario: disjoint partition maintained**
Setup: 11-agent team (1 GK, 3 PRIMARY_PRESS, 2 COVER_SHADOW, 5 HOLD_SHAPE).
Expected: pool = 5 agents; #13 agents receive no `MarkAssignment`; #14 agents
receive no `PressAssignment`. Zero overlap at every tick across 10 simulated ticks.
Validates: FR-DA-010; KD-4.

**T-DA-061 — Phase transition: IN_POSSESSION → OUT_OF_POSSESSION → IN_POSSESSION**
Setup: 3 ticks.
Tick 1: phase = IN_POSSESSION → all-ZONAL emitted.
Tick 2: phase = OUT_OF_POSSESSION → full algorithm runs; MAN_MARK assignments emerge.
Tick 3: phase = IN_POSSESSION → all-ZONAL emitted again.
Expected: hysteresis state preserved across tick 2 but phase gate suppresses use
on tick 3. No assignment data from tick 1 leaks into tick 2.
Validates: FR-DA-013; §3.1.

**T-DA-062 — Possession turnover in TRANSITION phase**
Setup: TRANSITION phase (defensive side); opponents break fast; one qualifies as
INTERCEPT_RUNNER.
Expected: INTERCEPT_RUNNER assignment issued within one tick of transition
detection; no latency from hysteresis (mode change from ZONAL → INTERCEPT_RUNNER
immediate on first qualifying tick, holdTicks = 1; commits on tick 4).
Validates: §3.1.3 TRANSITION fallback; §3.3.2.

**T-DA-063 — Offside trap: 4-defender backline advances simultaneously**
Setup: 4-man DEFENSE-line at x ≈ 35.0 m; 3 consecutive qualifying ticks.
Expected: all 4 defenders assigned `targetPosition.x = offsideStepDepth` on
the same tick; Y-components remain individual; `directive.offsideTrapActive == true`.
Validates: FR-DA-019; §3.7.4.

**T-DA-064 — Emergency scenario: last-man threatened + GK out of position**
Setup: ball at x = 10.0 m; lastMan at x = 18.0 m; GK advanced to x = 40.0 m.
Expected: On same tick:
(a) lastMan → `INTERCEPT_RUNNER`; `directive.emergencyFlag == true`.
(b) coverAgent (nearest non-lastMan DEFENSE agent) → `COVER_GK_ZONE` at (7.5, 34.0).
Both overrides issued in the same tick (Steps 4 and 4a of §3.13).
Validates: FR-DA-022; §3.9.1.

**T-DA-065 — Anti-chaos cascade: MAX_MAN_MARK_ASSIGNMENTS exceeded; resolves within 3 passes**
Setup: 6 agents all qualify for MAN_MARK; `MAX_MAN_MARK_ASSIGNMENTS = 4`.
Expected: demotion cascade reduces MAN_MARK count to 4 within pass 1 or 2;
all three invariants satisfied before publication.
Validates: FR-DA-028; KD-17; §3.10.4.

**T-DA-066 — Determinism: same input seed → same output on consecutive runs**
Setup: fixed perception snapshot, fixed #12 baseline, fixed #13 press directive.
Run pipeline twice with same inputs.
Expected: all `MarkAssignment[]`, `MarkDirective`, and `TackleIntentRequest[]`
bit-identical across both runs.
Validates: FR-DA-003; §4.6.

**T-DA-067 — Assign-then-suppress: opponent leaves radius mid-sequence**
Setup: tick 1: opponent within `MAN_MARK_CANDIDATE_RADIUS_M`; agent assigned MAN_MARK;
dwellCounter locked at 4.
Ticks 2–4: opponent exits radius (distance > 15.0 m); agent retains MAN_MARK
for remaining dwell.
Tick 5: dwellCounter = 0; re-evaluation; no candidates within radius → ZONAL.
Expected: ZONAL assignment starts on tick 5, not tick 2 (dwell lock respected).
Validates: FR-DA-015; §3.11.3 pre-check.

**T-DA-068 — GK re-entry to zone: COVER_GK_ZONE released**
Setup: GK out of zone for 3 ticks (COVER_GK_ZONE active); GK returns to
distToOwnGoal = 8.0 m on tick 4.
Expected: tick 4 `offsideState.coverGkZoneActiveTicks` resets to 0; cover
agent reverts to ZONAL; no further COVER_GK_ZONE assignments.
Validates: §3.9.3.

**T-DA-069 — Post-trap cooldown: dwell counter cannot accumulate during cooldown**
Setup: trap fired on tick T (`cooldownTicksRemaining = 10`).
Ticks T+1..T+9: all trigger conditions hold.
Expected: `dwellCounter` may increment but trap blocked by `cooldownTicksRemaining > 0`.
Trap re-fires on earliest eligible tick (after cooldown reaches 0).
Validates: §3.7.3.

**T-DA-070 — Full 90-minute match: pool sizes always consistent with #13 role partition**
Setup: simulated match trace; random press-role assignments per tick.
Expected: at every tick: `|PRIMARY_PRESS pool| + |COVER_SHADOW pool| + |HOLD_SHAPE pool| == 10`
(for a full 11-player team). GK never in any pool.
Validates: FR-DA-009, FR-DA-010; KD-4.

**T-DA-071 — #12 phase change latency test**
Setup: phase changes on tick T; #14 reads the updated phase on tick T.
Expected: ZONAL/full-algorithm switch takes effect immediately (no 1-tick lag).
Validates: FR-DA-013; §4.4.2 "read once at tick start."

---

## 5.4 Determinism Regression (Binding to #16 §5)

All determinism tests use the reference host configuration (Ryzen 7 5800X
@ 4.5 GHz, Mono backend, Unity 2022.3 LTS) per §6.3. Digest format follows
#16 §6.2. Each test verifies bit-identical outputs across two independent runs
with the same initial state.

**T-DA-DET-001 — 90-minute replay: bit-identical per-tick digest**
Procedure: simulate a full 90-minute match (54,000 ticks at 10 Hz);
record the per-tick determinism digest (MarkDirective × 2 teams +
MarkAssignment[] × 22 slots + MarkHysteresisState[] × 22 slots +
OffsideLineState × 2 teams + TackleIntentRequest[] × up to 20 slots).
Repeat on the same host from the same initial state.
Pass criterion: all 54,000 per-tick digests bit-identical.

**T-DA-DET-002 — Two independent simulations with identical initial state**
Procedure: two fresh simulation instances with identical initial state (same
formation, same attributes, same kick-off seed).
Pass criterion: the first 1,000 ticks produce identical MarkDirective /
MarkAssignment / TackleIntentRequest sequences.

**T-DA-DET-003 — Mid-match snapshot resume: digest continues identically**
Procedure: simulate to tick 5,000; save snapshot; resume; continue to tick 6,000.
Pass criterion: ticks 5,001–6,000 produce bit-identical digests vs. a
continuous run with no snapshot.

**T-DA-DET-004 — EntityId iteration order determinism**
Procedure: pool of 5 agents with EntityIds {113, 105, 117, 109, 102}.
Swap two EntityIds in the input; observe output permutation.
Pass criterion: output permutes predictably (same EntityId at same pool index
produces same assignment); no assignments affected by non-corresponding agent inputs.

**T-DA-DET-005 — RNG reproducibility via DOMAIN_TAG_DEFENSIVE_AI seed**
Procedure: force two candidates to produce identical threat score and cost
(RNG tie-break required); execute with same DOMAIN_TAG_DEFENSIVE_AI seed.
Pass criterion: same tie-break result on both runs.
Note: `DOMAIN_TAG_DEFENSIVE_AI = 0x1A [CROSS: #16 §3.4]` — ERR-014-004 resolved May 18, 2026.

**T-DA-DET-006 — Anti-chaos pass terminates in ≤ 3 passes for all inputs**
Procedure: generate 1,000 random initial assignment states; run EnforceAntiChaosInvariants.
Pass criterion: all runs terminate in ≤ 3 passes or trigger the F4 all-ZONAL fallback.
No infinite loop. Total iterations ≤ 3 × pool size for any input.

---

## 5.5 Performance Validation (Binding to §6.3)

**T-DA-PERF-001 — Worst-case per-tick execution ≤ 0.12 ms**
Procedure: 10 HOLD_SHAPE agents × 10 candidate opponents; all INTERCEPT_RUNNER
eligible (maximum candidate evaluation branches). Measured on reference host
(§6.3) using Unity's `Stopwatch`-based micro-benchmark (100 repetitions,
discard top 5% outliers).
Pass criterion: mean execution time ≤ 0.12 ms per tick.

**T-DA-PERF-002 — Zero heap allocation per tick**
Procedure: attach Unity Memory Profiler with Allocation Tracking enabled.
Run 10 consecutive ticks of the defensive AI pipeline on a full team.
Pass criterion: zero `GC.Alloc` events originating from `src/DefensiveAI/`.
Validates: FR-DA-006; #18 §3.7.

**T-DA-PERF-003 — Anti-chaos worst-case within budget**
Procedure: force invariant 2 violation requiring 3 full passes (7 agents
MAN_MARK; need 3 demotions to reach MAX_MAN_MARK_ASSIGNMENTS = 4).
Measure execution time of `EnforceAntiChaosInvariants` call alone.
Pass criterion: ≤ 0.02 ms (included in T-DA-PERF-001's 0.12 ms budget).

---

## 5.6 Anti-Chaos and Exploit-Resistance (KD-17, KD-18)

### 5.6.1 Anti-Chaos Invariant Violation Cascade

**T-DA-INV-001 — MIN_BACKLINE_AGENTS violated → demotion restores backline**
Input: 2 DEFENSE-line agents in ZONAL (below MIN_BACKLINE_AGENTS = 3).
Expected: one non-ZONAL DEFENSE-line agent demoted (lowest threat score);
backline in ZONAL count becomes 3. Invariant satisfied in ≤ 2 passes.
Validates: FR-DA-025, FR-DA-028; §3.10.2 invariant 1.

**T-DA-INV-002 — MAX_MAN_MARK_ASSIGNMENTS exceeded → lowest-threat demoted**
Input: 5 MAN_MARK assignments (> MAX_MAN_MARK_ASSIGNMENTS = 4).
Expected: agent with lowest threat score demoted to ZONAL; count = 4.
Validates: FR-DA-026, FR-DA-028; §3.10.2 invariant 2.

**T-DA-INV-003 — MAX_MARK_DISPLACEMENT_M exceeded → over-displaced demoted**
Input: agent's mark target requires displacement = 25.0 m (> MAX_MARK_DISPLACEMENT_M = 20.0 m).
Expected: that assignment demoted to ZONAL; agent reverts to formationSlot.
Validates: FR-DA-027, FR-DA-028; §3.10.2 invariant 3.

### 5.6.2 Exploit-Resistance Corpus (KD-18)

**T-DA-EXP-001 — EXPLOIT_OFFSIDE_TRAP_SPRUNG_EARLY**
Scenario: striker at x = 54.0 m starts a forward run during `dwellCounter = 2`
(OFFSIDE_TRAP_DWELL_TICKS = 3; needs 1 more qualifying tick).
Expected:
- Tick T (dwellCounter = 2): trap does NOT fire.
- The running striker qualifies as INTERCEPT_RUNNER (speed > 3.0 m/s, heading
  toward own goal) and is covered by an eligible HOLD_SHAPE agent.
- Tick T+1 (dwellCounter = 3, cooldown = 0): trap fires. Step-up occurs.
Pass criterion: step-up does NOT happen until dwellCounter reaches
`OFFSIDE_TRAP_DWELL_TICKS = 3`. Early-run exploitation does not prematurely
compress the defensive line while the runner is already through.
Validates: §3.7.3; FR-DA-018.

**T-DA-EXP-002 — EXPLOIT_SWITCH_THROUGH_HOLE**
Scenario: ball switched from left flank to an unguarded right channel at tick T.
Initial state: right-channel HOLD_SHAPE agent in ZONAL at #12 baseline.
Ball switch lands in a zone with no current MAN_MARK assignment.
Expected:
- Tick T: assignment algorithm detects the nearest opponent in right channel
  (within `MAN_MARK_CANDIDATE_RADIUS_M = 15.0 m`); assigns MAN_MARK or INTERCEPT_RUNNER.
- HoldTicks for new candidate starts at 1 (tick T).
- By tick T + REASSIGN_LATENCY_TICKS (T+2): assignment commits (within hysteresis window
  if `MARK_DWELL_TICKS = 4`). Note: the cover agent moves toward the channel
  on tick T (ZONAL targetPosition updates immediately even before hysteresis commits
  the mode-change); opponent tracking lags by up to MARK_DWELL_TICKS = 4 ticks.
Pass criterion: within `REASSIGN_LATENCY_TICKS = 2` ticks the right-channel
HOLD_SHAPE agent has updated its `targetPosition` toward the ball channel.
The `REASSIGN_LATENCY_TICKS = 2 [GT]` constant governs the position update;
hysteresis governs the mode-label commit (up to 4 ticks).
Validates: KD-18; §3.3; Appendix E.2.

**T-DA-EXP-003 — EXPLOIT_LAST_MAN_ONE_ON_ONE**
Scenario: attacker at x = 22.0 m; lastMan at x = 20.0 m (distToOwnGoal 20.0);
ball at x = 19.0 m. No teammates at distToOwnGoal < 20.0 m → `coverageDepth = 0`.
Expected:
- `IsLastManThreat = true` (19.0 < 20.0 + 5.0 = 25.0; 19.0 > 5.0).
- `directive.emergencyFlag = true`.
- lastMan → `INTERCEPT_RUNNER`.
- §3.6 tackle intent: `coverageDepth = 0 < TACKLE_COMMIT_COVERAGE_FLOOR = 1`
  AND last-man special-case override (§3.6.2) forces JOCKEY or HOLD; never COMMIT.
Pass criterion: `TackleIntentRequest.mode != COMMIT` for the last-man agent.
Validates: §3.8.2 Step 4; §3.6.2 last-man special case; KD-18.

**T-DA-EXP-004 — EXPLOIT_GK_OUT_OF_POSITION**
Scenario: GK advanced to x = 20.0 m (distToOwnGoal = 20.0 > GK_EXPECTED_ZONE_MAX_X = 15.0);
ball at x = 8.0 m; lastMan threat active.
Expected:
- `IsLastManThreat = true` → `emergencyFlag = true`.
- `distToOwnGoal(gkPos) = 20.0 > 15.0` → COVER_GK_ZONE condition 2 holds.
- COVER_GK_ZONE override issued to nearest backline defender
  within one tick (same tick as the GK out-of-zone condition is first detected).
- `abandonedZoneCenter = (7.5, 34.0)` for team defending x = 0.
Pass criterion: COVER_GK_ZONE assignment issued in the tick when GK out-of-zone
is first detected. No latency gap.
Validates: §3.9.2; KD-18; Appendix E.4.

---

## 5.7 xG-Surrogate Validation (Stage 0 — resolves outline.md L-13)

Full xG modelling is a Stage 1+ deliverable per Shot Mechanics #6 §7. At Stage 0
the following surrogate metrics are declared so that they are unambiguously
computable from the simulation trace at Stage 1 implementation.

### 5.7.1 Metric 1 — Shots-in-Box Conceded per Match

**Definition:** Count of ball-agent contact events (per Collision System #3) where:
- `ball.position` at contact is within the opponent penalty area of the team under evaluation.
- Penalty area bounds (per Ball Physics #1 §1.2 coordinate convention):
  - Team defending x = 0: x ∈ [0.0, 16.5] m; y ∈ [13.85, 54.15] m.
  - Team defending x = 105: x ∈ [88.5, 105.0] m; y ∈ [13.85, 54.15] m.

**Purpose:** a lower count indicates the defensive AI is successfully keeping
the ball out of the box. Absolute threshold values are not specified at Stage 0;
a reference value is established in the first simulated match and used as a
regression baseline.

**Measurement hook:** #3 contact events already carry `ball.position` at contact
(Ball Physics #1 §3.1 ball-state schema). The defensive AI unit does not need
to produce this metric directly; it is read from the simulation trace.

### 5.7.2 Metric 2 — Average Shot Distance from Own Goal

**Definition:** Euclidean distance from `ball.position` at shot-contact to the
own goal centre at tick of contact.

```
ownGoalCenter = (0.0, 34.0)  for team defending x = 0
              = (105.0, 34.0) for team defending x = 105

shotDistance = sqrt((ball.x − ownGoalCenter.x)² + (ball.y − ownGoalCenter.y)²)
```

**Purpose:** a higher average shot distance indicates shots are forced from
wider, less dangerous positions. Stage 0 baseline established from first match trace.

**Measurement hook:** same #3 contact-event trace as Metric 1.

### 5.7.3 Acceptance Criterion

Both metrics must be extractable from the simulation trace at Stage 1 delivery
of Defensive AI #14 runtime. The metric definitions above must be unambiguous to
any engineer reading this spec (no reference to undefined constants or unspecified
systems). This criterion is verifiable without a full xG model.

---

## 5.8 FR-to-Test Traceability Matrix

| FR | Test IDs |
|---|---|
| FR-DA-001 | T-DA-008, T-DA-060, T-DA-DET-001 |
| FR-DA-002 | T-DA-009, T-DA-060 |
| FR-DA-003 | T-DA-006, T-DA-DET-004 |
| FR-DA-004 | T-DA-DET-001, T-DA-DET-002 |
| FR-DA-005 | T-DA-DET-005 |
| FR-DA-006 | T-DA-PERF-002 |
| FR-DA-007 | §4.7 check 9 (build-time lint) |
| FR-DA-008 | §4.7 check 3 |
| FR-DA-009 | T-DA-001, T-DA-037, T-DA-070 |
| FR-DA-010 | T-DA-002, T-DA-003, T-DA-060, T-DA-070 |
| FR-DA-011 | T-DA-041 (four modes enumerated) |
| FR-DA-012 | §4.7 check implicitly |
| FR-DA-013 | T-DA-007, T-DA-008, T-DA-061 |
| FR-DA-014 | T-DA-011, T-DA-012 |
| FR-DA-015 | T-DA-051, T-DA-052, T-DA-053, T-DA-054 |
| FR-DA-016 | T-DA-009, T-DA-010, T-DA-017, T-DA-018 |
| FR-DA-017 | T-DA-013, T-DA-014, T-DA-015 |
| FR-DA-018 | T-DA-027, T-DA-029, T-DA-030, T-DA-031, T-DA-EXP-001 |
| FR-DA-019 | T-DA-032, T-DA-063 |
| FR-DA-020 | §4.7 check 5 (grep lint) |
| FR-DA-021 | T-DA-035, T-DA-036, T-DA-037 |
| FR-DA-022 | T-DA-038, T-DA-064 |
| FR-DA-023 | T-DA-021, T-DA-022, T-DA-023, T-DA-024, T-DA-025, T-DA-026 |
| FR-DA-024 | T-DA-048, T-DA-049 |
| FR-DA-025 | T-DA-046, T-DA-INV-001 |
| FR-DA-026 | T-DA-045, T-DA-INV-002 |
| FR-DA-027 | T-DA-047, T-DA-INV-003 |
| FR-DA-028 | T-DA-045, T-DA-046, T-DA-047, T-DA-065 |
| FR-DA-029 | T-DA-055 |
| FR-DA-030 | T-DA-056 |
| FR-DA-031 | T-DA-057 |
| FR-DA-032 | T-DA-049, T-DA-058 |
| FR-DA-033 | T-DA-036, T-DA-059 |
| FR-DA-034 | All §3 sections have worked examples; verified at section-file review |
| FR-DA-035 | §6.1 catalogue; Appendix A derivations |
| FR-DA-036 | §4.7 check 6 (grep lint) |
| FR-DA-037 | §7.1 gate conditions; §9.3(a)(b)(e) |

---

## 5.9 Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 17, 2026 | AI agent | Initial draft. 71 named unit and integration tests (T-DA-001..T-DA-071 contiguous). 6 determinism regression tests (T-DA-DET-001..006). 3 performance tests (T-DA-PERF-001..003). 3 anti-chaos invariant tests (T-DA-INV-001..003). 4 exploit-resistance tests (T-DA-EXP-001..004). xG surrogate metrics defined (§5.7). FR-to-test traceability matrix (§5.8; all 37 FRs mapped). Total named tests: ≥ 85 targets met (79 named + 6 determinism + FR-map coverage). KD-18 exploit corpus (Appendix E) referenced. |
| 0.2 | May 17, 2026 | AI agent | PASS-1 adversarial review fix pass. L1: §5.9 v0.1 row corrected test count from "59 named" to "71 named" (T-DA-001..T-DA-071 = 71 tests, not 59). |
