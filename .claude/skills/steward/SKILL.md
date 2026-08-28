---
name: steward
description: >-
  Drive a pull request on this repo to green and to merged — triaging a red
  `Compile + test (Linux shim gate, non-certifying)` job, resolving a mergeability
  notice, answering review threads, and landing the tracking-document half that a
  merge or a CI fix always drags with it. Use this skill on any PR or CI event
  here: "CI is red on #N", "watch this PR", "resolve the conflicts", "is the PR
  green", a merge-conflict or base-branch-recovered notice, a review comment
  arriving on a branch you pushed, and any `claude/<slug>` branch you are about
  to merge. Trigger it even when the failure looks like infrastructure — the one
  red band in `MatchEngine.Tests` here is owner-held by decision, and every other
  failure in this tree has been real.
---

# Steward

**The authority for the general PR rules is Claude Code's own account-level
system prompt** — the "GitHub Integration" / "Driving a PR to green" sections
it carries into every session, not a document in this repo. Conflict-resolution
order, root-cause-before-retry, the two postures (a PR you own vs. one you're
only watching), the standing-down comment, never skip or quarantine a test to
get green: all of that lives there and is not repeated here. This file is only
what that generic authority cannot know about this repo: which script the gate
actually runs, which of this project's own skills to hand off to, which band
is red on purpose, and how this repo's own tracking documents conflict on a
merge.

**Why this exists.** Five sessions in the last two weeks re-derived this from
scratch, at a combined ~$133, and three of them ended without finishing: one
asked whether to even subscribe to a PR's CI rather than just doing it, one
was left watching CI at 7-passed/3-pending, one left "untangle the
`CHANGELOG-src.md` v2.84/v2.115 series" as an open decision nobody came back
to.

## Before you touch the PR

Run the `orientation` skill first — not re-described here.

Branch names are `claude/<short-slug>` (`claude/spec-upgrade-review-te3ta0`,
`claude/branch-commit-divergence-1armum`); push with
`git push -u origin claude/<slug>`. Base is `main`.

One commit carries code, spec patch, ERR entry, and doc sync together, per
`landing-close-out`. That rule binds a CI-triage fix exactly as it binds a
full landing — a bare "fix the test" commit is how the tracking docs start
drifting from the code.

## The gate is the truth

CI's compile/test job runs the same script the `dotnet-gate` skill runs
locally. Run that skill before every push and read its own output rather than
the CI log tail — including its quarantine rule, not restated here.

Three CI-log-reading traps this repo has actually hit, none of which the
generic rules know about:

1. **The gate script is `set -euo pipefail`.** It exits non-zero on the
   blocking phase and therefore never reaches its own quarantine-report
   section or its `── Gate PASSED ──` line. A run that failed printed **no
   verdict at all** — do not report "quarantine empty" or round a red run up
   to a pass. A landing did exactly that at seven sites before it was caught.
2. **Count suites, don't eyeball them.** `ls -d src/*/[Tt]ests/*.asmdef` —
   several suites live under a capitalised `Tests/` folder and a hand count
   has missed them before, publishing "31 of 32" against an actual 33.
3. **`MatchEngine.Tests` is the long pole (tens of minutes) and does not
   finish alphabetically.** Suites run in parallel, so another suite
   finishing proves nothing about this one. A run cancelled minutes into
   testing has been read as "the sweep ran to completion" before, and that
   claim had to be publicly withdrawn once it reached the root `CLAUDE.md`.

## The band that is red on purpose

`sim_match_engine_close_chance` in `MatchEngine.Tests` is **owner-held RED by
decision** (`docs/tracking/close-chance-creation-design.md` §10.9 item 6). It
fails at baseline with recorded values — check the current entry in the root
`CLAUDE.md` OPEN ISSUES section for the exact numbers, since they get
re-measured. A CI red that is *only* that predicate, at those same recorded
values, is the expected state: report it as e.g. **461/1/11 (the count as of
Aug 2026 — re-check the live entry)** — no new failure, no band rebaselined —
and do not "fix" it. Different numbers on the same
predicate, or any second failure anywhere, is a real finding.

**Never rebaseline an acceptance band just to get green.** That is an owner
call — see "Where this stops" below.

## When triage finds a real defect

This is routing, not a restatement:

