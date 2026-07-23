# Transfers, Contracts & Negotiation #31 — Section 1: Introduction

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.2 — AR-3 fix pass; prior v0.1 initial)
**Version:** 0.2
**Status:** APPROVED

---

## 1.1 Scope

The **recruitment engine**: transfer windows, player search over #27's canonical pool, bids, contract terms
(wage/length at minimal; agents/clauses/loans/wage-structures at the deep tier), and a **counterparty
negotiation loop**. All #31 state advances on the **world tick** (`WorldClock`, one day = one `worldTick` —
never the 10 Hz/60 Hz match loops, living-world KD-4), is constrained by **#40's club budgets**, and is
committed back through **#30's roster owner**.

**Minimal (Stage 2)** = the master plan §4.3 model: **accept/reject inside a summer window** against a
**deterministic counterparty valuation**, both directions **manager-initiated** (buy and sell), no agents, no
clauses, no multi-day negotiation, **no autonomous AI-club bidding**, and **no wage-economy posting** — the
negotiated wage is recorded on the `Contract`, but posting it to #40's wage bill is a deep-tier concern,
preserving #40 FR-FN-015 (`WageBillAggregate ≡ 0` at Stage 2). **Deep (Stage 3)** = the same valuation
**identity** reads #33 personality (a multiplicative bias) and adds agents, clauses, loans, wage structures
(incl. the wage-bill producer + a `WageBudget` gate), multi-day negotiation, and stochastic rival bidding —
**one code path**, each deep feature defaulting to its Stage-2 identity via `deepTransfersEnabled`.

## 1.2 Out of scope (owned elsewhere, referenced as seams)

