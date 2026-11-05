# Pinned-host Unity compile and EditMode run — main at `1d3cebf3`

**Captured:** November 5, 2026, 14:34–14:37 UTC (06:34–06:37 on the host's Pacific clock)\
**Purpose:** Unity evidence for `main` at `1d3cebf3`, the merge of PR #536. Since the last evidenced head, `ea84be27`
(`../host-ea84be27-20261103/`), the only change under `src/`, `Packages/`, `ProjectSettings/` or `Assets/` is
`src/living-world/WorldStore.cs` v1.8. That edit, from PR #533 and carried by #536, rewrites the `WorldSeed` XML doc
comment and changes no code. D-01a has no comment-only exception: item 1 asks for a Unity compile on any head that
changes anything under `src/`, and item 4's carry-forward needs byte-identical trees. Item 2 asks for EditMode over the
test assemblies of the assemblies that changed, which here is `TacticalDirector.LivingWorld.Tests` alone.\
**Host:** the owner's Windows 11 machine, with the Unity 6000.4.9f1 (f7258d6eebbe) editor (Mono).\
**Source:** commit `1d3cebf3b8d874715158067d9deb5fa48b309a30` (`main`), `src` tree
`65c8a8bfc533ac354a9a652a79cf6fb7cc94b2c8`. The run used a fresh detached worktree with an empty `Library` and an
`Assets/Scripts` junction to its own `src`. The owner's open editor on the main checkout was not used.
`source-identity.txt` records the hashes and an empty
`git status --porcelain=v1 --untracked-files=all --ignored -- src Packages ProjectSettings`, taken before the compile,
after it and after the EditMode run. To check: `git rev-parse 1d3cebf3:src` must print `65c8a8bf…`.\
**Integrity:** `SHA256SUMS` covers every other file in this directory and is verified by
`tools/dotnet-ci/check_evidence_manifests.py`.

| File | What it is |
| --- | --- |
| `source-identity.txt` | Commit, parent and tree hashes for the repo, `src`, `Packages`, `ProjectSettings` and `Assets`, the one-file difference from `ea84be27`, the empty porcelain counts, the Unity version and the junction target. |
| `compile.txt` | Excerpt of the batch-mode Editor log for the full import (`-batchmode -quit`, 122 s `InitialRefreshV2`): `Tundra build success` (1,150 of 1,203 items), 0 `error CS`, 0 references to shim build output, 75 `TacticalDirector.*` assemblies for 75 `src` asmdefs. |
| `editmode-results.xml.xz` | NUnit 3.5 XML for the EditMode run of `TacticalDirector.LivingWorld.Tests`: 129 passed, 0 failed, 0 skipped. |
| `editmode-summary.txt` | Command, times, per-assembly and per-fixture counts, and the pre-run build line (0 items updated, so the tests ran on the assemblies from `compile.txt`). |

| Run | Scope | Total | Passed | Failed | Skipped |
| --- | --- | ---: | ---: | ---: | ---: |
| — | `TacticalDirector.LivingWorld.Tests` (9 fixtures, including `WorldStoreTests` 22/22) | 129 | 129 | 0 | 0 |

**Result.** The compile is clean and every living-world case passes under Unity's NUnit 3.5. With `ea84be27`'s set,
this leaves `main` at `1d3cebf3` owing no host run under D-01a.

**Warnings.** The same three as at `ea84be27`, none in `living-world`: `ball-physics/tests/BallIntegrationTests.cs(330)`
CS0219 (unused `radius`) and `match-client-unity/ClientShellBehaviour.cs(146, 154)` CS0618 (`GetInstanceID` is
obsolete).

**Not covered.** Whole-assembly MatchEngine EditMode was not run: the change is outside the engine and changes no
behaviour or snapshot layout (D-01a item 3). Also not run: the full EditMode suite across all assemblies (the
once-per-host-cycle check on `main`), the six-seed corpus and `bash tools/run-tests-local.sh --pr`. The full Editor
logs are not committed. Licensing handshake lines and the host's machine identifier are left out of the excerpt. The
XML keeps the host's local paths, as earlier evidence sets do.

The runs were driven from the command line (`Unity.exe -batchmode`). No project asset, scene or setting was modified.

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | November 5, 2026 | Initial evidence set: full-import compile and the living-world EditMode run at `main` `1d3cebf3`. |
