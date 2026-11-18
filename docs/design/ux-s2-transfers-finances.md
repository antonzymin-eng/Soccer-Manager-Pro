# System XI — S2 Transfers and Club Finances: Gate A audit and Gates B–C design

**Created:** November 5, 2026\
**Last Updated:** November 5, 2026 (v0.4 — T5: owner preflight, actual copy and capacity-hint limits; detailed plan remains UX status authority.)
**Last Updated (prior):** November 5, 2026\
**Version:** 0.4\
**Status:** GATE A RECORDED; GATES B–C DRAFTED, owner answered Q1–Q3 (November 5); host-free client half LANDED and reviewed (§7); Gates D–J not started\
**Purpose:** audit the transfer and club-finance capabilities against the tree of the day and draft the
task flow and information design for the two PM-3 screens the owner added to scope on November 4, 2026
(roadmap D-13: "PM-3 includes transfer and finance screens").\
**Execution authority:** [`ux-detailed-plan.md`](ux-detailed-plan.md) §5 (Gates A–J) and §8 (S2) ·
**Architecture:** [`ux-experience-architecture.md`](ux-experience-architecture.md) (Transfers and Finances rows) ·
**Client contract:** [`interactive-unity-client-design.md`](../tracking/interactive-unity-client-design.md) §5-P8

## 1. Audited boundary

Audited on branch `work` at `ad05b76` (November 5, 2026), which includes ERR-040-004 (genesis transfer
budget) and the opt-in P7b career binding (not yet compiled on the pinned Unity host). Vocabulary is
`ux-detailed-plan.md` §3: `LIVE`, `DESIGNABLE` (owning read/action exists, presentation does not),
`FUTURE-BLOCKED`, qualified `HOST-UNVERIFIED` where code has not run in Unity. Every row names the symbol
it was verified against.

`ux-detailed-plan.md` §8.1 remains the authority for current UX status; its November 5 refresh records
Transfers/Finances as DESIGNABLE with LIVE host-free client code and HOST-UNVERIFIED rendering.
This supplement supplies journey evidence and draft Gates B–C; it does not supersede the plan.

## 2. Gate A — capability classification

### 2.1 Reads

