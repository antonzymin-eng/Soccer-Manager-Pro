# Season & Competition Loop — Design Supplement

> **Created:** July 22, 2026
> **Status:** DESIGN SUPPLEMENT (pre-promotion — no section files, no `SPEC_INDEX.md` row).
> Candidate spec **#30** (proposed in `management-layer-spec-roadmap.md` / `spec-plans/spec-30-…`,
> not yet reserved). FR prefix (proposed): **FR-SN**. This is the same pre-section-file stage
> `tactical-instruction-layer-design.md` / `squad-player-data-design.md` occupied before #21/#27.
> **Master-plan home:** §4.1 (fixtures/season) / §4.5 (career continuity) · **Tier:** Stage 2 ·
> **Wave:** 1 (Spine).
> **Purpose:** Design the playable career/season spine — deterministic round-robin fixtures, a live
> league table, a calendar cursor with match-day flow, board objectives / job-security, and
> multi-season continuity — that **owns** the `SeasonSaveManager` composition root and drives it day
> to day, ticking the world (#22) and (later) #28/#29/#33 forward between fixtures.

---

## 0. Scope and governance

This is a **Stage-1-forward pull**, exactly like #21/#22/#27: the master development plan
(`docs/planning/master-development-plan.md` §4.1/§4.5) places the full career/season loop at
Stage 2, but the engine has a concrete present-day gap it creates — there is **no season to
play**. `MatchEngine` plays one fixture; `WorldStore` advances one world-day; `SeasonSaveManager`
persists "a world + an optional in-progress match" — but nothing schedules the fixtures, keeps a
table, moves a calendar cursor, or connects one match to the next. This doc scopes the minimum
loop that makes a **playable single-league season** true, and does it as the identity that #43
(Competition Structure) later generalizes rather than a throwaway.

**In scope:** round-robin fixture generation (deterministic from the world seed), a live league
table with tie-breaks, a calendar cursor + match-day flow, board objectives / job-security state,
multi-season continuity (the season-boundary roll), the `SEASON_SAVE_FORMAT_VERSION` bump that
persists all of it, and the day-advance loop that becomes the integration choke point for every
later world-tick spec.

**Explicitly out of scope** (each its own spec, per the roadmap): cups / continental / promotion-
relegation (**#43**), finances (**#40**), the human-systems model this loop advances (**#33**),
transfers (**#31**), injuries (**#41**), progression/aging (**#28**), training (**#29**),
discipline/suspensions (**#44**), and the UI that renders the loop (**#38**). The Stage-2 surface
is **single-division, single-competition** (master plan §4.1). Where this loop must *tick* a spec
that does not exist yet (#28/#29/#33), it does so through a **documented null seam** — the
`WorldLoop` phase-1/2/5 precedent (FR-LW-031: no phantom interfaces) — never an invented interface.

---

## 1. What exists vs. what this adds

| Layer | Exists today | Gap this closes |
|---|---|---|
| **Match** | `MatchEngine` plays one fixture; `MatchSaveManager` saves/restores it | Nothing decides *which* fixtures, *when*, or *what the result means* |
| **World** | `WorldStore.AdvanceDay()` runs one calendar day (`WorldClock`: one `worldTick` = one calendar day, living-world KD-4); `WorldLoop` phase-1 (match-outcome ingest) is a **dormant null seam** | No driver calls `AdvanceDay` in a schedule; phase-1 has no producer (this loop becomes it — but ingest stays deferred to #33, §7 KD-3) |
| **Save** | `SeasonSaveManager.Save(world, matchOrNull, path)` bundles a `WorldStore` composite + an optional in-progress `MatchEngine` into one file (`SeasonSaveCodec`: a `matchPresent` flag + two length-prefixed **opaque** sub-blobs, `SEASON_SAVE_FORMAT_VERSION = 1`) | The file carries **no season state** — no table, no fixtures, no calendar cursor, no board state |
| **Composition** | `TacticalDirector.SeasonSave` (`src/season-save/`) already sits **above both** `match-engine` and `living-world` — the only assembly that may see both (FR-LW-003) | Nothing *owns* it as a stateful career driver; it is a static save/load pair |

**This adds** a season-loop composition-root type (owning `SeasonSaveManager`), the season-state
value types (table / fixture list / calendar cursor / board state), a deterministic fixture
generator, a day-advance / match-day command API the UI (#38) and tests drive, a read-only
table+fixture view model for #37/#38, and the season-state sub-blob that extends
`SeasonSaveManager` — landing as a **third opaque sub-blob** so the codec-never-parses pattern
holds and the world/match blobs stay byte-untouched at their existing versions.

---

## 2. Staging (minimal-first → deep) — the identity #43 generalizes

Stage-2 minimal = **one single-division round-robin league** (double round-robin: home + away,
`N` clubs → `N·(N−1)` fixtures) with a **linear calendar** (one fixture round per match-day slot)
and **literal board objectives** ("finish ≥ position P"). This is authored deliberately as the
**identity** the later specs modulate, one code path with the competition set as a config dial:

- **#43 (Competition Structure)** generalizes the same fixture/table machinery to multiple
  concurrent competitions + knockout draws; promotion/relegation is a **season-boundary transform**
  over this loop's league state, not a rewrite (the #21 "Stage-2 surface is the identity, deeper
  stage is a dial" discipline).
- **#40 (Finances)** attaches a budget-from-league-finish block to the season-boundary roll.
- **#44 (Discipline)** derives a suspension-availability view read-only over the match card ledger
  (like #37) that this loop's squad-selection consumes.

So #30's contract is the **shapes** (fixture schedule, table, calendar cursor, board state, the
day-advance tick order, the serialization boundary), not the tuned magnitudes or the single-league
restriction — exactly as #21 §9.2's `[GT]` values are illustrative and #27's Stage-2 surface is
the identity later phases deepen.

---

## 3. Dependencies

- **Upstream (needs):** #27 (the roster world — which clubs exist, and the `Squad` each fields via
  `MatchEngine.ConfigureSquads` / `ISquadProvider`), `MatchEngine` (+ `MatchSaveManager` for the
  in-progress-match blob), `SeasonSaveManager` / `SeasonSaveCodec` / `SeasonSaveConstants` (the
  composition root it owns and extends), `WorldStore` / `WorldClock` / `WorldLoop` (the day-advance
  substrate), Deterministic Simulation #16 (a fixture/season RNG sub-stream), Event System #17 (the
  match card/goal ledger it reads to build results).
- **Downstream (consumers):** essentially every later management spec — #28/#29/#31/#32/#33/#35/
  #36/#43/#44/#45/#46 attach to the day-advance loop and season events; #37 reads the table/result
  view models; #38 renders the loop.

The **critical sequencing constraint** (roadmap §4 / #22 `FR-LW-032`): this loop *becomes* the
**phase-1 producer** #22's dormant `WorldLoop` seam was written for — it emits structured
match-outcome events. But **#22 phase-1 ingest is not activated by #30 alone.** `FR-LW-032`
(`living-world/section-2.md`, a MUST) gates Stage-1 activation on **both** structured match-outcome
events (#30) **and** the vol-2/vol-3 human-systems implementation (#33), and roadmap line 311 is
explicit: "Do not activate #22's dormant seams before #30 **and** #33 land." So #30's job here is to
be the **producer** (define + emit the event); wiring it into #22's ingest is **deferred to when #33
lands** and is out of #30's spine scope (**KD-3**).

---

## 4. Persistent state & save impact

Extends `SeasonSaveManager` from "world + optional match" to "world + **season state** + optional
match". The season-state block lands as a **third opaque, independently version-gated sub-blob** in
`SeasonSaveCodec` — the codec-never-parses-sub-blobs pattern already in place (it frames the world
and match blobs as opaque length-prefixed blocks and never reads their internals). So:

- The **world blob** (`WorldStore.Snapshot()`, `WORLD_STORE_FORMAT_VERSION`) and the **match blob**
  (`MatchSaveManager.Encode`, `MATCH_SAVE_FORMAT_VERSION`) stay **byte-untouched** — no
  `SNAPSHOT_SCHEMA_VERSION` / `WORLD_STORE_FORMAT_VERSION` / `MATCH_SAVE_FORMAT_VERSION` change.
- The season file adds a **season-state sub-blob** with its own **new**
  `SEASON_STATE_FORMAT_VERSION` (gating the table/fixtures/calendar/board layout), and the outer
  `SEASON_SAVE_FORMAT_VERSION` bumps **1 → 2** (a frame-layout change: a third length-prefixed
  block joins the frame). This is the codec's own rule — "bump only on a season-frame layout
  change" — and it is exactly one such change.

The block is a pure `CanonicalSerializer` byte payload (the `WorldStateSerializer` / `MatchSaveCodec`
posture: version gate first, length-prefixed counts via a `ReadCount`-style overflow-safe bound,
fail-loud on bad version / out-of-bounds prefix / trailing bytes). **KD-4** pins whether the
calendar cursor lives here or in `WorldStore`.

---

## 5. Determinism

Runs on the **world tick** (`WorldClock`, one day = one `worldTick`), never the 10 Hz/60 Hz match
loops. Two determinism facts:

1. **Fixture generation is a pure function of a seed** (a deterministic round-robin — the circle
   method), so the single-league case needs **no RNG draw at all**; the schedule is reproducible
   from `(club set, seed)`. **KD-5** decides regenerate-from-seed vs. serialize-the-fixture-list.
2. A dedicated season RNG **sub-stream** covers any genuinely stochastic season event (an objective
   set from a distribution, a tie-break that a rule leaves to a draw, a future cup draw in #43).
   New off-pitch allocations, back-propped into Deterministic Simulation #16 §3.4 (the
   `ERR-022-001`/`ERR-027-001` precedent — living-world `0x1E`/80, player-database `0x1F`/81):
   **`DOMAIN_TAG_SEASON_LOOP = 0x22`** and **`SubsystemOrdinals.SeasonLoop = 84`** (the values the
   roadmap §6 reserves for #30). Note `0x20`/82 and `0x21`/83 are roadmap-reserved for #28/#29, which
   land **after** #30 (Wave 2) — this pass files **only** #30's `0x22`/84 row and deliberately leaves
   `0x20`/`0x21` as catalogue gaps rather than pre-filing rows for specs that do not exist (unlike
   #27, which filed a *real* code-only `0x1E`; there is nothing real to file for #28/#29 yet). The
   accepted `ERR-016-003` orphaned-tag precedent makes the gaps legitimate; the section-file cross-
   cite records why they exist.

The day-advance loop is where #22's phase-1 **producer** side lights up — a structured match-outcome
event emitted **once per played fixture**, deterministic in the result it summarizes. Whether that
event is *ingested* by #22's dormant phase-1 is #33's concern (§7 KD-3), not #30's.

---

## 6. Primary surfaces (proposed — pinned at section-file stage)

- **`SeasonLoop`** (sealed composition root) — owns `SeasonSaveManager` usage; holds the season
  state (table, fixtures, calendar cursor, board state) + a reference to the `WorldStore` and the
  active-or-null `MatchEngine`; exposes the day-advance / match-day command API; `Snapshot()` /
  `Restore()` for its own season-state sub-blob (the world/match blobs stay `WorldStore` /
  `MatchSaveManager` owned).
- **`FixtureScheduler`** (static, pure) — `Generate(clubIds, seed) → Fixture[]`, deterministic
  round-robin (circle method), double round-robin (home+away).
- **`LeagueTable`** (value type + updater) — per-club P/W/D/L/GF/GA/GD/Pts; `ApplyResult`; ordered
  view with pinned tie-breaks (Pts → GD → GF → club-id, a documented Stage-0 rule #43 can extend).
- **`SeasonCalendar`** (value type) — the cursor over match-day slots; `AdvanceToNextFixtureDay`
  drives `WorldStore.AdvanceDay()` for the intervening days.
- **`BoardObjective` / `BoardState`** — the literal "finish ≥ P" objective + job-security state;
  evaluated at the season boundary (and, minimally, as a running "on track?" read).
- **`MatchResult` / a structured match-outcome event** — the #22 phase-1 **producer** payload #30
  emits per fixture (KD-3); the #22 ingest entry point that consumes it is deferred to #33's landing,
  not added here.
- **`SeasonViewModel`** (read-only) — table + fixture list + calendar position for #37/#38, the
  `match-viewer` observation-surface posture (no mutation path).

Existing seams referenced verbatim: `SeasonSaveManager` / `SeasonSaveCodec` / `SeasonSaveConstants`,
`WorldStore.AdvanceDay` / `.Snapshot` / `.Restore`, `MatchEngine.ConfigureSquads` /
`RestoreFromSnapshot`, `MatchSaveManager.Encode` / `Restore`, `ISquadProvider`.

---

## 7. Key design decisions the section files must resolve

- **KD-1 (season-state sub-blob layout + version).** A third opaque sub-blob in `SeasonSaveCodec`
  with its own `SEASON_STATE_FORMAT_VERSION`; the outer `SEASON_SAVE_FORMAT_VERSION` bumps 1 → 2.
  The season blob is **always present** (unlike the optional match blob), so the frame becomes
  `version → matchPresent flag → world block → season block → (match block iff matchPresent)`. Byte
  order *inside* the season block: version → club roster (for the table's per-club rows) → fixture
  list (or seed, per KD-5) → calendar cursor → table rows → board state. Overflow-safe length
  prefixes, fail-loud on version/prefix/trailing (the `MatchSaveCodec`/`WorldStateSerializer`
  posture). **This changes the codec + manager signatures:** `SeasonSaveCodec.Encode` /
  `Decode` gain the season block, and `SeasonSaveManager.Save`/`Load` gain a season parameter
  (`Save(WorldStore world, SeasonState season, MatchEngine matchOrNull, string path)` — today it is
  `Save(world, matchOrNull, path)`). **This resolves the §9 format-version-ordering risk** — #30 owns
  `SEASON_SAVE_FORMAT_VERSION`; a #27 on-disk-roster persistence pass (Stage-1+, not this loop) would
  add a *fourth* inner blob, not collide with the season block.
- **KD-2 (day-advance tick order — the integration choke point).** One restartable, round-trip-
  deterministic `AdvanceToNextFixtureDay` step: for each intervening calendar day, run the
  world-tick spec ticks in a **fixed, documented order** (Wave-2+ specs #28/#29/#33 slot in as null
  seams today — `WorldStore.AdvanceDay()` is the only live tick), then, on a fixture day, play/
  ingest the fixture. The tick order + the serialization boundary (save can land mid-sequence and
  restore == uninterrupted advance) is **load-bearing for all of Wave 2+**, so it is pinned here
  even though the only live tick today is the world's. Mid-day restore determinism is a §8 test.
- **KD-3 (#30 is the phase-1 *producer*; ingest activation is deferred, not #30's job).** #30
  **defines and emits** the structured match-outcome event, becoming the phase-1 producer #22's
  `WorldLoop` seam was written for. It does **not** wire that event into #22's ingest: `FR-LW-032`
  (`living-world/section-2.md`, MUST) gates Stage-1 phase-1 activation on **both** structured
  match-outcome events (#30) **and** the vol-2/vol-3 human-systems implementation (#33), and
  `WorldLoop.cs` phase-1 has **no interface today** (FR-LW-031 — no phantom interface until its
  producer *and consumer semantics* exist). A match outcome has no meaning on the manager↔player
  memory edges until #33's human-systems model defines it, so building the ingest now would wire a
  consumer ahead of its producer — the exact class FR-LW-031/`FR-LW-027` police. **Therefore:** #30
  emits the event (deterministic, once per fixture) and records it in the season state; the ingest
  **entry point on #22 does not exist yet and is not added here** — it lands as a #22 wiring change
  when #33 lands (roadmap §4 / `FR-LW-032`). The event's payload shape is **co-defined by #30 + #22 +
  #33 at that activation**, cross-checked against `FR-LW-027`/`FR-LW-032`/KD-9/KD-10; #30 owns only
  the producer-side payload it emits. When #30 references anything in the world layer it is
  `WorldStore`'s public surface only, **never `living-world` internals** (FR-LW-003 — the season root
  is the only assembly above both, so it composes them without either referencing the other).
- **KD-4 (calendar cursor home — season blob vs. `WorldStore`).** `WorldStore` already owns
  `WorldClock` (the calendar *day*). The **fixture calendar cursor** (which match-day slot / which
  fixture round is next) is **season-scoped state, not world-time**, so it lives in the **season
  sub-blob** — the world clock advances continuously; the fixture cursor is a discrete pointer into
  the schedule. Keeping them separate avoids coupling the season schedule to the world-day counter
  (a mid-season save restores both, and they are independently validated). The section files pin the
  invariant that the cursor's "next fixture day" is always ≥ the current `WorldClock` day.
- **KD-5 (fixture-generation determinism across regeneration).** **Serialize the fixture list** in
  the season sub-blob rather than regenerate-from-seed on load. Rationale: regeneration is only safe
  if the club set + the generator are byte-stable across the exact game build that saved and the one
  that loads (a #50 Save-Migration concern) — serializing the concrete schedule makes a loaded
  season independent of generator-version drift, and the schedule is small (`N·(N−1)` fixtures). The
  generator stays a pure `Generate(clubIds, seed) → Fixture[]` used at **season creation**, and its
  determinism is a §8 two-run test; the loaded season trusts the serialized list. (This mirrors the
  #19 `ScenarioIndex` "author the concrete value, don't recompute it on load" and the #27
  "serialize the roster reference, don't trust regeneration" postures.)
- **KD-6 (multi-season continuity — one restartable step).** The season-boundary roll is a single
  round-trip-deterministic transform over league state: finalize the table → evaluate board
  objectives / job-security → (Stage-2 minimal) reset fixtures for the new season via
  `FixtureScheduler.Generate` with the next season's seed → advance ages via #28 (a null seam
  today) → reset the table. It is restartable (a save taken mid-roll restores to the same point) and
  is **where #43's promotion/relegation transform later slots in** (as a step between "finalize
  table" and "reset fixtures") without changing the surrounding steps.
- **KD-7 (single-writer + observation-surface discipline).** The `SeasonLoop` is the **sole writer**
  of season state; the `SeasonViewModel` is a read-only value-copy surface for #37/#38 (the
  `match-viewer` / `MatchEngine.BallView` observer-neutral posture — reading the season never mutates
  it, and the digest/round-trip is unaffected by observation). UI/tests mutate season state **only**
  through the public command API (`AdvanceToNextFixtureDay`, `PlayNextFixture`, the season-boundary
  roll), never by poking fields — the `SetTeamTactic` command-seam precedent.
- **KD-8 (behaviour-neutral world-advance floor).** An empty / no-fixture day must advance the world
  **identically** to a bare `WorldStore.AdvanceDay()` — the season loop adds scheduling and result
  ingest, it does not change how a plain day ticks. This is the #21/#27 default-neutrality discipline
  and is a §8 test (a season day with no fixture == the pre-#30 world advance, byte-identical).

---

## 8. Test focus

- **Fixture determinism:** two-run `Generate(clubIds, seed)` byte-identical; round-robin
  completeness (every ordered pair exactly once for a double round-robin); no club plays twice in one
  round.
- **Table correctness:** `ApplyResult` W/D/L/GF/GA/GD/Pts arithmetic; tie-break ordering (the pinned
  Pts→GD→GF→club-id rule) exercised at an exact tie.
- **Save→restore round-trip determinism** for the **full season blob** (table + fixtures + calendar
  + board), byte-identical, through one file; plus the composed case (world + season + optional
  match all through `SeasonSaveManager`), reusing the existing `SeasonSaveManagerTests` posture.
- **Behaviour-neutral world floor (KD-8):** a no-fixture day advances the world byte-identically to
  the pre-#30 `WorldStore.AdvanceDay`.
- **Mid-day / mid-sequence restore (KD-2):** save@day-N mid-advance → restore → advance to N+K ==
  an uninterrupted advance (the match-engine "save@N → restore → tick == uninterrupted run"
  correctness contract, lifted to the world/season loop).
- **Two-run simulated season:** the same seed drives a full simulated season to a byte-identical
  final table (the end-to-end determinism lock).
- **Fail-loud gates** on the new `SEASON_STATE_FORMAT_VERSION` and the bumped
  `SEASON_SAVE_FORMAT_VERSION` (bad version / out-of-bounds length prefix / trailing bytes), the
  `SeasonSaveCodec` gate posture.
- **Season-boundary roll (KD-6):** a full boundary transform is round-trip-deterministic and
  restartable (save mid-roll → restore → same continuation).

No closed-loop `#19 ScenarioRunner` scenario is *required* at the design stage, but a
`season-multi-fixture` capstone scenario (boot a `SeasonLoop`, play K fixtures, assert table +
determinism digest) is the natural §8 addition once the loop is wired — the match-engine capstone
precedent.

---

## 9. Open questions / risks

- **Format-version ordering (resolved by KD-1/KD-5).** #30 owns the `SEASON_SAVE_FORMAT_VERSION`
  1 → 2 bump and adds the season block as a third inner sub-blob; a future #27 on-disk-roster
  persistence pass adds a *fourth* inner blob at its own inner version — no collision, because the
  codec frames each block opaquely and each carries its own version gate. The section files must
  still **sequence the two bumps** if both land close together (whoever bumps the outer frame second
  rebases on the other's frame layout), but the design makes them non-colliding.
- **The day-advance tick order is the choke point for all of Wave 2+.** Getting the fixed tick order
  + the serialization boundary right *now*, with only the world tick live, is load-bearing: #28/#29/
  #33 each slot into a pre-declared null-seam position, so a wrong order here forces a re-pin across
  every Wave-2+ spec. KD-2 pins it conservatively (world tick first, then fixture play/ingest) and
  the section files record the reserved seam positions explicitly.
- **#22 phase-1 ingest is not #30's to activate (KD-3).** Per `FR-LW-032` (MUST) + roadmap §4,
  phase-1 activation needs #30 **and** #33; #30 only produces the event. The risk is scoping the
  ingest into #30 anyway — mitigated by KD-3 deferring it. The phase-1 ingest contract is co-defined
  at #33's landing (cross-checked against `FR-LW-027`/`FR-LW-032`/KD-9/KD-10, not the vol-2 §2.1/§3.1
  social-graph sections); any genuine gap is a #22 back-prop ERR filed then, not an invented
  interface now (FR-LW-031). #30's own deliverable — the playable spine (table/fixtures/calendar/
  board) — needs **none** of #22's memory model, so the deferral costs the spine nothing.
- **Single-league restriction is deliberate, not a shortcut.** The Stage-2 surface is one division
  (master plan §4.1); #43 generalizes the same machinery. The risk is authoring the table/fixture/
  boundary code in a way that *assumes* one competition — mitigated by KD-6 structuring the boundary
  roll as discrete steps #43 inserts into, and by the fixture/table types taking a competition set as
  data, not hard-coding "the league."

---

## 10. Promotion pipeline (proposed)

Same path #21–#27 followed, recorded here so the section-file pass has a checklist:

1. Self-adversarial review of **this supplement** to convergence (AR-1 → AR-n; an L-only or clean
   round closes it — the #21–#27 convention). Findings fixed in place.
2. Promote to a full 11-file section set at `docs/specs/season-competition-loop/` (`IN REVIEW`),
   FR prefix **FR-SN**, `SPEC_INDEX.md` registry row added + candidate-number reservation retired.
3. Section-file PASS-1 adversarial review → AR-2 convergence.
4. File the cross-spec back-props at approval: the #16 §3.4 `DOMAIN_TAG_SEASON_LOOP = 0x22` /
   `SubsystemOrdinals.SeasonLoop = 84` allocation (+ ERR-030-001), and — **only if the §2.1/§3.1
   cross-check finds a gap** — the #22 phase-1-contract back-prop.
5. Lead-developer R-01..R-05 sign-off → `APPROVED`.
6. Implement per the §6 T-phase plan (T0 season-state value types + `FixtureScheduler` +
   `LeagueTable`, behaviour-neutral world floor; T1 `SeasonSaveCodec` third sub-blob +
   `SEASON_SAVE_FORMAT_VERSION` 1 → 2 + `SeasonSaveManager` season parameter; T2 day-advance loop +
   the match-outcome event **producer** (emit + record — NOT #22 ingest activation, which is #33's
   gate per KD-3); T3 season-boundary roll).

---

#### Version History
| Version | Date | Notes |
|---|---|---|
| 0.1 | 2026-07-22 | Initial draft from the `spec-plans/spec-30-…` high-level plan + the roadmap, grounded in the actual `SeasonSaveCodec`/`SeasonSaveManager`/`WorldStore` APIs. KD-1..KD-8 pinned; §9 risks map to the plan's own open questions. Pre-promotion — no section files, no `SPEC_INDEX.md` row. |
| 0.2 | 2026-07-22 | **AR-1: 1M+3L, all fixed** (findings verified against real source, not narrated). **M-1:** KD-3/T2 over-committed #30 to *activating* #22's phase-1 ingest — contradicts `FR-LW-032` (MUST — Stage-1 activation gated on match-outcome events **and** vol-2/vol-3 impl., i.e. #33) + roadmap §4 ("do not activate before #30 **and** #33 land"), and `WorldLoop.cs` phase-1 has no interface (FR-LW-031); reframed — #30 is the phase-1 **producer** (emits + records the event), ingest activation is deferred to #33's landing and is out of the spine's scope (which needs none of #22's memory model); T2/§6/§9 aligned. **L-1:** stale cross-ref "#22 §2.1/§3.1" (vol-2 social-graph) → the real anchors `FR-LW-027`/`FR-LW-032`/KD-9/KD-10. **L-2:** KD-1 now states the `SeasonSaveCodec`/`SeasonSaveManager` signature change the third sub-blob forces. **L-3:** domain-tag gap honesty — file only `0x22`/84, leave `0x20`/`0x21` as gaps (not phantom pre-allocations for absent #28/#29), the `ERR-016-003` precedent. **AR-2: clean sweep** (0H+0M+2L, doc-only, fixed in place) — KD-4 label collision in §1 (living-world's KD-4 vs this doc's KD-4) disambiguated; §1 phase-1 gap cell updated to reflect #30-as-producer. **CONVERGED** (an L-only round closes it, the #21–#27 convention). |
