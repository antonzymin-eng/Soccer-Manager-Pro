# Staff & Backroom #34 — Section 2: Requirements, Data Structures, Failure Modes

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.2 — section-file AR PASS-1; prior v0.1 initial)
**Version:** 0.2
**Status:** APPROVED

---

## 2.1 Functional requirements (FR-ST-001..024)

| ID | Requirement | Level | KD |
|---|---|---|---|
| FR-ST-001 | Every staff-quality projection MUST be a **pure deterministic integer function** of `StaffAttributes` (no RNG); a **neutral-baseline** staff MUST project to the consumer's **exact `Identity`**. | MUST | KD-3/KD-5 |
| FR-ST-002 | A projection MUST **return the consuming spec's own pre-existing identity type** (`MedicalModifier` #41 / `CoachingModifier` #29 / `staffMult` int #31 / `MentoringPlan` #33); #34 MUST NOT define a new multiplier convention. | MUST | KD-3 |
| FR-ST-003 | The per-club staff store MUST be a set of **role slots** (one `StaffRecord` per `StaffRole` — HeadCoach/HeadPhysio/ChiefScout, 1:1 with the enum); each consumer MUST read its **assigned role-slot-holder** (HeadCoach → `CoachingModifier`; HeadPhysio → `MedicalModifier`; ChiefScout → scout-quality **and** `staffMult`), a single deterministic slot read — never an unspecified aggregate. | MUST | KD-2/KD-3 |
| FR-ST-004 | The neutral baseline MUST be a **real** neutral-baseline house-staff `StaffRecord` (`NeutralHouseStaff`), **not** an absence sentinel; consumers MUST NOT special-case staff absence (they consume a modifier that equals `Identity`). | MUST | KD-5 |
| FR-ST-005 | `default(StaffRecord)` / `default(StaffAttributes)` (all-zero — attributes `∉ [1,20]`) MUST fail loud at the consuming seam (F4); the all-zero attribute set is the zero-value-trap discriminator (the #41 `default(MedicalModifier)` precedent). | MUST | KD-5/F4 |
| FR-ST-006 | `StaffAttributes` MUST be a **distinct staff-skill vocabulary** of `int [1,20]` fields (`Create()` = all `STAFF_ATTR_NEUTRAL = 10`), **NOT** #27's `PlayerAttributes`; #34 MUST NOT extend or reference #27's attribute schema. | MUST | KD-2 |
| FR-ST-007 | Each `StaffRecord` MUST carry a **stable `StaffId`** allocated from a **serialized monotonic `StaffState.NextStaffId`** high-water counter (never reused — the #22 `episodeId` discipline); a move MUST NOT change it. | MUST | KD-7 |
| FR-ST-008 | A hire MUST change the moved staff's **mutable `EmployerClubId`** within #34's own store; it MUST NOT re-key `StaffId`, MUST NOT request a #30 roster-commit, and MUST NOT dispatch a cross-system migration hook (nothing outside #34 keys by `StaffId`). | MUST | KD-7 |
| FR-ST-009 | The scaffold tier MUST register **no** RNG stream; `_RESERVED_0x26_` / `SubsystemOrdinals.Staff = 88` MUST remain RESERVED (not promoted). | MUST | KD-4 |
| FR-ST-010 | #34 state MUST persist as an opaque, independently version-gated `STAFF_SAVE_FORMAT_VERSION` sub-blob composed into #30's `SeasonSaveCodec`; the codec MUST NOT parse it; #34 MUST NOT bump `WORLD_STORE_FORMAT_VERSION`. | MUST | KD-4 |
| FR-ST-011 | At the scaffold, `StaffState` MUST track the **managed club's** staff only; AI clubs MUST be **unstaffed** (the consumers' built-in `Identity` default applies, byte-neutral); all-clubs staff modelling is deep. | MUST | KD-4/KD-8 |
| FR-ST-012 | The neutral-baseline roster MUST be seeded **only at new-career genesis**; a load MUST reconstruct `StaffState` from the sub-blob and MUST NOT re-seed (re-seeding would overwrite hired/aged staff). | MUST | KD-4 |
| FR-ST-013 | Staff records MUST survive `RollToNextSeason` (durable career state); staff **aging/retirement** is deep-tier (the scaffold's staff are static neutral). | MUST | KD-4 |
| FR-ST-014 | A season with a **neutral-baseline** staff roster MUST advance **byte-identical** to pre-#34 (identity projections; no stream registered; no `StaffWage` posted). | MUST | KD-8 |
| FR-ST-015 | #34 MUST supply only the **modifier** each consumer reads and MUST NOT add a second training/injury/mentoring/valuation path (the #29 FR-TR-016 / #41 FR-MD-016 single-path contract). | MUST | KD-3 |
| FR-ST-016 | #34 MUST NOT write `ClubFinances` fields or maintain a parallel wage total (FR-FN-015); the scaffold MUST post **no** `StaffWage`, so #40's `WageBillAggregate ≡ 0` at Stage 2 is preserved verbatim. | MUST | KD-6 |
| FR-ST-017 | *(deep)* A staff wage MUST be posted via #40's `ApplyTransaction` (`{Debit, StaffWage, wage}`, moving `WageBillAggregate` only, FR-FN-016), gated by `WageBillAggregate + wage ≤ WageBudget` (both **read from #40**); #34 MUST keep **no** wage counter of its own. | MUST | KD-6 |
| FR-ST-018 | *(deep)* Hiring MUST reuse #31's `NegotiationOutcome` enum + the validate-all-first atomic-commit pattern via a **thin staff-specific** `StaffOffer`/`EvaluateStaffOffer` (accept iff `offeredWage ≥ wageDemand`); hiring is **year-round** (no window). Every gate MUST pass before any mutation (F2). | MUST | KD-1 |
| FR-ST-019 | *(deep)* Candidate-pool generation MUST be the **first draw site**, promoting `DOMAIN_TAG_STAFF = 0x26` / ordinal 88 (spec-text-first); draws MUST be **position-independent keyed** on `(clubId, worldDay, purpose)`; no free-running cursor MUST be serialized. | MUST | KD-4 |
| FR-ST-020 | *(deep)* The `CoachingModifier` field shape + #29's consumption of it MUST land as a **#29 back-prop (ERR-029-002)** when #34 first produces a non-identity `CoachingModifier`; at the scaffold `ToCoachingModifier` MUST return `CoachingModifier.Identity` (the existing `default`), leaving #29 untouched. | MUST | KD-3 |
| FR-ST-021 | #34 MUST publish a **scout-quality projection** #32 will consume (scouts are staff) and MUST build no #32/#42 interface (FR-LW-031); #34's own deferred consumers default to identity seams. | MUST | KD-3 |
| FR-ST-022 | Every `StaffAttributes` / projection / wage field MUST be integer (attributes `[1,20]`, projections per-mille `int`, wages `long`); #34 MUST introduce **no** float. | MUST | KD-4 |
| FR-ST-023 | The #30 staff tick-order slot MUST be a **documented null seam at the scaffold** (declared reserve-ahead, ERR-030-006); #34's own daily tick work (candidate-pool aging, in-flight negotiation) is **deep** only. | MUST | KD-8 |
| FR-ST-024 | A minimal/deep hire MUST be initiated **only** by an explicit manager command (`HireStaff`, the `SetTeamTactic` discipline); the UI MUST drive it through the command seam and MUST NOT mutate #34 state directly. | MUST | KD-8 |

## 2.2 Data structures

```csharp
// KD-2 — the staff data layer (a DISTINCT staff-skill vocabulary; NOT #27's 31 player attrs). Integer [1,20].
// Create() = all STAFF_ATTR_NEUTRAL (10); default(StaffAttributes) (all-zero, ∉ [1,20]) is INVALID (F4).
public enum StaffRole : byte { Coach = 0, Scout = 1, Physio = 2 }   // ordinal-stable; deep may extend
public readonly struct StaffAttributes
{ public int Coaching, Fitness, Medical, ScoutJudgement, Motivating, Discipline, TacticalKnowledge; /* deep may append */ }

// Durable staff entity (serialized, KD-4). StaffId is STABLE (never re-keys, KD-7); EmployerClubId is mutable.
public struct StaffRecord
{
    public int  StaffId;            // #34-owned, STABLE (from StaffState.NextStaffId, KD-7)
    public string FirstName, LastName;
    public int  Age;                // deep: aging/retirement (FR-ST-013)
    public StaffRole Role;
    public int  EmployerClubId;     // MUTABLE — a hire changes THIS, not StaffId; -1 = unemployed (deep candidate)
    public StaffAttributes Attributes;
    // deep (behind deepStaffEnabled): a staff Contract { long WagePerPeriod; int LengthSeasons; } APPENDS here.
}

// KD-1 (deep) — the thin staff-hiring offer. The negotiated quantity is a WAGE (not a fee). Reuses #31's
// NegotiationOutcome enum { Rejected=0, Accepted=1, CounterOffered=2 /*deep*/ } (referenced, not re-declared).
public readonly struct StaffOffer { public int StaffId; public long WagePerPeriod; public int LengthSeasons; }

// Per-club staff store (serialized). ROLE SLOTS (FR-ST-003); managed-club-only at the scaffold (FR-ST-011).
// NextStaffId is the stable-id high-water allocator (FR-ST-007). Deep: candidate pool + in-flight negotiations.
public sealed class StaffState { /* role slots -> StaffRecord; int NextStaffId; /* deep: pool + in-flight */ }
```

## 2.3 Failure modes

| ID | Condition | Handling |
|---|---|---|
| **F1** | *(deep)* A hire with `WageBillAggregate + wage > WageBudget` (both read from #40) | **Fail loud** — over-ceiling wage spend is a caller/UI-contract bug, never silently clamped (FR-ST-017). |
| **F2** | *(deep)* Any hiring commit gate fails after a partial mutation would occur | **Fail loud + no mutation** — validate-all-before-write; the hire commit is atomic (FR-ST-018). |
| **F3** | Staff sub-blob: bad `STAFF_SAVE_FORMAT_VERSION` / out-of-bounds length prefix / trailing bytes | **Fail loud** — the `SeasonSaveCodec` posture; no cross-version migration at Stage 0 (FR-ST-010). |
| **F4** | `default(StaffRecord)` / `default(StaffAttributes)` (all-zero attributes, `∉ [1,20]`) reaching a consuming seam | **Fail loud** — the zero-value-trap default record caught at the projection/insertion seam (the #41 `default(MedicalModifier)` F4 precedent). `StaffId = 0` alone is **not** the trap (it is a real first-allocated id); the all-zero **attributes** are the discriminator (FR-ST-005). |
| **F5** | A role-slot assignment to an undefined role / a projection read of an empty slot when the neutral baseline was not seeded | **Fail loud** — every slot MUST hold a real `StaffRecord` (neutral baseline at minimum, KD-5); an empty slot is a seeding-contract bug. |
| **F6** | *(deep)* A `HireStaff` for a `StaffId` absent from the candidate pool, or a malformed `StaffOffer` (negative wage, `LengthSeasons ≤ 0`) | **Fail loud** — identity/magnitude validity is a caller-contract bug (the #31 F6 / #27 `SquadFileLoader` precedent). |

**Zero-value-trap discipline (KD-5/F4):** `StaffAttributes` has no all-zero neutral (`Create()` seeds all
`10`), so `default(StaffAttributes)` (all-zero, `∉ [1,20]`) is invalid by design and fails the projection
gate; `StaffRole` defaults `Coach` (0) but every real `StaffRecord` is constructed with an explicit role, so
the default is never routed unchecked. `EmployerClubId` default `0` is a real club id — the discriminator is
the attribute range, not the employer/id fields.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial §2 (FR-ST-001..024, data structures, F1..F6), promoted from design supplement v0.4. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | Section-file AR PASS-1 (M): FR-ST-003 reconciled the role-slot set to **3 slots 1:1 with `StaffRole`** (HeadCoach/HeadPhysio/ChiefScout; the ChiefScout drives both scout-quality and `staffMult`) — the "head of recruitment" 4th slot had no `StaffRole`. |
#endregion
