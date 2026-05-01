# Fixed64 Math Library Specification #9 — Section 3: Deterministic Utility Math

## 3.1 Square Root
- `sqrt(x)` MUST reject negative inputs with `ERR_FIXED64_DOMAIN` in checked mode.
- Implementation MUST use deterministic iteration (e.g., bounded Newton-Raphson with fixed iteration cap).
- Convergence bounds and worst-case error envelope MUST be published in golden vectors.

## 3.2 Trigonometric API Surface (Frozen v1)
- Required functions: `sin`, `cos`, `atan2`.
- `tan` is excluded from v1 and MAY only be added via versioned additive proposal.

## 3.3 Error Budgets
- Each utility function MUST declare:
  - input domain,
  - max absolute/relative error,
  - rounding mode.
- CI validation MUST fail on envelope violations.

## 3.4 Interpolation Helpers
- Required helpers: `lerp`, `invlerp`, `remap`.
- Optional helper: `smoothstep`.
- Clamping behavior MUST be explicit per API and never inferred by build configuration.

## 3.5 Hot-Path Interaction Rules
- Utility functions used in physics/AI loops MUST be allocation-free and deterministic under identical inputs.
- Lookup tables, if used, MUST be immutable and version-pinned.

## 3.6 Prohibited Dependencies
- Platform `libm`, locale-sensitive parsing, and unstable hardware intrinsics MUST NOT be required for deterministic results.

## 3.7 Version History
- v0.1 (2026-05-01): Initial draft aligned to outline Section 3.
