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
│   └── advisor-evidence.md          ← tests · football realism · sequencing      (Opus, read-only)
└── skills/
    ├── README.md                    ← why the seven workflow skills below exist (repetition evidence)
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
    ├── dotnet-gate/SKILL.md          ← run and report the Linux compile/test gate
    └── steward/SKILL.md              ← PR CI triage, mergeability, and tracking-doc merge conventions
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

2. **The root `CLAUDE.md` is ~395 KB**, and subagents inherit project context. This dominates the
   cost of a convening: a measured advisor call spent ~449 K tokens across *two* tool uses — the
   context, not the work. That is why the council is two advisors rather than the six lenses
   originally scoped; the lenses were combined by mindset, not dropped. Convene one advisor when the
   question is clearly one-sided, and reserve both for work that lands code. Scoping the prompt
   ("read at most two files") measurably helps; inherited context does not shrink.

## Changing any of this

The advisor personas are prose, and prose is the interface. Edit
`.claude/agents/advisor-*.md` to change what an advisor attacks or how it reports; edit
`.claude/advisors/invariants.md` to route a new rule. Adding a rule to the routing table without a
real authority behind it makes the table the authority, which is the one thing it must never be.
