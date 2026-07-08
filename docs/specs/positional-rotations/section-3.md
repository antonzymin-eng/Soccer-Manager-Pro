# Positional Rotations Specification #25 — Section 3: Formulas and Algorithms

**Created:** July 8, 2026
**Last Updated:** July 8, 2026 (v0.1)
**Version:** 0.1
**Status:** IN REVIEW

---

## 3.1 Trigger predicate (FM-RO-01)

For adjacency pair (A, B) with composed slot targets `tA`, `tB` (this tick's targets under the
**current** binding, computed by the previous stages — see §4.2 ordering note) and positions
`pA`, `pB`:

```
advantage = ROTATION_ADVANTAGE_M × ROTATION_ADVANTAGE_SCALAR[dial]          [m]
swapGain(A) = |pA − tA| − |pA − tB|      # how much closer A is to B's target
swapGain(B) = |pB − tB| − |pB − tA|
predicate  = swapGain(A) ≥ advantage AND swapGain(B) ≥ advantage
             AND phase ∈ {InPoss, TransToAtk}
```

- **Units/ranges:** metres; `ROTATION_ADVANTAGE_M > 0` `[GT]` = 4.0;
  `ROTATION_ADVANTAGE_SCALAR`: `Off → +∞` (never — implemented as the dial gate, not arithmetic),
  `Conservative → 1.5`, `Free → 1.0` `[GT]`.
- **Both-sided by design:** one agent drifting is not an exchange; requiring positive gain on both
  sides means the swap strictly reduces total displacement — the geometric guarantee that rotating
  is better than running home (`|pA−tB| + |pB−tA| < |pA−tA| + |pB−tB| − 2×advantage`).
- **Worked example:** left-mid A at (60, 10) with target (45, 12); left-back B at (44, 14) with
  target (58, 8). |pA−tA| = 15.1, |pA−tB| = 2.8 → swapGain(A) = 12.3. |pB−tB| = 15.2,
  |pB−tA| = 2.2 → swapGain(B) = 13.0. At `Conservative` (advantage = 6.0): both ≥ 6.0 → predicate
  true; dwell starts counting.

## 3.2 Dwell and commit (FM-RO-02)

Per pair, per heartbeat, in ascending Appendix-A row order, skipping pairs whose agents are
partner-locked (FR-RO-009):

```
if !Rotated:
    TriggerDwellTicks = predicate ? TriggerDwellTicks + 1 : 0
    if TriggerDwellTicks ≥ ROTATION_TRIGGER_DWELL_TICKS and teamCommitsThisTick < ROTATION_MAX_PER_TICK:
        swap SlotIndex(A) ↔ SlotIndex(B)          # atomic, §3.3
        Rotated = true; HoldTicksRemaining = ROTATION_HOLD_TICKS; TriggerDwellTicks = 0
        teamCommitsThisTick += 1
else:
    HoldTicksRemaining = max(0, HoldTicksRemaining − 1)
    if HoldTicksRemaining == 0:
        revertPredicate = same §3.1 form against the swapped targets
        TriggerDwellTicks = revertPredicate ? TriggerDwellTicks + 1 : 0
        if TriggerDwellTicks ≥ ROTATION_TRIGGER_DWELL_TICKS and teamCommitsThisTick < ROTATION_MAX_PER_TICK:
            swap back; Rotated = false; TriggerDwellTicks = 0
            teamCommitsThisTick += 1
```

`ROTATION_TRIGGER_DWELL_TICKS` `[GT]` = 5 (0.5 s of sustained exchange), `ROTATION_HOLD_TICKS`
`[GT]` = 30 (3.0 s minimum hold), subject to the FR-RO-007 `[DERIVED]` lower bound (Appendix D).

- **Worked example:** predicate first true at heartbeat 200 and holds → commit at 204 (5th
  consecutive). Revert evaluable from 234; if the mirrored predicate then holds 230–238
  continuously, dwell restarts at 234 (post-hold) and revert commits at 238.

## 3.3 Atomic swap and partner lock

The swap writes both agents' `SlotIndex` in one controller step before any downstream stage runs
(FR-RO-008); there is no observable intermediate. While `Rotated`, both agents are partner-locked:
rows in Appendix A sharing either agent are skipped (their dwell resets to 0), so chained/cyclic
motion cannot emerge from pairwise rules (cyclic rotation is §7.1, deliberately excluded).

## 3.4 Phase-exit behaviour

Leaving {`InPoss`, `TransToAtk`} freezes dwell accumulation and the hold countdown continues;
committed rotations persist (FR-RO-010). Rationale: an instant revert on turnover would teleport
defensive responsibilities across the pitch at the worst moment; the swapped agents simply *are*
the shape until play allows an organic revert. `ShapeAnalyzer`'s existing line/lane re-sort (which
runs on positions, not bindings) keeps defensive organisation coherent meanwhile — the KD-5
ordering contract is what makes this safe.

## 3.5 Constants

| Constant | Tag | Value | Units |
|---|---|---|---|
| `ROTATION_ADVANTAGE_M` | `[GT]` | 4.0 | m |
| `ROTATION_ADVANTAGE_SCALAR` (Cons/Free) | `[GT]` | 1.5 / 1.0 | — |
| `ROTATION_TRIGGER_DWELL_TICKS` | `[GT]` | 5 | heartbeats |
| `ROTATION_HOLD_TICKS` | `[GT]` | 30 | heartbeats (≥ line-dwell bound, FR-RO-007 `[DERIVED]` constraint) |
| `ROTATION_MAX_PER_TICK` | `[GT]` | 1 | commits/team/heartbeat |
| `ROTATION_MAX_PAIRS_PER_FAMILY` | `[FIXED]` | 8 | table rows (bounds evaluation cost; a cap, not a tunable) |

`[GT]` magnitudes pinned at the balance pass (#21 G2 precedent).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial FM-RO-01..02 + atomicity/partner-lock/phase-exit semantics. |
#endregion
