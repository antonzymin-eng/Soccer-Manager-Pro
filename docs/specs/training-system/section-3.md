# Training System #29 — Section 3: Algorithms

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.2 — APPROVED)
**Version:** 0.2
**Status:** APPROVED

---

All arithmetic is integer (cursors) or a pure integer→float projection (fatigue). No RNG (KD-6). Every step
is a pure function of serialized state, so save→restore is byte-exact.

## 3.1 The daily world-day step (`AdvanceTrainingDay`, slot-2)

```
AdvanceTrainingDay(ref TrainingState s, in PlayerAttributes a, in CoachingModifier coach, uint worldDay):
    # F6 idempotency — a day is advanced at most once. The sentinel is uint.MaxValue ("never
    # advanced"), NOT 0, so a legitimate world-day 0 cannot collide with the fresh-state value
    # (the day-0 double-accrual trap). TrainingState.Create seeds the sentinel.
    if s.LastAdvancedWorldDay != TRAINING_NOT_ADVANCED_SENTINEL and worldDay <= s.LastAdvancedWorldDay:
        return                                   # already advanced (no-op)

    # 1. Conditioning delta — deterministic function of focus + own attributes + coaching
    condDelta   = FocusConditionDelta[s.Focus]                       # Appendix A [GT] table
                + AttributeConditioningBonus(a)                      # deterministic, own-attribute (FR-TR-009)
    condDelta   = ApplyCoach(condDelta, coach)                       # ×1.0 at Identity (KD-3)
    s.Condition = Clamp(s.Condition + condDelta, CONDITION_MIN, CONDITION_MAX)   # F1

    # 2. Training-fatigue accrual — the WORLD-TICK accumulator (never the match counter, FR-TR-011)
    fatDelta          = FocusFatigueDelta[s.Focus]                   # Rest is negative (recovery)
    fatDelta          = ApplyCoach(fatDelta, coach)
    s.TrainingFatigue = Clamp(s.TrainingFatigue + fatDelta, 0, TRAINING_FATIGUE_MAX)   # F1

    # 3. Advance the idempotency cursor
    s.LastAdvancedWorldDay = worldDay
```

`AttributeConditioningBonus` is a fixed deterministic map of e.g. `WorkRate`/`Stamina` (own-attribute) —
never RNG. `ApplyCoach` is `×1.0` under `CoachingModifier.Identity`, so a no-staff game is unaffected.

**No rollover, no weekly batch:** the step is per-day and self-contained; "weekly" lives only in how often
the human calls `SetFocus`. There is no batch boundary to serialize beyond the normal per-day state (KD-4).

## 3.2 The growth-input read (`ComputeTrainingInput`, slot-1, pure)

```
ComputeTrainingInput(in TrainingState s, in PlayerAttributes a, in CoachingModifier coach) -> TrainingInput:
    if not CurveEnabled:                          # Stage-2 minimal / dial off
        return TrainingInput.Neutral              # KD-8 — #28's curve byte-identical to no-training
    # Stage-3 deep: a DETERMINISTIC per-attribute growth contribution weighted by focus + coaching.
    return BuildTrainingInput(s.Focus, a, coach)  # pure; no mutation of s; no RNG (FR-TR-006)
```

`ComputeTrainingInput` is a **read** — it never mutates `s`. #30 calls it at the slot-1 progression seam and
hands the result to #28's `GrowthProjection`; the slot-2 `AdvanceTrainingDay` then evolves #29's own state
the same world day. Because the read is pure, there is no ordering hazard and no staleness (KD-2).

## 3.3 The match-entry-fatigue projection (`ProjectMatchEntryFatigue`, pure)

```
ProjectMatchEntryFatigue(in TrainingState s) -> float in [0,1]:
    # One-directional: world-tick training-fatigue → match-boot STARTING fatigue offset.
    return Clamp01( (float)s.TrainingFatigue / TRAINING_FATIGUE_MAX * MATCH_ENTRY_FATIGUE_SCALE )
```

The match-boot caller (the future integration / #30 fixture-day path) reads this and passes it as the
`float fatigue` argument the `PlayerAttributeProjection.To*` seams already accept (KD-P4). **Match-tick
fatigue (`1 − AerobicPool`) never writes back**, and #29 never touches `AerobicPool`. The projection is
**not stored** — recomputed from the serialized accumulator — so it is identical before and after a
save→restore (KD-1). The two counters live in distinct fields in distinct assemblies, so no double-count
path exists.

## 3.4 The injury-risk output (`InjuryRiskContribution`, read)

```
InjuryRiskContribution(in TrainingState s, in PlayerAttributes a) -> InjuryRiskContribution:
    risk = FatigueRiskWeight * s.TrainingFatigue
         + LowConditionRiskWeight * (CONDITION_MAX - s.Condition)
         - RobustnessMitigation(a)                # deterministic, own-attribute
    return { RiskScore = Clamp(risk, 0, INJURY_RISK_MAX) }
```

A read-only scalar; **#41** consumes it and owns occurrence/severity/recovery. #29 computes only the input
(KD-5). No RNG, no #41 interface.

## 3.5 Composition at #30's day-advance loop (informative)

Per world day, for each club, in the club's deterministic roster-iteration order:
1. **slot-1 (progression, #28):** `growth = ComputeTrainingInput(state, attrs, coach)`; then
   `#28.GrowthProjection.Step(ref lifecycle, ref attrs, worldDay, growth)`.
2. **slot-2 (training, #29):** `AdvanceTrainingDay(ref state, attrs, coach, worldDay)`.

Both slots are pure/deterministic; #30's documented order is unchanged (KD-2).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial algorithms. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | Single `Condition` cursor; no-RNG model; APPROVED. |
#endregion
