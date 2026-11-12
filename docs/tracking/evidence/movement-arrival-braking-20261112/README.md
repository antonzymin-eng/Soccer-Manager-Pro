# Movement feedback baseline — W13 implementation

> **Created:** November 12, 2026 · **Version:** 0.2
> **Purpose:** Retain reproducible evidence for the arrival/speed-control planning review.
> **Implementation:** `80d60dc1cbf66aac2e91e24e1cc5bf7848474222`, remote branch
> `claude/great-sagan-rpcmhp`, inspected in an isolated worktree.
> **Runner:** Linux .NET shim, SDK 8.0.426 / runtime 8.0.32. Unity Editor was not run.

## What ran

Run the implementation's 16 explicit descriptive marking cases without modification, then repeat
the same 16 with the test-only `MovementArrivalBaselineProbe` observer. The complete descriptive
summary lines are identical between runs. This establishes repeatability of W13's existing deterministic tests, not independent confirmation of the rig or causal diagnosis, and is not a full digest-neutrality proof.
Separately, run the added stale-tackle diagnostic through the real `DefensiveAITick.Tick` for both
team IDs. All 34 diagnostic invocations completed successfully; their assertions check capture and
bug reproduction, not acceptable gameplay. No production fix is present.

The existing rig repairs the scripted attacker's Position, Velocity, cached Speed, and locomotion
state each physics step. It initially holds defenders on their slots, then releases the marker
for closure/pursuit. These are controlled scripted scenarios, not unscripted match measurements.
Runner/stationary cases are authored as geometric mirrors in world space for home and away; full equivalence of attributes, identities, cached state and contact/RNG ordering has not been established. The stale-tackle
case exercises #14's canonical coordinate frame for both team IDs; it does not exercise the
engine's world-to-canonical adapter or complete physical tackle resolution.

## Findings and owning records

