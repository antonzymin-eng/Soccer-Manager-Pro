# Pinned-host Unity validation — PR #564 head `e6fe2224` (W13 follow-ups)

**Captured:** November 24, 2026, 02:23–03:24 UTC (November 23 local time).\
**Created:** November 24, 2026.\
**Purpose:** Run the clean Unity 6000.4.9f1 compile and the full EditMode suite on `codex/w13-follow-ups` after
`e6fe2224` fixed the four compile errors that Unity reported at `a0d6504d`. This closes the pinned-host check recorded
in the `w13-defender-marking` entry of `open-issues.md`.\
**Host:** the owner's Windows 11 machine with Unity 6000.4.9f1 (f7258d6eebbe), Mono. This is the certified
determinism host.\
**Source:** commit `e6fe22242813430dcdaf4c1dcf94a4fe835288cb`, `src` tree
`2544adb0cbdbeec9b7c17804a43d907707f47339`. `Assets/Scripts` is a directory junction to that worktree's own `src`.
`source-identity.txt` gives the tree hashes, times and exit codes.\
**Integrity:** `SHA256SUMS` covers every other file in this directory and is verified by
`tools/dotnet-ci/check_evidence_manifests.py`.

## Result

| Check | Result |
| --- | --- |
| Compile, empty `Library` | `Tundra build success` (30.49 s, 1,150 of 1,203 items), **0 `error CS`**, exit 0, only the three known warnings |
| EditMode, every assembly, no filter | 5,156 results: 4,898 passed / 8 failed / 3 inconclusive / 247 skipped, 3,297 s, exit 2 |
| `TacticalDirector.MatchEngine.Tests` | 1,153: 1,115 passed / **0 failed** / 37 skipped / 1 inconclusive |
| Other assemblies this branch changes | AgentMovement 108/108; DefensiveAI 163 passed / 0 failed / 28 skipped; TacticalInstructions 59/59; MatchClientCore 192/192; MatchClientWeb 64/64; UiFramework 52/52; ClientApp 301 passed / 1 failed / 3 skipped (see below) |
| W13 fixtures | `MatchEngineMarkingTests` 75 passed / 19 skipped (descriptive rigs and the env-gated identity probe); `AgentMovementTrackingTests` 14/14; `ManagerAITests` 32/32; `MatchClientRouterTests` 22/22; `S0ScreenPresentationTests` 26/26; `TeamTacticFileLoaderTests` 9/9; `MarkingFlipMeasurementTests` 2 skipped (both env-gated instruments) |
| Snapshot fixtures (schema v31) | `MatchEngineSnapshotSchemaTests` 30/30, `MatchEngineSnapshotRestoreTests` 20/20, `MatchEngineSnapshotDigestReferenceTests` 1/1 |
| Source porcelain | `git status --short` 0 lines after all runs, back at `e6fe2224` |

**None of the 8 failures is caused by this branch.** On the pinned host, PR #564 at `e6fe2224` compiles cleanly and
has no failure that this branch introduces.

### The ClientApp failure: a harness artifact

`S0ContentContractTests.EveryCompiledBasePatternMatchesTheApprovedContractVerbatim` reported the four new roles
(`context.marking_scheme`, `marking.mixed`, `marking.zonal`, `marking.man_oriented`) as "not in the approved register".
The run used a sparse worktree without `docs/`, so the test's upward search for
`docs/design/ux-s0-binding-contracts.md` reached the owner's main checkout, which held the older v0.10 register. With
`docs/design/` checked out at `e6fe2224` (register v0.11), the class passes 2/2
(`s0-contract-with-docs-results.xml.xz`). An earlier rerun whose sparse-checkout command failed reproduced the
original failure and is not kept.

### The EventSystem failures: they predate this branch

Seven `TacticalDirector.EventSystem.Tests` cases fail: five `CosmeticChannelTests` Tier C cases with
`ERR_EVT_UNREGISTERED_ORDINAL (0x1706): Tier C struct size is 0 for ordinal 0x09`, and
`EventRegistrySchemaTests.AllSeededTierCRows_StructSizesAboveZero` and `IsRegistered_ReturnsTrue_ForFullySeededRows`
(`TickHeartbeatEvent` size 0, so not registered). Under Unity Mono the registry sees `TickHeartbeatEvent` with struct
size 0.

