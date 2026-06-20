# Tactical Instructions Specification #21 — Section 5: Test Plan

**Created:** June 20, 2026
**Last Updated:** June 20, 2026 (v0.3 — PASS-2 fix pass)
**Version:** 0.3
**Status:** IN REVIEW

> Test-ID prefixes follow #19 §3.1.4: `T-TI-U-*` unit, `T-TI-I-*` integration, `sim_*` /
> `T-TI-SIM-*` simulation (closed-loop on the #19 `ScenarioRunner`), `T-TI-DET-*` determinism,
> `T-TI-FAIL-*` failure-mode, `T-TI-EXP-*` exploit/robustness.

---

## 5.1 Test counts (target)

| Layer | Count (≥) | Notes |
|---|---|---|
| Unit | 40 | enum ordinals, factory identity, every §3 mapping endpoint |
| Integration | 18 | each consumer seam + man-mark override |
| Simulation (closed-loop) | 6 | one per consumer, via #19 ScenarioRunner |
| Determinism | 5 | stride-boundary apply; snapshot digest |
| Failure-mode | 5 | F1–F5 |
| Exploit/robustness | 4 | invariant precedence, widened-enum clamp, FocusPlay sanity, schema-order lock |
| **Total** | **≥ 78** | |

## 5.2 Unit tests (`T-TI-U-*`)

- **T-TI-U-001..016** — `EnumOrdinalStability`: the 14 sequential enums assert `(int)Member == N`; the
  2 `[Flags]` enums (`TacticTriggerMask`, `SetPieceDutyFlags`) assert bit-position values (1,2,4,…) and
  the 8-flag `byte` ceiling instead (APPEND-only lock; FR-TI-007).
- **T-TI-U-017..019** — factory identity: `TeamTactic.Balanced`, `PlayerInstructions.Default`,
  `PlayerTactic.Default(role)` field-by-field equal the documented neutral values (FR-TI-031).
- **T-TI-U-020..026** — `Mentality` map (§3.2): all 7 rows return the pinned `(profile, riskMult,
  lineBias)`; Balanced → (Possession, 1.00, 0.00) (FR-TI-011).
- **T-TI-U-027..030** — `RoleWeightModifiers`: default-row identity; Poacher SHOOT > 1 and HOLD < 1;
  every table cell ∈ [0.5, 2.0]; unknown `(role,type)` returns 1.0 (FR-TI-012).
- **T-TI-U-031..036** — direct-input transforms (§3.4): `InstrBias` {0.85,1.0,1.15}; `Tempo`/`Width`/
  `LineOfEngagement` endpoints + Default identity (FR-TI-013..017).
- **T-TI-U-037..040** — translation maps (§3.1): each `Tactic*` → subsystem enum 1:1; widened value
  clamps (F5).

## 5.3 Integration tests (`T-TI-I-*`)

- **T-TI-I-001..004** — #8: a `TeamTactic`/`PlayerTactic` flows through `TacticalContext` and changes
  `UtilityScorer` output in the expected direction (Attacking raises SHOOT utility; Poacher raises it
  further); default tactic leaves output unchanged (FR-TI-012/031).
- **T-TI-I-005..008** — #12/#13/#14/#15 seams: a width/trigger-mask/offside/style field on each
  snapshot produces the expected directive change.
- **T-TI-I-009..012** — man-mark override (FR-TI-023): valid target → `MarkMode.ManMark`; the override
  is demoted when it would breach a #14 invariant (F3).
- **T-TI-I-013..018** — GK distribution policy, focus-play branch, tempo threshold, transition gate,
  duty bias, tight-marking.

## 5.4 Determinism tests (`T-TI-DET-*`)

- **T-TI-DET-001** — an in-match tactic change applied at a non-stride tick is deferred to the next
  stride; two runs with the change requested at different sub-stride frames produce identical digests
  (FR-TI-027).
- **T-TI-DET-002** — default-tactic **world-state-subset** digest (ball + agent state, excluding the
  tactics block) == pre-tactics baseline world-state digest (FR-TI-031 behavioural identity). The full
  payload digest necessarily differs (tactics block added per FR-TI-028) and is NOT asserted equal.
- **T-TI-DET-003** — snapshot field-set ordering matches Appendix B; a perturbed instruction field
  changes the digest (proves it is digest-load-bearing; FR-TI-028).
- **T-TI-DET-004** — no RNG draw site is registered by this layer (FR-TI-026).
- **T-TI-DET-005** — same `(TeamTactic, PlayerTactic[])` + seed ⇒ identical 90-minute digest.

## 5.5 Simulation / closed-loop (`T-TI-SIM-*`, via #19 ScenarioRunner)

One scenario per consumer once its match-engine phase composes: e.g. `sim_high_mentality_shoots_more`
(#8), `sim_wide_tactic_spreads_shape` (#12), `sim_high_line_triggers_press_sooner` (#13),
`sim_offside_trap_steps_up` (#14), `sim_counter_transition_breaks_fast` (#15),
`sim_gk_quick_distribution` (#11). Each asserts an envelope predicate on the realised behaviour.

## 5.6 Robustness / balance (`T-TI-EXP-*`) and the balance pass

- **T-TI-EXP-001** — man-mark vs. anti-chaos: a manager man-marking 5 opponents never drops the backline
  below `MinBacklineAgents` (FR-TI-023 precedence).
- **T-TI-EXP-002** — widened-enum clamp (F5) never throws.
- **T-TI-EXP-003** — `FocusPlay.Left` measurably biases option laterality but never starves the
  opposite channel to zero options (no degenerate funnel).
- **T-TI-EXP-004** — schema-order lock: Appendix B order is asserted byte-for-byte.
- **Balance pass (gating §6.2 of the supplement):** `RoleWeightModifiers` + §3.2 values get a
  numerical-mirror + adversarial review before their `[GT]` values are pinned. Until then values are
  illustrative and tests assert **shape/direction**, not absolute magnitudes.

## 5.7 FR → test traceability

Most FRs trace to ≥1 executable test. Four FRs are structural and trace to a **named verification**
(asmdef-reference grep / inspection) rather than a runtime test: FR-TI-002, FR-TI-003, FR-TI-029,
FR-TI-030. These are listed as `verify:` below.

| FR | Tests | FR | Tests |
|---|---|---|---|
| 001 | I-001 | 017 | U-035, I-006 |
| 002–003 | verify: §4.7 asmdef-ref grep | 018 | I-006 |
| 004 | U-037..040 | 019 | I-007 |
| 005 | U-001..016 | 020 | I-016 |
| 006 | U-017..019 | 021 | EXP-003, I-014 |
| 007 | U-001..016 | 022 | I-013 |
| 008–010 | verify: §9 item 6 (constant-tag inspection) | 023 | I-009..012, EXP-001 |
| 011 | U-020..026 | 024 | I-001..008 |
| 012 | U-027..030, I-001..004 | 025 | DET-003, §4.7 grep |
| 013 | U-031, I-017 | 026 | DET-004 |
| 014 | U-031, I-013..018 | 027 | DET-001 |
| 015 | U-033, I-015 | 028 | DET-003, EXP-004 |
| 016 | U-034, I-005 | 029 | verify: §4.5 (no interface published) |
| | | 030 | verify: §7 stage gating |
| | | 031 | U-017..019, DET-002 |
| | | 032 | §3 worked examples |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-06-20 | — | Test plan: ≥78 tests across 6 layers + FR traceability + balance-pass gate. |
| 0.2 | 2026-06-20 | — | PASS-1 fix pass: `[Flags]` stability-test carve-out (M-3); FR-002/003/029/030 reclassified verify-by-inspection (M-4). |
| 0.3 | 2026-06-20 | — | PASS-2 fix pass: DET-002 compares the world-state subset, not the full payload (H-1); §5.7 FR-008/009/010 retargeted to constant-tag inspection (L-1). |
#endregion
