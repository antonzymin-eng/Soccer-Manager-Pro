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
- **Pre-commit topology implemented:** a versioned staged-snapshot hook and non-destructive bootstrap are present. The full composition has a hard 60-second failure bound and a fast generated-test-project path.
- **Nightly topology implemented as two host classes:** GitHub-hosted Linux is non-certifying functional/simulation/soak evidence; authoritative Spec #16 §5 execution is assigned to the pinned self-hosted Windows/Unity certification host.
- **Owner-held RED handling corrected:** `sim_match_engine_close_chance` is neither quarantine nor silently ignored. It executes separately against its recorded diagnostic baseline, and unexpected green/drift/abnormal execution blocks.
- **Auditors implemented with approval-state semantics:** whole-repository survey remains visible; blocking attaches to an affected changed non-legacy spec when its candidate state is `APPROVED`, matching FR-TS-042/052.

## Still open before A3.4 can declare conformance

1. Record a **successful** complete pre-commit run within 60 seconds on the certified developer host. Timeout structure alone is insufficient evidence.
2. Record a **successful** Spec #16 §5 run from the `determinism-certified` self-hosted Windows runner on the pinned Windows 11 / Unity 6000.4.9f1 / DX11 / Mono / x64 / SSE4.2 / one-worker tuple. Workflow structure alone is insufficient evidence.
3. Obtain explicit owner reapproval for the **substantive normative** FR-TS-011 and FR-TS-062 amendments. They must not be classified as documentation-only synchronization.
4. Keep D9, the per-tier coverage mapper/auditor, dedicated flake integration/eviction automation, and A8 architecture/evidence required-status activation outside this correction unless separately implemented and reviewed.
5. Perform landing-time synchronization of `SPEC_INDEX.md`, file manifest, changelog/open-issue state, and the combined §9 evidence against the actual merge result.

## A3.4 acceptance rule

A3.4 must evaluate executable state and recorded execution evidence separately. A versioned script, timeout, workflow YAML, or runner label proves topology/configuration only. It does not prove that the timing or certified-host execution obligation succeeded. Spec #19 may be reapproved only after those operational facts and the FR-TS-011/062 owner decisions are explicit.
