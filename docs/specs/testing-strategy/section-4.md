# Testing Strategy & Framework Specification #19 — Section 4: Architecture & Integration

**Created:** May 12, 2026
**Last Updated:** September 4, 2026
**Version:** 0.7
**Status:** AMENDMENT DRAFT (A3 candidate; May 15, 2026 approved baseline remains in force)
**Amendment plan:** `docs/planning/project-architecture-governance-integration-plan.md` v0.38, §7; A3.3/A3.4 review correction
**Purpose:** The *shape* of the test-harness architecture: folder
layout, fixture / golden-trace layout, harness API surface, the two
interfaces this spec exposes, and the CI pipeline topology. Concrete
paths and runner invocations are versioned in the repository tooling
surface and cited here.

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
└── index.<ext>       ← root manifest (§3.3.6); <ext> pinned Stage 0+1 (D9)

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

Architecture proof artifacts are governance/tooling evidence, not a new
runtime or mega-test assembly. Required executable proof remains in the
owning test surface where practical; reusable proof records use the A2
artifact contract described by §3.11 / Appendix G and are consumed by
the architecture/evidence gate through §6.2. This file does not create
an additional test ownership layer.

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
  subsection numbering may shift), so the byte-level layout is treated
  as sufficiently specified for interface-declaration purposes. If #16
  §3.2.4.1 reverts to an outline-level state, this interface MUST be
  deferred to Stage 1 alongside `IFlakeReporter` (§4.4.3); that
  demotion is a §3.2 review trigger (FR-TS-015).

Both interfaces live in `tests/shared/` per §4.1; no game-state code
may reference them.

### 4.4.3 `IFlakeReporter` — Intentionally NOT Declared

Per CLAUDE.md "Interface Design Principle" (only declare interfaces
when both sides are specified — ERR-001 / ERR-004 hazard), the CI
integration layer that would consume `IFlakeReporter` is unspecified
at Stage 0. The interface is deferred to §7.2 Stage 1 deliverables
and is declared in `src/CLAUDE.md` (or a Stage 1 CI spec) only after
the consumer is concretely specified.

## 4.5 CI Pipeline Topology

The repository carries the concrete FR-TS-075 topology as an A3
candidate. GitHub Actions is the provider; `tools/run-tests-local.sh`
is the versioned local/CI policy entry point. **Topology existence is
not by itself proof of operational conformance.** A3.4 must retain two
explicit acceptance facts: a successful incremental pre-commit
measurement within the 60-second certified-host budget, and a
successful #16 run on a registered/configured certified Windows runner.

### 4.5.1 Pre-Commit Pipeline

- Trigger: versioned local `git commit` hook at `.githooks/pre-commit`.
- Installation/bootstrap: `bash tools/bootstrap-dev.sh` configures
  `core.hooksPath=.githooks`, verifies the hook, and performs the
  one-time **cold** staged-snapshot/build-cache preparation outside the
  normal 60-second acceptance measurement. The installer refuses to
  overwrite a different pre-existing `core.hooksPath`.
- Snapshot semantics: the hook tests the Git **index** (staged
  snapshot), not unrelated unstaged working-tree edits. The snapshot
  persists under `.git/testing-strategy/precommit-snapshot`: tracked
  content is refreshed from the current index while untracked
  generated/bin/obj outputs survive for incremental reuse.
- Tiers: unit + property. Selection is expressed by
  `tools/dotnet-ci/precommit.runsettings` using NUnit METHOD-name
  prefix rules (`^int_`, `^sim_`, `^e2e_`) plus the determinism
  namespace/category exclusions. Bare FullyQualifiedName substring
  exclusions are forbidden because they over-match ordinary names such
  as `Point_`, `Fingerprint_`, `MalformedInt_`, and `QuickSim_`.
- Execution path: `tools/run-tests-local.sh --pre-commit` invokes both
  Spec #19 auditors in **survey-only** mode and then one incremental
  generated-solution `dotnet test` invocation. It skips the separate
  whole-tree meta/build pass; it does not run 34 sequential cold
  project invocations.
