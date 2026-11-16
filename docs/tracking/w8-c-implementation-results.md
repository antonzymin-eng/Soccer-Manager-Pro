# W8 C implementation and validation

Created November 6, 2026. Purpose: record the implementation of owner-approved
[#11 §3.8.5](../specs/goalkeeper-mechanics/section-3.md#385-hand-control-clock-and-law-12-offence-w8-c-err-011-021)
and [restart contract §5.3](match-flow-completion-design.md#53-goalkeeper-hand-control-over-eight-seconds--w8-c).
Base: `b6505350`. No `[GT]` changes, new gameplay RNG draws or policy changes.

**Status: implemented; paired Linux comparison passed; fresh full Linux run has zero failures; pinned-host Unity validation passed at `fdbbff5a`.**
Unity 6000.4.9f1 compiled the PR head from an empty `Library` with 0 `error CS`, and the unfiltered
`TacticalDirector.MatchEngine.Tests` EditMode run gave 939 passed / 0 failed / 16 skipped /
1 existing inconclusive ([host receipt](evidence/host-w8c-fdbbff5a-20261106/README.md)). The
implementation and Linux work were authored in a cloud environment without Unity; the host run was
made separately on the owner's machine.

The implementation uses snapshot **v28**. #11 owns a per-slot signed 64-bit
`HandControlStartFrame` with inactive sentinel −1, captured at each successful aerial, dive or smother
claim. Retained reject/cancel paths preserve it; completion, loss, restart and slot reset clear it.
The engine reads it in Resolve after the second outside-handling check and before fouls/executors.
The corner uses the same whistle-finish helper as outside handling. Distribution admission uses the
policy's own configured windup and the inclusive elapsed-480 limit. Both inherited hand deadlines
are removed; the unchanged 360-frame non-law stall guard is feet-only.

The body reader validates the hand-state/clock invariant, possessor and `Controlled` ownership, and
remaining CONTACT budget after decoding all subsystems. It derives keeper identity from restored
on-pitch roles, rather than constructor or pre-overlay slots. Full time skips the two timing checks
while retaining structural validation. Schema mismatch still fails without migration; the match-save
file format stays v3.

| Check | Result |
| --- | --- |
| Pre-change W8 B / outside-handling / re-handling baseline | 193 passed, 0 failed |
| Whole generated .NET solution compile | Passed; the final scoped gate built with 0 warnings / 0 errors. The earlier cold build and B reproduced 3 existing `ActionSelector.SelectionMetadata` CS0649 warnings |
| Focused MatchEngine law, W8 B, handling, clock and snapshot suite | 317 passed, 0 failed before the final four eligibility cases |
| Final W8 C fixture after PR review | 99 passed, 0 failed |
| Goalkeeper assembly | 111 passed, 0 failed, 27 existing deferred/skipped cases |
| Whole MatchEngine Linux assembly, initial run | 917 passed / 3 failed / 16 skipped in 42 min 59 s; all three failures were forced restore fixtures missing the newly required #11 hand episode |
| Initial affected fixtures after correction | 127 passed / 0 failed: all SnapshotRestore, W8BPrerequisite, W8CHandControl and GoalkeeperClockCorrection cases; production validation unchanged |
| Final affected fixtures after PR review | 143 passed / 0 failed, including 16 new exact-480 CONTACT round-trips |
| Fresh whole MatchEngine Linux assembly | **940 passed / 0 failed / 16 skipped / 1 inconclusive**, 34.8409 minutes; no filter or exclusions. Console omits the inconclusive case; TRX retains all 957 entries |
| Release fixture-hook build and IL inspection | Passed: no ball-injection/counter fields or call sites in release execution; both retained exactly once in development |
| Evidence integrity and tooling tests | Passed: 38 registered directories, 22 unit tests |
| Assembly graph | Passed; no upward references or cycle |
| Unity metadata / documentation consistency | Passed |
| Scoped lower-level repository gate | Passed: 83 W8 C cases and 15 claim-path cases; not a full PR policy run |
| Final full-time structural checks | 4 passed (sentinel/future clock and in-flight active-clock checks retained at full time) |
| Final tactical-clock regression fixture | 17 passed |
| Native pre-commit hook | Exceeded its fixed 60-second budget; receipt retained. Local handoff commit uses a per-command hook bypass after independent checks, not a passing-hook claim |
| Pinned-host Unity compile and whole MatchEngine EditMode | **Passed at `fdbbff5a`:** full import from an empty `Library`, 0 `error CS`, three known warnings; 956 results, **939 passed / 0 failed / 16 skipped / 1 existing inconclusive** in 1,792 s; `W8CHandControlTests` 99/99; source porcelain empty throughout ([receipt](evidence/host-w8c-fdbbff5a-20261106/README.md)) |

`W8CHandControlTests` covers exact 0/479/480/481 frames, legal CONTACT for all three delivery kinds,
admission at 480 versus 481, production-policy and defensive in-flight cancellation, terminal drain,
all eligibility gates, mirrored sides and the high-Y tie, inclusive area boundaries, legal body-outside
holds, outside-handling precedence, post-attachment out-of-play precedence, same-frame foul precedence
without card draws, separate feet counters, clock teardown/reset, corrupt restores and full-time freeze.
Existing W8 B replay/target/zero-RNG cases and D-07 tests remain in the focused selection.
`GoalkeeperClaimTests` checks the exact stored frame at all three real claim paths.

## Paired Linux comparison

The observer and population were frozen before result-bearing capture in
`work/w8c/probe/PREREGISTRATION.md`, with `FROZEN-SHA256SUMS`. Identical supplemental hooks are applied
only in isolated B/C copies. The frozen `W8StageADiagnosticTests.cs` is unchanged and its original
census runs alongside the supplemental observer. The supplementary 600-tick snapshot-neutrality test
passed in both arms. B fingerprints must reproduce the six adopted post-law values before C results
are interpreted. Both captures passed. All six B fingerprints reproduce. Every one of the 1,944,000 frame records
per arm is identical for possession, ball kinematics and canonical Tier-A events; no first divergence
exists. Snapshot digests are retained separately and differ in all six C cases with v28.

The canonical release populations and danger-window outcomes are also identical: 126 hand releases,
12 complete opponent-first-touch windows, no censoring, 3 overlapping windows, 8 unique opponent shots
and 6 unique opponent goals, with no releasing-team shot or goal. Corpus: 146 shots / 47 goals over
1,944,000 frames in each arm (0.0145062 goals per 600 frames). No excess opponent goal appears in C.
The [evidence record](evidence/w8-c-20261106/README.md) preserves raw rows, memberships, source hashes,
receipts, and the correction of a diagnostic release marker that initially included feet/parry kicks.
The first broad-marker analysis is retained as superseded; the final canonical population joins to
the unchanged frozen W8 observer's hand-episode identities. No gameplay or selection-rule change
was made to repair that classification.

Each frame records possession, the full ball kinematics and a digest of the exact canonical Tier-A
ledger bytes. Raw contact/release records preserve frame/order, source kind, player/team, position and
restriction history; canonical shot/goal identities preserve credited scorer/team and ScoringTeam
separately. The frozen derivation selects the first other-player opponent touch within 507 frames and
records the following 600-frame duration, censoring, overlap and team-attributed outcomes. Snapshot
digests are reported separately from gameplay equality because v28 changes the serialized state.

## Full Linux run and fixture correction

The whole-assembly run finished with 917 passed, 3 failed and 16 skipped. Its failures were
`RoundTrip_GoalkeeperDistributionMidWindup_RestoresFieldsAndContinuation`,
`RoundTrip_GoalkeeperDistributionThroughContact_RestoresDeterministically`, and
`CanonicalRestore_PreservesOriginalReceiver_ForExactlyOnceF05`. These pre-C fixtures forced a
#5 request while #11 was inactive and had no hand clock. The v28 reader correctly rejects that
state. The fixtures now enable GK wiring and stage the owning `Distributing` state, exact claim
frame and accepted-request wait latch. The validator is unchanged. All 127 cases in the affected
restore/prerequisite/C/clock fixtures pass after correction. The original full-run receipt and final
focused receipt are preserved. After the review follow-up below, a fresh unfiltered whole-assembly
run completes with **940 passed, 0 failed, 16 skipped and 1 inconclusive** in 34.8409 minutes.
Its TRX, console log and summary are retained alongside the initial failed receipt.

The console reports 956 tests (940 passed + 16 skipped); the TRX has 957 result entries because it
also retains `ATackleFoulIsGivenAsASlideTackleAndNotJudgedTwice` as inconclusive/`NotExecuted`.
That existing test uses `Assume.That(connected > 30)` and the corpus has 15 connecting challenges
(won 4, loose 10, foul 1). The initial full run has the identical assumption result and counts.
The 16 other skipped cases are existing opt-in diagnostics. No predicate or test was disabled.
This is a successful zero-failure Linux run, not a claim that every discovered case passed.

## PR #542 review follow-up

Main `af116839` is merged locally, resolving the file-manifest and roadmap conflicts while
preserving both evidence directories. The evidence registry test expects **38**, the merged total.

The post-reattachment ball injection and shared whistle-finish counter are now compiled only for
`UNITY_EDITOR`, `DEVELOPMENT_BUILD` or `UNITY_INCLUDE_TESTS`. Their call sites use matching
`Conditional` attributes. A release build with only `TRACE` defined passes; metadata/IL inspection
confirms both fields and both calls are absent. The development build retains each call exactly once.
This preserves fixture behavior without adding either hook to release execution.

Sixteen new snapshot round-trip cases cover CONTACT exactly at claim frame + 480, from both a
mid-windup save and a save with zero remaining windup ticks. They exercise both teams, all three
delivery kinds and the maximum 84-frame kick windup. Original and restored continuations have
identical snapshot digests, emit exactly one completion on frame 480 and award no corner.
The affected restore/prerequisite/C/clock suites pass **143/143**.

The published head initially had no GitHub workflow runs or commit statuses. Its pull-request
workflow targets main; GitHub cannot run that event while the PR has merge conflicts. Publishing
the conflict-resolved head is needed before checking CI again. Local Linux results do not establish
a GitHub or Unity result.

## Host handoff

Use the final source tree, Unity **6000.4.9f1**, and the established pinned-host workflow. Force the
recursive `Assets/Scripts` reimport; inspect the full compile interval in Editor.log, including
`error CS` lines logged as type Log. Then run **all of `TacticalDirector.MatchEngine.Tests` in
EditMode**, preserving XML, Editor.log, source identity and the result summary. A green Linux test run
does not satisfy this host requirement. The paired Linux comparison is complete.

**Completed November 6, 2026** at `fdbbff5a` (src tree `3a457778`): fresh detached worktree, empty
`Library`, `Assets/Scripts` junction to its own `src`, then an unfiltered whole-assembly EditMode run.
Results and redacted Editor logs are in
[`evidence/host-w8c-fdbbff5a-20261106/`](evidence/host-w8c-fdbbff5a-20261106/README.md). Unity's
process exit code is 2 only because its runner counts the existing inconclusive case; the XML records
0 failed. C closeout on the roadmap follows the PR merge.

## Version history

| Version | Date | Notes |
| --- | --- | --- |
| 1.0 | November 6, 2026 | W8 C implementation, v28 contract, Linux checks and outstanding host evidence. |
| 1.1 | November 6, 2026 | Six B fingerprints reproduced; exact B/C gameplay equality and identical canonical-release danger windows. Superseded marker analysis retained; host remains blocked. |
| 1.2 | November 6, 2026 | Full Linux run 917/3/16; three pre-C invalid restore fixtures corrected, 127 affected tests pass. Owner will arrange Claude local-host Unity validation. |
| 1.3 | November 6, 2026 | Native pre-commit hook reached the fixed 60-second timeout; receipt retained and local handoff commit uses a per-command hook bypass after independent checks. |
| 1.4 | November 6, 2026 | Merge main af116839 Pre-C control/tooling without simulation-source changes; preserve both evidence sets. All 126 release and 12 clearing identities agree; one parry/pickup classification correction aligns both applied-touch counts to 16,264 without changing outcomes. |
| 1.5 | November 6, 2026 | PR #542: release-only omission of two fixture hooks proven by build/IL, 16 exact-480 CONTACT restore cases, 143 affected passes and fresh full Linux 940 passed / 0 failed / 16 skipped / 1 existing inconclusive. Host Unity and published-head CI remain pending. |
| 1.6 | November 6, 2026 | Pinned-host Unity 6000.4.9f1 at `fdbbff5a`: clean full-import compile and whole MatchEngine EditMode 939 passed / 0 failed / 16 skipped / 1 existing inconclusive; receipt `evidence/host-w8c-fdbbff5a-20261106/` (39 registered directories). |
