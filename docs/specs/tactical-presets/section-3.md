# Tactical Presets & AI-Manager Selection Specification #26 — Section 3: Formulas and Algorithms

**Created:** July 8, 2026
**Last Updated:** July 11, 2026 (v0.3 — PASS-1 M-1 gates closed; `MATCH_TICKS_TOTAL` promoted to `[CROSS]`)
**Version:** 0.3
**Status:** APPROVED

---

## 3.1 Preset projection (FM-TP-01)

`Project(preset) → (TeamTacticConfig, PlayerTacticConfig)`: the team tactic fills every team slot
of a `TeamTacticConfig` for the managed team only (the other team's entry stays its own
configured/default value); `Players` when present fills the managed team's roster entries of a
`PlayerTacticConfig`, else `PlayerTacticConfig.Identity` rows. Pure construction; no engine call.

## 3.2 Decision gate (FM-TP-02; composition root, KD-3)

```
decisionDue(tick) = isKickoff(tick)
                 OR isHalfTimeBoundary(tick)                 # from existing match-phase counts (FR-TP-019)
                 OR (tick − LastDecisionTick) ≥ MANAGER_DECISION_INTERVAL_TICKS
evaluated only when IsAiStrideTick(tick) AND Mode == AI      # off-stride firing impossible (F5)
on fire: LastDecisionTick = tick
```

`MANAGER_DECISION_INTERVAL_TICKS` `[GT]` = 18 000 (5 match-minutes at 60 Hz).

- **Worked example:** kickoff decision at tick 0; next interval decision no earlier than tick
  18 000 (5:00), evaluated at the first stride tick ≥ due; half-time fires regardless of interval
  position.

## 3.3 Kickoff selection scoring (FM-TP-03; T3)

At kickoff the score differential is 0, so selection reduces to profile disposition:

```
score(p) = BASE_FIT[p]                                    # [GT] per-preset baseline row
         + Aggression × AGGR_AFFINITY[p]                  # [GT] per-preset affinity rows
         + Caution    × CAUT_AFFINITY[p]
select argmax score(p); tie → lowest ordinal (KD-8)
```

All rows dimensionless, bounded [−1, +1] by shape test (FR-TP-020). **Worked example
(Appendix B.1):** the `Aggressive` archetype (Aggression 0.8, Caution 0.2) with the Appendix A.3
rows scores Gegenpress 0.66 vs Balanced 0.50 vs ParkTheBus −0.58 → selects Gegenpress; the
`Pragmatic` archetype (0.3/0.7) scores Balanced 0.50 top → selects Balanced.

## 3.4 In-match adaptation ladder (FM-TP-04; T4)

At each fired decision point (Mode == AI, `HoldIntervalsRemaining == 0`):

