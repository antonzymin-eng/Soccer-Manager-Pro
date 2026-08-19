#!/usr/bin/env python3
# doc-claim-check.py — execute the verification commands this repo's documents
# quote, and diff the stated value against what they actually print.
#
# Created: August 18, 2026
# Purpose: close the defect class adversarial-review rounds 6-8 kept finding and
#          could not stop finding by review alone.
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
# SAFETY
# ------
# Commands come from documents, so they are untrusted input. Every pipeline
# segment must match ALLOWED_CMDS; anything with redirection, command
# substitution, chaining, or an unlisted binary is refused and counted. Execution
# is read-only by construction (no writing command is on the list), runs with a
# timeout, and never uses a shell for the outer invocation.
#
# Exit codes: 0 = every checkable claim reproduced, 1 = at least one mismatch,
#             2 = usage error.

import argparse
import pathlib
import re
import shlex
import subprocess
import sys

# Read-only binaries only. A command is refused unless EVERY pipeline segment's
# argv[0] is here. `git` is further restricted below to read-only subcommands.
ALLOWED_CMDS = {
    "grep", "egrep", "fgrep", "rg", "ls", "find", "wc", "cat", "head", "tail",
    "sort", "uniq", "sed", "awk", "cut", "tr", "python3", "git", "basename",
    "dirname", "echo", "printf", "stat", "diff",
}
GIT_READONLY = {
    "log", "grep", "show", "ls-files", "diff", "rev-parse", "rev-list",
    "cat-file", "describe", "status", "branch", "tag", "archive", "blame",
}
# Shell metacharacters that make a string more than a simple pipeline.
FORBIDDEN = re.compile(r"[;&><`\n]|\$\(|\|\|")

TIMEOUT_S = 60

# A NEGATED claim states what the command does NOT return — this repo writes
# "the plain `grep …` no longer returns 218" precisely to record a superseded
# figure. Reading that as an assertion inverts the document's meaning and
# reports a defect where the author already did the right thing. Found by this
# tool's own first run against the live tree, which is the complement-testing
# lesson rounds 5-8 kept re-learning: a matcher tuned on the instances that
# motivated it will misread their opposite.
NEGATOR = re.compile(
    r"\b(?:no longer|not|never|n't|instead of|rather than|superseded|was|used to|"
    r"previously|before this|pre-fix)\b", re.I)

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

# A claim: a backticked command, then within a short gap a stated integer,
# introduced by an arrow or a reporting verb. The gap is deliberately tight —
# round 8's H4 showed that a loose lookahead binds across unrelated clauses.
CLAIM = re.compile(
    r"`(?P<cmd>[^`\n]{4,200})`"          # the command
    r"(?P<gap>[^`\n]{0,40}?)"            # short gap, no intervening code span
    r"(?:→|->|\b(?:returns?|reports?|prints?|yields?|gives?)\b)"
    r"[^0-9`\n]{0,18}"
    r"\*{0,2}(?P<value>\d[\d,]*)\*{0,2}",
    re.I)


def parse_pipeline(cmd):
    """Return list of argv lists, or None if the command is not a safe pipeline."""
    # Markdown escapes a literal pipe as `\|` inside table cells, and a large
    # share of this repo's quoted commands live in tables. Unescape it before
    # parsing, or every piped command in a table row is declined as unparseable
    # — which would silently hide exactly the count claims this tool is for.
    # Only `\|` is unescaped: `\*` and friends are real regex inside grep
    # patterns and must survive untouched.
    cmd = cmd.replace("\\|", "|")
    if FORBIDDEN.search(cmd):
        return None
    segments = []
    for part in cmd.split("|"):
        part = part.strip()
        if not part:
            return None
        try:
            argv = shlex.split(part)
        except ValueError:
            return None
        if not argv:
            return None
        if argv[0] not in ALLOWED_CMDS:
            return None
        if argv[0] == "git":
            sub = next((a for a in argv[1:] if not a.startswith("-")), None)
            if sub not in GIT_READONLY:
                return None
        segments.append(argv)
    return segments or None


def run_pipeline(segments, cwd):
    """Run a validated pipeline, return stdout text or None on failure."""
    data = b""
    for i, argv in enumerate(segments):
        try:
            proc = subprocess.run(
                argv, cwd=cwd, input=data, stdout=subprocess.PIPE,
                stderr=subprocess.DEVNULL, timeout=TIMEOUT_S)
        except (OSError, subprocess.SubprocessError):
            return None
        data = proc.stdout
        # grep/find legitimately exit non-zero on "no match"; a later segment
        # still gets the empty input, which is the right answer.
    try:
        return data.decode("utf-8", "replace")
    except Exception:                                    # pragma: no cover
        return None


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


