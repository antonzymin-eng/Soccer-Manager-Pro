# Training System #29 — Section 2: Functional Requirements, Data Structures, Failure Modes

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.2 — PASS-1 → AR-2 → AR-3; APPROVED)
**Version:** 0.2
**Status:** APPROVED

---

## 2.1 Functional requirements

**Cadence & ownership**
- **FR-TR-001** — Training MUST run on the world tick only (`WorldClock` day); it MUST NOT read or advance
  the 10 Hz/60 Hz match loops.
- **FR-TR-002** — Per-player `TrainingState` (focus, conditioning, training-fatigue, last-advanced day) is
  #29-owned state, serialized under #29's sub-blob (KD-7).
- **FR-TR-003** — A team `TrainingSchedule` holds the per-player focus assignments; focus is a **persistent**
  field, changed only by the weekly `SetFocus` command (FR-TR-023).
- **FR-TR-004** — #29 exposes a **pure** `ComputeTrainingInput` (feeds #28 at #30's slot-1 seam) and a
  **mutating** `AdvanceTrainingDay` (the slot-2 world-day step); the two MUST be distinct entry points.

**Single-owner attribute mutation**
- **FR-TR-005** — #29 MUST write attributes **only** by populating #28's `TrainingInput`; it MUST NOT add a
  second attribute-mutation path (#28's `GrowthProjection` stays the sole writer, FR-PG-008).
- **FR-TR-006** — `ComputeTrainingInput` MUST be pure and deterministic — no mutation, no RNG, no jitter.
- **FR-TR-007** — With the attribute-growth dial off, `ComputeTrainingInput` MUST return `TrainingInput.
  Neutral`, so #28's `GrowthProjection` is byte-identical to the no-training path (KD-8).
- **FR-TR-008** — #29 MUST be fully deterministic and register **no** RNG stream; `_RESERVED_0x21_` /
  `SubsystemOrdinals` 83 remain reserved (KD-6).
- **FR-TR-009** — Any per-player training variation MUST be a deterministic function of the player's own
  attributes, never an RNG draw.

**Conditioning & fatigue**
- **FR-TR-010** — `Condition` is one integer cursor in `[CONDITION_MIN, CONDITION_MAX]`, training-driven,
  clamped (F1).
- **FR-TR-011** — `TrainingFatigue` is a world-tick integer accumulator in `[0, TRAINING_FATIGUE_MAX]`,
  **distinct** from match-tick fatigue; the two MUST NOT share a counter.
- **FR-TR-012** — `ProjectMatchEntryFatigue(in TrainingState) → float [0,1]` MUST be pure, MUST NOT be
  stored, and MUST feed the match-boot caller-supplied `float fatigue`; match-tick fatigue MUST NOT write
  back into `TrainingFatigue`.
- **FR-TR-013** — #29 MUST NOT reference or mutate `AerobicPool`, `PlayerAttributeProjection`, or
  `MatchEngine`.
- **FR-TR-014** — Training MUST accrue **daily** with no weekly batch boundary and no rollover step (KD-4).
- **FR-TR-015** — `AdvanceTrainingDay` MUST be idempotent per world day via `LastAdvancedWorldDay` (a
  save→restore→re-run of an already-advanced day is a no-op, F6).

**Seams**
- **FR-TR-016** — The step MUST take `in CoachingModifier` defaulting to `Identity` (×1.0); no #34 interface
  is built (KD-3).
- **FR-TR-017** — #29 MUST expose a read-only `InjuryRiskContribution` per player (from intensity +
  training-fatigue + conditioning); #41 reads it; #29 owns no injury model; no #41 interface is built (KD-5).

**Persistence**
- **FR-TR-018** — `TRAINING_SAVE_FORMAT_VERSION` [FIXED] = 1; #29's state lands as an opaque, independently
  version-gated sub-blob under #30's season save (`SeasonSaveCodec` pattern).
- **FR-TR-019** — Every `TrainingState` field + the schedule MUST be serialized and round-trip
  field-identical; **serialize, don't regenerate** (#30 KD-5).
- **FR-TR-020** — Restore MUST **fail loud** on version mismatch / out-of-bounds length prefix
  (overflow-safe `ReadCount`) / trailing bytes (F3/F5).

