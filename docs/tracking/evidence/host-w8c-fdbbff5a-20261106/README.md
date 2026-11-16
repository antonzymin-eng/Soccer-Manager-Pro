# Pinned-host Unity validation — PR #542 head `fdbbff5a` (W8 C)

**Captured:** November 6, 2026, 21:36–22:08 UTC.\
**Created:** November 6, 2026.\
**Purpose:** The governing Unity 6000.4.9f1 compile and whole-assembly `TacticalDirector.MatchEngine.Tests`
EditMode run required before PR #542 (W8 C, ERR-011-021, snapshot v28) can leave draft; see the host handoff in
[`w8-c-implementation-results.md`](../../w8-c-implementation-results.md#host-handoff).\
**Host:** the owner's Windows 11 machine with Unity 6000.4.9f1 (f7258d6eebbe), Mono. This is the certified
determinism host.\
**Source:** commit `fdbbff5a5d1f9303d59455eec35a07101e2a3fdc`, `src` tree `3a45777801dd32221249c5d935dda87e492ee8d9`.
Fresh detached worktree with no `Library`, so the compile is a full recursive import; `Assets/Scripts` is a directory
junction to that worktree's own `src`. `source-identity.txt` gives the tree hashes and the porcelain counts.\
**Integrity:** `SHA256SUMS` covers every other file in this directory and is verified by
`tools/dotnet-ci/check_evidence_manifests.py`.

## Result

| Check | Result |
| --- | --- |
| Compile (empty `Library`) | `Tundra build success` (31.68 s, 1,150 of 1,203 items), **0 `error CS`**, exit 0, only the three known warnings, none in W8 C files; 75 assemblies for 75 asmdefs |
| `TacticalDirector.MatchEngine.Tests`, EditMode, unfiltered | 956 results: **939 passed / 0 failed** / 16 skipped / 1 inconclusive, 1,792 s |
| `W8CHandControlTests` | **99 / 99 passed**, including the 16 exact-480 CONTACT save/restore cases |
| W8 B, re-handling, outside-handling, clock, baseline and snapshot fixtures | All passed (`editmode-summary.txt` lists each) |
| Source porcelain (`src`, `Packages`, `ProjectSettings`) | 0 lines before the compile, after the compile and after the test run |

The 16 skips are the env-gated diagnostic instruments. The inconclusive case is the existing
`ATackleFoulIsGivenAsASlideTackleAndNotJudgedTwice`, whose `connected > 30` assumption is unmet on this corpus; it is
also inconclusive on Linux and in every earlier host run. Unity's batch runner treats that inconclusive result as a
failure for its process exit code (2) while the XML records 0 failed.

This matches the fresh Linux run recorded for the same source (940 passed / 0 failed / 16 skipped / 1 inconclusive).
The one-case difference in the passed count is a counting difference between the two runners, not a different
outcome: neither run has a failure.

## Method

`Unity.exe -batchmode -quit` imported the fresh worktree, then a second batch process ran
`-runTests -testPlatform EditMode -assemblyNames TacticalDirector.MatchEngine.Tests`. No Editor was attached to the
worktree, no filter or exclusion was applied, and no project asset, scene or setting changed. The full Editor logs
are kept with licensing, machine-identifier, player-connection and LAN-address lines removed and the local user path
replaced by `<home>` / `<worktree>`; nothing else in them is altered.

| File | What it is |
| --- | --- |
| `source-identity.txt` | Commit, parent and tree hashes; porcelain counts before and after each process; process exit codes and times |
| `compile.txt` | Excerpt of the import log: compilation requests, Tundra result, warnings, counts |
| `compile-editor-log.txt.xz` | The full redacted import Editor log |
| `matchengine-results.xml.xz` | NUnit 3.5 XML for the whole MatchEngine EditMode run |
| `editmode-editor-log.txt.xz` | The full redacted test-run Editor log |
| `editmode-summary.txt` | Command, times, counts, the W8-relevant fixtures, every non-passed case with its reason, and the longest cases |

## Version History

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | November 6, 2026 | Initial host evidence for PR #542 at `fdbbff5a`: clean Unity compile and whole MatchEngine EditMode with 0 failures. |
