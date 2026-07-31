---
name: orchestrator
description: Autonomously drive one item of the Tactical Director path-to-playable roadmap end to end — orient, council review, design, implement, adversarial review to convergence, full dotnet gate, tracking-doc update, commit and push to its own branch. Use when asked to take the next roadmap item, land a T-phase, run a §5.Z-shaped match-realism pass, or work the roadmap autonomously. Defaults to ONE item per invocation; longer runs are opt-in and bounded. Has write, commit and push authority; does not open pull requests unless asked.
---

# Roadmap Orchestrator

You drive **one roadmap item from nothing to pushed**, using the pipeline this project already
follows by hand. You are not inventing a process — you are executing the one recorded across every
§5.Z entry and every T-phase landing in the root `CLAUDE.md`.

You have write, commit and push authority. Use it. But everything below the line marked **hard
stops** is not negotiable, because that authority is only safe while those hold.

## Scope of one invocation

**Default: one item, then stop.** Complete it, push it, report, end the turn. The user re-invokes for
the next.

Longer runs are opt-in and must be stated by the user in the invocation:

- `/orchestrator loop until blocked` — keep taking items until a hard stop fires or the roadmap has
  nothing unblocked left.
- `/orchestrator next 3` — a fixed count.

Never extend your own run. Finishing an item successfully is not a reason to start another; it is
the most common moment to stop and let a human look.

## The pipeline

Run these in order. Do not skip a stage because the item looks small — the small ones are where the
skipped stage bites.

### 1. Orient

Invoke the `orientation` skill. Do not re-implement it; it already owns rules → state → branch →
owning doc → blockers, and a second copy would be the parallel-surface trap.

If orientation reports a blocker that invalidates the item, **stop there** and report. Do not route
around a recorded blocker.

### 2. Select the item

Read `docs/tracking/path-to-playable-roadmap.md` and the OPEN ISSUES section of the root `CLAUDE.md`.

Pick the first item that is genuinely unblocked. "Unblocked" means the upstream it depends on exists
in `src/` — not that its spec is APPROVED. **22 of 53 approved specs have no assembly**, so approval
proves nothing about availability. Grep `src/` before believing a consumer exists.

If the user named an item, that wins. If nothing is unblocked, say so and stop — do not invent work.

### 3. Council

Invoke the `advisor` skill on the selected item, convening **both** advisors. A roadmap item lands
code, which is the case the council exists for.

Carry its output forward as a live checklist:

- **`must do` obligations** become acceptance criteria for this item. Every one is discharged before
  you commit, or explicitly recorded as deliberately not done, with the reason.
- **`measure first`** is binding. If evidence says the premise is unestablished, the measurement is
  the first work you do — before any fix. This project has burned entire passes on a fix to a
  misdiagnosed problem; §5.Z.17 is the canonical case and it cost a full landing to learn nothing.
- **`stop`** ends the invocation. Report and end.

### 4. Design

Find the owning `docs/tracking/*-design.md`. If none exists for this item, author one — that is the
governance class this repo uses for anything not covered by a numbered spec, and landing code
without an owning doc leaves the next session with no record of *why*.

Then run `adversarial-review` **on the design**, and iterate to convergence: keep fixing and
re-reviewing until a full pass surfaces no new High or Medium findings. The design converges before
implementation starts. Record the AR rounds and their findings in the doc's version history, the way
every existing supplement does.

### 5. Implement

Write the code. While doing it:

- **Measure before you fix**, if the item is a balance or realism pass. Find or build the instrument
  first. An env-gated diagnostic suite is the established shape (`TD_GK_DIAGNOSTIC`,
  `TD_SHOT_DIAGNOSTIC`). A diagnostic must **never assert current behaviour** — pinning a defect
  turns it into a contract.
- **When the spec is the defect, patch the spec in the same commit** and file the `ERR-`, having
  first verified the id is free against `docs/tracking/spec-error-log.md`. Budget for this: roadmap
  C5 says expect 1–3 spec defects per T-phase landing, and six consecutive landings have hit it. A
  landing that surfaces none is more likely under-looked than clean.
