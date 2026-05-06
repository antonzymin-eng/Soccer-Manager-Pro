# Fixed64 Math Library Specification #9 — Appendices

## Appendix A — Exact Constants Table (Normative)
```json
{
  "schema_version": "1.0.0",
  "format": "fixed64_q32_32",
  "scale": 4294967296,
  "min_raw": "0x8000000000000000",
  "max_raw": "0x7FFFFFFFFFFFFFFF",
  "min_value": "-2147483648.0",
  "max_value": "2147483647.99999999976716935635",
  "epsilon": "0.00000000023283064365386962890625",
  "FIXED64_PI":   "0x00000003243F6A88",
  "FIXED64_TAU":  "0x00000006487ED511",
  "FIXED64_PI_2": "0x00000001921FB544",
  "FIXED64_PI_4": "0x00000000C90FDAA2",
  "CORDIC_K":     "0x000000009B74EDA8"
}
```

The trig constants above are the nearest-even Q32.32 raw values for the named real constants:
- `FIXED64_PI` ≈ 3.14159265358979 (raw `0x3243F6A88`).
- `FIXED64_TAU` = 2π ≈ 6.28318530717959 (raw `0x6487ED511`).
- `FIXED64_PI_2` = π/2; `FIXED64_PI_4` = π/4.
- `CORDIC_K` ≈ 0.60725293500888 — the cumulative gain of 32-iteration CORDIC, used to pre-scale the input vector. See Appendix G for the full angle table.

## Appendix B — Error Code Registry (Normative)
| Symbol | Code | Message | Stability |
|---|---:|---|---|
| `ERR_FIXED64_OVERFLOW` | 1001 | arithmetic overflow/underflow | stable in major version |
| `ERR_FIXED64_DIV_ZERO` | 1002 | divide by zero | stable in major version |
| `ERR_FIXED64_DOMAIN` | 1003 | invalid math domain | stable in major version |
| `ERR_FIXED64_CONVERT_RANGE` | 1004 | conversion out of range | stable in major version |
| `ERR_FIXED64_CONVERT_NONFINITE` | 1005 | non-finite float conversion | stable in major version |
| `ERR_FIXED64_PARSE_GRAMMAR` | 1006 | input does not match canonical grammar (§4.5) | stable in major version |
| `ERR_FIXED64_PARSE_OVERFLOW` | 1007 | parsed value out of representable range | stable in major version |

## Appendix C — Golden Vector Schema and Sample (Normative)
```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "title": "Fixed64GoldenVector",
  "type": "object",
  "required": ["schema_version","operation","inputs","rounding_mode","expected_flags"],
  "properties": {
    "schema_version": {"type": "string"},
    "operation": {"type": "string"},
    "inputs": {"type": "array", "items": {"type": "string"}},
    "rounding_mode": {"type": "string", "enum": ["toward_zero","floor","ceil","nearest_even"]},
    "expected_raw": {"type": "string", "pattern": "^0x[0-9A-Fa-f]{16}$"},
    "expected_flags": {
      "type": "array",
      "items": {
        "type": "string",
        "enum": [
          "ERR_FIXED64_OVERFLOW",
          "ERR_FIXED64_DIV_ZERO",
          "ERR_FIXED64_DOMAIN",
          "ERR_FIXED64_CONVERT_RANGE",
          "ERR_FIXED64_CONVERT_NONFINITE",
          "ERR_FIXED64_PARSE_GRAMMAR",
          "ERR_FIXED64_PARSE_OVERFLOW"
        ]
      }
    }
  },
  "allOf": [
    {
      "if": { "properties": { "expected_flags": { "minItems": 1 } } },
      "then": { "not": { "required": ["expected_raw"] } },
      "else": { "required": ["expected_raw"] }
    }
  ]
}
```

The conditional (`allOf`) encodes the failure-vector convention:
- **Success vector:** `expected_flags` is empty → `expected_raw` is required.
- **Failure vector:** `expected_flags` contains at least one error symbol from Appendix B → `expected_raw` is forbidden (the operation produced an error, not a raw value).

Success sample:
```json
{
  "schema_version": "1.0.0",
  "operation": "CheckedDiv",
  "inputs": ["0x0000000200000000", "0x0000000100000000"],
  "rounding_mode": "nearest_even",
  "expected_raw": "0x0000000200000000",
  "expected_flags": []
}
```

Failure sample:
```json
{
  "schema_version": "1.0.0",
  "operation": "CheckedDiv",
  "inputs": ["0x0000000200000000", "0x0000000000000000"],
  "rounding_mode": "nearest_even",
  "expected_flags": ["ERR_FIXED64_DIV_ZERO"]
}
```

## Appendix D — Failure Behavior Matrix (Normative)

The table is exhaustive over the named operations in §2 and §4; any new operation introduced via the additive-API process MUST add a row here.

