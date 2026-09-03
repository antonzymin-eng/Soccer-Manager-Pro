# Testing Strategy & Framework Specification #19 — Detailed Outline

**Created:** May 12, 2026
**Last Updated:** September 3, 2026
**Version:** 1.3
**Status:** HISTORICAL DRAFTING ARTIFACT — A3.2b synchronized; authoritative rules are the section files, whose May 15 approved baseline remains operative pending A3.4 reapproval
**Amendment plan:** `docs/planning/project-architecture-governance-integration-plan.md` v0.37, §7; A3.2b
**Companion documents:** `outline.md` (high-level v1.0 + adversarial
review).

---

## PURPOSE OF THIS DOCUMENT

> **A3.2b synchronization boundary (September 3, 2026).** Dated May
> 2026 status-caveat/TBD instructions below are retained as drafting
> history where explicitly date-labelled. Current authority/status is
> the section-file amendment set; this outline does not reintroduce a
> retired `TBD-NORMATIVE` gate or supersede the approved section files.

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
- **KD-2 — Boundary with Deterministic Simulation #16.** Spec #16 §5
  ("Determinism Regression Suite") is the **authoritative** owner of
  the determinism test tiers (unit / integration / scenario / soak),
  golden-trace governance, and `EnvironmentFingerprint` gates. Spec #19
  *consumes* that suite as one of its required test layers; it does not
  duplicate or override it. Spec #19 **adds** non-determinism testing
  (functional correctness, property/fuzz, scenario-library tactical
  validation, coverage governance) on top.
  - **Status caveat (May 12, 2026).** Per `SPEC_INDEX.md`, Spec #16 is
    `IN PROGRESS`, not `APPROVED`. All §3.2, §5.7, §6.2, §3.4.3, §3.6,
    §3.8 citations of "#16 §1.1.1", "#16 §5", "#16 §5", "#16 §8" are
    tagged `TBD-NORMATIVE` (pattern adopted from #16 §8.3.1 per
    CLAUDE.md OPEN ISSUES) until #16 reaches `APPROVED`. Section files
    MUST carry the tag verbatim on every #16 citation; tag removal is a
    §9.2 quality-checklist row and is gated on #16 approval.
  - **Sequencing constraint (H2).** Per CLAUDE.md OPEN ISSUES, #16's
    Tier 2 final approval is gated on `#9 / #17 / #18 / #19 reaching
    IN REVIEW`. Spec #19 in turn binds substantively to #16 §5. The
    resolution path is: (1) #19 reaches `IN REVIEW` with `TBD-NORMATIVE`
    citations to #16; (2) #16 reaches Tier 2 `APPROVED`; (3) #19's
    `TBD-NORMATIVE` tags are resolved and #19 advances to `APPROVED`.
    `SPEC_INDEX.md` status transitions for #19 MUST follow this order.
- **KD-3 — Boundary with Performance Optimization #18.** Spec #18 owns
  performance regression gates and budget enforcement (#18 §4 / §7).
  Spec #19 owns functional and behavioural regression gates. Both are
  inputs to a single CI orchestration policy declared in Spec #19 §6
  (which cites #18 §4 by reference). Performance numbers are never
  republished by #19.
  - **Status caveat (May 12, 2026).** Per `SPEC_INDEX.md`, Spec #18 is
    `NOT STARTED`. No #18 text exists to cite yet. Every #18 reference
    in §6.2 and §6.6 is tagged `TBD-NORMATIVE` with a placeholder
    citation; the placeholder names the section that #18 is expected
    to expose ("#18 §4 performance regression gates"). #19 cannot
    advance past `IN REVIEW` until #18 has at least an outline-level
    draft in `docs/specs/performance-optimization/` that confirms the
    cited section numbers. This precondition is a §9.3 review-checklist
    row.
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
  the determinism tier classification in #16 §1.1.1: Tier A
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
  `EnvironmentFingerprint` gates) → Spec #16 §5. Spec #19 *consumes*
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
| KD-2 | Boundary with #16 §5 | §3.2, §5.1 |
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
    **Status:** `IN PROGRESS`. All citations tagged `TBD-NORMATIVE`
    until #16 approval (see KD-2 status caveat). Section authors MUST
    verify exact subsection numbers against the current
    `deterministic-sim/section-1.md` / `section-5.md` / `section-7.md`
    at draft time — #16 has been through three adversarial passes and
    subsection numbering may have shifted since this outline was
    written.
- **Upstream (consulted):** Spec #18 §4 / §7 (performance gates) —
  **status `NOT STARTED`**, citations tagged `TBD-NORMATIVE` per KD-3
  status caveat; Spec #20 (Code Standards) §3.9.4 (test-fixture rule
  carve-outs).
