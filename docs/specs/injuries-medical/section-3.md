# Injuries & Medical #41 — Section 3: Algorithms

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.3 — AR-2 fixed-radix append-parity; prior v0.2 AR-1 integer fix, v0.1 initial)
**Version:** 0.3
**Status:** APPROVED

---

All cursor arithmetic is integer. The one stochastic surface is a **single, keyed, position-independent**
draw per player per world day (KD-1) — there is no free-running cursor, so save→restore needs nothing
beyond `InjuryState` itself. Severity classification at Stage 2 re-uses that same draw (a deterministic
bucketing), so an occurrence day consumes exactly **one** draw total.

## 3.1 The daily world-day step (`AdvanceMedicalDay`)

```
AdvanceMedicalDay(ref InjuryState s, playerId, in PlayerAttributes a, in InjuryRiskContribution trainingRisk,
                  in MatchLoad recentMatchLoad, in MedicalModifier medical, worldDay, rng):
    # F6/F7 idempotency — a day is advanced at most once. The sentinel is uint.MaxValue ("never
    # advanced"), NOT 0, so a legitimate world-day 0 cannot collide with the fresh-state value
    # (the day-0 double-accrual trap). InjuryState.Create seeds the sentinel.
    if s.LastAdvancedWorldDay != MEDICAL_NOT_ADVANCED_SENTINEL:
        if worldDay <= s.LastAdvancedWorldDay:
            return                               # already advanced (no-op, F6)
        if worldDay > s.LastAdvancedWorldDay + 1:
            throw ArgumentException              # day gap — do NOT silently skip (F7)

    # KD-6 ordering guarantee: capture entry-state BEFORE the countdown mutates it. The occurrence
    # draw below is gated on THIS captured value, not on s.Severity after the countdown runs — so a
    # player whose recovery completes THIS call cannot also be re-injured THIS call.
    wasAvailableAtEntry = (s.Severity == InjurySeverity.None)

    # 1. Recovery countdown — runs FIRST (KD-6), only while currently injured. Fixed INTEGER decrement
    #    (RECOVERY_DAYS_PER_TICK_BASE = 1); staff recovery-speed is NOT applied here (it is applied to the
    #    assigned tier-days at injury time in step 2 — FR-MD-014 — so a fractional multiplier is never
    #    truncated against a base of 1).
    if s.Severity != InjurySeverity.None:
        s.RecoveryRemaining = Clamp(s.RecoveryRemaining - RECOVERY_DAYS_PER_TICK_BASE, 0, RECOVERY_MAX)  # F1
        if s.RecoveryRemaining == 0:
            s.Severity = InjurySeverity.None      # recovered — but ineligible for a NEW occurrence
                                                   # until the NEXT AdvanceMedicalDay call (see above)

    # 2. Occurrence draw — evaluated ONLY for a player healthy at call entry (§3.1 KD-6 guarantee).
    if wasAvailableAtEntry:
        risk = AssembleRiskScore(trainingRisk, recentMatchLoad, a, medical)   # §3.4; in [0, INJURY_RISK_MAX]
        actionOrdinal = DeriveActionOrdinal(worldDay, DRAW_PURPOSE_OCCURRENCE)     # §3.1.1
        draw = rng.DrawKeyed(STREAM_INJURIES_OCCURRENCE, entityId: playerId,
                              actionOrdinal: actionOrdinal, drawIndex: 0)          # in [0, OCCURRENCE_DRAW_DENOM)
        if draw < risk:                            # occurrence — the SAME draw also classifies severity (§3.2)
            severity = ClassifySeverityFromDraw(draw, risk)                       # §3.2 — NO second draw
            s.Severity           = severity
            # staff recovery-speed applied ONCE here (integer), not per-tick (FR-MD-014). Floor at 1 so a
            # confirmed injury always has >= 1 recovery-day — an aggressive multiplier must never divide the
            # assigned days to 0, which would leave RecoveryRemaining == 0 while Severity != None (F1 breach):
            s.RecoveryRemaining  = Max(1, RecoveryDaysForTier[severity] * 1000 / medical.RecoverySpeedMillMult)
            s.InjuryCount       += 1

    # 3. Advance the idempotency cursor
    s.LastAdvancedWorldDay = worldDay
```

A player with no `InjuryState` (a regen never inserted per FR-MD-025) is a lifecycle bug — the state must
exist via `InjuryState.Create` before this is ever called (F2). The step **fails loud on a day gap** rather
than skipping days (F7): #30 advances one world day at a time, so a gap is a caller bug, not a catch-up
case.

### 3.1.1 Deriving the action ordinal (KD-1)

