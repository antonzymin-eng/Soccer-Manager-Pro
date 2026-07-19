# Certified Perf Baseline — `match-engine-kickoff-multi-second`

**Created:** June 28, 2026
**Status:** ✅ **CERTIFIED (FR-PO-052 per-tick perf baseline).** 100-run capture executed on the
pinned Windows 11 / Unity 6000.4.9f1 / Mono host (2026-07-19); the `floatModelHash` gap that
previously blocked promotion was resolved by ERR-016-006 Option A (the §4.8.3 Mono mapping +
`CreateStage0MonoCertified` hasher). Platform Certification owner sign-off is recorded via the PR
merge (see Version History v1.2).
**Scope:** this certifies the FR-PO-052 **per-tick perf baseline** for this one scenario. It is NOT
the full platform determinism certification — `docs/tracking/certification-platform.md` stays
**⏳ RECERT REQUIRED** because the §4.8.2 runtime MXCSR validation and the determinism-KAT run on
the pinned host are separate, still-host-blocked deliverables (see "Residual" below).
**Spec:** Performance Optimization Strategy #18 §3.4.4 / §4.3.2 / FR-PO-031 / FR-PO-052;
`docs/tracking/certification-platform.md` (Stage 0 host pin).

This is the FR-PO-052 per-tick certified baseline corpus entry for the Match Engine capstone
scenario (`MatchEngineCapstoneScenarios.KickoffMultiSecondPath`) — the authoritative per-PR
regression reference (FR-PO-031, +5%) on the pinned host.

The code-side entry is now `CertifiedPerfBaseline.Certified(...)` (see
`src/performance-optimization/CertifiedPerfBaseline.cs` and the `KickoffCertified()` helper in
`src/match-engine/tests/CertifiedPerfBaselineTests.cs`): the manifest below (with a genuine
`EnvironmentFingerprint` built via `CreateStage0MonoCertified`) + the measured p50/p99 project to a
`BaselineRecord` via `TryBuildBaselineRecord`, which the certified-projection test flows through
`PerfGateRunner` as an FR-PO-031 self-compare. The +5% regression check against a live measurement
runs on the **pinned host** (a Linux measurement vs a Windows-certified number is apples-to-oranges,
so the non-certifying Linux capstone gate keeps its generous Stage-0 anchor).

## Intent (known now)

| Field | Value |
|-------|-------|
| Scenario manifest ID | `tests/scenarios/cross-spec/match-engine-kickoff-multi-second` |
| Loop | `PhysicsSixtyHz` (`LOOP-PHYSICS-60HZ`) |
| Threshold cited | `FR-PO-052` |
| Platform pin | `win11-unity6000.4.9f1-dx11-mono-x64-sse4.2-1w-detflags` |
| Seed | `MatchEngineCapstoneScenarios.KickoffMultiSecondSeed` (`0x0F1E2D3C4B5A6978`) |

## Metrics — CAPTURED (2026-07-19), pending promotion

A real 100-run capture executed on the pinned Windows 11 / Unity 6000.4.9f1 tuple via
`MatchEngineCapstonePerfHarnessTests.Harness_RunsRealEngine_ProducesFiniteMetrics`
(`TD_PERF_RUN_COUNT=100`). This is a genuine Stopwatch measurement of the real `MatchEngine`
capstone on the real pinned host — **not** the Linux `dotnet-ci` non-certifying gate.

| Field | Value |
|-------|-------|
| p50 (ms/tick) | `0.4768` |
| p99 (ms/tick) | `2.5669` |
| Test result | Passed (`perf-results.xml`, `MatchEngineCapstonePerfHarnessTests`) |

> Note: the harness's own internal `SessionManifest` (built inside
> `MatchEngineCapstonePerfHarness.Run`) always self-labels `platformPin` as
> `CertifiedPerfBaseline.LinuxNonCertPlatformPin` with placeholder git-SHA/timestamps,
> **regardless of the OS actually running it** — that internal manifest is a smoke-test fixture,
> not the certification record. The manifest below is assembled independently from the operator's
> own observations of the actual run (git SHA, real wall-clock timestamps from `perf-results.xml`,
> real hardware counters), per `cert-run-runbook.md` Step 2.4.

## Captured `SessionManifest`

| Field | Value |
|-------|-------|
| `GitSha` | `224ee7eb8d2647ab1e75a5e69cafee534dbdca8b` |
| `Seed` | `0x0F1E2D3C4B5A6978` |
| `PlatformPin` | `win11-unity6000.4.9f1-dx11-mono-x64-sse4.2-1w-detflags` |
| `ScenarioManifestId` | `tests/scenarios/cross-spec/match-engine-kickoff-multi-second` |
| `SessionStartUtc` | `2026-07-19T00:11:19Z` |
| `SessionEndUtc` | `2026-07-19T00:12:04Z` |
| `HardwareCounters.CpuModel` | `Intel(R) Core(TM) i5-9300H CPU @ 2.40GHz` |
| `HardwareCounters.CoreCount` | `4` |
| `HardwareCounters.ThermalState` | `idle, no sustained load prior to run (no monitoring tool used)` |
| `HarnessVersion` | `1.0` (no formal semver scheme exists yet for this harness in the codebase; this is its first real capture) |

### `EnvironmentFingerprint` (#16 §4.8) — all 6 fields captured

