#!/usr/bin/env python3
# ============================================================================
# File:     tools/doc-consistency-check.py
# Created:  2026-08-18
# Modified: 2026-08-18
# Author:   Claude Code
# ============================================================================
# Purpose: Catch the two defect classes that three consecutive adversarial-review
#          rounds surfaced more than any other, both of which are mechanically
#          decidable and neither of which any existing gate checks:
#
#            (1) STALE VERSION CITATIONS — document A says "`b.md` v1.2" while b.md's
#                own newest version-history row says v1.4. Round 2 found eight at once;
#                round 3 found four more that the round-2 fix had itself created.
#
#            (2) STALE CARDINALITIES — a hard-coded count ("42 design supplements",
#                "22 of 53 specs have no assembly", "34 production assemblies") that
#                drifted from the thing it counts. Round 3 found five sites of one
#                figure alone, two of them in files that same round had edited.
#                Two kinds: counts the tool can MEASURE from the repo (design
#                supplements, assemblies, open-issue bullets, Error Index rows), and
#                counts with no derivable oracle (the assembly-less-spec figure),
#                which are checked for CROSS-FILE AGREEMENT instead — every scanned
#                surface must state the same value, whatever that value is.
#
#          Both classes are invisible to recurring-defect-lint.py, which checks a
#          file's header against its OWN version table but never against what other
#          files say about it. This tool REUSES that lint's version-history parser
#          (VH_HEADING_RE / '#region VersionHistory' / fence stripping) so a file's
#          "newest version" comes only from its version-history block — never from
#          an arbitrary data table whose first cell happens to be a dotted decimal.
#
# Usage: python3 tools/doc-consistency-check.py [--repo .]
# Exit:  0 = clean, 1 = findings, 2 = usage error.
#
# WIRED: run by the `Spec hygiene checks` job in .github/workflows/ci.yml, on pushes
# to `main` and pull requests targeting `main` — that workflow's only triggers, so a
# topic-branch push runs nothing and the gate binds at the merge point. It was wired
# once green on the tree; a gate that is red on defects a PR did not introduce fails
# every PR, so keep it green. NOTE (round 6): the rebuild below un-masks REAL stale
# citations that were live while the tool read PASS — the tree is red until the
# owning documents fix them, which is the rebuild's point, not a tool defect.
#
# NOT a spell-checker for prose. It only compares a stated number against a number
# it can derive from the repo (or against the same figure on other surfaces), and it
# deliberately skips citations whose ENCLOSING SENTENCE carries an anchored
# historical marker (see HISTORICAL_MARKERS) so annotate-don't-rewrite records do
# not read as defects. The window is the sentence, not ±320 chars: the wide window
# was mutation-proved to let a NEIGHBOURING sentence's "corrected" suppress a
# genuinely stale citation.
#
# ROUND 6 REBUILD (2026-08-18) — two more inverted heuristics found passing over
# the exact class this tool exists to catch:
#   H1  The "version-history row" guard exempted EVERY line starting with '|' —
#       27% of all citations (CLAUDE.md's assembly map, file-manifest.md's
#       inventory, the roadmap's item tables), three of them live-stale — while
#       its stated rationale was already served by blank_frozen_history(). It is
#       narrowed: only rows of a table whose HEADER row's first cell starts with
#       "version" are exempt (see _version_history_table_row).
#   H2  A target whose version lives in a `**Version:**` HEADER field (not a
#       history table) resolved to None and its citations were silently dropped
#       — 22 of 107, 15 of them to spec-error-log.md, the most-cited document in
#       the corpus, two live-stale. newest_version() now falls back to RDL's own
#       VERSION_FIELD_RE (the delegation was half-applied), and citations whose
#       target STILL cannot be resolved are counted per surface and printed as
#       "N citations skipped (target version unresolvable)" instead of silently
#       continued — the same surface-contributes-nothing-must-be-visible rule
#       the coverage stats already apply, one level down.
#   Co-landed because H1/H2 un-mask them: a dated-pointer guard (see / full
#   account / per / cf. immediately BEFORE the filename is a pointer to the
#   revision documenting an event, not a currency claim — no trailing "at"
#   required); currency-escape forms for en/em-dash ranges ("v0.17–v0.18"),
#   ", now at vN", ", currently vN" and "(since advanced to vN)" (M3);
#   GAP_LIMIT 130 → 60 with ANY-newline rejection and a markup-tolerant
#   sentence boundary, so a version on the next line or after ".**" never binds
#   (M4); failure messages that name the run's LAST version, a column offset
#   and a ±40-char excerpt, because file:line does not locate a citation on
#   this repo's ~4,000-char lines (M1); and scope extended to
#   spec-error-log.md, open-issues-resolved.md and docs/tracking/*-design.md
#   (M5). Landing this rebuild surfaces REAL findings on the live tree — those
#   are defects for the owning documents' editors, not reasons to re-widen the
#   guards.
#
# ROUND 7 (2026-08-18) — DATED RECORDS vs CURRENCY CLAIMS. The v1.2 tool could
# not tell "X is at v1.5" (a currency claim, checkable) from "filed against X
# v1.5" / "the row that SATISFIED the gate was v1.1" (a dated record, correct
# forever). 35 of its 37 live-tree findings were dated records. The model now:
#
#   EVERY citation is still evaluated — nothing is blanked or skipped up front.
#   A citation that LOOKS stale is then EXCUSED — counted per surface and
#   totalled in the output, never silent — when its context marks it a dated
#   record, UNLESS it carries an explicit currency reassertion ("now vN",
#   "currently vN", "(since … vN)"), which is reported like any other stale
#   claim. Three excusal mechanisms, strongest signal first:
#
#   (R) RECORD REGIONS — structural. open-issues-resolved.md whole-file (its
#       own header: "Nothing here is live"), and spec-error-log.md from its
#       `## Error Index` heading to EOF (the Error Index rows and every
#       `## ERR-…` entry body are the log's chronicle; the head above stays
#       fully checked). Cardinalities and agreement figures in a region are
#       excused the same way. DELIBERATE COST, stated: an OPEN ERR entry CAN
#       carry a live pointer ("the current design is `x.md` v1.2") — without
#       a "now"/"currently" reassertion that goes unreported here. Accepted
#       because the log's convention is dated filing records, the excusal
#       count is printed for audit, and the same pointer almost always also
#       lives on a fully-checked surface (open-issues.md / CLAUDE.md).
#   (C) CHRONICLE TARGETS — citations OF the three append-only logs
#       (spec-error-log.md, CHANGELOG.md, CHANGELOG-src.md, per the TRACKING
#       DOCUMENTS table). Their version is a timestamp: "filed; `spec-error-
#       log.md` v1.39" dates the filing. Nobody needs a currency claim about
#       a log's version, and one that insists ("now v2.40") is still checked.
#   (P) RECORD PHRASING — the FRAGILE part, four small shapes, kept last so
#       the structural mechanisms absorb everything they can: a parenthetical
#       consisting only of file-vN citations (± ≤3 filler words) after any
#       claim ("SATISFIED (golden-vector files `x.md` v1.1, …)", "exist
#       (`a.md` v1.4, `B.cs` v1.30)"); a "(vN" revision apposition directly
#       after the filename ("`x-design.md` (v0.5, AR-1..AR-4 CONVERGED)");
#       a gap naming an artifact IN the revision ("§7 supersede note, v0.11");
#       and a record verb right after the version ("v1.6 both record its …").
#       Each is defeatable: the currency-reassertion override pierces all
#       four, and a plain "X is at vN" claim matches none of them.
#
#   Mutation-proved both ways on a scratch mirror (see the v1.3 history row):
#   a planted stale CURRENCY claim inside each excused region/shape is still
#   reported; the dated records each mechanism exists to excuse are not.
#   Live tree: 37 findings → 2, and both survivors are judged REAL (the
#   "`league-bootstrap-design.md` v1.5, now v1.6" chains in open-issues.md
#   and CHANGELOG.md's head entry — the file is at v1.7).

