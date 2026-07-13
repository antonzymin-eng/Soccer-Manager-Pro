# Certification Run Runbook

**Created:** July 4, 2026
**Purpose:** The operator checklist for executing the first Stage-0 determinism +
performance certification run on the pinned host platform, and promoting the
resulting number from `PENDING_CERT_RUN` to `CERTIFIED`. Ties together
`certification-platform.md` (the platform pin), the `CertifiedPerfBaseline`
corpus machinery, and the Match Engine Phase-F capstone scenario.

**Owner:** Platform Certification owner (sign-off per Deterministic Simulation
#16 §1.7 Governance Artifacts).

---

## Status: BLOCKED (prep complete)

This runbook is **ready to execute** but the run itself is blocked on two hard
prerequisites that do not exist in the current tree:

| # | Prerequisite | Why it blocks | Tracked in |
|---|--------------|---------------|------------|
| P1 | **Unity host to execute the harness** | *(Partially cleared 2026-07-13.)* The real harness now EXISTS: `StopwatchPerfHarness` + `MatchEngineCapstonePerfHarness` boot the real `MatchEngine` and Stopwatch-time each `RunTick` (superseding the synthetic `tools/perf-harness/run.sh` `p50=0.000` stub), and the Unity batch-mode command is now DEFINED (`src/CLAUDE.md` → "BUILD AND TEST COMMANDS"; Step 2 below). The harness runs on the Linux `dotnet-ci` gate as a non-certifying compile+execute proof. What remains: a **Unity host** to run that command so the capture executes under the pinned Mono runtime (folds into P2). | `src/CLAUDE.md` |
| P2 | **Access to the pinned host** | A certified number MUST be captured on the exact tuple in `certification-platform.md` v1.3 (Unity 6000.4.9f1, DX11 — target pin, not yet certified). The Linux compile/test gate (`tools/dotnet-ci`) is explicitly NON-certifying — a number sourced from it would be a fabricated certification. | `certification-platform.md` |

Everything else — the platform pin, the corpus entry, the code seam, the
capstone scenario, and the perf-gate wiring — is in place. When P1 and P2 clear,
follow the steps below; no further scaffolding is required.

---

## What gets certified

The first cert-run target is the Match Engine capstone:

| Field | Value |
|-------|-------|
| Scenario | `MatchEngineCapstoneScenarios.KickoffMultiSecondPath` |
| Scenario manifest ID | `tests/scenarios/cross-spec/match-engine-kickoff-multi-second` |
| Loop | `PhysicsSixtyHz` (`LOOP-PHYSICS-60HZ`) |
| Threshold cited | `FR-PO-052` (per-tick budget) |
| Seed | `MatchEngineCapstoneScenarios.KickoffMultiSecondSeed` |
| Sample count | `PerformanceOptimizationConstants.BaselineSampleCount` (= 100) |
| Corpus artifact | `docs/specs/performance-optimization/baselines/match-engine/kickoff-multi-second.cert.md` |
| Code entry | `CertifiedPerfBaseline.Pending(...)` in `src/performance-optimization/CertifiedPerfBaseline.cs` / `src/match-engine/tests/CertifiedPerfBaselineTests.cs` |

The capstone (`MatchEngineCapstoneTests.cs`) already activates the FR-PO-052
per-tick gate via `PerfGateRunner.Run` against a **generous in-code anchor**
baseline — that proves the gate *wiring*, not the *budget*. This runbook replaces
that anchor with a real certified number.

---

## Step 0 — Pre-flight: verify the host matches the pin

On the certification host, confirm every row of `certification-platform.md`
v1.3 before capturing anything. A mismatch on any row invalidates the run
(`EnvironmentFingerprint` mismatch → `ERR_DS_REPLAY_ENV_MISMATCH`, #16 §4.8).

> **Note (2026-07-13):** `certification-platform.md` v1.3 bumped the target pin
> to Unity **6000.4.9f1** (DX11) — a MAJOR version bump from the 2022.3.62f1
> tuple this runbook and the `P2` prerequisite were originally written against.
> No cert run has ever executed (P1/P2 below still block), so nothing is
> invalidated in-flight.
>
> **Update (2026-07-13, later same day):** the code follow-up this note flagged
> is now DONE — `CertifiedPerfBaseline.Stage0CertPlatformPin`
> (`src/performance-optimization/CertifiedPerfBaseline.cs`) and its guard test
> (`CertifiedPerfBaselineTests.PlatformPinTokens_MatchDocumentedTuple`) now
> encode `win11-unity6000.4.9f1-dx11-mono-x64-sse4.2-1w-detflags`. The token is
> updated so a first cert run stamps the correct pin; it does NOT assert a
> certified baseline exists (P1/P2 still block). Step 0 and Step 2 below cite the
> new value.

| Pin row | Required value | How to verify |
|---------|---------------|---------------|
| OS | Windows 11 | `winver` |
| Unity version | 6000.4.9f1 | Unity Hub → installed editors; or `Editor\Data\...\ProjectVersion.txt` |
| Graphics API | DX11 | Project Settings → Player → Other Settings → Graphics APIs |
| Backend | Mono | Project Settings → Player → Configuration → Scripting Backend |
| CPU arch | x64 | `wmic cpu get Architecture` (9 = x64) |
| SIMD level | SSE4.2 baseline, no AVX/AVX2/FMA | confirm build defines exclude AVX intrinsics; CPU-Z for the CPU feature set |
| Worker threads | 1 (single-threaded) | confirm no job-system parallelism enabled in the run config |
| Compiler flags | DAZ off · FTZ off · fp-contract off · FMA off | project build config; `/fp:strict`-equivalent for Mono |

Record the CPU model, physical core count, and thermal state — these become the
`HardwareCounterSnapshot` fields (all three are required; a default snapshot
fails `SessionManifest.IsComplete()`).

If any row differs and the difference is a legitimate host change (e.g. a Unity
patch bump), **stop** and update `certification-platform.md` with Platform
Certification owner sign-off *before* running (see that file's "Maintenance
Rule"). Do not certify against an unpinned tuple.

---

## Step 1 — Pin the build and the seed

1. Check out the exact commit to certify; capture its full 40-hex `git rev-parse HEAD`
   (→ `SessionManifest.GitSha`).
2. Use `MatchEngineCapstoneScenarios.KickoffMultiSecondSeed` verbatim
   (→ `SessionManifest.Seed`). Do not re-roll it — the certified baseline and
   every future FR-PO-031 comparison run must share the seed (`PerfGateRunner`
   rejects a seed mismatch).
3. Confirm a clean working tree (`git status`) so `GitSha` unambiguously
   identifies the measured bits.

---

## Step 2 — Capture (100 runs on the pinned host)

> **Update (2026-07-13):** the real harness now EXISTS (P1 Tier-A landed) —
> `StopwatchPerfHarness` (`src/performance-optimization/`) + the capstone runner
> `MatchEngineCapstonePerfHarness` (`src/match-engine/tests/`) boot the real
> `MatchEngine` and Stopwatch-time each `RunTick`, so this step is no longer
> blocked on writing the harness. The concrete host invocation is below (also in
> `src/CLAUDE.md` "BUILD AND TEST COMMANDS"). What still blocks a *certified*
> number is P2 (pinned-host access) — the Linux run of this same harness is
> NON-certifying.

Concrete host command (Unity Test Framework batch mode; set the run count to the
full `BaselineSampleCount`). The pinned host is Windows 11, so set the env var
first with `set TD_PERF_RUN_COUNT=100` (cmd) or `$env:TD_PERF_RUN_COUNT=100`
(PowerShell) — the inline `VAR=value command` prefix shown here is POSIX-shell
form for reference:

```bash
TD_PERF_RUN_COUNT=100 Unity -batchmode -runTests -projectPath . \
      -testPlatform EditMode \
      -testFilter "TacticalDirector.MatchEngine.MatchEngineCapstonePerfHarnessTests" \
      -testResults ./perf-results.xml -logFile -
```

1. Build the player / test assembly for the pinned Mono/x64 config with the
   determinism compiler flags above (creating the `Assets/Scripts` junction into
   `src/` first — see `Assets/README.md`).
2. Run the capstone scenario under the perf harness for `BaselineSampleCount`
   (= 100) runs (`TD_PERF_RUN_COUNT=100`), sampling per-tick wall time across the
   600-tick (10 s @ 60 Hz) run. `MatchEngineCapstonePerfHarnessTests` logs the
   measured per-tick `p50`/`p99` via `TestContext`.
3. Record the measured per-tick **p50** and **p99** (ms) — finite, positive,
   with `p99 ≥ p50` (the `Certified(...)` factory fails closed otherwise).
4. Capture the full `SessionManifest`:
   - `GitSha` (Step 1), `Seed` (Step 1)
   - `EnvironmentFingerprint` — the locked 6-field snapshot (#16 §4.8), not
     `CreateStage0Dev()`
   - `PlatformPin` = `CertifiedPerfBaseline.Stage0CertPlatformPin`
     (`win11-unity6000.4.9f1-dx11-mono-x64-sse4.2-1w-detflags`)
   - `ScenarioManifestId` = `tests/scenarios/cross-spec/match-engine-kickoff-multi-second`
   - `SessionStartUtc` / `SessionEndUtc` (RFC 3339)
   - `HardwareCounters` (CPU model, core count > 0, thermal state — Step 0)
   - `HarnessVersion`
5. Sanity-check determinism: two same-seed runs must produce byte-identical
   `CurrentSnapshotDigest` chains (the capstone's two-run determinism assertion
   already covers this; re-confirm on the pinned host).

---

## Step 3 — Promote PENDING → CERTIFIED

1. **Artifact** — `docs/specs/performance-optimization/baselines/match-engine/kickoff-multi-second.cert.md`:
   - replace the `p50` / `p99` `_PENDING_` placeholders with the measured values,
   - flip **Status** `PENDING_CERT_RUN` → `CERTIFIED`,
   - append the `SessionManifest` (git SHA, fingerprint, hardware counters,
     timestamps, harness version) and a Version-History row.
2. **Code** — swap the corpus entry from
   `CertifiedPerfBaseline.Pending(scenarioManifestId, loop, platformPin, thresholdCited)`
   to
   `CertifiedPerfBaseline.Certified(manifest, loop, p50Ms, p99Ms, "FR-PO-052")`,
   then unskip / extend the certified-projection assertions in
   `CertifiedPerfBaselineTests.cs` (the `TryBuildBaselineRecord` → `PerfGateRunner`
   self-compare path).
3. **Gate** — point the capstone's `PerfGateRunner.Run` baseline at the certified
   `BaselineRecord` (via `TryBuildBaselineRecord`) instead of the generous in-code
   anchor, so FR-PO-031 (+5%) now regresses against the real budget.
4. Run the full suite locally on the pinned host and confirm green.

---

## Step 4 — Sign-off

Obtain **Platform Certification owner** sign-off (Deterministic Simulation #16
§1.7 Governance Artifacts). Record the sign-off in the `.cert.md` Version History
and close the `certification-platform.md` downstream unblocker rows that were
gated on a first cert run (`FR-DS-009-GATE` Stage 0 activation; `FR-PO-052`
Stage 0+1 perf-gate).

---

## Version History

| Version | Date       | Author | Notes                                                            |
|---------|------------|--------|------------------------------------------------------------------|
| 1.0     | 2026-07-04 | —      | Initial runbook. Prep complete; run blocked on Unity project init |
|         |            |        | (P1) + pinned-host access (P2). Steps 0–4 authored against the    |
|         |            |        | existing pin + CertifiedPerfBaseline seam + Phase-F capstone.     |
| 1.1     | 2026-07-13 | —      | Step 0 pre-flight table updated for the `certification-platform.md` |
|         |            |        | v1.3 target pin bump (Unity 6000.4.9f1, DX11 row added). Flagged   |
|         |            |        | that `CertifiedPerfBaseline.Stage0CertPlatformPin` still hardcodes |
|         |            |        | the superseded `win11-unity2022.3.62f1-...` string and needs a    |
|         |            |        | code change before Step 2 can be executed against the new pin.    |
| 1.2     | 2026-07-13 | —      | Code follow-up DONE: `CertifiedPerfBaseline.Stage0CertPlatformPin` |
|         |            |        | + its guard test now encode                                       |
|         |            |        | `win11-unity6000.4.9f1-dx11-mono-x64-sse4.2-1w-detflags`. Step 0  |
|         |            |        | note + Step 2 citation updated to the new pin. P1/P2 still block   |
|         |            |        | an actual cert run; token update asserts no certified baseline.   |
| 1.3     | 2026-07-13 | —      | P1 harness LANDED (Tier A): `StopwatchPerfHarness` (concrete       |
|         |            |        | IPerfHarness, §3.3.5) + `MatchEngineCapstonePerfHarness` boot the  |
|         |            |        | real MatchEngine and Stopwatch-time each RunTick, superseding the  |
|         |            |        | synthetic run.sh stub. Step 2 now carries the concrete Unity       |
|         |            |        | batch-mode command; P1 row rewritten (harness exists; only a Unity |
|         |            |        | host remains, folding into P2). Linux run of the harness is        |
|         |            |        | NON-certifying; no certified number produced.                     |
