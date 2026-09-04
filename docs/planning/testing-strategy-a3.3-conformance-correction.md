# Testing Strategy A3.3 — Pipeline Conformance Correction

**Date:** September 4, 2026  
**Applies to:** `docs/planning/project-architecture-governance-integration-plan.md` v0.38, §7  
**Status:** Candidate correction feeding A3.4; no fresh Spec #19 approval is granted here

## Why this correction exists

Integration-plan v0.38 is a September 3 checkpoint. Its version-history row correctly recorded the repository state at that time: D2/D3 were deferred and the FR-TS-075/079 pre-commit/nightly/local-runner gap was open. Those statements are no longer the current candidate implementation state after PR #357. This correction preserves v0.38 as history while providing the current A3 hand-off state instead of silently rewriting what that earlier checkpoint observed.

## Current candidate state

- **D2 resolved:** FsCheck.NUnit 2.16.6 is pinned through `Directory.Build.targets`.
- **D3 collector resolved:** coverlet.collector 6.0.4 and `tools/dotnet-ci/coverage.runsettings` are wired. The §5.5 per-tier threshold mapper/auditor is still a separate open implementation obligation; D3 is not used to claim that mapper exists.
- **FR-TS-079 local/CI composition implemented:** `tools/run-tests-local.sh` is the stable composition entry point and PR CI reuses `--pr` rather than directly calling the lower-level Linux gate.
- **Pre-commit topology corrected:** the hook evaluates the staged Git index through a persistent snapshot under `.git/testing-strategy`. Bootstrap performs the one-time full materialization outside the normal 60-second acceptance measurement. Later hook runs compare the recorded Git index tree with the current tree, remove deleted tracked paths with a linear manifest `comm`, and refresh only changed index paths. Unchanged tracked files retain their mtimes and untracked generated/bin/obj outputs survive, so MSBuild can actually reuse incremental state. The normal attempt remains hard-bounded to 60 seconds.
- **Pre-commit selection corrected:** `tools/dotnet-ci/precommit.runsettings` uses anchored NUnit METHOD-name prefixes for `int_`, `sim_`, and `e2e_`. Bare `FullyQualifiedName` substring exclusions are not used; names such as `Point_`, `Fingerprint_`, `MalformedInt_`, and `QuickSim_` remain eligible.
- **Nightly topology implemented without pretending runner availability:** GitHub-hosted Linux supplies non-certifying functional/simulation/soak evidence. The authoritative Spec #16 §5 self-hosted Windows/Unity job is gated by repository variable `DETERMINISM_CERTIFIED_RUNNER_ENABLED=true`; until a certified runner is actually registered/configured, a GitHub-hosted notice records the open operational condition rather than queueing the unavailable job.
- **Owner-held RED handling corrected:** `sim_match_engine_close_chance` is neither quarantine nor silently ignored. The low-level gate selects/excludes it by exact test `Name`; the verifier requires one unambiguous result, the recorded diagnostics, no extra returned results, the expected failed outcome, and the expected test-failure runner exit. Unexpected green/drift/ambiguity/abnormal execution blocks.
- **Auditors implemented without a retroactive merge landmine:** routine pre-commit/PR/nightly composition runs checklist and §5 schema auditors in survey-only mode. PR CI now receives the PR base SHA, mechanically detects a spec section changing from non-approved/missing to `APPROVED`, and reruns both auditors as blocking only for those transitioning spec directories. FR-TS-042/052 enforcement therefore has an automatic approval-event trigger instead of relying on a human to remember a command, while existing approved-spec debt remains survey-only until a fresh approval transition.
- **Current survey debt is explicitly sized:** the stricter auditors reported 219 checklist findings plus 329 §5 schema findings on the current PR corpus, 548 total. These are survey findings, not 548 new merge blockers. A3.4 must re-run and size this corpus before any broad approval walk rather than discovering the volume during sign-off.
- **Required-status state checked:** the live `CI for Main branch` ruleset does not require either the historical or candidate Linux functional-gate context. The six required contexts remain Markdown lint, YAML lint, Markdown link check, Spec hygiene checks, File manifest sanity, and C# format check. The functional-job rename therefore does not create an unreachable required status, although name-keyed guidance still requires landing synchronization.

## Still open before A3.4 can declare conformance

1. Record a **successful** complete incremental pre-commit run within 60 seconds on the certified developer host. The cold-cache and whole-tree-mtime defects are fixed, but timeout/caching structure alone is insufficient execution evidence.
2. Register/configure the certified Windows runner, enable `DETERMINISM_CERTIFIED_RUNNER_ENABLED`, and record a **successful** Spec #16 §5 run on the pinned Windows 11 / Unity 6000.4.9f1 / DX11 / Mono / x64 / SSE4.2 / one-worker tuple. Workflow structure alone is insufficient evidence.
3. Obtain an explicit owner decision on the **substantive normative relaxations** in FR-TS-011 and FR-TS-062. A3.4 must judge those changes on their merits; they are not implementation synchronization and must not be approved merely because the implementation already follows them.
4. Re-run the approval auditors, size the then-current survey debt, and decide the cleanup/approval sequence deliberately. The September 4 candidate measurement is 548 findings (219 checklist + 329 schema); it is a planning input, not a frozen acceptance number.
5. Keep D9, the per-tier coverage mapper/auditor, dedicated flake integration/eviction automation, and A8 architecture/evidence required-status activation outside this correction unless separately implemented and reviewed.
6. Perform landing-time synchronization of `SPEC_INDEX.md`, file manifest, changelog/open-issue/spec-error state, name-keyed repository guidance, and the combined §9 evidence against the actual merge result.

## A3.4 acceptance rule

A3.4 must evaluate executable state, normative merit, corpus debt, and recorded execution evidence separately. A versioned script, timeout, cache design, workflow YAML, runner label, or repository variable proves topology/configuration only. It does not prove that the timing or certified-host execution obligation succeeded. Nor does implementation state justify relaxing an existing MUST. Spec #19 may be reapproved only after the operational facts are recorded, the approval-auditor debt is explicitly dispositioned, and FR-TS-011/062 are accepted or rejected on their merits.
