# Club Infrastructure & Facilities #53 — Appendices

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.2
**Status:** IN REVIEW

---

## Appendix A — Constant catalogue

Region order per Spec #20: Fixed → Derived → Cross → GT, **omitting any region with no constants** (#20
prohibits empty regions). #53 has no `[EST]` and no `[CROSS-PENDING]` constants, so neither region
appears. `[GT]` values are **illustrative pending the T3 balance pass** (§7.1) — the spec's contract is
their *shape, direction and identity behaviour*, never their magnitude, and §5 asserts nothing else.

### A.1 Fixed

| Constant | Value | Tag | Notes |
|---|---|---|---|
| `FACILITY_SAVE_FORMAT_VERSION` | `1` | `[FIXED]` | The sub-blob's own version gate (KD-5). Independent of `SEASON_SAVE_FORMAT_VERSION`; bumping either never implies the other (§4.6). Registered in #50's version registry (XC-053-017). |
| `FACILITY_NONE_SENTINEL` | `-1` | `[FIXED]` | The idle `InProgressFacility`. **Deliberately not `uint.MaxValue`** and deliberately **not `0`**: `0` is a valid `FacilityType` ordinal (F4a), and `uint.MaxValue` is a legal *computed* `CompletionWorldDay` (§3.2). The sibling specs' `uint.MaxValue` cursor sentinel is safe there only because their field is a *last-advanced* day, never a future one. |
| `FACILITY_LEVEL_MIN` | `1` | `[FIXED]` | Levels are 1-based so that `0` — the value every zero-initialised field carries — is **always** invalid and therefore always caught (F1). |
| `FACILITY_LEVEL_BASELINE` | `1` | `[FIXED]` | Genesis for every club and every facility (FR-IN-009). **Fixed, not tunable:** §1.7's identity property is *"at baseline every projection equals its consumer's identity"*, which requires `Steps(baseline) == 0` exactly. Moving it off `FACILITY_LEVEL_MIN` would also make level `1` a *below-baseline* state the Stage-3 lifecycle cannot reach. |
| `FACILITY_LEVEL_MAX` | `5` | `[FIXED]` | The upper level bound. Fixed rather than `[GT]` because it bounds the §3.4 overflow argument together with `FACILITY_PER_LEVEL_ABS_MAX`; raising it is an arithmetic-safety change, not a balance change. |
| `FACILITY_PER_LEVEL_ABS_MAX` | `1000` | `[FIXED]` | The absolute bound on every per-level `[GT]` constant, enforced at the consuming seam. With `FACILITY_LEVEL_SPAN_MAX` it is what keeps every §3.4 product inside `int` by three orders of magnitude. |
| `PERMILLE_ONE` | `1000` | `[FIXED]` | The per-mille identity, used when building `MedicalModifier` (KD-8). |

### A.2 Derived

| Constant | Formula | Tag | Notes |
|---|---|---|---|
| `FACILITY_TYPE_COUNT` | `Enum.GetValues(typeof(FacilityType)).Length` | `[DERIVED]` | The roster size — **4** at Stage 3. Derived rather than a literal because two assemblies carrying private copies of an enum's member count is the `POSITION_COUNT` parallel-surface defect this project has already hit; locked against the enum by T-IN-I-005. |
| `FACILITY_LEVEL_SPAN_MAX` | `FACILITY_LEVEL_MAX − FACILITY_LEVEL_BASELINE` | `[DERIVED]` | **4** — the maximum `Steps` value, and therefore the multiplier in every §3.4 overflow bound. Never set independently. |

### A.3 Cross (consumed read-only; never re-declared)

| Constant / type | Authority | Notes |
|---|---|---|
| `AcademyQuality`, `AcademyQuality.Neutral` (`default`, all-zero) | #42 `youth-academy-intake` §2.2 | Returned by `ProjectAcademyQuality`. Consumed, never re-declared (T-IN-BOUND-002). |
| `ACADEMY_CEILING_SHIFT_ABS_MAX` (`300‰`) | #42 `appendices.md` | #53 clamps against **#42's** bound, because #42 fails loud outside it (§3.4 / XC-053-003). |
| `MedicalModifier`, `MedicalModifier.Identity` (`new(1000, 1000)`) | #41 `injuries-medical` §2.2 | Returned by `ProjectMedicalModifier` — always via the factory, **never `default()`**, which is ×0 and fails loud at #41's seam (KD-8). |
| `TrainingInput` | #28 §2.2, **written solely by #29** | Named here only to record that #53 **never returns it** (KD-9 / FR-IN-024). |
| `ClubId` | #27 | The keying identity. #27's schema is untouched. |

