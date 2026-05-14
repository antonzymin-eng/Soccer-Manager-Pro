# Performance Optimization Strategy Specification #18 — Section 5: Test Plan (Reflexive Conformance Verification)

**Created:** May 13, 2026
**Last Updated:** May 14, 2026 (v0.2 PASS-1 adversarial-review fix pass)
**Purpose:** Verifies Spec #18 against itself. The template's §5
("Test Plan") slot is reflexive for a meta-spec: this section maps
every FR-PO-### to its conformance-verification mechanism, names the
auditor procedures (schema-conformance, loop-tag, baseline-
reproducibility, boundary), and records the Stage-0 acknowledged
degenerate state of most rows. Parallel slot reconciliation to Spec
#19 §5.

---

## 5.1 Conformance Verification Model

- **Spec #18 publishes its FRs in §2.2.** This section maps every FR
  to its verification mechanism.
- **Stage 0:** manual review (no code yet, parallel to Spec #19 §5.1
  / Spec #20 §5.1). Most rows in §5.6 resolve to "manual review
  against §3 mechanics" — acknowledged degenerate.
- **Stage 0+1:** tooling activates per the FR's "Activation stage"
  column in §2.2.

Auditor outputs land in `docs/tracking/PROGRESS.md` (Stage 0; monthly
per FR-PO-075) and in the CI dashboard (Stage 0+1; per-PR per
FR-PO-076).

## 5.2 Stage-Gated Activation Table (KD-5)

Per-FR activation status. "Stage 0" means the rule constrains spec
writing today and is verified by manual review. "Stage 0+1" means
activation triggers at first `src/` commit + `certification-platform.
md` Stage 0 row populated. "Stage 1" means activation at the dashboard
front-end ship date.

| FR Range | Stage 0 status | Activation stage | Activation criterion |
|----------|---------------|------------------|----------------------|
| FR-PO-001 … 008 | Active (spec-writing rules) | Stage 0 | Applies to every per-spec §6 today |
| FR-PO-009 … 015 | Active (spec-writing rules) | Stage 0 | Applies to every per-spec §6 today |
| FR-PO-016 … 023 | Inactive (manual harness only via FR-PO-023) | Stage 0+1 | First `src/` commit AND `certification-platform.md` Stage 0 row populated |
| FR-PO-024 … 030 | Inactive | Stage 0+1 | First `src/` commit |
| FR-PO-031 … 040 | Inactive (no CI yet) | Stage 0+1 | First `src/` commit AND CI provider selected (§7.5 D4) |
| FR-PO-041 … 047 | Active (spec-writing rules) | Stage 0 | Constrains spec degradation declarations today |
| FR-PO-048 … 053 | Inactive | Stage 0+1 | First `src/` commit AND IL2CPP build target available |
| FR-PO-054 … 062 | Active for trace-channel declaration (FR-PO-054); inactive for runtime emission | Stage 0+1 / Stage 1 | Channel registry schema declarable now; runtime emission needs `src/`; dashboards Stage 1 |
| FR-PO-063 … 068 | Partial (Stage 0 placeholder location FR-PO-064) | Stage 0+1 | First `src/` commit |
| FR-PO-069 … 074 | Active (Stage 0 manual benchmarking; FR-PO-070 Stage 0 leg = manual-review equivalents of §5.3 / §5.5 auditors; FR-PO-070 Stage 0+1 leg activates the automated `tools/budget-auditor.py` invocation per §7.1) | Stage 0 / Stage 0+1 | Applies to today's synthetic harness work; FR-PO-070 automation gated on `tools/budget-auditor.py` landing per §7.1 |
| FR-PO-075 … 080 | Active for reporting cadence (FR-PO-075); inactive for defect-class triage | Stage 0 / Stage 0+1 | Reporting cadence today; defect-class triage at first CI run |

## 5.3 Per-Spec §6 Schema-Conformance Auditor

Mechanics for FR-PO-001 … 008.

- **Stage 0 (manual).** A reviewer walks every approved spec's §6
  against the Appendix B template. Gaps are logged as `ERR-018-NNN`
  rows in `spec-error-log.md` per FR-PO-007.
- **Stage 0+1 (automated).** `tools/budget-auditor.py` (or equivalent
  — final language pin parallel to the Python tooling rule in
  CLAUDE.md "When Writing Code") parses §6 sections, validates schema,
  emits the §3.1.3 roll-up table programmatically.
- **Approved-spec posture.** Specs #1–#8 and #17 are surveyed at Stage
  0 (Appendix D); gaps are logged as `ERR-018-NNN` rows. Remediation
  happens at the next natural revision of each spec per the §3.1.2
  grandfather rule (KD-2; parallel to Spec #19 §3.5.4 acknowledged
  dilution policy).
- **New-spec posture.** New specs (#9, #10–#16, #18 itself, #19, #20):
  schema-conforming on first draft per FR-PO-006, or §9 approval is
  blocked.

Auditor output format declared in Appendix D.

## 5.4 Baseline-Reproducibility Auditor (KD-11)

Mechanics for FR-PO-063 … 068.

- **Stage 0:** not applicable (no `src/` to baseline). Stage 0 anchor
  baselines from FR-PO-072 are subject to the §3.3.2 session-contract
  check but not to the FR-PO-067 reproducibility re-run (synthetic
  harness has no scenario-deterministic semantics to re-run against
  until #16 §5 lands).
- **Stage 0+1:** for every baseline file in `tests/data/baselines/`,
  the auditor re-runs the recorded session manifest (seed, fingerprint,
  platform pin, scenario ID) and confirms the recaptured metric
  matches within the §3.4.3 confidence interval.
- **Failure:** baseline marked stale per FR-PO-068; the PR that
  introduced it is blocked from merge.

Auditor invocation lands in `src/CLAUDE.md` (§4.6).

## 5.5 Loop-Tag Conformance Auditor (KD-8)

Mechanics for FR-PO-009 … 015.

- **Stage 0 (manual).** The §5.3 auditor's pass simultaneously walks
  every §6 budget number for the `[LOOP-TACTICAL-10HZ]` /
  `[LOOP-PHYSICS-60HZ]` tag. Untagged numbers are logged as
  `ERR-018-NNN`.
- **Stage 0+1 (automated).** `tools/budget-auditor.py` regex pass
  rejects untagged numbers; the per-loop aggregation rule (FR-PO-012,
  FR-PO-013) is verified by separately totaling tagged entries.

## 5.6 FR-to-Verification Traceability

Single table indexed by FR-PO-###; columns: `Verification Mechanism |
Tooling | Activation Stage | Output Artifact`. For brevity the table
below lists the verification mechanism per FR range (the §3 / §5
subsection where the mechanic is detailed). Stage 0 most rows resolve
to "manual review against §3 mechanics" — acknowledged degenerate
(parallel to Spec #19 §5.6 / Spec #20 §5.5).

| FR range | Mechanism | Tooling | Stage | Artifact |
|----------|-----------|---------|-------|----------|
| FR-PO-001 … 008 | §5.3 schema-conformance auditor | Manual (Stage 0) → `tools/budget-auditor.py` | Stage 0 / 0+1 | Appendix D rows |
| FR-PO-009 … 015 | §5.5 loop-tag auditor | Manual → regex auditor | Stage 0 / 0+1 | Appendix D rows |
| FR-PO-016 … 023 | §5.4 baseline validator (session contract check) | Manual (Stage 0) → validator | Stage 0+1 | Baseline file pass/fail header |
| FR-PO-024 … 030 | PR review against §3.4 ladder evidence | Manual review (Stage 0+1) | Stage 0+1 | PR description checklist |
| FR-PO-031 … 040 | CI perf gate per §3.5 | Local runbook (Stage 0) → CI step (Stage 0+1) | Stage 0+1 | CI status + dashboard delta |
| FR-PO-041 … 047 | Spec-review against §3.6 degradation policy | Manual review | Stage 0 | Spec review notes |
| FR-PO-048 … 053 | Alloc-tracker step per §3.7.4 | None (Stage 0) → alloc-tracker | Stage 0+1 | CI status + alloc-dashboard |
| FR-PO-054 … 062 | §5.7 trace boundary review + dashboard inspection | Manual review → `tools/perf-dashboard/` | Stage 0+1 / Stage 1 | Channel registry + dashboards |
| FR-PO-063 … 068 | §5.4 baseline-reproducibility auditor | Manual (Stage 0) → auditor | Stage 0+1 | Auditor report |
| FR-PO-069 … 074 | Stage-0 local runbook execution (manual at Stage 0; automated at Stage 0+1) | `tools/run-perf-local.sh` (Stage 0 manual / Stage 0+1 automated invocation of `tools/budget-auditor.py`) | Stage 0 / 0+1 | Runbook output in PR description |
| FR-PO-075 … 080 | Reporting-cadence compliance check | Manual review of `PROGRESS.md` / dashboard | Stage 0 / 0+1 | `PROGRESS.md` rows / dashboard panels |

## 5.7 Boundary-Verification (KD-3 / KD-4)

### 5.7.1 #16 boundary check

Any change to:

- #16 §5 (regression scenarios) — scenario IDs or manifest format.
- #16 §3.2.4.1 (canonical record format) — field layout or version
  field.
- #16 §3.1.2 (canonical tick pipeline) — emission points or
  determinism-of-emission rules.

triggers a Spec #18 §3.3 / §3.8 review (recorded in §1.4 dependency
list).

The boundary check additionally walks the §3.8.2 channel registry at
each #18 revision and flags any registry entry that emits inside #16
§3.1 without recorded #16-owner sign-off (FR-PO-058a enforcement).

### 5.7.2 #19 boundary check

Any change to:

- #19 §6 (CI orchestration) — gate-composition or gate-ownership
  contract.
- #19 §3.7 (flake handling) — flake-quarantine policy.

triggers a Spec #18 §3.5.3 review.

### 5.7.3 Boundary-defect routing

Boundary breaches discovered in CI runs (e.g., perf gate firing on a
functional flake; functional gate failing because an alloc-tracker
init order changed) are routed to §6.4 defect-triage as a "Boundary
defect" class.

### 5.7.4 Stage-0 cadence

Stage 0: boundary review runs at each natural revision of #16 / #19
plus monthly anchor sweep (FR-PO-075). Stage 0+1: boundary review
runs at each CI invocation as the gate-ownership lookup table
(§3.5.3) is consulted.

## 5.8 Version History

| Version | Date         | Author      | Notes |
|---------|--------------|-------------|-------|
| 0.2     | May 14, 2026 | Claude Code | PASS-1 finding resolved: L-9 §5.7 #16 §3.1→§3.1.2 canonical tick pipeline citation. |
| 0.1     | May 13, 2026 | Claude Code | Initial draft from `outline-detailed.md` v1.1 §5. Reflexive verification model declared. Stage-gated activation table (KD-5) per FR range. Auditor procedures for schema-conformance (§5.3), baseline reproducibility (§5.4), loop-tag (§5.5), and boundary (§5.7) authored. Stage 0 acknowledged degenerate per parallel Spec #19 §5.1 / Spec #20 §5.1 convention. All #16 / #19 citations tagged `TBD-NORMATIVE`. |
| 0.2     | May 14, 2026 | Claude Code | PASS-1 adversarial-review fix pass (`ERR-018-009`). §5.2 stage-gated activation row for FR-PO-069 … 074 annotated to reflect FR-PO-070 Stage 0 manual leg vs Stage 0+1 automated leg; §5.6 FR-to-verification traceability row mirrored. |
