# Youth Academy & Intake #42 — Section 3: Core Algorithms

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.2 — section-file PASS-1 fix pass)
**Version:** 0.2
**Status:** IN REVIEW

---

**Arithmetic posture.** Every formula below is integer (FR-YA-022). Division is C# integer division
(truncation **toward zero**) — see the §3.6 convention lock, which pins that choice against two
plausible-looking "cleanups" that would each change behaviour.

## 3.1 The world-tick step (FM-YA-01)

Invoked by #30 at the pre-declared academy seam (ERR-030-007), after the staff seam and before the live
`WorldStore.AdvanceDay()`.

```
AdvanceAcademyDay(ref AcademyState st, uint currentWorldDay, in AcademyQuality quality,
                  DeterministicRngService rng) -> IntakeResult:

    # 1. Trigger (KD-4). Genesis is the explicit sentinel, never day-0 arithmetic (FR-YA-015).
    due := (st.HasIntaken == false)
           or (currentWorldDay >= st.LastIntakeWorldDay + ACADEMY_INTAKE_PERIOD_DAYS)
    if not due:
        return IntakeResult.Empty                 # FR-YA-017 — no draw, no state change

    # 2. Validate BEFORE any draw (refuse-before-draw; the world.text precedent).
    ValidateQuality(quality)                      # F2
    ValidateStateCoherent(st)                     # F4/F6

    # 3. Anchor the stream for THIS intake (KD-7) — makes the cohort position-independent.
    streamIndex := EnsureIntakeStream(rng, st.ClubId)                       # FR-YA-018, lazy
    AnchorStream(rng, streamIndex,
                 DeriveActionOrdinal(st.ClubId, currentWorldDay, DRAW_PURPOSE_INTAKE))   # §3.2

    # 4. Generate + transform the cohort.
    result := GenerateCohort(ref st, streamIndex, currentWorldDay, quality, rng)          # §3.3

    # 5. Stamp the latch LAST, so a throw anywhere above leaves the day retryable.
    st.LastIntakeWorldDay := currentWorldDay
    st.HasIntaken         := true
    st.LastAppliedQuality := quality               # provenance only; never re-applied
    return result
```

**Why the latch is stamped last.** If step 4 throws (F1/F6), the state must not record an intake that did
not happen — otherwise the club silently loses a cohort forever. Stamping last makes the step
**all-or-nothing** from the caller's perspective.

**Why the trigger is `>=` and not `==`.** #30 advances one calendar day at a time today, but
`AdvanceToNextFixtureDay` may cross several days in a loop; a `==` trigger would silently skip an intake
whenever the loop stepped over the exact due day. `>=` fires on the first day at or after the due day.

## 3.2 The per-intake anchor (FM-YA-02, KD-7)

```
DeriveActionOrdinal(clubId, worldDay, purpose) -> u64:
    # The #41 §3.1.1 shape, with #41's own AR-2 fix: a FIXED radix, never a growing purpose count
    # (a growing radix breaks cross-version replay parity the moment a purpose is appended).
    require 0 <= purpose < DRAW_PURPOSE_RADIX                     # bound guard
    require 0 <= clubId  < ACADEMY_CLUB_STRIDE                    # injectivity guard — see below
    return ((u64)worldDay * DRAW_PURPOSE_RADIX + purpose) * ACADEMY_CLUB_STRIDE + (u64)clubId
```

**Both guards are load-bearing, not defensive.** The expression is injective over
`(worldDay, purpose, clubId)` **only while each component stays inside its stride**: a `clubId ≥
ACADEMY_CLUB_STRIDE` would carry into the purpose/day digits and silently alias a *different* club's
anchor on a *different* day — two clubs generating the same cohort, with no error and no divergence
signal. The guard makes that a fail-loud (F2-class) instead. Range check: `worldDay` (`u32`) ×
`DRAW_PURPOSE_RADIX` (16) × `ACADEMY_CLUB_STRIDE` (65536) ≈ 4.5 × 10¹⁵, comfortably inside `u64`.

The anchor is written into the stream's `ActionOrdinal` (and its cursor reset) before the cohort's
reservations begin. Two distinct `(clubId, worldDay)` pairs therefore never share a draw position, and
the same pair always reproduces the same position — which is exactly the property that lets #42
serialize **no** cursor (FR-YA-020).

