#!/usr/bin/env python3
from pathlib import Path
import re


def load(path):
    return Path(path).read_text(encoding="utf-8")


def save(path, text):
    Path(path).write_text(text, encoding="utf-8")


def once(text, old, new, label):
    n = text.count(old)
    if n != 1:
        raise SystemExit(f"{label}: expected 1 literal target, found {n}")
    return text.replace(old, new, 1)


def regex_once(text, pattern, repl, label, flags=0):
    text, n = re.subn(pattern, repl, text, count=1, flags=flags)
    if n != 1:
        raise SystemExit(f"{label}: expected 1 regex target, found {n}")
    return text


def add_history(text, row, label):
    marker = "#endregion"
    idx = text.rfind(marker)
    if idx < 0:
        raise SystemExit(f"{label}: no #endregion")
    if row.split(" | ", 2)[1] in text:
        raise SystemExit(f"{label}: history version already exists")
    return text[:idx] + row + text[idx:]


# ---------------------------------------------------------------------------
# #40 §5 — executable acceptance contract.
# ---------------------------------------------------------------------------
p = "docs/specs/club-finances-economy/section-5.md"
t = load(p)
t = once(t,
    "**Last Updated:** September 11, 2026 (v0.5 — T-FN-LIFE-001 coverage split recorded across T2a/T2b)\n",
    "**Last Updated:** September 11, 2026 (v0.6 — ERR-030-051/T2b: boundary atomicity contract corrected and live lifecycle evidence named)\n"
    "**Last Updated (prior):** September 11, 2026 (v0.5 — T-FN-LIFE-001 coverage split recorded across T2a/T2b)\n",
    "#40 §5 header")
t = once(t, "**Version:** 0.5\n", "**Version:** 0.6\n", "#40 §5 version")
t = regex_once(t,
    r"- \*\*T-FN-DET-002\*\* — Save→restore across a \*\*mid-`RollToNextSeason\(\)`\*\* boundary.*?`SettleFinances` is not re-run for any club on resume\.\n",
    "- **T-FN-DET-002** — `RollToNextSeason()` is one synchronous, atomic command: there is **no supported\n"
    "  mid-call save seam** between (b') and (c). A save immediately before the call restores the pre-roll\n"
    "  finance state; a save immediately after a successful return restores the already-settled state\n"
    "  field-identically and does not settle a second time merely because it was restored. If the roll is\n"
    "  refused after the (b') result has been computed, live `ClubFinances` remains byte-for-byte unchanged.\n"
    "  This is the #40 extension of #30's all-or-nothing boundary contract (FR-FN-024 / ERR-030-051).\n",
    "#40 T-FN-DET-002", re.DOTALL)
t = regex_once(t,
    r"- \*\*T-FN-LIFE-001\*\* — Every `ClubId`.*?bootstrap invocation and settlement wiring\.\n",
    "- **T-FN-LIFE-001** — Every `ClubId` in #27's `Squad` enumeration has a `ClubFinances` entry after\n"
    "  `CreateInitial` bootstrap; the per-club entry count is unchanged across any number of season rolls\n"
    "  (clubs do not churn, KD-7) — no leak, no removal. T2a proves canonical bootstrap/universe coherence;\n"
    "  T2b's `SeasonLoopFinanceTests.RollToNextSeason_SettlesEveryClubFromFinalTable_AndPreservesClubSetAcrossRolls`\n"
    "  proves the across-roll half through the real #30 boundary.\n",
    "#40 T-FN-LIFE-001", re.DOTALL)
t = add_history(t,
    "| 0.6 | 2026-09-11 | — | **ERR-030-051 / T2b executable-contract correction.** T-FN-DET-002 removes the unsupported mid-call save premise and instead locks synchronous all-or-nothing boundary semantics; T-FN-LIFE-001 names the live across-roll regression evidence. |\n",
    "#40 §5")
save(p, t)

