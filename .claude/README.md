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

The advisors escalate *up* to Opus from a cheap session. The three Sonnet agents are the other half:
they push mechanical work *down*, so a session on Opus does not pay Opus rates to transcribe a build
log. All three are thin, in two shapes — `gate-runner` and `orienteer` **invoke** the skill that
already owns the job (`dotnet-gate`, `orientation`) and return its output; `doc-scribe` is **invoked
by** one (`landing-close-out`) and applies the strings it is handed. None restates the prose of the
skill it works with, for the same reason the advisors don't restate `invariants.md`.

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

**Verification status, August 26, 2026 — executed, not merely written.** What each test established:

| Agent | Status | Evidence |
|---|---|---|
| `orienteer` | **VERIFIED** | Called the `Skill` tool with the account-level `orientation` skill; it loaded and it followed the steps (confirmed by asking the agent directly). Returned the summary in the skill's own form, correctly identified the branch and HEAD, and — the designed behaviour — **stopped to ask rather than re-authoring work it found already done**. 43.7 K tokens, 7 tool uses, 29 s. |
| `doc-scribe` | **VERIFIED, including both fixes** | Applied exact-string edits character-for-character; refused an intent instruction ("record what changed in `src/widget/`") and asked for literal text; refused to invent a table row for a file the table did not contain. Both defects found below were then **re-tested in a fresh session and both fixes hold**: the routine `(prior)` relabel now applies (producing the correct one-bare-plus-two-`(prior)` chain), and the planted `v1.6 → v1.8` skip is now flagged, citing the criterion by name. |
| `gate-runner` | **VERIFIED** | Called the `Skill` tool with `dotnet-gate`; it loaded. Found the SDK missing, installed it, and **disclosed that as an environment action**. Reported **FAILED** with per-suite counts (Failed 2 / Passed 3095 / Skipped 217) and pasted both failures verbatim. Critically it did **not** repair, did **not** add either failure to `known-failures.txt`, flagged that a red acceptance test is neither quarantined nor `Assert.Ignore`-gated, and **declined to call the failures pre-existing without a baseline** — refusing the overclaim rather than making it. |

Two defects in `doc-scribe.md` were found *by* the testing and fixed in prose:

1. It treated a second `**Last Updated (prior):**` line as a chain-breaking collision and refused the
   relabel. That is wrong — the real chain is one bare label plus arbitrarily many `(prior)` entries
   (141 in `CHANGELOG.md` today), so the relabel is the normal operation. Only a duplicate *bare*
   label is the documented defect.
2. It applied a planted version skip (a table running v1.6 → v1.8) without flagging it, then reported
   `Flags: None`.

**Both fixes are written but UNVALIDATED, and cannot be validated in the session that wrote them** —
per the snapshot corollary in constraint 1 below, the running agent kept its pre-edit definition and
a re-test simply re-ran the old prose. Re-test these two in a fresh session before trusting them.

Each agent is written to **stop and say so** rather than improvise if its skill is unreachable or its
instruction needs a decision, so the failure mode is a wasted spawn — not a fabricated orientation,
an unrun gate reported as green, or an invented changelog entry. On the evidence above that posture
holds: every refusal observed was a stop-and-report, and the one wrong refusal erred toward stopping.

**The gate run surfaced a red tree, and it is not this branch's.** This branch changes six `.claude/`
files and **zero** `.cs` / `.asmdef` / `tools/dotnet-ci` files, so neither failure originates here:

- `sim_match_engine_close_chance` — the failure root `CLAUDE.md` already records as **owner-held RED
  by decision** (August 11, 2026). Expected red; noted here only because it is enforced by the gate
  rather than quarantined, so every gate run on every branch inherits it. **Confirmed red on `main`
  itself**: CI run 476 on `2092c8a` reports `MatchEngine.Tests` Failed 1 / Passed 472 / Skipped 11 /
  Total 484 — identical to the local run, so this one is demonstrably not any branch's doing.
- `GrowthProjection_DeclineIsUnbounded_ANeverRemovedVeteranReachesEveryAttributeAtMinimum`
  (`GrowthProjectionTests.cs:334`, expected 0 but was −1) — **not** recorded anywhere. It was *added*
  by `1a34ef4` (August 24, AR round 2), whose own changelog entry claims `PlayerProgression.Tests
  149/0/0`; the suite now totals 152 with 1 failing. Either that verification claim was wrong when
  written or a later merge broke it — **which one is not established here**, and settling it needs a
  checkout and run rather than a guess. Flagged for an owner; it is outside the scope of the branch
  that found it. Note the CI log is **not** evidence either way: only the tail of run 476's log was
  retrieved, and it contains no `PlayerProgression` lines at all, so it neither confirms nor clears
  this suite on CI. The failure above is from the local gate run, which did execute it.

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

   **Corollary, established by execution August 26, 2026: a registered agent definition is
   snapshotted and does NOT hot-reload.** `doc-scribe.md` was edited mid-session, after the agent had
   registered; a subagent dispatched *afterwards* quoted the **pre-edit** bullet verbatim and reported
   the newly-added phrases absent from its own instructions. Registration is also confirmed to carry
   `tools:` and `model:` as written, and — the previously open question — **`Skill` is grantable and
   works from inside a subagent**: `orienteer` called the Skill tool with the account-level
   `orientation` skill, it loaded, and the agent followed its steps.

   The practical consequence is a real testing hazard: **you cannot validate an edit to any
   `.claude/agents/*.md` in the session that made it.** Dispatching after the edit silently runs the
   old definition and returns a result that looks like a verdict on the new one. Confirm which text
   the agent actually holds — ask it to quote its own instructions back — or re-test in a fresh
   session. Two "failed" fixes were misdiagnosed this way before the snapshot behaviour was found.

2. **Subagents inherit project context, so the rules files price every spawn.** A measured advisor
   call spent ~449 K tokens across *two* tool uses — the context, not the work. That is why the
   council is two advisors rather than the six lenses originally scoped; the lenses were combined by
   mindset, not dropped. Scoping the prompt ("read at most two files") measurably helps.

   **The size figure behind that has changed by ~9.6x and this paragraph was stale until August 26,
   2026.** It read "the root `CLAUDE.md` is ~395 KB", and that was *exact* when written: the file
   measured 397,972 bytes on July 31, 2026, this README's own creation date. The August 22
   `landing-history.md` split took it to 110 KB and later trims to **41.5 KB**, measured. With
   `src/CLAUDE.md` (48 KB) that is ~22 K tokens of rules, not ~395 KB of them.

   This is not bookkeeping: it is the number that decides whether delegating to a cheap model is
   worth a spawn at all (see **Model tiers** above), and the old figure would have priced every
   cheap-tier dispatch out of existence. Re-measure with `wc -c CLAUDE.md src/CLAUDE.md` before
   citing it again rather than trusting this line — it went stale once already.

## Changing any of this

The advisor personas are prose, and prose is the interface. Edit
`.claude/agents/advisor-*.md` to change what an advisor attacks or how it reports; edit
`.claude/advisors/invariants.md` to route a new rule. Adding a rule to the routing table without a
real authority behind it makes the table the authority, which is the one thing it must never be.
