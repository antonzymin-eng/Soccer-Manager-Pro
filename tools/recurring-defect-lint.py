#!/usr/bin/env python3
# ============================================================================
# File:     tools/recurring-defect-lint.py
# Created:  2026-08-08
# Modified: 2026-08-08
# Author:   Claude Code
# Purpose:  Mechanically detects the recurring defect classes that thirteen
#           consecutive adversarial-review passes over the #29/#41 balance-pass
#           landing kept re-finding by hand. Pure text analysis over the working
#           tree — never compiles, never runs the .NET gate, never writes.
# ============================================================================
#
# Usage:  python3 tools/recurring-defect-lint.py --repo .
#
# Exit codes: 0 = no ERROR findings, 1 = at least one ERROR finding.
#
# The seven classes, and where each came from:
#
#   1. cs-header      FR-CS-056/057 header + version-history hygiene in src/**.cs
#                     (5+ recurrences: passes 1, 2, 4, 5 and the pass-3 test edits)
#   2. spec-version   **Version:** / Last Updated / version-table coherence in
#                     docs/specs/**.md and docs/tracking/*-design.md
#                     (AR passes 4 M5, 10 L5, 11 L1 — headers misstating their own currency)
#   3. phantom-stream ERR-041-012 residue: the `injuries.occurrence` registered
#                     stream that never existed. Five sweep widenings, each stopped
#                     at a grep boundary.
#   4. draw-key       ERR-041-019's key spelling (#41 §3.1.1 spelling rule, AR pass 12 L3 /
#                     13 L1). Three drifted spellings have already cost a sweep.
#   5. stale-forward  Forward-design claims in docs/specs that went stale silently
#                     five separate times ("empty until", "T2 wires", ...). WARN tier —
#                     a human must check each against src/.
#   6. stale-count    Hard-coded cardinality claims in XML docs / specs ("both blocks",
#                     "the two codecs") that a later landing invalidates. WARN tier.
#   7. pending-marker Leftover PASSn-GATE-VERDICT-PENDING / VERIFICATION PENDING markers
#                     in docs/tracking. Newest pass's markers are expected (INFO);
#                     older ones are ERROR.
#
# Suppressions: tools/recurring-defect-lint.suppressions, one rule per line:
#
#       <path-glob>:<regex>            # reason
#
#   The glob is matched (fnmatch) against the repo-relative path; the regex is
#   searched against "<class> <message>". A matched finding is downgraded to INFO
#   and printed with its reason. Blank lines and lines starting with # are ignored.

import argparse
import datetime
import fnmatch
import os
import re
import sys
from collections import Counter, OrderedDict

# --------------------------------------------------------------------------
# Project pins
# --------------------------------------------------------------------------

# "Today" for the future-date check. Bump deliberately; a rolling clock would
# make the lint's own verdict depend on when it ran.
TODAY = datetime.date(2026, 8, 8)

SKIP_DIRS = {".git", "obj", "bin", "Temp", "Library", "node_modules", "__pycache__",
             "stress-reports", "packages"}

SEVERITY_ORDER = {"ERROR": 0, "WARN": 1, "INFO": 2}

MONTHS = {m.lower(): i for i, m in enumerate(
    ["January", "February", "March", "April", "May", "June", "July",
     "August", "September", "October", "November", "December"], start=1)}

ISO_DATE_RE = re.compile(r"\b(\d{4})-(\d{2})-(\d{2})\b")
TEXT_DATE_RE = re.compile(
    r"\b(January|February|March|April|May|June|July|August|September|October|"
    r"November|December)\s+(\d{1,2}),?\s+(\d{4})\b")

VERSION_CELL_RE = re.compile(r"^v?(\d+(?:\.\d+)*)$")
BULLET_ROW_RE = re.compile(r"^[-*]\s+\*\*v?(\d+(?:\.\d+)*)\s*\(([^)]*)\)")


class Finding(object):
    __slots__ = ("severity", "cls", "path", "line", "message", "reason")

    def __init__(self, severity, cls, path, line, message):
        self.severity = severity
        self.cls = cls
        self.path = path
        self.line = line
        self.message = message
        self.reason = None


# --------------------------------------------------------------------------
# Small helpers
# --------------------------------------------------------------------------

def parse_date(text):
    """First ISO or 'Month D, YYYY' date in `text`, or None."""
    m = ISO_DATE_RE.search(text)
    if m:
        try:
            return datetime.date(int(m.group(1)), int(m.group(2)), int(m.group(3)))
        except ValueError:
            return None
    m = TEXT_DATE_RE.search(text)
    if m:
        try:
            return datetime.date(int(m.group(3)), MONTHS[m.group(1).lower()], int(m.group(2)))
        except ValueError:
            return None
    return None


