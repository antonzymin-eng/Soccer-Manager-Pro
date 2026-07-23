# Squad / Player Data Layer Specification #27 — Section 4: Architecture and File Layout

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.1)
**Version:** 0.1
**Status:** APPROVED

---

## 4.1 New assembly (bottom of the reference graph)

Unlike #23–#26 (which add files to existing assemblies), this spec introduces a **new**
bottom-of-graph assembly `TacticalDirector.PlayerDatabase` at `src/player-database/`. Its asmdef
references exactly one other assembly (KD-5):

```json
"references": [ "TacticalDirector.DeterministicSim" ]
```

`DeterministicSim` supplies `DeterministicRngService` + `SubsystemOrdinals` for roster generation;
nothing else is needed. The assembly does **not** reference `match-engine`, `positioning-ai`, or any
Physics/Mechanics/AI layer (KD-4) — it is a pure data layer that every consumer references, never the
reverse. `noEngineReferences` is true (no `UnityEngine` dependency).

## 4.2 File placement

Nine source files, one public type each (Code Standards #20 file-naming):

| File | Role |
|---|---|
| `PlayerDatabaseConstants.cs` | Fixed/Derived/GT catalogue incl. the `[4][31]` position-bias table (FR-SQ-011/014) |
| `AttrIdx.cs` | the single 31-way ordinal map (FR-SQ-006); shared by the array round-trip, generator, loader |
| `PlayerAttributes.cs` | canonical 31×`int[1,20]` record + `WeakFootRating [1,5]` (FR-SQ-001..006) |
| `PlayerPosition.cs` | coarse 4-value enum — NOT positioning-ai's `RoleId` (FR-SQ-007, KD-4) |
| `PlayerRecord.cs` | one player: club-scoped `PlayerId` + name/age/position + attributes (FR-SQ-008) |
| `Squad.cs` | one club's roster (`≤ CLUB_SQUAD_SIZE = 25`) (FR-SQ-009) |
| `NameCatalogue.cs` | Stage-0 in-code first/last name pools, 32 each, append-only (FR-SQ-017) |
| `RosterGenerator.cs` | deterministic generation over `DeterministicRngService`; stateless (FR-SQ-012/013) |
| `SquadFileLoader.cs` | Stage-0 human-authoring text import (FR-SQ-018/019, KD-8) |

Tests: `tests/PlayerAttributesTests.cs`, `tests/RosterGeneratorTests.cs`, `tests/SquadFileLoaderTests.cs`
under `player-database-tests.asmdef` (references the production asmdef + `DeterministicSim`).

## 4.3 RNG-stream registration contract (caller-owned)

`RosterGenerator` is **stateless** — it never registers or owns an RNG stream, so it is unit-testable
without booting a match (design supplement AR-1 #4). The **caller** registers the stream and passes the
resulting `streamIndex` in (FR-SQ-013):

```csharp
int idx = rng.RegisterStream("player-database.roster-generation",
                             SubsystemOrdinals.PlayerDatabase, // = 81 (off-pitch 80–99 band)
                             entityId: clubId, streamVersion: 1);
Squad squad = RosterGenerator.Generate(rng, idx, clubId, count);
```

`entityId: clubId` keys each club's draw sequence, so two clubs generated from one service never share a
cursor. Per player, `Generate` runs one `Reserve(FIELDS_PER_PLAYER = 36)` → 36× `DrawReserved` (identity
draws in `AttrIdx`-derived order, then 31 attribute draws) → `CloseReservation` — a fixed budget the F4
count assertion locks (§5). Determinism flows entirely through `DeterministicRngService`; there is no
`System.Random` anywhere in the assembly (KD-5). `DOMAIN_TAG_PLAYER_DATABASE = 0x1F` +
`SubsystemOrdinals.PlayerDatabase = 81` are `[CROSS]` mirrors allocated in DeterministicSim #16 §3.4
(ERR-022-001 back-prop precedent).

## 4.4 T1 CS0104 fully-qualify hazard

`PlayerDatabase.PlayerAttributes` shares its bare type name with the pre-existing, unrelated
`AgentMovement.PlayerAttributes` (a narrower Physical-only struct). No collision exists inside this
assembly. The moment a consumer references **both** assemblies — as the T1 match-engine wiring does —
every bare `PlayerAttributes` in that scope becomes ambiguous (CS0104), the exact defect class the
project hit at `src/CLAUDE.md` v1.73 (`TacticTranslation`). The T1 writer resolves it by fully-qualifying
`TacticalDirector.PlayerDatabase.PlayerAttributes` (KD-P6) from the first line that needs it — not by
discovering the error via a failed build (design supplement §4). This spec records the hazard; the
mitigation lives in the match-engine wiring, not here.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial architecture: new bottom-of-graph assembly, file placement, RNG-stream contract, CS0104 hazard. |
#endregion
