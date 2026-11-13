# Pinned-host Unity validation — W13 at `6491de79`

**Captured:** November 13, 2026, 21:43–22:18 UTC.\
**Created:** November 13, 2026.\
**Purpose:** The governing Unity 6000.4.9f1 compile and EditMode run (D-01a) for the W13 changes not yet on
main: W13.0 review fixes, W13.1 mark-target wiring (switched off by default) and W13.1c pace tier, on
`claude/great-sagan-rpcmhp` merged with main `39de4662`. Listed as not done in
[open-issues](../../open-issues.md#w13-defender-marking).\
**Host:** the owner's Windows 11 machine with Unity 6000.4.9f1 (f7258d6eebbe), Mono. This is the certified
determinism host.\
**Source:** commit `6491de79714052080011e3fbb3d00853b7638df5`, `src` tree `447828bc7f7a50264bf9076d700bba32a82bbfb1`.
Fresh detached worktree with no `Library`, so the compile is a full recursive import; `Assets/Scripts` is a directory
junction to that worktree's own `src`. `source-identity.txt` gives the tree hashes and the porcelain counts.\
**Integrity:** `SHA256SUMS` covers every other file in this directory and is verified by
`tools/dotnet-ci/check_evidence_manifests.py`.

## Result

| Check | Result |
| --- | --- |
| Compile (empty `Library`) | `Tundra build success` (37.11 s, 1,150 of 1,203 items), **0 `error CS`**, exit 0; only the two warnings already present in the [W8 C receipt](../host-w8c-fdbbff5a-20261106/README.md) (`BallIntegrationTests.cs` CS0219, `ClientShellBehaviour.cs` CS0618), none in W13 files |
| `TacticalDirector.MatchEngine.Tests`, EditMode, unfiltered | 1,017 results: **999 passed / 0 failed** / 17 skipped / 1 inconclusive, 1,912 s. The 999 include the 18 `[Explicit]` descriptive marking cases below, which assert nothing |
| `TacticalDirector.DefensiveAI.Tests`, EditMode, unfiltered | 175 results: **147 passed / 0 failed** / 28 skipped |
| `TacticalDirector.DecisionTree.Tests`, EditMode, unfiltered | 174 results: **170 passed / 0 failed** / 4 skipped |
| `MatchEngineMarkingTests` | 60 results: **41 behavioural cases passed**; 18 `[Explicit]` descriptive cases ran and completed (reported as passed); 1 skipped (the env-gated `IdentityProbe_SixSeedDigestsAtFrame18000`) |
| Other W13 fixtures (`MarkTargetComposerTests` 19, `MatchEngineAwayTeamTests` 2, `DispatcherTests` 38, `OptionGeneratorTests` 55, `TacticTranslationTests` 18) | All passed (`editmode-summary.txt` lists each) |
| Source porcelain (`src`, `Packages`, `ProjectSettings`) | 0 lines before the compile, after the compile and after the test run |

**Descriptive cases are not acceptance coverage.** The 18 cases are
`Descriptive_RunnerEnvelope_PeaksCommandFlipsAndGuardLocks` (8), `Descriptive_StationaryMan_MarkerSettlingAndSeparation`
(4), `Descriptive_GoalwardRunner_MarkerStateAndContact` (4) and `Descriptive_InterceptVersusMove_NearTheMarkPoint` (2).
Each is `[Explicit]` with a reason stating it records numbers and asserts no outcome. Unity's batch runner, filtered by
assembly name, executed them; the Linux gate did not. A "passed" result for one of them means only that the rig ran
to completion without an exception. It is not evidence that the behaviours they record — including the open
default-flip blockers W13-F5 to F8 — are acceptable. W13's behavioural acceptance coverage on this host is the 41
asserted marking cases plus the other fixtures above.

The skips are env-gated diagnostics (including the identity probe) and existing deferrals. The inconclusive case
is the existing `ATackleFoulIsGivenAsASlideTackleAndNotJudgedTwice` (`connected > 30` unmet), inconclusive in every
earlier host run and on Linux; it makes Unity's process exit code 2 while the XML records 0 failed.

The Linux-equivalent gate on the same source passed (MatchEngine 982 passed / 0 failed / 17 skipped; whole tree
4,620 / 0 / 226) without executing the 18 descriptive cases. Excluding them, Unity's MatchEngine count is 981 passed.
A one-case passed-count difference between the runners also appeared in the W8 C receipt (940 vs 939); it was not
traced here. Neither run has a failure.

**One earlier attempt is not adopted.** It used the same commit but materialized Git LFS only under `Assets/**`, so
`src/deterministic-sim/native/td_mxcsr.dll` was an LFS pointer. Unity could not load it, logged an error from
`MatchEngine..ctor` → `MxcsrValidator`, and the first test to construct an engine
(`GkRushTriggerTests.ComposedEngine_CommittedRush_MovesTheKeeperOffHisLineAndTerminates(0)`) failed on that unhandled
log. That was a setup fault, not a W13 result: after materializing `src/**`, deleting `Library` and re-running both
steps, the result above has no failure. Future host worktrees must materialize LFS for `src/**` as well as `Assets/**`.

## Method

`Unity.exe -batchmode -quit` imported the fresh worktree, then a second batch process ran
`-runTests -testPlatform EditMode -assemblyNames "TacticalDirector.MatchEngine.Tests;TacticalDirector.DefensiveAI.Tests;TacticalDirector.DecisionTree.Tests"`.
No Editor was attached to the worktree, no filter or exclusion was applied, and no project asset, scene or setting
changed. The full Editor logs are kept with licensing lines removed and the local user path replaced by
`<home>` / `<worktree>`; nothing else in them is altered.

| File | What it is |
| --- | --- |
| `source-identity.txt` | Commit, parent and tree hashes; native DLL hash; porcelain counts and process exit codes and times; the non-adopted attempt |
| `compile.txt` | Excerpt of the import log: Tundra result, warnings, exit, counts |
| `compile-editor-log.txt.xz` | The full redacted import Editor log |
| `editmode-results.xml.xz` | NUnit 3.5 XML for the whole three-assembly EditMode run |
| `editmode-editor-log.txt.xz` | The full redacted test-run Editor log |
| `editmode-summary.txt` | Command, times, per-assembly counts, the W13-relevant fixtures, every non-passed case with its reason, and the longest cases |

## Carry-forward

The receipt applies to any later commit whose `src`, `Packages`, `ProjectSettings` and `Assets` trees equal those in
`source-identity.txt` (D-01a); a documentation-only close-out commit on top of `6491de79` keeps it valid.

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | November 13, 2026 | Initial host evidence for W13 at `6491de79`: clean Unity compile and MatchEngine / DefensiveAI / DecisionTree EditMode with 0 failures. |
| 1.1 | November 13, 2026 | Review corrections: `editmode-results.xml.xz` regenerated from the same run with the redaction placeholders XML-escaped (`&lt;worktree&gt;`, `&lt;home&gt;`, `&lt;user&gt;`), so it parses; the README no longer says the `[Explicit]` descriptive rigs were skipped — Unity executed all 18 and they are separated from the 41 behavioural marking cases. No result changed. |
