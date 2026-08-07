---
name: chat-review
description: Analyze recent Claude Code sessions for this repo and surface only actionable findings — repeated asks that should become skills, workflow and script improvements, token-cost reductions, and lessons worth recording. Use when the user asks to review their sessions, asks what should become a skill, asks where token spend is going, asks to refresh or republish the chat-review dashboard, or invokes /chat-review. Do NOT run this on a schedule or produce a digest when nothing tripped a threshold.
---

# Chat review

Measure this repo's Claude Code sessions, then report **only what is actionable**.

## The contract

Silence is a valid, common, and correct result. `tools/chat-review.py` emits a
finding only when a threshold trips; if `findings` is empty, say so in one line
and stop. Do not manufacture observations to fill a report, and do not restate
findings the user has already acted on — check the dashboard's current state
first.

## Run it

```bash
python3 tools/chat-review.py --repo . --out /tmp/chat-review.json --since 14
```

Flags: `--since N` (days), `--projects PATH` (defaults to `~/.claude/projects`),
omit `--out` to print to stdout. No network and no model call — it reads the
`usage` fields Claude Code already writes to each session's JSONL, so token and
cost figures are **measured, not estimated** — including the cache-write TTL
split, which matters more than it looks: 1-hour-TTL writes price at 2.0× input
where 5-minute writes are 1.25×, and Claude Code sessions here write 1h. The
script priced everything at 1.25 until August 3 and understated its own sessions
by 27%. The one exception is rules-file size, which is a `chars/4` estimate; for
an exact count run:

```bash
ant messages count-tokens --model claude-opus-5 \
  --message '{role: user, content: "@./CLAUDE.md"}' --transform input_tokens -r
```

## The SessionStart hook

`.claude/settings.json` runs the analyzer in `--hook` mode at every session start.
It prints **nothing at all** when no threshold trips — no empty JSON, no "all
clear" — because a monitor that greets you every session trains you to ignore it.
When something does trip it emits a one-line-per-finding summary (capped at five,
no evidence arrays, no session dump) as `additionalContext`, plus a single
`systemMessage` line for the user.

The hook is a notice, not an instruction. Do not act on what it reports unless
the user asks, and do not re-raise a finding they have already declined. To
disable it, delete the `SessionStart` block or run `/hooks`.

## Read the output

`findings[]` is sorted by severity then magnitude. Each carries `category`
(`skill-candidate`, `script-candidate`, `token-cost`), a `metric`, and
`evidence` drawn from the transcripts. Categories map to actions:

| Category | Action |
|---|---|
| `skill-candidate` | Draft a `SKILL.md` under `.claude/skills/<name>/`. Name the trigger conditions, not just the behavior — a skill that does not fire is worth nothing. |
| `script-candidate` | Add to `tools/`, then reference it from the skill that should invoke it. |
| `token-cost` | Propose the specific edit and quantify the saving before making it. |
| `workflow` | Tracking-hygiene drift in `docs/tracking/open-issues.md` — a stale title, a duplicated entry. Re-read each against its owning source before acting; the analyzer detects the *shape*, not the truth. |

**Two rules keep the noise down, and both were added after they misfired.** A repeated
prompt shape or tool shape counts only if it spans **two or more distinct sessions** —
nine `Edit` calls inside one session is the shape of that session's work, not a habit
worth scripting. And harness-generated user turns (`Continue from where you left off.`,
interrupt markers, compaction resumes, slash-command echoes) are excluded before
counting; left in, they produced a skill suggestion for a message the user cannot stop
sending.

## Report

Lead with the outcome: how many actionable findings, and the single biggest one.
Then one short paragraph per finding — what tripped, the measured number, and the
proposed fix. Do not paste the JSON. If a finding proposes a skill, offer to
write it rather than writing it unasked.

## Refresh the dashboard

The findings render at the published `chat-review` artifact. To update it, edit
`docs/design/chat-review.html` and re-publish with the **same file path** so the
URL is preserved:

```
Artifact(file_path="docs/design/chat-review.html", url="<existing artifact URL>")
```

Find the URL with `Artifact(action="list")` if it is not to hand. Publishing to a
new path mints a new URL and orphans the old dashboard — don't.

## Repo obligations

This repo records its own history in a specific way, and a review pass is subject
to it. If a finding leads to a code or spec change, that change carries the
landing ritual documented in `CLAUDE.md` (header entry, `spec-error-log.md` if an
`ERR-` is filed, `file-manifest.md`, `src/CLAUDE.md` version bump). A review that
proposes changes does **not** perform that ritual — the change that lands does.

## What this skill does not do

It does not read the conversation for correctness, quality, or tone. It measures
shape and cost: what recurred, what it cost, and what could be encoded. Judgement
about whether a given piece of work was *good* is not something a transcript
parser can supply, and pretending otherwise would make the numbers less trusted.
