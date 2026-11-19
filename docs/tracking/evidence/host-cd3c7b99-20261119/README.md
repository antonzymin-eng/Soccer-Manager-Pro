# Pinned-host Unity acceptance run — career shutdown and startup at cd3c7b99

**Captured:** November 19, 2026, 04:16–05:45 UTC (November 18, 20:16–21:45 on the host's Pacific clock)\
**Purpose:** Pinned-host evidence for the P7b career shutdown change
([design §Acceptance item 4](../../career-shutdown-cancellation-design.md)) and the career startup scene
([open issues](../../open-issues.md#career-startup-unity-acceptance)) on `feat/c3-c4-career-shell` at `cd3c7b99`.
Its `src` tree equals that of `b412780c`; the two later commits change only documents.\
**Host:** the owner's Windows 11 machine, Unity 6000.4.9f1 (f7258d6eebbe), Mono. This is the certified host.\
**Integrity:** `SHA256SUMS` covers every other file in this directory and is verified by
`tools/dotnet-ci/check_evidence_manifests.py`.

| File | What it is |
| --- | --- |
| `source-identity.txt` | Commit and tree hashes, run times, and the three worktree changes made only for the player build. |
| `compile.txt` | Clean batch-mode import of a fresh worktree: 0 `error CS`, 75 `TacticalDirector.*` assemblies for 75 asmdefs. |
| `editmode-results.xml.xz` | NUnit 3.5 XML for ClientApp.Tests and SeasonSave.Tests. Host paths are replaced with `<scratch>` and `%USERPROFILE%`. |
| `editmode-summary.txt` | Command, times, per-assembly counts, the shutdown and replay cases, and the seven skips with reasons. |
| `editor-play-transcript.txt` | Editor Play runs: clicks, UI text dumps, play-mode transitions and profiler-marker samples. |
| `player-transcript.txt` | Player runs: harness timings, the in-player driver log and exception counts. |
| `career-probe-driver.cs.txt` | The scratch driver compiled into the test player. It is not part of `src` and was not committed. |
| `save-hashes.txt` | SHA-256 of every save-directory snapshot and the comparisons drawn from them. |

## Compile and EditMode

The compile ran in a fresh detached worktree with an empty `Library` (`-batchmode -quit`, 75 s). Tundra built
1,150 of 1,203 items with 0 `error CS`. The same three warnings as earlier host runs appear, none in shutdown code.

| Assembly | Total | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: | ---: |
| `TacticalDirector.ClientApp.Tests` | 285 | 282 | 0 | 3 |
| `TacticalDirector.SeasonSave.Tests` | 559 | 555 | 0 | 4 |
| **All** | **844** | **837** | **0** | **7** |

All 47 `ClientCareerCoordinatorTests` passed, including the six shutdown cases and
`ShutdownBeforeWriteAdmissionPreservesThePairAndAllowsDeterministicReplay`. The real-engine replay case
`SeasonLoopTests.sim_CancelledManagedRoundReplaysExactlyFromThePreRoundSave` passed in 194 s. The seven skips are the
existing opt-in diagnostics. `MatchClientUnity` has no test assembly, so its coverage is the compile and the Play runs.

## Shutdown acceptance

| Case | Where | Result |
| --- | --- | --- |
| Stop during managed matchday, about 40 s into the real-engine round | Editor | Stop request to edit mode: **219 ms** (idle Stop: 425 ms). Save directory byte-identical to the pre-round snapshot; no temporary or backup files. |
| Reopen, Continue, replay the round | Editor | Continue loaded the pre-round manual save. The round resolved Thorncliff United 6–2 Halloway City, and all four save files match the uninterrupted reference run byte for byte. |
| Window close (WM_CLOSE) during managed matchday, about 40 s in | Player | Close to process exit: **717 ms**, exit code 0. Save directory byte-identical to the pre-round snapshot. |
| Relaunch, Continue, replay the round, twice from the same pre-round bytes | Player | Both runs resolved 1–5 against Kesgrave Rovers and wrote byte-identical saves. No exceptions in any player log. |
| Quit after the write was admitted (temp file present 5.8 ms after Save) | Editor | `OnApplicationQuit` (Dispose) waited **4.8 ms**. The slot pair committed with a matching digest and no leftover files. Play exited in 303 ms. |
| Quit in the same frame as the Save click | Editor | The worker had already been admitted during the 5 ms click handler. Dispose waited **22.8 ms** and the pair committed with a matching digest. The refused-write case was not reached live. It is covered by the EditMode case named above. |

Every slot sidecar's `saveDigest` matched its save file, and sequences ran 2 to 5 without gaps. The only Console
errors were the Unity AI Assistant's subscription notices and one `MissingRoot` wiring rejection from a first Play
attempt. That attempt is explained in the transcript header: the open scene had been reloaded before the new
scripts compiled. Reopening the scene from disk showed all three career roots assigned.

**Save timings** (`ProfilerRecorder` per-frame totals of the two markers, read on the main thread):

| Save | `SeasonSave.Encode` | `CareerSaveSlots.Write` (includes Encode) |
| --- | ---: | ---: |
| Career creation autosave (first save in the session) | 15.5 ms | 40.9 ms |
| Manual save on the matchday | 1.2 ms | 16.5 ms |
| Round autosaves (two) | 1.7 ms, 3.3 ms | split across frame boundaries; not reliable |

The per-frame recorder is not a per-call timer. A save that crosses a frame boundary is split across rows, and
those rows are listed raw in the transcript. A Profiler capture would give exact per-call times. On these numbers,
encoding is a small share of a save, and the admitted-write wait at shutdown was 5–23 ms.

**Round duration:** 144 s in the Editor for both full rounds. In the player, 173.5 s from Continue to completion.

## Startup checklist (README P7b items)

Observed in the Editor with Career enabled in memory only:

- **Covered:**
  - Item 1: three inactive career roots under the shell.
  - Item 2: compile.
  - Item 3: career actions next to the demo match at startup.
  - Item 4, part: storage failure shows its reason and a Retry control, while career actions are disabled and the demo stays available. Retry then initializes. New career, club selection with Start disabled until a choice, Start with autosave, Continue, manual slot saves.
  - Item 5: every control disabled with the busy notice during a round.
  - Item 6, part: the table and summary update after a round.
  - Picker states: Ready, Empty, Incompatible (sidecar format changed) and Damaged (save file removed), with the unavailable slots disabled. Return to Main Menu without saving.
  - Demo entry: Play a demo match still reaches Tactics Setup with Career enabled.
- **Not covered:**
  - Item 6: autosave failure feedback, manual-save retry, repeated new/load/quit cycles.
  - Item 7: keyboard Tab and Shift-Tab, focus recovery, layouts, 200% text scale, long names.
  - Item 8: the full PM-1 journey with Career on and off.
  - Screenshots.
  - Steady-state allocations.
  - A throughput comparison against the pre-cancellation code; no baseline was run, so polling's cost is not measured.

## Finding outside this change

A save body with one byte flipped and a valid sidecar loaded through Continue without error and was not marked
Damaged. `CareerSaveSlots` uses the sidecar digest only for crash recovery, not on load. This predates the
shutdown change and is recorded here for triage, not fixed.

## Method

Compile, EditMode and the player build ran from the command line (`Unity.exe -batchmode`) in the worktree. Editor
Play checks ran in the owner's open editor through the Unity MCP `RunCommand` tool. `EditorApplication.update`
drivers clicked the real UGUI buttons (`Button.onClick`), wrote UI text dumps, and timed Stop
(`EditorApplication.ExitPlaymode`) and quit (`SendMessage("OnApplicationQuit")`). `Application.runInBackground` was
set for the runs and restored to `false`. The scene was reopened from disk afterwards. Player checks used the
driver in `career-probe-driver.cs.txt`, activated only by `-careerProbe`. Saves were snapshotted outside the game
between steps. No project asset, scene or setting in the repository was modified.

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | 2026-11-19 | Created: pinned-host compile, EditMode, Editor Play and player shutdown evidence for `cd3c7b99`. |
