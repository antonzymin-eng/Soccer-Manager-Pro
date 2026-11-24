# W13 default flip — off vs on full-match measurement

> **Created:** November 21, 2026 · **Purpose:** retain the descriptive measurement taken when W13 mark movement was
> switched on by default (owner decision OD-2), after the W13-F5..F10 blockers were fixed.
> **Source:** branch `claude/laughing-knuth-rj7vec`. The production source is identical to commit `cd1b204c` except
> comments: the matrix was built from `70aa11d4` + the flip edits, and the diffs since then in production files are
> comment-only or the switch default, which both arms override. **Toolchain:** Linux, .NET SDK 8.0.134 /
> runtime 8.0.34 (Ubuntu archive). **Non-certifying**: not the frozen lane (SDK 8.0.426 / CLR 8.0.32), not Unity, not
> the pinned host.

## What ran

`src/match-engine/tests/MarkingFlipMeasurementTests.cs` (env-gated, `[Category("Calibration")]`).

- **Matrix** (`matrix.csv`): 6 historical seeds × the #434 characterization cases C0–C9 × arms `off` / `on`, which is
  120 full matches of 324,000 ticks each. GK heading is enabled, as in the #434 harness.
  - C0: both F442.
  - C1 / C2: F433, home / away.
  - C3 / C4: F4231, home / away.
  - C5 / C6: all-Defend, home / away.
  - C7 / C8: all-Attack, home / away.
  - C9: both F4231.

  Each arm is set explicitly with `TestOnly_ArmMarkMovement(false / true)`, independent of the shipped default.
  Instrument v1.0 columns.
- **Knockdown anatomy** (`knockdown-anatomy-c0.csv`): C0 × 6 seeds × off / on with instrument v1.1. It adds
  entries into GROUNDED by reason, by whether the agent held a mark target at the last stride, and by
  possession state (defending / attacking / loose).
- `analysis.txt` is `analyze.py matrix.csv` verbatim. Reproduce with:

```bash
TD_MARKING_MEASURE_OUT=out.csv TD_MARKING_MEASURE_CASES=0,1,2,3,4,5,6,7,8,9 dotnet test src/match-engine/tests/match-engine-tests.gen.csproj --filter "FullyQualifiedName~MarkingFlip_FullMatchMeasurement"
```

```bash
python3 docs/tracking/evidence/w13-flip-measurement-20261121/analyze.py out.csv
```

`wallSeconds` was measured with three or four processes on a 4-core container, part of the time beside the full
gate. **It is not a performance measurement.**

## Results (descriptive; no realism band is approved, KD-W1)

**Hard stops:** non-finite agent state in 0 / 120 matches. Invariant and NaN locks are covered by the gate.

Per-match means, 60 paired matches per arm, off → on:

| Metric | Off | On | Change |
|---|---|---|---|
| Goals, home | 6.37 | 5.27 | −17% |
| Goals, away | 4.90 | 4.13 | −16% |
| Shots, home | 15.1 | 18.7 | +24% |
| Shots, away | 13.0 | 15.1 | +16% |
| Passes, per team | | | −8 to −9% |
| Tackles won | 0.77 | 1.53 | |
| Tackles loose | 2.37 | 4.93 | |
| Tackle fouls | 0.53 | 1.12 | |
| Tackles missed | 10.7 | 24.8 | |
| Yellow cards | 1.05 | 1.10 | |
| Grounded agent-ticks | 193,144 | 299,036 | +55% |
| OscillationGuard lock entries (all agents) | 61,750 | 48,954 | −21% |

Agent-strides with a mark target average 189,435 per match in the on arm.

**C0 scorelines, off | on:**

| Seed | Off | On |
|---|---|---|
| `0x0123456789ABCDEF` | 7-2 | 3-4 |
| `0x0F0F0F0F0F0F0F0F` | 4-4 | 3-3 |
| `0x1111111111111111` | 3-4 | 5-4 |
| `0x2222222222222222` | 3-2 | 4-6 |
| `0x3333333333333333` | 4-2 | 3-3 |
| `0xDEADBEEFCAFEBABE` | 6-7 | 3-3 |

Aggregate C0: 27-21 off, 21-23 on.

**#434 formation / Duty sensitivity** (the changed team's aggregate goals over 6 seeds, against its C0 base):

| Case | Off | On |
|---|---|---|
| C1 home F433 | 27 → 73 (×2.70) | 21 → 28 (×1.33) |
| C2 away F433 | 21 → 43 (×2.05) | 23 → 20 (×0.87) |
| C3 home F4231 | 27 → 42 (×1.56) | 21 → 44 (×2.10) |
| C4 away F4231 | 21 → 45 (×2.14) | 23 → 40 (×1.74) |
| C5 home all-Defend | ×0.48 | ×0.38 |
| C6 away all-Defend | ×0.57 | ×0.35 |
| C7 home all-Attack | 27 → 78 (×2.89) | 21 → 70 (×3.33) |
| C8 away all-Attack | ×2.48 | ×1.78 |
| C9 both F4231 (both teams' goals) | 48 → 80 (×1.67) | 44 → 67 (×1.52) |

**Reading.** Marking removes most of the F433 sensitivity. It does **not** remove the F4231 or Duty sensitivity:
home F4231 and home all-Attack get larger. The #434 player-control blocker is therefore **not** discharged by the
flip. Six seeds per case is a small sample, and single cases move by a few goals.

**Knockdown anatomy (C0, per-match means, off → on):**

| Metric | Off | On |
|---|---|---|
| Entries into GROUNDED | 1,884 | 2,799 (+49%) |
| — with `GroundedReason.COLLISION` | all | all |
| — sliding-tackle knockdowns | 0 | 0 |
| — with the ball loose | 1,708 | 2,485 |
| — of an agent holding a mark target | — | 470 |

With no carrier and no pass in flight, neither team is "in possession", so both teams compose mark points
(open issue `w13-loose-ball-knockdowns`). The baseline itself, about 1,900 collision knockdowns per match,
predates W13.

## Limits

These are descriptive data only. This is not the W13 plan's full §10 protocol: it has no `marking.bin` trace, no
§3 M1–M8 strata and no frozen lane. It is not a calibration and not certification. The identity probe
(`TD_MARK_IDENTITY_PROBE`) was not re-run. The schema moved to v30 and the default changed, so the earlier digests
are superseded by construction.

## Version history

| Version | Date | Notes |
|---|---|---|
| 1.0 | 2026-11-21 | Initial package: 120-match matrix, C0 knockdown anatomy, analysis. |
