# Spec #34 — Staff & Backroom — High-Level Plan

> **Created:** July 22, 2026
> **Status:** PLAN (pre-design-supplement — no section files, no `SPEC_INDEX.md` row). Candidate spec number **#34** (proposed in `management-layer-spec-roadmap.md`, not reserved).
> **Master-plan home:** §5 Stage 3 · **Tier:** S3 · **Wave:** 4 · **FR prefix (proposed):** FR-ST
> **Determinism:** domain tag `0x26` / SubsystemOrdinal 88 (proposed off-pitch block, §6 — pinned only at promotion)
> **Purpose:** Coaches, scouts, and physios as attributed entities that modulate coaching, scouting, and injury/development outcomes.

## 1. Scope
Staff (coaches, scouts, physios, and their roles/skills) modelled as **entities with attributes** — reusing #27's canonical record shape where it fits — that feed multiplier/quality inputs into #29 (coaching effect on training), #32 (scout judgement quality), and #41/#28 (medical modulation of injury recovery and development). Staff hiring is a transfer-market analogue: a candidate pool, an offer, an accept/reject, and a wage cost against #40. **Out of scope:** the training/scouting/injury/progression models themselves (owned by #29/#32/#41/#28 — #34 supplies only the staff-quality input seam); staff personality depth beyond a skill vector (defer to #33 shape reuse).

## 2. Staging (minimal-first → deep)
Minimal identity = every staff slot filled by a **neutral house baseline** whose modulation multipliers are exactly ×1.0 — i.e. #29/#32/#41 behave as they do today with no staff wired. The deep tier populates real staff with attribute-derived multipliers off that same one code path (a distinct-staff club diverges from the neutral baseline the way a distinct squad diverges from the all-neutral roster in #27). One projection function, config-dialled.

## 3. Dependencies
- **Upstream (needs):** #27 (record shape / attribute vocabulary), #33 (staff judgement/personality quality, at S3+), #40 (staff wage as a budget constraint), #30 (day-advance loop; hiring windows).
- **Downstream (consumers):** #29 (coaching multiplier), #32 (scout accuracy/coverage), #41 + #28 (physio/medical modulation of recovery and growth), #42 (academy coaching → intake quality).

## 4. Persistent state & save impact
New per-club staff roster (staff entities + role assignments + contracts) — a new world-state block. Bumps `WORLD_STORE_FORMAT_VERSION` (staff live in the persistent world alongside rosters), landing as an opaque, independently version-gated sub-blob per the `SeasonSaveCodec`/`WorldStateSerializer` never-parse-sub-blobs pattern. Round-trip determinism test required for the staff block.

## 5. Determinism
World tick (`WorldClock`) for hiring/candidate-pool generation. Dedicated RNG sub-stream (domain tag `0x26` / `SubsystemOrdinals` 88, proposed) for candidate-pool generation and any stochastic negotiation outcome — allocated in #16 §3.4 + `SubsystemOrdinals` at promotion, not now. Staff attribute → multiplier projections are pure (no draw).

## 6. Key design decisions to resolve (the supplement must answer)
- **KD-1** Reuse #31's negotiation machinery for hiring, or a parallel lightweight path? (Roadmap §3.34 flags reuse-vs-parallel as the load-bearing decision — staff hiring shares "candidate + offer + accept/reject + wage" shape with #31.)
- **KD-2** How much of #27's `PlayerRecord`/`PlayerAttributes` shape is reusable for a staff record vs. a distinct staff-attribute vector? Where is the boundary?
- **KD-3** Multiplier convention: does each staff role expose a single scalar per consumer (#29/#32/#41), or a richer per-facet vector? Neutral baseline must be exactly ×1.0.
- **KD-4** Candidate-pool generation — does it reuse #28/#42's regen/generation machinery keyed to club/nation, or its own generator?
- **KD-5** Is the human baseline (unfilled slot) a real neutral staff entity or an absence sentinel the consumers special-case? (Affects behaviour-neutral identity proof.)

## 7. Primary surfaces (proposed)
- `StaffRecord` / `StaffAttributes` (proposed) — reusing or paralleling #27 record shape (KD-2).
- Per-club `StaffRoster` block in the world store (proposed).
- Staff-quality projection functions (proposed) consumed by #29/#32/#41 — pure, neutral = ×1.0.
- A hiring API (proposed) — either delegating to #31 negotiation or a parallel `HireStaff`-style seam (KD-1).

## 8. Test focus
Behaviour-neutral identity: an unstaffed (neutral-baseline) club is byte-identical to the pre-#34 pipeline through #29/#32/#41. Round-trip determinism of the staff block through `WorldStore.Snapshot`/`Restore`. Two-run determinism of candidate-pool generation from a fixed world seed. Fail-loud gates on invalid role assignment / over-budget hire / malformed staff record.

## 9. Open questions / risks
- Reuse-vs-parallel with #31 (KD-1) is the biggest scoping fork; a wrong call duplicates negotiation logic.
- Ordering: #34 must precede #32 (scouts are staff) and land alongside/after #33 for judgement quality — authoring #32 first would create a phantom staff dependency.
- Multiplier composition risk: staff modulation must not double-count with #33 morale or #40 facility effects reaching the same consumer.

## Version History
| Version | Date | Change |
|---------|------|--------|
| v0.1 | July 22, 2026 | Initial high-level plan. |
