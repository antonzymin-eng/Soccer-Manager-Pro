#!/usr/bin/env python3
# test_doc_claim_check.py — the regression suite for tools/doc-claim-check.py.
#
# File:     tools/tests/test_doc_claim_check.py
# Created:  August 22, 2026
# Modified: August 22, 2026
# Author:   Claude Code
# Purpose: pin the safety and coverage properties of the checker that runs in
#          CI against untrusted document text.
#
# WHY THIS EXISTS
# ---------------
# `tools/doc-claim-check.py` executes commands quoted in this repo's documents,
# and `ci.yml` runs it on `pull_request` — so document text reaches a runner.
# Seventeen adversarial-review rounds established its safety properties by
# MANUAL SCRATCH-MIRROR EXPERIMENT ("Verified on a scratch mirror: 10 hatch
# attempts all refused"), and every one of those mirrors is gone. None of it
# was repeatable, and round 17 proved several of those properties had since
# become false. The demonstrated cost, in that round: a one-character
# regression to a matcher's gap bound dropped coverage from 3 executed to 2 and
# from 30 declines to 19, and the tool printed PASS and exited 0.
#
# So every property below is a TEST, and each test is written so that it can
# FAIL: each security case asserts BOTH that the claim was declined and NAMED
# (a silent skip is this project's filed defect class) AND that the exploit's
# own canary — a file, a git ref, a file's contents — is untouched. A test that
# passes against both the fixed and the broken tool is worse than no test; this
# repo has filed that "tautological lock" three times.
#
# WHAT IT DOES NOT DO
# -------------------
# It does not re-verify the checker against the live tree — that is the gate
# step itself (`python3 tools/doc-claim-check.py --repo .`), and a test that
# asserted today's live counts would fail on the next ordinary document edit,
# which is how a suite gets switched off. Every fixture here is a temp dir the
# test builds: nothing reads or writes the repository, nothing needs network,
# and the suite leaves no artefact.
#
# RUNNING IT (either form; the first is what ci.yml runs)
#     python3 tools/tests/test_doc_claim_check.py
#     python3 -m unittest discover -s tools/tests -t tools/tests -v
# `-t` is required: tools/ is not a package, so a bare `discover -s tools/tests`
# raises "Start directory is not importable" rather than running anything —
# a failure mode that reads like a skip.

import contextlib
import importlib.util
import io
import os
import pathlib
import shutil
import subprocess
import sys
import tempfile
import unittest
from unittest import mock

TOOLS_DIR = pathlib.Path(__file__).resolve().parent.parent
TOOL_PATH = TOOLS_DIR / "doc-claim-check.py"


def _load_tool():
    """Import the hyphenated checker by path, the way it imports its own
    sibling. Loading the module and calling its functions directly is what
    keeps this suite fast enough to run on every push — a subprocess per case
    would cost more than the gate it protects. The two cases that are ABOUT
    the process boundary (argv handling, the exit code the shell sees) spawn
    one deliberately."""
    spec = importlib.util.spec_from_file_location("doc_claim_check", TOOL_PATH)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    # The tool defers this import out of module scope (round 17, M13); every
    # excusal path below calls through it, so a failure here is a hard error
    # for the suite rather than a skip.
    err = mod._ensure_consistency_module()
    if err is not None:
        raise RuntimeError("doc-claim-check.py could not load its sibling: %s"
                           % err)
    return mod


M = _load_tool()


def _has(binary):
    return shutil.which(binary) is not None


# Requirement of this suite, and of the tool it tests: a skip is never silent.
# Only binaries a test actually EXECUTES are guarded — most security cases here
# are refused before any process is spawned, so they need nothing on PATH.
HAVE_GREP = _has("grep")
HAVE_PRINTF = _has("printf")
HAVE_GIT = _has("git")

# A file whose counts are unambiguous under every command quoted below:
# three lines, all three containing "a".
DATA_TEXT = "alpha\nbeta\ngamma\n"
TRUE_CMD = "grep -c 'a' data.txt"
TRUE_VALUE = 3


