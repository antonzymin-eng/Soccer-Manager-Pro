# Clean pinned-host EditMode run — shared repo-root resolver and SeasonSave timeouts

**Captured:** October 10, 2026, 01:39–02:19 UTC (evening of October 9 on the host's Pacific clock)\
**Purpose:** Evidence that the six EditMode failures recorded in
[`../host-93de60f6-20261009/`](../host-93de60f6-20261009/README.md) are fixed on the governing build.\
**Host:** the owner's Windows 11 machine, with the Unity 6000.4.9f1 editor (Mono). This is the
certified determinism host.\
**Source:** branch `claude/unity-clean-editmode`. Its working tree is PR #492 head `773ecb67` plus
the change this evidence lands with. `Assets/Scripts` is a junction to `src`. The source commit is
named in the PR that adds this directory.\
**Integrity:** `SHA256SUMS` covers every other file in this directory and is verified by
`tools/dotnet-ci/check_evidence_manifests.py`.

| File | What it is |
| --- | --- |
| `compile.txt` | Editor.log excerpt from the forced recursive reimport: two `Tundra build success` passes, 0 `error CS`, and only the three known warnings. |
| `quick-results.xml.xz` | NUnit XML for MatchAnalytics, UiFramework, ClubFinances and `RepositoryRootTests`: 169 passed, 0 failed. |
| `editmode-results.xml.xz` | NUnit XML for the full run of eight assemblies: 1,064 passed, 0 failed, 3 ignored, 0 inconclusive. |
| `editmode-summary.txt` | Times, per-assembly counts, the ignored opt-in harnesses, the six formerly failing tests and the shim cross-check. |

The run's NUnit status is `Skipped:Ignored`, which NUnit reports when any test is ignored. The
three ignored tests are opt-in harnesses that run only when an environment variable is set
(`TD_ENGINE_DIAGNOSTIC`, `TD_CALIBRATION_SAMPLES`, `TD_CALIBRATION_PILOT`). No test failed.

The runs were driven through the editor's `unity-mcp` `RunCommand` tool with `TestRunnerApi`. No
project asset, scene or setting was modified.

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | October 9, 2026 | Initial evidence set for the clean EditMode run after the shared repo-root resolver and SeasonSave timeouts. |
