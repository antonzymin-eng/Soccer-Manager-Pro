# Positioning AI Specification #12 — Section 6: Performance Analysis and Budgets

**Created:** May 15, 2026
**Last Updated:** May 18, 2026 (v0.3 — APPROVED: domain tag allocated, [EST] and GK constants promoted to [GT])
**Version:** 0.3
**Status:** APPROVED

---

## 6.1 Constant Catalogue

All `PositioningAIConstants.cs` constants are catalogued here with
tag, unit, source, and §-reference. Outline-stage `[EST]` values
require an Appendix A derivation entry before promotion to `[GT]`
(KD-12; CLAUDE.md "When Writing or Editing Specs").

| Constant | Value | Unit | Tag | Source / Reference |
|---|---|---|---|---|
| `PITCH_LENGTH_M` | 105.0 | m | `[FIXED]` | #1 §1.2 |
| `PITCH_WIDTH_M` | 68.0 | m | `[FIXED]` | #1 §1.2 |
| `MIN_AGENT_SEPARATION_M` | 1.5 | m | `[FIXED]` | #3 §3.x (collision radius) |
| `MIN_AGENT_SEPARATION_M_SQ` | 2.25 | m² | `[DERIVED]` | `= MIN_AGENT_SEPARATION_M²` |
| `SPACING_EPSILON_M2` | 1e-4 | m² | `[FIXED]` | KD-16 |
| `SPACING_EPSILON_M` | 1e-2 | m | `[DERIVED]` | `= sqrt(SPACING_EPSILON_M2)` |
| `DOMAIN_TAG_POSITIONING_AI` | 0x17 | byte | `[CROSS: #16 §3.4]` | #16 §3.4 v1.0.5 (May 18, 2026) — allocated as `0x17` per ERR-012-001; value shifted from 0x16 on May 16, 2026 after #10 took 0x16 via ERR-010-001; #12 reached `APPROVED` first, claiming 0x17 per first-to-`APPROVED` precedent |
| `ANCHOR_DWELL_TICKS` | 5 | tick | `[GT]` | §3.8 — Appendix A.1 |
| `LINE_HYSTERESIS_M` | 3.0 | m | `[GT]` | §3.3.2 — Appendix A.2 |
| `LINE_DWELL_TICKS` | 5 | tick | `[GT]` | §3.3.2 — Appendix A.3 |
| `LANE_HYSTERESIS_M` | 2.0 | m | `[GT]` | §3.4.2 — Appendix A.4 |
| `PHASE_HYSTERESIS_TICKS` | 3 | tick | `[GT]` | §3.0.3 — Appendix A.5 |
| `PHASE_LOOSE_VELOCITY_THRESHOLD` | 4.0 | m/s | `[GT]` | §3.0.2 — Appendix A.6 |
| `OFFSET_RANGE_X_M` | 12.0 | m | `[GT]` | §3.2.1 — Appendix A.7 |
| `OFFSET_RANGE_Y_M` | 8.0 | m | `[GT]` | §3.2.1 — Appendix A.8 |
| `SCORE_ATK_GAIN` | 0.05 | — | `[GT]` | §3.5.1 |
| `FATIGUE_LATERAL_RELAX` | 0.15 | — | `[GT]` | §3.5.1 |
| `INTENSITY_VERTICAL_GAIN` | 0.20 | — | `[GT]` | §3.5.1 |
| `SOFT_LANE_OVERLOAD_COST` | 0.5 | — | `[GT]` | §3.4.3 / §3.6.2 |
| `SPACING_MAX_PASSES` | 4 | — | `[GT]` | §3.6.1 (AR-S1-06 convergence cap; promoted `[EST]` → `[GT]` May 18, 2026 — designer-tunable max iteration count) |
| `LANE_EDGES_M[6]` | {0,13.6,27.2,40.8,54.4,68} | m | `[DERIVED]` | §3.4.1 (= `i · PITCH_WIDTH_M/5` as a literal array; AR-S1-12) |
| `GK_DEPTH_M` | 5.5 | m | `[GT]` | §3.3.3 (KD-13; promoted `[EST]` → `[GT]` atomically with #11 `APPROVED` May 18, 2026) |
| `GK_ADVANCE_FACTOR` | 8.0 | m | `[GT]` | §3.3.3 (KD-13; promoted `[EST]` → `[GT]` atomically with #11 `APPROVED` May 18, 2026) |
| `GK_LATERAL_FACTOR` | 2.0 | m | `[GT]` | §3.3.3 (KD-13; promoted `[EST]` → `[GT]` atomically with #11 `APPROVED` May 18, 2026) |
| `SENTINEL_NO_SLOT` | (−∞, −∞) | — | `[FIXED]` | §3.11 / §2.4 (AR-S1-07) |
| `PITCH_TOUCHLINE_MARGIN_M` | 0.5 | m | `[GT]` | FR-PA-033 / FR-PA-046 |
| `baseLateral[Phase]` table | (4 rows) | — | `[GT]` | §3.5 |
| `baseVertical[Phase]` table | (4 rows) | — | `[GT]` | §3.5 |
| `pullFactor[RoleId, Phase]` table | (13 × 4 rows, sparse) | — | `[GT]` | §3.2 |
| `FAMILY_4_4_2` archetype | 11 rows × 5 cols | — | `[GT]` | Appendix B |
| `FAMILY_4_3_3` archetype | 11 rows × 5 cols | — | `[GT]` | Appendix B |
| `FAMILY_4_2_3_1` archetype | 11 rows × 5 cols | — | `[GT]` | Appendix B |

## 6.2 Hot Path Enumeration (#18 KD-10 Binding)

The per-tick main loop (§3.11) has the following hot-path
operations, all marked `[HotPathAllocExempt]` per #18 §3.7 (zero
heap allocations):

| Operation | Per-Tick Count | Complexity |
|---|---|---|
| `ClassifyPhase` | 1 | O(1) |
| `ComputeCentroid` | 1 | O(22) |
| `ComputeAnchor` | 22 | O(1) lookup |
| `ComputeBallRelativeOffset` | 22 | O(1) lookup |
| `ApplyContextModifiers` | 22 | O(1) |
| `EnforceHardSpacingIterated` (§3.6 pairwise, up to `SPACING_MAX_PASSES`) | ≤ 4 × 231 = 924 pair-evaluations worst case | O(P·N²) — bounded P=4, N=22 |
| `ResolveLine` / `ResolveLane` post-spacing (§3.7 step 6) | 22 + 22 | O(1) each |
| `ClampToPitch` | 22 | O(1) |

Aggregate per tick: ≤ ~600 small struct operations + ≤ 924
squared-distance pair-evaluations (worst case under `SPACING_MAX_
PASSES = 4`; typical 1–2 passes → 231–462 evaluations).

## 6.3 Per-Tick Budget (Reference Host per KD-15)

**Target:** ≤ 0.15 ms per 10 Hz tick.

**Reference host** (KD-15; pinned until `certification-platform.md`
is filled by lead developer):

- CPU: AMD Ryzen 7 5800X @ 4.5 GHz
- RAM: 32 GB DDR4-3200
- OS: Windows 11
- Engine: Unity 2022.3 LTS, Mono backend
- Threading: single-threaded measurement
- Build: Unity Editor playmode profiler (AR-S1-20: "Editor playmode" — not a Player build; explicit caveat that Player IL2CPP / Release perf will differ. Final certification host per `certification-platform.md` will pin a Player-build configuration.)

**Caveat:** The cert-pinned budget supersedes once
`certification-platform.md` is filled. Values may shift ±30% on the
final certification host. This is acknowledged here in prose
(KD-15) and is NOT a `TBD-NORMATIVE` placeholder.

## 6.4 Per-Frame Budget

N/A — Positioning AI does no per-frame work. The 60 Hz steering
loop owned by Agent Movement #2 consumes the resolved `Action.
TargetPosition` produced by #8 (which in turn reads #12's slot from
`TacticalContext`).

## 6.5 Memory Footprint

| Item | Size | Persistence |
|---|---|---|
| `Vector2 formationSlot[22]` | 22 × 8 B = 176 B | per-tick output buffer |
| `HysteresisState` | ≈ 22 × (4+1+1) B + 8 B ≈ 140 B | digested state |
| Three `FormationArchetype` tables | 3 × 11 × (1 + 4 + 4 + 1 + 1) B ≈ 360 B | `static readonly`, never copied |
| `pullFactor` table | 13 × 4 × 8 B ≈ 416 B | `static readonly` |
| `baseLateral` / `baseVertical` tables | 2 × 4 × 4 B = 32 B | `static readonly` |
| **Total per-team mutable** | < 2 KB | — |
| **Total `static readonly`** | ≈ 1 KB | — |

## 6.6 Profiling Plan

| Profile | Tool | Frequency | Gate |
|---|---|---|---|
| Per-tick wall-clock | Unity Profiler / BenchmarkDotNet | Nightly CI | T-P-001 |
| Zero-allocation hot path | .NET allocation tracker | Per-PR CI | T-P-002 |
| Hot-path channel registry | #18 trace pipeline (Appendix F.0 channel schema) | Stage 0+1 manual; Stage 1+ automated | FR-PO-070 / FR-PA-006 |
| Determinism regression | #16 §5 digest harness | Nightly CI | T-D-001..T-D-006 |

## 6.7 Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 15, 2026 | AI agent (claude/draft-positional-ai-specs-MOejb) | Initial section-file draft from `outline-detailed.md` v1.2. Constant catalogue published with all tags; outline-stage `[EST]` values flagged for Appendix A derivation. |
| 0.2 | May 16, 2026 | AI agent (claude/review-positional-ai-specs-v4rmD) | PASS-1 adversarial fix pass. AR-S1-08 `FATIGUE_LATERAL_RELAX_M` removed (unused by any formula); AR-S1-11 GK constants demoted `[GT]` → `[EST]`; AR-S1-12 `LANE_EDGES_M` literal array added as `[DERIVED]`; AR-S1-06 `SPACING_MAX_PASSES = 4` added; `SENTINEL_NO_SLOT` added per AR-S1-07; AR-S1-20 §6.3 build-config disambiguated to "Editor playmode profiler"; §6.2 hot-path table updated for iterated spacing + post-spacing line/lane resolve. |
| 0.3 | May 18, 2026 | AI agent (claude/review-phase-0-requirements-yMzh6) | APPROVED patch. ERR-012-001 resolved: `DOMAIN_TAG_POSITIONING_AI` promoted `[CROSS-PENDING]` → `[CROSS: #16 §3.4]` (value confirmed 0x17, allocated in #16 §3.4 v1.0.5). All 8 hysteresis/offset constants promoted `[EST]` → `[GT]` (Appendix A.1–A.8 derivations confirmed). OI-005 (KD-13): GK constants `GK_DEPTH_M`, `GK_ADVANCE_FACTOR`, `GK_LATERAL_FACTOR` promoted `[EST]` → `[GT]` atomically with #11 `APPROVED` transition. |
| 0.4 | May 18, 2026 | AI agent (adversarial-specs-review-run3) | Run 3 fix: `SPACING_MAX_PASSES` promoted `[EST]` → `[GT]` (omitted from v0.3 batch; SPEC_INDEX.md claim "no `[EST]` remain" now accurate). |
