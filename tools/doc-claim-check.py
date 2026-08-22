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
# Round 17 (L3): this was the only checker in tools/ without the File/
# Modified/Author fields its siblings (doc-consistency-check.py,
# recurring-defect-lint.py) both carry — added here to match, not restyled.
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
#             in the same run; see the exit-code table below (round 17, M14).
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
# next defect hides, so every claim this tool declines to check is COUNTED AND
# NAMED, and ITEMIZED unless `--quiet` (round 20, L9 — the `--quiet` fix
# deliberately made the flag suppress the itemized per-claim decline LIST while
# keeping every count; this sentence was not updated at the time, and CI runs
# without `--quiet` so the gap was documentation-only, but it is the sentence
# this whole tool's honesty rests on).
#
# It recognises FOUR claim SHAPES, listed in CLAIM_SHAPES and printed by every
# run so the reader never has to trust this comment:
#   1. command, then the value       "`cmd` → 18", "`cmd` returned 218"
#      Its two routes have DIFFERENT gap grammars (round 23, H21): an arrow
#      cannot belong to a neighbouring clause, a VERB can, so the verb route
#      admits only whitespace, closing markup and adverbs between the command
#      and its verb, and only whitespace and markup between the verb and the
#      value. Ordinary English was binding integers out of unrelated clauses
#      — "(`cmd`) was re-run and found 2 orphans" reported a correct document
#      wrong — and the census could not see it, because a shape had bound it.
#   2. command, colon, value         "`cmd`: **0**"
#   3. value, then the command in parentheses    "8 scripts (`ls tools/*.py`)"
#   4. value, an attribution clause, the command
#                                    "60 files — re-derived by `ls … \| wc -l`"
#                                    "35 assemblies via `ls -d src/*/ \| wc -l`"
#      Since round 23 (H24) it also reads `with`, the bare stem `re-derive`,
#      and a wrap prefix before the command, because this repo hard-wraps
#      mid-sentence inside an ASCII tree diagram and a live claim there was
#      bound by nothing.
#
# THE RESIDUAL BLIND SPOT IS THE POINT OF THIS PARAGRAPH, and it is no longer
# described by naming instances someone happened to notice. Rounds 9 and 12
# each published a blind spot as prose, and the prose then drifted: round 12
# added shape 3 and left this section saying it "is not matched at all, and so
# is not counted among the declines either", which the very next run refuted by
# printing that exact claim in the declined list. So the blind spot is DERIVED
# now, not written down: every backticked span that reads as a command, has an
# integer near it — on its own line, or on the line either side when its own
# line carries no digit at all (round 23, H24: this repo hard-wraps
# mid-sentence, and README.md:930-931 was a LIVE drift-capable claim split
# across a wrap that no shape bound and the census could not see) — and binds
# to NO shape is counted and named in the `unrecognised-shape` decline
# bucket. Read that bucket, not this comment, for what the tool cannot see —
# and if a claim shape is worth learning, it will be sitting in that list
# under its own file and line.
#
# TWO THINGS STILL REACH NO BUCKET, named rather than implied, because every
# previous version of this paragraph was falsified by the next round:
#   * a backticked span whose head is an argument-less path (a bare
#     `tools/count-supplements.sh`) is refused by the census's own
#     command-shape test, which requires "a binary-shaped head plus an
#     argument only a command takes" so that the backticked IDENTIFIERS this
#     corpus is full of do not drown the bucket — measured 2026-08-22, that
#     predicate refuses 53,804 spans and admits 195, which is the ratio the
#     bucket would otherwise carry. (No figure is copied here from the
#     ~1,100 in check_claim's own note below: that one counts a DIFFERENT
#     population — identifiers a claim SHAPE bound and check_claim then had
#     to dispose of — and round 20's M19 is exactly the defect of moving a
#     number between the two.) That predicate cannot tell the two apart, and
#     round 23 (H25) verified the span stays invisible even with the bound
#     set emptied — so it is a limit of the SHAPE TEST, not of the
#     reservation H25 fixed.
#   * an integer further from the span than UNRECOGNISED_RADIUS, or two
#     wrapped lines away.
#
# Round 19 (H15): that sentence was FALSE for one whole class until this
# round, and it was false in the way this file keeps finding — "reads as a
# command" was a HAND-WRITTEN LIST of binary names. A claim quoting a binary
# on neither curated list (`comm`, `tac`, `pcregrep`, `tree`, `nl`, `du`) was
# dropped by check_claim with no bucket, no count and no line, and could not
# reach the census either, because the claim shape HAD bound the span. The
# test is a positive SHAPE now — a binary-shaped head plus an argument only a
# command takes — so a binary nobody here has heard of is named rather than
# vanishing. Latent when found (0 live instances), which makes it a false
# statement about coverage rather than a missed defect; it was the statement
# that had to change.
#
# The same rule governs the answer side. ANSWER_KINDS holds exactly one entry
# — DERIVED from the shapes that name it, never written out (round 19, H18) —
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
# `uniq IN OUT`. They carry READ hatches too, and those are the harder half,
# because nothing in rule 6 below bounds a read: `awk 'BEGIN{ARGV[ARGC++]=...}'`
# opens any file as main input, `grep -R` / `find -L` / `rg --follow` /
# `diff -r` walk out through a symlink, and `wc --files0-from=F` takes the
# paths it opens from F's bytes (all round 22).
#
# Rounds 9, 14 and 15 each falsified this section's then-current safety claim by
# reproduction, so state the recurring root error before the rules: **an escape
# hatch enumerated by name, on an argument that is itself a language, is a list
# of the hatches someone happened to think of.** Round 9 named `system` and
# `getline` for awk; round 15 executed `awk 'BEGIN{print "touch X" | "sh"}'`,
# which uses neither. Round 14 put it as "the validation ran on a SHAPE, not on
# the argv that actually executes"; round 15 adds its sibling — the validation
# ran on the SPELLING, not on the option (`sort -o` was denied while
# `sort -oFILE` wrote the file). Round 18 makes it FOUR consecutive rounds of
# the same class, each time inside the previous round's fix: `--output` was
# denied while `--o=`, `--out=` and `--compress-progr=` were not, because
# `getopt_long` accepts any unambiguous PREFIX of a long name and this file
# compared long options by exact string (H12). The same round found path
# confinement skipping every token beginning with `-`, so an ATTACHED option
# value (`grep -f/etc/hostname`, `--from-file=/tmp/secret`) never reached the
# containment test at all and its integer was compared (H14) — the separated
# spellings of both were correctly declined, which is the respelling shape
# again, one layer over.
#
# Round 21 makes it FIVE, and it arrived inside H14's own fix: H14 read the
# attached value as `tok[2:]`, which is the value only when the option letter
# is the first character of the token, and a CLUSTER puts it further along —
# `grep -cf/etc/hostname` handed the child `/etc/hostname` while this file
# computed `f/etc/hostname` and passed it. The fix is the first one in the
# chain that does not enumerate anything: an attached short-option value is a
# SUFFIX of its token by the definition of `getopt`, so every suffix is
# tested and the question "which letters take a value" — the question whose
# five successive wrong answers are this list — is never asked (H1,
# _attached_option_values). It is worth stating what that does and does not
# settle: it closes the SPELLING dimension of a dash-token completely, and it
# says nothing about a path that reaches a child by some other route.
#
# ROUND 22 IS THAT SENTENCE COMING TRUE, TWICE, AND IT IS WHY THE ROUNDS 9-21
# CHAIN ABOVE IS NOT THE WHOLE STORY. Both findings are the same class in the
# dimension round 21 explicitly disclaimed — a path reaching a child by a
# route that is not a dash-token — and the OS child limits (rule 6) touch
# neither, because every one of them bounds writing, CPU or memory and both
# of these are READS:
#   * H19 — a path can be a STRING LITERAL INSIDE A PROGRAM.
#     `awk 'BEGIN{ARGV[ARGC++]="/etc/passwd"}END{print NR}' data.txt` assigns
#     into awk's own operand vector, so awk opens that file as ordinary main
#     input. No pipe, no `getline`, no disallowed call, no forbidden
#     character; `self_contained` is satisfied by the legitimate `data.txt`,
#     and confinement never sees the path because it is not an operand.
#     Reproduced through scan() into the FAIL block at exit 1 ("document says
#     3; command returns 28"), and as a byte-level oracle over /etc/hostname
#     using only allow-listed calls. Closed by AWK_SPECIAL_ARRAYS.
#   * H20 — a path can be reached by TRAVERSAL, and by FILE CONTENTS.
#     `grep -R`, `find -L`/`-H`/`-follow`, `rg --follow` and `diff -r` walk
#     THROUGH a symlink inside an in-repo directory operand, whose own
#     realpath is inside the root and passes. And `wc --files0-from=F`,
#     `sort --files0-from=F`, `find -files0-from F` take the paths to read
#     out of another file's BYTES, where no operand check of any kind can
#     reach them. Every one reproduced against a committed symlink or a
#     committed byte string, with its non-following sibling as the control;
#     the `wc` one was live rather than latent, because `self_contained`
#     inspects only the FIRST pipeline segment. Closed by per-binary flag
#     denials AND by `escaping_symlink_under()`, which is the structural
#     half: no operand DIRECTORY may contain an escaping symlink at all,
#     whatever flags were passed and whatever the binary would do with them.
# The lesson the chain above had been drawing — "stop enumerating spellings"
# — was right and too narrow. The generalisation is: THE SET OF ARGV TOKENS
# IS NOT THE SET OF PATHS A CHILD OPENS. Rule 4 below now says so in terms,
# and names the three routes it cannot follow rather than implying there are
# none.
#
# So the sixth rule below is a different KIND of rule, and it was added for
# that recurrence rather than for any one report: after four rounds of
# closing respellings by name, the read-only property is moved off this
# file's judgement and onto the kernel — and round 21 is the measured proof
# that this was necessary but not sufficient, since the limits do not touch a
# READ and H1 was a read. Enumeration is still necessary — the
# limits do not stop reads, and reads are how this tool's answers are
# fabricated — but it is no longer the only thing standing between a document
# line and the runner's filesystem.
#
# The property therefore rests on six things together, in this order of
# importance:
#   1. ALLOW-LISTS WHEREVER THE ARGUMENT IS A LANGUAGE. `sed` (round 9) and
#      `python3` (round 15, H2) are DROPPED — for python3 the old rationale
#      ("a `.py` file the checkout already contains — CI runs those anyway")
#      was simply false: CI runs four NAMED scripts, and on `pull_request` the
#      checkout IS the pull request's head, so a PR that adds `tools/pwn.py`
#      and a claim quoting it had arbitrary code executed with write access.
#      `awk` is kept — a real, currently-reproducing claim on this tree pipes
#      through it (see the SCRIPT-hatches paragraph below, the canonical site
#      for that measurement — round 20, M19: the fraction used to be stated
#      here too, twice, and had drifted wrong in both places) — with its
#      program allow-listed (AWK_ALLOWED_CALLS) rather than blacklisted, plus a
#      flat refusal of `|` and `@` in any awk token and of every awk SPECIAL
#      ARRAY (AWK_SPECIAL_ARRAYS — `ARGV`/`ARGC`, `ENVIRON`/`PROCINFO`,
#      gawk's `SYMTAB`/`FUNCTAB`). Those are variables, not calls, so the
#      call allow-list structurally cannot see them; `ARGV` (round 22, H19)
#      is the one that opens an arbitrary FILE, and it was missed for four
#      rounds because it calls nothing at all.
#   2. Each binary's `denied_flags` / `denied_prefixes` (BINARIES, round 17
#      L1 — one `Binary` record per allow-listed binary, consolidated from
#      what used to be separate DENIED_FLAGS / DENIED_FLAG_PREFIXES tables)
#      compared on the option CORE, so an attached value (`-oFILE`,
#      `-O./p.sh`) or an un-enumerated `--long=value` cannot respell a denied
#      hatch past the check (round 15, H3) — and, since round 18 (H12),
#      compared as a PREFIX for long options, because `getopt_long` accepts
#      `--o=FILE` for `--output` and `--compress-progr=./p.sh` for
#      `--compress-program`. Over-refusal is the safe direction and no live
#      claim uses an abbreviated long option.
#   3. GIT_READONLY holding only subcommands that cannot destroy anything —
#      `branch` and `tag` are gone, because `-D`/`-d` delete refs (round 15,
#      H4), and `--output` is denied because a diff writes a file with it.
#   4. PATH CONFINEMENT, checked after glob expansion. The property it is
#      REACHING FOR is "a command may read the checkout and nothing else";
#      the property it ENFORCES is narrower and is stated that way here from
#      round 22 on, because every previous statement of it was falsified by
#      the next round's reproduction. What it enforces:
#        (a) every OPERAND, and every value a dash-token can carry, realpaths
#            inside the root (escaping_operand);
#        (b) no operand DIRECTORY contains a symlink leaving the root, at any
#            depth (escaping_symlink_under, round 22 H20) — so a recursing
#            command cannot walk out of the checkout even if this file has
#            mismodelled which of its flags follow links;
#        (c) the routes that reach a path by NEITHER of those are refused by
#            NAME, per binary and per language, because there is nothing in
#            argv for a containment rule to test: awk's `ARGV` (H19), and the
#            `--files0-from` / `-files0-from` family on `wc`, `sort` and
#            `find`, which read the paths to open out of another file's bytes
#            (the H20 sweep).
#      (c) is an enumeration and is therefore the weak leg, deliberately kept
#      visible as such rather than folded into the sentence above it.
#      Without any of this, `grep -c .
#      /etc/passwd` was a one-integer read oracle over the host (round 15, M3).
#      Round 18 (H14): "every operand" now includes the value ATTACHED to an
#      option token, which the check used to skip wholesale — until then this
#      very sentence was false as written, and `diff --from-file=/tmp/secret
#      data.txt \| wc -l` printed an integer derived from a file outside the
#      checkout, into the FAIL block, for anyone reading the CI log.
#      Round 21 (H1) — the FIFTH respelling, and this sentence was false
#      AGAIN in the same way, one layer down. H14 extracted the attached
#      value as `tok[2:]`, which assumes the option letter is the first
#      character; in a CLUSTER it is not. `grep -cf/etc/hostname data.txt` is
#      `-c -f /etc/hostname` to every GNU binary here, so the value is
#      `/etc/hostname` — while the check computed `f/etc/hostname`, a
#      RELATIVE path that resolves INSIDE the root and passed. The separated
#      (`-f /etc/hostname`) and attached (`-f/etc/hostname`) spellings were
#      correctly declined the whole time; only the clustered one walked
#      through, and the round-18 OS limits do not cover it, because a read is
#      not a write. The rule now refuses to model option grammars at all: an
#      attached short-option value is, by the definition of `getopt`, a
#      SUFFIX of its token, so EVERY suffix is tested as a path and the
#      binary's own choice of which one is the value stops mattering. See
#      _attached_option_values() for the argument, and for the honest
#      statement of what it does NOT cover (a path inside a `key=value` bare
#      operand, a file a binary opens on its own, the environment).
#      ROUND 22 (H19, H20) — and this sentence's "a file a binary opens on
#      its own" was not a hypothetical corner: it was THREE live routes, and
#      naming them as a residual is how they were found. The suffix rule is
#      still exactly right about dash-tokens and still says nothing about
#      anything else, which is why (b) and (c) above exist. What remains
#      unenforced by this file, named rather than implied — see
#      escaping_symlink_under()'s docstring and the round-22 version-history
#      row: a recursion with NO operand (defaulting to `.`), which the flag
#      denials and `needs_file` cover instead; a binary's own config or
#      dot-files (git's `.git/config`, rg's `RIPGREP_CONFIG_PATH`), none of
#      which a DOCUMENT can point anywhere; and TOCTOU between the symlink
#      walk and exec, against an external writer this tool has never claimed
#      to defend.
#   5. RESOURCE BOUNDS: no shell, a wall-clock timeout AND a hard cap on how
#      much a segment may print, because a timeout does not bound memory —
#      one document line drove the checker to 587 MB and `cat /dev/zero` would
#      OOM-kill the runner first (round 15, M1). NUL is refused up front and
#      ValueError caught, so document text cannot abort the scan (M2).
#   6. OS-ENFORCED CHILD LIMITS (round 18) — the only rule here that is not an
#      enumeration, and the only one that holds against a hatch nobody has
#      thought of. Every child runs under RLIMIT_FSIZE=0, RLIMIT_CPU and
#      RLIMIT_AS, set in the forked child before exec. Demonstrated on a hatch
#      fixed by no name in this file: `git status \| wc -l` passes the
#      allow-list, every deny-flag and confinement, and REWRITES `.git/index`
#      in the checkout — it now dies on that write and is declined by name,
#      with the index byte-identical afterwards. State its LIMITS honestly,
#      because a safety claim stated too broadly is the error this section
#      keeps recording:
#        * it does not stop READS at all. Rule 4 is still the only thing
#          between a document line and every file the runner can read.
#        * it does not stop EXECUTION. A permitted `--compress-program`
#          would still run its script; it just could not write.
#        * RLIMIT_FSIZE=0 stops a file GROWING past zero bytes. Creating an
#          empty file and TRUNCATING an existing one still succeed — measured,
#          not assumed. So rules 1-4 are load-bearing exactly as before, and
#          nothing was relaxed because these limits exist.
#        * it is POSIX-only. Without a `resource` module the tool degrades to
#          the pre-round-18 behaviour and PRINTS that it has done so
#          (child_limit_summary(), on every run) rather than losing the
#          property silently.
#        * a child killed mid-write can leave a partial artefact its own
#          cleanup would have removed: `git status` leaves a zero-byte
#          `.git/index.lock`. Recorded rather than worked around — exempting
#          git from the limit would exempt the one binary here that writes.
#        * the limits are the STRICTEST of what this file asks for and what
#          the runner already imposed — both fields, soft and hard. Round 21
#          (H2): that was the stated guarantee and not the implemented one.
#          `_lower_limit` clamped against the inherited HARD limit alone, so
#          an inherited soft limit was RAISED — measured, an inherited
#          RLIMIT_CPU of (10, 100) came out of the child as (60, 65), six
#          times the CPU the runner intended, and an inherited (10, INF) was
#          not clamped at all. Same weakening on RLIMIT_AS, where a runner's
#          memory cap is a real containment measure. RLIMIT_FSIZE=0 was never
#          affected and still is not — 0 is the minimum — but only because
#          the clamp excludes RLIM_INFINITY from the comparison rather than
#          calling `min()` on it: RLIM_INFINITY is -1 on Linux, so a naive
#          `min(0, RLIM_INFINITY)` would have turned the load-bearing write
#          ceiling into "unlimited" on any ordinary host. Checked, not
#          assumed (_tightest()).
#
# Every one of those refusals is COUNTED AND NAMED in the printed output. That
# is not politeness: a silent refusal is indistinguishable from a pass, which
# is the defect this whole tool exists to deny itself.
#
# Exit codes. Three kinds of non-zero, and the distinction is load-bearing:
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
#       MIN_LIVE_CLAIMS (round 19, H13), CHECK 2's own recall below its
#       floors (round 19, H17 — until then, blinding CHECK 2 completely still
#       printed PASS and exited 0), or the doc-consistency-check.py import
#       CHECK 1's dated-record excusal depends on failing to load or missing
#       the surface this file calls through it (round 17, M13). It outranks 1 and 3,
#       because with the surface set (or the import) broken, the mismatches
#       that were found are not the mismatches that exist. Before H9 existed,
#       deleting 8 of the 9 named surfaces printed eight MISSING SURFACE
#       lines, checked one claim, and exited 0 with PASS.
#   3 = CHECK 2 FOUND A DOCUMENT WRONG, AND CHECK 1 DID NOT: at least one spec
#       code fence references an identifier its own file does not declare, with
#       every claim CHECK 1 checked reproducing. Round 17 (M14) split this out
#       of 1 — before it, CHECK 1 and CHECK 2 fused into one exit code with
#       nothing in the number itself saying which of two unrelated defect
#       classes CI was red for; the printed FAIL block always named it, but the
#       exit code, the thing automation actually branches on, did not. From
#       the day this file was written until round 17, the dangling-identifier
#       path returned 1, same as CHECK 1 — that history is why 1 still wins
#       when both checks fail, rather than 3 taking over as the newer code.

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
_DCC_CONTRACT = ("record_regions", "frozen_chain_span", "sentence_window",
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
# Round 17 (L1). Every fact this file knows about ONE allow-listed binary used
# to live in up to eight separate parallel tables — ALLOWED_CMDS (membership),
# GIT_READONLY / GIT_GLOBAL_DENIED (git's two-phase flags), DENIED_FLAGS,
# DENIED_FLAG_PREFIXES, BENIGN_NONZERO, the needs-a-real-file-operand set, and
# `_KNOWN_BINARIES` for the OPPOSITE purpose (naming a binary this file
# recognises but refuses) — with nothing cross-checking any of them. Adding a
# binary to the allow-list therefore obliged the author to separately remember
# to consider all the others, and forgetting one is exactly this round's High
# findings. The drift was already live proof of the failure mode: a `sed` row
# sat in the needs-a-file table, unreachable since `sed` was dropped from the
# allow-list at round 9 (parse_pipeline rejects `sed` on argv[0] before that
# table is ever consulted), while `_KNOWN_BINARIES` — correctly — still lists
# `sed`, for the unrelated purpose of naming it in a decline.
#
# One `Binary` record per ALLOWED binary now, and ALLOWED_CMDS is DERIVED as
# its key set rather than maintained as a separate list — so a binary that is
# not allowed cannot carry a stray row in any of the other fields, and a
# binary that IS allowed has all four of its security-relevant properties
# sitting in one place, in the same commit, by construction. `_KNOWN_BINARIES`
# stays separate below: it deliberately names binaries this file does NOT
# allow, which is not a property `Binary` has any field for.
class Binary:
    """One allow-listed binary's read-only contract:
      denied_flags     — exact denied option cores (round 9 H1, round 15 H3)
      denied_prefixes  — denied `--long=` prefix forms not already caught by
                          the exact-core check above (see _option_cores()).
                          Round 20 (M23): this is a REAL constraint, not
                          decoration — `--flag=value` already reduces to the
                          bare core `--flag` via _option_cores(), so an entry
                          here whose bare form already sits in denied_flags
                          is dead weight the exact-core check already owns.
                          10 of this file's then-12 entries were exactly
                          that (measured, not assumed); the two survivors
                          were moved into `denied_flags` outright rather
                          than kept as a prefix — see git's entry below.
                          Every binary here currently needs NO prefix entry
                          at all, by construction, not by omission.
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
    __slots__ = ("denied_flags", "denied_prefixes", "benign_exit",
                 "needs_file", "pattern_operand", "git_subcommands",
                 "git_global_denied")

    def __init__(self, denied_flags=frozenset(), denied_prefixes=(),
                 benign_exit=frozenset(), needs_file=False,
                 pattern_operand=False, git_subcommands=None,
                 git_global_denied=None):
        self.denied_flags = denied_flags
        self.denied_prefixes = denied_prefixes
        self.benign_exit = benign_exit
        self.needs_file = needs_file
        self.pattern_operand = pattern_operand
        self.git_subcommands = git_subcommands
        self.git_global_denied = git_global_denied


_NO_BINARY = Binary()          # the all-defaults record for a plain binary


# Round 22 (H20). SYMLINK-FOLLOWING TRAVERSAL FLAGS, denied per binary.
#
# `escaping_operand` realpaths each OPERAND and refuses one that leaves the
# root — which correctly catches a symlink supplied DIRECTLY (`grep -c root
# q` where `q -> /etc/passwd` is declined). It does not model TRANSITIVE
# traversal: an in-repo DIRECTORY operand realpaths inside the root and
# passes, and a recursing binary then walks through a symlink inside it to a
# file outside. On `pull_request` the checkout is the PR head and
# `actions/checkout` preserves symlinks, so `sub/x.md -> /etc/passwd` plus a
# document claim is entirely attacker-controlled. Reproduced, each against a
# committed symlink and each with its non-following sibling as the control:
#   `grep -Rl 'root' sub \| wc -l`   -> 1   (`-rl` lowercase -> 0)
#   `find -L deep -name passwd \| wc -l`   -> 2   (plain `find` -> 0)
#   `find deep -follow -name passwd \| wc -l` -> 2
#   `rg --follow -l root sub \| wc -l` -> 1   (`rg -l` -> 0)
#   `diff -r sub other \| wc -l` -> a 30-line diff OF /etc/passwd
# The lowercase/plain forms are the control in every case, and they are also
# the ONLY forms the live corpus uses (`grep -r`, `grep -rn`, plain `find`),
# so denying the following variants costs this tree nothing — measured, not
# assumed.
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
    # `--generate` is a distinct info-dump hatch. No `denied_prefixes` entry
    # here (round 20, M23 — all three used to be listed, redundantly):
    # `_option_cores` already reduces `--pre=value` to the bare core `--pre`,
    # which the exact-core check above already matches, so a `--pre=` prefix
    # entry beside the bare `--pre` in denied_flags caught nothing the exact
    # check did not already catch.
    # `-L`/`--follow` is rg's own symlink-following switch (round 22, H20);
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
    # `--files0-from=F` (round 22, the H20 sweep) is the ONE member of that
    # family that was live rather than latent, and it was live because
    # `self_contained` only ever inspects the FIRST pipeline segment:
    # `cat data.txt \| wc -l --files0-from=l2.txt` put wc in second position,
    # where the needs-a-real-operand rule does not run, and it read
    # /etc/passwd — measured, two children spawned, the count declined only
    # because wc prints "25 /etc/passwd" rather than a bare integer, which
    # one `\| cut -d' ' -f1` undoes. The path never appears in argv at all:
    # it is a byte string inside another file, so neither the suffix rule nor
    # confinement can see it. Denied by name, as for `sort` and `find`.
    "wc": Binary(denied_flags=frozenset({"--files0-from"}), needs_file=True),
    "cat": Binary(needs_file=True),
    "head": Binary(denied_flags=frozenset({"-f", "--follow"}), needs_file=True),
    "tail": Binary(denied_flags=frozenset({"-f", "-F", "--follow"}),
                    needs_file=True),
    # `--compress-program` (round 15, H5) runs an arbitrary program whenever a
    # sort spills to disk, which `-S 1` forces; proven creating a canary here.
    # No `denied_prefixes` (round 20, M23 — both former entries were the same
    # redundancy as rg's above: `--output`/`--compress-program` are already
    # bare cores in denied_flags, so their `=value` forms are already caught).
    # `--files0-from=F` (round 22, the H20 sweep) makes sort read the list of
    # files to sort out of F's CONTENTS — a path reaching the child by no
    # dash-token route, so `_attached_option_values`' suffix rule cannot see
    # it and confinement never tests it. LIVE, not latent, and the first
    # guess about why it was safe was wrong: `needs_file` does decline it in
    # first position, but `self_contained` inspects only the FIRST segment,
    # so `cat data.txt \| sort --files0-from=l2.txt \| wc -l` reached the
    # FAIL block reporting 25 — the line count of /etc/passwd, three children
    # spawned. Denied by name, the same class as find's and wc's.
    "sort": Binary(
        denied_flags=frozenset({"-o", "--output", "--compress-program",
                                 "--files0-from"}),
        needs_file=True),
    # Denied flags empty by design — `uniq` is guarded by OPERAND COUNT
    # instead, in denied_flag()'s own uniq branch: `uniq IN OUT` writes OUT,
    # and no flag names that hatch.
    "uniq": Binary(needs_file=True),
    # `-l`/`--load`, `-i`/`--include` and `-E`/`--exec` are gawk's extension
    # and source-loading flags — the same class as `-f`, added in round 15
    # H5's one-pass audit of every remaining allow-listed binary rather than
    # one report at a time. The program-text allow-list (AWK_ALLOWED_CALLS,
    # AWK_ESCAPES) is a SEPARATE mechanism below, for the same reason `sed`
    # has no Binary entry at all: the escape lives in the SCRIPT, a language,
    # not a flag, so no flag table — consolidated or not — can describe it.
    # pattern_operand=True (round 20, M21): the first non-option token is the
    # PROGRAM text, not a file — same reasoning as the grep family above.
    # No `denied_prefixes` (round 20, M23 — all four former entries were the
    # same redundancy: every one's bare core already sits in denied_flags).
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
    # Round 20 (M23). `--exec-path`/`--upload-pack` moved INTO denied_flags,
    # from being denied_prefixes-only entries here. They used to be
    # load-bearing ONLY via git_global_denied (the pre-subcommand-only
    # check below) and the generic startswith() prefix fallback — three
    # mechanisms doing the work of one, and their cores were missing from
    # this set for no stated reason. Denying them here too is a strict
    # WIDENING (over-refusal, the stated safe direction): the exact-core
    # check now refuses them at ANY argv position, not only before the
    # subcommand, and their `=value` forms are caught the same way
    # `--output`'s already was — via `_option_cores`, with no
    # `denied_prefixes` entry needed. `--output` itself needed no entry
    # to begin with; it was already redundant with its own bare form here.
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
    # `-r`/`--recursive` (round 22, H20). GNU diff DEREFERENCES symlinks it
    # meets while recursing unless `--no-dereference` is given, so — unlike
    # grep and find, which have a safe default spelling to keep — there is no
    # non-following form of `diff -r` to preserve. Reproduced: with
    # `sub/x.md -> /etc/passwd` and a plain `other/x.md`, `diff -r sub other
    # \| wc -l` printed 30, i.e. a diff OF /etc/passwd, into the FAIL block.
    # The live corpus quotes no `diff -r` at all (its only diff is
    # `git diff --stat`), so the denial costs nothing.
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
# Round 9, H1: allow-listing argv[0] is NOT sufficient, and the header used to
# claim it was ("read-only by construction — no writing command is on the
# list"). Several read-only tools carry a write or execute ESCAPE HATCH behind
# a flag, and `sed -i` was demonstrated rewriting a file in the working tree
# while this tool printed PASS. The commands come from documents; on a
# `pull_request` CI trigger that is untrusted input reaching a runner.
#
# Two mechanisms, because the hatches are of two kinds:
#   (1) FLAG hatches — refusable exactly, by name. Listed per binary above, in
#       BINARIES.
#   (2) SCRIPT hatches — the argument IS a language, so no flag list can
#       contain them. `sed` (`w file`, `s///w file`) is therefore DROPPED from
#       the allow-list entirely; it has no use anywhere in the corpus, and a
#       future sed claim is DECLINED AND NAMED, which is the safe direction.
#       `awk` is a language too, and round 15 (H1) proved the consequence:
#       `awk 'BEGIN{print "touch X" | "sh"}'` executes a shell command using
#       NEITHER of the two words this file blacklisted, and FORBIDDEN does not
#       reject a single `|` because that is the pipeline separator. awk is
#       nonetheless KEPT: a real claim in this corpus pipes through it and
#       currently reproduces. (Round 20, M19 — THE CANONICAL SITE for this
#       fact: the SAFETY section's rule 1 above states it too, and cites here
#       rather than repeating a number, because the "2 of the 3 claims this
#       tool executes are awk" figure this paragraph used to carry was wrong
#       by the very re-measurement it invited — re-derived 2026-08-22, this
#       tool executes 3 DISTINCT commands; exactly 1 of them pipes through
#       awk, accounting for 3 of 7 executed INSTANCES (re-derived
#       2026-08-22 after round 23 — it read "3 of 6" until H24 bound a
#       seventh), and since H23 one of those three IS live: the
#       currency-pierced Version History row at code-standards
#       /section-3.md:1140. So the honest
#       argument for keeping awk is qualitative, not a coverage fraction that
#       goes stale the next time the corpus changes: SOME real, currently-
#       passing claim on this tree uses it, dropping it costs that measured
#       claim, and re-deriving which fraction is exactly the
#       `python3 tools/doc-claim-check.py --repo .` invocation
#       MIN_EXECUTED_CLAIMS already documents doing — do not copy a number
#       out of this comment without re-running it.) —
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

# awk's SPECIAL ARRAYS, each with the reason it is refused. Round 22 (H19).
#
# Round 18 refused `ENVIRON`/`PROCINFO` because an allow-list of FUNCTION
# names structurally cannot see a VARIABLE. `ARGV`/`ARGC` are the same
# mechanism aimed at the filesystem instead of the environment, and they were
# missed because every awk rule this file has ever written asks what the
# program CALLS or which CHARACTERS it contains — and this one calls nothing
# and contains nothing forbidden:
#
#     awk 'BEGIN{ARGV[ARGC++]="/etc/passwd"}END{print NR}' data.txt
#
# assigns a path into awk's own operand vector, so awk OPENS that file as
# ordinary main input. No pipe, no `getline`, no disallowed call, no forbidden
# character — and PATH CONFINEMENT is structurally blind to it, because the
# path is a string LITERAL inside the program token rather than an operand,
# while `self_contained` is satisfied by the legitimate `data.txt` beside it.
# Reproduced end to end through scan(): a document stating 3 landed in the
# FAIL block at exit 1 reading "document says 3; command returns 28", the 25
# extra lines being /etc/passwd; and as a BYTE-level oracle over
# /etc/hostname using only allow-listed calls (`index`, `substr`).
#
# `SYMTAB`/`FUNCTAB` are gawk's symbol tables and are refused with them: an
# `SYMTAB["ARGV"]` write reaches the same vector indirectly, so refusing the
# direct spelling alone would be the enumerate-the-spelling error this file's
# SAFETY section calls its recurring root cause. (`\bARGV\b` already matches
# inside the quoted string, so that particular route is doubly covered —
# refusing the table itself is what covers the routes nobody has written yet.)
#
# The rule is a NAME refusal, deliberately, and it is the right shape here for
# the reason the SAFETY section gives for awk generally: awk's argument is a
# LANGUAGE, so the load-bearing rule is the CALL allow-list, and these are the
# handful of things that allow-list cannot express because they are not calls.
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
# ROUND 19 (H15). This list is no longer the GATE, only a supplement to one.
# It could not be a gate: a runnable claim whose binary was on neither curated
# list (ALLOWED_CMDS or this) was dropped with no bucket, no count and no
# line — `check_claim` returned ("ignored", None), and `unrecognised_spans`
# could not recover it either, because the span WAS bound so its start sits in
# `bound`. `comm -12 a b \| wc -l`, `tac`, `pcregrep`, `tree`, `nl` and `du`
# claims were therefore invisible on BOTH routes, which made two sentences of
# this file's own header false — including "every backticked span that reads
# as a command ... is counted and named in the unrecognised-shape bucket. Read
# that bucket, not this comment, for what the tool cannot see." The blind spot
# WAS the hand-written list, which is the shape the SAFETY section names as
# this file's recurring root error, arriving on the census side.
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

    Round 17 (M13): `record_regions`'s own docstring states a precondition
    this caller used to violate — "computed on the same (frozen-history-
    blanked) text the scans read, so the offsets agree" — while this function
    passed RAW text. It was harmless only because `blank_frozen_history` is
    offset-preserving, which is a property of that function's IMPLEMENTATION,
    not a contract this file was entitled to assume. HONOURED HERE rather
    than replaced with a new assertion: `record_regions` is now called on the
    same blanked text `doc-consistency-check.py`'s own scans read.

    ROUND 19 (H13) — THE HALF THAT WAS STILL WRONG, and it was the important
    half. This function called `blank_frozen_history` only as INPUT to
    `record_regions` and then THREW THE BLANKING AWAY, returning
    `record_regions` plus a separately re-listed `frozen_chain_span`. But
    that function freezes TWO things, not one: the header chain below its
    head entry AND every `Version History` section (its own docstring: "a VH
    row is a dated record of its own revision and states no currency"). So
    the sibling excused a mismatch in a VH row and this tool GATED on it —
    the two tools disagreeing about which bytes are frozen, in the one file
    that imports the other specifically so they cannot.

    Not hypothetical, and not a corner: VH is 5.9% of the scanned corpus and
    FOUR of the six claims this tool executed sat inside one, two of them
    `ls -d src/*/ \| wc -l` -> 35 in rows whose own neighbours read "left as
    written per the do-not-rewrite-history convention". CI would have gone
    red on two correct historical records the day a 36th assembly landed —
    verbatim the hazard the dated-record model was introduced to prevent,
    and the reason the round-12 CHANGELOG entry had to be reworded by hand.

    The currency pierce is unchanged: a VH row that says the command returns
    N *now* is a present-tense claim wherever it sits, and `currency_asserted`
    still reports it."""
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
# `re.M` is load-bearing, not decoration (round 18, found by the new
# `tools/tests/test_doc_claim_check.py`). This pattern is used as
# `ERR_TABLE_ROW.match(text, ls)` with `ls` the start of a LINE, and without
# MULTILINE a bare `^` matches only at the real start of the string — never at
# a non-zero `pos`, whatever precedes it. So the table-row branch below could
# not fire for any row except one at byte 0, every index row fell through to
# the prose-section branch, and the whole `## Error Index` table was bounded as
# ONE entry — precisely what _err_log_entry_span's own docstring says must not
# happen ("a ✅ anywhere in its 213 rows [would] resolve every one of them").
# Live at the time of the fix: 191 of the 213 index rows carry a ✅, so every
# claim in every row, resolved or not, was excused.
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

    Round 20 (M22). The sentence window used to be searched for a date
    UNCLIPPED — exactly the defect `doc-consistency-check.py`'s own
    `historically_marked()` was fixed for at its `MARKER_RADIUS` (110),
    whose comment states the reason in terms: this repo's "sentences" are
    not sentences, so at full sentence-window length an annotation about
    something else entirely silently suppresses a real stale claim several
    hundred characters away. Measured on the live ERR log before this fix:
    window lengths median 249, p90 602, max 1126; 13 spans were excusable by
    this rule alone, with the excusing date 53 to 689 characters from the
    claim it excused — the model's own justification is "the claim's OWN
    sentence carries a date", and 689 characters away is not that. Fixed the
    same way the sibling fixed it for the same reason: the sentence window
    is intersected with a tight radius around the claim itself, in the
    window's own coordinates, before the date search runs."""
    a, b = _err_log_entry_span(text, start)
    if ERR_RESOLVED_MARKER.search(text[a:b]):
        return True
    window = DCC.sentence_window(text, start, end)
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
# Round 20 (M20). The floor used to be on INSTANCES, and half of what it
# protected could never fail: today's six executed instances are only THREE
# DISTINCT commands — one quoted in three files, one in two, one in one — and
# the three-instance one is pinned to an immutable git revision
# (`git grep -c 'CROSS-PENDING' 9b841d1^ ...`), so it can never drift and
# never fail regardless of what any document says. An instance floor cannot
# tell "N drift-capable claims" from "one pinned command quoted N times", and
# most of this file's own six instances were exactly that. The floor is now
# on DISTINCT COMMANDS — `checked_commands` in scan() — with the instance
# count still printed alongside it, so the gap between "commands" and
# "quotes of a command" stays visible either way. (Whether to exclude
# revision-pinned commands entirely from even the distinct count is a
# separate, owner-level call this fix does not make.)
#
# That redefinition shrinks the MEASURED base sharply (6 -> 3), and applying
# the old slack of 2 to a base that small would floor at 1 — the same
# near-zero-protection MIN_LIVE_CLAIMS below was written to avoid at ITS
# measurement of 1. So the slack here is re-derived too, at "roughly half the
# measured base", the same headroom rule CHECK 2's own floors
# (MIN_TYPED_FENCE_FILE_SHARE, MIN_EXAMINED_SHARE) already use, rather than
# carrying forward a slack sized for a population six times bigger.
#
# WHEN IT LEGITIMATELY CHANGES: coverage GROWING is the normal case (a new
# shape, a new allow-listed binary, a new claim written) — re-derive and raise
# the floor in the same commit, so the new coverage is protected too. Coverage
# SHRINKING is the case that must not be waved through: lower this number only
# with the reason recorded in the Version History row beside it, because
# lowering it silently is how the vacuous-pass failure class comes back.
MIN_EXECUTED_SLACK = 1
# Re-derived 2026-08-22 by the invocation above, after round 23: 3 DISTINCT
# commands executed (7 instances of them — 6 before H24 taught shape 4 the
# wrapped `re-derive with` attribution, which bound README.md:930's quote of
# a command CLAUDE.md:276 already quotes; a second INSTANCE of a command
# already counted, so the distinct figure the floor gates on is unmoved).
MIN_EXECUTED_CLAIMS = 3 - MIN_EXECUTED_SLACK

# ---------------------------------------------------------------------------
# THE LIVE FLOOR (round 19, H13) — the one that states this tool's real reach.
#
# MIN_EXECUTED_CLAIMS counts every claim the tool RAN. Four of the seven it
# runs on this tree sit inside a dated record whose own pierce fails, so a
# mismatch there is EXCUSED: the command still executes, the divergence is
# still printed, and CI stays green by design. (Re-derived 2026-08-22 after
# round 23; it read "five of the six" until H24 added an instance and H23
# stopped counting a currency-pierced record claim as frozen.) That is worth having — it is how "this historical figure no
# longer reproduces" stays visible — but it is not drift-catching coverage,
# and a floor built on it protects mostly frozen text. Until H13 the two were
# fused, and the headline "6 executed (floor 4)" described a live coverage of
# ONE.
#
# So the honest figure gets its own floor. Re-derived 2026-08-22, the same way
# and by the same invocation: read the "... of which LIVE (can gate)" line.
#
# ROUND 23 MOVED THE MEASUREMENT FROM 1 TO 3, for two unrelated reasons, and
# the FLOOR IS DELIBERATELY LEFT AT 1 — see the argument below, and do not
# read the measurement as the floor. What the line now reads:
#
#     live claims: 3   (2 distinct commands)
#       * CLAUDE.md:276  `ls docs/tracking/*-design.md \| wc -l` -> 60 — the
#         flagship instance this tool was written for, whose own sentence
#         records that it read 42 while the truth was 60 and nobody noticed.
#       * README.md:930  the SAME command, quoted across a hard wrap inside
#         the tree diagram. Live all along; invisible until H24 taught shape
#         4 the `re-derive with` attribution and the wrap prefix.
#       * code-standards/section-3.md:1140  `git grep -c 'CROSS-PENDING'
#         9b841d1^ …` -> 218, in a Version History row. Newly COUNTED live by
#         H23 rather than newly live: it always gated (the row says "245
#         today", and a currency assertion pierces the record excusal), while
#         the counter asked only where it sat.
#
# THE FLOOR STAYS AT 1, and the argument is MIN_EXECUTED_CLAIMS's own (round
# 20, M20) applied to this counter: a floor must not be raised by something
# that can be inflated without adding reach. Neither of the two new live
# instances adds a drift-capable COMMAND.
#
#   * README.md:930 is a second QUOTE of a command already live at
#     CLAUDE.md:276 — repetition, which is exactly what M20 took the executed
#     floor off instances to stop counting.
#   * section-3.md:1140 is REVISION-PINNED (`9b841d1^`), so it can never
#     drift whatever any document says, and its liveness rests on the word
#     "today" sitting within 120 characters of it in a frozen VH row.
#
# So the distinct live commands are 2, of which exactly ONE can drift — the
# same single command that has been this tool's whole live reach since H13
# measured it. Raising the floor to 3, or to 2, would gate CI on repetition
# and on prose adjacency inside a record, not on reach.
#
# THE RESIDUAL, recorded rather than engineered away, because H23 created it:
# a floor of 1 can now be met by the revision-pinned claim ALONE, so a tree
# that lost both design-supplement sentences would print PASS with no
# drift-catching coverage at all. Before H23 that case measured 0 and went
# red. The honest fix is a floor on DISTINCT DRIFT-CAPABLE live commands
# rather than on live instances, which needs "revision-pinned" to be a
# property this file can compute — an owner-level call M20 explicitly
# deferred ("whether to exclude revision-pinned commands entirely from even
# the distinct count is a separate, owner-level call this fix does not
# make"), and the same call decides both floors. It is left open here rather
# than approximated. NOTE FOR WHOEVER TAKES IT: raising this constant today
# fails 12 cases in tools/tests/test_doc_claim_check.py, because that suite's
# `scan()` harness patches MIN_EXECUTED_CLAIMS and not MIN_LIVE_CLAIMS, so
# every fixture is silently pinned to a live floor of 1.
#
# THE NUMBER IS THE POINT, NOT A PROBLEM TO BE ENGINEERED AWAY. Raise it when
# a NEW drift-capable command is quoted in live text, in the same commit;
# lower it only with the reason recorded in the Version History row beside it.
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
# ROUND 19 (H18) — WHAT THAT SENTENCE USED TO CLAIM, AND WHY IT WAS WRONG. It
# read "...and nothing in scan() changes". Executed literally, it crashed: a
# `PairAnswer` with bucket `not-a-pair` raised `KeyError: 'not-a-pair'`,
# because scan()'s `declined` dict was built from DECLINE_BUCKETS — a module
# table this banner never mentioned — and the traceback exited 1, the code
# meaning "a document is wrong", the collision round 17 (M14) spent a whole
# fix separating. Three smaller leaks in the same seam: ANSWER_KINDS was a
# hand-written tuple (a duplicate of the truth, in the tool whose thesis is
# that hand-maintained figures drift), every shape regex hard-coded the
# integer value pattern (shapes and answers were a CROSS-PRODUCT, not two
# orthogonal seams), and `answer.parse` may return a bucket its class never
# declares.
#
# All four are closed: ANSWER_KINDS is DERIVED from the shapes, the decline
# table is DERIVED from DECLINE_BUCKETS plus each kind's own bucket
# (decline_bucket_order), an unforeseen bucket is counted rather than fatal
# (record_decline), and the stated-value grammar lives on the answer kind
# with each shape's regex built from it. What is STILL true rather than
# aspirational, stated so this banner stops over-promising: registering a
# kind and naming it on a shape is enough for scan() to run, count and print
# it. What a new kind still touches outside this section is the SHAPE regex
# it is named on — the text AROUND the value placeholder (shape 1's
# `[^0-9`\n]{0,18}` run, for instance) is written for a value made of
# digits, so a radically different grammar wants its own shape, not just its
# own answer. That is a real limit and it is smaller than the one this
# banner used to hide.
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

# Round 17 (L5). No allow-listed binary this tool ever runs pads a count with
# a leading zero or groups it with a thousands separator — `wc -l`, `grep -c`,
# `git grep -c`, `ls | wc -l` all print a bare digit run. The old grammar
# (`\d[\d,]*`, both here and in single_integer() below) accepted both anyway,
# and Python's own `int()` NORMALISES what it is handed rather than validating
# it: `int("1,2,3".replace(",", ""))` is 123 (comma position is irrelevant to
# it) and `int("007")` is 7 (a leading zero is silently dropped) — so a
# malformed document transcription "reproduced" a real value by accident of
# `int()` being more permissive than any command's actual output grammar ever
# is. A WELL-FORMED integer literal, matched in full: either the single digit
# "0", or a nonzero digit run with no leading zero, optionally grouped in
# EXACT thousands (a document may write "12,345" for readability; no live
# claim's command ever emits the comma itself — see single_integer(), which
# does not accept one).
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
    # Round 19 (H18). The STATED-VALUE grammar belongs to the answer kind, not
    # to each claim shape: it used to be hard-coded as `\d[\d,]*` inside all
    # four shape regexes, so a second answer kind meant editing every shape —
    # shapes and answers were a cross-product, not two seams. Each shape's
    # regex is now BUILT from this, at ClaimShape construction. Deliberately
    # loose (it is the CAPTURE; `parse` below is what validates it), because
    # a malformed transcription must reach a NAMED decline rather than fail
    # to match and vanish.
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
            # Round 17 (L5). `raw` matched the shape's own loose capture
            # (`\d[\d,]*`) but is not a WELL-FORMED integer literal — a
            # malformed thousands grouping ("1,2,3") or a leading zero
            # ("007"). Declined and named rather than silently normalised by
            # `int()`, which would have accepted either and let a malformed
            # transcription "reproduce" a real value by coincidence.
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
    "August 18, 2026", so counting one would overstate what was given up.

    ROUND 19 (H16). The two date rules were ONE CASE of a general defect, and
    stating them as two named prefixes left the rest of the class live. The
    real rule is that a stated value must BEGIN ITS OWN TOKEN: any compound
    number whose tail digits abut a parenthesised command bound as a stated
    value, because the shapes' value pattern happily starts mid-token.
    Reproduced, both correct sentences the tool would have failed CI on:

        §2.2.2 (`grep -c a data.txt`)   -> "document says 2; command returns 3"
        v1.73 (`cmd`)                   -> "document says 73; command returns 3"

    The first is LIVE TEXT — `docs/specs/code-standards/section-3.md` binds
    stated value 2 out of "verified against §2.2.2 (`grep -n ... section-2.md`)"
    — and escaped only because `document_relative_operand` declines that
    command for a wholly unrelated reason, which is the same luck this
    class's docstring already warned about for the date case ("they escape
    only because those backticked spans hold ERR ids rather than runnable
    commands, which is luck, not a rule"). 216 of the 429 leading-value binds
    on this tree share the shape: section numbers (`§3.5.`->2), version
    numbers (`v1.`->73), spec numbers (`#20 §3.`->4), Markdown heading
    numbers (`### 5.1.`->2) and decimal fractions (`52.`->5).

    So the test is now the general one: the character before the value —
    ignoring only emphasis markup, which does not join tokens — must not be
    `.`, `#` or `§`. The date rules stay as they are, both because they catch
    a form this one does not (the digits of "August 18," DO begin their own
    token) and because their message names what was refused."""

    # A stated value begins its own token. These three characters are the ways
    # this corpus continues one: `.` (a section, version, heading or decimal
    # tail), `#` (a spec number) and `§` (a section number).
    COMPOUND_TAIL = (".", "#", "§")

    def rejects(self, text, m):
        before = text[max(0, m.start("value") - 32):m.start("value")]
        if DATE_YEAR_BEFORE.search(before):
            return "the integer is the YEAR of a date, not a stated value"
        if DATE_DAY_BEFORE.search(before):
            return "the integer is the DAY of a date, not a stated value"
        # `rstrip` over emphasis only: "**35**" is a stated value whose token
        # begins at the 3, while "§2.2.**2**" is still a section number.
        if before.rstrip("*_").endswith(self.COMPOUND_TAIL):
            return ("the integer is the last component of a COMPOUND number "
                    "(a section, version, spec, heading or decimal), not a "
                    "stated value — a stated value begins its own token")
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


# The run between the arrow-or-verb and the stated value. Round 23 (H21): it
# used to be `[^0-9`\n]{0,18}` — eighteen characters of ANY non-digit text,
# which is enough for a whole subordinate clause, and H21's third reproduction
# is exactly that:
#
#     `grep -c 'a' data.txt` returns, for the 2 tracking files, a count of 3.
#
# The verb is adjacent to the command, so the gap rule above passes it; ", for
# the " is ten characters and binds the 2 out of an aside, against a sentence
# whose stated answer, 3, is correct. A value the command ANSWERS follows its
# verb across markup and punctuation, never across words — "returned **218**",
# "→ 18", "shows: 4" — so the run is whitespace and markup only. Measured on
# this tree at the same time: of the post-verb runs on 217 verb-route matches
# the overwhelming majority are " ", " **", " == ", " = "; the ones carrying
# words (" the exact Stage-", " of FR-", " at step ") are prose that names a
# number, not a command reporting one, and none of the seven executed claims
# has a word here. (118 of those 217 carry a post-verb run this refuses —
# re-derived 2026-08-22, and see the count note at the CLAIM template below,
# which is where the two halves are added up.)
_POST_VERB = r"[^0-9A-Za-z`\n]{0,18}"


# SHAPE 1 — command, then the stated value: "`cmd` → 18", "`cmd` returned 218".
# The gap is deliberately tight — round 8's H4 showed that a loose lookahead
# binds across unrelated clauses.
#
# ROUND 23 (H21) — AND 40 CHARACTERS WAS STILL A WHOLE CLAUSE, so the two
# routes into this shape no longer share one gap grammar. An ARROW cannot
# belong to a neighbouring clause: "→ 18" after a command is that command's
# answer and nothing else, whatever the 40 characters before it say. A VERB
# can, and `found`/`counted`/`shows`/`returns` are among the commonest verbs
# in this repo's prose, so any sentence that mentions a command and later
# reports SOME number matched. Three reproductions against CORRECT documents,
# each producing "document says 2; command returns 3" end to end through
# scan(), at exit 1:
#
#     `grep -c 'a' data.txt` returns, for the 2 tracking files, a count of 3.
#     The instrument (`grep -c 'a' data.txt`) was re-run and found 2 orphans.
#     `grep -c 'a' data.txt` is the gate; the sweep counted 2 regressions.
#
# All three were re-run against this file both ways on 2026-08-22: at the
# pre-round-23 code each binds and reports "document says 2; command returns
# 3" at exit 1; after it none binds and each is named in the census instead.
#
# The census could not see any of them, because the shape HAD bound the span —
# this is round 19 (H15)'s shape one layer up, a claim that is invisible
# precisely because a matcher took it.
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
# Measured on this tree, and RE-DERIVED 2026-08-22 rather than carried,
# because the figure first written here was wrong: 217 verb-route matches, of
# which the gap rule alone refuses 147, the _POST_VERB rule alone refuses 118,
# and the two together refuse 184. (The number that stood here was 153, which
# is the pre-round-23 unrecognised-shape CENSUS count copied into a sentence
# about a different population — round 20's M19 defect, committed inside the
# paragraph that files it. The procedure is the one M19 prescribes: re-run the
# measurement, never copy the figure.) NONE of the seven claims the tool
# actually executes uses a non-empty verb gap, so the measured cost is nil,
# and the refused matches are re-counted in the census and decline buckets
# rather than dropped — 8 of them moved from a named decline bucket into the
# census on the live tree, and none of them was a claim this tool could run.
# A phrasing this list has not thought of lands in the census under its own
# file and line, which is the self-reporting failure mode round 16 (H7) built
# that bucket for, not a silent drop.
#
# THE THIRD REPRODUCTION IS CLOSED BY THE OTHER HALF, not by this rule, and
# the split is worth keeping straight: "`cmd` returns, for the 2 tracking
# files, a count of 3" has an EMPTY verb gap, so the rule above admits it. It
# is `_POST_VERB` (defined immediately above this comment) that refuses it,
# because the run between the verb and the value may hold no letters. Neither
# half alone closes all three reproductions; both are required.
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
#
# THE CLASS HOLDS EXACTLY TWO PREFIX CHARACTERS AND NOT A THIRD, and the
# third is the one worth recording. The round-23 draft of this line read
# `[\s│|>]+`, admitting the ASCII PIPE as well — which is a markdown TABLE
# CELL separator, so `| 35 | re-derived by | \`ls -d src/*/ \| wc -l\` |`
# bound the 35 out of a neighbouring column. That is H21's own defect
# (a value bound from an unrelated clause) reintroduced one shape over, in
# the same commit that fixed it. Measured before removing it: the ASCII pipe
# binds NOTHING on this tree (shape 4 binds 33 spans with it and 33 without),
# so it was pure risk. `│` is H24's actual need — the tree diagram's
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
            # ROUND 23 (H24). Bounding the census to the span's OWN line was
            # right for its stated job — stopping an entry being manufactured
            # out of the next bullet's numbers — and wrong for the shape this
            # repo actually writes, because it HARD-WRAPS MID-SENTENCE. Live
            # at README.md:930-931 when this landed: the stated value ("60
            # design supplements") sits on one physical line and the command
            # that re-derives it on the next, so no shape bound it AND the
            # census could not name it either. It was a SECOND live instance
            # of the very command this file's LIVE floor is built around,
            # and the tool neither checked it, declined it, nor mentioned
            # it. A corpus sweep found 13 runnable command spans in that
            # shape.
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

    Derived from the same `denied_flags` / `denied_prefixes` a binary already
    declares, never maintained separately — round 17 (L1) consolidated those
    tables precisely because a parallel list is a list someone forgets."""
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
    prefixes = binfo.denied_prefixes
    long_denied = _denied_long_names(exact, prefixes)
    for a in argv[1:]:
        for core in _option_cores(a):
            if core in exact:
                return a if core == a else "%s, attached as `%s`" % (core, a)
            if any(pfx == core + "=" for pfx in prefixes):
                return a
        if any(a.startswith(pfx) for pfx in prefixes):
            return a
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
# CHILD RESOURCE LIMITS (round 18) — DEFENCE IN DEPTH, NOT A REPLACEMENT.
#
# This is not a review finding. It is a structural decision taken on the
# evidence that ONE defect class has now recurred four consecutive rounds —
# round 9's H1, round 14's P1, round 15's H3, round 18's H12 — and each time
# INSIDE THE PREVIOUS ROUND'S FIX. The shape is always the same: a hatch is
# refused by the spelling someone enumerated, and the same hatch respelled
# (`sort -o` → `sort -oFILE` → `sort --o=FILE`) walks past the check. Every
# fix in that chain was correct and none of them was sufficient, because they
# all assert read-only-ness rather than ENFORCING it.
#
# Round 21 (H1) made it FIVE, and is the sharpest available statement of what
# this section can and cannot do: `grep -cf/etc/hostname data.txt` respelled
# H14's attached option value as a CLUSTER, walked past confinement, and READ
# a host file — and not one limit below applies, because every one of them
# bounds writing, CPU and memory, and none of them bounds a read. The limits
# are defence in depth for the WRITE/EXECUTE half only; confinement (rule 4)
# is still the whole of the read half, on its own, exactly as stated below.
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
# one direction and looser in another — round 17 (L5). Stricter: no comma is
# accepted at all, because no allow-listed binary's output ever contains one
# (the document side allows one for readability; the command side is what
# that readability is being checked AGAINST). Looser: an optional leading
# `-`, because `awk` (2 of this tool's 3 live executed claims) can
# legitimately compute and print a negative number, and before this a
# negative answer could not be READ at all — `single_integer("-3\n")` was
# None, which check_claim() reports as "not-a-single-integer", a decline that
# says the tool could not tell. That buries a genuine finding: a document's
# POSITIVE claim compared against a real NEGATIVE answer is a mismatch this
# tool can now actually make, rather than a claim it silently gives up on.
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

    Round 16 (M8). The old rule was `[a for a in argv[1:] if not
    a.startswith('-')]` — the exact OPTION-GRAMMAR error round 14 already
    fixed once for awk's program and python3's script: a flag's own VALUE
    does not start with `-` either, so it counts as an "operand" by this
    filter even though it names nothing. `grep -m 3 -c x` produced ["3", "x"]
    — two operands, meeting grep's old need-2 floor — and ran with an EMPTY
    stdin, reporting "document says N; command returns 0" against a document
    that was never wrong, which is precisely what this guard's own comment
    says it exists to prevent. `awk -v n=1 'END{print NR}'` failed the same
    way. Enumerating which flag on which binary takes a separate-argument
    value is the shape of rule round 14 and round 15 both found does not
    hold up — the fix here does not try: a segment used to be self-contained
    exactly when at least one of its tokens, whatever else it might look
    like, named a real file relative to the repo root.

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
    `ls -la`, `wc -lc file`, `grep -ic pattern file`) and all six live executed
    claims still execute, because a suffix escapes only by being absolute or
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
# MIN_EXECUTED_CLAIMS gates CHECK 1's recall and NOTHING gated CHECK 2's.
# Blinding CHECK 2 completely still printed PASS and exited 0 — reproduced on
# the live tree with one mutation making DECL_CLASS match nothing: "references
# examined : 1882 (1882 skipped)", every reference skipped, zero examined,
# success reported, and all 71 tests green throughout. That is the
# vacuous-pass failure class verbatim — fixed for CHECK 1 at round 16 (H9) and
# never applied here, because CHECK 2's counters (round 17, M14) were added
# without a gate to hang them on. It is reachable without a code change too:
# `_CSHARP_TAGS` is ("csharp", "cs"), so a corpus that starts writing ```C#
# silently zeroes the whole check.
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
# only with the reason recorded in the Version History row beside it.
MIN_TYPED_FENCE_FILE_SHARE = 0.20
MIN_EXAMINED_SHARE = 0.05


