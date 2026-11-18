# Pinned-host Unity validation — PR #539 (P7/P8/AR-2) merged with main, `src` tree `967655cc`

**Captured:** November 16, 2026, 15:23–15:39 UTC.\
**Created:** November 16, 2026.\
**Purpose:** The D-01a Unity 6000.4.9f1 compile and affected EditMode run that PR #539 lists as required,
following [`../HOST-REQUEST.md`](../HOST-REQUEST.md).\
**Host:** the owner's Windows 11 machine with Unity 6000.4.9f1 (f7258d6eebbe), Mono. This is the certified
determinism host. The run was executed by Claude Code on that machine at the owner's request.\
**Source:** PR head `34f29b843b0f1ffb67bc5112030a5cc69c7e606a` with `src` replaced by the conflict-free merge
of main `39de4662` (`src` tree `967655cc8ccfff5a2524ddce110bf0a195c5e7ac`). The PR conflicts with main only in
docs and tooling (`source-identity.txt` lists them), so this is the source tree that a merge of main into the PR
produces. `Assets`, `Packages` and `ProjectSettings` are identical on the PR head, main and the merge.\
**Integrity:** `../SHA256SUMS` covers every file in this directory and is verified by
`tools/dotnet-ci/check_evidence_manifests.py`.

## 1. Result

| Check | Result |
| --- | --- |
| Compile (empty `Library`) | `Tundra build success` (33.02 s, 1,150 of 1,203 items), **0 `error CS`**, exit 0, only the three known warnings; 75 assemblies for 75 asmdefs, including `TacticalDirector.MatchClientUnity` with its new `SeasonSave` reference |
| EditMode, six assemblies, unfiltered | 1,227 cases: **1,220 passed / 0 failed** / 7 skipped / 0 inconclusive, 792 s, exit 0 |
| ClientApp.Tests | 276 / 0 / 3 / 0 |
| SeasonSave.Tests | 553 / 0 / 4 / 0 |
| Transfers.Tests | 79 / 0 / 0 / 0 |
| ClubFinances.Tests | 70 / 0 / 0 / 0 |
| MatchClientCore.Tests | 190 / 0 / 0 / 0 |
| UiFramework.Tests | 52 / 0 / 0 / 0 |
| AR-2 M1 finance-restore cases | `ResumedFinances_UseOwnerMigrationOrKeepSavedValues`, all four (empty and populated) **passed** |
| Source porcelain | Identical before the compile and after the EditMode run: only the 34 staged `src` entries of the merge |

The seven skips are the environment-gated diagnostics (movement, J-05 throughput ×2, career drift, engine scoring,
calibration corpus and pilot); `editmode-summary.txt` gives each reason. Every fixture named in the host request
passed, apart from `CareerDriftDiagnosticTests`, whose one case is one of those diagnostics.

The two CS0618 warnings are the existing `GetInstanceID` calls in `ClientShellBehaviour.cs`, recorded at lines
146/154 on earlier host runs and at 172/180 here because PR #539 adds lines above them. They are not new.

Linux CI at `d35bf396` reported ClientApp 275/0/3. Unity reports 276 because `34f29b84` and the main merge add a
case; the other three PR assemblies match their Linux counts.

**Coverage rule.** A later PR #539 commit is covered by this run only if its `src`, `Assets`, `Packages` and
`ProjectSettings` trees equal the hashes in `source-identity.txt`. Resolving the docs/tooling conflicts as merged
here keeps `src` at `967655cc`; any other `src` change supersedes this receipt.

**Not covered:** Play mode, the target-player build, pinned-host timing, the Unity transfer/finance pages (a
separate follow-up), and the independent full-scope review PR #539 still requires. This receipt does not authorize
a merge.

## 2. Method

A detached worktree was created at the PR head with no `Library`, `src` was checked out from the merged tree, and
`Assets/Scripts` was made a directory junction to that worktree's `src`. `Unity.exe -batchmode -quit` imported it;
a second batch process ran `-runTests -testPlatform EditMode` on the six assemblies named in the request. No Editor
was attached to the worktree, no filter or exclusion was applied, and no project asset, scene or setting changed.

The Editor logs are kept with licensing, machine-identifier, player-connection and LAN-address lines removed and the
local paths replaced by `<home>`, `<worktree>` and `<out>`; nothing else in them is altered. The results XML has the
local paths replaced by `[worktree]`, `[out]` and `[home]` and is otherwise as Unity wrote it.

| File | What it is |
| --- | --- |
| `source-identity.txt` | Commits, merge base, merged tree, per-directory tree hashes, conflicted paths, porcelain, exit codes and times |
| `compile.txt` | Excerpt of the import log: compilation requests, Tundra result, warnings, counts |
| `compile-editor-log.txt.xz` | The full redacted import Editor log |
| `editmode-results.xml.xz` | NUnit 3.5 XML for the six-assembly EditMode run |
| `editmode-editor-log.txt.xz` | The full redacted test-run Editor log |
| `editmode-summary.txt` | Command, times, per-assembly counts, named fixtures, the restore cases, every non-passed case with its reason, and the longest cases |

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | November 16, 2026 | Initial host evidence for PR #539 merged with main at `src` tree `967655cc`: clean Unity compile and six-assembly EditMode with 0 failures. |
