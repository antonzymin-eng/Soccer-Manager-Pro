# Injuries & Medical #41 — Section 3: Algorithms

**Created:** July 23, 2026
**Last Updated:** August 8, 2026 (v0.8 — balance-pass AR pass 9 L4: §3.1's pseudocode gains the F8 sentinel-as-worldDay refusal the code has always enforced)
**Last Updated (prior):** August 8, 2026 (v0.7 — balance-pass AR pass 8: the missing v0.6 version row added (L1); the §3.1.1 transfer note separated from the radix rule it had been fused into (L5))
**Last Updated (prior):** August 8, 2026 (v0.6 — balance-pass AR pass 4: §3.1.1 records the TRANSFER residual beside the club-term refusal it qualifies (the only cross-club handoff path today RESETS a moved player's career state — worse than the luck-change the refusal prices; #31's arrival obligation, recorded at the code site too); §3.2's overflow bound corrected to 1.6×10⁷ after the pass-1 headroom raise. This header was also STALE at v0.4 while the table below carried v0.5 — the header-drift class #30 §3's history records; pass 1's 1% → 1.6% edit also shipped without a bump, folded into v0.5's row note.)
**Last Updated (prior):** August 8, 2026 (v0.5 — AR pass 3: §3.1's signature de-phantomed — `rng` → `worldSeed, occurrenceEnabled`, the dial gated in step 2, §3.5's call updated; §3.1.1 gains the ERR-041-019 draw-key global-uniqueness contract)
**Last Updated (prior):** August 7, 2026 (v0.4 — ERR-041-011 at the balance pass: §3.4 gains the normative-position `BASELINE_DAILY_RISK` term; the draw denominator decouples to the `[FIXED]` per-million `OCCURRENCE_DRAW_DENOM` with the `INJURY_RISK_MAX ≤ DENOM` invariant; §3.1's pseudocode re-anchored onto the keyed derivation (ERR-041-002/ERR-041-012); §3.6 re-derived (6600, + the congestion-clamp line))
**Last Updated (prior):** July 23, 2026 (v0.3 — AR-2 fixed-radix append-parity; prior v0.2 AR-1 integer fix, v0.1 initial)
**Version:** 0.8
**Status:** APPROVED

---

All cursor arithmetic is integer. The one stochastic surface is a **single, keyed, position-independent**
draw per player per world day (KD-1) — there is no free-running cursor, so save→restore needs nothing
beyond `InjuryState` itself. Severity classification at Stage 2 re-uses that same draw (a deterministic
bucketing), so an occurrence day consumes exactly **one** draw total.

## 3.1 The daily world-day step (`AdvanceMedicalDay`)

```
AdvanceMedicalDay(ref InjuryState s, playerId, in PlayerAttributes a, in InjuryRiskContribution trainingRisk,
                  in MatchLoad recentMatchLoad, in MedicalModifier medical, worldDay, worldSeed,
                  occurrenceEnabled):
    # worldSeed: the CAREER's world seed (WorldStore.WorldSeed) — the draw key's root, never a
    # per-match seed. occurrenceEnabled: the FR-MD-027 dial, a REQUIRED never-defaulted argument
    # of the step itself (§2 FR-MD-027 as revised at the balance pass).
    # F8 — the sentinel itself is not a day: refused outright, BEFORE the cursor checks.
    # Stored, it would read back as "never advanced" and re-arm the day-0 double-accrual
    # trap F6 exists to close.
    if worldDay == MEDICAL_NOT_ADVANCED_SENTINEL:
        throw ArgumentException                  # sentinel is a reserved value, not a day (F8)

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

    # 2. Occurrence draw — evaluated ONLY for a player healthy at call entry (§3.1 KD-6 guarantee),
    #    and ONLY with the FR-MD-027 dial armed: disarmed, the step is the recovery countdown and
    #    the cursor advance alone (the FR-MD-027 identity).
    if wasAvailableAtEntry and occurrenceEnabled:
        risk = AssembleRiskScore(trainingRisk, recentMatchLoad, a, medical)   # §3.4; in [0, INJURY_RISK_MAX]
        actionOrdinal = DeriveActionOrdinal(worldDay, DRAW_PURPOSE_OCCURRENCE)     # §3.1.1
        # ERR-041-002 (re-anchored at ERR-041-011): the draw is a LOCAL KEYED DERIVATION, not a
        # registered-stream call — #16 exposes no keyed-draw API and a registered stream is
        # cursor-positioned, which KD-1/FR-MD-007 forbid. DrawOccurrence folds
        # DOMAIN_TAG_INJURIES_MEDICAL, then playerId, then actionOrdinal, each through a SplitMix64
        # finalizer, reduced modulo the [FIXED] denominator:
        draw = DrawOccurrence(worldSeed, playerId, actionOrdinal)                  # in [0, OCCURRENCE_DRAW_DENOM)
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
persist (FR-MD-006/007).

**The full draw key is `(worldSeed, playerId, actionOrdinal)` — there is NO club term, so it requires
`PlayerId` to be GLOBALLY unique across the career (ERR-041-019).** That is a stronger promise than #27
makes: the squad/player data layer scopes `PlayerId` uniqueness to a club (its KD-3), and #30's career
state is keyed `(ClubId, PlayerId)` on exactly that premise. Two clubs carrying the same id would draw
bit-identical injury luck on every world day forever — silent and indistinguishable from chance. Today's
`RosterGenerator` allocation (`clubId × CLUB_SQUAD_SIZE + local`) happens to be globally unique, but that
is an accident of one allocator, not a contract; the precondition is therefore enforced fail-loud at
career construction and roster sync (`PlayerCareerStates`, the one layer that spans clubs), and any
future id allocator (#42 youth intake, #31 transfers) MUST preserve it. Deliberately NOT fixed by adding
`ClubId` to the key: the key is frozen by the same argument that pinned the denominator `[FIXED]` at
ERR-041-011 — changing it re-rolls every career's injury luck — and a club term would additionally make a
transferred player's luck change with his club, which FR-MD-006's "the player carries his medical
identity" posture refuses.

**Recorded, not fixed (AR pass 4):** the only cross-club handoff path that exists today —
`PlayerCareerStates`' per-club roster reconciliation — does WORSE than change a moved player's luck: it
resets his career state entirely (departure at the old club, `Create()` at the new one — conditioning,
injury history and any active injury gone; he arrives fit). Inert while no allocator moves players
between clubs, and deliberately not fixed here: carrying state across clubs is #31 Transfers' arrival
obligation (ERR-041-019's global-id guarantee is what makes it implementable — one pre-pass keyed on the
now-unique id), and the code site carries the same record.

The radix is the **FIXED** constant `DRAW_PURPOSE_RADIX` (Appendix A) — **not**
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
    # float division): draw/risk < n/1000  ⇔  draw*1000 < risk*n. draw < risk <= INJURY_RISK_MAX here
    # (an occurrence was confirmed), so products are bounded by INJURY_RISK_MAX × 1000 = 1.6 × 10^7 (10^7 before the balance-pass AR raised the ceiling to 16,000); the
    # implementation widens to long so a raised [GT] ceiling cannot silently overflow (ERR-041-011).
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
         + APPEARANCE_LOAD_WEIGHT * load.AppearanceDays                    # Stage-2 match-load term (FR-MD-010 window count)
         + HARD_CONTACT_WEIGHT * load.HardContacts                         # 0 at Stage 2 (deep-tier only)
         + BASELINE_DAILY_RISK                                             # exposure-independent floor (ERR-041-011)
         - RobustnessMitigation(a)                                         # deterministic, own-attribute
    risk = risk * medical.OccurrenceRiskMillMult / 1000                    # per-mille; ×1.0 at Identity (KD-5)
    return Clamp(risk, 0, INJURY_RISK_MAX)
```

**`BASELINE_DAILY_RISK`'s position is normative** (ERR-041-011): it sits **inside the sum, before the
mitigation**, so robustness discriminates the exposure-independent floor — a frail player's quiet week
is riskier than an iron man's — and, because #27 attributes floor at 1 and the default magnitudes keep
`BASELINE_DAILY_RISK` above the largest mitigation row, no valid-input player is ever injury-proof (the
third absurdity the T0 fifth AR pass measured: the default focus converged on exactly-0-forever). It is
the exposure-INDEPENDENT term: the research-alignment supplement's R-2 under-exposure arm must re-fit
against it rather than add beside it, or the left tail is priced three times (its §10 concern).

`RobustnessMitigation` is a fixed deterministic map over existing #27 physical attributes (e.g. `Strength` /
`Stamina` / `Balance` — never RNG, FR-MD-015); a dedicated `InjuryProneness` attribute is a recorded deep-tier
#27 append, not consumed here. `trainingRisk` is read-only (KD-2, FR-MD-009); `load` is a value the caller
supplies (FR-MD-010) — #41 never tracks match participation itself. The result is clamped to
`[0, INJURY_RISK_MAX]`, and §3.1 tests `draw < risk` against a draw uniform in the **`[FIXED]`
`OCCURRENCE_DRAW_DENOM` = 1,000,000** — so the assembled score IS the daily probability numerator on a
per-million scale, capped at `INJURY_RISK_MAX / OCCURRENCE_DRAW_DENOM` (1.6% at today's values — the ceiling was raised 10000 → 16000 at the balance-pass AR so one appearance plus the baseline, 9,600, leaves the #29 and robustness terms real range instead of compressing them into the top 4% of the clamp; 16000 stays below #29's ~19,960 unclamped producer maximum, so the clamp still binds).
**The denominator is deliberately DECOUPLED from the `[GT]` ceiling** (ERR-041-011, retiring the old
`OCCURRENCE_DRAW_DENOM == INJURY_RISK_MAX` identity): the draw is `hash % denominator`, so the
denominator determines the VALUE of every draw, not merely a threshold — a config-tunable denominator
would re-roll every career's injury luck on a config edit, with the save recording nothing about which
config produced it. Pinned, config edits move only thresholds. Invariant: `INJURY_RISK_MAX ≤
OCCURRENCE_DRAW_DENOM`, enforced fail-loud at the draw site (a ceiling past the denominator would make
a clamped risk mean "certain and then some").

## 3.5 Composition at #30's day-advance loop (informative)

Per world day, for each club, in the club's deterministic roster-iteration order, at the **new** slot KD-6
pins (after #28/#29/#33, before `WorldStore.AdvanceDay()`):

```
for each playerId in club roster:
    trainingRisk    = TrainingSystem.ComputeInjuryRisk(trainingState[playerId], attrs[playerId])   # #29 read
    recentMatchLoad = ... caller-supplied MatchLoad (Stage 2: AppearanceDays from #30's per-player
                          appearance record — the FR-MD-010 window count; ERR-041-010(b)) ...
    AdvanceMedicalDay(ref medicalState[playerId], playerId, attrs[playerId], trainingRisk,
                       recentMatchLoad, medical, worldDay, worldSeed, occurrenceEnabled)
```

Because this slot runs strictly after #29's own slot-2 `AdvanceTrainingDay` (per #30's KD-2 tick order),
`trainingRisk` reflects the **day's updated** training-fatigue / condition, not a one-day-stale value (the
KD-6 ordering rationale, §1.4). The `InjuryState` set for each club is maintained across the season boundary
per FR-MD-025 (regen insert / retiree remove).

## 3.6 Worked example

Player 501, world day 205: `TrainingRiskContribution.RiskScore = 3000`, `MatchLoad.AppearanceDays = 0`
(no match in the FR-MD-010 window), mean robustness attribute `14` (`RobustnessMitigation(14) = 400`),
`BASELINE_DAILY_RISK = 4000`, `MedicalModifier.Identity`. *(Re-derived at ERR-041-011; the pre-balance-
pass example used `AppearanceDays = 2` at weight 150 and no baseline, assembling 2900.)*

- `risk = 1×3000 + 0 + 4000 − 400 = 6600` (× `OccurrenceRiskMillMult 1000 / 1000` = unchanged; clamp
  within `[0, 16000]` inactive) — a 0.66%/day probability against the per-million draw. *(The same
  player the week after two matches assembles `3000 + 2×5600 + 4000 − 400 = 17800`, which CLAMPS to
  `INJURY_RISK_MAX = 16000`: a heavily-loaded congested week sits at the hard 1.6%/day ceiling —
  what sits beyond the cap is the residual the research supplement's R-2 refit inherits. A formula
  probe, not live Stage-0 behaviour: with `DaysBetweenRounds` = `APPEARANCE_WINDOW_DAYS` = 7 the
  wired schedule never yields `AppearanceDays = 2` — that input arrives with #43's congested cup
  calendars.)*
- Suppose `draw = 3500` (keyed on `(playerId=501, worldDay=205, purpose=Occurrence)`). Since `3500 < 6600`,
  an occurrence is confirmed. Integer bucketing: `draw×1000 = 3_500_000` vs `risk×SEVERITY_MINOR_PERMILLE =
  6600×600 = 3_960_000`; `3_500_000 < 3_960_000` ⇒ **Minor**. `RecoveryDaysForTier[Minor] = 7`; at
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
| 0.4 | 2026-08-07 | — | **ERR-041-011 / ERR-041-012 (the balance pass)**: §3.4 formula gains `BASELINE_DAILY_RISK` inside the sum before the mitigation (position normative — robustness discriminates the floor; kills the exactly-0-forever default); the `OCCURRENCE_DRAW_DENOM == INJURY_RISK_MAX` identity retired — denominator `[FIXED]` at 1,000,000, ceiling stays the `[GT]` clamp (1.6%/day since the balance-pass AR raised it 10,000 → 16,000; this row originally said 1%/day and was corrected like its appendices counterpart), invariant enforced at the draw site; §3.1 pseudocode shows the real keyed derivation instead of the phantom `rng.DrawKeyed`; §3.2's bound note updated; §3.5 names the ERR-041-010(b) appearance record; §3.6 worked example re-derived (6600; congestion clamps at the ceiling). |
| 0.5 | 2026-08-08 | — | **Balance-pass AR pass 3 (M5 + H1)**: §3.1's normative signature de-phantomed — `rng` (which the body never used) becomes `worldSeed, occurrenceEnabled`, and step 2 gates on `wasAvailableAtEntry and occurrenceEnabled` (FR-MD-027 is a required parameter of the step, so the algorithm that governs the armed subsystem now names the dial — the ERR-041-012 class recurring one section away); §3.5's composition call updated to match. **§3.1.1 gains the draw-key uniqueness contract (ERR-041-019)**: the key has no club term, so `PlayerId` must be GLOBALLY unique — stronger than #27's club-scoped KD-3 — enforced fail-loud at `PlayerCareerStates` construction/sync; a club term in the key is refused (re-rolls every career; a transfer would change a player's luck). v0.4's "1%/day" corrected in place to 1.6%. |
| 0.6 | 2026-08-08 | — | **Balance-pass AR pass 4 (M4/M5/L6)**: §3.1.1 gains the transfer-reset RECORDED-NOT-FIXED residual beside the club-term refusal it qualifies; §3.2's overflow bound corrected to 1.6×10⁷; header currency repaired. (Row added at AR pass 8 — the v0.6 edit shipped rowless, the class this chain keeps meeting; pass 5's §3.6 congestion formula-probe note also rides under this version.) |
| 0.7 | 2026-08-08 | — | **Balance-pass AR pass 8 (L1 + L5)**: the v0.6 row itself (added here — the version existed only in the header); the §3.1.1 paragraph break separating the pass-4 transfer note from the DRAW_PURPOSE_RADIX rule that had come to read as its continuation. |
| 0.8 | 2026-08-08 | — | **Balance-pass AR pass 9 (L4)**: §3.1's pseudocode gains the `worldDay == MEDICAL_NOT_ADVANCED_SENTINEL` refusal (**F8**, new in §2.3) that `MedicalStep.AdvanceMedicalDay` has enforced since T0 with no normative source — a production fail-loud with no spec row is the ERR-041-012 class inverted. Mirrored at the #29 sibling (`training-system` §2.3/§3.1) in the same commit — the folder-boundary lesson applied forward. |
#endregion
