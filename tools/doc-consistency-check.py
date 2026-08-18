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
# every PR, so keep it green.
#
# NOT a spell-checker for prose. It only compares a stated number against a number
# it can derive from the repo (or against the same figure on other surfaces), and it
# deliberately skips citations whose ENCLOSING SENTENCE carries an anchored
# historical marker (see HISTORICAL_MARKERS) so annotate-don't-rewrite records do
# not read as defects. The window is the sentence, not ±320 chars: the wide window
# was mutation-proved to let a NEIGHBOURING sentence's "corrected" suppress a
# genuinely stale citation.

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
    "docs/tracking/CHANGELOG.md",
    "docs/tracking/CHANGELOG-src.md",
    "docs/tracking/file-manifest.md",
    "docs/tracking/path-to-playable-roadmap.md",
    "docs/tracking/match-engine-wiring-backlog.md",
    ".claude/advisors/invariants.md",
    ".claude/agents/advisor-integrity.md",
)

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
    surface must never silently shrink the scope). .claude/**/*.md is globbed in
    addition; the two .claude files also on the named list are deduplicated.
    """
    out = []
    seen = set()
    for rel in CURRENT_STATE:
        p = repo / rel
        if not p.is_file():
            findings.append((rel, "named CURRENT_STATE surface is MISSING — "
                                  "refusing to treat an absent surface as clean"))
            continue
        text, frozen = blank_frozen_history(
            p.read_text(encoding="utf-8", errors="replace"))
        out.append((p, rel, text, frozen))
        seen.add(p.resolve())
    claude_dir = repo / ".claude"
    claude_files = sorted(claude_dir.glob("**/*.md")) if claude_dir.is_dir() else []
    if not claude_files:
        findings.append((".claude/**/*.md",
                         "no .claude markdown files found — the agent-config scan "
                         "is part of this tool's stated scope; an empty glob means "
                         "the scope silently shrank"))
    for p in claude_files:
        if p.resolve() in seen:
            continue
        rel = str(p.relative_to(repo))
        text, frozen = blank_frozen_history(
            p.read_text(encoding="utf-8", errors="replace"))
        out.append((p, rel, text, frozen))
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
    """Newest version in a file's version-history block(s), or None.

    Delegates to recurring-defect-lint.py's parser: fences stripped (so a
    template inside ```csharp``` is not read as history), and only rows inside
    a VH_HEADING_RE / '#region VersionHistory' block count — a data table whose
    first cell is a dotted decimal (perception-system appendix-b.md's "20.0")
    is not a version row. Falls back to the colon-form heading parser above
    only when RDL's authoritative parser finds no block at all.
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
    if not keys:
        return None
    return RDL.vstr(max(keys))


FILE_TOKEN = re.compile(r"`([A-Za-z0-9_./-]+\.md)`")
VER_TOKEN = re.compile(r"\**v(\d+(?:\.\d+)+)")
GAP_LIMIT = 130  # chars after the filename within which a version token binds to it


def scan_version_citations(repo, sources, findings, stats):
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
            # A sentence boundary or blank line between the filename and the
            # version token means the token belongs to the NEXT sentence, not
            # to this filename ("`x.md`. Section files authored (v0.1)…" cites
            # nothing) — refuse the binding rather than invent a citation.
            if re.search(r"[.!?](?:\s|$)\s*[A-Z0-9`(]|\n\s*\n", gap):
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
            stats[rel]["citations"] += 1
            actual = newest_version(target)
            if actual is None:
                continue
            cited = vm.group(1)
            if cited == actual:
                continue
            # This repo annotates rather than rewrites, so a superseded citation
            # is kept and the current version appended: "v0.17, now v0.18" — a
            # transition record reads "vOLD → vNEW" — and the CHANGELOG head
            # entry writes "**v1.5** *(since v1.6)*". Any of the three satisfies
            # the check when the appended version IS the current one; an
            # annotation naming a NON-current version is itself stale and still
            # fails. (?!\d|\.\d) — NOT (?![0-9.]) — so a sentence-final period
            # after the version does not defeat the escape.
            # This repo annotates rather than rewrites, and its currency notes CHAIN:
            # "v1.0.1 → v1.1, since advanced to v1.3" names three versions, and what
            # makes the claim current is the LAST one. So scan the annotation run that
            # follows the citation and accept when the final version named is current.
            # An annotation naming a non-current version is itself stale and still fails.
            tail = text[vm.end():vm.end() + 96]
            run = re.match(
                r"(?:\s*(?:\*\*)?\s*(?:,?\s*now\s+|→\s*|->\s*|\*?\(\s*(?:since|now)\s+"
                r"|,?\s*since\s+advanced\s+to\s*)(?:\*\*)?\s*v\d+(?:\.\d+)+"
                r"(?:\*\*)?\s*\)?\*?)+", tail)
            if run:
                named = re.findall(r"v(\d+(?:\.\d+)+)", run.group(0))
                if named and named[-1] == actual:
                    continue
            # Same-prefix coarser citation (v1.2 cited, target at v1.2.1) is
            # coarse but not wrong.
            if actual.startswith(cited + "."):
                continue
            # A citation inside a version-history ROW is a dated record of what
            # was true at that revision. Those rows start with `|`.
            line_start = text.rfind("\n", 0, m.start()) + 1
            if text[line_start:line_start + 1] == "|":
                continue
            if historically_marked(text, m.start(), vm.end()):
                continue
            line = text.count("\n", 0, m.start()) + 1
            findings.append(
                (f"{rel}:{line}",
                 f"cites `{name}` at v{cited}; that file's newest "
                 f"version-history row is v{actual}"))


def scan_cardinalities(repo, sources, findings, stats):
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


def scan_agreements(sources, findings, stats):
    for label, pat, ctx in AGREEMENT_GROUPS:
        sites = {}
        for src, rel, text, _frozen in sources:
            for m in pat.finditer(text):
                if ctx is not None:
                    window = text[max(0, m.start() - 80):m.end() + 120]
                    if not ctx.search(window):
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
    stats = {rel: {"citations": 0, "cardinalities": 0, "agreement figures": 0}
             for _p, rel, _t, _f in sources}

    scan_version_citations(repo, sources, findings, stats)
    measured = scan_cardinalities(repo, sources, findings, stats)
    scan_agreements(sources, findings, stats)

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
        print("coverage (surface: in-scope bytes / citations / cardinality "
              "claims / agreement figures):")
        for _p, rel, text, frozen in sources:
            s = stats[rel]
            note = f"  [{frozen} chars frozen history excluded]" if frozen else ""
            in_scope = sum(len(ln) for ln in text.splitlines() if ln.strip())
            print(f"  {rel}: {in_scope} chars in scope / {s['citations']} / "
                  f"{s['cardinalities']} / {s['agreement figures']}{note}")
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
