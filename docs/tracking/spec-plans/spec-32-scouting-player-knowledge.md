# Spec #32 — Scouting & Player Knowledge — High-Level Plan

> **Created:** July 22, 2026
> **Status:** **PROMOTED** (July 24, 2026) — design supplement `docs/tracking/scouting-player-knowledge-design.md` (AR-converged v0.3) → 11-file section set at `docs/specs/scouting-player-knowledge/` → **APPROVED**; `SPEC_INDEX.md` row 32 added. Two plan positions were revised at supplement stage with recorded rationale: §4's `WORLD_STORE_FORMAT_VERSION` bump → a `SCOUTING_SAVE_FORMAT_VERSION` season-save sub-blob (KD-6), and §7's optional #22 `InteractionTextGenerator` report prose → rejected (structured reports only, the #49 boundary). (Original PLAN status follows for history.) PLAN (pre-design-supplement — no section files, no `SPEC_INDEX.md` row). Candidate spec number **#32** (proposed in `management-layer-spec-roadmap.md`, not reserved).
> **Master-plan home:** §5 recruitment · **Tier:** S3 · **Wave:** 4 · **FR prefix (proposed):** FR-SC
> **Determinism:** domain tag `0x24` / SubsystemOrdinal 86 (proposed off-pitch block, §6 — pinned only at promotion)
> **Purpose:** Scout assignments, attribute masking / fog-of-war, reports, and recommendations — a per-manager knowledge view over true attributes.

## 1. Scope
Scout assignments, **attribute masking / fog-of-war** (the manager sees attribute *ranges*, not truths, until a player is scouted), scout reports, and recommendations. Knowledge accuracy sharpens with scouting effort and scout quality. **Out of scope — the roadmap §5 invariant this spec exists to honour:** knowledge is a **per-manager VIEW over #27's true attributes and is NEVER a mutation of them**; #32 stores a knowledge overlay, the canonical `PlayerAttributes` are untouched. Also out of scope: the scout entities themselves (#34 supplies scouts as staff), the transfer decision (#31 consumes recommendations), and the report-rendering UI (#38).

## 2. Staging (minimal-first → deep)
Minimal identity = an **omniscient view** — every attribute known exactly (full-accuracy overlay), so recruitment/UI behave as if fog-of-war is off. The deep tier narrows that overlay into ranges that tighten with scouting, **modulating the same view seam** (the fully-known overlay is the identity the masking model narrows), one code path. A player with maximal scouting knowledge collapses back to the exact-value identity.

## 3. Dependencies
- **Upstream (needs):** #27 (true canonical `PlayerAttributes` the view reads read-only), #30 (day-advance loop; scout-assignment progress over time), #34 (scouts are staff — scout quality/coverage drives accuracy), #33 (scout judgement as a personality/skill, at S3+).
- **Downstream (consumers):** #31 (transfer decisions read the knowledge view, not true attributes), #38 (UI renders ranges/reports).

## 4. Persistent state & save impact
New per-manager knowledge overlay (per-player known-range / confidence + assignment state + reports). **A per-manager view, not shared world truth** — it persists in the manager-scoped world store the way `ActiveSetMembership` / the `world.text` cursor already ride the `WorldStore` composite. Bumps `WORLD_STORE_FORMAT_VERSION`, landing as an opaque, independently version-gated sub-blob per the `SeasonSaveCodec`/`WorldStateSerializer` pattern. Overlay + assignment cursors serialized and round-trip-covered.

## 5. Determinism
World tick (`WorldClock`) drives scouting progress (never the match tick). Dedicated RNG sub-stream (domain tag `0x24` / `SubsystemOrdinals` 86, proposed) for **scouting accuracy draws** — the noise that makes a report's estimated range deviate from truth by scout-quality-scaled error, deterministic and replayable across `Snapshot`/`Restore` (the class of the living-world `world.text` aperiodic stream). Allocation pinned in #16 §3.4 at promotion.

## 6. Key design decisions to resolve (the supplement must answer)
- **KD-1** Overlay representation: per-attribute [min,max] range + confidence, or a sampled point-estimate + error band? How does maximal knowledge collapse to the exact-value identity?
- **KD-2** The view boundary — the enforcement that #32 never writes #27's true attributes (roadmap §5 invariant): read-only accessor discipline, and where #31/#38 are prevented from reaching around the overlay to true state.
- **KD-3** Accuracy draw ownership: one draw per scouting report vs. a persistent per-player-per-manager error seed — which keeps determinism-safe across saves and repeated views without re-rolling?
- **KD-4** Scout-quality (#34) → accuracy/coverage/speed mapping, neutral scout = baseline; and #33 judgement's role at S3+.
- **KD-5** Does #32 reuse #31's negotiation/search surface for shortlist/recommendation, or is search its own read-only query over the (masked) pool?

## 7. Primary surfaces (proposed)
- Per-manager `PlayerKnowledge` overlay block (proposed) — read-only view accessor over #27 truth.
- A scout-assignment API (proposed) driven by the #30 day-advance loop and #38 UI command seam.
- A masked attribute-view surface (proposed) consumed by #31 and #38 — the only attribute surface those layers see for un-fully-scouted players.
- Report/recommendation generation (proposed) — optionally a consumer of #22's `InteractionTextGenerator` for report prose.

## 8. Test focus
**View-not-mutation lock:** scouting a player leaves #27's canonical `PlayerAttributes` byte-identical (the load-bearing invariant test). Behaviour-neutral identity: the full-accuracy overlay reproduces exact-value recruitment/UI behaviour. Round-trip determinism of the knowledge overlay through `WorldStore.Snapshot`/`Restore`. Two-run determinism of scouting accuracy from a fixed world seed. Fail-loud gates on a knowledge query for a player absent from the pool / malformed overlay / assignment to a non-existent scout.

## 9. Open questions / risks
- The view-not-mutation invariant (KD-2) is the recurring trap the roadmap §5 calls out by name — any accidental write of true attributes is a correctness failure, not a bug-of-degree.
- Ordering: #34 (scouts as staff) and #33 (judgement) must precede #32 (roadmap §7 Wave 4) — authoring #32 first phantoms both.
- Accuracy-draw seeding (KD-3): re-rolling on every view breaks determinism; a persistent per-pair seed must serialize without bloating save size across a large pool.

## Version History
| Version | Date | Change |
|---------|------|--------|
| v0.1 | July 22, 2026 | Initial high-level plan. |
| v0.2 | July 24, 2026 | PROMOTED — supplement → section files → APPROVED (SPEC_INDEX row 32); status header updated with the two supplement-stage revisions (season-save sub-blob, not a WorldStore bump; structured reports, no #22 prose). |
