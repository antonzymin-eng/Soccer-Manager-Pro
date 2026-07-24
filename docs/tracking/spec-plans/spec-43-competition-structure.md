# Spec #43 — Competition Structure — High-Level Plan

> **Created:** July 22, 2026
> **Status:** **PROMOTED** (July 24, 2026) — design supplement `docs/tracking/competition-structure-design.md` (AR-converged v0.3) → 11-file section set at `docs/specs/competition-structure/` → **APPROVED**; `SPEC_INDEX.md` row 43 added. One plan position was revised at supplement stage with recorded rationale: §4/§5's serialized knockout-draw cursor → **position-independent keyed draws** (the off-pitch #41/#32/FR-SN-013a pattern; nothing RNG-serialized, no cross-competition cursor race). (Original PLAN status follows for history.) PLAN (pre-design-supplement — no section files, no `SPEC_INDEX.md` row). Candidate spec number **#43** (proposed in `management-layer-spec-roadmap.md`, not reserved).
> **Master-plan home:** §4.1 / §5 · **Tier:** Stage 2 min → Stage 5 deep · **Wave:** 5 · **FR prefix (proposed):** FR-CP
> **Determinism:** domain tag `0x2C` / `SubsystemOrdinal` 94 (proposed — to pin at promotion; the knockout-draw stream).
> **Purpose:** Generalize #30's single-league fixture/table machinery to multiple concurrent competitions (cups, continental) with deterministic knockout draws, plus promotion/relegation as a season-boundary transform.

## 1. Scope
Cups, continental competitions, and promotion/relegation over the season loop #30 ships. #30 delivers one single-division round-robin league; #43 makes the competition set a first-class collection — multiple concurrent competitions a club is entered in, knockout brackets with deterministic draws, and a season-boundary transform that moves clubs between divisions on final standings. **Out of scope:** the base league table/fixture engine (#30 owns it — #43 reuses it), match play (`MatchEngine`), discipline/suspension carry-across competitions (#44), national-team tournaments (#36, which overlays the calendar), and finances/prize money (#40).

## 2. Staging (minimal-first → deep)
Stage-2 minimal = the #30 single-league is modelled as **one competition instance** in a competition collection of size one — the identity. The deep tier adds more instances (a domestic cup with knockout rounds, a continental group+knockout competition) and the promotion/relegation season-boundary transform, all driven by the same fixture-generation + table machinery with the competition *format* (round-robin / knockout / group) as a config dial. One code path: a league is a competition with a round-robin format and no season-boundary movement; adding cups is populating the collection, not rewriting #30.

## 3. Dependencies
- **Upstream (needs):** #30 (fixture generation, league table, calendar cursor, day-advance loop — the machinery being generalized), #27 (the club/roster world competitions are drawn over), `WorldClock` / `WorldStore` (world-tick substrate + seed).
- **Downstream (consumers):** #36 (international tournaments overlay the same calendar/competition model), #30 tables (promotion/relegation writes back season-boundary standings), #44 (suspensions scoped per competition), #40 (prize money by competition finish), #38 UI (competition/bracket screens).

## 4. Persistent state & save impact
Adds the competition collection (per-competition format, entrant set, bracket/draw state, per-competition tables) to the season state #30 introduced. This rides #30's **`SEASON_SAVE_FORMAT_VERSION` bump** as an extension of the season sub-blob, or takes its own bump if it lands after #30 stabilizes — either way an **opaque, independently version-gated sub-blob** under the `SeasonSaveCodec` never-parses-sub-blobs pattern. The knockout-draw RNG cursor is serialized so a mid-season save resumes the exact bracket sequence.

## 5. Determinism
Runs on the world tick (`WorldClock`). Round-robin fixtures stay a pure function of the world seed (no draw). **Knockout and group draws need a dedicated RNG sub-stream** under domain tag `0x2C` / ordinal 94 — draws are the genuinely stochastic surface here. The stream cursor is serialized (the `match-flow.card-severity` cursor precedent) so save→restore reproduces the same bracket. Promotion/relegation is a deterministic transform over final standings — no draw.

## 6. Key design decisions to resolve (the supplement must answer)
- **KD-1** What is the competition-format abstraction — is a league a degenerate competition instance, or is there a league/cup/group-competition type union? (The minimal-first-as-identity constraint pushes toward the former.)
- **KD-2** Does the knockout-draw stream register at world-seed boot, or per-competition, so two competitions drawing in one day never share a cursor and never perturb each other's sequence?
- **KD-3** How is a knockout bracket serialized — regenerate from the seed + entrant set, or persist the resolved bracket — given entrants change as rounds resolve?
- **KD-4** Where does promotion/relegation execute in the season-boundary step, relative to #28 aging and #30's table roll, so the transform is one restartable round-trip-deterministic step?
- **KD-5** How are the same fixtures scheduled across concurrent competitions without calendar collisions, and who owns fixture-congestion resolution — #43 or #30's calendar?

## 7. Primary surfaces (proposed)
A competition-collection type owned by the #30 season state (proposed); a per-competition-format fixture/draw generator reusing #30's round-robin core (proposed); a knockout-draw service on the new sub-stream (proposed); a promotion/relegation season-boundary transform (proposed); a competition/bracket view model for #38 (proposed). Existing seams referenced: #30's fixture/table machinery, `SeasonSaveManager`, `SeasonSaveCodec`, `WorldClock`, `WorldStore`, `MatchEngine`.

## 8. Test focus
Save→restore round-trip determinism for the competition collection incl. the serialized draw cursor, byte-identical; two-run determinism of a drawn knockout bracket from one seed; behaviour-neutral proof that a single-competition collection advances a season identically to bare #30; deterministic promotion/relegation transform over fixed standings; fail-loud gates on the competition sub-blob version and malformed bracket state.

## 9. Open questions / risks
- Draw determinism is the whole risk surface — a draw that reads iteration order over an unordered entrant collection is a classic non-determinism trap; canonical entrant ordering must be pinned before the first draw.
- Generalizing #30's fixture engine risks a #30 rewrite if #30's league machinery wasn't authored competition-instance-shaped — cross-check #30's fixture-generation seam before scoping (mirrors the #30/#43 minimal-first note in roadmap §3).
- Fixture congestion across concurrent competitions is a scheduling problem #30's linear calendar may not anticipate; may force a #30 calendar edit.

## Version History
| Version | Date | Change |
|---------|------|--------|
| v0.1 | July 22, 2026 | Initial high-level plan. |
| v0.2 | July 24, 2026 | PROMOTED — supplement → section files → APPROVED (SPEC_INDEX row 43); the keyed-draws revision recorded in the status header. |
