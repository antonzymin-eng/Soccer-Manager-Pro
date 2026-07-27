# Club Infrastructure & Facilities #53 — Section 5: Test Plan

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.2
**Status:** APPROVED

---

Test-ID prefixes follow #19 §3.1.4: `T-IN-U-*` unit, `T-IN-I-*` integration, `T-IN-DET-*` determinism,
`T-IN-ID-*` identity / behaviour-neutrality, `T-IN-FAIL-*` fail-loud, `T-IN-BOUND-*` structural.

Every value asserted below is **hand-derivable from §3.7** or is a relational property. Nothing here
requires a fabricated expected number.

## 5.1 Identity / behaviour-neutrality (§1.7)

| ID | Test |
|---|---|
| T-IN-ID-001 | **The headline lock.** At `FACILITY_LEVEL_BASELINE` every projection equals its consumer's identity **exactly** — `ProjectAcademyQuality == AcademyQuality.Neutral`, `ProjectTrainingTerm == 0`, `ProjectMedicalModifier == MedicalModifier.Identity` — asserted by value equality against the consumers' own factories, not against a re-stated literal (§3.4). |
| T-IN-ID-002 | **The identity is not accidental at one club.** Swept across every club in a generated league: with every facility at baseline, every projection is the identity for **every** club. |
| T-IN-ID-003 | A career advanced for a full season with #53 present and every facility at baseline produces **field-identical** #42 / #41 / #29 outputs to the same career with the #53 seam null (the FR-SN-026 world-floor property). |
| T-IN-ID-004 | **(T0/T1 only.)** The season save is byte-identical to the pre-#53 save. Scoped deliberately: at **T2** the frame gains #53's sub-blob, so the *save* is not byte-identical — §1.7's identity claim is about behaviour and dials, never about the save frame. Conflating the two would make this test look like a stronger guarantee than #53 offers. |
| T-IN-ID-005 | An **unmodelled** club yields each consumer's identity from every projection — in particular `MedicalModifier.Identity` and **not** `default(MedicalModifier)`, which #41 rejects fail-loud (§3.4). The lock that catches the plausible "return default for absent" simplification. |

## 5.2 Unit — the projections (§3.4 / §3.5)

| ID | Test |
|---|---|
| T-IN-U-001 | §3.7(c) exact: `MedicalCentre` at level 4 ⇒ `(1000, 1120)`. |
| T-IN-U-002 | §3.7(d) exact: `YouthFacilities` at level 5 ⇒ `CeilingShiftPerMille = 60`. |
| T-IN-U-003 | §3.7(e) exact: `TrainingGround` at level 3 ⇒ training term `20`. |
| T-IN-U-004 | §3.7(f) exact: `Stadium` at level 3 ⇒ capacity `36000`. |
| T-IN-U-005 | **Monotonicity:** every projection is non-decreasing in its own facility's level across the full `[MIN, MAX]` range. |
| T-IN-U-006 | **Cross-facility independence:** changing any one facility's level leaves the other three projections **bit-identical**. The lock that fails if a future "holistic infrastructure score" is folded in behind the scenes. |
| T-IN-U-007 | `ProjectAcademyQuality` never returns a `CeilingShiftPerMille` outside **#42's own** `ACADEMY_CEILING_SHIFT_ABS_MAX` — swept at `FACILITY_LEVEL_MAX` with the per-level constant at `FACILITY_PER_LEVEL_ABS_MAX`, i.e. at the worst case the clamp exists for (§3.4). |
| T-IN-U-008 | `ProjectMedicalModifier.OccurrenceRiskMillMult` is `PERMILLE_ONE` at **every** level — #53 supplies a recovery term only (§3.4), so the double-count surface with #34's physio quality stays one field wide. |
| T-IN-U-009 | The §3.4 overflow bound holds: at `FACILITY_LEVEL_SPAN_MAX` steps and `FACILITY_PER_LEVEL_ABS_MAX` per level every product is exact and does not wrap. |
| T-IN-U-010 | Every projection is **pure**: called twice with no intervening mutation it returns equal values and leaves the store field-identical (FR-IN-026). |
| T-IN-U-011 | **No division exists** (§3.6): a source-level assertion over `src/club-infrastructure/` finds no `/` operator, no `Math.Floor`, no `Math.Round`, and no `double`/`float` in formula code. The mechanical form of FR-IN-003. |