def all_dates(text):
    out = []
    for m in ISO_DATE_RE.finditer(text):
        try:
            out.append(datetime.date(int(m.group(1)), int(m.group(2)), int(m.group(3))))
        except ValueError:
            pass
    for m in TEXT_DATE_RE.finditer(text):
        try:
            out.append(datetime.date(int(m.group(3)), MONTHS[m.group(1).lower()], int(m.group(2))))
        except ValueError:
            pass
    return out


def version_key(text):
    """'v1.0.14' -> (1, 0, 14); None if not a version."""
    m = VERSION_CELL_RE.match(text.strip())
    if not m:
        return None
    return tuple(int(p) for p in m.group(1).split("."))


def vstr(key):
    return ".".join(str(p) for p in key)


def split_row(line):
    """Markdown/comment pipe row -> list of stripped cells, or None."""
    s = line.strip()
    if s.startswith("//"):
        s = s[2:].strip()
    if not s.startswith("|"):
        return None
    cells = [c.strip() for c in s.strip("|").split("|")]
    return cells


def walk_files(root, subdir, suffixes):
    base = os.path.join(root, subdir)
    if not os.path.isdir(base):
        return
    for dirpath, dirnames, filenames in os.walk(base):
        dirnames[:] = [d for d in dirnames if d not in SKIP_DIRS]
        for fn in sorted(filenames):
            if any(fn.endswith(s) for s in suffixes):
                full = os.path.join(dirpath, fn)
                yield full, os.path.relpath(full, root).replace(os.sep, "/")


def read(path):
    with open(path, "r", encoding="utf-8", errors="replace") as fh:
        return fh.read()


# --------------------------------------------------------------------------
# Class 1 — FR-CS-056/057 header + version-history hygiene (src/**.cs)
# --------------------------------------------------------------------------

HEADER_FIELD_RE = re.compile(r"^//\s*([A-Za-z][A-Za-z ]*?):\s*(.*)$")
REQUIRED_HEADER_FIELDS = ("File", "Created", "Modified", "Author")


def header_fields(text):
    """Collect '// Field: value' pairs from the leading comment run.

    Returns {field: [values]} preserving multiplicity, because a file may carry
    several `// Modified:` lines (OptionGenerator.cs carries six).
    """
    fields = OrderedDict()
    for raw in text.splitlines()[:80]:
        s = raw.strip()
        if not s:
            continue
        if not s.startswith("//"):
            break
        m = HEADER_FIELD_RE.match(s)
        if m:
            fields.setdefault(m.group(1).strip(), []).append(m.group(2).strip())
    return fields


def version_rows(block_text, offset_line):
    """Version rows in a version-history block.

    A row is a pipe line whose FIRST cell parses as a version. Continuation rows
    (empty first cell) and the header/separator rows are skipped by construction.
    Returns [(lineno, version_key, date_or_None, raw)].
    """
    rows = []
    for i, raw in enumerate(block_text.splitlines()):
        cells = split_row(raw)
        if cells and len(cells) >= 2:
            key = version_key(cells[0])
            if key is not None:
                rows.append((offset_line + i, key, parse_date(cells[1]), raw.strip()))
                continue
        # Bullet form: '- **v1.0.14 (July 25, 2026):** …' — #16 §3.4/§3.5 use it,
        # and its two v1.0.14 rows are a documented residual.
        m = BULLET_ROW_RE.match(raw.strip())
        if m:
            rows.append((offset_line + i, version_key(m.group(1)),
                         parse_date(m.group(2)), raw.strip()))
    return rows


def check_ordering(rows, path, cls, findings, what):
    """Strictly monotone (either direction) with no duplicate version numbers."""
    if len(rows) < 2:
        return
    seen = {}
    for lineno, key, _d, _raw in rows:
        if key in seen:
            findings.append(Finding(
                "ERROR", cls, path, lineno,
                "%s: duplicate version row v%s (first seen line %d)"
                % (what, vstr(key), seen[key])))
        else:
            seen[key] = lineno
    # Direction from first vs last differing pair, so a newest-first supplement
    # table is not reported as one violation per row.
    first, last = rows[0][1], rows[-1][1]
    ascending = True if first == last else first < last
    for (l0, k0, _, _), (l1, k1, _, _) in zip(rows, rows[1:]):
        bad = (k1 <= k0) if ascending else (k1 >= k0)
        if bad and k0 != k1:  # duplicates already reported above
            findings.append(Finding(
                "ERROR", cls, path, l1,
                "%s: row v%s is out of order after v%s (table reads %s)"
                % (what, vstr(k1), vstr(k0), "oldest-first" if ascending else "newest-first")))


