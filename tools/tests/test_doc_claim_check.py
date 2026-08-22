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
# ROUND 2 (M17, M18) — TWO REVIEWED FINDINGS AGAINST THIS SUITE ITSELF, both
# proven by mutation before the fix and re-proven mutation-red afterwards:
#   M17: two independent mutation batteries (38 and 15 mutants) agreed on 18
#        survivors — the suite's own load-bearing guards could regress with
#        all 71 tests green. One test per surviving mutant is now below,
#        each written red against the mutant first (section 8), and several
#        deliberately drive scan() end to end rather than the helper alone —
#        "the current test cannot distinguish a working census from a
#        deleted call site" is M17's own diagnosis of why the old coverage
#        was not enough.
#   M18: this file's own header claimed "a skip is never silent" while
#        `unittest.main()`'s exit code depends only on `wasSuccessful()`,
#        which counts a skip as neither pass nor fail — reproduced by
#        stubbing `shutil.which` for grep/printf/git: 71 ran, 39 skipped,
#        `wasSuccessful() == True`. Fixed at the bottom of this file with a
#        SKIP CEILING: `unittest.main(exit=False)` plus an explicit
#        `sys.exit` that fails the run when any skip's reason is not
#        explained by a binary independently re-verified absent from PATH
#        right now (see `unexplained_skips()` below) — permitting a
#        genuinely missing binary while still catching a mass silent skip,
#        per the fix's own requirement. THIS CEILING ONLY RUNS UNDER THE
#        FIRST INVOCATION FORM below — `unittest -m unittest discover` uses
#        its own runner and `wasSuccessful()` alone, exactly the gap M18
#        describes, which is why ci.yml (and this file's own header) treat
#        the first form as authoritative and the second as a convenience.
# Round 16's own fixes landed with NO coverage at all — the same shape that
# made round 15's fixes silently regressable — so section 9 below locks ten
# of them, each written red against the pre-fix tool or an equivalent
# mutation of the current one (see the landing report for which).
#
# ROUND 3 — FOUR REVIEWED FINDINGS AGAINST THIS SUITE ITSELF (M25-M27, M30),
# fixed in sections 10-11 below, plus locks for round 18's own landings this
# suite had not caught up with:
#   M25: a 25-mutant battery found CHECK 2's own precision guards almost
#        entirely unlocked — 7 of 8 survived, 4 with a measured live-tree
#        false-positive effect (97 / 3 / 2 / 1 false dangling findings) —
#        while the suite's only precision tests were one `Foo.Bar` case and
#        one struct case. Section 10's `CheckTwoGuardRegressionTests` adds
#        one fixture per surviving guard.
#   M26: the skip ceiling only ever learned ONE skip-reason shape
#        ("<binary> is not on PATH"), and this file already emits a SECOND —
#        "<name> module unavailable" and, until this round, the COMPOUND
#        "git or the resource module is unavailable". Reproduced by running
#        this suite with `git` off PATH: SKIP CEILING EXCEEDED on a wholly
#        legitimate skip. Fixed with a second recognised shape
#        (`_skip_capability_from_reason` / `_module_importable`, mirroring
#        the binary shape's fresh-check discipline exactly) and by
#        splitting the compound decorator on `ChildResourceLimitTests` into
#        two single-reason ones, so a skip's reason always names exactly
#        one checkable thing.
#   M27: `test_help_does_not_touch_the_sibling_import` was a TAUTOLOGICAL
#        LOCK — restoring the pre-fix module-scope import left it green,
#        because it asserted only `returncode == 0` and `"--repo" in
#        stdout`, both true whether the import ran deferred or at module
#        scope. Replaced with three subprocess cases driven against a
#        throwaway copy of tools/ (`_write_tool_copy`): `--help` with the
#        sibling missing entirely, `--repo` with the sibling missing
#        (exit 2, "could not import"), and `--repo` against a stub sibling
#        missing one `_DCC_CONTRACT` name (exit 2, the contract-break text).
#   M30: two live security guards — the awk ENVIRON/PROCINFO refusal and the
#        @load/@include refusal — had ZERO coverage; deleting either left
#        all 121 tests green although the mutated tool was proven (see the
#        landing report) to reproduce a value derived from a REAL
#        environment variable, a one-integer read oracle over the CI
#        runner's own environment. `AwkEnvironmentAndLoadHatchTests` locks
#        both, neither needing awk on PATH since both are refused before a
#        process spawns.
# Section 11 locks round 18's own landings this suite had not caught up
# with: awk's ARGV/ARGC/SYMTAB/FUNCTAB refusal, the symlink-traversal
# denials (per-flag AND the structural directory-walk rule, with its own
# complement), `--files0-from` on every binary that carries it including a
# NON-FIRST pipeline segment, the frozen-span bound, `would_gate`'s LIVE
# classification, the restricted verb-gap grammar, ignored-span recovery
# into the census, the leading-value compound-token rule, the LIVE
# distinct-command floor, and the top-level exception boundary. Every one of
# the 51 new tests across both rounds was mutation-verified against a
# scratch copy of the tool (or, for M26, of this file itself) — see the
# landing report for the exact mutation and failure each one produced.
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
# RUNNING IT (either form; the first is what ci.yml runs AND is the only one
# that enforces the M18 skip ceiling — see ROUND 2 above)
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
import re
import shutil
import subprocess
import sys
import tempfile
import unittest
from unittest import mock

TOOLS_DIR = pathlib.Path(__file__).resolve().parent.parent
TOOL_PATH = TOOLS_DIR / "doc-claim-check.py"
# This file's own path — the two CliTests cases that spawn a subprocess of
# doc-claim-check.py, plus the M18 skip-ceiling self-test hooks below, spawn
# a subprocess of THIS file too.
TEST_PATH = pathlib.Path(__file__).resolve()


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
HAVE_SORT = _has("sort")
HAVE_LS = _has("ls")
HAVE_WC = _has("wc")
HAVE_FIND = _has("find")


def _write_tool_copy(dest, include_sibling=True, sibling_stub=None):
    """A `dest` dir holding a copy of doc-claim-check.py, for driving the
    two import-failure modes (M27) in a subprocess without ever touching the
    real tools/ directory this suite's own import depends on.

    `include_sibling=False` omits tools/doc-consistency-check.py entirely —
    the ImportError case `_ensure_consistency_module()` names "could not
    import". `sibling_stub` writes that TEXT as the sibling instead of
    copying the real one — the CONTRACT-BREAK case, a stub module missing
    one of `_DCC_CONTRACT`'s names. Real content is copied only when
    neither override is given."""
    dest.mkdir(parents=True, exist_ok=True)
    shutil.copy(TOOL_PATH, dest / "doc-claim-check.py")
    if sibling_stub is not None:
        (dest / "doc-consistency-check.py").write_text(sibling_stub,
                                                        encoding="utf-8")
    elif include_sibling:
        shutil.copy(TOOLS_DIR / "doc-consistency-check.py",
                    dest / "doc-consistency-check.py")

# A file whose counts are unambiguous under every command quoted below:
# three lines, all three containing "a".
DATA_TEXT = "alpha\nbeta\ngamma\n"
TRUE_CMD = "grep -c 'a' data.txt"
TRUE_VALUE = 3


# ---------------------------------------------------------------------------
# M18 — THE SKIP CEILING. "A skip is never silent" is this suite's own
# header claim, and until now it was false of itself: `unittest.main()`'s
# exit code depends only on `wasSuccessful()`, which counts a skip as neither
# a pass nor a failure. Reproduced by stubbing `shutil.which` for
# grep/printf/git before this module loaded: 71 tests ran, 39 skipped, and
# `wasSuccessful()` was True. Every exit-code test, every excusal test, every
# matcher complement and every run_pipeline test is in that 39 — the suite
# that exists to prove doc-claim-check.py never passes vacuously can go
# vacuous itself with nobody told.
#
# The fix does not hard-code a ceiling of zero, because that would fail this
# suite on a runner that genuinely lacks one of the three binaries — the
# complement M18 also names as a requirement. Instead every SKIP REASON this
# suite ever produces has one shape, "`<binary>` is not on PATH", and a skip
# is EXPLAINED only when a FRESH, independent `shutil.which` call — not the
# HAVE_* flag that was cached at import time and might itself be the thing
# that lied — confirms that binary really is absent right now. A skip whose
# reason names a binary that is actually present (M18's own reproduction) or
# whose reason has no such shape at all is UNEXPLAINED, and even one of those
# fails the run with a non-zero exit — see the `__main__` block at the
# bottom of this file, which is where this is actually wired to the exit
# code `unittest.main(exit=False)` would otherwise ignore.
#
# M26 (reviewed finding, round 2 of this suite's own hardening) — THE CEILING
# ONLY EVER LEARNED ONE SHAPE, AND THIS FILE ALREADY EMITS A SECOND ONE.
# `ChildResourceLimitTests` and `ResourceLimitMathTests` below both skip with
# a reason naming a CAPABILITY, not a binary — "resource module unavailable"
# and (before this fix) the compound "git or the resource module is
# unavailable". Neither matches `_SKIP_REASON_BINARY`, so BOTH were
# UNEXPLAINED no matter how genuinely absent the capability was — reproduced
# by running this suite with `git` off PATH: "SKIP CEILING EXCEEDED — 1 of 6
# skip(s) could not be explained", non-zero exit, on a wholly legitimate
# skip; the same fires on any host with no `resource` module (a non-POSIX
# host, which this file's own SAFETY section already documents as a real,
# supported degradation, not a defect). The ceiling's own two self-tests
# below only ever exercised the PATH shape, so this was never run against a
# reason the suite actually emits.
#
# Fixed the same way M18 fixed the binary shape, one dimension over: a second
# recognised SKIP REASON shape, "<name> module unavailable", explained only
# when a FRESH, independent `importlib.import_module(name)` call — never a
# cached flag such as `M.RLIMITS_AVAILABLE`, which a test body's own
# `mock.patch.object(M, "RLIMITS_AVAILABLE", False)` could have altered by
# the time a skip is being explained, the exact class of "the flag that lied"
# M18 was written against — confirms the module genuinely does not import
# right now. No hard-coded registry of capability names: `importlib` can
# attempt any name, exactly as `shutil.which` can look up any binary name,
# so "resource" is explained by a real absence and a nonsense name a
# self-test invents is explained the same way, with no list to keep in sync.
#
# THE OTHER HALF OF THE FIX is splitting the COMPOUND reason apart —
# "git or the resource module is unavailable" names TWO things at once, so
# even a correctly-implemented capability shape could not parse it as either
# alone. `ChildResourceLimitTests` below now stacks two single-reason
# `skipUnless` decorators instead of ANDing the conditions into one
# decorator with one prose reason, so a skip there is explained (or not) by
# checking the ONE condition that actually caused it — never both at once —
# which is the same "every reason names exactly one thing" property M25's
# own fix note asks CHECK 2's decline buckets for.
# ---------------------------------------------------------------------------
_SKIP_REASON_BINARY = re.compile(r"\A([A-Za-z0-9_.+-]+) is not on PATH\Z")
_SKIP_REASON_CAPABILITY = re.compile(
    r"\A([A-Za-z_][A-Za-z0-9_.]*) module unavailable\Z")


