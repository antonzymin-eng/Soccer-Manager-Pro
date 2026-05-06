# Testing Strategy & Framework Specification #19 — Outline

## Purpose
Define a comprehensive test framework that enforces correctness, determinism, and gameplay quality.

## Scope
Test pyramid ratios, deterministic replay workflows, scenario architecture, fuzz/property testing, and triage.

## Section Plan
- Section 1 — Testing pyramid targets (unit/integration/simulation/regression proportions).
- Section 2 — Deterministic replay tests and golden-output workflow.
- Section 3 — Scenario-library architecture for tactical and physics validations.
- Section 4 — Property and fuzz testing for edge-case discovery.
- Section 5 — Quality gates for spec-complete and implementation-ready states.
- Section 6 — Tooling standards and CI orchestration.
- Section 7 — Failure triage and defect-lifecycle process.
- Section 8 — Coverage and stability reporting expectations.
- Section 9 — Approval checklist.
- Appendices — Test-data governance and flaky-test policy.

---

## ADVERSARIAL REVIEW — May 6, 2026

> Reviewer: AI agent (claude/review-spec-sections-JQ8jy). Scope: this outline file
> measured against `CLAUDE.md`, the 9-section template, and adjacent specs.
> Severity legend: **H** = blocks draft start; **M** = must resolve during draft;
> **L** = follow-up.

### Verified premises
- Spec #19 status in `SPEC_INDEX.md`: NOT STARTED. Stage 0 has no source code
  (`src/` directory empty pending all 20 specs approved).
- Approved specs already declare §5 test plans (Shot Mechanics #6 §5.1
  targets ~104 tests; Pass Mechanics #5 §5 ~similar). Test-count expectations
  are emerging without a unified governance document — this is the gap
  Spec #19 must close.
- Project history: ERR-005 (Pass Mechanics) — fabricated approval-checklist
  values. Testing strategy must defend against this class.

### Findings

1. **[H] Missing metadata header.** Same gap as siblings. Add per Shot
   Mechanics #6 outline header.

2. **[H] Section plan deviates from CLAUDE.md template.** Quality gates
   in §5 (formula territory per template), tooling in §6 (performance
   territory), triage in §7 (future-extensions territory), reporting in
   §8 (references territory). No references slot. Re-map.

3. **[H] Boundary with Deterministic Simulation #16 unresolved.** §2
   "deterministic replay tests" duplicates #16 §7 ("Determinism Regression
   Suite"). #16 already mandates: unit / integration / scenario / soak
   tiers, golden-trace governance, CI gating. Either #19 *consumes*
   #16's regression suite as the authoritative determinism layer and
   adds non-determinism testing on top, or #19 owns all testing and #16
   §7 becomes advisory. Pre-commit.

4. **[H] Boundary with Performance Optimization #18 unresolved.** §5
   "quality gates" overlaps with #18 §4 "regression thresholds and CI
   performance-gate policy" and §7 "validation protocols". Two specs
   owning quality gates is an audit trap. Pre-commit.

5. **[H] Pyramid ratios meaningless without implementation.** §1 "test
   pyramid ratios" presumes an implemented codebase. Stage 0 has zero
   code per CLAUDE.md ("No code exists yet"). Ratios become enforceable
   only at Stage 0+1 transition. Outline must classify §1 as a contract
   that activates with implementation, or move it to Stage 1+.

6. **[H] Programmatic-verification mandate missing.** ERR-005 (Pass
   Mechanics) shows Approval Checklists are vulnerable to fabricated
   values. The Testing Strategy spec is the single best place to mandate
   "every approval-checklist row resolves to a programmatic check or a
   named, version-controlled file". Outline does not commit to this —
   the project's strongest defence against the most-recurring bug class
   is missing from the spec that should own it.

7. **[M] Ownership of per-spec §5 sections unstated.** Every spec has
   its own §5 test plan. Are those plans authoritative, or does Spec #19
   normalize them? If normalized, the per-spec §5 sections in approved
   specs (#1, #2, #3, #4, #6, #7, #8) become subject to revision —
   touching APPROVED specs requires lead-developer authorization
   (CLAUDE.md). Decide and document.

8. **[M] Fuzz / property tests against deterministic system need
   deterministic seeds.** §4 "property and fuzz testing" without
   committing to seed governance via #16 `DeterministicRngService`
   produces flaky tests by construction. Pre-commit.

9. **[M] Scenario library architecture unclear about source of truth.**
   §3 "scenario-library architecture for tactical and physics
   validations" — are scenarios defined here, or is this the runner
   for scenarios defined in each spec's §5? Either is fine; outline must
   pick.

10. **[M] CI orchestration infeasible at Stage 0.** §6 "tooling standards
    and CI orchestration" presumes a CI environment that does not exist
    in spec phase. Either define Stage-0-feasible local-only tooling, or
    scope CI to Stage 1+.

11. **[M] No coverage-target distinction by tier.** Tier A authoritative
    code (#16 §1.2) likely needs near-100% line coverage; Tier C
    cosmetic code can tolerate much less. Outline §8 "coverage and
    stability reporting" should pre-commit per-tier targets.

12. **[L] Flaky-test policy in appendix only.** Flake handling is
    determinism-adjacent and arguably belongs in §2 alongside replay
    workflow.

13. **[L] No mention of test-data governance interaction with
    deterministic save format.** Test data (snapshots, golden traces)
    must conform to #16 §5 canonical binary layout. Cross-link.

### Recommended next steps
- Add full metadata header.
- Re-map Section Plan to CLAUDE.md 9-section template (esp. §8 references).
- Resolve boundary with Deterministic Simulation #16 §7 and Performance
  #18 §4 / §7.
- Promote programmatic-verification rule to a named FR in §2 — this is
  the project's strongest mitigation for ERR-005 class bugs.
- Decide owner of per-spec §5 sections; if normalized, list approved
  specs whose §5 sections will require revision and route through
  lead-developer authorization.