```
DeriveActionOrdinal(worldDay, purpose) -> u64:
    assert 0 <= purpose < DRAW_PURPOSE_RADIX                 # bound guard (fail loud, F4-class)
    return (u64)worldDay * DRAW_PURPOSE_RADIX + (u64)purpose
```

A pure bijection from `(worldDay, purpose)` to a single `u64` — **not** an incrementing counter. Two calls
with the same `(playerId, worldDay, purpose)` always resolve to the same draw regardless of call order
across players or days, which is what makes the stream position-independent and gives it nothing to
persist (FR-MD-006/007). The radix is the **FIXED** constant `DRAW_PURPOSE_RADIX` (Appendix A) — **not**
the current purpose count — so appending a Stage-3 purpose ordinal (`DRAW_PURPOSE_OCCURRENCE = 0` today; a
future recurrence draw = 1, …) leaves **every existing** `(worldDay, Occurrence)` ordinal unchanged
(`worldDay × RADIX + 0`), preserving cross-version replay/save parity. Using the growing purpose *count* as
the radix would shift all prior ordinals when a purpose is appended — the exact hazard the **APPEND-only**
rule (FR-MD-008) exists to prevent; a fixed radix is what makes append-only actually parity-safe. `purpose`
MUST stay `< DRAW_PURPOSE_RADIX` (a catalogue invariant).

## 3.2 Severity classification (`ClassifySeverityFromDraw`, Stage 2)

```
ClassifySeverityFromDraw(draw, risk) -> InjurySeverity:
    # draw is already known to be < risk here (an occurrence was confirmed in §3.1). Bucket the SAME
    # draw value deterministically by FIXED proportions (Appendix A) — this is NOT a second RNG draw;
    # KD-1 draws exactly once per player per occurrence-eligible day. INTEGER cross-multiplication (no
    # float division): draw/risk < n/1000  ⇔  draw*1000 < risk*n. Products are bounded well within int
    # range (draw,risk <= INJURY_RISK_MAX = 10000 ⇒ <= 10^7); use a widening (long) product to be safe.
    if draw * SEVERITY_PERMILLE_DENOM < risk * SEVERITY_MINOR_PERMILLE:
        return InjurySeverity.Minor
    elif draw * SEVERITY_PERMILLE_DENOM < risk * (SEVERITY_MINOR_PERMILLE + SEVERITY_MODERATE_PERMILLE):
        return InjurySeverity.Moderate
    else:
        return InjurySeverity.Serious
```

Stage-3 (deep tier, deferred) replaces this fixed bucketing with a distribution-driven severity model and
adds recurrence risk on early return; both default to the Stage-2 fixed-tier / no-recurrence behaviour
under `deepMedicalEnabled` off (KD-4/FR-MD-013) — one code path, not a fork.

## 3.3 Recovery-speed modulation

`RECOVERY_DAYS_PER_TICK_BASE` (Appendix A) is the Stage-2 linear per-day decrement (a **fixed integer** — 1
day of `RecoveryRemaining` consumed per world day). Staff recovery-speed is **not** a per-tick multiplier
(against a base of 1 an integer multiply would truncate every fractional rate to a no-op); instead
`MedicalModifier.RecoverySpeedMillMult` scales the **assigned tier recovery-days once at injury time**
(§3.1 step 2, floored at 1), so a faster physio assigns fewer total days and the countdown stays a clean
integer 1/day. `MedicalModifier.Identity` is per-mille `1000` = ×1.0, so a no-staff game recovers in exactly
the severity tier's recovery-days constant. No RNG and no float is involved in recovery — a deterministic
integer countdown (FR-MD-014).

## 3.4 The risk-score assembly (`AssembleRiskScore`, pure)

```
AssembleRiskScore(in InjuryRiskContribution trainingRisk, in MatchLoad load, in PlayerAttributes a,
                   in MedicalModifier medical) -> int:
    # All terms and weights are INTEGER (no float — FR-MD-014).
    risk = TRAINING_RISK_PASSTHROUGH_WEIGHT * trainingRisk.RiskScore        # #29's already-published scalar (weight = 1)
         + APPEARANCE_LOAD_WEIGHT * load.AppearanceDays                    # Stage-2 match-load term
         + HARD_CONTACT_WEIGHT * load.HardContacts                         # 0 at Stage 2 (deep-tier only)
         - RobustnessMitigation(a)                                         # deterministic, own-attribute
    risk = risk * medical.OccurrenceRiskMillMult / 1000                    # per-mille; ×1.0 at Identity (KD-5)
    return Clamp(risk, 0, INJURY_RISK_MAX)
```

