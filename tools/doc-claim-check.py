#!/usr/bin/env python3
# doc-claim-check.py — execute the verification commands this repo's documents
# quote, and diff the stated value against what they actually print.
#
# File:     tools/doc-claim-check.py
# Created:  August 18, 2026
# Modified: August 22, 2026
# Author:   Claude Code
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
#             shares a surface set (the SURFACE_GLOBS half) and because a
#             second script would be a second thing to remember to run — it
#             does NOT share CHECK 1's exit code except when both checks fail
#             in the same run; see the exit-code table below.
#
# WHY THIS EXISTS
# ---------------
# Rounds 4-8 diagnosed one recurring shape: **verification prose is never itself
# verified.** The repo writes claims in a machine-checkable form constantly —
# "(`grep -c '^- \*\*' docs/tracking/open-issues.md` → 18)" — and nothing ever
# re-ran them. Four measured instances, each of which survived multiple hostile
# human-grade review rounds: root CLAUDE.md cited a grep as proof of a claim
# that grep REFUTES, on the day it was written; Spec #20 §9.2 Q-04 ratified
# "three distinct IDs" against an actual six, in the checklist file whose own
# header carries a fabrication prohibition; §9.1 C-02's recorded value stopped
# reproducing inside the very commit that claimed to have re-run every §9.1 and
# §9.2 command; §3.2.1 offered "218 occurrences" as proof of a command that
# returned 243, because the fix that wrote the sentence added 25. None is
# subtle. Checking them means running seventeen commands by hand, and a
# reviewer under time pressure reads the number instead. A tool cannot get
# bored.
#
# WHAT IT DOES NOT DO, STATED UP FRONT
# ------------------------------------
# This tool verifies claims whose command prints a SINGLE INTEGER. That is a
# deliberate floor, not an oversight: it is the class that has actually bitten
# (counts, tallies, cardinalities), and it is the class where "what the stated
# value should be" is unambiguous. A claim whose command prints prose, a table,
# or a multi-line report is COUNTED AND NAMED as unverified rather than silently
# skipped — the round-5/6 lesson is that a checker's silent skips are where the
# next defect hides. `--quiet` suppresses the itemized per-claim decline LIST
# and keeps every count; CI runs without it.
#
# It recognises FOUR claim SHAPES, listed in CLAIM_SHAPES and printed by every
# run so the reader never has to trust this comment:
#   1. command, then the value       "`cmd` → 18", "`cmd` returned 218"
#      Its two routes have DIFFERENT gap grammars, and the asymmetry is the
#      rule, not an accident: an arrow cannot belong to a neighbouring clause,
#      a VERB can. So the verb route admits only whitespace, closing markup
#      and adverbs between the command and its verb, and only whitespace and
#      markup between the verb and the value. Ordinary English was otherwise
#      binding integers out of unrelated clauses, and the census could not see
#      it, because a shape had bound it. (Locked by VerbGapPrecisionTests.)
#   2. command, colon, value         "`cmd`: **0**"
#   3. value, then the command in parentheses    "8 scripts (`ls tools/*.py`)"
#   4. value, an attribution clause, the command
#                                    "60 files — re-derived by `ls … \| wc -l`"
#                                    "35 assemblies via `ls -d src/*/ \| wc -l`"
#      It also reads `with`, the bare stem `re-derive`, and a wrap prefix
#      before the command, because this repo hard-wraps mid-sentence inside an
#      ASCII tree diagram and a live claim there was bound by nothing.
#
# THE RESIDUAL BLIND SPOT IS THE POINT OF THIS PARAGRAPH, and it is no longer
# described by naming instances someone happened to notice. Two earlier rounds
# each published a blind spot as prose, and the prose then drifted — one of
# them left this section asserting that a shape "is not matched at all", which
# the very next run refuted by printing that exact claim in the declined list.
# So the blind spot is DERIVED now, not written down: every backticked span
# that reads as a command, has an integer near it — on its own line, or on the
# line either side when its own line carries no digit at all, because this repo
# hard-wraps mid-sentence — and binds to NO shape is counted and named in the
# `unrecognised-shape` decline bucket. Read that bucket, not this comment, for
# what the tool cannot see; if a claim shape is worth learning, it will be
# sitting in that list under its own file and line.
#
# TWO THINGS STILL REACH NO BUCKET, named rather than implied, because every
# previous version of this paragraph was falsified by the next round:
#   * a backticked span whose head is an argument-less path (a bare
#     `tools/count-supplements.sh`) is refused by the census's own
#     command-shape test, which requires "a binary-shaped head plus an
#     argument only a command takes" so that the backticked IDENTIFIERS this
#     corpus is full of do not drown the bucket. Re-derived 2026-08-22 by
#     running `command_shaped()` over every `COMMAND_SPAN` match in every
#     file `collect_surfaces()` returns: it refuses 70,462 spans and admits
#     220. (The figure that stood here, 53,804 / 195, reproduced under no
#     variant of that walk — named surfaces alone, globbed alone, or with
#     the census's own `head in ALLOWED_CMDS` disjunct added — so it is
#     replaced rather than carried, and the METHOD is stated with it so the
#     next reader can re-run it instead of trusting it. Do NOT copy a figure
#     here from
#     check_claim's own note below: that one counts a DIFFERENT population —
#     identifiers a claim SHAPE bound and check_claim then had to dispose of
#     — and moving a number between the two is a defect this file has already
#     filed once.) That predicate cannot tell the two apart, and the span
#     stays invisible even with the bound set emptied — so it is a limit of
#     the SHAPE TEST, not of the census's span reservation.
#   * an integer further from the span than UNRECOGNISED_RADIUS, or two
#     wrapped lines away.
#
# THAT SENTENCE WAS FALSE FOR ONE WHOLE CLASS until round 19, and it was false
# in the way this file keeps finding: "reads as a command" was a HAND-WRITTEN
# LIST of binary names. A claim quoting a binary on neither curated list
# (`comm`, `tac`, `pcregrep`, `tree`, `nl`, `du`) was dropped with no bucket,
# no count and no line, and could not reach the census either, because the
# claim shape HAD bound the span. The test is a positive SHAPE now — a
# binary-shaped head plus an argument only a command takes — so a binary
# nobody here has heard of is named rather than vanishing. It was latent when
# found (0 live instances), which makes it a false statement about coverage
# rather than a missed defect; the statement was the thing that had to change.
# See command_shaped() and the measurement beside it.
#
# The same rule governs the answer side. ANSWER_KINDS holds exactly one entry
# — DERIVED from the shapes that name it, never written out — and the
# single-integer floor above is real; a claim whose command prints prose, a
# table or a multi-line report lands in `not-a-single-integer`, which is a
# count of what a second answer kind would be worth.
#
# SAFETY
# ------
# Commands come from documents, so they are untrusted input, and `ci.yml` runs
# this tool on `pull_request` — so document text reaches a CI runner. Every
# pipeline segment must match ALLOWED_CMDS; anything with redirection, command
# substitution, chaining, or an unlisted binary is refused and counted, the
# invocation never goes through a shell, and it runs under a timeout.
#
# Being on ALLOWED_CMDS is NOT by itself the safety property, and round 9 found
# the header claiming it was: "read-only by construction (no writing command is
# on the list)" was false, demonstrated by `sed -i` rewriting a file in the
# working tree while the tool printed PASS. Several genuinely read-only tools
# carry a write or execute escape hatch, and they carry READ hatches too —
# the harder half, because nothing in rule 6 below bounds a read. Every one is
# a named regression test in tools/tests/test_doc_claim_check.py
# (HatchRegressionTests, GitRefRegressionTests, AwkEnvironmentAndLoadHatchTests,
# AwkArgvArgcTests, SymlinkTraversalTests, Files0FromTests,
# GnuLongOptionAbbreviationTests, ClusteredShortOptionTests,
# PostExpansionRevalidationTests, ChildResourceLimitTests), so the exploits are
# not recited here; the RULES they produced are, because a test cannot state
# why a rule has the shape it has.
#
# TWO GENERALISATIONS, EACH PAID FOR BY SEVERAL ROUNDS OF THE SAME CLASS:
#
#   (i) AN ESCAPE HATCH ENUMERATED BY NAME, ON AN ARGUMENT THAT IS ITSELF A
#       LANGUAGE, IS A LIST OF THE HATCHES SOMEONE HAPPENED TO THINK OF.
#       Round 9 named `system` and `getline` for awk; round 15 executed
#       `awk 'BEGIN{print "touch X" | "sh"}'`, which uses neither. Round 14 put
#       it as "the validation ran on a SHAPE, not on the argv that actually
#       executes"; round 15 added its sibling — the validation ran on the
#       SPELLING, not on the option (`sort -o` was denied while `sort -oFILE`
#       wrote the file). Round 18 made it four consecutive rounds, each inside
#       the previous round's fix, because `getopt_long` accepts any unambiguous
#       PREFIX of a long name. Round 21 made it five, inside round 18's own
#       fix: an attached value read as `tok[2:]` is the value only when the
#       option letter is the FIRST character, and a CLUSTER puts it further
#       along. The fix that ended the chain is the first one that enumerates
#       nothing: an attached short-option value is a SUFFIX of its token by
#       the definition of `getopt`, so EVERY suffix is tested and the question
#       "which letters take a value" — whose five successive wrong answers are
#       this list — is never asked. See _attached_option_values(). It closes
#       the SPELLING dimension of a dash-token completely, and says nothing
#       about a path reaching a child by any other route.
#
#  (ii) THE SET OF ARGV TOKENS IS NOT THE SET OF PATHS A CHILD OPENS. Round 22
#       is (i)'s closing disclaimer coming true, twice, in the dimension it
#       explicitly gave up: a path can be a STRING LITERAL INSIDE A PROGRAM
#       (awk's `ARGV[ARGC++]="/etc/passwd"` makes awk open that file as
#       ordinary main input — no pipe, no `getline`, no disallowed call, no
#       forbidden character, and confinement never sees it because it is not an
#       operand), and a path can be reached by TRAVERSAL (a recursing binary
#       walking through a symlink inside an in-repo directory operand whose own
#       realpath passes) or by FILE CONTENTS (`--files0-from=F` takes the paths
#       it opens out of F's bytes, where no operand check can reach them).
#       The OS child limits (rule 6) touch none of it, because every one of
#       them bounds writing, CPU or memory and all three of these are READS.
#       Rule 4 below therefore states what it enforces rather than what it
#       reaches for, and NAMES the routes it cannot follow.
#
# So the sixth rule below is a different KIND of rule, added for that
# recurrence rather than for any one report: after four rounds of closing
# respellings by name, the read-only property is moved off this file's
# judgement and onto the kernel — and round 21 is the measured proof that this
# was necessary but not sufficient, since the limits do not touch a READ.
# Enumeration is still necessary — the limits do not stop reads, and reads are
# how this tool's answers are fabricated — but it is no longer the only thing
# standing between a document line and the runner's filesystem.
#
# The property therefore rests on six things together, in this order of
# importance:
#   1. ALLOW-LISTS WHEREVER THE ARGUMENT IS A LANGUAGE. `sed` and `python3` are
#      DROPPED. For python3 the old rationale ("a `.py` file the checkout
#      already contains — CI runs those anyway") was simply false: CI runs four
#      NAMED scripts, and on `pull_request` the checkout IS the pull request's
#      head, so a PR that adds `tools/pwn.py` and a claim quoting it had
#      arbitrary code executed with write access. `awk` is KEPT, and the
#      argument for keeping it is qualitative rather than a coverage fraction:
#      a real, currently-reproducing claim on this tree pipes through it, so
#      dropping it costs measured coverage. (The SCRIPT-hatches paragraph
#      beside AWK_ESCAPES below is the canonical site for that fact — the
#      fraction used to be stated here too and had drifted wrong in both
#      places.) Its program is allow-listed (AWK_ALLOWED_CALLS) rather than
#      blacklisted, plus a flat refusal of `|` and `@` in any awk token and of
#      every awk SPECIAL ARRAY (AWK_SPECIAL_ARRAYS — `ARGV`/`ARGC`,
#      `ENVIRON`/`PROCINFO`, gawk's `SYMTAB`/`FUNCTAB`). Those are variables,
#      not calls, so the call allow-list structurally cannot see them.
#   2. Each binary's `denied_flags` (BINARIES — one `Binary` record per
#      allow-listed binary) compared on the option CORE, so an attached value
#      (`-oFILE`, `-O./p.sh`) or an un-enumerated `--long=value` cannot respell
#      a denied hatch past the check — and a denied long option's NAME is also
#      matched as a PREFIX (`_abbreviated_denial()`), because `getopt_long`
#      accepts `--o=FILE` for `--output`. Over-refusal is the safe direction
#      and no live claim uses an abbreviated long option.
#   3. GIT_READONLY holding only subcommands that cannot destroy anything —
#      `branch` and `tag` are gone, because `-D`/`-d` delete refs, and
#      `--output` is denied because a diff writes a file with it.
#   4. PATH CONFINEMENT, checked after glob expansion. The property it is
#      REACHING FOR is "a command may read the checkout and nothing else";
#      the property it ENFORCES is narrower and is stated that way here from
#      round 22 on, because every previous statement of it was falsified by
#      the next round's reproduction. What it enforces:
#        (a) every OPERAND, and every value a dash-token can carry, realpaths
#            inside the root (escaping_operand);
#        (b) no operand DIRECTORY contains a symlink leaving the root, at any
#            depth (escaping_symlink_under) — so a recursing command cannot
#            walk out of the checkout even if this file has mismodelled which
#            of its flags follow links;
#        (c) the routes that reach a path by NEITHER of those are refused by
#            NAME, per binary and per language, because there is nothing in
#            argv for a containment rule to test: awk's `ARGV`, and the
#            `--files0-from` / `-files0-from` family on `wc`, `sort` and
#            `find`, which read the paths to open out of another file's bytes.
#      (c) is an enumeration and is therefore the weak leg, deliberately kept
#      visible as such rather than folded into the sentence above it. Without
#      any of this, `grep -c . /etc/passwd` was a one-integer read oracle over
#      the host. What remains unenforced by this file, named rather than
#      implied — see escaping_symlink_under()'s docstring: a recursion with NO
#      operand (defaulting to `.`), which the flag denials and `needs_file`
#      cover instead; a binary's own config or dot-files (git's `.git/config`,
#      rg's `RIPGREP_CONFIG_PATH`), none of which a DOCUMENT can point
#      anywhere; a path inside a `key=value` bare operand; the environment;
#      and TOCTOU between the symlink walk and exec, against an external
#      writer this tool has never claimed to defend.
#   5. RESOURCE BOUNDS: no shell, a wall-clock timeout AND a hard cap on how
#      much a segment may print, because a timeout does not bound memory —
#      one document line drove the checker to 587 MB and `cat /dev/zero` would
#      OOM-kill the runner first. NUL is refused up front and ValueError
#      caught, so document text cannot abort the scan.
#   6. OS-ENFORCED CHILD LIMITS — the only rule here that is not an
#      enumeration, and the only one that holds against a hatch nobody has
#      thought of. Every child runs under RLIMIT_FSIZE=0, RLIMIT_CPU and
#      RLIMIT_AS, set in the forked child before exec. It was demonstrated on
#      a hatch fixed by no name in this file: `git status \| wc -l` passes the
#      allow-list, every deny-flag and confinement, and REWRITES `.git/index`.
#      State its LIMITS honestly, because a safety claim stated too broadly is
#      the error this section keeps recording:
#        * it does not stop READS at all. Rule 4 is still the only thing
#          between a document line and every file the runner can read.
#        * it does not stop EXECUTION. A permitted `--compress-program`
#          would still run its script; it just could not write.
#        * RLIMIT_FSIZE=0 stops a file GROWING past zero bytes. Creating an
#          empty file and TRUNCATING an existing one still succeed — measured,
#          not assumed. So rules 1-4 are load-bearing exactly as before, and
#          nothing was relaxed because these limits exist.
#        * it is POSIX-only. Without a `resource` module the tool degrades to
#          the pre-limit behaviour and PRINTS that it has done so
#          (child_limit_summary(), on every run) rather than losing the
#          property silently.
#        * a child killed mid-write can leave a partial artefact its own
#          cleanup would have removed: `git status` leaves a zero-byte
#          `.git/index.lock`. Recorded rather than worked around — exempting
#          git from the limit would exempt the one binary here that writes.
#        * the limits are the STRICTEST of what this file asks for and what
#          the runner already imposed — both fields, soft and hard. That was
#          the stated guarantee before it was the implemented one: clamping
#          against the inherited HARD limit alone RAISED an inherited soft
#          limit. RLIMIT_FSIZE=0 was never affected and still is not, but only
#          because the clamp excludes RLIM_INFINITY from the comparison rather
#          than calling `min()` on it — see _tightest(), which carries that
#          argument, because it is a trap rather than a nicety.
#
# Every one of those refusals is COUNTED AND NAMED in the printed output. That
# is not politeness: a silent refusal is indistinguishable from a pass, which
# is the defect this whole tool exists to deny itself.
#
# Exit codes. Three kinds of non-zero, meaningfully distinct in what they
# report about a run:
#   0 = every executable claim reproduced its stated value, no dangling
#       identifier, and the run actually looked at what it is supposed to.
#   1 = CHECK 1 FOUND A DOCUMENT WRONG: at least one stated value does not
#       reproduce. Also returned when CHECK 1 AND CHECK 2 both fail in the
#       same run — 1 is the long-standing, documented code and keeps priority
#       over 3 so nothing downstream that already keys off "1 means a document
#       claim is wrong" starts reading 3 as a novel kind of failure it isn't.
#   2 = THIS TOOL COULD NOT DO ITS JOB, so its result is not a verdict on any
#       document: a usage error, a named surface missing from the tree, a
#       surface glob matching no file, fewer claims executed than
#       MIN_EXECUTED_CLAIMS, fewer LIVE (gate-capable) claims than
#       MIN_LIVE_CLAIMS, CHECK 2's own recall below its floors, or the
#       doc-consistency-check.py import CHECK 1's dated-record excusal depends
#       on failing to load or missing the surface this file calls through it.
#       It outranks 1 and 3, because with the surface set (or the import)
#       broken, the mismatches that were found are not the mismatches that
#       exist. Before this existed, deleting 8 of the 9 named surfaces printed
#       eight MISSING SURFACE lines, checked one claim, and exited 0 with PASS.
#   3 = CHECK 2 FOUND A DOCUMENT WRONG, AND CHECK 1 DID NOT: at least one spec
#       code fence references an identifier its own file does not declare, with
#       every claim CHECK 1 checked reproducing. This was split out of 1, which
#       had fused two unrelated defect classes into one number; the printed
#       FAIL block always named which, but the exit code did not. From the day
#       this file was written until that split, the dangling-identifier path
#       returned 1 — that history is why 1 still wins when both checks fail,
#       rather than 3 taking over as the newer code.
#
#       STATED HONESTLY RATHER THAN LEFT IMPLIED: "the thing automation
#       actually branches on" would overclaim a consumer this repo does not
#       have. `ci.yml`'s own step is a single `run:` line, and GitHub Actions
#       fails a `run:` step on ANY non-zero exit identically; nothing in this
#       repo currently reads 1 vs 2 vs 3 apart. The split is not wasted (a
#       reader with the log open, or a future step written to branch on it,
#       gets a real answer instead of one bit), but today it has no consumer,
#       and this file should not claim one it does not have.

import argparse
import importlib.util
import os
import pathlib
import re
import signal
import subprocess
import sys
import tempfile
import threading

try:                                                  # POSIX only; see the
    import resource                                   # CHILD RESOURCE LIMITS
except ImportError:                    # pragma: no cover - non-POSIX host
    resource = None                                   # section below.


def _load_consistency():
    """Import tools/doc-consistency-check.py (hyphenated name, so via importlib).

    Round 13: this tool needs the SAME answer to "which bytes of this document
    are a dated record?" that the citation checker already computes. Importing
    it rather than restating it is deliberate — two tools disagreeing about
    where the frozen header chain ends would mean one excusing a record the
    other reports, and a second copy of a definition is the duplicate-claim
    defect this repo keeps filing. The cost is a hard dependency between the
    two checkers; both are steps of the same CI job, so a break in either
    already fails that job.

    May raise — the caller (_ensure_consistency_module below) is where that is
    handled; this function's only job is the load itself."""
    q = pathlib.Path(__file__).with_name("doc-consistency-check.py")
    spec = importlib.util.spec_from_file_location("doc_consistency_check", q)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


# The imported surface this file actually calls through DCC. Round 17 (M13):
# a renamed helper used to fail as an AttributeError mid-scan, after this run
# had already printed part of a report — a partial report with no verdict is
# worse than no report, because it reads like a finished one. Checked as one
# batch, immediately after the load, so a contract break is reported before a
# single line of scan output exists.
#
# Round 24 (L10): `frozen_chain_span` used to sit in this tuple, but nothing
# in this file has called `DCC.frozen_chain_span` since the round-19 (H13)
# rewrite that made `dated_record_regions` derive frozen spans from
# `blank_frozen_history`'s own offset-preserving diff instead — the comment
# above was a false sentence about the code below it, and the entry was
# over-strict rather than unsafe: it would have exited 2 on a legitimate
# rename or removal of a sibling function this file never touches. Dropped;
# every name left here is one `grep -n "DCC\." tools/doc-claim-check.py`
# away from being reproven live.
_DCC_CONTRACT = ("record_regions", "sentence_window",
                 "LOG_BODY_FILES", "blank_frozen_history")

DCC = None


