# Spec #51 — Audio & Sound Design — High-Level Plan

> **Created:** July 24, 2026
> **Status:** PLAN (pre-design-supplement — no section files, no `SPEC_INDEX.md` row). Candidate spec number **#51** (proposed in `docs/planning/master-plan-amendment-01-audio-multiplayer-transport.md` §2, not reserved).
> **Master-plan home:** §3 Stage 1 Month-11–12 "UI & Polish" Sound-effects bullet + §7 Stage-1 doc list item 29 "Sound Design", both owned by Amendment 01 §2 · **Tier:** S1 min → S2 full → S3+ deep · **Wave:** 8 · **FR prefix (proposed):** FR-AU
> **Determinism:** presentation — none (no RNG stream, no domain tag; the `match-viewer`/#37/#48 class)
> **Purpose:** The game-wide audio framework — mixer/buses, music, UI audio, client-local settings, accessibility hooks — that #48's match-audio slice and #38's screens play through.

## 1. Scope
The audio *framework*, distinct from match-audio *content*: a mixer/bus architecture (music / SFX / crowd / commentary / UI buses), a cue catalogue + playback API, music playback, UI/menu audio, per-channel client-local settings (volume/mute), ducking rules (e.g. commentary ducks crowd), and the audio-accessibility hooks whose visual/subtitle equivalents route through #49's content tier. **Out of scope:** mapping match events to audio cues — that is #48's audio-event mapper (read-only over the event ledger), which *feeds* #51's buses; commentary text generation (#22/#48); localization of any audio-adjacent strings (#49); anything sim-side — no sim assembly may reference audio.

## 2. Staging (minimal-first → deep)
Minimal identity = no mixer: #48's minimal SFX (crowd loop, whistle, ball contact — the master plan's own Stage-1 Month-11–12 "UI & Polish" bullet) played directly, with #51 absent entirely; a build with the framework disabled sounds exactly like that minimal path (silence where no cue exists). S2 (V1 release) = the full framework: mixer graph, music, UI audio, settings, complete match soundscape routed through buses. S3+ deep = commentary-audio delivery and presentation-depth integration alongside #48's 3D/animation tier. Framework is additive over one playback path — enabling a bus with neutral settings changes nothing audible.

## 3. Dependencies
- **Upstream (needs):** #48's audio-event mapper (proposed in #48's plan) as the match-side cue producer (note the wave inversion: #48 is Wave 7 and may land its trigger mapping against direct playback first; #51 then rehomes playback onto buses — a pure playback-side refactor, since the trigger contract does not change); #38's framework for settings screens and UI-audio trigger points; #49's seam for a11y cue equivalents; the Unity audio host binding (host-gated, the #38 rendering-binding class — the contract is authorable host-free).
- **Downstream (consumers):** #48 and #38 route playback through #51's buses; referenced by no sim assembly.

## 4. Persistent state & save impact
Client-local audio settings only (per-bus volume/mute), stored outside the determinism save — never in the match/season/world save formats. No format-version impact on any sim save. Presentation layer — no persistent sim state.

## 5. Determinism
Presentation — no RNG stream, no domain tag (the `match-viewer` precedent). Observer neutrality is the load-bearing property: a match played with full audio is byte-identical to an unobserved same-seed run. Any cue-selection variation (e.g. alternating ball-contact samples) uses display-side, non-determinism-pinned randomness — never a `deterministic-sim` stream, and never `System.Random`-in-game-logic (audio is not game logic; the rule is that audio code can never *become* game logic by being read back). Nothing in the audio path writes to or is read by the sim.

## 6. Key design decisions to resolve (the supplement must answer)
- **KD-1 (boundary with #48 — load-bearing):** #48 owns event→cue *mapping*; #51 owns cue *playback* (catalogue, buses, mixing). Pin the cue-identifier contract between them so neither owns the other's half — and confirm the Wave-7/8 inversion in §3 stays a playback-side refactor only.
- **KD-2:** bus taxonomy and ducking rules — fixed catalogue (music/SFX/crowd/commentary/UI) vs. data-driven graph; where do ducking priorities live ([GT]-class client config, not sim config)?
- **KD-3:** settings persistence — file location, schema, and versioning for the client-local settings store (explicitly outside #50's save-migration scope, or a thin client-side analogue of it?).
- **KD-4:** the a11y contract with #49 — does every audible cue declare a visual/subtitle equivalent at cue-catalogue registration (coverage by construction, the FR-LC-008a pattern), or is coverage audited separately?
- **KD-5:** host gating — which tests run host-free (catalogue/routing/settings contract) vs. Unity-host-only (actual playback), mirroring the #38 rendering split.

## 7. Primary surfaces (proposed)
- An audio bus/mixer graph + ducking rules (proposed).
- A cue catalogue + playback API (proposed) — the surface #48's mapper and #38's UI triggers call.
- A client-local settings store (proposed), outside all sim save formats.
- An a11y cue-equivalence registry (proposed), consumed by #49's content tier.

## 8. Test focus
Observer neutrality: a full-audio match run is byte-identical to an unobserved same-seed run (the `MatchViewerTests` digest-lock class extended to audio). Layer-taxonomy lock: no sim assembly references the audio assembly. Cue-catalogue completeness: every #48 event mapping and every #38 UI trigger resolves to a defined cue on a defined bus (fail-loud on a dangling cue id). Settings round-trip without touching any sim save; neutral settings ⇒ audibly identical to the pre-framework path. Per KD-5, playback itself is host-gated; CI locks the contract layer only.

## 9. Open questions / risks
- Asset-heavy, engineering-light: the spec is contract + catalogue, not DSP — scope creep into "match feel tuning" or asset production must stay out of spec text (the #48 §9 risk, doubled).
- The Wave-7/8 inversion (KD-1): if #48 lands direct playback first, the #51 rehoming must be provably inaudible-neutral, or #48 ships against a stub bus API early.
- Host gating: no Unity audio in the Linux CI gate — a contract green-light is not a playback green-light (same caveat class as `match-client-unity`).
- A11y drift (KD-4): cues added after the registry exists must not silently skip equivalents — prefer coverage by construction.

## Version History
| Version | Date | Change |
|---------|------|--------|
| v0.1 | July 24, 2026 | Initial high-level plan, per Master Plan Amendment 01 §2. |
| v0.2 | July 24, 2026 | AR-1 fixes: master-plan anchor corrected §3.4 → §3 Month-11–12 "UI & Polish" (header + §2); §3 marks #48's audio-event mapper "(proposed)" per the README template convention. |
