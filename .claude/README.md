# `.claude/` — Advisor and Orchestrator Configuration

> **Created:** July 31, 2026
> **Status:** TOOLING CONFIG. Not a specification, not a design supplement, not on any build path.
> Nothing here is read by the sim, enters a snapshot, or affects a digest.
> **Purpose:** Two agent patterns for working this repo — an advisory **council** consulted before
> code is written, and an **orchestrator** that drives one roadmap item end to end.

## Layout

```
.claude/
├── README.md                        ← this file
├── settings.json                    ← team-wide config; the SessionStart chat-review hook
├── advisors/
│   └── invariants.md                ← routing table: trigger → question → authority
├── agents/
│   ├── advisor-integrity.md         ← determinism · layering · spec governance   (Opus, read-only)
│   ├── advisor-evidence.md          ← tests · football realism · sequencing      (Opus, read-only)
│   ├── gate-runner.md               ← run and report the gate, verbatim        (Sonnet, no repair)
│   ├── orienteer.md                 ← run the orientation sequence             (Sonnet, read-only)
│   └── doc-scribe.md                ← apply decided doc edits, verbatim       (Sonnet, no commits)
└── skills/
    ├── README.md                    ← why the six workflow skills below exist (repetition evidence)
    │
    │                                  ── agent patterns: who does the work ──
    ├── advisor/SKILL.md             ← /advisor      — convene and synthesize the council
    ├── orchestrator/SKILL.md        ← /orchestrator — drive one roadmap item to pushed
    ├── adversarial-review/SKILL.md  ← the post-implementation review loop, delegated across tiers
    │   └── scripts/findings.py      ← deterministic round bookkeeping for that loop
    ├── chat-review/SKILL.md         ← /chat-review  — session analysis; what should become a skill
    │
    │                                  ── workflow encodings: how a recurring job is done ──
    ├── match-realism-pass/SKILL.md   ← §5.Z measure → localize → calibrate → re-measure → lock
    ├── snapshot-schema-bump/SKILL.md ← cross-tick decision + serializer/reader/probe checklist
    ├── err-file-and-backprop/SKILL.md← ERR id allocation, entry shape, spec-patch-same-commit
    ├── landing-close-out/SKILL.md    ← the tracking-document sync at the end of a landing
    ├── spec-promotion/SKILL.md       ← supplement → 11-file spec set → the three gates
    └── dotnet-gate/SKILL.md          ← run and report the Linux compile/test gate
```

**Agent patterns** change who does the work; **workflow encodings** change how one person does a
recurring job correctly. Both are skills — the distinction is only about what goes wrong without them.

## The two patterns

**`/advisor`** convenes two read-only advisors, in parallel, on a decision that has not yet been
implemented — a design fork, a next-lever choice, a T-phase plan, a calibration. They answer from
different mindsets (*does this respect the machine's contracts?* / *is the claim proven, and is it the
right claim?*) and are synthesized into one recommendation with cumulative obligations.

They run on **Opus regardless of the session's model**, which is the escalation half of the design: a
cheaper session still gets Opus judgment at the decisions that matter.

**`/orchestrator`** takes one item from `docs/tracking/path-to-playable-roadmap.md` and runs the
pipeline this project already follows by hand — orient → council → design + AR to convergence →
implement → AR to convergence → full gate → tracking docs → commit → push to its own branch. It has
write, commit and push authority, defaults to **one item per invocation**, and has seven hard stops
that end the run rather than push through.

## Where the boundaries are

Three review-ish surfaces exist. They do not overlap, and keeping them from overlapping is the point:

| Surface | When | Input | Output |
|---|---|---|---|
| `orientation` (account-level skill) | Start of any task | The repo | Where you are, what's blocked |
| `/advisor` | **Before** implementation | A plan or decision | Verdict + obligations |
| `adversarial-review` (project skill, `.claude/skills/`) | **After** implementation | Written code or spec | H/M/L findings |

The orchestrator **calls** all three rather than reimplementing any of them. A second copy of
orientation or adversarial review inside the orchestrator would be precisely the parallel-surface
trap this repo has filed as a Medium finding at least four times.

## Model tiers — what runs cheap

The advisors escalate *up* to Opus from a cheap session. `gate-runner` and `orienteer` are the other
half: they push mechanical work *down* to Sonnet, so a session on Opus does not pay Opus rates to
transcribe a build log. Both are thin — each one invokes the skill that already owns the job and
returns its output. Neither restates the skill's prose, for the same reason the advisors don't
restate `invariants.md`.

