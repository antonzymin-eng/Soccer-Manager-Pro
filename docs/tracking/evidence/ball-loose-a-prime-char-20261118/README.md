# #440 A′ — six-seed before/after characterization (owner decision D1)

**Created:** November 18, 2026.\
**Purpose:** The descriptive six-seed before/after characterization that owner decision D1 requires for A′
(`ERR-014-011`, #14 §3.6.5.8), reported with Stage A's frozen time-to-regain bins
([w-440 design](../../w-440-ball-loose-design.md), "Reported time to original-carrier regain";
[tackle duel note](../../tackle-duel-design.md) §4). **Nothing here gates.** D1 accepts A′ on correctness
evidence only; these numbers are reported so that a fence which merely defers the carrier's regain stays
visible.\
**Integrity:** `SHA256SUMS` covers every other file in this directory and is verified by
`tools/dotnet-ci/check_evidence_manifests.py`.

## Arms

| Arm | Commit | What it is |
| --- | --- | --- |
| Before | `7ccc847fa00ece12f63c8cb7c7053c62576c1b90` | PR #544 head (main `4cdb4823` merged, D1 recorded); the pre-A′ release |
| After | `e3029d1a9040972fbc68500271ef6e02813a9abd` | PR #549 head: A′ on top of the before arm, nothing else |

Same six seeds as Stage A (`pre-c-control-20261104/expected.json`), full matches of 324,000 frames, Stage A's
observer (`src/match-engine/tests/BallLooseEpisodeObserver.cs`) and runner (`../ball-loose-stage-a-20261110/Program.cs.txt`)
used unchanged, observed mode only. Every match first passes Stage A's own independent audit
(`analyze.audit`, unchanged): chronology order, frame-end owner reconciliation, episode/census/engine counts and
censor attribution. All 12 runs exited 0.

## Results (pooled, all episodes, no exclusions)

| | Before | After |
| --- | ---: | ---: |
| BallLoose episodes (N) | 24 | 29 |
| Same-frame original-carrier regain | **24** | **0** |
| Carrier holding at entry to f+1 | 24 | 0 |
| Ball planar speed at the end of frame f (m/s): min / median / max | 0 / 0 / 0 | 0.74 / 1.90 / 5.04 |

**Time to the original carrier's first regain** (disjoint bins that exhaust N):

| Bin | Before | After |
| --- | ---: | ---: |
| same frame f | 24 | 0 |
| f+1..f+3 | 0 | 7 |
| f+4..f+12 | 0 | 9 |
| f+13..f+24 | 0 | 2 |
| f+25..f+60 | 0 | 1 |
| complete one-second window without regain | 0 | 10 |
| censored before first regain | 0 | 0 |

**Cumulative first regain** K/N with completion bounds [K/N, (K+U)/N]; no window was censored, so U = 0 and
the bounds are points:

| Horizon | Before | After |
| --- | --- | --- |
| f+3 | 24/24 (1.00) | 7/29 (0.24) |
| f+24 | 24/24 (1.00) | 18/29 (0.62) |
| f+60 | 24/24 (1.00) | 19/29 (0.66) |

| Other episode-prefix counts | Before | After |
| --- | ---: | ---: |
| Holder at f+60: still loose / original carrier / teammate / opponent / censored | 23 / 0 / 0 / 1 / 0 | 28 / 0 / 0 / 1 / 0 |
| Initiating tackler acquires within the window | 9 | 3 |
| Any opponent acquisition within the window | 9 | 3 |
| Any carrier-teammate acquisition within the window | 0 | 0 |

**By the carrier's team** (bins in the order of the table above):

| Arm | Team | N | Bins | Cumulative f+3 / f+24 / f+60 |
| --- | --- | ---: | --- | --- |
| Before | home (0) | 7 | 7, 0, 0, 0, 0, 0, 0 | 7 / 7 / 7 |
| Before | away (1) | 17 | 17, 0, 0, 0, 0, 0, 0 | 17 / 17 / 17 |
| After | home (0) | 9 | 0, 2, 4, 1, 0, 2, 0 | 2 / 7 / 7 |
| After | away (1) | 20 | 0, 5, 5, 1, 1, 8, 0 | 5 / 11 / 12 |

Per seed (BallLoose, same-frame regains): before 1/1, 5/5, 5/5, 3/3, 6/6, 4/4; after 4/0, 10/0, 3/0, 4/0, 6/0,
2/0 (seeds `…01`, `…5A5A5A5A`, `…99887766`, `…ABCDEF12`, `…D1A6D05E`, `0F1E…6978`). Whole-match goals 49 → 48 and
fouls 47 → 54 across the six matches; once the first knock changes play, the matches diverge, so these are not
paired effects.

## Reading

- **The defect is gone as specified.** No episode ends the tackle frame with the carrier, and every knocked ball
  is moving at frame end.
- **The fence mostly defers the regain; it does not usually turn the ball over.** The original carrier
  re-collects in 19 of 29 episodes within one second, 7 of them within three frames and 18 within 24 frames. Only
  one episode ends with the opponent holding at f+60, and acquisitions by the tackler fell from 9 to 3. This is
  the outcome the design kept delayed-regain reporting for. It is reported, not judged: whether a 0.6·max(c, 2 m/s)
  poke is enough of a dispossession is a calibration and B′ question (`tackle-duel-design.md` §5, D2/D4), not part
  of A′'s correctness acceptance.
- Before-arm counts differ from Stage A's 37 because the base moved (Stage A measured `d4b026aa`; this before arm
  includes #543, #548 and the rest of main up to `4cdb4823`). The mechanism is unchanged: 24/24 same-frame
  passive regains at rest.
- The observer's `initialVelocity` is captured at the tackle event, before the composition root applies the knock,
  so it reads zero in both arms; the frame-end velocity is the knock as it leaves frame f.

## Lane and limits

- Local Windows lane: .NET SDK 8.0.425, runtime 8.0.31, Debug, `DEVELOPMENT_BUILD`, not Stage A's frozen Linux
  SDK 8.0.426 / CLR 8.0.32 lane. Both arms ran on the same lane, so the comparison is like for like. The Windows
  and Linux .NET 8 shims have matched bit for bit on earlier gate runs; this run does not re-establish it.
- Observer neutrality (plain vs observed streams) was established by Stage A and is not re-run here. The observer
  source is unchanged between Stage A and both arms.
- Six seeds, small clustered samples, descriptive only. No definition, seed, horizon or episode was chosen after
  measurement.
- The raw traces (about 488 MB per arm) are not committed. `runs/<arm>/<seed>/raw-trace-hashes.txt` records the hash of
  every raw stream; re-running `char_run.py` on the two commits reproduces them. Re-auditing therefore needs a
  re-capture, which takes about 8 minutes per arm with three workers.

## Files

| File | What it is |
| --- | --- |
| `char_run.py` | Builds Stage A's observer runner for a commit (`git archive` of the same source paths, the same capture project) and runs the six seeds in observed mode |
| `char_analyze.py` | Runs Stage A's `analyze.audit` on every match, then computes the bins, cumulative bounds, endpoints and per-team splits |
| `characterization.json` | Every number above, per arm: pooled, by carrier team and per seed |
| `episodes-before.csv`, `episodes-after.csv` | One row per BallLoose episode |
| `runs/<arm>/head.txt`, `runs/<arm>/runs.json`, `runs/<arm>/build-tail.txt` | Commit, per-run commands (local paths redacted to `<work>`), exit codes and times, and the build result |
| `runs/<arm>/<seed>/` | `summary.json`, `census.json`, `episodes.jsonl`, `chronology.jsonl.xz`, `runner.log` and `raw-trace-hashes.txt` |

## Reproduce

```bash
python char_run.py --head 7ccc847fa00ece12f63c8cb7c7053c62576c1b90 --work-dir <work>/before
python char_run.py --head e3029d1a9040972fbc68500271ef6e02813a9abd --work-dir <work>/after
python char_analyze.py <work>
```

`DOTNET` selects the host executable; the scripts need `python3` on `PATH` for `generate_projects.py`.
