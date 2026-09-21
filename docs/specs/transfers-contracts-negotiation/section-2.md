# Transfers, Contracts & Negotiation #31 — Section 2: Requirements, Data Structures, Failure Modes

**Created:** July 23, 2026
**Last Updated:** September 14, 2026 (v0.6 — T0 review correction: typed submission outcomes, precise valuation, symmetric need)
**Last Updated (prior):** September 14, 2026 (v0.5 — T0 football-judgment close-out; prior v0.4 AR-6, v0.3 AR-3/AR-4, v0.2 AR-1, v0.1 initial)
**Version:** 0.6
**Status:** APPROVED

---

## 2.1 Functional requirements (FR-TX-001..028)

| ID | Requirement | Level | KD |
|---|---|---|---|
| FR-TX-001 | The Stage-2 **base valuation identity** MUST be a pure deterministic integer function of #27 `PlayerAttributes` + `Age` — no RNG, no #33 read, no #28 CA read. The arithmetic MUST retain the canonical attribute mean as an exact rational (`sum / ATTRIBUTE_COUNT`) through the currency calculation rather than truncate it to an integer 1..20 bucket before applying value/age multipliers. The counterparty view then applies FR-TX-002. | MUST | KD-1 |
| FR-TX-002 | Stage 2 MUST apply an **always-on deterministic positional-scarcity multiplier** derived only from the valuing club's #27 count at the player's coarse `PlayerPosition`, measured **excluding the player under negotiation** in both directions. Below-neutral stock raises value and overstock lowers it around a `1000‰` neutral pivot. Personality (#33), CA/PA (#28), and staff effects remain deep-tier multiplicative refinements; disabling/defering those producers MUST NOT remove the Stage-2 positional-need term. | MUST | KD-1 |
| FR-TX-003 | `EvaluateOffer(in Offer, long counterpartyValuation)` MUST resolve `Accepted` / `CounterOffered` / `Rejected` deterministically at minimal with **no draw**. Exact value is the acceptance pivot; a configured symmetric per-mille band around value emits `CounterOffered`; only terms beyond that band reject. The reusable evaluator is a consuming seam and MUST fail loud on every malformed `Offer` term covered by FR-TX-026. | MUST | KD-1 |
| FR-TX-004 | #31 MUST read the club spending ceiling only via #40's `AvailableTransferBudget(in ClubFinances) → long` (read-only; returns the static `TransferBudget` field). | MUST | KD-2 |
| FR-TX-005 | #31 MUST commit an accepted deal's money only via #40's `ApplyTransaction` — at minimal a buy posts **only** `{Debit,TransferFee,fee}` and a sell posts **only** `{Credit,TransferFee,fee}` (fee-only, preserving #40 FR-FN-015). The `PlayerWage` posts (buy `{Debit,PlayerWage,inWage}` / sell `{Credit,PlayerWage,outWage}`) are **deep-tier** (behind `deepTransfersEnabled`, §7). #31 MUST NOT write `ClubFinances` fields directly. | MUST | KD-2 |
| FR-TX-006 | #31 MUST NOT maintain a parallel cash ledger and MUST NOT expect `TransferBudget` to decrement as `ApplyTransaction` calls accumulate (the ceiling is `SettleFinances`-only, FR-FN-003/004). | MUST | KD-2 |
| FR-TX-007 | #31 MUST own a per-club `committedSpendThisWindow` counter (a spend-against-ceiling accumulator, distinct from #40's `Balance`/`WageBillAggregate`) for the affordability gate, reset to `0` at the **season boundary** (one window at minimal ⇒ boundary = window open; a deep winter window resets it at each window open). | MUST | KD-2 |
| FR-TX-008 | When an otherwise accepted buy has `fee > AvailableTransferBudget − committedSpendThisWindow`, `SubmitBid` MUST return the ordinary no-mutation result `TransferSubmissionOutcome.InsufficientBudget`; this player-reachable condition MUST NOT be represented by an exception. Invalid negative/out-of-range finance state remains fail-loud. | MUST | KD-2/F1 |
| FR-TX-009 | Every commit gate (window open, negotiation `Accepted`, destination `Squad` free slot, affordability) MUST pass **before any local #31/#40 mutation**. `Rejected`, `CounterOffered`, `InsufficientBudget`, and `SquadFull` are ordinary no-mutation command outcomes. After a successful roster preview, the roster commit is contractually infallible and MUST return the previewed id; #31 MUST verify that result before applying the staged finance copy or contract/spend mutation. | MUST | KD-2/KD-7/F2 |
| FR-TX-010 | The offer/response seam (`Offer`, `NegotiationOutcome`, `EvaluateOffer`) MUST be **counterparty-generic** (keyed on a caller-supplied valuation input) so #32/#34 reuse it without duplication. `TransferSubmissionOutcome` is the separate manager-command result and MUST NOT be used as the reusable negotiation type. | MUST | KD-3 |
| FR-TX-011 | #31 MUST NOT build a #32 or #34 interface (FR-LW-031); #31's own #34-staff-influence MUST be a deferred `×1000‰` identity routing seam. | MUST | KD-3 |
| FR-TX-012 | #31 state MUST persist as an opaque, independently version-gated `TRANSFERS_SAVE_FORMAT_VERSION` sub-blob composed into #30's `SeasonSaveCodec`; the codec MUST NOT parse it. | MUST | KD-4 |
| FR-TX-013 | #31 MUST NOT bump `WORLD_STORE_FORMAT_VERSION`; durable contracts live in the season-save sub-blob (the #40 `Balance` precedent). | MUST | KD-4 |
| FR-TX-014 | The transfers sub-blob codec MUST fail loud (F3) on a `TRANSFERS_SAVE_FORMAT_VERSION` mismatch, an out-of-bounds length prefix (overflow-safe `total − offset` bound), or trailing bytes — the `SeasonSaveCodec`/`MatchSaveCodec` posture. | MUST | KD-4/F3 |
| FR-TX-015 | Deep-tier clause/loan/wage-structure fields MUST **append** to the `Contract` record behind `deepTransfersEnabled`; the minimal wage+length schema MUST NOT be rewritten. | MUST | KD-4 |
| FR-TX-016 | The minimal tier MUST register **no** RNG stream; `_RESERVED_0x23_` / `SubsystemOrdinals.Transfers = 85` MUST remain RESERVED (not promoted). | MUST | KD-5 |
| FR-TX-017 | Any deep-tier draw MUST be a **position-independent keyed draw** on `(clubId, playerId, worldDay, purpose)` (fixed-radix action-ordinal); no free-running cursor MUST be serialized. | MUST | KD-5 |
| FR-TX-018 | The serialized transfers block MUST contain no `RngCursor`/`actionOrdinal` field at minimal (draw-free). | MUST | KD-5 |
| FR-TX-019 | The transfer window MUST be a #31-owned `TransferWindow [OpenWorldDay, CloseWorldDay]` derived deterministically from #30's `SeasonCalendar` (read-only); minimal = one summer window. #31 MUST NOT mutate the calendar. | MUST | KD-6 |
| FR-TX-020 | A transfer action outside an open window MUST fail loud (F4, `IsWindowOpen`). | MUST | KD-6/F4 |
| FR-TX-021 | A committed transfer MUST re-key the moved player's club-scoped `PlayerId` via a #30-owned mid-season roster-commit entry point; #31 MUST NOT mutate #27 `Squad`/`PlayerRecord` directly. | MUST | KD-7 |
| FR-TX-022 | The #31 preflight over the #30 roster adapter MUST detect whether a free destination `localIndex` exists. If none exists, `SubmitBid` MUST return `TransferSubmissionOutcome.SquadFull` before any local mutation. A successful preview fixes the exact destination id that the subsequent #30 commit MUST return. | MUST | KD-7/F5 |
| FR-TX-023 | #31 MUST migrate **only** its own `Contract` state on a re-key; #28 CA/PA and #33 morale MUST migrate their own keyed state; #31 MUST NOT migrate another system's state. For a managed↔external transfer (every minimal case), `OnPlayerRekeyed` is a **no-op**. On a buy, `SubmitBid` inserts the new managed contract only after the successful matching roster commit; on a sell, it removes the old managed contract only after that successful matching commit. This ordering prevents a port-contract breach from first mutating local finance/contract state while remaining safe because the managed↔external hook is a no-op. | MUST | KD-7 |
| FR-TX-024 | A season with **no** manager transfer action MUST advance byte-identical to pre-#31 (no autonomous transfer producer at minimal). | MUST | KD-8 |
| FR-TX-025 | A manager transfer command (`SubmitBid`) MUST be the only initiator of a minimal transfer; it MUST be window- and budget-gated (FR-TX-008/020), return `TransferSubmissionOutcome`, and expose ordinary player-reachable negotiation/budget/capacity results without exceptions. The UI MUST drive it through this command seam, never mutate #31 state directly. | MUST | KD-8 |
| FR-TX-026 | A bid on a `PlayerId` outside #27's club universe, or a malformed `Contract`/`Offer` (negative fee/wage, non-positive length), MUST fail loud (F6) at **every consuming seam**, including the reusable `EvaluateOffer`. | MUST | F6 |
| FR-TX-027 | Round-trip save→restore MUST be field-identical for `Contract` + window cursor + `committedSpendThisWindow`, including a mid-window save (and, deep, a mid-negotiation save); a full window's activity MUST be two-run deterministic from a fixed world seed. | MUST | KD-4/KD-8 |
| FR-TX-028 | At **new-career genesis** (never on load — a load reconstructs from the sub-blob) the managed club's #27 squad MUST be seeded with one `Contract` per player (default `[GT]` terms, F6/F7-valid — §3.8) so the sell/aging flows have a populated set. Contracts MUST survive `RollToNextSeason` (durable career state); at the roll each managed contract's `LengthSeasons` MUST be decremented, and a contract that **would** reach `0` MUST be **removed** (never stored as `0` — F7), the player becoming un-contracted (deep-tier re-signing/free-agency handles the sequel; §3.7). The window cursor + `committedSpendThisWindow` MUST reset at the season boundary; a retired/regenerated `PlayerId`'s contract MUST be removed/inserted in lockstep with #28's roster lifecycle. | MUST | KD-4/KD-7 |

## 2.2 Data structures

```csharp
// Durable contract state (serialized, KD-4). Integer amounts; deep clauses APPEND (FR-TX-015).
public struct Contract
{
    public int  PlayerId;
    public long WagePerPeriod;
    public int  LengthSeasons;
}

// Counterparty-generic negotiation seam (KD-3).
public readonly struct Offer
{ public int PlayerId; public int CounterpartyClubId; public long Fee; public long WagePerPeriod; public int LengthSeasons; public bool IsBuy; }
public enum NegotiationOutcome : byte
{ Rejected = 0, Accepted = 1, CounterOffered = 2 }

// Manager-command result. Budget/capacity are deliberately NOT negotiation outcomes.
public enum TransferSubmissionOutcome : byte
{ Rejected = 0, Accepted = 1, CounterOffered = 2, InsufficientBudget = 3, SquadFull = 4 }

public struct ClubTransferState
{ public long CommittedSpendThisWindow; public TransferWindow ActiveWindow; /* deep: in-flight negotiations */ }
public readonly struct TransferWindow
{ public uint OpenWorldDay, CloseWorldDay; }
public sealed class TransfersState { /* managed-club contracts keyed by PlayerId; ClubTransferState */ }
```

## 2.3 Failure modes

| ID | Condition | Handling |
|---|---|---|
| **F1** | An otherwise accepted buy exceeds `AvailableTransferBudget − committedSpendThisWindow` | **Normal command outcome** — return `InsufficientBudget`, no mutation. Corrupt/negative finance invariants still fail loud. |
| **F2** | A commit gate/port contract would create partial local state | **Fail loud + no prior local mutation** — validate/preflight first; verify the roster commit matches its preview before applying staged finances/spend/contracts. |
| **F3** | Transfers sub-blob: bad `TRANSFERS_SAVE_FORMAT_VERSION` / out-of-bounds length prefix / trailing bytes | **Fail loud** — the `SeasonSaveCodec` posture; no cross-version migration at Stage 0 (KD-4). |
| **F4** | A transfer action outside an open window | **Fail loud** (FR-TX-020, `IsWindowOpen`). |
| **F5** | Destination `Squad` full (no free `localIndex`) during preflight | **Normal command outcome** — return `SquadFull`, no mutation. |
| **F6** | Bid on a `PlayerId` outside #27's club universe, or malformed `Contract`/`Offer` (negative fee/wage, `LengthSeasons ≤ 0`) | **Fail loud** — magnitude/identity validity is a caller-contract bug. |
| **F7** | `default(Contract)` (`LengthSeasons = 0`) reaching a consuming seam | **Fail loud** — `LengthSeasons = 0 ∉ (0,∞)` catches the zero-value-trap record. |
| **F8** | *(deep-tier)* `ApplyTransaction` wage reversal larger than the current aggregate | Delegated to #40's F1 (fail loud). Only reachable once deep-tier wage posting is on. |

**Zero-value-trap discipline (KD-8/F7):** `Contract` has no `Create()` neutral; `default(Contract)` is invalid by
design. `Offer.IsBuy` defaults `false` (sell), but every real `Offer` is constructed by the command seam with an
explicit direction, so the default is never routed unchecked.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial §2 (FR-TX-001..028, data structures, F1..F8). Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-1: `Offer` gains `CounterpartyClubId`; `Contract` drops `ExpiryWorldDay`; `TransfersState` scoped to managed club. |
| 0.3 | 2026-07-23 | — | AR-3/AR-4: minimal fee-only finance, managed↔external hook model, contract aging/seeding and related clarifications. |
| 0.4 | 2026-07-23 | — | AR-6: genesis seeding never runs on load. |
| 0.5 | 2026-09-14 | — | T0 football-judgment close-out: positional-stock multiplier, deterministic counter band, explicit no-mutation negotiation results, evaluator term validation. |
| 0.6 | 2026-09-14 | — | PR #407 review correction: retain fractional attribute-mean precision; define positional stock excluding the negotiated player; separate `TransferSubmissionOutcome` from `NegotiationOutcome`; make insufficient budget/full squad normal results; move local mutation after successful matching roster commit. |
#endregion