| # | Element the screens need | Classification | Evidence |
|---|---|---|---|
| R1 | Managed club's transfer ceiling | `DESIGNABLE` | `SeasonLoop.AvailableTransferBudget(clubId)` (`SeasonLoop.cs:742`), the season ceiling; 100,000 at genesis (ERR-040-004) |
| R2 | Spend already committed this window | `DESIGNABLE` | `SeasonLoop.TransfersSnapshot().CommittedSpendThisWindow` (`:770`, a detached clone). Remaining = R1 − R2, exactly the check `TransferCommands.SubmitBid` makes |
| R3 | Window open/closed and its dates | `DESIGNABLE` | `TransfersSnapshot().ActiveWindow` (`OpenWorldDay`, `CloseWorldDay`, `IsWindowOpen(day)`) and `SeasonLoop.CurrentWorldDay` (`:573`). Summer window only; it stays open for `SummerWindowInSeasonDays` (38) days from the opening fixture day, so the last open day is the opening fixture day + 37 (`TransferWindow.SummerWindowFor`; genesis opening day 7 gives [0, 44]) |
| R4 | Every club's players: name, age, position, attributes | `DESIGNABLE` (truth read; fog note below) | `SeasonLoop.Progression.SquadFor(clubId)` → `PlayerRecord` (`FirstName`, `LastName`, `Age`, `Position`, `Attributes`) |
| R5 | Player current ability | `DESIGNABLE` (truth read; fog note below) | `ProgressionEngine.LifecycleView(clubId, playerId).CurrentAbility` |
| R6 | Club names | `DESIGNABLE` | `League.Clubs` (`Club.Name`) |
| R7 | Managed players' contracts (wage, seasons left) | `DESIGNABLE` | `TransfersSnapshot().ContractsSnapshot()` / `TryGetContract`. A player can be un-contracted (ERR-031-009) and stays sellable |
| R8 | Player availability (injury) | `DESIGNABLE` | `PlayerCareerStates.IsAvailable(clubId, playerId)`; an injury travels with a transferred player |
| R9 | Club finances: balance, transfer and wage budget, wage bill | `DESIGNABLE` | `SeasonLoop.FinanceView(clubId)` → `FinancesViewModel` (`Balance`, `TransferBudget`, `WageBudget`, `WageBillAggregate`) |
| R10 | A player's price | `DESIGNABLE` (Q1 answered: estimated value; truth read, fog note below) | No #31 read surface names a price. `PlayerValuation.ValuePlayer(attributes, age)` is #31's public base value; the acceptance pivot is `CounterpartyValue` = base × the counterparty's positional-need multiplier, computed inside `SubmitBid` |
| R11 | Counter-offer amount | `FUTURE-BLOCKED` | `TransferSubmissionOutcome.CounterOffered` carries no amount; only that the fee is inside the per-mille band around value (`NegotiationEngine.EvaluateOffer`). Showing an amount needs a #31 seam (FR-UI-020 / #38 KD-4) |
| R12 | Wage bill matching the contracts | `FUTURE-BLOCKED` | `WageBillAggregate` has no producer at Stage 2 (#40 KD-5): contracts carry wages but the finance view shows a 0 wage bill. Display it as "not tracked yet" or hide it |
| R13 | Season revenue so far | `FUTURE-BLOCKED` | `ClubFinances.SeasonRevenueAccrued` exists but `FinancesViewModel` does not expose it; daily revenue is default-off (#40 T3b2) |

**Fog dependency on #32 (M7).** #32 (APPROVED, no assembly) KD-2 states that manager-side surfaces read only
`KnownPlayer`, with fog covering *external* players' attributes (`scouting-player-knowledge/section-1.md`). R4, R5
and R10 read external players' true attributes, true `CurrentAbility` and `ValuePlayer` of true attributes. At
#32's minimal tier fog is off, so what the screen shows is identical today, and reading truth is the correct
behaviour under "do not wire an assembly-less specification ahead of its T0 landing". Once #32 T0 lands,
external-player CA and estimated value MUST switch to `KnownPlayer`; a value-from-knowledge seam would then be
filed against #31/#32 (FR-UI-020 / KD-4), not added client-side. Recorded as a condition on D-P8-1.

All reads are **sim-thread only** (`SeasonLoop` THREAD SAFETY note). The client must take them on the
P7b career worker and publish an immutable snapshot, as `ClientCareerCoordinator` already does for Career
Home.

### 2.2 Actions

| # | Action | Classification | Evidence |
|---|---|---|---|
| A1 | Buy a player (club, fee, wage, length) | `DESIGNABLE` | `SeasonLoop.SubmitTransferBid(in Offer)` (`:802`), the #31 FR-TX-025 UI seam. Typed outcomes: `Accepted`, `Rejected`, `CounterOffered`, `InsufficientBudget`, `SquadFull` (25 for a buy; on a sale it is the **buyer's** squad), `SquadDepleted` (the seller would be left without a complete starting eleven, not merely a head count) |
| A2 | Sell a player to a chosen club | `DESIGNABLE` | Same seam, `Offer.IsBuy = false`, counterparty = buying club; the buyer's need sets its value |
| A3 | Withdraw / cancel an offer | not needed | Bids are synchronous; nothing stays pending (#31 minimal has no negotiation state) |
| A4 | Renew or release a contract | `FUTURE-BLOCKED` | No #31 seam (re-signing is deep tier, §3.7) |
| A5 | AI clubs bidding for the manager's players | `FUTURE-BLOCKED` | No autonomous producer (#31 FR-TX-024) |

Caller-contract failures still throw at SubmitBid (F4/F6); the authoritative preflight is now #31
`Diagnose` via `SeasonLoop.DiagnoseTransferBid` / `CareerSession.DiagnoseTransfer`, run on the serialized
career worker against live state immediately before mutation (ERR-031-017 / ERR-030-059). It publishes
mapped typed refusals and never submits an invalid/stale offer. The UI `TransferOfferCheck` is only a
non-authoritative snapshot hint. #31 sets no wage upper bound; the worker separately enforces the
client's interim 0..50000000 policy pending ERR-031-016. Invariant/integration defects remain fail-loud.

### 2.3 Client, navigation and host

| # | Element | Classification | Evidence |
|---|---|---|---|
| C1 | Screen identities | `FUTURE-BLOCKED` | `ClientScreens` has seven ids; no Transfers or Finances id |
| C2 | Navigation moves | `FUTURE-BLOCKED` | `ClientScreenFlow` has no Career Home ↔ Transfers/Finances moves |
| C3 | Dispatch route | `DESIGNABLE` | No `IntentKind` carries offer terms, and `ui-framework` does not reference `Transfers`. The P7b precedent (`SaveTo`, `LoadCareer`) is a coordinator operation on the career worker. See §5-P8 |
| C4 | Unity rendering | `FUTURE-BLOCKED / HOST-UNVERIFIED` | Depends on the P7b UGUI binding, which has not compiled in Unity 6000.4.9f1 yet |
| C5 | Save after a transfer | decided (Q2: no autosave) | Autosave stays after rounds and season rolls only (D-P7-2); the next round's autosave or a manual save keeps a transfer |

**Gate A result:** every proposed control and data element is backed by a verified owner (R1–R9, A1–A2)
or is labelled `FUTURE-BLOCKED` (R11–R13, A4–A5, C1–C2, C4). R10 and C5 were settled by the owner's
answers (§5). No phantom action is proposed.

## 3. Gate B — task flows (draft)

### 3.1 Buy a player

- **Entry:** Career Home → Transfers. Available whenever a career is loaded; buying controls are enabled
  only while the window is open.
- **Goal:** sign a player who improves the squad within the remaining budget.
- **Information:** remaining budget (R1 − R2); window state and close day (R3); the list of other clubs'
  players with club, position, age, current ability, availability, and estimated value (Q1); own squad
  size against 25 and per position.
- **Decision:** pick a player, enter a fee (pre-filled with the estimated value), keep the default wage and length or change
  them, submit.
- **Outcomes, each with its own message:**
  - `Accepted` — player joins; budget, squad and contracts update.
  - `CounterOffered` — "Close: the club is open to a better offer." No amount (R11).
  - `Rejected` — "Too far from their valuation."
  - `InsufficientBudget` — shows the remaining budget.
  - `SquadFull` — "Your squad is full (25). Sell a player first."
  - `SquadDepleted` — "That club cannot release him: it would be left without a complete starting eleven."
- **Blocked paths:** window closed (list stays readable, buying disabled, "Next window opens at the start
  of next season"); a career operation running (busy notice, controls disabled); an offer the client refuses
  with a typed reason (§7: e.g. "The wage must be between 0 and 50000000 per period.").
- **Back:** returns to Career Home. Nothing is pending, so leaving never abandons anything.
- **Completion:** the result message, with the list refreshed from a new snapshot. Not autosaved (Q2):
  the next round's autosave or a manual save keeps the deal; the existing save-compatibility warning applies.

### 3.2 Sell a player

- **Entry:** Transfers → "Your squad" tab.
- **Decision:** pick one of the manager's players, pick a buying club, enter a fee (pre-filled with the estimated value),
  submit.
- **Outcomes:** as in 3.1, with two sale-side messages. `SquadFull` here is the **buyer's**: "{buyer} has no room
  in its squad ({capacity})." `SquadDepleted` here means "You cannot sell him: your squad would be left without
  a complete starting eleven." `InsufficientBudget` cannot occur on a sale (AI clubs' budgets are not checked; counterparty-ledger
  interim, ERR-031-013).
- **Blocked paths and back:** as in 3.1.

### 3.3 Check club finances

- **Entry:** Career Home → Finances. Read-only.
- **Information:** balance; transfer budget, committed spend and remaining; wage budget; contract wage
  total computed by #31's contracts (labelled "contracted wages") beside the #40 wage bill marked "not
  tracked yet" (R12); next settlement at season end.
- **Back:** Career Home.

## 4. Gate C — low-fidelity information design (draft)

**Transfers page** (one page, two tabs, keyboard-first like the P7b pages):

1. Header line: window state ("Window open until day 44" / "Window closed"), remaining budget, squad
   size "23 / 25".
2. Tab A "Other clubs' players" (default), tab B "Your squad".
3. Filter row: position, club; sort by ability, age or estimated value.
4. Player rows: name, club, position, age, ability, availability, estimated value. One row selected at a
   time.
5. Offer panel for the selected row: fee field, wage and contract length (defaults
   `DefaultWagePerPeriod` 1,000, `DefaultContractSeasons` 3), buying-club picker on tab B only, and one
   primary action "Make offer". The draft host design may disable it with a reason using window,
   busy and capacity snapshot hints. `TransferOfferCheck` itself does not check either the managed
   squad's capacity (buy) or the buyer's capacity (sale); no single implemented disable rule covers
   those cases. #31's authoritative destination-capacity gate returns `SquadFull`. Unity pages remain pending.
6. Result line under the panel (persistent until the next action), plus the busy notice.

Focus order: header → tabs → filters → list → offer panel → result → Back. The list and the panel reuse
the P7b scrolled-page factory, so text scale and pseudo-locale follow `ux-shared-system.md`.

**Finances page:** one column of labelled values (§3.3) and Back. No actions.

Copy lives under a new `ui.career.transfer.*` / `ui.career.finance.*` key family in the
`CareerScreenText` pattern (`S0TextFormatter` admission), not in `ui.s0.*`.

## 5. Owner decisions

**Answered November 5, 2026 (Anton Zymin):** Q1 (b) estimated value; Q2 (b) no autosave after a transfer;
Q3 no counter figure for PM-3. The questions as asked are kept below.


- **Q1 — What price does the player see?**
  - (a) Nothing: bid blind and learn from Rejected / Counter / Accepted.
  - (b) **Recommended:** #31's base value (`PlayerValuation.ValuePlayer`) labelled "estimated value". The
    club's actual price differs by its positional need (scarcity), so negotiation still matters.
  - (c) The exact price the club accepts. Every bid then succeeds first time; there is no negotiation.
- **Q2 — Save after a transfer?**
  - (a) **Recommended:** autosave after each accepted transfer, matching the "autosave after anything
    that changes the career" reading of D-P7-2.
  - (b) No autosave; the player saves manually.
- **Q3 — Counter-offer amount (R11).** Leave it out for PM-3 (recommended), or file a #31 seam so the
  club names a figure.

## 6. What this does not change

Gate A–C (§§1–5) made no spec, code, schema or navigation change by themselves. Contract renewal, AI-initiated
bids, scouting knowledge (#32) and the counterparty ledger stay out of scope.

## 7. As built: host-free client half and its review

Landed in `b04c19a`, then corrected by two review rounds (`c1b43e0`, `b57c6cf`); contract in
[`interactive-unity-client-design.md`](../tracking/interactive-unity-client-design.md) §5-P8. This section
records what the code now does where §§3–4 drafted it.

- **Result.** The coordinator publishes `LastTransferResult` (`TransferResult`): the submitted offer plus
  *either* #31's typed `TransferSubmissionOutcome` *or* a client `TransferOfferIssue` refusal. It replaces the
  earlier `LastTransferOutcome`, so the copy knows the deal's direction (buy or sale) without the page keeping
  the offer.
- **Owner-backed typed refusals.** #31 `Diagnose` on the worker is authoritative. It runs through the
  session/loop wrappers against live state, then mapped `TransferOfferIssue` values are published without
  calling SubmitBid. Cached UI `TransferOfferCheck` is only a hint, with its documented window/player/
  direction/counterparty/fee/wage/length order. Its order can differ from the owner's legacy exception order.
  A stale market cannot authorize a repeated completed sale. The worker additionally enforces the interim
  client wage ceiling; the built copy is "The wage must be between 0 and 50000000 per period."
- **Term bounds (client guard, not a game rule).** Fee ≥ 0; wage in 0..`MAX_OFFER_WAGE_PER_PERIOD`
  (50,000,000, inclusive; `[CROSS]` #40 `ClubFinancesBudgetCeilingMax`); length ≥ 1 season. The wage bound exists
  because #31 accepts any non-negative wage (ERR-031-016, Open): a wage of `long.MaxValue` was Accepted and
  overflowed the Club Finances projection.
- **Independent projections.** The five views (Home, New Career, Load, Transfers, Finances) project
  independently; a failing one keeps its last good value and its message goes to `LastProjectionError`, apart
  from `LastError`. The transfer result always publishes, so a committed deal is never hidden. Entry operations
  (Start/Load/Continue) whose Home projection fails release the held career,
  keep the current form/menu and publish localized LastError; the autosave remains available for Continue.
  The Unity notice consumes LastProjectionError after LastError; Quit clears it.
- **Rows and finances.** `TransferClubRow.SquadCount` lets a sale show a full buyer. The contracted-wage total
  saturates at `long.MaxValue` and is flagged (`IsContractedWagesSaturated`, copy "more than {0} per period").
  The #40 wage-bill line is shown only when `IsWageBillTracked` (false today, #40 KD-5), otherwise "not
  tracked yet". Both sources take remaining budget from `SeasonLoop.RemainingTransferBudget`, backed by #31's
  single owner calculation; view models carry that value as data. `TransferBudgetRule` was deleted.
- **Money copy.** Money formats through the formatter's new non-negative `long` argument type `L`
  (`S0TextRole` / `S0TextFormatter`); S0 roles and content hash are unchanged.
- **Dispatch.** The Transfers screen registers a null dispatcher; `SubmitTransfer` is a composition-root
  operation onto `SeasonLoop.SubmitTransferBid`, sanctioned by ERR-038-006 (#38 Appendix C.2). It needs no
  framework change beyond that amendment.
- **Known limitation.** The Unity shell has no roots for screens 8 and 9 yet. `ClientShellBehaviour` now rejects wiring if the flow reaches a screen it has no root for, instead of showing a blank
  screen; the Unity pages remain pending the P7b host compile (C4).
- **Tests.** Owner issue/SubmitBid agreement, live wrapper/headroom reads, stale-market repeated-sale
  refusal, failed-entry rollback, five projection-retention paths, Home unblocking, long-cell cache updates,
  hint order, saturation boundaries and S0 identity are covered. Current measured results and mutation
  evidence are recorded in the landing changelogs; this supplement is not the test-count ledger.

## Version history

| Version | Date | Notes |
|---|---|---|
| 0.4 | 2026-11-05 | T5: owner preflight, actual copy and capacity-hint limits; detailed plan remains UX status authority. |
| 0.3 | 2026-11-05 | Brought up to the built client after both review rounds: §7 as-built record (TransferResult, TransferOfferCheck/Issue, wage bound and ERR-031-016, independent projections, SquadCount, saturated wages, `L` money type, Unity shell limitation, test counts); SquadDepleted copy is the complete-starting-eleven rule; a sale's SquadFull is the buyer's and the offer panel is also disabled for a full buyer; window arithmetic corrected (last day = opening + 37); #32 fog dependency recorded on R4/R5/R10. |
| 0.2 | 2026-11-05 | Owner answers Q1 (b) estimated value, Q2 (b) no autosave, Q3 no counter figure; flows and layout updated. |
| 0.1 | 2026-11-05 | Gate A audit of transfers/finances at `ad05b76`; draft Gate B flows and Gate C layout; owner questions Q1–Q3. |
