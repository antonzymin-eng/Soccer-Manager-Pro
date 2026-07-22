# Spec #33 — Personalities, Morale & Squad Dynamics — High-Level Plan

> **Created:** July 22, 2026
> **Status:** PLAN (pre-design-supplement — no section files, no `SPEC_INDEX.md` row). Candidate spec number **#33** (proposed in `management-layer-spec-roadmap.md`, not reserved).
> **Master-plan home:** §5 Stage 4 / Master Vol 2 · **Tier:** S4 (pulled forward) · **Wave:** 3 (GATING) · **FR prefix (proposed):** FR-HS
> **Determinism:** domain tag `0x25` / SubsystemOrdinal 87 (proposed off-pitch block, roadmap §6)
> **Purpose:** The canonical human-systems model — personality traits, morale/happiness, cliques/chemistry, mentoring — that Living World #22 was built to consume read-only.

## 1. Scope
The vol-2 human-systems substrate: per-player personality traits, a morale/happiness model (the H-Gate confidence-vs-self-efficacy shape), squad cliques/chemistry, and mentoring relationships — advanced on the world tick and exposed as an authoritative, read-only committed-state surface. This is the single producer #22's dormant `WorldLoop` phase-2 seam and its FR-LW-004 `PlayerEdge`/relationship-layer mirror were written against.
Out of scope: #22's interaction/memory/arc layer (already built over this model), match-tick behaviour, media/press dialogue (#35), inbox man-management writes (#46) — those are consumers.

## 2. Staging (minimal-first → deep)
Stage-2/minimal surface is a small per-player trait vector + a scalar morale value updated by a deterministic daily projection — the **identity** the deep tier modulates. The Stage-4 deep model (H-Gate confidence/self-efficacy split, cliques/chemistry graph, mentoring propagation) is the same daily-update code path with config dials enabled, never a rewrite of the minimal one.

## 3. Dependencies
- **Upstream (needs):** #27 Squad/Player Data (canonical records keyed by player; morale/personality attach to the canonical record shape).
- **Downstream (consumers):** #22 Living World (dormant phase-2 read + `PlayerEdge` mirror light up **without a #22 rewrite**), #31 Transfers (negotiation willingness), #35 Media, #45 Board (morale-analogue shape reuse), #46 News/Inbox (man-management writes morale), #29 cohesion, match-engine input via the #27 attribute-projection seam.

## 4. Persistent state & save impact
New world-state block (per-player morale/personality/mentoring + squad clique/chemistry graph). Lands in the world store as an opaque, independently version-gated sub-blob → `WORLD_STORE_FORMAT_VERSION` bump; the season codec never parses it (the `SeasonSaveCodec`/`WorldStateSerializer` sub-blob pattern). Every new field serialized and round-trip-covered.

## 5. Determinism
World tick (`WorldClock`, one day = one `worldTick`). New RNG sub-stream + domain tag `0x25` + `SubsystemOrdinals` entry 87 (proposed) for any stochastic daily draws (morale drift, event reactions). Registration pinned in #16 §3.4 at promotion, not now.

## 6. Key design decisions to resolve (the supplement must answer)
- **KD-1 (load-bearing):** expose **exactly** the read-only surface #22's FR-LW-004 `PlayerEdge`/relationship-layer contract expects (the vol-2 §2.1 authoritative edge that `MemoryStore.ApplyEvent` refuses to write and that `SeedFrom`/the phase-2 read mirror consumes) so #22's dormant seams wire up with **no #22 redesign** — cross-check #22 §2.1/§3.1 before scoping. The coupling is one-directional: #33 writes canon, #22 reads a mirror; #33 never reads #22's memory layer.
- **KD-2** What is the minimal trait vector, and does the H-Gate morale split (confidence vs self-efficacy) exist at Stage 2 as a collapsed scalar or only at Stage 4?
- **KD-3** Morale → match-engine projection: is it a read-only input via the #27 projection seam only (define direction, no two-way coupling)?
- **KD-4** Cliques/chemistry: a derived graph over relationship edges, or independent persisted state? Reconcile with #22's edge store to avoid duplicate truth.
- **KD-5** Mentoring: a daily propagation of trait/morale between paired players — who owns the pairing lifecycle (#33 vs #34 staff-driven)?
- **KD-6** Determinism-block reservation: confirm `0x25`/87 is free and contiguous at promotion.

## 7. Primary surfaces (proposed)
- A read-only committed human-systems view (proposed) matching #22's phase-2 read contract.
- A per-player morale/personality accessor (proposed) for the #27 attribute-projection seam.
- A daily-advance entry point (proposed) driven by the #30 day-advance loop.
- Read accessors for #31/#35/#45/#46 (proposed) — never mutation from consumers except #46's morale write.

## 8. Test focus
Round-trip determinism (save@N → restore → advance == uninterrupted run) over the new world-state block; behaviour-neutral identity (minimal morale collapses to the Stage-2 projection; a default squad advances identically to pre-#33 once wired); fail-loud gates on out-of-range morale/trait values (the `[0,1]` edge-validation posture in `MemoryStore`); the #22 phase-2 wiring proven to change no #22 output byte until real canon is present.

## 9. Open questions / risks
- Getting KD-1's surface shape wrong forces the #22 rewrite the whole sequencing constraint (§4) exists to avoid — highest risk in the batch.
- Two-way morale coupling temptation with #31/#35 — must stay a projection, not a feedback loop, or determinism ordering gets fragile.
- Clique/chemistry double-truth against #22's edge store.

## Version History
| Version | Date | Change |
|---------|------|--------|
| v0.1 | July 22, 2026 | Initial high-level plan. |
