# Board & Ownership Dynamics #45 — Section 2: Requirements, Data Structures, Failure Modes

**Created:** July 25, 2026
**Last Updated:** July 25, 2026 (v0.2 — section-file PASS-1 fix pass)
**Version:** 0.2
**Status:** APPROVED

---

## 2.1 Functional requirements

**Cadence & ownership**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-BD-001 | All #45 state MUST advance on the **world tick** (`WorldClock`, one day = one `worldTick`) — never the 10 Hz/60 Hz match loops. | MUST | KD-7 |
| FR-BD-002 | #45 MUST own per-`ClubId` `BoardConfidence` + `OwnershipProfile` (+ deep-tier `TakeoverState`), serialized under #45's sub-blob. No other assembly writes them. | MUST | KD-1/KD-6 |
| FR-BD-003 | All #45 fields and formulas MUST be **integer per-mille**. No float MUST appear anywhere in #45 at any tier. | MUST | KD-1 |
| FR-BD-004 | `BoardConfidence.ConfidencePermille` MUST be clamped to `[0, 1000]` at every write. | MUST | KD-1 |
| FR-BD-005 | State MUST be created via the `Create()` / `Identity` factories; `default(OwnershipProfile)` (all dials `0`) MUST NOT be treated as a valid runtime value and MUST fail loud at any consuming seam (F4). | MUST | KD-4 |
| FR-BD-005a | A club's `{BoardConfidence, OwnershipProfile}` MUST be inserted as a **pair**, both factory-built, and the **enforced** guard MUST be at **record insertion** — not only at the consuming seam. `default(BoardConfidence)` is field-in-*range* (`ConfidencePermille = 0`, `LastAdvancedWorldDay = 0`) and therefore cannot be caught by a range check, yet it is **semantically severe**: confidence `0` is the `Critical` band, so a default-constructed entry reads as *"dismissal imminent"*, and its `LastAdvancedWorldDay = 0` (not the sentinel) makes the F6 guard **no-op** a day-0 advance instead of failing loud. Insertion-time validation is what closes it. | MUST | KD-1/§1.6 |

**The daily advance**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-BD-006 | `AdvanceBoardDay` MUST be a **deterministic** function of committed inputs and MUST make **no stochastic draw** at the minimal tier. | MUST | KD-2 |
| FR-BD-007 | Confidence MUST drift toward a target assembled from committed inputs by a bounded integer per-mille step, clamped `[0,1000]`. | MUST | KD-1 |
| FR-BD-008 | `LastAdvancedWorldDay` MUST be an idempotency cursor whose unadvanced sentinel is `BD_NOT_ADVANCED_SENTINEL = uint.MaxValue` — **not** `0`, since day `0` is a legal world day. | MUST | KD-1/§1.6 |
| FR-BD-009 | Advancing the same `worldDay` twice for a club MUST be a **no-op**; a day **gap** (> 1 day since `LastAdvancedWorldDay`, when not the sentinel) MUST **fail loud** — #30 advances one day at a time (F6). | MUST | KD-7 |
| FR-BD-010 | The daily input MUST have an explicit **neutral** value for a day on which nothing happened; a non-fixture day MUST be a well-defined advance (drift toward an unchanged target), not a skipped one. | MUST | KD-5 |
| FR-BD-011 | #45 MUST NOT define the semantics of the "on track?" projection it consumes (including a pre-first-fixture table); it consumes the committed integer #30 supplies (FR-SN-015). | MUST | KD-5 |

**Boundaries — what #45 must never do**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-BD-012 | #45 MUST NOT expose a sacking API, and MUST NOT fire any event that terminates a manager. It supplies confidence; **#30 decides**. | MUST | KD-3 |
| FR-BD-013 | #45 MUST NOT write `BoardObjective`, the league table, or any #40 state. Effects propagate **one-directionally** via values #45 projects and its consumers read. | MUST | KD-2/KD-5 |
| FR-BD-014 | #45 MUST NOT hold a copy of #30's objective or #40's budget; mirroring either would re-introduce the double truth KD-5 removes. | MUST | KD-6 |
| FR-BD-015 | #45's assembly MUST reference neither #30, #33, `living-world`, `SeasonSave`, nor `MatchEngine`, **at any tier**. | MUST | KD-3 |
| FR-BD-016 | The deep-tier #33 morale input MUST arrive as **routed committed values**, never as a #45→#33 assembly reference (preserving FR-BD-015). | MUST | KD-1 |

