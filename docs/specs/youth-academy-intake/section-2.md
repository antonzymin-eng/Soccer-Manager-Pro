# Youth Academy & Intake #42 — Section 2: Functional Requirements & Data Structures

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.1 — initial)
**Version:** 0.1
**Status:** IN REVIEW

---

## 2.1 Functional requirements (FR-YA-001..028)

### Generation reuse (KD-1 / KD-2)

| ID | Requirement | Level | KD |
|---|---|---|---|
| FR-YA-001 | Cohort generation MUST be performed by calling **#28's `RegenGenerator.GenerateRegen`** with a #42-owned `streamIndex`. #42 MUST NOT fork, copy, or wrap the generator's draw sequence, and MUST NOT require any change to #28. | MUST | KD-1 |
| FR-YA-002 | The per-prospect draw budget MUST be exactly #28's `PROGRESSION_REGEN_FIELDS`. #42 MUST introduce **no draw of its own**. | MUST | KD-1 |
| FR-YA-003 | Academy quality MUST be applied **after** generation, as a pure transform over the returned `(PlayerRecord, PlayerLifecycle)` pair — never as a parameter threaded into the generator. | MUST | KD-1 |
| FR-YA-004 | The quality transform MUST modify **`PlayerLifecycle.PotentialAbility` only**. It MUST NOT modify `CurrentAbility` (a derived cache of `AbilityModel.ComputeCA`) or any attribute in `PlayerAttributes`. | MUST | KD-2 |
| FR-YA-005 | The shifted `PotentialAbility` MUST be clamped into `[paFloor, ABILITY_MAX]` where `paFloor = max(PA_MIN, min(CurrentAbility + REGEN_PA_HEADROOM, ABILITY_MAX))` — **`RegenGenerator`'s own generation floor, reproduced verbatim** — so every prospect satisfies the generator's "room to grow" postcondition. | MUST | KD-2 |
| FR-YA-006 | A `CeilingShiftPerMille` of `0` MUST leave the pair **byte-identical** (an early return, not an arithmetic no-op that could round). | MUST | KD-2 |
| FR-YA-007 | The age re-anchor (KD-2b) MUST update `PlayerRecord.Age` **and** `PlayerLifecycle.BirthWorldDay` together, deriving `BirthWorldDay` by #28's own formula from the same `worldDay`; it MUST NOT update one alone. | MUST | KD-2b |
| FR-YA-008 | At the minimal tier the intake age band MUST be exactly `[REGEN_AGE_MIN, REGEN_AGE_MAX]`, making the re-anchor a no-op. Any narrowed / bio-banded band is deep-tier and MUST be `[GT]`. | MUST | KD-2b |

### The quality input (KD-3)

| ID | Requirement | Level | KD |
|---|---|---|---|
| FR-YA-009 | `AcademyQuality` MUST be a #42-owned value type supplied **by the caller**. #42 MUST NOT reference #34 or #40, and MUST NOT define an interface for either (FR-LW-031). | MUST | KD-3 |
| FR-YA-010 | `default(AcademyQuality)` MUST equal `AcademyQuality.Neutral`, and `Neutral` MUST be the exact identity for every transform. | MUST | KD-3 |
| FR-YA-011 | An `AcademyQuality` whose dials fall outside their declared bounds MUST **fail loud** at the consuming seam (F2), never be clamped silently. | MUST | KD-3 |
| FR-YA-012 | The quality dial MUST modulate the **intake ceiling only**. #42 MUST NOT expose or apply any per-day growth modifier — that path belongs to #29 → #28 (F7). | MUST | KD-2 |

### The intake trigger (KD-4)

| ID | Requirement | Level | KD |
|---|---|---|---|
| FR-YA-013 | The intake MUST be driven from #30's day-advance tick order at the pre-declared **academy seam** (ERR-030-007), and MUST NOT read #30's calendar, fixtures, table, or board. | MUST | KD-4 |
| FR-YA-014 | The trigger MUST be `currentWorldDay ≥ LastIntakeWorldDay + ACADEMY_INTAKE_PERIOD_DAYS`, evaluated against **serialized** state. | MUST | KD-4 |
| FR-YA-015 | `LastIntakeWorldDay` MUST be serialized, and MUST be accompanied by an explicit genesis sentinel (`HasIntaken` flag or a reserved sentinel value) — world day `0` is a legal day and MUST NOT be overloaded to mean "never". | MUST | KD-4 |
| FR-YA-016 | A save taken on an intake day, restored, and advanced MUST produce **exactly one** cohort for that day. | MUST | KD-4 |
| FR-YA-017 | The intake MUST be a no-op (and MUST consume no draw) on any day the trigger does not fire. | MUST | KD-4 |

### Determinism & the RNG stream (KD-7)

| ID | Requirement | Level | KD |
|---|---|---|---|
| FR-YA-018 | #42 MUST register exactly one `youth.intake` stream **per club that runs an academy**, lazily at that club's first intake — never earlier (a stream with zero draw sites is the phantom surface FR-LW-031 forbids). | MUST | KD-7 |
| FR-YA-019 | Immediately before each intake the stream MUST be re-anchored to `DeriveActionOrdinal(clubId, intakeWorldDay, DRAW_PURPOSE_INTAKE)`, so a cohort is a pure function of `(worldSeed, clubId, intakeWorldDay)` and is **independent of how many draws any prior intake consumed**. | MUST | KD-7 |
| FR-YA-020 | #42 MUST NOT serialize an RNG cursor. | MUST | KD-7 |
| FR-YA-021 | At the minimal tier only the **managed club** MUST run an academy; world-wide academies are deferred with the global sim (and are gated on the shared `MaxRngStreams` bound, §7). | MUST | KD-7 |
| FR-YA-022 | All #42 arithmetic MUST be integer; no `float` MUST appear in any #42 formula, field, or serialized value. | MUST | §1.5 |