import argparse
import importlib.util
import pathlib
import re
import sys


def _load_lint():
    """Import tools/recurring-defect-lint.py (hyphenated name, so via importlib).

    Its collect_md_version_rows/version_rows/strip_fences are the repo's one
    authoritative version-history parser; carrying a second, weaker one here is
    how this tool originally read a data-table row as "v20.0".
    """
    p = pathlib.Path(__file__).with_name("recurring-defect-lint.py")
    spec = importlib.util.spec_from_file_location("recurring_defect_lint", p)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


RDL = _load_lint()

# A citation whose ENCLOSING SENTENCE carries one of these is a deliberate record
# of a superseded value, not a claim about the present. This repo's convention is
# to annotate rather than rewrite, so these must not be reported. Anchored forms
# only, matched case-insensitively: bare "corrected" (any casing), bare "⚠️",
# "was v" and "at the time" were each mutation-proved to suppress genuinely stale
# citations ("was v" matched "was verified"; bare "⚠️" and bare "corrected" fire
# on unrelated annotations two clauses away).
HISTORICAL_MARKERS = tuple(re.compile(p, re.I) for p in (
    r"⚠️?\s*\**\s*(?:corrected|qualified|superseded|annotated)",
    r"\*\(\s*(?:\w+\s+)?corrected",          # *(Corrected … / *(bullet corrected …
    r"corrected\s+(?:in place\s+)?(?:here\s+)?"
    r"(?:january|february|march|april|may|june|july|august"
    r"|september|october|november|december)\s+\d",
    r"corrected\s+\d{4}-\d{2}-\d{2}",
    r"superseded",
    r"as published",
    r"annotated in place",
    r"historical",
    r"\bretired\b",
    r"table stops at",
    r"ahead of the record",
    # A figure the sentence itself corrects in place ("its 'all 20 spec
    # folders' figure is the May-2026 registry — there are now 53") is a
    # deliberate record of a superseded value with the current one attached —
    # the cardinality analogue of the ", now vN" citation chain (round 6).
    r"there (?:are|is) now",
))


def sentence_window(text, start, end):
    """The sentence (never crossing a newline) enclosing text[start:end]."""
    a = text.rfind("\n", 0, start) + 1
    for sep in (". ", "! ", "? "):
        i = text.rfind(sep, a, start)
        if i != -1:
            a = max(a, i + len(sep))
    b = text.find("\n", end)
    if b == -1:
        b = len(text)
    for sep in (". ", "! ", "? "):
        j = text.find(sep, end, b)
        if j != -1:
            b = min(b, j + 1)
    return text[a:b]


# A marker suppresses a citation only if it is genuinely ADJACENT to it. This repo's
# "sentences" are not sentences: measured over the live tree, 54 of 154 candidate
# windows exceed 320 chars, median 233, p90 965, max 4,140. At that length an
# annotation about something else entirely — the orphan-counting methodology, a
# neighbouring correction — silently suppresses a real stale claim several hundred
# characters away. So the window is the sentence INTERSECTED with a tight radius.
MARKER_RADIUS = 110


def historically_marked(text, start, end):
    window = sentence_window(text, start, end)
    # Clip to a tight radius around the citation itself, expressed in the same
    # coordinates as the sentence so the two genuinely intersect.
    a = text.rfind("\n", 0, start) + 1
    sent_start = text.find(window, a) if window else start
    if sent_start == -1:
        sent_start = a
    lo = max(sent_start, start - MARKER_RADIUS)
    hi = min(sent_start + len(window), end + MARKER_RADIUS)
    clipped = text[lo:hi]
    return any(rx.search(clipped) for rx in HISTORICAL_MARKERS)


# ---------------------------------------------------------------------------
# DATED-RECORD MODEL (round 7 — see the header). Claims are always EVALUATED;
# a mismatch is EXCUSED (counted, printed, never silent) when context marks it
# a dated record, unless an explicit currency reassertion pierces the excusal.
# ---------------------------------------------------------------------------

# (R) Record regions — structural.
RECORD_ARCHIVE_FILES = {"docs/tracking/open-issues-resolved.md"}
LOG_BODY_FILES = {"docs/tracking/spec-error-log.md"}
LOG_BODY_HEADING = re.compile(r"^## (?:Error Index|ERR-)", re.M)


def record_regions(rel, text):
    """[(start, end)] spans of TEXT that are dated records by structure.

    Computed on the same (frozen-history-blanked) text the scans read, so the
    offsets agree. Fail-open: if spec-error-log.md ever loses its `## Error
    Index` / `## ERR-` headings, no region is claimed and every citation gets
    full reporting — the safe direction.
    """
    if rel in RECORD_ARCHIVE_FILES:
        return ((0, len(text)),)
    if rel in LOG_BODY_FILES:
        m = LOG_BODY_HEADING.search(text)
        if m:
            return ((m.start(), len(text)),)
    return ()


