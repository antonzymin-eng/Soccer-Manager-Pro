# Spec #37 — Match Analytics & Statistics — High-Level Plan

> **Created:** July 22, 2026
> **Status:** PLAN (pre-design-supplement — no section files, no `SPEC_INDEX.md` row). Candidate spec number **#37** (proposed in `management-layer-spec-roadmap.md`, not reserved).
> **Master-plan home:** §3.3 · **Tier:** Stage 1 · **Wave:** 1 · **FR prefix (proposed):** FR-AN
> **Determinism:** read-only / presentation — no RNG stream, no domain tag (match-viewer / analytics class).
> **Purpose:** Possession, shots, pass-completion, tackles + advanced xG / PPDA / territorial % / heatmaps, derived read-only from the match engine's already-emitted event ledger — no new match-engine surface.

## 1. Scope
Match statistics derived **read-only** from the match engine's already-emitted event ledger (Event System #17 — the Tier A records `EventBus` serializes): basic stats (possession, shots/on-target, pass completion, tackles) and advanced ones (xG via a shot-location model, PPDA, territorial %, heatmaps). It mirrors `match-viewer`'s observational read — it adds no match-engine surface and mutates nothing. **Out of scope:** any new engine event or producer (if a stat needs an event the ledger doesn't emit, that is a match-engine change, not #37); the UI that renders the report (#38); news/inbox consumption (#46).

## 2. Staging (minimal-first → deep)
Stage-1 minimal = the basic counting stats read straight off the ledger (possession share, shot/pass/tackle tallies) — pure aggregation. The advanced layer (xG location model, PPDA, territorial %, heatmaps) is the same read-only aggregation deepened: one derivation path over the same ledger, with the advanced metrics as additional projections rather than a re-read. The xG location model is the only non-trivial computation and it is a pure function of shot-event geometry already in the ledger.

## 3. Dependencies
- **Upstream (needs):** the match engine's event ledger / Event System #17 (`EventBus` Tier A records) — read-only. No dependency on #27/#30 for the derivation itself.
- **Downstream (consumers):** #38 UI post-match report, #46 news/inbox. #44 discipline is a sibling read-only ledger derivation, not a consumer.

## 4. Persistent state & save impact
No persistent state — read-only presentation. Analytics are recomputed from the ledger (which is itself digest-load-bearing and already serialized as part of the match snapshot); #37 stores nothing new and bumps no format version. If a UI wants to persist a computed report, that is #38's/#30's concern, not #37's.

## 5. Determinism
Read-only / presentation — no RNG stream, no domain tag, no `SubsystemOrdinal` (the `match-viewer` / analytics class). Because analytics derive purely from the already-deterministic ledger, they are automatically reproducible: the same match snapshot yields the same stats. #37 must not introduce any draw or any dependency on wall-clock/observation order that would make the derivation non-deterministic.

## 6. Key design decisions to resolve (the supplement must answer)
- **KD-1** Is the ledger's current Tier A record set sufficient for every target stat, or do some (e.g. touch maps for heatmaps, precise shot geometry for xG) require a match-engine event addition — and if so, is that addition in scope for #37 or a match-engine follow-up?
- **KD-2** What is the xG location model — a pinned deterministic function of shot position/angle, and where do its coefficients live (a `[GT]` catalogue, illustrative pending a balance pass)?
- **KD-3** Does #37 consume the ledger live during a match (streaming, `match-viewer`-style) or only post-match from the snapshot — or both, via one aggregation core?
- **KD-4** What is the view-model contract #38 renders against, and how does it stay presentation-layer-clean (sim never references #37)?

## 7. Primary surfaces (proposed)
A read-only analytics aggregator over the event ledger (proposed); per-match basic + advanced stat view models (proposed); a deterministic xG location model (proposed); a heatmap/territorial-% derivation over positional/touch events (proposed). Existing seams referenced: the `EventBus` ledger / Event System #17, and the `MatchReplayRecorder` observational-read precedent in `src/match-viewer/`.

## 8. Test focus
Determinism of derivation: the same match snapshot/ledger yields byte-identical stats across two runs (two-run determinism); observer-neutrality — computing analytics does not perturb the match (the `match-viewer` digest-lock precedent); xG-model unit locks against hand-derived shot geometries; fail-loud on a malformed/truncated ledger. No save round-trip (no persistent state).

## 9. Open questions / risks
- The main risk is discovering a target stat the ledger cannot supply — that turns a "read-only, early, cheap" spec into a match-engine change with its own review; KD-1 must be settled first to keep #37 in the observational class.
- xG coefficients are `[GT]` and illustrative until a balance pass; the spec's contract is the model shape, not the tuned numbers (the #21/#8 precedent).
- Live-vs-post-match consumption (KD-3) affects whether #37 shares the `match-viewer` streaming path or is purely a snapshot reader.

## Version History
| Version | Date | Change |
|---------|------|--------|
| v0.1 | July 22, 2026 | Initial high-level plan. |
