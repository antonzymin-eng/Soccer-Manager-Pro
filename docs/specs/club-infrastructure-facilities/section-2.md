# Club Infrastructure & Facilities #53 — Section 2: Requirements, Data Structures, Failure Modes

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.2 — PASS-1 fix pass)
**Version:** 0.2
**Status:** APPROVED

---

## 2.1 Functional requirements

**Cadence & ownership**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-IN-001 | All #53 state MUST advance on the **world tick** (`WorldClock`, one day = one `worldTick`) — never the 10 Hz tactical or 60 Hz physics loops. No #53 type MUST be reachable from `MatchEngine.RunTick`. | MUST | KD-3 |
| FR-IN-002 | #53 MUST be the **sole writer** of per-`ClubId` facility state (`ClubFacilities`). No other assembly writes it. | MUST | KD-1 |
| FR-IN-003 | All #53 fields and formulas MUST be **integer**. **No float MUST appear anywhere in #53 at any tier** — including the projections, which are integer or integer per-mille. | MUST | KD-8 |
| FR-IN-004 | Every facility level MUST be clamped to `[FACILITY_LEVEL_MIN, FACILITY_LEVEL_MAX]` at every write, and an out-of-range level reaching a consuming seam MUST fail loud rather than be silently clamped (F1). | MUST | KD-2 |
| FR-IN-005 | #53 MUST hold **no currency value**, MUST expose no price, and MUST perform no budget check. Cost is #40's (F7 is not a #53 failure mode because #53 never sees money). | MUST | KD-1 |
| FR-IN-006 | State MUST be created via the `ClubFacilities.CreateBaseline()` factory. `default(ClubFacilities)` MUST NOT be treated as a valid runtime value. | MUST | §1.6 |
| FR-IN-006a | The **enforced** guard MUST be at **record insertion**, not only at the consuming seam. `default(ClubFacilities)` carries `InProgressFacility = 0`, which is a *valid `FacilityType` ordinal* (`TrainingGround`) and therefore reads as *"a training-ground build is in progress"* — a state no range check can catch. Insertion-time validation is what closes it (F4a). | MUST | §1.6 |

**The facility roster**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-IN-007 | `FacilityType` MUST be a **fixed** enum, **APPEND-only**, with contiguous ordinals from `0`. Ordinals are persisted; reordering MUST be treated as a breaking change requiring a `FACILITY_SAVE_FORMAT_VERSION` bump. | MUST | KD-2 |
| FR-IN-008 | The Stage-3 roster MUST be exactly `{ TrainingGround, YouthFacilities, MedicalCentre, Stadium }` — **one member per existing consumer dial**. A member with no consumer dial MUST NOT be declared (FR-LW-031). | MUST | KD-2 |
| FR-IN-009 | Every club MUST begin at the **uniform baseline** `FACILITY_LEVEL_BASELINE` for every facility. Genesis MUST NOT depend on the world seed, on club identity, or on any generator. | MUST | KD-2 |
| FR-IN-010 | Because of FR-IN-009, #53 MUST remain **outside `WORLD_GENERATION_VERSION`** (#50 KD-2). Any future seed-varied genesis MUST be an explicit promotion decision that enrols #53 in that version, never a silent default. | MUST | KD-2 |

