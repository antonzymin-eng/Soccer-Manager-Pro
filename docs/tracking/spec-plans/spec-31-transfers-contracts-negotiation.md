# Spec #31 — Transfers, Contracts & Negotiation — High-Level Plan

> **Created:** July 22, 2026
> **Status:** PLAN (pre-design-supplement — no section files, no `SPEC_INDEX.md` row). Candidate spec number **#31** (proposed in `management-layer-spec-roadmap.md`, not reserved).
> **Master-plan home:** §4.3, §5 (complex clauses) · **Tier:** S2 min → S3 deep · **Wave:** 4 · **FR prefix (proposed):** FR-TX
> **Determinism:** domain tag `0x23` / SubsystemOrdinal 85 (proposed off-pitch block, §6 — pinned only at promotion)
> **Purpose:** Transfer windows, player search, bids, contracts (wage/length/clauses), and negotiation against club budgets.

## 1. Scope
The recruitment engine: transfer windows, player search over #27's pool, bids, contract terms (wage/length/clauses), and a negotiation loop with a counterparty. Minimal = master plan §4.3 accept/reject within a summer window; deep = agents, clauses, loans, and wage structures. **Out of scope:** the economy (#40 owns budgets/wages as the constraint #31 reads); scouting/fog-of-war (#32 reuses #31's negotiation machinery but owns knowledge); counterparty personality (#33 supplies the psychology #31's valuation later reads); the on-disk save codec (#30 owns the season format #31 state lands in).

## 2. Staging (minimal-first → deep)
Minimal identity = counterparty behaviour is a **deterministic valuation function** (player value + club need → accept/reject) inside a summer window — no agents, no clauses. The S3 deep tier has that valuation **read #33 personality** and adds agents/clauses/loans/wage structures **modulating the same valuation identity**, one code path (the §4.3 valuation is the identity the personality layer later modulates — roadmap §3.31 KD). A #33-unconfigured negotiation yields exactly the deterministic minimal valuation.

## 3. Dependencies
- **Upstream (needs):** #27 (player pool + valuation attribute inputs), #30 (transfer-window calendar, day-advance loop, season save root), #40 (budget/wage constraint — the counterparty and self spending ceiling), #33 (negotiation counterparty psychology, at S3+), #34 (staff influence on valuation/negotiation, optional).
- **Downstream (consumers):** #32 (scouting **reuses** the negotiation machinery for bids on scouted players), #30 (roster changes committed into the season/world state).

## 4. Persistent state & save impact
New transfer/contract world-and-season state: active contracts (wage/length/clauses), in-flight negotiations, window cursor, transfer history. Contract terms persist across seasons (world store); in-flight negotiation and window cursor are season state. Coordinated version bump — `SEASON_SAVE_FORMAT_VERSION` for the season-scoped negotiation/window state and/or `WORLD_STORE_FORMAT_VERSION` for durable contracts — each as an opaque, independently version-gated sub-blob per the `SeasonSaveCodec` never-parse pattern. Round-trip determinism required, including a mid-negotiation save.

## 5. Determinism
World tick (`WorldClock`) drives window open/close and day-by-day negotiation progress (never the match tick). Dedicated RNG sub-stream (domain tag `0x23` / `SubsystemOrdinals` 85, proposed) for any stochastic counterparty behaviour (rival bids, agent demands); the minimal §4.3 valuation is a pure function with zero draw, keeping the stream dormant until the deep tier. Allocation pinned in #16 §3.4 at promotion.

## 6. Key design decisions to resolve (the supplement must answer)
- **KD-1** Author the Stage-2 valuation as the exact identity the #33 personality layer modulates (roadmap §3.31) — what is the valuation's input vector and how does personality enter as a multiplier/bias rather than a replacement path?
- **KD-2** The #40 boundary: #31 reads budgets/wages as a read-only constraint and commits accepted deals back — where is the write seam, and does it go through #40's ledger or #30's roster-commit? (Avoid two-way coupling per roadmap §5.)
- **KD-3** Negotiation-machinery reuse: what is the shared surface #32 (scouting bids) and #34 (staff hiring, if it reuses per its own KD) consume, so it is authored once?
- **KD-4** Clause/loan/wage-structure representation (deep tier) — how it serializes as durable contract state without a minimal-tier schema rewrite.
- **KD-5** Rival-AI-club bidding — does it draw from the #31 stream at S2 or defer entirely to S3 (keeping the minimal tier a pure single-counterparty function)?

## 7. Primary surfaces (proposed)
- A negotiation loop / offer-response surface (proposed) — the reusable seam #32 and possibly #34 consume (KD-3).
- Deterministic counterparty valuation function (proposed) — the minimal identity.
- Contract state (wage/length/clauses) block (proposed) in durable world state.
- Transfer-action command APIs (proposed) — the public seams the #38 UI drives (mirroring the engine's existing `SetTeamTactic`-style command discipline; UI never mutates directly).

## 8. Test focus
Behaviour-neutral identity: a #33-unconfigured negotiation reproduces the deterministic §4.3 valuation exactly. Round-trip determinism of transfer/contract state through the season + world save, including a mid-negotiation and mid-window save. Two-run determinism of a full window's transfer activity from a fixed world seed. Fail-loud gates on over-budget bid / malformed contract / negotiation action outside a window / bid on an unlisted-in-pool player.

## 9. Open questions / risks
- The #40/#33 dependency order (roadmap §7 Wave 4, critical path #33 → #31) — #31 phantoms if authored before #40 economy and #33 psychology exist.
- The reusable negotiation surface (KD-3) is load-bearing for #32/#34 — a poorly-factored seam forces duplication downstream.
- Save-scope split (season vs. world) for negotiation-vs-contract state (KD-4) must not double-serialize or desync across the two version gates.

## Version History
| Version | Date | Change |
|---------|------|--------|
| v0.1 | July 22, 2026 | Initial high-level plan. |
