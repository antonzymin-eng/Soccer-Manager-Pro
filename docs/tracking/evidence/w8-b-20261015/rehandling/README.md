# W8 B own-release re-handling review closeout

**Created:** October 16, 2026
**Purpose:** Preserve the PR #511 review repairs, original-head regression failures, and an isolated
corrected-B versus post-law six-seed comparison. This is supplementary Linux evidence.

## Source and environment

- Corrected B control: `e54335b051ed51a0c3c394ccdaa016cb8ae28245`.
- Repaired source: `751b11435843b9b0ba2b64c06b7e5042358de58b`, src tree
  `0b46dfe1418c3ab682df83f3b530985d494e55d1`. Later evidence/doc-only commits preserve this src tree.
- SDK 8.0.422, CLR 8.0.28, Linux x64; NUnit/NUnitLite 3.14.0. Production targets netstandard2.1;
  tests target net8.0. Direct Roslyn compilation uses the generated asmdef project compile sets and
  transitive reference closure, no optimization, `DEVELOPMENT_BUILD` defined. `provenance.json`
  records the exact production assembly hashes and source tree. This is a local comparison on one
  environment, not a Unity/platform equivalence assertion.
- The ordinary CLI/VSTest driver crashes in this container. `vstest-failure.log.xz` preserves the
  failure. Direct NUnit execution is used here; it does not claim a complete repository PR-policy run.
- `production-equivalence.txt` proves that rebuilding MatchEngine from the final source, after source
  header/comment cleanup, yields the identical assembly used by the measurement. The later changes
  to the law fixture strengthen tests only; the census and production statements are unchanged.

## Regression and compatibility evidence

`final-engine.xml.xz`: **200 passed / 0 failed**, covering the own-release law, feet-only avoidance,
legal keeper body rebound, same-frame foul disposal, W8 policies/deadlines/fallbacks, snapshot schema,
restarts and census digest neutrality. `final-goalkeeper.xml.xz`: **107 passed / 0 failed / 27 existing
skips**, including two sub-minimum-quality miss cases and mirrored hand-intent eligibility cases.
A sub-threshold hand attempt calls none of ApplyKick, SetPossessor or ParkBall and records no touch.

The same new law fixture was overlaid on original head `fff7cf5b`; the only accompanying source
change appends the observational enum value used by the fixture, with no producer or gameplay change.
`pre-review-red.xml.xz` has **10 failures / 2 passes out of 12**: mirrored direct claim-formation checks
(with/without restore), stale smother cancellation (with/without restore), and captured-foul disposal
fail before the fix and pass afterwards. The inherited-drop feet-pickup fixture already passes on the
original head; no claim is made that every drop previously produced an IFK.

`nunit35-compile.log.xz` records compilation of both test assemblies against NUnit 3.5 with a matching
TestTools shim. This checks API compatibility only. **Unity 6000.4.9f1 editor compilation and required
host EditMode are not run: this session exposes no connected Unity host/editor tool.**

`tooling.log.xz`: the initial 346-case invocation passes. The final default-Git rerun has 345
passes / one failure in the unchanged precommit LFS fixture (`tooling-closeout.log.xz`); the targeted
retry also fails, while the original-head isolated case passes. The trace fails at `git write-tree`
inside the temporary fixture before the mocked test runner executes. No keeper code, hook or Python
test changes are involved. The entire current 346-case suite passes with system Git 2.43.0 via PATH
(`tooling-git243.log.xz`); default Git reports 2.51.1. This is observed Git/path-dependent local
behavior, not a confirmed upstream Git root cause. Both failing and successful runs are retained;
normal final-head CI remains authoritative. Document consistency, assembly-tier direction, metadata
and whitespace checks pass. The optional recurring-defect lint still reports six existing errors and one warning
in this scoped folder set; its original-head control has the same six plus four missing carrier
header fields repaired here. Those unrelated header/history findings are not silently counted green.

## Measurement contract

Both arms run the same frozen six seeds through a normal constructor plus `EnableGkHeading()`,
324,000 ticks per seed, shipping SlowDown policy, no squad override. The existing B census provides
CONTACTs, W5 feed checks, receiver/same-keeper/other outcomes, fouls, cards and final fingerprints.
The post-law census adds read-only restricted-hand-contact observations and ledger restart follow-ups.
The four existing census neutrality cases compare every digest for 300 ticks each and pass.

