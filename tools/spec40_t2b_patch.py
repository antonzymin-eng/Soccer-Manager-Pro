#!/usr/bin/env python3
from pathlib import Path

path = Path("src/season-save/SeasonLoop.cs")
text = path.read_text(encoding="utf-8")

replacements = [
    (
        "// Created:  2026-07-26\n// Modified: 2026-09-10 (ERR-030-050 — v1.31: the #40 T1b review restores the persisted-family\n",
        "// Created:  2026-07-26\n// Modified: 2026-09-11 (ERR-030-051 — v1.32: #40 T2b runtime wiring. Finance bootstrap is owned by\n"
        "//           League.CreateLoop; this loop exposes the keyed ledger/read surfaces, stages every club's\n"
        "//           season settlement at step (b'), and installs it only after BeginNextSeason succeeds.)\n"
        "// Modified: 2026-09-10 (ERR-030-050 — v1.31: the #40 T1b review restores the persisted-family\n",
    ),
    (
        "//           FR-MD-003/022/023/025; Club Finances & Economy #40 FR-FN-020/021/025, §7.1 T1b;\n"
        "//           ERR-030-002 / ERR-030-009 / ERR-030-050;\n",
        "//           FR-MD-003/022/023/025; Club Finances & Economy #40\n"
        "//           FR-FN-001/002/003/004/012/013/020/021/023/025/027, §7.1 T2b;\n"
        "//           ERR-030-002 / ERR-030-009 / ERR-030-050 / ERR-030-051;\n",
    ),
    (
        "        // #40 T1b: persistence-only state. Empty is the explicit pre-T2 composition. Once non-empty,\n"
        "        // SeasonFinanceCoherence requires exactly one entry for every SeasonState.ClubId. The loop does\n"
        "        // not mutate this state until T2 wires CreateInitial and SettleFinances; T1b only preserves it.\n"
        "        private readonly ClubFinanceEntry[] _finances;\n",
        "        // #40 T2b: live per-club finance state. League.CreateLoop bootstraps one entry per canonical\n"
        "        // squad for a new game; Restore threads the persisted entries back in. Empty remains legal only\n"
        "        // for the explicit legacy/pre-T2 generic-constructor path. Non-empty state is canonicalized and\n"
        "        // club-set checked by SeasonFinanceCoherence before this field is assigned.\n"
        "        private readonly ClubFinanceEntry[] _finances;\n",
    ),
    (
        "        /// <param name=\"financesOrNull\">The #40 T1b finance entries to carry through this loop. Null or\n"
        "        /// empty is the explicit pre-T2 state. A non-empty set must exactly match the season's club set\n"
        "        /// and is snapshot-copied/canonicalized at composition (ERR-030-050).</param>\n",
        "        /// <param name=\"financesOrNull\">The #40 finance entries this loop owns at runtime. Null or empty\n"
        "        /// remains legal only for legacy/pre-T2 generic composition; <see cref=\"League.CreateLoop\"/>\n"
        "        /// bootstraps new games and <see cref=\"Restore\"/> threads resumed state. A non-empty set must\n"
        "        /// exactly match the season's club set and is snapshot-copied/canonicalized at composition\n"
        "        /// (ERR-030-050 / ERR-030-051).</param>\n",
    ),
    (
        "        /// <summary>\n"
        "        /// A value-copy snapshot of the #40 finance entries this loop carries for persistence. Internal:\n"
        "        /// the T1b loop is a carrier, not a second mutation owner; the save root consumes this copy.\n"
        "        /// </summary>\n"
        "        internal ClubFinanceEntry[] FinanceEntriesForSave() => (ClubFinanceEntry[])_finances.Clone();\n",
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
        "        internal ClubFinanceEntry[] FinanceEntriesForSave() => (ClubFinanceEntry[])_finances.Clone();\n",
    ),
    (
        "        /// <b>Ordering is load-bearing.</b> Everything is computed and validated before anything is\n"
        "        /// written, and the one write that can fail (<c>BeginNextSeason</c>, which re-applies the\n"
        "        /// constructor's club-set and calendar-coverage gates) runs BEFORE the one that cannot\n"
        "        /// (<c>SetBoard</c>, whose value is pre-clamped into range). A refused roll therefore leaves the\n"
        "        /// season completely untouched, rather than half-rolled with a new board verdict against an old\n"
        "        /// schedule — the `ConfigureSquads` validate-both-before-write discipline.\n"
        "        /// </para>\n"
        "        /// <para>\n"
        "        /// <b>Insertion points (FR-SN-031), declared and empty.</b> (a') #43's promotion/relegation\n"
        "        /// transform and (b') #40's finance settlement sit between the board evaluation and the\n"
        "        /// regeneration, in that order, so budgets reflect the post-promotion division. They are\n"
        "        /// positions in this method, not interfaces — #40's T0/T1b code now exists, but its boundary\n"
        "        /// producer remains deliberately unwired until T2 (FR-SN-034 / FR-LW-031).\n"
        "        /// (d) #28's age advance is the same: a documented position, empty until #28 T2.\n"
        "        /// </para>\n",
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
        "        /// </para>\n",
    ),
    (
        "            // ── (a') #43 promotion/relegation inserts HERE (FR-SN-031) — empty at Stage 2. ──────\n"
        "            // ── (b') #40 finance settlement inserts HERE (ERR-030-003) — T2-deferred. ───────────\n\n"
        "            // ── (c) regenerate ──────────────────────────────────────────────────────────────────\n",
        "            // ── (a') #43 promotion/relegation inserts HERE (FR-SN-031) — empty at Stage 2. ──────\n"
        "            // ── (b') #40 finance settlement — LIVE (T2b / ERR-030-051). ─────────────────────────\n"
        "            // Stage the COMPLETE finance result here, before regeneration, but do not install it yet.\n"
        "            // BeginNextSeason below is still the roll's fallible commit; mutating _finances before it\n"
        "            // would leave a refused roll with next-season budgets beside the old season table.\n"
        "            ClubFinanceEntry[] settledFinances =\n"
        "                SeasonFinanceRuntime.PrepareSettlement(_state, _finances);\n\n"
        "            // ── (c) regenerate ──────────────────────────────────────────────────────────────────\n",
    ),
    (
        "            // Cannot throw: EvaluateAtSeasonEnd returns an already-validated BoardState.\n"
        "            _state.SetBoard(evaluated);\n\n"
        "            // The staged reconciliation installs last, after the final write that could have refused\n",
        "            // Cannot throw: EvaluateAtSeasonEnd returns an already-validated BoardState.\n"
        "            _state.SetBoard(evaluated);\n\n"
        "            // ERR-030-051: the b' values were calculated from the FINAL table before (c), but become\n"
        "            // live only now, after the roll's final fallible write. Length and ordering are unchanged,\n"
        "            // so this copy cannot create/remove a club and cannot throw after composition validation.\n"
        "            System.Array.Copy(settledFinances, _finances, settledFinances.Length);\n\n"
        "            // The staged reconciliation installs last, after the final write that could have refused\n",
    ),
    (
        "// | 1.31    | 2026-09-10 | —      | ERR-030-050: the #40 T1b finance state now lives on SeasonLoop,  |\n"
        "// |         |            |        | constructor/Restore accept financesOrNull, and non-empty entries |\n"
        "// |         |            |        | are normalized against the current SeasonState.ClubIds so the    |\n"
        "// |         |            |        | persisted family is resumable before T2 wires its producer.      |\n"
        "#endregion\n",
        "// | 1.31    | 2026-09-10 | —      | ERR-030-050: the #40 T1b finance state now lives on SeasonLoop,  |\n"
        "// |         |            |        | constructor/Restore accept financesOrNull, and non-empty entries |\n"
        "// |         |            |        | are normalized against the current SeasonState.ClubIds so the    |\n"
        "// |         |            |        | persisted family is resumable before T2 wires its producer.      |\n"
        "// | 1.32    | 2026-09-11 | —      | ERR-030-051 / #40 T2b: live keyed finance read/ledger commands;   |\n"
        "// |         |            |        | settlement staged at (b') and installed only after the season    |\n"
        "// |         |            |        | commit succeeds, preserving all-or-nothing boundary semantics.    |\n"
        "#endregion\n",
    ),
]

for old, new in replacements:
    count = text.count(old)
    if count != 1:
        raise SystemExit(f"expected exactly one replacement target, found {count}: {old[:100]!r}")
    text = text.replace(old, new, 1)

path.write_text(text, encoding="utf-8")