def _ensure_consistency_module():
    """Load and validate the doc-consistency-check.py import. Returns an error
    string on failure, or None once `DCC` is set and safe to call through.

    Round 17 (M13). Before this, `DCC = _load_consistency()` ran at IMPORT
    TIME as a bare module-level statement: a missing sibling raised out of
    `spec.loader.exec_module` as an uncaught OSError, printing a raw traceback
    and exiting 1 — the same exit code CHECK 1 uses for "a stated value did
    not reproduce", so a broken import was indistinguishable from a real
    finding on the number alone. And because it ran at import time, it fired
    for `--help` too, before argparse ever got to print anything. Deferred to
    here, called once from `main()` after `--repo`/`--quiet` are parsed but
    before `scan()` does any work, so `--help` never touches it and a failure
    is reported through this file's own named-error convention (exit 2 — "this
    tool could not do its job") rather than a stack trace."""
    global DCC
    if DCC is not None:
        return None
    try:
        mod = _load_consistency()
    except Exception as exc:                          # pragma: no branch
        return ("could not import tools/doc-consistency-check.py (%s: %s) — "
                "CHECK 1's dated-record excusal has no answer to work with"
                % (type(exc).__name__, exc))
    missing = [name for name in _DCC_CONTRACT if not hasattr(mod, name)]
    if missing:
        return ("tools/doc-consistency-check.py no longer exposes %s — the "
                "imported contract this file depends on has changed"
                % ", ".join(missing))
    DCC = mod
    return None

# ---------------------------------------------------------------------------
# ONE `Binary` RECORD PER ALLOW-LISTED BINARY, and ALLOWED_CMDS is DERIVED as
# its key set. Every fact about a binary used to live in up to eight separate
# parallel tables with nothing cross-checking any of them, so adding one
# obliged the author to remember all the others — and a stale `sed` row
# survived in the needs-a-file table for six rounds after `sed` left the
# allow-list, which is the failure mode rather than an anecdote about it.
# Deriving membership from the records means a binary that is not allowed
# cannot carry a stray row in any other field, and an allowed one has all its
# security-relevant properties in one place by construction.
# `_KNOWN_BINARIES` stays separate below: it deliberately names binaries this
# file does NOT allow, which is not a property `Binary` has any field for.
class Binary:
    """One allow-listed binary's read-only contract:
      denied_flags     — exact denied option cores. NO BINARY BELOW NEEDS A
                          PREFIX ENTRY, and that is structural rather than
                          lucky: `_option_cores()` already reduces
                          `--flag=value` to the bare core `--flag`, and
                          `_abbreviated_denial()` already catches any
                          unambiguous GNU prefix of a denied long name, so a
                          third, separate prefix table could only ever repeat
                          what those two refuse. One existed, carried twelve
                          entries that were all exactly that redundancy, and
                          was removed. Do not reintroduce it.
      benign_exit      — exit codes that are a RESULT, not a failure (grep-
                          family 1 = "no match", diff 1 = "files differ")
      needs_file       — the first pipeline segment must name a real on-disk
                          file (or directory — round 20, M24) among its
                          operands, or it is declined as reading from an
                          (empty) stdin rather than the quoted text
                          (round 16, M8)
      pattern_operand  — this binary's grammar consumes its FIRST non-option
                          token as a PATTERN (grep/egrep/fgrep/rg) or a
                          PROGRAM (awk), never a file — so that position is
                          excluded from the needs_file search (round 20,
                          M21): a pattern that happens to spell a real
                          repo-relative path is not evidence the segment
                          reads it, and self_contained() must ask "could an
                          operand occupy this position", not "does this
                          token's text name a file"
    git carries two more, read only by the git-specific branches in
    denied_flag()/parse_pipeline():
      git_subcommands     — the read-only subcommand allow-list (GIT_READONLY)
      git_global_denied   — flags denied only BEFORE the subcommand, where git
                             parses its own global options (GIT_GLOBAL_DENIED)
    """
    __slots__ = ("denied_flags", "benign_exit",
                 "needs_file", "pattern_operand", "git_subcommands",
                 "git_global_denied")

    def __init__(self, denied_flags=frozenset(),
                 benign_exit=frozenset(), needs_file=False,
                 pattern_operand=False, git_subcommands=None,
                 git_global_denied=None):
        self.denied_flags = denied_flags
        self.benign_exit = benign_exit
        self.needs_file = needs_file
        self.pattern_operand = pattern_operand
        self.git_subcommands = git_subcommands
        self.git_global_denied = git_global_denied


_NO_BINARY = Binary()          # the all-defaults record for a plain binary


# SYMLINK-FOLLOWING TRAVERSAL FLAGS, denied per binary. Locked by
# SymlinkTraversalTests, whose lowercase/plain siblings are the positive
# controls.
#
# The rule, which the tests cannot state: `escaping_operand` refuses a
# symlink supplied DIRECTLY but does not model TRANSITIVE traversal — an
# in-repo DIRECTORY operand realpaths inside the root and passes, and a
# recursing binary then walks through a symlink inside it to a file outside.
# On `pull_request` the checkout is the PR head and `actions/checkout`
# preserves symlinks, so that arrangement is entirely attacker-controlled.
# The non-following spellings are the ONLY forms the live corpus uses
# (`grep -r`, `grep -rn`, plain `find`), so denying the following variants
# costs this tree nothing — measured, not assumed.
#
# `diff` earns its `-r` denial for a different reason from the rest, worth
# stating because it is not symmetrical with them: GNU diff has no
# non-dereferencing default to fall back to. `-r` recursion DEREFERENCES
# symlinks unless `--no-dereference` is passed, so unlike grep and find there
# is no safe spelling to keep — the whole flag goes.
#
# THE FLAG DENIAL IS NOT THE WHOLE FIX. It rests on each binary's documented
# default (grep `-r` follows only command-line symlinks; find without `-L` is
# `-P`; rg follows nothing without `--follow`), which is exactly the
# "enumerate what you happen to know" shape this file's SAFETY section calls
# its recurring root error. `escaping_symlink_under()` below is the
# structural half: no operand DIRECTORY may contain an escaping symlink at
# all, whatever flags were passed and whatever the binary would have done
# with them.
_FOLLOWING_GREP = frozenset({"-R", "--dereference-recursive"})

BINARIES = {
    # pattern_operand=True on the whole grep family (round 20, M21): the
    # first non-option token is the PATTERN, not a file, so a search regex
    # that happens to spell a real repo path (`grep -c 'CLAUDE.md'`) must not
    # be read as evidence this segment names a file to read.
    "grep": Binary(denied_flags=_FOLLOWING_GREP,
                   benign_exit=frozenset({1}), needs_file=True,
                   pattern_operand=True),
    "egrep": Binary(denied_flags=_FOLLOWING_GREP,
                    benign_exit=frozenset({1}), needs_file=True,
                    pattern_operand=True),
    "fgrep": Binary(denied_flags=_FOLLOWING_GREP,
                    benign_exit=frozenset({1}), needs_file=True,
                    pattern_operand=True),
    # `--pre`/`--pre-glob`/`--hostname-bin` hand ripgrep a program to run;
    # `--generate` is a distinct info-dump hatch. Bare cores only — see the
    # `denied_flags` note in the Binary docstring for why no `=value` entry
    # is needed anywhere in this table.
    # `-L`/`--follow` is rg's own symlink-following switch;
    # `-R`/`--dereference-recursive` are carried too, spelling-for-spelling
    # with the grep family, because rg does not define them and a claim
    # quoting one is an error either way — refusing it is free and keeps the
    # family's denial set readable as one rule rather than three.
    "rg": Binary(
        denied_flags=frozenset({"--pre", "--pre-glob", "--hostname-bin",
                                 "--generate", "-L", "--follow"})
                    | _FOLLOWING_GREP,
        benign_exit=frozenset({1}), needs_file=True, pattern_operand=True),
    "ls": Binary(),
    # `-L`, `-H` and the old `-follow` synonym make find DEREFERENCE symlinks
    # (round 22, H20); `-files0-from` reads the paths to walk out of a FILE'S
    # CONTENTS, which is a path reaching the child by no dash-token route at
    # all — see escaping_symlink_under() and the sweep note beside it.
    "find": Binary(denied_flags=frozenset({
        "-exec", "-execdir", "-ok", "-okdir", "-delete", "-fprint",
        "-fprint0", "-fprintf", "-fls", "-L", "-H", "-follow",
        "-files0-from"})),
    # `--files0-from=F` was the ONE member of that family that was LIVE
    # rather than latent, and the reason generalises: `self_contained` only
    # ever inspects the FIRST pipeline segment, so a second-position `wc`
    # never meets the needs-a-real-operand rule. The path never appears in
    # argv at all — it is a byte string inside another file, so neither the
    # suffix rule nor confinement can see it. Denied by name, as for `sort`
    # and `find`. Locked by Files0FromTests, including the non-first-segment
    # case.
    "wc": Binary(denied_flags=frozenset({"--files0-from"}), needs_file=True),
    "cat": Binary(needs_file=True),
    "head": Binary(denied_flags=frozenset({"-f", "--follow"}), needs_file=True),
    "tail": Binary(denied_flags=frozenset({"-f", "-F", "--follow"}),
                    needs_file=True),
    # `--compress-program` runs an arbitrary program whenever a sort spills
    # to disk, which `-S 1` forces. `--files0-from=F` makes sort read the
    # list of files to sort out of F's CONTENTS — a path reaching the child
    # by no dash-token route, so the suffix rule cannot see it and
    # confinement never tests it; `needs_file` declines it in first position
    # only, and `self_contained` inspects only the FIRST segment. Denied by
    # name, the same class as find's and wc's.
    "sort": Binary(
        denied_flags=frozenset({"-o", "--output", "--compress-program",
                                 "--files0-from"}),
        needs_file=True),
    # Denied flags empty by design — `uniq` is guarded by OPERAND COUNT
    # instead, in denied_flag()'s own uniq branch: `uniq IN OUT` writes OUT,
    # and no flag names that hatch.
    "uniq": Binary(needs_file=True),
    # `-l`/`--load`, `-i`/`--include` and `-E`/`--exec` are gawk's extension
    # and source-loading flags — the same class as `-f`. The program-text
    # allow-list (AWK_ALLOWED_CALLS, AWK_ESCAPES) is a SEPARATE mechanism
    # below, for the same reason `sed` has no Binary entry at all: the escape
    # lives in the SCRIPT, a language, not a flag, so no flag table —
    # consolidated or not — can describe it.
    # pattern_operand=True: the first non-option token is the PROGRAM text,
    # not a file — same reasoning as the grep family above.
    "awk": Binary(
        denied_flags=frozenset({"-f", "--file", "--source", "--exec", "-l",
                                 "--load", "-i", "--include", "-E"}),
        needs_file=True, pattern_operand=True),
    "cut": Binary(needs_file=True),
    "tr": Binary(needs_file=True),
    # `git` is split in two: GLOBAL flags (before the subcommand, where git
    # parses its own options) vs. the subcommand allow-list. `-O`/
    # `--open-files-in-pager` hands the match list to a command; `--output` (a
    # `log`/`diff`/`show` option) WRITES a file — `git diff --output=WROTE
    # HEAD` created it here under a printed PASS before this was denied.
    # `branch`/`tag` are NOT read-only subcommands (round 15, H4): `-D`/`-d`
    # DESTROY a ref, proven deleting one in a fixture repo under a printed
    # PASS; the argument-less read forms are not worth that surface and
    # nothing in this corpus quotes one, so both are gone entirely — a future
    # claim quoting them is DECLINED AND NAMED, the safe direction.
    # `--exec-path`/`--upload-pack` are denied HERE as well as in
    # git_global_denied, deliberately: the global set refuses them only
    # BEFORE the subcommand, while the exact-core check refuses them at any
    # argv position. Over-refusal is the stated safe direction.
    "git": Binary(
        denied_flags=frozenset({"-O", "--open-files-in-pager", "--output",
                                 "--exec-path", "--upload-pack"}),
        benign_exit=frozenset({1}),
        git_subcommands=frozenset({
            "log", "grep", "show", "ls-files", "diff", "rev-parse",
            "rev-list", "cat-file", "describe", "status", "blame"}),
        git_global_denied=frozenset({
            "-c", "-C", "--exec-path", "--upload-pack"})),
    "basename": Binary(),
    "dirname": Binary(),
    "echo": Binary(),
    "printf": Binary(),
    "stat": Binary(),
    # `-r`/`--recursive`: GNU diff DEREFERENCES symlinks it meets while
    # recursing unless `--no-dereference` is given, so — unlike grep and
    # find, which have a safe default spelling to keep — there is no
    # non-following form of `diff -r` to preserve. The live corpus quotes no
    # `diff -r` at all (its only diff is `git diff --stat`), so the denial
    # costs nothing.
    "diff": Binary(denied_flags=frozenset({"-r", "--recursive"}),
                   benign_exit=frozenset({1})),
}

# ALLOWED_CMDS is DERIVED from BINARIES — a binary is allow-listed exactly
# when it has a Binary record, never the other way around.
ALLOWED_CMDS = frozenset(BINARIES)
GIT_READONLY = BINARIES["git"].git_subcommands
GIT_GLOBAL_DENIED = BINARIES["git"].git_global_denied

# Shell metacharacters that make a string more than a simple pipeline. `\x00`
# is here for a different reason from the rest (round 15, M2): subprocess
# raises ValueError on a NUL in argv, which is not a SubprocessError, so an
# embedded NUL in a backticked span aborted the ENTIRE scan with a traceback —
# every later claim and the whole dangling-identifier check silently never ran.
# Refused up front here; ValueError is also caught in run_pipeline as a
# backstop, because a crash is the one outcome that hides defects wholesale.
FORBIDDEN = re.compile(r"[;&><`\n\x00]|\$\(|\|\|")

# ---------------------------------------------------------------------------
# Allow-listing argv[0] is NOT sufficient — the header used to claim it was
# ("read-only by construction — no writing command is on the list") and `sed
# -i` was demonstrated rewriting a file in the working tree while this tool
# printed PASS. Locked by HatchRegressionTests.
#
# Two mechanisms, because the hatches are of two kinds:
#   (1) FLAG hatches — refusable exactly, by name. Listed per binary above, in
#       BINARIES.
#   (2) SCRIPT hatches — the argument IS a language, so no flag list can
#       contain them. `sed` (`w file`, `s///w file`) is therefore DROPPED from
#       the allow-list entirely; it has no use anywhere in the corpus, and a
#       future sed claim is DECLINED AND NAMED, which is the safe direction.
#       `python3` is DROPPED for the same reason: its argument is a language
#       too, and "the script must be an in-repo .py file" is not a restriction
#       at all when the checkout under test is a pull request's own head — the
#       PR simply adds the .py file.
#
#       `awk` IS A LANGUAGE TOO AND IS NONETHELESS KEPT. THIS IS THE CANONICAL
#       SITE FOR WHY, and rule 1 of the SAFETY section cites here rather than
#       repeating it. The consequence of keeping it is real: `awk 'BEGIN{print
#       "touch X" | "sh"}'` executes a shell command using NEITHER of the two
#       words this file originally blacklisted, and FORBIDDEN does not reject
#       a single `|` because that is the pipeline separator. It is kept
#       because SOME real, currently-passing claim on this tree pipes through
#       it, so dropping it costs measured coverage — and its program is
#       ALLOW-listed instead of escape-blacklisted (AWK_ALLOWED_CALLS below),
#       which is the shape of rule that survives a hatch nobody has named.
#
#       THE ARGUMENT IS QUALITATIVE ON PURPOSE, NOT A COVERAGE FRACTION. The
#       "2 of the 3 claims this tool executes are awk" figure this paragraph
#       used to carry was wrong by the very re-measurement it invited, and any
#       replacement goes stale the next time the corpus changes. Re-deriving
#       the fraction is exactly the `python3 tools/doc-claim-check.py --repo .`
#       invocation MIN_EXECUTED_CLAIMS already documents doing — do not copy a
#       number out of this comment without re-running it.
#
# How a flag is MATCHED is generalised separately: see _option_cores(). A
# deny-list keyed on the exact spelling an option happened to be written in
# misses the same option carrying an attached value (`sort -oFILE`,
# `git grep -O./p.sh`), which is not a new hatch but the same one respelled.
#
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
#   * `ENVIRON` / `PROCINFO` anywhere in an awk token (round 18) — gawk's
#     special ARRAYS. They are variables, not calls, so the allow-list below
#     is the wrong shape of rule for them exactly as a call-name blacklist was
#     the wrong shape for `print x | "sh"`; see denied_flag()'s awk branch.
#   * `ARGV` / `ARGC` anywhere in an awk token (round 22, H19) — the SAME
#     shape one dimension over, and the one the "every suffix of a dash-token
#     is tested" rule explicitly never claimed. See AWK_SPECIAL_ARRAYS below.
AWK_CALL = re.compile(r"([A-Za-z_][A-Za-z0-9_]*)\s*\(")

# awk's SPECIAL ARRAYS, each with the reason it is refused. Locked by
# AwkArgvArgcTests and AwkEnvironmentAndLoadHatchTests.
#
# WHY A NAME REFUSAL IS THE RIGHT SHAPE HERE, which is the part no test
# states: every other awk rule in this file asks what the program CALLS or
# which CHARACTERS it contains, and a special array is neither — it is a
# VARIABLE, so the call allow-list structurally cannot see it, and it
# contains nothing FORBIDDEN. `ARGV[ARGC++]="/etc/passwd"` assigns a path
# into awk's own operand vector, so awk opens that file as ordinary main
# input; path confinement is blind to it because the path is a string
# LITERAL inside the program token rather than an operand, and
# `self_contained` is satisfied by the legitimate operand beside it.
# `SYMTAB`/`FUNCTAB` are refused with them because a `SYMTAB["ARGV"]` write
# reaches the same vector indirectly, and refusing the direct spelling alone
# would be the enumerate-the-spelling error the SAFETY section calls this
# file's recurring root cause.
#
# Over-refusal is the stated safe direction and costs nothing measurable: the
# one live awk claim on this tree (`... | awk '{s+=$1} END{print s}'`) names
# none of them, and a document that legitimately wants NR or NF is untouched.
AWK_SPECIAL_ARRAYS = (
    (re.compile(r"\b(?:ARGV|ARGC)\b"),
     "which is awk's own OPERAND VECTOR — assigning a path into `ARGV` (or "
     "raising `ARGC`) makes awk OPEN that file as ordinary main input, with "
     "no pipe, no getline and no disallowed call, and path confinement "
     "cannot see it because the path is a string LITERAL inside the program "
     "token rather than an operand"),
    (re.compile(r"\b(?:ENVIRON|PROCINFO)\b"),
     "which exposes the runner's environment and process state"),
    (re.compile(r"\b(?:SYMTAB|FUNCTAB)\b"),
     "which is gawk's symbol table — it reaches every global variable "
     "indirectly, `ARGV` among them, so refusing only the direct spelling "
     "would be an enumeration of spellings again"),
)
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
#
# THIS LIST IS NOT THE GATE, only a supplement to one, and it must never
# become the gate again. It could not be one: a runnable claim whose binary
# was on neither curated list (ALLOWED_CMDS or this) was dropped with no
# bucket, no count and no line — `check_claim` returned ("ignored", None),
# and `unrecognised_spans` could not recover it either, because the span WAS
# bound so its start sat in `bound`. `comm`, `tac`, `pcregrep`, `tree`, `nl`
# and `du` claims were therefore invisible on BOTH routes at once, which made
# two sentences of this file's own header false. The blind spot WAS the
# hand-written list — the shape the SAFETY section names as this file's
# recurring root error, arriving on the census side.
#
# The list stays because it can only ADD recognition: it names heads whose
# ARGUMENTS carry no command-shaped evidence (`npm install`, `make`) and which
# the positive test below would otherwise refuse. Removing a name from it can
# lose a decline; adding one can never cost anything.
_KNOWN_BINARIES = frozenset((
    "dotnet", "curl", "wget", "ps", "bash", "sh", "zsh", "make", "npm", "npx",
    "node", "python", "python3", "pip", "docker", "jq", "tee", "xargs", "sed",
    "unity", "dos2unix", "pwsh", "powershell",
))

# The POSITIVE test (round 19, H15), replacing "is this head on a list I wrote"
# with "does this span carry the evidence a command carries". Three parts, and
# each was chosen against the live corpus rather than from taste:
#   * a BINARY-SHAPED head — all lower case, the shape every real binary name
#     on any of these lists has, and the shape a C# identifier (`PascalCase`,
#     `UPPER_SNAKE`) never has;
#   * at least one ARGUMENT that only a command takes — an option token, a
#     filename with an extension, or a path with a directory separator whose
#     last segment carries an extension or is empty (`src/`);
#   * measured noise. Head-shape alone took the unrecognised-shape census from
#     132 to 1297 on this tree, drowning every real entry in prose whose first
#     word happens to be lower case (`public readonly byte[]`, `is available
#     to`, `var = μ(1+αμ)`) — a census nobody reads is the silent skip wearing
#     a bucket label. With the argument test it is 132 -> 140, and nothing the
#     old predicate named is lost.
# A NEGATIVE NUMBER is excluded from the option test explicitly: `-0.7` and
# `-1f` are how this corpus writes constants, not how anything writes a flag.
_BINARY_HEAD = re.compile(r"[a-z][a-z0-9_.+-]*\Z")
_OPTION_TOKEN = re.compile(r"--?[A-Za-z0-9]")
_NEGATIVE_NUMBER = re.compile(r"-\d+(?:\.\d+)?[A-Za-z]?\Z")
_FILENAME_TOKEN = re.compile(r"[A-Za-z0-9_*?+-][A-Za-z0-9_*?.+-]*"
                             r"\.[A-Za-z][A-Za-z0-9]{0,7}\Z")
_PATH_TOKEN = re.compile(r"[A-Za-z0-9_*?.+-]+(?:/[A-Za-z0-9_*?.+-]*)+\Z")


def _command_operand(tok):
    """True when `tok` is an argument only a COMMAND takes — an option, a
    filename with an extension, or a real path. `L/2`, `base/compactness` and
    `IEventA/IEventB` are paths by punctuation and by nothing else, so a path
    must end in a directory separator or carry an extension in its last
    segment."""
    if _OPTION_TOKEN.match(tok) and not _NEGATIVE_NUMBER.match(tok):
        return True
    if _FILENAME_TOKEN.match(tok):
        return True
    if _PATH_TOKEN.match(tok):
        return tok.endswith("/") or "." in tok.rsplit("/", 1)[1]
    return False


