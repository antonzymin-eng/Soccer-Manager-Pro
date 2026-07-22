# Spec #48 — Match Presentation Depth — High-Level Plan

> **Created:** July 22, 2026
> **Status:** PLAN (pre-design-supplement — no section files, no `SPEC_INDEX.md` row). Candidate spec number **#48** (proposed in `management-layer-spec-roadmap.md`, not reserved).
> **Master-plan home:** §3.1 · **Tier:** S1 min → S2+ deep · **Wave:** 7 · **FR prefix (proposed):** FR-MP
> **Determinism:** read-only / presentation — none (no RNG stream, no domain tag; the `match-viewer` class)
> **Purpose:** Commentary, animation/3D, and audio layered over the live viewer — observation-only, mutating nothing in the sim.

## 1. Scope
The presentation depth that upgrades the bare live viewer into a watchable match: text commentary, animation / 3D rendering of agents and ball, and audio (crowd, effects). All of it derives from the match-engine observation surface — the same observation-only contract `match-viewer` already honours. Commentary text can consume #22's `InteractionTextGenerator` (deterministic procedural text off the `world.text` sub-stream). **Out of scope:** the UI framework / navigation / screens that host the match view (that is #38); the match simulation itself (owned by match-engine); localization of commentary strings (routes through #49); any gameplay outcome (presentation reads results, never produces them).

## 2. Staging (minimal-first → deep)
Minimal identity = the bare live viewer's current fidelity (2D positions + score, the `LiveMatchStreamer` / `HtmlReplayExporter` observation output). The deep tier — commentary, 3D animation, audio — layers on top of that same observation path without changing what is observed; presentation depth is additive over one observation code path, so a match with all depth disabled renders exactly the minimal viewer. Nothing in the pipeline feeds back into the sim.

## 3. Dependencies
- **Upstream (needs):** the match-engine observation surface (`BallView`/`AgentView`/`AgentTeamId`/`AgentIsGoalkeeper`/`PossessingAgentId`/`HomeScore`/`MatchEnded`) and the event ledger for commentary triggers; #22's `InteractionTextGenerator` for procedural commentary text; #37 analytics (optional, for stat-driven commentary lines).
- **Downstream (consumers):** none — hosted by #38's match view but referenced by no sim assembly.

## 4. Persistent state & save impact
No persistent game state — commentary/animation/audio are derived per-frame from observation and the ledger. Audio/graphics settings are client-local, outside the determinism save. No format-version impact. Presentation layer — no persistent sim state.

## 5. Determinism
Presentation/infra — no RNG stream, no domain tag (the `match-viewer` precedent). It needs none because it only observes value-type copies of sim state and replays the already-produced event ledger; it advances no sim tick and draws from no deterministic stream. Where commentary text is procedurally generated, that determinism is #22's `InteractionTextGenerator` `world.text` sub-stream — #48 consumes it, it does not own a stream. Observer neutrality (rendering a match must not perturb it) is the load-bearing property.

## 6. Key design decisions to resolve (the supplement must answer)
- **KD-1 (layer taxonomy — load-bearing):** presentation depth is observation-only (the `match-viewer` contract); no sim assembly may reference it, and it never mutates match state. What is the observation/event-ledger read contract it binds to?
- **KD-2:** commentary text — consume #22's `InteractionTextGenerator` (build as a consumer of the deterministic generator) rather than forking a fresh text system; what facts/slots does a match feed it, and does commentary determinism ride the `world.text` sub-stream or is it purely display-side (non-determinism-pinned, like the HTML replay coordinates)?
- **KD-3:** animation/3D — does 3D need any new observation field on the match engine (e.g. richer pose data), and if so is that an additive read-only surface owned by match-engine, never a presentation-side push?
- **KD-4:** audio-event triggering off the event ledger — read-only consumption vs. any new emission (must be read-only, per #37/analytics precedent).
- **KD-5:** how does #48 compose into #38's match view without #38 owning presentation logic or #48 owning UI framework?

## 7. Primary surfaces (proposed)
- A commentary generator (proposed) consuming #22's `InteractionTextGenerator` + match facts / event ledger.
- An animation/3D render layer (proposed) over the observation surface (`AgentView` / `BallView`).
- An audio-event mapper (proposed) — read-only over the event ledger.
- All bound through the same observation-only contract `match-viewer` established (proposed) — no reverse sim reference.

## 8. Test focus
Observer neutrality: a match rendered with full presentation depth is byte-identical to an unobserved same-seed run (the `MatchViewerTests` digest-lock class extended to animation/audio, and to commentary **only if KD-2 resolves commentary to display-only / non-`world.text` text** — commentary that draws from the serialized `world.text` stream advances persisted state and therefore cannot be observer-neutral; this test is conditioned on that KD-2 decision, not asserted over a world.text-backed commentary path). Layer-taxonomy lock: no sim assembly references the presentation-depth assembly. Where commentary consumes `world.text`, save-boundary generation determinism (the `WorldStore` `Snapshot`/`Restore` text-resume property). Fail-loud on malformed observation/ledger input; presentation must not mutate sim.

## 9. Open questions / risks
- Commentary determinism scope (KD-2): if commentary rides `world.text`, its draws must not perturb #22's arc cursor — reuse the aperiodic-sub-stream discipline #22 already established, or keep commentary display-only (non-pinned).
- 3D pose data (KD-3): a temptation to push richer state out of the sim for rendering would invert the layer taxonomy — any new field must be an additive read-only match-engine surface.
- Depth is Wave 7 (hosted by #38); building it before the #38 match view exists risks a presentation surface with no host.
- Audio/animation are the largest asset/engineering surface — the spec is mostly contract + trigger mapping, not sim logic; scope creep into "match feel tuning" must stay display-side.

## Version History
| Version | Date | Change |
|---------|------|--------|
| v0.1 | July 22, 2026 | Initial high-level plan. |
| v0.2 | July 22, 2026 | AR fix: §8 observer-neutrality lock conditioned on KD-2 — commentary that draws from the serialized `world.text` stream advances persisted state and cannot be observer-neutral; the byte-identical claim now applies to animation/audio unconditionally and to commentary only if KD-2 keeps it display-only. |
