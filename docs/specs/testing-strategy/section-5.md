# Testing Strategy & Framework Specification #19 — Section 5: Test Plan (Conformance Verification of This Spec Itself)

**Created:** May 12, 2026
**Last Updated:** September 4, 2026
**Version:** 0.6
**Status:** AMENDMENT DRAFT (A3.2b; May 15, 2026 approved baseline remains in force)
**Amendment plan:** `docs/planning/project-architecture-governance-integration-plan.md` v0.38, §7; A3.2b
**Purpose:** Reflexive test plan: this section verifies Spec #19
against itself. Per-spec §5 conformance verification (which Spec #19
mandates for *other* specs) is mechanics-defined in §3.5; the auditor
mechanics for that conformance check live here in §5.4.

> **Slot reconciliation.** The template's §5 ("Test Plan") is
> reflexive for a meta-spec: there is no numerical algorithm to verify.
> What §5 verifies is that Spec #19 itself complies with the rules it
> publishes (KD-6 self-application), and that the auditors capable of
> checking other specs against Spec #19 are described here.

---

## 5.1 Conformance Verification Model

- Spec #19 publishes its FRs in §2.2. This section maps every FR to
  its verification mechanism.
- **Stage 0:** manual review (no code yet, parallel to Spec #20 §5.1).
- **Stage 0+1:** tooling activates per the "Activation stage" column
  in §2.2; the §5.6 traceability table records the tooling per FR.

## 5.2 Stage-Gated Activation Table (KD-5)

Per-FR table. Most FRs read "Stage 0+1" with criterion "first `src/`
code committed". A subset reads "Stage 0" with criterion "applies to
spec drafts now" — notably the KD-6 mandate FRs (FR-TS-040 … 045) and
per-spec §5 schema FRs (FR-TS-046 … 052).

> **Column semantics.** The "Current Status" column below describes
> current enforcement/implementation state. The "Activation Stage"
> column matches the same-name column in §2.2. A resolved tool pin does
> not by itself prove that every downstream auditor for that FR range
> has been implemented; partial conformance is stated explicitly.

| FR Range | Current Status | Activation Stage | Activation Criterion |
|----------|----------------|------------------|----------------------|
| FR-TS-001 … 010 | ACTIVE (Stage 0+1); legacy taxonomy migration remains incomplete | Stage 0+1 | First `src/` code commit |
| FR-TS-011 … 020 | ACTIVE where #16 runner integration exists; authoritative full certification is nightly on the pinned Windows/Unity host | Stage 0+1 | #16 §5 CI integration available |
| FR-TS-021 … 030 | ACTIVE in executable scenario-runner surfaces; D9 root manifest/index remains overdue | Stage 0+1 | Scenario runner implemented |
| FR-TS-031 … 039 | **ACTIVE (Stage 0+1)** — D2 resolved on FsCheck.NUnit 2.16.6; D8 coverage-guided fuzzing remains a later-stage decision | Stage 0+1 | Property framework pinned (D2) — **reached** |
| FR-TS-040 … 045 | **ACTIVE** — `tools/checklist-auditor.py` is implemented and invoked by the stable runner | Stage 0 | Applies to current spec drafts |
| FR-TS-046 … 052 | **ACTIVE** — `tools/spec5-schema-auditor.py` is implemented and invoked by the stable runner; approved #1–#8 remain survey-only per KD-4 | Stage 0 | Applies to current spec drafts |
| FR-TS-053 … 060 | **ACTIVE (Stage 0+1) — partially conformant**. D3 is resolved on coverlet.collector 6.0.4 and PR/nightly collection is wired; the §5.5 per-tier threshold mapper/auditor remains unimplemented | Stage 0+1 | Coverage tool pinned (D3) — **reached** |
| FR-TS-061 … 067 | Inactive pending the specified flake integration layer | Stage 0+1 | CI integration layer specified (§7.2) |
| FR-TS-068 … 074 | Schema/implementation mixed; fixture-population obligations activate as fixtures are committed | Stage 0+1 | First fixture committed |
| FR-TS-075 … 078, 080 | **ACTIVE (Stage 0+1) — pipeline topology implemented in this candidate.** Versioned pre-commit, PR, and nightly surfaces now exist. PR runs a conservative whole-tree scenario superset rather than claiming exact D9 manifest selection. Linux nightly evidence is non-certifying; #16 certification is a separate pinned Windows/Unity job | Stage 0+1 | First `src/` code commit (KD-5; §7.1) — **reached** |
| FR-TS-079 | **ACTIVE (Stage 0) — implementation present in this candidate.** `tools/run-tests-local.sh` is the stable local/CI composition entry point and invokes both auditors plus the executable gate | Stage 0 | Applies to the current repository |
| FR-TS-081 … 085 | **ACTIVE (Stage 0, partial)** | Stage 0+1 | Spec-defect class active now; implementation / test / determinism classes activate with code |
| FR-TS-086 … 092, 094 … 097 | **AMENDMENT DRAFT; non-blocking** | Stage 0+1 | A3.4 reapproval plus applicable A4 resolver/proof prerequisites and A8 architecture/evidence-gate activation |
| FR-TS-093 | **AMENDMENT DRAFT; non-blocking** | **Stage 0** | A3.4 reapproval only. §2.2 assigns FR-TS-093 Stage 0: it is a review-mechanics requirement with no implementation prerequisite, so it acquires no A4 resolver/proof or A8 gate-activation condition. It remains non-blocking solely because the May 15, 2026 baseline stays operative until A3.4 |

`ERR-019-001` remains a live tracking entry until the candidate pipeline
implementation lands on `main`; this section no longer repeats its old
"artifacts do not exist" diagnosis as current candidate-tree fact.

## 5.3 Approval-Checklist Auditor (KD-6 Mechanics)

Mechanics for FR-TS-040 … 045.

### 5.3.1 Stage 0 (Manual)

- A reviewer (the "checklist auditor") walks every approval-checklist
  row in every spec under review and resolves each citation against
  the current repo state.
- Resolution outcomes:
  - **Resolved (file path).** The cited path exists; the cited value
    appears verbatim in the cited file. Recorded as `RESOLVED`.
  - **Resolved (programmatic check).** The named check is runnable
    and produces the cited output. Recorded as `RESOLVED`.
  - **Unresolved.** Neither (a) nor (b) holds. Recorded as `BLOCK`.
- Output is appended to the PR description in the format declared in
  Appendix C.
- Any `BLOCK` row prevents the spec from reaching APPROVED status
  (FR-TS-042).

### 5.3.2 Stage 0+1 (Automated)

`tools/checklist-auditor.py` is implemented and invoked by every
executable `tools/run-tests-local.sh` mode.

- Parses approval-checklist tables under `docs/specs/`.
- For each row, resolves file-path citations against the repository.
- Emits a machine-readable/console report suitable for CI consumption.
- Exit non-zero on unresolved blocking evidence.
- Behavior-level tooling tests prove that a valid evidence path passes
  and a missing path blocks.

The initial implementation deliberately remains conservative about
arbitrary command execution from documentation: untrusted prose is not
turned into a shell command merely because it appears in a checklist.

Output schema remains Appendix C authority.

## 5.4 Per-Spec §5 Schema-Conformance Auditor

Mechanics for FR-TS-046 … 052.

### 5.4.1 Schema Check

The auditor walks every spec's §5 against the Appendix C template:

- Required headings/content classes present (FR-TS-046 … 051).
- Test-count-by-layer surface present.
- Property-test surface present with tier classification where declared.
- Scenario surface present with manifest linkage where declared.
- Coverage targets per tier present.
- Authoritative-field determinism-tier classifications present.
- Approval-checklist linkages present.

### 5.4.2 Stage 0 Application

- **New specs from this point forward (#9 … #20):** schema-conforming
  on first draft or §9 approval is blocked (FR-TS-052).
- **Approved specs (#1 … #8):** survey-only at Stage 0 per §3.5.4 (KD-4
  no-forced-re-open rule). Gaps are survey findings rather than a
  forced retroactive approval failure.

### 5.4.3 Stage 0+1 Automation

`tools/spec5-schema-auditor.py` is implemented and invoked by the stable
runner. It emits blocking findings for the active post-#8 schema
surface and survey-only output for legacy #1–#8. Tooling tests lock the
legacy survey-only behavior so the migration policy cannot silently
turn into retroactive blocking.

## 5.5 Coverage-Report Auditor (KD-9)

Mechanics for FR-TS-053 … 060.

### 5.5.1 Stage 0

Not applicable (no code).

### 5.5.2 Stage 0+1

- D3 is resolved on coverlet.collector 6.0.4.
- PR/nightly runner modes collect Cobertura-formatted `XPlat Code
  Coverage` through `tools/dotnet-ci/coverage.runsettings`.
- The still-owed auditor maps each production file to its #16 §1.1.1
  tier and applies KD-9 thresholds:
  - Tier A: ≥ 98% line, ≥ 95% branch (FR-TS-053).
  - Tier B: ≥ 90% line, ≥ 80% branch (FR-TS-054).
  - Tier C: lint-only (FR-TS-055).
- Test code excluded from coverage measurement (FR-TS-056).
- Exemption handling per §3.6.5: lead-developer sign-off recorded in
  `tests/coverage-exemptions.md`.

**Current boundary:** collector selection/invocation is implemented;
the per-tier threshold mapper/auditor is not. This subsection does not
convert the collector pin into a false threshold-enforcement claim.

### 5.5.3 Reporting

Per-PR delta at Stage 0+1; absolute per-tier dashboard at Stage 1
(FR-TS-058).

## 5.6 FR-to-Verification Traceability

Single table indexed by FR-TS-###; columns: `FR | Verification
Mechanism | Tooling | Activation Stage | Output Artifact`.

| FR | Mechanism | Tooling | Activation | Output Artifact |
|----|-----------|---------|------------|-----------------|
| FR-TS-001 … 010 | Pyramid-ratio check at PR-merge | Coverage/tooling surfaces; taxonomy migration still incomplete | Stage 0+1 | CI report |
| FR-TS-011 … 020 | Boundary review at #16 §5 change; nightly full certified execution | Linux regression evidence + certified Windows/Unity nightly job | Stage 0+1 | Review note + certified test result |
| FR-TS-021 … 030 | Scenario runner schema check | `ScenarioRunner` load step + §5.4 auditor; D9 index overdue | Stage 0+1 | Runner exit status |
| FR-TS-031 … 039 | Property / fuzz seed-log inspection | FsCheck.NUnit 2.16.6 + seed-governance review | Stage 0+1 | Test/run log |
| FR-TS-040 … 045 | Checklist auditor | `tools/checklist-auditor.py` | **Stage 0** | Auditor output |
| FR-TS-046 … 052 | §5.4 schema-conformance auditor | `tools/spec5-schema-auditor.py` | **Stage 0** | Auditor output |
| FR-TS-053 … 060 | Coverage collection + tier audit | coverlet.collector 6.0.4 implemented; per-tier mapper still owed | Stage 0+1 | Cobertura report; future tier verdict |
| FR-TS-061 … 067 | Flake ledger + eviction-log review | CI double-run + `tests/flake-eviction-log.md` when activated | Stage 0+1 | Flake ledger |
| FR-TS-068 … 074 | Fixture validator | `IFixtureValidator` at scenario load | Stage 0+1 | Runner exit status |
| FR-TS-075 … 078, 080 | CI gate composition | `.githooks/pre-commit`, `tools/run-tests-local.sh`, `.github/workflows/ci.yml`, `.github/workflows/nightly.yml` | Stage 0+1 | Local/PR/nightly gate results |
| FR-TS-079 | Stable local gate composition | `tools/run-tests-local.sh` + Appendix E | **Stage 0** | Local gate output |
| FR-TS-081 … 085 | Defect log review | `tests/test-defect-log.md` + manual triage | Stage 0+1 (partial Stage 0) | Defect log + cycle review |
| FR-TS-086 | Strict applicability resolution | A2 applicability schema/reference semantics; A4 resolver when implemented | Stage 0+1 | Matched-rule / obligation record or fail-closed diagnostic |
| FR-TS-087 | Proof-artifact shape, subject digest and provenance separation | `proof-artifact.schema.json` + reference semantics v2.1.0 | Stage 0+1 | Schema-valid reusable proof artifact |
| FR-TS-088 | Structural closed-world / bounded-surface verification | A4 closed-world inventory + structural proof resolver | Stage 0+1 | Structural proof or approved bounded result with uncertainty |
| FR-TS-089 | Lifecycle/order proof | Owning lifecycle tests + proof resolver | Stage 0+1 | Lifecycle-order proof artifact + execution record |
| FR-TS-090 | Deliberate failure-injection proof | Owning executable test + exact perturbation identity | Stage 0+1 | Failure-injection proof artifact |
| FR-TS-091 | Targeted mutation sensitivity | Targeted governance mutation protocol (§3.11.9), independent of project-wide D6 tooling | Stage 0+1 | Mutation proof with exact target/mutant/detector/restoration record |
| FR-TS-092 | Closure/freshness revalidation | A2 mechanically derived closure + freshness / changed-proof decision | Stage 0+1 | Fresh/stale/proven-non-impact decision |
| FR-TS-093 | Governance convergence consumption | Governance finding/run state; no severity-derived convergence | **Stage 0** (on A3.4 reapproval) | Valid disposition/status + fresh-run convergence result |
| FR-TS-094 | Required execution truth / exclusion intersection | Execution-state evaluator + required-test/exclusion-set check | Stage 0+1 | Blocking unsatisfied-state diagnostic or satisfied proof |
| FR-TS-095 | Merge-critical governance-tool verification | Focused tool self-tests / negative fixtures per Governance FR-AG-036A/040C | Stage 0+1 | Tool-verification result bound to tool identity |
| FR-TS-096 | Bounded-substitute validation | A2 execution truth + approved-limitation schema | Stage 0+1 | Valid bounded result only for eligible `excluded` / `unavailable` / `not-run` states |
| FR-TS-097 | Activation/KD-W1 tuning precondition | #20 activation state + machine disable anchor + exact approved exception scope | Stage 0+1 | Active-owner satisfaction or exact-scope exception diagnostic |

### 5.6.1 Architecture-Proof Negative Fixture Set

Before any FR-TS-086 … 097 machine check may block, its owning tool
suite MUST contain discriminating positive/negative fixtures for the
failure class it claims to detect. At minimum the architecture-proof
surface covers:

- missing or stale proof;
- zero-match, ambiguous, or otherwise unresolved applicability;
- dependency-closure drift, including a transitive dependency change;
- missing/invalid activation disable anchor;
- KD-W1 tuning against an `intentionally-disabled`,
  `pending-integration`, or `unresolved` owner without an exact approved
  exception;
- required execution reported as `skipped`, or intersecting an active
  exclusion/quarantine source;
- bounded substitution attempted for `failed`, `skipped`, or
  `runner-failed`;
- wrong-target, no-op/equivalent, surviving-detector, or unrestored
  mutation;
- wrong-target or ineffective failure injection; and
- governance-tool failure paths where the checker itself must fail
  closed.

These are proof-tool fixtures, not a new project-wide mutation score or
mega test assembly. A3.2b records the verification contract only; A4/A8
own implementation/activation.

## 5.7 Determinism-Suite Consumption Verification

> **Cross-reference.** §2.2 partition-table "Verification in" column
> points to §5.7 for FR-TS-011 … 020; §6.2.4 is the pipeline-level
> composition authority.

- Spec #19 declares **no numerical determinism tests of its own**.
- Spec #19 consumes #16 according to pipeline scope, not by pretending
  every host is a certification host: pre-commit carries no
  determinism certification; PR may carry partial/non-certifying
  regression evidence; nightly executes the **full authoritative #16
  suite on the pinned certified Windows/Unity host**.
- Linux `dotnet`/shim results remain useful regression evidence but do
  not certify the platform tuple.
- Failures in the authoritative #16 nightly suite are determinism-gate
  failures under KD-2; Spec #19 does not soften #16 exit criteria.
- **Boundary review check (FR-TS-015).** Any change to #16 §5 that
  touches tier names or exit criteria triggers a Spec #19 §3.2 review
  before the change can land. The reviewer:
  - Re-grep the cited subsection numbers in Spec #19 §3.2 and §3.6.1.
  - If upstream churn re-introduces a `TBD-NORMATIVE` tag (per §2.3
    self-applied failure mode), file the re-introduction immediately
    and flip #19 status to `SUSPENDED` per §9.4 state-transition table.
  - Update §1.4 dependency list if upstream section numbering shifted.

## 5.8 Version History

| Version | Date         | Author      | Notes |
|---------|--------------|-------------|-------|
| 0.6     | September 4, 2026 | — | **FR-TS-075/079 implementation synchronization.** Updates activation state after D2/D3 pins and pipeline implementation; records automated checklist/schema auditors, staged-snapshot pre-commit, PR whole-tree scenario superset, Linux non-certifying nightly evidence, and the separate pinned Windows/Unity full #16 suite. D3 collector closure is not overclaimed: §5.5's per-tier threshold mapper remains owed. `ERR-019-001` remains live until the candidate lands on `main`. |
| 0.1     | May 12, 2026 | Claude Code | Initial draft from `outline-detailed.md` v1.1. Stage-gated activation table + traceability table populated. |
| 0.2     | May 12, 2026 | Claude Code | Self-critique sweep. #16 §7 → §5; #16 §1.3 → §1.1.1. L5 §5.6 / §5.7 cross-reference added. L6 column-semantics disambiguation added to §5.2 lead. |
| 0.5     | September 3, 2026 | — | **A3.2b review correction (`ERR-019-001`).** Reconciles the FR-TS-075 … 080 band in §5.2 and §5.6 with the normative core: FR-TS-079 gets its own **Stage 0** row per §2.2, and FR-TS-075 … 078, 080 carry the actual Stage 0+1 criterion (first `src/` code commit, KD-5 / §7.1) — **reached** — in place of "CI provider pinned (D4)", which the normative core never defined. The band was therefore mis-reported as `Inactive` from the day it was written, not by D4's closure. Activation is deliberately not deferred behind a new prerequisite: gating FR-TS-075 on the three-pipeline topology it itself mandates would be circular and fail-open. The real gap this concealed — two absent mandatory pipelines and an absent Appendix E script — is recorded at `ERR-019-001` and open at `docs/tracking/open-issues.md`. |
| 0.4     | September 3, 2026 | — | **A3.2b review correction (Codex #353 finding 1).** Splits FR-TS-093 out of the FR-TS-086 … 097 band in §5.2 and corrects its §5.6 activation cell: §2.2 assigns FR-TS-093 **Stage 0**, so the band's Stage 0+1 value contradicted the normative core and attached A4/A8 prerequisites the requirement does not have. Its Stage 0 *status* is unchanged — still AMENDMENT DRAFT and non-blocking until A3.4 reapproval, because the May 15, 2026 baseline remains operative; no requirement is activated here. |
| 0.3     | September 3, 2026 | — | **A3.2b supporting-surface synchronization.** Extends §5.2 and §5.6 through FR-TS-097, adds the architecture-proof negative-fixture matrix, preserves non-blocking draft state until A3.4/A4/A8 prerequisites, and does not activate a CI gate. |
