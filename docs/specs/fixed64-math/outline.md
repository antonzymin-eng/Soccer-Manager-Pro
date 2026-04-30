# Fixed64 Math Library Specification #9 — Expanded Outline

## Purpose
Provide a deterministic, high-performance fixed-point math standard (`Fixed64`) for all simulation-critical gameplay systems so that identical inputs produce identical outputs across platforms and runs.

## Scope
This specification defines representation, arithmetic semantics, deterministic utility functions, type-conversion boundaries, performance constraints, and validation strategy for the Fixed64 library.

## Normative Terms
- **MUST**: mandatory requirement for compliance.
- **SHOULD**: recommended unless a documented exception is approved.
- **MAY**: optional behavior requiring explicit opt-in.

## Non-Goals
- Replacing all non-simulation floating-point usage (e.g., editor-only tooling, offline analytics).
- Defining gameplay tuning values for specific mechanics (those belong to subsystem specs).
- Supporting arbitrary-precision or decimal-precision finance-style workloads.

## Audience
- Engine/runtime programmers implementing Fixed64 primitives.
- Gameplay/AI/physics developers consuming deterministic numeric APIs.
- QA/automation engineers validating determinism and regression integrity.

## Deliverables
- Normative behavior specification for all public Fixed64 APIs.
- Golden vector corpus and deterministic cross-platform validation harness.
- Performance budget targets and measurement methodology.

## Section Plan

### Section 1 — Fixed64 Representation and Numeric Domain
1.1 Canonical type definition: signed 64-bit **Q32.32** fixed-point representation (MUST).  
1.2 Scaling factor: `1 << 32` and unit conversion formulae (MUST).  
1.3 Numeric range constants (MUST): min = `-2,147,483,648.0000000000`, max = `+2,147,483,647.9999999998`, quantum/epsilon = `1/2^32` (`0.00000000023283064365`).  
1.4 Special-value policy: no NaN/Inf sentinels in runtime representation (MUST).  
1.5 Overflow/underflow behavior hierarchy (MUST):
- default APIs: checked with deterministic error/trap policy,
- explicit `Saturating*` APIs: clamp to min/max,
- explicit `Unchecked*` APIs: no checks, caller-owned safety.
1.6 Serialization and byte-order requirements for deterministic persistence/replay (MUST include endianness and schema versioning).

### Section 2 — Core Arithmetic and Comparison Semantics
2.1 Addition/subtraction rules with exact overflow detection semantics.  
2.2 Multiplication semantics: widening strategy, rescaling, and overflow boundaries.  
2.3 Division semantics matrix (MUST): `CheckedDiv` returns deterministic error code `ERR_FIXED64_DIV_ZERO`; `SaturatingDiv` clamps to signed min/max by numerator sign when denominator is zero; `UncheckedDiv` has undefined behavior and MUST be prohibited in simulation code paths by lint rule.  
2.4 Global rounding policy table (MUST): toward-zero, floor, ceil, nearest-even with tie-breaking definitions and ownership by API.  
2.5 Unary/helpers (`abs`, `negate`, `clamp`, `min`, `max`) and edge-case behavior (e.g., min-value negate).  
2.6 Comparison semantics (`==`, ordering, total-order guarantees) and canonical equivalence rules.  
2.7 Algebraic-consistency guarantees and approved deviations from real arithmetic.

### Section 3 — Deterministic Utility Math
3.1 Deterministic square-root algorithm and convergence bounds.  
3.2 Trigonometric API surface (frozen): `sin`, `cos`, and `atan2` are required; `tan` is excluded from v1 and may be added only via versioned additive API proposal.  
3.3 Error budgets by function with normative max-error thresholds and domains.  
3.4 Interpolation helpers (`lerp`, `invlerp`, `remap`, optional `smoothstep`) and clamping policy.  
3.5 Deterministic helper interactions used by physics/AI hot paths.  
3.6 Prohibited nondeterministic dependencies (platform libm, locale-sensitive parsing, unstable intrinsics).

### Section 4 — Conversion Rules and Safety Boundaries
4.1 Int↔Fixed64 conversion boundaries with exact safe ranges.  
4.2 Float↔Fixed64 conversion rules with deterministic rounding and precision-loss disclosure.  
4.3 Checked conversion API behavior and error surfaces.  
4.4 Unsafe conversion APIs (MUST): naming conventions, required caller annotations, and code-review gate requirements.  
4.5 Parsing/formatting contracts for debug output and golden-data interoperability.  
4.6 Conversion failure behavior matrix (checked/saturating/unchecked) with deterministic outcomes.

