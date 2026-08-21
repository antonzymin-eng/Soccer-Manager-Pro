#!/usr/bin/env python3
# doc-claim-check.py — execute the verification commands this repo's documents
# quote, and diff the stated value against what they actually print.
#
# Created: August 18, 2026
# Purpose: close the defect class adversarial-review rounds 6-8 kept finding and
#          could not stop finding by review alone.
#
# TWO CHECKS RUN, not one, and both gate:
#   CHECK 1 — the claim checker. Find every claim of the form "this command
#             returns N", run the command, compare. This is the bulk of the
#             file and everything above the CHECK 2 banner belongs to it.
#   CHECK 2 — dangling identifier references inside spec code fences. In src/ a
#             missed rename is a build error; in a spec's worked example nothing
#             binds, so it dangles silently. See its own banner further down for
#             why it is deliberately narrow. It shares this file because it
#             shares the surface set and the exit code, and because a second
#             script would be a second thing to remember to run.
#
# WHY THIS EXISTS
# ---------------
# Rounds 4-8 diagnosed one recurring shape: **verification prose is never itself
# verified.** The repo writes claims in a machine-checkable form constantly —
# "(`grep -c '^- \*\*' docs/tracking/open-issues.md` → 18)" — and nothing ever
# re-ran them. Measured instances of the resulting defect:
#
#   * root CLAUDE.md cited a grep as proof of a claim that grep REFUTES, and did
#     so on the day it was written (round 8, rules H2).
#   * Spec #20 §9.2 Q-04 ratified "three distinct IDs" against an actual six, in
#     the checklist file whose own header carries a fabrication prohibition.
#   * §9.1 C-02's recorded value stopped reproducing inside the very commit that
#     claimed to have re-run every §9.1 and §9.2 command.
#   * §3.2.1 offered "218 occurrences (`grep -rn ... | wc -l`)" as proof; the
#     command returned 243, because the fix that wrote the sentence added 25.
#
# None of these is subtle. All survived multiple hostile human-grade review
# rounds, because checking them means running seventeen commands by hand and a
# reviewer under time pressure reads the number instead. A tool cannot get bored.
#
# WHAT IT DOES NOT DO, STATED UP FRONT
# ------------------------------------
# This tool verifies claims whose command prints a SINGLE INTEGER. That is a
# deliberate floor, not an oversight: it is the class that has actually bitten
# (counts, tallies, cardinalities), and it is the class where "what the stated
# value should be" is unambiguous. A claim whose command prints prose, a table,
# or a multi-line report is COUNTED AND NAMED as unverified rather than silently
# skipped — the round-5/6 lesson is that a checker's silent skips are where the
# next defect hides, so every claim this tool declines to check is printed.
#
# It recognises FOUR claim SHAPES, listed in CLAIM_SHAPES and printed by every
# run so the reader never has to trust this comment:
#   1. command, then the value       "`cmd` → 18", "`cmd` returned 218"
#   2. command, colon, value         "`cmd`: **0**"
#   3. value, then the command in parentheses    "8 scripts (`ls tools/*.py`)"
#   4. value, an attribution clause, the command
#                                    "60 files — re-derived by `ls … \| wc -l`"
#                                    "35 assemblies via `ls -d src/*/ \| wc -l`"
#
# THE RESIDUAL BLIND SPOT IS THE POINT OF THIS PARAGRAPH, and it is no longer
# described by naming instances someone happened to notice. Rounds 9 and 12
# each published a blind spot as prose, and the prose then drifted: round 12
# added shape 3 and left this section saying it "is not matched at all, and so
# is not counted among the declines either", which the very next run refuted by
# printing that exact claim in the declined list. So the blind spot is DERIVED
# now, not written down: every backticked span that reads as a command, has an
# integer near it on the same line, and binds to NO shape is counted and named
# in the `unrecognised-shape` decline bucket. Read that bucket, not this
# comment, for what the tool cannot see — and if a claim shape is worth
# learning, it will be sitting in that list under its own file and line.
#
# The same rule governs the answer side. ANSWER_KINDS holds exactly one entry,
# and the single-integer floor above is real; a claim whose command prints
# prose, a table or a multi-line report lands in `not-a-single-integer`, which
# is a count of what a second answer kind would be worth.
#
# SAFETY
# ------
# Commands come from documents, so they are untrusted input, and `ci.yml` runs
# this tool on `pull_request` — so document text reaches a CI runner. Every
# pipeline segment must match ALLOWED_CMDS; anything with redirection, command
# substitution, chaining, or an unlisted binary is refused and counted, the
# invocation never goes through a shell, and it runs under a timeout.
#
# Being on ALLOWED_CMDS is NOT by itself the safety property, and round 9 (H1)
# found the header claiming it was: "read-only by construction (no writing
# command is on the list)" was false, demonstrated by `sed -i` rewriting a file
# in the working tree while the tool printed PASS. Several genuinely read-only
# tools carry a write or execute escape hatch — `sed -i`, `find -delete` /
# `-exec`, `python3 -c`, `sort -o`, `rg --pre`, `git -c`, `awk 'BEGIN{system()}'`,
# `uniq IN OUT`.
#
# Rounds 9, 14 and 15 each falsified this section's then-current safety claim by
# reproduction, so state the recurring root error before the rules: **an escape
# hatch enumerated by name, on an argument that is itself a language, is a list
# of the hatches someone happened to think of.** Round 9 named `system` and
# `getline` for awk; round 15 executed `awk 'BEGIN{print "touch X" | "sh"}'`,
# which uses neither. Round 14 put it as "the validation ran on a SHAPE, not on
# the argv that actually executes"; round 15 adds its sibling — the validation
# ran on the SPELLING, not on the option (`sort -o` was denied while
# `sort -oFILE` wrote the file).
#
# The property therefore rests on five things together, in this order of
# importance:
#   1. ALLOW-LISTS WHEREVER THE ARGUMENT IS A LANGUAGE. `sed` (round 9) and
#      `python3` (round 15, H2) are DROPPED — for python3 the old rationale
#      ("a `.py` file the checkout already contains — CI runs those anyway")
#      was simply false: CI runs four NAMED scripts, and on `pull_request` the
#      checkout IS the pull request's head, so a PR that adds `tools/pwn.py`
#      and a claim quoting it had arbitrary code executed with write access.
#      `awk` is kept — 2 of the 3 claims this tool executes are awk — with its
#      program allow-listed (AWK_ALLOWED_CALLS) rather than blacklisted, plus a
#      flat refusal of `|` and `@` in any awk token.
#   2. DENIED_FLAGS / DENIED_FLAG_PREFIXES compared on the option CORE, so an
#      attached value (`-oFILE`, `-O./p.sh`) or an un-enumerated `--long=value`
#      cannot respell a denied hatch past the check (round 15, H3).
#   3. GIT_READONLY holding only subcommands that cannot destroy anything —
#      `branch` and `tag` are gone, because `-D`/`-d` delete refs (round 15,
#      H4), and `--output` is denied because a diff writes a file with it.
#   4. PATH CONFINEMENT on every operand, checked after glob expansion: a
#      command may read the checkout and nothing else. Without it, `grep -c .
#      /etc/passwd` was a one-integer read oracle over the host (round 15, M3).
#   5. RESOURCE BOUNDS: no shell, a wall-clock timeout AND a hard cap on how
#      much a segment may print, because a timeout does not bound memory —
#      one document line drove the checker to 587 MB and `cat /dev/zero` would
#      OOM-kill the runner first (round 15, M1). NUL is refused up front and
#      ValueError caught, so document text cannot abort the scan (M2).
#
# Every one of those refusals is COUNTED AND NAMED in the printed output. That
# is not politeness: a silent refusal is indistinguishable from a pass, which
# is the defect this whole tool exists to deny itself.
#
# Exit codes. Two kinds of non-zero, and the distinction is load-bearing:
#   0 = every executable claim reproduced its stated value, no dangling
#       identifier, and the run actually looked at what it is supposed to.
#   1 = A FINDING ABOUT A DOCUMENT. Either at least one stated value does not
#       reproduce (CHECK 1) or at least one spec code fence references an
#       identifier its own file does not declare (CHECK 2). The old exit table
#       named only the first, while the dangling-identifier path had returned 1
#       since the day it was written.
#   2 = THIS TOOL COULD NOT DO ITS JOB, so its result is not a verdict on any
#       document: a usage error, a named surface missing from the tree, a
#       surface glob matching no file, or fewer claims executed than
#       MIN_EXECUTED_CLAIMS. It outranks 1, because with the surface set broken
#       the mismatches that were found are not the mismatches that exist.
#       Before this existed, deleting 8 of the 9 named surfaces printed eight
#       MISSING SURFACE lines, checked one claim, and exited 0 with PASS.

import argparse
import importlib.util
import os
import pathlib
import re
import subprocess
import sys
import tempfile
import threading


def _load_consistency():
    """Import tools/doc-consistency-check.py (hyphenated name, so via importlib).

    Round 13: this tool needs the SAME answer to "which bytes of this document
    are a dated record?" that the citation checker already computes. Importing
    it rather than restating it is deliberate — two tools disagreeing about
    where the frozen header chain ends would mean one excusing a record the
    other reports, and a second copy of a definition is the duplicate-claim
    defect this repo keeps filing. The cost is a hard dependency between the
    two checkers; both are steps of the same CI job, so a break in either
    already fails that job."""
    q = pathlib.Path(__file__).with_name("doc-consistency-check.py")
    spec = importlib.util.spec_from_file_location("doc_consistency_check", q)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


DCC = _load_consistency()

# Read-only binaries only. A command is refused unless EVERY pipeline segment's
# argv[0] is here. `git` is further restricted below to read-only subcommands.
ALLOWED_CMDS = {
    "grep", "egrep", "fgrep", "rg", "ls", "find", "wc", "cat", "head", "tail",
    "sort", "uniq", "awk", "cut", "tr", "git", "basename",
    "dirname", "echo", "printf", "stat", "diff",
}
# Round 15 (H4): `branch` and `tag` were on this list as READ subcommands, and
# `git branch -D x` / `git tag -d x` DESTROY a ref — both proven here deleting
# a ref in a fixture repo under a printed PASS. The argument-less read forms
# (`git branch`, `git tag`) are not worth that surface, and nothing in this
# corpus quotes one, so both are gone: a future claim quoting them is DECLINED
# AND NAMED, which is the safe direction.
GIT_READONLY = {
    "log", "grep", "show", "ls-files", "diff", "rev-parse", "rev-list",
    "cat-file", "describe", "status", "blame",
}
# Shell metacharacters that make a string more than a simple pipeline. `\x00`
# is here for a different reason from the rest (round 15, M2): subprocess
# raises ValueError on a NUL in argv, which is not a SubprocessError, so an
# embedded NUL in a backticked span aborted the ENTIRE scan with a traceback —
# every later claim and the whole dangling-identifier check silently never ran.
# Refused up front here; ValueError is also caught in run_pipeline as a
# backstop, because a crash is the one outcome that hides defects wholesale.
FORBIDDEN = re.compile(r"[;&><`\n\x00]|\$\(|\|\|")

# ---------------------------------------------------------------------------
# Round 9, H1: allow-listing argv[0] is NOT sufficient, and the header used to
# claim it was ("read-only by construction — no writing command is on the
# list"). Several read-only tools carry a write or execute ESCAPE HATCH behind
# a flag, and `sed -i` was demonstrated rewriting a file in the working tree
# while this tool printed PASS. The commands come from documents; on a
# `pull_request` CI trigger that is untrusted input reaching a runner.
#
# Two mechanisms, because the hatches are of two kinds:
#   (1) FLAG hatches — refusable exactly, by name. Listed per binary below.
#   (2) SCRIPT hatches — the argument IS a language, so no flag list can
#       contain them. `sed` (`w file`, `s///w file`) is therefore DROPPED from
#       the allow-list entirely; it has no use anywhere in the corpus, and a
#       future sed claim is DECLINED AND NAMED, which is the safe direction.
#       `awk` is a language too, and round 15 (H1) proved the consequence:
#       `awk 'BEGIN{print "touch X" | "sh"}'` executes a shell command using
#       NEITHER of the two words this file blacklisted, and FORBIDDEN does not
#       reject a single `|` because that is the pipeline separator. awk is
#       nonetheless KEPT — 2 of the 3 claims this tool actually executes are
#       awk, so dropping it would surrender two thirds of the live coverage —
#       and its program is ALLOW-listed instead of escape-blacklisted: see
#       AWK_ALLOWED_CALLS below. `python3` is DROPPED (H2) for the same reason
#       as sed: its argument is a language too, and "the script must be an
#       in-repo .py file" is not a restriction at all when the checkout under
#       test is a pull request's own head — the PR simply adds the .py file.
#
# Round 15 also generalised HOW a flag is matched: see _option_cores(). A
# deny-list keyed on the exact spelling an option happened to be written in
# misses the same option carrying an attached value (`sort -oFILE`,
# `git grep -O./p.sh`), which is not a new hatch but the same one respelled.
DENIED_FLAGS = {
    # exact flags
    "find": {"-exec", "-execdir", "-ok", "-okdir", "-delete", "-fprint",
             "-fprint0", "-fprintf", "-fls"},
    # `--compress-program` (round 15, H5) runs an arbitrary program whenever a
    # sort spills to disk, which `-S 1` forces; proven creating a canary here.
    "sort": {"-o", "--output", "--compress-program"},
    "uniq": set(),           # guarded by operand count below (uniq IN OUT writes)
    # `-l`/`--load`, `-i`/`--include` and `-E`/`--exec` are gawk's extension
    # and source-loading flags — the same class as `-f`, added in H5's
    # one-pass audit of every remaining allow-listed binary rather than one
    # report at a time.
    "awk": {"-f", "--file", "--source", "--exec", "-l", "--load", "-i",
            "--include", "-E"},
    "head": {"-f", "--follow"},
    "tail": {"-f", "-F", "--follow"},
    "rg": {"--pre", "--pre-glob", "--hostname-bin", "--generate"},
    # `git` is split in two: see GIT_GLOBAL_DENIED. A flag denied ANYWHERE goes
    # here — `git grep -O <cmd>` hands the match list to a command, and
    # `--output` (a diff option `log`/`diff`/`show` all accept) WRITES a file:
    # `git diff --output=WROTE_THIS HEAD` created it here under a printed PASS.
    "git": {"-O", "--open-files-in-pager", "--output"},
}
# Denied only BEFORE the subcommand, where git parses its own global options.
# The same spellings after it belong to the subcommand and are harmless — and
# refusing them there broke both of this repo's only executable claims, whose
# command is `git grep -c …` (round 9: caught because the fix was re-measured
# against the live corpus rather than accepted on its own reasoning).
GIT_GLOBAL_DENIED = {"-c", "-C", "--exec-path", "--upload-pack"}
# Prefix forms of the same hatches: `--output=x`, `--pre=x`, `-c=x`.
DENIED_FLAG_PREFIXES = {
    "sort": ("--output=", "--compress-program="),
    "rg": ("--pre=", "--pre-glob=", "--hostname-bin="),
    "git": ("--exec-path=", "--upload-pack=", "--output="),
    "awk": ("--source=", "--file=", "--load=", "--include="),
}
# awk program text that escapes the process. FORBIDDEN already removes every
# redirection character, so these two are what is left OF THE NAMED FORMS —
# and round 15 (H1) proved that naming forms is the wrong shape of rule for a
# language: `print x | "sh"` is arbitrary command execution containing neither
# word. Kept as a backstop only; the load-bearing rule is now the allow-list
# below.
AWK_ESCAPES = re.compile(r"\b(?:system|getline)\b")

