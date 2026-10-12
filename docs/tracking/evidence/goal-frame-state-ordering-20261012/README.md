# Goal-frame state ordering — ERR-001-007

**Created:** October 12, 2026
**Version:** 1.2
**Purpose:** Preserve executable failing-before and passing-after evidence for the PR #499
outside-post `OutOfPlay` deadlock. This is non-certifying .NET 8 / repository UnityShim evidence.

## Results

| Run | Outcome |
|---|---|
| Final new engine cases on unmodified production `a17aa82e` | 0 passed / 10 failed |
| Corrected complete restart fixture | 19 passed / 0 failed |
| New front-frame / already-out physics regressions on unmodified production | 0 passed / 5 failed |
| Corrected complete Ball Physics assembly | 120 passed / 0 failed |

The baseline engine cases fail for the intended reasons: all four captured-approach variants
and both restore cases retain `OutOfPlay` at the in-play rebound; all four rear-contact
variants incorrectly count a frame contact after the full crossing. The initial post-contact
state assertion is checked only after confirming an actual woodwork strike, an in-play
position and no restart. Both goals and both halves are staged using normal engine ticks.
The correction advances the rebound next tick and awards the later genuine exit. The
crossing-before-contact cases lock both corner and goal-kick awards. Restore is compared
sequentially, using the existing canonical header/payload codec and per-tick snapshot digests.

## Behaviour scope

Every eligible integrated woodwork strike now reaches final state classification at its
post-contact position and velocity. The usual outcome can remain the same, but a transition
between Airborne, Bouncing, Rolling or Stationary can change even when the old match did not
deadlock. Such differences can alter later trajectories, digests and RNG consumption. The
completed-crossing guard also removes later frame contacts from the woodwork counter.
The internal sweep start is captured after bounce handling; for a Bouncing input, ApplyBounce
can change Z to the resting radius first, so that starting geometry can differ from the old
pre-update host capture. It is not strictly true that the starting point is always unchanged. No
production executable code is changed by this review follow-up.

## Execution boundary

SDK 8.0.415, runtime 8.0.21, NUnit/NUnitLite 3.14.0, Ubuntu 24.04.3. The repository generator
built the normal asmdef-derived projects; these are not hand-combined Roslyn engine assemblies.
`dotnet test` compiled the projects but VSTest failed on container process lookup. The normal
`tools/run-tests-local.sh --pr` policy was also attempted with its explicit PR base and failed
at CLI restore on the same process-information boundary. Neither command is reported green.
`local-pr-gate-process-failure.txt` retains that failure and the full-log digest.

The scratch NUnitLite runner loads one generated test DLL and resolves its adjacent dependencies;
its complete source is retained as `NUnitLiteRunner.cs.txt`. `--workers=0` prevents concurrent
fixtures from sharing the process-static EventBus. Ordinary NUnit assertions, test actions and
the repository LogAssert verification remain enabled. NUnitLite has no Coverlet/VSTest collection:
these runs do not satisfy the PR-equivalent policy, coverage or governing Unity checks.

## Reproduction on a normal host

1. Check out this fix and run `python3 tools/dotnet-ci/generate_projects.py`.
2. Run `dotnet test src/ball-physics/tests/ball-physics-tests.gen.csproj`.
3. Run `dotnet test src/match-engine/tests/match-engine-tests.gen.csproj --filter
   FullyQualifiedName~MatchEngineRestartTests` (keep the filter on one command line).
4. To reproduce before the fix, use a separate checkout at `a17aa82e3a0d1aa38f2749356c8e73709a63d14f`,
   copy only this fix's two existing test files into it, generate its projects and repeat the
   same tests. The production files must stay byte-identical to that base. The evidence baseline
   temporarily restored exactly those three production files from `git show HEAD:<path>` before
   rebuilding, then restored the correction immediately after the baseline run.

The actual container compile command used `dotnet <SDK>/MSBuild.dll <generated-project>
-t:Build -m:1 -nr:false -p:UseSharedCompilation=false -verbosity:minimal`; normal restoration had
already populated dependencies. The runner command was `dotnet NUnitLiteRunner.dll <test-DLL>
--workers=0 --result=<XML>`, with the baseline filters `method =~ int_OutsidePostRebound or
method =~ int_FullCrossingBeforeRearPostContact`, and `method == PhysicsUpdate_FrameBeforeFullCrossing_KeepsReboundPlayable
or method == OutOfPlayBall_IsNeverReturnedToPlayByAFrameCollision`. XML/log files preserve the
exact selections and outcomes.

## Remaining governing checks

