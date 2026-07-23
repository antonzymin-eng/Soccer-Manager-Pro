# Staff & Backroom #34 — Design Supplement

> **Created:** July 23, 2026
> **Last Updated:** July 23, 2026 (v0.4 — AR-3 0H+0M+1L + **CONVERGENCE**; prior v0.3 AR-2 1M+1L, v0.2 AR-1 1H+4M+1L, v0.1 initial).
> **Status:** DESIGN SUPPLEMENT (pre-promotion — no section files, no `SPEC_INDEX.md` row).
> **Candidate spec:** #34 · **FR prefix:** FR-ST (grep-verified unclaimed across `docs/specs/**` — only the roadmap/plan proposal cites it).
> **Master-plan home:** §5 Stage 3 · **Tier:** S3 (an identity scaffold pulled forward + the Stage-3 deep system) · **Wave:** 4 (recruitment/economy cluster — after #31, which owns the reusable negotiation seam; before #32, since scouts are staff).
> **Determinism (proposed):** `DOMAIN_TAG_STAFF` / `SubsystemOrdinals.Staff` = `0x26` / `88` — the roadmap §6 off-pitch reservation, **already present as the `_RESERVED_0x26_` placeholder row** in #16 §3.4 (verified `deterministic-sim/section-3.md:270`). **Stays RESERVED at approval** (the scaffold tier is draw-free — the #40 ERR-040-001 / #31 reservation precedent); promotes at the deep tier's first stochastic draw (candidate-pool generation).
> **Source plan:** `docs/tracking/spec-plans/spec-34-staff-backroom.md` v0.1.

---

## 0. Scope

The **backroom**: coaches, scouts, and physios modelled as **attributed entities** with roles, skills, and
(deep) hiring — that **modulate** the systems they support. Staff advance on the **world tick** (`WorldClock`,
one day = one `worldTick` — never the 10 Hz/60 Hz match loops), are constrained by #40's club budgets (staff
wages), and are persisted alongside the season/career save. #34 is fundamentally a **Stage-3 system**; its
pulled-forward floor is an **identity scaffold** — a real neutral-baseline staff roster whose quality
projections are exactly the identity each consumer already defaults to — so #34 can land **behaviour-neutral**
and give #29/#41/#33/#31 a real producer in place of their built-in identity defaults, with the actual
staff-management gameplay (real attributes, hiring, wages) as the deep tier on **one code path**.

**#34 supplies only the staff-quality input; it never owns the models it feeds.** The training model is
#29's, the injury/recovery model is #41's, the morale/mentoring model is #33's, the valuation model is #31's
— #34 produces the **modifier** each of those already reads and adds no second path into any of them.