# ALLOW-LIST for awk program text (round 15, H1). Every FUNCTION CALL an awk
# token makes must be named here; an unknown name is DECLINED AND NAMED, so a
# gawk builtin nobody on this project has heard of cannot become the next
# hatch the way `system` was. Paired with two flat refusals that no allow-list
# of call names can express:
#   * `|` anywhere in an awk token — the output pipe `print x | "cmd"` and the
#     input pipe `"cmd" | getline` are BOTH command execution, and tokenize()
#     deliberately keeps a QUOTED `|` literal so it reaches the program intact
#     (an unquoted one is a pipeline separator and never lands in a token).
#   * `@` anywhere in an awk token — gawk's `@load` / `@include`.
# What is left after those, plus FORBIDDEN's refusal of `>` `<` `` ` `` `;`
# `&` anywhere in the string, is arithmetic and printing.
AWK_CALL = re.compile(r"([A-Za-z_][A-Za-z0-9_]*)\s*\(")
AWK_ALLOWED_CALLS = frozenset((
    # keywords that take a parenthesised clause
    "if", "while", "for", "do", "else", "return", "function", "func",
    # string / math builtins that cannot leave the process
    "length", "substr", "index", "split", "sub", "gsub", "match", "sprintf",
    "printf", "print", "int", "sqrt", "exp", "log", "sin", "cos", "atan2",
    "rand", "srand", "tolower", "toupper",
))

# Round 9 (M1). CLAIM deliberately matches any backticked span before an arrow,
# because that is how this repo writes a count claim; most such spans are
# IDENTIFIERS, not commands (`SEASON_SAVE_FORMAT_VERSION` **5 → 6**). This
# separates the two so an unrunnable COMMAND is named while a version bump is
# not mistaken for one: command-shaped means "has arguments, and its head token
# is a path or a plausible binary name".
_HEAD_SHAPE = re.compile(r"(?:\.{1,2}/)?[A-Za-z0-9_][A-Za-z0-9_.-]*"
                         r"(?:/[A-Za-z0-9_.-]+)*\Z")
# Binaries this tool does NOT run but must still RECOGNISE, so that a claim
# quoting one is counted and named instead of silently skipped. `python3` and
# `sed` are here because they were dropped from ALLOWED_CMDS (rounds 15 and 9):
# dropping a binary without adding it here would move its claims from a NAMED
# decline to an invisible one, which is the decline contract failing in the
# direction this file calls its worst.
_KNOWN_BINARIES = frozenset((
    "dotnet", "curl", "wget", "ps", "bash", "sh", "zsh", "make", "npm", "npx",
    "node", "python", "python3", "pip", "docker", "jq", "tee", "xargs", "sed",
    "unity", "dos2unix", "pwsh", "powershell",
))


def command_shaped(cmd, head):
    """True when this backticked span reads as a shell command rather than an
    identifier — the discriminator that lets an unlisted BINARY be named
    without also naming every version-bump arrow in the corpus."""
    if " " not in cmd.strip():
        return False
    if not _HEAD_SHAPE.match(head):
        return False
    return ("/" in head or head.endswith((".py", ".sh"))
            or head in _KNOWN_BINARIES)


TIMEOUT_S = 60

# A NEGATED claim states what the command does NOT return — this repo writes
# "the plain `grep …` no longer returns 218" precisely to record a superseded
# figure. Reading that as an assertion inverts the document's meaning and
# reports a defect where the author already did the right thing. Found by this
# tool's own first run against the live tree, which is the complement-testing
# lesson rounds 5-8 kept re-learning: a matcher tuned on the instances that
# motivated it will misread their opposite.
#
# Round 16 (M5): the alternation used to include the BARE words `not`, `was`,
# `never` and `n't`, and an excusal is the one outcome that costs coverage
# silently — the claim is never run at all. Those words saturate this repo's
# prose, and the window is 40 raw characters either side, so the excusal fired
# on sentences that negate something else entirely. Proven before the fix:
#
#     This was decided by the owner and is not in dispute: 99 assemblies
#     (`ls -d src/*/ | wc -l`)
#
# — a present-tense claim wrong by a factor of 33 — was declined as
# negated-or-historical, 0 claims executed, PASS. The alternation is now
# restricted to phrases that can only be negating THE CLAIM: a bare `not` may
# negate anything in the sentence, but `no longer returns` cannot negate
# anything else. Every verb-bound form is spelled out rather than left to a
# bare adverb, so widening this list is a deliberate act.
# `pre-fix` is deliberately NOT in the bare alternation, and that is a
# correction to this round's own first draft, made on evidence. It fails M5's
# own test — "phrases that can only negate a claim" — because in this corpus it
# is a noun modifier: #20 §3's v1.4 row reads "re-derived against the pre-fix
# commit instead (`git grep -c 'CROSS-PENDING' 9b841d1^ …` → 218)", a LIVE
# claim pinned to an immutable revision, and with bare `pre-fix` on the list
# the new look-back window (M4) excused it — silently costing a quarter of
# every executed claim on the tree. Kept only where it modifies a measurement.
NEGATOR = re.compile(
    r"\b(?:no longer|superseded|instead of|rather than|before this fix|"
    r"pre-fix\s+(?:figure|value|count|number|reading|measurement|baseline)|"
    r"(?:never|used to|previously|no longer|did not|does not|didn't|doesn't)\s+"
    r"(?:return(?:s|ed)?|report(?:s|ed)?|print(?:s|ed)?|yield(?:s|ed)?|"
    r"give(?:s|n)?|gave|show(?:s|ed|n)?|output(?:s|ted)?|emit(?:s|ted)?|"
    r"count(?:s|ed)?|find(?:s)?|found|read(?:s)?|was|is|be)"
    r")\b", re.I)

# ---------------------------------------------------------------------------
# DATED RECORDS (round 13). A claim in an APPEND-ONLY record states what a
# command returned AT THE TIME. When the underlying figure later moves, the
# record is still correct and the tool must not fail CI on it.
#
# This is not hypothetical and it is not someone else's mistake: at round 12
# the pass writing the CHANGELOG entry quoted Spec #20's own drift-prone
# example verbatim, with its value, INTO the chain — which would have failed
# this gate on a correct historical entry the day the 36th assembly landed. It
# was caught and the example was described instead of quoted, but "remember not
# to write that" is not a mechanism.
#
# The model is `doc-consistency-check.py`'s, ported rather than reinvented, and
# kept to its two load-bearing properties:
#   * a mismatch inside a record region is EXCUSED, not skipped — the claim is
#     still executed, and the excusal is COUNTED AND PRINTED, so "this
#     historical figure no longer reproduces" stays visible without gating;
#   * an explicit CURRENCY ASSERTION pierces the excusal. A record that says
#     the command returns N *now* is making a present-tense claim wherever it
#     sits, and is reported like any other.
# Regions come from that module (frozen header chain, log body, archive) so the
# two tools cannot disagree about which bytes are frozen.
CURRENCY_ASSERTION = re.compile(
    r"\b(?:now|currently|today|at\s+HEAD|as\s+of\s+(?:today|HEAD))\b", re.I)
CURRENCY_RADIUS = 120


def dated_record_regions(rel, text):
    """Spans of `text` that are dated records by structure."""
    spans = list(DCC.record_regions(rel, text))
    chain = DCC.frozen_chain_span(text)
    if chain:
        spans.append(chain)
    return tuple(spans)


def currency_asserted(text, start, end):
    """True when the claim reasserts that the value is current — bounded to its
    own line so a marker from a neighbouring sentence cannot pierce for it."""
    lo = max(text.rfind("\n", 0, start) + 1, start - CURRENCY_RADIUS)
    nl = text.find("\n", end)
    hi = min(nl if nl != -1 else len(text), end + CURRENCY_RADIUS)
    return bool(CURRENCY_ASSERTION.search(text[lo:hi]))


# Surfaces scanned. Kept explicit rather than globbed: this tool executes what it
# finds, so the scanned set is a security boundary and must be a decision.
SURFACES = (
    "CLAUDE.md",
    "src/CLAUDE.md",
    "README.md",
    "docs/tracking/open-issues.md",
    "docs/tracking/file-manifest.md",
    "docs/tracking/CHANGELOG.md",
    "docs/tracking/CHANGELOG-src.md",
    "docs/tracking/spec-error-log.md",
    "docs/tracking/path-to-playable-roadmap.md",
)
SURFACE_GLOBS = ("docs/specs/*/section-*.md", "docs/specs/*/appendices.md",
                 "docs/specs/*/section-9-approval-checklist.md")

# ---------------------------------------------------------------------------
# COVERAGE FLOOR (round 16, H9).
#
# A run that checked NOTHING used to print PASS and exit 0. MISSING SURFACE was
# a print statement: it incremented no counter and gated nothing, proven with 8
# of the 9 named surfaces deleted — 1 claim executed, PASS, exit 0. The globbed
# half had no missing concept at all, and every executed claim on this tree
# comes from ONE globbed folder, so renaming that folder would have taken the
# executed count to zero with not one line of output changed. The imported
# sibling states the opposite rule for itself in terms ("a named surface
# MISSING from the tree is an ERROR, not a skip"), and this file's own header
# calls a vacuous pass "the failure class this project files as High".
#
# Two gates, because a surface list and a claim count fail differently: a
# surface can vanish while the count holds up, and the count can collapse
# (a matcher regression, a document rewritten) while every surface is present.
#
# HOW MIN_EXECUTED_CLAIMS IS DERIVED — it is not a magic number, it is a
# measurement, and it is re-derived by running the tool:
#
#     python3 tools/doc-claim-check.py --repo .
#
# and reading the "claims executed and compared" line. The floor is set BELOW
# that figure by MIN_EXECUTED_SLACK, so that ordinary document edits — a
# sentence rephrased, a claim deleted with the paragraph it lived in — do not
# turn a green tree red, while a matcher regression or a renamed surface
# folder, which take the count to zero or near it, do.
#
# WHEN IT LEGITIMATELY CHANGES: coverage GROWING is the normal case (a new
# shape, a new allow-listed binary, a new claim written) — re-derive and raise
# the floor in the same commit, so the new coverage is protected too. Coverage
# SHRINKING is the case that must not be waved through: lower this number only
# with the reason recorded in the Version History row beside it, because
# lowering it silently is how the vacuous-pass failure class comes back.
MIN_EXECUTED_SLACK = 2
# Re-derived 2026-08-21 by the invocation above: 6 executed and compared.
MIN_EXECUTED_CLAIMS = 6 - MIN_EXECUTED_SLACK

# ---------------------------------------------------------------------------
# SEAM 1 — ANSWER KINDS (round 16, H11).
#
# "The command prints a single integer" used to be hard-wired at six separate
# sites inside scan(): the value sub-pattern, the `int(...)` conversion, the
# single_integer() read-back, the `not-single-int` decline bucket, the `!=`
# comparison and the FAIL text. A second answer kind — a pair, a version
# string, "prints nothing" — therefore meant editing all six, which is how a
# floor becomes permanent. It is ONE object now: add a class here, name it on
# a claim shape, and nothing in scan() changes.
#
# An answer kind owns four things:
#   parse(text, start, end) -> (value, None) | (None, (bucket, reason))
#       the STATED value, read out of the document. It may DECLINE — see the
#       approximate-or-range rule below, which is a property of what a stated
#       integer means, not of any particular claim shape.
#   read(output) -> value | None          the command's own answer
#   matches(stated, got) -> bool          the comparison
#   describe(stated, got) -> str          the FAIL line
# ---------------------------------------------------------------------------

