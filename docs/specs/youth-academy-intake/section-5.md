# Youth Academy & Intake #42 — Section 5: Test Plan

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.2 — section-file PASS-1 fix pass)
**Version:** 0.2
**Status:** IN REVIEW

---

Test-ID prefixes follow #19 §3.1.4: `T-YA-U-*` unit, `T-YA-I-*` integration, `T-YA-DET-*` determinism,
`T-YA-ID-*` identity/behaviour-neutrality, `T-YA-FAIL-*` fail-loud.

**A note on what is NOT hand-verified.** The *contents* of a generated cohort are the output of #28's
draw sequence over SipHash-keyed material and are **not** hand-computable; this spec therefore does not
table expected attribute values (fabricating them would violate the project's "never fabricate
verification values" rule). The cohort is pinned by **relational** properties instead — identity against
a direct `RegenGenerator` call, two-run equality, and position-independence — each of which is
mechanically checkable without knowing a single drawn number. The §3.5 worked examples, which *are* hand
arithmetic, are pinned by exact-value tests.

## 5.1 Identity / behaviour-neutrality (KD-8)

| ID | Test |
|---|---|
| T-YA-ID-001 | **The headline lock.** A cohort generated with `AcademyQuality.Neutral` is **field-identical** to the sequence produced by calling `RegenGenerator.GenerateRegen` directly, same stream, same anchor, same ids — i.e. #42 at minimal is provably an identity over #28. |
| T-YA-ID-002 | `default(AcademyQuality) == AcademyQuality.Neutral` (FR-YA-010 — the zero-value lock §4.3 exists to make explicit). |
| T-YA-ID-003 | `ApplyCeilingShift(life, Neutral)` returns a **bit-identical** `PlayerLifecycle` (FR-YA-006 early return, not an arithmetic round-trip). |
| T-YA-ID-004 | With `ACADEMY_AGE_*` at their minimal values, `ReanchorAge` returns the input pair unchanged (FR-YA-008). |
| T-YA-ID-005 | A season advanced with the #30 academy seam null is **byte-identical** to the same season pre-#42 (the FR-SN-026 world-floor property is unaffected by a null seam). |

## 5.2 Unit — the transforms (§3.3)

| ID | Test |
|---|---|
| T-YA-U-001 | §3.5(b) exact: `PA=6000, CA=4200, +150‰ ⇒ PA'=6900` (floor 5200, not binding). |
| T-YA-U-002 | §3.5(c) exact: `PA=8000, CA=5500, −400‰ ⇒ PA'=6500` (floor binding). |
| T-YA-U-003 | **Monotonicity** (§3.3.1): over a swept input grid satisfying the entry invariant, a negative dial never raises PA and a positive dial never lowers it. |
| T-YA-U-004 | **Sign-symmetry** (§3.6): `±N‰` move PA by equal magnitude in opposite directions — the lock that fails if `Math.Floor` / `Math.Round` is substituted. |
| T-YA-U-005 | The shifted PA always satisfies `PA' ≥ max(PA_MIN, min(CA + REGEN_PA_HEADROOM, ABILITY_MAX))` — the generator's own postcondition (FR-YA-005); no dial magnitude can produce a zero-headroom prospect. |
| T-YA-U-006 | `ApplyCeilingShift` leaves `CurrentAbility` and every `PlayerAttributes` field untouched (FR-YA-004). |
| T-YA-U-007 | A positive dial clamps at `ABILITY_MAX` and never exceeds it. |
| T-YA-U-008 | `ReanchorAge` (deep band) moves `Age` **and** `BirthWorldDay` together, and `BirthWorldDay` matches #28's own formula for the resulting age (FR-YA-007). |
| T-YA-U-009 | `DeriveActionOrdinal` is injective over `(clubId, worldDay, purpose)` across the tested range, and refuses `purpose ≥ DRAW_PURPOSE_RADIX` (§3.2 bound guard). |
| T-YA-U-014 | `DeriveActionOrdinal` refuses `clubId ≥ ACADEMY_CLUB_STRIDE` (§3.2 injectivity guard) — the case that would otherwise silently alias two clubs onto one anchor with no divergence signal. |

## 5.3 Unit — the trigger (§3.1)

| ID | Test |
|---|---|
| T-YA-U-010 | Genesis (`HasIntaken == false`) fires on the first evaluated day, **including world day 0** (FR-YA-015 — the sentinel, not day-0 arithmetic). |
| T-YA-U-011 | Days between intakes return `IntakeResult.Empty`, mutate no state, and consume no draw (FR-YA-017). |
| T-YA-U-012 | The trigger is `>=`: an advance loop that steps **over** the exact due day still fires on the next evaluated day (§3.1). |
| T-YA-U-013 | A throw inside generation leaves `LastIntakeWorldDay` / `HasIntaken` unchanged, so the day remains retryable (§3.1 stamp-last). |

## 5.4 Determinism (KD-7)

| ID | Test |
|---|---|
| T-YA-DET-001 | Two runs from the same `(worldSeed, clubId, intakeWorldDay)` produce **field-identical** cohorts (the `RosterGeneratorTests` shape). |
| T-YA-DET-002 | **Position-independence — the KD-7 lock.** An intake preceded by a *different number of prior draws* on the same stream produces the **same** cohort. This is the test that fails if the per-intake anchor is later "simplified" away. |
| T-YA-DET-003 | Distinct clubs, same world day ⇒ distinct cohorts (the anchor separates them). |
| T-YA-DET-004 | Distinct world days, same club ⇒ distinct cohorts. |
| T-YA-DET-005 | An intake performed **after** a save→restore is byte-identical to the same intake in an uninterrupted run — with **no cursor in the blob** (FR-YA-020). |
| T-YA-DET-006 | A refused intake (F2/F4) consumes no draw: the stream state after the refusal equals the state before it. |

## 5.5 Integration — save / restore (KD-6)

| ID | Test |
|---|---|
| T-YA-I-001 | `AcademyState` → `Encode` → `Decode` is **field-identical**, including the latch, the sentinel, the id high-water, and every prospect. |
| T-YA-I-002 | Round-trip through a full `SeasonSaveCodec` frame: the academy sub-blob is opaque to the outer codec, and the world / match blobs are byte-unchanged. |
| T-YA-I-003 | **One-shot across a restore** (FR-YA-016): save on the intake day → restore → advance ⇒ exactly one cohort. |
| T-YA-I-004 | An empty academy (genesis, no cohort) round-trips. |

## 5.6 Integration — promotion (KD-5)

| ID | Test |
|---|---|
| T-YA-I-005 | An accepted promotion removes the prospect from the academy roster and returns the record with an **unchanged `PlayerId`** (FR-YA-026). |
| T-YA-I-006 | Promotion into a senior squad at `CLUB_SQUAD_SIZE` is **refused**, and leaves the academy roster untouched (FR-YA-025 / F5). |
| T-YA-I-007 | Promotion of an unknown prospect is refused (F5). |
| T-YA-I-008 | A promoted prospect's `PlayerId` is never re-issued by a later intake (FR-YA-026 / F6). |
| T-YA-I-009 | #42 performs **no** `Squad` write on any path. Note this **cannot** be asserted from the reference graph — #42 references #27 for `PlayerRecord` / `CLUB_SQUAD_SIZE`, so `Squad` *is* reachable. It is asserted behaviourally: a `Squad` handed to the composition root alongside every #42 entry point is field-unchanged after intake, promotion, and save/restore, and the absence of a write is a standing review item (§4.6). |

## 5.7 Fail-loud (§2.3)

| ID | Test |
|---|---|
| T-YA-FAIL-001 | Sub-blob decode: wrong `ACADEMY_SAVE_FORMAT_VERSION` ⇒ throws (F3). |
| T-YA-FAIL-002 | Sub-blob decode: an out-of-bounds / near-`int.MaxValue` length prefix ⇒ throws via the overflow-safe bound, never wraps (F3, the `MatchSaveCodec` hardening). |
| T-YA-FAIL-003 | Sub-blob decode: trailing bytes ⇒ throws (F3). |
| T-YA-FAIL-004 | An out-of-bounds `AcademyQuality` dial ⇒ throws at the consuming seam, never clamped (F2 / FR-YA-011). |
| T-YA-FAIL-005 | An intake for an unknown `ClubId` ⇒ throws (F4), never auto-creates state. |
| T-YA-FAIL-006 | A lifecycle violating `PA ≥ CA` reaching a #42 seam ⇒ throws (F1). |
| T-YA-FAIL-007 | An allocation at or below the id high-water ⇒ throws (F6). |
| T-YA-FAIL-008 | A cohort size outside `[ACADEMY_COHORT_SIZE_MIN, ACADEMY_COHORT_SIZE_MAX]` ⇒ throws (F2). |

## 5.8 Closed-loop scenario (#19 `ScenarioRunner`, T-phase)

One Simulation-layer scenario, `academy-intake-across-a-restore`, owning specs `{16, 19, 27, 28, 42}`,
registered under `SCENARIO_PATH_CROSS_SPEC_PREFIX`: run a career through an intake, save, restore,
continue to the next intake, and assert both cohorts match an uninterrupted run — the composition-level
proof that KD-4's latch and KD-7's anchor hold together, which no unit test exercises jointly.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §5 (identity, transform + trigger units, determinism incl. the position-independence lock, save/promotion integration, fail-loud, the T-phase closed-loop scenario), with an explicit note on why cohort contents are pinned relationally rather than by fabricated expected values. Status IN REVIEW. |
| 0.2 | 2026-07-24 | — | PASS-1 fix (M): T-YA-I-009's structural claim was **false** — #42 references #27 for `PlayerRecord`/`CLUB_SQUAD_SIZE`, so `Squad` IS reachable and the no-write property cannot be asserted from the reference graph; re-stated as a behavioural assertion + standing review item. Added T-YA-U-014 for the new §3.2 clubId guard. |
#endregion