**Call-site note (decided in §4.2):** the anchor is written through the seam §4.2 selects —
`RestoreStream` today, or a dedicated #16 `SeekStream` if that back-prop is taken. The **invariant** here
is the anchor value and its position-independence, not the call.

## 3.3 Cohort generation and the two transforms (FM-YA-03)

```
GenerateCohort(ref st, streamIndex, uint worldDay, in AcademyQuality q, rng) -> IntakeResult:
    size := ACADEMY_INTAKE_COHORT_SIZE + q.CohortSizeDelta        # deep-tier delta; 0 at minimal
    require ACADEMY_COHORT_SIZE_MIN <= size <= ACADEMY_COHORT_SIZE_MAX        # F2

    for k in 0 .. size-1:
        newId := AllocateYouthPlayerId(ref st)                    # monotonic high-water, F6

        # (a) #28's generator, UNMODIFIED (FR-YA-001). Consumes exactly PROGRESSION_REGEN_FIELDS draws.
        (record, life) := RegenGenerator.GenerateRegen(rng, streamIndex, st.ClubId, newId, worldDay)

        # (b) Transform 1 — the ceiling shift (FR-YA-004/005/006).
        life := ApplyCeilingShift(life, q)

        # (c) Transform 2 — the age re-anchor (FR-YA-007/008); a no-op at minimal.
        (record, life) := ReanchorAge(record, life, worldDay)

        AssertCoherent(record, life)                              # F1
        AppendProspect(ref st, record, life, worldDay)

    return IntakeResult(...)
```

### 3.3.1 `ApplyCeilingShift` — the quality dial (KD-2)

```
ApplyCeilingShift(PlayerLifecycle life, in AcademyQuality q) -> PlayerLifecycle:
    if q.CeilingShiftPerMille == 0:
        return life                                   # FR-YA-006 — byte-identical early return

    shifted := life.PotentialAbility
             + (life.PotentialAbility * q.CeilingShiftPerMille) / 1000

    # RegenGenerator's OWN generation floor, reproduced verbatim (FR-YA-005):
    paFloor := max(PA_MIN, min(life.CurrentAbility + REGEN_PA_HEADROOM, ABILITY_MAX))

    life.PotentialAbility := Clamp(shifted, paFloor, ABILITY_MAX)
    return life                                       # CurrentAbility + attributes untouched (FR-YA-004)
```

**Monotonicity (proved, not assumed).** On entry `PotentialAbility ≥ paFloor`, because the generator drew
it from `[paFloor, ABILITY_MAX]` using this same expression. Therefore a negative dial can never *raise*
PA: `shifted < PA`, and `Clamp(shifted, paFloor, MAX) ≤ max(shifted, paFloor) ≤ PA`. Symmetrically a
positive dial can never lower it. This is why the floor must be the generator's floor and not the weaker
`max(PA_MIN, CA)` — under the weaker floor the entry invariant would not pin the result, and a
sufficiently negative dial could produce a **zero-headroom** prospect that can never grow, which is a
different thing from "a weak academy".

### 3.3.2 `ReanchorAge` — bio-banding (KD-2b, deep-tier)

```
ReanchorAge(PlayerRecord rec, PlayerLifecycle life, uint worldDay) -> (PlayerRecord, PlayerLifecycle):
    if ACADEMY_AGE_MIN == REGEN_AGE_MIN and ACADEMY_AGE_MAX == REGEN_AGE_MAX:
        return (rec, life)                            # FR-YA-008 — minimal identity, byte-identical

    targetAge := ReprojectIntoBand(rec.Age, REGEN_AGE_MIN, REGEN_AGE_MAX,
                                            ACADEMY_AGE_MIN, ACADEMY_AGE_MAX)   # no new draw
    rec.Age := targetAge
    # BirthWorldDay MUST move with it, by #28's own formula (FR-YA-007):
    birthDays := targetAge * (long)DAYS_PER_YEAR
    life.BirthWorldDay := worldDay >= birthDays ? (uint)(worldDay - birthDays) : 0u
    return (rec, life)
```

`ReprojectIntoBand` is a deterministic integer re-scale of the **already-drawn** age — it consumes no
draw (FR-YA-002).

## 3.4 Promotion (FM-YA-04, KD-5)

