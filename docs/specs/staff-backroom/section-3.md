# Staff & Backroom #34 — Section 3: Algorithms

**Created:** July 23, 2026
**Last Updated:** July 27, 2026 (v0.3 — back-prop landed atomically with the ten-spec approval wave; see the version-history row)
**Last Updated (prior):** July 23, 2026 (v0.2 — section-file AR PASS-1; prior v0.1 initial)
**Version:** 0.3
**Status:** APPROVED

---

All arithmetic is **integer** (staff attributes `[1,20]`; projections per-mille `int`; wages `long`). No
stochastic draw occurs at the scaffold tier (FR-ST-001/009). `PERMILLE_DENOM = 1000`;
`STAFF_ATTR_NEUTRAL = 10`; `STAFF_MODIFIER_IDENTITY_PERMILLE = 1000`.

## 3.1 Staff-quality projections (FR-ST-001/002/003)

Each projection is a **pure deterministic integer function** of the assigned role-slot-holder's
`StaffAttributes`, returning the **consuming spec's own identity type** so that a neutral-baseline staff
(all attributes `= 10`) yields **exactly** that type's `Identity` (KD-3/KD-5). The general per-mille shape,
for a facet attribute `a ∈ [1,20]` with neutral pivot `10`:

```
FacetPermille(a):                                          # neutral (a = 10) => 1000 (identity); [GT] slope
    return PERMILLE_DENOM + (a - STAFF_ATTR_NEUTRAL) * STAFF_FACET_SLOPE_PERMILLE   # integer; monotone in a
```

