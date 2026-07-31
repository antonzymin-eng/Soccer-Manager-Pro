---
name: advisor
description: Convene the Tactical Director advisory council on a decision, plan, or diagnosis BEFORE code is written. Two read-only Opus advisors — integrity (determinism, layering, spec governance) and evidence (test adequacy, football realism, sequencing) — answer in parallel and are synthesized into one recommendation. Use when facing a design fork, choosing a next lever, planning a T-phase landing, sizing a calibration, or whenever a plan is about to be committed to and a second opinion is worth the tokens. Also invoked by the orchestrator at its decision points. This is PRE-implementation advice; adversarial-review owns POST-implementation findings.
---

# Advisory Council

Two advisors, both read-only, both on Opus regardless of what the session is running on:

| Advisor | Asks | Owns |
|---|---|---|
| `advisor-integrity` | Does this respect the contracts the machine runs on? | Determinism & snapshot state · architecture & layering · spec governance |
| `advisor-evidence` | Is the claim proven, and is it the right claim? | Test adequacy · football realism · sequencing |

They exist because this project's expensive mistakes are not typos. They are plans that were
internally coherent and wrong at the premise — a lever that turned out to be worth zero goals, a
threshold that was a cliff rather than a dial, a spec formula faithfully implemented and wrong for
the sport it models. Those are catchable *before* the code, and only before the code is it cheap.

## This is not adversarial review

Keep the boundary sharp; two overlapping review surfaces is exactly the parallel-surface trap this
repo keeps filing against itself.

- **`/advisor` runs before implementation.** Input is a plan, a fork, a diagnosis, a proposed next
  lever. Output is a verdict and a set of obligations. It changes what you build.
- **`adversarial-review` runs after implementation.** Input is written code or a drafted spec. Output
  is severity-ranked H/M/L findings against an artifact that exists. It changes what you fix.

If code already exists and the question is "is this correct," that is adversarial review — say so and
stop. Do not run the council on a finished diff.

## Choosing who to convene

Convening both costs roughly double. Pick deliberately.

**Integrity alone** — the question is structural and no measurement is at stake: a new field, a new
assembly reference, whether something needs a schema bump, where a rule should live, whether an
interface has both sides specified, whether an `ERR-` id is free.

**Evidence alone** — the question is empirical and the structure is settled: is this the right next
lever, does the premise hold, is this test adequate, is the measured number football, will a sweep
give the value or only the shape.

**Both, in parallel** — the default for anything that lands code: a T-phase landing, a §5.Z-shaped
pass, a calibration, any change to the match engine. Also whenever you are unsure which applies,
since guessing wrong costs more than the second advisor does.

**Neither** — the answer is already written down. Check `path-to-playable-roadmap.md`, the owning
`docs/tracking/*-design.md`, and the OPEN ISSUES section of the root `CLAUDE.md` first. The council
is for judgment, not for lookup.

## How to convene

The advisor personas live in `.claude/agents/advisor-integrity.md` and
`.claude/agents/advisor-evidence.md`. Those files are the **single definition** of each advisor —
whichever dispatch path you take, the persona is loaded from there and never restated in a prompt.
Two copies of an advisor's instructions would drift the first time either was edited.

**Path A — native, preferred.** Spawn with the `Agent` tool, `subagent_type` set to the advisor name
(`advisor-integrity` / `advisor-evidence`). This honours the file's `model: opus` and its
`tools: Read, Grep, Glob` restriction, which makes read-only a structural guarantee rather than a
request.

**Path B — fallback, when Path A errors with `Agent type '…' not found`.** Repo-local agent
definitions are not always registered in a running session — writing the file does not necessarily
make the type resolvable until the session restarts. When that happens, dispatch to the built-in
`Explore` type with `model: "opus"` and open the prompt with:

> Read `.claude/agents/advisor-<lens>.md`. Ignore its YAML frontmatter. Adopt the persona defined in
> its body completely for this task — it defines who you are, what you attack, the output shape you
> must emit, and your tone. Also read `.claude/advisors/invariants.md` §<sections>.
> You are read-only for this task regardless of what tools you hold: do not create, edit, or delete
> any file, and do not run any command that mutates the repo or git state.

`Explore` is chosen deliberately over `general-purpose`: it structurally lacks `Edit`, `Write` and
`NotebookEdit`, so the read-only property survives the fallback for file mutation. It does retain
`Bash`, so the shell half of read-only is instructed rather than enforced — state that limitation if
it ever matters to the advice.

Try Path A first every time. It is cheaper, and if agent registration has taken effect the fallback's
persona-loading reads are pure waste.

**Convene both in a single message** so they run concurrently. Neither advisor reads the other's
output, so sequential convening doubles wall-clock for nothing. Prefer synchronous results; if the
runtime backgrounds them anyway, wait for both before synthesizing rather than acting on the first to
land.

Each advisor's prompt must carry:

1. **The decision, stated as a decision** — not "look at the shot code" but "should the next lever be
   parry placement or the Stage-0 pointQuality lottery, given contact rate is now ~72% and goals did
   not move?"
2. **The specific paths** that bear on it. They can read; they cannot guess what you meant.
3. **What is already established, and how it was established** — measured, inferred, or assumed. Say
   which. An advisor told a measurement is a measurement when it was an assumption will build on
   sand, and the evidence advisor's whole value is checking that distinction.
4. **The constraint you are actually under** — a schema bump you want to avoid, a branch already in
   flight, a gate that must stay green.

Do not paste large file contents into the prompt. They have `Read`; a path is cheaper and more
current than a quotation.

## Synthesizing

Do not concatenate the two reports. Produce one recommendation:

```
## Council — <the decision>

**Recommendation:** <what to do, in one or two sentences>

**Must do** — obligations from either advisor, deduplicated, each with its authority
- …

**Measure first** — anything evidence flagged as unestablished
- …

**Disagreement** — only if they actually conflict
- integrity: … / evidence: … / resolution: …

**Consulted:** integrity | evidence | both
```

Rules for the synthesis:

- **Obligations are cumulative, never averaged.** If integrity requires a schema bump and evidence
  requires a pre-fix failure check, the plan does both. Neither advisor gets to be overruled by the
  other on its own domain.
- **`measure first` beats `proceed`.** If evidence says the premise is unestablished, that governs
  the recommendation even when integrity is happy — a structurally perfect fix to a misdiagnosed
  problem is what §5.Z.17 delivered, at the cost of a whole pass.
- **`stop` beats everything.** Report it as the recommendation and do not soften it.
- **Report genuine disagreement; do not manufacture it.** Two advisors agreeing is the common case
  and reporting agreement as "consensus" adds nothing. Only surface a conflict when following one
  would violate the other.
- **Say who was consulted.** A single-advisor council must not read as a full one.

## What to do with the result

The council advises; it does not decide, and it has no authority to write anything. You still own the
call. Where you go against a `must do`, say so explicitly and say why — silently dropping an
obligation is how an advisory layer becomes theatre.

Carry the obligations forward into the work itself. An obligation that is agreed and then forgotten
three steps later cost tokens and bought nothing.