def _in_region(regions, pos):
    return any(s <= pos < e for s, e in regions)


# (C) Chronicle targets: the repo's append-only logs (TRACKING DOCUMENTS
# table). A citation of one names the revision at which something was filed.
CHRONICLE_TARGETS = {"spec-error-log.md", "CHANGELOG.md", "CHANGELOG-src.md"}

# The override that pierces every excusal: an annotation run containing one of
# these words REASSERTS currency ("v1.5, now v1.6", "(since v1.6)"), so the
# claim is checked and reported normally. A bare "vOLD → vNEW" arrow chain
# does NOT — inside a record context it is the natural notation for the bump
# an event made ("v1.0.1 → v1.1" in a changed-files table), not a claim that
# vNEW is current today.
CURRENCY_WORD = re.compile(r"\b(?:now|currently|since)\b", re.I)


def _unwrap(s):
    return re.sub(r"\s*\n>?\s*", " ", s)


# (P) Record phrasing — the fragile, deliberately small part (header, round 7).
_CITE_ITEM = r"`[^`\n]{1,80}`\s*\**v\d+(?:\.\d+)*\**"
# A parenthetical consisting only of file-vN citations (± ≤3 leading filler
# words): "(golden-vector files `x.md` v1.1, `y.md` v1.1, `z.md` v1.0)",
# "(`certification-platform.md` v1.2)", "(`a.md` v1.4, `B.cs` v1.30)".
PAREN_CITELIST_PRE = re.compile(
    r"\(\s*(?:[\w-]+\s+){0,3}(?:" + _CITE_ITEM + r"\s*[,;]\s*){0,4}$")
PAREN_CITELIST_POST = re.compile(
    r"^(?:\s*[,;]\s*" + _CITE_ITEM + r"){0,4}\s*\)")
# "`x-design.md` (v0.5, AR-1..AR-4 CONVERGED)" — a version OPENING a
# parenthetical directly after the filename is a revision apposition: it names
# the revision being described, not the file's version today.
REV_APPOSITION_GAP = re.compile(r"^\s*\(\s*$")
# "`path-to-playable-roadmap.md` §7 supersede note, v0.11" — the gap names an
# artifact IN the target (a note/row/entry), so the version dates the artifact.
ARTIFACT_NOUN_GAP = re.compile(
    r"(?:§[\w.]+[\s/,–—-]*)?(?:[\w-]+\s+){0,2}"
    r"(?:notes?|rows?|entr(?:y|ies))\s*[,:]?\s*$", re.I)
# "`match-engine-wiring-backlog.md` v1.6 both record its … predicate" — a
# record verb right after the version cites that revision as the SOURCE of a
# statement.
POST_RECORD_VERB = re.compile(
    r"^\s*(?:both\s+|which\s+|already\s+)?"
    r"(?:record(?:s|ed)?|document(?:s|ed)?|captur(?:es|ed)|"
    r"state(?:s|d)?|says?|said)\b", re.I)


# ---------------------------------------------------------------------------
# SCOPE. A stale citation is a defect only where the text is a CURRENT-STATE claim.
# Most citations in this repo are frozen history — "promoted from supplement v0.2",
# a dated CHANGELOG entry, an archived issue — and those are correct as written.
# Reporting them is how a checker manufactures 350 findings out of 12 real ones, so
# this tool looks at the named current-state surfaces below plus every file under
# .claude/**/*.md (agent config is current-state by nature, and manual grep found
# stale figures there that the named list missed).
#
# Every surface then has its FROZEN-HISTORY regions blanked (line numbering
# preserved) before scanning:
#   (a) the append-only header chain — everything from the first
#       `**Last Updated (prior):**` / `**Updated (prior):**` marker to the next
#       markdown heading (or EOF). The head entry above the marker is the
#       current claim and stays in scope; every entry below it is a dated record
#       of a past pass, and scanning those manufactures false findings. For the
#       two CHANGELOGs the chain is the whole body, so only the head entry is
#       scanned — a few hundred bytes of a multi-hundred-KB file, deliberate and
#       stated per-surface in the coverage stats so a surface that contributes
#       nothing is visible as such rather than silently counted as "checked".
#   (b) the file's own VERSION HISTORY section(s) — README.md carries one; its
#       rows and entries are dated records, not claims about now.
#
# A named surface that is MISSING from the tree is an ERROR, not a skip — with a
# skip, moving all eleven files aside yields a green run that checked nothing.
# ---------------------------------------------------------------------------
CURRENT_STATE = (
    "CLAUDE.md",
    "src/CLAUDE.md",
    "README.md",
    "docs/tracking/open-issues.md",
    "docs/tracking/open-issues-resolved.md",
    "docs/tracking/spec-error-log.md",
    "docs/tracking/CHANGELOG.md",
    "docs/tracking/CHANGELOG-src.md",
    "docs/tracking/file-manifest.md",
    "docs/tracking/path-to-playable-roadmap.md",
    "docs/tracking/match-engine-wiring-backlog.md",
    ".claude/advisors/invariants.md",
    ".claude/agents/advisor-integrity.md",
)
# Round 6 (M5): spec-error-log.md (named authoritative in root CLAUDE.md's
# TRACKING DOCUMENTS table) and open-issues-resolved.md joined the named list
# above, and every docs/tracking/*-design.md supplement is globbed like
# .claude/**/*.md — the design supplements are where real staleness was living
# while the scope omitted them. Their frozen regions (prior-entry chains, own
# VERSION HISTORY sections) are blanked exactly like every other surface's.

# Line-anchored (blockquote prefix allowed): CHANGELOG-src.md's preamble MENTIONS
# the marker mid-sentence in its update instructions, and matching that mention
# would end the "current" region 30 lines into a 2,400-line chain.
PRIOR_MARKER = re.compile(r"^\s*>?\s*\*\*(?:Last Updated|Updated) \(prior\):\*\*", re.M)
HEADING_RE = re.compile(r"^#{1,6} ", re.M)


def _blank(text, start, end):
    """Replace text[start:end] with newlines, preserving line numbering."""
    return text[:start] + "\n" * text.count("\n", start, end) + text[end:]


