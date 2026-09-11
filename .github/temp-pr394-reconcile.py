from pathlib import Path
import subprocess

REPO_BRANCH = "design/ux-f4-validation-setup"
MANIFEST = Path("docs/tracking/file-manifest.md")
PROTOCOL = Path("docs/design/ux-validation-protocol.md")
CHANGELOG = Path("docs/tracking/CHANGELOG.md")
WORKFLOW = Path(".github/workflows/temp-pr394-reconcile.yml")
SELF = Path(".github/temp-pr394-reconcile.py")


def run(*args, check=True):
    return subprocess.run(args, text=True, capture_output=True, check=check)


def replace_once(text, old, new, label):
    if old not in text:
        raise RuntimeError(f"missing expected anchor: {label}")
    return text.replace(old, new, 1)


# Preserve PR394's manifest-only additions before merging current main.
s = MANIFEST.read_text(encoding="utf-8")
title = "# File Manifest (Post-Migration Baseline)\n\n"
if not s.startswith(title):
    raise RuntimeError("manifest title changed")
rest = s[len(title):]
prior = rest.find("**Last Updated (prior):**")
if prior < 0:
    raise RuntimeError("manifest prior-header anchor missing")
pr394_header = rest[:prior]
ux_start = s.find("## UX Workstream Documents")
ux_end = s.find("## Planning Documents", ux_start)
if ux_start < 0 or ux_end < 0:
    raise RuntimeError("UX manifest section missing before merge")
ux_section = s[ux_start:ux_end]

run("git", "config", "user.name", "github-actions[bot]")
run("git", "config", "user.email", "41898282+github-actions[bot]@users.noreply.github.com")
run("git", "fetch", "origin", "main")
merge = run("git", "merge", "--no-commit", "--no-ff", "origin/main", check=False)
if merge.returncode != 0:
    conflicts = run("git", "diff", "--name-only", "--diff-filter=U").stdout.strip().splitlines()
    if conflicts != ["docs/tracking/file-manifest.md"]:
        run("git", "merge", "--abort", check=False)
        raise RuntimeError(f"unexpected merge conflicts: {conflicts}")
    run("git", "checkout", "--theirs", "docs/tracking/file-manifest.md")
    s = MANIFEST.read_text(encoding="utf-8")
    if not s.startswith(title):
        raise RuntimeError("main manifest title changed")
    rest = s[len(title):]
    if not rest.startswith("**Last Updated:**"):
        raise RuntimeError("main manifest newest header missing")
    rest = rest.replace("**Last Updated:**", "**Last Updated (prior):**", 1)
    s = title + pr394_header + rest
    if "## UX Workstream Documents" not in s:
        marker = "## Planning Documents"
        i = s.find(marker)
        if i < 0:
            raise RuntimeError("Planning Documents anchor missing on main manifest")
        s = s[:i] + ux_section + s[i:]
    MANIFEST.write_text(s, encoding="utf-8")
    run("git", "add", str(MANIFEST))

# Protocol v0.5: close both current Codex findings.
s = PROTOCOL.read_text(encoding="utf-8")
s = replace_once(s, "**Version:** 0.4", "**Version:** 0.5", "protocol version")

old = """If a task depends on a capability still marked `FUTURE-BLOCKED`, record the dependency and omit that
task from completion scoring until the prototype provides an honest simulated representation of the
specified future surface. Do not silently convert it to `LIVE`."""
new = """If a prescribed task depends on a capability still marked `FUTURE-BLOCKED`, Gate F remains **FAIL**
until the prototype provides an honest simulated representation of that specified future surface.
Do not run the Gate-G participant round with the task omitted from scoring, and do not silently convert
the dependency to `LIVE`. If the dependency is discovered only during a participant session, record it
as a prototype/dependency blocker, fail Gate G for that round, and return to Gate F; it is not a
participant error. Prescribed task coverage therefore has no \"honestly testable\" denominator escape."""
s = replace_once(s, old, new, "FUTURE-BLOCKED scoring")

start = s.find("| Condition | Required check |")
end = s.find("\n\nFor S0 conditions", start)
if start < 0 or end < 0:
    raise RuntimeError("Gate-E matrix anchor missing")
block = s[start:end]
lines = block.splitlines()
if lines[:2] != ["| Condition | Required check |", "|---|---|"]:
    raise RuntimeError("Gate-E matrix header changed")
