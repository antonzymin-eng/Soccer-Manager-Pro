#!/usr/bin/env python3
from pathlib import Path
import re

path = Path("src/season-save/SeasonLoop.cs")
text = path.read_text(encoding="utf-8")

# Idempotent, short-anchor patcher. Core code anchors fail loud; prose upgrades are best-effort and
# separately reviewable in the resulting diff.
if "ERR-030-051 — v1.32" not in text:
    marker = "// Created:  2026-07-26\n"
    if marker not in text:
        raise SystemExit("created-line anchor missing")
    text = text.replace(
        marker,
        marker
        + "// Modified: 2026-09-11 (ERR-030-051 — v1.32: #40 T2b runtime wiring. Finance bootstrap is owned by\n"
        + "//           League.CreateLoop; this loop exposes keyed ledger/read surfaces, stages every club's\n"
        + "//           season settlement at step (b'), and installs it only after BeginNextSeason succeeds.)\n",
        1,
    )

text = text.replace(
    "Club Finances & Economy #40 FR-FN-020/021/025, §7.1 T1b;",
    "Club Finances & Economy #40 FR-FN-001/002/003/004/012/013/020/021/023/025/027, §7.1 T2b;",
    1,
)
text = text.replace(
    "ERR-030-002 / ERR-030-009 / ERR-030-050;",
    "ERR-030-002 / ERR-030-009 / ERR-030-050 / ERR-030-051;",
    1,
)

if "// #40 T2b: live per-club finance state." not in text:
    text, count = re.subn(
        r"        // #40 T1b: persistence-only state\..*?\n        private readonly ClubFinanceEntry\[\] _finances;",
        "        // #40 T2b: live per-club finance state. League.CreateLoop bootstraps one entry per canonical\n"
        "        // squad for a new game; Restore threads the persisted entries back in. Empty remains legal only\n"
        "        // for the explicit legacy/pre-T2 generic-constructor path. Non-empty state is canonicalized and\n"
        "        // club-set checked by SeasonFinanceCoherence before this field is assigned.\n"
        "        private readonly ClubFinanceEntry[] _finances;",
        text,
        count=1,
        flags=re.DOTALL,
    )
    if count != 1:
        raise SystemExit("finance-field anchor missing")

if "League.CreateLoop" not in text[text.find("<param name=\"financesOrNull\""):text.find("<param name=\"financesOrNull\"") + 1000]:
    text, _ = re.subn(
        r"        /// <param name=\"financesOrNull\">The #40 T1b finance entries.*?</param>\n",
        "        /// <param name=\"financesOrNull\">The #40 finance entries this loop owns at runtime. Null or empty\n"
        "        /// remains legal only for legacy/pre-T2 generic composition; <see cref=\"League.CreateLoop\"/>\n"
        "        /// bootstraps new games and <see cref=\"Restore\"/> threads resumed state. A non-empty set must\n"
        "        /// exactly match the season's club set and is snapshot-copied/canonicalized at composition\n"
        "        /// (ERR-030-050 / ERR-030-051).</param>\n",
        text,
        count=1,
        flags=re.DOTALL,
    )

if "public FinancesViewModel FinanceView(int clubId)" not in text:
    save_marker = "        internal ClubFinanceEntry[] FinanceEntriesForSave() => (ClubFinanceEntry[])_finances.Clone();\n"
    if save_marker not in text:
        raise SystemExit("FinanceEntriesForSave anchor missing")
    api = (
        "\n        /// <summary>Returns a detached #40 observer value for one club (FR-FN-003/026).</summary>\n"
        "        public FinancesViewModel FinanceView(int clubId) =>\n"
        "            SeasonFinanceRuntime.View(_finances, clubId);\n\n"
        "        /// <summary>Returns the club's available transfer budget through #40's canonical ledger API.</summary>\n"
        "        public long AvailableTransferBudget(int clubId) =>\n"
        "            SeasonFinanceRuntime.AvailableTransferBudget(_finances, clubId);\n\n"
        "        /// <summary>Applies one validated #40 ledger transaction to the selected club.</summary>\n"
        "        public void ApplyTransaction(int clubId, in FinanceTransaction transaction) =>\n"
        "            SeasonFinanceRuntime.ApplyTransaction(_finances, clubId, in transaction);\n"
    )
    text = text.replace(save_marker, save_marker + api, 1)