def lint_cs_headers(root, findings):
    cls = "cs-header"
    for full, rel in walk_files(root, "src", (".cs",)):
        if ".gen." in os.path.basename(rel) or "/obj/" in rel or "/bin/" in rel:
            continue
        text = read(full)
        fields = header_fields(text)

        # (a) required fields
        for req in REQUIRED_HEADER_FIELDS:
            if req not in fields:
                findings.append(Finding(
                    "ERROR", cls, rel, 1,
                    "FR-CS-057: header field '// %s:' missing" % req))

        # (e) File: path matches the real path
        for value in fields.get("File", []):
            declared = value.split()[0].strip() if value.split() else ""
            if declared and declared != rel:
                findings.append(Finding(
                    "ERROR", cls, rel, 1,
                    "FR-CS-057: '// File: %s' does not match actual path %s"
                    % (declared, rel)))

        # (b) version-history region with at least one row
        m = re.search(r"#region\s+VersionHistory(.*?)#endregion", text, re.S)
        if not m:
            findings.append(Finding(
                "ERROR", cls, rel, 1,
                "FR-CS-058: no '#region VersionHistory' block"))
            continue
        offset = text[:m.start(1)].count("\n") + 1
        rows = version_rows(m.group(1), offset)
        if not rows:
            findings.append(Finding(
                "ERROR", cls, rel, offset,
                "FR-CS-058: '#region VersionHistory' block carries no version row"))
            continue

        # (d) ordering / duplicates
        check_ordering(rows, rel, cls, findings, "version history")

        # (c) newest '// Modified:' date == newest version-row date
        mod_dates = [d for d in (parse_date(v) for v in fields.get("Modified", [])) if d]
        row_dates = [d for (_l, _k, d, _r) in rows if d]
        if fields.get("Modified") and not mod_dates:
            findings.append(Finding(
                "ERROR", cls, rel, 1,
                "FR-CS-057: '// Modified:' carries no parseable YYYY-MM-DD date"))
        elif mod_dates and row_dates:
            newest_mod = max(mod_dates)
            newest_row = max(row_dates)
            if newest_mod != newest_row:
                findings.append(Finding(
                    "ERROR", cls, rel, 1,
                    "FR-CS-057: newest '// Modified: %s' != newest version-row date %s"
                    % (newest_mod.isoformat(), newest_row.isoformat())))

        # future dates
        for lineno, key, d, _raw in rows:
            if d and d > TODAY:
                findings.append(Finding(
                    "ERROR", cls, rel, lineno,
                    "version row v%s is dated %s, in the future (today %s)"
                    % (vstr(key), d.isoformat(), TODAY.isoformat())))


# --------------------------------------------------------------------------
# Class 2 — spec / supplement version-field coherence
# --------------------------------------------------------------------------

VERSION_FIELD_RE = re.compile(r"^\s*>?\s*\*\*Version:\*\*\s*(.+?)\s*$", re.M)
LAST_UPDATED_RE = re.compile(r"^\s*>?\s*\*\*Last Updated:\*\*\s*(.*)$", re.M)
STATUS_RE = re.compile(r"^\s*>?\s*\*\*Status:\*\*\s*(.*)$", re.M)
CREATED_RE = re.compile(r"^\s*>?\s*\*\*Created:\*\*\s*(.*)$", re.M)
# Three shapes exist in this repo: a numbered section heading ('## 1.5 Version
# History'), a bare heading ('## Version History', '## Appendix Version History'),
# and a bold line ('**Version History**'). All three are the same table.
VH_HEADING_RE = re.compile(
    r"^(?:#{2,6}\s*(?:[\d.]+\s+)?(?:Appendix\s+)?Version\s+History\b.*"
    r"|\*\*(?:Appendix\s+)?Version\s+History\*\*.*)$", re.M | re.I)


FENCE_RE = re.compile(r"^(```|~~~).*?^\1", re.M | re.S)


def strip_fences(text):
    """Blank out fenced code blocks, preserving line numbering.

    Required: the code-standards appendices carry the FR-CS-058 version-history
    TEMPLATE inside ```csharp fences, and a naive scan reads the template's
    placeholder rows as the file's own history.
    """
    def blank(m):
        return "\n" * m.group(0).count("\n")
    return FENCE_RE.sub(blank, text)


