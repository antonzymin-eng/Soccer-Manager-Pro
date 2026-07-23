# Match Analytics & Statistics #37 — Appendices

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.2 — section-file PASS-1 (2M+3L) → AR-2 convergence; APPROVED)
**Version:** 0.2
**Status:** APPROVED

---

## Appendix A — Constant catalogue (`MatchAnalyticsConstants`)

`[GT]` magnitudes are illustrative pending a Stage-2 balance pass (§7.4 — the #21 §9.2 precedent; the
contract is the model shape, not the numbers).

| Constant | Tag | Value | Meaning |
|---|---|---|---|
| `XG_INTERCEPT` | `[GT]` | 0.86 | logistic intercept (§3.3) |
| `XG_DIST_COEF` | `[GT]` | 0.11 | per-metre distance penalty |
| `XG_ANGLE_COEF` | `[GT]` | 0.017 | per-degree subtended-angle bonus |
| `GOAL_WIDTH_M` | `[CROSS]` | 7.32 | goal mouth; mirror of the IFAB pitch catalogue (Ball Physics #1 / `MatchViewerConstants` IFAB `[FIXED]`) — consumed read-only for the xG angle geometry |
| `HEATMAP_COLS` | `[GT]` | 12 | positional-bin grid columns over `[0, PITCH_LENGTH]` |
| `HEATMAP_ROWS` | `[GT]` | 8 | positional-bin grid rows over `[0, PITCH_WIDTH]` |
| `TERRITORIAL_SAMPLE_STRIDE` | `[GT]` | 1 | observed-tick stride for the territorial/heatmap sample (deterministic cadence, §3.4) |

`PITCH_LENGTH_M` / `PITCH_WIDTH_M` are consumed as existing `[CROSS]` mirrors of the coordinate-system
catalogue (Ball Physics #1 §1.2), not re-declared here.

## Appendix B — Ledger-record → stat derivation table (the KD-1 in-scope set)

The 8 Tier A record types the engine actually publishes, and the stat each feeds (verified in
`src/event-system/*.cs` + `MatchEngine.cs`):

| Ordinal | Record | Fields used | Stat (§3.2) | Team via |
|---|---|---|---|---|
| `0x04` | `PossessionChangedEvent` | `NewHolder` | possession share (§3.1) | KD-6 map |
| `0x05` | `FoulCommittedEvent` | `Offender`, `Location` | fouls + foul map | KD-6 map (`Offender`) |
| `0x06` | `CardIssuedEvent` | `Recipient`, `CardKind` | yellow/red cards | KD-6 map (`Recipient`) |
| `0x07` | `GoalAwardedEvent` | `ScoringTeam`, `BallPosition` | goals + goal map (crossing pt) | `ScoringTeam` |
| `0x08` | `SubstitutionEvent` | `Team` | substitutions | `Team` (direct) |
| `0x18` | `OffsideCalledEvent` | `Team`, `Location` | offsides + offside map | `Team` |
| `0x19` | `RestartAwardedEvent` | `RestartKind`, `AwardedTeam` | corners/throw-ins/goal-kicks | `AwardedTeam` |
| `0x1A` | `MatchPhaseChangedEvent` | `newPhase` | phase-boundary context only — drives no Stage-1 tally (available for a future per-half split, §7.5) | — |

Positional stats (territorial %, heatmap) derive from the observational world-state sample, **not** the
ledger (§3.4 — no event streams a position).

## Appendix C — xG worked example (penalty spot)

Illustrative coefficients `INTERCEPT=0.86, DIST=0.11, ANGLE=0.017`; central shot 11 m from goal centre:

- `d = 11.0` m.
- Each post is `GOAL_WIDTH/2 = 3.66` m off-centre; from 11 m each subtends `atan(3.66/11) = 18.43°`, so
  `θ = 36.87°`.
- `z = 0.86 − 0.11·11 + 0.017·36.87 = 0.86 − 1.21 + 0.6268 = 0.2768`.
- `xG = 1/(1 + e^−0.2768) = 0.5688`.

Shape checks: 6-yard central tap (`d≈5.5`, `θ≈65°`) → `z≈0.86−0.605+1.105=1.36` → `xG≈0.796`; 25 m
central (`d≈25`, `θ≈16°`) → `z≈0.86−2.75+0.272=−1.618` → `xG≈0.165`. Monotone: closer + wider ⇒ higher
(T-AN-XG-001/002).

## Appendix D — Deferred producer-gated stats (KD-1 / §7.2)

| Deferred stat | Waits on (new Tier A producer) | Ledger gap today |
|---|---|---|
| Shots / on-target | `ShotAttemptedEvent` | no shot event published (only goals) |
| xG over shots | `ShotAttemptedEvent` (shot **origin**) | goal position = crossing point, not origin |
| Pass-completion % | pass producer (`0x0C/0x0D` registered, unpublished) | no pass event published |
| Tackles / interceptions | `TackleEvent` | no tackle event exists |
| Saves | `SaveAttemptedEvent` committed to the digest | GK `0x14` is opt-in, not in the digest |

Each is a match-engine change with its own review; #37 builds no consumer for it now (FR-LW-031). F5
(§2.3) guarantees an added producer does not crash pre-extension #37.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial appendices: constant catalogue, ledger→stat table, xG worked example, deferred-producer table. Status IN REVIEW. |
| 0.2 | 2026-07-22 | — | Section-file PASS-1 (0H+2M+3L; M-1 lossless every-tick + F6, M-2 possession known-handler, L-1 `SubstitutionEvent.Team`, L-2 territorial disambiguation, L-3 phase-context) → AR-2 convergence; APPROVED. See section-9 §9.3. |
#endregion
