# Testing Strategy & Framework Specification #19 — Section 6: CI Orchestration & Triage

**Created:** May 12, 2026
**Last Updated:** May 12, 2026
**Purpose:** CI orchestration policy, gate composition rule, Stage-0
local runbook, defect lifecycle and triage, reporting cadence, and the
boundary with Spec #18 §4 / §7.

> **Slot reconciliation.** Replaces the template's "Performance
> Analysis" slot. A meta-spec has no algorithm to analyse; it codifies
> the CI orchestration policy and defect-lifecycle rules. Justification
> in §1.3 KD-3 (boundary with Spec #18) and KD-5 (Stage gating).

---

## 6.1 Tooling Standards (Stage-Gated per KD-5)

### 6.1.1 Stage 0

No tooling activates. This subsection enumerates *selection criteria*,
not chosen tools.

### 6.1.2 Stage 0+1 Tool Slate

Selection finalized at the Stage 0 → Stage 1 transition. Until then,
the items below are tracked as deferred decisions in §7.5.

- **Test runner.** NUnit or xUnit. Selection criterion: must support
  attribute-based test discovery and parameterised cases. Final pin
  parallels Spec #20 §5.2 Roslyn analyzer pin. (D1)
- **Property framework.** FsCheck or equivalent C# property-based
  framework. Selection criterion: must allow seed injection (KD-7
  routing through `DeterministicRngService`). (D2)
- **Coverage tool.** Coverlet (provisional). Selection criterion: must
  emit per-tier breakdown consumable by §5.5 auditor (FR-TS-057). (D3)
- **Mutation testing.** Stryker.NET (provisional). Selection criterion:
  parallels coverage tool. Activation deferred to Stage 1 (D6).
- **CI provider.** Deferred to `src/CLAUDE.md`. **Selection criteria:**
  (a) must support the three pipeline shapes in §4.5 (pre-commit, PR,
  nightly); (b) must support functional gate composition with the #18
  performance gate `[TBD-NORMATIVE]` and the #16 §7 determinism gate
  `[TBD-NORMATIVE]` (KD-2 / KD-3); (c) must expose pass/fail at the
  granularity required by §6.2 gate-composition rules. No assumption
  is made that #18 "owns" CI-provider selection; selection happens in
  `src/CLAUDE.md` against these neutral criteria once the producer
  specs (#16 §7, #18 §4) are concretely citable. (D4)

## 6.2 CI Pipeline Policy (Boundary with #18)

### 6.2.1 Gate Authority

- **Functional regression gates** — Spec #19 authority. Test pass /
  fail.
- **Performance regression gates** — Spec #18 §4 `[TBD-NORMATIVE]`
  authority. Budget threshold.
- **Determinism gates** — Spec #16 §7 `[TBD-NORMATIVE]` authority.
  Bitwise equality across `EnvironmentFingerprint`.

All three feed a single CI orchestrator.

### 6.2.2 Gate Composition Rule

- Functional gate failure → block merge (Spec #19 authority).
- Performance gate failure → block merge (Spec #18 authority).
- Determinism gate failure → block merge (#16 §7 authority).
- **No gate is "soft."** Flake quarantine (§3.7) is the only escape
  valve and applies only to functional gates (FR-TS-077).

### 6.2.3 Cite-Not-Redefine Binding (KD-3)

Spec #19 cites #18 §4 thresholds by reference (FR-TS-080); it MUST
NOT republish them. Cross-listing for FR-TS-075 … FR-TS-080 in §6.6.

### 6.2.4 Pipeline-to-Gate Map

| Pipeline | Functional Gate | Performance Gate | Determinism Gate |
|----------|-----------------|------------------|------------------|
| Pre-commit | Yes (unit + property) | No | No |
| PR | Yes (unit + integration + property + scenarios) | Yes (#18 §4) | Partial (#16 unit tier) |
| Nightly | Yes (simulation + soak) | Yes (#18 §7) | Yes (#16 §7 full suite) |

## 6.3 Stage-0 Local-Only Runbook

Until CI activates, the same gate composition runs locally
(FR-TS-079).

- **`tools/run-tests-local.sh`** (Stage 0 deliverable; Appendix E).
  Invokes:
  - §5.3 checklist auditor (manual at Stage 0) against `docs/specs/`.
  - §5.4 schema-conformance auditor against `docs/specs/`.
- No `src/` is touched (none exists yet).
- Output of local runbook → reviewer pastes into PR description.

## 6.4 Defect Lifecycle & Triage (FR-TS-081 … 085)

### 6.4.1 Defect Classes

- **Spec defect.** Rule wrong / contradictory. Fix in spec; recorded
  in `docs/tracking/spec-error-log.md` as `ERR-NNN-NNN`. ACTIVE at
  Stage 0.
- **Implementation defect.** Code violates approved spec. Fix in code;
  tracked in issue tracker. Activates at Stage 0+1.
- **Test defect.** Test wrong (e.g., asserts on a wall-clock value).
  Fix test; recorded in `tests/test-defect-log.md`. Activates at
  Stage 0+1.
- **Determinism defect.** Routed to #16 §7 process per KD-2 (FR-TS-085).
  Activates at Stage 0+1.

Misclassified defects are themselves a procedural violation
(FR-TS-081).

### 6.4.2 Triage Cadence (FR-TS-082)

- **PR-blocking failures:** investigated within 24 hours.
- **Quarantined tests:** reviewed weekly.
- **Spec defects:** reviewed at next spec-revision cycle.
- **Critical-severity defects (§6.4.3):** investigated immediately,
  regardless of class.

### 6.4.3 Severity Scale (FR-TS-083)

- **Critical** — blocks Stage milestone.
- **High** — blocks current sprint.
- **Medium** — backlogged with date target.
- **Low** — backlog, no date.

### 6.4.4 Defect-to-FR Traceability (FR-TS-084)

Every defect MUST cite the FR it violated (Spec #19 FR or owning-spec
FR). Defects without FR citation are themselves a procedural violation
(parallel to KD-6 mandate).

The defect log schema (`tests/test-defect-log.md`) MUST contain:

| Defect ID | Date | Class | Severity | FR cited | Resolution |
|-----------|------|-------|----------|----------|------------|

Equivalent for `spec-error-log.md` already exists with the
`ERR-NNN-NNN` pattern.

## 6.5 Reporting Cadence

- **Stage 0:** monthly survey of `spec-error-log.md` +
  checklist-auditor output appended to `docs/tracking/PROGRESS.md`.
- **Stage 0+1:** per-PR delta + weekly dashboard.
- **Stage 1:** per-PR delta + nightly dashboard + monthly
  retrospective.

## 6.6 Performance-Related Cross-Listing

FR-TS-075 … FR-TS-080 (CI orchestration) cite #18 §4 / §7
`[TBD-NORMATIVE]` by reference per KD-3. No performance numbers are
republished here. When #18 advances from `NOT STARTED` to `IN
REVIEW`, the §1.4 dependency list is updated and the `[TBD-NORMATIVE]`
tags on FR-TS-075 … FR-TS-080 are reviewed for removal.

## 6.7 Version History

| Version | Date         | Author      | Notes |
|---------|--------------|-------------|-------|
| 0.1     | May 12, 2026 | Claude Code | Initial draft from `outline-detailed.md` v1.1. Slot reconciliation replaces performance-analysis template. |