def collect_md_version_rows(text):
    """All version rows from every version-history block in a markdown file.

    Two shapes exist in this repo: a '#region VersionHistory' block (spec section
    files, mirroring the .cs convention) and a '## Version History' heading
    followed by a markdown table (design supplements). Returns
    [(lineno, key, date, raw)] for the FIRST block found, plus a count of blocks.
    """
    blocks = []
    for m in re.finditer(r"#region\s+VersionHistory(.*?)#endregion", text, re.S):
        blocks.append((text[:m.start(1)].count("\n") + 1, m.group(1)))
    if not blocks:
        for m in VH_HEADING_RE.finditer(text):
            tail = text[m.end():]
            nxt = re.search(r"^#{1,4}\s+\S", tail, re.M)
            body = tail[:nxt.start()] if nxt else tail
            blocks.append((text[:m.end()].count("\n") + 1, body))
    return blocks


def lint_spec_versions(root, findings):
    cls = "spec-version"
    targets = list(walk_files(root, "docs/specs", (".md",)))
    tracking = os.path.join(root, "docs", "tracking")
    if os.path.isdir(tracking):
        for fn in sorted(os.listdir(tracking)):
            if fn.endswith("-design.md"):
                full = os.path.join(tracking, fn)
                targets.append((full, os.path.relpath(full, root).replace(os.sep, "/")))

    for full, rel in targets:
        text = strip_fences(read(full))
        blocks = collect_md_version_rows(text)
        rows = []
        for offset, body in blocks:
            rows.extend(version_rows(body, offset))
        vfield = VERSION_FIELD_RE.search(text)

        if not rows:
            # Outside class 2's population (which is files carrying BOTH a
            # **Version:** field and a table), so WARN rather than ERROR: this is
            # the pre-May-2026 Stage-0 spec shape, not the coherence class the
            # thirteen AR passes kept re-finding.
            if vfield:
                findings.append(Finding(
                    "WARN", cls, rel, text[:vfield.start()].count("\n") + 1,
                    "carries '**Version:** %s' but no version-history table to check it against"
                    % vfield.group(1).strip()))
            continue

        # (c) ordering / duplicates
        check_ordering(rows, rel, cls, findings, "version history")

        max_row = max(k for (_l, k, _d, _r) in rows)

        # (a) **Version:** equals the highest row
        if vfield:
            declared = version_key(vfield.group(1).strip().split()[0]
                                   if vfield.group(1).strip() else "")
            lineno = text[:vfield.start()].count("\n") + 1
            if declared is None:
                findings.append(Finding(
                    "ERROR", cls, rel, lineno,
                    "'**Version:** %s' is not a parseable version"
                    % vfield.group(1).strip()))
            elif declared != max_row:
                findings.append(Finding(
                    "ERROR", cls, rel, lineno,
                    "'**Version:** %s' != highest version-history row v%s"
                    % (vstr(declared), vstr(max_row))))

            # (b) newest **Last Updated:** names the same version
            lu = LAST_UPDATED_RE.search(text)
            if lu:
                lu_line = text[:lu.start()].count("\n") + 1
                named = re.search(r"\(v(\d+(?:\.\d+)*)", lu.group(1))
                if named:
                    named_key = version_key(named.group(1))
                    if named_key != declared:
                        findings.append(Finding(
                            "ERROR", cls, rel, lu_line,
                            "newest '**Last Updated:**' names v%s but '**Version:**' is %s"
                            % (vstr(named_key), vfield.group(1).strip())))

        # (d) no future dates in the header fields or the table
        for regex, label in ((CREATED_RE, "**Created:**"),
                             (LAST_UPDATED_RE, "**Last Updated:**"),
                             (STATUS_RE, "**Status:**")):
            m = regex.search(text)
            if not m:
                continue
            lineno = text[:m.start()].count("\n") + 1
            for d in all_dates(m.group(1)):
                if d > TODAY:
                    findings.append(Finding(
                        "ERROR", cls, rel, lineno,
                        "%s carries the future date %s (today %s)"
                        % (label, d.isoformat(), TODAY.isoformat())))
                    break
        for lineno, key, d, _raw in rows:
            if d and d > TODAY:
                findings.append(Finding(
                    "ERROR", cls, rel, lineno,
                    "version row v%s is dated %s, in the future (today %s)"
                    % (vstr(key), d.isoformat(), TODAY.isoformat())))


# --------------------------------------------------------------------------
# Class 3 — ERR-041-012 phantom-stream residue
# --------------------------------------------------------------------------

# Files excluded WHOLE. Two reasons, kept apart deliberately:
#   (i)  history-of-record files, which must describe the defect to record it;
#   (ii) frozen-by-design supplements, named in the ERR-041-012 row of
#        docs/tracking/spec-error-log.md as deliberately left at their
#        convergence text.
PHANTOM_HISTORY_FILES = (
    "docs/tracking/spec-error-log.md",
    "docs/tracking/spec-error-log-err012-addendum.md",
    "docs/tracking/CHANGELOG.md",
    "docs/tracking/CHANGELOG-src.md",
)
PHANTOM_FROZEN_FILES = (
    "docs/tracking/injuries-medical-design.md",
    "docs/tracking/personalities-morale-dynamics-design.md",
    "docs/tracking/transfers-contracts-negotiation-design.md",
)
# The one line-scoped exclusion: #16's own patch note recording the retirement.
PHANTOM_LINE_EXCLUSIONS = (
    ("docs/specs/deterministic-sim/section-3.md", re.compile(r"v1\.0\.11")),
)

