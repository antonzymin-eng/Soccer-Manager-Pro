# Board & Ownership Dynamics #45 — Section 5: Test Plan

**Created:** July 25, 2026
**Last Updated:** July 25, 2026 (v0.2 — section-file PASS-1 fix pass)
**Version:** 0.2
**Status:** APPROVED

---

Test-ID prefixes follow #19 §3.1.4: `T-BD-U-*` unit, `T-BD-I-*` integration, `T-BD-DET-*` determinism,
`T-BD-ID-*` identity/behaviour-neutrality, `T-BD-FAIL-*` fail-loud, `T-BD-BOUND-*` structural.

Every value asserted below is **hand-derivable from §3.7** or is a relational property. Nothing here
requires a fabricated expected number.

## 5.1 Identity / behaviour-neutrality (KD-8)

| ID | Test |
|---|---|
| T-BD-ID-001 | **The headline lock.** With `OwnershipProfile.Identity` and `BD_MORALE_WEIGHT_PERMILLE = 0`, `ComputeConfidenceTarget` returns **exactly** `input.ObjectiveTrackPermille` — the §3.2 identity property, swept across the full `[0,1000]` input range. |
| T-BD-ID-002 | With `BD_BUDGET_SENSITIVITY_PERMILLE = 0`, `TryProjectBoardModifier` returns **exactly** `BoardModifier.Identity` for **every** confidence value in `[0,1000]`, not merely at neutral (FR-BD-019). |
| T-BD-ID-003 | `OwnershipProfile.Identity` has every dial at `1000`, and is **not** equal to `default(OwnershipProfile)`. |
| T-BD-ID-004 | A club #45 does not model yields `false` + caller-substituted `Identity`, so #40's settled budget is **bit-identical** to the pre-#45 result (FR-BD-018). |
| T-BD-ID-005 | **(T0/T1 only.)** A season advanced with the #30 board seam **null** is byte-identical to the same season pre-#45 (the FR-SN-026 world-floor property is unaffected by a null seam). Scoped deliberately: at **T2** the seam is live and the save gains #45's sub-blob, so the *save* is **not** byte-identical — KD-8's identity claim is about #40's settled budgets and existing RNG cursors, never about the save frame. Conflating the two would make this test look like a stronger guarantee than #45 offers. |

## 5.2 Unit — the projections (§3.2 / §3.3 / §3.4)

| ID | Test |
|---|---|
| T-BD-U-001 | §3.7(b) exact: identity owner, `track = 800`, `conf = 500` ⇒ `conf = 520`. |
| T-BD-U-002 | §3.7(c) exact: identity owner, `track = 200`, `conf = 500` ⇒ `conf = 480`. |
| T-BD-U-003 | §3.7(d) exact: `severity = 1200`, `track = 800` ⇒ `target = 700` (**not** 800) — the reference-shift lock. |
| T-BD-U-004 | **The direction lock that kills the rejected formulation.** A higher `ExpectationSeverity` yields a target **≤** the identity target for *every* on-track input — including inputs **below** neutral. The deviation-scaling alternative passes this above neutral and fails it below, which is precisely why §3.2 shifts the reference instead. |
| T-BD-U-005 | §3.7(e) exact: `patience = 1500`, falling ⇒ step `30`, `conf = 470`; and patience has **no** effect on a rising step. |
| T-BD-U-006 | §3.7(g) exact: `conf = 800`, `contribution = 1000`, `sensitivity = 200` ⇒ `mult = 1060`. |
| T-BD-U-007 | **Monotonicity:** target is non-decreasing in `ObjectiveTrackPermille`; the projected multiplier is non-decreasing in confidence. |
| T-BD-U-008 | **Sign-symmetry** (§3.6): `±N` deviations move the multiplier by equal magnitudes in opposite directions — the lock that fails if `Math.Floor` or `Math.Round` is substituted. |
| T-BD-U-009 | `DriftPermille` is idempotent at `cur == tgt`, never overshoots (`|new − cur| ≤ min(step, |tgt − cur|)`), and stays in `[0,1000]`. |
| T-BD-U-010 | `DriftPermille` is **semantically equivalent** to #33 §3.1's specified formula across a swept grid (the KD-1 equivalence pin — asserted against the formula as written, since #45 does not reference #33). |
| T-BD-U-011 | Every clamp holds at the extremes: a target of `0`/`1000` is reachable and never exceeded. |
| T-BD-U-012 | `DeriveJobSecurityBand` is **exhaustive and half-open**: every value in `[0,1000]` maps to exactly one band, and each boundary value maps to the **upper** band (§3.7(h): `200 → Insecure`). |
| T-BD-U-013 | The §3.3 overflow bound holds: at `|dev| = 500`, `contribution = BD_DIAL_MAX`, `sensitivity = 1000` the computation is exact and does not wrap. |

