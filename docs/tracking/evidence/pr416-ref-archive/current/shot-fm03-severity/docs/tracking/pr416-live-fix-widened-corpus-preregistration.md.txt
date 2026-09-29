# PR #416 live-fix widened tackle corpus preregistration

**Date:** 2026-09-17
**Status:** preregistered before widened observation
**Scope:** evidence-only validation of the current PR #416 correction.

## Why this supersedes the earlier widening attempt

PR #416 advanced from diagnostic checkpoint `07dcc670fed1ecc6aa32e9711d4bc8830c0ec2ad`
to live head `a7f2b77af61c1d0b7138bfdfce21cd1772c91f26` before the earlier
Rolling/pre-force widening produced a valid simulation result. That old widening is therefore
not release evidence for the live fix.

The live correction preserves ERR-001-006 and fixes the absorbing HOLD trajectory in perception.
A candidate follow-up changes Shot FM-03 diagnostic severity only; it does not change gameplay
state or trajectory.

## Frozen corpus

Reuse the pre-existing W2 diagnostic population from
`src/match-engine/tests/TackleIntentDiagnosticTests.cs`:

- `0x0F1E2D3C4B5A6978`
- `0x00000000D1A6D05E`
- `0x5EED000000000003`

Each seed runs `324000` ticks (one 90-minute match). No seed may be added, removed, or replaced
after observing the run.

## Frozen arms

1. pinned main: `1bad655f5826070d1e29f54a845cdf2c549f66bc`;
2. live-fix candidate: `54d2388c454f7f4fdc6a2afeaab5bfe2dd41fb3e`,
   which is PR head `a7f2b77...` plus only the behavior-neutral Shot FM-03
   `LogError -> LogWarning` classification correction.

## Frozen measurements

For every seed and arm record:

- Won / Loose / Foul / Missed;
- observed tackle dispossessions using the production fixture's landed-challenge + holder-change
  predicate;
- tackle gate anatomy;
- Controlled ticks by holder;
- longest continuous Controlled run and selected action/executor state.

## Sufficiency decision frozen before observation

The live-fix corpus remains under-powered for the positive occurrence claims if it produces fewer
than **3 clean wins** or fewer than **3 observed tackle dispossessions**, pooled across the three
full matches.

These are evidence-sufficiency floors, not gameplay calibration targets and not permanent CI
thresholds. The live fix must also avoid any terminal/prolonged Controlled-HOLD attractor of the
kind observed at `07dcc67` (97,682 ticks on seed A).

The foul-share test's existing `connected > 30` assumption is not used as a correctness bar.
Pinned-main test outcome is the regression reference. No foul/card or tackle-rate calibration is
authorized by this measurement.