- **`ToMedicalModifier(in StaffRecord physio) → MedicalModifier`** (#41's type): maps the physio's `Medical`
  / `Fitness` to `MedicalModifier(OccurrenceRiskMillMult, RecoverySpeedMillMult)`. Neutral ⇒
  `MedicalModifier.Identity` (1000/1000). A better physio **lowers** occurrence risk and **raises** recovery
  speed (the [GT] slope signs are pinned so the direction is fixed; magnitudes are balance-pass-illustrative).
- **`ToCoachingModifier(in StaffRecord coach) → CoachingModifier`** (#29's type): maps `Coaching` /
  `TacticalKnowledge`. **#29's `CoachingModifier` is a bare `default` today** (no per-mille fields), so at the
  scaffold `ToCoachingModifier` returns `CoachingModifier.Identity` (= the existing `default`); the field
  shape + #29's consumption is a **deep #29 back-prop (ERR-029-002)** landing when #34 first produces a
  non-identity coaching modifier (FR-ST-020).
- **`ToStaffMult(in StaffRecord chiefScout) → int`** (#31's `staffMult`, per-mille): neutral ⇒
  `TRANSFERS_STAFF_MULT_IDENTITY = 1000`. Consumed at #31's deep tier. Read from the **ChiefScout** slot (the
  same slot `ToScoutQuality` reads — recruitment influence and scouting quality both come from the scout at
  Stage 3; a dedicated director-of-football slot is a deep extension).
- **`ToMentoringOverride(in StaffRecord coach, /* squad ctx */) → MentoringPlan`** (#33's type): neutral ⇒
  `MentoringPlan.None`. Read from the HeadCoach slot; consumed at #33's deep tier.
- **`ToScoutQuality(in StaffRecord scout) → int`** (the #32-facing projection): neutral ⇒ a baseline #32 will
  define. Read from the ChiefScout slot; deferred consumer (#32 does not exist yet).

**Sole-path discipline (FR-ST-015):** each projection is the **only** staff route into its consumer's
modifier; #34 adds no second `MedicalModifier`/`CoachingModifier` source, so staff modulation never
double-counts with #33 morale or **#53 facilities** (which reach those consumers by their own separate
seams). *(ERR-034-001, at #53's approval — re-attributed from #40, which funds facilities rather than
owning their level.)*

## 3.2 The neutral baseline (FR-ST-004/005)

```
NeutralHouseStaff(staffId, role, clubId) → StaffRecord:    # a REAL entity, not an absence sentinel (KD-5)
    return StaffRecord{ StaffId = staffId, Role = role, EmployerClubId = clubId,
                        Age = HOUSE_STAFF_AGE, Attributes = StaffAttributes.Create() /* all 10 */,
                        FirstName = ..., LastName = ... }
```

`NeutralHouseStaff` projects to each consumer's exact `Identity` (§3.1), so a scaffold season is
byte-identical to pre-#34 (FR-ST-014). A `default(StaffRecord)` (all-zero attributes) is **invalid** and
fails loud at the projection seam (F4) — the identity is the explicit neutral-`10` factory, never a zero
sentinel.

## 3.3 Initial staff population (career start) (FR-ST-012)

At **new-career genesis** the managed club's **role slots** are seeded with one `NeutralHouseStaff` each, so
every role a consumer reads is filled:

```
SeedInitialStaff(managerClubId, ref StaffState s):         # new-career genesis ONLY (never on load, KD-4)
    for each roleSlot in ROLE_SLOTS:                       # HeadCoach, HeadPhysio, ChiefScout (1:1 with StaffRole)
        id := s.AllocateStaffId()                          # NextStaffId high-water; never reused (FR-ST-007)
        s.AssignSlot(roleSlot, NeutralHouseStaff(id, RoleOf(roleSlot), managerClubId))
```

Seeding mutates only `StaffState` (the managed club, FR-ST-011) and is **not read by the sim except via the
identity projections** (which return `Identity`), so it does not perturb the byte-identical season advance
(FR-ST-014). **Seeding runs once, at new-career genesis ONLY.** A load-from-save reconstructs `StaffState`
from the sub-blob (§4, F3-gated) and **MUST NOT re-seed** — re-seeding a loaded career would collide with the
present ids (`AllocateStaffId`/slot-assign throws) or overwrite hired/aged staff, silently destroying career
progress. The composition root invokes `SeedInitialStaff` at career creation **and** the sub-blob decode on
load — **never both** (§4.5).

## 3.4 Hiring (deep) — `HireStaff` (FR-ST-018, atomic)

Invoked by a manager command (never autonomously). Hiring is **year-round** (no window). **Validate every gate
before any mutation** (F2 — no half-written hire):

```
EvaluateStaffOffer(in StaffOffer o, long wageDemand):      # draw-free predicate; the candidate accepts a
    return o.WagePerPeriod >= wageDemand ? Accepted : Rejected    #   sufficient wage (NOT a fee — KD-1)

HireStaff(managerClubId, in StaffOffer o, ref ClubFinances finances, ref StaffState s):
    # ---- VALIDATE-ALL-FIRST (no mutation) ----
    require CandidateInPool(o.StaffId)                                       else throw   # F6
    require o well-formed (WagePerPeriod >= 0, LengthSeasons > 0)            else throw   # F6
    demand := WageDemandOf(o.StaffId)                                        # the candidate's deterministic ask
    if EvaluateStaffOffer(o, demand) != Accepted:  return Rejected                        # no mutation, not a failure
    require WageBillAggregate(finances) + o.WagePerPeriod <= WageBudget(finances)  else throw   # F1 (both read from #40; NO #34 counter)
    require IsDefinedRole(RoleOf(o.StaffId))                                 else throw   # F5 (the target role slot must exist)
    # ---- COMMIT (atomic; all gates passed). A hire REPLACES the role-slot occupant — every slot is ALWAYS filled (KD-5). ----
    ApplyTransaction(ref finances, {Debit, StaffWage, o.WagePerPeriod})      # WageBillAggregate += (FR-FN-016)
    displaced := s.SlotHolder(RoleOf(o.StaffId))                            # the prior occupant (neutral house staff or a previous hire)
    moved := s.CandidateToEmployed(o.StaffId, managerClubId, o)             # EmployerClubId := managerClubId; StaffId UNCHANGED (KD-7)
    s.ReplaceSlot(RoleOf(o.StaffId), moved)                                  # REPLACE the occupant; displaced.EmployerClubId := -1 (unemployed)
    return Accepted
```

Because every gate cleared first (including the defined-role check, F5, and the affordability check against
#40's running `WageBillAggregate`, F1), no individual commit step can fail mid-way (`ApplyTransaction`
magnitudes are pre-validated; a `Debit` cannot fail on a pre-checked ceiling), so the club is never charged a
wage for a staff member it does not employ. **A hire REPLACES the role-slot occupant** — every slot is always
filled (the neutral baseline or a prior hire, KD-5), so there is **no "free slot" requirement**; the displaced
staff becomes unemployed (`EmployerClubId := -1`). **The staff `StaffId` never re-keys** — only
`EmployerClubId` changes — so **no #30 roster-commit and no cross-system migration hook fire** (KD-7). The wage gate reads
#40's `WageBillAggregate` directly; **#34 keeps no wage counter** (a `committedStaffWage` accumulator would be
the parallel wage total FR-FN-015 forbids, KD-6).

## 3.5 The #40 wage boundary (deep) (FR-ST-016/017)

The **scaffold posts no `StaffWage`** (no hiring), so #40's `WageBillAggregate ≡ 0` at Stage 2 (FR-FN-015)
holds verbatim with **no #40 back-prop at approval**. The deep tier is #34's `StaffWage` producer: a hire
posts `{Debit, StaffWage, wage}` (moving `WageBillAggregate` only, FR-FN-016) and a departure/expiry posts
`{Credit, StaffWage, wage}`; both go through #40's single `ApplyTransaction` path. The affordability truth is
#40's running `WageBillAggregate` (which #40 maintains), gated against `WageBudget` — **not** a #34 counter.
This lands with the **shared deferred ERR-040** #40 back-prop (relaxing FR-FN-015 for the wage producers +
wiring the `WageBudget` gate), the same relaxation #31 defers for `PlayerWage`.

## 3.6 Worked example (behaviour-neutral scaffold)

New career, managed club seeded with neutral house staff. `RunWorldTickInFixedOrder` reaches the new staff
slot, which — at the scaffold — has no daily work (candidate-pool/hiring are deep), so it is a null seam. The
composition root, building #29's and #41's daily inputs, calls `ToCoachingModifier(headCoach)` and
`ToMedicalModifier(headPhysio)`; both return `Identity` (all-neutral staff), so #29/#41 tick byte-identical to
pre-#34. No `HireStaff` is issued ⇒ no `ApplyTransaction`, no `StaffWage`, no draw ⇒ the season is
byte-identical to pre-#34 (FR-ST-014). A save→restore here is field-identical (FR-ST-012). When the manager
*does* `HireStaff` a real coach (deep), one `{Debit, StaffWage, wage}` post + one role-slot assignment land
deterministically and atomically, and thereafter `ToCoachingModifier(headCoach)` yields a **non-identity**
`CoachingModifier` — the identity the deep tier modulates.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial §3 (projections, neutral baseline + seeding, hiring, the #40 wage boundary, worked example), promoted from design supplement v0.4. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | Section-file AR PASS-1 (M): §3.1 `ToStaffMult` reads the **ChiefScout** slot (was a phantom "head of recruitment"); §3.4 `HireStaff` uses **replace semantics** — a hire replaces the always-filled role-slot occupant (displaced → unemployed), F5 is now a **defined-role** check (the old `DestinationRoleSlotFree` gate would always fail, since every slot is seeded). §3.3 seeding comment aligned to the 3 slots. |
| 0.3 | 2026-07-27 | — | **ERR-034-001** (at #53's approval): same re-attribution at the §3 seam-composition note — *"#40 facilities"* → **#53 facilities**. |
#endregion