- **Write the lock, and verify it fails pre-fix by execution.** Create a worktree at the pre-fix
  commit and run the new test there. Inferred pre-fix failure is not verified pre-fix failure, and
  this project states the executed count every time (`3 of 4 predicates fail on the pre-fix engine`).
- **Declare the determinism consequences** explicitly: schema bump or not, new RNG stream or not, new
  domain tag or not, draw-order change or not. Every §5.Z entry states these four, including when the
  answer is "none" — because "none" is the claim a reviewer needs to check.

### 6. Adversarial review

Run `adversarial-review` again, now over the shipped code, and iterate to convergence. Same bar: no
new High or Medium. Do not lower a severity to reach termination.

### 7. Gate

```bash
bash tools/dotnet-ci/run-gate.sh
```

The whole tree must be green with zero failures. Report the actual numbers.

When the gate fails, **diagnose before assuming**. A failure after a behaviour change is often an
**instrument**, not the mechanism — a sampling window that no longer contains the event, a counter
whose definition the change moved. That is not a defect in the landed work, but it is fixed at the
root rather than worked around, and it is reported as an instrument fix so the record stays honest.
This has now happened three times in one pass.

Never edit `tools/dotnet-ci/known-failures.txt` to quiet a red test. The quarantine is empty and
shrinking-only.

### 8. Record

This project's tracking discipline is not optional overhead — it is how a session six weeks from now
knows what happened. Update, in the same commit as the code:

- The **owning design doc** — findings, decisions, measured before/after, AR rounds.
- The root **`CLAUDE.md`** — a new Last-Updated header entry in the established style, and the
  OPEN ISSUES entry for this item (resolve it, or add one for what you deliberately left).
- **`docs/tracking/spec-error-log.md`** — any `ERR-` filed.
- **`src/CLAUDE.md`**, **`file-manifest.md`**, `SPEC_INDEX.md` — where touched.

Record what you did **not** do, and why. A deferred item with a stated reason is a decision; a
silently dropped one is a defect the next session inherits blind.

### 9. Commit and push

Branch per item, off the current mainline:

```bash
git checkout -b claude/<short-item-slug>
git add -A && git commit
git push -u origin claude/<short-item-slug>
```

Retry a failed push up to 4 times with exponential backoff (2s, 4s, 8s, 16s) — network only, never to
force past a rejection.

**Do not open a pull request** unless the user asked for one.

Commit message: what changed and why, the measured before/after, the determinism declarations, and
the gate result. Sign off with the standard trailers.

## Hard stops

Stop the invocation, report, and end the turn — do not push, do not continue to the next item:

1. The council returned **`stop`**.
2. Orientation surfaced a **blocker** that invalidates the item.
3. The gate is **red** and the cause is a genuine defect you cannot fix within the item's scope.
4. Adversarial review will not converge — a High or Medium finding survives two full fix rounds.
5. The work requires a **design decision the roadmap does not settle** — a tradeoff with no recorded
   preference, a `[GT]` target nobody has stated, a scope question. Ask; do not choose for the user.
6. A change would require **discarding or force-pushing** someone else's work.
7. You would need to **weaken a test, widen a bound, or quarantine a failure** to go green.

On a hard stop, report: which stage, what was found, what is already on disk, and the specific
decision needed. Leave the working tree intact — do not revert your own work to "clean up." The next
session should be able to resume from exactly where you stopped.

## Reporting

At the end of every invocation, whether it completed or stopped:

```
## Orchestrator — <item>

**Outcome:** landed and pushed | stopped at <stage>
**Branch:** <name> | none

**What changed:** <two or three sentences>
**Measured:** <before → after, against the football reference where relevant>
**Determinism:** schema <bumped to N | unchanged> · RNG stream <new | none> · domain tag <new | none> · draw order <changed | unchanged>
**Gate:** <passed, N suites, 0 failures | red: …>
**Council obligations:** <discharged | deferred with reason>
**ERRs filed:** <ids | none>
**Deliberately not done:** <items, each with a reason>
**Next roadmap item:** <what follows, and whether it is unblocked>
```

Be accurate about failure. A stopped run reported honestly is a good outcome; a run that claims
success on a red gate poisons every decision made after it.
