from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def replace_once(rel, old, new):
    path = ROOT / rel
    text = path.read_text()
    count = text.count(old)
    if count != 1:
        raise SystemExit(f"{rel}: expected exactly one occurrence, found {count}: {old[:120]!r}")
    path.write_text(text.replace(old, new, 1))


# Finding 1: player-database consumer prose must match the actual T0/T1a asmdef graph.
replace_once(
    "docs/specs/code-standards/section-3.md",
    "**Modified:** September 4, 2026\n**Version:** 1.13",
    "**Modified:** September 7, 2026\n**Version:** 1.13",
)
replace_once(
    "docs/specs/code-standards/section-3.md",
    "Today's consumers are Composition's `match-engine`, six of the seven Management assemblies (`player-progression`, `training-system`, `injuries-medical`, `discipline`, `season-save`, `club-finances` — not `living-world`) and Client's `match-client-core`",
    "Today's consumers are Composition's `match-engine`, five of the seven Management assemblies (`player-progression`, `training-system`, `injuries-medical`, `discipline`, `season-save` — not `living-world` or `club-finances`) and Client's `match-client-core`",
)
replace_once(
    "docs/specs/code-standards/section-3.md",
    "| 1.13 | September 4, 2026 | Codex | **D4/#40 T0 seating.** Adds `club-finances` (#40) to Tier 7 Management in the same landing as its production `.asmdef` and refreshes the `player-database` current Management-consumer note from five-of-six to six-of-seven. No dependency-direction rule or A3.1a governance semantics change. | — |",
    "| 1.13 | September 7, 2026 | Codex | **D4/#40 T0/T1a seating, review-corrected in place before merge.** Adds `club-finances` (#40) to Tier 7 Management in the same landing as its production `.asmdef`. The `player-database` current Management-consumer note remains five consumers while the denominator moves six→seven because `club-finances` deliberately defers its #27 edge to T2; current non-consumers are `living-world` and `club-finances`. No dependency-direction rule or A3.1a governance semantics change. | — |",
)

# Finding 2: normalize the new CHANGELOG-src table row to the table's four-column grammar.
replace_once(
    "docs/tracking/CHANGELOG-src.md",
    "| v2.128 | September 6, 2026 | #40 Club Finances T0 + T1a: new production/test assembly, standalone FNCE codec, external-review dependency/phase/test corrections; T1b/T2/T3 deferred. |",
    "| 2.128 | 2026-09-06 | — | **#40 Club Finances T0 + T1a.** New production/test assembly, standalone `FNCE` codec, external-review dependency/phase/test corrections, and follow-up non-positive `BoardModifier` fail-loud correction; T1b/T2/T3 deferred. |",
)

# Finding 3: decide the negative-board-modifier contract deliberately: non-positive is invalid and fails loud.
replace_once(
    "src/club-finances/FinanceStep.cs",
    "// Modified: 2026-09-06",
    "// Modified: 2026-09-07",
)
replace_once(
    "src/club-finances/FinanceStep.cs",
    "if (board.BudgetMultiplierMillPermille == 0)",
    "if (board.BudgetMultiplierMillPermille <= 0)",
)
replace_once(
    "src/club-finances/FinanceStep.cs",
    '"BoardModifier multiplier 0 is invalid; use BoardModifier.Identity for no adjustment (F4)."',
    '"BoardModifier multiplier must be positive; use BoardModifier.Identity for no adjustment (F4)."',
)
replace_once(
    "src/club-finances/FinanceStep.cs",
    "// 1.1     | 2026-09-06 | —             | Header author attribution corrected to automated-agent placeholder.\n#endregion",
    "// 1.1     | 2026-09-06 | —             | Header author attribution corrected to automated-agent placeholder.\n// 1.2     | 2026-09-07 | OpenAI        | F4 widened from zero-only to all non-positive board multipliers.\n#endregion",
)

