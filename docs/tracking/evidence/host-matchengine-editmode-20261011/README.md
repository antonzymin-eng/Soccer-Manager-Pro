# Pinned-host MatchEngine EditMode run — PR #497 head `fce91e53`

**Captured:** October 11, 2026, 04:38–15:10 UTC (evening of October 10 into October 11 on the host's
Pacific clock)\
**Purpose:** First run of `TacticalDirector.MatchEngine.Tests` on the governing build, taken to check
PR #497's always-on observer-neutrality gate under Unity before merge. #497 merged at 04:36 UTC, before
the run started; its head and the merge commit have the same `src` tree, so this is also a run of
`main`'s match-engine tests.\
**Host:** the owner's Windows 11 machine, with the Unity 6000.4.9f1 (f7258d6eebbe) editor (Mono). This
is the certified determinism host.\
**Source:** commit `fce91e5365530cbe22f2599a023ac721961b6ee9` (PR #497 head), `src` tree
`a59653e020640c237cf71c0284e2112a7fb54824`, equal to `git rev-parse 82505636:src` (the #497 merge).
`Assets/Scripts` is a junction to `src`. `source-identity.txt` records the hashes during and after the
run, with `git status --porcelain=v1 --untracked-files=all --ignored -- src Packages ProjectSettings`
empty each time. The baseline in §3 ran at pre-PR `main` `2a396d56` (`src` tree `5253abe2…`).\
**Integrity:** `SHA256SUMS` covers every other file in this directory and is verified by
`tools/dotnet-ci/check_evidence_manifests.py`.

## 1. Result

| Check | Result |
| --- | --- |
| Compile at `fce91e53` | `Tundra build success` (50.03 s), 0 `error CS`, 0 shim build-output references, the three known warnings |
| PR #497 gate `HeaderReachabilityObserver_DoesNotChangeSnapshotDigests` | **Passed twice**: alone (62.8 s) and in the full run (60.9 s). 18,000 frames, every per-frame snapshot digest identical with the observer attached |
| Full `TacticalDirector.MatchEngine.Tests` | 574 results: **547 passed, 12 failed, 14 skipped, 1 inconclusive** |

The 12 failures are of two kinds. **This run does not show that they are all unrelated to PR #497.**
Eleven never reached their assertions, so their behavior under Unity is unverified.

1. **11 Unity default timeouts (180 s).** Nine full-match `sim_match_engine_*` scenarios and both
   `SaveAndRestoreCarryTheTackleLatches` cases carry no `[Timeout]`. The Linux gate has no per-test
   timeout, and the Unity editor run is about three times slower (for example `shot_speed` takes
   12 m 14 s on Linux and 39 m 29 s here). A timeout replaces each test's own result, so these runs
   do not show whether the assertions hold under Unity, with or without #497. SeasonSave hit the same
   pattern and took `[Timeout(7200000)]` on October 9 (`../host-clean-editmode-20261009/`).
2. **`sim_match_engine_inposs_gate` fails 2 of 6 predicates under Unity, and the failure predates
   #497.** The predicate counts final-third samples in which a team's committed positioning phase is a
   possession phase (`InPoss` or `OutOfPoss`) rather than a transition, from each team's view. It is
   not that team's share of possession. On seed `0x0F1E2D3C4B5A6978` the home-view and away-view
   shares are both **0.361**, against a bound of > 0.70. The same test at pre-PR `main` `2a396d56`
   gives the identical 0.361 / 0.361, so #497 did not cause it. At `fce91e53` the Linux gate passes it
   with **0.928** on the same seed (CI run `38108462714`), and its quarantine and owner-held-red lists
   are empty. **The cause is unknown.** The two runs differ in more than the runtime: Linux uses the
   `tools/dotnet-ci/UnityShim` replacements for `Mathf`, `Vector2` and `Vector3`, while Unity uses
   the engine's own. Small numerical differences can accumulate over a full match into a large
   outcome difference, so this result does not isolate Mono, the math implementations or a
   behavior-level path.

The inconclusive test, `ATackleFoulIsGivenAsASlideTackleAndNotJudgedTwice` ("too few connecting
challenges to judge the share"), is also inconclusive on Linux. The 14 skips are env-gated diagnostic
instruments. `editmode-summary.txt` lists every failure with its duration.

## 2. Files

| File | What it is |
| --- | --- |
| `source-identity.txt` | Commit and tree hashes for `src`, `Packages`, `ProjectSettings` and `Assets`; empty porcelain listings; Unity version; junction target. Captured during the run, after it, and for the baseline checkout. |
| `compile.txt` | Editor.log excerpts for the forced recursive reimports at `fce91e53` and at the baseline `2a396d56`. |
| `editmode-log.txt` | The full run's per-test log: status, duration and full name for all 574 results, failure messages truncated at 300 characters, framed by its own `STARTED` and `FINISHED` lines. |
| `editmode-summary.txt` | Times, counts, every failure, the baseline and Linux comparison, the sleep interruption, and the lost XML. |
| `targeted-runs-log.txt` | The three single-test runs: the observer gate alone, the sleep-affected case rerun, and the baseline inposs gate. |
| `rerun-results.xml.xz` | NUnit XML for the sleep-affected case rerun at `fce91e53`. |
| `baseline-inposs-results.xml.xz` | NUnit XML for `sim_match_engine_inposs_gate` at `2a396d56`, with the full predicate message. |

## 3. Caveats

- **The host slept from 07:36:00 to 14:01:26 UTC** (lid or power button). Only
  `SaveAndRestoreCarryTheTackleLatches(3517370462UL)` was running across the sleep, so its 23,384.0 s
  is wall-clock time. Rerun alone, it failed the same way in 216.9 s. No other duration is affected.
- **The full run's NUnit XML is lost.** `TestRunnerApi.RegisterCallbacks` registers callbacks for the
  whole editor process, so the later rerun also fired the full run's `RunFinished` callback and
  overwrote its XML. `editmode-log.txt` was written test by test by the full run's own callback and
  holds exactly its 574 results, so it is the record. The two targeted runs' XML files are intact. A
  later run should unregister its callbacks or write to a per-run path.
- Runs were driven through the editor's `unity-mcp` `RunCommand` tool with `TestRunnerApi`. No project
  asset, scene or setting was modified.

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | October 11, 2026 | Initial evidence set: first pinned-host run of `TacticalDirector.MatchEngine.Tests`, at PR #497 head `fce91e53`, with the pre-PR inposs-gate baseline. |