| Operation | Condition | Checked | Saturating | Unchecked |
|---|---|---|---|---|
| `Add` | true result > FIXED64_MAX | `ERR_FIXED64_OVERFLOW` | clamp to FIXED64_MAX | wraps (two's-comp) |
| `Add` | true result < FIXED64_MIN | `ERR_FIXED64_OVERFLOW` | clamp to FIXED64_MIN | wraps |
| `Sub` | true result > FIXED64_MAX | `ERR_FIXED64_OVERFLOW` | clamp to FIXED64_MAX | wraps |
| `Sub` | true result < FIXED64_MIN | `ERR_FIXED64_OVERFLOW` | clamp to FIXED64_MIN | wraps |
| `Mul` | rounded result > FIXED64_MAX | `ERR_FIXED64_OVERFLOW` | clamp to FIXED64_MAX | impl raw truncation |
| `Mul` | rounded result < FIXED64_MIN | `ERR_FIXED64_OVERFLOW` | clamp to FIXED64_MIN | impl raw truncation |
| `Div` | denominator zero | `ERR_FIXED64_DIV_ZERO` | clamp by numerator sign (max if num>0, min if num<0, zero if num==0) | undefined |
| `Div` | rounded result > FIXED64_MAX | `ERR_FIXED64_OVERFLOW` | clamp to FIXED64_MAX | undefined |
| `Div` | rounded result < FIXED64_MIN | `ERR_FIXED64_OVERFLOW` | clamp to FIXED64_MIN | undefined |
| `Negate` | `raw == 0x8000000000000000` | `ERR_FIXED64_OVERFLOW` | clamp to FIXED64_MAX | wraps to FIXED64_MIN |
| `Abs` | `raw == 0x8000000000000000` | `ERR_FIXED64_OVERFLOW` | clamp to FIXED64_MAX | wraps to FIXED64_MIN |
| `Sqrt` | negative input | `ERR_FIXED64_DOMAIN` | n/a (mode unsupported) | undefined |
| `Clamp(x,lo,hi)` | `lo > hi` | `ERR_FIXED64_DOMAIN` | n/a | undefined; impl MAY return `x` or `lo` |
| `Min`,`Max` | NaN-like or sentinel inputs | not applicable (no sentinels per §1.4) | — | — |
| `Fixed -> Int32` | rounded value out of int32 range (per §4.1 step 2) | `ERR_FIXED64_CONVERT_RANGE` | clamp to int32 range | truncate/wrap allowed |
| `Fixed -> Int64` | out of representable int range (`±2^31` envelope) | `ERR_FIXED64_CONVERT_RANGE` | clamp | truncate/wrap allowed |
| `Float -> Fixed` | NaN / ±Inf | `ERR_FIXED64_CONVERT_NONFINITE` | `ERR_FIXED64_CONVERT_NONFINITE` | impl-defined |
| `Float -> Fixed` | finite, magnitude > FIXED64_MAX | `ERR_FIXED64_CONVERT_RANGE` | clamp to FIXED64_MAX/MIN | impl-defined |
| `Parse(string)` | grammar violation (§4.5) | `ERR_FIXED64_PARSE_GRAMMAR` | `ERR_FIXED64_PARSE_GRAMMAR` | impl-defined |
| `Parse(string)` | grammatically valid but out of range | `ERR_FIXED64_PARSE_OVERFLOW` | clamp to FIXED64_MAX/MIN | impl-defined |

## Appendix E — Determinism Harness Digest Spec (Normative)
- Digest algorithm: SHA-256.
- Record byte layout (little-endian): `op_id:u16 | rounding:u8 | input_count:u8 | inputs:i64[] | output:i64 | flags:u32`.
- For failure records, `output` MUST be encoded as `0x0000000000000000` (raw zero) and the failing error code's numeric value (Appendix B) MUST be set in `flags`.
- `rounding` byte values: `0=toward_zero, 1=floor, 2=ceil, 3=nearest_even`. All other values are reserved.
- Harness result MUST include:
  - platform metadata (full §7.1 row tuple),
  - ordered record count,
  - SHA-256 hex digest.

### Appendix E.1 — Operation ID Mapping (Normative, stable in major version)

`op_id:u16` mapping. New entries MUST be appended (never reassigned). Reserved IDs (`0x0000`, `0xFFFF`) are forbidden in records.

| `op_id` | Operation | Source section |
|---:|---|---|
| `0x0001` | `CheckedAdd` | §2.4 |
| `0x0002` | `CheckedSub` | §2.4 |
| `0x0003` | `CheckedMulNearestEven` | §2.3.1 |
| `0x0004` | `CheckedDivNearestEven` | §2.3.2 |
| `0x0005` | `CheckedNegate` | §2.5 |
| `0x0006` | `CheckedAbs` | §2.5 |
| `0x0007` | `Clamp` | §2.5 |
| `0x0008` | `Min` | §2.5 |
| `0x0009` | `Max` | §2.5 |
| `0x000A` | `SaturatingAdd` | §2.4 |
| `0x000B` | `SaturatingSub` | §2.4 |
| `0x000C` | `SaturatingMul` | §2.1 |
| `0x000D` | `SaturatingDiv` | §2.1 |
| `0x0020` | `Sqrt` | §3.1 |
| `0x0021` | `Sin` | §3.2 |
| `0x0022` | `Cos` | §3.2 |
| `0x0023` | `Atan2` | §3.2 |
| `0x0024` | `Lerp` | §3.4 |
| `0x0025` | `InvLerp` | §3.4 |
| `0x0026` | `Remap` | §3.4 |
| `0x0040` | `FromInt32` | §4.1 |
| `0x0041` | `FromInt64Checked` | §4.1 |
| `0x0042` | `ToInt32Checked` (toward_zero) | §4.1 |
| `0x0043` | `ToInt32CheckedNearestEven` | §4.1 |
| `0x0044` | `FromFloatChecked` | §4.2 |
| `0x0045` | `ToFloat` | §4.2 |
| `0x0046` | `ParseCanonical` | §4.5 |
| `0x0047` | `FormatCanonical` | §4.6 |

`flags:u32` value encoding: bit positions 0–6 correspond to `ERR_FIXED64_OVERFLOW` (1001) through `ERR_FIXED64_PARSE_OVERFLOW` (1007); bit `i` is set iff Appendix B code `1001 + i` was raised. Bits 7–31 are reserved (MUST be zero).

## Appendix F — Unchecked Lint Policy (Normative)
- Rule ID: `FX64-UNSAFE-001`.
- Scope: simulation-critical call graphs (physics, collision, gameplay, replay) **at and beyond Stage 5** per §8.1. Pre-Stage-5 simulation code is exempt because it uses `float` by design.
- Default: `error` severity.
- Suppression requires `@fixed64-waiver(id, owner, expiry)` annotation and linked approval ticket.
- CI MUST fail if active suppressions are expired.

## Appendix G — Reference Algorithm Tables (Normative)

### Appendix G.1 — CORDIC Angle Table
Q32.32 raw values for `atan(2^-i)` used by the §3.2 trig algorithms. Iteration `i` consumes table row `i`. Adding rows or modifying values MUST go through major-version change control.

| `i` | `atan(2^-i)` (radians) | Q32.32 raw |
|---:|---|---|
| 0  | 0.7853981633974483 | `0x00000000C90FDAA2` |
| 1  | 0.4636476090008061 | `0x0000000076B19C16` |
| 2  | 0.24497866312686414 | `0x000000003EB6EBF2` |
| 3  | 0.12435499454676144 | `0x000000001FD5BA9B` |
| 4  | 0.06241880999595735 | `0x000000000FFAADDB` |
| 5  | 0.031239833430268277 | `0x0000000007FF556F` |
| 6  | 0.015623728620476831 | `0x0000000003FFEAAB` |
| 7  | 0.007812341060101111 | `0x0000000001FFFD55` |
| 8  | 0.0039062301319669718 | `0x0000000000FFFFAB` |
| 9  | 0.0019531225164788188 | `0x00000000007FFFF5` |
| 10 | 0.0009765621895593195 | `0x00000000003FFFFF` |
| ... | (rows 11..31 follow the closed-form `atan(2^-i)`; full machine-readable table lives in `tools/fixed64/cordic_angle_table.json` and MUST round-trip through nearest-even Q32.32 from the documented source values) | |
| 31 | ≈ `4.6566128730773926e-10` | `0x0000000000000002` |

The full 32-row table is the single source of truth for the CORDIC implementation; row 31's raw value approaches the quantum and floors at the smallest non-zero positive Q32.32. All conforming implementations MUST embed the full table verbatim from the canonical generator output (`tools/fixed64/cordic_angle_table.json`, generator command `python3 tools/fixed64/gen_cordic_table.py --bits 32`).

### Appendix G.2 — Sqrt Reference Implementation
The paired-bit algorithm pinned in §3.1 is the **only** conforming `sqrt` algorithm for v1.0. Newton-Raphson, lookup tables, and float-bit-trick initial estimates are explicitly excluded. The §3.1 pseudocode is the binding reference; deviations break cross-platform digest comparison and MUST be treated as conformance failures.

## Appendix H — Version History
- v0.3 (2026-05-06): Added trig constants and `CORDIC_K` to Appendix A; added `ERR_FIXED64_PARSE_*` codes to Appendix B; made `expected_raw` conditional on `expected_flags` emptiness in Appendix C; expanded Appendix D failure matrix to cover Add/Sub/Mul/conversion/parse rows; added op_id mapping table and flags-bit encoding in Appendix E.1; tied Appendix F lint rule to §8.1 stage gating; added Appendix G with CORDIC angle table and sqrt-algorithm pinning.
- v0.2 (2026-05-01): Initial appendices content drop (constants, error codes, vector schema, failure matrix, digest spec, lint policy).
