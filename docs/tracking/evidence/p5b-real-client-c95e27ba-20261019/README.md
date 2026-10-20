# P5b real-client acceptance and B8/B10 capture — pinned-host evidence

**Captured:** October 19–20, 2026 UTC (October 18–19 on the host's Pacific clock). Run-log timestamps are UTC time of day.\
**Purpose:** Raw evidence for [`p5b-real-client-acceptance.md`](../../p5b-real-client-acceptance.md) v0.1.\
**Host:** the owner's Windows 11 machine, Unity 6000.4.9f1 (DX11, Mono, Release code optimization). Certified determinism host.\
**Source:** main `c95e27ba7fd398e37d297647ba060c2ab1f4baa7`. Run 1 compiled at Editor launch from the Desktop checkout
before a parallel session modified files there (its first `src/` edit is timestamped after that compile, and no domain
reload occurred during run 1). Runs 2–4 used a dedicated worktree at the same commit. After the I-Q15 crash, runs used
that commit plus `fix-S0ScreensBehaviour-v1.7.diff` (SHA-256 of the diff `a1e2e8de…a59920`).\
**Integrity:** `SHA256SUMS` covers every other file in this directory.

| File | What it is |
| --- | --- |
| `probe-log-run1.txt.xz` | Run 1 (Desktop checkout, unmodified commit): boot, I-Q01–14, two full matches. Contains the documented harness-oracle failures (I-Q03 withheld-text oracle, I-Q10 re-pause, goalkeeper-pair refusals before the role-matched selector). |
| `probe-log-run2.txt.xz` | Runs 2–4 (worktree): I-Q07 rerun, I-Q09/18 cap attempt (J-02), I-Q10 deferred-at-whistle variants, I-Q15 (crash run, then 38/38 with the fix), I-Q16 matrix. Earlier lines of the same file hold one void start (scene not open) and the pre-capture-fix layout run. |
| `editor-log-iq15-stackoverflow.txt` | Editor.log excerpts of J-03: the recursive `ChoiceArrow → Control → Refresh → Keyboard` stack, the renderer rejection and the failed Start. The full 400 MB log is kept compressed outside the repository. |
| `fix-S0ScreensBehaviour-v1.7.diff` | The source change verified by the I-Q15 rerun and used for the player builds. |
| `shots-1-behaviour.tar`, `shots-2-keyboard.tar`, `shots-3-layout.tar` | 16 curated PNG screenshots in three plain tars, each under the 1 MiB binary limit (`*.png` is Git LFS-tracked and the manifest job checks out without LFS). `run1-*` were taken with the asynchronous Editor capture and may show the state one step later than named; `run2-iq16-*` are synchronous end-of-frame captures at the named size. |
| `perf/capture-final.log` | One summary line per B8/B10 condition: frames, FPS, p50/p95/p99/max, frames over 16.7 ms, sim ticks/s, per-marker cadence. |
| `perf/frame-csv-final.tar.xz` | Per-frame CSVs behind those summaries. |
| `perf/end-states.tar` | Player screenshots (`s100-end-states.png`, `s200-end-states.png`, `s100-10x-open-end.png`) proving each condition's speed and statistics state at the end of its hold. |
| `probe-source/` | The untracked harness and driver scripts used, verbatim, as `.txt`. |

The player `.raw` profiles (5.3 GB), the development player builds and the void-attempt material stay outside the repository.

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | October 19, 2026 | Initial evidence set for the P5b real-client acceptance, B8/B10 capture and Gate-J technical verdict. |