```
Promote(ref AcademyState st, int prospectPlayerId, int seniorSquadCount) -> PromotionResult:
    idx := IndexOfProspect(st, prospectPlayerId)
    if idx < 0:                       return PromotionResult.Refused(UnknownProspect)   # F5
    if seniorSquadCount >= CLUB_SQUAD_SIZE:
                                      return PromotionResult.Refused(SeniorSquadFull)   # F5, FR-YA-025

    p := st.Cohort[idx]
    RemoveProspectAt(ref st, idx)     # academy side applied here; senior side by the ROOT (FR-YA-024)
    return PromotionResult.Accepted(p.Record, p.Life)      # PlayerId unchanged — no re-key (FR-YA-026)
```

`seniorSquadCount` is passed **in** by the composition root — #42 never reads or writes a `Squad`
(FR-YA-023). The root applies the accepted result to the senior squad and the academy removal as one
atomic step; a refusal leaves both sides untouched.

## 3.5 Worked examples

All values below are hand-verifiable integer arithmetic against the #28 constants
(`PA_MIN` = 4000, `ABILITY_MAX` = 10000, `REGEN_PA_HEADROOM` = 1000, `DAYS_PER_YEAR` = 365).

**(a) Neutral quality — the identity.** `CeilingShiftPerMille = 0` ⇒ `ApplyCeilingShift` returns on the
first line; `ACADEMY_AGE_*` equal `REGEN_AGE_*` ⇒ `ReanchorAge` returns on the first line. The prospect
is **exactly** `RegenGenerator`'s output. This is the FR-YA-006 / FR-YA-008 identity, locked by
T-YA-ID-001.

**(b) A strong academy, positive dial.** Generated `PA = 6000`, `CA = 4200`, dial `+150‰`:

```
shifted = 6000 + (6000 * 150) / 1000 = 6000 + 900          = 6900
paFloor = max(4000, min(4200 + 1000, 10000)) = max(4000, 5200) = 5200
PA'     = Clamp(6900, 5200, 10000)                          = 6900     (raised by 900)
```

**(c) A weak academy, negative dial — the floor doing its job.** Generated `PA = 8000`, `CA = 5500`,
dial `−400‰`:

```
shifted = 8000 + (8000 * -400) / 1000 = 8000 - 3200        = 4800
paFloor = max(4000, min(5500 + 1000, 10000)) = max(4000, 6500) = 6500
PA'     = Clamp(4800, 6500, 10000)                          = 6500     (lowered to the headroom floor)
```

The prospect ends weaker but still with `PA − CA = 1000` of headroom — a **low-ceiling** prospect, not a
zero-headroom one (§3.3.1).

**(d) The trigger.** `ACADEMY_INTAKE_PERIOD_DAYS = DAYS_PER_YEAR = 365`. At genesis
`HasIntaken = false`, so the first evaluated day fires regardless of its value (including day `0`);
`LastIntakeWorldDay := 0`, `HasIntaken := true`. The next intake is due at day `365`: days `1..364`
return `IntakeResult.Empty` with no draw; day `365` (or the first day after it that #30's advance loop
evaluates) fires.

## 3.6 Division convention (pinned)

The per-mille shift MUST use plain C# integer division, which truncates **toward zero** and is therefore
**sign-symmetric**: a dial of `−N‰` moves `PotentialAbility` by exactly as much as `+N‰`, in the opposite
direction, for every input. The dial has no directional bias, and the same inputs always produce the same
bytes.

This is pinned because the two obvious "cleanups" are both behaviour changes:

- `Math.Floor` rounds negatives **away** from zero, making negative dials systematically stronger than
  positive ones of equal magnitude — a silent balance change.
- `Math.Round` introduces banker's rounding **and** a `double`, violating FR-YA-022's no-float rule.

A test locks the sign-symmetry (T-YA-U-004) so either substitution fails loudly rather than drifting.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §3 (FM-YA-01..04, the anchor derivation, both transforms with the monotonicity proof, promotion, four worked examples, the division-convention note), promoted from design supplement v0.3. Status IN REVIEW. |
| 0.2 | 2026-07-24 | — | PASS-1 fix (L): §3.2 gains the `clubId < ACADEMY_CLUB_STRIDE` injectivity guard with its rationale — without it an out-of-stride clubId carries into the day/purpose digits and silently aliases two clubs onto one anchor (same cohort, no error, no divergence signal). Overflow range check recorded. |
#endregion
