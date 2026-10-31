# Pinned-host Unity compile and EditMode run — PR #521 head after merging main

**Captured:** October 30, 2026, 21:55–22:11 UTC (14:55–15:11 on the host's Pacific clock)\
**Purpose:** Unity evidence for PR #521 (season-save career lane: #31 Transfers T1/T2, #28 season-boundary regen)
at its final head `5849b562`, the merge of `main` (`c51d3cbd`, after PRs #522, #525, #513 and #524) into the PR. The
PR body listed this as outstanding: the new `InternalsVisibleTo` grants, four changed `.asmdef` files and the
IL-scan test that uses Mono `Module.ResolveMember` had been verified only on the Linux shim. Under the roadmap's D-01a
host-evidence tiers (ratified October 28) a source-affecting head needs a Unity compile, plus EditMode for the
assemblies it changes.\
**Host:** the owner's Windows 11 machine, with the Unity 6000.4.9f1 (f7258d6eebbe) editor (Mono). This is the
certified determinism host.\
**Source:** commit `5849b5620356791da087ebf21a94c4dc191fdbb5` (PR #521 head, `claude/career-season-save-086ae3`),
`src` tree `fbf58f863f8396c809a84e5b54d824357333b5c9`. The run used a fresh detached worktree with an empty `Library`
and an `Assets/Scripts` junction to its own `src`. The owner's open editor on the main checkout was not used.
`source-identity.txt` records the hashes and an empty
`git status --porcelain=v1 --untracked-files=all --ignored -- src Packages ProjectSettings`, taken before the compile,
after it and after the EditMode run. To check: `git rev-parse 5849b562:src` must print `fbf58f86…`.\
**Integrity:** `SHA256SUMS` covers every other file in this directory and is verified by
`tools/dotnet-ci/check_evidence_manifests.py`.

| File | What it is |
| --- | --- |
| `source-identity.txt` | Commit, parent and tree hashes for the repo, `src`, `Packages`, `ProjectSettings` and `Assets`, the empty porcelain listings, the Unity version and the junction target. |
| `compile.txt` | Excerpt of the batch-mode Editor log for the full import (`-batchmode -quit`, 85 s `InitialRefreshV2`): `Tundra build success` (1,150 of 1,203 items), 0 `error CS`, 0 references to shim build output, 75 `TacticalDirector.*` assemblies for 75 `src` asmdefs. |
| `editmode-results.xml.xz` | NUnit 3.5 XML for the EditMode run of the seven test assemblies of the assemblies the PR changes: 1,291 passed, 0 failed, 6 skipped, 0 inconclusive. |
| `editmode-summary.txt` | Command, times, per-assembly counts, the IL-scan test's result, the six skipped cases with their reasons, and the pre-run build line (0 items updated, so the tests ran on the assemblies from `compile.txt`). |

**Assemblies run.** The PR changes `src/` under `client-app`, `deterministic-sim`, `discipline`, `player-database`,
`player-progression`, `season-save` and `transfers`, so the run covers their seven test assemblies:

| Assembly | Total | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: | ---: |
| `TacticalDirector.ClientApp.Tests` | 194 | 192 | 0 | 2 |
| `TacticalDirector.DeterministicSim.Tests` | 91 | 90 | 0 | 1 |
| `TacticalDirector.Discipline.Tests` | 146 | 146 | 0 | 0 |
| `TacticalDirector.PlayerDatabase.Tests` | 50 | 50 | 0 | 0 |
| `TacticalDirector.PlayerProgression.Tests` | 193 | 193 | 0 | 0 |
| `TacticalDirector.SeasonSave.Tests` | 554 | 551 | 0 | 3 |
| `TacticalDirector.Transfers.Tests` | 69 | 69 | 0 | 0 |
| **All** | **1,297** | **1,291** | **0** | **6** |

**Result.** The compile is clean and every case that runs passes under Unity's NUnit 3.5. The IL-scan test
`TransfersT0Tests.T0StateAndCommands_ContainNoFloatOrDoubleFields`, which resolves members through Mono's
`Module.ResolveMember`, passed. The six skips are the existing opt-in cases: the two J-05 throughput benchmarks
(`TD_SIM_THROUGHPUT_DIAGNOSTIC`), the ERR-030-014 engine scoring diagnostic (`TD_ENGINE_DIAGNOSTIC`), the two A4a/KD-8
calibration drivers (`TD_CALIBRATION_SAMPLES`, `TD_CALIBRATION_PILOT`) and the Stage 1 mid-tick save case.

**Warnings.** The same three as at `05898cd0`, none in this PR's files: `ball-physics/tests/BallIntegrationTests.cs(330)`
CS0219 (unused `radius`) and `match-client-unity/ClientShellBehaviour.cs(146, 154)` CS0618 (`GetInstanceID` is
obsolete).

**Not covered.** Whole-assembly MatchEngine EditMode was not run: the PR changes no engine behaviour or engine
snapshot layout and opens or closes no engine-evidence window (D-01a item 3). Only the seven assemblies above were
run, not the full EditMode suite. The Linux functional gate result for `5849b562` is recorded by CI, not here. The full
Editor logs (92 MB for the test run) are not committed. Licensing handshake lines and the host's machine identifier
are left out of the excerpts.

The runs were driven from the command line (`Unity.exe -batchmode`). No project asset, scene or setting was modified.

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | October 30, 2026 | Initial evidence set: full-import compile and the seven-assembly EditMode run at PR #521 head `5849b562`. |
