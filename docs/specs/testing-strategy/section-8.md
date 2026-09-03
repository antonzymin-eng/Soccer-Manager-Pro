# Testing Strategy & Framework Specification #19 — Section 8: References & Citation Audit

**Created:** May 12, 2026
**Last Updated:** September 3, 2026
**Version:** 0.3
**Status:** AMENDMENT DRAFT (A3.2b; May 15, 2026 approved baseline remains in force)
**Amendment plan:** `docs/planning/project-architecture-governance-integration-plan.md` v0.38, §7; A3.2b
**Purpose:** Source register, verification notes, cross-spec citation
audit, and constant-provenance summary.

---

## 8.1 Source Register

### 8.1.1 Internal Sources

- **Root `CLAUDE.md`** — project invariants, "When Writing Code"
  rules, coordinate system, fatigue convention, constant-tag
  taxonomy, Interface Design Principle. Authoritative for KD-7
  (SplitMix64), banned-API list (`System.Random`, `DateTime.Now`),
  Stage 0 `float` posture.
- **Project Architecture Governance v0.10** — architectural-property
  admission, applicability/exclusion authority, proof triggers,
  disposition/status semantics, evidence freshness requirements, and
  convergence. Consumed by FR-TS-086 … 096, §3.11, §5.6, and §6.2.
