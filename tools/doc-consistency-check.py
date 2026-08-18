#!/usr/bin/env python3
# tools/doc-consistency-check.py
# Created: 2026-08-18
# Modified: 2026-08-18
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
#
#          Both classes are invisible to recurring-defect-lint.py, which checks a
#          file's header against its OWN version table but never against what other
#          files say about it.
#
# Usage: python3 tools/doc-consistency-check.py [--repo .]
# Exit:  0 = clean, 1 = findings, 2 = usage error.
#
# NOT a spell-checker for prose. It only compares a stated number against a number
# it can derive from the repo, and it deliberately skips citations that are marked
# as historical (see HISTORICAL_MARKERS) so annotate-don't-rewrite records do not
# read as defects.

import argparse
import pathlib
import re
import sys

# A citation whose surrounding text carries one of these is a deliberate record of a
# superseded value, not a claim about the present. This repo's convention is to
# annotate rather than rewrite, so these must not be reported.
HISTORICAL_MARKERS = (
    "since advanced to", "corrected", "CORRECTED", "superseded", "SUPERSEDED",
    "as published", "was v", "annotation", "Annotation", "at the time",
    "historical", "retired", "⚠️", "table stops at", "ahead of the record",
)


# ---------------------------------------------------------------------------
# SCOPE. A stale citation is a defect only where the text is a CURRENT-STATE claim.
# Most citations in this repo are frozen history — "promoted from supplement v0.2",
# a dated CHANGELOG entry, an archived issue — and those are correct as written.
# Reporting them is how a checker manufactures 350 findings out of 12 real ones, so
# this tool deliberately looks at a short, named list of current-state surfaces only.
#
# For the three append-only chain files, "current" means the head entry: everything
# above the first `**Last Updated (prior):**` / `**Updated (prior):**` marker.
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

HEAD_ONLY = {
    "docs/tracking/CHANGELOG.md",
    "docs/tracking/CHANGELOG-src.md",
    "docs/tracking/file-manifest.md",
}

PRIOR_MARKER = re.compile(r"\*\*(?:Last Updated|Updated) \(prior\):\*\*")


def current_state_sources(repo):
    """Yield (path, text, line_offset) for each in-scope surface."""
    for rel in CURRENT_STATE:
        p = repo / rel
        if not p.is_file():
            continue
        text = p.read_text(encoding="utf-8", errors="replace")
        if rel in HEAD_ONLY:
            m = PRIOR_MARKER.search(text)
            if m:
                text = text[:m.start()]
        yield p, text


VERSION_ROW = re.compile(r"^\|\s*v?(\d+(?:\.\d+)+)\s*\|", re.M)


def newest_version(path):
    """The highest version in a file's own version-history table(s), or None."""
    try:
        text = path.read_text(encoding="utf-8", errors="replace")
    except OSError:
        return None
    versions = VERSION_ROW.findall(text)
    if not versions:
        return None
    return max(versions, key=lambda v: tuple(int(x) for x in v.split(".")))