Unity 6000.4.9f1 final-head compile; the new engine and Ball Physics regression cases; then the
unchanged `sim_match_engine_inposs_gate`. No Unity instance is attached to this container.
The deadlock and original inposs entries remain open until that evidence is recorded.
A post-fix match-corpus stall audit is also still owed; none is claimed by the regression
fixture or by a passing share alone. On the final head run
`PlatformDivergenceDiagnostic_SixSeedSharesAndStateTrace` with
`TD_PLATFORM_DIVERGENCE_OUT=<dir>` and a platform-specific prefix, without a ticks override,
on both the pinned Unity host and the .NET lane. Retain all six complete match traces and
inspect both halves for repeated `OutOfPlay` ball states (state ordinal 5) with unchanged
position. After tick 3600 the existing instrument samples every 600 frames (10 seconds);
consecutive such checkpoints are evidence of a long stall, but this sampled trace does not
prove the exact duration of a shorter episode. Record every candidate's seed, start/end
checkpoint, interval and period; reject a corpus with a long stall and obtain a per-tick
window for diagnosis rather than recalibrating the share floor. This audit is a closure
check, not an additional completed result.

**.NET half recorded (v1.2).** On merged head `0a82a51` (src/tools identical to `c5f8c8b`) the
test ran on this Linux container with the Ubuntu .NET 8.0.131 SDK / 8.0.31 runtime, Debug build,
no ticks override. `stall-audit-net8-linux.txt` keeps the command, shares, per-seed audit output
and full-trace SHA-256s; `stall_audit.py.txt` is the audit script. No seed has a sampled
`OutOfPlay` row in either half; the script does flag the pre-fix Unity freeze on
`0x0F1E2D3C4B5A6978`. All six traces are byte-identical to the pre-fix .NET traces in
`inposs-platform-divergence-20261011/full-trace-sha256.txt`, so the correction never takes
effect in these six .NET matches. The result shows no stall and no regression on .NET; it does
not show that the fix removes the Unity stall. The Unity half remains owed.
No completed result was retained for the supplementary broader local engine attempt; it is
not evidence of a pass. GitHub's full functional gate on `97a0576` completed successfully: 37 suites,
3,837 passed / 0 failed / 221 skipped; Match Engine 569/0/15 and Ball Physics 120/0/0.
`ci-97a0576-functional-summary.txt` preserves the per-suite summaries. The unchanged
frozen scenario bounds passed; this does not supply the pending corpus stall audit. The Markdown link job failed on an external 503 in unchanged
`docs/specs/deterministic-sim/golden-vectors/siphash-2-4-kat.md` (workflow `38280459956`, job
`114916213832`). The initial retry request was refused because the workflow was still running.
After completion, the one accepted retry passed (job `114939623604`);
`ci-97a0576-link-retry.txt` records that outcome.
`link-check-97a0576-failure.txt` preserves the original failing URL/status. The next published
head must receive fresh CI; no blanket "static CI passed" claim is made.
Local static checks passed: document consistency, assembly layering, Unity metadata, evidence integrity
and the CI-scoped Markdown lint. Evidence-manifest unit tests are 17/17. The tooling sweep is
343 passed / 1 failed / 2 skipped out of 346; the one failure is
`test_snapshot_checkout_disables_lfs_process_and_smudge_even_when_required`, with Git exit 128
("the remote end hung up unexpectedly"). It also fails at clean `a17aa82e` in this container;
`tooling-checkout-baseline.log` preserves that independent 3-test reproduction.

Full second-half stall duration was not simulated to the end. The before cases prove the same
invalid absorbing state in that half; source inspection shows full-time sets `_matchEnded`
without resetting the ball, whereas half-time applies a centre-spot restart. The fix does not
rely on either boundary to resume play.

## Integrity

`SHA256SUMS` covers every member other than itself. `source-identity.txt` preserves the initial head's baseline/fingerprints, and
`review-source-identity.txt` verifies unchanged executable/assertion lines in this follow-up.
The original record also states the comment-only metadata timing. The original PR #499 Unity trace
remains in the separate `inposs-platform-divergence-20261011` evidence directory.

## Revision history

| Version | Date | Change |
|---|---|---|
| 1.2 | October 12, 2026 | .NET half of the six-seed corpus stall audit: no candidate; traces byte-identical to pre-fix .NET. Added `stall-audit-net8-linux.txt` and `stall_audit.py.txt`. Unity half still owed. |
| 1.1 | October 12, 2026 | Review corrections: broader contact/state impact, final 120/120 count, honest incomplete local/CI status and explicit pending post-fix corpus stall audit. Recorded test XML/logs unchanged. |