# Round 16 (H8). A number is not a stated value merely by being an integer near
# a command. Two classes are refused OUTRIGHT rather than compared, because in
# both the document is right and a comparison could only fabricate a finding:
#   * a RANGE endpoint — "→ 2-4" against a true 3 is a correct sentence, and
#     binding the 2 fails it;
#   * an APPROXIMATION — "~30 files (`ls … | wc -l`)" is deliberately not an
#     exact claim, and this repo writes ~ and ≈ constantly.
# Both are DECLINED AND NAMED (`approximate-or-range`), never silently dropped:
# a claim this tool cannot compare is part of the coverage statement.
RANGE_BEFORE = re.compile(r"\d\s*(?:-|–|—|to)\s*$")
RANGE_AFTER = re.compile(r"\s*(?:-|–|—)\s*\d")
APPROX_BEFORE = re.compile(
    r"(?:[~≈≥≤]\s*|\b(?:about|roughly|approximately|approx|circa|around|"
    r"nearly|almost|at least|at most|up to|no more than|no fewer than)\s+)$",
    re.I)


class SingleIntegerAnswer:
    """The one answer kind this tool has ever had: the command prints exactly
    one integer, and the document states it.

    That floor is deliberate and is stated in the header: it is the class that
    has actually bitten here (counts, tallies, cardinalities) and the class
    where "what the stated value should be" is unambiguous."""

    name = "single-integer"
    bucket = "not-single-int"
    unreadable = "output is not a single integer"

    def parse(self, text, start, end):
        before = text[max(0, start - 24):start]
        after = text[end:end + 8]
        if RANGE_BEFORE.search(before) or RANGE_AFTER.match(after):
            return None, ("approximate-or-range",
                          "stated value is one endpoint of a RANGE — a range "
                          "is not a single stated value and comparing an "
                          "endpoint would fabricate a finding")
        if APPROX_BEFORE.search(before):
            return None, ("approximate-or-range",
                          "stated value carries an approximation marker "
                          "(~ / ≈ / about / roughly) — the document is not "
                          "claiming an exact figure")
        return int(text[start:end].replace(",", "")), None

    def read(self, out):
        return single_integer(out)

    def matches(self, stated, got):
        return stated == got

    def describe(self, stated, got):
        return "document says %d; command returns %d" % (stated, got)


SINGLE_INTEGER = SingleIntegerAnswer()
ANSWER_KINDS = (SINGLE_INTEGER,)


# ---------------------------------------------------------------------------
# SEAM 2 — CLAIM SHAPES (round 16, H11).
#
# A shape is one recognised way of WRITING a claim. Each entry owns its own
# regex, its own negation window and its own "this match is not a claim at all"
# rule, so the next shape is one more entry in CLAIM_SHAPES rather than one
# more inline branch in scan(). That is not tidiness: the two shapes that existed
# before this round needed incompatible negation logic, it was expressed as two
# hand-written loops, and one of them was silently left un-updated for a whole
# round — round 16's M4, and the reason the seam is shaped this way.
#
# CLAIM_SHAPES order is a PRECEDENCE order: a command span is one claim,
# whichever shape binds it first.
# ---------------------------------------------------------------------------

# How far a negation window looks back from the start of a match, bounded to
# the claim's own line so a negator from a neighbouring sentence cannot excuse.
NEGATION_LOOKBACK = 40


class ClaimShape:
    """One recognised claim shape."""

    def __init__(self, name, regex, answer=SINGLE_INTEGER):
        self.name = name
        self.regex = regex
        self.answer = answer

    def find(self, text):
        return self.regex.finditer(text)

    def negation_window(self, text, m):
        """The text a NEGATOR may appear in to mark this claim historical.

        Round 16 (M4). This is ONE implementation shared by every shape,
        deliberately. The look-back was added at round 12 to the value-first
        shape only, because that was the shape whose negator demonstrably
        precedes the match; the forward shape kept passing the text AFTER its
        gap, which can only ever contain the arrow and the digits, so that
        argument was dead code and a forward-shape claim whose negator sits
        BEFORE the backtick was never recognised as historical. Proven, same
        document and same negator: "before this fix, `grep -c … f` → 4" was
        reported as a MISMATCH while the value-first phrasing of the identical
        sentence correctly declined.

        Both directions are covered by looking back from the start of the
        match (bounded to the line) and adding the shape's own gap — which for
        a forward shape is the text between the command and the arrow, and for
        a leading-value shape is the text between the value and the command.
        A shape needing something else overrides this method; sharing it by
        default is what stops the asymmetry being reintroduced silently."""
        back = max(text.rfind("\n", 0, m.start()) + 1,
                   m.start() - NEGATION_LOOKBACK)
        return text[back:m.start()] + m.group("gap")

    def rejects(self, text, m):
        """A reason this match is NOT a claim, or None.

        Distinct from a decline: a decline says "this claim exists and I could
        not check it" and is counted. This says "there is no claim here"."""
        return None


class LeadingValueShape(ClaimShape):
    """A shape whose stated value is written BEFORE the command.

    Round 16 (H8): this direction can bind a number that is not a stated value
    at all, because the prose to the left of a command is where this repo
    writes its dates. `August 18, 2026 (`cmd`)` parsed as stated value 2026,
    and a correct sentence produced "document says 2026; command returns 3",
    exit 1 — the tool fabricating the finding it exists to prevent. The live
    tree already extracts value=2026 in two places; they escape only because
    those backticked spans hold ERR ids rather than runnable commands, which is
    luck, not a rule.

    A date is refused rather than declined: there is no claim in
    "August 18, 2026", so counting one would overstate what was given up."""

    def rejects(self, text, m):
        before = text[max(0, m.start("value") - 32):m.start("value")]
        if DATE_YEAR_BEFORE.search(before):
            return "the integer is the YEAR of a date, not a stated value"
        if DATE_DAY_BEFORE.search(before):
            return "the integer is the DAY of a date, not a stated value"
        return None


_MONTH = (r"(?:Jan(?:uary)?|Feb(?:ruary)?|Mar(?:ch)?|Apr(?:il)?|May|Jun(?:e)?|"
          r"Jul(?:y)?|Aug(?:ust)?|Sep(?:t|tember)?|Oct(?:ober)?|Nov(?:ember)?|"
          r"Dec(?:ember)?)")
# "August 18, 2026 (`cmd`)" — the comma-space form H8 names. The month name is
# required, so a genuine "18, 2026" list of counts is untouched.
DATE_YEAR_BEFORE = re.compile(
    _MONTH + r"\s+\d{1,2}(?:st|nd|rd|th)?\s*,\s*$", re.I)
# "August 18 (`cmd`)" — the same date with the year elided.
DATE_DAY_BEFORE = re.compile(_MONTH + r"\s+$", re.I)

# Verbs that introduce a command's own answer. Round 16 (H7): this list used to
# be `returns?|reports?|prints?|yields?|gives?` — five present-tense forms with
# a `\b` after each, which makes "returned", "reported" and "printed"
# UNMATCHABLE. This repo records measurements in the past tense constantly
# ("`git grep -c … \| awk …` returned **218**"), so the omission was not a
# corner: those spans bound to no shape and were counted in no bucket.
#
# The complement was tested as well as the widening, and it removed a verb the
# first draft included. `read` is NOT a reporting verb in this repo: it is how
# this corpus describes what a DOCUMENT said, almost always the refuted value —
# root CLAUDE.md's own design-supplement sentence is "…(60 files — re-derived
# by `ls docs/tracking/*-design.md \| wc -l`…); this literal read \"42\" while
# the true count was 60". With `read` on the list, shape 1 bound the 42, beat
# shape 4's correct binding of the 60 on precedence, and FAILED a sentence
# whose entire subject is that 42 is wrong. A widened matcher that starts
# reporting a correct document is worse than the gap it closed.
_REPORT_VERB = (
    r"returns?|returned|reports?|reported|prints?|printed|yields?|yielded|"
    r"gives?|gave|shows?|showed|outputs?|emits?|emitted|finds?|found|"
    r"counts?|counted")

# SHAPE 1 — command, then the stated value: "`cmd` → 18", "`cmd` returned 218".
# The gap is deliberately tight — round 8's H4 showed that a loose lookahead
# binds across unrelated clauses.
CLAIM = re.compile(
    r"`(?P<cmd>[^`\n]{4,200})`"          # the command
    r"(?P<gap>[^`\n]{0,40}?)"            # short gap, no intervening code span
    r"(?:→|->|\b(?:" + _REPORT_VERB + r")\b)"
    r"[^0-9`\n]{0,18}"
    r"\*{0,2}(?P<value>\d[\d,]*)\*{0,2}",
    re.I)

# SHAPE 2 — command, then a COLON, then the stated value:
# "`python3 tools/recurring-defect-lint.py --repo .`: **0 ERROR**".
# Round 16 (H7). The colon must be adjacent to the closing backtick — a colon
# anywhere inside shape 1's 40-character gap would bind across an unrelated
# clause, which is the defect round 8 filed. Adjacency makes the form
# unambiguous, and it is the form this repo's gate lines actually use.
CLAIM_COLON = re.compile(
    r"`(?P<cmd>[^`\n]{4,200})`(?P<gap>\s*:\s*)"
    r"\*{0,2}(?P<value>\d[\d,]*)\*{0,2}",
    re.I)

# SHAPE 3 — the VALUE-FIRST shape: "8 scripts (`ls tools/*.py`)", "35 (`ls -d
# src/*/ \| wc -l`)". Round 9 (L2) named this as unrecognised AND uncounted and
# stopped there; round 12 closed it, because the live instances are exactly the
# drift-prone kind — Spec #20 §5.4.5 states the assembly count this way, in
# APPROVED text, and it goes stale the day the 36th assembly lands. The
# parenthesis is required: it is this repo's idiom for "and here is how to
# check it", and without it the pattern would bind any number near any
# backtick.
CLAIM_VALUE_FIRST = re.compile(
    r"\*{0,2}(?P<value>\d[\d,]*)\*{0,2}"
    r"(?P<gap>(?:\s+[A-Za-z][\w./-]*){0,4}\s*)"
    r"\(\s*`(?P<cmd>[^`\n]{4,200})`\s*\)",
    re.I)

# SHAPE 4 — the value, then an ATTRIBUTION CLAUSE, then the command:
#   "(60 files — re-derived by `ls docs/tracking/*-design.md \| wc -l` …)"
#   "**148 total** (re-derived August 18, 2026 by `python3 tools/…`)"
# Round 16 (H7). Both are live, and the first is the flagship miss: root
# CLAUDE.md's design-supplement count, whose own sentence records that it read
# 42 while the truth was 60 and that nobody noticed. Neither binds shape 3,
# because the parenthesis opens before the VALUE, not before the command.
#
# The gap here is much looser than shape 3's, so it is anchored twice instead:
# it must contain a MEASUREMENT VERB and end on a connective ("by", "via",
# "per", "using", "from"). Prose that merely mentions a number near a command
# has neither. The character budgets below and the one-newline rule in
# rejects() keep it from reaching across a paragraph — this repo hard-wraps
# mid-sentence, so refusing every newline would have missed the second example
# above, and allowing any number of them would bind across bullets.
_ATTRIBUTION_VERB = (
    r"(?:re-)?(?:derived|measured|counted|verified|confirmed|computed|"
    r"checked|produced|reproduced|obtained|generated|re-run|rerun)")
# The two halves of the gap, and their budgets. Both were tightened after the
# first draft FAILED a correct document, which is round 8's H4 recurring in a
# new shape: with 80 characters before the verb, #20 §7.2's version-history row
#
#     …§7.2's "Stage 0 status" paragraph rewritten …, every figure re-derived
#     August 18, 2026 (35 assemblies via `ls -d src/*/ \| wc -l`, …)
#
# bound the 0 of "Stage 0 status" — 71 characters away, across a quotation and
# two clauses — and reported "document says 0; command returns 35" against a
# row whose own stated value, 35, is correct. 24 characters is what the live
# instances actually need (" files — ", " total (") and is short enough that
# the value and the verb must belong to the same clause. Sentence and quote
# marks are excluded outright for the same reason. The POST half may cross the
# one hard wrap rejects() allows; the PRE half may not.
# Both halves are single CHARACTER CLASSES, never an alternation of "ordinary
# char" and "newline plus indent": the alternation form `(?:[^`\n]|\n[ \t>]*)`
# is ambiguous — "\n> " can be consumed by either branch — and under two
# bounded repetitions that is catastrophic backtracking. Measured on the first
# draft: the whole scan went from 1.5 s to no result in 120 s. The newline
# BUDGET is enforced in rejects() instead, where it is a two-line rule rather
# than a regex nobody can reason about.
_ATTR_PRE = r"[^`;\"'.\n]{0,24}?"
_ATTR_POST = r"[^`;\"'.]{0,40}?"
# The second route to the same shape, and the reason the connectives are split
# in two: `via` and `per` introduce a METHOD and essentially nothing else,
# while `by`, `from` and `using` are ordinary English. So `via`/`per` may stand
# alone after a short plain noun phrase — "35 assemblies via `ls -d src/*/ \|
# wc -l`", #20 §7.2's own version-history row — whereas `by`/`from`/`using`
# must be earned by a measurement verb. The bare-connective gap is restricted
# to letters, spaces and hyphens, so it cannot cross a clause the way the
# 80-character draft did.
_ATTR_BARE = r"[A-Za-z \t-]{0,24}?"
CLAIM_ATTRIBUTED = re.compile(
    r"\*{0,2}(?P<value>\d[\d,]*)\*{0,2}"
    r"(?P<gap>(?:" + _ATTR_PRE + r"\b" + _ATTRIBUTION_VERB + r"\b"
    + _ATTR_POST + r"\b(?:by|via|per|using|from)"
    r"|" + _ATTR_BARE + r"\b(?:via|per)"
    r")\s+)"
    r"`(?P<cmd>[^`\n]{4,200})`",
    re.I)


