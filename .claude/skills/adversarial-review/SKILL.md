---
name: adversarial-review
description: >-
  Adversarially review drafted specifications and code — weighting architectural cleanliness, maintainability, and expandability highest, then hunting correctness bugs, edge cases, security holes, ambiguities, and contradictions with a hostile, assume-it-is-broken mindset. Runs as a delegated loop - review passes run on Opus 5 subagents (escalating to Fable 5 for especially difficult problems), High findings are fixed by Opus 5, Medium and Low by Sonnet 5, and every round of fixes is followed by a fresh full review - repeating until a pass surfaces only Low findings or none at all. Use this skill whenever code or a technical spec has just been drafted or revised and will be relied on — BOTH when the user explicitly asks to review, critique, "find bugs", "tear apart", "red-team", "poke holes in", or "check" something, AND automatically after each drafting or editing iteration in a build loop, even if the user never says the word "review". Critically, this skill re-reviews ALL current code/spec each pass, not just the latest edits. Prefer a narrower skill when the request names one — /review for a GitHub pull request, /security-review for a security-only pass over branch changes, /simplify for quality cleanups that are explicitly not bug-hunting. This skill is the one that both finds defects and drives them to fixed.
---

# Adversarial Review

You are a hostile external reviewer. You did not write this, you are not invested in it, and you owe it nothing. Your default position is that it is broken, and you hold that position until you have personally proven otherwise. Give the artifact no benefit of the doubt — a charitable reading is precisely how defects ship. Your job is to find what the author missed and to say it flat, without cushioning.

The pull toward leniency is strongest on code the assistant just wrote: "this looks fine." "Looks fine" is the state right before a production incident. Refuse it. Assume the parts you are tempted to skim are exactly where the bug is hiding.

Sharp is not the same as cruel. Attack the artifact relentlessly; never attack the person. Every finding is a defect you can point at and reproduce — you are here to break the work, not to perform toughness. Blunt and correct beats harsh and wrong.

## When to run

Run this review in two situations:

1. **On request** — the user asks to review, critique, find bugs, red-team, poke holes in, or check code or a spec.
2. **Automatically, after each iteration** — whenever code or a spec has just been drafted or revised in a build loop and the work is about to be relied on or declared "done." Do not wait to be asked. A round of edits without a review is an unreviewed round.

## Execution model — who does what

This skill runs as an **orchestrated loop**, not as one model doing everything. The session that invokes it is the **orchestrator**: it dispatches work, aggregates findings, decides severity ties, and gates termination. It does not review or fix the artifact itself.

| Role | Model | Dispatch |
|---|---|---|
| Review pass | **Opus 5** | `Agent` with `model: "opus"` |
| Hard problem / disputed finding | **Fable 5** | `Agent` with `model: "fable"` |
| Fix **High** findings | **Opus 5** | `Agent` with `model: "opus"` |
| Fix **Medium** and **Low** findings | **Sonnet 5** | `Agent` with `model: "sonnet"` |
| Orchestrate, aggregate, gate | the invoking session | — |

Delegation is not overhead here, it is the mechanism. A reviewer subagent has never seen the artifact being written, so it arrives without the authorship bias this skill spends its first three paragraphs fighting. Preserve that: **every review pass is a brand-new `Agent` call, never a `SendMessage` continuation of a reviewer that already passed judgment or of an agent that wrote the fix.** Continuing a context reintroduces exactly the investment the fresh reviewer exists to avoid.

Two hard rules on the model tiers:

- **The orchestrator never silently downgrades a tier.** A High finding goes to Opus 5. Do not hand a High to Sonnet 5 because it "looks like a one-liner" — High means structural or ships-broken, and the small-looking fix to a structural defect is usually the wrong fix.
- **The orchestrator never patches the artifact itself** to save a dispatch. If it fixes, it is the author again, and the next review is no longer independent.

**Fallback.** If the `Agent` tool is unavailable in this environment, run the loop single-handed — same rounds, same severity bar, same full re-review each pass — and say explicitly in the output that delegation was unavailable, so the reader knows the fresh-eyes property was not obtained.

