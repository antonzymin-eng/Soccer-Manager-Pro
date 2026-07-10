# Dismarking & Marker-Awareness AI Specification #23 — Appendices

**Created:** July 8, 2026
**Last Updated:** July 8, 2026 (v0.1)
**Version:** 0.1
**Status:** APPROVED

---

## Appendix A — Constant catalogue (authoritative)

| Constant | Tag | Value | Units | Home | Rationale |
|---|---|---|---|---|---|
| `MARKING_RADIUS_M` | `[GT]` | 3.0 | m | `PositioningAIConstants` | tight-marking engagement distance; sits between #14's mark-candidate scale and touch-tight range |
| `MARKING_DWELL_FULL_TICKS` | `[GT]` | 10 | heartbeats | `PositioningAIConstants` | 1.0 s of sustained attention ⇒ fully "marked" |
| `MARKING_DWELL_DECAY_PER_TICK` | `[GT]` | 2 | heartbeats | `PositioningAIConstants` | full decay in 0.5 s — release is faster than establishment |
| `DISMARK_PRESSURE_FLOOR` | `[GT]` | 0.15 | — | `PositioningAIConstants` | dead-band so incidental proximity never nudges targets |
| `DISMARK_OFFSET_MAX_M` | `[GT]` | 2.5 | m | `PositioningAIConstants` | bounded by spacing-stage separation scale so the offset cannot defeat spacing |
| `DISMARK_INTENSITY_SCALAR` | `[GT]` | 0.0 / 0.6 / 1.0 | — | `PositioningAIConstants` | Off row is the identity (KD-4) |
| `TARGET_MARKED_UTILITY_MULT` | `[GT]` | 0.7 | — | #8 `TacticalWeights` | same magnitude class as the Mentality risk multipliers |
| `DISMARK_MIN_MARKER_DIST_EPS` | `[FIXED]` | 1e-3 | m | `PositioningAIConstants` | degenerate-normalize guard; not tunable |

## Appendix B — Snapshot field order (pinned before wiring, FR-DM-014)

Per agent, roster order, appended after the existing per-agent AI-state block:

1. `MarkingDwellState.DwellTicks` (int32)
2. `MarkingDwellState.LastMarkerId` (int32)

`TeamTactic.DismarkIntensity` (byte) rides the existing `WriteTeamTactic` block, appended after
`MarkingOrientation` (the #21 Appendix B amendment, §2.4).

## Appendix C — FR traceability matrix

Completed as tests land (§5.4); seeded here with the v0.1 mapping:

| FR | Tests |
|---|---|
| FR-DM-001/004 | T-DM-U-003 |
| FR-DM-002 | T-DM-U-001/002 |
| FR-DM-003 | T-DM-U-004/005, T-DM-I-003 |
| FR-DM-005 | (mechanical: signature audit at implementation AR) |
| FR-DM-006 | T-DM-U-006 |
| FR-DM-007 | T-DM-I-002 |
| FR-DM-008 | T-DM-I-001 |
| FR-DM-009 | T-DM-U-008/009 |
| FR-DM-010/011 | T-DM-U-011/012 |
| FR-DM-012 | T-DM-U-010, T-DM-DET-002 |
| FR-DM-013 | T-DM-U-013 |
| FR-DM-014 | T-DM-I-005, T-DM-DET-003, T-DM-U-014 |
| FR-DM-015 | T-DM-I-004 |
| FR-DM-016/017/018 | (mechanical/doc audits at PASS-1 and implementation AR) |

## Appendix D — Sensitivity notes

- `MARKING_RADIUS_M` couples §3.1 and §3.4 (shared by design — one definition of "tight"); tuning
  it moves both consumers together. If the balance pass wants them decoupled, split into two `[GT]`
  constants *then*, with the §3.4 one `[DERIVED]` = the §3.1 one until proven otherwise.
- Dwell asymmetry (slow build, fast decay) is the stability knob: raising
  `MARKING_DWELL_DECAY_PER_TICK` toward the cap makes pressure flickery; 2 gives a 5-tick release.
- `DISMARK_OFFSET_MAX_M` must stay below the spacing-stage separation distance or the two stages
  fight; record the inequality as a test assertion at implementation (`BalancePassInvariants`
  pattern).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial appendices: constants, snapshot order, traceability seed, sensitivity. |
#endregion
