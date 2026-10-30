# Pinned-host J-05 re-measure — PR #520 after merging main: Unity compile, EditMode and the 10× development-player run

**Captured:** October 29, 2026, 19:59–20:17 UTC (compile, EditMode, player builds) and October 30, 2026, 01:07–01:21
UTC (the B8/B10 conditions); host clock October 29, 12:59–18:21 Pacific.\
**Created:** October 30, 2026.\
**Purpose:** The last open Gate-J finding, J-05: the S0 client's 10× rung delivered 315–340 sim ticks/s against a 600
target on October 19. PR #520 makes the simulation cheaper (design P1a + P1b,
[`perception-shared-pass-design.md`](../../perception-shared-pass-design.md)). Its pinned-host checklist asks for a
Unity compile, an EditMode run of the new fixtures, and a development-player 10× re-measure. The compile and EditMode
legs were met at `01a5fcdc` ([`../host-01a5fcdc-20261028/`](../host-01a5fcdc-20261028/README.md)); since then main gained
PR #518, so the merged head is a new `src` tree and both legs are repeated here, followed by the first player
re-measure.\
**Host:** the owner's Windows 11 machine (i5-9300H, GTX 1650), Unity 6000.4.9f1 (f7258d6eebbe), Mono. This is the
certified determinism host. The owner's own Editor on the main checkout stayed open throughout; nothing else ran during
the player conditions and the owner was away from the PC.\
**Source:** commit `05898cd0` (the merge of main `4878d43c` into PR #520 head `56cfd3e0`). `source-identity.txt` gives
the tree hashes. The `main` control player is built from `4ff45ee8`, whose `src`, `Packages`, `ProjectSettings` and
`Assets` trees equal main's (`4878d43c`).\
**Integrity:** `SHA256SUMS` covers every other file in this directory and is verified by
`tools/dotnet-ci/check_evidence_manifests.py`. Screenshots are in plain `.tar` files under 1 MiB because `*.png` is
LFS-tracked and the evidence job checks out without LFS.

| File | What it is |
| --- | --- |
| `source-identity.txt` | Commit, parents and tree hashes; empty `src`/`Packages`/`ProjectSettings` porcelain before the compile and after both test runs. |
| `compile.txt` | Excerpt of the batch-mode full import from an empty `Library`: `Tundra build success` (1,150 of 1,203 items), 0 `error CS`, the three known warnings, 75 assemblies for 75 asmdefs, exit 0. |
| `editmode-a-results.xml.xz`, `editmode-b-results.xml.xz` | NUnit 3.5 XML. Run A: DeterministicSim, CollisionSystem, ClientApp, UiFramework and MatchClientCore tests. Run B: `MatchEngineSnapshotDigestReferenceTests`. |
| `editmode-summary.txt` | Commands, times, per-assembly counts, every case of the named fixtures, and each ignored test with its reason. |
| `perf/capture.log` | One summary line per player condition (`S0HostPerf.DumpFile`): frames, FPS, p50/p95/p99/max, frames over 16.7 ms, sim ticks/s and per-marker cadence. |
| `perf/frame-csv.tar.xz` | The per-frame CSVs behind the twelve summaries. |
| `perf/end-states-s100.tar`, `perf/end-states-s200.tar` | The player's screen at the end of each hold, showing the selected speed and the statistics state. |
| `perf/builds.txt` | The four development-player builds (one discarded, see Method). |
| `probe-source/run_conds.ps1.txt`, `probe-source/S0J05Batch.cs.txt` | The condition sequencer and the batch build entry written for this run. Every other harness file is the owner's October copy, changed only in its output folder (see Method). |

The player `.raw` profiles, player logs and builds stay outside the repository.

## 1. Unity compile and EditMode at `05898cd0`

Fresh detached worktree, empty `Library`, `Assets/Scripts` junction to its own `src`.

- **Compile:** 0 `error CS`, exit 0, the three known warnings (`BallIntegrationTests.cs(330)` CS0219,
  `ClientShellBehaviour.cs(146, 154)` CS0618), none in #520's or #518's files.
- **Run A:** 566 tests, **554 passed, 0 failed**, 12 ignored by attribute (ClientApp 182/184, CollisionSystem 40/49,
  DeterministicSim 90/91, MatchClientCore 190/190, UiFramework 52/52). Named fixtures: `SnapshotDigestBackendTests` 4/4
  (CNG active under Unity's Mono; steady-state `Encode` allocates nothing by Unity's recorder),
  `SpatialHashGridQueryOrderTests` 2/2. The 12 ignored are the two env-gated `S0SimThroughputDiagnosticTests`
  benchmarks and the ten Stage 0+1 placeholders, as at `01a5fcdc`.
- **Run B:** `MatchEngineSnapshotDigestReferenceTests` 1/1 (900 real ticks, each digest re-derived and chain-linked).
- `src`, `Packages` and `ProjectSettings` porcelain was empty before the compile and after both runs.

## 2. J-05 re-measure in the development player

**Method — the October B8/B10 method, unchanged.** Windows x64 Mono development players (`runInBackground` off as
committed), 1920×1080 window, one player session per condition driven by keyboard only. The player writes its own
profiler log (`-profiler-enable -profiler-log-file`) while an interactive Editor records; each condition is held 30 s,
and the last ~20 s before the 10 quit frames (1,989 frames) are summarized. Sim ticks/s is the count of
`SessionMatchAnalytics.ObserveTick` samples on any thread divided by the summed frame time. Keys are real scan codes and
are only ever sent while the player owns the foreground window. The harness is the owner's October copy
(`S0HostBuild`, `S0HostPerf`, `S0HostChannel` identical to `../p5b-real-client-c95e27ba-20261019/probe-source/`;
`pcond.ps1` v2 identical to `../p5b-gatej-recapture-20261029/probe-source/pcond.ps1.txt`), changed only in its output
folder (`S0HostProbe.OutDir`, `pcond.ps1` `$root`, `pkey.ps1` `$perf`). Two additions: `S0J05Batch` builds the players in
batch mode through the unchanged `S0HostBuild.Build`, and `run_conds.ps1` runs the conditions in a fixed order and
summarizes each. **`main` ran in the same session as a control**, alternating with #520 on the 10× conditions so that
host drift hits both alike.

| Build | Text | Condition | mean FPS | p50 | p99 | max | > 16.7 ms | sim ticks/s |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| main | 100% | 10×, stats open | 208 | 4.48 | 11.29 | 14.46 | 0 | 557 |
| **#520** | 100% | 10×, stats open | 204 | 4.55 | 10.88 | 13.72 | 0 | **601** |
| main | 100% | 10×, stats closed | 209 | 4.61 | 6.85 | 8.29 | 0 | 516 |
| **#520** | 100% | 10×, stats closed | 218 | 4.40 | 6.47 | 7.70 | 0 | **602** |
| #520 | 100% | 1×, stats closed | 236 | 4.09 | 5.97 | 8.48 | 0 | 60 |
| #520 | 100% | 1×, stats open | 231 | 4.18 | 6.31 | 11.37 | 0 | 60 |
| #520 | 200% | 1×, stats closed | 248 | 3.95 | 5.94 | 7.93 | 0 | 60 |
| #520 | 200% | 1×, stats open | 238 | 4.07 | 6.29 | 10.62 | 0 | 60 |
| **#520** | 200% | 10×, stats open | 215 | 4.31 | 9.85 | 13.45 | 0 | **601** |
| **#520** | 200% | 10×, stats closed | 227 | 4.22 | 6.26 | 11.04 | 0 | **601** |
| main (repeat) | 100% | 10×, stats open | 178 | 5.23 | 12.85 | 14.11 | 0 | 468 |
| **#520** (repeat) | 100% | 10×, stats open | 203 | 4.62 | 9.73 | 12.45 | 0 | **601** |

Times are milliseconds of main-thread CPU frame time; budget p99 ≤ 16.7 ms.

**Result — J-05 PASS.** Every #520 10× condition delivered **601–602 sim ticks/s**, the 600 ticks/s the 10× rung asks for
(the extra tick is the window's edge): the pace, not the simulation, is now the limit. `main` in the same session
delivered 468–557. 1× holds 60 ticks/s. B8/B10 still hold: the worst #520 p99 is 10.88 ms, no frame over 16.7 ms in
any condition, and frame times are no worse than `main`'s. The end-state screenshots show "Selected speed 10×" with
"Already at the fastest speed — faster is unavailable" in each 10× condition, "Selected speed 1×" in the 1× ones,
and the statistics panel open or closed as named.

**Reading the control.** `main` varied by 89 ticks/s across its three 10× sessions (468–557) and delivered 551–586 in
the October 29 recapture on the same host; how far short of 600 it falls depends on what else the host is doing. #520
was at 601–602 in all six 10× sessions, including the two run between `main`'s.

**Not measured.** The `gcBytes` column of `capture.log` reads 0 for every condition, as it did for the 100% rows of the
October 29 recapture: the per-frame GC counter was not present in these player logs, so this run says nothing about
allocation (the Unity allocation leg is `SnapshotDigestBackendTests.Encode_SteadyState_AllocatesNothing`, §1). The
FR-PO-052 baseline (design E5, `MatchEngineCapstonePerfHarnessTests`) was not re-run.

## Method notes and incidents

- **Builds.** The first `main` build came from a worktree that still carried an unrelated
  `UnityConnectSettings.asset` change from an earlier session; it was deleted and rebuilt after restoring that file.
  Every build then flips that file's `m_Enabled` 0 → 1 and re-serializes `Scene.unity` while setting the text scale,
  so all three measured players were built alike; both files were restored with git after each build.
- **Firewall.** Each new player's first launch raised a Windows Firewall prompt. The first attempt (October 29, 20:18 UTC)
  stopped before its first key because the prompt held the foreground: the key guard refused and nothing was sent. The
  harness never answers such a prompt. When the run was restarted at 01:07 UTC the prompts were gone and Windows held an
  inbound "Allow, Private" rule for each of the three players; no key was refused in that run.
- **Editor.** The recording Editor was reopened for the second attempt; its channel answered before the first
  condition.

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | October 30, 2026 | Initial evidence set: Unity compile and EditMode at the merged head `05898cd0`, and the J-05 development-player re-measure with a same-session `main` control. |