### A.4 GT (illustrative, balance-pass pending)

| Constant | Value | Notes |
|---|---|---|
| `FACILITY_BUILD_DAYS_PER_LEVEL` | `180` | Build duration per level step (§3.2). At `FACILITY_LEVEL_SPAN_MAX = 4` the longest single build is 720 days — deliberately multi-season, so an upgrade is a commitment rather than a routine purchase. |
| `FACILITY_ACADEMY_SHIFT_PER_LEVEL` | `15` (‰) | Per level above baseline, into `AcademyQuality.CeilingShiftPerMille`. At max level: `+60‰`, well inside #42's `±300‰` bound — the clamp exists for a retuned constant, not for this value. |
| `FACILITY_TRAINING_TERM_PER_LEVEL` | `10` | Per level, into #29's `ComputeTrainingInput` (KD-9). Its *interpretation* is **#29's**, not #53's — #53 supplies a magnitude on a scale #29 owns, which is why this row cannot be balanced independently of #29's Stage-3 tier. |
| `FACILITY_MEDICAL_MULT_PER_LEVEL` | `40` (‰) | Per level, into `MedicalModifier.RecoverySpeedMillMult`. At max level: `1160` — recovery ~16% faster. |
| `FACILITY_MEDICAL_MULT_MIN` / `_MAX` | `1000` / `1300` | Clamp on the projected recovery multiplier. **The floor is `1000`, not lower**: at Stage 3 a facility never makes recovery *worse* than identity, because there is no below-baseline level to express it (A.1). A deep-tier decay model (§7.2) would be what lowers this floor — and would need to say so explicitly. |
| `STADIUM_BASE_CAPACITY` | `20000` | Baseline capacity (§3.5). #40 calibrates its deferred attendance model against **this** value; that calibration is #40's to do (§8.2). |
| `STADIUM_CAPACITY_PER_LEVEL` | `8000` | Per level above baseline. At max level: 52 000. |
| `FACILITY_BUDGET_ADVANCE_US` | `5` | §6.3 ceiling for one club's daily advance. A **ceiling, not a measurement** — no certified number exists for #53. |
| `FACILITY_BUDGET_PROJECTION_US` | `2` | §6.3 ceiling for one club's single projection. Same caveat. |

**Where the upgrade *price* is not.** No currency constant appears in this catalogue, and that is
FR-IN-005 rather than an omission: #53 holds no price. The price table lives beside the command handler
(§4.3), which is the layer that owns the purchase.

**Consequence of the `[GT]` staging, stated plainly:** #53's behavioural neutrality (§1.7) holds **at
baseline levels**, not at these magnitudes — the magnitudes only matter once a level has changed, which
is T3. A retune therefore cannot break the identity tier, and §5's identity tests do not depend on any
value in this table.

## Appendix B — Save sub-blob layout (KD-5)

Canonical field order, written through #16's `CanonicalSerializer`. **Opaque to `SeasonSaveCodec`** — the
outer codec sees a length-prefixed byte block and never parses it (FR-IN-032).

| # | Field | Type | Notes |
|---|---|---|---|
| 1 | `FACILITY_SAVE_FORMAT_VERSION` | `u16` | **Version gate first** — read and checked before any field below it is interpreted (F3). |
| 2 | `ClubCount` | `i32` | Length prefix — read through the overflow-safe bound compared against `total − offset`, never `offset + need` (F5; the `MatchSaveCodec` hardening). |
| 3 | per club × `ClubCount` | — | `ClubId` (`i32`); then `FACILITY_TYPE_COUNT` × level (`i32` each, in `FacilityType` ordinal order); then `InProgressFacility` (`i32`), `TargetLevel` (`i32`), `CompletionWorldDay` (`u32`). |
| — | *(trailing-byte guard)* | — | The read MUST end exactly at the block end (F5). |

Clubs are written in **ascending `ClubId` order**, and each club's levels in **`FacilityType` ordinal
order**, so the blob is a function of state — never of insertion order or of iteration order.

**Decode validates, it does not trust** (FR-IN-034 / F2 / F8): every level is range-checked, the level
count is checked against `FACILITY_TYPE_COUNT`, and `InProgressFacility` must be either
`FACILITY_NONE_SENTINEL` or a defined ordinal. A blob that decodes to a structurally impossible club
throws rather than materializing.