- **Spec #16 (Deterministic Simulation)** —.
  - **§1.1.1** — tier classification ("Equivalence policy by
    artifact"). Consumed by KD-9 (§3.6.2) and FR-TS-029, FR-TS-050,
    FR-TS-060.
  - **§3.2.4.1** — `SerializeCanonical` normative byte-level schema.
    Consumed by KD-10 (§3.3.4, §3.8.1, §4.2, FR-TS-026, FR-TS-034,
    FR-TS-069).
  - **§3.4.2** — Tier B comparator default policy. Consumed by
    `AssertWithinTolerance` (§4.3.2).
  - **§4.8** — `EnvironmentFingerprint` (environment pinning).
    Consumed by FR-TS-061, FR-TS-071, §3.7.5 ("flaky in CI only"
    diagnostic).
  - **§5** — Test Strategy / Test Catalogue / Certification Matrix /
    Detailed Test Fixture Requirements / Test Card Template
    (regression-suite authority). Consumed by KD-2 (§3.2,
    FR-TS-011 … 020).
  - Status: `APPROVED (Tier 2, May 14, 2026)`. Section numbers
    re-grepped against `deterministic-sim/section-1.md`, `section-3.md`,
    `section-4.md`, and `section-5.md` on May 12, 2026 (v0.2 sweep)
    and again on May 15, 2026 (v1.0.1 `[TBD-NORMATIVE]` sweep against
    final APPROVED text). Per §3.6.1 cite-precision guard, MUST be
    re-grepped at next revision touching §3.2 / §3.6 / §3.7 / §5.7 /
    §6.2 / §6.6 / §8.
  - **Outline-vs-actual divergences fixed in v0.2 sweep:** v1.1 outline
    had #16 §1.3.1 (actual: §1.1.1), #16 §5 for canonical layout
    (actual: §3.2.4.1), #16 §7 for regression suite (actual: §5),
    #16 §8 for "trace channels" (no such section exists in current
    #16). All citations corrected.
- **Spec #18 (Performance Optimization)** —.
  - §4 — performance regression gates. Consumed by KD-3 (§6.2,
    FR-TS-080).
  - §7 — performance budget enforcement.
  - Status: `IN REVIEW` (section files at v0.3, May 14, 2026).
    `outline-detailed.md` v1.1 landed May 13 and section files reached
    v0.3 May 14 (PASS-2 adversarial review resolved). The cited §4
    perf-regression-gate authority and §7 perf-budget-enforcement
    authority are now stable in #18's IN REVIEW text. KD-2 precondition
    (b) cleared; #19 v1.0.1 (May 15, 2026) `[TBD-NORMATIVE]` sweep
    resolved all #18 citation tags.
- **Spec #20 (Code Standards)** — APPROVED May 11, 2026.
  - §2.1 — conformance levels (consumed by §2.1).
  - §3.4.2 — `System.Random` ban (cited by FR-TS-036).
  - §3.5.5 — IoC anti-pattern (cited by §4.3.1).
  - §3.9.4 — test-fixture rule carve-outs (cited by §1.4, §3.9.4).
  - §4.1 — dependency-arrow shape (cited by §4.1).
  - §5.1 — Stage 0 manual-review acknowledgement (cited by §5.1).
  - §5.2 — Roslyn analyzer pin (cited by §6.1.2).
  - §5.3 — Stage 1 numeric-threshold revisit (cited by §3.1.2).
  - §5.5 — degenerate Stage 0 traceability acknowledgement (cited by
    §5.6).
  - §7.3 — Stage 5+ posture (cited by §7.3).
- **`docs/planning/development-best-practices.md`** — general project
  conventions; consulted at draft time.
- **`docs/planning/master-development-plan.md`** — stage-gating
  rationale; consulted for KD-5.
- **`docs/tracking/certification-platform.md`** — placeholder at
  draft time per CLAUDE.md OPEN ISSUES; cited in §1.4 for Stage 0+1
  activation precondition.
- **`docs/tracking/spec-error-log.md`** — destination for
  `ERR-019-NNN` rows logged by §3.5.4 acknowledged dilution policy.

### 8.1.2 External Sources

- **RFC 2119** — MUST / SHOULD / MAY keywords. Consumed by §2.1.
- **NUnit 3.14.0 / NUnit3TestAdapter 4.6.0** — current test-runner pin
  established by `tools/dotnet-ci/generate_projects.py` (D1).
- **FsCheck / Coverlet / Stryker.NET** — remaining property/coverage/
  project-wide-mutation candidates; D2/D3/D6 remain deferred. Targeted
  FR-TS-091 governance mutation does not depend on the D6 choice.
- **GitHub Actions** — current CI provider at
  `.github/workflows/ci.yml` (D4). This source registration does not
  activate the architecture/evidence gate.

## 8.2 Verification Notes

- Every CLAUDE.md citation in §3 was verified against the current
  CLAUDE.md text on this spec's drafting date (May 12, 2026).
- All Spec #16 citation `[TBD-NORMATIVE]` qualifiers were swept on
  May 15, 2026 (v1.0.1 patch) following #16 Tier 2 `APPROVED` on
  May 14. Section numbers (#16 §1.1.1, §3.2.4.1, §3.4.2, §4.8, §5)
  re-grepped against final `deterministic-sim/section-*.md` text;
  §3.6.1 cite-precision guard mandates re-grep at every subsequent
  revision for any author touching §3.2, §3.6, §3.7, §5.7, §6.2, §6.6,
  or this §8.
- All Spec #18 citation `[TBD-NORMATIVE]` qualifiers were swept on
  May 15, 2026 (v1.0.1 patch) against #18's IN REVIEW v0.3 surface
  (KD-2 precondition (b) cleared May 13). If #18 churn shifts §4 / §7
  subsection numbers before #18 reaches `APPROVED`, file the
  re-introduction per §2.3 self-applied failure mode and flip #19
  status to `SUSPENDED`.
- Every Spec #20 citation was verified against
  `code-standards/section-N.md` at draft time. The A3 amendment also
  consumes FR-CS-074 … 081 / §3.5 / Appendix F for runtime-surface,
  integration, lifecycle, activation, and disable-anchor declarations;
  #19 does not own those declarations.
- A3.2b rechecked the new #19 proof/evidence references against
  Governance v0.10, the frozen A2 schema family, reference semantics
  v2.1.0, and the coordinated #20 A3 draft. This is citation/ownership
  synchronization only; it is not evidence that the A8 gate is active.

## 8.3 Cross-Spec Citation Audit

- **Cited by:** every per-spec §5 (downstream). The cited content is
  Spec #19's taxonomy, naming convention, coverage-tier policy, and
  Appendix C schema.
- **Cites:**
  - #16 (substantive): tier classification §1.1.1 (§3.6.1, §8.1.1),
    canonical schema §3.2.4.1 (§3.3.4, §3.8.1, §4.2), Tier B
    comparator §3.4.2 (§4.3.2), `EnvironmentFingerprint` §4.8
    (§3.7.1, §3.8.4), regression suite §5 (§3.2)..
  - #18 (boundary): performance gates (§6.2, §6.6)..
  - #20 (boundary): test-fixture carve-outs (§3.9.4), IoC anti-pattern
    (§4.3.1), `System.Random` ban (§3.4.5), and A3 integration/
    activation declarations consumed by §3.11 / §6.2.
  - Project Architecture Governance v0.10 (substantive governance
    authority): applicability, proof triggers, bounded exclusions,
    evidence freshness, disposition/status, and convergence consumed
    by FR-TS-086 … 096.
- **No `[CROSS]` constants imported.** Spec #19 declares no physical
  constants. Tier vocabulary cited from #16 §1.1.1 by reference only
  (KD-1).

## 8.4 Constant Provenance Summary

Spec #19 declares **no physical constants**. The numeric thresholds
it publishes are governance values (`[GT]`); the full table is in
§3.10. Provenance summary:

| Constant | Tag | Provenance |
|---------|-----|------------|
| Pyramid percentages (60% / 25% / 12% / 3%) | `[GT]` | Standard pyramid heuristic; revisited Stage 1 (§3.1.2). |
| Tier A coverage (98% / 95%) | `[GT]` | KD-9 authoritative-tier target (§3.6.2). |
| Tier B coverage (90% / 80%) | `[GT]` | KD-9 bounded-authoritative-tier target (§3.6.2). |
| Unit wall-time bound (1 ms) | `[GT]` | Sub-millisecond fast-feedback bound (§3.1.1). |
| Quarantine auto-expiry (14 days) | `[GT]` | Two-week resolution window (§3.7.3). |
| Eviction threshold (3 quarantines / 90 days) | `[GT]` | Three-strikes rule (§3.7.4). |

KD-6 evidence artifact for each value: the section-file citation that
publishes the number (§3.10 evidence-artifact convention; §9.4
checklist row).

## 8.5 Version History

| Version | Date         | Author      | Notes |
|---------|--------------|-------------|-------|
| 0.1     | May 12, 2026 | Claude Code | Initial draft from `outline-detailed.md` v1.1. Source register populated; constant-provenance summary references §3.10. |
| 0.2     | May 12, 2026 | Claude Code | Self-critique sweep. #16 source register rebuilt against actual #16 layout: §1.1.1 (tier), §3.2.4.1 (canonical schema), §3.4.2 (Tier B comparator), §4.8 (`EnvironmentFingerprint`), §5 (regression suite). Outline-vs-actual divergences enumerated. §8.3 cite mapping updated. |
| 0.3     | September 3, 2026 | — | **A3.2b supporting-surface synchronization.** Registers Governance v0.10 and the #20 A3 integration/activation contract as explicit authorities for FR-TS-086 … 097 and records the A2 v2.1.0 proof-semantics verification boundary. No enforcement activation or `SPEC_INDEX.md` change. Live-repo audit also records D1=NUnit and D4=GitHub Actions; D2/D3/D5–D8 remain deferred. |