def command_shaped(cmd, head):
    """True when this backticked span reads as a shell command rather than an
    identifier — the discriminator that lets an unlisted BINARY be named
    without also naming every version-bump arrow in the corpus.

    Round 19 (H15): the head test is a SHAPE plus argument evidence, not a
    membership test against a hand-written list. See the two comment blocks
    above for the measurement that set the argument rule."""
    cmd = cmd.strip()
    if " " not in cmd:
        return False
    if not _HEAD_SHAPE.match(head):
        return False
    if ("/" in head or head.endswith((".py", ".sh"))
            or head in _KNOWN_BINARIES):
        return True
    if not _BINARY_HEAD.match(head):
        return False
    return any(_command_operand(tok) for tok in cmd.split()[1:])


TIMEOUT_S = 60

# A NEGATED claim states what the command does NOT return — this repo writes
# "the plain `grep …` no longer returns 218" precisely to record a superseded
# figure. Reading that as an assertion inverts the document's meaning and
# reports a defect where the author already did the right thing. Found by this
# tool's own first run against the live tree, which is the complement-testing
# lesson rounds 5-8 kept re-learning: a matcher tuned on the instances that
# motivated it will misread their opposite.
#
# THE ALTERNATION HOLDS ONLY PHRASES THAT CAN NEGATE NOTHING BUT THE CLAIM.
# An excusal is the one outcome that costs coverage silently — the claim is
# never run at all — so a bare `not`, `was`, `never` or `n't`, which saturate
# this repo's prose and can negate anything in a sentence, excused correct
# claims that were wrong by a factor of 33 while the run still printed PASS.
# `no longer returns` cannot negate anything else. Every verb-bound form is
# spelled out rather than left to a bare adverb, so widening this list is a
# deliberate act. Locked by
# NegatorPrecisionTests.test_a_bare_negation_word_near_a_wrong_claim_does_not_excuse_it.
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
# This is not hypothetical and it is not someone else's mistake: a pass
# writing the CHANGELOG entry once quoted Spec #20's own drift-prone example
# verbatim, with its value, INTO the chain — which would have failed this gate
# on a correct historical entry the day the 36th assembly landed. It was
# caught by hand, but "remember not to write that" is not a mechanism.
#
# The model is `doc-consistency-check.py`'s, ported rather than reinvented, and
# kept to its two load-bearing properties:
#   * a mismatch inside a record region is EXCUSED, not skipped — the claim is
#     still executed, and the excusal is COUNTED AND PRINTED, so "this
#     historical figure no longer reproduces" stays visible without gating;
#   * an explicit CURRENCY ASSERTION pierces the excusal. A record that says
#     the command returns N *now* is making a present-tense claim wherever it
#     sits, and is reported like any other.
# Regions come from that module (frozen header chain, VERSION HISTORY
# sections, log body, archive) so the two tools cannot disagree about which
# bytes are frozen. Round 19 (H13): the VH half of that list was missing here
# for six rounds while the sibling excused it, and the spans are now DERIVED
# from the sibling's own blanking rather than re-listed — see
# dated_record_regions() and _blanked_runs() below.
CURRENCY_ASSERTION = re.compile(
    r"\b(?:now|currently|today|at\s+HEAD|as\s+of\s+(?:today|HEAD))\b", re.I)
CURRENCY_RADIUS = 120


def _blanked_runs(text, blanked):
    """The maximal spans in which `blanked` differs from `text`.

    Round 19 (H13). `blank_frozen_history` is OFFSET-PRESERVING by contract
    (a space per non-newline character, a newline per newline), so the set of
    bytes it froze is recoverable exactly: they are the positions where the
    two strings disagree. Deriving the spans that way is what makes the
    import mean what its docstring says it means — the sibling and this file
    cannot disagree about which bytes are frozen, because there is only one
    computation and this reads its result rather than re-listing a subset of
    its inputs.

    Compared LINE BY LINE, not character by character: a whole-corpus
    character diff is ~8 MB of Python-level comparisons per run, while a line
    compare is one C-level `!=` for the ~99% of lines that are untouched.
    Consecutive differing lines are one run, and two runs separated by
    nothing but whitespace are merged — a blank line inside a Version History
    section is blank-invariant (a newline maps to a newline) and must not cut
    the section into pieces."""
    runs, pos, start, last_end = [], 0, None, 0
    for line in text.splitlines(keepends=True):
        n = len(line)
        seg = blanked[pos:pos + n]
        if seg != line:
            diff = [i for i in range(n) if seg[i] != line[i]]
            if start is None:
                start = pos + diff[0]
            last_end = pos + diff[-1] + 1
        elif start is not None:
            runs.append((start, last_end))
            start = None
        pos += n
    if start is not None:
        runs.append((start, last_end))
    merged = []
    for span in runs:
        if merged and not text[merged[-1][1]:span[0]].strip():
            merged[-1] = (merged[-1][0], span[1])
        else:
            merged.append(span)
    return merged


def dated_record_regions(rel, text):
    """Spans of `text` that are dated records by structure.

    `record_regions` is called on the same frozen-history-BLANKED text
    `doc-consistency-check.py`'s own scans read, honouring a precondition its
    docstring states ("so the offsets agree") that this caller used to
    violate by passing RAW text — harmless only because
    `blank_frozen_history` happens to be offset-preserving, which is a
    property of that function's IMPLEMENTATION and not a contract this file
    was entitled to assume.

    THE SPANS ARE DERIVED FROM THE BLANKING, NOT RE-LISTED, and that is the
    load-bearing decision. `blank_frozen_history` freezes TWO things: the
    header chain below its head entry AND every `Version History` section.
    While this function threw the blanking away and re-listed a separate
    `frozen_chain_span`, the sibling excused a mismatch in a VH row and this
    tool GATED on it — the two tools disagreeing about which bytes are
    frozen, in the one file that imports the other specifically so they
    cannot. That was not a corner: Version History sections are a
    substantial slice of the scanned corpus and most of what this tool
    executed sat inside one. (No percentage is quoted, deliberately — the
    figure that stood here separated VH from the header chain, which the
    imported blanking does not expose separately, so it was not
    re-derivable.) Locked by
    VersionHistoryExcusalTests and FrozenSpanBoundaryTests.

    The currency pierce is unchanged: a VH row that says the command returns
    N *now* is a present-tense claim wherever it sits, and `currency_asserted`
    still reports it.

    WHAT IS STILL NOT SHARED, stated honestly because the import docstring
    above claims the two tools "cannot disagree" and that is not quite true.
    `blank_frozen_history` returns a THIRD value,
    `pierced`: citations inside the header chain that carry a maintained
    "since advanced to **vC**" currency pointer, which the sibling's own
    `scan_version_citations` deliberately UN-FREEZES and evaluates directly
    — a currency reassertion means that specific citation is being actively
    maintained, not archived. This function discards that third value
    (`_pierced` above) and blanks the WHOLE header-chain span regardless, so
    a single-integer claim sitting inside a pierced citation's own excerpt
    would be treated as a frozen dated record here even though the sibling
    treats the citation it is attached to as live. Only the SPAN is shared;
    the PIERCE POLICY is not. Reconstructing the pierced sub-spans exactly
    would mean re-deriving `blank_frozen_history`'s own internal excerpt
    bounds (the 40-character padding around each match), which is not part
    of its public contract — attempted and rejected here as more likely to
    silently mis-locate a span than to close this gap correctly.
    MEASURED 2026-08-22 (`DCC.blank_frozen_history` run over every SURFACES
    file, the pierced list summed): 21 pierced citations exist today
    (README.md 1, file-manifest.md 11, CHANGELOG.md 9), and NONE sits within
    300 characters of any claim this file's own shapes currently bind — so
    the disagreement is real but latent, not a live miscount. Re-run that
    measurement before trusting this note's "latent" half; re-deriving
    whether it is still latent needs both, not just the pierced count."""
    blanked, _frozen, _pierced = DCC.blank_frozen_history(text)
    spans = list(DCC.record_regions(rel, blanked))
    spans.extend(_blanked_runs(text, blanked))
    return tuple(spans)


def currency_asserted(text, start, end):
    """True when the claim reasserts that the value is current — bounded to its
    own line so a marker from a neighbouring sentence cannot pierce for it."""
    lo = max(text.rfind("\n", 0, start) + 1, start - CURRENCY_RADIUS)
    nl = text.find("\n", end)
    hi = min(nl if nl != -1 else len(text), end + CURRENCY_RADIUS)
    return bool(CURRENCY_ASSERTION.search(text[lo:hi]))


# ---------------------------------------------------------------------------
# Round 16 (M12). currency_asserted()'s proximity window is right for the
# CHANGELOG-style append-only chain the header above reasons about — a record
# is frozen by POSITION alone there, so a nearby "now"/"currently" is the only
# thing that can plausibly mean "no, THIS one is still true". The ERR log is a
# different kind of dated record: measured region coverage is 99.7% of
# spec-error-log.md and 99.2% of CHANGELOG.md, and inside that span a mismatch
# is excused unless a currency word happens to sit within 120 characters on
# the same line — which most sentences in a 213-entry remediation backlog
# simply do not carry, whether or not the claim they hold is current. Proven:
# a present-tense acceptance criterion inside a still-Open ERR entry was
# excused by the proximity rule alone. Every ERR entry already NAMES its own
# resolution state, so that is the thing to ask instead — excuse a mismatch
# there only when the enclosing entry is marked resolved, or the claim's own
# sentence carries a date (a claim that names when it was measured is a
# record of that measurement, resolved or not). Scoped to LOG_BODY_FILES only
# — every other dated-record region (the frozen header chain, the resolved
# archive) keeps the proximity rule above, unchanged.
# ---------------------------------------------------------------------------
ERR_RESOLVED_MARKER = re.compile(r"✅[^\n]{0,24}?(?:RESOLVED|CLOSED)", re.I)
ERR_HEADING = re.compile(r"^## .*$", re.M)
# `re.M` is load-bearing, not decoration. This pattern is used as
# `ERR_TABLE_ROW.match(text, ls)` with `ls` the start of a LINE, and without
# MULTILINE a bare `^` matches only at the real start of the string — never at
# a non-zero `pos`, whatever precedes it. Without it the table-row branch
# below could not fire for any row except one at byte 0, every index row fell
# through to the prose-section branch, and the whole `## Error Index` table
# was bounded as ONE entry — precisely what _err_log_entry_span's own
# docstring says must not happen. Locked by
# ExcusalTests.test_an_err_index_table_row_is_bounded_to_its_own_line.
ERR_TABLE_ROW = re.compile(r"^\|\s*ERR-", re.M)


def _err_log_entry_span(text, pos):
    """(start, end) bounds of the spec-error-log.md entry enclosing `pos`.

    Two shapes coexist: a row of the big `## Error Index` table (one line,
    `| ERR-... | ... |`) and a `## ERR-NNN-NNN: ...` prose section below it.
    A table row is bounded to its own line — the whole table sits under ONE
    heading, so treating the table as a single entry would let a ✅ anywhere
    in its 213 rows resolve every one of them. A prose section is bounded by
    the nearest preceding `## ` heading and the next one."""
    ls = text.rfind("\n", 0, pos) + 1
    le = text.find("\n", pos)
    le = len(text) if le == -1 else le
    if ERR_TABLE_ROW.match(text, ls):
        return ls, le
    start, end = None, len(text)
    for m in ERR_HEADING.finditer(text):
        if m.start() <= pos:
            start = m.start()
        else:
            end = m.start()
            break
    if start is None:
        return ls, le
    return start, end


def err_log_excused(text, start, end):
    """True when a mismatch at text[start:end] is excused under the ERR
    log's OWN rule: the enclosing entry is marked resolved, or the claim's
    own sentence carries a date. See the section banner above.

    THE SENTENCE WINDOW IS CLIPPED before the date search runs, and the
    reason is the one `doc-consistency-check.py` states at its own
    `MARKER_RADIUS`: this repo's "sentences" are not sentences (measured on
    the live ERR log, window lengths median 249, p90 602, max 1126), so at
    full window length an annotation about something else entirely silently
    suppresses a real stale claim several hundred characters away. The
    model's own justification is "the claim's OWN sentence carries a date",
    and 689 characters away — the worst case measured — is not that."""
    a, b = _err_log_entry_span(text, start)
    if ERR_RESOLVED_MARKER.search(text[a:b]):
        return True
    window = DCC.sentence_window(text, start, end)
    # RECORDED AS A KNOWING DUPLICATE, NOT SILENTLY LEFT AS ONE. The five
    # lines below are the SAME clipping algorithm as
    # `doc-consistency-check.py`'s `historically_marked()`, statement for
    # statement, with two substitutions: that file's `MARKER_RADIUS` (110)
    # for `CURRENCY_RADIUS` (120), and this caller's own regex search
    # instead of `HISTORICAL_MARKERS`. It cannot simply be called: that
    # function fuses window, clip and its own historical-marker search into
    # one return value, so there is no narrower surface to call THROUGH, and
    # reusing it as written would ask it the wrong question. This file's own
    # import docstring calls a second copy of a definition "the
    # duplicate-claim defect this repo keeps filing", which is exactly what
    # this is. The 110-vs-120 divergence is UNEXPLAINED on both sides and is
    # flagged rather than silently harmonised, since changing either number
    # is a calibration decision with no measurement behind it. The REAL fix
    # is a shared clipping helper the sibling exports and both files call.
    ls = text.rfind("\n", 0, start) + 1
    sent_start = text.find(window, ls) if window else start
    if sent_start == -1:
        sent_start = ls
    lo = max(sent_start, start - CURRENCY_RADIUS)
    hi = min(sent_start + len(window), end + CURRENCY_RADIUS)
    return bool(ERR_LOG_DATE.search(text[lo:hi]))


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
SURFACE_GLOBS = ("docs/specs/*/section-*.md", "docs/specs/*/appendices.md")
# Round 16 (M6): a THIRD glob, "docs/specs/*/section-9-approval-checklist.md",
# used to sit here — but that file's name already matches the first glob
# ("section-" + anything + ".md"), so every approval checklist was scanned
# TWICE and every finding on one printed twice at the same path:line. Measured
# on this tree: 649 glob hits, 596 unique, the 53 duplicates being every
# approval checklist in the corpus — the tool's own headline motivating case
# double-counted. Deleted rather than kept-and-deduplicated, because a glob
# that names no file the others do not already match is dead weight even once
# de-duplication (below) makes it harmless.

# ---------------------------------------------------------------------------
# COVERAGE FLOOR (round 16, H9).
#
# A run that checked NOTHING used to print PASS and exit 0. MISSING SURFACE was
# a print statement: it incremented no counter and gated nothing, proven with 8
# of the 9 named surfaces deleted — 1 claim executed, PASS, exit 0. The globbed
# half had no missing concept at all, and every executed claim on this tree
# comes from ONE globbed folder, so renaming that folder would have taken the
# executed count to zero with not one line of output changed. Locked by
# ExitCodeTests (the exit-2 cases for a missing named surface, a glob matching
# no file, and no claim executed at all).
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
# THE FLOOR IS ON DISTINCT COMMANDS, NOT INSTANCES, and that is the rule worth
# keeping: an instance floor cannot tell "N drift-capable claims" from "one
# command quoted N times", and most of this file's own executed instances were
# exactly that — one command quoted in three files, one in two, one in one,
# with the three-instance one pinned to an immutable git revision so it can
# never drift regardless of what any document says. `checked_commands` in
# scan() is what the floor reads; the instance count is still printed beside
# it, so the gap between "commands" and "quotes of a command" stays visible
# either way. (Whether to exclude revision-pinned commands from even the
# distinct count is a separate, owner-level call this file does not make.)
#
# The SLACK is re-derived with the base rather than carried: at "roughly half
# the measured base", the same headroom rule CHECK 2's own floors
# (MIN_TYPED_FENCE_FILE_SHARE, MIN_EXAMINED_SHARE) use. A slack sized for a
# population six times bigger would floor this one at 1.
#
# WHEN IT LEGITIMATELY CHANGES: coverage GROWING is the normal case (a new
# shape, a new allow-listed binary, a new claim written) — re-derive and raise
# the floor in the same commit, so the new coverage is protected too. Coverage
# SHRINKING is the case that must not be waved through: lower this number only
# with the reason stated in the commit message that lowers it, because
# lowering it silently is how the vacuous-pass failure class comes back.
MIN_EXECUTED_SLACK = 1
# Re-derived 2026-08-22 by the invocation above: 3 DISTINCT commands executed
# (7 instances of them).
MIN_EXECUTED_CLAIMS = 3 - MIN_EXECUTED_SLACK

# ---------------------------------------------------------------------------
# THE LIVE FLOOR (round 19, H13) — the one that states this tool's real reach.
#
# MIN_EXECUTED_CLAIMS counts every claim the tool RAN. Most of what it runs on
# this tree sits inside a dated record whose own pierce fails, so a mismatch
# there is EXCUSED: the command still executes, the divergence is still
# printed, and CI stays green by design. That is worth having — it is how "this
# historical figure no longer reproduces" stays visible — but it is not
# drift-catching coverage, and a floor built on it protects mostly frozen text.
# Until H13 the two were fused, and a headline of "6 executed (floor 4)"
# described a live coverage of ONE.
#
# So the honest figure gets its own floor, re-derived by the same invocation:
# read the "... of which LIVE (can gate)" line. Re-derived 2026-08-22: 3 live
# instances, 2 distinct commands.
#
# THE FLOOR IS DELIBERATELY LEFT AT 1 — DO NOT READ THE MEASUREMENT AS THE
# FLOOR. The argument is MIN_EXECUTED_CLAIMS's own, applied to this counter: a
# floor must not be raised by something that can be inflated without adding
# reach. Of the three live instances, one is a second QUOTE of a command
# already live elsewhere (repetition, exactly what the executed floor came off
# instances to stop counting) and one is REVISION-PINNED (`9b841d1^`), so it
# can never drift whatever any document says, and its liveness rests on the
# word "today" sitting within 120 characters of it inside a frozen Version
# History row. So the distinct live commands are 2, of which exactly ONE can
# drift — the same single command that has been this tool's whole live reach
# since H13 measured it. Raising the floor to 3, or to 2, would gate CI on
# repetition and on prose adjacency inside a record, not on reach.
#
# THE RESIDUAL, recorded rather than engineered away: a floor of 1 can be met
# by the revision-pinned claim ALONE, so a tree that lost both
# design-supplement sentences would print PASS with no drift-catching coverage
# at all. The honest fix is a floor on DISTINCT DRIFT-CAPABLE live commands
# rather than on live instances, which needs "revision-pinned" to be a property
# this file can compute — the same owner-level call MIN_EXECUTED_CLAIMS defers,
# and it decides both floors. Left open here rather than approximated.
#
# The GATE below compares `len(live_commands)`, not `live` (INSTANCES) —
# matching MIN_EXECUTED_CLAIMS, and closing the REPETITION half of the residual
# above. It does NOT close the revision-pin half. Locked by
# LiveDistinctFloorTests.
#
# NOTE FOR WHOEVER RAISES IT: raising this constant today fails 12 cases in
# tools/tests/test_doc_claim_check.py, because that suite's `scan()` harness
# patches MIN_EXECUTED_CLAIMS and not MIN_LIVE_CLAIMS, so every fixture is
# silently pinned to a live floor of 1.
#
# THE NUMBER IS THE POINT, NOT A PROBLEM TO BE ENGINEERED AWAY. Raise it when
# a NEW drift-capable command is quoted in live text, in the same commit;
# lower it only with the reason stated in the commit message that lowers it.
MIN_LIVE_CLAIMS = 1

# ---------------------------------------------------------------------------
# SEAM 1 — ANSWER KINDS (round 16, H11).
#
# "The command prints a single integer" used to be hard-wired at six separate
# sites inside scan(): the value sub-pattern, the `int(...)` conversion, the
# single_integer() read-back, the `not-single-int` decline bucket, the `!=`
# comparison and the FAIL text. A second answer kind — a pair, a version
# string, "prints nothing" — therefore meant editing all six, which is how a
# floor becomes permanent. It is ONE object now: add a class here and name it
# on a claim shape.
#
# THE BANNER USED TO SAY "...and nothing in scan() changes", and executed
# literally that crashed. Four leaks, all closed and locked by
# AnswerKindRegistrationTests: ANSWER_KINDS is DERIVED from the shapes rather
# than hand-written (a hand-maintained duplicate, in the tool whose thesis is
# that hand-maintained figures drift), the decline table is DERIVED from
# DECLINE_BUCKETS plus each kind's own bucket (decline_bucket_order), an
# unforeseen bucket is counted rather than fatal (record_decline), and the
# stated-value grammar lives on the answer kind with each shape's regex built
# from it — before that, shapes and answers were a CROSS-PRODUCT rather than
# two orthogonal seams.
#
# WHAT IS STILL TRUE RATHER THAN ASPIRATIONAL, stated so this banner stops
# over-promising: registering a kind and naming it on a shape is enough for
# scan() to run, count and print it. What a new kind still touches outside
# this section is the SHAPE regex it is named on — the text AROUND the value
# placeholder is written for a value made of digits, so a radically different
# grammar wants its own shape, not just its own answer. That is a real limit,
# and it is smaller than the one this banner used to hide.
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

# No allow-listed binary this tool runs pads a count with a leading zero or
# groups it with a thousands separator — `wc -l`, `grep -c`, `git grep -c`,
# `ls | wc -l` all print a bare digit run. The trap this guards is Python's
# own `int()`, which NORMALISES what it is handed rather than validating it:
# `int("1,2,3".replace(",", ""))` is 123 (comma position is irrelevant to it)
# and `int("007")` is 7 (a leading zero is silently dropped), so a malformed
# document transcription could "reproduce" a real value by accident of `int()`
# being more permissive than any command's output grammar ever is. A
# WELL-FORMED integer literal, matched in full: either the single digit "0",
# or a nonzero digit run with no leading zero, optionally grouped in EXACT
# thousands (a document may write "12,345" for readability; no live claim's
# command ever emits the comma itself — see single_integer(), which does not
# accept one). Locked by MatcherTests.test_a_malformed_integer_literal_is_declined.
_WELL_FORMED_INT = re.compile(r"\A(?:0|[1-9]\d*|[1-9]\d{0,2}(?:,\d{3})+)\Z")


