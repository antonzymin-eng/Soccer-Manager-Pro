# Pinned-host Unity compile and EditMode run — PR #518 head with #519 merged

**Captured:** October 28, 2026, 15:26–15:48 UTC (08:26–08:48 on the host's Pacific clock)\
**Purpose:** Merge-readiness evidence for PR #518. Its head `963f48ee` merges main, which brought in #519's C3/C4
career shell, and #519 landed without a Unity compile (its commit says "Unity editor compile not yet run"). This is
the first Unity build of that combined tree.\
**Host:** the owner's Windows 11 machine, with the Unity 6000.4.9f1 (f7258d6eebbe) editor (Mono). This is the
certified determinism host.\
**Source:** commit `963f48ee11a514d9624eafffd7158df6370074ee` (PR #518 head, `client/p5b-host-acceptance`), `src` tree
`911230d4d8c2a85554b00385b7dae663df539f1d`. The run used a fresh detached worktree with an empty `Library` and an
`Assets/Scripts` junction to its own `src`. The owner's `SMP-p5b-host` checkout was not used, because its untracked
`Assets/HostProbes/` editor scripts would have been compiled too. `source-identity.txt` records the hashes and an
empty `git status --porcelain=v1 --untracked-files=all --ignored -- src Packages ProjectSettings`. That listing was
taken after the run, not before the compile. Nothing was edited between `git worktree add` and the end of the run.
To check: `git rev-parse 963f48ee:src` must print `911230d4…`.\
**Integrity:** `SHA256SUMS` covers every other file in this directory and is verified by
`tools/dotnet-ci/check_evidence_manifests.py`.

| File | What it is |
| --- | --- |
| `source-identity.txt` | Commit and tree hashes for the repo, `src`, `Packages`, `ProjectSettings` and `Assets`, the empty porcelain listing, the Unity version and the junction target. |
| `compile.txt` | Excerpt of the batch-mode Editor log for the full import (`-batchmode -quit`, 70 s `InitialRefreshV2`): `Tundra build success` (1,150 of 1,203 items), 0 `error CS`, 0 references to shim build output, 75 `TacticalDirector.*` assemblies for 75 `src` asmdefs, three warnings, exit code 0. |
| `editmode-results.xml.xz` | NUnit 3.5 XML for the EditMode run of `TacticalDirector.ClientApp.Tests`, `TacticalDirector.UiFramework.Tests` and `TacticalDirector.MatchClientCore.Tests`: 423 passed, 0 failed, 0 skipped, 0 inconclusive. |
| `editmode-summary.txt` | Command, times, per-assembly counts (181 / 52 / 190) and the pre-run build line (0 items updated, so the tests ran on the assemblies from `compile.txt`). |

**Warnings.** None is in #519's or #518's files: `ball-physics/tests/BallIntegrationTests.cs(330)` CS0219 (unused
`radius`) and `match-client-unity/ClientShellBehaviour.cs(146, 154)` CS0618 (`GetInstanceID` is obsolete).

**Not covered.** Only the three assemblies both PRs touch were run, not the full EditMode suite. The B8/B10
re-capture, the clean I-Q15 keyboard pass, the owner's wording approval and #520 gate Gate J acceptance, not this
merge, and are outside this run. The full Editor logs (55 MB for the test run) are not committed. The licensing
lines they contain, including the host's machine identifier, are left out of every excerpt here.

The runs were driven from the command line (`Unity.exe -batchmode`). No project asset, scene or setting was modified.

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | October 28, 2026 | Initial evidence set: full-import compile and the ClientApp/UiFramework/MatchClientCore EditMode run at PR #518 head `963f48ee`. |
