# Testing Strategy & Framework Specification #19 — Section 1: Purpose & Scope

**Created:** May 12, 2026
**Last Updated:** May 12, 2026
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
  authority). **Status: `IN PROGRESS`.** All citations tagged
  `TBD-NORMATIVE` per KD-2. Section authors MUST grep
  `deterministic-sim/section-1.md`, `section-3.md`, `section-4.md`,
  and `section-5.md` at draft time to verify exact subsection numbers
  — #16 has been through five adversarial passes and subsection
  numbering may shift again.

**Upstream (consulted).**

- Spec #18 (Performance Optimization) §4 / §7 — performance gates.
  **Status: `NOT STARTED`.** All citations tagged `TBD-NORMATIVE` per
  KD-3.
- Spec #20 (Code Standards) §3.9.4 — test-fixture rule carve-outs.
  **Status: `APPROVED` (May 11, 2026).**

**Bidirectional sequencing with #16 (per CLAUDE.md OPEN ISSUES).**

- #16's Tier 2 final approval is gated on `#9 / #17 / #18 / #19`
  reaching `IN REVIEW`.
- #16's `APPROVED` status is a precondition for #19's own `APPROVED`
  status.
- Resolution path: (1) #19 reaches `IN REVIEW` with `TBD-NORMATIVE`
  citations to #16; (2) #16 reaches Tier 2 `APPROVED`; (3) #19's
  `TBD-NORMATIVE` tags are resolved and #19 advances to `APPROVED`.
  `SPEC_INDEX.md` status transitions for #19 MUST follow this order.

**Downstream.**

- Every per-spec §5 (consumes Spec #19 taxonomy and Appendix C schema).
- `src/CLAUDE.md` (consumes test-runner / harness invocation, Stage 1).
- CI configuration files (Stage 1+).

**Cross-spec constants imported.** None. Spec #19 imports tier
*vocabulary* from #16 §1.1.1 by reference (KD-1); no `[CROSS]`
constant declarations.

**Stage 0 host platform pin.** Test execution requires the pins named
in `docs/tracking/certification-platform.md`. Drafting Spec #19 does
not require those pins to be filled in; first CI activation (Stage 0+1
transition) does. Per CLAUDE.md OPEN ISSUES, that file is currently a
placeholder.

## 1.5 Version History

| Version | Date         | Author      | Notes |
|---------|--------------|-------------|-------|
| 0.1     | May 12, 2026 | Claude Code | Initial draft from `outline-detailed.md` v1.1. SPEC_INDEX flip to `IN REVIEW` is **author-driven**, not review-driven: it reflects "draft complete, awaiting lead-developer sign-off" per CLAUDE.md status definition. The §9 approval-checklist rows have not been walked. |
| 0.2     | May 12, 2026 | Claude Code | Self-critique fixes (16 H/M/L findings). #16 section-number sweep: §1.3.1 → §1.1.1 (tier classification), §7 → §5 (regression suite), §5 → §3.2.4.1 (canonical schema), §8 → deleted (no trace channels in current #16). KD-1 / KD-2 / KD-3 narratives tightened; status / sequencing text moved out of §1.3 into §1.4. ERR-005-class fabrication terminology corrected (no ERR-005 binding; ERR-019 namespace used). M3 IFixtureValidator deferral judgment made explicit. M4 storage-layout reconciliation. M5 §6.4.2 Stage-gating. L1–L3 [GT] tags. L4 property-naming reconciliation. L5–L8 cross-reference and policy-placement fixes. |