def fence_coverage_shortfalls(coverage):
    """Every reason CHECK 2's recall is too low to call this run a verdict.

    Both floors are skipped when their DENOMINATOR is zero, and the third
    check is what stops that being a hole: a corpus with fenced spec files
    but not one examinable reference means the REFERENCE matcher itself has
    collapsed, which no share can express."""
    fenced, typed, examined, skipped = coverage
    out = []
    if fenced and typed / fenced < MIN_TYPED_FENCE_FILE_SHARE:
        out.append(
            "only %d of %d fenced spec file(s) yielded a csharp/cs fence "
            "(%.2f, floor %.2f) — CHECK 2 examined almost nothing, so its "
            "silence is not a verdict; see CHECK 2'S COVERAGE FLOOR"
            % (typed, fenced, typed / fenced, MIN_TYPED_FENCE_FILE_SHARE))
    if fenced and not examined:
        out.append(
            "%d fenced spec file(s) and NOT ONE examinable reference — the "
            "reference matcher has collapsed, so CHECK 2 found nothing "
            "because it looked at nothing" % fenced)
    elif examined and (examined - skipped) / examined < MIN_EXAMINED_SHARE:
        out.append(
            "%d of %d reference(s) examined were SKIPPED, leaving %.2f "
            "actually examined (floor %.2f) — a run that skips everything "
            "reports success for the same reason a blinded one does; see "
            "CHECK 2'S COVERAGE FLOOR"
            % (skipped, examined, (examined - skipped) / examined,
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


def scan_fence_identifiers(repo, quiet=False):
    """Report Type.MEMBER references whose Type the file declares and whose
    MEMBER it does not.

    Returns `(findings, coverage)`, where coverage is
    `(fenced_files, typed_fence_files, examined, skipped)` — round 19 (H17)
    needs those four to gate this check's recall, and they were previously
    printed and thrown away. (Round 17's L3 had corrected this docstring for
    promising a second return value the function did not have; it has one
    now, and it is used, not decorative.)

    `quiet` is kept for call-signature compatibility with the caller in
    scan() but no longer gates anything here (round 20, L7): every print in
    this function is now unconditional, matching CHECK 1's own treatment —
    there is no itemized per-item CHECK 2 listing for `--quiet` to suppress
    in the first place, since the dangling-identifier findings themselves
    already print unconditionally back in scan()."""
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
    print("  coverage floors                : %d/%d fenced files typed "
          "(floor %.2f), %d/%d references actually examined (floor %.2f)"
          % (scanned, fenced_files, MIN_TYPED_FENCE_FILE_SHARE,
             examined - skipped, examined, MIN_EXAMINED_SHARE))
    return findings, (fenced_files, scanned, examined, skipped)


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
        dangling, fence_coverage = scan_fence_identifiers(repo, quiet)
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
    if live < MIN_LIVE_CLAIMS:
        blocked.append(
            "only %d LIVE claim(s) executed — below the floor of %d. Every "
            "other executed claim sits inside a dated record whose own "
            "pierce does not fire, so a mismatch there is excused and this "
            "run could not have caught drift in any document. See THE LIVE "
            "FLOOR beside MIN_LIVE_CLAIMS"
            % (live, MIN_LIVE_CLAIMS))
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
# | 1.7     | 2026-08-22 | Claude Code | Round 17 (M10, M11, M13, M14, L1-L5) — five  |
# |         |            |             | reviewer-named High/Medium findings plus five|
# |         |            |             | Low, all reproduced before the fix and       |
# |         |            |             | re-proven in BOTH directions after. **M10:** |
# |         |            |             | DECL_CLASS matched `class` only — 181 struct |
# |         |            |             | / 91 class / 65 enum / 14 interface declared |
# |         |            |             | on this tree's fenced corpus, so a struct    |
# |         |            |             | member reference was silently unexaminable,  |
# |         |            |             | and root CLAUDE.md names struct-based        |
# |         |            |             | architecture as the STANDING rule. Identical |
# |         |            |             | reference proven reported for `class` and    |
# |         |            |             | missed for `readonly struct`. Extended to    |
# |         |            |             | class/struct/interface/enum/record, with enum|
# |         |            |             | members harvested SEPARATELY (an enumerator  |
# |         |            |             | has no preceding type, and the existing      |
# |         |            |             | member patterns can only see one) — a        |
# |         |            |             | comma-splitting bug in that harvest, caused  |
# |         |            |             | by un-stripped `///` doc-comment prose       |
# |         |            |             | containing its own commas, was caught by this|
# |         |            |             | round's own re-run: `BallStateType`'s six    |
# |         |            |             | real members came back MISSING, not extra,   |
# |         |            |             | until comments were stripped before the      |
# |         |            |             | split. **M11:** the dangling line number was |
# |         |            |             | recovered by an unconditional-break search of|
# |         |            |             | the WHOLE FILE TEXT, so it could report a    |
# |         |            |             | prose occurrence outside every fence, at the |
# |         |            |             | wrong line, and collapse two distinct fence  |
# |         |            |             | sites onto one finding. Every fence's file   |
# |         |            |             | offset is now carried through a join/offset  |
# |         |            |             | table computed once per file; a failed lookup|
# |         |            |             | raises rather than printing the literal      |
# |         |            |             | string 'None'. **M13:** `DCC =               |
# |         |            |             | _load_consistency()` ran at IMPORT TIME — a  |
# |         |            |             | missing sibling gave a raw traceback and exit|
# |         |            |             | 1, indistinguishable from a real finding, and|
# |         |            |             | fired even for `--help`; a renamed helper    |
# |         |            |             | failed mid-scan after partial output.        |
# |         |            |             | Deferred to a validated loader called once   |
# |         |            |             | from main(), checked against a five-name     |
# |         |            |             | contract before scan() prints anything. Its  |
# |         |            |             | own docstring's PRECONDITION ("computed on   |
# |         |            |             | the same frozen-history-blanked text") was   |
# |         |            |             | violated by this caller passing RAW text —   |
# |         |            |             | honoured directly (blank first, then call    |
# |         |            |             | record_regions) rather than replaced with a  |
# |         |            |             | new assertion. **M14:** CHECK 1 and CHECK 2  |
# |         |            |             | fused into one exit code, and CHECK 2 had NO |
# |         |            |             | decline accounting at all while CHECK 1      |
# |         |            |             | spends forty header lines establishing that  |
# |         |            |             | every decline is printed. CHECK 2 now returns|
# |         |            |             | exit 3 when it alone fails (1 still wins when|
# |         |            |             | both fail — the long-standing code), plus its|
# |         |            |             | own "N examined, M skipped" line across four |
# |         |            |             | named buckets (fence-untagged,               |
# |         |            |             | no-declared-type, filename-shaped,           |
# |         |            |             | type-not-declared-here). **L1:** eight       |
# |         |            |             | parallel per-binary tables, one already dead |
# |         |            |             | — a `sed` row in the needs-a-file set,       |
# |         |            |             | unreachable since `sed` was dropped from     |
# |         |            |             | ALLOWED_CMDS at round 9, while               |
# |         |            |             | `_KNOWN_BINARIES` still listed `sed` for the |
# |         |            |             | OPPOSITE purpose. Consolidated into one      |
# |         |            |             | `Binary` record per binary; ALLOWED_CMDS is  |
# |         |            |             | now DERIVED from its keys, so a dropped      |
# |         |            |             | binary cannot leave a stray row in any other |
# |         |            |             | table again. Every documented exploit        |
# |         |            |             | re-proven refused after consolidation (an awk|
# |         |            |             | pipe-to-sh, `sort -oFILE`, `git branch -D`,  |
# |         |            |             | `sort --compress-program=`, an operand       |
# |         |            |             | escaping the repo root), each with zero side |
# |         |            |             | effects on a live fixture repo, and the full |
# |         |            |             | 46-case decision battery is BYTE-IDENTICAL   |
# |         |            |             | before and after. **L2:** the safety gate's  |
# |         |            |             | `claim.head` used a naive whitespace split   |
# |         |            |             | disagreeing with the quote-aware tokenizer   |
# |         |            |             | validation actually uses; `` `"grep" -c '^A' |
# |         |            |             | f` `` — a perfectly runnable, quoted command |
# |         |            |             | — was silently IGNORED: 0 executed, every    |
# |         |            |             | decline bucket at 0. `head` is now           |
# |         |            |             | tokenize-derived; a claim whose command does |
# |         |            |             | not tokenize at all is declined by the SAME  |
# |         |            |             | name parse_pipeline already uses for it,     |
# |         |            |             | never dropped. **L3:** corrected a docstring |
# |         |            |             | promising a `files_with_fences` return value |
# |         |            |             | the function has never actually returned,    |
# |         |            |             | deleted a near-miss disjunct fully subsumed  |
# |         |            |             | by its own sibling (provably: it can fire    |
# |         |            |             | only when the two are textually identical),  |
# |         |            |             | added the missing File/Modified/Author header|
# |         |            |             | fields, and defaulted --repo to '.' —        |
# |         |            |             | matching every other checker in tools/.      |
# |         |            |             | **L4:** --quiet used to suppress the coverage|
# |         |            |             | counts while the PASS line still cited them  |
# |         |            |             | as "stated above"; now suppresses only the   |
# |         |            |             | itemized per-claim decline list, never the   |
# |         |            |             | counts or the verdict. **L5:** `\d[\d,]*` let|
# |         |            |             | Python's `int()` silently NORMALISE a        |
# |         |            |             | malformed transcription — "1,2,3" as 123,    |
# |         |            |             | "007" as 7 — into a false reproduction. Both |
# |         |            |             | sides now require a well-formed integer      |
# |         |            |             | literal (proper thousands grouping, no       |
# |         |            |             | leading zero); the command side additionally |
# |         |            |             | accepts an optional leading `-`, so a        |
# |         |            |             | negative answer (awk can print one) is       |
# |         |            |             | COMPARED — surfacing a real mismatch — rather|
# |         |            |             | than declined unreadable. Live tree unchanged|
# |         |            |             | in VERDICT (605 surfaces, 6 executed / floor |
# |         |            |             | 4, PASS, exit 0) but not in accounting:      |
# |         |            |             | declines 172 -> 181 (nine newly-NAMED        |
# |         |            |             | untokenizable spans that were previously     |
# |         |            |             | silently ignored prose, none a real command —|
# |         |            |             | L2's fix); the dangling check stays at 0     |
# |         |            |             | findings with 348 declared types now         |
# |         |            |             | considered (M10, previously uncounted) and a |
# |         |            |             | new 1,882-reference / 1,684-skipped          |
# |         |            |             | accounting (M14, previously nonexistent).    |
# |         |            |             | Every delta reproduced on a fixture in both  |
# |         |            |             | directions before landing; siblings          |
# |         |            |             | (doc-consistency-check.py,                   |
# |         |            |             | recurring-defect-lint.py) re-run green.      |
# | 1.8     | 2026-08-22 | Claude Code | ONE-LINE FIX, found by the new suite         |
# |         |            |             | tools/tests/test_doc_claim_check.py while it |
# |         |            |             | was being written: ERR_TABLE_ROW carried no  |
# |         |            |             | re.MULTILINE flag, and it is used as         |
# |         |            |             | `.match(text, ls)` with `ls` a LINE start —  |
# |         |            |             | where a bare `^` matches only at the real    |
# |         |            |             | start of the string, never at a non-zero     |
# |         |            |             | pos. So _err_log_entry_span's table-row      |
# |         |            |             | branch could not fire for any row except one |
# |         |            |             | at byte 0: every ERR index row fell through  |
# |         |            |             | to the prose-section branch and the WHOLE    |
# |         |            |             | `## Error Index` table was bounded as ONE    |
# |         |            |             | entry — exactly what that function's own     |
# |         |            |             | docstring says must not happen ("a ✅         |
# |         |            |             | anywhere in its 213 rows [would] resolve     |
# |         |            |             | every one of them"). Live at the time of the |
# |         |            |             | fix: 191 of the 213 index rows carry a ✅, so |
# |         |            |             | a stale claim in ANY index row, resolved or  |
# |         |            |             | not, would have been excused instead of      |
# |         |            |             | reported. Latent rather than active today —  |
# |         |            |             | no index-row claim currently mismatches, so  |
# |         |            |             | the live verdict and every printed figure    |
# |         |            |             | are unchanged (605 surfaces, 6 executed /    |
# |         |            |             | floor 4, 181 declines, 0 excused, PASS,      |
# |         |            |             | exit 0). Locked by                           |
# |         |            |             | ExcusalTests.test_an_err_index_table_row_is_ |
# |         |            |             | bounded_to_its_own_line, which was written   |
# |         |            |             | RED against the unfixed tool and re-verified |
# |         |            |             | red by mutation afterwards.                  |
# | 1.9     | 2026-08-22 | Claude Code | Round 18 (H12, H14) plus the OS-ENFORCED     |
# |         |            |             | CHILD LIMITS the owner approved on the       |
# |         |            |             | evidence that this class has now recurred    |
# |         |            |             | FOUR consecutive rounds, each time inside    |
# |         |            |             | the previous round's fix. **H12:** long      |
# |         |            |             | options were denied by EXACT core, but       |
# |         |            |             | getopt_long accepts any unambiguous PREFIX,  |
# |         |            |             | so `--output` was denied while `--o=`,       |
# |         |            |             | `--out=`, `--outpu=` and `--compress-progr=` |
# |         |            |             | were not. Reproduced under a printed PASS:   |
# |         |            |             | `sort -S 1 --compress-progr=/…/pwn.sh        |
# |         |            |             | big.txt \| wc -l` EXECUTED an attacker       |
# |         |            |             | script on the runner; `sort --o=/tmp/OUTSIDE |
# |         |            |             | data.txt` wrote outside the checkout (path   |
# |         |            |             | confinement could not see it — the target is |
# |         |            |             | an option VALUE, H14); `sort --outpu=CANARY  |
# |         |            |             | data.txt` wrote inside it. Fixed as a PREFIX |
# |         |            |             | rule over each binary's own denied long      |
# |         |            |             | names (and git's globals), derived from      |
# |         |            |             | denied_flags/denied_prefixes rather than     |
# |         |            |             | listed again. Re-proven after at five        |
# |         |            |             | abbreviation lengths, each declined and      |
# |         |            |             | NAMED with no canary, while `sort            |
# |         |            |             | --numeric-sort`, `grep --count`,             |
# |         |            |             | `grep -c --regexp=a`, `wc --lines` and       |
# |         |            |             | `git grep --count` still execute. **H14:**   |
# |         |            |             | escaping_operand() skipped every token       |
# |         |            |             | starting with `-`, so an ATTACHED option     |
# |         |            |             | value never reached the containment test.    |
# |         |            |             | Reproduced, all executed and COMPARED:       |
# |         |            |             | `grep -c --file=/etc/hostname data.txt`,     |
# |         |            |             | `grep -c -f/etc/hostname data.txt`,          |
# |         |            |             | `git --git-dir=../other/.git rev-list        |
# |         |            |             | --count HEAD`, and `diff                     |
# |         |            |             | --from-file=/tmp/hostsecret.txt data.txt \|  |
# |         |            |             | wc -l`, whose printed integer is derived     |
# |         |            |             | from a file OUTSIDE the repository — so this |
# |         |            |             | header's own property 4 was false as         |
# |         |            |             | written. The value is now extracted (after   |
# |         |            |             | `=` for long, after the first char for       |
# |         |            |             | short) and given the identical realpath      |
# |         |            |             | test; the in-repo forms `-fpatterns.txt`,    |
# |         |            |             | `--file=patterns.txt`, `-F:` and `-m3` still |
# |         |            |             | execute. Same round, same shape: awk's       |
# |         |            |             | `ENVIRON`/`PROCINFO` are ARRAYS, not calls,  |
# |         |            |             | so AWK_ALLOWED_CALLS structurally could not  |
# |         |            |             | see them and                                 |
# |         |            |             | `awk 'END{print length(ENVIRON["X"])}' f`    |
# |         |            |             | read the runner's environment one integer at |
# |         |            |             | a time — refused flat, beside `\|` and `@`.  |
# |         |            |             | **CHILD RESOURCE LIMITS** (not a finding —   |
# |         |            |             | an owner decision on the recurrence):        |
# |         |            |             | RLIMIT_FSIZE=0, RLIMIT_CPU and RLIMIT_AS set |
# |         |            |             | in the forked child before exec, so          |
# |         |            |             | read-only-ness is enforced by the OS rather  |
# |         |            |             | than asserted by a list. DEFENCE IN DEPTH:   |
# |         |            |             | every allow-list, deny-flag and confinement  |
# |         |            |             | check is unchanged and nothing was relaxed.  |
# |         |            |             | Demonstrated against a hatch fixed by NO     |
# |         |            |             | name here: `git status \| wc -l` passes      |
# |         |            |             | every check and REWRITES `.git/index` — with |
# |         |            |             | the limits off it executed and the index     |
# |         |            |             | digest changed; with them on it died on the  |
# |         |            |             | write, was declined by name, and the index   |
# |         |            |             | was byte-identical. Same for `sort -S 1      |
# |         |            |             | big.txt \| wc -l`, whose spill file is a     |
# |         |            |             | write the allow-list permits. Limits stated  |
# |         |            |             | honestly in SAFETY rule 6: no effect on      |
# |         |            |             | READS, none on EXECUTION, FSIZE=0 stops a    |
# |         |            |             | file GROWING but not an empty create or a    |
# |         |            |             | truncate (measured), POSIX-only with a       |
# |         |            |             | printed UNAVAILABLE line where `resource` is |
# |         |            |             | missing, and a killed child can leave a      |
# |         |            |             | partial artefact (`git status` leaves a      |
# |         |            |             | zero-byte `.git/index.lock`) — recorded, not |
# |         |            |             | worked around by exempting git. A limit kill |
# |         |            |             | is a NAMED decline (SIGXFSZ/SIGXCPU/SIGKILL  |
# |         |            |             | mapped in _LIMIT_KILL), never a crash and    |
# |         |            |             | never a silent zero that gets compared. The  |
# |         |            |             | parent's stdin staging is unaffected (the    |
# |         |            |             | PARENT writes the temp file) and stdout is a |
# |         |            |             | pipe, so the output cap and reader thread    |
# |         |            |             | are untouched — verified, incl. the live     |
# |         |            |             | `git grep -c 'CROSS-PENDING' 9b841d1^ --     |
# |         |            |             | docs/specs \| awk …` claim, which still      |
# |         |            |             | executes under the limits. Live tree         |
# |         |            |             | unchanged: 605 surfaces, 6 executed / floor  |
# |         |            |             | 4, 181 declines, 0 excused, PASS, exit 0;    |
# |         |            |             | the 71-test suite passes unmodified; both    |
# |         |            |             | sibling tools re-run green.                  |
# | 1.10    | 2026-08-22 | Claude Code | Round 19 (H13, H15, H16, H17, H18), each     |
# |         |            |             | proven by reproduction before the fix and in |
# |         |            |             | BOTH directions after. **H13 — the load-     |
# |         |            |             | bearing one, and it CHANGED THE HEADLINE     |
# |         |            |             | NUMBERS ON PURPOSE.** `dated_record_regions` |
# |         |            |             | called `blank_frozen_history` only as INPUT  |
# |         |            |             | to `record_regions` and then threw the       |
# |         |            |             | blanking away, re-listing                    |
# |         |            |             | `frozen_chain_span` by hand. But that        |
# |         |            |             | function freezes the header chain AND every  |
# |         |            |             | `Version History` section, so the sibling    |
# |         |            |             | excused a VH row and this tool GATED on it — |
# |         |            |             | falsifying the docstring that says the       |
# |         |            |             | import exists so the two cannot disagree     |
# |         |            |             | about which bytes are frozen. VH is 5.9% of  |
# |         |            |             | the corpus and FOUR of the six executed      |
# |         |            |             | claims sat inside one, two of them `ls -d    |
# |         |            |             | src/*/ \| wc -l` -> 35 in rows whose         |
# |         |            |             | neighbours read "left as written per the do- |
# |         |            |             | not-rewrite-history convention": CI would    |
# |         |            |             | have gone red on two correct historical      |
# |         |            |             | records the day a 36th assembly landed,      |
# |         |            |             | verbatim the hazard the dated-record model   |
# |         |            |             | exists to prevent. Spans are now DERIVED     |
# |         |            |             | from the blanking itself (`_blanked_runs`,   |
# |         |            |             | line-wise so the whole corpus costs          |
# |         |            |             | nothing), which is one computation instead   |
# |         |            |             | of two definitions. The currency pierce is   |
# |         |            |             | unchanged — a VH row saying the command      |
# |         |            |             | returns N *now* still reports. Proven: the   |
# |         |            |             | historical row mismatches at HEAD and is     |
# |         |            |             | EXCUSED after; the "now returns" row still   |
# |         |            |             | MISMATCHES; a live sentence outside the      |
# |         |            |             | section still gates. **The honest            |
# |         |            |             | consequence, stated rather than engineered   |
# |         |            |             | away: live coverage is 1, not 6.** Executed  |
# |         |            |             | is still 6 — a record claim still runs and   |
# |         |            |             | still prints — but five of the six can never |
# |         |            |             | fail CI, so the printed output now carries   |
# |         |            |             | "... of which LIVE (can gate)" beside the    |
# |         |            |             | headline and a new MIN_LIVE_CLAIMS=1 floor   |
# |         |            |             | gates on it (exit 2). MIN_EXECUTED_SLACK is  |
# |         |            |             | deliberately NOT applied at a measurement of |
# |         |            |             | 1: any slack puts that floor at zero, which  |
# |         |            |             | is the vacuous pass the floor exists to      |
# |         |            |             | deny. Region coverage is now a UNION, not a  |
# |         |            |             | sum — the spans can overlap since VH sits    |
# |         |            |             | inside the log body. **H16 — the date guard  |
# |         |            |             | was one case of a general defect.**          |
# |         |            |             | `LeadingValueShape.rejects()` refused        |
# |         |            |             | exactly two prefixes, so every compound      |
# |         |            |             | number whose tail digits abut a              |
# |         |            |             | parenthesised command bound as a stated      |
# |         |            |             | value: `§2.2.2 (`cmd`)` -> 2 and `v1.73      |
# |         |            |             | (`cmd`)` -> 73, both correct sentences the   |
# |         |            |             | tool would have failed CI on, and the first  |
# |         |            |             | is LIVE at code-standards/section-3.md:1140, |
# |         |            |             | escaping only because                        |
# |         |            |             | `document_relative_operand` declined it for  |
# |         |            |             | an unrelated reason. Generalised to "a       |
# |         |            |             | stated value begins its own token": the      |
# |         |            |             | character before it, emphasis stripped, may  |
# |         |            |             | not be `.`, `#` or `§`. 216 of 429 leading-  |
# |         |            |             | value binds on this tree were section,       |
# |         |            |             | version, spec, heading or decimal tails;     |
# |         |            |             | total binds 1734 -> 1518, NOT ONE new        |
# |         |            |             | mismatch, all six executed claims unchanged, |
# |         |            |             | and the three genuine leading-value forms    |
# |         |            |             | still bind and reproduce. **H15 — a runnable |
# |         |            |             | claim whose binary was on neither curated    |
# |         |            |             | list was dropped with no bucket, no count    |
# |         |            |             | and no line.** `check_claim` returned        |
# |         |            |             | ("ignored", None) and the census could not   |
# |         |            |             | recover it, because the span WAS bound. So   |
# |         |            |             | `comm -12 a b \| wc -l`, `tac`, `pcregrep`,  |
# |         |            |             | `nl`, `du`, `tree` were invisible on BOTH    |
# |         |            |             | routes and two header sentences were false — |
# |         |            |             | the blind spot being a hand-written list,    |
# |         |            |             | the shape the SAFETY section names as this   |
# |         |            |             | file's recurring root error.                 |
# |         |            |             | `command_shaped` is a positive SHAPE test    |
# |         |            |             | now: a binary-shaped head plus an argument   |
# |         |            |             | only a command takes (option, filename, real |
# |         |            |             | path). Head shape ALONE was measured first   |
# |         |            |             | and rejected — it took the census from 132   |
# |         |            |             | to 1297, drowning it in prose; with the      |
# |         |            |             | argument test, 132 -> 140 and nothing        |
# |         |            |             | previously named is lost. `_KNOWN_BINARIES`  |
# |         |            |             | survives as a supplement that can only ADD   |
# |         |            |             | recognition, never gate. Latent (0 live      |
# |         |            |             | instances), so this corrects a false         |
# |         |            |             | coverage statement rather than catching a    |
# |         |            |             | live defect. **H17 — CHECK 2 had no coverage |
# |         |            |             | floor, and blinding it completely still      |
# |         |            |             | printed PASS and exited 0.** Reproduced on   |
# |         |            |             | the live tree: one mutation making           |
# |         |            |             | DECL_CLASS match nothing gives "references   |
# |         |            |             | examined: 1882 (1882 skipped)", PASS, exit   |
# |         |            |             | 0, all 71 tests green — the vacuous-pass     |
# |         |            |             | class fixed for CHECK 1 at round 16 and      |
# |         |            |             | never applied to the check whose counters    |
# |         |            |             | were added without a gate. Also reachable    |
# |         |            |             | with no code change: a corpus writing ```C#  |
# |         |            |             | zeroes it. CHECK 2 now returns its coverage  |
# |         |            |             | and gets a two-part floor folded into the    |
# |         |            |             | same `blocked` list (exit 2). The floors are |
# |         |            |             | SHARES, not the absolute counts the finding  |
# |         |            |             | proposed, and that is a deliberate departure |
# |         |            |             | recorded here: an absolute floor derived     |
# |         |            |             | from this tree (101 files, 198 references)   |
# |         |            |             | is one no smaller corpus can meet, including |
# |         |            |             | this file's own fixtures, where the intended |
# |         |            |             | verdict is PASS on one file holding one      |
# |         |            |             | fence. Measured 0.40 and 0.11, floored at    |
# |         |            |             | 0.20 and 0.05 — half, the same headroom rule |
# |         |            |             | MIN_EXECUTED_SLACK expresses — plus a zero-  |
# |         |            |             | denominator guard so "fenced files but not   |
# |         |            |             | one examinable reference" is caught by shape |
# |         |            |             | rather than by ratio. Both blindings now     |
# |         |            |             | exit 2 and name themselves. **H18 —          |
# |         |            |             | ANSWER_KINDS was not an extension point; the |
# |         |            |             | documented way to add one crashed scan()     |
# |         |            |             | with an uncaught KeyError, exiting 1** — the |
# |         |            |             | code meaning "a document is wrong", the      |
# |         |            |             | collision round 17 (M14) spent a fix         |
# |         |            |             | separating. Reproduced with a throwaway      |
# |         |            |             | `PairAnswer` whose bucket is `not-a-pair`,   |
# |         |            |             | because `declined` was built from            |
# |         |            |             | DECLINE_BUCKETS, a table the SEAM 1 banner   |
# |         |            |             | never mentions. Both tables are derived now: |
# |         |            |             | ANSWER_KINDS from the shapes that name it,   |
# |         |            |             | the decline order from DECLINE_BUCKETS plus  |
# |         |            |             | each kind's bucket, with `record_decline`    |
# |         |            |             | counting an unforeseen bucket rather than    |
# |         |            |             | raising. The stated-value grammar moved onto |
# |         |            |             | the answer kind and each shape's regex is    |
# |         |            |             | built from it, so shapes and answers stop    |
# |         |            |             | being a cross-product. Re-executed the       |
# |         |            |             | banner's promise on the live corpus by       |
# |         |            |             | registering the throwaway kind in the file   |
# |         |            |             | itself: 2 answer kinds, a `not-a-pair`       |
# |         |            |             | column, PASS, exit 0, no other edit          |
# |         |            |             | anywhere. The banner is corrected rather     |
# |         |            |             | than left over-promising, and names the      |
# |         |            |             | limit that REMAINS — the text around a       |
# |         |            |             | shape's value placeholder is still written   |
# |         |            |             | for digits. **Live tree: 605 surfaces, 6     |
# |         |            |             | executed (floor 4) of which 1 LIVE (floor    |
# |         |            |             | 1), 187 declines (unlisted-binary 20->19,    |
# |         |            |             | did-not-run 1->0, not-a-single-integer 7->6, |
# |         |            |             | unrecognised-shape 132->141), 0 excused,     |
# |         |            |             | PASS, exit 0. The 71-test suite passes       |
# |         |            |             | UNMODIFIED — no test premise was             |
# |         |            |             | invalidated. Both sibling tools re-run       |
# |         |            |             | green.**                                     |
# | 1.11    | 2026-08-22 | Claude Code | Round 20 (M15, M16, M19-M24, L6-L9), each    |
# |         |            |             | proven by reproduction before the fix and in |
# |         |            |             | BOTH directions after; M23 done LAST, per    |
# |         |            |             | its own risk note. **M15**:                  |
# |         |            |             | unrecognised_spans() used a second, naive    |
# |         |            |             | cmd.split()[0] head parser disagreeing with  |
# |         |            |             | the one Claim.head already tokenizes (round  |
# |         |            |             | 17, L2), and dropped every FORBIDDEN-bearing |
# |         |            |             | span via a bare continue. Extracted the      |
# |         |            |             | shared command_head() tokenizer both now     |
# |         |            |             | call; a FORBIDDEN span lands in              |
# |         |            |             | unrecognised-shape with its own reason       |
# |         |            |             | (unrecognised_span_reason()) instead of      |
# |         |            |             | vanishing. **M16**: check_claim()'s          |
# |         |            |             | head-is-None branch declined every           |
# |         |            |             | untokenizable span unconditionally;          |
# |         |            |             | measured, 9 of 10 unsafe-bucket entries were |
# |         |            |             | markdown prose, not commands. Gated behind   |
# |         |            |             | the same command_shaped() test the           |
# |         |            |             | unlisted-binary branch already applies — 8   |
# |         |            |             | of 9 false declines now read ignored (the    |
# |         |            |             | residual one trips command_shaped's own      |
# |         |            |             | pre-existing _command_operand path-token     |
# |         |            |             | quirk on 'v2.104/v2.105', not a new defect). |
# |         |            |             | **M19**: the 'awk is kept — 2 of 3 claims    |
# |         |            |             | executed are awk' rationale (stated twice)   |
# |         |            |             | and the UNRECOGNISED_RADIUS saturation curve |
# |         |            |             | (86/117/138/143/145/145) were both wrong     |
# |         |            |             | against today's tree — re-measured: only 1   |
# |         |            |             | of 3 DISTINCT executed commands uses awk,    |
# |         |            |             | and it is not this run's one LIVE claim; the |
# |         |            |             | true curve is 97/126/148/153/155/155. Both   |
# |         |            |             | sites reworded to state the fact once,       |
# |         |            |             | qualitatively, and cite each other, rather   |
# |         |            |             | than repeat a number that goes stale.        |
# |         |            |             | **M20**: the coverage floor counted          |
# |         |            |             | INSTANCES (6), half of which are one         |
# |         |            |             | revision-pinned command quoted three times   |
# |         |            |             | and so can never drift or fail. Floored on   |
# |         |            |             | DISTINCT COMMANDS now (checked_commands,     |
# |         |            |             | printed alongside the instance count); floor |
# |         |            |             | re-derived at 3 distinct minus 1 slack = 2,  |
# |         |            |             | since the old slack of 2 would floor a base  |
# |         |            |             | this small at 1, the near-zero-protection    |
# |         |            |             | problem MIN_LIVE_CLAIMS already avoids.      |
# |         |            |             | **M21**: self_contained() accepted a         |
# |         |            |             | grep/awk PATTERN operand that merely         |
# |         |            |             | happened to spell a real repo path,          |
# |         |            |             | fabricating a finding (grep -c 'CLAUDE.md'   |
# |         |            |             | with no file operand at all read empty       |
# |         |            |             | stdin, printed 0, and that 0 was compared    |
# |         |            |             | against a real stated value). Fixed by       |
# |         |            |             | POSITION via new Binary.pattern_operand      |
# |         |            |             | (grep family, awk): the first non-option     |
# |         |            |             | token is excluded from the file search, not  |
# |         |            |             | by content. **M24**: the same function       |
# |         |            |             | rejected grep -rn over a directory with the  |
# |         |            |             | false reason 'would read from an empty       |
# |         |            |             | stdin' — os.path.isdir now accepted          |
# |         |            |             | alongside os.path.isfile; a binary that      |
# |         |            |             | truly cannot read a directory still fails    |
# |         |            |             | downstream at run_pipeline's own             |
# |         |            |             | non-zero-exit decline. **M22**:              |
# |         |            |             | err_log_excused() searched an UNCLIPPED      |
# |         |            |             | sentence window for an excusing date — the   |
# |         |            |             | same defect doc-consistency-check.py's       |
# |         |            |             | historically_marked() was fixed for at       |
# |         |            |             | MARKER_RADIUS; measured 13 spans excusable   |
# |         |            |             | by a date 53 to 689 characters away.         |
# |         |            |             | Intersected the sentence window with         |
# |         |            |             | CURRENCY_RADIUS, mirroring the sibling's own |
# |         |            |             | fix. **L6**: ClaimShape's cmd/value/gap      |
# |         |            |             | group contract was undocumented and          |
# |         |            |             | unvalidated; negation_window() reads         |
# |         |            |             | group('gap') unconditionally. Asserted at    |
# |         |            |             | construction now, with a named               |
# |         |            |             | AssertionError instead of a mid-scan         |
# |         |            |             | IndexError. **L7**: --quiet suppressed CHECK |
# |         |            |             | 2's own coverage counts (spec files with     |
# |         |            |             | code fences, declared types considered) —    |
# |         |            |             | the exact rule CHECK 1 was fixed to honor at |
# |         |            |             | round 17, L4. Both prints moved              |
# |         |            |             | unconditional; there is no itemized CHECK-2  |
# |         |            |             | listing left for --quiet to suppress at all. |
# |         |            |             | **L8**: _map_offset's fail-loud              |
# |         |            |             | AssertionError was uncaught, so a drift      |
# |         |            |             | between the fence-join bookkeeping and the   |
# |         |            |             | code it describes would traceback AFTER      |
# |         |            |             | CHECK 1 had already printed a verdict.       |
# |         |            |             | Caught in scan() and routed through blocked  |
# |         |            |             | (exit 2), reproduced by forcing the raise:   |
# |         |            |             | uncaught before, a clean 'ERROR ... exit 2'  |
# |         |            |             | after. **L9**: the header's 'every claim     |
# |         |            |             | this tool declines to check is printed' is   |
# |         |            |             | false under --quiet (only counted, not       |
# |         |            |             | itemized); reworded to 'COUNTED AND NAMED,   |
# |         |            |             | and ITEMIZED unless --quiet'. **M23 (done    |
# |         |            |             | LAST)**: 10 of 12 denied_prefixes entries    |
# |         |            |             | (rg x3, sort x2, awk x4, git x1) were        |
# |         |            |             | strictly subsumed by the exact-core check —  |
# |         |            |             | _option_cores already reduces --flag=value   |
# |         |            |             | to the bare core --flag, so a --flag= prefix |
# |         |            |             | entry beside an existing bare --flag in      |
# |         |            |             | denied_flags caught nothing new; deleted.    |
# |         |            |             | The two survivors (git's                     |
# |         |            |             | --exec-path=/--upload-pack=) were            |
# |         |            |             | load-bearing only via git_global_denied and  |
# |         |            |             | the generic startswith fallback, because     |
# |         |            |             | their bare cores were missing from git's own |
# |         |            |             | denied_flags for no stated reason — moved    |
# |         |            |             | in, so the exact-core check owns them at     |
# |         |            |             | every argv position, a strict widening. The  |
# |         |            |             | dead 'or core in ("--exec-path",             |
# |         |            |             | "--upload-pack")' disjunct (both already     |
# |         |            |             | members of GIT_GLOBAL_DENIED) and its        |
# |         |            |             | matching redundant tuple argument to         |
# |         |            |             | _denied_long_names were deleted. THE         |
# |         |            |             | FINDING'S FOURTH ASK — deleting the          |
# |         |            |             | post-expansion denied_flag() re-run in       |
# |         |            |             | expand_globs as 'subsumed by the             |
# |         |            |             | hit.startswith("-") refusal' — was           |
# |         |            |             | investigated and REJECTED, per the finding's |
# |         |            |             | own instruction to report what could not be  |
# |         |            |             | preserved with confidence rather than delete |
# |         |            |             | it anyway. Reproduced: uniq tools/doc-*.py   |
# |         |            |             | has ONE pre-expansion operand (uniq's        |
# |         |            |             | OPERAND-COUNT hatch does not fire) and       |
# |         |            |             | expands to two real files, NEITHER starting  |
# |         |            |             | with '-' (the startswith check passes it     |
# |         |            |             | clean too) — only the post-expansion re-run, |
# |         |            |             | seeing the actual two-operand argv, catches  |
# |         |            |             | the write. The 71-green-tests claim behind   |
# |         |            |             | the deletion proposal is real but beside the |
# |         |            |             | point: it is a gap in the SUITE's coverage   |
# |         |            |             | of an operand-COUNT hatch, not evidence the  |
# |         |            |             | code is dead. Kept, with the reproduction    |
# |         |            |             | recorded in a comment beside it. Every named |
# |         |            |             | exploit re-verified refused after every M23  |
# |         |            |             | change: sort -oFILE, sort --output=, sort    |
# |         |            |             | --o=, sort --compress-progr=, git grep       |
# |         |            |             | -O./p.sh, git grep --open-f=, git            |
# |         |            |             | --exec-path= (now caught in BOTH pre- and    |
# |         |            |             | post-subcommand position), git               |
# |         |            |             | --upload-pack=, git branch -D, an operand    |
# |         |            |             | outside the repo, and the uniq multi-glob    |
# |         |            |             | case above — plus the legitimate corpus      |
# |         |            |             | commands (ls -d src/*/, the git grep | awk   |
# |         |            |             | claim) still execute clean. **Live tree: 605 |
# |         |            |             | surfaces, 6 executed (3 distinct commands,   |
# |         |            |             | floor 2) of which 1 LIVE (floor 1), 191      |
# |         |            |             | declines (unsafe 10->2, unrecognised-shape   |
# |         |            |             | 141->153 — the FORBIDDEN-routing and         |
# |         |            |             | quoted-head fixes recovering 12              |
# |         |            |             | previously-invisible spans), 0 excused,      |
# |         |            |             | PASS, exit 0. The 71-test suite passes       |
# |         |            |             | UNMODIFIED — not edited, per the fixer       |
# |         |            |             | boundary. Both sibling tools re-run green.** |
# | 1.12    | 2026-08-22 | Claude Code | Two defects from the automated review of PR  |
# |         |            |             | #330, both independently reproduced against  |
# |         |            |             | the committed tree before and after the fix. |
# |         |            |             | **H1 (security): CLUSTERED SHORT OPTIONS     |
# |         |            |             | DEFEATED PATH CONFINEMENT — the FIFTH        |
# |         |            |             | consecutive round of one class, and the      |
# |         |            |             | fourth to land inside the previous round's   |
# |         |            |             | fix.** Round 18's H14 extracted an attached  |
# |         |            |             | option value as `tok[2:]`, which is the      |
# |         |            |             | value only when the option letter is the     |
# |         |            |             | token's first character. GNU grep parses     |
# |         |            |             | `-cf/etc/hostname` as `-c -f /etc/hostname`  |
# |         |            |             | (`-c` takes no value, `-f, --file=FILE`      |
# |         |            |             | does), so the value is `/etc/hostname` while |
# |         |            |             | this file computed `f/etc/hostname` — a      |
# |         |            |             | RELATIVE path that resolves inside the root  |
# |         |            |             | and passed. Reproduced through the real      |
# |         |            |             | validation chain: the separated              |
# |         |            |             | (`-c -f /etc/hostname`) and attached         |
# |         |            |             | (`-c -f/etc/hostname`) spellings DECLINED,   |
# |         |            |             | the clustered one ACCEPTED with              |
# |         |            |             | escaping_operand → None; and end to end as a |
# |         |            |             | working one-integer read ORACLE over host    |
# |         |            |             | file CONTENTS —                              |
# |         |            |             | `grep -hf/etc/hostname data.txt data2.txt \| |
# |         |            |             | wc -l` reproduced a stated 1 when a fixture  |
# |         |            |             | file held the runner's real hostname and     |
# |         |            |             | FAILED on a wrong guess, i.e. a document     |
# |         |            |             | line interrogating /etc one bit at a time.   |
# |         |            |             | The round-18 OS child limits do NOT cover    |
# |         |            |             | it: every one of them bounds writing, CPU or |
# |         |            |             | memory, and this is a READ. FIXED by         |
# |         |            |             | refusing to model option grammars at all —   |
# |         |            |             | _attached_option_value (one guess) becomes   |
# |         |            |             | _attached_option_values (a tuple), returning |
# |         |            |             | EVERY suffix from index 2 to the end, each   |
# |         |            |             | subjected to the identical realpath test.    |
# |         |            |             | The argument that this is not a sixth        |
# |         |            |             | spelling waiting to happen is structural,    |
# |         |            |             | not an enumeration: for POSIX getopt and GNU |
# |         |            |             | getopt_long alike an attached short-option   |
# |         |            |             | value is `&argv[i][j+1]`, a SUFFIX of the    |
# |         |            |             | token, so the suffix set BOUNDS what any     |
# |         |            |             | dash-token can hand a child, whatever the    |
# |         |            |             | binary's option table says; which suffix the |
# |         |            |             | real binary picks stops mattering because    |
# |         |            |             | all of them were checked. Stated honestly in |
# |         |            |             | the docstring: it is exhaustive over paths   |
# |         |            |             | carried INSIDE a dash-token and says nothing |
# |         |            |             | about a path reaching a child by another     |
# |         |            |             | route (a `key=value` bare operand, a file a  |
# |         |            |             | binary opens on its own, the environment —   |
# |         |            |             | the last two closed for awk by name).        |
# |         |            |             | OVER-REFUSAL MEASURED, NOT ASSUMED, since    |
# |         |            |             | over-refusal is what the extra suffixes buy  |
# |         |            |             | and the floors now gate the exit code: a     |
# |         |            |             | fixture of five legitimate clustered forms   |
# |         |            |             | (grep -rn over a directory, sort -nr, ls     |
# |         |            |             | -la, wc -lc, grep -ic) executes 5 of 5 with  |
# |         |            |             | 0 declines, IDENTICALLY before and after —   |
# |         |            |             | a suffix escapes only by being absolute or   |
# |         |            |             | traversing out with `..`, which no letter    |
# |         |            |             | cluster does. The decline NAMES which value  |
# |         |            |             | escaped, so escaping_operand now returns a   |
# |         |            |             | rendered description rather than a bare      |
# |         |            |             | token; the bare-operand message is unchanged |
# |         |            |             | byte for byte. **H2: _lower_limit RAISED an  |
# |         |            |             | inherited SOFT limit while its own docstring |
# |         |            |             | said it never raises one the runner imposes  |
# |         |            |             | — the guarantee stated in the function whose |
# |         |            |             | whole purpose it is.** It clamped against    |
# |         |            |             | the inherited HARD limit alone. Reproduced   |
# |         |            |             | in a child holding real inherited limits:    |
# |         |            |             | RLIMIT_CPU (10, 100) → (60, 65), six times   |
# |         |            |             | the CPU the runner intended; (10, INF) → the |
# |         |            |             | same, since the infinite-hard path clamped   |
# |         |            |             | nothing at all; RLIMIT_AS (512 MiB, 1 GiB) → |
# |         |            |             | (1 GiB, 1 GiB). After: (10, 10), (10, 10)    |
# |         |            |             | and (512 MiB, 512 MiB), with the untouched   |
# |         |            |             | default host case still (60, 65) / 2 GiB.    |
# |         |            |             | Every limit is now the strictest of {what    |
# |         |            |             | this file asks for, the inherited soft, the  |
# |         |            |             | inherited hard}, so the function can only    |
# |         |            |             | tighten. RLIMIT_FSIZE stays EXACTLY 0 in     |
# |         |            |             | every case measured (inherited INF, and      |
# |         |            |             | inherited (100, 1000)) — and that needed the |
# |         |            |             | new _tightest() rather than min(), because   |
# |         |            |             | RLIM_INFINITY is -1 on Linux and a naive     |
# |         |            |             | min(0, RLIM_INFINITY) would have turned the  |
# |         |            |             | load-bearing write ceiling into UNLIMITED on |
# |         |            |             | any ordinary host; verified by re-running    |
# |         |            |             | the SIGXFSZ kill (a 1-byte write still dies, |
# |         |            |             | file size 0, and SIGXFSZ is still a named    |
# |         |            |             | decline). The `+5` CPU grace collapses on a  |
# |         |            |             | runner that already caps CPU, which costs    |
# |         |            |             | nothing: _LIMIT_KILL names SIGKILL as a      |
# |         |            |             | resource kill too. SAFETY section corrected  |
# |         |            |             | in both places it was now inaccurate — rule  |
# |         |            |             | 4's "every operand" (the fifth respelling)   |
# |         |            |             | and rule 6's limit ledger (the soft-limit    |
# |         |            |             | guarantee, plus the note that these limits   |
# |         |            |             | bound the write/execute half ONLY and rule 4 |
# |         |            |             | is the whole of the read half). **Live tree  |
# |         |            |             | AFTER: byte-identical to before — 605        |
# |         |            |             | surfaces, 6 executed (3 distinct, floor 2)   |
# |         |            |             | of which 1 LIVE (floor 1), 191 declines      |
# |         |            |             | (2 unsafe / 19 unlisted-binary / 8           |
# |         |            |             | not-self-contained / 6 not-a-single-integer  |
# |         |            |             | / 3 negated / 153 unrecognised-shape), 0     |
# |         |            |             | excused, PASS, exit 0. The six executed      |
# |         |            |             | claims are the same six BY NAME, and the one |
# |         |            |             | LIVE claim is still CLAUDE.md:276's          |
# |         |            |             | `ls docs/tracking/*-design.md \| wc -l`, so  |
# |         |            |             | the coverage delta is zero and neither floor |
# |         |            |             | moved. The 71-test suite passes UNMODIFIED — |
# |         |            |             | not edited, per the fixer boundary. Both     |
# |         |            |             | sibling tools re-run green.**                |
# | 1.13    | 2026-08-22 | Claude Code | Two reviewed findings, both the SAME class   |
# |         |            |             | as rounds 9-21 in the ONE dimension round    |
# |         |            |             | 21's suffix rule explicitly disclaimed — a   |
# |         |            |             | path reaching a child by a route that is NOT |
# |         |            |             | a dash-token — and both READS, which the     |
# |         |            |             | round-18 OS child limits do not bound. Each  |
# |         |            |             | reproduced through the real `scan()` path    |
# |         |            |             | before the fix and re-proven after, with the |
# |         |            |             | legitimate case of the same shape still      |
# |         |            |             | executing. **H19: awk `ARGV`/`ARGC` OPENED   |
# |         |            |             | AN ARBITRARY HOST FILE — a read oracle       |
# |         |            |             | confinement is structurally blind to.** The  |
# |         |            |             | awk guard refused `\|`, `@`, system/getline, |
# |         |            |             | ENVIRON/PROCINFO and un-allow-listed calls;  |
# |         |            |             | it never touched ARGV. `awk 'BEGIN{ARGV[     |
# |         |            |             | ARGC++]="/etc/passwd"}END{print NR}'         |
# |         |            |             | data.txt` assigns a path into awk's own      |
# |         |            |             | operand vector, so awk OPENS it as ordinary  |
# |         |            |             | main input: no pipe, no getline, no          |
# |         |            |             | disallowed call, no forbidden character, and |
# |         |            |             | the path is a string LITERAL inside the      |
# |         |            |             | program token rather than an operand, so     |
# |         |            |             | escaping_operand cannot see it while         |
# |         |            |             | self_contained is satisfied by the           |
# |         |            |             | legitimate `data.txt`. BEFORE: scan()        |
# |         |            |             | reached the FAIL block at exit 1 — "document |
# |         |            |             | says 3; command returns 28", the 25 extra    |
# |         |            |             | lines being /etc/passwd — with parse_        |
# |         |            |             | pipeline OK, denied_flag None, self_         |
# |         |            |             | contained True and escaping_operand None,    |
# |         |            |             | i.e. every gate clean individually; and as a |
# |         |            |             | BYTE-level oracle over /etc/hostname using   |
# |         |            |             | only allow-listed calls (`index`). AFTER:    |
# |         |            |             | declined `unsafe`, NAMED, and 0 children     |
# |         |            |             | spawned (measured by spying on               |
# |         |            |             | subprocess.Popen), so the read demonstrably  |
# |         |            |             | does not happen. FIXED by AWK_SPECIAL_       |
# |         |            |             | ARRAYS, replacing AWK_ENV_ARRAYS: a name     |
# |         |            |             | refusal for `ARGV`/`ARGC`, the existing      |
# |         |            |             | `ENVIRON`/`PROCINFO`, and gawk's `SYMTAB`/   |
# |         |            |             | `FUNCTAB` — the symbol table reaches ARGV    |
# |         |            |             | indirectly, so refusing only the direct      |
# |         |            |             | spelling would be the enumerate-the-spelling |
# |         |            |             | error again. A name refusal IS the right     |
# |         |            |             | shape here for the reason the SAFETY section |
# |         |            |             | gives for awk generally: the load-bearing    |
# |         |            |             | rule is the CALL allow-list, and a special   |
# |         |            |             | array is a VARIABLE — this one calls NOTHING |
# |         |            |             | AT ALL, which is why four rounds of awk      |
# |         |            |             | rules missed it. Cost measured, not assumed: |
# |         |            |             | the one live awk claim names none of them    |
# |         |            |             | and still executes, as do a bare             |
# |         |            |             | `awk 'END{print NR}' data.txt` and the       |
# |         |            |             | corpus's own pipeline shape. **H20: `grep    |
# |         |            |             | -R` AND `find -L` FOLLOWED SYMLINKS OUT OF   |
# |         |            |             | THE CHECKOUT.** escaping_operand realpaths   |
# |         |            |             | each operand and refuses one that leaves the |
# |         |            |             | root — correct for a symlink handed over     |
# |         |            |             | directly, blind to TRANSITIVE traversal: an  |
# |         |            |             | in-repo DIRECTORY operand resolves inside    |
# |         |            |             | the root and passes, and the binary walks    |
# |         |            |             | through a symlink inside it. On              |
# |         |            |             | `pull_request` the checkout is the PR head   |
# |         |            |             | and actions/checkout preserves symlinks, so  |
# |         |            |             | `sub/x.md -> /etc/passwd` plus a document    |
# |         |            |             | claim is entirely attacker-controlled.       |
# |         |            |             | BEFORE, each against a committed symlink and |
# |         |            |             | each with its non-following sibling as the   |
# |         |            |             | control: `grep -Rl 'root' sub \| wc -l` -> 1 |
# |         |            |             | (`-rl` -> 0); `find -L deep -name passwd \|  |
# |         |            |             | wc -l` -> 2 (plain find -> 0); `find deep    |
# |         |            |             | -follow ...` -> 2; `rg --follow -l root sub  |
# |         |            |             | \| wc -l` -> 1 (`rg -l` -> 0); and `diff -r  |
# |         |            |             | sub other \| wc -l` reached the FAIL block   |
# |         |            |             | at 30 — a diff OF /etc/passwd. AFTER: all    |
# |         |            |             | declined and named, 0 children spawned.      |
# |         |            |             | FIXED IN TWO HALVES, because the prescribed  |
# |         |            |             | flag denial alone rests on each binary's     |
# |         |            |             | documented default, which is the enumerate-  |
# |         |            |             | what-you-know shape this file keeps filing.  |
# |         |            |             | (i) per-binary denials: grep/egrep/fgrep/rg  |
# |         |            |             | `-R`/`--dereference-recursive`, rg also      |
# |         |            |             | `-L`/`--follow`, find `-L`/`-H`/`-follow`,   |
# |         |            |             | and diff `-r`/`--recursive` — diff for a     |
# |         |            |             | different reason worth stating, since GNU    |
# |         |            |             | diff DEREFERENCES while recursing unless     |
# |         |            |             | `--no-dereference` is passed, so there is no |
# |         |            |             | safe spelling to keep. (ii) the STRUCTURAL   |
# |         |            |             | half, escaping_symlink_under(): no operand   |
# |         |            |             | DIRECTORY may contain a symlink leaving the  |
# |         |            |             | root at any depth, whatever flags were given |
# |         |            |             | and whatever the binary would have done with |
# |         |            |             | them — so lowercase `grep -rl root sub` is   |
# |         |            |             | declined too, because the difference between |
# |         |            |             | it and `-Rl` is a fact about GNU grep and    |
# |         |            |             | not one this tool can enforce. `os.walk(     |
# |         |            |             | followlinks=False)` (a symlinked dir is      |
# |         |            |             | examined, never descended, so no loop can    |
# |         |            |             | hang it), memoised per resolved directory,   |
# |         |            |             | with SYMLINK_WALK_CAP as a fail-SAFE runaway |
# |         |            |             | guard (hitting it declines). Measured on the |
# |         |            |             | live tree: 36 walks, 12 ms total, whole run  |
# |         |            |             | 4.0 s -> 4.2 s. **THE REQUIREMENT-2 SWEEP    |
# |         |            |             | FOUND A THIRD ROUTE, and one instance of it  |
# |         |            |             | was LIVE, not latent: a path taken from      |
# |         |            |             | another file's BYTES.** `wc -l --files0-from |
# |         |            |             | =F` reads the files named inside F. It       |
# |         |            |             | escapes needs_file because self_contained    |
# |         |            |             | inspects only the FIRST pipeline segment, so |
# |         |            |             | `cat data.txt \| wc -l --files0-from=l2.txt` |
# |         |            |             | EXECUTED (2 children spawned) and read       |
# |         |            |             | /etc/passwd; it was declined only because wc |
# |         |            |             | prints "25 /etc/passwd" rather than a bare   |
# |         |            |             | integer, which one `\| cut -d' ' -f1` undoes |
# |         |            |             | — luck, not a rule. THE SAME FAMILY ON       |
# |         |            |             | `sort` WAS WORSE, and it corrects this       |
# |         |            |             | round's own first guess that only wc's was   |
# |         |            |             | live: `cat data.txt \| sort --files0-from=   |
# |         |            |             | l2.txt \| wc -l` reached the FAIL BLOCK at   |
# |         |            |             | 25 — the line count of /etc/passwd, three    |
# |         |            |             | children spawned — because self_contained    |
# |         |            |             | never runs on a second segment.              |
# |         |            |             | `find -files0-from list.txt -name passwd \|  |
# |         |            |             | wc -l` reproduced a stated 2 out of /etc in  |
# |         |            |             | FIRST position. All three declined and named |
# |         |            |             | after, 0 children. No operand check of any   |
# |         |            |             | kind can reach these, so they are refused by |
# |         |            |             | NAME and rule 4 now labels that leg its weak |
# |         |            |             | one rather than folding it in. Sweep         |
# |         |            |             | RESIDUALS, named because a named residual is |
# |         |            |             | worth more than a silent one: a recursion    |
# |         |            |             | with NO operand (covered by the flag denials |
# |         |            |             | and needs_file, not by the walk); a binary's |
# |         |            |             | own config/dot-files (git `.git/config`, rg  |
# |         |            |             | `RIPGREP_CONFIG_PATH`), which no DOCUMENT    |
# |         |            |             | can point anywhere; `ls -R`/`stat -L`, safe  |
# |         |            |             | only because ls prints names and both are    |
# |         |            |             | already covered by (a)+(b); `find -printf    |
# |         |            |             | '%l'`, which leaks an in-repo symlink's      |
# |         |            |             | TARGET STRING and is covered by (b) whenever |
# |         |            |             | that target escapes; and TOCTOU between the  |
# |         |            |             | walk and exec, against an external writer    |
# |         |            |             | this file has never claimed to defend.       |
# |         |            |             | SAFETY section rewritten where these         |
# |         |            |             | findings made it false: the awk hatch list   |
# |         |            |             | (no ARGV), rule 1 (special arrays), rule 4   |
# |         |            |             | (restated as what it ENFORCES — (a) operands |
# |         |            |             | and dash-token suffixes, (b) no escaping     |
# |         |            |             | symlink under an operand directory, (c) the  |
# |         |            |             | named refusals — instead of "a command may   |
# |         |            |             | read the checkout and nothing else"), and a  |
# |         |            |             | new chain paragraph stating the              |
# |         |            |             | generalisation the rounds 9-21 chain had     |
# |         |            |             | been drawing too narrowly: THE SET OF ARGV   |
# |         |            |             | TOKENS IS NOT THE SET OF PATHS A CHILD       |
# |         |            |             | OPENS. **Live tree AFTER: byte-identical to  |
# |         |            |             | before — 605 surfaces, 6 executed (3         |
# |         |            |             | distinct, floor 2) of which 1 LIVE (floor    |
# |         |            |             | 1), 191 declines in the same eight buckets,  |
# |         |            |             | 0 excused, PASS, exit 0. Coverage delta is   |
# |         |            |             | ZERO and neither floor moved: the live       |
# |         |            |             | corpus quotes only `grep -r`/`grep -rn`,     |
# |         |            |             | plain `find`, and an awk program naming no   |
# |         |            |             | special array, and it contains no symlinks   |
# |         |            |             | at all. The 121-test suite passes            |
# |         |            |             | UNMODIFIED — not edited, per the fixer       |
# |         |            |             | boundary. Both sibling tools re-run green.** |
# | 1.14    | 2026-08-22 | Claude Code | Round 23 — five reviewed findings, each      |
# |         |            |             | reproduced before and after, and one of them |
# |         |            |             | a FALSE PASS. **H22 (in the sibling, so both |
# |         |            |             | tools agree): `frozen_chain_span` ran to the |
# |         |            |             | NEXT MARKDOWN HEADING, and a chain is        |
# |         |            |             | inserted INTO a file's header field block,   |
# |         |            |             | so everything between the last chain entry   |
# |         |            |             | and that heading — the file's own present-   |
# |         |            |             | tense status block — was frozen by position  |
# |         |            |             | alone.** On README.md that is lines 556-565, |
# |         |            |             | whose `**Current Stage:**` says "All 26      |
# |         |            |             | approved specs" against a real 53; the same  |
# |         |            |             | shape on file-manifest.md (`**Purpose:**`),  |
# |         |            |             | spec-error-log.md (`**Status:**`, `**Raised  |
# |         |            |             | During:**`) and three design supplements.    |
# |         |            |             | Reproduced on a fixture mirroring README's   |
# |         |            |             | structure: a count-with-command wrong by a   |
# |         |            |             | factor of 33, EXCUSED, exit 0, PASS — the    |
# |         |            |             | worst failure this tool can produce — and    |
# |         |            |             | after the fix the same fixture FAILS at exit |
# |         |            |             | 1 while a genuine chain entry one line above |
# |         |            |             | stays excused. The span now ends at the LAST |
# |         |            |             | marker's entry (a blank line or a new        |
# |         |            |             | `**Label:**`), deliberately not "the first   |
# |         |            |             | non-marker line", because lines BETWEEN      |
# |         |            |             | markers are wrapped entry bodies and this    |
# |         |            |             | repo writes them starting with emphasis and  |
# |         |            |             | a colon. **H23: LIVE was computed by         |
# |         |            |             | POSITION while the excusal needed position   |
# |         |            |             | AND a failed pierce**, so a currency-        |
# |         |            |             | asserted claim inside a record — which gates |
# |         |            |             | perfectly well — was counted non-live.       |
# |         |            |             | Reproduced: one such claim with a wrong      |
# |         |            |             | value printed `LIVE (can gate): 0`, `0       |
# |         |            |             | mismatch(es) EXCUSED`, a FAIL block          |
# |         |            |             | reporting the drift, and "this run could not |
# |         |            |             | have caught drift in any document", exiting  |
# |         |            |             | 2 and demoting a real document defect to a   |
# |         |            |             | tooling error. Hoisted into `would_gate()`,  |
# |         |            |             | used by both sites; after, the same fixture  |
# |         |            |             | reads LIVE 1 and exits 1. **H21: shape 1's   |
# |         |            |             | 40-character gap bound integers out of       |
# |         |            |             | unrelated clauses** — "(`cmd`) was re-run    |
# |         |            |             | and found 2 orphans" and two more correct    |
# |         |            |             | sentences reported as mismatch(2,3) at exit  |
# |         |            |             | 1, invisible to the census because a shape   |
# |         |            |             | had bound them. The arrow route keeps its    |
# |         |            |             | gap (an arrow cannot belong to another       |
# |         |            |             | clause); the verb route admits only          |
# |         |            |             | whitespace, closing markup and ADVERBS (`now |
# |         |            |             | returns`, `no longer returns` both still     |
# |         |            |             | bind and are both in this file's suite), and |
# |         |            |             | the post-verb run admits no words, which is  |
# |         |            |             | what closes the third reproduction           |
# |         |            |             | (`returns, for the 2 tracking files, a count |
# |         |            |             | of 3`). **H25: a span `check_claim` IGNORED  |
# |         |            |             | still reserved itself**, so `curl … >        |
# |         |            |             | out.txt`, `make build && echo 7` and `dotnet |
# |         |            |             | test … 2>&1` reached no bucket at all while  |
# |         |            |             | the same dotnet claim WITHOUT the            |
# |         |            |             | redirection was named — adding a redirection |
# |         |            |             | moved a claim from a named decline to        |
# |         |            |             | invisibility. Ignored `cmd_start`s are now   |
# |         |            |             | subtracted from `bound`; fixture: 0 -> 3     |
# |         |            |             | named. Verified in passing that the report's |
# |         |            |             | "with `bound` emptied the census names all   |
# |         |            |             | of them" is NOT true of an argument-less     |
# |         |            |             | script path — that one is refused by the     |
# |         |            |             | census's own command-shape test, and the     |
# |         |            |             | header now says so. **H24: a live drift-     |
# |         |            |             | capable claim split across a hard wrap was   |
# |         |            |             | invisible to both the shapes and the         |
# |         |            |             | census** — README.md:930-931, a SECOND live  |
# |         |            |             | instance of the very command this file's     |
# |         |            |             | LIVE floor is built around. Shape 4 learned  |
# |         |            |             | `with`, the bare stem `re-derive` and a wrap |
# |         |            |             | prefix, so it now BINDS, executes and        |
# |         |            |             | reproduces (60); the census widens by one    |
# |         |            |             | physical line each way only when the span's  |
# |         |            |             | own line carries no digit. **Live tree: 605  |
# |         |            |             | surfaces, 7 executed (3 distinct, floor 2)   |
# |         |            |             | of which 3 LIVE (2 distinct, floor 1), 224   |
# |         |            |             | declines, 0 excused, PASS, exit 0.**         |
# |         |            |             | Coverage deltas, all accounted: executed 6   |
# |         |            |             | -> 7 (H24 binding README:930, a new INSTANCE |
# |         |            |             | of an already-counted command); LIVE 1 -> 3  |
# |         |            |             | (H23 counting the currency-pierced VH claim  |
# |         |            |             | that always gated, plus H24's new instance); |
# |         |            |             | declines 191 -> 224, which is 8 spans moved  |
# |         |            |             | out of the named buckets into the census by  |
# |         |            |             | H21 and 33 more named by H24's widened       |
# |         |            |             | window, net +41 in one bucket and -8 across  |
# |         |            |             | four (unsafe 2->1, unlisted-binary 19->18,   |
# |         |            |             | not-self-contained 8->6, not-a-single-       |
# |         |            |             | integer 6->2, unrecognised-shape 153->194).  |
# |         |            |             | (Both corrected 2026-08-22 in the recovery   |
# |         |            |             | pass that verified this round: the row read  |
# |         |            |             | "194 declines" and "declines 191 -> 194",    |
# |         |            |             | quoting the unrecognised-shape BUCKET as the |
# |         |            |             | TOTAL, and "12 spans moved out", which       |
# |         |            |             | counted the four region-coverage lines of a  |
# |         |            |             | before/after report diff as declines. The    |
# |         |            |             | eight are: README:1176, code-standards       |
# |         |            |             | /section-2.md:330, CHANGELOG.md:1240 and     |
# |         |            |             | :2789, file-manifest.md:4 and :5 twice, and  |
# |         |            |             | spec-error-log.md:943.) H25 contributes 0    |
# |         |            |             | census entries on THIS corpus — its fixture  |
# |         |            |             | proof (0 -> 3) is the evidence, not a live   |
# |         |            |             | count.                                       |
# |         |            |             | MIN_EXECUTED_CLAIMS unmoved: the floor is on |
# |         |            |             | DISTINCT commands and that is still 3.       |
# |         |            |             | MIN_LIVE_CLAIMS unmoved at 1 WITH ITS        |
# |         |            |             | RESIDUAL RECORDED: both new live instances   |
# |         |            |             | are repetition or a revision-pinned command, |
# |         |            |             | so no new drift-capable command became live, |
# |         |            |             | and raising an instance floor on either is   |
# |         |            |             | what round 20's M20 took the executed floor  |
# |         |            |             | off instances to prevent — but a floor of 1  |
# |         |            |             | can now be met by the pinned claim alone,    |
# |         |            |             | which is a real hole H23 created and which   |
# |         |            |             | wants a distinct-drift-capable-command       |
# |         |            |             | floor, an owner-level call M20 already       |
# |         |            |             | deferred. The census RADIUS CURVE was re-    |
# |         |            |             | measured in the same change (122 / 154 / 186 |
# |         |            |             | / 194 / 199 / 199 at 40 / 60 / 120 / 200 /   |
# |         |            |             | 400 / unbounded), saturating at 200 as       |
# |         |            |             | before. Header and SAFETY text corrected     |
# |         |            |             | where these findings falsified it: the four- |
# |         |            |             | shape list, the derived-blind-spot paragraph |
# |         |            |             | (which now names the two classes that still  |
# |         |            |             | reach no bucket instead of implying there    |
# |         |            |             | are none), and the radius note. The 121-test |
# |         |            |             | suite passes UNMODIFIED — not edited, per    |
# |         |            |             | the fixer boundary; note for its owner that  |
# |         |            |             | its `scan()` harness patches                 |
# |         |            |             | MIN_EXECUTED_CLAIMS but not MIN_LIVE_CLAIMS, |
# |         |            |             | which silently pins every fixture to a live  |
# |         |            |             | floor of 1. Both sibling tools re-run green. |
# | 1.15    | 2026-08-22 | Claude Code | Round 23 RECOVERY AND AUDIT PASS. The round  |
# |         |            |             | above was authored in a session that was     |
# |         |            |             | interrupted before it reported, so every one |
# |         |            |             | of its five findings was re-proved here from |
# |         |            |             | scratch against `git show HEAD:tools/…` —    |
# |         |            |             | the defect reproduced on the PRE-round code  |
# |         |            |             | and the fixed behaviour and its legitimate   |
# |         |            |             | complement measured after. All five hold:    |
# |         |            |             | H21's three sentences bind and report        |
# |         |            |             | mismatch(2,3) before and are named in the    |
# |         |            |             | census after, while arrow/verb/adverb/       |
# |         |            |             | markup/negation phrasings still bind; H22's  |
# |         |            |             | README-shaped fixture is EXCUSED at exit 0   |
# |         |            |             | PASS before and FAILs at exit 1 after, with  |
# |         |            |             | the genuine chain entry one line above still |
# |         |            |             | excused; H23's currency-pierced record claim |
# |         |            |             | prints LIVE 0 + a FAIL block + exit 2 before |
# |         |            |             | and LIVE 1 + exit 1 after; H24 binds         |
# |         |            |             | README.md:930 (60) where nothing bound       |
# |         |            |             | before; H25 takes a fixture from 0 named to  |
# |         |            |             | 3. **What the audit CHANGED.** (1) Shape 4's |
# |         |            |             | wrap-prefix class admitted the ASCII PIPE,   |
# |         |            |             | which is a markdown TABLE CELL separator, so |
# |         |            |             | a three-column row reading "35", then        |
# |         |            |             | "re-derived by", then a backticked command   |
# |         |            |             | bound the 35 out of a neighbouring column    |
# |         |            |             | (reproduced on a fixture) — H21's own        |
# |         |            |             | defect reintroduced one shape over, in the   |
# |         |            |             | commit that fixed it. Measured at 33 shape-4 |
# |         |            |             | bindings with it and 33 without, so it was   |
# |         |            |             | pure risk; removed, `│` and `>` kept and     |
# |         |            |             | each justified by measurement. (2) The LIVE  |
# |         |            |             | line and its blocked message said the        |
# |         |            |             | non-live claims "carry no currency           |
# |         |            |             | assertion", which is false of the ERR-log    |
# |         |            |             | claims — those are excused by the stricter   |
# |         |            |             | resolved-or-dated rule. Both now name the    |
# |         |            |             | TEST (`would_gate`), not one of its two      |
# |         |            |             | branches. (3) FIGURES, every one re-derived  |
# |         |            |             | rather than carried, because the round       |
# |         |            |             | above committed round 20's M19 defect four   |
# |         |            |             | times: the verb-gap refusal count (153 was   |
# |         |            |             | the census total copied into a sentence      |
# |         |            |             | about verb-route matches; measured 147 for   |
# |         |            |             | the gap rule, 118 for _POST_VERB, 184 for    |
# |         |            |             | the two together, of 217); the decline total |
# |         |            |             | (191 -> 224, not 194 — the unrecognised      |
# |         |            |             | BUCKET quoted as the TOTAL); the spans H21   |
# |         |            |             | moved out of the named buckets (8, not 12 —  |
# |         |            |             | the 12 counted four region-coverage lines of |
# |         |            |             | a report diff); "~130 chain entries in       |
# |         |            |             | README.md" in the sibling (CHANGELOG.md's    |
# |         |            |             | 139 under README's name; README holds 36     |
# |         |            |             | entries in total, so 35 is the real figure); |
# |         |            |             | and a `~1,100` transplanted into the header  |
# |         |            |             | from a note about a different population.    |
# |         |            |             | Six instance counts stale at "six" are now   |
# |         |            |             | seven. (4) The MIN_LIVE_CLAIMS note opened   |
# |         |            |             | "the floor is set at 2" and closed "THE      |
# |         |            |             | FLOOR STAYS AT 1" over a constant of 1 —     |
# |         |            |             | corrected to say the MEASUREMENT moved, not  |
# |         |            |             | the floor. **What the audit CONFIRMED and    |
# |         |            |             | did not change:** the radius curve (122 /    |
# |         |            |             | 154 / 186 / 194 / 199 / 199 at 40 / 60 /     |
# |         |            |             | 120 / 200 / 400 / unbounded) reproduces      |
# |         |            |             | exactly; H24's "13 runnable spans in this    |
# |         |            |             | shape" reproduces exactly under its own      |
# |         |            |             | definition (value on the previous physical   |
# |         |            |             | line, runnable head); the sibling's per-file |
# |         |            |             | scope deltas reproduce exactly; H25's        |
# |         |            |             | residual (a bare argument-less script path   |
# |         |            |             | stays invisible with `bound` emptied) is     |
# |         |            |             | real; and MIN_LIVE_CLAIMS = 2 does fail      |
# |         |            |             | exactly 12 cases in the test suite. Live     |
# |         |            |             | tree unchanged by this pass: 605 surfaces, 7 |
# |         |            |             | executed (3 distinct, floor 2) of which 3    |
# |         |            |             | LIVE (2 distinct, floor 1), 224 declines     |
# |         |            |             | (1/18/6/0/2/0/3/194), 0 excused, PASS, exit  |
# |         |            |             | 0. The 121-test suite passes UNMODIFIED.     |