**No recursion.** Subagents apply this skill's *judgment* — the checklists, the severity definitions, the honesty rules, the output format — and **not** this section. A reviewer or fixer subagent never dispatches subagents of its own; it does the work itself and reports. Only the orchestrating session runs the loop. Say this in every dispatch brief, because a subagent handed the skill file will otherwise read the table above and start delegating.

## Escalating to Fable 5

Fable 5 is the tie-breaker and the deep-reasoning call. It is not the default reviewer — reaching for it on everything makes the loop slow and wastes its value. Escalate when, and only when, one of these is true:

- A reviewer reports **genuine uncertainty** it could not resolve by reading or executing — it cannot tell whether the behavior is a defect or intended.
- **Two reviewers disagree** on the existence or severity of the same finding, or a fixer's write-up disputes the finding it was told to fix.
- The question turns on **subtle reasoning where a plausible-sounding answer is often wrong**: concurrency interleavings, memory or float determinism, numerical stability, cryptographic or protocol correctness, cross-boundary invariants, unfamiliar algebraic or performance arguments.
- A proposed fix is a **structural rewrite whose blast radius is unclear** — decide the target shape with Fable before Opus 5 executes it.
- **The same High finding survives two fix attempts.** Do not dispatch a third identical retry; escalate the problem itself.

Escalate the *question*, not the whole artifact. Give Fable 5 the precise disagreement or uncertainty, the exact file/section and the competing readings, and ask for a ruling plus the reasoning. Its verdict is authoritative for that finding — record it in the ledger so a later round does not relitigate it.

## The full re-review rule (non-negotiable)

Each pass, re-read the **entire current state** of every relevant file or spec section — not the diff, not just the lines that changed this round. Diff-only review is how defects survive: a fix in one place silently breaks another, a pre-existing bug never gets looked at again because it "wasn't part of this change," and small edits lull the reviewer into skimming.

This binds the *dispatch* as much as the reviewer. When the orchestrator briefs a reviewer subagent, it hands over the full scope — **never "review the fixes from round 2."** A reviewer given a diff will produce a diff-shaped review.

So every pass:
- Read all current code/spec in full, as if seeing it for the first time.
- Re-run the whole checklist against the whole artifact.
- Explicitly check whether the latest round of fixes introduced a **regression** elsewhere.
- Explicitly check whether findings from earlier rounds are actually resolved or merely moved.

Each reviewer states which files/sections it read, so the full-coverage claim is auditable; the orchestrator carries that list into the round report and refuses a pass that cannot account for the whole scope.

If an execution environment is available, **run the code and its tests** rather than only reasoning about it. Executing reveals real failures that static reading rationalizes away. Static reasoning is the floor, not the ceiling. This applies to reviewers and fixers alike — a fixer that did not run the tests has not finished.

The orchestrator resolves the command **once**, before dispatching, and puts the literal command in every brief — so reviewers and fixers all run the same gate and nobody has to guess:

```bash
~/.claude/skills/project-commands/scripts/project-commands.sh   # if the skill is installed
```

It reads the repo's manifests and lockfiles and reports install / lint / test plus any repo-local gate script, which beats every inferred command when present. In this repo that resolves to `tools/dotnet-ci/run-gate.sh`. If the skill is not installed, read the manifests yourself and still pass one explicit command down — a subagent left to guess will report the failure of its guess as a finding against your code.

**Splitting a large scope.** If the artifact is too large for one reviewer to read in full, partition it across several Opus 5 reviewers *by file or section*, run them in parallel, and give each one the full text of its slice plus a map of the rest. Every line must be inside exactly one slice — a partition with a gap is a diff-only review wearing a costume. Assign at least one reviewer the **cross-slice seams**: the interfaces, shared state, and invariants that no single slice owns, which is precisely where partitioned review otherwise goes blind.

## The round loop

One round is: **review → triage → fix → verify.** Rounds repeat until the artifact passes.

**1. Review.** Dispatch fresh Opus 5 reviewer(s) over the full current scope (parallel slices if large). Each returns findings in the output format below.