# Streams other specs genuinely own. A line naming one of these is talking about
# somebody else's stream, not #41's phantom.
OWNED_STREAMS = (
    "media.selection", "player-progression.regen", "season-loop.season-events",
    "world.text", "world.arcs", "competition.draws", "RegisterStream",
)

# Legitimate negative statements — the whole point of the de-phantoming sweep was
# to turn every positive claim into one of these, so they must not re-raise.
PHANTOM_NEGATIONS = re.compile(
    r"registers?\s+no\b[^.\n]{0,30}stream"
    r"|no\s+registered\s+stream"
    r"|not\s+a\s+registered\b"
    r"|never\s+register"
    r"|not\s+register(ed|s)?\b"
    r"|MUST\s+NOT\s+register"
    r"|may\s+not\s+be\s+added"
    r"|(exists|are)\s+or\s+may\s+be\s+added"
    r"|no\s+stream\s+registration"
    r"|no\s+stream\s+is\s+registered"
    r"|nothing\s+to\s+register"
    r"|registers\s+nothing"
    r"|registered\s+nothing"
    r"|MUST\s+register\s+\*\*no\*\*"
    r"|registers\s+\*\*no\*\*"
    r"|other\s+registered\s+stream"
    r"|rather\s+than\s+a\s+registered"
    r"|no\s+\*\*registered\*\*"
    r"|neither\s+register"
    # historical narration: the sweep's whole product is text that says the
    # stream USED to be required and is not any more.
    r"|originally\s+(required|promised|designated|named)"
    r"|this\s+KD\s+originally"
    r"|is\s+cursor-positioned"
    r"|superseded"
    r"|retired\s+the\s+registered"
    r"|ERR-041-012",
    re.I)
# The negation may sit one or two wrapped lines away from the positive-looking
# phrase; markdown prose here wraps at ~100 columns mid-sentence.
PHANTOM_NEGATION_WINDOW = 2

# Context markers that make a register/stream line an INJURIES one. Without this
# the pattern sweeps every spec that legitimately registers a stream.
PHANTOM_CONTEXT = re.compile(
    r"#41|injur|medical|FR-MD-|0x2A|InjuriesMedical|ordinal\s+92", re.I)

PHANTOM_TOKEN_RE = re.compile(r"injuries\.occurrence")
PHANTOM_REGISTER_RE = re.compile(
    r"(register|registers|registered|registering)[^.\n]{0,50}stream", re.I)


def _is_version_row(line):
    """A version-history row: a pipe line whose second cell holds a date."""
    cells = split_row(line)
    if not cells or len(cells) < 2:
        return False
    if version_key(cells[0]) is None:
        return False
    return parse_date(cells[1]) is not None


def _is_last_updated(line):
    s = line.strip().lstrip("> ").strip()
    return s.startswith("**Last Updated") or s.startswith("**Updated")


LAST_UPDATED_START_RE = re.compile(r"^\s*>?\s*\*\*(?:Last )?Updated")


def narrative_excluded_lines(text):
    """1-based line numbers that describe HISTORY rather than making a claim.

    Two ranges, both of which the thirteen passes established must never
    re-raise: (i) every line of a version-history block, continuation rows
    included — a row records what WAS wrong; (ii) the whole `**Last Updated:**`
    /`**Updated:**` note paragraph, which wraps over many lines in this repo and
    exists to describe the defect it fixed.
    """
    lines = text.splitlines()
    excluded = set()

    # (i) version-history blocks — region form and heading form alike.
    for m in re.finditer(r"#region\s+VersionHistory.*?#endregion", text, re.S):
        start = text[:m.start()].count("\n") + 1
        end = start + m.group(0).count("\n")
        excluded.update(range(start, end + 1))
    for m in VH_HEADING_RE.finditer(text):
        start = text[:m.start()].count("\n") + 1
        tail = text[m.end():]
        nxt = re.search(r"^#{1,4}\s+\S", tail, re.M)
        body = tail[:nxt.start()] if nxt else tail
        excluded.update(range(start, start + body.count("\n") + 1))

    # (ii) Last Updated / Updated note paragraphs.
    i = 0
    while i < len(lines):
        if LAST_UPDATED_START_RE.match(lines[i]):
            j = i
            while j < len(lines):
                excluded.add(j + 1)
                stripped = lines[j].strip().lstrip("> ").strip()
                # A note ends at a blank line, a rule, or the next bold field.
                if j > i and (not stripped or stripped.startswith("---")
                              or stripped.startswith("**")):
                    break
                j += 1
            i = j
        i += 1
    return excluded


