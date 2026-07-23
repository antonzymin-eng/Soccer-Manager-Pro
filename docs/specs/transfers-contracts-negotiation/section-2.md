# Transfers, Contracts & Negotiation #31 — Section 2: Requirements, Data Structures, Failure Modes

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.2 — AR-1 fix pass; prior v0.1 initial)
**Version:** 0.2
**Status:** APPROVED

---

## 2.1 Functional requirements (FR-TX-001..028)

| ID | Requirement | Level | KD |
|---|---|---|---|
| FR-TX-001 | The Stage-2 counterparty valuation MUST be a **pure deterministic integer function** of #27 `PlayerAttributes` + `Age` + a club-need signal — no RNG, no #33 read, no #28 CA read. | MUST | KD-1 |
| FR-TX-002 | Personality (#33) and CA/PA (#28) MUST enter only at the deep tier as a **multiplicative** bias on the FR-TX-001 identity; with `deepTransfersEnabled` off the bias MUST be exactly `1000‰` (identity). | MUST | KD-1 |
| FR-TX-003 | `EvaluateOffer(in Offer, long counterpartyValuation)` MUST resolve accept/reject deterministically at minimal (no draw); a `#33`-unconfigured negotiation MUST reproduce the FR-TX-001 valuation exactly. | MUST | KD-1 |
| FR-TX-004 | #31 MUST read the club spending ceiling only via #40's `AvailableTransferBudget(in ClubFinances) → TransferBudget` (read-only). | MUST | KD-2 |
| FR-TX-005 | #31 MUST commit an accepted deal's money only via #40's `ApplyTransaction` — a buy posts `{Debit,TransferFee,fee}` + `{Debit,PlayerWage,inWage}`; a sell posts `{Credit,TransferFee,fee}` + `{Credit,PlayerWage,outWage}`. #31 MUST NOT write `ClubFinances` fields directly. | MUST | KD-2 |
| FR-TX-006 | #31 MUST NOT maintain a parallel cash ledger and MUST NOT expect `TransferBudget` to decrement as `ApplyTransaction` calls accumulate (the ceiling is `SettleFinances`-only, FR-FN-003/004). | MUST | KD-2 |
| FR-TX-007 | #31 MUST own a per-club `committedSpendThisWindow` counter (a spend-against-ceiling accumulator, distinct from #40's `Balance`/`WageBillAggregate`) for the affordability gate, reset to `0` each window open. | MUST | KD-2 |
| FR-TX-008 | A buy MUST fail loud (F1) when `fee > AvailableTransferBudget − committedSpendThisWindow`. | MUST | KD-2/F1 |
| FR-TX-009 | Every gate (window open, counterparty accepts, destination `Squad` free slot, affordability) MUST pass **before any mutation**; the commit (finance posts + roster move + hook) MUST be a single atomic block leaving finances and roster untouched on any failed gate (F2). | MUST | KD-2/KD-7/F2 |
| FR-TX-010 | The offer/response seam (`Offer`, `NegotiationOutcome`, `EvaluateOffer`) MUST be **counterparty-generic** (keyed on a caller-supplied valuation input) so #32/#34 reuse it without duplication. | MUST | KD-3 |
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
| FR-TX-022 | The #30 roster-commit MUST (a) allocate a free `localIndex` in the destination `Squad` — **fail loud (F5)** if full — (b) move the `PlayerRecord`, (c) dispatch a roster-move hook each per-`PlayerId` system subscribes to. | MUST | KD-7/F5 |
| FR-TX-023 | #31 MUST migrate **only** its own `Contract` in the roster-move hook (`OnPlayerRekeyed`); #28 CA/PA and #33 morale MUST migrate their own keyed state; #31 MUST NOT migrate another system's state. | MUST | KD-7 |
| FR-TX-024 | A season with **no** manager transfer action MUST advance byte-identical to pre-#31 (no autonomous transfer producer at minimal). | MUST | KD-8 |
| FR-TX-025 | A manager transfer command (`SubmitBid`) MUST be the only initiator of a minimal transfer; it MUST be window- and budget-gated (FR-TX-008/020). The UI MUST drive it through this command seam, never mutate #31 state directly. | MUST | KD-8 |
| FR-TX-026 | A bid on a `PlayerId` outside #27's club universe, or a malformed `Contract`/`Offer` (negative fee/wage, non-positive length), MUST fail loud (F6) at the consuming seam. | MUST | F6 |
| FR-TX-027 | Round-trip save→restore MUST be field-identical for `Contract` + window cursor + `committedSpendThisWindow`, including a mid-window save (and, deep, a mid-negotiation save); a full window's activity MUST be two-run deterministic from a fixed world seed. | MUST | KD-4/KD-8 |
| FR-TX-028 | Contracts MUST survive `RollToNextSeason` (durable career state); the window cursor + `committedSpendThisWindow` MUST reset at the season boundary; a retired/regenerated `PlayerId`'s contract MUST be removed/inserted in lockstep with #28's roster lifecycle. | MUST | KD-4/KD-7 |

## 2.2 Data structures

```csharp
// Durable contract state (serialized, KD-4). Integer amounts; deep clauses APPEND (FR-TX-015).
// Contract END is a SINGLE source of truth — LengthSeasons (remaining seasons, decremented at
// RollToNextSeason, expires at 0). Absolute-world-day precise expiry is a deep-tier option, NOT dual-stored.
public struct Contract
{
    public int  PlayerId;          // #27 club-scoped id (clubId*CLUB_SQUAD_SIZE+localIndex)
    public long WagePerPeriod;     // >= 0 integer currency/period (the #40 FinanceTransaction.Amount posture)
    public int  LengthSeasons;     // > 0 remaining seasons (F6/F7 on <= 0); the sole contract-end field
    // deep (behind deepTransfersEnabled): clauses/loan/wage-structure + precise ExpiryWorldDay APPEND here.
}

// The reusable offer/response seam (KD-3), counterparty-generic (FR-TX-010). CounterpartyClubId is the OTHER
// club: on a buy it is the player's owning (selling) club; on a sell it is the manager-named target buyer
// (no autonomous AI selects one at minimal, KD-8).
public readonly struct Offer            // a manager-initiated bid (buy) or listing to a named buyer (sell)
{ public int PlayerId; public int CounterpartyClubId; public long Fee; public long WagePerPeriod; public int LengthSeasons; public bool IsBuy; }
public enum NegotiationOutcome : byte   { Rejected = 0, Accepted = 1, CounterOffered = 2 /* deep */ }

// Per-club season-scoped transfer state (serialized). committedSpendThisWindow is FR-TX-007.
public struct ClubTransferState
{ public long CommittedSpendThisWindow; public TransferWindow ActiveWindow; /* deep: in-flight negotiations */ }
public readonly struct TransferWindow   { public uint OpenWorldDay, CloseWorldDay; }   // FR-TX-019

// The #31 store composed into the save. At minimal, #31 tracks contracts + spend for the MANAGED club only
// (AI clubs are valuation functions with no persisted #31 state — their squads exist in #27 rosters without
// #31 contract records); all-clubs contract modeling arrives with autonomous AI at the deep tier (KD-8/§7).
public sealed class TransfersState { /* managed-club contracts keyed by PlayerId; ClubTransferState */ }
```

## 2.3 Failure modes

| ID | Condition | Handling |
|---|---|---|
| **F1** | A buy with `fee > AvailableTransferBudget − committedSpendThisWindow` | **Fail loud** — over-ceiling spend is a caller/UI-contract bug, never silently clamped (FR-TX-008). |
| **F2** | Any commit gate fails after a partial mutation would occur | **Fail loud + no mutation** — validate-all-before-write; the commit is atomic (FR-TX-009). |
| **F3** | Transfers sub-blob: bad `TRANSFERS_SAVE_FORMAT_VERSION` / out-of-bounds length prefix / trailing bytes | **Fail loud** — the `SeasonSaveCodec` posture; no cross-version migration at Stage 0 (KD-4). |
| **F4** | A transfer action outside an open window | **Fail loud** (FR-TX-020, `IsWindowOpen`). |
| **F5** | Destination `Squad` full (no free `localIndex`) at roster commit | **Fail loud** — a squad-size-cap violation is a bug, caught before any finance post (FR-TX-022). |
| **F6** | Bid on a `PlayerId` outside #27's club universe, or malformed `Contract`/`Offer` (negative fee/wage, `LengthSeasons ≤ 0`) | **Fail loud** — magnitude/identity validity is a caller-contract bug (the #27 `SquadFileLoader` / #40 F2 precedent). |
| **F7** | `default(Contract)` (`LengthSeasons = 0`) reaching a consuming seam | **Fail loud** — `LengthSeasons = 0 ∉ (0,∞)` catches a zero-value-trap default record at insertion validation (the #33 F4 precedent). `PlayerId = 0` alone is not the trap (it is a real club-0/local-0 id); `LengthSeasons` is the discriminator. |
| **F8** | `ApplyTransaction` reversal larger than the current aggregate (a sell Credit-off exceeding `WageBillAggregate`) | Delegated to #40's F1 (fail loud) — #31 does not repair #40 state. |

**Zero-value-trap discipline (KD-8/F7):** `Contract` has no `Create()` neutral (a contract is only ever
constructed from an accepted `Offer`), so `default(Contract)` is invalid by design — `LengthSeasons = 0`
fails the FR-TX-026/F6 gate at insertion. `Offer.IsBuy` defaults `false` (sell) but every real `Offer` is
constructed by the command seam with an explicit direction, so the default is never routed unchecked.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial §2 (FR-TX-001..028, data structures, F1..F8). Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-1: `Offer` gains `CounterpartyClubId` (M1); `Contract` drops `ExpiryWorldDay` → single contract-end truth `LengthSeasons` (M3); `TransfersState` scoped to the managed club (M2); F7 note tightened. |
#endregion
