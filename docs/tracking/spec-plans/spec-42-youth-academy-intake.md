# Spec #42 — Youth Academy & Intake — High-Level Plan

> **Created:** July 22, 2026
> **Status:** PLAN (pre-design-supplement — no section files, no `SPEC_INDEX.md` row). Candidate spec number **#42** (proposed in `management-layer-spec-roadmap.md`, not reserved).
> **Master-plan home:** §5 Stage 3 youth · **Tier:** Stage 3 · **Wave:** 5 · **FR prefix (proposed):** FR-YA
> **Determinism:** domain tag `0x2B` / `SubsystemOrdinal` 93 (proposed — to pin at promotion; the intake-generation stream).
> **Purpose:** The academy pipeline — annual bio-banded intake generation, youth contracts, and promotion to the senior squad — reusing #28's regen/generation machinery keyed to club/nation, with academy structure (facilities/coaching) modulating intake quality.

## 1. Scope
The youth pipeline: an annual intake event that generates a cohort of youth players for each club (bio-banding per Master Vol 1), youth contract handling, and promotion of youth players into the senior squad. Intake **quality** is modulated by academy structure — facilities and youth coaching — which couples to #34 (staff) and #40 (finances). **Out of scope:** the general regen/newgen generator for the senior population (#28 owns it — #42 keys into it), player progression curves once a youth is generated (#28), senior-squad management (#30), and the training that develops promoted youth (#29). #42 is distinct from #28 regens: #28 replenishes the world population; #42 is the club-scoped academy intake structure over it.

## 2. Staging (minimal-first → deep)
Stage-3 minimal = an annual intake of a fixed cohort size per club with quality a flat function of club reputation, generated day-deterministically — the identity. The deep tier layers academy-structure modulation (facility level + youth-coaching quality raising cohort CA/PA distribution), youth-contract negotiation depth, and staged promotion criteria. One code path: minimal is the structure dials pinned at a neutral level; deepening is populating those dials, not a rewrite.

## 3. Dependencies
- **Upstream (needs):** #27 (canonical `PlayerAttributes`/`PlayerRecord`/`Squad` roster shape the intake writes into — `RosterGenerator` in `src/player-database/`), #28 (the regen/generation machinery + CA/PA model the intake reuses, keyed to club/nation), #34 (youth coaching staff modulating quality), #40 (facility investment feeding quality); world seed via `WorldClock`/`WorldStore`.
- **Downstream (consumers):** #30 squad (promoted youth enter the senior selectable pool), #29 (training develops them), #32 (scouting reports on youth prospects), #38 UI (an academy/intake screen).

## 4. Persistent state & save impact
Adds per-club academy state (facility/coaching-driven quality inputs, the current youth cohort, contract state) to the world/season state. If keyed to the roster world it may extend the world store (**`WORLD_STORE_FORMAT_VERSION` bump**); if season-scoped it rides #30's season sub-blob. Either way an **opaque, independently version-gated sub-blob** under the `SeasonSaveCodec`/`WorldStore.Snapshot()` pattern. The intake-generation RNG cursor is serialized so a reload reproduces the same cohort.

## 5. Determinism
Runs on the world tick (`WorldClock`), with the annual intake firing on a calendar boundary #30's day-advance crosses. **Intake generation needs a dedicated RNG sub-stream** under domain tag `0x2B` / ordinal 93 — the cohort's attributes/positions/potentials are the stochastic surface (the `RosterGenerator` deterministic-generation precedent: `DOMAIN_TAG_PLAYER_DATABASE`=0x1F / ordinal 81). The stream keys on club/nation so two clubs' intakes never share a cursor and are reproducible independently. Validation runs before any draw so a refused intake consumes no cursor (the living-world `world.text` refuse-before-draw precedent).

## 6. Key design decisions to resolve (the supplement must answer)
- **KD-1** Does #42 call #28's regen generator with an academy-quality parameter, or wrap a parallel generator? (Roadmap §3 mandates reuse of #28's machinery keyed to club/nation — the coupling seam must be defined so #42 isn't a fork.)
- **KD-2** Where does academy state live — world store (club-scoped, persistent across seasons) or #30 season state — and which format version bumps?
- **KD-3** How does academy structure (facilities/coaching from #34/#40) map to a cohort quality distribution without #42 reading #34/#40 internals ahead of their producers (FR-LW-031 phantom-interface discipline)?
- **KD-4** What is the intake calendar trigger and how is it made a one-shot per season year that survives save→restore across the boundary (the #26 half-time one-shot-flag precedent)?
- **KD-5** What is the promotion criterion from youth to senior squad, and does it mutate the roster or move a record between two rosters (world-store roster movement contract)?

## 7. Primary surfaces (proposed)
An academy-intake service reusing #28's generator keyed to club/nation on the new sub-stream (proposed); per-club academy state in the world/season store (proposed); a youth-contract handler (proposed); a promotion command moving a youth record into the senior `Squad` (proposed); an academy/intake view model for #38 (proposed). Existing seams referenced: #27 `PlayerAttributes`/`PlayerRecord`/`Squad`/`RosterGenerator`, #28 regen/CA-PA machinery, `WorldClock`, `WorldStore`, `SeasonSaveCodec`.

## 8. Test focus
Save→restore round-trip determinism for academy state incl. the serialized intake cursor, byte-identical; two-run determinism of a generated cohort from one seed (mirrors `RosterGeneratorTests`); one-shot intake trigger surviving a boundary-crossing restore; behaviour-neutral proof that neutral academy dials reproduce the flat-quality identity; fail-loud gates on the academy sub-blob version; promotion moves a record without corrupting either roster.

## 9. Open questions / risks
- The #28 reuse seam (KD-1) is the central risk — if #28's generator isn't parameterized for academy quality when #42 lands, #42 either forks the generator (violates roadmap §3) or forces a #28 edit.
- #34/#40 coupling (KD-3): #42 is Wave 5 and #34 is Wave 4, #40 is Wave 2 — the ordering is favourable, but the quality-input contract must be stubbed neutral if either isn't wired, to stay phantom-free.
- Bio-banding (Master Vol 1) intake-age/quality banding needs its source model confirmed against the master plan before the cohort generator is scoped.

## Version History
| Version | Date | Change |
|---------|------|--------|
| v0.1 | July 22, 2026 | Initial high-level plan. |
