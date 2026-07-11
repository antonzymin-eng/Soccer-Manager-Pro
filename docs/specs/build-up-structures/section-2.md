# Scripted Build-Up Structures Specification #24 — Section 2: Functional Requirements, Data Structures, Failure Modes

**Created:** July 8, 2026
**Last Updated:** July 10, 2026 (v0.3 — §2.3 back-props filed: ERR-021-006 / ERR-012-008; append order pinned)
**Version:** 0.3
**Status:** APPROVED

---

## 2.1 Functional Requirements

| FR | Subject | Conformance | Source |
|---|---|---|---|
| FR-BU-001 | No new `ActionType`, no #8 change: the overlay affects composed positioning targets only. | MUST | KD-1 |
| FR-BU-002 | Ball-progression zone is classified from **team-relative** ball X into {OwnThird, MiddleThird, FinalThird} per §3.1; the same physical ball position classifies mirror-symmetrically for the two teams. | MUST | KD-2 / §1.2 |
| FR-BU-003 | Zone transitions apply hysteresis: the boundary must be crossed by more than `BUILDUP_ZONE_HYSTERESIS_M` beyond the threshold to commit a new zone; otherwise the committed zone holds (§3.1). | MUST | KD-2 |
| FR-BU-004 | The overlay is active only when `BuildUpStructure ≠ None` AND phase = `Phase.InPoss` AND committed zone ∈ {OwnThird, MiddleThird} AND the post-regain suppression window (§3.3) is closed. | MUST | KD-3 |
| FR-BU-005 | `BuildUpStructure.None` (zero value) is the exact identity: zero offsets, and a default match is byte-identical to pre-#24. | MUST | KD-6 |
| FR-BU-006 | `TeamTactic.TransitionWon ∈ {CounterAttack, CounterPress}` opens a suppression window of `REGAIN_SUPPRESS_TICKS` heartbeats on each **team-level regain** — the possessing *team* (derived from the possession-changed signal's holder ids) transitions opponent → this team. Intra-team possessor changes MUST NOT arm the window (PASS-1 M-1: the raw per-agent event fires on teammate receptions too); `{HoldShape, Regroup}` opens none. | MUST | KD-3 / §3.3 |
| FR-BU-007 | Overlay offsets are additive per-slot displacements from the structure catalogue (Appendix A), keyed by `(BuildUpStructure, zone, LineId, LaneId)`; applied after `ContextModifier`, before `SpacingResolver` and the pitch clamp. | MUST | KD-4 / §3.2 |
| FR-BU-008 | Offsets are bounded: every catalogue entry satisfies `|Δx| ≤ BUILDUP_OFFSET_MAX_M` and `|Δy| ≤ BUILDUP_OFFSET_MAX_M`, enforced by a catalogue-invariant test. | MUST | §3.2 |
| FR-BU-009 | The goalkeeper receives no overlay offset (GK positioning stays #11/#12-owned). | MUST | §3.2 |
| FR-BU-010 | `BuildUpStructure` is `byte`-backed, APPEND-only, ordinal-stability-tested (`None=0, BackThree=1, DoublePivot=2, InvertedFullBacks=3`). | MUST | #16 §6.2 precedent |
| FR-BU-011 | Per-team persistent state (committed zone, zone-boundary side, suppression-ticks-remaining) enters the canonical snapshot with a `SNAPSHOT_SCHEMA_VERSION` bump at wiring; field order pinned in Appendix B. | MUST | KD-6 / #16 |
| FR-BU-012 | Routing per the #21 pattern: Phase-D writer is the sole populator of the #12 snapshot's `BuildUpStructure` field; translation once per tactic change. | MUST | §4.3 |
| FR-BU-013 | The classifier and overlay are pure deterministic functions; no RNG draw site, no domain tag. | MUST | KD-7 |
| FR-BU-014 | All constants live in `PositioningAIConstants` with exactly one tag each; catalogue tables are `[GT]` data. | MUST | #20 / CLAUDE.md |
| FR-BU-015 | Every §3 formula/table has units, ranges, and a worked example. | MUST | CLAUDE.md |
| FR-BU-016 | No phantom interfaces; the pass-pattern-scripting idea stays a §7 deferral with no hook. | MUST | CLAUDE.md / #20 |

## 2.2 Data structures

### 2.2.1 `BuildUpStructure` (new enum, #21-owned after back-prop)

`None = 0` (identity), `BackThree = 1`, `DoublePivot = 2`, `InvertedFullBacks = 3`. Appended to
`TeamTactic` after `DismarkIntensity` if #23 lands first, else after `MarkingOrientation`
(coordination rule: append order = spec-approval order; pinned in each spec's Appendix B at
back-prop time — the renumbering-cascade hazard applied to field order). **PINNED July 10, 2026
(ERR-021-006):** #23/#24/#25 approved in one pass; the order is `MarkingOrientation` →
`DismarkIntensity` (#23) → **`BuildUpStructure` (#24)** → `RotationFreedom` (#25), recorded in #21
Appendix B v0.5.

### 2.2.2 `BuildUpZoneState` (per team, persistent)

| Field | Type | Notes |
|---|---|---|
| CommittedZone | `byte` (zone enum) | zero value = OwnThird — a valid kickoff-adjacent default, but seeded from actual ball X at boot anyway |
| SuppressTicksRemaining | `int ≥ 0` | post-regain window countdown; 0 = closed |

### 2.2.3 Overlay catalogue rows

`readonly struct BuildUpOffset { float Dx; float Dy; }` in metres, team-relative frame; tables in
Appendix A. Rows are addressed by the slot's **existing** `FormationSlotRecord.DefaultLine` /
`DefaultLane` — no new slot identity is introduced (that is #25's territory, deliberately not
duplicated here).

## 2.3 Cross-spec back-props (filed at `APPROVED`)

**FILED AND LANDED July 10, 2026, atomically with `APPROVED`** (spec-error-log.md v1.30):

| ERR | Target | Amendment |
|---|---|---|
| **ERR-021-006** (filed, resolved) | #21 §2.2.1 / Appendix B | `TeamTactic.BuildUpStructure` field + order row (after `DismarkIntensity` — append order pinned #23 → #24 → #25 per §2.2.1; `tactical-instructions/section-2.md` v0.5 + `appendices.md` v0.5) |
| **ERR-012-008** (filed, resolved) | #12 §3.7.1 | `SlotComposer` overlay stage + zone classifier state (`positioning-ai/section-3.md` v0.6) |

## 2.4 Failure modes

| F | Mode | Handling |
|---|---|---|
| F1 | Non-finite ball X reaches the classifier | hold the committed zone (decay-free no-op), never classify from NaN |
| F2 | Deserialized `SuppressTicksRemaining` negative or > `REGAIN_SUPPRESS_TICKS` cap, or `CommittedZone` byte > FinalThird (PASS-1 L-2) | fail loud at the snapshot seam |
| F3 | Undefined `BuildUpStructure` byte at a routing seam | refuse (fail loud), never clamp |
| F4 | Catalogue row exceeding `BUILDUP_OFFSET_MAX_M` | build-time invariant test failure (FR-BU-008) — cannot ship |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial FR set (16), data structures, back-props, failure modes. |
| 0.2 | 2026-07-08 | — | PASS-1 fixes: FR-BU-006 team-level-regain arming (M-1); F2 CommittedZone gate (L-2). |
| 0.3 | 2026-07-10 | — | §2.3 back-props FILED and landed atomically with `APPROVED`: ERR-021-006 / ERR-012-008 (spec-error-log.md v1.30); §2.2.1 append order PINNED (#23 → #24 → #25). |
#endregion
