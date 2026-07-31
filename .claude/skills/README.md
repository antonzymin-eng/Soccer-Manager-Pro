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
| `match-realism-pass` | measure → localize → fix → calibrate → re-measure → lock | 6 passes (§5.Z.17–§5.Z.22) in ~2 weeks |
| `snapshot-schema-bump` | the cross-tick decision + serializer/reader/test checklist | 19 schema bumps, 2 of them fixing earlier omissions |
| `err-file-and-backprop` | ERR id allocation, entry shape, spec-patch-same-commit | `spec-error-log.md` at v1.53; 2 live id collisions |
| `landing-close-out` | the six-document sync at the end of a landing | every landing; one whole reconciliation pass caused by skipping it |
| `spec-promotion` | supplement → 11-file spec set → the three gates | 11 promotions, 10 in one day |
| `dotnet-gate` | running and reporting the Linux compile/test gate | every landing |

Two of this repo's most-repeated activities are already covered by personal skills and are
deliberately **not** duplicated here: `adversarial-review` (40 of the last 200 commits) and
`orientation`. `match-realism-pass` and `spec-promotion` invoke `adversarial-review` rather than
restating it.

Several `docs/tracking/` documents are already skill-shaped prose runbooks — `cert-run-runbook.md`
most clearly — and could be converted cheaply if that workflow starts recurring.
