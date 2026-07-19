# Certification Platform Pin

**Created:** May 2, 2026
**Last Updated:** July 19, 2026 (v1.4 — **CERTIFIED against Unity 6000.4.9f1 (DX11) / Mono.** The Stage-0 platform-determinism KAT run executed on the pinned host (commit `819f9d1`): all three golden-vector corpora (#16 §9.5 #4 a/b/c) + the §5 determinism-tier locks pass byte-exact — 44 passed / 0 failed / 4 Stage-0+1-deferred skips (`TacticalDirector.DeterministicSim.Tests`, EditMode). Status flips **⏳ RECERT REQUIRED → ✅ PINNED**; every row below flips to ✅; `FR-DS-009-GATE` and the other downstream unblockers close. Evidence + full run record: `docs/specs/deterministic-sim/cert-runs/determinism-cert-2026-07-19.md` (+ `determinism-results-2026-07-19.xml`). Distinct from and complementary to the FR-PO-052 perf baseline certified the same day. Platform Certification owner sign-off recorded via the PR merge (Maintenance Rule). Residual, non-blocking: the §4.8.2 runtime MXCSR validation is unbuilt — a guard that *enforces* the pin, not part of *proving* the bits, now buildable against this certified pin. See Version History v1.4.)
**Last Updated (prior):** July 13, 2026 (v1.3 — **Unity engine version bump proposed: 2022.3.62f1 → Unity 6000.4.9f1, graphics API pinned to DX11.** This is a MAJOR Unity version bump under this file's own Maintenance Rule (row 2), which REQUIRES full recertification before the tuple can be marked ✅ Pinned again. `ProjectSettings/ProjectVersion.txt` has been updated to `6000.4.9f1` to match. No certification run has been performed against the new tuple — the Unity-version and Graphics-API rows below are recorded as the TARGET pin, status **⏳ Recert required**, not yet ✅ Pinned. All downstream unblockers this file previously closed (`FR-DS-009-GATE`, `FR-PO-052`, §7.5 D1, `EnvironmentFingerprint`) revert to blocked until a certification run completes against the new tuple per `cert-run-runbook.md`. See Version History v1.3.)
**Last Updated (prior):** June 12, 2026 (v1.2 — non-certifying Linux compile/test gate note added; pin unchanged). Prior: June 7, 2026 (Stage 0 host platform pinned — closes the standing OPEN ISSUE that blocked `FR-DS-009-GATE` Stage 0 activation across #16 §5.5, #18 FR-PO-052 perf-gate, #19 §7.5 D1 test-runner pin, #18 §3.9.4 IL2CPP/Mono warmup measurement, and the four downstream `[EST]` constants that depend on the measured warmup characteristic. Pin set: Windows 11, Unity 2022 LTS revision **2022.3.62f1** (default Stage 0; revise if a later patch release supersedes before first cert run), Mono backend (IL2CPP migrates at Stage 5+), x64, SSE4.2 SIMD baseline, 1 worker thread (single-threaded — multi-threading is a Stage 5+ concern), deterministic compiler flags per row 5.)
**Purpose:** Records the exact Stage 0 host platform tuple for deterministic simulation certification runs, as required by Spec #16 §5.5.

---

## Status

**✅ PINNED — certified against Windows 11 / Unity 6000.4.9f1 / DX11 / Mono / x64 / SSE4.2 / 1 worker / deterministic flags on July 19, 2026.**

The Stage 0 host platform tuple was re-certified July 19, 2026 after the July-13 major Unity version bump (2022.3.62f1 → 6000.4.9f1) that had reverted it to ⏳ Recert required. The platform-determinism KAT run executed on the pinned host (commit `819f9d1`, `TacticalDirector.DeterministicSim.Tests` under Unity Test Framework EditMode): all three golden-vector corpora (#16 §9.5 #4 a/b/c) and the §5 determinism-tier locks pass **byte-exact — 44 passed / 0 failed** (4 skips are documented Stage-0+1 file-I/O deferrals, outside the Stage-0 surface). Full run record + raw NUnit evidence: `docs/specs/deterministic-sim/cert-runs/determinism-cert-2026-07-19.md`. Platform Certification owner sign-off is recorded via the PR merge landing this flip (Spec #16 §1.7 Governance Artifacts / this file's Maintenance Rule). `FR-DS-009-GATE` Stage 0 activation and the other downstream unblockers below are now **closed**.

**Companion certification:** the FR-PO-052 per-tick perf baseline was certified the same day on this tuple (`docs/specs/performance-optimization/baselines/match-engine/kickoff-multi-second.cert.md`). This document certifies *determinism* (the bits are exact); that one certifies *performance* (the per-tick budget).

**Residual, non-blocking:** the §4.8.2 runtime MXCSR validation (query live float-mode flags at match start, reject on mismatch) is unbuilt. It is a guard that *enforces* this pin at replay time, not part of *proving* the bits exact — the KAT run above is that proof. With a certified pin now in place for it to enforce, it becomes buildable; tracked in the root `CLAUDE.md` OPEN ISSUES floatModelHash entry.

**Re-executing a certification run against this pin:** see the operator runbook at
`docs/tracking/cert-run-runbook.md` (host pre-flight against the tuple below). The
determinism half is the `-testFilter "TacticalDirector.DeterministicSim.Tests"`
EditMode batch-mode run recorded in the cert-run record above; the perf half is the
100-run `CertifiedPerfBaseline` capture in that runbook.

---

## Stage 0 Host Platform

| Field | Required value | Pinned value | Status |
|-------|---------------|--------------|--------|
| OS | Windows 10 or 11 | **Windows 11** | ✅ Pinned |
| Unity version | Unity 6 LTS | **Unity 6000.4.9f1** | ✅ Pinned |
| Graphics API | Pinned per platform default | **DX11** | ✅ Pinned |
| Backend | Mono or IL2CPP per project default | **Mono** | ✅ Pinned (confirmed the Stage-0 default under Unity 6 — the July-19 KAT run executed under the editor's Mono EditMode runtime) |
| IL2CPP version | — | N/A (Mono backend) | ✅ N/A |
| Compiler flag set | Deterministic flags (denormals-are-zero off, fp-contract off, fma off unless platform-pinned) | **DAZ off · FTZ off · fp-contract off · FMA intrinsics off · /fp:strict-equivalent** | ✅ Pinned |
| CPU architecture | x64 | **x64** | ✅ Fixed |
| Worker thread count | Pinned (see §4.8 EnvironmentFingerprint) | **1 (main thread only — Stage 0 is single-threaded)** | ✅ Pinned |
| SIMD feature level | Pinned (see §4.8) | **SSE4.2 baseline (no AVX / AVX2 / FMA intrinsics)** | ✅ Pinned |

---

## Pin Rationale

**OS — Windows 11.** Win 10 standard support ended October 2025; Win 11 is the supported developer target through Stage 0+1.

**Unity 6000.4.9f1.** Target Unity 6 LTS revision, superseding the prior 2022.3.62f1 pin (record retained below in Version History, not deleted). This is a MAJOR version bump under this file's own rule — it invalidates the June 7, 2026 certification and requires a fresh certification run before the tuple can be marked ✅ Pinned again. Subsequent patch releases (`f10`, `f11`, …) may be adopted before that first Unity-6 certification run by updating this file with sign-off; any further major version bump requires the same reset.

**Graphics API — DX11.** New row, not present under the Unity 2022.3.62f1 pin (this file had no graphics-API row because none of the Stage 0 gameplay-simulation surface renders — determinism certification is a headless/logic concern). Recorded here because Unity 6's default graphics API selection differs by platform and template; DX11 is pinned explicitly for the Windows 11 host so `EnvironmentFingerprint` (#16 §4.8) has an unambiguous value to capture once the digest is extended to include it. Rendering is not part of the Stage 0 determinism surface — this pin exists for host-tuple completeness, not because gameplay logic reads the graphics API.

**Mono backend.** Faster iteration than IL2CPP for the Stage 0 implementation phase; the determinism story is simpler (no AOT compilation pass). IL2CPP migration is a Stage 5+ concern when ship-quality perf becomes a hard requirement (per `src/CLAUDE.md` "Fixed64 stage scope decision" precedent — single-machine determinism via state snapshots is sufficient at Stage 0).

**Compiler flags.** Standard determinism set: denormals-are-zero (DAZ) off and flush-to-zero (FTZ) off so subnormal results round per IEEE-754 default; `fp-contract off` so the compiler cannot insert implicit fused multiply-adds; FMA hardware intrinsics off (FR-CS-040 ban — re-evaluated at Stage 5+ per platform pin). Mono's JIT honors these via project settings; no MSVC `/fp:strict` flag at Stage 0 since the runtime is Mono, not native C++ — equivalent semantic.

**Single-threaded (1 worker).** Matches Stage 0's float + state-snapshot determinism model. Per-frame parallel work (Spec #16 §3.2.5 BFS dispatch, multi-agent loops) runs on the main thread; cross-thread determinism is a Stage 5+ concern when multi-agent batching becomes a perf necessity.

**SSE4.2 baseline.** Conservative; broadly available on any x64 CPU from 2008+. AVX/AVX2/FMA excluded — survives hardware variance across developer machines and CI runners without requiring Stage 0 to enumerate per-CPU codepaths.

---

## Downstream Unblockers

This pin unblocks the following spec-level deliverables that were gated on it.
**Status (2026-07-19): the pin is certified, so every row below is now active/closed** — the
`FR-DS-009-GATE` determinism gate is satisfied by the KAT run, `FR-PO-052` by the same-day perf
capture, and the §7.5 D1 test-runner pin is the EditMode Unity Test Framework runner that executed
the cert.

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

1. **Unity patch-release bump** (e.g. 6000.4.9f1 → 6000.4.10f1) — preferred path, document in row 2 with date.
2. **Major Unity version bump** (e.g. 2022.3.X → 6000.X, or any future Unity 7+) — REQUIRES full recertification per #16 §4.8. (This is the exact case this v1.3 update records: the pin below is the target tuple, not yet certified.)
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
| 1.3     | 2026-07-13 | —      | Unity engine version bumped: 2022.3.62f1 → **6000.4.9f1**, graphics API      |
|         |            |        | pinned **DX11** (new row). MAJOR version bump per row 2 of the Maintenance   |
|         |            |        | Rule — resets certification status to ⏳ Recert required; the June 7, 2026    |
|         |            |        | certification against 2022.3.62f1 no longer applies. `ProjectVersion.txt`    |
|         |            |        | updated to match. Backend/compiler-flag/worker-count/SIMD rows carried over  |
|         |            |        | unverified pending a real Unity 6 host check. No recertification run has     |
|         |            |        | been performed; all downstream unblockers revert to blocked. Documentation-  |
|         |            |        | only change — no Platform Certification owner sign-off obtained for this     |
|         |            |        | edit (required before the tuple can be marked ✅ Pinned again).              |
| 1.4     | 2026-07-19 | —      | **CERTIFIED against Unity 6000.4.9f1 / DX11 / Mono.** Stage-0 platform-      |
|         |            |        | determinism KAT run executed on the pinned host (commit 819f9d1,             |
|         |            |        | TacticalDirector.DeterministicSim.Tests, Unity Test Framework EditMode):     |
|         |            |        | all three golden-vector corpora (#16 §9.5 #4 a/b/c: HKDF-SHA256, SipHash-    |
|         |            |        | 2-4-64, SerializeCanonical) + the §5 determinism-tier locks pass byte-exact  |
|         |            |        | — 44 passed / 0 failed / 4 Stage-0+1-deferred skips. Status ⏳ Recert        |
|         |            |        | required → ✅ PINNED; all tuple rows flip to ✅; FR-DS-009-GATE + downstream  |
|         |            |        | unblockers close. Evidence: docs/specs/deterministic-sim/cert-runs/          |
|         |            |        | determinism-cert-2026-07-19.md (+ raw NUnit XML). Companion to the FR-PO-052 |
|         |            |        | perf baseline certified the same day. Owner sign-off via the PR merge.       |
|         |            |        | Residual (non-blocking): §4.8.2 runtime MXCSR validation unbuilt.            |