| Work | Model | How to dispatch |
|---|---|---|
| A decision, before code exists | **Opus** | `/advisor` (pinned Opus regardless of session model) |
| Review passes, High fixes | **Opus** (Fable when stuck) | `adversarial-review` |
| Medium / Low fixes | **Sonnet** | `adversarial-review` already routes these |
| Running and reporting the gate | **Sonnet** | `Agent` with `subagent_type: "gate-runner"` |
| The orientation sequence | **Sonnet** | `Agent` with `subagent_type: "orienteer"` |
| Broad "where is X" greps | **Sonnet** | built-in `Explore`, `model: "sonnet"` — **no new agent**; a third search surface would be the parallel-surface trap again |
| Applying decided doc edits | **Sonnet** | `Agent` with `subagent_type: "doc-scribe"` — see the split below |

What stays on Opus, and why it is not a cost decision: `match-realism-pass`, `spec-promotion`,
`err-file-and-backprop`, `snapshot-schema-bump`, and the review passes themselves all turn on
judgment this repo has been burned by — fabricated checklist values, a capstone that asserted tick
count while every match was a 0–0 deadlock, a spec defect implemented faithfully. Those failures are
invisible to a cheaper model precisely because the output looks right.

**The doc sync splits by judgment, not by document.** `landing-close-out` owns this and states it in
full; the shape is that *deciding and composing* stay with the caller — which documents a landing
touches, the changelog and OPEN ISSUES narrative, the determinism declaration, the blast-radius
check, the gate line — while *applying already-written text* goes to `doc-scribe`: version bumps,
manifest rows, the `**Last Updated (prior):**` relabel, a README status line. The scribe is handed
exact strings and refuses an intent; it has no shell and no git, so it cannot commit, and the caller
reads `git diff` before it does. Note that this lives in `landing-close-out` and **not** in
`adversarial-review` — AR points at the close-out skill rather than restating it, which is what the
August 25, 2026 skill audit fixed across three skills.

**When delegation actually pays.** A subagent re-pays the inherited project context on every spawn,
so it wins on long or noisy work (a failing gate and its triage, a wide sweep) and roughly breaks
even on a one-shot green gate — where the real gain is context hygiene, not tokens. It is never a
win as a wrapper around a single file read.

**All three are UNVERIFIED as of August 26, 2026** — written, not yet executed. `gate-runner` and
`orienteer` additionally depend on reaching `Skill` from inside a subagent, which is untested here
(see constraint 1 below; account-level `orientation` especially). Each is written to **stop and say
so** rather than improvise a substitute if its skill is unreachable or its instruction needs a
decision, so the failure mode is a wasted spawn — not a fabricated orientation, an unrun gate
reported as green, or an invented changelog entry. Record the result here once any of them has run.

Likewise `advisors/invariants.md` is a **routing table, not a rulebook** — it names a trigger, the
question it forces, and where the real authority lives. It deliberately does not restate the rules,
because a second copy of project policy drifts the moment either copy changes.

## Known mechanical constraints

Two things were established by execution in this environment, not assumed:

1. **Skills register immediately; agent definitions register on a delay.** Writing
   `.claude/skills/<name>/SKILL.md` made the skill invocable at once. Writing
   `.claude/agents/<name>.md` did **not** make `subagent_type: <name>` resolvable straight away — but
   it did resolve later in the same session, with no restart, and with the frontmatter honoured
   (`tools: Read, Grep, Glob` enforced; the advisor confirmed it held exactly those three and no
   write or shell tool). So native dispatch is the normal route, and the `advisor` skill's fallback
   path exists to cover the registration window and any environment where it does not take effect.
   Both paths load one persona definition from the same file; there is no second copy.

2. **Subagents inherit project context, so the rules files price every spawn.** A measured advisor
   call spent ~449 K tokens across *two* tool uses — the context, not the work. That is why the
   council is two advisors rather than the six lenses originally scoped; the lenses were combined by
   mindset, not dropped. Scoping the prompt ("read at most two files") measurably helps.

   **The size figure behind that has changed by ~9x and this paragraph was stale until August 26,
   2026.** It read "the root `CLAUDE.md` is ~395 KB", which was true when written — 383 KB on
   July 28, 2026, and still growing. The August 22 `landing-history.md` split took it to 110 KB and
   later trims to **41.5 KB**, measured. With `src/CLAUDE.md` (48 KB) that is ~22 K tokens of rules,
   not ~395 KB of them.

   This is not bookkeeping: it is the number that decides whether delegating to a cheap model is
   worth a spawn at all (see **Model tiers** above), and the old figure would have priced every
   cheap-tier dispatch out of existence. Re-measure with `wc -c CLAUDE.md src/CLAUDE.md` before
   citing it again rather than trusting this line — it went stale once already.

## Changing any of this

The advisor personas are prose, and prose is the interface. Edit
`.claude/agents/advisor-*.md` to change what an advisor attacks or how it reports; edit
`.claude/advisors/invariants.md` to route a new rule. Adding a rule to the routing table without a
real authority behind it makes the table the authority, which is the one thing it must never be.
