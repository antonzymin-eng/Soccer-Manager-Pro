# Performance Optimization Strategy Specification #18 — Section 6: CI Orchestration Policy & Triage

**Created:** May 13, 2026
**Last Updated:** May 13, 2026
**Purpose:** Slot reconciliation: the template's §6 "Performance
Analysis" slot is replaced by CI Orchestration Policy & Triage. A
meta-spec has no algorithm to analyze for asymptotic complexity; it
codifies the CI orchestration policy and defect-lifecycle rules that
compose with Spec #19 §6 (functional gates) and Spec #16 §5
(determinism gates). Justification: §1.3 KD-4 (boundary with #19) and
KD-5 (Stage gating). Parallel slot reconciliation to Spec #19 §6.

---

## 6.1 Tooling Standards (Stage-gated per KD-5)

- **Stage 0:** no tooling activates. This subsection enumerates
  *selection criteria*, not chosen tools.
- **Stage 0+1 tool slate** (selection finalized at transition):
  - **Sampling profiler.** Unity Profiler + Tracy or Superluminal.
    Selection criteria: deterministic re-play support, headless
    batch-mode capture for CI, per-frame breakdown emission. Tracked
    as §7.5 D1.
  - **Allocation tracker.** Unity Memory Profiler or equivalent
    IL2CPP-compatible tool. Selection criteria: per-method alloc
    counts, integratable into the CI step at §3.7.4. Tracked as §7.5
    D2.
  - **Benchmark framework.** BenchmarkDotNet or Unity Performance
    Testing Extension. Selection criteria: statistical-significance
    reporting per §3.4.3, scenario-manifest binding per #16 §5
    (`TBD-NORMATIVE`). Tracked as §7.5 D3.
  - **CI provider.** Deferred to `src/CLAUDE.md` (parallel to Spec
    #19 §6.1; tracked as §7.5 D4). Selection criteria:
    - Supports the three pipeline shapes in §4.5 (pre-commit, PR,
      nightly).
    - Supports gate composition with #16 §5 determinism gate and #19
      §6.2 functional gate.
    - Exposes per-step pass/fail at gate-composition granularity
      (§3.5.3).

## 6.2 Stage-0 Local-Only Runbook

Until CI activates, the same gate composition runs locally:

- **Pre-commit hook script** (Stage 0 deliverable; Appendix E):
  `tools/run-perf-local.sh` invokes the manual §5.3 schema-conformance
  auditor and §5.5 loop-tag auditor against `docs/specs/` only.
- **Stage 0 manual benchmarking** runs against synthetic harnesses in
  `tools/perf-harness/` (no `src/` yet). It produces "anchor"
  baselines that exercise the tooling but do not yet represent
  gameplay code (FR-PO-072).
- **Output of local runbook** → reviewer pastes into PR description
  per FR-PO-071.

## 6.3 CI Perf-Gate Topology (Stage 0+1, boundary with #19)

- Spec #18 declares performance regression gates (§3.5.2 threshold,
  §3.5.6 absolute guard, §3.7 alloc gate).
- Spec #19 §6.2 (`TBD-NORMATIVE`) declares functional regression gates
  and orchestrates composition.

**Gate composition rule** (KD-4 binding; also recorded in §3.5.3
table):

| Gate failure | Effect |
|--------------|--------|
| Functional gate (Spec #19) | Block merge; short-circuit perf step |
| Determinism gate (Spec #16 §5) | Block merge |
| Performance gate (Spec #18) | Block merge |
| Allocation gate (Spec #18) | Block merge |

No gate is "soft". Perf-gate exceptions require lead-developer
sign-off per §3.5.5 / FR-PO-040.

## 6.4 Defect Lifecycle & Triage (FR-PO-075 … 080)

### 6.4.1 Defect classes

- **Budget overrun.** Subsystem exceeds §3.5.2 threshold. Resolution:
  fix code or re-allocate budget via §3.1.5.
- **Allocation regression.** Non-zero alloc on a hot path. Resolution:
  fix code; allocation on a Tier A path is Critical per FR-PO-079.
- **Baseline non-reproducibility** (KD-11 violation). Resolution:
  re-capture or investigate environment drift.
- **Boundary defect.** Perf gate firing on a functional flake, or
  vice versa. Resolution: route to §5.7 boundary review.
- **Inconclusive optimization** (§3.4.3 significance failure).
  Resolution: backlog with date target.

### 6.4.2 Triage cadence

- **PR-blocking failures:** investigated within 24 hours (parallel to
  Spec #19 §6.4.2; FR-PO-080).
- **Inconclusive optimizations:** reviewed weekly.
- **Boundary defects:** reviewed at the next spec-revision cycle of
  the boundary spec (#16 or #19).

### 6.4.3 Severity scale

- **Critical** — Tier A allocation. Blocks the current Stage
  milestone.
- **High** — >+10% milestone-baseline drift (§3.5.6 trip). Blocks
  current sprint.
- **Medium** — per-PR threshold trip on a Tier B path. Backlogged
  with a date target.
- **Low** — inconclusive optimization. Backlog with no date.

### 6.4.4 Defect-to-FR traceability

Every defect cites the FR it violated (Spec #18 FR or owning-spec §6
budget number). Defects without FR citation are themselves a
procedural violation (FR-PO-078; parallel to Spec #19 §6.4.4).

## 6.5 Reporting Cadence

- **Stage 0** (FR-PO-075): monthly survey of §5.3 schema-conformance
  auditor output + §5.5 loop-tag auditor output appended to
  `docs/tracking/PROGRESS.md`.
- **Stage 0+1** (FR-PO-076): per-PR delta + weekly dashboard
  (§3.8.5).
- **Stage 1** (FR-PO-077): per-PR delta + nightly dashboard + monthly
  retrospective.

## 6.6 Functional-Related Cross-Listing

FR-PO-031 … 040 (regression gates) cite Spec #19 §6.2 by reference per
KD-4. No functional gate rules are republished here.

## 6.7 Version History

| Version | Date         | Author      | Notes |
|---------|--------------|-------------|-------|
| 0.1     | May 13, 2026 | Claude Code | Initial draft from `outline-detailed.md` v1.1 §6. Slot reconciliation explained (template's "Performance Analysis" replaced by CI Orchestration & Triage for this meta-spec — parallel to Spec #19 §6). Tooling-standards selection criteria declared for D1 / D2 / D3 / D4. Stage-0 local-only runbook pointer at §6.2; gate-composition rule at §6.3; defect classes / triage cadence / severity scale / traceability at §6.4. Reporting cadence at §6.5. All #16 / #19 citations tagged `TBD-NORMATIVE`. |
