# ERR-019-001 / ERR-019-003 — Current Candidate Status

**Date:** September 4, 2026  
**Issues:** Testing Strategy #19 FR-TS-075 / FR-TS-079 pipeline gap (`ERR-019-001`) and missing automated checklist/schema auditor deliverables (`ERR-019-003`)  
**Status:** OPEN — implementation candidate present; operational acceptance, aggregate landing synchronization, and A3.4 decisions remain outstanding

This record is the current-status successor to the September 3/4 diagnoses retained in `docs/tracking/open-issues.md` and `docs/tracking/spec-error-log.md`. Those original entries remain historical statements of what was absent when each ERR was filed. Their claims that `tools/run-tests-local.sh`, `tools/checklist-auditor.py`, and `tools/spec5-schema-auditor.py` do not exist are **superseded for PR #357's candidate tree** by the implementation below; the ERRs themselves are not closed merely because candidate files now exist.

PR #357 was reconciled with `main` after PR #358 landed. The reconciliation deliberately preserved PR #358's authoritative `docs/specs/testing-strategy/*` files unchanged. This candidate therefore does not smuggle normative changes into the executable repair; A3.4 remains the place where any substantive requirement amendment is judged.

## Candidate implementation now present

- `tools/run-tests-local.sh` is the versioned local/CI policy entry point; PR CI invokes `bash tools/run-tests-local.sh --pr` rather than bypassing it.
- `tools/checklist-auditor.py` and `tools/spec5-schema-auditor.py` now exist. Routine pre-commit/PR/nightly composition runs them **survey-only**, so pre-existing corpus debt cannot become an unrelated merge blocker. An explicit approval-transition invocation omits `--survey-only` and fails closed.
- checklist evidence is not accepted merely because a cited file exists: an approval walk requires a concrete cited section/literal carried by the evidence file, or an explicitly supplied `--captured-check` attestation for a named programmatic check.
- the §5 schema auditor validates Appendix-C-shaped payloads rather than keyword presence: layer/count rows, property/tier/owner rows, scenario/manifest/tier rows with resolving paths, coverage tier thresholds, determinism field/tier/source rows, approval linkage, and version history.
- `.githooks/pre-commit` evaluates the staged Git index in a persistent `.git/testing-strategy/precommit-snapshot`. Tracked content is refreshed from the index while untracked generated/build outputs survive between commits. `tools/bootstrap-dev.sh` installs/verifies the hook without overwriting another `core.hooksPath` and performs the one-time cold cache preparation outside the normal 60-second acceptance measurement.
- pre-commit selection is declared in `tools/dotnet-ci/precommit.runsettings` with NUnit method-name-prefix rules (`^int_`, `^sim_`, `^e2e_`) rather than unsafe `FullyQualifiedName` substring exclusions. Ordinary unit names containing `Point_`, `Fingerprint_`, `MalformedInt_`, or `QuickSim_` remain eligible.
- the normal pre-commit attempt has an enforced 60-second whole-composition failure bound and uses one incremental generated-solution `dotnet test` invocation rather than 34 cold sequential project invocations. `--fast` is valid without a test filter.
- D2 is pinned to FsCheck.NUnit 2.16.6 in the candidate tooling surface.
- D3 collector selection is pinned to coverlet.collector 6.0.4 in the candidate tooling surface; the separate per-tier threshold mapper/auditor remains open.
- `sim_match_engine_close_chance` remains owner-held RED, not quarantine. The PR composition selects it by exact test `Name`, executes it separately, and requires one unambiguous failed result with the recorded `-0.165 / 0.407` diagnostics, no extra results, and the expected runner exit. Drift, ambiguity, abnormal execution, or unexpected green blocks.
- `.github/workflows/nightly.yml` separates GitHub-hosted Linux functional/simulation/soak execution from authoritative Spec #16 certification. The self-hosted Windows/Unity job is disabled unless repository variable `DETERMINISM_CERTIFIED_RUNNER_ENABLED=true`; until then a GitHub-hosted notice records the operational gap rather than queueing indefinitely for an unregistered runner.
- the live `CI for Main branch` ruleset was read directly. Its required contexts are Markdown lint, YAML lint, Markdown link check, Spec hygiene checks, File manifest sanity, and C# format check. Neither Linux functional-job name is required, so the job-name change does not create an unreachable branch-protection context.

## Conditions that still prevent closure

1. **Pre-commit timing acceptance is unproven.** The zero-cache-by-construction defect is removed, but timeout/caching structure does not prove that a complete passing incremental composition finishes within 60 seconds on the certified developer host. A successful measured run is still required.
2. **Certified nightly execution is unproven.** The Windows/Unity job is deliberately gated off until a runner carrying `[self-hosted, windows, x64, determinism-certified]` is actually registered/configured and the enable variable is set. A successful execution on the pinned host is still required.
3. **Normative acceptance is pending.** The currently approved Spec #19 text on `main` remains authoritative. Any proposed relaxation such as FR-TS-011 pipeline scoping or FR-TS-062 deferred double-run activation must be accepted or rejected on its merits at A3.4; implementation state is not authority.
4. **Aggregate landing synchronization is pending.** Before merge, the landing set still must add forward/current-state pointers in `open-issues.md` / `spec-error-log.md`, synchronize changelog/file-manifest/name-keyed guidance, and re-derive maintained counts against the final branch head. Historical entries should be preserved rather than rewritten.

## Closure rule

`ERR-019-001` and the implementation portion of `ERR-019-003` may close only when their landed-baseline requirements are actually satisfied and the aggregate tracking surfaces are synchronized. `ERR-019-001` additionally requires a successful ≤60-second certified-host pre-commit measurement and a successful certified Windows/Unity nightly determinism run. Any normative relaxation required to make the executable topology conformant remains an explicit A3.4 owner decision, not an implementation-side assumption.
