# Spec #29 — Training System — High-Level Plan

> **Created:** July 22, 2026
> **Status:** PLAN (pre-design-supplement — no section files, no `SPEC_INDEX.md` row). Candidate spec number **#29** (proposed in `management-layer-spec-roadmap.md`, not reserved).
> **Master-plan home:** §4.4 · **Tier:** Stage 2 min → Stage 3 deep · **Wave:** 2 · **FR prefix (proposed):** FR-TR
> **Determinism:** domain tag `0x21` / `SubsystemOrdinal` 83 (proposed — to pin at promotion).
> **Purpose:** Weekly team + individual training driving fitness/form/injury-risk and (deep tier) attribute growth feeding #28, advanced on the world tick.

## 1. Scope
Weekly team and individual training scheduled on the **world tick**: a training focus → effects on fitness, form, and injury risk, and (Stage 3) granular per-attribute growth that becomes an *input to* #28's CA/PA curve. **Out of scope:** the growth curve itself (#28 owns it — #29 feeds it), injury occurrence/severity/recovery (#41 owns the model; #29 supplies the risk input), and coaching-staff attributes (#34, which modulates #29). Must reconcile training fatigue (world-tick accumulator) with the match engine's in-match fatigue (match-tick).

## 2. Staging (minimal-first → deep)
Stage-2 minimal = the §4.4 "pick one of N focuses → affects form/fitness only" model: a per-player weekly focus selection with a deterministic form/fitness delta, no attribute change. Stage-3 deep adds granular per-attribute training that writes into #28's shared growth-input seam. **One code path:** the deep tier's attribute-growth contribution is dialed to zero at Stage 2, so the minimal form/fitness surface is the identity the deep tier extends — the #21 default-behaviour-neutral discipline, not a rewrite.

## 3. Dependencies
- **Upstream (needs):** #27 (player records to train), #30 (the day/week-advance loop that ticks training), #34 (coaching staff that modulate training effectiveness — routing seam, consumption deferred until #34 lands).
- **Downstream (consumers):** #28 (attribute growth input via the shared progression seam), #41 (injury-risk input).

## 4. Persistent state & save impact
Adds per-player training state (current focus, accumulated training-fatigue, form/fitness cursors) and the team training schedule to the world. Lands as an opaque, independently version-gated sub-blob per the `SeasonSaveCodec` pattern under #30's season save; the owning format-version bump is coordinated with #30. Every new field serialized and round-trip-covered.

## 5. Determinism
World tick only (`WorldClock`, one day = one `worldTick`). New RNG sub-stream under domain tag `0x21` / `SubsystemOrdinal` 83 for any stochastic training outcomes (growth jitter, incident-free variation); deterministic form/fitness deltas need no draw. Critically, training-fatigue is a **world-tick accumulator** distinct from the match engine's **match-tick** fatigue — the two never share a counter; the reconciliation (how a fatigued-in-training player enters a match) is a defined projection, not a coupling.

## 6. Key design decisions to resolve (the supplement must answer)
- **KD-1** What is the exact reconciliation between world-tick training fatigue and match-tick in-match fatigue — does training fatigue project into the match as a starting-fatigue offset, and how does a mid-week save→restore stay exact?
- **KD-2** What is the shared growth-input seam #29 writes and #28 reads, so attribute growth is single-owned by #28's curve (KD from #28)?
- **KD-3** How does the #34 coaching modulation attach as a routing seam that is identity (×1.0) until #34 lands, avoiding a phantom consumer (FR-LW-031)?
- **KD-4** Is training weekly or daily at rest — does the world-tick day loop batch a week, or accumulate daily, and how is the boundary serialized?
- **KD-5** How is the injury-risk output shaped so #41 consumes it without #29 owning the injury model?

## 7. Primary surfaces (proposed)
A per-day/per-week training step invoked by #30's loop (proposed); a training-focus command API for the UI (proposed); a training-fatigue accumulator distinct from match fatigue (proposed); the shared growth-input seam to #28 and the injury-risk output seam to #41 (proposed). Existing seams referenced: `WorldClock`, `WorldStore`, `WorldLoop`, #27's `PlayerAttributes`.

## 8. Test focus
Save→restore round-trip determinism across a mid-week boundary (training-fatigue accumulator lock); behaviour-neutral identity proof that Stage-2 (attribute-growth dial off) changes only form/fitness, never attributes; the fatigue-reconciliation test — a trained player's match-entry fatigue is the defined projection, not a double-count; two-run determinism from one seed; fail-loud gates on the training sub-blob version.

## 9. Open questions / risks
- The fatigue double-count trap is the headline risk: sharing any counter between world-tick and match-tick fatigue breaks determinism and realism both — the two must stay separate with a one-directional projection.
- The #28/#29 growth-seam boundary must be co-designed with #28 (same wave) or the two specs duplicate attribute mutation.
- #34 coaching is upstream but lands later; the routing-seam-as-identity pattern must be honored so #29 ships behaviour-neutral before staff exist.

## Version History
| Version | Date | Change |
|---------|------|--------|
| v0.1 | July 22, 2026 | Initial high-level plan. |
