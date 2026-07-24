# #28 Player Progression — T0 Implementation Plan (file-by-file)

> **Created:** July 23, 2026 · **Status:** PLAN (implementation-ready, file-by-file).
> **Drills:** `squad-player-stage1-plan.md` §3 Track A.1 (the buildable-now, no-new-dependency slice).
> **Authoritative contract:** `docs/specs/player-progression-lifecycle/` (Spec #28, APPROVED) —
> section-2 (FRs), section-3 (algorithms), section-4 (architecture/file layout), section-5 (tests),
> section-6 (perf), appendices (constants + worked examples). **Where this doc and the section files
> differ, the section files win** — this doc is a build order + signature sketch grounded in them and
> in the real #27/#16 source (verified July 23, 2026), not a new contract.
> **Purpose:** Turn #28 T0 into a per-file build list: exact files, headers, type/method signatures,
> constants (with tags), the FRs each satisfies, and the named `T-PG-*` tests — so the slice can be
> implemented and reviewed without re-deriving anything.

---

## 0. Scope of T0 (and what is explicitly NOT T0)

**T0 = the draw-free lifecycle core + the pure single-player regen generator**, behaviour-neutral,
depending only on **#27 `PlayerDatabase`** + **#16 `DeterministicSim`** (both landed). It is
**not** wired into any engine, owns no roster, writes no save. The stateful owner + persistence +
world-tick wiring are later phases:

| §4.2 file | Phase | Why |
|---|---|---|
| `PlayerProgressionConstants.cs` | **T0** | pure constants |
| `PlayerLifecycle.cs` | **T0** | value type (§2.2) |
| `TrainingInput.cs` | **T0** | value type, `Neutral` identity (§2.2 / FR-PG-009) |
| `AbilityModel.cs` | **T0** | pure: `ComputeCA` + `ClassifyAgeBand` + weighted spend (§3.1.2/§3.2) |
| `GrowthProjection.cs` | **T0** | pure per-player daily step (§3.1) |
| `RegenGenerator.cs` | **T0** | pure single-player generation (§3.3) — see the KD-B stream-const note |
| `RetirementResult.cs` / `RegenResult.cs` | **T0-optional** | trivial value types; land with their T2 consumer (`RunSeasonBoundary`) to avoid an unused type |
| `LifecycleViewModel.cs` | **T0-optional** | value type; its accessor is on `ProgressionEngine` (T2) — land with it |
| `ProgressionEngine.cs` | **T2** | owns the roster + `AdvanceDay`/`RunSeasonBoundary`; the sole writer (KD-7) |
| `ProgressionSaveCodec.cs` | **T1** | the `PROGRESSION_SAVE_FORMAT_VERSION` block (persistence) |

So T0 lands **6 production files + the asmdef + 4 test files + the tests asmdef** (§3–§4 below).

---

## 1. Two key decisions this drill surfaced (confirm before coding)

- **KD-A — the age model is `BirthWorldDay`, NOT `AgeAnchorDay`.** The APPROVED section files
  (FR-PG-005, §3.1.1) supersede the design supplement: age is **derived** —
  `AgeYears = (worldDay − BirthWorldDay) / DAYS_PER_YEAR` (integer division) — there is **no**
  `AgeAnchorDay` field and **no** discrete year-rollover step, so nothing can be double-counted at a
  year boundary. `PlayerRecord.Age` is kept **current** as a derived cache (the CA-cache pattern).
  `GrowthCursor` is **`long`** (integer fixed-point), not `int`. *(The parent plan
  `squad-player-stage1-plan.md` carried the stale `AgeAnchorDay`/`int` from the supplement — corrected
  there in the same change as this doc.)*
- **KD-B — `RegenGenerator` is T0; keep its stream ordinal faithful to §4.3 (no early `deterministic-sim`
  change).** `RegenGenerator` (pure) is a T0 deliverable (§10 T-phase plan), but its `T-PG-REG-001`
  determinism test must register a `player-progression.regen` stream, which needs an ordinal. §4.3 lands
  `SubsystemOrdinals.PlayerProgression = 82` + the **production** registration at the first regen (T2),
  to avoid a zero-draw phantom stream (FR-LW-031). **Keep that as-is** — do **not** add the const to
  `deterministic-sim` in T0. `RegisterStream` takes a plain `int subsystemOrdinal`, so the T0
  `RegenGeneratorTests` registers under the ordinal **value** via a documented test-local constant:
  ```csharp
  const int OrdinalPlayerProgression = 82;  // = the T2 SubsystemOrdinals.PlayerProgression (#16 §3.4); kept local until T2
  int s = rng.RegisterStream("player-progression.regen", OrdinalPlayerProgression, entityId: clubId, streamVersion: 1);
  ```
  This is a genuine draw site (the test draws), with **no** production stream and **no** early
  deterministic-sim change — faithful to both §10's T0 scope and §4.3's const timing. The production
  `SubsystemOrdinals.PlayerProgression = 82` + the `[CROSS]` mirrors in `PlayerProgressionConstants` +
  the production `RegisterStream` call all land together at **T2**. *(Alternative: defer `RegenGenerator`
  + `T-PG-REG-*` wholesale to T2 — but the test-local ordinal keeps the pure generator in T0 without
  touching #16.)*

---

## 2. Preflight (before the first file)

1. **CS0104 grep (FR §4.4):** confirm no existing type named `ProgressionEngine` / `PlayerLifecycle` /
   `GrowthProjection` / `AbilityModel` / `RegenGenerator` / `TrainingInput` / `PlayerProgressionConstants`
   in `src/**` or `docs/specs/**`. `PlayerProgression.PlayerAttributes` is **not** introduced (#28
   consumes #27's `PlayerDatabase.PlayerAttributes` directly), so the `AgentMovement.PlayerAttributes`
   CS0104 class #27 T1 hit does **not** recur.
2. **Header block** (every file, Code Standards #20 — the #27 source pattern):
   ```
   // File:     src/player-progression/<Name>.cs
   // Created:  2026-..-..
   // Modified: 2026-..-..
   // Author:   —
   // Spec:     Player Progression & Lifecycle #28 §<n> (<what>); Deterministic Simulation #16 (RNG); Code Standards #20
   // Purpose:  <one line>
   ```
   + a `#region VersionHistory` table at the foot.

---

## 3. Production files (T0)

### 3.1 `player-progression.asmdef`
```json
{
  "name": "TacticalDirector.PlayerProgression",
  "rootNamespace": "TacticalDirector.PlayerProgression",
  "references": [ "TacticalDirector.PlayerDatabase", "TacticalDirector.DeterministicSim" ],
  "autoReferenced": true,
  "noEngineReferences": true
}
```
Mirrors `player-database.asmdef` (no Unity engine ref — pure logic). **Nothing else referenced**
(FR §4.1: no season assembly, no match engine).

### 3.2 `PlayerProgressionConstants.cs` — Appendix A catalogue (region order Fixed → Derived → GT)
```csharp
public static class PlayerProgressionConstants
{
    // #region Fixed
    public const int  DAYS_PER_YEAR = 365;                       // [FIXED] age-derivation divisor (§3.1.1)
    public const uint PROGRESSION_SAVE_FORMAT_VERSION = 1;       // [FIXED] lifecycle sub-blob version (§3.5) — declared now, consumed at T1
    // #region Cross  (mirrors of #27 — the authority exists today)
    public const int ATTRIBUTE_MIN = PlayerDatabaseConstants.ATTRIBUTE_MIN;   // [CROSS] 1
    public const int ATTRIBUTE_MAX = PlayerDatabaseConstants.ATTRIBUTE_MAX;   // [CROSS] 20
    // #region Derived
    public const int PROGRESSION_REGEN_FIELDS =
        PlayerDatabaseConstants.IDENTITY_DRAWS_PER_PLAYER + PlayerDatabaseConstants.ATTRIBUTE_COUNT + 1; // [DERIVED] 5+31+1(PA) = 37 (§3.3 fixed budget)
    // #region GT  (illustrative pending the balance pass; shapes/tags are the contract — Appendix A)
    public const int  ABILITY_MAX          = 10000;             // [GT] wide-integer CA/PA scale ceiling
    public const long POINT_COST           = DAYS_PER_YEAR;     // [GT] cursor points per whole attribute-point ⇒ 1 step/yr with the band step (KD-8)
    public const int  GROWTH_AGE           = 24;                // [GT] < this ⇒ Growth band (§4.3)
    public const int  DECLINE_AGE          = 30;                // [GT] >= this ⇒ Decline band
    public const int  RETIREMENT_AGE       = 36;                // [GT] hard retirement, deterministic (§3.4)
    public const int  GROWTH_DAILY_POINTS  = +1;                // [GT] Growth-band daily cursor accrual
    public const int  DECLINE_DAILY_POINTS = -1;                // [GT] Decline-band daily cursor accrual (Stable = 0)
}
```
> **Deferred to T2** (KD-B): `DOMAIN_TAG_PLAYER_PROGRESSION = 0x20` + `SUBSYSTEM_ORDINAL_PLAYER_PROGRESSION = 82`
> `[CROSS]` mirrors (Appendix A) land with the production stream registration; `SubsystemOrdinals.PlayerProgression = 82`
> in `deterministic-sim` lands then too (or const-only in T0 per KD-B if `RegenGenerator`'s test needs it).

**Satisfies:** the constant-catalogue half of FR-PG-002/004/007/013. **Tests:** §4.1 `PlayerProgressionConstantsTests`.

### 3.3 `PlayerLifecycle.cs` — the per-player overlay (§2.2, KD-A)
```csharp
public struct PlayerLifecycle
{
    public int  PotentialAbility;   // ceiling, [0, ABILITY_MAX]; generated once, never rises (§3.2)
    public int  CurrentAbility;     // DERIVED cache of the [1,20] attrs (recomputed; never a 2nd accumulator, FR-PG-003)
    public long GrowthCursor;       // the ONLY accumulator — integer fixed-point points pool
    public uint BirthWorldDay;      // the authoritative age anchor (KD-A); age = (worldDay − this)/DAYS_PER_YEAR
    public bool RetirementFlag;     // set on the world tick at RETIREMENT_AGE (FR-PG-013)
    public uint RetirementDay;      // world-day the flag was set (0 if unflagged)
}
```
No `PlayerAttributes` here — the evolving `[1,20]` values stay on the career-state `PlayerRecord`
(#27 type) the T2 block holds (FR-PG-003/016). **Satisfies:** FR-PG-003 (structure). **Tests:** exercised via `GrowthProjectionTests`.

### 3.4 `TrainingInput.cs` — the #29 seam (§2.2 / FR-PG-008/009)
```csharp
public readonly struct TrainingInput
{
    public static TrainingInput Neutral => default;   // all-zero = the identity contribution (FR-PG-009)
    // Stage-3 #29 fields (focus / intensity / coach quality) APPEND here; the daily step READS them.
}
```
A **value type**, not an interface (FR §4.5 / FR-LW-031 — no `IProgressionInput` against the absent
#29 producer). **Satisfies:** FR-PG-009 structure. **Tests:** `T-PG-ID-002` (seam neutrality).

### 3.5 `AbilityModel.cs` — pure CA / age-band / weighted spend (§3.1.2 / §3.2)
```csharp
public static class AbilityModel
{
    public enum AgeBand { Growth, Stable, Decline }   // no separate AgeBand.cs — §4.2 keeps it here

    public static AgeBand ClassifyAgeBand(int ageYears)
        => ageYears <  PlayerProgressionConstants.GROWTH_AGE  ? AgeBand.Growth
         : ageYears >= PlayerProgressionConstants.DECLINE_AGE ? AgeBand.Decline
         : AgeBand.Stable;

    // Position-weighted mean of the 31 [1,20] attrs scaled to [0, ABILITY_MAX] (§3.2).
    // Weights derive from PlayerDatabaseConstants.PositionAttributeBias; the exact weighting is a
    // balance-pass [GT] detail — pin it against §3.2 at coding time.
    public static int ComputeCA(in PlayerAttributes attrs, PlayerPosition pos);

    // Raise the next attribute by the deterministic weighted order (§3.1.2): highest PositionAttributeBias
    // weight first, ties by ascending AttrIdx; SKIP an attr at ATTRIBUTE_MAX or whose raise would push
    // ComputeCA past potentialAbility (F1). Returns false if none is raisable (caller leaves the cursor).
    public static bool TrySpendOnePoint(ref PlayerRecord rec, int potentialAbility);

    // Symmetric decline: lower the next attribute by the mirror order (§3.1 drain).
    public static void DrainOnePoint(ref PlayerRecord rec);
}
```
**Satisfies:** FR-PG-003 (CA derived), FR-PG-004 (weighted spend + ceiling). **Tests:** §4.1 `AbilityModelTests`
(`T-PG-CA-001/002/003`).

### 3.6 `GrowthProjection.cs` — the pure per-player daily step (§3.1, the sole mutation path)
```csharp
public static class GrowthProjection
{
    // The §3.1 pseudocode verbatim — no RNG (FR-PG-002), the sole attribute-mutation path (FR-PG-008).
    public static void AdvanceDayForPlayer(
        ref PlayerRecord rec, ref PlayerLifecycle life, uint worldDay,
        in TrainingInput training, bool curveEnabled)
    {
        int age = (int)((worldDay - life.BirthWorldDay) / PlayerProgressionConstants.DAYS_PER_YEAR); // KD-A: derived, no rollover
        rec.Age = age;                                                                                // keep the record current (cache)
        var band = AbilityModel.ClassifyAgeBand(age);
        life.GrowthCursor += DailyPoints(band, rec.Position, in training, curveEnabled);
        while (life.GrowthCursor >= PlayerProgressionConstants.POINT_COST) {
            if (!AbilityModel.TrySpendOnePoint(ref rec, life.PotentialAbility)) break;                // ceiling: leave cursor (no thrash, F1)
            life.GrowthCursor -= PlayerProgressionConstants.POINT_COST;
        }
        while (life.GrowthCursor <= -PlayerProgressionConstants.POINT_COST) {
            AbilityModel.DrainOnePoint(ref rec);
            life.GrowthCursor += PlayerProgressionConstants.POINT_COST;
        }
        life.CurrentAbility = AbilityModel.ComputeCA(in rec.Attributes, rec.Position);                // derived (FR-PG-003)
    }

    // Signed integer daily accrual. curveEnabled OFF ⇒ the literal §4.3 band step (KD-8): Growth +1,
    // Decline −1, Stable 0; TrainingInput.Neutral adds 0. curveEnabled ON (T3) modulates by (PA−CA) + training.
    private static long DailyPoints(AbilityModel.AgeBand band, PlayerPosition pos, in TrainingInput t, bool curveEnabled);
}
```
**Satisfies:** FR-PG-001/002/003/004/005/007/008. **Tests:** §4.1 `GrowthProjectionTests`
(`T-PG-DET-001/002`, `T-PG-ID-001/002`).

### 3.7 `RegenGenerator.cs` — pure single-player generation (§3.3, mirrors `RosterGenerator.GenerateOne`)
```csharp
public static class RegenGenerator
{
    // Fixed-budget reservation (PROGRESSION_REGEN_FIELDS) so a regen consumes a constant #draws — the
    // #27 discipline. Mirrors RosterGenerator.GenerateOne (name/age/position/weakFoot/31 attrs) + a PA
    // draw; sets BirthWorldDay from the drawn (young) age and worldDay (KD-A). FRESH monotonic id (FR-PG-011).
    public static (PlayerRecord record, PlayerLifecycle life) GenerateRegen(
        DeterministicRngService rng, int streamIndex, int clubId, int newPlayerId, uint worldDay);
    // steps: Reserve(streamIndex, PROGRESSION_REGEN_FIELDS) -> DrawBounded per field (young age band, a [GT] detail)
    //        -> draw PA in [PA_MIN, ABILITY_MAX] -> generate 31 attrs BELOW PA (room to grow, FR-PG-010/§3.3 / T-PG-REG-003)
    //        -> CloseReservation -> BirthWorldDay = worldDay - age*DAYS_PER_YEAR
    //        -> PlayerRecord{ PlayerId = newPlayerId, ... }, PlayerLifecycle{ PotentialAbility = drawnPA, CurrentAbility = ComputeCA(...), GrowthCursor = 0, BirthWorldDay, RetirementFlag=false }
}
```
> **Nation** is not a #27 `PlayerRecord` field today, so T0 draws name/age/position/weakFoot/attrs/PA
> only (the "club/nation from the reference roster" of §3.3 is a forward reference; `clubId` scopes
> the id). The young-age band + PA distribution are `[GT]` balance details — pin against §3.3.

**Satisfies:** FR-PG-010/011 (+ FR-PG-012's *emit-don't-mutate* is honored by returning a value, not
touching a `Squad`). **Tests:** §4.1 `RegenGeneratorTests` (`T-PG-REG-001/003`; `T-PG-REG-002`
fresh-id is exercised more fully at T2 with the block). **Requires** KD-B (a registered stream for the test).

---

## 4. Test files (T0)

`tests/player-progression-tests.asmdef` — mirrors `player-database-tests.asmdef`:
```json
{ "name": "TacticalDirector.PlayerProgression.Tests",
  "references": [ "TacticalDirector.PlayerProgression", "TacticalDirector.PlayerDatabase",
                  "TacticalDirector.DeterministicSim", "UnityEngine.TestRunner", "UnityEditor.TestRunner" ],
  "includePlatforms": ["Editor"], "overrideReferences": true,
  "precompiledReferences": ["nunit.framework.dll"], "autoReferenced": false }
```

| File | Named tests (→ §5 IDs) | Locks |
|---|---|---|
| `PlayerProgressionConstantsTests.cs` | balance-pass invariants | `POINT_COST == DAYS_PER_YEAR`; `GROWTH_AGE < DECLINE_AGE < RETIREMENT_AGE`; `GROWTH_DAILY_POINTS == +1`, `DECLINE_DAILY_POINTS == −1`; `PROGRESSION_REGEN_FIELDS == IDENTITY_DRAWS_PER_PLAYER + ATTRIBUTE_COUNT + 1` *(the derivation, not the literal — confirm the `+1` PA-only draw count vs. §3.3)*; `ATTRIBUTE_MIN/MAX` mirror #27 (Appendix A) |
| `AbilityModelTests.cs` | `T-PG-CA-001/002/003` | `ComputeCA` recompute-equals-stored; spend clamps at PA ceiling (F1); weighted order raises signature attrs first, ties by ascending `AttrIdx` |
| `GrowthProjectionTests.cs` | `T-PG-DET-001/002`, `T-PG-ID-001/002` | Appendix B byte-exact growth ("save on any day == continuous" — at T0 there is no codec yet, so the restore is a **copied value-type snapshot** of `{PlayerRecord, PlayerLifecycle}`; value semantics stand in for the T1 codec round-trip, which `T-PG-SAVE-*` locks at T1); far-future gap == day-by-day (age gap-independent); `curveEnabled` off == literal §4.3 step; `TrainingInput.Neutral` == default(no-training) |
| `RegenGeneratorTests.cs` | `T-PG-REG-001/003` | same seed+club ⇒ identical record (fixed `PROGRESSION_REGEN_FIELDS` budget, bounds); attrs generated below PA. *(Registers the stream per KD-B.)* |

> **Deferred test IDs** (need the T1/T2 stateful engine + save): `T-PG-DET-003` (multi-season two-run),
> `T-PG-RET-*` (retirement/boundary), `T-PG-SAVE-*` (persistence), `T-PG-REG-002` (fresh-id across the
> block), `T-PG-SIM-001` (capstone). Listed in §9 of the parent plan.

---

## 5. Commit slicing & acceptance

Two PRs keep each reviewable and green under the **full dotnet gate** (`tools/dotnet-ci/run-gate.sh` —
whole tree green, quarantine empty, any new failure/compile-error fails CI):

1. **`feat(progression): #28 T0 draw-free lifecycle core`** — asmdef, `PlayerProgressionConstants`,
   `PlayerLifecycle`, `TrainingInput`, `AbilityModel`, `GrowthProjection` + `PlayerProgressionConstantsTests`
   / `AbilityModelTests` / `GrowthProjectionTests`. Fully RNG-free; no #16 stream touched.
2. **`feat(progression): #28 T0 regen generator`** — `RegenGenerator` + `RegenGeneratorTests` (the test
   registers its stream under a documented ordinal literal, KD-B). **No `deterministic-sim` change** —
   the `SubsystemOrdinals.PlayerProgression = 82` const + production registration land at T2.

**Acceptance:** both PRs green; T0 is behaviour-neutral by construction (no engine consumes it yet —
`git grep` shows zero references to the new assembly outside its own tests). Adversarial-review the code
each PR (the project's build-loop discipline) before merge. **Next:** T1 (`ProgressionSaveCodec` +
`ProgressionEngine.Snapshot/Restore`) — gated on Track B's #30 season frame for composition.

---

#### Version History
| Version | Date | Notes |
|---|---|---|
| 0.1 | 2026-07-23 | Initial file-by-file T0 plan, grounded in #28 section-2/3/4/5/6 + appendices (APPROVED) and verified #27/#16 source. Surfaces KD-A (BirthWorldDay supersedes the supplement's AgeAnchorDay; `GrowthCursor` is `long`) and KD-B (regen stream-const timing). Per-file signatures, constants (Appendix A tags), FR mapping, `T-PG-*` test assignment, 2-PR commit slice. |