# ---------------------------------------------------------------------------
# #40 §7 — phase status and landed T2b mechanics.
# ---------------------------------------------------------------------------
p = "docs/specs/club-finances-economy/section-7.md"
t = load(p)
t = once(t,
    "**Last Updated:** September 11, 2026 (v1.1 — PR #392 formalizes T2a/T2b: T2a is the #27-backed bootstrap factory/reference activation; T2b is #30 invocation + settlement wiring)\n",
    "**Last Updated:** September 11, 2026 (v1.2 — T2b implemented: #30 bootstrap invocation, live boundary settlement, and finance command/read surfaces; ERR-030-051 atomicity correction)\n"
    "**Last Updated (prior):** September 11, 2026 (v1.1 — PR #392 formalizes T2a/T2b: T2a is the #27-backed bootstrap factory/reference activation; T2b is #30 invocation + settlement wiring)\n",
    "#40 §7 header")
t = once(t, "**Version:** 1.1\n", "**Version:** 1.2\n", "#40 §7 version")
t = regex_once(t,
    r"- \*\*T2b\*\* — Wire #30's one-time invocation.*?No #30\n  tick-order change beyond the KD-6 back-prop already filed \(KD-6\)\.\n",
    "- **T2b** *(implemented September 11, 2026)* — #30 now owns the one-time production invocation through\n"
    "  `League.CreateLoop`: it feeds the league's canonical `Squad[]` set into T2a's\n"
    "  `ClubFinanceEntry.CreateInitialForSquads` exactly once and composes the resulting entries into\n"
    "  `SeasonLoop`. `RollToNextSeason()` computes every club's `FinanceStep.SettleFinances` result at the\n"
    "  reserved step (b') from the final table, before (c) regenerate, through `SeasonFinanceRuntime`. The\n"
    "  values are staged until `BeginNextSeason` succeeds and only then copied into the live finance array,\n"
    "  preserving #30's refused-roll atomicity (ERR-030-051) without moving the semantic settlement point.\n"
    "  `SeasonLoop.FinanceView`, `AvailableTransferBudget`, and `ApplyTransaction` expose the #40-owned\n"
    "  observer/query/command surfaces without granting direct field mutation. T3 is now the next #40 phase.\n",
    "#40 §7 T2b", re.DOTALL)
t = add_history(t,
    "| 1.2 | 2026-09-11 | — | **T2b implementation / ERR-030-051.** Records `League.CreateLoop` as #30's one-time bootstrap owner, the live staged settlement at (b'), post-commit finance installation preserving refused-roll atomicity, and the public observer/query/command surfaces. T3 becomes the next phase. |\n",
    "#40 §7")
save(p, t)

# ---------------------------------------------------------------------------
# #40 §4 — architecture now has live T2b rather than future arrows.
# ---------------------------------------------------------------------------
p = "docs/specs/club-finances-economy/section-4.md"
t = load(p)
t = once(t,
    "**Last Updated:** September 11, 2026 (v0.6 — PR #392 T2a: the consumed #40 → #27 Squad.ClubId edge is now live; #30 invocation remains T2b)\n",
    "**Last Updated:** September 11, 2026 (v0.7 — T2b: #30 bootstrap/settlement composition is live; ERR-030-051 pins staged commit semantics)\n"
    "**Last Updated (prior):** September 11, 2026 (v0.6 — PR #392 T2a: the consumed #40 → #27 Squad.ClubId edge is now live; #30 invocation remains T2b)\n",
    "#40 §4 header")
t = once(t, "**Version:** 0.6\n", "**Version:** 0.7\n", "#40 §4 version")
t = once(t,
    "T2b future:   #30 SeasonSave ──────▶ ClubFinances (#40)         [invoke bootstrap + settle at step (b')]\nT2+ future:   #31/#34/#45 ─────────▶ ClubFinances (#40)         [query/commands/modifier producer]\n",
    "T2b current:  #30 SeasonSave ──────▶ ClubFinances (#40)         [League.CreateLoop bootstrap + staged settle at step (b')]\nT3/future:    #31/#34/#45 ─────────▶ ClubFinances (#40)         [downstream query/commands/modifier producers]\n",
    "#40 §4 diagram")
