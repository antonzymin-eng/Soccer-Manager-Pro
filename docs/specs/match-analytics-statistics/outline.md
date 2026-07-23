# Match Analytics & Statistics Specification #37 — Outline

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.2 — section-file PASS-1 (2M+3L) → AR-2 convergence; APPROVED)
**Version:** 0.2
**Status:** APPROVED
**Source:** `docs/tracking/match-analytics-statistics-design.md` v0.2 (July 22, 2026), AR-1 (1M) → AR-2 converged

---

## Purpose

Defines **read-only match statistics** derived from what a real match already produces — the
digest-bearing Tier A event ledger (Event System #17) and the observational world-state surface
(`MatchEngine`'s `BallView`/`AgentView`, the `match-viewer` precedent). #37 adds **no engine event
and no producer**, mutates nothing, stores nothing new, and bumps no format version. It is the
read-only prerequisite the post-match report UI (#38) and news/inbox (#46) render against. Authored
**minimal-first**: the Stage-1 surface is exactly the set derivable from today's ledger + positional
sample; the stats that need a new match-engine producer (shots, passes, tackles, geometry-based xG)
are **named and deferred** to a match-engine follow-up with its own review — never built as a
phantom consumer here (FR-LW-031). This is a **forward design** — nothing is built yet (the #21–#30
specification-before-code posture).

## Section map

| Section | Content |
|---|---|
| 1 | Introduction, scope (the derivable set; the producer-gated deferrals), dependencies, key decisions (KD-1..KD-7), boundary matrix (analytics vs. ledger vs. engine; observation vs. producer) |
| 2 | Functional requirements (FR-AN-001..021), data structures (`MatchStatline`/`AdvancedStatline`/`MatchAnalyticsResult`/`XgLocationModel`/`MatchAnalyticsAggregator`), failure modes F1–F6 |
| 3 | Algorithms: possession tick-weighting; per-record event derivation + agent→team routing; the xG location function (shape); territorial %/positional binning; the aggregation-core loop. Worked examples |
| 4 | Architecture: the `TacticalDirector.MatchAnalytics` assembly (`src/match-analytics/`), the read-only per-tick ledger tap (`BallView`-class observation surface), the presentation-clean reference direction, no RNG/tag/ordinal |
| 5 | Test plan (two-run determinism / observer-neutrality digest-lock / xG-model unit locks / fail-loud on malformed observation / producer-gated stats return empty) + FR traceability |
| 6 | Performance: world-tick cadence observation (not the 60 Hz hot path); accumulator sizing; zero persistent state |
| 7 | Forward extensions: the deferred-producer follow-up (shots/passes/tackles + live xG), persisted reports, the T-phase plan |
| 8 | References (association-football stat conventions; the xG two-term geometric model lineage) |
| 9 | Approval checklist + R-01..R-05 lead-developer gates |
| Appendices | Constant catalogue (`MatchAnalyticsConstants`); the ledger-record → stat derivation table; the xG worked example; the deferred-producer table |

## Key decisions (detailed in §1)

- **KD-1** Scope = exactly the derivable set (possession, goals + goal-location map, fouls/cards/
  offsides/corners/throw-ins/goal-kicks/subs, territorial %/heatmaps from the positional sample); the
  producer-gated set (shots/on-target, pass %, tackles, saves, shot-geometry xG) is a **named
  match-engine follow-up**, not #37's surface.
- **KD-2** xG is a pinned deterministic model **shape** (`[GT]` coefficients), **fully producer-gated**
  — authored + unit-locked at T0, no valid Stage-1 live input (the goal event's position is the
  crossing point, not the shot origin).
- **KD-3** Live observational aggregation (no post-match ledger reader exists), one core, two read taps.
- **KD-4** The #38 view-model contract: immutable per-match value structs, one-directional (sim never
  references #37).
- **KD-5** Determinism by construction — no RNG, no domain tag, no ordinal; two-run byte-identical +
  observer-neutral.
- **KD-6** Agent→team resolution via the observational `AgentTeamId` map, snapshotted at boot.
- **KD-7** The read-only per-tick ledger tap is an observation surface (the `BallView` class), not a
  producer — no boundary violation.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial outline authored from the converged design supplement. Status IN REVIEW. |
| 0.2 | 2026-07-22 | — | Section-file PASS-1 (0H+2M+3L; M-1 lossless every-tick + F6, M-2 possession known-handler, L-1 `SubstitutionEvent.Team`, L-2 territorial disambiguation, L-3 phase-context) → AR-2 convergence; APPROVED. See section-9 §9.3. |
#endregion
