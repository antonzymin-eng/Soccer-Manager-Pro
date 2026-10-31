# Clean pinned-host EditMode run — shared repo-root resolver and SeasonSave timeouts

**Captured:** October 10, 2026, 03:46–04:22 UTC (evening of October 9 on the host's Pacific clock)\
**Purpose:** Evidence that the six EditMode failures recorded in
[`../host-93de60f6-20261009/`](../host-93de60f6-20261009/README.md) are fixed on the governing build.\
**Host:** the owner's Windows 11 machine, with the Unity 6000.4.9f1 (f7258d6eebbe) editor (Mono).
This is the certified determinism host.\
**Source:** commit `3927a13bde897cc7c0828a1c0c0efffdbbe5e0d1` (PR #494 head), `src` tree
`0124d2b3e1945437d28ec8fdf509913fa79f0fd1`. `Assets/Scripts` is a junction to `src`.
`source-identity.txt` records these hashes immediately before the compile and again after the run.
Both times `git status --porcelain=v1 --untracked-files=all --ignored -- src Packages ProjectSettings`
was empty, so the editor compiled and tested exactly that commit and nothing else. To check:
`git rev-parse 3927a13b:src` must print `0124d2b3…`.\
**Integrity:** `SHA256SUMS` covers every other file in this directory and is verified by
`tools/dotnet-ci/check_evidence_manifests.py`.

| File | What it is |
| --- | --- |
| `source-identity.txt` | Branch, commit, tree hashes for the repo, `src`, `Packages`, `ProjectSettings` and `Assets`, the empty porcelain listings, the Unity version and the junction target, captured before the compile and re-captured after the run. |
| `compile.txt` | Editor.log excerpt from `AssetDatabase.Refresh(ForceUpdate)` plus the forced recursive reimport at that commit: `Tundra build success`, 0 `error CS`, 0 references to shim build output, only the three known warnings. |
| `editmode-results.xml.xz` | NUnit XML for the full run of eight assemblies: 1,064 passed, 0 failed, 3 ignored, 0 inconclusive. |
| `editmode-summary.txt` | Times, per-assembly counts, the ignored opt-in harnesses and the six formerly failing tests. |
| `unpinned-*` | **Superseded.** The first clean run (01:39–02:19 UTC). Its source was an uncommitted working tree described only as "PR #492 head `773ecb67` plus this change", and no source identity was captured, so it cannot be tied to a commit. Kept so the record is complete; results were the same (1,064 / 0 / 3 and a 169/169 quick run). The `unpinned-editmode-summary.txt` cross-check of the .NET 8 shim on the same host also belongs to that unpinned tree. |

The run's NUnit status is `Skipped:Ignored`, which NUnit reports when any test is ignored. The
three ignored tests are opt-in harnesses that run only when an environment variable is set
(`TD_ENGINE_DIAGNOSTIC`, `TD_CALIBRATION_SAMPLES`, `TD_CALIBRATION_PILOT`). No test failed.

**Why the rerun was clean-tree only.** After the unpinned run, a local `dotnet test` left the
shim's `bin/`, `obj/` and `*.gen.csproj` output under `src/`, and Unity imported those DLLs as
plugins at 02:46 UTC, 27 minutes after the unpinned run finished. Before the pinned compile, all
of that output and its `.meta` files were deleted (none is tracked). The empty ignored-file
listing in `source-identity.txt` and the zero `bin/Debug` references in `compile.txt` show that
none was present.

The runs were driven through the editor's `unity-mcp` `RunCommand` tool with `TestRunnerApi`. No
project asset, scene or setting was modified.

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | October 9, 2026 | Initial evidence set for the clean EditMode run after the shared repo-root resolver and SeasonSave timeouts. |
| 1.1 | October 9, 2026 | Review: the 1.0 source was an unpinned working tree. Reran at commit `3927a13b` with source identity captured before and after; the earlier files are kept as `unpinned-*` and superseded. |
