# Transfers, Contracts & Negotiation #31 — Section 5: Test Plan

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.1 — initial)
**Version:** 0.1
**Status:** APPROVED

---

## 5.1 Unit — valuation & offer (KD-1)

- **T-TX-VAL-001** — `ValuePlayerPermille` is a pure integer function of #27 attributes + age + club-need; two
  calls with equal inputs return equal values; no RNG parameter, no #33/#28 read (reflection/static assertion).
- **T-TX-VAL-002** — `AgeCurvePermille` monotone shape: a peak-band age ≥ a decline-band age (older) and ≥ the
  very-young discount; the peak band = `1000‰` neutral.
- **T-TX-VAL-003** — deep identity: with `deepTransfersEnabled` off, `personalityMult ≡ 1000‰` and CA-swap off
  ⇒ `value_deep == value` exactly (FR-TX-002).
- **T-TX-OFF-001** — `EvaluateOffer` accept/reject boundary: a buy accepts iff `fee ≥ cv`, a sell iff
  `fee ≤ cv`; the boundary (`fee == cv`) accepts; draw-free (FR-TX-003).

## 5.2 Integration — the bid pipeline (KD-2/KD-7, atomic)

- **T-TX-BID-001** — an accepted fair-value buy posts exactly `{Debit,TransferFee,fee}` + `{Debit,PlayerWage,
  inWage}` via `ApplyTransaction`, moves `Balance`/`WageBillAggregate` per FR-FN-016, adds `fee` to
  `committedSpendThisWindow`, and inserts a re-keyed `Contract`.
- **T-TX-BID-002** — a sell posts the `Credit` pair and removes the `Contract`.
- **T-TX-BID-003 (F1)** — a buy over `AvailableTransferBudget − committedSpendThisWindow` **fails loud**; two
  buys each under the ceiling but summing over it: the second fails loud (the counter enforces the static
  ceiling).
- **T-TX-BID-004 (F2 atomicity)** — a full destination squad (F5) or failed affordability gate leaves
  `ClubFinances` **and** the roster **untouched** — no `ApplyTransaction` fired, no `Contract` inserted (the
  no-half-written-deal lock).
- **T-TX-BID-005** — #31 writes no `ClubFinances` field directly and holds no parallel cash ledger
  (static/reflection assertion on the #40 boundary); `TransferBudget` is unchanged by any `ApplyTransaction`.

## 5.3 Roster re-key (KD-7)

- **T-TX-REKEY-001** — a committed transfer re-keys `PlayerId` to `toClub*CLUB_SQUAD_SIZE+freeLocalIndex` and
  moves the `Contract` from old→new id via `OnPlayerRekeyed` with no orphaned/duplicated contract.
- **T-TX-REKEY-002 (F5)** — a full destination `Squad` fails loud at `AllocateFreeLocalIndex` before any
  finance post.
- **T-TX-REKEY-003** — #31 migrates only its own `Contract` in the hook; the #28/#33 migrations are dispatched
  by #30, not by #31 (assert #31 has no reference to #28/#33 state).

## 5.4 Window (KD-6)

- **T-TX-WIN-001** — `IsWindowOpen` is a deterministic predicate over #30's `SeasonCalendar`; a `SubmitBid`
  outside the window **fails loud** (F4); minimal = one summer window `[open, close]`.

## 5.5 Save round-trip & determinism (KD-4/KD-8)

- **T-TX-DET-001** — `Contract` + `ClubTransferState` (window cursor + `committedSpendThisWindow`) restore
  **field-identical** across a mid-window save.
- **T-TX-DET-002** — contracts survive a `RollToNextSeason` boundary; the window cursor + spend counter reset;
  a retired/regenerated `PlayerId`'s contract is removed/inserted in lockstep with #28.
- **T-TX-DET-003** — two-run determinism: a full window's transfer activity from a fixed world seed produces
  a byte-identical `TransfersState`.
- **T-TX-NEU-001 (behaviour-neutral, KD-8)** — a season with **no** manager transfer action advances
  byte-identical to pre-#31; registering #31 leaves every existing RNG stream's cursor byte-identical (the
  #40 `T-FN-NEU-003` class — #31 registers **no** stream).
- **T-TX-INT-001 (integer posture)** — every `Contract`/valuation/spend field is integer; #31 introduces **no**
  float (static/reflection assertion — stronger than #33's one mirror-boundary float).
- **T-TX-SHAPE-001 (draw-free)** — the serialized transfers block contains no `RngCursor`/`actionOrdinal`
  field (schema-shape assertion, FR-TX-018).

## 5.6 Fail-loud (F1..F8)

- **T-TX-FAIL-001 (F3)** — bad `TRANSFERS_SAVE_FORMAT_VERSION` / out-of-bounds length prefix (the overflow-safe
  `total − offset` `Require`) / trailing bytes all throw at decode.
- **T-TX-FAIL-002 (F6)** — a bid on a `PlayerId` outside #27's club universe, a negative fee/wage, or
  `LengthSeasons ≤ 0` throws at the consuming seam.
- **T-TX-FAIL-003 (F7)** — a `default(Contract)` (`LengthSeasons = 0`) reaching insertion validation throws.

## 5.7 Requirement traceability

Every FR-TX-001..028 maps to a T-TX-* test above **or** a recorded §7 deferral. Deep-tier-only requirements
(FR-TX-002 deep half, FR-TX-015 clause append, FR-TX-017 keyed draw) are locked at their identity boundary
now (the `deepTransfersEnabled`-off equality) and fully at the deep T-phase.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial §5 (valuation/offer units, atomic bid integration, roster re-key, window, save/determinism/neutrality, fail-loud, traceability). Status IN REVIEW. |
#endregion