expanded = [
    "| Condition | Required check | Result | Prototype/version | Evidence / finding or N/A reason |",
    "|---|---|---|---|---|",
]
for line in lines[2:]:
    if line.startswith("|") and line.endswith("|"):
        expanded.append(line[:-1] + " | | | |")
    else:
        expanded.append(line)
s = s[:start] + "\n".join(expanded) + s[end:]

marker = """For S0 conditions that do not apply to a given prototype, record `N/A` with a reason rather than
silently skipping the row."""
replacement = """Every Gate-E run must complete the `Result`, `Prototype/version`, and evidence/finding field for
every condition row. `Result` is `PASS`, `FAIL`, or justified `N/A`; a blank result/evidence field means
Gate E is incomplete and cannot pass. A `FAIL` links a stable finding ID where one exists. `N/A` must
state why the condition cannot apply to that prototype version rather than merely that it was not run.

For S0 conditions that do not apply to a given prototype, record `N/A` with a reason rather than
silently skipping the row."""
s = replace_once(s, marker, replacement, "Gate-E result guidance")

s = replace_once(
    s,
    "Record the stop reason as evidence; do not count it as participant error.",
    """Record the stop reason as evidence; do not count it as participant error. A stop caused by a missing
prescribed capability or `FUTURE-BLOCKED` dependency invalidates Gate G for that round and returns the
prototype to Gate F. The task may not be removed from the Gate-G denominator simply because the
prototype could not honestly present it.""",
    "session stop guidance",
)

old = """| Two independent participants completed the round | PASS / FAIL |
| All prescribed tasks attempted where honestly testable | PASS / FAIL |
| Back/cancel task S0-T7 attempted | PASS / FAIL |"""
new = """| Two independent participants completed the round | PASS / FAIL |
| Gate F passed with the complete prescribed S0-T1–T7 task, including back/cancel | PASS / FAIL |
| All prescribed S0-T1–T7 tasks attempted in the participant round | PASS / FAIL |
| Back/cancel task S0-T7 attempted | PASS / FAIL |"""
s = replace_once(s, old, new, "Gate-G check table")

old = """Gate G passes only when both independent participants completed the round, all honestly testable
prescribed tasks including back/cancel were attempted, there is no unresolved Blocker, and every
remaining Major has explicit owner acceptance with rationale."""
new = """Gate G passes only when Gate F had already passed with the complete prescribed task, both independent
participants completed the round, **every** prescribed S0-T1–T7 task including back/cancel was
attempted, there is no unresolved Blocker, and every remaining Major has explicit owner acceptance with
rationale. If a prescribed task proves untestable during the round, Gate G is `FAIL`; record the
prototype/dependency blocker and return to Gate F rather than excluding that task from scoring."""
s = replace_once(s, old, new, "Gate-G pass rule")

s = replace_once(
    s,
    "| Scripted self-walkthrough defined | READY | §5 covers the complete F4.6 minimum profile set plus Gate-E-only resilience checks |",
    "| Scripted self-walkthrough defined | READY | §5 covers the complete F4.6 minimum profile set plus Gate-E-only resilience checks and an auditable per-condition result/evidence record |",
    "F4 status Gate-E evidence",
)

history = "| 0.4 | September 11, 2026 | Codex review correction: split color-independent meaning into its own Gate-E condition so it remains binding even when the `Many status indicators` stress profile is N/A; density/scannability is now checked separately. |"
new_history = history + "\n| 0.5 | September 11, 2026 | Codex review corrections: prescribed tasks can no longer disappear behind an `honestly testable` qualifier — an untestable prescribed task fails Gate F/G and returns the prototype to Gate F; Gate E now has per-condition `PASS`/`FAIL`/justified-`N/A`, prototype-version, evidence and finding/reason fields, with blank rows explicitly preventing a Gate-E pass. |"
s = replace_once(s, history, new_history, "v0.4 history")
PROTOCOL.write_text(s, encoding="utf-8")

# CHANGELOG: v0.5 and findings 7-8.
s = CHANGELOG.read_text(encoding="utf-8")
start = s.find("> **Last Updated:** September 11, 2026 — **UX F4 validation protocol packet lands (PR #394).")
end = s.find("> **Last Updated (prior):**", start)
if start < 0 or end < 0:
    raise RuntimeError("PR394 CHANGELOG header region missing")
