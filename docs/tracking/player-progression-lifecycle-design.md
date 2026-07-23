# Player Progression & Lifecycle — Design Supplement

> **Created:** July 23, 2026
> **Status:** PROMOTED (July 23, 2026) — advanced to a full 11-file section-file set at
> `docs/specs/player-progression-lifecycle/` (Spec **#28**, authored → `IN REVIEW` → **APPROVED**;
> `SPEC_INDEX.md` row added; FR prefix **FR-PG**; FR-PG-001..024). Section-file PASS-1 (0H+2M) → AR-2
> (3M cross-fix) → AR-3 convergence; R-01..R-05 signed. The #16 §3.4 `_RESERVED_0x20_` → `0x20`/82
> promotion (ERR-028-001) filed at approval. This supplement is retained as the design-history record;
> the section files are authoritative. (Original status line follows for history.) DESIGN SUPPLEMENT (pre-promotion — no section files, no `SPEC_INDEX.md` row).
> Candidate spec **#28** (`_RESERVED_0x20_` / `SubsystemOrdinals` 82 already held for it in
> `deterministic-sim/section-3.md`). FR prefix (proposed): **FR-PG**.
> **Master-plan home:** §4.3 (aging/decline/retirement) / §5 (youth/regens) · **Tier:** Stage 2
> minimal → Stage 3 deep · **Wave:** 2.
> **Determinism (proposed, promoting the reserved rows):** `DOMAIN_TAG_PLAYER_PROGRESSION = 0x20`,
> `SubsystemOrdinals.PlayerProgression = 82`, off-pitch band.
> **Purpose:** Design player lifecycle on the **world tick** — aging, attribute decline, retirement,
> regen/newgen production, and attribute growth via a current/potential-ability (CA/PA) model over
> #27's canonical record — as **one code path with a config dial** (the deep curve reduces to the
> literal §4.3 step when the curve is off), driven by #30's day-advance loop at the seam #30 already
> reserved for it.

---

## 0. Scope and governance

This is a **Stage-1-forward pull**, exactly like #21/#22/#27/#30: the master development plan
(`docs/planning/master-development-plan.md` §4.3/§5) places aging + youth at Stage 2–3, but #30
(Season & Competition Loop) already reserves a **null-seam position** for #28 in its day-advance
tick order (KD-2) and its season-boundary roll ("advance ages via #28 — a null seam today", KD-6).
Nothing fills that seam: a played season currently ages no one, retires no one, and produces no
regens. This doc scopes the minimum lifecycle that makes a **multi-season career** true, and does
it as the **identity** the deep tier (#29 training, #42 youth academy) modulates rather than a
throwaway.

**In scope:** the per-day progression step #30 invokes (aging + decline + growth), the CA/PA model
over #27's canonical `PlayerAttributes`, retirement (evaluation on the world tick; roster removal
deferred to the season boundary), regen/newgen production reusing #27's `RosterGenerator`-class
draw machinery, the shared growth-input seam #29 (training) writes to, and the #28-owned lifecycle
persistence block composed into the season save.

**Explicitly out of scope** (each its own spec, per the roadmap): the youth-academy *structure*
(facilities → intake quality) is **#42**; training-driven growth *input* is **#29** (a shared
seam #28 defines, not a duplicate mutation — KD-2); valuations/price are **#31**; the day-advance
loop that *drives* #28 is **#30** (#28 exposes the step; #30 calls it — never the reverse). Where
#28 must reference a spec that does not exist yet (#29's producer), it does so through a **method
input defaulted to neutral** — the #21 routing-field precedent — never an invented interface
(FR-LW-031: no phantom interfaces).

---

## 1. What exists vs. what this adds

| Layer | Exists today | Gap this closes |
|---|---|---|
| **Roster record** | #27 `PlayerRecord` (`PlayerId` club-scoped, `Age`, `Position`, `PlayerAttributes` — 31 `int[1,20]` + `WeakFootRating [1,5]`); **no CA/PA fields, no growth state** | No player ever ages, grows, declines, or retires; `Age` is a generation-time constant |
| **Generation** | #27 `RosterGenerator.Generate(rng, streamIndex, clubId, count)` — deterministic, stateless, `FIELDS_PER_PLAYER` fixed reservation; produces a `Squad` | No **single-player** regen; nothing fills a retirement vacancy |
| **Day loop** | #30 `SeasonLoop.AdvanceToNextFixtureDay` runs `WorldStore.AdvanceDay()` per intervening day and reserves a **null-seam** slot for #28 in a fixed tick order (KD-2); the season-boundary roll (KD-6) reserves an "advance ages via #28" step | The seam is empty — no progression tick, no season-boundary aging |
| **Save** | #30 season save (`SeasonSaveCodec`: opaque length-prefixed sub-blobs, `SEASON_SAVE_FORMAT_VERSION` 1 → 2 for the season block) | The evolving roster (aged attributes / CA / PA / retirement state) is persisted **nowhere** — a save→load would lose every accumulated day |

