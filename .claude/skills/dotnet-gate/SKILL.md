---
name: dotnet-gate
description: >-
  Run the repository's non-certifying Linux PR-equivalent test policy and report the result, including
  ordinary blocking failures, quarantine state, and owner-held RED verification. Use before landing
  code/tooling changes, when asked to run tests/gate/check a build, or when triaging the Linux
  functional CI job.
---

# Run the Dotnet Gate

The **policy entry point** is:

```bash
bash tools/run-tests-local.sh --pr
```

That is the command the PR Linux functional job uses. It composes the Spec #19 survey auditors,
whole-tree generated .NET build/test execution, Coverlet collection, quarantine handling, and the
separate owner-held RED verifier.

`tools/dotnet-ci/run-gate.sh` is the lower-level executor. Use it only for targeted executor debugging;
a bare run is **not** PR-equivalent evidence because it bypasses the policy composition.

## Pre-commit

Developer bootstrap and the normal fast path are:

```bash
bash tools/bootstrap-dev.sh
bash tools/run-tests-local.sh --pre-commit
```

The hook tests the staged Git index through a persistent build snapshot under
`.git/testing-strategy/precommit-snapshot`. Bootstrap performs the one-time cold cache preparation.
The normal attempted composition is hard-bounded to 60 seconds, but do not claim that requirement is
met until an actual successful certified-developer-host run is measured within the bound.

Pre-commit selection comes from `tools/dotnet-ci/precommit.runsettings`, using NUnit METHOD-name
prefix rules for `^int_`, `^sim_`, and `^e2e_`. Do not replace these with bare FullyQualifiedName
substring filters: those over-match ordinary unit names such as `Point_`, `Fingerprint_`, and
`QuickSim_`.

## Ordinary failures, quarantine, and owner-held RED

`tools/dotnet-ci/known-failures.txt` is the shrinking-only functional quarantine ledger. Do not add a
test merely to obtain green.

`sim_match_engine_close_chance` is different: it is **owner-held RED by decision**, not quarantine.
PR/nightly policy excludes that exact test `Name` from the ordinary pass, runs the exact `Name`
separately, and requires:

- exactly one matching result;
- outcome `Failed`;
- the recorded diagnostic tokens;
- no extra returned results; and
- the expected test-failure runner exit.

Unexpected green, drift, ambiguity/missing identity, extra results, or abnormal exit is a real failure.
Never rebaseline the band just to get green.

## Reading failures

Check these common classes first:

- Unity-vs-supported-BCL API mismatches;
- NUnit test visibility;
- static initialization ordering;
- ambiguous cross-assembly type names; and
- diagnostic tests that intentionally skip unless their documented `TD_*_DIAGNOSTIC` switch is set.

Do not round a pending or cancelled long suite up to a pass. `MatchEngine.Tests` is the long pole and
other suites completing does not prove it finished.

## Reporting

Report the actual PR-equivalent command, completed CI/run state, ordinary failure count, quarantine
state, owner-held RED verifier outcome, and what remains unproven. Never restate a previous run as the
current result.

## Certification boundary

This Linux policy is explicitly **non-certifying**. Authoritative determinism certification is the
pinned Windows/Unity/Mono Spec #16 run. The scheduled certified job is disabled until the matching
self-hosted runner is actually registered/configured and repository variable
`DETERMINISM_CERTIFIED_RUNNER_ENABLED=true` is set. Workflow YAML alone is not certification evidence.
