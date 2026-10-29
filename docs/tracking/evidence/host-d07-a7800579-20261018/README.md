# Pinned-host Unity validation — PR #515 head `a7800579`

**Captured:** October 18, 2026, about 20:20–22:30 UTC (afternoon of October 18 on the host's Pacific
clock).\
**Purpose:** The governing Unity 6000.4.9f1 compile and EditMode validation required before PR #515
(D-07 option A: outside-area goalkeeper handling, with its review fixes) can merge.\
**Host:** the owner's Windows 11 machine, with the Unity 6000.4.9f1 (f7258d6eebbe) editor (Mono).
This is the certified determinism host.\
**Source:** commit `a7800579337f76ff4fe924d75f25c2cd077e589b`, `src` tree
`c3bb11ffa5ffe11832a2c7a4f74bb0238304df7e`. `Assets/Scripts` is a junction to `src`.
`Packages` and `ProjectSettings` are unchanged from `origin/main`. `source-identity.txt` records the
state before the reimport and the hashes after the last compile.\
**Integrity:** `SHA256SUMS` covers every other file in this directory.

## 1. Result

| Check | Result |
| --- | --- |
| Compile used by the runs (forced reimport) | `Tundra build success`, 0 `error CS`, 0 shim build-output references, only the three known warnings |
| Compile re-check (clean build cache, after the runs) | `Tundra build success` (66.83 s, 948 items), 0 `error CS`, same three warnings |
| D-07, re-handling and W8 B suites; GoalkeeperMechanics | 300 passed / **0 failed** / 27 skipped (existing GoalkeeperMechanics skips) |
| MatchEngine (whole assembly) | 843 results: 826 passed / **0 failed** / 16 skipped / 1 inconclusive (existing) |

All 52 `KeeperOutsideAreaHandlingTests` results pass, including the review-fix cases for a legal catch
with the keeper's body outside the area and for goal-line overhang. All 42 `KeeperRehandlingLawTests`
and all 99 `W8BDistributionTests` results pass. All ten `sim_match_engine_*` full-match scenarios and
both `SaveAndRestoreCarryTheTackleLatches` seeds pass. The inconclusive
`ATackleFoulIsGivenAsASlideTackleAndNotJudgedTwice` is also inconclusive on Linux and in the #511
record. The 16 skips are env-gated diagnostic instruments. `editmode-summary.txt` lists durations.

EventSystem and the other mechanics assemblies were not rerun. This PR changes no code in them;
the `RestartAwardedEvent.cs` change is comments only.

## 2. Discarded first compile

The first forced reimport saw `bin/` and `obj/` build outputs left under `src/` by a local
Linux-shim gate run. Unity treated their DLLs as plugins: 74 same-filename errors, `UnityShim` type
clashes and a failed Tundra build. No test ran on that build. Every generated file under `src/` was
removed (`git clean -fdX src` plus the `bin.meta`/`obj.meta` files Unity had created), the porcelain
listing for `src` became empty, and the reimport was repeated. `compile.txt` keeps counts for the
discarded attempt and the full excerpts for the two good compiles.

## 3. Targeted-run XML

The targeted run's callback was not unregistered when it finished. It therefore also received the
full MatchEngine run's events, and its `results.xml` was overwritten by that run. Its per-test
outcomes survive in its status log, kept as `targeted-run-status.txt.xz` (the run's first 329
lines). The full MatchEngine XML is unaffected; it has its own callback and output directory. Every
MatchEngine case in the targeted run is repeated in the full run.

## 4. Method

Runs were driven through the editor's `unity-mcp` `RunCommand` tool with `TestRunnerApi`. The
targeted batch combined two filters: the three MatchEngine fixtures above and the whole
`TacticalDirector.GoalkeeperMechanics.Tests` assembly. The second batch was the whole
`TacticalDirector.MatchEngine.Tests` assembly. No project asset, scene or setting was modified.

| File | What it is |
| --- | --- |
| `source-identity.txt` | Head and `src` porcelain before the reimport; commit and tree hashes plus empty porcelain listings after the last compile |
| `compile.txt` | Editor.log excerpts: discarded attempt (counts), compile used by the runs, clean-build-cache re-check |
| `editmode-summary.txt` | Batch timings, counts and full-match scenario durations |
| `matchengine-results.xml.xz` | NUnit XML for the whole MatchEngine assembly at `a7800579` |
| `targeted-run-status.txt.xz` | Per-test status log of the targeted batch (§3) |

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | October 18, 2026 | Initial host evidence for PR #515 at `a7800579`. |
