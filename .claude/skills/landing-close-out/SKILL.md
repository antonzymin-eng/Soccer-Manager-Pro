---
name: landing-close-out
description: >-
  Close out a landing by syncing every tracking document the change touches — the root CLAUDE.md
  header chain and OPEN ISSUES entry, src/CLAUDE.md's version bump, file-manifest.md, README.md, the
  owning design supplement's version history, and the gate-result line — in the same commit as the
  code. Use this skill at the end of any pass that lands code, a spec change, or a new assembly:
  when the work is done and about to be committed, when the user says "wrap this up", "record this",
  "update the docs", or "commit and push", and whenever an ERR was filed or a schema version bumped.
  Trigger it even when the change feels small — the documents drift precisely on the landings that
  seemed too minor to record, and reconstructing them later is a whole separate pass.
disable-model-invocation: true
---

# Landing Close-Out

This repo's tracking documents *are* the project memory — the root `CLAUDE.md` header chain is how
any agent picks up context, and it is the only place a landing's measured results live in narrative
form. When a landing skips the sync, the cost is not cosmetic: commit `9af9626` is a whole
"Documentation sync: reconcile root docs with codebase" pass that exists because two landings never
updated the root docs, and it found the assembly count, the spec counts, and the entire assembly map
stale.

Check the current drift before you start. This is a fixed lookup, not a judgment call, so it's
scripted:

```bash
.claude/skills/landing-close-out/scripts/check_drift.sh
```

It flags a duplicate bare `**Last Updated:**` label in root `CLAUDE.md` (found and fixed at least
three times — see the rule under item 1 below), reports each tracking doc's declared date next to
when it was actually last touched, and re-derives the OPEN ISSUES active/resolved counts the same way
this repo's own changelog has had to by hand, repeatedly. If `README.md` or
`docs/tracking/file-manifest.md` trails the last few landings, say so rather than adding a seventh
layer on top of a stale base.

## What to update

Work through these; skip one only when the change genuinely does not touch it, and say which you
skipped.

**1. Root `CLAUDE.md` — the header chain.** Add a new `**Last Updated:**` entry summarising the
landing: what changed, the ERR ids, the measured before → after numbers, the determinism declaration,
what locks it, and the gate result. Two conventions the file enforces on itself:

- Relabel the previous entry to `**Last Updated (prior):**`. **Exactly one bare `**Last Updated:**`
  label may exist** — this file has been found with two, which makes it self-contradictory about its
  own currency, and it has been fixed at least three times.
- Historical entries are preserved verbatim. Never rewrite an old entry to match what you now know;
  supersede it in the new entry instead.

**2. Root `CLAUDE.md` — OPEN ISSUES.** Add or update the entry for this area *in the same commit*.
Each entry keeps its "since" / opened date so staleness is visible, and a resolved entry is updated in
place rather than deleted — the original diagnosis is kept, marked RESOLVED, with what the measurement
actually showed. Several entries here are valuable specifically because the fix refuted the entry's
own diagnosis, and deleting them would have erased that.

If the pass recorded a residual it deliberately did not fix, that becomes its own entry with the
measurement attached. That recorded residual is how the next pass starts.

**3. `src/CLAUDE.md`.** Bump the version (currently in the v2.5x range) and add its own entry — this
one is file-and-symbol level: which files changed, to what version, and what the new seams are. It is
the coding guide, so it answers "what does the code look like now", not "what did we learn".

**4. `docs/tracking/file-manifest.md`.** The authoritative file inventory. Add new files, note
modified ones with their new versions, and record any new assembly. If the change added an assembly,
the assembly map in the root `CLAUDE.md` needs a row too — that table was missing a `match-analytics`
row for a full landing cycle.

**5. `README.md`.** Update the status summary and `Last Updated`. This is the most frequently skipped
document and the one most often found stale, because nothing in the build depends on it.

**6. The owning design supplement** (`docs/tracking/<topic>-design.md`). Append to its version
history: the AR rounds and their findings counts (`AR-1 1H+3M+2L → AR-2 CONVERGENCE`), the calibration
iterations, and the measured results. If the pass produced a finding worth keeping that has no other
home — a refused design, a dead surface, a negative result — it goes here.

**7. `docs/tracking/spec-error-log.md`** if an ERR was filed — see the `err-file-and-backprop` skill.

**8. `docs/tracking/path-to-playable-roadmap.md`** if the landing moved a roadmap item, and
`SPEC_INDEX.md` if a spec's status changed.

## Blast radius — check before you write the entry

No document in the sync list covers this, and it is where the last several passes lost time. Before
declaring the landing done, ask what *else* your change perturbed:

- **Scenarios with hardcoded tick windows or per-90 rate bands.** Any behaviour change moves the tick
  at which a given seed's events occur, which silently breaks instruments that were correct when
  written. The keeper-contact pass broke three this way and one escaped to CI. The goal-rate-sensitive
  locks are the usual casualties.
- **Downstream calibration.** A goal-rate change invalidates any A4a round-resolution fit and needs
  Step 0 re-run before the corpus. Say so in the entry either way.
- **The `FR-PO-052` perf baseline**, if the change adds per-tick work. That is a certified-baseline
  question on the pinned host, not something to settle on the Linux gate.

Recording "checked, nothing moved" is a real outcome and worth one sentence.

## The gate line

Every landing entry ends with the gate result, in the established form — for example:
*"Full dotnet gate: PASSED, 0 failures (whole tree green; match-engine 360 → 366)."* Per-suite counts
matter because they make a silently-skipped suite visible. Run the `dotnet-gate` skill and quote the
real numbers; never restate a previous landing's result as if it were this one's.

If the gate could not run in this environment, say that explicitly and say what was done instead —
this file has entries reading *"dotnet gate not runnable in this environment; verified by exhaustive
manual review in place of `dotnet test`"*, and that honesty is what makes the other entries
trustworthy.

## Writing the entry

Match the register of the surrounding entries: specific, measured, and willing to state what did
*not* work. The most useful entries in this file are the ones that record a null result —
*"three genuine defects, each of which had to be fixed before a save was possible at all, are worth
about one goal a match; the named lever was real and is now spent, and it was not where the mass
is."* An entry that only claims success teaches the next agent nothing.

State the determinism consequence explicitly every time: whether `SNAPSHOT_SCHEMA_VERSION` moved and
whether a new RNG stream, domain tag, draw site, or draw-order change was introduced. "No schema
change, no new RNG stream / domain tag / draw site, no draw-order change" is a sentence worth writing
even when it is boring, because its absence is ambiguous.

## Commit

One commit carrying code, spec patches, ERR entry, and doc sync together — so the record and the
change cannot separate. Then push to the designated branch with `git push -u origin <branch>`.
