# Club Infrastructure & Facilities #53 — Section 4: Architecture

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.2
**Status:** IN REVIEW

---

## 4.1 Assembly and reference direction

New assembly **`TacticalDirector.ClubInfrastructure`** at `src/club-infrastructure/`, referencing
**only** `TacticalDirector.PlayerDatabase` (#27, for `ClubId`) and `TacticalDirector.DeterministicSim`
(#16, for `CanonicalSerializer`).

```
root ──▶ #30 Season Loop ──▶ #53 Club Infrastructure ──▶ {#27, #16}
  │                                    │
  ├──▶ #40 Finances                    └──▶ (values only) ──▶ root ──▶ {#42, #29, #41}
  └──▶ command layer ──▶ {#53 check, #40 debit, #53 latch}
```

**#53 is a leaf, at every tier** (FR-IN-027). It references no consumer — the consumers own the dial
types and the root maps — and it does **not** reference #40, because KD-1 puts the purchase sequence in
the command layer. This is the wave's established pattern (#48's cue sink, #50's generator registry,
#51's cue mapping), and it is what lets a *new* candidate join a mature graph without any existing spec
gaining a reference.

**Note the one thing #16 is used for.** `CanonicalSerializer` and nothing else — specifically **not**
`DeterministicRngService`, which #53 must not even be able to reach (KD-6). The reference is retained
rather than dropped because the codec genuinely needs it; §5.8 asserts the *absence of a draw* directly
rather than trying to prove it from the reference graph, which cannot.

**CS0104 pre-check.** #53 introduces `FacilityType`, `ClubFacilities`, `FacilityStore`,
`FacilityProjection`, `FacilitySaveCodec`, `FacilityViewModel`. Each was checked against every type name
in an assembly that could be in scope with it before authoring, because this project has hit CS0104
twice (`TacticTranslation`, `PlayerAttributes`). None collides. `AcademyQuality`, `MedicalModifier` and
`TrainingInput` are **consumed, never re-declared** (FR-IN-020) — a parallel declaration of any of them
would be the exact second-source defect KD-4 forbids, and would compile.

## 4.2 File layout

```
src/club-infrastructure/
├── ClubInfrastructureConstants.cs   # the Appendix A catalogue — no magic numbers in formula code
├── FacilityType.cs                  # the fixed APPEND-only roster (KD-2)
├── ClubFacilities.cs                # per-club state + CreateBaseline()
├── FacilityStore.cs                 # per-ClubId store; the SINGLE writer; Try* accessors; insertion guard
├── FacilityUpgrade.cs               # FM-IN-01/02 — CanStartUpgrade (pure) + StartUpgrade (latch)
├── FacilityDayAdvance.cs            # FM-IN-03 — the day advance
├── FacilityProjection.cs            # FM-IN-04/05 — pure projections into the consumers' own types
├── FacilitySaveCodec.cs             # KD-5 sub-blob, version gate first
├── FacilityViewModel.cs             # read-only value copies for #38
└── tests/
```

**`CanStartUpgrade` and `StartUpgrade` share a file but not an entry point.** They are co-located because
the latch re-runs the predicate (FR-IN-013) and the two must not drift; they stay separate public methods
because merging them is exactly what KD-1 forbids.

## 4.3 The command-layer purchase sequence (KD-1)

The one sequence in this spec that spans three layers, written out because getting its order wrong is
unrecoverable for the player:

```
# in the command layer — NOT in #53 and NOT in #40
OnUpgradeFacilityCommand(clubId, type, targetLevel):
    if (!facilities.CanStartUpgrade(clubId, type, targetLevel))   return Refused;   # 1. pure check
    cost = PriceOf(type, targetLevel);                                              # 2. the command
                                                                                    #    layer's price
    if (!finances.TryApplyTransaction(clubId, -cost))             return Refused;   # 3. #40 debits
    facilities.StartUpgrade(clubId, type, targetLevel);                             # 4. #53 latches
    return Started;
```

**Neither spec references the other**, and neither holds the other's quantity: #53 has no `cost`, #40 has
no `level`. This is #40's established relationship with #31 applied to construction.

**Why the order cannot be relaxed.** Debit-then-latch-then-refuse leaves the player charged for a build
that never started, with no recovery path — and roll-back-on-failure is the pattern #50 KD-4 identifies
as the one that loses data precisely when the roll-back is what fails. Check-then-debit-then-latch has
only one failure mode left, and it is benign: a refused debit after a successful check leaves everything
untouched.

**Where the price lives is a stated gap, not an oversight.** `PriceOf` is **not** #53's (FR-IN-005: no
currency) and **not** #40's (#40 owns the ledger, not a facility price list). It belongs to the layer
that owns the command, and at Stage 3 that is a `[GT]` table beside the command handler. Recorded here
because an unstated third quantity is how a fourth spec ends up owning a price list nobody assigned.

**The premise the two-step rests on** is that nothing mutates #53 between steps 1 and 4: one command,
inside one world tick, with no interleaved advance. FR-IN-013's re-validation is what converts a broken
premise from a silent wrong build into a loud failure — so the premise is defended, not merely assumed.

## 4.4 The #30 seam (the caller side)

#30 owns the call; #53 owns the state. At its tick-order slot (ERR-030-020):

```
# inside #30's RunWorldTickInFixedOrder()
facilities.AdvanceFacilityDay(clubId, worldDay)      # per club #53 models
```

**Slot placement has a real constraint**, which is why it is filed at approval rather than decided later:
#53's slot MUST sit **before** any step that reads a facility-derived input on the same day. Otherwise a
build completing on day *N* would not reach its consumers until day *N+1* — a one-day lag that is
harmless in itself but would be **unstated**, and an unstated lag is what a later maintainer "fixes" by
reordering a pinned sequence six specs cite by number. #53 takes the earliest free slot and the ordering
is pinned by test (§5.6).

This mirrors the two precedents exactly: #41's `AdvanceMedicalDay` and #45's board seam
(`ERR-030-008`, which renumbered `AdvanceDay` 8 → 9) both landed as filed insertions at approval.

**Which clubs advance.** #30 advances the clubs #53 models. A club with no entry **fails loud** on
advance — a bootstrap bug, never auto-created (F7, the #40 FR-FN-025 posture).

## 4.5 The projection seams (the consumer side, KD-4)

Every consumer receives a **root-assembled value**. #53 supplies one term; the root combines it with
#34's, if any:

```
# in the composition root — the ONLY place #53's term meets #34's
var academy  = Combine(staff.ProjectAcademyTerm(clubId),   facilities.ProjectAcademyQuality(clubId));
var medical  = Combine(staff.ProjectMedicalModifier(clubId), facilities.ProjectMedicalModifier(clubId));
var trainTerm =        facilities.ProjectTrainingTerm(clubId);          // a #29 INPUT (KD-9)

academyEngine.AdvanceAcademyDay(state, worldDay, academy, ...);          // #42 sees ONE dial
medicalEngine.AdvanceMedicalDay(state, medical, worldDay, rng);          // #41 sees ONE modifier
trainingEngine.ComputeTrainingInput(coachingModifier, trainTerm, ...);   // #29 folds BOTH into
                                                                         //     the one TrainingInput
```

**The combination point is the root, and it has to be.** #41 takes a single already-assembled
`MedicalModifier`; #42 takes a single already-assembled `AcademyQuality`. Neither consumer *sees* two
sources, so neither *can* own the combination — a point the design supplement stated the other way round
and which this section corrects (§9.4.1 M-3). #29 is the one exception, and only because #29 is itself a
producer: it is the sole writer of `TrainingInput` (FR-TR-005), so its two inputs are folded inside #29,
not before it (KD-9).

**What `Combine` must not become.** It composes two independent terms; it is not a place to add a third
model. If a future spec produces a third term for the same dial, it is added here as a term — not blended
into #53's or #34's projection, which is how double-counting gets built by a well-meaning producer
(FR-IN-023, locked by §5.7).

## 4.6 Save composition (KD-5)

#53's sub-blob is composed into #30's `SeasonSaveCodec` alongside #40's, #33's, #41's and the rest, as a
length-prefixed **opaque** block: the outer codec never parses it, so `FACILITY_SAVE_FORMAT_VERSION` and
`SEASON_SAVE_FORMAT_VERSION` move independently (FR-IN-032). Layout in Appendix B.

**Two versions are in play and neither implies the other** — #53's `FACILITY_SAVE_FORMAT_VERSION` and the
outer `SEASON_SAVE_FORMAT_VERSION` (bumped at T2, when the sub-blob is first composed in). Unlike #45,
#53 changes **no** existing spec's serialized representation, so `SEASON_STATE_FORMAT_VERSION` is
untouched — #53's landing is purely additive to the save layer.

**Migration posture at T2: none — pre-T2 saves are rejected fail-loud.** T2 bumps the outer version, so a
save written before it is not loadable after it. #53 defines **no** migration path and **no** silent
upgrade: the version gate rejects it with a clear error, matching the living-world slice-2 precedent
(*"v2 payloads rejected fail-loud, no migration"*). Cross-version save migration is **#50's** subject;
recording the posture here means #50 inherits a stated position rather than discovering an assumption.

**Why not fold into #40's block.** Rejected explicitly: it would make #40's codec parse state #40 does not
own, recreating in the save layer exactly the ownership confusion §1.1 documents in the spec layer. The
cost of the alternative — a twenty-sixth format version and a row in #50's registry bookkeeping — is
acknowledged in KD-5 and accepted.

## 4.7 Contracts with neighbours

| Neighbour | Contract |
|---|---|
| **#30** | Invokes `AdvanceFacilityDay` at its pinned slot. **Sole caller** of the advance. #53 references #30 never. |
| **#40** | **No reference in either direction.** #40 debits through its own `ApplyTransaction`, sequenced by the command layer (§4.3). #40's deferred matchday accrual reads `StadiumCapacity` at its own T3. |
| **#42** | Receives `AcademyQuality` — **#42's own type**, root-assembled. #42's shape, `Neutral` identity and root-assembly pattern are all unchanged; #53 is a pointer fix to who produces the input, not a design change. |
| **#29** | Receives the training term as a root-assembled **input parameter**, alongside #34's `CoachingModifier`. #29 stays the sole writer of `TrainingInput` (KD-9 / ERR-029-001). |
| **#41** | Receives a `MedicalModifier` — **#41's own type**, root-assembled, always via the `Identity` factory shape and never `default()` (§3.4). |
| **#34** | **No reference in either direction.** Both project independent terms into the same dials; the root combines. #53 never reads staff state (FR-IN-023). |
| **#27** | `ClubId` only. #27's assembly stays schema-untouched — #53 adds no field to `Squad` or `PlayerRecord`. |
| **#38** | Reads `FacilityViewModel` — value copies, never a live handle (the `FinancesViewModel` / `MatchEngine.BallView` observer-neutral posture). |
| **#16** | `CanonicalSerializer` **only**. Not `DeterministicRngService` (KD-6). |
| **#50** | Registers `FACILITY_SAVE_FORMAT_VERSION` in the version registry; #53 is **outside** `WORLD_GENERATION_VERSION` while KD-2's uniform genesis holds. |

**Standing review item:** #53 performs **no** write to `Squad`, `PlayerRecord`, `ClubFinances`,
`SeasonState`, `AcademyState`, or any #34 type. This cannot be asserted from the reference graph alone —
#27 is referenced, so its types are reachable — so it is asserted **behaviourally** in §5.8 and re-checked
at each review.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §4 (leaf assembly + DAG with the CS0104 pre-check, file layout, the command-layer purchase sequence, the #30 slot seam, the root-assembled projection seams, save composition, neighbour contracts + the no-foreign-write standing item). Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 fixes. **M:** §4.5 corrected — the combination point is the **composition root**, not the consumer, because #41 and #42 each take a *single* already-assembled dial and therefore cannot own a combination; #29 is the one exception and only because it is itself the sole writer of `TrainingInput` (KD-9). **M:** §4.3 now names **where the price lives** — neither #53 (no currency) nor #40 (no facility price list), so it is the command layer's `[GT]` table; an unstated third quantity is how a fourth spec ends up owning a price list nobody assigned. **L:** §4.4 gained the *reason* the slot must precede same-day consumers (an unstated one-day lag is what a later reorder "fixes"); §4.1 gained the note that #16 is referenced for `CanonicalSerializer` **only**, with the draw-absence asserted behaviourally since the reference graph cannot prove it; §4.6 gained the explicit contrast with #45 (#53 is purely additive to the save layer). |
#endregion
