# Transfers, Contracts & Negotiation #31 — Section 7: Future Extensions & T-Phase Plan

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.2 — AR-3 fix pass; prior v0.1 initial)
**Version:** 0.2
**Status:** APPROVED

---

## 7.1 T-phase implementation plan (post-APPROVED)

- **T0** — `TacticalDirector.Transfers` assembly: value types (`Contract`, `Offer`, `NegotiationOutcome`,
  `TransferWindow`, `ClubTransferState`, `TransfersState`), the deterministic `ValuePlayerPermille` /
  `EvaluateOffer` / `IsWindowOpen`, `SubmitBid` (the atomic validate-all-first pipeline), `TransfersConstants`.
  Behaviour-neutral by construction (KD-8 — no autonomous producer; a bid is a manager command).
- **T1** — `TransfersSaveCodec` (`TRANSFERS_SAVE_FORMAT_VERSION` = 1) + composition into #30's season save
  (the `SeasonSaveCodec` sub-blob; #30's outer `SEASON_SAVE_FORMAT_VERSION` bump coordinated here — exact
  version TBD, §4.4). Fail-loud gates (F3).
- **T2** — Wire the world-tick step at #30's **new transfers slot** (ERR-030-004, declared at approval — §8);
  **build the #30 mid-season `RequestRosterCommit` entry point + `DispatchRosterMoveHook`** (KD-7 — a new #30
  capability; #28/#33 subscribe their own keyed migration; recorded ERR-030-005/T2 in #30). Expose the
  read-only transfer/contract accessors later consumers need. **No RNG stream registered (draw-free).**
- **T3** — Deep tier (each defaulting to its Stage-2 identity via `deepTransfersEnabled`): the **club-need
  signal** (`needMult`, positional scarcity — the first deep multiplicative bias on the identity); the **#33
  personality-modulated valuation** (`personalityMult` — requires a #33 back-prop for the trait read surface,
  §7.3); the **#28 CA/PA valuation refinement**; the **wage-bill producer** (the deferred
  `{Debit/Credit,PlayerWage,…}` posts + a `WageBudget` affordability gate, landing with a #40 back-prop relaxing
  FR-FN-015); **clauses / loans / wage-structures** (appended `Contract` fields); **multi-day in-flight
  negotiation** (the tick-order slot fills here); **stochastic rival-AI-club bidding** (the first draw site —
  promotes `DOMAIN_TAG_TRANSFERS = 0x23` / `SubsystemOrdinals.Transfers = 85`, spec-text-first, ERR-016, keyed
  on `(clubId, playerId, worldDay, purpose)`); and the **#34 staff-influence seam** (a non-identity `staffMult`
  producer).

## 7.2 Deferred (recorded, not built)

- **Autonomous AI-club bidding.** Minimal is manager-initiated only; AI clubs proactively bidding without a
  prompt needs the daily tick + stochastic target selection — the deep-tier first draw (KD-5). The tick-order
  slot is declared now (reserve-ahead) but empty until this lands.
- **Wage-bill economy.** Minimal posts **only** the transfer fee (FR-TX-005); the `PlayerWage` posts + a
  `WageBudget` affordability gate are deep-tier, landing with a #40 back-prop relaxing FR-FN-015
  (`WageBillAggregate ≡ 0` at Stage 2, "no #31 producer yet"). The negotiated wage is recorded on the
  `Contract` meanwhile — durable, just not yet reflected in #40's wage liability.
- **Club-need signal.** Minimal valuation is attributes+age only; `needMult` (positional scarcity) is a deep
  multiplicative bias on the identity, defaulting to `1000‰` (KD-1).
- **Contract free-agency / renewal / expiry warnings.** §3.7 removes an expired contract (the player becomes
  un-contracted); re-signing, free-agency, and expiry-warning flows are deep.
- **Agents / clauses / loans / wage structures.** Contracts carry wage + length at minimal; these are appended
  `Contract` fields behind `deepTransfersEnabled` (FR-TX-015).
- **The #33 personality-modulated valuation.** Minimal valuation is #27-attributes-only; personality is a
  multiplicative bias added when #33's read accessors are consumed (KD-1). #33 §7.3 already names #31 a
  read-only consumer.
- **CA/PA-from-#28 valuation refinement.** Minimal rates on the #27 attribute mean; swapping in #28's CA is a
  deep-tier input change on the same identity (no minimal #28 dependency).
- **The #34 staff influence.** A `staffMult` routing seam defaulting to `1000‰` until #34 produces a value.
- **Indexed/cached player search.** Minimal search is a linear scan; an index is a deep-tier performance
  extension.

## 7.3 Seam contracts recorded for downstream authors

- **#40 (Club Finances):** #31 reads `AvailableTransferBudget` (`→ long`, the static `TransferBudget` field)
  read-only and posts **only** through `ApplyTransaction`; it MUST NOT write `ClubFinances` fields or hold a
  parallel cash ledger, and MUST own its own `committedSpendThisWindow` (FR-FN-004 gives #40 no such concept).
  At minimal #31 posts **only** the `TransferFee` — it is **not** a wage producer, so #40's FR-FN-015
  (`WageBillAggregate ≡ 0` at Stage 2) is preserved verbatim and **no #40 back-prop is needed at approval**.
  The deep-tier `PlayerWage` producer + a `WageBudget` affordability gate (which #40 exposes as a read for
  #31/#34 but wires no gate for) land together with a #40 back-prop relaxing FR-FN-015.
- **#30 (season loop):** owns the world-tick slot timing, the season-save composition, and the **new
  mid-season `RequestRosterCommit` entry point + roster-move hook** (KD-7). #30 stays producer-only for #22
  (FR-SN-017). #31 MUST NOT reference #30.
- **#27 (squad/player data):** `Squad.ClubId` / `PlayerId = clubId*CLUB_SQUAD_SIZE+localIndex` is the
  authoritative identity; a transfer **re-keys** through #30's roster owner, never by #31 mutating #27
  directly. #31 MUST NOT gain a competing identity notion.
- **#32 (scouting, future) / #34 (staff, future):** consume the KD-3 offer/response seam with their own
  counterparty-valuation inputs; #31 builds no interface for them (FR-LW-031). #34 additionally becomes the
  `staffMult` producer.
- **#33 (personalities, future):** the deep-tier valuation reads #33 **read-only** (never writes #33 state).
  Today #33 §7.3 exposes only `MoraleOf` to #31; the personality-**trait** read surface (`PersonalityProfile`
  loyalty/ambition) the deep `personalityMult` needs is **not yet granted** — it requires a #33 back-prop at
  #31 T3 (recorded, not built). #33 is a producer #31 consumes, not the reverse.
- **#38 (UI, future):** drives the transfer-action command APIs (`SubmitBid` etc.); MUST NOT mutate #31 state
  directly (the `SetTeamTactic` command discipline).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial T-phase plan (T0–T3) + deferred extensions + downstream seam contracts. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-3: T3/§7.2 add the deferred wage-bill producer + `WageBudget` gate + #40 FR-FN-015 back-prop (H), the deep club-need signal, and contract free-agency; §7.3 #40 seam corrected (minimal is fee-only, no back-prop at approval) + #33 seam notes only `MoraleOf` is granted, `PersonalityProfile` needs a T3 back-prop (L). |
#endregion
