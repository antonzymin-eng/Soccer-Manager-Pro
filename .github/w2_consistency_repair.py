from pathlib import Path


def replace_exact(path, old, new, minimum=1):
    p = Path(path)
    text = p.read_text(encoding="utf-8")
    count = text.count(old)
    if count < minimum:
        raise SystemExit(f"{path}: expected at least {minimum} occurrence(s) of {old!r}, found {count}")
    p.write_text(text.replace(old, new), encoding="utf-8")
    print(f"{path}: {count} replacement(s): {old!r} -> {new!r}")


# W2 activation advances the authoritative backlog from v1.20 to v1.21. The
# consistency checker intentionally treats these 'now/since' citations as live
# pointers even when they appear inside a historical header chain.
replace_exact(
    "docs/tracking/open-issues.md",
    "`docs/tracking/match-engine-wiring-backlog.md` v1.8, now v1.20",
    "`docs/tracking/match-engine-wiring-backlog.md` v1.8, now v1.21",
)
replace_exact(
    "docs/tracking/open-issues.md",
    "`match-engine-wiring-backlog.md` v1.9, now v1.20",
    "`match-engine-wiring-backlog.md` v1.9, now v1.21",
)
replace_exact(
    "docs/tracking/open-issues.md",
    "current `match-engine-wiring-backlog.md` v1.20",
    "current `match-engine-wiring-backlog.md` v1.21",
)

replace_exact(
    "docs/tracking/CHANGELOG.md",
    "since advanced to **v1.20**",
    "since advanced to **v1.21**",
)
replace_exact(
    "docs/tracking/CHANGELOG.md",
    "*(since v1.20)*",
    "*(since v1.21)*",
)

replace_exact(
    "docs/tracking/file-manifest.md",
    "(**v1.20, Sep 16, 2026**)",
    "(**v1.21, Sep 16, 2026**)",
)
replace_exact(
    "docs/tracking/file-manifest.md",
    "since advanced to **v1.20**",
    "since advanced to **v1.21**",
)
