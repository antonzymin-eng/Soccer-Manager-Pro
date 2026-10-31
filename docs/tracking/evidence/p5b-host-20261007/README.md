# P5b consumed lifecycle/identity — pinned-host validation evidence

**Captured:** October 7, 2026\
**Purpose:** Raw evidence for the pinned-host checklist in
[`p5b-lifecycle-validation.md`](../../p5b-lifecycle-validation.md) v0.5.\
**Host:** the owner's Windows 11 machine, with the Unity 6000.4.9f1 editor (Mono, Release code
optimization). This is the certified determinism host.\
**Source:** main `5d112babb5d05597f392484a2bdcab5545910f82`, checked out cleanly with
`Assets/Scripts` as a junction to `src`. The rerun used `6e949a26`, which is that commit plus
`S0DemoFixtureComparisonTests.cs` v1.3 (`[Timeout(7200000)]`).\
**Integrity:** `SHA256SUMS` covers every other file in this directory and is verified by
`tools/dotnet-ci/check_evidence_manifests.py`.

| File | What it is |
| --- | --- |
| `compile-5d112bab.txt` | Editor.log excerpt from the forced recursive reimport and compile: the Tundra result, a count of `error CS` lines (0), and the unique warnings. |
| `editmode-results.xml.xz` | Unity Test Runner NUnit XML for ClientApp, MatchClientCore, MatchAnalytics and UiFramework. 335 passed and 3 failed. |
| `editmode-summary.txt` | The run's start and finish times, its failures and its counts. |
| `fullmatch-rerun-results.xml.xz` | Unity Test Runner XML for the full-match comparison test alone with `[Timeout]`. It passed in 4,547 s, and the `P5B_FULL_MATCH` lines are in its output. |
| `fullmatch-rerun-summary.txt` | The rerun's start and finish times and its result. |
| `play-session2.txt` | The Play-mode lifecycle probes plus the 1× full-time, report, return and second-Start run. Only one `MatchEngine` was alive at a time. |
| `play-session1-void.txt` | **Void.** Concurrent paced matches in one process broke the process-static `EventBus` (#17 §3.2.1). It is kept only so the record is complete. |

The probes were driven through the editor's `unity-mcp` `RunCommand` tool, using
`EditorApplication.update` state machines. Each probe renderer was a fresh
`MatchClientBehaviour` that copied the scene renderer's serialized wiring with
`EditorUtility.CopySerialized`. No project asset or scene was modified. The session set
`Application.runInBackground` at runtime so that Play mode would keep ticking while the editor
was unfocused, and restored `PlayerSettings.runInBackground = false` afterwards.
`ProjectSettings.asset` is unchanged.

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | October 7, 2026 | Initial evidence set for the P5b pinned-host validation. |
