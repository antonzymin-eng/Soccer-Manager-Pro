# Transfers, Contracts & Negotiation #31 — Section 5: Test Plan

**Created:** July 23, 2026
**Last Updated:** October 20, 2026 (v0.10 — T2 locks live)
**Last Updated (prior):** October 20, 2026 (v0.9 — ERR-031-006: T-TX-BID-005 and §5.7 restated for the stable-id re-home)
**Last Updated (prior):** October 19, 2026 (v0.8 — ERR-031-005: §5.3 re-home locks)
**Last Updated (prior):** October 18, 2026 (v0.7 — T1 locks live: T-TX-DET-001, T-TX-FAIL-001)
**Last Updated (prior):** September 14, 2026 (v0.6 — PR #407 residual review cleanup and T2 hook obligation)
**Last Updated (prior):** September 14, 2026 (v0.5 — PR #407 review corrections and stronger valuation/atomicity locks; prior v0.4 T0 football/Codex locks, v0.3 AR-6, v0.2 AR-3/AR-4, v0.1 initial)
**Version:** 0.10
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
  spend, re-homes the player under his unchanged id, and inserts the contract keyed by it; `TransferBudget`/
  `WageBillAggregate` remain unchanged.
- **T-TX-BID-002** — accepted sell commits the roster, then removes the managed contract and applies the staged
  `{Credit,TransferFee,fee}` finance copy.
- **T-TX-BID-003 (F1)** — cumulative over-budget buy returns `TransferSubmissionOutcome.InsufficientBudget`,
  not an exception, and leaves finance/spend/roster/contracts unchanged for that attempt.
- **T-TX-BID-004 (F5)** — full destination returns `TransferSubmissionOutcome.SquadFull`, not an exception, with
  no local mutation.
- **T-TX-BID-005 (F2)** — if a producer violates the successful-preview contract with a commit that does not land
  (after `RequestRosterCommit`, `TryGetPlayer` does not report the player on the destination roster) on a **buy**,
  `SubmitBid` fails loud before staged finance/spend/contract state is applied. *(ERR-031-006: originally "returns a
  different id" — the commit returns nothing once ids are stable, ERR-031-005.)*
- **T-TX-BID-006 (F2)** — the same producer breach on a **sell** leaves finance unchanged and deliberately retains
  the old managed contract locally; the external roster may already have moved, which is why T2 must make the
  production preview→commit contract infallible rather than relying on rollback.
- **T-TX-BID-007** — counteroffer/reject are ordinary no-mutation `TransferSubmissionOutcome` values.
- **T-TX-BID-008** — #31 writes no `ClubFinances` field directly and keeps no parallel cash ledger.

## 5.3 Roster re-home (KD-7; stable ids since ERR-031-005)

- **T-TX-REKEY-001** — *(restated at ERR-031-005)* a committed transfer keeps the player's id and moves his record
  to the destination roster; ownership is the roster's answer — a player whose id lies outside every packed range
  (a regen) is bought from the club whose roster carries him, and a direction that disagrees with roster ownership
  fails loud. Managed contract state is inserted after a confirmed buy commit or removed after a confirmed sell.
- **T-TX-REKEY-002** — *(restated)* the commit is confirmed through the port before local mutation; a commit that
  does not land fails loud with local state untouched (both directions).
- **T-TX-REKEY-003** — #31 migrates only its own contract state; #28/#33 state is not referenced.
- **T-TX-REKEY-004** — positional-stock read is read-only and bounded; an invalid producer count fails loud.
- **T-TX-REKEY-005 (deferred T2; restated at ERR-031-005)** — a real #30 re-home MUST carry the player's whole
  career with him under the same id — #28 record + lifecycle, #29/#41/#30 rows, #44 tally — and leave nothing at the
  source club; the moved player is then selectable by, and records appearances for, his new club.

## 5.4 Window (KD-6)

- **T-TX-WIN-001** — `IsWindowOpen` is inclusive at both boundaries; outside-window `SubmitBid` fails loud
  before valuation or mutation.

## 5.5 Save round-trip & determinism (KD-4/KD-8)

- **T-TX-DET-001** — live at T1: `Contract` + `ClubTransferState` restore field-identical across a mid-window save,
  through both the codec (`TransfersT1Tests`) and the season file + `SeasonLoop.Restore` resume path
  (`SeasonSaveTransfersTests`), with non-default spend so a reseed-on-load could not pass.
- **T-TX-DET-002** — live at T2: contracts age/decrement/remove at the season boundary, a retiree's contract is
  removed and a managed regen contracted, spend resets, the window re-derives from the roll day, and the rolled
  career still saves (`TransfersT2Tests`, `SeasonTransfersT2Tests.TheRoll_…`); three boundaries expire every genesis
  contract without a throw and the manager can still sell (ERR-031-009).
- **T-TX-INIT-001** — live at T2: `League.CreateLoop` with a #28 store seeds one genesis contract per managed player
  and opens the window on the new-game day; a load never re-seeds (`ALoadNeverReSeeds_…`, after a sale, so a reseed
  could not pass).
- **T-TX-DEP-001 (F9, ERR-031-007)** — a release that would leave the releasing club below a matchday squad returns
  `SquadDepleted` with #40, #28, #31 and the career byte-identical.
- **T-TX-FIN-001 (§7.1 T2)** — through `SeasonLoop.SubmitTransferBid`, every non-accepted outcome (Rejected,
  CounterOffered, InsufficientBudget, SquadFull, SquadDepleted) leaves the live #40 entry byte-identical; an
  accepted buy debits exactly one fee; a raw `TransferFee` posting is refused once #31 is wired.
- **T-TX-DET-003** — two runs over identical transfer activity produce identical transfer state.
- **T-TX-NEU-001** — no manager transfer action leaves pre-#31 season behavior unchanged; no RNG stream exists.
  Live at T2: a wired career that never bids plays a season byte-identically (season state + roster) to an unwired
  one, and one transfer moves it (non-vacuity).
- **T-TX-INT-001** — transfer state/valuation/spend fields are integer; no float/double state is introduced.

## 5.6 Fail-loud vs normal outcomes

- **T-TX-FAIL-001 (F3)** — live at T1: wrong magic (a sibling `FNCE` block), wrong version, every truncation,
  trailing bytes, `clubCount > 1`, contracts without a club, out-of-order ids, F6/F7 contract values, an inverted
  window and negative spend all throw at decode; a pinned golden byte vector locks the layout (ERR-031-003).
- **T-TX-FAIL-002 (F6)** — invalid player id or malformed fee/wage/length throws at consuming seams.
- **T-TX-FAIL-003 (F7)** — `default(Contract)` fails insertion validation.
- **T-TX-NORMAL-001 (F1/F5)** — insufficient budget and full destination are explicitly **not** fail-loud paths;
  they return typed no-mutation submission outcomes.

## 5.7 Requirement traceability

Every FR-TX-001..028 maps to a T-TX-* test above or a recorded §7 deferral. T0 now locks the reviewed public
API shape, precise integer valuation, symmetric positional need, typed normal command outcomes, and both sides
of the defensive commit-not-landed breach. T2 additionally owes the production carry-everything re-home lock in
T-TX-REKEY-005 (the re-home carries the player's #28 record + lifecycle, #29/#41/#30 rows and #44 tally under the
same id and leaves nothing behind). *(ERR-031-006: originally "the production hook-dispatch/no-op lock" — the
re-key hook was deleted at ERR-031-005.)*

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial §5 test plan; status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-3/AR-4 fee-only, aging/seeding and re-key coverage updates. |
| 0.3 | 2026-07-23 | — | AR-6 load-vs-genesis and managed↔AI hook corrections. |
| 0.4 | 2026-09-14 | — | T0 close-out: positional scarcity, counter band, reusable evaluator validation and no-mutation negotiation coverage. |
| 0.5 | 2026-09-14 | — | PR #407 review correction: literal golden/fractional valuation vectors, strict age boundaries, symmetric exclude-player need, typed budget/full outcomes, and preview-mismatch-before-local-mutation regression. |
| 0.6 | 2026-09-14 | — | Residual review cleanup: add sell-direction preview/commit breach coverage and pin the T2 production hook-dispatch plus observable managed↔external #31 no-op regression. |
| 0.7 | 2026-10-18 | — | T1 landing: T-TX-DET-001 and T-TX-FAIL-001 move from deferred to live, with the golden vector and sibling-magic refusal (ERR-031-003). |
| 0.8 | 2026-10-19 | — | ERR-031-005: §5.2/§5.3 restated for a stable-id re-home — T-TX-REKEY-001 locks roster-judged ownership including an off-range (regen) id; T-TX-REKEY-002 confirms the landing; T-TX-REKEY-005 becomes the T2 carry-everything integration lock. |
| 0.9 | 2026-10-20 | — | ERR-031-006 (ERR-031-005 back-prop completion): T-TX-BID-005 restated as "a commit that does not land" (was "returns a different id"); §5.7's T2 obligation is the T-TX-REKEY-005 carry-everything lock, not a hook-dispatch/no-op lock. The v0.6 row's hook-dispatch pin is superseded. |
| 0.10 | 2026-10-20 | — | #31 T2: T-TX-DET-002 / T-TX-INIT-001 / T-TX-NEU-001 live; new T-TX-DEP-001 (ERR-031-007) and T-TX-FIN-001 (the #40 staging seam). |
#endregion