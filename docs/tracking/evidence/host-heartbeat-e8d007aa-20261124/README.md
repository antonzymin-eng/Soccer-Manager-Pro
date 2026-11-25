# Pinned-host Unity validation — `TickHeartbeatEvent` reserved byte (`e8d007aa`)

**Captured:** November 24, 2026, 23:57 UTC – November 25, 2026, 00:56 UTC.\
**Created:** November 24, 2026.\
**Purpose:** Show that the seven EventSystem failures seen on every full Unity run since October 16, 2026
(open issue `unity-eventsystem-heartbeat`) come from Mono sizing the empty `TickHeartbeatEvent` struct as
0 bytes, and that giving the struct one reserved byte fixes them without any other change in outcome.\
**Host:** the owner's Windows 11 machine with Unity 6000.4.9f1 (f7258d6eebbe), Mono. This is the certified
determinism host.\
**Source:** commit `e8d007aa6921254cdddd4ca5e3b700b77aabdfd4`, `src` tree `45c99fd6d2b12422fcc6d2dac7cdb95d59641922`,
whose parent is main after PR #565. `Assets/Scripts` is a directory junction to that worktree's own `src`.
`source-identity.txt` gives the tree hashes, the run order, times and exit codes.\
**Integrity:** `SHA256SUMS` covers every other file in this directory and is verified by
`tools/dotnet-ci/check_evidence_manifests.py`.

## Result

| Check | Result |
| --- | --- |
| Compile, empty `Library` | `Tundra build success` (29.56 s, 1,150 of 1,203 items), **0 `error CS`**, exit 0, only the three known warnings |
| EventSystem, with the fix | 66: 55 passed / **0 failed** / 1 inconclusive / 10 skipped |
| EventSystem, `TickHeartbeatEvent.cs` restored to main | 66: 47 passed / **8 failed** / 1 inconclusive / 10 skipped |
| EditMode, every assembly, no filter, with the fix | 5,178 results: 4,928 passed / **0 failed** / 3 inconclusive / 247 skipped, 3,373 s |
| Source porcelain | `git status --short` 0 lines after all runs |

Without the fix, the eight failures are the seven known cases plus the new lock
`EventRegistrySchemaTests.TickHeartbeatEvent_StructSizeIsExactlyOne_AndMatchesRegistry`, which reads
"must measure 1 byte on every runtime — Expected: 1, But was: 0". So `EventRegistry.SizeOfStruct<TickHeartbeatEvent>()`,
which is `MemoryMarshal.AsBytes(new T[1]).Length`, returns 0 under Unity's Mono. The registry stored size 0
for ordinal `0x09`, `IsRegistered(0x09)` was false, and each `CosmeticChannel` publish threw
`ERR_EVT_UNREGISTERED_ORDINAL (0x1706): Tier C struct size is 0 for ordinal 0x09`. CoreCLR sizes the same
empty struct at 1, which is why the .NET gate never failed. With the fix all eight pass, and no other
EventSystem case changes outcome between the two arms (`editmode-summary.txt`).

The `EventBusWiringSmokeTests` ledger digest reads `de1e5d82…2faa6` in both arms and in the full run, the same
value as on main (`host-pr561-79ee7854-20261124`). Tier C events are not written to the ledger, so the
reserved byte changes no ledger byte.

This is the first full EditMode run on the pinned host with 0 failures. The three inconclusives are the
existing ones: `MatchEngineTackleTests.ATackleFoulIsGivenAsASlideTackleAndNotJudgedTwice` (24 connecting
challenges against the `> 30` floor), `BodyMechanicsTests.BM007_PoorMechanicsHighPower_StumbleTriggered` and
`EventBusWiringSmokeTests.Boot_PublishOnePerSpec_DrainTick_SerializeLedger_DigestStable` (golden digest not
pinned yet, by design).

The non-certifying .NET gate (`bash tools/run-tests-local.sh --pr`) on the same commit passed: 4,930 passed /
0 failed / 229 skipped over 37 assemblies, quarantine and owner-held RED ledger empty.

## Method

`Unity.exe -batchmode -quit -nographics` compiled a new worktree, which had no `Library`. Three test processes
followed on the same worktree, each `-runTests -testPlatform EditMode`: EventSystem only
(`-assemblyNames TacticalDirector.EventSystem.Tests`) with the fix; the same with only
`src/event-system/TickHeartbeatEvent.cs` checked out from main `212589a7`; and, after checking that file back
out from `e8d007aa`, every assembly with no `-assemblyNames` and no `-testFilter`. Unity recompiled the
changed file before each of the last two runs (60 items each). No Editor was attached to the worktree,
diagnostic switches stayed at their defaults, and no project asset, scene or setting changed. The worktree was
a sparse checkout of `/Assets/ /Packages/ /ProjectSettings/ /src/ /docs/design/`.

The Editor logs are kept with licensing, machine-identifier and player-connection/LAN-address lines removed and local
paths replaced by `<home>` / `<worktree>` / `<log-dir>`. The NUnit XML has the same path replacement, written as
`&lt;worktree&gt;` so that it stays well-formed; nothing else in it is altered.

| File | What it is |
| --- | --- |
| `source-identity.txt` | Commit, parent and tree hashes; worktree layout; run order, times and exit codes |
| `compile.txt` | Excerpt of the compile log |
| `compile-editor-log.txt.xz` | The full redacted compile Editor log |
| `eventsystem-results.xml.xz` | NUnit 3.5 XML, EventSystem only, with the fix |
| `eventsystem-without-fix-results.xml.xz` | NUnit 3.5 XML, EventSystem only, `TickHeartbeatEvent.cs` from main |
| `eventsystem-without-fix-editor-log.txt.xz` | The redacted Editor log of that run, including its recompile |
| `editmode-results.xml.xz` | NUnit 3.5 XML for the full EditMode run with the fix |
| `editmode-editor-log.txt.xz` | The full redacted Editor log of that run |
| `editmode-summary.txt` | Per-assembly counts for all three runs, every non-passed case with its message, and the cases whose outcome differs between the EventSystem arms |

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | November 24, 2026 | Initial host evidence for the `TickHeartbeatEvent` fix at `e8d007aa`: clean Unity compile; EventSystem 0 failed with the fix and 8 failed without it; full EditMode 4,928 passed / 0 failed / 3 inconclusive / 247 skipped. |
