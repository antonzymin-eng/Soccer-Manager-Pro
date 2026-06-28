# Certified Perf Baseline — `match-engine-kickoff-multi-second`

**Created:** June 28, 2026
**Status:** ⏳ **PENDING_CERT_RUN** — no measurement on the pinned certification platform yet.
**Spec:** Performance Optimization Strategy #18 §3.4.4 / §4.3.2 / FR-PO-031 / FR-PO-052;
`docs/tracking/certification-platform.md` (Stage 0 host pin).

This is the FR-PO-052 per-tick certified baseline corpus entry for the Match Engine capstone
scenario (`MatchEngineCapstoneScenarios.KickoffMultiSecondPath`). It is the authoritative
per-PR regression reference (FR-PO-031, +5%) **once captured on the pinned platform**.

The code-side entry is `CertifiedPerfBaseline.Pending(...)` (see
`src/performance-optimization/CertifiedPerfBaseline.cs` and
`src/match-engine/tests/CertifiedPerfBaselineTests.cs`).

## Intent (known now)

| Field | Value |
|-------|-------|
| Scenario manifest ID | `tests/scenarios/cross-spec/match-engine-kickoff-multi-second` |
| Loop | `PhysicsSixtyHz` (`LOOP-PHYSICS-60HZ`) |
| Threshold cited | `FR-PO-052` |
| Platform pin | `win11-unity2022.3.62f1-mono-x64-sse4.2-1w-detflags` |
| Seed | `MatchEngineCapstoneScenarios.KickoffMultiSecondSeed` |

## Metrics (PENDING)

| Field | Value |
|-------|-------|
| p50 (ms/tick) | `_PENDING_` |
| p99 (ms/tick) | `_PENDING_` |

> No number is recorded here. The Linux compile/test gate (`tools/dotnet-ci`) is explicitly
> **NON-certifying** (`certification-platform.md` "Relationship to the Linux compile/test gate"):
> a number sourced from it is not authoritative, so recording one would be a fabricated
> certification. The capstone test on the Linux gate proves the perf-gate **wiring** only,
> against a generous in-code anchor — not this certified budget.

## Runbook — promoting PENDING → CERTIFIED

1. On the pinned platform (`certification-platform.md` Stage 0 tuple), run the capstone scenario
   under the perf harness for `BaselineSampleCount` (= 100) runs.
2. Record the measured per-tick `p50`/`p99` (ms) and the full `SessionManifest`
   (git SHA, `EnvironmentFingerprint`, hardware counters, timestamps, harness version).
3. Replace the metric placeholders above; flip Status to **CERTIFIED**.
4. In code, swap the `Pending(...)` entry for `CertifiedPerfBaseline.Certified(manifest, loop,
   p50, p99, "FR-PO-052")` and unskip / extend the certified-projection assertions.
5. Obtain Platform Certification owner sign-off (Spec #16 §1.7 Governance Artifacts).

## Version History

| Version | Date       | Author | Notes                                                        |
|---------|------------|--------|--------------------------------------------------------------|
| 1.0     | 2026-06-28 | —      | Created PENDING. First corpus entry under `baselines/`.       |
