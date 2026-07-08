# Dismarking & Marker-Awareness AI Specification #23 — Section 2: Functional Requirements, Data Structures, Failure Modes

**Created:** July 8, 2026
**Last Updated:** July 8, 2026 (v0.1)
**Version:** 0.1
**Status:** IN REVIEW

---

## 2.1 Functional Requirements

Conformance per RFC 2119. Citations resolve to a KD in §1.4 or a downstream section.

| FR | Subject | Conformance | Source |
|---|---|---|---|
| FR-DM-001 | `MarkingPressure` is computed exclusively from the agent's own `FilteredView` (`VisibleOpponents[0..Count-1].PerceivedPosition`). No input may originate from `MarkAssignment`, `PressDirective`, `AttackDirective`, or any opposing team's internal state. | MUST | KD-1 |
| FR-DM-002 | `MarkingPressure ∈ [0,1]`; `0` = unmarked, `1` = fully marked. Computed per §3.1 as `proximity01 × dwell01`. | MUST | §3.1 |
| FR-DM-003 | The dwell counter updates once per 10 Hz heartbeat per agent, before any consumer reads it that tick (increment while a perceived opponent is within `MARKING_RADIUS_M`; decay by `MARKING_DWELL_DECAY_PER_TICK` otherwise), per the §3.2 state machine. | MUST | §3.2 |
| FR-DM-004 | An unperceived marker (out of FoV / occluded / blind side) contributes zero pressure. The evaluator MUST NOT compensate with ground-truth data. | MUST | KD-1 |
| FR-DM-005 | The evaluator is a pure static function of `(FilteredView, dwell state)`; no RNG draw site, no #16 domain tag, no allocation. | MUST | KD-2 / #20 |
| FR-DM-006 | Evaluation applies only while the agent's team phase is `Phase.InPoss`; any other phase returns the identity (pressure 0, no offset) and freezes decay-only dwell updates per §3.2. | MUST | KD-6 |
| FR-DM-007 | The ball carrier and the goalkeeper are excluded from the dismark offset stage. | MUST | KD-6 |
| FR-DM-008 | The dismark offset stage runs inside #12 `SlotComposer` **after** `SpacingResolver` and **before** the pitch clamp, so composed targets remain on-pitch. | MUST | §3.3 / KD-3 |
| FR-DM-009 | Offset magnitude = `DISMARK_OFFSET_MAX_M × MarkingPressure × DismarkIntensityScalar[dial]`; direction per §3.3 (away from the perceived marker), with the degenerate-distance guard `DISMARK_MIN_MARKER_DIST_EPS`. | MUST | §3.3 |
| FR-DM-010 | The #8 marked-pass-target penalty multiplies PASS option utility by `Lerp(1.0, TARGET_MARKED_UTILITY_MULT, targetProximity01 × awareness01)` where `awareness01` is the **passer's** mean of `A_Decisions`/`A_Anticipation` — an unaware passer plays the marked pass anyway (mirrors the corrected rest-defense design, #8 §7.7). | MUST | §3.4 |
| FR-DM-011 | `targetProximity01` in FR-DM-010 is computed from the **passer's** `FilteredView` (perceived teammate + perceived opponents), never from the teammate's own dwell state or ground truth. | MUST | KD-1 / §3.4 |
| FR-DM-012 | `DismarkIntensity.Off` (zero value) is the exact identity: offset scalar 0.0 and penalty mult ×1.0. A default match is byte-identical to pre-#23. | MUST | KD-4 |
| FR-DM-013 | `DismarkIntensity` is `byte`-backed, APPEND-only, with an ordinal-stability test (`Off=0, Conservative=1, Aggressive=2`). | MUST | #16 §6.2 precedent |
| FR-DM-014 | Per-agent dwell state (`DwellTicks`, `LastMarkerId`) enters the canonical snapshot with a `SNAPSHOT_SCHEMA_VERSION` bump when the match-engine writer lands; field order pinned in Appendix B before wiring. | MUST | KD-5 / #16 |
| FR-DM-015 | Routing follows the #21 pattern: the match-engine Phase-D assembly layer is the sole populator of the `DismarkIntensity` routing field on the #12 snapshot and the #8 `TacticalContext`; translation runs once per tactic change, never per agent per tick. | MUST | §4.3 |
| FR-DM-016 | All constants live in the owning assembly's existing catalogue (`PositioningAIConstants` / `TacticalWeights`), each with exactly one source tag; no magic numbers in formula code. | MUST | CLAUDE.md / #20 |
| FR-DM-017 | Every §3 formula includes units, valid input ranges, and at least one worked example. | MUST | CLAUDE.md |
| FR-DM-018 | No interface or accessor is produced against an unspecified consumer (no phantom interfaces); the marker-side counter-behaviour hook is a §7 deferral, not an interface. | MUST | CLAUDE.md / #20 |

