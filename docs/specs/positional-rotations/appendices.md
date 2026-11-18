# Positional Rotations Specification #25 — Appendices

**Created:** July 8, 2026
**Last Updated:** November 9, 2026 (v0.4 — ERR-025-001: v29 metadata/count/contiguous-row order, strict validation and byte arithmetic.)
**Version:** 0.4
**Status:** APPROVED

---

## Appendix A — Rotation adjacency tables (`[GT]` data)

All three `FormationFamily` tables (F442 / F433 / F4231 — the complete enum roster, verified
against `src/positioning-ai/FormationFamily.cs`) are authored below, closing the v0.1/v0.2 §9.1
completeness item. Each table is hand-audited against the F1 invariants: GK-free (slot 0 excluded),
valid slot names per the family's `PositioningAIConstants.Family*` table, distinct rows, ≤ 8 rows
(`ROTATION_MAX_PAIRS_PER_FAMILY`). Slot naming follows the family's #12 formation table order.
Row order within each table is commit priority (FR-RO-009): flank exchanges (most common, least
disruptive) rank above central ones.

### A.1 4-4-2 (`Family442`: GK, LB, CB1, CB2, RB, LM, CM1, CM2, RM, ST1, ST2)

| Row | Pair | Rationale |
|---|---|---|
| 0 | LB ↔ LM | flank underlap/overlap exchange |
| 1 | RB ↔ RM | flank underlap/overlap exchange |
| 2 | LCM ↔ RCM | pivot box rotation |
| 3 | LM ↔ LST | inside-forward drift exchange |
| 4 | RM ↔ RST | inside-forward drift exchange |

