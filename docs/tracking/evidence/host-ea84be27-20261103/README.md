# Pinned-host Unity compile and EditMode run — PR #526 final head

**Captured:** November 3, 2026, 14:56–15:14 UTC (06:56–07:14 on the host's Pacific clock)\
**Purpose:** Post-merge Unity evidence for PR #526 (seven-branch consolidation) at its final head `ea84be27`. The PR
merged as `9be79639` with no Unity compile or EditMode run claimed. Roadmap §5.1 lists this as "#526 post-merge host
validation", the first host job, ahead of the pre-C control capture. Under the D-01a host-evidence tiers a
source-affecting head needs a Unity compile, plus EditMode for what it changes.\
**Host:** the owner's Windows 11 machine, with the Unity 6000.4.9f1 (f7258d6eebbe) editor (Mono).\
**Source:** commit `ea84be27bd44d9ee8d593d81027324d408137a91` (PR #526 head, `codex/consolidate-seven-branches`), `src`
tree `78e3bd0c6a605b4ebc2c5c5a11f5647f1bbb6369`. The run used a fresh detached worktree with an empty `Library` and an
`Assets/Scripts` junction to its own `src`. The owner's open editor on the main checkout was not used.
`source-identity.txt` records the hashes and an empty
`git status --porcelain=v1 --untracked-files=all --ignored -- src Packages ProjectSettings`, taken before the compile,
after it and after the EditMode runs. To check: `git rev-parse ea84be27:src` must print `78e3bd0c…`.\
**Commit named by the roadmap:** the roadmap item names `4bcc2ae`; this run used the PR's actual final head, as D-01a
asks. `git diff --quiet ea84be27 4bcc2ae1 -- src Packages ProjectSettings Assets` is empty, and so is the same diff
against merge `9be79639` and `32ff375f`, so the result applies to all of them.\
**Integrity:** `SHA256SUMS` covers every other file in this directory and is verified by
`tools/dotnet-ci/check_evidence_manifests.py`.

| File | What it is |
| --- | --- |
| `source-identity.txt` | Commit, parent and tree hashes for the repo, `src`, `Packages`, `ProjectSettings` and `Assets`, the tree comparisons, the empty porcelain counts, the Unity version and the junction target. |
| `compile.txt` | Excerpt of the batch-mode Editor log for the full import (`-batchmode -quit`, 156 s `InitialRefreshV2`): `Tundra build success` (1,150 of 1,203 items), 0 `error CS`, 0 references to shim build output, 75 `TacticalDirector.*` assemblies for 75 `src` asmdefs. |
| `editmode-a-results.xml.xz` | NUnit 3.5 XML for run A, the two fixtures the roadmap names: 14 passed, 0 failed, 2 skipped. |
| `editmode-b-results.xml.xz` | NUnit 3.5 XML for run B, `MatchEngineSnapshotDigestReferenceTests`: 1 passed. |
| `editmode-c-results.xml.xz` | NUnit 3.5 XML for run C, the whole `TacticalDirector.ClientApp.Tests` assembly: 192 passed, 0 failed, 3 skipped. |
| `editmode-summary.txt` | Commands, times, per-assembly and per-fixture counts, the neutrality test's result, the skipped cases with their reasons, and each pre-run build line (0 items updated, so the tests ran on the assemblies from `compile.txt`). |

**Scope.** Run A is what the roadmap item asks for. Runs B and C go beyond it. Run B checks the `MatchEngine.cs` change
with the digest reference test, as the `host-05898cd0-20261030` set did. Run C follows the literal assembly scope of
D-01a item 2, because the PR adds a fixture to `ClientApp.Tests`.

| Run | Scope | Total | Passed | Failed | Skipped |
| --- | --- | ---: | ---: | ---: | ---: |
| A | `HeaderReachabilityDiagnosticTests` | 15 | 14 | 0 | 1 |
| A | `S0MovementDiagnosticTests` | 1 | 0 | 0 | 1 |
| B | `MatchEngineSnapshotDigestReferenceTests` | 1 | 1 | 0 | 0 |
| C | `TacticalDirector.ClientApp.Tests` (includes the movement case again) | 195 | 192 | 0 | 3 |
| | **Distinct cases** | **211** | **207** | **0** | **4** |

**Result.** The compile is clean and every case that runs passes under Unity's NUnit 3.5. The 18,000-frame
`HeaderReachabilityObserver_DoesNotChangeSnapshotDigests` passed, which is the Unity check that attaching the new
observer leaves every snapshot digest unchanged.

**Skips.** All four are opt-in cases, left off because their environment variables were unset:
`S0MovementDiagnostic_ReportsPlayerTravelAndSpread` (`TD_MOVEMENT_DIAGNOSTIC`),
`HeaderReachabilityDiagnostic_ReportsFrozenSixSeedCounters` (`TD_HEADER441_DIAGNOSTIC`) and the two J-05 throughput
benchmarks (`TD_SIM_THROUGHPUT_DIAGNOSTIC`). The movement case is the only one in `S0MovementDiagnosticTests`, so that
fixture was compiled and discovered under Unity but its measurement did not execute.

**Warnings.** The same three as at `5849b562`, none in this PR's files: `ball-physics/tests/BallIntegrationTests.cs(330)`
CS0219 (unused `radius`) and `match-client-unity/ClientShellBehaviour.cs(146, 154)` CS0618 (`GetInstanceID` is
obsolete).

**Not covered.** Whole-assembly MatchEngine EditMode was not run: the engine change is a null-by-default observation
hook with no behaviour or snapshot-layout change, and it opens or closes no engine-evidence window (D-01a item 3). Also
not run: the full EditMode suite across all assemblies, the six-seed corpus and `bash tools/run-tests-local.sh --pr`.
The full Editor logs (55 MB for run C) are not committed. Licensing handshake lines and the host's machine identifier
are left out of the excerpts. The XML keeps the host's local paths, as earlier evidence sets do.

**Failed launch before this run.** A first launch one minute earlier exited with code 127 at every step: Unity could
not create its log file under an over-long output path, and it never opened the project. No `Library` was created and
no test ran, so nothing from that attempt is in this set. The run recorded here used a shorter output path and changed
nothing else apart from stopping on a failed compile.

The runs were driven from the command line (`Unity.exe -batchmode`). No project asset, scene or setting was modified.

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | November 3, 2026 | Initial evidence set: full-import compile and the three EditMode runs at PR #526 head `ea84be27`. |