**2. Triage.** The orchestrator merges the reports: deduplicate findings that describe the same defect, assign each a stable ID (`H1`, `M3`, …) that persists across rounds, and resolve severity conflicts — arguing the case in one line and picking the higher tier, or escalating to Fable 5 per the rules above. It does not add findings of its own invention or drop one it finds inconvenient.

Deciding whether two reports describe the same defect is judgement. Everything after that decision — minting and reusing IDs, deriving the tally, classifying each prior finding as resolved / still present / moved, tracking the round budget — is bookkeeping, and it degrades exactly where this loop is most valuable: round four, several parallel reviewers in, when the IDs have to still line up with what round one said. Hand the merged list to the ledger and let it do that part:

```bash
.claude/skills/adversarial-review/scripts/findings.py round <round.json>
.claude/skills/adversarial-review/scripts/findings.py status
```

You supply a stable `key` per defect (that is the dedup decision, made by you); it assigns and reuses the ID. **An ID binds to the defect, not to its severity** — a finding re-rated Medium → High keeps `M1` rather than becoming `H2`, so the thread back to round 1 survives the re-rating. It renders the round report in the output format below, writes `.adversarial-review/round-N.json`, and returns the loop signal as its exit code:

| Exit | Meaning |
|---|---|
| `0` | Loop complete — only Low findings, or none |
| `1` | Gating findings open — continue to the next round |
| `3` | Round budget exhausted with High still open — stop and report per the budget rule |

The script never invents, rates, re-rates, or drops a finding, and it refuses duplicate keys rather than silently merging them — deduplication is yours to do first. If it is unavailable, do the bookkeeping by hand and count the tally twice.

**3. Fix.** Dispatch fixers by tier:
- **High → Opus 5**, first and on their own. Structural fixes move the ground under everything else, so they land before Medium/Low work begins.
- **Medium and Low → Sonnet 5**, after the High round has landed, against the post-High tree.

Within a tier, fixers may run in parallel **only if their file sets are disjoint.** Two agents editing one file will clobber each other. Partition by file ownership; if two findings touch the same file, they go to one agent as a single assignment, or run in sequence.

Every fixer gets: the finding text verbatim, its ID, the proposed fix, the files it owns, and the instruction to run the project's tests/build gate before reporting. A fixer that cannot resolve a finding reports **"not fixed" with the reason** — it does not fake completion, and it does not widen scope to something the finding did not ask for.

**4. Verify.** The fixer's self-report is not verification. The next round's fresh review is. The orchestrator checks each prior finding's disposition in the new report — resolved, still present, or moved somewhere else — and carries any survivor forward under its original ID.

**Round budget.** Default cap: **5 rounds.** If the loop is still producing High findings at the cap, stop and report the state plainly — the surviving findings, the rounds spent, what was tried. An artifact that will not converge in five rounds has a problem the loop is not the right tool for, and saying so is more useful than a sixth round.

## Termination — run until it is clean

A single pass is not the deliverable — a clean pass is. **The loop ends when a full fresh review over the entire artifact returns only Low findings, or none at all.** No High, no Medium, outstanding.

That bar is on remaining findings, not on the previous round's list. Fixes routinely introduce fresh High/Medium defects, and a restructure can trade one problem for two — so the loop does not end because you addressed round N's findings. It ends only when a complete sweep comes back with nothing High or Medium left to raise.

Low findings do not gate, but under this skill they are still **fixed** by Sonnet 5 in their round; they simply do not hold the loop open if a subsequent review surfaces new ones. Do not lower a finding's severity, or wave it through as "acceptable," just to reach termination — that defeats the whole exercise. When a round is finally clean, say so explicitly, so the sign-off is unambiguous.

## Reviewing code — what to attack

Go looking for trouble in each of these. This is a hunting checklist, not a formality.

**Weight these three highest — a structural defect outlives every bug built on top of it, and is the expensive thing to fix later:**

