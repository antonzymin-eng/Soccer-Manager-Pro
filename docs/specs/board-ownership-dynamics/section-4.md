# Board & Ownership Dynamics #45 — Section 4: Architecture

**Created:** July 25, 2026
**Last Updated:** July 25, 2026 (v0.2 — section-file PASS-1 fix pass)
**Version:** 0.2
**Status:** APPROVED

---

## 4.1 Assembly and reference direction

New assembly **`TacticalDirector.BoardOwnership`** at `src/board-ownership/`, referencing **only**
`TacticalDirector.PlayerDatabase` (#27, for `ClubId`) and `TacticalDirector.DeterministicSim` (#16, for
`CanonicalSerializer` and — at the deep tier — `DeterministicRngService`).

```
root ──▶ #30 Season Loop ──▶ #45 Board & Ownership ──▶ {#27, #16}
                │                      │
                └──▶ #40 Finances ◀────┘   (#45 → #40: the BoardModifier producer)
```

**Acyclic at every tier.** #45 references neither #30, #33, `living-world`, `SeasonSave`, nor
`MatchEngine` (FR-BD-015). The deep-tier #33 morale input arrives as a routed integer, not a reference
(FR-BD-016), so this property is unconditional and testable by reference-absence rather than by review
vigilance.

**CS0104 pre-check.** #45 introduces `OwnershipProfile`, `BoardConfidence`, `BoardDayInput`,
`TakeoverState`, `OwnershipType`, `JobSecurityBand`. None collides with an existing type name in an
assembly that could be in scope with it — checked before authoring, because the project has hit CS0104
twice (`TacticTranslation`, `PlayerAttributes`). `BoardModifier` is **#40's** type, consumed not
re-declared (FR-BD-017 — a parallel declaration would be exactly the second budget-multiplier path #40 §7
forbids).

## 4.2 File layout

```
src/board-ownership/
├── BoardOwnershipConstants.cs      # the Appendix A catalogue — no magic numbers in formula code
├── BoardConfidence.cs              # per-club state + Create()
├── OwnershipProfile.cs             # dials + Identity factory + OwnershipType
├── BoardDayInput.cs                # committed-values input + Neutral
├── TakeoverState.cs                # deep-tier state, zero-valued and serialized at minimal
├── JobSecurityBand.cs              # the KD-5 derived band
├── BoardStore.cs                   # per-ClubId store; the single writer; Try* accessors
├── BoardProjection.cs              # FM-BD-02/03/04 — pure projections
├── BoardDayAdvance.cs              # FM-BD-01 — the daily step
├── TakeoverEngine.cs               # FM-BD-05 — deep tier ONLY; absent at minimal (FR-LW-031)
├── BoardSaveCodec.cs               # KD-6 sub-blob, version gate first
├── BoardViewModel.cs               # read-only value copies for #38
└── tests/
```

`TakeoverEngine.cs` is listed for the deep tier and **is not created at T2** — an empty engine with no
draw site is the phantom surface FR-LW-031 forbids.

## 4.3 The `OwnershipProfile` seam

`OwnershipProfile` is a `readonly struct` of integer dials with an **explicit `Identity` factory**. The
zero-value trap is closed by construction: `default(OwnershipProfile)` has every dial at `0` (a ×0
multiplier), which is not merely wrong but *silently* wrong — it would zero a club's budget contribution
and flatten its target assembly. It **fails loud** at every consuming seam (F4).

This is #40's `BoardModifier` and #41's `MedicalModifier` lesson applied at authoring time. #40 §1.6
records having done the same deliberately; #45 follows that precedent rather than re-learning it.

## 4.4 The #30 seam (the caller side)

#30 owns the call; #45 owns the state. At tick-order slot **8**:

```
# inside #30's RunWorldTickInFixedOrder(), step 8
board.AdvanceBoardDay(clubId, own, input, worldDay)      # per club #45 models
```

**Provenance is enforced at #30's call seam**, not inside #45: #45 cannot verify that
`input.ObjectiveTrackPermille` really came from the current table, only that it is in range. This is the
same division of responsibility #33 uses for `HumanSystemsDayInput`, and it is what keeps #45 free of a
#30 reference.

**Which clubs advance.** #30 advances the clubs #45 models (the managed club at minimal, §2.2). A club
with no entry **fails loud** on advance — a bootstrap bug, never auto-created (F7, the #40 FR-FN-025
posture).

