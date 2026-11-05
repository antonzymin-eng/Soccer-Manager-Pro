# System XI — S2 Transfers and Club Finances: Gate A audit and Gates B–C design

**Created:** November 5, 2026\
**Last Updated:** November 5, 2026\
**Version:** 0.2\
**Status:** GATE A RECORDED; GATES B–C DRAFTED, owner answered Q1–Q3 (November 5); Gates D–J not started\
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

`ux-detailed-plan.md` §8.1 (September 6) and `ux-experience-architecture.md` list Transfers and Finances as
`FUTURE-BLOCKED / SPEC-ONLY`. That was true then. #31 T0–T2 and #40 T0–T3b2 have since landed, so the rows
below supersede those two labels for this journey.

## 2. Gate A — capability classification

### 2.1 Reads

| # | Element the screens need | Classification | Evidence |
|---|---|---|---|
| R1 | Managed club's transfer ceiling | `DESIGNABLE` | `SeasonLoop.AvailableTransferBudget(clubId)` (`SeasonLoop.cs:742`), the season ceiling; 100,000 at genesis (ERR-040-004) |
| R2 | Spend already committed this window | `DESIGNABLE` | `SeasonLoop.TransfersSnapshot().CommittedSpendThisWindow` (`:770`, a detached clone). Remaining = R1 − R2, exactly the check `TransferCommands.SubmitBid` makes |
| R3 | Window open/closed and its dates | `DESIGNABLE` | `TransfersSnapshot().ActiveWindow` (`OpenWorldDay`, `CloseWorldDay`, `IsWindowOpen(day)`) and `SeasonLoop.CurrentWorldDay` (`:573`). Summer window only; it closes `SummerWindowInSeasonDays` (38) days after the opening fixture |
| R4 | Every club's players: name, age, position, attributes | `DESIGNABLE` | `SeasonLoop.Progression.SquadFor(clubId)` → `PlayerRecord` (`FirstName`, `LastName`, `Age`, `Position`, `Attributes`) |
| R5 | Player current ability | `DESIGNABLE` | `ProgressionEngine.LifecycleView(clubId, playerId).CurrentAbility` |
| R6 | Club names | `DESIGNABLE` | `League.Clubs` (`Club.Name`) |
| R7 | Managed players' contracts (wage, seasons left) | `DESIGNABLE` | `TransfersSnapshot().ContractsSnapshot()` / `TryGetContract`. A player can be un-contracted (ERR-031-009) and stays sellable |
| R8 | Player availability (injury) | `DESIGNABLE` | `PlayerCareerStates.IsAvailable(clubId, playerId)`; an injury travels with a transferred player |
| R9 | Club finances: balance, transfer and wage budget, wage bill | `DESIGNABLE` | `SeasonLoop.FinanceView(clubId)` → `FinancesViewModel` (`Balance`, `TransferBudget`, `WageBudget`, `WageBillAggregate`) |
| R10 | A player's price | `DESIGNABLE` (Q1 answered: estimated value) | No #31 read surface names a price. `PlayerValuation.ValuePlayer(attributes, age)` is #31's public base value; the acceptance pivot is `CounterpartyValue` = base × the counterparty's positional-need multiplier, computed inside `SubmitBid` |
| R11 | Counter-offer amount | `FUTURE-BLOCKED` | `TransferSubmissionOutcome.CounterOffered` carries no amount; only that the fee is inside the per-mille band around value (`NegotiationEngine.EvaluateOffer`). Showing an amount needs a #31 seam (FR-UI-020 / #38 KD-4) |
| R12 | Wage bill matching the contracts | `FUTURE-BLOCKED` | `WageBillAggregate` has no producer at Stage 2 (#40 KD-5): contracts carry wages but the finance view shows a 0 wage bill. Display it as "not tracked yet" or hide it |
| R13 | Season revenue so far | `FUTURE-BLOCKED` | `ClubFinances.SeasonRevenueAccrued` exists but `FinancesViewModel` does not expose it; daily revenue is default-off (#40 T3b2) |

All reads are **sim-thread only** (`SeasonLoop` THREAD SAFETY note). The client must take them on the
P7b career worker and publish an immutable snapshot, as `ClientCareerCoordinator` already does for Career
Home.

### 2.2 Actions

| # | Action | Classification | Evidence |
|---|---|---|---|
| A1 | Buy a player (club, fee, wage, length) | `DESIGNABLE` | `SeasonLoop.SubmitTransferBid(in Offer)` (`:802`), the #31 FR-TX-025 UI seam. Typed outcomes: `Accepted`, `Rejected`, `CounterOffered`, `InsufficientBudget`, `SquadFull` (25), `SquadDepleted` (seller would drop below a matchday squad) |
| A2 | Sell a player to a chosen club | `DESIGNABLE` | Same seam, `Offer.IsBuy = false`, counterparty = buying club; the buyer's need sets its value |
| A3 | Withdraw / cancel an offer | not needed | Bids are synchronous; nothing stays pending (#31 minimal has no negotiation state) |
| A4 | Renew or release a contract | `FUTURE-BLOCKED` | No #31 seam (re-signing is deep tier, §3.7) |
| A5 | AI clubs bidding for the manager's players | `FUTURE-BLOCKED` | No autonomous producer (#31 FR-TX-024) |

Caller-contract failures throw rather than return (F4 window closed, F6 unknown player or club, wrong
direction, malformed terms). The screen must make them unreachable: offer only open-window, valid
choices, never rely on catching them.

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
  - `SquadDepleted` — "That club cannot release him without dropping below a matchday squad."
- **Blocked paths:** window closed (list stays readable, buying disabled, "Next window opens at the start
  of next season"); a career operation running (busy notice, controls disabled).
- **Back:** returns to Career Home. Nothing is pending, so leaving never abandons anything.
- **Completion:** the result message, with the list refreshed from a new snapshot. Not autosaved (Q2):
  the next round's autosave or a manual save keeps the deal; the existing save-compatibility warning applies.

### 3.2 Sell a player

- **Entry:** Transfers → "Your squad" tab.
- **Decision:** pick one of the manager's players, pick a buying club, enter a fee (pre-filled with the estimated value),
  submit.
- **Outcomes:** as in 3.1; `SquadDepleted` here means "You cannot drop below a matchday squad."
  `InsufficientBudget` cannot occur on a sale (AI clubs' budgets are not checked; counterparty-ledger
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
   primary action "Make offer". Disabled with a stated reason when the window is closed, the squad is
   full (buy) or a career operation is running.
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

No spec, code, schema or navigation change is made by this document. Contract renewal, AI-initiated
bids, scouting knowledge (#32) and the counterparty ledger stay out of scope.

## Version history

| Version | Date | Notes |
|---|---|---|
| 0.2 | 2026-11-05 | Owner answers Q1 (b) estimated value, Q2 (b) no autosave, Q3 no counter figure; flows and layout updated. |
| 0.1 | 2026-11-05 | Gate A audit of transfers/finances at `ad05b76`; draft Gate B flows and Gate C layout; owner questions Q1–Q3. |