- **Bidirectional sequencing with #16 (H2):** #19's `IN REVIEW` status
  is a precondition for #16's Tier 2 `APPROVED` (per CLAUDE.md OPEN
  ISSUES); #16's `APPROVED` status is a precondition for #19's own
  `APPROVED`. See KD-2 sequencing constraint.
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
| FR-TS-086 … 097 | Architecture proof/evidence integration | §3.11, §5.6, §6.2, Appendix G |

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
  4. **Determinism** — owned by #16 §5; consumed by Spec #19 as a
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
  - **Bound semantics (L1).** These are *ceilings* on integration /
    simulation / e2e and a *floor* on unit. A suite that is 100% unit
    and 0% else satisfies the arithmetic; that outcome is intentional —
    the pyramid contract guards against top-heavy suites, not against
    bottom-heavy ones. Per-spec §5 sections MAY declare tighter lower
    bounds if subsystem maturity warrants. Authors who want a balanced
    distribution should set those lower bounds locally, not via this
    spec.
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
  - Determinism tests use the #16 §5 naming (cited, not restated).

### 3.2 Determinism-Suite Consumption (FR-TS-011 … 020)

- 3.2.1 Citation: #16 §5 is the authoritative owner of the determinism
  regression suite. KD-2 binding.
- 3.2.2 Spec #19's obligations toward #16 §5:
  - Every CI pipeline declared in §6 MUST include #16 §5's regression
    tiers in their canonical order (unit / integration / scenario /
    soak).
  - Failures in any #16 tier block merges; Spec #19 does not soften or
    override #16's exit criteria.
  - Spec #19's own test taxonomy MUST NOT collide with #16 tier names
    (§3.1.4 already disambiguates).
