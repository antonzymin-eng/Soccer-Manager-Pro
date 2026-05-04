# Deterministic Simulation Specification #16 — Section 8: References & Citation Audit

## 8.1 Source Register
Primary internal sources:
- Deterministic Simulation section files §1–§9 (canonical normative content; latest versions per each file's `Version History`).
- Deterministic Simulation outline (`outline.md`, v0.4 — **SUPERSEDED** May 4, 2026 per Pass 5 H-1; retained as historical scaffolding only, not normative).
- Deterministic Simulation consolidated critique log (`critique-log.md`, v1.2; merges and supersedes the former `adversarial-review.md` and `third-pass-fix-log.md`).
- Related system specs: event-system (#17), testing-strategy (#19), fixed64-math (#9), performance-optimization (#18) — all currently `NOT STARTED` per `SPEC_INDEX.md`; see §8.3 sequencing constraint.

### 8.1.1 Citation quality rubric
| Grade | Criteria |
|---|---|
| A | direct normative dependency and verified terminology |
| B | supporting dependency, partial terminology overlap |
| C | contextual reference only |

## 8.2 Verification Notes
- Normative requirements mirrored from refined outline sections 1–8.
- Terminology standardized: Tier A/B/C, hard desync/soft drift/cosmetic divergence.
- Interface and testing obligations mapped to FR identifiers in Section 2 and Section 5.

### 8.2.1 Verification checklist
- naming consistency checked against glossary,
- formula symbols checked for collisions,
- error code namespaces checked for uniqueness,
- section cross-references validated.

## 8.3 Cross-Spec Citation Audit
Cross-spec dependencies to validate during approval:
- Event ordering semantics with Event System spec.
- Numeric comparator/tolerance policies with Fixed64 Math spec.
- Certification and CI gates with Testing Strategy spec.
- Overhead budgets with Performance Optimization spec.

**Sequencing constraint (May 2, 2026):** Per `SPEC_INDEX.md`, all four cited dependency specs are currently `NOT STARTED` (#9 Fixed64) or `NOT STARTED` (#17 Event System, #18 Performance Optimization, #19 Testing Strategy). Final sign-off of this spec is therefore explicitly **deferred** until each named dependency reaches `IN REVIEW` or `APPROVED` and a reciprocal audit row is filed. This is not a blocker on continued authoring of #16; it is a sequencing gate at the approval step.

### 8.3.1 Audit table
| Dependency | SPEC_INDEX status | Verification action | Status |
|---|---|---|---|
| event-system (#17) | NOT STARTED | compare event ordering rules | deferred dependency |
| fixed64-math (#9) | NOT STARTED | compare comparator semantics | deferred dependency |
| testing-strategy (#19) | NOT STARTED | align certification gate wording | deferred dependency |
| performance-optimization (#18) | NOT STARTED | align overhead thresholds | deferred dependency |

A row transitions from `deferred dependency → pending → complete` once the upstream spec reaches `IN REVIEW` and the line-by-line comparison can be executed.

## 8.4 Constant Provenance Summary
Constants in this spec are governance placeholders derived from outline policy and MUST be finalized during implementation design review.

### 8.4.1 Provenance fields
Each constant MUST declare: source section, owner team, initial rationale, verification method, and last-reviewed date.

## 8.5 Version History
- **v1.0 (May 4, 2026):** Pass 6 follow-up audit. §8.1 Source Register updated: outline reference corrected from `v0.3 refined` to `v0.4 SUPERSEDED` (matches Pass 6 H-1 freeze); section files added as primary canonical source; dependency specs annotated with their current `NOT STARTED` SPEC_INDEX status and pointer to §8.3 sequencing constraint.
- **v0.9 (May 3, 2026):** §8.1 source register updated to point at the consolidated `critique-log.md` (former `adversarial-review.md` and `third-pass-fix-log.md` merged and removed in the same commit).
- **v0.7 (May 2, 2026):** §8.3 sequencing constraint added: cited dependency specs (#9, #17, #18, #19) all currently `NOT STARTED`; audit rows reclassified as `deferred dependency`. This explicitly defers final sign-off on #16 until upstream specs reach `IN REVIEW`.
- **v0.5:** Added citation quality rubric, audit table, and constant provenance fields.
- **v0.3:** Initial citation audit draft aligned to refined deterministic outline.

## 8.6 Cross-Spec Consistency Checks
- Comparator names in this spec MUST match fixed64-math comparator glossary.
- Event ordering terms MUST match event-system ledger semantics.
- Certification pass/fail language MUST match testing-strategy release gates.

## 8.7 Audit Execution Procedure
1. Select dependency row from Section 8.3.1.
2. Compare normative terms line-by-line.
3. Record mismatch type (`terminology`, `algorithm`, `threshold`, `ownership`).
4. File reconciliation task with owning teams.
5. Mark row complete only after reviewer sign-off.

## 8.8 Evidence Package Requirements
Approval package SHOULD include:
- rendered diff excerpts,
- mapping notes per dependency,
- unresolved discrepancy list with owners and due dates.