def scan_version_citations(repo, findings):
    """Find `<name>.md` ... v<N> citations and compare against the target's own table."""
    # Only basenames that are UNIQUE repo-wide can be resolved from a bare citation.
    # `section-3.md` exists in 53 spec folders, so a bare mention of it names nothing;
    # resolving such a citation to an arbitrary match is how a checker invents defects.
    seen = {}
    for p in repo.glob("docs/**/*.md"):
        seen.setdefault(p.name, []).append(p)
    targets = {name: paths[0] for name, paths in seen.items() if len(paths) == 1}

    # `foo-design.md` followed, within 120 chars, by v1.2 / **v1.2** / (v1.2,
    cite = re.compile(r"`([a-z0-9./-]+\.md)`(.{0,120}?)\**v(\d+(?:\.\d+)+)", re.S)

    for src, text in current_state_sources(repo):
        for m in cite.finditer(text):
            gap = m.group(2)
            # If another .md is named between the filename and the version number, the
            # version probably belongs to that one. Refuse the guess rather than file it.
            if ".md" in gap:
                continue
            # "…its VERSION HISTORY v2.1 entry" names a specific row in the target's
            # history, not the target's current version. Annotating those is wrong.
            if re.search(r"VERSION\s+HISTORY|version[- ]history", gap, re.I):
                continue
            name = m.group(1).split("/")[-1]
            target = targets.get(name)
            if target is None or target.resolve() == src.resolve():
                continue
            actual = newest_version(target)
            if actual is None:
                continue
            cited = m.group(3)
            if cited == actual:
                continue
            # This repo annotates rather than rewrites, so a superseded citation is
            # kept and the current version appended: "v0.17, now v0.18". That satisfies
            # the check — the reader is told which version is current.
            tail = text[m.end():m.end() + 40]
            if re.match(r"\s*,\s*now v" + re.escape(actual) + r"(?![0-9.])", tail):
                continue
            # Same major.minor prefix with the target deeper (v1.2 vs v1.2.1) is a
            # coarser but not wrong citation.
            if actual.startswith(cited + "."):
                continue
            line = text.count("\n", 0, m.start()) + 1
            # A citation inside a version-history ROW is a dated record of what was
            # true at that revision, not a claim about now. Those rows start with `|`.
            line_start = text.rfind("\n", 0, m.start()) + 1
            if text[line_start:line_start + 2].startswith("|"):
                continue
            window = text[max(0, m.start() - 320):m.end() + 320]
            if any(mark in window for mark in HISTORICAL_MARKERS):
                continue
            findings.append(
                (f"{src.relative_to(repo)}:{line}",
                 f"cites `{name}` at v{cited}; that file's newest version row is v{actual}"))


def scan_cardinalities(repo, findings):
    """Compare hard-coded counts against the thing they count."""
    measured = {
        "design supplements": len(list((repo / "docs/tracking").glob("*-design.md"))),
        "spec folders": len([d for d in (repo / "docs/specs").iterdir() if d.is_dir()]),
        "production assemblies": len([d for d in (repo / "src").iterdir()
                                      if d.is_dir() and list(d.glob("*.asmdef"))]),
    }
    oi = repo / "docs/tracking/open-issues.md"
    oir = repo / "docs/tracking/open-issues-resolved.md"
    if oi.exists():
        measured["active open issues"] = len(re.findall(r"^- \*\*", oi.read_text(), re.M))
    if oir.exists():
        measured["resolved open issues"] = len(re.findall(r"^- \*\*", oir.read_text(), re.M))

    checks = (
        (re.compile(r"(\d+)\s+design supplements"), "design supplements"),
        (re.compile(r"(\d+)\s+spec folders"), "spec folders"),
        (re.compile(r"(\d+)\s+production assembl"), "production assemblies"),
        (re.compile(r"\*\*(\d+) active\*\*"), "active open issues"),
        (re.compile(r"\*\*(\d+) resolved\*\*"), "resolved open issues"),
    )

    for src, text in current_state_sources(repo):
        for pattern, key in checks:
            for m in pattern.finditer(text):
                stated = int(m.group(1))
                if stated == measured.get(key):
                    continue
                line = text.count("\n", 0, m.start()) + 1
                window = text[max(0, m.start() - 260):m.end() + 260]
                if any(mark in window for mark in HISTORICAL_MARKERS):
                    continue
                findings.append(
                    (f"{src.relative_to(repo)}:{line}",
                     f"states {stated} {key}; measured {measured.get(key)}"))
    return measured


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--repo", default=".")
    args = ap.parse_args()
    repo = pathlib.Path(args.repo).resolve()
    if not (repo / "docs").is_dir():
        print(f"error: {repo} does not look like the repo root", file=sys.stderr)
        return 2

    findings = []
    scan_version_citations(repo, findings)
    measured = scan_cardinalities(repo, findings)

    print("measured:", ", ".join(f"{k}={v}" for k, v in sorted(measured.items())))
    if not findings:
        print(f"PASS — no stale cross-document version citation or cardinality found.")
        return 0
    print(f"\nFAIL — {len(findings)} stale cross-document claim(s):")
    for where, what in findings:
        print(f"  {where}\n      {what}")
    return 1


if __name__ == "__main__":
    sys.exit(main())
