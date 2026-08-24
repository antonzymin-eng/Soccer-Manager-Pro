# Injuries & Medical #41 — Section 3: Algorithms

**Created:** July 23, 2026
**Last Updated:** August 24, 2026 (v0.19 — round-2 M/L pass, L5: §3.4 gains a bullet stating the age
term's ordering dependency on #30's KD-2 tick order — today normative only in a `season-save` code
comment — and its consequence if the order ever inverts (~150-per-million per affected player,
invisible to every existing lock). The season-save test lock and a #30 KD-2 slot-table row are
recorded as out of scope for this pass.)
**Last Updated (prior):** August 22, 2026, later same day (v0.18 — **ERR-041-021** (adversarial review over the ERR-041-020 landing, H4 + H7), spec + code in the same commit. **H4:** §3.4's normative-position wording for BOTH `AgeRiskFor` and `BASELINE_DAILY_RISK` — "before the mitigation, so robustness discriminates it" — is arithmetically vacuous, since `RobustnessMitigation` is SUBTRACTED and addition commutes (measured: the same +1200 age penalty at robustness 1, 14 and 20, and larger in relative terms for the more robust player). Restated as what is load-bearing — inside the sum, BEFORE the `OccurrenceRiskMillMult` scaling and BEFORE the clamp — with both superseded sentences annotated in place rather than deleted; the T-MD-AGE-004 lock is rebuilt to fail against the scaling and clamp mutants it was claimed to catch. **H7:** the term's evidential claim is corrected — the research supplement's E-4 (Strong) is U-SHAPED and puts the 16-20 band at ELEVATED risk, so the monotone form follows the evidence above the pivot and INVERTS it below; the shape is deliberately NOT changed (that is R-1's design, awaiting owner sign-off, and its surviving scope under `ERR-041-013`). Also recorded: "the season bands hold unmoved" is evidence for P5 and NOT for the term being wired — all four bands are age-blind, and forced ages 17/26/35 give 623/783/929 league injuries, all three inside the band. Prior entry below.)
**Last Updated (prior):** August 22, 2026 (v0.17 — **ERR-041-020**, the football-judgment proxy review's batch-1 #41 finding, spec + code in the same commit: §3.4's assembly presented as multi-factor risk while omitting player **age** entirely — from the sum, the signature and §2 — despite age being among the best-established real-world risk factors and already carried on the `PlayerRecord` the caller resolves for the attributes. §3.4 gains `AgeRiskFor(ageYears)`, linear and anti-symmetric about `AGE_RISK_PIVOT_YEARS` with no threshold anywhere (doctrine P1), placed inside the sum BEFORE the mitigation for the same normative reason `BASELINE_DAILY_RISK` is (robustness discriminates it); the pivot is the bootstrap roster's mean age, so the term sums to zero over that population and the measured season bands do not move (P5), and `AGE_RISK_SPAN = 0` is the exact pre-fix identity. §3.1's step signature and §3.5's composition gain `ageYears`. §3.6's worked example re-derived at three ages. Prior entry below.)
**Last Updated (prior):** August 8, 2026, third final entry (v0.16 — AR pass 16 L3: §3.3's own summary of the assignment aligned with the clamp)
**Last Updated (prior):** August 8, 2026, second final entry (v0.15 — balance-pass AR pass 15 M1+M2: the §3.1 draw branch made atomic — fallible call before writes — and its assignment gains the RECOVERY_MAX ceiling the code has always applied)
**Last Updated (prior):** August 8, 2026, final entry of the day (v0.14 — balance-pass AR pass 14 M1: the RECOVERY_MAX guard moved to §3.3's assignment step, the one site whose clamp can breach it)
**Last Updated (prior):** August 8, 2026, last entry of the day (v0.13 — balance-pass AR pass 13 M1: the guard class completed — RECOVERY_MAX ≥ 1 at the countdown site, the ceiling's positive side at the draw site)
**Last Updated (prior):** August 8, 2026, even later same day (v0.12 — balance-pass AR pass 12 M3 + L3: §3.1's recovery countdown gains the non-positive-rate refusal; §3.1.1 pins the draw key's canonical spelling and its two sanctioned abbreviations)
**Last Updated (prior):** August 8, 2026, still later same day (v0.10 — balance-pass AR pass 11 L3: the §3.2 guard mirrors all three lock predicates)
**Last Updated (prior):** August 8, 2026, later same day (v0.9 — balance-pass AR pass 10 M1: §3.2 enforces the severity-split invariant at the classifying site)
**Last Updated (prior):** August 8, 2026 (v0.8 — balance-pass AR pass 9 L4: §3.1's pseudocode gains the F8 sentinel-as-worldDay refusal the code has always enforced)
**Last Updated (prior):** August 8, 2026 (v0.7 — balance-pass AR pass 8: the missing v0.6 version row added (L1); the §3.1.1 transfer note separated from the radix rule it had been fused into (L5))
**Last Updated (prior):** August 8, 2026 (v0.6 — balance-pass AR pass 4: §3.1.1 records the TRANSFER residual beside the club-term refusal it qualifies (the only cross-club handoff path today RESETS a moved player's career state — worse than the luck-change the refusal prices; #31's arrival obligation, recorded at the code site too); §3.2's overflow bound corrected to 1.6×10⁷ after the pass-1 headroom raise. This header was also STALE at v0.4 while the table below carried v0.5 — the header-drift class #30 §3's history records; pass 1's 1% → 1.6% edit also shipped without a bump, folded into v0.5's row note.)
**Last Updated (prior):** August 8, 2026 (v0.5 — AR pass 3: §3.1's signature de-phantomed — `rng` → `worldSeed, occurrenceEnabled`, the dial gated in step 2, §3.5's call updated; §3.1.1 gains the ERR-041-019 draw-key global-uniqueness contract)
**Last Updated (prior):** August 7, 2026 (v0.4 — ERR-041-011 at the balance pass: §3.4 gains the normative-position `BASELINE_DAILY_RISK` term; the draw denominator decouples to the `[FIXED]` per-million `OCCURRENCE_DRAW_DENOM` with the `INJURY_RISK_MAX ≤ DENOM` invariant; §3.1's pseudocode re-anchored onto the keyed derivation (ERR-041-002/ERR-041-012); §3.6 re-derived (6600, + the congestion-clamp line))
**Last Updated (prior):** July 23, 2026 (v0.3 — AR-2 fixed-radix append-parity; prior v0.2 AR-1 integer fix, v0.1 initial)
**Version:** 0.19
**Status:** APPROVED

---

All cursor arithmetic is integer. The one stochastic surface is a **single, keyed, position-independent**
draw per player per world day (KD-1) — there is no free-running cursor, so save→restore needs nothing
beyond `InjuryState` itself. Severity classification at Stage 2 re-uses that same draw (a deterministic
bucketing), so an occurrence day consumes exactly **one** draw total.

## 3.1 The daily world-day step (`AdvanceMedicalDay`)

```
AdvanceMedicalDay(ref InjuryState s, playerId, in PlayerAttributes a, ageYears,
                  in InjuryRiskContribution trainingRisk,
                  in MatchLoad recentMatchLoad, in MedicalModifier medical, worldDay, worldSeed,
                  occurrenceEnabled):
    # ageYears: the player's CURRENT age in whole years — #27's PlayerRecord.Age, which #28 keeps
    # current as a derived cache (FR-PG-005) and #30's KD-2 order refreshes at slot 1, before this
    # slot-4 step. Read ONLY by §3.4's age term (ERR-041-020); #41 stores no age of its own.
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
        # Recovery-rate invariant, enforced HERE at the one countdown site (the §3.4 draw-site
        # guard posture): a [GT] config key the catalogue lock only sees the fallback of — a
        # non-positive rate makes every injury PERMANENT, silently (AR pass 12 M3). The RECOVERY_MAX
        # half lives in §3.3's assignment step (moved at AR pass 14 M1 — here it was provably dead:
        # the F1 entry gate refuses any injured state above RECOVERY_MAX and forces
        # RecoveryRemaining >= 1 while injured, so the predicate cannot fire on this branch).
        if RECOVERY_DAYS_PER_TICK_BASE <= 0:
            throw InvalidOperationException      # catalogue/config integrity failure
        s.RecoveryRemaining = Clamp(s.RecoveryRemaining - RECOVERY_DAYS_PER_TICK_BASE, 0, RECOVERY_MAX)  # F1
        if s.RecoveryRemaining == 0:
            s.Severity = InjurySeverity.None      # recovered — but ineligible for a NEW occurrence
                                                   # until the NEXT AdvanceMedicalDay call (see above)

    # 2. Occurrence draw — evaluated ONLY for a player healthy at call entry (§3.1 KD-6 guarantee),
    #    and ONLY with the FR-MD-027 dial armed: disarmed, the step is the recovery countdown and
    #    the cursor advance alone (the FR-MD-027 identity).
    if wasAvailableAtEntry and occurrenceEnabled:
        risk = AssembleRiskScore(trainingRisk, recentMatchLoad, a, ageYears, medical)   # §3.4; in [0, INJURY_RISK_MAX]
        actionOrdinal = DeriveActionOrdinal(worldDay, DRAW_PURPOSE_OCCURRENCE)     # §3.1.1
        # ERR-041-002 (re-anchored at ERR-041-011): the draw is a LOCAL KEYED DERIVATION, not a
        # registered-stream call — #16 exposes no keyed-draw API and a registered stream is
        # cursor-positioned, which KD-1/FR-MD-007 forbid. DrawOccurrence folds
        # DOMAIN_TAG_INJURIES_MEDICAL, then playerId, then actionOrdinal, each through a SplitMix64
        # finalizer, reduced modulo the [FIXED] denominator:
        draw = DrawOccurrence(worldSeed, playerId, actionOrdinal)                  # in [0, OCCURRENCE_DRAW_DENOM)
        if draw < risk:                            # occurrence — the SAME draw also classifies severity (§3.2)
            severity = ClassifySeverityFromDraw(draw, risk)                       # §3.2 — NO second draw
            # RECOVERY_MAX >= 1 refused HERE, before ANY write (AR pass 14 M1 sited the guard; AR
            # pass 15 M1 made the branch atomic — with Severity written first, the refusal itself
            # left a half-injured career): a refused advance mutates nothing (the F7 standard).
            if RECOVERY_MAX < 1:
                throw InvalidOperationException  # catalogue/config integrity failure (§3.3)
            # staff recovery-speed applied ONCE here (integer), not per-tick (FR-MD-014). Floor at 1 so a
            # confirmed injury always has >= 1 recovery-day, ceiling at RECOVERY_MAX (AR pass 15 M2 —
            # the ceiling was in the code and FR-MD-014's countdown clause but NOT in this normative
            # step: an implementer following it wrote 241+ for a slow physio on the Serious tier,
            # which ValidateState refuses the next day):
            recoveryDays = Clamp(RecoveryDaysForTier[severity] * 1000 / medical.RecoverySpeedMillMult,
                                 1, RECOVERY_MAX)
            s.Severity           = severity
            s.RecoveryRemaining  = recoveryDays
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

**Spelling rule (AR pass 12, L3):** the draw key's canonical full spelling is
`(worldSeed, playerId, actionOrdinal = worldDay × DRAW_PURPOSE_RADIX + purpose)`. Three abbreviations
are sanctioned and mean the same key: `(worldSeed, playerId, worldDay, purpose)` — the ordinal expanded
into its two components (the outline/§2.2/#16-row form, sanctioned at AR pass 13 L1); `(playerId,
worldDay, purpose)` — the varying components, the seed being career-constant (FR-MD-006's form); and
"keyed on `PlayerId` with no club term" where only the club-absence matters (FR-SQ-010's form). Any
other spelling is a defect; three drifted spellings of this one key have already cost a sweep.

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
    # Split invariant, enforced HERE at the one classifying site (the §3.4 draw-site guard posture):
    # both numerators NON-NEGATIVE (zero is a deliberate empty tier; NEGATIVE silently deletes its
    # tier through the same mechanism the sum bound stops), and their sum strictly <
    # SEVERITY_PERMILLE_DENOM — both numerators are [GT] config keys, the catalogue suite only sees
    # the fallbacks, and at a sum of exactly 1000 the second bucket's bound is this method's own
    # precondition, so Serious would be silently unreachable (Appendix A).
    if SEVERITY_MINOR_PERMILLE < 0 or SEVERITY_MODERATE_PERMILLE < 0:
        throw InvalidOperationException          # catalogue/config integrity failure
    if SEVERITY_MINOR_PERMILLE + SEVERITY_MODERATE_PERMILLE >= SEVERITY_PERMILLE_DENOM:
        throw InvalidOperationException          # catalogue/config integrity failure

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
(§3.1 step 2, clamped to `[1, RECOVERY_MAX]` — the third statement of this rule, aligned at AR pass 16 L3), so a faster physio assigns fewer total days and the countdown stays a clean
integer 1/day. `MedicalModifier.Identity` is per-mille `1000` = ×1.0, so a no-staff game recovers in exactly
the severity tier's recovery-days constant. No RNG and no float is involved in recovery — a deterministic
integer countdown (FR-MD-014).

**The `RECOVERY_MAX ≥ 1` invariant is enforced at the assignment** (AR pass 14 M1 — moved from §3.1's
countdown guard, where the F1 entry gate makes it provably unreachable under any config): with
`RECOVERY_MAX < 1` the assignment clamp's `value > max` arm would return `RECOVERY_MAX` — an
F1-breaching value for a confirmed injury. `AssignRecoveryDays` fail-louds
(`InvalidOperationException`, catalogue/config integrity) before the clamp, **and the §3.1 draw branch
sequences that call before ANY state write** (AR pass 15 M1 — with `Severity` written first, the
refusal itself left a half-injured career behind; prevention is the ordering's property, the guard
alone only made the breach loud).

## 3.4 The risk-score assembly (`AssembleRiskScore`, pure)

```
AssembleRiskScore(in InjuryRiskContribution trainingRisk, in MatchLoad load, in PlayerAttributes a,
                   int ageYears, in MedicalModifier medical) -> int:
    # All terms and weights are INTEGER (no float — FR-MD-014).
    risk = TRAINING_RISK_PASSTHROUGH_WEIGHT * trainingRisk.RiskScore        # #29's already-published scalar (weight = 1)
         + APPEARANCE_LOAD_WEIGHT * load.AppearanceDays                    # Stage-2 match-load term (FR-MD-010 window count)
         + HARD_CONTACT_WEIGHT * load.HardContacts                         # 0 at Stage 2 (deep-tier only)
         + BASELINE_DAILY_RISK                                             # exposure-independent floor (ERR-041-011)
         + AgeRiskFor(ageYears)                                            # the age term (ERR-041-020), same position
         - RobustnessMitigation(a)                                         # deterministic, own-attribute
    risk = risk * medical.OccurrenceRiskMillMult / 1000                    # per-mille; ×1.0 at Identity (KD-5)
    return Clamp(risk, 0, INJURY_RISK_MAX)
```

**The age term (`ERR-041-020`).** Until August 22, 2026 this formula presented as multi-factor risk
assembly while omitting player **age** entirely — from the sum, from the method signature, and from §2's
requirements — despite age being one of the best-established real-world injury-risk factors, and despite
it already being carried on the `PlayerRecord` the caller resolves in order to read the attributes above.
Recorded as pattern (c) by `docs/tracking/football-judgment-proxy-review.md` §3; fixed under that
document's §6 doctrine **P1** (continuous, never a cliff) and **P5** (pivot on today's baseline).

```
AgeRiskFor(ageYears) -> int:
    if ageYears < 0: FAIL LOUD                       # a derived age is never negative (#28 §3.1.1)
    if AGE_RISK_PER_YEAR_FROM_PIVOT < 0 or AGE_RISK_SPAN < 0: FAIL LOUD    # catalogue/config integrity
    return Clamp(AGE_RISK_PER_YEAR_FROM_PIVOT * (ageYears − AGE_RISK_PIVOT_YEARS),
                 −AGE_RISK_SPAN, +AGE_RISK_SPAN)
```

- **No threshold anywhere.** Every year of age moves the term by the same amount, so there is no age at
  which a player's risk steps — which is what separates this from the age-band cliff its sibling #28
  carried until `ERR-028-020`. The input is whole years because whole years is what #27 exposes
  (`PlayerRecord.Age`, kept current by #28's derived cache, FR-PG-005); a uniform per-year increment is
  not the pattern-(b) shape, which is a judgment collapsed onto ONE cutoff. Day resolution would mean
  #41 reading #28's `BirthWorldDay` for a term whose slope is a first-guess `[GT]`, and is not taken.
- **Its POSITION is normative — inside the sum, BEFORE the `OccurrenceRiskMillMult` scaling and BEFORE
  the clamp** (`ERR-041-021`, correcting the position wording this bullet carried at `ERR-041-020`).
  Both halves are load-bearing: before the scaling, the staff seam modulates the age term exactly as it
  modulates every other term rather than leaving an unmodulated island inside a scaled score; before
  the clamp, the term can never lift the result past `INJURY_RISK_MAX`, which is what keeps every
  assembled score a probability numerator at or below `OCCURRENCE_DRAW_DENOM`.
  > **Superseded wording (`ERR-041-020`, annotated in place, not deleted):** *"inside the sum, before
  > the mitigation, so a robust veteran carries less of his age penalty than a frail one."* That is
  > **arithmetically vacuous**. `RobustnessMitigation` is **subtracted** and addition commutes, so the
  > term's position relative to it is a no-op for every input — measured, the age penalty is the same
  > `+1200` for a robustness-1, a robustness-14 and a robustness-20 player alike, and *larger in
  > relative terms* for the more robust one, whose assembled score is smaller. The consequence was
  > wrong in both readings, and the lock written to enforce it passed against a mutant that moved the
  > term across the mitigation. What robustness genuinely does is lower the whole assembled score,
  > this term's contribution included, wherever in the sum it sits. The same wording sits on
  > `BASELINE_DAILY_RISK` from `ERR-041-011` and is corrected with it, below.
- **Ordering dependency on #30's KD-2 tick order (L5, round-2 M/L pass, August 24, 2026).** `ageYears`
  is not #41's own state — it is #28's derived `PlayerRecord.Age` cache, read live at this call. The
  value is only current because #30's KD-2 day-advance order refreshes #28's slot BEFORE #41's slot-4
  step runs (the §3.1 pseudocode comment on `AdvanceMedicalDay`'s `ageYears` parameter states the same
  dependency at the call-site level). **Today that ordering constraint has exactly one normative
  source: a code comment in `src/season-save/PlayerCareerStates.cs`, and nothing else** — no #41 FR,
  no #30 KD-2 slot-table entry, and no test that fails if the order inverts. If a future slot is
  inserted between #28's refresh and #41's step, or KD-2 is revised for #44 suspensions, #41 reads
  **yesterday's** age for the players whose age ticks that exact day (of the order ~1-in-365 per
  player per year — a ≈150-per-million difference in the assembled score, per `AGE_RISK_PER_YEAR_FROM_
  PIVOT`). That drift is invisible to every existing lock and to the season-scale injury bands, which
  are age-blind by construction (see the bullet above). **Recorded, not fixed, and deliberately out of
  scope here**: promoting the dependency into a #30 KD-2 slot-table row and a season-save order-inversion
  test lock is `season-save` territory, not this spec's.
- **P5 pivot.** `AGE_RISK_PIVOT_YEARS` is the MEAN of the bootstrap roster's age distribution
  (`RosterGenerator` draws uniformly on #27's `[AgeMin, AgeMax]`), and the term is linear and
  anti-symmetric about it, so the age contributions over that population sum to zero: the squad-wide and
  league-wide injury rates are unchanged and only the DISTRIBUTION across the squad moves, which is the
  whole content of the finding. Measured after the fix: the season-scale instrument's league, starter,
  reserve and squad-unavailability bands all hold unmoved. `AGE_RISK_SPAN = 0` is the exact pre-fix
  identity, locked by execution rather than by assertion.
  > **What "the bands hold unmoved" does and does not establish (`ERR-041-021`).** It establishes the
  > P5 property above — an anti-symmetric term about the population mean does not move the aggregate.
  > It is **not** evidence that the term is wired, and it was published as if it were. All four bands
  > are age-blind by construction, and measurement confirms it: forcing every player's age to 17, 26
  > and 35 yields **623 / 783 / 929** league injuries a season, and **all three pass the asserted
  > band**, in both directions. The age axis is locked separately, by
  > `SeasonInjuryRealismTests`' over-30-vs-under-23 split (measured 1.34×; 1.01× with the production
  > call site's age neutralised, which fails the assert).
- **Only the VETERAN half of this term follows the evidence (`ERR-041-021`).** The research-alignment
  supplement's E-4 is rated **Strong** and is **U-shaped**: musculoskeletal maturity continues to
  ~24–25, and *the 16–20 band carries elevated risk at adult match intensity*. A monotone linear term
  about a pivot of 26 reproduces that above the pivot and **inverts it below** — a 19-year-old receives
  −1050, making 16–20-year-olds the safest players in the league. `ERR-041-020`'s claim that the term
  is "the direction and rough magnitude the epidemiology supports" is therefore true of the veteran arm
  and false of the young arm. The shape is **deliberately not changed here**: the U-shape is the
  research supplement's **R-1** design, it is awaiting owner sign-off, and re-shaping shipped football
  behaviour is the owner's call rather than a review loop's. R-1's surviving scope under its reserved
  back-prop `ERR-041-013` is exactly this young-tail arm; its age-plumbing half is what landed as
  `ERR-041-020`. Any refit **re-shapes this term** rather than adding beside it.
- **Not a second robustness read (P3).** The term is age, not durability; `RobustnessMitigation` keeps
  Strength/Stamina/Balance, and nothing else in #41 reads age. (#28's own `ERR-041-003`-driven decision
  runs the other way for the same reason: its retirement offset deliberately avoids that trio.)
- **For the research-alignment supplement (its §10):** this is now a THIRD term its refit must fold
  into rather than add beside, alongside `BASELINE_DAILY_RISK`.

**`BASELINE_DAILY_RISK`'s position is normative** (ERR-041-011, position wording corrected at
`ERR-041-021`): it sits **inside the sum, BEFORE the `OccurrenceRiskMillMult` scaling and BEFORE the
clamp**, for the two reasons the age term's bullet above gives — the staff seam modulates it like every
other term, and it cannot lift the result past `INJURY_RISK_MAX`. Because #27 attributes floor at 1 and
the default magnitudes keep
`BASELINE_DAILY_RISK` above the largest mitigation row, no valid-input player is ever injury-proof (the
third absurdity the T0 fifth AR pass measured: the default focus converged on exactly-0-forever).
> **Superseded wording (`ERR-041-011`, annotated in place, not deleted):** *"before the mitigation, so
> robustness discriminates the exposure-independent floor — a frail player's quiet week is riskier than
> an iron man's."* Inert, for the reason given in the age term's bullet above: the mitigation is
> subtracted and addition commutes, so no term's position relative to it changes any output. The
> injury-proof-forever property is a consequence of the floor's *magnitude* relative to the largest
> mitigation row, which the surviving sentence states, not of where it sits in the sum.

It is
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
config produced it. Pinned, config edits move only thresholds. Invariant: `0 < INJURY_RISK_MAX ≤
OCCURRENCE_DRAW_DENOM`, enforced fail-loud at the draw site on BOTH sides (a ceiling past the
denominator would make a clamped risk mean "certain and then some"; a non-positive ceiling clamps every
score to 0 and the ARMED dial injures nobody, forever, silently — AR pass 13 M1).

## 3.5 Composition at #30's day-advance loop (informative)

Per world day, for each club, in the club's deterministic roster-iteration order, at the **new** slot KD-6
pins (after #28/#29/#33, before `WorldStore.AdvanceDay()`):

```
for each playerId in club roster:
    trainingRisk    = TrainingSystem.ComputeInjuryRisk(trainingState[playerId], attrs[playerId])   # #29 read
    recentMatchLoad = ... caller-supplied MatchLoad (Stage 2: AppearanceDays from #30's per-player
                          appearance record — the FR-MD-010 window count; ERR-041-010(b)) ...
    AdvanceMedicalDay(ref medicalState[playerId], playerId, attrs[playerId], ages[playerId],
                       trainingRisk, recentMatchLoad, medical, worldDay, worldSeed, occurrenceEnabled)
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

- `risk = 1×3000 + 0 + 4000 + AgeRiskFor(26) − 400 = 1×3000 + 0 + 4000 + 0 − 400 = 6600`
  (× `OccurrenceRiskMillMult 1000 / 1000` = unchanged; clamp within `[0, 16000]` inactive) — a
  0.66%/day probability against the per-million draw. *(Player 501 is taken to be `AGE_RISK_PIVOT_YEARS`
  old, which is why the arithmetic is unchanged by ERR-041-020: the age term is zero at the pivot, and
  that is what makes this example — and every §5 expectation written before that ERR — still exact.
  **Re-derived at two other ages, same player otherwise:** at 20 the assembly is
  `3000 + 4000 − 900 − 400 = 5700` (0.57%/day) and at 34 it is `3000 + 4000 + 1200 − 400 = 7800`
  (0.78%/day) — the veteran carries about 1.37× the youngster's daily risk, the direction and rough
  magnitude the epidemiology supports, at a size that cannot dominate the exposure terms beside it.)* *(The same
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
| 0.9 | 2026-08-08 | — | **Balance-pass AR pass 10 (M1)**: §3.2's pseudocode gains the fail-loud split-invariant guard at the classifying site — both numerators are `[GT]` config keys, the catalogue suite only sees the fallbacks (ERR-041-003's class), and a config summing to the denominator deleted the `Serious` tier silently. The §3.4 draw-site guard posture, at #41's other config-breakable invariant. |
| 0.10 | 2026-08-08 | — | **Balance-pass AR pass 11 (L3)**: the §3.2 guard was ONE of the design-time lock's three predicates while both new comments called them two halves of one invariant — a negative `[GT]` numerator passed the sum guard and silently deleted its own tier (the pass-6 rule-at-one-boundary shape, inside the fix being verified). Non-negativity added; zero stays legal (an expressible empty-tier intent). |
| 0.11 | 2026-08-08 | — | **Balance-pass AR pass 12 (M3)**: `RECOVERY_DAYS_PER_TICK_BASE` was the one `[GT]` in the landing whose design-time lock had NO runtime mirror *(claim corrected at v0.13 — two more sides were unmirrored)* — non-positive, the countdown never falls and every injury is permanent, silently, with the armed dial progressively injuring the whole league; §3.1 gains the fail-loud refusal at the countdown site (the §3.4 guard posture, fourth instance). |
| 0.12 | 2026-08-08 | — | **Balance-pass AR pass 12 (L3)**: §3.1.1 pins the key's canonical spelling + the two sanctioned abbreviations — the key had accumulated three drifted spellings across two assemblies and two specs, and a sweep needs a rule, not a preference. *(Folded into the same pass as v0.11 — one bump per pass would have hidden the two distinct changes.)* |
| 0.13 | 2026-08-08 | — | **Balance-pass AR pass 13 (M1 + L1)** *(the RECOVERY_MAX half's placement corrected at v0.14 — the countdown site cannot reach it)*: *(L1: the §3.1.1 spelling rule gains the 4-tuple as a third sanctioned expansion — three live sites already used it and the rule as written made them defects.)* v0.11's "the one `[GT]` whose lock had no runtime mirror" was FALSE — `RECOVERY_MAX` had none (below 1, the §3.3 assignment clamp's min exceeds its max and writes `RecoveryRemaining == 0` while injured — the F1 breach the floor's own doc names — surfacing a day later as data corruption blamed on the state), and `INJURY_RISK_MAX`'s guard was one-sided (non-positive: the armed dial injures nobody, forever, silently — the pass-12 failure shape itself). §3.1's countdown guard and §3.4's draw-site invariant now cover both. |
| 0.14 | 2026-08-08 | — | **Balance-pass AR pass 14 (M1)** *(its "fail-louds before the clamp" prevention claim corrected at v0.15 — the branch wrote Severity first)*: v0.13 placed the `RECOVERY_MAX < 1` refusal on the countdown branch, where it is PROVABLY DEAD — the F1 entry gate refuses any injured state above the ceiling and forces `RecoveryRemaining ≥ 1` while injured, so the predicate is unsatisfiable there under any config, while the breach it names happens on the mutually exclusive draw branch (demonstrated by model: a healthy player drawn injured gets `RecoveryRemaining == 0` written beside a severity, refused a day later as a state fault). Moved to §3.3's assignment step; §3.1's guard reverts to rate-only. A guard on a mutually-exclusive branch ships green precisely because it is unreachable — the pass-13 verification gap. |
| 0.15 | 2026-08-08 | — | **Balance-pass AR pass 15 (M1 + M2)**: **M1** — the pass-14 guard fired AFTER `s.Severity` was written, making the draw branch the step's one partial-write throw site: the refusal itself left `RecoveryRemaining == 0` beside a fresh severity in the LIVE career, the exact breach being refused, surfacing a day later as a state-blaming fault (demonstrated by model; fixing the config did not recover the session). The branch is now atomic — fallible call first, three writes after — and the three prevention claims are corrected: prevention is the ORDERING's property. **M2** — §3.1's normative assignment had NO `RECOVERY_MAX` ceiling while the code has always clamped to it: an implementer following the step wrote 241+ for a below-average physio on the Serious tier, refused by `ValidateState` the next day and persisted happily by the codec. The ceiling was only in the two paragraphs pass 14 wrote — the normative step now carries `Clamp(…, 1, RECOVERY_MAX)` and FR-MD-014's assignment clause gains the ceiling. |
| 0.16 | 2026-08-08 | — | **Balance-pass AR pass 16 (L3)**: §3.3's prose — the section that OWNS recovery-speed modulation — still said "floored at 1" after M2 swept the other two statements of the rule; the third aligned (the grep-boundary class, one clause short of the owning section). |
| 0.17 | 2026-08-22 | — | **ERR-041-020** (football-judgment proxy review, batch 1 — spec + code, same commit). §3.4's `AssembleRiskScore` presented as multi-factor risk assembly while omitting player **age** from the sum, from the method signature and from §2's requirements — a well-established real-world risk factor, already on the `PlayerRecord` the caller resolves in order to read the attributes it does use, and already consumed by #31's valuation. Pattern (c). The assembly gains `AgeRiskFor(ageYears)`: linear in age, anti-symmetric about `AGE_RISK_PIVOT_YEARS`, saturating at `±AGE_RISK_SPAN`, with **no threshold anywhere** (doctrine P1 — the uniform per-year increment is deliberately not the one-cutoff shape, and the whole-year granularity is what #27 exposes). Its **position is normative** for the same reason `BASELINE_DAILY_RISK`'s is: inside the sum, before the mitigation, so robustness discriminates it. **P5**: the pivot is the mean of #27's bootstrap age distribution and the term is anti-symmetric about it, so it sums to zero over that population — the measured season bands (league injuries, starter/reserve means, squad unavailability) hold unmoved — and `AGE_RISK_SPAN = 0` reproduces the pre-fix assembly exactly, locked by execution through a parameterised overload rather than asserted. **P3**: the term is age, not durability — `RobustnessMitigation` keeps Strength/Stamina/Balance and nothing else in #41 reads age. §3.1's step signature and §3.5's composition loop gain `ageYears`; §3.6's worked example re-derived. No draw, no stream, no domain tag, no format version. |
| 0.18 | 2026-08-22 | — | **ERR-041-021** (adversarial review over the ERR-041-020 landing — H4 + H7; spec + code, same commit). **H4 — the normative-position claim was arithmetically vacuous, in TWO places.** §3.4 said of both `AgeRiskFor` (v0.17) and `BASELINE_DAILY_RISK` (v0.4/ERR-041-011) that they sit "before the mitigation, so robustness discriminates it". `RobustnessMitigation` is **subtracted** and addition commutes, so neither term's position relative to it changes any output for any input: measured, the age penalty is `+1200` for a robustness-1, a robustness-14 and a robustness-20 player alike — and *larger in relative terms* for the more robust one, so the stated consequence is wrong in both readings. A reviewer's three mutants (term after the mitigation / after the `OccurrenceRiskMillMult` scaling / after the clamp) all left `InjuriesMedical.Tests` green, and the third can return **above** `INJURY_RISK_MAX`, breaking ERR-041-011's every-daily-probability-≤-1 invariant. Both positions are restated as what is genuinely load-bearing — **inside the sum, BEFORE the scaling and BEFORE the clamp** — with the two superseded sentences annotated in place, not deleted, and T-MD-AGE-004 rebuilt to fail against the scaling and clamp mutants (the after-the-mitigation mutant is an identity over 956,480 sampled inputs and is therefore no longer claimed rather than newly locked). **H7 — the term's evidential support was overstated, and its monotone shape INVERTS the repo's own Strong-rated young-tail evidence.** The research-alignment supplement's E-4 is **U-shaped** — maturity continues to ~24–25 and the 16–20 band carries elevated risk at adult match intensity — so a monotone term about pivot 26 follows the evidence above the pivot and contradicts it below, making 16–20-year-olds the safest players in the league (a 19-year-old receives −1050). The shape is **deliberately not changed**: it is the supplement's R-1 design, awaiting owner sign-off, and re-shaping shipped football behaviour is the owner's call. R-1 is annotated in the supplement as landed-in-part, with its reserved back-prop `ERR-041-013` now covering only the residual U-shape / young-tail arm. **Also corrected:** "the measured season bands hold unmoved" is evidence for P5 and **not** evidence that the term is wired — all four bands are age-blind, and forced ages 17/26/35 give 623/783/929 league injuries, all three inside the asserted band; the age axis now has its own lock in `SeasonInjuryRealismTests` (1.34× measured, 1.01× and failing with the production call site's age neutralised). |
| 0.19 | 2026-08-24 | — | **Round-2 Medium/Low pass (L5)**: §3.4 gains a bullet stating the age term's ordering dependency on #30's KD-2 tick order — `ageYears` is #28's derived cache, current only because #30 refreshes it at slot 1 before #41's slot-4 step runs; today that ordering constraint is normative ONLY in a `src/season-save/PlayerCareerStates.cs` code comment, with no #41 FR, no #30 KD-2 slot-table row, and no order-inversion test. States the consequence if a future slot lands between them or KD-2 is revised for #44: ~150-per-million drift for the ~1-in-365 players whose age ticks that exact day, invisible to every existing lock and to the age-blind season bands. The season-save test lock and a #30 KD-2 slot-table row are recorded as out of scope for this pass. |
#endregion
