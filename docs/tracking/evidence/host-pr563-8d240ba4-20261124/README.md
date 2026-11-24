# Pinned-host Unity validation — PR #563 merged with main (`8d240ba4`, then `b87ba024`)

**Captured:** November 24, 2026, 16:25–17:54 UTC.\
**Created:** November 24, 2026.\
**Purpose:** Compile PR #563 (ERR-002-001 review amendment) in Unity 6000.4.9f1 and run the full EditMode suite
after main, with PR #564, was merged into it. Then rerun the two realism scenarios at `b87ba024`, where the owner
loosened their bounds under KD-W1.\
**Host:** the owner's Windows 11 machine with Unity 6000.4.9f1 (f7258d6eebbe), Mono. This is the certified
determinism host.\
**Source:** full run at commit `8d240ba47ddd48bbefe592db914616527c57a6b9`, `src` tree
`1e2effb4e1b1d48c156633188995110d67a8df1a`. Side run at `b87ba024874af8543099bad2ce3f7f37133f31cf`, which changes only the two
scenario files. `Assets/Scripts` is a directory junction to that worktree's own `src`. `source-identity.txt` gives
the tree hashes, times and exit codes.\
**Integrity:** `SHA256SUMS` covers every other file in this directory and is verified by
`tools/dotnet-ci/check_evidence_manifests.py`.

## Result

| Check | Result |
| --- | --- |
| Compile at `8d240ba4`, empty `Library` | `Tundra build success` (35.98 s, 1,150 of 1,203 items), **0 `error CS`**, exit 0, only the three known warnings |
| EditMode at `8d240ba4`, every assembly, no filter | 5,158 results: 4,901 passed / 7 failed / 3 inconclusive / 247 skipped, 3,636 s, exit 2 |
| `TacticalDirector.MatchEngine.Tests` | 1,153: 1,115 passed / **0 failed** / 37 skipped / 1 inconclusive |
| `TacticalDirector.AgentMovement.Tests` | 110/110, including both cases of `DeceleratingOntoASteadyPoint_AtItsSpeed_ReentersTheRunningBand` |
| `sim_match_engine_close_chance`, `sim_match_engine_keeper_contact` at `8d240ba4` (old bounds) | both **passed** |
| Same two at `b87ba024` (loosened bounds) | 2 passed / 0 failed, exit 0 (`bounds-results.xml.xz`) |
| Source porcelain | `git status --short` 0 lines after all runs |

**None of the 7 failures is caused by this PR.** They are the seven EventSystem Tier C cases
(`TickHeartbeatEvent` struct size 0 under Mono, so ordinal 0x09 is unregistered). They also fail at `e6fe2224` and at
`0dce35ee` (`host-w13fu-e6fe2224-20261124`), and this PR changes no file under `src/event-system`. They are tracked
in `open-issues.md`.

The run included `docs/design/`, so the `S0ContentContractTests` harness artifact seen in
`host-w13fu-e6fe2224-20261124` did not occur: ClientApp 302 passed / 0 failed / 3 skipped.

### Inconclusives

- `MatchEngineTackleTests.ATackleFoulIsGivenAsASlideTackleAndNotJudgedTwice`: the existing inconclusive (24
  connecting challenges against the `> 30` floor).
- `BodyMechanicsTests.BM007_PoorMechanicsHighPower_StumbleTriggered` (ShotMechanics): could not produce a score below
  the stumble threshold. This PR does not touch ShotMechanics.
- `EventBusWiringSmokeTests.Boot_PublishOnePerSpec_DrainTick_SerializeLedger_DigestStable`: its golden digest is not
  pinned yet, by design.

## The two realism scenarios: Unity and the .NET gate disagree

On the same commit `8d240ba4`, the non-certifying .NET gate (`bash tools/run-tests-local.sh --pr` on this host's
.NET 8 SDK, 37 assemblies, 4,908 passed / 2 failed / 229 skipped) failed both scenarios with the same values
PR #563 recorded before the merge:

| Scenario | .NET gate at `8d240ba4` | Old bound | New bound (`b87ba024`) | Pre-fix world |
| --- | --- | --- | --- | --- |
| `sim_match_engine_close_chance` | meanCosine −0.233; goalwardShare 0.391 | > −0.16; > 0.42 | > −0.26; > 0.37 | ≈ −0.29; ≈ 0.31 |
| `sim_match_engine_keeper_contact` | contacts 11, uncontacted crossings 11 | contacts > crossings | contacts ≥ crossings | 8 vs 15 |

Unity passed both against the old bounds on the same commit. Its run logs no metric values when a scenario passes,
so this receipt does not report Unity's numbers. The two runtimes most likely produce different match
trajectories from the same seeds. Under KD-W1 the owner chose to loosen the bounds. Each new bound still refuses
its pre-fix world. A targeted .NET run of the two scenarios at `b87ba024` passed 2/2.

## Method

`Unity.exe -batchmode -quit -nographics` compiled a new worktree, which had no `Library`. A second batch process ran
`-runTests -testPlatform EditMode` with no `-assemblyNames` and no `-testFilter`. After the bounds commit, the
worktree was moved to `b87ba024` and a third batch process ran the two scenarios alone
(`-assemblyNames TacticalDirector.MatchEngine.Tests -testFilter "sim_match_engine_close_chance|sim_match_engine_keeper_contact"`).
No Editor was attached to the worktree, diagnostic switches stayed at their defaults, and no project asset, scene or
setting changed. The worktree was a sparse checkout of `/Assets/ /Packages/ /ProjectSettings/ /src/ /docs/design/`.

The Editor logs are kept with licensing, machine-identifier and player-connection/LAN-address lines removed and local
paths replaced by `<home>` / `<worktree>` / `<log-dir>`. The NUnit XML files have the same path replacement, written
as `&lt;worktree&gt;` so that they stay well-formed; nothing else in them is altered.

| File | What it is |
| --- | --- |
| `source-identity.txt` | Commits, parents and tree hashes; worktree layout; times and exit codes of every run |
| `compile.txt` | Excerpt of the compile log |
| `compile-editor-log.txt.xz` | The full redacted compile Editor log |
| `editmode-results.xml.xz` | NUnit 3.5 XML for the full EditMode run at `8d240ba4` |
| `editmode-editor-log.txt.xz` | The full redacted test-run Editor log |
| `editmode-summary.txt` | Per-assembly counts, every non-passed case with its message, the fixtures this PR touches, the longest cases, and the side run |
| `bounds-results.xml.xz` | NUnit XML: the two scenarios at `b87ba024` |
| `bounds-editor-log.txt.xz` | The redacted Editor log of that side run |

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | November 24, 2026 | Initial host evidence for PR #563: clean Unity compile at `8d240ba4`; full EditMode 4,901 passed / 7 failed / 3 inconclusive / 247 skipped, with no failure caused by the PR; both realism scenarios pass in Unity at the old and the loosened bounds. |