## 5.3 Unit — the daily step (§3.1)

| ID | Test |
|---|---|
| T-BD-U-014 | Genesis (`LastAdvancedWorldDay == BD_NOT_ADVANCED_SENTINEL`) advances on the first evaluated day, **including world day 0** — the sentinel is `uint.MaxValue`, so day 0 is a legal advance and not a silent no-op (FR-BD-008). |
| T-BD-U-015 | Re-advancing the same `worldDay` is a **no-op**: state field-identical, cursor unchanged (F6). |
| T-BD-U-016 | A day **gap** (`worldDay > last + 1`) **throws** (F6). |
| T-BD-U-017 | `BoardDayInput.Neutral` on a non-fixture day is a well-defined advance toward an unchanged target, not a skip (FR-BD-010). |
| T-BD-U-018 | **Stamp-last:** a throw inside validation or target assembly leaves `LastAdvancedWorldDay` **and** `ConfidencePermille` unchanged, so the day stays retryable (FR-BD-023). |

## 5.4 Determinism

| ID | Test |
|---|---|
| T-BD-DET-001 | Two runs over the same input sequence produce **field-identical** state. |
| T-BD-DET-002 | An advance after a save→restore is field-identical to the same advance in an uninterrupted run — with **no cursor in the blob** (FR-BD-028). |
| T-BD-DET-003 | The whole minimal tier is draw-free: running a full season of advances leaves **every** registered RNG stream's cursor byte-identical (FR-BD-020). |
| T-BD-DET-004 | *(deep tier)* **Position-independence:** a takeover evaluation preceded by a different number of prior draws yields the **same** result — the lock that fails if the keyed ordinal is later "simplified" to a free-running cursor. |
| T-BD-DET-005 | *(deep tier)* `DeriveActionOrdinal` is injective over `(clubId, worldDay, purpose)` across the tested range, and refuses `purpose ≥ DRAW_PURPOSE_RADIX` and `clubId ≥ BD_CLUB_STRIDE` (§3.5 guards — the second is the one that would otherwise silently alias two clubs onto one draw). |

## 5.5 Integration — save / restore (KD-6)

| ID | Test |
|---|---|
| T-BD-I-001 | State → `Encode` → `Decode` is **field-identical**, including the cursor sentinel, the dials, and the zero-valued `TakeoverState`. |
| T-BD-I-002 | Round-trip through a full `SeasonSaveCodec` frame: #45's sub-blob is **opaque** to the outer codec, and the world / season / match blobs are byte-unchanged. |
| T-BD-I-003 | An empty store (no modelled club) round-trips. |
| T-BD-I-004 | The three format versions move **independently**: bumping `BOARD_SAVE_FORMAT_VERSION` does not require a `SEASON_SAVE_FORMAT_VERSION` bump, and vice versa. |

## 5.6 Integration — the #30 / #40 seams

| ID | Test |
|---|---|
| T-BD-I-005 | The boundary projection is asked for **every** club and answers `false` for unmodelled ones without throwing, while `AdvanceBoardDay` on an unmodelled club **throws** — the deliberate §4.5 asymmetry, locked so a later "consistency" refactor cannot quietly collapse it. |
| T-BD-I-006 | Confidence → `JobSecurityBand` → #30's decision is a **read**: #30 consuming the band mutates no #45 state (KD-3). |
| T-BD-I-007 | **The one-day-stale contract** (KD-7): a board delta committed on day *N* is the value observed at #33's slot-3 read on day *N+1*, not day *N*. Pinned as a test so a later slot reorder fails here rather than silently changing six specs' cited positions. |