def blank_frozen_history(text):
    """Blank the header chain below its head entry, and own VERSION HISTORY
    sections. Returns (text, frozen_chars) — frozen_chars is the number of
    characters excluded from scanning (all counts in characters, one unit)."""
    frozen = 0
    m = PRIOR_MARKER.search(text)
    if m:
        nxt = HEADING_RE.search(text, m.start())
        end = nxt.start() if nxt else len(text)
        frozen += end - m.start()
        text = _blank(text, m.start(), end)
    # Own version-history sections (heading form; RDL's authoritative regex).
    spans = []
    for vh in RDL.VH_HEADING_RE.finditer(text):
        nxt = re.compile(r"^#{1,4}\s+\S", re.M).search(text, vh.end())
        spans.append((vh.start(), nxt.start() if nxt else len(text)))
    for start, end in reversed(spans):
        frozen += end - start
        text = _blank(text, start, end)
    return text, frozen


def current_state_sources(repo, findings):
    """[(path, rel, text, frozen_bytes)] for every in-scope surface.

    Named surfaces that do not exist are reported as findings (FN-4: a missing
    surface must never silently shrink the scope). .claude/**/*.md and
    docs/tracking/*-design.md (round 6, M5) are globbed in addition; files also
    on the named list are deduplicated. An EMPTY glob is a finding for the same
    FN-4 reason: both globs matched dozens of files when they were added.
    """
    out = []
    seen = set()

    def add(p, rel):
        text, frozen = blank_frozen_history(
            p.read_text(encoding="utf-8", errors="replace"))
        out.append((p, rel, text, frozen))
        seen.add(p.resolve())

    for rel in CURRENT_STATE:
        p = repo / rel
        if not p.is_file():
            findings.append((rel, "named CURRENT_STATE surface is MISSING — "
                                  "refusing to treat an absent surface as clean"))
            continue
        add(p, rel)
    design_files = sorted((repo / "docs" / "tracking").glob("*-design.md"))
    if not design_files:
        findings.append(("docs/tracking/*-design.md",
                         "no design supplements found — 60 existed when this "
                         "scope was added (round 6, M5); an empty glob means "
                         "the scope silently shrank"))
    claude_dir = repo / ".claude"
    claude_files = sorted(claude_dir.glob("**/*.md")) if claude_dir.is_dir() else []
    if not claude_files:
        findings.append((".claude/**/*.md",
                         "no .claude markdown files found — the agent-config scan "
                         "is part of this tool's stated scope; an empty glob means "
                         "the scope silently shrank"))
    for p in design_files + claude_files:
        if p.resolve() in seen:
            continue
        add(p, str(p.relative_to(repo)))
    return out


# RDL's VH_HEADING_RE requires the colon OUTSIDE the bold ("**Version History**"),
# but eight-plus spec files write it INSIDE ("**Version History:**") — without
# this supplement their history is invisible and their citations uncheckable.
_COLON_VH_RE = re.compile(
    r"^\*\*(?:Appendix\s+)?Version\s+History:\*\*.*$", re.M | re.I)


def _colon_vh_rows(text):
    """Version rows from '**Version History:**'-headed tables.

    The body is confined to the CONTIGUOUS table following the heading — not
    "until the next markdown heading" — so a data table further down the file
    whose first cell is a dotted decimal can never be read as history.
    """
    rows = []
    for m in _COLON_VH_RE.finditer(text):
        lines = text[m.end():].splitlines()
        table = []
        for ln in lines:
            if not ln.strip():
                if table:
                    break  # table ended
                continue  # leading blank(s) between heading and table
            if ln.strip().startswith("|"):
                table.append(ln)
            else:
                break
        if table:
            rows.extend(RDL.version_rows("\n".join(table), 0))
    return rows


def newest_version(path):
    """Newest version a file claims for itself, or None.

    Delegates to recurring-defect-lint.py's parser: fences stripped (so a
    template inside ```csharp``` is not read as history), and only rows inside
    a VH_HEADING_RE / '#region VersionHistory' block count — a data table whose
    first cell is a dotted decimal (perception-system appendix-b.md's "20.0")
    is not a version row. Falls back to the colon-form heading parser above
    when RDL's authoritative parser finds no block at all, and then — round 6,
    H2 — to RDL.VERSION_FIELD_RE, because most tracking documents carry no
    version-history table at all: their version lives in a `**Version:**`
    header field (spec-error-log.md, the most-cited target in the corpus).
    That is the same authority the table path already delegates to; the
    delegation was half-applied, which silently dropped every citation of a
    header-form target.
    """
    try:
        text = RDL.strip_fences(RDL.read(str(path)))
    except OSError:
        return None
    rows = []
    for offset, body in RDL.collect_md_version_rows(text):
        rows.extend(RDL.version_rows(body, offset))
    if not rows:
        rows = _colon_vh_rows(text)
    keys = [key for (_ln, key, _date, _raw) in rows if key]
    if keys:
        return RDL.vstr(max(keys))
    m = RDL.VERSION_FIELD_RE.search(text)
    if m:
        vm = re.match(r"v?(\d+(?:\.\d+)+)\b", m.group(1))
        if vm:
            return vm.group(1)
    return None


FILE_TOKEN = re.compile(r"`([A-Za-z0-9_./-]+\.md)`")
VER_TOKEN = re.compile(r"\**v(\d+(?:\.\d+)+)")
# Chars after the filename within which a version token binds to it. Round 6
# (M4): 130 → 60 — at 130 a version 115 chars away, belonging to a different
# subject entirely, still bound ("`src/CLAUDE.md` … a way of counting #20 v1.2"
# read as src/CLAUDE.md-at-v1.2). Any newline in the gap now also refuses the
# binding (see scan_version_citations).
GAP_LIMIT = 60


def _version_history_table_row(text, line_start):
    """True when the table row starting at line_start belongs to a table whose
    HEADER row's first cell starts with "version" — i.e. a genuine
    version-history table, whose rows are dated records of past revisions.

    Round 6 (H1): the previous guard exempted EVERY line starting with '|',
    which silently exempted every ordinary current-state table — 27% of all
    citations could never be reported, three of them live-stale. Heading-form
    history is already blanked by blank_frozen_history(); this narrowed guard
    covers only the colon-form / headerless-heading tables that blanking cannot
    see, by walking back through the contiguous run of '|' lines to the table's
    first row and requiring its first cell to start with "version".
    """
    if text[line_start:line_start + 1] != "|":
        return False
    header_start = line_start
    pos = line_start
    while pos > 0:
        prev_start = text.rfind("\n", 0, pos - 1) + 1
        if text[prev_start:prev_start + 1] != "|":
            break
        header_start = prev_start
        if prev_start == 0:
            break
        pos = prev_start
    header_end = text.find("\n", header_start)
    if header_end == -1:
        header_end = len(text)
    first_cell = text[header_start:header_end].strip().strip("|").split("|", 1)[0]
    return first_cell.strip(" *`_").lower().startswith("version")


