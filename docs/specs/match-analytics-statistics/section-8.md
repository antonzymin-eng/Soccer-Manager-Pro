# Match Analytics & Statistics #37 — Section 8: References

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.2 — section-file PASS-1 (2M+3L) → AR-2 convergence; APPROVED)
**Version:** 0.2
**Status:** APPROVED

---

## 8.1 In-project sources (authoritative)

- **Event System #17** — the Tier A event record set and `EventBus`/`EventLedger` serialization; the
  8 record types #37 derives from (`docs/specs/event-system/`, verified against `src/event-system/*.cs`).
- **`src/match-viewer/`** (`MatchReplayRecorder`, `MatchViewerTests`) — the observational-read +
  observer-neutrality digest-lock precedent #37 reuses.
- **`MatchEngine` observation surface** — `BallView`/`AgentView`/`AgentTeamId`/`PossessingAgentId`/
  `CurrentTick` (v1.24, the read-only presentation accessors), the class the KD-7 ledger tap extends.
- **Coordinate system** — Ball Physics #1 §1.2 (corner origin; X goal-to-goal 0–105 m, Y touchline
  0–68 m); used by the goal-location map, territorial %, and the xG geometry.

## 8.2 Domain conventions (background)

- **Association-football match-statistic conventions** — possession %, shots, fouls, cards, offsides,
  corners as the standard match statline (the FM/Opta/StatsBomb baseline class; a data convention, not a
  citable formula).
- **xG two-term geometric model** — the shot-quality-from-distance-and-angle logistic shape is the
  well-established public expected-goals lineage (distance to goal + goal-mouth subtended angle as the
  two dominant geometric predictors). #37 pins the **shape**; the coefficients are `[GT]`, illustrative
  pending a balance pass (§7.4). No proprietary dataset or vendor model is used or required.

> Citation posture matches the project's other model-shape specs (#8/#21): the reviewed contract is the
> functional form + monotonicity, not a fitted coefficient set, so no DOI-verified fitting corpus is
> claimed. If a Stage-2 balance pass fits real coefficients, its data provenance is recorded then.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial references: in-project authoritative sources + the stat-convention / xG-shape background. Status IN REVIEW. |
| 0.2 | 2026-07-22 | — | Section-file PASS-1 (0H+2M+3L; M-1 lossless every-tick + F6, M-2 possession known-handler, L-1 `SubstitutionEvent.Team`, L-2 territorial disambiguation, L-3 phase-context) → AR-2 convergence; APPROVED. See section-9 §9.3. |
#endregion
