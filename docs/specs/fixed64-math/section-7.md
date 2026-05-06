# Fixed64 Math Library Specification #9 — Section 7: Cross-Platform Determinism Validation Harness

## 7.1 Platform Matrix (Normative)

The harness MUST execute the full golden-vector corpus on every row of the table below. Adding or removing a row MUST go through the schema-versioning process in §6.7 and the spec change-control process; "approved" without a row in this table is not approved.

| Row | OS | Architecture | CPU feature floor | Compiler / Runtime | Optimization flags |
|---|---|---|---|---|---|
| 1 | Ubuntu 24.04 LTS (kernel 6.8.x) | x86-64 (Zen 4) | SSE4.2, AVX2 | clang 18.1.x | `-O3 -fno-fast-math -fno-unsafe-math-optimizations` |
| 2 | Ubuntu 24.04 LTS (kernel 6.8.x) | x86-64 (Intel 13th-gen) | SSE4.2, AVX2 | clang 18.1.x | same as row 1 |
| 3 | Windows 11 23H2 | x86-64 (Zen 4) | SSE4.2, AVX2 | MSVC v143 (VS 2022 17.10) | `/O2 /fp:strict` |
| 4 | macOS 14.x | ARM64 (Apple M2 or later) | NEON, FP16 | clang 15.0.x (Xcode 15) | `-O3 -fno-fast-math` |
| 5 | Ubuntu 24.04 LTS (kernel 6.8.x) | ARM64 (Graviton 4) | NEON | clang 18.1.x | same as row 1 |
| 6 | Linux (CI image, kernel 6.8.x) | WASM32 | wasm-bigint | wasmer 4.x or wasmtime 21.x | `-O2 -msimd128=off` |

Rows 1–5 are release-blocking. Row 6 is informational in v1.0 and becomes release-blocking in v1.1 once the deterministic-WASM toolchain is stabilized; until then row 6 emits its digest as a non-blocking artifact.

CPU-feature gating: the harness MUST disable runtime CPU-feature detection for SIMD codepaths (`-DFIXED64_FORCE_SCALAR=1` or equivalent) and MUST verify by digest comparison that scalar and SIMD codepaths produce identical raw outputs on the same row.

## 7.2 Harness Architecture
- Components: vector loader, execution runner, digest generator, comparator, artifact emitter.
- Digest construction is normative (see Appendix E).

## 7.3 Pass/Fail Criteria
- Core arithmetic, conversion, sqrt, sin, cos, and atan2 MUST match exactly by raw bit value across every row in §7.1. The error envelopes published in §3.3 govern accuracy versus real-valued reference and do **not** authorize cross-row divergence.
- A row whose digest disagrees with row 1 MUST fail the gate and emit forensic artifacts per §7.4.

## 7.4 Divergence Workflow
- Any drift MUST emit forensic artifacts: input seed, op trace, platform metadata (full §7.1 row tuple), digest diff, and the offending vector index.
- Forensic artifacts MUST be retained for 90 days minimum.

## 7.5 CI Integration
- Determinism harness MUST run in release-blocking CI stage on rows 1–5.
- Row 6 runs in the same stage but does not block until §7.1's v1.1 transition.

## 7.6 Incident Process
- Determinism incidents MUST have owner, SLA, and documented rollback/mitigation path.
- See Appendix F (incident report template; deferred to v1.1 of this spec — tracked in §8.3 deferred dependencies).

## 7.7 Version History
- v0.2 (2026-05-06): Enumerated the platform matrix (six rows, release-blocking flags); pinned CPU-feature gating; tightened §7.3 to enumerate utility functions covered by bit-exact equality.
- v0.1 (2026-05-01): Initial draft aligned to outline Section 7.
