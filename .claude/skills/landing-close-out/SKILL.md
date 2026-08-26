---
name: landing-close-out
description: >-
  Close out a landing by syncing every tracking document the change touches — the
  docs/tracking/CHANGELOG.md header chain, root CLAUDE.md's OPEN ISSUES entry, src/CLAUDE.md's version
  bump (and its own CHANGELOG-src.md chain), file-manifest.md, README.md, the owning design
  supplement's version history, and the gate-result line — in the same commit as the code. Use this
  skill at the end of any pass that lands code, a spec change, or a new assembly:
  when the work is done and about to be committed, when the user says "wrap this up", "record this",
  "update the docs", or "commit and push", and whenever an ERR was filed or a schema version bumped.
  Trigger it even when the change feels small — the documents drift precisely on the landings that
  seemed too minor to record, and reconstructing them later is a whole separate pass.
---

# Landing Close-Out

This repo's tracking documents *are* the project memory — the `docs/tracking/CHANGELOG.md` header
chain is how any agent picks up context, and it is the only place a landing's measured results live in
narrative form. When a landing skips the sync, the cost is not cosmetic: commit `9af9626` is a whole
"Documentation sync: reconcile root docs with codebase" pass that exists because two landings never
updated the root docs, and it found the assembly count, the spec counts, and the entire assembly map
stale.

Check the current drift before you start. This is a fixed lookup, not a judgment call, so it's
scripted:

```bash
.claude/skills/landing-close-out/scripts/check_drift.sh
```

It flags a duplicate bare `**Last Updated:**` label in the changelog chain (found and fixed at least
three times — see the rule under item 1 below), reports each tracking doc's declared date next to
when it was actually last touched, and checks the OPEN ISSUES active/resolved counts against a direct
recount — the same comparison this repo's own changelog has had to make by hand, repeatedly, and got
wrong at least once (the August 10, 2026 correction in root `CLAUDE.md`). If `README.md` or
`docs/tracking/file-manifest.md` trails the last few landings, say so rather than adding a seventh
layer on top of a stale base.

## What to update

Work through these; skip one only when the change genuinely does not touch it, and say which you
skipped.

**1. `docs/tracking/CHANGELOG.md` — the header chain.** Root `CLAUDE.md` itself carries no header
chain any more; the chain was split out on July 31, 2026 and root `CLAUDE.md` only holds OPEN ISSUES
now. Add a new `**Last Updated:**` entry summarising the landing: what changed, the ERR ids, the
measured before → after numbers, the determinism declaration, what locks it, and the gate result. Two
conventions the file enforces on itself:

- Relabel the previous entry to `**Last Updated (prior):**`. **Exactly one bare `**Last Updated:**`
  label may exist** — this file has been found with two, which makes it self-contradictory about its
  own currency, and it has been fixed at least three times. (The same rule applies independently to
  `docs/tracking/CHANGELOG-src.md`, item 3 below.)
- Historical entries are preserved verbatim. Never rewrite an old entry to match what you now know;
  supersede it in the new entry instead.

**2. Root `CLAUDE.md` — OPEN ISSUES.** Add or update the entry for this area *in the same commit*.
Each entry keeps its "since" / opened date so staleness is visible, and a resolved entry is updated in
place rather than deleted — the original diagnosis is kept, marked RESOLVED, with what the measurement
actually showed. Several entries here are valuable specifically because the fix refuted the entry's
own diagnosis, and deleting them would have erased that.

If the pass recorded a residual it deliberately did not fix, that becomes its own entry with the
measurement attached. That recorded residual is how the next pass starts.

**3. `src/CLAUDE.md`.** Bump the version (currently in the v2.5x range). Its own entry — the
`**Last Updated:**` chain and the `VERSION HISTORY` table, file-and-symbol level: which files changed,
to what version, and what the new seams are — lives in `docs/tracking/CHANGELOG-src.md`, per
`src/CLAUDE.md`'s own pointer at the top of the file, not inline. It answers "what does the code look
like now", not "what did we learn".

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

## Delegating the mechanical half

This sync splits cleanly, and the split is not by document — it is by whether the step needs
judgment. **Deciding and composing stays with you; applying can go to Sonnet.**

Keep, always — these are the steps that go wrong invisibly:

- **Which documents this landing touches**, and which are genuinely skippable (the sync list above is
  a checklist, not a script — item 8 in particular is a judgment about whether a roadmap item moved).
- **The narrative content**: item 1's changelog entry, item 2's OPEN ISSUES entry, item 6's supplement
  history. The register these are written in — measured, specific, willing to record a null result —
  is the whole value of the chain.
- **The determinism declaration** and the **blast-radius** check.
- **The gate line**, which comes from a real run, not from a previous landing.

Delegate, once the text exists and you are handing over exact strings:

- Item 3's `src/CLAUDE.md` version bump and its `CHANGELOG-src.md` file-and-symbol rows.
- Item 4's `file-manifest.md` rows, and the assembly-map row if one is needed.
- Item 5's `README.md` status summary and `Last Updated` line.
- The `**Last Updated:**` → `**Last Updated (prior):**` relabel, per item 1.
- Appending a version-history block you have already written.

Dispatch with `Agent`, `subagent_type: "doc-scribe"`. Two conditions make this safe, and it is not
safe without them:

1. **Hand over exact text, never an intent.** "Add a manifest row for `src/foo/Bar.cs` v1.0 reading
   `<string>`" is delegable; "record what changed in `src/foo/`" is you asking a cheap model to
   re-derive the landing, which is the decision you were supposed to keep.
2. **Read the diff yourself before committing.** `git diff` on the delegated files, every time. The
   scribe cannot run the drift script's judgment calls and cannot tell a stale base from a current
   one — the reconciliation pass at `9af9626` exists because nobody checked.

If the change is small enough that writing the exact strings costs more than making the edits, make
the edits. The delegation pays on a wide sync (six-plus documents, a new assembly, a manifest with
many rows), not on a two-line date bump.

## Commit

One commit carrying code, spec patches, ERR entry, and doc sync together — so the record and the
change cannot separate.

Commit yourself — **never delegate the commit**. The scribe has no commit authority for this reason:
one agent must hold the whole record at the moment it is written down.

**Invoked standalone** (nothing else is about to commit): make that commit yourself, then **stop
before pushing** — report the commit and the branch, and ask for confirmation. Push only on an
explicit go: `git push -u origin <branch>`.

**Invoked from `orchestrator`** (its step 8): do not commit here. Leave the doc-sync edits staged and
uncommitted — `orchestrator` step 9 makes the single commit covering code and docs together, and owns
the push under its own authority. Two commits from one landing would split the record the "one commit"
rule above exists to keep whole.
