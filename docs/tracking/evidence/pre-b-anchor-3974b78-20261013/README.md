# Pre-B engine anchor freeze at `3974b78` (roadmap step 6)

**Created:** October 13, 2026
**Version:** 1.0
**Purpose:** Freeze the latest pre-B engine anchor that W8 B (roadmap step 7) is compared against:
frozen seeds, outcome counts and terminal fingerprints, plus the long-`OutOfPlay`-stall check that
ERR-001-007 made a condition of any new baseline capture. Non-certifying Linux .NET evidence, the same
platform class as every earlier W8 Stage A baseline.
**Integrity:** `SHA256SUMS` covers every other file in this directory and is verified by
`tools/dotnet-ci/check_evidence_manifests.py`.

## Anchor

| Item | Value |
|---|---|
| Anchor commit | `3974b78b0383adca3e9546bb47a7b93a62b3cf92` (`main` after PR #500, ERR-001-007) |
| Anchor `src` tree | `71d6ce456f563d6070aa8953561c1367599a359f`, the tree compiled and tested in Unity 6000.4.9f1 on the pinned host at PR #500 head `c1ad304` |
| Snapshot schema | v24 |
| Population | `w8-stage-a-preregistration.md`: seeds `0x0F1E2D3C4B5A6978`, `0x00000000D1A6D05E`, `0x0000000000000001`, `0x00000000ABCDEF12`, `0x0000000099887766`, `0x000000005A5A5A5A`; `new MatchEngine(seed)`, `EnableGkHeading()`, no `ConfigureSquads`, 324,000 ticks each |
| W8 instrument | blob `520ba31c71e06ed408fd5c282b5fd5611cf74879`, the version that produced the ERR-011-018 comparison corpus (`W8StageADiagnosticTests-520ba31c.cs.txt`; from `measure/w8-clock-correction-six-seed-20260926` at `3aea8a4`). `main`'s own blob `7348650e…` lacks its 9-line same/opponent-keeper reclaim split, so it was substituted in a scratch worktree only |
| Foul instrument | `FoulRateDiagnosticTests.cs` unchanged on `main` (`3bdc0217…`) |
| Host | Ubuntu 24.04.5 container, .NET SDK 8.0.131 / runtime 8.0.31 (Ubuntu package), Debug |

`provenance.txt` records each run's worktree commit, `src` tree, env, filter and wall-clock times.
`run-lane-equivalent.sh.txt` is the exact runner: the `measure.yml` lane's `dotnet test` invocation
(`TreatNoTestsAsError`, detailed console and TRX loggers) without its preceding whole-tree gate.
`tools/dotnet-ci/measurement_catalog.py verify-run` passes on all three instrument runs.

## Host-agreement check

The same instrument on `692db66` (PR #465 merge, ERR-011-018) reproduces all six ERR-011-018 terminal
digests recorded in `w8-stage-a-baseline-results.md` byte for byte (`check-w8-*`). This host and the
earlier hosts therefore agree on this corpus.

## Result

**The anchor's W8 report is identical to the ERR-011-018 report**, every episode row, transition,
counter and discipline line, except the six terminal digests and the elapsed-time line. All 55
aggregated counters match, including every figure of the ERR-011-018 table: 135 hand claims;
135 hand-origin passes reaching CONTACT; 24 interrupted by a later hand claim (24 same keeper /
0 opponent); 0 old 360-frame drops; #11 `Recovering → Set` 344, `Set → Anticipate` 2,064,
`Anticipate → Diving` 28, `Anticipate → Rushing` 204; restarts KickOff 64 / FreeKick 88 / GoalKick 20 /
ThrowIn 70 / Corner 1; fouls / yellows / straight reds 49 / 7 / 2.

The digests change because the anchor carries snapshot schema v24 (the dormant W8 B substrate)
where `692db66` carried v23; digests are fingerprints, not cross-schema equality tests. What the
identical report shows is narrower than "same matches": everything this instrument observes is
unchanged on these six seeds by the engine landings since ERR-011-018, ERR-001-007 included. It does
not prove that unobserved state is identical.

The foul instrument (`anchor-foul-*`) agrees: 49 fouls / 7 yellows / 2 straight reds over 1,943,994
played ticks, 8.17 fouls and 1.17 yellows per 90 minutes.

| Seed | Anchor terminal digest (schema v24) |
|---|---|
| `0x0F1E2D3C4B5A6978` | `0d019bbf34ca29ad05385b3289ff15140c3de386140785d1a1d81c65697917b5` |
| `0x00000000D1A6D05E` | `4c19d44c7761152ddfdbbeb80753f8019f1df2d8b43b4aa29d781d16ec2ab643` |
| `0x0000000000000001` | `2517376ed7ee21dbaa304786b2d5cbf80812a6e54e46e51978fef1ebcd2f9387` |
| `0x00000000ABCDEF12` | `e2537ed15f7590446e7f50b3dbd564e870b3a25a6a06f935b2b62d85de999bc3` |
| `0x0000000099887766` | `dd92348f001dac5ac8c7b620f733b5ea667fdfbdc0e289a7f0189aba5f814b11` |
| `0x000000005A5A5A5A` | `21226b56c4e9b005c43791781a4e57db2b7682b7976bbe0e0b1117c50d0e4954` |

## Stall check

A scratch test (`w8-corpus-stall-trace.cs.txt`, not committed to `src/`) plays the same six matches
with no observer and reads `BallView` after **every** tick. Its terminal digests equal the anchor
digests above, so it observed the same matches. Result (`w8-corpus-stall-trace.csv`): **no tick ends
with the ball `OutOfPlay` on any seed, in either half**; zero runs of any length, so zero candidates
at the 600-tick (10 s) threshold. The longest stretch with the ball at a bitwise-identical position,
in any state, is 420 ticks (7.0 s, `Stationary`, seed `0x0F1E2D3C4B5A6978` from tick 45,120); the
other seeds' maxima are 388–406 ticks. No long stall of any kind is present.

This check covers the W8 corpus only. Four of its six seeds were never in the PR #500 stall audit,
whose seeds and squad set-up differ.

## Limits

- Linux .NET only. Unity and .NET diverge from tick 5 on the same seed (PR #499), so this anchor is
  comparable only with W8 B measured on the same lane. No Unity capture of this corpus exists.
- Local, not a CI artifact. A measurement-lane run from a measurement branch carrying the `520ba31c`
  instrument would add an independent artifact; it was not taken.
- Six seeds support per-claim comparison, not match-level outcome claims
  (`w8-goalkeeper-distribution-decision.md` §5).

## Files

| File | Content |
|---|---|
| `anchor-w8-output.txt.xz`, `anchor-w8.trx.xz` | W8 instrument at the anchor: full console output and TRX |
| `anchor-foul-output.txt.xz`, `anchor-foul.trx.xz` | Foul instrument at the anchor |
| `anchor-trace-output.txt.xz`, `anchor-trace.trx.xz` | Per-tick stall trace run |
| `check-w8-output.txt.xz`, `check-w8.trx.xz` | W8 instrument at `692db66` (host-agreement check) |
| `w8-corpus-stall-trace.csv` | Stall-trace result: per-seed counts, longest runs and terminal digests |
| `w8-corpus-stall-trace.cs.txt` | Source of the scratch stall-trace test |
| `W8StageADiagnosticTests-520ba31c.cs.txt` | The frozen W8 instrument source used for both W8 runs |
| `run-lane-equivalent.sh.txt` | Runner script |
| `provenance.txt` | Per-run commit, tree, env, filter, timing; host; instrument identities |

## Reproduction

1. Check out `3974b78` in a scratch worktree; replace `src/match-engine/tests/W8StageADiagnosticTests.cs`
   with `W8StageADiagnosticTests-520ba31c.cs.txt` (`git hash-object` must print `520ba31c…`) and, for the
   stall check, add `w8-corpus-stall-trace.cs.txt` as a `.cs` file in the same folder.
2. `python3 tools/dotnet-ci/generate_projects.py`, then build
   `src/match-engine/tests/match-engine-tests.gen.csproj` (Debug).
3. Run `run-lane-equivalent.sh.txt` with `TD_W8_STAGE_A=1`, `TD_FOUL_DIAGNOSTIC=1` and
   `TD_W8_STALL_TRACE_OUT=<file>` and the filters listed in `provenance.txt`.

## Revision history

| Version | Date | Change |
|---|---|---|
| 1.0 | October 13, 2026 | Initial capture: anchor `3974b78`, host-agreement check at `692db66`, per-tick stall trace. |
