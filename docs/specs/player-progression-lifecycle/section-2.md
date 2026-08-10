# Player Progression & Lifecycle #28 — Section 2: Functional Requirements, Data Structures, Failure Modes

**Created:** July 23, 2026
**Last Updated:** August 10, 2026 (v0.6 — ERR-028-017: AR pass 5 spec corrections — §2.3 F3's exception type corrected (`InvalidOperationException`, matching #29/#41's own ERR-029-004/ERR-041-008 corrections), F5 gains the same type, F8 extended from one refusing site to five, new F9 for the FR-PG-021 batch's four validation gates, §2.2 gains `ClubTrainingInputs`/`TrainingInputBatch`)
**Last Updated (prior):** August 9, 2026 (v0.5 — ERR-028-014: the never-advanced sentinel retired from #28's legal store states)
**Last Updated (prior):** August 8, 2026 (v0.4 — ERR-028-006: `BirthWorldDay` becomes a signed anchor; ERR-028-009: F8 sentinel-refusal row)
**Version:** 0.6
**Status:** APPROVED

---

## 2.1 Functional requirements

**Cadence & determinism (KD-1)**
- **FR-PG-001** — All player lifecycle mutation MUST run on the world tick (`WorldClock` day), never
  the 10 Hz/60 Hz match loops.
- **FR-PG-002** — Aging, decline, and growth of existing players MUST be a pure deterministic integer
  projection with **no RNG draw**. Growth MUST accumulate in an **integer fixed-point `GrowthCursor`**,
  never a float accumulator.
- **FR-PG-003** — The `[1,20]` `PlayerAttributes` values MUST be the **single source of truth** for a
  player's ability; `CurrentAbility` MUST be a **derived** summary (never a second accumulator);
  `PotentialAbility` MUST be the ceiling; the `GrowthCursor` MUST be the only accumulator.
- **FR-PG-004** — The daily step MUST accrue `dailyPoints` to the cursor and, when it crosses
  `POINT_COST`, spend one attribute-point on the next attribute in a **deterministic weighted order**,
  respecting the `PotentialAbility` ceiling; decline MUST be the symmetric drain.
- **FR-PG-005** — Age MUST be **derived** from a serialized `BirthWorldDay`: `AgeYears = (worldDay −
  BirthWorldDay) / DAYS_PER_YEAR` (integer division). There MUST be **no** discrete per-year attribute
  step — all attribute change is the `GrowthCursor` (FR-PG-004) — so nothing can be double-counted at
  a year boundary. #28 MUST keep the career-state `PlayerRecord.Age` current as a derived cache (like
  `CurrentAbility`), so a consumer reading `record.Age` gets **current** age, never the new-game seed.
- **FR-PG-006** — A mid-year save→restore MUST reproduce the identical continuation (byte-exact); no
  step MUST be double-counted across a save boundary.
- **FR-PG-007** — With `curveEnabled` off, `GrowthProjection` MUST reproduce the literal §4.3 step
  exactly (the behaviour-neutral identity, KD-8).

**The #29 training seam (KD-2)**
- **FR-PG-008** — `GrowthProjection` MUST be the **sole** attribute-mutation path; the daily step
  MUST take a per-player `TrainingInput` it **reads** — training is an input, never a parallel
  mutation of the same attributes.
- **FR-PG-009** — `TrainingInput` MUST default to `Neutral`; a `Neutral` input MUST leave the daily
  step byte-identical to no training input. #28 MUST NOT declare an interface against the absent #29
  producer (FR-LW-031).

**Regens (KD-3)**
- **FR-PG-010** — A regen MUST be produced deterministically from the `progression.regen` stream,
  reusing #27's fixed-budget Reserve/DrawReserved/CloseReservation draw pattern (so a regen is
  byte-reproducible from `(seed, clubId, stream position)`).
