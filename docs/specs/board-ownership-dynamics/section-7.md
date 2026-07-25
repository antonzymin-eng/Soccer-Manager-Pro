# Board & Ownership Dynamics #45 — Section 7: Future Extensions & T-Phase Plan

**Created:** July 25, 2026
**Last Updated:** July 25, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** IN REVIEW

---

## 7.1 T-phase plan

| Phase | Content | Behaviour |
|---|---|---|
| **T0** | The assembly + value types + constants catalogue + `BoardStore` + the pure projections (§3.2/§3.3/§3.4) and their tests. Nothing wired into #30. | **Inert** — no caller exists |
| **T1** | `BoardSaveCodec` + the round-trip / fail-loud suite. Still not composed into the season save. | **Inert** |
| **T2** | **First non-inert phase.** Wire the daily advance at #30 slot 8; compose the sub-blob into `SeasonSaveCodec` (bumps `SEASON_SAVE_FORMAT_VERSION`); land ERR-030-009's *effect* (`JobSecurity` → derived band, bumping `SEASON_STATE_FORMAT_VERSION`); #30 consumes the band; #40 consumes the `Try` projection. | **Live**, and identity-preserving: sensitivity `0` ⇒ `BoardModifier.Identity` ⇒ budgets unchanged |
| **T3** | Deep tier: ownership types as non-identity dials, the `[GT]` balance pass, `BD_BUDGET_SENSITIVITY_PERMILLE` > 0, the #33 morale input, and takeovers — which promotes `_RESERVED_0x2D_` → `DOMAIN_TAG_BOARD_OWNERSHIP` at the first draw. | **Named activation**, not behaviour-neutral by design |

T2 is where #45 becomes observable, and it is deliberately still budget-neutral: confidence moves and job
security becomes real, but money does not change until T3 turns sensitivity on. That split exists so the
save-format work and the money-balance work fail independently rather than together.

## 7.2 Deep-tier extensions (designed for, not built)

- **Ownership types with real dials** — `Ambitious` / `Frugal` / `Absentee` as values on the one code
  path (KD-4). No new branch, no subtype.
- **Multi-factor confidence** — transfer activity, financial health, cup runs, all entering through the
  same `ComputeConfidenceTarget` assembly as additional committed inputs, never as new code paths.
- **Takeovers** (§3.5) — the only stochastic surface; keyed draws, single stream.
- **The #33 morale input** — `BD_MORALE_WEIGHT_PERMILLE` > 0, arriving as a routed value (FR-BD-016).
- **#45 as producer of #33's board delta** — `HumanSystemsDayInput.BoardObjectiveDeltaPermille` exists
  today with no producer; #45 is its natural one, under the KD-7 one-day-stale contract.
- **AI-manager job security** — modelling all clubs rather than the managed one. The state shape already
  supports it (the store is keyed by `ClubId` and holds any subset); what is missing is an AI-manager
  model, which is not #45's to invent.

## 7.3 Explicitly not planned

- **A sacking model.** #45 will never fire a manager (KD-3). If a future spec wants richer termination
  semantics, it consumes confidence; it does not move the decision into #45.
- **A second budget path.** #40 §7 forbids it and #45 accepts that constraint (FR-BD-017).
- **Objective authorship.** #45 will not set or mutate `BoardObjective` (FR-BD-013). A deep tier where
  ownership *influences* the next season's objective is a **#30-side** read of #45's expectation
  projection, not a #45-side write — recorded here so the direction is not quietly reversed later.
- **Board state for the match engine.** No #45 value reaches the 10 Hz/60 Hz loops.

## 7.4 Risks carried

- **R-1 — the ERR-030-009 amendment is the one non-additive change.** If `BoardState.JobSecurity` has
  acquired a consumer by the time #45 lands, the derived band must preserve that consumer's observable
  behaviour, and the `SEASON_STATE_FORMAT_VERSION` bump must be sequenced with any in-flight save work.
  Re-verify at T2 rather than assuming the July-2026 reading still holds.
- **R-2 — confidence/objective drift.** Closed by construction today (FR-BD-014: #45 holds no copy of
  the objective), but a future maintainer caching the objective in #45's blob for convenience would
  re-open exactly the double truth KD-5 removed. Appendix B carries the warning at the point of
  temptation.
- **R-3 — standing option, not a debt:** extract a shared integer-primitives assembly if a third
  `DriftPermille` call site appears (KD-1). Two do not justify it.
- **R-4 — `[GT]` magnitudes are illustrative.** Every value in Appendix A.3 is shape-only pending the T3
  balance pass (the #21 G2 / #22 / #26 §9.2 precedent). §5 asserts identity and direction, never
  magnitude, so the balance pass cannot invalidate a passing test suite.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-25 | — | Initial §7 (T0–T3 with T2 as the first non-inert phase and its deliberate budget-neutrality, deep-tier extensions incl. #45 as producer of #33's existing board-delta field, the not-planned list with the objective-direction note, risks R-1..R-4). Status IN REVIEW. |
#endregion