- Exit criteria: all selected tests pass. Survey findings remain visible
  but do not become unrelated commit blockers; checklist/schema
  blocking is attached to an explicit approval transition per §5.3/§5.4.
- Wall-time enforcement: the normal entire composition is terminated
  and fails with exit 124 if it exceeds 60 seconds. **This proves a
  hard upper bound on an attempted normal run; it does not prove the
  passing path normally completes within 60 seconds.** A3.4 acceptance
  requires a successful measured incremental run on the certified
  developer host.

### 4.5.2 PR Pipeline

- Trigger: PR open / push through `.github/workflows/ci.yml`.
- CI entry point: the workflow invokes `bash tools/run-tests-local.sh
  --pr`; it does not bypass the versioned composition by calling the
  lower-level dotnet gate directly.
- Required shape: unit + integration + property + per-spec-changed
  scenarios.
- Current implementation runs the whole non-certifying functional test
  surface as a **strict superset** of the changed-spec scenario
  requirement, plus survey-mode Spec #19 auditors and Coverlet
  collection. This avoids a false claim of exact change-selection while
  D9's root scenario-manifest encoding/index remains overdue.
- The owner-held `sim_match_engine_close_chance` RED is not quarantine:
  the blocking pass excludes that exact test `Name`, then executes that
  exact `Name` separately. The verifier requires one unambiguous result,
  the value-pinned diagnostics, failed outcome, no extra results and the
  expected test-failure runner exit. Changed diagnostics, unexpected
  pass, missing/ambiguous identity, extra returned tests, abnormal runner
  exit, or any ordinary blocking failure fails the gate.
- Exit criteria: all currently active gates pass per §6.2 composition
  rule. The architecture/evidence gate joins this topology only after
  A8 activation.

### 4.5.3 Nightly Pipeline

- Trigger: scheduled `.github/workflows/nightly.yml`.
- Non-certifying functional/simulation/soak job: GitHub-hosted Linux
  runs the full simulation surface and enables the existing
  `ShotOutcomeDiagnosticTests` full-match driver.
- Certified determinism job definition: a distinct self-hosted Windows
  runner labelled `determinism-certified` is required to execute the
  #16 §5 Unity EditMode suite on the pinned Windows 11 / Unity
  6000.4.9f1 / DX11 / Mono / x64 / SSE4.2 / one-worker deterministic
  tuple. The job verifies the platform pin before invoking Unity
  through `TD_UNITY_EXE`.
- **Availability gate:** the self-hosted job runs only when repository
  variable `DETERMINISM_CERTIFIED_RUNNER_ENABLED=true`. Until a matching
  runner is actually registered/configured, a GitHub-hosted notice job
  records the open operational condition instead of leaving a scheduled
  job queued for unavailable labels.
- **Operational boundary:** workflow definition, labels, the enable
  variable, and environment checks do not prove that the certified
  suite executed successfully. FR-TS-075's authoritative nightly
  determinism leg remains operationally unproven/non-conformant until
  an actual certified-host run passes.
- Linux evidence is explicitly **non-certifying**; it is never treated
  as the authoritative determinism host.
- Exit criteria once enabled: functional/simulation/soak checks pass,
  the owner-held RED remains exactly at its recorded diagnostic state,
  and the certified #16 suite passes on the pinned Windows host.

### 4.5.4 Pipeline Sequencing Diagram

```
trigger
  │
  ├── pre-commit:  persistent staged snapshot → anchored unit/property selection → pass within 60 s or fail
  ├── PR:          unit/integration/property + scenario superset + survey auditors + coverage → pass/fail
  └── nightly:     Linux simulation/soak + (when enabled) certified Windows #16 suite → pass/fail
```

The architecture/evidence gate described here remains inactive until
A8. The pre-commit timing acceptance and certified-runner execution
facts above remain A3.4 evidence obligations; neither is replaced by a
structural test of the workflow text.

## 4.6 Concrete Runner / Tool Pointers