class AttributedShape(LeadingValueShape):
    """Shape 4's gap may cross ONE hard-wrap and no more."""

    def rejects(self, text, m):
        why = LeadingValueShape.rejects(self, text, m)
        if why is not None:
            return why
        if m.group("gap").count("\n") > 1:
            return "the attribution clause spans more than one wrapped line"
        return None


CLAIM_SHAPES = (
    ClaimShape("command-then-value", CLAIM),
    ClaimShape("command-colon-value", CLAIM_COLON),
    LeadingValueShape("value-then-parenthesised-command", CLAIM_VALUE_FIRST),
    AttributedShape("value-then-attributed-command", CLAIM_ATTRIBUTED),
)

# Every backticked span, for the unrecognised-shape census below.
COMMAND_SPAN = re.compile(r"`(?P<cmd>[^`\n]{4,200})`")
# How near an integer must be, on the SAME line, for a command-shaped span that
# binds to no shape to be reported as a possible claim this tool cannot read.
# Bounding to the line is what stops a census entry being manufactured out of
# the next bullet's numbers, and 200 characters is where the census effectively
# SATURATES: measured on this tree at 86 / 117 / 138 / 143 / 145 / 145 for
# radii 40 / 60 / 120 / 200 / 400 / unbounded-within-the-line. Past 200 a wider
# window buys two entries; below it the census starts missing spans on this
# repo's very long bullet lines. Re-measure this curve if the corpus changes
# shape — it is the only thing that justifies the number.
UNRECOGNISED_RADIUS = 200


class Claim:
    """One recognised claim: the quoted command, the stated value, and where
    both came from. The value type H11 asked for — before it, scan() carried
    the same seven facts as loose locals threaded through 155 lines."""

    __slots__ = ("rel", "text", "shape", "cmd", "cmd_start", "start", "end",
                 "value_start", "value_end", "negation_window", "line")

    def __init__(self, rel, text, shape, m):
        self.rel = rel
        self.text = text
        self.shape = shape
        self.cmd = m.group("cmd").strip()
        self.cmd_start = m.start("cmd")
        self.start, self.end = m.start(), m.end()
        self.value_start, self.value_end = m.start("value"), m.end("value")
        self.negation_window = shape.negation_window(text, m)
        self.line = text.count("\n", 0, m.start()) + 1

    @property
    def head(self):
        parts = self.cmd.split()
        return parts[0] if parts else ""

    @property
    def answer(self):
        return self.shape.answer


def collect_claims(rel, text):
    """Every claim any shape recognises in `text`, in document order.

    Deduplicated by the position of the COMMAND a claim quotes: a span two
    shapes both match is one claim, not two. A match a shape REJECTS (a date,
    a gap spanning two wraps) is not a claim and does not reserve its command
    span — another shape may still bind it, and if none does, the census below
    reports the span rather than letting it disappear."""
    claims, bound = [], set()
    for shape in CLAIM_SHAPES:
        for m in shape.find(text):
            if m.start("cmd") in bound:
                continue
            if shape.rejects(text, m) is not None:
                continue
            bound.add(m.start("cmd"))
            claims.append(Claim(rel, text, shape, m))
    claims.sort(key=lambda c: c.start)
    return claims


def unrecognised_spans(text, bound):
    """Command-shaped backticked spans with an integer nearby that NO shape
    bound — yields (line, cmd).

    Round 16 (H7). This is the half of that finding that matters more than the
    widened matchers. Before it, a claim written in a shape none of the regexes
    knew was invisible AND uncounted: it appeared in no bucket, so the printed
    coverage statement was false by omission, and the tool's own header could
    describe a blind spot only by naming instances someone had happened to
    notice. Three wrong claims in unrecognised shapes produced all six decline
    buckets at 0 and exit 0.

    A shape gap is now SELF-REPORTING. The census is deliberately cruder than
    the shapes — same line, an integer within UNRECOGNISED_RADIUS characters —
    because its job is to over-report: everything it names is either a claim a
    shape should learn, or prose that happens to sit near a number, and both
    are better in the printed list than in nobody's."""
    for s in COMMAND_SPAN.finditer(text):
        if s.start("cmd") in bound:
            continue
        cmd = s.group("cmd").strip()
        parts = cmd.split()
        head = parts[0] if parts else ""
        # command_shaped() alone is NOT the right test here, and the complement
        # case caught it: that predicate exists to tell an UNLISTED binary from
        # a backticked identifier, so its head test is "a path, a script, or a
        # known-but-unrunnable binary" — which excludes every binary this tool
        # can actually RUN. `35 assemblies. The gate runs `ls -d src/*/ \| wc
        # -l` nightly.` was therefore missing from the census: a runnable
        # command in a shape no matcher knows, the single most valuable thing
        # this bucket can report, and the one class it was blind to.
        if FORBIDDEN.search(cmd):
            continue
        if not (command_shaped(cmd, head)
                or (head in ALLOWED_CMDS and " " in cmd)):
            continue
        ls = text.rfind("\n", 0, s.start()) + 1
        le = text.find("\n", s.end())
        le = len(text) if le == -1 else le
        near = (text[max(ls, s.start() - UNRECOGNISED_RADIUS):s.start()]
                + text[s.end():min(le, s.end() + UNRECOGNISED_RADIUS)])
        if not re.search(r"\d", near):
            continue
        yield text.count("\n", 0, s.start()) + 1, cmd


def tokenize(cmd):
    """Split `cmd` into segments of (text, has_unquoted_glob) tokens.

    Round 9 (H2): `shlex.split` DISCARDS the quoting, and expand_globs then
    treated a QUOTED regex pattern as a shell glob — `find . -name '*.md' |
    wc -l` became `find . -name CLAUDE.md doc.md`, which errors, prints
    nothing, and (with H3's unchecked exit status) was reported as "document
    says 12; command returns 0" against a perfectly correct document. A
    checker that fabricates a failing finding out of a correct claim is the
    exact defect this tool exists to catch, so the quoting is now carried
    through to the expansion decision.

    Splitting on `|` also happens HERE rather than by `cmd.split("|")`, so a
    pipe inside quotes (`grep -c 'a|b' file`) is a literal character instead
    of an unbalanced-quote parse failure.

    Returns None when the string does not tokenize (unterminated quote,
    trailing backslash) — the caller declines it.
    """
    segments, cur_seg, cur = [], [], []
    started = False
    quote = None
    i, n = 0, len(cmd)
    while i < n:
        c = cmd[i]
        if quote is None:
            if c.isspace():
                if started:
                    cur_seg.append(cur); cur = []; started = False
                i += 1; continue
            if c == "|":
                if started:
                    cur_seg.append(cur); cur = []; started = False
                segments.append(cur_seg); cur_seg = []
                i += 1; continue
            # Markdown escapes a literal pipe as `\|` inside a table cell, and a
            # large share of this repo's quoted commands live in tables — both
            # of its executable claims among them. OUTSIDE quotes that is a
            # pipeline separator. INSIDE quotes it is left alone, because there
            # `\|` is far more likely to be BRE alternation
            # (`grep -n "typeof\|GetFields\|Reflection" …` is live in
            # spec-error-log.md) and rewriting it to a literal `|` would run a
            # DIFFERENT regex and report its count as the document's.
            if c == "\\" and i + 1 < n and cmd[i + 1] == "|":
                if started:
                    cur_seg.append(cur); cur = []; started = False
                segments.append(cur_seg); cur_seg = []
                i += 2; continue
            if c in "'\"":
                quote = c; started = True; i += 1; continue
            if c == "\\":
                if i + 1 >= n:
                    return None
                cur.append((cmd[i + 1], True)); started = True; i += 2; continue
            cur.append((c, False)); started = True; i += 1; continue
        if c == quote:
            quote = None; i += 1; continue
        if quote == '"' and c == "\\" and i + 1 < n and cmd[i + 1] in '"\\$`':
            cur.append((cmd[i + 1], True)); i += 2; continue
        cur.append((c, True)); i += 1
    if quote is not None:
        return None
    if started:
        cur_seg.append(cur)
    segments.append(cur_seg)
    out = []
    for seg in segments:
        toks = []
        for t in seg:
            text = "".join(ch for ch, _q in t)
            # Expandable only when EVERY glob character in the token is
            # unquoted — bash expands a mixed token too, but declining to is
            # the conservative direction and it never invents a command.
            globs = [q for ch, q in t if GLOB_CH.match(ch)]
            toks.append((text, bool(globs) and not any(globs)))
        out.append(toks)
    return out


def _option_cores(tok):
    """Every option spelling `tok` could carry, for deny-list comparison.

    Round 15 (H3). The deny-list tested `a in exact`, which sees `-o` and (via
    the prefix set) the `=` forms someone enumerated by hand — but NOT the same
    option with its value attached. Proven: `sort -oSORT_CANARY data.txt` WROTE
    the canary and `git grep -O./p.sh MATCHME` EXECUTED the script, both under
    a printed PASS, while `-o` and `-O` sat in the deny-list the whole time.
    That is not a new hatch; it is a denied hatch respelled, and the same
    respelling exists for every denied short option that takes a value and
    every long option whose `=` form nobody thought to list.

    So the option CORE is compared instead of the spelling: a single-dash token
    is decomposed into its whole cluster (`-no FILE` is `-n` then `-o`, and an
    attached value cannot hide the letter that precedes it), and a long token
    is split on `=`. Over-refusal is the safe direction — a cluster letter that
    coincides with a denied option is DECLINED AND NAMED, never run."""
    if not tok.startswith("-") or tok in ("-", "--"):
        return ()
    if tok.startswith("--"):
        core = tok.split("=", 1)[0]
        return (tok,) if core == tok else (tok, core)
    return (tok,) + tuple("-" + ch for ch in tok[1:])


def denied_flag(argv):
    """The write/execute escape hatch this argv reaches for, or None.

    Round 9 (H1). Read-only-by-allow-list was false: `sed -i`, `find -delete`,
    `python3 -c`, `sort -o`, `rg --pre` and `git -c` all execute or write from
    a binary the list called read-only."""
    name = argv[0]
    exact = DENIED_FLAGS.get(name, ())
    prefixes = DENIED_FLAG_PREFIXES.get(name, ())
    for a in argv[1:]:
        for core in _option_cores(a):
            if core in exact:
                return a if core == a else "%s, attached as `%s`" % (core, a)
            if any(pfx == core + "=" for pfx in prefixes):
                return a
        if any(a.startswith(pfx) for pfx in prefixes):
            return a
    if name == "git":
        for a in argv[1:]:
            if not a.startswith("-"):
                break            # the subcommand: globals end here
            for core in _option_cores(a):
                if core in GIT_GLOBAL_DENIED or core in ("--exec-path",
                                                         "--upload-pack"):
                    return a if core == a else ("%s, attached as `%s`"
                                                % (core, a))
    if name == "awk":
        # Round 14 (external review, P1): scan EVERY token, never the token
        # GUESSED to be the program. `-v` and `-F` take a SEPARATE argument, so
        # "first operand not starting with -" picked `x=1` out of
        # `awk -v x=1 'BEGIN{system("touch /tmp/pwn")}END{print 1}' f` and never
        # looked at the program at all — the command then ran system() and
        # returned the claimed integer under a printed PASS. Verified
        # exploitable before this fix. Scanning all tokens needs no awk option
        # grammar and cannot be outflanked by adding one: `system`/`getline`
        # must appear literally to be called, and a FILENAME containing either
        # word is merely declined-and-named, which is the safe direction.
        #
        # Round 15 (H1): scanning every token for two BLACKLISTED words was
        # still the wrong shape of rule, because awk's escape lives in its
        # SCRIPT and a script is a language, not a vocabulary.
        # `awk 'BEGIN{print "touch X" | "sh"} END{print 1}' CLAUDE.md` created
        # the file and returned the claimed 1 under a printed PASS, using
        # neither `system` nor `getline`. The rule is inverted: what the
        # program may CALL is allow-listed, and the two pipe characters that no
        # call-name list can describe are refused outright.
        for a in argv[1:]:
            if "|" in a:
                return ("awk token containing `|` — `print x | \"cmd\"` and "
                        "`\"cmd\" | getline` both run a shell command")
            if "@" in a:
                return "awk token containing `@` — gawk @load/@include"
            for call in AWK_CALL.findall(a):
                if call not in AWK_ALLOWED_CALLS:
                    return ("awk program calling `%s(`, which is not on the "
                            "allow-list of awk functions" % call)
            if AWK_ESCAPES.search(a):
                return "awk program calling system()/getline"
    if name == "uniq" and len([a for a in argv[1:] if not a.startswith("-")]) >= 2:
        return "uniq with an OUTPUT operand"
    return None