A **re-handling attempt** is an actual restricted call to the law contact backstop, before the
out-of-play precedence test. It is not a rejected tactical opportunity; prevention forms no hand
intent. A **sanction** is a Resolve ledger restart with appended event kind 5 (IFK) or 6 (DFK).
A before-arm attempt count is unavailable: that arm has no own-release law instrumentation. Its
zero law sanctions are structural, not observed law compliance.

For every sanction retain the raw contact position and awarded spot, distinguishing the goal-area
line, other penalty-area positions and outside-area positions. For both teams count executed shots
and goals in `(restartFrame, restartFrame + 600]` (ten seconds at 60 Hz), even across another restart;
this is temporal association, not attribution. `followedFrames` identifies truncated end-of-match
windows. No sanction means no follow-up population, rather than evidence about restart danger.
Ordinary foul-event totals and new law sanctions are reported separately.

All six final fingerprints are retained by arm. Snapshot v26 → v27 itself changes fingerprints, so
a cross-arm digest change is not a measure of gameplay effect. The two composed LongKick fixtures
remain separate from the six-seed totals. No gameplay constant,
trajectory, policy or recovery tuning is performed. New law/avoidance branches change conditional
consumption of existing RNG draws, but introduce no new site or stream.

## Completed six-seed results and post-B anchor

Both arm invocations pass their existing CONTACT/event/W5, zero-drop/DT-race and no-long-OutOfPlay
checks. The control reproduces **every archived corrected-B summary field and all six fingerprints**,
despite the older local runtime. `comparison.json` and `per-match.csv` retain all results; run
`PYTHONDONTWRITEBYTECODE=1 python3 compare_rehandling.py` here to regenerate the derived files.

| Six-match total | Corrected B, e54335b0 | Law plus avoidance, 751b1143 |
|---|---:|---:|
| Canonical distributions / executed events / releases / W5 feeds | 348 | 126 |
| Intended receiver control | 89 (25.6%) | 114 (90.5%) |
| Same-keeper hand return | 155 | 0 |
| Same-keeper feet return as terminal distribution outcome | 0 | 0 |
| Other-agent control | 14 | 12 |
| Interrupted by another kick | 90 | 0 |
| Restricted hand-contact attempts | Not instrumented | 0 (0 per match, all six) |
| Re-handling sanctions | Not implemented | 0 (0 per match, all six) |
| Ordinary foul events | 46 | 48 |
| Yellow / straight red / second yellow | 6 / 1 / 0 | 8 / 1 / 0 |
| Inherited drops / DT races / pending distributions | 0 / 0 / 0 | 0 / 0 / 0 |

No law restarts occur: **0 on the goal-area line, 0 elsewhere in the box, 0 outside the box**.
The ten-second shots/goals follow-up population is therefore empty; its zero counts are not evidence
about how dangerous an IFK is. `handling-restarts.csv` and `restricted-attempts.csv` retain their
schemas with no invented observations. Placement, direct-goal prevention and the rare sanction path
are proven by the mirrored composed regressions, not by these no-sanction matches.

All six terminal fingerprints change. They are recorded exactly in `per-match.csv`; the schema
change alone also changes fingerprints, so they are provenance, not an effect-size measure.
The finite-corpus receiver/claim changes are descriptive, with no gameplay tuning or realism
certification inferred. General outside-area handling, teammate back-pass, eight-second C and
LongKick trajectory remain outside this fix.

**Post-B Linux baseline for B→C:** source `751b11435843b9b0ba2b64c06b7e5042358de58b`, src tree
`0b46dfe1418c3ab682df83f3b530985d494e55d1`, this six-seed post-law arm and the environment above.
C must use this post-law baseline rather than the superseded 348-contact pre-law B arm. An alternative
host/runtime needs a reproduced control or a fresh paired comparison. This recorded measurement
anchor does not close the final-head CI or pinned Unity merge gate; the PR remains a draft.

## Reproduction

Use the pinned source commits and normal repository-generated projects with the recorded toolchain.
Run the named `W8BDistributionDiagnostic_ReportsFrozenCorpusAndLongKickLandings` method with
`TD_W8_B=1`, serial workers, and retain the complete NUnit result and console output. For the direct
fallback, `compile.py` expects `TD_REPO`, `TD_VALIDATION_OUT`, `TD_DOTNET_ROOT` (and an optional
`TD_UNSAFE_REF`) and the normal NUnit/FsCheck runtime dependencies in the output directory. Compile
`Runner.cs.txt` (copy to a scratch `Runner.cs`) there against NUnitLite and NUnit, then execute it with the target assembly and NUnit
options. The canonical CI policy runner remains `tools/run-tests-local.sh --pr`.

All files here belong to the parent evidence directory's complete `SHA256SUMS` contract.
