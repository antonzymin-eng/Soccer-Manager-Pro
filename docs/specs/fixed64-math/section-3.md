# Fixed64 Math Library Specification #9 — Section 3: Deterministic Utility Math

## 3.1 Square Root (Normative Algorithm)

`sqrt(x)` MUST reject negative inputs with `ERR_FIXED64_DOMAIN` in checked mode. For non-negative inputs, implementations MUST use the **integer paired-bit (digit-by-digit) algorithm** below, which produces a bit-exact result across all conforming implementations without depending on platform `libm`, FMA fusion, or floating-point intermediates.

Inputs and outputs are raw `int64` values interpreted as Q32.32. The algorithm computes `floor(sqrt(x))` in Q32.32 with explicit half-tie correction so that the result is the nearest-even Q32.32 value to the true real square root.

```text
// Input:  x.raw, an int64 with x.raw >= 0 (i.e. x >= 0).
// Output: r.raw, an int64 in Q32.32 representing nearest-even sqrt(x).

// Step 1: convert x to a Q64.0-style operand by shifting left 32. This means
// the true value being rooted is (x.raw << 32) interpreted as Q64.64, whose
// square root is in Q32.32. Use uint128 to avoid overflow.
n = uint128(x.raw) << 32                       // exact, fits in 96 bits

// Step 2: bit-by-bit integer sqrt. 64 iterations cover all 128 bits in pairs,
// extracting one Q32.32 result bit per iteration.
r = uint128(0)                                  // current result accumulator
b = uint128(1) << 126                           // current bit, walks 126 -> 0 by 2

while b > n:
    b >>= 2

while b != 0:
    if n >= r + b:
        n  -= r + b
        r   = (r >> 1) + b
    else:
        r >>= 1
    b >>= 2

// After the loop, r = floor(sqrt(x.raw << 32)) and n is the residual.
// True sqrt = r + n / (2r + 1) in real arithmetic; round half-to-even.

// Step 3: nearest-even tie-break. The half-step in Q32.32 corresponds to
// comparing (2*n) to (2*r + 1).
two_n     = n << 1
two_r_p1  = (r << 1) + 1
if two_n > two_r_p1:
    r += 1
else if two_n == two_r_p1 and (r & 1) != 0:
    r += 1

return Fixed64(raw = int64(r))                  // r fits: sqrt(2^63) < 2^32
```

Worst-case absolute error versus real-valued `sqrt`: `0.5 * EPS = 2^-33 ≈ 1.16e-10` (one half-quantum, by construction of the half-tie step). This is the published error envelope for §3.3.

## 3.2 Trigonometric API Surface (Frozen v1, Normative Algorithms)

Required functions: `sin(theta)`, `cos(theta)`, `atan2(y, x)`. `tan` is excluded from v1 and MAY only be added via versioned additive proposal.

All three functions MUST use the **CORDIC** (COordinate Rotation DIgital Computer) algorithm in rotation/vectoring mode, parameterized as follows. Two compliant implementations MUST therefore produce bit-identical raw outputs.

| Parameter | Value |
|---|---|
| Iterations (`N`) | 32 |
| Internal accumulator type | `int64` raw Q32.32 |
| Argument-reduction modulus | `2π` (using normative constant `FIXED64_TAU`, see Appendix A) |
| Quadrant folding | reduce to `[-π/4, +π/4]` via sign/quadrant lookup before the CORDIC loop |
| Per-iteration angle table | `atan(2^-i)` for `i = 0..31`, stored as Q32.32 raw constants in Appendix G's `cordic_angle_table` |
| Cumulative gain `K` | `K = ∏ cos(atan(2^-i))` over `i=0..31`, stored as Q32.32 raw constant `CORDIC_K` in Appendix G |
| Output rounding | nearest-even on the final raw value |

`sin`/`cos` consume the same CORDIC pass; both raw outputs are produced from a single rotation. `atan2(y, x)` uses CORDIC vectoring mode with the same iteration count and the same angle table.

Worst-case absolute error versus real-valued reference (the published envelope for §3.3):

| Function | Domain | Max abs error |
|---|---|---|
| `sin`, `cos` | `[-π, +π]` after reduction | `2^-30 ≈ 9.31e-10` (≈ 4 quanta) |
| `atan2` | `(y, x) ≠ (0, 0)`, full quadrant | `2^-30 ≈ 9.31e-10` |

Implementations MUST publish their CORDIC angle table and `CORDIC_K` constant verbatim from Appendix G; deviation breaks cross-platform digest comparison.

## 3.3 Error Budgets
- Each utility function MUST declare:
  - input domain,
  - max absolute/relative error vs real-valued reference,
  - rounding mode.
- Published values (anchored by §3.1 and §3.2 above):
  - `sqrt`: domain `x ≥ 0`; max abs error `2^-33`; nearest-even.
  - `sin`/`cos`: domain reduced to `[-π/4, π/4]`; max abs error `2^-30`; nearest-even on raw output.
  - `atan2`: domain `(y, x) ≠ (0, 0)`; max abs error `2^-30`; nearest-even on raw output.
- The error envelope governs **accuracy versus mathematical reality**, not cross-platform divergence. Cross-platform divergence MUST be zero (bit-exact); §7.3 governs that requirement and is satisfied because §3.1 and §3.2 are bit-exact algorithms with normative tables.
- CI validation MUST fail on envelope violations.

## 3.4 Interpolation Helpers
- Required helpers: `lerp`, `invlerp`, `remap`.
- Optional helper: `smoothstep`.
- `lerp(a, b, t)` MUST be implemented as `a + CheckedMul(CheckedSub(b, a), t)` with single nearest-even rounding at the multiply step (no double-rounding). The clamping policy MUST NOT be inferred from build configuration; if clamping is desired, use the explicit `LerpClamped` variant.
- `invlerp(a, b, x)` MUST return `ERR_FIXED64_DIV_ZERO` when `a == b` in checked mode.

## 3.5 Hot-Path Interaction Rules
- Utility functions used in physics/AI loops MUST be allocation-free and deterministic under identical inputs.
- Lookup tables (including `cordic_angle_table` and `CORDIC_K`) MUST be immutable, version-pinned, and embedded as compile-time constants — never loaded from disk at runtime.

## 3.6 Prohibited Dependencies
- Platform `libm`, locale-sensitive parsing, and unstable hardware intrinsics MUST NOT be required for deterministic results.
- FMA / fused-multiply-add intrinsics MUST NOT appear in any utility-math hot path; explicit `CheckedMul` + `CheckedAdd` ordering is required so that round-off behavior is identical across CPU vendors.

## 3.7 Version History
- v0.2 (2026-05-06): Pinned normative algorithms — integer paired-bit sqrt with half-tie correction; CORDIC N=32 for sin/cos/atan2 with named angle table and gain constant in Appendix G; quantified per-function error envelopes; clarified bit-exact vs error-envelope distinction; added FMA prohibition.
- v0.1 (2026-05-01): Initial draft aligned to outline Section 3.