## 5.3 Unit — the lifecycle (§3.1 / §3.2 / §3.3)

| ID | Test |
|---|---|
| T-IN-U-012 | §3.7(g) exact: `StartUpgrade(MedicalCentre, 3)` on day 400 from level 1 ⇒ `CompletionWorldDay = 760`, `TargetLevel = 3`. |
| T-IN-U-013 | §3.7(h): at day 759 nothing changes — still building. |
| T-IN-U-014 | §3.7(i): **the KD-7 idempotency lock.** `AdvanceFacilityDay(760)` applies the level and clears the record; a **second** call on the same day is a no-op with field-identical state. |
| T-IN-U-015 | §3.7(j): **the KD-7 gap lock.** Jumping straight from day 400 to day 900 completes the build and yields state field-identical to advancing every intervening day one at a time. Asserted as an equality between the two runs, so the property is that gaps are *equivalent*, not merely *tolerated*. |
| T-IN-U-016 | **No cursor exists.** `ClubFacilities` declares no `LastAdvancedWorldDay` field and the sub-blob contains none (KD-7). Asserted structurally so the predictable "consistency fix" — which would make T-IN-U-015 fail loud — cannot land silently. |
| T-IN-U-017 | §3.7(k): `CanStartUpgrade` returns **`false`** (no throw) while another build is in progress. |
| T-IN-U-018 | §3.7(l): a target at or below the current level returns `false` — the no-op purchase the player must not be charged for (FR-IN-016). |
| T-IN-U-019 | **`CanStartUpgrade` is pure** (FR-IN-011): called 1 000 times against every combination of facility and target level, the store is byte-identical afterwards. The whole KD-1 ordering rests on this, so it is asserted directly rather than inferred from the signature. |
| T-IN-U-020 | Completion is **atomic**: after `AdvanceFacilityDay` applies a level, `InProgressFacility == FACILITY_NONE_SENTINEL` and `TargetLevel == 0` in the same observed state — never a level applied with the record still set (§3.3). |
| T-IN-U-021 | `StartUpgrade` on a club **already at `FACILITY_LEVEL_MAX`** for the named facility is refused by the predicate and throws at the latch (FR-IN-016 / F6). |

## 5.4 Determinism

| ID | Test |
|---|---|
| T-IN-DET-001 | Two runs over the same command and day sequence produce **field-identical** state. |
| T-IN-DET-002 | `save@N → restore → advance to N+K` is **field-identical** to the uninterrupted run, including a build that spans the save boundary (FR-IN-033). |
| T-IN-DET-003 | **Draw-free** (FR-IN-030): running a full season of advances and upgrades leaves **every** registered RNG stream's cursor byte-identical. The mechanical proof that #53 registers nothing and draws nothing. |
| T-IN-DET-004 | **Genesis uniformity** (KD-2 / FR-IN-009): two careers created from **different world seeds** start with **identical** facility levels for every club. This is the lock that keeps #53 outside `WORLD_GENERATION_VERSION`, and the one that fails first if a seed-varied baseline is introduced without the accompanying promotion decision (FR-IN-010). |
| T-IN-DET-005 | **Order-independence:** advancing clubs in a permuted order yields field-identical state for every club — #53 holds no cross-club state, and this is what proves it. |

## 5.5 Integration — save / restore (KD-5)

| ID | Test |
|---|---|
| T-IN-I-001 | State → `Encode` → `Decode` is **field-identical**, including the `FACILITY_NONE_SENTINEL` idle state, a mid-build record, and every level. |
| T-IN-I-002 | Round-trip through a full `SeasonSaveCodec` frame: #53's sub-blob is **opaque** to the outer codec, and the world / season / match / sibling blobs are **byte-unchanged**. |
| T-IN-I-003 | An empty store (no modelled club) round-trips. |
| T-IN-I-004 | The two format versions move **independently**: bumping `FACILITY_SAVE_FORMAT_VERSION` does not require a `SEASON_SAVE_FORMAT_VERSION` bump, and vice versa. |
| T-IN-I-005 | **`FacilityType` ordinal stability**: each member's ordinal equals its pinned value, and the member count equals `FACILITY_TYPE_COUNT` — the `CueId` / `PassType` precedent. This is what makes FR-IN-007's APPEND-only contract enforceable rather than aspirational: a reorder fails here, before it re-points every saved club's facilities. |
| T-IN-I-006 | A decoded `Levels` array whose length ≠ `FACILITY_TYPE_COUNT` **throws** (F8) — the shape a roster append against an un-bumped version would produce. |

