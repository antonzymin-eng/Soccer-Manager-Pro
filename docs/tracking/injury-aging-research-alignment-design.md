# Injury / Aging / Form — Research-Alignment Design Supplement

> **Created:** July 26, 2026
> **Status:** DESIGN SUPPLEMENT — **✅ SIGNED OFF August 17, 2026 (owner decision).** All three structural
> decisions accepted as written: the `Severe` tier + `RECOVERY_MAX` raise (R-5), the #41→#28 aftermath seam
> (R-6 / KD-R4, with the v0.3 consumer-owns-the-seam-type correction), and the deterministic-not-drawn PA
> reduction (KD-R4a). §11 steps 1–2 are complete; steps 3–5 are unblocked and unscheduled.
> **v0.6 (August 17, 2026, post-sign-off): the sign-off's factual premise — KD-R1's "both specs are
> pre-code, so the changes are free" — was re-verified after the fact and found EXPIRED** (#41 T0/T1/T2
> landed August 5–7; #28 T1/T2a landed August 8; both version-1 sub-blobs are written by every save).
> §1, §6, §10 and §11 are corrected below; the three structural decisions and the sign-off itself
> are UNCHANGED. **Not a new
> candidate spec** — this note proposes
> corrections to the APPROVED text of **Injuries & Medical #41** and **Player Progression & Lifecycle #28**,
> and records two deliberately-undesigned forward items.
> **Candidate spec:** none · **FR prefix:** none of its own (proposes appends to FR-MD / FR-PG)
> **Owner specs:** #41 (FR-MD-028..034 proposed) · #28 (FR-PG-025..028 proposed)
> **Back-props proposed:** ERR-041-013..018 · ERR-028-020..022 *(the #28 ids re-allocated at v0.6:
> the originally proposed ERR-028-002..004 are ALL consumed — -002 was filed July 27, 2026 at #53's
> approval and is published in #28 §1/§7; -003 and -004 were filed August 8, 2026 at the #28 T1/T2a
> landing. The next free block after the highest id in use (ERR-028-019) is 020..022; mapping
> -002→-020 (R-7), -003→-021 (R-8), -004→-022 (R-6 consumer + R-11))*. **Zero** #30 / #29 / #27 / #16 changes.
> *(Id-map re-based August 7, 2026 at the balance pass: the originally proposed ERR-041-002 and
> ERR-041-003 were both filed by other findings while this supplement awaited sign-off — -002 is the T0
> `DrawKeyed` API defect, -003 the `InjuryRiskMax` `[CROSS]` retag — and 004..011 have since been either
> consumed or superseded (the T1/T2/balance-pass chain runs to ERR-041-012). The soft-reservation
> convention stands: 013..018 are reserved for this supplement's six #41-side findings, re-verified free
> at re-basing; every `Back-prop:` line below is re-pointed. Note for R-2: `BASELINE_DAILY_RISK`
> (ERR-041-011) is now the exposure-INDEPENDENT term — R-2's under-exposure arm must re-fit against it
> rather than add beside it, per #41 Appendix A's note.)*
> *(**`ERR-041-013` NARROWED August 22, 2026 at `ERR-041-021`.** R-1's plumbing half landed
> independently as `ERR-041-020` — the `int ageYears` parameter and a monotone linear age term — so
> -013's reserved scope is now the **residual U-shape / young-tail arm only**. The other five
> reservations (014..018) are untouched. See the box at R-1 before acting on it.)*
> **Determinism impact:** none — no new RNG stream, no new domain tag, no new `SubsystemOrdinal`,
> no `DETERMINISM_DIGEST_VERSION` bump (§5).
> **Save impact (corrected v0.6):** R-4's two `InjuryState` fields and R-6's `PlayerLifecycle` field are
> now **format bumps with no migration path** — `MedicalSaveCodec` (August 5) and `ProgressionSaveCodec`
> (August 8) write version-1 sub-blobs in #30's season frame (`SEASON_SAVE_FORMAT_VERSION` = 6), and
> both codecs' F3 refuses Stage-0 cross-version loads (§6). The v0.1–v0.5 "no byte has ever been
> written" argument is void; KD-R1's window has closed.

---

## 0. Scope

This supplement reconciles two APPROVED specs against the published sports-science literature on injury
occurrence, injury aftermath, and athletic aging. It covers:

- **Injuries & Medical #41** — the occurrence model (§3.4 `AssembleRiskScore`), the severity/recovery model
  (§3.1/§3.2), and the deferred recurrence and attribute-decline seams (§0 / KD-4).
- **Player Progression & Lifecycle #28** — the age-band model (§4.3 / `ClassifyAgeBand`) and the decline
  mechanism (`AbilityModel.DrainOnePoint`).

**Out of scope (recorded, not designed — §9):** match form, congestion→coordination degradation, and the
contract-year effect. Each is recorded with the evidence and a binding constraint for its eventual owner,
because designing them here would either invent a surface with no owner (FR-LW-031) or model an effect the
football-specific evidence does not support.

**Explicitly not proposed:** any change to #29 Training, #30 Season Loop, #27 Squad Data, #16 Determinism,
or #33 Personalities. Every finding below is containable inside #41 and #28.

## 1. Why now — the cost argument (KD-R1) — **VOID at v0.6**

The finding that governed this supplement's *timing* was about cost, not content. **Its factual premise
expired between writing (July 26) and sign-off (August 17), and is corrected here** — re-verified against
`src/` on August 17, 2026:

| Owning spec | Implementation state (**re-verified August 17, 2026**) | Cost of these changes today |
|---|---|---|
| **#41 Injuries & Medical** | **Built and live.** `src/injuries-medical/` landed T0 August 5, 2026; T1 the same wave — `MedicalSaveCodec.cs` writes `InjuryState` field-by-field (Severity, RecoveryRemaining, InjuryCount, LastAdvancedWorldDay) under `MEDICAL_SAVE_FORMAT_VERSION = 1` as a mandatory sub-blob of #30's season frame; T2 (August 6) wired the day step at `SeasonLoop` slot 4; the occurrence dial was ARMED at the August 7 balance pass (FR-MD-027). | R-1..R-3 modify a live daily step (`MedicalStep.AdvanceMedicalDay` runs at slot 4 for every club). R-4's two `InjuryState` fields are a `MEDICAL_SAVE_FORMAT_VERSION` 1 → 2 bump; `MedicalSaveCodec.Decode` refuses a version mismatch outright ("no cross-version migration at Stage 0 (F3)"), so existing saves are invalidated. R-5's `Severe` tier changes live severity classification. |
| **#28 Player Progression** | **Built and live.** T0 (July) plus T1/T2a August 8, 2026 — `ProgressionSaveCodec.cs` writes `PlayerRecord` + `PlayerLifecycle` per player under `PROGRESSION_SAVE_FORMAT_VERSION = 1` (magic-led, ERR-028-004); `ProgressionEngine` is live at `SeasonLoop` slot 1 and the block is **the serialized career roster** (KD-4). | R-7/R-8 change live career behaviour exercised daily at slot 1, not just T0 test expectations. R-6's `PlayerLifecycle.LastAftermathAppliedDay` is a `PROGRESSION_SAVE_FORMAT_VERSION` 1 → 2 bump with the same F3 no-migration refusal — and because the block is the roster authority (KD-4), a refused load invalidates the whole career, not one subsystem's state. |

So the v0.1–v0.5 framing — "#41-side changes are corrections before first code, #28-side changes are
corrections before first consumer, both free exactly once" — **is no longer true in any part**: both specs
have code, both have production consumers, and both formats have written version-1 bytes. What survives is
the *content* argument: a wrong-shaped model cannot be fixed by later fitting (§10), so the findings are
still correct to land — at the price of a coordinated format bump per owning sub-blob rather than for free.

## 2. Evidence basis

Each finding below cites one or more of these. Effect strengths are the reviewer's assessment of how well
the finding is established, not a claim about magnitude.

| # | Finding | Strength |
|---|---|---|
| E-1 | Matches are ~8× more dangerous per hour than training (24–30 vs 3–5 injuries / 1000 h). | Strong |
| E-2 | Dose–response of match exposure to injury is **U-shaped**, not monotone: <~15 or >~35 matches/12 months both elevate risk (7-season rugby cohort); in football, *reduced* exposure in the previous two matches predicts hamstring injury. | Moderate (cross-code for the U-shape) |
| E-3 | Recovery interval dominates raw totals: muscle-injury rate ~20% lower at 6–10-day spacing vs ≤3 days (14-year, >130 000 match observations). | **Strongest single effect** |
| E-4 | Age is the primary effect modifier: musculoskeletal maturity continues to ~24–25; the 16–20 band carries elevated risk at adult match intensity; growth-phase (PHV) risk peaks around >7.2 cm/yr growth. | Strong |
| E-5 | Re-injury rate ~15–17% (ACL) and ~15–20% (hamstring); early return is a significant predictor of second ACL. | Strong |
| E-6 | ACL: 67.2% return to play at a mean **11.6 months**; only **47.5% maintain their previous level**; ~2 years to regain pre-injury minutes/appearances; measurable decline persists ~2 seasons; 65% still at top level at 3 years. | Strong |
| E-7 | Peak age 25–27, strongly position-dependent: wingers peak ~26.1 and decline fastest; centre-backs hold to 31–32; goalkeepers latest. Aggregate: 75% of peak at 32.2, 50% at 34.5. | Strong |
| E-8 | Physical qualities peak *earliest* and decline first — endurance ~24.8, speed ~25.7, explosiveness ~26 — which is the mechanism behind E-7's positional spread. | Strong |
| E-9 | Player-attributable variance in performance ratings is only ~20–26%; finishing overperformance ~7–30% (mean ~21%); EPL shows performance *reversal* after winning streaks, no goalscoring momentum. | Strong |
| E-10 | Fixture congestion: total distance is preserved (players pace themselves) but **tactical performance and inter-player synchronisation degrade**; intensity drops ~12% after 70′ on a 3-day cycle. | Moderate |
| E-11 | Contract-year effect is established in the NFL and AFL but **not** in football — 249 players, four major European leagues, 2008–2015 found no final-year boost and no post-signing decline. | Moderate (a null result) |

## 3. Findings

Severity convention for this supplement: **H** = the model produces behaviour the evidence contradicts
outright *and* the fix is structural; **M** = a missing modulator or input; **L** = tuning or documentation.

---

### R-1 (H) — `AssembleRiskScore` has no age term at all

> ### ⚠️ LANDED IN PART, August 22, 2026 — `ERR-041-020`. Read this box before acting on R-1.
>
> The finding below was filed independently by the football-judgment proxy review and **its plumbing
> half shipped** as `ERR-041-020` (spec + code, same commit): `AdvanceMedicalDay` and
> `AssembleRiskScore` now take `int ageYears` — caller-supplied at #30's composition root, exactly the
> KD-R2 value-not-reference shape this finding proposed — and §3.4 assembles an integer age term
> before the `OccurrenceRiskMillMult` scaling and the clamp. `#41` now has a `src/` assembly, so the
> §11 note below ("R-1..R-5 are spec text until #41 T0 opens") is spent for this finding.
>
> **The shapes DIVERGE, and the difference is the whole of what remains.** R-1's contract is
> **U-shaped** — elevated below `AGE_MATURITY_YEARS` (24), zero across the prime band, rising above
> `AGE_VETERAN_YEARS` (31) — from **E-4, rated Strong**, whose academy half is explicit: *the 16–20
> band carries elevated risk at adult match intensity*. What shipped is **monotone linear** about a
> pivot of 26, so a 19-year-old receives **−1050** and 16–20-year-olds become **the safest players in
> the league**. That reproduces E-4 above the pivot and **inverts** it below. `ERR-041-020`'s claim
> that the term is "the direction and rough magnitude the epidemiology supports" is accordingly true
> of the veteran arm only; `ERR-041-021` corrected that claim at #41 §3.4, Appendix A and the
> catalogue, and this box is its counterpart here.
>
> **`ERR-041-013` is NOT spent — it is narrowed.** Its reserved scope is now exactly the residual:
> the **U-shape / young-tail arm**. The plumbing (the `ageYears` parameter, the seam shape, the
> KD-R2 argument) is closed by `ERR-041-020` and must not be re-proposed. A refit **re-shapes the
> shipped term** rather than adding a second age term beside it — the same instruction #41 Appendix A
> already carries for R-2 against `BASELINE_DAILY_RISK`.
>
> **Nothing here re-shapes the term.** The U-shape is this supplement's design, this supplement is
> **awaiting owner sign-off**, and changing shipped football behaviour is the owner's call rather than
> a review loop's. Two implementation notes for whoever lands it: the band-table form proposed below
> would reintroduce the age **threshold** doctrine P1 forbids and `ERR-028-020` has just removed from
> the sibling spec, so the U-shape wants a continuous piecewise form; and the shipped term's P5 pivot
> (the bootstrap mean age, which is why the landing moved no measured aggregate) does not survive an
> asymmetric U — a refit must re-establish P5 rather than inherit it.

**Evidence:** E-4. **Owner:** #41 §3.4 / Appendix A. **Back-prop:** ERR-041-013 — **narrowed at
`ERR-041-021` to the residual U-shape / young-tail arm only** *(re-based; originally -002, since taken)*.

`AssembleRiskScore(trainingRisk, load, attributes, medical)` (#41 §3.4) assembles risk from training fatigue,
appearance load, a robustness term derived from `Strength`/`Stamina`/`Balance`, and the staff modifier.
**Age is not an input.** A 19-year-old and a 34-year-old with identical attributes and identical load carry
identical injury risk, which contradicts the single best-established modifier in the literature.

The input is already free: #28 serializes `BirthWorldDay` and derives `ageYears` on the same world tick, and
#30's day-advance loop runs #28 (slot 1) before injuries (slot 4).

**Proposed fix.** `AdvanceMedicalDay` takes an additional `int ageYears`, and `AssembleRiskScore` gains an
additive integer term from a `[GT]` band table:

```
risk = TRAINING_RISK_PASSTHROUGH_WEIGHT * trainingRisk.RiskScore
     + MatchLoadRisk(load)                       # R-2 / R-3 replace the linear term
     + AgeRiskDelta(ageYears)                    # NEW — [GT] band table, signed integer
     + RecurrenceRisk(s, worldDay)               # R-4
     - RobustnessMitigation(a)
```

`AgeRiskDelta` is a piecewise-constant `[GT]` table over age bands (illustrative, pending the balance pass):
elevated below `AGE_MATURITY_YEARS` (24), zero across the prime band, rising above `AGE_VETERAN_YEARS` (31).
It is **not** a curve fit — the contract is the shape (U-shaped in age, minimum in the prime band) and the
integer discipline.

**Explicitly not proposed:** a PHV / growth-spurt model. `PlayerRecord` has no height and no growth rate, and
inventing one to model E-4's academy half would be a #27 schema ripple for a population (16–17 year olds) that
`RosterGenerator` barely produces (`REGEN_AGE_MIN` = 16). The age-band term captures the professional half of
E-4; PHV is recorded as a deep-tier item requiring a #27 append.

**Passing age as a value, not a reference (KD-R2).** `ageYears` is caller-supplied at #30's composition root,
derived from #28's lifecycle. #41 gains **no** reference to #28 — the `TrainingInput` / `MedicalModifier`
precedent (a value type, not an interface; FR-LW-031). The reference DAG is unchanged.

---

### R-2 (M) — match-load risk is monotone; the evidence is U-shaped

**Evidence:** E-2. **Owner:** #41 §3.4 / Appendix A. **Back-prop:** ERR-041-014 *(re-based; originally -003, since taken)*.

`APPEARANCE_LOAD_WEIGHT * load.AppearanceDays` rises linearly and without bound, so under the current model an
**unused player is maximally safe**. The rugby cohort puts both tails at elevated risk, and the football
hamstring finding puts reduced recent exposure on the risky side.

**Proposed fix.** Replace the linear term with a piecewise-linear integer `MatchLoadRisk(load)` over a `[GT]`
band: a penalty below `MATCH_LOAD_UNDER_DAYS`, flat through the middle band, rising above
`MATCH_LOAD_OVER_DAYS`. Integer-only, no division.

**Double-count risk (KD-R3), and why the two arms live where they do.** #29 already exposes
`InjuryRiskContribution` as "computed from `TrainingFatigue` + **low Condition**" (FR-TR-017), so the
deconditioning arm partly exists. These are *not* the same quantity and must not be merged:

- #29's `Condition` is **training-driven** general conditioning — it falls with inactivity of any kind.
- #41's `AppearanceDays` is **match-driven** competitive exposure — match sharpness, which a fully trained
  player returning from a ban or a bench spell still lacks (this is precisely what E-2's football finding
  measures: reduced exposure in the previous two *matches*).

They read distinct accumulators in distinct assemblies, so no counter is shared and a double count is not
representable — the same argument #41 KD-2 already makes for fatigue. The residual risk is **additive
over-weighting** of the left tail, which is a balance-pass concern (both terms are `[GT]`), recorded in §10.

---

### R-3 (M) — no representation of recovery interval, the best-evidenced effect

**Evidence:** E-3. **Owner:** #41 §2.2 (`MatchLoad`) / §3.4. **Back-prop:** ERR-041-015 *(re-based)*.

`MatchLoad` carries `AppearanceDays` (a count) and `HardContacts` (deep-tier). Nothing encodes **spacing**,
yet ≤3-day vs 6–10-day turnaround is the strongest single quantified effect in the corpus (~20%). Two players
with identical appearance counts and radically different congestion are indistinguishable to the model.

**Proposed fix.** `MatchLoad` gains `int ShortTurnaroundCount` — appearances made within
`CONGESTION_INTERVAL_DAYS` (`[GT]`, illustrative 3) of the player's previous appearance. `MatchLoadRisk` adds
`CONGESTION_WEIGHT * ShortTurnaroundCount`.

`MatchLoad` is caller-supplied (FR-MD-010 — "#41 never tracks match participation itself"), and #30's fixture
result already carries the fixture day, so the counter is derivable at the composition root with no new #41
state and nothing new to serialize. `MatchLoad.None` (all-zero) remains the neutral value.

---

### R-4 (M) — recurrence is deferred to Stage 3, but its input is already serialized

**Evidence:** E-5. **Owner:** #41 §3.1 / §7 KD-4. **Back-prop:** ERR-041-016 *(re-based)*.

`InjuryState.InjuryCount` is serialized and, at Stage 2, **read by nothing**. Re-injury (~15–20%) is among the
most robust numbers in the literature and, unlike most deep-tier items, its producer already exists.

**Proposed fix.** Promote recurrence from Stage 3 to the Stage-2 minimal tier as a deterministic integer
multiplier — no new draw:

```
RecurrenceRisk(s, worldDay):
    if s.InjuryCount == 0: return 0
    if s.LastReturnWorldDay == MEDICAL_NOT_ADVANCED_SENTINEL: return 0
    daysSinceReturn = worldDay - s.LastReturnWorldDay
    if daysSinceReturn > RECURRENCE_WINDOW_DAYS: return 0
    return RECURRENCE_RISK_BY_SEVERITY[s.LastSeverity]        # [GT], decaying over the window
```

`InjuryState` gains two fields — `uint LastReturnWorldDay` and `InjurySeverity LastSeverity` — set when the
recovery countdown reaches zero. Both are integer. **(Corrected v0.6:)** both now cost a
`MEDICAL_SAVE_FORMAT_VERSION` 1 → 2 bump — `MedicalSaveCodec` has written version-1 files since
August 5, 2026, and its `Decode` refuses a version mismatch with no Stage-0 migration path (F3) — see §6.

This also gives E-5's "early return predicts second injury" a home at the deep tier, where a manager-forced
early return would shorten `RecoveryRemaining` and raise the recurrence term for the same window.

---

### R-5 (H) — the severity model cannot represent the injury class that shapes careers

**Evidence:** E-6. **Owner:** #41 §2.2 / §3.2 / Appendix A. **Back-prop:** ERR-041-017 *(re-based)*.

Appendix A pins `RecoveryDaysForTier` at Minor 7 / Moderate 21 / **Serious 60** and `RECOVERY_MAX` at 240.
The ACL mean return-to-play is **~11.6 months (~350 days)** — longer than the model's ceiling, let alone its
worst tier. The Stage-2 model's most severe outcome is a two-month muscle injury. A season-ending injury is
**structurally unrepresentable**, so no amount of `[GT]` tuning reaches it.

This matters beyond realism: a season-ending injury is the event that drives squad-depth decisions, transfer
urgency (#31), and board expectation (#45). Its absence removes a whole class of managerial pressure.

**Proposed fix, in three parts:**

1. **`InjurySeverity` gains `Severe = 4`** — an APPEND after `Serious = 3`, preserving every existing ordinal
   (the project's APPEND-only enum rule; `RestartType` / `PassType` precedent).
2. **`RecoveryDaysForTier[Severe]` = 350** `[GT]` — set at E-6's ACL mean rather than below it, so the tier
   can actually represent the injury that motivated it — and **`RECOVERY_MAX` 240 → 400** so the tier plus a
   deep-tier recurrence extension fits under the ceiling.
3. **The per-mille table re-splits** within the unchanged `SEVERITY_PERMILLE_DENOM = 1000`: Minor 600 /
   Moderate 300 / Serious 90 / **Severe 10**. The §3.2 bucketing generalises from two comparisons to a
   cumulative walk; it remains integer cross-multiplication with no second draw, so #41's KD-1
   "exactly one draw per player per occurrence-eligible day" is untouched.

The catalogue invariant becomes `Σ SEVERITY_*_PERMILLE ≤ SEVERITY_PERMILLE_DENOM` over four tiers.

---

### R-6 (H) — a player returns from injury with byte-identical attributes

**Evidence:** E-6. **Owner:** #41 §0 / #28 §3.1. **Back-prop:** ERR-041-018 *(re-based)* (signal) + ERR-028-022 (consumer) *(re-allocated v0.6; originally -004, consumed August 8, 2026 at the #28 T1/T2a landing)*.

#41 §0 puts "attribute decline from injury" out of scope and exposes "a read-only injury signal #28 *may*
later read". #28 has no such reader. So the seam is **named on one side and built on neither**, and the shipped
model asserts full recovery: a player completes a 60-day Serious injury and resumes with exactly the attributes
they had. The evidence is the opposite and unusually specific — only **47.5% maintain their level**, ~2 years to
regain minutes, measurable decline persisting ~2 seasons.

This is the largest *modelling* gap in the audit, though not the cheapest fix.

**Proposed fix (KD-R4).** A one-directional value-type signal populated at #30's composition root. #28 remains
the sole attribute writer (FR-PG-008 preserved), and **neither assembly gains a reference to the other**.

The ownership is the load-bearing part. The seam type is **#28-owned** (it lives in
`src/player-progression/`), not #41-owned — exactly mirroring `TrainingInput`, which is the #29 seam yet
lives in `src/player-progression/TrainingInput.cs` (#28 §4.5). A producer-owned type would force
`player-progression.asmdef` to reference #41's assembly to name the parameter, which is the coupling this
design exists to avoid. For the same reason the struct carries an **integer severity rank**, not #41's
`InjurySeverity` enum — the consumer's vocabulary, so no #41 type crosses the boundary at all:

```csharp
// #28-OWNED (src/player-progression/InjuryAftermath.cs) — the #41 seam value type, mirroring
// TrainingInput. #28 references nothing new; #30 populates it from InjuryState's public fields.
public readonly struct InjuryAftermath
{
    // IDENTITY GATE: consumers MUST gate on SeverityRank == 0, NOT on a DaysSinceReturn sentinel.
    // Rank 0 is "no aftermath", so `default` — and therefore `None` — is provably inert. A
    // "-1 means absent" convention on an int field would be the zero-value trap this project has
    // hit repeatedly (MatchFrameView.Empty's bool IsEmpty defaulting false; MarkingOrientation /
    // LineOfEngagement zero-value enum defaults).
    public readonly int  SeverityRank;        // 0 = none; ascending = worse. The discriminator.
    public readonly int  DaysSinceReturn;     // meaningful only when SeverityRank > 0
    public readonly int  CareerInjuryCount;
    public readonly uint LastReturnWorldDay;  // idempotency key for the one-shot below
    public static InjuryAftermath None => default;   // identity — provably zero effect
}
```

`InjurySeverity`'s ordinals are already ascending in severity (`None = 0 … Severe = 4`), so #30's projection
is `(int)state.LastSeverity` — no mapping table, and the R-5 append extends it for free.

#28's `AdvanceDayForPlayer` takes `in InjuryAftermath aftermath` (defaulting to `None`) and applies two
distinct effects, matching the two distinct findings in E-6:

- **Transient (the ~2-season minutes/rating decline).** While `DaysSinceReturn <= AFTERMATH_WINDOW_DAYS`,
  `DailyPoints` is reduced by a `[GT]` severity-scaled amount — a temporary drag on the existing cursor, with
  **no new accumulator** (FR-PG-002/003 preserved).
- **Permanent (the 52.5% who do not return to level).** On a *new* `Serious`-or-worse injury, a one-shot
  integer reduction of `PotentialAbility`, scaled by severity **and by age** (older players recover worse —
  E-4 × E-6). Idempotency uses a new `PlayerLifecycle.LastAftermathAppliedDay` compared against
  `aftermath.LastReturnWorldDay`, so replaying a day cannot double-apply it.

**Why deterministic and not drawn (KD-R4a).** E-6's 47.5% is distributional, and the faithful model draws.
But #28 is **draw-free by contract** (FR-PG-002), and adding a stream to it would allocate the reserved
`_RESERVED_0x21_`-class row this project has repeatedly refused to allocate without a genuine draw site. The
deterministic age-scaled reduction is proposed instead; the distributional version is recorded as a deep-tier
item that, if ever built, belongs on **#41's existing keyed occurrence derivation** (the
`DOMAIN_TAG_INJURIES_MEDICAL` SplitMix64 derivation — no registered stream, ERR-041-012; drawn at injury
time, carried on the aftermath value) rather than as a new #28 stream. This keeps #28's draw-free contract intact
either way.

**One-day staleness (KD-R4b), and why it is correct.** #30's pinned tick order is
`1 #28 · 2 #29 · 3 #33 · 4 injuries · 5 AdvanceDay`, so #28 at slot 1 reads an `InjuryState` last written on
day *D−1*. The aftermath signal is therefore **one day stale by construction**. This is acceptable and must be
*documented*, not fixed: it is a monotone day counter over a multi-hundred-day window, so a one-day lag is
immaterial; and reordering #30's pinned sequence to chase it would re-pin a reserved seam position, which the
ERR-030-002 precedent explicitly avoids. The #23 one-stride-stale contract is the governing precedent.

---

### R-7 (H) — age bands are position-blind, so all positions age identically

**Evidence:** E-7. **Owner:** #28 §4.3 / Appendix A / `AbilityModel.ClassifyAgeBand`.
**Back-prop:** ERR-028-020 *(re-allocated v0.6; originally -002, consumed July 27, 2026 at #53's approval)*.

Verified in code: `AbilityModel.ClassifyAgeBand(int ageYears)` takes age alone and compares against the global
scalars `GROWTH_AGE = 24` / `DECLINE_AGE = 30`. Every player in the game grows until 24, plateaus, and declines
after 30, regardless of position — while the positional spread (wingers ~26.1 and falling fastest; centre-backs
holding to 31–32; goalkeepers latest) is the most interesting and best-established part of E-7.

The seam is already half-built: `GrowthProjection.DailyPoints(band, rec.Position, in training, curveEnabled)`
**already takes position** and discards it at T0. Only the band classifier is position-blind.

**Proposed fix.** `ClassifyAgeBand(int ageYears, PlayerPosition pos)` reads per-position `[GT]` tables
`GROWTH_AGE_BY_POSITION[POSITION_COUNT]` / `DECLINE_AGE_BY_POSITION[POSITION_COUNT]`, replacing the two
scalars. Illustrative rows pending the balance pass: GK 25/33 · DF 24/31 · MF 24/30 · FW 23/29.

**Honest limitation.** `PlayerPosition` is the coarse 4-value enum (GK/DF/MF/FW), so it **cannot express
"winger vs striker"** — E-7's sharpest single split. The coarse table captures GK-latest / DF-slowest /
FW-earliest, which is the bulk of the effect; the winger/striker split needs positioning-ai's 13-value
`RoleId`, and routing that into `player-database` would be a layering change (a Mechanics enum reaching a
bottom-of-graph data assembly). Recorded as deep-tier, **not** proposed here.

---

### R-8 (H) — decline sheds the wrong attributes, and is nearly invisible to CA

**Evidence:** E-8. **Owner:** #28 §3.1 / `AbilityModel.DrainOnePoint`. **Back-prop:** ERR-028-021 *(re-allocated v0.6; originally -003, consumed August 8, 2026 at the #28 T1/T2a landing)*.

Verified in code (`AbilityModel.cs`): `DrainOnePoint` walks `for (level = 0; level <= maxBias; level++)` —
**lowest position-bias first**, documented as "a declining player sheds their least-emphasised attributes
first." Two consequences, and the second is worse than the first:

1. **The order is backwards versus the mechanism.** E-8 puts physical qualities at the earliest peak, and that
   is *why* wingers fall off faster than centre-backs. Under the current rule a 34-year-old winger sheds
   Marking and Tackling — attributes they never used — and **keeps their Pace**. The model therefore cannot
   produce the positional divergence R-7 is trying to enable, even with per-position bands: the two findings
   are coupled, and fixing R-7 alone would produce differently-timed decline of the wrong attributes.

2. **Decline is maximally CA-inefficient, so the curve has the wrong *shape*.** `ComputeCA` weights each
   attribute by `1 + bias` (verified: `AbilityModel.ComputeCAFromArray`), so `ΔCA ∝ −w_i` for the drained
   attribute *i*. Draining the **lowest**-bias attribute therefore costs the **minimum CA the model can
   charge** on every step. The consequence is about shape, not magnitude: CA declines more slowly than the
   attribute-drain rate implies for as long as low-bias attributes remain above `ATTRIBUTE_MIN`, then
   accelerates once the drain is forced upward into high-weight attributes — a flatter-then-steeper curve
   where E-7 describes a smooth decay. Since CA is the summary every downstream consumer reads (#31
   valuation, #32 scouting, `SquadRating`), the mis-shaped curve propagates well beyond #28.

   *(The magnitude of the flattening depends on the spread of `PositionAttributeBias`, which this note does
   not measure — the claim above is about the sign and shape, both of which follow from `ΔCA ∝ −w_i` alone.)*

**Proposed fix.** A new `[GT]` `DECLINE_PRIORITY[ATTRIBUTE_COUNT]` table keyed by `AttrIdx`, ordering
attributes by how early they decline: physical (Pace, Acceleration, Stamina, Agility, Jumping) first; technical
next; mental (Positioning, Decisions, Anticipation, Composure) last. `DrainOnePoint` walks that table, with the
**existing position bias retained as the within-class tie-break**, so positional identity is preserved.

**Explicitly not proposed:** simply reversing to highest-bias-first. That would drain a striker's Finishing
and a centre-back's Marking first — a different wrong answer, and it would over-correct CA (every drained point
would carry the maximum weight). Decline priority is a physiological ordering, not a positional one; conflating
them is what produced the current defect.

---

### R-9 (M) — form is unowned; record the amplitude constraint before someone builds it

**Evidence:** E-9. **Owner:** none. **Status: RECORDED, NOT DESIGNED. No back-prop.**

`docs/specs/training-system/section-1.md` states: *"Match-participation-driven sharpness / morale ('form') — a
future owner's concern."* #29's design-AR collapsed a Form/Fitness two-cursor muddle into a single `Condition`
cursor and deferred match-driven form. #33 covers morale, relationships and cliques — not performance form.
So nothing computes form and nothing owns it.

**The deferral is correct and should stand.** E-9 shows player-attributable variance in ratings is only
~20–26%, finishing overperformance is mostly noise, and the EPL exhibits performance *reversal* after winning
streaks rather than momentum. A form system is a small-amplitude effect that the genre habitually oversizes.

**What this note adds is a binding constraint for the eventual owner**, so the deferral does not become an
unconstrained future addition:

- **Amplitude ceiling.** A form term must not move a player's effective output by more than the
  player-attributable share of variance the data supports (~20–25%). Anything larger overwhelms the ability
  signal it modulates.
- **Mean-reverting by construction**, with no momentum term. If a "hot streak" is representable at all, the
  model must also make reversal at least as likely — E-9's actual finding.
- **Context first.** Most *visible* performance variation should emerge from opponent, teammates and role —
  which the match engine already models — rather than from a per-player form scalar. Form that has to carry
  work the engine should be doing is a symptom, not a feature.
- **Where it must not go.** Not into `PlayerAttributes` (#27 is truth, and #32's fog-of-war is a view over
  truth); not into #28's `GrowthCursor` (that is the aging accumulator, and #29's AR already rejected exactly
  this conflation once).

---

### R-10 (M) — congestion degrades coordination, and there is no path for that

**Evidence:** E-10. **Owner:** #29 KD-1 projection seam + the match engine.
**Status: RECORDED, NOT DESIGNED. No back-prop.**

One genuine congestion mechanism exists: #29's KD-1 projects world-tick training fatigue into the match-boot
starting fatigue (`1 − AerobicPool`), so a congested squad starts matches tired and Agent Movement responds.
That is the **physical** half.

E-10's actual finding is that the physical half largely *holds up* — total distance is preserved because
players pace themselves — while **tactical performance and inter-player synchronisation degrade**, with
intensity dropping ~12% after 70′ on a 3-day cycle. The coordination half has no path into the engine at all.

**Why this is not designed here.** It requires deciding which subsystem *carries* coordination — a squad-level
cohesion scalar modulating #12's spacing/compactness tolerance, or decision noise in #8, or both — and that is
a match-engine architecture question with its own owner, not an injury/aging question. Designing it in this
note would invent a surface with no specified consumer, which is the FR-LW-031 trap.

**Recorded for its eventual owner:** the shape is a *coordination* penalty, not a speed penalty; it should
scale with recent match density (the same `ShortTurnaroundCount` R-3 introduces) and grow within the match
rather than applying flat from kickoff.

**Upstream state (corrected v0.6).** ERR-030-014 was **resolved July 26, 2026** (match-engine §5.Z
Phase H — a production match develops play), so a coordination effect is no longer unobservable in
principle. What still gates it is calibration fidelity, not playability: the engine's contact stream is
pre-tackle until W2 arms (`TackleContactRadiusM` ships at 0), and KD-W1 defers `[GT]` calibration to the
single post-W2 pass.

---

### R-11 (L) — retirement is a hard deterministic age gate

**Evidence:** E-6 (career attrition), E-7. **Owner:** #28 §3.4. **Back-prop:** folded into ERR-028-022 *(re-allocated v0.6; originally -004, consumed August 8, 2026)*.

`RETIREMENT_AGE = 36`, deterministic, no draw. Defensible as a minimal-tier rule, but E-6 puts career
attrition squarely on injury history (65% still at top level three years post-ACL), and `InjuryCount` /
`LastSeverity` are exactly the inputs — arriving via R-6's aftermath value anyway.

**Proposed fix (small).** The hard gate stays as the ceiling; the aftermath signal may lower the effective
retirement age by a `[GT]` amount per severe career injury, floored at `RETIREMENT_AGE_MIN`. Deterministic,
integer, no draw. Lands with R-6 or not at all — it is the same input and the same consumer.

---

### R-12 (L) — do not import the contract-year effect

**Evidence:** E-11. **Owner:** #31 Transfers & Contracts (advisory).
**Status: RECORDED as a negative result. No back-prop.**

The contract-year performance boost is real in the NFL (and the AFL shows performance rising later in the
contract cycle), and it is a standard genre feature. The football-specific study — 249 players across the
Bundesliga, Premier League, Ligue 1 and La Liga, 2008–2015 — found **no** final-year boost and **no**
post-signing decline.

Recorded here so that a future #31 T-phase does not import the American-sports finding by analogy. If it is
built anyway (as a *game-design* choice rather than a simulation-fidelity one), that should be stated
explicitly rather than justified by evidence that does not exist for this sport.

---

## 4. Staging (minimal-first → deep, one code path)

Consistent with #41 KD-8 and #28 KD-8 — every addition defaults to an identity that reproduces the
pre-supplement model, so the deep tier extends one code path rather than forking it.

| Term | Stage-2 minimal | Identity (dial off / neutral input) | Deep tier |
|---|---|---|---|
| `AgeRiskDelta` | `[GT]` band table | all-zero table ⇒ pre-supplement risk | PHV / growth-rate term (needs #27 append) |
| `MatchLoadRisk` | piecewise band | single band ⇒ the current linear term | per-minute load from the event ledger |
| `ShortTurnaroundCount` | caller-supplied count | `MatchLoad.None` ⇒ 0 ⇒ no contribution | ledger-derived per-fixture physical load |
| `RecurrenceRisk` | integer multiplier over a window | `InjuryCount == 0` ⇒ 0 | early-return interaction; distribution-driven |
| `Severe` tier | fourth fixed tier | 10‰ slice ⇒ rare, and the other three tiers keep their days | distribution-driven severity draw |
| `InjuryAftermath` | transient drag + one-shot PA cut | `InjuryAftermath.None` ⇒ zero effect | drawn (distributional) PA outcome, on #41's stream |
| Per-position bands | `[GT]` per-position table | identical rows ⇒ the current global scalars | `RoleId`-granular bands |
| `DECLINE_PRIORITY` | `[GT]` ordering over `AttrIdx` | priority equal to inverse position bias ⇒ current order | attribute-specific decline rates |

Every identity above is **exactly** reconstructible, so the supplement's own correctness can be locked by a
test that pins the pre-supplement behaviour under identity settings (§8).

## 5. Determinism impact — none (KD-R5)

- **No new RNG stream, domain tag, or `SubsystemOrdinal`.** Every proposed term is a *deterministic integer
  modulator* on inputs to the existing single keyed occurrence draw (the `DOMAIN_TAG_INJURIES_MEDICAL`
  SplitMix64 derivation — no registered stream, ERR-041-012). The `Severe` tier reuses that
  same draw through the §3.2 bucketing (still no second draw). Recurrence and aftermath are multipliers and
  countdowns. #28 remains draw-free (FR-PG-002).
- **The keyed-draw property is preserved.** #41 KD-1 keys on `(playerId, worldDay, purpose)` with a fixed
  `DRAW_PURPOSE_RADIX`. Nothing here adds a draw purpose, so every existing ordinal is unchanged and the
  append-parity argument stands untouched.
- **Age does not perturb the draw.** `ageYears` is a pure function of `(worldDay, BirthWorldDay)` (#28 §3.1.1,
  no discrete rollover), so it enters only the *threshold* the draw is compared against — never the draw.
  Save→restore reproduces it exactly with nothing to continue.
- **No `DETERMINISM_DIGEST_VERSION` bump.** No #16 change of any kind is proposed.

## 6. Save impact — **two format bumps, no migration path (corrected v0.6)**

| Block | Change | Version impact (**re-verified August 17, 2026**) |
|---|---|---|
| `MEDICAL_SAVE_FORMAT_VERSION` | `InjuryState` gains `LastReturnWorldDay` + `LastSeverity` | **Bump 1 → 2.** `MedicalSaveCodec` (landed August 5, 2026) serializes `InjuryState` field-by-field at version 1, and `Decode` refuses a version mismatch — "no cross-version migration at Stage 0 (F3)" — so every existing save's medical block becomes unloadable at the bump. |
| `PROGRESSION_SAVE_FORMAT_VERSION` | `PlayerLifecycle` gains `LastAftermathAppliedDay` | **Bump 1 → 2.** `ProgressionSaveCodec` (landed August 8, 2026) serializes `PlayerRecord` + `PlayerLifecycle` per player at version 1 with the same F3 refusal — and since #28's block is the serialized career roster (KD-4), a refused load invalidates the whole career. |
| `MatchLoad.ShortTurnaroundCount` | new field | **None** — `MatchLoad` is still caller-supplied per day and never serialized (FR-MD-010); it is now *derived* at #30's composition root from the persisted `APPR` v1 appearance record, which already carries the per-day bitmask the count would be computed from. |
| `InjuryAftermath` | new **#28-owned** value type (KD-R4) | **None** — projected from `InjuryState`'s public fields at #30's composition root; stores nothing. |
| `SEASON_SAVE_FORMAT_VERSION` | — | **None** — no sub-blob is added or removed (currently 6; sub-blob versions are interior to their own opaque payloads). |

The v0.1–v0.5 version of this table read "**None**" in the first two rows because "no file has ever been
written at version 1". That was true when written (July 26) and false by sign-off: #41 T1 landed
August 5, 2026 and #28 T1 landed August 8, 2026, so the timing argument this table carried **expired at
#41 T1 exactly as it predicted**. The cost of R-4 and R-6 is now a coordinated format bump per owning
sub-blob, with KD-7/F3 forbidding any Stage-0 migration — old saves are refused, not converted.

## 7. Primary surfaces (proposed → pinned in the section files)

```csharp
// ---- #41 -------------------------------------------------------------------

public enum InjurySeverity : byte { None = 0, Minor, Moderate, Serious, Severe }   // APPEND (R-5)

public struct InjuryState
{
    public InjurySeverity Severity;
    public int  RecoveryRemaining;
    public int  InjuryCount;
    public uint LastAdvancedWorldDay;
    public uint LastReturnWorldDay;      // NEW (R-4) — MEDICAL_NOT_ADVANCED_SENTINEL if never returned
    public InjurySeverity LastSeverity;  // NEW (R-4) — severity of the most recent completed injury
}

public readonly struct MatchLoad
{
    public readonly int AppearanceDays;
    public readonly int HardContacts;
    public readonly int ShortTurnaroundCount;   // NEW (R-3)
    public static MatchLoad None => default;    // identity preserved
}

// R-1: ageYears added; everything else unchanged. Still the ONLY #41 draw site.
public static void AdvanceMedicalDay(ref InjuryState s, int playerId, int ageYears,
                                     in PlayerAttributes a, in InjuryRiskContribution trainingRisk,
                                     in MatchLoad recentMatchLoad, in MedicalModifier medical,
                                     uint worldDay, DeterministicRngService rng);

// ---- #28 -------------------------------------------------------------------

// R-6: the #41 seam value type lives HERE (KD-R4), mirroring TrainingInput. #30 populates it
// from InjuryState's public fields; #28 references nothing new.
public readonly struct InjuryAftermath { /* §3 R-6 */ }

// R-7: position-aware bands.
public static AgeBand ClassifyAgeBand(int ageYears, PlayerPosition pos);

// R-6/R-11: aftermath as a defaulted value input — no interface, no reference to #41.
public static void AdvanceDayForPlayer(ref PlayerRecord rec, ref PlayerLifecycle life, uint worldDay,
                                       in TrainingInput training, in InjuryAftermath aftermath,
                                       bool curveEnabled);

// R-8: decline walks DECLINE_PRIORITY, position bias tie-breaks within a class.
public static void DrainOnePoint(ref PlayerRecord rec, ref PlayerLifecycle life);
```

## 8. Test focus

- **Identity locks (the load-bearing tests).** For every §4 row: with the identity setting, the model is
  **byte-identical** to the pre-supplement behaviour. This is what makes the whole supplement reviewable —
  the #21 `FR-TI-031` / #28 KD-8 precedent.
- **R-1:** two players identical but for age produce different risk; the prime band produces zero delta.
- **R-2/R-3:** `MatchLoadRisk` is non-monotone — an unused player and an overloaded player both exceed the
  mid-band; `ShortTurnaroundCount` raises risk at constant `AppearanceDays` (the E-3 isolation test).
- **R-4:** recurrence fires inside the window and not outside it; `InjuryCount == 0` is exactly zero;
  a save taken mid-window restores field-identical and resumes the same risk.
- **R-5:** `InjurySeverity` ordinals 0–3 are unchanged (the APPEND lock, `EnumOrdinalStabilityTests`
  precedent); the four per-mille slices sum ≤ denominator; `RecoveryDaysForTier[Severe] < RECOVERY_MAX`;
  a `Severe` injury's full countdown completes without clamping.
- **R-6:** `InjuryAftermath.None` is byte-identical to no aftermath; the transient drag expires exactly at
  the window edge; the one-shot PA reduction applies **once** across a replayed day (the idempotency lock);
  an older player takes a larger reduction than a younger one for the same severity.
- **R-7:** a GK and a FW of identical age classify into different bands at the boundary ages; identical
  per-position rows reproduce the current global-scalar behaviour exactly.
- **R-8:** a declining forward loses a physical attribute before a low-bias defensive one (the direct
  inversion lock); **for the same number of drained points, CA falls further under the corrected priority
  than under the current lowest-bias-first order** (the §3 R-8 second-consequence lock — a comparative
  assertion, since "CA falls" alone is already true today and would pass against the defect); position bias
  still decides within a priority class.
- **Cross-cutting:** two-run determinism over a full simulated career; the #30 tick-order one-day-stale
  aftermath contract asserted explicitly rather than assumed (KD-R4b).

## 9. What this supplement deliberately does not do

1. **Model PHV / growth spurts** — needs a #27 height/growth append for a population the generator barely
   produces (R-1).
2. **Model role-granular aging** — needs `RoleId` in a bottom-of-graph data assembly (R-7).
3. **Design form** — recorded with a binding amplitude constraint instead (R-9).
4. **Design congestion→coordination** — recorded with its shape and owner; designing it here would invent a
   consumer (R-10).
5. **Import the contract-year effect** — recorded as a negative result for football (R-12).
6. **Touch #29, #30, #27, #16, or #33** — every finding is containable in #41 and #28.
7. **Fit any `[GT]` magnitude.** Every number above is illustrative pending each spec's balance pass; the
   contract is the *shape*, the *sign*, and the integer discipline. *(Corrected v0.6:)* the engine now
   produces match data — ERR-030-014 was resolved July 26, 2026, and #41's own August 7 balance pass
   fitted its occurrence `[GT]`s against season-scale measurement (`SeasonInjuryRealismTests`) — but
   fitting the values *this supplement* adds waits on KD-W1: the contact stream is pre-tackle until W2
   arms (`TackleContactRadiusM` = 0), and the owner's call is one calibration pass after arming.

## 10. Risks

- **Additive over-weighting of the left tail (R-2).** #29's low-`Condition` term and #41's under-exposure term
  both raise risk for an inactive player. Structurally distinct (KD-R3), but jointly tunable — a balance-pass
  item, flagged rather than dissolved.
- **`[GT]` proliferation.** This supplement adds ~8 new `[GT]` rows across two catalogues, none fitted. Same
  posture as #21's illustrative magnitudes pre-G2, and the same obligation: the balance pass must run before
  any of it is treated as calibrated.
- **The aftermath seam crosses two specs (R-6).** Mitigated by KD-R4 (value type, composition-root read,
  #28 stays sole attribute writer) — no reference either way, so the DAG is unchanged. The residual risk is
  *ordering*, addressed and documented by KD-R4b rather than by a #30 change.
- **PA reduction is deterministic where the evidence is distributional (R-6).** Accepted to preserve #28's
  draw-free contract; the drawn version is recorded with a specific home (#41's existing stream) so it does
  not later arrive as a new #28 stream.
- **The window has closed (KD-R1 — corrected v0.6).** #41 T1 (August 5, 2026) and #28 T1 (August 8, 2026)
  both landed before sign-off, so these are no longer text edits: R-4 and R-6 each cost a coordinated
  format bump with no migration path (§6). The findings remain correct to land; only their price changed.
- **`[GT]` fitting is deferred, not blocked (corrected v0.6 — was "unverifiable, ERR-030-014").**
  ERR-030-014 was resolved July 26, 2026; production matches develop play, and #41's occurrence dial is
  armed at measured, football-band rates. What remains true: every `[GT]` this supplement adds is subject
  to KD-W1 — the tackle challenge feeding the contact stream ships disabled (`TackleContactRadiusM` = 0,
  W2), so a fit landed before the post-W2 calibration capture would be re-fitted immediately. The findings
  are *directional* corrections grounded in external evidence, not fitted parameters — and they are
  correct to land regardless, since a wrong-shaped model cannot be fixed by later fitting.

## 11. Promotion pipeline

1. ✅ **AR to convergence on this supplement** — converged at v0.3 (self-AR pass 2: 0H + 1M + 0L), with
   v0.4 a single stale-citation correction from another pass. See Version History.
2. ✅ **Owner sign-off** on the three structural decisions — **granted August 17, 2026**, all three as
   written: the `Severe` tier + `RECOVERY_MAX` raise (R-5), the #41→#28 aftermath seam (R-6/KD-R4), and
   the deterministic-not-drawn PA reduction (KD-R4a). The reasoning recorded with the decision: the
   supplement is AR-converged and was blocking only alignment work, and if a later measurement changes an
   answer the cost is one supplement edit — cheaper than the standing cost of a live-but-unsigned design
   that the next #41 landing would have to read and guess at.
3. **File the back-props** — ERR-041-013..018 (re-based; re-verified still free August 17, 2026 — they
   appear only in this supplement and in CHANGELOG references to it, never in `spec-error-log.md`) and
   **ERR-028-020..022** (re-allocated v0.6: the originally named 002..004 are all consumed — see the
   header block and Version History; 020..022 is the next free block after ERR-028-019) in
   `spec-error-log.md`, each patching the named
   section files and appending FR-MD-028..034 / FR-PG-025..028.
4. **Patch the section files** — #41 §2.2 / §3.1 / §3.2 / §3.4 / Appendix A; #28 §3.1 / §4.3 / Appendix A.
   No `SPEC_INDEX.md` row changes (both specs stay APPROVED; these are back-props, not re-approvals).
5. **T-phase implementation, in two independent tranches (premises corrected v0.6 — both specs are now
   built and wired, so every tranche lands against live code, not ahead of it).**
   - **#28 R-7 + R-8 land against the live T2a assembly** — `ClassifyAgeBand` / `DrainOnePoint` are still
     pure functions with existing tests, but they now HAVE a production caller: `ProgressionEngine` is
     driven daily at #30's slot 1 (August 8, 2026), so these changes alter live career behaviour in every
     running season, not just T0 test expectations. Cost is the constant tables, the test expectations,
     and the behaviour change itself.
   - **#28 R-6 + R-11's caller now exists** — the original wording ("land with T2, because the aftermath
     parameter needs a caller") is satisfied: `ProgressionEngine` at #30's slot 1 is live (T2a), so these
     land against it directly, plus the `PROGRESSION_SAVE_FORMAT_VERSION` bump for
     `PlayerLifecycle.LastAftermathAppliedDay` (§6).
   - **Every #41-side change lands against the live #41 assembly** — the v0.5 premise ("#41 T0 does not
     yet exist, so R-1..R-5 are spec text until that phase opens") was false at sign-off: T0/T1/T2 landed
     August 5–6, 2026 and the occurrence dial is armed. R-1..R-3 modify the live `MedicalStep` /
     `AssembleRiskScore` chain at slot 4; R-4 carries the `MEDICAL_SAVE_FORMAT_VERSION` bump (§6); R-5
     changes live severity classification. R-6's #41 side is only the §0 read contract plus the two
     `InjuryState` fields R-4
     already adds; the `InjuryAftermath` type itself is #28-owned (KD-R4) and the projection is #30's.

## Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| v0.1 | July 26, 2026 | — | Initial supplement. 12 findings (5H + 5M + 2L) from the research cross-reference: R-1 age term absent, R-2 monotone load, R-3 no recovery-interval input, R-4 recurrence deferred with its input already serialized, R-5 severity model cannot represent an ACL, R-6 no post-injury consequence, R-7 position-blind age bands, R-8 decline order inverted + CA-inefficient, R-9 form (recorded), R-10 congestion→coordination (recorded), R-11 retirement gate, R-12 contract-year (recorded as a negative result). All code claims verified against source: `AbilityModel.ClassifyAgeBand` position-blind; `AbilityModel.DrainOnePoint` lowest-bias-first; `AbilityModel.ComputeCAFromArray` weights by `1 + bias`; `GrowthProjection.DailyPoints` already takes position and discards it; `#41` has no `src/`; `#28` T0 wired into nothing. KD-R1 (cost/timing) … KD-R5 (zero determinism impact). |
| v0.2 | July 26, 2026 | — | **Self-AR pass 1 over the v0.1 draft: 0H + 4M + 2L, all fixed.** **M-1 (dangling anchors):** the doc cited KD-R5b/KD-R9 while defining only KD-R1/R2/R3/R5/R5a/R5b — KD-R4/R6/R7/R8 were never defined, so three citations resolved to nothing (the stale-cross-reference class the root `CLAUDE.md` names as the project's most recurring bug). Renumbered to the contiguous set actually used: KD-R1..R3, KD-R4/R4a/R4b (aftermath), KD-R5 (determinism); every citation re-pointed. **M-2 (zero-value trap in my own proposed surface):** `InjuryAftermath` documented `DaysSinceReturn = -1` as the absent sentinel while `None => default` yields `0`, so the identity value would have read as "returned today" — precisely the `MatchFrameView.Empty` / `MarkingOrientation` defect class. The discriminator is now `LastSeverity` (ordinal 0 = `None`), making `default` provably inert. **M-3 (back-prop ID collision):** ERR-041-002 was assigned to both R-1 and R-6; R-6's signal half moved to ERR-041-007 and the header range corrected 002..006 → 002..007. **M-4 (overstated magnitude claim):** R-8's second consequence claimed decline leaves CA "barely moved" — `ΔCA ∝ −w_i` supports the *sign and shape* (flatter-then-steeper) but not a magnitude, which depends on the unmeasured `PositionAttributeBias` spread; narrowed to the shape claim with the limitation stated, and the §8 test rewritten as a **comparative** lock (corrected order drains more CA per point than the current order) because the original "CA falls measurably" assertion passes against the defect. **L-1:** `RecoveryDaysForTier[Severe]` 300 → 350, since 300 sits below the E-6 ACL mean the tier exists to represent. **L-2:** §11 step 5 claimed all #28 findings need T2 — R-7/R-8 touch pure T0 functions with no production caller and land immediately; split into two tranches. |
| v0.3 | July 26, 2026 | — | **Self-AR pass 2: 0H + 1M + 0L, fixed.** **M-1 (architectural — a reference-direction violation in my own proposal):** v0.2 defined `InjuryAftermath` as **#41-owned** while simultaneously claiming in KD-R4 that "neither assembly gains a reference to the other" — but #28's `AdvanceDayForPlayer` takes `in InjuryAftermath`, so naming that parameter type would have forced `player-progression.asmdef` to reference #41's assembly, which is exactly the coupling KD-R4 exists to prevent. The claim and the surface contradicted each other. Fixed by following the established precedent rather than inventing one: **the consumer owns the seam type** — `TrainingInput` is the #29 seam and lives in `src/player-progression/TrainingInput.cs` (#28 §4.5), so `InjuryAftermath` lives there too, and #30's composition root projects it from `InjuryState`'s public fields. Second-order consequence also fixed: the struct now carries an **integer `SeverityRank`** rather than #41's `InjurySeverity` enum, so no #41 type crosses the boundary at all (and since `InjurySeverity`'s ordinals ascend in severity, the projection is a plain cast that R-5's `Severe` append extends for free). The identity discriminator moved with it (`SeverityRank == 0`), preserving the v0.2 M-2 zero-value fix. §7 surfaces, §6 save table and §11 tranche wording re-pointed. |
| v0.4 | August 8, 2026 | — | **Balance-pass AR pass 9 (M2)**: two lines still named "#41's existing `injuries.occurrence` stream" (KD-R4a's deep-tier routing and §5's determinism-impact bullet) — ERR-041-012 established that stream never existed and may not; both re-anchored to the keyed `DOMAIN_TAG_INJURIES_MEDICAL` derivation. Matters here because this supplement is LIVE (awaiting owner sign-off) and will drive #41's next landing. |
| v0.5 | August 17, 2026 | — | **SIGNED OFF (owner decision).** No design content changed — the three structural decisions (R-5, R-6/KD-R4, KD-R4a) are accepted as written at v0.3/v0.4. Status header and §11 steps 1–2 updated to record it. What this unblocks: §11 step 3 (file ERR-041-013..018 and ERR-028-002..004, the soft-reserved ids re-verified free at the August-7 re-basing — **⚠️ CORRECTED at v0.6: false in its #28 half on its own date.** The August-7 re-basing only ever re-verified the **#41-side** block (013..018); the #28 ids were never re-checked, and ERR-028-002 had already been consumed July 27, 2026 at #53's approval — ten days before that re-basing — with -003/-004 consumed August 8 at the #28 T1/T2a landing. The #28 block is re-allocated to ERR-028-020..022 at v0.6), step 4 (patch #41 §2.2/§3.1/§3.2/§3.4/Appendix A and #28 §3.1/§4.3/Appendix A, appending FR-MD-028..034 / FR-PG-025..028, with **no** `SPEC_INDEX.md` row change — both specs stay APPROVED, these are back-props), and step 5's two tranches. None of that is scheduled here. **Standing caveat, unchanged and load-bearing for step 5:** R-2's under-exposure arm must re-fit against `BaselineDailyRisk` (ERR-041-011) as the exposure-independent term rather than add beside it, and every `[GT]` this supplement moves is subject to KD-W1 — #41's occurrence dial is armed but the tackle challenge that feeds the contact stream ships at `TackleContactRadiusM = 0` (W2), so a fit landed before the W2 calibration capture would be re-fitted immediately. |
| v0.6 | August 17, 2026 | — | **Post-sign-off factual correction (reviewed adversarial-review findings H2 + H3). The sign-off's factual premise was re-verified after the fact and found EXPIRED; the owner's sign-off of the three structural decisions — R-5, R-6/KD-R4, KD-R4a — is UNCHANGED. What changed is the cost/timing argument (KD-R1), which is now VOID.** **H2 (five falsified sites):** §1's implementation-state table claimed #41 had "no `src/` at all" and #28's T1/T2 were unbuilt — false since August 5 and August 8, 2026 respectively (`src/injuries-medical/MedicalSaveCodec.cs` at `MEDICAL_SAVE_FORMAT_VERSION` = 1; `src/player-progression/ProgressionSaveCodec.cs` at `PROGRESSION_SAVE_FORMAT_VERSION` = 1; both mandatory sub-blobs of #30's frame, `SEASON_SAVE_FORMAT_VERSION` = 6; #28's `ProgressionEngine` live at slot 1, #41's `MedicalStep` at slot 4, occurrence dial armed). §1 re-verified and rewritten; §6's "None" save-impact cells for R-4/R-6 replaced with the real 1 → 2 format bumps under KD-7/F3's no-migration refusal; §10's "window is closing" restated as closed; §11 step 5's premises corrected (every tranche now lands against live, wired code); the header's save-impact line corrected; and R-4's "neither costs a format bump" corrected in place. Separately, the three ERR-030-014 live-blocker citations (§3 R-10, §9 item 7, §10) retired — that ERR was **resolved July 26, 2026** (same day this supplement was written), production matches develop play, and what actually gates validation today is KD-W1 (the contact stream is pre-tackle until W2 arms at `TackleContactRadiusM` = 0). **H3 (id collision):** all three #28-side back-prop ids the sign-off unblocked were already taken — ERR-028-002 filed July 27, 2026 at #53's approval (published in #28 §1/§7), ERR-028-003/-004 filed August 8, 2026 at the #28 T1/T2a landing; the v0.5 row's "re-verified free at the August-7 re-basing" claim was false on its own date and is annotated in place (that re-basing only re-verified the #41 side). **Re-allocated by repo-wide grep over `docs/` AND `src/`** (highest in use: ERR-028-019): **old → new mapping -002 → -020 (R-7), -003 → -021 (R-8), -004 → -022 (R-6 consumer + R-11)**; every citation re-pointed (header block, R-6/R-7/R-8/R-11 `Back-prop:` lines, §11 step 3). The ERR-041-013..018 block was re-verified by the same method and **is still free** (cited only by this supplement and CHANGELOG references to it; nothing in `spec-error-log.md`); it stands unchanged. No design content changed; the R-1..R-12 findings' substance is untouched. |
| v0.7 | August 22, 2026 | — | **`ERR-041-021` (adversarial review over the `ERR-041-020` landing, H7): R-1 annotated IN PLACE as landed-in-part; no other finding touched, and no shape changed.** The football-judgment proxy review filed R-1's defect independently and landed its **plumbing half** on August 22 as `ERR-041-020` — the `int ageYears` parameter through `AdvanceMedicalDay`/`AssembleRiskScore`, caller-supplied at #30's composition root, i.e. exactly R-1's own KD-R2 value-not-reference shape — so the defect was momentarily recorded twice, once resolved and once open, with `ERR-041-013` reserved for a finding whose primary half was already spent. **The shapes diverge:** R-1's contract is U-shaped from E-4 (rated *Strong*, whose academy half is explicit — the 16–20 band carries elevated risk at adult match intensity), while the shipped term is monotone linear about pivot 26, giving a 19-year-old −1050 and making 16–20-year-olds the safest players in the league — E-4 reproduced above the pivot and INVERTED below it. `ERR-041-013` is therefore **narrowed, not closed**: its remaining scope is the U-shape / young-tail arm alone, and a refit RE-SHAPES the shipped term rather than adding beside it (the instruction #41 Appendix A already carries for R-2 against `BASELINE_DAILY_RISK`). The term is deliberately **not** re-shaped by this pass — the U-shape is this supplement's design, the supplement is still awaiting owner sign-off, and changing shipped football behaviour is the owner's call. Two forward notes recorded in the R-1 box: the proposed piecewise-constant BAND table would reintroduce the age threshold doctrine P1 forbids and `ERR-028-020` has just removed from the sibling spec, and the shipped term's P5 pivot does not survive an asymmetric U — a refit must re-establish P5 rather than inherit it. |
