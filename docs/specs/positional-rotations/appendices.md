# Positional Rotations Specification #25 — Appendices

**Created:** July 8, 2026
**Last Updated:** July 8, 2026 (v0.1)
**Version:** 0.1
**Status:** IN REVIEW

---

## Appendix A — Rotation adjacency tables (`[GT]` data)

v0.1 ships the 4-4-2 exemplar; the remaining `FormationFamily` tables are an explicit §9.1
completeness item to author before `APPROVED` (each family's table is hand-audited against F1
invariants: GK-free, valid, distinct, ≤ 8 rows). Slot naming follows the family's #12 formation
table order.

### A.1 4-4-2

| Row | Pair | Rationale |
|---|---|---|
| 0 | LB ↔ LM | flank underlap/overlap exchange |
| 1 | RB ↔ RM | flank underlap/overlap exchange |
| 2 | LCM ↔ RCM | pivot box rotation |
| 3 | LM ↔ LST | inside-forward drift exchange |
| 4 | RM ↔ RST | inside-forward drift exchange |

Row order is commit priority (FR-RO-009): flank exchanges (most common, least disruptive) rank
above central ones.

## Appendix B — Snapshot field order (pinned before wiring, FR-RO-013)

1. Per agent, roster order: `SlotIndex` (int32) — the binding permutation
2. Per pair, table-row order: `TriggerDwellTicks` (int32), `Rotated` (byte), `HoldTicksRemaining` (int32)
3. `TeamTactic.RotationFreedom` (byte) via `WriteTeamTactic` per the append-order coordination rule

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
change again. The catalogue value 30 sits well above today's line-dwell constant; T-RO-U-013 locks
the inequality against future retuning of either side. (The reverse direction needs no bound: the
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
#endregion
