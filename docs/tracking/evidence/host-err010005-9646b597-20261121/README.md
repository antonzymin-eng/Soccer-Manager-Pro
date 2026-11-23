# Pinned-host Unity validation — PR #559 head `9646b597` (#441 `ERR-010-005`, rerun)

**Captured:** November 21, 2026, 04:59–05:34 UTC.\
**Created:** November 21, 2026.\
**Purpose:** Re-run the owner-requested Unity 6000.4.9f1 compile and unfiltered four-assembly EditMode set on PR #559
after the [`199e5399` run](../host-err010005-199e5399-20261120/README.md) failed
`HeaderReachabilityObserver_DoesNotChangeSnapshotDigests` and `3feab44c` lengthened that test's neutrality window.\
**Host:** the owner's Windows 11 machine with Unity 6000.4.9f1 (f7258d6eebbe), Mono. This is the certified
determinism host.\
**Source:** commit `9646b5974f96df16979e31cd8bdf340620876d93` (merge of main `c7861ed3` into the branch), `src` tree
`f88ea3669c95b99dbadd065af6d0c4ddf2383d01`, equal to the `src` tree of `3feab44c`. `Assets/Scripts` is a directory
junction to that worktree's own `src`. `source-identity.txt` gives the tree hashes and the porcelain counts.\
**Integrity:** `SHA256SUMS` covers every other file in this directory and is verified by
`tools/dotnet-ci/check_evidence_manifests.py`.

## Result

| Check | Result |
| --- | --- |
| Compile | `Tundra build success` (13.07 s, 403 of 1,203 items; `TacticalDirector.MatchEngine.dll` and `.Tests.dll` recompiled from this tree), **0 `error CS`**, exit 0, only the three known warnings |
| Neutrality test alone, before the full run | **Passed** (1/1), exit 0 |
| EditMode, four assemblies, unfiltered | 1,538 results: 1,507 passed / **0 failed** / 30 skipped / 1 inconclusive, 1,939 s, exit 2 |
| `TacticalDirector.MatchEngine.Tests` | 1,112: 1,094 passed / 0 failed / 17 skipped / 1 inconclusive |
| `TacticalDirector.PositioningAI.Tests` | 156: 143 passed / 0 failed / 13 skipped |
| `TacticalDirector.MatchClientCore.Tests` | 192: **192 passed** |
| `TacticalDirector.MatchViewer.Tests` | 78: **78 passed** |
| PR #559 fixtures | `GkHeadingIntentSourceTests` **25/25**, `MatchEngineGkHeadingTests` **38/38**, `MatchEngineGkHeadingScenarioTests` 1/1, `HeaderReachabilityDiagnosticTests` 14 passed / 1 skipped (the env-gated six-seed counter instrument) |
| Snapshot fixtures | `MatchEngineSnapshotSchemaTests` 28/28, `MatchEngineSnapshotRestoreTests` 19/19, `MatchEngineSnapshotDigestReferenceTests` 1/1 |
| Source porcelain | `git status --short` 0 lines before the compile and after the run; ignored-inclusive `src`/`Packages`/`ProjectSettings` 0 |

### The neutrality test under Mono

`HeaderReachabilityObserver_DoesNotChangeSnapshotDigests` passes in the full run (15.1 s) and alone (15.8 s). Its
output line is `neutrality window: 23592 frames, first #10 evaluation at frame 22992`: under Unity Mono the first
Heading evaluation for seed `0x0F1E2D3C4B5A6978` arrives at frame 22,992, past the old fixed 18,000-frame window
(which is why `199e5399` failed) and well inside the 108,000-frame cap. The window then ran 600 more frames, the
non-vacuity guards held, and the observed and unobserved engines' snapshot digests matched. On Linux .NET the same
test reaches its first evaluation at frame 3,775 (PR #559's own receipt); this run does not explain why the runtimes
differ.

### Existing non-passes

Unity exits 2 because one case is not Passed. That case is the existing inconclusive
`ATackleFoulIsGivenAsASlideTackleAndNotJudgedTwice` (`connected > 30` unmet, 26 here, the same count as at
`199e5399`); it is inconclusive on Linux and in every earlier host run. The 17 MatchEngine skips are the env-gated
diagnostic instruments and the 13 PositioningAI skips are the existing Stage 0+1 deferrals; both lists match the
`199e5399` run exactly (`editmode-summary.txt`).

**On the pinned host, PR #559 at `9646b597` has no failures.** This clears the host gate that the `199e5399` run left
open; it does not change any #441 measurement or close #441.

## Method

`Unity.exe -batchmode -quit -nographics` compiled the worktree. A second batch process ran the neutrality test alone
with `-testFilter`, and a third ran `-runTests -testPlatform EditMode -assemblyNames
TacticalDirector.MatchEngine.Tests;TacticalDirector.PositioningAI.Tests;TacticalDirector.MatchClientCore.Tests;TacticalDirector.MatchViewer.Tests`,
the assembly set of the PR #543 and `199e5399` host runs. No Editor was attached to the worktree, no filter or
exclusion was applied to the full run, diagnostic flags stayed at their defaults, and no project asset, scene or
setting changed.

**Deviation from earlier host runs (same as `199e5399`):** the worktree's `Library` was copied from the owner's main
checkout instead of starting empty, to skip the full import. The commit that `Library` was last compiled against is
not recorded. Unity's script compilation pipeline hashes its inputs, and the compile log shows the engine assemblies
rebuilt by `Csc` from this tree (`compile.txt`).

The Editor logs are kept with licensing, machine-identifier and player-connection/LAN-address lines removed and the
local user path replaced by `<home>` / `<worktree>` / `<log-dir>`; nothing else in them is altered. The NUnit XML
files are byte-identical to the runner's output.

| File | What it is |
| --- | --- |
| `source-identity.txt` | Commit, parent and tree hashes; porcelain counts; process exit codes and times |
| `compile.txt` | Excerpt of the compile log: compilation requests, Tundra result, MatchEngine `Csc` lines, warnings, counts |
| `compile-editor-log.txt.xz` | The full redacted compile Editor log |
| `single-test-9646b597-results.xml.xz` | NUnit XML: the neutrality test alone at the PR head (passed) |
| `editmode-results.xml.xz` | NUnit 3.5 XML for the four-assembly EditMode run |
| `editmode-editor-log.txt.xz` | The full redacted test-run Editor log |
| `editmode-summary.txt` | Command, times, per-assembly counts, the PR and snapshot fixtures, the neutrality test's output line, every non-passed case with its message, the skips, the longest cases and the single-test result |

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | November 21, 2026 | Initial host evidence for PR #559 at `9646b597`: clean Unity compile; four-assembly EditMode 1,507 passed / 0 failed / 30 skipped / 1 existing inconclusive; the neutrality test passes, with its first #10 evaluation at frame 22,992 under Mono. |
