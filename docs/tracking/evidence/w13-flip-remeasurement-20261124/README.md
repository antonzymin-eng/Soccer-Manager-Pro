# W13 marking re-measurement after PR #563

**Created November 24, 2026.** This repeats the [November 21 flip measurement](../w13-flip-measurement-20261121/README.md) on the merged [PR #563](https://github.com/antonzymin-eng/Soccer-Manager-Pro/pull/563) tree, which includes the tracking DECELERATING exit fix.

## Setup

- Source `4e3c96054a034f81ac8556c55cde6572d506e0e7`, with the existing `MarkingFlip_FullMatchMeasurement` fixture and snapshot schema v31. [Build identity](source-identity.txt)
- Six historical seeds × #434 cases C0–C9 × both explicit marking arms. Each of the 120 rows covers 324,000 physics ticks. GK heading is enabled. Both sides use the Mixed marking scheme and ordinary instructions for marking. Each off/on pair uses the same seed and case.
- Linux x64 .NET shim, SDK 8.0.426 / CLR 8.0.32, Release. The November 21 matrix used SDK 8.0.134 / CLR 8.0.34 and schema v30. Snapshot digests are retained for this run and the same-runtime control. The historical comparison uses gameplay counters because schema v30 digests encode a different snapshot layout.
- One additional C0 off/on pair used the first parent of the PR #563 merge (`2d89e45a`), with the same runtime, build configuration, fixture and seed `0123456789ABCDEF`.

## Reproduce

From a checkout at `4e3c9605`:

```bash
python3 tools/dotnet-ci/generate_projects.py
dotnet build src/match-engine/tests/match-engine-tests.gen.csproj -c Release

TD_MARKING_MEASURE_OUT="$PWD/w13-rerun.csv" \
TD_MARKING_MEASURE_SEEDS=0123456789ABCDEF,1111111111111111,2222222222222222,3333333333333333,DEADBEEFCAFEBABE,0F0F0F0F0F0F0F0F \
TD_MARKING_MEASURE_CASES=0,1,2,3,4,5,6,7,8,9 \
TD_MARKING_MEASURE_ARMS=off,on TD_MARKING_MEASURE_TICKS=324000 \
dotnet test src/match-engine/tests/match-engine-tests.gen.csproj -c Release --no-build --no-restore \
  --filter 'FullyQualifiedName~MarkingFlip_FullMatchMeasurement'
```

Use a fresh CSV path because the fixture appends to an existing file. The retained run split the cases across ten fixture invocations with separate output files, two workers, and merged the rows by seed, case and arm. The [original analyzer](../w13-flip-measurement-20261121/analyze.py) reproduces the match aggregates and formation/Duty ratios. Compute C0 anatomy by averaging the corresponding counts over its six matches per arm. The CSV's `wallSeconds` is elapsed time under concurrent execution.

## Coverage

- 120 unique seed/case/arm rows, 60 matched pairs, each with 324,000 ticks and zero observations of non-finite agent position or velocity.
- All ten case runs passed. Both parameterizations of `DeceleratingOntoASteadyPoint_AtItsSpeed_ReentersTheRunningBand` passed on the same fixed commit. [Test receipts](test-receipts.txt)
- [Raw matrix](matrix.csv), [original analyzer output](analysis.txt) and [pre-fix control rows](pre-fix-c0-control.csv).

## Match outcomes

Per-match means over all cases (60 matches per arm). Change is relative to the marking-off arm. Tackle attempts are the sum of won, loose, foul and missed outcomes.

| Metric | Nov 21 off → on | Change | With #563 off → on | Change |
|---|---:|---:|---:|---:|
| Home goals | 6.37 → 5.27 | −17.3% | 6.37 → 5.07 | −20.4% |
| Away goals | 4.90 → 4.13 | −15.6% | 4.90 → 4.57 | −6.8% |
| Total goals | 11.27 → 9.40 | −16.6% | 11.27 → 9.63 | −14.5% |
| Home shots | 15.12 → 18.72 | +23.8% | 15.12 → 16.65 | +10.1% |
| Away shots | 13.02 → 15.08 | +15.9% | 13.02 → 16.72 | +28.4% |
| Total shots | 28.13 → 33.80 | +20.1% | 28.13 → 33.37 | +18.6% |
| Tackle attempts | 14.35 → 32.38 | +125.7% | 14.35 → 30.02 | +109.2% |
| Tackle fouls | 0.53 → 1.12 | +109.4% | 0.53 → 1.12 | +109.4% |
| Tackles missed | 10.68 → 24.80 | +132.1% | 10.68 → 23.12 | +116.4% |
| Yellow cards | 1.05 → 1.10 | +4.8% | 1.05 → 1.10 | +4.8% |
| Grounded agent-ticks | 193,144.40 → 299,035.62 | +54.8% | 193,144.40 → 297,399.38 | +54.0% |
| OscillationGuard lock entries | 61,749.98 → 48,954.40 | −20.7% | 61,749.98 → 48,862.12 | −20.9% |

All 60 marking-off rows reproduced every shared gameplay counter from November 21. The six C0 marking-off rows also reproduced every knockdown anatomy counter. All 60 marking-on rows differed in at least one shared counter. The current marking-on arm still produces fewer goals, more shots, and about twice as many tackle attempts.

## C0 knockdowns

Per-match means over six both-F442 matches per arm. Every GROUNDED entry was tagged `COLLISION`.

| Metric | Nov 21 off → on | Change | With #563 off → on | Change |
|---|---:|---:|---:|---:|
| grounded agent-ticks | 214,830.33 → 313,924.83 | +46.1% | 214,830.33 → 325,622.83 | +51.6% |
| collision-tagged entries | 1,884.17 → 2,798.83 | +48.5% | 1,884.17 → 2,904.50 | +54.2% |
| sliding-tackle entries | 0.00 → 0.00 | — | 0.00 → 0.00 | — |
| entries without a ball holder | 1,708.33 → 2,485.00 | +45.5% | 1,708.33 → 2,597.67 | +52.1% |
| entries of agents with mark targets | 0.00 → 470.33 | — | 0.00 → 469.17 | — |

The collision increase is 54.2%, compared with 48.5% in the November 21 run. About 87% of the current off-to-on increase occurred with no individual ball holder. [Open knockdown issue](../../open-issues.md#w13-loose-ball-knockdowns)

## Formation and Duty sensitivity

Aggregate goals for the changed team over six seeds divided by that team's C0 total in the same arm. C9 uses both teams' goals.

| Case | Nov 21 off | Nov 21 on | With #563 off | With #563 on |
|---|---:|---:|---:|---:|
| C1 Home F433 | 73/27 (×2.70) | 28/21 (×1.33) | 73/27 (×2.70) | 17/23 (×0.74) |
| C2 Away F433 | 43/21 (×2.05) | 20/23 (×0.87) | 43/21 (×2.05) | 19/24 (×0.79) |
| C3 Home F4231 | 42/27 (×1.56) | 44/21 (×2.10) | 42/27 (×1.56) | 43/23 (×1.87) |
| C4 Away F4231 | 45/21 (×2.14) | 40/23 (×1.74) | 45/21 (×2.14) | 46/24 (×1.92) |
| C5 Home all-Defend | 13/27 (×0.48) | 8/21 (×0.38) | 13/27 (×0.48) | 13/23 (×0.57) |
| C6 Away all-Defend | 12/21 (×0.57) | 8/23 (×0.35) | 12/21 (×0.57) | 15/24 (×0.62) |
| C7 Home all-Attack | 78/27 (×2.89) | 70/21 (×3.33) | 78/27 (×2.89) | 60/23 (×2.61) |
| C8 Away all-Attack | 52/21 (×2.48) | 41/23 (×1.78) | 52/21 (×2.48) | 54/24 (×2.25) |
| C9 Both F4231 (total goals) | 80/48 (×1.67) | 67/44 (×1.52) | 80/48 (×1.67) | 63/47 (×1.34) |

With marking on, F433 scoring is below C0 for both sides. F4231 still produces about 1.9× C0 per changed side. All-Attack scores 2.61× C0 at home and 2.25× away, while all-Defend scores 0.57× and 0.62×. The formation/Duty player-control blocker remains open. [Open formation/Duty issue](../../open-issues.md#formation-duty-player-control-blocker)

## Same-runtime pre-fix control

For C0 seed `0123456789ABCDEF`, the marking-off score was 7–2 before and after #563. All recorded counters and the schema-v31 final digest matched. With marking on, the score moved from 3–4 to 6–2 and the collision count from 2,941 to 2,968. The pre-fix pair also matched the November 21 gameplay and knockdown counters for this seed. This control isolates #563 for one seed and case. The 120-match historical comparison spans the intervening changes and toolchains.

## Scope

These figures are descriptive on the Linux shim. Six seeds per case limit formation and Duty conclusions. `groundedLoose` counts entries with no individual ball holder and includes passes in flight. This capture used the reduced off/on fixture. The full W13 §10 trace protocol, Unity Editor validation, platform certification, and KD-W1 calibration are outside its result.
