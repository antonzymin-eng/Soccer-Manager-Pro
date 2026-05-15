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

**Sequencing constraint (updated May 14, 2026 — line-by-line audit pass):** Per `SPEC_INDEX.md`, all four cited dependency specs reached at least `IN REVIEW` (#9 Fixed64 `IN REVIEW` May 6; #17 Event System `APPROVED` May 13; #18 Performance Optimization `IN REVIEW` May 14; #19 Testing Strategy `IN REVIEW` May 12). The upstream-spec gate on §9.4.3 / §9.4.2 is cleared. The line-by-line per-row re-audit (Tier 2 deliverable per §9.4.2 item (2)) was executed against the actual upstream spec text on May 14, 2026 and the §8.3.1 table updated below; three rows (#17, #18, #19) advanced to `complete`, and #9 advanced to `complete (deferral documented)` (the only consumed cross-reference — `TIER_B_DEFAULT_COMPARATOR = AbsEpsilon` class name — is a forward placeholder for a Stage-5+ Fixed64 comparator glossary that does not yet exist in #9; the deferral is intentional and documented per §8.3.2 below). All four `[TBD-NORMATIVE: pending #N]` suffixes have therefore been stripped. Residual Tier 2 work after this audit pass is limited to §9.3 review-checklist sign-offs (lead-developer Tier 2, QA-automation, platform-certification).

### 8.3.1 Audit table
| Dependency | SPEC_INDEX status | Verification action | Status |
|---|---|---|---|
| event-system (#17) | APPROVED (May 13, 2026) | compare event ordering rules | complete (May 14, 2026) — ERR-017-001 closed atomically: `DOMAIN_TAG_EVENT_LEDGER = 0x15` allocated in #16 §3.4 (next value after `DOMAIN_TAG_ENV_FP = 0x14`); #17 §3.10 `[CROSS-PENDING]` row promotes to `[CROSS]` at #16 Tier 2 `APPROVED` per #17 §3.4.2; FM-017-001 `EventLedgerDigestScope` formula reuses #16 §3.2.4.1 `SerializeCanonical` semantics verbatim (no algorithmic divergence); #17 §3.7.1 producer-phase-change protocol mirrors the ERR-016-002 / `XC-002-001` back-prop pattern. No terminology, algorithm, threshold, or ownership mismatch found. |
| fixed64-math (#9) | IN REVIEW (May 6, 2026) | compare comparator semantics | complete — deferral documented (May 14, 2026). #9 §8.1 v0.2 confirms the Stage 0–4 `float` / Stage 5+ Fixed64 staging that #16 is drafted against; no Stage scoping mismatch. The `TIER_B_DEFAULT_COMPARATOR = AbsEpsilon` class name in #16 §3.4 has no upstream definition in #9 (no comparator glossary published; #9 §3 catalogues utility-math error envelopes only). This is a forward-looking placeholder for a Stage-5+ design deliverable; see §8.3.2 below. Per CLAUDE.md "Interface Design Principle" (only write interfaces when both sides are specified), the absence is intentional, not a drift. The §6.6 cross-spec consistency rule remains in force as a future-binding constraint on whoever publishes the #9 comparator glossary. |
| testing-strategy (#19) | IN REVIEW (May 12, 2026) | align certification gate wording | complete (May 14, 2026) — #19 §5.7 explicitly defers all numerical-determinism testing to #16 §5's full tier set (no parallel pass/fail vocabulary in #19); #19 FR-TS-011 / FR-TS-012 / FR-TS-015 carry the certification-gate wording as a `TBD-NORMATIVE` cite of #16 §5 (resolvable at #16 Tier 2 `APPROVED`); FR-TS-015 is a boundary-review guard that auto-triggers a #19 §3.2 review on any #16 §5 tier-name or exit-criterion change, preserving alignment under future revisions. No terminology, algorithm, threshold, or ownership mismatch found. |
| performance-optimization (#18) | IN REVIEW (May 14, 2026) | align overhead thresholds | complete (May 14, 2026) — #18 KD-3 (v1.1, May 13) inverts the trace-pipeline boundary as required: #16 retains authority over (a) §3.2.4.1 canonical record format, (b) §3.1 determinism-of-emission veto over tick-pipeline trace points, and (c) §5 regression-scenario corpus; #18 owns trace pipeline + aggregation only. #18 FR-PO-058a (v0.2, May 14) enforces #16-owner sign-off for any #18-proposed trace point inside the §3.1.2 canonical tick pipeline. The 0 bytes/tick hot-path allocation budget is `[FIXED]` per #18 §3.10 / §8.4 v0.2 (reclassified from `[GT]` in PASS-1 fix pass) and derives from #16's determinism constraints, not overridden by #18. No terminology, algorithm, threshold, or ownership mismatch found. |

A row transitions from `deferred dependency → pending re-audit → complete` once the upstream spec reaches `IN REVIEW` and the line-by-line comparison can be executed. All four rows have transitioned to `complete` as of May 14, 2026 (the #9 row's `complete` qualifier records the comparator-glossary deferral per §8.3.2). The corresponding `[TBD-NORMATIVE: pending #N]` suffixes have been stripped.

### 8.3.2 Documented deferral — Fixed64 comparator glossary (#9)
The `TIER_B_DEFAULT_COMPARATOR = AbsEpsilon` class name declared in §3.4 references a comparator type system that #9 has not yet published. The absence is intentional under the May 14, 2026 audit pass for two reasons:
1. **Stage scoping.** #9 §8.1 v0.2 binds Fixed64 (and therefore the Fixed64-side comparator surface) to Stage 5+. #16 is drafted Stage 0 against `float` per CLAUDE.md "When Writing Code". A comparator glossary in #9 is not required for #16 Tier 2 `APPROVED` because no Stage-0 code path consumes it.
2. **Interface-design principle.** Per CLAUDE.md "Write interfaces only when both sides are specified" (ERR-001 / ERR-004 mitigation), authoring the comparator glossary in #16 against an unwritten #9 surface would manufacture exactly the phantom-interface class of error the rule exists to prevent.

**Forward-binding constraint (preserved).** §6.6 ("Comparator names in this spec MUST match fixed64-math comparator glossary") remains in force as a future-binding constraint on whoever publishes #9's comparator glossary. When that publication lands, the audit row above re-opens for one of two outcomes: (a) `AbsEpsilon` matches verbatim — promote to fully-resolved `[CROSS]`; or (b) the glossary chooses a different name — file a back-prop into #16 §3.4 to rename the constant atomically with the new #9 surface (same pattern as ERR-016-002 / `XC-002-001`).

**Rationale for not blocking Tier 2 on this.** The deferral is structurally identical to #19's `TBD-NORMATIVE` cites of #16 (one direction of an unwritten interface) — both rely on the cross-reference taxonomy to keep the binding enforceable across the gap. Treating this as a blocker would re-introduce the exact chicken-and-egg the §9.4 two-tier model exists to prevent.

## 8.4 Constant Provenance Summary
Constants in this spec are governance placeholders derived from outline policy and MUST be finalized during implementation design review.

### 8.4.1 Provenance fields
Each constant MUST declare: source section, owner team, initial rationale, verification method, and last-reviewed date.

## 8.5 Version History
- **v1.2 (May 14, 2026, later same day):** Tier 2 line-by-line cross-spec re-audit pass executed. All four §8.3.1 rows promoted from `pending re-audit` → `complete` (the #9 row carries a `complete (deferral documented)` qualifier per new §8.3.2). All four `[TBD-NORMATIVE: pending #N]` suffixes stripped. Audit findings per row: (i) **#17** — `DOMAIN_TAG_EVENT_LEDGER = 0x15` allocated in #16 §3.4 (next value after `DOMAIN_TAG_ENV_FP = 0x14`); ERR-017-001 closed atomically; #17 §3.10 `[CROSS-PENDING]` row promotes to `[CROSS]` at #16 Tier 2 `APPROVED`; FM-017-001 reuses #16 §3.2.4.1 `SerializeCanonical` verbatim; no algorithmic divergence. (ii) **#9** — Stage-scoping (#9 §8.1 v0.2 Stage 5+ Fixed64) confirmed consistent with #16's Stage 0 float baseline; comparator glossary not yet published in #9; deferral documented in new §8.3.2 with forward-binding §6.6 constraint preserved per CLAUDE.md "Interface Design Principle". (iii) **#19** — §5.7 defers all numerical-determinism testing to #16 §5; FR-TS-015 boundary-review guard preserves alignment under future #16 §5 revisions; no parallel pass/fail vocabulary in #19. (iv) **#18** — KD-3 v1.1 inverts the trace-pipeline boundary; #16 retains §3.2.4.1 / §3.1 / §5 authority; FR-PO-058a v0.2 enforces #16-owner sign-off for in-pipeline trace points; 0 bytes/tick budget `[FIXED]` per #18 §3.10 / §8.4 v0.2. New §8.3.2 added documenting the #9 comparator-glossary deferral. Sequencing-constraint prose in §8.3 updated to record the audit completion. No normative §8.4 / §8.6 changes.
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
