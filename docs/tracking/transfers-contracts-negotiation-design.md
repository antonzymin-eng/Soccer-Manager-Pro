# Transfers, Contracts & Negotiation #31 — Design Supplement

> **Created:** July 23, 2026
> **Last Updated:** July 23, 2026 (v0.3 — **PROMOTED**; prior v0.2 AR-1 fix pass 3M+3L).
> **Status:** DESIGN SUPPLEMENT → **PROMOTED** (July 23, 2026) — 11-file section set authored at
> `docs/specs/transfers-contracts-negotiation/` (FR-TX-001..028) → section-file AR-1 (3M+1L) → AR-2 (1L) →
> CONVERGENCE → R-01..R-05 signed → **APPROVED**; `SPEC_INDEX.md` row 31 added (**37 APPROVED**). **One
> approval-time cross-spec back-prop:** ERR-030-004 (the #30 transfers tick-order step-5 null seam);
> `0x23`/85 stays reserved (draw-free); #40/#33/#27/#16 unchanged. Section files are authoritative; this
> supplement is the design-history record. (Original status line follows for history.)
> DESIGN SUPPLEMENT (pre-promotion — no section files, no `SPEC_INDEX.md` row).
> **Candidate spec:** #31 · **FR prefix:** FR-TX (grep-verified unclaimed across `docs/specs/**` — only the roadmap/plan proposal cites it).
> **Master-plan home:** §4.3, §5 (complex clauses) · **Tier:** S2 min → S3 deep · **Wave:** 4 (recruitment/economy cluster — first, owns the reusable negotiation seam).
> **Determinism (proposed):** `DOMAIN_TAG_TRANSFERS` / `SubsystemOrdinals.Transfers` = `0x23` / `85` — the roadmap §6 off-pitch reservation, **already present as the `_RESERVED_0x23_` placeholder row** in #16 §3.4 (verified `deterministic-sim/section-3.md:267`). **Stays RESERVED at approval** (minimal tier is draw-free — the #40 ERR-040-001 / #29 reservation precedent); promotes at the deep tier's first stochastic draw (rival-AI-club bidding).
> **Source plan:** `docs/tracking/spec-plans/spec-31-transfers-contracts-negotiation.md` v0.1.

---

## 0. Scope

The **recruitment engine**: transfer windows, player search over #27's pool, bids, contract terms
(wage/length, deep-tier clauses), and a **counterparty negotiation loop** — advanced on the **world tick**
(`WorldClock`, one day = one `worldTick` — never the 10 Hz/60 Hz match loops), constrained by #40's club
budgets, and committed back through #30's roster owner. Minimal = master plan §4.3 **accept/reject inside a
summer window** against a **deterministic counterparty valuation** (no agents, no clauses, no rival bids); the
deep tier has that same valuation identity **read #33 personality** and adds agents, clauses, loans, wage
structures, and stochastic rival bidding on one code path.

**Out of scope (owned elsewhere, referenced as seams):**
- **The economy (#40 Club Finances).** #40 owns budgets/wages as the **read-only constraint** #31 reads
  (`AvailableTransferBudget`) and the **single mutation path** #31 posts accepted deals through
  (`ApplyTransaction`). #31 **never** writes `ClubFinances` fields directly and keeps **no parallel cash
  ledger** (KD-2).
- **Scouting / fog-of-war (#32).** #32 **reuses** #31's negotiation machinery for bids on scouted players but
  owns the per-manager knowledge view; #31 exposes the reusable offer/response seam (KD-3) and builds no
  scouting.
- **Counterparty personality (#33).** #33 supplies the psychology #31's valuation later reads
  (`PersonalityProfile` traits, `MoraleOf`) — **read-only, deferred** (#33 §7.3 names #31 a read-only
  consumer; no accessor is wired ahead of the producer, FR-LW-031). Minimal valuation makes **no #33 read**;
  it is the exact identity #33 modulates (KD-1).
- **The on-disk season save codec (#30).** #30 owns `SeasonSaveCodec` + the outer `SEASON_SAVE_FORMAT_VERSION`;
  #31 lands its state as an **opaque, independently version-gated `TRANSFERS_SAVE_FORMAT_VERSION` sub-blob**
  the codec never parses (KD-4).
- **The season day-advance loop + roster ownership (#30).** #30 owns `RunWorldTickInFixedOrder` and the
  roster lifecycle (`SeasonLoop`/`SeasonState`, `RollToNextSeason`); it **invokes** #31 at a new pre-declared
  tick-order slot and owns the roster-commit + PlayerId **re-key** a transfer triggers (KD-6/KD-7). #31 never
  references #30.

## 1. What exists vs. what #31 adds

**Exists (verified against source / approved specs):**
- **#40 Club Finances & Economy (APPROVED, FR-FN)** — the constraint + commit surface, verbatim:
  - `struct ClubFinances { long Balance; long TransferBudget; long WageBudget; long WageBillAggregate;
    long SeasonRevenueAccrued; long FfpBalanceWindow; }` — all **integer** (`long`).
    `TransferBudget`/`WageBudget` are **static per-season ceilings** (NOT decremented by spend; reset at the
    season boundary by `SettleFinances`); `Balance` is club cash (may go negative); `WageBillAggregate` is the
    current total wage **liability**.
  - **Read:** `AvailableTransferBudget(in ClubFinances f) → long` returns `f.TransferBudget` (pure passthrough;
    does **not** fold in `Balance` — balance-awareness is a #31 policy decision, KD-2). `T-FN-BOUND-001` locks
    it non-mutating.
  - **Write (the one #40-owned mutation path #31 MUST call):** `ApplyTransaction(ref ClubFinances f, in
    FinanceTransaction txn)` — a `TransferFee`/`General` line moves `Balance` **only**; a `PlayerWage`/
    `StaffWage` line moves `WageBillAggregate` **only**; it **never** touches `TransferBudget`/`WageBudget`
    (those are `SettleFinances`-only, season-boundary). `FinanceTransaction { FinanceTransactionKind Kind
    (Debit/Credit); FinanceLineItem LineItem (TransferFee/General/PlayerWage/StaffWage); long Amount (≥0, sign
    in Kind); int BoardModifier (identity per-mille 1000); }`.
  - `FINANCE_SAVE_FORMAT_VERSION` = 1 (season-save sub-blob). Domain: `_RESERVED_0x29_` / ordinal 91
    (RESERVED — #40 minimal is draw-free).
- **#33 Personalities/Morale (APPROVED, FR-HS)** — the deferred psychology read:
  `struct PersonalityProfile { byte Professionalism, Ambition, Loyalty, Temperament, Determination; }` each
  `[1,20]`, `Create()` = all `TRAIT_NEUTRAL`; `MoraleOf(in MoraleState) → int`. **§7.3 seam contract (verbatim):**
  "*#31/#35/#45 (future): read-only morale-accessor consumers; MUST NOT write #33 state. #46 is the sole
  consumer that writes #33 morale — deferred.*" #31 binds to these **read-only**, **deferred** (no interface
  built; consumed only at the deep-tier personality modulation).
- **#30 Season & Competition Loop (APPROVED, FR-SN)** — the invoker/root:
  - Outer `SEASON_SAVE_FORMAT_VERSION` = 2 (bumped 1→2 by #30 to add its own `SeasonStateCodec` sub-blob);
    inner `SEASON_STATE_FORMAT_VERSION`. `SeasonSaveCodec` frames opaque sub-blobs (§6).
  - `SeasonState { int ManagedClubId; ulong Seed; int[] ClubIds; Fixtures; Table; SeasonCalendar Calendar;
    Board; }`; `SeasonCalendar { int NextRoundIndex; int[] RoundToDay; }` — **a fixture-round cursor, NOT a
    transfer-window concept** (there is *no* window-open/close anywhere in #30 — #31 owns it, KD-6).
  - `RunWorldTickInFixedOrder()` pinned slot list (FR-SN-034): **1 progression(#28) · 2 training(#29) ·
    3 human-systems(#33) · 4 injuries(#41) · 5 `WorldStore.AdvanceDay()`** — **no slot for #31** (contrast
    #33, which *filled* a pre-declared slot). #30 is **producer-only** for #22 (FR-SN-017); the managed club's
    roster resolves via `ISquadProvider.ResolveByClubId` → `ConfigureSquads`; the roster lifecycle
    (regen-insert / retire-remove of per-`PlayerId` state) is owned at `RollToNextSeason()`.
    `DOMAIN_TAG_SEASON_LOOP = 0x22` / ordinal 84.
- **#27 Squad/Player Data (APPROVED, FR-SQ; `src/player-database/` built)** — the pool + valuation inputs:
  `struct PlayerRecord { int PlayerId; string FirstName, LastName; int Age; PlayerPosition Position;
  PlayerAttributes Attributes; }`; **`PlayerId = clubId * CLUB_SQUAD_SIZE + localIndex`** (club-scoped,
  globally unique); `CLUB_SQUAD_SIZE = 25` [FIXED]; `Squad { int ClubId; ≤25 PlayerRecord; }`;
  `PlayerAttributes` = 31 `int[1,20]` fields (+ `WeakFootRating [1,5]`). **No `Overall`/`CA`/`PA` on #27** —
  CA (derived current-ability summary) / PA (ceiling) are **#28-owned** career-state keyed by `PlayerId`.
- **#16 §3.4** — `_RESERVED_0x23_` / ordinal `85` placeholder row **already exists**, held for #31.
- **`SeasonSaveCodec` fail-loud posture** (`src/season-save/SeasonSaveCodec.cs`) — the exact convention #31's
  sub-blob codec mirrors: `Require(offset, need, total, what)` compares against **`total − offset`** (never
  `offset + need`, so a near-`int.MaxValue` prefix cannot wrap past the guard); version-mismatch throw;
  0-or-1 flag validity; per-read `Require` before consuming bytes; trailing-byte guard (`if (o != len) throw`).

**#31 adds:** a deterministic **counterparty valuation** function over #27 attributes + age (the minimal
identity, KD-1); a **transfer-window** model over #30's calendar (a new concept #30 lacks, KD-6); a reusable
**offer/response negotiation seam** (KD-3); durable **`Contract`** state (wage/length; deep clauses append) +
in-flight negotiation + window cursor + a **committed-spend-this-window** counter, all in a
`TRANSFERS_SAVE_FORMAT_VERSION` season-save sub-blob (KD-4); **transfer-action command APIs** (the #38-UI
seams, the `SetTeamTactic` command discipline); and a **roster-commit request** through #30 that re-keys the
transferred player's `PlayerId` and fires the roster-lifecycle hook each per-`PlayerId` system reacts to
(KD-7). **No RNG stream at minimal** (draw-free — rival bidding is the deep-tier first draw).

## 2. Staging (minimal-first → deep, one code path)

- **Stage-2 minimal** — the counterparty is a **pure deterministic valuation function** (player value from
  #27 attributes + an age-curve, plus a club-need signal → accept/reject an offer) inside a **single summer
  window**. Both directions are supported but **always manager-initiated**: a **buy** (the manager bids on
  another club's player → that club's deterministic valuation accepts/rejects) and a **sell** (the manager
  lists a player → a deterministic AI-buyer valuation produces an accept/counter offer). **No agents, no
  clauses, no multi-day negotiation** (an offer resolves synchronously), and — critically — **no
  *autonomous* AI-club bidding** (AI clubs proactively initiating transfers without a manager prompt; that
  needs the daily tick + stochastic target selection and is the deferred deep-tier draw, KD-5). Contracts
  carry **wage + length only**. Every valuation is **integer per-mille and deterministic — no draw** (KD-5).
  By default (no manager transfer action, no autonomous AI activity) **zero transfers occur** ⇒ a season is
  byte-identical to pre-#31 (KD-8).
- **Stage-3 deep** — the same valuation identity **reads #33 personality** (a `personalityMult` bias, not a
  replacement path); **agents/clauses/loans/wage-structures** append to the same `Contract` record;
  **multi-day in-flight negotiation** runs at the pre-declared tick-order slot; **stochastic rival-AI-club
  bidding** is the first draw site (promotes `0x23`/85); **CA/PA-from-#28** refines the valuation input; and
  the **#34 staff influence** on valuation/negotiation attaches via an identity ×1.0 routing seam — all on
  **one code path**, each defaulting to its Stage-2 identity via a config dial (`deepTransfersEnabled` off ⇒
  pure valuation, no #33 read, no draws, wage+length contracts, synchronous single-counterparty resolution).

**One code path (KD-8):** the pure valuation, synchronous single-counterparty resolution, wage+length
contract, and draw-free posture are the exact identities the deep tier modulates — the #21/#27/#40/#41
default-behaviour-neutral discipline, not a rewrite.

## 3. Dependencies & reference direction (one-way, no cycle)

- **#30 → #31** — the day-advance loop *invokes* #31's world-tick step at a **new pre-declared tick-order
  slot** (a documented null seam #30 inserts, the #41 ERR-030-002 pattern — contrast #33's *fill* of an
  existing slot), passing committed season/calendar state as **values**; the **composition root** routes
  #31's transfer-action commands and the roster-commit request. #30 owns the roster + calendar; #31 reads them
  read-only and **never** references #30.
- **#31 → #40** — reads the budget ceiling via `AvailableTransferBudget` (read-only) and posts accepted deals
  via `ApplyTransaction` (the one #40 mutation path). **One-directional** — #40 never reads #31 (KD-2).
- **#31 → #27** — reads `PlayerRecord`/`PlayerAttributes` for valuation and player search; a committed
  transfer moves a `PlayerRecord` between `Squad`s via the #30 roster owner (#31 requests, does not mutate #27
  directly, KD-7).
- **#31 → #16** — the determinism namespace + world-tick `DeterministicRngService` (only when the deep tier
  draws rival bids).
- **Consumers (deferred, no interface built):** **#32** (scouting bids) and **#34** (staff hiring, if it
  reuses per its own KD) consume the KD-3 offer/response seam #31 authors once; **#33** personality +
  **#28** CA/PA feed the deep-tier valuation modulation. All deferred (FR-LW-031) — #31 builds none of them,
  and its own #33/#34 influence is a ×1.0 identity routing seam until those producers wire up.

Reference DAG: `compositionRoot → {#30, #31}`, `#31 → {#40, #27, #16}`. **Acyclic.** No sim assembly
references #31's consumers; #40/#27 stay schema-untouched (#31 reads existing surfaces; the sole #27-side
effect is the #30-owned roster move).

## 4. Persistent state & save impact (KD-4)

Adds an opaque, independently version-gated **transfers sub-blob** (`TRANSFERS_SAVE_FORMAT_VERSION` [FIXED] =
1) composed into #30's season save via the `SeasonSaveCodec` pattern — **one** sub-blob holding **both**
durable and season-scoped state (this **supersedes the plan §4 "and/or `WORLD_STORE_FORMAT_VERSION`" guess**;
§7 KD-4 gives the rationale — contracts are per-`PlayerId` career-state #30's loop advances, exactly like #40
`Balance`, which chose the season-save sub-blob). The block carries, per club:
- **Durable across seasons:** active `Contract` records (`PlayerId`, wage-per-period, expiry
  world-day/season; deep clauses append). Contracts survive `RollToNextSeason` — they are career state, so
  the season-save sub-blob (which *is* the multi-season career save) is their home, not a separate world-store
  bump.
- **Season-scoped:** the transfer-window cursor, in-flight negotiations (deep), and the
  **committed-transfer-spend-this-window** counter (the #31-owned affordability accumulator — see KD-2; reset
  at the season boundary alongside #40's `SettleFinances`).

Mirror the `SeasonSaveCodec` fail-loud posture exactly (`Require` bound against `total − offset`,
version-mismatch throw, per-read `Require`, trailing-byte guard). **No `WORLD_STORE_FORMAT_VERSION` bump.** The
composing outer `SEASON_SAVE_FORMAT_VERSION` bump (already 2 from #30 → **3** for #31) is coordinated with #30
at the T-phase, exactly as #28/#29/#40/#41/#33 defer their outer bump; the codec never parses the sub-blob.
**No RNG cursor is serialized** — minimal is draw-free; the deep-tier rival-bid draws are **position-independent
keyed draws** on `(clubId/playerId, worldDay, purpose)` (the #41/#28/#30 off-pitch keyed-draw precedent), so
even the deep tier persists no free-running cursor. Round-trip determinism required, **including a
mid-window and (deep) mid-negotiation save**. **Roster-lifecycle in lockstep with #30/#28:** a retirement/
regen at the season boundary removes/inserts the affected `PlayerId`'s contract; a **transfer** re-keys it
(KD-7).

## 5. Determinism (KD-5 — single world clock, draw-free minimal)

**All #31 state advances on the WORLD tick** (window open/close, and — deep — day-by-day negotiation
progress), at #30's pre-declared slot, from **committed** values #30 routes in. The **minimal tier makes no
stochastic draw** — the counterparty valuation is a pure deterministic integer-per-mille function; a bid
resolves synchronously against it. Consequently:
- **`0x23`/85 stays `_RESERVED_0x23_`** at #31's approval (no `DOMAIN_TAG_TRANSFERS` promotion, **no #16
  spec-text change**) — the **draw-free reserved-not-promoted precedent of #40 (ERR-040-001, `_RESERVED_0x29_`)
  / #33 / #29 (`_RESERVED_0x21_`)**, all of whose minimal tiers register no stream. It promotes to a live
  domain tag + `SubsystemOrdinals.Transfers = 85` only at the **deep tier's first draw** (stochastic
  rival-AI-club bidding / agent demands), with that stream registered on the world-tick
  `DeterministicRngService` at #31 T3, keyed on `(clubId, playerId, worldDay, purpose)`.
- **Save→restore is byte-exact with nothing to continue** — no cursor at minimal, and keyed draws (no cursor)
  at deep.
- **Stream independence (trivially):** registering **no** stream leaves every existing stream's cursor
  byte-identical (the #40 `_RESERVED_0x29_` / T-FN-NEU-003 property, stronger than #41's one registered stream).

Integer-per-mille internally (valuation, wage arithmetic, spend accounting); there is **no float in #31** —
unlike #33's single `edgePermille/1000f` mirror boundary, #31 exchanges only integer `long` amounts with #40
(`FinanceTransaction.Amount`) and integer per-mille valuations, so the integer posture is total. One clock
(world), so no determinism-ordering fragility between loops can arise.

## 6. Primary surfaces (proposed → pinned in §4 of the section files)

```csharp
// #31-owned durable contract state (serialized, KD-4). Integer amounts; deep clauses APPEND (no minimal rewrite).
public struct Contract
{
    public int  PlayerId;              // #27 club-scoped id (clubId*CLUB_SQUAD_SIZE+localIndex)
    public long WagePerPeriod;         // integer currency/period (the #40 FinanceTransaction.Amount posture)
    public uint ExpiryWorldDay;        // contract end on the WorldClock; HS-style sentinel discipline for "unset"
    // deep-tier: clauses/loan/wage-structure APPEND here behind `deepTransfersEnabled` (no minimal schema move)
}

// The reusable offer/response seam (KD-3) — authored ONCE; #32 (scouting bids) and #34 (staff hiring) consume
// it generically over a counterparty-inputs struct. Minimal counterparty = the OTHER club's deterministic
// valuation (selling club on a buy, buying club on a sell); no #33 read (that is the deep-tier modulation of THIS identity, KD-1).
public readonly struct Offer            { public int PlayerId; public long Fee; public long WagePerPeriod; public int LengthSeasons; }
public enum NegotiationOutcome          { Rejected = 0, Accepted, CounterOffered /* deep */ }

// KD-1 — the deterministic counterparty valuation: the Stage-2 IDENTITY the #33 personality layer modulates.
// Pure integer per-mille over #27 attributes + age + a club-need signal. NO #33 read, NO #28 CA read at minimal.
public static long ValuePlayerPermille(in PlayerAttributes attrs, int age, int clubNeedPermille);
public static NegotiationOutcome EvaluateOffer(in Offer offer, long counterpartyValuation);  // draw-free at minimal

// KD-2 — the #40 boundary. Read the ceiling; commit via #40's ApplyTransaction; track committed-spend-this-window
// (#31-owned, against the STATIC ceiling — ApplyTransaction never decrements TransferBudget; FR-FN-004 says #40
// deliberately holds no "remaining budget net of committed" concept, so #31 must). No parallel cash ledger.
//   VALIDATE-ALL-FIRST (no mutation until every gate passes — the #33/#27/#40 validate-before-write discipline):
//     window open (KD-6) AND counterparty accepts (KD-1) AND destination Squad has a free localIndex (KD-7)
//     AND (buy) bid.Fee <= AvailableTransferBudget(finances) - committedSpendThisWindow   (else fail loud)
//   THEN commit atomically (finance txns + roster move + hook dispatch, KD-7):
//     BUY:  ApplyTransaction(ref finances, {Debit,  TransferFee, fee});   // Balance -=
//           ApplyTransaction(ref finances, {Debit,  PlayerWage,  inWage}); // WageBillAggregate += (FR-FN-016)
//           committedSpendThisWindow += fee;
//     SELL: ApplyTransaction(ref finances, {Credit, TransferFee, fee});   // Balance +=
//           ApplyTransaction(ref finances, {Credit, PlayerWage,  outWage}); // WageBillAggregate -= (FR-FN-016)

// KD-6 — the transfer-window model (#31-owned; #30 has none). Deterministic from #30's SeasonCalendar (read-only).
public readonly struct TransferWindow   { public uint OpenWorldDay, CloseWorldDay; }   // minimal = one summer window
public static bool IsWindowOpen(in TransferWindow w, uint worldDay);   // action outside an open window fails loud

// KD-7 — roster commit + PlayerId re-key. #31 REQUESTS; #30 owns the re-key + roster-lifecycle-hook dispatch.
// #31 migrates only its OWN keyed Contract state in the hook (#28 CA/PA, #33 morale migrate their own).
public static void OnPlayerRekeyed(int oldPlayerId, int newPlayerId, /* transfers store */ ref TransfersState s);  // moves the Contract

// Transfer-action command APIs (the #38-UI seams; UI never mutates #31 state directly — the SetTeamTactic discipline).
public /* command */ NegotiationOutcome SubmitBid(int managerClubId, in Offer offer /* , world ctx */);  // window + budget gated
```

## 7. Key design decisions

- **KD-1 (the minimal valuation as the exact identity #33 modulates — the plan's headline KD).** The Stage-2
  counterparty is a **pure deterministic integer-per-mille valuation** over inputs #31 already has on #27: a
  **mean-attribute rating** (the `LineupSelector` mean-attribute precedent already in the codebase) × an
  **age-curve multiplier** (peak-age band neutral, decline past ~30, discount for the very young — the master
  plan §4.3 shape), scaled by a **club-need signal** (position scarcity in the buying squad). It makes **no
  #33 read and no #28 CA read**: personality enters at the deep tier as a **multiplicative `personalityMult`
  bias** on this identity (a loyal/ambitious seller holds out for more; read-only via #33's deferred
  accessors), **never a replacement path** — a `#33`-unconfigured negotiation yields **exactly** the
  deterministic valuation (`deepTransfersEnabled` off ⇒ `personalityMult ≡ 1000‰`). CA/PA-from-#28 is a
  **recorded deep-tier refinement** of the valuation input, deliberately **not** a minimal dependency (keeps
  #31's upstream exactly the plan's #27/#30/#40 set — #28 is not a plan upstream, and a mean-attribute rating
  over #27 is a complete Stage-2 identity). This is the "author the §4.3 valuation as the identity the
  personality layer later modulates" contract, made concrete.

- **KD-2 (the #40 boundary — read the ceiling, commit through `ApplyTransaction`, no parallel ledger,
  validate-before-commit).** #31 reads the spending constraint via `AvailableTransferBudget(in ClubFinances)
  → TransferBudget` (read-only; it does **not** fold in `Balance`, so **#31 owns the affordability policy**:
  a buy's `Fee` must fit `TransferBudget − committedSpendThisWindow`, else **fail loud**). On an accepted
  deal #31 posts through the **single #40-owned mutation path** `ApplyTransaction`: a **buy** posts
  `{Debit, TransferFee, fee}` (moves `Balance`) + `{Debit, PlayerWage, inWage}` (increases
  `WageBillAggregate`, FR-FN-016); a **sell** posts `{Credit, TransferFee, fee}` + `{Credit, PlayerWage,
  outWage}` (decreases `WageBillAggregate`). #31 posting the first `PlayerWage` line is precisely the
  arrival of the FR-FN-015 wage producer #40 anticipated (`WageBillAggregate = 0` pre-#31 by design). #31
  **MUST NOT** write `ClubFinances` fields directly, **MUST NOT** maintain a parallel cash ledger, and **MUST
  NOT** expect `TransferBudget` to decrement as calls accumulate — the ceiling is static (reset by #40's
  `SettleFinances` at the season boundary, and **FR-FN-004 deliberately gives #40 no "remaining budget net of
  committed" concept**), so #31 keeps its **own** `committedSpendThisWindow` counter (a spend-against-ceiling
  accumulator, distinct from #40's `Balance`/`WageBillAggregate` cash truth) purely for the pre-bid
  affordability gate. **Atomicity (the validate-before-write discipline, #33 F4 / #27 / #40 F-gates):**
  **every** gate — window open (KD-6), counterparty accepts (KD-1), destination `Squad` has a free
  `localIndex` (KD-7), and (buy) the affordability inequality — MUST pass **before any mutation**; the commit
  (finance transactions + roster move + hook dispatch) is then a single atomic block, so a failed gate leaves
  finances **and** roster untouched (no half-written deal: no debit for a player never received).
  **One-directional:** #31 reads the ceiling + posts transactions; #40 never reads #31. This resolves the
  plan's KD-2 (write seam = #40's `ApplyTransaction`; the *money* does not go through #30's roster commit —
  only the *player move* does, KD-7).

- **KD-3 (the reusable negotiation seam — authored once for #32/#34).** #31 defines the offer/response surface
  generically: `Offer` + `NegotiationOutcome` + `EvaluateOffer(in Offer, long counterpartyValuation)` (the
  synchronous minimal resolution) and the deep-tier multi-day in-flight negotiation state machine, all keyed
  on a **counterparty-valuation input** the caller supplies. **#32** (bids on scouted players) and **#34**
  (staff hiring, if it reuses per its own KD) consume this seam with their own valuation inputs — authored
  once here so neither duplicates it (the plan's KD-3 load-bearing seam). **No #32/#34 interface is built**
  (FR-LW-031); #31's own #34-staff-influence-on-valuation is a **deferred ×1.0 routing seam**
  (identity-until-producer, the #21 `TacticTranslation` / #41 `MedicalModifier` pattern). Getting this
  factoring right is a §10 risk — a counterparty-generic seam is what lets #32/#34 reuse without a rewrite.

- **KD-4 (persistence — one season-save sub-blob; supersedes the plan's `WORLD_STORE`/`and-or` guess).**
  `TRANSFERS_SAVE_FORMAT_VERSION` [FIXED] = 1 opaque sub-blob composed into `SeasonSaveCodec`, holding **both**
  durable contracts **and** season-scoped window/negotiation/spend state — **not** a
  `WORLD_STORE_FORMAT_VERSION` bump, and **not** split across two version gates. Rationale: contracts are
  per-`PlayerId` **career state** #30's day-advance loop owns and advances, exactly like #40's `Balance`
  (which persists across seasons yet chose the `FINANCE_SAVE_FORMAT_VERSION` **season-save** sub-blob, not the
  world store) and #28's CA/PA and #33's morale — the season save **is** the multi-season career save, so
  "durable across seasons" does not imply "world store". Splitting negotiation-state into the season blob and
  contracts into the world blob (the plan's "and/or") would create a **two-version-gate desync risk** the
  plan itself flags (§9) for no benefit; one sub-blob dissolves it. Clause/loan/wage-structure representation
  (deep) **appends** to the `Contract` record behind `deepTransfersEnabled` — no minimal schema move (the
  #41/#28 deep-fields-append discipline). Mirror the `SeasonSaveCodec` fail-loud posture; serialize-don't-
  regenerate; outer `SEASON_SAVE_FORMAT_VERSION` bump (→3) coordinated with #30 at T-phase.

- **KD-5 (determinism — draw-free minimal; rival bidding is the deep-tier first draw).** Answers the plan's
  KD-5 ("does rival-AI bidding draw at S2 or defer to S3"): **defer entirely to S3.** Minimal is a pure
  single-counterparty valuation with **zero draw**, so `0x23`/85 **stays `_RESERVED_0x23_`** at approval (no
  #16 change — the #40/#29 precedent) and every existing stream's cursor stays byte-identical. Stochastic
  rival-AI-club bidding + agent demands are the deep tier's **first draw site**, promoting
  `DOMAIN_TAG_TRANSFERS = 0x23` / `SubsystemOrdinals.Transfers = 85` at T3 (spec-text-first, ERR-016), keyed
  position-independently on `(clubId, playerId, worldDay, purpose)` — no serialized cursor even at deep.

- **KD-6 (the transfer-window model — #31-owned, a concept #30 lacks).** #30 has **no** window-open/close
  concept (its `SeasonCalendar` is a fixture-round cursor). #31 owns it: a `TransferWindow`
  `[OpenWorldDay, CloseWorldDay]` derived **deterministically** from #30's `SeasonCalendar` (read-only) —
  minimal = **one summer window** at the season boundary. The window cursor is **season state** (in the
  sub-blob); a transfer action outside an open window **fails loud** (`IsWindowOpen`). #31 reads the calendar
  read-only and never mutates it — #30 owns the calendar.

- **KD-7 (roster commit + PlayerId re-key — a NEW #30 mid-season entry point; #31 requests, #30 owns).**
  Because `PlayerId = clubId*CLUB_SQUAD_SIZE+localIndex` is **club-scoped** (verified `PlayerRecord.cs`), a
  completed transfer **re-keys** the player (new club ⇒ new id). This is the load-bearing structural fact of
  #31 (§10 headline): #30's per-`PlayerId` roster churn today runs **only at `RollToNextSeason()`** (the
  boundary); `RunWorldTickInFixedOrder` mutates no roster. A transfer commits **mid-season**, so the
  roster-commit seam is a **genuinely new #30 capability — a mid-season roster-mutation entry point** — not a
  mere generalization of the boundary lifecycle, and it **forces #28 (CA/PA) and #33 (morale), which also key
  by `PlayerId` and today migrate only at the boundary, to become mid-season-migration-capable.** #31 does
  **not** own the re-key or other systems' migration; it **requests** the commit through the
  **#30/composition-root seam**, which (a) allocates a free `localIndex` in the destination `Squad`
  (**fail loud** if full), (b) moves the #27 `PlayerRecord`, and (c) **dispatches a roster-move hook** each
  per-`PlayerId` system subscribes to — **#31 migrates only its own `Contract`** (`OnPlayerRekeyed`), #28
  migrates CA/PA, #33 migrates morale, #40's wage liability already rides the KD-2 transactions. The seam is
  a **T-phase #30 coordination** (declared as this KD at approval; the mid-season entry point + hook land at
  T2 via ERR-030-NNN, with #28/#33 subscribing their own keyed migration), **not** double-owned by #31. The
  commit is atomic with the KD-2 finance post (validate-all-first), so a full destination squad or a failed
  affordability gate aborts the whole deal with no partial mutation.

- **KD-8 (behaviour-neutral identity + stream independence + the command/activation boundary).** #31's minimal
  addition is neutral in three senses: (a) **stream independence** — registering **no** stream leaves every
  existing cursor byte-identical (the #40 property); (b) **no default transfer activity** — rival AI bidding
  is deferred (KD-5) and there is no other autonomous transfer producer, so with no manager action **zero
  transfers occur** and a season is byte-identical to pre-#31 (the #27 T0 "records exist but unconsumed"
  class); (c) **the tick-order slot is a documented null seam at minimal** (window open/close is a cheap
  calendar predicate; multi-day negotiation is deep) so wiring it changes no default behaviour. **The
  command/activation boundary is explicit:** a **manager-initiated bid is an explicit command**
  (`SubmitBid`, the `SetTeamTactic` discipline) that legitimately moves a player and posts a `FinanceTransaction`
  — that is *manager-driven behaviour*, not a neutrality violation, exactly as `SetTeamTactic` lighting up a
  non-default tactic is not. Deferred consumers/producers (#32/#34 seam, #33 personality, #28 CA, #34 staff
  influence) all default to identity seams. The deep tier extends the pure-valuation / synchronous /
  wage+length / draw-free surface, never rewrites it.

## 8. Cross-spec back-props

**At approval: ONE cross-spec spec-text back-prop** (cleaner than #40's two and #41's two — #31 is draw-free
so there is no #16 promotion, unlike #40/#41):
- **#30 — insert a transfers tick-order null-seam slot** (ERR-030-NNN). `RunWorldTickInFixedOrder`'s pinned
  list (verified `season-competition-loop/section-3.md`: 1 progression · 2 training · 3 human-systems ·
  4 injuries · 5 `WorldStore.AdvanceDay()`) gains a **new documented null seam** for transfers — proposed as
  **new slot 5, after injuries and before `AdvanceDay`** (so a committed roster move settles before the world
  ticks; the exact position is a #30-owned decision the back-prop coordinates), pushing `AdvanceDay` to 6.
  FR-SN-034 enumerates null seams for **#28/#29/#33/#41 only** (not #31), so this is a genuine **insertion**
  (the #41 ERR-030-002 precedent, which appended #41 as step 4) — *not* a fill of a pre-declared slot
  (contrast #33's zero-back-prop, which had slot 3 reserved ahead of it). **A deliberate difference from #41:**
  #41's slot is *used at #41 minimal* (recovery countdown + occurrence draw); **#31's slot is a deep-tier
  position reservation** — minimal transfers are command-driven (window open/close is a cheap calendar
  predicate, no daily work), so the slot is empty until the deep tier's daily in-flight-negotiation /
  rival-bid processing. It is declared **now** (not deferred to T3) per the reserve-ahead discipline, so the
  deep daily-processing lands without a future tick-order re-pin — the FR-SN-034 "documented position, never
  an invented interface" contract.
- **#16 §3.4** — **no change.** `_RESERVED_0x23_`/85 already exists and stays reserved (draw-free minimal,
  KD-5) — the **#40 (ERR-040-001, reservation-not-promotion) / #33 / #29** draw-free precedent. **Contrast
  #41 (ERR-041-001)** — the only member of the cluster that *promoted* a real tag (`0x2A`), because #41 draws
  (`injuries.occurrence`).
- **#40, #33, #27** — **no change.** #31 reads their existing surfaces (`AvailableTransferBudget` /
  `ApplyTransaction`; `MoraleOf` / `PersonalityProfile` deferred; `PlayerRecord` / `Squad`); the #40 §7.3 /
  #33 §7.3 seam contracts already name #31 as the consumer. #31 §8 cites those as the producer/consumer sides
  of the existing cross-references.

**At the #31 T-phase (deferred, lands with code — the #28/#29/#40/#41/#33 deferred-coordination precedent):**
- **#30** — (a) the outer `SEASON_SAVE_FORMAT_VERSION` bump (→3) composing the new sub-blob (coordinated at
  T1, as every prior off-pitch spec defers its outer bump); (b) the **roster-commit + PlayerId-re-key seam**
  (KD-7) — a #30 code addition adding a **new mid-season roster-mutation entry point + roster-move hook**
  (distinct from the season-boundary lifecycle; recorded ERR-030-NNN at that wiring, T2), which #28/#33 also
  subscribe to for their own keyed state.
- **#16** — `DOMAIN_TAG_TRANSFERS = 0x23` / `SubsystemOrdinals.Transfers = 85` promotes at #31 **T3** (the
  first deep-tier rival-bid draw), spec-text-first, with the stream registered at the draw site.

## 9. Test focus

- **Behaviour-neutral identity (KD-8, the headline).** A season with **no manager transfer action** advances
  **byte-identical** to pre-#31 (no autonomous transfer producer at minimal; no stream registered). Two-run
  determinism of a full window's transfer activity from a fixed world seed once the manager *does* act.
- **KD-1 valuation identity.** `ValuePlayerPermille` is a pure integer function of #27 attributes + age +
  club-need (no #33 read, no #28 read, no draw); a `#33`-unconfigured `EvaluateOffer` reproduces the
  deterministic valuation exactly (`personalityMult ≡ 1000‰`).
- **KD-2 #40 boundary.** A bid over `AvailableTransferBudget − committedSpendThisWindow` **fails loud**; an
  accepted deal posts exactly a `{Debit,TransferFee}` (moves `Balance` only) + a `{Debit,PlayerWage}` (moves
  `WageBillAggregate` only) via `ApplyTransaction`; #31 writes **no** `ClubFinances` field directly and keeps
  no parallel cash ledger (a static/reflection assertion on the boundary); `TransferBudget` is unchanged by
  the deal (ceiling is `SettleFinances`-only).
- **KD-4 save round-trip.** `Contract` + window cursor + `committedSpendThisWindow` restore **field-identical**
  across a **mid-window** save and a **mid-`RollToNextSeason`** boundary (contracts survive; window/spend
  reset); the serialized block contains **no** `RngCursor` (schema-shape assertion, KD-5); fail-loud on bad
  `TRANSFERS_SAVE_FORMAT_VERSION` / out-of-bounds length prefix (the overflow-safe `total − offset` `Require`)
  / trailing bytes.
- **KD-6 window model.** `IsWindowOpen` is a deterministic predicate over #30's `SeasonCalendar`; a
  `SubmitBid` outside an open window fails loud; the window is one summer window at minimal.
- **KD-7 roster re-key.** A committed transfer re-keys the `PlayerId` (destination `clubId*25+freeLocalIndex`)
  and moves the `Contract` from old→new id via `OnPlayerRekeyed` with no orphaned/duplicated contract; a full
  destination `Squad` **fails loud** (no free localIndex); #31 migrates only its own state (the hook dispatch
  to #28/#33 is #30-owned, asserted not double-owned).
- **KD-3 reusable seam.** `EvaluateOffer` is generic over a `counterpartyValuation` input (a #32/#34
  consumer passes its own); #31 builds no #32/#34 interface (grep — no `FR-SC`/`FR-ST` reference).
- **Integer posture (KD-5).** Every `Contract`/valuation/spend field is integer; #31 introduces **no** float
  (static/reflection assertion — stronger than #33, which has one mirror-boundary float).
- **Fail-loud roster/pool.** A bid on a `PlayerId` outside #27's club universe, a malformed `Contract`, or a
  negotiation action outside a window fails loud at the consuming seam (the #27 `SquadFileLoader` / #40 F-gate
  precedent).

## 10. Risks

- **Valuation identity leaks a #33/#28 read (headline).** De-risked by KD-1: the minimal valuation is a pure
  function of #27 attributes + age only; personality/CA are deep-tier **multiplicative** modulations of that
  identity, gated off by `deepTransfersEnabled` (≡ ×1000‰). The `#33`-unconfigured-reproduces-valuation test
  is the lock.
- **Two-way coupling with #40 (parallel ledger / direct writes).** Mitigated by KD-2: #31 reads the ceiling
  and posts through the one `ApplyTransaction` path; the only #31-owned money-adjacent state is the
  spend-against-ceiling counter (not a cash truth). No #40→#31 read.
- **The reusable seam (KD-3) is load-bearing for #32/#34.** A counterparty-generic offer/response surface is
  the mitigation — a poorly-factored (transfer-specific) seam forces duplication in #32/#34. Authored once,
  keyed on caller-supplied valuation inputs.
- **PlayerId re-key strands per-`PlayerId` state (KD-7, the structural headline).** The re-key is intrinsic
  to #27's club-scoped id and forces a **new #30 mid-season roster-mutation entry point** (today's per-id
  churn is season-boundary-only) plus mid-season migration in #28/#33. Mitigated by routing through a single
  #30-owned roster-move hook each per-`PlayerId` system subscribes to, atomic with the KD-2 finance post, so
  #28/#33/#31 each migrate their own keyed state or the whole deal aborts; #31 owns only the `Contract` move.
  This is the T-phase coordination to watch — de-risked by declaring the seam + hook contract at approval.
- **Save-scope split desync (the plan's own §9 risk).** Dissolved by KD-4: one season-save sub-blob for both
  durable contracts and season-scoped negotiation/window/spend state — no two-version-gate desync, no
  `WORLD_STORE` bump.
- **Deferred producers/consumers land later.** Mitigated by identity seams: the #32/#34 reuse seam, the
  deferred #33 personality / #28 CA read, the ×1.0 #34 staff-influence routing seam, and the deferred rival-AI
  bidding — all default to their Stage-2 identities.

## 11. Promotion pipeline

1. Author the 11-file section set at `IN REVIEW` (FR-TX-001..NNN).
2. Section-file PASS-1 adversarial review → AR-2/AR-3 to convergence.
3. R-01..R-05 lead-developer sign-off → APPROVED; flip `SPEC_INDEX.md` row.
4. **Back-props at approval: one** (§8) — the #30 transfers tick-order null-seam slot (ERR-030-NNN);
   `0x23`/85 stays reserved (draw-free); #40/#33/#27 unchanged.
5. T-phase (post-APPROVED): T0 value types (`Contract`, `Offer`, `TransferWindow`, `TransfersState`) + the
   deterministic Stage-2 valuation / synchronous single-counterparty resolution / window model
   (behaviour-neutral) → T1 `TRANSFERS_SAVE_FORMAT_VERSION` sub-blob + season-save composition (#30 outer
   bump coordination) → T2 the world-tick step wired at #30's new slot + the #30 roster-commit/re-key seam
   (ERR-030-NNN) + #28/#33 subscribing their own keyed migration → T3 deep personality-modulated valuation /
   CA-from-#28 refinement / clauses/loans/wage-structures / multi-day negotiation / stochastic rival bidding
   (promotes `0x23`/85, ERR-016) / #34 staff-influence seam.

## Version History
| Version | Date | Change |
|---------|------|--------|
| v0.1 | July 23, 2026 | Initial design supplement from spec-plan v0.1, grounded on the verbatim #40 `ClubFinances`/`ApplyTransaction`, #33 `PersonalityProfile`/`MoraleOf` §7.3, #30 `RunWorldTickInFixedOrder` slot list + `SeasonSaveCodec`, and #27 `PlayerRecord`/`PlayerId` contracts. Resolves plan KD-1..KD-5 + adds KD-6 (window model), KD-7 (PlayerId re-key), KD-8 (neutrality/command boundary). One approval-time back-prop (the #30 transfers slot); `0x23`/85 stays reserved. |
| v0.2 | July 23, 2026 | AR-1 (3M+3L). **M1** — §5/§8 corrected: #40 (ERR-040-001) is **draw-free / reservation-not-promotion** exactly like #31, not a tag-promoter; only #41 (ERR-041-001) promoted `0x2A`. **M2** — KD-7/§10 reframed: the roster-commit is a **new #30 mid-season entry point** (today's per-id churn is season-boundary-only), forcing #28/#33 mid-season migration — not a "generalization" of the boundary hook. **M3** — KD-2/KD-7 pin **validate-all-before-commit atomicity** (no half-written deal). **L1** — §2/KD-1 pin minimal = manager-initiated **buy AND sell** (deterministic); autonomous AI bidding is the deferred draw. **L2** — §8 owns the #41-slot-used-at-minimal vs #31-slot-deep-reservation contrast. **L3** — KD-2 pseudocode gains the sell side + cites FR-FN-004 for the #31-owned committed-spend counter. |
| v0.3 | July 23, 2026 | PROMOTED — 11-file section set authored + APPROVED (section-file AR-1 3M+1L → AR-2 1L → CONVERGENCE). Notable section-file fixes beyond the supplement: `Offer` gained an explicit `CounterpartyClubId` (buy = the player's owning club, sell = a manager-named target buyer); `Contract` collapsed to a **single** contract-end field `LengthSeasons` (dropped the dual `ExpiryWorldDay`/`CONTRACT_NO_EXPIRY`); `TransfersState` scoped to the managed club at minimal (AI clubs are untracked valuation functions); the counterparty valuation's need signal is the **valuing** club's (seller on a buy, buyer on a sell). Back-prop ERR-030-004 filed. |