def scan_version_citations(repo, sources, findings, stats, regions):
    """Find `<name>.md` … v<N> citations and compare against the target's own table.

    Enumerates the backticked FILENAMES first and forward-searches each one's own
    gap for the nearest version token, so a filename with no version never
    consumes its successor's citation (the original finditer-over-pairs shape
    silently skipped the citation FOLLOWING any unversioned mention).
    """
    # Bare basenames resolve only when UNIQUE repo-wide: `section-3.md` exists in
    # 53 spec folders, so a bare mention of it names nothing. Fully-qualified
    # citations (any name containing '/') resolve repo-relative and are exempt
    # from the uniqueness rule — `docs/specs/code-standards/section-3.md` is
    # unambiguous no matter how many section-3.md files exist.
    seen = {}
    for pattern in ("*.md", "docs/**/*.md", ".claude/**/*.md", "src/**/*.md",
                    "tools/**/*.md"):
        for p in repo.glob(pattern):
            seen.setdefault(p.name, set()).add(p.resolve())
    targets = {name: next(iter(paths))
               for name, paths in seen.items() if len(paths) == 1}

    for src, rel, text, _frozen in sources:
        fmatches = list(FILE_TOKEN.finditer(text))
        for i, m in enumerate(fmatches):
            gap_end = m.end() + GAP_LIMIT
            if i + 1 < len(fmatches):
                gap_end = min(gap_end, fmatches[i + 1].start())
            vm = VER_TOKEN.search(text, m.end(), gap_end)
            if not vm:
                continue  # this filename cites no version; the NEXT filename
                          # is still evaluated from its own position
            gap = text[m.end():vm.start()]
            # ANY newline between the filename and the version token refuses
            # the binding (round 6, M4): on this repo's ~4,000-char table rows
            # and wrapped prose, a version on the next line usually belongs to
            # a neighbouring claim ("`src/CLAUDE.md` … <newline> … a way of
            # counting #20 v1.2" bound spec #20's version to src/CLAUDE.md).
            if "\n" in gap:
                continue
            # A sentence boundary between the filename and the version token
            # means the token belongs to the NEXT sentence, not to this
            # filename ("`x.md`. Section files authored (v0.1)…" cites
            # nothing). The boundary class tolerates closing markup between
            # the punctuation and the whitespace ("`x.md`.** Bump …" is a
            # boundary too — round 6, M4).
            if re.search(r"[.!?][*_)\]\"']*(?:\s|$)\s*[A-Z0-9`(]", gap):
                continue
            line_start = text.rfind("\n", 0, m.start()) + 1
            # Dated-pointer phrasing immediately BEFORE the filename — "Full
            # account: `spec-error-log.md` v2.17", "per `x-design.md` §6 /
            # v1.5", "see `y.md` v1.2", "**Source plan:** `spec-plans/….md`
            # v0.1" — names the revision that DOCUMENTS an event, not the
            # version the target is at now. The verb-at guard below cannot see
            # these (the pointer word precedes the filename, not the version),
            # and no trailing "at" is required (round 6, H2 co-fix; header-form
            # targets attract exactly this phrasing). The window is unwrapped
            # (a pointer wraps: "Full\naccount: `…`", "See `CHANGELOG.md` and\n
            # > `spec-error-log.md` v2.43") and the phrase may point at a LIST
            # ("see `A.md`, `B.md` v2.42" — up to three chained filenames, each
            # optionally §-qualified). The $ anchor keeps it adjacent: only
            # filler/filenames may stand between the pointer word and the
            # citation, so a "see" about something else cannot suppress.
            pre = re.sub(r"\s*\n>?\s*", " ", text[max(0, m.start() - 110):m.start()])
            if re.search(r"\b(?:see|full\s+account(?:\s+in)?|per|cf\.|"
                         r"promoted\s+from|source\s+plan|tracked\s+in)"
                         r"[\s*:]*(?:the\s+(?:[\w-]+\s+){0,2})?"
                         r"(?:`[A-Za-z0-9_./-]+`(?:\s*§[\w.]+)?\s*(?:,|and|\+|;)\s*){0,3}$",
                         pre, re.I):
                continue
            # A version reached through "corrected there AT v1.2", "landed at v0.3",
            # "OPENED … v0.1" names the REVISION IN WHICH something happened. That is
            # a dated historical fact, not a claim about what is current, and updating
            # it would falsify the record. Distinguish it from a currency pointer.
            if re.search(r"(?:corrected|fixed|landed|recorded|resolved|opened|added|"
                         r"filed|amended|revised)\b[^`\n]{0,40}?\bat\s*$",
                         gap, re.I) or re.search(r"\b(?:OPENED|opened)\b[^`\n]{0,30}$", gap):
                continue
            # "…its VERSION HISTORY v2.1 entry" names a specific row in the
            # target's history, not the target's current version.
            if re.search(r"VERSION\s+HISTORY|version[- ]history", gap, re.I):
                continue
            name = m.group(1)
            if "/" in name:
                target = repo / name
                if not target.is_file():
                    continue  # a broken path is link-check territory, not ours
            else:
                target = targets.get(name)
                if target is None:
                    continue  # ambiguous or unknown basename: refuse the guess
            if pathlib.Path(target).resolve() == src.resolve():
                continue
            # A citation inside a VERSION-HISTORY table row is a dated record
            # of what was true at that revision. Round 6 (H1): only rows of a
            # genuine version-history table are exempt — see the helper.
            if _version_history_table_row(text, line_start):
                continue
            actual = newest_version(target)
            if actual is None:
                # Round 6 (H2): visible, not silent — the silent `continue`
                # here dropped 22 of 107 citations with no output. They are
                # counted per surface and totalled in the output instead.
                stats[rel]["unresolvable"] += 1
                continue
            stats[rel]["citations"] += 1
            cited = vm.group(1)
            # This repo annotates rather than rewrites, and its currency notes CHAIN:
            # "v1.0.1 → v1.1, since advanced to v1.3" names three versions, and what
            # makes the claim current is the LAST one. So scan the annotation run that
            # follows the citation and accept when the final version named is current.
            # An annotation naming a non-current version is itself stale and still
            # fails. Round 6 (M3): the connector alternation gains en/em-dash ranges
            # ("v0.17–v0.18"), "now at", "currently", and the paren branch tolerates
            # "(since advanced to vN)" — all live annotation forms the previous
            # regex rejected.
            tail = text[vm.end():vm.end() + 96]
            run = re.match(
                r"(?:\s*(?:\*\*)?\s*(?:,?\s*now\s+(?:at\s+)?|,?\s*currently\s+"
                r"|→\s*|->\s*|[–—]\s*"
                r"|\*?\(\s*(?:since|now)\s+(?:advanced\s+to\s+)?"
                r"|,?\s*since\s+advanced\s+to\s*)(?:\*\*)?\s*v\d+(?:\.\d+)+"
                r"(?:\*\*)?\s*\)?\*?)+", tail)
            effective, cite_end = cited, vm.end()
            if run:
                named = re.findall(r"v(\d+(?:\.\d+)+)", run.group(0))
                if named:
                    effective = named[-1]
                    cite_end = vm.end() + run.end()
            if cited == actual or effective == actual:
                continue
            # "all fixed (`spec-error-log.md` v1.81)", "withdrawn
            # (`spec-error-log.md` v1.75)", "landed atomically (`….md` v1.30:
            # ERR-…)" — an event verb followed by a PARENTHESIZED citation is
            # this repo's idiom for "documented in that revision of the log":
            # a dated record, the verb-at guard's shape with the verb before
            # the filename and "(" in place of "at". Suppressed ONLY when no
            # currency chain follows (a trailing ", now vN" reasserts currency
            # and is checked above like any other chain — so "recorded … (`x`
            # v1.5, now v1.6)" still fails when the target moved to v1.7).
            if effective == cited and re.search(
                    r"(?:corrected|fixed|landed|recorded|resolved|opened|added|"
                    r"filed|amended|revised|withdrawn|converged|captured)"
                    r"\b[^`()\n]{0,40}?\(\s*$", pre, re.I):
                continue
            # Same-prefix coarser citation (v1.2 cited, target at v1.2.1) is
            # coarse but not wrong.
            if actual.startswith(cited + ".") or actual.startswith(effective + "."):
                continue
            # ---- Dated-record excusal (round 7, see header). The claim WAS
            # evaluated and looks stale; it is excused — counted per surface,
            # totalled in the output — when context marks it a dated record.
            # An explicit currency reassertion ("now vN"/"currently vN"/
            # "(since … vN)") pierces every excusal and is reported normally.
            currency = bool(run and CURRENCY_WORD.search(run.group(0)))
            if not currency:
                if _in_region(regions.get(rel, ()), m.start()):
                    stats[rel]["excused_region"] += 1
                    continue
                if pathlib.Path(name).name in CHRONICLE_TARGETS:
                    stats[rel]["excused_chronicle"] += 1
                    continue
                post = _unwrap(text[cite_end:cite_end + 200])
                if ((PAREN_CITELIST_PRE.search(pre)
                     and PAREN_CITELIST_POST.match(post))
                        or REV_APPOSITION_GAP.match(gap)
                        or ARTIFACT_NOUN_GAP.search(gap)
                        or POST_RECORD_VERB.match(post)):
                    stats[rel]["excused_phrase"] += 1
                    continue
            if historically_marked(text, m.start(), vm.end()):
                continue
            # Round 6 (M1): on this repo's ~4,000-char lines, file:line does not
            # locate a citation — report the column too, name the run's LAST
            # version when a chained annotation matched (the defect is the final
            # claim, not the first token), and quote a ±40-char excerpt.
            line = text.count("\n", 0, m.start()) + 1
            col = m.start() - line_start + 1
            excerpt = text[max(0, m.start() - 40):min(len(text), cite_end + 40)]
            excerpt = excerpt.replace("\n", " ")
            chained = f" (chained from v{cited})" if effective != cited else ""
            findings.append(
                (f"{rel}:{line}:{col}",
                 f"cites `{name}` at v{effective}{chained}; that file's newest "
                 f"recorded version is v{actual} — “…{excerpt}…”"))