def _skip_binary_from_reason(reason):
    """The binary name a skip reason shaped like '<binary> is not on PATH'
    names, or None when the reason is not that shape at all."""
    m = _SKIP_REASON_BINARY.match((reason or "").strip())
    return m.group(1) if m else None


def _skip_capability_from_reason(reason):
    """The module name a skip reason shaped like '<name> module unavailable'
    names, or None when the reason is not that shape at all. M26."""
    m = _SKIP_REASON_CAPABILITY.match((reason or "").strip())
    return m.group(1) if m else None


def _module_importable(name):
    """A FRESH check — not a cached flag — of whether `name` can be imported
    right now. M26: the capability-shape twin of `shutil.which`, general on
    purpose so no per-capability registry is needed; a genuinely-missing
    module (a self-test's nonsense name, or `resource` on a non-POSIX host)
    fails to import and is EXPLAINED, exactly as a genuinely-missing binary
    is."""
    try:
        importlib.import_module(name)
        return True
    except Exception:
        return False


def unexplained_skips(skipped):
    """Every (test, reason) in `skipped` (a TestResult's own `.skipped` list)
    that is NOT explained by a binary or capability genuinely absent right
    now.

    Re-checks fresh for every skip rather than trusting whichever HAVE_* flag
    or cached module-level flag produced it — a flag computed once at import
    time is exactly what a stubbed `shutil.which` (M18's own reproduction),
    a mocked `M.RLIMITS_AVAILABLE` (M26's own reproduction, one capability
    over), or a plain bug in `_has()` would have already fooled."""
    out = []
    for test, reason in skipped:
        binary = _skip_binary_from_reason(reason)
        if binary is not None:
            if shutil.which(binary) is None:
                continue                  # explained: genuinely absent
            out.append((test, reason))
            continue
        capability = _skip_capability_from_reason(reason)
        if capability is not None:
            if not _module_importable(capability):
                continue                  # explained: genuinely unavailable
            out.append((test, reason))
            continue
        out.append((test, reason))        # no recognised shape at all
    return out


# Self-test hooks for the M18/M26 ceiling logic, exercised by CliTests below
# via a fresh subprocess of THIS file. Each is defined ONLY when its matching
# env var is set, so neither ever exists — and neither ever costs a line of
# `unittest.main()`'s own discovery — in a normal run of this suite.
if os.environ.get("_DCC_SELFTEST_FAKE_UNEXPLAINED_SKIP") == "1":
    class _FakeUnexplainedSkipProbe(unittest.TestCase):
        # `grep` genuinely IS on PATH in every environment this suite runs
        # in (HAVE_GREP-gated classes above depend on it) — this reason is
        # FALSE, exactly the shape a stubbed `shutil.which` or a `_has()`
        # bug would produce, and is what test_an_unexplained_skip_fails_the_
        # run below proves the ceiling catches.
        @unittest.skip("grep is not on PATH")
        def test_fake_skip(self):
            pass

if os.environ.get("_DCC_SELFTEST_FAKE_LEGITIMATE_SKIP") == "1":
    class _FakeLegitimateSkipProbe(unittest.TestCase):
        @unittest.skip("zzz-nonexistent-binary-4f2c1a is not on PATH")
        def test_fake_skip(self):
            pass

# M26's own pair, the capability shape's twin of the two probes above.
if os.environ.get("_DCC_SELFTEST_FAKE_UNEXPLAINED_CAPABILITY_SKIP") == "1":
    class _FakeUnexplainedCapabilitySkipProbe(unittest.TestCase):
        # `resource` genuinely IS importable on every POSIX host this suite
        # runs on (the `RLIMITS_AVAILABLE`-gated classes below depend on
        # that being true) — this reason is FALSE, exactly the shape a
        # mocked `M.RLIMITS_AVAILABLE` would produce.
        @unittest.skip("resource module unavailable")
        def test_fake_skip(self):
            pass

if os.environ.get("_DCC_SELFTEST_FAKE_LEGITIMATE_CAPABILITY_SKIP") == "1":
    class _FakeLegitimateCapabilitySkipProbe(unittest.TestCase):
        @unittest.skip("zzz_nonexistent_module_4f2c1a module unavailable")
        def test_fake_skip(self):
            pass

