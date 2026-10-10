# Main 93de60f6 — pinned-host compile and EditMode evidence

**Captured:** October 9, 2026 (03:51–04:46 UTC, second run 05:10 UTC; evening of October 8 on the host's Pacific clock)\
**Purpose:** Raw evidence for the pinned Unity compile and EditMode run that PRs #487 (#49 L2
localization), #489 (P5b screens) and #491 (Club Finances T3b1) merged without.\
**Host:** the owner's Windows 11 machine, with the Unity 6000.4.9f1 editor (Mono). This is the
certified determinism host.\
**Source:** main `93de60f629dd4d948debb96aa594b53f8fdeefd0`, clean, with `Assets/Scripts` as a
junction to `src`, plus the Editor-regenerated `Packages/packages-lock.json` that lands with this
evidence (same packages and versions; `com.unity.modules.ui` depth 2 → 1, `com.unity.ugui` entry
reordered).\
**Integrity:** `SHA256SUMS` covers every other file in this directory and is verified by
`tools/dotnet-ci/check_evidence_manifests.py`.

| File | What it is |
| --- | --- |
| `compile-93de60f6.txt` | Editor.log excerpt. Round 1 is **void**: the editor was open when #489 added `com.unity.ugui` to `Packages/manifest.json` and had not re-resolved packages, so `match-client-unity` reported 378 `error CS` lines for `UnityEngine.UI` / `UnityEngine.EventSystems`. After `PackageManager.Client.Resolve()` registered `com.unity.ugui@2.0.0`, round 2's forced recursive reimport built with 0 `error CS` and only the three known warnings. |
| `editmode-results.xml.xz` | Unity Test Runner NUnit XML for Localization, ClubFinances, SeasonSave, ClientApp and MatchClientCore. 918 passed, 4 failed, 3 skipped, 0 inconclusive. |
| `editmode-summary.txt` | The run's start and finish times, its failures and per-assembly counts. |
| `editmode2-results.xml.xz` | Second EditMode run (05:10 UTC), added after Codex review on PR #492. It covers MatchAnalytics and UiFramework, the analytics/framework regressions that the screens procedure's step 2 requires and the first run omitted. 107 passed, 2 failed. Run at PR head `5cc06ed5`, whose `src/` is identical to `93de60f6`. |
| `editmode2-summary.txt` | The second run's times, failures and per-assembly counts. |

**Overall status: both runs are `Failed`.** The suites P5b, L2 and T3b1 own are green. Six tests
fail, and under the screens record's own rule a failed EditMode run leaves main red on the
governing build until follow-ups fix them. These files therefore record "compile passes; EditMode
red on six tracked, pre-existing failures", not a passing EditMode gate.

## Reading the failures

None is attributable to #487, #489 or #491. Run 2 adds two:
`MatchAnalyticsValueTypeTests.NoOtherAssemblyReferencesMatchAnalytics` and
`MatchViewObserverNeutralityTests.NoOtherAssemblyReferencesTheUiFramework`. Both are the same
repo-root lock, so all four of those locks now fail at one commit. The first run's four failures
are listed below.

- `ClubFinancesCritiqueTests.AssemblyReferences_AreExactlyCurrentPhaseDependencies` and
  `MinimalFinanceSaveShape_HasNoRngCursorOrActionOrdinal`: "could not locate repository root".
  Both are already named in the open repo-root-discovery issue in `docs/tracking/open-issues.md`.
- `SeasonLoopDisciplineTests.ARealEngineFixtureFoldsItsCardsOntoPlayerRecordsAndChangesNothingElse`
  (1,106 s) and `SeasonLoopScenarioTests.sim_season_multi_fixture` (560 s): Unity's default 180 s
  per-test timeout. Both tests predate the three PRs (the only #491 edit to their files is an
  unrelated test and a header line), play real full matches, and had not previously been run in
  Unity. Tracked as their own open issue.

The run was driven through the editor's `unity-mcp` `RunCommand` tool with `TestRunnerApi`. A
`groupNames` regex meant to exclude `S0DemoFixtureComparisonTests` did not take effect, so the
full-match comparison also ran; it passed in 1,534 s. No project asset, scene or setting was
modified other than the regenerated package lock.

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | October 9, 2026 | Initial evidence set for the main `93de60f6` pinned-host compile and EditMode run. |
| 1.1 | October 9, 2026 | Codex review on PR #492: adds the MatchAnalytics/UiFramework run (`editmode2-*`) and states that the overall EditMode status is Failed. |
