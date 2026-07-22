# Spec #28 — Player Progression & Lifecycle — High-Level Plan

> **Created:** July 22, 2026
> **Status:** PLAN (pre-design-supplement — no section files, no `SPEC_INDEX.md` row). Candidate spec number **#28** (proposed in `management-layer-spec-roadmap.md`, not reserved).
> **Master-plan home:** §4.3 (aging) / §5 (youth) · **Tier:** Stage 2 min → Stage 3 deep · **Wave:** 2 · **FR prefix (proposed):** FR-PG
> **Determinism:** domain tag `0x20` / `SubsystemOrdinal` 82 (proposed — to pin at promotion).
> **Purpose:** Aging, decline, retirement, regens/newgens, and attribute CA/PA growth over #27's canonical record, advanced on the world tick.

## 1. Scope
Player lifecycle on the **world tick** (`WorldClock`, one day = one `worldTick`, never the match tick): aging, attribute decline, retirement, and regen/newgen production, plus attribute growth via a current/potential-ability (CA/PA) model over the #27 canonical record. **Out of scope:** the youth-academy *structure* (facilities → intake quality) is #42; training-driven growth *input* is #29 (shared seam, not duplicated); valuations are #31. Regens/newgens reference clubs/nations from #27's roster world.

## 2. Staging (minimal-first → deep)
Stage-2 minimal is the master plan's literal §4.3 rule — >30 → −1/yr, <24 → +1/yr, retire at 36 — expressed as a **deterministic per-day projection** (a fractional daily delta accumulating to the yearly step, so mid-season restore is exact). Stage-3 deep replaces the flat step with per-attribute CA/PA growth-decline curves keyed to age, position, and (via #29) training. Both tiers are **one code path with a config dial**: the deep curve reduces to the literal step when the curve dial is off — the minimal surface is the identity the deep tier modulates, not a throwaway.

## 3. Dependencies
- **Upstream (needs):** #27 (canonical attribute record + CA/PA fields), #30 (the day-advance loop that ticks it forward).
- **Downstream (consumers):** #42 youth academy (shares the regen/generation machinery), #31 valuations (age/PA feed price), #29 training (growth is #28's output, training is its input).

## 4. Persistent state & save impact
Adds per-player lifecycle state to the world (CA/PA, growth cursor, retirement flag, accumulated daily fractional deltas) plus a regen production cursor. Lands as an opaque, independently version-gated sub-blob per the `SeasonSaveCodec` pattern under #30's season save (or `WORLD_STORE_FORMAT_VERSION` if the roster world owns it) — the owning bump is decided with #27/#30. Every new field must be serialized and round-trip-determinism-covered.

## 5. Determinism
World tick only. New RNG sub-stream under domain tag `0x20` / `SubsystemOrdinal` 82 in the off-pitch band, feeding regen attribute/potential draws and any stochastic growth jitter. Aging/decline of existing players is a deterministic projection (no draw); only generation and jitter draw. Draw sites are pinned APPEND-only so replay parity holds across fail-loud paths.

## 6. Key design decisions to resolve (the supplement must answer)
- **KD-1** How is the yearly ±1 step decomposed into a per-day fractional projection so a mid-year save→restore lands byte-identically (no double-count on the day the step crosses)?
- **KD-2** What is the shared progression seam #28 and #29 both write to, so training growth is an *input to* #28's curve rather than a parallel mutation of the same attributes?
- **KD-3** How do regens reference club/nation from #27's roster world, produced day-deterministically, without #28 owning roster identity (which is #27's)?
- **KD-4** Where does the CA/PA model live relative to #27's record — new fields on the canonical record (needs #27 buy-in) or a #28-owned parallel per-player block?
- **KD-5** What is the retirement → roster-removal contract, and how does it reconcile with an in-progress season's fixtures/selection?

## 7. Primary surfaces (proposed)
A per-day progression step invoked by #30's day-advance loop (proposed); a CA/PA growth model over the #27 record (proposed); a regen/newgen generator reusing #27's `RosterGenerator`-class draw machinery (proposed); a shared growth-input seam consumed by #29 (proposed). Existing seams referenced: `WorldClock`, `WorldStore`, `WorldLoop`, #27's `PlayerAttributes` / `RosterGenerator`.

## 8. Test focus
Save→restore round-trip determinism across a mid-year boundary (the fractional-step KD-1 lock); two-run determinism of a multi-season aging projection from one seed; behaviour-neutral identity proof that the deep curve with the dial off reproduces the literal §4.3 step exactly; regen determinism (same seed → same newgen); fail-loud gates on the lifecycle sub-blob version.

## 9. Open questions / risks
- The #28/#29 shared-seam boundary is the main architectural risk — building training growth as a separate mutation would double-count and break the "one code path" invariant.
- CA/PA field ownership straddles #27 and #28; deciding it late forces a #27 record change (schema-version ripple).
- Regen volume × world size is a save-size concern the master plan flags — the retirement/generation balance must not grow the world-state blob unboundedly.

## Version History
| Version | Date | Change |
|---------|------|--------|
| v0.1 | July 22, 2026 | Initial high-level plan. |