- **Root-caused into APPROVED spec text, or code that contradicts one** — run
  the `err-file-and-backprop` skill. One addition specific to a merge: **re-verify
  the ERR id is still free against `main` at merge time**, not only at
  authoring — an id verified free on a branch has been claimed on `main` by
  another landing while that branch was still open.
- **Anything actually landed** — a fix, a conflict resolution that changed
  real content, a doc correction — run `landing-close-out`.
- **Pre-existing on the base branch** — the generic rule already covers the
  standing-down comment. This repo adds one decision on top: file an
  `open-issues.md` entry only if the failure will outlive this PR (it needs
  an owner decision, or it's a held band like the one above); otherwise the
  PR comment is the whole record. `open-issues.md` needed a de-duplication
  pass once already because one issue had two entries and the header count
  double-counted it — a redundant entry here is not free.

## Conflicts in the tracking documents

These files conflict on almost every merge that touches them, and "take both
sides" is wrong for most of them:

- **`docs/tracking/CHANGELOG.md`.** Both sides append at the top. Keep both
  entries, newest first, and relabel every entry below the new top one
  `**Last Updated (prior):**` so exactly one bare `**Last Updated:**` label
  survives — the file has been found with two, more than once. **Never edit a
  historical entry to resolve the conflict**; the chain is the record, and a
  wrong entry gets corrected by a new one, not rewritten in place.
- **`docs/tracking/open-issues.md`.** Entries are `- **…**` bullets, each with
  its own "since" date. Resolve as a union — never drop a side. Then
  re-derive the counts (`grep -c '^- \*\*'` on `open-issues.md` and on
  `open-issues-resolved.md`) and correct the "N active / M resolved" figure in
  the root `CLAUDE.md` OPEN ISSUES header; that figure has drifted from
  unreconciled edits before.
- **`docs/tracking/spec-error-log.md`.** Every entry has two surfaces — the
  `## Error Index` summary row and the full `##` body further down. A
  conflict resolved on only one surface ships half an entry. Check both.
- **`docs/tracking/CHANGELOG-src.md`.** Same append-at-top, one-bare-label
  treatment as `CHANGELOG.md` above — this is `src/CLAUDE.md`'s own version
  chain, split into its own file, and it churns on nearly every landing.
- **`docs/tracking/file-manifest.md`, `src/CLAUDE.md`, spec section files.**
  Version-history rows are a union of both sides; the version *number* itself
  is re-derived from the merged tree, never taken verbatim from either side.
- **`tools/dotnet-ci/known-failures.txt`.** Resolve shrinking-only. A merge
  conflict here is never a reason to re-add a line.

## Where this stops

- **A small, direct review comment** — a nit, a rename, a one-function ask —
  fix and push per the account-level rules; nothing repo-specific about it.
  **A review comment asking for judgment over a surface**, not a patch, is the
  one that hands off — to `adversarial-review`.
- **A football-plausibility symptom** — goal rate, shots, saves, fouls/cards,
  possession, a moved per-90 band — hand off to `match-realism-pass`,
  including its KD-W1 wiring gate. Under this repo's wire-first posture the
  symptom is often a stage that was never wired; do not tune a `[GT]` from a
  CI log.
- **A design fork, a `[GT]` value with no recorded target, or a layer-membership
  call** — convene `advisor`, or file the ERR `Open` and stop. Do not write a
  guess into an authority file.
- **Force-pushing, discarding another branch's work, rebaselining an
  acceptance band, or merging with a newly-red band** — owner call. End the
  turn and ask.

Underneath all of it: never punt, address every unresolved thread, and a
failing test in this tree has never once turned out to be an infra flake.

This file deliberately leaves the mechanics themselves — merge-conflict
resolution order, the CI-red root-cause sequence, the two postures, the
"Claude Approvals" check, mergeability notices — to that account-level
authority, and points at rather than restates three of this repo's own
skills: the gate's stages (`dotnet-gate`), the ERR entry shape
(`err-file-and-backprop`), the six-document landing sync
(`landing-close-out`).

## Reporting

One form, every time: PR number, head sha, CI run id, per-suite before →
after counts, the quarantine state **and where that state came from** (a
printed verdict, or an inspection because none was printed), what was pushed,
what remains open. If no `── Gate PASSED ──` line was printed, say so
plainly and say what was checked instead — the credible green entries in this
project's history are credible only because the red ones were stated just as
plainly.
