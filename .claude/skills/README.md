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
| `err-file-and-backprop` | ERR id allocation, entry shape, spec-patch-same-commit | `spec-error-log.md` — see its own `**Version:**` header (v2.45, 210 Error Index rows, re-derived August 18, 2026); id-collision recurrence per the skill's own examples (a design-supplement id reused before authoring finished, a branch-vs-main collision, a stale proposed range) — no live duplicate-id count re-verified as of this pass |
| `landing-close-out` | the six-document sync at the end of a landing | every landing; one whole reconciliation pass caused by skipping it |
| `spec-promotion` | supplement → 11-file spec set → the three gates | 11 promotions, 10 in one day |
| `dotnet-gate` | running and reporting the Linux compile/test gate | every landing |

The other most-repeated activity in this repo, `adversarial-review` (40 of the last 200 commits), is
**not** restated here — `spec-promotion` invokes it. *(⚠️ Corrected August 18, 2026, reviewed-findings
pass: this sentence also named `match-realism-pass`, but that skill's own SKILL.md contains no
invocation of `adversarial-review` — verified by `grep -ci adversarial .claude/skills/match-realism-pass/SKILL.md`
returning 0; its incidental uses of the word "review" are prose, not a call to the skill. Root
`CLAUDE.md` repeats the same false claim and is owned by a different pass.)* It landed as a project
skill of its own in PR #283 and now lives at `.claude/skills/adversarial-review/`; `orientation`
remains account-level. Either way the rule is the same: invoke, never re-describe.

For the council and orchestrator patterns that share this directory — `advisor`, `orchestrator`, and
the agent definitions under `.claude/agents/` — see `.claude/README.md`, which is the index of what
`.claude/` contains. This file deliberately does not duplicate that list; it exists for the
*derivation* evidence above, which is the part that justifies each skill existing at all.

Several `docs/tracking/` documents are already skill-shaped prose runbooks — `cert-run-runbook.md`
most clearly — and could be converted cheaply if that workflow starts recurring.
