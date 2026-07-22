# Spec #36 — National Teams & International Management — High-Level Plan

> **Created:** July 22, 2026
> **Status:** PLAN (pre-design-supplement — no section files, no `SPEC_INDEX.md` row). Candidate spec number **#36** (proposed in `management-layer-spec-roadmap.md`, not reserved).
> **Master-plan home:** §5 Stage 5 · **Tier:** S5 · **Wave:** 6 · **FR prefix (proposed):** FR-NT
> **Determinism:** domain tag `0x28` / SubsystemOrdinal 90 (proposed off-pitch block, roadmap §6)
> **Purpose:** National-team management — call-ups, international windows as a calendar overlay on #30, and tournaments — with the national squad as a selection view over #27's global pool.

## 1. Scope
Call-up selection, international windows scheduled as a calendar overlay on the #30 season loop, and international tournaments (group + knockout). The national squad is a **selection view** over #27's global player pool — never a copy or a mutation of it. Availability and fatigue from international duty feed back into the #30 calendar.
Out of scope: the global-sim scope that populates other nations' rosters (matures at Stage 5, a hard prerequisite), the base fixture/table machinery (generalised from #30/#43), the draw mechanics if #43 owns them.

## 2. Staging (minimal-first → deep)
Minimal surface = a single international window that withdraws called-up players from club availability and returns them — the **identity** the deep tier modulates. Deep tier adds full tournament structure, qualification, and a national-team job for the manager on the same calendar-overlay + selection-view code path.

## 3. Dependencies
- **Upstream (needs):** #27 Squad/Player Data (the global pool the national squad selects over), #30 Season & Competition Loop (calendar to overlay windows onto; fixture/table machinery to generalise).
- **Downstream (consumers):** #30 calendar/availability (call-ups mark players unavailable for club fixtures during windows), player fatigue/progression via the world tick.

## 4. Persistent state & save impact
New season/world state: call-up selections, tournament state, international-window cursor. Lands as an opaque, independently version-gated sub-blob → `SEASON_SAVE_FORMAT_VERSION` bump (the season block already exists per #30; this extends it or adds a sibling sub-blob the codec never parses). National squads are views, not stored rosters — only the selection is persisted.

## 5. Determinism
World tick (`WorldClock`). New RNG sub-stream + domain tag `0x28` + `SubsystemOrdinals` entry 90 (proposed) for tournament draws and any call-up stochasticity; deterministic from the world seed like #43's competition draws. Registration pinned at promotion.

## 6. Key design decisions to resolve (the supplement must answer)
- **KD-1** National squad = a **selection view** over #27's pool with zero mutation of canonical records — define the view contract (like #32's fog-of-war-as-view discipline, but for eligibility/selection).
- **KD-2** International windows as a **calendar overlay** on #30: how does a window preempt club fixtures and mark availability without #36 owning the calendar (one-directional dependency into #30)?
- **KD-3** Draw ownership: does #36 reuse #43's knockout-draw machinery or define its own? Avoid duplicate draw logic.
- **KD-4** Global-sim maturity gate: which minimal surface is authorable **before** other nations' rosters are fully simulated (Stage 5), and what is explicitly deferred until then?
- **KD-5** Fatigue/availability reconciliation with #29 training and club match load on the world-tick accumulator.

## 7. Primary surfaces (proposed)
- A call-up selection view (proposed) over the #27 pool.
- A calendar-overlay registration (proposed) that #30's day-advance loop honours for windows.
- A tournament-state advance entry point (proposed), draws off the `0x28` stream (or delegated to #43).

## 8. Test focus
Round-trip determinism over call-up/tournament/window state; identity behaviour (a single window withdraws and returns players deterministically); selection-view non-mutation of #27 records; deterministic tournament draw from the world seed; fail-loud on ineligible call-ups.

## 9. Open questions / risks
- Hard Stage-5 dependency on global-sim scope — most of the spec is not authorable until nations are simulated; the minimal surface must be carved carefully.
- Draw-machinery duplication with #43.
- Calendar-overlay coupling must stay one-directional into #30.

## Version History
| Version | Date | Change |
|---------|------|--------|
| v0.1 | July 22, 2026 | Initial high-level plan. |
