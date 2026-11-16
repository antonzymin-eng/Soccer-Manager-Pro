# Pinned-host Unity validation — PR #543 source/test target `75d62186` (#434 formation and Duty)

**Captured:** November 11, 2026, 03:31–04:04 UTC (November 10 local).\
**Created:** November 10, 2026.\
**Purpose:** The owner-run Unity 6000.4.9f1 compile and unfiltered four-assembly EditMode run requested in
[`HOST-REQUEST.md`](../formation-duty-20261109/HOST-REQUEST.md) for PR #543.\
**Host:** the owner's Windows 11 machine with Unity 6000.4.9f1 (f7258d6eebbe), Mono. This is the certified
determinism host.\
**Source:** commit `75d62186264b2fe0615812cc1d1e0f6e06d20993`, `src` tree `fd5a1a5d5a70691f917141d5643e97157ba90493`.
Later PR heads `dc2a0e9e` and `46cee75f` change documentation/evidence only, so this run covers their source, tests
and Unity inputs. Fresh detached worktree with no `Library`, so the compile is a full recursive import;
`Assets/Scripts` is a directory junction to that worktree's own `src`. `source-identity.txt` gives the tree hashes
and the porcelain counts.\
**Integrity:** `SHA256SUMS` covers every other file in this directory and is verified by
`tools/dotnet-ci/check_evidence_manifests.py`.

## Result

| Check | Result |
| --- | --- |
| Compile (empty `Library`) | `Tundra build success` (32.98 s, 1,150 of 1,203 items), **0 `error CS`**, exit 0, only the three known warnings |
| EditMode, four assemblies, unfiltered | 1,424 results: **1,394 passed / 0 failed** / 29 skipped / 1 inconclusive, 1,856 s |
| `TacticalDirector.MatchEngine.Tests` | 998: 981 passed / 0 failed / 16 skipped / 1 inconclusive |
| `TacticalDirector.PositioningAI.Tests` | 156: 143 passed / 0 failed / 13 skipped |
| `TacticalDirector.MatchClientCore.Tests` | 192: **192 passed** |
| `TacticalDirector.MatchViewer.Tests` | 78: **78 passed** |
| `MatchEngineFormationDutyTests` | **42 / 42 passed**, including all six `KickoffSelection_FirstUseAllocatesNothing_AndFirstTickKeepsBaseline` cases and raw requested 255 / effective F442 for both teams |
| Schema, rotation, restart, SlotComposerStage, TacticTranslation, TickStampedCommandReplay, LiveMatchFrameCue | All passed (`editmode-summary.txt` lists each) |
| Source porcelain | `git status --short` 0 lines before the compile and after the run; ignored-inclusive `src`/`Packages`/`ProjectSettings` 0 after the run |

**Native allocation oracle.** Under Unity the six kickoff-selection cases compile the `UNITY_5_3_OR_NEWER` branch:
first `Assert.That(control, UnityIs.AllocatingGCMemory())` on a known 1,024-byte allocation (positive control), then
`Assert.That(select, UnityIs.Not.AllocatingGCMemory())` on the cold `InitializeFormation` selection/seeding seam. All six
cases passed, so the native recorder detected the control allocation and recorded no managed allocation for the
selection/seeding path. The .NET counter branch did not run here. This does not address the separately tracked
536 B whole-first-tick cost or later-frame residual
([open issue](../../open-issues.md#engine-frame-allocation)).

The 16 MatchEngine skips are the env-gated diagnostic instruments, and the 13 PositioningAI skips are the existing
Stage 0+1 deferrals (`T-D-*`, `T-I-*`, `T-P-*`); neither set is new. The inconclusive case is the existing
`ATackleFoulIsGivenAsASlideTackleAndNotJudgedTwice`, whose `connected > 30` assumption is unmet (23 here); it is
also inconclusive on Linux and in every earlier host run. Unity's batch runner treats that inconclusive result as a
failure for its process exit code (2) while the XML records 0 failed. No full-match case timed out.

This run does not authorize merge. PR #543 still requires the held trace diagnosis and the
[player-control blocker](../../open-issues.md#formation-duty-player-control-blocker).

## Method

`Unity.exe -batchmode -quit` imported the fresh worktree, then a second batch process ran
`-runTests -testPlatform EditMode -assemblyNames
TacticalDirector.MatchEngine.Tests;TacticalDirector.PositioningAI.Tests;TacticalDirector.MatchClientCore.Tests;TacticalDirector.MatchViewer.Tests`,
following `HOST-REQUEST.md`. No Editor was attached to the worktree, no filter or exclusion was applied, diagnostic
flags stayed at their defaults, and no project asset, scene or setting changed. The Editor logs are kept with
licensing, machine-identifier and player-connection/LAN-address lines removed and the local user path replaced by
`<home>` / `<worktree>`; nothing else in them is altered. The NUnit XML is byte-identical to the runner's output.

| File | What it is |
| --- | --- |
| `source-identity.txt` | Commit, parent and tree hashes; porcelain counts; process exit codes and times |
| `compile.txt` | Excerpt of the import log: compilation requests, Tundra result, warnings, counts |
| `compile-editor-log.txt.xz` | The full redacted import Editor log |
| `editmode-results.xml.xz` | NUnit 3.5 XML for the four-assembly EditMode run |
| `editmode-editor-log.txt.xz` | The full redacted test-run Editor log |
| `editmode-summary.txt` | Command, times, per-assembly counts, the requested fixtures, all 42 formation cases, every non-passed case with its reason, and the longest cases |

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | November 10, 2026 | Initial host evidence for PR #543 at `75d62186`: clean Unity compile and four-assembly EditMode with 0 failures; formation fixture 42/42 with the native allocation oracle. |
