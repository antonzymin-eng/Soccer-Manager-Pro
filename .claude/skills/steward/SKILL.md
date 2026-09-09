---
name: steward
description: >-
  Drive a pull request on this repo to green and to merged — triaging CI,
  resolving mergeability notices and conflicts, answering review threads, and
  landing the tracking-document half that a merge or CI fix drags with it. Use
  this skill for PR/CI events such as "CI is red on #N", "watch this PR",
  "resolve the conflicts", "is the PR green", a mergeability/base-branch
  notice, or review feedback arriving on a branch you pushed.
---

# Steward

The general PR-driving authority lives in Claude Code's account-level GitHub rules. This file contains only repository-specific routing: which test entry point is policy-authoritative, how the owner-held RED is treated, and which tracking surfaces must be reconciled at landing.

## Before you touch the PR

Run the `orientation` skill first. Base is `main`.

One landing set carries code, spec/ERR changes, and tracking synchronization together per `landing-close-out`. A bare test fix without its changed authority/tracking surfaces is incomplete.

## The policy gate is the truth

The repository policy entry point is now:

```bash
bash tools/run-tests-local.sh --pr
```

CI's non-certifying Linux functional job invokes that command. `tools/dotnet-ci/run-gate.sh` is a **lower-level executor** and must not be substituted for the policy runner when claiming PR-equivalent evidence; a bare low-level run omits the Spec #19 auditor/coverage/owner-held composition.

For developer bootstrap and pre-commit:

```bash
bash tools/bootstrap-dev.sh
bash tools/run-tests-local.sh --pre-commit
```

The hook evaluates the staged Git index through a persistent build snapshot under `.git/testing-strategy/`; bootstrap prepares its cold cache once. After that first materialization the hook refreshes only tracked paths whose index blobs changed, preserving unchanged source mtimes as well as untracked bin/obj outputs so MSBuild can actually reuse incremental state. Snapshot checkout disables Git-LFS smudging locally because this gate needs staged pointer bytes, not binary asset payloads. The normal attempted composition is hard-bounded to 60 seconds, but that limit is not acceptance evidence until a successful run is measured on the certified developer host.

Routine checklist/§5 audits are survey-only. `docs/specs/SPEC_INDEX.md` is the canonical approval authority. On PR CI, the policy runner receives the PR base SHA, compares the base/head registry states, and reruns the auditors as blocking only for spec directories whose canonical status changes from non-approved/missing to `APPROVED`. Missing/unparseable registry history fails closed. This prevents historical corpus debt from blocking an unrelated edit without leaving FR-TS-042/052 enforcement as a command somebody has to remember.

Three CI-reading rules remain important:

1. **Never round an incomplete run up to green.** A pending/cancelled long functional job proves nothing about its final test verdict.
2. **Count suites mechanically.** `ls -d src/*/[Tt]ests/*.asmdef` rather than hand-counting.
3. **`MatchEngine.Tests` is the long pole.** Another suite finishing does not imply it completed.

## Owner-held RED is not quarantine

`sim_match_engine_close_chance` remains **owner-held RED by decision** (`docs/tracking/close-chance-creation-design.md` §10.9 item 6). Never rebaseline it just to get green.

The policy runner no longer makes the whole Linux job red merely because this one owner-held predicate remains at its approved RED state. Instead it:

1. excludes that exact test `Name` from the ordinary blocking pass;
2. executes the exact test separately;
3. requires exactly one result;
4. requires outcome `Failed` and the recorded diagnostic tokens;
5. fails on unexpected green, changed diagnostics, missing/ambiguous identity, extra results, or abnormal runner exit.

That is **not flake quarantine**. `tools/dotnet-ci/known-failures.txt` remains the separate shrinking-only quarantine source. If any new ordinary test fails, or the owner-held predicate changes state, treat it as a real finding.

## Required-status configuration

Do not infer merge protection from a job name. Read the live repository ruleset before making a required-context claim.

As of the September 4, 2026 PR #357 correction, ruleset `CI for Main branch` requires exactly these six contexts:

- `Markdown lint`
- `YAML lint`
- `Markdown link check`
- `Spec hygiene checks`
- `File manifest sanity`
- `C# format check`

The non-certifying Linux functional job is **not** currently required. Historical A1c records that explain why the old steady-red job was not required remain historical evidence; PR #357 changes the owner-held RED handling and therefore invalidates that old rationale as a statement of current behavior without invalidating the historical measurement.

## Certified determinism boundary

GitHub-hosted Linux is non-certifying regression/functional evidence only. Authoritative Spec #16 determinism runs on the pinned Windows/Unity environment.

`.github/workflows/nightly.yml` keeps the self-hosted certified job disabled unless repository variable `DETERMINISM_CERTIFIED_RUNNER_ENABLED=true`. Do not treat the workflow definition, labels, or variable as proof that a runner exists or that certification passed; require an actual successful certified-host run.

## When triage finds a real defect

- **Approved spec/code contradiction:** run `err-file-and-backprop`; re-check the ERR id against `main` at merge time.
- **Anything that actually lands:** run `landing-close-out`.
- **Pre-existing debt discovered by survey tooling:** do not turn it into an unrelated PR blocker by accident. File/update an issue if it will outlive the PR; Spec #19 checklist/schema auditors block only at a detected canonical registry approval transition, while routine composition is survey-only.
- **Football-plausibility symptom:** route through `match-realism-pass`, including KD-W1 wiring checks; never tune a `[GT]` from a CI symptom without proving the component is active.
- **Design fork / new `[GT]` / layer-membership call:** owner/advisor decision, not an agent guess.
- **Review comment asking for judgment over a surface rather than a direct patch:** hand off to `adversarial-review`.
- **Force-push, discard another branch's work, rebaseline acceptance, or merge with a newly-red band:** owner call.

## Conflicts in tracking documents

- **`docs/tracking/CHANGELOG.md` / `CHANGELOG-src.md`:** keep both append-only histories, newest first; exactly one bare `**Last Updated:**` label. Correct old errors with successor entries, not rewrites.
- **`docs/tracking/open-issues.md`:** union both sides. Re-derive active/resolved counts after moving an issue.
- **`docs/tracking/spec-error-log.md`:** keep the Error Index row and full ERR body synchronized.
- **`docs/tracking/file-manifest.md`, `src/CLAUDE.md`, spec files:** merge version histories and re-derive the resulting version/state from the merged tree.
- **`tools/dotnet-ci/known-failures.txt`:** shrinking-only; a conflict is never permission to re-add a quarantine.

## Reporting

Report PR number, head SHA, CI run id, completed job conclusions, owner-held/quarantine state and its evidence source, what was pushed, and what remains open. If a long job has not completed, say so. If certified-host execution or the ≤60-second measured pre-commit acceptance has not happened, say so rather than inferring it from configuration.