`RobustnessMitigation` is a fixed deterministic map over existing #27 physical attributes (e.g. `Strength` /
`Stamina` / `Balance` — never RNG, FR-MD-015); a dedicated `InjuryProneness` attribute is a recorded deep-tier
#27 append, not consumed here. `trainingRisk` is read-only (KD-2, FR-MD-009); `load` is a value the caller
supplies (FR-MD-010) — #41 never tracks match participation itself. The result is clamped to the same
`[0, INJURY_RISK_MAX]` scale the occurrence draw compares against (§3.1), so `OCCURRENCE_DRAW_DENOM ==
INJURY_RISK_MAX` (Appendix A) and no extra scale factor is needed between the assembled score and the draw.

## 3.5 Composition at #30's day-advance loop (informative)

Per world day, for each club, in the club's deterministic roster-iteration order, at the **new** slot KD-6
pins (after #28/#29/#33, before `WorldStore.AdvanceDay()`):

```
for each playerId in club roster:
    trainingRisk    = TrainingSystem.ComputeInjuryRisk(trainingState[playerId], attrs[playerId])   # #29 read
    recentMatchLoad = ... caller-supplied MatchLoad (Stage 2: AppearanceDays from #30's fixture result) ...
    AdvanceMedicalDay(ref medicalState[playerId], playerId, attrs[playerId], trainingRisk,
                       recentMatchLoad, medical, worldDay, rng)
```

Because this slot runs strictly after #29's own slot-2 `AdvanceTrainingDay` (per #30's KD-2 tick order),
`trainingRisk` reflects the **day's updated** training-fatigue / condition, not a one-day-stale value (the
KD-6 ordering rationale, §1.4). The `InjuryState` set for each club is maintained across the season boundary
per FR-MD-025 (regen insert / retiree remove).

## 3.6 Worked example

Player 501, world day 205: `TrainingRiskContribution.RiskScore = 3000`, `MatchLoad.AppearanceDays = 2`
(`APPEARANCE_LOAD_WEIGHT = 150`), mean robustness attribute `14` (`RobustnessMitigation(14) = 400`),
`MedicalModifier.Identity`.

- `risk = 1×3000 + 150×2 − 400 = 2900` (× `OccurrenceRiskMillMult 1000 / 1000` = unchanged; clamp within
  `[0, 10000]` inactive).
- Suppose `draw = 1500` (keyed on `(playerId=501, worldDay=205, purpose=Occurrence)`). Since `1500 < 2900`,
  an occurrence is confirmed. Integer bucketing: `draw×1000 = 1_500_000` vs `risk×SEVERITY_MINOR_PERMILLE =
  2900×600 = 1_740_000`; `1_500_000 < 1_740_000` ⇒ **Minor**. `RecoveryDaysForTier[Minor] = 7`; at
  `RecoverySpeedMillMult = 1000`, `RecoveryRemaining = max(1, 7×1000/1000) = 7`.
- `InjuryState`: `Severity = Minor`, `RecoveryRemaining = 7`, `InjuryCount += 1`, `LastAdvancedWorldDay =
  205`.
- Day 206: `wasAvailableAtEntry = false` (still `Minor`). Countdown: `RecoveryRemaining = 6`. No occurrence
  draw this day (KD-6 gate).
- … Day 212: entry `RecoveryRemaining = 1`, countdown brings it to `0` ⇒ `Severity = None` this call. Still
  `wasAvailableAtEntry = false` (captured **before** the countdown), so **no** occurrence draw fires this
  same day — the KD-6 guarantee in effect.
- Day 213: `wasAvailableAtEntry = true` (healthy all day). Occurrence draw evaluated normally again, keyed
  on `(501, 213, Occurrence)`.
- A save taken after day 212 and restored resumes with `InjuryState { None, 0, 1, 212 }` field-identical;
  advancing day 213 post-restore reproduces the exact same draw as an uninterrupted run (KD-1 — nothing was
  skipped because nothing was ever a running cursor).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial algorithms: `AdvanceMedicalDay`, action-ordinal derivation, severity bucketing, risk-score assembly, composition, worked example. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | AR-1 (1M): integer-arithmetic fix — no float division; integer per-mille severity bucketing; recovery-speed applied once at injury assignment (floored at 1 for F1 coherence); per-mille occurrence-risk mult; worked example redone in integer form. |
| 0.3 | 2026-07-23 | — | AR-2 (1M): §3.1.1 `DeriveActionOrdinal` uses the fixed `DRAW_PURPOSE_RADIX` (was the growing purpose count, which broke cross-version replay parity on append) + a purpose bound guard. |
#endregion