### The academy roster & promotion (KD-5)

| ID | Requirement | Level | KD |
|---|---|---|---|
| FR-YA-023 | The academy roster MUST be #42-owned state. #42 MUST NOT write to a #27 `Squad` under any circumstance. | MUST | KD-5 |
| FR-YA-024 | `Promote` MUST emit a `PromotionResult` the composition root applies; the prospect MUST be removed from the academy roster and inserted into the senior squad **atomically by the root** (never half-applied). | MUST | KD-5 |
| FR-YA-025 | A promotion into a senior squad already at `CLUB_SQUAD_SIZE` MUST be **refused** (F5), never silently dropped or over-filled. | MUST | KD-5 |
| FR-YA-026 | A prospect's `PlayerId` MUST be unchanged across promotion, and MUST never be reused after the prospect leaves the academy. | MUST | KD-5 |
| FR-YA-027 | Prospect `PlayerId`s MUST come from a serialized monotonic high-water allocator, and MUST NOT collide with #28's regen allocator — the reconciliation is a composition-root contract (§4.5). | MUST | KD-5 |

### Persistence (KD-6)

| ID | Requirement | Level | KD |
|---|---|---|---|
| FR-YA-028 | Academy state MUST be persisted as **one opaque, independently version-gated sub-blob** (`ACADEMY_SAVE_FORMAT_VERSION`) composed into `SeasonSaveCodec`, which MUST NOT parse it. #42 MUST NOT bump `WORLD_STORE_FORMAT_VERSION`. | MUST | KD-6 |

## 2.2 Data structures

```csharp
// The KD-3 caller-supplied quality input. All-zero == Neutral, by construction.
public readonly struct AcademyQuality
{
    public readonly int CeilingShiftPerMille;   // [-ACADEMY_CEILING_SHIFT_ABS_MAX, +ACADEMY_CEILING_SHIFT_ABS_MAX]
    public readonly int CohortSizeDelta;        // deep-tier; 0 at minimal
    public static AcademyQuality Neutral => default;   // FR-YA-010
}

// One prospect on the academy roster.
public readonly struct YouthProspect
{
    public readonly PlayerRecord Record;        // #27 shape (KD-1 output)
    public readonly PlayerLifecycle Life;       // #28 overlay (carries the KD-2-shifted PA)
    public readonly uint IntakeWorldDay;        // provenance; also the KD-7 anchor key of its cohort
    public readonly int ContractState;          // deep-tier; 0 == none at minimal
}

// Per-club academy state — the serialized surface (KD-6).
public sealed class AcademyState
{
    public int   ClubId;
    public bool  HasIntaken;                    // FR-YA-015 genesis sentinel
    public uint  LastIntakeWorldDay;            // FR-YA-014 latch
    public int   NextYouthPlayerId;             // FR-YA-027 monotonic high-water
    public AcademyQuality LastAppliedQuality;   // provenance only; never re-applied
    public YouthProspect[] Cohort;              // the academy roster
    // NOTE: no RngStreamState / cursor — FR-YA-020.
}

public readonly struct IntakeResult    { /* the generated prospects + their fresh PlayerIds, per club */ }
public readonly struct PromotionResult { /* the promoted PlayerRecord + PlayerLifecycle + its academy slot */ }
public readonly struct AcademyViewModel{ /* read-only cohort + quality summary for #38 */ }
```

## 2.3 Failure modes

| ID | Condition | Handling |
|---|---|---|
| **F1** | A `PlayerLifecycle` reaching a #42 seam violates `PotentialAbility ≥ CurrentAbility`, or a shifted PA falls outside `[paFloor, ABILITY_MAX]` | **Fail loud** — an invalid combination is a bug, never silently clamped or repaired (the #27/#28/#41 F1-class precedent). |
| **F2** | `AcademyQuality` carries an out-of-bounds dial | **Fail loud** at the consuming seam — magnitude validity is a caller-contract bug, never defaulted (the #34 KD-3 posture). |
| **F3** | Sub-blob decode: version mismatch, an out-of-bounds length prefix, or trailing bytes | **Fail loud** — the `SeasonSaveCodec` / `MatchSaveCodec` posture; version gate first, overflow-safe bound, trailing guard. |
| **F4** | An intake is requested for a `ClubId` with no `AcademyState` | **Fail loud** — a bootstrap/lifecycle bug, never auto-created (the #40 F6 precedent). |
| **F5** | A promotion targets an unknown prospect, or a senior squad already at `CLUB_SQUAD_SIZE` | **Refused** with an explicit result — a legal game state, not a crash (distinct from F4). |
| **F6** | An intake would allocate a `PlayerId` at or below the serialized high-water | **Fail loud** — id reuse breaks every keyed-by-`PlayerId` consumer (#28/#33/#41). |
| **F7** | Any #42 surface exposes a per-day growth modifier | **Forbidden by construction** — FR-YA-012; the ceiling dial and the #29 → #28 growth path are disjoint, and a reviewer finding one has found a double-count. |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §2 (FR-YA-001..028, data structures, F1..F7), promoted from design supplement v0.3. Status IN REVIEW. |
#endregion