def lint_phantom_stream(root, findings):
    cls = "phantom-stream"
    targets = list(walk_files(root, "docs", (".md",))) + \
        list(walk_files(root, "src", (".cs", ".md")))
    for full, rel in targets:
        if rel in PHANTOM_HISTORY_FILES or rel in PHANTOM_FROZEN_FILES:
            continue
        if "/obj/" in rel or "/bin/" in rel:
            continue
        text = read(full)
        lines = text.splitlines()
        skip_lines = narrative_excluded_lines(text)
        for i, line in enumerate(lines, start=1):
            hit_token = PHANTOM_TOKEN_RE.search(line)
            hit_reg = PHANTOM_REGISTER_RE.search(line) and PHANTOM_CONTEXT.search(line)
            if not (hit_token or hit_reg):
                continue
            if i in skip_lines or _is_version_row(line) or _is_last_updated(line):
                continue
            window = "\n".join(lines[max(0, i - 1 - PHANTOM_NEGATION_WINDOW):
                                     i + PHANTOM_NEGATION_WINDOW])
            if PHANTOM_NEGATIONS.search(window):
                continue
            if any(s in line for s in OWNED_STREAMS):
                continue
            skip = False
            for exc_path, exc_re in PHANTOM_LINE_EXCLUSIONS:
                if rel == exc_path and exc_re.search(line):
                    skip = True
            if skip:
                continue
            findings.append(Finding(
                "ERROR", cls, rel, i,
                "ERR-041-012: possible phantom-stream residue — %s"
                % line.strip()[:180]))


# --------------------------------------------------------------------------
# Class 4 — ERR-041-019 draw-key spelling (#41 §3.1.1 spelling rule)
# --------------------------------------------------------------------------

# The canonical full spelling plus the three sanctioned abbreviations. Anything
# else pairing playerId with worldDay inside one tuple is a drifted spelling.
SANCTIONED_KEY_RES = (
    re.compile(r"^\(\s*worldSeed\s*,\s*playerId\s*,\s*actionOrdinal\s*"
               r"(=\s*worldDay\s*[x×]\s*(DRAW_PURPOSE_)?RADIX\s*\+\s*purpose\s*)?\)$"),
    re.compile(r"^\(\s*worldSeed\s*,\s*playerId\s*,\s*worldDay\s*,\s*purpose\s*\)$"),
    re.compile(r"^\(\s*playerId\s*,\s*worldDay\s*,\s*purpose\s*\)$"),
)
TUPLE_RE = re.compile(r"\([^()\n]{0,140}\)")
# `playerId` as a whole token — NOT `fieldedPlayerIds`, `newPlayerId`,
# `SubjectPlayerId`, which are other systems' identifiers.
PLAYERID_TOKEN_RE = re.compile(r"\bplayer_?ids?\b", re.I)
WORLDDAY_TOKEN_RE = re.compile(r"\bworld_?day\b", re.I)
WORLDSEED_TOKEN_RE = re.compile(r"\bworld_?seed\b", re.I)
# Other specs key their own draws on a player and a day (#31 on
# `(clubId, playerId, worldDay, purpose)`, #42 on a regen id). The class is
# #41's key, so a candidate must either carry the world seed or sit on a line
# that is talking about the injury occurrence draw.
DRAWKEY_CONTEXT = re.compile(
    r"occurrence|FR-MD-|InjuriesMedical|DrawPurpose|#41|injur", re.I)
# `(playerId=501, worldDay=205, purpose=Occurrence)` is the sanctioned
# three-part abbreviation with its worked-example values bound.
NAMED_VALUE_RE = re.compile(r"\s*[:=]\s*[^,)]*")


def _normalize_tuple(tup):
    inner = tup[1:-1]
    parts = [NAMED_VALUE_RE.sub("", p).strip() for p in inner.split(",")]
    return "(" + ", ".join(p for p in parts if p) + ")"