## 4.5 The #40 seam (the consumer side)

At #30's boundary-roll step (b'), #30 settles finances **per club** — including every club #45 does not
model. The seam is therefore a `Try` (FR-BD-018):

```
# inside #30's RollToNextSeason(), step (b')
var mod = board.TryProjectBoardModifier(clubId, out var m) ? m : BoardModifier.Identity;
SettleFinances(financeState[clubId], position, clubCount, mod);
```

The asymmetry with §4.4 is deliberate and worth naming: **advance** is for clubs #45 owns (absence is a
bug), **projection** is asked for every club in the league (absence is normal). Collapsing them into one
posture would either spam exceptions for every AI club or hide a genuine bootstrap failure.

#40 needs **no change** — FR-FN-018/019/027 already define `BoardModifier`, its identity, its fail-loud
default, and the `#45 → #40` direction (§8.1).

## 4.6 Save composition (KD-6)

#45's sub-blob is composed into #30's `SeasonSaveCodec` alongside #40's and #33's, as a
length-prefixed **opaque** block: the outer codec never parses it, so `BOARD_SAVE_FORMAT_VERSION` and
`SEASON_SAVE_FORMAT_VERSION` move independently (FR-BD-027). Layout in Appendix B.

Three versions are in play and none implies the others — #45's `BOARD_SAVE_FORMAT_VERSION`, #30's
`SEASON_STATE_FORMAT_VERSION` (which ERR-030-009 bumps at T2 for the `JobSecurity` representation
change), and the outer `SEASON_SAVE_FORMAT_VERSION` (bumped at T2 when the sub-blob is first composed
in). Stated explicitly because conflating them is how a save-format change becomes an unbootable save.

**Migration posture at T2: none — pre-T2 saves are rejected fail-loud.** T2 bumps two versions at once
(the outer frame gains a sub-blob; #30's season block changes `JobSecurity`'s representation), so a save
written before T2 is **not loadable** after it. #45 defines **no** migration path and **no** silent
upgrade: the version gates reject it with a clear error, matching the living-world slice-2 precedent
(*"v2 payloads rejected fail-loud, no migration"*) and the project's blanket refusal to interpret bytes
under a version it does not recognise. Cross-version save migration is **#50's** subject, not #45's;
recording the posture here means #50 inherits a stated position rather than discovering an assumption.

## 4.7 Contracts with neighbours

| Neighbour | Contract |
|---|---|
| **#30** | Invokes the daily advance (slot 8) and the boundary projection (step b'); routes committed inputs; reads confidence for job security; **sole writer** of `BoardObjective`. #45 references #30 never. |
| **#40** | Consumes `BoardModifier` at `SettleFinances`. #45 adds no second multiplier path. |
| **#27** | `ClubId` only. #27's assembly stays schema-untouched — #45 adds no field to `Squad` or `PlayerRecord`. |
| **#33** | Deep tier only, and only as **routed values** in both directions (a morale signal in, a board delta out). No reference either way; #33's FR-HS-024 anticipates the read as deferred. |
| **#38** | Reads `BoardViewModel` — value copies, never a live handle (the `FinancesViewModel` / `MatchEngine.BallView` observer-neutral posture). |
| **#16** | `CanonicalSerializer`; the reserved namespace slot; the deep-tier RNG service. |

**Standing review item:** #45 performs **no** write to `Squad`, `PlayerRecord`, `ClubFinances`, or
`SeasonState`. This cannot be asserted from the reference graph alone — #27 is referenced, so its types
are reachable — so it is asserted behaviourally in §5 and re-checked at each review.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-25 | — | Initial §4 (assembly + acyclic DAG with the CS0104 pre-check, file layout with the deferred `TakeoverEngine`, the `OwnershipProfile` zero-value seam, the #30 advance seam and #40 `Try` projection seam with their deliberate asymmetry, three-independent-versions save composition, neighbour contracts + the no-foreign-write standing item). Status IN REVIEW. |
| 0.2 | 2026-07-25 | — | PASS-1 fix (M): §4.6 gained the **T2 migration posture** — two version bumps land at once, so pre-T2 saves are **rejected fail-loud with no migration** (the living-world slice-2 precedent); cross-version migration is #50's subject, and stating the position here means #50 inherits it rather than discovering an assumption. |
#endregion
