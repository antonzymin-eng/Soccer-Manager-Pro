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
# It also recognises only ONE claim SHAPE: the command first, then the stated
# value ("`cmd` → 18", "`cmd` returns 18"). The value-first form root CLAUDE.md
# also uses — "8 scripts (`ls tools/*.py`)" — is not matched at all, and so is
# not counted among the declines either. Stated here because round 9 found the
# omission published nowhere: an unrecognised shape is invisible, and invisible
# is the property this tool's whole design is meant to deny itself.
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
# `uniq IN OUT`. The property now rests on three things together: the allow-list,
# DENIED_FLAGS/DENIED_FLAG_PREFIXES refusing those hatches by name, and dropping
# the one binary whose escape lives in its SCRIPT rather than a flag (`sed`).
# `python3` survives restricted to running a `.py` file the checkout already
# contains — CI runs those anyway — never `-c`/`-m`.
#
# Exit codes: 0 = every checkable claim reproduced, 1 = at least one mismatch,
#             2 = usage error.

import argparse
import pathlib
import re
import subprocess
import sys

# Read-only binaries only. A command is refused unless EVERY pipeline segment's
# argv[0] is here. `git` is further restricted below to read-only subcommands.
ALLOWED_CMDS = {
    "grep", "egrep", "fgrep", "rg", "ls", "find", "wc", "cat", "head", "tail",
    "sort", "uniq", "awk", "cut", "tr", "python3", "git", "basename",
    "dirname", "echo", "printf", "stat", "diff",
}
GIT_READONLY = {
    "log", "grep", "show", "ls-files", "diff", "rev-parse", "rev-list",
    "cat-file", "describe", "status", "branch", "tag", "blame",
}
# Shell metacharacters that make a string more than a simple pipeline.
FORBIDDEN = re.compile(r"[;&><`\n]|\$\(|\|\|")

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
#       `awk` is kept because both of this repo's only two executable claims
#       use it, so dropping it would take the tool to zero verified claims —
#       a vacuous pass, the failure class this project files as High. Its two
#       reachable hatches are refused by name instead (`system`, `getline` —
#       every redirection form is already refused by FORBIDDEN, which rejects
#       `>` `<` `` ` `` `;` `&` anywhere in the string, quoted or not).
DENIED_FLAGS = {
    # exact flags
    "python3": {"-c", "-m", "-"},
    "find": {"-exec", "-execdir", "-ok", "-okdir", "-delete", "-fprint",
             "-fprint0", "-fprintf", "-fls"},
    "sort": {"-o", "--output"},
    "uniq": set(),           # guarded by operand count below (uniq IN OUT writes)
    "awk": {"-f", "--file", "--source", "--exec"},
    "head": {"-f", "--follow"},
    "tail": {"-f", "-F", "--follow"},
    "rg": {"--pre", "--pre-glob", "--hostname-bin", "--generate"},
    # `git` is split in two: see GIT_GLOBAL_DENIED. A flag denied ANYWHERE goes
    # here — `git grep -O <cmd>` hands the match list to a command.
    "git": {"-O", "--open-files-in-pager"},
}
# Denied only BEFORE the subcommand, where git parses its own global options.
# The same spellings after it belong to the subcommand and are harmless — and
# refusing them there broke both of this repo's only executable claims, whose
# command is `git grep -c …` (round 9: caught because the fix was re-measured
# against the live corpus rather than accepted on its own reasoning).
GIT_GLOBAL_DENIED = {"-c", "-C", "--exec-path", "--upload-pack"}
# Prefix forms of the same hatches: `--output=x`, `--pre=x`, `-c=x`.
DENIED_FLAG_PREFIXES = {
    "sort": ("--output=",),
    "rg": ("--pre=", "--pre-glob=", "--hostname-bin="),
    "git": ("--exec-path=", "--upload-pack="),
    "awk": ("--source=", "--file="),
}
# awk program text that escapes the process. FORBIDDEN already removes every
# redirection character, so these two are what is left.
AWK_ESCAPES = re.compile(r"\b(?:system|getline)\b")