def parse_pipeline(cmd):
    """Return (segments, None), or (None, reason) when the command is not a
    safe pipeline.

    The reason is NAMED rather than generic: this tool's contract is that a
    declined claim is counted AND named, and "not an allow-listed read-only
    pipeline" does not tell a reader whether their command was refused for a
    stray backtick or for `sort -o`."""
    # `\|` is handled inside tokenize(), quote-aware — see the note there. It
    # used to be unescaped by a blind `cmd.replace("\\|", "|")` before parsing,
    # which was harmless only because a quoted `\|` then broke the parse; with
    # the tokenizer keeping such a command runnable, blind unescaping would have
    # started SILENTLY REWRITING regexes instead (round 9, found reviewing this
    # round's own fix).
    bad = FORBIDDEN.search(cmd)
    if bad:
        return None, ("contains the shell metacharacter %r — redirection, "
                      "substitution and chaining are refused"
                      % bad.group(0))
    parsed = tokenize(cmd)
    if parsed is None:
        return None, "does not tokenize (unterminated quote or trailing \\)"
    segments = []
    for toks in parsed:
        if not toks:
            return None, "empty pipeline segment"
        argv = [text for text, _g in toks]
        if argv[0] not in ALLOWED_CMDS:
            return None, ("`%s` is not an allow-listed read-only binary"
                          % argv[0])
        if argv[0] == "git":
            sub = next((a for a in argv[1:] if not a.startswith("-")), None)
            if sub not in GIT_READONLY:
                return None, ("`git %s` is not a read-only subcommand"
                              % (sub if sub else "<none>"))
        hatch = denied_flag(argv)
        if hatch is not None:
            return None, ("`%s` reaches a write/execute escape hatch (%s)"
                          % (argv[0], hatch))
        segments.append(toks)
    if not segments:
        return None, "empty pipeline"
    return segments, None


# Exit codes that are a RESULT, not a failure: grep-family 1 = "no match",
# diff 1 = "files differ". Everything else non-zero means the command did not
# run as written (bad path, bad flag, missing file), and its output is not an
# answer to anything.
BENIGN_NONZERO = {"grep": {1}, "egrep": {1}, "fgrep": {1}, "rg": {1},
                  "diff": {1}, "git": {1}}


# A hard ceiling on how much a single pipeline segment may print. The answers
# this tool compares are single integers, so any legitimate segment is orders
# of magnitude below this; the cap exists because TIMEOUT_S bounds wall time
# and nothing bounded MEMORY (round 15, M1).
OUTPUT_CAP_BYTES = 8 * 1024 * 1024


def _read_capped(stream, box):
    """Drain `stream` into `box["data"]`, giving up at OUTPUT_CAP_BYTES.

    Runs on its own thread so the caller can enforce TIMEOUT_S and kill a child
    that is still writing. `read1` is deliberate: it is ONE read syscall, so
    the cap is enforced per chunk rather than after a buffered reader has
    already accumulated the whole stream."""
    chunks, total = [], 0
    try:
        while True:
            chunk = stream.read1(65536)
            if not chunk:
                break
            total += len(chunk)
            if total > OUTPUT_CAP_BYTES:
                box["overflow"] = True
                break
            chunks.append(chunk)
    except (OSError, ValueError):                        # killed mid-read
        pass
    box["data"] = b"".join(chunks)


def run_pipeline(segments, cwd):
    """Run a validated pipeline.

    Returns (stdout_text, None) on success, or (None, reason) when the
    pipeline did not run as written.

    Round 9 (H3): the previous form ignored every segment's exit status, so a
    FAILING segment's empty output flowed into the next one and the pipeline
    still produced a confident number — `grep -rn 'X' nosuchdir/ | wc -l`
    printed `0`, which was then reported as "document says 218; command
    returns 0" against a document that was not wrong about anything. The tool
    exists to stop fabricated verification; manufacturing a mismatch out of a
    broken command is the same defect wearing the other sign. A failed
    pipeline is now DECLINED and named, never compared.

    Round 15 (M1): each segment's output used to be buffered whole, with no
    limit — TIMEOUT_S bounds WALL TIME, which is not the resource a document
    line can exhaust. `printf %300000000d 1 \\| wc -c` drove the checker to
    587 MB RSS here and still printed PASS; `cat /dev/zero` OOM-kills the
    runner long before 60 s elapse. Output is now read INCREMENTALLY against a
    hard byte cap set far above any legitimate single-integer answer, the child
    is killed on overflow, and the claim is declined and NAMED like every other
    refusal. Input is staged through a temporary file rather than a pipe so a
    capped reader can never deadlock against an unread stdin.

    Round 15 (M2): ValueError was not caught, and subprocess raises it — not a
    SubprocessError — on a NUL in argv, so one backticked span with an embedded
    NUL ended the whole scan in a traceback: every later claim AND the entire
    dangling-identifier check never ran, with a real defect elsewhere masked
    behind a crash. FORBIDDEN now refuses NUL up front; this catch is the
    backstop, because "the checker died" is the one result that hides
    everything."""
    data = b""
    for argv in segments:
        with tempfile.TemporaryFile() as sin:
            sin.write(data)
            sin.seek(0)
            try:
                proc = subprocess.Popen(
                    argv, cwd=cwd, stdin=sin, stdout=subprocess.PIPE,
                    stderr=subprocess.DEVNULL)
            except ValueError as exc:
                return None, ("`%s` cannot be executed as written (%s)"
                              % (argv[0], exc))
            except (OSError, subprocess.SubprocessError):
                return None, "command could not be executed (%s)" % argv[0]
            box = {"overflow": False, "data": b""}
            reader = threading.Thread(target=_read_capped,
                                      args=(proc.stdout, box), daemon=True)
            reader.start()
            reader.join(TIMEOUT_S)
            timed_out = reader.is_alive()
            if timed_out or box["overflow"]:
                proc.kill()
                proc.wait()
                reader.join(5)
                proc.stdout.close()
                if timed_out:
                    return None, "command timed out after %ds" % TIMEOUT_S
                return None, ("`%s` output exceeded %d bytes — the child was "
                              "killed and its output is not treated as an "
                              "answer" % (argv[0], OUTPUT_CAP_BYTES))
            try:
                rc = proc.wait(timeout=TIMEOUT_S)
            except subprocess.TimeoutExpired:
                proc.kill()
                proc.wait()
                return None, "command timed out after %ds" % TIMEOUT_S
            finally:
                proc.stdout.close()
        if rc != 0 and rc not in BENIGN_NONZERO.get(argv[0], ()):
            return None, ("`%s` exited %d — its output is not treated as an "
                          "answer" % (argv[0], rc))
        data = box["data"]
    try:
        return data.decode("utf-8", "replace"), None
    except Exception:                                    # pragma: no cover
        return None, "output is not decodable text"


def single_integer(out):
    """The command's answer as an int, or None if it did not print exactly one."""
    if out is None:
        return None
    lines = [l.strip() for l in out.splitlines() if l.strip()]
    if len(lines) != 1:
        return None
    if not re.fullmatch(r"\d[\d,]*", lines[0]):
        return None
    return int(lines[0].replace(",", ""))


GLOB_CH = re.compile(r"[*?\[]")

# Minimum POSITIONAL operands the FIRST pipeline segment needs to be
# self-contained. A quoted command missing its file operand reads stdin, which is
# empty here, and returns 0 or nothing — reporting that as "document says 18,
# command returns 0" would be a fabricated finding, the very thing this tool
# exists to prevent. This repo genuinely writes such prose ("`grep -c '^- \*\*'`
# over each file returns 18"), where the filename lives outside the backticks, so
# the command as quoted is not runnable and the claim is UNVERIFIABLE, not false.
FIRST_SEGMENT_MIN_OPERANDS = {
    "grep": 2, "egrep": 2, "fgrep": 2, "rg": 2,   # pattern + at least one file
    "sed": 2, "awk": 2,                           # script + at least one file
    "cat": 1, "wc": 1, "sort": 1, "uniq": 1,
    "head": 1, "tail": 1, "cut": 1, "tr": 1,
}


def self_contained(segments):
    """False when the first segment would read stdin because its file operand is
    missing from the quoted text."""
    argv = segments[0]
    need = FIRST_SEGMENT_MIN_OPERANDS.get(argv[0])
    if need is None:
        return True
    operands = [a for a in argv[1:] if not a.startswith("-")]
    return len(operands) >= need


def escaping_operand(argv, repo):
    """The first non-option operand whose realpath leaves the repo, or None.

    Round 15 (M3). cwd was the repo and nothing else confined anything: a
    document line could read any file on the host and have the number COMPARED.
    Proven — `grep -c . /etc/passwd` was executed and compared, a one-integer
    read oracle over host files, and `cat ../OUTSIDE.txt \\| wc -l` reproduced
    its stated value under a printed PASS. It is also the half of the
    output-size problem that admits `/dev/zero` and `find /`.

    The rule generalises the one python3's script operand used to carry alone,
    and is applied AFTER glob expansion for the round-14 reason: validating a
    shape rather than the argv that executes is this file's recurring root
    error. A pattern operand that happens to read as an escaping path
    (`grep -c '..' f`) is declined and NAMED rather than run — over-refusal is
    the safe direction, and containment is the property that becomes
    load-bearing the moment the execution hatches are closed."""
    root = os.path.realpath(str(repo))
    for a in argv[1:]:
        if a.startswith("-"):
            continue
        real = os.path.realpath(os.path.join(root, a))
        if real != root and not real.startswith(root + os.sep):
            return a
    return None


def expand_globs(segments, repo):
    """Expand UNQUOTED glob tokens as a shell would: sorted matches relative to
    the repo root, or the literal token when nothing matches (bash default).

    Returns (segments, None) or (None, reason).

    Round 9 (H2): a QUOTED token is never expanded, because it is not a glob —
    it is a regex the document wrote inside quotes. Every glob character in
    this corpus is of that kind (7 of 7: `'^- \*\*'`, `'public readonly
    byte\[\]'`), and expanding them rewrote the command into a different one.
    `tokenize` carries the quoting through so the decision can be made here.

    Round 9 (M2): an unquoted ABSOLUTE pattern raises NotImplementedError out
    of pathlib rather than expanding; document text must not be able to crash
    the checker, so it is declined by name instead."""
    out = []
    for toks in segments:
        new = [toks[0][0]]
        for text, expandable in toks[1:]:
            if not expandable:
                new.append(text)
                continue
            try:
                hits = sorted(str(q.relative_to(repo)) for q in repo.glob(text))
            except (NotImplementedError, ValueError, OSError) as exc:
                return None, ("glob `%s` is not expandable from the repo root "
                              "(%s)" % (text, type(exc).__name__))
            # Round 14 (external review, P1): a FILENAME may look like an
            # option. With a repo file named `--output=canary`, the validated
            # command `sort * \| wc -l` expanded to
            # `sort --output=canary …` and WROTE that file, because
            # denied_flag() had run on the pre-expansion argv. Verified
            # exploitable before this fix. Both halves of the gap are closed:
            # an expanded name that would be read as an option is refused
            # here...
            for hit in hits:
                if hit.startswith("-"):
                    return None, ("glob `%s` expands to `%s`, which the "
                                  "command would read as an option, not a "
                                  "file" % (text, hit))
            new.extend(hits if hits else [text])
        out.append(new)
    # ...and the escape-hatch validation is re-run on the argv that will
    # ACTUALLY execute, so no expansion can introduce a hatch that the
    # pre-expansion check certified absent. Validating a shape instead of the
    # real argv is the single root error behind both of this round's findings.
    for argv in out:
        hatch = denied_flag(argv)
        if hatch is not None:
            return None, ("after glob expansion, `%s` reaches a write/execute "
                          "escape hatch (%s)" % (argv[0], hatch))
        outside = escaping_operand(argv, repo)
        if outside is not None:
            return None, ("operand `%s` resolves outside the repository root — "
                          "this tool reads the checkout, not the host"
                          % outside)
    return out, None



# ---------------------------------------------------------------------------
# CHECK 2 — dangling identifier references inside spec code fences.
#
# In src/ a rename that misses a site is a build error. In a spec's worked
# example nothing binds, so it dangles silently — which is how ERR-020-001
# behaved in May 2026 (renamed the constant in §4.2, not in the appendix) and
# how round 7's own fix behaved in August (renamed §C.1's declaration, left
# §C.2's reference on the old name, annotated "§3.2.3 — named constant", i.e.
# claiming conformance to the rule that forced the rename).
#
# Deliberately narrow, for precision over recall: a reference `Type.MEMBER` is
# reported ONLY when `Type` is a class the same file declares, and `MEMBER` is
# not declared anywhere in that file. Cross-file resolution against src/ is NOT
# attempted — a spec exemplar legitimately names symbols that do not exist yet,
# and flagging those would train readers to ignore the tool.
# ---------------------------------------------------------------------------

# ONLY `csharp`/`cs` fences. Spec files are full of ASCII file-tree diagrams and
# untagged prose blocks in which `ShotExecutor.Execute()` is a sentence, not a
# member access; parsing those produced three false positives for every true one
# on first run, and a gate at that ratio gets switched off.
FENCE = re.compile(r"```(csharp|cs)\n(.*?)```", re.S | re.I)
DECL_CLASS = re.compile(r"\b(?:static\s+)?class\s+([A-Z][A-Za-z0-9_]*)")
DECL_MEMBER = re.compile(
    r"\b(?:public|internal|private|protected)\s+"
    r"(?:static\s+|readonly\s+|const\s+)*"
    r"[A-Za-z_][A-Za-z0-9_<>,\[\]\.]*\s+"
    r"([A-Za-z_][A-Za-z0-9_]*)\s*(?:=|;|\()")
# Any `<type-ish> Name` followed by ; = ( or { — catches modifier-less fields,
# method signatures, and members sketched inside comments.
DECL_LOOSE = re.compile(
    r"\b(?:int|uint|float|double|bool|byte|sbyte|string|long|ulong|short|ushort|"
    r"decimal|char|var|void|[A-Z][A-Za-z0-9_<>,\[\]]*)\s+"
    r"([A-Za-z_][A-Za-z0-9_]*)\s*(?:<[^>\n]*>)?\s*[;=({]")