**Out of scope (owned elsewhere, referenced as seams):**
- **The training model (#29).** #29 owns `AdvanceTrainingDay`/`ComputeTrainingInput`; #34 produces the
  `CoachingModifier` #29 already takes `in` (default `Identity`). #34 **MUST NOT** add a second
  training-effectiveness path (#29 §7 KD-3 contract).
- **The injury/medical model (#41).** #41 owns `AdvanceMedicalDay`/`ComputeInjuryRisk`; #34 produces the
  `MedicalModifier` #41 already takes `in` (default `Identity`). #34 **MUST NOT** add a second occurrence-risk
  or recovery-speed path (#41 §7 KD-5 contract).
- **Player development (#28).** #28 is **schema-untouched** by #34: coaching reaches growth **only** through
  #29 (`CoachingModifier → TrainingInput → #28`), never a direct #34→#28 seam (#28 §9 R-03).
- **The economy (#40 Club Finances).** #40 owns budgets/wages; #34 posts staff wages **only** through
  `ApplyTransaction` (`LineItem = StaffWage`) and reads `WageBudget`/`AvailableTransferBudget` read-only. #34
  **MUST NOT** write `ClubFinances` fields or hold a parallel wage total (FR-FN-015). Wage posting is a
  **deep-tier** concern (the scaffold hires no one), so FR-FN-015 (`WageBillAggregate ≡ 0` at Stage 2) is
  preserved verbatim (KD-6).
- **The morale/personality model (#33).** #33 owns morale/personality; #34 produces the `MentoringPlan`
  override #33 already reads (default `MentoringPlan.None`, FR-HS-022) and reads `MoraleOf`/`PersonalityProfile`
  read-only (deep). #34 builds no morale path.
- **The negotiation seam (#31).** #31 owns the reusable `NegotiationOutcome` + the valuation-driven
  accept/reject **pattern**; #34 reuses that enum and pattern for **staff hiring** (deep) but authors a thin
  staff-specific offer/evaluator because the negotiated quantity is a **wage, not a fee** (KD-1). #34 is also
  the producer of #31's own deferred `staffMult` (×1000‰ identity today).
- **Scouting / fog-of-war (#32).** #32 does **not exist yet**; #34 publishes a **scout-quality projection**
  #32 will consume (scouts are staff) and builds no scouting (FR-LW-031).
- **The season loop + save codec (#30).** #30 owns `RunWorldTickInFixedOrder`, `SeasonSaveCodec`, and the
  outer `SEASON_SAVE_FORMAT_VERSION`; it **invokes** #34 at a new pre-declared tick-order slot and composes
  #34's opaque save sub-blob. #34 never references #30.

## 1. What exists vs. what #34 adds

**Exists (verified against source / approved specs — the seam reconnaissance):**

- **#41 Injuries & Medical (APPROVED, FR-MD)** — the **gold-standard identity routing seam** #34 plugs into,
  fully built (`injuries-medical/section-2.md`):
  ```csharp
  public readonly struct MedicalModifier {              // per-mille integer multipliers (1000 = ×1.0)
      public int OccurrenceRiskMillMult, RecoverySpeedMillMult;
      public static MedicalModifier Identity => new(1000, 1000);
      public MedicalModifier(int occ, int rec) { … }
  }
  ```
  `MEDICAL_MODIFIER_IDENTITY_PERMILLE = 1000` [FIXED]. **FR-MD-016:** `AdvanceMedicalDay(… in MedicalModifier
  medical …)` and `ComputeInjuryRisk(… in MedicalModifier)` take it defaulting to `Identity` "until #34 lands;
  no #34 interface is built (FR-LW-031)"; **`default(MedicalModifier)` (all-zero) MUST NOT be a valid runtime
  value** (fail-loud F4). #41 §7 KD-5: "#34 becomes the producer of a non-identity modifier; #34 MUST NOT add
  a second occurrence-risk or recovery-speed path — it supplies the modifier #41 already reads." **Consume-ready.**
- **#29 Training System (APPROVED, FR-TR)** — the coaching seam, **type reserved but shape deferred**
  (`training-system/section-2.md`):
  ```csharp
  public readonly struct CoachingModifier { public static CoachingModifier Identity => default; }  // bare
  ```
  **FR-TR-016:** `AdvanceTrainingDay(… in CoachingModifier coach …)` / `ComputeTrainingInput(… in
  CoachingModifier)` default to `Identity` (×1.0); no #34 interface. **Unlike #41, `CoachingModifier` has no
  per-mille fields and no constant catalogue** — #29 reserved only the type name and the ×1.0 behaviour. #34
  defines its internal multiplier shape + identity constant, and #29's consumption of the (future) fields is a
  **deep-tier #29 back-prop** (ERR-029-002) — at the scaffold #34 produces `CoachingModifier.Identity` (= the
  existing `default`), so #29 is untouched.
- **#28 Player Progression & Lifecycle (APPROVED, FR-PG)** — **no direct #34 seam.** #28 exposes only the
  #29-owned `TrainingInput` (`Neutral => default`); coaching modulates growth **indirectly** via #29's
  `CoachingModifier → TrainingInput → #28`. No `#34` reference anywhere in #28 (schema-untouched, #28 §9 R-03).
- **#40 Club Finances & Economy (APPROVED, FR-FN)** — the staff-wage seam, fully built
  (`club-finances-economy/section-2.md`):
  - `enum FinanceLineItem : byte { General = 0, TransferFee = 1, PlayerWage = 2, StaffWage = 3 }` — **`StaffWage`
    is a distinct member.**
  - `ApplyTransaction(ref ClubFinances f, in FinanceTransaction txn)` — the single ledger-mutation path; a
    `StaffWage` line moves `WageBillAggregate` **only** (Debit +, Credit −, F1 fail-loud if a credit drives it
    negative; FR-FN-016). `AvailableTransferBudget(in ClubFinances f) => f.TransferBudget` (pure read);
    `WageBudget` is a static per-season ceiling flagged "#31/#34 read".
  - **FR-FN-015:** "`WageBillAggregate` MUST be `0` at Stage 2 (**no #31/#34 producer exists yet**); #31/#34
    MUST NOT maintain a parallel wage total." #40 §7: "#34 becomes a second caller of `ApplyTransaction`
    (`LineItem = StaffWage`) — the same contract as #31's wage line items." **Consume-ready; the wage post is
    deep (KD-6).**
- **#33 Personalities, Morale & Squad Dynamics (APPROVED, FR-HS)** — read surface + the mentoring override:
  `MoraleOf(in MoraleState) → int`; `PersonalityProfile` (5 traits, `Create()` = `TRAIT_NEUTRAL = 10`,
  `default` fails loud). **FR-HS-022:** deep-tier mentoring pairing "is overridable by a **#34 staff-driven**
  routing seam — no #34 interface is built," via `MentoringPlan` (identity `MentoringPlan.None`). **No
  "staff-judgement-quality" surface exists** — only the mentoring-pairing override.
- **#31 Transfers, Contracts & Negotiation (APPROVED, FR-TX)** — the reusable negotiation seam + the awaiting
  `staffMult`:
  - `enum NegotiationOutcome : byte { Rejected = 0, Accepted = 1, CounterOffered = 2 /* deep */ }` and
    `EvaluateOffer(in Offer offer, long counterpartyValuation)` — **but `Offer` is transfer-shaped**
    (`PlayerId`, `CounterpartyClubId`, `Fee`, `WagePerPeriod`, `LengthSeasons`, `IsBuy`) and `EvaluateOffer`
    tests **`Fee`** (buy: `Fee ≥ cv`; sell: `cv ≥ Fee`). **FR-TX-010** makes the seam "counterparty-generic …
    so #32/#34 reuse it without duplication."
  - `TRANSFERS_STAFF_MULT_IDENTITY = 1000` [FIXED]; **FR-TX-011:** "#31's own #34-staff-influence MUST be a
    deferred ×1000‰ identity routing seam." So **#34 is both a consumer of `NegotiationOutcome` and the
    producer of #31's `staffMult`.**
- **#30 Season & Competition Loop (APPROVED, FR-SN)** — the invoker/save root:
  - Save composition: each downstream writes its **own** independently version-gated sub-blob into
    `SeasonSaveCodec` (the `MEDICAL_SAVE_FORMAT_VERSION` / `HUMAN_SYSTEMS_SAVE_FORMAT_VERSION` precedents, both
    "No `WORLD_STORE_FORMAT_VERSION` bump"); outer `SEASON_SAVE_FORMAT_VERSION` = 2, bumped at the T-phase.
  - `RunWorldTickInFixedOrder` / **FR-SN-034** pinned list: **1 progression(#28) · 2 training(#29) · 3
    human-systems(#33) · 4 injuries(#41) · 5 transfers(#31) · 6 `WorldStore.AdvanceDay()`** — **no #34 slot**
    (slots #41/#31 were each added by back-prop at their approvals — ERR-030-002/004).
  - `RequestRosterCommit` **does not exist** (a deferred #31 back-prop, ERR-030-005) and is **player-`Squad`-only**
    — it re-keys a #27 `PlayerId`. **Staff are not in #27's `Squad`**, so it does not cover them (KD-7).
- **#27 Squad/Player Data (APPROVED, FR-SQ; `src/player-database/` built)** — the record-shape template only:
  `struct PlayerRecord { int PlayerId; string FirstName, LastName; int Age; PlayerPosition Position;
  PlayerAttributes Attributes; }`; `PlayerAttributes` = 31 `int[1,20]`; `Squad { int ClubId; int Count;
  PlayerRecord GetPlayer(int); }`; `CLUB_SQUAD_SIZE = 25` [FIXED]; `PlayerId = clubId*CLUB_SQUAD_SIZE+localIndex`.
  **No `StaffRecord`/`StaffAttributes`/staff-ID formula exists** — #34 authors these from scratch (KD-2).
- **#16 §3.4** — `_RESERVED_0x26_` / ordinal `88` placeholder row **already exists**, held for #34.

**#34 adds:** a canonical **staff data layer** (`StaffRecord`/`StaffAttributes`, a distinct staff-skill
vocabulary — KD-2); a **per-club staff roster** seeded with a **real neutral-baseline** house staff (KD-5),
persisted in a `STAFF_SAVE_FORMAT_VERSION` season-save sub-blob (KD-4); **staff-quality projection functions**
that map staff attributes into each consumer's **own pre-existing identity type** (`MedicalModifier` /
`CoachingModifier` / `staffMult` / `MentoringPlan` — KD-3), neutral baseline ⇒ each type's `Identity`; and
(deep) a **candidate pool** (the first draw — KD-4), a **staff hiring** flow reusing #31's negotiation pattern
(KD-1) with **`StaffWage` posts** + a `WageBudget` gate (KD-6). **No RNG stream at the scaffold** (draw-free —
candidate-pool generation is the deep-tier first draw). **No #30 roster-commit** (staff are #34-owned; a hire
changes a mutable employer field, never re-keys — KD-7).

## 2. Staging (identity scaffold → deep, one code path)

- **Scaffold (pulled forward — always present, behaviour-neutral)** — the **managed club** holds a **real
  neutral-baseline staff roster** (all-neutral `StaffAttributes`, the #27 `CreateDefault` / #33 `TRAIT_NEUTRAL`
  discipline) filling its role slots; **AI clubs are unstaffed** and get the consumers' own built-in `Identity`
  default (byte-neutral, exactly as today — all-clubs staff is deep, KD-4/§4). #34's projections map the
  managed roster to each consumer's **exact `Identity`** (`MedicalModifier.Identity`, `CoachingModifier.Identity`,
  `staffMult = 1000‰`, `MentoringPlan.None`), and the composition root threads the **two live seams** at the
  scaffold — `MedicalModifier` → #41 and `CoachingModifier` → #29 — in place of their built-in identity
  defaults (#31's `staffMult` / #33's `MentoringPlan` are consumed at *their own* deep tiers, so the scaffold
  does not yet feed them). Because a neutral-baseline staff projects to Identity, **every consumer behaves
  byte-identical to pre-#34** — the only new state is #34's own save sub-blob (the #27 "records exist but
  unconsumed at identity" class). **No hiring, no candidate pool, no `StaffWage` post, no draw** — `0x26`/88
  stays reserved (KD-4/KD-6/KD-8).
- **Stage-3 deep** — real staff with **attribute-derived, non-identity** projections (a distinct-staff club
  diverges from the neutral baseline the way a distinct #27 squad diverges from the all-neutral roster); a
  **stochastic candidate pool** (the first draw site, promotes `0x26`/88); **hiring** via the #31 negotiation
  **pattern** (a thin staff `StaffOffer`/`EvaluateStaffOffer` on wage-vs-demand, KD-1) with **`StaffWage`
  posts** + a `WageBudget` affordability gate (relaxes FR-FN-015, KD-6); **#33 judgement** feeding scout/coach
  quality; and the **#29 `CoachingModifier` field shape + consumption** (deep #29 back-prop) — all on **one
  code path**, each defaulting to its scaffold identity via a config dial (`deepStaffEnabled` off ⇒ neutral
  baseline, identity projections, no draws, no wages).

**One code path (KD-8):** the neutral-baseline roster, the identity projections, and the draw-free/no-wage
posture are the exact identities the deep tier modulates — the #21/#27/#40/#41 default-behaviour-neutral
discipline, not a rewrite.

## 3. Dependencies & reference direction (one-way, no cycle)

- **#30 → #34** — the day-advance loop *invokes* #34's world-tick step at a **new pre-declared tick-order slot**
  (a documented null seam #30 inserts, the #41/#31 ERR-030-002/004 pattern), and the **composition root**
  threads #34's projections into the consumers and routes #34's (deep) hiring commands. #30 owns the
  calendar/save; #34 reads them read-only and **never** references #30.
- **#34 → #41 / #29 / #33 / #31** — #34 **references each consumer's assembly to construct that consumer's own
  identity type** (`MedicalModifier`, `CoachingModifier`, `MentoringPlan`, and — deep — `NegotiationOutcome`)
  and to produce #31's `staffMult`. **One-directional** — none of #41/#29/#33/#31 reference #34 (each built
  its own `Identity` default, FR-LW-031); the scaffold subset is `#34 → {#41, #29}` (the two live tick
  consumers), widening to `{#33, #31}` at the deep tier.
- **#34 → #40** — posts staff wages via `ApplyTransaction` (`StaffWage`) and reads `WageBudget` (deep only; the
  scaffold posts nothing). One-directional — #40 never reads #34 (KD-6).
- **#34 → #16** — the determinism namespace + world-tick `DeterministicRngService` (only when the deep tier
  draws candidate pools).
- **#34 does NOT reference #27 or #28** — it authors `StaffRecord`/`StaffAttributes` fresh (mirroring #27's
  record *shape* is pattern reuse, not an assembly reference), and reaches development only through #29.
- **Consumers (deferred, no interface built):** **#32** (scouts are staff — #34 publishes a scout-quality
  projection #32 consumes) and **#42** (academy coaching → intake quality). All deferred (FR-LW-031).

Reference DAG: `compositionRoot → {#30, #34}`, `#34 → {#41, #29, #33, #31, #40, #16}`. **Acyclic** (no consumer
references #34; #41/#29/#33/#31/#40 stay schema-untouched at approval — #34 constructs their existing identity
types and posts through #40's existing `StaffWage` path).

## 4. Persistent state & save impact (KD-4)

Adds an opaque, independently version-gated **staff sub-blob** (`STAFF_SAVE_FORMAT_VERSION` [FIXED] = 1)
composed into #30's season save via the `SeasonSaveCodec` pattern — the **#41 `MEDICAL_SAVE_FORMAT_VERSION` /
#33 `HUMAN_SYSTEMS_SAVE_FORMAT_VERSION` precedent, both explicitly "No `WORLD_STORE_FORMAT_VERSION` bump."**
Staff are **durable career state** (a hired coach survives `RollToNextSeason`), so the season save — which *is*
the multi-season career save — is their home, not the world store. The block carries, per club:
- **Durable across seasons:** the managed club's **role slots** + their `StaffRecord`s (id, name, age,
  `StaffRole`, `StaffAttributes`, `EmployerClubId`, and — deep — a staff `Contract`: wage/length; clauses
  append behind `deepStaffEnabled`) + the monotonic `NextStaffId` allocator (KD-7). **Managed-club scope:** the
  scaffold tracks the **managed club's** staff only; AI clubs are unstaffed and project `Identity` (untracked,
  byte-neutral, exactly as today) — all-clubs staff modelling arrives with autonomous AI at the deep tier (the
  #31 `TransfersState` managed-club-scope precedent).
- **Season-scoped (deep):** in-flight hiring negotiations + the deep candidate-pool state. Staff hiring is
  **year-round — no window** (a deliberate difference from #31's summer window: staff are not transfer-listed
  players; KD-1). The scaffold carries none of these (no hiring). **#34 keeps NO wage counter** — the deep
  affordability gate reads #40's running `WageBillAggregate` directly (KD-6), so there is no
  `committedStaffWage` state to reset.

Mirror the `SeasonSaveCodec` fail-loud posture exactly (`Require(offset, need, total)` bound against
**`total − offset`**, version-mismatch throw, per-read `Require`, trailing-byte guard). **No
`WORLD_STORE_FORMAT_VERSION` bump.** The outer `SEASON_SAVE_FORMAT_VERSION` bump is coordinated with #30 at the
T-phase (exact version assigned by whichever T-phase lands first — the #28/#29/#40/#41/#33/#31 deferral
pattern; **not hardcoded here**). **No `RngCursor` is serialized** — the scaffold is draw-free; deep
candidate-pool draws are **position-independent keyed draws** on `(clubId, worldDay, purpose)` (the
#41/#28/#30 off-pitch keyed-draw precedent), so even the deep tier persists no free-running cursor. Round-trip
determinism required (a **new-career-genesis-seeded** roster round-trips field-identical; and, deep, a
mid-negotiation save). **Genesis-vs-load lifecycle (the #31 §3.8 lesson):** the neutral-baseline roster is seeded
**only at new-career genesis** — a load reconstructs the roster from the sub-blob and **MUST NOT re-seed**
(re-seeding would overwrite hired/aged staff).

## 5. Determinism (KD-4 — single world clock, draw-free scaffold)

**All #34 state advances on the WORLD tick** at #30's pre-declared slot, from **committed** values #30 routes
in. The **scaffold tier makes no stochastic draw** — projections are pure integer-per-mille functions of the
staff roster; there is no candidate pool and no hiring. Consequently:
- **`0x26`/88 stays `_RESERVED_0x26_`** at #34's approval (no `DOMAIN_TAG_STAFF` promotion, **no #16 spec-text
  change**) — the **draw-free reserved-not-promoted precedent of #40 (ERR-040-001) / #31 / #29**. It promotes
  to a live domain tag + `SubsystemOrdinals.Staff = 88` only at the **deep tier's first draw** (candidate-pool
  generation), keyed on `(clubId, worldDay, purpose)`.
- **Save→restore is byte-exact with nothing to continue** — no cursor at the scaffold, keyed draws (no cursor)
  at deep.
- **Stream independence (trivially):** registering **no** stream leaves every existing cursor byte-identical
  (the #40 `_RESERVED_0x29_` / `T-FN-NEU-003` property).

Integer-per-mille internally (staff attributes `[1,20]`, projections per-mille `1000`, wage arithmetic `long`);
there is **no float in #34** — projections cross-multiply per-mille integers into each consumer's integer
identity type, and wages are integer `long` exchanged with #40. One clock (world), so no
determinism-ordering fragility between loops.

## 6. Primary surfaces (proposed → pinned in §4 of the section files)

```csharp
// KD-2 — the staff data layer, authored fresh (a DISTINCT staff-skill vocabulary; NOT #27's 31 player attrs).
// Mirrors #27's record SHAPE + the neutral-identity discipline; default(...) all-zero fails loud (the #41 F4 / #33 pattern).
public enum StaffRole : byte { Coach = 0, Scout = 1, Physio = 2 }   // deep may extend; ordinal-stable
public readonly struct StaffAttributes                              // each int [1,20]; Create() = all STAFF_ATTR_NEUTRAL (10)
{ public int Coaching, Fitness, Medical, ScoutJudgement, Motivating, Discipline, TacticalKnowledge; /* deep may append */ }
public struct StaffRecord
{
    public int  StaffId;            // #34-owned, STABLE — does NOT re-key on a move (KD-7)
    public string FirstName, LastName;
    public int  Age;
    public StaffRole Role;
    public int  EmployerClubId;     // MUTABLE — a hire changes THIS, not StaffId (KD-7); -1 = unemployed (deep)
    public StaffAttributes Attributes;
    // deep (behind deepStaffEnabled): a staff Contract { long WagePerPeriod; int LengthSeasons; } APPENDS here.
}

// KD-3 — staff-quality projections into each consumer's OWN pre-existing identity type. #34 invents NO new
// multiplier convention; neutral-baseline staff ⇒ each type's exact Identity (byte-neutral).
public static MedicalModifier   ToMedicalModifier(in StaffRecord physio);       // #41's type; neutral => MedicalModifier.Identity
public static CoachingModifier  ToCoachingModifier(in StaffRecord coach);       // #29's type; neutral => CoachingModifier.Identity
public static int               ToStaffMult(in StaffRecord /* head of recruitment */);  // #31's staffMult; neutral => 1000
public static MentoringPlan     ToMentoringOverride(/* staff + squad ctx */);   // #33's type; neutral => MentoringPlan.None
public static int               ToScoutQuality(in StaffRecord scout);           // #32 (deferred consumer); neutral => baseline

// KD-5 — the neutral-baseline house staff is a REAL entity projecting explicit Identity, NOT an absence sentinel.
public static StaffRecord NeutralHouseStaff(int staffId, StaffRole role, int clubId);   // all-neutral attrs
public static void SeedInitialStaff(int managerClubId, ref StaffState s);                // new-career genesis ONLY (KD-4)

// KD-2/KD-7 — the managed-club staff store: ROLE SLOTS (one StaffRecord per role slot), keyed by a stable,
// serialized, monotonic StaffId allocator (NextStaffId; high-water, never reused — the #22 episodeId discipline).
// Each consumer reads its assigned slot-holder's projection (below); the neutral baseline fills every slot.
public sealed class StaffState { /* managed-club role slots -> StaffRecord; int NextStaffId; deep: candidate pool + in-flight */ }

// KD-1 (DEEP) — hiring reuses #31's NegotiationOutcome + the validate-all-first atomic-commit PATTERN, but a
// thin staff-specific offer/evaluator: the negotiated quantity is a WAGE (accept iff offered >= demand), NOT a fee.
public readonly struct StaffOffer { public int StaffId; public long WagePerPeriod; public int LengthSeasons; }
public static NegotiationOutcome EvaluateStaffOffer(in StaffOffer o, long wageDemand);   // deep; draw-free predicate (offer.Wage >= demand)
public /* command */ NegotiationOutcome HireStaff(int managerClubId, in StaffOffer o /* , world ctx */);  // WageBudget-gated, year-round (deep)

// KD-6 (DEEP) — the #40 boundary for staff wages. Scaffold posts NOTHING (FR-FN-015 preserved). NO #34 wage counter.
//   VALIDATE-ALL-FIRST: candidate accepts AND (WageBillAggregate + wage <= WageBudget)   // both read from #40; year-round, no window
//   THEN commit atomically: ApplyTransaction(ref finances, {Debit, StaffWage, wage});    // WageBillAggregate += (FR-FN-016)
```

## 7. Key design decisions

- **KD-1 (hiring reuses #31's *pattern + enum*, not its `Offer` struct — the reuse-vs-parallel headline).**
  The roadmap flags reuse-vs-parallel with #31 as the load-bearing fork. Resolution: **reuse the genuinely
  generic assets** — #31's `NegotiationOutcome` enum and the **validate-all-first, accept-iff-clears-a-
  deterministic-counterparty-valuation, atomic-commit pattern** — and author a **thin staff-specific
  `StaffOffer` + `EvaluateStaffOffer`** rather than consuming #31's `Offer`/`EvaluateOffer` verbatim. Rationale
  (structural, not taste): #31's `EvaluateOffer` tests **`Fee`** against the counterparty valuation and its
  `Offer` carries `CounterpartyClubId`/`IsBuy`/transfer-`PlayerId` — a staff hire has **no fee** (the
  negotiated quantity is the **wage**: a candidate accepts iff `offeredWage ≥ wageDemand`), **no selling club**,
  and **no buy/sell duality**. Forcing staff through the transfer `Offer` would (a) test the **wrong quantity**
  (wage-as-fee) and (b) drag three meaningless transfer fields into staff — the abstraction-leak/false-reuse
  smell. The reusable asset #31 correctly identified (FR-TX-010) is the **decision pattern + the outcome type**,
  not the transfer-shaped data struct #31 itself specialised (`CounterpartyClubId`/`IsBuy`). This is **not
  "duplicating negotiation logic"**: the ~one-line accept predicate is not the asset; the heavy machinery
  (multi-day in-flight negotiation, rival bidding) is a **shared deep-tier pattern**, and if the deep shapes
  converge a later refactor can extract a common generic kernel over a caller-supplied valuation — but
  coupling the minimal shapes to the transfer struct now is premature. **All hiring is deep-tier** (the
  scaffold hires no one), so this seam does not exist until `deepStaffEnabled`; and hiring is **year-round —
  there is no transfer-style window** (staff are not transfer-listed players), a deliberate simplification away
  from #31's summer window (KD-6/§4).

- **KD-2 (the staff data layer — authored fresh, a distinct vocabulary; #27 reuse is *shape*, not schema).**
  `StaffRecord`/`StaffAttributes` are new #34-owned value types mirroring #27's record **shape** (id / name /
  age / role / attributes) and the neutral-identity discipline, but with a **distinct staff-skill attribute
  vocabulary** (`Coaching`/`Fitness`/`Medical`/`ScoutJudgement`/`Motivating`/`Discipline`/`TacticalKnowledge`,
  each `[1,20]`) — **not** #27's 31 player attributes (a coach has no `Finishing`/`Pace`). `StaffAttributes.
  Create()` = all `STAFF_ATTR_NEUTRAL = 10` (the #33 `TRAIT_NEUTRAL` / #27 `CreateDefault` pattern);
  `default(StaffRecord)` / `default(StaffAttributes)` (all-zero) is **invalid** and fails loud at the consuming
  seam (the #41 `default(MedicalModifier)` F4 discipline). #34 does **not** extend #27's `PlayerAttributes` and
  does **not** reference #27's assembly — the shape is a template, not a dependency. The boundary: reuse the
  record-shape *pattern* and the per-mille neutral-identity discipline; own a separate attribute schema. **The
  per-club store is a set of ROLE SLOTS** (one `StaffRecord` per role — head coach, head physio, chief scout,
  head of recruitment; deep may add more), so the club→modifier reduction is a **single well-defined slot
  read** (KD-3), not an unspecified aggregate over many staff; the neutral baseline fills every slot with
  `NeutralHouseStaff`. Staff are keyed by a **stable, serialized, monotonic `StaffId`** from a
  `StaffState.NextStaffId` high-water counter (never reused — the #22 `episodeId` monotonicity discipline), so
  genesis-seeded house staff and deep candidate-pool staff draw collision-free ids from one deterministic
  source (KD-7).

- **KD-3 (projection convention — into each consumer's OWN identity type; #34 invents no multiplier scheme).**
  #34's quality projections return **each consumer's pre-existing identity type**, so the neutral baseline is
  exactly that type's `Identity` and #34 introduces no new convention to reconcile: `ToMedicalModifier →
  MedicalModifier` (#41's per-mille pair; neutral ⇒ `Identity` = 1000/1000), `ToCoachingModifier →
  CoachingModifier` (#29's type; neutral ⇒ `Identity`), `ToStaffMult → int` (#31's `staffMult`; neutral ⇒
  `TRANSFERS_STAFF_MULT_IDENTITY = 1000`), `ToMentoringOverride → MentoringPlan` (#33's type; neutral ⇒
  `None`), `ToScoutQuality → int` (the #32-facing projection; neutral ⇒ a baseline #32 will define). The
  per-mille `1000 = ×1.0` convention is **inherited** from #41/#31, not minted. **Each projection reads the
  club's assigned role-slot-holder** (KD-2) — `ToCoachingModifier` the head coach, `ToMedicalModifier` the head
  physio, `ToScoutQuality` a scout, `ToStaffMult` the head of recruitment — so the club→modifier reduction is a
  single deterministic slot read, never an unspecified aggregate over a variable-size staff set. **#29's `CoachingModifier` is a
  bare `default` today** (no per-mille fields, no consumption), so `ToCoachingModifier` returns `Identity` at
  the scaffold, and the **field shape + #29's consumption of it is a deep-tier #29 back-prop (ERR-029-002)** —
  the one consumer whose modifier internals #34 must help define, deferred to when #34 produces non-identity
  coaching. **Multiplier-composition discipline (a §10 risk):** a single staff facet MUST reach each consumer
  **once** — #34's projection is the sole staff path into `MedicalModifier`/`CoachingModifier`/etc. (FR-MD/FR-TR
  forbid a second path), so staff modulation cannot double-count with #33 morale or #40 facility effects, which
  reach those consumers by their own separate seams.

- **KD-4 (persistence + determinism — one season-save sub-blob; draw-free scaffold).**
  `STAFF_SAVE_FORMAT_VERSION` [FIXED] = 1 opaque sub-blob composed into `SeasonSaveCodec` (the #41/#33
  precedent), holding durable staff records + (deep) season-scoped hiring state — **not** a
  `WORLD_STORE_FORMAT_VERSION` bump (staff are career state the season save owns, like #40 `Balance` / #41
  medical / #33 morale). The **scaffold is draw-free**, so `0x26`/88 **stays reserved** at approval (no #16
  change — the #40/#31/#29 precedent) and every existing stream's cursor stays byte-identical. Candidate-pool
  generation is the deep tier's **first draw site**, promoting `DOMAIN_TAG_STAFF = 0x26` / `SubsystemOrdinals.
  Staff = 88` at T3 (spec-text-first, ERR-016), keyed position-independently on `(clubId, worldDay, purpose)`
  — no serialized cursor even at deep. Seeding is **new-career-genesis-only** (a load reconstructs from the
  sub-blob, never re-seeds — the #31 §3.8 genesis-vs-load lesson).

- **KD-5 (the neutral baseline is a REAL entity, not an absence sentinel).** An unfilled staff slot is a
  **real neutral-baseline house-staff `StaffRecord`** (all-neutral attributes) that projects to each consumer's
  explicit `Identity`, **not** an absence/null the consumers special-case. This is mandated by the existing
  seams: #41/#29 already take `in MedicalModifier`/`in CoachingModifier` **defaulting to `Identity`** — they do
  **not** branch on "no staff," they consume a modifier that happens to be `Identity`. So #34 supplies a real
  Identity-producing entity, and — critically — `default(StaffRecord)`/`default` modifier (all-zero) is the
  **invalid zero-value trap** that fails loud (the #41 `default(MedicalModifier)` F4 discipline), never a
  silent neutral. This makes the behaviour-neutral proof a clean equality (neutral staff → `Identity` →
  byte-identical) with **no consumer branch to add**.

- **KD-6 (staff wages — deep-tier; the scaffold preserves FR-FN-015 verbatim; the gate reads #40's running
  wage bill, NOT a #34 counter).** The **scaffold hires no one and posts no `StaffWage`**, so #40's FR-FN-015
  (`WageBillAggregate ≡ 0` at Stage 2, "no #31/#34 producer exists yet") is **preserved verbatim with no #40
  back-prop at approval** — exactly #31's resolution. The **deep tier** is #34's `StaffWage` producer: on an
  accepted hire it posts `{Debit, StaffWage, wage}` through #40's `ApplyTransaction` (moving `WageBillAggregate`
  only, FR-FN-016), gated by a **`WageBudget` affordability check read entirely from #40**:
  `WageBillAggregate + wage ≤ WageBudget` (both read-only from `ClubFinances`). **This is a mandatory
  divergence from #31's KD-2 committed-spend counter:** #31 must keep its own `committedSpendThisWindow`
  because it gates transfer **fees** against `TransferBudget`, which `ApplyTransaction` leaves **static**
  (FR-FN-004 gives #40 no net-of-committed concept). A staff **wage** is the opposite case — an **ongoing
  liability #40 already maintains as the running `WageBillAggregate`** (each `StaffWage`/`PlayerWage` post
  updates it, FR-FN-016), so the affordability truth is already in #40 and #34 reads it directly. **#34 keeps
  NO wage counter of its own** — a `committedStaffWageThisWindow` would be exactly the "parallel wage total"
  FR-FN-015 **forbids** (`WageBillAggregate` is #40's canonical truth). This lands with a **#40 back-prop
  relaxing FR-FN-015** for the wage producers + wiring the `WageBudget` gate #40 exposes as a read (the
  **shared deferred ERR-040**, T-phase — the same relaxation #31 defers; the two wage producers, `PlayerWage`
  and `StaffWage`, arrive together).

- **KD-7 (staff roster ownership — #34 owns its store; a stable `StaffId`, no re-key, no #30 roster-commit).**
  #34 owns the staff store **entirely**. A staff member has a **stable `StaffId`** (allocated from the
  serialized monotonic `StaffState.NextStaffId` high-water counter — KD-2 — and `[FIXED]` for the entity's
  life) and a **mutable `EmployerClubId`** field; a hire changes `EmployerClubId` **within #34's own state**,
  and nothing outside #34 keys by `StaffId`, so there is
  **no re-key, no cross-system migration hook, and no #30 roster-commit entry point.** This is a **deliberate,
  simpler divergence from #31's KD-7** (which needs the new #30 `RequestRosterCommit` precisely because
  `PlayerId = clubId*CLUB_SQUAD_SIZE+localIndex` is club-scoped and #28/#33/#41 all key by it, so a transfer
  re-keys a #27-owned id and forces cross-system mid-season migration). Staff carry none of that: they are not
  in #27's `Squad`, and #34 is the sole owner of staff-keyed state. **This supersedes the reconnaissance's
  "declare a staff-roster-commit back-prop" flag** — #34 needs none, and avoiding it is strictly better (no new
  #30 capability, no #28/#33 mid-season-migration burden). The only #30 back-prop #34 needs is the tick-order
  slot (KD-8).

- **KD-8 (behaviour-neutral identity + the reserve-ahead tick slot + the command boundary).** #34's scaffold is
  neutral in three senses: (a) **stream independence** — registering **no** stream leaves every existing cursor
  byte-identical (the #40 property); (b) **identity projections** — a neutral-baseline roster projects to each
  consumer's exact `Identity`, so every consumer behaves byte-identical to pre-#34: the composition root swaps
  the #29/#41 hardcoded `Identity` default for a projection that *equals* `Identity` (the two live seams), and
  #31/#33 are untouched at the scaffold (fed only at their own deep tiers); (c) **the tick-order slot is a
  documented null seam at the scaffold** — projections are pull-based inputs the composition root threads into
  the consumers each day, so #34's own tick slot has no scaffold work (candidate-pool aging + in-flight hiring
  are deep). The **one approval-time back-prop is the reserve-ahead #30 staff tick-order null-seam slot**
  (ERR-030-006) — declared **now** so the deep daily processing lands without a future tick-order re-pin (the
  #31 reserve-ahead precedent; a **deliberate difference from #41**, whose slot is *used at minimal*).
  **Command boundary:** a **manager-initiated hire is an explicit command** (`HireStaff`, the `SetTeamTactic`
  discipline, deep) that legitimately posts a `StaffWage` transaction — manager-driven behaviour, not a
  neutrality violation. Deferred consumers/producers (#32 scout-quality, #42 academy, #33 judgement) default to
  identity seams.

## 8. Cross-spec back-props

**At approval: ONE cross-spec spec-text back-prop** (the #31 pattern — draw-free, so no #16 promotion):
- **#30 — insert a staff tick-order null-seam slot** (ERR-030-006). `RunWorldTickInFixedOrder`'s pinned list
  (verified `season-competition-loop/section-3.md`: 1 progression · 2 training · 3 human-systems · 4 injuries ·
  5 transfers · 6 `WorldStore.AdvanceDay()`) gains a **new documented null seam** for staff — proposed as **new
  slot 6, after transfers and before `AdvanceDay`** (the exact position is a #30-owned decision the back-prop
  coordinates), pushing `AdvanceDay` to 7. FR-SN-034 enumerates slots for #28/#29/#33/#41/#31 (not #34), so
  this is a genuine **insertion** (the #41 ERR-030-002 / #31 ERR-030-004 precedent). **A deliberate difference
  from #41:** #41's slot is *used at #41 minimal*; **#34's slot is a deep-tier position reservation** — the
  scaffold's projections are pull-based (no daily work), so the slot is empty until the deep tier's daily
  candidate-pool / in-flight-hiring processing. Declared **now** (reserve-ahead), so the deep daily processing
  lands without a future tick-order re-pin. **ERR-030-005 is soft-reserved by #31** (its deferred
  `RequestRosterCommit` build), so #34 takes **006**.
- **#16 §3.4** — **no change.** `_RESERVED_0x26_`/88 already exists and stays reserved (draw-free scaffold,
  KD-4) — the #40/#31/#29 reservation-not-promotion precedent. **Contrast #41 (ERR-041-001)**, which promoted a
  real tag because it draws at minimal.
- **#41, #29, #40, #33, #31, #27** — **no change at approval.** #34 constructs their existing identity types
  (`MedicalModifier`/`CoachingModifier`/`MentoringPlan`/`NegotiationOutcome`) and posts through #40's existing
  `StaffWage` line (deep); the #41 §7 KD-5 / #29 §7 KD-3 / #40 §7 / #33 FR-HS-022 / #31 FR-TX-011 seam
  contracts already name #34 as the producer/consumer. #34 §8 cites those as the existing cross-reference sides.

**At the #34 T-phase (deferred, lands with code — the #28/#29/#40/#41/#33/#31 deferred-coordination precedent):**
- **#30** — the outer `SEASON_SAVE_FORMAT_VERSION` bump composing the new `STAFF_SAVE_FORMAT_VERSION` sub-blob
  (coordinated at T1). **No roster-commit back-prop** (KD-7 — staff never re-key).
- **#29** — `CoachingModifier` gains its per-mille field shape + `AdvanceTrainingDay`/`ComputeTrainingInput`
  consumption of it (**ERR-029-002**), when #34 produces a non-identity `CoachingModifier` (deep).
- **#40** — relax FR-FN-015 for the wage producers + wire the `WageBudget` affordability gate (the **shared
  deferred ERR-040**, arriving with #31's `PlayerWage` producer), when #34 posts `StaffWage` (deep).
- **#16** — `DOMAIN_TAG_STAFF = 0x26` / `SubsystemOrdinals.Staff = 88` promotes at #34 **T3** (the first
  candidate-pool draw), spec-text-first, with the stream registered at the draw site.

## 9. Test focus

- **Behaviour-neutral identity (KD-8, the headline).** A season with a **neutral-baseline staff roster**
  advances **byte-identical** to pre-#34: every projection returns the consumer's exact `Identity`. The
  composition root threads the two **live** seams at the scaffold — `MedicalModifier` → #41 and
  `CoachingModifier` → #29 — so #41/#29 tick identically (#31's `staffMult` and #33's `MentoringPlan` are
  consumed at *their own* deep tiers, so #34's scaffold does not yet feed them — their identity projections are
  proven but dormant). No stream registered; no `StaffWage` posted (`WageBillAggregate` unchanged, FR-FN-015
  preserved — the #31 `T-TX-BID-006` analogue).
- **KD-3 projection identity + divergence.** Each projection is a pure integer function of `StaffAttributes`;
  neutral ⇒ the exact `Identity`; a **distinct (non-neutral) staff** produces a **deterministic non-identity**
  modifier (the #27 distinct-squad-diverges analogue), and it is the **sole** staff path into each consumer
  (no double-count — a static/reflection assertion that #34 adds no second `MedicalModifier`/`CoachingModifier`
  source).
- **KD-2 data layer.** `StaffAttributes.Create()` = all-neutral `10`; `default(StaffRecord)`/`default`
  modifier (all-zero) **fails loud** at the consuming seam (the #41 F4 zero-value-trap lock); `StaffRole`
  ordinal stability.
- **KD-4 save round-trip + genesis-vs-load.** The staff sub-blob round-trips **field-identical** across a save
  (and, deep, a mid-negotiation save) and survives `RollToNextSeason`; a **load reconstructs from the sub-blob and
  does NOT re-seed** (the #31 `T-TX-DET-001` genesis-only lock); the serialized block contains **no**
  `RngCursor` (schema-shape assertion, KD-4); fail-loud on bad `STAFF_SAVE_FORMAT_VERSION` / out-of-bounds
  length prefix (the overflow-safe `total − offset` `Require`) / trailing bytes.
- **KD-7 roster ownership.** A (deep) hire changes `EmployerClubId` **within #34's store** and leaves `StaffId`
  **unchanged** (no re-key); #34 requests **no** #30 roster-commit and dispatches **no** cross-system migration
  hook (a static assertion — #34 references neither #30 nor a `RequestRosterCommit`).
- **KD-1 hiring seam (deep).** `EvaluateStaffOffer` accepts iff `offeredWage ≥ wageDemand` (draw-free
  predicate, the #31 `EvaluateOffer` shape but on **wage**); reuses `NegotiationOutcome`; a hire where
  `WageBillAggregate + wage > WageBudget` (both read from #40, **no #34 counter**) **fails loud** (KD-6) and
  posts nothing (atomic — no `ApplyTransaction`); an accepted hire posts exactly `{Debit, StaffWage, wage}`.
  Hiring is **year-round** (no window gate).
- **Integer posture (KD-4).** Every `StaffAttributes`/projection/wage field is integer; #34 introduces **no**
  float (static/reflection assertion).
- **Two-run determinism (deep).** A full season's hiring activity + candidate-pool generation from a fixed
  world seed produces a byte-identical `StaffState` (keyed draws, no cursor).

## 10. Risks

- **False-reuse of #31's negotiation seam (the KD-1 headline).** Mitigated by KD-1: reuse the
  `NegotiationOutcome` enum + the atomic-commit pattern, but a **staff-specific offer/evaluator on wage** —
  because #31's `EvaluateOffer` tests `Fee` and its `Offer` carries transfer-only fields. Forcing staff through
  it would test the wrong quantity and couple the two; the reuse test is that #34's evaluator shares the enum
  and the validate-all-first shape, not the struct.
- **Multiplier double-counting (KD-3).** Staff modulation reaching a consumer (#29/#41/#33) **more than once**,
  or colliding with #33 morale / #40 facilities at the same seam. Mitigated by KD-3: #34's projection is the
  **sole** staff path into each consumer's `Identity`-typed modifier (FR-MD/FR-TR forbid a second path), and
  morale/facilities reach those consumers by their own separate seams — locked by the "no second source"
  assertion.
- **Save-scope error (WorldStore vs season save).** Dissolved by KD-4: one `STAFF_SAVE_FORMAT_VERSION`
  season-save sub-blob (the #41/#33 precedent), no `WORLD_STORE_FORMAT_VERSION` bump — staff are career state
  the season save owns.
- **A wage producer breaks FR-FN-015 (at the scaffold *or* by a parallel wage total).** Mitigated by KD-6 on
  both fronts: (a) the scaffold hires no one and posts no `StaffWage`, so `WageBillAggregate ≡ 0` holds
  verbatim (no #40 back-prop at approval; producer + relax are deep, shared ERR-040 with #31); (b) the deep
  affordability gate reads #40's running `WageBillAggregate` directly — **#34 keeps no wage counter**, because
  (unlike #31's transfer-fee counter against the static `TransferBudget`) a `committedStaffWage` accumulator
  would itself be the "parallel wage total" FR-FN-015 forbids. This is the AR-1 correction — over-analogizing
  #31's counter is the trap.
- **`CoachingModifier` is under-specified upstream (KD-3).** #29 reserved only the type name (`default`
  identity); #34 must define its field shape + #29's consumption (deep ERR-029-002). De-risked by producing
  `Identity` at the scaffold (no #29 change) and deferring the field shape to the deep tier where the coaching
  model is exercised.
- **Deferred producers/consumers land later.** Mitigated by identity seams: the #32 scout-quality projection,
  the #42 academy hook, the #33 judgement read, and the deep candidate pool all default to their scaffold
  identities until their producers/consumers wire up (FR-LW-031).

## 11. Promotion pipeline

1. Author the 11-file section set at `IN REVIEW` (FR-ST-001..NNN).
2. Section-file PASS-1 adversarial review → AR-2/AR-3 to convergence.
3. R-01..R-05 lead-developer sign-off → APPROVED; add `SPEC_INDEX.md` row 34.
4. **Back-props at approval: one** (§8) — the #30 staff tick-order null-seam slot (ERR-030-006); `0x26`/88
   stays reserved (draw-free); #41/#29/#40/#33/#31/#27/#16 unchanged.
5. T-phase (post-APPROVED): T0 value types (`StaffRecord`, `StaffAttributes`, `StaffRole`, `StaffState`) + the
   identity projections + neutral-baseline seeding (behaviour-neutral) → T1 `STAFF_SAVE_FORMAT_VERSION` sub-blob
   + season-save composition (#30 outer bump coordination) → T2 the world-tick step wired at #30's new slot
   (null at scaffold) → T3 deep candidate-pool generation (promotes `0x26`/88, ERR-016) / hiring via the #31
   negotiation pattern / `StaffWage` posts + `WageBudget` gate (ERR-040) / non-identity projections incl. the
   #29 `CoachingModifier` field shape (ERR-029-002) / #33 judgement.

## Version History
| Version | Date | Change |
|---------|------|--------|
| v0.1 | July 23, 2026 | Initial design supplement from spec-plan v0.1, grounded on the verbatim upstream seam reconnaissance (#41 `MedicalModifier`, #29 `CoachingModifier`, #40 `StaffWage`/`ApplyTransaction`/FR-FN-015, #33 `MentoringPlan`/FR-HS-022, #31 `NegotiationOutcome`/`staffMult`, #30 `RunWorldTickInFixedOrder`/`SeasonSaveCodec`, #27 `PlayerRecord` shape, #16 `_RESERVED_0x26_`). Resolves plan KD-1..KD-5 + adds KD-6 (wage deferral), KD-7 (stable `StaffId`, no re-key, no #30 roster-commit — supersedes the reconnaissance flag), KD-8 (neutrality + reserve-ahead tick slot + command boundary). One approval-time back-prop (the #30 staff tick slot, ERR-030-006); `0x26`/88 stays reserved. |
| v0.2 | July 23, 2026 | AR-1 (1H+4M+1L). **H** — KD-6/§4/§6/§9: removed the `committedStaffWageThisWindow` counter (an over-analogy to #31's transfer-fee counter that itself **violated FR-FN-015**'s "no parallel wage total"); the deep affordability gate now reads #40's running `WageBillAggregate` directly (`WageBillAggregate + wage ≤ WageBudget`) — a mandatory divergence from #31, since #40 maintains the wage liability but leaves `TransferBudget` static. **M1** — KD-2/KD-3/§6: pinned the **role-slot** model (one `StaffRecord` per role slot; each consumer reads its slot-holder) so the club→modifier reduction is a single well-defined slot read. **M2** — KD-2/KD-7/§6: pinned a serialized monotonic `StaffState.NextStaffId` allocator (high-water, never reused). **M3** — KD-1/KD-6/§4: staff hiring is **year-round** (dropped the transfer-style window cursor + gate). **M4** — §4/§2: `StaffState` is **managed-club-only** at the scaffold (AI clubs project Identity, untracked; all-clubs is deep — the #31 scope precedent). **L** — §9/KD-8: the scaffold threads only the two live seams (#41/#29); #31/#33 consume #34 at their own deep tiers. |
| v0.3 | July 23, 2026 | AR-2 (1M+1L, both regressions from AR-1). **M** — §2 still said "**every club** holds a … roster" and threaded "#29/#41/#33/#31", contradicting the AR-1 M4 managed-club-only scope (§4) and the L live-seams fix (§9); §2 reconciled (managed club only; AI clubs unstaffed → built-in Identity; scaffold threads #29/#41 only). **L** — KD-8(b) carried the same "#29/#41/#33/#31 … swaps a hardcoded default" over-claim; tightened to the two live seams (#31/#33 untouched at the scaffold). |
| v0.4 | July 23, 2026 | AR-3 (0H+0M+1L) → **CONVERGENCE**. **L** — the AR-1 M3 year-round decision had left three stale "window" terms (§4 "mid-window save"; §9 "mid-window save" + "hiring window's activity"); reconciled to "mid-negotiation save" / "a full season's hiring activity". Full hostile re-read otherwise clean at High/Medium — the loop is converged; the supplement is ready to promote to section files. |
