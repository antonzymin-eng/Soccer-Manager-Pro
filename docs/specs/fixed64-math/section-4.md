# Fixed64 Math Library Specification #9 — Section 4: Conversion Rules and Safety Boundaries

## 4.1 Int↔Fixed64 Boundaries
- `int32 -> Fixed64` is always exact.
- `int64 -> Fixed64` checked conversion MUST fail when magnitude exceeds representable integer range of Q32.32.
- `Fixed64 -> int*` conversions MUST specify rounding mode and overflow handling.

## 4.2 Float↔Fixed64 Rules
- Float-to-fixed checked conversion MUST reject NaN/Inf and out-of-range values.
- Rounding mode MUST be explicit and deterministic.
- Precision-loss warnings SHOULD be surfaced in tooling for non-exact decimal inputs.

## 4.3 Checked Conversion APIs
- Checked conversion APIs MUST return deterministic error codes (`ERR_FIXED64_CONVERT_RANGE`, `ERR_FIXED64_CONVERT_NONFINITE`).

## 4.4 Unsafe Conversion APIs (MUST)
- Unsafe entry points MUST be prefixed `Unchecked`.
- Call sites MUST include explicit annotation/comments justifying safety.
- Code review policy MUST treat new unsafe call sites as high-risk changes.

## 4.5 Parsing/Formatting Contracts
- Canonical debug format SHOULD be fixed decimal with explicit sign and bounded fractional digits.
- Golden-data format MUST be locale-invariant.

## 4.6 Conversion Failure Behavior Matrix
- All conversions MUST publish checked/saturating/unchecked behavior and deterministic outcome for each failure class.

## 4.7 Version History
- v0.1 (2026-05-01): Initial draft aligned to outline Section 4.
