# Pressing AI Specification #13 — Section 5: Test Plan

**Created:** May 17, 2026
**Last Updated:** May 17, 2026 (v0.2 PASS-1 adversarial-review fix pass)
**Version:** 0.2
**Status:** DRAFT
**Source:** `outline-detailed.md` v1.0

---

The test plan binds to Testing Strategy #19 §3 (test taxonomy) and
§4 (FR traceability framework). All implementation lands at
Stage 1 alongside the runtime per KD-12; the plan itself is a
Stage 0 deliverable.

**#19 prefix conformance (AR-S1-L4 — pending).** Prefixes
`T-U-`, `T-I-`, `T-D-`, `T-P-` are expected to match the #19 §3
canonical taxonomy. `T-C-` (anti-chaos invariant) and `T-X-`
(exploit-resistance) are spec-local inventions; a grep of
`testing-strategy/section-3.md` must confirm whether these are
enumerated or whether a one-line back-prop into #19 §3 is required.
This verification is captured as a §9.3 precondition (see §9.3 (h)).

## 5.1 Test Counts

| Category | Target | Source |
|---|---|---|
| Unit (trigger detection, debounce, selection, lane geometry, hysteresis, stamina, disengage) | ≥40 | §3.1–§3.8 |
| Integration (full-team press under each trigger × each phase) | ≥10 | §3.11 |
| Determinism regression | ≥6 | #16 §5 |
| Performance | ≥3 | §6 |
| Anti-chaos invariant tests | ≥6 | KD-16 |
| Exploit-resistance (KD-17 corpus) | ≥4 | KD-17 |
| **Total** | **≥69** | — |

## 5.2 Unit Test List (representative)

### 5.2.1 Triggers (§3.1)

- **T-U-001** `BAD_TOUCH` fires when `q = 0.30, postTouchVelocity =
  6.0 m/s`; does NOT fire when `q = 0.50`.
- **T-U-002** `BACKWARD_PASS` worked example: kick velocity
  `(−6, 3, 0)`, attackingDirection `(+1, 0)` → fires
  (dot = −0.894).
- **T-U-003** `SIDELINE_TRAP` worked example: ball `(45, 5.0)`,
  carrier facing `(0.5, −0.87)` → fires.
- **T-U-004** `WEAK_RECEIVER` excludes opposing GK from candidate
  set (KD-13).
- **T-U-005** Trigger origin EntityId is the ball-carrier for
  `BAD_TOUCH` / `SIDELINE_TRAP`; the receiver for `BACKWARD_PASS`
  / `WEAK_RECEIVER`.

### 5.2.2 Debounce (§3.2)

- **T-U-010** Each trigger holds for `TRIGGER_DWELL_TICKS = 2`
  ticks before firing.
- **T-U-011** Release: `TRIGGER_RELEASE_TICKS = 3` ticks of cleared
  raw condition before the committed flag clears.
- **T-U-012** Asymmetric release: a single tick of cleared raw
  condition does NOT clear a committed flag (release counter
  resets on raw-true).

### 5.2.3 Primary-Press Selection (§3.3)

- **T-U-020** Worked-example arithmetic: ball-carrier `(40, 30)`
  with velocity `(3, 0)`, A=`(38, 31)`, B=`(42, 32)` → B wins
  (cost 5.21 < 9.41).
- **T-U-021** Ineligible-stamina agents are excluded (fatigue ≥
  `PRESS_FATIGUE_CEILING`).
- **T-U-022** GK never selected (FR-PR-017).
- **T-U-023** EntityId terminal tie-break when costs are within
  `SPACING_EPSILON_M2`.

### 5.2.4 Cover-Shadow Selection (§3.4)

- **T-U-030** Worked example: ball-carrier `(60, 30)`, receiver
  `(75, 40)` → shadow `(68.25, 35.5)`.
- **T-U-031** Greedy assignment by threat-score, descending.
- **T-U-032** Unfilled slot demotes to `HOLD_SHAPE` (F4 /
  FR-PR-038).
- **T-U-033** Anti-chaos rejection demotes the candidate slot to
  `HOLD_SHAPE` rather than escalating other agents.

