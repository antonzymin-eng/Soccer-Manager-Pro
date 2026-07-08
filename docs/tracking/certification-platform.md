# Certification Platform Pin

**Created:** May 2, 2026
**Last Updated:** June 12, 2026 (v1.2 — non-certifying Linux compile/test gate note added; pin unchanged). Prior: June 7, 2026 (Stage 0 host platform pinned — closes the standing OPEN ISSUE that blocked `FR-DS-009-GATE` Stage 0 activation across #16 §5.5, #18 FR-PO-052 perf-gate, #19 §7.5 D1 test-runner pin, #18 §3.9.4 IL2CPP/Mono warmup measurement, and the four downstream `[EST]` constants that depend on the measured warmup characteristic. Pin set: Windows 11, Unity 2022 LTS revision **2022.3.62f1** (default Stage 0; revise if a later patch release supersedes before first cert run), Mono backend (IL2CPP migrates at Stage 5+), x64, SSE4.2 SIMD baseline, 1 worker thread (single-threaded — multi-threading is a Stage 5+ concern), deterministic compiler flags per row 5.)
**Purpose:** Records the exact Stage 0 host platform tuple for deterministic simulation certification runs, as required by Spec #16 §5.5.

---

## Status

**✅ PINNED — Stage 0 host platform tuple set June 7, 2026.**

This pin satisfies the precondition for `FR-DS-009-GATE` Stage 0 activation per Spec #16 §5.5. Updates require Platform Certification owner sign-off per Spec #16 §1.7 Governance Artifacts.

**Executing a certification run against this pin:** see the operator runbook at
`docs/tracking/cert-run-runbook.md` (host pre-flight against the tuple below,
100-run capture, `PENDING → CERTIFIED` promotion of the
`CertifiedPerfBaseline` corpus entry, and sign-off). The first run is currently
blocked on Unity project initialization + pinned-host access — see that file's
Status section.

---

## Stage 0 Host Platform

| Field | Required value | Pinned value | Status |
|-------|---------------|--------------|--------|
| OS | Windows 10 or 11 | **Windows 11** | ✅ Pinned |
| Unity version | Unity 2022 LTS | **Unity 2022.3.62f1** | ✅ Pinned |
| Backend | Mono or IL2CPP per project default | **Mono** | ✅ Pinned |
| IL2CPP version | — | N/A (Mono backend) | ✅ N/A |
| Compiler flag set | Deterministic flags (denormals-are-zero off, fp-contract off, fma off unless platform-pinned) | **DAZ off · FTZ off · fp-contract off · FMA intrinsics off · /fp:strict-equivalent** | ✅ Pinned |
| CPU architecture | x64 | **x64** | ✅ Fixed |
| Worker thread count | Pinned (see §4.8 EnvironmentFingerprint) | **1 (main thread only — Stage 0 is single-threaded)** | ✅ Pinned |
| SIMD feature level | Pinned (see §4.8) | **SSE4.2 baseline (no AVX / AVX2 / FMA intrinsics)** | ✅ Pinned |

---

## Pin Rationale

**OS — Windows 11.** Win 10 standard support ended October 2025; Win 11 is the supported developer target through Stage 0+1.

**Unity 2022.3.62f1.** Latest Unity 2022 LTS revision at pin time. Subsequent patch releases (`f2`, `f3`, …) may be adopted before the first certification run by updating this file with sign-off; **major version bumps (Unity 6, 2023.X, 2024.X) require a new pin and full recertification** per #16 §4.8 `EnvironmentFingerprint` invariant.

**Mono backend.** Faster iteration than IL2CPP for the Stage 0 implementation phase; the determinism story is simpler (no AOT compilation pass). IL2CPP migration is a Stage 5+ concern when ship-quality perf becomes a hard requirement (per `src/CLAUDE.md` "Fixed64 stage scope decision" precedent — single-machine determinism via state snapshots is sufficient at Stage 0).

**Compiler flags.** Standard determinism set: denormals-are-zero (DAZ) off and flush-to-zero (FTZ) off so subnormal results round per IEEE-754 default; `fp-contract off` so the compiler cannot insert implicit fused multiply-adds; FMA hardware intrinsics off (FR-CS-040 ban — re-evaluated at Stage 5+ per platform pin). Mono's JIT honors these via project settings; no MSVC `/fp:strict` flag at Stage 0 since the runtime is Mono, not native C++ — equivalent semantic.

**Single-threaded (1 worker).** Matches Stage 0's float + state-snapshot determinism model. Per-frame parallel work (Spec #16 §3.2.5 BFS dispatch, multi-agent loops) runs on the main thread; cross-thread determinism is a Stage 5+ concern when multi-agent batching becomes a perf necessity.

**SSE4.2 baseline.** Conservative; broadly available on any x64 CPU from 2008+. AVX/AVX2/FMA excluded — survives hardware variance across developer machines and CI runners without requiring Stage 0 to enumerate per-CPU codepaths.

---

## Downstream Unblockers

This pin unblocks the following spec-level deliverables that were gated on it:

| Gate | Spec | Effect |
|------|------|--------|
| `FR-DS-009-GATE` Stage 0 activation | #16 §5.5 | Stage 0 cert runs can now execute against this platform tuple. |
| `FR-PO-052` Stage 0+1 perf-gate activation | #18 | Perf baselines captured against this pin become comparison-valid per FR-PO-031. |
| §7.5 D1 test-runner pin | #19 | `GoldenVectorRunner` Stage 0+1 deferred-status results can promote to live KAT execution. |
| §3.9.4 warmup measurement | #18 | `FirstTickWarmupCount` `[EST]` can be measured and promoted to `[GT]`. |
| `EnvironmentFingerprint` 6-field digest | #16 §4.8 | Replay-mismatch detection (`ERR_DS_REPLAY_ENV_MISMATCH`) becomes meaningful against a known reference platform. |

---

## Maintenance Rule

Update this file and obtain Platform Certification owner sign-off before:

1. **Unity patch-release bump** (e.g. 2022.3.62f1 → 2022.3.63f1) — preferred path, document in row 2 with date.
2. **Major Unity version bump** (2022.3.X → Unity 6 or later) — REQUIRES full recertification per #16 §4.8.
3. **Backend swap** (Mono → IL2CPP) — REQUIRES Stage 5+ planning per `src/CLAUDE.md` Fixed64 stage scope; not a Stage 0 path.
4. **SIMD baseline raise** (SSE4.2 → AVX2) — REQUIRES re-running all FR-PO-031 baselines against the new instruction set.
5. **Worker count bump** (1 → N) — REQUIRES Stage 5+ planning; introduces cross-thread determinism concerns not addressed at Stage 0.

A PR updating this file requires sign-off from the Platform Certification owner per Spec #16 §1.7 Governance Artifacts.

---

## Relationship to the Linux compile/test gate (non-certifying)

The CI job `dotnet-compile-test` (`tools/dotnet-ci/run-gate.sh`, added June 12,
2026) compiles the entire `src/` tree and executes every NUnit suite on
ubuntu-latest under .NET 8 with a UnityEngine shim. **That gate is explicitly
NON-CERTIFYING**: it is a smoke gate for the never-compiled / dead-test-suite
defect class and for test execution, not a determinism certification. No digest,
perf number, or replay produced on the Linux gate is authoritative.
`FR-DS-009-GATE`, `FR-PO-052`, golden-digest pins, and all bit-exactness claims
are certified ONLY on the pinned tuple above. The two coexist by design: the
Linux gate answers "does it compile and do the tests pass," this pin answers
"are the bits exactly right."

---

## Version History

| Version | Date       | Author | Notes                                                                        |
| 1.0     | 2026-05-02 | —      | Placeholder file created. All rows `_TBD_` / `⏳ Not pinned`.                |
| 1.1     | 2026-06-07 | —      | Stage 0 pin landed: Windows 11 / Unity 2022.3.62f1 / Mono / x64 / SSE4.2 /   |
|         |            |        | 1 worker / deterministic flags per row 5. Closes the standing OPEN ISSUE     |
|         |            |        | blocking FR-DS-009-GATE. Downstream unblockers documented per row.            |
| 1.2     | 2026-06-12 | —      | Non-certifying-gate note added: the new Linux dotnet compile/test CI gate     |
|         |            |        | (tools/dotnet-ci) is a smoke gate only; certification stays on this pin.      |
