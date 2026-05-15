# Testing Strategy & Framework Specification #19 — Section 4: Architecture & Integration

**Created:** May 12, 2026
**Last Updated:** May 12, 2026
**Purpose:** The *shape* of the test-harness architecture: folder
layout, fixture / golden-trace layout, harness API surface, the two
interfaces this spec exposes, and the CI pipeline topology. Concrete
paths and runner invocations land in `src/CLAUDE.md` (deferred per
KD-5).

---

## 4.1 `tests/` Folder Layout (Shape, Not Concrete Paths)

Convention: one `tests/` folder per Stage 0 spec, sibling of
`src/<spec>/`. Layout matches Spec #20 §4.1 dependency-arrow shape.

```
src/<spec>/
tests/<spec>/
├── unit/             ← FR-TS-002 unit tests
├── integration/      ← FR-TS-003 integration tests
├── simulation/       ← FR-TS-004 simulation tests
└── properties/       ← §3.4 property and fuzz tests

tests/scenarios/
├── <owning-spec>/    ← per-spec scenarios (KD-8)
├── cross-spec/       ← Spec #19-owned cross-spec scenarios
└── index.<ext>       ← root manifest (§3.3.6); <ext> pinned Stage 0+1

tests/data/           ← §3.8.2 storage layout
tests/shared/         ← read-only harness utilities (NOT game-state assemblies)
tests/exceptions.md   ← exception ledger (FR-TS-012-style sign-offs)
tests/coverage-exemptions.md  ← coverage exemptions (§3.6.5)
tests/flake-eviction-log.md   ← §3.7.4 eviction record
tests/test-defect-log.md      ← §6.4.1 test-defect log
```

**Rule.** Game-state assemblies under `src/` MUST NOT reference
`tests/shared/`. The harness lives below the test surface; the
dependency direction is one-way (test → src), never reversed.

## 4.2 Fixture & Golden-Trace Layout

Concrete layout per §3.8.2:

```
tests/data/
├── fixtures/         ← in-repo small fixtures
├── golden/           ← golden outputs for replay assertions
├── corpora/          ← fuzz corpora (LFS-tracked, pending D5)
├── captured-seeds/   ← §3.4.3 holding area
├── run-logs/         ← Stage 0+1 CI run logs
└── migrations/       ← format-version migration scripts (§3.8.3)
```

Format conforms to #16 §3.2.4.1 (`SerializeCanonical`
normative byte-level schema; KD-10). Index / manifest schema in
Appendix A.

## 4.3 Harness API Surface

### 4.3.1 `ITestHarness`

Consumed by per-spec test runners. **Single concrete implementation;
no IoC container in test code** (parallel to Spec #20 §3.5.5
anti-pattern list).

```
interface ITestHarness {
  ScenarioResult RunScenario(string manifestPath, ulong seed);
  CoverageReport CollectCoverage();
  DeterminismSuiteResult RunDeterminismTiers();   // delegates to #16 §5
  FlakeStatus QueryFlakeStatus(string testId);
}
```

- `RunDeterminismTiers()` is the single integration point through
  which #16 §5 is invoked (FR-TS-016). Duplicate entry points are
  forbidden.
- `CollectCoverage()` returns the per-tier breakdown consumable by
  §5.5 (FR-TS-057).
- `QueryFlakeStatus` reads from the flake ledger (§2.4).

### 4.3.2 Assertion Helpers

- `AssertBitwise(snapshot, golden)` — Tier A assertions; routes
  through #16 §5 bitwise comparison.
- `AssertWithinTolerance(actual, expected, toleranceRow)` — Tier B
  assertions; `toleranceRow` sourced from #16 §3.4.2 (Tier B
  comparator default policy).
- `AssertEnvelope(actual, envelope)` — Tier C / functional-regression
  assertions; the predicate set comes from the scenario manifest
  (§3.3.2).

### 4.3.3 Scenario Runner Contract

Per §3.3.3. Single entry point `ScenarioRunner.Run(manifestPath,
seed)`. The harness exposes `RunScenario` as the thin pass-through.

## 4.4 Interface Contracts (This Spec Exposes)

Per CLAUDE.md "Interface Design Principle": interfaces are declared
only when both producer and consumer are specified.

### 4.4.1 `IScenario`

- **Producer:** scenario authors (per-spec §5 and Spec #19 §3
  cross-spec scenarios).
- **Consumer:** `ScenarioRunner` (§3.3.3).
- Single method: `ScenarioResult Run(ulong seed)`.
- Both sides specified in this spec → declaration permitted.

### 4.4.2 `IFixtureValidator`

- **Producer:** fixture-format owners. Concretely: one
  `IFixtureValidator` implementation per `format_version`. The
  upstream byte-level layout is owned by #16 §3.2.4.1
  (`SerializeCanonical`).
