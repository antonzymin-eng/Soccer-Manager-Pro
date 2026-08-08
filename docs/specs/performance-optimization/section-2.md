# Performance Optimization Strategy Specification #18 — Section 2: Functional Requirements & Budget Governance Model

**Created:** May 13, 2026
**Last Updated:** May 14, 2026 (v0.3 PASS-2 adversarial-review fix pass)
**Purpose:** Publishes the FR-PO-### functional-requirement catalogue,
the conformance-level grammar, the failure-to-comply modes, the
data-structure inventory (informational; Spec #18 declares no runtime
gameplay structures), and the failure-mode catalogue specific to this
spec.

---

## 2.1 Conformance Levels

This spec uses **MUST / SHOULD / MAY** per RFC 2119. Each FR row in
§2.2 carries one of these three levels.

**Exception with sign-off** semantics are identical to Spec #20 §2.1
and Spec #19 §2.1: a MUST may be waived for a specific PR or release
only with explicit lead-developer sign-off recorded in `spec-error-
log.md`. Silent waiver is itself a violation. Per-PR perf-gate
exceptions follow the same procedure (see §3.5.5).

## 2.2 Functional Requirement Catalogue

Every FR-PO-### below is normative. Source citation names the
authoritative KD or section that justifies the rule; verification
pointer (§5.x) names the conformance-verification mechanism in §5.
**Activation stage** column reflects KD-5 stage-gating: rows marked
"Stage 0" apply to spec-writing today; rows marked "Stage 0+1" activate
when `src/` and CI infrastructure exist; rows marked "Stage 1" activate
when the dashboard front-end ships.

### 2.2.1 Budget roll-up authority & per-spec §6 schema (FR-PO-001 … 008, KD-2)

