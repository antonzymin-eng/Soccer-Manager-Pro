# Testing Strategy & Framework Specification #19 — Section 5: Test Plan (Conformance Verification of This Spec Itself)

**Created:** May 12, 2026
**Last Updated:** May 12, 2026
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

> **Column semantics.** The "Stage 0 Status" column below describes
> *current enforcement state* (`ACTIVE (Stage 0)` = enforced today;
> `Inactive` = waiting for activation criterion). The "Activation
> Stage" column matches the same-name column in §2.2 (the stage at
> which the FR begins to gate merges). For procedural FRs (FR-TS-040
> … 052), both columns read "Stage 0" because the activation criterion
> is "applies to spec drafts now" — there is nothing further to wait
> for.

| FR Range | Stage 0 Status | Activation Stage | Activation Criterion |
|----------|----------------|------------------|----------------------|
| FR-TS-001 … 010 | Inactive (no code) | Stage 0+1 | First `src/` code commit |
| FR-TS-011 … 020 | Inactive | Stage 0+1 | #16 §5 CI integration available |
| FR-TS-021 … 030 | Schema only (Stage 0) | Stage 0+1 | Scenario runner implemented |
| FR-TS-031 … 039 | Inactive | Stage 0+1 | Property framework pinned (D2) |
| FR-TS-040 … 045 | **ACTIVE (Stage 0)** | Stage 0 | Applies to current spec drafts |
| FR-TS-046 … 052 | **ACTIVE (Stage 0)** | Stage 0 | Applies to current spec drafts |
| FR-TS-053 … 060 | Inactive | Stage 0+1 | Coverage tool pinned (D3) |
| FR-TS-061 … 067 | Inactive | Stage 0+1 | CI integration layer specified (§7.2) |
| FR-TS-068 … 074 | Schema only | Stage 0+1 | First fixture committed |
| FR-TS-075 … 080 | Inactive | Stage 0+1 | CI provider pinned (D4) |
| FR-TS-081 … 085 | **ACTIVE (Stage 0, partial)** | Stage 0+1 | Spec-defect class active now; implementation / test / determinism classes activate with code |

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

`tools/checklist-auditor.py` (final language pin parallels CLAUDE.md
"When Writing Code" Python tooling rules):

- Parses §9 approval-checklist tables in every spec under
  `docs/specs/`.
- For each row, resolves the citation:
  - File-path citations: confirms the file exists and contains the
    literal value.
  - Programmatic-check citations: invokes the named check and
    captures stdout / exit status.
- Emits a structured report consumed by the CI gate.
- Exit non-zero on any unresolved row.

Output schema in Appendix C.

## 5.4 Per-Spec §5 Schema-Conformance Auditor

Mechanics for FR-TS-046 … 052.

### 5.4.1 Schema Check

The auditor walks every spec's §5 against the Appendix C template:

- Required headings present (FR-TS-046 … 051).
- Test-count-by-layer table parses.
- Property-test list parses; every property has a tier classification.
- Scenario list parses; every scenario has a manifest path that
  resolves under `tests/scenarios/`.
- Coverage targets per tier present.
- Authoritative-field determinism-tier classifications present.
- Approval-checklist linkages present.

### 5.4.2 Stage 0 Application