## 5.7 Fail-loud (§2.3)

| ID | Test |
|---|---|
| T-BD-FAIL-001 | `default(OwnershipProfile)` at any consuming seam ⇒ throws (F4) — advance, projection, and encode each independently. |
| T-BD-FAIL-002 | An input or confidence outside `[0,1000]` ⇒ throws, never clamped silently (F1). |
| T-BD-FAIL-003 | Decode: wrong `BOARD_SAVE_FORMAT_VERSION` ⇒ throws (F3). |
| T-BD-FAIL-004 | Decode: an out-of-bounds / near-`int.MaxValue` length prefix ⇒ throws via the overflow-safe bound, never wraps (F5). |
| T-BD-FAIL-005 | Decode: trailing bytes ⇒ throws (F5). |
| T-BD-FAIL-006 | Decode: an undefined `OwnershipType` ordinal ⇒ throws (F2). |
| T-BD-FAIL-007 | `AdvanceBoardDay` for a `ClubId` with no entry ⇒ throws; state is **not** auto-created (F7). |
| T-BD-FAIL-008 | Inserting a **default-constructed `BoardConfidence`** ⇒ throws **at insertion** (F4a). The test must assert this at the *insertion* seam specifically: every field is in range, so no range check can catch it, and a `default` entry silently means `Critical` standing with a broken day-0 guard (FR-BD-005a). |
| T-BD-FAIL-009 | The pair is enforced: inserting a valid `BoardConfidence` with a `default(OwnershipProfile)`, or vice versa, ⇒ throws at insertion (FR-BD-005a). |

## 5.8 Structural (the boundaries #45 must not cross)

| ID | Test |
|---|---|
| T-BD-BOUND-001 | #45's assembly references **only** #27 and #16 — asserted from the assembly's reference set, so a future `using` of #30/#33/`SeasonSave`/`MatchEngine` fails the build's test gate (FR-BD-015, the #40 `T-FN-BOUND-002` posture). |
| T-BD-BOUND-002 | #45 exposes **no** member whose name or signature could serve as a sacking command, and publishes no event (KD-3). |
| T-BD-BOUND-003 | #45 declares no type named `BoardModifier` — #40's is consumed, not shadowed (FR-BD-017). |
| T-BD-BOUND-004 | **No foreign writes:** a `Squad`, `PlayerRecord`, `ClubFinances`, and `SeasonState` handed alongside every #45 entry point are **field-unchanged** after advance, projection, and save/restore. Asserted behaviourally — #27 is referenced, so its types *are* reachable and the reference graph cannot prove this (§4.7 standing item). |

## 5.9 Closed-loop scenario (#19 `ScenarioRunner`, T-phase)

One Simulation-layer scenario, `board-confidence-across-a-season`, owning specs `{16, 19, 27, 30, 40, 45}`,
registered under `SCENARIO_PATH_CROSS_SPEC_PREFIX`: run a season with a modelled managed club and
unmodelled AI clubs, save mid-season, restore, continue to the boundary roll, and assert that confidence,
the derived band, and **every club's** settled budget match an uninterrupted run. This is the
composition-level proof that KD-5's split, KD-7's cursor, and KD-6's blob hold **together** — which no
unit test exercises jointly, and which is exactly where the unmodelled-club asymmetry (§4.5) would fail
if it were wrong.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-25 | — | Initial §5 (identity, projection + daily-step units keyed to the §3.7 worked examples, determinism incl. the deep-tier position-independence lock, save/seam integration, fail-loud, structural boundary tests, the T-phase closed-loop scenario). Notably T-BD-U-004 locks the §3.2 direction property below neutral, where the rejected deviation-scaling formulation fails. Status IN REVIEW. |
| 0.2 | 2026-07-25 | — | PASS-1 fixes (L+M): T-BD-ID-005 scoped to **T0/T1** — at T2 the seam is live and the save gains #45's sub-blob, so the *save* is not byte-identical; KD-8's identity is about budgets and existing cursors, never the save frame. Added T-BD-FAIL-008/009 for the new FR-BD-005a / F4a insertion guard. |
#endregion
