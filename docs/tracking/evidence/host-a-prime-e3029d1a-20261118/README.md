# Pinned-host Unity validation — #440 A′ at `e3029d1a`

**Captured:** November 18, 2026, 23:37 UTC – November 19, 2026, 00:26 UTC.\
**Created:** November 18, 2026.\
**Purpose:** The governing Unity 6000.4.9f1 compile and EditMode run (D-01a) for A′ (`ERR-014-011`, #14 §3.6.5.8):
the BallLoose geometry knock and the same-frame carrier fence, PR #549 stacked on PR #544.\
**Host:** the owner's Windows 11 machine with Unity 6000.4.9f1 (f7258d6eebbe), Mono. This is the certified
determinism host.\
**Source:** commit `e3029d1a9040972fbc68500271ef6e02813a9abd` (parent `7ccc847f`, PR #544 head), `src` tree
`033873ac5b26cc9b50f1aec05eb9b8e81cd72529`. Fresh detached worktree with no `Library`, so the compile is a full
recursive import; `Assets/Scripts` is a directory junction to that worktree's own `src`; Git LFS materialized for
`Assets/**` and `src/**` (the native `td_mxcsr.dll` is the real binary, hash in `source-identity.txt`).\
**Integrity:** `SHA256SUMS` covers every other file in this directory and is verified by
`tools/dotnet-ci/check_evidence_manifests.py`.

## Result

| Check | Result |
| --- | --- |
| Compile (empty `Library`) | `Tundra build success` (46.56 s, 1,150 of 1,203 items), **0 `error CS`**, exit 0. Only the warnings already in the [W13 receipt](../host-w13-6491de79-20261113/README.md) (`BallIntegrationTests.cs` CS0219, `ClientShellBehaviour.cs` CS0618 ×2), none in A′ files |
| `TacticalDirector.MatchEngine.Tests`, EditMode, unfiltered | 1,093 results: **1,075 passed / 0 failed** / 17 skipped / 1 inconclusive, 1,945 s |
| `TacticalDirector.DefensiveAI.Tests`, EditMode, unfiltered | 181 results: **153 passed / 0 failed** / 28 skipped |
| `MatchEngineBallLooseReleaseTests` (new) | **14 / 14 passed**: knock velocity mirrored home/away, the fence on pickup, first touch and keeper hands with unfenced controls, fence lifetime, and composed play with exact restore at the BallLoose frame |
| `TackleOutcomeResolverTests` | **19 / 19 passed**, including the six A′ knock cases |
| Source porcelain (`src`, `Packages`, `ProjectSettings`) | 0 lines after the test run |

The skips are the env-gated diagnostics and the Stage 0+1 DefensiveAI deferrals listed in `editmode-summary.txt`,
unchanged in kind from earlier host runs. The inconclusive case is the existing
`ATackleFoulIsGivenAsASlideTackleAndNotJudgedTwice` (`connected > 30` unmet; here won 9, loose 15, foul 2), inconclusive in
every earlier host run and on Linux; it makes Unity's process exit code 2 while the XML records 0 failed.

The composed restore test runs its two engines one after the other, as on Linux, because `EventBus` is
process-static; it passed here on Mono.

## Method

`Unity.exe -batchmode -quit -nographics` imported the fresh worktree, then a second batch process ran
`-runTests -testPlatform EditMode -assemblyNames "TacticalDirector.MatchEngine.Tests;TacticalDirector.DefensiveAI.Tests"`.
No Editor was attached to the worktree, no filter or exclusion was applied, and no project asset, scene or setting
changed. The full Editor logs are kept with licensing lines removed and the local user path replaced by
`<home>` / `<worktree>` / `<user>`; nothing else in them is altered.

| File | What it is |
| --- | --- |
| `source-identity.txt` | Commit, parent and tree hashes; native DLL hash; process exit codes and times; porcelain count |
| `compile.txt` | Excerpt of the import log: Tundra result, warnings, exit |
| `compile-editor-log.txt.xz` | The full redacted import Editor log |
| `editmode-results.xml.xz` | NUnit 3.5 XML for the two-assembly EditMode run |
| `editmode-editor-log.txt.xz` | The full redacted test-run Editor log |
| `editmode-summary.txt` | Command, times, per-assembly counts, every A′ case, every non-passed case with its reason, and the longest cases |

## Not covered

Only the two assemblies A′ touches were run, not the full EditMode suite. The before/after behaviour is in
[`ball-loose-a-prime-char-20261118`](../ball-loose-a-prime-char-20261118/README.md) (local .NET lane). The tested commit
contains all of PR #544 (`7ccc847f`) plus A′, so it also compiles #544's source on this host; it is not a receipt for
#544's head on its own.
