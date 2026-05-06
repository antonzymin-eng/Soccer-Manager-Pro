# Fixed64 Math Library Specification #9 — Section 4: Conversion Rules and Safety Boundaries

## 4.1 Exact Ranges (Normative)
- `FIXED64_SCALE = 2^32`.
- Exact representable integer range in `Fixed64`: **[-2,147,483,648, 2,147,483,647]**.
- `int32 -> Fixed64`: always exact.
- `int64 -> Fixed64` checked conversion MUST fail unless input is in the exact representable integer range above.
- `Fixed64 -> int32` checked conversion MUST be evaluated in two ordered steps; both MUST be performed in the order shown:
  1. **Round** `raw` to an integer-valued Q32.32 according to the configured rounding mode (`toward_zero` for the default `Fixed64ToInt32`; or `floor`/`ceil`/`nearest_even` for the suffixed variants). The result is a Fixed64 whose fractional bits are zero.
  2. **Range-check** the rounded integer against `[INT32_MIN, INT32_MAX]`. Return `ERR_FIXED64_CONVERT_RANGE` if it lies outside.
  This ordering is normative because some inputs (e.g., `raw` representing `2^31 - 2^-33` ≈ INT32_MAX + 0.5) succeed under `toward_zero` but overflow under `nearest_even`. The two-step matrix:

  | Input value | toward_zero | floor | ceil | nearest_even |
  |---|---|---|---|---|
  | `INT32_MAX + 0.5` | INT32_MAX | INT32_MAX | overflow | overflow (ties to even = `2^31`) |
  | `INT32_MIN - 0.5` | INT32_MIN | overflow | INT32_MIN | overflow |
  | `INT32_MAX + 0.25` | INT32_MAX | INT32_MAX | overflow | INT32_MAX |
  | `INT32_MAX - 0.5` | INT32_MAX − 1 | INT32_MAX − 1 | INT32_MAX | INT32_MAX − 1 (ties to even) or INT32_MAX (impl-detail of which integer is even — see below) |

  For the last row: nearest-even between `INT32_MAX − 1` (even) and `INT32_MAX` (odd) rounds to the even `INT32_MAX − 1`, normatively.

## 4.2 Float↔Fixed64 Rules
- Float-to-fixed checked conversion MUST reject NaN/Inf and out-of-range values.
- Default rounding mode is nearest-even unless API suffix specifies otherwise.
- Precision-loss warnings SHOULD be surfaced in tooling for non-exact decimal inputs.
- **Cross-platform non-determinism warning (Normative).** Even with deterministic conversion-side rounding, the **input float itself can differ across platforms** due to FMA fusion, x87 80-bit intermediates, SIMD-vs-scalar codegen, denormals-as-zero modes, and compiler reordering. `Float -> Fixed64` therefore MUST NOT be used to materialize simulation state from float arithmetic that was not itself produced under a controlled deterministic-float regime. Permitted call sites: literal constants in source code, designer-tuned configuration loaded from canonical text, debug/editor tooling. Forbidden call sites: any path inside the simulation tick that computes the float input from non-trivial arithmetic. The lint policy in Appendix F covers this prohibition under rule `FX64-UNSAFE-001` when the call site is in a simulation-critical call graph.

## 4.3 Checked Conversion APIs
- Checked conversion APIs MUST return deterministic error codes:
  - `ERR_FIXED64_CONVERT_RANGE`
  - `ERR_FIXED64_CONVERT_NONFINITE`

## 4.4 Unsafe Conversion APIs (MUST)
- Unsafe entry points MUST be prefixed `Unchecked`.
- Call sites MUST include explicit annotation/comments justifying safety.
- Code review policy MUST treat new unsafe call sites as high-risk changes.

## 4.5 Canonical Parsing Grammar (Normative)
Accepted grammar (locale-invariant ASCII):
```text
number   := sign? int frac?
sign     := "+" | "-"
int      := "0" | digit1_9 digit*
frac     := "." digit{1,12}
digit    := "0".."9"
digit1_9 := "1".."9"
```
The leading `sign` is **optional on input**; the parser MUST treat its absence as equivalent to `+`. This intentional asymmetry with the formatter (§4.6, which always emits an explicit sign) preserves backward compatibility with hand-written test data while still guaranteeing canonical-form output.

Rejections (MUST):
- exponent notation (`1e3`), grouping separators (`1,000`), leading/trailing spaces, locale decimal commas, `nan`, `inf`, `_` separators.

## 4.6 Formatting Contract
- Canonical format: `sign integer "." frac` where `sign` is **always present**: `+` for non-negative values (including zero), `-` for negative values.
- Integer part: one or more decimal digits, no leading zero unless the integer part is exactly `0`.
- Fractional part: 1 to 10 decimal digits, with trailing fractional zeros trimmed but at least one fractional digit retained (so `1` formats as `+1.0`, not `+1.`).
- Zero MUST format as `+0.0`.
- Round-trip uniqueness: 10 fractional decimal digits provide ~5e-11 precision, finer than the Fixed64 quantum (~2.328e-10), so every distinct Fixed64 value formats to a distinct canonical string and parses back to the same raw value under nearest-even input rounding.
- Golden-data format MUST be locale-invariant UTF-8 with `.` decimal separator.

## 4.7 Conversion Failure Behavior Matrix
| Conversion | Checked | Saturating | Unchecked |
|---|---|---|---|
| `int64 -> Fixed64` out of range | `ERR_FIXED64_CONVERT_RANGE` | clamp | truncate/wrap allowed |
| `float -> Fixed64` NaN/Inf | `ERR_FIXED64_CONVERT_NONFINITE` | `ERR_FIXED64_CONVERT_NONFINITE` | implementation-defined |
| `float -> Fixed64` finite out-of-range | `ERR_FIXED64_CONVERT_RANGE` | clamp | implementation-defined |
| `Fixed64 -> int32` out-of-range | `ERR_FIXED64_CONVERT_RANGE` | clamp | truncate/wrap allowed |

## 4.8 Version History
- v0.3 (2026-05-06): Made `Fixed64 -> int32` two-step (round, then range-check) with normative ordering and worked-example matrix; added cross-platform non-determinism warning to §4.2; clarified §4.5/§4.6 sign-prefix rule (optional on parse, required on format) and round-trip uniqueness statement.
- v0.2 (2026-05-01): Added exact range constants, lexical grammar, and failure matrix.
- v0.1 (2026-05-01): Initial draft aligned to outline Section 4.
