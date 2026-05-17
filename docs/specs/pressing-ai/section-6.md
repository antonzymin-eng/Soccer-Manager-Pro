# Pressing AI Specification #13 — Section 6: Performance Analysis and Budgets

**Created:** May 17, 2026
**Last Updated:** May 17, 2026 (v0.2 PASS-1 adversarial-review fix pass)
**Version:** 0.2
**Status:** DRAFT
**Source:** `outline-detailed.md` v1.0

---

## 6.1 Constant Catalogue

All `PressingAIConstants.cs` constants are catalogued here with
tag, unit, source, and §-reference. Outline-stage `[EST]` values
require an Appendix A derivation entry before promotion to `[GT]`
(KD-14; CLAUDE.md "When Writing or Editing Specs").

### 6.1.1 Coordinate / Physical (`[FIXED]` / `[DERIVED]` / `[CROSS]`)

| Constant | Value | Unit | Tag | Source / Reference |
|---|---|---|---|---|
| `PITCH_LENGTH_M` | 105.0 | m | `[CROSS]` | #1 §1.2 (`XC-013-001`) |
| `PITCH_WIDTH_M` | 68.0 | m | `[CROSS]` | #1 §1.2 (`XC-013-001`) |
| `DT_TACTICAL` | 0.10 | s | `[DERIVED]` | `= 1 / 10 Hz` (CLAUDE.md) |
| `SPACING_EPSILON_M2` | 1e-4 | m² | `[CROSS]` | #12 §3.6.1 / KD-16 (`XC-013-003`) |
| `DOMAIN_TAG_PRESSING_AI` | 0x19 | byte | `[CROSS-PENDING]` | #16 §3.4 via `ERR-013-005` (inherits ERR-012-001 block proposal) |

### 6.1.2 Trigger Thresholds (`[GT]`)

| Constant | Value | Unit | Tag | Source / Reference |
|---|---|---|---|---|
| `BAD_TOUCH_THRESHOLD` | 0.40 | — | `[GT]` | §3.1.1 |
| `BAD_TOUCH_VELOCITY_M_S` | 4.0 | m/s | `[GT]` | §3.1.1 |
| `BACKWARD_PASS_THRESHOLD` | −0.30 | — (dot) | `[GT]` | §3.1.2 |
| `SIDELINE_TRAP_DISTANCE_M` | 8.0 | m | `[GT]` | §3.1.3 |
| `WEAK_RECEIVER_THRESHOLD` | 10 | (1–20 attr) | `[GT]` | §3.1.4 |
| `WEAK_RECEIVER_PRESSURE` | 0.50 | — | `[GT]` | §3.1.4 |

### 6.1.3 Hysteresis (`[EST]` — promote to `[GT]` with Appendix A)

| Constant | Value | Unit | Tag | Source / Reference |
|---|---|---|---|---|
| `TRIGGER_DWELL_TICKS` | 2 | tick (200 ms) | `[EST]` | §3.2 — Appendix A pending |
| `TRIGGER_RELEASE_TICKS` | 3 | tick (300 ms) | `[EST]` | §3.2 — Appendix A pending |
| `ROLE_DWELL_TICKS` | 3 | tick (300 ms) | `[EST]` | §3.6 — Appendix A pending |
| `INTERCEPT_LOOKAHEAD_TICKS` | 3 | tick (300 ms) | `[EST]` | §3.3 — Appendix A pending |

### 6.1.4 Role / Cover-Shadow / Threat (`[GT]`)