class SingleIntegerAnswer:
    """The one answer kind this tool has ever had: the command prints exactly
    one integer, and the document states it.

    That floor is deliberate and is stated in the header: it is the class that
    has actually bitten here (counts, tallies, cardinalities) and the class
    where "what the stated value should be" is unambiguous."""

    name = "single-integer"
    bucket = "not-single-int"
    unreadable = "output is not a single integer"
    # The STATED-VALUE grammar belongs to the answer kind, not to each claim
    # shape; each shape's regex is BUILT from this at ClaimShape
    # construction. Deliberately loose (it is the CAPTURE; `parse` below is
    # what validates it), because a malformed transcription must reach a
    # NAMED decline rather than fail to match and vanish.
    value_pattern = r"\d[\d,]*"

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
        raw = text[start:end]
        if not _WELL_FORMED_INT.match(raw):
            # `raw` matched the shape's own loose capture but is not a
            # WELL-FORMED integer literal — a malformed thousands grouping
            # ("1,2,3") or a leading zero ("007"). Declined and named rather
            # than silently normalised by `int()`. See _WELL_FORMED_INT.
            return None, (self.bucket,
                          "stated value %r is not a well-formed integer "
                          "literal — a digit run with no leading zero, "
                          "optionally grouped in exact thousands; no "
                          "allow-listed command's output is shaped any other "
                          "way" % raw)
        return int(raw.replace(",", "")), None

    def read(self, out):
        return single_integer(out)

    def matches(self, stated, got):
        return stated == got

    def describe(self, stated, got):
        return "document says %d; command returns %d" % (stated, got)


SINGLE_INTEGER = SingleIntegerAnswer()
# ANSWER_KINDS is DERIVED from CLAIM_SHAPES below, never written out here —
# round 19 (H18). It was a hand-maintained duplicate referenced at exactly one
# site (a printed count) in the one tool whose entire thesis is that
# hand-maintained figures drift.


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
    """One recognised claim shape.

    `template` is a regex with one `{value}` placeholder, filled from the
    ANSWER KIND's own `value_pattern` — round 19 (H18). Substituted textually
    rather than through `str.format`, because a regex is full of braces
    (`{0,40}`) that `format` would try to read as fields of its own.

    CONTRACT (round 20, L6): the compiled regex MUST expose three named
    groups — `cmd`, `value` and `gap` — asserted at construction rather than
    left implicit. `negation_window()` below reads `group("gap")`
    UNCONDITIONALLY, with no fallback, so a shape added without one would
    die with an uncaught `IndexError` the first time `collect_claims()`
    walked the live tree — a traceback mid-scan, at the exit code this
    file's own header reserves for "a document is wrong" rather than "this
    tool broke". Failing at construction, with a message naming the missing
    group, is cheaper than failing at the first real document that happens
    to trip it."""

    def __init__(self, name, template, answer=SINGLE_INTEGER):
        self.name = name
        self.answer = answer
        self.regex = re.compile(
            template.replace("{value}", answer.value_pattern), re.I)
        missing = [g for g in ("cmd", "value", "gap")
                   if g not in self.regex.groupindex]
        if missing:
            raise AssertionError(
                "ClaimShape %r's regex is missing required named group(s) "
                "%s — every shape's regex must expose cmd, value AND gap "
                "(see this class's own docstring); negation_window() reads "
                "group(\"gap\") unconditionally with no fallback"
                % (name, missing))

    def find(self, text):
        return self.regex.finditer(text)

    def negation_window(self, text, m):
        """The text a NEGATOR may appear in to mark this claim historical.

        ONE implementation shared by every shape, deliberately. When the
        look-back existed on the value-first shape alone, the forward shape
        passed the text AFTER its gap — which can only ever contain the arrow
        and the digits — so a forward-shape claim whose negator sits BEFORE
        the backtick was never recognised as historical, while the
        value-first phrasing of the identical sentence declined correctly.
        Locked by MatcherTests.test_a_negated_claim_declines_in_every_shape.

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
    "August 18, 2026", so counting one would overstate what was given up.

    THE DATE RULES ARE ONE CASE OF A GENERAL RULE, and the general rule is
    the one implemented: A STATED VALUE MUST BEGIN ITS OWN TOKEN. The shapes'
    value pattern happily starts mid-token, so any compound number whose tail
    digits abut a parenthesised command bound as a stated value — section
    numbers, version numbers, spec numbers, Markdown heading numbers, decimal
    fractions, slash-separated ratios, clock times, slash dates, and a bare
    letter prefix (`v2`). Stating the rule as an ENUMERATION of joiners was
    tried twice and failed twice, in both cases because the enumeration was
    the ways THIS corpus continues a token rather than the ways a token CAN
    be continued.

    So the test is: the single character immediately before the value (after
    stripping emphasis markup, which does not join tokens) must be neither
    ALPHANUMERIC nor one of the punctuation joiners in COMPOUND_TAIL. Locked
    by LeadingValueTokenTests and CompoundTokenPrecisionTests.

    The date rules stay as they are, both because they catch a form this one
    does not (the digits of "August 18," DO begin their own token) and
    because their message names what was refused."""

    # A stated value begins its own token. These are the PUNCTUATION ways
    # this corpus continues one — `.` (a section, version, heading or
    # decimal tail), `#` (a spec number), `§` (a section number), `/` (a
    # slash-separated ratio like `461/1/2`, or a slash date), and `:` (a
    # clock time like `16:59:45`). Any ALPHANUMERIC character before the
    # value (a letter, as in `v2`, or a digit, as in an unseparated compound)
    # is refused too — see rejects() below, which is the actual test.
    COMPOUND_TAIL = (".", "#", "§", "/", ":")

    def rejects(self, text, m):
        before = text[max(0, m.start("value") - 32):m.start("value")]
        if DATE_YEAR_BEFORE.search(before):
            return "the integer is the YEAR of a date, not a stated value"
        if DATE_DAY_BEFORE.search(before):
            return "the integer is the DAY of a date, not a stated value"
        # `rstrip` over emphasis only: "**35**" is a stated value whose token
        # begins at the 3, while "§2.2.**2**" is still a section number.
        # The docstring's general rule, not an enumeration of it: refuse
        # when the single character immediately before the value is either
        # alphanumeric (a letter, as in `v2 (\`cmd\`)`, or an unseparated
        # digit) or one of the punctuation joiners in COMPOUND_TAIL.
        tail = before.rstrip("*_")[-1:]
        if tail.isalnum() or tail in self.COMPOUND_TAIL:
            return ("the integer is not the start of its own token — it is "
                    "joined to what precedes it by %r (a section, version, "
                    "spec, heading, decimal, ratio, clock time or date, not "
                    "a stated value)" % tail)
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

# Round 16 (M12). The same month-name alternation, reused for a different
# question: not "is this integer secretly a date" (the two above), but "does
# the claim's own sentence carry one at all" — err_log_excused() near
# currency_asserted() uses this to decide whether an ERR-log mismatch is a
# dated record on its own terms, without a nearby "now"/"currently".
ERR_LOG_DATE = re.compile(_MONTH + r"\s+\d{1,2}(?:st|nd|rd|th)?", re.I)

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

# The only words the verb route's gap may hold (round 23, H21) — adverbs and
# negators. Every one of them modifies the verb without supplying a new
# subject, which is exactly the property that keeps the verb attached to the
# command: "`cmd` now returns 93", "the plain `cmd` no longer returns 99".
# `no`/`not`/`never`/`longer` are here so a NEGATED claim still binds and can
# reach its `negated-or-historical` decline — refusing to bind it would move
# it from a named decline to the census, losing the negation signal the
# lookback window exists to record.
_GAP_ADVERB = (r"no|not|never|longer|now|still|again|already|always|"
               r"currently|today|then|once|only|thus")
# Whitespace and closing/emphasis markup, then up to three adverbs. Lazy, so
# the gap stops at the first verb rather than reaching for a later one.
_VERB_GAP = (r"[\s)\]}*_'\"—–-]{0,24}?"
             r"(?:\b(?:" + _GAP_ADVERB + r")\b[\s,]{1,4}){0,3}")


# The run between the arrow-or-verb and the stated value: whitespace and
# markup only, no letters. The rule, which is what the eighteen-character
# any-text run it replaced could not express: a value the command ANSWERS
# follows its verb across markup and punctuation, never across words —
# "returned **218**", "→ 18", "shows: 4". A run carrying words (" the exact
# Stage-", " of FR-", " at step ") is prose that names a number, not a command
# reporting one. Locked by
# VerbGapPrecisionTests.test_a_subordinate_clause_before_the_verb_does_not_bind,
# which is exactly the case this half closes and the gap rule above does not
# ("`cmd` returns, for the 2 tracking files, a count of 3" has an EMPTY verb
# gap).
_POST_VERB = r"[^0-9A-Za-z`\n]{0,18}"


# SHAPE 1 — command, then the stated value: "`cmd` → 18", "`cmd` returned 218".
#
# ITS TWO ROUTES DO NOT SHARE ONE GAP GRAMMAR, and the asymmetry is the whole
# rule: an ARROW cannot belong to a neighbouring clause ("→ 18" after a
# command is that command's answer and nothing else, whatever the 40
# characters before it say), while a VERB can — `found`/`counted`/`shows`/
# `returns` are among the commonest verbs in this repo's prose, so any
# sentence that mentioned a command and later reported SOME number matched.
#
# So the verb route's gap may hold only whitespace, CLOSING or EMPHASIS markup
# (") ", " **", " — "), and ADVERBS OR NEGATORS. The rule is not "punctuation
# only" and the difference is load-bearing: an adverb cannot introduce a new
# SUBJECT, so "`cmd` now returns 93" and "the plain `cmd` no longer returns 99"
# are still the command's own clause, while "was re-run and", "is the gate;
# the sweep" and any other noun or conjunction hand the verb to somebody else.
# (Both admitted forms are live in this file's own test suite — the currency
# pierce and the negation-window case — and a rule that refused them would
# have been wrong about English, not merely strict.) A `.` `;` or `:` ends the
# clause outright and is refused whatever follows.
#
# NOT ONE OF THE CLAIMS THIS TOOL EXECUTES USES A NON-EMPTY VERB GAP, so the
# measured cost of the restriction is nil; and a refused match is re-counted
# in the census and decline buckets rather than dropped, so a phrasing this
# rule has not thought of lands in the census under its own file and line
# rather than vanishing. That matters more than the restriction itself: before
# it, the census could not see a wrongly-bound claim AT ALL, because the shape
# HAD bound the span — the same shape as the hand-written-binary-list blind
# spot, one layer up.
#
# THE TWO HALVES ARE BOTH REQUIRED and the split is worth keeping straight.
# The gap rule closes "was re-run and found 2 orphans" and "is the gate; the
# sweep counted 2"; `_POST_VERB` (immediately above) closes "returns, for the
# 2 tracking files, a count of 3", which has an EMPTY verb gap and which the
# gap rule therefore admits. Only the `_POST_VERB` case carries its own
# regression test; the two gap-rule reproductions are named here because
# nothing else records them.
CLAIM = (
    r"`(?P<cmd>[^`\n]{4,200})`"          # the command
    # ONE `gap` group, two grammars — ClaimShape's contract requires the group
    # to exist on every path, and negation_window() reads it with no fallback.
    # Each branch stops on a lookahead so the gap still excludes the arrow or
    # verb itself, exactly as the single-branch form did.
    r"(?P<gap>[^`\n]{0,40}?(?=→|->)"
    r"|" + _VERB_GAP + r"(?=\b(?:" + _REPORT_VERB + r")\b))"
    r"(?:→|->|\b(?:" + _REPORT_VERB + r")\b)"
    + _POST_VERB +
    r"\*{0,2}(?P<value>{value})\*{0,2}")

# SHAPE 2 — command, then a COLON, then the stated value:
# "`python3 tools/recurring-defect-lint.py --repo .`: **0 ERROR**".
# Round 16 (H7). The colon must be adjacent to the closing backtick — a colon
# anywhere inside shape 1's 40-character gap would bind across an unrelated
# clause, which is the defect round 8 filed. Adjacency makes the form
# unambiguous, and it is the form this repo's gate lines actually use.
CLAIM_COLON = (
    r"`(?P<cmd>[^`\n]{4,200})`(?P<gap>\s*:\s*)"
    r"\*{0,2}(?P<value>{value})\*{0,2}")

# SHAPE 3 — the VALUE-FIRST shape: "8 scripts (`ls tools/*.py`)", "35 (`ls -d
# src/*/ \| wc -l`)". Round 9 (L2) named this as unrecognised AND uncounted and
# stopped there; round 12 closed it, because the live instances are exactly the
# drift-prone kind — Spec #20 §5.4.5 states the assembly count this way, in
# APPROVED text, and it goes stale the day the 36th assembly lands. The
# parenthesis is required: it is this repo's idiom for "and here is how to
# check it", and without it the pattern would bind any number near any
# backtick.
CLAIM_VALUE_FIRST = (
    r"\*{0,2}(?P<value>{value})\*{0,2}"
    r"(?P<gap>(?:\s+[A-Za-z][\w./-]*){0,4}\s*)"
    r"\(\s*`(?P<cmd>[^`\n]{4,200})`\s*\)")

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
# Round 23 (H24) added the bare STEM `derive` beside `derived`: README's own
# tree diagram writes the instruction, not the record — "[60 design
# supplements — re-derive with `ls … \| wc -l`]" — and the past-tense-only
# list could not read it.
_ATTRIBUTION_VERB = (
    r"(?:re-)?(?:derives?|derived|measured|counted|verified|confirmed|"
    r"computed|checked|produced|reproduced|obtained|generated|re-run|rerun)")
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
# EVERY RUN IN THIS SHAPE IS A SINGLE CHARACTER CLASS, never an alternation
# of "ordinary char" and "newline plus indent". The alternation form
# `(?:[^`\n]|\n[ \t>]*)` is ambiguous — "\n> " can be consumed by either
# branch — and under two bounded repetitions that is catastrophic
# backtracking: measured on the first draft, the whole scan went from 1.5 s
# to no result in 120 s. The newline BUDGET is enforced in rejects()
# instead, where it is a two-line rule rather than a regex nobody can reason
# about.
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
# Round 23 (H24): `with` joins the earned connectives (`by`/`from`/`using`),
# and the whitespace run before the command admits a WRAP PREFIX. This repo
# hard-wraps inside an ASCII tree diagram, where the continuation line opens
# with box-drawing rules and spaces:
#
#     │       └── *-design.md   [60 design supplements — re-derive with
#     │                        `ls docs/tracking/*-design.md \| wc -l`; ...]
#
# A bare `\s+` stops at the `│`, so a LIVE, drift-capable claim quoting the
# very command this tool's LIVE floor is built around bound to nothing. The
# run is ONE character class (never an alternation of "ordinary char" and
# "newline plus indent" — that form is ambiguous under two bounded
# repetitions and took a draft scan from 1.5 s to no result in 120 s), and
# the one-hard-wrap budget in AttributedShape.rejects() still bounds it,
# because this run sits inside the `gap` group it counts newlines in.
# (The single-class rule is _ATTR_PRE/_ATTR_POST's, stated above; it applies
# here for the same reason and is not restated.)
#
# THE CLASS HOLDS EXACTLY TWO PREFIX CHARACTERS AND NOT A THIRD, and the
# third is the one worth recording. The round-23 draft of this line read
# `[\s│|>]+`, admitting the ASCII PIPE as well — which is a markdown TABLE
# CELL separator, so `| 35 | re-derived by | \`ls -d src/*/ \| wc -l\` |`
# bound the 35 out of a neighbouring column. That is H21's own defect
# (a value bound from an unrelated clause) reintroduced one shape over, in
# the same commit that fixed it. Measured before removing it: the ASCII pipe
# bound NOTHING on this tree — shape 4's bind count was identical with and
# without it — so it was pure risk. `│` is H24's actual need — the tree
# diagram's
# continuation rule — and `>` is the blockquote continuation this repo writes
# in every design supplement; `>` binds one further span here
# (CHANGELOG.md:4177), a backticked identifier that check_claim ignores and
# the census then names, which is the designed disposal, not a finding.
CLAIM_ATTRIBUTED = (
    r"\*{0,2}(?P<value>{value})\*{0,2}"
    r"(?P<gap>(?:" + _ATTR_PRE + r"\b" + _ATTRIBUTION_VERB + r"\b"
    + _ATTR_POST + r"\b(?:by|via|per|using|from|with)"
    r"|" + _ATTR_BARE + r"\b(?:via|per)"
    r")[\s│>]+)"
    r"`(?P<cmd>[^`\n]{4,200})`")


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

# DERIVED, in first-use order, never written out (round 19, H18). Registering
# an answer kind is naming it on a shape; there is no second place to forget.
ANSWER_KINDS = tuple(dict.fromkeys(shape.answer for shape in CLAIM_SHAPES))

# Every backticked span, for the unrecognised-shape census below.
COMMAND_SPAN = re.compile(r"`(?P<cmd>[^`\n]{4,200})`")
# How near an integer must be, on the SAME line, for a command-shaped span that
# binds to no shape to be reported as a possible claim this tool cannot read.
# Bounding to the line is what stops a census entry being manufactured out of
# the next bullet's numbers, and 200 characters is where the census effectively
# SATURATES. RE-MEASURED 2026-08-22 after round 23, because that round changed
# three things this curve depends on — H21 (shape 1 binds less, so more spans
# reach the census), H25 (an ignored span no longer reserves itself) and H24
# (the window widens by one line each way when the span's own line has no
# digit): 122 / 154 / 186 / 194 / 199 / 199 for radii
# 40 / 60 / 120 / 200 / 400 / unbounded-within-the-window. The shape of the
# curve is unchanged and so is the conclusion — past 200 a wider window buys
# five more entries and then nothing, while below it the census starts
# missing spans on this repo's very long bullet lines — but every absolute
# count moved, which is why the figures are re-derived rather than carried.
# (Round 20, M19: the figures on this line were once six-for-six wrong
# against the very re-measurement procedure this comment prescribes — the
# defect class this file exists to close, found inside the file that closes
# it.) Re-measure this curve whenever the corpus changes shape or this file's
# own matchers change, and per M19: DO NOT copy the figures on this line into
# prose elsewhere without re-running the measurement, because that copy is
# what went stale here.
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
        """The command's real first token — see `command_head()`, the one
        tokenize-derived parser both this property and the census
        (`unrecognised_spans`) call, so a third disagreeing copy cannot
        appear (round 20, M15).

        Round 17 (L2). This property used to be `self.cmd.split()[0]`: a
        SECOND parser for "the head token", disagreeing with the one
        validation (parse_pipeline, denied_flag, expand_globs, ...) actually
        uses. Proven: `` `"grep" -c '^A' CLAUDE.md` `` tokenizes to a real,
        runnable `grep` command — tokenize() strips the quotes exactly the
        way it must for parse_pipeline to accept the claim — but the naive
        split's head was the six characters `"grep"`, never equal to `grep`,
        so a perfectly good claim failed `head not in ALLOWED_CMDS` in
        check_claim() below, then failed command_shaped()'s quote-led shape
        test too, and fell through the return-("ignored", None) branch: 0
        executed, every decline bucket at 0, PASS — not run, not counted, not
        named. That is round 9's M1 (the tool's one silent decline route)
        re-created by a disagreeing parser rather than a bare `continue`.

        Returns None when `self.cmd` does not tokenize AT ALL (an
        unterminated quote, a trailing backslash) — check_claim() declines
        that through the same NAMED path parse_pipeline already owns for it,
        rather than falling through here."""
        return command_head(self.cmd)

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


def unrecognised_span_reason(cmd):
    """The decline reason for one `unrecognised_spans` hit — a plain
    unrecognised-shape span, or one that also carries a FORBIDDEN shell
    metacharacter (round 20, M15). Kept as a separate function, rather than a
    third field on the generator's yield, so `unrecognised_spans` keeps
    returning the two-tuple its own test fixture asserts against."""
    if FORBIDDEN.search(cmd):
        return ("command-shaped, an integer is nearby, and it contains a "
                "shell metacharacter FORBIDDEN refuses (redirection, "
                "substitution or chaining) — no claim shape can ever bind "
                "it, so it is named here rather than silently dropped")
    return ("command-shaped, an integer is nearby, and NO claim shape binds "
            "it — this tool cannot read the claim, if it is one")