**This adds** a `ProgressionEngine` (the per-day step #30 invokes + the season-boundary aging/
retirement/regen step), the CA/PA lifecycle value types, a single-player `RegenGenerator` reusing
the #27 draw pattern, the training-input seam #29 writes, and the `PROGRESSION_SAVE_FORMAT_VERSION`
lifecycle sub-blob the season save composes.

---

## 2. Staging (minimal-first → deep) — one code path, a config dial

Stage-2 minimal is the master plan's literal §4.3 rule expressed as a **deterministic per-day
projection**: age advances one year per `DAYS_PER_YEAR` world-days; a player over `DECLINE_AGE`
loses growth (≈ −1 attribute-point/year), under `GROWTH_AGE` gains (≈ +1/year); a player at
`RETIREMENT_AGE` (36) retires. Stage-3 deep replaces the flat step with per-attribute CA/PA
growth-decline curves keyed to age, position, and (via #29) training.

Both tiers are **one code path with a config dial** (KD-8): the daily growth function takes a
`curveEnabled` dial; **off**, it reproduces the literal §4.3 step exactly (the behaviour-neutral
identity, digest-locked); **on**, the deep curve modulates it. The minimal surface is the identity
the deep tier modulates, not a throwaway — the #21 "Stage-2 surface is the identity, deeper stage is
a dial" discipline and #30's own KD-8 world-advance-floor posture.

---

## 3. Dependencies

- **Upstream (needs):** #27 (`PlayerRecord` / `PlayerAttributes` / `RosterGenerator` / `Squad` +
  the roster world — which clubs and players exist), Deterministic Simulation #16 (the
  `progression.*` RNG sub-stream for regen draws), #30 (the day-advance loop + season-boundary roll
  that **invoke** #28's step, and the season save that **composes** #28's blob).
- **Downstream (consumers):** #29 training (writes the growth-input seam #28 defines), #42 youth
  academy (shares the regen/generation machinery), #31 valuations (age/PA feed price), #38 UI (a
  read-only lifecycle view model).

The **critical direction**: #30 depends on #28 (calls its step), #28 does **not** depend on #30 —
#28 exposes `AdvanceDay` / the season-boundary step and #30 invokes them at its reserved seams
(the #26 manager-decision-gate-invoked-by-the-engine precedent). #28 references #27's public
`player-database` surface only. The season-save composition lives at #30's root (the only layer
above both), so #28 never references the season assembly and #30 never reaches into #28 internals.

---

## 4. Persistent state & save impact

#28 owns a **per-player career-state block** keyed by `PlayerId` (KD-4). Because #27's on-disk
roster is Stage-1+ deferred and #30 KD-5 **rejects regeneration-on-load as fragile** (generator-
version drift — a #50 concern), the career-state roster must be *serialized*, not recomputed — so
#28's block serializes the **complete career-state `PlayerRecord` set it manages** (identity **and**
evolving attributes), plus the lifecycle overlay it alone owns (`CurrentAbility` derived summary,
`PotentialAbility` ceiling, the KD-1 `GrowthCursor`, `AgeAnchorDay`, retirement flag/day). It
serializes to a **new, independent `PROGRESSION_SAVE_FORMAT_VERSION`**, and the season-save root
composes it as **one more opaque sub-blob** in `SeasonSaveCodec` — the codec-never-parses-sub-blobs
pattern (`WorldStateSerializer` / `MatchSaveCodec` posture: version gate first, overflow-safe
length-prefixed counts, fail-loud on bad version / out-of-bounds prefix / trailing bytes).

