# Deterministic Simulation Specification #16 — Section 8: References & Citation Audit

## 8.1 Source Register
Primary internal sources:
- Deterministic Simulation outline (`outline.md`, v0.3 refined).
- Deterministic Simulation adversarial review (`adversarial-review.md`).
- Related system specs: event-system, testing-strategy, fixed64-math, performance-optimization.

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

Sign-off is blocked while any row remains pending.

### 8.3.1 Audit table
| Dependency | Verification action | Status |
|---|---|---|
| event-system | compare event ordering rules | pending |
| fixed64-math | compare comparator semantics | pending |
| testing-strategy | align certification gate wording | pending |
| performance-optimization | align overhead thresholds | pending |

## 8.4 Constant Provenance Summary
Constants in this spec are governance placeholders derived from outline policy and MUST be finalized during implementation design review.

### 8.4.1 Provenance fields
Each constant MUST declare: source section, owner team, initial rationale, verification method, and last-reviewed date.

## 8.5 Version History
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
