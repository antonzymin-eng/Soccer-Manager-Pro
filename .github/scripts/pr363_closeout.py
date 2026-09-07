# Temporary branch-only helper; it deletes itself after a verified close-out.
from pathlib import Path
import re
import subprocess


def once(text, old, new, label):
    count = text.count(old)
    if count != 1:
        raise SystemExit(f"{label}: expected 1 match, found {count}")
    return text.replace(old, new, 1)


# file-manifest.md
path = Path("docs/tracking/file-manifest.md")
text = path.read_text()
if "#40 Club Finances T0 + T1a implemented" not in text:
    old = "**Last Updated:** September 6, 2026 — **AP-01 art repository/meta contract implemented and mutation-proven; no production GameArt tree or asset landed.**"
    new = """**Last Updated:** September 6, 2026 — **#40 Club Finances T0 + T1a implemented; PR #363 external-review corrections and tracking close-out.**
New production assembly `src/club-finances/` (`TacticalDirector.ClubFinances`) delivers deterministic integer-only finance state, season settlement, ledger/query seams, and the standalone self-identifying `FNCE` persistence codec. The final T0/T1a dependency boundary is `DeterministicSim` + cross-cutting `ProjectConstants`; the unused `PlayerDatabase` edge is deferred to T2 with its first `Squad.ClubId` consumer. #40 §7 now names T1a separately from T1b (#30 `SeasonSaveCodec` composition/version bump). External-review locks cover the asmdef boundary, no-RNG serialized shape, both budget clamps, decode ordering, and short/truncated framing. T1b/T2/T3 remain deferred. Branch was merged with current `main`; the Linux MatchEngine red is inherited and is not rebaselined by #40.
**Last Updated (prior):** September 6, 2026 — **AP-01 art repository/meta contract implemented and mutation-proven; no production GameArt tree or asset landed.**"""
    text = once(text, old, new, "manifest head")
old = "| 40 | `docs/specs/club-finances-economy/` | APPROVED (Jul 23, 2026) — no assembly |"
if old in text:
    text = once(text, old, "| 40 | `docs/specs/club-finances-economy/` | APPROVED (Jul 23, 2026) — implemented at `src/club-finances/` (T0 + standalone T1a; T1b/T2/T3 deferred) |", "manifest #40")
text = re.sub(
    r"(`docs/specs/code-standards/section-3\.md`[^\n]{0,180}?since advanced to \*\*)v1\.12(\*\*)",
    r"\g<1>v1.13\g<2>",
    text,
)
marker = "### Club Finances (`src/club-finances/`)"
if marker not in text:
    files = subprocess.check_output(["git", "ls-files", "src/club-finances"], text=True).splitlines()
    if not files:
        raise SystemExit("no tracked club-finances files")
    rows = [
        "\n" + marker,
        "",
        "#40 T0 + standalone T1a. T1b/T2/T3 remain deferred by #40 §7.",
        "",
        "| Path | Role |",
        "|---|---|",
    ]
    for file_name in files:
        role = "Unity metadata sidecar" if file_name.endswith(".meta") else "#40 T0/T1a source, assembly, or test surface"
        rows.append(f"| `{file_name}` | {role} |")
    block = "\n".join(rows) + "\n"
    maintenance = re.search(r"\n##\s+Maintenance", text, re.I)
    text = text[: maintenance.start()] + block + text[maintenance.start() :] if maintenance else text.rstrip() + "\n" + block
path.write_text(text)

# open-issues.md — preserve the old count as a historical measurement at 019def1.
path = Path("docs/tracking/open-issues.md")
text = path.read_text()
if "Historical September 3, 2026 measurement" not in text:
    old = "Re-derived today at `019def1`: **35 production assemblies**"
    if old not in text:
        raise SystemExit("open-issues 019def1 anchor missing")
    text = text.replace(old, "**Historical September 3, 2026 measurement:** re-derived at `019def1`: **35 production assemblies**", 1)
path.write_text(text)

# path-to-playable-roadmap.md
path = Path("docs/tracking/path-to-playable-roadmap.md")
text = path.read_text()
old = "| **D4** | **#40 minimal** — budget-from-league-finish at #30's (b') boundary point. | #40 §7 |"
new = "| **D4** ◑ | **#40 minimal — T0 + standalone T1a LANDED September 6, 2026 (PR #363).** `src/club-finances/` now owns deterministic integer-only finance state, budget-from-finish settlement, ledger/query seams, and the self-identifying `FNCE` codec. External review split the already-landed codec into T1a and kept #30 composition as **T1b**, removed the dead `PlayerDatabase` edge until its T2 `Squad.ClubId` consumer, and added boundary/RNG/clamp/corrupt-save locks. **REMAINDER:** T1b compose into #30 + outer format bump; T2 bootstrap/(b') wiring + first #27 consumer; T3 deep economics/stochastic draw. | #40 §7 |"
if old in text:
    text = once(text, old, new, "roadmap D4")