- Local/CI policy composition: `tools/run-tests-local.sh`.
- Developer bootstrap and cold-cache preparation: `tools/bootstrap-dev.sh`.
- Versioned staged-index hook: `.githooks/pre-commit`.
- Anchored pre-commit selection: `tools/dotnet-ci/precommit.runsettings`.
- Non-certifying generated .NET **lower-level executor**: `tools/dotnet-ci/run-gate.sh`.
- Property framework pin: FsCheck.NUnit 2.16.6 through
  `Directory.Build.targets` (D2 resolved).
- Coverage collector pin: coverlet.collector 6.0.4 through
  `Directory.Build.targets`, configured by
  `tools/dotnet-ci/coverage.runsettings` (D3 resolved as the collector
  choice; §5.5's per-tier threshold auditor remains its own tooling
  obligation).
- PR CI: `.github/workflows/ci.yml`.
- Scheduled simulation/soak plus gated certified determinism orchestration:
  `.github/workflows/nightly.yml`.

Still deferred: `IFlakeReporter` declaration (§4.4.3) and D9's root
scenario-manifest encoding/index decision.

## 4.7 Version History

| Version | Date         | Author      | Notes |
|---------|--------------|-------------|-------|
| 0.7     | September 4, 2026 | — | **Claude review correction.** Replaces the zero-cache temporary pre-commit design with a persistent staged-index cache prepared by bootstrap; replaces unsafe FQN substring selection with anchored NUnit method-prefix rules; records routine auditor survey-only behavior, exact owner-held test identity, and certified-nightly enable gating. Operational timing/certified-host success remains unproven. |
| 0.6     | September 4, 2026 | — | **A3.3 review correction.** Stops treating timeout/workflow structure as execution proof: the pre-commit path now has a dedicated fast test-project mode and a hard 60-second failure bound, but full conformance still requires a successful measured certified-host run; the certified Windows nightly job remains operationally unproven until a registered runner actually executes it. Also records that PR CI reuses `tools/run-tests-local.sh --pr` and that unexpected owner-held-RED greens/abnormal exits are blocking. These are review/evidence corrections pending A3.4, not fresh approval. |
| 0.5     | September 4, 2026 | — | **FR-TS-075/079 implementation synchronization candidate.** Records the versioned staged-snapshot pre-commit hook/bootstrap, hard 60-second whole-composition budget, PR whole-tree scenario superset, value-pinned owner-held-RED handling, D2/D3 tool pins, Linux non-certifying nightly simulation/soak job, and separate pinned Windows/Unity #16 determinism certification job. Removes the stale claim that hook installation and D3 invocation are deferred. D9 and `IFlakeReporter` remain separate unresolved work. |
| 0.1     | May 12, 2026 | Claude Code | Initial draft from `outline-detailed.md` v1.1. `ITestHarness`, `IScenario`, `IFixtureValidator` declared; `IFlakeReporter` explicitly deferred. |
| 0.2     | May 12, 2026 | Claude Code | Self-critique sweep. #16 §7 → §5 throughout; #16 §5 → §3.2.4.1 (canonical schema, §4.2); tolerance-matrix citation pinned to #16 §3.4.2 (§4.3.2). M2 `index.<ext>` notation. M3 `IFixtureValidator` phantom-interface judgment made explicit (§4.4.2). L3 §3.10 cross-reference added to pre-commit budget (§4.5.1). |
| 0.4     | September 3, 2026 | — | **A3.2b review correction (Codex #353 finding 2).** Repoints the `tests/scenarios/index.<ext>` encoding/extension pin from D1 to the new **D9**. A3.2b closed D1 on the test runner (NUnit) alone, which stranded the manifest encoding decision that D1 had jointly owned; every live `index.<ext>` reference now names D9. No extension is pinned here — pinning one in A3.2b would be a normative content decision outside this slice. |
| 0.3     | September 3, 2026 | — | **A3.2b supporting-surface synchronization.** Separates owning test placement from reusable architecture-proof records and records the A8-only architecture/evidence gate topology without creating a mega test assembly or activating enforcement. The live-repo pass also replaces obsolete D1/D4 deferrals with the existing NUnit/`dotnet test` gate and GitHub Actions paths. |
