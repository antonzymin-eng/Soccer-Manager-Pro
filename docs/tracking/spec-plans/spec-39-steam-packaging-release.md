# Spec #39 — Steam Packaging & Release Engineering — High-Level Plan

> **Created:** July 22, 2026
> **Status:** PLAN (pre-design-supplement — no section files, no `SPEC_INDEX.md` row). Candidate spec number **#39** (proposed in `management-layer-spec-roadmap.md`, not reserved).
> **Master-plan home:** §4.6 / §4.7 release · **Tier:** Stage 2 · **Wave:** 8 (LAST) · **FR prefix (proposed):** FR-PK
> **Determinism:** infra / process — none (no RNG stream, no domain tag; read-only over career/season events; build-determinism is a certification concern, not a sim stream).
> **Purpose:** The build/packaging pipeline, store-page asset checklist, achievements (read-only over career/season events), Steam Cloud save (versioning + conflict resolution against `SEASON_SAVE_FORMAT_VERSION`), and the release QA/certification gate — authored last against a shippable build.

## 1. Scope
Release engineering: the deterministic build/packaging pipeline, the store-page asset checklist, achievement definitions (read-only consumers of career/season events), Steam Cloud save integration bound to the game's save format, and the release QA/certification gate. This is **mostly process and checklist, not sim code** — it is authored last, against a genuinely shippable build. **Out of scope:** the save format itself (#30 owns `SeasonSaveManager`/`SEASON_SAVE_FORMAT_VERSION`; #39 packages it), the forward-migration contract (#50 owns cross-version save migration; #39 depends on it for Cloud conflict resolution), and any sim/gameplay behaviour.

## 2. Staging (minimal-first → deep)
Stage-2 minimal = a reproducible build + a single-slot Steam Cloud save round-trip + a small achievement set + a cert-QA checklist. Deeper release passes add more achievements, richer store assets, and multi-slot/branch Cloud handling. Because #39 is infra, the "identity" discipline maps to keeping the minimal pipeline the one code path that later releases extend — the cert gate and Cloud-conflict contract are stable from the first shippable build, not rewritten per release.

## 3. Dependencies
- **Upstream (needs):** a genuinely shippable build (every gameplay/UI wave landed), #30's save format (`SeasonSaveManager` / `SeasonSaveCodec` / `SeasonSaveConstants.SEASON_SAVE_FORMAT_VERSION`) — what Steam Cloud syncs, #50 (save migration — the forward-migration contract Cloud conflict resolution relies on), the pinned certification platform (`docs/tracking/certification-platform.md`) and the determinism cert-run machinery (the build must be reproducible/certified the way the match engine already is).
- **Downstream (consumers):** none — #39 is the terminal spec on the critical path (#27 → #30 → #33 → #31 → #38 → #39).

## 4. Persistent state & save impact
Adds **no new sim persistent state**. Steam Cloud save is a distribution concern *over* #30's existing save file — #39 specifies its sync, versioning, and conflict-resolution rules against `SEASON_SAVE_FORMAT_VERSION` / `WORLD_STORE_FORMAT_VERSION`, but never changes the codec or bumps a format version. Achievement progress may need a small local persistence surface (a design decision — KD-3), kept outside the determinism-gated save blobs.

## 5. Determinism
Infra / process — **no RNG stream, no domain tag, no `SubsystemOrdinals` entry** (roadmap §6 lists #39 among the infra specs). The determinism relevance is *build* determinism (a reproducible, certifiable binary — the existing `certification-platform.md` pin + cert-run runbook precedent) and the guarantee that Cloud sync/conflict resolution never corrupts a save. Achievements are a pure read-only consumer of career/season events (the `match-viewer`/analytics observational precedent) and add no stochastic surface.

## 6. Key design decisions to resolve (the supplement must answer)
- **KD-1** What is the Steam Cloud conflict-resolution contract against `SEASON_SAVE_FORMAT_VERSION` — how does a save synced from a v(N) build open (or refuse to open) in a v(N+1) build, and how does this hand off to #50's migration contract?
- **KD-2** What is the build-determinism / cert-QA gate — how does the release pipeline reuse the existing pinned-host certification machinery (determinism KAT + perf baseline) as a ship gate?
- **KD-3** Where does achievement progress persist, and how are achievements defined as pure read-only derivations over career/season events (never a sim mutation)?
- **KD-4** What is the store-page asset checklist scope, and which items are hard release-gate blockers vs. soft?
- **KD-5** How does Cloud sync interact with an in-progress match blob inside the season save (the optional `matchPresent` blob) — sync mid-match saves, or gate on a between-fixtures boundary?

## 7. Primary surfaces (proposed)
A build/packaging pipeline definition (proposed, process); a Steam Cloud sync + conflict-resolution policy over the season save file (proposed); an achievement-definition set as read-only event consumers (proposed); a release QA/cert checklist reusing the determinism cert-run gate (proposed). Existing seams referenced: `SeasonSaveManager`, `SeasonSaveCodec`, `SeasonSaveConstants.SEASON_SAVE_FORMAT_VERSION`, `certification-platform.md` / the cert-run runbook, career/season event surfaces from #30.

## 8. Test focus
Steam Cloud save round-trip integrity (a synced save opens byte-identical); conflict-resolution correctness against a version-mismatched pair; the release cert gate as a hard fail-loud (an uncertified/non-reproducible build cannot ship); achievements verified read-only (observing events does not alter determinism); the `matchPresent` mid-match blob sync boundary.

## 9. Open questions / risks
- #39 is genuinely last — it must not be front-loaded (roadmap §3 KD), so its main risk is being scoped before the build is shippable; the checklist should be drafted but the QA gate only exercised against real artifacts.
- The Cloud/#50 boundary (KD-1) is the sharpest coupling: conflict resolution and forward-migration overlap, and getting the ownership split wrong duplicates #50's migration logic.
- Build determinism is a real gate but the pinned host is human/host-access-limited (per the existing cert-run OPEN ISSUES history) — the ship-gate design must not assume CI can certify what only the pinned host can.

## Version History
| Version | Date | Change |
|---------|------|--------|
| v0.1 | July 22, 2026 | Initial high-level plan. |
