# Training System #29 — Section 3: Algorithms

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.3 — PASS-2 re-review; prior APPROVED)
**Version:** 0.3
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
    if s.LastAdvancedWorldDay != TRAINING_NOT_ADVANCED_SENTINEL:
        if worldDay <= s.LastAdvancedWorldDay:
            return                               # already advanced (no-op, F6)
        if worldDay > s.LastAdvancedWorldDay + 1:
            throw ArgumentException              # day gap — do NOT silently skip accrual (F7, FR-TR-026)

    # 1. Conditioning delta — deterministic function of focus + own attributes + coaching
    condDelta   = FocusConditionDelta[s.Focus]                       # Appendix A [GT] table
                + AttributeConditioningBonus(a)                      # deterministic, own-attribute (FR-TR-009)
    condDelta   = ApplyCoach(condDelta, coach)                       # ×1.0 at Identity (KD-3)
    s.Condition = Clamp(s.Condition + condDelta, CONDITION_MIN, CONDITION_MAX)   # F1

    # 2. Training-fatigue accrual = load − passive recovery — the WORLD-TICK accumulator (never the
    #    match counter, FR-TR-011). FATIGUE_DAILY_RECOVERY applies EVERY day regardless of focus, so a
    #    non-Rest regime reaches a sub-max equilibrium instead of saturating to 1.0 match-entry fatigue.
    fatDelta          = FocusFatigueDelta[s.Focus] - FATIGUE_DAILY_RECOVERY   # net; Rest ⇒ strongly negative
    fatDelta          = ApplyCoach(fatDelta, coach)
    s.TrainingFatigue = Clamp(s.TrainingFatigue + fatDelta, 0, TRAINING_FATIGUE_MAX)   # F1

    # 3. Advance the idempotency cursor
    s.LastAdvancedWorldDay = worldDay
```

`AttributeConditioningBonus` is a fixed deterministic map of e.g. `WorkRate`/`Stamina` (own-attribute) —
never RNG. `ApplyCoach` is `×1.0` under `CoachingModifier.Identity`, so a no-staff game is unaffected. The
step **fails loud on a day gap** rather than skipping days (F7): #30 advances one world day at a time, so a
gap is a caller bug, not a catch-up case. A player with no `TrainingState` (a regen not inserted per
FR-TR-025) is likewise a lifecycle bug — the state must be created via `TrainingState.Create` (F7).

**No rollover, no weekly batch:** the step is per-day and self-contained; "weekly" lives only in how often
the human calls `SetFocus`. There is no batch boundary to serialize beyond the normal per-day state (KD-4).

## 3.2 The growth-input read (`ComputeTrainingInput`, slot-1, pure)

```
ComputeTrainingInput(in TrainingState s, in PlayerAttributes a, in CoachingModifier coach,
                     bool deepTrainingEnabled) -> TrainingInput:
    if not deepTrainingEnabled:                   # a #29-OWNED Stage-2/Stage-3 dial (NOT #28's curveEnabled)
        return TrainingInput.Neutral              # KD-8 — #28's growth byte-identical to no-training
    # Stage-3 deep: a DETERMINISTIC per-attribute growth contribution weighted by focus + coaching.
    # Reads ONLY s.Focus (+ a, coach) — never s.Condition / s.TrainingFatigue (FR-TR-006 invariant).
    return BuildTrainingInput(s.Focus, a, coach)  # pure; no mutation of s; no RNG (FR-TR-006)
```

`ComputeTrainingInput` is a **read** — it never mutates `s`. #30 gathers each player's result into the
`trainingInputs` batch and hands it to #28's `AdvanceDay` at the slot-1 progression seam; the slot-2
`AdvanceTrainingDay` then evolves #29's own state the same world day. The slot-1 read is order-independent of
the slot-2 mutation **not because it is pure, but because it reads only fields `AdvanceTrainingDay` does not
mutate** (`Focus`, `a`, `coach`) — the load-bearing FR-TR-006 invariant. `deepTrainingEnabled` is #29's own
gate; #28's `curveEnabled` independently governs whether #28 realizes the non-neutral input (§2.1 FR-TR-007).

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

## 3.4 The injury-risk output (`ComputeInjuryRisk`, read)

```
ComputeInjuryRisk(in TrainingState s, in PlayerAttributes a) -> InjuryRiskContribution:
    risk = FatigueRiskWeight * s.TrainingFatigue
         + LowConditionRiskWeight * (CONDITION_MAX - s.Condition)
         - RobustnessMitigation(a)                # deterministic, own-attribute
    return { RiskScore = Clamp(risk, 0, INJURY_RISK_MAX) }
```

A read-only scalar; **#41** consumes it and owns occurrence/severity/recovery. #29 computes only the input
(KD-5). No RNG, no #41 interface.

## 3.5 Composition at #30's day-advance loop (informative)

#28's public daily entry point is the **batch** `AdvanceDay(worldDay, in trainingInputs)` (#28 FR-PG-021 —
one `TrainingInput` per player), not a per-player call. Per world day, for each club, in the club's
deterministic roster-iteration order:
1. **slot-1 (progression, #28):** gather each player's `growth[i] = ComputeTrainingInput(state[i], attrs[i],
   coach)` into the `trainingInputs` batch, then one `#28.AdvanceDay(worldDay, trainingInputs)`.
2. **slot-2 (training, #29):** `AdvanceTrainingDay(ref state[i], attrs[i], coach, worldDay)` per player.

Because `ComputeTrainingInput` reads only fields slot-2 does not mutate (FR-TR-006), the two slots are
order-independent; #30's documented slot order is unchanged (KD-2). The `TrainingState` set for each club is
maintained across the season boundary per FR-TR-025 (regen insert / retiree remove).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial algorithms. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | Single `Condition` cursor; no-RNG model; APPROVED. |
| 0.3 | 2026-07-23 | — | PASS-2: §3.1 day-gap fail-loud (F7) + `FATIGUE_DAILY_RECOVERY`; §3.2 `deepTrainingEnabled` param + field-independence invariant; §3.4 method renamed `ComputeInjuryRisk`; §3.5 rewritten to #28's batch `AdvanceDay(worldDay, in trainingInputs)` + FR-TR-025 lifecycle. |
#endregion
