# EnvironmentFingerprint `floatModelHash` — Mono-backend mapping (PROPOSAL, awaiting owner sign-off)

> **File:** docs/tracking/env-fingerprint-float-model-hash-mono-mapping.md
> **Created:** 2026-07-19
> **Status:** PROPOSAL — NOT APPROVED. No spec text or production hasher changes until the owners in §7 sign off.
> **Tracks:** ERR-016-006 (`docs/tracking/spec-error-log.md`); root `CLAUDE.md` OPEN ISSUES.
> **Purpose:** Resolve how Deterministic Simulation #16 §4.8.3's `floatModelHash` tuple is populated under
> the pinned Stage-0 **Mono** backend, so a live-host fingerprint can be computed without fabricating values.
> This is a decision request, not an implementation.

---

## 0. Scope

This document asks the owners to make **one decision** (§3) and record sign-off (§7). It changes no spec
text and no production code by itself. The code-side honesty fixes that DON'T need the decision have already
landed under ERR-016-006 (`EnvironmentFingerprint.cs` v1.2 — SSE4.2 pin fix, named placeholder sentinel,
`IsDevPlaceholder`, gap-flagging docs). The §4.8.3 spec edit and the live-host hasher land only **after**
sign-off.

## 1. Problem

`SessionManifest` (Spec #18 §3.3.2, `src/performance-optimization/SessionManifest.cs`) requires an
`EnvironmentFingerprint` (#16 §4.8). Its `floatModelHash` field is specified in **§4.8.3** as:

```
floatModelHash = SHA-256(SerializeCanonical(0x14 ‖ floatFlagTuple))
```

over an ordered 11-field tuple. Three linked issues block computing it honestly for the pinned Stage-0 host.

### 1.1 No live-host hasher exists
`EnvironmentFingerprint.FloatModelHash` (`src/deterministic-sim/EnvironmentFingerprint.cs`) is a plain
`string` constructor argument. The class's `ComputeDigest()` hashes the **outer** 6-field fingerprint for the
§3.2.3 snapshot-header preimage — a *different* digest. The §4.8.3 11-field float-flag tuple has **no
implementation anywhere**; the only value ever supplied is the `STAGE0_DEV_PLACEHOLDER` sentinel from
`CreateStage0Dev()`.

### 1.2 The tuple's own fields contradict the pinned backend
The 11 fields (§4.8.3):

| # | Field | Type | Maps cleanly to Mono? |
|---|-------|------|-----------------------|
| 1 | `compilerToolchain` (MSVC/Clang/AppleClang/GCC) | string | **No** — native-toolchain enum; Mono JIT is none of these |
| 2 | `compilerVersion` (Major.Minor.Patch) | string | **Partly** — a Mono/runtime version exists but isn't a "compiler" version in the §4.8.3 sense |
| 3 | `targetTriple` (LLVM triple, e.g. `x86_64-pc-windows-msvc`) | string | **No** — no LLVM triple under Mono JIT |
| 4 | `il2cppVersion` (`"MONO"` sentinel for Mono) | string | **Contradiction** — see below |
| 5 | `denormalsAreZero` | bool | **Yes** — MXCSR at process start |
| 6 | `flushToZero` | bool | **Yes** — MXCSR |
| 7 | `roundingMode` | u8 | **Yes** — MXCSR / `fesetround` |
| 8 | `fpContractMode` | u8 | **Yes** — JIT/project flag |
| 9 | `fmaEnabled` | bool | **Yes** — FR-CS-040 ban ⇒ false |
| 10 | `fastMath` | bool | **Yes** — false |
| 11 | `simdLevel` (must match `simdFeatureLevel`) | string | **Yes** — `SSE4.2` |

Fields 5–11 map cleanly (a runtime MXCSR/CSR read plus the pinned SIMD string). Fields 1–4 are
native-compilation / IL2CPP concepts.

### 1.3 The spec-vs-pin contradiction
- §4.8.3 field 4: *"Stage-0 certification **REQUIRES IL2CPP** … MUST reject any snapshot whose fingerprint
  contains `"MONO"` as `ERR_DS_REPLAY_ENV_MISMATCH`."*
- §5.5 row 0 (certification matrix): Stage-0 developer host = *"Unity 2022 LTS, **IL2CPP (MSVC backend)**"*.
- §5.5.1: deterministic flag strings are written for MSVC/Clang native + IL2CPP-emitted C++.
- **But** `docs/tracking/certification-platform.md` v1.3 pins the Stage-0 backend to **Mono**
  (`Backend | **Mono**`; `IL2CPP version | N/A (Mono backend)`), rationale: *"IL2CPP migration is a Stage 5+
  concern."*

§4.8.3 and §5.5 (May 3–4, 2026) **predate** the platform pin (June 7, 2026) and were never reconciled with
it. So the spec both requires IL2CPP and provides a `"MONO"` fallback it then says certification must reject —
neither of which matches the runtime actually pinned. A live hasher cannot be written respectably until this
is resolved: fields 1–4 have no defined, non-fabricated value under Mono.

## 2. What is / isn't blocked (blast radius)

Latent, not live:
- `SaveManager` writes the header `Fingerprint = null` (`src/deterministic-sim/SaveManager.cs`) — the
  fingerprint is not yet wired into the save path.
- The fingerprint is load-bearing only at a real **certification run**, which is independently blocked: no
  Unity host in the current environment, and `certification-platform.md` is `⏳ RECERT REQUIRED` after the
  Unity 6 bump.
- Every current consumer uses `CreateStage0Dev()`, now honestly labelled a placeholder and assertable via
  `IsDevPlaceholder`.

So nothing is silently drifting. The cost of leaving this open is only that the placeholder rests on an
undecided spec — which this proposal exists to close.

Distinct from **ERR-016-005**'s follow-up, which concerns the *outer* envFp preimage golden vector, not this
inner §4.8.3 tuple.

## 3. The decision required

**Under the pinned Stage-0 Mono backend, what does the §4.8.3 `floatModelHash` tuple contain, and does
Stage-0 certification accept a Mono fingerprint?** Pick one of §4.

## 4. Options

### Option A — Map the tuple onto Mono, keep the 11-field shape *(recommended)*
Amend §4.8.3 so fields 1–4 have Mono-backend meanings, and make the "reject MONO" rule **stage-gated** (Stage
5+ / IL2CPP only):

| # | Field | Mono-backend value | Source |
|---|-------|--------------------|--------|
| 1 | `compilerToolchain` | `"Mono"` (extend the enum) | fixed for the Mono backend |
| 2 | `compilerVersion` | Mono runtime / BCL version string | queried at boot (pin the exact API in the spec edit) |
| 3 | `targetTriple` | .NET RID, e.g. `"win-x64"` | OS+arch, the determinism-relevant part of a triple |
| 4 | `il2cppVersion` | `"MONO"` sentinel (already specified) | fixed |
| 5–10 | float-mode flags | live MXCSR/CSR read | `_MM_GET_*` equivalent at process start |
| 11 | `simdLevel` | `"SSE4.2"` | pinned baseline |

Reconciliation: §4.8.3 field 4 and §5.5 flip from "certification REQUIRES IL2CPP / rejects MONO" to
"Stage-0 certification is Mono; the reject-MONO rule and the IL2CPP tuple apply from Stage 5+." Keeps a single
tuple shape and a genuinely-computable hash; the backend still binds into the fingerprint (a Mono-vs-IL2CPP
cross-backend replay still fails, just via `compilerToolchain`/`il2cppVersion` both differing).

**Cost:** touches §4.8.3, §5.5 row 0, §5.5.1 (add a Mono flag-string section); needs the compilerVersion
query API pinned.

### Option B — Flip the Stage-0 pin to IL2CPP, keep §4.8.3 as written
Make `certification-platform.md` pin IL2CPP at Stage 0. **Not recommended:** directly reverses a deliberate,
documented Stage-0 decision (Mono chosen for iteration speed + a simpler determinism story; IL2CPP explicitly
deferred to Stage 5+, mirroring the Fixed64 stage-scope precedent). Large operational cost (AOT pipeline,
recert) for no Stage-0 gameplay benefit.

### Option C — Stage-0 tuple variant (hash fields 5–11 + a backend tag)
Add a `floatFlagTupleVersion`; the Stage-0/Mono variant hashes fields 5–11 plus an explicit backend tag,
deferring the native fields 1–3 to a Stage-5+/IL2CPP variant. Cleanest fit to the Mono reality (no
placeholder-ish RID/version in "compiler" fields) but introduces a tuple schema version and a second
serialization shape. Viable if owners dislike putting a RID into `targetTriple`.

## 5. Recommendation

**Option A.** Least structural change, keeps one tuple shape, yields a genuinely computable hash, and the
only substantive spec change is making the reject-MONO / IL2CPP-required clause stage-gated — which simply
aligns §4.8.3/§5.5 with the platform pin that already superseded them. Fall back to **Option C** if the
owners prefer not to overload the native-compiler fields with Mono/RID values.

## 6. What lands after sign-off (deferred — not in this proposal)

1. **Spec edit** to §4.8.3 (+ §5.5/§5.5.1) per the chosen option, with a `spec-error-log` closure note on
   ERR-016-006.
2. **Live-host hasher**: implement `SHA-256(SerializeCanonical(0x14 ‖ floatFlagTuple))` — fields 5–10 from an
   MXCSR/CSR read at match start (the value §4.8.2's "reject if observed ≠ recorded" clause enforces), fields
   1–4/11 per the chosen mapping. Replaces the placeholder in a new certification factory alongside
   `CreateStage0Dev()`.
3. **Golden vector** pinning the §4.8.3 tuple hash (parallel to the ERR-016-005 outer-envFp follow-up).
4. **Verification** requires a Unity host on the pinned tuple (`cert-run-runbook.md` P2) — cannot run in the
   current Linux/no-Unity environment.

## 7. Sign-off

Per Spec #16 §1.7 (platform / determinism changes require owner sign-off):

- [ ] **Deterministic-Sim spec owner** — approves the chosen option's §4.8.3 tuple semantics.
- [ ] **Platform-Certification owner** — approves the §5.5 / `certification-platform.md` reconciliation.

Decision recorded: __________ (option) on __________ (date) by __________.

---

## Version History

| Version | Date       | Author | Notes                                                                    |
|---------|------------|--------|--------------------------------------------------------------------------|
| 0.1     | 2026-07-19 | —      | Initial proposal. Problem statement, options A/B/C, recommendation (A), deferred implementation plan, sign-off block. Tracks ERR-016-006. |