# Round 9 (M1). CLAIM deliberately matches any backticked span before an arrow,
# because that is how this repo writes a count claim; most such spans are
# IDENTIFIERS, not commands (`SEASON_SAVE_FORMAT_VERSION` **5 → 6**). This
# separates the two so an unrunnable COMMAND is named while a version bump is
# not mistaken for one: command-shaped means "has arguments, and its head token
# is a path or a plausible binary name".
_HEAD_SHAPE = re.compile(r"(?:\.{1,2}/)?[A-Za-z0-9_][A-Za-z0-9_.-]*"
                         r"(?:/[A-Za-z0-9_.-]+)*\Z")
_KNOWN_BINARIES = frozenset((
    "dotnet", "curl", "wget", "ps", "bash", "sh", "zsh", "make", "npm", "npx",
    "node", "python", "pip", "docker", "jq", "tee", "xargs", "sed", "unity",
    "dos2unix", "pwsh", "powershell",
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

# The VALUE-FIRST shape: "8 scripts (`ls tools/*.py`)", "35 (`ls -d src/*/ \| wc
# -l`)". Round 9 (L2) named this as unrecognised AND uncounted and stopped
# there; round 12 closes it, because the live instances are exactly the
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


def denied_flag(argv):
    """The write/execute escape hatch this argv reaches for, or None.

    Round 9 (H1). Read-only-by-allow-list was false: `sed -i`, `find -delete`,
    `python3 -c`, `sort -o`, `rg --pre` and `git -c` all execute or write from
    a binary the list called read-only."""
    name = argv[0]
    exact = DENIED_FLAGS.get(name, ())
    prefixes = DENIED_FLAG_PREFIXES.get(name, ())
    for a in argv[1:]:
        if a in exact:
            return a
        if any(a.startswith(pfx) for pfx in prefixes):
            return a
    if name == "git":
        for a in argv[1:]:
            if not a.startswith("-"):
                break            # the subcommand: globals end here
            if a in GIT_GLOBAL_DENIED or a.startswith(("--exec-path=",
                                                       "--upload-pack=")):
                return a
    if name == "awk":
        script = next((a for a in argv[1:] if not a.startswith("-")), "")
        if AWK_ESCAPES.search(script):
            return "awk program calling system()/getline"
    if name == "python3":
        # Only an in-repo script may run: `-c`/`-m` are refused above, and a
        # path that is not a repo `.py` file is refused here, so a document
        # can at most re-run code the checkout already contains.
        operands = [a for a in argv[1:] if not a.startswith("-")]
        if (not operands or not operands[0].endswith(".py")
                or operands[0].startswith("/")
                or ".." in pathlib.PurePosixPath(operands[0]).parts):
            return "python3 without an in-repo .py script"
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
    pipeline is now DECLINED and named, never compared."""
    data = b""
    for argv in segments:
        try:
            proc = subprocess.run(
                argv, cwd=cwd, input=data, stdout=subprocess.PIPE,
                stderr=subprocess.DEVNULL, timeout=TIMEOUT_S)
        except subprocess.TimeoutExpired:
            return None, "command timed out after %ds" % TIMEOUT_S
        except (OSError, subprocess.SubprocessError):
            return None, "command could not be executed (%s)" % argv[0]
        if proc.returncode != 0 and proc.returncode not in BENIGN_NONZERO.get(
                argv[0], ()):
            return None, ("`%s` exited %d — its output is not treated as an "
                          "answer" % (argv[0], proc.returncode))
        data = proc.stdout
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
            new.extend(hits if hits else [text])
        out.append(new)
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
                "negated": 0, "unlisted-binary": 0, "did-not-run": 0}
    declined_list = []
    findings = []

    for rel, path in files:
        text = path.read_text(encoding="utf-8", errors="replace")
        # Both claim shapes, deduplicated by the position of the command they
        # quote: a span matched by both is one claim, not two.
        matches, claimed_at = [], set()
        for m in CLAIM.finditer(text):
            matches.append((m, m.group(0)[m.end("gap") - m.start():]))
            claimed_at.add(m.start("cmd"))
        for m in CLAIM_VALUE_FIRST.finditer(text):
            if m.start("cmd") not in claimed_at:
                # The negation window must LOOK BACK here, not forward: in this
                # shape the number comes first, so "no longer 3 files (`cmd`)"
                # puts the negator BEFORE the match, where the gap cannot see
                # it. Reusing the forward shape's gap reported such a claim as
                # a mismatch — found by constructing the complement rather than
                # by an instance, which is the check this tool's own header
                # says every matcher here has to survive. Bounded to the line.
                back = max(text.rfind("\n", 0, m.start()) + 1, m.start() - 40)
                matches.append((m, text[back:m.end("gap")]))
        matches.sort(key=lambda pair: pair[0].start())
        for m, negation_window in matches:
            cmd = m.group("cmd").strip()
            stated = int(m.group("value").replace(",", ""))
            # A command must look like one: start with an allowed binary.
            # Round 9 (L1): a backticked run of whitespace matches CLAIM and
            # used to crash here on `"".split()[0]`.
            head = cmd.split()[0] if cmd.split() else ""
            # Round 10: the negation test used to run BEFORE this one, so a
            # backticked IDENTIFIER near a negation ("`SeasonSaveContents` …
            # was not 4") was counted and printed as a declined CLAIM. Five of
            # the eight "negated-or-historical" declines were of that kind —
            # never claims, so counting them overstated how much real coverage
            # the tool was giving up, in the very figure that exists to state
            # that honestly. The command test now gates the negation test.
            if head not in ALLOWED_CMDS:
                # Round 9 (M1): this `continue` was the tool's THIRD decline
                # path and the only silent one, while its header and its
                # file-manifest row both published "every declined claim is
                # counted AND named". Most of what lands here is not a command
                # at all — CLAIM also matches a backticked IDENTIFIER before an
                # arrow (`SEASON_SAVE_FORMAT_VERSION` **5 → 6**), ~1,100 of
                # them — so counting all of it would drown the real signal.
                # Only genuinely command-SHAPED text is counted and named: a
                # head token that is a path or a known binary. Measured on the
                # live tree when this was added: 10, all real (7 ×
                # `tools/recurring-defect-lint.py`, `curl`, `ps … | grep`,
                # `dotnet test --filter`).
                # FORBIDDEN here too, so a backticked EXPRESSION that merely
                # looks path-shaped (`permille/1000f > 0.6f`, live in #33's
                # appendices) is not announced as a rejected binary. It was
                # never a command; naming it would be the mirror of the noise
                # command_shaped exists to suppress.
                if command_shaped(cmd, head) and not FORBIDDEN.search(cmd):
                    declined["unlisted-binary"] += 1
                    declined_list.append(
                        (rel, text.count("\n", 0, m.start()) + 1, cmd,
                         "`%s` is not an allow-listed read-only binary" % head))
                continue
            if NEGATOR.search(m.group("gap")) or NEGATOR.search(
                    negation_window):
                declined["negated"] += 1
                declined_list.append(
                    (rel, text.count("\n", 0, m.start()) + 1, cmd,
                     "claim is NEGATED or historical — states what the command "
                     "does not / no longer return"))
                continue
            segments, why = parse_pipeline(cmd)
            if segments is None:
                declined["unsafe"] += 1
                declined_list.append((rel, text.count("\n", 0, m.start()) + 1,
                                      cmd, why))
                continue
            line = text.count("\n", 0, m.start()) + 1
            segments, why = expand_globs(segments, repo)
            if segments is None:
                declined["unsafe"] += 1
                declined_list.append((rel, line, cmd, why))
                continue
            if not self_contained(segments):
                declined["not-self-contained"] += 1
                declined_list.append((rel, line, cmd,
                                      "operand missing from the quoted text — "
                                      "not runnable as written"))
                continue
            out, why = run_pipeline(segments, str(repo))
            if out is None:
                declined["did-not-run"] += 1
                declined_list.append((rel, line, cmd, why))
                continue
            got = single_integer(out)
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
        print("  claims DECLINED (each named)   : %d unsafe / %d unlisted-binary /"
              " %d not-self-contained / %d did-not-run / %d not-a-single-integer /"
              " %d negated-or-historical"
              % (declined["unsafe"], declined["unlisted-binary"],
                 declined["not-self-contained"], declined["did-not-run"],
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
