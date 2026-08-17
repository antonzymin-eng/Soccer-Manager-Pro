# Scouting & Player Knowledge #32 — Design Supplement

> **Created:** July 24, 2026
> **Last Updated:** July 24, 2026 (v0.4 — **PROMOTED**; prior v0.3 AR-2 0H+0M+3L + CONVERGENCE, v0.2 AR-1 0H+3M+2L, v0.1 initial)
> **Status:** DESIGN SUPPLEMENT → **PROMOTED** (July 24, 2026) — 11-file section set authored at
> `docs/specs/scouting-player-knowledge/` (FR-SC-001..027) → section-file AR PASS-1 (3M+1L) → PASS-2
> (1M+2L) → PASS-3 clean → CONVERGENCE → R-01..R-05 signed → **APPROVED**; `SPEC_INDEX.md` row 32 added
> (**39 APPROVED** — Wave 4 complete). **One approval-time cross-spec back-prop:** ERR-030-007 (the #30
> scouting tick-order step-7 null seam; `spec-error-log.md` v1.38); `0x24`/86 stays reserved (draw-free
> minimal); #34/#31/#27/#38/#16 unchanged. Section files are authoritative; this supplement is the
> design-history record. (Original status line follows for history.)
> DESIGN SUPPLEMENT (pre-promotion — no section files, no `SPEC_INDEX.md` row).
> **Candidate spec:** #32 · **FR prefix:** FR-SC (grep-verified unclaimed across `docs/specs/**` — only the roadmap/plan proposal cites it).
> **Master-plan home:** §5 recruitment · **Tier:** S3 · **Wave:** 4 (recruitment/economy cluster — after #31, which owns the negotiation seam, and #34, which supplies scouts as staff; #32 is Wave 4's third and final spec).
> **Determinism (proposed):** `DOMAIN_TAG_SCOUTING` / `SubsystemOrdinals.Scouting` = `0x24` / `86` — the roadmap §6 off-pitch reservation, **already present as the `_RESERVED_0x24_` placeholder row** in #16 §3.4 (verified `deterministic-sim/section-3.md:268`, whose rationale already says "Scouting accuracy is a documented draw site … expected to promote at #32's first draw"). **Stays RESERVED at approval** (the minimal tier is draw-free — the #40 ERR-040-001 / #31 FR-TX-016 / #34 precedent); promotes at the deep tier's first accuracy draw.
> **Source plan:** `docs/tracking/spec-plans/spec-32-scouting-player-knowledge.md` v0.1.

---

## 0. Scope

**Player knowledge as a per-manager VIEW**: scout assignments, **attribute masking / fog-of-war** (the
manager sees attribute *ranges*, not truths, until a player is scouted), scout reports, and
recommendations. Knowledge accuracy sharpens with scouting effort and scout quality. All #32 state
advances on the **world tick** (`WorldClock`, one day = one `worldTick` — never the 10 Hz/60 Hz match
loops) and persists alongside #30's season/career save.

**The roadmap §5 invariant this spec exists to honour:** knowledge is a **per-manager VIEW over #27's
true attributes and is NEVER a mutation of them.** #32 stores a knowledge overlay; the canonical
`PlayerAttributes` are untouched. Everything else in this supplement is downstream of that invariant.

**Out of scope (owned elsewhere, referenced as seams):**
- **The truth (#27 Squad/Player Data).** #27 owns `PlayerRecord`/`PlayerAttributes`/`Squad`. #32 reads
  them **read-only** and never writes; the view is a pure function over caller-supplied truth records.
- **The scouts themselves (#34 Staff & Backroom).** Scouts are staff; #34 owns the `StaffRecord` and
  publishes `ToScoutQuality(in StaffRecord scout) → int` from the ChiefScout role slot (#34 §3.1,
  XC-034-008 — "neutral ⇒ a baseline #32 will define"). #32 consumes that projection read-only (deep)
  and **defines the baseline** (`SCOUT_QUALITY_NEUTRAL_PERMILLE = 1000`), closing #34's open constant.
  #32 builds no staff modelling.
- **The transfer decision (#31 Transfers, Contracts & Negotiation).** #31 owns bids, valuation, and the
  negotiation seam. #32 produces the **knowledge the manager's decision is based on** (estimates,
  recommendations); the action stays #31's `SubmitBid`, unchanged. #32 itself issues no offers at Stage
  3 (KD-5) — the FR-TX-010 seam-reuse expectation transfers to the far-deep autonomous-AI-scouting tier.
- **Development potential (#28 Player Progression).** CA/PA live in #28. Stage-3 #32 fog covers the 31
  canonical **attributes** only; a potential-estimate ("PA star rating") surface is a deep extension
  that would read #28's career state read-only (§7 of the spec, not built here).
- **The report-rendering UI (#38 UI/Client Framework).** #38 renders ranges/reports as view models
  (FR-UI-002 immutable read-only projections — exactly the shape #32's view surface exposes). Deferred
  consumer; no interface built (FR-LW-031).
- **Report prose / localization (#49, #22).** #32 emits **structured** reports only (ids, bands,
  integer estimates). It generates **no display text** — prose is a presentation concern behind #49's
  localize-after-generate boundary, and the plan's floated `InteractionTextGenerator` (#22) consumption
  is **rejected** (it would couple #32 to the living-world assembly and spend a `world.text` draw on a
  presentation artifact). #32 never references #22.
- **The season loop + save codec + tick order (#30).** #30 owns `RunWorldTickInFixedOrder`,
  `SeasonSaveCodec`, and the outer `SEASON_SAVE_FORMAT_VERSION`; it invokes #32 at a new pre-declared
  tick-order slot and composes #32's opaque save sub-blob. #32 never references #30.

## 1. What exists vs. what #32 adds

**Exists (verified against source / approved specs — the seam reconnaissance):**

- **#27 Squad/Player Data (APPROVED, FR-SQ; `src/player-database/` built)** — the truth #32's view masks:
  `PlayerAttributes` = **31 `int [1,20]` fields** (FR-SQ-002) + `WeakFootRating [1,5]` on its own scale
  (FR-SQ-003, excluded from the 31-array); `ATTRIBUTE_COUNT = 31` `[DERIVED]`; a single named **`AttrIdx`
  ordinal map** with `ToArray()`/`FromArray(int[31])` round-trip (FR-SQ-006) — #32 keys per-attribute
  estimates by `AttrIdx` ordinal, no second 31-way switch; `PlayerRecord { PlayerId, FirstName, LastName,
  Age, Position, Attributes }`; `Squad { ClubId, Count, GetPlayer(int) }`; `CLUB_SQUAD_SIZE = 25`
  `[FIXED]`; `PlayerId = clubId·CLUB_SQUAD_SIZE + localIndex` (club-scoped). **Read-only for #32.**
- **#34 Staff & Backroom (APPROVED, FR-ST)** — the scout supply: per-club **role slots** 1:1 with
  `StaffRole` (`HeadCoach`/`HeadPhysio`/**`ChiefScout`**, FR-ST-003 — always filled, at worst by the
  neutral `NeutralHouseStaff`); `ToScoutQuality(in StaffRecord scout) → int` read from the ChiefScout
  slot (#34 §3.1) with "neutral ⇒ a baseline #32 will define" — **the one #34 constant #32 must close**;
  `StaffAttributes.ScoutJudgement [1,20]` (neutral 10) is the underlying facet, and #33
  personality/judgement reaches scouting **only through #34's projection** (#34 KD-3 sole-path
  discipline). FR-ST-021: #34 built no #32 interface (FR-LW-031). **Consume-ready (deep).**
- **#31 Transfers (APPROVED, FR-TX)** — the action layer #32 informs: `SubmitBid` is the only minimal
  transfer initiator (FR-TX-025, manager command); the counterparty valuation is a pure function of
  **true** #27 attributes (FR-TX-001 — the AI selling club knows its own player; fog applies to the
  *manager's* information, not the counterparty's); #31 §4.4 anticipates "#32 (scouting) passes a
  scout-knowledge-fogged valuation" **as the manager-side decision input**, and FR-TX-010/011 keep the
  seam generic while #31 builds no #32 interface. **No #31 change needed at #32's approval.**
- **#30 Season & Competition Loop (APPROVED, FR-SN)** — the invoker/save root:
  `RunWorldTickInFixedOrder` pinned order (verified `season-competition-loop/section-3.md`): **1
  progression(#28) · 2 training(#29) · 3 human-systems(#33) · 4 injuries(#41) · 5 transfers(#31) · 6
  staff(#34) · 7 `WorldStore.AdvanceDay()`** — **no #32 slot** (slots 4/5/6 were each added by back-prop
  at their spec's approval — ERR-030-002/004/006). Save composition: each downstream writes its **own**
  independently version-gated sub-blob into `SeasonSaveCodec` (the `MEDICAL_` / `HUMAN_SYSTEMS_` /
  `TRANSFERS_` / `STAFF_SAVE_FORMAT_VERSION` precedents, all "No `WORLD_STORE_FORMAT_VERSION` bump");
  the outer `SEASON_SAVE_FORMAT_VERSION` bump is a T-phase coordination. `SeasonCalendar` read-only.
- **#41 Injuries & Medical (APPROVED, FR-MD)** — the **keyed-draw mechanism precedent** #32's deep
  accuracy draws copy: all draws position-independent on `(playerId, worldDay, purpose)` via a
  **fixed-radix action-ordinal bijection** (#41 §3.1.1 — fixed `DRAW_PURPOSE_RADIX`, APPEND-only purpose
  ordinals, no free-running cursor, nothing serialized, same-key ⇒ same draw regardless of call order).
- **#16 §3.4** — `_RESERVED_0x24_` / ordinal `86` placeholder row **already exists**, held for #32, with
  promotion-at-first-draw already anticipated in its rationale text.
- **#38 UI framework (APPROVED, FR-UI)** — FR-UI-002 (immutable read-only view-model projections) and
  FR-UI-004 (UI computes no game state) are the contract #32's view surface will be rendered under.
  Deferred consumer.

**#32 adds:** a **per-manager knowledge overlay** (`ScoutingState` — per-player knowledge **band** +
assignment state, managed-manager-scoped), persisted in a `SCOUTING_SAVE_FORMAT_VERSION` season-save
sub-blob (KD-6); a **masked attribute-view surface** (`KnownPlayer` / `AttributeEstimate` — per-attribute
`[Min,Max]` ranges **derived on read** from the band, never stored, KD-1) that collapses to the
exact-value identity at maximal knowledge; **scout assignments** (manager command + world-tick progress
at #30's new slot, deep — KD-7); **reports** as structured band-stamp records (KD-1); a **recommendation
ranking** (pure read-only query over the masked pool, deep — KD-5); and (deep) the **accuracy noise
draws** — position-independent keyed draws that promote `0x24`/86 at the T-phase (KD-3). **No RNG stream
at the minimal tier** (fog-off is draw-free). **No mutation of any #27 state, ever** (KD-2).

## 2. Staging (omniscient identity → fog, one code path)

- **Minimal identity (always present, behaviour-neutral)** — fog is **off** (`fogEnabled = false`, the
  config-dial class of `deepTransfersEnabled`/`deepStaffEnabled`): every knowledge read resolves at the
  **maximal band** (`KNOWLEDGE_BAND_MAX`), whose error half-width is `0`, so `EstimateFor` returns
  `[truth, truth]` for all 31 attributes — the **omniscient view**. The overlay stores **no per-player
  entries**, no assignment exists, #30's #32 tick slot is empty, and **no RNG call is made** (a
  zero-width estimate short-circuits before any draw — KD-3), so a season with #32 landed is
  **byte-identical to pre-#32**; the only new state is #32's own (nearly empty) save sub-blob (the #34
  scaffold-posture precedent). Recruitment/UI behave as if fog-of-war does not exist.
- **Stage-3 deep (`fogEnabled = true`)** — an **external** unscouted player resolves at **band 0**
  (widest ranges); the **managed club's own players always resolve at `KNOWLEDGE_BAND_MAX`**
  (own-squad omniscience — an explicit short-circuit ahead of the overlay read; the manager picks the
  lineup from these very attributes, KD-2). The manager assigns the scout (`AssignScout`, a command),
  assignment progress accrues on the world tick at #30's slot (days-per-band scaled by #34 scout
  quality — KD-4), and each band-up stores the advanced band and stamps a report — the next view read
  derives the **narrower** estimate (re-centred by the stateless keyed noise for the new band, KD-3).
  At `KNOWLEDGE_BAND_MAX` the estimate **collapses back to the exact-value identity** (`[truth,
  truth]`) — the same code path the minimal tier short-circuits through, not a second one.

**One code path (KD-8):** `EstimateFor(truth, band)` is the single view function; the minimal tier calls
it with `band = KNOWLEDGE_BAND_MAX` for every player, the deep tier with the overlay's stored band. The
identity is the band-MAX row of the same width table the fog tiers use.

## 3. Dependencies & reference direction (one-way, no cycle)

- **#30 → #32** — the day-advance loop *invokes* #32's world-tick step at a **new pre-declared
  tick-order slot** (a documented null seam #30 inserts — the #41/#31/#34 ERR-030-002/004/006 pattern;
  ERR-030-007), and the composition root routes the manager's `AssignScout` commands and threads squad
  truth (via the existing `ISquadProvider` resolution) into the view calls. #30 owns the calendar/save;
  #32 reads them read-only and **never** references #30.
- **#32 → #27** — reads `PlayerRecord`/`PlayerAttributes`/`AttrIdx` **read-only** (the view input). #32
  holds no mutable reference into #27 storage: `EstimateFor` takes `in PlayerRecord` (a value copy) and
  the overlay keys by `PlayerId` alone (KD-2).
- **#32 → #34 (deep)** — reads `ToScoutQuality` of the managed club's ChiefScout slot-holder, read-only.
  The scaffold consumes it only as the identity baseline (`1000‰` — no behavioural effect at minimal).
- **#32 → #16 (deep)** — the determinism namespace + world-tick `DeterministicRngService` (only when the
  deep tier draws accuracy noise).
- **#32 does NOT reference #30, #31, #33, #28, #38, #22, or #49.** #33 judgement reaches #32 only
  through #34's projection; #31 is informed by #32 only through the human manager's decisions; #38/#46
  render/aggregate later (FR-LW-031 — no interface built).
- **Consumers (deferred, no interface built):** **#38** (renders ranges/reports), **#46** (news/inbox
  aggregation of report events), **#42** (youth scouting reuses the knowledge-overlay pattern), and the
  far-deep autonomous-AI recruitment tier (AI managers scouting with fog — the point where the
  FR-TX-010 negotiation-seam reuse becomes live).

Reference DAG: `compositionRoot → {#30, #32}`, `#32 → {#27, #34, #16}` (minimal subset `{#27}`).
**Acyclic** — no consumer references #32; #27/#34/#31/#30/#16 stay schema-untouched at approval.

## 4. Persistent state & save impact (KD-6 — a deliberate revision of the plan §4)

Adds an opaque, independently version-gated **scouting sub-blob** (`SCOUTING_SAVE_FORMAT_VERSION`
[FIXED] = 1) composed into #30's season save via the `SeasonSaveCodec` pattern — **NOT** the
`WORLD_STORE_FORMAT_VERSION` bump the plan §4 proposed. Rationale for the revision (KD-6): the
`WorldStore` composite is **#22-owned living-world state** (`WorldStateSerializer` serializes #22's own
stores — memory/arcs/cold/text-cursor); parking foreign #32 state inside it would be a layering
violation requiring a #22 schema change, and every management-layer sibling that faced the same choice
(#40 finances, #41 medical, #33 human-systems, #31 transfers, #34 staff) chose the season-save sub-blob
with an explicit "no `WORLD_STORE_FORMAT_VERSION` bump." The knowledge overlay is **per-manager career
state** exactly like #31's managed-club `TransfersState` — and the season save *is* the per-manager
career save — so the "manager-scoped" property the plan reached for lives there naturally. The block
carries:
- **Per-scouted-player overlay entries:** `(PlayerId, KnowledgeBand, LastReportWorldDay)` — **only**
  players the manager has scouted (band > 0); unstored ⇒ band 0 under fog, and the minimal tier stores
  **nothing** (fog off ⇒ the map is empty and stays empty). Estimates/ranges are **never serialized**
  (derived, KD-1) — the save cannot bloat with the pool size (the plan §9 risk dissolved by
  construction).
- **Assignment state (deep):** the active assignment `(PlayerId, DaysIntoBand)` (at most
  `MAX_ACTIVE_ASSIGNMENTS = 1` per the single ChiefScout slot at Stage 3 — a deep extension may widen
  with #34's deep staff pool), or none.
- **No `RngCursor` / no `actionOrdinal`** — all deep draws are position-independent keyed draws (KD-3),
  so nothing RNG-related is ever serialized (the FR-TX-018 posture).

Mirror the `SeasonSaveCodec` fail-loud posture exactly (version-mismatch throw, per-read `Require`
bounded against **`total − offset`** (overflow-safe), trailing-byte guard). **No
`WORLD_STORE_FORMAT_VERSION` bump.** The outer `SEASON_SAVE_FORMAT_VERSION` bump is coordinated with #30
at the T-phase (the #41/#33/#31/#34 deferral pattern — not hardcoded here). **Genesis-vs-load (the #31
§3.8 lesson):** there is nothing to seed at genesis (the empty overlay *is* the genesis state) — a load
reconstructs the overlay from the sub-blob and never resets a band (knowledge is durable career state; a
scouted player stays scouted across `RollToNextSeason` — knowledge does not expire at Stage 3;
staleness/decay is a §7 deep extension).

## 5. Determinism (KD-3 — draw-free minimal; keyed draws deep; views mutate no RNG state)

**All #32 state advances on the WORLD tick** at #30's pre-declared slot. The **minimal tier makes no
stochastic draw** — the omniscient view is a pure pass-through. Consequently:
- **`0x24`/86 stays `_RESERVED_0x24_`** at #32's approval (no `DOMAIN_TAG_SCOUTING` promotion, **no #16
  spec-text change** — the row's own rationale already anticipates promotion at #32's first draw, which
  is the **deep tier's** first accuracy draw at the T-phase). The draw-free reserved-not-promoted
  precedent of #40 (ERR-040-001) / #31 (FR-TX-016) / #34.
- **Deep accuracy draws are position-independent keyed draws** through the (then-registered)
  `scouting.accuracy` stream (`entityId = playerId`), with the action ordinal a **fixed-radix bijection**
  over `(band, attrIdx, purpose)` (the #41 §3.1.1 mechanism — fixed radices `KNOWLEDGE_BAND_RADIX` /
  `ATTR_RADIX = 32 > ATTRIBUTE_COUNT` / `DRAW_PURPOSE_RADIX`, APPEND-only purposes). **Deliberately NOT
  keyed on `worldDay`** (a divergence from #41's key, which is per-day by design): an estimate for
  `(player, band)` must be **stable across days and across repeated views** — it re-centres only when
  the band advances. Same key ⇒ same estimate, in any call order, on any day, across any save/restore.
- **Views mutate no RNG state.** `EstimateFor` derives the range from `(truth, band)` + the keyed
  noise — a **stateless computation performed on read** (no cursor-advancing draw; same key ⇒ same
  value). A band-up changes only the stored band; the next read derives the narrower estimate.
  Repeated views are identical; save→restore is byte-exact with nothing to continue (no cursor);
  re-rolling is structurally impossible (the plan's KD-3 question dissolved).
- **A zero-width estimate makes NO RNG call** — at `band = KNOWLEDGE_BAND_MAX` (every minimal-tier read)
  the derivation short-circuits before the draw, so the minimal tier is *provably* draw-free, not just
  draw-neutral.
- **Stream independence:** registering **no** stream at minimal leaves every existing cursor
  byte-identical (the #40 `_RESERVED_0x29_` property).

**Integer posture:** attributes, bands, half-widths, estimates, and quality are all `int` (per-mille
where scaled); there is **no float in #32** (the #40/#41/#31/#34 off-pitch integer discipline). One
clock (world), so no determinism-ordering fragility between loops.

## 6. Primary surfaces (proposed → pinned in §4 of the section files)

```csharp
// KD-1/KD-2 — the masked view. Pure value types; derived on read; never stored; never written back.
public readonly struct AttributeEstimate            // integer [1,20] range; Min <= Max; truth ∈ [Min,Max] (containment invariant)
{ public int Min, Max; }
public readonly struct KnownPlayer                  // the manager's view of one player
{
    public int PlayerId;                            // identity facts are PUBLIC knowledge — exact at any band:
    public int Age; public PlayerPosition Position; //   name/age/position are never fogged (Stage-3 scope: fog covers the 31 attributes)
    public int WeakFootRating;                      //   [1,5] — exact at any band at Stage 3 (identity-class; fogging its 5-point scale
    public int KnowledgeBand;                       //     would need its own width table for marginal value — a §7 extension)
    /* 31 AttributeEstimate fields in AttrIdx order (or an indexer over them) */
}

// The single view function (KD-8 — one code path; minimal calls it with band = KNOWLEDGE_BAND_MAX).
// Deep: `rng` supplies the keyed noise draws; at width 0 no draw is made (KD-3).
public static KnownPlayer EstimateFor(in PlayerRecord truth, int knowledgeBand /*, rng svc (deep) */);

// KD-1 — the overlay: band per scouted player; estimates DERIVED, not stored. Managed-manager scope.
public sealed class ScoutingState
{ /* Dictionary<PlayerId, (KnowledgeBand, LastReportWorldDay)>; active assignment (deep); NO RngCursor */ }

// KD-7 (DEEP) — assignments: a manager command + world-tick progress at #30's slot 7 (ERR-030-007).
public /* command */ void AssignScout(int playerId /* , world ctx */);       // fail-loud: unknown/own-squad PlayerId, slot busy
public /* command */ void CancelAssignment();                                // in-band progress discarded; completed bands kept
public /* #30-invoked */ void AdvanceScoutingDay(/* threaded truth + #34 scout quality */);
//   progress: DaysIntoBand++; on reaching DaysPerBand(quality) => band+1, report stamped, assignment
//   continues to KNOWLEDGE_BAND_MAX then clears. Empty at minimal (fog off => no assignments exist).

// KD-4 (DEEP) — scout quality: #34's ToScoutQuality(chiefScout), per-mille; #32 defines the baseline.
//   SCOUT_QUALITY_NEUTRAL_PERMILLE = 1000 [FIXED]  — closes #34 §3.1's "a baseline #32 will define".
//   DaysPerBand = max(1, DAYS_PER_BAND_BASE * SCOUT_QUALITY_NEUTRAL_PERMILLE / quality); quality <= 0 fails loud.
//   Quality scales assignment SPEED (DaysPerBand), never estimate widths (KD-4 — widths are f(band) alone).

// KD-5 (DEEP) — recommendation: a pure read-only ranking over the masked pool. No mutation, no bid.
public static /* ordered ids */ RankByEstimate(PlayerPosition pos, /* candidate KnownPlayers */);
//   deterministic: estimate midpoints, PlayerId tiebreak. The manager acts via #31's SubmitBid, unchanged.
```

**Estimate derivation (the §3 core, sketched):** for attribute `a` at band `b`:
`w = KNOWLEDGE_BAND_HALFWIDTH[b]` (a `[GT]` int table, strictly decreasing to `0` at
`KNOWLEDGE_BAND_MAX`); if `w == 0` ⇒ `[truth, truth]` (no draw); else `offset = KeyedDraw(playerId, b,
attrIdx, PURPOSE_CENTER) mod (2w+1) − w` (∈ `[−w, +w]`), `center = truth + offset`, estimate =
`[max(ATTRIBUTE_MIN, center−w), min(ATTRIBUTE_MAX, center+w)]`. Since `|center − truth| ≤ w`, **truth ∈
[Min, Max] always** (containment survives the `[1,20]` intersection because truth itself is in
`[1,20]`) — reports are honest-but-imprecise, and the invariant is directly testable without exposing
truth.

## 7. Key design decisions

- **KD-1 (overlay representation — a knowledge BAND per player; estimates derived, never stored; the
  live-form freshness semantic).** The plan's fork (per-attribute `[min,max]` + confidence **vs**
  point-estimate + error band) is resolved by storing **neither**: the overlay persists only an integer
  `KnowledgeBand ∈ [0, KNOWLEDGE_BAND_MAX]` per scouted player (+ a report day-stamp), and the
  per-attribute ranges are **derived on read** by the pure `EstimateFor` function (band → `[GT]`
  half-width table; keyed noise re-centre; §6). Maximal knowledge collapses to the exact-value identity
  **arithmetically** (`HALFWIDTH[BAND_MAX] = 0` ⇒ `[truth, truth]`), not via a special case. This
  dissolves both plan §9 risks at once: **save bloat** (one small entry per *scouted* player, nothing
  per attribute, nothing at minimal) and **re-roll determinism** (nothing stored can drift; nothing
  derived can re-roll — KD-3). A "report" is a band-stamp record, re-derivable at display time; #32
  serializes no estimate. **Freshness semantic (pinned):** because the range derives from **current**
  truth and #28 mutates truth over a career, a Stage-3 estimate is a **live-form window** — the *width*
  is the scouted quantity; the window's centre legitimately tracks the player's current development
  (deliberate simplification, named consequence: a range's endpoints shift as the player grows, so a
  manager watching an unscouted range over time can infer development *deltas* at band-width
  granularity without scouting effort). Frozen-at-report staleness (window pinned to last-report truth,
  which requires **storing** per-attribute snapshots — the exact state KD-1 exists to avoid) is a §7
  deep extension, as is quantized-truth centering (rejected at Stage 3: quantization error breaks the
  containment invariant unless every width absorbs it).

- **KD-2 (the view boundary — read-only by construction, not by discipline; own-squad omniscience).**
  The roadmap §5 invariant (#32 never writes #27 truth) is enforced structurally: (a) `EstimateFor`
  takes `in PlayerRecord` — a readonly value copy; #32 holds **no** reference into #27's stores (the
  composition root resolves squads via the existing `ISquadProvider` and passes records in); (b) the
  overlay keys by `PlayerId` alone and stores no attribute data; (c) `AttributeEstimate`/`KnownPlayer`
  are readonly value types (the #38 FR-UI-002 shape); (d) the assembly reference `#32 → #27` is one-way
  and #32 exposes no API taking `ref Squad`/`ref PlayerRecord`. The **load-bearing invariant test**:
  scouting a player through every #32 path leaves the #27 canonical squads **byte-identical**.
  **Own-squad omniscience:** the managed club's own players **always resolve at
  `KNOWLEDGE_BAND_MAX`** — an explicit short-circuit ahead of the overlay read (the manager selects the
  lineup from these very attributes via `LineupSelector` today; fogging them would contradict the
  existing pipeline). `AssignScout` on an own-squad `PlayerId` fails loud (nothing to scout). Fog
  covers the 31 `[1,20]` attributes of **external** players only at Stage 3; identity facts
  (name/age/position) and `WeakFootRating [1,5]` are exact at any band (identity-class — fogging the
  5-point scale would need its own width table for marginal value, a §7 extension). Consumers reaching
  *around* the overlay to truth is then a consumer-side contract: #38 is barred by FR-UI-002/004 (view
  models only); #31's counterparty valuation legitimately reads truth (the AI selling club knows its
  own player — fog is the *manager's* condition, FR-TX-001 untouched); the manager-side surfaces (#38
  screens, deep recommendation) read only `KnownPlayer`.

- **KD-3 (accuracy draw ownership — keyed draws on `(playerId, band, attrIdx)`; views mutate no RNG
  state; draw-free minimal).** The plan's fork (draw per report **vs** persistent per-pair error seed) is
  resolved with the #41 keyed-draw mechanism: deep draws are position-independent on the
  `scouting.accuracy` stream (`entityId = playerId`) with a **fixed-radix action ordinal** over `(band,
  attrIdx, purpose)` — **deliberately not `worldDay`** (an estimate must be stable until the band
  advances; #41's per-day key is the right key for injuries and the wrong one here). Same key ⇒ same
  noise forever: repeated views, saves, restores, and call orders all agree, with **no serialized seed,
  no cursor, no per-pair state** (the plan's KD-3 "persistent per-pair seed must serialize without
  bloating" concern is dissolved — the world seed + the key *is* the per-pair seed). A zero-width
  estimate short-circuits **before** the draw, so the minimal tier makes zero RNG calls and
  `_RESERVED_0x24_`/86 **stays reserved at approval**, promoting to `DOMAIN_TAG_SCOUTING = 0x24` /
  `SubsystemOrdinals.Scouting = 86` at the deep T-phase's first draw (spec-text-first, the ERR-016
  pattern; siteId `scouting.accuracy`).

- **KD-4 (scout quality → assignment SPEED, not estimate width; #32 closes #34's baseline).** #34's
  `ToScoutQuality(chiefScout) → int` (per-mille; neutral house scout ⇒ `1000`) is consumed at the deep
  tier as a **speed scalar only**: `DaysPerBand = DAYS_PER_BAND_BASE · SCOUT_QUALITY_NEUTRAL_PERMILLE /
  quality` (integer, floor, min-clamped ≥ 1; `quality ≤ 0` fails loud) — a better scout learns
  **faster**. Estimate widths stay a
  pure function of the band (KD-1). This kills a subtle retroactivity trap the plan missed: if quality
  entered the *width* formula, hiring a better scout would retroactively narrow (re-centre) every
  already-derived estimate — knowledge already reported would silently change. Speed-only keeps every
  derived estimate stable under staff turnover; a width/ceiling role for quality (e.g. a poor scout
  cannot reach `BAND_MAX` on a world-class player) is a §7 deep extension that would store the
  achieved-width band explicitly. #32 defines `SCOUT_QUALITY_NEUTRAL_PERMILLE = 1000` `[FIXED]`,
  closing #34 §3.1's open "baseline #32 will define" (value-compatible with #34's `FacetPermille`
  neutral row — no #34 change needed); #33 judgement reaches #32 **only** through #34's projection
  (#34's sole-path discipline — #32 reads no #33 surface).

- **KD-5 (recommendation/search is #32's own read-only query; #32 issues no offers).** The plan's fork
  (reuse #31's negotiation/search surface **vs** own read-only query) is resolved as **own read-only
  query**: #31 owns *negotiation*, not *search* — there is no #31 search surface to reuse, and the
  FR-TX-010 "counterparty-generic so #32/#34 reuse it" expectation is about **offers**, which Stage-3
  #32 never makes (the manager is the human; their action is #31's `SubmitBid`, informed by — not
  routed through — #32's view; #31 §4.4's "scout-knowledge-fogged valuation" is the human's decision
  input). `RankByEstimate` is a pure deterministic ranking over caller-supplied `KnownPlayer`s
  (estimate midpoint, `PlayerId` tiebreak — no draw, no mutation, no stored shortlist at Stage 3). The
  negotiation-seam reuse transfers to the far-deep autonomous-AI-scouting tier (AI managers bidding
  under fog), recorded as an XC so FR-TX-010's expectation stays honest.

- **KD-6 (persistence — a `SCOUTING_SAVE_FORMAT_VERSION` season-save sub-blob; NOT the plan's
  `WORLD_STORE` bump).** A deliberate, argued revision of plan §4 (full rationale in §4 above): the
  `WorldStore` composite is #22-owned living-world state and every management-layer sibling
  (#40/#41/#33/#31/#34) chose the season-save sub-blob; the season save is the per-manager career save,
  so the overlay's per-manager scope is preserved. Opaque, independently version-gated, fail-loud codec
  (the `SeasonSaveCodec` posture); **no `WORLD_STORE_FORMAT_VERSION` bump**; outer
  `SEASON_SAVE_FORMAT_VERSION` bump deferred to the T-phase with #30. Knowledge is **durable career
  state** (survives `RollToNextSeason`; no decay at Stage 3 — staleness is a §7 extension). The
  **retirement/transfer hygiene rule (pinned):** overlay entries key by `PlayerId`, which #31 transfers
  **re-key** (FR-TX-021) and #28 retirement retires — and the keyed noise is *also* keyed on `PlayerId`
  (KD-3), so a re-key would silently re-roll every estimate for the moved player. The rule: on any
  roster re-key/retirement event, the overlay entry for the affected `PlayerId` is **dropped** — a buy
  is then covered by the own-squad-omniscience short-circuit (KD-2), and a sell resets knowledge of the
  departed player (a Stage-3 simplification, named as such — re-scout to regain); a view query for a
  `PlayerId` no longer resolvable in the pool **fails loud** (silently returning stale knowledge for a
  re-keyed id is the trap). Delivery: a **transfer** re-key arrives via the #30 roster-move hook #31's
  FR-TX-022 dispatches (the hook lands with #31's deferred roster-commit, ERR-030-005); a
  **retirement/regen** removal arrives via the same #28 season-boundary roster-lifecycle coordination
  #31's FR-TX-028 already names ("in lockstep with #28's roster lifecycle") — the exact hook shape for
  that path is a T-phase coordination. Both are T-phase sequencing dependencies: #32's deep fog
  requires them if transfers/retirement are active (recorded in §8).

- **KD-7 (assignments — manager commands + world-tick progress at #30's reserved slot; managed-manager
  scope).** `AssignScout` is an explicit manager command (the `SetTeamTactic`/`SubmitBid`/`HireStaff`
  discipline), fail-loud on an unknown or **own-squad** `PlayerId` (KD-2 — nothing to scout) or a busy
  slot; `CancelAssignment` is its inverse (in-band `DaysIntoBand` progress discarded, completed bands
  kept — the deterministic baseline the section files pin). Progress accrues in `AdvanceScoutingDay` at
  #30's **new pre-declared tick-order slot 7** (after staff #34 — so a day's scouting reads that day's
  staff state — and before the `WorldStore.AdvanceDay()` live tick, which becomes step 8). The slot is
  **empty at minimal** (fog off ⇒ no assignments can exist — the #31/#34 deep-tier
  position-reservation pattern), declared now (reserve-ahead) as **the one approval-time back-prop,
  ERR-030-007**. Scope is the **managed manager only** (the #31 `TransfersState` / #34 `StaffState`
  precedent): AI clubs do not scout at Stage 3 — omniscient AI valuation (FR-TX-001) is the existing,
  unchanged posture; per-AI-manager overlays arrive with the far-deep autonomous-AI tier.

- **KD-8 (one code path; behaviour-neutral identity).** `fogEnabled` off ⇒ every read is the
  `BAND_MAX` row of the same tables the deep tier uses (`[truth, truth]`), zero draws, empty overlay,
  empty tick slot — a season with #32 landed is byte-identical to pre-#32 (identity in the #34
  scaffold sense: the only new artifact is the sub-blob in the save). The deep tier **narrows the same
  view seam** — no consumer ever switches code paths on the dial; only the resolved band changes.

## 8. Cross-spec back-props

**At approval: ONE cross-spec spec-text back-prop** (the #31/#34 pattern — draw-free, so no #16
promotion):
- **#30 — insert a scouting tick-order null-seam slot** (**ERR-030-007**). `RunWorldTickInFixedOrder`
  gains a documented null seam for scouting as **new step 7, after staff (#34) and before the world-day
  tick** (`WorldStore.AdvanceDay()` → step 8); FR-SN-034's enumeration + §3.3 prose updated (the
  ERR-030-002/004/006 precedent). A **deep-tier position reservation** — empty at minimal (fog off ⇒ no
  assignments), declared now so the deep daily assignment processing lands without a tick-order re-pin.
  Positioned after #34 so a scouting day reads the day's staff state (the scout doing the scouting).
- **#16 §3.4 — no change.** `_RESERVED_0x24_`/86 already exists, already names #32, and already
  anticipates promotion at #32's first draw — which is the deep T-phase, not approval (the #40/#31/#34
  reservation posture).
- **#34, #31, #27, #38 — no change at approval.** #34's `ToScoutQuality` open baseline is closed by a
  **#32-owned constant** (`SCOUT_QUALITY_NEUTRAL_PERMILLE = 1000`, value-compatible with #34's neutral
  `FacetPermille` row — #34's spec text already defers the definition to #32, so no #34 edit is
  needed); #31's FR-TX-010/011 and #38's FR-UI-002 already record their #32-facing contracts from
  their own side.

**At the #32 T-phase (deferred, lands with code):**
- **#30** — the outer `SEASON_SAVE_FORMAT_VERSION` bump composing the new `SCOUTING_SAVE_FORMAT_VERSION`
  sub-blob (coordinated at T1); the roster-event hygiene hook consumption (KD-6) when #31's
  roster-commit (ERR-030-005) lands.
- **#16** — `DOMAIN_TAG_SCOUTING = 0x24` / `SubsystemOrdinals.Scouting = 86` promotes at the deep
  tier's first accuracy draw (T3), spec-text-first, stream `scouting.accuracy` registered at the draw
  site.

## 9. Test focus

- **View-not-mutation lock (KD-2, the headline).** Exercising every #32 path — view reads at every
  band, assignment lifecycle, band-ups, reports, ranking — leaves the #27 canonical squads
  **byte-identical** (the roadmap §5 invariant test).
- **Behaviour-neutral identity (KD-8).** A fog-off season advances byte-identical to pre-#32; the
  omniscient view equals truth per-attribute for every player (`Min == Max == truth`, all 31 via
  `AttrIdx`); no stream registered; zero RNG calls (cursor-untouched assertion, the #40
  `T-FN-NEU-003` class).
- **Containment + collapse invariants (KD-1).** For every band: `Min ≤ Max`, both ∈ `[1,20]`, and
  **truth ∈ [Min, Max]**; half-widths strictly decrease in band; `band = KNOWLEDGE_BAND_MAX` ⇒
  `[truth, truth]` exactly.
- **Derived-estimate stability (KD-3).** Same `(playerId, band)` ⇒ bit-identical estimate across
  repeated views, across days, across save→restore, and across call orders; the estimate changes
  **only** on a band advance; a zero-width read makes no RNG call.
- **Own-squad omniscience + roster hygiene (KD-2/KD-6, deep).** A managed-club player resolves
  `[truth, truth]` at any overlay state; `AssignScout` on an own-squad id fails loud; a
  re-key/retirement event drops the affected overlay entry (buy → own-squad rule takes over; sell →
  knowledge reset); a view query for an unresolvable `PlayerId` fails loud; `WeakFootRating` and
  identity facts are exact at every band.
- **Assignment lifecycle + quality scaling (KD-4/KD-7, deep).** Band-up cadence follows
  `DaysPerBand(quality)` (neutral scout = base cadence; better = faster; floor-clamp ≥ 1; `quality ≤ 0`
  fails loud); assignment completes at `BAND_MAX` and clears; `CancelAssignment` discards in-band
  progress and keeps completed bands; `AssignScout` fail-loud on an unknown/unresolvable `PlayerId` or
  a busy slot; the #30 slot is a no-op at minimal.
- **Save round-trip (KD-6).** The scouting sub-blob round-trips field-identical (empty at minimal;
  populated bands + a mid-assignment cursor at deep) and survives `RollToNextSeason`; contains **no**
  `RngCursor` (schema-shape assertion); fail-loud on bad `SCOUTING_SAVE_FORMAT_VERSION` /
  out-of-bounds length prefix (overflow-safe `total − offset`) / trailing bytes.
- **Two-run determinism (deep).** A full season of assignments/reports from a fixed world seed
  produces a byte-identical `ScoutingState` and identical derived estimates.
- **Integer posture.** No float anywhere in #32 (static/reflection assertion — the #40/#41/#31/#34
  lock).
- **Fail-loud gates.** Knowledge query for a player absent from the resolvable pool; malformed overlay
  entry (band out of `[0, BAND_MAX]`); `default`-struct zero-value traps at consuming seams.

## 10. Risks

- **The view-not-mutation invariant (KD-2)** is the recurring trap the roadmap §5 calls out by name —
  any write of true attributes is a correctness failure, not a bug-of-degree. Mitigated structurally
  (value-copy inputs, no storage reference, readonly view types) + the byte-identity lock, not by
  discipline alone.
- **Deviating from the plan's `WorldStore` persistence (KD-6).** The plan proposed a
  `WORLD_STORE_FORMAT_VERSION` bump; this supplement argues the season-save sub-blob instead. The risk
  is an unexamined precedent break — mitigated by the explicit rationale (§4): #22-owned composite,
  five sibling precedents, and the season save already being the per-manager career save. If review
  rejects this, the fallback is a #22 extensibility seam — a materially bigger change, which is the
  point of resolving it at supplement stage.
- **Retroactive estimate shifts (KD-4).** Quality-in-the-width-formula would let a staff change
  silently rewrite reported knowledge. Dissolved by speed-only quality; re-opens (deliberately, with
  stored achieved-widths) only as a §7 extension.
- **Stale knowledge under re-key/retirement (KD-6).** Overlay entries — and the keyed noise — key by
  `PlayerId`, which #31 re-keys and #28 retires. Pinned: drop-on-roster-event + fail-loud view for
  unresolvable ids (never silent staleness); the drop consumes #31's roster-move hook, a T-phase
  sequencing dependency (§8).
- **The live-form freshness leak (KD-1).** The pinned Stage-3 semantic lets a watched range shift with
  #28 development (delta inference at band-width granularity, no scouting effort). A named, accepted
  limitation — frozen-at-report staleness is the §7 extension that closes it, at the cost of stored
  per-attribute snapshots.
- **Save bloat / re-roll determinism** — the two plan §9 mechanical risks — are dissolved by
  construction (KD-1 derived estimates; KD-3 keyed draws), not mitigated at runtime.
- **Phantom consumers.** #38 rendering, #46 aggregation, #42 youth scouting, AI-manager fog — all
  deferred with no interface built (FR-LW-031); #32's minimal surface is consumed by nothing and must
  stay behaviour-neutral until they land.

## 11. Promotion pipeline

1. Author the 11-file section set at `IN REVIEW` (FR-SC-001..NNN).
2. Section-file PASS-1 adversarial review → AR-2/AR-3 to convergence.
3. R-01..R-05 lead-developer sign-off → APPROVED; add `SPEC_INDEX.md` row 32.
4. **Back-props at approval: one** (§8) — the #30 scouting tick-order null-seam slot (ERR-030-007);
   `0x24`/86 stays reserved (draw-free minimal); #34/#31/#27/#38/#16 unchanged.
5. T-phase (post-APPROVED): T0 value types (`AttributeEstimate`, `KnownPlayer`, `ScoutingState`) + the
   `EstimateFor` identity path + the fog-off pass-through (behaviour-neutral) → T1
   `SCOUTING_SAVE_FORMAT_VERSION` sub-blob + season-save composition (#30 outer bump coordination) →
   T2 the world-tick step wired at #30's slot 7 (null at minimal) + `AssignScout` command → T3 deep
   fog: band tables, keyed accuracy draws (promotes `0x24`/86, ERR-016), #34 `ToScoutQuality`
   consumption, reports + `RankByEstimate`, roster-event hygiene.

## Version History
| Version | Date | Change |
|---------|------|--------|
| v0.1 | July 24, 2026 | Initial design supplement from spec-plan v0.1, grounded on the verbatim upstream seam reconnaissance (#27 `PlayerAttributes`/`AttrIdx`/FR-SQ-002/006, #34 `ToScoutQuality`/ChiefScout/FR-ST-021, #31 FR-TX-001/010/011/025, #30 tick order steps 1–7 + `SeasonSaveCodec`, #41 §3.1.1 fixed-radix keyed draws, #16 `_RESERVED_0x24_`, #38 FR-UI-002). Resolves plan KD-1..KD-5 + adds KD-6 (season-save sub-blob — a deliberate revision of plan §4's `WorldStore` bump), KD-7 (managed-manager scope + the reserve-ahead #30 slot), KD-8 (one code path). One approval-time back-prop (the #30 scouting tick slot, ERR-030-007); `0x24`/86 stays reserved. |
| v0.2 | July 24, 2026 | AR-1 (0H+3M+2L). **M-1** — KD-2/KD-6/KD-7/§2/§6/§9: the deep tier as written fogged the managed club's **own squad** and never addressed roster transitions; pinned **own-squad omniscience** (own players always `BAND_MAX`, `AssignScout` on own-squad fails loud) + the **re-key/retirement hygiene rule** (drop-on-roster-event — buy → own-squad rule, sell → knowledge reset; fail-loud view for unresolvable ids; consumes #31's roster-move hook, T-phase sequencing recorded). **M-2** — KD-1/§10: knowledge-freshness under #28 progression was unspecified (derived-from-live-truth slides the window for free); pinned the **live-form window** semantic explicitly (width is the scouted quantity; delta-visibility named as an accepted limitation; frozen-at-report staleness + quantized centering recorded as §7 extensions with their costs). **M-3** — KD-2/§6: `WeakFootRating [1,5]` was unaddressed; pinned exact-at-any-band (identity-class) with fog-it as a §7 extension. **L-1** — §2/§5: "views never draw" reconciled to "views mutate no RNG state" (stateless keyed noise computed on read; band-up stores only the band). **L-2** — KD-4/KD-7/§6: `CancelAssignment` named (in-band progress discarded, completed bands kept) + the `quality ≤ 0` fail-loud gate on the `DaysPerBand` divisor. |
| v0.3 | July 24, 2026 | AR-2 (0H+0M+3L) → **CONVERGENCE** (L-only round per the project convention). **L-1** — the §5 and KD-3 headings still said "views never draw" after the AR-1 body reconciliation; reworded to "views mutate no RNG state". **L-2** — KD-4's normative formula sentence gains the `quality ≤ 0` fail-loud gate its §6/§9 siblings already carried. **L-3** — KD-6's retirement drop over-cited the FR-TX-022 roster-move hook (transfer-shaped); split the delivery citation — transfers via the FR-TX-022 hook, retirement/regen via the #28 season-boundary lifecycle coordination FR-TX-028 names, exact shape a T-phase coordination. Full hostile re-read otherwise clean at High/Medium — the supplement is ready to promote to section files. |
| v0.4 | July 24, 2026 | **PROMOTED** — 11-file section set authored at `docs/specs/scouting-player-knowledge/` (FR-SC-001..027), section-file AR to convergence (PASS-1 3M+1L → PASS-2 1M+2L → PASS-3 clean), R-01..R-05 signed, APPROVED; `SPEC_INDEX.md` row 32; ERR-030-007 filed (`spec-error-log.md` v1.38, `season-competition-loop` section-2/3 v0.7). Wave 4 complete. |
