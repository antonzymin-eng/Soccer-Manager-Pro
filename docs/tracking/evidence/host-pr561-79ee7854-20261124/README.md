# Pinned-host Unity validation — PR #561 merged with main (`79ee7854`)

**Captured:** November 24, 2026, 19:09–20:07 UTC.\
**Created:** November 24, 2026.\
**Purpose:** Run the Unity 6000.4.9f1 compile and full EditMode suite on PR #561 (#44 T3 quick-sim card synthesis)
after main, with PRs #562, #564 and #563, was merged into it. The PR description recorded this as pending.\
**Host:** the owner's Windows 11 machine with Unity 6000.4.9f1 (f7258d6eebbe), Mono. This is the certified
determinism host.\
**Source:** commit `79ee78548111e05eba53dd21d88a0d4ad21738e4`, `src` tree `36fd908b0a8a61c8b279c6cdb149caa66d0e1d96`.
`Assets/Scripts` is a directory junction to that worktree's own `src`. `source-identity.txt` gives the tree hashes,
times and exit codes. PR #561 merged at `45d32d17` (20:03 UTC, before this run finished). That commit's root tree
`df49d29f87167a31d4b66831e6d1fe1dc272e5ca` is identical to `79ee7854`'s, so this run covers main as merged.\
**Integrity:** `SHA256SUMS` covers every other file in this directory and is verified by
`tools/dotnet-ci/check_evidence_manifests.py`.

## Result

| Check | Result |
| --- | --- |
| Compile, empty `Library` | `Tundra build success` (33.74 s, 1,150 of 1,203 items), **0 `error CS`**, exit 0, only the three known warnings |
| EditMode, every assembly, no filter | 5,177 results: 4,920 passed / 7 failed / 3 inconclusive / 247 skipped, 3,365 s, exit 2 |
| `TacticalDirector.Discipline.Tests` | 151/151 |
| `TacticalDirector.SeasonSave.Tests` | 573: 569 passed / **0 failed** / 4 skipped |
| `TacticalDirector.MatchEngine.Tests` | 1,153: 1,115 passed / **0 failed** / 37 skipped / 1 inconclusive |
| Fixtures this PR touches | `CardLedgerFoldTests` 38/38; `RoundResolutionModelTests` 31/31; `SeasonLoopDisciplineTests` 28/28; `SeasonLoopTests` 31/31 |
| Source porcelain | `git status --short` 0 lines after all runs |

**None of the 7 failures is caused by this PR.** They are the seven EventSystem Tier C cases
(`TickHeartbeatEvent` struct size 0 under Mono, so ordinal 0x09 is unregistered). They also fail on main
(`host-w13fu-e6fe2224-20261124`, `host-pr563-8d240ba4-20261124`), this PR changes no file under `src/event-system`,
and they are tracked in `open-issues.md`.

The three inconclusives are the existing ones: `MatchEngineTackleTests.ATackleFoulIsGivenAsASlideTackleAndNotJudgedTwice`
(24 connecting challenges against the `> 30` floor), `BodyMechanicsTests.BM007_PoorMechanicsHighPower_StumbleTriggered`
and `EventBusWiringSmokeTests.Boot_PublishOnePerSpec_DrainTick_SerializeLedger_DigestStable` (golden digest not
pinned yet, by design).

The non-certifying .NET gate (`bash tools/run-tests-local.sh --pr`) on the same commit passed: 4,929 passed /
0 failed / 229 skipped over 37 assemblies, quarantine and owner-held RED ledger empty.

## Method

`Unity.exe -batchmode -quit -nographics` compiled a new worktree, which had no `Library`. A second batch process ran
`-runTests -testPlatform EditMode` with no `-assemblyNames` and no `-testFilter`. No Editor was attached to the
worktree, diagnostic switches stayed at their defaults, and no project asset, scene or setting changed. The worktree
was a sparse checkout of `/Assets/ /Packages/ /ProjectSettings/ /src/ /docs/design/`.

The Editor logs are kept with licensing, machine-identifier and player-connection/LAN-address lines removed and local
paths replaced by `<home>` / `<worktree>` / `<log-dir>`. The NUnit XML has the same path replacement, written as
`&lt;worktree&gt;` so that it stays well-formed; nothing else in it is altered.

| File | What it is |
| --- | --- |
| `source-identity.txt` | Commit, parents and tree hashes; worktree layout; times and exit codes |
| `compile.txt` | Excerpt of the compile log |
| `compile-editor-log.txt.xz` | The full redacted compile Editor log |
| `editmode-results.xml.xz` | NUnit 3.5 XML for the full EditMode run |
| `editmode-editor-log.txt.xz` | The full redacted test-run Editor log |
| `editmode-summary.txt` | Per-assembly counts, every non-passed case with its message, the fixtures this PR touches and the longest cases |

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | November 24, 2026 | Initial host evidence for PR #561 at `79ee7854`: clean Unity compile; full EditMode 4,920 passed / 7 failed / 3 inconclusive / 247 skipped, with no failure caused by the PR. |