This table is a fixed record of the retained run, not a second live blocker register. W13 owns
[F5](https://github.com/antonzymin-eng/Soccer-Manager-Pro/blob/claude/great-sagan-rpcmhp/docs/tracking/open-issues.md#w13-goalward-runner-collision),
[F6](https://github.com/antonzymin-eng/Soccer-Manager-Pro/blob/claude/great-sagan-rpcmhp/docs/tracking/open-issues.md#w13-gate-flapping-guard-locks),
[F7](https://github.com/antonzymin-eng/Soccer-Manager-Pro/blob/claude/great-sagan-rpcmhp/docs/tracking/open-issues.md#w13-stationary-man-orbit), and
[F8](https://github.com/antonzymin-eng/Soccer-Manager-Pro/blob/claude/great-sagan-rpcmhp/docs/tracking/open-issues.md#w13-tight-standoff-body-contact).
The stale-request probe supports the previously recorded ERR-014-010 limitation, promoted to
[W13-F9](https://github.com/antonzymin-eng/Soccer-Manager-Pro/blob/claude/great-sagan-rpcmhp/docs/tracking/open-issues.md#w13-stale-tackle-after-demotion).

| Blocker | Reproduced observation |
|---|---|
| W13-F5 — goal-bound runner | The cases labelled 3 and 5 m/s use those values as speed caps. Their ramps are identical until knockdown at about stride 12, so the common 19/40 GROUNDED count does not test response to different contact speeds. Later trajectories differ as the caps take effect. This does not isolate a sole cause. |
| W13-F6 — command flicker / guard | At 2.6 m/s with ordinary stand-off, both teams record 32 command flips across 59 boundaries and four guard-lock entries. In the 360 physics steps of each recorded pursuit window, the added observer records 24 resulting state changes and four false-to-true guard flag changes. In these ordinary cases the marker remains goal-side with a bounded gap; measured harm is churn/lock engagement, while gameplay costs remain unmeasured. |
| W13-F7 — stationary target | With ordinary stand-off, both teams remain at 2.20 m/s throughout the 15-stride observation. Target-point error spans 0.01–0.86 m home and 0.02–0.86 m away. The marker does not settle. Positions are retained at 60 Hz. |
| W13-F8 — tight runner | At 2.6 m/s the away case is not goal-side on 54 of the 58 checked samples, within a 60-stride run; the fixture deliberately excludes strides 0–1 from this predicate. Home records one such sample. The reported 54 figure is reproduced, with this denominator qualification. |
| W13-F9 — demoted duplicate | At canonical ticks 3, 7, and 11, agent 17 publishes a COMMIT request against opponent 100 although its final assignment is ZONAL with target −1. Agent 18 retains the mark. Three stale requests occur for each team ID. Physical execution of those requests remains a separate engine-level regression requirement. |

The exact implementation matters: `ActionDispatcher.SelectMoveTier` applies the man's speed tier
only while the marker is slower than his man. Thus these 2.6 m/s flips occur through that speed gate,
not by repeatedly crossing the 6 m distance boundary. W13.1c is already present in this baseline and
has not eliminated the defect. Retained output also covers 3.5/5 m/s pursuit and stationary tight marks.

## Unresolved mirror asymmetry — calibration hold

[W13-F10](https://github.com/antonzymin-eng/Soccer-Manager-Pro/blob/claude/great-sagan-rpcmhp/docs/tracking/open-issues.md#w13-mirror-asymmetry)
owns this question. The tight runner differs by 1 home versus 54 away non-goal-side samples;
tight stationary marking is goal-side on 12/15 versus 4/15 samples; goal-bound minimum gap differs
by 0.68 m versus 0.50 m. These observations do not identify stand-off distance as the cause or prove
an engine mirror defect. Rig inputs/attributes, identity/phase and collision/RNG processing order
must be compared to find the first divergence. Until explained, these data must not be used to
choose/calibrate the tight-marking floor. Static body-radius arithmetic remains valid independently.

## Instrument and traces

`MovementArrivalBaselineProbe` is the test-only addition to the existing `MatchEngineMarkingTests.Rig`
in [probe.patch](probe.patch). It copies the two centre-backs' state immediately before and after
each real `E.RunTick`, and writes one row per centre-back per physics step to the directory selected
by `TD_MOVEMENT_REVIEW_TRACE_DIR`. The patch also contains `MovementArrivalStaleTackleProbe`.

[physics-traces.tar.gz](physics-traces.tar.gz) contains one CSV for each of the 16 descriptive cases.
Each records frame, team/entity, states before/after, resulting state-change indicator, raw guard
flags before/after, lock-entry indicator and lock expiry, command state, speed, position, mark point,
target validity, and grounded reason. The flags are read directly; lock engagement is not inferred
from command changes. `lock_until_s = -Infinity` is the guard's unused sentinel.

These are resulting state changes at tick boundaries, not a count of rejected transition attempts.
The raw `IsLocked` flag can persist until another transition attempt; it must not be equated with
active suppression on every flagged frame. The expiry is retained for that reason. A fix-validation
probe should additionally capture requested transitions and the guard's rejection decision where
those are needed to distinguish mechanisms.

For `RunEnvelope`, the recorded pursuit begins at CSV frame 426 after the fixture's warm-up and
closure; frames 426–785 are the 60-stride pursuit. Keep warm-up separate from that window.
The observer reads state only; no scene, package, runtime source, or production setting was changed.

## Reproduce

In a separate checkout of the implementation commit, activate the prepared .NET environment and
generate the project files. Select the explicit probes by name:

```bash
source /workspace/.cloud-tools/activate.sh
python3 tools/dotnet-ci/generate_projects.py
dotnet test src/match-engine/tests/match-engine-tests.gen.csproj \
  --filter 'FullyQualifiedName~MatchEngineMarkingTests.Descriptive_RunnerEnvelope_PeaksCommandFlipsAndGuardLocks|FullyQualifiedName~MatchEngineMarkingTests.Descriptive_StationaryMan_MarkerSettlingAndSeparation|FullyQualifiedName~MatchEngineMarkingTests.Descriptive_GoalwardRunner_MarkerStateAndContact' \
  --logger 'console;verbosity=normal' --logger 'trx;LogFileName=baseline.trx'
git apply --unidiff-zero /path/to/retained/probe.patch
TD_MOVEMENT_REVIEW_TRACE_DIR=/path/to/new-traces dotnet test \
  src/match-engine/tests/match-engine-tests.gen.csproj --no-restore \
  --filter 'FullyQualifiedName~MatchEngineMarkingTests.Descriptive_RunnerEnvelope_PeaksCommandFlipsAndGuardLocks|FullyQualifiedName~MatchEngineMarkingTests.Descriptive_StationaryMan_MarkerSettlingAndSeparation|FullyQualifiedName~MatchEngineMarkingTests.Descriptive_GoalwardRunner_MarkerStateAndContact' \
  --logger 'console;verbosity=normal' --logger 'trx;LogFileName=instrumented.trx'
dotnet test src/defensive-ai/Tests/defensive-ai-tests.gen.csproj \
  --filter 'FullyQualifiedName~MovementArrivalStaleTackleProbe' \
  --logger 'console;verbosity=normal' --logger 'trx;LogFileName=stale-tackle.trx'
```

The patch uses zero context; `--unidiff-zero` is required. The source patch is retained as diagnostic material and is not applied to the planning branch.
Use a fresh trace directory on each run. Ordinary commands may require different SDK activation
outside this prepared environment.

## Inventory and limits

- [source-identity.txt](source-identity.txt): exact source and toolchain identity.
- [probe.patch](probe.patch): all diagnostic source changes, including the stale-tackle reproduction.
- [summary.txt](summary.txt): deduplicated descriptive and stale-request results.
- [baseline.log.gz](baseline.log.gz), [instrumented.log.gz](instrumented.log.gz),
  [stale-tackle.log.gz](stale-tackle.log.gz): complete runner output.
- [baseline.trx.gz](baseline.trx.gz), [instrumented.trx.gz](instrumented.trx.gz),
  [stale-tackle.trx.gz](stale-tackle.trx.gz): NUnit/VSTest results.
- [physics-traces.tar.gz](physics-traces.tar.gz): all 16 per-physics-step CSV traces.
- [SHA256SUMS](SHA256SUMS): integrity hashes of every other file in this directory.

The approximate 0.97 m braking / 1.19 m combined travel figures in the plan remain calculations,
not new stopping-distance measurements. This targeted diagnostic run is not the repository PR gate,
Unity validation, pinned-platform certification, an arrival fix, or permission to enable marking.

## Version history

| Version | Date | Notes |
|---|---|---|
| 0.1 | 2026-11-12 | Retain the W13 corrected-rig baseline, direct state/guard observations, and stale-request reproduction for the v0.2 planning review. |
| 0.2 | 2026-11-12 | Move under the registered evidence root, link W13 issue ownership, qualify deterministic repeatability/gameplay harm/speed-cap sensitivity, and hold tight-floor calibration on unresolved mirror asymmetry. Raw capture artifacts are unchanged. |