# True whenever THIS process is already a self-test subprocess spawned by one
# of the four CliTests cases below. Every one of these env vars runs the
# WHOLE file again — including CliTests itself — so without this guard each
# spawned subprocess would spawn another or itself, recursing until the
# runner falls over (reproduced once while writing this: dozens of stacked
# interpreters).
_IN_SELFTEST_SUBPROCESS = bool(
    os.environ.get("_DCC_SELFTEST_FAKE_UNEXPLAINED_SKIP")
    or os.environ.get("_DCC_SELFTEST_FAKE_LEGITIMATE_SKIP")
    or os.environ.get("_DCC_SELFTEST_FAKE_UNEXPLAINED_CAPABILITY_SKIP")
    or os.environ.get("_DCC_SELFTEST_FAKE_LEGITIMATE_CAPABILITY_SKIP"))


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

    # -- M27 — the case above is a TAUTOLOGICAL LOCK, and the whole deferred-
    # import fix was otherwise unprotected. Restoring the module-scope
    # `DCC = _load_consistency()` this round's own docstring names — verbatim
    # the pre-fix code — leaves the case above green too, because it asserts
    # only `returncode == 0` and `"--repo" in stdout`, both of which hold
    # whether the import runs deferred (as intended) or at module scope (the
    # M13 defect): the real repo's own doc-consistency-check.py is present
    # either way, so the import always succeeds and `--help` always exits 0
    # regardless of WHEN it ran. Nothing here drove the actual contract this
    # fix exists for — the import failing, and the exit-2 routing that
    # follows. The three cases below do, each against a throwaway copy of
    # tools/ built by `_write_tool_copy` so nothing here touches the real
    # tree.
    def test_help_still_exits_0_with_the_sibling_missing(self):
        # The deferred-import half: `--help` must never touch the sibling at
        # all, so removing it changes nothing about `--help`. Reverting to a
        # module-scope import would make THIS case fail — the bare `import`
        # statement runs before argparse gets to print anything, and a
        # missing sibling raises an uncaught OSError out of it.
        with tempfile.TemporaryDirectory() as d:
            dest = pathlib.Path(d) / "tools"
            _write_tool_copy(dest, include_sibling=False)
            proc = subprocess.run(
                (sys.executable, str(dest / "doc-claim-check.py"), "--help"),
                stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
            self.assertEqual(proc.returncode, 0, proc.stdout + proc.stderr)
            self.assertIn("--repo", proc.stdout)

    def test_repo_exits_2_with_the_sibling_missing(self):
        # The exit-2 routing half: once real work is asked for, the missing
        # sibling must be reported through this file's own named-error
        # convention (exit 2, "could not import") rather than a bare
        # traceback at exit 1 — the round-17 (M13) defect this whole fix
        # closed.
        with tempfile.TemporaryDirectory() as d:
            dest = pathlib.Path(d) / "tools"
            _write_tool_copy(dest, include_sibling=False)
            fixture = pathlib.Path(d) / "fixture-repo"
            fixture.mkdir()
            (fixture / "CLAUDE.md").write_text("# Fixture\n", encoding="utf-8")
            proc = subprocess.run(
                (sys.executable, str(dest / "doc-claim-check.py"),
                 "--repo", str(fixture)),
                stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
            self.assertEqual(proc.returncode, 2, proc.stdout + proc.stderr)
            self.assertIn("could not import", proc.stdout)

    def test_repo_exits_2_on_a_contract_break_in_the_sibling(self):
        # The other failure `_ensure_consistency_module()` names: the
        # sibling imports fine but no longer exposes every name in
        # `_DCC_CONTRACT` — a stub missing `blank_frozen_history` alone,
        # every other contract name present, so this proves the per-name
        # check rather than "the sibling is broken somehow".
        with tempfile.TemporaryDirectory() as d:
            dest = pathlib.Path(d) / "tools"
            stub = ("def record_regions(rel, text):\n"
                   "    return ()\n"
                   "def sentence_window(text, start, end):\n"
                   "    return ''\n"
                   "LOG_BODY_FILES = set()\n"
                   "# blank_frozen_history deliberately NOT defined\n")
            _write_tool_copy(dest, sibling_stub=stub)
            fixture = pathlib.Path(d) / "fixture-repo"
            fixture.mkdir()
            (fixture / "CLAUDE.md").write_text("# Fixture\n", encoding="utf-8")
            proc = subprocess.run(
                (sys.executable, str(dest / "doc-claim-check.py"),
                 "--repo", str(fixture)),
                stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
            self.assertEqual(proc.returncode, 2, proc.stdout + proc.stderr)
            self.assertIn("no longer exposes", proc.stdout)
            self.assertIn("blank_frozen_history", proc.stdout)

    # -- M26: the capability shape's own end-to-end pair, the twin of M18's
    # two binary-shape cases below.
    if not _IN_SELFTEST_SUBPROCESS:
        def test_an_unexplained_capability_skip_fails_the_run(self):
            env = dict(os.environ,
                      _DCC_SELFTEST_FAKE_UNEXPLAINED_CAPABILITY_SKIP="1")
            proc = subprocess.run(
                (sys.executable, str(TEST_PATH)),
                stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True,
                env=env)
            self.assertNotEqual(proc.returncode, 0,
                                proc.stdout + proc.stderr)
            self.assertIn("SKIP CEILING EXCEEDED",
                          proc.stdout + proc.stderr)

        def test_a_skip_naming_a_genuinely_unavailable_capability_does_not_fail_the_run(
                self):
            env = dict(os.environ,
                      _DCC_SELFTEST_FAKE_LEGITIMATE_CAPABILITY_SKIP="1")
            proc = subprocess.run(
                (sys.executable, str(TEST_PATH)),
                stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True,
                env=env)
            self.assertEqual(proc.returncode, 0, proc.stdout + proc.stderr)
            self.assertNotIn("SKIP CEILING EXCEEDED",
                             proc.stdout + proc.stderr)

    # -- M18: the skip ceiling, driven end to end as its own subprocess -----
    #
    # Both cases spawn a subprocess of THIS file with a self-test env var
    # set. Neither method is even DEFINED (not skipped — genuinely absent
    # from the class, so it adds no skip entry either) inside a subprocess
    # already running under one of those two env vars — see
    # _IN_SELFTEST_SUBPROCESS above for why: without this, each spawned
    # subprocess would spawn another generation of itself, recursing until
    # the runner falls over (reproduced once while writing this).
    if not _IN_SELFTEST_SUBPROCESS:
        def test_an_unexplained_skip_fails_the_run(self):
            env = dict(os.environ, _DCC_SELFTEST_FAKE_UNEXPLAINED_SKIP="1")
            proc = subprocess.run(
                (sys.executable, str(TEST_PATH)),
                stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True,
                env=env)
            self.assertNotEqual(proc.returncode, 0,
                                proc.stdout + proc.stderr)
            self.assertIn("SKIP CEILING EXCEEDED",
                          proc.stdout + proc.stderr)

        def test_a_skip_naming_a_genuinely_absent_binary_does_not_fail_the_run(
                self):
            env = dict(os.environ, _DCC_SELFTEST_FAKE_LEGITIMATE_SKIP="1")
            proc = subprocess.run(
                (sys.executable, str(TEST_PATH)),
                stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True,
                env=env)
            self.assertEqual(proc.returncode, 0, proc.stdout + proc.stderr)
            self.assertNotIn("SKIP CEILING EXCEEDED",
                             proc.stdout + proc.stderr)


class SkipCeilingLogicTests(unittest.TestCase):
    """unexplained_skips() in isolation — no subprocess, so this pair is the
    fast complement to CliTests' two end-to-end cases above."""

    def test_a_reason_naming_a_present_binary_is_unexplained(self):
        # grep is on PATH in every environment this suite runs in (several
        # HAVE_GREP-gated classes above already depend on that being true).
        self.assertEqual(
            len(unexplained_skips([("t", "grep is not on PATH")])), 1)

    def test_a_reason_naming_a_genuinely_absent_binary_is_explained(self):
        self.assertEqual(unexplained_skips(
            [("t", "zzz-nonexistent-binary-4f2c1a is not on PATH")]), [])

    def test_a_reason_with_no_recognisable_binary_is_unexplained(self):
        self.assertEqual(
            len(unexplained_skips([("t", "some other reason entirely")])), 1)

    def test_an_explained_and_an_unexplained_skip_are_told_apart(self):
        skipped = [("t1", "grep is not on PATH"),
                  ("t2", "zzz-nonexistent-binary-4f2c1a is not on PATH")]
        out = unexplained_skips(skipped)
        self.assertEqual([t for t, _r in out], ["t1"])

    # -- M26: the capability shape's fast complements to the CliTests pair --

    def test_a_reason_naming_an_importable_module_is_unexplained(self):
        # `resource` genuinely IS importable on every POSIX host this suite
        # runs on (the RLIMITS_AVAILABLE-gated classes above depend on it).
        self.assertEqual(
            len(unexplained_skips([("t", "resource module unavailable")])),
            1)

    def test_a_reason_naming_a_genuinely_unimportable_module_is_explained(
            self):
        self.assertEqual(unexplained_skips(
            [("t", "zzz_nonexistent_module_4f2c1a module unavailable")]), [])

    def test_a_capability_and_a_binary_reason_are_both_recognised_in_one_run(
            self):
        # The compound reason M26 found unlocked — "git or the resource
        # module is unavailable" — named two things at once; this is its
        # replacement's complement, one binary reason and one capability
        # reason in the SAME list, each explained (or not) on its own terms
        # rather than folded into a single ANDed condition.
        skipped = [("t1", "grep is not on PATH"),
                  ("t2", "resource module unavailable")]
        out = unexplained_skips(skipped)
        self.assertEqual({t for t, _r in out}, {"t1", "t2"})


# ---------------------------------------------------------------------------
# (8) LOAD-BEARING GUARD REGRESSIONS — one test per mutant that survived two
# independent mutation batteries (M17). Each is written so that its own
# helper being deleted, short-circuited or reverted to the pre-fix behaviour
# is what turns it red; each was mutation-verified against a scratch copy of
# the tool (see the landing report). Several deliberately go through scan()
# rather than the helper alone, per M17's own fix note: "the current test
# cannot distinguish a working census from a deleted call site."
# ---------------------------------------------------------------------------


@unittest.skipUnless(HAVE_GREP, "grep is not on PATH")
class DocumentRelativeOperandTests(FixtureCase):
    """`document_relative_operand` — a bare filename quoted from a nested
    spec is ambiguous between the repo root and the quoting document's own
    directory, and a checker must never guess which one was meant."""

    def test_a_bare_sibling_filename_declines_as_ambiguous(self):
        self.write("docs/specs/demo/section-2.md", "TODO one\nTODO two\n")
        claim, (outcome, payload) = self.check(
            "references its neighbour: `grep -c TODO section-2.md` -> 2",
            rel="docs/specs/demo/section-1.md")
        self.assertEqual(outcome, "declined", payload)
        bucket, why = payload
        self.assertEqual(bucket, "did-not-run")
        self.assertIn("ambiguous", why)
        self.assertIn("section-2.md", why)


class NegatorPrecisionTests(FixtureCase):
    """NEGATOR — round 16 M5: widened back to bare `not`/`was`/`never`, this
    excuses a WRONG claim merely because one of those words sits somewhere
    nearby in ordinary prose, which is exactly how a claim wrong by a factor
    of 33 was declined as historical instead of reported."""

    @unittest.skipUnless(HAVE_GREP, "grep is not on PATH")
    def test_a_bare_negation_word_near_a_wrong_claim_does_not_excuse_it(self):
        _claim, (outcome, payload) = self.check(
            "This was decided by the owner and is not in dispute: "
            "`%s` -> 99" % TRUE_CMD)
        self.assertEqual(outcome, "mismatch", payload)


class FenceOffsetMappingTests(FixtureCase):
    """`_map_offset` — round 17 M11: the dangling-reference line number used
    to come from an unconditional-break search of the WHOLE FILE TEXT for the
    reference's own spelling, so a PROSE mention of the same text earlier in
    the file could steal the line number that belongs to the real fence
    occurrence."""

    def test_the_reported_line_is_the_fence_occurrence_not_a_prose_echo(self):
        text = (
            "# Spec\n\n"
            "Prose note: Foo.Nope is mentioned here for illustration only.\n\n"
            "```csharp\n"
            "public class Foo { public int Bar; }\n"
            "int x = Foo.Nope;\n"
            "```\n")
        self.write("docs/specs/demo/section-1.md", text)
        findings, _coverage = M.scan_fence_identifiers(self.dir)
        self.assertEqual(len(findings), 1, findings)
        _rel, line, typ, mem, _near = findings[0]
        self.assertEqual((typ, mem), ("Foo", "Nope"))
        fence_line = text.count("\n", 0, text.index("int x = Foo.Nope;")) + 1
        prose_line = text.count("\n", 0, text.index("Prose note: Foo.Nope")) + 1
        self.assertNotEqual(fence_line, prose_line,
                            "fixture must place the prose echo on a "
                            "DIFFERENT line for this to prove anything")
        self.assertEqual(line, fence_line)


class SurfaceDeduplicationTests(FixtureCase):
    """`_dedup_by_resolved_path` — round 16 M6: a SURFACE_GLOBS entry that
    matches a file another entry already matched used to double-scan and
    double-report it. Driven through `collect_surfaces` rather than the
    helper alone, so a short-circuited helper is what turns this red, not
    just an isolated unit result."""

    def test_collect_surfaces_deduplicates_an_overlapping_glob(self):
        self.spec_file("body\n", rel="docs/specs/demo/section-1.md")
        with mock.patch.object(M, "SURFACES", ()), \
             mock.patch.object(M, "SURFACE_GLOBS",
                               ("docs/specs/*/section-*.md",
                                "docs/specs/*/section-1.md")):
            files, missing = M.collect_surfaces(self.dir)
        self.assertEqual(missing, [])
        rels = [rel for rel, _p in files]
        self.assertEqual(rels.count("docs/specs/demo/section-1.md"), 1,
                         "the same file was scanned twice: %r" % rels)


class CensusWiringTests(FixtureCase):
    """The unrecognised-shape CENSUS — round 16 H7's own self-reporting
    bucket already has a unit test of `unrecognised_spans` in MatcherTests,
    but that cannot distinguish a working census from a deleted call site
    inside scan() itself (M17's own fix note). This drives scan() end to end
    and reads the count back out of its printed decline-bucket line."""

    @unittest.skipUnless(HAVE_GREP, "grep is not on PATH")
    def test_scan_reports_an_unrecognised_shape_through_its_declined_bucket(self):
        self.write("CLAUDE.md",
                   "# Fixture\n\n35 assemblies. The gate runs `%s` nightly.\n\n"
                   "`%s` -> %d\n" % (TRUE_CMD, TRUE_CMD, TRUE_VALUE))
        self.spec_file("prose only\n")
        code, out = self.scan()
        self.assertEqual(code, 0, out)
        m = re.search(r"(\d+) unrecognised-shape", out)
        self.assertIsNotNone(m, "no unrecognised-shape count in output:\n%s"
                             % out)
        self.assertGreaterEqual(int(m.group(1)), 1)


class CurrencyAssertedLineBoundTests(FixtureCase):
    """`currency_asserted`'s line bound — a "now"/"currently" that belongs to
    the NEXT sentence must not reach back and pierce the excusal of a claim
    on the line before it."""

    def test_a_currency_word_on_the_next_line_does_not_pierce(self):
        prefix = "record: `%s` -> 92\n" % TRUE_CMD
        suffix = "this next line currently states something unrelated.\n"
        text = prefix + suffix
        m = re.search(r"92", prefix)
        self.assertFalse(M.currency_asserted(text, m.start(), m.end()))

    def test_a_currency_word_on_the_same_line_does_pierce(self):
        text = "record: `%s` -> 92, still true now\n" % TRUE_CMD
        m = re.search(r"92", text)
        self.assertTrue(M.currency_asserted(text, m.start(), m.end()))


# ---------------------------------------------------------------------------
# (9) ROUND 16 LOCKS — the fixes this round landed with no coverage at all.
# Each is written red against the pre-fix tool (verified against a scratch
# copy; see the landing report), the same discipline as section (8) above.
# ---------------------------------------------------------------------------


class GnuLongOptionAbbreviationTests(FixtureCase):
    """Round 18 (H12): `getopt_long` accepts any UNAMBIGUOUS PREFIX of a long
    option name, so denying only the full spelling left `--o=`, `--out=` and
    `--outpu=` all reaching `--output`'s write. None of these needs the
    binary on PATH — every one is declined before a process is spawned."""

    def test_sort_o_equals_abbreviates_output(self):
        why = self.assertDeclined("`sort --o=CANARY data.txt` -> 3",
                                  "unsafe", "ABBREVIATION")
        self.assertIn("--output", why)
        self.assertNoFile("CANARY")

    def test_sort_out_equals_abbreviates_output(self):
        why = self.assertDeclined("`sort --out=CANARY data.txt` -> 3",
                                  "unsafe", "ABBREVIATION")
        self.assertIn("--output", why)
        self.assertNoFile("CANARY")

    def test_sort_outpu_equals_abbreviates_output(self):
        why = self.assertDeclined("`sort --outpu=CANARY data.txt` -> 3",
                                  "unsafe", "ABBREVIATION")
        self.assertIn("--output", why)
        self.assertNoFile("CANARY")

    def test_sort_compress_progr_equals_abbreviates_compress_program(self):
        self.write("p.sh", "#!/bin/sh\ntouch CANARY\n")
        why = self.assertDeclined(
            "`sort --compress-progr=./p.sh -S 1 data.txt` -> 3",
            "unsafe", "ABBREVIATION")
        self.assertIn("--compress-program", why)
        self.assertNoFile("CANARY")

    def test_git_grep_open_f_equals_abbreviates_open_files_in_pager(self):
        self.write("p.sh", "#!/bin/sh\ntouch CANARY\n")
        why = self.assertDeclined(
            "`git grep --open-f=./p.sh alpha` -> 3",
            "unsafe", "ABBREVIATION")
        self.assertIn("--open-files-in-pager", why)
        self.assertNoFile("CANARY")


class ClusteredShortOptionTests(FixtureCase):
    """Round 21 (H1): an ATTACHED short-option value is a SUFFIX of its
    token, and a CLUSTER puts it further along than index 2 —
    `grep -cf/etc/hostname data.txt` hands the child `/etc/hostname`, not the
    `f/etc/hostname` a single-guess extractor computes. The complement matters
    more than the decline: over-refusing every one of these five legitimate
    clustered forms would be a silent coverage loss nobody would notice."""

    def test_grep_cf_slash_etc_hostname_is_declined(self):
        why = self.assertDeclined("`grep -cf/etc/hostname data.txt` -> 1",
                                  "unsafe", "resolves outside")
        self.assertIn("/etc/hostname", why)
        self.assertIn("SUFFIX", why)

    @unittest.skipUnless(HAVE_GREP, "grep is not on PATH")
    def test_grep_rn_pattern_dir_still_executes(self):
        self.write("sub/needle.txt", "pattern here\npattern again\n")
        self.assertIsNone(M.escaping_operand(
            ["grep", "-rn", "pattern", "sub/"], self.dir))
        self.assertIsNone(M.denied_flag(["grep", "-rn", "pattern", "sub/"]))
        out, why = M.run_pipeline([["grep", "-rn", "pattern", "sub/"]],
                                  str(self.dir))
        self.assertIsNone(why, why)
        self.assertIn("pattern", out)

    @unittest.skipUnless(HAVE_SORT, "sort is not on PATH")
    def test_sort_nr_file_still_executes(self):
        self.write("nums.txt", "3\n1\n2\n")
        self.assertIsNone(M.escaping_operand(["sort", "-nr", "nums.txt"],
                                             self.dir))
        self.assertIsNone(M.denied_flag(["sort", "-nr", "nums.txt"]))
        out, why = M.run_pipeline([["sort", "-nr", "nums.txt"]], str(self.dir))
        self.assertIsNone(why, why)
        self.assertEqual(out.split(), ["3", "2", "1"])

    @unittest.skipUnless(HAVE_LS, "ls is not on PATH")
    def test_ls_la_still_executes(self):
        self.assertIsNone(M.escaping_operand(["ls", "-la"], self.dir))
        self.assertIsNone(M.denied_flag(["ls", "-la"]))
        out, why = M.run_pipeline([["ls", "-la"]], str(self.dir))
        self.assertIsNone(why, why)

    @unittest.skipUnless(HAVE_WC, "wc is not on PATH")
    def test_wc_lc_file_still_executes(self):
        out, why = M.run_pipeline([["wc", "-lc", "data.txt"]], str(self.dir))
        self.assertIsNone(why, why)

    @unittest.skipUnless(HAVE_GREP, "grep is not on PATH")
    def test_grep_ic_pattern_file_still_executes(self):
        out, why = M.run_pipeline([["grep", "-ic", "ALPHA", "data.txt"]],
                                  str(self.dir))
        self.assertIsNone(why, why)
        self.assertEqual(M.single_integer(out), 1)


# M26: two STACKED single-reason decorators, not one compound-reason
# decorator ANDing both conditions — "git or the resource module is
# unavailable" named two things at once, which even a correctly-implemented
# capability shape could not parse as either alone. Stacking means a skip
# here is always explained (or not) by the ONE condition that actually
# caused it.
@unittest.skipUnless(HAVE_GIT, "git is not on PATH")
@unittest.skipUnless(M.RLIMITS_AVAILABLE, "resource module unavailable")
class ChildResourceLimitTests(FixtureCase):
    """Round 18 — OS-ENFORCED CHILD LIMITS, the one guard in the tool that is
    not an enumeration. `git status` is a READ-ONLY subcommand on the
    allow-list and yet rewrites `.git/index` on a stat-cache refresh — the
    tool's own SAFETY section names this BY NAME as "a hatch fixed by no
    name in this file". RLIMIT_FSIZE=0 is what stands between that and a
    printed PASS."""

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
        self.git_env = env
        # Force a stat-cache mismatch so `git status` has an index write to
        # perform on this run — a future mtime is enough, no sleep needed.
        p = self.dir / "data.txt"
        st = p.stat()
        os.utime(p, (st.st_atime + 5, st.st_mtime + 5))

    def index_bytes(self):
        return (self.dir / ".git" / "index").read_bytes()

    def test_git_status_index_write_is_killed_and_named_not_silent(self):
        before = self.index_bytes()
        why = self.assertDeclined("`git status \\| wc -l` -> 2",
                                  "did-not-run", "WRITE a file")
        self.assertIn("zero write limit", why)
        self.assertEqual(self.index_bytes(), before,
                         "the index changed despite RLIMIT_FSIZE=0")


class ResourceLimitMathTests(FixtureCase):
    """Round 21 (H2): `_lower_limit` used to clamp against the inherited HARD
    limit alone, so an inherited SOFT limit was RAISED — the opposite of its
    own docstring's guarantee (measured: an inherited RLIMIT_CPU of (10, 100)
    came out of the child as (60, 65)). `resource.getrlimit`/`setrlimit` are
    mocked throughout, so the real test process's own limits are never
    touched."""

    @unittest.skipUnless(M.RLIMITS_AVAILABLE, "resource module unavailable")
    def test_tightest_treats_rlim_infinity_as_no_limit_not_a_number(self):
        INF = M.resource.RLIM_INFINITY
        # The exact trap this exists to avoid: RLIM_INFINITY is -1 on Linux,
        # so a naive min(0, RLIM_INFINITY) is RLIM_INFINITY, not 0.
        self.assertEqual(M._tightest(0, INF), 0)
        self.assertEqual(M._tightest(INF, 0), 0)
        self.assertEqual(M._tightest(INF, INF), INF)
        self.assertEqual(M._tightest(10, 5, INF), 5)

    @unittest.skipUnless(M.RLIMITS_AVAILABLE, "resource module unavailable")
    def test_lower_limit_never_raises_an_inherited_soft_limit(self):
        calls = []
        with mock.patch.object(M.resource, "getrlimit",
                               return_value=(10, 100)), \
             mock.patch.object(M.resource, "setrlimit",
                               side_effect=lambda w, l: calls.append(l)):
            M._lower_limit(M.resource.RLIMIT_CPU, 60, 65)
        self.assertEqual(calls, [(10, 10)],
                         "an inherited soft limit of 10 must not come out "
                         "looser than it went in — got %r" % calls)

    @unittest.skipUnless(M.RLIMITS_AVAILABLE, "resource module unavailable")
    def test_lower_limit_clamps_against_an_infinite_inherited_hard_limit(self):
        INF = M.resource.RLIM_INFINITY
        calls = []
        with mock.patch.object(M.resource, "getrlimit",
                               return_value=(10, INF)), \
             mock.patch.object(M.resource, "setrlimit",
                               side_effect=lambda w, l: calls.append(l)):
            M._lower_limit(M.resource.RLIMIT_CPU, 60, 65)
        self.assertEqual(calls, [(10, 10)])

    @unittest.skipUnless(M.RLIMITS_AVAILABLE, "resource module unavailable")
    def test_lower_limit_keeps_fsize_at_zero_with_infinite_inherited_limits(self):
        INF = M.resource.RLIM_INFINITY
        calls = []
        with mock.patch.object(M.resource, "getrlimit",
                               return_value=(INF, INF)), \
             mock.patch.object(M.resource, "setrlimit",
                               side_effect=lambda w, l: calls.append(l)):
            M._lower_limit(M.resource.RLIMIT_FSIZE, 0, 0)
        self.assertEqual(calls, [(0, 0)])

    def test_child_limits_degrade_without_crashing_when_resource_is_missing(self):
        with mock.patch.object(M, "resource", None), \
             mock.patch.object(M, "RLIMITS_AVAILABLE", False):
            self.assertIsNone(M.child_limits_preexec())
            self.assertIn("UNAVAILABLE", M.child_limit_summary())

    @unittest.skipUnless(HAVE_GREP, "grep is not on PATH")
    def test_a_claim_still_executes_when_resource_is_unavailable(self):
        with mock.patch.object(M, "resource", None), \
             mock.patch.object(M, "RLIMITS_AVAILABLE", False):
            _claim, (outcome, payload) = self.check(
                "`%s` -> %d" % (TRUE_CMD, TRUE_VALUE))
        self.assertEqual(outcome, "reproduced", payload)


@unittest.skipUnless(HAVE_GREP, "grep is not on PATH")
class VersionHistoryExcusalTests(FixtureCase):
    """Round 19 (H13): a claim inside a `## Version History` section is a
    dated record of its OWN revision exactly like the CHANGELOG header chain
    ExcusalTests already covers — but nothing exercised the generic VH
    heading form directly until now."""

    VH_DOC = (
        "# Doc\n\n"
        "## Version History\n\n"
        "| Version | Notes |\n"
        "| 1.0 | measured `%s` -> 92 |\n"
        "| 1.1 | measured `%s` -> 93 (still true now) |\n\n"
        "## Somewhere else\n\n"
        "Outside the VH table: `%s` -> 94.\n"
        % (TRUE_CMD, TRUE_CMD, TRUE_CMD))

    def outcomes(self):
        regions = M.dated_record_regions("CLAUDE.md", self.VH_DOC)
        self.assertTrue(regions, "no Version History region was computed")
        return [M.check_claim(c, self.dir, regions)[0]
                for c in M.collect_claims("CLAUDE.md", self.VH_DOC)]

    def test_a_drifted_vh_claim_is_excused(self):
        self.assertEqual(self.outcomes()[0], "excused")

    def test_a_currency_word_pierces_the_vh_excusal(self):
        self.assertEqual(self.outcomes()[1], "mismatch")

    def test_a_claim_outside_the_vh_section_still_gates(self):
        self.assertEqual(self.outcomes()[2], "mismatch")

    def test_exit_2_when_every_claim_is_frozen_in_a_vh_section(self):
        # The LIVE floor: a corpus whose only claims sit inside a dated
        # record has zero drift-catching coverage and must not read PASS.
        self.write("CLAUDE.md",
                   "# Fixture\n\n## Version History\n\n"
                   "| 1.0 | `%s` -> %d |\n" % (TRUE_CMD, TRUE_VALUE))
        self.spec_file("prose only\n")
        code, out = self.scan()
        self.assertEqual(code, 2, out)
        self.assertIn("LIVE claim", out)


class LeadingValueTokenTests(FixtureCase):
    """`LeadingValueShape.rejects` — round 19 H16: a stated value must begin
    its own token. `§2.2.2` and `v1.73` share a section/version number's tail
    digit with the value pattern; an ordinary count does not."""

    def test_a_section_number_does_not_bind_a_claim(self):
        self.assertEqual(M.collect_claims(
            "CLAUDE.md", "verified against §2.2.2 (`%s`)" % TRUE_CMD), [])

    def test_a_version_number_does_not_bind_a_claim(self):
        self.assertEqual(M.collect_claims(
            "CLAUDE.md", "as of v1.73 (`%s`)" % TRUE_CMD), [])

    @unittest.skipUnless(HAVE_GREP, "grep is not on PATH")
    def test_a_plain_count_still_binds(self):
        claim = self.bind("%d files (`%s`)" % (TRUE_VALUE, TRUE_CMD))
        self.assertEqual(claim.shape.name, "value-then-parenthesised-command")
        self.assertEqual(M.check_claim(claim, self.dir, ())[0], "reproduced")

    @unittest.skipUnless(HAVE_GREP, "grep is not on PATH")
    def test_a_bold_total_with_attribution_still_binds(self):
        claim = self.bind("**%d total** (re-derived by `%s`)"
                          % (TRUE_VALUE, TRUE_CMD))
        self.assertEqual(claim.shape.name, "value-then-attributed-command")
        self.assertEqual(M.check_claim(claim, self.dir, ())[0], "reproduced")


@unittest.skipUnless(HAVE_GREP, "grep is not on PATH")
class CheckTwoCoverageFloorTests(FixtureCase):
    """Round 19 (H17): CHECK 2 had no coverage floor at all — blinding it
    completely (DECL_CLASS matching nothing, or no fence tag ever
    recognised) still printed PASS and exited 0."""

    CLEAN_FENCE = ("```csharp\npublic class Foo { public int Bar; }\n"
                   "int x = Foo.Bar;\n```\n")

    def true_claim(self):
        self.write("CLAUDE.md", "# Fixture\n\n`%s` -> %d\n"
                   % (TRUE_CMD, TRUE_VALUE))

    def test_exit_2_when_decl_class_matches_nothing(self):
        self.true_claim()
        self.spec_file(self.CLEAN_FENCE)
        with mock.patch.object(M, "DECL_CLASS", re.compile(r"(?!)")):
            code, out = self.scan()
        self.assertEqual(code, 2, out)
        self.assertIn("actually examined", out)

    def test_exit_2_when_no_fence_tag_is_recognised(self):
        self.true_claim()
        self.spec_file(self.CLEAN_FENCE)
        with mock.patch.object(M, "_CSHARP_TAGS", ()):
            code, out = self.scan()
        self.assertEqual(code, 2, out)
        self.assertIn("csharp/cs fence", out)


@unittest.skipUnless(HAVE_GREP, "grep is not on PATH")
class AnswerKindRegistrationTests(FixtureCase):
    """Round 19 (H18): registering a second answer kind used to crash —
    `declined` was built from DECLINE_BUCKETS alone, a table the SEAM 1
    banner's promise never mentioned, so a kind whose bucket was not already
    listed raised KeyError out of scan()."""

    def test_decline_bucket_order_includes_every_registered_answer_kind(self):
        class FakeAnswer:
            bucket = "fake-unreadable-kind"
        with mock.patch.object(M, "ANSWER_KINDS",
                               M.ANSWER_KINDS + (FakeAnswer(),)):
            order = M.decline_bucket_order()
        self.assertIn(("fake-unreadable-kind", "fake-unreadable-kind"), order)

    def test_record_decline_never_keyerrors_on_an_unforeseen_bucket(self):
        declined = {b: 0 for b, _label in M.DECLINE_BUCKETS}
        order = list(M.DECLINE_BUCKETS)
        M.record_decline(declined, order, "totally-novel-bucket")
        self.assertEqual(declined["totally-novel-bucket"], 1)
        self.assertIn(("totally-novel-bucket", "totally-novel-bucket"), order)
        M.record_decline(declined, order, "totally-novel-bucket")
        self.assertEqual(declined["totally-novel-bucket"], 2)

    def test_scan_tolerates_an_extra_registered_bucket_end_to_end(self):
        class FakeAnswer:
            name = "fake-kind"
            bucket = "fake-unreadable-kind"
        self.write("CLAUDE.md", "# Fixture\n\n`%s` -> %d\n"
                   % (TRUE_CMD, TRUE_VALUE))
        self.spec_file("prose only\n")
        with mock.patch.object(M, "ANSWER_KINDS",
                               M.ANSWER_KINDS + (FakeAnswer(),)):
            code, out = self.scan()
        self.assertEqual(code, 0, out)
        self.assertIn("fake-unreadable-kind", out)


class CensusShapeTests(FixtureCase):
    """The unrecognised-shape census — round 19 H15's positive shape test
    plus round 20 M15's FORBIDDEN-bearing fix, neither directly locked
    before now."""

    def test_a_quoted_head_reaches_the_census(self):
        text = ("35 assemblies. The gate runs `\"grep\" -c 'a' data.txt` "
                "nightly.")
        bound = {c.cmd_start for c in M.collect_claims("CLAUDE.md", text)}
        cmds = [cmd for _line, cmd in M.unrecognised_spans(text, bound)]
        self.assertIn("\"grep\" -c 'a' data.txt", cmds)

    def test_a_forbidden_bearing_span_is_named_not_dropped(self):
        text = ("Also `rm -rf / || true` is refused everywhere (see rule 6)."
                )
        bound = {c.cmd_start for c in M.collect_claims("CLAUDE.md", text)}
        hits = list(M.unrecognised_spans(text, bound))
        self.assertEqual([cmd for _line, cmd in hits], ["rm -rf / || true"])
        reason = M.unrecognised_span_reason(hits[0][1])
        self.assertIn("shell metacharacter", reason)


class SelfContainedPositionTests(FixtureCase):
    """`self_contained`'s POSITION rule — round 20 M21: a grep/awk PATTERN
    that happens to spell a real file must not count as the file operand, or
    a pattern-only command silently reads an empty stdin and its output is
    fabricated-compared against the document's stated value."""

    @unittest.skipUnless(HAVE_GREP, "grep is not on PATH")
    def test_a_pattern_naming_a_real_file_is_not_a_file_operand(self):
        # The pattern here is literally "data.txt" — the real fixture file —
        # and there is no OTHER operand at all.
        self.assertDeclined("`grep -c 'data.txt'` -> 3",
                            "not-self-contained", "empty stdin")

    @unittest.skipUnless(HAVE_GREP, "grep is not on PATH")
    def test_a_directory_operand_still_counts_and_executes(self):
        self.write("docs/readme.txt", "x has a needle\ny plain\n")
        expanded, why = M.expand_globs(M.tokenize("grep -rc x docs/"),
                                       self.dir)
        self.assertIsNone(why, why)
        self.assertTrue(M.self_contained(expanded, self.dir))
        out, why = M.run_pipeline(expanded, str(self.dir))
        self.assertIsNone(why, why)


@unittest.skipUnless(HAVE_GREP, "grep is not on PATH")
class PostExpansionRevalidationTests(FixtureCase):
    """`expand_globs`'s post-expansion re-run of the escape-hatch check — a
    reviewer had called it dead because removing it left all 71 tests green.
    `uniq tools/doc-*.py` is the exact case that proves otherwise: ONE
    pre-expansion operand, so `uniq`'s own operand-COUNT hatch does not fire
    pre-expansion, and it expands to TWO real files, neither starting with
    `-`, so the option-shaped-filename check misses it too."""

    def test_uniq_operand_count_hatch_survives_glob_expansion(self):
        self.write("tools/doc-a.py", "alpha content\n")
        self.write("tools/doc-b.py", "beta content\n")
        segments = M.tokenize("uniq tools/doc-*.py")
        self.assertEqual(len(segments[0]) - 1, 1,
                         "fixture must be ONE pre-expansion operand for this "
                         "to prove anything")
        expanded, why = M.expand_globs(segments, self.dir)
        self.assertIsNone(expanded)
        self.assertIsNotNone(why)
        self.assertIn("OUTPUT operand", why)
        self.assertEqual((self.dir / "tools/doc-a.py").read_text(),
                         "alpha content\n")
        self.assertEqual((self.dir / "tools/doc-b.py").read_text(),
                         "beta content\n")

    def test_uniq_glob_hatch_is_declined_through_a_full_claim(self):
        self.write("tools/doc-a.py", "alpha content\n")
        self.write("tools/doc-b.py", "beta content\n")
        self.assertDeclined("`uniq tools/doc-*.py` -> 3",
                            "unsafe", "OUTPUT operand")
        self.assertEqual((self.dir / "tools/doc-a.py").read_text(),
                         "alpha content\n")
        self.assertEqual((self.dir / "tools/doc-b.py").read_text(),
                         "beta content\n")


# ---------------------------------------------------------------------------
# (10) PART A — FOUR REVIEWED FINDINGS AGAINST THIS SUITE ITSELF (M25, M26,
# M27, M30). M26 and M27 are fixed above, in the skip-ceiling machinery and
# CliTests respectively; M25 and M30 are locked here.
# ---------------------------------------------------------------------------


class CheckTwoGuardRegressionTests(FixtureCase):
    """M25 — a 25-mutant battery found CHECK 2's own precision guards almost
    completely unlocked: 7 of 8 survived, 4 of them with a measured
    live-tree false-positive effect (97 / 3 / 2 / 1 false dangling findings
    when deleted) while the suite's only precision tests were one `Foo.Bar`
    case and one struct case. One fixture per surviving guard below, each
    mutation-verified against the guard's own deletion in a scratch copy of
    the tool (see the landing report for the exact edit and failure each
    produced)."""

    def test_an_enum_member_reference_does_not_dangle(self):
        # `_enum_members` — deleting it turned this into 97 false findings on
        # the live tree, because an enum's members are declared nowhere
        # DECL_MEMBER or DECL_LOOSE can see (neither pattern's grammar
        # allows for an enumerator, which has no preceding type).
        self.spec_file(
            "```csharp\n"
            "public enum Severity { Yellow, Red }\n"
            "public class Foo { public Severity S = Severity.Yellow; }\n"
            "```\n")
        findings, _coverage = M.scan_fence_identifiers(self.dir)
        self.assertEqual(findings, [])

    def test_a_declaration_inside_a_comment_suppresses_and_a_comment_reference_is_not_scanned(
            self):
        # `_strip_comments` — the SAME asymmetry this file's own code
        # comment states as deliberate ("comments COUNT as declarations ...
        # but NEVER as references"), in one fixture: `Bar`, declared only
        # inside a `/* ... */` sketch, must suppress `Foo.Bar` (declaration
        # harvesting reads the UNSTRIPPED text, by design); and
        # `Other.Update()`, mentioned only inside a `///` doc-comment line,
        # must never be SCANNED as a reference at all — deleting the
        # comment-strip before REFERENCE scanning turned this into 3 false
        # findings on the live tree, because a doc-comment naming another
        # spec's type and method is prose, not code that must satisfy
        # anything.
        self.spec_file(
            "```csharp\n"
            "public class Foo {\n"
            "    /* public int Bar; */\n"
            "}\n"
            "public class Other {\n"
            "    public void Tick() {}\n"
            "}\n"
            "/// Called by Other.Update()\n"
            "int x = Foo.Bar;\n"
            "```\n")
        findings, _coverage = M.scan_fence_identifiers(self.dir)
        self.assertEqual(findings, [])

    def test_a_namespace_path_does_not_bind_as_a_reference(self):
        # REFERENCE's own lookbehind/lookahead guards, which keep a
        # namespace path (neither side of `Physics` continues the chain
        # here) from being read as a member access at all — deleting them
        # turned this into 2 false findings on the live tree.
        self.spec_file(
            "```csharp\n"
            "public class TacticalDirector { public int Existing; }\n"
            "var y = TacticalDirector.Physics.Collision.Something;\n"
            "```\n")
        findings, _coverage = M.scan_fence_identifiers(self.dir)
        self.assertEqual(findings, [])

    def test_a_modifier_less_field_suppresses_via_the_loose_declaration_harvest(
            self):
        # DECL_LOOSE — the harvest for fields sketched with no access
        # modifier at all, which this corpus's spec fences use constantly
        # ("int NextStaffId;"). Deleting it turned this into 1 false finding
        # on the live tree.
        self.spec_file(
            "```csharp\n"
            "public class Foo {\n"
            "    int Count;\n"
            "}\n"
            "int x = Foo.Count;\n"
            "```\n")
        findings, _coverage = M.scan_fence_identifiers(self.dir)
        self.assertEqual(findings, [])

    def test_a_filename_shaped_member_does_not_dangle(self):
        # FILE_EXTENSIONS — `BallPhysicsConstants.cs` is a filename, not a
        # member access, written here as an ordinary string literal so the
        # comment-stripping guard above cannot be the thing suppressing it.
        self.spec_file(
            "```csharp\n"
            "public class BallPhysicsConstants { public float Gravity; }\n"
            "string path = \"BallPhysicsConstants.cs\";\n"
            "```\n")
        findings, _coverage = M.scan_fence_identifiers(self.dir)
        self.assertEqual(findings, [])

    def test_a_using_line_does_not_bind_as_a_reference(self):
        # `_USING_NAMESPACE_LINE` blanking — a `using X.Y;` line is a
        # declaration of a PATH, never a member access, and must not be
        # scanned as one even when `Y` coincides with a real member name of
        # a locally-declared type spelled the same as the namespace segment.
        self.spec_file(
            "```csharp\n"
            "using System.Text;\n"
            "public class System { public int Present; }\n"
            "int x = System.Present;\n"
            "```\n")
        findings, _coverage = M.scan_fence_identifiers(self.dir)
        self.assertEqual(findings, [])


class AwkEnvironmentAndLoadHatchTests(FixtureCase):
    """M30 — two live security guards (round 18's ENVIRON/PROCINFO refusal,
    round 15's @load/@include refusal) had ZERO coverage: deleting the
    ENVIRON/PROCINFO check left all 121 tests green although the mutated
    tool was proven to reproduce a value derived from a real environment
    variable — a one-integer read oracle over the CI runner's environment,
    driven entirely from document text, and the one rule the OS child
    limits explicitly do not cover (they bound writes, not reads). Neither
    case below needs awk on PATH — both are refused before a process
    spawns, exactly like the neighbouring, already-locked awk call
    allow-list and escape checks."""

    def test_awk_environ_read_is_declined_and_named(self):
        why = self.assertDeclined(
            "`awk 'END{print length(ENVIRON[\"PATH\"])}' data.txt` -> 3",
            "unsafe", "ENVIRON", "environment")
        self.assertIn("special array", why)

    def test_awk_procinfo_read_is_declined_and_named(self):
        self.assertDeclined(
            "`awk 'END{print PROCINFO[\"pid\"]}' data.txt` -> 3",
            "unsafe", "PROCINFO")

    def test_awk_at_load_is_declined_and_named(self):
        # No embedded newline (CLAIM's own `cmd` group is `[^`\n]{4,200}`,
        # so a backticked span may not contain one) and no `;` either
        # (FORBIDDEN refuses it before this ever reaches the awk-specific
        # check) — validity as an awk program does not matter, since this
        # is declined before any process spawns.
        self.assertDeclined(
            "`awk '@load \"fake\" END{print 3}' data.txt` -> 3",
            "unsafe", "@")

    def test_awk_at_include_is_declined_and_named(self):
        self.assertDeclined(
            "`awk '@include \"fake\" END{print 3}' data.txt` -> 3",
            "unsafe", "@")


# ---------------------------------------------------------------------------
# (11) PART B — ROUND 18 LOCKS. Round 18 landed a large amount of safety and
# correctness work this suite did not cover at all: awk's ARGV/ARGC/SYMTAB/
# FUNCTAB refusal, the symlink-traversal denials (per-flag AND the
# structural directory-walk rule), `--files0-from` on every binary that
# carries it (including a NON-FIRST pipeline segment, which is how it used
# to evade `self_contained`), the frozen-span bound, `would_gate`'s LIVE
# classification, the restricted verb-gap grammar, ignored-span recovery
# into the census, the compound-token rule on a leading value, the LIVE
# distinct-command floor, and the top-level exception boundary. Every case
# below is written red against the pre-fix tool first — a mutation of the
# CURRENT tool reproducing the pre-fix behaviour it replaced, unless noted
# otherwise — and re-verified green against the real one.
# ---------------------------------------------------------------------------


class AwkArgvArgcTests(FixtureCase):
    """Round 22 (H19) / round 18's own SYMTAB/FUNCTAB extension — awk's
    OPERAND VECTOR and gawk's symbol tables, refused by name because they
    are variables, not calls, so the CALL allow-list structurally cannot see
    either. Declined before a process spawns, so the read is demonstrably
    never attempted; `M.denied_flag` is asserted directly for that reason,
    ahead of the full-claim decline."""

    ARGV_CMD = ('awk \'BEGIN{ARGV[ARGC++]="/etc/passwd"}END{print NR}\' '
               "data.txt")

    def test_awk_argv_argc_assignment_is_refused_pre_spawn(self):
        self.assertIsNotNone(M.denied_flag(["awk",
                                            'BEGIN{ARGV[ARGC++]="/etc/passwd"}END{print NR}',
                                            "data.txt"]))

    def test_awk_argv_argc_claim_is_declined_and_the_read_never_happens(self):
        why = self.assertDeclined("`%s` -> 3" % self.ARGV_CMD,
                                  "unsafe", "ARGV")
        self.assertIn("OPERAND VECTOR", why)

    def test_awk_symtab_is_declined(self):
        self.assertDeclined(
            "`awk 'BEGIN{SYMTAB[\"x\"]=1}END{print 3}' data.txt` -> 3",
            "unsafe", "SYMTAB")

    def test_awk_functab_is_declined(self):
        self.assertDeclined(
            "`awk 'BEGIN{print length(FUNCTAB)}' data.txt` -> 3",
            "unsafe", "FUNCTAB")


class SymlinkTraversalTests(FixtureCase):
    """Round 22 (H20) — symlink-FOLLOWING flags declined per binary by name
    (the enumeration half), and the structural rule that no operand
    DIRECTORY may contain a symlink leaving the repository root, whatever
    flags were passed. Every symlink fixture is built inside the temp repo
    dir this case owns, or in a second throwaway temp dir standing in for
    the host outside it — never in the real checkout."""

    # -- the per-flag denials — refused before any process spawns, so none
    # of these five needs its binary on PATH.

    def test_grep_capital_r_is_declined(self):
        self.assertDeclined("`grep -Rc a data.txt` -> 3", "unsafe", "-R")

    def test_find_dash_L_is_declined(self):
        self.assertDeclined("`find -L . -name x` -> 3", "unsafe", "-L")

    def test_find_follow_is_declined(self):
        self.assertDeclined("`find . -follow -name x` -> 3",
                            "unsafe", "-follow")

    def test_rg_follow_is_declined(self):
        self.assertDeclined("`rg --follow -c a data.txt` -> 3",
                            "unsafe", "--follow")

    def test_diff_dash_r_is_declined(self):
        self.assertDeclined("`diff -r sub other` -> 3", "unsafe", "-r")

    # -- the structural rule: an operand DIRECTORY containing an escaping
    # symlink is declined even for the LOWERCASE, non-following form.

    def test_a_directory_containing_an_escaping_symlink_is_declined_for_plain_lowercase_r(
            self):
        outside = pathlib.Path(tempfile.mkdtemp(prefix="doc-claim-outside-"))
        self.addCleanup(shutil.rmtree, outside, ignore_errors=True)
        (outside / "secret.txt").write_text("secret\nsecret\n")
        sub = self.dir / "sub"
        sub.mkdir()
        (sub / "link").symlink_to(outside / "secret.txt")
        why = self.assertDeclined("`grep -rc a sub` -> 3",
                                  "unsafe", "symlink",
                                  "leaves the repository root")
        self.assertIn("link", why)

    # -- the complement: the same commands, non-following, on a CLEAN tree
    # (no symlink at all) still execute.

    @unittest.skipUnless(HAVE_GREP, "grep is not on PATH")
    def test_grep_lowercase_r_still_executes_on_a_clean_tree(self):
        self.write("sub/needle.txt", "alpha\n")
        self.assertIsNone(M.escaping_symlink_under(
            ["grep", "-rc", "alpha", "sub"], self.dir))
        # GNU grep -r always prefixes the filename ("sub/needle.txt:1"),
        # never a bare integer -- this proves EXECUTION, the property under
        # test, not single-integer parsing, which is a different matcher's
        # job and is exercised everywhere else in this suite.
        out, why = M.run_pipeline([["grep", "-rc", "alpha", "sub"]],
                                  str(self.dir))
        self.assertIsNone(why, why)
        self.assertIn("needle.txt:1", out)

    @unittest.skipUnless(HAVE_FIND, "find is not on PATH")
    def test_find_plain_still_executes_on_a_clean_tree(self):
        self.write("sub/needle.txt", "x\n")
        self.assertIsNone(M.escaping_symlink_under(
            ["find", "sub", "-name", "needle.txt"], self.dir))
        out, why = M.run_pipeline(
            [["find", "sub", "-name", "needle.txt"]], str(self.dir))
        self.assertIsNone(why, why)
        self.assertIn("needle.txt", out)


class Files0FromTests(FixtureCase):
    """Round 22 (the H20 sweep) — `--files0-from` reads the paths a binary
    opens out of ANOTHER FILE'S BYTES, a route no operand check of any kind
    can see. Denied by NAME on every binary that carries it, in EVERY
    pipeline segment — the live gap was `self_contained` inspecting only the
    FIRST segment, so `wc --files0-from=...` in second position read
    /etc/passwd untouched by the first-segment-only rule."""

    def test_wc_files0_from_is_declined(self):
        self.assertDeclined("`wc --files0-from=list.txt` -> 3",
                            "unsafe", "--files0-from")

    def test_sort_files0_from_is_declined(self):
        self.assertDeclined("`sort --files0-from=list.txt` -> 3",
                            "unsafe", "--files0-from")

    def test_find_files0_from_is_declined(self):
        self.assertDeclined("`find -files0-from list.txt` -> 3",
                            "unsafe", "-files0-from")

    def test_files0_from_is_declined_in_a_non_first_pipeline_segment(self):
        # `self_contained` only ever inspects the FIRST segment, so this is
        # the exact shape that evaded it — `sort` sits second, behind a
        # `cat` that legitimately reads a real in-repo file.
        why = self.assertDeclined(
            "`cat data.txt \\| sort --files0-from=list.txt \\| wc -l` -> 3",
            "unsafe", "--files0-from")
        self.assertIn("sort", why)


@unittest.skipUnless(HAVE_GREP, "grep is not on PATH")
class FrozenSpanBoundaryTests(FixtureCase):
    """The frozen-span bound `_blanked_runs` computes: two genuinely
    separate frozen regions (the header chain; a Version History section)
    must stay two regions, never merged across the LIVE, present-tense text
    that sits between them on this repo's own surfaces (round 23, H22's
    "Current Stage"-shaped status block, reproduced here as a fixture rather
    than depended on as a fact about README.md)."""

    CHAIN = (
        "# Doc\n\n"
        "**Last Updated:** head entry — `%s` → 91\n\n"
        "**Last Updated (prior):** older entry — `%s` → 92\n\n"
        "**Current Stage:** the count reads `%s` → 99\n\n"
        "## Version History\n\n"
        "| 1.0 | `%s` -> 92 |\n"
        % (TRUE_CMD, TRUE_CMD, TRUE_CMD, TRUE_CMD))

    def outcomes(self):
        regions = M.dated_record_regions("CLAUDE.md", self.CHAIN)
        return [M.check_claim(c, self.dir, regions)[0]
                for c in M.collect_claims("CLAUDE.md", self.CHAIN)]

    def test_the_genuine_chain_entry_stays_excused(self):
        self.assertEqual(self.outcomes()[1], "excused")

    def test_the_present_tense_status_block_after_the_chain_is_reported(
            self):
        self.assertEqual(self.outcomes()[2], "mismatch")

    def test_the_version_history_row_is_still_its_own_excused_region(self):
        self.assertEqual(self.outcomes()[3], "excused")


@unittest.skipUnless(HAVE_GREP, "grep is not on PATH")
class WouldGateLiveGateTests(FixtureCase):
    """Round 23 (H23) — a currency-pierced record claim must be counted
    LIVE and gate at exit 1, not read as zero live coverage and exit 2 with
    "this run could not have caught drift" — the exact demotion of a real
    document defect into a tooling error H23 fixed."""

    def test_a_currency_pierced_record_gates_at_exit_1(self):
        self.write(
            "CLAUDE.md",
            "# Doc\n\n"
            "**Last Updated (prior):** older, still true now: `%s` -> 92\n"
            % TRUE_CMD)
        self.spec_file("prose only\n")
        code, out = self.scan()
        self.assertEqual(code, 1, out)
        self.assertIn("document says 92; command returns 3", out)
        self.assertNotIn("could not have caught drift", out)


@unittest.skipUnless(HAVE_GREP, "grep is not on PATH")
class VerbGapPrecisionTests(FixtureCase):
    """Round 23 (H21) — the verb route's gap admits only whitespace,
    closing/emphasis markup and adverbs; a genuine subordinate clause
    between the command and its verb must not bind, and the plain arrow and
    plain verb forms must still bind either side of that restriction."""

    def test_a_subordinate_clause_before_the_verb_does_not_bind(self):
        text = ("`%s` returns, for the 2 tracking files, a count of %d."
               % (TRUE_CMD, TRUE_VALUE))
        self.assertEqual(M.collect_claims("CLAUDE.md", text), [])

    def test_the_plain_arrow_form_still_binds(self):
        claim = self.bind("`%s` -> %d" % (TRUE_CMD, TRUE_VALUE))
        self.assertEqual(M.check_claim(claim, self.dir, ())[0], "reproduced")

    def test_the_plain_verb_form_still_binds(self):
        claim = self.bind("`%s` returned %d" % (TRUE_CMD, TRUE_VALUE))
        self.assertEqual(M.check_claim(claim, self.dir, ())[0], "reproduced")


@unittest.skipUnless(HAVE_GREP, "grep is not on PATH")
class IgnoredSpanCensusTests(FixtureCase):
    """Round 23 (H25) — a span `check_claim` IGNORES (command-shaped, an
    integer nearby, but FORBIDDEN-bearing) must still reach the
    unrecognised-shape census; before this fix it reserved its own span and
    then landed in NO bucket at all, invisible on both routes at once."""

    def _census_out(self, cmd, value):
        self.write("CLAUDE.md",
                   "# Fixture\n\nmeasured via `%s` -> %d\n\n`%s` -> %d\n"
                   % (cmd, value, TRUE_CMD, TRUE_VALUE))
        self.spec_file("prose only\n")
        code, out = self.scan()
        self.assertEqual(code, 0, out)
        return out

    def test_curl_with_redirection_reaches_the_census(self):
        out = self._census_out("curl http://x > out.txt", 7)
        self.assertIn("curl http://x > out.txt", out)

    def test_make_with_a_chain_reaches_the_census(self):
        out = self._census_out("make build && echo 7", 7)
        self.assertIn("make build && echo 7", out)

    def test_dotnet_with_a_redirected_stderr_reaches_the_census(self):
        out = self._census_out("dotnet test foo.csproj 2>&1", 5)
        self.assertIn("dotnet test foo.csproj 2>&1", out)


class CompoundTokenPrecisionTests(FixtureCase):
    """Round 24 (M31) — a stated value must begin its own token: the
    character immediately before it may not be alphanumeric (a letter, as in
    `v2`, or an unseparated digit) or one of the punctuation joiners this
    corpus continues a token with (`.#§/:`). The old rule was a three-
    character enumeration (`.`, `#`, `§`) that missed every one of these."""

    def test_a_slash_separated_ratio_does_not_bind(self):
        self.assertEqual(M.collect_claims(
            "CLAUDE.md", "test counts 461/1/2 (`%s`)" % TRUE_CMD), [])

    def test_a_clock_time_does_not_bind(self):
        self.assertEqual(M.collect_claims(
            "CLAUDE.md", "at 16:59:45 (`%s`)" % TRUE_CMD), [])

    def test_a_bare_letter_prefixed_version_does_not_bind(self):
        self.assertEqual(M.collect_claims(
            "CLAUDE.md", "v2 (`%s`)" % TRUE_CMD), [])

    def test_a_slash_dated_value_does_not_bind(self):
        self.assertEqual(M.collect_claims(
            "CLAUDE.md", "measured 2026/08/22 (`%s`)" % TRUE_CMD), [])

    @unittest.skipUnless(HAVE_GREP, "grep is not on PATH")
    def test_an_ordinary_count_still_binds(self):
        claim = self.bind("%d scripts (`%s`)" % (TRUE_VALUE, TRUE_CMD))
        self.assertEqual(claim.shape.name, "value-then-parenthesised-command")
        self.assertEqual(M.check_claim(claim, self.dir, ())[0], "reproduced")


@unittest.skipUnless(HAVE_GREP, "grep is not on PATH")
class LiveDistinctFloorTests(FixtureCase):
    """Round 24 (L12) — the LIVE floor gates on DISTINCT commands, exactly
    as the executed floor already does (round 20, M20): one command quoted
    twice is one drift-capable claim, not two, and must not satisfy a floor
    of 2."""

    def test_one_command_quoted_twice_does_not_satisfy_a_floor_of_two(self):
        self.write("CLAUDE.md",
                   "# Fixture\n\n`%s` -> %d\n\nAgain: `%s` -> %d\n"
                   % (TRUE_CMD, TRUE_VALUE, TRUE_CMD, TRUE_VALUE))
        self.spec_file("prose only\n")
        code, out = self.scan(MIN_LIVE_CLAIMS=2)
        self.assertEqual(code, 2, out)
        self.assertIn("distinct LIVE command(s)", out)


class TopLevelBoundaryTests(unittest.TestCase):
    """Round 24 (L14) — an unforeseen error anywhere inside `scan()` must be
    routed through the named ERROR block at exit 2, never propagate
    uncaught to Python's default handler at exit 1, the code CHECK 1 uses
    for "a document is wrong"."""

    def test_an_unforeseen_error_exits_2_not_1(self):
        with tempfile.TemporaryDirectory() as d:
            repo = pathlib.Path(d)
            (repo / "CLAUDE.md").write_text("# Fixture\n", encoding="utf-8")
            argv = ["doc-claim-check.py", "--repo", str(repo)]
            buf = io.StringIO()
            with mock.patch.object(sys, "argv", argv), \
                 mock.patch.object(M, "scan",
                                   side_effect=RuntimeError("boom")), \
                 contextlib.redirect_stdout(buf):
                code = M.main()
        self.assertEqual(code, 2, buf.getvalue())
        self.assertIn("unforeseen error", buf.getvalue())



if __name__ == "__main__":
    # M18: `exit=False` so this file, not unittest's own default, decides the
    # process exit code — `wasSuccessful()` alone counts a skip as neither a
    # pass nor a failure, which is how the whole suite went vacuously green
    # under a stubbed `shutil.which`. See unexplained_skips() above.
    _program = unittest.main(verbosity=2, exit=False)
    _result = _program.result
    _bad_skips = unexplained_skips(_result.skipped)
    if _bad_skips:
        sys.stderr.write(
            "\nSKIP CEILING EXCEEDED (M18) — a skip that leaves the exit "
            "code at 0 is silent to the only consumer that acts on it. "
            "%d of %d skip(s) could not be explained by a binary genuinely "
            "absent from PATH right now:\n"
            % (len(_bad_skips), len(_result.skipped)))
        for _test, _reason in _bad_skips:
            sys.stderr.write("  - %s: %s\n" % (_test, _reason))
        sys.exit(1)
    sys.exit(0 if _result.wasSuccessful() else 1)

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
# | 1.1     | 2026-08-22 | Claude Code | M17 + M18 (two reviewed findings      |
# |         |            |             | against this suite) plus round 16's   |
# |         |            |             | own fixes, which had landed with no   |
# |         |            |             | coverage at all — the same shape that |
# |         |            |             | made round 15's fixes silently        |
# |         |            |             | regressable. 71 -> 121 tests, 0.39s   |
# |         |            |             | -> ~1.9s. M17: two independent        |
# |         |            |             | mutation batteries (38 and 15         |
# |         |            |             | mutants) agreed on 18 survivors —     |
# |         |            |             | one test per survivor now (section    |
# |         |            |             | 8), several driving scan() end to end |
# |         |            |             | rather than the helper alone, per     |
# |         |            |             | M17's own fix note that the old       |
# |         |            |             | coverage "cannot distinguish a        |
# |         |            |             | working census from a deleted call    |
# |         |            |             | site." M18: this file's own "a skip   |
# |         |            |             | is never silent" claim was false of   |
# |         |            |             | itself — `unittest.main()`'s exit     |
# |         |            |             | code ignores skips entirely, proven   |
# |         |            |             | by stubbing shutil.which for grep/    |
# |         |            |             | printf/git (71 ran, 39 skipped,       |
# |         |            |             | wasSuccessful() True). Fixed with a   |
# |         |            |             | SKIP CEILING in the `__main__` block: |
# |         |            |             | `unittest.main(exit=False)` plus an   |
# |         |            |             | explicit exit that fails the run      |
# |         |            |             | unless every skip's reason is         |
# |         |            |             | independently re-verified against a   |
# |         |            |             | FRESH `shutil.which` call — not the   |
# |         |            |             | HAVE_* flag that might itself have    |
# |         |            |             | been fooled — so a genuinely missing  |
# |         |            |             | binary still passes while a mass      |
# |         |            |             | silent skip does not; locked by two   |
# |         |            |             | self-test hooks driven as their own   |
# |         |            |             | subprocess (CliTests) plus fast       |
# |         |            |             | direct-call complements               |
# |         |            |             | (SkipCeilingLogicTests). Section 9    |
# |         |            |             | adds ten locks for round 16's own     |
# |         |            |             | landings: GNU long-option             |
# |         |            |             | abbreviation, clustered short-option  |
# |         |            |             | attached values (plus the five        |
# |         |            |             | legitimate clustered forms that must  |
# |         |            |             | keep executing), the OS child         |
# |         |            |             | resource limits (a real `git status`  |
# |         |            |             | index-write kill, the strictest-of-   |
# |         |            |             | three clamp, the RLIM_INFINITY trap,  |
# |         |            |             | and degradation with no `resource`    |
# |         |            |             | module), the generic Version History  |
# |         |            |             | excusal plus the LIVE floor, the      |
# |         |            |             | leading-value compound-token rule,    |
# |         |            |             | CHECK 2's own coverage floors,        |
# |         |            |             | ANSWER_KINDS registration not         |
# |         |            |             | KeyError-ing, two more census shapes  |
# |         |            |             | (a quoted head, a FORBIDDEN-bearing   |
# |         |            |             | span), self_contained's positional    |
# |         |            |             | pattern-operand rule, and the         |
# |         |            |             | post-expansion escape-hatch re-run    |
# |         |            |             | (`uniq tools/doc-*.py`, proven NOT    |
# |         |            |             | dead against the exact case a         |
# |         |            |             | reviewer had used to argue it was).   |
# |         |            |             | Every one of the 16 new load-bearing  |
# |         |            |             | tests (6 M17 + 10 round-16) was       |
# |         |            |             | mutation-verified against a scratch   |
# |         |            |             | edit of the real tool file (applied,  |
# |         |            |             | confirmed red, `git checkout`-ed      |
# |         |            |             | back) — see the landing report for    |
# |         |            |             | the exact mutation and failure each   |
# |         |            |             | one produced. Two mutations were      |
# |         |            |             | caught by a DIFFERENT layer than the  |
# |         |            |             | one under test (the RLIMIT_FSIZE      |
# |         |            |             | defence-in-depth kill, on the         |
# |         |            |             | abbreviation and clustered-suffix     |
# |         |            |             | mutants) — the assertions still went  |
# |         |            |             | red because they check the SPECIFIC   |
# |         |            |             | bucket/reason the mutated layer owns, |
# |         |            |             | not merely "declined by something."   |
# |         |            |             | doc-claim-check.py ITSELF was not     |
# |         |            |             | edited: no defect was found in it     |
# |         |            |             | while writing this round.             |
# | 1.2     | 2026-08-22 | Claude Code | ROUND 3: four reviewed findings       |
# |         |            |             | against this suite itself (M25-M27,   |
# |         |            |             | M30), plus locks for round 18's own   |
# |         |            |             | landings this suite had not caught up |
# |         |            |             | with. 121 -> 172 tests, ~1.9s ->      |
# |         |            |             | ~4.5s. M25: a 25-mutant battery found |
# |         |            |             | CHECK 2's own precision guards        |
# |         |            |             | 12%-killed (7 of 8 survived, 4 with a |
# |         |            |             | measured 97/3/2/1 false-dangling      |
# |         |            |             | live-tree effect) against two         |
# |         |            |             | precision tests total; six new        |
# |         |            |             | fixtures in                           |
# |         |            |             | CheckTwoGuardRegressionTests, one per |
# |         |            |             | surviving guard (enum-member harvest, |
# |         |            |             | comment-stripping before REFERENCE    |
# |         |            |             | scanning — paired with the asymmetric |
# |         |            |             | comment-DECLARES-but-never-           |
# |         |            |             | REFERENCES property in the same       |
# |         |            |             | fixture, REFERENCE's namespace-path   |
# |         |            |             | lookaround guards, the loose-         |
# |         |            |             | declaration harvest, the filename-    |
# |         |            |             | shaped-member guard, the using/       |
# |         |            |             | namespace-line blanking). M26: the    |
# |         |            |             | skip ceiling had learned only ONE     |
# |         |            |             | reason shape ("<binary> is not on     |
# |         |            |             | PATH") although this file already     |
# |         |            |             | emits a second — reproduced by        |
# |         |            |             | running this suite with git off PATH: |
# |         |            |             | SKIP CEILING EXCEEDED on a wholly     |
# |         |            |             | legitimate skip. RECONCILED (not      |
# |         |            |             | widened into a hole): a second        |
# |         |            |             | recognised shape, "<name> module      |
# |         |            |             | unavailable", explained only by a     |
# |         |            |             | FRESH `importlib.import_module`       |
# |         |            |             | probe — never a cached flag such as   |
# |         |            |             | M.RLIMITS_AVAILABLE, which a test     |
# |         |            |             | body's own mock.patch could have      |
# |         |            |             | altered by the time a skip is being   |
# |         |            |             | explained, the exact "flag that lied" |
# |         |            |             | class M18 was written against, one    |
# |         |            |             | capability over. General by           |
# |         |            |             | construction (no per-capability       |
# |         |            |             | registry, mirroring shutil.which's    |
# |         |            |             | own generality), so it explains a     |
# |         |            |             | genuinely-missing capability exactly  |
# |         |            |             | as readily as a genuinely-missing     |
# |         |            |             | binary and nothing more readily than  |
# |         |            |             | that — proven both ways by mutating   |
# |         |            |             | this file's OWN unexplained_skips()   |
# |         |            |             | back to the pre-fix, binary-only      |
# |         |            |             | logic. The compound reason on         |
# |         |            |             | ChildResourceLimitTests ("git or the  |
# |         |            |             | resource module is unavailable")      |
# |         |            |             | named two things at once, unparseable |
# |         |            |             | by either shape alone; split into two |
# |         |            |             | stacked single-reason decorators.     |
# |         |            |             | Verified against the REAL tree with   |
# |         |            |             | git genuinely removed from PATH: all  |
# |         |            |             | 172 tests still pass (4 legitimately  |
# |         |            |             | skipped), exit 0 — the actual         |
# |         |            |             | reconciliation this finding asked     |
# |         |            |             | for. M27:                             |
# |         |            |             | test_help_does_not_touch_the_sibling_ |
# |         |            |             | import was a TAUTOLOGICAL LOCK —      |
# |         |            |             | restoring the pre-fix module-scope    |
# |         |            |             | `DCC = _load_consistency()` left it   |
# |         |            |             | green, because it asserted only       |
# |         |            |             | returncode==0 and "--repo" in stdout, |
# |         |            |             | both true whether the import ran      |
# |         |            |             | deferred or at module scope — proven  |
# |         |            |             | by that exact mutation. Replaced with |
# |         |            |             | three subprocess cases against a      |
# |         |            |             | throwaway tools/ copy                 |
# |         |            |             | (_write_tool_copy, built fresh in a   |
# |         |            |             | temp dir every time): --help with the |
# |         |            |             | sibling missing entirely (still exits |
# |         |            |             | 0); --repo with the sibling missing   |
# |         |            |             | (exit 2, "could not import"); --repo  |
# |         |            |             | against a stub sibling missing        |
# |         |            |             | blank_frozen_history alone (exit 2,   |
# |         |            |             | the contract-break text naming it).   |
# |         |            |             | M30: the awk ENVIRON/PROCINFO refusal |
# |         |            |             | and the @load/@include refusal had    |
# |         |            |             | ZERO coverage — deleting the          |
# |         |            |             | ENVIRON/PROCINFO check left all 121   |
# |         |            |             | tests green while the mutated tool    |
# |         |            |             | was proven, live, to print a value    |
# |         |            |             | derived from this runner's own $PATH  |
# |         |            |             | (length 189) — a real environment     |
# |         |            |             | read oracle, not a theoretical one,   |
# |         |            |             | and the one class the OS child limits |
# |         |            |             | explicitly do not cover (they bound   |
# |         |            |             | writes). Four new hatch-regression    |
# |         |            |             | cases, none needing awk on PATH.      |
# |         |            |             | SECTION 11 locks round 18's own       |
# |         |            |             | landings, each written red against a  |
# |         |            |             | targeted mutation of the current      |
# |         |            |             | tool first (see the landing report    |
# |         |            |             | for the exact edit and failure each   |
# |         |            |             | produced): awk ARGV/ARGC (proven,     |
# |         |            |             | live, to actually read /etc/passwd    |
# |         |            |             | when the guard is removed — 28 lines  |
# |         |            |             | back instead of 3) and SYMTAB/        |
# |         |            |             | FUNCTAB; the five symlink-following   |
# |         |            |             | flag denials (grep -R, find -L,       |
# |         |            |             | find -follow, rg --follow, diff -r)   |
# |         |            |             | PLUS the structural rule (an operand  |
# |         |            |             | directory containing a symlink that   |
# |         |            |             | leaves the root is declined even for  |
# |         |            |             | plain lowercase grep -r) and its      |
# |         |            |             | execution complement, built with real |
# |         |            |             | symlinks in a throwaway temp dir      |
# |         |            |             | (never the checkout); --files0-from   |
# |         |            |             | on wc/sort/find including a NON-FIRST |
# |         |            |             | pipeline segment; the frozen-span     |
# |         |            |             | bound (_blanked_runs' merge-across-   |
# |         |            |             | whitespace rule, mutated to merge     |
# |         |            |             | unconditionally, which wrongly        |
# |         |            |             | excused a present-tense status line   |
# |         |            |             | sitting between two genuinely frozen  |
# |         |            |             | regions); would_gate's LIVE           |
# |         |            |             | classification (a currency-pierced    |
# |         |            |             | record claim gates at exit 1, not     |
# |         |            |             | "0 LIVE ... could not have caught     |
# |         |            |             | drift" at exit 2 — H23's own          |
# |         |            |             | demotion, reproduced exactly by       |
# |         |            |             | reverting would_gate to its pre-fix   |
# |         |            |             | position-only rule); the verb-gap     |
# |         |            |             | grammar (a subordinate clause between |
# |         |            |             | command and verb must not bind, the   |
# |         |            |             | plain arrow and plain verb forms      |
# |         |            |             | must); ignored-span recovery into the |
# |         |            |             | census (curl/make/dotnet with         |
# |         |            |             | redirection, reverting scan()'s       |
# |         |            |             | ignored_spans subtraction makes all   |
# |         |            |             | three vanish from every bucket at     |
# |         |            |             | once, reproducing H25 exactly); the   |
# |         |            |             | leading-value compound-token rule (a  |
# |         |            |             | slash ratio, a clock time, a bare-    |
# |         |            |             | letter version and a slash date must  |
# |         |            |             | not bind, reverting                   |
# |         |            |             | LeadingValueShape.rejects to its      |
# |         |            |             | pre-M31 three-character enumeration   |
# |         |            |             | binds all four); the LIVE distinct-   |
# |         |            |             | command floor (one command quoted     |
# |         |            |             | twice does not satisfy a floor of 2,  |
# |         |            |             | reverting the gate to count INSTANCES |
# |         |            |             | rather than distinct commands turns   |
# |         |            |             | exit 2 back into a silent PASS); and  |
# |         |            |             | the top-level exception boundary (an  |
# |         |            |             | unforeseen error exits 2, not 1,      |
# |         |            |             | proven by mocking scan() to raise and |
# |         |            |             | separately by deleting main()'s own   |
# |         |            |             | try/except, which lets the exception  |
# |         |            |             | propagate uncaught). Every complement |
# |         |            |             | test (the ones asserting a legitimate |
# |         |            |             | form still binds/executes/passes) was |
# |         |            |             | independently proven able to fail too |
# |         |            |             | — via an over-eager complementary     |
# |         |            |             | mutation — not merely proven able to  |
# |         |            |             | pass, per this file's own standing    |
# |         |            |             | rule that a test green against both   |
# |         |            |             | the fixed and the broken tool is      |
# |         |            |             | worse than none. doc-claim-check.py   |
# |         |            |             | ITSELF was not edited: every finding  |
# |         |            |             | in Part A and every round-18 property |
# |         |            |             | in Part B was reproduced as a real    |
# |         |            |             | defect in the CURRENT tool under a    |
# |         |            |             | targeted mutation, never as a defect  |
# |         |            |             | the current tool already has.         |