replace_once(
    "src/club-finances/BoardModifier.cs",
    "// Modified: 2026-09-06",
    "// Modified: 2026-09-07",
)
replace_once(
    "src/club-finances/BoardModifier.cs",
    "/// <summary>Budget multiplier in per-mille units; zero is invalid at the settlement seam.</summary>",
    "/// <summary>Budget multiplier in per-mille units; non-positive values are invalid at the settlement seam.</summary>",
)
replace_once(
    "src/club-finances/BoardModifier.cs",
    "// 1.1     | 2026-09-06 | —             | Header author attribution corrected to automated-agent placeholder.\n#endregion",
    "// 1.1     | 2026-09-06 | —             | Header author attribution corrected to automated-agent placeholder.\n// 1.2     | 2026-09-07 | OpenAI        | Documented the F4 positive-multiplier runtime contract.\n#endregion",
)

replace_once(
    "docs/specs/club-finances-economy/section-2.md",
    "**Last Updated:** September 4, 2026 (v0.3 — T0 reference-contract back-prop)\n**Last Updated (prior):** July 23, 2026 (v0.2 — AR-1 wage-semantics fix; prior v0.1 initial)\n**Version:** 0.3",
    "**Last Updated:** September 7, 2026 (v0.4 — PR #363 follow-up: non-positive BoardModifier values fail loud)\n**Last Updated (prior):** September 4, 2026 (v0.3 — T0 reference-contract back-prop)\n**Version:** 0.4",
)
replace_once(
    "docs/specs/club-finances-economy/section-2.md",
    "`BudgetMultiplierMillPermille = 1000`), and `default(BoardModifier)` (all-zero, ×0) MUST NOT be treated\n  as a valid runtime value — reaching `SettleFinances` it MUST **fail loud** (F4, the #41 `MedicalModifier`\n  zero-value-trap lesson applied here per §1.6).",
    "`BudgetMultiplierMillPermille = 1000`), and any `BoardModifier` with\n  `BudgetMultiplierMillPermille <= 0` (including `default(BoardModifier)`, all-zero ×0) MUST NOT be treated\n  as a valid runtime value — reaching `SettleFinances` it MUST **fail loud** (F4, the #41 `MedicalModifier`\n  zero-value-trap lesson generalized to the full invalid non-positive domain per §1.6).",
)
replace_once(
    "docs/specs/club-finances-economy/section-2.md",
    "// x0) is NOT a valid runtime value; it MUST fail loud at SettleFinances (FR-FN-018 / F4), mirroring #41's\n// MedicalModifier Identity-vs-default() lesson (§1.6).",
    "// x0) and any negative multiplier are NOT valid runtime values; they MUST fail loud at SettleFinances\n// (FR-FN-018 / F4), mirroring #41's MedicalModifier Identity-vs-default() lesson (§1.6).",
)
replace_once(
    "docs/specs/club-finances-economy/section-2.md",
    "public readonly int BudgetMultiplierMillPermille;   // 1000 = x1.0; > 1000 raises the projected ceilings",
    "public readonly int BudgetMultiplierMillPermille;   // must be > 0; 1000 = x1.0; > 1000 raises the projected ceilings",
)
replace_once(
    "docs/specs/club-finances-economy/section-2.md",
    "| **F4** | A `BoardModifier` with `BudgetMultiplierMillPermille == 0` (e.g. `default(BoardModifier)`) reaching `SettleFinances` | **Fail loud** — a zero multiplier is a caller-contract bug (×0 budget), not a legitimate \"no adjustment\" identity (the #41 `MedicalModifier` zero-value-trap precedent, §1.6). |",
    "| **F4** | A `BoardModifier` with `BudgetMultiplierMillPermille <= 0` (including `default(BoardModifier)`) reaching `SettleFinances` | **Fail loud** — a non-positive multiplier is a caller-contract bug, not a legitimate budget adjustment or \"no adjustment\" identity (the #41 `MedicalModifier` zero-value-trap precedent generalized to the invalid negative domain, §1.6). |",
)
replace_once(
    "docs/specs/club-finances-economy/section-2.md",
    "| 0.3 | 2026-09-04 | — | **T0 implementation back-prop.** FR-FN-027 now explicitly permits the cross-cutting `ProjectConstants` foundation edge used only for Code Standards #20-mandated `[GT]` `GameplayConfig.Get*` loading; the domain dependency direction and forbidden upward references are unchanged. |\n#endregion",
    "| 0.3 | 2026-09-04 | — | **T0 implementation back-prop.** FR-FN-027 now explicitly permits the cross-cutting `ProjectConstants` foundation edge used only for Code Standards #20-mandated `[GT]` `GameplayConfig.Get*` loading; the domain dependency direction and forbidden upward references are unchanged. |\n| 0.4 | 2026-09-07 | OpenAI | **PR #363 follow-up review correction.** FR-FN-018/F4 now make the whole non-positive `BoardModifier` domain (`<= 0`) fail loud instead of allowing a negative multiplier to reach the budget clamp. |\n#endregion",
)