### 5.2.5 Role Hysteresis (§3.6)

- **T-U-040** Oscillating candidate stays in `lastRole` for
  `ROLE_DWELL_TICKS = 3` ticks of mismatch.
- **T-U-041** Stable candidate transition fires exactly at the Nth
  consecutive matching tick.

### 5.2.6 Stamina (§3.7)

- **T-U-050** `STAMINA_COST_PRIMARY_PER_TICK = 0.0040` accumulates
  exactly as expected over 100 ticks (10 s = +0.40).
- **T-U-051** Ceiling: agent at `fatigue = 0.85` is excluded;
  selection re-runs (regression for the historical inversion bug —
  `0 = rested, 1 = fatigued` per CLAUDE.md). Directional assertions:
  (a) agent at `fatigue = 0.84` IS eligible; (b) agent at
  `fatigue = 0.85` is excluded; (c) agent at `fatigue = 1.0`
  (fully fatigued) is excluded; (d) agent at `fatigue = 0.0`
  (fully rested) IS eligible.

### 5.2.7 Disengage and Reset (§3.8)

- **T-U-060** Timeout: no trigger for `DISENGAGE_TIMEOUT_TICKS = 8`
  consecutive ticks fires disengage.
- **T-U-061** Zone disengage: ball at `ballX < PRESS_ZONE_X_MIN`
  triggers immediate disengage.
- **T-U-062** Reset cooldown: no new press fires for
  `RESET_LATENCY_TICKS = 12` ticks after disengage.

### 5.2.8 Failure Modes (§2.4)

- **T-U-070** F1 stale perception → previous-tick output reused.
- **T-U-071** F2 NaN in `q` → `BAD_TOUCH` suppressed, other
  triggers proceed.
- **T-U-072** F3 mid-tick possession change → trigger evaluation
  deferred.
- **T-U-073** F4 empty cover-shadow candidate set → slot demotes.
- **T-U-074** F5 invariant violation → all-`HOLD_SHAPE` + dev-log
  `PRESSING_INVARIANT_FALLBACK`.
- **T-U-075** F6 sentinel slot from #12 → no override emitted for
  that agent.

## 5.3 Integration Test List

- **T-I-001** Each trigger × `OutOfPoss` phase produces a valid
  non-empty directive (4 cells).
- **T-I-002** Phase boundary `OutOfPoss → InPoss` immediately
  empties the directive (FR-PR-033).
- **T-I-003** Possession turnover sequence
  `InPoss → TransToDef → OutOfPoss` produces correct per-tick
  directive evolution.
- **T-I-004** Disengage round-trip: 50-tick window covering
  trigger → press → timeout → disengage → reset → trigger again.
- **T-I-005** Substitution event: substituted agent's
  `PressAssignment` is preserved at last value (F6).
- **T-I-006** Two triggers fire simultaneously (`BAD_TOUCH` +
  `SIDELINE_TRAP`) — primary-press and cover-shadow assigned;
  trigger origin tie-break is EntityId ascending.
- **T-I-007** Three-agent eligibility tie under `cost +
  SPACING_EPSILON_M2` window resolves deterministically by
  EntityId.
- **T-I-008** F5 invariant fallback after a forced
  `MAX_PRESS_DISPLACEMENT_M` violation by cover-shadow assignment
  past 25 m.
