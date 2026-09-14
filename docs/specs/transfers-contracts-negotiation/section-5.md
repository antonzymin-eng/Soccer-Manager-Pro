# Transfers, Contracts & Negotiation #31 — Section 5: Test Plan

**Created:** July 23, 2026
**Last Updated:** September 14, 2026 (v0.5 — PR #407 review corrections and stronger valuation/atomicity locks)
**Last Updated (prior):** September 14, 2026 (v0.4 — T0 football-judgment/Codex regression locks; prior v0.3 AR-6, v0.2 AR-3/AR-4, v0.1 initial)
**Version:** 0.5
**Status:** APPROVED

---

## 5.1 Unit — valuation & offer (KD-1)

- **T-TX-VAL-001** — `ValuePlayer` is deterministic, excludes weak foot, and is pinned by a literal golden vector:
  default 31×10 attributes, age 25 ⇒ `100000` under the default T0 config.
- **T-TX-VAL-002** — fractional attribute information is not discarded: raising exactly one canonical attribute
  from 10→11 changes the default golden value to `100322`, proving the base model is not quantised to 20 prices.
- **T-TX-VAL-003** — `AgeCurvePermille` has a strict young discount, `1000‰` at both peak boundaries, a strict
  post-peak decline, and reaches exactly `MinimumAgeMultiplierPermille` at sufficiently old age.
- **T-TX-VAL-004** — `ClubNeedMultiplierPermille`: below-neutral prospective stock > `1000‰`, neutral ==
  `1000‰`, maximum legal stock remains positive and below neutral; counter-band tuning remains in `(0,1000)`.
- **T-TX-VAL-005** — command-level positional need measures stock **excluding the negotiated player** on both
  buy and sell paths; equivalent zero-stock counterparty situations produce mirrored near-value counteroffers.
- **T-TX-OFF-001** — `EvaluateOffer` is draw-free and three-way: exact value accepts; inside the configured band
  counters; beyond it rejects, mirrored for buy/sell.
- **T-TX-OFF-002 (F6 / Codex P2)** — direct evaluator calls reject malformed wage/length terms.

## 5.2 Integration — the bid pipeline (KD-2/KD-7, atomic)

- **T-TX-BID-001** — accepted fair-value buy posts exactly one staged `{Debit,TransferFee,fee}` effect, commits
  spend, moves the roster, and inserts a re-keyed contract; `TransferBudget`/`WageBillAggregate` remain unchanged.
- **T-TX-BID-002** — accepted sell commits the roster, then removes the managed contract and applies the staged
  `{Credit,TransferFee,fee}` finance copy. The managed↔external #31 re-key hook is a no-op during the move.
- **T-TX-BID-003 (F1)** — cumulative over-budget buy returns `TransferSubmissionOutcome.InsufficientBudget`,
  not an exception, and leaves finance/spend/roster/contracts unchanged for that attempt.
- **T-TX-BID-004 (F5)** — full destination returns `TransferSubmissionOutcome.SquadFull`, not an exception, with
  no local mutation.
- **T-TX-BID-005 (F2)** — if a producer violates the successful-preview contract and returns a different id,
  `SubmitBid` fails loud **before** staged finance/spend/contract state is applied.
- **T-TX-BID-006** — counteroffer/reject are ordinary no-mutation `TransferSubmissionOutcome` values.
- **T-TX-BID-007** — #31 writes no `ClubFinances` field directly and keeps no parallel cash ledger.

## 5.3 Roster re-key (KD-7)

- **T-TX-REKEY-001** — a committed transfer uses `toClub*CLUB_SQUAD_SIZE+freeLocalIndex`; managed contract state
  is inserted after a successful buy commit or removed after a successful sell commit.
- **T-TX-REKEY-002** — the preview/commit id equality is checked before local mutation.
- **T-TX-REKEY-003** — #31 migrates only its own contract state; #28/#33 state is not referenced.
- **T-TX-REKEY-004** — positional-stock read is read-only and bounded; an invalid producer count fails loud.

## 5.4 Window (KD-6)

- **T-TX-WIN-001** — `IsWindowOpen` is inclusive at both boundaries; outside-window `SubmitBid` fails loud
  before valuation or mutation.

## 5.5 Save round-trip & determinism (KD-4/KD-8)

- **T-TX-DET-001** — deferred T1: `Contract` + `ClubTransferState` restore field-identical across mid-window save.
- **T-TX-DET-002** — deferred T2: contracts age/decrement/remove at season boundary and window/spend resets.
- **T-TX-INIT-001** — deferred T2: career genesis seeds one valid managed-club contract per #27 player.
- **T-TX-DET-003** — two runs over identical transfer activity produce identical transfer state.
- **T-TX-NEU-001** — no manager transfer action leaves pre-#31 season behavior unchanged; no RNG stream exists.
- **T-TX-INT-001** — transfer state/valuation/spend fields are integer; no float/double state is introduced.

## 5.6 Fail-loud vs normal outcomes

- **T-TX-FAIL-001 (F3)** — deferred T1 bad transfer save version/length/trailing bytes throw at decode.
- **T-TX-FAIL-002 (F6)** — invalid player id or malformed fee/wage/length throws at consuming seams.
- **T-TX-FAIL-003 (F7)** — `default(Contract)` fails insertion validation.
- **T-TX-NORMAL-001 (F1/F5)** — insufficient budget and full destination are explicitly **not** fail-loud paths;
  they return typed no-mutation submission outcomes.

## 5.7 Requirement traceability

Every FR-TX-001..028 maps to a T-TX-* test above or a recorded §7 deferral. T0 now locks the reviewed public
API shape, precise integer valuation, symmetric positional need, typed normal command outcomes, and the roster
preview/commit boundary.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial §5 test plan; status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-3/AR-4 fee-only, aging/seeding and re-key coverage updates. |
| 0.3 | 2026-07-23 | — | AR-6 load-vs-genesis and managed↔AI hook corrections. |
| 0.4 | 2026-09-14 | — | T0 close-out: positional scarcity, counter band, reusable evaluator validation and no-mutation negotiation coverage. |
| 0.5 | 2026-09-14 | — | PR #407 review correction: literal golden/fractional valuation vectors, strict age boundaries, symmetric exclude-player need, typed budget/full outcomes, and preview-mismatch-before-local-mutation regression. |
#endregion