## 5.6 Integration — the #30 and command-layer seams

| ID | Test |
|---|---|
| T-IN-I-007 | **The purchase ordering** (KD-1 / §4.3): attempting an **invalid** upgrade with **sufficient funds** leaves the club's balance **untouched** and no build started. This is the exact case a debit-first implementation gets wrong and the player cannot recover from, so it is constructed rather than sampled. |
| T-IN-I-008 | The complementary case: a **valid** upgrade with **insufficient** funds leaves #53's state untouched — no build latched — because the sequence never reaches step 4. |
| T-IN-I-009 | **`StartUpgrade` re-validates** (FR-IN-013 / F6): a latch issued after the store has changed such that the predicate no longer holds **throws**, rather than starting a build from a stale check. |
| T-IN-I-010 | **Tick-order placement** (§4.4 / ERR-030-020): a build completing on day *N* is visible to every same-day consumer of a facility-derived input **on day *N***, not *N+1*. Pinned as a test so a later slot reorder fails here rather than silently introducing an unstated one-day lag. |
| T-IN-I-011 | `AdvanceFacilityDay` and both upgrade entry points **throw** for a `ClubId` with no entry, while every projection returns the consumer's identity for the same id — the deliberate F7 asymmetry, locked so a later "consistency" refactor cannot quietly collapse it in either direction. |

## 5.7 The double-count lock (KD-4 / FR-IN-023)

| ID | Test |
|---|---|
| T-IN-I-012 | **#53's projections are independent of staff state.** With a #34 staff projection varied across its full range and #53's facility levels held fixed, **every** #53 projection is bit-identical. Asserted directly, because *"the producer pre-blended it"* is the realistic way this breaks — a well-meaning producer that "helpfully" folds in coaching quality passes every other test in this file. |
| T-IN-I-013 | The **root** is the only place the two terms meet: with both a #34 term and a #53 term non-identity, the consumer's observed dial equals `Combine(staffTerm, facilityTerm)` and each input appears **exactly once** in it. |
| T-IN-I-014 | #53 exposes **no** parameter, field, or overload that accepts a `CoachingModifier`, a `MedicalModifier`, or any #34 type as an *input* — asserted over the public surface, so the double-count path cannot be opened by adding an overload. |

## 5.8 Structural (the boundaries #53 must not cross)

| ID | Test |
|---|---|
| T-IN-BOUND-001 | #53's assembly references **only** #27 and #16 — asserted from the assembly's reference set, so a future `using` of #30 / #40 / #34 / any consumer / `SeasonSave` / `MatchEngine` fails the build's test gate (FR-IN-027, the #40 `T-FN-BOUND-002` / #45 `T-BD-BOUND-001` posture). |
| T-IN-BOUND-002 | #53 declares **no** type named `AcademyQuality`, `MedicalModifier`, `TrainingInput`, `CoachingModifier`, or `BoardModifier` — the consumers' types are consumed, never shadowed (FR-IN-020). A parallel declaration would compile, which is why this is mechanical. |
| T-IN-BOUND-003 | #53 exposes **no** member carrying a currency, price, cost, or budget quantity, and none that could serve as a spend command (FR-IN-005 / FR-IN-028). |
| T-IN-BOUND-004 | #53 makes **no** call into `DeterministicRngService` — asserted over the compiled surface rather than the reference graph, which cannot prove it since #16 is legitimately referenced for `CanonicalSerializer` (§4.1 / KD-6). |
| T-IN-BOUND-005 | **No foreign writes:** a `Squad`, `PlayerRecord`, `ClubFinances`, `SeasonState` and `AcademyState` handed alongside every #53 entry point are **field-unchanged** after every upgrade, advance, projection, and save/restore. Asserted behaviourally — #27 is referenced, so its types *are* reachable and the reference graph cannot prove this (§4.7 standing item). |