### Section 5 — Performance and Allocation Constraints
5.1 Quantitative budgets (MUST): add/sub/compare ≤ 3 ns/op, mul ≤ 6 ns/op, div ≤ 12 ns/op, sqrt ≤ 25 ns/op on reference benchmark host; no regression > 5% without approved waiver.  
5.2 Allocation policy: zero heap allocations/op for primitives and utility math (hard gate).  
5.3 Inlining/vectorization expectations and forbidden hidden costs (boxing, virtual dispatch).  
5.4 Thread-safety/reentrancy rules for static helpers and lookup tables.  
5.5 Benchmark methodology with fixed scenes, warmup policy, and required run counts.  
5.6 Performance acceptance criteria and regression threshold policy for CI gates.

### Section 6 — Golden Test Vectors
6.1 Golden vector schema (inputs, op, expected output, rounding mode, flags, schema version).  
6.2 Baseline arithmetic/comparison vectors and edge-case coverage minimums.  
6.3 Boundary vectors for min/max, sign transitions, overflow edges, and divide-by-zero outcomes.  
6.4 Utility vectors for sqrt/trig/interpolation including error-envelope assertions.  
6.5 Conversion vectors across int/float/Fixed64 plus unsafe-path examples.  
6.6 Vector provenance policy: canonical generator, fixed seeds, and reproducible generation command.  
6.7 Change-control/versioning policy and backward-compat expectations for vector updates.

### Section 7 — Cross-Platform Determinism Validation Harness
7.1 Platform matrix (OS, architecture, compiler/runtime versions, optimization flags, CPU feature flags).  
7.2 Harness architecture (runner, vector executor, digest/checksum comparison).  
7.3 Pass/fail criteria: exact equality for core arithmetic; bounded envelopes only where explicitly approved (e.g., approximations).  
7.4 Drift thresholds, divergence triage workflow, and required forensic artifacts.  
7.5 CI integration and gating policy with release-blocking criteria.  
7.6 Determinism incident process: ownership, SLA, and rollback/mitigation steps.

### Section 8 — Integration, API Surface, and Rollout Guidance
8.1 Module ownership boundaries and where Fixed64 usage is mandatory.  
8.2 Public API naming/package conventions (`Checked*`, `Saturating*`, `Unchecked*`) and compatibility policy (semver + explicit deprecation window of two minor releases).  
8.3 Migration playbook for legacy float paths (shadow-mode, dual-path assertions, phased cutover).  
8.4 Interop contracts with physics, collision, AI, replay, and serialization systems.  
8.5 Reference implementation maturity states and rollout gates.  
8.6 Documentation and onboarding requirements for downstream teams.

### Section 9 — Approval Checklist
9.1 Representation/scaling/range constants finalized and signed off.  
9.2 Arithmetic, rounding, and divide-by-zero matrices approved by engine + gameplay owners.  
9.3 Utility error envelopes validated with signed benchmark/vector evidence IDs.  
9.4 Conversion safe/unsafe paths audited with explicit owning teams.  
9.5 Quantitative performance budgets met in CI benchmarks.  
9.6 Determinism harness passes across full matrix with no unresolved drift tickets.  
9.7 Rollout gates complete and reference implementation status marked release-ready.

## Risk Closure Notes
- Decision freeze complete for representation format, trig API v1 scope, divide-by-zero behavior matrix, and API compatibility policy.  
- Quantitative performance thresholds defined for CI gating and waiver handling.  
- Appendix artifacts are release-blocking for implementation kickoff (constants table + failure matrix + vector schema).

## Appendices
- Appendix A — Exact constants table (Q format, min/max, epsilon, scale factors) with machine-readable source file reference.  
- Appendix B — Error-bound derivations and approximation method notes.  
- Appendix C — Performance benchmark templates and sample dashboards.  
- Appendix D — Golden vector schema, generator reference, and sample records.  
- Appendix E — Failure behavior matrix (operation × mode × outcome).  
- Appendix F — Determinism incident report template and troubleshooting guide.