| Field | Value |
|-------|-------|
| `WorkerCount` | `1` |
| `SchedulerPolicy` | `Stage0-SingleThread-v1` |
| `ReductionTopology` | `Serial` |
| `SimdFeatureLevel` | `SSE4.2` |
| `UnicodeNormalizationVersion` | `15.1` |
| `FloatModelHash` | `73c47ad54d3a81408b46694b513634fd244f25262aa4104614712134b6bb756a` |

Built via `EnvironmentFingerprint.CreateStage0MonoCertified(monoRuntimeVersion)` (ERR-016-006
Option A). The §4.8.3 float-flag tuple's field 2 (`compilerVersion` = Mono runtime version) is the
one host-supplied value: recorded as **`mono-bundled-unity6000.4.9f1`** — the honest identifier for
Unity's forked Mono runtime, which Unity versions by editor release rather than a standalone Mono
semver (option A of the two the owner was offered; the alternative was a distinct `mono --version`
string the host does not expose separately). The remaining tuple fields are the pinned Stage-0 Mono
mapping (`Mono` / `win-x64` / `MONO` sentinel) + the §4.8.3 Required Stage-0 float-mode flag values
(denormals off, FTZ off, rounding NearestEven, fp-contract off, FMA off, fast-math off, SIMD SSE4.2).
The hash was computed by `FloatFlagTuple.ComputeHash()` and independently reproduced by a Python
mirror of `CanonicalSerializer`, validated byte-for-byte against the pinned golden vector
(`89f50a31…f343e7` for the `"6.13.0"` test input) before recording this value.

## Residual — still host-blocked (NOT part of this perf certification)

Two items remain and are deliberately outside the scope of this FR-PO-052 perf-baseline
certification; they gate `certification-platform.md`'s broader ✅ PINNED status, which stays
**⏳ RECERT REQUIRED**:

1. **§4.8.2 runtime MXCSR validation** — querying the live host float-mode flags at match start and
   rejecting on mismatch is unimplemented (needs native interop on the pinned host). The recorded
   tuple above uses the pinned Stage-0 flag values, which is exactly what §4.8.2 validates against;
   until the live check exists, the recorded flags are asserted, not runtime-verified.
2. **Full platform determinism certification** — the determinism KAT run on the pinned
   Windows/Unity/Mono host (distinct from this per-tick perf capture). Owner-scheduled per
   `cert-run-runbook.md`.

Neither blocks the FR-PO-052 perf baseline: the per-tick measurement, git SHA, seed, platform pin,
timestamps, hardware counters, and now all 6 `EnvironmentFingerprint` fields are complete.

## Runbook — promoting PENDING → CERTIFIED

> Full operator procedure (host pre-flight, capture, promotion, sign-off):
> `docs/tracking/cert-run-runbook.md`. The abridged steps below stay here for
> quick reference.

1. ✅ On the pinned platform (`certification-platform.md` Stage 0 tuple), run the capstone
   scenario under the perf harness for `BaselineSampleCount` (= 100) runs. — Done 2026-07-19.
2. ✅ Record the measured per-tick `p50`/`p99` (ms) and the full `SessionManifest`, including all 6
   `EnvironmentFingerprint` fields (the `FloatModelHash` gap resolved by ERR-016-006 Option A).
3. ✅ Replace the metric placeholders above; flip Status to **CERTIFIED**.
4. ✅ In code, swap the `Pending(...)` entry for `CertifiedPerfBaseline.Certified(manifest, loop,
   p50, p99, "FR-PO-052")` (the `KickoffCertified()` helper) and unskip / extend the
   certified-projection assertions.
5. ⏳ Platform Certification owner sign-off (Spec #16 §1.7 Governance Artifacts) — recorded via the
   PR merge that lands this promotion (solo-project governance). Does NOT close the separate
   §4.8.2 MXCSR validation / platform-determinism cert (see Residual).

## Version History

| Version | Date       | Author | Notes                                                        |
|---------|------------|--------|--------------------------------------------------------------|
| 1.0     | 2026-06-28 | —      | Created PENDING. First corpus entry under `baselines/`.       |
| 1.1     | 2026-07-19 | —      | Real 100-run capture on the pinned Windows 11 / Unity 6000.4.9f1 host: p50=0.4768ms / p99=2.5669ms, full SessionManifest except `EnvironmentFingerprint.FloatModelHash`. Status stayed PENDING — §4.8.3's `floatModelHash` tuple was IL2CPP/AOT-shaped with no defined Mono/JIT meaning; flagged as an open gap requiring a Platform Certification owner decision rather than fabricated. |
| 1.2     | 2026-07-19 | —      | **Promoted PENDING → CERTIFIED.** The `floatModelHash` gap is resolved by ERR-016-006 Option A (§4.8.3 Mono mapping + `CreateStage0MonoCertified` hasher): field 2 = host-supplied Mono runtime version, recorded `mono-bundled-unity6000.4.9f1`; `FloatModelHash = 73c47ad5…b6bb756a` (golden-vector-validated Python mirror). All 6 fingerprint fields now captured. Code-side entry swapped `Pending(...)` → `Certified(...)` (`KickoffCertified()`). Platform Certification owner sign-off recorded via PR merge. Scope: FR-PO-052 perf baseline only — the §4.8.2 MXCSR runtime validation + the platform-determinism cert stay host-blocked and `certification-platform.md` remains ⏳ RECERT REQUIRED (see Residual). |
