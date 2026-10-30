# Pinned-host Unity compile and EditMode run — PR #520 (J-05) after merging main

**Captured:** October 28, 2026, 17:25–17:36 UTC (10:25–10:36 on the host's Pacific clock)\
**Purpose:** The first two items of PR #520's pinned-host checklist (§"For the Gate-J session"): a Unity compile in
6000.4.9f1 and an EditMode run of `SnapshotDigestBackendTests`, `SpatialHashGridQueryOrderTests` and
`MatchEngineSnapshotDigestReferenceTests`. This covers the "Unity compile" leg of P1a and the Unity allocation leg of E3
for P1b in [`perception-shared-pass-design.md`](../../perception-shared-pass-design.md). The dev-player 10× re-measure
and the FR-PO-052 re-baseline (E5) are not covered here.\
**Host:** the owner's Windows 11 machine, with the Unity 6000.4.9f1 (f7258d6eebbe) editor (Mono). This is the
certified determinism host.\
**Source:** commit `01a5fcdc113591c4cc8f3a584ee23801f0b8a507`, the merge of main into `perf/j05-sim-throughput`.
It contains #519's C3/C4 career shell, so this is also the first Unity build of J-05 together with #519. The `src`
tree is `13f5ff07544c0122548e2b501b49aac640d215a6`. The run used a detached worktree whose `Library` was deleted
first, so the compile is a full import, and an `Assets/Scripts` junction to its own `src`. `source-identity.txt`
records the hashes and an empty `git status --porcelain=v1 --untracked-files=all --ignored -- src Packages
ProjectSettings`, captured **before the compile** and again after the test runs. To check:
`git rev-parse 01a5fcdc:src` must print `13f5ff07…`.\
**Integrity:** `SHA256SUMS` covers every other file in this directory and is verified by
`tools/dotnet-ci/check_evidence_manifests.py`.

| File | What it is |
| --- | --- |
| `source-identity.txt` | Commit and tree hashes, the empty porcelain listings before the compile and after the runs, the Unity version and the junction target. |
| `compile.txt` | Excerpt of the batch-mode Editor log for the full import (`-batchmode -quit`): `Tundra build success` (1,150 of 1,203 items), 0 `error CS`, 0 references to shim build output, 75 `TacticalDirector.*` assemblies for 75 `src` asmdefs, three warnings, exit code 0. |
| `editmode-a-results.xml.xz` | NUnit 3.5 XML, run A: the full `TacticalDirector.DeterministicSim.Tests`, `TacticalDirector.CollisionSystem.Tests` and `TacticalDirector.ClientApp.Tests` assemblies. 309 passed, 0 failed, 12 ignored. |
| `editmode-b-results.xml.xz` | NUnit 3.5 XML, run B: `TacticalDirector.MatchEngine.Tests` filtered to `MatchEngineSnapshotDigestReferenceTests`. 1 passed. |
| `editmode-summary.txt` | Commands, times, per-assembly counts, every test case of the named fixtures, and each ignored test with its reason. |

**Named fixtures.** `SnapshotDigestBackendTests` 4/4, including `CngSha256_OnWindows_IsActive_AndAgreesWithManagedSha256`
(CNG is active under Unity's Mono on Windows) and `Encode_SteadyState_AllocatesNothing`, whose Unity branch asserts
`Is.Not.AllocatingGCMemory()` with Unity's recorder. `SpatialHashGridQueryOrderTests` 2/2.
`MatchEngineSnapshotDigestReferenceTests` 1/1 (900 real ticks, each digest re-derived and chain-linked).

**Ignored, not failed.** All 12 are ignored by attribute: the two `S0SimThroughputDiagnosticTests` benchmarks run only
with `TD_SIM_THROUGHPUT_DIAGNOSTIC=1`, and ten Stage 0+1 placeholders (nine in `CollisionSystemIntegrationTests`, one
in `DeterministicSimSaveLoadTests`) wait for Play Mode or later-stage wiring. NUnit reports the run as
`Skipped:Ignored` because any test is ignored.

**Warnings.** None is in #520's or #519's files: `ball-physics/tests/BallIntegrationTests.cs(330)` CS0219 and
`match-client-unity/ClientShellBehaviour.cs(146, 154)` CS0618, the same three as PR #518's
pinned-host compile at `963f48ee` (its evidence directory `host-963f48ee-20261028/` is on that branch).

**Not covered.** The dev-player 10× re-measure (checklist item 3; part of the Gate-J B8/B10 re-capture), the optional
editor benchmark (item 4), the FR-PO-052 re-baseline (item 5, E5), and the rest of the MatchEngine suite. The full
Editor logs are not committed. The licensing lines they contain, including the host's machine identifier, are left
out of every excerpt here.

The runs were driven from the command line (`Unity.exe -batchmode`). No project asset, scene or setting was modified.

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | October 28, 2026 | Initial evidence set: full-import compile and EditMode runs at PR #520 head `01a5fcdc` (main merged). |