t = once(t,
    "- **To #30 (T1b/T2b):** T1b composes #40's opaque codec into `SeasonSaveCodec` and bumps the composing\n  format. T2b makes the #30 composition root invoke T2a's bootstrap transform once at league/game bootstrap,\n  then makes `RollToNextSeason()` invoke `SettleFinances` per club at the reserved step (b'). #40 never\n  references #30 and never invokes its own bootstrap factory independently.\n",
    "- **To #30 (T1b/T2b, live):** T1b composes #40's opaque codec into `SeasonSaveCodec`. T2b's\n  `League.CreateLoop` is the new-game lifecycle seam: it invokes T2a's bootstrap transform exactly once.\n  `SeasonLoop.RollToNextSeason()` computes `SettleFinances` per club at reserved step (b') and stages those\n  values until the fallible #30 season commit succeeds; only then are they installed (ERR-030-051). #40\n  never references #30 and never invokes its own bootstrap factory independently.\n",
    "#40 §4 #30 seam")
t = add_history(t,
    "| 0.7 | 2026-09-11 | — | **T2b / ERR-030-051 architecture back-prop.** Promotes #30's bootstrap/settlement edge to current, records `League.CreateLoop` as the lifecycle owner, and pins settlement-at-(b') with post-commit installation to preserve #30 atomicity. |\n",
    "#40 §4")
save(p, t)

# ---------------------------------------------------------------------------
# #40 appendices — replace the impossible mid-call save worked example.
# ---------------------------------------------------------------------------
p = "docs/specs/club-finances-economy/appendices.md"
t = load(p)
t = once(t,
    "**Last Updated:** September 6, 2026 (v0.5 — PR #363 critique: config-loader range disclosure and T1a terminology)\n",
    "**Last Updated:** September 11, 2026 (v0.6 — ERR-030-051: season-boundary worked example corrected to synchronous atomic RollToNextSeason semantics)\n"
    "**Last Updated (prior):** September 6, 2026 (v0.5 — PR #363 critique: config-loader range disclosure and T1a terminology)\n",
    "#40 appendix header")
t = once(t, "**Version:** 0.5\n", "**Version:** 0.6\n", "#40 appendix version")
t = once(t,
    "## Appendix B — Worked example: save/restore across a mid-season AND a mid-boundary-roll boundary\n",
    "## Appendix B — Worked example: save/restore mid-season AND across the atomic season boundary\n",
    "#40 Appendix B title")
t = regex_once(t,
    r"\*\*Mid-`RollToNextSeason\(\)` boundary\.\*\*.*?season-boundary wiring phases land\.\n",
    "**Atomic `RollToNextSeason()` boundary.** `RollToNextSeason()` is synchronous; callers cannot save\n"
    "between its internal steps. At step (b') #30 computes every club's settled finance value from the final\n"
    "table and holds those values staged. If the later season commit refuses, the live finance array remains\n"
    "unchanged and a save still contains the pre-roll values. After a successful return the staged values are\n"
    "installed exactly once; a save taken then restores those six fields identically. Restoring that completed\n"
    "save does not re-run settlement merely because a restore occurred. This is T-FN-DET-002 after the\n"
    "ERR-030-051 atomicity correction.\n",
    "#40 Appendix B boundary", re.DOTALL)
t = add_history(t,
    "| 0.6 | 2026-09-11 | — | **ERR-030-051 / T2b correction.** Replaces the impossible mid-`RollToNextSeason()` save example with the supported synchronous contract: stage finance settlement at (b'), leave live values untouched on refusal, install once after a successful season commit, and round-trip before/after-call saves field-identically. |\n",
    "#40 appendices")
save(p, t)