# Update the boundary contract prose without making code application depend on the exact old wrapping.
if "settlement is COMPUTED at step (b')" not in text:
    start = text.find("        /// <b>Ordering is load-bearing.</b>")
    end_marker = "        /// <b>(d′) the FR-TR-025 / FR-MD-025 roster reconciliation is LIVE, and split in two.</b>"
    end = text.find(end_marker, start)
    if start >= 0 and end >= 0:
        text = (
            text[:start]
            + "        /// <b>Ordering is load-bearing.</b> Everything is computed and validated before anything is\n"
            + "        /// written, and the one write that can fail (<c>BeginNextSeason</c>) runs before the\n"
            + "        /// non-throwing installs. #40 follows the same discipline: settlement is COMPUTED at step (b')\n"
            + "        /// from the final table, but its staged values are installed only after that commit succeeds.\n"
            + "        /// A refused roll therefore leaves season, board and finances untouched (ERR-030-051).\n"
            + "        /// </para>\n"
            + "        /// <para>\n"
            + "        /// <b>Insertion points (FR-SN-031).</b> (a') #43 remains reserved. (b') #40 is LIVE and\n"
            + "        /// precedes (c) regeneration; (d) #28 remains its separately documented reserved position.\n"
            + "        /// </para>\n"
            + "        /// <para>\n"
            + end_marker
            + text[end + len(end_marker):]
        )

if "SeasonFinanceRuntime.PrepareSettlement(_state, _finances)" not in text:
    lines = text.splitlines(keepends=True)
    target = next((i for i, line in enumerate(lines) if "#40 finance settlement inserts HERE" in line), None)
    if target is None:
        raise SystemExit("b-prime code anchor missing")
    lines[target:target + 1] = [
        "            // ── (b') #40 finance settlement — LIVE (T2b / ERR-030-051). ─────────────────────────\n",
        "            // Stage the complete result here, before regeneration, but install it only after the\n",
        "            // fallible season commit. A refused roll must leave finance state untouched.\n",
        "            ClubFinanceEntry[] settledFinances =\n",
        "                SeasonFinanceRuntime.PrepareSettlement(_state, _finances);\n",
    ]
    text = "".join(lines)

if "System.Array.Copy(settledFinances, _finances, settledFinances.Length);" not in text:
    commit_marker = "            _state.SetBoard(evaluated);\n"
    if text.count(commit_marker) != 1:
        raise SystemExit("SetBoard commit anchor missing/ambiguous")
    text = text.replace(
        commit_marker,
        commit_marker
        + "\n            // ERR-030-051: b' used the final table before (c), but the staged values become live only\n"
        + "            // after BeginNextSeason succeeds. The canonical array length/order is unchanged.\n"
        + "            System.Array.Copy(settledFinances, _finances, settledFinances.Length);\n",
        1,
    )

if "// | 1.32    | 2026-09-11" not in text:
    last_region = text.rfind("#endregion")
    if last_region < 0:
        raise SystemExit("version-history region missing")
    text = (
        text[:last_region]
        + "// | 1.32    | 2026-09-11 | —      | ERR-030-051 / #40 T2b: live keyed finance read/ledger commands;   |\n"
        + "// |         |            |        | settlement staged at (b') and installed only after the season    |\n"
        + "// |         |            |        | commit succeeds, preserving all-or-nothing boundary semantics.    |\n"
        + text[last_region:]
    )

path.write_text(text, encoding="utf-8")
