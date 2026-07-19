# Certified Perf Baseline — `match-engine-kickoff-multi-second`

**Created:** June 28, 2026
**Status:** ⏳ **PENDING — measurement captured on the pinned platform; NOT promotable to CERTIFIED
until the `floatModelHash` gap below is resolved by the Platform Certification owner.**
**Spec:** Performance Optimization Strategy #18 §3.4.4 / §4.3.2 / FR-PO-031 / FR-PO-052;
`docs/tracking/certification-platform.md` (Stage 0 host pin).

This is the FR-PO-052 per-tick certified baseline corpus entry for the Match Engine capstone
scenario (`MatchEngineCapstoneScenarios.KickoffMultiSecondPath`). It is the authoritative
per-PR regression reference (FR-PO-031, +5%) **once fully certified**.

The code-side entry remains `CertifiedPerfBaseline.Pending(...)` (see
`src/performance-optimization/CertifiedPerfBaseline.cs` and
`src/match-engine/tests/CertifiedPerfBaselineTests.cs`) — unchanged by this capture, and
correctly so: `CertificationStatus` has only `Pending`/`Certified` (no partial state), and this
entry is not yet eligible for `Certified(...)` per the gap below. `TryBuildBaselineRecord` still
correctly refuses to build a `BaselineRecord`, so no unresolved-gap number can leak into the
FR-PO-031 regression gate.

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

### `EnvironmentFingerprint` (#16 §4.8) — 5 of 6 fields captured; 1 field BLOCKED

| Field | Value |
|-------|-------|
| `WorkerCount` | `1` |
| `SchedulerPolicy` | `Stage0-SingleThread-v1` |
| `ReductionTopology` | `Serial` |
| `SimdFeatureLevel` | `SSE4.2` |
| `UnicodeNormalizationVersion` | `15.1` |
| `FloatModelHash` | **NOT COMPUTABLE — see gap below** |

## OPEN GAP — `FloatModelHash` cannot be honestly computed for this host

Per `deterministic-sim/section-4.md` §4.8.3, `FloatModelHash` is a SHA-256 over an 11-field
tuple — `compilerToolchain` (enum-constrained to `"MSVC"` / `"Clang"` / `"AppleClang"` /
`"GCC"`), `compilerVersion`, `targetTriple`, plus denormals/flush-to-zero/rounding-mode/
fp-contract/FMA flags and others. This tuple's shape assumes a **natively AOT-compiled** binary
(e.g. an IL2CPP build) with an actual compiler-toolchain invocation whose version and target
triple can be cited.

This capture ran on **Mono / JIT** — the Stage 0 backend per `certification-platform.md`'s own
pin. Mono JIT-compiles IL to machine code at runtime; there is no ahead-of-time
`compilerToolchain` invocation to cite, and the spec's enum has no "N/A" / JIT member. Inventing
a plausible-looking value here (e.g. citing the toolchain Mono's own runtime binary happened to
be built with) would misrepresent what is actually being measured, and would silently defeat the
`ERR_DS_REPLAY_ENV_MISMATCH` divergence check this field exists to support if the project later
migrates to IL2CPP.

**This entry stays PENDING (not Certified) until a Platform Certification owner decides:**
(a) define a Mono/JIT-specific substitute tuple for `floatModelHash`, or
(b) formally exempt JIT-runtime captures from this field with documented rationale in #16 §4.8.3.

Everything else required for certification (the p50/p99 measurement, git SHA, seed, platform
pin, timestamps, hardware counters, and 5/6 `EnvironmentFingerprint` fields) is captured and
ready the moment this gap resolves.

## Runbook — promoting PENDING → CERTIFIED

> Full operator procedure (host pre-flight, capture, promotion, sign-off):
> `docs/tracking/cert-run-runbook.md`. The abridged steps below stay here for
> quick reference.

1. ✅ On the pinned platform (`certification-platform.md` Stage 0 tuple), run the capstone
   scenario under the perf harness for `BaselineSampleCount` (= 100) runs. — Done 2026-07-19.
2. ⏳ Record the measured per-tick `p50`/`p99` (ms) and the full `SessionManifest` — Done except
   `EnvironmentFingerprint.FloatModelHash` (see OPEN GAP above).
3. ⏳ Replace the metric placeholders above; flip Status to **CERTIFIED**. — Blocked on the gap.
4. ⏳ In code, swap the `Pending(...)` entry for `CertifiedPerfBaseline.Certified(manifest, loop,
   p50, p99, "FR-PO-052")` and unskip / extend the certified-projection assertions. — Blocked;
   `Certified(...)` should not be called with a fabricated `FloatModelHash`.
5. ⏳ Obtain Platform Certification owner sign-off (Spec #16 §1.7 Governance Artifacts) — sign-off
   must additionally resolve the `FloatModelHash` gap, not just approve the measured numbers.

## Version History

| Version | Date       | Author | Notes                                                        |
|---------|------------|--------|--------------------------------------------------------------|
| 1.0     | 2026-06-28 | —      | Created PENDING. First corpus entry under `baselines/`.       |
| 1.1     | 2026-07-19 | —      | Real 100-run capture on the pinned Windows 11 / Unity 6000.4.9f1 host: p50=0.4768ms / p99=2.5669ms, full SessionManifest except `EnvironmentFingerprint.FloatModelHash`. Status stays PENDING — §4.8.3's `floatModelHash` tuple assumes native AOT compilation and has no defined meaning for this project's Mono/JIT Stage-0 backend; flagged as an open gap requiring a Platform Certification owner decision rather than fabricated. Code-side entry unchanged (`CertifiedPerfBaseline.Pending(...)`; `CertificationStatus` has no partial state). |