**The upgrade lifecycle**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-IN-011 | `CanStartUpgrade(clubId, type, targetLevel) → bool` MUST be a **pure predicate**: it MUST mutate no #53 state, allocate nothing, and be safely callable any number of times with identical results. | MUST | KD-1 |
| FR-IN-012 | `StartUpgrade(clubId, type, targetLevel)` MUST be the **only** mutation that begins a build, and MUST be a **separate** entry point from FR-IN-011. A combined `TryStartUpgrade` MUST NOT be offered, because it cannot be sequenced correctly around #40's transaction without roll-back-on-failure. | MUST | KD-1 |
| FR-IN-013 | `StartUpgrade` MUST **re-validate** the same predicate and **fail loud** if it does not hold. The check→debit→latch sequence relies on nothing running in between; re-validation is what makes a broken premise loud rather than a build started from a stale check. | MUST | KD-1 |
| FR-IN-014 | A club MUST have **at most one** build in progress at Stage 3. `StartUpgrade` against a club already building MUST be refused by FR-IN-011 and MUST fail loud at FR-IN-013. | MUST | KD-3 |
| FR-IN-015 | An upgrade MUST store a **`CompletionWorldDay`** — an absolute world day computed once at latch time. A remaining-days counter, a per-day progress accumulator, or any other per-day mutation of build state MUST NOT be used. | MUST | KD-3 |
| FR-IN-016 | `targetLevel` MUST satisfy `currentLevel < targetLevel ≤ FACILITY_LEVEL_MAX`. A target at or below the current level MUST be refused (it is a no-op the caller should not have paid for). | MUST | KD-3 |
| FR-IN-017 | `AdvanceFacilityDay(clubId, worldDay)` MUST apply a completion when `worldDay >= CompletionWorldDay`, setting the level to `TargetLevel` and **clearing** the in-progress record in the same operation. | MUST | KD-3 |
| FR-IN-018 | The day advance MUST be **idempotent by construction**: re-invoking it for the same `worldDay` MUST be a no-op, and invoking it after a multi-day gap MUST complete any build whose completion day fell inside the gap. #53 MUST NOT carry a `LastAdvancedWorldDay` cursor and MUST NOT fail loud on a day gap. | MUST | KD-7 |
| FR-IN-019 | A refused start or a refused advance MUST mutate **nothing** — no level, no in-progress record. | MUST | KD-1 |