- The same seven fail when the assembly runs alone at `e6fe2224` (`eventsystem-e6fe2224-results.xml.xz`).
- The same seven fail, with the same messages, at `0dce35ee`, main before PR #562
  (`eventsystem-0dce35ee-results.xml.xz`).
- This branch changes no file under `src/event-system`. The non-certifying Linux gate passes these cases.

Earlier host runs used a four-assembly set that excludes EventSystem, so these failures were not seen before. They
are recorded as the open issue `event-system-tier-c-heartbeat-size-unity`.

### Inconclusives

- `MatchEngineTackleTests.ATackleFoulIsGivenAsASlideTackleAndNotJudgedTwice`: the existing inconclusive (29
  connecting challenges against the `> 30` floor; earlier host runs had 26).
- `BodyMechanicsTests.BM007_PoorMechanicsHighPower_StumbleTriggered` (ShotMechanics): could not produce a score below
  the stumble threshold. This branch does not touch ShotMechanics.
- `EventBusWiringSmokeTests.Boot_PublishOnePerSpec_DrainTick_SerializeLedger_DigestStable`: its golden digest is not
  pinned yet, by design.

## Main does not compile in Unity before this PR

On the same worktree and `Library`, main at `2f513fbc` (PR #562 merged) fails to compile:
`MarkingFlipMeasurementTests.cs(150,37): error CS0117: 'Convert' does not contain a definition for 'ToHexString'`
(`compile.txt`, `main-2f513fbc-editor-log.txt.xz`). `e6fe2224` replaces that call, so merging PR #564 restores the
Unity build of main.

## Method

`Unity.exe -batchmode -quit -nographics` compiled the worktree after its `Library` was deleted. A second batch process
ran `-runTests -testPlatform EditMode` with no `-assemblyNames` and no `-testFilter`. Four short side runs followed
(listed in `source-identity.txt`): EventSystem alone at `e6fe2224`; main at `2f513fbc`, which stopped at
compilation; EventSystem alone at `0dce35ee`; and `S0ContentContractTests` at `e6fe2224` with `docs/design/`. No Editor
was attached to the worktree, diagnostic switches stayed at their defaults, and no project asset, scene or setting
changed.

**Deviation:** the worktree was a sparse checkout of `/Assets/ /Packages/ /ProjectSettings/ /src/`, without `docs/`.
Only `S0ContentContractTests` reads from `docs/`, and that case was rerun with `docs/design/` present.

The Editor logs are kept with licensing, machine-identifier and player-connection/LAN-address lines removed and local
paths replaced by `<home>` / `<worktree>` / `<log-dir>`. The NUnit XML files have the same path replacement, written
as `&lt;worktree&gt;` so that they stay well-formed; nothing else in them is altered.

| File | What it is |
| --- | --- |
| `source-identity.txt` | Commit, parent and tree hashes; worktree layout; times and exit codes of every run |
| `compile.txt` | Excerpt of the compile log, plus the `2f513fbc` compile failure |
| `compile-editor-log.txt.xz` | The full redacted compile Editor log |
| `editmode-results.xml.xz` | NUnit 3.5 XML for the full EditMode run |
| `editmode-editor-log.txt.xz` | The full redacted test-run Editor log |
| `editmode-summary.txt` | Per-assembly counts, every non-passed case with its message, the W13 and snapshot fixtures, the longest cases, and the side runs |
| `eventsystem-e6fe2224-results.xml.xz` | NUnit XML: EventSystem.Tests alone at `e6fe2224` |
| `eventsystem-0dce35ee-results.xml.xz` | NUnit XML: EventSystem.Tests alone at `0dce35ee` |
| `s0-contract-with-docs-results.xml.xz` | NUnit XML: `S0ContentContractTests` at `e6fe2224` with `docs/design/` (2/2) |
| `main-2f513fbc-editor-log.txt.xz` | The redacted Editor log of the failed `2f513fbc` compile |

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | November 24, 2026 | Initial host evidence for PR #564 at `e6fe2224`: clean Unity compile; full EditMode 4,898 passed / 8 failed / 3 inconclusive / 247 skipped, with no failure caused by the branch; main at `2f513fbc` fails to compile. |