**Deliberately absent — three things, each for its own reason:**

1. **Any `RngStreamState` or cursor.** #53 is draw-free (KD-6), so there is nothing to serialize and
   nothing to resume.
2. **Any `LastAdvancedWorldDay`.** KD-7: the advance is idempotent by construction, so a cursor would
   buy nothing while adding a field here and a failure mode to the surface. **This is the point of
   temptation** — a maintainer aligning #53 with its four sibling specs would add it here first, and
   T-IN-U-016 exists to fail when they do.
3. **Any currency, price, or cost.** FR-IN-005. Storing an upgrade's price alongside its completion day
   would look natural and would create a second truth for a quantity #40 and the command layer own
   between them.

**APPEND-only** (FR-IN-035). New fields — and new `FacilityType` members, which widen row 3 — go at the
**end** with a `FACILITY_SAVE_FORMAT_VERSION` bump. Inserting mid-block, or appending an enum member
without the bump, shifts every subsequent offset; F8's length check is what turns the second of those
from silent corruption into a loud failure.

## Appendix C — Facility roster and dial mapping

| `FacilityType` | Ordinal | Consumer | Dial (consumer's own type) | Identity convention | Status |
|---|---|---|---|---|---|
| `TrainingGround` | `0` | **#29** Training | an input to `ComputeTrainingInput` | additive, zero-identity | live at T2 |
| `YouthFacilities` | `1` | **#42** Youth Academy | `AcademyQuality.CeilingShiftPerMille` | additive, zero-identity | live at T2 |
| `MedicalCentre` | `2` | **#41** Injuries & Medical | `MedicalModifier.RecoverySpeedMillMult` | **multiplicative, 1000-identity** | live at T2 |
| `Stadium` | `3` | **#40** Club Finances | matchday attendance bound | **absolute — not a dial** | deferred to #40's T3 |

**Ordinals are persisted and APPEND-only** (FR-IN-007). Reordering this table re-points every saved club's
facilities to the wrong building, silently and for every existing career — which is why T-IN-I-005 asserts
each ordinal against its pinned value rather than merely asserting the members exist.

**One member per existing consumer dial** (FR-IN-008). A `ScoutingInfrastructure` row is **absent** by
decision, not oversight: #32 declares no such dial, and an enum member with no consumer is the phantom
FR-LW-031 forbids — permanently, in an APPEND-only roster. Adding it later costs one append and a version
bump (§7.2).

**The two identity conventions are visible in this table on purpose** (KD-8). A reader scanning it should
see immediately that `MedicalCentre` is the odd one out, because that is exactly the asymmetry a
tidying refactor would erase — and erasing it would hand #41 a ×0 recovery multiplier or #42 a permanent
`+1000‰` ceiling shift.

**Not tabulated: a per-level effect-size table for the consumers.** What a `+15‰` ceiling shift or a
`1120` recovery multiplier *means* in play is each consumer's own response curve (#42's, #41's, #29's),
and tabulating it here would restate their models — creating exactly the second source §1.2 exists to
prevent. #53's contract ends at the number it hands over.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial appendices (A.1 Fixed incl. the sentinel rationale and the overflow-bounding constants, A.2 Derived, A.3 Cross, A.4 GT with the identity-at-baseline consequence; B save layout with the three deliberately-absent fields; C the roster + dial-mapping table). Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 fixes. **M:** `FACILITY_BUDGET_ADVANCE_US` / `FACILITY_BUDGET_PROJECTION_US` were declared `[GT]` in §6.3 but **absent from this catalogue**, which is meant to be the single catalogue and is what a reader greps for tag discipline (the #45 PASS-1 M-2 defect, repeated) — added to A.4 with their ceiling-not-measurement caveat. **L:** A.1 gained the reason `FACILITY_LEVEL_MIN` is `1` (so `0` — every zero-initialised field — is always invalid) and why `FACILITY_LEVEL_BASELINE` is `[FIXED]` rather than `[GT]` (the identity property requires `Steps(baseline) == 0` exactly); A.2 added `FACILITY_LEVEL_SPAN_MAX` and the `POSITION_COUNT` rationale for deriving `FACILITY_TYPE_COUNT`; A.4 recorded that `FACILITY_TRAINING_TERM_PER_LEVEL` sits on a scale **#29** owns and so cannot be balanced independently, and why the medical floor is `1000`; B gained the decode-validates paragraph and marked the missing cursor as the **point of temptation**; C gained the identity-convention column and the no-effect-size-table rationale. |
#endregion
