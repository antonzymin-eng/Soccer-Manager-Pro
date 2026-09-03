# Testing Strategy & Framework Specification #19 — Section 1: Purpose & Scope

**Created:** May 12, 2026
**Last Updated:** September 3, 2026
**Version:** 0.3
**Status:** AMENDMENT DRAFT (A3.2b; May 15, 2026 approved baseline remains in force)
**Amendment plan:** `docs/planning/project-architecture-governance-integration-plan.md` v0.37, §7; A3.2b
**Purpose:** Establishes what Spec #19 governs, what it excludes, the
ten cross-cutting key decisions (KD-1 … KD-10) that bind the rest of
the document, the upstream / downstream contracts, and the version
history.

---

## 1.1 What This Specification Covers

Spec #19 is the project's authoritative governance document for
**how the codebase is tested** — not what is tested, which is owned by
each subsystem spec's §5. Its scope is:

- **Test taxonomy and pyramid contract.** The five-layer test
  classification (unit / integration / simulation / determinism /
  end-to-end-or-soak) and the count-ratio contract every test suite
  must satisfy. See §3.1.
- **Deterministic-replay test consumption.** The contract by which
  Spec #19 *consumes* Spec #16 §5's determinism regression suite as a
  required test layer, without duplicating or overriding it (KD-2).
  See §3.2.
- **Scenario-library architecture.** The runner contract, manifest
  schema, fixture validator, and directory layout for both per-spec
  scenarios (owned by each spec's §5) and cross-spec scenarios (owned
  by Spec #19). See §3.3, Appendix A.
- **Property / fuzz testing with seed governance.** The framework
  selection criteria, seed-routing-through-`DeterministicRngService`
  rule (KD-7), failed-seed capture, and property catalogue. See §3.4,
  Appendix B.
- **Programmatic-verification mandate (KD-6).** Every
  approval-checklist row in every spec MUST resolve to either a named,
  version-controlled file path or a programmatic check whose output is
  captured. Auditor mechanics are in §3.5 and §5.3.
- **Per-spec §5 conformance schema (KD-4).** Spec #19 publishes the
  taxonomy, naming, coverage targets, and quality-gate criteria every
  per-spec §5 must conform to; it does not rewrite those §5 sections.
  See §3.5.3 and Appendix C.
- **CI orchestration policy (Stage-gated).** The pipeline topology,
  gate composition rule, defect lifecycle, and reporting cadence. See
  §6.
- **Coverage and flake reporting.** Per-tier coverage targets bound to
  #16 §1.1.1 tier classification (KD-9), and the flake-detection /
  quarantine / eviction rules (§3.7). All Stage-gated per KD-5.
- **Architecture proof and evidence integration.** Spec #19 owns the
  reusable proof-artifact contract, execution truth, bounded-substitute
  mechanics, freshness/revalidation, and architecture/evidence gate
  consumption defined by FR-TS-086 … 097, §3.11, Appendix G, §5.6,
  and §6.2. Governance owns the architectural property/applicability/
  convergence decision layer; Spec #20 owns integration and activation
  declarations. This amendment does not activate the new gate.

**Applicability.**

- **Primary:** every test file under `src/<spec>/tests/` once coding
  begins (Stage 0+1 transition).
- **Secondary (governance-only):** every spec's §5 section under
  `docs/specs/`. Spec #19 publishes the taxonomy and naming those §5
  sections must use; it does not rewrite them (KD-4).

For taxonomy mechanics see §3; for quality gates see §5; for CI
orchestration and defect triage see §6.

## 1.2 What Is Out of Scope

Each line below cites the owning document.

- **Determinism regression suite mechanics** (tier definitions, golden
  traces, `EnvironmentFingerprint` gates) → Spec #16 §5 (Test Strategy
  / Test Catalogue / Certification Matrix / Detailed Test Fixture
  Requirements / Test Card Template). Spec #19 consumes the suite as
  a required layer; it does not duplicate or override it (KD-2).
- **Performance regression gates and budget enforcement** → Spec #18
  §4 / §7 (KD-3).