def lint_draw_key(root, findings):
    cls = "draw-key"
    targets = list(walk_files(root, "docs/specs", (".md",))) + \
        list(walk_files(root, "src", (".cs",)))
    for full, rel in targets:
        if "/obj/" in rel or "/bin/" in rel or ".gen." in os.path.basename(rel):
            continue
        text = read(full)
        skip_lines = narrative_excluded_lines(text)
        for i, line in enumerate(text.splitlines(), start=1):
            if i in skip_lines or _is_version_row(line) or _is_last_updated(line):
                continue
            for m in TUPLE_RE.finditer(line):
                tup = m.group(0)
                # Only the #41 key shape: playerId AND worldDay in one tuple.
                if not (PLAYERID_TOKEN_RE.search(tup) and WORLDDAY_TOKEN_RE.search(tup)):
                    continue
                if not (WORLDSEED_TOKEN_RE.search(tup) or DRAWKEY_CONTEXT.search(line)):
                    continue
                # The 'keyed on PlayerId with no club term' prose form is
                # sanctioned and is not a tuple, so it never reaches here.
                if any(r.match(tup) or r.match(_normalize_tuple(tup))
                       for r in SANCTIONED_KEY_RES):
                    continue
                findings.append(Finding(
                    "ERROR", cls, rel, i,
                    "#41 §3.1.1 spelling rule: unsanctioned draw-key spelling `%s`" % tup))


# --------------------------------------------------------------------------
# Class 5 / 6 — stale forward-design and stale-count claims (WARN tier)
# --------------------------------------------------------------------------

STALE_FORWARD_PHRASES = (
    "nothing is built yet", "does not exist yet", "empty until", "when they land",
    "not yet implemented", "will be wired at", "T2 wires",
)
STALE_COUNT_PHRASES = (
    "the two current callers", "the two blocks", "both blocks",
    "the two codecs", "two sub-blobs",
)
# A line that already says the thing is live/landed has been checked; the whole
# defect class is the UN-annotated claim.
LIVE_ANNOTATION = re.compile(
    r"\bLIVE\b|\blanded\b|\bLANDED\b|\bsettled\b|\bsince T2\b|\bno longer\b"
    r"|\bwas wrong\b|\bcorrected\b|\bretired\b|\bRETIRED\b", re.I)

HISTORY_FILES = PHANTOM_HISTORY_FILES


def _stale_scan(root, findings, cls, phrases, subdirs, suffixes, note):
    targets = []
    for sub in subdirs:
        targets.extend(walk_files(root, sub, suffixes))
    for full, rel in targets:
        if rel in HISTORY_FILES or "/obj/" in rel or "/bin/" in rel:
            continue
        text = read(full)
        skip_lines = narrative_excluded_lines(text)
        for i, line in enumerate(text.splitlines(), start=1):
            low = line.lower()
            for phrase in phrases:
                if phrase.lower() not in low:
                    continue
                if i in skip_lines or _is_version_row(line) or _is_last_updated(line):
                    continue
                if LIVE_ANNOTATION.search(line):
                    continue
                findings.append(Finding(
                    "WARN", cls, rel, i,
                    "%s: \"%s\" — %s | %s" % (note, phrase, "check against src/",
                                              line.strip()[:150])))
                break


def lint_stale_forward(root, findings):
    _stale_scan(root, findings, "stale-forward", STALE_FORWARD_PHRASES,
                ("docs/specs",), (".md",),
                "forward-design claim that went stale silently five times")


def lint_stale_count(root, findings):
    _stale_scan(root, findings, "stale-count", STALE_COUNT_PHRASES,
                ("docs/specs", "src"), (".md", ".cs"),
                "hard-coded cardinality claim")


# --------------------------------------------------------------------------
# Class 7 — leftover pending markers
# --------------------------------------------------------------------------

PENDING_RE = re.compile(r"PASS(\d+)-GATE-VERDICT-PENDING|VERIFICATION PENDING")


def lint_pending_markers(root, findings):
    cls = "pending-marker"
    hits = []
    for full, rel in walk_files(root, "docs/tracking", (".md",)):
        for i, line in enumerate(read(full).splitlines(), start=1):
            for m in PENDING_RE.finditer(line):
                pass_no = int(m.group(1)) if m.group(1) else None
                hits.append((rel, i, m.group(0), pass_no))
    if not hits:
        return
    numbered = [h[3] for h in hits if h[3] is not None]
    newest = max(numbered) if numbered else None
    for rel, lineno, marker, pass_no in hits:
        if pass_no is not None and pass_no == newest:
            findings.append(Finding(
                "INFO", cls, rel, lineno,
                "%s — newest pass's marker, expected while its gate runs" % marker))
        else:
            findings.append(Finding(
                "ERROR", cls, rel, lineno,
                "%s — stale pending marker (newest pass present is %s)"
                % (marker, ("PASS%d" % newest) if newest else "unnumbered")))


# --------------------------------------------------------------------------
# Suppressions
# --------------------------------------------------------------------------

