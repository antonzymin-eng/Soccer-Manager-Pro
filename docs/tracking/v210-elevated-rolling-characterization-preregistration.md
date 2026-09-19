# Ball Physics v2.10 elevated-Rolling characterization pre-registration

**Date:** 2026-09-18  
**Status:** FROZEN BEFORE RESULT  
**Production baseline:** `33cf81443e2c5ab43d7a1befcb22e6c0cfea7289` (`main` at Step 3.3 start)  
**Historical contract landing:** PR #416 / Ball Physics v2.10  
**Counterfactual provenance:** preserved experimental branch
`evidence/pr416-narrow-rolling-candidate` at
`bb501a2128f9efbef5e98bffadb0d98214493e78`.

## Question

Ball Physics v2.10 is contract-correct for PR #416: an elevated `Rolling` state is reclassified to
`Airborne` before force selection, and the state machine checks altitude before the low-speed stop
rule. The open issue is distributional, not contractual.

This evidence asks:

1. how often natural full-match play reaches the raw **elevated `Rolling`** state at the exact
   MatchEngine Physics-phase boundary;
2. how often those states are moving versus already below `MinVelocity`;
3. how much current v2.10 changes representative downstream ball trajectories and Decision Tree
   action selection compared with the preserved **narrow** counterfactual.

No measured value in this instrument is a pass/fail acceptance bound. A large delta does not by
itself make v2.10 defective, and a small delta does not revise the v2.10 contract.

## Frozen semantic arms

### `v210` — shipped baseline

Current production behavior, unchanged.

- `BallPhysicsCore`: any elevated `Stationary` or `Rolling` state normalizes to `Airborne`
  before force selection.
- `BallStateMachine.Rolling`: altitude is checked before the low-speed stop rule.

### `narrow` — non-shipping counterfactual

The workflow applies only the two behavioral replacements preserved by
`bb501a2128f9efbef5e98bffadb0d98214493e78`, reconstructed onto the current baseline:

- elevated `Stationary` still normalizes immediately;
- elevated `Rolling` normalizes before force selection only when it is already below
  `MinVelocity`;
- in the `Rolling` state-machine branch, the stop rule runs first and a **slow** elevated ball
  becomes `Airborne`; a **moving** elevated ball remains `Rolling`.

No `ApplyKick`, `BallCollision`, possession, Perception, W2, or other production behavior is
changed by the counterfactual. The workspace patch is retained in each narrow-arm artifact and the
workflow fails if it modifies any path other than `BallPhysicsCore.cs` and
`BallStateMachine.cs`.

This arm is characterization only. It is not a proposed v2.11.

## Frozen six-seed corpus

The corpus is the existing six-seed `CloseChanceDiagnosticTests` population, including its
`diagnostic.roster` squad recipe:

- `0x0F1E2D3C4B5A6978`
- `0x00000000D1A6D05E`
- `0x5EED000000000003`
- `0x5EED000000000004`
- `0x00000000D1A6D05F`
- `0x1A2B3C4D5E6F7081`

Each arm runs **324,000 ticks / one full 90-minute match per seed**.

## Frozen measurements

### Direct elevated-Rolling exposure

Captured immediately before `BallPhysicsCore.UpdateBallPhysics`:

- elevated-`Rolling` raw ticks;
- elevated-`Rolling` episodes (contiguous raw ticks count once);
- moving versus slow raw ticks and episodes, split at production `State.MinVelocity`;
- post-Physics result of those raw ticks: `Airborne`, `Rolling`, or other;
- mean/max raw elevated-`Rolling` height and speed.

The **moving episode/tick count on the v2.10 arm** is the direct frequency of the broadened v2.10
semantic surface: those are precisely the observations the narrow arm would preserve as
`Rolling`.

### Representative downstream trajectory

Per seed/arm:

- speed-integral distance proxy `sum(|v| × dt)`;
- full-match mean/max ball height and speed;
- ball-state residency for all six `BallStateType` values;
- ticks in either final third;
- ticks with an authoritative holder;
- holder-change count;
- final score;
- one deterministic trajectory fingerprint sampled once per simulated second from quantized
  position, velocity, state, and holder.

The fingerprint is an identity/difference detector, not a scalar distance metric.

### Representative downstream action selection

For all 22 Decision Trees, count each newly selected heartbeat action exactly once by stable
`ActionType` ordinal:

`PASS, SHOOT, DRIBBLE, HOLD, MOVE_TO_POSITION, PRESS, INTERCEPT, SAVE`.

These are descriptive counts. No action count is an acceptance target.

## Interpretation rules

- Current v2.10 remains the baseline and remains contract-correct unless separate evidence establishes
  an actual contract defect.
- The counterfactual is used only to quantify distribution sensitivity.
- Deterministic per-seed differences are reported individually before any pooled summary.
- A trajectory fingerprint mismatch proves different sampled trajectories, not that either
  trajectory is better.
- Action-count changes are downstream resampling evidence, not causal attribution to a particular
  tactical subsystem.
- Existing warning/error traffic is retained in detailed logs. The diagnostic may suppress NUnit's
  failing-message policing so characterization can complete, but green execution must not be cited
  as warning-clean.
- No threshold, seed, run length, metric, or counterfactual definition may be changed after the
  result-bearing run is observed.

## Closeout rule

After evidence is captured, remove the temporary MatchEngine observation fields/accessors, the
env-gated test, the counterfactual patch helper, and the branch-only workflow. Preserve the frozen
pre-registration verbatim and archive exact aggregate rows plus provenance in
`docs/tracking/evidence/`.
