# Spec #45 — Board & Ownership Dynamics — High-Level Plan

> **Created:** July 22, 2026
> **Status:** PLAN (pre-design-supplement — no section files, no `SPEC_INDEX.md` row). Candidate spec number **#45** (proposed in `management-layer-spec-roadmap.md`, not reserved).
> **Master-plan home:** §5 Stage 3 board · **Tier:** S3 · **Wave:** 5 · **FR prefix (proposed):** FR-BD
> **Determinism:** domain tag `0x2D` / SubsystemOrdinal 95 (proposed off-pitch block, roadmap §6)
> **Purpose:** Ownership types, takeovers, and board confidence beyond #30's season objectives — with board confidence modelled as a morale-shape analogue and takeovers drawn from a dedicated stream.

## 1. Scope
Club ownership types, takeover events, and a board-confidence model that extends #30's season objectives into a persistent, evolving relationship between manager and board. Board confidence reuses the #33 morale-model **shape** (not a new bespoke model); takeovers are stochastic events on a dedicated stream that can change ownership type, budgets, and objectives.
Out of scope: the season objectives themselves (owned by #30), the budget numbers (owned by #40), the sacking/job-security decision (feeds #30, decided there), the morale mechanics themselves (shape borrowed from #33).

## 2. Staging (minimal-first → deep)
Minimal surface = a single board-confidence scalar driven by objective progress (the #30 season objective already in place) — the **identity** the deep tier modulates. Deep tier adds ownership types, takeover events, differentiated board personalities/patience, and multi-factor confidence (finances, results, transfers) on the same confidence-update code path.

## 3. Dependencies
- **Upstream (needs):** #33 Personalities/Morale (confidence-model shape to reuse), #40 Club Finances (budget context that conditions board expectations).
- **Downstream (consumers):** #30 Season Loop (sacking/job-security state; objectives conditioned by ownership), #40 Finances (takeovers change budgets).

## 4. Persistent state & save impact
New world/season state: ownership type, board-confidence value(s), takeover history/pending. Lands as an opaque, independently version-gated sub-blob → `SEASON_SAVE_FORMAT_VERSION` bump (season codec never parses it). Confidence may reuse #33's block shape but is club-scoped, not player-scoped.

## 5. Determinism
World tick (`WorldClock`). New RNG sub-stream + domain tag `0x2D` + `SubsystemOrdinals` entry 95 (proposed) for **takeover events** (occurrence/type). Board-confidence drift from objective progress is a deterministic projection, not a draw. Registration pinned at promotion.

## 6. Key design decisions to resolve (the supplement must answer)
- **KD-1** Board confidence as a **morale-model analogue**: reuse #33's confidence/self-efficacy shape rather than a bespoke model — define what maps (objective progress → confidence, patience → decay) and confirm it is club-scoped state, not a #33 player edge.
- **KD-2** Takeover event ownership: what draws from the `0x2D` stream, and what does a takeover mutate (ownership type → #40 budget, → #30 objectives) without #45 owning those blocks?
- **KD-3** Sacking/job-security boundary: #45 supplies confidence state; #30 makes the decision. Define the read direction so #45 never fires the manager itself.
- **KD-4** Ownership types: minimal (one generic board) at Stage 3 min vs differentiated (ambitious/frugal/absentee) — one code path with dials.
- **KD-5** Reconciliation with #30's existing season-objective state so board confidence extends rather than duplicates it.

## 7. Primary surfaces (proposed)
- A board-confidence accessor (proposed) read by #30 for job-security.
- A takeover-event advance entry point (proposed) driven by the #30 day-advance loop, draws off `0x2D`.
- Ownership-type read model (proposed) feeding #40 budget derivation.

## 8. Test focus
Round-trip determinism over ownership/confidence/takeover state; identity behaviour (minimal single-scalar confidence tracks #30 objectives byte-identically); deterministic takeover draw from the world seed; fail-loud on confidence out of range (borrowing #33's bounded-value posture); one-directional feed into #30/#40 (no #45-side sacking).

## 9. Open questions / risks
- Confidence-model duplication with #33 if the shape is re-derived rather than reused.
- Objective double-truth with #30.
- Takeover cascade into #40 budgets and #30 objectives must be a clean one-directional mutation, not a coupling knot.

## Version History
| Version | Date | Change |
|---------|------|--------|
| v0.1 | July 22, 2026 | Initial high-level plan. |
