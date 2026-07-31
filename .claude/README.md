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
├── advisors/
│   └── invariants.md                ← routing table: trigger → question → authority
├── agents/
│   ├── advisor-integrity.md         ← determinism · layering · spec governance   (Opus, read-only)
│   └── advisor-evidence.md          ← tests · football realism · sequencing      (Opus, read-only)
└── skills/
    ├── advisor/SKILL.md             ← /advisor      — convene and synthesize the council
    └── orchestrator/SKILL.md        ← /orchestrator — drive one roadmap item to pushed
```

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
| `adversarial-review` (account-level skill) | **After** implementation | Written code or spec | H/M/L findings |

The orchestrator **calls** all three rather than reimplementing any of them. A second copy of
orientation or adversarial review inside the orchestrator would be precisely the parallel-surface
trap this repo has filed as a Medium finding at least four times.

Likewise `advisors/invariants.md` is a **routing table, not a rulebook** — it names a trigger, the
question it forces, and where the real authority lives. It deliberately does not restate the rules,
because a second copy of project policy drifts the moment either copy changes.

## Known mechanical constraints

Two things were established by execution in this environment, not assumed:

1. **Skills hot-register; agent definitions may not.** Writing `.claude/skills/<name>/SKILL.md` made
   the skill invocable immediately in the same session. Writing `.claude/agents/<name>.md` did **not**
   make `subagent_type: <name>` resolvable in that session. The `advisor` skill therefore carries a
   fallback dispatch path (built-in `Explore` type + `model: opus`, loading the persona from the same
   agent file) so the council works whether or not registration has taken effect. Both paths read one
   persona definition; there is no second copy.

2. **The root `CLAUDE.md` is ~395 KB**, and subagents inherit project context. That is the reason the
   council is two advisors rather than the six lenses originally scoped — the lenses were combined by
   mindset, not dropped. Convene one advisor when the question is clearly one-sided; both is the
   default only for work that lands code.

## Changing any of this

The advisor personas are prose, and prose is the interface. Edit
`.claude/agents/advisor-*.md` to change what an advisor attacks or how it reports; edit
`.claude/advisors/invariants.md` to route a new rule. Adding a rule to the routing table without a
real authority behind it makes the table the authority, which is the one thing it must never be.
