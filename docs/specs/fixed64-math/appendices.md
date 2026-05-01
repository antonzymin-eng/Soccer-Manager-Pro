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
  "epsilon": "0.00000000023283064365386962890625"
}
```

## Appendix B — Error Code Registry (Normative)
| Symbol | Code | Message | Stability |
|---|---:|---|---|
| `ERR_FIXED64_OVERFLOW` | 1001 | arithmetic overflow/underflow | stable in major version |
| `ERR_FIXED64_DIV_ZERO` | 1002 | divide by zero | stable in major version |
| `ERR_FIXED64_DOMAIN` | 1003 | invalid math domain | stable in major version |
| `ERR_FIXED64_CONVERT_RANGE` | 1004 | conversion out of range | stable in major version |
| `ERR_FIXED64_CONVERT_NONFINITE` | 1005 | non-finite float conversion | stable in major version |

## Appendix C — Golden Vector Schema and Sample (Normative)
```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "title": "Fixed64GoldenVector",
  "type": "object",
  "required": ["schema_version","operation","inputs","rounding_mode","expected_raw","expected_flags"],
  "properties": {
    "schema_version": {"type": "string"},
    "operation": {"type": "string"},
    "inputs": {"type": "array", "items": {"type": "string"}},
    "rounding_mode": {"type": "string", "enum": ["toward_zero","floor","ceil","nearest_even"]},
    "expected_raw": {"type": "string", "pattern": "^0x[0-9A-Fa-f]{16}$"},
    "expected_flags": {"type": "array", "items": {"type": "string"}}
  }
}
```

Sample:
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

## Appendix D — Failure Behavior Matrix (Normative)
| Operation | Condition | Checked | Saturating | Unchecked |
|---|---|---|---|---|
| `Negate` | `raw==0x8000000000000000` | `ERR_FIXED64_OVERFLOW` | clamp to max | wraps |
| `Abs` | `raw==0x8000000000000000` | `ERR_FIXED64_OVERFLOW` | clamp to max | wraps |
| `Div` | denominator zero | `ERR_FIXED64_DIV_ZERO` | clamp by numerator sign | undefined |
| `Sqrt` | negative input | `ERR_FIXED64_DOMAIN` | n/a | undefined |

## Appendix E — Determinism Harness Digest Spec (Normative)
- Digest algorithm: SHA-256.
- Record byte layout (little-endian): `op_id:u16 | rounding:u8 | input_count:u8 | inputs:i64[] | output:i64 | flags:u32`.
- Harness result MUST include:
  - platform metadata,
  - ordered record count,
  - SHA-256 hex digest.

## Appendix F — Unchecked Lint Policy (Normative)
- Rule ID: `FX64-UNSAFE-001`.
- Scope: simulation-critical call graphs (physics, collision, gameplay, replay).
- Default: `error` severity.
- Suppression requires `@fixed64-waiver(id, owner, expiry)` annotation and linked approval ticket.
- CI MUST fail if active suppressions are expired.