| Constant | Value | Unit | Tag | Source / Reference |
|---|---|---|---|---|
| `MAX_COVER_SHADOWS` | 2 | — | `[GT]` | KD-8 / §3.4 / FR-PR-016 |
| `COVER_SHADOW_LANE_FRACTION` | 0.55 | — | `[GT]` | §3.5 / FR-PR-024 |
| `COVER_SHADOW_CANDIDATE_RADIUS_M` | 20.0 | m | `[GT]` | §3.4 |
| `THREAT_PROGRESSION_W` | 0.50 | — | `[GT]` | §3.4 |
| `THREAT_OPEN_W` | 0.30 | — | `[GT]` | §3.4 |
| `THREAT_SKILL_W` | 0.20 | — | `[GT]` | §3.4 |
| `THREAT_PRESSURE_NORMALIZER` | 3.0 | — | `[GT]` | §3.4 — three own-team defenders within candidate radius saturates geometric pressure signal |

### 6.1.5 Stamina / Fatigue (`[GT]`)

| Constant | Value | Unit | Tag | Source / Reference |
|---|---|---|---|---|
| `STAMINA_COST_PRIMARY_PER_TICK` | 0.0040 | — / tick | `[GT]` | §3.7 / FR-PR-027 |
| `STAMINA_COST_SHADOW_PER_TICK` | 0.0020 | — / tick | `[GT]` | §3.7 / FR-PR-028 |
| `PRESS_FATIGUE_CEILING` | 0.85 | — | `[GT]` | §3.7 / FR-PR-029 |

### 6.1.6 Disengage / Reset / Zone (`[GT]`)

| Constant | Value | Unit | Tag | Source / Reference |
|---|---|---|---|---|
| `DISENGAGE_TIMEOUT_TICKS` | 8 | tick (800 ms) | `[GT]` | §3.8 / FR-PR-030 |
| `RESET_LATENCY_TICKS` | 12 | tick (1.2 s) | `[GT]` | §3.8 / FR-PR-032 |
| `PRESS_ZONE_X_MIN` | 35.0 | m | `[GT]` | §3.8 / FR-PR-031 (default pressing zone — mid-block geometry; high-press style uses ≈ 70 m) |
| `PRESS_ZONE_X_MAX` | 105.0 | m | `[GT]` | §3.8 / FR-PR-031 (intentional trivially-true upper bound — ball cannot exceed 105 m in a live match; retained as defensive guard per two-parameter zone contract) |

### 6.1.7 Anti-Chaos Invariants (`[GT]`)

| Constant | Value | Unit | Tag | Source / Reference |
|---|---|---|---|---|
| `MAX_PRESSERS_BALL_THIRD` | 3 | — | `[GT]` | KD-16 / FR-PR-018 |
| `MIN_BACKLINE_AGENTS` | 3 | — | `[GT]` | KD-16 / FR-PR-019 |
| `MAX_PRESS_DISPLACEMENT_M` | 25.0 | m | `[GT]` | KD-16 / FR-PR-020 |

### 6.1.8 Cite-not-redefine (cross-spec read-only)

| Constant | Value | Unit | Tag | Source |
|---|---|---|---|---|
| `PRESS_STAMINA_MINIMUM` | 0.20 | — | `[CROSS]` | #8 §3.1.8.1 (`XC-013-004`) |
| `PRESS_TRIGGER_DISTANCE` | 8.0 | m | `[CROSS]` | #8 §3.1.8.2 (`XC-013-005`) |

## 6.2 Hot Path Enumeration (#18 KD-10 Binding)

The per-tick main loop (§3.11) has the following hot-path
operations, all expected `[HotPathAllocExempt]` per #18 §3.7.5
(declaration site to be confirmed at Stage 1 first commit):

| Operation | Per-Tick Count | Complexity |
|---|---|---|
| Phase gate read (`pos12.Phase`) | 1 | O(1) |
| `EvaluateRawTriggers` | 1 | O(N) where N = visible opponents ≤ 11 |
| `UpdateTriggerDebounce` | 4 flags | O(1) |
| `SelectPrimaryPress` | 1 | O(M) where M = eligible defenders ≤ 10 |
| `SelectCoverShadows` | 1 | O(M·R) where R = receivers within radius ≤ 11; bounded ≈ 50 pair checks |
| `ApplyRoleHysteresis` | 22 | O(1) per agent |
| `EnforceInvariants` | ≤ 3 cover-shadow demotion iterations (invariants (1) and (3)); invariant (2) backline-floor breach: 1 iteration (F5 immediate) | O(K) per iteration where K = number of press roles |
| `AccumulateStamina` | 22 | O(1) per agent |
| `WriteAssignments` | 22 | O(1) per agent |