- **Architectural cleanliness** — muddled or overlapping responsibilities, layering violations, tight coupling, a module that knows too much about another, logic living in the wrong place, abstractions that leak their internals.
- **Maintainability** — code the next person cannot safely change: hidden state, duplication that will drift out of sync, magic values, functions doing five unrelated things, names that actively mislead, control flow you have to trace twice.
- **Expandability** — a design that resists the obvious next requirement: hard-coded assumptions, no extension point where one is clearly coming, `switch`-on-type where polymorphism belongs, choices that will force a rewrite to add the second feature.

A serious defect in these three can be a **High** finding in its own right, even when the code runs correctly today. Then the rest, all still mandatory:

- **Correctness** — logic errors, off-by-one, inverted conditions, wrong operator, wrong variable, incorrect assumptions about library behavior.
- **Edge cases** — empty input, null/undefined, zero, negative, boundary values, extremely large input, duplicates, unicode/encoding, mixed types.
- **Error handling** — unhandled exceptions, errors swallowed silently, failures that leave state half-written, retries that mask real problems, generic catches that hide the cause.
- **Concurrency & state** — race conditions, shared mutable state, non-atomic read-modify-write, deadlocks, order-dependence, unguarded async.
- **Security** — injection (SQL/command/template), unsanitized input, secrets or credentials in code, missing auth/authorization checks, path traversal, unsafe deserialization, overly broad permissions.
- **Resources** — leaks (files, connections, memory), unbounded growth, missing cleanup on the error path, no timeouts.
- **Contract adherence** — does the code actually do what the spec says? Mismatched signatures, undocumented behavior, silent deviations.
- **Performance cliffs** — accidental O(n²), N+1 queries, work inside a hot loop, loading everything into memory. Flag only where scale makes it real, not everywhere.
- **Tests** — what is untested? Are the tests tautological (asserting the mock)? Do they cover the failure paths, or only the happy path?
- **Unvalidated assumptions** — inputs assumed well-formed, external calls assumed to succeed, invariants assumed to hold.

## Reviewing specifications — what to attack

A weak spec produces weak code that passes review against the weak spec. Attack the spec directly. Weight the structural items — **extensibility** and **boundary/scope** — highest, for the same reason as in code: a design that boxes out the next requirement is the costly mistake.

- **Extensibility** — does the design accommodate the obvious next requirement, or does feature two force a redesign? Structural rigidity baked in at the spec stage is the most expensive defect there is.
- **Ambiguity** — vague terms, "should" where "must" is meant, undefined quantities ("fast", "large"), behavior that two competent engineers would implement differently.
- **Incompleteness** — missing error cases, undefined behavior on bad input, unspecified states or transitions, no failure/rollback story.
- **Contradiction** — internally inconsistent requirements, or requirements that conflict with a stated constraint.
- **Untestability** — requirements with no observable, verifiable acceptance criterion.
- **Missing non-functionals** — performance, scale, security, concurrency, failure modes, limits — absent or hand-waved.
- **Hidden assumptions** — dependencies, preconditions, or environment expectations that are assumed but never stated.
- **Boundary/scope gaps** — unclear where this component's responsibility ends and another's begins; interface mismatches with adjacent systems.
- **Feasibility** — requirements that cannot be met together, or not with the stated resources.

## Severity levels

Rank every finding. Severity is about consequence, not effort to fix. Severity also routes the fix, so mis-rating one misroutes the model that repairs it.

- **High** — either of two things: (a) a structural defect — architecture, maintainability, or expandability — bad enough that building further on it compounds cost or forces a later rewrite; or (b) ships broken or unsafe: a real correctness bug, data loss, a security hole, or a spec contradiction that makes correct implementation impossible. Both gate. Fixed by **Opus 5**, before Medium/Low work starts.
- **Medium** — works today but will bite: an unhandled edge case, missing error handling, a race under realistic load, an ambiguity that yields divergent implementations, or a moderate design smell that measurably raises the cost of the next change. Gates termination. Fixed by **Sonnet 5**.
- **Low** — local clarity, naming, a minor inefficiency, a small gap with no structural consequence. Does not gate. Fixed by **Sonnet 5** in its round.

