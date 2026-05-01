# Fixed64 Math Library Specification #9 — Section 5: Performance and Allocation Constraints

## 5.1 Quantitative Budgets (MUST)
- Reference-host budgets:
  - add/sub/compare ≤ 3 ns/op,
  - mul ≤ 6 ns/op,
  - div ≤ 12 ns/op,
  - sqrt ≤ 25 ns/op.
- Regressions >5% MUST require approved waiver.

## 5.2 Allocation Policy
- Primitive and utility operations MUST perform zero heap allocations per operation.

## 5.3 Inlining and Hidden-Cost Rules
- APIs SHOULD be inlinable where hot.
- Hidden boxing, virtual dispatch, or implicit dynamic allocation in hot paths is forbidden.

## 5.4 Thread Safety and Reentrancy
- Static helpers and lookup tables MUST be immutable and thread-safe.

## 5.5 Benchmark Methodology
- Benchmark suite MUST define fixed workloads, warmup policy, and minimum run counts.
- Results MUST include variance and environment metadata.

## 5.6 CI Acceptance Criteria
- CI gates MUST compare against pinned baselines and fail on budget breaches without waiver metadata.

## 5.7 Version History
- v0.1 (2026-05-01): Initial draft aligned to outline Section 5.