*(LCM/RCM = CM1/CM2 and LST/RST = ST1/ST2 in the `Family442` source order — the left/right
qualifiers reflect the slots' LateralPct 0.38/0.62 and 0.42/0.58 placements.)*

### A.2 4-3-3 (`Family433`: GK, LB, CB1, CB2, RB, DM, CM1, CM2, LW, ST, RW)

| Row | Pair | Rationale |
|---|---|---|
| 0 | LB ↔ LW | flank underlap/overlap exchange |
| 1 | RB ↔ RW | flank underlap/overlap exchange |
| 2 | CM1 ↔ CM2 | interior-eight box rotation |
| 3 | LW ↔ ST | wide-forward / false-nine drift exchange |
| 4 | RW ↔ ST | wide-forward / false-nine drift exchange |

The single pivot (DM) is deliberately excluded from every pair: it anchors the rest-defence shape
and has no like-for-like partner in this family — a DM rotation would vacate the sole holding slot
(the same rest-defence concern #12 §7.13 evaluates). 5 rows ≤ 8 ✓; GK-free ✓; all slots exist in
`Family433` ✓; rows distinct ✓ (LW/RW/ST appear in two rows each — legal, the FR-RO-008 partner
lock prevents concurrent commits sharing a slot).

### A.3 4-2-3-1 (`Family4231`: GK, LB, CB1, CB2, RB, DM1, DM2, LAM, CAM, RAM, ST)

| Row | Pair | Rationale |
|---|---|---|
| 0 | LB ↔ LAM | flank underlap/overlap exchange |
| 1 | RB ↔ RAM | flank underlap/overlap exchange |
| 2 | DM1 ↔ DM2 | double-pivot box rotation |
| 3 | CAM ↔ ST | ten–nine vertical exchange |
| 4 | LAM ↔ CAM | attacking-band interchange |
| 5 | RAM ↔ CAM | attacking-band interchange |

6 rows ≤ 8 ✓; GK-free ✓; all slots exist in `Family4231` ✓; rows distinct ✓ (LAM/RAM/CAM/ST shared
across rows under the FR-RO-008 partner lock as above). The double pivot rotates as a pair (row 2)
— unlike the 4-3-3 single pivot, swapping DM1 ↔ DM2 never vacates the holding band.

## Appendix B — Snapshot field order (pinned before wiring, FR-RO-013)

World-state v29 (ERR-025-001), in the engine's existing little-endian canonical encoding:

- Immediately after `uint32 worldSchema`, `uint64 tick`: team order 0,1, each
  `uint8 initialized` followed by `int32 effective TacticFormation` (10 bytes total).
  Validate this before reading any positioning hysteresis; select the saved family first.
- At each team's existing rotation location: `int32 rowCount`, then:
  1. Per agent, roster order: `SlotIndex` int32 (binding permutation).
  2. Per agent, roster order: `LastComposedTarget.X/Y` float32 (cache).
  3. Per pair, table-row order: `TriggerDwellTicks` int32, `Rotated` uint8,
     `HoldTicksRemaining` int32. Each row is 9 bytes; do not split row 5 into a trailer.
- The existing `TeamTactic.RotationFreedom` uses int32 through `WriteTeamTactic`,
  despite the enum's byte backing; its previous position and all tactic bytes stay unchanged.

For eleven agents, each block has 132 agent bytes plus 4 count bytes plus 45/54 row
bytes = 181/190 bytes. Compared with v28, total overhead is 18 bytes for two five-row
teams, 27 for one six-row team, 36 for two. This owner-approved one-off layout change
excepts the repository snapshot skill's append-last rule; v28 is rejected, not migrated.
The reader mirrors this order and rejects mismatched counts before reading rows.

Restore's initialized metadata must be 0/1, effective family one of F442/F433/F4231.
Uninitialized is allowed only for a synthetic tick-0 F442 body. An initialized tick-0
body retains its saved choice and cannot resample on its first resumed tick. Requested
and effective formations may differ legally after kickoff (#21 §4.6.1).

## Appendix C — FR traceability matrix (completed as tests land)

| FR | Tests |
|---|---|
| FR-RO-001/008 | T-RO-U-008, T-RO-I-001/003 |
| FR-RO-002/003/016 | T-RO-U-010 |
| FR-RO-004 | T-RO-U-001/002/003 |
| FR-RO-005 | T-RO-U-004 |
| FR-RO-006 | T-RO-U-005 |
| FR-RO-007 | T-RO-U-013 |
| FR-RO-009 | T-RO-U-006/007 |
| FR-RO-010 | T-RO-U-003, T-RO-I-004 |
| FR-RO-011 | T-RO-DET-002 |
| FR-RO-012 | T-RO-U-011 |
| FR-RO-013 | T-RO-U-012, T-RO-I-006, T-RO-DET-003 |
| FR-RO-014 | T-RO-I-005 |
| FR-RO-015/017/018 | mechanical/doc audits at PASS-1 + implementation AR |

## Appendix D — FR-RO-007 derivation (hysteresis non-interference)

`ShapeAnalyzer` re-sorts lines/lanes with its own dwell `D_line` (the `AgentHysteresisState`
constants). If a rotation could revert faster than a line re-sort can commit
(`ROTATION_HOLD_TICKS < D_line`), a swap could flip bindings back while the analyzer is still
mid-dwell toward the *post-swap* line assignment, and the two systems would chase each other's
transients (the supplement's KD-4(c) oscillation hazard). Requiring

```
ROTATION_HOLD_TICKS ≥ D_line        [DERIVED lower bound]
```

guarantees the analyzer's dwell always resolves against a stable binding before the binding can
change again. Verified against source (PASS-1 L-3): `PositioningAIConstants.LINE_DWELL_TICKS = 5`,
so the catalogue value 30 satisfies the bound with 6× margin; T-RO-U-013 locks the inequality
against future retuning of either side. (The reverse direction needs no bound: the
analyzer re-sorting mid-hold is harmless — it operates on positions and the binding is stable.)

## Appendix E — Sensitivity notes

- `ROTATION_ADVANTAGE_M` is the primary chaos knob: below ~2 m casual overlaps trigger; above ~6 m
  only fully-committed exchanges ratify. The 1.5× Conservative scalar narrows without a second
  constant.
- The §4.2 one-tick-stale target read biases *against* rotation during fast target movement — a
  conservative failure direction (missed rotation, never a spurious one).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial appendices: 4-4-2 exemplar, snapshot order, traceability, FR-RO-007 derivation. |
| 0.2 | 2026-07-08 | — | PASS-1: Appendix B gains the `LastComposedTarget` block (H-1); Appendix D line-dwell value verified = 5 (L-3). |
| 0.3 | 2026-07-10 | — | Appendix A completed: A.2 (4-3-3, 5 rows) + A.3 (4-2-3-1, 6 rows) authored against the verified `Family433`/`Family4231` slot rosters; A.1 slot-name↔source-order mapping note added; F1 hand-audit recorded per table. Closes the §9.1 family-completeness item. |
| 0.4 | November 9, 2026 | — | ERR-025-001: v29 metadata/count/contiguous-row order, strict validation and byte arithmetic. |
#endregion