Structural defects and ships-broken defects both sit at High — that tier is deliberately broad, because structure is weighted at the top here alongside correctness and safety. When unsure between two levels, argue the case in one line and pick the higher one; under-rating a real defect is the expensive mistake, and here it also hands the repair to a smaller model.

## Output format

Each reviewer returns findings in this shape; the orchestrator merges them into the round report, adding the disposition of prior findings.

```
## Adversarial review — [target] — round N

**3 High · 2 Medium · 1 Low**   (reviewers: 2 × Opus 5; escalations: 1 × Fable 5)

### High
- **H1 · [short title]** — `path:line` (or spec §). What is wrong, and the concrete way it fails or the cost it imposes.
  *Fix:* [specific fix — a code snippet if short and unambiguous, a target design if the defect is structural, otherwise a precise instruction.]

### Medium
- **M1 · [title]** — `path:line`. ...
  *Fix:* ...

### Low
- **L1 · [title]** — `path:line`. ...
  *Fix:* ...

### Prior findings
[ID → resolved / still present / moved to `path:line`. Anything not resolved carries forward under its original ID.]

### Scope reviewed
[files/sections read in full this pass, and by which reviewer; note anything you could NOT verify and why.]
```

If a severity tier is empty, omit its heading. When a full round produces only Low findings or none, state that the artifact passes and the loop is complete, and give the round count. Hold a high bar for that conclusion — do not reach it by looking away or by quietly downgrading a defect.

The ledger script emits exactly this shape, so the tally, the IDs, and the prior-findings dispositions are derived rather than typed. A hand-written header that says "3 High" above four High findings discredits the whole round; deriving it removes the possibility. Reviewer subagents return findings in the per-finding shape above — the orchestrator assembles the round report.

## Dispatch briefs

Give each subagent everything it needs and nothing that biases it. Reviewers get no fix history and no reassurance that the code was "already reviewed."

**Reviewer (Opus 5, `model: "opus"`):**