def expand_globs(segments, repo):
    """Expand glob tokens exactly as a shell would: sorted matches relative to
    the repo root, or the literal token when nothing matches (bash default).
    Done here rather than by handing the string to a shell, so the command stays
    a validated argv list and never re-enters a shell parser."""
    out = []
    for argv in segments:
        new = [argv[0]]
        for tok in argv[1:]:
            if GLOB_CH.search(tok):
                hits = sorted(str(q.relative_to(repo)) for q in repo.glob(tok))
                new.extend(hits if hits else [tok])
            else:
                new.append(tok)
        out.append(new)
    return out



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


def scan(repo, quiet=False):
    files = []
    for rel in SURFACES:
        p = repo / rel
        if p.exists():
            files.append((rel, p))
        else:
            print("  MISSING SURFACE: %s" % rel)
    for pat in SURFACE_GLOBS:
        for p in sorted(repo.glob(pat)):
            files.append((str(p.relative_to(repo)), p))

    checked = mismatches = 0
    declined = {"unsafe": 0, "not-self-contained": 0, "not-single-int": 0,
                "negated": 0}
    declined_list = []
    findings = []

    for rel, path in files:
        text = path.read_text(encoding="utf-8", errors="replace")
        for m in CLAIM.finditer(text):
            cmd = m.group("cmd").strip()
            stated = int(m.group("value").replace(",", ""))
            if NEGATOR.search(m.group("gap")) or NEGATOR.search(
                    m.group(0)[m.end("gap") - m.start():]):
                declined["negated"] += 1
                declined_list.append(
                    (rel, text.count("\n", 0, m.start()) + 1, cmd,
                     "claim is NEGATED or historical — states what the command "
                     "does not / no longer return"))
                continue
            # A command must look like one: start with an allowed binary.
            if cmd.split()[0] not in ALLOWED_CMDS:
                continue
            segments = parse_pipeline(cmd)
            if segments is None:
                declined["unsafe"] += 1
                declined_list.append((rel, text.count("\n", 0, m.start()) + 1, cmd,
                                      "not an allow-listed read-only pipeline"))
                continue
            line = text.count("\n", 0, m.start()) + 1
            segments = expand_globs(segments, repo)
            if not self_contained(segments):
                declined["not-self-contained"] += 1
                declined_list.append((rel, line, cmd,
                                      "operand missing from the quoted text — "
                                      "not runnable as written"))
                continue
            got = single_integer(run_pipeline(segments, str(repo)))
            if got is None:
                declined["not-single-int"] += 1
                declined_list.append((rel, line, cmd, "output is not a single integer"))
                continue
            checked += 1
            if got != stated:
                mismatches += 1
                findings.append((rel, line, cmd, stated, got))

    if not quiet:
        print("doc-claim-check — executing the verification commands the documents quote")
        print("  surfaces scanned              : %d" % len(files))
        print("  claims executed and compared  : %d" % checked)
        print("  claims DECLINED (each named)   : %d unsafe / %d not-self-contained /"
              " %d not-a-single-integer / %d negated-or-historical"
              % (declined["unsafe"], declined["not-self-contained"],
                 declined["not-single-int"], declined["negated"]))
        for rel, line, cmd, why in declined_list:
            print("      - %s:%d  %s  [%s]" % (rel, line, cmd[:70], why))
        print("  (a declined claim is UNVERIFIED, not passed — the count is the honest"
              " statement of this tool's coverage)")

    if findings:
        print("\nFAIL — %d stated value(s) the command does not reproduce:" % len(findings))
        for rel, line, cmd, stated, got in findings:
            print("  %s:%d" % (rel, line))
            print("      command : %s" % cmd)
            print("      document says %d; command returns %d" % (stated, got))

    dangling = scan_fence_identifiers(repo, quiet)
    if dangling:
        print("\nFAIL — %d dangling identifier reference(s) in spec code fences:"
              % len(dangling))
        for rel, line, typ, mem, near in dangling:
            print("  %s:%s" % (rel, line))
            print("      `%s.%s` — the file declares `%s` but no member `%s`%s"
                  % (typ, mem, typ, mem,
                     ("; did you mean `%s`?" % near[0]) if near else ""))
        return 1
    if findings:
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
