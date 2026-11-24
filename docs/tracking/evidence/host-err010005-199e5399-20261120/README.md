# Pinned-host Unity validation — PR #559 head `199e5399` (#441 `ERR-010-005`)

**Captured:** November 20, 2026, 20:12–20:48 UTC (12:12–12:48 local).\
**Created:** November 20, 2026.\
**Purpose:** The owner-requested Unity 6000.4.9f1 compile and unfiltered four-assembly EditMode run for PR #559
(header-trigger approach condition and KD-7 latch, `ERR-010-005`), plus a base/head control of the one failure.\
**Host:** the owner's Windows 11 machine with Unity 6000.4.9f1 (f7258d6eebbe), Mono. This is the certified
determinism host.\
**Source:** commit `199e5399dbcb9e8ed26c22ddc0a4655200502c4d`, `src` tree `8faebad0c11b0cedd66ebc67ba16935bd54711be`.
`Assets/Scripts` is a directory junction to that worktree's own `src`. `source-identity.txt` gives the tree hashes
and the porcelain counts.\
**Integrity:** `SHA256SUMS` covers every other file in this directory and is verified by
`tools/dotnet-ci/check_evidence_manifests.py`.

## Result

| Check | Result |
| --- | --- |
| Compile | `Tundra build success` (14.06 s, 402 of 1,203 items; `TacticalDirector.MatchEngine.dll` and `.Tests.dll` recompiled from this tree), **0 `error CS`**, exit 0, only the three known warnings |
| EditMode, four assemblies, unfiltered | 1,538 results: 1,506 passed / **1 failed** / 30 skipped / 1 inconclusive, 1,960 s, exit 2 |
| `TacticalDirector.MatchEngine.Tests` | 1,112: 1,093 passed / **1 failed** / 17 skipped / 1 inconclusive |
| `TacticalDirector.PositioningAI.Tests` | 156: 143 passed / 0 failed / 13 skipped |
| `TacticalDirector.MatchClientCore.Tests` | 192: **192 passed** |
| `TacticalDirector.MatchViewer.Tests` | 78: **78 passed** |
| PR #559 fixtures | `GkHeadingIntentSourceTests` **25/25**, `MatchEngineGkHeadingTests` **38/38**, `MatchEngineGkHeadingScenarioTests` 1/1 |
| Snapshot fixtures | `MatchEngineSnapshotSchemaTests` 28/28, `MatchEngineSnapshotRestoreTests` 19/19, `MatchEngineSnapshotDigestReferenceTests` 1/1 |
| Source porcelain | `git status --short` 0 lines before the compile and after the run; ignored-inclusive `src`/`Packages`/`ProjectSettings` 0 |

### The failure

`HeaderReachabilityDiagnosticTests.HeaderReachabilityObserver_DoesNotChangeSnapshotDigests` fails its own
non-vacuity guard: *no #10 evaluation was observed in the neutrality window; lengthen it* (expected > 0, was 0).
The test runs seed `0x0F1E2D3C4B5A6978` for `NeutralityFrames = 18000` with the #441 observer attached and
requires at least one Pending/Prepared/Failed Heading sample before it compares snapshot digests against an
unobserved engine. It never reached the digest comparison, so this run says nothing either way about observer
neutrality at this head.

| Control (same worktree and Library, `-testFilter` on that one test) | Result |
| --- | --- |
| Pre-fix base `420c0730` (PR #559's merge-base; `src` equals PR #558's head) | **Passed**, exit 0 |
| PR head `199e5399`, test alone | **Failed**, same message, exit 2 |

So on the pinned host the failure is caused by PR #559's change and does not depend on test order. The same test
**passed** on Linux .NET in the PR's own receipt
([`post-fix-match-engine-suite.trx.xz`](../header441-err010005-20261120/README.md)). Under Mono the post-fix
trigger produces no Heading evaluation in this seed's first 18,000 frames; under CoreCLR it produces at least one.
This run does not establish why the two runtimes differ, and does not test a longer window. Commits fell 92% with
the fix (counters §6.8), which makes a thin window plausible.

### Existing non-passes

The 17 MatchEngine skips are the env-gated diagnostic instruments (`editmode-summary.txt` lists them; the PR's
Linux run also skips 17), and the 13 PositioningAI skips are the existing Stage 0+1 deferrals. The inconclusive case is
the existing `ATackleFoulIsGivenAsASlideTackleAndNotJudgedTwice` (`connected > 30` unmet, 26 here); it is
inconclusive on Linux and in every earlier host run.

**This run does not clear PR #559 for merge.** The neutrality test must pass on the pinned host first.

## Method

`Unity.exe -batchmode -quit` compiled the worktree, then a second batch process ran
`-runTests -testPlatform EditMode -assemblyNames
TacticalDirector.MatchEngine.Tests;TacticalDirector.PositioningAI.Tests;TacticalDirector.MatchClientCore.Tests;TacticalDirector.MatchViewer.Tests`,
the assembly set of the PR #543 host run. No Editor was attached to the worktree, no filter or exclusion was applied,
diagnostic flags stayed at their defaults, and no project asset, scene or setting changed.

**Deviation from earlier host runs:** the worktree's `Library` was copied from the owner's main checkout (main at
`52d1f000`) instead of starting empty, to skip the full import. Unity's script compilation pipeline hashes its
inputs, and the compile log shows the changed engine assemblies rebuilt by `Csc` from this tree
(`compile.txt`). The control runs recompiled the same worktree at each checkout.

The Editor logs are kept with licensing, machine-identifier and player-connection/LAN-address lines removed and the
local user path replaced by `<home>` / `<worktree>` / `<log-dir>`; nothing else in them is altered. The NUnit XML
files are byte-identical to the runner's output.

| File | What it is |
| --- | --- |
| `source-identity.txt` | Commit, parent and tree hashes; porcelain counts; process exit codes and times; the control sequence |
| `compile.txt` | Excerpt of the compile log: compilation requests, Tundra result, MatchEngine `Csc` lines, warnings, counts |
| `compile-editor-log.txt.xz` | The full redacted compile Editor log |
| `editmode-results.xml.xz` | NUnit 3.5 XML for the four-assembly EditMode run |
| `editmode-editor-log.txt.xz` | The full redacted test-run Editor log |
| `editmode-summary.txt` | Command, times, per-assembly counts, the PR and snapshot fixtures, every non-passed case with its message, the skips, the longest cases and the control results |
| `control-base-420c0730-results.xml.xz` | NUnit XML: the neutrality test alone at the pre-fix base (passed) |
| `control-head-199e5399-results.xml.xz` | NUnit XML: the neutrality test alone at the PR head (failed) |

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | November 20, 2026 | Initial host evidence for PR #559 at `199e5399`: clean Unity compile; four-assembly EditMode 1 failure (`HeaderReachabilityObserver_DoesNotChangeSnapshotDigests` non-vacuity guard), caused by the PR per base/head control; PR fixtures all pass. |