**The projections**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-IN-020 | #53 MUST project each facility level into the value type its consumer **already defines**, and MUST NOT declare a parallel dial type of its own. | MUST | KD-4 |
| FR-IN-021 | At `FACILITY_LEVEL_BASELINE` every projection MUST equal its consumer's identity **exactly** — `AcademyQuality.Neutral`, the zero training term, `MedicalModifier.Identity`. | MUST | KD-4 |
| FR-IN-022 | The projections MUST respect the **two identity conventions**: additive/zero-identity for `AcademyQuality` and the training term, multiplicative per-mille/1000-identity for `MedicalModifier`. A single unified convention MUST NOT be imposed. | MUST | KD-8 |
| FR-IN-023 | #53's projections MUST be **independent of staff state**. #53 MUST NOT accept, read, or pre-blend a #34 `CoachingModifier` / `MedicalModifier` / scout-quality value. The **composition root** combines. | MUST | KD-4 |
| FR-IN-024 | The training-ground term MUST be delivered as a root-assembled **input to #29's `ComputeTrainingInput`**, alongside #34's `CoachingModifier`. #53 MUST NOT produce a `TrainingInput` — #29 is its sole writer (FR-TR-005). | MUST | KD-9 |
| FR-IN-025 | `StadiumCapacity(clubId) → int` MUST be a read-only query returning an absolute capacity. It is **not** a deviation dial and carries no identity requirement while its consumer (#40 §7.2) is deferred. | MUST | KD-2 |
| FR-IN-026 | Every projection MUST be **pure** — no mutation, no allocation on the per-day path, and no dependence on call order. | MUST | KD-4 |

**Boundaries — what #53 must never do**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-IN-027 | #53's assembly MUST reference **only** `TacticalDirector.PlayerDatabase` (#27) and `TacticalDirector.DeterministicSim` (#16), at every tier. It MUST reference no consumer, not #40, not #30, not `SeasonSave`, and not `MatchEngine`. | MUST | KD-1 |
| FR-IN-028 | #53 MUST contain no logic that **decides** to upgrade — no AI, no heuristic, no autonomous spend. It applies validated commands only. | MUST | KD-1 |
| FR-IN-029 | #53 MUST write nothing in #27, #40, #30 or any consumer. It holds club-keyed state and projects values; it mutates no foreign type. | MUST | KD-1 |

**Determinism**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-IN-030 | #53 MUST be **draw-free**: no `RegisterStream`, no `DOMAIN_TAG_*`, no `SubsystemOrdinal`, at every tier declared here. It MUST NOT consume the roadmap §6 reserved slack (`0x2E`–`0x2F` / 96–97). | MUST | KD-6 |
| FR-IN-031 | Any future stochastic facility behaviour (build overruns, variable outcomes) MUST be an **explicit promotion of this spec** that takes `0x2E` / 96 on the record — never absorbed as an implementation detail. | MUST | KD-6 |

**Persistence**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-IN-032 | `FACILITY_SAVE_FORMAT_VERSION` [FIXED] = 1; #53's state MUST land as an **opaque, independently version-gated** sub-blob composed into #30's `SeasonSaveCodec` — **not** a `WORLD_STORE_FORMAT_VERSION` bump and **not** folded into #40's block. | MUST | KD-5 |
| FR-IN-033 | Every field MUST round-trip **field-identical**; **serialize, don't regenerate**. There is no RNG cursor to serialize (FR-IN-030) and no idempotency cursor (FR-IN-018). | MUST | KD-5 |
| FR-IN-034 | Restore MUST **fail loud** on version mismatch, on an out-of-bounds length prefix (overflow-safe bound compared against `total − offset`), on trailing bytes, and on any out-of-contract byte — an undefined `FacilityType` ordinal, a level outside its range, or an `InProgressFacility` that is neither `-1` nor a defined ordinal. | MUST | KD-5 |
| FR-IN-035 | The sub-blob MUST be **APPEND-only**: new fields go at the end with a `FACILITY_SAVE_FORMAT_VERSION` bump, never inserted mid-block. | MUST | KD-5 |

## 2.2 Data structures

```csharp
// Fixed, APPEND-only. Ordinals are PERSISTED — reordering re-points every saved club's
// facilities to the wrong building (FR-IN-007). One member per EXISTING consumer dial (FR-IN-008).
public enum FacilityType : byte
{
    TrainingGround   = 0,   // -> #29 ComputeTrainingInput (KD-9)
    YouthFacilities  = 1,   // -> #42 AcademyQuality
    MedicalCentre    = 2,   // -> #41 MedicalModifier
    Stadium          = 3,   // -> #40 matchday attendance (deferred, §7.2)
}

// #53-owned per-club world-tick state (serialized, KD-5). Integers only (FR-IN-003).
public struct ClubFacilities
{
    // Indexed by (int)FacilityType; each in [FACILITY_LEVEL_MIN, FACILITY_LEVEL_MAX].
    public int[] Levels;                  // length == FACILITY_TYPE_COUNT

    // At most one build (FR-IN-014). FACILITY_NONE_SENTINEL (-1) == idle.
    // NOT uint.MaxValue on CompletionWorldDay: that is a legal computed day (§1.6 item 3).
    public int  InProgressFacility;       // -1, or a defined FacilityType ordinal
    public int  TargetLevel;              // meaningful only while InProgressFacility != -1
    public uint CompletionWorldDay;       // absolute day, computed once at latch (FR-IN-015)

    public static ClubFacilities CreateBaseline() => new() {
        Levels             = FilledWith(FACILITY_LEVEL_BASELINE, FACILITY_TYPE_COUNT),
        InProgressFacility = FACILITY_NONE_SENTINEL,
        TargetLevel        = 0,
        CompletionWorldDay = 0 };         // never default(ClubFacilities) — FR-IN-006/006a
}

// Read-only value copies for #38 (KD-4 observer posture). Allocated only when asked, off the tick.
public readonly struct FacilityViewModel
{
    public readonly int  TrainingGroundLevel, YouthFacilitiesLevel, MedicalCentreLevel, StadiumLevel;
    public readonly int  StadiumCapacity;
    public readonly int  InProgressFacility;      // -1 when idle
    public readonly uint CompletionWorldDay;      // meaningful only when building
}
```

**Types #53 consumes but does **not** declare** — re-declaring any of them would be the parallel-surface
trap #1.1 exists to prevent:

| Type | Owner | #53's use |
|---|---|---|
| `AcademyQuality` | #42 §2.2 | returned by `ProjectAcademyQuality` |
| `MedicalModifier` | #41 §2.2 | returned by `ProjectMedicalModifier` |
| `TrainingInput` | #28 §2.2, written by #29 | **never returned by #53** (KD-9 / FR-IN-024) |
| `ClubId` | #27 | keying identity |

## 2.3 Failure modes

| ID | Condition | Response |
|---|---|---|
| **F1** | A facility level outside `[FACILITY_LEVEL_MIN, FACILITY_LEVEL_MAX]` at any seam. | **Fail loud** (`ArgumentOutOfRangeException`) — never silently clamped. |
| **F2** | An undefined `FacilityType` ordinal, or an `InProgressFacility` that is neither `FACILITY_NONE_SENTINEL` nor a defined ordinal. | **Fail loud** at the seam and at deserialize (the `MatchSaveCodec` posture). |
| **F3** | Bad `FACILITY_SAVE_FORMAT_VERSION` on restore. | **Fail loud** — the version gate is read **first**, before any field below it is interpreted. |
| **F4** | `default(ClubFacilities)` reaching a consuming seam — a null `Levels` array, or every level `0` (below `FACILITY_LEVEL_MIN`). | **Fail loud**. The level half is caught by F1; the array-shape half by an explicit length check. |
| **F4a** | A **default-constructed `ClubFacilities` inserted into the store**. Its `InProgressFacility = 0` is a *valid ordinal* — `TrainingGround` — so it reads as *"a build is in progress"*, with `TargetLevel = 0` and `CompletionWorldDay = 0`, meaning the next advance would "complete" a build by setting the training ground to level `0`. **No range check on the ordinal can catch this**, because the value is in range. | **Fail loud at insertion** (FR-IN-006a) — the #45 F4a / #33 FR-HS-005 posture: the enforced guard is insertion, not the advance path. |
| **F5** | Out-of-bounds length prefix or trailing bytes in the sub-blob. | **Fail loud**, via an overflow-safe bound compared against `total − offset` — never `offset + need`, which can wrap negative on a crafted near-`int.MaxValue` prefix. |
| **F6** | `StartUpgrade` whose predicate no longer holds — already building, target ≤ current, target above `FACILITY_LEVEL_MAX`, unknown facility. | **Fail loud** (FR-IN-013). This is the re-validation that makes a stale check visible instead of silently starting a wrong build. |
| **F7** | An advance, projection, or upgrade for a `ClubId` with **no #53 entry**. | **Advance and `StartUpgrade` fail loud** — a bootstrap bug; state is never auto-created (the #40 FR-FN-025 posture). **Projections return the consumer's identity** for an unmodelled club, so the root can assemble a dial for every club without special-casing. The asymmetry is deliberate and is locked (§5.6): #53 models a subset of clubs, but the root assembles dials for all of them. |
| **F8** | A `Levels` array whose length ≠ `FACILITY_TYPE_COUNT` on restore — the shape a roster append would produce against an un-bumped version. | **Fail loud** at deserialize. This is what makes FR-IN-035's APPEND-only rule enforceable rather than aspirational. |

**Deliberately not a failure mode: a day gap.** Advancing from day *N* to day *N+30* in one call is
**legal and correct** (FR-IN-018 / KD-7). It is listed here by its absence because four sibling specs do
fail loud on exactly that, and a reviewer scanning for the guard should find the reason rather than the
omission.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §2 (FR-IN-001..030, data structures, F1..F6) from supplement v0.4. Status IN REVIEW. |
| 0.2 | 2026-07-27 | — | PASS-1 fixes. **M:** added **FR-IN-013** (`StartUpgrade` must **re-validate**) — without it the split surface's safety rests on an unstated "nothing runs in between" premise, and a broken premise would start a build from a stale check silently; **F6** added to match. **M:** added **F4a** — `default(ClubFacilities).InProgressFacility == 0` is a *valid* `FacilityType` ordinal, so the default state reads as a live training-ground build that the next advance would "complete" at level `0`; no range check can catch it, so the guard is at insertion (FR-IN-006a). **M:** added **FR-IN-024** (training term feeds #29, not #28 — FR-TR-005 makes #29 the sole `TrainingInput` writer) and **FR-IN-022** (the two identity conventions). **L:** added **F8** (a `Levels` length mismatch, which is what makes APPEND-only enforceable), **FR-IN-025** (capacity is absolute, not a dial), and the explicit *"a day gap is not a failure mode"* note, since four sibling specs guard exactly that and its absence would otherwise read as an oversight. |
#endregion
