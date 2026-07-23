# Staff & Backroom #34 — Section 5: Test Plan

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.2 — section-file AR PASS-2; prior v0.1 initial)
**Version:** 0.2
**Status:** APPROVED

---

## 5.1 Unit — projections (KD-3/KD-5)

- **T-ST-PROJ-001** — every projection is a **pure integer function** of `StaffAttributes` (no RNG parameter,
  no #33/#28 read); two calls with equal inputs return equal outputs (reflection/static assertion).
- **T-ST-PROJ-002 (identity)** — a **neutral-baseline** staff (all attributes `= 10`) projects to each
  consumer's **exact `Identity`**: `ToMedicalModifier → MedicalModifier.Identity` (1000/1000),
  `ToCoachingModifier → CoachingModifier.Identity`, `ToStaffMult → 1000`, `ToMentoringOverride →
  MentoringPlan.None`.
- **T-ST-PROJ-003 (divergence + direction)** — a **distinct (non-neutral)** staff produces a **deterministic
  non-identity** modifier whose direction is fixed (a better physio lowers `OccurrenceRiskMillMult` and raises
  `RecoverySpeedMillMult`; magnitudes are balance-pass-illustrative); the same input always yields the same
  output.
- **T-ST-PROJ-004 (sole path)** — #34 adds **no second** `MedicalModifier`/`CoachingModifier` source
  (static/reflection assertion — the FR-ST-015 no-double-count lock).
- **T-ST-PROJ-005 (role-slot read)** — each projection reads its **assigned role-slot-holder** (head coach →
  `CoachingModifier`, head physio → `MedicalModifier`), a single slot read (FR-ST-003).

## 5.2 Unit — data layer (KD-2/KD-5)

- **T-ST-DATA-001** — `StaffAttributes.Create()` = all `STAFF_ATTR_NEUTRAL = 10`; `StaffRole` ordinal
  stability (`Coach=0, Scout=1, Physio=2`).
- **T-ST-DATA-002 (F4)** — `default(StaffRecord)` / `default(StaffAttributes)` (all-zero attributes,
  `∉ [1,20]`) **fails loud** at the projection/insertion seam; `StaffId = 0` alone does **not** fail (a real
  first-allocated id) — the all-zero **attributes** are the discriminator.
- **T-ST-DATA-003 (stable id)** — a hire changes `EmployerClubId` and leaves `StaffId` **unchanged** (no
  re-key); `StaffState.NextStaffId` is monotonic and never reused across genesis-seeded + deep candidate ids
  (FR-ST-007).

## 5.3 Behaviour-neutral identity (KD-8) — the headline

- **T-ST-NEU-001** — a season with a **neutral-baseline** staff roster advances **byte-identical** to pre-#34:
  the composition root threads `MedicalModifier.Identity` → #41 and `CoachingModifier.Identity` → #29, so
  #41/#29 tick identically; **no** RNG stream registered (every existing cursor byte-identical — the #40
  `T-FN-NEU-003` class); **no** `StaffWage` posted (`WageBillAggregate` unchanged, FR-FN-015 preserved — the
  #31 `T-TX-BID-006` analogue).
- **T-ST-NEU-002** — AI clubs are **unstaffed** at the scaffold (the consumers' built-in `Identity` default
  applies); `StaffState` tracks the **managed club** only (FR-ST-011).

## 5.4 Save round-trip & determinism (KD-4)

- **T-ST-DET-001** — `StaffState` (role slots + `StaffRecord`s + `NextStaffId`) restores **field-identical**
  across a save; the restored roster comes from the **sub-blob decode** and the load path does **not** re-run
  `SeedInitialStaff` (seeding is genesis-only — §3.3; a re-seed on load would overwrite/collide).
- **T-ST-DET-002** — staff survive a `RollToNextSeason` boundary (durable career state, FR-ST-013).
- **T-ST-DET-003 (deep)** — two-run determinism: a full season's hiring activity + candidate-pool generation
  from a fixed world seed produces a **byte-identical** `StaffState` (keyed draws, no cursor).
- **T-ST-SHAPE-001 (draw-free)** — the serialized staff block contains **no** `RngCursor`/`actionOrdinal`
  field (schema-shape assertion, FR-ST-009).
- **T-ST-INT-001 (integer posture)** — every `StaffAttributes`/projection/wage field is integer; #34
  introduces **no** float (static/reflection assertion).

## 5.5 Hiring & the #40 boundary (deep, KD-1/KD-6)

- **T-ST-HIRE-001** — `EvaluateStaffOffer` accepts iff `offeredWage ≥ wageDemand` (draw-free predicate); the
  boundary (`offeredWage == wageDemand`) accepts; reuses `NegotiationOutcome`.
- **T-ST-HIRE-002 (F1)** — a hire with `WageBillAggregate + wage > WageBudget` (both read from #40) **fails
  loud**; #34 holds **no** wage counter (static/reflection assertion on the #40 boundary).
- **T-ST-HIRE-003 (F2 atomicity)** — an undefined-role (F5) or failed-affordability (F1, `WageBillAggregate +
  wage > WageBudget`) gate leaves `ClubFinances` **and** `StaffState` **untouched** — no `ApplyTransaction`
  fired, no slot replaced (a hire **replaces** the occupant, §3.4, so there is no "full slot" failure).
- **T-ST-HIRE-004** — an accepted hire posts exactly `{Debit, StaffWage, wage}` (moving `WageBillAggregate`
  only, FR-FN-016), changes `EmployerClubId`, leaves `StaffId` unchanged, and requests **no** #30
  roster-commit / dispatches **no** migration hook (FR-ST-008).
- **T-ST-HIRE-005 (year-round)** — a `HireStaff` succeeds on any world day (no window gate — FR-ST-018).

## 5.6 Fail-loud (F1..F6)

- **T-ST-FAIL-001 (F3)** — bad `STAFF_SAVE_FORMAT_VERSION` / out-of-bounds length prefix (the overflow-safe
  `total − offset` `Require`) / trailing bytes all throw at decode.
- **T-ST-FAIL-002 (F4)** — a `default(StaffRecord)` (all-zero attributes) reaching a projection throws.
- **T-ST-FAIL-003 (F5)** — a projection read of an unseeded (empty) role slot, or an assignment to an
  undefined role, throws.
- **T-ST-FAIL-004 (F6, deep)** — a `HireStaff` for a `StaffId` absent from the candidate pool, a negative
  wage, or `LengthSeasons ≤ 0` throws at the consuming seam.

## 5.7 Requirement traceability

Every FR-ST-001..024 maps to a T-ST-* test above **or** a recorded §7 deferral. Deep-tier-only requirements
(FR-ST-017/018/019/020) are locked at their scaffold identity boundary now (the `deepStaffEnabled`-off
equality — neutral projections, no draw, no wage) and fully at the deep T-phase.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial §5 (projection/data units, behaviour-neutral identity, save/determinism, hiring + #40 boundary, fail-loud, traceability), promoted from design supplement v0.4. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | Section-file AR PASS-2 (M, regression from PASS-1's replace-semantics fix): T-ST-HIRE-003 dropped the stale "full role slot (F5)" trigger (a hire replaces the occupant — there is no full-slot failure) → an undefined-role (F5) / failed-affordability (F1) gate. |
#endregion
