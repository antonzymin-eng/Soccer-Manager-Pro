# Training System #29 — Section 2: Functional Requirements, Data Structures, Failure Modes

**Created:** July 23, 2026
**Last Updated:** August 8, 2026, final entry of the day (v0.9 — AR pass 14 L2: the ERR-029-008 sweep completed — the §2.2 field comment and F2 stop naming the deleted free command)
**Last Updated (prior):** August 8, 2026, last entry of the day (v0.8 — ERR-029-008 at balance-pass AR pass 13 M2: FR-TR-003/FR-TR-023/§2.2 stop publishing the pre-T0-AR unsafe shape)
**Last Updated (prior):** August 8, 2026, later same day (v0.7 — pass-10 L5's row reorder rowed at pass 11 L1)
**Last Updated (prior):** August 8, 2026 (v0.6 — balance-pass AR pass 9 L4: new F8 — the sentinel itself is not a day; the refusal the code has always enforced gets its normative row)
**Last Updated (prior):** August 6, 2026 (v0.5 — ERR-029-004: §2.3 F3's exception type corrected to match the posture it cites)
**Last Updated (prior):** July 27, 2026 (v0.4 — back-prop landed atomically with the ten-spec approval wave; see the version-history row)
**Version:** 0.9
**Status:** APPROVED

---

## 2.1 Functional requirements

**Cadence & ownership**
- **FR-TR-001** — Training MUST run on the world tick only (`WorldClock` day); it MUST NOT read or advance
  the 10 Hz/60 Hz match loops.
- **FR-TR-002** — Per-player `TrainingState` (focus, conditioning, training-fatigue, last-advanced day) is
  #29-owned state, serialized under #29's sub-blob (KD-7).
- **FR-TR-003** — Focus is a **persistent per-player field** living on `TrainingState.Focus` — the **single
  source of truth**, changed only by the FR-TR-023 command. `TrainingSchedule` is the **club-scoped
  handle over the per-player `TrainingState.Focus` values, and it OWNS the FR-TR-023 write** — the club's
  ids and states are bound as a pair at its construction, so the command provably cannot pair one club's
  ids with another's states *(restated at ERR-029-008 — the T0 AR's H2 moved the writer here from the
  two-array `TrainingStep.SetFocus`, and this FR kept describing the pre-fix read-only view for three
  months)*; it MUST NOT store focus separately (no duplicate, drift-prone copy) and is NOT independently
  serialized.
- **FR-TR-004** — #29 exposes a **pure** `ComputeTrainingInput` (feeds #28 at #30's slot-1 seam) and a
  **mutating** `AdvanceTrainingDay` (the slot-2 world-day step); the two MUST be distinct entry points.

**Single-owner attribute mutation**
- **FR-TR-005** — #29 MUST write attributes **only** by populating #28's `TrainingInput`; it MUST NOT add a
  second attribute-mutation path (#28's `GrowthProjection` stays the sole writer, FR-PG-008).
- **FR-TR-005a** *(ERR-029-003, at #53's approval)* — `ComputeTrainingInput` MUST accept the #53
  **training-ground facility term as a SECOND root-assembled input**, alongside #34's `CoachingModifier`.
  It MUST NOT be delivered by #53 returning a `TrainingInput`: FR-TR-005 makes #29 the **sole writer** of
  that type, and a #53-returned one would be exactly the second path it forbids. The root assembles both
  terms and passes them in; **#29's logic is unchanged and #28's type is untouched.** Neutral facilities
  MUST yield the same result as today, so the addition is behaviour-neutral until a club upgrades.
  ◑ Spec-text-first: the requirement lands at approval, the parameter at #29's Stage-3 tier.
- **FR-TR-006** — `ComputeTrainingInput` MUST be pure and deterministic — no mutation, no RNG, no jitter —
  and MUST read **only** fields `AdvanceTrainingDay` does not mutate (`Focus`, the player's attributes, and
  the `CoachingModifier`). It MUST NOT read `Condition` / `TrainingFatigue` / `LastAdvancedWorldDay`. This
  field-independence — not purity alone — is what makes the slot-1 read order-independent of the slot-2
  mutation (KD-2); a future growth term that reads a mutated field would reintroduce a slot ordering hazard.
- **FR-TR-007** — `ComputeTrainingInput` MUST be gated by a **#29-owned** `deepTrainingEnabled` flag (the
  Stage-2/Stage-3 dial), **not** #28's `curveEnabled`: with `deepTrainingEnabled` off it MUST return
  `TrainingInput.Neutral`, so #28's growth is byte-identical to the no-training path (KD-8). Because #28's
  literal §4.3 step (its `curveEnabled` off) ignores the `TrainingInput` magnitude, #29's Stage-3
  contribution is realized by #28 **only** when #28's own `curveEnabled` is on — an independent dial #29 does
  not set.
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
- **FR-TR-017** — #29 MUST expose a read-only `InjuryRiskContribution` per player (computed by
  `ComputeInjuryRisk` from `TrainingFatigue` + low `Condition`, mitigated by the player's own robustness
  attributes — §3.4); #41 reads it; #29 owns no injury model; no #41 interface is built (KD-5).

**Persistence**
- **FR-TR-018** — `TRAINING_SAVE_FORMAT_VERSION` [FIXED] = 1; #29's state lands as an opaque, independently
  version-gated sub-blob under #30's season save (`SeasonSaveCodec` pattern).
- **FR-TR-019** — Every `TrainingState` field (per club, keyed by `PlayerId`) MUST be serialized and
  round-trip field-identical; **serialize, don't regenerate** (#30 KD-5). `TrainingSchedule` is derived from
  the per-player focus and MUST NOT be serialized separately (FR-TR-003).
- **FR-TR-020** — Restore MUST **fail loud** on version mismatch / out-of-bounds length prefix
  (overflow-safe `ReadCount`) / trailing bytes (F3/F5).

**Commands, observers, boundaries**
- **FR-TR-021** — An out-of-contract focus or `TrainingInput` MUST **fail loud** at the consuming seam (the
  #27 `SquadFileLoader` / #28 F4 precedent) — not silently clamped.
- **FR-TR-022** — A read-only `TrainingViewModel` (value copies) MUST be exposed for #31/#38 (KD-7).
- **FR-TR-023** — `TrainingSchedule.TrySetFocus(playerId, focus)` MUST validate the focus enum and refuse
  an out-of-range value or an unknown player (F2/F4). It lives on the club-scoped handle — NOT a free
  `SetFocus(club, playerId, focus)` command *(the shape this FR specified until ERR-029-008: the free
  command's two-array form let one club's ids be silently paired with another club's states — same
  length, no guard, the wrong club's player written; the T0 AR deleted it and the spec kept publishing
  it)* — because binding ids and states once at construction is what makes the mispair structurally
  impossible rather than merely unvalidated.
- **FR-TR-024** — The reference direction MUST stay one-way: `#30 → #29 → #28 → {#27,#16}`; #28's assembly
  stays schema-untouched.

**Roster-membership lifecycle (co-designed with #28's churning roster)**
- **FR-TR-025** — The per-club `TrainingState` set MUST track roster membership in lockstep with #28's
  season-boundary roster mutation (FR-PG-011/015): on a #28 `RegenResult`, a `TrainingState.Create(Balanced)`
  MUST be inserted for each **fresh `PlayerId`** (never `default(TrainingState)` — that reintroduces the
  day-0 trap); on a `RetirementResult`, the retiree's `TrainingState` entry MUST be **removed**. This is the
  FR-PG-011 "remove-retiree / insert-regen" parallel, keyed by `PlayerId`, applied at the season boundary by
  the roster owner (#30). Without it the block leaks retired entries unboundedly across seasons and regens
  have no defined training state (F7 — the day-0 hazard).
- **FR-TR-026** — In normal operation `AdvanceTrainingDay` MUST be called with `worldDay == LastAdvanced +
  1` (post-`Create`, the first advance is the player's first world day). A gap (`worldDay > LastAdvanced +
  1`, post-sentinel) MUST **fail loud** (`ArgumentException`) rather than silently skip the intervening
  days' accrual — #29 does not batch-replay a gap (KD-4, no rollover loop; #30 advances one day at a time).

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
    public TrainingFocus Focus;         // persistent; set by TrainingSchedule.TrySetFocus (KD-4)
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

// The club-scoped focus handle (FR-TR-003/FR-TR-023, as restated at ERR-029-008): iterates
// PlayerId → state.Focus AND owns the one write, TrySetFocus — ids and states bound as a pair at
// construction. NOT a stored copy and NOT separately serialized (focus lives only on
// TrainingState.Focus, single source of truth).
public readonly struct TrainingSchedule { /* PlayerId → Focus view + TrySetFocus(playerId, focus) */ }

// KD-3 coaching routing seam — identity until #34 lands.
public readonly struct CoachingModifier { public static CoachingModifier Identity => default; }

// KD-5 injury-risk output — read-only per-player scalar #41 consumes.
public readonly struct InjuryRiskContribution { public readonly int RiskScore; }

// KD-7 observer surface for #31/#38 (value copies).
public readonly struct TrainingViewModel { /* focus / condition / training-fatigue */ }
```

The **training block** persisted under `TRAINING_SAVE_FORMAT_VERSION` is, per club: each player's
`TrainingState` keyed by `PlayerId` (focus included — the single source of truth). `TrainingSchedule` is
**not** persisted (it is derived from those states, FR-TR-003/019). The `TrainingState` set tracks roster
membership per FR-TR-025 (regen inserts, retiree removes).

`ComputeTrainingInput` (pure) and `AdvanceTrainingDay` (mutating) both take `in CoachingModifier`; the
former returns a `TrainingInput` (#28's type) gated by the #29-owned `deepTrainingEnabled` flag (FR-TR-007),
the latter accrues `Condition` + `TrainingFatigue`. See §3.

## 2.3 Failure modes

| ID | Condition | Handling |
|---|---|---|
| **F1** | A daily delta would push `Condition`/`TrainingFatigue` past its bound | Clamped at the bound, deterministic (the #28 F1 ceiling precedent). |
| **F2** | `TrainingSchedule.TrySetFocus` targets a player not in the club roster | Refused / no-op (bounded — the roster is authoritative). |
| **F3** | `TRAINING_SAVE_FORMAT_VERSION` mismatch on restore | **Fail loud** (`InvalidOperationException`), the `MatchSaveCodec` posture — corrected from `ArgumentException` at #29 T1 (ERR-029-004): the cited posture throws `InvalidOperationException`, which is not an `ArgumentException`, so the two halves of this row contradicted each other. Framing corruption is a state fault in the bytes, not a bad argument. |
| **F4** | An out-of-contract `TrainingFocus` / `TrainingInput` reaches a consuming seam | **Fail loud** — an invalid value is a bug, not silently clamped (FR-TR-021). |
| **F5** | Corrupt length prefix (out-of-bounds) or trailing bytes in the block | **Fail loud** (overflow-safe bound; the `WorldStateSerializer.ReadCount` posture). |
| **F6** | `AdvanceTrainingDay` invoked twice for one world day (`worldDay <= LastAdvanced`) | Idempotent no-op guarded by `LastAdvancedWorldDay` — a mid-week save→restore→re-run does not double-accrue. |
| **F7** | `AdvanceTrainingDay` called with a **day gap** (`worldDay > LastAdvanced + 1`, post-sentinel), or a player with no `TrainingState` (a regen never inserted per FR-TR-025) | **Fail loud** (`ArgumentException`) — a gap silently under-accrues and a missing state is a lifecycle bug (the day-0 hazard); neither is clamped or defaulted. |
| **F8** | `AdvanceTrainingDay` invoked with `worldDay == TRAINING_NOT_ADVANCED_SENTINEL` itself | **Fail loud** (`ArgumentException`) — the sentinel is a reserved value, not a day; stored, the cursor would read back "never advanced" and re-arm the day-0 double-accrual trap F6 closes. |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial FR set (FR-TR-001..024), data structures, F1..F6. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | PASS-1 M-1 (single `Condition` cursor) / M-2 (no stream) folded from the supplement; AR-2/AR-3 clean; APPROVED. |
| 0.3 | 2026-07-23 | — | PASS-2: +FR-TR-025 (regen/retire lifecycle) / FR-TR-026 (day-gap fail-loud); FR-TR-003 (focus single-source, schedule = derived view) / 006 (field-independence invariant) / 007 (#29-owned `deepTrainingEnabled`) / 019 (schedule not serialized); +F7. |
| 0.4 | 2026-07-27 | — | **ERR-029-003** (at #53's approval): new **FR-TR-005a** — `ComputeTrainingInput` accepts #53's training-ground term as a **second root-assembled input**, alongside #34's `CoachingModifier`. Explicitly **not** delivered as a #53-returned `TrainingInput`, which FR-TR-005 forbids (#29 is that type's sole writer). Behaviour-neutral at neutral facilities; ◑ parameter at #29's Stage-3 tier. |
| 0.5 | 2026-08-06 | — | **ERR-029-004** (at #29 T1): §2.3 **F3** said `ArgumentException` while citing the `MatchSaveCodec` posture, which throws `InvalidOperationException` — the row contradicted itself, and an implementer honouring the type would have diverged from every sibling codec. Corrected to `InvalidOperationException`. |
| 0.6 | 2026-08-08 | — | **Balance-pass AR pass 9 (L4)**: new **F8** — `AdvanceTrainingDay` invoked with the never-advanced sentinel as `worldDay` itself fails loud; enforced in code since T0 with no F-row. §3.1's pseudocode gains the guard in the same commit; found at the #41 sibling, fixed at both. |
| 0.7 | 2026-08-08 | — | **Balance-pass AR pass 10 (L5) — rowed at pass 11 (L1)**: the version table's rows reordered ascending (0.4 had sat below 0.5/0.6); the reorder shipped rowless. |
| 0.8 | 2026-08-08 | — | **ERR-029-008 (balance-pass AR pass 13, M2)** *("all three restated" was three of seven — the §2.2 field comment, F2, §3 and §5 still said `SetFocus`; completed at pass 14 L2)*: FR-TR-003 still called `TrainingSchedule` a read-only view and FR-TR-023 still specified the free `SetFocus(club, playerId, focus)` — the exact two-array shape the T0 AR's High DELETED (one club's ids silently paired with another's states) — twelve passes after the fix; the §2.2 sketch matched. All three restated to the club-scoped handle that owns the write; the code needed no change. |
| 0.9 | 2026-08-08 | — | **Balance-pass AR pass 14 (L2)**: ERR-029-008's sweep had left four bare `SetFocus` sites, two INSIDE the section it rewrote (the §2.2 field comment, F2) — the grep-boundary class without even a file boundary; corrected here + §3/§5. |
#endregion
