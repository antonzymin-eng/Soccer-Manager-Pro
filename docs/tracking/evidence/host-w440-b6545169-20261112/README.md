# Pinned-host Unity validation — PR #544 head `b6545169` (#440 BallLoose Stage A)

**Captured:** November 12, 2026, 19:59–20:31 UTC. The superseded prior-head compile ran 14:43–14:56 UTC the same day.\
**Created:** November 12, 2026.\
**Purpose:** The governing Unity 6000.4.9f1 compile and whole-assembly `TacticalDirector.MatchEngine.Tests`
EditMode run that PR #544 (#440 Stage A observer and census) recorded as pending; see
[`w-440-ball-loose-design.md`](../../w-440-ball-loose-design.md).\
**Host:** the owner's Windows 11 machine with Unity 6000.4.9f1 (f7258d6eebbe), Mono. This is the certified
determinism host.\
**Source:** commit `b6545169e722de39e2227878b47b278030f22f93`, `src` tree `96a657be45955eca14fc1f38988efcbd95a5b863`.
Detached worktree with `Library`, `Logs`, `Temp` and `obj` deleted first, so the compile is a full recursive import;
`Assets/Scripts` is a directory junction to that worktree's own `src`. `source-identity.txt` gives the tree hashes
and the porcelain counts.\
**Integrity:** `SHA256SUMS` covers every other file in this directory and is verified by
`tools/dotnet-ci/check_evidence_manifests.py`.

## 1. Result

| Check | Result |
| --- | --- |
| Compile (empty `Library`) | `Tundra build success` (29.40 s, 1,150 of 1,203 items), **0 `error CS`**, exit 0, only the three known warnings, none in #440 files; 75 assemblies for 75 asmdefs |
| `TacticalDirector.MatchEngine.Tests`, EditMode, unfiltered | 976 results: **959 passed / 0 failed** / 16 skipped / 1 inconclusive, 1,815 s |
| `BallLooseEpisodeObserverTests` | **20 / 20 passed** |
| W8 B, W8 C, re-handling and snapshot fixtures | All passed (`editmode-summary.txt` lists each) |
| Source porcelain (`src`, `Packages`, `ProjectSettings`) | 0 lines before the compile, after the compile and after the test run |

The 16 skips are the env-gated diagnostic instruments. The inconclusive case is the existing
`ATackleFoulIsGivenAsASlideTackleAndNotJudgedTwice`, whose `connected > 30` assumption is unmet on this corpus
(23 here); it is inconclusive in every earlier host run. Unity's batch runner treats that inconclusive result as a
failure for its process exit code (2) while the XML records 0 failed.

The separate environment-gated Stage A census was not run on the host. This receipt covers the compile and the
always-run suite only; it does not reproduce the Stage A measurement under Unity.

## 2. Prior head `da713ff5` (superseded)

The first import, at the then PR head `da713ff5` (`src` tree `6384884f`, identical to the `210ede77` target the PR
named), **failed**: `Tundra build failed`, exit 1. The only error was CS0246 at
`BallLooseEpisodeObserverTests.cs(18,19)`: `[NonParallelizable]` exists from NUnit 3.7, and Unity bundles NUnit 3.5.
The Linux gate uses NUnit 3.14, which is why it passed there. `prior-head-da713ff5-compile.txt` has the excerpt.

`b6545169` removes that attribute and changes nothing else under `src` (one file, 3 insertions and 2 deletions,
including the header date and version-history row). Neither runner parallelizes the fixture, so the attribute had no
effect. `S0DemoFixtureComparisonTests` v1.2 made the same correction on October 6, 2026.

Before `b6545169` was committed, the same edit was applied uncommitted in the worktree and the whole suite was run
once as a probe: 959 passed / 0 failed / 16 skipped / 1 inconclusive in 1,857 s. That run had a modified `src`
porcelain, so it is not evidence for any commit and its files are not kept here.

## 3. Method

`Unity.exe -batchmode -quit` imported the worktree, then a second batch process ran
`-runTests -testPlatform EditMode -assemblyNames TacticalDirector.MatchEngine.Tests`. No Editor was attached to the
worktree, no filter or exclusion was applied, and no project asset, scene or setting changed. The full Editor logs
are kept with licensing, machine-identifier, player-connection and LAN-address lines removed and the local paths
replaced by `<home>`, `<worktree>` and `<out>`; nothing else in them is altered. The results XML has the same two
local paths replaced by `[worktree]` and `[out]` (square brackets keep it well-formed) and is otherwise as Unity
wrote it.

| File | What it is |
| --- | --- |
| `source-identity.txt` | Commit, parent and tree hashes; porcelain counts before and after each process; process exit codes and times, for both heads |
| `compile.txt` | Excerpt of the import log at `b6545169`: compilation requests, Tundra result, warnings, counts |
| `compile-editor-log.txt.xz` | The full redacted import Editor log at `b6545169` |
| `matchengine-results.xml.xz` | NUnit 3.5 XML for the whole MatchEngine EditMode run |
| `editmode-editor-log.txt.xz` | The full redacted test-run Editor log |
| `editmode-summary.txt` | Command, times, counts, selected fixtures, every non-passed case with its reason, and the longest cases |
| `prior-head-da713ff5-compile.txt` | Excerpt of the failed import at `da713ff5`: Tundra result and the compiler errors |
| `prior-head-da713ff5-compile-editor-log.txt.xz` | The full redacted failed-import Editor log |

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | November 12, 2026 | Initial host evidence for PR #544 at `b6545169`: clean Unity compile and whole MatchEngine EditMode with 0 failures, after a CS0246 compile failure at `da713ff5`. |
