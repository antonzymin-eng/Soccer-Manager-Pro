# Staff & Backroom #34 — Section 4: Architecture

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.1 — initial)
**Version:** 0.1
**Status:** APPROVED

---

## 4.1 Assembly & reference direction

New assembly **`TacticalDirector.Staff`** (`src/staff/`, at the T-phase). References **`#41 Injuries&Medical`**
(the `MedicalModifier` type), **`#29 Training`** (the `CoachingModifier` type), and **`#16 DeterministicSim`**
(namespace; the world-tick `DeterministicRngService` only when the deep tier draws). At the **deep tier** it
additionally references **`#33 Personalities`** (`MentoringPlan`, `MoraleOf`/`PersonalityProfile`), **`#31
Transfers`** (`NegotiationOutcome`; the `staffMult` it produces), and **`#40 ClubFinances`** (`ApplyTransaction`
/ `WageBudget`). It references **neither #30 nor #27 nor #28** — the composition root (the season loop, which
owns `SeasonSave`) invokes #34 and threads its projections into the consumers, so the reference is one-way.

```
compositionRoot (season loop) ──► #34 Staff ──► { #41, #29, #16 }   (scaffold)
        │                             │  └────────► { #33, #31, #40 } (deep)
        │                             ▲
        └─ invokes the world-tick     └── #32 (scouting), #42 (academy) reuse the scout-quality projection (deferred)
           slot / HireStaff / threads
           projections into #29/#41
```

Acyclic; no consumer references #34 (each built its own `Identity` default, FR-LW-031). #41/#29/#40/#33/#31
stay **schema-untouched at approval** — #34 constructs their existing identity types and posts through #40's
existing `StaffWage` line.

## 4.2 File layout (proposed, at T-phase)

| File | Contents |
|---|---|
| `StaffAttributes.cs` / `StaffRole.cs` | the distinct staff-skill value types (KD-2) |
| `StaffRecord.cs` | the durable staff entity (stable `StaffId` + mutable `EmployerClubId`, KD-7) |
| `StaffProjections.cs` | `ToMedicalModifier` / `ToCoachingModifier` / `ToStaffMult` / `ToMentoringOverride` / `ToScoutQuality` (KD-3) |
| `StaffState.cs` | per-club role slots + `NextStaffId` allocator; `SeedInitialStaff` (KD-2/KD-5) |
| `StaffOffer.cs` / `StaffHiring.cs` | `StaffOffer` + `EvaluateStaffOffer` + `HireStaff` (the deep command seam, KD-1) |
| `StaffSaveCodec.cs` | `STAFF_SAVE_FORMAT_VERSION` sub-blob encode/decode (KD-4) |
| `StaffConstants.cs` | the Appendix A catalogue |

## 4.3 The projection seam (KD-3)

The projections are authored **generically over each consumer's own identity type**: `ToMedicalModifier`
returns #41's `MedicalModifier`, `ToCoachingModifier` returns #29's `CoachingModifier`, and so on. The
neutral-baseline input yields that type's exact `Identity`, so the composition root can thread the projection
into the consumer in place of the hardcoded `Identity` default with **no behaviour change** (KD-8). At the
scaffold only the two **live** seams are threaded (`MedicalModifier` → #41, `CoachingModifier` → #29); #31's
`staffMult` and #33's `MentoringPlan` projections are proven-identity but consumed at *their own* deep tiers,
so the scaffold does not yet feed them. **#34 builds no #32/#42 interface** (FR-LW-031) — it publishes the
scout-quality projection; the consumers attach when they land.

## 4.4 Save composition (KD-4)

`StaffSaveCodec.Encode(in StaffState) → byte[]` produces the opaque sub-blob; the composition root appends it
to #30's `SeasonSaveCodec` frame as an additional opaque sub-blob (the **#41 `MEDICAL_SAVE_FORMAT_VERSION` /
#33 `HUMAN_SYSTEMS_SAVE_FORMAT_VERSION` precedent**, both "No `WORLD_STORE_FORMAT_VERSION` bump"), and the
outer `SEASON_SAVE_FORMAT_VERSION` bump is coordinated with #30 at T1 (**exact version TBD** — assigned by
whichever T-phase lands first, not hardcoded here). The codec mirrors the `SeasonSaveCodec` fail-loud posture
exactly: version-gate first (`STAFF_SAVE_FORMAT_VERSION`, F3), an overflow-safe `Require(offset, need, total)`
bound against **`total − offset`** on every length-prefixed read, and a trailing-byte guard. The block is
**opaque to `SeasonSaveCodec`** (never parsed) and carries its own inner version gate — the world/season/match
blobs stay byte-untouched. Layout in Appendix B. **No `RngCursor`** is serialized (draw-free scaffold; deep
draws are keyed, no cursor).

## 4.5 Interface contracts recorded for the composition root & #30

- **The composition root** (season loop) MUST: invoke #34's world-tick step at #30's new tick-order slot
  (null at the scaffold); **thread #34's projections into #29/#41** (and, deep, #33/#31) when building their
  daily inputs; route `HireStaff`/staff commands from the UI to #34; and post staff wages via #40's
  `ApplyTransaction` (deep). It MUST NOT let the UI mutate #34 state directly. It MUST call `SeedInitialStaff`
  (§3.3) **only at new-career genesis** and reconstruct `StaffState` from the sub-blob on **load** — never
  both (re-seeding a loaded career would destroy restored staff).
- **#30** MUST, at the T-phase: (a) add the **staff tick-order null-seam slot** (ERR-030-006, at approval —
  §8); (b) bump `SEASON_SAVE_FORMAT_VERSION` (exact version coordinated at T1) composing the sub-blob. **#30
  grows no roster-commit for staff** (KD-7 — staff never re-key; they are not in #27's `Squad`).
- **#41 / #29** are consumed by #34 constructing their existing identity types; **#40** is consumed read-only
  (`WageBudget` / `WageBillAggregate`) + through `ApplyTransaction` (`StaffWage`, deep). #34 adds nothing to
  #41/#29/#40 at approval; the `CoachingModifier` field shape (ERR-029-002) and the FR-FN-015 relax (ERR-040)
  are deferred deep back-props.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial §4 (assembly/reference direction, file layout, the projection seam, save composition, root/#30/#41/#29/#40 interface contracts), promoted from design supplement v0.4. Status IN REVIEW. |
#endregion