class FixtureCase(unittest.TestCase):
    """A minimal fake repo in a temp dir, plus the three ways this suite drives
    the checker: one claim through `check_claim`, a whole `scan`, and the CLI."""

    def setUp(self):
        self.dir = pathlib.Path(tempfile.mkdtemp(prefix="doc-claim-check-t-"))
        self.addCleanup(shutil.rmtree, self.dir, ignore_errors=True)
        self.write("CLAUDE.md", "# Fixture repo\n")
        self.write("data.txt", DATA_TEXT)

    # -- fixture construction ------------------------------------------------

    def write(self, rel, text):
        p = self.dir / rel
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text(text, encoding="utf-8")
        return p

    def spec_file(self, body, rel="docs/specs/demo/section-1.md"):
        return self.write(rel, "# Demo spec\n\n" + body)

    # -- driving one claim ---------------------------------------------------

    def bind(self, doc_text, rel="CLAUDE.md"):
        """The first claim any shape binds in `doc_text`. Fails loudly when no
        shape binds — a test whose fixture stopped being recognised would
        otherwise assert nothing at all."""
        claims = M.collect_claims(rel, doc_text)
        self.assertTrue(claims, "no claim shape bound this fixture: %r"
                        % doc_text)
        return claims[0]

    def check(self, doc_text, rel="CLAUDE.md", regions=()):
        claim = self.bind(doc_text, rel)
        return claim, M.check_claim(claim, self.dir, regions)

    def assertDeclined(self, doc_text, bucket, *needles):
        """Declined, into the named bucket, with a reason that NAMES the hatch.

        Both halves matter: the tool's contract is that a declined claim is
        counted AND named, and a decline whose reason says only "refused"
        cannot be told from a claim nobody thought about."""
        claim, (outcome, payload) = self.check(doc_text)
        self.assertEqual(outcome, "declined",
                         "expected a decline for %r, got %s %r"
                         % (claim.cmd, outcome, payload))
        got_bucket, why = payload
        self.assertEqual(got_bucket, bucket, "wrong decline bucket: %s" % why)
        self.assertTrue(why and why.strip(), "decline reason is empty")
        for needle in needles:
            self.assertIn(needle, why)
        return why

    def assertNoFile(self, rel):
        self.assertFalse((self.dir / rel).exists(),
                         "canary %r was created — the command executed" % rel)

    # -- driving a whole scan ------------------------------------------------

    def scan(self, **patches):
        """`scan()` over the fixture, with the module globals a fixture repo
        needs narrowed. SURFACES/SURFACE_GLOBS are a security boundary in the
        tool and a fixture cannot carry the real nine, so they are patched
        rather than faked on disk; MIN_EXECUTED_CLAIMS is the live tree's
        coverage floor and would make every fixture exit 2. Returns
        (exit_code, printed_output)."""
        patches.setdefault("SURFACES", ("CLAUDE.md",))
        patches.setdefault("SURFACE_GLOBS", ("docs/specs/*/section-*.md",))
        patches.setdefault("MIN_EXECUTED_CLAIMS", 1)
        buf = io.StringIO()
        with contextlib.ExitStack() as stack:
            for name, value in patches.items():
                stack.enter_context(mock.patch.object(M, name, value))
            with contextlib.redirect_stdout(buf):
                code = M.scan(self.dir)
        return code, buf.getvalue()

    def executed_count(self, output):
        for line in output.splitlines():
            if "claims executed and compared" in line:
                return int(line.split(":")[1].split("(")[0].strip())
        self.fail("the run printed no coverage line:\n%s" % output)


# ---------------------------------------------------------------------------
# (1) ONE REGRESSION TEST PER HISTORICAL EXPLOIT.
#
# Every case below was reachable at some point in this file's history and was
# proven exploitable by hand at the round that closed it. The reference for
# each is in doc-claim-check.py's SAFETY header and version history.
# ---------------------------------------------------------------------------