- 3.2.3 Spec #19's additions on top of #16 §5:
  - Functional / behavioural regression assertions that don't depend on
    bitwise determinism (e.g., "shot-on-target rate stays within
    designer-tuned envelope across N seeds").
  - Cross-spec scenario assertions (KD-8).
- 3.2.4 Boundary review obligation: any change to #16 §5 that affects
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
  - Every fixture file is checked against #16 §3.2.4.1 canonical
    byte-level schema at load time. Drift fails the test (does not silently
    accept).
- 3.3.5 Scenario library directory layout:
  - `tests/scenarios/<owning-spec>/` for per-spec scenarios.
  - `tests/scenarios/cross-spec/` for KD-8 cross-spec scenarios owned
    by Spec #19.
- 3.3.6 Scenario index / manifest:
  - Single root manifest (`tests/scenarios/index.<ext>` — final
    extension chosen at Stage 0+1 under D9) lists every scenario with its
    metadata. Stage 0 deliverable: schema only (Appendix A); Stage 1
    deliverable: populated index.

### 3.4 Property & Fuzz Testing (FR-TS-031 … 039)

- 3.4.1 Framework selection:
  - Property tests use FsCheck (or equivalent C# property-based
    framework) — final pin deferred to Stage 0+1 with the wider tool
    selection (§6.1).
  - Fuzz tests use a structured fuzzing harness. Coverage-guided fuzzing
    (AFL-style) is a Stage 1+ posture decision tracked in §7.5
    deferred-decisions list; not adopted by default.
- 3.4.2 Seed governance (KD-7):
  - Property/fuzz seeds may be selected non-deterministically *for the
    selection step only*.
  - The executed test body MUST route through #16
    `DeterministicRngService` (`SplitMix64`) with the selected seed.
  - Selected seed is logged at start of each run.
- 3.4.3 Failed-seed capture (M1 — read-only boundary with #16 §5):
  - Spec #19 does **not** write directly into the Spec #16 §5
    regression suite. Per KD-2, #19 consumes #16 §5 read-only; #16 §5
    is the sole authority for what enters its regression corpus.
  - Mechanics: every failing fuzz / property seed is captured into a
    Spec #19-owned holding area at
    `tests/data/captured-seeds/<spec>/<YYYY-MM-DD>-<seed>.fixture`
    (final path pinned at Stage 0+1). Capture format conforms to
    KD-10 (canonical byte-level schema binding from #16 §3.2.4.1).
  - Promotion path: #16 §5 SHOULD publish an "external capture hook"
    contract that periodically (cadence TBD by #16) pulls from #19's
    holding area into the #16 §5 regression corpus. Until that hook is
    published in #16 §5, captured seeds remain in the #19 holding area
    and are re-run by #19's own property/fuzz suite on every CI run —
    a one-time fuzz hit still becomes a permanent #19-side guardrail.
  - Cross-spec dependency: this subsection's "promotion path" is
    `TBD-NORMATIVE` per KD-2 status caveat; resolved when #16 §5
    publishes its external-capture-hook contract.
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
- 3.5.4 Migration policy for already-APPROVED specs (KD-4) and
  acknowledged KD-6 dilution (M2):
  - Approved specs (#1–#8) are not forcibly re-opened. Their §5
    sections are surveyed against the schema; gaps are recorded in
    `docs/tracking/spec-error-log.md` as `ERR-019-NNN` rows; remediation
    happens at next natural revision of each spec.
  - **Acknowledged dilution.** This migration policy is in tension with
    KD-6 ("every approval-checklist row in every spec MUST resolve to a
    file path or programmatic check"). Specs #1–#8 were approved before
    KD-6 existed, and KD-4 explicitly forbids re-opening them. Net
    effect: KD-6 is **unenforced retroactively** for the eight specs
    where ERR-005-class fabrication is statistically most likely to
    already exist. This is a *known dilution*, not a migration
    technicality. Mitigation: the Appendix D survey enumerates every
    unresolved row as a `ERR-019-NNN` entry so the dilution is
    *visible* even when not *enforced*. Full enforcement reaches each
    of #1–#8 only at that spec's next natural revision.
  - Audit table location: Appendix D.
- 3.5.5 Anti-patterns:
  - Approval-checklist row whose "evidence" is prose without a file
    path or check name (the ERR-005 pattern).
  - Per-spec §5 declaring tests that do not exist in `src/`.
  - Coverage claim without a coverage-report artifact.

### 3.6 Coverage Targets — Per-Tier Policy (FR-TS-053 … 060)

- 3.6.1 Citation: tier vocabulary owned by #16 §1.1.1; not restated.
  - **Cite-precision guard (L2).** The subsection number "§1.3.1" is
    tagged `TBD-NORMATIVE` per KD-2 because #16 has been through three
    adversarial passes and subsection numbering may have shifted.
    Section §3 author MUST grep `deterministic-sim/section-1.md` for
    the tier classification block at draft time and update the cited
    number atomically. Same guard applies to every "§5", "§7", "§8"
    citation of #16 in this spec.
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

> **Stage-gated per KD-5 (M4).** Every rule in §3.7 is a contract that
> activates at the Stage 0 → Stage 1 transition. Until CI exists, there
> is nothing to flake. The §3.7.3 "14-day auto-expiry", §3.7.4 "≥3
> quarantines in 90 days = eviction", and §3.7.2 "CI runs every test
> twice" rules all presume the CI integration layer enumerated in §7.2.
> Per-FR activation status recorded in §5.2 Stage-Gated Activation
> Table; the corresponding FR-TS-061 … 067 rows read "Activation stage:
> Stage 0+1, criterion: CI integration layer specified per §7.2."

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

- 3.8.1 Citation: KD-10 (binding to #16 §3.2.4.1 canonical byte-level schema).
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
- **KD-6 evidence artifact for governance numbers (L5).** Each `[GT]`
  governance number's KD-6 evidence is the *citation line in this
  spec's body text* that introduces the number — for example, the
  pyramid percentages' evidence is `section-3.md §3.1.2`, the Tier A
  coverage thresholds' evidence is `section-3.md §3.6.2`, the
  flake-eviction window's evidence is `section-3.md §3.7.4`. The
  approval-checklist auditor (§5.3) resolves these citations by
  confirming the cited file path contains the literal number claimed.
  No separate `tools/governance-numbers.md` file is created; the spec
  body IS the evidence, and changes to a governance number are
  themselves a spec revision tracked in the relevant section's
  version-history table.

### 3.11 Architecture Proof & Evidence Integration (FR-TS-086 … 097)

- Governance owns property admission, applicability authority,
  property exceptions, finding disposition/status, and convergence.
- Spec #20 owns runtime-surface identity, integration/lifecycle/host
  declarations, activation state, and disable anchors.
- Spec #19 owns proof artifacts, mechanically derived closure,
  execution truth, bounded substitutes, failure-injection/mutation
  evidence, freshness/revalidation, and architecture/evidence-gate
  consumption.
- Proof classes are structural-reachability, lifecycle-order,
  failure-injection, and mutation. Persistence relations join the
  mechanically derived closure only for a current
  `persistence-boundary` or `external-resource-dependency`
  change type.
- `passed` satisfies directly. `failed`, `skipped`, and
  `runner-failed` cannot be converted by bounded substitution.
  Deliberate `excluded`, `unavailable`, or `not-run` states are
  bounded-eligible only where the exact obligation permits FR-TS-096.
- Subject identity is material proof scope; Git revision/tree is
  provenance only. Relevant closure changes stale proof; unrelated
  out-of-closure changes do not.
- Targeted governance mutation is independent of deferred project-wide
  mutation tooling.
- A3.2b does not activate enforcement; A3.4/A4/A8 prerequisites remain.

### 3.12 Version History

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
- Format conforms to #16 §3.2.4.1 (KD-10).
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
  method. Producer = scenario authors; consumer = `ScenarioRunner`
  (§3.3.3). Both sides specified in this spec → permitted under
  CLAUDE.md "Interface Design Principle".
- `IFixtureValidator` — implemented per fixture format-version.
  Producer = fixture-format owners; consumer = `ScenarioRunner` fixture
  load step (§3.3.4). Both sides specified.
- Both live in `tests/shared/` per §4.1; no game-state code may
  reference them.
- **`IFlakeReporter` is intentionally NOT declared here.** Per the
  CLAUDE.md "Interface Design Principle" (only declare interfaces when
  both sides are specified — ERR-001 / ERR-004 hazard), the CI
  integration layer that would consume `IFlakeReporter` is unspecified
  at Stage 0. The interface is deferred to §7.2 Stage 1 deliverables
  and is declared in `src/CLAUDE.md` (or a Stage 1 CI spec) only after
  the consumer is concretely specified.

### 4.5 CI Pipeline Topology (shape only; concrete config Stage 1+)

- Pre-commit pipeline: unit + property (fast).
- PR pipeline: unit + integration + property + per-spec-changed
  scenarios.
- Nightly pipeline: full simulation tier + soak + #16 §5 determinism
  full suite.
- Diagram: trigger → tier → exit criteria. GitHub Actions is the
  repository CI provider at `.github/workflows/ci.yml`; the proposed
  architecture/evidence gate remains inactive until A8.

### 4.6 Concrete Runner / CI Paths

- D1 is resolved on NUnit: `tools/dotnet-ci/generate_projects.py`
  pins NUnit 3.14.0 + NUnit3TestAdapter 4.6.0 and the blocking Linux
  shim gate executes `dotnet test` through
  `tools/dotnet-ci/run-gate.sh`.
- D4 is resolved on GitHub Actions at `.github/workflows/ci.yml`.
- Coverage tooling, pre-commit installation, and `IFlakeReporter`
  remain deferred where their prerequisites are still absent.

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
  file to its #16 §1.1.1 tier and applies KD-9 thresholds.
- Exemption handling per §3.6.5.

### 5.6 FR-to-Verification Traceability

- Single table indexed by FR-TS-###; columns: `Verification Mechanism |
  Tooling | Activation Stage | Output Artifact`.
- Coverage is FR-TS-001 … 097.
- FR-TS-086 … 097 map to strict applicability, proof-artifact/schema
  validation, structural/lifecycle/failure-injection/mutation proof,
  closure freshness, Governance convergence, execution truth,
  governance-tool self-verification, bounded substitution, and KD-W1
  activation/tuning checks.
- The architecture-proof fixture set includes stale/missing proof,
  applicability ambiguity, closure drift, activation-anchor/KD-W1
  violations, skip/exclusion conflicts, and wrong-target/no-op mutation
  or failure injection.
- Stage 0 most rows resolve to "manual review against §3 mechanics" —
  acknowledged degenerate (parallel to Spec #20 §5.5 acknowledgement).

### 5.7 Determinism-Suite Consumption Verification

- Spec #19 declares no numerical determinism tests of its own.
- This subsection records the *consumption* contract: every CI pipeline
  runs #16 §5's full tier set; failures block per KD-2.
- Boundary review check: any change to #16 §5 that touches tier names
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
  - Test runner — **D1 resolved:** NUnit; generated projects pin
    NUnit 3.14.0 + NUnit3TestAdapter 4.6.0 and the Linux shim gate runs
    `dotnet test`.
  - Property framework: FsCheck or equivalent — D2 still deferred.
  - Coverage: Coverlet or equivalent — D3 still deferred.
  - Project-wide mutation testing: Stryker.NET or equivalent — D6
    still deferred; FR-TS-091 targeted governance mutation is separate.
  - CI provider — **D4 resolved:** GitHub Actions at
    `.github/workflows/ci.yml`. This fact does not activate the
    architecture/evidence required status; A8 owns that transition.

### 6.2 CI Pipeline Policy (boundary with #18 / Governance / #20)

- Four gate classes feed the orchestrator: functional (#19),
  performance (#18), determinism (#16 §5), and architecture/evidence
  (#19 mechanics consuming Governance + #20 authorities).
- The architecture/evidence gate is topology only in A3.2b and does
  not block until A8 activation after A3.4/A4 prerequisites.
- Flake quarantine suppresses only an eligible functional-gate effect;
  it cannot satisfy required architecture proof.
- Required architecture execution reported `failed`, `skipped`, or
  `runner-failed` remains unsatisfied. Only deliberate
  `excluded`/`unavailable`/`not-run` may use an exact approved
  FR-TS-096 bounded substitute.
- Required-test/exclusion intersections are rejected. Owning test
  placement does not imply execution; the gate must execute or consume
  an exact machine-readable owning-runner result.
- KD-3 binding: Spec #19 cites #18 thresholds by reference; never
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
  - **Determinism defect** → routed to #16 §5 process (KD-2).
- 6.4.2 Triage cadence:
  - PR-blocking failures: investigated within 24 hours.
  - Quarantined tests: reviewed weekly.
  - Spec defects: reviewed at next spec-revision cycle.
- 6.4.3 Severity scale:
  - **Critical** — blocks Stage milestone.
  - **High** — blocks current sprint.
  - **Medium** — backlogged with date target.
  - **Low** — backlog, no date.
  - Severity schedules Spec #19 defect handling only; Governance
    convergence is disposition/status-based under FR-TS-093.
- 6.4.4 Defect-authority traceability:
  - Authority may be an FR, admitted architectural property, approved
    invariant/equivalent authority, or concrete independently
    established correctness/integrity failure. Novel generalized
    preference routes to Candidate Property.

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
- `IFlakeReporter` interface declaration (deferred from §4.4 per
  CLAUDE.md "Interface Design Principle" — declared in `src/CLAUDE.md`
  or a Stage 1 CI spec once the CI integration layer is concretely
  specified; both producer and consumer must be specified before this
  interface is written).
- Project-wide mutation-testing first activation (§6.1); FR-TS-091 targeted governance mutation is separate and does not wait for this deferral.
- Scenario library populated index (§3.3.6).
- Per-spec §5 schema-conformance auto-check (§5.4).
- Appendix D approved-spec §5 survey populated (deferred from §9.2 per
  §3.5.4 dilution policy; see M3 in adversarial-review resolution map).

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

- D1 — **RESOLVED: NUnit**, generated-project + `dotnet test` gate.
- D2 — Property framework pin — still deferred.
- D3 — Coverage tool pin — still deferred.
- D4 — **RESOLVED: GitHub Actions**, `.github/workflows/ci.yml`.
- D5 — LFS storage decision for fixtures (§3.8.2) — Stage 0+1.
- D6 — Project-wide mutation-testing activation date — Stage 1; excludes FR-TS-091 targeted governance mutation.
- D7 — Visual-regression framework selection — Stage 1+ (§3.9.3).
- D8 — Coverage-guided (AFL-style) fuzzing adoption — Stage 1+
  (§3.4.1).

### 7.6 Version History

---

## SECTION 8 — REFERENCES & CITATION AUDIT (`section-8.md`)

### 8.1 Source Register

- Root `CLAUDE.md` (project invariants; "When Writing Code" rules).
- Project Architecture Governance v0.10 (property/applicability/
  exceptions/proof triggers/disposition/convergence authority).
- Spec #16 (Deterministic Simulation) — §1.3 tier classification, §5
  canonical save format, §7 regression suite, §8 trace channels.
- Spec #18 (Performance Optimization) — §4 / §7 regression gates.
- Spec #20 (Code Standards) — test-fixture carve-outs plus A3 FR-CS-074 … 081 / §3.5 integration and activation declarations consumed by §3.11.
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
- Tier vocabulary cited from #16 §1.1.1 by reference only (KD-1).

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
- Boundary statements with #16 §5 (KD-2), #18 §4 / §7 (KD-3), Governance v0.10, and #20 A3 integration/activation authority explicit.
- FR catalogue and §5.6 traceability cover FR-TS-001 … 097.

### 9.2 Quality Checklist

- Cite-not-redefine rule audited (no #16 / #18 / #20 restatements).
- Every FR row through FR-TS-097 resolves to a §5.x verification mechanism.
- Architecture proof examples/fixtures and execution truth match the frozen A2 contract; the architecture/evidence gate carries no A3.2b enforcement claim.
- Every approval-checklist row in *this* checklist cites either a file
  path or a check name (KD-6 self-application).
- All cross-references (XC-/FM-/EC-/ERR-) resolve.
- Per-spec §5 schema (Appendix C) present and complete.
- All `TBD-NORMATIVE`-tagged citations of #16 (KD-2) and #18 (KD-3)
  enumerated; outstanding tags listed for the reviewer.
- **Appendix D survey is NOT a #19-approval gate (M3).** The survey
  of #1–#8 §5 sections is a Stage 0+1 deliverable (§7.2); for #19's
  own approval the requirement is only that Appendix D *exists with
  the schema and an empty / partial table*. Completing the survey
  rows is deferred so #19's approval is not converted into an
  eight-spec audit task. Sequencing rationale recorded in
  PROGRESS.md alongside #19's milestone.

### 9.3 Review Checklist

- Open issues logged in `CLAUDE.md` "OPEN ISSUES" if any.
- Lead-developer sign-off captured.
- `spec-error-log.md` updated with any cross-spec drift discovered
  during drafting.
- `SPEC_INDEX.md` status updated atomically with sign-off at A3.4; A3.2b leaves the approved-baseline row untouched.

### 9.4 Decision

- Status block (`IN REVIEW` / `APPROVED` / `SUSPENDED` / `DEFERRED`).
- Approval evidence: file paths to programmatically-verifiable sources
  (KD-6 self-application — every row of this checklist must comply).
- **Evidence-artifact convention for `[GT]` governance numbers (L5).**
  Per §3.10, each governance number's evidence is the section-file
  citation that publishes the number (e.g., `section-3.md §3.1.2` for
  pyramid percentages). Checklist rows pointing at `[GT]` numbers MUST
  cite the section-file path verbatim; the §5.3 auditor confirms the
  literal number is present at that path.

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
  ERR-019-NNN. **Scope at #19 approval (M3):** Appendix D ships with
  the schema and the table headers populated; row contents are a
  Stage 0+1 deliverable (§7.2). The survey itself is *not* a #19
  approval gate; KD-6 dilution remains *visible* via the empty rows
  even before the survey is filled in. Stage 1 trigger for actual
  per-spec revisions remains unchanged.

- **Appendix E — Stage-0 Local Runbook.**
  Concrete shell-script outline for `tools/run-tests-local.sh`: pre-
  commit checks against `docs/specs/` only (no `src/` yet); invocation
  of §5.3 checklist auditor and §5.4 schema-conformance auditor.

- **Appendix G — Architecture Proof Artifact & Closure Contract.**
  Canonical schema-shaped examples and explanatory contract for
  FR-TS-086 … 097: proof fields, class closure, execution truth,
  freshness, N/A/bounded semantics, failure injection, mutation, and
  gate-consumption boundary. Example identifiers use reserved/example
  namespaces rather than live requirement IDs.

- **Appendix F — Glossary.**
  Spec #19-specific terms only (taxonomy layer names, flake, quarantine,
  eviction, scenario, fixture, golden trace). Determinism / performance
  terms cited from #16 / #18.

---

## VERSION HISTORY

| Version | Date         | Author      | Notes                                                                                                         |
|---------|--------------|-------------|---------------------------------------------------------------------------------------------------------------|
| 1.0     | May 12, 2026 | Claude Code | Initial detailed outline drafted from `outline.md` v1.0. Addresses all 13 findings from May 6 adversarial review. |
| 1.1     | May 12, 2026 | Claude Code | Addresses all 12 findings (3H / 4M / 5L) from second adversarial review (May 12). Changes: KD-2 / KD-3 status caveats + `TBD-NORMATIVE` tagging (H1); §1.4 + KD-2 disclose #16↔#19 sequencing (H2); §4.4 removes `IFlakeReporter`, deferred to §7.2 (H3); §3.4.3 restructured to capture seeds into #19-owned holding area (M1); §3.5.4 acknowledges KD-6 retroactive dilution (M2); §9.2 + Appendix D down-scope survey out of #19 approval gate (M3); §3.7 explicitly Stage-gated (M4); §3.1.2 clarifies ceiling-only bound semantics (L1); §3.6.1 cite-precision guard on #16 §1.3.1 (L2); §3.4.1 drops vacuous Stage-0 disclaimer (L3); §6.1 neutral CI-provider selection criteria (L4); §3.10 + §9.4 specify `[GT]` evidence artifact (L5). No FR text changes; no new FR IDs introduced. |
| 1.3     | September 3, 2026 | — | **A3.2b review correction (Codex #353 finding 2).** Repoints the `tests/scenarios/index.<ext>` encoding/extension pin from D1 to the new **D9**. A3.2b closed D1 on the test runner (NUnit) alone, which stranded the manifest encoding decision that D1 had jointly owned; every live `index.<ext>` reference now names D9. No extension is pinned here — pinning one in A3.2b would be a normative content decision outside this slice. Also drops the outline's pre-empting `index.json` for the spec's `index.<ext>` notation. |
| 1.2     | September 3, 2026 | — | **A3.2b synchronization overlay.** Updates the active drafting map to FR-TS-001 … 097, §3.11 architecture proof mechanics / §3.12 history, four-gate topology, Governance/#20 authority split, targeted-mutation exception, Appendix G, and A3.4/A8 activation boundaries. Historical May adversarial-review tables below remain historical evidence. Live-repo audit records D1=NUnit and D4=GitHub Actions while leaving D2/D3/D5–D8 deferred. |

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

### Second adversarial review (May 12, 2026 — outline-detailed.md v1.0)

| Finding | Severity | Resolved by |
|---------|----------|-------------|
| H1 — Forward-binds to unwritten #16 (IN PROGRESS) and #18 (NOT STARTED) | H | KD-2 status caveat; KD-3 status caveat; §1.4 upstream-status notes; `TBD-NORMATIVE` tagging mandate |
| H2 — Circular dependency #16 ↔ #19 not acknowledged | H | KD-2 sequencing constraint; §1.4 bidirectional-sequencing note |
| H3 — `IFlakeReporter` phantom interface (ERR-001 / ERR-004 hazard) | H | §4.4 removal + explicit rationale; §7.2 Stage 1 deliverable |
| M1 — `IFlakeReporter`-style write-binding into #16 §7's owned suite via auto-capture | M | §3.4.3 restructured: capture into #19-owned `tests/data/captured-seeds/` holding area; promotion via #16-published external-capture-hook contract (TBD-NORMATIVE) |
| M2 — KD-6 retroactive scope vs KD-4 grandfather creates unenforced dilution | M | §3.5.4 explicit "acknowledged dilution" paragraph; Appendix D enumerates gaps so dilution is visible even when not enforced |
| M3 — Appendix D survey of #1–#8 converted #19 approval into 8-spec audit | M | §9.2 + Appendix D down-scope: survey-row contents are Stage 0+1 (§7.2); only the schema is a #19 approval gate |
| M4 — §3.7 flake handling normative immediately, inconsistent with §7.2 Stage-1 gating | M | §3.7 header block: explicit Stage-gated activation per KD-5; FR-TS-061…067 rows updated in §5.2 |
| L1 — Pyramid arithmetic under-constrained (ceilings-only) | L | §3.1.2 bound-semantics paragraph clarifies intentional ceiling-only design |
| L2 — `#16 §1.3.1` subsection-number cite-precision risk | L | §1.4 upstream-status note; §3.6.1 cite-precision guard requiring grep verification at draft time |
| L3 — Vacuous "no AFL fuzzing at Stage 0" disclaimer | L | §3.4.1 rephrased; tracked as D8 deferred decision in §7.5 |
| L4 — §6.1 CI provider rationale was speculation ("perf gates are longest CI step") | L | §6.1 replaced with neutral selection criteria; no #18-ownership inference |
| L5 — KD-6 self-application against `[GT]` governance numbers ambiguous | L | §3.10 declares spec body IS the evidence; §9.4 evidence-artifact convention names section-file citations as compliant evidence |