- **FR-PG-011** — A regen MUST receive a **fresh, monotonically-allocated `PlayerId`** (never a
  retiree's — the career-state block keys on `PlayerId`, so reuse would leak stale lifecycle state);
  the retiree's block entry MUST be removed as the regen's fresh entry is inserted.
- **FR-PG-012** — Regen club/nation MUST be read from #27's roster world (read-only); #28 MUST NOT
  mutate #27's `Squad` directly — it emits a `RegenResult` the roster owner (#30/#27) applies.

**Retirement (KD-5)**
- **FR-PG-013** — Retirement MUST be evaluated on the world tick, **hard at `RETIREMENT_AGE`** (the
  §4.3 literal); a retiring player MUST be **flagged** (`RetirementFlag` + `RetirementDay`), not
  removed.
- **FR-PG-014** — A flagged-retiring player MUST remain selectable until the season boundary (an
  in-progress season's fixtures/selection MUST NOT be disrupted).
- **FR-PG-015** — Roster removal + regen replacement MUST happen **only at the season boundary** via
  `RetirementResult` / `RegenResult`, never mid-fixture.

**Persistence (KD-4)**
- **FR-PG-016** — #28 MUST serialize the complete career-state `PlayerRecord` set + the lifecycle
  overlay under its own **`PROGRESSION_SAVE_FORMAT_VERSION`**; #27's canonical `PlayerRecord` /
  `PlayerAttributes` struct MUST gain **no** CA/PA fields.
- **FR-PG-017** — The block MUST be composed by the season-save root as an **opaque, independently
  version-gated sub-blob** (`SeasonSaveCodec` pattern); the world blob (`WORLD_STORE_FORMAT_VERSION`)
  and match blob (`MATCH_SAVE_FORMAT_VERSION`) MUST stay byte-untouched.
- **FR-PG-018** — The codec MUST fail loud on a bad `PROGRESSION_SAVE_FORMAT_VERSION`, an out-of-bounds
  length prefix (overflow-safe bound), or trailing bytes (the `MatchSaveCodec`/`WorldStateSerializer`
  posture).
- **FR-PG-019** — The lifecycle-block entry count MUST equal the managed roster size (a vacancy is
  filled 1:1); the blob MUST NOT grow unboundedly across seasons.

**Determinism identifiers (KD-7 / §4.3)**
- **FR-PG-020** — #28 MUST register a `player-progression.regen` RNG stream **per club**
  (`entityId = clubId` — the #27 `RosterGenerator.RegisterStream(..., entityId: clubId, ...)` pattern,
  so each club's newgen sequence is an independent reproducible sub-stream), under
  `DOMAIN_TAG_PLAYER_PROGRESSION = 0x20` / `SubsystemOrdinals.PlayerProgression = 82`. A club's stream
  registers at the **first regen for that club** (T-phase), never earlier — registering a stream with
  zero draw sites is the phantom-surface class FR-LW-031 forbids. Draw sites MUST be APPEND-only.

**Invocation & discipline (KD-6 / KD-7)**
- **FR-PG-021** — #28 MUST expose `AdvanceDay(worldDay, in trainingInputs)` + `RunSeasonBoundary(...)`
  invoked by #30; #28 MUST NOT reference #30 / the season assembly.
- **FR-PG-022** — `ProgressionEngine` MUST be the **sole writer** of lifecycle state and the sole
  mutator of the managed roster's attributes; #30/tests MUST mutate only through the public step API,
  never by poking fields.
- **FR-PG-023** — The `LifecycleViewModel` MUST be a read-only value-copy surface; reading it MUST NOT
  mutate state (observer-neutral — the digest/round-trip is unaffected by observation).
- **FR-PG-024** — `RunSeasonBoundary` MUST NOT re-bank growth (banked daily, KD-1); it applies the
  deferred retirements + produces regens, and MUST be restartable (a mid-boundary save restores to
  the same continuation, idempotent per boundary).

## 2.2 Data structures

```csharp
// The per-player lifecycle overlay #28 alone owns (the [1,20] attributes live on the career-state
// PlayerRecord the block also holds — KD-1/KD-4; NOT duplicated here).
public struct PlayerLifecycle
{
    public int PotentialAbility;   // the ceiling, wide integer [0, ABILITY_MAX]
    public int CurrentAbility;     // DERIVED summary of the [1,20] attributes (cache; recomputed)
    public long GrowthCursor;      // the ONLY accumulator — integer fixed-point points pool
    public long BirthWorldDay;     // the authoritative age anchor — the world-day this player was "born"
                                   //   (= newGameDay − Age0·DAYS_PER_YEAR at new-game); age is DERIVED
                                   //   from it, so there is no discrete "rollover" step to double-count.
                                   //   SIGNED deliberately (ERR-028-006): a new world starts on world day
                                   //   0, so for every generated player with Age0 > 0 the anchor is
                                   //   NEGATIVE. Held unsigned it had to be clamped to 0, which made the
                                   //   derived age worldDay/DAYS_PER_YEAR — the entire league read as age
                                   //   0 after the first daily step, the Decline band was unreachable, and
                                   //   RETIREMENT_AGE could never fire. A player born before the epoch is
                                   //   the ORDINARY case for a non-zero generated age, not an edge case.
    public bool RetirementFlag;    // set on the world tick at RETIREMENT_AGE (KD-5)
    public uint RetirementDay;     // the world-day the flag was set (0 if not flagged)
    public uint LastAdvancedWorldDay;  // the last world day the daily step ran. PROGRESSION_NOT_ADVANCED_SENTINEL
                                   //   (uint.MaxValue) is NOT a legal value here (ERR-028-014) — it is a
                                   //   refused WORLD DAY (F8) only. Every carried player's cursor is real:
                                   //   SeedFrom (§3.1.1) anchors it at the seed day at generation, and
                                   //   FromBlocks (the restore path) refuses a lifecycle carrying the
                                   //   sentinel on decode.
}

// The per-player growth contribution #29 writes (KD-2). Neutral == no training (FR-PG-009).
public readonly struct TrainingInput
{
    public static TrainingInput Neutral => default;  // all-zero = the identity contribution
    // Stage-3 #29 fields (focus/intensity/coach quality) append here; the daily step reads them.
}

// The season-boundary signals #28 emits (KD-5); #30/#27 apply the Squad mutation.
public readonly struct RetirementResult { /* retiree PlayerIds, per club */ }
public readonly struct RegenResult      { /* new PlayerRecords + their fresh PlayerIds, per club */ }

// Read-only observer surface for #31/#38 (KD-7).
public readonly struct LifecycleViewModel { /* age / CA / PA / retirement (value copies) */ }

// The FR-PG-021 batch parameter's actual type (ERR-028-017 — neither had a declared shape here; §4.5
// called the seam "a TrainingInput method parameter", and §3.1's pseudocode wrote AdvanceDay(worldDay,
// in trainingInputs) without saying what that was). One club's per-player contributions, ids carried
// alongside the inputs so AdvanceDay can VERIFY the pairing rather than trust two arrays to stay in
// the same order.
public readonly struct ClubTrainingInputs
{
    public readonly int ClubId;
    public readonly int[] PlayerIds;      // never null; index i pairs with Inputs[i]
    public readonly TrainingInput[] Inputs;  // never null; same length as PlayerIds
}

// The whole league's contributions for one world day — AdvanceDay's trainingInputs argument.
// TrainingInputBatch.Neutral (Clubs == null) is the explicit "no training anywhere" identity
// (FR-PG-009); a BOUND batch must cover every carried club, in the store's own ascending-ClubId
// order, with an entry for every one of that club's players in the store's own ascending-PlayerId
// order (F9) — a partial batch is refused, not gap-filled with Neutral.
public readonly struct TrainingInputBatch
{
    public readonly ClubTrainingInputs[] Clubs;   // null == Neutral
    public static TrainingInputBatch Neutral => default;
    public bool IsNeutral => Clubs == null;
}
```

The sentinel for `LastAdvancedWorldDay` is `uint.MaxValue`, not `0` — day 0 is a legitimate world day (the
day-0 trap: a zero default would read as "already advanced through day 0" and silently skip that player's
first real step), the same reasoning and the same sentinel value #29 uses for
`TRAINING_NOT_ADVANCED_SENTINEL`. The cursor is what makes the daily step idempotent when #30 runs a
fixture day's KD-2 slots twice — once pre-round and once from the advance loop (ERR-030-027) — so a
day already reflected in the cursor is a no-op rather than a second application of growth (ERR-028-005).

**Unlike #29/#41's identically-valued sentinels, #28's is never a legal resting state of the cursor
itself (ERR-028-014).** #29's and #41's fresh states carry no clock-anchored quantity (zero fatigue, no
injuries), so a state that has genuinely "never advanced" reads the same at every world day and their
sentinel is a legitimate value the cursor field holds until the first daily step. #28's fresh state
carries `BirthWorldDay`, from which age is derived (§3.1.1) — so a "never advanced" #28 state means a
different age at every world day it might be composed against. `SeedFrom` therefore anchors
`LastAdvancedWorldDay` at the seed day rather than at the sentinel, and `FromBlocks` refuses to
construct a lifecycle whose cursor **is** the sentinel. The constant survives only as the refused input
to `AdvanceDay`'s `worldDay` parameter (F8) — it is a value the API rejects, never a value the cursor
field is found holding.

The **career-state block** persisted under `PROGRESSION_SAVE_FORMAT_VERSION` is, per `PlayerId`:
the complete `PlayerRecord` (identity + evolving `PlayerAttributes`, #27 types) **and** its
`PlayerLifecycle` overlay, plus a store-level `NextPlayerId` monotonic cursor (FR-PG-011).

## 2.3 Failure modes

| ID | Condition | Handling |
|---|---|---|
| **F1** | A growth spend would exceed the `PotentialAbility` ceiling | The spend is a no-op (clamped at the ceiling), deterministic; the cursor is not consumed past the ceiling. |
| **F2** | A regen is requested for a club with no vacancy (roster already full) | Refused / no-op — the bounded-roster invariant (FR-PG-019); a regen is produced only for an actual retirement vacancy. |
| **F3** | `PROGRESSION_SAVE_FORMAT_VERSION` mismatch on restore | **Fail loud** (`InvalidOperationException`), the `MatchSaveCodec` posture — corrected from `ArgumentException` (ERR-028-017), the same self-contradiction ERR-029-004 (#29 §2.3 F3) and ERR-041-008 (#41 §2.3 F3) filed against their sibling rows: the cited `MatchSaveCodec` posture IS `InvalidOperationException`, so the row's own two halves disagreed. Third instance of the class. |
| **F4** | A `TrainingInput` carries an out-of-contract value | **Fail loud** at the consuming seam (the #27 `SquadFileLoader` bounds-gate precedent) — an invalid input from the future #29 producer is a bug, not silently clamped. |
| **F5** | Corrupt length prefix (out-of-bounds) or trailing bytes in the block | **Fail loud** (`InvalidOperationException`; overflow-safe bound; the `WorldStateSerializer.ReadCount` posture). Exception type added at ERR-028-017 alongside F3's correction — the codec's framing gates (`SaveBlobFramingHelpers.Require`/`ReadCount`) throw the same type as F3's format-version gate, not `ArgumentException`. |
| **F6** | `RunSeasonBoundary` invoked twice for one season boundary | Idempotent per boundary (FR-PG-024) — the second invocation is a no-op (guarded by the boundary marker), so a mid-roll save→restore→re-run does not double-apply. |
| **F8** | The never-advanced sentinel (`PROGRESSION_NOT_ADVANCED_SENTINEL`) is presented where a real cursor or world day is required | **Fail loud, at FIVE sites — not one, and not uniformly typed** (ERR-028-017 corrects this row, which named only `AdvanceDay`): `AdvanceDay(worldDay == sentinel)` → `ArgumentOutOfRangeException` (ERR-028-009); `SeedFrom(newGameWorldDay == sentinel)` → `ArgumentOutOfRangeException` (ERR-028-015 — anchoring the cursor at the seed day made the seed site a second way to write the one value `FromBlocks` refuses); `FromBlocks` (a decoded lifecycle's cursor equals the sentinel) → `ArgumentException`; `ProgressionSaveCodec.Encode` and `ProgressionSaveCodec.Decode`, both via the shared `RequireNoNeverAdvancedSentinel` → `ArgumentException` at each (the ERR-028-011(a) class, one ERR later than ERR-028-014 — never write or read back what `FromBlocks` refuses). Storing the sentinel as a real cursor re-arms the day-0 trap (a player reads as never-advanced forever, so the step stops being idempotent) and the gap-replay loop would not terminate at `uint.MaxValue`. |
| **F9** | The FR-PG-021 batch (`TrainingInputBatch`, §2.2) is bound but malformed | **Fail loud** (`ArgumentException`, `ProgressionEngine.ValidateBatch`), at four independent gates, none stated in any prior revision of this spec (ERR-028-017): **(a)** the batch's club count does not equal the store's carried club count — a dropped club would otherwise advance on Neutral, indistinguishable from a club that genuinely trained neutrally; **(b)** a batch club does not positionally match the store's club at the same index — both sides hold clubs in ascending `ClubId` order, so a mismatch means the two roster views have drifted; **(c)** a batch club's player count does not equal that club's carried player count — a partial batch is refused rather than gap-filled with `TrainingInput.Neutral`, which would make a dropped player indistinguishable from an untrained one; **(d)** a batch player id does not equal the store's player id at the same position — the batch would train the wrong player. `TrainingInputBatch.Neutral` (an unbound batch) bypasses all four — it is the explicit "no training anywhere" identity (FR-PG-009), not a batch to validate. |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial FR set (FR-PG-001..024), data structures, failure modes F1..F6. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | Section-file PASS-1 (0H+2M: M-1 age-model muddle → one BirthWorldDay-derived representation; M-2 per-club regen stream) → AR-2 (3M cross-fix regressions) → AR-3 convergence; APPROVED. See section-9 §9.3.1. |
| 0.3 | 2026-08-08 | — | ERR-028-005: `PlayerLifecycle` gains `LastAdvancedWorldDay` (sentinel `uint.MaxValue`) so `AdvanceDay` is idempotent per day and gap-complete, matching #29's `TRAINING_NOT_ADVANCED_SENTINEL` precedent; documented alongside the struct listing. Spec + code, same commit (T1/T2a). |
| 0.4 | 2026-08-08 | — | ERR-028-006: `BirthWorldDay` becomes a **signed** `long` — a new world starts on day 0, so any generated player with Age0 > 0 anchors negative; clamping to 0 read the whole league as age 0 after one daily step. ERR-028-009: new **F8** row — `AdvanceDay` fails loud on the never-advanced sentinel, matching #29/#41's guard. Spec + code, same commit (AR over the T1/T2a landing). |
| 0.5 | 2026-08-09 | — | ERR-028-014: the `LastAdvancedWorldDay` struct comment and the sentinel discussion below §2.2's listing are corrected — the sentinel is NOT a legal cursor state for #28 as it is for #29/#41 (their fresh states carry no clock-anchored quantity; #28's derives age from `BirthWorldDay`). `SeedFrom` anchors the cursor at the seed day and `FromBlocks` refuses a lifecycle carrying the sentinel; the constant survives only as F8's refused `worldDay` input. Spec + code, same commit. |
| 0.6 | 2026-08-10 | — | ERR-028-017 (AR pass 5 spec-vs-code sweep, found against the T1/T2a landing, no code change): §2.3 **F3** corrected `ArgumentException` → `InvalidOperationException` (the row cited the `MatchSaveCodec` posture, which throws `InvalidOperationException` — the third instance of this exact self-contradiction, after ERR-029-004 and ERR-041-008 on the sibling #29/#41 rows); **F5** gains the same exception type, undocumented until now; **F8** extended from naming one refusing site (`AdvanceDay`) to all five the code carries (`AdvanceDay`, `SeedFrom`, `FromBlocks`, `ProgressionSaveCodec.Encode`, `ProgressionSaveCodec.Decode` — two exception types across them); new **F9** for the FR-PG-021 batch's four `ValidateBatch` refusals (club-count coverage, positional club agreement, per-club player-count exactness, per-player id agreement), none previously stated anywhere. §2.2 gains `ClubTrainingInputs`/`TrainingInputBatch` — the batch parameter's actual shape, previously declared in no document. |
#endregion
