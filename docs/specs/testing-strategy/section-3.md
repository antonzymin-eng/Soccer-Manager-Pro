# Testing Strategy & Framework Specification #19 — Section 3: Technical Specification (Rule Mechanics)

**Created:** May 12, 2026
**Last Updated:** May 17, 2026 (v0.3 patch: §3.1.4 spec-local test-requirement identifier table added; T-C- and T-X- from Pressing AI #13 §5 bound to Simulation layer per OI-010 / §9.3 (h))
**Purpose:** Mechanics of every rule named in §2.2. Each subsection
cites the FR-TS-### IDs it implements and provides the *mechanics*; it
does not redefine the rule statement. Section ordering mirrors the
FR-catalogue partition in §2.2.

> **`TBD-NORMATIVE` tagging (resolved May 15, 2026).** Citations of Spec
> #16 and Spec #18 were originally tagged per KD-2 /
> KD-3 status caveats while the upstream specs were `IN PROGRESS` /
> `NOT STARTED`. Spec #16 reached Tier 2 `APPROVED` on May 14, 2026;
> Spec #18 reached `IN REVIEW` (section files at v0.3) on May 14, 2026.
> All tags below have been swept and replaced with
> firm citations to the now-stable upstream sections, satisfying
> §9.3.8 / §9.2.6 and KD-2 precondition (c). See the §3.x version
> history below for the sweep entry.

---

## 3.1 Test Taxonomy & Pyramid Contract (FR-TS-001 … 010)

### 3.1.1 Five-Layer Taxonomy

The taxonomy is exhaustive and mutually exclusive. Every executable
test belongs to exactly one layer (FR-TS-001).

1. **Unit.** Exercises a single struct or method. No heap allocation
   in the assertion body. Sub-millisecond wall-time on the certified
   host (≤ 1 ms `[GT]`, see §3.10). No file I/O, no Unity scene, no
   scenario manifest.
2. **Integration.** Wires two to five subsystems together without
   instantiating a Unity scene. May allocate. Wall-time budget set
   per-test, not globally; typical bound ~10 ms.
3. **Simulation.** Invokes the full subsystem stack under a scripted
   scenario. Rendering is disabled. Loads scenario fixtures via the
   §3.3 runner.
4. **Determinism.** Owned by Spec #16 §5. Consumed
   by Spec #19 as a required layer (KD-2). Listed here for
   completeness; mechanics are not restated.
5. **End-to-end / soak.** Long-horizon runs (≥ one full 90-minute
   match of in-game time; `90 min` `[FIXED]` per §3.10). Primarily a
   determinism + performance vehicle; functional assertions are
   coarse-grained.

### 3.1.2 Pyramid Contract

Stage-gated per KD-5 (activates at Stage 0+1).

- Unit ≥ 60% of test count. `[GT]`
- Integration ≤ 25%. `[GT]`
- Simulation ≤ 12%. `[GT]`
- End-to-end / soak ≤ 3%. `[GT]`
- Determinism layer is counted separately (owned by #16); not part of
  the pyramid percentages.

**Bound semantics.** These are *ceilings* on integration / simulation
/ e2e and a *floor* on unit. A suite that is 100% unit and 0% else
satisfies the arithmetic; that outcome is intentional — the pyramid
contract guards against top-heavy suites, not against bottom-heavy
ones. Per-spec §5 sections MAY declare tighter lower bounds locally if
subsystem maturity warrants (FR-TS-009).

**Stage 1 review.** Numeric thresholds are revisited at the Stage 1
first-real-code milestone against actual code (parallel to Spec #20
§5.3). The `[GT]` tags above reflect that designer-tunability.

### 3.1.3 Anti-Patterns (FR-TS-010)

Flagged at code review; MUST NOT merge:

- **Integration test masquerading as unit.** Allocates, touches more
  than one subsystem, or exceeds the unit time budget but lives under
  `tests/<spec>/unit/`.
- **"Simulation" test asserting on a single physical quantity.** That
  test belongs in `tests/<spec>/unit/` against the relevant struct.
- **Per-spec §5 declaring layer percentages that contradict the
  pyramid contract.** Caught by §5.4 schema-conformance auditor.
- **End-to-end test asserting on bitwise determinism without invoking
  the #16 §5 suite.** Determinism assertions belong in the #16-owned
  layer (KD-2).
- **Property test classified as "simulation".** Property tests live
  under `tests/<spec>/properties/` regardless of the layer they target;
  classification by mechanism, not by output.

### 3.1.4 Naming Convention (FR-TS-008)

- `unit_<system>_<behaviour>` — e.g., `unit_ballphysics_aerodynamic_drag`.
- `int_<systemA>_<systemB>_<behaviour>` — e.g.,
  `int_passmechanics_collision_first_touch`.
- `sim_<scenario>` — e.g., `sim_corner_kick_set_piece`.
- `e2e_<scenario>` — e.g., `e2e_90min_match_baseline`.
- Determinism tests use the #16 §5 naming; cited,
  not restated.
- Property tests use `prop_<system>_<property>` (no layer prefix; the
  property tests in `tests/<spec>/properties/` carry the layer in the
  folder, not in the name). Worked examples in Appendix B follow this
  form.

**Spec-local test-requirement identifiers.** Per-spec §5 sections use
spec-local `T-<category>-NNN` identifiers (e.g., `T-U-001`, `T-C-001`)
as requirement references, distinct from the executable test file names
above. These IDs name *what* must be tested; the file-name convention
governs the *artefact*. Mapping to the five-layer taxonomy:

| Prefix | Category | Layer | Notes |
|---|---|---|---|
| `T-U-` | Unit | Unit | — |
| `T-I-` | Integration | Integration | — |
| `T-S-` | Simulation (general) | Simulation | — |
| `T-C-` | Anti-chaos (Simulation sub-category) | Simulation | Pressing AI #13 §5 / KD-16 three measurable invariants |
| `T-X-` | Exploit-resistance (Simulation sub-category) | Simulation | Pressing AI #13 §5 / KD-17 four-exploit corpus |
| `T-E-` | End-to-end / soak | End-to-end / soak | — |

`T-C-` and `T-X-` executable test files use the `sim_<scenario>`
file-name convention. The spec-local ID is the requirement reference;
the file name is the artefact. Other specs MAY declare additional
sub-category prefixes within the Simulation layer by adding rows to
a table of this form in their own §5 preamble — no #19 amendment
required, provided the prefix does not collide with existing entries above.

## 3.2 Determinism-Suite Consumption (FR-TS-011 … 020)

### 3.2.1 Citation and Authority

Spec #16 §5 is the authoritative owner of the
determinism regression suite. Spec #19 consumes the suite; KD-2
binding.

### 3.2.2 Spec #19's Obligations Toward #16 §5

- Every CI pipeline declared in §6 MUST include #16 §5's regression
  tiers in their canonical order (unit / integration / scenario /
  soak) (FR-TS-011).
- Failures in any #16 tier block merges; Spec #19 does not soften or
  override #16's exit criteria (FR-TS-012).
- Spec #19's own test taxonomy MUST NOT collide with #16 tier names
  (FR-TS-013); §3.1.4 already disambiguates by prefix.
- The #16 §5 suite is invoked through a single integration point
  (`ITestHarness`, §4.3); duplicate entry points are forbidden
  (FR-TS-016).
- Spec #19's functional-regression assertions layered on top of the
  determinism suite MUST be tagged so they can be disabled
  independently for bisection (FR-TS-017).
- Spec #19 MUST NOT introduce new determinism tier categories
  (FR-TS-018); new categories require a #16 §5 revision.

### 3.2.3 Spec #19's Additions on Top of #16 §5

- Functional / behavioural regression assertions that don't depend on
  bitwise determinism. Example: "shot-on-target rate stays within
  designer-tuned envelope across N seeds" — owned by Spec #19, not by
  #16.
- Cross-spec scenario assertions (KD-8); see §3.3.
- Property and fuzz assertions over seed corpora (§3.4). Failed seeds
  are captured into the #19-owned holding area (§3.4.3), not directly
  into the #16 §5 corpus.

### 3.2.4 Boundary Review Obligation

Any change to #16 §5 that affects tier names or exit criteria triggers
a Spec #19 §3.2 review (FR-TS-015). The trigger is recorded in §1.4's
dependency list. Boundary drift is enumerated in §2.5 failure modes.

## 3.3 Scenario Library Architecture (FR-TS-021 … 030)

### 3.3.1 Source-of-Truth Rule (KD-8)

- **Per-spec scenarios** are defined in the owning spec's §5
  (FR-TS-022). Example: a corner-kick scenario validating set-piece
  defensive positioning is owned by Defensive AI's §5.
- **Cross-spec scenarios** are defined in Spec #19 §3 and stored in
  `tests/scenarios/cross-spec/`. Example: a full-match smoke test
  exercising Specs #1–#8 jointly. Authorship: Spec #19.

### 3.3.2 Scenario File Format

Each scenario carries the following fields (full schema in Appendix A;
the list below is normative for review):

- `name` — string, kebab-case.
- `owning_spec_ids` — list of spec numbers (one for per-spec, ≥ 2 for
  cross-spec).
- `seed` — `uint64`, recorded verbatim (FR-TS-025).
- `expected_outcome_envelope` — bounded predicate set; "implicit pass"
  is forbidden (FR-TS-030).
- `tier_classification` — Tier A / B / C per #16 §1.1.1
  (FR-TS-029).
- `fixture_refs` — list of fixture paths under `tests/data/fixtures/`.
- `format_version` — integer, validated by §3.3.4.
- `provenance_edges` — optional list of upstream scenarios this scenario
  derives from (FR-TS-074).

### 3.3.3 Runner Contract (FR-TS-024)

Single entry point. Hermetic.

```
ScenarioRunner.Run(manifestPath: string, seed: uint64) -> ScenarioResult
```

- `ScenarioResult` is a structured value: `{ status: Passed | Failed |
  Quarantined, diagnostics: MachineReadable, durationMs: int,
  fingerprint: EnvironmentFingerprint }`.
- No global state. Every invocation re-initialises subsystem state
  from the fixtures named in the manifest (FR-TS-023).
- The seed parameter is passed verbatim to `DeterministicRngService`
  before any subsystem is initialised (KD-7).
- The runner does not write to disk except for `diagnostics`; capture
  of failed seeds (§3.4.3) is performed by the property / fuzz harness,
  not by the scenario runner itself.

### 3.3.4 Fixture Validator (KD-10)

Every fixture file is checked against #16 §3.2.4.1
(`SerializeCanonical` normative byte-level schema)
at load time:

- Validator is implemented as `IFixtureValidator` per format version
  (§4.4).
- Drift fails the test (FR-TS-026); silent acceptance is forbidden.
- Unknown `format_version` values are rejected (FR-TS-070).
- Validator runs before any subsystem state is initialised; a
  validator failure is a load-time error, not a runtime assertion.

### 3.3.5 Directory Layout (FR-TS-027)

> **Provisional file extension.** The root-manifest filename is
> written as `index.<ext>` throughout this spec because the final
> extension (and therefore on-disk encoding — JSON vs. JSON5 vs.
> binary) is pinned at Stage 0+1 (D1 in §7.5). Normative occurrences
> are at: §3.3.5 (here), §3.3.6, §4.1, §4.5, §7.2, FR-TS-028, and
> Appendix A.2. The illustrative example in Appendix A.2 uses `.json`
> syntax for readability.

```
tests/scenarios/
├── ball-physics/         ← per-spec, owned by #1
├── agent-movement/       ← per-spec, owned by #2
├── …                     ← one folder per owning spec
├── cross-spec/           ← owned by Spec #19 (KD-8)
└── index.<ext>           ← root manifest (FR-TS-028); <ext> pinned Stage 0+1
```

### 3.3.6 Scenario Index / Manifest (FR-TS-028)

- Single root manifest at `tests/scenarios/index.<ext>` (extension
  pinned at Stage 0+1 alongside the test-runner pin in §6.1).
- Stage 0 deliverable: schema only (Appendix A).
- Stage 1 deliverable: populated index covering every scenario in the
  scenario folders.
- Manifest is the input to `ScenarioRunner.Run`; the runner refuses to
  execute a scenario whose entry is missing from the index.

## 3.4 Property & Fuzz Testing (FR-TS-031 … 039)

### 3.4.1 Framework Selection

- Property tests use FsCheck or an equivalent C# property-based
  framework. Final pin deferred to Stage 0+1 (§6.1) — tracked as D2 in
  §7.5.
- Fuzz tests use a structured fuzzing harness. Coverage-guided
  (AFL-style) fuzzing is a Stage 1+ posture decision tracked as D8 in
  §7.5 (FR-TS-039).
- The Stage 0 disclaimer is intentionally minimal: framework pinning
  cannot precede the broader Stage 0+1 tool slate.

### 3.4.2 Seed Governance (KD-7)

- Property / fuzz seeds MAY be selected non-deterministically *for the
  selection step only* (FR-TS-032).
- The executed test body MUST route through `DeterministicRngService`
  (`SplitMix64`) with the selected seed.
- The selected seed MUST be logged at the start of each run
  (FR-TS-033). Log location: stdout for local runs;
  `tests/data/run-logs/` for CI runs (Stage 0+1).

### 3.4.3 Failed-Seed Capture (read-only boundary with #16 §5)

Per KD-2, Spec #19 **does not** write directly into Spec #16 §5's
regression suite. #16 §5 is the sole authority for what enters its
regression corpus.

**Capture mechanics.**

- Every failing fuzz or property seed is captured into a Spec
  #19-owned holding area at
  `tests/data/captured-seeds/<spec>/<YYYY-MM-DD>-<seed>.fixture`
  (final path pinned at Stage 0+1) (FR-TS-034).
- Capture format conforms to KD-10 (canonical save format from #16 §5
 ).
- Each captured fixture records: source seed, capturing-spec ID,
  capture date, `EnvironmentFingerprint` at capture (FR-TS-071).

**Promotion path.**

- #16 §5 SHOULD publish an "external capture hook" contract that
  periodically (cadence TBD by #16) pulls from #19's holding area into
  the #16 §5 regression corpus.
- Until that hook is published in #16 §5, captured seeds remain in
  the #19 holding area and are re-run by #19's own property / fuzz
  suite on every CI run (FR-TS-035) — a one-time fuzz hit still
  becomes a permanent #19-side guardrail.
- Cross-spec dependency: the promotion path consumes #16 §5's
  external-capture-hook surface. #16 §5 is APPROVED (Tier 2, May 14,
  2026); if the hook contract is not yet published in §5's current
  text, the binding re-opens for revision once #16 publishes it
  (Stage 0+1 deliverable per #16 §7.2 Deferred Decisions).

### 3.4.4 Property Catalogue (Categorical)

Full enumeration in Appendix B; categories named here:

- **Physics invariants.** Energy non-increase under collision;
  conservation laws where applicable. Tier A.
- **State-machine reachability.** No orphaned states; every declared
  state is reachable. Tier A.
- **Idempotence.** Snapshot → load → snapshot = original. Tier A.
- **Commutativity / associativity.** Parallel reductions, deterministic
  aggregations. Tier B per KD-9 (mid-tier numerical tolerance allowed).
- **Boundary saturation.** Values at coordinate-system bounds (per
  the CLAUDE.md coordinate convention) do not produce NaN /
  Infinity. Tier A.
- **Monotonicity.** Fatigue (per CLAUDE.md fatigue convention) is
  non-decreasing across a match in absence of recovery events.
  Tier B.

### 3.4.5 Anti-Patterns

- Property test using `System.Random` directly (FR-TS-036; parallels
  Spec #20 §3.4.2).
- Property test asserting on a wall-clock-derived value (banned by
  CLAUDE.md "When Writing Code"; FR-TS-019 binding).
- Fuzz test that runs without recording its seed (FR-TS-037).

## 3.5 Programmatic-Verification Mandate (FR-TS-040 … 045) and Per-Spec §5 Conformance (FR-TS-046 … 052)

### 3.5.1 Mandate Statement (KD-6)

Every approval-checklist row in every spec MUST resolve to either:

- **(a)** a named, version-controlled file path containing the
  claimed value; or
- **(b)** a programmatic check (script / test / linter) whose output
  is captured.

Prose evidence is non-conformant (FR-TS-041).

### 3.5.2 Verification Mechanics

- **Stage 0 (manual).** A reviewer (the "checklist auditor") walks
  every approval-checklist row and resolves each citation against the
  current repo state. Output is appended to the PR description.
- **Stage 0+1 (automated).** `tools/checklist-auditor.py` (final
  language pin parallels CLAUDE.md "When Writing Code"; Python tooling
  rules apply) parses checklist tables, resolves cited file paths,
  invokes named programmatic checks, and emits a structured report
  (FR-TS-043).
- Unresolved citations block APPROVED status (FR-TS-042); binds to
  `SPEC_INDEX.md` status transitions.

### 3.5.3 Per-Spec §5 Conformance Schema (FR-TS-046 … 052)

Every per-spec §5 MUST contain:

- **Test count by taxonomy layer** (FR-TS-046).
- **Property test list** with property names and tier classification
  (FR-TS-047).
- **Scenario list** with manifest paths (FR-TS-048).
- **Coverage targets** per-tier per KD-9 (FR-TS-049).
- **Determinism-tier classification** of every authoritative field
  referenced (FR-TS-050).
- **Approval-checklist linkage** — pointer to the §9 row each test
  verifies (FR-TS-051).

Paste-ready schema is in Appendix C. Schema-conformance check at
review time (FR-TS-052) is performed by §5.4.

### 3.5.4 Migration Policy for Approved Specs (KD-4) and Acknowledged KD-6 Dilution

- **Migration.** Approved specs (#1–#8) are not forcibly re-opened
  (KD-4). Their §5 sections are surveyed against the schema
  (Appendix D); gaps are recorded in
  `docs/tracking/spec-error-log.md` as `ERR-019-NNN` rows;
  remediation happens at the next natural revision of each spec.
- **Acknowledged dilution.** This migration policy is in tension with
  KD-6. Specs #1–#8 were approved before KD-6 existed, and KD-4
  explicitly forbids re-opening them. Net effect: KD-6 is
  **unenforced retroactively** for the eight specs where
  fabrication-class findings (the CLAUDE.md "fabricated checklist
  values" hazard pattern, recorded in `spec-error-log.md` under the
  `ERR-019-NNN` namespace once surveyed) are statistically most likely
  to already exist. This is a *known dilution*, not a migration
  technicality (FR-TS-045).
- **Mitigation.** The Appendix D survey enumerates every unresolved
  row as an `ERR-019-NNN` entry so the dilution is *visible* even
  when not *enforced*. Full enforcement reaches each of #1–#8 only at
  that spec's next natural revision.

### 3.5.5 Anti-Patterns

- Approval-checklist row whose "evidence" is prose without a file
  path or check name (the CLAUDE.md "fabricated checklist values"
  hazard; FR-TS-041).
- Per-spec §5 declaring tests that do not exist in `src/`.
- Coverage claim without a coverage-report artifact (Stage 0+1).

## 3.6 Coverage Targets — Per-Tier Policy (FR-TS-053 … 060)

### 3.6.1 Tier Vocabulary

Tier vocabulary is owned by #16 §1.1.1 ("Equivalence policy by
artifact") and is not restated here (KD-1).

**Cite-precision guard.** The subsection number "§1.1.1" was
re-grepped against current `deterministic-sim/section-1.md` on May
12, 2026; the tier-classification table is at §1.1.1 (not §1.3.1 as
the v1.1 outline had it). Any section-3 author MUST re-grep
`deterministic-sim/section-1.md` for the tier-classification block at
each revision and update the cited number atomically. The same guard
applies to every #16 citation of §3.2.4.1 (canonical schema), §4.8
(`EnvironmentFingerprint`), and §5 (regression suite) in this spec.

### 3.6.2 Targets (Stage-Gated per KD-5)

- **Tier A (authoritative hard).** ≥ 98% line, ≥ 95% branch. `[GT]`
- **Tier B (bounded-authoritative).** ≥ 90% line, ≥ 80% branch. `[GT]`
- **Tier C (non-authoritative).** Lint-only; no numeric target.
- Test code itself: NOT counted (FR-TS-056).

### 3.6.3 Coverage Tool

Selection deferred to Stage 0+1 (§6.1); tracked as D3 in §7.5. The
selection criterion is that the tool MUST emit a per-tier breakdown
consumable by the §5.5 auditor (FR-TS-057).

### 3.6.4 Reporting Cadence (FR-TS-058)

- Stage 0+1: per-PR delta only.
- Stage 1: absolute per-tier dashboard.

### 3.6.5 Coverage Exemption Procedure (FR-TS-059)

- Lead-developer sign-off required.
- Recorded in `tests/coverage-exemptions.md` (Stage-1 artifact) with
  rationale, FR cited, and expiry trigger.
- Exemption expires at the next refactor of the affected file.

## 3.7 Flake Handling (FR-TS-061 … 067)

> **Stage-gated per KD-5.** Every rule in §3.7 is a contract that
> activates at the Stage 0 → Stage 1 transition. Until CI exists,
> there is nothing to flake. The §3.7.3 "14-day auto-expiry", §3.7.4
> "≥3 quarantines in 90 days = eviction", and §3.7.2 "CI runs every
> test twice" rules all presume the CI integration layer enumerated in
> §7.2. Per-FR activation status is recorded in §5.2 Stage-Gated
> Activation Table; the corresponding FR-TS-061 … 067 rows read
> "Activation stage: Stage 0+1, criterion: CI integration layer
> specified per §7.2."

### 3.7.1 Definition (FR-TS-061)

A test is **flaky** if two runs of the same revision under the same
`EnvironmentFingerprint` produce different pass / fail outcomes. This
is a determinism-adjacent definition cited from #16 §4.8
(`EnvironmentFingerprint`).

### 3.7.2 Detection (FR-TS-062)

- CI runs every test twice on the same revision.
- Disagreement between runs → automatic quarantine.
- A test that passes both runs but produces non-bitwise-identical
  outputs in #16 §5 tiers is a determinism defect (§6.4.1), not a
  flake.

### 3.7.3 Quarantine Pool (FR-TS-063, FR-TS-064)

- Quarantined tests continue to execute but do not block merges.
- Auto-expiry: 14 days. `[GT]`
- After expiry, the test MUST be either fixed or deleted; "permanent
  quarantine" is forbidden.
- Quarantine events recorded in the flake ledger (§2.4) with: test
  ID, revision, fingerprint, entry date, expiry date, rationale.

### 3.7.4 Eviction Rule (FR-TS-065, FR-TS-066)

- A test quarantined ≥ 3 times in 90 days is deleted. `[GT]`
- Eviction recorded in `tests/flake-eviction-log.md` with rationale,
  test ID, and the three quarantine events that triggered eviction.
- Re-introduction requires a new test ID and a written root-cause
  analysis (FR-TS-066). Reusing the evicted test ID is forbidden.

### 3.7.5 Anti-Patterns (FR-TS-067)

- "Flaky in CI only." Root cause is invariably an
  `EnvironmentFingerprint` violation; investigate via #16 §4.8
 .
- `[Retry]` attributes to mask flake.
- Sleep-based synchronization in tests.

## 3.8 Test-Data Governance (FR-TS-068 … 074)

### 3.8.1 Citation

KD-10 (binding to #16 §3.2.4.1 `SerializeCanonical`
normative byte-level schema).

### 3.8.2 Storage Layout (FR-TS-068)

```
tests/data/
├── fixtures/         ← small, in-repo fixtures
├── golden/           ← golden outputs for replay assertions
├── corpora/          ← fuzz corpora (LFS-tracked, pending D5)
├── captured-seeds/   ← §3.4.3 holding area for fuzz / property failures
├── run-logs/         ← Stage 0+1 CI run logs
└── migrations/       ← format-version migration scripts (§3.8.3)
```

Concrete LFS / no-LFS decision is recorded at Stage 0+1 (D5).

### 3.8.3 Versioning (FR-TS-070)

- Each fixture has a `format-version` integer field.
- Validator rejects unknown versions (no silent migration).
- Format-version bumps are paired with a corresponding migration
  script; the script lives under `tests/data/migrations/` and is
  invoked by `tools/fixture-migrate.py` (Stage 0+1 deliverable).

### 3.8.4 Provenance (FR-TS-071, FR-TS-074)

Every captured fixture records:

- `source_seed: uint64`.
- `capturing_spec_id: int`.
- `capture_date: ISO-8601`.
- `environment_fingerprint: string` (verbatim from #16 §4.8
 ).
- `provenance_edges: list<fixture_path>` — upstream fixtures this
  capture derives from.

### 3.8.5 Eviction (FR-TS-072)

Fixtures whose owning test is deleted MUST be deleted in the same
commit (no orphan fixtures). The pre-commit hook (Appendix E) walks
the diff and rejects commits that delete a test without deleting its
fixtures.

### 3.8.6 Full Mechanics

Full schema and migration mechanics are in Appendix A.

## 3.9 Edge Cases (Rule-Application Carve-Outs)

### 3.9.1 Editor-Only / Debug-Tool Tests

- SHOULD conform to taxonomy and naming.
- MAY relax §3.6 coverage targets (Tier C lint-only is acceptable).
- MUST conform to §3.4 seed governance if exercising RNG.

### 3.9.2 Benchmark / Micro-Perf Scaffolds

Outside Spec #19's scope; owned by Spec #18. Pointer
only.

### 3.9.3 Visual-Regression Tests (UI Screenshots)

- Tier C only; diagnostic, never gate.
- Framework selection deferred to D7 in §7.5.

### 3.9.4 Stage-0 Spec-Only Tests

Constants-catalogue verification scripts, cross-reference resolvers,
and the checklist auditor itself fall under "tooling tests":

- Conform to KD-6 mandate.
- Do NOT conform to §3.1 pyramid contract (which presumes runtime
  code).
- Live under `tools/tests/` rather than `src/<spec>/tests/`.

## 3.10 Constants Catalogue (Governance Metadata)

This spec declares **no physical constants**. The numeric thresholds
it publishes are *governance* values, each tagged `[GT]` with
rationale recorded inline at point of declaration.

| Constant | Value | Tag | Location | Rationale |
|---------|-------|-----|----------|-----------|
| Unit pyramid floor | ≥ 60% | `[GT]` | §3.1.2 | Standard pyramid heuristic; revisited Stage 1. |
| Integration ceiling | ≤ 25% | `[GT]` | §3.1.2 | Same. |
| Simulation ceiling | ≤ 12% | `[GT]` | §3.1.2 | Same. |
| E2E / soak ceiling | ≤ 3% | `[GT]` | §3.1.2 | Same. |
| Tier A line coverage | ≥ 98% | `[GT]` | §3.6.2 | Authoritative tier per KD-9. |
| Tier A branch coverage | ≥ 95% | `[GT]` | §3.6.2 | Same. |
| Tier B line coverage | ≥ 90% | `[GT]` | §3.6.2 | Bounded-authoritative tier per KD-9. |
| Tier B branch coverage | ≥ 80% | `[GT]` | §3.6.2 | Same. |
| Unit wall-time bound | ≤ 1 ms | `[GT]` | §3.1.1, FR-TS-002 | Sub-millisecond fast-feedback bound. |
| Quarantine auto-expiry | 14 days | `[GT]` | §3.7.3 | Two-week resolution window. |
| Eviction quarantine count | ≥ 3 | `[GT]` | §3.7.4 | Three-strikes rule. |
| Eviction window | 90 days | `[GT]` | §3.7.4 | Calendar-quarter window. |
| End-to-end / soak match length | 90 min | `[FIXED]` | §3.1.1, FR-TS-006 | Laws of football; not designer-tunable. |
| Pre-commit pipeline wall-time budget | ≤ 60 s | `[GT]` | §4.5.1 | Local-feedback budget; revisited Stage 1. |

**KD-6 evidence artifact for governance numbers.** Each `[GT]`
governance number's KD-6 evidence is the *citation line in this
spec's body text* that introduces the number — for example, the
pyramid percentages' evidence is `section-3.md §3.1.2`; the Tier A
coverage thresholds' evidence is `section-3.md §3.6.2`; the
flake-eviction window's evidence is `section-3.md §3.7.4`. The
approval-checklist auditor (§5.3) resolves these citations by
confirming the cited file path contains the literal number claimed.
No separate `tools/governance-numbers.md` file is created; the spec
body IS the evidence, and changes to a governance number are
themselves a spec revision tracked in the relevant section's
version-history table (FR-TS-044).

## 3.11 Version History

| Version | Date         | Author      | Notes |
|---------|--------------|-------------|-------|
| 0.1     | May 12, 2026 | Claude Code | Initial draft from `outline-detailed.md` v1.1. Rule mechanics for FR-TS-001 … 074; §3.10 governance constants table. |
| 0.2     | May 12, 2026 | Claude Code | Self-critique sweep. #16 §7 → §5 (regression suite); #16 §1.3.1 → §1.1.1 (tier vocabulary, §3.6.1); #16 §5 → §3.2.4.1 (canonical schema, §3.3.4 / §3.8.1); #16 §1.3 → §4.8 (`EnvironmentFingerprint`, §3.7.1). ERR-005 misnomer corrected (§3.5.4, §3.5.5). M1 coordinate restatement tightened. M2 `index.<ext>` provisional disclosure added (§3.3.5). M4 `migrations/` row added to §3.8.2. L1 / L2 inline `[GT]` / `[FIXED]` pointers (§3.1.1). L3 / L2 §3.10 expanded with `90 min [FIXED]` and `≤ 60 s [GT]`. L4 property naming reconciled (§3.1.4). |
| 0.3     | May 17, 2026 | AI agent (claude/fix-ai-specs-review-qgWFR) | OI-010 back-prop: §3.1.4 spec-local `T-<category>-NNN` identifier table added; `T-C-` (anti-chaos) and `T-X-` (exploit-resistance) bound to Simulation layer per Pressing AI #13 §5 / KD-16 / KD-17. Resolves §9.3 (h) in #13. |
