# ERR-019-001 — Current Candidate Status

**Date:** September 4, 2026  
**Issue:** Testing Strategy #19 FR-TS-075 / FR-TS-079 pipeline conformance gap  
**Status:** OPEN — implementation candidate present; operational acceptance and A3.4 approval remain outstanding

This record is the current-status companion to the September 3 diagnosis in `docs/tracking/open-issues.md`. The original entry is retained as the historical statement of what was absent when `ERR-019-001` was filed. Its claims that the pre-commit, nightly, and `tools/run-tests-local.sh` surfaces do not exist are **superseded for this PR candidate** by the repository state described below. The issue itself is not closed by creating those files.

## Candidate implementation now present

- `tools/run-tests-local.sh` is the versioned local/CI composition entry point.
- `.githooks/pre-commit` evaluates the staged Git index; `tools/bootstrap-dev.sh` installs/verifies the hook without overwriting a different existing `core.hooksPath`.
- the pre-commit composition has an enforced 60-second wall-clock failure bound and uses the generated test-project fast path rather than the whole-tree build path.
- PR CI invokes `bash tools/run-tests-local.sh --pr` rather than bypassing the versioned composition.
- D2 is pinned to FsCheck.NUnit 2.16.6.
- D3 collector selection is pinned to coverlet.collector 6.0.4; the separate per-tier threshold mapper/auditor remains open.
- `sim_match_engine_close_chance` remains an owner-held RED, not quarantine. The PR composition runs it separately and verifies the recorded `-0.165 / 0.407` diagnostics; changed diagnostics, missing/ambiguous results, abnormal runner exit, or an unexpected pass are blocking.
- `.github/workflows/nightly.yml` separates GitHub-hosted Linux functional/simulation/soak execution from the authoritative Spec #16 determinism job assigned to the pinned self-hosted Windows/Unity environment.
- checklist and per-spec §5 auditors are executable. They survey the repository; unresolved evidence blocks an affected changed non-legacy spec when that candidate is in `APPROVED` state, matching FR-TS-042/052 rather than retroactively blocking unrelated code changes.

## Conditions that still prevent closure

1. **Pre-commit timing acceptance is unproven.** The timeout proves that an attempted run cannot exceed 60 seconds and still succeed. It does not prove that the complete passing composition actually finishes within 60 seconds on the certified developer host. A successful measured run is still required.
2. **Certified nightly execution is unproven.** The Windows/Unity job definition exists, but YAML presence does not prove that a runner carrying `[self-hosted, windows, x64, determinism-certified]` is registered/configured or that `TD_UNITY_EXE` and the platform-pin environment are valid. A successful execution on the pinned host is still required.
3. **A3.4 reapproval is pending.** FR-TS-011 and FR-TS-062 are substantive normative amendment candidates. The branch implementation does not self-approve those changed MUSTs; explicit owner reapproval is required.
4. **Landing synchronization is pending.** The final A3.4/landing pass must update the aggregate open-issue/manifest/status surfaces against the actual merge result rather than pre-claiming a landed state on the PR branch.

## Closure rule

`ERR-019-001` may move to `open-issues-resolved.md` only after all three conformance facts are true in the landed baseline: the versioned three-pipeline topology is present, the successful ≤60-second certified-host pre-commit evidence is recorded, and the certified Windows/Unity nightly determinism job has executed successfully; A3.4 owner reapproval must also have accepted the substantive FR-TS-011/062 amendments and synchronized the authoritative status surfaces.
