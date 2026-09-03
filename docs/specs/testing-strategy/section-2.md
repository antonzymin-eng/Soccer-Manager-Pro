# Testing Strategy & Framework Specification #19 — Section 2: Functional Requirements & Test Governance Model

**Created:** May 12, 2026
**Last Updated:** September 2, 2026
**Version:** 0.3
**Status:** AMENDMENT DRAFT (A3.2a; May 15, 2026 approved baseline remains in force)
**Amendment plan:** docs/planning/project-architecture-governance-integration-plan.md v0.35, §7; A3.2a
**Purpose:** Conformance vocabulary, the full FR-TS-### catalogue with
verification pointers, and the failure-to-comply modes. Rule mechanics
for every FR live in §3; §2 publishes the rule statement.

---

## 2.1 Conformance Levels

Spec #19 adopts RFC 2119 keywords (MUST / SHOULD / MAY / MUST NOT)
identically to Spec #20 §2.1.

- **MUST** — non-conformance blocks merge once the relevant gate
  activates; before activation, non-conformance is recorded as a §2.3
  finding.
- **SHOULD** — non-conformance is permitted with documented rationale
  signed off by the lead developer; the exception expires at the next
  test-suite refactor (§2.3).
- **MAY** — permissive; documents an allowed practice without imposing
  it.
- **MUST NOT** — banned. Use parallels Spec #20 §2.1 (e.g., banned-API
  rule for `System.Random`).

**Exception with sign-off** remains a Spec #19-local mechanism. The
exception is recorded in tests/exceptions.md (Stage 0+1 deliverable)
with rationale, the exact FR cited, bounded scope, approval, and expiry
trigger.

A Spec #19 exception or coverage exemption can waive only the exact
Spec #19 obligation its owning rule permits. It **MUST NOT** waive an
admitted architectural property, required architectural proof,
concrete correctness/integrity failure, or Governance Blocker. Where
both a Spec #19 exception and a Governance property exception are
required, both records are independently necessary; neither mechanism
substitutes for the other.

## 2.2 Functional Requirement Catalogue

Each FR row carries: `ID | Statement | Level | Activation stage`. The
detailed mechanics are in the §3 subsection named in the partition
table below; source citations and verification pointers are reproduced
in the §5.6 traceability table.

**Partition.**

| FR Range | Topic | Rule mechanics in | Verification in |
|----------|-------|-------------------|-----------------|
| FR-TS-001 … 010 | Test taxonomy & pyramid contract | §3.1 | §5.2, §5.6 |
| FR-TS-011 … 020 | Determinism regression consumption | §3.2 | §5.7 |
| FR-TS-021 … 030 | Scenario library architecture & runner | §3.3 | §5.6 |
| FR-TS-031 … 039 | Property / fuzz testing & seed governance | §3.4 | §5.6 |
| FR-TS-040 … 045 | Programmatic-verification mandate (KD-6) | §3.5 | §5.3 |
| FR-TS-046 … 052 | Per-spec §5 conformance schema | §3.5.3 | §5.4 |
| FR-TS-053 … 060 | Coverage targets, per-tier policy | §3.6 | §5.5 |
| FR-TS-061 … 067 | Flake detection, quarantine, eviction | §3.7 | §5.6 |
| FR-TS-068 … 074 | Test-data governance | §3.8 | §5.6 |
| FR-TS-075 … 080 | CI orchestration (Stage-gated) | §6.1, §6.2 | §5.6 |
| FR-TS-081 … 085 | Defect lifecycle and triage | §6.4 | §5.6 |
| FR-TS-086 … 097 | Architecture proof / evidence integration | §3.11 | §5.6 / architecture-evidence gate |

**FR catalogue.**

> "Stage 0" = applies to spec drafts now. "Stage 0+1" = activates at
> first `src/` code commit (KD-5 governs Stage 0+1 rows). Source-cite
> column is omitted in this table; cross-reference KD-N is shown where
> the rule traces to a key decision. The §5.6 traceability table
> reproduces verification mechanism and tooling per FR.