Aggregate per tick: ≤ ~120 small struct operations + ≤ 50
pair-cost evaluations in the cover-shadow scan worst case.

## 6.3 Per-Tick Budget (Reference Host per KD-15 / #12 §6.3)

**Target:** ≤ 0.10 ms per 10 Hz tick.

**Reference host** (inherited from #12 §6.3 KD-15; pinned until
`certification-platform.md` is filled by lead developer):

- CPU: AMD Ryzen 7 5800X @ 4.5 GHz
- RAM: 32 GB DDR4-3200
- OS: Windows 11
- Engine: Unity 2022.3 LTS, Mono backend
- Threading: single-threaded measurement
- Build: Unity Editor playmode profiler (not a Player build —
  matches #12 §6.3 profiling host description)

**Caveat:** The cert-pinned budget supersedes once
`certification-platform.md` is filled. Values may shift ±30% on
the final certification host. This is acknowledged in prose
(KD-15 inheritance) and is NOT a `TBD-NORMATIVE` placeholder.

## 6.4 Per-Frame Budget

N/A — Pressing AI does no per-frame work. The 60 Hz steering loop
owned by Agent Movement #2 consumes the resolved `Action.
TargetPosition` produced by #8 (which in turn reads #13's
`PressAssignment` via the OI-001 mechanism).

## 6.5 Memory Footprint

| Item | Size | Persistence |
|---|---|---|
| `PressDirective` | ~ 80 B | per-tick output |
| `PressAssignment[22]` | 22 × 32 B ≈ 704 B | per-tick output |
| `RoleHysteresisState` | ~ 100 B | digested state |
| `PressTrigger` | ~ 40 B | digested state |
| Constants (catalogue) | < 1 KB | `static readonly` |
| **Total per-team mutable** | < 1 KB | — |

## 6.6 Profiling Plan

| Profile | Tool | Frequency | Gate |
|---|---|---|---|
| Per-tick wall-clock | Unity Profiler / BenchmarkDotNet | Nightly CI | T-P-001 |
| Zero-allocation hot path | .NET allocation tracker | Per-PR CI | T-P-002 |
| Hot-path channel registry — conformance to #18 Appendix F.0 schema | #18 trace pipeline (Appendix F.0 schema) | Stage 1 first-commit deliverable; Stage 1+ automated CI | FR-PO-070 |
| Hot-path zero-allocation audit | .NET allocation tracker (FR-PR-006) | Per-PR CI | FR-PR-006 |
| Determinism regression | #16 §5 digest harness | Nightly CI | T-D-001..T-D-006 |

## 6.7 Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 17, 2026 | AI agent (claude/draft-ai-specification-5tvwH) | Initial draft from `outline-detailed.md` v1.0. Constant catalogue published with all tags; outline-stage `[EST]` values flagged for Appendix A derivation. |
| 0.2 | May 17, 2026 | AI agent (claude/fix-ai-specs-review-qgWFR) | PASS-1 adversarial fix pass. AR-S1-H6: `THREAT_PRESSURE_NORMALIZER = 3.0 [GT]` added to §6.1.4. AR-S1-M1: §6.1.6 labels corrected (PRESS_ZONE_X_MIN: "mid-block default"; PRESS_ZONE_X_MAX: "trivially-true upper bound"). AR-S1-M6: §6.2 EnforceInvariants row clarified (≤3 for cover-shadow demotions; backline = 1 iteration / F5). AR-S1-M7: §6.6 split into two rows (channel registry / FR-PO-070 separate from zero-alloc / FR-PR-006). AR-S1-L3: §6.3 citation changed from "#12 AR-S1-20" to "#12 §6.3". |
