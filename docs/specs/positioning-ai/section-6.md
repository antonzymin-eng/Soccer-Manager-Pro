# Positioning AI Specification #12 — Section 6: Performance Analysis and Budgets

**Created:** May 15, 2026
**Last Updated:** May 15, 2026 (v0.1 — initial draft from `outline-detailed.md` v1.2)
**Version:** 0.1
**Status:** DRAFT

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
| `DOMAIN_TAG_POSITIONING_AI` | 0x16 | byte | `[CROSS-PENDING]` | #16 §3.4 via `ERR-012-001` |
| `ANCHOR_DWELL_TICKS` | 5 | tick | `[EST]` | §3.8 — Appendix A pending |
| `LINE_HYSTERESIS_M` | 3.0 | m | `[EST]` | §3.3.2 — Appendix A pending |
| `LINE_DWELL_TICKS` | 5 | tick | `[EST]` | §3.3.2 — Appendix A pending |
| `LANE_HYSTERESIS_M` | 2.0 | m | `[EST]` | §3.4.2 — Appendix A pending |
| `PHASE_HYSTERESIS_TICKS` | 3 | tick | `[EST]` | §3.0.3 — Appendix A pending |
| `PHASE_LOOSE_VELOCITY_THRESHOLD` | 4.0 | m/s | `[EST]` | §3.0.2 — Appendix A pending |
| `OFFSET_RANGE_X_M` | 12.0 | m | `[EST]` | §3.2.1 — Appendix A pending |
| `OFFSET_RANGE_Y_M` | 8.0 | m | `[EST]` | §3.2.1 — Appendix A pending |
| `SCORE_ATK_GAIN` | 0.05 | — | `[GT]` | §3.5.1 |
| `FATIGUE_LATERAL_RELAX` | 0.15 | — | `[GT]` | §3.5.1 |
| `FATIGUE_LATERAL_RELAX_M` | 4.0 | m | `[GT]` | §3.5.1 |
| `INTENSITY_VERTICAL_GAIN` | 0.20 | — | `[GT]` | §3.5.1 |
| `SOFT_LANE_OVERLOAD_COST` | 0.5 | — | `[GT]` | §3.4.3 / §3.6.2 |
| `GK_DEPTH_M` | 5.5 | m | `[GT]` | §3.3.3 |
| `GK_ADVANCE_FACTOR` | 8.0 | m | `[GT]` | §3.3.3 |
| `GK_LATERAL_FACTOR` | 2.0 | m | `[GT]` | §3.3.3 |
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
| `ResolveLine` / `ResolveLane` | 22 + 22 | O(1) each (hysteresis is local state) |
| `EnforceHardSpacing` (§3.6 pairwise) | 22 × 21 / 2 = 231 pairs | O(N²) — bounded N=22 |
| `ClampToPitch` | 22 | O(1) |

Aggregate per tick: ≤ ~600 small struct operations + 231 squared-
distance pairs.

## 6.3 Per-Tick Budget (Reference Host per KD-15)

**Target:** ≤ 0.15 ms per 10 Hz tick.

**Reference host** (KD-15; pinned until `certification-platform.md`
is filled by lead developer):

- CPU: AMD Ryzen 7 5800X @ 4.5 GHz
- RAM: 32 GB DDR4-3200
- OS: Windows 11
- Engine: Unity 2022.3 LTS, Mono backend
- Threading: single-threaded measurement
- Build: Editor Profile, Release configuration

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