class HatchRegressionTests(FixtureCase):
    """Rounds 9, 14 and 15: write/execute escape hatches behind read-only
    binaries. Each asserts the decline is named AND that the hatch's own canary
    is untouched."""

    # -- round 9: the founding five -----------------------------------------

    def test_sed_i_rewrites_a_file(self):
        # `sed` is DROPPED from the allow-list entirely: its write lives in its
        # script, which is a language no flag table can describe.
        self.assertDeclined("`sed -i 's/alpha/PWNED/' data.txt` → 3",
                            "unlisted-binary", "sed", "not an allow-listed")
        self.assertEqual((self.dir / "data.txt").read_text(), DATA_TEXT)

    def test_find_delete(self):
        self.write("victim.txt", "keep me\n")
        self.assertDeclined("`find . -name 'victim.txt' -delete` → 3",
                            "unsafe", "-delete", "escape hatch")
        self.assertTrue((self.dir / "victim.txt").exists(),
                        "find -delete removed the victim file")

    def test_python3_dash_c(self):
        self.assertDeclined(
            "`python3 -c 'open(\"CANARY\",\"w\")'` → 3",
            "unlisted-binary", "python3", "not an allow-listed")
        self.assertNoFile("CANARY")

    def test_python3_running_an_in_repo_script(self):
        # Round 15 (H2): "the script must be an in-repo .py file" is no
        # restriction at all when the checkout under test is a pull request's
        # own head — the PR simply adds the .py file, as this fixture does.
        self.write("tools/pwn.py", "open('CANARY', 'w').close()\n")
        self.assertDeclined("`python3 tools/pwn.py` → 3",
                            "unlisted-binary", "python3")
        self.assertNoFile("CANARY")

    def test_sort_output_flag(self):
        self.assertDeclined("`sort -o CANARY data.txt` → 3",
                            "unsafe", "-o", "escape hatch")
        self.assertNoFile("CANARY")

    def test_rg_pre(self):
        # Declined on the flag, before any process is spawned — so this case
        # needs no ripgrep on PATH, and deliberately does not skip when there
        # is none.
        self.write("p.sh", "#!/bin/sh\ntouch CANARY\n")
        self.assertDeclined("`rg --pre ./p.sh alpha data.txt` → 3",
                            "unsafe", "--pre")
        self.assertNoFile("CANARY")

    def test_git_dash_c(self):
        # `-c` takes a SEPARATE argument, so `git -c core.pager=X log` is
        # refused one gate earlier than the global-flag list: the first
        # non-option token — the config assignment — is not a read-only
        # subcommand. Named either way, which is the contract.
        self.assertDeclined("`git -c core.pager=CANARY log` → 3",
                            "unsafe", "read-only subcommand")
        self.assertNoFile("CANARY")

    def test_git_global_exec_path(self):
        # `--exec-path` points git at a directory it will run helper binaries
        # out of. Unlike the cases above this one carries no canary assertion,
        # deliberately: every read-only subcommand on the allow-list is a git
        # BUILT-IN, so an exec-path payload would not fire even against the
        # unguarded tool, and asserting a canary that cannot be created either
        # way would be exactly the tautological lock this suite exists to
        # avoid. The property is locked by the decline, and its
        # mutation-verification needs BOTH git flag guards removed at once —
        # the shared denied_prefixes table and git's own pre-subcommand list
        # each catch the attached form on their own.
        self.assertDeclined("`git --exec-path=. log` → 3",
                            "unsafe", "--exec-path")

    def test_uniq_with_an_output_operand(self):
        # No flag names this hatch — `uniq IN OUT` writes OUT — so it is
        # guarded by operand COUNT instead.
        self.assertDeclined("`uniq data.txt CANARY` → 3",
                            "unsafe", "OUTPUT operand")
        self.assertNoFile("CANARY")

    def test_awk_system(self):
        self.assertDeclined(
            "`awk 'BEGIN{system(\"touch CANARY\")}END{print 3}' data.txt` → 3",
            "unsafe", "system")
        self.assertNoFile("CANARY")

    def test_awk_with_a_leading_v_assignment(self):
        # Round 14 (external review, P1): the program used to be located as
        # "first operand not starting with -", so `-v x=1` fed the check `x=1`
        # and the program was never looked at.
        self.assertDeclined(
            "`awk -v x=1 'BEGIN{system(\"touch CANARY\")}END{print 3}' "
            "data.txt` → 3", "unsafe", "system")
        self.assertNoFile("CANARY")

    def test_awk_output_pipe(self):
        # Round 15 (H1): `print x | "sh"` is arbitrary command execution using
        # NEITHER blacklisted word, and FORBIDDEN does not reject a single `|`
        # because that is the pipeline separator.
        self.assertDeclined(
            "`awk 'BEGIN{print \"touch CANARY\" | \"sh\"}END{print 3}' "
            "data.txt` → 3", "unsafe", "|")
        self.assertNoFile("CANARY")

    def test_an_unknown_awk_call_is_declined(self):
        # Round 15 (H1) inverted the awk rule: what the program may CALL is
        # ALLOW-listed, so a gawk builtin nobody on this project has heard of
        # cannot become the next hatch the way `system` was. Locked separately
        # from the two system()/getline cases above, which the named-form
        # BACKSTOP also catches — without this, opening the allow-list
        # completely leaves the suite green (verified by mutation).
        self.assertDeclined(
            "`awk 'BEGIN{x=strftime()}END{print 3}' data.txt` → 3",
            "unsafe", "strftime", "allow-list")

    def test_a_bare_getline_is_caught_by_the_named_backstop(self):
        # `getline` without parentheses is the one awk escape the CALL
        # allow-list cannot see — AWK_CALL matches `name(` only — so this is
        # the sole lock on AWK_ESCAPES, the named-form backstop the allow-list
        # otherwise shadows completely.
        self.assertDeclined("`awk '{getline}' data.txt` → 3",
                            "unsafe", "getline")

    def test_glob_expanding_to_an_option_shaped_filename(self):
        # Round 14 (external review, P1): with a repo file named
        # `--output=canary`, the validated command `sort * | wc -l` expanded to
        # `sort --output=canary …` and WROTE it — denied_flag() had run on the
        # PRE-expansion argv.
        self.write("--output=canary", "original\n")
        self.assertDeclined("`sort * \\| wc -l` → 3",
                            "unsafe", "expands to", "--output=canary")
        self.assertEqual((self.dir / "--output=canary").read_text(),
                         "original\n")

    # -- round 15: the same hatches RESPELLED --------------------------------

    def test_sort_output_flag_with_an_attached_value(self):
        # `sort -o` was denied while `sort -oFILE` wrote the file: the deny
        # list compared the SPELLING, not the option core.
        why = self.assertDeclined("`sort -oCANARY data.txt` → 3",
                                  "unsafe", "-o")
        self.assertIn("attached", why)
        self.assertNoFile("CANARY")

    def test_git_grep_open_files_in_pager_with_an_attached_value(self):
        self.write("p.sh", "#!/bin/sh\ntouch CANARY\n")
        self.assertDeclined("`git grep -O./p.sh alpha` → 3", "unsafe", "-O")
        self.assertNoFile("CANARY")

    def test_git_diff_output(self):
        self.assertDeclined("`git diff --output=CANARY HEAD` → 3",
                            "unsafe", "--output")
        self.assertNoFile("CANARY")

    def test_sort_compress_program(self):
        # Runs an arbitrary program whenever a sort spills to disk, which
        # `-S 1` forces.
        self.write("p.sh", "#!/bin/sh\ntouch CANARY\n")
        self.assertDeclined("`sort --compress-program=./p.sh -S 1 data.txt` "
                            "→ 3", "unsafe", "--compress-program")
        self.assertNoFile("CANARY")

    # -- round 15: containment and resource bounds ---------------------------

    def test_operand_outside_the_repository_root(self):
        # A one-integer read oracle over the host: `grep -c . /etc/passwd` was
        # executed and COMPARED before path confinement existed.
        outside = pathlib.Path(tempfile.mkdtemp(prefix="doc-claim-outside-"))
        self.addCleanup(shutil.rmtree, outside, ignore_errors=True)
        (outside / "OUTSIDE.txt").write_text("secret\nsecret\n")
        rel = os.path.relpath(outside / "OUTSIDE.txt", self.dir)
        self.assertDeclined("`cat %s \\| wc -l` → 2" % rel,
                            "unsafe", "resolves outside")

    def test_absolute_operand_outside_the_repository_root(self):
        self.assertDeclined("`grep -c . /etc/passwd` → 3",
                            "unsafe", "resolves outside")

    def test_embedded_nul_declines_and_does_not_abort_the_scan(self):
        # Round 15 (M2): subprocess raises ValueError — not a SubprocessError —
        # on a NUL in argv, so ONE backticked span ended the whole scan in a
        # traceback and every later claim silently never ran. The decline is
        # half the property; the other half is that the run continues.
        self.assertDeclined("`grep -c a\x00b data.txt` → 3",
                            "unsafe", "metacharacter")
        self.write("CLAUDE.md",
                   "# Fixture\n\n`grep -c a\x00b data.txt` → 3\n\n"
                   "`%s` → %d\n" % (TRUE_CMD, TRUE_VALUE))
        self.spec_file("prose, no fences\n")
        code, out = self.scan()
        self.assertEqual(code, 0, out)
        self.assertEqual(self.executed_count(out), 1,
                         "the claim after the NUL never ran:\n%s" % out)

    @unittest.skipUnless(HAVE_PRINTF, "printf is not on PATH")
    def test_output_cap(self):
        # Round 15 (M1): TIMEOUT_S bounds WALL TIME, which is not the resource
        # a document line can exhaust — one line drove the checker to 587 MB.
        why = self.assertDeclined(
            "`printf %9000000d 1 \\| wc -c` → 9000000",
            "did-not-run", "exceeded")
        self.assertIn("killed", why)


