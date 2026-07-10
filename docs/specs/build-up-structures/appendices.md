# Scripted Build-Up Structures Specification #24 — Appendices

**Created:** July 8, 2026
**Last Updated:** July 8, 2026 (v0.1)
**Version:** 0.1
**Status:** APPROVED

---

## Appendix A — Overlay catalogue (all rows `[GT]`, metres, team-relative; lateral magnitude resolves toward pitch centre per lane side, §3.2)

Unlisted (structure, zone, line, lane) combinations are (0, 0). FinalThird is all-zero by rule
(FR-BU-004).

### A.1 `BackThree` (lane keys corrected per PASS-1 M-3 — fullbacks occupy the wide L/R lanes)

| Zone | Line | Lane | Δx | Δy (toward centre) | Meaning |
|---|---|---|---|---|---|
| OwnThird | DEF | L/R (wide) | −4.0 | 6.0 | fullbacks tuck beside CBs |
| OwnThird | MID | LH/RH | −4.0 | 0.0 | central pair drops toward the back line |
| MiddleThird | DEF | L/R (wide) | −2.0 | 3.0 | partial tuck while progressing |
| MiddleThird | MID | LH/RH | −2.0 | 0.0 | half-drop |

A lane-keyed table cannot single out one of two half-space midfielders as "the pivot" — both LH/RH
slots take the row. Slot-specific rows (per-`SlotIndex` overlays) are a §7.6 deferral.

### A.2 `DoublePivot`

| Zone | Line | Lane | Δx | Δy (toward centre) | Meaning |
|---|---|---|---|---|---|
| OwnThird | MID | LH/RH | −5.0 | 4.0 | two half-space mids form the pivot pair |
| OwnThird | ATT | LH/RH | −3.0 | 0.0 | inner forwards drop to link |
| MiddleThird | MID | LH/RH | −2.5 | 2.0 | pivot pair holds shape while progressing |

### A.3 `InvertedFullBacks`

| Zone | Line | Lane | Δx | Δy (toward centre) | Meaning |
|---|---|---|---|---|---|
| OwnThird | DEF | L/R (wide) | 2.0 | 6.0 | fullbacks step in and up toward the half-space |
| MiddleThird | DEF | L/R (wide) | 3.0 | 8.0 | fuller inversion in the progression phase |

All rows satisfy FR-BU-008 (≤ 8.0 m componentwise; T-BU-U-004 enforces this mechanically). Note the
inversion is deliberately bounded to a half-space step, not a full drift to the centre — a full
inversion (Δy ≈ 12 m) would exceed the bound and fight `SpacingResolver`; if the balance pass wants
it, `BUILDUP_OFFSET_MAX_M` must rise with it (co-tuning constraint, Appendix D).

## Appendix B — Snapshot field order (pinned before wiring, FR-BU-011)

Per team, team-index order, appended after the existing per-team AI-state block:

1. `BuildUpZoneState.CommittedZone` (byte)
2. `BuildUpZoneState.SuppressTicksRemaining` (int32)

`TeamTactic.BuildUpStructure` (byte) rides `WriteTeamTactic` per the §2.2.1 append-order
coordination rule.

## Appendix C — FR traceability matrix (completed as tests land)

| FR | Tests |
|---|---|
| FR-BU-002/003 | T-BU-U-001/002, T-BU-I-002 |
| FR-BU-004 | T-BU-U-007, T-BU-I-003 |
| FR-BU-005 | T-BU-U-005, T-BU-DET-002 |
| FR-BU-006 | T-BU-U-008/009, T-BU-I-004 |
| FR-BU-007/008 | T-BU-U-004/012, T-BU-I-001 |
| FR-BU-009 | T-BU-U-006 |
| FR-BU-010 | T-BU-U-010 |
| FR-BU-011 | T-BU-I-006, T-BU-DET-003, T-BU-U-011 |
| FR-BU-012 | T-BU-I-005 |
| FR-BU-001/013..016 | mechanical/doc audits at PASS-1 + implementation AR |

## Appendix D — Sensitivity notes

- `BUILDUP_ZONE_HYSTERESIS_M` trades responsiveness against flap; 2.0 m ≈ two ball radii of
  progression beyond a third boundary before the team re-shapes.
- `REGAIN_SUPPRESS_TICKS` interacts with #21 `TransitionHoldTicks` (the Mentality profile's
  transition dimension); the balance pass should co-tune them and record the relationship.
- The A.3 inversion rows are the largest displacements in the catalogue and the most likely to
  fight `SpacingResolver`; the sim scenario should include an `InvertedFullBacks` variant if PASS-1
  keeps the structure.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial appendices; A.3 inversion bounded to the half-space step so the whole catalogue satisfies FR-BU-008. |
| 0.2 | 2026-07-08 | — | PASS-1 M-3: A.1/A.2 lane keys corrected to actual formation lane occupancy (fullbacks L/R wide; mid pair LH/RH); slot-specific rows recorded as the §7.6 deferral. |
#endregion
