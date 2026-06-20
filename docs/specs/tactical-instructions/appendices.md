# Tactical Instructions Specification #21 — Appendices

**Created:** June 20, 2026
**Last Updated:** June 20, 2026 (v0.1)
**Version:** 0.1
**Status:** IN REVIEW

---

## Appendix A — Constant catalogue (`TacticalInstructionsConstants.cs`)

All values are illustrative `[GT]` defaults pending the §5.6 balance pass (G2). Shapes are normative;
magnitudes are not pinned until the balance pass. Region order Fixed → Derived → GT.

### A.1 Fixed (structural)

| Constant | Tag | Value | Rationale |
|---|---|---|---|
| `MENTALITY_LEVELS` | [FIXED] | 7 | enum cardinality (VeryDefensive…VeryAttacking) |
| `INSTR_BIAS_LEVELS` | [FIXED] | 3 | Less/Default/More |
| `TIME_WASTING_MAX` | [FIXED] | 4 | dial range [0..4] |

### A.2 Derived

| Constant | Tag | Formula | Rationale |
|---|---|---|---|
| `RISK_MULT_BALANCED` | [DERIVED] | 1.0 | identity row of the §3.2 table (FR-TI-031) |
| `LINE_BIAS_BALANCED` | [DERIVED] | 0.0 | identity row of the §3.2 table |

### A.3 GT (game-tuned; illustrative pending balance pass)

| Constant | Tag | Value | Consumed by |
|---|---|---|---|
| `MentalityRiskMult[7]` | [GT] | {0.80,0.88,0.94,1.00,1.06,1.14,1.20} | §3.2 / #8 |
| `MentalityLineBias[7]` | [GT] | {−0.20,−0.12,−0.05,0,+0.05,+0.12,+0.20} | §3.2 |
| `InstrBiasMult[3]` | [GT] | {0.85, 1.00, 1.15} | §3.4 |
| `TempoThresholdStep` | [GT] | 0.10 | §3.4 / #8 thresholds |
| `WidthScalar[5]` | [GT] | {0.85,0.92,1.00,1.08,1.15} | §3.4 / #12 |
| `DefWidthScalar[3]` | [GT] | {0.90,1.00,1.10} | §3.4 / #12 |
| `LineOfEngagementScalar[5]` | [GT] | {0.80,0.90,1.00,1.10,1.20} | §3.4 / #13 |
| `DutyForeOffsetM[3]` | [GT] | {−3.0, 0.0, +3.0} | §3.4 / #12 (Defend/Support/Attack) |
| `DutyAggressionBias[3]` | [GT] | {−0.05, 0.0, +0.05} | §3.4 / #8 |
| `RoleWeightModifiers[PlayerRole][ActionType]` | [GT] | table A.4 | §3.3 / #8 |

> Every `[GT]` constant carries `// TODO: replace with config loader (Stage 1)` per `src/CLAUDE.md`.

### A.4 `RoleWeightModifiers` (illustrative excerpt; full table at T2 balance pass)

Columns are `ActionType {PASS,SHOOT,DRIBBLE,HOLD,MOVE_TO_POSITION,PRESS,INTERCEPT}`. Default row = all 1.0.

| PlayerRole | PASS | SHOOT | DRIBBLE | HOLD | MOVE | PRESS | INTERCEPT |
|---|---|---|---|---|---|---|---|
| (Default) | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 |
| Poacher | 0.95 | 1.25 | 1.00 | 0.80 | 1.05 | 0.90 | 0.95 |
| DeepLyingPlaymaker | 1.20 | 0.90 | 0.90 | 1.05 | 1.00 | 0.95 | 1.05 |
| BallWinningMid | 0.95 | 0.90 | 0.90 | 1.00 | 1.00 | 1.25 | 1.20 |
| InsideForward | 1.00 | 1.15 | 1.15 | 0.85 | 1.05 | 0.95 | 0.95 |
| TargetMan | 1.05 | 1.10 | 0.85 | 1.10 | 0.90 | 0.90 | 0.95 |

All cells ∈ [0.5, 2.0] (T-TI-U-029). Magnitudes illustrative; directions are the reviewable contract.

## Appendix B — Canonical snapshot field order (pinned for FR-TI-028)

When match-engine Phase D serializes tactics, the order is: **TeamTactic** (Mentality, Formation, Tempo,
Width, Passing, Pressing, LineOfEngagement, DefensiveLine, DefensiveWidth, TransitionWon, TransitionLost,
OffsideTrap, TriggerPressMask, FocusPlay, GkDistribution, TimeWasting) → **per agent PlayerTactic**
(Role, Duty, then PlayerInstructions: RiskyPasses, ShootTendency, DribbleTendency, CrossTendency,
PositioningFreedom, CloseDown, TightMarking, MarkTargetEntityId, SetPieceRoles). Enums serialize as their
`byte` ordinal; the order above is digest-load-bearing and locked by T-TI-EXP-004. Any reorder/field add
requires a `SNAPSHOT_SCHEMA_VERSION` bump.

## Appendix C — Worked example (full tactic → behaviour)

Team set **Attacking / Wide / High press / High line**, an `InsideForward` (Attack duty) in the
attacking third evaluating SHOOT:

1. base utility from #8 product = U.
2. ×`RoleWeightModifiers[InsideForward][SHOOT]` = ×1.15.
3. ×`MentalityRiskMult[Attacking]` = ×1.14.
4. ×`DutyAggressionBias→mult` (Attack, SHOOT) ≈ ×1.05.
5. ×instruction (no ShootTendency override) = ×1.00.
6. clamp to `[UTILITY_FLOOR, UTILITY_CEILING]`.
   `DefensiveLine` 0.50 + `MentalityLineBias[Attacking]` 0.12 = 0.62; width scalar 1.08 feeds #12;
   `LineOfEngagementScalar[High]` 1.10 shortens #13 trigger distance. Net: this forward shoots
   meaningfully more often and the team presses higher and holds a higher line — the intended feel.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-06-20 | — | Constant catalogue (illustrative [GT]), RoleWeightModifiers excerpt, snapshot order, worked example. |
#endregion
