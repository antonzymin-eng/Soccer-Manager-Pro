# Transfers, Contracts & Negotiation #31 — Section 5: Test Plan

**Created:** July 23, 2026
**Last Updated:** September 14, 2026 (v0.4 — T0 football-judgment/Codex regression locks)
**Last Updated (prior):** July 23, 2026 (v0.3 — AR-6 fix pass; prior v0.2 AR-3/AR-4, v0.1 initial)
**Version:** 0.4
**Status:** APPROVED

---

## 5.1 Unit — valuation & offer (KD-1)

- **T-TX-VAL-001** — `ValuePlayerPermille` remains a pure integer function of #27 attributes + age; two calls
  with equal inputs return equal values; no RNG parameter, no #33/#28 read.
- **T-TX-VAL-002** — `AgeCurvePermille` shape: peak band = `1000‰`; very-young and older decline bands do not
  exceed the peak.
- **T-TX-VAL-003** — `ClubNeedMultiplierPermille`: below-neutral positional stock > `1000‰`, the #27-derived
  neutral count == `1000‰`, overstock < `1000‰` but remains positive. This term is present at Stage 2 even
  when deep personality/staff/CA refinements are absent.
- **T-TX-OFF-001** — `EvaluateOffer` is draw-free and three-way: exact counterparty value accepts; a term inside
  `NEGOTIATION_COUNTER_BAND_PERMILLE` returns `CounterOffered`; a term beyond the band rejects, mirrored for
  buy and sell.
- **T-TX-OFF-002 (F6 / Codex P2)** — direct calls to the reusable evaluator reject malformed terms, including
  negative wage and non-positive length; validation is not only a `SubmitBid` responsibility.

## 5.2 Integration — the bid pipeline (KD-2/KD-7, atomic)

- **T-TX-BID-001** — an accepted fair-value buy posts exactly `{Debit,TransferFee,fee}` via `ApplyTransaction`
  (fee-only at minimal), moves `Balance` per FR-FN-016, adds `fee` to `committedSpendThisWindow`, and inserts a
  re-keyed `Contract` carrying the agreed `WagePerPeriod` (recorded, **not** posted to `WageBillAggregate`).
- **T-TX-BID-002** — a sell posts exactly `{Credit,TransferFee,fee}` and removes the `Contract`; the
  `RemoveContract` runs **before** the re-key, so no orphaned or double-handled contract survives under the new
  id.
- **T-TX-BID-003 (F1)** — a buy over `AvailableTransferBudget − committedSpendThisWindow` fails loud; two buys
  individually under the ceiling but cumulatively over it cause the second to fail before mutation.
- **T-TX-BID-004 (F2 atomicity)** — a full destination squad or failed affordability gate leaves
  `ClubFinances`, transfer state and roster untouched.
- **T-TX-BID-005** — #31 writes no `ClubFinances` field directly and holds no parallel cash ledger;
  `TransferBudget` is unchanged by `ApplyTransaction`.
- **T-TX-BID-006 (FR-FN-015 preserved)** — a minimal buy or sell leaves #40's `WageBillAggregate` unchanged.
- **T-TX-BID-007** — a `CounterOffered` or `Rejected` command performs no finance post, no spend accumulation,
  no roster commit and no contract mutation.

## 5.3 Roster re-key (KD-7)

- **T-TX-REKEY-001** — a committed transfer re-keys `PlayerId` to `toClub*CLUB_SQUAD_SIZE+freeLocalIndex`;
  #31's managed contract is inserted on buy or removed on sell; `OnPlayerRekeyed` is a no-op for managed↔AI.
- **T-TX-REKEY-002 (F5)** — a full destination `Squad` fails loud before any finance post.
- **T-TX-REKEY-003** — #31 migrates only its own contract state; #28/#33 state is not referenced by #31.
- **T-TX-REKEY-004** — the T0 roster port's positional-stock read is read-only and returns a count in
  `[0, CLUB_SQUAD_SIZE]`; an invalid producer count fails loud before offer evaluation.

## 5.4 Window (KD-6)

- **T-TX-WIN-001** — `IsWindowOpen` is inclusive at both boundaries and a `SubmitBid` outside the window fails
  loud before valuation or mutation.

## 5.5 Save round-trip & determinism (KD-4/KD-8)

- **T-TX-DET-001** — `Contract` + `ClubTransferState` restore field-identical across a mid-window save; load
  does not re-run genesis seeding.
- **T-TX-DET-002** — contracts survive a season boundary; lengths decrement, would-be zero contracts are
  removed, and window/spend state resets.
- **T-TX-INIT-001** — career genesis seeds one valid managed-club contract per #27 player; AI clubs are not
  seeded at minimal.
- **T-TX-DET-003** — two runs over the same transfer activity produce identical transfer state.
- **T-TX-NEU-001** — no manager transfer action leaves pre-#31 season behavior unchanged; #31 registers no RNG
  stream.
- **T-TX-INT-001** — transfer state/valuation/spend fields are integer; no float/double state is introduced.
- **T-TX-SHAPE-001** — the serialized transfer block contains no RNG cursor/action ordinal.

## 5.6 Fail-loud (F1..F8)

- **T-TX-FAIL-001 (F3)** — bad transfer save version / out-of-bounds length / trailing bytes throw at decode.
- **T-TX-FAIL-002 (F6)** — invalid player id, negative fee/wage or non-positive length throws at every relevant
  consuming seam.
- **T-TX-FAIL-003 (F7)** — `default(Contract)` fails insertion validation.

## 5.7 Requirement traceability

Every FR-TX-001..028 maps to a T-TX-* test above or a recorded §7 deferral. The Stage-2 positional-need and
counter-offer behavior is covered now; deep personality/CA/staff, multi-day state, wage economics and stochastic
rival bidding remain deferred.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial §5 (valuation/offer units, atomic bid integration, roster re-key, window, save/determinism/neutrality, fail-loud, traceability). Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-3: T-TX-BID-001/002 fee-only + sell no-orphan lock; new T-TX-BID-006 (FR-FN-015 `WageBillAggregate` unchanged); T-TX-VAL-001/003 drop club-need / add `needMult` identity; T-TX-DET-002 covers decrement-and-remove aging. AR-4: new T-TX-INIT-001 (career-start contract seeding, §3.8 — M). |
| 0.3 | 2026-07-23 | — | AR-6: T-TX-REKEY-001 corrected to the fixed insert/remove-via-`SubmitBid` model (`OnPlayerRekeyed` no-op — was still asserting the AR-3-removed hook-move, M); T-TX-DET-001 locks load-decodes-not-reseeds (M); T-TX-REKEY-003 "in the hook" → "on a re-key" (L). |
| 0.4 | 2026-09-14 | — | T0 close-out: adds positional-scarcity lock, accepted/counter/rejected band lock, direct malformed-evaluator regression for Codex P2, and explicit counter/reject no-mutation coverage. |
#endregion