## 5.9 Fail-loud (§2.3)

| ID | Test |
|---|---|
| T-IN-FAIL-001 | A stored level outside `[MIN, MAX]` reaching any seam ⇒ **throws**, never silently clamped, and specifically **not** returned as an ordinary `false` from `CanStartUpgrade` (§3.7(m) / F1). |
| T-IN-FAIL-002 | Inserting a **default-constructed `ClubFacilities`** ⇒ **throws at insertion** (F4a). The test must assert this at the *insertion* seam specifically: `InProgressFacility == 0` is a **valid** `FacilityType` ordinal, so no range check can catch it, and the entry would read as a live training-ground build that the next advance "completes" at level `0`. |
| T-IN-FAIL-003 | An undefined `FacilityType` ordinal, or an `InProgressFacility` that is neither `-1` nor a defined ordinal, ⇒ **throws** at the seam and at decode (F2). |
| T-IN-FAIL-004 | Decode: wrong `FACILITY_SAVE_FORMAT_VERSION` ⇒ throws (F3), with the version read **before** any field below it is interpreted. |
| T-IN-FAIL-005 | Decode: an out-of-bounds / near-`int.MaxValue` length prefix ⇒ throws via the overflow-safe bound compared against `total − offset`, never wraps (F5). |
| T-IN-FAIL-006 | Decode: trailing bytes ⇒ throws (F5). |
| T-IN-FAIL-007 | `StartUpgrade` whose predicate no longer holds ⇒ throws (F6) — see T-IN-I-009. |
| T-IN-FAIL-008 | **Completion-day overflow** (§3.2): a latch whose `worldDay + days` would wrap the `uint` day space ⇒ **throws**. Without the guard the build would carry a completion day in the *past* and complete instantly — a failure no other check in #53 would catch. |
| T-IN-FAIL-009 | A `Levels` array of the wrong length ⇒ throws at both insertion and decode (F8). |

## 5.10 Closed-loop scenario (#19 `ScenarioRunner`, T-phase)

One Simulation-layer scenario, `facility-upgrade-across-a-season`, owning specs `{16, 19, 27, 29, 30, 40,
41, 42, 53}`, registered under `SCENARIO_PATH_CROSS_SPEC_PREFIX`:

start a career with every club at baseline; assert every consumer output matches a #53-absent run
(T-IN-ID-003's composition-level form); issue an upgrade through the real command sequence and assert the
balance moved **once**; save mid-build; restore; advance past the completion day and assert the level
applied on the same day as an uninterrupted run; then assert the consumers' dials changed **once** and in
the direction §3.4 specifies.

This is the composition-level proof that KD-1's ordering, KD-3's dated latch, KD-4's single-combination
root, KD-5's blob, and KD-7's cursor-free advance hold **together** — which no unit test exercises
jointly, and which is exactly where the F7 modelled/unmodelled asymmetry (§4.5) would fail if it were
wrong.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §5 (identity, projection and lifecycle units keyed to the §3.7 worked examples, determinism incl. the genesis-uniformity lock, save/seam integration, the double-count lock, structural boundary tests, fail-loud, the T-phase closed-loop scenario). Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 fixes. **M:** added **T-IN-U-016** (no cursor exists, asserted *structurally*) — without it KD-7 is a prose claim and the predictable "consistency fix" that would break T-IN-U-015 could land with the suite green. **M:** added **T-IN-ID-005** (unmodelled club yields `MedicalModifier.Identity`, not `default`) and **T-IN-FAIL-008** (completion-day overflow) to match §3's PASS-1 corrections. **M:** added **T-IN-BOUND-004** — the draw-absence must be asserted over the compiled surface, since #16 is legitimately referenced and the reference graph therefore cannot prove KD-6. **L:** added T-IN-U-011 (no division, the mechanical form of FR-IN-003), T-IN-U-006 (cross-facility independence), T-IN-I-005/006 (ordinal stability + array-length guard, which make APPEND-only enforceable), T-IN-I-014 (no #34-typed input parameter, so the double-count path cannot be opened by an overload), T-IN-DET-005 (order-independence); T-IN-U-015 strengthened from *"tolerated"* to an **equality** against the day-by-day run. |
#endregion
