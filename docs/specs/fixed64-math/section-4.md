# Fixed64 Math Library Specification #9 — Section 4: Conversion Rules and Safety Boundaries

## 4.1 Exact Ranges (Normative)
- `FIXED64_SCALE = 2^32`.
- Exact representable integer range in `Fixed64`: **[-2,147,483,648, 2,147,483,647]**.
- `int32 -> Fixed64`: always exact.
- `int64 -> Fixed64` checked conversion MUST fail unless input is in the exact representable integer range above.
- `Fixed64 -> int32` checked conversion MUST fail unless `raw` is within `[-2^31<<32, (2^31-1)<<32]` and fractional bits are handled per rounding mode.

## 4.2 Float↔Fixed64 Rules
- Float-to-fixed checked conversion MUST reject NaN/Inf and out-of-range values.
- Default rounding mode is nearest-even unless API suffix specifies otherwise.
- Precision-loss warnings SHOULD be surfaced in tooling for non-exact decimal inputs.

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
Rejections (MUST):
- exponent notation (`1e3`), grouping separators (`1,000`), leading/trailing spaces, locale decimal commas, `nan`, `inf`, `_` separators.

## 4.6 Formatting Contract
- Canonical format: `[+|-]d+.d{1,10}` with trailing fractional zeros trimmed, leaving at least one fractional digit.
- Zero MUST format as `+0.0`.
- Golden-data format MUST be locale-invariant UTF-8 with `.` decimal separator.

## 4.7 Conversion Failure Behavior Matrix
| Conversion | Checked | Saturating | Unchecked |
|---|---|---|---|
| `int64 -> Fixed64` out of range | `ERR_FIXED64_CONVERT_RANGE` | clamp | truncate/wrap allowed |
| `float -> Fixed64` NaN/Inf | `ERR_FIXED64_CONVERT_NONFINITE` | `ERR_FIXED64_CONVERT_NONFINITE` | implementation-defined |
| `float -> Fixed64` finite out-of-range | `ERR_FIXED64_CONVERT_RANGE` | clamp | implementation-defined |
| `Fixed64 -> int32` out-of-range | `ERR_FIXED64_CONVERT_RANGE` | clamp | truncate/wrap allowed |

## 4.8 Version History
- v0.2 (2026-05-01): Added exact range constants, lexical grammar, and failure matrix.
- v0.1 (2026-05-01): Initial draft aligned to outline Section 4.
