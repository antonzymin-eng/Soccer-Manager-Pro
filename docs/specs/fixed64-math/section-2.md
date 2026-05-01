# Fixed64 Math Library Specification #9 — Section 2: Core Arithmetic and Comparison Semantics

## 2.1 API Behavior Matrix (Normative)
| API | Default rounding | Tie rule | Checked failure(s) | Saturating behavior | Unchecked behavior |
|---|---|---|---|---|---|
| `Add` / `Sub` | exact (none) | n/a | `ERR_FIXED64_OVERFLOW` | clamp to min/max | two's-complement wrap |
| `Mul` | nearest-even | to even LSB after `/ 2^32` | `ERR_FIXED64_OVERFLOW` | clamp to min/max | implementation raw truncation allowed |
| `Div` | nearest-even | to even LSB after `* 2^32 / b` | `ERR_FIXED64_DIV_ZERO`, `ERR_FIXED64_OVERFLOW` | div-zero clamps by numerator sign | undefined for `b==0` |
| `Sqrt` | nearest-even | to even LSB | `ERR_FIXED64_DOMAIN` | n/a | undefined for `x<0` |
| `Fixed->Int` | toward-zero (default) | n/a | `ERR_FIXED64_CONVERT_RANGE` | clamp | truncation/wrap allowed |
| `Float->Fixed` | nearest-even | to even | `ERR_FIXED64_CONVERT_NONFINITE`, `ERR_FIXED64_CONVERT_RANGE` | clamp out-of-range finite values | implementation-defined |

Any API that deviates from this table MUST encode mode in the function name (e.g., `MulFloor`, `DivTowardZero`).

## 2.2 Deterministic Failure Precedence (Normative)
When multiple failure conditions could apply, implementations MUST apply this precedence order:
1. **Invalid domain** (`ERR_FIXED64_DOMAIN`, including `sqrt(x<0)`).
2. **Divide by zero** (`ERR_FIXED64_DIV_ZERO`).
3. **Conversion non-finite** (`ERR_FIXED64_CONVERT_NONFINITE`).
4. **Overflow/underflow/range** (`ERR_FIXED64_OVERFLOW`, `ERR_FIXED64_CONVERT_RANGE`).

Example precedence cases:
- `CheckedDiv(FIXED64_MIN, 0)` => `ERR_FIXED64_DIV_ZERO` (not overflow).
- `CheckedNegate(FIXED64_MIN)` => `ERR_FIXED64_OVERFLOW`.
- `CheckedFromFloat(NaN)` => `ERR_FIXED64_CONVERT_NONFINITE`.

## 2.3 Arithmetic Algorithms (Normative Pseudocode)

### 2.3.1 `CheckedMulNearestEven(a, b)`
```text
wide = int128(a.raw) * int128(b.raw)
q = wide >> 32                   // arithmetic shift
r = abs(wide) & ((1<<32)-1)      // remainder magnitude
half = 1<<31
if r > half: q += sign(wide)
if r == half and (q & 1) != 0: q += sign(wide)
if q < INT64_MIN or q > INT64_MAX: return ERR_FIXED64_OVERFLOW
return Fixed64(raw=int64(q))
```

### 2.3.2 `CheckedDivNearestEven(a, b)`
```text
if b.raw == 0: return ERR_FIXED64_DIV_ZERO
num = int128(a.raw) << 32
den = int128(b.raw)
q = num / den                    // trunc toward zero in int math
r = abs(num % den)
half = abs(den) / 2
if r > half: q += sign(num*den)
if (abs(den) % 2 == 0) and r == half and (q & 1) != 0: q += sign(num*den)
if q < INT64_MIN or q > INT64_MAX: return ERR_FIXED64_OVERFLOW
return Fixed64(raw=int64(q))
```

## 2.4 Addition/Subtraction Semantics
- `CheckedAdd(a,b)` and `CheckedSub(a,b)` MUST detect signed 64-bit overflow before commit.
- Overflow result MUST return deterministic error `ERR_FIXED64_OVERFLOW`.
- `SaturatingAdd/Sub` MUST clamp to numeric min/max.
- `UncheckedAdd/Sub` MAY wrap per two's-complement machine behavior but is prohibited in simulation paths.

## 2.5 Unary and Helper Operations
- `abs`: checked mode MUST error on `FIXED64_MIN`.
- `negate`: checked mode MUST error on `FIXED64_MIN`.
- `clamp/min/max`: MUST be branch-deterministic and side-effect free.

## 2.6 Comparison Semantics
- Equality MUST be raw-bit equality.
- Ordering MUST follow signed raw ordering (total order guaranteed).
- `0` has exactly one encoding (`raw == 0`); no signed-zero alternate encoding exists.
- No tolerance/epsilon-based compare in core API.

## 2.7 Algebraic Consistency and Deviations
- Addition/subtraction are exact when in range.
- Multiplication/division are deterministic approximations due to rescaling and rounding.
- API docs MUST explicitly note non-associativity for mixed-scale chains caused by rounding points.

## 2.8 Version History
- v0.2 (2026-05-01): Added normative behavior matrix, precedence order, and pseudocode for mul/div rounding.
- v0.1 (2026-05-01): Initial draft aligned to outline Section 2.