replace_once(
    "docs/specs/club-finances-economy/section-3.md",
    "**Last Updated:** July 23, 2026 (v0.2 — AR-1 wage-semantics fix; prior v0.1 initial)\n**Version:** 0.2",
    "**Last Updated:** September 7, 2026 (v0.3 — PR #363 follow-up: non-positive board multiplier failure gate)\n**Last Updated (prior):** July 23, 2026 (v0.2 — AR-1 wage-semantics fix; prior v0.1 initial)\n**Version:** 0.3",
)
replace_once(
    "docs/specs/club-finances-economy/section-3.md",
    "assert board.BudgetMultiplierMillPermille != 0                     # F4 — zero-value trap (fail loud)",
    "assert board.BudgetMultiplierMillPermille > 0                      # F4 — non-positive caller error (fail loud)",
)
replace_once(
    "docs/specs/club-finances-economy/section-3.md",
    "| 0.2 | 2026-07-23 | — | AR-1 (1M): §3.2 `ApplyTransaction` split — wage line items change `WageBillAggregate` only (periodic cash-out deferred), cash line items change `Balance` only; worked example updated. |\n#endregion",
    "| 0.2 | 2026-07-23 | — | AR-1 (1M): §3.2 `ApplyTransaction` split — wage line items change `WageBillAggregate` only (periodic cash-out deferred), cash line items change `Balance` only; worked example updated. |\n| 0.3 | 2026-09-07 | OpenAI | **PR #363 follow-up review correction.** §3.1 now rejects every non-positive board multiplier before arithmetic; the lower budget clamp is not an authorization for a negative modifier. |\n#endregion",
)

replace_once(
    "docs/specs/club-finances-economy/section-5.md",
    "**Last Updated:** July 23, 2026 (v0.2 — AR-1 wage-semantics fix; prior v0.1 initial)\n**Version:** 0.2",
    "**Last Updated:** September 7, 2026 (v0.3 — PR #363 follow-up: non-positive board failure coverage)\n**Last Updated (prior):** July 23, 2026 (v0.2 — AR-1 wage-semantics fix; prior v0.1 initial)\n**Version:** 0.3",
)
replace_once(
    "docs/specs/club-finances-economy/section-5.md",
    "- **T-FN-FAIL-BOARD-001** — `default(BoardModifier)` (`BudgetMultiplierMillPermille == 0`) reaching\n  `SettleFinances` fails loud (the zero-value-trap gate, mirrors #41's `MedicalModifier` F4 lesson) —\n  FR-FN-018/F4.",
    "- **T-FN-FAIL-BOARD-001** — Any `BoardModifier` with `BudgetMultiplierMillPermille <= 0`, including\n  `default(BoardModifier)` and an explicit negative multiplier, reaching `SettleFinances` fails loud —\n  FR-FN-018/F4. The clamp is not used to legitimize an invalid board multiplier.",
)
replace_once(
    "docs/specs/club-finances-economy/section-5.md",
    "| 0.2 | 2026-07-23 | — | AR-1 (1M): T-FN-LEDGER-002 / T-FN-BOUND-003 restated — wage transaction moves the aggregate only, `Balance` unchanged. |\n#endregion",
    "| 0.2 | 2026-07-23 | — | AR-1 (1M): T-FN-LEDGER-002 / T-FN-BOUND-003 restated — wage transaction moves the aggregate only, `Balance` unchanged. |\n| 0.3 | 2026-09-07 | OpenAI | **PR #363 follow-up review correction.** T-FN-FAIL-BOARD-001 widened from default/zero only to every non-positive board multiplier. |\n#endregion",
)

