# Pinned-host Unity run — merged main 5a3c310e and the remaining career shutdown checks

**Captured:** November 19, 2026, 18:49–19:38 UTC (10:49–11:38 on the host's Pacific clock)\
**Purpose:** Compile and EditMode receipt for main after [PR #550](https://github.com/antonzymin-eng/Soccer-Manager-Pro/pull/550)
merged the career shutdown branch with main's #440 A′ engine change. Also the four shutdown checks the
[`cd3c7b99` run](../host-cd3c7b99-20261119/README.md) left open under
[design acceptance item 4](../../career-shutdown-cancellation-design.md#acceptance): player window close during a save,
steady-state allocations, and match throughput and no-cancel result identity against the pre-cancellation path.\
**Host:** the owner's Windows 11 machine, Unity 6000.4.9f1 (f7258d6eebbe), Mono. This is the certified host.\
**Integrity:** `SHA256SUMS` covers every other file in this directory and is verified by
`tools/dotnet-ci/check_evidence_manifests.py`.

| File | What it is |
| --- | --- |
| `source-identity.txt` | Commit and tree hashes for main and both bench arms, and the uncommitted player-build changes. |
| `compile.txt` | Clean batch-mode import of merged main: 0 `error CS`, 75 assemblies for 75 asmdefs. |
| `editmode-results.xml.xz` | NUnit 3.5 XML for ClientApp.Tests and SeasonSave.Tests. Host paths are replaced with `<scratch>` and `%USERPROFILE%`. |
| `editmode-summary.txt` | Command, times, per-assembly counts, the shutdown and replay cases, and the seven skips. |
| `bench-results.txt` | Raw output of the round-throughput bench, both arms. |
| `career-round-bench.cs.txt`, `bench-run-arms.sh.txt` | The bench Editor script and the arm runner. Neither was committed. |
| `player-transcript.txt` | Development-player harness log, in-player probe log, write-watcher logs and per-log exception counts. |
| `alloc-samples.txt` | Raw per-frame allocation samples, 600 frames per state. |
| `save-hashes.txt` | SHA-256 of every save-directory snapshot, with each sidecar's sequence and digest check. |
| `career-probe-driver.cs.txt`, `build-probe-player.cs.txt`, `run-player.ps1.txt` | The scratch player driver, development-build script and launch harness. None were committed. |

## Compile and EditMode on merged main

A fresh detached worktree with an empty `Library` compiled in 96 s: Tundra built 1,150 of 1,203 items with
0 `error CS`. The same three warnings as earlier runs appear. The import log also shows Unity AI Assistant
package resources failing to import because the scratch path exceeds the Windows path limit; no project
code is involved.

| Assembly | Total | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: | ---: |
| `TacticalDirector.ClientApp.Tests` | 285 | 282 | 0 | 3 |
| `TacticalDirector.SeasonSave.Tests` | 559 | 555 | 0 | 4 |
| **All** | **844** | **837** | **0** | **7** |

These are the same counts as the pre-merge run. All 47 `ClientCareerCoordinatorTests` passed, and so did the
engine-backed `sim_CancelledManagedRoundReplaysExactlyFromThePreRoundSave` (195 s) on the A′ engine.

## Window close during a save (player)

A development player with Career enabled continued the saved career and clicked *Save to manual slot 2*. A
watcher thread in the player polled the save directory and posted `WM_CLOSE` to the player's own window as soon
as the write's first temporary file appeared. That is the window-close path a player uses. Times are
milliseconds on one in-process clock per launch.

| Trial | Temp file seen, `WM_CLOSE` posted | Quit requested (`wantsToQuit`) | Write finished, directory clean | `quitting` event | Quit request to process exit |
| --- | ---: | ---: | ---: | ---: | ---: |
| SQ1 | 826.5 | not hooked (build 1) | ~846 | not hooked | exit code 0 |
| SQ2 | 847.1 | 849.7, temp file present | ~873 | 874.8 | 324 ms, code 0 |
| SQ3 | 811.4 | 814.0, temp file present | ~842 | 843.8 | 198 ms, code 0 |
| SQ4 | 832.3 | 834.5, temp file present | ~857 | 861.1 | 182 ms, code 0 |

In every trial quit began while the write was in progress. The slot then went through the full staged
sequence: body temp, sidecar temp, backups, swap. The directory was clean before Unity raised `quitting`, which
it does after all `OnApplicationQuit` handlers, including `ClientCareerCoordinator.Dispose`, have returned. So
shutdown waited for the admitted write and did not abandon it; where the probe's own quit handler ran first
(SQ2, SQ3), the wait was about 21 and 25 ms. `manual-2`'s sequence advanced
4 → 7 across SQ2–SQ4. Every sidecar digest matches its body, and no temporary or backup file remained. A
relaunch listed slots 1 and 2 as Ready and Continue resumed the career. No player log contains an exception.

The refused-before-admission case was again not reached live: a click admits the write in its own frame, and
the window message is handled later. That case stays covered by
`ShutdownBeforeWriteAdmissionPreservesThePairAndAllowsDeterministicReplay`.

## Steady-state allocations (development player)

Each state was sampled for 600 frames after 120 settle frames, with the probe doing nothing but array writes.
`GC Allocated In Frame` counts all threads. The main-thread figure is a `GC.Alloc` marker recorder restricted
to the main thread (`CollectOnlyOnCurrentThread`).

| State | All-thread bytes (600 frames) | Frames with any allocation | Main-thread `GC.Alloc` samples |
| --- | ---: | ---: | ---: |
| Main menu, career initialized | 0 | 0 | 0 |
| Career Home, idle before the round | 0 | 0 | 0 |
| Managed round in progress | 2,095,977 | 137 | **0** |
| Career Home, idle after the round | 0 | 0 | 0 |
| Control: main thread allocates 256 B each frame | 172,800 | 600 | 600 |
| Control: worker thread allocates, main idle | 152,640 | 409 | 0 |

The controls show that the main-thread recorder sees main-thread allocations and ignores worker ones. The
career UI therefore allocates nothing per frame in every steady state, including while it polls a running
round. The round's allocations are on the worker: match engine, season loop and development-build logging.
The cancellation poll adds a counter decrement and, every 64 ticks, a token check, neither of which allocates.
Worker allocation could not be compared between arms on Unity: `GC.GetAllocatedBytesForCurrentThread` returns
0 under this Mono runtime, and the earlier all-thread run (ALLOC, build 1) gave a similar 2,072,616 B.

## Throughput and no-cancel result identity (Editor batch mode)

The bench builds one managed four-club career, saves the pre-round state, then repeatedly restores those
bytes and times `AdvanceAndPlayNextRound()` on the calling thread, with one warm-up and five measured rounds per
arm. It ran with `-batchmode -nographics -releaseCodeOptimization` on an otherwise idle host, PRE first.

| Arm | Code | Round time, runs 1–5 (s) | Mean | Median |
| --- | --- | --- | ---: | ---: |
| PRE | `52466c28`, before cancellation polling | 120.0, 113.3, 114.1, 111.6, 112.0 | 114.2 s | 113.3 s |
| POST | `b412780c`, cancellation polling | 112.2, 113.1, 113.5, 115.1, 113.8 | 113.6 s | 113.5 s |

The difference (−0.5% mean, +0.2% median) is well inside the spread between runs of the same arm (7.6% for PRE, 2.6% for POST), so
polling has no measurable cost. Both arms built byte-identical checkpoints. All twelve rounds, warm-ups
included, produced the same season snapshot (`e8b46607…`) and the same post-round save (`03973322…`). An
uncancelled round is therefore result-identical to the pre-cancellation path. The arms predate the A′ engine
change on main, which keeps the engine constant; merged main was not benchmarked against them.

## Method

Compile and EditMode ran from the command line in a fresh worktree of `5a3c310e`. The bench and the
development player used a second, sparse worktree (`source-identity.txt`). The player build enabled Career in
the scene and added the probe driver, activated only by `-careerProbe`. Neither change was committed. The
owner's open editor was not used. Probe saves went to the normal persistent-data path, which did not exist
before the run, and were moved out afterwards.

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | 2026-11-19 | Created: merged-main compile and EditMode, player close during save, steady-state allocations, throughput and result identity. |