def scan_cardinalities(repo, sources, findings, stats, regions):
    """Compare hard-coded counts against the thing they count."""
    measured = {
        "design supplements": len(list((repo / "docs/tracking").glob("*-design.md"))),
        "spec folders": len([d for d in (repo / "docs/specs").iterdir()
                             if d.is_dir()]),
        "production assemblies": len([d for d in (repo / "src").iterdir()
                                      if d.is_dir() and list(d.glob("*.asmdef"))]),
    }
    oi = repo / "docs/tracking/open-issues.md"
    oir = repo / "docs/tracking/open-issues-resolved.md"
    if oi.exists():
        measured["active open issues"] = len(
            re.findall(r"^- \*\*", oi.read_text(encoding="utf-8"), re.M))
    if oir.exists():
        measured["resolved open issues"] = len(
            re.findall(r"^- \*\*", oir.read_text(encoding="utf-8"), re.M))
    sel = repo / "docs/tracking/spec-error-log.md"
    if sel.exists():
        # The Error Index: one `| ERR-…` row per filed entry. A doc claiming
        # "N `ERR-` entries" is checked against this row count.
        measured["ERR- index rows"] = len(
            re.findall(r"^\| ERR-", sel.read_text(encoding="utf-8"), re.M))

    checks = (
        (re.compile(r"(\d+)\s+design\s+supplements"), "design supplements"),
        (re.compile(r"(\d+)\s+spec\s+folders"), "spec folders"),
        (re.compile(r"(\d+)\s+production\s+assembl"), "production assemblies"),
        (re.compile(r"\*\*(\d+) active\*\*"), "active open issues"),
        (re.compile(r"\*\*(\d+) resolved\*\*"), "resolved open issues"),
        (re.compile(r"(\d+)\s+`?ERR-`?\s+entries"), "ERR- index rows"),
    )

    for src, rel, text, _frozen in sources:
        for pattern, key in checks:
            for m in pattern.finditer(text):
                stats[rel]["cardinalities"] += 1
                stated = int(m.group(1))
                if stated == measured.get(key):
                    continue
                # A mismatched count inside a record region is a dated record
                # of what the figure was ("all 20 spec folders" in an archived
                # 2026-05 entry) — excused, counted (round 7).
                if _in_region(regions.get(rel, ()), m.start()):
                    stats[rel]["excused_region"] += 1
                    continue
                if historically_marked(text, m.start(), m.end()):
                    continue
                line = text.count("\n", 0, m.start()) + 1
                findings.append(
                    (f"{rel}:{line}",
                     f"states {stated} {key}; measured {measured.get(key)}"))
    return measured