## 2.2 Data structures

### 2.2.1 `MarkingDwellState` (per agent, persistent across heartbeats)

| Field | Type | Notes |
|---|---|---|
| DwellTicks | `int ≥ 0` | heartbeats a perceived opponent has stayed within `MARKING_RADIUS_M`; capped at `MARKING_DWELL_FULL_TICKS` |
| LastMarkerId | `int` | `AgentId` of the nearest qualifying perceived opponent last tick; `-1` = none |

Zero-init (`DwellTicks = 0, LastMarkerId = -1`) is the valid "unmarked" state — no ctor seeding
required (KD-4 discipline applied to state as well as enums).

### 2.2.2 `DismarkIntensity` (new enum, #21-owned after back-prop)

`Off = 0` (identity), `Conservative = 1`, `Aggressive = 2`. Appended to `TeamTactic` after
`MarkingOrientation` (canonical field order, Appendix B).

### 2.2.3 Routing fields

- #12: `PositioningPerceptionSnapshot` gains `DismarkIntensity` (zero value = `Off` = identity — safe unseeded).
- #8: `TacticalContext` gains `DismarkIntensity` (same zero-value-identity contract; `Stage0Default` needs no special seed).

## 2.3 Serialization

At wiring time the match-engine serializer adds, per agent in roster order: `DwellTicks` (int32),
`LastMarkerId` (int32); plus the active+pending `TeamTactic.DismarkIntensity` byte already covered by
the existing `WriteTeamTactic` block once the #21 field is appended. One `SNAPSHOT_SCHEMA_VERSION`
bump covers both.

## 2.4 Cross-spec back-props (filed at `APPROVED` per pipeline step 6)

| Pending ERR | Target | Amendment |
|---|---|---|
| ERR-021-NNN (to file) | #21 §2.2.1 / Appendix B | `TeamTactic.DismarkIntensity` field + canonical order row + `WriteTeamTactic` coverage |
| ERR-012-NNN (to file) | #12 §4 | `SlotComposer` pipeline gains the dismark offset stage (order pinned by FR-DM-008) |
| ERR-008-NNN (to file) | #8 §3.2 | `UtilityScorer` marked-pass-target multiplier row |

## 2.5 Failure modes

| F | Mode | Handling |
|---|---|---|
| F1 | Non-finite perceived position reaches the evaluator | NaN-gate per the project pattern (`!(d > 0f) || IsInfinity(d)` class): treat as no marker this tick (decay path); never propagate NaN into an offset |
| F2 | `DwellTicks` deserialized negative or above cap | fail loud at the snapshot seam (`ArgumentException`), matching the living-world validating-seam precedent |
| F3 | Marker exactly coincident with agent (`d < DISMARK_MIN_MARKER_DIST_EPS`) | skip offset this tick (deterministic no-op); pressure still computed from dwell |
| F4 | `DismarkIntensity` byte outside defined members at a routing seam | refuse at the seam (fail loud), never silently clamp |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial FR set (18), data structures, back-prop table, failure modes. |
#endregion