**The #40 seam**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-BD-017 | #45 MUST be the producer of #40's `BoardModifier` (#40 FR-FN-019) and MUST NOT add a second budget-multiplier path (#40 §7). | MUST | KD-2 |
| FR-BD-018 | The projection MUST be `TryProjectBoardModifier(clubId, out BoardModifier) → bool`, returning **false** for a club #45 does not model; the caller substitutes `BoardModifier.Identity`. "Not modelled" is a **named legal state** — never an exception, never a silent default. A *present but malformed* profile MUST still fail loud (F4). | MUST | KD-5/§1.6 |
| FR-BD-019 | With identity dials the projection MUST return **exactly** `BoardModifier.Identity` (`1000`), so #40's budget is bit-identical to pre-#45. | MUST | KD-8 |

**Determinism**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-BD-020 | Because the minimal tier is draw-free, #45 MUST register **no** RNG stream and MUST promote **no** domain tag at approval — `_RESERVED_0x2D_` / `SubsystemOrdinals.BoardOwnership = 95` stay **RESERVED**. | MUST | KD-2 |
| FR-BD-021 | Any deep-tier draw MUST be a **position-independent keyed** draw over `(clubId, worldDay, purpose)` at a **fixed** `DRAW_PURPOSE_RADIX` — no free-running cursor is persisted. | MUST | KD-2 |
| FR-BD-022 | The deep tier MUST register **one** subsystem-wide stream (fixed entity sentinel), **not** one per club — #45 MUST NOT contribute to the shared `MaxRngStreams` bound (#42 §7.4 R-1) at any tier. | MUST | KD-2 |
| FR-BD-023 | A refused advance (F1/F5/F6-fail path) MUST consume no draw and mutate no state. | MUST | KD-2 |

**Ownership & takeovers**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-BD-024 | Ownership types MUST be **values on one code path** (dials), never separate code paths or subtypes. | MUST | KD-4 |
| FR-BD-025 | The minimal tier MUST ship exactly one generic profile whose dials are the identity. | MUST | KD-4/KD-8 |
| FR-BD-026 | A takeover MUST change **#45-owned state only** — it **replaces** the stored `OwnershipProfile` (a `readonly struct`; there is no in-place dial mutation) and updates `TakeoverState`. It MUST write nothing in #30 or #40. | MUST | KD-2 |

**Persistence**

| FR | Requirement | Level | KD |
|---|---|---|---|
| FR-BD-027 | `BOARD_SAVE_FORMAT_VERSION` [FIXED] = 1; #45's state lands as an **opaque, independently version-gated** sub-blob composed into #30's `SeasonSaveCodec` — **not** a `WORLD_STORE_FORMAT_VERSION` bump. | MUST | KD-6 |
| FR-BD-028 | Every field MUST round-trip **field-identical**; **serialize, don't regenerate**. No RNG cursor exists to serialize (FR-BD-021). | MUST | KD-6 |
| FR-BD-029 | Restore MUST **fail loud** on version mismatch / out-of-bounds length prefix (overflow-safe bound compared against `total − offset`) / trailing bytes (F3/F5). | MUST | KD-6 |
| FR-BD-030 | The sub-blob MUST be **APPEND-only**: deep-tier fields go at the end with a `BOARD_SAVE_FORMAT_VERSION` bump, never inserted mid-block. | MUST | KD-6 |

## 2.2 Data structures

