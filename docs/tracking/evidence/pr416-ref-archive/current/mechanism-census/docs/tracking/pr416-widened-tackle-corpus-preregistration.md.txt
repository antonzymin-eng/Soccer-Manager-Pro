# PR #416 widened tackle corpus preregistration

**Date:** 2026-09-17
**Scope:** evidence-only follow-up for PR #416 ERR-001-006 regression localization.

## Purpose

The current composed tackle lock uses two seeds × 150,000 ticks and its positive occurrence
assertions can clear on a single clean win and a single observed dispossession. This preregistration
widens the evidence before any production correction is proposed.

## Frozen corpus

Reuse the pre-existing W2 tackle diagnostic population from
`src/match-engine/tests/TackleIntentDiagnosticTests.cs`:

- `0x0F1E2D3C4B5A6978`
- `0x00000000D1A6D05E`
- `0x5EED000000000003`

Run each seed for `324000` ticks (one full 90-minute match). No seed may be added, removed, or
replaced after observing this run.

## Frozen arms

1. pinned main: `1bad655f5826070d1e29f54a845cdf2c549f66bc`;
2. PR #416 control: `07dcc670fed1ecc6aa32e9711d4bc8830c0ec2ad`;
3. PR #416 with exactly the two localized Rolling changes removed:
   - restore the pre-#416 `BallStateMachine.Rolling` low-speed-before-height order;
   - remove the ERR-001-006 pre-force `Stationary || Rolling` normalization from
     `BallPhysicsCore`.

The third arm is diagnostic evidence, not a proposed shipping patch. It intentionally leaves the
other ERR-001-006 production clauses unchanged.

## Measurements

For every seed and arm record:

- tackle outcomes: Won / Loose / Foul / Missed;
- observed tackle dispossessions using the same landed-challenge + holder-change predicate as
  `MatchEngineTackleTests`;
- tackle gate anatomy;
- controlled ticks by holder;
- longest continuous Controlled run and its action/executor state.

## Interpretation frozen before observation

The widened corpus is still considered under-powered for a production decision if **either pinned
main or the localized pair arm** produces fewer than:

- **3 clean wins**, or
- **3 observed tackle dispossessions**

pooled across the three full matches.

These are evidence sufficiency floors only, not football calibration targets and not permanent CI
thresholds. The pair arm must also continue to remove the seed-A long-Controlled lock rather than
merely crossing the existing >0 assertions.

The foul-share test's existing `connected > 30` Assume is not an acceptance requirement here.
Pinned-main per-test behavior is the reference for regression acceptance; calibration of foul/card
rates remains out of scope.
