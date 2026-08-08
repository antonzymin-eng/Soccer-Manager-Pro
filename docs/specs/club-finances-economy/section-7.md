# Club Finances & Economy #40 — Section 7: Future Extensions & T-Phase Plan

**Created:** July 23, 2026
**Last Updated:** August 8, 2026 (v0.2 — balance-pass AR pass 8 M2: the §7 precedent citation renamed off the phantom stream)
**Last Updated (prior):** July 23, 2026 (v0.2 — AR-1 wage-semantics fix; prior v0.1 initial)
**Version:** 0.2
**Status:** APPROVED

---

## 7.1 T-phase implementation plan (post-APPROVED)

- **T0** — `TacticalDirector.ClubFinances` assembly: value types (`FinanceTransactionKind`, `FinanceLineItem`,
  `ClubFinances`, `FinanceTransaction`, `BoardModifier`, `FinancesViewModel`), the deterministic
  `SettleFinances` + `PrizeMoneyForPosition` + `ApplyTransaction` + `AvailableTransferBudget`,
  `ClubFinancesConstants`. Behaviour-neutral by construction (KD-8).
- **T1** — `ClubFinancesSaveCodec` (`FINANCE_SAVE_FORMAT_VERSION` = 1) + composition into #30's season save
  (the `SeasonSaveCodec` sub-blob; #30's composing format-version bump coordinated here). Fail-loud gates.
- **T2** — Wire `SettleFinances` at #30's **new** reserved step (b') (after the (a') #43 insertion point,
  before (c) regenerate, the ERR-030-003 back-prop); wire `CreateInitial` at league/game bootstrap for every
  `ClubId` (#30-driven, not #40-driven); expose `AvailableTransferBudget`/`ApplyTransaction` for #31/#34/#42
  to call once those specs exist. No #30 tick-order change beyond the KD-6 back-prop already filed (KD-6).
- **T3** — Deep tier: per-day revenue accrual (matchday/sponsorship, a new daily #30 tick-order slot — the
  #41 pattern), the stochastic sponsorship-variance draw (promotes `DOMAIN_TAG_CLUB_FINANCES = 0x29` /
  `SubsystemOrdinals.ClubFinances = 91`, keyed on `(clubId, seasonNumber, purpose)`), the FFP soft-penalty
  modulating the next season's projected budget, non-identity `BoardModifier` consumption when #45 lands, and
  non-identity wage-ledger producers when #31/#34 land — all defaulting to their Stage-2 identities via a
  config dial (one code path, KD-4/KD-8).

## 7.2 Deferred (recorded, not built)

- **Per-day revenue accrual.** Stage-2 settles once per season; a Stage-3 daily accrual (matchday attendance
  revenue, sponsorship instalments) would need a new daily #30 tick-order slot analogous to #41's
  `AdvanceMedicalDay` insertion — not built here (KD-1).
- **Periodic wage cash-out.** Stage-2 `ApplyTransaction` records a wage as a change to the liability
  `WageBillAggregate` only (never `Balance`, §3.2/FR-FN-016). The periodic (weekly/monthly) *payment* of that
  wage bill — the step that actually debits `Balance` from `WageBillAggregate` — is a deep-tier accrual on
  the same future daily slot as revenue accrual; not built here, so at Stage 2 the wage bill is a liability
  figure that never drains cash.
- **Stochastic sponsorship/revenue variance.** The genuine first draw site on the reserved
  `_RESERVED_0x29_`/91 namespace slot; promotes the tag only when this lands (KD-2).
- **FFP soft-penalty.** A deep-tier adjustment to the *next* season's projected budget, composing
  multiplicatively with `BoardModifier`; defaults to "no penalty" at Stage 2 (KD-4).
- **Non-identity `BoardModifier` (#45).** #45 becomes the producer of a real board-driven multiplier
  (takeover windfalls, confidence-linked budget adjustments); no #45 interface is built ahead of that
  (FR-LW-031).
- **Non-identity wage-ledger producers (#31/#34).** Player-contract and staff-contract wage line items via
  `ApplyTransaction`; the ledger structure exists today, empty (KD-5).
- **A "remaining budget net of season spend" running total.** Stage 2's `TransferBudget`/`WageBudget` are
  static per-season ceilings, not decremented by `ApplyTransaction` (§1.6); a deep-tier extension could add
  a derived "remaining" field if #31 needs it, without changing the ceiling semantics.
- **A genuinely stochastic FFP/board-confidence interaction** beyond the fixed multiplicative composition. If
  a later extension needs this, it composes as an additional multiplicative term or an additional keyed draw
  purpose on the same T3 stream — no second stream is needed, since the stream is already keyed per-purpose
  (the #41 keyed-derivation append-only-purpose precedent — ERR-041-012: no stream).

## 7.3 Seam contracts recorded for downstream authors

- **#31 (transfer market):** becomes the caller of `AvailableTransferBudget`/`ApplyTransaction`. #31 MUST NOT
  write `ClubFinances` fields directly, MUST NOT maintain a parallel budget/ledger total, and MUST NOT expect
  `TransferBudget`/`WageBudget` to decrement as `ApplyTransaction` calls accumulate (they are season
  constants set by `SettleFinances`, §1.6).
- **#34 (staff, future):** becomes a second caller of `ApplyTransaction` (`LineItem = StaffWage`) — the same
  contract as #31's wage line items; #34 MUST NOT add a second wage-aggregation path.
- **#45 (board & ownership, future):** becomes the producer of a non-identity `BoardModifier`. #45 MUST
  supply a **non-zero** `BudgetMultiplierMillPermille` — `default(BoardModifier)` reaching `SettleFinances`
  fails loud by design (FR-FN-018); #45 MUST NOT add a second budget-multiplier path.
- **#43 (promotion/relegation, future):** when it lands, its transform inserted at #30's step (a') produces
  the post-promotion division/`finalTablePosition` #40's step (b') already reads — no #40-side change is
  needed (the KD-6 ordering rationale is written to anticipate this); #43 MUST NOT itself call
  `SettleFinances` or otherwise reach into #40.
- **#30 (season loop):** owns `SettleFinances` invocation timing (KD-6) and the one-time
  `ClubFinances.CreateInitial` bootstrap per club; #40 MUST NOT reference #30 or drive its own club-bootstrap
  independently (the one-way composition, FR-FN-027).
- **#27 (squad/player data):** the `Squad.ClubId` enumeration #40 reads for F6's club-universe check MUST
  remain the authoritative club-identity source; #40 MUST NOT gain a second, competing club-identity notion.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial T-phase plan (T0–T3) + deferred extensions + downstream seam contracts. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-1 (1M): §7.2 records the deferred periodic wage cash-out (the step that debits `Balance` from `WageBillAggregate`). |
| 0.2 | 2026-08-08 | — | **ERR-041-012 back-prop**: the append-only-purpose precedent citation renamed — #41 has a keyed derivation, not an `injuries.occurrence` stream. |
#endregion
