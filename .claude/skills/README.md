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
| `snapshot-schema-bump` | the cross-tick decision + serializer/reader/test checklist | 21 schema bumps (as of August 25, 2026 — run `scripts/version_table.sh` for the live number), 2 of them fixing earlier omissions |
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

- **Visibility.** Only `orchestrator` carries `disable-model-invocation: true` — it self-describes
  "write, commit and push authority" and nothing invokes it cross-skill, so nothing else needs it to
  reach it. `landing-close-out` was flagged in a first pass and **the flag was removed after testing
  it live**: it blocks the Skill tool's cross-skill invocation entirely, not just a top-level
  auto-trigger, and three skills (`err-file-and-backprop`, `match-realism-pass`, `spec-promotion`)
  invoke `landing-close-out` by name as their final step — flagging it broke all three, including from
  the flagged `orchestrator` itself (a flagged caller gains no special privilege; it hits the same
  wall). The push risk is now gated at the actual push line instead: `landing-close-out`'s `## Commit`
  section stops and asks for confirmation before `git push`, except when invoked from `orchestrator`,
  whose own push authority already covers it. `chat-review`'s artifact-republish (the other externally
  visible action in the set) stays unflagged — it's idempotent (same file path, same URL, no new
  sharing) and already gated by its own "only when asked" prose.
- **Scripted three fixed lookups that were previously done by an agent reasoning through prose steps:**
  `err-file-and-backprop/scripts/next_err_id.sh` (grep `docs/`+`src/` for the highest `ERR-<spec>-*`
  id, print the next one — the allocation step, not the re-verify-at-merge judgment call, which stays
  prose); `landing-close-out/scripts/check_drift.sh` (duplicate-label check on the changelog chain —
  `docs/tracking/CHANGELOG.md`/`CHANGELOG-src.md`, where that convention actually lives now, not root
  `CLAUDE.md` — per-doc declared-vs-git-touched dates, and an actual pass/fail comparison of the OPEN
  ISSUES active/resolved counts against root `CLAUDE.md`'s stated figure, not just a side-by-side
  printout); and `snapshot-schema-bump/scripts/version_table.sh` (every live `*_FORMAT_VERSION`/
  `SNAPSHOT_SCHEMA_VERSION` constant in `src/`, so the skill's own version table can't go stale the way
  its "1 → 19" claim already had — the real number is 21, across 12 constants, 6 of which the prose
  table didn't list).
- **Composability.** `adversarial-review`'s "Repo obligation" section and `orchestrator` steps 5, 7 and
  8 each restated logic `err-file-and-backprop`, `dotnet-gate`, or `landing-close-out` already own (the
  id-allocation grep, the quarantine rule, the six-document sync). All now point at the owning skill
  instead of re-describing it. `spec-promotion`'s `FR-` prefix collision grep was widened from
  `docs/specs/` alone to also cover `docs/tracking/` and `src/` — an unpromoted design supplement can
  already hold a prefix (`FR-DT-` does, today, in no spec), so the narrower scope would have missed
  exactly the collision the check exists to catch.

### Addendum, August 26, 2026 — cheap-tier delegation

Three Sonnet agent definitions were added (`gate-runner`, `orienteer`, `doc-scribe`) so mechanical
work stops running at Opus rates, and `landing-close-out` gained a **Delegating the mechanical half**
section splitting the doc sync by judgment rather than by document: deciding and composing stay with
the caller, applying already-written text goes to `doc-scribe`. It landed there, not in
`adversarial-review` — AR only points at `landing-close-out`, and restating the sync inside AR is the
exact composability defect the audit above had just removed from three skills.

The same pass found `.claude/README.md`'s "root `CLAUDE.md` is ~395 KB" **stale by ~9.6x** — exact
when written (397,972 bytes on July 31, 2026, that README's own creation date), **41.5 KB** measured
now, after the August 22 `landing-history.md` split. It is
corrected there with the measuring command, because it is the figure that prices every subagent spawn
and the old one would have made cheap-tier dispatch look unaffordable. Same lesson as the round
below, one file over: verify the number against the file, not against what this documentation set
said last time.

This round was caught by an independent Opus review of the first pass, which found the
`landing-close-out` flag broken by execution (not just reasoned about), the changelog-target drift,
and the stale facts above. Worth remembering for the next skill audit: verify a claim like "the header
chain lives in root `CLAUDE.md`" against the file, not against what an earlier version of this same
document said.
