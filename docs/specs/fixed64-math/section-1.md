# Fixed64 Math Library Specification #9 — Section 1: Fixed64 Representation and Numeric Domain

## 1.1 Canonical Type Definition (MUST)
- `Fixed64` MUST be represented as a signed 64-bit integer storing a **Q32.32** fixed-point value.
- Raw storage type: `int64` (two's complement).
- Numeric interpretation rule: `value = raw / 2^32`.
- All simulation-critical math APIs MUST accept/return `Fixed64` (or aggregates that are composed only of `Fixed64` and deterministic integer/boolean metadata).

## 1.2 Scaling Factor and Unit Conversion (MUST)
- Scale factor constant: `FIXED64_SCALE = 2^32 = 4,294,967,296`.
- Conversion formulas:
  - `raw = round_mode(real * FIXED64_SCALE)`
  - `real = raw / FIXED64_SCALE`
- Quantum (smallest representable increment): `EPS = 1 / 2^32`.
- Multiplication and division MUST preserve this scaling by widening and rescaling deterministically (detailed in Section 2).

## 1.3 Numeric Range Constants (MUST)
- Canonical raw minimum: `0x8000000000000000`.
- Canonical raw maximum: `0x7FFFFFFFFFFFFFFF`.
- Represented minimum: `-2^31` (`-2,147,483,648.0` exactly).
- Represented maximum: `(2^31) - (1 / 2^32)` (`+2,147,483,647.99999999976716935635` approx).
- Quantum/epsilon: `1 / 2^32` (`0.00000000023283064365` approx).
- Implementations MUST expose these as compile-time constants and include them in machine-readable golden constants metadata.

## 1.4 Special-Value Policy (MUST)
- Runtime `Fixed64` representation MUST NOT include NaN, ±Inf, signaling sentinels, or payload-bit encodings.
- Exceptional arithmetic outcomes MUST be expressed via explicit API mode:
  - checked result + deterministic error,
  - saturating clamp,
  - unchecked behavior where explicitly allowed.
- Parsing and formatting paths MUST reject non-finite float literals (`nan`, `inf`, etc.) when converting to `Fixed64`.

## 1.5 Overflow/Underflow Behavior Hierarchy (MUST)
- Three explicit API families MUST exist:
  - `Checked*`: detects overflow/invalid states and returns deterministic error code.
  - `Saturating*`: clamps to `FIXED64_MIN`/`FIXED64_MAX` deterministically.
  - `Unchecked*`: no checks; caller owns safety obligations.
- Default APIs used by gameplay/simulation code MUST map to checked behavior unless a subsystem spec grants an approved saturating/unchecked exception.
- `Unchecked*` APIs MUST be lint-restricted from simulation call graphs unless waived.

## 1.6 Serialization and Byte Order (MUST)
- On-disk and network canonical encoding MUST be little-endian signed 64-bit raw value.
- Serialization schema MUST include:
  - numeric format identifier (`fixed64_q32_32`),
  - schema version,
  - optional feature flags for additive future fields.
- Replay logs and save files MUST preserve bit-exact raw values with no locale-dependent formatting layer.
- Cross-platform loaders MUST fail closed on unknown schema versions unless explicit migration support is present.

## 1.7 Version History
- v0.1 (2026-05-01): Initial draft for representation/domain requirements aligned to outline Section 1.
