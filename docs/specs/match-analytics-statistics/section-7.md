# Match Analytics & Statistics #37 — Section 7: Forward Extensions

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.2 — section-file PASS-1 (2M+3L) → AR-2 convergence; APPROVED)
**Version:** 0.2
**Status:** APPROVED

---

## 7.1 T-phase implementation plan (post-approval, forward design)

The #21–#30 pre-T0 posture: this spec approves the design; the code lands in phases.

- **T0** — value types (`MatchStatline`/`AdvancedStatline`/`StatPoint`/`MatchAnalyticsResult`),
  `MatchAnalyticsConstants`, `XgLocationModel` (pure) + unit locks (T-AN-XG-*). No engine wiring;
  behaviour-neutral (the engine is untouched). The CS0104 name-collision grep (§4.6) runs here.
- **T1** — the read-only per-tick ledger tap on `MatchEngine` (KD-7, the `BallView`-class accessor) +
  `MatchAnalyticsAggregator` consuming it and the world-state sample; the observer-neutrality digest-lock
  (T-AN-NEU-001) and two-run determinism (T-AN-DET-001) + the basic/territorial derivations.
- **T2** — **the deferred-producer follow-up (a match-engine change with its own review, NOT #37's
  surface).** See §7.2.

## 7.2 The deferred producer-gated stats (KD-1 — Appendix D)

The stats #37 cannot derive today, each with the exact new Tier A producer it waits on. Each is a
match-engine change reviewed on its own; #37's aggregation then extends over the new record (F5 means
old #37 ignores it until extended — no lockstep break).

| Deferred stat | Needs new producer | Why the ledger can't supply it today |
|---|---|---|
| Shots / shots-on-target | `ShotAttemptedEvent` (origin, on-target flag) | no shot event is published; only goals appear |
| **xG over shots** | `ShotAttemptedEvent` (shot **origin** geometry) | the goal event's position is the crossing point, not the origin (§3.3 / KD-2) |
| Pass-completion % | `PassCompletedEvent` / `PassAttemptEvent` producer | ordinals `0x0C/0x0D` are registered but never published |
| Tackles / interceptions | `TackleEvent` | no tackle event exists |
| Saves | `SaveAttemptedEvent` in the digest | the GK plugin's `0x14` is opt-in and not committed to the per-tick digest |

**Discipline:** #37 does **not** build any of these consumers now (FR-LW-031 phantom-consumer rule).
When a producer lands, extending the aggregator's §3.2 routing table + widening the xG corpus (§3.3) is
the whole change — no redesign.

## 7.3 Persisted reports (out of scope)

If career mode wants a saved historical report, that needs a persistence surface (and, for a post-match
recompute, a #17 ledger **reader** that does not exist, §1.4). Both are #38/#30 concerns, not #37 —
#37 stays a live read-only derivation.

## 7.4 xG balance pass (post-approval, non-blocking)

The `[GT]` xG coefficients (Appendix A) are illustrative; a Stage-2 numerical-mirror balance pass pins
them (the #21 §9.2 precedent). The model **shape** (§3.3) is the reviewed contract and does not change.

## 7.5 Generalization seams

- The view models are per-match; a competition-level aggregate (season top-scorers, team form) is a #30/
  #37-consumer concern built **over** repeated `MatchAnalyticsResult`s, not inside #37.
- The heatmap grid dimensions and territorial thirds are `[GT]`, so a richer spatial model (zones,
  packing) is a coefficient/derivation extension, not a contract change.
- **Per-half / per-period splits:** `MatchPhaseChangedEvent` (0x1A) is already observed but drives no
  Stage-1 tally (§3.2 / App. B); a future extension can partition the accumulators at each phase
  boundary to report first-half / second-half statlines, with no new engine surface (the boundary
  record already exists).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial forward extensions: T-phase plan, the deferred-producer table (KD-1), persisted-reports deferral, xG balance pass, generalization seams. Status IN REVIEW. |
| 0.2 | 2026-07-22 | — | Section-file PASS-1 (0H+2M+3L; M-1 lossless every-tick + F6, M-2 possession known-handler, L-1 `SubstitutionEvent.Team`, L-2 territorial disambiguation, L-3 phase-context) → AR-2 convergence; APPROVED. See section-9 §9.3. |
#endregion