**Commands, observers, boundaries**
- **FR-TR-021** — An out-of-contract focus or `TrainingInput` MUST **fail loud** at the consuming seam (the
  #27 `SquadFileLoader` / #28 F4 precedent) — not silently clamped.
- **FR-TR-022** — A read-only `TrainingViewModel` (value copies) MUST be exposed for #31/#38 (KD-7).
- **FR-TR-023** — `SetFocus(club, playerId, focus)` MUST validate the focus enum and refuse an out-of-range
  value or an unknown player (F2/F4).
- **FR-TR-024** — The reference direction MUST stay one-way: `#30 → #29 → #28 → {#27,#16}`; #28's assembly
  stays schema-untouched.

## 2.2 Data structures

```csharp
public enum TrainingFocus : byte
{
    Balanced = 0,   // default; no dominant emphasis
    Rest,           // recovery emphasis — lowers training-fatigue faster, small conditioning gain
    Fitness,        // conditioning emphasis
    Technical,      // deep-tier: weights technical attributes in the growth input
    Physical,       // deep-tier: weights physical attributes
    Tactical,       // deep-tier: weights mental/positional attributes
}

// #29-owned per-player world-tick training state (serialized, KD-7).
public struct TrainingState
{
    public TrainingFocus Focus;         // persistent; set by SetFocus (KD-4)
    public int Condition;               // ONE conditioning cursor [CONDITION_MIN, CONDITION_MAX]
    public int TrainingFatigue;         // world-tick accumulator [0, TRAINING_FATIGUE_MAX] (NOT match fatigue)
    public uint LastAdvancedWorldDay;   // idempotency cursor (F6); NOT_ADVANCED sentinel = never advanced

    // Runtime states are created via Create — LastAdvancedWorldDay is seeded to the NOT_ADVANCED
    // sentinel (uint.MaxValue), NOT 0, so that a legitimate world-day 0 cannot collide with "never
    // advanced" (the day-0 double-accrual trap). default(TrainingState) is NOT a valid runtime state.
    public static TrainingState Create(TrainingFocus focus) =>
        new() { Focus = focus, Condition = CONDITION_START, TrainingFatigue = 0,
                LastAdvancedWorldDay = TRAINING_NOT_ADVANCED_SENTINEL };
}

// The team schedule = per-player focus, keyed by the club-scoped PlayerId (#27).
public readonly struct TrainingSchedule { /* PlayerId → TrainingFocus, per club */ }

// KD-3 coaching routing seam — identity until #34 lands.
public readonly struct CoachingModifier { public static CoachingModifier Identity => default; }

// KD-5 injury-risk output — read-only per-player scalar #41 consumes.
public readonly struct InjuryRiskContribution { public readonly int RiskScore; }

// KD-7 observer surface for #31/#38 (value copies).
public readonly struct TrainingViewModel { /* focus / condition / training-fatigue */ }
```

The **training block** persisted under `TRAINING_SAVE_FORMAT_VERSION` is, per club: each player's
`TrainingState` (keyed by `PlayerId`) + the club's `TrainingSchedule`.

`ComputeTrainingInput` (pure) and `AdvanceTrainingDay` (mutating) both take `in CoachingModifier`; the
former returns a `TrainingInput` (#28's type), the latter accrues `Condition` + `TrainingFatigue`. See §3.

## 2.3 Failure modes

| ID | Condition | Handling |
|---|---|---|
| **F1** | A daily delta would push `Condition`/`TrainingFatigue` past its bound | Clamped at the bound, deterministic (the #28 F1 ceiling precedent). |
| **F2** | `SetFocus` targets a player not in the club roster | Refused / no-op (bounded — the roster is authoritative). |
| **F3** | `TRAINING_SAVE_FORMAT_VERSION` mismatch on restore | **Fail loud** (`ArgumentException`), the `MatchSaveCodec` posture. |
| **F4** | An out-of-contract `TrainingFocus` / `TrainingInput` reaches a consuming seam | **Fail loud** — an invalid value is a bug, not silently clamped (FR-TR-021). |
| **F5** | Corrupt length prefix (out-of-bounds) or trailing bytes in the block | **Fail loud** (overflow-safe bound; the `WorldStateSerializer.ReadCount` posture). |
| **F6** | `AdvanceTrainingDay` invoked twice for one world day | Idempotent no-op guarded by `LastAdvancedWorldDay` — a mid-week save→restore→re-run does not double-accrue. |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial FR set (FR-TR-001..024), data structures, F1..F6. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | PASS-1 M-1 (single `Condition` cursor) / M-2 (no stream) folded from the supplement; AR-2/AR-3 clean; APPROVED. |
#endregion