# A member access, NOT a namespace segment. `TacticalDirector.Physics.Collision`
# is a namespace path: `Physics` is preceded by a dot and `Collision` followed by
# one. Requiring neither side to continue the chain removes that whole class,
# which was the second noise source found on the live tree.
REFERENCE = re.compile(
    r"(?<![.\w])([A-Z][A-Za-z0-9_]*)\.([A-Za-z_][A-Za-z0-9_]*)(?![\w.])")
# `BallPhysicsConstants.cs` is a FILENAME, not a member access. Without this the
# tool reports every filename in every fence — noise that would get it ignored,
# which is how a checker stops working long before it stops running.
FILE_EXTENSIONS = {
    "cs", "md", "py", "json", "asmdef", "meta", "txt", "yml", "yaml", "sh",
    "csv", "csproj", "sln", "xml", "html", "js", "ruleset", "editorconfig",
}


def scan_fence_identifiers(repo, quiet=False):
    """Report Type.MEMBER references whose Type the file declares and whose
    MEMBER it does not. Returns (findings, files_with_fences)."""
    findings = []
    files = []
    for pat in SURFACE_GLOBS:
        files.extend(sorted(repo.glob(pat)))
    scanned = 0
    for path in files:
        text = path.read_text(encoding="utf-8", errors="replace")
        blocks = [b for _tag, b in FENCE.findall(text)]
        if not blocks:
            continue
        scanned += 1
        code = "\n".join(blocks)
        # `using` / `namespace` lines are declarations of paths, never member
        # accesses; drop them before looking for references.
        code = "\n".join(l for l in code.splitlines()
                          if not re.match(r"\s*(?:using|namespace)\b", l))
        classes = set(DECL_CLASS.findall(code))
        # Harvest declarations LOOSELY as well as strictly. Spec fences elide:
        # members appear without access modifiers (`int NextStaffId;`), inside
        # /* ... */ sketches of a class body, and as method signatures. A missed
        # declaration is a FALSE POSITIVE, and a checker that cries wolf is
        # ignored long before it is fixed — so over-harvest deliberately and
        # accept lower recall. Precision is the property that keeps it trusted.
        members = set(DECL_MEMBER.findall(code)) | set(DECL_LOOSE.findall(code)) | classes
        if not classes:
            continue
        # Asymmetry, deliberate: comments COUNT as declarations (spec fences
        # sketch class bodies inside /* ... */) but NEVER as references — a
        # `/// Called by MatchSimulator.Update() at 60Hz` line is prose about
        # another spec's type, not a member access this file must satisfy.
        # Harvest from the full text above; scan for references here only.
        code_nc = re.sub(r"/\*.*?\*/", " ", code, flags=re.S)
        code_nc = re.sub(r"//[^\n]*", " ", code_nc)
        for m in REFERENCE.finditer(code_nc):
            typ, mem = m.group(1), m.group(2)
            if mem in FILE_EXTENSIONS:
                continue
            if typ in classes and mem not in members:
                # Locate the reference in the FILE for an actionable line number.
                line = None
                for lm in re.finditer(re.escape(typ + "." + mem), text):
                    line = text.count("\n", 0, lm.start()) + 1
                    break
                findings.append((str(path.relative_to(repo)), line, typ, mem,
                                 sorted(x for x in members
                                        if x.lower() == mem.replace("_", "").lower()
                                        or x.replace("_", "").lower() == mem.replace("_", "").lower())))
    if not quiet:
        print("\ndangling-identifier check — references inside spec code fences")
        print("  spec files with code fences   : %d" % scanned)
        print("  (a reference is reported only when its TYPE is declared in the same"
              " file and its MEMBER is not — cross-file resolution is deliberately"
              " not attempted)")
    return findings


# ---------------------------------------------------------------------------
# Round 16 (H11): scan() used to be a 155-line monolith. One function did
# surface collection, missing-surface reporting, region computation, matching
# for two shapes with two bespoke and mutually inconsistent negation windows,
# per-claim validate/expand/run/compare/excuse, six-bucket counting, four print
# blocks, an unrelated check and exit-code composition. "Single integer" was
# hard-wired at six sites inside it. It is now three functions: collect_claims()
# (seam 2), check_claim() below (which knows nothing about printing), and a
# scan() that iterates and reports.
# ---------------------------------------------------------------------------

# Every decline bucket, in print order. A bucket is a promise: a claim landing
# in one is UNVERIFIED, counted and named — never silently passed. Adding a
# decline path means adding it here, which is the point of the list existing.
DECLINE_BUCKETS = (
    ("unsafe", "unsafe"),
    ("unlisted-binary", "unlisted-binary"),
    ("not-self-contained", "not-self-contained"),
    ("did-not-run", "did-not-run"),
    ("not-single-int", "not-a-single-integer"),
    ("approximate-or-range", "approximate-or-range"),
    ("negated", "negated-or-historical"),
    ("unrecognised-shape", "unrecognised-shape"),
)


def check_claim(claim, repo, regions):
    """Validate, run and compare ONE claim. Returns (outcome, payload):

        ("ignored",    None)            not a command at all — no claim here
        ("declined",   (bucket, why))   counted and named; UNVERIFIED
        ("reproduced", (stated, got))   the document is right
        ("excused",    (stated, got))   wrong, but inside a dated record
        ("mismatch",   (stated, got))   wrong, and gating

    Knows nothing about printing, counters or exit codes; scan() owns those."""
    answer = claim.answer
    if claim.head not in ALLOWED_CMDS:
        # Round 9 (M1): this path was the tool's THIRD decline route and the
        # only silent one, while its header and its file-manifest row both
        # published "every declined claim is counted AND named". Most of what
        # lands here is not a command at all — the shapes also match a
        # backticked IDENTIFIER before an arrow (`SEASON_SAVE_FORMAT_VERSION`
        # **5 → 6**), ~1,100 of them — so counting all of it would drown the
        # real signal. Only genuinely command-SHAPED text is counted and named.
        # FORBIDDEN gates it too, so a backticked EXPRESSION that merely looks
        # path-shaped (`permille/1000f > 0.6f`, live in #33's appendices) is
        # not announced as a rejected binary. It was never a command; naming it
        # would be the mirror of the noise command_shaped exists to suppress.
        if command_shaped(claim.cmd, claim.head) and not FORBIDDEN.search(claim.cmd):
            return "declined", ("unlisted-binary",
                                "`%s` is not an allow-listed read-only binary"
                                % claim.head)
        return "ignored", None
    # Round 10: the negation test used to run BEFORE the command test, so a
    # backticked IDENTIFIER near a negation ("`SeasonSaveContents` … was not
    # 4") was counted and printed as a declined CLAIM. Five of the eight
    # "negated-or-historical" declines were of that kind — never claims, so
    # counting them overstated how much real coverage the tool was giving up,
    # in the very figure that exists to state that honestly.
    if NEGATOR.search(claim.negation_window):
        return "declined", ("negated",
                            "claim is NEGATED or historical — states what the "
                            "command does not / no longer return")
    stated, why = answer.parse(claim.text, claim.value_start, claim.value_end)
    if stated is None:
        return "declined", why
    segments, why = parse_pipeline(claim.cmd)
    if segments is None:
        return "declined", ("unsafe", why)
    segments, why = expand_globs(segments, repo)
    if segments is None:
        return "declined", ("unsafe", why)
    if not self_contained(segments):
        return "declined", ("not-self-contained",
                            "operand missing from the quoted text — not "
                            "runnable as written")
    out, why = run_pipeline(segments, str(repo))
    if out is None:
        return "declined", ("did-not-run", why)
    got = answer.read(out)
    if got is None:
        return "declined", (answer.bucket, answer.unreadable)
    if answer.matches(stated, got):
        return "reproduced", (stated, got)
    if (any(a <= claim.start < b for a, b in regions)
            and not currency_asserted(claim.text, claim.start, claim.end)):
        return "excused", (stated, got)
    return "mismatch", (stated, got)


def collect_surfaces(repo):
    """(files, missing). A named surface that is not in the tree, and a glob
    that matches nothing, are both MISSING — see the COVERAGE FLOOR note."""
    files, missing = [], []
    for rel in SURFACES:
        p = repo / rel
        if p.exists():
            files.append((rel, p))
        else:
            missing.append("named surface `%s` is not in the tree" % rel)
    for pat in SURFACE_GLOBS:
        hits = sorted(repo.glob(pat))
        if not hits:
            missing.append("surface glob `%s` matches no file" % pat)
        for p in hits:
            files.append((str(p.relative_to(repo)), p))
    return files, missing


def scan(repo, quiet=False):
    files, missing = collect_surfaces(repo)

    checked = 0
    declined = {bucket: 0 for bucket, _label in DECLINE_BUCKETS}
    declined_list = []
    findings = []
    excused = []

    for rel, path in files:
        text = path.read_text(encoding="utf-8", errors="replace")
        regions = dated_record_regions(rel, text)
        claims = collect_claims(rel, text)
        for claim in claims:
            outcome, payload = check_claim(claim, repo, regions)
            if outcome == "ignored":
                continue
            if outcome == "declined":
                bucket, why = payload
                declined[bucket] += 1
                declined_list.append((rel, claim.line, claim.cmd, why))
                continue
            checked += 1
            stated, got = payload
            if outcome == "excused":
                excused.append((rel, claim.line, claim.cmd, stated, got))
            elif outcome == "mismatch":
                findings.append((rel, claim.line, claim.cmd,
                                 claim.answer.describe(stated, got)))
        # The census of shapes NOBODY recognised — H7's self-reporting half.
        bound = {c.cmd_start for c in claims}
        for line, cmd in unrecognised_spans(text, bound):
            declined["unrecognised-shape"] += 1
            declined_list.append(
                (rel, line, cmd,
                 "command-shaped, an integer is nearby, and NO claim shape "
                 "binds it — this tool cannot read the claim, if it is one"))

    if not quiet:
        print("doc-claim-check — executing the verification commands the documents quote")
        print("  surfaces scanned              : %d" % len(files))
        print("  claim shapes recognised       : %d (%s)"
              % (len(CLAIM_SHAPES), ", ".join(s.name for s in CLAIM_SHAPES)))
        print("  answer kinds recognised       : %d (%s)"
              % (len(ANSWER_KINDS), ", ".join(a.name for a in ANSWER_KINDS)))
        print("  claims executed and compared  : %d  (floor %d)"
              % (checked, MIN_EXECUTED_CLAIMS))
        print("  claims DECLINED (each named)   : %s"
              % " / ".join("%d %s" % (declined[b], label)
                           for b, label in DECLINE_BUCKETS))
        for rel, line, cmd, why in declined_list:
            print("      - %s:%d  %s  [%s]" % (rel, line, cmd[:70], why))
        print("  (a declined claim is UNVERIFIED, not passed — the count is the honest"
              " statement of this tool's coverage)")
    # Always printed, --quiet included: an excusal is a mismatch the tool chose
    # not to report, and the round-5/6 lesson is that those must never be
    # silent. Counted and NAMED, same rule as the declines.
    print("  %d mismatch(es) EXCUSED as dated records in append-only regions "
          "(a claim there records what the command returned AT THE TIME; an "
          "explicit \"now\"/\"currently\"/\"today\" pierces the excusal and is "
          "reported)" % len(excused))
    for rel, line, cmd, stated, got in excused:
        print("      - %s:%d  %s  [record says %d; command now returns %d]"
              % (rel, line, cmd[:60], stated, got))

    if findings:
        print("\nFAIL — %d stated value(s) the command does not reproduce:" % len(findings))
        for rel, line, cmd, what in findings:
            print("  %s:%d" % (rel, line))
            print("      command : %s" % cmd)
            print("      %s" % what)

    dangling = scan_fence_identifiers(repo, quiet)
    if dangling:
        print("\nFAIL — %d dangling identifier reference(s) in spec code fences:"
              % len(dangling))
        for rel, line, typ, mem, near in dangling:
            print("  %s:%s" % (rel, line))
            print("      `%s.%s` — the file declares `%s` but no member `%s`%s"
                  % (typ, mem, typ, mem,
                     ("; did you mean `%s`?" % near[0]) if near else ""))

    # Round 16 (H9). A run that looked at nothing must not be able to report
    # success, and neither missing surfaces nor a collapsed claim count used to
    # gate anything at all. These are exit 2, not 1: exit 1 means "a document
    # is wrong", which is a finding this tool made; exit 2 means "this tool
    # could not do its job", which is not a verdict on any document and must
    # never be readable as one. It therefore also OUTRANKS a mismatch — with
    # the surface set broken, the mismatches that were found are not the
    # mismatches that exist.
    blocked = list(missing)
    if checked < MIN_EXECUTED_CLAIMS:
        blocked.append(
            "only %d claim(s) executed and compared, below the floor of %d — "
            "see the COVERAGE FLOOR note beside SURFACE_GLOBS for how the "
            "floor is re-derived and when it may be changed"
            % (checked, MIN_EXECUTED_CLAIMS))
    if blocked:
        print("\nERROR — this run could not verify what it is supposed to "
              "verify, so its result is not a verdict on any document:")
        for why in blocked:
            print("  * %s" % why)
        return 2

    if findings or dangling:
        return 1

    print("\nPASS — every executable claim reproduced its stated value, and no"
          " spec code fence references an identifier its own file does not declare"
          " (with the coverage stated above).")
    return 0


def main():
    ap = argparse.ArgumentParser(
        description="Execute the verification commands quoted in this repo's "
                    "documents and diff the stated value against the real one.")
    ap.add_argument("--repo", required=True, help="repository root")
    ap.add_argument("--quiet", action="store_true")
    args = ap.parse_args()
    repo = pathlib.Path(args.repo).resolve()
    if not (repo / "CLAUDE.md").is_file():
        print("not a Tactical Director repo root: %s" % repo, file=sys.stderr)
        return 2
    return scan(repo, args.quiet)


