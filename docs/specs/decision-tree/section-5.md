# Decision Tree Specification #8 — Section 5: Test Plan

**File:** `section-5.md`  
**Purpose:** Define unit, integration, balance, and performance validation strategy for Decision Tree Specification #8.  
**Created:** April 20, 2026  
**Version:** 1.0  
**Status:** DRAFT — Awaiting Lead Developer Review  
**Specification Number:** 8 of 20 (Stage 0 — Physics Foundation)  
**Author:** Claude (AI) with Anton (Lead Developer)

---

## Table of Contents

- [5.1 Test Strategy](#51-test-strategy)
- [5.2 Unit Tests — Option Generation](#52-unit-tests--option-generation)
- [5.3 Unit Tests — Utility Scoring](#53-unit-tests--utility-scoring)
- [5.4 Unit Tests — Action Selection](#54-unit-tests--action-selection)
- [5.5 Unit Tests — Dispatch Population](#55-unit-tests--dispatch-population)
- [5.6 Integration Tests](#56-integration-tests)
- [5.7 Balance Validation Scenarios](#57-balance-validation-scenarios)
- [5.8 Performance and Determinism Tests](#58-performance-and-determinism-tests)
- [5.9 Test Data and Traceability](#59-test-data-and-traceability)
- [5.10 Version History](#510-version-history)

---

## 5.1 Test Strategy

Decision Tree validation is split into four layers:
1. Unit tests for deterministic formulas and gating logic.
2. Integration tests across Perception → Decision Tree → execution requests.
3. Balance tests verifying realistic tactical outcomes.
4. Performance and determinism tests at 22-agent heartbeat scale.

**Minimum quality bar:** test vectors must be derived from Section 3 formulas; no fabricated expected values.

---

## 5.2 Unit Tests — Option Generation

Coverage target: all seven action generators and generation invariants (INV-GEN-01..10).

| Test ID | Scenario | Expected Result |
|---|---|---|
| UT-OG-01 | possession branch | PASS/SHOOT/DRIBBLE/HOLD generated only |
| UT-OG-02 | non-possession branch | MOVE/PRESS/INTERCEPT generated only |
| UT-OG-03 | no visible teammates | PASS count = 0, HOLD still present |
| UT-OG-04 | stale ball snapshot | INTERCEPT rejected when `BallStalenessFrames > 0` |
| UT-OG-05 | Decisions low/high | PASS candidate cap monotonic with Decisions |
| UT-OG-06 | pitch bounds | all `TargetPosition` within [0,105]m × [0,68]m |

---

## 5.3 Unit Tests — Utility Scoring

Coverage target: §3.2 formulas + §3.4 tactical modifiers.

| Test ID | Scenario | Expected Result |
|---|---|---|
| UT-US-01 | PASS formula baseline | score matches worked example within tolerance |
| UT-US-02 | SHOOT attacking-zone boost | higher score in ATTACKING vs MIDFIELD at same inputs |
| UT-US-03 | PRESS under HIGH pressing | multiplier applied as defined in §3.4.2 |
| UT-US-04 | HOLD pressure behavior | increasing P lowers HOLD utility per coefficients |
| UT-US-05 | risk penalty boundaries | risk term remains in [0.0, 1.0] |

---

## 5.4 Unit Tests — Action Selection

Coverage target: §3.3 noise model, deterministic tie rules, state-safe outputs.

| Test ID | Scenario | Expected Result |
|---|---|---|
| UT-AS-01 | identical seed/input replay | byte-identical selected `AgentAction` |
| UT-AS-02 | Composure=1 vs Composure=20 | low composure shows higher selection variance |
| UT-AS-03 | equal utilities | deterministic enum-ordinal tiebreak |
| UT-AS-04 | same action-type candidates | same noise sample for same type as defined in §3.3 |

---

## 5.5 Unit Tests — Dispatch Population

Coverage target: §3.5 field mapping correctness.

| Test ID | Dispatch Type | Verification |
|---|---|---|
| UT-DP-01 | PASS | required `PassRequest` fields populated and in valid ranges |
| UT-DP-02 | SHOOT | `PowerIntent`, `SpinIntent`, `PlacementTarget` range checks |
| UT-DP-03 | MOVE_TO_POSITION | pace threshold mapping (`MOVE_SPRINT_THRESHOLD`, `MOVE_JOG_THRESHOLD`) |
| UT-DP-04 | PRESS | target lock + emergency deceleration set correctly |
| UT-DP-05 | INTERCEPT | intercept point passed unchanged from payload |

---

## 5.6 Integration Tests

| Test ID | Scenario | Success Criteria |
|---|---|---|
| IT-DT-01 | full 22-agent heartbeat | 22 actions dispatched, no duplicates, no missing actions |
| IT-DT-02 | possession transition | action set shifts correctly next tick after possession change |
| IT-DT-03 | forced refresh mid-execution | interrupt behavior follows §3.6.3 / §3.7 rules |
| IT-DT-04 | pass chain | DT pass dispatch aligns with Pass Mechanics request acceptance |
| IT-DT-05 | clear shooting lane | SHOOT selected in high-quality box scenario |
| IT-DT-06 | no-ball visibility | repositioning/pressing behaviors dominate |

---

## 5.7 Balance Validation Scenarios

Balance tests ensure gameplay believability, not just formula correctness.

| Balance ID | Scenario | Acceptance Gate |
|---|---|---|
| BAL-DT-01 | high finishing striker in box | SHOOT selected in ≥80% qualifying ticks |
| BAL-DT-02 | low composure under pressure | suboptimal choice rate measurably above elite composure |
| BAL-DT-03 | HIGH pressing instruction | PRESS/INTERCEPT share rises vs MEDIUM baseline |
| BAL-DT-04 | SHORT passing instruction | short PASS candidates selected more often than long PASS |

---

## 5.8 Performance and Determinism Tests

### 5.8.1 Per-heartbeat budget

- Heartbeat duration: 100 ms (10 Hz).
- Decision Tree all-agent budget: **4.0 ms** [GT].
- Per-agent budget target: `4.0 ms / 22 = 0.182 ms` [DERIVED].

**Worked example:**
- If measured batch time is 3.3 ms for 22 agents, per-agent average is `3.3 / 22 = 0.150 ms`, which passes the 0.182 ms target.

### 5.8.2 Determinism replay test

Run identical 90-minute simulation inputs twice with same seed and assert byte-identical action stream hashes.

---

## 5.9 Test Data and Traceability

- Every unit test references the exact source formula section ID.
- Every integration test references FR IDs from Section 2.
- Every balance test references one or more constants tagged [GT] for tuning traceability.
- Approval checklist (§9) includes a traceability gate requiring all FRs to map to at least one test case.

---

## 5.10 Version History

| Version | Date | Author | Changes |
|---|---|---|---|
| 1.0 | April 20, 2026 | Claude (AI) / Anton | Initial draft of Section 5 test plan with unit/integration/balance/performance strategy. |

---

*End of Section 5 — Decision Tree Specification #8*  
*Tactical Director — Specification #8 of 20 | Stage 0: Physics Foundation*
