# Season & Competition Loop Specification #30 — Section 1: Introduction, Scope, Dependencies

**Created:** July 22, 2026
**Last Updated:** August 8, 2026 (v0.3 — the lint sweep: §1's null-seam list stops naming #29 among specs that do not exist)
**Last Updated (prior):** July 22, 2026 (v0.2 — section-file PASS-1 fixes, §9.3)
**Version:** 0.3
**Status:** APPROVED
**Source:** `docs/tracking/season-competition-loop-design.md` v0.2

---

## 1.1 Purpose and scope

This spec defines the **season/career game loop** — the playable spine that turns "a match engine
that plays one fixture" and "a world store that advances one day" into "a season you play." **In
scope:** deterministic round-robin fixture generation, a live league table with pinned tie-breaks, a
calendar cursor with match-day flow, board objectives / job-security, multi-season continuity (the
season-boundary roll), the `SEASON_SAVE_FORMAT_VERSION` bump that persists all of it, and the
day-advance loop that becomes the integration choke point for every later world-tick spec.

**Out of scope (explicitly, per design supplement §0), each its own spec:** cups / continental /
promotion-relegation (**#43**), finances (**#40**), the human-systems model this loop advances
(**#33**), transfers (**#31**), injuries (**#41**), progression/aging (**#28**), training (**#29**),
discipline/suspensions (**#44**), and the UI that renders the loop (**#38**). The Stage-2 surface is
**single-division, single-competition** (master plan §4.1). Where this loop must *tick* a world-tick
spec that has not yet landed (#28/#33 today — #29 and #41 landed at T2 and occupy slots 2/4 LIVE;
currency corrected at the lint sweep, August 8, 2026, the FR-SN-034 class in §1), it does so through a
**documented null seam** — the
`WorldLoop` phase-1/2/5 precedent (FR-LW-031: no phantom interfaces) — never an invented interface.

This is a **Stage-1-forward pull** (master plan §4.1/§4.5 place the full loop at Stage 2), motivated
by the concrete present-day gap: there is no season to play. `MatchEngine` plays one fixture,
`WorldStore.AdvanceDay()` advances one world-day, and `SeasonSaveManager` persists "a world + an
optional in-progress match" — but nothing schedules fixtures, keeps a table, moves a calendar, or
connects one match to the next.

**Promotion posture (forward design).** Unlike #27 (which documented already-built code), **nothing
in #30 is built yet.** Section text is a specification-before-code plan; §7 is a forward T-phase plan,
not a landed-status record. This matches the #21–#26 IN-REVIEW posture.

## 1.2 Convention inheritance

Inherits the project conventions verbatim: corner-origin pitch (Ball Physics #1 §1.2); every constant
carries exactly one `[FIXED]/[DERIVED]/[GT]/[CROSS]` tag; deterministic RNG only (HKDF-SipHash via #16
— no `System.Random`, no `DateTime.Now`). The loop runs on the **world tick** (`WorldClock`: one
`worldTick` = one calendar day, living-world KD-4) — **never** the 10 Hz tactical / 60 Hz physics
match loops (FR-SN-025). Save/restore obeys the project's round-trip determinism contract ("save@N →
restore → advance == uninterrupted run", byte-identical), lifted from the match engine to the season
loop.

## 1.3 Dependencies

| Dep | Direction | Nature |
|---|---|---|
| Squad/Player Data #27 | this → it | the roster world (which clubs exist); the `Squad` each club fields, resolved via `MatchEngine.ConfigureSquads` / `ISquadProvider` |
| Match Engine (design note) | this → it | plays each fixture; `MatchSaveManager.Encode`/`Restore` for the in-progress-match blob; `RestoreFromSnapshot` |
| Living World #22 | this → it (composition root only) | `WorldStore.AdvanceDay`/`.Snapshot`/`.Restore` — the day-advance substrate; #30 becomes the phase-1 **producer** (KD-3), never touching `living-world` internals (FR-LW-003) |
| Unified Season Save (`src/season-save/`) | this owns/extends | `SeasonSaveManager` / `SeasonSaveCodec` / `SeasonSaveConstants` — the composition root #30 extends with a season-state sub-blob |
| Deterministic Sim #16 | this → it | a season/fixture RNG sub-stream; `DOMAIN_TAG_SEASON_LOOP = 0x22` + `SubsystemOrdinals.SeasonLoop = 84` allocated there (KD-5) |
| Event System #17 | this → it | reads the match goal/card ledger to build a `MatchResult` |
| Code Standards #20 | governs | layering, naming, constant tags |

`TacticalDirector.SeasonSave` already sits **above both** `match-engine` and `living-world` — the
only assembly that may reference both (FR-LW-003 keeps those two independent). #30 extends that
existing composition root; it introduces no new cross-layer reference direction.

## 1.4 Key decisions

- **KD-1 — Season state is a third opaque sub-blob.** The season state (table / fixtures / calendar
  cursor / board state) is persisted as a **third opaque, independently version-gated sub-blob** in
  `SeasonSaveCodec` (which today frames a `matchPresent` flag + a world blob + an optional match blob,
  never parsing either). The block carries its own `SEASON_STATE_FORMAT_VERSION`; the outer
  `SEASON_SAVE_FORMAT_VERSION` bumps **1 → 2** (a frame-layout change: a third length-prefixed block
  joins the frame). The world and match blobs stay **byte-untouched** at their existing versions.
- **KD-2 — The day-advance tick order is the integration choke point.** `AdvanceToNextFixtureDay` is
  one restartable, round-trip-deterministic step: for each intervening calendar day, run the
  world-tick spec ticks in a **fixed, documented order** (Wave-2+ specs #28/#29/#33 slot in as null
  seams; `WorldStore.AdvanceDay()` is the only live tick today), then, on a fixture day, play the
  **whole round** (KD-9). This order is **load-bearing for all of Wave 2+**, so it is pinned here even
  with only the world tick live; a save may land mid-sequence and restore must equal an uninterrupted
  advance.
- **KD-9 — A fixture-day resolves the whole round; the managed club plays in full, the rest
  deterministically.** A round holds `N/2` fixtures (every club plays), so a fixture-day MUST resolve
  **all** of them and apply every result to the table — resolving a subset leaves the unplayed clubs'
  rows undefined, and "a league table" that reflects only one club's matches is not a league table.
  `SeasonState.ManagedClubId` selects the one fixture that runs through the full `MatchEngine` (under
  the human's tactical influence, #21); the others resolve through a deterministic round-resolution
  model (§3.4.1). The **minimal identity** may full-sim every fixture; the **quick-sim** deepening
  resolves non-managed fixtures from the `DOMAIN_TAG_SEASON_LOOP` sub-stream — a `SeasonState`/config
  dial, not a rewrite, and the concrete consumer of the reserved season RNG stream.
- **KD-3 — #30 is the phase-1 *producer*; ingest activation is deferred to #33.** #30 defines and
  emits the structured match-outcome event, becoming the producer #22's `WorldLoop` phase-1 seam was
  written for. It does **not** wire the event into #22's ingest: `FR-LW-032` (a MUST) gates Stage-1
  phase-1 activation on **both** match-outcome events (#30) **and** the vol-2/vol-3 human-systems
  implementation (#33), and phase-1 has no interface today (FR-LW-031). A match outcome has no meaning
  on the manager↔player memory edges until #33 defines it, so building the ingest now would wire a
  consumer ahead of its producer. The ingest entry point is **not added here**; the payload shape is
  co-defined at #33's landing, cross-checked against `FR-LW-027`/`FR-LW-032`/living-world KD-9/KD-10.
- **KD-4 — Calendar cursor lives in the season blob, not `WorldStore`.** `WorldStore` owns `WorldClock`
  (the calendar *day*). The **fixture calendar cursor** (which match-day slot / fixture round is next)
  is **season-scoped state, not world-time** — the world clock advances continuously; the fixture
  cursor is a discrete pointer into the schedule. It lives in the season sub-blob, independently
  validated on restore, with the invariant that "next fixture day ≥ current `WorldClock` day."
- **KD-5 — Serialize the fixture list, don't regenerate on load.** The fixture generator is a pure
  `Generate(clubIds, seed) → Fixture[]` (circle method) used at **season creation**; the concrete
  schedule is **serialized** in the season blob. Rationale: regenerating on load is only safe if the
  club set + generator are byte-stable across the build that saved and the build that loads (a #50
  Save-Migration concern); serializing the concrete schedule makes a loaded season independent of
  generator-version drift. The schedule is small (`N·(N−1)` fixtures). (The #19 `ScenarioIndex` /
  #27 roster-reference "author the concrete value, don't recompute on load" posture.)
- **KD-6 — Multi-season continuity is one restartable step.** The season-boundary roll is a single
  round-trip-deterministic transform: finalize the table → evaluate board objectives / job-security →
  (Stage-2 minimal) regenerate fixtures for the new season → advance ages via #28 (a null seam today)
  → reset the table. It is restartable (a save mid-roll restores to the same point) and is **where
  #43's promotion/relegation transform later slots in** (between "finalize table" and "regenerate
  fixtures") without changing the surrounding steps.
- **KD-7 — Single-writer + observation-surface discipline.** `SeasonLoop` is the **sole writer** of
  season state; `SeasonViewModel` is a read-only value-copy surface for #37/#38 (the `match-viewer` /
  `MatchEngine.BallView` observer-neutral posture — reading never mutates, the round-trip is
  unaffected). UI/tests mutate season state **only** through the public command API
  (`AdvanceToNextFixtureDay`, `AdvanceAndPlayNextRound`, the boundary roll) — the `SetTeamTactic` command-seam
  precedent — never by poking fields.
- **KD-8 — Behaviour-neutral world-advance floor.** A no-fixture day advances the world
  **byte-identically** to a bare `WorldStore.AdvanceDay()` — the loop adds scheduling and result
  ingest, it does not change how a plain day ticks (the #21/#27 default-neutrality discipline; a §5
  test).

## 1.5 Boundary matrix

| # | Boundary | This spec | Other side |
|---|---|---|---|
| 1 | Season state (table/fixtures/calendar/board) | owns `SeasonState`, sole writer (KD-7) | — |
| 2 | Calendar day (world time) | reads `WorldClock` via `WorldStore` | Living World #22 owns `WorldClock` (living-world KD-4) |
| 3 | One fixture's play | orchestrates via `MatchEngine`/`MatchSaveManager` | Match Engine owns the tick pipeline |
| 4 | Roster per club | resolves via `ISquadProvider` / `ConfigureSquads` | Squad/Player Data #27 owns `Squad`/`PlayerRecord` |
| 5 | Match-outcome event | **produces** it (KD-3) | Living World #22 owns the phase-1 **ingest** — deferred, activated with #33 |
| 6 | Save frame | owns the season sub-blob + `SEASON_SAVE_FORMAT_VERSION` | world blob = `WorldStore`, match blob = `MatchSaveManager` (opaque, untouched) |
| 7 | Determinism substrate | consumes only | #16 owns `DeterministicRngService` + `DOMAIN_TAG_SEASON_LOOP`/ordinal |
| 8 | Multiple competitions / promotion-relegation | not built (single league) | #43 generalizes this loop's machinery |

## 1.6 Stage binding

Stage-2 minimal = one single-division double round-robin league, linear calendar, literal "finish ≥
P" board objectives — authored as the **identity** #43 modulates (§2 staging). The T-phase plan (§7)
lands it in slices: T0 value types + `FixtureScheduler` + `LeagueTable` (behaviour-neutral world
floor); T1 the `SeasonSaveCodec` third sub-blob + `SEASON_SAVE_FORMAT_VERSION` 1 → 2 +
`SeasonSaveManager` season parameter; T2 the day-advance loop + the match-outcome **producer**; T3 the
season-boundary roll. #22 ingest activation, finances (#40), promotion-relegation (#43), and the UI
(#38) are all out of scope (§1.1).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial section from supplement v0.2; forward-design plan; KD-1..KD-8 carried from supplement §7; KD-9 (whole-round resolution) added at section-file PASS-1. |
| 0.2 | 2026-07-22 | — | Section-file PASS-1: whole-round resolution (KD-9 / FR-SN-012/013a/013b / §3.4 / ManagedClubId), API-name corrections (`RunTick`→`MatchEnded`, `ResolveByClubId`), `uint` world-day, KD-collision + label reconciliation. See section-9 §9.3. |
| 0.3 | 2026-08-08 | — | **Lint sweep**: §1's boundary text still counted #29 among world-tick specs "that do not exist yet" — the FR-SN-034/§3.3 class (passes 11–12) reaching §1. |
#endregion