- **Consumer:** `ScenarioRunner` fixture-load step (§3.3.4).
- Method: `ValidationResult Validate(byte[] fixtureBytes, int
  formatVersion)`.
- **Phantom-interface risk acknowledgement (historical; resolved
  May 14, 2026).** The producer's contract depends on #16 §3.2.4.1,
  which was `TBD-NORMATIVE` at #19 IN REVIEW time but is now firm
  (#16 reached Tier 2 `APPROVED` May 14, 2026). #16 §3.2.4.1 has been
  through five adversarial passes and is structurally stable (only
  subsection numbering may
  shift), so the byte-level layout is treated as "sufficiently
  specified" for interface-declaration purposes per the CLAUDE.md
  Interface Design Principle. If #16 §3.2.4.1 reverts to an
  outline-level state, this interface MUST be deferred to Stage 1
  alongside `IFlakeReporter` (§4.4.3); that demotion is a §3.2 review
  trigger (FR-TS-015). Compare with `IFlakeReporter`, where the
  consumer is genuinely unspecified at Stage 0.

Both interfaces live in `tests/shared/` per §4.1; no game-state code
may reference them.

### 4.4.3 `IFlakeReporter` — Intentionally NOT Declared

Per CLAUDE.md "Interface Design Principle" (only declare interfaces
when both sides are specified — ERR-001 / ERR-004 hazard), the CI
integration layer that would consume `IFlakeReporter` is unspecified
at Stage 0. The interface is deferred to §7.2 Stage 1 deliverables
and is declared in `src/CLAUDE.md` (or a Stage 1 CI spec) only after
the consumer is concretely specified.

## 4.5 CI Pipeline Topology (Shape Only)

Concrete provider configuration is Stage 1+ deliverable; this
subsection declares the *shape* (FR-TS-075).

### 4.5.1 Pre-Commit Pipeline

- Trigger: local `git commit` hook.
- Tiers: unit + property (fast).
- Exit criteria: all unit tests pass; property tests run a default of
  100 cases per property (configurable in Stage 0+1).
- Wall-time budget: ≤ 60 seconds on the certified host (`[GT]`,
  catalogued in §3.10; revisited Stage 1).

### 4.5.2 PR Pipeline

- Trigger: PR open / push.
- Tiers: unit + integration + property + per-spec-changed scenarios.
- "Per-spec-changed" = the diff touches `src/<spec>/`; the PR
  pipeline runs `tests/scenarios/<spec>/` plus any cross-spec
  scenario whose `owning_spec_ids` includes that spec.
- Exit criteria: all gates pass per §6.2 composition rule.

### 4.5.3 Nightly Pipeline

- Trigger: scheduled nightly.
- Tiers: full simulation tier + soak + #16 §5 full determinism suite
 .
- Exit criteria: all tiers pass; soak completes ≥ one full 90-minute
  in-game match.

### 4.5.4 Pipeline Sequencing Diagram

```
trigger → load fixtures → tier → exit criteria
  │
  ├── pre-commit:  unit → property → pass/fail
  ├── PR:          unit → integration → property → scenarios → pass/fail
  └── nightly:     simulation → soak → #16 §5 tiers → pass/fail
```

Concrete CI provider selection is deferred to Stage 0+1 (§6.1, D4 in
§7.5).

## 4.6 Pointer to `src/CLAUDE.md`

Concrete paths, runner invocations, and CI provider configuration land
in `src/CLAUDE.md` when coding begins. Spec #19 declares the *shape*;
`src/CLAUDE.md` declares the *paths*. The deferred items are:

- Exact runner invocation (e.g., `dotnet test`, `nunit-console`).
- CI provider configuration file location and syntax.
- Coverage tool invocation and report path.
- Pre-commit hook installation procedure.
- `IFlakeReporter` declaration (§4.4.3 deferral).

## 4.7 Version History

| Version | Date         | Author      | Notes |
|---------|--------------|-------------|-------|
| 0.1     | May 12, 2026 | Claude Code | Initial draft from `outline-detailed.md` v1.1. `ITestHarness`, `IScenario`, `IFixtureValidator` declared; `IFlakeReporter` explicitly deferred. |
| 0.2     | May 12, 2026 | Claude Code | Self-critique sweep. #16 §7 → §5 throughout; #16 §5 → §3.2.4.1 (canonical schema, §4.2); tolerance-matrix citation pinned to #16 §3.4.2 (§4.3.2). M2 `index.<ext>` notation. M3 `IFixtureValidator` phantom-interface judgment made explicit (§4.4.2). L3 §3.10 cross-reference added to pre-commit budget (§4.5.1). |
