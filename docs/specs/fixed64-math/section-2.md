# Fixed64 Math Library Specification #9 — Section 2: Core Arithmetic and Comparison Semantics

## 2.1 Addition/Subtraction Semantics
- `CheckedAdd(a,b)` and `CheckedSub(a,b)` MUST detect signed 64-bit overflow before commit.
- Overflow result MUST return deterministic error `ERR_FIXED64_OVERFLOW`.
- `SaturatingAdd/Sub` MUST clamp to numeric min/max.
- `UncheckedAdd/Sub` MAY wrap per two's-complement machine behavior but is prohibited in simulation paths.

## 2.2 Multiplication Semantics
- Multiply MUST widen to signed 128-bit intermediate: `wide = (int128)a.raw * (int128)b.raw`.
- Rescale rule MUST be `result_raw = wide >> 32` with configured rounding mode where applicable.
- Checked multiply MUST detect if post-rescale value exceeds signed 64-bit range.

## 2.3 Division Semantics Matrix (MUST)
- `CheckedDiv(a,b)`:
  - if `b == 0`, return `ERR_FIXED64_DIV_ZERO`.
  - else compute `(a.raw << 32) / b.raw` in widened precision with deterministic rounding.
- `SaturatingDiv(a,b)`:
  - if `b == 0`, clamp to max for non-negative numerator, min for negative numerator.
- `UncheckedDiv(a,b)`:
  - behavior undefined for `b == 0`; MUST be lint-banned in simulation code.

## 2.4 Global Rounding Policy Table (MUST)
- Library MUST support explicit rounding modes:
  - toward-zero,
  - floor,
  - ceil,
  - nearest-even.
- Each public API MUST declare its owned rounding mode; no implicit platform-dependent rounding is allowed.

## 2.5 Unary and Helper Operations
- `abs`: checked mode MUST error on `FIXED64_MIN`.
- `negate`: checked mode MUST error on `FIXED64_MIN`.
- `clamp/min/max`: MUST be branch-deterministic and side-effect free.

## 2.6 Comparison Semantics
- Equality MUST be raw-bit equality.
- Ordering MUST follow signed raw ordering (total order guaranteed).
- No tolerance/epsilon-based compare in core API.

## 2.7 Algebraic Consistency and Deviations
- Addition/subtraction are exact when in range.
- Multiplication/division are deterministic approximations due to rescaling and rounding.
- API docs MUST explicitly note non-associativity for mixed-scale chains caused by rounding points.

## 2.8 Version History
- v0.1 (2026-05-01): Initial draft aligned to outline Section 2.
