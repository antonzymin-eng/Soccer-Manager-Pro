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

The pseudocode below is **normative**. Implementations MUST produce bit-exact equivalent results across platforms, in particular for negative-operand corner cases. Both algorithms operate on **magnitude + sign** to avoid the floor-vs-truncate ambiguity that arises from arithmetic right-shift on signed negatives. All intermediate `int128`/`uint128` arithmetic MUST be exact (no overflow) under the documented input domain.

### 2.3.1 `CheckedMulNearestEven(a, b)`
```text
// Magnitude-then-sign formulation. Required because arithmetic right-shift
// of a signed negative widened product gives a floor-divided quotient whose
// remainder is NOT abs(wide) & ((1<<32) - 1); the two disagree whenever
// wide is negative and the low 32 bits are non-zero.

sign_neg = (a.raw < 0) XOR (b.raw < 0)            // true if result is negative
ua = uint128(a.raw >= 0 ? int128(a.raw) : -int128(a.raw))    // |a.raw| as uint128
ub = uint128(b.raw >= 0 ? int128(b.raw) : -int128(b.raw))    // |b.raw| as uint128
mag  = ua * ub                                    // exact non-negative product
q    = mag >> 32                                  // floor on unsigned == truncate
r    = mag & ((uint128(1) << 32) - 1)             // exact remainder in [0, 2^32)
half = uint128(1) << 31

// Banker's rounding on the magnitude.
if r > half:
    q += 1
else if r == half and (q & 1) != 0:
    q += 1

// Apply sign and check fit in signed 64-bit.
if sign_neg:
    if q > uint128(1) << 63: return ERR_FIXED64_OVERFLOW   // |result| > 2^63
    if q == uint128(1) << 63: return Fixed64(raw=INT64_MIN) // exact INT64_MIN
    return Fixed64(raw = -int64(q))
else:
    if q > uint128(INT64_MAX): return ERR_FIXED64_OVERFLOW
    return Fixed64(raw = int64(q))
```

### 2.3.2 `CheckedDivNearestEven(a, b)`
```text
if b.raw == 0: return ERR_FIXED64_DIV_ZERO

sign_neg = (a.raw < 0) XOR (b.raw < 0)
num = uint128(a.raw >= 0 ? int128(a.raw) : -int128(a.raw)) << 32   // |a.raw| * 2^32
den = uint128(b.raw >= 0 ? int128(b.raw) : -int128(b.raw))         // |b.raw|
q   = num / den                                   // unsigned floor == truncate
r   = num - q * den                               // exact remainder in [0, den)

// Banker's rounding on the magnitude. Tie-detection requires the doubled
// remainder to equal the denominator exactly, which is only possible when
// den is even (so that den/2 is an integer that r can equal).
two_r = r << 1
if two_r > den:
    q += 1
else if two_r == den and (q & 1) != 0:
    q += 1

// Apply sign and check fit in signed 64-bit. INT64_MAX / -1 etc. trip here.
if sign_neg:
    if q > uint128(1) << 63: return ERR_FIXED64_OVERFLOW
    if q == uint128(1) << 63: return Fixed64(raw=INT64_MIN)
    return Fixed64(raw = -int64(q))
else:
    if q > uint128(INT64_MAX): return ERR_FIXED64_OVERFLOW
    return Fixed64(raw = int64(q))
```

Note on tie detection: comparing `two_r` to `den` (rather than `r` to `den/2`) avoids an integer-division step and removes the prior `(abs(den) % 2 == 0)` parity guard — `two_r == den` already implies `den` is even.

## 2.4 Addition/Subtraction Semantics
- `CheckedAdd(a,b)` and `CheckedSub(a,b)` MUST detect signed 64-bit overflow before commit.
- Overflow result MUST return deterministic error `ERR_FIXED64_OVERFLOW`.
- `SaturatingAdd/Sub` MUST clamp to `FIXED64_MAX` when the true mathematical result exceeds the representable maximum, and to `FIXED64_MIN` when it falls below the representable minimum. The clamp side is determined by the **sign of the true result**, not by the sign of either operand alone (e.g., `SaturatingSub(FIXED64_MIN, 1)` clamps to `FIXED64_MIN`).
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

## 2.8 Operator Overload Semantics (Normative)

When the implementation language provides operator overloading (the C# reference implementation MUST), each operator below MUST bind to the named `Checked*` API and inherit its rounding rule, failure precedence, and deterministic-error contract. Operator overloads MUST NOT bind to `Saturating*` or `Unchecked*` semantics; those families remain accessible only via their named methods.

| Operator | Binds to | Failure transport |
|---|---|---|
| `a + b` | `CheckedAdd(a, b)` | throws `Fixed64ArithmeticException(ERR_FIXED64_OVERFLOW)` on overflow |
| `a - b` | `CheckedSub(a, b)` | throws on overflow |
| `a * b` | `CheckedMulNearestEven(a, b)` | throws on overflow |
| `a / b` | `CheckedDivNearestEven(a, b)` | throws on `ERR_FIXED64_DIV_ZERO` or `ERR_FIXED64_OVERFLOW` |
| `-a` | `CheckedNegate(a)` | throws on `FIXED64_MIN` |
| `a == b` / `a != b` | raw-bit equality (§2.6) | total |
| `a < b`, `a <= b`, `a > b`, `a >= b` | signed raw ordering (§2.6) | total |

Rationale: gameplay/physics code paths overwhelmingly reach for `+ - * /` first; binding them to the safe, error-surfacing family aligns the operator surface with §1.5's "default APIs MUST map to checked behavior" rule. Per-operator escape hatches MUST be named methods (`SaturatingMul`, `UncheckedMul`, etc.) so that the lint policy in Appendix F can target unsafe call sites by symbol.

The exception type `Fixed64ArithmeticException` MUST carry the failing error code from Appendix B as a stable numeric field; harnesses and golden vectors compare on the code, not the message text.

## 2.9 Version History
- v0.3 (2026-05-06): Rewrote §2.3.1 (mul) and §2.3.2 (div) using magnitude+sign formulation to eliminate negative-operand rounding bug; tightened §2.4 saturating clamp side to depend on true-result sign; added §2.8 operator overload binding table.
- v0.2 (2026-05-01): Added normative behavior matrix, precedence order, and pseudocode for mul/div rounding.
- v0.1 (2026-05-01): Initial draft aligned to outline Section 2.
