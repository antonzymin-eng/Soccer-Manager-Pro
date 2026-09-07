from pathlib import Path

root = Path(__file__).resolve().parents[2]
path = root / "docs/tracking/CHANGELOG.md"
text = path.read_text()
old = "Added locks cover exact current references, no RNG cursor/action ordinal, upper/lower budget clamps, decode ordering, and short/truncated framing."
new = "Added locks cover exact current references, no RNG cursor/action ordinal, the upper budget clamp, non-positive board-modifier failure, decode ordering, and short/truncated framing."
if text.count(old) != 1:
    raise SystemExit(f"expected one stale #40 clamp sentence, found {text.count(old)}")
path.write_text(text.replace(old, new, 1))
(root / ".github/scripts/pr363_changelog_fix.py").unlink()
(root / ".github/workflows/pr363-changelog-fix.yml").unlink()