# ---------------------------------------------------------------------------
# #40 §9 — source status, but gate-dependent checkboxes stay open until CI.
# ---------------------------------------------------------------------------
p = "docs/specs/club-finances-economy/section-9-approval-checklist.md"
t = load(p)
t = once(t,
    "**Last Updated:** September 11, 2026 (v0.5 — PR #392 T2a source status recorded; checkbox held open pending executed gate evidence)\n",
    "**Last Updated:** September 11, 2026 (v0.6 — T2b source status recorded; T2a/T2b checkboxes remain open pending executed current-head gate evidence)\n"
    "**Last Updated (prior):** September 11, 2026 (v0.5 — PR #392 T2a source status recorded; checkbox held open pending executed gate evidence)\n",
    "#40 §9 header")
t = once(t, "**Version:** 0.5\n", "**Version:** 0.6\n", "#40 §9 version")
t = regex_once(t,
    r"- \[ \] T2b production invocation \+ settlement — NOT YET LANDED\..*?until this wiring lands\.\n",
    "- [ ] T2b production invocation + settlement — **IMPLEMENTED ON THIS BRANCH; GATE EVIDENCE PENDING**.\n"
    "      Source anchors: `League.CreateLoop` invokes `CreateInitialForSquads` once from canonical league\n"
    "      squads; `SeasonFinanceRuntime.PrepareSettlement` computes every club at (b'); `SeasonLoop` installs\n"
    "      the staged values only after `BeginNextSeason` succeeds (ERR-030-051); `FinanceView`,\n"
    "      `AvailableTransferBudget`, and `ApplyTransaction` expose the observer/query/command surfaces; and\n"
    "      `SeasonLoopFinanceTests` covers bootstrap, keyed ledger routing, across-roll identity/settlement, and\n"
    "      refused-roll atomicity. Keep this box open until the current landing head's functional gate passes.\n",
    "#40 §9 T2b", re.DOTALL)
t = add_history(t,
    "| 0.6 | 2026-09-11 | — | **T2b source-status refresh / ERR-030-051.** Records the live #30 bootstrap, staged boundary settlement, command/read surfaces and `SeasonLoopFinanceTests`; both T2a/T2b evidence boxes remain deliberately unchecked until the current-head functional gate executes successfully. |\n",
    "#40 §9")
save(p, t)

# ---------------------------------------------------------------------------
# #30 §3 — live b' ordering plus atomic installation semantics.
# ---------------------------------------------------------------------------
p = "docs/specs/season-competition-loop/section-3.md"
t = load(p)
if "v2.20 — ERR-030-051" not in t:
    t = once(t, "**Created:** July 22, 2026\n",
        "**Created:** July 22, 2026\n"
        "**Last Updated:** September 11, 2026 (v2.20 — ERR-030-051: #40 T2b makes (b') live while preserving refused-roll atomicity by staging settlement until the fallible season commit succeeds)\n",
        "#30 §3 header insert")
# First Version field only.
t = regex_once(t, r"\*\*Version:\*\* [^\n]+", "**Version:** 2.20", "#30 §3 version")
t = once(t,
    "    # (b')  <-- #40 finance settlement inserts HERE (ERR-030-003) — after (a') so budgets reflect the\n    #           post-promotion division; SettleFinances(financeState[club], position, clubCount, board)\n    #           per club. NULL SEAM until #40 T2 wires it; #40 references #30 never (one-way #30 → #40).\n",
    "    # (b')  #40 finance settlement — LIVE at T2b (ERR-030-051), after (a') so budgets reflect the\n    #           post-promotion division. Compute/stage SettleFinances(financeState[club], position,\n    #           clubCount, board) for every club HERE; do not install into live finance state yet.\n",
    "#30 §3 bprime pseudo")
t = once(t,
    "    Seed := nextSeed\n    # (f) #44's season-boundary sweep (FR-DC-017) — LIVE since T2 (C1/C2, August 13, 2026):\n",
    "    Seed := nextSeed\n    FinanceState := stagedFinanceState                 # commit b' only AFTER (e) succeeds; cannot throw\n    # (f) #44's season-boundary sweep (FR-DC-017) — LIVE since T2 (C1/C2, August 13, 2026):\n",
    "#30 §3 finance commit pseudo")