| ID | Statement | Level | Activation |
|----|-----------|-------|------------|
| FR-TS-001 | Every executable test belongs to exactly one of the five taxonomy layers (unit / integration / simulation / determinism / e2e-or-soak). | MUST | Stage 0+1 |
| FR-TS-002 | Unit tests perform no heap allocation in the assertion body and complete in ≤ 1 ms wall-time on the certified host. (`1 ms` tag `[GT]`, see §3.10.) | MUST | Stage 0+1 |
| FR-TS-003 | Integration tests wire 2–5 subsystems without instantiating a Unity scene. | MUST | Stage 0+1 |
| FR-TS-004 | Simulation tests invoke the full subsystem stack under a scripted scenario; rendering is disabled. | MUST | Stage 0+1 |
| FR-TS-005 | Determinism tests are owned by Spec #16 §5; #19 consumes them as a required layer and does not redefine them. (KD-2) | MUST | Stage 0+1 |
| FR-TS-006 | End-to-end / soak tests run ≥ one full 90-minute match of in-game time. (`90 min` tag `[FIXED]` — laws of football; see §3.10.) | MUST | Stage 0+1 |
| FR-TS-007 | The pyramid contract (unit ≥ 60%, integration ≤ 25%, simulation ≤ 12%, e2e/soak ≤ 3%) MUST be satisfied at PR-merge time once activated. Determinism layer is counted separately. | MUST | Stage 0+1 |
| FR-TS-008 | Tests MUST be named per §3.1.4 (`unit_<system>_<behaviour>`, `int_<A>_<B>_<behaviour>`, `sim_<scenario>`, `e2e_<scenario>`). | MUST | Stage 0+1 |
| FR-TS-009 | A per-spec §5 MAY declare tighter lower bounds on its own layer mix; it MUST NOT declare upper bounds that exceed the pyramid contract. | MUST | Stage 0+1 |
| FR-TS-010 | Anti-patterns enumerated in §3.1.3 MUST be flagged at code review and MUST NOT merge. | MUST | Stage 0+1 |
| FR-TS-011 | Every CI pipeline declared in §6 MUST include Spec #16 §5's regression tiers in their canonical order (unit / integration / scenario / soak). (KD-2) | MUST | Stage 0+1 |
| FR-TS-012 | A failure in any #16 §5 tier blocks merge; Spec #19 does not soften the exit criteria. (KD-2) | MUST | Stage 0+1 |
| FR-TS-013 | Spec #19's taxonomy MUST NOT collide with #16 §5 tier names. | MUST | Stage 0 |
| FR-TS-014 | Cross-spec scenario assertions that do not depend on bitwise determinism are owned by Spec #19 §3.3, not by #16 §5. (KD-8) | MUST | Stage 0+1 |
| FR-TS-015 | Any change to #16 §5 tier names or exit criteria triggers a §3.2 review of Spec #19. | MUST | Stage 0 |
| FR-TS-016 | The #16 §5 suite is invoked through a single integration point declared in §4.3 (`ITestHarness`); duplicate entry points are forbidden. | MUST | Stage 0+1 |
| FR-TS-017 | Spec #19's functional-regression assertions on top of #16 §5 MUST be tagged so they can be disabled independently for bisection. | MUST | Stage 0+1 |
| FR-TS-018 | Spec #19 MUST NOT introduce new determinism tier categories. New categories require a #16 §5 revision. (KD-2) | MUST | Stage 0 |
| FR-TS-019 | Functional regression assertions that depend on RNG MUST route through `DeterministicRngService` (KD-7). | MUST | Stage 0+1 |
| FR-TS-020 | The boundary review obligation in FR-TS-015 MUST be recorded in §1.4 of any future Spec #19 revision. | MUST | Stage 0 |
| FR-TS-021 | Every scenario is defined by a manifest entry conforming to Appendix A's schema. | MUST | Stage 0+1 |
| FR-TS-022 | Per-spec scenarios are defined in the owning spec's §5; cross-spec scenarios are defined in Spec #19 §3 (KD-8). | MUST | Stage 0 |
| FR-TS-023 | Scenarios MUST be hermetic: no shared global state between scenario runs. | MUST | Stage 0+1 |
| FR-TS-024 | The single scenario runner entry point is `ScenarioRunner.Run(manifestPath, seed)` (§3.3.3). | MUST | Stage 0+1 |
| FR-TS-025 | Every scenario records its RNG seed in the manifest; seeds are reproducible verbatim. | MUST | Stage 0+1 |
| FR-TS-026 | Every fixture loaded by a scenario passes the §3.3.4 fixture validator against #16 §3.2.4.1 canonical byte-level schema (KD-10). | MUST | Stage 0+1 |
| FR-TS-027 | Scenario directory layout follows `tests/scenarios/<owning-spec>/` for per-spec and `tests/scenarios/cross-spec/` for cross-spec. | MUST | Stage 0+1 |
| FR-TS-028 | A root manifest (`tests/scenarios/index.<ext>`; `<ext>` pinned at Stage 0+1) enumerates every scenario. Stage 0 deliverable: schema (Appendix A). Stage 1 deliverable: populated index. | MUST | Stage 0+1 |
| FR-TS-029 | Scenario tier classification (Tier A / B / C) MUST be assigned per #16 §1.1.1 at scenario creation time. (KD-9) | MUST | Stage 0+1 |
| FR-TS-030 | Scenarios MUST declare an expected-outcome envelope; "implicit pass" is forbidden. | MUST | Stage 0+1 |
| FR-TS-031 | Property tests use the framework pinned in §6.1 (Stage 0+1 selection); during Stage 0 the framework is unpinned. | MUST | Stage 0+1 |
| FR-TS-032 | Property and fuzz seed *selection* MAY be non-deterministic; the executed test body MUST route through `DeterministicRngService` (`SplitMix64`) with the selected seed. (KD-7) | MUST | Stage 0+1 |
| FR-TS-033 | The selected seed MUST be logged at start of every property / fuzz run. | MUST | Stage 0+1 |
| FR-TS-034 | A failing property / fuzz seed MUST be captured to `tests/data/captured-seeds/<spec>/<YYYY-MM-DD>-<seed>.fixture` in #16 §3.2.4.1 canonical format. (KD-10) | MUST | Stage 0+1 |
| FR-TS-035 | Captured seeds are re-run by Spec #19's own property / fuzz suite on every CI run until #16 §5 publishes an external-capture-hook contract that promotes them into the regression corpus. | MUST | Stage 0+1 |
| FR-TS-036 | Property tests using `System.Random` directly are banned (parallels Spec #20 §3.4.2). | MUST NOT | Stage 0+1 |
| FR-TS-037 | Fuzz tests that do not record their seed are banned. | MUST NOT | Stage 0+1 |
| FR-TS-038 | The property catalogue (Appendix B) MUST cover every category named in §3.4.4 with at least one exemplar. | MUST | Stage 0 |
| FR-TS-039 | Coverage-guided (AFL-style) fuzzing is not adopted by default; activation is deferred to D8 in §7.5. | MAY | Stage 1+ |
| FR-TS-040 | Every approval-checklist row in every spec MUST resolve to either (a) a named, version-controlled file path containing the claimed value, or (b) a programmatic check whose output is captured. (KD-6) | MUST | Stage 0 |
| FR-TS-041 | An approval-checklist row whose "evidence" is prose without a file path or check name MUST be rejected at review time. | MUST | Stage 0 |
| FR-TS-042 | The §5.3 checklist auditor walks every approval-checklist row at spec review time; unresolved rows block APPROVED status. | MUST | Stage 0 |
| FR-TS-043 | Stage 0 auditor mechanics are manual (a reviewer); Stage 0+1 auditor mechanics are automated via `tools/checklist-auditor.py` (final language pin parallel to CLAUDE.md "When Writing Code"). | MUST | Stage 0+1 |
| FR-TS-044 | For `[GT]` governance numbers in this spec, the evidence artifact is the section-file citation that publishes the number (§3.10, §9.4). | MUST | Stage 0 |
| FR-TS-045 | KD-6 enforcement is retroactively diluted for approved specs (#1–#8) per §3.5.4 acknowledged-dilution policy; gaps are recorded as `ERR-019-NNN` in `spec-error-log.md`. | SHOULD | Stage 0 |
| FR-TS-046 | Every per-spec §5 MUST contain a test-count-by-taxonomy-layer table. | MUST | Stage 0 |
| FR-TS-047 | Every per-spec §5 MUST list its property tests with property names and tier classification. | MUST | Stage 0 |
| FR-TS-048 | Every per-spec §5 MUST list its scenarios with manifest paths. | MUST | Stage 0 |
| FR-TS-049 | Every per-spec §5 MUST declare coverage targets per-tier per KD-9. | MUST | Stage 0 |
| FR-TS-050 | Every per-spec §5 MUST declare the determinism-tier classification of every authoritative field it references. | MUST | Stage 0 |
| FR-TS-051 | Every per-spec §5 MUST point to the §9 approval-checklist row each test verifies. | MUST | Stage 0 |
| FR-TS-052 | Per-spec §5 schema-conformance is checked at spec-review time per §5.4; nonconformance blocks APPROVED status for that spec. | MUST | Stage 0 |
| FR-TS-053 | Tier A authoritative code MUST achieve ≥ 98% line and ≥ 95% branch coverage. (KD-9) | MUST | Stage 0+1 |
| FR-TS-054 | Tier B bounded-authoritative code MUST achieve ≥ 90% line and ≥ 80% branch coverage. (KD-9) | MUST | Stage 0+1 |
| FR-TS-055 | Tier C non-authoritative code is lint-only; no numeric coverage target. (KD-9) | MAY | Stage 0+1 |
| FR-TS-056 | Test code itself is NOT counted toward coverage. | MUST | Stage 0+1 |
| FR-TS-057 | The coverage tool (§6.1, deferred) MUST emit a per-tier breakdown consumable by the §5.5 auditor. | MUST | Stage 0+1 |
| FR-TS-058 | Reporting cadence: per-PR delta at Stage 0+1; absolute per-tier dashboard at Stage 1 (§3.6.4). | MUST | Stage 0+1 |
| FR-TS-059 | Coverage exemptions require lead-developer sign-off and are recorded in `tests/coverage-exemptions.md`; exemptions expire at the next refactor of the affected file. | MUST | Stage 0+1 |
| FR-TS-060 | Tier classification of every file under coverage measurement MUST be sourced from #16 §1.1.1; #19 does not redefine it. | MUST | Stage 0+1 |
| FR-TS-061 | A test is "flaky" if two runs of the same revision under the same `EnvironmentFingerprint` produce different pass/fail outcomes (cited from #16 §4.8). | MUST | Stage 0+1 |
| FR-TS-062 | CI MUST run every test twice on the same revision; disagreement triggers automatic quarantine. | MUST | Stage 0+1 |
| FR-TS-063 | Quarantined tests continue to execute; quarantine removes only their eligible functional-gate blocking effect and remains subject to FR-TS-077. | MUST | Stage 0+1 |
| FR-TS-064 | Quarantine auto-expires after 14 days; expired tests MUST be either fixed or deleted. Permanent quarantine is forbidden. | MUST | Stage 0+1 |
| FR-TS-065 | A test quarantined ≥ 3 times in 90 days MUST be deleted and the rationale recorded in `tests/flake-eviction-log.md`. | MUST | Stage 0+1 |
| FR-TS-066 | Re-introduction of an evicted test requires a new test ID and a written root-cause analysis. | MUST | Stage 0+1 |
| FR-TS-067 | `[Retry]` attributes and sleep-based synchronization in tests are banned (§3.7.5). | MUST NOT | Stage 0+1 |
| FR-TS-068 | Fixtures live in `tests/data/fixtures/`; golden outputs in `tests/data/golden/`; fuzz corpora in `tests/data/corpora/`. | MUST | Stage 0+1 |
| FR-TS-069 | Every fixture file conforms to #16 §3.2.4.1 canonical byte-level schema. (KD-10) | MUST | Stage 0+1 |
| FR-TS-070 | Every fixture carries a `format-version` field; validator rejects unknown versions (no silent migration). | MUST | Stage 0+1 |
| FR-TS-071 | Every captured fixture records source seed, capturing-spec ID, capture date, and `EnvironmentFingerprint` at capture. | MUST | Stage 0+1 |
| FR-TS-072 | Fixtures whose owning test is deleted MUST be deleted in the same commit (no orphan fixtures). | MUST | Stage 0+1 |
| FR-TS-073 | LFS / no-LFS storage decision for fixtures is deferred to D5 (§7.5). | MAY | Stage 0+1 |
| FR-TS-074 | Cross-fixture provenance edges (e.g., scenario A reuses fixture B) MUST be declared in the manifest (Appendix A). | MUST | Stage 0+1 |
| FR-TS-075 | Three pipelines are mandatory: pre-commit (unit + property), PR (unit + integration + property + per-spec-changed scenarios), nightly (full simulation + soak + #16 §5 full suite). See §4.5. | MUST | Stage 0+1 |
| FR-TS-076 | Functional-gate failure blocks merge (Spec #19 authority); performance-gate failure blocks merge (Spec #18 authority); determinism-gate failure blocks merge (#16 §5 authority); once activated, architecture/evidence-gate failure blocks merge under Spec #19 evidence mechanics while Governance and Spec #20 retain ownership of the underlying architectural obligations. | MUST | Stage 0+1 |
| FR-TS-077 | No gate is "soft." Flake quarantine (§3.7) may relax only an eligible functional-test failure; it **MUST NOT** waive missing or stale required architectural proof, a structural governance gate, determinism/performance gates, or any other independently blocking obligation. | MUST | Stage 0+1 |
| FR-TS-078 | CI provider selection criteria are declared in §6.1 (L4); the final pin lands in `src/CLAUDE.md`. | MUST | Stage 0+1 |
| FR-TS-079 | Until CI activates, the same gate composition runs locally via `tools/run-tests-local.sh` (Appendix E). | MUST | Stage 0 |
| FR-TS-080 | Spec #19 cites #18 §4 thresholds by reference; #19 MUST NOT republish performance numbers. (KD-3) | MUST NOT | Stage 0+1 |
| FR-TS-081 | Defects are classified per §6.4.1 (spec / implementation / test / determinism). Misclassified defects are themselves a procedural violation. | MUST | Stage 0+1 |
| FR-TS-082 | PR-blocking failures are investigated within 24 hours; quarantined tests are reviewed weekly; spec defects are reviewed at next spec-revision cycle. | MUST | Stage 0+1 |
| FR-TS-083 | Defect severity uses the four-level scale in §6.4.3 (Critical / High / Medium / Low). | MUST | Stage 0+1 |
| FR-TS-084 | Every defect MUST cite its governing authority: an FR, admitted architectural property, approved invariant/equivalent authority, or concrete independently established correctness/integrity failure. A novel generalized preference with no existing authority MUST be routed as a Governance Candidate Property rather than treated as a defect solely by reviewer preference. | MUST | Stage 0+1 |
| FR-TS-085 | Determinism defects are routed to #16 §5's process; Spec #19's triage is bypassed for that class. (KD-2) | MUST | Stage 0+1 |
| FR-TS-086 | Architectural changes MUST resolve the versioned applicability manifest and record every matched trigger, requirement, and proof class. | MUST | Stage 0+1 |
| FR-TS-087 | Required architectural proof MUST use the canonical versioned artifact, separate material subject identity from provenance, and bind the applicability-resolved dependency/configuration/tool surface by reproducible digest. | MUST | Stage 0+1 |
| FR-TS-088 | Structural proof MUST cover the complete applicability-resolved host/root/alternate/test/public universe or, only where FR-TS-096 applies, record an approved bounded substitute and the omitted uncertainty. A bounded substitute is not a Governance FR-AG-026 surface exclusion: every mechanically finite surface remains accounted for through scope, recorded Non-scope, or Governance exception. | MUST | Stage 0+1 |
| FR-TS-089 | Lifecycle/order proof MUST independently demonstrate required construction, activation, use, teardown, and restore ordering rather than rely on declaration text. | MUST | Stage 0+1 |
| FR-TS-090 | Meaningful triggered failure paths MUST be deliberately executed where reasonably inducible and record the exact injected condition, target, expected path, executed test/command, and observed result. | MUST | Stage 0+1 |
| FR-TS-091 | Triggered mutation MUST demonstrate evidence sensitivity for the named critical invariant using an exact target and reproducible mutant/patch identity, baseline result, mutant result, and expected detector; no project-wide mutation-score target is created. | MUST | Stage 0+1 |
| FR-TS-092 | Reusable proof MUST have its complete relevant dependency universe mechanically derived and validated by proof class, and MUST become stale only on material changes inside that resolved closure or its applicable tool/configuration semantics. | MUST | Stage 0+1 |
| FR-TS-093 | Spec #19 merge/review mechanics MUST consume Governance disposition/status/convergence state and MUST NOT rederive convergence from severity. Governance remains the normative owner of finding disposition and convergence semantics. | MUST | Stage 0 |
| FR-TS-094 | Missing, failed, stale, schema-invalid, applicability-incomplete, skipped, excluded, unavailable, not-run, or runner-failed required architectural proof MUST block merge once the gate is active. A bounded substitute MAY replace only an excluded, unavailable, or not-run execution when FR-TS-096 permits it; it MUST NOT convert failed, skipped, or runner-failed execution into satisfaction. | MUST | Stage 0+1 |
| FR-TS-095 | Merge-critical governance tooling MUST have known-good, known-bad, and blind-spot verification proportionate to the consequence of false positives and false negatives. | MUST | Stage 0+1 |
| FR-TS-096 | Bounded substitutes are permitted only for computationally disproportionate, intentionally omitted, or unavailable proof and MUST record authority, approval, scope/rationale, and the omitted surface or remaining uncertainty. They MUST NOT waive an executed proof failure, runner failure, ordinary skipped execution, or an independently applicable Governance exclusion requirement. | MUST | Stage 0+1 |
| FR-TS-097 | A [GT] or owner-declared calibration/tuning change MUST NOT land for a component whose activation state is intentionally-disabled, pending-integration, or unresolved unless an approved exception explicitly authorizes that exact tuning scope. | MUST | Stage 0+1 |

## 2.3 Failure-to-Comply Modes

The following compliance modes apply. They are not a severity ladder where
one mode can weaken an independently applicable blocker:

- **Review block.** The PR cannot merge. Applies once the relevant CI
  gate activates.
- **Quarantine.** An eligible flaky functional test moves to the
  flake-quarantine pool with 14-day auto-expiry. Quarantine removes only
  that functional-gate blocking effect; if the same test/result is required
  architectural proof, the architecture obligation remains unsatisfied under
  FR-TS-077/094 (§3.7, §3.11.5).
- **Refactor required.** The PR merges with a follow-up issue filed
  against the spec or test; severity per §6.4.3. This mode is unavailable
  when FR-TS-094 or another independently blocking authority requires the
  current change to remain blocked.
- **Exception with sign-off.** Recorded in tests/exceptions.md with
  rationale, exact FR, bounded scope, approval, and expiry trigger; expires
  at the owning rule's trigger. It cannot waive an admitted property,
  required proof, concrete correctness/integrity failure, or Governance
  Blocker.
- **Architecture-evidence block.** Once the architecture/evidence gate
  is active, any FR-TS-094 unsatisfied state blocks merge unless the exact
  obligation is validly satisfied by FR-TS-096. A stale or incomplete proof
  cannot be downgraded to a follow-up.
- **Governance-convergence block.** Review convergence consumes Governance
  disposition/status state. An open or invalidly dispositioned finding
  remains open regardless of severity.
- **Activation/tuning block.** FR-TS-097 prevents calibration/tuning changes
  for inactive, pending, or unresolved owning components unless the exact
  permitted exception path is satisfied.
- **Spec-§5 nonconformance.** A per-spec §5 fails Spec #19's schema
  check at draft-review time; the spec cannot reach APPROVED status
  until conformance is restored (§5.4). For approved specs #1–#8 the
  §3.5.4 acknowledged-dilution policy applies.
- **TBD-NORMATIVE re-introduction (Spec #19 self-applies).** After
  Spec #19 reaches APPROVED, any subsequent revision that re-introduces
  a `TBD-NORMATIVE` tag (e.g., upstream churn in #16 invalidates a
  previously resolved citation) flips Spec #19 status to SUSPENDED
  until the tag is re-resolved. This is the SUSPENDED trigger named
  in §9.4.

## 2.4 Data Structures (informational)

Spec #19 defines **no runtime data structures consumed by gameplay**.
The test-harness data structures it declares are:

- **Scenario manifest** — JSON-schema declaration in Appendix A;
  on-disk binary embeddings of fixtures conform to KD-10.
- **Fixture index** — root manifest enumerating every fixture under
  `tests/data/`; schema in Appendix A.
- **Flake ledger** — append-only record of every quarantine event,
  expiry, and eviction; schema in §3.7 and Appendix A.
- **Captured-seed corpus** — tests/data/captured-seeds/ holding area
  for fuzz / property failures; format conforms to KD-10 (§3.4.3).
- **Architecture proof artifact** — versioned governance evidence conforming
  to docs/tracking/architecture-governance/schemas/proof-artifact.schema.json
  and the frozen A2 executable semantics. It is tooling evidence only and is
  never game-state data.

None of these structures appears in the game-state assemblies; they
live under `tests/shared/` per §4.1.

## 2.5 Failure Modes

Spec #19's own failure modes (additional to §2.3):

- **Per-spec §5 schema drift** — discovered by §5.4 conformance check
  at spec review time.
- **Fabricated approval-checklist value** — caught by KD-6 mandate
  (FR-TS-040…042) at §5.3 audit.
- **Fixture-format drift from #16 §5** — caught by §3.3.4 fixture
  validator at scenario load.
- **Flake threshold breach** — caught by §3.7.4 eviction rule (≥ 3
  quarantines in 90 days).
- **Boundary drift with #16 §5** — caught by FR-TS-015 boundary review
  obligation any time #16 §5 changes.
- **Boundary drift with #18 §4 / §7** — caught at §6.2 gate-composition
  review when #18 publishes a draft that changes section numbering.
- **Applicability ambiguity or incomplete change context** — strict proof
  certification fails rather than guessing which proof classes apply.
- **Proof-closure or freshness drift** — a changed applicable root,
  transitive dependency, topology edge, configuration, extractor, or
  proof-semantic dependency invalidates only the affected proof closure.
- **Execution-truth mismatch** — a required proof records skipped, failed,
  runner-failed, or another unsatisfied execution state while claiming
  satisfaction; the artifact is invalid.
- **Wrong-target / ineffective perturbation** — failure injection or mutation
  does not exercise the named target or detector; the claimed proof is
  rejected.
- **Activation/tuning mismatch** — a [GT] or owner-declared tuning surface
  changes while an applicable owning component is not active and no exact
  permitted exception authorizes that scope.

## 2.6 Version History

| Version | Date         | Author      | Notes |
|---------|--------------|-------------|-------|
| 0.1     | May 12, 2026 | Claude Code | Initial draft from `outline-detailed.md` v1.1. FR-TS-001 … 085 enumerated; partition table aligns to §3 mechanics. |
| 0.2     | May 12, 2026 | Claude Code | Self-critique sweep. #16 citations corrected (§5 / §3.2.4.1 / §1.1.1 / §4.8); FR-TS-002 / FR-TS-006 carry inline value-tag pointers (L1 / L2); SUSPENDED-on-tag-reintroduction added to §2.3 (L8). |
| 0.3     | September 2, 2026 | Codex | **A3.2a governance amendment draft.** Appends FR-TS-086–097, qualifies FR-TS-063 quarantine, amends FR-TS-076/077/084, adds the architecture proof/evidence partition and failure modes, separates bounded substitutes from Governance-approved surface exclusions, and closes the Spec #19 exception boundary. The May 15 approved baseline remains operative until A3.4 atomic reapproval. |
