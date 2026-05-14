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

**Sequencing constraint (updated May 14, 2026):** Per `SPEC_INDEX.md`, all four cited dependency specs have now reached at least `IN REVIEW` (#9 Fixed64 `IN REVIEW` May 6; #17 Event System `APPROVED` May 13; #18 Performance Optimization `IN REVIEW` May 14; #19 Testing Strategy `IN REVIEW` May 12). The upstream-spec gate on §9.4.3 / §9.4.2 is therefore cleared. Tier 2 final sign-off of #16 remains explicitly **deferred** until the per-row line-by-line audit below transitions every row to `complete` and the `[TBD-NORMATIVE: pending #N]` suffixes are stripped — see §9.4.2 for the corresponding Tier 2 audit task. This is not a blocker on Tier 1 (`CONDITIONAL APPROVAL — IN REVIEW`, signed off May 14, 2026); it is the residual sequencing gate at the Tier 2 (`APPROVED`) step.

### 8.3.1 Audit table
| Dependency | SPEC_INDEX status | Verification action | Status |
|---|---|---|---|
| event-system (#17) | APPROVED (May 13, 2026) | compare event ordering rules | pending re-audit [TBD-NORMATIVE: pending #17] — atomic with ERR-017-001 closure |
| fixed64-math (#9) | IN REVIEW (May 6, 2026) | compare comparator semantics | pending re-audit [TBD-NORMATIVE: pending #9] |
| testing-strategy (#19) | IN REVIEW (May 12, 2026) | align certification gate wording | pending re-audit [TBD-NORMATIVE: pending #19] |
| performance-optimization (#18) | IN REVIEW (May 14, 2026) | align overhead thresholds | pending re-audit [TBD-NORMATIVE: pending #18] |

A row transitions from `deferred dependency → pending re-audit → complete` once the upstream spec reaches `IN REVIEW` and the line-by-line comparison can be executed. All four rows have advanced to `pending re-audit` as of May 14, 2026; the comparison work itself is a Tier 2 deliverable per §9.4.2 item (2). Promotion to `complete` strips the `[TBD-NORMATIVE: pending #N]` suffix from that row.

## 8.4 Constant Provenance Summary
Constants in this spec are governance placeholders derived from outline policy and MUST be finalized during implementation design review.

### 8.4.1 Provenance fields
Each constant MUST declare: source section, owner team, initial rationale, verification method, and last-reviewed date.

## 8.5 Version History
- **v1.1 (May 14, 2026):** §8.3 sequencing constraint and §8.3.1 audit table refreshed against current `SPEC_INDEX.md`. All four upstream-spec rows updated from `NOT STARTED → deferred dependency` to current statuses (#9 IN REVIEW, #17 APPROVED, #18 IN REVIEW, #19 IN REVIEW) and reclassified `deferred dependency → pending re-audit` with `[TBD-NORMATIVE: pending #N]` suffix per §9.4.1. Sequencing constraint prose updated to record Tier 1 sign-off (May 14, 2026) and isolate the residual Tier 2 audit work. No normative content change — the §8.4 constants and §8.6 cross-spec consistency rules are unchanged.
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
