from pathlib import Path


def patch(path, transform):
    p = Path(path)
    old = p.read_text(encoding="utf-8")
    new = transform(old)
    if new == old:
        raise SystemExit(f"no change produced for {path}")
    p.write_text(new, encoding="utf-8")


def project_reference(s):
    old = "├── src/                            ← Implementation (coding began May 19, 2026) — 35 production assemblies"
    if old not in s:
        # tolerate the shorter spacing variant in the compact map
        old = "├── src/                     ← Implementation (coding began May 19, 2026) — 35 production assemblies"
    if old not in s:
        raise SystemExit("project-reference src count anchor missing")
    return s.replace(old, old.replace("35 production assemblies", "37 production assemblies"), 1)


def data_contract_index(s):
    anchor = "| Club finance transaction seam | #40 §2.2, §3.2 | `club-finances` | `FinanceTransaction`, `FinanceLedger` |"
    if anchor not in s:
        raise SystemExit("data-contract finance anchor missing")
    if "| Managed transfer state | #31" in s:
        raise SystemExit("#31 data-contract rows already present")
    rows = (
        "\n| Managed transfer state | #31 §2.2, §3 | `transfers` | `Contract`, `TransferWindow`, `ClubTransferState`, `TransfersState` |"
        "\n| Transfer negotiation / manager command seam | #31 §2.2, §3.2–§3.3 | `transfers` | `Offer`, `NegotiationOutcome`, `TransferCommands` |"
        "\n| Transfer roster consumer port | #31 §4.5 | `transfers` | `ITransferRosterPort` |"
    )
    return s.replace(anchor, anchor + rows, 1)


def changelog(s):
    old = "> **Last Updated:** September 12, 2026 — **Unity now governs the build: 22 Unity-editor-only compile errors fixed that the Linux gate could not see. The Linux gate is kept.**"
    if old not in s:
        raise SystemExit("CHANGELOG head anchor missing")
    prior = old.replace("**Last Updated:**", "**Last Updated (prior):**", 1)
    entry = """> **Last Updated:** September 14, 2026 — **D5 / Transfers, Contracts & Negotiation #31 T0 implemented in PR #407; both C6-deferred football-judgment findings and both Codex P2 findings are closed.**
>
> New Tier-7 `TacticalDirector.Transfers` production/test assemblies provide the draw-free integer T0 core: attributes+age valuation with always-on #27 coarse-position scarcity; deterministic `Accepted` / `CounterOffered` / `Rejected` negotiation around a configurable near-value band; managed-club contract/window/committed-spend state; and atomic manager `SubmitBid` that consumes #40 only through `AvailableTransferBudget` / `ApplyTransaction` and delegates roster ownership to the #31-owned `ITransferRosterPort` for #30's T2 producer. Wages are recorded on contracts but not posted at T0. No autonomous transfer producer, save composition, season-loop call site or production roster adapter lands here; those remain T1/T2, with personality/CA/staff/wage/multi-day/rival-bid depth at T3.
>
> **C6 close-out.** The original one-currency-unit accept/reject cliff now has a synchronous deterministic `CounterOffered` band, and minimal valuation no longer becomes context-blind when deep producers are absent: #27 positional stock is always-on at Stage 2. The football-judgment ledger moves **34 recorded / 5 fixed / 29 open → 34 / 7 / 27**; its assembly-less subset moves **6 specs / 8 findings → 5 / 6**, leaving the workable queue at 21. Codex P2 #1 is closed by shared `Offer.ValidateTerms` plus a direct malformed-evaluator regression; P2 #2 is closed by a complete `src/transfers/` section in the authoritative file manifest.
>
> **Governance / verification.** Code Standards §3.5.2 seats all **37/37** production assembly folders with **0 upward references**; the corresponding CI run also passed **279/279 tooling tests**, markdown/YAML/link checks, C# whitespace, Unity meta integrity and asset hygiene. That run's only completed failure was the stale file-manifest maintained pointer to Code Standards v1.13; the pointer is now v1.14. Its long Linux functional job was still running when this record was written, so this entry does **not** promote that preliminary run to a full green gate. The final rebased/squashed PR head owns the merge verdict. Unity 6000.4.9f1 remains the governing compiler and has **not** been run for this #31 landing in this session.
>
> **Determinism:** no match snapshot/save schema, season-save format, RNG stream/domain tag, draw site or draw order changes in T0.

"""
    return s.replace(old, entry + prior, 1)


def changelog_src(s):
    old = "> **Last Updated:** September 12, 2026 (v2.134 — **Unity editor compile fixes; Unity governs the build.**"
    if old not in s:
        raise SystemExit("CHANGELOG-src head anchor missing")
    new_head = "> **Last Updated:** September 14, 2026 (v2.135 — **#31 Transfers T0 / D5.** New `src/transfers/` production/test assemblies and metas; `Contract`, `Offer`, `NegotiationOutcome`, `TransferWindow`, `ClubTransferState`, `PlayerValuation`, `NegotiationEngine`, `TransfersState`, `ITransferRosterPort`, `TransferCommands`, `TransfersConstants`, and `TransfersT0Tests`. Review corrections centralize malformed-offer validation, add the deterministic counter-offer band and always-on positional scarcity, and use an explicit `ClubFinancesState` alias for Unity/C# namespace-vs-type resolution. Code Standards seats `transfers` at Tier 7. No save/schema/RNG/domain/draw-order change; T1/T2/T3 remain deferred. Preliminary code-bearing CI: 37/37 assembly placement, 279 tooling tests and all completed non-hygiene jobs green; the sole completed red was a stale manifest version pointer already corrected, while the long Linux functional job remained in progress. Unity editor not run for this landing.)\n>\n> **Last Updated (prior):** September 12, 2026 (v2.134 — **Unity editor compile fixes; Unity governs the build.**"
    s = s.replace(old, new_head, 1)
    marker = "| Version | Date"
    pos = s.find(marker)
    if pos < 0:
        raise SystemExit("CHANGELOG-src version table header missing")
    line1 = s.find("\n", pos)
    line2 = s.find("\n", line1 + 1)
    if line1 < 0 or line2 < 0:
        raise SystemExit("CHANGELOG-src version table separator missing")
    row = "| 2.135 | Sep 14, 2026 | #31 Transfers T0 / D5: new `transfers` production/test assembly, deterministic positional-need valuation + counter-offer band, atomic fee-only manager command, shared malformed-offer validation, Tier-7 seating; T1/T2/T3 deferred; no save/schema/RNG/domain/draw-order change. |"
    if "| 2.135 |" in s:
        raise SystemExit("CHANGELOG-src v2.135 already present")
    return s[: line2 + 1] + row + "\n" + s[line2 + 1 :]


patch("docs/agent-guides/project-reference.md", project_reference)
patch("docs/tracking/data-contract-index.md", data_contract_index)
patch("docs/tracking/CHANGELOG.md", changelog)
patch("docs/tracking/CHANGELOG-src.md", changelog_src)
