# Fixed64 Math Library Specification #9 — Section 5: Performance and Allocation Constraints

## 5.1 Quantitative Budgets (MUST)
- Reference-host budgets:
  - add/sub/compare ≤ 3 ns/op,
  - mul ≤ 6 ns/op,
  - div ≤ 12 ns/op,
  - sqrt ≤ 25 ns/op.
- Regressions >5% MUST require approved waiver.

## 5.2 Reference Benchmark Host (Pinned)
- CPU: AMD Ryzen 9 7950X (fixed 4.50 GHz all-core, SMT on).
- RAM: DDR5-6000, 64GB.
- OS: Ubuntu 24.04 LTS, kernel 6.8.x.
- Compiler: clang 18.1.x with `-O3 -fno-fast-math -fno-unsafe-math-optimizations`.
- Governor: `performance`; turbo variation disabled for benchmark job.
- Isolation: dedicated runner; no co-scheduled workloads.

Equivalent hosts MAY be used for local profiling, but CI gate decisions MUST use this pinned profile (or an explicitly versioned successor profile).

## 5.3 Allocation Policy
- Primitive and utility operations MUST perform zero heap allocations per operation.
- This requirement applies to runtime library code only; harness/reporting tooling is out-of-scope.

## 5.4 Benchmark Methodology
- Warmup: 3 iterations.
- Measurement: minimum 20 iterations, minimum 1e6 operations/iteration.
- Acceptance uses median ns/op and coefficient of variation (CV).
- CV MUST be ≤ 3%; otherwise result is inconclusive and benchmark reruns.

## 5.5 CI Acceptance Criteria
- CI gates compare against pinned baselines.
- Fail conditions:
  1. median regression > 5% on any required op,
  2. CV > 3% after one retry,
  3. missing environment metadata artifact.

## 5.6 Version History
- v0.2 (2026-05-01): Added pinned host profile and statistical pass/fail criteria.
- v0.1 (2026-05-01): Initial draft aligned to outline Section 5.
