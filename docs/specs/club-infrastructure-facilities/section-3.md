# Club Infrastructure & Facilities #53 — Section 3: Algorithms

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.2
**Status:** IN REVIEW

---

All arithmetic is **integer** (FR-IN-003). No float appears at any tier, and **no formula below makes a
stochastic draw** (FR-IN-030) — #53 has no draw site to specify.

## 3.1 `CanStartUpgrade` — the pure startability predicate (FM-IN-01)

The check the command layer runs **before** #40 debits anything (KD-1 step 2). It is the half of the
split surface that must be safe to call freely, so it mutates nothing and allocates nothing.

```
CanStartUpgrade(clubId, FacilityType type, int targetLevel) -> bool:
    if not TryGet(clubId, out ClubFacilities f):     return false   # unmodelled club (F7)
    if not IsDefined(type):                          return false   # F2
    if f.InProgressFacility != FACILITY_NONE_SENTINEL: return false # already building (FR-IN-014)

    current := f.Levels[(int)type]
    RequireInRange(current, FACILITY_LEVEL_MIN, FACILITY_LEVEL_MAX)  # F1 — a corrupt store fails LOUD,
                                                                     # it does not quietly return false
    if targetLevel <= current:                       return false   # a no-op purchase (FR-IN-016)
    if targetLevel >  FACILITY_LEVEL_MAX:            return false   # FR-IN-016
    return true
```

**Two response kinds in one function, and the distinction is deliberate.** A *legitimate refusal* — the
club has no entry, is already building, or asked for a level it cannot have — returns `false`, because
the command layer must be able to ask without exception handling. A *corrupt store* — a level outside its
own declared range — **throws**, because it means #53's own invariant is broken and returning `false`
would present a data-integrity bug as an ordinary "you can't build that". Collapsing the two, in either
direction, is the mistake this pseudocode is written to prevent.

## 3.2 `StartUpgrade` — the latch (FM-IN-02)

Runs **after** #40's debit (KD-1 step 4). It re-validates first (FR-IN-013).

```
StartUpgrade(clubId, FacilityType type, int targetLevel):
    if not CanStartUpgrade(clubId, type, targetLevel):
        throw                                        # F6 — a stale check is LOUD, never a silent no-op

    ref f := GetRequired(clubId)                     # F7: unmodelled club throws (the check already
                                                     # returned false, so this is unreachable in practice)
    current := f.Levels[(int)type]
    days    := FACILITY_BUILD_DAYS_PER_LEVEL * (targetLevel - current)      # >= 1 level (FR-IN-016)
    RequireNoOverflow(worldDay, days)                # see below

    f.InProgressFacility := (int)type
    f.TargetLevel        := targetLevel
    f.CompletionWorldDay := worldDay + (uint)days
```

**Why the completion day is computed once here and never again.** It is the whole of KD-3: from this
point the build is a stored fact about the calendar, not a process. Nothing decrements, so nothing can
double-decrement across a restore or a replayed day boundary.

**The overflow guard, and why `uint.MaxValue` is not a sentinel.** `CompletionWorldDay` is a `uint`;
`worldDay + days` would wrap on a career run to the end of the `uint` day space, producing a completion
day in the *past* and a build that completes instantly. `RequireNoOverflow` fails loud instead. And
because a computed day of `uint.MaxValue` is legal, that value is **not** used as any #53 sentinel — the
idle state is carried by `InProgressFacility == FACILITY_NONE_SENTINEL` alone (§1.6 item 3). The sibling
specs' `uint.MaxValue` cursor sentinel is safe there because their field is a *last-advanced* day, never
a *computed future* day; the same value would be a collision here.

## 3.3 `AdvanceFacilityDay` — the day advance (FM-IN-03)

Invoked once per modelled club per world day at #30's tick-order slot (§4.4).

```
AdvanceFacilityDay(clubId, uint worldDay):
    ref f := GetRequired(clubId)                     # F7 — unmodelled club throws

    if f.InProgressFacility == FACILITY_NONE_SENTINEL:  return       # idle — nothing to do
    if worldDay < f.CompletionWorldDay:                 return       # still building

    # Complete: apply the level and clear the record ATOMICALLY (FR-IN-017).
    RequireInRange(f.TargetLevel, FACILITY_LEVEL_MIN, FACILITY_LEVEL_MAX)      # F1
    RequireDefinedOrdinal(f.InProgressFacility)                                # F2
    f.Levels[f.InProgressFacility] := f.TargetLevel
    f.InProgressFacility := FACILITY_NONE_SENTINEL
    f.TargetLevel        := 0
```

