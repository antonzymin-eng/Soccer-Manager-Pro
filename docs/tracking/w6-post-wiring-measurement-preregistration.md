# W6 post-wiring measurement pre-registration

**Date:** 2026-09-14  
**Status:** PRE-REGISTERED BEFORE W6 EVIDENCE ACCEPTANCE  
**Purpose:** Fix the predictions, denominators, thresholds, and falsifiers used to interpret the post-W6 run before PR #412 is accepted or W2 is re-armed for evidence.

## 1. Why this exists

W6 changes possession/ball-control semantics and is expected to change match trajectories. Therefore the post-W6 corpus must **not** be interpreted by requiring exact absolute-count equality with the corrected W12 baseline. The comparison uses normalized shares and the existing governed pass/fail predicates.

The corrected W12 baseline is the durable evidence in `docs/tracking/evidence/w12/`; `w12-gate-firing-post398-comparison.md` is the derived checked record.

## 2. Corrected W12 baseline

Across 3 deterministic seeds × both teams:

- team-heartbeats: **323,994**
- `InPossession`: **159,801**
- non-`InPossession` denominator: **164,193**

Exit distribution over the non-`InPossession` denominator:

| Exit | Count | Share |
|---|---:|---:|
| `InvariantRejected` | 139,309 | **84.8447%** |
| `Cooldown` | 22,671 | **13.8075%** |
| `Disengaged` | 1,890 | **1.1511%** |
| `Active` | 140 | **0.0853%** |
| `NoCommittedTrigger` | 99 | **0.0603%** |
| `NoPrimaryPresser` | 84 | **0.0512%** |

These six exits sum exactly to 164,193.

`Cooldown` here is the Pressing AI / `DisengageResolver` exit. It is **not** MatchEngine's `_tackleCooldown`; the open Codex P2 on PR #412 concerns the latter and must not be justified using this W12 percentage.

## 3. Primary W6 hypothesis — W2 armed-stall blocker

### Prediction P-W6-1

After W6 physical control is wired and the PR #412 tackle-cooldown P2 is fixed, re-run the existing W2 armed possession evidence without weakening its governed threshold.

The pre-W6 armed evidence produced a `sim_match_engine_inposs_gate` collapse to **0.501** on one scenario seed against the existing **0.70** bound, while tackles-off evidence was **0.975 / 0.966**.

**Support criterion:** with W2 armed against the W6 state model, **every governed scenario seed must remain >= 0.70**. No bound widening is allowed.

**Falsifier F-W6-1:** if any governed armed scenario still falls below **0.70**, then W6 did not remove the W2 stall blocker. W6 may still be correctly wired as a subsystem, but the W2 activation blocker remains unresolved and tackle activation stays disabled.

This is the primary causal test because it exercises the failure W6 was selected to address.

## 4. Secondary W12 hypothesis — rejection wall

`InvariantRejected` is produced by Pressing AI anti-chaos geometry invariants (cover-shadow displacement, maximum pressers in the ball-side third, minimum backline agents). W6 does not edit those invariants directly. Any relationship between physical ball control and the rejection wall is therefore a **hypothesis**, not an expected correctness condition for W6.

### Prediction P-W6-2

If possession/ball-position divergence is materially contributing to the W12 rejection wall, post-W6 canonical measurement should reduce `InvariantRejected` as a share of the non-`InPossession` population.

Pre-registered zones:

- **Support:** `InvariantRejected <= 82.8447%` — at least a **2.0 percentage-point** reduction from baseline.
- **Inconclusive:** `82.8447% < InvariantRejected <= 83.8447%` — between 1.0 and 2.0 percentage points lower.
- **Falsifier F-W6-2:** `InvariantRejected > 83.8447%` — less than a 1.0 percentage-point reduction (including no reduction or an increase). Treat the rejection wall as a separate problem rather than crediting W6 for it.

A drop in `InvariantRejected` does **not** count as support if suppression is merely displaced. For the same run, report the full exit distribution. In particular:

- no single suppression bucket among `Cooldown`, `Disengaged`, `NoCommittedTrigger`, or `NoPrimaryPresser` may increase by **>= 2.0 percentage points** and still be described as a clean reduction in the rejection wall;
- `Active` must be reported explicitly; do not infer improvement from a lower rejection share if active directives disappear;
- report both raw counts and shares, but interpret cross-trajectory movement from shares.

The thresholds above are diagnostic decision thresholds, not `[GT]` gameplay constants.

## 5. Measurement procedure

1. Land the W12 evidence repair first.
2. Rebase PR #412 onto that repaired `main`.
3. Fix the recovered `_tackleCooldown` P2 and add a regression lock before measurement.
4. Run the normal W6 targeted/affected suites.
5. Run W12 through the canonical `.github/workflows/measure.yml` `w12-gate-firing` lane from the stable PR/head commit. Do not substitute an ad-hoc temporary workflow as the authoritative post-W6 record.
6. Re-run the existing W2 armed evidence on the same W6 head while keeping the production shipping radius disabled unless/until the governed activation decision is separately passed.
7. Record all six non-`InPossession` exit shares, the W2 governed `sim_match_engine_inposs_gate` results, run IDs, commit SHA, artifact digests, and exact instrument-output hashes.

## 6. Decision boundary

- **W6 subsystem readiness** requires its functional/regression suite to pass and the known cooldown P2 to be closed.
- **W2 activation readiness** additionally requires P-W6-1 to pass. If F-W6-1 fires, W2 remains disabled regardless of W6's local tests.
- P-W6-2 is diagnostic only. Its falsification does not make W6 incorrect; it means the W12 `InvariantRejected` wall has another cause and should be investigated separately.
- No post-W6 result authorizes `[GT]` tuning while KD-W1 still applies to the affected subsystem.