def load_suppressions(path):
    rules = []
    if not path or not os.path.isfile(path):
        return rules
    for lineno, raw in enumerate(open(path, encoding="utf-8"), start=1):
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        body, _, reason = line.partition("#")
        body = body.strip()
        reason = reason.strip() or "(no reason given)"
        if ":" not in body:
            sys.stderr.write("suppressions:%d: malformed rule (need path:pattern)\n" % lineno)
            continue
        glob, _, pattern = body.partition(":")
        try:
            rules.append((glob.strip(), re.compile(pattern.strip()), reason))
        except re.error as exc:
            sys.stderr.write("suppressions:%d: bad regex (%s)\n" % (lineno, exc))
    return rules


def apply_suppressions(findings, rules):
    n = 0
    for f in findings:
        if f.severity == "INFO":
            continue
        subject = "%s %s" % (f.cls, f.message)
        for glob, pattern, reason in rules:
            if fnmatch.fnmatch(f.path, glob) and pattern.search(subject):
                f.severity = "INFO"
                f.reason = reason
                n += 1
                break
    return n


# --------------------------------------------------------------------------
# Driver
# --------------------------------------------------------------------------

CHECKS = OrderedDict((
    ("cs-header", lint_cs_headers),
    ("spec-version", lint_spec_versions),
    ("phantom-stream", lint_phantom_stream),
    ("draw-key", lint_draw_key),
    ("stale-forward", lint_stale_forward),
    ("stale-count", lint_stale_count),
    ("pending-marker", lint_pending_markers),
))


def main(argv=None):
    ap = argparse.ArgumentParser(
        description="Lint the recurring defect classes the #29/#41 AR passes keep re-finding.")
    ap.add_argument("--repo", default=".", help="repository root (default: .)")
    ap.add_argument("--suppressions",
                    default=None,
                    help="suppressions file (default: <repo>/tools/recurring-defect-lint.suppressions)")
    ap.add_argument("--no-suppressions", action="store_true",
                    help="ignore the suppressions file entirely")
    ap.add_argument("--class", dest="only", action="append",
                    choices=list(CHECKS), help="run only this class (repeatable)")
    ap.add_argument("--hide-info", action="store_true",
                    help="print only ERROR and WARN findings")
    ap.add_argument("--path", action="append", metavar="GLOB",
                    help="report only findings whose path matches this glob "
                         "(repeatable) — e.g. --path 'src/season-save/*' to scope a "
                         "sweep to one landing's surface")
    args = ap.parse_args(argv)

    root = os.path.abspath(args.repo)
    if not os.path.isdir(os.path.join(root, "docs")):
        sys.stderr.write("error: %s does not look like the repo root (no docs/)\n" % root)
        return 2

    findings = []
    for name, fn in CHECKS.items():
        if args.only and name not in args.only:
            continue
        fn(root, findings)

    if args.path:
        findings = [f for f in findings
                    if any(fnmatch.fnmatch(f.path, g) for g in args.path)]

    supp_path = args.suppressions or os.path.join(
        root, "tools", "recurring-defect-lint.suppressions")
    n_supp = 0
    if not args.no_suppressions:
        n_supp = apply_suppressions(findings, load_suppressions(supp_path))

    findings.sort(key=lambda f: (SEVERITY_ORDER[f.severity], f.cls, f.path, f.line))

    by_class = OrderedDict((c, Counter()) for c in CHECKS)
    current = None
    for f in findings:
        by_class[f.cls][f.severity] += 1
        if args.hide_info and f.severity == "INFO":
            continue
        if f.cls != current:
            print("\n=== %s ===" % f.cls)
            current = f.cls
        suffix = ("  [suppressed: %s]" % f.reason) if f.reason else ""
        print("%-6s %-14s %s:%d  %s%s"
              % (f.severity, f.cls, f.path, f.line, f.message, suffix))

    print("\n" + "=" * 78)
    print("%-16s %8s %8s %8s" % ("CLASS", "ERROR", "WARN", "INFO"))
    print("-" * 78)
    totals = Counter()
    for cls in CHECKS:
        if args.only and cls not in args.only:
            continue
        c = by_class[cls]
        totals.update(c)
        print("%-16s %8d %8d %8d" % (cls, c["ERROR"], c["WARN"], c["INFO"]))
    print("-" * 78)
    print("%-16s %8d %8d %8d" % ("TOTAL", totals["ERROR"], totals["WARN"], totals["INFO"]))
    print("suppressed (ERROR/WARN downgraded to INFO): %d" % n_supp)
    print("=" * 78)

    return 1 if totals["ERROR"] else 0


if __name__ == "__main__":
    sys.exit(main())

# Version History
# | Version | Date       | Author      | Notes                                        |
# | 1.0     | 2026-08-08 | Claude Code | Initial: seven mechanically-detectable       |
# |         |            |             | recurring defect classes from the 13-pass    |
# |         |            |             | #29/#41 balance-pass review record.          |