t = regex_once(t,
    r"Each step mutates a well-defined slice of `SeasonState`;.*?\(per-club `ClubFinances` prior state carried in\)\.\n",
    "Each boundary step is first **computed/staged** from the prior state. The supported public operation is the\n"
    "single synchronous `RollToNextSeason()` call; there is no externally observable save point between its\n"
    "internal steps. #43 remains a transform inserted at (a'). #40 T2b is live at (b'): its complete per-club\n"
    "settlement is computed from the final table before regeneration, but live finance entries are not replaced\n"
    "until after `BeginNextSeason` — the roll's final fallible season commit — succeeds. A refusal therefore\n"
    "leaves both `SeasonState` and `ClubFinances` untouched; a successful call installs the staged finance values\n"
    "exactly once. This resolves the prior #40 mid-call-save wording without weakening FR-SN-029 (ERR-030-051).\n",
    "#30 §3 boundary prose", re.DOTALL)
t = once(t,
    "a refused roll leaves the season untouched rather than carrying a committed board verdict against a\nschedule that was then rejected.\n",
    "a refused roll leaves the season **and staged #40 finance result** untouched rather than carrying a\ncommitted board verdict or next-season budget against a schedule that was then rejected.\n",
    "#30 §3 boundary condition")
t = add_history(t,
    "| 2.20 | 2026-09-11 | — | **ERR-030-051 / #40 T2b.** Step (b') becomes live. The complete finance result is computed there from the final table before regeneration, then installed only after `BeginNextSeason` succeeds; refused rolls leave finance state untouched. The prior mid-roll-save wording is retired because `RollToNextSeason()` is synchronous and exposes no such save seam. |\n",
    "#30 §3")
save(p, t)

# ---------------------------------------------------------------------------
# #30 §4 — composition root's live finance surfaces.
# ---------------------------------------------------------------------------
p = "docs/specs/season-competition-loop/section-4.md"
t = load(p)
t = once(t,
    "**Last Updated:** September 10, 2026 (v1.0 — ERR-030-050: the T1b resume/composition surface includes `SeasonLoop.Restore(..., financesOrNull)` and loop-held finance state. Prior update follows)\n",
    "**Last Updated:** September 11, 2026 (v1.1 — ERR-030-051 / #40 T2b: live bootstrap/settlement and finance observer/query/command surfaces on the #30 composition root)\n"
    "**Last Updated (prior):** September 10, 2026 (v1.0 — ERR-030-050: the T1b resume/composition surface includes `SeasonLoop.Restore(..., financesOrNull)` and loop-held finance state. Prior update follows)\n",
    "#30 §4 header")
t = once(t, "**Version:** 1.0\n", "**Version:** 1.1\n", "#30 §4 version")
t = regex_once(t,
    r"- the \*\*`ClubFinanceEntry\[\] _finances`\*\* carrier.*?production wiring\.\n",
    "- the **`ClubFinanceEntry[] _finances`** live finance state (T1b carrier; T2b producer/mutator,\n"
    "  ERR-030-050/051). `League.CreateLoop` bootstraps exactly one entry per canonical league squad for a new\n"
    "  game; `Restore(..., financesOrNull)` supplies resumed state. Non-empty entries exactly cover\n"
    "  `_state.ClubIds`. `FinanceView(clubId)` returns a detached observer value;\n"
    "  `AvailableTransferBudget(clubId)` and `ApplyTransaction(clubId, ...)` route through #40's canonical\n"
    "  ledger; `FinanceEntriesForSave()` remains an internal defensive-copy persistence bridge. At the season\n"
    "  boundary #30 stages #40's (b') settlement and installs it only after the fallible season commit succeeds.\n",
    "#30 §4 finance holding", re.DOTALL)
t = once(t,
    "`RollToNextSeason()`, plus read-only `View()` → `SeasonViewModel`\n(FR-SN-033) and `Snapshot()` / `Restore(...)` for the season sub-blob.",
    "`RollToNextSeason()`, the #40-facing `FinanceView(clubId)` / `AvailableTransferBudget(clubId)` /\n`ApplyTransaction(clubId, ...)` surfaces, plus read-only `View()` → `SeasonViewModel` (FR-SN-033) and\n`Snapshot()` / `Restore(...)` for the season sub-blob.",
    "#30 §4 command API")