| ID | Statement | Level | Source | Verify | Stage |
|----|-----------|-------|--------|--------|-------|
| FR-PO-001 | Each per-spec §6 (or §4.5 in Shot Mechanics #6's case) is the authoritative source for that spec's per-tick / per-frame budget. Spec #18 ratifies; it MUST NOT override. | MUST | KD-2 | §5.3 | Stage 0 |
| FR-PO-002 | Every per-spec §6 MUST publish total per-tick budget (ms), per-tick budget by loop tag, allocation budget, worst-case input parameters, and headroom multiplier per the Appendix B schema. | MUST | KD-2 / KD-8 / KD-10 | §5.3 | Stage 0 |
| FR-PO-003 | Spec #18 MUST maintain a single read-only roll-up table per platform target in Appendix C. | MUST | KD-2 | §5.3 | Stage 0 |
| FR-PO-004 | Roll-up table updates MUST be mechanical sync from per-spec §6; Spec #18 MUST NOT introduce design decisions during roll-up. | MUST | KD-2 | §5.3 | Stage 0 |
| FR-PO-005 | When §3.1.3 roll-up total exceeds §3.1.4 headroom on any platform target, Spec #18 MUST invoke the §3.1.5 re-allocation procedure. Silent truncation is forbidden. | MUST | KD-2 | §5.3 | Stage 0+1 |
| FR-PO-006 | New specs adopting the §6 schema MUST be schema-conforming on first draft; §9 approval MUST be blocked otherwise. | MUST | KD-2 | §5.3 | Stage 0 |
| FR-PO-007 | Approved specs (#1–#8, #17) are surveyed at Stage 0 (Appendix D); gaps are logged as `ERR-018-NNN` rows in `spec-error-log.md`; remediation happens at next natural revision of each spec (grandfather rule). | MUST | KD-2 | §5.3 | Stage 0 |
| FR-PO-008 | Re-allocation review MUST produce a version-history entry on every affected spec's §6 and an atomic update to Spec #18 §3.1.3 roll-up table. | MUST | KD-2 | §5.3 | Stage 0+1 |

### 2.2.2 Loop separation tagging (FR-PO-009 … 015, KD-8)

| ID | Statement | Level | Source | Verify | Stage |
|----|-----------|-------|--------|--------|-------|
| FR-PO-009 | Every budget number in every per-spec §6 MUST carry one of `[LOOP-TACTICAL-10HZ]` or `[LOOP-PHYSICS-60HZ]` loop tags. | MUST | KD-8 | §5.5 | Stage 0 |
| FR-PO-010 | Untagged budget numbers MUST be rejected by the §5.5 loop-tag auditor. | MUST | KD-8 | §5.5 | Stage 0 |
| FR-PO-011 | Cross-loop subsystems (e.g., Decision Tree #8 reading 60 Hz state from a 10 Hz loop) MUST declare separate budgets for the work each loop performs. | MUST | KD-8 | §5.5 | Stage 0 |
| FR-PO-012 | The 60 Hz budget total MUST include only 60 Hz-tagged entries; the 10 Hz budget total MUST include only 10 Hz-tagged entries. | MUST | KD-8 | §5.5 | Stage 0+1 |
| FR-PO-013 | Mixed-loop budget totals are forbidden. | MUST | KD-8 | §5.5 | Stage 0+1 |
| FR-PO-014 | Per-second budgets (ambiguous between 10 Hz × 10 and 60 Hz × 60) MUST NOT be used; budgets are stated per-tick or per-frame. | MUST | KD-8 | §5.5 | Stage 0 |
| FR-PO-015 | Per-call budgets without amortized call rate MUST NOT be used. | MUST | KD-8 | §5.5 | Stage 0 |

### 2.2.3 Profiling methodology, determinism-bound (FR-PO-016 … 023, KD-6)

| ID | Statement | Level | Source | Verify | Stage |
|----|-----------|-------|--------|--------|-------|
| FR-PO-016 | Every profiling session MUST record git SHA, recorded seed, `EnvironmentFingerprint` (per #16 §4.8), platform pin (per KD-9), scenario manifest ID (per #16 §5), session start/end timestamps, and hardware perf-counter snapshot. | MUST | KD-6 | §5.4 | Stage 0+1 |
| FR-PO-017 | Sessions missing any field required by FR-PO-016 MUST be rejected by the §3.4.4 baseline validator. | MUST | KD-6 / KD-11 | §5.4 | Stage 0+1 |
| FR-PO-018 | Spec #18 MUST NOT author its own perf scenarios; every profiling session runs an #16 §5 scenario verbatim. | MUST | KD-3 / KD-6 | §5.7 | Stage 0+1 |
| FR-PO-019 | Cross-scenario profiling (Spec #19 KD-8 cross-spec scenarios) is permitted. | MAY | KD-6 | §5.4 | Stage 0+1 |
| FR-PO-019a | For any cross-scenario profiling session entered into the baseline corpus, the manifest ID and seed MUST be recorded per FR-PO-016. | MUST | KD-6 | §5.4 | Stage 0+1 |
| FR-PO-020 | Wall-clock-seeded or random-seed profiling runs MUST NOT be entered into the baseline corpus. | MUST | KD-6 | §5.4 | Stage 0+1 |
| FR-PO-021 | Profiling MUST NOT be performed in editor-mode without scripting-backend pin (Mono vs IL2CPP differences invalidate comparison). | MUST | KD-6 / KD-9 | §5.4 | Stage 0+1 |
| FR-PO-022 | Sampling-profiler default cadence is 1 kHz wall-clock samples (`[EST]`, pinned at Stage 0+1 §7.5 D1). | SHOULD | KD-6 | §5.4 | Stage 0+1 |
| FR-PO-023 | Stage 0 sessions MUST use the manual `Stopwatch` harness described in Appendix E; profiler pin (§7.5 D1) is deferred. | MUST | KD-5 / KD-6 | §5.4 | Stage 0 |

### 2.2.4 Optimization ladder (FR-PO-024 … 030)

| ID | Statement | Level | Source | Verify | Stage |
|----|-----------|-------|--------|--------|-------|
| FR-PO-024 | Every optimization MUST proceed through the five rungs in order: Measure → Attribute → Fix → Verify → Lock. | MUST | §3.4.1 | §5.6 | Stage 0+1 |
| FR-PO-025 | Optimization PRs without pre-fix baseline evidence MUST be blocked at review. | MUST | §3.4.2 | §5.6 | Stage 0+1 |
| FR-PO-026 | Improvement claims MUST report N samples with a non-overlapping confidence interval against the pre-fix baseline (N pinned at Stage 0+1, §7.5 D8). | MUST | §3.4.3 | §5.6 | Stage 0+1 |
| FR-PO-027 | Below-significance improvements MUST NOT be entered into the baseline; they are recorded as §6.4 "Inconclusive" defects. | MUST | §3.4.3 | §5.6 | Stage 0+1 |
| FR-PO-028 | Post-fix baselines MUST be re-captured under the same scenario, seed, and platform pin as the pre-fix baseline. | MUST | §3.4 / KD-11 | §5.4 | Stage 0+1 |
| FR-PO-029 | Locked baselines MUST trigger an atomic update to the §3.1.3 roll-up table and Appendix C. | MUST | §3.4 | §5.4 | Stage 0+1 |
| FR-PO-030 | Optimization tickets MUST reference the FR-PO ID of the gate addressed, the pre-fix baseline SHA, and the target metric improvement; closed tickets MUST reference the post-fix baseline SHA. | MUST | §3.4.5 | §5.6 | Stage 0+1 |

### 2.2.5 Performance regression gates (FR-PO-031 … 040, KD-3 / KD-4)

| ID | Statement | Level | Source | Verify | Stage |
|----|-----------|-------|--------|--------|-------|
| FR-PO-031 | Default per-PR regression threshold: post-PR baseline MUST be within +5% per spec, per loop, of the pre-PR baseline for the same scenario, seed, and platform pin. `[GT]` pinned at Stage 0+1 §7.5 D9. | MUST | §3.5.2 | §5.6 | Stage 0+1 |
| FR-PO-032 | Any non-zero allocation on a hot-path entry (KD-10 union) MUST block merge regardless of magnitude. | MUST | KD-10 / §3.7 | §5.6 | Stage 0+1 |
| FR-PO-033 | Per-spec §6 MAY declare a tighter threshold than FR-PO-031; tighter thresholds MUST cite the owning §6 authority. | MAY | KD-2 / §3.5.2 | §5.6 | Stage 0+1 |
| FR-PO-034 | The functional gate (Spec #19 §6.2,) MUST block on test fail. Out of #18's authority; declared for gate-composition completeness. | MUST | KD-4 | §5.7 | Stage 0+1 |
| FR-PO-035 | The determinism gate (Spec #16 §5 + §3.2.4.1,) MUST block on bitwise mismatch against the canonical-record-format golden trace. Out of #18's authority; declared for gate-composition completeness. | MUST | KD-3 | §5.7 | Stage 0+1 |
| FR-PO-036 | The performance gate (this spec §3.5) MUST block on FR-PO-031 threshold exceeded. | MUST | §3.5 | §5.6 | Stage 0+1 |
| FR-PO-037 | The allocation gate (this spec §3.7) MUST block on FR-PO-032 violation. | MUST | §3.7 | §5.6 | Stage 0+1 |
| FR-PO-038 | No gate MAY be configured as "soft"; flake-quarantine (Spec #19 §3.7) applies to functional gates only — perf-gate variance exceeding §3.5.2 threshold is treated as a potential KD-6 violation, triggers root-cause analysis per §6.4, and confirmed non-determinism routes to #16 §5 triage. | MUST | KD-4 / KD-6 | §5.7 | Stage 0+1 |
| FR-PO-039 | An absolute-threshold guard MUST compare against the milestone baseline independently of per-PR delta. Drift beyond +10% (`[GT]`) of milestone baseline MUST block merge regardless of incremental delta history. | MUST | §3.5.6 | §5.6 | Stage 0+1 |
| FR-PO-040 | Perf-gate exceptions MUST follow the §2.1 exception-with-sign-off procedure; silent threshold bypass is forbidden. | MUST | §2.1 / §3.5.5 | §5.7 | Stage 0+1 |

### 2.2.6 Degradation policy — Tier C only (FR-PO-041 … 047, KD-7)

| ID | Statement | Level | Source | Verify | Stage |
|----|-----------|-------|--------|--------|-------|
| FR-PO-041 | Tier A authoritative outputs (ball state, agent position, agent decision, event emission) MUST NOT vary under performance pressure. | MUST | KD-7 / #16 §1.3 | §5.7 | Stage 0 |
| FR-PO-042 | Proposed degradation paths that touch a Tier A output MUST be rejected at spec review. | MUST | KD-7 | §5.7 | Stage 0 |
| FR-PO-043 | Tier B bounded-authoritative degradation paths MAY exist within the owning spec's declared tolerance band; the path MUST be declared at spec time, not adopted at runtime. | MAY | KD-7 | §5.7 | Stage 0 |
| FR-PO-044 | Tier B tolerance bands MUST be cited from the owning spec; Spec #18 MUST NOT republish them. | MUST | KD-1 / KD-7 | §5.7 | Stage 0 |
| FR-PO-045 | Tier C degradation paths (render LOD, debug overlay fidelity, telemetry sampling rate, dashboard refresh frequency) are permitted; they MUST be itemized in the §3.6.4 table. | MAY | KD-7 | §5.7 | Stage 1 |
| FR-PO-046 | Stage 0 declares NO dynamic degradation paths at all; Stage 0 budget enforcement is manual remediation. | MUST | KD-5 / KD-7 | §5.7 | Stage 0 |
| FR-PO-047 | Stage 1 adaptive-degradation posture (any Tier B / Tier C runtime fallbacks?) is a deferred decision (§7.5 D5). | INFORMATIVE | KD-7 | §5.7 | Stage 1 |

### 2.2.7 Hot-path enumeration & zero-allocation enforcement (FR-PO-048 … 053, KD-10)

| ID | Statement | Level | Source | Verify | Stage |
|----|-----------|-------|--------|--------|-------|
| FR-PO-048 | The set of hot paths MUST be the union of every approved spec's §6 budget table. No separate authoritative hot-path list is maintained. | MUST | KD-10 | §5.6 | Stage 0+1 |
| FR-PO-049 | The hot-path union MUST be materialized at build time as `tools/hot-path-union.json` (Stage 0+1; Stage 0 placeholder structure in Appendix D). | MUST | KD-10 | §5.6 | Stage 0+1 |
| FR-PO-050 | Every hot-path entry MUST declare allocation budget = 0 bytes per tick. | MUST | KD-10 / CLAUDE.md | §5.6 | Stage 0+1 |
| FR-PO-051 | The per-build allocation tracker MUST diff against the FR-PO-049 union; non-zero allocations in a union method MUST block merge. | MUST | KD-10 | §5.6 | Stage 0+1 |
| FR-PO-052 | Enforcement runs on the IL2CPP build per `certification-platform.md` Stage 0 row (`TBD` pin); editor-mode (Mono) runs are not enforcement-grade. | MUST | KD-9 / KD-10 | §5.4 | Stage 0+1 |
| FR-PO-053 | One-shot allocations exempt via `[HotPathAllocExempt]` (governance identifier declared in Spec #18 §3.7.5; C# attribute definition deferred to Stage 0+1 per KD-5; zero-allocation mandate cites Spec #20 §3) MUST cite a rationale and require lead-developer sign-off. | MUST | KD-10 | §5.6 | Stage 0+1 |

### 2.2.8 Trace pipeline & dashboard mechanics (FR-PO-054 … 062, KD-3 inverted)

| ID | Statement | Level | Source | Verify | Stage |
|----|-----------|-------|--------|--------|-------|
| FR-PO-054 | Spec #18 owns the trace pipeline architecture: channel registry, verbosity tiers, sampling rules, channel-to-sink routing, instrumentation API. | MUST | KD-3 | §5.7 | Stage 0 |
| FR-PO-055 | Verbosity tiers MUST be `minimal`, `standard`, `debug`, `exhaustive`; numeric semantics (sampling rate per tier) pinned at Stage 0+1 (§7.5 D10). | MUST | §3.8.2 | §5.7 | Stage 0+1 |
| FR-PO-056 | Sampling rules per tier MUST be: every-tick (exhaustive), per-N-ticks (standard / debug), event-driven only (minimal). N pinned at Stage 0+1. | MUST | §3.8.2 | §5.7 | Stage 0+1 |
| FR-PO-057 | Channel-to-sink routing MUST include in-memory ring buffer (default), file sink (baseline-capture builds), and a Stage 1+ network sink. | SHOULD | §3.8.2 | §5.7 | Stage 0+1 |
| FR-PO-058 | Every trace record emitted via a #18-owned channel MUST conform to the canonical record format at #16 §3.2.4.1. | MUST | KD-3 / KD-11 | §5.7 | Stage 0+1 |
| FR-PO-058a | Every trace point MUST be determinism-clean: no wall-clock-derived field, no `System.Random` field, no managed allocation on hot-path tick code, no field that captures `EnvironmentFingerprint`-divergent data (CPU brand string, locale, etc.). Trace points inside the canonical tick pipeline (#16 §3.1,) additionally require #16-owner sign-off (emission-veto authority). | MUST | KD-3 | §5.7 | Stage 0+1 |
| FR-PO-059 | Dashboards MUST consume records emitted by FR-PO-054 channels in the FR-PO-058 format; dashboards MUST NOT define a parallel record format. | MUST | KD-3 / KD-11 | §5.7 | Stage 1 |
| FR-PO-060 | Aggregation logic (rolling averages, p99 windows, regression bands) MUST live in `tools/perf-dashboard/`; gameplay code MUST NOT reference dashboard helpers. | MUST | §3.8.5 / Spec #20 §4.1 | §5.7 | Stage 1 |
| FR-PO-061 | Dashboard refresh cadence MUST be: per-PR delta synchronous with CI run, milestone trend weekly (Stage 1: nightly). | MUST | §3.8.7 | §5.7 | Stage 1 |
| FR-PO-062 | The Stage 1 dashboard catalogue MUST include per-spec per-tick budget, per-PR delta, milestone-baseline trend, allocation-tracker, and flake/determinism cross-reference dashboards (Appendix F schema). | MUST | §3.8.6 | §5.7 | Stage 1 |

### 2.2.9 Baseline reproducibility & storage (FR-PO-063 … 068, KD-11)

| ID | Statement | Level | Source | Verify | Stage |
|----|-----------|-------|--------|--------|-------|
| FR-PO-063 | Every baseline file MUST be reproducible from recorded git SHA, seed, `EnvironmentFingerprint`, and platform pin. | MUST | KD-11 | §5.4 | Stage 0+1 |
| FR-PO-064 | Baselines MUST live at `tests/data/baselines/<spec>/` once `src/` exists. Stage 0 placeholder location: `docs/specs/performance-optimization/baselines/`. | MUST | KD-11 | §5.4 | Stage 0 |
| FR-PO-065 | Baseline file format MUST conform to #16 §3.2.4.1 canonical record format; Appendix A schema is the paste-ready layout. | MUST | KD-3 / KD-11 | §5.7 | Stage 0+1 |
| FR-PO-066 | Capture cadence: per-PR delta at Stage 0+1; full re-baseline at each Stage milestone. | MUST | KD-11 | §5.4 | Stage 0+1 |
| FR-PO-067 | The §5.4 baseline-reproducibility auditor MUST re-run the recorded session manifest and confirm the recaptured metric matches within §3.4.3 confidence interval. | MUST | KD-11 | §5.4 | Stage 0+1 |
| FR-PO-068 | Baselines failing FR-PO-067 MUST be marked stale; the PR that introduced them MUST be blocked. | MUST | KD-11 | §5.4 | Stage 0+1 |

### 2.2.10 Stage-0 manual benchmarking & local runbook (FR-PO-069 … 074, KD-5)

| ID | Statement | Level | Source | Verify | Stage |
|----|-----------|-------|--------|--------|-------|
| FR-PO-069 | Stage 0 manual benchmarking MUST run against synthetic harnesses in `tools/perf-harness/`; no `src/` code yet. | MUST | KD-5 | §5.1 | Stage 0 |
| FR-PO-070 | At Stage 0 the reviewer MUST execute the manual-review equivalents of the §5.3 schema-conformance and §5.5 loop-tag auditors against `docs/specs/` only (the `tools/run-perf-local.sh` script in Appendix E is the structural template for the Stage 0+1 automation; the Python implementation lands per §7.1 / D2). At Stage 0+1 activation, `tools/run-perf-local.sh` MUST invoke the automated `tools/budget-auditor.py` against `docs/specs/` only. | MUST | KD-5 | §5.1 | Stage 0 / Stage 0+1 |
| FR-PO-071 | Local runbook output MUST be pasted into the PR description by the reviewer. | SHOULD | §6.2 | §5.1 | Stage 0 |
| FR-PO-072 | Stage 0 anchor baselines MAY be captured against synthetic harnesses; they MUST be marked "anchor / Stage 0" and MUST NOT be cited as gameplay baselines. | MAY | KD-5 / KD-11 | §5.4 | Stage 0 |
| FR-PO-073 | Stage 0 baselines using a synthetic harness MUST still record the FR-PO-016 session-contract fields (with `EnvironmentFingerprint` recorded as best-available before #16 §4 normative landing). | MUST | KD-6 / KD-11 | §5.4 | Stage 0 |
| FR-PO-074 | Stage 0+1 transition MUST replace synthetic-harness anchor baselines with real `src/<spec>/` baselines atomically; mixed baselines are forbidden. | MUST | KD-5 | §5.4 | Stage 0+1 |

### 2.2.11 Reporting cadence & defect lifecycle (FR-PO-075 … 080)

| ID | Statement | Level | Source | Verify | Stage |
|----|-----------|-------|--------|--------|-------|
| FR-PO-075 | Stage 0 reporting cadence: monthly survey of §5.3 / §5.5 auditor output appended to `docs/tracking/PROGRESS.md`. | MUST | §6.5 | §5.1 | Stage 0 |
| FR-PO-076 | Stage 0+1 reporting cadence: per-PR delta synchronous with CI; weekly dashboard. | MUST | §6.5 | §5.7 | Stage 0+1 |
| FR-PO-077 | Stage 1 reporting cadence: per-PR delta + nightly dashboard + monthly retrospective. | MUST | §6.5 | §5.7 | Stage 1 |
| FR-PO-078 | Every defect MUST cite the FR-PO ID violated (or the owning-spec §6 budget number); uncited defects are themselves a procedural violation. | MUST | §6.4.4 | §5.6 | Stage 0+1 |
| FR-PO-079 | Tier A allocation defects MUST be classified Critical and block the current Stage milestone. | MUST | §6.4.3 | §5.6 | Stage 0+1 |
| FR-PO-080 | PR-blocking failures MUST be investigated within 24 hours; Boundary defects MUST be reviewed at next spec-revision cycle of the boundary spec (#16 or #19). | MUST | §6.4.2 | §5.7 | Stage 0+1 |

## 2.3 Failure-to-Comply Modes

- **Budget overrun.** Subsystem exceeds declared §6 budget by more than
  the §3.5.2 threshold → §3.5 regression gate blocks merge.
- **Allocation in hot path** (FR-PO-032 / KD-10). Regression gate
  blocks merge; Tier A allocation is a Critical defect per §6.4.3.
- **Untagged budget loop** (FR-PO-009 / KD-8). Per-spec §6 review
  rejects spec.
- **Non-deterministic profiling run** (FR-PO-020 / KD-6). Baseline
  rejected at capture time; not entered into baseline corpus.
- **Tier-A degradation path proposed** (FR-PO-042 / KD-7). Spec review
  rejects.
- **Per-spec §6 schema drift** (FR-PO-002 / KD-2). §5.3 conformance
  auditor flags as `ERR-018-NNN`.
- **Trace record format drift from #16 §3.2.4.1** (FR-PO-058). §5.7
  boundary review blocks.
- **Unsigned tick-pipeline trace point** (FR-PO-058a). §5.7 boundary
  review blocks; emission-veto under KD-3 inverted.

## 2.4 Data Structures (informational)

Spec #18 declares **no runtime data structures** consumed by gameplay
code. The performance-harness data structures listed below are
test-side / tooling-side only and are formalized in §4 and Appendix A:

- `BaselineRecord` — immutable value type; serialized per Appendix A;
  on-disk encoding conforms to #16 §3.2.4.1 canonical binary layout
 .
- `BudgetRollupEntry` — read-only view onto a per-spec §6 declaration;
  recomputed at build time, never edited by hand.
- `ProfilingSessionManifest` — captures the FR-PO-016 session-contract
  fields.

Tier vocabulary (Tier A / B / C) is cited from #16 §1.3 by reference
(KD-1); not redeclared here.

## 2.5 Failure Modes (Spec-internal)

Failure modes of Spec #18's own governance machinery, in addition to
the §2.3 compliance modes:

- **Per-spec §6 schema drift** — discovered by §5.3 conformance check;
  logged as `ERR-018-NNN`.
- **Baseline non-reproducibility** (missing seed, missing fingerprint,
  missing platform pin) — caught by §3.4.4 baseline validator;
  baseline rejected at capture time.
- **Budget-allocation total exceeds platform headroom** — handled by
  §3.1.5 re-allocation procedure; never silently truncated.
- **Trace-record format drift from #16 §3.2.4.1** — caught by §3.8.4
  record-format binding audit and §5.7 boundary review.
- **Unsigned tick-pipeline trace point inserted by #18 channel** —
  caught by §3.8.3 emission-veto audit and §5.7 boundary review.
- **Loop-tag conformance gap** — caught by §5.5 loop-tag auditor;
  `ERR-018-NNN` row filed.
- **Cite-not-redefine violation** (Spec #18 republishes a per-spec
  budget number or restates a CLAUDE.md invariant) — caught by §5.3
  conformance auditor's KD-1 pass.

## 2.6 Version History

| Version | Date         | Author      | Notes |
|---------|--------------|-------------|-------|
| 0.3     | May 14, 2026 | Claude Code | PASS-2 adversarial-review fix pass (`ERR-018-014`, `ERR-018-017`). Duplicate v0.2 version-history row removed (consolidated into single row carrying union of notes — root cause: PR #59 + PR #60 parallel-branch merge). FR-PO-019 split into FR-PO-019 (MAY, permission only) + FR-PO-019a (MUST, manifest+seed recording). FR count is now 82 (FR-PO-001 … 080 + FR-PO-019a + FR-PO-058a). |
| 0.2     | May 14, 2026 | Claude Code | PASS-1 adversarial-review fix pass (`ERR-018-002`, `ERR-018-009`). FR-PO-053 reworded — `[HotPathAllocExempt]` ownership relocated to Spec #18 §3.7.5 (no longer cites Spec #20 §3); source-citation column for FR-PO-053 narrowed to `KD-10` (KD-1 cite-not-redefine framing dropped for this case). FR-PO-070 split Stage 0 manual / Stage 0+1 automated to align with §7.1 tool-deliverable schedule; activation-stage column annotated `Stage 0 / Stage 0+1`. FR-PO-016 #16 §4→§4.8 EnvironmentFingerprint citation corrected; FR-PO-038 perf-gate flake claim softened. |
| 0.1     | May 13, 2026 | Claude Code | Initial draft from `outline-detailed.md` v1.1 §2. FR catalogue published with 81 FRs (FR-PO-001 … 080 + FR-PO-058a per outline v1.1 emission-constraint addition). All FRs assigned source citation, verification pointer, and activation stage. tags applied to every #16 / #19 citation. |
