# Transfers, Contracts & Negotiation #31 — Outline

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.2 — AR-3 fix pass; prior v0.1 initial)
**Version:** 0.2
**Status:** APPROVED

---

## Purpose

The recruitment engine: transfer windows, player search over #27's pool, bids, contract terms
(wage/length, deep clauses), and a counterparty negotiation loop — advanced on the world tick, constrained by
#40's club budgets, committed back through #30's roster owner. Minimal = master plan §4.3 accept/reject inside
a summer window against a deterministic counterparty valuation; the deep tier reads #33 personality and adds
agents/clauses/loans/wage-structures/rival bidding on one code path.

## Section map

| Section | Content |
|---------|---------|
| 1 | Introduction, scope, dependencies, key decisions (KD-1..KD-8) |
| 2 | Functional requirements (FR-TX-001..028), data structures, failure modes (F1..F8) |
| 3 | Core algorithms: valuation, offer evaluation, the #40-boundary commit, window model, roster re-key |
| 4 | Architecture, assembly/file layout, the reusable negotiation seam, save composition |
| 5 | Test plan (unit + integration + determinism + fail-loud) |
| 6 | Performance analysis and budgets |
| 7 | Future extensions and T-phase plan (T0–T3) |
| 8 | References and cross-spec cross-references (XC-031-*) |
| 9 | Approval checklist |
| Appendices | Constant catalogue, save-block layout, worked valuation example |

## Governing decisions (see §1)

- **KD-1** — the Stage-2 valuation is a pure deterministic integer function over #27 attributes + age;
  club-need, #33 personality, and #28 CA all enter the deep tier as multiplicative bias, never a replacement
  path (each defaulting to `1000‰` / the #27 mean identity with `deepTransfersEnabled` off).
- **KD-2** — #40 boundary: read `AvailableTransferBudget`; commit via `ApplyTransaction`; #31 owns a
  `committedSpendThisWindow` counter (FR-FN-004 gives #40 no such concept); no parallel cash ledger; validate
  all gates before any mutation (atomic commit).
- **KD-3** — the reusable offer/response seam authored once for #32/#34 (counterparty-generic).
- **KD-4** — one `TRANSFERS_SAVE_FORMAT_VERSION` season-save sub-blob for durable contracts + season-scoped
  window/negotiation/spend; no `WORLD_STORE_FORMAT_VERSION` bump.
- **KD-5** — draw-free minimal; `_RESERVED_0x23_`/85 stays reserved; rival bidding is the deep-tier first draw.
- **KD-6** — the transfer-window model is #31-owned (a concept #30 lacks), derived from #30's `SeasonCalendar`.
- **KD-7** — a transfer re-keys the club-scoped `PlayerId` through a NEW #30 mid-season roster-commit entry
  point + roster-move hook; #31 migrates only its own `Contract`.
- **KD-8** — behaviour-neutral: zero manager action ⇒ zero transfers ⇒ byte-identical season; a bid is an
  explicit command (the `SetTeamTactic` discipline).

## Back-props

- **At approval:** one — the #30 transfers tick-order null-seam slot (ERR-030-004, proposed). `0x23`/85 stays
  reserved (draw-free); #40/#33/#27/#16 unchanged.
- **At T-phase:** the #30 outer `SEASON_SAVE_FORMAT_VERSION` bump + roster-commit/re-key seam; the #16
  `0x23` promotion at the deep-tier first draw.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial outline from design supplement v0.2. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-3: KD-1 folds club-need into the deep multiplicative bias (minimal = attributes+age). |
#endregion