t = add_history(t,
    "| 1.1 | 2026-09-11 | — | **ERR-030-051 / #40 T2b.** §4.3 promotes `_finances` from persistence-only carrier to live composed state, records `League.CreateLoop` bootstrap ownership, the observer/query/command surfaces, and staged-(b')/post-commit installation semantics. |\n",
    "#30 §4")
save(p, t)

# ---------------------------------------------------------------------------
# Error ledger — allocate and resolve ERR-030-051 atomically with the repair.
# ---------------------------------------------------------------------------
p = "docs/tracking/spec-error-log.md"
t = load(p)
if "| ERR-030-051 |" in t:
    raise SystemExit("ERR-030-051 collision: rebase/reallocate before landing")
t = once(t, "**Version:** 2.54\n", "**Version:** 2.55\n", "error log version")
t = once(t,
    "**Updated:** September 10, 2026 (v2.54 — **`ERR-030-050` filed and RESOLVED**",
    "**Updated:** September 11, 2026 (v2.55 — **`ERR-030-051` filed and RESOLVED** with #40 T2b. Club Finances §5/Appendix B required a save from inside synchronous `RollToNextSeason()` after finance settlement but before regeneration, while #30's executable boundary contract permits no caller-visible mid-call save seam and requires every refused roll to leave all subsystem state untouched. T2b resolves the conflict without moving KD-6's semantic settlement point: finance results are computed/staged at (b') from the final table before regeneration, then installed only after `BeginNextSeason` succeeds. A refused roll therefore cannot expose next-season finances beside an old season. #40's acceptance text now tests supported before/after-call save/restore plus refusal atomicity. Error Index cardinality increases by one RESOLVED row. Prior update below.)\n**Updated (prior):** September 10, 2026 (v2.54 — **`ERR-030-050` filed and RESOLVED**",
    "error log header")
row50 = re.search(r"^\| ERR-030-050 \|.*$", t, re.MULTILINE)
if not row50:
    raise SystemExit("ERR-030-050 index row not found")
row51 = (
    "\n| ERR-030-051 | Season & Competition Loop #30 §3.5 / Club Finances & Economy #40 T-FN-DET-002 + Appendix B, found during #40 T2b implementation, September 11, 2026: #40 required a save taken inside synchronous `RollToNextSeason()` between (b') finance settlement and (c) regeneration, but #30 exposes no mid-call save seam and its all-or-nothing roll contract requires a refusal to leave every subsystem untouched. Committing finance at (b') before the later fallible `BeginNextSeason` would create a half-rolled state on refusal. | High | 8 (`season-competition-loop/section-3.md`, `section-4.md`; `club-finances-economy/section-4.md`, `section-5.md`, `section-7.md`, `appendices.md`, `section-9-approval-checklist.md`; this log) | ✅ **Resolved September 11, 2026, spec + code same T2b landing.** `FinanceStep.SettleFinances` remains semantically at (b'): `SeasonFinanceRuntime.PrepareSettlement` computes a complete per-club result from the final table before regeneration. The result is staged, not live, until #30's final fallible season commit (`BeginNextSeason`) succeeds; then it is copied into the canonical finance array before the remaining non-throwing boundary installs. Refused rolls leave finance state unchanged. `League.CreateLoop` owns one-time new-game bootstrap, restore continues through the T1b persisted carrier, and `SeasonLoopFinanceTests` locks bootstrap, keyed ledger routing, across-roll identity/settlement and refusal atomicity. T-FN-DET-002/Appendix B now test only supported before/after-call save semantics rather than an impossible internal save point. |"
)
pos = row50.end()
t = t[:pos] + row51 + t[pos:]
save(p, t)

print("patched Spec #40 T2b documentation and ERR-030-051")
