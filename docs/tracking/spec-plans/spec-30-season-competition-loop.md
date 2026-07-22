# Spec #30 — Season & Competition Loop — High-Level Plan

> **Created:** July 22, 2026
> **Status:** PLAN (pre-design-supplement — no section files, no `SPEC_INDEX.md` row). Candidate spec number **#30** (proposed in `management-layer-spec-roadmap.md`, not reserved).
> **Master-plan home:** §4.1 / §4.5 · **Tier:** Stage 2 · **Wave:** 1 (Spine) · **FR prefix (proposed):** FR-SN
> **Determinism:** domain tag `0x22` / `SubsystemOrdinal` 84 (proposed — to pin at promotion).
> **Purpose:** The playable career spine — league table, deterministic fixtures, calendar/match-day flow, board objectives, multi-season continuity — that owns the `SeasonSave` composition root and drives it day to day.

## 1. Scope
The career/season game loop: round-robin fixture generation, a live league table, a calendar cursor with match-day flow, board objectives/job-security, and multi-season continuity. It **owns** `SeasonSaveManager` (`src/season-save/`) — the assembly already sitting above `match-engine` and `living-world` — and is the day-advance driver that ticks #28/#29/#22/#33 forward between fixtures. **Out of scope:** cups/continental/promotion-relegation (#43), finances (#40), the human-systems model it advances (#33), and transfers (#31); the Stage-2 surface is single-league (master plan §4.1).

## 2. Staging (minimal-first → deep)
Stage-2 minimal = one single-division round-robin league with a linear calendar and literal board objectives (finish position). This is authored as the identity: #43 later generalizes the same fixture/table machinery to multiple concurrent competitions and knockout draws, and season-boundary transforms (promotion/relegation) become a transform over the same league state — one code path with the competition set as a config dial, not a rewrite.

## 3. Dependencies
- **Upstream (needs):** #27 (roster world to schedule matches over), `MatchEngine` (plays each fixture), `SeasonSaveManager` / `SeasonSaveCodec` (the composition root it owns), `WorldStore` / `WorldClock` / `WorldLoop` (the day-advance substrate).
- **Downstream (consumers):** essentially every other management spec — #28/#29/#31/#32/#33/#35/#36/#43/#44/#45/#46 all attach to the day-advance loop and season events; #38 UI renders it.

## 4. Persistent state & save impact
Extends `SeasonSaveManager` from "world + optional in-progress match" to "world + **season state** (table, fixture list, calendar cursor, board state) + optional match". This is a **`SEASON_SAVE_FORMAT_VERSION` bump** (currently 1, `SeasonSaveConstants`), with the season block landing as another **opaque, independently version-gated sub-blob** — the codec-never-parses-sub-blobs pattern already in `SeasonSaveCodec`, so the match and world blobs stay untouched at their existing versions.

## 5. Determinism
Runs on the world tick (`WorldClock`, one day = one `worldTick`), never the 10 Hz/60 Hz match loops. Fixture generation is a pure function of the world seed (deterministic round-robin), so it needs no draw at all for the single-league case; a dedicated sub-stream under domain tag `0x22` / ordinal 84 covers any stochastic season events (objective setting, tie-breaks requiring a draw). The day-advance loop is where #22's phase-1 structured match-outcome ingest producer finally lights up (§4 of the roadmap).

## 6. Key design decisions to resolve (the supplement must answer)
- **KD-1** What is the exact season-state sub-blob layout, and does the calendar cursor live in the season blob or in `WorldStore` (which already owns `WorldClock`)?
- **KD-2** How does the day-advance loop order the tick of #28/#29/#22/#33 relative to fixture play, so save→restore mid-day is deterministic?
- **KD-3** How are structured match-outcome events emitted to #22's dormant `WorldLoop` phase-1 seam without #30 referencing `living-world` internals beyond the composition root?
- **KD-4** How is fixture generation pinned deterministic across a schedule regeneration (e.g. reload) — regenerate from seed, or serialize the fixture list?
- **KD-5** What is the multi-season continuity contract — how does a season-boundary transform (roll table, advance ages via #28, reset fixtures) stay one restartable, round-trip-deterministic step?

## 7. Primary surfaces (proposed)
A season-loop composition-root type owning `SeasonSaveManager` (proposed); a day-advance/match-day command API the UI and tests drive (proposed); a league-table + fixture-list view model for #37/#38 (proposed); a structured match-outcome event emitter feeding #22 phase-1 (proposed). Existing seams referenced: `SeasonSaveManager`, `SeasonSaveCodec`, `SeasonSaveConstants`, `WorldStore`, `WorldClock`, `MatchEngine`.

## 8. Test focus
Save→restore round-trip determinism for the full season blob (table + fixtures + calendar + board), byte-identical; two-run determinism of a simulated season from one seed; behaviour-neutral proof that an empty/no-match day advances the world identically to the pre-#30 `WorldStore.AdvanceDay`; fail-loud gates on the new `SEASON_SAVE_FORMAT_VERSION`; mid-day restore == uninterrupted advance.

## 9. Open questions / risks
- Format-version ordering: #30 bumps `SEASON_SAVE_FORMAT_VERSION` while #27 may want to persist rosters — must sequence the two bumps to avoid a collision.
- The day-advance loop becomes the integration choke point for every world-tick spec; getting the tick order + serialization boundary right early is load-bearing for all of Wave 2+.
- #22 phase-1 activation is a wiring change in #22, but if #30's event shape doesn't match FR-LW's expected contract it forces a #22 edit — cross-check #22 §2.1/§3.1 before pinning the event schema.

## Version History
| Version | Date | Change |
|---------|------|--------|
| v0.1 | July 22, 2026 | Initial high-level plan. |