# Figures with no derivable oracle, checked for cross-file AGREEMENT: every
# scanned surface stating the figure must state the same value. The context
# regex keeps unrelated "N of 53" phrasings (none known today) out of the set.
AGREEMENT_GROUPS = (
    ("assembly-less approved specs (\"N of 53\")",
     re.compile(r"(\d+)\s+of\s+(?:the\s+)?53\b"),
     # The context must say what the figure MEANS ("…have no assembly", "…of the
     # registry"), not merely mention specs — "29 of 53 approved specs" in the
     # implementation-begun sense is a different figure and must not be pooled
     # into this agreement set.
     re.compile(r"no\s+`?src/?`?\s*assembly|no\s+assembly|assembly-less"
                r"|of\s+the\s+registry", re.I)),
    ("registry share (\"N% of the registry\")",
     re.compile(r"(\d+)%\s+of\s+the\s+registry"),
     None),
)


def scan_agreements(sources, findings, stats, regions):
    for label, pat, ctx in AGREEMENT_GROUPS:
        sites = {}
        for src, rel, text, _frozen in sources:
            for m in pat.finditer(text):
                if ctx is not None:
                    window = text[max(0, m.start() - 80):m.end() + 120]
                    if not ctx.search(window):
                        continue
                # A figure inside a record region is what the figure WAS at
                # that dated entry; pooling it manufactures cross-file
                # disagreement out of history — excused, counted (round 7).
                if _in_region(regions.get(rel, ()), m.start()):
                    stats[rel]["excused_region"] += 1
                    continue
                if historically_marked(text, m.start(), m.end()):
                    continue
                stats[rel]["agreement figures"] += 1
                line = text.count("\n", 0, m.start()) + 1
                sites.setdefault(int(m.group(1)), []).append(f"{rel}:{line}")
        if len(sites) > 1:
            detail = "; ".join(
                f"{v} at {', '.join(where)}" for v, where in sorted(sites.items()))
            findings.append(
                ("cross-file",
                 f"the {label} figure disagrees across scanned surfaces "
                 f"(no oracle exists for this figure, so agreement IS the "
                 f"check): {detail}"))


def main():
    ap = argparse.ArgumentParser(
        description="Cross-document stale-version-citation and stale-cardinality "
                    "checker (see file header).")
    ap.add_argument("--repo", default=".")
    ap.add_argument("--quiet", action="store_true",
                    help="suppress the per-surface coverage stats")
    args = ap.parse_args()
    repo = pathlib.Path(args.repo).resolve()
    if not (repo / "docs").is_dir():
        print(f"error: {repo} does not look like the repo root", file=sys.stderr)
        return 2

    findings = []
    sources = current_state_sources(repo, findings)
    stats = {rel: {"citations": 0, "cardinalities": 0, "agreement figures": 0,
                   "unresolvable": 0, "excused_region": 0,
                   "excused_chronicle": 0, "excused_phrase": 0}
             for _p, rel, _t, _f in sources}
    regions = {rel: record_regions(rel, text)
               for _p, rel, text, _f in sources}

    scan_version_citations(repo, sources, findings, stats, regions)
    measured = scan_cardinalities(repo, sources, findings, stats, regions)
    scan_agreements(sources, findings, stats, regions)

    # Self-check: a scan that evaluated NOTHING is a broken scan, not a clean
    # tree (the vacuous-pass class: with every surface moved aside or a regex
    # regressed, "no findings" must not read as assurance).
    total_cites = sum(s["citations"] for s in stats.values())
    total_cards = sum(s["cardinalities"] for s in stats.values())
    if sources and total_cites == 0:
        findings.append(("(self-check)",
                         "0 version citations evaluated across every scanned "
                         "surface — the citation scan is not reaching its inputs"))
    if sources and total_cards == 0:
        findings.append(("(self-check)",
                         "0 cardinality claims evaluated across every scanned "
                         "surface — the cardinality scan is not reaching its inputs"))

    if not args.quiet:
        print("coverage (surface: in-scope bytes / citations checked / "
              "cardinality claims / agreement figures / citations skipped):")
        for _p, rel, text, frozen in sources:
            s = stats[rel]
            note = f"  [{frozen} chars frozen history excluded]" if frozen else ""
            exc = (s["excused_region"], s["excused_chronicle"],
                   s["excused_phrase"])
            if any(exc):
                note += (f"  [excused as dated records: {exc[0]} region / "
                         f"{exc[1]} chronicle / {exc[2]} phrasing]")
            in_scope = sum(len(ln) for ln in text.splitlines() if ln.strip())
            print(f"  {rel}: {in_scope} chars in scope / {s['citations']} / "
                  f"{s['cardinalities']} / {s['agreement figures']} / "
                  f"{s['unresolvable']}{note}")
    # Round 6 (H2): a citation whose target's version cannot be resolved is
    # SKIPPED, never silently — "a surface contributing nothing is visible
    # rather than assumed", one level down. Always printed, even with --quiet.
    skipped = sum(s["unresolvable"] for s in stats.values())
    print(f"{skipped} citation(s) skipped (target version unresolvable)")
    # Round 7: every dated-record excusal is a MISMATCH the tool declined to
    # report. Counted and always printed — even with --quiet — so the next
    # reviewer can see what was not checked and audit it when the counts move.
    ex_r = sum(s["excused_region"] for s in stats.values())
    ex_c = sum(s["excused_chronicle"] for s in stats.values())
    ex_p = sum(s["excused_phrase"] for s in stats.values())
    print(f"{ex_r + ex_c + ex_p} stale-looking claim(s) excused as dated "
          f"records ({ex_r} in archive/log-body record regions, {ex_c} citing "
          f"append-only chronicles, {ex_p} by record phrasing) — excusals are "
          f"counted, never silent; audit them when these counts move")
    print("measured:", ", ".join(f"{k}={v}" for k, v in sorted(measured.items())))
    if not findings:
        print("PASS — no stale cross-document version citation or cardinality "
              "found (with the coverage stated above).")
        return 0
    print(f"\nFAIL — {len(findings)} stale cross-document claim(s):")
    for where, what in findings:
        print(f"  {where}\n      {what}")
    return 1