Consequences:
- #27 keeps its role — the record **shape** (`PlayerRecord` / `PlayerAttributes`) + the
  **generation** of the *initial/reference* roster (`RosterGenerator`) — and its canonical struct
  stays **schema-untouched**: **no** new CA/PA fields, **no** #27 version ripple (the KD-4 decision:
  the lifecycle overlay is a #28-owned parallel block, not a #27 record change). #28 owns the
  **persisted, evolving instance** of that record across a career — it is the roster-persistence home
  #27 deferred (serialize, don't regenerate — the #30 KD-5 posture), because #28 is the single writer
  of the roster's over-time evolution. A future shipped-database / on-disk-roster pass (#47 / a #27
  Stage-1+ deliverable) supplies the *initial* roster; #28 remains the owner of the *career-state*
  roster from new-game onward. This is recorded as KD-4 + a §9 handoff note, not built here.
- The outer `SEASON_SAVE_FORMAT_VERSION` bumps (**2 → 3** if it lands after #30's season block; the
  section files sequence the two bumps if #28 and #30 land close together — the #30 §9 ordering
  note). The world blob (`WORLD_STORE_FORMAT_VERSION`) and match blob (`MATCH_SAVE_FORMAT_VERSION`)
  stay byte-untouched.

---

## 5. Determinism

Runs on the **world tick** (`WorldClock`, one day = one `worldTick`), never the 10 Hz/60 Hz match
loops. Two determinism facts:

1. **Aging + decline + growth of existing players is a pure deterministic projection — NO draw.**
   Age, decline, and the §4.3 growth step are a pure function of `(ageAnchorDay, currentDay,
   growthCursor, position, curveEnabled, trainingInput)`. The KD-1 fractional accumulation is
   **integer fixed-point** (never a float accumulator), so a mid-year save→restore is byte-exact and
   never double-counts on the day a step crosses.
2. **Only generation draws.** A dedicated `progression.*` RNG **sub-stream** covers regen/newgen
   attribute + potential generation (the sole Stage-2 draw consumer) and any Stage-3 stochastic
   jitter. New off-pitch allocations, **promoting the rows the #16 catalogue already reserves for
   #28** (the `_RESERVED_0x20_` placeholder + `SubsystemOrdinals` 82 in `deterministic-sim/section-3.md`):
   **`DOMAIN_TAG_PLAYER_PROGRESSION = 0x20`** and **`SubsystemOrdinals.PlayerProgression = 82`**,
   siteId `player-progression.regen`, `entityId = clubId` (the vacancy's club). This is the #30
   `0x22`/84 precedent — the section-file approval promotes the placeholder to a real row + registers
   the stream at the first draw site (T-phase), never earlier (registering a stream with zero draw
   sites is the phantom-surface class FR-LW-031 avoids — the `world.arcs` precedent). Draw sites are
   pinned **APPEND-only** so replay parity holds across fail-loud paths.

**Retirement is deterministic-hard** at `RETIREMENT_AGE` (the §4.3 literal), so Stage-2 retirement
draws nothing; the Stage-3 probabilistic-retirement dial adds a documented, appended draw site.
Stage-2 growth is a pure projection (no jitter), so the whole minimal tier is **draw-free except for
regen generation** — which is what makes the aging half trivially two-run deterministic.

---

## 6. Primary surfaces (proposed — pinned at section-file stage)

- **`ProgressionEngine`** (sealed) — owns the lifecycle block for the managed roster world; exposes
  `AdvanceDay(worldDay, in trainingInputs)` (the per-day step #30's day-advance loop invokes at its
  reserved seam) and `RunSeasonBoundary(...)` (the aging/retirement/regen step #30's boundary roll
  invokes); `Snapshot()` / `Restore()` for its own `PROGRESSION_SAVE_FORMAT_VERSION` sub-blob.
- **`PlayerLifecycle`** (value type, keyed by `PlayerId`) — the overlay #28 alone owns:
  `PotentialAbility` (the ceiling, a wide integer scale `[0, ABILITY_MAX]`), `CurrentAbility` (a
  *derived* summary of the `[1,20]` attributes — a recomputed/cached value, not a second source of
  truth, KD-1), the KD-1 `GrowthCursor` (integer fixed-point), `AgeAnchorDay`, and `RetirementFlag`
  + `RetirementDay`. The **evolving `PlayerAttributes` themselves live on the career-state
  `PlayerRecord`** the block also holds (KD-4) — the attribute values are the single source of truth
  the overlay summarizes, never duplicated onto the overlay.
- **`GrowthProjection`** (static, pure) — the daily `(lifecycle, age, position, curveEnabled,
  trainingInput) → deltas` function; the §4.3 identity when `curveEnabled` is off (KD-8). The single
  writer of attribute change — training is an **input**, never a parallel mutation (KD-2).
- **`RegenGenerator`** (static, pure) — `GenerateOne(rng, streamIndex, clubId, ...) → PlayerRecord`,
  the single-player analogue of #27's `RosterGenerator` (same Reserve/DrawReserved/Close fixed-budget
  pattern), referencing club/nation from #27's roster world (KD-3). Fills a retirement vacancy.
