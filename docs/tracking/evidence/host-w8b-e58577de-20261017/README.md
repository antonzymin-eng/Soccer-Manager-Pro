# Pinned-host Unity validation — PR #511 head `e58577de`

**Captured:** October 17, 2026, 02:46–14:54 UTC (evening of October 16 into October 17 on the host's
Pacific clock). The prior-head run at `2d6ddf1a` was on October 16, 23:32 UTC to October 17, 01:22 UTC.\
**Purpose:** The governing Unity 6000.4.9f1 compile and EditMode validation required before PR #511
(W8 B goalkeeper distribution plus the own-release re-handling law) can merge.\
**Host:** the owner's Windows 11 machine, with the Unity 6000.4.9f1 (f7258d6eebbe) editor (Mono).
This is the certified determinism host.\
**Source:** commit `e58577de038647a0134e7ceecb151126cf7fa025`, `src` tree
`cae1dd3cc1c6476c59d5344209c2ddefd6884656`. `Assets/Scripts` is a junction to `src`.
`source-identity.txt` records the hashes before the compile and again after the last run. Both times,
`git status --porcelain=v1 --untracked-files=all --ignored -- src Packages ProjectSettings` was empty.\
**Integrity:** `SHA256SUMS` covers every other file in this directory.

## 1. Result

| Check | Result |
| --- | --- |
| Compile (clean build cache) | `Tundra build success` (44.78 s, 948 items), 0 `error CS`, 0 shim build-output references, only the three known warnings |
| GoalkeeperMechanics, PassMechanics, ShotMechanics, CollisionSystem, TacticalInstructions | 388 passed / **0 failed** / 60 skipped / 1 inconclusive (existing BM-007) |
| EventSystem | 47 passed / 7 failed / 10 skipped / 1 inconclusive, **identical test for test to `origin/main` `1963cfbe`** (existing `TickHeartbeatEvent` Tier C registration issue, tracked in `open-issues.md`) |
| MatchEngine | 773 passed / 1 failed / 16 skipped / 1 inconclusive. The one failure is a sleep-interrupted timeout that **passes on rerun** (§2) |

All 99 `W8BDistributionTests` results and all 42 `KeeperRehandlingLawTests` results pass. All ten
`sim_match_engine_*` full-match scenarios and both `SaveAndRestoreCarryTheTackleLatches` seeds pass,
including `sim_match_engine_inposs_gate`, which failed under Unity on October 11.
The inconclusive `ATackleFoulIsGivenAsASlideTackleAndNotJudgedTwice` is also inconclusive on Linux.
The 16 skips are env-gated diagnostic instruments. `editmode-summary.txt` lists durations.

## 2. Sleep interruption

The host slept from 03:33:14Z to 14:03:41Z (Kernel-Power 42/107/1, "Button or Lid") while
`sim_match_engine_keeper_conversion` was running. That case recorded 38,136.5 s and exceeded its
7,200,000 ms `[Timeout]`. No other test was running across the sleep. It was rerun alone at the same
head and passed in 293.3 s (`keeper-conversion-rerun-results.xml.xz`). The rerun is the result for
that case; the timeout is kept in the results file so the record is complete.

## 3. Prior head `2d6ddf1a` (superseded)

The compile result was the same. MatchEngine had 28 `W8BDistributionTests` failures, each failing
`Assert.AreEqual(1, events)`. The cause was a test-only static-initialization-order defect: a
`static readonly` field cached `GetOrdinal<DistributionExecutedEvent>()`, but ordinal 0x16 is an
external row registered at `MatchEngine` boot. Mono ran the initializer before any engine booted, so
the field held 0. CoreCLR runs it lazily, which is why Linux passed.

Run alone in a fresh domain, the fixture gave 71 passed and 28 failed. With only that field made a
lazy property, it gave 99 passed and 0 failed; that probe was then reverted. The `e58577de` fix
resolves both fixtures' external ordinals lazily and asserts that they are nonzero. Production code
did not change between the two heads. The prior-head XML and the probe log are kept with the
`prior-head-2d6ddf1a-` prefix.

## 4. Method

Runs were driven through the editor's `unity-mcp` `RunCommand` tool with `TestRunnerApi`, one batch
per filter. Each batch's callback unregisters itself when it finishes, because `TestRunnerApi`
callbacks are global. An earlier combined attempt at `2d6ddf1a` was discarded: callbacks from
several runs had written to the same files. No project asset, scene or setting was modified.

| File | What it is |
| --- | --- |
| `source-identity.txt` | Commit and tree hashes plus empty porcelain listings, captured before the compile and after the last run |
| `compile.txt` | Editor.log excerpt: compilation requests, Tundra results, warnings |
| `editmode-summary.txt` | Batch timings, counts and full-match scenario durations |
| `eventsystem-results.xml.xz`, `mechanics-results.xml.xz`, `matchengine-results.xml.xz`, `keeper-conversion-rerun-results.xml.xz` | NUnit XML at `e58577de` |
| `baseline-main-1963cfbe-eventsystem-results.xml.xz` | EventSystem at `origin/main`, the attribution baseline |
| `prior-head-2d6ddf1a-*` | Superseded prior-head MatchEngine XML, isolated W8B fixture XML and lazy-ordinal probe log |

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | October 17, 2026 | Initial host evidence for PR #511 at `e58577de`. |
