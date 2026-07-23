# Staff & Backroom #34 — Appendices

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.2 — section-file AR PASS-1; prior v0.1 initial)
**Version:** 0.2
**Status:** APPROVED

---

## Appendix A — Constant catalogue

| Constant | Tag | Value | Notes |
|---|---|---|---|
| `STAFF_SAVE_FORMAT_VERSION` | `[FIXED]` | 1 | the staff sub-blob's own version gate (KD-4). |
| `PERMILLE_DENOM` | `[FIXED]` | 1000 | integer per-mille denominator (the #41/#40 posture). |
| `STAFF_ATTR_NEUTRAL` | `[FIXED]` | 10 | the neutral staff-attribute value + projection pivot (`Create()` seeds all fields to it; the #33 `TRAIT_NEUTRAL` precedent). |
| `STAFF_MODIFIER_IDENTITY_PERMILLE` | `[FIXED]` | 1000 | the projection identity (`×1.0`); a neutral staff (`a = 10`) projects to this on every facet. |
| `STAFF_FACET_SLOPE_PERMILLE` | `[GT]` | illustrative | per-attribute-point per-mille slope of a projection (§3.1); direction pinned (better staff → favourable modifier), magnitude balance-pass-pinned (#21 G2). |
| `ROLE_SLOTS` | `[FIXED]` | {HeadCoach, HeadPhysio, ChiefScout} — one per `StaffRole` (Coach/Physio/Scout) | the per-club role slots each consumer reads (FR-ST-003), **1:1 with `StaffRole`**; the **ChiefScout** drives both `ToScoutQuality` (#32) and `ToStaffMult` (#31). A dedicated director-of-football slot is a deep extension (append-only, ordinal-stable). |
| `HOUSE_STAFF_AGE` | `[GT]` | illustrative | the neutral house-staff age (§3.2); balance-pass-pinned. |
| `DEFAULT_STAFF_WAGE_*` | `[GT]` | illustrative | *(deep)* the candidate wage-demand `[GT]` function of a staff record (§3.4); `≥ 0` (F6-valid); balance-pass-pinned. |
| `TRANSFERS_STAFF_MULT_IDENTITY` | `[CROSS]` | #31 = 1000 | the `staffMult` identity #34 produces (default until real staff); sourced from #31 `appendices.md` (FR-TX-011). |
| `MEDICAL_MODIFIER_IDENTITY_PERMILLE` | `[CROSS]` | #41 = 1000 | the `MedicalModifier` identity the neutral projection returns; sourced from #41 (FR-MD-016). |
| `_RESERVED_0x26_` / `SubsystemOrdinals.Staff = 88` | `[CROSS]` | #16 | reserved for #34; stays RESERVED at approval (draw-free scaffold, KD-4); promotes at the deep first draw. |

**Tag note:** the `[GT]` projection/wage magnitudes are **illustrative pending a Stage-3 balance pass** — the
reviewed contract is the shapes/directions (monotone projection, neutral ⇒ identity, ceiling-gated
affordability), not the numbers (the #21 G2 / #40 / #41 / #31 precedent). `STAFF_MODIFIER_IDENTITY_PERMILLE`
and the `[CROSS]` identities are `[FIXED]`/`[CROSS]` because the **identity** (neutral ⇒ `×1.0`) is the
load-bearing behaviour-neutrality contract, not a tunable.

## Appendix B — Staff sub-blob layout (KD-4)

Composed into #30's `SeasonSaveCodec` frame as an opaque, independently version-gated block (mirrors the
`SeasonSaveCodec` / `MedicalSaveCodec` posture; every length-prefixed read preceded by an overflow-safe
`Require(offset, need, total)` bound against `total − offset`):

| Field | Type | Notes |
|---|---|---|
| version | u32 | `STAFF_SAVE_FORMAT_VERSION`; **gate first** (F3) |
| managedClubId | i32 | the club whose staff this block holds (managed-club scope, FR-ST-011) |
| nextStaffId | i32 | the monotonic stable-id high-water allocator (FR-ST-007) |
| slotCount | u32 | `Require`-bounded count (role slots; 3 at the scaffold, 1:1 with `StaffRole`) |
| per slot: StaffRole | u8 | ordinal-stable — **the slot key** (Coach/Physio/Scout); no separate role-slot field (1:1) |
| per slot: StaffId | i32 | stable id (FR-ST-007) |
| per slot: EmployerClubId | i32 | mutable employer (KD-7); `-1` = unemployed (deep) |
| per slot: Age | i32 | |
| per slot: StaffAttributes (7 × i32) | i32×7 | each `[1,20]` (F4 on `∉ [1,20]`) |
| per slot: FirstName / LastName | len-prefixed utf8 | `Require`-bounded |
| (deep: candidate pool + in-flight negotiations append here behind `deepStaffEnabled`) | — | append-only; the scaffold layout above is never reordered |
| (trailing-byte guard) | — | `if (o != len) throw` (F3) |

**No `RngCursor`/`actionOrdinal` field** — the scaffold is draw-free (FR-ST-009); deep draws are keyed
(position-independent), so no cursor is ever serialized. Deep candidate-pool / negotiation / staff-`Contract`
fields **append** behind `deepStaffEnabled` (FR-ST-013), guarded by the same version bump.

## Appendix C — Worked projection example

A head physio with `Medical = 15`, `Fitness = 12` (above the neutral `10`):
- `OccurrenceRiskMillMult = 1000 + (15 − 10) × slope_occ` — with a favourable (negative) occurrence slope,
  `< 1000` (lower injury risk).
- `RecoverySpeedMillMult = 1000 + (12 − 10) × slope_rec` — with a favourable (positive) recovery slope,
  `> 1000` (faster recovery).
- ⇒ `MedicalModifier(< 1000, > 1000)` — a **non-identity** modifier #41 consumes (deterministic; magnitudes
  balance-pass-illustrative).

A **neutral** head physio (`Medical = Fitness = … = 10`):
- every facet `= 1000 + 0 × slope = 1000` ⇒ `MedicalModifier(1000, 1000) = MedicalModifier.Identity`.
- ⇒ #41 behaves byte-identical to pre-#34 (FR-ST-014). All integer; two runs identical.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial appendices (constant catalogue, sub-blob layout, worked projection example), promoted from design supplement v0.4. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | Section-file AR PASS-1 (M): `ROLE_SLOTS` reconciled to **3 slots 1:1 with `StaffRole`** (dropped the phantom HeadOfRecruitment); Appendix B stores `StaffRole` once as the slot key (dropped the redundant `RoleSlot` field). |
#endregion