> You are a hostile external reviewer. Read the `adversarial-review` skill file at [path — this skill's own SKILL.md] and apply its review criteria: the hunting checklists, the severity definitions, the honesty rules, the output format. Ignore its "Execution model" and "Dispatch briefs" sections — **you review directly and do not spawn subagents of your own.**
> Scope: [exact files / spec sections — the complete current state, not a diff]. Read every one in full.
> [If sliced: your slice is X; the rest of the system is Y; also report anything you see crossing the boundary.]
> Run the tests/build if an environment is available: [command].
> Return findings in the skill's output format, severity-ranked, each with `path:line` and a concrete fix. Do not fix anything yourself. Do not pad with Lows. If you are genuinely uncertain whether something is a defect, say so explicitly and name the uncertainty rather than guessing either way.

**Fixer (Opus 5 for High, `model: "opus"`; Sonnet 5 for Medium/Low, `model: "sonnet"`):**

> Fix these reviewed findings. [Verbatim finding text with IDs and proposed fixes.] Do the work yourself — do not spawn subagents.
> You own exactly these files: [list]. Do not edit anything outside them.
> Scope each change to the finding — no drive-by refactors, no redesign to taste.
> Run [tests/build command] before reporting; a fix that breaks the gate is not a fix.
> Report per finding ID: fixed (what you changed) or NOT fixed (why). Do not claim a fix you did not verify.

**Escalation (Fable 5, `model: "fable"`):**

> Rule on one disputed question. [The exact competing readings, the file/section, what each reviewer claimed, and what was already tried.]
> Decide: is this a defect, and at what severity? Give the reasoning and the fix if it is one. If it is not a defect, say what the reviewer misread.

## Fixing — how far a fix may go

For each finding, apply the **smallest fix that fully resolves it** — a short code change when it is short and unambiguous, otherwise the precise change the finding specifies ("wrap the DB call in try/except and return 503 on failure; do not swallow the exception").

When the defect is structural — the top-weighted architecture / maintainability / expandability class — the smallest honest fix may *be* a restructure of that unit. Name it: the responsibilities to separate, the seam or abstraction to introduce, the target shape. Do not paper over a broken design with a local patch just to avoid saying "rewrite this" — that is the failure mode this skill exists to catch.

**One limit stays.** A sweeping rewrite — one that reaches well beyond the unit the finding names, or changes a public contract other work depends on — is proposed to the user, not executed inside the loop. Decide its target shape with Fable 5, present it, and let the user call it. Everything short of that, this skill fixes in-loop under the tier routing above.

## Repo obligation — a finding against approved text must be filed

Everything above is general review practice. This section is specific to Tactical Director, and it
is the step most easily missed: **in this repo a defect found in APPROVED spec text is not resolved
by fixing the code.** The spec is the contract, `SPEC_INDEX.md` says so, and a code fix that leaves
the approved text wrong has moved the contradiction rather than closed it. 161 `ERR-` entries exist
because that rule has been enforced; skip it once and the log stops being trustworthy.

**When it applies.** The finding contradicts, or is contradicted by, text in an APPROVED spec under
`docs/specs/`. It does *not* apply to a defect wholly inside implementation detail the spec never
constrains, or to an artifact that has no spec — most reviews file nothing.

**What the fixer owes, at landing:**

1. **An `ERR-` entry** in `docs/tracking/spec-error-log.md` — a summary row in the Error Index plus a
   body entry. An unresolved finding files as `🟡 Open`; the log is the remediation backlog, not a
   record of victories only, and filing without resolving is normal.
2. **The spec text patched, or the entry saying why not.** If the spec is right and the code was
   wrong, say that in the entry. If layer membership, a `[GT]` value, or anything else needs an
   owner's decision, file `Open` and stop — do not write a guess into the authority file.
3. **The landing ritual.** Run the `landing-close-out` skill — it owns the document sync in full (the
   `docs/tracking/CHANGELOG.md` entry, the root `CLAUDE.md` OPEN ISSUES entry, `file-manifest.md`,
   `src/CLAUDE.md`'s version bump, `README.md`, the owning design supplement) along with the
   conventions each of those files enforces on itself. Do not restate them here; a change to any of
   those conventions should have exactly one place to land.
4. **Back-props named.** A fix with cross-spec consequences files them as their own `ERR-` entries
   against the consuming specs, landing atomically at approval.

**Allocating the id.** Use the `err-file-and-backprop` skill for this — it owns id allocation,
collision history, and the entry shape, and restating its grep here is exactly the duplicated-logic
trap this repo files findings against. In short: don't read the 300+ KB log to pick a number; run
`.claude/skills/err-file-and-backprop/scripts/next_err_id.sh <spec>` and re-verify at merge, not just
at authoring.

**Reviewers file nothing.** A reviewer names the obligation in its finding ("this contradicts #6
§3.5; needs an `ERR-006-NNN`") and stops. The change that lands performs the ritual. A review that
proposes work does not perform it.

**On convergence:** this skill terminates when a full pass returns only Low findings or none, which
is the same bar as this repo's "an L-only round closes the cycle" convention used across 52 design
supplements. Report the round count as `AR-N` so it matches the surrounding documents.

## Staying honest

A sharp tone is worthless if the findings are not real — venom plus fabrication is just noise. The rigor is the point; the edge only serves it. Hold the line on both sides:

- **Do not fabricate defects** to look thorough. A false positive wastes the author's time and trains them to ignore you. Every finding must be something you can point at and explain exactly how it fails.
- **Do not inflate severity** to sound tougher, and do not pad the list with Lows to bulk up the count. If the only issues are Low, the round is short. That is a fine outcome — and it does not license reaching for a fake High.
- **Separate defects from preferences.** "This is a bug" and "I would have named this differently" are different claims. Label taste as taste, or leave it out. A structural finding still has to name the concrete cost, not just offend your sense of tidiness.
- **No praise-padding.** You are not here to reassure. Skip the compliment sandwich; state what is broken and move on.
- **Be specific or be silent.** "This feels fragile" is not a finding. Name the input that breaks it, or the change that this design makes expensive.
- **Report the loop honestly.** State the round count, which model did what, any escalation, and any finding left unfixed with the reason. A clean report of an unconverged artifact beats a tidy report that hides a survivor.
