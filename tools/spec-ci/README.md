# `tools/spec-ci/` — identifier-collision gate

> **Created:** August 7, 2026
> **Runs in:** `.github/workflows/ci.yml`, job `spec-hygiene`, on `push` to `main` and every `pull_request`.
> **Runtime deps:** bash, git, grep, sed, awk, coreutils. No Unity, no .NET, no network.

## What it is for

Every identifier collision this project has recorded came from the same cause: two workstreams
allocated the same id against state that moved underneath them. The four instances on file —

| # | Class | Instance |
|---|---|---|
| 1 | Supplement proposed an id already filed | three supplements in the July 2026 wave; `ERR-028-002..004` went stale the same way |
| 2 | **Branch vs. main** | `ERR-030-015` verified free on a branch, then claimed on `main` by #30's T3 landing while that branch was still open → reassigned `ERR-030-025` |
| 3 | Two branches fixing the same ERR | PR #59 + PR #60 both authored an Appendix F.0 schema for `ERR-018-005`; git kept both → `ERR-018-012..018` |
| 4 | Same id filed from two approvals | `ERR-030-007` at #42's approval and again at #32's; both took "step 7", in a sequence six approved specs cite by number |

— share one property: **verifying an id free at authoring time is not sufficient.** Class 2 is
invisible to the author, because the log moves under an open branch. This gate runs on
`pull_request`, which is the first moment both sides of that race are visible.

## Running it

```bash
bash tools/spec-ci/check-id-collisions.sh                 # the gate
bash tools/spec-ci/check-id-collisions.sh --emit-baseline # regenerate the baseline
```

Exit 0 = clean. Exit 1 = a collision that is not baselined.

## The checks

Blocking, no baseline — all clean across the tree as of this commit:

| # | Check | Source of truth |
|---|---|---|
| 1 | Duplicate `## ERR-NNN-NNN` detail entries | `docs/tracking/spec-error-log.md` |
| 2 | Duplicate `\| ERR-NNN-NNN \|` Error Index rows | same |
| 3 | Two `DOMAIN_TAG_*` constants sharing a value | `src/deterministic-sim/DeterministicSimConstants.cs` |
| 4 | Two `SubsystemOrdinals` constants sharing a value | `src/deterministic-sim/SubsystemOrdinals.cs` |
| 5 | The same `FR-XX-NNN` defined twice | `docs/specs/*/section-2*.md` |
| 6 | One `FR-XX-` prefix defined by two spec folders | same |

Blocking, baselined against `known-id-collisions.txt`:

| # | Check | Catches |
|---|---|---|
| 7 | One version number on two version-history rows | the PR #59 + PR #60 class directly |
| 8 | More than one `**Version:**` field in a file | the header-drift class in `spec-error-log.md`'s own v1.36 note |

Informational (never fails the build):

| # | Check |
|---|---|
| 9 | An ERR detail entry with no Error Index row — the log has two surfaces per entry and it is easy to land in the detail section and never scroll back. 24 pre-existing, all predating the convention. |

## Two things that were tuned, and why

**Check 3 is scoped to `public const byte` declarations.** The constants file's own version-history
comments quote every tag they allocated (`DOMAIN_TAG_LIVING_WORLD = 0x1E allocated (ERR-022-001…`),
so an unscoped grep reports four duplicates that do not exist.

**Check 7 requires a date in column 2.** A version-history row is `| <ver> | <date> | … |`, but the
spec tree is full of numeric data tables whose first column is also a decimal — sensitivity tables,
lookup tables, per-value catalogues. Without the date constraint the check reports 15 files, all but
a few of them false. With it, every hit is genuine. Both date forms in use (`2026-07-24` and
`July 24, 2026`) are matched.

## Verification

The gate was proved to fail before it was trusted. Each of the eight blocking checks was run against
an injected collision of its own class — a duplicated ERR heading, a duplicated index row, a
`DOMAIN_TAG` re-pointed onto `0x22`, an ordinal re-pointed onto `80`, a re-defined `FR-MD-001`, an
`FR-MD-` definition planted in `training-system/`, a second `| 0.4 | 2026-08-07 |` row, and a second
`**Version:**` field — and each failed with its own message, then passed again on restore. A check
that has never been observed to fail is not evidence of anything, which is this repo's own
ERR-030-014 lesson.

## What it does not catch

Worth stating plainly, so the green tick is not read as more than it is.

**A collision split across the log's two surfaces.** Checks 1 and 2 count duplicates *within* a
surface — two detail headings, or two index rows. If branch A files an Error Index row for
`ERR-030-027` and branch B files a `## ERR-030-027` detail entry for a different defect, the merge
produces one index row and one detail entry, which is exactly the shape a well-formed entry has. No
id-counting check can separate that from the real thing; only reading the two can. Check 9's
informational output is the nearest signal, and it points the other way (detail without index).

**A collision in prose.** Positional numbering — "step 7", "the (a′) insertion point" — is the class
that caused `ERR-030-007`'s worst damage, and it has no id to count. The mitigation is a convention
rather than a gate: prefer named anchors (`(c′)`) over ordinals, which is what let #30's
calendar-rebuild fix slot in without moving FR-SN-031's meaning.

**Specs predating the current FR convention.** Checks 5 and 6 read `- **FR-XX-NNN** — …` definition
lines in `section-2*.md`; a spec that does not use that shape contributes no definitions and is
silently out of scope. 171 definitions across 8 folders are covered today.

**Anything outside the tracked tree.** The file lists come from `git ls-files`; an untracked working
file is invisible.

## Baseline discipline

`known-id-collisions.txt` records duplicates that already exist. Its rules are in its header; the
one that matters: **never add a line to silence a duplicate your own branch introduced.** That is the
defect the gate exists to catch. Reconcile by union and renumber the later row.