region = s[start:end]
region = region.replace("**v0.4**", "**v0.5**")
region = replace_once(
    region,
    "**Six review findings were raised across three rounds and all six are fixed (v0.1 → v0.4).**",
    "**Eight review findings were raised across four rounds and all eight are fixed (v0.1 → v0.5).**",
    "CHANGELOG finding count",
)
f6 = "**(6) Codex found that color-independent meaning was coupled to the `Many status indicators` stress row, so marking that density profile N/A could silently waive Gate E's color-independent-meaning requirement. v0.4 splits color-independent meaning into its own Gate-E condition that remains binding wherever semantic state or action meaning uses color; the many-indicators row now tests density/scannability only.**"
region = replace_once(
    region,
    f6,
    f6 + " **(7) Codex found that the `honestly testable` qualifier could let Gate G pass after a prescribed `FUTURE-BLOCKED` task was omitted, bypassing Gate F's complete-task requirement; v0.5 makes any untestable prescribed task a Gate-F/G failure and sends the prototype back to Gate F.** **(8) Codex found that the Gate-E matrix had no per-condition outcome/evidence record; v0.5 adds `PASS`/`FAIL`/justified-`N/A`, prototype/version, evidence, and finding/reason fields for every condition and makes blank rows gate-blocking.**",
    "CHANGELOG finding 6",
)
s = s[:start] + region + s[end:]
CHANGELOG.write_text(s, encoding="utf-8")

# Manifest: v0.5, findings 7-8, and current UX row.
s = MANIFEST.read_text(encoding="utf-8")
if not s.startswith(title):
    raise RuntimeError("merged manifest title changed")
rest = s[len(title):]
prior = rest.find("**Last Updated (prior):**")
if prior < 0:
    raise RuntimeError("merged manifest prior anchor missing")
region = rest[:prior]
region = region.replace("**v0.4**", "**v0.5**")
region = replace_once(
    region,
    "**Review corrections carried in the same PR across three rounds (v0.1 → v0.4), all six verified against `ux-detailed-plan.md` rather than taken on report:**",
    "**Review corrections carried in the same PR across four rounds (v0.1 → v0.5), all eight verified against `ux-detailed-plan.md` rather than taken on report:**",
    "manifest finding count",
)
tail = "At v0.4, the Codex finding that color-independent meaning disappeared whenever `Many status indicators` was N/A is closed by a standalone Gate-E color-independent-meaning condition; the many-indicators row now covers density/scannability only."
region = replace_once(
    region,
    tail,
    tail + " At v0.5, Codex's two follow-up findings are closed: a prescribed task that cannot be honestly represented now blocks Gate F/G instead of disappearing from the scoring denominator, and every Gate-E condition now carries an explicit result, prototype/version, evidence, and finding-or-N/A-reason record.",
    "manifest v0.4 review text",
)
s = title + region + rest[prior:]
ux_start = s.find("## UX Workstream Documents")
ux_end = s.find("## Planning Documents", ux_start)
if ux_start < 0 or ux_end < 0:
    raise RuntimeError("merged UX manifest section missing")
ux = s[ux_start:ux_end]
ux = replace_once(
    ux,
    "`docs/design/ux-validation-protocol.md` | F4 output — the repeatable validation operating packet (v0.4, Sep 11, 2026):",
    "`docs/design/ux-validation-protocol.md` | F4 output — the repeatable validation operating packet (v0.5, Sep 11, 2026):",
    "UX protocol manifest version",
)
ux = replace_once(
    ux,
    "a standalone Gate-E color-independent-meaning condition, moderator script, evidence templates, severity/disposition, and the Gate-G/Gate-J records |",
    "a standalone Gate-E color-independent-meaning condition, auditable per-condition Gate-E results/evidence, moderator script, evidence templates, severity/disposition, Gate-G no-skip semantics for prescribed tasks, and the Gate-G/Gate-J records |",
    "UX protocol manifest description",
)
s = s[:ux_start] + ux + s[ux_end:]
MANIFEST.write_text(s, encoding="utf-8")

# Mechanical documentation checks before committing.
subprocess.run(["python3", "tools/recurring-defect-lint.py", "--repo", "."], check=True)
subprocess.run(["python3", "tools/doc-consistency-check.py", "--repo", "."], check=True)

# Remove temporary machinery from the resulting tree.
run("git", "rm", str(WORKFLOW), str(SELF))
run("git", "add", str(PROTOCOL), str(CHANGELOG), str(MANIFEST))
run("git", "commit", "-m", "docs(ux): reconcile F4 validation review")
run("git", "push", "origin", f"HEAD:{REPO_BRANCH}")