- **The economy (#40 Club Finances).** #40 owns budgets/wages: #31 **reads** `AvailableTransferBudget` and
  **commits** accepted deals through `ApplyTransaction` (the single #40-owned mutation path). #31 never writes
  `ClubFinances` fields directly and keeps no parallel cash ledger (KD-2).
- **Scouting / fog-of-war (#32).** #32 **reuses** #31's negotiation seam (KD-3) for bids on scouted players
  but owns the per-manager knowledge view. #31 builds no scouting.
- **Counterparty personality (#33).** #33 supplies `MoraleOf` (granted to #31 by #33 §7.3) — **read-only,
  deferred**; the `PersonalityProfile` trait read surface the deep `personalityMult` also needs is not yet
  granted (a T3 #33 back-prop, §7.3/§8.3). Minimal makes **no** #33 read; personality is the deep-tier
  modulation of the valuation identity (KD-1).
- **The on-disk season codec + roster ownership + tick order (#30).** #30 owns `SeasonSaveCodec` /
  `SEASON_SAVE_FORMAT_VERSION`, `RunWorldTickInFixedOrder`, and the roster lifecycle
  (`SeasonLoop`/`SeasonState`, `RollToNextSeason`). #30 **invokes** #31 at a new pre-declared tick-order slot
  and owns the roster-commit + PlayerId **re-key** a transfer triggers. #31 never references #30 (KD-6/KD-7).
- **CA/PA (#28).** The current/potential-ability career-state keyed by `PlayerId` is #28-owned; the minimal
  valuation does **not** read it (a recorded deep-tier refinement, KD-1).

## 1.3 Dependencies

**Upstream (needs):** #27 (player pool + valuation attribute inputs), #30 (transfer-window calendar,
day-advance loop, season-save root, roster commit), #40 (budget/wage constraint + commit path). **Deep-tier
upstream:** #33 (counterparty psychology), #28 (CA/PA valuation refinement), #34 (staff influence — a ×1.0
identity routing seam until #34 exists).

**Downstream (consumers, deferred — no interface built, FR-LW-031):** #32 (scouting reuses the negotiation
seam), #34 (staff hiring may reuse it), #30 (roster changes committed into season/world state), #38 (UI drives
the transfer-action command APIs).

Reference DAG: `compositionRoot → {#30, #31}`, `#31 → {#40, #27, #16}`. **Acyclic.**

## 1.4 Key decisions

- **KD-1 (valuation as the identity the deep tier modulates).** The Stage-2 counterparty valuation is a **pure
  deterministic integer function** over #27 attributes + age (§3.1). **Club-need, #33 personality, and #28 CA
  all** enter the **deep tier as a multiplicative bias** on this identity, **never a replacement path**; with
  `deepTransfersEnabled` off every bias is exactly `1000‰` (`needMult`, `personalityMult`, CA-swap off), so the
  negotiation reproduces the attributes+age valuation exactly.
- **KD-2 (the #40 boundary — read + commit + own the spend counter + atomic commit).** #31 reads
  `AvailableTransferBudget` (returns the static `TransferBudget` ceiling) and posts accepted deals via
  `ApplyTransaction`. Because `ApplyTransaction` never decrements the ceiling and **FR-FN-004 gives #40 no
  "remaining budget net of committed" concept**, #31 keeps its own `committedSpendThisWindow` counter for the
  affordability gate. Every gate is validated **before any mutation** (atomic commit — no half-written deal).
  Wage posting to #40's `WageBillAggregate` is **deferred to the deep tier** — minimal posts only the
  `TransferFee`, so #40 FR-FN-015 (`WageBillAggregate ≡ 0` at Stage 2, "no #31/#34 producer exists yet") is
  preserved verbatim; the deep-tier wage producer lands with a #40 back-prop relaxing FR-FN-015 + a `WageBudget`
  affordability gate (§7).
- **KD-3 (the reusable negotiation seam).** The offer/response surface (`Offer`, `NegotiationOutcome`,
  `EvaluateOffer`) is authored once, **counterparty-generic**, so #32 (scouting bids) and #34 (staff hiring)
  consume it without duplication.
- **KD-4 (persistence — one season-save sub-blob).** `TRANSFERS_SAVE_FORMAT_VERSION` [FIXED] = 1, an opaque
  independently version-gated sub-blob composed into `SeasonSaveCodec`, holding **both** durable contracts and
  season-scoped window/negotiation/spend state — **not** a `WORLD_STORE_FORMAT_VERSION` bump (the #40 `Balance`
  season-save precedent).
- **KD-5 (determinism — draw-free minimal).** Minimal makes no stochastic draw; `_RESERVED_0x23_` / ordinal 85
  **stays reserved** (the #40 ERR-040-001 / #29 precedent). Rival bidding is the deep-tier first draw
  (promotes `DOMAIN_TAG_TRANSFERS = 0x23` at T3, keyed on `(clubId, playerId, worldDay, purpose)`).
- **KD-6 (the transfer-window model — #31-owned).** #30 has no window concept; #31 owns a `TransferWindow`
  derived deterministically from #30's `SeasonCalendar` (read-only). Minimal = one summer window.
- **KD-7 (roster commit + PlayerId re-key — a NEW #30 mid-season entry point).** A transfer re-keys the
  club-scoped `PlayerId` (`clubId*CLUB_SQUAD_SIZE+localIndex`). #31 **requests** a commit through a new
  #30-owned mid-season roster-mutation entry point that re-keys the player and dispatches a roster-move hook;
  #31 migrates only its own `Contract` (#28/#33 migrate their own keyed state).
- **KD-8 (behaviour-neutral identity).** Zero manager action ⇒ zero transfers (no autonomous producer at
  minimal) ⇒ a season byte-identical to pre-#31; registering no stream keeps every cursor byte-identical. A
  manager bid is an explicit command (the `SetTeamTactic` discipline), not a neutrality violation.

## 1.5 Determinism & coordinate posture

All arithmetic is **integer** (currency `long`; valuation/club-need per-mille `int`). There is **no float in
#31** — it exchanges only integer amounts with #40. All state advances on the world clock at #30's pre-declared
slot; minimal is draw-free (KD-5). This is the #40/#41/#28 off-pitch integer + world-tick posture.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial §1 (scope, out-of-scope seams, dependencies, KD-1..KD-8, determinism posture). Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-3 (H): wage posting deferred to the deep tier (minimal fee-only, preserves #40 FR-FN-015) — §1.1/KD-2; KD-1 folds club-need into the deep multiplicative bias (minimal = attributes+age). |
#endregion
