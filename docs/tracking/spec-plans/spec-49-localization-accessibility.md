# Spec #49 — Localization & Accessibility — High-Level Plan

> **Created:** July 22, 2026
> **Status:** PLAN (pre-design-supplement — no section files, no `SPEC_INDEX.md` row). Candidate spec number **#49** (proposed in `management-layer-spec-roadmap.md`, not reserved).
> **Master-plan home:** (cross-cutting) · **Tier:** S2 · **Wave:** 1 (seam + template contract) / 8 (locales + a11y content) · **FR prefix (proposed):** FR-LC
> **Determinism:** read-only / presentation-content — none (no RNG stream, no domain tag; no sim reference)
> **Purpose:** One localization seam routing all user-facing text (including procedurally generated text) through an i18n string catalogue, plus the accessibility surface.

## 1. Scope
The internationalization string catalogue and the accessibility (a11y) surface for the client. The load-bearing contract: **all** user-facing text — static UI strings and procedurally generated text alike (including #22's `InteractionTextGenerator` output, #35 media/press text, #46 news/inbox text) — routes through one localization seam. Accessibility covers the presentation-side concerns (text scaling, colour/contrast, input assist). **Out of scope:** the text *producers* (owned by #22/#35/#46 — #49 supplies the routing seam they emit through, it does not generate content); the UI framework hosting the localized strings (#38); the sim, which produces no user-facing text directly. #49 owns the seam and the catalogue, not the copy.

## 2. Staging (minimal-first → deep)
**The spec splits across two waves, mirroring #38 (framework early / content late)** — because the load-bearing invariant is that *every* text producer emits through the one seam, and those producers land across Waves 1–7. The **seam + template contract** (Wave 1) is the framework the producers bind to as they are authored; the **locales + a11y content** (Wave 8) is data added later. Minimal identity for the Wave-1 contract = a single catalogue + lookup seam with the base locale, where an un-added key falls through to a stable default (the identity: today's English text, byte-for-byte). Publishing the seam in Wave 1 means #38 (framework, Wave 1), #35/#46 (Wave 6), and #38 screens (Wave 7) emit through it from the start — only #22's already-built `InteractionTextGenerator` needs a retrofit, not every producer. The Wave-8 content tier adds locales and a11y options on that same seam — adding a locale is data, not new plumbing. One text-routing code path; the base locale is the identity later locales modulate.

## 3. Dependencies
- **Upstream (needs):** all user-facing text producers — #22 `InteractionTextGenerator` (procedural interaction/commentary text off `world.text`), #35 media/press, #46 news/inbox, and #38's static UI strings. The seam must accept both static keys and the slot-expanded procedural text these emit.
- **Downstream (consumers):** none — presentation/content layer; no sim assembly references it.

## 4. Persistent state & save impact
Locale/a11y selection is a client-local setting outside the determinism save. The string catalogue is a content artifact, not live game state — no `SEASON_SAVE_FORMAT_VERSION` / `WORLD_STORE_FORMAT_VERSION` impact. Presentation/content — no persistent sim state. (Note: because generated text like #22's is produced deterministically from sim state on demand, localizing it is a display transform — it does not change what is serialized.)

## 5. Determinism
Read-only / presentation-content — no RNG stream, no domain tag. Localization is a display-time lookup over sim-produced facts/keys; it advances no sim tick and draws from no stream. Critically, the localization transform must be applied *after* deterministic generation — #22's `world.text` draw and the serialized memory stay locale-independent, so a save round-trips identically regardless of display locale. The seam localizes the presented string, never the generated determinism-relevant state.

## 6. Key design decisions to resolve (the supplement must answer)
- **KD-1 (single seam — load-bearing):** all user-facing text (incl. #22/#35/#46 generated text) routes through one localization seam. What is that seam's contract — static keys plus a slot/template model for procedural text — and how do the generators emit through it without embedding baked strings?
- **KD-2:** localize-after-generate ordering — the transform is display-side so procedural determinism (#22's `world.text` draw, serialized memory) stays locale-independent. How is that boundary enforced (generators produce keys/slots; #49 renders)?
- **KD-3:** pluralization / gender / grammatical agreement for procedural text across locales — how deep at Stage 2, and what template model does #22's slot expansion adopt to support it?
- **KD-4:** accessibility scope at Stage 2 — text scaling / contrast / colourblind-safe presentation (reuse the dataviz-class colour discipline) / input assist — where is the minimal boundary?
- **KD-5:** fallback policy for a missing key/locale — stable default (base-locale identity) vs. visible marker; must never crash or mutate state.

## 7. Primary surfaces (proposed)
- A localization lookup seam (proposed) — the single routing point for all user-facing text.
- An i18n string catalogue (proposed) — keyed, per-locale content data.
- A procedural-text template/slot contract (proposed) that #22/#35/#46 emit through (keys + slots, not baked strings).
- An a11y presentation options surface (proposed) — client-local settings.

## 8. Test focus
Coverage lock: no user-facing string bypasses the seam (a producer emitting a baked non-key string fails a routing check). Base-locale identity: with only the base locale, presented text is byte-identical to today's strings. Localize-after-generate: #22 `world.text` generation + serialized memory are locale-independent (a save round-trips identically across display locales). Fallback fail-safe: a missing key/locale renders the stable default without crashing or touching sim state.

## 9. Open questions / risks
- The single-seam invariant (KD-1) is only as good as producer discipline — a generator that bakes a localized string forks the catalogue; enforce emit-through-seam at each producer's spec.
- Localize-after-generate (KD-2) is a determinism-adjacent trap: localizing before/inside generation would make `world.text` output locale-dependent and break save round-trip.
- Procedural grammar depth (KD-3) can balloon; Stage 2 must pick a bounded template model.
- The retrofit risk is the reason for the two-wave split: a single Wave-8 spec would force retrofitting the seam across #22/#35/#46/#38 after they had all baked non-keyed strings. Splitting the **seam + template contract into Wave 1** (published before its producers author emission) reduces that to a single retrofit of #22 (already built); every producer authored afterward emits through the seam from the start. (Contrast #50, which correctly stays Wave-8-whole: its per-bump migration steps are a *post-ship* concern, so pre-ship format bumps need no step and there is no continuous-emission retrofit to front-load.) The Wave-8 content tier can then be pure data.

## Version History
| Version | Date | Change |
|---------|------|--------|
| v0.1 | July 22, 2026 | Initial high-level plan. |
| v0.2 | July 22, 2026 | AR fix: split into a **Wave-1 seam + template contract** tier and a **Wave-8 locales + a11y content** tier (mirrors #38's framework/screens split) so text producers bind to the localization seam as they land instead of being retrofitted after Wave 8; header/§2/§9 updated; README + roadmap §7 reordered to match. |