- **New specs from this point forward (#9 … #20):** schema-conforming
  on first draft or §9 approval is blocked (FR-TS-052).
- **Approved specs (#1 … #8):** survey-only at Stage 0 per §3.5.4 (KD-4
  no-forced-re-open rule). Gaps are logged as `ERR-019-NNN` rows in
  `docs/tracking/spec-error-log.md`. Population of those rows is a
  Stage 0+1 deliverable (§7.2); Appendix D ships at #19 approval with
  schema and headers only.

### 5.4.3 Stage 0+1 Automation

`tools/spec5-schema-auditor.py` (Stage 0+1 deliverable). Same approach
as §5.3.2; emits a structured report.

## 5.5 Coverage-Report Auditor (KD-9)

Mechanics for FR-TS-053 … 060.

### 5.5.1 Stage 0

Not applicable (no code).

### 5.5.2 Stage 0+1

- Coverage tool (D3) produces per-file report.
- Auditor maps each file to its #16 §1.1.1 tier and
  applies KD-9 thresholds:
  - Tier A: ≥ 98% line, ≥ 95% branch (FR-TS-053).
  - Tier B: ≥ 90% line, ≥ 80% branch (FR-TS-054).
  - Tier C: lint-only (FR-TS-055).
- Test code excluded from coverage measurement (FR-TS-056).
- Exemption handling per §3.6.5: lead-developer sign-off recorded in
  `tests/coverage-exemptions.md`.

### 5.5.3 Reporting

Per-PR delta at Stage 0+1; absolute per-tier dashboard at Stage 1
(FR-TS-058).

## 5.6 FR-to-Verification Traceability

Single table indexed by FR-TS-###; columns: `FR | Verification
Mechanism | Tooling | Activation Stage | Output Artifact`. At Stage 0
most rows resolve to "manual review against §3 mechanics" —
acknowledged degenerate (parallel to Spec #20 §5.5).

| FR | Mechanism | Tooling | Activation | Output Artifact |
|----|-----------|---------|------------|-----------------|
| FR-TS-001 … 010 | Pyramid-ratio check at PR-merge | Coverage tool + custom analyzer | Stage 0+1 | CI report |
| FR-TS-011 … 020 | Boundary review at #16 §5 change | Manual at Stage 0; CI sentinel at Stage 0+1 | Stage 0+1 | Review note in PR |
| FR-TS-021 … 030 | Scenario runner schema check | `ScenarioRunner` load step + §5.4 auditor | Stage 0+1 | Runner exit status |
| FR-TS-031 … 039 | Property / fuzz seed-log inspection | Property framework log + manual review | Stage 0+1 | Run log under `tests/data/run-logs/` |
| FR-TS-040 … 045 | Checklist auditor | Manual (Stage 0) → `tools/checklist-auditor.py` (Stage 0+1) | **Stage 0** | Auditor report appended to PR description |
| FR-TS-046 … 052 | §5.4 schema-conformance auditor | Manual (Stage 0) → `tools/spec5-schema-auditor.py` (Stage 0+1) | **Stage 0** | Auditor report |
| FR-TS-053 … 060 | Coverage auditor | D3 coverage tool + per-tier mapper | Stage 0+1 | Per-PR coverage delta |
| FR-TS-061 … 067 | Flake ledger + eviction-log review | CI double-run + `tests/flake-eviction-log.md` | Stage 0+1 | Flake ledger |
| FR-TS-068 … 074 | Fixture validator | `IFixtureValidator` at scenario load | Stage 0+1 | Runner exit status |
| FR-TS-075 … 080 | CI gate composition | CI provider (D4) + §6.2 policy | Stage 0+1 | CI gate report |
| FR-TS-081 … 085 | Defect log review | `tests/test-defect-log.md` + manual triage | Stage 0+1 (partial Stage 0) | Defect log + cycle review |

## 5.7 Determinism-Suite Consumption Verification

> **Cross-reference.** §2.2 partition-table "Verification in" column
> points to §5.7 for FR-TS-011 … 020; §5.6 traceability row for that
> range names "Manual at Stage 0; CI sentinel at Stage 0+1" with
> output artifact "Review note in PR." The two views are
> complementary, not contradictory: §5.7 publishes the *contract*;
> §5.6 publishes the *artifact*.

- Spec #19 declares **no numerical determinism tests of its own**.
- This subsection records the *consumption* contract: every CI
  pipeline runs #16 §5's full tier set; failures
  block merge per KD-2 (FR-TS-011, FR-TS-012).
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
| 0.1     | May 12, 2026 | Claude Code | Initial draft from `outline-detailed.md` v1.1. Stage-gated activation table + traceability table populated. |
| 0.2     | May 12, 2026 | Claude Code | Self-critique sweep. #16 §7 → §5; #16 §1.3 → §1.1.1. L5 §5.6 / §5.7 cross-reference added. L6 column-semantics disambiguation added to §5.2 lead. |
