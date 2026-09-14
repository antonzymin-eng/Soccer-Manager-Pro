# W12 gate-firing post-#398 comparison — corrected record

> **Correction, September 14, 2026:** the prior version of this document was not a valid transcription of run `34847990460`. This file is generated from the mechanically parsed census and checked against the committed raw source bytes. Do not hand-edit measurement values; run `python tools/check-w12-evidence.py --write` after an intentional census update.

## Provenance

| Lane | Run | Measured commit / ref | Durable source | Artifact corroboration |
|---|---:|---|---|---|
| Corrected pre-#398 | `34844425733` | `c379788c2c96f1f49034c8fdb7dc54f487f9cf5f` / `refs/heads/wiring/w12-gate-firing-diagnostic` | committed `pre/instrument-output.txt` bytes, SHA-256 `66dfe2604e963b3d5569eed5ff10a14d3216cf9042fe182ca86e2e7c8a8adebc` | ZIP SHA-256 `0d65be3a1b808933a58f785d7e65c321ae5974626089e2c0a49cfe3c00b5231c` |
| Post-#398 | `34847990460` (job `103988507476`) | `0821bfa9ff66b24393014eacf0401904fdc0fb37` / `refs/heads/tmp/pr398-post-w12` | committed `post/instrument-output.txt` bytes, SHA-256 `8c3764765968e74d63cfb232815ed6a72b4b3789bed07de9757327adf1cf8655` | ZIP SHA-256 `691efe7a3c77ac1017ed86a78dcfea384a12fcd25248ade4090cbb59a48fc73d` |

The raw files are losslessly preserved under `docs/tracking/evidence/w12/`; workflow metadata and the GitHub ZIP digests corroborate their origin. `w12-gate-firing-census.json` is a mechanical parse of the first complete census block in each committed raw instrument output.

## Measurement semantics

- `latestPass` counts eligible pressing heartbeats on which `PassEventRing.TryGetLatest` succeeded. It is a retained-ring-availability observation, **not pass throughput or a pass-event count**.
- `raw BackwardPass` is not a pure event count. The evaluator defines it as `freshBackwardPass || pendingBackwardPassDwell`, so it can remain true on dwell-continuation heartbeats.
- `committed` means the trigger debounce/dwell state is live; `Active` means the resulting press directive survived downstream gates.
- Phase/cooldown exits occur before pass lookup and trigger evaluation. The checker enforces `latestPass <= samples - InPossession - Cooldown - StaleTick` for every team row, along with complete exit accounting and aggregate reconstruction.

## Matched three-seed result

Scorelines are identical: **0-3, 2-5, 2-1** pre and post.

| Metric | Pre #398 | Post #398 | Delta |
|---|---:|---:|---:|
| latestPass | 0 | 141,491 | +141,491 |
| primaryAssigned | 21,800 | 21,803 | +3 |
| coverShadows | 10,664 | 10,664 | +0 |
| raw BackwardPass | 0 | 1,827 | +1,827 |
| committed BackwardPass | 0 | 2,770 | +2,770 |
| raw SidelineTrap | 849 | 849 | +0 |
| committed SidelineTrap | 979 | 979 | +0 |
| raw WeakReceiver | 141,379 | 141,376 | -3 |
| committed WeakReceiver | 141,414 | 141,411 | -3 |

### Gate outcomes — exact equality

| Exit/outcome | Pre #398 | Post #398 | Delta |
|---|---:|---:|---:|
| Active | 140 | 140 | +0 |
| InvariantRejected | 139,309 | 139,309 | +0 |
| NoPrimaryPresser | 84 | 84 | +0 |
| Disengaged | 1,890 | 1,890 | +0 |
| Cooldown | 22,671 | 22,671 | +0 |
| NoCommittedTrigger | 99 | 99 | +0 |
| InPossession | 159,801 | 159,801 | +0 |

Every gate-outcome counter is unchanged by exact equality. `primaryAssigned` rises by 3 while `WeakReceiver` falls by 3 on the same team/seed; this is positive wiring evidence that committed `BACKWARD_PASS` reached primary-press selection and perturbed internal selection bookkeeping, without an observed change in gate outcomes in this corpus.

## Per-seed / per-team census