def unrecognised_spans(text, bound):
    """Command-shaped backticked spans with an integer nearby that NO shape
    bound — yields (line, cmd). See `unrecognised_span_reason()` for the
    decline text a caller should print for each hit.

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
    are better in the printed list than in nobody's.

    Round 20 (M15). This used to have TWO defects, the naive-head one shared
    with `Claim.head` before round 17 (L2) fixed it there and not here, and a
    second of its own: every span containing `>` `;` or a backtick was
    dropped by a bare `if FORBIDDEN.search(cmd): continue`, silently, before
    this function's own coverage claim ("a command-shaped span ... is counted
    and named") could apply to it. Both fixed the same way — derive, don't
    drop: the head now comes from `command_head()`, the one tokenizer this
    file and `Claim.head` both call, so a quoted head (`` `"grep" -c ...` ``)
    is recognised here exactly as it is there; and a FORBIDDEN-bearing span
    that is otherwise command-shaped is yielded like any other census hit
    instead of vanishing — its caller names the difference via
    `unrecognised_span_reason()` — so a document line that quotes
    `rm -rf / \|\| true` is named as unreadable rather than uncounted."""
    for s in COMMAND_SPAN.finditer(text):
        if s.start("cmd") in bound:
            continue
        cmd = s.group("cmd").strip()
        head = command_head(cmd)
        if head is None:
            # Same crude fallback the untokenizable case gets everywhere
            # else in this file (round 20, M16's sibling note): the census's
            # job is to over-report, not to require a valid pipeline.
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
        if not (command_shaped(cmd, head)
                or (head in ALLOWED_CMDS and " " in cmd)):
            continue
        ls = text.rfind("\n", 0, s.start()) + 1
        le = text.find("\n", s.end())
        le = len(text) if le == -1 else le

        def near(lo, hi):
            return (text[max(lo, s.start() - UNRECOGNISED_RADIUS):s.start()]
                    + text[s.end():min(hi, s.end() + UNRECOGNISED_RADIUS)])

        if not re.search(r"\d", near(ls, le)):
            # BOUNDING THE CENSUS TO THE SPAN'S OWN LINE was right for its
            # stated job — stopping an entry being manufactured out of the
            # next bullet's numbers — and wrong for the shape this repo
            # actually writes, because it HARD-WRAPS MID-SENTENCE: a stated
            # value on one physical line and the command that re-derives it
            # on the next was bound by no shape AND unnameable by the
            # census. A corpus sweep found 13 runnable command spans in that
            # shape, one of them a live instance of the very command this
            # file's LIVE floor is built around.
            #
            # So the window widens by ONE physical line each way, and only
            # when the span's own line carries no digit at all — a line that
            # has one is already answered, and widening it would pull in the
            # neighbour's numbers for no gain. The radius still applies, so
            # this reaches a neighbouring line's digits only when they are
            # genuinely near. The SHAPES stay line-bounded; only the
            # over-reporting census widens, which is the asymmetry that keeps
            # a fabricated finding impossible here — everything this bucket
            # names is named, never compared.
            pls = text.rfind("\n", 0, ls - 1) + 1 if ls > 0 else 0
            nle = text.find("\n", le + 1) if le < len(text) else le
            nle = len(text) if nle == -1 else nle
            if not re.search(r"\d", near(pls, nle)):
                continue
        # Yields (line, cmd) only — the FORBIDDEN-vs-ordinary reason text is
        # picked by the caller (unrecognised_span_reason() below), so this
        # generator's return shape stays the two-tuple its test fixture
        # already asserts against.
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


def command_head(cmd):
    """`cmd`'s real first token, via the quote-aware tokenizer — never a
    naive whitespace split.

    Round 20 (M15). This used to be TWO disagreeing parsers: `Claim.head`
    (below) already tokenizes, because a second parser disagreeing with the
    one validation actually uses is how round 9's M1 silent-decline route
    came back one function later — but `unrecognised_spans`' own head
    extraction was still `cmd.split()[0]`, unable to see past a quoted head
    the same way. Proven: `` `"grep" -c '^A' CLAUDE.md` `` naive-splits to the
    six characters `"grep"`, never a member of ALLOWED_CMDS or
    `_KNOWN_BINARIES`, so a runnable claim in an unrecognised shape vanished
    from the census exactly as it used to vanish from `check_claim` before
    round 17 (L2) — the same blind spot, one function over, with the header's
    "read that bucket ... for what the tool cannot see" promise false for it.
    One helper now, called from both, so a third copy cannot appear.

    Returns None when `cmd` does not tokenize AT ALL (an unterminated quote,
    a trailing backslash) — callers decide what that means for them."""
    segments = tokenize(cmd)
    if segments is None:
        return None
    for seg in segments:
        if seg:
            return seg[0][0]
    return ""


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


def _long_option_core(tok):
    """`--name` out of `--name` or `--name=value`, or None when `tok` is not a
    long option at all (`-`, `--`, a short cluster, a bare operand).

    `--` returns None deliberately: it is the end-of-options marker, it is
    live in this corpus (`git grep -c … -- docs/specs`, three of the seven
    executed claims — re-derived 2026-08-22 after round 23), and it names no
    option to abbreviate."""
    if not tok.startswith("--") or len(tok) <= 2:
        return None
    return tok.split("=", 1)[0]


def _denied_long_names(*tables):
    """Every DENIED long option NAME across `tables`, `=` suffix stripped.

    Derived from the same `denied_flags` a binary already declares (plus, for
    git, `GIT_GLOBAL_DENIED`), never maintained separately — round 17 (L1)
    consolidated those tables precisely because a parallel list is a list
    someone forgets. Takes `*tables` rather than one so the git-global call
    site below can hand it a second table; every current caller passes
    exactly one."""
    names = set()
    for table in tables:
        for entry in table or ():
            core = entry.split("=", 1)[0]
            if core.startswith("--") and len(core) > 2:
                names.add(core)
    return names


def _abbreviated_denial(tok, names):
    """The denied long option `tok` ABBREVIATES, or None.

    Round 18 (H12), and the fourth consecutive round in which a denied hatch
    walked past this file RESPELLED rather than renamed. `_option_cores`
    compares a long option's core by EXACT string — but `getopt_long`, which
    every GNU binary on ALLOWED_CMDS uses, accepts any UNAMBIGUOUS PREFIX of
    a long option name. So `--output` was denied while `--o=`, `--out=` and
    `--outpu=` were not, and all three write the file. Reproduced before this
    fix, each under a printed PASS:
      * `sort -S 1 --compress-progr=/tmp/pwn.sh big.txt \\| wc -l` EXECUTED an
        attacker-supplied script on the runner (`-S 1` forces the spill that
        invokes it);
      * `sort --o=/tmp/OUTSIDE data.txt` wrote OUTSIDE the checkout, which
        path confinement could not see because the target is an option VALUE
        (round 18, H14 — fixed in escaping_operand below);
      * `sort --outpu=IN_REPO_CANARY data.txt` wrote inside it.
    Not one of those is a new hatch. Each is a hatch this file already denies,
    spelled the way the binary's own parser accepts it.

    The rule is therefore a PREFIX rule, not a longer list of spellings: a
    long-option token is denied when its core is a prefix of ANY denied long
    name for that binary. It deliberately over-refuses relative to
    `getopt_long` — a prefix shared by two options is AMBIGUOUS and the real
    binary would reject it, while this refuses it — because over-refusal is
    this file's stated safe direction, and a claim refused here is DECLINED
    AND NAMED, never silently dropped. Zero of the seven executed claims
    uses an abbreviated long option (they use `--` and nothing else —
    re-derived 2026-08-22 after round 23), so the cost on this corpus is
    nil."""
    core = _long_option_core(tok)
    if core is None:
        return None
    for name in sorted(names):
        if name.startswith(core):
            return name
    return None


def denied_flag(argv):
    """The write/execute escape hatch this argv reaches for, or None.

    Round 9 (H1). Read-only-by-allow-list was false: `sed -i`, `find -delete`,
    `python3 -c`, `sort -o`, `rg --pre` and `git -c` all execute or write from
    a binary the list called read-only."""
    name = argv[0]
    binfo = BINARIES.get(name, _NO_BINARY)
    exact = binfo.denied_flags
    long_denied = _denied_long_names(exact)
    for a in argv[1:]:
        for core in _option_cores(a):
            if core in exact:
                return a if core == a else "%s, attached as `%s`" % (core, a)
        # Round 18 (H12): the same denied long option, ABBREVIATED — see
        # _abbreviated_denial() for the three reproductions this closes.
        full = _abbreviated_denial(a, long_denied)
        if full is not None:
            return ("%s, a GNU long-option ABBREVIATION of the denied `%s` — "
                    "getopt_long accepts any prefix" % (a, full))
    if name == "git":
        # Round 20 (M23): the `or core in ("--exec-path", "--upload-pack")`
        # disjunct this branch used to carry was DEAD BY CONSTRUCTION —
        # GIT_GLOBAL_DENIED (BINARIES["git"].git_global_denied) already
        # contains both strings verbatim, so the extra disjunct, and the
        # matching extra tuple handed to _denied_long_names below, could
        # never change either call's result. Deleted; GIT_GLOBAL_DENIED
        # alone is both the exact-match set and the abbreviation-name
        # source, same as every other binary's git_global_denied-style use.
        git_long_denied = _denied_long_names(GIT_GLOBAL_DENIED)
        for a in argv[1:]:
            if not a.startswith("-"):
                break            # the subcommand: globals end here
            for core in _option_cores(a):
                if core in GIT_GLOBAL_DENIED:
                    return a if core == a else ("%s, attached as `%s`"
                                                % (core, a))
            full = _abbreviated_denial(a, git_long_denied)
            if full is not None:
                return ("%s, a GNU long-option ABBREVIATION of the denied "
                        "git global `%s`" % (a, full))
    if name == "awk":
        # SCAN EVERY TOKEN, never the token GUESSED to be the program.
        # `-v` and `-F` take a SEPARATE argument, so "first operand not
        # starting with -" picked `x=1` out of `awk -v x=1 '...' f` and never
        # looked at the program at all. Scanning all tokens needs no awk
        # option grammar and cannot be outflanked by adding one; a FILENAME
        # that happens to contain a refused word is merely
        # declined-and-named, which is the safe direction. Locked by
        # HatchRegressionTests.test_awk_with_a_leading_v_assignment.
        #
        # WHAT IS SCANNED FOR IS AN ALLOW-LIST, NOT A BLACKLIST, because
        # awk's escape lives in its SCRIPT and a script is a language, not a
        # vocabulary: `print x | "sh"` runs a shell command using neither of
        # the two words a blacklist named. So what the program may CALL is
        # allow-listed, and the two characters that no call-name list can
        # describe are refused outright.
        for a in argv[1:]:
            if "|" in a:
                return ("awk token containing `|` — `print x | \"cmd\"` and "
                        "`\"cmd\" | getline` both run a shell command")
            if "@" in a:
                return "awk token containing `@` — gawk @load/@include"
            # Round 18 (H14, the related note) for ENVIRON/PROCINFO; round 22
            # (H19) for ARGV/ARGC and gawk's SYMTAB/FUNCTAB. A special array
            # is a VARIABLE, not a call, so AWK_ALLOWED_CALLS — an allow-list
            # of FUNCTION names — structurally cannot see one:
            # `awk 'END{print length(ENVIRON["TD_SECRET"])}' data.txt` calls
            # only `length(`, and
            # `awk 'BEGIN{ARGV[ARGC++]="/etc/passwd"}END{print NR}' data.txt`
            # calls NOTHING AT ALL. Both are one-integer read oracles — one
            # over the runner's environment, one over any file it can open —
            # so both are refused flat, beside `|` and `@`, for the reason
            # those two are: no allow-list of call names can express them.
            # See AWK_SPECIAL_ARRAYS for the reproductions.
            for pattern, why in AWK_SPECIAL_ARRAYS:
                hit = pattern.search(a)
                if hit is not None:
                    return ("awk token naming the special array `%s`, %s — a "
                            "variable, not a call, so the function allow-list "
                            "structurally cannot see it" % (hit.group(0), why))
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
# answer to anything. Round 17 (L1): each binary's own `benign_exit` field in
# BINARIES, above, is this table now — see run_pipeline()'s use of it below.


# A hard ceiling on how much a single pipeline segment may print. The answers
# this tool compares are single integers, so any legitimate segment is orders
# of magnitude below this; the cap exists because TIMEOUT_S bounds wall time
# and nothing bounded MEMORY (round 15, M1).
OUTPUT_CAP_BYTES = 8 * 1024 * 1024


# ---------------------------------------------------------------------------
# CHILD RESOURCE LIMITS — DEFENCE IN DEPTH, NOT A REPLACEMENT.
#
# This is not a review finding. It is a structural decision taken on the
# evidence the SAFETY header records: one defect class recurred five
# consecutive rounds, each time INSIDE the previous round's fix, because
# every fix in that chain ASSERTS read-only-ness rather than ENFORCING it.
#
# The fifth of those is also the sharpest available statement of what this
# section can and cannot do: the hatch that ended the chain was a clustered
# short option that walked past confinement and READ a host file, and not
# one limit below applies to it, because every one of them bounds writing,
# CPU and memory and none of them bounds a read. The limits are defence in
# depth for the WRITE/EXECUTE half only; confinement (rule 4) is still the
# whole of the read half, on its own, exactly as stated below. Locked by
# ChildResourceLimitTests and ResourceLimitMathTests.
#
# So the property is moved off the allow-list and onto the kernel. Every child
# this tool spawns runs under:
#   RLIMIT_FSIZE = 0   — no file may GROW past zero bytes: a write of any
#                        byte raises SIGXFSZ and kills the child, whatever
#                        hatch produced it, whether or not this file has ever
#                        heard of it. Measured caveat, not assumed: creating
#                        an empty file and TRUNCATING an existing one still
#                        succeed, so this bounds what a child can write, not
#                        that it can touch nothing.
#   RLIMIT_CPU         — CPU seconds, complementing TIMEOUT_S: a wall-clock
#                        timeout does not bound a spinning child's cost, and
#                        a CPU limit does not bound a sleeping one, so both.
#   RLIMIT_AS          — an address-space ceiling, so an allocation bomb dies
#                        in the child instead of OOM-killing the runner.
#                        OUTPUT_CAP_BYTES bounds what a child PRINTS; nothing
#                        bounded what it ALLOCATES without printing.
#
# WHAT IT DOES NOT DO, because a limit stated too broadly is the same error
# as an allow-list stated too broadly:
#   * it does not stop READS. A child may still open any file the runner can
#     read; path confinement (escaping_operand) is the only thing that stops
#     that, and it stays exactly as strict.
#   * it does not stop EXECUTION as such. `--compress-program=./p.sh` under
#     these limits still RUNS the script — it just cannot write anything. The
#     deny-lists stay exactly as strict too.
#   * it is POSIX-only. On a platform with no `resource` module the tool
#     degrades to the previous behaviour and SAYS SO in its printed output
#     (child_limit_summary(), printed by every run) rather than silently
#     losing a safety property.
# Nothing above was relaxed because these limits exist.
#
# The tool's own temp files are unaffected: stdin is staged by the PARENT
# (tempfile.TemporaryFile in run_pipeline), which runs under no such limit,
# and a child's stdout is a PIPE — RLIMIT_FSIZE bounds regular files, not
# pipes — so the output cap and the reader thread are untouched. Verified by
# running the whole live corpus, not by reasoning about it.
# ---------------------------------------------------------------------------
CHILD_FSIZE_BYTES = 0
CHILD_CPU_SECONDS = TIMEOUT_S
CHILD_AS_BYTES = 2 * 1024 * 1024 * 1024
RLIMITS_AVAILABLE = resource is not None


def _tightest(*values):                  # pragma: no cover - runs post-fork
    """The strictest of some rlimit values, treating RLIM_INFINITY as "no
    limit" rather than as a number.

    `min()` cannot be used directly and the reason is a trap, not a nicety:
    on Linux `resource.RLIM_INFINITY` is **-1**, so `min(0, RLIM_INFINITY)`
    is RLIM_INFINITY — i.e. the naive clamp turns the load-bearing
    RLIMIT_FSIZE=0 into "unlimited" on the completely ordinary host whose
    inherited file-size limit is infinite. Verified against the values this
    file actually sets, not assumed."""
    out = resource.RLIM_INFINITY
    for value in values:
        if value == resource.RLIM_INFINITY:
            continue
        out = value if out == resource.RLIM_INFINITY else min(out, value)
    return out


def _lower_limit(which, soft, hard):     # pragma: no cover - runs post-fork
    """Set one rlimit to the STRICTEST of {what this file asks for, the soft
    limit inherited from the runner, the hard limit inherited from the
    runner}, so that neither field can come out above the value it had on
    entry. This function only ever tightens.

    setrlimit fails when the requested hard limit exceeds the inherited one,
    and a failure here surfaces as "command could not be executed" — a
    decline, so it fails safe, but it would decline every claim on a runner
    that already caps CPU. Hence the clamp rather than a bare setrlimit.

    ROUND 21 (H2). The docstring used to say "never RAISING one the runner
    already imposes" while the code clamped against the inherited HARD limit
    alone, so an inherited SOFT limit was raised — the opposite of the stated
    guarantee, in the function whose whole purpose is that guarantee.
    Reproduced: with an inherited RLIMIT_CPU of (10, 100) the child came out
    with (60, 65) — six times the CPU the runner intended — and with an
    inherited (10, RLIM_INFINITY) no clamping happened at all, so the soft
    limit still went 10 → 60. The same weakening applied to RLIMIT_AS, where
    a runner's memory cap is a real containment measure.

    RLIMIT_FSIZE=0 is unaffected and that is checked rather than assumed: 0 is
    the minimum a limit can take, so the strictest of {0, anything} is 0 — but
    only once RLIM_INFINITY is excluded from the comparison, which is why
    _tightest() exists above. A child still cannot grow any file by one byte."""
    cur_soft, cur_hard = resource.getrlimit(which)
    hard = _tightest(hard, cur_soft, cur_hard)
    soft = _tightest(soft, cur_soft, cur_hard, hard)
    resource.setrlimit(which, (soft, hard))


def _apply_child_limits():               # pragma: no cover - runs post-fork
    """preexec_fn: runs in the forked child, before exec, so the limits are
    already in force when the untrusted binary starts.

    RLIMIT_FSIZE is the load-bearing one and is deliberately NOT wrapped: if
    it cannot be set, the child must not run, and Popen turns the exception
    into this file's named "could not be executed" decline. RLIMIT_AS is
    best-effort — some hosts refuse it outright — because it bounds cost, not
    authority, and losing it must not cost the write ceiling.

    The `+ 5` on the CPU hard limit buys a SIGXCPU (at the soft limit) before
    the SIGKILL (at the hard one), so the kill is named rather than anonymous.
    On a runner that already caps CPU more tightly than this file asks, round
    21's clamp collapses that grace — soft and hard both come down to the
    runner's own soft limit — which costs nothing: `_LIMIT_KILL` names SIGKILL
    as a resource kill too, so the claim is still DECLINED AND NAMED."""
    _lower_limit(resource.RLIMIT_FSIZE, CHILD_FSIZE_BYTES, CHILD_FSIZE_BYTES)
    _lower_limit(resource.RLIMIT_CPU, CHILD_CPU_SECONDS, CHILD_CPU_SECONDS + 5)
    try:
        _lower_limit(resource.RLIMIT_AS, CHILD_AS_BYTES, CHILD_AS_BYTES)
        _lower_limit(resource.RLIMIT_CORE, 0, 0)
    except (ValueError, OSError, AttributeError):
        pass


def child_limits_preexec():
    """The preexec_fn to hand Popen, or None where `resource` is missing."""
    return _apply_child_limits if RLIMITS_AVAILABLE else None


