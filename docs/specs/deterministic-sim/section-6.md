# Deterministic Simulation Specification #16 — Section 6: Performance Analysis

## 6.1 Complexity Analysis
- Canonical sorting overhead: `O(n log n)` per authoritative collection per phase (unless pre-sorted index maintained).
- Digest generation overhead: linear in serialized scope size.
- Replay from checkpoint: `O(snapshot_size + input_count_after_T)`.

## 6.2 Budget Context
Determinism instrumentation must not invalidate runtime targets.
Budgets are applied by verbosity tier (minimal, standard, forensic).

## 6.3 Performance Targets
- Standard determinism checks: <= 3% CPU overhead.
- Forensic trace mode: <= 10% CPU overhead (non-shipping diagnostic runs).
- Additional memory overhead for deterministic metadata: <= 5% of authoritative state footprint.
- Artifact size budget per match trace: <= 128 MB compressed.

## 6.4 Profiling Plan
- Benchmark with representative scenario corpus at fixed tick budget.
- Collect per-phase timings with and without determinism instrumentation.
- Track digest and trace throughput by platform.
- Block release when overhead exceeds approved thresholds.

## 6.5 Retention & Storage Policy
- CI keeps standard traces for 14 days.
- CI keeps forensic traces for 30 days for failing builds only.
- Compression algorithm and level MUST be fixed in build config to ensure reproducible artifact hashes.

## 6.6 Version History
- **v0.4:** Added concrete artifact cap and retention requirements.
- **v0.3:** Added deterministic observability budget targets and enforcement plan.

## 6.7 Benchmark Suite Definition
Benchmark suite MUST include:
- low-entity scenario,
- median production scenario,
- stress scenario with peak entity count.

For each suite entry capture:
- mean tick time,
- p95 tick time,
- digest time share,
- snapshot serialization time share,
- trace write throughput.

## 6.8 Performance Regression Policy
If CPU overhead exceeds target by >1 percentage point for two consecutive CI runs:
1. mark as regression,
2. open blocking performance issue,
3. require owner acknowledgement and mitigation plan.

## 6.9 Storage Cost Examples
At 60Hz:
- standard traces should stay within capped compressed size,
- forensic traces may exceed standard mode but MUST obey retention constraints.

Note: Section 6.10 phase shares are baseline without instrumentation slack; 3-10% observability overhead is budgeted separately.

## 6.10 Phase-Level Budget Allocation (Guideline)
**Reading rule (normative).** Each value below is a **per-phase upper bound on the indicated tick class** — NOT a flat per-tick budget. Rows that fire only on certain ticks are explicitly marked. Non-stride-tick slack (when the AI row does not consume) is left as runtime headroom and SHOULD remain idle; it MUST NOT be reallocated to other phases. (Pass 4 L-5.)

| Phase | CPU budget share (target) | Tick class | Notes |
|---|---|---|---|
| Input + Intent | 8% | every tick | parsing and intent mapping |
| AI | 22% | stride tick only (every 6th tick at 10 Hz) | On the other 5 ticks `AI_NoOp` runs with near-zero cost. Tick-averaged AI budget ≈ 3.7% (22% / 6). The 22% figure MUST NOT be used as a flat per-tick budget. |
| Physics | 34% | every tick | usually highest compute share |
| Resolve + Events | 18% | every tick | conflict resolution + event ledger |
| Snapshot + Digest (steady state) | 12% | every tick | per-tick `PhaseDigest` computation (§3.2.2) and snapshot serialization for in-memory ring buffer; does NOT include durable-save commit |
| Save commit (scheduled) | ≤ 6% | save-cadence ticks only | `SnapshotStore.CommitAtomic` (§4.6.1.1): fsync, atomic rename, directory fsync. Spikes on save ticks; averaged over a save cadence of `N` ticks the contribution is `6% / N`. The 18% figure previously combined this row with the steady-state row; that combined view is now an averaging artifact, not a budget. (Pass 5 L-6.) |

## 6.11 Performance Failure Triage Procedure
1. detect budget violation in CI.
2. identify dominant phase by timing traces.
3. isolate regression commit range via bisect.
4. rerun with forensic trace mode to confirm root cause.
5. assign owner and remediation ETA.

## 6.12 Quantitative Acceptance Thresholds
- p95 tick time increase > 5% over baseline for same corpus => fail.
- snapshot/digest phase cost increase > 10% => requires explicit waiver.
- retention artifact cap breach in standard mode => fail certification run.

## 6.13 Version History
- **v1.0 (May 4, 2026):** Pass 4 / Pass 5 critique. (a) Pass 4 L-5: §6.10 reading rule made explicit — values are per-phase upper bounds on the indicated tick class, not flat per-tick. Non-stride slack is idle, not reallocated. (b) Pass 5 L-6: `Snapshot + Digest 18%` split into `Snapshot + Digest (steady state) 12%` (every tick — phase digest + ring-buffer serialize) and `Save commit (scheduled) ≤ 6%` (save-cadence ticks only — atomic-write contract). The 18% combined figure was an averaging artifact masking where cost actually lands.
- **v0.8 (May 2, 2026):** §6.10 AI row updated to clarify that 22% is a per-stride-tick budget; tick-averaged AI budget ≈ 3.7% (A-1).
- **v0.6:** Added phase-level budget allocation, triage procedure, and quantitative acceptance thresholds.