replace_once(
    "docs/specs/club-finances-economy/section-7.md",
    "**Last Updated:** September 6, 2026 (v0.6 — PR #363 external-review correction: T1a/T1b split and phase-real dependencies)\n**Last Updated (prior):** September 4, 2026 (v0.5 — T0 critique closure)\n**Version:** 0.6",
    "**Last Updated:** September 7, 2026 (v0.7 — PR #363 follow-up review correction)\n**Last Updated (prior):** September 6, 2026 (v0.6 — PR #363 external-review correction: T1a/T1b split and phase-real dependencies)\n**Version:** 0.7",
)
replace_once(
    "docs/specs/club-finances-economy/section-7.md",
    "   removed from T0/T1a and assigned to T2, where `Squad.ClubId` is first consumed. Executable locks now cover\n   the exact current asmdef boundary, no-RNG serialized shape, both budget clamps, decode ordering, and short/\n   truncated framing.\n\nThis landing therefore delivers **T0 + T1a**.",
    "   removed from T0/T1a and assigned to T2, where `Squad.ClubId` is first consumed. Executable locks now cover\n   the exact current asmdef boundary, no-RNG serialized shape, the upper budget clamp, decode ordering, and short/\n   truncated framing.\n6. **Board-modifier domain correction.** A follow-up review found that the new negative-multiplier test had\n   silently made a nonsense board multiplier valid without spec authority. FR-FN-018/F4 and §3.1 now reject\n   every non-positive multiplier before arithmetic; the regression lock asserts that negative input fails loud.\n\nThis landing therefore delivers **T0 + T1a**.",
)
replace_once(
    "docs/specs/club-finances-economy/section-7.md",
    "| 0.6 | 2026-09-06 | — | **PR #363 external-review correction.** Reclassifies the already-landed standalone codec as T1a, creates T1b for #30 composition/version bump, defers the unused PlayerDatabase edge to its first T2 consumer, and records the added regression locks. |\n#endregion",
    "| 0.6 | 2026-09-06 | — | **PR #363 external-review correction.** Reclassifies the already-landed standalone codec as T1a, creates T1b for #30 composition/version bump, defers the unused PlayerDatabase edge to its first T2 consumer, and records the added regression locks. |\n| 0.7 | 2026-09-07 | OpenAI | **PR #363 follow-up review correction.** Records the deliberate non-positive `BoardModifier` fail-loud decision and corrects the clamp-coverage wording. |\n#endregion",
)

# Remove stale close-out wording that would become false after the negative-modifier correction.
replace_once(
    "docs/tracking/file-manifest.md",
    "External-review locks cover the asmdef boundary, no-RNG serialized shape, both budget clamps, decode ordering, and short/truncated framing.",
    "External-review locks cover the asmdef boundary, no-RNG serialized shape, the upper budget clamp, non-positive board-modifier failure, decode ordering, and short/truncated framing.",
)
replace_once(
    "docs/tracking/path-to-playable-roadmap.md",
    "Added locks cover exact current references, no RNG cursor/action ordinal, upper/lower budget clamps, decode ordering, and short/truncated framing.",
    "Added locks cover exact current references, no RNG cursor/action ordinal, the upper budget clamp, non-positive board-modifier failure, decode ordering, and short/truncated framing.",
)

# The helper removes itself and its one-shot workflow from the commit it produces.
(ROOT / ".github/scripts/pr363_claude_followup.py").unlink()
(ROOT / ".github/workflows/pr363-claude-followup.yml").unlink()
