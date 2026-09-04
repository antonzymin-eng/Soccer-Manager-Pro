# Testing Strategy & Framework Specification #19 — Section 6: CI Orchestration & Triage

**Created:** May 12, 2026
**Last Updated:** September 4, 2026
**Version:** 0.5
**Status:** AMENDMENT DRAFT (A3.2b; May 15, 2026 approved baseline remains in force)
**Amendment plan:** `docs/planning/project-architecture-governance-integration-plan.md` v0.38, §7; A3.2b
**Purpose:** CI orchestration policy, gate composition rule, local runbook,
defect lifecycle and triage, reporting cadence, and the boundary with
Spec #18 §4 / §7.

> **Slot reconciliation.** Replaces the template's "Performance
> Analysis" slot. A meta-spec has no algorithm to analyse; it codifies
> the CI orchestration policy and defect-lifecycle rules. Justification
> in §1.3 KD-3 (boundary with Spec #18) and KD-5 (Stage gating).

---

## 6.1 Tooling Standards (Stage-Gated per KD-5)

### 6.1.1 Stage 0

No runtime tooling activates. This subsection preserves the original
selection-criteria boundary for the pre-code stage.

### 6.1.2 Stage 0+1 Tool Slate

The Stage 0+1 slate is reconciled against the live repository.

- **Test runner — D1 RESOLVED.** NUnit is the repository runner.
  `tools/dotnet-ci/generate_projects.py` pins NUnit 3.14.0,
  NUnit3TestAdapter 4.6.0, and Microsoft.NET.Test.Sdk 17.11.1; the
  non-certifying Linux shim gate executes the generated solution with
  `dotnet test` via `tools/dotnet-ci/run-gate.sh`.
- **Property framework — D2 RESOLVED.** FsCheck.NUnit 2.16.6 is pinned
  through `Directory.Build.targets`, preserving compatibility with the
  NUnit 3.14 runner. Property tests therefore enter the normal test
  surface when present and retain FsCheck's 100-case default unless an
  owning property declares a stronger setting. Seed governance remains
  §3.4/KD-7 authority.
- **Coverage tool — D3 RESOLVED AS COLLECTOR PIN.**
  coverlet.collector 6.0.4 is pinned through `Directory.Build.targets`
  and configured by `tools/dotnet-ci/coverage.runsettings`; PR and
  nightly local-runner modes collect `XPlat Code Coverage`. The
  separate §5.5 per-tier threshold mapper/auditor remains an
  implementation obligation and is not implied complete by choosing
  the collector.
- **Mutation testing.** Stryker.NET (provisional). Selection criterion:
  parallels coverage tool. Activation deferred to Stage 1 (D6).
- **CI provider — D4 RESOLVED.** GitHub Actions is the repository CI
  provider at `.github/workflows/ci.yml`; scheduled Testing Strategy
  orchestration is at `.github/workflows/nightly.yml`. The provider
  pin is declared in `src/CLAUDE.md` as FR-TS-078 requires. This pin
  does not activate the proposed architecture/evidence gate; A8 still
  owns that required-status transition.

## 6.2 CI Pipeline Policy (Boundary with #18)

### 6.2.1 Gate Authority

- **Functional regression gates** — Spec #19 authority. Test pass /
  fail.
- **Performance regression gates** — Spec #18 §4
  authority. Budget threshold.
- **Determinism gates** — Spec #16 §5 authority.
  Bitwise equality across `EnvironmentFingerprint`.
- **Architecture/evidence gate** — Spec #19 owns proof/evidence and
  execution-truth mechanics; Governance owns applicability/property/
  convergence authority; Spec #20 owns integration/activation
  declarations consumed by the proof.

All four feed a single CI orchestration model. The fourth gate is
**topology only in A3.2b**: it is not a required status and does not
block until A3.4 reapproval, applicable A4 resolver/proof prerequisites,
and A8 activation are complete.

### 6.2.2 Gate Composition Rule

- Functional gate failure → block merge once that gate is active
  (Spec #19 authority).
- Performance gate failure → block merge once active (Spec #18
  authority).
- Determinism gate failure → block merge once active (#16 §5
  authority).
- Architecture/evidence gate unsatisfied state → block merge **only
  after A8 activation**. Missing/stale proof, unresolved applicability,
  invalid Governance convergence, or an unsatisfied required execution
  cannot be converted into a soft follow-up.
- Flake quarantine suppresses only an otherwise-eligible **functional**
  gate blocking effect. It does not satisfy or waive a required
  architecture proof (FR-TS-063/077/094).
- An explicitly owner-held failing acceptance predicate is **not flake
  quarantine**. The current `sim_match_engine_close_chance` owner-held
  RED is excluded only from the ordinary blocking pass under the
  versioned owner-held mode, then executed separately and checked
  against its recorded diagnostic values. A changed value, unexpected
  pass, or additional failure is blocking.
- A bounded substitute is not a generic escape valve: it may satisfy
  only an exact obligation that permits FR-TS-096 and only for
  deliberate `excluded`, `unavailable`, or `not-run` execution
  states. `failed`, `skipped`, and `runner-failed` remain
  unsatisfied.

### 6.2.3 Cite-Not-Redefine Binding (KD-3)

Spec #19 cites #18 §4 thresholds by reference (FR-TS-080); it MUST
NOT republish them. Cross-listing for FR-TS-075 … FR-TS-080 in §6.6.

### 6.2.4 Pipeline-to-Gate Map

| Pipeline | Functional Gate | Performance Gate | Determinism Gate | Architecture / Evidence Gate |
|----------|-----------------|------------------|------------------|------------------------------|
| Pre-commit | Yes (unit + property compatibility subset, staged snapshot, whole composition ≤60 s) | No | No | Report-only/local validation where available; never a new required status from A3.2b |
| PR | Yes (whole-tree unit + integration + property + scenario superset) | Yes (#18 §4) | Partial/non-certifying regression evidence only; not platform certification | After A8: resolve applicable obligations, execute or consume exact owning-runner results, validate proof/freshness/convergence |
| Nightly | Yes (Linux full simulation + soak, non-certifying) | Yes (#18 §7) | Yes: separate certified Windows/Unity #16 §5 job | After A8: revalidate applicable reusable proof and any scheduled/full-run obligations |

The Linux jobs are regression/functional hosts supplied by GitHub
Actions. They are never evidence that the Windows/Unity determinism
platform was certified.

### 6.2.5 Owning-Runner / Result Bridge

Architecture tests remain with the assembly/host that owns the behavior
unless the proof is genuinely cross-host. Placement is not evidence of
execution. Every required executable proof MUST resolve to a runner
that can compile/execute the owning test and to a machine-readable
execution record whose test/command identity and
`subject_scope_digest` bind it to the proof artifact.

The architecture/evidence gate MUST reject an intersection between the
resolved required-test set and active quarantine/exclusion sources.
Where it cannot execute an owning test directly, it consumes a
mandatory upstream runner result with exact identity/result binding.
An observed framework skip is `skipped`, not a deliberate
`excluded` state and cannot be relabelled after the fact.

## 6.3 Versioned Local / CI Runbook

`tools/run-tests-local.sh` is the stable FR-TS-079 composition entry
point now that CI exists. It is used locally and by the scheduled
Testing Strategy workflow rather than being a temporary pre-CI stub.

Every executable mode invokes, before the .NET gate:

- `tools/checklist-auditor.py` against `docs/specs/`;
- `tools/spec5-schema-auditor.py` against `docs/specs/`.

Modes:

- `--pre-commit` — staged-snapshot unit/property compatibility subset,
  with the **entire** composition hard-bounded to 60 seconds.
- `--pr` — whole-tree functional test superset plus Coverlet collection;
  the owner-held RED is executed separately and value-verified rather
  than quarantined.
- `--nightly` — whole-tree non-certifying simulation/functional run,
  Coverlet collection, and `ShotOutcomeDiagnosticTests` full-match
  soak activation. Platform determinism is deliberately not performed
  by this Linux entry point; `.github/workflows/nightly.yml` supplies
  the separate certified Windows/Unity #16 job.
- `--install-hook` / `--verify-hook` — configure or verify
  `.githooks/pre-commit`. `tools/bootstrap-dev.sh` is the normal
  developer bootstrap and refuses to overwrite an unrelated existing
  `core.hooksPath`.

The hook runs against the staged Git index, not unstaged working-tree
content. This prevents a commit from passing because unrelated local
edits happened to make the worktree green.

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
- **Determinism defect.** Routed to #16 §5 process per KD-2 (FR-TS-085).
  Activates at Stage 0+1.

Misclassified defects are themselves a procedural violation
(FR-TS-081).

### 6.4.2 Triage Cadence (FR-TS-082)

> **Stage-gated per KD-5.** "PR-blocking failures" presumes CI exists;
> the 24-hour SLA, weekly quarantined-test review, and "next
> spec-revision cycle" review all activate at Stage 0+1 with
> FR-TS-082. Until CI exists, only the **spec-defect** class is in
> scope (the others have no instances), and spec defects are reviewed
> at the next spec-revision cycle as below. This Stage-gating is
> recorded in §5.2 (FR-TS-082 row).

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

Severity remains a scheduling/triage attribute for Spec #19 defects.
It MUST NOT determine Governance finding disposition, terminal status,
or convergence. Governance v0.10's Disposition × Status model is
consumed under FR-TS-093 independently of this severity scale.

### 6.4.4 Defect Authority Traceability (FR-TS-084)

Every defect MUST cite the authority that makes the defect actionable.
That authority may be a Spec #19 or owning-spec FR, an admitted
architectural property, an approved invariant/equivalent authority, or
a concrete independently established correctness/integrity failure.
A novel generalized preference is not defect authority; route it to
Governance as a Candidate Property.

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

FR-TS-075 … FR-TS-080 (CI orchestration) cite #18 §4 / §7 by
reference per KD-3. No performance numbers are republished here.
**#18 reached `IN REVIEW` (section files at v0.3) on May 14, 2026;
the `[TBD-NORMATIVE]` tag previously suffixing this row was swept in
#19 v1.0.1 (May 15, 2026)** against #18's stable surface. If #18
churn re-shifts §4 / §7 subsection numbers before #18 reaches
`APPROVED`, file the re-introduction per §2.3 self-applied failure
mode and flip #19 status to `SUSPENDED`.

## 6.7 Version History

| Version | Date         | Author      | Notes |
|---------|--------------|-------------|-------|
| 0.5     | September 4, 2026 | — | **FR-TS-075/079 implementation synchronization.** Records D2 FsCheck.NUnit and D3 Coverlet pins, the permanent local/CI runner composition, staged-snapshot 60-second pre-commit behavior, explicit owner-held-RED verification, PR whole-tree scenario superset, Linux non-certifying nightly simulation/soak, and the separate pinned Windows/Unity determinism certification job. |
| 0.1     | May 12, 2026 | Claude Code | Initial draft from `outline-detailed.md` v1.1. Slot reconciliation replaces performance-analysis template. |
| 0.2     | May 12, 2026 | Claude Code | Self-critique sweep. #16 §7 → §5 throughout. M5 §6.4.2 explicit Stage-gating header. |
| 0.4     | September 3, 2026 | — | **A3.2b review correction (Codex #353 finding 3).** Records where the D4 CI-provider pin lands. FR-TS-078 requires the final provider pin in `src/CLAUDE.md`, which carried no provider declaration, so closing D4 at `.github/workflows/ci.yml` alone left the resolution and the normative FR mutually unsatisfiable. `src/CLAUDE.md` now declares the provider as a pointer to the workflow. FR-TS-078 itself is deliberately unchanged: it lives in §2.2, which A3.2a owns and this slice does not touch. |
| 0.3     | September 3, 2026 | — | **A3.2b supporting-surface synchronization.** Adds the fourth architecture/evidence gate topology, owning-runner/result bridge, strict execution/quarantine/bounded-substitute behavior, Governance convergence boundary, and FR-TS-084 authority model. Gate remains inactive until A8; no required status is created here. Live-repo audit also closes D1 on NUnit/`dotnet test` and D4 on GitHub Actions; D2/D3/D5–D8 remain deferred. No A4/A8 prerequisite is claimed complete. |
