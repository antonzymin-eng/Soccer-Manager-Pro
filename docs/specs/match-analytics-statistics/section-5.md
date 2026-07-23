# Match Analytics & Statistics #37 — Section 5: Test Plan

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.2 — section-file PASS-1 (2M+3L) → AR-2 convergence; APPROVED)
**Version:** 0.2
**Status:** APPROVED

---

## 5.1 Test groups

### T-AN-DET — Determinism (FR-AN-016)
- **T-AN-DET-001** — Two same-seed match runs, analytics observed over each, yield **byte-identical**
  `MatchAnalyticsResult` (every statline field, every map point in order). The `MatchViewerTests`
  two-run precedent.
- **T-AN-DET-002** — `Build()` is idempotent: two calls on the same accumulator state return equal
  results and the second does not consume the accumulators (a live-HUD repeat-Build never drifts).

### T-AN-NEU — Observer neutrality (FR-AN-017)
- **T-AN-NEU-001** — A match run **with** analytics observing produces the **same match digest** as the
  same-seed run with **no** observer (the `match-viewer` digest-lock — computing stats perturbs nothing).

### T-AN-BASIC — Basic derivations (FR-AN-003..009, 012)
- **T-AN-BASIC-001** — A scripted match with known `GoalAwardedEvent`s ⇒ correct per-team goals + goal
  map points at the crossing positions.
- **T-AN-BASIC-002** — Possession tick-weighting: a fabricated `PossessionChanged` sequence over N ticks
  ⇒ `share%` matches the hand-computed tick fractions, with the dead-ball remainder in the `none` bucket
  (F3), the triple summing to 100 (§3.1).
- **T-AN-BASIC-003** — Fouls/cards/offsides routed to the correct team via the KD-6 map (an
  `Offender`/`Recipient` agentId on each team) and via the direct team byte (`Offside.Team`).
- **T-AN-BASIC-004** — Restart tallies split correctly by `RestartKind` into corners/throw-ins/
  goal-kicks per `AwardedTeam`.

### T-AN-XG — xG model shape (FR-AN-013, 014)
- **T-AN-XG-001** — `XgLocationModel.Evaluate` monotonicity: closer origin ⇒ higher xG at fixed angle;
  wider angle ⇒ higher xG at fixed distance; output clamped `[0,1]`.
- **T-AN-XG-002** — The §3.3 penalty-spot worked example reproduces `xG ≈ 0.569` (± a documented
  tolerance) against the Appendix A illustrative coefficients — the golden hand-derivation lock.
- **T-AN-XG-003** — Symmetry: a team-0 origin and its mirror for team 1 (reflected across the pitch
  centre) yield equal xG (the model is team-relative, no home/away asymmetry — the ERR-008-002 mirror
  discipline).
- **T-AN-XG-004** — Producer-gated (F4): with no shot corpus, `AdvancedStatline.LiveXgAvailable=false`
  and `XgSum=0` — the model exists but reports no live xG at Stage 1.

### T-AN-TERR — Territorial / heatmap (FR-AN-010, 011)
- **T-AN-TERR-001** — A scripted ball-position sample sequence ⇒ territorial % matching the hand
  fraction; a midfield-line sample assigned by the strict `>` (total split).
- **T-AN-TERR-002** — Heatmap bins accumulate to the correct fixed grid cells for known agent positions;
  team split via the KD-6 map.

### T-AN-FAIL — Fail-loud (FR-AN-018) + forward-compat (F5)
- **T-AN-FAIL-001** — An agent index outside `[0, SQUAD_SIZE)` from an observation ⇒ **throw** (F1).
- **T-AN-FAIL-002** — A non-finite sampled position or location ⇒ **throw** (F2, NaN-gate).
- **T-AN-FAIL-003** — A tap record of a Tier A ordinal #37 does not aggregate ⇒ **ignored**, no throw
  (F5 forward-compat — locks that adding a future producer does not crash old analytics).
- **T-AN-FAIL-004** — `ObserveTick` called with a non-consecutive `currentTick` (a dropped tick) ⇒
  **throw** (F6 — the lossless every-tick contract is enforced, not merely documented).

## 5.2 FR traceability

| FR | Test(s) |
|---|---|
| FR-AN-001/002/020/021 (read-only posture) | T-AN-NEU-001 (no mutation) + assembly-reference audit (§4.1) |
| FR-AN-003 possession | T-AN-BASIC-002 |
| FR-AN-004..009 basic tallies | T-AN-BASIC-001/003/004 |
| FR-AN-010/011 territorial/heatmap | T-AN-TERR-001/002 |
| FR-AN-012 agent→team | T-AN-BASIC-003, T-AN-TERR-002 |
| FR-AN-013/014 xG | T-AN-XG-001/002/003/004 |
| FR-AN-015 view models / clean refs | §4.1 reference audit |
| FR-AN-016 determinism | T-AN-DET-001/002 |
| FR-AN-017 observer neutrality | T-AN-NEU-001 |
| FR-AN-018 fail-loud | T-AN-FAIL-001/002 |
| FR-AN-003/021 lossless every-tick (F6) | T-AN-FAIL-004 |
| FR-AN-019 deferred producers named | Appendix D + T-AN-FAIL-003 (F5) |

## 5.3 Deliberately untested (out of scope)

- No save round-trip (no persistent state, FR-AN-020).
- No shot/pass/tackle derivation (producer-gated, KD-1 — those tests land with the T2 follow-up's own
  review).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial test plan (T-AN-DET/NEU/BASIC/XG/TERR/FAIL) + FR traceability. Status IN REVIEW. |
| 0.2 | 2026-07-22 | — | Section-file PASS-1 (0H+2M+3L; M-1 lossless every-tick + F6, M-2 possession known-handler, L-1 `SubstitutionEvent.Team`, L-2 territorial disambiguation, L-3 phase-context) → AR-2 convergence; APPROVED. See section-9 §9.3. |
#endregion