**Prerequisite (PASS-1 M-1):** `goalDiff` requires engine score state and `MATCH_TICKS_TOTAL` an
engine match-length model — neither exists today (no goal producer is wired; KD-2's own grep). The
formulas below are the reviewed contract; their live inputs arrive with goal detection (§7.2).
**CLOSED 2026-07-11:** the engine substrate landed — a Resolve-phase goal producer (score state,
`GoalAwardedEvent` 0x07, centre-spot restart) and the match-length model
(`MatchEngineConstants.MATCH_TICKS_TOTAL` / `HALF_TIME_BOUNDARY_TICK`). The engine's
decision-point seam now passes live `goalDiff`/`ticksRemaining`/`matchTicksTotal`, and the §3.2
half-time trigger is active (FR-TP-019). The ladder keeps its explicit-parameter signature (the
data assembly cannot reference the engine upward).

```
t01     = clamp01(ticksRemaining / MATCH_TICKS_TOTAL)      # 1 → full match left, 0 → final whistle
urgency = goalDiff < 0 ? Aggression × (1 − t01) × min(−goalDiff, URGENCY_DIFF_CAP) : 0
protect = goalDiff > 0 ? Caution    × (1 − t01) × min(goalDiff,  URGENCY_DIFF_CAP) : 0

target  = urgency ≥ ADAPT_STEP_THRESHOLD  ? StepToward(MoreAttacking, current)
        : protect ≥ ADAPT_STEP_THRESHOLD  ? StepToward(MoreDefensive, current)
        : current

if target ≠ current:
    apply via SetTeamTactic/SetPlayerTactic (FR-TP-005)
    CurrentPresetOrdinal = target
    HoldIntervalsRemaining = MANAGER_SWITCH_HOLD_INTERVALS × PatienceIntervals
```

`StepToward` walks one rung along the pinned attacking-order ladder (Appendix A.1 order:
ParkTheBus ← CounterAttack ← Balanced → Possession → Gegenpress; one step per decision, saturating
at the ends). One-rung stepping + hold intervals (FR-TP-011) make oscillation structurally
impossible within `2 × hold` windows.

- **Units/ranges:** `t01 ∈ [0,1]`; `urgency/protect ∈ [0, URGENCY_DIFF_CAP]`;
  `ADAPT_STEP_THRESHOLD` `[GT]` = 0.35; `URGENCY_DIFF_CAP` `[GT]` = 2 (a 3-goal deficit is not
  more urgent than 2 — it is lost); `MANAGER_SWITCH_HOLD_INTERVALS` `[GT]` = 2.
- **Worked example (Appendix B.2):** Aggressive profile (Aggression 0.8), 0–1 down at the 70'
  decision point (t01 = 20/90 ≈ 0.222): urgency = 0.8 × 0.778 × 1 = 0.622 ≥ 0.35 → step Balanced →
  Possession; holds 2 intervals (10 min); if still down at 80', steps Possession → Gegenpress.
  The Pragmatic profile (0.3) in the same state: urgency = 0.233 < 0.35 → no change.

## 3.5 Constants

| Constant | Tag | Value | Units |
|---|---|---|---|
| `MANAGER_DECISION_INTERVAL_TICKS` | `[GT]` | 18 000 | 60 Hz ticks (5 min) |
| `MANAGER_SWITCH_HOLD_INTERVALS` | `[GT]` | 2 | decision intervals |
| `ADAPT_STEP_THRESHOLD` | `[GT]` | 0.35 | — |
| `URGENCY_DIFF_CAP` | `[GT]` | 2 | goals |
| `BASE_FIT` / `AGGR_AFFINITY` / `CAUT_AFFINITY` tables | `[GT]` | Appendix A.3 | — |
| Ladder order | `[FIXED]` | Appendix A.1 | catalogue ordinal order — an ordering contract, not a tunable |
| `MATCH_TICKS_TOTAL` | `[CROSS]` | 324 000 | 60 Hz ticks; authoritative source: `MatchEngineConstants.MATCH_TICKS_TOTAL` (match-engine design note — allocated 2026-07-11, closing the PASS-1 M-1 gate; consumed as an explicit ladder parameter, never re-declared, since the #26 data assembly sits below the engine) |

`[GT]` magnitudes pinned at this spec's own balance review (§9.2); preset *contents* reuse #21
pinned values (KD-7) and add no magnitudes.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial FM-TP-01..04 with worked examples; ladder + hold = structural anti-churn. |
| 0.2 | 2026-07-08 | — | PASS-1 M-1: §3.4 prerequisite note + `MATCH_TICKS_TOTAL` added to §3.5 as `[CROSS-PENDING]` (was an untagged phantom). |
| 0.3 | 2026-07-11 | — | PASS-1 M-1 gates CLOSED: the engine substrate landed goal detection (score state, `GoalAwardedEvent`, centre-spot restart) + the match-length model; `MATCH_TICKS_TOTAL` promoted `[CROSS-PENDING]` → `[CROSS]` (authority `MatchEngineConstants.MATCH_TICKS_TOTAL` = 324 000); §3.2's half-time trigger and §3.4's live inputs are active in the engine. |
#endregion
