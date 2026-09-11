#!/usr/bin/env python3
from pathlib import Path
import re

path = Path("src/season-save/SeasonLoop.cs")
text = path.read_text(encoding="utf-8")


def replace_once(old: str, new: str) -> None:
    global text
    count = text.count(old)
    if count != 1:
        raise SystemExit(f"expected one literal target, found {count}: {old[:120]!r}")
    text = text.replace(old, new, 1)


def regex_once(pattern: str, replacement: str, flags: int = 0) -> None:
    global text
    text, count = re.subn(pattern, replacement, text, count=1, flags=flags)
    if count != 1:
        raise SystemExit(f"expected one regex target, found {count}: {pattern[:120]!r}")


replace_once(
    "// Created:  2026-07-26\n",
    "// Created:  2026-07-26\n"
    "// Modified: 2026-09-11 (ERR-030-051 — v1.32: #40 T2b runtime wiring. Finance bootstrap is owned by\n"
    "//           League.CreateLoop; this loop exposes the keyed ledger/read surfaces, stages every club's\n"
    "//           season settlement at step (b'), and installs it only after BeginNextSeason succeeds.)\n",
)

replace_once(
    "Club Finances & Economy #40 FR-FN-020/021/025, §7.1 T1b;",
    "Club Finances & Economy #40 FR-FN-001/002/003/004/012/013/020/021/023/025/027, §7.1 T2b;",
)
replace_once(
    "ERR-030-002 / ERR-030-009 / ERR-030-050;",
    "ERR-030-002 / ERR-030-009 / ERR-030-050 / ERR-030-051;",
)

regex_once(
    r"        // #40 T1b: persistence-only state\..*?\n        private readonly ClubFinanceEntry\[\] _finances;",
    "        // #40 T2b: live per-club finance state. League.CreateLoop bootstraps one entry per canonical\n"
    "        // squad for a new game; Restore threads the persisted entries back in. Empty remains legal only\n"
    "        // for the explicit legacy/pre-T2 generic-constructor path. Non-empty state is canonicalized and\n"
    "        // club-set checked by SeasonFinanceCoherence before this field is assigned.\n"
    "        private readonly ClubFinanceEntry[] _finances;",
    re.DOTALL,
)

regex_once(
    r"        /// <param name=\"financesOrNull\">The #40 T1b finance entries.*?</param>\n",
    "        /// <param name=\"financesOrNull\">The #40 finance entries this loop owns at runtime. Null or empty\n"
    "        /// remains legal only for legacy/pre-T2 generic composition; <see cref=\"League.CreateLoop\"/>\n"
    "        /// bootstraps new games and <see cref=\"Restore\"/> threads resumed state. A non-empty set must\n"
    "        /// exactly match the season's club set and is snapshot-copied/canonicalized at composition\n"
    "        /// (ERR-030-050 / ERR-030-051).</param>\n",
    re.DOTALL,
)

regex_once(
    r"        /// <summary>\n        /// A value-copy snapshot of the #40 finance entries this loop carries for persistence\..*?\n"
    r"        internal ClubFinanceEntry\[\] FinanceEntriesForSave\(\) => \(ClubFinanceEntry\[\]\)_finances\.Clone\(\);",
    "        /// <summary>Returns a detached #40 observer value for one club (FR-FN-003/026).</summary>\n"
    "        public FinancesViewModel FinanceView(int clubId) =>\n"
    "            SeasonFinanceRuntime.View(_finances, clubId);\n\n"
    "        /// <summary>Returns the club's currently available transfer budget through #40's ledger API.</summary>\n"
    "        public long AvailableTransferBudget(int clubId) =>\n"
    "            SeasonFinanceRuntime.AvailableTransferBudget(_finances, clubId);\n\n"
    "        /// <summary>Applies one validated #40 ledger transaction to the selected club.</summary>\n"
    "        public void ApplyTransaction(int clubId, in FinanceTransaction transaction) =>\n"
    "            SeasonFinanceRuntime.ApplyTransaction(_finances, clubId, in transaction);\n\n"
    "        /// <summary>\n"
    "        /// A value-copy snapshot of the live #40 entries for the save root. The clone prevents a save\n"
    "        /// consumer from becoming a second mutation owner.\n"
    "        /// </summary>\n"
    "        internal ClubFinanceEntry[] FinanceEntriesForSave() => (ClubFinanceEntry[])_finances.Clone();",
    re.DOTALL,
)

regex_once(
    r"        /// <b>Ordering is load-bearing\.</b>.*?"
    r"        /// <b>\(d′\) the FR-TR-025 / FR-MD-025 roster reconciliation is LIVE, and split in two\.</b>",
    "        /// <b>Ordering is load-bearing.</b> Everything is computed and validated before anything is\n"
    "        /// written, and the one write that can fail (<c>BeginNextSeason</c>, which re-applies the\n"
    "        /// constructor's club-set and calendar-coverage gates) runs BEFORE the non-throwing installs.\n"
    "        /// #40 follows the same discipline: settlement is COMPUTED at step (b') from the final table,\n"
    "        /// but its staged values are installed only after <c>BeginNextSeason</c> succeeds. A refused roll\n"
    "        /// therefore leaves season, board and finances untouched (ERR-030-051).\n"
    "        /// </para>\n"
    "        /// <para>\n"
    "        /// <b>Insertion points (FR-SN-031).</b> (a') #43's promotion/relegation transform remains a\n"
    "        /// declared empty position. (b') #40's finance settlement is LIVE: it follows (a') and precedes\n"
    "        /// (c) regeneration so budgets are derived from the completed season/post-promotion division.\n"
    "        /// (d) #28's season-boundary churn remains its separately documented reserved position.\n"
    "        /// </para>\n"
    "        /// <para>\n"
    "        /// <b>(d′) the FR-TR-025 / FR-MD-025 roster reconciliation is LIVE, and split in two.</b>",
    re.DOTALL,
)

regex_once(
    r"^            // .*\(b'\) #40 finance settlement inserts HERE .*T2-deferred\..*$",
    "            // ── (b') #40 finance settlement — LIVE (T2b / ERR-030-051). ─────────────────────────\n"
    "            // Stage the complete result here, before regeneration, but install it only after the\n"
    "            // fallible season commit. A refused roll must leave finance state untouched.\n"
    "            ClubFinanceEntry[] settledFinances =\n"
    "                SeasonFinanceRuntime.PrepareSettlement(_state, _finances);",
    re.MULTILINE,
)

replace_once(
    "            _state.SetBoard(evaluated);\n",
    "            _state.SetBoard(evaluated);\n\n"
    "            // ERR-030-051: b' used the final table before (c), but the staged values become live only\n"
    "            // after BeginNextSeason succeeds. The canonical array length/order is unchanged.\n"
    "            System.Array.Copy(settledFinances, _finances, settledFinances.Length);\n",
)

last_region = text.rfind("#endregion")
if last_region < 0:
    raise SystemExit("version-history #endregion not found")
version_row = (
    "// | 1.32    | 2026-09-11 | —      | ERR-030-051 / #40 T2b: live keyed finance read/ledger commands;   |\n"
    "// |         |            |        | settlement staged at (b') and installed only after the season    |\n"
    "// |         |            |        | commit succeeds, preserving all-or-nothing boundary semantics.    |\n"
)
if "// | 1.32    | 2026-09-11" in text:
    raise SystemExit("v1.32 already present")
text = text[:last_region] + version_row + text[last_region:]

path.write_text(text, encoding="utf-8")
