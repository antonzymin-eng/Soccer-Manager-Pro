# Spec #27 — Squad / Player Data Layer — High-Level Plan

> **Created:** July 22, 2026
> **Status:** PLAN (pre-design-supplement — no section files, no `SPEC_INDEX.md` row). Candidate spec number **#27** (proposed in `management-layer-spec-roadmap.md`, not reserved).
> **Master-plan home:** §4.2 · **Tier:** Stage-1 pull · **Wave:** 0 (Foundation) · **FR prefix (proposed):** FR-SQ
> **Determinism:** domain tag `0x1F` / `SubsystemOrdinal` 81 — ALREADY allocated (`src/deterministic-sim/SubsystemOrdinals.cs`, `PlayerDatabase = 81`).
> **Purpose:** Promote the existing squad-player data supplement + `src/player-database` implementation to a numbered spec — canonical player-attribute record, deterministic rosters, text import — replacing all-neutral synthetic seeding.

## 1. Scope
This is a **promotion**, not a greenfield authoring: a design supplement (`docs/tracking/squad-player-data-design.md`) and a partial `src/player-database/` assembly already exist, and MatchEngine attribute wiring (T1/T2) plus lineup selection have landed. This plan governs turning that supplement into a formally numbered spec — registering the #27 row in `SPEC_INDEX.md`, closing `ERR-007` (the long-open engine-side attribute-proxy gap the T1 projections already resolved) formally, and pinning the canonical `PlayerAttributes` / `Squad` / `RosterGenerator` / `SquadFileLoader` surface as spec text. **Out of scope:** on-disk save-format squad persistence, transfer market, aging/progression (those are #28/#30/#31), and the per-spec GK (#11) / Heading (#10) projections deferred until those specs are engine-wired.

## 2. Staging (minimal-first → deep)
The Stage-0/1 layer is already the identity: `PlayerAttributes.CreateDefault()` (all-neutral 10) is the behaviour-neutral baseline, and `ConfigureSquads` + `LineupSelector` project distinct rosters through the same code path a neutral match uses. The spec formalizes that the deeper tiers (CA/PA growth #28, masking views #32, aging #28) modulate this record rather than replace it — one canonical record, many read-only projections. No rewrite: the deep-tier consumers key off the same 31-field record already shipped.

## 3. Dependencies
- **Upstream (needs):** Deterministic Simulation #16 only (RNG service, domain-tag/ordinal allocation, canonical serializer).
- **Downstream (consumers):** #28 progression, #29 training, #31 transfers, #32 scouting, #36 national teams, #42 youth academy, and the match-engine attribute projection (`PlayerAttributeProjection`, already consuming).

## 4. Persistent state & save impact
Today the roster is a boot-time/in-code + text-import artifact, not part of the season save. The spec must decide the persistence contract: when #30 owns the season save, the canonical roster world becomes a durable sub-blob. As a promotion pass this bumps **no** format version by itself — persistence lands with #30. If #27 chooses to persist rosters ahead of #30, it lands as an opaque, independently version-gated sub-blob per the `SeasonSaveCodec` pattern.

## 5. Determinism
`RosterGenerator` already draws from a dedicated sub-stream under domain tag `0x1F` / `SubsystemOrdinal` 81 (allocated July 15, 2026). Generation is a pure function of the world seed + club/nation key — deterministic per-day where invoked by the world tick. The spec pins these draw sites and the APPEND-only attribute/name catalogue ordering as the wire-stable surface.

## 6. Key design decisions to resolve (the supplement must answer)
- **KD-1** Exactly which registry/back-prop actions does promotion entail — does closing `ERR-007` formally require re-tagging the `[TEMPORARY-PROXY-ERR-007]` fields in the per-spec attribute structs, or only recording that the engine-side proxies are now computed from real attributes?
- **KD-2** Where does the canonical roster world live at rest — inside #30's season sub-blob, or a standalone `WORLD_STORE_FORMAT_VERSION`-gated block — and who owns the bump?
- **KD-3** How do the still-deferred GK (#11) / Heading (#10) projections attach without a phantom consumer, given those specs are not engine-wired yet (KD-P8)?
- **KD-4** Does the `PlayerDatabase.PlayerAttributes` vs `AgentMovement.PlayerAttributes` bare-name collision get resolved at promotion (namespace/rename) or stay a documented CS0104 hazard?

## 7. Primary surfaces (proposed)
Existing and already-shipped (referenced, not fabricated): `PlayerAttributes`, `PlayerRecord`, `Squad`, `RosterGenerator`, `SquadFileLoader` (all in `src/player-database/`), and the match-engine `PlayerAttributeProjection` / `LineupSelector` consumers. New spec surfaces are formalizations of these; any additional persistence seam is marked **(proposed)** pending #30.

## 8. Test focus
The suite largely exists (`PlayerAttributeProjectionTests`, `MatchEngineSquadTests`, `LineupSelectorTests`, roster-generator determinism). Promotion adds: default-path digest byte-identity lock (already green via KD-P7), two-run roster-generation determinism, `SquadFileLoader` round-trip, and — if #27 persists rosters — save→restore round-trip field-identity for the roster sub-blob with fail-loud version/tag gates.

## 9. Open questions / risks
- Promotion is mostly documentation + registry work, but ERR-007 formal closure touches multiple approved specs' attribute structs — a renumbering-cascade-class hazard if the back-props are not filed atomically.
- Persisting rosters ahead of #30 risks a format-version ordering conflict with the season loop's own save extension; safest to defer persistence to #30.
- The bare-name CS0104 collision will bite the first spec that wires both `PlayerAttributes` types into one assembly; promotion is the natural moment to decide it.

## Version History
| Version | Date | Change |
|---------|------|--------|
| v0.1 | July 22, 2026 | Initial high-level plan. |