@unittest.skipUnless(HAVE_GIT, "git is not on PATH")
class GitRefRegressionTests(FixtureCase):
    """Round 15 (H4): `branch` and `tag` were read-only SUBCOMMANDS on the
    allow-list, and `-D`/`-d` DESTROY a ref — proven deleting one in a fixture
    repo under a printed PASS. These build their own throwaway git repo; they
    never touch the repository under test."""

    def setUp(self):
        super().setUp()
        env = dict(os.environ,
                   HOME=str(self.dir), GIT_CONFIG_NOSYSTEM="1",
                   GIT_CONFIG_GLOBAL=str(self.dir / ".gitconfig"),
                   GIT_AUTHOR_NAME="t", GIT_AUTHOR_EMAIL="t@example.invalid",
                   GIT_COMMITTER_NAME="t",
                   GIT_COMMITTER_EMAIL="t@example.invalid")
        run = lambda *a: subprocess.run(  # noqa: E731 - fixture setup only
            ("git",) + a, cwd=self.dir, env=env, check=True,
            stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        run("init", "-q")
        run("add", "data.txt", "CLAUDE.md")
        run("commit", "-q", "-m", "fixture")
        run("branch", "doomed-branch")
        run("tag", "doomed-tag")
        self.git_env = env

    def refs(self):
        out = subprocess.run(("git", "for-each-ref", "--format=%(refname)"),
                             cwd=self.dir, env=self.git_env, check=True,
                             stdout=subprocess.PIPE, text=True).stdout
        return out.split()

    def test_git_branch_delete(self):
        self.assertDeclined("`git branch -D doomed-branch` → 3",
                            "unsafe", "read-only subcommand")
        self.assertIn("refs/heads/doomed-branch", self.refs())

    def test_git_tag_delete(self):
        self.assertDeclined("`git tag -d doomed-tag` → 3",
                            "unsafe", "read-only subcommand")
        self.assertIn("refs/tags/doomed-tag", self.refs())

    @unittest.skipUnless(HAVE_GREP, "grep is not on PATH")
    def test_a_read_only_git_subcommand_still_runs(self):
        # The complement: closing the ref hatches must not have cost the two
        # live `git grep -c` claims this tool actually executes.
        _claim, (outcome, payload) = self.check(
            "`git ls-files \\| wc -l` → 2")
        self.assertEqual(outcome, "reproduced", payload)


# ---------------------------------------------------------------------------
# (2) MATCHER COMPLEMENTS.
# ---------------------------------------------------------------------------


@unittest.skipUnless(HAVE_GREP, "grep is not on PATH")
class MatcherTests(FixtureCase):

    def test_a_true_claim_reproduces(self):
        _claim, (outcome, payload) = self.check("`%s` → %d"
                                                % (TRUE_CMD, TRUE_VALUE))
        self.assertEqual(outcome, "reproduced", payload)
        self.assertEqual(payload, (TRUE_VALUE, TRUE_VALUE))

    def test_a_false_claim_mismatches(self):
        _claim, (outcome, payload) = self.check("`%s` → 99" % TRUE_CMD)
        self.assertEqual(outcome, "mismatch", payload)
        self.assertEqual(payload, (99, TRUE_VALUE))

    def test_a_false_claim_fails_the_run_with_exit_1(self):
        self.write("CLAUDE.md", "# Fixture\n\n`%s` → 99\n" % TRUE_CMD)
        self.spec_file("prose only\n")
        code, out = self.scan()
        self.assertEqual(code, 1, out)
        self.assertIn("FAIL", out)
        self.assertIn("document says 99; command returns 3", out)

    def test_every_shape_binds_the_shape_it_is_meant_to(self):
        # The precedence order is a property in its own right: a shape that
        # silently stopped binding would be invisible in a suite that only
        # asserted the outcome.
        for text, shape in (
                ("`%s` → %d" % (TRUE_CMD, TRUE_VALUE),
                 "command-then-value"),
                ("`%s`: **%d**" % (TRUE_CMD, TRUE_VALUE),
                 "command-colon-value"),
                ("%d lines (`%s`)" % (TRUE_VALUE, TRUE_CMD),
                 "value-then-parenthesised-command"),
                ("%d lines — re-derived by `%s`" % (TRUE_VALUE, TRUE_CMD),
                 "value-then-attributed-command")):
            with self.subTest(shape=shape):
                claim = self.bind(text)
                self.assertEqual(claim.shape.name, shape)
                self.assertEqual(M.check_claim(claim, self.dir, ())[0],
                                 "reproduced")

    def test_a_negated_claim_declines_in_every_shape(self):
        # Round 16 (M4): the look-back window was added to ONE shape, so a
        # forward-shape claim whose negator sits BEFORE the backtick was
        # reported as a mismatch while the value-first phrasing of the
        # identical sentence correctly declined.
        for text, shape in (
                ("the plain `%s` no longer returns 99" % TRUE_CMD,
                 "command-then-value"),
                ("no longer reported by `%s`: **99**" % TRUE_CMD,
                 "command-colon-value"),
                ("no longer 99 lines (`%s`)" % TRUE_CMD,
                 "value-then-parenthesised-command"),
                ("no longer 99 lines — re-derived by `%s`" % TRUE_CMD,
                 "value-then-attributed-command")):
            with self.subTest(shape=shape):
                claim = self.bind(text)
                self.assertEqual(claim.shape.name, shape)
                outcome, payload = M.check_claim(claim, self.dir, ())
                self.assertEqual(outcome, "declined", payload)
                self.assertEqual(payload[0], "negated", payload)

    def test_a_backticked_identifier_is_neither_counted_nor_named(self):
        # ~1,100 of these exist on the live tree ("`SNAPSHOT_SCHEMA_VERSION`
        # **20 → 21**"). Counting them would drown the real decline signal, so
        # they are IGNORED — but the census must not pick them up either.
        text = "`SEASON_SAVE_FORMAT_VERSION` **5 → 6**"
        claim = self.bind(text)
        self.assertEqual(M.check_claim(claim, self.dir, ())[0], "ignored")
        self.assertEqual(list(M.unrecognised_spans(text, {claim.cmd_start})),
                         [])

    def test_an_unrecognised_shape_is_counted_and_named(self):
        # The complement of the case above, and the half of round 16 (H7) that
        # matters most: a runnable command in a shape no matcher knows must
        # land in the census rather than vanishing.
        text = "35 assemblies. The gate runs `%s` nightly." % TRUE_CMD
        bound = {c.cmd_start for c in M.collect_claims("CLAUDE.md", text)}
        self.assertEqual([cmd for _line, cmd in
                          M.unrecognised_spans(text, bound)], [TRUE_CMD])

    def test_a_date_year_is_not_a_stated_value(self):
        # Round 16 (H8): "August 18, 2026 (`cmd`)" parsed as stated value 2026
        # and produced "document says 2026; command returns 3" — the tool
        # fabricating the finding it exists to prevent.
        self.assertEqual(M.collect_claims(
            "CLAUDE.md", "August 18, 2026 (`%s`)" % TRUE_CMD), [])

    def test_a_date_day_is_not_a_stated_value(self):
        self.assertEqual(M.collect_claims(
            "CLAUDE.md", "measured August 18 (`%s`)" % TRUE_CMD), [])

    def test_a_range_endpoint_is_declined_not_compared(self):
        self.assertDeclined("`%s` → 2-4" % TRUE_CMD,
                            "approximate-or-range", "RANGE")

    def test_an_approximation_is_declined_not_compared(self):
        self.assertDeclined("~30 files (`%s`)" % TRUE_CMD,
                            "approximate-or-range", "approximation marker")

    def test_a_malformed_integer_literal_is_declined(self):
        # Round 17 (L5): `int()` NORMALISES what it is handed — int("007") is
        # 7 — so a malformed transcription could "reproduce" a real value by
        # accident of int() being more permissive than any command's output.
        self.assertDeclined("`%s` → 003" % TRUE_CMD,
                            "not-single-int", "well-formed")

    def test_a_command_with_no_file_operand_is_declined(self):
        # Round 16 (M8): this repo genuinely writes claims whose FILENAME
        # lives outside the backticks ("`grep -c ...` over each file returns
        # 18"). Run as quoted, such a command reads an empty stdin and returns
        # 0 — reporting that as "document says 18; command returns 0" would be
        # a fabricated finding, so it is UNVERIFIABLE, not false.
        self.assertDeclined("`grep -c '^- ..'` → 18",
                            "not-self-contained", "empty stdin")

    def test_a_multi_line_answer_is_declined_not_compared(self):
        # The file's FIRST line is the stated value, so a read-back that took
        # `lines[0]` instead of requiring exactly one line would "reproduce"
        # it. The single-integer floor means one integer and nothing else.
        self.write("counts.txt", "3\n7\n")
        self.assertDeclined("`cat counts.txt` → 3",
                            "not-single-int", "single integer")


# ---------------------------------------------------------------------------
# (3) run_pipeline.
# ---------------------------------------------------------------------------


@unittest.skipUnless(HAVE_GREP, "grep is not on PATH")
class RunPipelineTests(FixtureCase):

    def test_a_failing_segment_declines_rather_than_compares(self):
        # Round 9 (H3): a FAILING segment's empty output flowed into the next
        # one and the pipeline still produced a confident number — "document
        # says 218; command returns 0" against a document that was not wrong.
        out, why = M.run_pipeline([["grep", "-c", "x", "nosuchfile.txt"],
                                   ["wc", "-l"]], str(self.dir))
        self.assertIsNone(out)
        self.assertIn("exited", why)

    def test_grep_exit_1_is_benign(self):
        # grep 1 means "no match", which IS the answer 0 — treating it as a
        # failure would surrender the tool's most common command.
        out, why = M.run_pipeline([["grep", "-c", "zzzz", "data.txt"]],
                                  str(self.dir))
        self.assertIsNone(why)
        self.assertEqual(M.single_integer(out), 0)

    def test_a_claim_over_a_failing_pipeline_is_declined_and_named(self):
        self.assertDeclined(
            "`grep -c 'a' data.txt nosuchfile.txt` → 3", "did-not-run",
            "exited")


# ---------------------------------------------------------------------------
# (4) tokenize / expand_globs.
# ---------------------------------------------------------------------------


class TokenizeTests(FixtureCase):

    def test_a_quoted_glob_is_not_expanded(self):
        # Round 9 (H2): shlex.split DISCARDS quoting, so a QUOTED regex was
        # expanded as a shell glob — `find . -name '*.md' | wc -l` ran as
        # `find . -name CLAUDE.md doc.md`, which errors and prints nothing.
        segments = M.tokenize("grep -c '*.md' data.txt")
        self.assertEqual([[text for text, _g in seg] for seg in segments],
                         [["grep", "-c", "*.md", "data.txt"]])
        self.assertEqual([expandable for _t, expandable in segments[0]],
                         [False, False, False, False])
        self.write("other.md", "x\n")
        expanded, why = M.expand_globs(segments, self.dir)
        self.assertIsNone(why)
        self.assertEqual(expanded, [["grep", "-c", "*.md", "data.txt"]])

    def test_an_unquoted_glob_is_expanded(self):
        # The complement — without it the case above passes against a tokenizer
        # that expands nothing at all.
        (self.dir / "g1.tmpx").write_text("x\n")
        (self.dir / "g2.tmpx").write_text("x\n")
        segments = M.tokenize("wc -l *.tmpx")
        expanded, why = M.expand_globs(segments, self.dir)
        self.assertIsNone(why)
        self.assertEqual(expanded, [["wc", "-l", "g1.tmpx", "g2.tmpx"]])

    def test_a_quoted_escaped_pipe_does_not_split_the_pipeline(self):
        # Markdown escapes a literal pipe as `\|` in a table cell, but inside
        # quotes it is BRE alternation — rewriting it would run a DIFFERENT
        # regex and report its count as the document's.
        segments = M.tokenize("grep -c 'a\\|b' data.txt")
        self.assertEqual(len(segments), 1)
        self.assertEqual([text for text, _g in segments[0]],
                         ["grep", "-c", "a\\|b", "data.txt"])

    def test_an_unquoted_escaped_pipe_does_split_the_pipeline(self):
        segments = M.tokenize("grep -c 'a' data.txt \\| wc -l")
        self.assertEqual([[t for t, _g in seg] for seg in segments],
                         [["grep", "-c", "a", "data.txt"], ["wc", "-l"]])

    def test_an_unterminated_quote_declines_by_name(self):
        self.assertIsNone(M.tokenize("grep -c 'a data.txt"))
        self.assertDeclined("`grep -c 'a data.txt` → 3",
                            "unsafe", "does not tokenize")

    def test_a_trailing_backslash_declines_by_name(self):
        self.assertIsNone(M.tokenize("grep -c a data.txt \\"))

    def test_a_directory_globs_trailing_slash_is_preserved(self):
        # Round 16 (M9): pathlib normalises the slash away on both ends, so a
        # command anchored on it (`ls -d src/*/ | grep -c '/$'`) compared
        # against a DIFFERENT command's output — 2 under bash, 0 here.
        (self.dir / "src" / "aa").mkdir(parents=True)
        (self.dir / "src" / "bb").mkdir(parents=True)
        segments = M.tokenize("ls -d src/*/")
        expanded, why = M.expand_globs(segments, self.dir)
        self.assertIsNone(why)
        self.assertEqual(expanded, [["ls", "-d", "src/aa/", "src/bb/"]])

    def test_a_quoted_command_head_still_tokenizes_to_the_real_binary(self):
        # Round 17 (L2): `claim.head` used to be `cmd.split()[0]`, a SECOND
        # parser disagreeing with the one validation uses — `"grep"` was never
        # equal to `grep`, so a runnable claim fell through every named path
        # and was neither run nor counted.
        claim = self.bind("`\"grep\" -c 'a' data.txt` → %d" % TRUE_VALUE)
        self.assertEqual(claim.head, "grep")


# ---------------------------------------------------------------------------
# (5) THE EXCUSAL MODEL — dated records.
# ---------------------------------------------------------------------------


@unittest.skipUnless(HAVE_GREP, "grep is not on PATH")
class ExcusalTests(FixtureCase):
    """A claim in an append-only record states what a command returned AT THE
    TIME, so a mismatch there is EXCUSED — counted and printed, never silent —
    unless the record reasserts that the value is current."""

    CHAIN = (
        "# Doc\n\n"
        "**Last Updated:** head entry — `%s` → 91\n\n"
        "**Last Updated (prior):** older entry — `%s` → 92\n\n"
        "A third entry: `%s` now returns 93.\n" % (TRUE_CMD, TRUE_CMD,
                                                   TRUE_CMD))

    def chain_outcomes(self):
        regions = M.dated_record_regions("CLAUDE.md", self.CHAIN)
        self.assertTrue(regions, "no frozen-chain region was computed")
        return [M.check_claim(c, self.dir, regions)[0]
                for c in M.collect_claims("CLAUDE.md", self.CHAIN)]

    def test_head_entry_above_the_marker_is_reported(self):
        self.assertEqual(self.chain_outcomes()[0], "mismatch")

    def test_a_record_below_the_marker_is_excused(self):
        self.assertEqual(self.chain_outcomes()[1], "excused")

    def test_a_currency_assertion_pierces_the_excusal(self):
        self.assertEqual(self.chain_outcomes()[2], "mismatch")

    def test_the_excusal_is_printed_not_silent(self):
        # An excusal is a mismatch the tool chose not to report, and the
        # round-5/6 lesson is that those must never be silent — so the run
        # PASSES and still prints the excused figure. The head entry states the
        # true value here, so the only mismatch in the file is the frozen one.
        self.write("CLAUDE.md", self.CHAIN
                   .replace("A third entry: `%s` now returns 93.\n" % TRUE_CMD,
                            "")
                   .replace("head entry — `%s` → 91" % TRUE_CMD,
                            "head entry — `%s` → %d" % (TRUE_CMD, TRUE_VALUE)))
        self.spec_file("prose only\n")
        code, out = self.scan()
        self.assertEqual(code, 0, out)
        self.assertIn("1 mismatch(es) EXCUSED", out)
        self.assertIn("record says 92; command now returns 3", out)

    # -- the ERR log's own, stricter rule (round 16, M12) --------------------

    ERR_REL = "docs/tracking/spec-error-log.md"
    ERR_LOG = (
        "# Log\n\n"
        "## Error Index\n\n"
        "| ERR-001-001 | still open, undated | `%s` → 81 |\n"
        "| ERR-001-002 | ✅ RESOLVED | `%s` → 82 |\n\n"
        "## ERR-001-003: a dated entry\n\n"
        "Measured August 12, 2026: `%s` → 83.\n\n"
        "## ERR-001-004: an open, undated entry\n\n"
        "Acceptance criterion: `%s` → 84.\n"
        % (TRUE_CMD, TRUE_CMD, TRUE_CMD, TRUE_CMD))

    def err_outcomes(self):
        regions = M.dated_record_regions(self.ERR_REL, self.ERR_LOG)
        self.assertTrue(regions, "no log-body region was computed")
        return [M.check_claim(c, self.dir, regions)[0]
                for c in M.collect_claims(self.ERR_REL, self.ERR_LOG)]

    def test_a_resolved_err_entry_is_excused(self):
        self.assertEqual(self.err_outcomes()[1], "excused")

    def test_a_dated_err_entry_is_excused(self):
        self.assertEqual(self.err_outcomes()[2], "excused")

    def test_an_open_undated_err_entry_is_reported(self):
        # The whole point of M12's stricter rule: a present-tense acceptance
        # criterion inside a still-open entry was being excused by proximity
        # alone.
        self.assertEqual(self.err_outcomes()[3], "mismatch")

    def test_an_err_index_table_row_is_bounded_to_its_own_line(self):
        # `_err_log_entry_span`'s own docstring: "the whole table sits under
        # ONE heading, so treating the table as a single entry would let a ✅
        # anywhere in its 213 rows resolve every one of them." On the live tree
        # 191 of the 213 index rows carry a ✅, so without per-line bounding
        # EVERY index-row claim is excused, resolved or not.
        self.assertEqual(self.err_outcomes()[0], "mismatch")


# ---------------------------------------------------------------------------
# (6) EXIT CODES, AND (7) THE VACUOUS-PASS GUARD.
# ---------------------------------------------------------------------------


@unittest.skipUnless(HAVE_GREP, "grep is not on PATH")
class ExitCodeTests(FixtureCase):

    DANGLING_FENCE = ("```csharp\npublic class Foo { public int Bar; }\n"
                      "int x = Foo.Nope;\n```\n")
    CLEAN_FENCE = ("```csharp\npublic class Foo { public int Bar; }\n"
                   "int x = Foo.Bar;\n```\n")

    def true_claim(self):
        self.write("CLAUDE.md", "# Fixture\n\n`%s` → %d\n"
                   % (TRUE_CMD, TRUE_VALUE))

    def test_exit_0_when_everything_reproduces(self):
        self.true_claim()
        self.spec_file(self.CLEAN_FENCE)
        code, out = self.scan()
        self.assertEqual(code, 0, out)
        self.assertIn("PASS", out)

    def test_exit_1_when_a_document_is_wrong(self):
        self.write("CLAUDE.md", "# Fixture\n\n`%s` → 42\n" % TRUE_CMD)
        self.spec_file(self.CLEAN_FENCE)
        code, out = self.scan()
        self.assertEqual(code, 1, out)

    def test_exit_2_when_a_named_surface_is_missing(self):
        # Round 16 (H9): deleting 8 of the 9 named surfaces printed eight
        # MISSING SURFACE lines, checked one claim, and exited 0 with PASS.
        self.true_claim()
        self.spec_file(self.CLEAN_FENCE)
        code, out = self.scan(SURFACES=("CLAUDE.md", "docs/tracking/gone.md"))
        self.assertEqual(code, 2, out)
        self.assertIn("named surface", out)
        self.assertIn("ERROR", out)

    def test_exit_2_when_a_surface_glob_matches_no_file(self):
        self.true_claim()
        self.spec_file(self.CLEAN_FENCE)
        code, out = self.scan(SURFACE_GLOBS=("docs/specs/*/absent-*.md",))
        self.assertEqual(code, 2, out)
        self.assertIn("matches no file", out)

    def test_exit_2_when_no_claim_was_executed_at_all(self):
        # THE VACUOUS-PASS GUARD. Surfaces all present, documents all readable,
        # and nothing checked: this must be an ERROR, not a PASS. It is the
        # failure mode a matcher regression produces, and the one this project
        # files as High.
        self.write("CLAUDE.md", "# Fixture\n\nProse with no claims at all.\n")
        self.spec_file("prose only\n")
        code, out = self.scan()
        self.assertEqual(code, 2, out)
        self.assertIn("below the floor", out)
        self.assertNotIn("PASS —", out)

    def test_exit_2_outranks_a_mismatch(self):
        # "With the surface set broken, the mismatches that were found are not
        # the mismatches that exist."
        self.write("CLAUDE.md", "# Fixture\n\n`%s` → 42\n" % TRUE_CMD)
        self.spec_file(self.CLEAN_FENCE)
        code, _out = self.scan(SURFACES=("CLAUDE.md",
                                         "docs/tracking/gone.md"))
        self.assertEqual(code, 2)

    def test_exit_3_when_only_a_fence_identifier_dangles(self):
        self.true_claim()
        self.spec_file(self.DANGLING_FENCE)
        code, out = self.scan()
        self.assertEqual(code, 3, out)
        self.assertIn("dangling identifier reference", out)
        self.assertIn("Foo.Nope", out)

    def test_exit_1_outranks_exit_3_when_both_checks_fail(self):
        # 1 is the long-standing documented code for "a document is wrong" and
        # keeps priority, so nothing downstream starts reading 3 as a novel
        # failure kind.
        self.write("CLAUDE.md", "# Fixture\n\n`%s` → 42\n" % TRUE_CMD)
        self.spec_file(self.DANGLING_FENCE)
        code, _out = self.scan()
        self.assertEqual(code, 1)

    def test_a_declared_member_does_not_dangle(self):
        # The complement of the dangling case: without it, a CHECK 2 that
        # reported every reference would pass the test above.
        self.true_claim()
        self.spec_file(self.CLEAN_FENCE)
        code, _out = self.scan()
        self.assertEqual(code, 0)

    def test_a_struct_member_is_examined_like_a_class_member(self):
        # Round 17 (M10): DECL_CLASS matched `class` only, so every reference
        # into a struct was silently unexaminable — and struct-based
        # architecture is this repo's STANDING RULE.
        self.true_claim()
        self.spec_file("```csharp\npublic readonly struct Foo "
                       "{ public int Bar; }\nint x = Foo.Nope;\n```\n")
        code, out = self.scan()
        self.assertEqual(code, 3, out)


class CliTests(unittest.TestCase):
    """The two cases that are genuinely about the process boundary. Everything
    else in this suite calls the module directly, because a subprocess per case
    would cost more than the gate it protects."""

    def test_a_directory_that_is_not_a_repo_root_exits_2(self):
        with tempfile.TemporaryDirectory() as d:
            proc = subprocess.run(
                (sys.executable, str(TOOL_PATH), "--repo", d),
                stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
            self.assertEqual(proc.returncode, 2, proc.stdout + proc.stderr)
            self.assertIn("not a Tactical Director repo root", proc.stderr)

    def test_help_does_not_touch_the_sibling_import(self):
        # Round 17 (M13): the sibling import used to run at MODULE scope, so it
        # fired for `--help` too, before argparse printed anything.
        proc = subprocess.run(
            (sys.executable, str(TOOL_PATH), "--help"),
            stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
        self.assertEqual(proc.returncode, 0, proc.stderr)
        self.assertIn("--repo", proc.stdout)


if __name__ == "__main__":
    unittest.main(verbosity=2)

# Version History
# | Version | Date       | Author      | Notes                                 |
# | 1.0     | 2026-08-22 | Claude Code | Initial. The round-17 review found    |
# |         |            |             | that every safety property of         |
# |         |            |             | doc-claim-check.py had been           |
# |         |            |             | established by manual scratch-mirror  |
# |         |            |             | experiments that no longer exist,     |
# |         |            |             | that several had since become false,  |
# |         |            |             | and that a one-character regression   |
# |         |            |             | to a matcher gap bound dropped        |
# |         |            |             | coverage 3->2 executed and 30->19     |
# |         |            |             | declined while the tool printed PASS. |
# |         |            |             | One regression test per historical    |
# |         |            |             | exploit (each asserting BOTH the      |
# |         |            |             | named decline AND an untouched        |
# |         |            |             | canary — file, contents or git ref),  |
# |         |            |             | matcher complements, run_pipeline,    |
# |         |            |             | tokenize, the dated-record excusal    |
# |         |            |             | model, all four exit codes, and the   |
# |         |            |             | vacuous-pass guard. Every fixture is  |
# |         |            |             | a temp dir the test builds; nothing   |
# |         |            |             | reads or writes the repository, and   |
# |         |            |             | the two git cases build their own     |
# |         |            |             | throwaway repo. Every test was        |
# |         |            |             | mutation-verified against a scratch   |
# |         |            |             | copy of the tool — see the landing    |
# |         |            |             | report — because a lock that passes   |
# |         |            |             | against the broken tool is the        |
# |         |            |             | tautological-lock defect this repo    |
# |         |            |             | has filed three times. Found one real |
# |         |            |             | defect while being written:           |
# |         |            |             | ERR_TABLE_ROW carried no re.MULTILINE |
# |         |            |             | flag, so `^` could never match at a   |
# |         |            |             | non-zero line start and the ERR index |
# |         |            |             | table's per-ROW bounding never fired  |
# |         |            |             | — one ✅ among its 213 rows excused    |
# |         |            |             | every claim in every row, exactly     |
# |         |            |             | what that function's docstring says   |
# |         |            |             | it exists to prevent.                 |