```csharp
// #45-owned per-club world-tick state (serialized, KD-6). Integer per-mille — no float, ever.
public struct BoardConfidence
{
    public int  ConfidencePermille;      // [0,1000]; BD_CONFIDENCE_NEUTRAL_PERMILLE (500) = neutral standing
    public uint LastAdvancedWorldDay;    // idempotency cursor; BD_NOT_ADVANCED_SENTINEL = uint.MaxValue (F6)
    public static BoardConfidence Create() => new() {
        ConfidencePermille   = BD_CONFIDENCE_NEUTRAL_PERMILLE,
        LastAdvancedWorldDay = BD_NOT_ADVANCED_SENTINEL };        // never default()
}

// The club's ownership. Types are VALUES on one code path (KD-4). Identity = all dials 1000.
public readonly struct OwnershipProfile
{
    public readonly OwnershipType Type;
    public readonly int ExpectationSeverityPermille;   // 1000 = x1.0 — how demanding the target feels
    public readonly int PatienceDecayPermille;         // 1000 = x1.0 — how fast confidence erodes off-track
    public readonly int BudgetContributionPermille;    // 1000 = x1.0 — feeds the BoardModifier projection
    public static OwnershipProfile Identity =>         // EXPLICIT factory; default() is x0 and fails loud (F4)
        new(OwnershipType.Generic, 1000, 1000, 1000);
}

public enum OwnershipType : byte { Generic = 0, Ambitious, Frugal, Absentee }   // deep-tier members; Generic = minimal

// Deep tier. Zero-valued and serialized at the minimal tier (APPEND-only discipline, FR-BD-030).
public struct TakeoverState { public uint LastTakeoverWorldDay; public int TakeoverCount; }

// Committed-values input #30 routes in (the HumanSystemsDayInput precedent) — #45 references no #30 type.
public readonly struct BoardDayInput
{
    public readonly int ObjectiveTrackPermille;   // [0,1000] #30's committed "on track?" projection (FR-SN-015)
    public readonly int MoraleSignalPermille;     // [0,1000] deep-tier #33 routed value; NEUTRAL at minimal (FR-BD-016)
    public static BoardDayInput Neutral =>        // a day on which nothing happened (FR-BD-010)
        new(BD_TRACK_NEUTRAL_PERMILLE, BD_TRACK_NEUTRAL_PERMILLE);
}

// The band #30's JobSecurity becomes at #45 T2 (KD-5 / ERR-030-009) — derived, never stored by #45.
public enum JobSecurityBand : byte { Critical = 0, Insecure, Stable, Secure }
```

## 2.3 Failure modes

| ID | Condition | Response |
|---|---|---|
| F1 | An out-of-range value at a consuming seam — confidence or an input outside `[0,1000]`, a dial ≤ 0. | Fail loud (`ArgumentOutOfRangeException`). |
| F2 | An out-of-contract byte on restore (negative per-mille, an undefined `OwnershipType`). | Fail loud at deserialize (the `MatchSaveCodec` posture). |
| F3 | Bad `BOARD_SAVE_FORMAT_VERSION` on restore. | Fail loud (version gate, read **first**). |
| F4 | `default(OwnershipProfile)` (all dials `0`, i.e. ×0 — the zero-value trap) reaching a consuming seam. | Fail loud (#40 `BoardModifier` / #41 `MedicalModifier` precedent). |
| F4a | A **default-constructed `BoardConfidence`** inserted into the store. It passes every range check (`0` is a legal per-mille and a legal world day), so it is invisible to F1 — but it means `Critical` standing and a broken day-0 guard (FR-BD-005a). | Fail loud **at insertion** (the #33 FR-HS-005 posture: the enforced guard is insertion, not the F6 path). |
| F5 | Out-of-bounds length prefix / trailing bytes in the sub-blob. | Fail loud (overflow-safe bound vs `total − offset`). |
| F6 | Re-advancing the same `worldDay` for a club; **or** a `worldDay` gap (> 1 day past `LastAdvancedWorldDay`, when not the sentinel). | **No-op** / **fail loud**, respectively. |
| F7 | An advance or projection for a `ClubId` with no #45 entry. | **Advance** fails loud (a bootstrap bug — never auto-created, the #40 FR-FN-025 posture). **Projection** returns `false` — a named legal state, not an error (FR-BD-018). The asymmetry is deliberate: #30 settles finances for *every* club but advances only clubs #45 models. |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-25 | — | Initial §2 (FR-BD-001..030, data structures, F1..F7 incl. the deliberate F7 advance-vs-projection asymmetry) from supplement v0.3. Status IN REVIEW. |
| 0.2 | 2026-07-25 | — | PASS-1 fixes (M+L): added **FR-BD-005a** + **F4a** — the `default(BoardConfidence)` zero-value trap was unaddressed, and it is *worse* than the `OwnershipProfile` one because every field is in range (so F1 cannot catch it) while confidence `0` means the `Critical` band and `LastAdvancedWorldDay = 0` no-ops the day-0 guard; enforced guard placed at record insertion per #33 FR-HS-005. FR-BD-026 reworded: `OwnershipProfile` is a `readonly struct`, so a takeover **replaces** it rather than mutating dials in place. |
#endregion
