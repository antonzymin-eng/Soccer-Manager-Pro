# Frozen #435 foul/card six-seed characterization evidence

**Captured:** September 22, 2026  
**Preregistration:** PR #435 merge `e92b13ed826927dd5d3a5bcc57b24afb81f10b1e`  
**Instrument landing:** PR #436 merge `c56e5e1adff4e240b54c9abbb5ebea9c0290f65e`  
**Measured production SHA:** `c56e5e1adff4e240b54c9abbb5ebea9c0290f65e`  
**Successful run/job:** `35765635130` / `106874328764`  
**Status:** result-bearing post-W2 **characterization**, not the final KD-W1 calibration fit.

**Branch disposition (2026-09-25):** `evidence/foul-card-six-seed-20260922` may be deleted only if it still points at recorded head `7db673cacf7fb24d1db0b8340cbc3ef5e3821daf`. Its branch-only workflow history is already preserved in this evidence set/archive; deleting the ref does not alter the retained Actions runs. Do not rewrite the hash-covered evidence payloads to reflect ref deletion.

## Execution boundary

The one-shot workflow was triggered from `evidence/foul-card-six-seed-20260922` at harness commit
`50d6229ccd5cf3e2118aae1497d27b40f6986b29`, but the workflow explicitly checked out the landed
`main` SHA `c56e5e1a...` and failed if `git rev-parse HEAD` differed. `harness-50d6229.yml` is the
exact Git blob from that result-producing commit.

The run used the frozen six #435 seeds, 324,000 physics ticks per seed, and the shipped default
settings on the measured SHA: `FoulCallProbability = 0.030`, `FoulImpactForceThresholdN = 1200`,
and `FoulCooldownTicks = 180`. No gameplay `[GT]` was changed for this run.

The harness mirrors the canonical measurement lane's build/resolve/proved-run/verify pattern. Its
pre-measurement gate built the full generated tree but applied
`FullyQualifiedName~FoulRateDiagnostic` to the test phase; it therefore **does not prove the full
suite passed**. The result-bearing invocation ran the owning MatchEngine test project with
`TD_FOUL_DIAGNOSTIC=1`, detailed console + TRX loggers, and the catalog's sentinel/pass verifier.

## Governing observed output

| Seed | Collision called | Tackle called | Fouls | Yellows | Straight red | 2Y dismissal | ID checks / mismatches | Tackle raised at tick-start cooldown > 0 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| `0x0F1E2D3C4B5A6978` | 4 | 1 | 5 | 1 | 0 | 0 | 4 / 0 | 0 |
| `0x00000000D1A6D05E` | 6 | 1 | 7 | 1 | 0 | 0 | 6 / 0 | 0 |
| `0x0000000000000001` | 9 | 0 | 9 | 0 | 0 | 0 | 9 / 0 | 0 |
| `0x00000000ABCDEF12` | 6 | 3 | 9 | 2 | 0 | 0 | 6 / 0 | 0 |
| `0x0000000099887766` | 7 | 2 | 9 | 1 | 0 | 0 | 7 / 0 | 0 |
| `0x000000005A5A5A5A` | 10 | 1 | 11 | 0 | 1 | 0 | 10 / 0 | 0 |
| **Aggregate** | **42** | **8** | **50** | **5** | **1** | **0** | **42 / 0** | **0** |

Aggregate shipped rates reported by the instrument are **8.33 fouls / 0.83 yellows / 0.167
straight reds / 0 second-yellow dismissals / 0.167 total dismissals per 90**. The collision funnel
also closes exactly:

`773 candidates = 25 cooldown-suppressed + 0 decided-tackle displaced + 3 stronger-same-tick lost + 0 sent-off-shadowed + 745 priced`.

The applied-foul identity closes as `42 FROM_BEHIND + 8 SLIDE_TACKLE = 50 total fouls`, the dismissal
identity closes as `1 straight red + 0 second-yellow = 1 total dismissal`, and all **42** called
priced-candidate identity checks match production offender/victim identity.

## Cooldown-bypass observation is not a disposition

Both preregistered tackle/cooldown populations are zero:

- `slideTackleCallsDuringFoulCooldown = 0` on the literal tick-start `> 0` basis;
- `slideTackleCallsDuringCollisionSuppressionWindow = 0` on the post-Resolve-decrement `> 0`
  comparison basis (tick-start `> 1`).

Only eight tackle fouls occurred in six matches. The observed zero therefore has insufficient
exposure to decide whether the production bypass is intended or defective. The owner decision
required by #435 §2.1 remains **OPEN**. This evidence must not be cited as proving symmetric cooldown
semantics, proving the bypass harmless, or closing the invalidation trigger if that runtime behavior
is later changed.

## Relation to the frozen §5 calibration envelope

This characterization is **not** the final calibration regression governed by #435 §5. The §5
numbers can be used only as descriptive orientation here: 8.33 fouls/90 is below its eventual
15–30 envelope, 0.83 cautions/90 is below its eventual 1.5–6.0 envelope, `5 / 50 = 0.10` lies within
the eventual 0.08–0.30 caution/foul ratio, and one total dismissal happens to fall in the future
six-match 0–2 bucket. None of those observations authorizes a `[GT]` fit now, and the single
dismissal says nothing sufficient about straight-red/card severity.

KD-W1 remains controlling because W3, W8, W9 and W10 are still Class-A wiring work. Any #435 §8
trajectory/contact-stream invalidator requires this same frozen source-complete corpus to be rerun.

## Failed first attempt

Run `35765454797`, job `106873731837`, is explicitly **non-result-bearing**. Its harness commit
`7a45f57f...` leaked `INSTRUMENT=foul-rate` into job-level environment, which C# interpreted as a
compiler instrumentation setting. The full-tree build stopped with `CS8111: Invalid instrumentation
kind: foul-rate` before the proved measurement invocation. See `failed-attempt.txt`. The corrective
run changed only harness scoping; the measured production SHA remained `c56e5e1a...`.

## Durable files

- `harness-50d6229.yml` — exact result-producing one-shot harness.
- `measurement.runsettings` — exact VSTest runsettings retained from the successful artifact.
- `measurement.trx.xz` — lossless xz-compressed exact TRX; original TRX SHA-256 is recorded in
  `provenance.txt`.
- `report.txt` — verbatim first complete #435 report block extracted from the instrument-only stream.
- `results.tsv` / `results.json` — machine-derived six-seed + aggregate counters.
- `provenance.txt` — SHA/run/job/artifact/command/config metadata.
- `failed-attempt.txt` — first-attempt non-result-bearing record.
- `SHA256SUMS` — full integrity manifest for every tracked file in this directory except itself.

`results.tsv`, `results.json`, and `report.txt` are produced by
`tools/dotnet-ci/parse_foul_card_characterization.py`. The parser fails closed unless it finds all six
seed rows and all frozen counter fields, and it rechecks the aggregate collision funnel, foul-source
identity, dismissal identity, and zero priced-candidate identity mismatches.

To restore the retained binary/text streams:

```bash
xz -dc measurement.trx.xz > measurement.trx
```

The original successful Actions artifact was configured for 90-day retention. This committed
package is the durable record and does not depend on that artifact remaining available.