**No cursor, no gap guard — and that is KD-7, not an omission.** The two properties a
`LastAdvancedWorldDay` cursor exists to provide are already true here:

- **Same-day re-invocation is a no-op.** The first call clears `InProgressFacility`; the second takes the
  idle branch. Nothing accumulates, so nothing double-applies.
- **A multi-day gap is correct, not an error.** `worldDay >= CompletionWorldDay` is a `>=`, not an `==`,
  so a build whose completion day fell inside a skipped range completes on the first day observed after
  it. There is nothing missed to fail loud about.

Adding a cursor would therefore buy nothing and cost a save field, a failure mode, and — the real risk —
a **gap guard that would fail loud on a legitimate multi-day advance**. §5.3 locks both properties so
that the "consistency fix" fails a test rather than shipping.

**Clearing is atomic with applying.** If the level were written and the record cleared in two steps that
could be interrupted, a re-entry between them would re-apply the same completion — harmlessly today,
since the write is idempotent, but the invariant *"`InProgressFacility != -1` implies a build that has
not yet been applied"* is what the whole idempotency argument rests on, so it is stated rather than
inferred.

## 3.4 `ProjectFacilityTerm` — level → dial (FM-IN-04)

One shared shape, two identity conventions (KD-8). Every projection is a pure function of a single
level — **never of staff state** (FR-IN-023).

```
Steps(level) -> int:
    return level - FACILITY_LEVEL_BASELINE           # >= 0 at Stage 3; the deviation from identity

# --- Zero-identity consumers (additive) ------------------------------------------------
ProjectAcademyQuality(clubId) -> AcademyQuality:                                    # #42's type
    if not TryGet(clubId, out f):  return AcademyQuality.Neutral                    # F7
    s := Steps(f.Levels[(int)FacilityType.YouthFacilities])
    return new AcademyQuality(
        CeilingShiftPerMille: Clamp(s * FACILITY_ACADEMY_SHIFT_PER_LEVEL,
                                    -ACADEMY_CEILING_SHIFT_ABS_MAX,
                                    +ACADEMY_CEILING_SHIFT_ABS_MAX),                # #42's own bound
        CohortSizeDelta:      0)                                                    # deep tier

ProjectTrainingTerm(clubId) -> int:                                                 # a #29 INPUT (KD-9)
    if not TryGet(clubId, out f):  return 0
    return Steps(f.Levels[(int)FacilityType.TrainingGround]) * FACILITY_TRAINING_TERM_PER_LEVEL

# --- 1000-identity consumer (multiplicative per-mille) ---------------------------------
ProjectMedicalModifier(clubId) -> MedicalModifier:                                  # #41's type
    if not TryGet(clubId, out f):  return MedicalModifier.Identity                  # NOT default() — x0
    s := Steps(f.Levels[(int)FacilityType.MedicalCentre])
    return new MedicalModifier(
        OccurrenceRiskMillMult: PERMILLE_ONE,                                       # #53 affects RECOVERY only
        RecoverySpeedMillMult:  Clamp(PERMILLE_ONE + s * FACILITY_MEDICAL_MULT_PER_LEVEL,
                                      FACILITY_MEDICAL_MULT_MIN,
                                      FACILITY_MEDICAL_MULT_MAX))
```

**The identity property, which is the load-bearing one (§5.1 sweeps it).** At
`level == FACILITY_LEVEL_BASELINE`, `Steps` is `0`, so:

| Projection | At baseline | Consumer's identity | Equal? |
|---|---|---|---|
| `ProjectAcademyQuality` | `(0, 0)` | `AcademyQuality.Neutral == default` = `(0, 0)` | **exactly** |
| `ProjectTrainingTerm` | `0` | the zero term | **exactly** |
| `ProjectMedicalModifier` | `(1000, 1000)` | `MedicalModifier.Identity == new(1000, 1000)` | **exactly** |

