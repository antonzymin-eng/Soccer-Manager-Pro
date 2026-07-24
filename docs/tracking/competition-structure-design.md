# Competition Structure #43 — Design Supplement

> **Created:** July 24, 2026
> **Last Updated:** July 24, 2026 (v0.4 — **PROMOTED**; prior v0.3 AR-2 CONVERGENCE, v0.2 AR-1, v0.1 initial)
> **Status:** DESIGN SUPPLEMENT → **PROMOTED** (July 24, 2026) — 11-file section set authored at
> `docs/specs/competition-structure/` (FR-CP-001..025) → section-file AR PASS-1 (1M+1L) → PASS-2 clean →
> CONVERGENCE → R-01..R-05 signed → **APPROVED**; `SPEC_INDEX.md` row 43 added (**40 APPROVED**). **One
> approval-time back-prop:** ERR-043-001 (the #16 §3.4 A-04 placeholder sweep `_RESERVED_0x2B_`/`_0x2C_`/
> `_0x2D_`; `spec-error-log.md` v1.39); no #30/#40 change (seams pre-reserved); ERR-030-008 soft-reserved
> (T-phase). Section files are authoritative; this supplement is the design-history record. (Original
> status line follows for history.)
> DESIGN SUPPLEMENT (pre-promotion — no section files, no `SPEC_INDEX.md` row).
> **Candidate spec:** #43 · **FR prefix:** FR-CP (grep-verified unclaimed across `docs/specs/**` — only the roadmap/plan proposal cites it).
> **Master-plan home:** §4.1 / §5 · **Tier:** Stage 2 minimal → Stage 5 deep · **Wave:** 5 (first of the wave — #44/#42/#45 follow).
> **Determinism (proposed):** `DOMAIN_TAG_COMPETITION` / `SubsystemOrdinals.Competition` = `0x2C` / `94` — the roadmap §6 reservation. **The #16 §3.4 catalogue currently ends at `0x2A`** (verified — no `_RESERVED_0x2B_`..`_0x2D_` rows exist), so #43's approval files the **A-04 gap-rule placeholder sweep** (`_RESERVED_0x2B_` #42 / `_RESERVED_0x2C_` #43 / `_RESERVED_0x2D_` #45 — completing the roadmap §6 block, the v1.0.13 precedent). **`0x2C` stays RESERVED at approval** (the minimal tier is draw-free — the #40/#31/#34/#32 precedent); promotes at the deep tier's first knockout draw.
> **Source plan:** `docs/tracking/spec-plans/spec-43-competition-structure.md` v0.1.

---

## 0. Scope

**The competition set as a first-class collection**: cups, continental competitions, and
promotion/relegation over the season loop #30 ships. #30 delivers one single-division round-robin
league; #43 makes competitions a **collection** — multiple concurrent competitions a club is entered
in, knockout brackets with **deterministic draws**, and a **season-boundary promotion/relegation
transform** at the insertion point #30 already reserved (FR-SN-031 (a')). All #43 state advances on
the world tick / fixture-day flow and persists alongside #30's season/career save.

**Out of scope (owned elsewhere, referenced as seams):**
- **The base fixture/table engine (#30).** #30 owns `FixtureScheduler.Generate(clubIds, seed)`
  (FR-SN-001 — a pure function of `(clubIds, seed)`), `LeagueTable` (+ `Empty(clubIds)`),
  `SeasonCalendar`, `AdvanceAndPlayNextRound`, and the boundary roll. **These are already
  competition-instance-shaped** (parameterized by club set + seed; #30 §7's own generalization row:
  "multiple competitions reuse `FixtureScheduler`/`LeagueTable` per competition") — #43 **reuses
  them per instance and rewrites nothing** (the plan §9 rewrite risk is dissolved by verification,
  not mitigation).
- **Match play (`MatchEngine`)** and the non-managed round-resolution model (#30 FR-SN-013a) —
  #43's fixtures resolve through the same paths #30's league fixtures do.
- **Discipline / suspension scoping (#44).** #43 carries a `CompetitionId` on its fixtures/results
  so #44 can scope suspensions per competition; #43 builds no discipline model (FR-LW-031).
- **National-team tournaments (#36).** #36 overlays the same calendar/competition model later; no
  interface built.
- **Finances / prize money (#40).** #40's `SettleFinances` runs at (b') **after** #43's (a') so
  budgets read the post-promotion division (the ordering #40 §1 pinned at its own approval);
  per-competition prize money is a #40 deep extension. #43 owns no money.
- **The season save root (#30).** `SeasonSaveCodec` composes #43's opaque sub-blob; #43 never
  references #30's assembly (composition-root threading, the established one-way direction).

## 1. What exists vs. what #43 adds

**Exists (verified against approved spec text — the seam reconnaissance):**

- **#30 Season & Competition Loop (APPROVED, FR-SN):**
  - `FixtureScheduler.Generate(clubIds, seed)` — pure, deterministic, double round-robin over an
    arbitrary club set (FR-SN-001/002), odd-`N` bye handling (FR-SN-004). **Instance-ready.**
  - `LeagueTable` + `Empty(clubIds)` + the pinned total-order tie-break (FR-SN-007). **Instance-ready.**
  - `RollToNextSeason` (§3.5) with **FR-SN-031's pre-declared insertion point (a')** — "#43's
    promotion/relegation transform inserts HERE" — between table-finalize and fixture-regenerate,
    **before** #40's (b') finance settlement (ERR-030-003's pinned ordering: budgets read the
    post-promotion division). **The boundary-step position needs NO new #30 back-prop.**
  - The quick-sim round-resolution model (FR-SN-013a) drawing **position-independent keyed draws**
    "keyed on `(seed, seasonNumber, roundIndex, homeClubId, awayClubId)`" — the keyed-draw shape
    #43's knockout draws copy.
  - `SeasonViewModel` read-only value-copy discipline (FR-SN-033) — the shape #43's bracket views
    follow.
  - #30 §7's generalization table names #43's seams verbatim: "the boundary-roll insertion point
    (a') + fixture/table types taking a competition set as *data*."
- **#40 Club Finances (APPROVED, FR-FN):** `SettleFinances` at (b'), explicitly "positioned AFTER
  the FR-SN-031 (a') #43 promotion/relegation insertion point, because the budget depends on the
  club's post-promotion division" (#40 §1). **The (a')→(b') dependency is already recorded from
  #40's side.**
- **#16 §3.4:** the catalogue's off-pitch block ends at `DOMAIN_TAG_INJURIES_MEDICAL = 0x2A`;
  **no placeholder rows exist for `0x2B`/`0x2C`/`0x2D`** (#42/#43/#45 per roadmap §6) — the A-04
  "every allocation gap must have an explicit placeholder" sweep is #43's one approval-time
  back-prop (§8).
- **#41 §3.1.1 / #32 §3.3** — the fixed-radix keyed-ordinal mechanism (APPEND-only purposes, fixed
  radices, no cursor) #43's draw ordinals reuse.

**#43 adds:** a **competition registry** (`CompetitionSet` — per-competition format, entrant set,
and per-instance fixture/table/bracket state, with the #30 league as **instance 0 by reference**,
KD-1); a **format abstraction** (`CompetitionFormat { RoundRobin, Knockout, GroupThenKnockout }` —
a league is a round-robin competition, not a separate type); **deterministic knockout draws**
(keyed draws over a canonical entrant order — KD-2/KD-7, revising the plan's serialized-cursor
proposal); **persisted bracket state** (KD-3); the **promotion/relegation transform** at (a')
(KD-4); a **deep-tier merged fixture-day view** for concurrent competitions (KD-5); and a
`COMPETITION_SAVE_FORMAT_VERSION` season-save sub-blob (KD-6). **No RNG stream at the minimal
tier** (a singleton-league collection makes no draw). **No #30 rewrite** — #43 composes #30's pure
instance-ready machinery.

## 2. Staging (singleton-collection identity → multi-competition, one code path)

- **Stage-2 minimal (always present, behaviour-neutral)** — the collection holds exactly **one
  competition: the #30 league, as a binding row** (an id/tag recording "the league lives in #30" —
  no data migration, no second copy, no stored #30 object, KD-1). Every instance-0 read goes
  through the composition root against #30's read surface;
  no draw is made, no bracket exists, no promotion/relegation runs (a one-division world has
  nowhere to promote to), and the (a') insertion point stays empty exactly as #30 shipped it. A
  season advances **byte-identical to pre-#43**; the only new artifact is #43's own (nearly empty)
  save sub-blob (the #34/#32 posture).
- **Deep (populating the collection)** — a domestic knockout cup, then a group+knockout continental
  competition, each an **instance** with its own entrant set + format driving the same
  `FixtureScheduler`/`LeagueTable`/resolution machinery; **knockout draws** on the (then-registered)
  `competition.draws` keyed stream; a **second division** as another round-robin instance, which
  activates the **(a') promotion/relegation transform** (bottom-N ↔ top-N swap over final
  standings); and the **merged fixture-day view** interleaving competitions congestion-free (KD-5).
  Populating the collection is data, not a rewrite — the league instance's path never changes.

## 3. Dependencies & reference direction (one-way, no cycle)

- **compositionRoot → {#30, #43}** — the season loop drives fixture days and the boundary roll; at
  minimal it runs exactly #30's path (instance 0); at deep it consults #43's merged fixture-day
  view and invokes per-instance resolution + the (a') transform. #43 never references #30's
  assembly — instance 0 wraps state the root threads in, and #43's own instances use #30's **pure
  types** (`FixtureScheduler`/`LeagueTable` — verified pure functions/value machinery) via the
  root.
- **#43 → #27** — the club universe competitions are drawn over (read-only club ids).
- **#43 → #16 (deep)** — the `competition.draws` keyed stream (`entityId = competitionId`) at the
  first knockout draw.
- **#43 does NOT reference #40, #44, #36, #38, #22.** Prize money stays #40-side (reading the
  post-promotion table via #30, the recorded (a')→(b') flow); #44/#36/#38 are deferred consumers
  (FR-LW-031).

Reference DAG: `compositionRoot → {#30, #43}`, `#43 → {#27, #16}` (minimal subset `{#27}`).
**Acyclic**; #30/#40/#16/#27 schema-untouched at approval except the #16 placeholder sweep (§8).

## 4. Persistent state & save impact (KD-6)

An opaque, independently version-gated **competition sub-blob** (`COMPETITION_SAVE_FORMAT_VERSION`
[FIXED] = 1) composed into #30's `SeasonSaveCodec` (the #41/#33/#31/#34/#32 precedent; **no**
`WORLD_STORE_FORMAT_VERSION` bump; the outer `SEASON_SAVE_FORMAT_VERSION` bump is a T1
coordination). Contents:
- **The registry:** per non-league competition — `CompetitionId`, format, entrant `ClubId` set
  (canonical ascending order), per-instance season-scoped state. **Instance 0 (the league) is NOT
  duplicated here** — it lives in #30's own season blob exactly as today (KD-1); the registry
  records only its id binding. At minimal the registry holds nothing beyond that binding —
  a version tag + the instance-0 row.
- **Bracket state (deep, KD-3):** per knockout competition, the resolved rounds — entrant lists
  per round + winners — **persisted, not regenerated** (serialize-don't-regenerate, the #28 KD-4
  discipline), canonical order, fail-loud coherence gates (a winner must be one of its pairing's
  two entrants; a round's entrant count must halve).
- **Division membership (deep):** which division instance each club sits in (the promotion/
  relegation transform's subject) — season-scoped, rolled at (a').
- **No `RngCursor`** — draws are keyed (KD-2), nothing RNG-related serializes (the FR-TX-018 /
  FR-SC-014 posture). **This revises the plan §4/§5 serialized-draw-cursor proposal** (see KD-2).

Codec posture: version-gate first, overflow-safe `Require` against `total − offset`, trailing-byte
guard, canonical-order decode gates (ascending `CompetitionId`; ascending entrant `ClubId`s).
Genesis-vs-load: minimal genesis is the instance-0 binding only; a load reconstructs and never
re-seeds/re-draws (a drawn bracket restores from the blob, never re-rolls — KD-3).

## 5. Determinism (KD-2/KD-7 — draw-free minimal; keyed draws deep; canonical order everywhere)

- **The minimal tier makes no draw** (a singleton league collection; round-robin fixtures stay
  #30's pure function). So **`0x2C`/94 stays `_RESERVED_0x2C_` at approval** — after the A-04 sweep
  *creates* that row (§8) — promoting to `DOMAIN_TAG_COMPETITION = 0x2C` / `SubsystemOrdinals.
  Competition = 94` at the deep tier's first knockout draw (spec-text-first, the ERR-016 pattern;
  siteId `competition.draws`).
- **Knockout/group draws are position-independent keyed draws** (revising the plan's
  serialized-cursor proposal — the cursor was the #26 `match-flow.card-severity` precedent, which
  is a *match-tick* pattern; the off-pitch siblings #41/#32 and #30's own FR-SN-013a quick-sim all
  key): stream `competition.draws`, `entityId = competitionId`, fixed-radix ordinal over
  `(seasonNumber, roundIndex, slotIndex, purpose)` (APPEND-only purposes; fixed radices). Same key
  ⇒ same draw across call orders, saves, and restores; **two competitions drawing on the same day
  cannot perturb each other** (distinct `entityId`) — the plan's KD-2 question dissolves; **nothing
  is serialized** — the plan §4 "cursor serialized" clause is dropped.
- **Canonical entrant ordering (KD-7 — the plan §9 headline trap).** Every draw operates over the
  entrant set in **ascending `ClubId` order** (the canonical base), and the drawn permutation is a
  keyed-draw Fisher–Yates over that base (draw `i` keyed with `slotIndex = i`). No iteration over
  an unordered collection ever feeds a draw; the registry stores entrant sets canonically and the
  decode gate enforces it (§4).
- **Promotion/relegation is a pure deterministic transform** over final standings (no draw): the
  bottom `RELEGATION_COUNT` of division `d` swap with the top `PROMOTION_COUNT` of division `d+1`,
  ties already impossible (#30's FR-SN-007 total order). Runs at (a'), inside #30's restartable
  boundary roll (FR-SN-029's mid-roll-save contract extends over it).
- **Integer posture:** ids, rounds, slots, counts — all `int`; no float in #43.

## 6. Primary surfaces (proposed → pinned in §4 of the section files)

```csharp
public enum CompetitionFormat : byte { RoundRobin = 0, Knockout = 1, GroupThenKnockout = 2 }  // ordinal-stable

// KD-1 — a competition INSTANCE; a league is a RoundRobin instance, not a separate type.
public sealed class Competition
{
    /* int CompetitionId;  CompetitionFormat Format;  int[] EntrantClubIds (canonical ascending);
       CompetitionId is [FIXED] config-assigned at genesis, deterministic; instance 0 = 0 (KD-1).
       RoundRobin: Fixture[] + LeagueTable (the #30 types, per instance). Instance 0 is a BINDING
                   ROW ONLY — an id/tag meaning "the league lives in #30"; #43 holds NO #30 object
                   (any instance-0 read goes through the composition root against #30's read
                   surface — FR-SN-032/033 respected, KD-1);
       Knockout:   BracketState (persisted rounds — entrants per round + winners, KD-3);
       GroupThenKnockout: group instances (RoundRobin over subsets; group assignment is a keyed
                   draw, PURPOSE_GROUP_ASSIGN — APPEND-only) + a BracketState. */
}
public sealed class CompetitionSet { /* registry: instance-0 binding + non-league instances; canonical order */ }

// KD-2/KD-7 (DEEP) — the knockout draw: keyed Fisher–Yates over the canonical entrant order.
public static int[] DrawRound(int competitionId, int seasonNumber, int roundIndex, int[] canonicalEntrants /*, rng */);
//   draw i keyed on (competitionId | seasonNumber, roundIndex, slotIndex = i, PURPOSE_PAIRING);
//   pure given the world seed — no cursor, no serialized RNG state. Pairings: drawn[0]v[1], [2]v[3], …

// KD-4 (DEEP) — the (a') season-boundary transform (FR-SN-031's reserved point; before #40's (b')).
public static void ApplyPromotionRelegation(/* division tables (final, #30 total order), membership registry */);
//   bottom RELEGATION_COUNT of div d  <->  top PROMOTION_COUNT of div d+1; deterministic; no draw;
//   changes MEMBERSHIP only — ClubIds are stable (no re-key, no #27/#31-style migration).

// KD-5 (DEEP) — the merged fixture-day view (concurrent competitions, congestion-free).
public /* read */ NextFixtureDayView MergedNextFixtureDay(/* per-competition round→day mappings */);
//   deterministic slotting: cup rounds only on league-free days; one fixture per club per day;
//   #30's own SeasonCalendar (the league's mapping) is UNCHANGED — the root queries the merged view
//   only when the collection has >1 competition (minimal path untouched).
```

## 7. Key design decisions

- **KD-1 (a league IS a competition instance; instance 0 is a BINDING, not a stored reference).**
  The plan's format fork (degenerate instance vs a type union) resolves to the **degenerate
  instance**: `CompetitionFormat.RoundRobin` with no bracket. The minimal collection is
  `{instance 0}` where instance 0 is a **binding row only** — an id/tag recording "the league lives
  in #30"; #43 holds **no** #30 object or live reference (a stored reference would bypass
  FR-SN-032's sole-writer/command-API discipline and couple the assemblies §3 keeps uncoupled).
  Any instance-0 read goes through the composition root against #30's read surface (FR-SN-033's
  value-copy discipline). No migration, no duplication, no #30 schema change. This is exactly the
  generalization #30 §7 pre-declared ("fixture/table types taking a competition set as data"), and
  it makes the behaviour-neutral proof structural: the minimal path *is* #30's path.
  `CompetitionId` is `[FIXED]` config-assigned at genesis (deterministic; instance 0 = 0; never
  reused).
- **KD-2 (draws are keyed, not cursor-based — a revision of the plan §4/§5).** The plan proposed a
  serialized draw cursor (the `match-flow.card-severity` precedent) — but that is the *match-tick*
  pattern; every off-pitch sibling (#41 occurrence, #32 accuracy, #30's own FR-SN-013a quick-sim)
  uses **position-independent keyed draws**, and a cursor would be new serialized state with an
  ordering hazard (two competitions drawing the same day would race the cursor — the plan's own
  KD-2 worry). Keyed draws dissolve both: `entityId = competitionId` isolates competitions;
  `(seasonNumber, roundIndex, slotIndex, purpose)` fixed-radix ordinals make every draw a pure
  function of the world seed + key; nothing serializes. Minimal makes zero draws ⇒ the reservation
  stands at approval.
- **KD-3 (brackets are persisted, not regenerated).** Entrants change as rounds resolve (the plan's
  own observation), so a bracket is **state**, not a derivation: `BracketState` persists each
  round's entrant list + winners (serialize-don't-regenerate, the #28 KD-4 discipline), with
  fail-loud coherence gates (winner ∈ pairing; halving counts; canonical order). The keyed draws
  make a *re-derivation cross-check* possible in tests (T-CP-DET), but the blob is authoritative on
  load — a restore never re-rolls.
- **KD-4 (promotion/relegation at the pre-declared (a'); membership-only; no re-key; the
  mechanical hook named).** FR-SN-031 already reserves the point and #40 already pins (b') after
  it — #43 slots in with **no #30 spec-text back-prop**. The transform is a pure swap over the
  divisions' final standings (bottom-N ↔ top-N, `[GT]` counts), mutating **division membership
  only**: `ClubId`s are stable world identities (#27), squads/finances/knowledge key by
  `ClubId`/`PlayerId` and never notice — no re-key, no migration hook (the #34 KD-7 simplicity
  class, deliberately unlike #31's player re-key). **The mechanical seam (pinned):** the
  transform's membership output must be applied to **every** division instance's entrant set —
  including instance 0's `SeasonState.ClubIds`, which is #30-owned and mutable only through #30's
  command API (FR-SN-032) — **before** the roll's step (c) regenerates fixtures. The code-side
  hook that lets (a') execute inside `RollToNextSeason` (a roll parameter/delegate or an
  API-mediated root sequencing) is a **T-phase #30 coordination, folded into the soft-reserved
  ERR-030-008** — the (a') *position* is pre-declared spec text; the *hook* lands with #43 T2's
  second division, as its own reviewed change. Runs inside #30's restartable roll, so FR-SN-029's
  mid-roll determinism covers it.
- **KD-5 (concurrent scheduling — a #43-owned merged view; #30's calendar untouched).** #30's
  `SeasonCalendar` is and stays the **league's** round→day mapping. At deep, each competition
  instance owns its own mapping, and #43 exposes a **merged next-fixture-day view** built by a
  deterministic slotting function: cup rounds are assigned only to league-free days, one fixture
  per club per day (the #30 FR-SN-003 invariant lifted to the collection). The composition root
  queries the merged view **only when the collection has >1 competition** — the minimal path never
  changes. The deep driver that resolves a multi-competition fixture day (which instance's fixtures
  play today) is a **named T-phase coordination with #30** (soft-reserved as a future ERR-030-008;
  deliberately NOT filed at approval — the minimal tier does not need it, and FR-SN-032's
  sole-writer contract is respected by driving everything through the root).
- **KD-6 (persistence — own sub-blob; instance 0 never duplicated).** `COMPETITION_SAVE_FORMAT_
  VERSION` = 1 opaque sub-blob (§4); the #41/#33/#31/#34/#32 precedent. The league stays in #30's
  blob — one source of truth; #43's blob is nearly empty at minimal. Canonical-order decode gates;
  no RNG state.
- **KD-7 (canonical entrant ordering — the draw-determinism discipline).** Ascending-`ClubId`
  canonical base everywhere (registry storage, decode gate, draw input); the drawn permutation is
  keyed Fisher–Yates over that base. The plan §9 iteration-order trap is closed by pinning the
  order at every surface that could feed a draw.
- **KD-8 (behaviour-neutral identity).** Minimal = the singleton collection delegating to #30
  state: no draw, no stream, no transform, the (a') point empty, the sub-blob nearly empty — a
  season is byte-identical to pre-#43. Deep populates the collection; the league instance's code
  path never changes.

## 8. Cross-spec back-props

**At approval: ONE cross-spec spec-text back-prop:**
- **#16 §3.4 — the A-04 placeholder sweep (ERR-043-001):** add `_RESERVED_0x2B_` (held for Youth
  Academy #42, `SubsystemOrdinals` 93), `_RESERVED_0x2C_` (held for Competition Structure #43,
  `SubsystemOrdinals.Competition = 94`), and `_RESERVED_0x2D_` (held for Board & Ownership #45,
  `SubsystemOrdinals` 95) — completing the roadmap §6 contiguous block `0x20`–`0x2D` (the v1.0.13
  "every allocation gap must have an explicit placeholder" precedent; the catalogue currently ends
  at `0x2A`, and #43 is the first of the three to reach it). `_RESERVED_0x2C_` **stays reserved**
  at #43's approval (draw-free minimal, KD-2); promotes at the deep first knockout draw. Pure
  namespace reservation; no `DETERMINISM_DIGEST_VERSION` bump.
- **#30 — NO back-prop at approval.** The (a') insertion point + FR-SN-031 already exist
  (pre-declared at #30's authoring); no tick-order slot is needed (#43 has no daily work —
  fixtures resolve on fixture days, draws at round completion, the transform at the boundary
  roll). The **code-side** (a') hook + deep driver are T-phase coordinations (soft-reserved
  ERR-030-008, below). **Contrast #41/#31/#34/#32**, which each filed a tick-slot ERR — #43 is the
  first management spec whose #30 spec-text seams were all reserved ahead.
- **#40, #27, #44, #36, #38 — no change.** #40's (b') ordering is already recorded from its side;
  the rest are deferred consumers (FR-LW-031).

**At the #43 T-phase (deferred):** the #30 outer `SEASON_SAVE_FORMAT_VERSION` bump (T1); the
**#30 code-side coordinations folded into the soft-reserved ERR-030-008** — the (a') execution
hook (T2: the transform's membership output applied to `SeasonState.ClubIds` through #30's command
API before step (c), KD-4) and the deep multi-competition fixture-day driver (T3, KD-5); the #16
`DOMAIN_TAG_COMPETITION = 0x2C` promotion at the first knockout draw (ERR-016 pattern, T3).

## 9. Test focus

- **Behaviour-neutral identity (KD-8, the headline).** A season under the singleton collection
  advances **byte-identical** to bare #30 (fixtures/table/roll all delegate); no stream registered;
  the (a') point unvisited; the sub-blob is the version tag + instance-0 binding only.
- **Draw determinism (KD-2/KD-7, deep).** Two-run byte-identical brackets from one world seed; the
  same round drawn in any call order / after any save→restore yields the same pairings; two
  competitions drawing on the same day are mutually independent (permute one's draw calls, the
  other's bracket is unchanged); a shuffled-input entrant set produces the same pairings as the
  canonical input (the canonical-order lock).
- **Bracket coherence (KD-3, deep).** Round entrant counts halve; a winner is one of its pairing's
  entrants; a restored bracket equals the pre-save bracket field-identically and **no draw is
  re-rolled on load**; fail-loud on incoherent decoded bracket state.
- **Promotion/relegation (KD-4, deep).** A fixed pair of final standings produces the exact
  expected membership swap; the transform is a no-op in a one-division world; mid-roll
  save→restore continues the transform deterministically (FR-SN-029 extension); club ids unchanged
  (no re-key — a static assertion that #43 mutates membership only).
- **Merged calendar (KD-5, deep).** No club plays twice in a day; cup rounds land only on
  league-free days; the merged view is a pure function of the per-competition mappings.
- **Save round-trip (KD-6).** Sub-blob round-trips field-identical (minimal: binding only; deep:
  registry + brackets + membership); fail-loud on version/length/trailing/canonical-order/coherence
  violations; **no** `RngCursor` field (schema-shape assertion).
- **Integer posture.** No float in #43 (static assertion).

## 10. Risks

- **Draw non-determinism via unordered iteration** — the plan §9 headline. Closed by KD-7's pinned
  canonical order at every draw-feeding surface + the shuffled-input equivalence test.
- **A #30 rewrite hiding inside "generalization."** Dissolved by verification: `FixtureScheduler`/
  `LeagueTable` are already `(clubIds, seed)`-parameterized pure machinery, and #30 §7 pre-declared
  the composition. KD-1's by-reference instance 0 keeps one source of truth.
- **Fixture congestion (KD-5).** Real, but deep-tier; the merged-view design keeps #30's calendar
  untouched and names the deep driver coordination (soft ERR-030-008) instead of forcing a #30
  edit now. If the deep driver proves to need a #30 API extension, it lands as its own reviewed
  back-prop then.
- **The plan's serialized draw cursor** would have introduced cross-competition cursor races and
  new save state — revised to keyed draws (KD-2) with the rationale recorded.
- **Boundary-roll ordering.** (a') before (b') is already pinned from both sides (#30 FR-SN-031,
  #40 §1); the transform must stay inside the restartable-roll contract — locked by the mid-roll
  test.

## 11. Promotion pipeline

1. Author the 11-file section set at `IN REVIEW` (FR-CP-001..NNN).
2. Section-file PASS-1 adversarial review → AR-2/AR-3 to convergence.
3. R-01..R-05 sign-off → APPROVED; add `SPEC_INDEX.md` row 43.
4. **Back-props at approval: one** (§8) — the #16 A-04 placeholder sweep (ERR-043-001,
   `_RESERVED_0x2B_`/`_0x2C_`/`_0x2D_`); **no #30 change** (the (a') point + FR-SN-031 were
   pre-declared).
5. T-phase (post-APPROVED): T0 `CompetitionFormat`/`Competition`/`CompetitionSet` + the instance-0
   binding (behaviour-neutral) → T1 `COMPETITION_SAVE_FORMAT_VERSION` sub-blob + season-save
   composition (#30 outer bump) → T2 a second round-robin instance + division membership +
   `ApplyPromotionRelegation` at (a') → T3 knockout cups: `BracketState`, `DrawRound` keyed draws
   (promotes `0x2C`/94), the merged fixture-day view + the deep driver coordination (soft
   ERR-030-008), group+knockout format.

## Version History
| Version | Date | Change |
|---------|------|--------|
| v0.4 | July 24, 2026 | **PROMOTED** — 11-file section set (FR-CP-001..025), section-file AR to convergence (PASS-1 1M+1L → PASS-2 clean), R-01..R-05 signed, APPROVED; `SPEC_INDEX.md` row 43; ERR-043-001 filed (`spec-error-log.md` v1.39, `deterministic-sim/section-3.md` v1.0.14); ERR-030-008 soft-reserved. |
| v0.3 | July 24, 2026 | AR-2 (0H+0M+2L) → **CONVERGENCE** (L-only round). **L-1** — §2's minimal bullet still said "by reference … wraps" after the AR-1 M-1 binding fix; reconciled to the binding-row wording. **L-2** — §8's #30 bullet scoped to "NO back-prop **at approval**" with a pointer to the T-phase ERR-030-008 coordinations. Full hostile re-read otherwise clean at High/Medium — ready to promote to section files. |
| v0.2 | July 24, 2026 | AR-1 (0H+2M+1L). **M-1** — KD-1/§6: "instance 0 by reference" disambiguated to a **binding row** (an id/tag; #43 holds no #30 object — a stored live reference would bypass FR-SN-032's sole-writer discipline; instance-0 reads go through the root against #30's read surface). **M-2** — KD-4/§8: the (a') transform's **mechanical seam** named — its membership output must reach `SeasonState.ClubIds` (via #30's command API) before roll step (c) regenerates; the execution hook is a T-phase #30 coordination folded into the soft-reserved ERR-030-008 (the (a') *position* is pre-declared; the *hook* was silently unaddressed). **L** — `CompetitionId` allocation pinned (config-assigned at genesis, deterministic, instance 0 = 0, never reused); group-assignment draw purpose noted. |
| v0.1 | July 24, 2026 | Initial design supplement from spec-plan v0.1, grounded on the verified seam reconnaissance (#30 FR-SN-001/007/013a/029/031/032/033 + §3.5's pre-declared (a') point + §7's generalization row; #40 §1's (a')→(b') ordering; #16 §3.4 ending at `0x2A` — no `_RESERVED_0x2B_`..`_0x2D_` rows; the #41/#32 keyed-ordinal mechanism). Resolves plan KD-1..KD-5 (KD-1 degenerate-instance; KD-2 **revised** to keyed draws, dropping the plan's serialized cursor; KD-3 persisted brackets; KD-4 the pre-declared (a'); KD-5 a #43-owned merged view, #30 calendar untouched) + adds KD-6 (own sub-blob, instance 0 by reference), KD-7 (canonical entrant order), KD-8 (identity). One approval-time back-prop: the #16 A-04 placeholder sweep (ERR-043-001); **no #30 back-prop** (first management spec with all #30 seams pre-reserved). |
