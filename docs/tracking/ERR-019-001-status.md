# ERR-019-001 — Current Candidate Status

**Date:** September 4, 2026  
**Issue:** Testing Strategy #19 FR-TS-075 / FR-TS-079 pipeline conformance gap  
**Status:** OPEN — implementation candidate present; operational acceptance and A3.4 approval remain outstanding

This record is the current-status companion to the September 3 diagnosis in `docs/tracking/open-issues.md`. The original entry is retained as the historical statement of what was absent when `ERR-019-001` was filed. Its claims that the pre-commit, nightly, and `tools/run-tests-local.sh` surfaces do not exist are **superseded for this PR candidate** by the repository state described below. The issue itself is not closed by creating those files.

## Candidate implementation now present

- `tools/run-tests-local.sh` is the versioned local/CI composition entry point; PR CI invokes `bash tools/run-tests-local.sh --pr` rather than bypassing it.
- `.githooks/pre-commit` evaluates the staged Git index in a persistent `.git/testing-strategy/precommit-snapshot`. Tracked content is refreshed from the index while untracked generated/build outputs survive between commits. `tools/bootstrap-dev.sh` installs/verifies the hook without overwriting a different `core.hooksPath` and performs the one-time cold cache preparation outside the normal 60-second acceptance measurement.
- pre-commit selection is declared in `tools/dotnet-ci/precommit.runsettings` with NUnit method-name-prefix rules (`^int_`, `^sim_`, `^e2e_`) rather than unsafe `FullyQualifiedName` substring exclusions. Ordinary unit names containing `Point_`, `Fingerprint_`, `MalformedInt_`, or `QuickSim_` therefore remain eligible.
- the normal pre-commit attempt has an enforced 60-second whole-composition failure bound and uses one incremental generated-solution `dotnet test` invocation rather than 34 cold sequential project invocations.
- D2 is pinned to FsCheck.NUnit 2.16.6.
- D3 collector selection is pinned to coverlet.collector 6.0.4; the separate per-tier threshold mapper/auditor remains open.
- `sim_match_engine_close_chance` remains an owner-held RED, not quarantine. The PR composition selects it by exact test `Name`, executes it separately, and verifies the recorded `-0.165 / 0.407` diagnostics. Changed diagnostics, duplicate/ambiguous/missing identity, extra results, abnormal runner exit, or an unexpected pass are blocking.
- `.github/workflows/nightly.yml` separates GitHub-hosted Linux functional/simulation/soak execution from authoritative Spec #16 certification. The self-hosted Windows/Unity job is disabled unless repository variable `DETERMINISM_CERTIFIED_RUNNER_ENABLED=true`; until then a GitHub-hosted notice job records the operational gap rather than queueing indefinitely for an unregistered runner.
- checklist and per-spec §5 auditors are executable and fail closed when explicitly used for an approval transition. Routine pre-commit/PR/nightly composition runs them in **survey-only** mode, so pre-existing corpus debt cannot turn an unrelated one-line spec correction into a new repository-wide merge blocker.
- the live repository ruleset `CI for Main branch` does **not** require either the historical or candidate Linux functional job name. Its required contexts are the six established lint/spec/manifest/format checks. Therefore the job-name change does not itself create an unreachable required-status check; name-keyed repository guidance still must be synchronized before landing.

## Conditions that still prevent closure

1. **Pre-commit timing acceptance is unproven.** The cold-cache-by-construction defect is removed, but the timeout and persistent-cache design do not prove that a complete passing incremental composition finishes within 60 seconds on the certified developer host. A successful measured run is still required.
2. **Certified nightly execution is unproven.** The Windows/Unity job is deliberately gated off until a runner carrying `[self-hosted, windows, x64, determinism-certified]` is actually registered/configured and the enable variable is set. A successful execution on the pinned host is still required.
3. **A3.4 reapproval is pending.** FR-TS-011 and FR-TS-062 are substantive normative relaxations/amendment candidates. They must be accepted or rejected on their merits; the executable branch does not self-approve the changed MUSTs or recast them as mere synchronization.
4. **Landing synchronization is pending.** `open-issues.md`, `spec-error-log.md`, changelog/file-manifest and other name-keyed guidance need forward pointers/current-state corrections in the landing set. Historical records stay historical rather than being silently rewritten.

## Closure rule

`ERR-019-001` may move to `open-issues-resolved.md` only after all conformance facts are true in the landed baseline: the versioned three-pipeline topology is present, a successful ≤60-second certified-host pre-commit measurement is recorded, and the certified Windows/Unity nightly determinism job has executed successfully. A3.4 owner reapproval must also have accepted the substantive FR-TS-011/062 amendments on their merits and synchronized the authoritative status/tracking surfaces.