| Lane | Seed | Final | Team | samples | latestPass | active | primaryAssigned | InPoss | NoCommitted | Active exit | NoPrimary | InvariantRejected | Disengaged | Cooldown | raw Bwd | committed Bwd | raw Side | committed Side | raw Weak | committed Weak |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| pre | 0x0F1E2D3C4B5A6978 | 0-3 | 0 | 53999 | 0 | 6 | 4916 | 26504 | 24 | 6 | 3 | 23822 | 280 | 3360 | 0 | 0 | 239 | 264 | 24104 | 24109 |
| pre | 0x0F1E2D3C4B5A6978 | 0-3 | 1 | 53999 | 0 | 0 | 2740 | 26880 | 40 | 0 | 0 | 22779 | 331 | 3969 | 0 | 0 | 149 | 177 | 23075 | 23107 |
| pre | 0x00000000D1A6D05E | 2-5 | 0 | 53999 | 0 | 104 | 3760 | 25055 | 1 | 104 | 70 | 23842 | 379 | 4548 | 0 | 0 | 159 | 184 | 24396 | 24395 |
| pre | 0x00000000D1A6D05E | 2-5 | 1 | 53999 | 0 | 0 | 3191 | 28286 | 18 | 0 | 0 | 22094 | 277 | 3324 | 0 | 0 | 95 | 118 | 22368 | 22369 |
| pre | 0x5EED000000000003 | 2-1 | 0 | 53999 | 0 | 30 | 4058 | 24464 | 1 | 30 | 11 | 25261 | 326 | 3906 | 0 | 0 | 46 | 58 | 25629 | 25628 |
| pre | 0x5EED000000000003 | 2-1 | 1 | 53999 | 0 | 0 | 3135 | 28612 | 15 | 0 | 0 | 21511 | 297 | 3564 | 0 | 0 | 161 | 178 | 21807 | 21806 |
| post | 0x0F1E2D3C4B5A6978 | 0-3 | 0 | 53999 | 24135 | 6 | 4916 | 26504 | 24 | 6 | 3 | 23822 | 280 | 3360 | 275 | 414 | 239 | 264 | 24104 | 24109 |
| post | 0x0F1E2D3C4B5A6978 | 0-3 | 1 | 53999 | 23139 | 0 | 2743 | 26880 | 40 | 0 | 0 | 22779 | 331 | 3969 | 315 | 478 | 149 | 177 | 23072 | 23104 |
| post | 0x00000000D1A6D05E | 2-5 | 0 | 53999 | 24395 | 104 | 3760 | 25055 | 1 | 104 | 70 | 23842 | 379 | 4548 | 309 | 473 | 159 | 184 | 24396 | 24395 |
| post | 0x00000000D1A6D05E | 2-5 | 1 | 53999 | 22383 | 0 | 3191 | 28286 | 18 | 0 | 0 | 22094 | 277 | 3324 | 303 | 458 | 95 | 118 | 22368 | 22369 |
| post | 0x5EED000000000003 | 2-1 | 0 | 53999 | 25627 | 30 | 4058 | 24464 | 1 | 30 | 11 | 25261 | 326 | 3906 | 334 | 509 | 46 | 58 | 25629 | 25628 |
| post | 0x5EED000000000003 | 2-1 | 1 | 53999 | 21812 | 0 | 3135 | 28612 | 15 | 0 | 0 | 21511 | 297 | 3564 | 291 | 438 | 161 | 178 | 21807 | 21806 |

## Verdict

**W5 producer→consumer wiring is proven.** In the matched three-seed pre/post corpus, every gate-outcome counter is exactly unchanged: Active **140 → 140**, InvariantRejected **139,309 → 139,309**, NoPrimaryPresser **84 → 84**, Disengaged **1,890 → 1,890**, Cooldown **22,671 → 22,671**, and all three scorelines are identical. Meanwhile `latestPass` becomes available on **141,491** eligible heartbeats and BackwardPass becomes observable (**1,827 raw / 2,770 committed**). The `primaryAssigned +3` / `WeakReceiver −3` delta shows that the new trigger reached selection and perturbed internal processing, but produced **no observed change in gate outcomes in this corpus**.

This satisfies the pre-registered W5 acceptance contract: a real producer feeds the consumer and the BackwardPass trigger is evaluable from real events without gate collapse. It does **not** establish a general claim of behavioural inertness or material match-level effect.
