# Testing Strategy & Framework Specification #19 — Detailed Outline

**Created:** May 12, 2026
**Last Updated:** May 12, 2026
**Version:** 1.0
**Status:** DRAFT — addresses all 13 findings from `outline.md`
adversarial review (May 6, 2026); ready for section-file authoring.
**Companion documents:** `outline.md` (high-level v1.0 + adversarial
review).

---

## PURPOSE OF THIS DOCUMENT

Expansion of `outline.md` v1.0 into a section-by-section subsection plan
that resolves every finding from the May 6, 2026 adversarial review.
For every subsection: the rules / FRs it will publish, the boundary
declarations it will hold, and the cross-references it will emit.
Detailed enough that `section-1.md` … `section-9-approval-checklist.md`
and `appendices.md` can be drafted directly from this document.

This document does **not** publish FR text in normative form — that text
lands in `section-2.md`. The detailed outline records every FR's
intended rule, conformance level, and source so the FR table can be
authored mechanically.

---

## CROSS-CUTTING DESIGN DECISIONS

These decisions are referenced throughout the outline. They are stated
once here and cited below by KD-number, never restated.

- **KD-1 — Cite-not-redefine.** Spec #19 never restates a CLAUDE.md
  invariant or a rule already published by another approved spec. It
  cites and binds.
- **KD-2 — Boundary with Deterministic Simulation #16.** Spec #16 §7
  ("Determinism Regression Suite") is the **authoritative** owner of
  the determinism test tiers (unit / integration / scenario / soak),
  golden-trace governance, and `EnvironmentFingerprint` gates. Spec #19
  *consumes* that suite as one of its required test layers; it does not
  duplicate or override it. Spec #19 **adds** non-determinism testing
  (functional correctness, property/fuzz, scenario-library tactical
  validation, coverage governance) on top.
