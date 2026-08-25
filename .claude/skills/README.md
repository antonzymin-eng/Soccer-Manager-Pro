# Project Skills

**Created:** July 31, 2026
**Purpose:** Project-scoped Claude Code skills encoding this repo's recurring workflows.

These are **project skills**, checked into the repo rather than installed under `~/.claude/skills/`,
because each one encodes conventions that live in this repo and change with it — the §5.Z pass
sequence, the ERR log format, `SNAPSHOT_SCHEMA_VERSION` discipline, the spec-promotion gates. A skill
that encodes a convention should be versioned alongside it.

Each was derived from repeated evidence in the git history and the `CLAUDE.md` landing chain, not
invented: the counts below are from the last 200 commits.

| Skill | Encodes | Observed repetition |
|---|---|---|
| `match-realism-pass` | **wiring gate** (backlog → chain → 6 checks) → measure → localize → fix → calibrate *(gate-permitting; frozen under KD-W1)* → re-measure → lock | 8 passes (§5.Z.17–§5.Z.24) in 9 days; 2 of the 8 (§5.Z.17, §5.Z.23) arrived as quality briefs over a stage that was missing |
| `snapshot-schema-bump` | the cross-tick decision + serializer/reader/test checklist | 19 schema bumps, 2 of them fixing earlier omissions |
| `err-file-and-backprop` | ERR id allocation, entry shape, spec-patch-same-commit | `spec-error-log.md` at v1.53; 2 live id collisions |
| `landing-close-out` | the six-document sync at the end of a landing | every landing; one whole reconciliation pass caused by skipping it |
| `spec-promotion` | supplement → 11-file spec set → the three gates | 11 promotions, 10 in one day |
| `dotnet-gate` | running and reporting the Linux compile/test gate | every landing |

The other most-repeated activity in this repo, `adversarial-review` (40 of the last 200 commits), is
**not** restated here — `match-realism-pass` and `spec-promotion` invoke it. It landed as a project
skill of its own in PR #283 and now lives at `.claude/skills/adversarial-review/`; `orientation`
remains account-level. Either way the rule is the same: invoke, never re-describe.

For the council and orchestrator patterns that share this directory — `advisor`, `orchestrator`, and
the agent definitions under `.claude/agents/` — see `.claude/README.md`, which is the index of what
`.claude/` contains. This file deliberately does not duplicate that list; it exists for the
*derivation* evidence above, which is the part that justifies each skill existing at all.

Several `docs/tracking/` documents are already skill-shaped prose runbooks — `cert-run-runbook.md`
most clearly — and could be converted cheaply if that workflow starts recurring.

## Skill audit, August 25, 2026

A visibility/determinism/composability pass over all ten skills. Full findings and rewrites are in
that session's record; summary of what changed:

- **Visibility.** `orchestrator` and `landing-close-out` both end in an unconfirmed `git push` — the
  first by design ("has write, commit and push authority"), the second as its final step. Both now
  carry `disable-model-invocation: true`, so a model-judged auto-trigger can no longer fire either;
  they still run instantly on explicit invocation (`/orchestrator`, `/landing-close-out`). No other
  skill here pushes, commits without being asked, or sends anything external, so no other flag changed.
- **Scripted two fixed lookups that were previously done by an agent reasoning through prose steps:**
  `err-file-and-backprop/scripts/next_err_id.sh` (grep `docs/`+`src/` for the highest `ERR-<spec>-*`
  id, print the next one — the allocation step, not the re-verify-at-merge judgment call, which stays
  prose) and `landing-close-out/scripts/check_drift.sh` (duplicate-`**Last Updated:**`-label check,
  per-doc declared-vs-git-touched dates, and the OPEN ISSUES active/resolved recount this repo's own
  changelog has hand-run with `grep -c` repeatedly — and mis-stated at least once, per the August 10
  correction in root `CLAUDE.md`).
- **Composability.** `adversarial-review`'s "Repo obligation" section and `orchestrator` steps 5 and 7
  each restated logic `err-file-and-backprop` and `dotnet-gate` already own (the id-allocation grep,
  the quarantine rule). Both now point at the owning skill instead of re-describing it, so a future
  change to either convention has one place to land rather than three.
