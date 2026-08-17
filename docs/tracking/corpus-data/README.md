# A4a corpus — raw capture slices

> **Created:** August 12, 2026
> **Purpose:** The engine-simulated match rows behind `docs/tracking/round-resolution-corpus.md`.
> Committed as data, not as a document — the artifact is generated *from* these.

## Why these are in the repo

Each row is one full 90-minute `MatchEngine` match at ~90 s of compute. The full set below is
**~9.5 hours** of engine time. Regenerating it is expensive and, once the engine's scoring moves,
**impossible** — KD-8's re-capture trigger means a later tree produces a different corpus, so these
rows are the only record of what the engine did at commit `95ffc31`.

They were nearly lost. The deepening slices (`z*.csv`) lived only in an ephemeral session scratchpad
until an advisory review pointed out that the artifact had preserved just their three-number W/D/L
summary. Any future analysis that needs the **joint** scoreline distribution — which is exactly what
a distribution-family decision needs (`ERR-030-034`) — needs the rows, not the summary.

## The files

| File | Buckets | Samples | Rows |
|---|---|---|---|
| `s1.csv` | `dSquad` −5…−3 | 0–17 | 54 |
| `s2.csv` | `dSquad` −2…0 | 0–17 | 54 |
| `s3.csv` | `dSquad` +1…+3 | 0–17 | 54 |
| `s4.csv` | `dSquad` +4…+5 | 0–17 | 36 |
| `z1.csv` | `dSquad` 0 | 18–62 | 45 |
| `z2.csv` | `dSquad` 0 | 63–107 | 45 |
| `z3.csv` | `dSquad` 0 | 108–152 | 45 |
| `z4.csv` | `dSquad` 0 | 153–197 | 45 |

`s*.csv` are **the fit corpus** (198 matches, 11 buckets × 18). `z*.csv` **deepen the `dSquad ≈ 0`
acceptance bucket to n = 198** and are fed to `--wdl-csv`, which deliberately keeps them *out* of the
sample-weighted objective — folding them in would re-weight the whole grid toward the one bucket that
happens to have been measured hardest.

Columns: `dSquad,homeGoals,awayGoals,matchSeed,homeDelta,awayDelta,homeBaseClubId,awayBaseClubId`.
Every column is observable at capture time, which is the property KD-8's first draft lacked when it
asked the harness to record `dRating`.

## Reproducing and consuming them

The capture commands are in `round-resolution-corpus.md` §0.a. To re-fit without re-capturing:

```bash
python3 tools/round-resolution-fit.py docs/tracking/corpus-data/s{1,2,3,4}.csv \
    --wdl-csv docs/tracking/corpus-data/z{1,2,3,4}.csv \
    --engine-sha 95ffc31 --schema-version 20 \
    --platform "Linux x64 (Ubuntu 24.04), .NET SDK 8.0.129, Release — non-certifying" \
    --out docs/tracking/round-resolution-corpus.md
```

Capture provenance: engine commit `95ffc31`, `SNAPSHOT_SCHEMA_VERSION` 20, Linux x64 / .NET 8.0.129
Release, non-certifying. All-fit squads, default tactics, no substitutions — see the artifact's §0.b.

## Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 1.0 | 2026-08-12 | — | The A4a capture committed as data: the 198-match fit corpus plus the 180 deepening rows at the acceptance bucket. Filed after an advisory review found the deepening rows existed only as a W/D/L summary in the artifact, which is insufficient for the joint-distribution analysis `ERR-030-034`'s decision needs. |