- **`RetirementResult` / `RegenResult`** — the season-boundary signals #28 emits; #30/#27 apply the
  roster mutation (remove the retiree, insert the regen) at the boundary. #28 never mutates #27's
  `Squad` directly (KD-5).
- **`TrainingInput`** (value type) — the per-player growth contribution #29 writes; **defaults to
  neutral** so the daily step runs the pure age curve until #29 lands (KD-2).
- **`LifecycleViewModel`** (read-only) — age / CA / PA / retirement view for #31/#38, the
  `match-viewer` observation-surface posture (no mutation path).

Existing seams referenced verbatim: #27 `PlayerRecord` / `PlayerAttributes` / `RosterGenerator` /
`Squad` / `PlayerDatabaseConstants`, #16 `DeterministicRngService` (`RegisterStream` / `Reserve` /
`DrawReserved` / `CloseReservation`) + `CanonicalSerializer`, #30 day-advance + season-boundary
seams.

---

## 7. Key design decisions the section files must resolve

- **KD-1 (byte-exact fractional-daily projection — no float, no double-count; ONE ability model).**
  Pin a single, non-overlapping representation (the three-way muddle of "CA-as-accumulator vs. a
  cursor vs. stored attributes" is a real divergence hazard — resolve it here):
  - The **`PlayerAttributes` `[1,20]` values are the mutated state** (the single source of truth for
    a player's ability), keyed by `PlayerId`.
  - A per-player **`GrowthCursor` (integer fixed-point points pool)** is the sub-attribute-point
    accumulator — the *only* accumulator. Each day it accrues `dailyPoints` (a pure integer function
    of age band + position + `trainingInput`); when it crosses `POINT_COST` it **spends** one
    attribute-point on the next attribute in a deterministic weighted order (respecting the PA
    ceiling) and decrements. Decline is the symmetric drain.
  - **`PotentialAbility` (PA)** is the ceiling (a wide-integer summary bound); **`CurrentAbility`
    (CA)** is a **derived weighted summary** of the `[1,20]` attributes — recomputed, never a
    second accumulator, so it can never diverge from the attribute state.
  - Because everything is integer (attributes + cursor + PA), a mid-year save carries the cursor +
    `AgeAnchorDay` and restore recomputes the identical continuation — byte-exact. Year rollover is a
    pure integer event (the day `currentDay − AgeAnchorDay` first crosses `DAYS_PER_YEAR`), so the
    year-end step fires exactly once and is never double-counted across a save boundary. The section
    files pin `POINT_COST`, the weighted spend order, and the fixed-point representation.
- **KD-2 (the #29 shared growth-input seam — one code path, training is an input).** #28 owns the
  **only** attribute-mutation path (`GrowthProjection`). #29 (training) supplies a per-player
  `TrainingInput` that #28's daily step **reads**, so training growth is an **input to** #28's curve,
  never a parallel mutation of the same attributes (which would double-count). The seam is a **method
  parameter defaulted to neutral** (`TrainingInput.Neutral`), not a pre-built interface against the
  absent #29 producer — the FR-LW-031 phantom-interface rule + the #21 routing-field-seeded-to-
  identity precedent. Until #29 lands, the daily step runs the pure age curve (behaviour-neutral).
  This is the entry's headline architectural risk (§9) and KD-2 is its resolution.
- **KD-3 (regens reference #27's roster world; produced day-deterministically; #28 does not own
  roster identity).** A retirement creates a club vacancy; a regen fills it. `RegenGenerator`
  produces one `PlayerRecord` for a `clubId`, deterministic from the `progression.*` stream (siteId
  `player-progression.regen`, `entityId = clubId`), reusing #27's exact Reserve/DrawReserved/Close
  fixed-budget draw pattern so a regen is byte-reproducible. Club/nation come from #27's roster world
  (read-only). **A regen MUST get a FRESH `PlayerId`** (a monotonic allocation beyond the initial
  `clubId * CLUB_SQUAD_SIZE + localIndex` range), never the retiree's — #28's career-state block is
  keyed by `PlayerId`, so reusing a retired id would leak stale lifecycle state onto a new person;
  the retiree's entry is removed as its regen's fresh entry is inserted. #28 does **not** own the
  `PlayerId` *allocation policy* (#27's `KD-3` club-scoped-id contract) — the section files pin
  whether the monotonic allocator lives in #27 or #28 (default: a #28 `NextPlayerId` cursor persisted
  in the career-state block, since #28 owns that block). #28 hands the new record to the roster owner
  (#30/#27) via a `RegenResult` at the season boundary. Whether `RegenGenerator` lives in #28 or as a
  #27 addition is a section-file call (default: #28-owned, reusing #27's public constants/name
  catalogue, to avoid a #27 change).
- **KD-4 (career-state ownership — a #28-owned block, not a #27 record change).** #28 owns a block
  keyed by `PlayerId` (§4) holding the **complete career-state `PlayerRecord` set it manages**
  (identity + the evolving `PlayerAttributes`) **plus** the lifecycle overlay (CA derived summary, PA
  ceiling, `GrowthCursor`, `AgeAnchorDay`, retirement flag/day), serialized under
  `PROGRESSION_SAVE_FORMAT_VERSION` and composed by the season-save root as an opaque sub-blob.
  Rationale: #27's on-disk roster is Stage-1+ deferred and #30 KD-5 rejects regeneration-on-load as
  fragile, so the career-state roster must be *serialized* — and #28 is its single over-time writer,
  so it is the natural persistence owner. #27's canonical struct gains **no** CA/PA fields (avoids a
  #27 record schema ripple; the KD-4 in the high-level plan) — #27 keeps the record **shape** + the
  **initial/reference-roster generation**, #28 owns the **persisted career-state instance**. Deciding
  this late would force a #27 record change — pinning it here keeps #27 frozen.
- **KD-5 (retirement → roster-removal contract; season-boundary, never mid-fixture).** Retirement is
  **evaluated** on the world tick (hard at `RETIREMENT_AGE`), but a retiring player is only
  **flagged** (`RetirementFlag` + `RetirementDay` in the lifecycle block) — roster removal + the
  regen replacement happen at the **season boundary** (#30's KD-6 roll), never mid-fixture. A
  flagged-retiring player stays selectable until the season ends, so an in-progress season's
  fixtures/selection are never disrupted. #28 emits `RetirementResult` / `RegenResult` at the
  boundary; #30/#27 apply the `Squad` mutation. This reconciles lifecycle with the live season
  exactly as #30 KD-5/KD-6 keep fixture/table mutation at pinned discrete steps.
- **KD-6 (the season-boundary step order #30 invokes).** Aging/growth/decline are **continuous**
  (banked daily in `AdvanceDay`, per KD-1 — each player's year-end step fires on its own
  `AgeAnchorDay` rollover, not in a batch), and retirement is **flagged** daily when age crosses
  `RETIREMENT_AGE` (KD-5). So `RunSeasonBoundary` does **not** re-bank growth; its job is to **apply
  the deferred roster mutations**: collect the players flagged-retiring this season → emit
  `RetirementResult` → produce a deterministic regen per vacancy (`RegenResult`, fresh `PlayerId`,
  KD-3). #30/#27 apply the `Squad` removal+insert (KD-5). It is a single restartable,
  round-trip-deterministic step (a save mid-boundary restores to the same point) — the analogue of
  #30's own restartable boundary roll, slotting into #30 KD-6's reserved "advance ages via #28" step.
- **KD-7 (single-writer + observation-surface discipline).** `ProgressionEngine` is the **sole
  writer** of lifecycle state and the sole mutator of the managed roster's attributes; the
  `LifecycleViewModel` is a read-only value-copy surface for #31/#38 (the `match-viewer` /
  `MatchEngine.BallView` observer-neutral posture — reading never mutates, the digest/round-trip is
  unaffected by observation). #30/tests drive lifecycle **only** through the public step API
  (`AdvanceDay`, `RunSeasonBoundary`), never by poking fields — the `SetTeamTactic` command-seam
  precedent.
- **KD-8 (behaviour-neutral minimal identity — the deep curve reduces to the §4.3 step).** With
  `curveEnabled` off, `GrowthProjection` reproduces the literal §4.3 ±1/year step exactly, digest-
  locked; a two-run multi-season aging projection from one seed is byte-identical, and the deep-curve-
  off run equals the literal-step run byte-for-byte. The #21/#27/#30 default-neutrality discipline;
  a §8 test.

---

## 8. Test focus

- **KD-1 byte-exact restore:** save→restore round-trip determinism across a **mid-year** boundary —
  a save on any day inside an age-year restores to the same cursor/anchor and the year-rollover step
  fires exactly once, never double-counted across the save boundary.
- **Two-run multi-season projection:** the same seed drives a multi-season aging projection to a
  byte-identical roster state (the end-to-end determinism lock; the aging half is draw-free, so this
  is a pure-projection lock).
- **KD-8 behaviour-neutral identity:** `curveEnabled` off reproduces the literal §4.3 step exactly
  (the deep-curve-off run == the literal-step run, byte-for-byte).
- **Regen determinism (KD-3):** same seed + same club → same newgen `PlayerRecord` (the
  `RosterGeneratorTests` posture — exact per-player draw budget, club-scoped `PlayerId`, bounds).
- **Retirement contract (KD-5):** a player crossing `RETIREMENT_AGE` mid-season stays selectable and
  is flagged; the `Squad` mutation lands only at the season boundary; the vacancy is filled by a
  deterministic regen.
- **KD-2 seam neutrality:** the daily step with `TrainingInput.Neutral` == the daily step with no
  training input, byte-for-byte (the #29 seam adds nothing until #29 writes a non-neutral value).
- **Fail-loud gates** on `PROGRESSION_SAVE_FORMAT_VERSION` (bad version / out-of-bounds length prefix
  / trailing bytes), the `SeasonSaveCodec` gate posture; plus the composed case (world + season +
  progression + optional match through the season save).

A closed-loop `#19 ScenarioRunner` scenario is not required at the design stage; a
`multi-season-aging` capstone (build a roster, advance N seasons, assert the aged state + a
determinism digest) is the natural §8 addition once the engine is wired — the match-engine capstone
precedent.

---

## 9. Open questions / risks

- **The #28/#29 shared-seam boundary is the main architectural risk (resolved by KD-2).** Building
  training growth as a **separate** mutation of the same attributes would double-count and break the
  "one code path" invariant. KD-2 pins training as an **input** to #28's single growth function
  (a neutral-defaulted method parameter, not a phantom interface). The risk is a future #29 author
  adding a parallel mutation anyway — mitigated by making `GrowthProjection` the sole documented
  writer and the neutral-seam a §8 lock.
- **CA/PA field ownership straddles #27 and #28 (resolved by KD-4).** Deciding it late forces a #27
  record change (a `SNAPSHOT`/roster schema ripple). KD-4 keeps the CA/PA overlay in a #28-owned
  block keyed by `PlayerId`, and #28 serializes the **complete career-state `PlayerRecord`** (the
  serialize-don't-regenerate posture #30 KD-5 fixed) — so #27's canonical struct stays frozen (shape
  + initial-roster generation), and #28 owns the persisted career-state instance. The handoff to a
  future #27/#47 on-disk *reference* roster (which supplies the *initial* roster, not the career
  state) is a section-file note; no #27 code lands here.
- **Regen volume × world size is a save-size concern the master plan flags.** The
  retirement/generation balance must not grow the lifecycle blob unboundedly — the block is bounded
  by the roster size (a vacancy is filled 1:1, not appended), so the blob size is stable across
  seasons. The section files pin the invariant (`|lifecycle block| = |managed roster|`, no growth per
  season).
- **Format-version sequencing with #30.** #28's `SEASON_SAVE_FORMAT_VERSION` bump follows #30's
  1 → 2; whoever lands second rebases on the other's frame layout (the #30 §9 ordering note). The
  progression sub-blob carries its own `PROGRESSION_SAVE_FORMAT_VERSION`, so the two are
  non-colliding (each inner block is opaque + version-gated); only the outer frame bump sequences.

---

## 10. Promotion pipeline (proposed)

Same path #21–#30 followed:

1. Self-adversarial review of **this supplement** to convergence (AR-1 → AR-n; an L-only or clean
   round closes it — the #21–#30 convention). Findings fixed in place.
2. Promote to a full 11-file section set at `docs/specs/player-progression-lifecycle/` (`IN REVIEW`),
   FR prefix **FR-PG**, `SPEC_INDEX.md` registry row added.
3. Section-file PASS-1 adversarial review → AR-2 convergence.
4. File the cross-spec back-props at approval: the #16 §3.4 promotion of `_RESERVED_0x20_` →
   `DOMAIN_TAG_PLAYER_PROGRESSION = 0x20` / `SubsystemOrdinals.PlayerProgression = 82` (+ ERR-028-001),
   and the #30 season-save-composition note (the reserved #28 seam is now filled).
5. Lead-developer R-01..R-05 sign-off → `APPROVED`.
6. Implement per the §6 T-phase plan (T0 lifecycle value types + `GrowthProjection` §4.3 identity +
   `RegenGenerator`, behaviour-neutral; T1 the `PROGRESSION_SAVE_FORMAT_VERSION` block + season-save
   composition; T2 the `AdvanceDay` / `RunSeasonBoundary` steps wired at #30's reserved seams —
   **which requires #30 implemented first** (the Wave-1 → Wave-2 ordering guarantees it; #28 T2 must
   not land before #30's seam code exists, or it would wire against a phantom); T3 the deep CA/PA
   curve dial + the #29 training-input consumption).

---

#### Version History
| Version | Date | Notes |
|---|---|---|
| 0.1 | 2026-07-23 | Initial draft from the `spec-plans/spec-28-…` high-level plan + the #30 supplement's reserved seams, grounded in the actual `PlayerRecord`/`PlayerAttributes`/`RosterGenerator`/`WorldStore`/`SeasonSaveCodec` APIs and the `_RESERVED_0x20_`/82 rows #16 already holds. KD-1..KD-8 pinned; §9 risks map to the plan's own open questions. Pre-promotion — no section files, no `SPEC_INDEX.md` row. |
| 0.3 | 2026-07-23 | **AR-2: 0H+3M, all fixed (cross-fix regressions from the v0.2 edits — exactly what the full-re-review rule catches) → AR-3 clean, CONVERGENCE.** M-1: §9's second risk still described the pre-M-2 "identity (#27) vs. evolving fields (#28)" split — realigned to "#28 serializes the complete career-state record; #27 keeps shape + initial generation." M-2: §6's `PlayerLifecycle` still listed "the current aged `PlayerAttributes`" as an overlay field + `CurrentAbility` as stored state — realigned to the resolved KD-1/KD-4 (attributes live on the career-state `PlayerRecord`; CA is a derived summary). M-3: KD-6 said `RunSeasonBoundary` "banks each player's year-end step", contradicting KD-1/§5 (aging is continuous/daily in `AdvanceDay`, per-player birthday) — reframed to "boundary applies the deferred retirement roster-mutations + regens; growth is banked daily, not re-banked here." AR-3 (grep + §2/§5/§8/KD-5 cross-ref sweep): no new High/Medium — the ability model, ownership split, and aging-location are consistent end to end. An L-only/clean round closes the cycle (the #21–#30 convention). |
| 0.2 | 2026-07-23 | **AR-1: 0H+2M+2L, all fixed** (findings verified against real source, not narrated). **M-1 (internal contradiction — muddled ability model):** KD-1 said "CA is the accumulator" while KD-4 also listed a separate `GrowthCursor` + stored attributes — three overlapping representations with no pinned source of truth. Resolved to ONE model: `[1,20]` attributes are the mutated state (single source of truth), `GrowthCursor` is the only (integer fixed-point) accumulator, CA is a *derived* summary (never a second accumulator), PA the ceiling. **M-2 (boundary/scope gap):** §4/KD-4 leaned on "a deferred #27 on-disk roster" for immutable-identity persistence, implicitly requiring regeneration-on-load — the fragility #30 KD-5 rejected. Resolved: #28's block serializes the **complete career-state `PlayerRecord` set** (identity + evolving), the serialize-don't-regenerate posture; #27 keeps the record shape + initial-roster generation. **L-1:** a regen MUST get a FRESH `PlayerId` (no reuse of a retiree's — the block keys on `PlayerId`, so reuse would leak stale lifecycle state); a persisted `NextPlayerId` cursor added to KD-3. **L-2:** #28 T2 wires at #30's reserved seams → requires #30 implemented first (Wave-1 → Wave-2 ordering), noted in §10 to avoid wiring against a phantom seam. |
