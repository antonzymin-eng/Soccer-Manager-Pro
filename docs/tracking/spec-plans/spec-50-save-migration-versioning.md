# Spec #50 — Save Migration & Versioning — High-Level Plan

> **Created:** July 22, 2026
> **Status:** PLAN (pre-design-supplement — no section files, no `SPEC_INDEX.md` row). Candidate spec number **#50** (proposed in `management-layer-spec-roadmap.md`, not reserved).
> **Master-plan home:** §4.6 · **Tier:** S2 · **Wave:** 8 · **FR prefix (proposed):** FR-MG
> **Determinism:** infra — none (no RNG stream, no domain tag; a migration/process contract, not sim code)
> **Purpose:** The forward-migration contract that lets a live player save from a shipped build open in a later shipped build across `SEASON_SAVE_FORMAT_VERSION` / `WORLD_STORE_FORMAT_VERSION` bumps.

## 1. Scope
The contract for migrating **live player saves across shipped game updates**: how a save written by build v(N) opens in build v(N+1) when the format version has bumped. This is distinct from the determinism format versions themselves — those (`SEASON_SAVE_FORMAT_VERSION`, `WORLD_STORE_FORMAT_VERSION`, and the inner match/snapshot versions) currently gate **corruption** (fail-loud on a version mismatch), which is the opposite of forward-migration. #50 defines the upgrade path that turns a hard version-mismatch rejection into a supported v(N)→v(N+1) transform. **Out of scope:** the save formats themselves (owned by #30/season-save/world-store — #50 migrates across their bumps, it does not define them); corruption detection (already the codecs' fail-loud job); Steam Cloud distribution/conflict (that is #39, which #50 pairs with). #50 owns the migration transform, not the wire format.

## 2. Staging (minimal-first → deep)
Minimal identity = the current single-version world where no migration is needed: a v(N) save opens in a v(N) build unchanged (identity migration = no-op). The deep tier adds an ordered chain of per-bump forward transforms so a v(N) save is upgraded step-by-step to the current version on load; the no-op identity is the base case of that same chain (a same-version load runs zero transforms). One migration-pipeline code path, entered on every load, that is a no-op until a bump exists.

## 3. Dependencies
- **Upstream (needs):** #30 season loop / `SeasonSave` (`SEASON_SAVE_FORMAT_VERSION`) and the world store (`WORLD_STORE_FORMAT_VERSION`) — the version constants and sub-blob structure it migrates across; each management spec that bumps a version supplies its per-bump transform.
- **Downstream (consumers):** #39 Steam Packaging & Release (Cloud save versioning/conflict resolution is specified against this migration contract). #30's load path invokes the pipeline.

## 4. Persistent state & save impact
No new game-state block of its own — #50 defines behaviour **over** the existing save versions, not new persisted fields (it may record a save's origin build/version if not already present). It does not itself bump a format version; it consumes each bump the other specs make. The migration transform reads a v(N) blob and writes a v(N+1)-shaped one, respecting the opaque-sub-blob discipline (`SeasonSaveCodec` never parses sub-blobs — a migration touches only the block whose version bumped).

## 5. Determinism
Infra — no RNG stream, no domain tag. Migration is a deterministic structural transform over serialized bytes (no simulation, no draw); the same v(N) save always migrates to the same v(N+1) bytes. It is process/infra, not sim code — consistent with the roadmap §6 classification (migration is not a determinism domain, it operates on the save the determinism system produces). It is emphatically distinct from the determinism format versions, which gate corruption; #50 adds forward-migration on top.

## 6. Key design decisions to resolve (the supplement must answer)
- **KD-1 (migration ≠ corruption gate — load-bearing):** forward-migration is distinct from the determinism format versions (which gate corruption, not migration). How does the load path distinguish "old-but-migratable version" from "corrupt/unknown version", and where does that decision live relative to the codecs' existing fail-loud version checks?
- **KD-2:** the transform-chain model — an ordered set of per-bump v(N)→v(N+1) migrations composed on load, so any old save reaches current. Who owns each step (the spec that made the bump) and how are they registered?
- **KD-3:** sub-blob granularity — a migration touches only the block whose version bumped (season vs. world-store vs. inner match/snapshot), preserving the `SeasonSaveCodec` never-parse-other-sub-blobs pattern. How is a per-sub-blob version tracked?
- **KD-4:** unmigratable / too-old / newer-than-build saves — refusal policy (fail-loud, never silent data loss) and user-facing messaging (routed through #49).
- **KD-5:** interaction with #39 Steam Cloud conflict resolution — which version wins, and is a migrated save written back or kept alongside the original?

## 7. Primary surfaces (proposed)
- A migration pipeline (proposed) invoked on load, composing an ordered chain of per-bump transforms.
- A per-bump migration step contract (proposed) owned by the spec that made the bump.
- A version-classification seam (proposed) distinguishing migratable / current / corrupt / too-new, sitting in front of the codecs' fail-loud checks.
- A refusal + messaging path (proposed) for unmigratable saves — routed through #49 for user-facing text.

## 8. Test focus
Round-trip after migration: a v(N) save migrated to v(N+1) loads into a state that then satisfies the standard snapshot round-trip determinism contract (migrated save advances == a native v(N+1) save of the same career). Deterministic transform: the same v(N) bytes always migrate to the same v(N+1) bytes. Fail-loud gates: corrupt/unknown/too-new versions are refused (never silently "migrated"); no silent data loss. Identity: a same-version load runs zero transforms and is byte-unchanged. Sub-blob isolation: a bump to one block does not require parsing/rewriting the others.

## 9. Open questions / risks
- The migration/corruption boundary (KD-1) is the subtlest risk: mis-classifying a corrupt save as migratable corrupts a career; mis-classifying an old save as corrupt loses it. The classification seam must be conservative and fail-loud.
- Ownership of per-bump steps (KD-2): if the migration lives in #50 rather than the bumping spec, #50 becomes a bottleneck that lags every format change — steps should be contributed by the spec that bumps.
- Long chains: many accumulated bumps across shipped updates make the composed transform long and hard to test end-to-end; needs a golden-save corpus per shipped version.
- Wave 8 / needs a genuinely shippable, updated build to be meaningful — do not front-load; but publish the version-classification + step-registration contract early so bumping specs can supply their transforms as they land.

## Version History
| Version | Date | Change |
|---------|------|--------|
| v0.1 | July 22, 2026 | Initial high-level plan. |
