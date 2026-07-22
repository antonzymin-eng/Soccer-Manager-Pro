# Season & Competition Loop Specification #30 — Section 4: Architecture

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.2 — section-file PASS-1 reconciliation, §9.3)
**Version:** 0.2
**Status:** APPROVED
**Source:** `docs/tracking/season-competition-loop-design.md` v0.2

---

## 4.1 Assembly placement

The season loop lives in the existing **`TacticalDirector.SeasonSave`** assembly (`src/season-save/`)
— the composition/persistence layer that already sits **above both** `match-engine` and
`living-world` and is the only assembly permitted to reference both (FR-LW-003 keeps those two
independent; the season root is their `match-viewer`-over-`match-engine` layer class). #30 adds no new
cross-layer reference direction: it extends a root that already references
`TacticalDirector.MatchEngine` + `TacticalDirector.LivingWorld` + `TacticalDirector.DeterministicSim`,
and (for FR-SN-012's `ConfigureSquads` / `ISquadProvider`) the `TacticalDirector.PlayerDatabase`
reference the season-save tests already carry.

```
                 TacticalDirector.SeasonSave   (season loop lives here — above both)
                    │            │
        ┌───────────┘            └───────────┐
   match-engine                         living-world
   (#1..#17 substrate)                  (#22 — never references match-engine, FR-LW-003)
        │                                    │
        └──────────── deterministic-sim ─────┘   (#16 — referenced by all)
```

**No new assembly.** `SeasonLoop`, `FixtureScheduler`, `LeagueTable`, `SeasonCalendar`, `BoardState`,
`MatchResult`, `SeasonState`, `SeasonViewModel` are new files under `src/season-save/`, alongside
`SeasonSaveManager`/`SeasonSaveCodec`/`SeasonSaveConstants`.

## 4.2 File layout (proposed — pinned at T-phase)

```
src/season-save/
├── season-save.asmdef            ← existing; gains PlayerDatabase ref (already in the tests asmdef)
├── SeasonSaveConstants.cs        ← existing; SEASON_SAVE_FORMAT_VERSION 1 → 2; + SEASON_STATE_FORMAT_VERSION
├── SeasonSaveCodec.cs            ← existing; Encode/Decode gain the season block (§4.4)
├── SeasonSaveContents.cs         ← existing; gains the reconstructed SeasonState
├── SeasonSaveManager.cs          ← existing; Save/Load gain the season parameter (§4.4)
├── SeasonState.cs                ← NEW: the serialized season surface (§2.2)
├── Fixture.cs                    ← NEW
├── FixtureScheduler.cs           ← NEW: static, pure Generate(clubIds, seed) → Fixture[]
├── LeagueTableRow.cs             ← NEW
├── LeagueTable.cs                ← NEW: ApplyResult + OrderedView (tie-break)
├── SeasonCalendar.cs             ← NEW
├── BoardObjective.cs             ← NEW
├── BoardState.cs                 ← NEW
├── MatchResult.cs                ← NEW: the match-outcome producer payload
├── SeasonViewModel.cs            ← NEW: read-only surface for #37/#38
├── SeasonLoop.cs                 ← NEW: the composition root (sole writer; command API; Snapshot/Restore)
├── SeasonStateCodec.cs           ← NEW: the season sub-blob codec (§3.6) — pure, testable in memory
└── tests/
    ├── season-save-tests.asmdef  ← existing
    ├── FixtureSchedulerTests.cs  ← NEW
    ├── LeagueTableTests.cs       ← NEW
    ├── SeasonStateCodecTests.cs  ← NEW
    ├── SeasonLoopTests.cs        ← NEW: day-advance / play-fixture / boundary-roll / round-trip
    └── SeasonSaveManagerTests.cs ← existing; extended for the season parameter
```

## 4.3 The `SeasonLoop` composition root

`SeasonLoop` (sealed) is the **sole writer** of `SeasonState` (KD-7 / FR-SN-032). It holds:

- `SeasonState _state` — the season sub-blob's surface (owned; never handed out by reference).
- `WorldStore _world` — the day-advance substrate (referenced, not owned; #22 owns its lifecycle).
- `MatchEngine _activeMatch` — the in-progress fixture, or null between fixtures (KD-3 / KD-1
  matchPresent flag).

Public command API (the only mutation path, FR-SN-032): `AdvanceToNextFixtureDay()`,
`AdvanceAndPlayNextRound(ISquadProvider)` (resolves the whole round, KD-9), `RollToNextSeason()`, plus
read-only `View()` → `SeasonViewModel`
(FR-SN-033) and `Snapshot()` / `Restore(...)` for the season sub-blob. It is **not** on the 60 Hz hot
path (§1.2 world-tick cadence), so allocation / `new` / exceptions are permitted — the
`SeasonSaveManager` / `WorldStore` precedent.

## 4.4 The `SeasonSaveManager` / `SeasonSaveCodec` signature change (FR-SN-019..021)

Today (from source):

```csharp
// SeasonSaveManager.cs
public static void Save(WorldStore world, MatchEngine matchOrNull, string path)
public static SeasonSaveContents Load(string path, ISquadProvider squads = null)

// SeasonSaveCodec.cs
public static byte[] Encode(byte[] worldBlob, byte[] matchBlobOrNull)
public static SeasonSaveBlobs Decode(byte[] blob)
```

After #30 (the season block is **always present**, unlike the optional match block):

```csharp
// SeasonSaveManager.cs
public static void Save(WorldStore world, SeasonState season, MatchEngine matchOrNull, string path)
public static SeasonSaveContents Load(string path, ISquadProvider squads = null)   // Contents gains .Season

// SeasonSaveCodec.cs
public static byte[] Encode(byte[] worldBlob, byte[] seasonBlob, byte[] matchBlobOrNull)
public static SeasonSaveBlobs Decode(byte[] blob)   // SeasonSaveBlobs gains .SeasonBlob (always non-null)
```

`Save` captures all three blobs (`world.Snapshot()`, `SeasonStateCodec.Encode(season)`,
`MatchSaveManager.Encode(matchOrNull)`) **before** opening the file — the existing blob-before-file /
atomic temp→fsync→rename contract (§4.6.1.1) is unchanged; only a third length-prefixed block joins
the frame. `SEASON_SAVE_FORMAT_VERSION` bumps **1 → 2** (the codec's own "bump only on a season-frame
layout change" rule; adding a block is exactly one). The world and match sub-blobs stay opaque and
byte-untouched — no `WORLD_STORE_FORMAT_VERSION` / `MATCH_SAVE_FORMAT_VERSION` change (FR-SN-020).

**Frame layout (KD-1):** `SEASON_SAVE_FORMAT_VERSION → matchPresent flag → [len]world → [len]season →
([len]match iff matchPresent)`. The `matchPresent` flag keeps its current meaning (a season between
fixtures has a world + season but no match); the season block is unconditional.

## 4.5 RNG-stream registration (FR-SN-027 / KD-5)

`SeasonLoop` registers a `DeterministicRngService` stream at construction (or reuses the
`WorldStore`'s service if that is the chosen composition — a T-phase decision) with siteId
`"season-loop.season-events"`, `SubsystemOrdinals.SeasonLoop = 84`, `entityId: SeasonNumber`. The
generator is a pure static that takes an already-drawn permutation (or the identity), so
`FixtureScheduler` is testable without a season boot — the #27 `RosterGenerator` stateless posture.
Back-prop (§7): `DOMAIN_TAG_SEASON_LOOP = 0x22` + `SubsystemOrdinals.SeasonLoop = 84` land in #16 §3.4
at approval (only #30's row; `0x20`/`0x21` stay gaps for #28/#29 per KD-5's honesty note).

## 4.6 The #22 producer-not-consumer boundary (KD-3 / FR-SN-016..018)

`SeasonLoop.EmitMatchOutcome(result)` records the `MatchResult` in `SeasonState` and is the phase-1
**producer**. It **does not** call any #22 ingest method — none exists (`WorldLoop` phase-1 has no
interface, FR-LW-031), and #30 **must not add one** (FR-SN-017). The only #22 surface #30 touches is
`WorldStore`'s public API (`AdvanceDay`/`Snapshot`/`Restore`/`CurrentWorldTick`), never `living-world`
internals (FR-SN-018 / FR-LW-003). The eventual ingest is a #22 wiring change at #33's landing
(`FR-LW-032`), co-defining the payload against `FR-LW-027`/`FR-LW-032`/living-world KD-9/KD-10 — a #22 edit, not a
#30 one.

## 4.7 The CS0104 hazard note (carried from #27 T1)

`SeasonState` / `MatchResult` are new names in `SeasonSave`, so no collision today. But `SeasonLoop`
already sees `MatchEngine.MatchEngine` (namespace == class, requiring the `TacticalDirector.MatchEngine.MatchEngine`
fully-qualified form the existing `SeasonSaveManager` uses) and, via `ConfigureSquads`,
`PlayerDatabase.PlayerAttributes` (the #27 v1.73 CS0104 class). The T-phase implementer must
fully-qualify `MatchEngine` and any `player-database` type that shares a bare name with a
`match-engine` type **from the first line that needs it**, not discover it via a failed build (the
#27 §4 / `src/CLAUDE.md` v1.73 lesson).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial architecture: SeasonSave-assembly extension, file layout, SeasonLoop root, the codec/manager signature change, RNG registration, the #22 producer boundary, CS0104 hazard. |
| 0.2 | 2026-07-22 | — | Section-file PASS-1 reconciliation (whole-round KD-9 command/API rename, living-world-KD disambiguation, KD/FR label fixes). See section-9 §9.3. |
#endregion
