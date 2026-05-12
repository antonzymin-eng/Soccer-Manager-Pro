# Testing Strategy & Framework Specification #19 — Section 2: Functional Requirements & Test Governance Model

**Created:** May 12, 2026
**Last Updated:** May 12, 2026
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

**Exception with sign-off** semantics are identical to Spec #20 §2.1:
the exception is recorded in `tests/exceptions.md` (Stage 0+1
deliverable) with rationale, FR cited, and expiry trigger.

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

**FR catalogue.**

> "Stage 0" = applies to spec drafts now. "Stage 0+1" = activates at
> first `src/` code commit (KD-5 governs Stage 0+1 rows). Source-cite
> column is omitted in this table; cross-reference KD-N is shown where
> the rule traces to a key decision. The §5.6 traceability table
> reproduces verification mechanism and tooling per FR.

| ID | Statement | Level | Activation |
|----|-----------|-------|------------|
| FR-TS-001 | Every executable test belongs to exactly one of the five taxonomy layers (unit / integration / simulation / determinism / e2e-or-soak). | MUST | Stage 0+1 |
| FR-TS-002 | Unit tests perform no heap allocation in the assertion body and complete in ≤ 1 ms wall-time on the certified host. | MUST | Stage 0+1 |
| FR-TS-003 | Integration tests wire 2–5 subsystems without instantiating a Unity scene. | MUST | Stage 0+1 |
| FR-TS-004 | Simulation tests invoke the full subsystem stack under a scripted scenario; rendering is disabled. | MUST | Stage 0+1 |
| FR-TS-005 | Determinism tests are owned by Spec #16 §7; #19 consumes them as a required layer and does not redefine them. (KD-2) | MUST | Stage 0+1 |
| FR-TS-006 | End-to-end / soak tests run ≥ one full 90-minute match of in-game time. | MUST | Stage 0+1 |
| FR-TS-007 | The pyramid contract (unit ≥ 60%, integration ≤ 25%, simulation ≤ 12%, e2e/soak ≤ 3%) MUST be satisfied at PR-merge time once activated. Determinism layer is counted separately. | MUST | Stage 0+1 |
| FR-TS-008 | Tests MUST be named per §3.1.4 (`unit_<system>_<behaviour>`, `int_<A>_<B>_<behaviour>`, `sim_<scenario>`, `e2e_<scenario>`). | MUST | Stage 0+1 |
| FR-TS-009 | A per-spec §5 MAY declare tighter lower bounds on its own layer mix; it MUST NOT declare upper bounds that exceed the pyramid contract. | MUST | Stage 0+1 |
| FR-TS-010 | Anti-patterns enumerated in §3.1.3 MUST be flagged at code review and MUST NOT merge. | MUST | Stage 0+1 |
| FR-TS-011 | Every CI pipeline declared in §6 MUST include Spec #16 §7's regression tiers in their canonical order (unit / integration / scenario / soak). (KD-2) | MUST | Stage 0+1 |
| FR-TS-012 | A failure in any #16 §7 tier blocks merge; Spec #19 does not soften the exit criteria. (KD-2) | MUST | Stage 0+1 |
| FR-TS-013 | Spec #19's taxonomy MUST NOT collide with #16 §7 tier names. | MUST | Stage 0 |
| FR-TS-014 | Cross-spec scenario assertions that do not depend on bitwise determinism are owned by Spec #19 §3.3, not by #16 §7. (KD-8) | MUST | Stage 0+1 |
| FR-TS-015 | Any change to #16 §7 tier names or exit criteria triggers a §3.2 review of Spec #19. | MUST | Stage 0 |
| FR-TS-016 | The #16 §7 suite is invoked through a single integration point declared in §4.3 (`ITestHarness`); duplicate entry points are forbidden. | MUST | Stage 0+1 |
| FR-TS-017 | Spec #19's functional-regression assertions on top of #16 §7 MUST be tagged so they can be disabled independently for bisection. | MUST | Stage 0+1 |
| FR-TS-018 | Spec #19 MUST NOT introduce new determinism tier categories. New categories require a #16 §7 revision. (KD-2) | MUST | Stage 0 |
| FR-TS-019 | Functional regression assertions that depend on RNG MUST route through `DeterministicRngService` (KD-7). | MUST | Stage 0+1 |
| FR-TS-020 | The boundary review obligation in FR-TS-015 MUST be recorded in §1.4 of any future Spec #19 revision. | MUST | Stage 0 |
| FR-TS-021 | Every scenario is defined by a manifest entry conforming to Appendix A's schema. | MUST | Stage 0+1 |
| FR-TS-022 | Per-spec scenarios are defined in the owning spec's §5; cross-spec scenarios are defined in Spec #19 §3 (KD-8). | MUST | Stage 0 |
| FR-TS-023 | Scenarios MUST be hermetic: no shared global state between scenario runs. | MUST | Stage 0+1 |
| FR-TS-024 | The single scenario runner entry point is `ScenarioRunner.Run(manifestPath, seed)` (§3.3.3). | MUST | Stage 0+1 |
| FR-TS-025 | Every scenario records its RNG seed in the manifest; seeds are reproducible verbatim. | MUST | Stage 0+1 |
| FR-TS-026 | Every fixture loaded by a scenario passes the §3.3.4 fixture validator against #16 §5 canonical layout (KD-10). | MUST | Stage 0+1 |
| FR-TS-027 | Scenario directory layout follows `tests/scenarios/<owning-spec>/` for per-spec and `tests/scenarios/cross-spec/` for cross-spec. | MUST | Stage 0+1 |
| FR-TS-028 | A root manifest (`tests/scenarios/index.json`) enumerates every scenario. Stage 0 deliverable: schema (Appendix A). Stage 1 deliverable: populated index. | MUST | Stage 0+1 |
| FR-TS-029 | Scenario tier classification (Tier A / B / C) MUST be assigned per #16 §1.3 at scenario creation time. (KD-9) | MUST | Stage 0+1 |
| FR-TS-030 | Scenarios MUST declare an expected-outcome envelope; "implicit pass" is forbidden. | MUST | Stage 0+1 |
| FR-TS-031 | Property tests use the framework pinned in §6.1 (Stage 0+1 selection); during Stage 0 the framework is unpinned. | MUST | Stage 0+1 |
| FR-TS-032 | Property and fuzz seed *selection* MAY be non-deterministic; the executed test body MUST route through `DeterministicRngService` (`SplitMix64`) with the selected seed. (KD-7) | MUST | Stage 0+1 |
| FR-TS-033 | The selected seed MUST be logged at start of every property / fuzz run. | MUST | Stage 0+1 |
| FR-TS-034 | A failing property / fuzz seed MUST be captured to `tests/data/captured-seeds/<spec>/<YYYY-MM-DD>-<seed>.fixture` in #16 §5 canonical format. (KD-10) | MUST | Stage 0+1 |
| FR-TS-035 | Captured seeds are re-run by Spec #19's own property / fuzz suite on every CI run until #16 §7 publishes an external-capture-hook contract that promotes them into the regression corpus. | MUST | Stage 0+1 |
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
| FR-TS-060 | Tier classification of every file under coverage measurement MUST be sourced from #16 §1.3; #19 does not redefine it. | MUST | Stage 0+1 |
| FR-TS-061 | A test is "flaky" if two runs of the same revision under the same `EnvironmentFingerprint` produce different pass/fail outcomes (cited from #16 §1.3). | MUST | Stage 0+1 |
| FR-TS-062 | CI MUST run every test twice on the same revision; disagreement triggers automatic quarantine. | MUST | Stage 0+1 |
| FR-TS-063 | Quarantined tests continue to execute but do not block merges. | MUST | Stage 0+1 |
| FR-TS-064 | Quarantine auto-expires after 14 days; expired tests MUST be either fixed or deleted. Permanent quarantine is forbidden. | MUST | Stage 0+1 |
| FR-TS-065 | A test quarantined ≥ 3 times in 90 days MUST be deleted and the rationale recorded in `tests/flake-eviction-log.md`. | MUST | Stage 0+1 |
| FR-TS-066 | Re-introduction of an evicted test requires a new test ID and a written root-cause analysis. | MUST | Stage 0+1 |
| FR-TS-067 | `[Retry]` attributes and sleep-based synchronization in tests are banned (§3.7.5). | MUST NOT | Stage 0+1 |
| FR-TS-068 | Fixtures live in `tests/data/fixtures/`; golden outputs in `tests/data/golden/`; fuzz corpora in `tests/data/corpora/`. | MUST | Stage 0+1 |
| FR-TS-069 | Every fixture file conforms to #16 §5 canonical binary layout. (KD-10) | MUST | Stage 0+1 |
| FR-TS-070 | Every fixture carries a `format-version` field; validator rejects unknown versions (no silent migration). | MUST | Stage 0+1 |
| FR-TS-071 | Every captured fixture records source seed, capturing-spec ID, capture date, and `EnvironmentFingerprint` at capture. | MUST | Stage 0+1 |
| FR-TS-072 | Fixtures whose owning test is deleted MUST be deleted in the same commit (no orphan fixtures). | MUST | Stage 0+1 |
| FR-TS-073 | LFS / no-LFS storage decision for fixtures is deferred to D5 (§7.5). | MAY | Stage 0+1 |
| FR-TS-074 | Cross-fixture provenance edges (e.g., scenario A reuses fixture B) MUST be declared in the manifest (Appendix A). | MUST | Stage 0+1 |
| FR-TS-075 | Three pipelines are mandatory: pre-commit (unit + property), PR (unit + integration + property + per-spec-changed scenarios), nightly (full simulation + soak + #16 §7 full suite). See §4.5. | MUST | Stage 0+1 |
| FR-TS-076 | Functional-gate failure blocks merge (Spec #19 authority); performance-gate failure blocks merge (Spec #18 authority); determinism-gate failure blocks merge (#16 §7 authority). | MUST | Stage 0+1 |
| FR-TS-077 | No gate is "soft"; flake quarantine (§3.7) is the only escape valve and applies only to functional gates. | MUST | Stage 0+1 |
| FR-TS-078 | CI provider selection criteria are declared in §6.1 (L4); the final pin lands in `src/CLAUDE.md`. | MUST | Stage 0+1 |
| FR-TS-079 | Until CI activates, the same gate composition runs locally via `tools/run-tests-local.sh` (Appendix E). | MUST | Stage 0 |
| FR-TS-080 | Spec #19 cites #18 §4 thresholds by reference; #19 MUST NOT republish performance numbers. (KD-3) | MUST NOT | Stage 0+1 |
| FR-TS-081 | Defects are classified per §6.4.1 (spec / implementation / test / determinism). Misclassified defects are themselves a procedural violation. | MUST | Stage 0+1 |
| FR-TS-082 | PR-blocking failures are investigated within 24 hours; quarantined tests are reviewed weekly; spec defects are reviewed at next spec-revision cycle. | MUST | Stage 0+1 |
| FR-TS-083 | Defect severity uses the four-level scale in §6.4.3 (Critical / High / Medium / Low). | MUST | Stage 0+1 |
| FR-TS-084 | Every defect MUST cite the FR it violated; uncited defects are themselves a procedural violation. | MUST | Stage 0+1 |
| FR-TS-085 | Determinism defects are routed to #16 §7's process; Spec #19's triage is bypassed for that class. (KD-2) | MUST | Stage 0+1 |

## 2.3 Failure-to-Comply Modes

Five modes apply, in increasing order of severity:

- **Review block.** The PR cannot merge. Applies once the relevant CI
  gate activates.
- **Quarantine.** The test moves to the flake-quarantine pool with
  14-day auto-expiry; merges are not blocked while the test is in the
  pool (§3.7).
- **Refactor required.** The PR merges with a follow-up issue filed
  against the spec or test; severity per §6.4.3.
- **Exception with sign-off.** Recorded in `tests/exceptions.md` with
  rationale, FR cited, and expiry trigger; expires at next test-suite
  refactor.
- **Spec-§5 nonconformance.** A per-spec §5 fails Spec #19's schema
  check at draft-review time; the spec cannot reach APPROVED status
  until conformance is restored (§5.4). For approved specs #1–#8 the
  §3.5.4 acknowledged-dilution policy applies.

## 2.4 Data Structures (informational)

Spec #19 defines **no runtime data structures consumed by gameplay**.
The test-harness data structures it declares are:

- **Scenario manifest** — JSON-schema declaration in Appendix A;
  on-disk binary embeddings of fixtures conform to KD-10.
- **Fixture index** — root manifest enumerating every fixture under
  `tests/data/`; schema in Appendix A.
- **Flake ledger** — append-only record of every quarantine event,
  expiry, and eviction; schema in §3.7 and Appendix A.
- **Captured-seed corpus** — `tests/data/captured-seeds/` holding area
  for fuzz / property failures; format conforms to KD-10 (§3.4.3).

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
- **Boundary drift with #16 §7** — caught by FR-TS-015 boundary review
  obligation any time #16 §7 changes.
- **Boundary drift with #18 §4 / §7** — caught at §6.2 gate-composition
  review when #18 publishes a draft that changes section numbering.

## 2.6 Version History

| Version | Date         | Author      | Notes |
|---------|--------------|-------------|-------|
| 0.1     | May 12, 2026 | Claude Code | Initial draft from `outline-detailed.md` v1.1. FR-TS-001 … 085 enumerated; partition table aligns to §3 mechanics. |
