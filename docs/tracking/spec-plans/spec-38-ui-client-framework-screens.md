# Spec #38 — UI / Client Framework & Screens — High-Level Plan

> **Created:** July 22, 2026
> **Status:** PLAN (pre-design-supplement — no section files, no `SPEC_INDEX.md` row). Candidate spec number **#38** (proposed in `management-layer-spec-roadmap.md`, not reserved).
> **Master-plan home:** §3.1 / §3.4 / §4 · **Tier:** S1 min → S2 full · **Wave:** 1 (framework) / 7 (screens) · **FR prefix (proposed):** FR-UI
> **Determinism:** read-only / presentation — none (no RNG stream, no domain tag; the `match-viewer` class)
> **Purpose:** The Unity UGUI client — menus, tactics screen, interactive match view, and Stage-2 management screens — reading observation surfaces and mutating sim only through existing public command seams.

## 1. Scope
The whole player-facing client: main menu / navigation shell, the tactics screen, the interactive match view (upgrading the current bare live web viewer — `LiveMatchStreamer` / `LiveMatchServer`), and the Stage-2 management screens (squad, transfer, training, scouting). Unity UGUI per master plan §3.4. It renders every data/loop/analytics spec through view models and issues manager intent through the sim's existing public command seams. **Out of scope:** all sim/model logic (owned by the data + loop + analytics specs); commentary/animation/audio depth (that is #48); localization/a11y routing (that is #49); the on-disk save/migration contract (#30/#50). The UI computes no game state — it only presents and commands.

## 2. Staging (minimal-first → deep)
Minimal identity = a thin shell over what already runs: menus + tactics screen + an interactive match view that observes a live `MatchEngine` (the `MatchReplayRecorder` / `LiveMatchStreamer` observation path, promoted from bare web viewer to an in-client UGUI surface). Every later management screen attaches to that same navigation + view-model contract without changing the framework — the framework spec is the identity the screen specs extend, one navigation/view-model code path, not a rewrite per screen.

## 3. Dependencies
- **Upstream (needs):** all data specs (#27 rosters, #28/#29/#31/#32 management data), #21 tactical instructions, the match-engine observation surface (`BallView`/`AgentView`/`PossessingAgentId`/`HomeScore`/`MatchEnded`), #30 season loop (calendar/table/day-advance), #37 analytics (post-match reports). Presentation depth (#48) and localization (#49) compose alongside.
- **Downstream (consumers):** none — top of the dependency graph; no sim assembly may reference it.

## 4. Persistent state & save impact
No game-state persistence — UI preferences / layout / last-screen are client-local settings outside the determinism save. No `SEASON_SAVE_FORMAT_VERSION` / `WORLD_STORE_FORMAT_VERSION` impact. Presentation layer — no persistent sim state.

## 5. Determinism
Presentation/infra — no RNG stream, no domain tag (the `match-viewer` precedent: `TacticalDirector.MatchViewer` is referenced by no sim assembly and is digest-locked observer-neutral). The UI needs none because it never advances the sim or draws from a deterministic stream — it reads value-type observation copies and issues commands the sim already validates. Observer neutrality (reading state must not change it) is the load-bearing property, not reproducibility of the UI itself.

## 6. Key design decisions to resolve (the supplement must answer)
- **KD-1 (layer taxonomy — load-bearing):** the UI is the presentation layer; no sim assembly may reference it (the `match-viewer` contract). It reads observation surfaces + view models and mutates sim ONLY through existing public command seams (`SetTeamTactic`/`SetPlayerTactic`, the loop's day-advance / transfer-action APIs). What is the exact view-model contract that keeps this one-directional?
- **KD-2 (the split):** the cluster almost certainly splits into (a) UI framework + screen-navigation + view-model contract, (b) tactics / match-view screens (S1), (c) management screens (S2). Confirm the split, author the framework spec first, screen specs as it stabilises.
- **KD-3:** what is the view-model refresh model against the world tick vs. the 10 Hz/60 Hz match loops — does the match view poll the observation surface each frame, and how is that decoupled from the sim tick?
- **KD-4:** which command seams are missing for management intent (squad selection, transfer action, training focus, day-advance), and are those the loop/data specs' surfaces to add — never the UI's — before the screen spec can consume them?
- **KD-5:** how do #37 analytics + #48 presentation depth + #49 localization compose into a screen without the UI owning any of their logic?

## 7. Primary surfaces (proposed)
- A view-model contract layer (proposed) — read-only projections of sim/loop/analytics state for screens to bind.
- A navigation/screen-shell framework (proposed) over UGUI.
- An interactive match-view surface (proposed) over the existing observation path (`MatchReplayRecorder` / `LiveMatchStreamer` promoted into the client).
- Command dispatch through existing public seams only (proposed) — `SetTeamTactic`/`SetPlayerTactic` and the loop/transfer action APIs; the UI adds no new mutation path.

## 8. Test focus
Layer-taxonomy enforcement: assert no sim assembly references the UI assembly (the `match-viewer` no-reverse-reference lock). Observer neutrality: a match observed through the UI is byte-identical to an unobserved same-seed run (the `MatchViewerTests` digest-lock class). The UI must not mutate sim except via existing public command seams — verified by confirming command dispatch routes only through `SetTeamTactic`/`SetPlayerTactic` / the loop's public action APIs. Fail-loud on malformed/out-of-range view-model input.

## 9. Open questions / risks
- The split (KD-2) is the biggest structural fork; authoring one monolithic UI spec would couple framework and screens and thrash as data specs land.
- Screen specs are gated on their data specs existing (Wave 7) — building a transfer screen before #31 lands is the phantom-consumer trap.
- Temptation to add a UI-side "convenience" mutation seam that bypasses sim validation — any such seam violates KD-1 and must be pushed down into the owning sim/loop spec.
- Match-view refresh cadence vs. sim tick decoupling (KD-3) is a correctness-adjacent risk (tearing / stale reads) even though the UI has no determinism obligation.

## Version History
| Version | Date | Change |
|---------|------|--------|
| v0.1 | July 22, 2026 | Initial high-level plan. |