if __name__ == "__main__":
    sys.exit(main())

# Version History
# | Version | Date       | Author      | Notes                                        |
# | 1.0     | 2026-08-18 | Claude Code | Initial: stale `x.md` vN citations vs the    |
# |         |            |             | target's own version table + stale           |
# |         |            |             | cardinalities on 11 named surfaces.          |
# | 1.1     | 2026-08-18 | Claude Code | Hostile-review rebuild (1-for-3 precision, 4 |
# |         |            |             | proven false-negative classes): filename-    |
# |         |            |             | first citation scan (FN-1 — an unversioned   |
# |         |            |             | mention no longer consumes its successor);   |
# |         |            |             | path-qualified citations resolve repo-       |
# |         |            |             | relative (FN-2); newest_version delegates to |
# |         |            |             | recurring-defect-lint's version-history      |
# |         |            |             | parser so data tables never read as history  |
# |         |            |             | (FN-3); missing named surfaces are errors,   |
# |         |            |             | not skips, + zero-candidate self-check       |
# |         |            |             | (FN-4/vacuous pass); per-surface coverage    |
# |         |            |             | stats printed (coverage illusion); ", now    |
# |         |            |             | vX" escape survives sentence-final period    |
# |         |            |             | (FP-1); historical markers anchored, case-   |
# |         |            |             | insensitive, sentence-windowed — "was v" and |
# |         |            |             | "at the time" dropped (FP-2); assembly-less  |
# |         |            |             | figure + registry-share cross-file AGREEMENT |
# |         |            |             | checks and the Error Index row-count measure |
# |         |            |             | added; .claude/**/*.md scanned; frozen       |
# |         |            |             | history (prior-entry chains, own VERSION     |
# |         |            |             | HISTORY sections) blanked in place of the    |
# |         |            |             | HEAD_ONLY byte cut; "vOLD → vNEW" and        |
# |         |            |             | "*(since vNEW)*" join ", now vNEW" as        |
# |         |            |             | annotate-don't-rewrite escapes (accepted     |
# |         |            |             | only when the appended version IS current);  |
# |         |            |             | colon-form "**Version History:**" headings   |
# |         |            |             | parsed via a confined-table fallback (8+     |
# |         |            |             | spec files use them; RDL's regex does not    |
# |         |            |             | match the colon inside the bold). Header     |
# |         |            |             | gains Author + this block.                   |
# | 1.2     | 2026-08-18 | Claude Code | AR round-6 rebuild (2 High + 4 co-required): |
# |         |            |             | H1 — the every-'|'-line exemption narrowed   |
# |         |            |             | to rows whose table HEADER's first cell      |
# |         |            |             | starts with "version" (27% of citations had  |
# |         |            |             | been unreportable, 3 live-stale); H2 —       |
# |         |            |             | newest_version falls back to                 |
# |         |            |             | RDL.VERSION_FIELD_RE for header-form targets |
# |         |            |             | (22 silently dropped citations, 2 live-      |
# |         |            |             | stale), still-unresolvable citations counted |
# |         |            |             | + printed, dated-pointer pre-guard           |
# |         |            |             | (see/full account/per/cf., no "at"           |
# |         |            |             | required); M3 — currency escapes gain        |
# |         |            |             | en/em-dash ranges, "now at", "currently",    |
# |         |            |             | "(since advanced to vN)"; M4 — GAP_LIMIT     |
# |         |            |             | 130→60, any-newline gap rejection, markup-   |
# |         |            |             | tolerant sentence boundary; M1 — findings    |
# |         |            |             | name the run's LAST version + column +       |
# |         |            |             | ±40-char excerpt; M5 — scope gains           |
# |         |            |             | spec-error-log.md, open-issues-resolved.md,  |
# |         |            |             | docs/tracking/*-design.md (empty glob =      |
# |         |            |             | finding). Real findings on the live tree are |
# |         |            |             | expected output, owned by the documents.     |
# | 1.3     | 2026-08-18 | Claude Code | Dated-record model (round 7): 35 of the 37   |
# |         |            |             | live findings were dated records ("filed at  |
# |         |            |             | X vN"), not currency claims ("X is at vN").  |
# |         |            |             | Every claim is still EVALUATED; a mismatch   |
# |         |            |             | is EXCUSED — counted per surface + totalled  |
# |         |            |             | in output, never silent — via (R) record     |
# |         |            |             | regions (open-issues-resolved.md whole-file  |
# |         |            |             | per its "Nothing here is live" header;       |
# |         |            |             | spec-error-log.md from `## Error Index` to   |
# |         |            |             | EOF — head stays fully checked, fail-open if |
# |         |            |             | headings vanish; cardinalities + agreement   |
# |         |            |             | figures excused likewise), (C) chronicle     |
# |         |            |             | targets (citations OF spec-error-log.md /    |
# |         |            |             | CHANGELOG(-src).md date a filing), (P) four  |
# |         |            |             | record-phrasing shapes — the fragile part:   |
# |         |            |             | paren citation-list, "(vN" apposition,       |
# |         |            |             | note/row/entry gap, post-version record      |
# |         |            |             | verb. A "now vN"/"currently vN"/"(since …    |
# |         |            |             | vN)" reassertion PIERCES every excusal (a    |
# |         |            |             | bare "vOLD → vNEW" does not — it records a   |
# |         |            |             | bump). Measured: 37 findings → 2, both       |
# |         |            |             | judged REAL (the league-bootstrap "now       |
# |         |            |             | v1.6" chains vs its v1.7). Mutation-proved   |
# |         |            |             | both ways on a scratch mirror: planted stale |
# |         |            |             | currency claims inside archive, ERR body,    |
# |         |            |             | chronicle citation and all four phrase       |
# |         |            |             | shapes each still FAIL (via the currency     |
# |         |            |             | override; plain "is the authority" control   |
# |         |            |             | too, and the log head above Error Index      |
# |         |            |             | still reports); the 35 dated records + one   |
# |         |            |             | planted per mechanism are excused with       |
# |         |            |             | counters moving 25/4/8 accordingly.          |