**Why `MedicalModifier` must be built with the explicit factory shape and not `default`.** #41's
`default(MedicalModifier)` is all-zero — a **×0** recovery multiplier, which would make every injury
permanent — and #41 fails loud on it (FR-MD-016). A projection that returned `default` for an unmodelled
club would therefore convert a *legal* absent-club case into a hard failure at #41's seam. Returning
`Identity` is not a convenience; it is the difference between the two identity conventions being
respected and being conflated (KD-8).

**Why `OccurrenceRiskMillMult` stays at identity.** A medical centre speeds *recovery*; it does not stop
players getting injured on the pitch. Leaving occurrence at `1000` is a scope statement — #53 supplies a
recovery term and no more — and it keeps the double-count surface with #34's physio quality to a single
field.

**Clamping against the consumer's own bound.** `ProjectAcademyQuality` clamps to
`ACADEMY_CEILING_SHIFT_ABS_MAX` — **#42's** constant, consumed read-only — rather than to a #53 bound,
because #42 fails loud on an out-of-bounds dial (its F2). Producing a value its consumer would reject is
a producer bug, and the clamp is where #53 declines to commit it.

**Overflow.** With `FACILITY_LEVEL_MAX − FACILITY_LEVEL_BASELINE ≤ FACILITY_LEVEL_SPAN_MAX` (a `[FIXED]`
bound) and every per-level constant bounded by `FACILITY_PER_LEVEL_ABS_MAX`, every product above is at
most `FACILITY_LEVEL_SPAN_MAX × FACILITY_PER_LEVEL_ABS_MAX`, three orders of magnitude inside `int`. The
bound is `[FIXED]` rather than tunable for exactly this reason: raising it is an arithmetic-safety change,
not a balance change.

## 3.5 `StadiumCapacity` — the deferred #40 input (FM-IN-05)

```
StadiumCapacity(clubId) -> int:
    if not TryGet(clubId, out f):  return STADIUM_BASE_CAPACITY      # the baseline, for an unmodelled club
    s := Steps(f.Levels[(int)FacilityType.Stadium])
    return STADIUM_BASE_CAPACITY + s * STADIUM_CAPACITY_PER_LEVEL
```

**The one projection that is not an identity dial**, and the reason is worth stating rather than leaving
as an inconsistency: capacity is an *absolute* club property, like a roster size, not a deviation from a
neutral. At the minimal tier **nothing reads it**, so §1.7's identity claim holds vacuously; from #40's
T3 it holds by #40 calibrating its attendance model against `STADIUM_BASE_CAPACITY`, which is #40's
calibration to do and is recorded as a deferred back-prop (§8.2) rather than assumed here.

Holding capacity now, before its consumer exists, is **not** a phantom surface: the value is meaningful
club state on its own terms, and #40's consumer is already specified as *deferred* rather than absent
(its §7.2). That is the distinction FR-LW-031 actually draws — it forbids inventing an *interface* for an
unspecified consumer, not holding a *value* a specified-but-deferred consumer will read.

## 3.6 Division and rounding convention (pinned)

