# Testing Strategy & Framework Specification #19 — Section 7: Future Extensions

**Created:** May 12, 2026
**Last Updated:** September 3, 2026
**Version:** 0.3
**Status:** AMENDMENT DRAFT (A3.2b; May 15, 2026 approved baseline remains in force)
**Amendment plan:** `docs/planning/project-architecture-governance-integration-plan.md` v0.37, §7; A3.2b
**Purpose:** Stage 0+1 transition deliverables, Stage 1 deliverables,
Stage 5+ extensions, permanent exclusions, and the deferred decisions
tracker (D1 … D8).

---

## 7.1 Stage 0+1 Transition Deliverables

Items below activate at the Stage 0 → Stage 1 transition (first `src/`
code commit). KD-5 governs.

- **D1 resolved:** NUnit is pinned by the generated .NET test projects;
  `tools/dotnet-ci/run-gate.sh` executes them with `dotnet test`.
- Property framework pin (§6.1, D2) remains deferred.
- Coverage tool pin (§6.1, D3) remains deferred.
- Test-runner invocation/runner paths are established by
  `tools/dotnet-ci/generate_projects.py` and
  `tools/dotnet-ci/run-gate.sh`; no additional pin is required for
  the existing Linux shim gate.
- `tools/checklist-auditor.py` initial implementation (§5.3).
- `tools/spec5-schema-auditor.py` initial implementation (§5.4).
- Pre-commit hook script (`tools/run-tests-local.sh`) extended to
  invoke `src/`-side tests (§6.3, Appendix E).
- Pyramid-ratio thresholds (§3.1.2) re-evaluated against actual code.
- Scenario library populated index (`tests/scenarios/index.<ext>`,
  §3.3.6; `<ext>` pinned at Stage 0+1 per D1).
- Test-defect log (`tests/test-defect-log.md`) instantiated.
- Targeted governance mutation needed by FR-TS-091 is part of the
  architecture-proof path and does **not** wait for D6's project-wide
  mutation program. Its proof semantics are §3.11.9; implementation and
  blocking eligibility still follow the A4/A8 governance rollout.

## 7.2 Stage 1 Deliverables

- Coverage dashboard (§3.6.4 absolute per-tier).
- Flake quarantine + eviction tooling (§3.7).
- `IFlakeReporter` interface declaration — deferred from §4.4 per
  CLAUDE.md "Interface Design Principle". Declared in `src/CLAUDE.md`
  or a Stage 1 CI spec once the CI integration layer is concretely
  specified; both producer and consumer MUST be specified before this
  interface is written.
- Mutation-testing first activation (§6.1, D6).
- Per-spec §5 schema-conformance auto-check (§5.4.3).
- **Appendix D approved-spec §5 survey populated.** Deferred from §9.2
  per §3.5.4 dilution policy; row contents are a Stage 0+1 deliverable
  (FR-TS-045).
- `tests/coverage-exemptions.md` instantiated. Coverage exemptions may
  waive only the exact Spec #19 coverage obligation their rule permits;
  they cannot waive an admitted architectural property, Governance
  Blocker, or required architecture proof.
- `tests/exceptions.md` instantiated. Spec #19 exceptions remain local
  to Spec #19 and cannot substitute for a Governance property exception
  or waive required Governance evidence/correctness obligations.

## 7.3 Stage 5+ Extensions

- Cross-platform bit-exact parity test layer activates (parallel to
  Spec #20 §7.3). Currently Stage 0 uses `float` per CLAUDE.md "When
  Writing Code"; cross-platform parity is a Stage 5+ concern.
- Fixed64-aware property tests (Spec #9 dependency).
- Multiplayer determinism-cert layer.
- Network-replay test corpus (replay across two hosts).

## 7.4 Permanent Exclusions

- Test framework debates this spec refuses to relitigate. Frameworks
  are chosen at Stage 0+1 once and not revisited absent vendor
  abandonment.
- **Flake suppression by retry attribute** — never permitted (§3.7.5
  anti-pattern; FR-TS-067).
- Per-spec §5 sections will NOT be forcibly rewritten by Spec #19
  (KD-4 permanent rule).
- Sleep-based synchronization in tests — never permitted (§3.7.5
  anti-pattern).

## 7.5 Deferred Decisions Tracker

| ID | Decision | Target | Owner |
|----|----------|--------|-------|
| D1 | **RESOLVED — NUnit**; generated test projects pin NUnit 3.14.0 + NUnit3TestAdapter 4.6.0 and run through `tools/dotnet-ci/run-gate.sh` | Resolved in live repository; verified September 3, 2026 | Repository tooling |
| D2 | Property framework pin (FsCheck vs alternative) | Stage 0+1 | Lead developer |
| D3 | Coverage tool pin (Coverlet vs alternative) | Stage 0+1 | Lead developer |
| D4 | **RESOLVED — GitHub Actions** at `.github/workflows/ci.yml`; architecture/evidence required-status activation remains A8-owned | Resolved before A3.2b | Repository CI configuration |
| D5 | LFS storage decision for fixtures (§3.8.2) | Stage 0+1 | Lead developer |
| D6 | Project-wide mutation-testing program activation date (for example Stryker.NET); excludes FR-TS-091 targeted governance mutation | Stage 1 | Lead developer |
| D7 | Visual-regression framework selection (§3.9.3) | Stage 1+ | Lead developer |
| D8 | Coverage-guided (AFL-style) fuzzing adoption (§3.4.1) | Stage 1+ | Lead developer |

## 7.6 Version History

| Version | Date         | Author      | Notes |
|---------|--------------|-------------|-------|
| 0.1     | May 12, 2026 | Claude Code | Initial draft from `outline-detailed.md` v1.1. D1 … D8 deferred-decision tracker populated. |
| 0.2     | May 12, 2026 | Claude Code | Self-critique pass 2. `index.json` → `index.<ext>` with D1 cross-reference. |
| 0.3     | September 3, 2026 | — | **A3.2b supporting-surface synchronization.** Separates targeted FR-TS-091 governance mutation from deferred project-wide mutation tooling; constrains Spec #19 exception/coverage-exemption records so they cannot waive Governance properties, blockers, or required architecture proof; D1 (NUnit) and D4 (GitHub Actions) are recorded as already-resolved repository facts; D2/D3/D5–D8 remain deferred. No A4/A8 prerequisite is claimed complete. |
