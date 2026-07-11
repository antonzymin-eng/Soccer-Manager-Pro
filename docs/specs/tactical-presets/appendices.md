# Tactical Presets & AI-Manager Selection Specification #26 — Appendices

**Created:** July 8, 2026
**Last Updated:** July 10, 2026 (v0.3)
**Version:** 0.3
**Status:** APPROVED

---

## Appendix A — Stage-0+1 catalogue

### A.1 Presets (APPEND-only; ordinal = ladder position, defensive → attacking)

| Ord | Name | TeamTactic composition (all values verified #21 enum members / pins; non-listed dials stay at their `TeamTactic.Balanced` identity values) |
|---|---|---|
| 0 | ParkTheBus | `Mentality.VeryDefensive`, `TacticPressing.Low`, `LineOfEngagement.Low`, `DefensiveLine = 0.30`, `TimeWasting = 3`, `TransitionWon = TransitionPlan.HoldShape` |
| 1 | CounterAttack | `Mentality.Defensive`, `TransitionWon = TransitionPlan.CounterAttack`, `Tempo.Fast`, `TacticPassing.Direct`, `DefensiveLine = 0.40` |
| 2 | Balanced | `TeamTactic.Balanced` verbatim (the FR-TI-031 identity) |
| 3 | Possession | `Mentality.Positive`, `TacticPassing.Short`, `Tempo.Slow`, `TacticWidth.Wide`, `DefensiveLine = 0.55` |
| 4 | Gegenpress | `Mentality.Attacking`, `TacticPressing.High`, `LineOfEngagement.High`, `TransitionLost = TransitionPlan.CounterPress`, `DefensiveLine = 0.65` |

**Ladder order note (§3.4/§3.5):** the `StepToward` ladder *is* this ordinal order — pinned
`[FIXED]` as an ordering contract. `Balanced` at ordinal 2 is both the catalogue midpoint and the
kickoff default for a profile with no affinity. **Member names pinned July 10, 2026** against the
actual `src/tactical-instructions/` enums (PASS-1 L-2 close-out): every name above verified
present — `Mentality` {VeryDefensive=0..VeryAttacking=6}, `TacticPressing` {Low=0, Medium=1,
High=2}, `LineOfEngagement` {VeryLow=0..VeryHigh=4}, `Tempo` {VerySlow=0..VeryFast=4},
`TacticPassing` {Short=0, Mixed=1, Direct=2}, `TacticWidth` {VeryNarrow=0..VeryWide=4},
`TransitionPlan` {CounterAttack=0, HoldShape=1, CounterPress=2, Regroup=3}. The compositions are
complete at the dial level and add no new magnitudes (KD-7).

### A.2 `ManagerProfile` archetypes (`[GT]`)

| Archetype | Aggression | Caution | PatienceIntervals |
|---|---|---|---|
| Aggressive | 0.8 | 0.2 | 1 |
| Pragmatic | 0.3 | 0.7 | 2 |
| Balanced | 0.5 | 0.5 | 2 |

### A.3 Selection affinity rows (`[GT]`, bounded [−1, +1])

| Preset | BASE_FIT | AGGR_AFFINITY | CAUT_AFFINITY |
|---|---|---|---|
| ParkTheBus | −0.30 | −0.50 | 0.60 |
| CounterAttack | 0.10 | 0.10 | 0.30 |
| Balanced | 0.50 | 0.00 | 0.00 |
| Possession | 0.20 | 0.30 | −0.10 |
| Gegenpress | 0.10 | 0.80 | −0.40 |

## Appendix B — Worked examples (numerics for §3.3/§3.4; T-TP-U-005/006 lock these exactly)

### B.1 Kickoff selection

Aggressive (0.8/0.2): Gegenpress = 0.10 + 0.8×0.80 + 0.2×(−0.40) = 0.10 + 0.64 − 0.08 = **0.66**;
Balanced = 0.50 + 0 + 0 = 0.50; Possession = 0.20 + 0.24 − 0.02 = 0.42; CounterAttack = 0.10 +
0.08 + 0.06 = 0.24; ParkTheBus = −0.30 − 0.40 + 0.12 = −0.58 → **Gegenpress**.
Pragmatic (0.3/0.7): Balanced 0.50; Gegenpress 0.10 + 0.24 − 0.28 = 0.06; ParkTheBus −0.30 − 0.15
+ 0.42 = −0.03; CounterAttack 0.10 + 0.03 + 0.21 = 0.34; Possession 0.20 + 0.09 − 0.07 = 0.22 →
**Balanced**.

### B.2 Adaptation

Aggressive, 0–1 down, 70′ (t01 = 20/90 ≈ 0.222): urgency = 0.8 × 0.778 × 1 = **0.622** ≥ 0.35 →
step one rung attacking. Pragmatic same state: 0.3 × 0.778 × 1 = **0.233** < 0.35 → hold.

*(§3.3/§3.4's prose examples quote these exact values — B.1/B.2 are the authoritative full
derivations; T-TP-U-005/006 lock both surfaces to the same numbers.)*

## Appendix C — Snapshot field order (pinned before wiring, FR-TP-012)

Per team, team-index order: `Mode` (byte), `ProfileOrdinal` (byte), `CurrentPresetOrdinal` (byte),
`HoldIntervalsRemaining` (int32), `LastDecisionTick` (int32).

## Appendix D — FR traceability matrix (completed as tests land)

| FR | Tests |
|---|---|
| FR-TP-001/003 | T-TP-U-001 + mechanical audit |
| FR-TP-002/013 | T-TP-U-001/011/012 |
| FR-TP-004/005/010 | T-TP-I-001/002 |
| FR-TP-006/018/019 | T-TP-U-003/004 |
| FR-TP-007 | T-TP-U-004, T-TP-I-004, T-TP-DET-002 |
| FR-TP-008/009 | T-TP-U-005 + mechanical audit (signature admits no opponent input) |
| FR-TP-011 | T-TP-U-007/008 |
| FR-TP-012 | T-TP-I-005, T-TP-DET-003 |
| FR-TP-014 | T-TP-U-001 |
| FR-TP-020 | T-TP-U-010 |
| FR-TP-015/016/017 | mechanical/doc audits at PASS-1 + implementation AR |

## Appendix E — Sensitivity notes

- `ADAPT_STEP_THRESHOLD` vs archetype Aggression/Caution spans decide *which* managers ever adapt.
  Derived (PASS-1 L-1 corrected values): the Aggressive archetype (0.8) crosses 0.35 in a one-goal
  deficit when (1−t01) ≥ 0.35/0.8 = 0.4375, i.e. from ~39.4 match-minutes elapsed. The Pragmatic
  archetype (0.3) never crosses for a single goal ((1−t01) ≥ 1.167 is impossible) and at a
  two-goal deficit crosses when (1−t01) ≥ 0.35/0.6 = 0.5833, i.e. from ~52.5′ — a late-urgency
  profile, by design.
- `PatienceIntervals` doubling the hold makes Pragmatic switch at most ~4 times/match even if
  thresholds retune.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-08 | — | Initial appendices; B.1/B.2 derivations authoritative and aligned with §3.3/§3.4 prose. |
| 0.2 | 2026-07-08 | — | PASS-1 L-1: Appendix E sensitivity values re-derived (~39.4′ Aggressive one-goal; ~52.5′ Pragmatic two-goal; one-goal-never claim stands). |
| 0.3 | 2026-07-10 | — | A.1 member names pinned against the actual #21 enums (PASS-1 L-2 close-out); every composition value verified present; non-listed-dials-stay-identity rule made explicit. |
#endregion
