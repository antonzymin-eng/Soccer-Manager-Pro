# Positional Rotations Specification #25 — Section 5: Test Plan

**Created:** July 8, 2026
**Last Updated:** July 8, 2026 (v0.1)
**Version:** 0.1
**Status:** IN REVIEW

---

## 5.1 Unit

| ID | Locks |
|---|---|
| T-RO-U-001 | §3.1 worked example: swapGains 12.3/13.0; predicate true at Conservative, and the strict-total-displacement inequality holds |
| T-RO-U-002 | One-sided drift (swapGain(B) < advantage) ⇒ predicate false |
| T-RO-U-003 | Phase ∉ {InPoss, TransToAtk} ⇒ predicate false; dwell frozen not reset on phase exit mid-accumulation (FR-RO-010 nuance: predicate-miss resets, phase-exit freezes) |
| T-RO-U-004 | Dwell: 5 consecutive holds commit; a miss at tick 4 resets to 0 |
| T-RO-U-005 | Hold: revert not evaluable before `ROTATION_HOLD_TICKS`; §3.2 worked-example timeline (commit 204, revert 238) |
| T-RO-U-006 | Cap: two pairs simultaneously eligible ⇒ only the lower table row commits this tick; the other's dwell continues |
| T-RO-U-007 | Partner lock: rows sharing a rotated agent are skipped with dwell reset (no chained motion) |
| T-RO-U-008 | Atomicity: post-controller bindings are always a permutation; both writes in one step (F3 direction rule) |
| T-RO-U-009 | NaN position/target ⇒ predicate false, dwell reset, no NaN propagation (F4) |
| T-RO-U-010 | Adjacency catalogue invariants: GK-free, valid indices, distinct pairs, row cap (F1) |
| T-RO-U-011 | `RotationFreedom` ordinal stability (Off=0/Conservative=1/Free=2) |
| T-RO-U-012 | Restore seam refuses a non-permutation `SlotIndex` set (F2) |
| T-RO-U-013 | FR-RO-007 invariant: `ROTATION_HOLD_TICKS ≥` line-dwell constant (BalancePassInvariants style) |

## 5.2 Integration

| ID | Locks |
|---|---|
| T-RO-I-001 | Controller→Composer→ShapeAnalyzer order: post-swap tick composes both agents at their new targets; lines/lanes re-sort sees post-swap bindings |
| T-RO-I-002 | Away-team mirror: identical scenario mirrored for the away team rotates the mirrored pair (ERR-008-002 class) |
| T-RO-I-003 | Previous-tick-target read: a swap does not recompose within its own tick (§4.2) |
| T-RO-I-004 | Turnover mid-rotation: bindings persist through OutOfPoss; no snap-home (FR-RO-010) |
| T-RO-I-005 | Phase-D routing per team via `TestOnly_SlotBinding`; teams independent |
| T-RO-I-006 | Schema probe: permutation + pair state + dial feed the digest (at wiring) |

## 5.3 Determinism / closed-loop

| ID | Locks |
|---|---|
| T-RO-DET-001 | Two same-seed runs with `Free`: bitwise-identical digests |
| T-RO-DET-002 | Default (`Off`) run digest-identical to pre-#25 |
| T-RO-DET-003 | Save/restore with an active rotation (mid-hold) resumes byte-identically, bindings included |
| sim_rotation-ratifies-exchange | #19 scenario: scripted movement drives an organic LM/LB exchange under `Free`; envelope asserts exactly one rotation commits, total displacement decreases vs the `Off` baseline, and no oscillation occurs over 20 s |

## 5.4 FR traceability

Matrix in Appendix C, completed as tests land.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial plan: 13 unit, 6 integration, 3 determinism + 1 scenario. |
#endregion