- **T-I-009** Hysteresis state survives a save/restore round-trip
  (#16 §3.2 binding).
- **T-I-010** Pure-function property: identical
  `(perception, passEvents, pos12, attackingDir, prevHyst,
  prevTrigger)` produces bit-identical output across two
  invocations.

## 5.4 Determinism Regression (Binding to #16 §5)

- **T-D-001** 90-min match replay on reference host produces
  bit-identical per-tick digest over two runs (same seed).
- **T-D-002** EntityId-permuted input produces identical
  post-iteration state (#16 §3.2.5 binding).
- **T-D-003** `RoleHysteresisState` + `PressTrigger` digest
  contributions are non-empty for every tick a press fires.
- **T-D-004** Cross-run digest stability: 10 consecutive
  90-minute runs produce the same final-tick digest.
- **T-D-005** Save/load mid-press: post-load tick 1 digest matches
  pre-save tick (N+1) digest.
- **T-D-006** RNG domain-tag isolation: removing
  `DOMAIN_TAG_PRESSING_AI` calls from another spec's RNG stream
  leaves #13's stream unchanged (regression once Stage 1+
  stochastic steps land).

## 5.5 Performance Validation (Binding to §6)

- **T-P-001** Per-tick wall-clock ≤ 0.10 ms on the named reference
  host (§6.3).
- **T-P-002** Zero heap allocations on the hot path under
  .NET allocation tracker (FR-PR-006, #18 §3.7).
- **T-P-003** Cover-shadow candidate scan bounded by
  `COVER_SHADOW_CANDIDATE_RADIUS_M` measurable cost ≤ 0.03 ms.

## 5.6 Anti-Chaos and Exploit-Resistance Scenarios (KD-16 / KD-17)

### 5.6.1 Anti-chaos invariants (KD-16, FR-PR-018..021)

- **T-C-001** Forcing 4 simultaneous press candidates into the
  ball-side third → 1 demotes to `HOLD_SHAPE` (count = 3).
- **T-C-002** Forcing a Defense-line agent into `PRIMARY_PRESS`
  that would drop `MIN_BACKLINE_AGENTS` below 3 → promotion
  blocked.
- **T-C-003** Forcing a cover-shadow target > 25 m from baseline →
  rejected, demote to `HOLD_SHAPE`.
- **T-C-004** Cascading violations (1+2+3 simultaneously) resolve
  to a clean directive within 3 demotion iterations.
- **T-C-005** Unresolvable cascade (primary-press demotion
  required) triggers F5 / FR-PR-039 fallback.
- **T-C-006** Repeated F5 fallback across N ticks does NOT corrupt
  hysteresis state (regression for state leakage on fallback).
- **T-C-007** Backline-floor breach triggers F5 immediately (1
  iteration, not the cover-shadow demotion path): force a
  Defense-line agent into `PRIMARY_PRESS` such that
  `backlineCount < MIN_BACKLINE_AGENTS = 3` — directive must fall
  back to all-`HOLD_SHAPE` in the same tick, distinct from the
  cascading-cover-shadow path covered by T-C-004 / T-C-005.

### 5.6.2 KD-17 Exploit-Resistance Corpus (Appendix E)

- **T-X-001 `EXPLOIT_LONG_BALL_OVER_PRESSERS`** — long ball over a
  high press must not collapse the entire defensive line:
  `MIN_BACKLINE_AGENTS` floor holds.
- **T-X-002 `EXPLOIT_SWITCH_OF_PLAY`** — switch to weak-side
  isolated zone must trigger disengage and reset within
  `RESET_LATENCY_TICKS = 12` ticks of the new ball position.
- **T-X-003 `EXPLOIT_ONE_TWO_BOUNCE`** — drag-and-bounce one-twos
  through the press must not deterministically beat it: at least
  one defender remains behind the bounce per
  `MIN_BACKLINE_AGENTS`.
- **T-X-004 `EXPLOIT_GK_PIVOT`** — backward pass to GK triggers
  `BACKWARD_PASS` but does not commit beyond halfway line
  (KD-13 + `PRESS_ZONE_X_MIN`).

## 5.7 Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 17, 2026 | AI agent (claude/draft-ai-specification-5tvwH) | Initial draft from `outline-detailed.md` v1.0. Unit target ≥40; integration ≥10; KD-16 ≥6; KD-17 corpus ≥4. Total ≥69. |
| 0.2 | May 17, 2026 | AI agent (claude/fix-ai-specs-review-qgWFR) | PASS-1 adversarial fix pass. AR-S1-H3: T-U-051 extended with directional assertions (fully-rested eligible; fully-fatigued excluded). AR-S1-M6: T-C-007 added for backline-floor breach + F5 path. AR-S1-L4: §5 preamble note added for #19 prefix conformance grep (pending). |