def child_limit_summary():
    """One line, printed by every run: what the OS is enforcing, or that it is
    enforcing nothing. A safety property that degrades silently is the decline
    contract failing in the direction this file calls its worst."""
    if not RLIMITS_AVAILABLE:
        return ("UNAVAILABLE — no `resource` module on this platform, so "
                "children run under the allow-list, the deny-flags, path "
                "confinement, the %ds timeout and the %d-byte output cap "
                "ALONE, with no OS-enforced write/CPU/memory ceiling"
                % (TIMEOUT_S, OUTPUT_CAP_BYTES))
    return ("RLIMIT_FSIZE=%d (no file may grow past 0 bytes — a write kills "
            "the child), RLIMIT_CPU=%ds, RLIMIT_AS=%d MiB — enforced by the "
            "OS, not asserted by a list; READS are not limited by these"
            % (CHILD_FSIZE_BYTES, CHILD_CPU_SECONDS,
               CHILD_AS_BYTES // (1024 * 1024)))


# A child killed by one of these limits is DECLINED AND NAMED, like every
# other refusal — never a crash, and never the silent `0` that a killed
# `wc -l` would otherwise hand to the comparison. Keyed by signal number so a
# platform missing one of them simply has one fewer row.
_LIMIT_KILL = {}
for _sig, _why in (
        ("SIGXFSZ", "was killed attempting to WRITE a file — this tool runs "
                    "every command under a zero write limit (RLIMIT_FSIZE), "
                    "so its output is not treated as an answer"),
        ("SIGXCPU", "exceeded the %ds CPU limit (RLIMIT_CPU) and was killed "
                    "— its output is not treated as an answer"
                    % CHILD_CPU_SECONDS),
        ("SIGKILL", "was killed by the kernel, which on this tool's children "
                    "means a hard resource limit (CPU or address space) was "
                    "reached — its output is not treated as an answer")):
    if hasattr(signal, _sig):
        _LIMIT_KILL[int(getattr(signal, _sig))] = _why
del _sig, _why


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
                    stderr=subprocess.DEVNULL,
                    preexec_fn=child_limits_preexec())
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
                # Round 24 (L16). This join used to be unchecked: whatever
                # its outcome, control fell through to `proc.stdout.close()`
                # and a normal decline, as if the reader were always known
                # dead. CPython documents `preexec_fn` as unsafe in the
                # presence of threads — a child forked while another thread
                # holds a lock (the allocator, `import`, ...) can deadlock
                # in the window between fork() and exec(), because the lock
                # is copied held with no thread left in the child to release
                # it. A reader still blocked on `stream.read1()` 5 extra
                # seconds after its process was killed is not expected, but
                # it is exactly the live-thread-at-fork state that warning
                # describes, and the very next `Popen` in this loop — or in
                # the next claim's call to this function — forks while it
                # may still be alive. Declined and named instead of risking
                # that fork: `proc.stdout` is left OPEN deliberately, since
                # closing a stream a thread is actively reading from is its
                # own race, not a fix.
                if reader.is_alive():
                    return None, (
                        "`%s` output reader did not stop within 5s of the "
                        "child being killed — declined rather than risking "
                        "a live thread across the next fork (preexec_fn is "
                        "documented unsafe in the presence of threads)"
                        % argv[0])
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
        if rc < 0 and -rc in _LIMIT_KILL:
            # Round 18. A child that hit an OS limit did not "exit" with a
            # status — it was SIGNALLED, and Popen reports that as a negative
            # rc. Named here rather than left to the generic "exited -25"
            # below, because the whole point of the limits is that the reason
            # a command was stopped is legible: "sort was killed attempting
            # to write" is a security event, not a broken command.
            return None, "`%s` %s" % (argv[0], _LIMIT_KILL[-rc])
        if rc != 0 and rc not in BINARIES.get(argv[0], _NO_BINARY).benign_exit:
            return None, ("`%s` exited %d — its output is not treated as an "
                          "answer" % (argv[0], rc))
        data = box["data"]
    try:
        return data.decode("utf-8", "replace"), None
    except Exception:                                    # pragma: no cover
        return None, "output is not decodable text"


# The command side is stricter than the document side's _WELL_FORMED_INT in
# one direction and looser in another. Stricter: no comma is accepted at all,
# because no allow-listed binary's output ever contains one (the document
# side allows one for readability; the command side is what that readability
# is being checked AGAINST). Looser: an optional leading `-`, because `awk`
# can legitimately compute and print a negative number, and without it a
# negative answer could not be READ at all — `single_integer("-3\n")` was
# None, which check_claim() reports as "not-a-single-integer", a decline that
# says the tool could not tell. That buries a genuine finding: a document's
# POSITIVE claim compared against a real NEGATIVE answer is a mismatch this
# tool can now actually make, rather than a claim it silently gives up on.
# (A third copy of the "how many executed claims pipe through awk" fraction
# used to sit here and had drifted both wrong on the number and loose on the
# term — it said "live", which this file defines precisely; see
# MIN_LIVE_CLAIMS. The SCRIPT-hatches paragraph above is the canonical site,
# and it deliberately carries no number.)
_WELL_FORMED_COMMAND_INT = re.compile(r"\A-?(?:0|[1-9]\d*)\Z")


def single_integer(out):
    """The command's answer as an int, or None if it did not print exactly one
    well-formed integer literal (see _WELL_FORMED_COMMAND_INT above)."""
    if out is None:
        return None
    lines = [l.strip() for l in out.splitlines() if l.strip()]
    if len(lines) != 1:
        return None
    if not _WELL_FORMED_COMMAND_INT.match(lines[0]):
        return None
    return int(lines[0])


GLOB_CH = re.compile(r"[*?\[]")

# A quoted command missing its file operand reads stdin, which is empty here,
# and returns 0 or nothing — reporting that as "document says 18, command
# returns 0" would be a fabricated finding, the very thing this tool exists to
# prevent. This repo genuinely writes such prose ("`grep -c '^- \*\*'` over
# each file returns 18"), where the filename lives outside the backticks, so
# the command as quoted is not runnable and the claim is UNVERIFIABLE, not
# false. Which binaries need this check is each one's own `needs_file` field
# in BINARIES, above — round 17 (L1) deleted the separate table this used to
# be: it still carried a `sed` row when this function was last touched, dead
# ever since `sed` was dropped from ALLOWED_CMDS at round 9, because nothing
# tied the two tables together. Consolidated, that specific drift cannot
# recur: a dropped binary has no Binary record at all, so `.get(name,
# _NO_BINARY).needs_file` reads False for it rather than a stale True.
def self_contained(segments, repo):
    """False when the first segment would read stdin because no token AT A
    POSITION AN OPERAND COULD OCCUPY names something real on disk under the
    repo root — a file, or a directory (round 20, M24).

    THE TEST NAMES A REAL FILE; IT DOES NOT MODEL AN OPTION GRAMMAR. The
    rule it replaced was `[a for a in argv[1:] if not a.startswith('-')]` —
    the same OPTION-GRAMMAR error as guessing awk's program token, because a
    flag's own VALUE does not start with `-` either and so counted as an
    "operand" that names nothing. `grep -m 3 -c x` produced ["3", "x"], met
    the old operand floor, ran with an EMPTY stdin, and reported "document
    says N; command returns 0" against a document that was never wrong —
    precisely what this guard exists to prevent. Enumerating which flag on
    which binary takes a separate-argument value is the shape of rule this
    file has repeatedly found does not hold up, so the fix does not try: a
    segment is self-contained exactly when at least one of its tokens,
    whatever else it might look like, names something real on disk.

    Round 20 (M21) narrowed "whatever else it might look like" to "at a
    position an operand can occupy", because CONTENT alone fabricates a
    finding: the rule as stated let a grep/awk PATTERN (never a file
    position at all) satisfy it merely by spelling a real path. Reproduced:
    `` `grep -c 'CLAUDE.md'` `` over each tracking file was declined
    honestly before this fix would have made it worse — `grep -c
    'CLAUDE.md'` ALONE, no file operand at all, ran against an EMPTY stdin
    and printed 0, which this guard then compared against the document's
    stated value as a real mismatch, fabricating the exact finding this
    guard exists to prevent. Fixed by POSITION, not by inspecting what the
    pattern says: `Binary.pattern_operand` (grep family, awk) marks that the
    first non-option token is consumed as the pattern/program and is
    excluded from the file search; every binary without that grammar keeps
    every non-option token as a candidate, exactly as before.

    Round 20 (M24). `os.path.isfile` alone declined `grep -rn TODO src/` —
    one of this file's own four motivating WHY THIS EXISTS cases — with a
    reason that says the segment "would read from an empty stdin", which is
    false: `-r` makes a directory operand a normal, real read. `os.path.isdir`
    is now accepted alongside `isfile`; a binary that cannot actually read a
    directory (`cat somedir`) still fails downstream at run_pipeline's own
    non-zero-exit decline, so widening the check here costs nothing and
    fixes the false reason for binaries that can."""
    argv = segments[0]
    binfo = BINARIES.get(argv[0], _NO_BINARY)
    if not binfo.needs_file:
        return True
    root = str(repo)
    operands = argv[1:]
    if binfo.pattern_operand:
        # The FIRST non-option token is the pattern/program, not a file
        # candidate — every later token (option or not) is unaffected.
        filtered, seen_pattern = [], False
        for a in operands:
            if not seen_pattern and not a.startswith("-"):
                seen_pattern = True
                continue
            filtered.append(a)
        operands = filtered
    return any(os.path.isfile(os.path.join(root, a))
               or os.path.isdir(os.path.join(root, a))
               for a in operands)


def _attached_option_values(tok):
    """EVERY value an option token could be carrying — a tuple, never one guess.

    `--file=PATH` → `("PATH",)`; `-`, `--`, `--file`, `-f` and a bare operand →
    `()`. `--` is excluded for _long_option_core's own reason (it is the
    end-of-options marker, live in this corpus).

    ROUND 21 (H1), and the FIFTH consecutive round in which an option hatch
    reached this file in a spelling it did not model — round 9's H1, round
    14's P1, round 15's H3 (attached values), round 18's H12 (GNU long-option
    abbreviations), and now CLUSTERING. Every one of those lived inside the
    previous one's fix, and the OS child limits added at round 18 do not touch
    this one, because a read is not a write.

    The predecessor returned `tok[2:]` for a short-option token — i.e. it
    ASSUMED the option letter is at index 1 and the value starts at index 2.
    That is false for a CLUSTER, and every GNU binary on ALLOWED_CMDS clusters:
    `grep -cf/etc/hostname data.txt` is `-c -f /etc/hostname` (`-c, --count`
    takes no value; `-f, --file=FILE` does), so the value is `/etc/hostname` —
    while `tok[2:]` computed `f/etc/hostname`, a RELATIVE path that does not
    exist, which therefore resolved INSIDE the repo root and passed
    confinement. Reproduced before this fix, driving the real validation chain:

        grep -c -f/etc/hostname data.txt   DECLINED (`-f/etc/hostname`)
        grep -c -f /etc/hostname data.txt  DECLINED (`/etc/hostname`)
        grep -cf/etc/hostname data.txt     ACCEPTED — escaping_operand → None

    and end to end as a working one-integer read ORACLE over host file
    CONTENTS: `grep -hf/etc/hostname data.txt data2.txt \\| wc -l` reproduced a
    stated `1` when a fixture file held the runner's real hostname and FAILED
    when it held a wrong guess — a document line interrogating /etc one bit at
    a time, which is exactly what SAFETY rule 4 exists to deny.

    THE RULE, and why it is not a sixth spelling waiting to happen. It does
    NOT ask which letters of which binary take a value — that is precisely the
    per-binary option modelling that has now failed five times, and this file
    will not do it again. It rests instead on one structural fact about short
    options that holds for POSIX `getopt` and GNU `getopt_long` alike: an
    ATTACHED short-option value is always `&argv[i][j+1]`, the remainder of the
    token after the option character — that is, a SUFFIX of the token. So the
    set of paths a single dash-token can hand a child is bounded, exactly, by
    its suffixes, and every suffix from index 2 (the earliest position a value
    can begin, since index 1 is always an option character) to the end is
    returned here for the identical realpath test. Which suffix the real binary
    would pick is then irrelevant: whatever it picks, this checked it. Adding a
    letter, reordering a cluster, or moving to a binary with a different option
    grammar cannot produce a value outside that set.

    Over-refusal is this file's stated safe direction and is what the extra
    suffixes buy: a cluster whose deeper suffix reads as an escaping path is
    DECLINED AND NAMED, never run. Measured cost on this corpus: nil — the
    five legitimate clustered forms (`grep -rn pattern dir/`, `sort -nr file`,
    `ls -la`, `wc -lc file`, `grep -ic pattern file`) and every claim this
    tool currently EXECUTES — not just the LIVE subset, which is a stricter,
    separately-floored term (see MIN_LIVE_CLAIMS); re-derive the count by
    running `python3 tools/doc-claim-check.py --repo .` rather than trusting
    a number copied here (round 24, M33 — the third copy of a coverage
    fraction this file found stale, and the second site conflating "live"
    with plain "executed") — still executes, because a suffix escapes only
    by being absolute or
    by traversing out with `..`, which no option letter cluster does.

    STATED HONESTLY, because a safety claim stated too broadly is this file's
    recurring error: the rule is exhaustive over paths carried INSIDE a
    dash-token. A path that reaches a child by some OTHER route is a different
    mechanism's job — a `key=value` bare operand (`awk -v x=/etc/passwd`,
    whose read hatches are separately closed by the `|`/`getline`/FORBIDDEN
    refusals), a file the binary opens on its own initiative, or the
    environment (refused for awk by name in denied_flag). Confinement here
    still reads one token as one path; it does not parse inside a token that
    is not an option."""
    if not tok.startswith("-") or tok in ("-", "--"):
        return ()
    if tok.startswith("--"):
        if "=" not in tok:
            return ()
        value = tok.split("=", 1)[1]
        return (value,) if value else ()
    # Every position a value could begin at, not the one position the old
    # code assumed. `-f/etc/hostname` → ("/etc/hostname", "etc/hostname", ...);
    # `-cf/etc/hostname` → ("f/etc/hostname", "/etc/hostname", ...), and it is
    # the SECOND of those that the old single-guess helper never looked at.
    return tuple(tok[i:] for i in range(2, len(tok)))


def escaping_operand(argv, repo):
    """The first operand whose realpath leaves the repo, or None.

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
    load-bearing the moment the execution hatches are closed.

    Round 18 (H14). The loop below used to open with `if a.startswith("-"):
    continue` — every option token skipped WHOLESALE — so a path handed to a
    command as an ATTACHED OPTION VALUE never reached the realpath test at
    all. That is the same respelling class as H12 one layer over: the
    separated forms (`grep -f /etc/hostname`) were correctly declined, and the
    attached forms were not. Reproduced, all four executed and COMPARED under
    a printed PASS:
      * `grep -c --file=/etc/hostname data.txt`
      * `grep -c -f/etc/hostname data.txt`
      * `git --git-dir=../other/.git rev-list --count HEAD`
      * `diff --from-file=/tmp/hostsecret.txt data.txt \\| wc -l` — whose
        printed integer is derived from the CONTENTS of a file outside the
        repository and lands in the FAIL block for anyone reading the CI log.
    So this file's own header property 4 ("a command may read the checkout and
    nothing else") was false as written. An option token is no longer skipped:
    its attached value, if it has one, is subjected to the identical test.

    Short-option values that are not paths at all (`-F:`, `-m3`, the `n` of a
    `-rn` cluster) resolve INSIDE the root and pass, so the widening costs
    nothing on this corpus; one that reads as an escaping path is declined and
    NAMED, which is the same over-refusal the bare-operand rule already
    accepts.

    Round 21 (H1). H14 above closed the ATTACHED spelling and left the
    CLUSTERED one open — `grep -cf/etc/hostname data.txt` reached the child
    and read the host file, because the helper took the value to be `tok[2:]`
    and a cluster puts it further along. Every suffix of a dash-token is a
    candidate value now, and the reason it cannot be respelled a sixth time is
    argued where the suffixes are produced, in _attached_option_values().

    RETURNS a rendered, backticked description rather than the bare token, so
    the caller's decline can NAME which value inside a cluster escaped — an
    unnamed refusal of `-cf/etc/hostname` would leave a reader guessing which
    of six suffixes the tool objected to."""
    root = os.path.realpath(str(repo))
    for a in argv[1:]:
        candidates = _attached_option_values(a) if a.startswith("-") else (a,)
        for candidate in candidates:
            real = os.path.realpath(os.path.join(root, candidate))
            if real != root and not real.startswith(root + os.sep):
                if candidate == a:
                    return "`%s`" % a
                return ("`%s` (the option value `%s` it can carry — a short "
                        "option's attached value is a SUFFIX of its token, "
                        "and this tool deliberately does not model which "
                        "letters of which binary take one)" % (a, candidate))
    return None


# How many directory entries `escaping_symlink_under` may examine under ONE
# operand before it gives up and DECLINES rather than pass a tree it did not
# finish reading. Deliberately far above anything this repo can present (a
# whole-tree walk here is ~3.6k entries including `.git`), because the cap is
# a runaway guard, not a policy: a tree big enough to hit it is one this tool
# could not have proved anything about, and "declined and named" is the only
# honest answer to that. Fail-safe by construction — hitting the cap refuses
# the claim, it does not wave it through.
SYMLINK_WALK_CAP = 500_000

# Memo for the walk, keyed by the resolved directory path. The tool only ever
# READS the tree — every child runs under RLIMIT_FSIZE=0 — so a directory's
# contents cannot change between two claims of one run, which is what makes a
# process-lifetime memo sound here. Keyed on the RESOLVED path so two
# spellings of one directory share an answer.
_SYMLINK_WALK_MEMO = {}


def escaping_symlink_under(argv, repo):
    """The first (operand-directory, escaping-symlink) pair reachable by
    recursing into an operand, rendered for a decline, or None.

    ROUND 22 (H20), and the half of that finding that is a RULE rather than a
    list. `escaping_operand` above realpaths each operand, which catches a
    symlink handed over DIRECTLY and nothing else: an in-repo DIRECTORY
    operand resolves inside the root and passes, and a recursing binary then
    walks THROUGH a symlink inside it to a file outside the checkout. On a
    `pull_request` trigger the checkout is the pull request's own head and
    `actions/checkout` preserves symlinks, so both halves — the symlink and
    the document line quoting the command — are attacker-supplied.

    The per-binary denial of `-R` / `-L` / `-follow` / `diff -r` (see the
    BINARIES note above) closes every following spelling this file knows
    about, and that is precisely the objection: it is a list of the traversal
    behaviours someone thought of, resting on each binary's documented
    default for everything else. THIS function does not model binaries at
    all. If an operand is a directory, the whole subtree under it must be
    free of symlinks leaving the root — whatever flags were passed, whatever
    the binary would have done with them, and whether or not this file has
    ever heard of the binary's traversal rules. `grep -rl root sub` is
    declined by this check exactly as `grep -Rl root sub` is, because the
    difference between them is a fact about GNU grep and not a fact this tool
    can enforce.

    Deliberately NOT scoped to binaries that recurse. `wc -l somedir` cannot
    read the subtree, and refusing it costs a claim that would have failed at
    run_pipeline's non-zero-exit decline anyway; scoping the check would mean
    keeping a second list of which binaries recurse, which is the shape of
    rule this file has now watched fail six times.

    WHAT IT DOES NOT COVER, stated because a containment claim stated too
    broadly is this file's recurring error:
      * a recursion with NO operand at all, which defaults to `.` — `find`
        with no path, `rg` with no file. Those are covered by the flag denial
        (`find -L`) or by `needs_file` (`rg` alone declines as reading an
        empty stdin), not by this walk.
      * a path the child derives from FILE CONTENTS rather than from argv —
        `find -files0-from`, `sort --files0-from`. There is no operand to
        walk there at all; both are denied by name in BINARIES.
      * TOCTOU. The walk runs before exec. Nothing in this tool's own run
        writes to the tree, so the window is against an external writer, and
        this file has never claimed to defend one.
      * a subtree the walk cannot READ. `os.walk` swallows a permission
        error and yields nothing for that directory, so it would be reported
        clean. Harmless rather than a hole, because the child runs as the
        same user: a directory this process cannot open is one the child
        cannot recurse into either.
    """
    root = os.path.realpath(str(repo))
    for a in argv[1:]:
        if a.startswith("-"):
            # A dash-token's suffixes are handled by escaping_operand's own
            # rule; none of them is a directory operand to recurse into.
            continue
        target = os.path.join(root, a)
        if not os.path.isdir(target) or os.path.islink(target):
            continue
        real = os.path.realpath(target)
        found = _SYMLINK_WALK_MEMO.get(real)
        if found is None:
            found = _walk_for_escaping_symlink(real, root)
            _SYMLINK_WALK_MEMO[real] = found
        if found:
            return a, found
    return None


def _walk_for_escaping_symlink(target, root):
    """The first symlink at or under `target` whose realpath leaves `root`,
    as a repo-relative path — or the cap message, or "" for a clean subtree.

    `followlinks=False` is the load-bearing argument: a symlinked directory
    appears in `dirnames` (so it IS examined) and is not descended into (so a
    symlink loop cannot hang the walk)."""
    seen = 0
    for dirpath, dirnames, filenames in os.walk(target, followlinks=False):
        for name in dirnames + filenames:
            seen += 1
            if seen > SYMLINK_WALK_CAP:
                return ("<more than %d entries — this tool stopped reading "
                        "before it could prove the subtree contains no "
                        "symlink leaving the checkout>" % SYMLINK_WALK_CAP)
            q = os.path.join(dirpath, name)
            if not os.path.islink(q):
                continue
            real = os.path.realpath(q)
            if real != root and not real.startswith(root + os.sep):
                return os.path.relpath(q, root)
    return ""


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
            # Round 16 (M9): pathlib normalises away a trailing slash on BOTH
            # ends — repo.glob("src/*/") already filters to directories only,
            # but relative_to() renders each hit as "src/a", never "src/a/".
            # A command that pipes to something anchored on the slash
            # (`ls -d src/*/ \| grep -c '/$'`) then compares against a
            # DIFFERENT command's output — bash's own expansion keeps the
            # slash, so `ls -d src/*/ \| grep -c '/$'` returns 2 there and 0
            # here for the identical claim. Restore the character the pattern
            # itself carried; bash's directory-glob does the same.
            if text.endswith("/"):
                hits = [h + "/" for h in hits]
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
    #
    # Round 20 (M23) considered, and REJECTED, deleting this re-run as
    # "subsumed by the `hit.startswith('-')` refusal above" — reproduced
    # instead of trusted, because that claim is false on the current code.
    # The startswith check only inspects successful glob HITS; it cannot see
    # a hatch that depends on the ARGV SHAPE post-expansion rather than on
    # any one token's spelling. Proven: `uniq tools/doc-*.py` — ONE
    # pre-expansion operand, so `uniq`'s own OPERAND-COUNT hatch
    # (denied_flag()'s uniq branch: `uniq IN OUT` writes OUT) does not fire
    # pre-expansion — expands to TWO real files, NEITHER of which starts
    # with `-`, so the startswith check passes it clean too. Only this
    # re-run, seeing the actual two-operand post-expansion argv, catches it.
    # A mutation-test claim that "all 71 tests stay green with this
    # deleted" was independently reproduced as true and is beside the
    # point: it is a gap in the SUITE's coverage of this exploit shape, not
    # evidence this code is dead. Kept.
    for argv in out:
        hatch = denied_flag(argv)
        if hatch is not None:
            return None, ("after glob expansion, `%s` reaches a write/execute "
                          "escape hatch (%s)" % (argv[0], hatch))
        outside = escaping_operand(argv, repo)
        if outside is not None:
            # `outside` arrives already backticked and, for a value carried
            # inside an option cluster, already carrying WHICH value escaped
            # (round 21, H1) — a decline that named only the token would leave
            # a reader guessing which suffix of `-cf/etc/hostname` was refused.
            return None, ("operand %s resolves outside the repository root — "
                          "this tool reads the checkout, not the host"
                          % outside)
        # Round 22 (H20). The TRANSITIVE half of the same property: an
        # operand that resolves inside the root may still be a doorway out
        # of it. Checked here, on the post-expansion argv, for the round-14
        # reason every other containment rule is — a glob may expand onto a
        # directory that was never written in the document.
        reach = escaping_symlink_under(argv, repo)
        if reach is not None:
            operand, link = reach
            return None, ("operand `%s` is a directory containing `%s`, a "
                          "symlink that leaves the repository root — a "
                          "recursing command reads through it, and an "
                          "operand's own realpath cannot see that. This "
                          "tool reads the checkout, not the host"
                          % (operand, link))
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

# ONLY `csharp`/`cs` fences are examined for real declarations/references (see
# ANY_FENCE below for the OTHER tags — round 17, M14). Spec files are full of
# ASCII file-tree diagrams and untagged prose blocks in which
# `ShotExecutor.Execute()` is a sentence, not a member access; parsing those as
# code produced three false positives for every true one on first run, and a
# gate at that ratio gets switched off.
_CSHARP_TAGS = ("csharp", "cs")
# Every fenced block regardless of tag — round 17 (M14) needs this to COUNT a
# reference living inside an untagged fence as a named decline rather than a
# vanished one, without promoting untagged prose into the real scan above.
ANY_FENCE = re.compile(r"```([A-Za-z0-9_+-]*)\n(.*?)```", re.S)

# Round 17 (M10): DECL_CLASS matched the `class` keyword only. Measured over
# this tree's fenced corpus: 181 struct / 91 class / 65 enum / 14 interface
# declarations — so the `if typ in classes` gate downstream made every
# reference into a struct silently unexaminable, and root CLAUDE.md names
# struct-based architecture as the STANDING RULE, not the exception. Proven:
# the identical dangling reference was reported when its owning type read
# `class Foo` and missed when it read `readonly struct Foo`. Every C# type-
# declaration keyword this corpus actually uses is covered now; the variable
# keeps its old name because every caller still means "the set of types a
# `Type.Member` reference may resolve against" by it.
_TYPE_MODIFIER = (r"(?:public|internal|private|protected|static|readonly|"
                  r"sealed|abstract|partial|unsafe|ref|new)\s+")
DECL_CLASS = re.compile(
    r"\b(?:" + _TYPE_MODIFIER + r")*"
    r"(?:class|struct|interface|enum|record(?:\s+(?:class|struct))?)\s+"
    r"([A-Z][A-Za-z0-9_]*)")
# An enum MEMBER (`None`, `Yellow = 1`) is not a declaration DECL_MEMBER or
# DECL_LOOSE can see below — neither pattern's grammar allows for the one
# thing an enumerator omits: a preceding type. Harvested separately so that
# widening DECL_CLASS to `enum` does not turn every enum into a false-positive
# generator — without this, `Severity.Yellow` would dangle the instant
# `enum Severity { Yellow, Red }` came into scope, for every member, in every
# file that declares one.
DECL_ENUM = re.compile(
    r"\benum\s+[A-Z][A-Za-z0-9_]*\s*(?::\s*[A-Za-z_][\w.]*\s*)?\{(.*?)\}", re.S)
_ENUM_MEMBER_NAME = re.compile(r"([A-Za-z_][A-Za-z0-9_]*)")


def _enum_members(code):
    """Every identifier declared inside an `enum { ... }` body in `code`.

    Comments are stripped from each body BEFORE the comma-split, and must be —
    this corpus documents enum values with a `/// <summary>...</summary>` line
    per member, and that prose routinely contains its own commas ("Ball at
    rest on ground, no movement"). Splitting the raw body on `,` let a comma
    inside a doc-comment cut the member after it in half, burying the real
    name (`STATIONARY`) mid-fragment where the leading-identifier match could
    never reach it — proven: `BallStateType`'s six real members all came back
    missing, not extra, and the fragment blindly matched a stray English word
    (`no`) as a phantom member instead. This is not a case of a member being
    SKETCHED inside a comment (the class/DECL_LOOSE asymmetry is deliberate
    and stays); the member here is ordinary code, only its DELIMITER was
    corrupted by unrelated comment text."""
    members = set()
    for body in DECL_ENUM.findall(code):
        body = _strip_comments(body)
        for part in body.split(","):
            m = _ENUM_MEMBER_NAME.match(part.strip())
            if m:
                members.add(m.group(1))
    return members


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

# Every decline bucket CHECK 2 can land a reference in, in print order — round
# 17 (M14). Before this, CHECK 2 silently dropped a reference that was
# filename-shaped, whose type was not locally declared, whose fence carried no
# `csharp`/`cs` tag, or whose file declared no type at all: no bucket, no
# count, no line, while CHECK 1 spends forty header lines establishing that
# every decline it makes is counted and named. Same rule, second check.
FENCE_DECLINE_BUCKETS = (
    ("fence-untagged", "fence-untagged"),
    ("no-declared-type", "no-declared-type-in-file"),
    ("filename-shaped", "filename-shaped"),
    ("type-not-declared-here", "type-not-declared-here"),
)


# ---------------------------------------------------------------------------
# CHECK 2'S COVERAGE FLOOR (round 19, H17).
#
# MIN_EXECUTED_CLAIMS gates CHECK 1's recall and NOTHING gated CHECK 2's:
# blinding CHECK 2 completely still printed PASS and exited 0, the
# vacuous-pass failure class verbatim, with the whole suite green throughout.
# It is reachable without a code change too — `_CSHARP_TAGS` is ("csharp",
# "cs"), so a corpus that starts writing ```C# silently zeroes the whole
# check. Locked by CheckTwoCoverageFloorTests, both blindings.
#
# The floors are SHARES of what the corpus offers, not absolute counts, and
# that is a deliberate departure from how MIN_EXECUTED_CLAIMS is written. An
# absolute floor derived from this tree (101 fence files, 198 references
# examined-not-skipped) is a floor no smaller corpus can meet — including
# this file's own test fixtures, where the intended verdict is PASS on one
# spec file holding one fence. A share is scale-free: it is 1.00 on a
# one-fence fixture, 0.40 and 0.11 here, and 0.00 under either blinding
# above, which is the only property the gate needs.
#
# RE-DERIVED 2026-08-22 by `python3 tools/doc-claim-check.py --repo .`:
#     spec files with any fence   : 251
#     ... with a csharp/cs fence  : 101   -> share 0.40
#     references examined         : 1882
#     ... not skipped             : 198   -> share 0.11
# Each floor sits at roughly half its measurement, the same headroom rule
# MIN_EXECUTED_SLACK expresses: ordinary corpus drift must not turn a green
# tree red, a blinded check must. Raise them when the shares rise; lower one
# only with the reason stated in the commit message that lowers it.
#
# THE EXAMINED SHARE IS COMPUTED OVER EXAMINABLE REFERENCES — those found in
# a TAGGED fence — not over every reference found in any fence. An untagged
# fence is never given to the csharp scanner at all, so its references can
# never become "actually examined"; counting them meant adding an ASCII tree
# or a bash block to a spec file grew the denominator and drove the ratio
# toward the floor for free, at no cost in real coverage. Under the correct
# denominator the measurement above reads 198/983 = 0.20 against an unmoved
# floor of 0.05.
MIN_TYPED_FENCE_FILE_SHARE = 0.20
MIN_EXAMINED_SHARE = 0.05


def fence_coverage_shortfalls(coverage):
    """Every reason CHECK 2's recall is too low to call this run a verdict.

    Both floors are skipped when their DENOMINATOR is zero, and the third
    check is what stops that being a hole: a corpus with fenced spec files
    but not one examinable reference means the REFERENCE matcher itself has
    collapsed, which no share can express.

    Round 24 (L13): `coverage` gained a fifth field, `fence_untagged` — see
    CHECK 2'S COVERAGE FLOOR above for why the examined-share denominator
    excludes it."""
    fenced, typed, examined, skipped, fence_untagged = coverage
    out = []
    if fenced and typed / fenced < MIN_TYPED_FENCE_FILE_SHARE:
        out.append(
            "only %d of %d fenced spec file(s) yielded a csharp/cs fence "
            "(%.2f, floor %.2f) — CHECK 2 examined almost nothing, so its "
            "silence is not a verdict; see CHECK 2'S COVERAGE FLOOR"
            % (typed, fenced, typed / fenced, MIN_TYPED_FENCE_FILE_SHARE))
    examinable = examined - fence_untagged
    examinable_skipped = skipped - fence_untagged
    if fenced and not examinable:
        out.append(
            "%d fenced spec file(s) and NOT ONE examinable reference (every "
            "reference found sits in an untagged fence, which the csharp "
            "scanner never reaches) — the reference matcher has collapsed, "
            "so CHECK 2 found nothing because it looked at nothing"
            % fenced)
    elif (examinable
          and (examinable - examinable_skipped) / examinable
              < MIN_EXAMINED_SHARE):
        out.append(
            "%d of %d examinable reference(s) (excluding %d found in "
            "untagged fences, which are never examinable) were SKIPPED, "
            "leaving %.2f actually examined (floor %.2f) — a run that "
            "skips everything it could examine reports success for the "
            "same reason a blinded one does; see CHECK 2'S COVERAGE FLOOR"
            % (examinable_skipped, examinable, fence_untagged,
               (examinable - examinable_skipped) / examinable,
               MIN_EXAMINED_SHARE))
    return out


def _blank_matches(pattern, s):
    """Replace every match of `pattern` in `s` with same-length whitespace —
    a space per non-newline character, the newline itself kept — so every
    OTHER character's offset in `s` is unchanged.

    Round 17 (M11). The old comment-stripping step used a length-CHANGING
    substitution (`re.sub(r"/\\*.*?\\*/", " ", code)`), which is one of the two
    reasons a reference's position inside the stripped text could never be
    trusted back to a real file offset — the other being the block-join below.
    """
    def repl(m):
        return re.sub(r"[^\n]", " ", m.group(0))
    return pattern.sub(repl, s)


_USING_NAMESPACE_LINE = re.compile(r"^\s*(?:using|namespace)\b.*$", re.M)
_BLOCK_COMMENT = re.compile(r"/\*.*?\*/", re.S)
_LINE_COMMENT = re.compile(r"//[^\n]*")


def _strip_comments(code):
    return _blank_matches(_LINE_COMMENT, _blank_matches(_BLOCK_COMMENT, code))


def _join_with_offsets(blocks):
    """Join `[(body, file_offset)]` the way the old code joined fence bodies
    (one `\\n` between consecutive blocks) and return `(code, offset_map)`,
    where `offset_map` is `[(code_start, code_end, file_start)]`.

    Round 17 (M11). This table is the fix's load-bearing piece: every
    reference position computed downstream is a position IN `code`, and it is
    mapped back through this table to a real file offset — never re-derived
    by searching the raw file text for the reference's own spelling, which is
    what let the old code report the FIRST textual occurrence of
    `Type.Member` anywhere in the file (a prose sentence outside every fence,
    on a wrong line) instead of the occurrence that was actually flagged."""
    parts, offset_map, pos = [], [], 0
    for body, file_start in blocks:
        parts.append(body)
        offset_map.append((pos, pos + len(body), file_start))
        pos += len(body) + 1                    # +1 for the "\n" joiner
    return "\n".join(parts), offset_map


def _map_offset(offset_map, pos):
    """The file offset a position inside the joined `code` string corresponds
    to. Round 17 (M11): fails LOUD — the old code let `line` stay `None` on a
    failed lookup and printed the literal string 'None', which is silence
    wearing the shape of an answer."""
    for code_start, code_end, file_start in offset_map:
        if code_start <= pos <= code_end:
            return file_start + (pos - code_start)
    raise AssertionError(
        "dangling-identifier offset %d maps to no fence block — the fence "
        "join/offset bookkeeping has drifted from the code it describes"
        % pos)


def scan_fence_identifiers(repo):
    """Report Type.MEMBER references whose Type the file declares and whose
    MEMBER it does not.

    Returns `(findings, coverage)`, where coverage is
    `(fenced_files, typed_fence_files, examined, skipped, fence_untagged)` —
    round 19 (H17) needs the first four to gate this check's recall, and they
    were previously printed and thrown away; round 24 (L13) added the fifth
    so the examined-share floor can exclude references an untagged fence can
    never make examinable. (Round 17's L3 had corrected this docstring for
    promising a second return value the function did not have; it has one
    now, and it is used, not decorative.)

    Round 24 (L16): this used to take a `quiet` parameter kept only "for
    call-signature compatibility with the caller in scan()" — its own
    previous docstring's words — after round 20 (L7) made every print in
    this function unconditional and left the parameter with nothing to gate.
    A parameter a docstring itself says does nothing is worth dropping
    rather than carrying; the caller below no longer passes one."""
    findings = []
    fenced_files = 0
    files = []
    for pat in SURFACE_GLOBS:
        files.extend(sorted(repo.glob(pat)))
    files = _dedup_by_resolved_path(files)
    scanned = 0
    type_count = 0
    examined = 0
    decline = {bucket: 0 for bucket, _label in FENCE_DECLINE_BUCKETS}
    for path in files:
        text = path.read_text(encoding="utf-8", errors="replace")
        fences = list(ANY_FENCE.finditer(text))
        if not fences:
            continue
        fenced_files += 1
        untagged = [(m.group(2), m.start(2)) for m in fences
                    if m.group(1).lower() not in _CSHARP_TAGS]
        for body, _off in untagged:
            n = len(REFERENCE.findall(_strip_comments(body)))
            examined += n
            decline["fence-untagged"] += n
        tagged = [(m.group(2), m.start(2)) for m in fences
                  if m.group(1).lower() in _CSHARP_TAGS]
        if not tagged:
            continue
        scanned += 1
        code, offset_map = _join_with_offsets(tagged)
        # `using` / `namespace` lines are declarations of paths, never member
        # accesses; blanked (never deleted — deleting a line would break
        # offset_map's byte accounting, round 17 M11) before looking for
        # either declarations or references.
        code = _blank_matches(_USING_NAMESPACE_LINE, code)
        classes = set(DECL_CLASS.findall(code))
        type_count += len(classes)
        # Harvest declarations LOOSELY as well as strictly. Spec fences elide:
        # members appear without access modifiers (`int NextStaffId;`), inside
        # /* ... */ sketches of a class body, and as method signatures. A missed
        # declaration is a FALSE POSITIVE, and a checker that cries wolf is
        # ignored long before it is fixed — so over-harvest deliberately and
        # accept lower recall. Precision is the property that keeps it trusted.
        members = (set(DECL_MEMBER.findall(code)) | set(DECL_LOOSE.findall(code))
                   | classes | _enum_members(code))
        # Asymmetry, deliberate: comments COUNT as declarations (spec fences
        # sketch class bodies inside /* ... */) but NEVER as references — a
        # `/// Called by MatchSimulator.Update() at 60Hz` line is prose about
        # another spec's type, not a member access this file must satisfy.
        # Harvest from the full text above; scan for references here only.
        code_nc = _strip_comments(code)
        if not classes:
            n = len(REFERENCE.findall(code_nc))
            examined += n
            decline["no-declared-type"] += n
            continue
        for m in REFERENCE.finditer(code_nc):
            examined += 1
            typ, mem = m.group(1), m.group(2)
            if mem in FILE_EXTENSIONS:
                decline["filename-shaped"] += 1
                continue
            if typ not in classes:
                decline["type-not-declared-here"] += 1
                continue
            if mem not in members:
                # Locate the reference in the FILE via the offset map — round
                # 17 (M11) — never by re-searching the raw file text.
                line = (text.count("\n", 0, _map_offset(offset_map, m.start(1)))
                        + 1)
                # Round 17 (L3): this used to OR in a first disjunct
                # (`x.lower() == mem.replace("_", "").lower()`) that can never
                # fire alone — it is true only when `x` itself carries no
                # underscore, and in exactly that case `x.replace("_", "")`
                # is `x` unchanged, making the first disjunct textually
                # identical to the second. Dead code, deleted rather than
                # documented as dead.
                findings.append((str(path.relative_to(repo)), line, typ, mem,
                                 sorted(x for x in members
                                        if x.replace("_", "").lower()
                                        == mem.replace("_", "").lower())))
    # Round 20 (L7). The header and its two counts used to sit behind
    # `if not quiet`, in the very function whose own round-17 (M14) comment
    # two lines below states the rule this violated: "the counts every
    # verdict refers to are never suppressed" — and these two ARE the
    # coverage figures CHECK 2'S COVERAGE FLOOR gates on. `--quiet` still has
    # nothing left to suppress for CHECK 2: unlike CHECK 1's itemized
    # per-claim decline list, there is no per-item CHECK 2 listing gated
    # here at all — the dangling-identifier findings themselves print
    # unconditionally, in scan(), regardless of `quiet`. So the whole block
    # is unconditional now, matching CHECK 1's treatment exactly rather than
    # only half of it.
    print("\ndangling-identifier check — references inside spec code fences")
    print("  spec files with code fences   : %d" % scanned)
    print("  declared types considered      : %d" % type_count)
    print("  (a reference is reported only when its TYPE is declared in the same"
          " file and its MEMBER is not — cross-file resolution is deliberately"
          " not attempted)")
    # Round 17 (M14): always printed, --quiet included — the same rule L4
    # applies to CHECK 1's own counts, and CHECK 2's coverage line did not
    # exist in any form before this round.
    print("  references examined            : %d  (%d skipped)"
          % (examined, sum(decline.values())))
    print("  references SKIPPED (each named) : %s"
          % " / ".join("%d %s" % (decline[b], label)
                       for b, label in FENCE_DECLINE_BUCKETS))
    skipped = sum(decline.values())
    # Round 24 (L13): the printed floor now matches what actually gates —
    # examinable references (found in a tagged fence) — rather than the raw
    # `examined` total, which untagged fences (ASCII trees, bash blocks)
    # inflate for free without adding real coverage. See CHECK 2'S COVERAGE
    # FLOOR beside MIN_EXAMINED_SHARE.
    examinable = examined - decline["fence-untagged"]
    print("  coverage floors                : %d/%d fenced files typed "
          "(floor %.2f), %d/%d examinable reference(s) actually examined "
          "(floor %.2f; excludes %d in untagged fences)"
          % (scanned, fenced_files, MIN_TYPED_FENCE_FILE_SHARE,
             examinable - (skipped - decline["fence-untagged"]), examinable,
             MIN_EXAMINED_SHARE, decline["fence-untagged"]))
    return findings, (fenced_files, scanned, examined, skipped,
                      decline["fence-untagged"])


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


def decline_bucket_order():
    """Every bucket a decline can land in, in print order — DECLINE_BUCKETS
    plus each registered ANSWER KIND's own unreadable-output bucket.

    Round 19 (H18). The SEAM 1 banner promised "add a class here, name it on
    a claim shape, and nothing in scan() changes", and executing that promise
    literally CRASHED: `declined` was built from DECLINE_BUCKETS alone, a
    module table the banner never mentions, so a new kind whose bucket was
    not already listed raised `KeyError` out of scan() and exited 1 — the
    code meaning "a document is wrong", which a previous round spent a whole
    fix separating from "this tool broke". Derived here instead, so
    registering a kind cannot forget to register its bucket."""
    order = list(DECLINE_BUCKETS)
    known = {bucket for bucket, _label in order}
    for kind in ANSWER_KINDS:
        if kind.bucket not in known:
            known.add(kind.bucket)
            order.append((kind.bucket, kind.bucket))
    return tuple(order)


def record_decline(declined, order, bucket):
    """Count one decline, ADDING the bucket to `order` if it is new.

    The derivation above covers every bucket an answer kind DECLARES; an
    `answer.parse` may still return a second bucket of its own (the live
    `approximate-or-range` does exactly that). A bucket that reaches this
    function unlisted is therefore counted and printed rather than crashing —
    the same rule as everywhere else in this file: never let an unforeseen
    decline become invisible, and never let it become a traceback."""
    if bucket not in declined:
        declined[bucket] = 0
        order.append((bucket, bucket))
    declined[bucket] += 1


def document_relative_operand(rel, argv, repo):
    """The first operand in `argv` that is ambiguous between the repo root
    and the QUOTING DOCUMENT's own directory, or None.

    Round 16 (M7). Every command runs from the repo root, with nothing
    relating an operand to `rel`'s own directory — fine when `rel` IS the
    repo root (CLAUDE.md, README.md: document-relative and root-relative are
    the same thing there), but not when `rel` sits under docs/specs/. A bare
    filename quoted there is ambiguous: it could mean "relative to the repo
    root" or "relative to the file quoting it", and this tool cannot know
    which. Proven both directions on a fixture: a spec quoting
    `grep -c TODO README.md` about its own folder's README FAILED when the
    two files' counts differed (comparing against the WRONG file) and PASSED
    SILENTLY when the root file's count happened to match (still the wrong
    file — the claim was never actually checked). "A checker must never
    guess which of two files the author meant" — so this declines whenever a
    document-relative candidate of the same bare name exists, independent of
    whether a root-relative one also does, and leaves every other bare
    operand alone: `wc -l CLAUDE.md` quoted from a nested surface with no
    sibling `CLAUDE.md` of its own has no candidate to collide with and is
    not flagged.

    Live on this tree, not merely hypothetical: docs/specs/code-standards/
    section-3.md quotes `grep -n '...' section-2.md`, the bare name of its
    OWN sibling file, in exactly this idiom."""
    doc_dir = pathlib.PurePosixPath(rel).parent
    if str(doc_dir) in ("", "."):
        return None                       # the document IS at the repo root
    for a in argv[1:]:
        if a.startswith("-") or "/" in a:
            continue
        if (repo / doc_dir / a).is_file():
            return a
    return None


def would_gate(claim, regions):
    """True when a MISMATCH on this claim would be REPORTED rather than
    excused — i.e. when executing it is drift-catching coverage.

    Round 23 (H23). The excusal test and the LIVE counter had drifted into
    two different questions. Excusing a mismatch requires BOTH that the claim
    sits inside a dated-record span AND that the region's own pierce fails
    (a currency assertion outside the ERR log; the resolved/dated rule inside
    it) — while `scan()` counted a claim as live on POSITION ALONE. So a
    currency-asserted claim inside a record, which gates perfectly well, was
    counted non-live, and one such claim carrying a wrong value produced
    three falsehoods in a single run: `of which LIVE (can gate): 0`,
    `0 mismatch(es) EXCUSED`, and a FAIL block reporting the drift — followed
    by "only 0 LIVE claim(s) executed ... so this run could not have caught
    drift in any document" and exit 2, which OUTRANKS 1. A genuine document
    defect was demoted to a tooling error, by the very mechanism this file
    keeps for honesty.

    One helper, called by both sites, so the two can no longer disagree — the
    same rule this file already applies to `command_head` (round 20, M15) and
    to the imported frozen-history definition: where two answers must agree,
    there is one computation."""
    if not any(a <= claim.start < b for a, b in regions):
        return True
    # Round 16 (M12): the ERR log gets its OWN, stricter rule — see the
    # section banner above err_log_excused(). Every other dated-record
    # region keeps the proximity rule this file has used since round 13.
    if claim.rel in DCC.LOG_BODY_FILES:
        return not err_log_excused(claim.text, claim.start, claim.end)
    return currency_asserted(claim.text, claim.start, claim.end)


def check_claim(claim, repo, regions):
    """Validate, run and compare ONE claim. Returns (outcome, payload):

        ("ignored",    None)            not a command at all — no claim here
        ("declined",   (bucket, why))   counted and named; UNVERIFIED
        ("reproduced", (stated, got))   the document is right
        ("excused",    (stated, got))   wrong, but `would_gate()` is False —
                                        inside a dated record whose own
                                        pierce did not fire
        ("mismatch",   (stated, got))   wrong, and gating

    An "ignored" claim reserves NOTHING (round 23, H25): scan() subtracts
    these command spans from `bound` so the census can still name them.

    Knows nothing about printing, counters or exit codes; scan() owns those."""
    answer = claim.answer
    head = claim.head
    if head is None:
        # Round 17 (L2) introduced this branch and round 20 (M16) narrowed
        # it, on evidence the L2 reasoning got backwards. `claim.cmd` does
        # not tokenize AT ALL (an unterminated quote, a trailing backslash)
        # — the same failure parse_pipeline names "does not tokenize
        # (unterminated quote or trailing \\)" for a claim whose head WAS
        # already recognised. L2 declined it UNCONDITIONALLY, arguing that a
        # claim SHAPE binding this span had already answered whether it is a
        # command. It had not: a shape only recognises "value, then a
        # command-looking backticked span", and markdown prose caught
        # between two code spans binds that shape just as readily as a real
        # command does. Measured on this tree: 9 of the 10 `unsafe`-bucket
        # entries this branch produced were exactly that — ordinary prose
        # ("...**(2)** §3.1's age-derivation ", "...this branch's seven rows
        # renumbered ") whose stray quote or trailing backslash merely broke
        # the tokenizer, never a command anyone wrote. Gated now behind the
        # same command_shaped() test the sibling not-allow-listed branch
        # below already applies — using the naive split as the shape probe,
        # since tokenize() has nothing better to offer a string it could not
        # parse at all. A genuinely command-shaped span that still fails to
        # tokenize (an unterminated quote INSIDE a real command) keeps the
        # named `unsafe` decline; ordinary prose is `ignored`, as it always
        # was for every other shape-bound non-command on this tree.
        naive_head = claim.cmd.split()[0] if claim.cmd.split() else ""
        if command_shaped(claim.cmd, naive_head):
            return "declined", ("unsafe",
                                "does not tokenize (unterminated quote or "
                                "trailing \\)")
        return "ignored", None
    if head not in ALLOWED_CMDS:
        # This path was once the tool's THIRD decline route and the only
        # SILENT one, while its header and its file-manifest row both
        # published "every declined claim is counted AND named". Most of
        # what lands here is not a command at all — the shapes also match a
        # backticked IDENTIFIER before an arrow (`SEASON_SAVE_FORMAT_VERSION`
        # **5 → 6**) — so counting all of it would drown the real signal.
        # Only genuinely command-SHAPED text is counted and named.
        #
        # RE-DERIVED 2026-08-22, and stated as TWO figures because a single
        # one is ambiguous between them and transplanting the wrong one is a
        # defect this file has already filed: of 776 spans a claim SHAPE
        # bound on this tree, 735 reach this branch (head not allow-listed)
        # and 489 of those are a bare identifier with no argument at all.
        # The figure that stood here was "~1,100", which is neither. Both are
        # measured by walking `collect_claims` over `collect_surfaces` — do
        # not copy either without re-running it, and do not carry either into
        # the header's census note, which counts a DIFFERENT population
        # (spans no shape bound at all).
        # FORBIDDEN gates it too, so a backticked EXPRESSION that merely looks
        # path-shaped (`permille/1000f > 0.6f`, live in #33's appendices) is
        # not announced as a rejected binary. It was never a command; naming it
        # would be the mirror of the noise command_shaped exists to suppress.
        if command_shaped(claim.cmd, head) and not FORBIDDEN.search(claim.cmd):
            return "declined", ("unlisted-binary",
                                "`%s` is not an allow-listed read-only binary"
                                % head)
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
    for seg in segments:
        ambiguous = document_relative_operand(claim.rel, seg, repo)
        if ambiguous is not None:
            return "declined", ("did-not-run",
                                "operand `%s` is ambiguous between the repo "
                                "root and %s's own directory (a file of that "
                                "name exists there) — a checker must never "
                                "guess which one the document meant"
                                % (ambiguous, claim.rel))
    if not self_contained(segments, repo):
        return "declined", ("not-self-contained",
                            "no argv token names a file that exists on disk "
                            "— this segment would read from an empty stdin, "
                            "not from the quoted text")
    out, why = run_pipeline(segments, str(repo))
    if out is None:
        return "declined", ("did-not-run", why)
    got = answer.read(out)
    if got is None:
        return "declined", (answer.bucket, answer.unreadable)
    if answer.matches(stated, got):
        return "reproduced", (stated, got)
    # Round 23 (H23): the excusability test is `would_gate()` above, shared
    # with scan()'s LIVE counter so "can this gate?" has ONE answer.
    if not would_gate(claim, regions):
        return "excused", (stated, got)
    return "mismatch", (stated, got)


def _dedup_by_resolved_path(items):
    """De-duplicate `items` (each a (rel, Path) pair or a bare Path) by
    REALPATH, keeping first-seen order.

    Round 16 (M6). Deleting the one redundant glob fixes today's overlap, but
    the glob SET is a decision, not a proof of disjointness — SURFACE_GLOBS
    gaining a second overlapping entry later would silently reintroduce the
    double-scan/double-report defect. Comparing resolved paths, rather than
    the (rel, Path) tuple or the Path's own string form, is what makes this
    robust to two different glob patterns naming the same file two different
    ways."""
    seen = set()
    out = []
    for item in items:
        p = item[1] if isinstance(item, tuple) else item
        real = str(p.resolve())
        if real in seen:
            continue
        seen.add(real)
        out.append(item)
    return out


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
    return _dedup_by_resolved_path(files), missing


def scan(repo, quiet=False):
    files, missing = collect_surfaces(repo)

    checked = 0
    checked_commands = set()      # ... and how many DISTINCT commands they are
                                   # (round 20, M20 — see MIN_EXECUTED_CLAIMS)
    live = 0                      # executed claims OUTSIDE every record span
    live_commands = set()         # ... and how many DISTINCT commands they are
    bucket_order = list(decline_bucket_order())
    declined = {bucket: 0 for bucket, _label in bucket_order}
    declined_list = []
    findings = []
    excused = []
    region_coverage = []          # (rel, covered_chars, total_chars) — M12

    for rel, path in files:
        text = path.read_text(encoding="utf-8", errors="replace")
        regions = dated_record_regions(rel, text)
        # M12's coverage figure is about the named tracking surfaces the
        # excusal rule was written against (spec-error-log.md, CHANGELOG.md,
        # ...) — every glob-matched spec file also carries a Version History
        # table and so a small frozen-chain span of its own, which would
        # otherwise turn this into a ~600-line wall of near-zero percentages
        # that answers a question nobody asked.
        if regions and text and rel in SURFACES:
            # UNION, not a sum (round 19): since H13 the spans can overlap —
            # a Version History section inside spec-error-log.md's log body
            # is covered by both rules — and adding them printed more frozen
            # bytes than the file has.
            covered, reach = 0, 0
            for a, b in sorted((max(0, a), min(b, len(text)))
                               for a, b in regions):
                if b > reach:
                    covered += b - max(a, reach)
                    reach = b
            region_coverage.append((rel, covered, len(text)))
        claims = collect_claims(rel, text)
        ignored_spans = set()         # round 23 (H25) — see `bound` below
        for claim in claims:
            outcome, payload = check_claim(claim, repo, regions)
            if outcome == "ignored":
                ignored_spans.add(claim.cmd_start)
                continue
            if outcome == "declined":
                bucket, why = payload
                record_decline(declined, bucket_order, bucket)
                declined_list.append((rel, claim.line, claim.cmd, why))
                continue
            checked += 1
            checked_commands.add(claim.cmd)
            # Round 19 (H13). WHERE a claim sits decides what executing it is
            # worth. Inside a dated record the command still runs and a
            # mismatch is still printed, but it can never gate — so it is not
            # drift-catching coverage, and counting it as such is how a
            # headline of 6 came to describe a live coverage of 1.
            if would_gate(claim, regions):
                live += 1
                live_commands.add(claim.cmd)
            stated, got = payload
            if outcome == "excused":
                excused.append((rel, claim.line, claim.cmd, stated, got))
            elif outcome == "mismatch":
                findings.append((rel, claim.line, claim.cmd,
                                 claim.answer.describe(stated, got)))
        # The census of shapes NOBODY recognised — H7's self-reporting half.
        #
        # ROUND 23 (H25). A span `check_claim` IGNORED must not reserve
        # itself. `check_claim` returns ("ignored", None) on two paths — the
        # text is not command-shaped, or it is but FORBIDDEN matches — and
        # this set used to hold EVERY bound `cmd_start`, so such a span
        # reached no bucket, no count and no line: not executed, not
        # declined, and invisible to the census that exists to name exactly
        # what no shape could read. Measured: `python3 -c '...'`,
        # `curl http://x > out.txt`, `make build && echo 7`,
        # `dotnet test ... 2>&1` and a bare `tools/count-supplements.sh` all
        # landed nowhere, while the same `dotnet` claim WITHOUT the `2>&1`
        # was named — so ADDING a redirection, which every real
        # how-I-measured-it line carries, moved a claim from a named decline
        # to complete invisibility. That falsifies this file's central
        # contract ("every claim this tool declines to check is COUNTED AND
        # NAMED"), and the census already discriminates correctly: with this
        # set emptied it names all of them.
        #
        # The rule mirrors `collect_claims`'s own — a match a shape REJECTS
        # does not reserve its command span — one layer down: a claim the
        # CHECKER ignored does not reserve it either.
        bound = {c.cmd_start for c in claims} - ignored_spans
        for line, cmd in unrecognised_spans(text, bound):
            record_decline(declined, bucket_order, "unrecognised-shape")
            declined_list.append(
                (rel, line, cmd, unrecognised_span_reason(cmd)))

    # Round 17 (L4). --quiet used to gate this ENTIRE block, coverage counts
    # included — so a quiet run's PASS line ("...with the coverage stated
    # above") cited a statement that was never printed: nothing was "stated
    # above" under --quiet, only the excusal/region-coverage lines that
    # happen to sit outside this gate. --quiet now suppresses only the
    # ITEMIZED per-claim decline list (the `- rel:line cmd [why]` lines,
    # potentially hundreds of them) — the counts every verdict downstream
    # refers to, and the verdict itself, are never suppressed. MISSING
    # SURFACE and the coverage-floor error already print unconditionally too
    # (below); this makes the summary counts consistent with that, and with
    # what CHECK 2's own coverage line (M14) already does.
    print("doc-claim-check — executing the verification commands the documents quote")
    print("  surfaces scanned              : %d" % len(files))
    # Round 18. Printed unconditionally, --quiet included: whether the OS is
    # enforcing the write ceiling is a property of what this run PROVED, and
    # a safety property that degrades on an unsupported platform without
    # saying so is the silent-skip failure this file's whole decline contract
    # exists to deny itself.
    print("  child resource limits         : %s" % child_limit_summary())
    print("  claim shapes recognised       : %d (%s)"
          % (len(CLAIM_SHAPES), ", ".join(s.name for s in CLAIM_SHAPES)))
    print("  answer kinds recognised       : %d (%s)"
          % (len(ANSWER_KINDS), ", ".join(a.name for a in ANSWER_KINDS)))
    # Round 20 (M20). The floor is on DISTINCT COMMANDS, not instances — see
    # MIN_EXECUTED_CLAIMS's own note for why: an instance count cannot tell
    # "N drift-capable claims" from "one command quoted N times", and half of
    # today's six instances are exactly that (one revision-pinned command
    # quoted in three files). Both figures are printed; the floor is on the
    # one that cannot be inflated by repetition.
    print("  claims executed and compared  : %d  (%d distinct command(s), "
          "floor %d)" % (checked, len(checked_commands), MIN_EXECUTED_CLAIMS))
    # Round 19 (H13). The headline above is NOT this tool's drift-catching
    # coverage and must never be read as it: a claim inside a dated record is
    # executed and its mismatch printed, but it is EXCUSED, so it cannot fail
    # CI and cannot catch drift. Both figures are printed, on adjacent lines,
    # with the one that actually gates carrying the floor.
    # The wording names the TEST, not a position: `would_gate()` asks whether
    # the claim sits in a dated record AND that region's own pierce fails —
    # the currency-word rule everywhere, the resolved-or-dated rule inside
    # LOG_BODY_FILES. Saying "carries no currency assertion" would be false
    # of the ERR-log claims, which are excused under the stricter rule.
    print("  ... of which LIVE (can gate)  : %d  (floor %d) — %d distinct "
          "command(s); the other %d sit inside a dated record WHOSE OWN "
          "PIERCE DOES NOT FIRE (a currency word, or — in the ERR log — a "
          "resolved or dated entry), so a mismatch there is EXCUSED — "
          "executed coverage, but NOT drift-catching coverage"
          % (live, MIN_LIVE_CLAIMS, len(live_commands), checked - live))
    print("  claims DECLINED (each named)   : %s"
          % " / ".join("%d %s" % (declined[b], label)
                       for b, label in bucket_order))
    if not quiet:
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

    # Round 16 (M12): "how much of each surface is a dated record" was never
    # printed, so a reader had no way to see that the gate was effectively
    # switched off across most of spec-error-log.md and CHANGELOG.md — the
    # excusal rule was invisible exactly where it mattered most. Printed
    # always, like the excusal count above, and only for surfaces that have
    # ANY dated-record span (most spec files have none and would be pure
    # noise here).
    if region_coverage:
        print("  dated-record region coverage (inside a span, a mismatch is excused"
              " under the rule stated above rather than reported):")
        for rel, covered, total in sorted(region_coverage,
                                          key=lambda t: -(t[1] / t[2])):
            print("      - %-40s %5.1f%%  (%d/%d bytes)"
                  % (rel, 100.0 * covered / total, covered, total))

    if findings:
        print("\nFAIL — %d stated value(s) the command does not reproduce:" % len(findings))
        for rel, line, cmd, what in findings:
            print("  %s:%d" % (rel, line))
            print("      command : %s" % cmd)
            print("      %s" % what)

    # Round 20 (L8). `_map_offset`'s fail-loud AssertionError used to be
    # uncaught here, producing a raw traceback AFTER CHECK 1 had already
    # printed its own counts and verdict text above — a partial report with
    # no overall verdict, at the very exit code (an uncaught exception exits
    # 1) round 17 (M14) spent a whole fix separating from "a document is
    # wrong". Believed unreachable today (a consistency defect rather than a
    # live bug), so this is prophylactic: caught here and routed through
    # `blocked`, so a future drift between the fence-join bookkeeping and
    # the code it describes exits 2 — "this tool could not do its job" — with
    # a named reason, never a bare stack trace.
    try:
        dangling, fence_coverage = scan_fence_identifiers(repo)
    except AssertionError as exc:
        dangling = []
        fence_blocked = [
            "CHECK 2 (dangling-identifier scan) could not complete: %s — "
            "this run's dangling-identifier result is not a verdict on any "
            "document" % exc]
    else:
        fence_blocked = fence_coverage_shortfalls(fence_coverage)
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
    # Round 20 (M20): the gate is on DISTINCT COMMANDS — `checked` alone
    # would let one revision-pinned command, quoted in arbitrarily many
    # files, inflate a floor meant to catch a matcher regression.
    if len(checked_commands) < MIN_EXECUTED_CLAIMS:
        blocked.append(
            "only %d distinct command(s) executed and compared (%d "
            "instance(s)), below the floor of %d — see the COVERAGE FLOOR "
            "note beside SURFACE_GLOBS for how the floor is re-derived and "
            "when it may be changed"
            % (len(checked_commands), checked, MIN_EXECUTED_CLAIMS))
    # Round 19 (H13): the floor that measures reach rather than activity.
    # Round 24 (L12): gated on `live` (INSTANCES) rather than
    # `len(live_commands)` (DISTINCT commands) — exactly the hole
    # MIN_EXECUTED_CLAIMS was moved off of at round 20 (M20), reopened here
    # one counter over. The two floors now share the same shape: an instance
    # count cannot tell "N drift-capable claims" from "one command quoted N
    # times", and the LIVE side is where a REVISION-PINNED command (see THE
    # LIVE FLOOR banner beside MIN_LIVE_CLAIMS) makes that concretely worse
    # rather than merely theoretical — its residual is recorded there, not
    # fixed by this change: gating on distinct commands closes the
    # repetition half of the hole (the property this fix actually shares
    # with M20) and leaves the revision-pin half exactly where that banner
    # already states it, honestly, as a deferred owner-level call.
    if len(live_commands) < MIN_LIVE_CLAIMS:
        blocked.append(
            "only %d distinct LIVE command(s) executed (%d LIVE claim "
            "instance(s)) — below the floor of %d. Every other executed "
            "claim sits inside a dated record whose own pierce does not "
            "fire, so a mismatch there is excused and this run could not "
            "have caught drift in any document. See THE LIVE FLOOR beside "
            "MIN_LIVE_CLAIMS"
            % (len(live_commands), live, MIN_LIVE_CLAIMS))
    blocked.extend(fence_blocked)
    if blocked:
        print("\nERROR — this run could not verify what it is supposed to "
              "verify, so its result is not a verdict on any document:")
        for why in blocked:
            print("  * %s" % why)
        return 2

    # Round 17 (M14): CHECK 1 and CHECK 2 used to fuse into the SAME exit code
    # (1), so CI reported one red step for two unrelated defect classes and a
    # reader had to open the log to learn which check actually failed. CHECK 1
    # keeps 1 — it is the documented, long-standing meaning, and it wins when
    # BOTH checks fail in the same run, exactly as before. A run where CHECK 2
    # is the ONLY failure is now machine-attributable as its own code, 3,
    # distinct from both "a document is wrong" (1) and "this tool could not do
    # its job" (2). The printed FAIL blocks above already say which check
    # failed either way; this is about the exit code alone.
    if findings:
        return 1
    if dangling:
        return 3

    # Round 24 (M32). This line used to print UNCONDITIONALLY on exit 0,
    # including a run that printed "N mismatch(es) EXCUSED ... [record says
    # 99; command now returns 3]" five lines above it — the single
    # most-read line of output stating "every executable claim reproduced
    # its stated value" about a run that had just shown one that did not.
    # An excusal is not a pass on that claim; it is a deliberate choice not
    # to gate on it. Named here so the headline can never contradict its own
    # printed excusal block.
    if excused:
        print("\nPASS — every claim OUTSIDE a dated record reproduced its stated"
              " value, and no spec code fence references an identifier its own"
              " file does not declare (with the coverage stated above). %d"
              " mismatch(es) were EXCUSED as dated records rather than verified"
              " — see the EXCUSED list above."
              % len(excused))
    else:
        print("\nPASS — every executable claim reproduced its stated value, and no"
              " spec code fence references an identifier its own file does not declare"
              " (with the coverage stated above).")
    return 0


def main():
    ap = argparse.ArgumentParser(
        description="Execute the verification commands quoted in this repo's "
                    "documents and diff the stated value against the real one.")
    # Round 17 (L3): this was the only checker in tools/ requiring --repo —
    # doc-consistency-check.py and recurring-defect-lint.py both default it
    # to '.'. Aligned; still overridable, so `--repo .` (as ci.yml and this
    # file's own callers already pass) is unchanged.
    ap.add_argument("--repo", default=".", help="repository root (default: .)")
    ap.add_argument("--quiet", action="store_true")
    args = ap.parse_args()
    repo = pathlib.Path(args.repo).resolve()
    if not (repo / "CLAUDE.md").is_file():
        print("not a Tactical Director repo root: %s" % repo, file=sys.stderr)
        return 2
    # Round 17 (M13). Deferred here — after argparse (so `--help` never
    # touches it) and before scan() prints anything (so a broken import is
    # reported whole, not mid-scan) — see _ensure_consistency_module()'s own
    # docstring for what this replaces.
    dcc_error = _ensure_consistency_module()
    if dcc_error is not None:
        print("\nERROR — this run could not verify what it is supposed to "
              "verify, so its result is not a verdict on any document:")
        print("  * %s" % dcc_error)
        return 2
    # Round 24 (L14). No boundary sat here before this: an unforeseen error
    # anywhere inside scan() — a `read_text()` on a path that turns out to be
    # a directory (a surface glob can match one), a regex or bookkeeping
    # defect this file has not yet found — propagated all the way to
    # `sys.exit(main())` uncaught, and Python's default handler exits 1: the
    # exact code CHECK 1 uses for "a stated value did not reproduce", after a
    # partial report had already printed above it. That is the same
    # confusion round 17 (M14) spent a whole fix separating for the
    # documented FAIL/ERROR distinction, reopened here for the UNDOCUMENTED
    # failure. Routed through the same named ERROR block as every other
    # "this tool could not do its job" case, returning 2 — never a bare
    # traceback masquerading as a document defect.
    try:
        return scan(repo, args.quiet)
    except Exception as exc:
        print("\nERROR — this run could not verify what it is supposed to "
              "verify, so its result is not a verdict on any document:")
        print("  * an unforeseen error interrupted the scan (%s: %s) — "
              "whatever printed above, if anything, is not a verdict on any "
              "document" % (type(exc).__name__, exc))
        return 2


if __name__ == "__main__":
    sys.exit(main())

# HISTORY — NOT KEPT HERE.
#
# A 1,589-line Version History table used to sit at this point: seventeen rows
# (v1.0 - v1.16), one per adversarial-review round, each reproducing that
# round's findings in prose. It was 26% of the file, nothing verified it, and
# a single reviewer pass found six countable falsehoods in this file's
# explanatory prose while that table was the largest single body of it. It was
# DELETED rather than corrected, on this repo's own stated convention for spec
# folders — "No version suffixes in filenames. Git tracks history." — applied
# to the one file that had opted out of it.
#
# Where the history now lives, in the order worth consulting:
#   * `git log -p -- tools/doc-claim-check.py` — the rows described commits;
#     the commits are the rows, and they cannot drift from what they describe.
#   * `docs/tracking/CHANGELOG.md` — the round-by-round narrative, which is
#     that file's job rather than this one's.
#   * `tools/tests/test_doc_claim_check.py` — every exploit a row recited is a
#     named regression test there. A test is the form of history that fails
#     when it stops being true.
#
# The reasoning a row carried that is NOT recoverable from a diff — why a rule
# has the shape it has, and what a safety property does not cover — was kept,
# in the section that owns the rule rather than in a table at the bottom.
