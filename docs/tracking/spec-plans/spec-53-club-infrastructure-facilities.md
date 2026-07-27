# Spec #53 — Club Infrastructure & Facilities — High-Level Plan

> **Created:** July 26, 2026
> **Status:** PLAN (pre-design-supplement — no section files, no `SPEC_INDEX.md` row). Candidate spec number **#53** (proposed in `../management-layer-spec-roadmap.md` v0.6, not reserved). **A converged design supplement already exists** — `../club-infrastructure-facilities-design.md` v0.4 — because the gap was found while authoring Wave 8; this plan is written to complete the record, and the supplement is authoritative where they differ.
> **Master-plan home:** §5 Stage 3 *"Infrastructure upgrades (training ground, stadium)"* · **Tier:** S3 · **Wave:** 5 (late — after its consumers) · **FR prefix (proposed):** FR-IN
> **Determinism:** **none** — draw-free; no RNG stream, no domain tag, no `SubsystemOrdinal`; consumes none of the roadmap §6 reserved slack.
> **Purpose:** Own the per-club facility model (levels + upgrade lifecycle) that #34, #42 and #28 already consume and mis-attribute to #40.

## 1. Scope
Per-club facility **levels** for a fixed, APPEND-only roster — `TrainingGround`, `YouthFacilities`, `MedicalCentre`, `Stadium` (capacity) — plus the **upgrade lifecycle** on the world tick and the **projections** into value-input dials that already exist in #42, #29 and #41 (and #40's deferred matchday model). **Out of scope:** money (#40), who decides to spend (the command layer), staff quality (#34), each consumer's own response curve, and the stadium as a rendered place (#48).

## 2. Staging (minimal-first → deep)
Minimal = every facility at baseline, no upgrades: each projection equals its consumer's existing `Neutral`, so minimal #53 is **behaviourally indistinguishable from #53 not existing**. S3 = the upgrade lifecycle, costs through #40, non-neutral projections. Deep = maintenance/decay, capacity economics, reputation effects.

## 3. Dependencies
- **Upstream (needs):** #40 (funding, via the command layer — never a reference), #30 (a day-advance tick-order slot).
- **Downstream (consumers):** #42 `AcademyQuality`, #29 `TrainingInput`, #41's recovery input, #40's deferred matchday attendance. All are **existing dials**; #53 adds none.

## 4. Persistent state & save impact
Per-club `{ levels, at most one in-progress build }` in a `FACILITY_SAVE_FORMAT_VERSION`-gated opaque sub-blob (the convention `SeasonSaveCodec` composes without parsing). Integer-only. This is the **twenty-sixth** format version, adding a row to #50's registry — accepted as the cost of the ownership model.

## 5. Determinism
Draw-free at every planned tier: an upgrade completes on a **stored world-day** (a pure clock comparison, restore-safe by construction), a level is an integer, a projection is a table lookup. **Uniform genesis** keeps #53 outside `WORLD_GENERATION_VERSION` — a seed-varied baseline would make genesis a generation concern and is recorded as a deep-tier option with that consequence attached.

## 6. Key design decisions (resolved in the supplement)
- **KD-1** #53 owns levels, #40 owns money; the command layer sequences **check → debit → latch**, which is why the surface is split into a pure `CanStartUpgrade` and a mutating `StartUpgrade`.
- **KD-2** Fixed APPEND-only roster, one member per **existing** consumer dial; no speculative members.
- **KD-3** Upgrades store a completion day, not a countdown.
- **KD-4** Projections are value inputs assembled by the root, one seam per consumer (the #34 "no second source" rule).
- **KD-5/KD-6** Own sub-blob; draw-free, so the reserved `0x2E`–`0x2F` slack is untouched.

## 7. Primary surfaces (proposed)
`FacilityType` (enum); `ClubFacilities` + `LevelOf`; `CanStartUpgrade` / `StartUpgrade`; `AdvanceFacilityDay`; the four projections; `FacilitySaveCodec`. All proposed — none exists.

## 8. Test focus
Baseline identity (every projection exactly `Neutral`; a career with #53 present is byte-identical to one without); refused build leaves the balance untouched, and `CanStartUpgrade` leaves state byte-identical; a build in progress across save/restore completes on the same world day and is not double-advanced; facility projections independent of staff state (no double-counting); genesis uniformity across seeds; sub-blob round-trip + `FacilityType` ordinal stability.

## 9. Open questions / risks
- The mis-attribution is the reason to act: four approved specs point at #40 for a model its scope excludes, and each will otherwise improvise (the parallel-surface trap).
- A twenty-sixth format version feeds #50's registry-bookkeeping risk.
- Scope creep toward club operations (ticket pricing, naming rights) belongs to #40/#45.
- The draw-free commitment is load-bearing for the determinism block's remaining slack.

## Version History
| Version | Date | Change |
|---------|------|--------|
| v0.1 | July 26, 2026 | Initial plan, written alongside the converged supplement v0.4 (which is authoritative). |