if __name__ == "__main__":
    sys.exit(main())

# Version History
# | Version | Date       | Author      | Notes                                        |
# | 1.0     | 2026-08-18 | Claude Code | Initial. Closes the "verification prose is   |
# |         |            |             | never itself verified" class that AR rounds  |
# |         |            |             | 6-8 kept finding and review alone could not  |
# |         |            |             | stop finding: CLAUDE.md citing a grep that   |
# |         |            |             | refutes its own claim, §9.2 Q-04 ratifying   |
# |         |            |             | three IDs against six, §9.1 C-02 breaking    |
# |         |            |             | inside the commit that claimed to re-run it, |
# |         |            |             | §3.2.1's 218-vs-243 figure. Scope is         |
# |         |            |             | deliberately floored at single-integer       |
# |         |            |             | output — the class that actually bit — and   |
# |         |            |             | every declined claim is COUNTED AND PRINTED  |
# |         |            |             | rather than skipped, because rounds 5 and 6  |
# |         |            |             | both found this project's checkers hiding    |
# |         |            |             | real defects behind silent skips. Commands   |
# |         |            |             | are untrusted document text: every pipeline  |
# |         |            |             | segment must be an allow-listed read-only    |
# |         |            |             | binary (git further restricted to read-only  |
# |         |            |             | subcommands), no shell is used, redirection/ |
# |         |            |             | substitution/chaining are refused, and glob- |
# |         |            |             | dependent commands are DECLINED rather than  |
# |         |            |             | approximated — a guessed reproduction would  |
# |         |            |             | be a fabricated verification, which is the   |
# |         |            |             | defect this tool exists to catch.            |
# | 1.1     | 2026-08-19 | Claude Code | AR round-9 (3 High, 3 Medium, 2 Low), all   |
# |         |            |             | proven by reproduction before the fix and   |
# |         |            |             | re-proven in BOTH directions after. **H1:**  |
# |         |            |             | "read-only by construction (no writing      |
# |         |            |             | command is on the list)" was FALSE — the    |
# |         |            |             | allow-list gates argv[0] only, and `sed -i` |
# |         |            |             | was demonstrated rewriting a file in the    |
# |         |            |             | working tree while this tool printed PASS.  |
# |         |            |             | `python3 -c`, `find -delete`/`-exec`,       |
# |         |            |             | `sort -o`, `rg --pre`, `git -c`, `uniq IN   |
# |         |            |             | OUT` and `awk 'BEGIN{system()}'` were all   |
# |         |            |             | reachable, and ci.yml runs this on          |
# |         |            |             | pull_request, so document text reaches a    |
# |         |            |             | runner. Fixed by naming the hatches:        |
# |         |            |             | DENIED_FLAGS / DENIED_FLAG_PREFIXES, git    |
# |         |            |             | globals scoped to BEFORE the subcommand     |
# |         |            |             | (refusing them after it broke `git grep -c` |
# |         |            |             | — both live executable claims — caught by   |
# |         |            |             | re-measuring rather than by reasoning),     |
# |         |            |             | `sed` DROPPED (its write lives in its       |
# |         |            |             | script, not a flag), `awk` KEPT but with    |
# |         |            |             | system()/getline refused, because both      |
# |         |            |             | executable claims in the corpus use it and  |
# |         |            |             | dropping it would have made every run a     |
# |         |            |             | vacuous pass. **H2:** `shlex.split` discards |
# |         |            |             | quoting, so expand_globs treated a QUOTED   |
# |         |            |             | regex as a shell glob: `find . -name        |
# |         |            |             | '*.md' \| wc -l` ran as `find . -name       |
# |         |            |             | CLAUDE.md doc.md`. New quote-preserving     |
# |         |            |             | tokenize(); only tokens whose glob chars    |
# |         |            |             | are ALL unquoted expand. All 7 glob-char    |
# |         |            |             | tokens in the live corpus are quoted regex. |
# |         |            |             | It also splits pipelines on UNQUOTED `\|`   |
# |         |            |             | only, so `grep -c 'a\|b' f` stops being a   |
# |         |            |             | parse failure. **H3:** segment exit status  |
# |         |            |             | was never checked, so a FAILED segment's    |
# |         |            |             | empty output flowed downstream and `grep    |
# |         |            |             | -rn X nosuchdir/ \| wc -l` printed 0, which |
# |         |            |             | was reported as "document says 218; command |
# |         |            |             | returns 0" against a document that was not  |
# |         |            |             | wrong — the tool fabricating the finding it |
# |         |            |             | exists to prevent. Non-zero now DECLINES,   |
# |         |            |             | with grep/rg/diff/git 1 (no match / differ) |
# |         |            |             | as the named benign case. **M1:** the       |
# |         |            |             | unlisted-binary `continue` was a THIRD, and |
# |         |            |             | the only silent, decline path, while the    |
# |         |            |             | header and the file-manifest row both       |
# |         |            |             | published "every declined claim is counted  |
# |         |            |             | AND named". Counted and named now, behind a |
# |         |            |             | command-SHAPE discriminator so the ~1,100   |
# |         |            |             | backticked identifiers CLAIM also matches   |
# |         |            |             | (`SNAPSHOT_SCHEMA_VERSION` **20 → 21**) do  |
# |         |            |             | not drown it: 9 named, all real. **M2:** an |
# |         |            |             | absolute glob (`ls /etc/*.conf`) crashed    |
# |         |            |             | the tool with an uncaught                   |
# |         |            |             | NotImplementedError out of pathlib —        |
# |         |            |             | document text must not be able to crash the |
# |         |            |             | checker; declined by name instead. **L1:**  |
# |         |            |             | a backticked run of whitespace matches      |
# |         |            |             | CLAIM and crashed on `"".split()[0]`.       |
# |         |            |             | **L2:** the header never stated that only   |
# |         |            |             | the command-then-value SHAPE is recognised, |
# |         |            |             | so root CLAUDE.md's "8 scripts (`ls         |
# |         |            |             | tools/*.py`)" is invisible AND uncounted;   |
# |         |            |             | stated now. Also: every refusal now NAMES   |
# |         |            |             | the hatch instead of "not an allow-listed   |
# |         |            |             | read-only pipeline". Live tree unchanged at |
# |         |            |             | 2 executed / PASS; declines 21 → 30, the    |
# |         |            |             | 9 new ones being the previously-silent      |
# |         |            |             | class (later 21 -> 25 at round 10: five     |
# |         |            |             | "negated" declines were backticked          |
# |         |            |             | IDENTIFIERS beside a negation, never        |
# |         |            |             | claims, so counting them overstated the     |
# |         |            |             | coverage being given up in the very figure  |
# |         |            |             | that exists to state it honestly; the       |
# |         |            |             | command test now gates the negation test).  |
# |         |            |             | Verified on a scratch mirror: 10            |
# |         |            |             | hatch attempts all refused with the canary  |
# |         |            |             | file intact and no file created, and the    |
# |         |            |             | complement — the same quoted-glob command   |
# |         |            |             | with its TRUE value stated — passes.        |
# |         |            |             | Two further defects were found in THIS      |
# |         |            |             | round's own fix, by re-reviewing it as      |
# |         |            |             | hostilely as the original (round 8's        |
# |         |            |             | lesson, applied to round 9): the blind      |
# |         |            |             | `\|` unescape was harmless only while a     |
# |         |            |             | quoted pipe broke the parse — once the      |
# |         |            |             | tokenizer kept such a command runnable it   |
# |         |            |             | would have silently rewritten BRE           |
# |         |            |             | alternation (`grep -n "typeof\|GetFields"`, |
# |         |            |             | live in spec-error-log.md) into a literal   |
# |         |            |             | pipe and reported the wrong regex's count;  |
# |         |            |             | it is now quote-aware. And python3's script |
# |         |            |             | operand was checked for a `.py` suffix but  |
# |         |            |             | not for staying inside the repo, so         |
# |         |            |             | `python3 ../../evil.py` satisfied the       |
# |         |            |             | "in-repo script" rule its own comment       |
# |         |            |             | claimed to enforce.                         |
# | 1.2     | 2026-08-19 | Claude Code | AR round 12 — the VALUE-FIRST claim shape.  |
# |         |            |             | Round 9 (L2) named it unrecognised AND      |
# |         |            |             | uncounted and stopped at naming it; this    |
# |         |            |             | closes it, because the live instances are   |
# |         |            |             | the drift-prone kind: Spec #20 §5.4.5       |
# |         |            |             | states the assembly count as "35 (`ls -d    |
# |         |            |             | src/*/ \| wc -l`)" in APPROVED text, which   |
# |         |            |             | goes stale the day the 36th assembly lands  |
# |         |            |             | and which nothing watched. Seven live       |
# |         |            |             | value-first claims quote a real command;    |
# |         |            |             | all seven were verified by hand first and   |
# |         |            |             | all are currently TRUE, so this adds        |
# |         |            |             | coverage, not findings. Executed 2 -> 3.    |
# |         |            |             | The complement test caught a defect in the  |
# |         |            |             | addition itself before it landed: in this   |
# |         |            |             | shape the NEGATOR precedes the number ("no  |
# |         |            |             | longer 3 files (`cmd`)"), so reusing the    |
# |         |            |             | forward shape's forward-looking gap         |
# |         |            |             | reported a correctly-negated claim as a     |
# |         |            |             | mismatch. The window now looks back,        |
# |         |            |             | bounded to the line. Also: FORBIDDEN now    |
# |         |            |             | gates the unlisted-binary naming, so a      |
# |         |            |             | backticked EXPRESSION that merely looks     |
# |         |            |             | path-shaped (`permille/1000f > 0.6f`, live  |
# |         |            |             | in #33's appendices) is not announced as a  |
# |         |            |             | rejected binary — the mirror of the noise   |
# |         |            |             | command_shaped exists to suppress.          |
# | 1.3     | 2026-08-19 | Claude Code | AR round 13 — the DATED-RECORD model,       |
# |         |            |             | ported from doc-consistency-check rather    |
# |         |            |             | than reinvented. A claim in an append-only  |
# |         |            |             | record states what a command returned AT    |
# |         |            |             | THE TIME; when the figure later moves the   |
# |         |            |             | record is still correct, and this gate      |
# |         |            |             | would have failed CI on it. Not             |
# |         |            |             | hypothetical: at round 12 the pass writing  |
# |         |            |             | the CHANGELOG quoted Spec #20's own         |
# |         |            |             | drift-prone example verbatim INTO the       |
# |         |            |             | chain, which fails the day the 36th         |
# |         |            |             | assembly lands. It was caught and reworded, |
# |         |            |             | but "remember not to write that" is not a   |
# |         |            |             | mechanism. Regions come from                |
# |         |            |             | doc-consistency-check (frozen header chain  |
# |         |            |             | via the new shared frozen_chain_span, log   |
# |         |            |             | body, archive) so the two tools cannot      |
# |         |            |             | disagree about which bytes are frozen. Both |
# |         |            |             | of that model's load-bearing properties are |
# |         |            |             | kept: the claim is still EXECUTED and the   |
# |         |            |             | mismatch EXCUSED — counted and NAMED, never |
# |         |            |             | skipped, so "this historical figure no      |
# |         |            |             | longer reproduces" stays visible without    |
# |         |            |             | gating — and an explicit "now"/"currently"/ |
# |         |            |             | "today" PIERCES the excusal, because a      |
# |         |            |             | record asserting a value is current is a    |
# |         |            |             | present-tense claim wherever it sits.       |
# |         |            |             | 0 excusals on today's tree: prophylactic,   |
# |         |            |             | stated as such. Proved four ways on a       |
# |         |            |             | scratch mirror, in both region kinds        |
# |         |            |             | (frozen chain and log body): the head entry |
# |         |            |             | above the marker is REPORTED, a plain       |
# |         |            |             | record below it is EXCUSED and named, and a |
# |         |            |             | reasserted one below it is REPORTED.        |
# | 1.4     | 2026-08-19 | Claude Code | AR round 14 — two P1 holes in round 9's H1  |
# |         |            |             | fix, found by an EXTERNAL reviewer on PR    |
# |         |            |             | #328 and both verified exploitable here     |
# |         |            |             | before fixing. One root error behind both:  |
# |         |            |             | the validation ran on a SHAPE, not on the   |
# |         |            |             | argv that actually executes. (a) awk's      |
# |         |            |             | program was located as "first operand not   |
# |         |            |             | starting with -", but `-v` and `-F` take a  |
# |         |            |             | SEPARATE argument, so                       |
# |         |            |             | `awk -v x=1 'BEGIN{system(...)}' f` handed  |
# |         |            |             | the check `x=1` and never looked at the     |
# |         |            |             | program — system() ran and the command      |
# |         |            |             | returned the claimed integer under a        |
# |         |            |             | printed PASS. Every token is scanned now,   |
# |         |            |             | which needs no awk option grammar and       |
# |         |            |             | cannot be outflanked by adding one. The     |
# |         |            |             | same heuristic was wrong for python3        |
# |         |            |             | (`-X foo.py bar.py` runs bar.py), so the    |
# |         |            |             | script must be argv[1] with no interpreter  |
# |         |            |             | flags at all. (b) denied_flag ran BEFORE    |
# |         |            |             | glob expansion, so a repo file named        |
# |         |            |             | `--output=canary` turned the validated      |
# |         |            |             | `sort * \| wc -l` into                      |
# |         |            |             | `sort --output=canary …`, which WROTE that  |
# |         |            |             | file. An expanded name that would be read   |
# |         |            |             | as an option is refused, and the whole      |
# |         |            |             | escape-hatch check re-runs on the           |
# |         |            |             | post-expansion argv. Both re-proved: the    |
# |         |            |             | two exploits are declined and named,        |
# |         |            |             | neither artefact is created, and the live   |
# |         |            |             | claims still execute (3, unchanged).        |
# | 1.5     | 2026-08-21 | Claude Code | AR round 15 (5 High, 3 Medium) — the third  |
# |         |            |             | consecutive round to falsify this header's  |
# |         |            |             | read-only claim by reproduction, so the ROOT|
# |         |            |             | error is now stated in the SAFETY section   |
# |         |            |             | rather than the instances: an escape hatch  |
# |         |            |             | enumerated BY NAME, on an argument that is  |
# |         |            |             | itself a language, is a list of the hatches |
# |         |            |             | someone happened to think of. Round 14      |
# |         |            |             | called it "the validation ran on a SHAPE,   |
# |         |            |             | not on the argv that actually executes";    |
# |         |            |             | this round adds its sibling — the validation|
# |         |            |             | ran on the SPELLING, not on the option.     |
# |         |            |             | Every finding was reproduced BEFORE the fix |
# |         |            |             | and re-proved three ways after: exploit     |
# |         |            |             | DECLINED AND NAMED, no canary created and no|
# |         |            |             | ref destroyed, and a legitimate command of  |
# |         |            |             | the same shape still executed. **H1:** `awk |
# |         |            |             | 'BEGIN{print "touch X" | "sh"}'` is         |
# |         |            |             | arbitrary command execution using NEITHER   |
# |         |            |             | blacklisted word, and FORBIDDEN does not    |
# |         |            |             | reject a single `|` (tokenize keeps a quoted|
# |         |            |             | one literal, so it reaches the program      |
# |         |            |             | intact) — the canary was created under a    |
# |         |            |             | printed PASS. awk is KEPT, because the      |
# |         |            |             | header's old reason for keeping it          |
# |         |            |             | ("dropping it would take the tool to zero   |
# |         |            |             | verified claims") is STALE but its          |
# |         |            |             | conclusion is not: re-measured, 2 of the 3  |
# |         |            |             | executed claims are awk, so dropping it     |
# |         |            |             | costs two thirds of the live coverage, not  |
# |         |            |             | all of it. Its program is now ALLOW-listed  |
# |         |            |             | (AWK_ALLOWED_CALLS — an unknown call name is|
# |         |            |             | declined and named, so the next gawk builtin|
# |         |            |             | cannot be the next hatch) with `|` and `@`  |
# |         |            |             | refused outright. **H2:** `python3 <any     |
# |         |            |             | in-repo .py>` executed attacker-added code —|
# |         |            |             | on `pull_request` the checkout IS the PR    |
# |         |            |             | head, so a PR adding tools/pwn.py plus a    |
# |         |            |             | claim quoting it got arbitrary code run with|
# |         |            |             | write access; canary written, PASS, exit 0. |
# |         |            |             | The rationale in the header ("CI runs those |
# |         |            |             | anyway") was false: CI runs four NAMED      |
# |         |            |             | scripts. DROPPED, at zero coverage cost —   |
# |         |            |             | re-measured, no python3 claim has ever      |
# |         |            |             | executed (all decline as                    |
# |         |            |             | not-a-single-integer) — and added to        |
# |         |            |             | _KNOWN_BINARIES in the same change, or its  |
# |         |            |             | claims would have moved from a NAMED decline|
# |         |            |             | to an invisible one, which is the decline   |
# |         |            |             | contract failing in the direction this file |
# |         |            |             | calls its worst. **H3:** the deny-list      |
# |         |            |             | compared the SPELLING, so `sort             |
# |         |            |             | -oSORT_CANARY` wrote the file and `git grep |
# |         |            |             | -O./p.sh` executed the script while `-o` and|
# |         |            |             | `-O` sat in the list; the option CORE is    |
# |         |            |             | compared now, whatever attaches to it (new  |
# |         |            |             | _option_cores: a short token is decomposed  |
# |         |            |             | into its whole cluster, a long one split on |
# |         |            |             | `=`). **H4:** GIT_READONLY listed `branch`  |
# |         |            |             | and `tag`, which DELETE refs — both proven  |
# |         |            |             | destroying a ref in a fixture repo — and    |
# |         |            |             | `git diff --output=` wrote a file. Both     |
# |         |            |             | subcommands dropped, `--output` denied.     |
# |         |            |             | **H5:** `sort --compress-program=` runs an  |
# |         |            |             | arbitrary program whenever a sort spills,   |
# |         |            |             | which `-S 1` forces; denied, and every      |
# |         |            |             | remaining allow-listed binary audited in the|
# |         |            |             | same pass (gawk's -l/-i/-E added). **M1:**  |
# |         |            |             | output was buffered whole with no cap —     |
# |         |            |             | TIMEOUT_S bounds wall time, not memory: one |
# |         |            |             | document line drove the checker to 587.6 MB |
# |         |            |             | (measured, own peak RSS), and `cat          |
# |         |            |             | /dev/zero` OOM-kills the runner before 60 s |
# |         |            |             | elapse. Read incrementally against an 8 MiB |
# |         |            |             | cap, child killed, declined and named; same |
# |         |            |             | line now peaks at 31.0 MB. **M2:** an       |
# |         |            |             | embedded NUL raised an uncaught ValueError  |
# |         |            |             | (subprocess raises it, and it is not a      |
# |         |            |             | SubprocessError), aborting the WHOLE scan in|
# |         |            |             | a traceback — every later claim and the     |
# |         |            |             | entire dangling-identifier check never ran, |
# |         |            |             | masking any real defect behind a crash. NUL |
# |         |            |             | added to FORBIDDEN, ValueError caught as the|
# |         |            |             | backstop. **M3:** nothing confined the      |
# |         |            |             | OPERANDS, so `grep -c . /etc/passwd` was a  |
# |         |            |             | one-integer read oracle over the host and   |
# |         |            |             | `cat ../OUTSIDE.txt` reproduced its value   |
# |         |            |             | under PASS; every non-option operand must   |
# |         |            |             | now realpath inside the repo, checked AFTER |
# |         |            |             | expansion. Live tree unchanged: PASS, exit  |
# |         |            |             | 0, 3 executed (the same 3, by name), 30     |
# |         |            |             | declines — the only movement is two python3 |
# |         |            |             | claims changing decline REASON. Siblings    |
# |         |            |             | re-run green.                               |
# | 1.6     | 2026-08-21 | Claude Code | AR round 16 (4 High, 2 Medium) — the         |
# |         |            |             | through-line is that this tool's whole value |
# |         |            |             | is not lying about its own coverage, and it  |
# |         |            |             | was lying in its header, in its buckets and  |
# |         |            |             | in its verdict. **H11 (landed first, so the  |
# |         |            |             | rest sit on the seam):** scan() was a        |
# |         |            |             | 155-line monolith doing surface collection,  |
# |         |            |             | matching for two shapes with two hand-       |
# |         |            |             | written and mutually inconsistent negation   |
# |         |            |             | windows, per-claim                           |
# |         |            |             | validate/run/compare/excuse, six-bucket      |
# |         |            |             | counting, four print blocks, an unrelated    |
# |         |            |             | check and exit composition, with "single     |
# |         |            |             | integer" hard-wired at six sites. Three      |
# |         |            |             | seams extracted — a Claim value type,        |
# |         |            |             | CLAIM_SHAPES whose entries own their find()  |
# |         |            |             | and negation_window(), ANSWER_KINDS owning   |
# |         |            |             | parse/read/compare/describe — leaving scan() |
# |         |            |             | to iterate and report. M4 existed BECAUSE    |
# |         |            |             | there was no seam. **H7:** the coverage      |
# |         |            |             | statement was false by omission.             |
# |         |            |             | Instrumented over its own surfaces, 2 of the |
# |         |            |             | 3 executed claims were the same command      |
# |         |            |             | pinned to an immutable revision, so live     |
# |         |            |             | drift-catching coverage was ONE claim, while |
# |         |            |             | command-shaped spans with a nearby integer   |
# |         |            |             | bound to neither regex and appeared in NO    |
# |         |            |             | bucket — including root CLAUDE.md's design-  |
# |         |            |             | supplement count, the flagship case whose    |
# |         |            |             | own sentence records that it drifted 42 ->   |
# |         |            |             | 60 undetected. Past-tense verbs were         |
# |         |            |             | unmatchable (the \b after `returns?` kills   |
# |         |            |             | "returned"), the colon form was unknown, and |
# |         |            |             | an attribution clause broke the value-first  |
# |         |            |             | gap. Two shapes added (colon; value +        |
# |         |            |             | attribution clause) and the verb list        |
# |         |            |             | widened — and, the half that matters more,   |
# |         |            |             | an `unrecognised-shape` bucket now COUNTS    |
# |         |            |             | AND NAMES every command-shaped span with an  |
# |         |            |             | integer near it that no shape binds (143 on  |
# |         |            |             | this tree), so a shape gap is self-reporting |
# |         |            |             | instead of self-concealing. **H8:** the      |
# |         |            |             | value-first extractor bound a date year or a |
# |         |            |             | range endpoint as the stated value — "August |
# |         |            |             | 18, 2026 (`cmd`)" parsed as 2026 and FAILED  |
# |         |            |             | a correct sentence, and the live tree        |
# |         |            |             | already extracts 2026 twice, escaping only   |
# |         |            |             | because those spans hold ERR ids rather than |
# |         |            |             | runnable commands. Dates are refused (no     |
# |         |            |             | claim exists there); ranges and              |
# |         |            |             | approximation markers are DECLINED AND NAMED |
# |         |            |             | as `approximate-or-range` rather than        |
# |         |            |             | compared. **H9:** a run that checked nothing |
# |         |            |             | printed PASS. MISSING SURFACE incremented no |
# |         |            |             | counter and gated nothing — proven with 8 of |
# |         |            |             | 9 surfaces deleted: 1 claim executed, PASS,  |
# |         |            |             | exit 0 — and the globbed half had no missing |
# |         |            |             | concept at all, so renaming one folder took  |
# |         |            |             | executed to 0 with no output line changed. A |
# |         |            |             | missing named surface, a glob matching no    |
# |         |            |             | file, and a claim count below                |
# |         |            |             | MIN_EXECUTED_CLAIMS are now exit 2, a class  |
# |         |            |             | that outranks exit 1 because it is not a     |
# |         |            |             | verdict on any document. The floor is re-    |
# |         |            |             | derived by running the tool, not typed.      |
# |         |            |             | **M4:** the look-back negation window was    |
# |         |            |             | added to only one of the two shapes; the     |
# |         |            |             | forward shape still searched the text AFTER  |
# |         |            |             | its gap, which can only hold the arrow and   |
# |         |            |             | the digits, so "before this fix, `grep -c …  |
# |         |            |             | f` -> 4" was reported as a mismatch while    |
# |         |            |             | the value-first phrasing of the same         |
# |         |            |             | sentence correctly declined. One shared      |
# |         |            |             | negation_window() now, on the seam. **M5:**  |
# |         |            |             | NEGATOR held bare `not`/`was`/`never`, which |
# |         |            |             | saturate this repo's prose, and an excusal   |
# |         |            |             | costs coverage silently: "…is not in         |
# |         |            |             | dispute: 99 assemblies (`ls -d src/*/ \| wc  |
# |         |            |             | -l`)" — wrong by a factor of 33 — was        |
# |         |            |             | declined as historical, 0 executed, PASS.    |
# |         |            |             | Restricted to verb-bound phrases that can    |
# |         |            |             | only negate a claim. **H6 (landed last, on   |
# |         |            |             | purpose):** the header said the value-first  |
# |         |            |             | shape "is not matched at all" while the      |
# |         |            |             | baseline run printed that exact claim in its |
# |         |            |             | declined list, the exit table named one of   |
# |         |            |             | two ways to return 1, and CHECK 2 was        |
# |         |            |             | documented nowhere. Rewritten, and the blind |
# |         |            |             | spot is now DERIVED from the unrecognised-   |
# |         |            |             | shape bucket rather than restated as prose   |
# |         |            |             | that drifts. EVERY matcher change was proved |
# |         |            |             | in BOTH directions — 46 cases, each a claim  |
# |         |            |             | that must now bind and its complement that   |
# |         |            |             | must not — and the complement caught three   |
# |         |            |             | defects in this round's own work before they |
# |         |            |             | landed: `read` on the verb list bound the    |
# |         |            |             | refuted 42 and FAILED root CLAUDE.md; an     |
# |         |            |             | 80-character attribution gap bound the 0 of  |
# |         |            |             | "Stage 0 status" 71 characters away and      |
# |         |            |             | FAILED #20 §7.2; and the census's            |
# |         |            |             | command_shaped() test excluded every ALLOW-  |
# |         |            |             | LISTED binary, so a runnable command in an   |
# |         |            |             | unknown shape — the most valuable thing that |
# |         |            |             | bucket can report — was the one class it     |
# |         |            |             | could not see. Bare `pre-fix` was dropped    |
# |         |            |             | from NEGATOR on the same evidence: it is a   |
# |         |            |             | noun modifier here ("the pre-fix commit"),   |
# |         |            |             | and with M4's new look-back it excused a     |
# |         |            |             | live claim pinned to an immutable revision.  |
# |         |            |             | Live tree: 3 executed -> 6, PASS, exit 0;    |
# |         |            |             | declines 30 -> 40 named plus the 143-entry   |
# |         |            |             | census, every delta accounted for. Siblings  |
# |         |            |             | re-run green.                                |