if "| v0.24 | September 6, 2026 | **D4 #40 T0 + T1a landing.**" not in text:
    separator = "|---------|------|--------|\n"
    row = "| v0.24 | September 6, 2026 | **D4 #40 T0 + T1a landing.** Records the implemented finance core and standalone codec, the T1a/T1b correction, and remaining T1b/T2/T3 work; no later D-item is pulled forward. |\n"
    text = once(text, separator, separator + row, "roadmap history")
path.write_text(text)

# CHANGELOG.md
path = Path("docs/tracking/CHANGELOG.md")
text = path.read_text()
if "#40 Club Finances T0 + T1a landed on PR #363" not in text:
    old = "> **Last Updated:** September 6, 2026 — **AP-01 art repository/meta contract implemented and mutation-proven; no production art landed.**"
    new = """> **Last Updated:** September 6, 2026 — **#40 Club Finances T0 + T1a landed on PR #363; external-review correction cycle closed.**
> New `TacticalDirector.ClubFinances` provides deterministic integer-only finance state, `SettleFinances` / prize calculation, cash-vs-wage-liability transaction handling, a read-only finance view, and the standalone magic-led `FNCE` save codec. The approved worked example remains 1,715,790 prize / 786,316 transfer budget / 307,368 wage budget.
> Claude's external review found that the branch had already crossed from T0 into persistence while the spec/PR still denied T1 work. #40 §7 now defines **T1a standalone codec** and **T1b #30 composition + outer format bump**. The unused `PlayerDatabase` asmdef edge is removed until T2's first `Squad.ClubId` consumer. Added locks cover exact current references, no RNG cursor/action ordinal, upper/lower budget clamps, decode ordering, and short/truncated framing. The shared `GameplayConfig.GetInt` Int32 tuning ceiling for long finance fields is documented rather than hidden.
> The branch was reconciled with current `main`. Tracking/spec-hygiene surfaces are synchronized here. The Linux `MatchEngine.Tests` red is inherited from `main`; #40 does not rebaseline it. **Deferred:** T1b, T2 season/bootstrap wiring, T3 deep/stochastic economics.
>
> **Last Updated (prior):** September 6, 2026 — **AP-01 art repository/meta contract implemented and mutation-proven; no production art landed.**"""
    text = once(text, old, new, "CHANGELOG head")
path.write_text(text)

# CHANGELOG-src.md — source landing ledger; compact src/CLAUDE remains rules-only.
path = Path("docs/tracking/CHANGELOG-src.md")
text = path.read_text()
if "v2.128 — **#40 Club Finances T0 + T1a" not in text:
    text = once(text, "> **Last Updated:** September 4, 2026 (v2.127 —", "> **Last Updated (prior):** September 4, 2026 (v2.127 —", "CHANGELOG-src prior")
    anchor = "## Header chain\n\n"
    entry = "> **Last Updated:** September 6, 2026 (v2.128 — **#40 Club Finances T0 + T1a implemented and externally reviewed.** New `src/club-finances/` production/test assemblies provide deterministic integer-only finance state, settlement, ledger/query seams, and the standalone `FNCE` codec. Review corrections formalize T1a/T1b, remove the unused PlayerDatabase dependency until T2, and add boundary/RNG/clamp/corrupt-save locks. T1b/T2/T3 remain deferred; the inherited MatchEngine red is not rebaselined. Full account: `docs/tracking/CHANGELOG.md` and #40 §7.)\n>\n"
    text = once(text, anchor, anchor + entry, "CHANGELOG-src header")
    old_row = "| v2.127 |"
    new_row = "| v2.128 | September 6, 2026 | #40 Club Finances T0 + T1a: new production/test assembly, standalone FNCE codec, external-review dependency/phase/test corrections; T1b/T2/T3 deferred. |\n"
    text = once(text, old_row, new_row + old_row, "CHANGELOG-src row")
path.write_text(text)

# Correct false human co-authorship in new source headers, with append-only history rows.
for path in sorted(Path("src/club-finances").rglob("*.cs")):
    text = path.read_text()
    header = text.split("namespace", 1)[0]
    if "Codex / Anton" not in header:
        continue
    text = re.sub(r"(?m)^// Modified:\s+\d{4}-\d{2}-\d{2}\s*$", "// Modified: 2026-09-06", text, count=1)
    text = re.sub(r"(?m)^// Author:\s+Codex / Anton\s*$", "// Author:   —", text, count=1)
    history = text.rfind("#region VersionHistory")
    end = text.find("#endregion", history)
    if history < 0 or end < 0:
        raise SystemExit(f"{path}: missing VersionHistory")
    versions = re.findall(r"(?m)^//\s*(\d+)\.(\d+)\s+\|", text[history:end])
    if not versions:
        raise SystemExit(f"{path}: no parseable version row")
    major, minor = map(int, versions[-1])
    row = f"// {major}.{minor + 1}     | 2026-09-06 | —             | Header author attribution corrected to automated-agent placeholder.\n"
    text = text[:end] + row + text[end:]
    path.write_text(text)

# Temporary helper files do not belong in the landing.
for file_name in [
    ".github/workflows/pr363-closeout.yml",
    ".github/workflows/pr363-closeout-v2.yml",
    ".github/scripts/pr363_closeout.py",
]:
    temporary = Path(file_name)
    if temporary.exists():
        temporary.unlink()