- **Numeric correctness of physics / AI formulas** → owning specs
  (#1–#8) §3.
- **C# code style and banned-API rules** → Spec #20 (Code Standards).
- **Architectural-property admission, applicability authority, property
  exceptions, finding disposition, and convergence semantics** →
  `docs/planning/project-architecture-governance.md` v0.10.
- **Runtime-surface identity, integration ownership, lifecycle/host
  declarations, activation state, and disable anchors** → Spec #20
  FR-CS-074 … 081 and §3.5. Spec #19 consumes those declarations for
  proof; it does not redefine them.
- **Fixed64 numeric-library tests** → Spec #9 §5.
- **CI server choice, build commands, IDE configuration** →
  `src/CLAUDE.md` (deferred until coding begins).
- **Asset-pipeline / content QA** → Stage 1+ specs.
- **PR-process rules** (review approval count, branch-protection,
  required-reviewers) → repository settings.

## 1.3 Key Design Decisions

Ten cross-cutting decisions referenced throughout this spec. The
**authoritative definition** of each KD is in this section file (here);
the codification map below names the §3 / §5 / §6 subsection that
publishes the rule mechanics. `outline-detailed.md` is a drafting
artifact and is **no longer authoritative** once this section file is
APPROVED — if the two diverge, this file wins. Sequencing-constraint
text and status-caveat text live in §1.4 (dependency contracts), not
in §1.3, so the KD definitions here remain stable when upstream spec
status changes.

- **KD-1 — Cite-not-redefine.** Spec #19 never restates a CLAUDE.md
  invariant or a rule already published by another approved spec. It
  cites and binds.
- **KD-2 — Boundary with Deterministic Simulation #16.** #16 §5 (Test
  Strategy / Test Catalogue / Certification Matrix / Fixture
  Requirements / Test Card Template) is the authoritative owner of
  the determinism regression suite. Spec #19 consumes; it does not
  duplicate. Status caveats and sequencing constraints with #16 live
  in §1.4.
- **KD-3 — Boundary with Performance Optimization #18.** #18 §4 / §7
  owns performance regression gates and budget enforcement. Spec #19
  owns functional / behavioural regression gates. Both feed a single
  CI orchestrator declared here. Status caveats with #18 live in §1.4.
- **KD-4 — Per-spec §5 ownership.** Spec #19 does not rewrite or
  supersede the §5 test plans in approved specs. It publishes the
  taxonomy, naming, coverage targets, and quality-gate criteria those
  §5 sections must conform to. New specs adopt the schema directly;
  approved specs reconcile at next natural revision.
- **KD-5 — Stage-gated activation.** Sections that presume an
  implemented codebase (pyramid ratios, CI gates, coverage thresholds,
  flake handling) are first-class normative content but activate only
  at the Stage 0 → Stage 1 transition. Activation status is recorded
  per-FR in §5.2.
- **KD-6 — Programmatic-verification mandate.** Every
  approval-checklist row in every spec MUST resolve to either (a) a
  named, version-controlled file path containing the claimed value, or
  (b) a programmatic check whose output is captured in CI logs. This
  is the project's strongest mitigation against the CLAUDE.md
  "fabricated checklist values" hazard pattern (the original
  manifestation: an Approval Checklist claiming sections existed that
  were never written; CLAUDE.md "Things That Have Gone Wrong" table).
- **KD-7 — Determinism-aware fuzz / property testing.** Seeds may be
  selected non-deterministically at discovery time, but every executed
  test body re-runs from the recorded seed via
  `DeterministicRngService` (`SplitMix64`). Failed seeds are captured
  verbatim.
- **KD-8 — Scenario-library source of truth.** Per-spec scenarios are
  defined in their owning spec's §5. Cross-spec scenarios are defined
  in Spec #19 §3 and stored in the scenario library. Spec #19 owns the
  runner, the file format, the manifest, and the index.
- **KD-9 — Per-tier coverage policy.** Coverage targets are bound to
  the determinism tier classification in #16 §1.1.1 ("Equivalence
  policy by artifact"): Tier A near-100% line + branch; Tier B ≥90%
  line, ≥80% branch; Tier C lint-only.
- **KD-10 — Test-data ↔ canonical save format binding.** Golden
  traces, snapshot fixtures, and replay corpora MUST conform to #16
  §3.2.4.1 (`SerializeCanonical` normative byte-level schema).
  Fixture-format drift is a §5-blocking finding.

**Codification map.**

| KD | Topic | Codified in |
|----|-------|-------------|
| KD-1 | Cite-not-redefine | All sections |
| KD-2 | Boundary with #16 §5 | §3.2, §5.1, §5.7 |
| KD-3 | Boundary with #18 §4 / §7 | §6.2 |
| KD-4 | Per-spec §5 ownership | §3.5, §5.4 |
| KD-5 | Stage-gated activation | §5.2, §7 |
| KD-6 | Programmatic-verification mandate | §2.2 (FR-TS-040…045), §3.5, §5.3 |
| KD-7 | Determinism-aware fuzz | §3.4, §3.4.3 |
| KD-8 | Scenario-library source of truth | §3.3 |
| KD-9 | Per-tier coverage policy | §3.6, §5.5 |
| KD-10 | Test-data ↔ canonical save format binding | §3.3.4, §3.8, §4.2 |

## 1.4 Dependencies and Integration Contracts

**Upstream (substantive).**

- Root `CLAUDE.md` — project invariants and "When Writing Code" rules
  (deterministic RNG mandate, banned `System.Random` / `DateTime.Now`,
  constant-tag taxonomy).