- **KD-3 — Boundary with Performance Optimization #18.** Spec #18 owns
  performance regression gates and budget enforcement (#18 §4 / §7).
  Spec #19 owns functional and behavioural regression gates. Both are
  inputs to a single CI orchestration policy declared in Spec #19 §6
  (which cites #18 §4 by reference). Performance numbers are never
  republished by #19.
- **KD-4 — Per-spec §5 sections remain authoritative for their own
  spec.** Spec #19 does **not** rewrite or supersede the §5 test plans
  in approved specs (#1–#8). Spec #19 publishes the *taxonomy*, *naming*,
  *coverage targets*, and *quality-gate criteria* every per-spec §5
  must conform to. New / unapproved specs adopt the taxonomy directly;
  approved specs reconcile at next revision (no forced re-approval
  cycle, per CLAUDE.md authorization rule).
- **KD-5 — Stage-gated activation.** Sections that presume an
  implemented codebase (pyramid ratios, CI gates, coverage thresholds)
  are written as *contracts that activate at the Stage 0 → Stage 1
  transition*. They are first-class normative content of this spec but
  are not enforceable during the spec-writing phase. Activation status
  is tracked per-FR in §5.
- **KD-6 — Programmatic-verification mandate.** Every approval-checklist
  row in every spec MUST resolve to either (a) a named, version-
  controlled file path that contains the value being claimed, or
  (b) a programmatic check (script / test / linter rule) whose output
  is captured in CI logs. This is the project's strongest mitigation
  against ERR-005-class fabrication. Owned by Spec #19 §2 as a named
  FR; see KD-6 references throughout.
- **KD-7 — Determinism-aware fuzz / property testing.** All
  property-based and fuzz-test seeds route through Spec #16
  `DeterministicRngService`. Seed *selection* may be wall-clock at
  test-discovery time, but every executed test body re-runs from the
  recorded seed via `SplitMix64`. Failed seeds are captured to the
  regression suite verbatim; this prevents flake-by-construction.
- **KD-8 — Scenario-library source of truth.** Scenario *definitions*
  live in each owning spec's §5 (per-spec authority). Spec #19 owns the
  *runner*, the *scenario file format*, the *manifest*, and the *index*.
  Cross-spec scenarios (e.g., a full-match smoke test exercising #1–#8
  jointly) are owned by Spec #19 §3 and stored in the scenario library.
- **KD-9 — Per-tier coverage policy.** Coverage targets are bound to
  the determinism tier classification in #16 §1.3.1: Tier A
  authoritative code targets near-100% line + branch coverage; Tier B
  targets ≥90% line, ≥80% branch; Tier C is opportunistic (no
  numeric target, lint-only).
- **KD-10 — Test-data ↔ canonical save format binding.** Golden traces,
  snapshot fixtures, and replay corpora MUST conform to #16 §5
  canonical binary layout. Fixture-format drift from the canonical
  layout is a §5-blocking review finding.

---

## SECTION 1 — PURPOSE & SCOPE (`section-1.md`)

### 1.1 What This Specification Covers

**Subsection target length:** ~40 lines.

**Content:**
- Opening declarative scope statement.
- Bullet list of governance areas (8 items): test taxonomy and pyramid
  contract, deterministic-replay test consumption (from #16),
  scenario-library architecture, property/fuzz testing with seed
  governance, programmatic-verification mandate (KD-6), per-spec §5
  conformance, CI orchestration policy (Stage-gated), coverage and
  flake reporting.
- Applicability block:
  - **Primary:** every test file under `src/<spec>/tests/` once coding
    begins.
  - **Secondary (governance-only):** every spec's §5 section in
    `docs/specs/`. Spec #19 publishes the taxonomy and naming those
    §5 sections must use; it does not rewrite them (KD-4).
- Closing pointer to §3 (taxonomy mechanics) and §5 (quality gates).

### 1.2 What Is Out of Scope

**Subsection target length:** ~30 lines.

One-line entries with the owning document:

- Determinism regression suite mechanics (tiers, golden traces,
  `EnvironmentFingerprint` gates) → Spec #16 §7. Spec #19 *consumes*
  this suite as a required layer; it does not duplicate or override it
  (KD-2).
- Performance regression gates and budget enforcement → Spec #18 §4 /
  §7 (KD-3).
- Numeric correctness of physics/AI formulas → owning specs (#1–#8) §3.
- C# code style and banned-API rules → Spec #20 (Code Standards).
- Fixed64 numeric library tests → Spec #9 §5.
- CI server choice, build commands, IDE configuration →
  `src/CLAUDE.md` (deferred until coding begins).
- Asset-pipeline / content QA → Stage 1+ specs.
- PR-process rules (review approval count, branch-protection,
  required-reviewers) → repository settings.

### 1.3 Key Design Decisions

Full restatement of KD-1 … KD-10 with one-line rationale and the
section that codifies each:

| KD | Topic | Codified in |
|----|-------|-------------|
| KD-1 | Cite-not-redefine | All sections |
| KD-2 | Boundary with #16 §7 | §3.2, §5.1 |
| KD-3 | Boundary with #18 §4 / §7 | §6.2 |
| KD-4 | Per-spec §5 ownership | §3.5, §5.4 |
| KD-5 | Stage-gated activation | §5.2, §7 |
| KD-6 | Programmatic-verification mandate | §2.2 (FR-TS-040..045), §5.3 |
| KD-7 | Determinism-aware fuzz | §3.4, §3.4.3 |
| KD-8 | Scenario-library source of truth | §3.3 |
| KD-9 | Per-tier coverage policy | §3.6, §5.5 |
| KD-10 | Test-data ↔ canonical save format binding | §3.3.4, §4.2 |

### 1.4 Dependencies and Integration Contracts

- **Upstream (substantive):**
  - Root `CLAUDE.md` (project invariants, "When Writing Code" rules).
  - Spec #16 (Deterministic Simulation) §1.3 tier classification, §5
    canonical binary layout, §7 regression suite, §8 trace channels.
- **Upstream (consulted):** Spec #18 §4 / §7 (performance gates);
  Spec #20 (Code Standards) §3.9.4 (test-fixture rule carve-outs).
- **Downstream:**
  - Every per-spec §5 (consumes Spec #19 taxonomy).
  - `src/CLAUDE.md` (consumes test-runner / harness invocation).
  - CI configuration files (Stage 1+).
- **Cross-spec constants imported:** none. Spec #19 imports tier
  *vocabulary* from #16 §1.3 by reference (KD-1 cite-not-redefine);
  no `[CROSS]` constant declarations.
- **Stage 0 host platform pin:** test execution requires the pins
  named in `docs/tracking/certification-platform.md`. Drafting Spec #19
  does not require those pins to be filled in; first CI activation
  (Stage 0+1 transition) does.

### 1.5 Version History

Standard version-history table (initially empty, populated on draft).

---

## SECTION 2 — FUNCTIONAL REQUIREMENTS & TEST GOVERNANCE MODEL (`section-2.md`)

### 2.1 Conformance Levels

- MUST / SHOULD / MAY (RFC 2119 cited).
- "Exception with sign-off" semantics identical to Spec #20 §2.1.

### 2.2 Functional Requirement Catalogue

All FR-TS-### live here with rule statement, conformance level, source
citation, and verification pointer (`§5.x`). Detailed outline names the
partition; section file fills in every numbered FR.

| FR Range | Topic | Rule mechanics in |
|----------|-------|-------------------|
| FR-TS-001 … 010 | Test taxonomy & pyramid contract | §3.1 |
| FR-TS-011 … 020 | Determinism regression consumption (boundary with #16) | §3.2 |
| FR-TS-021 … 030 | Scenario library architecture & runner | §3.3 |
| FR-TS-031 … 039 | Property / fuzz testing with seed governance | §3.4 |
| FR-TS-040 … 045 | Programmatic-verification mandate (KD-6) | §3.5 |
| FR-TS-046 … 052 | Per-spec §5 conformance schema | §3.5, §5.4 |
| FR-TS-053 … 060 | Coverage targets and per-tier policy | §3.6 |
| FR-TS-061 … 067 | Flake handling, quarantine, eviction | §3.7 |
| FR-TS-068 … 074 | Test-data governance | §3.8, Appendix A |
| FR-TS-075 … 080 | CI orchestration (Stage-gated, KD-5) | §6 |
| FR-TS-081 … 085 | Defect lifecycle and triage | §6.4 |

Each FR row: `ID | Statement | Level | Source citation | Verification (§5.x) | Activation stage`.

### 2.3 Failure-to-Comply Modes

- Review block (PR cannot merge — applies once CI activates).
- Quarantine (test moves to flake-quarantine pool with auto-expiry —
  see §3.7).
- Refactor required (merged with follow-up issue).
- Exception with sign-off (recorded; expires at next test-suite
  refactor).
- Spec-§5 nonconformance (per-spec §5 fails Spec #19 schema check at
  draft-review time).

### 2.4 Data Structures (informational)

- Spec #19 defines no runtime data structures used by gameplay.
- Test-harness data structures (scenario manifest, fixture index, flake
  ledger) are declared in §4 and Appendix A; their on-disk encoding is
  governed by KD-10 (canonical save format binding).

### 2.5 Failure Modes

Spec #19's own failure modes (in addition to §2.3):
- Per-spec §5 schema drift — discovered by §5.4 conformance check.
- Fabricated approval-checklist value — caught by KD-6 mandate.
- Fixture-format drift from #16 §5 — caught by §3.3.4 fixture validator.
- Flake threshold breach — caught by §3.7.4 eviction rule.

### 2.6 Version History

---

## SECTION 3 — TECHNICAL SPECIFICATION (rule mechanics) (`section-3.md`)

> Each subsection cites the FR-TS-### IDs it implements (defined in
> §2.2) and provides the *mechanics*. It does not redefine the rule
> statement.

### 3.1 Test Taxonomy & Pyramid Contract (FR-TS-001 … 010)

- 3.1.1 Five-layer taxonomy (with definitions):
  1. **Unit** — single struct/method, no allocation, sub-millisecond.
  2. **Integration** — two-to-five subsystems wired together, no Unity
     scene.
  3. **Simulation** — full subsystem stack invoked under a scripted
     scenario; no rendering.
  4. **Determinism** — owned by #16 §7; consumed by Spec #19 as a
     required layer (KD-2). Listed here for completeness; mechanics not
     restated.
  5. **End-to-end / soak** — long-horizon runs (≥ one full match);
     primarily a determinism + performance vehicle.
- 3.1.2 Pyramid contract (Stage-gated per KD-5; activates at Stage 0+1):
  - Unit ≥ 60% of test count.
  - Integration ≤ 25%.
  - Simulation ≤ 12%.
  - End-to-end / soak ≤ 3%.
  - Determinism layer counted separately (owned by #16); not part of
    the pyramid percentages.
  - Numeric thresholds revisited at Stage 1 first-real-code milestone
    against actual code (parallel to Spec #20 §5.3).
- 3.1.3 Anti-patterns enumerated:
  - Integration test masquerading as unit (allocates, touches > 1
    subsystem).
  - "Simulation" test that asserts on a single physical quantity (should
    be unit).
  - Per-spec §5 declaring layer percentages that contradict the pyramid
    contract.
- 3.1.4 Naming convention:
  - `unit_<system>_<behaviour>` / `int_<systemA>_<systemB>_<behaviour>`
    / `sim_<scenario>` / `e2e_<scenario>`.
  - Determinism tests use the #16 §7 naming (cited, not restated).

### 3.2 Determinism-Suite Consumption (FR-TS-011 … 020)

- 3.2.1 Citation: #16 §7 is the authoritative owner of the determinism
  regression suite. KD-2 binding.
- 3.2.2 Spec #19's obligations toward #16 §7:
  - Every CI pipeline declared in §6 MUST include #16 §7's regression
    tiers in their canonical order (unit / integration / scenario /
    soak).
  - Failures in any #16 tier block merges; Spec #19 does not soften or
    override #16's exit criteria.
  - Spec #19's own test taxonomy MUST NOT collide with #16 tier names
    (§3.1.4 already disambiguates).
- 3.2.3 Spec #19's additions on top of #16 §7:
  - Functional / behavioural regression assertions that don't depend on
    bitwise determinism (e.g., "shot-on-target rate stays within
    designer-tuned envelope across N seeds").
  - Cross-spec scenario assertions (KD-8).
- 3.2.4 Boundary review obligation: any change to #16 §7 that affects
  tier names or exit criteria triggers a Spec #19 §3.2 review (recorded
  in §1.4 dependency list).

### 3.3 Scenario Library Architecture (FR-TS-021 … 030)

- 3.3.1 Source-of-truth rule (KD-8):
  - Per-spec scenarios → defined in owning spec's §5.
  - Cross-spec scenarios → defined in Spec #19 §3 and stored in the
    scenario library.
- 3.3.2 Scenario file format:
  - On-disk layout pointer to Appendix A (manifest schema).
  - Each scenario: name, owning spec ID(s), required RNG seed
    (recorded), expected outcome envelope, tier classification (Tier A
    / B / C per #16 §1.3).
- 3.3.3 Runner contract:
  - Single entry-point: `ScenarioRunner.Run(manifestPath, seed)`.
  - Returns a structured result: pass / fail / quarantined, with
    machine-readable diagnostics.
  - No global state; every run is hermetic.
- 3.3.4 Fixture validator (KD-10):
  - Every fixture file is checked against #16 §5 canonical binary
    layout at load time. Drift fails the test (does not silently
    accept).
- 3.3.5 Scenario library directory layout:
  - `tests/scenarios/<owning-spec>/` for per-spec scenarios.
  - `tests/scenarios/cross-spec/` for KD-8 cross-spec scenarios owned
    by Spec #19.
- 3.3.6 Scenario index / manifest:
  - Single root manifest (`tests/scenarios/index.json` — final
    extension chosen at Stage 0+1) lists every scenario with its
    metadata. Stage 0 deliverable: schema only (Appendix A); Stage 1
    deliverable: populated index.

### 3.4 Property & Fuzz Testing (FR-TS-031 … 039)

- 3.4.1 Framework selection:
  - Property tests use FsCheck (or equivalent C# property-based
    framework) — final pin deferred to Stage 0+1 with the wider tool
    selection (§6.1).
  - Fuzz tests use a structured fuzzing harness; no AFL-style coverage
    fuzzing at Stage 0.
- 3.4.2 Seed governance (KD-7):
  - Property/fuzz seeds may be selected non-deterministically *for the
    selection step only*.
  - The executed test body MUST route through #16
    `DeterministicRngService` (`SplitMix64`) with the selected seed.
  - Selected seed is logged at start of each run.
- 3.4.3 Failed-seed capture:
  - Every failing seed is auto-captured to the determinism regression
    suite (#16 §7) as a new fixed-seed regression test. This converts a
    one-time fuzz hit into a permanent guardrail.
  - Capture format conforms to KD-10 (canonical save format binding).
- 3.4.4 Property catalogue (categorical only — full list in Appendix B):
  - Physics invariants (energy non-increase under collision, conservation
    where applicable).
  - State-machine reachability (no orphaned states).
  - Idempotence (snapshot → load → snapshot = original).
  - Commutativity / associativity claims explicitly tagged Tier B per
    KD-9 (parallel reductions, etc.).
- 3.4.5 Anti-patterns:
  - Property test that uses `System.Random` directly (banned per Spec
    #20 §3.4.2).
  - Property test that asserts on a wall-clock-derived value.
  - Fuzz test that runs without recording its seed.

### 3.5 Programmatic-Verification Mandate (FR-TS-040 … 045) + Per-Spec §5 Conformance (FR-TS-046 … 052)

- 3.5.1 Mandate statement (KD-6):
  - Every approval-checklist row in every spec MUST resolve to either:
    (a) a named, version-controlled file path containing the claimed
    value, or (b) a programmatic check (script / test / linter) whose
    output is captured.
- 3.5.2 Verification mechanics:
  - At spec-review time, a checklist auditor (Stage-0 manual; Stage-1
    automated script) walks every approval-checklist row and resolves
    each citation.
  - Unresolved citation → spec cannot be marked APPROVED (binds to
    `SPEC_INDEX.md` status transitions).
- 3.5.3 Per-spec §5 conformance schema (FR-TS-046 … 052):
  - Every per-spec §5 MUST contain: test count by taxonomy layer, list
    of property tests with property names, list of scenarios with
    manifest paths, coverage target by tier (KD-9), determinism-tier
    classification of every authoritative field referenced, and a
    pointer to the §9 approval checklist row each test verifies.
  - Schema published in Appendix C as a paste-ready template.
- 3.5.4 Migration policy for already-APPROVED specs (KD-4):
  - Approved specs (#1–#8) are not forcibly re-opened. Their §5
    sections are surveyed against the schema; gaps are recorded in
    `docs/tracking/spec-error-log.md` as `ERR-019-NNN` rows; remediation
    happens at next natural revision of each spec.
  - Audit table location: Appendix D.
- 3.5.5 Anti-patterns:
  - Approval-checklist row whose "evidence" is prose without a file
    path or check name (the ERR-005 pattern).
  - Per-spec §5 declaring tests that do not exist in `src/`.
  - Coverage claim without a coverage-report artifact.

### 3.6 Coverage Targets — Per-Tier Policy (FR-TS-053 … 060)

- 3.6.1 Citation: tier vocabulary owned by #16 §1.3.1; not restated.
- 3.6.2 Targets (Stage-gated per KD-5):
  - **Tier A (authoritative hard):** ≥ 98% line, ≥ 95% branch.
  - **Tier B (bounded-authoritative):** ≥ 90% line, ≥ 80% branch.
  - **Tier C (non-authoritative):** lint-only; no numeric target.
  - Test code itself: not counted.
- 3.6.3 Coverage tool selection: deferred to Stage 0+1 (§6.1).
- 3.6.4 Reporting cadence:
  - Per-PR delta only at Stage 0+1.
  - Absolute per-tier dashboard at Stage 1.
- 3.6.5 Coverage exemption procedure:
  - Lead-developer sign-off required.
  - Recorded in `tests/coverage-exemptions.md` (Stage-1 artifact);
    expires at next refactor of the affected file.

### 3.7 Flake Handling (FR-TS-061 … 067)

- 3.7.1 Definition:
  - A test is "flaky" if two runs of the same revision under the same
    `EnvironmentFingerprint` produce different pass/fail outcomes.
  - This is a determinism-adjacent definition; cited from #16 §1.3.
- 3.7.2 Detection:
  - CI runs every test twice on the same revision (Stage 1 deliverable,
    deferred at Stage 0).
  - Disagreement between runs → automatic quarantine.
- 3.7.3 Quarantine pool:
  - Quarantined tests still execute but do not block merges.
  - Auto-expiry: 14 days. After expiry, the test must be either fixed
    or deleted; "permanent quarantine" is forbidden.
- 3.7.4 Eviction rule:
  - A test that has been quarantined ≥ 3 times in 90 days is deleted
    and recorded in `tests/flake-eviction-log.md` with rationale.
  - Re-introduction requires a new test ID and a written root-cause
    analysis.
- 3.7.5 Anti-patterns:
  - "Flaky in CI only" (root cause is invariably an
    `EnvironmentFingerprint` violation; investigate via #16 §4.8).
  - Adding `[Retry]` attributes to mask flake.
  - Sleep-based synchronization in tests.

### 3.8 Test-Data Governance (FR-TS-068 … 074)

- 3.8.1 Citation: KD-10 (binding to #16 §5 canonical save format).
- 3.8.2 Storage layout:
  - `tests/data/fixtures/` — small, in-repo fixtures.
  - `tests/data/golden/` — golden outputs for replay assertions.
  - `tests/data/corpora/` — fuzz corpora (LFS-tracked).
  - Concrete LFS / no-LFS decision recorded at Stage 0+1.
- 3.8.3 Versioning:
  - Each fixture has a `format-version` field.
  - Validator rejects unknown versions (no silent migration).
- 3.8.4 Provenance:
  - Every captured fixture records: source seed, capturing-spec ID,
    capture date, `EnvironmentFingerprint` at capture time.
- 3.8.5 Eviction:
  - Fixtures whose owning test is deleted are also deleted in the same
    commit (no orphan fixtures).
- 3.8.6 Full mechanics → Appendix A.

### 3.9 Edge Cases (rule-application carve-outs)

- 3.9.1 Editor-only / debug-tool tests: SHOULD conform to taxonomy and
  naming; MAY relax §3.6 coverage targets; MUST conform to §3.4 seed
  governance if exercising RNG.
- 3.9.2 Benchmark / micro-perf scaffolds: outside Spec #19 (owned by
  Spec #18); pointer only.
- 3.9.3 Visual-regression tests (UI screenshots): Tier C only;
  diagnostic, never gate.
- 3.9.4 Stage-0 spec-only tests (e.g., constants-catalogue verification
  scripts): treated as "tooling tests"; conform to KD-6 mandate but not
  to §3.1 pyramid contract (which presumes runtime code).

### 3.10 Constants Catalogue (governance metadata only)

- This spec declares **no physical constants**. Numeric thresholds it
  publishes (pyramid percentages, coverage targets, flake-eviction
  windows) are governance values tagged `[GT]` with rationale recorded
  inline. Section retained per template with one-line justification.

### 3.11 Version History

---

## SECTION 4 — ARCHITECTURE & INTEGRATION (`section-4.md`)

### 4.1 `tests/` Folder Layout (shape, not concrete paths)

- Convention: one `tests/` folder per Stage 0 spec (sibling of
  `src/<spec>/`), matching Spec #20 §4.1 dependency-arrow shape.
- Within each `tests/<spec>/`: `unit/`, `integration/`, `simulation/`,
  `properties/` subfolders.
- Cross-spec scenarios live at `tests/scenarios/cross-spec/` (KD-8).
- Shared harness code at `tests/shared/` (read-only utilities; NOT
  game-state assemblies).

### 4.2 Fixture & Golden-Trace Layout

- `tests/data/` root with `fixtures/`, `golden/`, `corpora/`
  subfolders (per §3.8.2).
- Format conforms to #16 §5 (KD-10).
- Index / manifest schema in Appendix A.

### 4.3 Harness API Surface

- `ITestHarness` consumed by per-spec test runners. Single concrete
  implementation; no IoC container in test code (parallel to Spec #20
  §3.5.5 anti-pattern list).
- Assertion helpers: `AssertBitwise(snapshot, golden)` for Tier A;
  `AssertWithinTolerance(actual, expected, toleranceRow)` for Tier B
  with the tolerance row sourced from #16's tolerance matrix.
- Scenario runner contract per §3.3.3.

### 4.4 Interface Contracts (this spec exposes)

- `IScenario` — implemented by every scenario; single `Run(seed)`
  method.
- `IFixtureValidator` — implemented per fixture format-version.
- `IFlakeReporter` — implemented by the CI integration layer
  (Stage 1+).
- All three live in `tests/shared/` per §4.1; no game-state code may
  reference them.

### 4.5 CI Pipeline Topology (shape only; concrete config Stage 1+)

- Pre-commit pipeline: unit + property (fast).
- PR pipeline: unit + integration + property + per-spec-changed
  scenarios.
- Nightly pipeline: full simulation tier + soak + #16 §7 determinism
  full suite.
- Diagram: trigger → tier → exit criteria. Concrete CI provider
  selection deferred to Stage 0+1 (§6.1).

### 4.6 Pointer to `src/CLAUDE.md`

- Concrete paths, runner invocations, and CI provider configuration
  land in `src/CLAUDE.md` when coding begins. Spec #19 declares the
  *shape*; `src/CLAUDE.md` declares the *paths*.

### 4.7 Version History

---

## SECTION 5 — TEST PLAN (CONFORMANCE VERIFICATION OF THIS SPEC ITSELF) (`section-5.md`)

> **Slot reconciliation:** The template's §5 ("Test Plan") is reflexive
> for a meta-spec: this section verifies Spec #19 against itself. Per-spec
> §5 conformance verification (which Spec #19 mandates for *other*
> specs) is mechanics-defined in §3.5 above; auditor mechanics live
> here in §5.4.

### 5.1 Conformance Verification Model

- Spec #19 publishes its FRs (§2.2). This section maps every FR to its
  verification mechanism.
- Stage 0: manual review (no code yet, parallel to Spec #20 §5.1).
- Stage 0+1: tooling activates per FR's "Activation stage" column in
  §2.2.

### 5.2 Stage-Gated Activation Table (KD-5)

- Per-FR table: `FR-TS-### | Stage 0 status | Activation stage | Activation criterion`.
- Most FRs read "Stage 0+1" with criterion "first `src/` code
  committed".
- A few read "Stage 0" with criterion "applies to spec drafts now"
  (notably KD-6 mandate FRs and per-spec §5 schema FRs).

### 5.3 Approval-Checklist Auditor (KD-6 mechanics)

- Manual at Stage 0: checklist auditor (a reviewer) walks every
  approval-checklist row and resolves each citation against the
  current repo state.
- Automated at Stage 0+1: `tools/checklist-auditor.py` (or equivalent
  — final language pin parallel to Python tooling rule in CLAUDE.md
  "When Writing Code") parses checklist tables, resolves cited file
  paths, and flags unresolved rows.
- Output format declared in Appendix C.

### 5.4 Per-Spec §5 Schema-Conformance Auditor

- Mechanics for FR-TS-046 … 052.
- Schema check walks every spec's §5 against the Appendix C template.
- Approved specs (#1–#8): survey-only at Stage 0; gaps logged as
  `ERR-019-NNN` per §3.5.4.
- New specs from this point forward: schema-conforming on first draft
  or §9 approval is blocked.

### 5.5 Coverage-Report Auditor (KD-9)

- Mechanics for FR-TS-053 … 060.
- Stage 0: not applicable (no code).
- Stage 0+1: coverage tool produces per-file report; auditor maps each
  file to its #16 §1.3 tier and applies KD-9 thresholds.
- Exemption handling per §3.6.5.

### 5.6 FR-to-Verification Traceability

- Single table indexed by FR-TS-###; columns: `Verification Mechanism |
  Tooling | Activation Stage | Output Artifact`.
- Stage 0 most rows resolve to "manual review against §3 mechanics" —
  acknowledged degenerate (parallel to Spec #20 §5.5 acknowledgement).

### 5.7 Determinism-Suite Consumption Verification

- Spec #19 declares no numerical determinism tests of its own.
- This subsection records the *consumption* contract: every CI pipeline
  runs #16 §7's full tier set; failures block per KD-2.
- Boundary review check: any change to #16 §7 that touches tier names
  or exit criteria triggers a Spec #19 §3.2 review.

### 5.8 Version History

---

## SECTION 6 — CI ORCHESTRATION & TRIAGE (`section-6.md`)

> **Slot reconciliation:** Replaces the template's "Performance
> Analysis" slot. A meta-spec has no algorithm to analyse; it codifies
> the CI orchestration policy and defect-lifecycle rules. Justification
> in §1.3 KD-3 (boundary with Spec #18) and KD-5 (Stage gating).

### 6.1 Tooling Standards (Stage-gated per KD-5)

- Stage 0: no tooling activates. This subsection enumerates *selection
  criteria*, not chosen tools.
- Stage 0+1 tool slate (selection finalized at transition):
  - Test runner: NUnit or xUnit (final pin parallel to Spec #20 §5.2
    Roslyn analyzer pin).
  - Property framework: FsCheck or equivalent.
  - Coverage: Coverlet (selection criterion: must emit per-tier
    breakdown consumable by §5.5 auditor).
  - Mutation testing: Stryker.NET (selection criterion: parallels
    coverage tool; deferred to Stage 1 for first activation).
  - CI provider: deferred to `src/CLAUDE.md` (KD-3 boundary with #18
    leaves provider choice to perf side, since perf gates are the
    longest CI step).

### 6.2 CI Pipeline Policy (boundary with #18)

- Spec #19 declares functional regression gates (test pass/fail).
- Spec #18 §4 declares performance regression gates (budget threshold).
- Both feed a single CI orchestrator; gate composition rule:
  - Functional gate failure → block merge (Spec #19 authority).
  - Performance gate failure → block merge (Spec #18 authority).
  - Determinism gate failure → block merge (#16 §7 authority).
  - No gate is "soft"; flake quarantine (§3.7) is the only escape
    valve and applies only to functional gates.
- KD-3 binding: Spec #19 cites #18 §4 thresholds by reference; never
  republishes them.

### 6.3 Stage-0 Local-Only Runbook

- Until CI activates, the same gate composition runs locally:
  - Pre-commit hook script (Stage 0 deliverable; Appendix E):
    `tools/run-tests-local.sh` invokes the manual checklist auditors
    of §5.3 / §5.4 against `docs/specs/` only (no `src/` yet).
- Output of local runbook → reviewer pastes into PR description.

### 6.4 Defect Lifecycle & Triage (FR-TS-081 … 085)

- 6.4.1 Defect classes:
  - **Spec defect** (rule wrong / contradictory) → fix in spec; recorded
    in `spec-error-log.md` as `ERR-NNN-NNN`.
  - **Implementation defect** (code violates approved spec) → fix in
    code; tracked in issue tracker.
  - **Test defect** (test wrong) → fix test; recorded in
    `tests/test-defect-log.md`.
  - **Determinism defect** → routed to #16 §7 process (KD-2).
- 6.4.2 Triage cadence:
  - PR-blocking failures: investigated within 24 hours.
  - Quarantined tests: reviewed weekly.
  - Spec defects: reviewed at next spec-revision cycle.
- 6.4.3 Severity scale:
  - **Critical** — blocks Stage milestone.
  - **High** — blocks current sprint.
  - **Medium** — backlogged with date target.
  - **Low** — backlog, no date.
- 6.4.4 Defect-to-FR traceability:
  - Every defect cites the FR it violated (Spec #19 FR or owning-spec
    FR). Defects without FR citation are themselves a procedural
    violation (parallel to KD-6 mandate).

### 6.5 Reporting Cadence

- Stage 0: monthly survey of `spec-error-log.md` + checklist-auditor
  output appended to `docs/tracking/PROGRESS.md`.
- Stage 0+1: per-PR delta + weekly dashboard.
- Stage 1: per-PR delta + nightly dashboard + monthly retrospective.

### 6.6 Performance-Related Cross-Listing

- FR-TS-075 … FR-TS-080 (CI orchestration) cite #18 §4 / §7 by
  reference per KD-3. No performance numbers republished here.

### 6.7 Version History

---

## SECTION 7 — FUTURE EXTENSIONS (`section-7.md`)

### 7.1 Stage 0+1 Transition Deliverables

- Test runner pin (§6.1).
- Property framework pin (§6.1).
- Coverage tool pin (§6.1).
- First `src/CLAUDE.md` test-runner section.
- `tools/checklist-auditor.py` (§5.3) initial implementation.
- Pre-commit hook script (§6.3).
- Pyramid-ratio thresholds (§3.1.2) re-evaluated against actual code.

### 7.2 Stage 1 Deliverables

- Coverage dashboard (§3.6.4).
- Flake quarantine + eviction tooling (§3.7).
- Mutation-testing first activation (§6.1).
- Scenario library populated index (§3.3.6).
- Per-spec §5 schema-conformance auto-check (§5.4).

### 7.3 Stage 5+ Extensions

- Cross-platform bit-exact parity test layer activates (parallel to
  Spec #20 §7.3).
- Fixed64-aware property tests (Spec #9 dependency).
- Multiplayer determinism-cert layer.

### 7.4 Permanent Exclusions

- Test framework debates this spec refuses to relitigate (chosen at
  Stage 0+1 once and not revisited absent vendor abandonment).
- "Flake suppression by retry attribute" — never permitted (§3.7.5
  anti-pattern).
- Per-spec §5 sections will not be forcibly rewritten by Spec #19 (KD-4
  permanent rule).

### 7.5 Deferred Decisions Tracker

- D1 — Test runner pin (NUnit vs xUnit) — Stage 0+1.
- D2 — Property framework pin — Stage 0+1.
- D3 — Coverage tool pin — Stage 0+1.
- D4 — CI provider — `src/CLAUDE.md` (KD-3).
- D5 — LFS storage decision for fixtures (§3.8.2) — Stage 0+1.
- D6 — Mutation-testing activation date — Stage 1.
- D7 — Visual-regression framework selection — Stage 1+ (§3.9.3).

### 7.6 Version History

---

## SECTION 8 — REFERENCES & CITATION AUDIT (`section-8.md`)

### 8.1 Source Register

- Root `CLAUDE.md` (project invariants; "When Writing Code" rules).
- Spec #16 (Deterministic Simulation) — §1.3 tier classification, §5
  canonical save format, §7 regression suite, §8 trace channels.
- Spec #18 (Performance Optimization) — §4 / §7 regression gates.
- Spec #20 (Code Standards) — §3.9.4 test-fixture carve-outs.
- `docs/planning/development-best-practices.md`.
- `docs/planning/master-development-plan.md`.
- `docs/tracking/certification-platform.md` (placeholder at draft time;
  see open-issue note in §1.4).
- RFC 2119 (MUST/SHOULD/MAY).
- External: NUnit / xUnit / FsCheck / Coverlet / Stryker.NET URLs +
  retrieval dates (Stage 0+1 deliverable; placeholders at draft).

### 8.2 Verification Notes

- Every CLAUDE.md citation in §3 verified against current CLAUDE.md
  text on this spec's drafting date.
- Every Spec #16 / #18 / #20 citation verified against the current
  approved-or-draft text and section number per `SPEC_INDEX.md`.

### 8.3 Cross-Spec Citation Audit

- Spec #19 is **cited by** every per-spec §5 (downstream).
- Spec #19 cites #16 (substantive: tiers, regression suite,
  fingerprint, save format), #18 (boundary: perf gates), #20 (boundary:
  test-fixture carve-outs).
- No `[CROSS]` constants are imported (Spec #19 declares none).
- Tier vocabulary cited from #16 §1.3.1 by reference only (KD-1).

### 8.4 Constant Provenance Summary

- Spec #19 declares no physical constants.
- Governance numerics (pyramid %, coverage %, flake-eviction window)
  are `[GT]` per §3.10; rationale recorded inline at point of
  declaration.

### 8.5 Version History

---

## SECTION 9 — APPROVAL CHECKLIST (`section-9-approval-checklist.md`)

> Spec #19's own §9 is the **first** application of KD-6 to itself
> (the auditor must not exempt Spec #19 from its own mandate).

### 9.1 Content Checklist

- All required sections present (incl. template-slot reconciliation in
  §5 / §6).
- All FR-TS-### present in §2.2 with conformance level and activation
  stage.
- KD-1 … KD-10 each codified in at least one §3 / §5 / §6 subsection.
- Boundary statements with #16 §7 (KD-2) and #18 §4 / §7 (KD-3) explicit.

### 9.2 Quality Checklist

- Cite-not-redefine rule audited (no #16 / #18 / #20 restatements).
- Every FR row resolves to a §5.x verification mechanism.
- Every approval-checklist row in *this* checklist cites either a file
  path or a check name (KD-6 self-application).
- All cross-references (XC-/FM-/EC-/ERR-) resolve.
- Per-spec §5 schema (Appendix C) present and complete.
- Survey of approved specs #1–#8 §5 sections completed; gaps logged
  as `ERR-019-NNN` per §3.5.4.

### 9.3 Review Checklist

- Open issues logged in `CLAUDE.md` "OPEN ISSUES" if any.
- Lead-developer sign-off captured.
- `spec-error-log.md` updated with any cross-spec drift discovered
  during drafting.
- `SPEC_INDEX.md` status updated atomically with sign-off.

### 9.4 Decision

- Status block (`IN REVIEW` / `APPROVED` / `SUSPENDED` / `DEFERRED`).
- Approval evidence: file paths to programmatically-verifiable sources
  (KD-6 self-application — every row of this checklist must comply).

---

## APPENDICES (`appendices.md`)

- **Appendix A — Scenario / Fixture Manifest Schema.**
  Paste-ready JSON-schema-style declaration; field names, types,
  required / optional, format-version semantics; binding to #16 §5
  canonical layout (KD-10).

- **Appendix B — Property-Test Catalogue.**
  Full enumeration of property categories named in §3.4.4 with one
  exemplar property per category. Per-property: name, owning spec,
  tier classification (Tier A / B / C per #16 §1.3), expected
  invariant.

- **Appendix C — Per-Spec §5 Schema Template.**
  Paste-ready Markdown template every per-spec §5 must conform to.
  Sections: test-count-by-layer table, property-test list, scenario
  list with manifest paths, coverage targets by tier, determinism-tier
  classification, approval-checklist linkage. This is the artifact
  KD-6 + KD-4 mandate.

- **Appendix D — Approved-Spec §5 Survey.**
  Table of #1 … #8 §5 sections rated against Appendix C schema.
  Columns: spec ID, schema-conforming Y/N, missing fields, remediation
  ERR-019-NNN. Stage 0 deliverable; Stage 1 trigger for actual
  per-spec revisions.

- **Appendix E — Stage-0 Local Runbook.**
  Concrete shell-script outline for `tools/run-tests-local.sh`: pre-
  commit checks against `docs/specs/` only (no `src/` yet); invocation
  of §5.3 checklist auditor and §5.4 schema-conformance auditor.

- **Appendix F — Glossary.**
  Spec #19-specific terms only (taxonomy layer names, flake, quarantine,
  eviction, scenario, fixture, golden trace). Determinism / performance
  terms cited from #16 / #18.

---

## VERSION HISTORY

| Version | Date         | Author      | Notes                                                                                                         |
|---------|--------------|-------------|---------------------------------------------------------------------------------------------------------------|
| 1.0     | May 12, 2026 | Claude Code | Initial detailed outline drafted from `outline.md` v1.0. Addresses all 13 findings from May 6 adversarial review. |

---

## ADVERSARIAL-REVIEW FINDINGS RESOLUTION MAP

For traceability — every finding in `outline.md` adversarial review
section is resolved by a specific subsection above.

| Finding | Severity | Resolved by |
|---------|----------|-------------|
| 1 — Missing metadata header | H | Top of this file |
| 2 — Section plan deviates from template | H | §5 / §6 / §7 / §8 re-mapped (slot reconciliations stated in §5 and §6 headers) |
| 3 — Boundary with #16 §7 unresolved | H | KD-2; §3.2; §5.7; §6.2 |
| 4 — Boundary with #18 §4 / §7 unresolved | H | KD-3; §6.2; §6.6 |
| 5 — Pyramid ratios meaningless without code | H | KD-5; §3.1.2; §5.2 stage-gating table |
| 6 — Programmatic-verification mandate missing | H | KD-6; §2.2 FR-TS-040..045; §3.5; §5.3 |
| 7 — Per-spec §5 ownership unstated | M | KD-4; §3.5.3 / §3.5.4; §5.4; Appendix C / D |
| 8 — Fuzz seed governance | M | KD-7; §3.4.2 / §3.4.3 |
| 9 — Scenario library source of truth | M | KD-8; §3.3.1; §3.3.5 |
| 10 — CI orchestration infeasible at Stage 0 | M | KD-5; §6.1; §6.3 (Stage-0 local runbook); Appendix E |
| 11 — No coverage target distinction by tier | M | KD-9; §3.6.2; §5.5 |
| 12 — Flaky-test policy in appendix only | L | §3.7 (promoted to §3, not appendix); also referenced from §2.5 |
| 13 — Test data ↔ canonical save format binding | L | KD-10; §3.3.4; §3.8; Appendix A |
