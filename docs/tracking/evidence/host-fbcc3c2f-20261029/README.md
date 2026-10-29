# Pinned-host Unity compile and EditMode run — PR #518 head with the §13 presenter fix

**Captured:** October 29, 2026, 13:57–14:19 UTC (06:57–07:19 on the host's Pacific clock)\
**Purpose:** Unity evidence for PR #518's §13 review fix (`9139701c`, `S0ScreenPresenter` v1.5 and
`S0ScreenPresentationTests` v1.8). That fix was verified only on the Linux shim; the §14 host runs used `aa858e97`, which
predates it. Under the roadmap's D-01a host-evidence tiers (ratified October 28) a source-affecting head needs a Unity
compile, plus EditMode for the assemblies it changes.\
**Host:** the owner's Windows 11 machine, with the Unity 6000.4.9f1 (f7258d6eebbe) editor (Mono). This is the
certified determinism host.\
**Source:** commit `fbcc3c2f8e3c6f2195e2f2345263eebf91ab6900` (PR #518 head, `client/p5b-host-acceptance`), `src` tree
`3b7981ef054ca6106a6ff4474493113f73c7bb78`. `fbcc3c2f` is docs-only after `9139701c`, so both commits have this `src`
tree. The run used a fresh detached worktree with an empty `Library` and an `Assets/Scripts` junction to its own `src`.
The owner's `SMP-p5b-host` checkout was not used, because its untracked `Assets/HostProbes/` editor scripts would have
been compiled too. `source-identity.txt` records the hashes and an empty
`git status --porcelain=v1 --untracked-files=all --ignored -- src Packages ProjectSettings`, taken before the compile
and again after the run. To check: `git rev-parse fbcc3c2f:src` and `git rev-parse 9139701c:src` must both print
`3b7981ef…`.\
**Integrity:** `SHA256SUMS` covers every other file in this directory and is verified by
`tools/dotnet-ci/check_evidence_manifests.py`.

| File | What it is |
| --- | --- |
| `source-identity.txt` | Commit and tree hashes for the repo, `src`, `Packages`, `ProjectSettings` and `Assets`, the empty porcelain listing, the Unity version and the junction target. |
| `compile.txt` | Excerpt of the batch-mode Editor log for the full import (`-batchmode -quit`, 70 s `InitialRefreshV2`): `Tundra build success` (1,150 of 1,203 items), 0 `error CS`, 0 references to shim build output, 75 `TacticalDirector.*` assemblies for 75 `src` asmdefs, three warnings, exit code 0. |
| `editmode-results.xml.xz` | NUnit 3.5 XML for the EditMode run of `TacticalDirector.ClientApp.Tests`, `TacticalDirector.UiFramework.Tests` and `TacticalDirector.MatchClientCore.Tests`: 424 passed, 0 failed, 0 skipped, 0 inconclusive. |
| `editmode-summary.txt` | Command, times, per-assembly counts (182 / 52 / 190), the §13 regression test's result and the pre-run build line (0 items updated, so the tests ran on the assemblies from `compile.txt`). |

**Result.** ClientApp is 182 against 181 at `963f48ee`; the extra case is the §13 regression test
`ReopeningSubstitutionAfterCancelGreysNothingBeforeAnOutgoingPick`, which passed under Unity's NUnit 3.5. The assembly
set matches the `963f48ee` run so the two are comparable; ClientApp is the only assembly the fix changes.

**Warnings.** The same three as at `963f48ee`, none in the fix's files: `ball-physics/tests/BallIntegrationTests.cs(330)`
CS0219 (unused `radius`) and `match-client-unity/ClientShellBehaviour.cs(146, 154)` CS0618 (`GetInstanceID` is obsolete).

**Harness incident, not a result.** The first compile launch exited after one second with code 127 and wrote no log.
Its `-logFile` pointed into a very long temporary path, which is the likely cause; it was not investigated further. The
retry, with a short log path and the same worktree, is the run recorded here. Licensing handshake errors appear in both
logs and the editor ignored them; they are left out of the excerpts with the host's machine identifier.

**Not covered.** Only these three assemblies were run, not the full EditMode suite. The full Editor logs (55 MB for the
test run) are not committed. Gate J acceptance and J-05 (PR #520) are outside this run.

The runs were driven from the command line (`Unity.exe -batchmode`). No project asset, scene or setting was modified.

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | October 29, 2026 | Initial evidence set: full-import compile and the ClientApp/UiFramework/MatchClientCore EditMode run at PR #518 head `fbcc3c2f`, covering the §13 presenter fix `9139701c`. |