- Spec #16 (Deterministic Simulation) — §1.1.1 tier classification
  ("Equivalence policy by artifact"), §3.2.4.1 canonical byte-level
  schema (`SerializeCanonical`), §4.8 environment pinning
  (`EnvironmentFingerprint`), §5 test strategy / fixture requirements
  / certification matrix / test card template (regression-suite
  authority). **Status: `APPROVED` (Tier 2, May 14, 2026).** All
  `TBD-NORMATIVE` citation tags swept and replaced with firm references
  in #19 v1.0.1 patch (May 15, 2026); subsection numbers re-verified
  against the now-stable `deterministic-sim/section-*.md` text.

**Upstream (consulted).**

- Spec #18 (Performance Optimization) §4 / §7 — performance gates.
  **Status: `IN REVIEW` (section files at v0.3, May 14, 2026).** All
  `TBD-NORMATIVE` citation tags swept in #19 v1.0.1 (May 15, 2026)
  against #18's now-stable v0.3 surface; the cited §4 / §7 / §3.5
  perf-gate authorities are firm.
- Project Architecture Governance v0.10 — architectural-property,
  applicability, exception, evidence-trigger, disposition, and
  convergence authority. **Status: approved under A0.**
- Spec #20 (Code Standards) — test-fixture carve-outs plus the A3
  integration/activation ownership contract (FR-CS-074 … 081; §3.5;
  Appendix F). The May 11 approved baseline remains operative while
  its coordinated A3 amendment awaits A3.4 reapproval.

**Bidirectional sequencing with #16 (resolved May 15, 2026).**

- #16's Tier 2 final approval was gated on `#9 / #17 / #18 / #19`
  reaching `IN REVIEW`. **Cleared:** #9 IN REVIEW May 6 (later APPROVED
  May 15); #17 APPROVED May 13; #18 IN REVIEW May 14; #19 IN REVIEW
  May 12.
- #16's `APPROVED` status was a precondition for #19's own `APPROVED`
  status. **Cleared May 14, 2026** when #16 reached Tier 2 `APPROVED`.
- Resolution path executed: (1) #19 reached `IN REVIEW` May 12, 2026
  with `TBD-NORMATIVE` citations to #16; (2) #16 reached Tier 2
  `APPROVED` May 14, 2026; (3) #19's `TBD-NORMATIVE` tags swept May 15,
  2026 (v1.0.1 patch); #19 advancement to `APPROVED` granted same day.

**Downstream.**

- Every per-spec §5 (consumes Spec #19 taxonomy and Appendix C schema).
- `src/CLAUDE.md` (consumes test-runner / harness invocation).
- `.github/workflows/ci.yml` and `tools/dotnet-ci/` (current CI /
  NUnit runner integration; D1/D4 are resolved repository facts).

**Cross-spec constants imported.** None. Spec #19 imports tier
*vocabulary* from #16 §1.1.1 by reference (KD-1); no `[CROSS]`
constant declarations.

**Certification platform pin.** Determinism certification uses
`docs/tracking/certification-platform.md`, currently **PINNED** after
the July 19, 2026 certification against Windows 11 / Unity 6000.4.9f1
(DX11) / Mono. The Linux `tools/dotnet-ci` gate is a separate
non-certifying compile/test execution surface.

## 1.5 Version History

| Version | Date         | Author      | Notes |
|---------|--------------|-------------|-------|
| 0.1     | May 12, 2026 | Claude Code | Initial draft from `outline-detailed.md` v1.1. SPEC_INDEX flip to `IN REVIEW` is **author-driven**, not review-driven: it reflects "draft complete, awaiting lead-developer sign-off" per CLAUDE.md status definition. The §9 approval-checklist rows have not been walked. |
| 0.2     | May 12, 2026 | Claude Code | Self-critique fixes (16 H/M/L findings). #16 section-number sweep: §1.3.1 → §1.1.1 (tier classification), §7 → §5 (regression suite), §5 → §3.2.4.1 (canonical schema), §8 → deleted (no trace channels in current #16). KD-1 / KD-2 / KD-3 narratives tightened; status / sequencing text moved out of §1.3 into §1.4. ERR-005-class fabrication terminology corrected (no ERR-005 binding; ERR-019 namespace used). M3 IFixtureValidator deferral judgment made explicit. M4 storage-layout reconciliation. M5 §6.4.2 Stage-gating. L1–L3 [GT] tags. L4 property-naming reconciliation. L5–L8 cross-reference and policy-placement fixes. |
| 0.3     | September 3, 2026 | — | **A3.2b supporting-surface synchronization.** Adds the Governance / Spec #20 ownership boundary for FR-TS-086 … 097, points scope to §3.11 / Appendix G / §5.6 / §6.2, and records that the architecture/evidence gate remains unactivated pending A3.4/A4/A8. Approved May 15 baseline remains in force; `SPEC_INDEX.md` unchanged. The live-repo pass also records D1=NUnit, D4=GitHub Actions, and the July 19 certified platform pin without changing enforcement. |