Every expression above is **exact integer arithmetic** — multiplication, addition, and comparison only.
**#53 contains no division at any tier**, so no rounding convention arises, and none may be introduced
without a spec change: a future per-level *fraction* (say, "each level adds 7.5% recovery speed") would
have to be expressed as an integer per-mille multiply, never as a divide-and-round, because
`Math.Round`'s banker's rounding and `Math.Floor`'s toward-−∞ behaviour both break the sign symmetry the
project pins elsewhere (#45 §3.6) and `Math.Round` operates on `double`, violating FR-IN-003 outright.

Stated positively: **if a future reviewer finds a `/` in #53's formula code, that is the finding.**

## 3.7 Worked examples (hand-verifiable)

At `FACILITY_LEVEL_BASELINE = 1`, `FACILITY_LEVEL_MIN = 1`, `FACILITY_LEVEL_MAX = 5`,
`FACILITY_BUILD_DAYS_PER_LEVEL = 180`, `FACILITY_ACADEMY_SHIFT_PER_LEVEL = 15`,
`FACILITY_TRAINING_TERM_PER_LEVEL = 10`, `FACILITY_MEDICAL_MULT_PER_LEVEL = 40`,
`STADIUM_BASE_CAPACITY = 20000`, `STADIUM_CAPACITY_PER_LEVEL = 8000`.

| # | Setup | Working | Result |
|---|---|---|---|
| (a) | Baseline club, `ProjectAcademyQuality` | `Steps = 1 − 1 = 0`; `0 × 15 = 0` | `(0, 0)` = **`AcademyQuality.Neutral` exactly** |
| (b) | Baseline club, `ProjectMedicalModifier` | `Steps = 0`; `1000 + 0 × 40 = 1000` | `(1000, 1000)` = **`MedicalModifier.Identity` exactly** |
| (c) | `MedicalCentre` at level 4 | `Steps = 3`; `1000 + 3 × 40 = 1120` | `(1000, 1120)` — recovery 12% faster; occurrence untouched |
| (d) | `YouthFacilities` at level 5 | `Steps = 4`; `4 × 15 = 60` | `CeilingShiftPerMille = 60` (subject to #42's own abs-max clamp) |
| (e) | `TrainingGround` at level 3 | `Steps = 2`; `2 × 10 = 20` | training term `20`, handed to #29 — **not** a `TrainingInput` (KD-9) |
| (f) | `Stadium` at level 3 | `Steps = 2`; `20000 + 2 × 8000` | capacity `36000` |
| (g) | `StartUpgrade(MedicalCentre, 3)` on day 400, current level 1 | `days = 180 × (3 − 1) = 360`; `400 + 360` | `CompletionWorldDay = 760`, `TargetLevel = 3` |
| (h) | (g) then `AdvanceFacilityDay(day 759)` | `759 < 760` | no change — still building |
| (i) | (g) then `AdvanceFacilityDay(day 760)` twice | first: `760 ≥ 760` ⇒ level `3`, record cleared. second: idle branch | level `3`; **second call is a no-op** — the KD-7 idempotency |
| (j) | (g) then a jump straight to `AdvanceFacilityDay(day 900)` | `900 ≥ 760` ⇒ completes | level `3` — **a day gap is correct, not an error** |
| (k) | `CanStartUpgrade(Stadium, 5)` while the medical centre is building | `InProgressFacility != −1` | `false` — refusal, **no throw** |
| (l) | `CanStartUpgrade(Stadium, 1)` at current level 3 | `1 ≤ 3` | `false` — a no-op purchase the player must not be charged for |
| (m) | `CanStartUpgrade` on a club whose stored level is `9` | `9 > FACILITY_LEVEL_MAX` at the range check | **throws** (F1) — a corrupt store is not an ordinary refusal (§3.1) |

Examples (i), (j) and (m) are the three that would each fail under a plausible "tidier" implementation:
a decrementing counter breaks (i) and (j); a uniformly-`false`-returning predicate breaks (m).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §3 (FM-IN-01..05: the pure predicate, the latch, the day advance, the level→dial projections, capacity; §3.6 arithmetic convention; worked examples) from supplement v0.4. Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 fixes. **M:** §3.2 gained the **`RequireNoOverflow` guard on `worldDay + days`** and the explanation of why `uint.MaxValue` cannot be a #53 sentinel — a wrapped completion day is a build that completes *instantly and in the past*, which no other guard would catch. **M:** §3.4's `ProjectMedicalModifier` unmodelled-club return corrected to `MedicalModifier.Identity` — returning `default` would hand #41 an all-zero (×0) modifier and turn a *legal* absent-club case into a hard failure at #41's own fail-loud seam. **M:** §3.1 gained the explicit **refuse-vs-throw** distinction (a legitimate refusal returns `false`; a corrupt store throws), which the v0.1 pseudocode left ambiguous — collapsing them in either direction is a real defect, and worked example (m) now locks it. **L:** §3.4 gained the clamp against **#42's own** `ACADEMY_CEILING_SHIFT_ABS_MAX` (producing a value the consumer would reject is a producer bug) and the overflow bound; §3.6 restated as *"#53 contains no division"* with the positive rule; §3.5 gained the FR-LW-031 value-vs-interface distinction; §3.3 gained the atomicity note. |
#endregion
